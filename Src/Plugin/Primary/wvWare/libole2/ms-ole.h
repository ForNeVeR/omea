/**
 * ms-ole.h: MS Office OLE support for Gnumeric
 *
 * Authors:
 *    Michael Meeks (michael@imaginator.com)
 *    Arturo Tena (arturo@directmail.org)
 *
 * Copyright 1998-2001 Ximian, Inc., Arturo Tena
 **/

#ifndef MS_OLE_H
#define MS_OLE_H

#ifdef __cplusplus
extern "C" {
#endif

/* This should be done in glib */
#if !defined(_WIN32) || defined(__CYGWIN__)
#if defined(__QNXNTO__)
# include <sys/types.h>	
#endif
# include <fcntl.h>       /* for mode_t */
#else
# include <stdlib.h>
#ifdef __MINGW32__
# include <sys/types.h>
#else
	typedef unsigned long mode_t;
	typedef long off_t;
#endif
	typedef size_t ssize_t;
	typedef long caddr_t;
#endif

#include <glib.h>

typedef enum {
	MS_OLE_ERR_OK,
	MS_OLE_ERR_EXIST,
	MS_OLE_ERR_INVALID,
	MS_OLE_ERR_FORMAT,
	MS_OLE_ERR_PERM,
	MS_OLE_ERR_MEM,
	MS_OLE_ERR_SPACE,
	MS_OLE_ERR_NOTEMPTY,
	MS_OLE_ERR_BADARG
} MsOleErr;

typedef enum {
	MsOleSeekSet,
	MsOleSeekCur,
	MsOleSeekEnd
} MsOleSeek;

typedef enum  {
	MsOleStorageT = 1,
	MsOleStreamT  = 2,
	MsOleRootT    = 5
} MsOleType;

typedef guint32 MsOlePos;
typedef gint32  MsOleSPos;

typedef struct _MsOle             MsOle;
typedef struct _MsOleStat         MsOleStat;
typedef struct _MsOleStream       MsOleStream;
typedef struct _MsOleSysWrappers  MsOleSysWrappers;
typedef gpointer MsOleHandleType;

#define BAD_MSOLE_HANDLE NULL

struct _MsOleSysWrappers {
	MsOleHandleType     (*open2)	(const char *pathname, int flags,
					 gpointer closure);
	MsOleHandleType     (*open3)	(const char *pathname, int flags, 
					 mode_t mode, gpointer closure);
	ssize_t (*read)		(MsOleHandleType fd, void *buf, size_t count,
				 gpointer closure);
	int     (*close)	(MsOleHandleType fd, gpointer closure);
	ssize_t (*write)	(MsOleHandleType fd, const void *buf, 
				 size_t count, gpointer closure);
	off_t   (*lseek)	(MsOleHandleType fd, off_t offset,
				 int whence, gpointer closure);
	int     (*isregfile)	(MsOleHandleType fd, gpointer closure);
	int     (*getfilesize)	(MsOleHandleType fd, guint32 *size,
				 gpointer closure);

	/* Optionaly implementable */
	void   *(*mmap)         (void *start, size_t length, int prot,
				 int flags, MsOleHandleType fd, off_t offset,
				 gpointer closure);
	int     (*munmap)       (void *start, size_t length,
				 gpointer closure);
  
        gpointer closure;
};

struct _MsOleStat {
	MsOleType type;
	MsOlePos  size;
};

void                    ms_ole_init             (MsOleSysWrappers *wrappers);
extern MsOleSysWrappers *ms_ole_get_default_fs  (void);
#define                 ms_ole_open(fs,path)     ms_ole_open_vfs ((fs), (path), TRUE, NULL)
extern MsOleErr		ms_ole_open_vfs		(MsOle **fs,
						 const char *path,
						 gboolean try_mmap,
						 MsOleSysWrappers *wrappers);
#define                 ms_ole_create(fs,path)   ms_ole_create_vfs ((fs), (path), TRUE, NULL)
extern MsOleErr		ms_ole_create_vfs	(MsOle **fs,
						 const char *path,
						 int try_mmap,
						 MsOleSysWrappers *wrappers);
extern void		ms_ole_destroy		(MsOle **fs);
extern MsOleErr		ms_ole_unlink		(MsOle *fs,
						 const char *path);
extern MsOleErr		ms_ole_directory	(char ***names,
						 MsOle *fs,
						 const char *dirpath);
extern MsOleErr		ms_ole_stat		(MsOleStat *stat,
						 MsOle *fs,
						 const char *dirpath,
						 const char *name);

struct _MsOleStream {

	MsOlePos size;

	gint		(*read_copy)	(MsOleStream *stream,
					 guint8 *ptr,
					 MsOlePos length);

	guint8 *	(*read_ptr)	(MsOleStream *stream,
					 MsOlePos length);

	MsOleSPos	(*lseek)	(MsOleStream *stream,
					 MsOleSPos bytes,
					 MsOleSeek type);

	MsOlePos	(*tell)		(MsOleStream *stream);

	MsOlePos	(*write)	(MsOleStream *stream,
					 guint8 *ptr,
					 MsOlePos length);


	/**
	 * Private.
	 **/
	enum {
		MsOleSmallBlock,
		MsOleLargeBlock
	} type;
	MsOle		*file;
	void		*pps;		/* Straight PPS */
	GArray		*blocks;	/* A list of the blocks in the file
					   if NULL: no file */
	MsOlePos	 position;	/* Current offset into file.
					   Points to the next byte to read */
};

#define MS_OLE_GET_GUINT8(p)  (*((const guint8 *)(p) + 0))
#define MS_OLE_GET_GUINT16(p) (guint16)(*((const guint8 *)(p)+0) |        \
					(*((const guint8 *)(p)+1)<<8))
#define MS_OLE_GET_GUINT32(p) (guint32)(*((const guint8 *)(p)+0) |        \
					(*((const guint8 *)(p)+1)<<8) |   \
					(*((const guint8 *)(p)+2)<<16) |  \
					(*((const guint8 *)(p)+3)<<24))
#define MS_OLE_GET_GUINT64(p) (MS_OLE_GET_GUINT32(p) | \
			       (((guint32)MS_OLE_GET_GUINT32((const guint8 *)(p)+4))<<32))

#define MS_OLE_SET_GUINT8(p,n)  (*((guint8 *)(p) + 0) = n)
#define MS_OLE_SET_GUINT16(p,n) ((*((guint8 *)(p)+0)=((n)&0xff)),         \
				 (*((guint8 *)(p)+1)=((n)>>8)&0xff))
#define MS_OLE_SET_GUINT32(p,n) ((*((guint8 *)(p)+0)=((n))&0xff),         \
				 (*((guint8 *)(p)+1)=((n)>>8)&0xff),      \
				 (*((guint8 *)(p)+2)=((n)>>16)&0xff),     \
				 (*((guint8 *)(p)+3)=((n)>>24)&0xff))

extern MsOleErr		ms_ole_stream_open	(MsOleStream ** const stream,
						 MsOle *fs,
						 const char *dirpath,
						 const char *name,
						 char mode);
extern MsOleErr		ms_ole_stream_close	(MsOleStream ** const stream);
extern MsOleErr		ms_ole_stream_duplicate	(MsOleStream ** const stream_copy,
						 const MsOleStream *
						 const stream);


extern gint ms_ole_stream_read_copy   (MsOleStream *stream,
                                       guint8 *ptr,
                                       MsOlePos length);

extern guint8 * ms_ole_stream_read_ptr     (MsOleStream *stream,
					     MsOlePos length);

extern MsOleSPos ms_ole_stream_lseek        (MsOleStream *stream,
					     MsOleSPos bytes,
					     MsOleSeek type);

extern MsOlePos  ms_ole_stream_tell         (MsOleStream *stream);

extern MsOlePos  ms_ole_stream_write       (MsOleStream *stream,
					    guint8 *ptr,
					    MsOlePos length);



extern void		ms_ole_dump		(guint8 const *ptr,
						 guint32 len);

extern void		ms_ole_ref		(MsOle *fs);
extern void		ms_ole_unref		(MsOle *fs);
extern void		ms_ole_debug		(MsOle *fs,
						 int magic);

#ifdef __cplusplus
}
#endif

#endif	/* MS_OLE_H */
