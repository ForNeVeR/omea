/* vim: set sw=8: -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/**
 * ms-ole.c: MS Office OLE support for Gnumeric
 *
 * Authors:
 *    Michael Meeks (michael@ximian.com)
 *    Arturo Tena   (arturo@directmail.org)
 *    Dom Lachowicz (doml@appligent.com)
 *
 * Copyright 1998-2000 Helix Code, Inc., Arturo Tena
 **/

/* #include <libole2-config.h> */
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif

#include <libole2/ms-ole.h>

/* BSDs require unistd.h before including stat.h */
#ifdef HAVE_UNISTD_H
#	include <unistd.h>
#else
#	define S_IRUSR 0000400
#	define S_IWUSR 0000200
#	define S_IRGRP 0000040
#	define S_IWGRP 0000020
#	define _S_ISREG(m) (((m)&0170000) == 0100000)
#	define S_ISREG(m) _S_ISREG(m)
#	define O_NONBLOCK 0x4000
#endif

#include <sys/stat.h>	/* for struct stat */
#include <fcntl.h>
#include <stdio.h>
#include <assert.h>
#include <ctype.h>
#include <string.h>

#ifdef __MINGW32__ /* incomplete defines on mingw */

#define _S_IXGRP        _S_IEXEC
#define _S_IWGRP        _S_IWRITE
#define _S_IRGRP        _S_IREAD

#define S_IXGRP         _S_IXGRP
#define S_IWGRP         _S_IWGRP
#define S_IRGRP         _S_IRGRP

#endif
 
#ifdef HAVE_MMAP
/*lvm007@aha.ru fix*/
#if defined WIN32
#include "winmmap.h"
#else
#include <sys/mman.h>
#endif
#endif 

#ifndef PROT_READ
#define PROT_READ 0x1
#endif

#ifndef PROT_WRITE
#define PROT_WRITE 0x2
#endif

#if !defined(MAP_FAILED) || defined(__osf__)
/* Someone needs their head examining - BSD ? */
#	define MAP_FAILED ((void *)-1)
#endif

#if !defined(MAP_SHARED) || !defined(HAVE_MMAP)
/* Only define this where mmap() is not supported */
#	define MAP_SHARED 0
#endif

/* Implementational detail - not for global header */
#define OLE_DEBUG 0
#define OLE_CHAIN_DEBUG 0

/* FIXME tenix add ADD_BBD_LIST_BLOCK where it should be used) */
#define ADD_BBD_LIST_BLOCK   0xfffffffc       /* -4 */
#define SPECIAL_BLOCK        0xfffffffd       /* -3 (BBD_LIST BLOCK) */
#define END_OF_CHAIN         0xfffffffe       /* -2 */
#define UNUSED_BLOCK         0xffffffff       /* -1 */

/* FIXME tenix laola reads this from the header */
#define BB_BLOCK_SIZE     512
#define SB_BLOCK_SIZE      64

/* FIXME tenix laola understand the next header:
      MAGIC     => undef,       #      00
      CLSID     => undef,       # guid 08
      REVISION  => undef,       # word 18
      VERSION   => undef,       # word 1a
      BYTEORDER => undef,       # word 1c
      B_S_LOG   => undef,       # word 1e       big block size = 2^b_s_log
      S_S_LOG   => undef,       # word 20       small block size = 2^s_s_log
      UK1       => undef,       # word(5) 22
      B_D_NUM   => undef,       # long 2c       bbd num of blocks
      ROOT_SB   => undef,       # long 30       root start block
      UK2       => undef,       # long 34
      B_S_MIN   => undef,       # long 38       minimum size of big_block
      S_D_SB    => undef,       # long 3c       sbd start block
      S_D_NUM   => undef,       # long 40       number of sbd blocks
      B_XD_SB   => undef,       # long 44
      B_XD_NUM  => undef,       # long 48
 */


/**
 * Structure describing an OLE file
 **/
struct _MsOle {
	int               ref_count;
	gboolean          ole_mmap;
	guint8           *mem;
	guint32           length;
	MsOleSysWrappers *syswrap;

	char              mode;
	MsOleHandleType   file_des;
	int               dirty;
	GArray           *bb;      /* Big  blocks status  */
	GArray           *sb;      /* Small block status  */
	GArray           *sbf;     /* The small block file */
	guint32           num_pps; /* Count of number of property sets */
	GList            *pps;     /* Property Storage -> struct _PPS, always 1 valid entry or NULL */
/* if memory mapped */
	GPtrArray        *bbattr;  /* Pointers to block structures */
/* end if memory mapped */
};

#define BLOCK_COUNT(f) (((f)->length + BB_BLOCK_SIZE - 1) / BB_BLOCK_SIZE)

static MsOleHandleType
open2_wrap (const char *pathname, int flags, gpointer closure)
{
	gint fd;
#ifdef O_BINARY
	fd = open (pathname, flags | O_BINARY);
#else
	fd = open (pathname, flags);
#endif

	if (fd == -1)
		return BAD_MSOLE_HANDLE;
	return GINT_TO_POINTER(fd);
}

static MsOleHandleType
open3_wrap (const char *pathname, int flags, mode_t mode, gpointer closure)
{
	gint fd;
#ifdef O_BINARY
	fd = open (pathname, flags | O_BINARY, mode);
#else
	fd = open (pathname, flags, mode);
#endif

	if (fd == -1)
		return BAD_MSOLE_HANDLE;
	return GINT_TO_POINTER(fd);
}

static ssize_t
read_wrap (MsOleHandleType fd, void *buf, size_t count, gpointer closure)
{
	return read (GPOINTER_TO_INT(fd), buf, count);
}

static int
close_wrap (MsOleHandleType fd, gpointer closure)
{
	return close (GPOINTER_TO_INT(fd));
}

static ssize_t
write_wrap (MsOleHandleType fd, const void *buf, size_t count,
	    gpointer closure)
{
	return write (GPOINTER_TO_INT(fd), (void *)buf, count);
}

static off_t
lseek_wrap (MsOleHandleType fd, off_t offset, int whence, gpointer closure)
{
	return lseek (GPOINTER_TO_INT(fd), offset, whence);
}

static int
isregfile_wrap (MsOleHandleType fd, gpointer closure)
{
	struct stat st;

	if (fstat (GPOINTER_TO_INT(fd), &st))
		return 0;

	return S_ISREG (st.st_mode);
}

static int
getfilesize_wrap (MsOleHandleType fd, guint32 *size, gpointer closure)
{
	struct stat st;

	if (fstat (GPOINTER_TO_INT(fd), &st))
		return -1;

	*size = st.st_size;
	return 0;
}

#if defined(HAVE_MMAP)
static void *
mmap_wrap (void *start, size_t length, int prot,
	   int flags, MsOleHandleType fd, off_t offset, gpointer closure)
{
	return mmap (start, length, prot, flags, GPOINTER_TO_INT(fd), offset);
}

static int
munmap_wrap (void *start, size_t length, gpointer closure)
{
	return munmap (start, length);
}
#endif /* HAVE_MMAP */

static MsOleSysWrappers ms_ole_default_wrappers = {
	open2_wrap,
	open3_wrap,
	read_wrap,
	close_wrap,
	write_wrap,
	lseek_wrap,
	isregfile_wrap,
	getfilesize_wrap,

#if defined(HAVE_MMAP)
	mmap_wrap,
	munmap_wrap,
#else
	NULL,
	NULL,
#endif
	NULL /* no closure */
};

/*
 * The default filesystem wrappers
 */
static MsOleSysWrappers * default_wrappers = &ms_ole_default_wrappers;

static void
take_wrapper_functions (MsOle *f, MsOleSysWrappers *wrappers)
{
	if (wrappers == NULL)
		f->syswrap = default_wrappers;
	else
		f->syswrap = wrappers;
}

/*
 * Initialize MS-OLE
 * @wrappers - if NULL, use the default FILE* based filesystem
 * else use the supplied filesystem
 */
void
ms_ole_init (MsOleSysWrappers *wrappers)
{
	static gboolean bInitted = FALSE;
	g_return_if_fail(!bInitted);

	if (wrappers != NULL)
		default_wrappers = wrappers;
	bInitted = TRUE;
}

/*
 * Always returns the default FILE* based filesystem
 */
MsOleSysWrappers *
ms_ole_get_default_fs (void)
{
	return &ms_ole_default_wrappers;
}

gint ms_ole_stream_read_copy   (MsOleStream *stream,
				guint8 *ptr,
				MsOlePos length)
{
	g_return_val_if_fail (stream != NULL, -1);
	return stream->read_copy(stream, ptr, length);
}

guint8 * ms_ole_stream_read_ptr     (MsOleStream *stream,
				     MsOlePos length)
{
	g_return_val_if_fail(stream != NULL, NULL);
	return stream->read_ptr(stream, length);
}

MsOleSPos ms_ole_stream_lseek        (MsOleStream *stream,
				      MsOleSPos bytes,
				      MsOleSeek type)
{
	g_return_val_if_fail (stream != NULL, -1);
	return stream->lseek (stream, bytes, type);
}

MsOlePos  ms_ole_stream_tell         (MsOleStream *stream)
{
	g_return_val_if_fail (stream != NULL, -1);
	return stream->tell(stream);
}

MsOlePos  ms_ole_stream_write       (MsOleStream *stream,
				     guint8 *ptr,
				     MsOlePos length)
{
	g_return_val_if_fail (stream != NULL, -1);
	return stream->write(stream, ptr, length);
}

/*
 * A global variable to enable calles to check_stream,
 * applications should optionally enable due to the performance penalty.
 * of 30-50 % of load time.
 */
gboolean libole2_debug = FALSE;

typedef guint32 PPS_IDX ;

#if OLE_DEBUG > 0
/* Very grim, but quite necessary */
#       define ms_array_index(a,b,c) (b)my_array_hack ((a), sizeof(b), (c))

static guint32
my_array_hack (GArray *a, guint s, guint32 idx)
{
	g_assert (a != NULL);
	g_assert (idx >= 0);
	g_assert (idx < a->len);
	g_assert (s == 4);
	return ((guint32 *)a->data)[idx];
}
#else
/* Far far faster... */
#       define ms_array_index(a,b,c) g_array_index (a, b, c)
#endif


typedef guint32 BLP;	/* Block pointer */


#define BB_THRESHOLD   0x1000

#define PPS_ROOT_INDEX    0
#define PPS_BLOCK_SIZE 0x80
#define PPS_END_OF_CHAIN 0xffffffff

typedef struct _PPS PPS;

#define PPS_SIG 0x13579753
#define IS_PPS(p) (((PPS *)(p))->sig == PPS_SIG)

struct _PPS {
	int      sig;
	char    *name;
	GList   *children;
	PPS     *parent;
	guint32  size;
	BLP      start;
	MsOleType type;
	PPS_IDX  idx; /* Only used on write */
};

#define BB_R_PTR(f,b) ((f)->ole_mmap ? ((f)->mem + ((b) + 1) * BB_BLOCK_SIZE) : \
				       (get_block_ptr (f, b, FALSE)))
#define BB_W_PTR(f,b) ((f)->ole_mmap ?  BB_R_PTR ((f),(b)) :			\
				       (get_block_ptr ((f), (b), TRUE)))

#define GET_SB_R_PTR(f,b) (BB_R_PTR(f, g_array_index ((f)->sbf, BLP, (b)/(BB_BLOCK_SIZE/SB_BLOCK_SIZE))) \
			   + (((b)%(BB_BLOCK_SIZE/SB_BLOCK_SIZE))*SB_BLOCK_SIZE))
#define GET_SB_W_PTR(f,b) (BB_W_PTR(f, g_array_index ((f)->sbf, BLP, (b)/(BB_BLOCK_SIZE/SB_BLOCK_SIZE))) \
			   + (((b)%(BB_BLOCK_SIZE/SB_BLOCK_SIZE))*SB_BLOCK_SIZE))

#define MAX_CACHED_BLOCKS  32

typedef struct {
	guint32  blk;
	gboolean dirty;
	int      usage;
	guint8   *data;
} BBBlkAttr;

static BBBlkAttr *
bb_blk_attr_new (guint32 blk)
{
	BBBlkAttr *attr = g_new (BBBlkAttr, 1);
	attr->blk   = blk;
	attr->dirty = FALSE;
	attr->usage = 0;
	attr->data  = 0;
	return attr;
}

static void
write_cache_block (MsOle *f, BBBlkAttr *attr)
{
	size_t offset;

	g_return_if_fail (f);
	g_return_if_fail (attr);
	g_return_if_fail (attr->data);

	offset = (attr->blk+1)*BB_BLOCK_SIZE;
	if (f->syswrap->lseek (f->file_des, offset, SEEK_SET, f->syswrap->closure) == (off_t)-1 ||
	    f->syswrap->write (f->file_des, attr->data, BB_BLOCK_SIZE, f->syswrap->closure) == -1)
		g_warning ("Fatal error writing block %d at %d\n", attr->blk, offset);
#if OLE_DEBUG > 2
	g_print ("Writing cache block %d to offset %d\n",
		 attr->blk, offset);
#endif
	attr->dirty = FALSE;
}

static guint8 *
get_block_ptr (MsOle *f, BLP b, gboolean forwrite)
{
	BBBlkAttr *attr, *tmp, *min;
	size_t offset;
	guint32 i, blks;

	g_assert (f);
	g_assert (b < f->bbattr->len);

	/* Have we cached it ? */
	attr = g_ptr_array_index (f->bbattr, b);
	g_assert (attr);
	g_assert (attr->blk == b);

	if (attr->data) {
		attr->usage++;
		if (forwrite)
			attr->dirty = TRUE;
		return attr->data;
	}

	/* LRU strategy */
	min  = NULL;
	blks = 0;
	for (i = 0;i<f->bbattr->len;i++) {
		tmp = g_ptr_array_index (f->bbattr, i);
		if (tmp->data) {
			blks++;
			if (!min)
				min = tmp;
		        else if (tmp->usage < min->usage)
				min = tmp;
		}
		tmp->usage = (guint32)(tmp->usage*0.707);
	}
	if (blks < MAX_CACHED_BLOCKS)
		min = 0;

	g_assert (!attr->data);
	if (min) {
		g_assert (min->data);
#if OLE_DEBUG > 2
		g_print ("Replacing cache block %d with %d\n", min->blk, b);
#endif
		if (min->dirty)
			write_cache_block (f, min);
		attr->data  = min->data;
		min->data   = 0;
		min->usage  = 0;
	} else
		attr->data = g_new (guint8, BB_BLOCK_SIZE);

	offset = (b+1)*BB_BLOCK_SIZE;
	f->syswrap->lseek (f->file_des, offset, SEEK_SET, f->syswrap->closure);
	(f->syswrap->read) (f->file_des, attr->data, BB_BLOCK_SIZE, f->syswrap->closure);
	attr->usage = 1;
	attr->dirty = forwrite;

	return attr->data;
}


/* This is a list of big blocks which contain a flat description of all blocks
   in the file. Effectively inside these blocks is a FAT of chains of other BBs,
   so the theoretical max size = 128 BB Fat blocks, thus = 128*512*512/4 blocks
   ~= 8.4MBytes */
/* FIXME tenix the max size would actually be 109*512*512/4 + 512 blocks ~=
   7MBytes if we don't take in count the additional Big Block Depot lists.
   Number of additional lists is in header:0x48, the location of the first
   additional list is in header:0x44, the location of the second additional
   list is at the very end of the first additional list and so on, the last
   additional list have at the end a END_OF_CHAIN.
   Each additional list can address 128*512/4*512 blocks ~= 8MBytes */
/* The number of Big Block Descriptor (fat) Blocks */
#define GET_NUM_BBD_BLOCKS(f)   (MS_OLE_GET_GUINT32 ((f)->mem + 0x2c))
#define SET_NUM_BBD_BLOCKS(f,n) (MS_OLE_SET_GUINT32 ((f)->mem + 0x2c, (n)))
/* The block locations of the Big Block Descriptor Blocks */
#define MAX_SIZE_BBD_LIST           109
/* FIXME tenix next is broken with big files */
#define GET_BBD_LIST(f,i)           (MS_OLE_GET_GUINT32 ((f)->mem + 0x4c + (i)*4))
/* FIXME tenix next is broken with big files */
#define SET_BBD_LIST(f,i,n)         (MS_OLE_SET_GUINT32 ((f)->mem + 0x4c + (i)*4, (n)))
#define NEXT_BB(f,n)                (g_array_index ((f)->bb, BLP, n))
#define NEXT_SB(f,n)                (g_array_index ((f)->sb, BLP, n))
/* Additional Big Block Descriptor (fat) Blocks */
#define MAX_SIZE_ADD_BBD_LIST       127
#define GET_NUM_ADD_BBD_LISTS(f)   (MS_OLE_GET_GUINT32 ((f)->mem + 0x48))
#define GET_FIRST_ADD_BBD_LIST(f)  (MS_OLE_GET_GUINT32 ((f)->mem + 0x44))

/* Get the start block of the root directory ( PPS ) chain */
#define GET_ROOT_STARTBLOCK(f)   (MS_OLE_GET_GUINT32 ((f)->mem + 0x30))
#define SET_ROOT_STARTBLOCK(f,i) (MS_OLE_SET_GUINT32 ((f)->mem + 0x30, i))
/* Get the start block of the SBD chain */
#define GET_SBD_STARTBLOCK(f)    (MS_OLE_GET_GUINT32 ((f)->mem + 0x3c))
#define SET_SBD_STARTBLOCK(f,i)  (MS_OLE_SET_GUINT32 ((f)->mem + 0x3c, i))


/* NB it is misleading to assume that Microsofts linked lists link correctly.
   It is not the case that pps_next(f, pps_prev(f, n)) = n ! For the final list
   item there are no valid links. Cretins. */
#define PPS_GET_NAME_LEN(p)   (MS_OLE_GET_GUINT16 (p + 0x40))
#define PPS_SET_NAME_LEN(p,i) (MS_OLE_SET_GUINT16 (p + 0x40, (i)))
#define PPS_GET_PREV(p)   ((PPS_IDX) MS_OLE_GET_GUINT32 (p + 0x44))
#define PPS_GET_NEXT(p)   ((PPS_IDX) MS_OLE_GET_GUINT32 (p + 0x48))
#define PPS_GET_DIR(p)    ((PPS_IDX) MS_OLE_GET_GUINT32 (p + 0x4c))
#define PPS_SET_PREV(p,i) ((PPS_IDX) MS_OLE_SET_GUINT32 (p + 0x44, i))
#define PPS_SET_NEXT(p,i) ((PPS_IDX) MS_OLE_SET_GUINT32 (p + 0x48, i))
#define PPS_SET_DIR(p,i)  ((PPS_IDX) MS_OLE_SET_GUINT32 (p + 0x4c, i))
/* These get other interesting stuff from the PPS record */
#define PPS_GET_STARTBLOCK(p)      ( MS_OLE_GET_GUINT32 (p + 0x74))
#define PPS_GET_SIZE(p)            ( MS_OLE_GET_GUINT32 (p + 0x78))
#define PPS_GET_TYPE(p) ((MsOleType)( MS_OLE_GET_GUINT8 (p + 0x42)))
#define PPS_SET_STARTBLOCK(p,i)    ( MS_OLE_SET_GUINT32 (p + 0x74, i))
#define PPS_SET_SIZE(p,i)          ( MS_OLE_SET_GUINT32 (p + 0x78, i))
#define PPS_SET_TYPE(p,i)          ( MS_OLE_SET_GUINT8  (p + 0x42, i))

/* Try to mark the Big Block "b" as as unused if it is marked as "c", in the
   FAT "f". */
#define TRY_MARK_UNUSED_BLOCK(f,block,mark) {                                    \
        if (g_array_index ((f), BLP, (block)) != (mark)) {			 \
	        g_warning ("Tried to mark as unused the block %d which has %d\n",\
                   (block), g_array_index ((f), BLP, (block)));                  \
        } else { g_array_index ((f), BLP, (block)) = UNUSED_BLOCK; } }

/* FIXME: This needs proper unicode support ! current support is a guess */
/* Length is in bytes == 1/2 the final text length */
/* NB. Different from biff_get_text, looks like a bug ! */
static char *
pps_get_text (guint8 *ptr, int length)
{
	int lp;
	char *ans;
	guint16 c;
	guint8 *inb;

	length = (length+1)/2;

	if (length <= 0 ||
	    length > (PPS_BLOCK_SIZE/4)) {
#if OLE_DEBUG > 0
		g_print ("Nulled name of length %d\n", length);
#endif
		return 0;
	}

	ans = (char *) g_malloc (sizeof (char) * length + 1);

	inb = ptr;
	for (lp = 0; lp < length; lp++) {
		c = MS_OLE_GET_GUINT16 (inb);
		ans [lp] = (char) c;
		inb += 2;
	}
	ans [lp] = 0;

	return ans;
}

static void
dump_header (MsOle *f)
{
	g_print ("--------------------------MsOle HEADER-------------------------\n");
	g_print ("Num BBD Blocks : %d Root %%d, SB blocks %d\n",
		 f->bb?f->bb->len:-1,
/*		f->pps?f->pps->len:-1, */
/* FIXME tenix, here is not f->num_pps? */
		 f->sb?f->sb->len:-1);
	g_print ("-------------------------------------------------------------\n");
}

static void
characterise_block (MsOle *f, BLP blk, char **ans)
{
	int nblk;

	nblk = g_array_index (f->bb, BLP, blk);
	if (nblk == UNUSED_BLOCK) {
		*ans = "unused";
		return;
	} else if (nblk == SPECIAL_BLOCK) {
		*ans = "special";
		return;
	} else if (nblk == ADD_BBD_LIST_BLOCK) {
		*ans = "additional special";
		return;
	} else if (nblk == END_OF_CHAIN) {
		*ans = "end of chain";
		return;
	}

	*ans = "unknown";
	g_return_if_fail (f);
	g_return_if_fail (f->bb);
	g_return_if_fail (f->pps);

/*	for (lp = 0;lp<f->pps->len;lp++) {
		PPS *p = g_ptr_array_index (f->pps, lp);
		BLP cur = p->start;
		while (cur != END_OF_CHAIN) {
			if (cur == SPECIAL_BLOCK ||
			    cur == UNUSED_BLOCK) {
				*ans = "serious block error";
				return;
			}
			if (cur == blk) {
				*ans = p->name;
				return;
			}
			cur = NEXT_BB (f, cur);
		}
		}*/
}

static void
dump_tree (GList *list, int indent)
{
	PPS *p;
	int lp;
	char indentstr[64];
	g_return_if_fail (indent<60);

	for (lp = 0;lp<indent;lp++)
		indentstr[lp]= '-';
	indentstr[lp] = 0;

	while (list) {
		p = list->data;
		if (p) {
			g_print ("%s '%s' - %d\n",
				 indentstr, p->name, p->size);
			if (p->children)
				dump_tree (p->children, indent+1);
		} else
			g_print ("%s NULL!\n", indentstr);
		list = g_list_next (list);
	}
}

static void
dump_allocation (MsOle *f)
{
	int lp;
	char *blktype;

	for (lp = 0;lp<(int)f->bb->len;lp++) {
		characterise_block (f, lp, &blktype);
		g_print ("Block %d -> block %d ( '%s' )\n", lp,
			 g_array_index (f->bb, BLP, lp),
			 blktype);
	}

	if (f->pps) {
		g_print ("Root blocks : %d\n", f->num_pps);
		dump_tree (f->pps, 0);
	} else
		g_print ("No root yet\n");
/*
	printf ("sbd blocks : %d\n", h->sbd_list->len);
	for (lp = 0;lp<h->sbd_list->len;lp++)
	printf ("sbd_list[%d] = %d\n", lp, (int)ms_array_index (h->sbd_list, SBPtr, lp));*/
	g_print ("-------------------------------------------------------------\n");
}

/*
 * Dump some useful facts.
 * magic: 2       : dump tree
 *        default : dump header and allocation
 */
void
ms_ole_debug (MsOle *fs, int magic)
{
	switch (magic) {
	case 0:
		dump_allocation (fs);
	case 1:
		dump_header (fs);
	case 2:
		if (fs->pps)
			dump_tree (fs->pps, 0);
		else
			g_print ("There are no tree (no pps)\n");
		break;
	default:
		break;
	}
}

/*
 * get_next_block:
 * @f:   the file handle
 * @blk: an index into the big block fat
 *
 * Return value: the block index of the BBD block.
 */
static BLP
get_next_block (MsOle *f, BLP blk, gboolean *err)
{
	BLP bbd = GET_BBD_LIST (f, blk / (BB_BLOCK_SIZE / 4));

	if (bbd > BLOCK_COUNT (f)) {
		*err = TRUE;
		return 0;
	} else
		*err = FALSE;

	return MS_OLE_GET_GUINT32 (BB_R_PTR (f, bbd) +
				   4 * (blk % (BB_BLOCK_SIZE / 4)));
}

/* Builds the FAT */
static int
read_bb (MsOle *f)
{
	/* FIXME tenix may be later we wish to split this function */
	guint32  numbbd;
	BLP      lp;
	guint32  num_add_bbd_lists;
	BLP      missing_lps;
	BLP      missing_bbds;
	guint32  visited_add_bbd_list;
	BLP tmp;
	BLP bbd;

	g_return_val_if_fail (f, 0);
	g_return_val_if_fail (f->mem, 0);

	f->bb   = g_array_new (FALSE, FALSE, sizeof(BLP));
	numbbd  = GET_NUM_BBD_BLOCKS  (f);

        /* Sanity checks */
/* FIXME tenix reading big files
	if (numbbd < ((f->length - BB_BLOCK_SIZE
		      + ((BB_BLOCK_SIZE*BB_BLOCK_SIZE)/4) - 1)
			 / ((BB_BLOCK_SIZE*BB_BLOCK_SIZE)/4))) {
		printf ("Duff block descriptors\n");
		return 0;
	}
 */
	/* FIXME tenix check if size is small, there's no add bbd lists */

	/* Add BBD's that live in the BBD list */
	for (lp = 0; (lp < BLOCK_COUNT (f) - 1) &&
		     (lp < MAX_SIZE_BBD_LIST * BB_BLOCK_SIZE / 4); lp++) {
		gboolean err;

		tmp = get_next_block (f, lp, &err);
		if (err)
			return 0;

		g_array_append_val (f->bb, tmp);
	}

	/* Add BBD's that live in the additional BBD lists */
	num_add_bbd_lists = GET_NUM_ADD_BBD_LISTS (f);
	if (num_add_bbd_lists > 0) {
		if (lp != MAX_SIZE_BBD_LIST * BB_BLOCK_SIZE / 4)
			return 0;

		visited_add_bbd_list = GET_FIRST_ADD_BBD_LIST (f);
		missing_lps = BLOCK_COUNT (f) - 1
			       - MAX_SIZE_BBD_LIST*BB_BLOCK_SIZE/4;
		for (lp = 0; lp < missing_lps; lp++) {
			if ((lp != 0) && !(lp%(MAX_SIZE_ADD_BBD_LIST*
					     (BB_BLOCK_SIZE/4)))) {
				/* This lp lives in the next add bbd list */
				visited_add_bbd_list = MS_OLE_GET_GUINT32 (
						BB_R_PTR(f,visited_add_bbd_list)
						+4*MAX_SIZE_ADD_BBD_LIST);
				if (visited_add_bbd_list == END_OF_CHAIN) {
					if (lp + 1 != missing_lps) {
						/* FIXME tenix error */
					}
				}
			}

			/* tmp here means the number of one block that
			   belongs to the fat */
			bbd = MS_OLE_GET_GUINT32 (BB_R_PTR (f, visited_add_bbd_list) + 4*((lp/(BB_BLOCK_SIZE/4))%MAX_SIZE_ADD_BBD_LIST));
			tmp = MS_OLE_GET_GUINT32 (BB_R_PTR(f,bbd) +
						  4 * (lp % (BB_BLOCK_SIZE / 4)));
			g_array_append_val (f->bb, tmp);
		}
		/* FIXME tenix do we check if we have visited all lp's but
		   there are more additional lists? */
	}

	/* Mark the bbd list blocks as unused */
	for (lp = 0; lp < MIN (numbbd, MAX_SIZE_BBD_LIST); lp++) {
		TRY_MARK_UNUSED_BLOCK (f->bb, GET_BBD_LIST(f,lp),
				       SPECIAL_BLOCK);
	}
	if (num_add_bbd_lists > 0) {
		visited_add_bbd_list = GET_FIRST_ADD_BBD_LIST (f);
		TRY_MARK_UNUSED_BLOCK (f->bb, visited_add_bbd_list,
				       ADD_BBD_LIST_BLOCK);
		missing_bbds = numbbd - MAX_SIZE_BBD_LIST;
		for (lp = 0; lp < missing_bbds; lp++) {
			if ((lp != 0) && !(lp % (MAX_SIZE_ADD_BBD_LIST))) {
				/* This lp lives in the next add bbd list */
				visited_add_bbd_list = MS_OLE_GET_GUINT32 (
						BB_R_PTR(f,visited_add_bbd_list)
						+ 4*MAX_SIZE_ADD_BBD_LIST);
				if (visited_add_bbd_list == END_OF_CHAIN) {
					if (lp + 1 != missing_lps) {
						/* FIXME tenix error */
					}
				}
				TRY_MARK_UNUSED_BLOCK (f->bb,
						       visited_add_bbd_list,
						       ADD_BBD_LIST_BLOCK);
			}

			bbd = MS_OLE_GET_GUINT32 (BB_R_PTR(f, visited_add_bbd_list) + 4*(lp%MAX_SIZE_ADD_BBD_LIST));
			TRY_MARK_UNUSED_BLOCK (f->bb, bbd, SPECIAL_BLOCK);
		}
	}

	g_assert (f->bb->len < BLOCK_COUNT (f));
	/* FIXME tenix better check?:
	   g_assert (f->bb->len == f->length/BB_BLOCK_SIZE - 1); */

	/* More sanity checks */
/* FIXME
	for (lp = 0; lp<numbbd; lp++) {
		BLP bbdblk = GET_BBD_LIST(f, lp);
		if (g_array_index(f->bb, BLP, bbdblk) != SPECIAL_BLOCK) {
			printf ("Error - BBD blocks not marked correctly\n");
			g_array_free (f->bb, TRUE);
			return 0;
		}
		}
*/

#if OLE_DEBUG > 1
	dump_header (f);
#endif
	return 1;
}


static void
remap_file (MsOle *f, guint blocks)
{
	MsOleHandleType file;
	guint8 *newptr, zero = 0;
	guint32 filesize;
	guint32 oldlen;
	guint32 icount;
	gchar zeroblock [BB_BLOCK_SIZE];

	memset (zeroblock, zero, BB_BLOCK_SIZE);
	g_assert (f);
	file = f->file_des;

	g_assert (f->syswrap->munmap (f->mem, f->length, f->syswrap->closure) != -1);

	/* Extend that file by blocks */
	if (f->syswrap->getfilesize (file, &filesize, f->syswrap->closure)) {
		g_warning ("Serious error extending file\n");
		f->mem = 0;
		return;
	}

	if (f->syswrap->lseek (file, 0, SEEK_END, f->syswrap->closure) == (off_t)-1) {
		g_warning ("Serious error extending file\n");
		f->mem = 0;
		return;
	}

	for (icount = 0; icount < blocks; icount++) {
		if (f->syswrap->write (file, zeroblock, BB_BLOCK_SIZE -
				       ((icount == blocks - 1) ? 1 : 0),
				       f->syswrap->closure)
		    == -1) {
			g_warning ("Serious error extending file\n");
			f->mem = 0;
			return;
		}
	}
	if (f->syswrap->write (file, &zero, 1, f->syswrap->closure) == -1) {
		g_warning ("Serious error extending file\n");
		f->mem = 0;
		return;
	}

	oldlen = filesize;

	if (f->syswrap->getfilesize (file, &(f->length), f->syswrap->closure))
		g_warning ("couldn't get the size of the file\n");

	g_assert (f->length == BB_BLOCK_SIZE*blocks + oldlen);

	if (f->length % BB_BLOCK_SIZE)
		g_warning ("file %d non-integer number of blocks\n", f->length);

	newptr = f->syswrap->mmap (f->mem, f->length, PROT_READ|PROT_WRITE,
				   MAP_SHARED, file, 0, f->syswrap->closure);
#if OLE_DEBUG > 0
	if (newptr != f->mem)
		g_print ("Memory map moved from %p to %p\n",
			 f->mem, newptr);

#endif /* OLE_DEBUG */

	if (newptr == MAP_FAILED) {
		g_warning ("panic: re-map failed!");
		f->mem = NULL;
	} else
		f->mem = newptr;
}

static void
extend_file (MsOle *f, guint blocks)
{
	if (f->ole_mmap && f->syswrap->mmap &&
	    f->syswrap->munmap) {
		remap_file (f, blocks);

	} else {
		BBBlkAttr *s;
		guint32 blkidx, i;

		if (f->bbattr->len) {
			s = g_ptr_array_index (f->bbattr, f->bbattr->len-1);
			blkidx = s->blk+1;
		} else
			blkidx = 0;

		for (i = 0; i < blocks; i++) {
			g_ptr_array_add (f->bbattr, bb_blk_attr_new (blkidx++));
			f->length += BB_BLOCK_SIZE;
		}
	}
}

static BLP
next_free_bb (MsOle *f)
{
	BLP blk, tblk;

	g_assert (f);

	blk = 0;
	g_assert (f->bb->len < BLOCK_COUNT (f));
	while (blk < f->bb->len)
		if (g_array_index (f->bb, BLP, blk) == UNUSED_BLOCK)
			return blk;
	        else
			blk++;

	extend_file (f, 1);
	tblk = UNUSED_BLOCK;
	g_array_append_val (f->bb, tblk);
	g_assert ((g_array_index (f->bb, BLP, blk) == UNUSED_BLOCK));
	g_assert (f->bb->len < BLOCK_COUNT (f));
	return blk;
}

static int
write_bb (MsOle *f)
{
	guint32 numbbd;
	BLP     lp, lpblk;
	int a = BB_BLOCK_SIZE / 4;

	g_return_val_if_fail (f, 0);
	g_return_val_if_fail (f->mem, 0);
	g_return_val_if_fail (f->bb,  0);

	numbbd = (f->bb->len + a - 2) / (a - 1); /* Think really hard! */
	SET_NUM_BBD_BLOCKS (f, numbbd);

	for (lp = 0;lp<numbbd;lp++) {
		BLP blk = next_free_bb(f);
		SET_BBD_LIST (f, lp, blk);
		g_array_index (f->bb, BLP, blk) = SPECIAL_BLOCK;
	}

	lpblk = 0;
	while (lpblk < f->bb->len) { /* Described blocks */
		guint8 *mem = BB_W_PTR(f, GET_BBD_LIST(f, lpblk/(BB_BLOCK_SIZE/4)));
		MS_OLE_SET_GUINT32 (mem + (lpblk%(BB_BLOCK_SIZE/4))*4,
			     g_array_index (f->bb, BLP, lpblk));
		lpblk++;
	}
	while (lpblk % (BB_BLOCK_SIZE/4) != 0) { /* Undescribed blocks */
		guint8 *mem;
		g_assert (lpblk/(BB_BLOCK_SIZE/4) < numbbd);
		mem = BB_W_PTR(f, GET_BBD_LIST(f, lpblk/(BB_BLOCK_SIZE/4)));
		MS_OLE_SET_GUINT32 (mem + (lpblk%(BB_BLOCK_SIZE/4))*4,
			     UNUSED_BLOCK);
		lpblk++;
	}
	g_array_free (f->bb, TRUE);
	f->bb = 0;
	return 1;
}

static BLP
next_free_sb (MsOle *f)
{
	BLP blk, tblk;

	g_assert (f);

	blk = 0;
	while (blk < f->sb->len)
		if (g_array_index (f->sb, BLP, blk) == UNUSED_BLOCK)
			return blk;
	        else
			blk++;

	tblk = UNUSED_BLOCK;
	g_array_append_val (f->sb, tblk);
	g_assert ((g_array_index (f->sb, BLP, blk) == UNUSED_BLOCK));
	g_assert (blk < f->sb->len);

	if ((f->sb->len + (BB_BLOCK_SIZE/SB_BLOCK_SIZE) - 1) /
	    (BB_BLOCK_SIZE/SB_BLOCK_SIZE) >= f->sbf->len) {
	/* Create an extra big block on the small block stream */
		BLP new_sbf = next_free_bb(f);
		if (f->sbf->len > 0)
			g_array_index (f->bb, BLP,
				       g_array_index (f->sbf, BLP, f->sbf->len-1)) = new_sbf;
		g_array_append_val (f->sbf, new_sbf);
		g_array_index (f->bb, BLP, new_sbf) = END_OF_CHAIN;
	}

	g_assert ((f->sb->len + (BB_BLOCK_SIZE/SB_BLOCK_SIZE) - 1) /
		  (BB_BLOCK_SIZE/SB_BLOCK_SIZE) <= f->sbf->len);

	return blk;
}

static guint8 *
get_pps_ptr (MsOle *f, PPS_IDX i, gboolean forwrite)
{
	int lp;
	BLP blk = GET_ROOT_STARTBLOCK (f);

	lp = i/(BB_BLOCK_SIZE/PPS_BLOCK_SIZE);
	while (lp && blk != END_OF_CHAIN) {
		if (blk == SPECIAL_BLOCK ||
		    blk == UNUSED_BLOCK) {
			g_warning ("Duff block in root chain\n");
			return 0;
		}
		lp--;
		blk = NEXT_BB (f, blk);
	}
	if (blk == END_OF_CHAIN) {
		g_warning ("Serious error finding pps %d\n", i);
		return 0;
	}

#if OLE_DEBUG > 0
	g_print ("get_pps_ptr: blk = %d\n", blk);
#endif

	if ( blk >=  BLOCK_COUNT(f)) {
		g_warning ("Serious error finding pps %d\n", i);
		return 0;
	}

	if (forwrite)
		return BB_W_PTR(f, blk) + (i%(BB_BLOCK_SIZE/PPS_BLOCK_SIZE))*PPS_BLOCK_SIZE;
	else
		return BB_R_PTR(f, blk) + (i%(BB_BLOCK_SIZE/PPS_BLOCK_SIZE))*PPS_BLOCK_SIZE;
}

static gint
pps_compare_func (PPS *a, PPS *b)
{
	g_return_val_if_fail (a, 0);
	g_return_val_if_fail (b, 0);
	g_return_val_if_fail (a->name, 0);
	g_return_val_if_fail (b->name, 0);

	return g_strcasecmp (b->name, a->name);
}

static void
pps_decode_tree (MsOle *f, PPS_IDX p, PPS *parent)
{
	PPS    *pps;
	guint8 *mem;

	if (p == PPS_END_OF_CHAIN)
		return;

	pps           = g_new (PPS, 1);
	pps->sig      = PPS_SIG;
	mem           = get_pps_ptr (f, p, FALSE);
	if (!mem) {
		g_warning ("Serious directory error %d\n", p);
		f->pps = NULL;
		return;
	}
#if OLE_DEBUG > 0
	g_print ("pps_decode_tree: mem (offset)= %#8.8x\n", mem - f->mem);
#endif
	pps->name     = pps_get_text  (mem, PPS_GET_NAME_LEN(mem));
	pps->type     = PPS_GET_TYPE  (mem);
	pps->size     = PPS_GET_SIZE  (mem);
	pps->children = NULL;
	pps->parent   = parent;
	pps->idx      = 0;
	if (!pps->name) { /* Make safe */
		g_print ("how odd: blank named file in directory\n");
		g_free (pps);
		return;
	}

	f->num_pps++;

	if (parent) {
#if OLE_DEBUG > 0
		g_print ("Inserting '%s' into '%s'\n", pps->name, parent->name);
#endif
		parent->children = g_list_insert_sorted (parent->children, pps,
							 (GCompareFunc)pps_compare_func);
	}
	else {
#if OLE_DEBUG > 0
		g_print ("Setting root to '%s'\n", pps->name);
#endif
		f->pps = g_list_append (0, pps);
	}

	if (PPS_GET_NEXT(mem) && PPS_GET_NEXT(mem) != PPS_END_OF_CHAIN)
		pps_decode_tree (f, PPS_GET_NEXT(mem), parent);
	
        /* the underlying caching might have reused the memory      */
        /* when not using mmap                                      */
        /* therefore we have to reload the right sector here again  */
        mem           = get_pps_ptr (f, p, FALSE);

	if (PPS_GET_PREV(mem) != PPS_END_OF_CHAIN)
		pps_decode_tree (f, PPS_GET_PREV(mem), parent);

        /* same as above */
        mem           = get_pps_ptr (f, p, FALSE);
	
	if (PPS_GET_DIR (mem) != PPS_END_OF_CHAIN)
		if (pps->type == MsOleStorageT ||
		    pps->type == MsOleRootT)
			pps_decode_tree (f, PPS_GET_DIR(mem), pps);

        /* same as above */
        mem           = get_pps_ptr (f, p, FALSE);

	pps->start   = PPS_GET_STARTBLOCK (mem);

#if OLE_DEBUG > 1
        g_print ("PPS decode : '%s'\n", pps->name?pps->name:"Null");
	ms_ole_dump (mem, PPS_BLOCK_SIZE);
#endif
	return;
}

static int
read_pps (MsOle *f)
{
	PPS *pps;
	g_return_val_if_fail (f, 0);

	f->num_pps = 0;
	pps_decode_tree (f, PPS_ROOT_INDEX, NULL);

	if (!f->pps || g_list_length (f->pps) < 1 ||
	    g_list_length (f->pps) > 1) {
		g_warning ("Invalid root chain\n");
		return 0;
	} else if (!f->pps->data) {
		g_warning ("No root entry\n");
		return 0;
	}

	/* Fiddle root, perhaps our get_text is broken */
	/* perhaps it is just an MS oddity in coding */
	pps = f->pps->data;
	if (pps->name)
		g_free (pps->name);
	pps->name = g_strdup ("Root Entry");

	{ /* Free up the root chain */
		BLP blk, last;
		last = blk = GET_ROOT_STARTBLOCK (f);
		while (blk != END_OF_CHAIN) {
			last = blk;
			blk = NEXT_BB (f, blk);
			g_array_index (f->bb, BLP, last) = UNUSED_BLOCK;

			if (blk >= f->bb->len && blk != END_OF_CHAIN)
				{						
					/* Added boundary condition to the block chain traversal */
					g_warning ("Corrupt block file: invalid block in chain\n");
					break;
				}
		}
	}

	if (!f->pps) {
		g_warning ("Root directory too small\n");
		return 0;
	}
	return 1;
}

/**
 * Write the blocks main data recursively.
 **/
static void
pps_encode_tree_initial (MsOle *f, GList *list, PPS_IDX *p)
{
	int lp, max;
	guint8 *mem;
	PPS    *pps;

	g_return_if_fail (list);
	g_return_if_fail (list->data);

	pps = list->data;
	pps->idx = *p;
	(*p)++;

#if OLE_DEBUG > 0
	g_print ("encoding '%s' as %d\n", pps->name, pps->idx);
#endif

	mem = get_pps_ptr (f, pps->idx, TRUE);

	/* Blank stuff I don't understand */
	for (lp = 0; lp < PPS_BLOCK_SIZE; lp++)
		MS_OLE_SET_GUINT8 (mem + lp, 0);

	if (pps->name) {
		max = strlen (pps->name);
		if (max >= (PPS_BLOCK_SIZE / 4))
			max = (PPS_BLOCK_SIZE / 4);

		for (lp = 0; lp < max; lp++)
			MS_OLE_SET_GUINT16 (mem + lp * 2, pps->name [lp]);
	} else {
	        g_print ("No name %d\n", *p);
		max = -1;
	}
	PPS_SET_NAME_LEN (mem, (max + 1)*2);

	/* Magic numbers */
	if (pps->idx == PPS_ROOT_INDEX) { /* Only Root */
		MS_OLE_SET_GUINT32  (mem + 0x50, 0x00020900);
		MS_OLE_SET_GUINT32  (mem + 0x58, 0x000000c0);
		MS_OLE_SET_GUINT32  (mem + 0x5c, 0x46000000);
		MS_OLE_SET_GUINT8   (mem + 0x43, 0x01); /* or zero ? */
	} else if (pps->size >= BB_THRESHOLD) {
		MS_OLE_SET_GUINT32  (mem + 0x50, 0x00020900);
		MS_OLE_SET_GUINT8   (mem + 0x43, 0x01);
	} else {
		MS_OLE_SET_GUINT32  (mem + 0x64, 0x09299c3c);
		MS_OLE_SET_GUINT32  (mem + 0x6c, 0x09299c3c);
		MS_OLE_SET_GUINT8   (mem + 0x43, 0x00);
	}

	PPS_SET_TYPE (mem, pps->type);
	PPS_SET_SIZE (mem, pps->size);
        PPS_SET_STARTBLOCK(mem, pps->start);
	PPS_SET_NEXT (mem, PPS_END_OF_CHAIN);
	PPS_SET_PREV (mem, PPS_END_OF_CHAIN);
	PPS_SET_DIR  (mem, PPS_END_OF_CHAIN);

#if OLE_DEBUG > 1
	g_print ("Encode '%s' as \n", pps->name);
	ms_ole_dump (mem, PPS_BLOCK_SIZE);
#endif

	if (pps->children)
		pps_encode_tree_initial (f, pps->children, p);
	if (g_list_next (list))
		pps_encode_tree_initial (f, g_list_next(list), p);
}


/*
 * Chain the blocks together afterwards
 */
static void
pps_encode_tree_chain (MsOle *f, GList *list)
{
	PPS	*parent;		/* parent's PPS */
	int	len;			/* how many childrens are there */
	GList	*lchildren;		/* visited children */
        PPS	*children;		/* visited children's PPS */
	PPS     *next;			/* next children's PPS */
	PPS	*prev;			/* previous children's PPS */
	guint8	*mem;			/* a PPS in memory */
	guint8	*mem_parent;		/* a PPS in memory */
	gint	i;
	int	half_way;

	g_return_if_fail (list);
	g_return_if_fail (list->data);

	parent = list->data;
	len = g_list_length (parent->children);
	half_way = len / 2;
	lchildren = parent->children;

	/* The base node of the directory */

	/* Choose the first child */
	mem_parent = get_pps_ptr (f, parent->idx, TRUE);

	if (len == 1) {
		PPS_SET_DIR (mem_parent, ((PPS *)(lchildren->data))->idx);

#if OLE_DEBUG > 1
		g_print ("Final encode '%s' as \n",
			((PPS *)(parent))->name);
		ms_ole_dump (mem_parent, PPS_BLOCK_SIZE);
		g_print ("Final encode '%s' as \n",
			((PPS *)(lchildren->data))->name);
		ms_ole_dump (get_pps_ptr (f, ((PPS *)(lchildren->data))->idx, FALSE),
		      PPS_BLOCK_SIZE);
#endif

		return;
	}

#if OLE_DEBUG > 1
	if (len == 0)
		g_print ("Empty directory '%s'\n", ((PPS *)(children))->name);
#endif

	i = 0;
	for (; lchildren; lchildren = g_list_next (lchildren)) {
		children = lchildren->data;

		if (children->type == MsOleStorageT)
			pps_encode_tree_chain (f, lchildren);

		if (i == half_way)
			PPS_SET_DIR (mem_parent, ((PPS *)(children))->idx);

		mem = get_pps_ptr (f, children->idx, TRUE);
		if (i == half_way) {
			if (g_list_previous (lchildren)) {
				prev = g_list_previous(lchildren)->data;
				PPS_SET_PREV (mem, prev->idx);
			}

			if (g_list_next (lchildren)) {
				next = g_list_next (lchildren)->data;
				PPS_SET_NEXT (mem, next->idx);
			}
		} else if (i < half_way) {
			if (g_list_previous(lchildren)) {
				prev = g_list_previous (lchildren)->data;
				PPS_SET_PREV (mem, prev->idx);
			}
		} else /* i > half_way */ {
			if (g_list_next(lchildren)) {
				next = g_list_next (lchildren)->data;
				PPS_SET_NEXT (mem, next->idx);
			}
		}

#if OLE_DEBUG > 1
			g_print ("Final encode '%s' as \n",
				((PPS *)(children))->name);
			ms_ole_dump (mem, PPS_BLOCK_SIZE);
#endif

		i++;
	}

#if OLE_DEBUG > 1
	g_print ("Final encode '%s' as \n", ((PPS *)(parent))->name);
	ms_ole_dump (mem_parent, PPS_BLOCK_SIZE);
#endif
}


static void
destroy_pps (GList *l)
{
	GList *tmp;

	if (!l)
		return;

	for (tmp = l; tmp; tmp = g_list_next (tmp)) {
		PPS *pps = tmp->data;
		if (pps->name) {
			g_free (pps->name);
			pps->name = NULL;
		}
		destroy_pps (pps->children);
		pps->children = NULL;
		g_free (pps);
		pps = NULL;
	}
	g_list_free (l);
}

static int
write_pps (MsOle *f)
{
	int lp;
	PPS_IDX idx;
	BLP blk  = END_OF_CHAIN;
	BLP last = END_OF_CHAIN;

	/* Build the root chain */
	for (lp = 0;lp<(int)(f->num_pps+(BB_BLOCK_SIZE/PPS_BLOCK_SIZE)-1)/(BB_BLOCK_SIZE/PPS_BLOCK_SIZE);lp++) {
		last  = blk;
		blk   = next_free_bb (f);
		g_assert (g_array_index (f->bb, BLP, blk) == UNUSED_BLOCK);
		if (last != END_OF_CHAIN)
			g_array_index (f->bb, BLP, last) = blk;
		else {
#if OLE_DEBUG > 0
			g_print ("Set root block to %d\n", blk);
#endif
			SET_ROOT_STARTBLOCK (f, blk);
		}
		g_array_index (f->bb, BLP, blk) = END_OF_CHAIN;
	}

	g_assert (GET_ROOT_STARTBLOCK(f) != END_OF_CHAIN);

	idx    = PPS_ROOT_INDEX;
	pps_encode_tree_initial (f, f->pps, &idx);
	pps_encode_tree_chain   (f, f->pps);

	destroy_pps (f->pps);
	f->pps = NULL;
	f->num_pps = 0;
	return 1;
}

static int
read_sb (MsOle *f)
{
	BLP ptr;
	int lastidx, idx;
	PPS *root;

	g_return_val_if_fail (f, 0);
	g_return_val_if_fail (f->pps, 0);

	root = f->pps->data;
	g_return_val_if_fail (root, 0);

	f->sbf = g_array_new (FALSE, FALSE, sizeof(BLP));
	f->sb  = g_array_new (FALSE, FALSE, sizeof(BLP));

	/* List of big blocks in SB file */
	ptr = root->start;
#if OLE_DEBUG > 0
	g_print ("Starting Small block file at %d\n", root->start);
#endif
	while (ptr != END_OF_CHAIN) {
		if (ptr == UNUSED_BLOCK ||
		    ptr == SPECIAL_BLOCK ||
		    ptr >= f->bb->len) {
			g_warning ("Corrupt small block file: serious error, "
				   "invalid block in chain\n");
			g_array_free (f->sbf, TRUE);
			f->sbf = 0;
			return 0;
		}
		g_array_append_val (f->sbf, ptr);
		ptr = NEXT_BB (f, ptr);
	}

	/* Description of small blocks */
	lastidx = -1;
	idx     = 0;
	ptr = GET_SBD_STARTBLOCK (f);

	if (f->sbf->len == 0 && ptr != END_OF_CHAIN) {
		g_warning ("No small block file, but small block depot start block exists!: "
			"ignore depot, since there's no small block files after all.\n");
		ptr = END_OF_CHAIN;
	}

	while (ptr != END_OF_CHAIN) {
		guint32 lp;
		if (ptr == UNUSED_BLOCK ||
		    ptr == SPECIAL_BLOCK) {
			g_warning ("Corrupt file descriptor: serious error, "
				   "invalid block in chain\n");
			g_array_free (f->sb, TRUE);
			f->sb = 0;
			return 0;
		}
		for (lp = 0;lp<BB_BLOCK_SIZE/4;lp++) {
			BLP p = MS_OLE_GET_GUINT32 (BB_R_PTR(f, ptr) + lp*4);
			g_array_append_val (f->sb, p);

			if (p != UNUSED_BLOCK)
				lastidx = idx;
			idx++;
		}
		ptr = NEXT_BB (f, ptr);
	}
	if (lastidx>0)
		g_array_set_size (f->sb, lastidx+1);

	if (f->sbf->len * BB_BLOCK_SIZE < f->sb->len*SB_BLOCK_SIZE) {
		g_warning ("Not enough small block file for descriptors\n"
			   "sbf->len == %d, sb->len == %d\n", f->sbf->len,
			f->sb->len);
		/* some files have broken SBAT but all
                   streams in BAT. To process them we have to 
                   return success here */
		return 1;
	}

	return 1;
}

static int
write_sb (MsOle *f)
{
	guint32 lp, lastused;
	PPS *root;
	BLP sbd_start  = END_OF_CHAIN;
	BLP sbf_start  = END_OF_CHAIN;

	g_return_val_if_fail (f, 0);
	g_return_val_if_fail (f->pps, 0);

	root = f->pps->data;

	if (f->sbf->len * BB_BLOCK_SIZE < f->sb->len*SB_BLOCK_SIZE) {
		g_warning ("Not enough descriptor / blocks being written %d %d\n",
			   f->sbf->len, f->sb->len);
	}
	if (f->sbf->len>0)
		sbf_start = g_array_index (f->sbf, BLP, 0);

	lastused = END_OF_CHAIN;
	for (lp = 0;lp<f->sb->len;lp++) {
		if (g_array_index (f->sb, BLP, lp) != UNUSED_BLOCK)
			lastused = lp;
	}

	if (lastused != END_OF_CHAIN) { /* Bother writing stuff */
		guint8 *mem = 0;
		guint32 num_sbdf = (lastused + (BB_BLOCK_SIZE/4)-1) /
			(BB_BLOCK_SIZE/4);
		BLP blk = END_OF_CHAIN, last;

#if OLE_DEBUG > 0
		g_print ("Num SB descriptor blocks : %d\n", num_sbdf);
#endif

		for (lp = 0;lp<num_sbdf*(BB_BLOCK_SIZE/4);lp++) {
			BLP set;
			if (lp%(BB_BLOCK_SIZE/4) == 0) {
				last = blk;
				blk = next_free_bb(f);
				if (!lp)
					sbd_start = blk;
				if (last != END_OF_CHAIN)
					g_array_index (f->bb, BLP, last) = blk;
				g_array_index (f->bb, BLP, blk) = END_OF_CHAIN;
				mem = BB_W_PTR (f, blk);
			}
			if (lp<f->sb->len)
				set = g_array_index (f->sb, BLP, lp);
			else
				set = UNUSED_BLOCK;
			MS_OLE_SET_GUINT32 (mem + (lp%(BB_BLOCK_SIZE/4))*4, set);
		}
	} else {
#if OLE_DEBUG > 0
		g_print ("Blank SB allocation\n");
#endif
		sbf_start = END_OF_CHAIN;
	}

	root->start = sbf_start;
	SET_SBD_STARTBLOCK (f, sbd_start);
	g_array_free (f->sb,  TRUE);
	g_array_free (f->sbf, TRUE);
	f->sb       = 0;
	f->sbf      = 0;
	return 1;
}

static int
ms_ole_setup (MsOle *f)
{
	if (!f->ole_mmap) {
		guint32 i;
		f->bbattr = g_ptr_array_new ();
		for (i = 0; i < BLOCK_COUNT (f); i++)
			g_ptr_array_add (f->bbattr, bb_blk_attr_new (i));
	}

	if (read_bb  (f) &&
	    read_pps (f) &&
	    read_sb  (f)) {
#if OLE_DEBUG > 1
		g_print ("Just read header of\n");
		dump_header (f);
#endif
		return 1;
	}
	return 0;
}

static int
ms_ole_cleanup (MsOle *f)
{
	if (f->mode != 'w') /* Nothing to write */
		return 1;
#if OLE_DEBUG > 1
        g_print ("About to write header of: \n");
	dump_header (f);
#endif
	if (write_sb  (f) &&
	    write_pps (f) &&
	    write_bb  (f))
		return 1;
	return 0;
}

static MsOle *
ms_ole_new ()
{
	MsOle *f = g_new0 (MsOle, 1);

	f->mem    = (guint8 *)0xdeadbeef;
	f->length = 0;
	f->mode   = 'r';
	f->bb     = 0;
	f->bbattr = 0;
	f->sb     = 0;
	f->sbf    = 0;
	f->pps    = 0;
	f->dirty  = 0;

	return f;
}


/**
 * ms_ole_ref:
 * @fs: filesystem object.
 *
 * Increment by one the count of references to the filesystem.
 **/
void
ms_ole_ref (MsOle *fs)
{
	g_return_if_fail (fs != NULL);
	fs->ref_count++;
}


/**
 * ms_ole_unref:
 * @fs: filesystem object.
 *
 * Decrement by one the count of references to the filesystem.
 **/
void
ms_ole_unref (MsOle *fs)
{
	g_return_if_fail (fs != NULL);
	fs->ref_count--;
}


/**
 * ms_ole_open_vfs:
 * @fs: filesystem object.
 * @path: path to the filesystem-in-the file on the actual filesystem.
 * @try_mmap: TRUE if try to mmap(2) the filesystem-in-a-file,
 *            instead of opening.
 * @wrappers: system functions wrappers, %NULL if standard functions are used.
 *
 * Opens the filesystem-in-the-file @path and creates the filesystem object @fs.
 *
 * Return value: a #MsOleErr code.
 **/
MsOleErr
ms_ole_open_vfs (MsOle **fs, const char *name,
		 gboolean try_mmap,
		 MsOleSysWrappers *wrappers)
{
	int prot = PROT_READ | PROT_WRITE;
	MsOle *f;
	MsOleHandleType file;

	if (!fs)
		return MS_OLE_ERR_BADARG;

#if OLE_DEBUG > 0
	g_print ("New OLE file '%s'\n", name);
#endif

	f = *fs = ms_ole_new ();
	take_wrapper_functions (f, wrappers);

	f->file_des = file = f->syswrap->open2 (name, O_RDWR, f->syswrap->closure);
	f->ref_count = 0;
	f->mode = 'w';

	if (file == BAD_MSOLE_HANDLE) {
		f->file_des = file = f->syswrap->open2 (name, O_RDONLY, f->syswrap->closure);
		f->mode = 'r';
		prot &= ~PROT_WRITE;
	}

	if ((file == BAD_MSOLE_HANDLE) || !(f->syswrap->isregfile (file, f->syswrap->closure))) {
		g_warning ("No such file '%s'\n", name);
		g_free (f) ;
		*fs = NULL;
		return MS_OLE_ERR_EXIST;
	}

	if (f->syswrap->getfilesize (file, &(f->length), f->syswrap->closure)) {
		g_warning ("Couldn't get the size of file '%s'\n", name);
		f->syswrap->close (file, f->syswrap->closure) ;
		g_free (f);
		*fs = NULL;
		return MS_OLE_ERR_EXIST;
	}

	if (f->length <= 0x4c) { /* Bad show */
#if OLE_DEBUG > 0
		g_warning ("File '%s' too short\n", name);
#endif
		f->syswrap->close (file, f->syswrap->closure) ;
		g_free (f) ;
		*fs = NULL;
		return MS_OLE_ERR_FORMAT;
	}

	if (try_mmap && f->syswrap->mmap) {
		f->mem = f->syswrap->mmap (
			0, f->length, prot, MAP_SHARED, file, 0, f->syswrap->closure);
		if (!f->mem || (caddr_t)f->mem == (caddr_t)MAP_FAILED) {
			g_warning ("I can't mmap that file, falling back to slower method");
			f->mem = NULL;
		} else
			f->ole_mmap = TRUE;
	} else {
		f->mem = NULL;
		g_warning ("I won't mmap that file, using a slower method\n");
	}


	if (f->mem == NULL) {
		f->ole_mmap = FALSE;
		f->mem = g_new (guint8, BB_BLOCK_SIZE);

		if (!f->mem ||
		    (f->syswrap->read) (file, f->mem, BB_BLOCK_SIZE,
				      f->syswrap->closure) == -1) {
			g_warning ("Error reading header\n");
			f->syswrap->close (file, f->syswrap->closure) ;
			g_free (f);
			*fs = NULL;
			return MS_OLE_ERR_EXIST;
		}
	}

	if (MS_OLE_GET_GUINT32 (f->mem    ) != 0xe011cfd0 ||
	    MS_OLE_GET_GUINT32 (f->mem + 4) != 0xe11ab1a1) {
#if OLE_DEBUG > 0
		g_print ("Failed OLE2 magic number %x %x\n",
			 MS_OLE_GET_GUINT32(f->mem), MS_OLE_GET_GUINT32(f->mem+4));
#endif
		ms_ole_destroy (fs);
		return MS_OLE_ERR_FORMAT;
	}

	{
		unsigned short bbs = MS_OLE_GET_GUINT16 (f->mem + 0x1e);
		unsigned short sbs = MS_OLE_GET_GUINT16 (f->mem + 0x20);

		if ((1 << bbs) != BB_BLOCK_SIZE)
			g_warning ("Big-block-size mismatch [%d] -- expect trouble.", bbs);

		if ((1 << sbs) != SB_BLOCK_SIZE)
			g_warning ("Small-block-size mismatch [%d] -- expect trouble.", sbs);
	}

	if (f->length % BB_BLOCK_SIZE)
		g_warning ("Warning file '%s': %d bytes, non-integer number of blocks\n",
			   name, f->length);

	if (!ms_ole_setup (f)) {
		g_warning ("'%s' : duff file !\n", name);
		ms_ole_destroy (fs);
		return MS_OLE_ERR_FORMAT;
	}

	g_assert (f->bb->len < BLOCK_COUNT (f));

#if OLE_DEBUG > 0
	g_print ("New OLE file '%s'\n", name);
#endif
	/* If writing then when destroy commit it */
	return MS_OLE_ERR_OK;
}


/**
 * ms_ole_create_vfs:
 * @fs: filesystem object.
 * @path: path to the filesystem-in-the file on the actual filesystem.
 * @try_mmap: TRUE if try to mmap(2) the filesystem-in-a-file,
 *            instead of opening.
 * @wrappers: system functions wrappers, %NULL if standard functions are used.
 *
 * Creates the filesystem-in-the-file @path and creates the filesystem @fs.
 *
 * Return value: a #MsOleErr code.
 **/
MsOleErr
ms_ole_create_vfs (MsOle **fs, const char *name, gboolean try_mmap,
		   MsOleSysWrappers *wrappers)
{
	MsOle *f;
	MsOleHandleType file;
	int zero = 0;
	int init_blocks = 1, lp;

	if (!fs)
		return MS_OLE_ERR_BADARG;

	f = *fs = ms_ole_new ();
	take_wrapper_functions (f, wrappers);
	if ((file = f->syswrap->open3 (name,
				       O_RDWR|O_CREAT|O_TRUNC,
				       S_IRUSR|S_IWUSR|S_IRGRP|S_IWGRP,
				       f->syswrap->closure))
	    == BAD_MSOLE_HANDLE) {
		g_warning ("Can't create file '%s'\n", name);
		g_free (f);
		*fs = NULL;
		return MS_OLE_ERR_PERM;
	}

	if ((f->syswrap->lseek (file, BB_BLOCK_SIZE * init_blocks - 1,
		    SEEK_SET, f->syswrap->closure) == (off_t)-1) ||
	    (f->syswrap->write (file, &zero, 1, f->syswrap->closure) == -1)) {
		g_warning ("Serious error extending file to %d bytes\n",
			   BB_BLOCK_SIZE*init_blocks);
		g_free (f);
		*fs = NULL;
		return MS_OLE_ERR_SPACE;
	}

	f->ref_count = 0;
	f->file_des  = file;
	f->mode      = 'w';
	if (f->syswrap->getfilesize (file, &(f->length), f->syswrap->closure)) {
		g_warning ("Warning couldn't get the size of the file '%s'\n",
			   name);
	}
	if (f->length % BB_BLOCK_SIZE)
		g_warning ("Warning file %d non-integer number of blocks\n",
			   f->length);

	if (try_mmap) {
		if (f->syswrap->mmap)
			f->mem = f->syswrap->mmap (
				0, f->length, PROT_READ | PROT_WRITE,
				MAP_SHARED, file, 0, f->syswrap->closure);
		else
			f->mem = NULL;

		if (!f->mem || (caddr_t)f->mem == (caddr_t)MAP_FAILED) {
			g_warning ("I can't mmap that file, falling back to slower method");
			f->mem  = g_new (guint8, BB_BLOCK_SIZE);
			f->ole_mmap = FALSE;
		} else
			f->ole_mmap = TRUE;

	} else /* !try_mmap */ {
		g_warning ("I won't mmap that file, using a slower method");
		f->ole_mmap = FALSE;
		f->mem  = g_new (guint8, BB_BLOCK_SIZE);
	}

	/* The header block */
	for (lp = 0; lp < BB_BLOCK_SIZE / 4; lp++)
		MS_OLE_SET_GUINT32(f->mem + lp * 4,
				   (lp < (0x52 / 4)) ? 0: UNUSED_BLOCK);

	MS_OLE_SET_GUINT32 (f->mem,     0xe011cfd0); /* Magic number */
	MS_OLE_SET_GUINT32 (f->mem + 4, 0xe11ab1a1);

	/* More magic numbers */
	MS_OLE_SET_GUINT32 (f->mem + 0x18, 0x0003003e);
	MS_OLE_SET_GUINT32 (f->mem + 0x1c, 0x0009fffe);
	MS_OLE_SET_GUINT32 (f->mem + 0x20, 0x6);
	MS_OLE_SET_GUINT32 (f->mem + 0x38, 0x00001000);
/*	MS_OLE_SET_GUINT32 (f->mem + 0x40, 0x1);  */
	MS_OLE_SET_GUINT32 (f->mem + 0x44, 0xfffffffe);

	SET_NUM_BBD_BLOCKS  (f, 0);
	SET_ROOT_STARTBLOCK (f, END_OF_CHAIN);
	SET_SBD_STARTBLOCK  (f, END_OF_CHAIN);

	{
		PPS *p;

		f->bb  = g_array_new (FALSE, FALSE, sizeof(BLP));
		f->sb  = g_array_new (FALSE, FALSE, sizeof(BLP));
		f->sbf = g_array_new (FALSE, FALSE, sizeof(BLP));

		p           = g_new (PPS, 1);
		p->sig      = PPS_SIG;
		p->name     = g_strdup ("Root Entry");
		p->start    = END_OF_CHAIN;
		p->type     = MsOleRootT;
		p->size     = 0;
		p->children = NULL;
		p->parent   = NULL;
		f->pps      = g_list_append (0, p);
		f->num_pps  = 1;

		if (f->ole_mmap)
			f->bbattr = NULL;
		else
			f->bbattr   = g_ptr_array_new ();
	}
	g_assert (f->bb->len < BLOCK_COUNT (f));

	return MS_OLE_ERR_OK;
}


/**
 * ms_ole_destroy:
 * @fs: filesystem object.
 *
 * Closes the filesystem @fs and truncates any free blocks.
 **/
void
ms_ole_destroy (MsOle **ptr)
{
	MsOle *f = *ptr;

#if OLE_DEBUG > 0
	g_print ("FIXME: should truncate to remove unused blocks\n");
#endif
	if (f) {
		if (f->ref_count != 0)
			g_warning ("Unclosed files exist on this OLE stream\n");

		if (f->dirty)
			ms_ole_cleanup (f);

		if (f->mem == (void *)0xdeadbeef)
		    f->mem = NULL;
		else if (f->ole_mmap) {
#ifdef HAVE_MMAP
			munmap (f->mem, f->length);
#else
			g_warning ("Unmapping while we dont have mmap call");
#endif
		} else {
			if (f->bbattr) {
				guint32 i;
				for (i = 0; i < f->bbattr->len; i++) {
					BBBlkAttr *attr = g_ptr_array_index (f->bbattr, i);
					if (f->dirty && attr->dirty)
						write_cache_block (f, attr);
					g_free (attr->data);
					attr->data = NULL;
					g_free (attr);
				}
				g_ptr_array_free(f->bbattr,TRUE);
				f->bbattr = NULL;
			}

			if (f->dirty) {
				f->syswrap->lseek (f->file_des, 0, SEEK_SET,
						   f->syswrap->closure);
				f->syswrap->write (f->file_des, f->mem,
						   BB_BLOCK_SIZE,
						   f->syswrap->closure);
			}
			g_free (f->mem);
			f->mem = NULL;
		}

		destroy_pps (f->pps);
		f->pps = NULL;

		f->syswrap->close (f->file_des, f->syswrap->closure);
		g_free (f);

#if OLE_DEBUG > 0
		g_print ("Closing OLE file\n");
#endif
	}
	*ptr = NULL;
}

/**
 * ms_ole_dump:
 * @ptr: memory area to be dumped.
 * @len: how many bytes will be dumped.
 *
 * Dump @len bytes from the memory location given by @ptr.
 **/
void
ms_ole_dump (guint8 const *ptr, guint32 len)
{
	guint32 lp,lp2;
	guint32 off;

	for (lp = 0;lp<(len+15)/16;lp++)
	{
		g_print ("%8x | ", lp*16);
		for (lp2 = 0;lp2<16;lp2++) {
			off = lp2 + (lp<<4);
			off<len?g_print("%2x ", ptr[off]):g_print("XX ");
		}
		printf ("| ");
		for (lp2 = 0;lp2<16;lp2++) {
			off = lp2 + (lp<<4);
			g_print ("%c", off<len?(ptr[off]>'!'&&ptr[off]<127?ptr[off]:'.'):'*');
		}
		g_print ("\n");
	}
}


/*
 * Redundant stream check function.
 */
static void
check_stream (MsOleStream *s)
{
	BLP blk;
	guint32 idx;
	PPS *p;
	MsOle *f;

	g_return_if_fail (s);
	g_return_if_fail (s->file);

	f = s->file;
	p = s->pps;

	g_return_if_fail (p);
	blk = p->start;
	idx = 0;
	if (s->type == MsOleSmallBlock) {
		while (blk != END_OF_CHAIN) {
			g_assert (g_array_index (s->blocks, BLP, idx) ==
				  blk);
#if OLE_DEBUG > 2
			ms_ole_dump (GET_SB_R_PTR(f, blk), SB_BLOCK_SIZE);
#endif
			blk = NEXT_SB (f, blk);
			idx++;
		}
	} else {
		while (blk != END_OF_CHAIN) {
			g_assert (g_array_index (s->blocks, BLP, idx) ==
				  blk);
#if OLE_DEBUG > 2
			ms_ole_dump (BB_R_PTR(f, blk), BB_BLOCK_SIZE);
#endif
			blk = NEXT_BB (f, blk);
			idx++;
		}
	}
}


static MsOlePos
tell_pos (MsOleStream *s)
{
	return s->position;
}


/*
 * Free the allocation chains, and free up the blocks.
 * "It was for freedom that Christ has set us free."
 *   Galatians 5:11
 */
static void
free_allocation (MsOle *f, guint32 startblock, gboolean is_big_block_stream)
{
	g_return_if_fail (f);

#if OLE_DEBUG > 0
        g_print ("Free allocation %d : (%d)\n", startblock,
		 is_big_block_stream);
#endif

	if (is_big_block_stream)
	{
		BLP p = startblock;
	        g_print ("FIXME: this should also free up blocks\n");
		while (p != END_OF_CHAIN) {
			BLP next = NEXT_BB (f,p);
			if (next == p) {
				g_warning ("Serious bug: cyclic ring in BB allocation\n");
				return;
			} else if (p == SPECIAL_BLOCK ||
				   p == UNUSED_BLOCK) {
				g_warning ("Serious bug: Special / Unused block "
					   "in BB allocation\n");
				return;
			}
			g_array_index (f->bb, BLP, p) = UNUSED_BLOCK;
			p = next;
		}
	}
	else
	{
		BLP p = startblock;
		while (p != END_OF_CHAIN) {
			BLP next = NEXT_SB (f,p);
			if (next == p) {
				g_warning ("Serious bug: cyclic ring in SB allocation\n");
				return;
			} else if (p == SPECIAL_BLOCK ||
				   p == UNUSED_BLOCK) {
				g_warning ("Serious bug: Special / Unused block "
					   "in SB allocation\n");
				return;
			}
			g_array_index (f->sb, BLP, p) = UNUSED_BLOCK;
			p = next;
		}
		/* Seek forwards to find blank sbf blocks */
		{
			guint32 lp;
			BLP     lastused = END_OF_CHAIN;
			for (lp = 0;lp<f->sb->len;lp++) {
				if (g_array_index (f->sb, BLP, lp) != UNUSED_BLOCK)
					lastused = lp;
			}
			if (lastused == END_OF_CHAIN) {
				for (lp = 0;lp<f->sbf->len;lp++) {
					BLP sbfd = g_array_index (f->sbf, BLP, lp);
					g_array_index (f->bb, BLP, sbfd) = UNUSED_BLOCK;
				}
				g_array_set_size (f->sbf, 0);
				g_array_set_size (f->sb, 0);
			} else {
				guint32 sbf_needed = (lastused+(BB_BLOCK_SIZE/SB_BLOCK_SIZE)-1) /
					             (BB_BLOCK_SIZE/SB_BLOCK_SIZE);

				if (sbf_needed == f->sbf->len)
					return;

				for (lp=sbf_needed;lp<f->sbf->len;lp++) {
					BLP sbfd = g_array_index (f->sbf, BLP, lp);
					g_array_index (f->bb, BLP, sbfd) = UNUSED_BLOCK;
				}
				g_array_set_size (f->sbf, sbf_needed);
				g_array_set_size (f->sb, lastused+1);
			}
		}
	}
}


/**
 * ms_ole_lseek:
 * @s: stream object.
 * @bytes: number of bytes to set the stream pointer.
 * @type: relative from where the stream pointer will be set.
 *
 * Set the stream pointer for @s as many as @bytes bytes according to @type.
 *
 * Return value: the new position of the stream pointer.
 **/
static MsOleSPos
ms_ole_lseek (MsOleStream *s, MsOleSPos bytes, MsOleSeek type)
{
	/* FIXME tenix improve limits detection: avoid gint vs guint limits */
	MsOleSPos newpos;

	g_return_val_if_fail (s, -1);

	if (type == MsOleSeekSet)
		newpos  = bytes;
	else if (type == MsOleSeekCur)
		newpos = s->position + bytes;
	else
		newpos = s->size + bytes;

	if (newpos > (MsOleSPos) s->size || newpos < 0) {
		g_warning ("Invalid seek");
		return -1;
	}
	s->position = newpos;
	return newpos;
}


/*
 *  Returns:
 *  NULL    - on error
 */
static guint8*
ms_ole_read_ptr_bb (MsOleStream *s, MsOlePos length)
{
	int blklen;
	guint8 *ans;
	guint32 len  = length;
	int blockidx = s->position/BB_BLOCK_SIZE;

	g_return_val_if_fail (s, NULL);

	if (!s->blocks || blockidx >= (int) s->blocks->len) {
		g_warning ("Reading from NULL file\n");
		return NULL;
	}

	blklen = BB_BLOCK_SIZE - s->position % BB_BLOCK_SIZE;

	if (len > (guint32) blklen && !s->file->ole_mmap)
		return NULL;

	while (len > (guint32) blklen) {
		len -= blklen;
		blklen = BB_BLOCK_SIZE;
		if (blockidx >= (int) (s->blocks->len - 1)
		    || (ms_array_index (s->blocks, BLP, blockidx)
			!= (BLP) (blockidx + 1) ))
			return NULL;
		blockidx++;
	}
	/* Straight map, simply return a pointer */
	ans = BB_R_PTR (s->file, ms_array_index (s->blocks, BLP,
						 s->position/BB_BLOCK_SIZE))
		+ s->position % BB_BLOCK_SIZE;
	ms_ole_lseek (s, length, MsOleSeekCur);

	if (libole2_debug)
		check_stream (s);

	return ans;
}


/*
 *  Returns:
 *  NULL    - on error
 */
static guint8*
ms_ole_read_ptr_sb (MsOleStream *s, MsOlePos length)
{
	int blklen;
	guint8 *ans;
	guint32 len  = length;
	int blockidx = s->position / SB_BLOCK_SIZE;

	g_return_val_if_fail (s, NULL);

	if (!s->blocks || blockidx >= (int) s->blocks->len) {
		g_warning ("Reading from NULL file\n");
		return NULL;
	}

	blklen = SB_BLOCK_SIZE - s->position % SB_BLOCK_SIZE;

	if (len > (guint32) blklen && !s->file->ole_mmap)
		return NULL;

	while (len > (guint32) blklen) {
		len -= blklen;
		blklen = SB_BLOCK_SIZE;
		if (blockidx >= (int) (s->blocks->len - 1)
		    || (ms_array_index (s->blocks, BLP, blockidx)
			!= (BLP) (blockidx + 1) ))
			return NULL;
		blockidx++;
	}
	/* Straight map, simply return a pointer */
	ans = GET_SB_R_PTR (s->file, ms_array_index (s->blocks, BLP,
						     s->position/SB_BLOCK_SIZE))
		+ s->position % SB_BLOCK_SIZE;
	ms_ole_lseek (s, length, MsOleSeekCur);

	if (libole2_debug)
		check_stream (s);

	return ans;
}


/*
 *  Returns:
 *  zero    - on error
 *  no zero - on success
 */
static gint
ms_ole_read_copy_bb (MsOleStream *s, guint8 *ptr, MsOlePos length)
{
	guint8 *src;
	int offset = s->position % BB_BLOCK_SIZE;
	int blkidx = s->position / BB_BLOCK_SIZE;

	g_return_val_if_fail (s, 0);
	g_return_val_if_fail (ptr, 0);

	if (!s->blocks) {
		g_warning ("Reading from NULL file\n");
		return 0;
	}

	while (length > 0)
	{
		BLP block;
		int cpylen = BB_BLOCK_SIZE - offset;
		if (cpylen > (int) length)
			cpylen = length;

		if (s->position + cpylen > s->size
		    || blkidx == (int) s->blocks->len) {
#if OLE_DEBUG > 0
			g_print ("Trying 2 to read beyond end of stream %d+%d %d\n",
				s->position, cpylen, s->size);
#endif
			return 0;
		}
		g_assert (blkidx < (int) s->blocks->len);
		block = ms_array_index (s->blocks, BLP, blkidx);
		src = BB_R_PTR (s->file, block) + offset;

		memcpy (ptr, src, cpylen);
		ptr    += cpylen;
		length -= cpylen;

		offset = 0;

		blkidx++;
		s->position+=cpylen;
	}

	if (libole2_debug)
		check_stream (s);

	return 1;
}


/*
 *  Returns:
 *  zero    - on error
 *  no zero - on success
 */
static gint
ms_ole_read_copy_sb (MsOleStream *s, guint8 *ptr, MsOlePos length)
{
	int offset = s->position % SB_BLOCK_SIZE;
	int blkidx = s->position / SB_BLOCK_SIZE;
	guint8 *src;

	g_return_val_if_fail (s, 0);
	g_return_val_if_fail (ptr, 0);

	if (!s->blocks) {
		g_warning ("Reading from NULL file\n");
		return 0;
	}

	while (length > 0)
	{
		int cpylen = SB_BLOCK_SIZE - offset;
		BLP block;
		if (cpylen>(int)length)
			cpylen = length;
		if (s->position + cpylen > s->size
		    || blkidx == (int) s->blocks->len) {
#if OLE_DEBUG > 0
			g_print ("Trying 3 to read beyond end of stream %d+%d %d\n",
				s->position, cpylen, s->size);
#endif
			return 0;
		}
		g_assert (blkidx < (int) s->blocks->len);
		block = ms_array_index (s->blocks, BLP, blkidx);
		src = GET_SB_R_PTR (s->file, block) + offset;

		memcpy (ptr, src, cpylen);
		ptr += cpylen;
		length -= cpylen;

		offset = 0;

		blkidx++;
		s->position += cpylen;
	}

	if (libole2_debug)
		check_stream (s);

	return 1;
}


static void
ms_ole_append_block (MsOleStream *s)
{
	BLP block;
	BLP lastblk = END_OF_CHAIN;
	BLP eoc     = END_OF_CHAIN;

	if (s->type == MsOleSmallBlock) {
		if (!s->blocks)
			s->blocks = g_array_new (FALSE, FALSE, sizeof(BLP));

		else if (s->blocks->len>0)
			lastblk = ms_array_index (s->blocks, BLP, s->blocks->len-1);

		block = next_free_sb (s->file);
		g_array_append_val (s->blocks, block);

		if (lastblk != END_OF_CHAIN) { /* Link onwards */
			g_array_index (s->file->sb, BLP, lastblk) = block;
#if OLE_CHAIN_DEBUG > 1
			g_print ("Chained Small block %d to previous block %d\n", block, lastblk);
#endif
		} else { /* First block in a file */
			PPS *p = s->pps;
#if OLE_CHAIN_DEBUG > 0
			g_print ("Set first Small block to %d\n", block);
#endif
			p->start = block;
		}

		g_array_index (s->file->sb, BLP, block) = eoc;
	} else {
		if (!s->blocks)
			s->blocks = g_array_new (FALSE, FALSE, sizeof(BLP));
		else if (s->blocks->len>0)
			lastblk = ms_array_index (s->blocks, BLP, s->blocks->len-1);

		block = next_free_bb (s->file);
#if OLE_CHAIN_DEBUG > 0
		{
			int lp;
			g_assert (g_array_index (s->file->bb, BLP, block) == UNUSED_BLOCK);
			for (lp=0;lp<s->blocks->len;lp++)
				g_assert (g_array_index (s->blocks, BLP, lp) != block);
		}
#endif
		g_array_append_val (s->blocks, block);

		if (lastblk != END_OF_CHAIN) { /* Link onwards */
			g_array_index (s->file->bb, BLP, lastblk) = block;
#if OLE_CHAIN_DEBUG > 1
		        g_print ("Chained Big block %d to block %d\n", block, lastblk);
#endif
		} else { /* First block in a file */
			PPS *p = s->pps;
#if OLE_CHAIN_DEBUG > 0
			g_print ("Set first Big block to %d\n", block);
#endif
			p->start = block;
		}

		g_array_index (s->file->bb, BLP, block) = eoc;
	}
}


/* FIXME: I'm sure these functions should fail gracefully somehow :-) */
static MsOlePos
ms_ole_write_bb (MsOleStream *s, guint8 *ptr, MsOlePos length)
{
	guint8  *dest;
	gint32   lengthen;
	guint32  bytes   = length;
	int      offset  = s->position%BB_BLOCK_SIZE;
	guint32  blkidx  = s->position/BB_BLOCK_SIZE;

	s->file->dirty = 1;
	while (bytes > 0) {
		BLP block;
		int cpylen = BB_BLOCK_SIZE - offset;
		if (cpylen > (int) bytes)
			cpylen = bytes;

		if (!s->blocks || blkidx >= s->blocks->len)
			ms_ole_append_block (s);

		g_assert (blkidx < s->blocks->len);
		block = ms_array_index (s->blocks, BLP, blkidx);

		dest = BB_W_PTR(s->file, block) + offset;

#if OLE_DEBUG > 1
		g_print ("Copy %d bytes to block %d\n", cpylen, block);
#endif
		memcpy (dest, ptr, cpylen);
		ptr   += cpylen;
		bytes -= cpylen;

		offset = 0;
		blkidx++;
	}

	lengthen = s->position - s->size + length;
	if (lengthen > 0)
		s->size += lengthen;

	s->lseek (s, length, MsOleSeekCur);

	if (libole2_debug)
		check_stream (s);

	return length;
}


/* FIXME: I'm sure these functions should fail gracefully somehow :-) */
static MsOlePos
ms_ole_write_sb (MsOleStream *s, guint8 *ptr, MsOlePos length)
{
	guint8 *dest;
	int     offset  = s->position%SB_BLOCK_SIZE;
	guint32 blkidx  = s->position/SB_BLOCK_SIZE;
	guint32 bytes   = length;
	gint32  lengthen;

	s->file->dirty = 1;
	while (bytes > 0) {
		BLP block;
		int cpylen = SB_BLOCK_SIZE - offset;

		if (cpylen > (int) bytes)
			cpylen = bytes;

		if (!s->blocks || blkidx >= s->blocks->len)
			ms_ole_append_block (s);
		g_assert (s->blocks);

		g_assert (blkidx < s->blocks->len);
		block = ms_array_index (s->blocks, BLP, blkidx);

		dest = GET_SB_W_PTR(s->file, block) + offset;

		g_assert (cpylen >= 0);

		memcpy (dest, ptr, cpylen);
		ptr   += cpylen;
		bytes -= cpylen;

		lengthen = s->position + length - bytes - s->size;
		if (lengthen > 0)
			s->size += lengthen;

		/* Must be exactly filling the block */
		if (s->size >= BB_THRESHOLD)
		{
			PPS         *p = s->pps;
			MsOlePos oldlen;
			guint8      *buffer;

			buffer       = g_new (guint8, s->size);
			s->lseek     (s, 0, MsOleSeekSet);
			oldlen       = s->size;
			s->read_copy (s, buffer, oldlen);

			free_allocation (s->file, p->start, 0);
			p->start    = END_OF_CHAIN;
#if OLE_DEBUG > 1
			g_print ("\n\n--- Converting ---\n\n\n");
#endif
			s->read_copy = ms_ole_read_copy_bb;
			s->read_ptr  = ms_ole_read_ptr_bb;
			s->lseek     = ms_ole_lseek;
			s->tell      = tell_pos;
			s->write     = ms_ole_write_bb;

			g_assert (s->size % SB_BLOCK_SIZE == 0);

			/* Convert the file to BBlocks */
			s->size     = 0;
			s->position = 0;
			s->type  = MsOleLargeBlock;
			g_array_free (s->blocks, TRUE);
			s->blocks   = 0;

			s->write (s, buffer, oldlen);

			/* Continue the interrupted write */
			ms_ole_write_bb (s, ptr, bytes);
			bytes = 0;
#if OLE_DEBUG > 1
			g_print ("\n\n--- Done ---\n\n\n");
#endif
			g_free (buffer);
			return length;
		}

		offset = 0;
		blkidx++;

		if (libole2_debug)
			check_stream (s);
	}
	s->lseek (s, length, MsOleSeekCur);

	return length;
}


/**
 * pps_create:
 * @f: ole file handle.
 * @p: returned pps.
 * @parent: parent pps.
 * @name: its name.
 * @type: the type.
 *
 * Creates a storage or stream.
 *
 * Return value: error status.
 **/
static MsOleErr
pps_create (MsOle *f, GList **p, GList *parent, const char *name,
	    MsOleType type)
{
	PPS *pps, *par;

	if (!p || !parent || !parent->data || !name) {
		g_warning ("duff arguments to pps_create");
		return MS_OLE_ERR_BADARG;
	}

	pps  = g_new (PPS, 1);
	if (!pps)
		return MS_OLE_ERR_MEM;

	pps->sig      = PPS_SIG;
	pps->name     = g_strdup (name);
	pps->type     = type;
	pps->size     = 0;
	pps->start    = END_OF_CHAIN;
	pps->children = NULL;
	pps->parent   = parent->data;

	par = (PPS *)parent->data;
	par->children = g_list_insert_sorted (par->children, pps,
					      (GCompareFunc)pps_compare_func);
	*p = g_list_find (par->children, pps);
	f->num_pps++;

	return MS_OLE_ERR_OK;
}


/**
 * find_in_pps:
 * @l: the parent storage chain element.
 *
 * Find the right Stream ... John 4:13-14 ...
 * in a storage
 *
 * Return value: %NULL if not found or pointer to the child list
 **/
static GList *
find_in_pps (GList *l, const char *name)
{
	PPS   *pps;
	GList *cur;

	g_return_val_if_fail (l != NULL, NULL);
	g_return_val_if_fail (l->data != NULL, NULL);
	pps = l->data;
	g_return_val_if_fail (IS_PPS (pps), NULL);

	if (pps->type == MsOleStorageT ||
	    pps->type == MsOleRootT)
		cur = pps->children;
	else {
		g_warning ("trying to enter a stream '%s'",
			   pps->name?pps->name:"no name");
		return NULL;
	}

	for ( ;cur ; cur = g_list_next (cur)) {
		PPS *pps = cur->data;
		g_return_val_if_fail (IS_PPS (pps), NULL);

		if (!pps->name)
			continue;

		if (!g_strcasecmp (pps->name, name))
			return cur;
	}
	return NULL;
}


/**
 * path_to_pps:
 * @pps:  pointer to pps to return value in.
 * @f:    ole file hande.
 * @path: path to find.
 * @file: file to find in path.
 * @create_if_not_found: create the pps with the given path if not found.
 *
 * Locates a stream or storage with the given path.
 *
 * Return value: a #MsOleErr code.
 **/
static MsOleErr
path_to_pps (PPS **pps, MsOle *f, const char *path,
	     const char *file,
	     gboolean create_if_not_found)
{
	guint     lp;
	gchar   **dirs;
	GList    *cur, *parent;

	g_return_val_if_fail (f != NULL, MS_OLE_ERR_BADARG);
	g_return_val_if_fail (path != NULL, MS_OLE_ERR_BADARG);

	dirs = g_strsplit (path, "/", -1);
	g_return_val_if_fail (dirs != NULL, MS_OLE_ERR_BADARG);

	parent = cur = f->pps;

	for (lp = 0; dirs[lp]; lp++) {
		if (dirs[lp][0] == '\0' || !cur) {
			g_free (dirs[lp]);
			continue;
		}

		parent = cur;

		cur = find_in_pps (parent, dirs[lp]);

		if (!cur && create_if_not_found &&
		    pps_create (f, &cur, parent, dirs[lp], MsOleStorageT) !=
		    MS_OLE_ERR_OK)
			cur = NULL;
		/* else carry on not finding them before dropping out */

		g_free (dirs[lp]);
	}
	g_free (dirs);

	if (!cur || !cur->data)
		return MS_OLE_ERR_EXIST;

	if (file[0] == '\0') { /* We just want a directory */
		*pps = cur->data;
		g_return_val_if_fail (IS_PPS (cur->data), MS_OLE_ERR_INVALID);
		return MS_OLE_ERR_OK;
	}

	parent = cur;
	cur = find_in_pps (parent, file);

	/* now the file */
	if (!cur) {
		if (create_if_not_found) {
			MsOleErr result;
			result = pps_create (f, &cur, parent, file,
					     MsOleStreamT);
			if (result == MS_OLE_ERR_OK) {
				*pps = cur->data;
				g_return_val_if_fail (IS_PPS (cur->data),
						      MS_OLE_ERR_INVALID);
				return MS_OLE_ERR_OK;
			} else
				return result;
		}
		return MS_OLE_ERR_EXIST;
	}

	if (cur && cur->data) {
		*pps = cur->data;
		g_return_val_if_fail (IS_PPS (cur->data), MS_OLE_ERR_INVALID);
		return MS_OLE_ERR_OK;
	}

	return MS_OLE_ERR_EXIST;
}


/**
 * ms_ole_unlink:
 * @fs: filesystem object.
 * @path: path of the stream or directory to delete.
 *
 * Delete the stream or directory @path on the filesystem @fs.
 *
 * Return value: a #MsOleErr code.
 **/
MsOleErr
ms_ole_unlink (MsOle *f, const char *path)
{
	g_warning ("Unimplemented");

	/* FIXME missing implementation, or at least a better error code =-) */
	return MS_OLE_ERR_NOTEMPTY;
}


/**
 * ms_ole_directory:
 * @names: array where the names are storesd, it's %NULL ended.
 * @fs: filesystem object.
 * @dirpath: directory path.
 *
 * Gets the names of the streams and directories in the directory @dirpath.
 *
 * Returns: a #MsOleErr code.
 **/
MsOleErr
ms_ole_directory (char ***names, MsOle *f, const char *path)
{
	char    **ans;
	PPS      *pps;
	MsOleErr  result;
	GList    *l;
	int       lp;

	g_return_val_if_fail (f != NULL, MS_OLE_ERR_BADARG);
	g_return_val_if_fail (path != NULL, MS_OLE_ERR_BADARG);

	if ((result = path_to_pps (&pps, f, path, "", FALSE)) !=
	    MS_OLE_ERR_OK)
		return result;

	if (!pps)
		return MS_OLE_ERR_INVALID;

	l   = pps->children;
	ans = g_new (char *, g_list_length (l) + 1);

	lp = 0;
	for (; l; l = g_list_next (l)) {
		pps = (PPS *)l->data;

		if (!pps->name)
			continue;

		ans[lp] = g_strdup (pps->name);
		lp++;
	}
	ans[lp] = NULL;

	*names = ans;
	return MS_OLE_ERR_OK;
}


/**
 * ms_ole_stat:
 * @stat: stat information.
 * @fs: filesystem object.
 * @dirpath: directory path.
 * @name: stream or directory name.
 *
 * Gets information about the stream or the directory which is in the directory
 * @dirpath and its name is @file.
 *
 * Returns: a #MsOleErr code.
 **/
MsOleErr
ms_ole_stat (MsOleStat *stat, MsOle *f, const char *path,
	     const char *file)
{
	PPS      *pps;
	MsOleErr  result;

	g_return_val_if_fail (f != NULL, MS_OLE_ERR_BADARG);
	g_return_val_if_fail (file != NULL, MS_OLE_ERR_BADARG);
	g_return_val_if_fail (path != NULL, MS_OLE_ERR_BADARG);
	g_return_val_if_fail (stat != NULL, MS_OLE_ERR_BADARG);

	if ((result = path_to_pps (&pps, f, path, file, FALSE)) !=
	    MS_OLE_ERR_OK)
		return result;

	if (!pps)
		return MS_OLE_ERR_INVALID;

	stat->type = pps->type;
	stat->size = pps->size;

	return MS_OLE_ERR_OK;
}


/**
 * ms_ole_stream_open:
 * @stream: stream object.
 * @fs: filesystem object.
 * @dirpath: directory of the stream.
 * @name: stream name.
 * @mode: mode of opening stream.
 *
 * Opens the stream in @dirpath with the name @name and creates the stream
 * object @stream. If @mode is '%r' it opens read only, and if it is '%w'
 * it opens for write only.
 *
 * Return value: a #MsOleErr code.
 **/
MsOleErr
ms_ole_stream_open (MsOleStream ** const stream, MsOle *f,
		    const char *path, const char *fname, char mode)
{
	PPS         *p;
	MsOleStream *s;
	int lp, panic = 0;
	MsOleErr     result;

	if (!stream)
		return MS_OLE_ERR_BADARG;
	*stream = NULL;

	if (!path || !f)
		return MS_OLE_ERR_BADARG;

	if (mode == 'w' && f->mode != 'w') {
		g_print ("Opening stream '%c' when file is '%c' only\n",
			 mode, f->mode);
		return MS_OLE_ERR_PERM;
	}

	if ((result = path_to_pps (&p, f, path, fname, (mode == 'w'))) !=
	    MS_OLE_ERR_OK)
		return result;

	if (p->type != MsOleStreamT)
		return MS_OLE_ERR_INVALID;

	s           = g_new0 (MsOleStream, 1);
	s->file     = f;
	s->pps      = p;
	s->position = 0;
	s->size     = p->size;
	s->blocks   = NULL;

#if OLE_DEBUG > 0
	g_print ("Parsing blocks\n");
#endif
	if (s->size >= BB_THRESHOLD) {
		BLP b = p->start;

		s->read_copy = ms_ole_read_copy_bb;
		s->read_ptr  = ms_ole_read_ptr_bb;
		s->lseek     = ms_ole_lseek;
		s->tell      = tell_pos;
		s->write     = ms_ole_write_bb;

		s->blocks    = g_array_new (FALSE, FALSE, sizeof(BLP));
		s->type      = MsOleLargeBlock;
		for (lp = 0; !panic && (lp < (s->size + BB_BLOCK_SIZE - 1) / BB_BLOCK_SIZE); lp++) {
			g_array_append_val (s->blocks, b);
#if OLE_DEBUG > 2
			g_print ("Block %d\n", b);
#endif
			if (b == END_OF_CHAIN ||
			    b == SPECIAL_BLOCK ||
			    b == UNUSED_BLOCK) {

				g_warning ("Panic: broken stream, truncating to block %d\n", lp);
				s->size = (lp-1)*BB_BLOCK_SIZE;
				panic   = 1;

#if OLE_DEBUG > 0
				if (b == END_OF_CHAIN)
					g_warning ("Warning: bad file length in '%s'\n", p->name);
				else if (b == SPECIAL_BLOCK)
					g_warning ("Warning: special block in '%s'\n", p->name);
				else if (b == UNUSED_BLOCK)
					g_warning ("Warning: unused block in '%s'\n", p->name);
#endif
			} else
				b = NEXT_BB (f, b);
		}
		if (b != END_OF_CHAIN) {
			BLP next;
			g_warning ("Panic: extra unused blocks on end of '%s', %x wiping it\n",
				   p->name, b);
			while (b != END_OF_CHAIN &&
			       b != UNUSED_BLOCK &&
			       b != SPECIAL_BLOCK &&
			       b < f->bb->len) {
				next = NEXT_BB (f, b);
				g_array_index (f->bb, BLP, b) = END_OF_CHAIN;
				b = next;
			}
		}
	} else {
		BLP b = p->start;

		s->read_copy = ms_ole_read_copy_sb;
		s->read_ptr  = ms_ole_read_ptr_sb;
		s->lseek     = ms_ole_lseek;
		s->tell      = tell_pos;
		s->write     = ms_ole_write_sb;

		if (s->size > 0)
			s->blocks = g_array_new (FALSE, FALSE, sizeof(BLP));
		else
			s->blocks = NULL;

		s->type   = MsOleSmallBlock;

		for (lp = 0; !panic & (lp < (int) (s->size + SB_BLOCK_SIZE - 1) / SB_BLOCK_SIZE); lp++) {
			g_array_append_val (s->blocks, b);
#if OLE_DEBUG > 0
			g_print ("Block %d\n", b);
#endif
			if (b == END_OF_CHAIN ||
			    b == SPECIAL_BLOCK ||
			    b == UNUSED_BLOCK) {

				g_warning ("Panic: broken stream, truncating to block %d\n", lp);
				s->size = (lp-1)*SB_BLOCK_SIZE;
				panic   = 1;
#if OLE_DEBUG > 0
				if (b == END_OF_CHAIN)
					g_warning ("Warning: bad file length in '%s'\n", p->name);
				else if (b == SPECIAL_BLOCK)
					g_warning ("Warning: special block in '%s'\n", p->name);
				else if (b == UNUSED_BLOCK)
					g_warning ("Warning: unused block in '%s'\n", p->name);
#endif
			} else
				b = NEXT_SB (f, b);
		}
		if (b != END_OF_CHAIN) {
			BLP next;
			g_warning ("Panic: extra unused blocks on end of '%s', wiping it\n",
				p->name);
			while (b != END_OF_CHAIN &&
			       b != UNUSED_BLOCK &&
			       b != SPECIAL_BLOCK &&
			       b < f->sb->len) {
				next = NEXT_SB (f, b);
				g_array_index (f->sb, BLP, b) = END_OF_CHAIN;
				b = next;
			}
			if (b != END_OF_CHAIN)
				g_warning ("Panic: even more serious block error\n");
		}
	}
	*stream = s;
	ms_ole_ref (s->file);

	return MS_OLE_ERR_OK;
}


/**
 * ms_ole_stream_close:
 * @stream: stream object to be closed.
 *
 * Closes the @stream.
 *
 * Return value: a #MsOleErr code.
 **/
MsOleErr
ms_ole_stream_close (MsOleStream ** const s)
{
	if (*s) {
		if ((*s)->file && (*s)->file->mode == 'w')
			((PPS *)(*s)->pps)->size = (*s)->size;

		if ((*s)->blocks)
			g_array_free ((*s)->blocks, TRUE);

		ms_ole_unref ((*s)->file);

		g_free (*s);
		*s = NULL;

		return MS_OLE_ERR_OK;
	}
	return MS_OLE_ERR_BADARG;
}
