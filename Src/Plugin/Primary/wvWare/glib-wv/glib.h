/* GLIB - Library of useful routines for C programming
 * Copyright (C) 1995-1997  Peter Mattis, Spencer Kimball and Josh MacDonald
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License as published by the Free Software Foundation; either
 * version 2 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.	 See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License along with this library; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

/*
 * Modified by the GLib Team and others 1997-1999.  See the AUTHORS
 * file for a list of people on the GLib Team.  See the ChangeLog
 * files for a list of changes.  These files are distributed with
 * GLib at ftp://ftp.gtk.org/pub/gtk/. 
 */

#ifndef __G_LIB_H__
#define __G_LIB_H__

/* system specific config file glibconfig.h provides definitions for
 * the extrema of many of the standard types. These are:
 *
 *  G_MINSHORT, G_MAXSHORT
 *  G_MININT, G_MAXINT
 *  G_MINLONG, G_MAXLONG
 *  G_MINFLOAT, G_MAXFLOAT
 *  G_MINDOUBLE, G_MAXDOUBLE
 *
 * It also provides the following typedefs:
 *
 *  gint8, guint8
 *  gint16, guint16
 *  gint32, guint32
 *  gint64, guint64
 *
 * It defines the G_BYTE_ORDER symbol to one of G_*_ENDIAN (see later in
 * this file). 
 *
 * And it provides a way to store and retrieve a `gint' in/from a `gpointer'.
 * This is useful to pass an integer instead of a pointer to a callback.
 *
 *  GINT_TO_POINTER(i), GUINT_TO_POINTER(i)
 *  GPOINTER_TO_INT(p), GPOINTER_TO_UINT(p)
 *
 * Finally, it provide the following wrappers to STDC functions:
 *
 *  g_ATEXIT
 *    To register hooks which are executed on exit().
 *    Usually a wrapper for STDC atexit.
 *
 *  void *g_memmove(void *dest, const void *src, guint count);
 *    A wrapper for STDC memmove, or an implementation, if memmove doesn't
 *    exist.  The prototype looks like the above, give or take a const,
 *    or size_t.
 */

#define g_memmove(d,s,n) { memmove ((d), (s), (n)); }
#define G_MAXINT 2147483647

/* include varargs functions for assertment macros
 */
#include <stdarg.h>

#ifdef __cplusplus
extern "C" {
#endif /* __cplusplus */


/* Provide type definitions for commonly used types.
 *  These are useful because a "gint8" can be adjusted
 *  to be 1 byte (8 bits) on all platforms. Similarly and
 *  more importantly, "gint32" can be adjusted to be
 *  4 bytes (32 bits) on all platforms.
 */

typedef char   gchar;
typedef short  gshort;
typedef long   glong;
typedef int    gint;
typedef gint   gboolean;

typedef unsigned char	guchar;
typedef unsigned short	gushort;
typedef unsigned long	gulong;
typedef unsigned int	guint;

typedef float	gfloat;
typedef double	gdouble;

/* HAVE_LONG_DOUBLE doesn't work correctly on all platforms.
 * Since gldouble isn't used anywhere, just disable it for now */

#if 0
#ifdef HAVE_LONG_DOUBLE
typedef long double gldouble;
#else /* HAVE_LONG_DOUBLE */
typedef double gldouble;
#endif /* HAVE_LONG_DOUBLE */
#endif /* 0 */

typedef void* gpointer;
typedef const void *gconstpointer;

  typedef int   gint32;
  typedef unsigned int guint32;

  typedef short gint16;
  typedef unsigned short guint16;

  typedef char  gint8;
  typedef unsigned char guint8;

typedef gint32	gssize;
typedef guint32 gsize;

typedef void	(*GPrintFunc)		(const gchar	*string);
void		g_print			(const gchar	*format,
					 ...);
void		g_printerr		(const gchar	*format,
					 ...);

/* Provide definitions for some commonly used macros.
 *  Some of them are only provided if they haven't already
 *  been defined. It is assumed that if they are already
 *  defined then the current definition is correct.
 */
#ifndef	NULL
#define	NULL	((void*) 0)
#endif

#ifndef	FALSE
#define	FALSE	(0)
#endif

#ifndef	TRUE
#define	TRUE	(!FALSE)
#endif

#undef	MAX
#define MAX(a, b)  (((a) > (b)) ? (a) : (b))

#undef	MIN
#define MIN(a, b)  (((a) < (b)) ? (a) : (b))

#undef	ABS
#define ABS(a)	   (((a) < 0) ? -(a) : (a))

#undef	CLAMP
#define CLAMP(x, low, high)  (((x) > (high)) ? (high) : (((x) < (low)) ? (low) : (x)))

#ifndef GINT_TO_POINTER
#define GINT_TO_POINTER(i) (gpointer)(i)
#endif

#ifndef GPOINTER_TO_INT
#define GPOINTER_TO_INT(p) (gint)(p)
#endif

/* Define G_VA_COPY() to do the right thing for copying va_list variables.
 * glibconfig.h may have already defined G_VA_COPY as va_copy or __va_copy.
 */
#if !defined (G_VA_COPY)
#  if defined (__GNUC__) && defined (__PPC__) && (defined (_CALL_SYSV) || defined (_WIN32))
#  define G_VA_COPY(ap1, ap2)	  (*(ap1) = *(ap2))
#  elif defined (G_VA_COPY_AS_ARRAY)
#  define G_VA_COPY(ap1, ap2)	  g_memmove ((ap1), (ap2), sizeof (va_list))
#  else /* va_list is a pointer */
#  define G_VA_COPY(ap1, ap2)	  ((ap1) = (ap2))
#  endif /* va_list is a pointer */
#endif /* !G_VA_COPY */


/* Provide macros for easily allocating memory. The macros
 *  will cast the allocated memory to the specified type
 *  in order to avoid compiler warnings. (Makes the code neater).
 */

#  define g_new(type, count)	  \
      ((type *) g_malloc ((unsigned) sizeof (type) * (count)))
#  define g_new0(type, count)	  \
      ((type *) g_malloc0 ((unsigned) sizeof (type) * (count)))
#  define g_renew(type, mem, count)	  \
      ((type *) g_realloc (mem, (unsigned) sizeof (type) * (count)))

#define g_mem_chunk_create(type, pre_alloc, alloc_type)	( \
  g_mem_chunk_new (#type " mem chunks (" #pre_alloc ")", \
		   sizeof (type), \
		   sizeof (type) * (pre_alloc), \
		   (alloc_type)) \
)
#define g_chunk_new(type, chunk)	( \
  (type *) g_mem_chunk_alloc (chunk) \
)
#define g_chunk_new0(type, chunk)	( \
  (type *) g_mem_chunk_alloc0 (chunk) \
)
#define g_chunk_free(mem, mem_chunk) { \
  g_mem_chunk_free ((mem_chunk), (mem)); \
}


#define g_string(x) #x

/* Provide macros for error handling. The "assert" macros will
 *  exit on failure. The "return" macros will exit the current
 *  function. Two different definitions are given for the macros
 *  if G_DISABLE_ASSERT is not defined, in order to support gcc's
 *  __PRETTY_FUNCTION__ capability.
 */

#ifdef G_DISABLE_ASSERT

#define g_assert(expr)
#define g_assert_not_reached()

#else /* !G_DISABLE_ASSERT */

#define g_assert(expr){		\
     if (!(expr)) {						\
              g_printerr ("Critical Assertion Failed: ");         \
              g_printerr ("FILE %s: LINE %d (%s)\n", __FILE__, __LINE__,  \
                          #expr);                               \
	} \
     }

#define g_assert_not_reached() { \
     g_printerr ("Critical Error Should not be Reached: "); \
     g_printerr ("FILE %s: LINE %d\n", __FILE__, __LINE__);         \
     }

#endif /* !G_DISABLE_ASSERT */

#ifdef G_DISABLE_CHECKS

#define g_return_if_fail(expr)
#define g_return_val_if_fail(expr,val)

#else /* !G_DISABLE_CHECKS */

#define g_return_if_fail(expr) {		\
     if (!(expr))						\
       {							\
	 g_printerr ("Assertion (%s) failed: ", #expr);         \
         g_printerr (" FILE %s: LINE %d\n", __FILE__, __LINE__); \
	 return;						\
       };				}

#define g_return_val_if_fail(expr, val)	{		\
     if (!(expr))						\
       {							\
	 g_printerr ("Assertion (%s) failed: ", #expr);         \
         g_printerr (" FILE %s: LINE %d\n", __FILE__, __LINE__); \
	 return val;						\
       };				}

#endif /* !G_DISABLE_CHECKS */


/* Portable endian checks and conversions
 *
 * glibconfig.h defines G_BYTE_ORDER which expands to one of
 * the below macros.
 */
#define G_LITTLE_ENDIAN 1234
#define G_BIG_ENDIAN    4321
#define G_PDP_ENDIAN    3412		/* unused, need specific PDP check */	


/* Glib version.
 * we prefix variable declarations so they can
 * properly get exported in windows dlls.
 */
#ifdef NATIVE_WIN32
#  ifdef GLIB_COMPILATION
#    define GUTILS_C_VAR __declspec(dllexport)
#  else /* !GLIB_COMPILATION */
#    define GUTILS_C_VAR extern __declspec(dllimport)
#  endif /* !GLIB_COMPILATION */
#else /* !NATIVE_WIN32 */
#  define GUTILS_C_VAR extern
#endif /* !NATIVE_WIN32 */

GUTILS_C_VAR const guint glib_major_version;
GUTILS_C_VAR const guint glib_minor_version;
GUTILS_C_VAR const guint glib_micro_version;
GUTILS_C_VAR const guint glib_interface_age;
GUTILS_C_VAR const guint glib_binary_age;

#define GLIB_CHECK_VERSION(major,minor,micro)    \
    (GLIB_MAJOR_VERSION > (major) || \
     (GLIB_MAJOR_VERSION == (major) && GLIB_MINOR_VERSION > (minor)) || \
     (GLIB_MAJOR_VERSION == (major) && GLIB_MINOR_VERSION == (minor) && \
      GLIB_MICRO_VERSION >= (micro)))

/**************************** Line of defunked ******************************/

/* Forward declarations of glib types.
 */
typedef struct _GAllocator	GAllocator;
typedef struct _GArray		GArray;
typedef struct _GByteArray	GByteArray;
typedef struct _GList		GList;
typedef struct _GMemChunk	GMemChunk;
typedef struct _GPtrArray	GPtrArray;
typedef struct _GSList		GSList;
typedef struct _GTree		GTree;

/* Tree traverse flags */
typedef enum
{
  G_TRAVERSE_LEAFS	= 1 << 0,
  G_TRAVERSE_NON_LEAFS	= 1 << 1,
  G_TRAVERSE_ALL	= G_TRAVERSE_LEAFS | G_TRAVERSE_NON_LEAFS,
  G_TRAVERSE_MASK	= 0x03
} GTraverseFlags;

/* Tree traverse orders */
typedef enum
{
  G_IN_ORDER,
  G_PRE_ORDER,
  G_POST_ORDER,
  G_LEVEL_ORDER
} GTraverseType;

/* Log level shift offset for user defined
 * log levels (0-7 are used by GLib).
 */
#define	G_LOG_LEVEL_USER_SHIFT	(8)

typedef gint		(*GCompareFunc)		(gconstpointer	a,
						 gconstpointer	b);
typedef void		(*GFunc)		(gpointer	data,
						 gpointer	user_data);
typedef gint		(*GSearchFunc)		(gpointer	key,
						 gpointer	data);
typedef	void		(*GVoidFunc)		(void);
typedef gint		(*GTraverseFunc)	(gpointer	key,
						 gpointer	value,
						 gpointer	data);

struct _GList
{
  gpointer data;
  GList *next;
  GList *prev;
};

struct _GSList
{
  gpointer data;
  GSList *next;
};

struct _GArray
{
  gchar *data;
  guint len;
};

struct _GByteArray
{
  guint8 *data;
  guint	  len;
};

struct _GPtrArray
{
  gpointer *pdata;
  guint	    len;
};

/* Doubly linked lists
 */
void   g_list_push_allocator    (GAllocator     *allocator);
void   g_list_pop_allocator     (void);
GList* g_list_alloc		(void);
void   g_list_free		(GList		*list);
void   g_list_free_1		(GList		*list);
GList* g_list_append		(GList		*list,
				 gpointer	 data);
GList* g_list_prepend		(GList		*list,
				 gpointer	 data);
GList* g_list_insert		(GList		*list,
				 gpointer	 data,
				 gint		 position);
GList* g_list_insert_sorted	(GList		*list,
				 gpointer	 data,
				 GCompareFunc	 func);
GList* g_list_concat		(GList		*list1,
				 GList		*list2);
GList* g_list_remove		(GList		*list,
				 gpointer	 data);
GList* g_list_remove_link	(GList		*list,
				 GList		*llink);
GList* g_list_reverse		(GList		*list);
GList* g_list_copy		(GList		*list);
GList* g_list_nth		(GList		*list,
				 guint		 n);
GList* g_list_find		(GList		*list,
				 gpointer	 data);
GList* g_list_find_custom	(GList		*list,
				 gpointer	 data,
				 GCompareFunc	 func);
gint   g_list_position		(GList		*list,
				 GList		*llink);
gint   g_list_index		(GList		*list,
				 gpointer	 data);
GList* g_list_last		(GList		*list);
GList* g_list_first		(GList		*list);
guint  g_list_length		(GList		*list);
void   g_list_foreach		(GList		*list,
				 GFunc		 func,
				 gpointer	 user_data);
GList* g_list_sort              (GList          *list,
		                 GCompareFunc    compare_func);
gpointer g_list_nth_data	(GList		*list,
				 guint		 n);
#define g_list_previous(list)	((list) ? (((GList *)(list))->prev) : NULL)
#define g_list_next(list)	((list) ? (((GList *)(list))->next) : NULL)


/* Singly linked lists
 */
void    g_slist_push_allocator  (GAllocator     *allocator);
void    g_slist_pop_allocator   (void);
GSList* g_slist_alloc		(void);
void	g_slist_free		(GSList		*list);
void	g_slist_free_1		(GSList		*list);
GSList* g_slist_append		(GSList		*list,
				 gpointer	 data);
GSList* g_slist_prepend		(GSList		*list,
				 gpointer	 data);
GSList* g_slist_insert		(GSList		*list,
				 gpointer	 data,
				 gint		 position);
GSList* g_slist_insert_sorted	(GSList		*list,
				 gpointer	 data,
				 GCompareFunc	 func);
GSList* g_slist_concat		(GSList		*list1,
				 GSList		*list2);
GSList* g_slist_remove		(GSList		*list,
				 gpointer	 data);
GSList* g_slist_remove_link	(GSList		*list,
				 GSList		*llink);
GSList* g_slist_reverse		(GSList		*list);
GSList*	g_slist_copy		(GSList		*list);
GSList* g_slist_nth		(GSList		*list,
				 guint		 n);
GSList* g_slist_find		(GSList		*list,
				 gpointer	 data);
GSList* g_slist_find_custom	(GSList		*list,
				 gpointer	 data,
				 GCompareFunc	 func);
gint	g_slist_position	(GSList		*list,
				 GSList		*llink);
gint	g_slist_index		(GSList		*list,
				 gpointer	 data);
GSList* g_slist_last		(GSList		*list);
guint	g_slist_length		(GSList		*list);
void	g_slist_foreach		(GSList		*list,
				 GFunc		 func,
				 gpointer	 user_data);
GSList*  g_slist_sort           (GSList          *list,
		                 GCompareFunc    compare_func);
gpointer g_slist_nth_data	(GSList		*list,
				 guint		 n);
#define g_slist_next(slist)	((slist) ? (((GSList *)(slist))->next) : NULL)


/* Balanced binary trees
 */
GTree*	 g_tree_new	 (GCompareFunc	 key_compare_func);
void	 g_tree_destroy	 (GTree		*tree);
void	 g_tree_insert	 (GTree		*tree,
			  gpointer	 key,
			  gpointer	 value);
void	 g_tree_remove	 (GTree		*tree,
			  gpointer	 key);
gpointer g_tree_lookup	 (GTree		*tree,
			  gpointer	 key);
void	 g_tree_traverse (GTree		*tree,
			  GTraverseFunc	 traverse_func,
			  GTraverseType	 traverse_type,
			  gpointer	 data);
gpointer g_tree_search	 (GTree		*tree,
			  GSearchFunc	 search_func,
			  gpointer	 data);
gint	 g_tree_height	 (GTree		*tree);
gint	 g_tree_nnodes	 (GTree		*tree);

void g_error (const gchar *format,
	      ...);

void g_message (const gchar *format,
		...);

void g_warning (const gchar *format,
		...);

gpointer g_malloc      (gulong	  size);
gpointer g_malloc0     (gulong	  size);
gpointer g_realloc     (gpointer  mem,
			gulong	  size);
void	 g_free	       (gpointer  mem);

void	 g_mem_profile (void);
void	 g_mem_check   (gpointer  mem);

/* Generic allocators
 */
GAllocator* g_allocator_new   (const gchar  *name,
			       guint         n_preallocs);
void        g_allocator_free  (GAllocator   *allocator);

#define	G_ALLOCATOR_LIST	(1)
#define	G_ALLOCATOR_SLIST	(2)
#define	G_ALLOCATOR_NODE	(3)


/* "g_mem_chunk_new" creates a new memory chunk.
 * Memory chunks are used to allocate pieces of memory which are
 *  always the same size. Lists are a good example of such a data type.
 * The memory chunk allocates and frees blocks of memory as needed.
 *  Just be sure to call "g_mem_chunk_free" and not "g_free" on data
 *  allocated in a mem chunk. ("g_free" will most likely cause a seg
 *  fault...somewhere).
 *
 * Oh yeah, GMemChunk is an opaque data type. (You don't really
 *  want to know what's going on inside do you?)
 */

/* ALLOC_ONLY MemChunk's can only allocate memory. The free operation
 *  is interpreted as a no op. ALLOC_ONLY MemChunk's save 4 bytes per
 *  atom. (They are also useful for lists which use MemChunk to allocate
 *  memory but are also part of the MemChunk implementation).
 * ALLOC_AND_FREE MemChunk's can allocate and free memory.
 */

#define G_ALLOC_ONLY	  1
#define G_ALLOC_AND_FREE  2

GMemChunk* g_mem_chunk_new     (gchar	  *name,
				gint	   atom_size,
				gulong	   area_size,
				gint	   type);
void	   g_mem_chunk_destroy (GMemChunk *mem_chunk);
gpointer   g_mem_chunk_alloc   (GMemChunk *mem_chunk);
gpointer   g_mem_chunk_alloc0  (GMemChunk *mem_chunk);
void	   g_mem_chunk_free    (GMemChunk *mem_chunk,
				gpointer   mem);
void	   g_mem_chunk_clean   (GMemChunk *mem_chunk);
void	   g_mem_chunk_reset   (GMemChunk *mem_chunk);
void	   g_mem_chunk_print   (GMemChunk *mem_chunk);
void	   g_mem_chunk_info    (void);

/* Ah yes...we have a "g_blow_chunks" function.
 * "g_blow_chunks" simply compresses all the chunks. This operation
 *  consists of freeing every memory area that should be freed (but
 *  which we haven't gotten around to doing yet). And, no,
 *  "g_blow_chunks" doesn't follow the naming scheme, but it is a
 *  much better name than "g_mem_chunk_clean_all" or something
 *  similar.
 */
void g_blow_chunks (void);


/* String utility functions that modify a string argument or
 * return a constant string that must not be freed.
 */
#define	 G_STR_DELIMITERS	"_-|> <."
gchar*	 g_strdelimit		(gchar	     *string,
				 const gchar *delimiters,
				 gchar	      new_delimiter);
gdouble	 g_strtod		(const gchar *nptr,
				 gchar	    **endptr);
gchar*	 g_strerror		(gint	      errnum);
gchar*	 g_strsignal		(gint	      signum);
gint	 g_strcasecmp		(const gchar *s1,
				 const gchar *s2);
gint	 g_strncasecmp		(const gchar *s1,
				 const gchar *s2,
				 guint 	      n);
void	 g_strdown		(gchar	     *string);
void	 g_strup		(gchar	     *string);
void	 g_strreverse		(gchar	     *string);
/* removes leading spaces */
gchar*   g_strchug              (gchar        *string);
/* removes trailing spaces */
gchar*  g_strchomp              (gchar        *string);
/* removes leading & trailing spaces */
#define g_strstrip( string )	g_strchomp (g_strchug (string))

/* String utility functions that return a newly allocated string which
 * ought to be freed from the caller at some point.
 */
gchar*	 g_strdup		(const gchar *str);
gchar*	 g_strdup_printf	(const gchar *format,
				 ...);
gchar*	 g_strdup_vprintf	(const gchar *format,
				 va_list      args);
gchar*	 g_strndup		(const gchar *str,
				 guint	      n);
gchar*	 g_strnfill		(guint	      length,
				 gchar	      fill_char);
gchar*	 g_strconcat		(const gchar *string1,
				 ...); /* NULL terminated */
gchar*   g_strjoin		(const gchar  *separator,
				 ...); /* NULL terminated */
gchar*	 g_strescape		(gchar	      *string);
gpointer g_memdup		(gconstpointer mem,
				 guint	       byte_size);

/* NULL terminated string arrays.
 * g_strsplit() splits up string into max_tokens tokens at delim and
 * returns a newly allocated string array.
 * g_strjoinv() concatenates all of str_array's strings, sliding in an
 * optional separator, the returned string is newly allocated.
 * g_strfreev() frees the array itself and all of its strings.
 */
gchar**	 g_strsplit		(const gchar  *string,
				 const gchar  *delimiter,
				 gint          max_tokens);
gchar*   g_strjoinv		(const gchar  *separator,
				 gchar       **str_array);
void     g_strfreev		(gchar       **str_array);



/* calculate a string size, guarranteed to fit format + args.
 */
guint	g_printf_string_upper_bound (const gchar* format,
				     va_list	  args);

/* Resizable arrays, remove fills any cleared spot and shortens the
 * array, while preserving the order. remove_fast will distort the
 * order by moving the last element to the position of the removed 
 */

#define g_array_append_val(a,v)	  g_array_append_vals (a, &v, 1)
#define g_array_prepend_val(a,v)  g_array_prepend_vals (a, &v, 1)
#define g_array_insert_val(a,i,v) g_array_insert_vals (a, i, &v, 1)
#define g_array_index(a,t,i)      (((t*) (a)->data) [(i)])

GArray* g_array_new	          (gboolean	    zero_terminated,
				   gboolean	    clear,
				   guint	    element_size);
void	g_array_free	          (GArray	   *array,
				   gboolean	    free_segment);
GArray* g_array_append_vals       (GArray	   *array,
				   gconstpointer    data,
				   guint	    len);
GArray* g_array_prepend_vals      (GArray	   *array,
				   gconstpointer    data,
				   guint	    len);
GArray* g_array_insert_vals       (GArray          *array,
				   guint            index,
				   gconstpointer    data,
				   guint            len);
GArray* g_array_set_size          (GArray	   *array,
				   guint	    length);
GArray* g_array_remove_index	  (GArray	   *array,
				   guint	    index);
GArray* g_array_remove_index_fast (GArray	   *array,
				   guint	    index);

/* Resizable pointer array.  This interface is much less complicated
 * than the above.  Add appends appends a pointer.  Remove fills any
 * cleared spot and shortens the array. remove_fast will again distort
 * order.  
 */
#define	    g_ptr_array_index(array,index) (array->pdata)[index]
GPtrArray*  g_ptr_array_new		   (void);
void	    g_ptr_array_free		   (GPtrArray	*array,
					    gboolean	 free_seg);
void	    g_ptr_array_set_size	   (GPtrArray	*array,
					    gint	 length);
gpointer    g_ptr_array_remove_index	   (GPtrArray	*array,
					    guint	 index);
gpointer    g_ptr_array_remove_index_fast  (GPtrArray	*array,
					    guint	 index);
gboolean    g_ptr_array_remove		   (GPtrArray	*array,
					    gpointer	 data);
gboolean    g_ptr_array_remove_fast        (GPtrArray	*array,
					    gpointer	 data);
void	    g_ptr_array_add		   (GPtrArray	*array,
					    gpointer	 data);

/* Byte arrays, an array of guint8.  Implemented as a GArray,
 * but type-safe.
 */

GByteArray* g_byte_array_new	           (void);
void	    g_byte_array_free	           (GByteArray	 *array,
					    gboolean	  free_segment);
GByteArray* g_byte_array_append	           (GByteArray	 *array,
					    const guint8 *data,
					    guint	  len);
GByteArray* g_byte_array_prepend           (GByteArray	 *array,
					    const guint8 *data,
					    guint	  len);
GByteArray* g_byte_array_set_size          (GByteArray	 *array,
					    guint	  length);
GByteArray* g_byte_array_remove_index	   (GByteArray	 *array,
					    guint	  index);
GByteArray* g_byte_array_remove_index_fast (GByteArray	 *array,
					    guint	  index);

#ifdef __cplusplus
}
#endif /* __cplusplus */


#endif /* __G_LIB_H__ */
