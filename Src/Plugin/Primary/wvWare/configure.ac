dnl Process this file with autoconf to produce a configure script.
AC_INIT([wv],[1.0.0],[wvware-devel@sourceforge.net])
AC_CONFIG_SRCDIR([exporter/wvexporter.h])

dnl This next section is courtesy gtk+
dnl
# Making releases:
#   WV_MICRO_VERSION += 1;
#   WV_INTERFACE_AGE += 1;
#   WV_BINARY_AGE += 1;
# if any functions have been added, set WV_INTERFACE_AGE to 0.
# if backwards compatibility has been broken,
# set WV_BINARY_AGE and WV_INTERFACE_AGE to 0.
#
WV_MAJOR_VERSION=1
WV_MINOR_VERSION=0
WV_MICRO_VERSION=0
WV_INTERFACE_AGE=0
WV_BINARY_AGE=0
WV_VERSION=$WV_MAJOR_VERSION.$WV_MINOR_VERSION.$WV_MICRO_VERSION

AC_SUBST(WV_MAJOR_VERSION)
AC_SUBST(WV_MINOR_VERSION)
AC_SUBST(WV_MICRO_VERSION)
AC_SUBST(WV_INTERFACE_AGE)
AC_SUBST(WV_BINARY_AGE)
AC_SUBST(WV_VERSION)

# libtool versioning
LT_RELEASE=$WV_MAJOR_VERSION.$WV_MINOR_VERSION
LT_CURRENT=`expr $WV_MICRO_VERSION - $WV_INTERFACE_AGE`
LT_REVISION=$WV_INTERFACE_AGE
LT_AGE=`expr $WV_BINARY_AGE - $WV_INTERFACE_AGE`
AC_SUBST(LT_RELEASE)
AC_SUBST(LT_CURRENT)
AC_SUBST(LT_REVISION)
AC_SUBST(LT_AGE)

# For automake.
VERSION=$WV_VERSION
PACKAGE=wv

dnl Initialize automake stuff
AM_INIT_AUTOMAKE($PACKAGE, $VERSION)

# Specify a configuration file
AM_CONFIG_HEADER(config.h)

AH_TOP([
#ifndef HAVE_WV_CONFIG_H
#define HAVE_WV_CONFIG_H
])

dnl Checks for programs.

AC_PROG_CC
if test "x$GCC" = "xyes"; then
	ANSI_CFLAGS="-ansi"
else
	ANSI_CFLAGS=""
fi
AC_SUBST(ANSI_CFLAGS)

AC_PROG_CPP
AC_PROG_AWK
AC_PROG_INSTALL
AC_PROG_LN_S
AC_PROG_MAKE_SET

AC_DISABLE_STATIC
AC_PROG_LIBTOOL

dnl Checks for headers.

AC_HEADER_STDC
dnl AC_HEADER_SYS_WAIT
dnl AC_TYPE_SIGNAL
dnl AC_FUNC_WAIT3

AC_CHECK_HEADERS(fcntl.h io.h malloc.h sys/file.h sys/ioctl.h unistd.h)

dnl Checks for typedefs, structures, and compiler characteristics.

AC_C_CONST

dnl exporter support
dnl =======================================================

AC_ARG_WITH(exporter,[  --with-exporter         build the exporter (default is no)],[
	if test "x$withval" = "xno"; then
		EXPORTER=""
	else
		dnl Need to watch out for case-insensitive file systems (e.g. HFS+)
		EXPORTER="BEXPORTER"
	fi
],[	EXPORTER=""
])

AC_SUBST(EXPORTER)
AM_CONDITIONAL(BUILD_EXPORTER,[ test "$EXPORTER" = "BEXPORTER" ])

dnl zlib support
dnl =======================================================

_cppflags=$CPPFLAGS
_ldflags=$LDFLAGS

AC_ARG_WITH(zlib,[  --with-zlib=DIR         use libz in DIR],[
	if test "x$withval" = "xyes"; then
		ZLIB_DIR=""
	elif test "x$withval" != "xno"; then
		ZLIB_DIR="$withval"
		CPPFLAGS="$CPPFLAGS -I$withval/include"
		LDFLAGS="$LDFLAGS -L$withval/lib"
	else
		AC_MSG_WARN([No zlib? Are you sure? I think I'll look for it anyway...])
		ZLIB_DIR=""
	fi
],[	ZLIB_DIR=""
])

AC_CHECK_HEADER(zlib.h,[
	AC_CHECK_LIB(z,gzread,[
		AC_DEFINE(HAVE_ZLIB,1,[Define if you want zlib to uncompress wmf files])
		HAVE_ZLIB=1
		if test "x$ZLIB_DIR" != "x"; then
			ZLIB_CFLAGS="-I$ZLIB_DIR/include"
			ZLIB_LIBS="-L$ZLIB_DIR/lib -lz"
		else
			ZLIB_CFLAGS=""
			ZLIB_LIBS="-lz"
		fi
	],[	AC_CHECK_LIB(gz,gzread,[
			AC_DEFINE(HAVE_ZLIB,1,[Define if you want zlib to uncompress wmf files])
			HAVE_ZLIB=1
			if test "x$ZLIB_DIR" != "x"; then
				ZLIB_CFLAGS="-I$ZLIB_DIR/include"
				ZLIB_LIBS="-L$ZLIB_DIR/lib -lgz"
			else
				ZLIB_CFLAGS=""
				ZLIB_LIBS="-lgz"
			fi
		],[	dnl Shouldn't this be an error?
			AC_MSG_WARN([* * * Unable to find zlib, which is kinda necessary. * * *])
			HAVE_ZLIB=0
			ZLIB_CFLAGS=""
			ZLIB_LIBS=""
		])
	])
])

AC_SUBST(ZLIB_CFLAGS)
AC_SUBST(ZLIB_LIBS)

CPPFLAGS=$_cppflags
LDFLAGS=$_ldflags

dnl system png support
dnl ========================================================

_cppflags=$CPPFLAGS
_ldflags=$LDFLAGS

dnl libpng requires zlib
CPPFLAGS="$CPPFLAGS $ZLIB_CFLAGS"

AC_ARG_WITH(png,[  --with-png=DIR          use png library in DIR],[
	if test "x$withval" = "xyes"; then
		PNG_DIR=""
	elif test "x$withval" != "xno"; then
		PNG_DIR=$withval
		CPPFLAGS="$CPPFLAGS -I$withval/include"
		LDFLAGS="$LDFLAGS -L$withval/lib"
	else
		AC_MSG_WARN([No libpng? Are you sure? I think I'll look for it anyway...])
		PNG_DIR=""
	fi
],[	PNG_DIR=""
])

AC_CHECK_HEADER(png.h,[
	AC_CHECK_HEADER(pngconf.h,[
		AC_CHECK_LIB(png, png_free,[
			AC_DEFINE(HasPNG,1,[define if you have libpng])
			HAVE_PNG=1
			if test "x$PNG_DIR" != "x"; then
				PNG_CFLAGS="-I$PNG_DIR/include"
				PNG_LIBS="-L$PNG_DIR/lib -lpng"
			else
				PNG_CFLAGS=""
				PNG_LIBS="-lpng"
			fi
		],[	dnl Shouldn't this be an error?
			AC_MSG_WARN([* * * Unable to find libpng, which is kinda necessary. * * *])
			HAVE_PNG=0
			PNG_CFLAGS=""
			PNG_LIBS=""
		],$ZLIB_LIBS -lm)
	],[	dnl Shouldn't this be an error?
		AC_MSG_WARN([* * * Missing "pngconf.h" header file * * *])
		HAVE_PNG=0
		PNG_CFLAGS=""
		PNG_LIBS=""
	])
],[	dnl Shouldn't this be an error?
	AC_MSG_WARN([* * * Missing "png.h" header file * * *])
	HAVE_PNG=0
	PNG_CFLAGS=""
	PNG_LIBS=""
])

AC_SUBST(PNG_CFLAGS)
AC_SUBST(PNG_LIBS)

CPPFLAGS=$_cppflags
LDFLAGS=$_ldflags

dnl system XML support
dnl ========================================================

_cppflags=$CPPFLAGS
_ldflags=$LDFLAGS

dnl There are various options:
dnl (1) wv's 'included' expat (default)
dnl (2) system 'expat' library
dnl (3) system 'libxml2' library
dnl (4) 'peer' expat (i.e., AbiWord build) ?

wv_xml="included"

AC_ARG_WITH(expat,[  --with-expat[=DIR]      use system expat (default is no)],[
	if test "x$withval" = "xyes"; then
		wv_xml="expat"
		XML_DIR=""
	elif test "x$withval" = "xpeer"; then
		wv_xml="peer"
		XML_DIR=""
	elif test "x$withval" != "xno"; then
		wv_xml="expat"
		XML_DIR=$withval
		CPPFLAGS="$CPPFLAGS -I$withval/include"
		LDFLAGS="$LDFLAGS -L$withval/lib"
	fi
])

AC_ARG_WITH(libxml2,[  --with-libxml2[=DIR]    use system libxml2 (default is no)],[
	if test "x$wv_xml" = "xexpat"; then
		AC_MSG_WARN([* * * Cannot specify expat AND libxml2. I'm going to use expat * * *])
	else
		if test "x$withval" = "xyes"; then
			wv_xml="libxml2"
			XML_DIR=""
		elif test "x$withval" != "xno"; then
			wv_xml="libxml2"
			XML_DIR=$withval
		fi
	fi
])

if test "x$wv_xml" = "xincluded"; then
	INCLUDED_EXPAT=1
	HAVE_LIBXML2=0
	XML_CFLAGS=""
	XML_LIBS=""
elif test "x$wv_xml" = "xexpat"; then
	AC_DEFINE(HAVE_EXPAT,1,[Define if you have expat])
	INCLUDED_EXPAT=0
	HAVE_LIBXML2=0
	AC_CHECK_HEADER(expat.h,[
		AC_CHECK_LIB(expat,XML_ParserCreate,[
			if test "x$XML_DIR" != "x"; then
				XML_CFLAGS="-I$XML_DIR/include"
				XML_LIBS="-L$XML_DIR/lib -lexpat"
			else
				XML_CFLAGS=""
				XML_LIBS="-lexpat"
			fi
		],[	AC_MSG_ERROR([* * * Unable to find expat library * * *])
		])
	],[	AC_MSG_ERROR([* * * Unable to find header file "expat.h" * * *])
	])
elif test "x$wv_xml" = "xlibxml2"; then
	AC_DEFINE(HAVE_LIBXML2,1,[Define if you have libxml2])
	INCLUDED_EXPAT=0
	HAVE_LIBXML2=1

	if [ test -n "$XML_DIR" ]; then
		AC_PATH_PROG(XML2_CONFIG,xml2-config, ,[$XML_DIR/bin:$PATH])
	else
		AC_PATH_PROG(XML2_CONFIG,xml2-config)
	fi

	if [ test -n "$XML2_CONFIG" ]; then
		xml2_cflags=`$XML2_CONFIG --cflags`
		xml2_libs=`$XML2_CONFIG --libs`
	else
		AC_MSG_ERROR([* * * unable to find xml2-config; unable to continue * * *])
	fi

	CPPFLAGS="$CPPFLAGS $xml2_cflags"
	LDFLAGS="$LDFLAGS $xml2_libs"

	AC_CHECK_HEADER(libxml/parser.h,[
		AC_CHECK_LIB(xml2,xmlParseDocument,[
			XML_CFLAGS="$xml2_cflags"
			XML_LIBS="$xml2_libs"
		],[	AC_MSG_ERROR([* * * Unable to find libxml2 library * * *])
		],$xml2_libs)
	],[	AC_MSG_ERROR([* * * Unable to find header file "libxml/parser.h" * * *])
	])
elif test "x$wv_xml" = "xpeer"; then
	AC_DEFINE(HAVE_EXPAT,1,[Define if you have expat])
	INCLUDED_EXPAT=0
	HAVE_LIBXML2=0
	XML_CFLAGS="-I\$(top_builddir)/../expat/lib"
	XML_LIBS=""
else
	AC_MSG_ERROR([* * * Glitch! Unexpected value. * * *])
fi

AC_SUBST(INCLUDED_EXPAT)
AM_CONDITIONAL(BUILD_EXPAT,[ test "$INCLUDED_EXPAT" = "1" ])

AC_SUBST(HAVE_LIBXML2)

AC_SUBST(XML_CFLAGS)
AC_SUBST(XML_LIBS)

CPPFLAGS=$_cppflags
LDFLAGS=$_ldflags

dnl system glib support
dnl ========================================================

AC_ARG_WITH(glib,[  --with-glib[=DIR]       use system glib [in DIR]],[
	if [ test "x$withval" = "xno" ]; then
		wv_sys_glib=no
	elif [ test "x$withval" = "xyes" ]; then
		wv_sys_glib=required
		GLIB_DIR=""
	elif [ test "x$withval" = "xglib2" ]; then
		wv_sys_glib=glib2
		GLIB_DIR=""
	else
		wv_sys_glib=required
		GLIB_DIR="$withval"
	fi
],[	wv_sys_glib=check
	GLIB_DIR=""
])

if test $wv_sys_glib = glib2; then
	PKG_CHECK_MODULES(GLIB,glib-2.0 >= 2.0,[
		dnl do we need to set these?
		wv_glib2=yes
	],[	wv_glib2=no
	])
	if test $wv_glib2 = no; then
		AC_MSG_ERROR([$GLIB_PKG_ERRORS])
	fi

elif test $wv_sys_glib != no; then
	wv_glib2=no
	if test "x$GLIB_DIR" = "x"; then
		AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
		if test "x$PKG_CONFIG" != "xno" ; then
			PKG_CHECK_MODULES(GLIB,glib-2.0 >= 2.0,[
				wv_sys_glib=glib2
				wv_glib2=yes
			],[	AC_MSG_WARN([*** falling back to glib-1.2])
			])
		fi
	fi
	if test $wv_glib2 = no; then
		if test "x$GLIB_DIR" = "x"; then
			AC_PATH_PROG(GLIB_CONFIG, glib12-config, , [$PATH])
		else
			AC_PATH_PROG(GLIB_CONFIG, glib12-config, , [$GLIB_DIR/bin:$PATH])
		fi
		if test "x$GLIB_CONFIG" = "x"; then
			if test "x$GLIB_DIR" = "x"; then
				AC_PATH_PROG(GLIB_CONFIG, glib-config, , [$PATH])
			else
				AC_PATH_PROG(GLIB_CONFIG, glib-config, , [$GLIB_DIR/bin:$PATH])
			fi
		fi
		if test "x$GLIB_CONFIG" = "x"; then
			if test $wv_sys_glib = required; then
				AC_MSG_ERROR([* * * Unable to find glib12-config or glib-config in path! * * *])
			fi
			wv_sys_glib=no
		fi
	fi
fi

if test $wv_sys_glib = no; then
	HAVE_GLIB=0
	GLIB_CFLAGS=""
	GLIB_LIBS=""
elif test $wv_sys_glib != glib2; then
	AC_DEFINE(HAVE_GLIB,1,[Define if you have glib])
	HAVE_GLIB=1
	GLIB_CFLAGS=`$GLIB_CONFIG --cflags`
	GLIB_LIBS=`$GLIB_CONFIG --libs`
else
	AC_DEFINE(HAVE_GLIB,1,[Define if you have glib])
	HAVE_GLIB=1
fi

AC_SUBST(HAVE_GLIB)
AM_CONDITIONAL(BUILD_GLIB,[ test "$HAVE_GLIB" = "0" ])
AC_SUBST(GLIB_CFLAGS)
AC_SUBST(GLIB_LIBS)

dnl system libwmf support
dnl ========================================================

dnl Check for libwmf-0.2.1 or higher

_cppflags=$CPPFLAGS
_ldflags=$LDFLAGS

have_libwmf=no

AC_ARG_WITH(libwmf,[  --with-libwmf=DIR       use libwmf(2) in DIR],[
	if [ test "x$withval" != "xno" ]; then
		if [ test "x$withval" != "xyes" ]; then
			LIBWMF_DIR=$withval
		fi
		search_for_libwmf=yes
	else
		search_for_libwmf=no
	fi
],[	search_for_libwmf=no
])

if [ test $search_for_libwmf != no ]; then
	if [ test -n "$LIBWMF_DIR" ]; then
		AC_PATH_PROG(LIBWMF_CONFIG,libwmf-config, ,[$LIBWMF_DIR/bin:$PATH])
	else
		AC_PATH_PROG(LIBWMF_CONFIG,libwmf-config)
	fi

	if [ test -n "$LIBWMF_CONFIG" ]; then
		libwmf_cflags=`$LIBWMF_CONFIG --cflags`
		libwmf_libs=`$LIBWMF_CONFIG --libs`
	else
		AC_MSG_ERROR(* * * unable to find libwmf-config; unable to continue * * *)
	fi

	CPPFLAGS="$CPPFLAGS $libwmf_cflags"
	LDFLAGS="$LDFLAGS $libwmf_libs"

	AC_CHECK_HEADER(libwmf/api.h,[
		AC_CHECK_LIB(wmf,wmf_stream_create,have_libwmf=yes,[
			AC_MSG_ERROR(* * * libwmf >= 0.2.1 required * * *)
		],$libwmf_libs)
		AC_CHECK_HEADER(libwmf/foreign.h,[
			AC_DEFINE(HAVE_LIBWMF_FOREIGN_H,1,[Define if libwmf(2) has support for reading 'foreign' image formats])
		],[	AC_MSG_WARN(* * * libwmf >= 0.2.2 recommended * * *)
		])
	])

	if [ test $have_libwmf != no]; then
		AC_DEFINE(HAVE_LIBWMF,1,[Define if you have libwmf(2) and want it to convert wmf to png files])

		WMF_CFLAGS="$libwmf_cflags"
		WMF_LIBS="$libwmf_libs"
	fi
fi

CPPFLAGS=$_cppflags
LDFLAGS=$_ldflags

AC_SUBST(WMF_LIBS)
AC_SUBST(WMF_CFLAGS)

dnl test for iconv
dnl ========================================================

_cppflags=${CPPFLAGS}
_ldflags=${LDFLAGS}

have_iconv=no
use_peer_libiconv=no
check_for_libiconv=yes
default_to_libiconv=yes

AC_ARG_WITH(libiconv,[  --with-libiconv[=DIR]   use libiconv [in DIR]],[
	if [ test "$withval" = "no" ]; then
		check_for_libiconv=no
	elif [ test "$withval" != "yes" ]; then
		check_for_libiconv=yes
		LIBICONV_DIR=$withval
	fi
],default_to_libiconv=no)

dnl use --with-peer-iconv to get libiconv from the peer directory
dnl 
AC_ARG_WITH(peer-iconv,[  --with-peer-iconv       use libiconv from peer directory ],[
	if test "$withval" != "no"; then
		use_peer_libiconv=$withval
		if [ test -d $srcdir/../libiconv ]; then
			check_for_libiconv=yes
			default_to_libiconv=yes
			LIBICONV_DIR="$srcdir/../libiconv"
		fi
	fi
])

dnl first search for system iconv
dnl 
if [ test "$default_to_libiconv" = "no" ]; then
	AC_CHECK_FUNC(iconv,[
		AC_CHECK_HEADER(iconv.h,[
			have_iconv=sys
			check_for_libiconv=no
		],[	AC_MSG_ERROR(* * * I found system iconv but can't find iconv.h header! * * *)
		])
	])
fi

dnl then search for libiconv
dnl 
if [ test "$check_for_libiconv" = "yes" ]; then
	if [ test "x$LIBICONV_DIR" != "x" ]; then
		if [ test "$use_peer_libiconv" != "no" ]; then
			ICONV_CFLAGS="-I\$(top_builddir)/../libiconv/include"
			ICONV_LIBS="-L\$(top_builddir)/../libiconv/lib/.libs -liconv"
			CPPFLAGS="$CPPFLAGS -I../libiconv/include"
			LDFLAGS="$LDFLAGS -L../libiconv/lib/.libs -liconv"
		else
			ICONV_CFLAGS="-I$LIBICONV_DIR/include"
			ICONV_LIBS="-L$LIBICONV_DIR/lib -liconv"
			CPPFLAGS="$CPPFLAGS $ICONV_CFLAGS"
			LDFLAGS="$LDFLAGS $ICONV_LIBS"
		fi
	else
		ICONV_CFLAGS=""
		ICONV_LIBS="-liconv"
		LDFLAGS="$LDFLAGS $ICONV_LIBS"
	fi

	if [ test "$use_peer_libiconv" = "abi" ]; then
		dnl if configure is called with --with-peer-iconv=abi
		dnl then assume that iconv exists and will be linked eventually
		dnl 
		AC_CHECK_HEADER(iconv.h,have_iconv=abi)
	else
		AC_CHECK_HEADER(iconv.h,[
			AC_CHECK_LIB(iconv,iconv_open,[
				have_iconv=iconv
			],[	AC_CHECK_LIB(iconv,libiconv_open,[
					have_iconv=libiconv
				],[	AC_MSG_ERROR(* * * I found iconv.h header but no working library! * * *)
				])
			])
		])
	fi	
fi

if [ test "$have_iconv" = "no" ]; then
	AC_MSG_ERROR(* * * iconv is required: ftp://ftp.gnu.org/pub/gnu/libiconv * * *)
fi

dnl Apparently this trick fails on RedHat 6.2 (and presumably earlier RedHats & other
dnl concurrent Linux distributions) because g++ issues a warning rather than an error.
dnl Perhaps we should have something like (I'm not sure about the flag):        (fjf)
dnl if test "x$GCC" = "xyes"; then CFLAGS="$CFLAGS --fstrict-ansi"; fi

AC_LANG_CPLUSPLUS

AC_MSG_CHECKING(need for const in iconv)
AC_TRY_COMPILE([
#include <iconv.h>
],[
	iconv_t handle;
	size_t ib;
	size_t ob;
	const char* in = 0;
	char* out = 0;
	iconv (handle,const_cast<char **>(&in),&ib,&out,&ob);
],[	AC_MSG_RESULT(no)
],[	AC_MSG_RESULT(yes)
	AC_DEFINE(ICONV_REQUIRES_CONST,1,[Define this if the second argument of iconv requires a const])
])

AC_LANG_C

AC_SUBST(ICONV_CFLAGS)
AC_SUBST(ICONV_LIBS)

CPPFLAGS=$_cppflags
LDFLAGS=$_ldflags

dnl end of iconv
dnl ========================================================

dnl ========================================================
dnl begin of possible optimization tests
dnl read notes/optimization

AC_CHECK_SIZEOF(char,1)
AC_CHECK_SIZEOF(short,2)
AC_CHECK_SIZEOF(int,4)
AC_CHECK_SIZEOF(long,4)

if [test "$SIZEOF_CHAR" != "1" ]; then
    if [test "$SIZEOF_SHORT" != "2" ]; then
        if [test "$SIZEOF_INT" != "4" ]; then
            AC_DEFINE(MATCHED_TYPE,1,[Define if sizeof({char,short,int}) != {1,2,4}])
        fi
    fi
fi

if [ test "$cross_compiling" != "yes" ]; then
    AC_C_BIGENDIAN
dnl	here we will know the endianness
	if [ test "$ac_cv_c_bigendian" = "no" ]; then
	    AC_DEFINE(XML_BYTE_ORDER,12,[Define as 12 for little-endian, 21 for big-endian])
	else 
		if [ test "$ac_cv_c_bigendian" = "yes" ]; then
			AC_DEFINE(XML_BYTE_ORDER,21,[Define as 12 for little-endian, 21 for big-endian])
		fi
	fi
else
dnl	here we will not know the endianness, so we will
dnl go with the mechanism that works everywhere
    AC_DEFINE(WORDS_BIGENDIAN,1,[Define if byte order is big-endian])
fi

dnl given matched types and little endian we can read types larger
dnl than a byte with fread and avoid byteswapping

dnl at some later stage we will have a test to see if the compiler can
dnl get rid of holes, for the moment we will assume only that we can
dnl do it if we have __GNUC__ defined

dnl end of possible optimization tests

dnl check for getopt
dnl 
AC_CHECK_FUNC(getopt_long,,[
	AC_LIBOBJ(getopt1)
	AC_LIBOBJ(getopt)
])

dnl check for strerror
dnl 
AC_CHECK_FUNC(strerror,,[
	AC_LIBOBJ(strerror)
])

dnl check for strcasecmp
dnl 
AC_CHECK_FUNC(strcasecmp,,[
	AC_LIBOBJ(strcasecmp)
])

AC_EGREP_HEADER(strcasecmp,string.h,,[
	AC_DEFINE(DEFINE_STRCASECMP,1,[Define this if strcasecmp must be defined])
])

dnl check for memcpy, if not there we'll use bcopy
dnl 
AC_CHECK_FUNCS(memcpy)

dnl check for mmap
dnl 
AC_FUNC_MMAP

AH_BOTTOM([
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_FCNTL_H
#include <fcntl.h>
#endif
#if defined(HAVE_ERRNO_H)
#include <errno.h>
#endif
#include <sys/types.h>
#include <sys/stat.h>
#include <limits.h>
#include <stdarg.h>

#if defined(__GNUC__) && !defined(WORDS_BIGENDIAN) && defined(MATCHED_TYPE)
#define NO_HOLES
#endif

#ifndef HAVE_MEMCPY
#define memcpy(d, s, n) bcopy ((s), (d), (n))
#endif /* not HAVE_MEMCPY */

#ifdef ICONV_REQUIRES_CONST
#define ICONV_CONST const
#else
#define ICONV_CONST
#endif

/* redefs of things that are either in glibc or we have to include them ourselves */
#if defined(WIN32) && !defined(__MWERKS__)
#define strcasecmp(s1,s2) stricmp(s1,s2)
#else
#if !defined(__GLIBC__) || (__GLIBC__ < 2) || (__GLIBC__ == 2 && __GLIBC_MINOR__ < 2)
#ifdef DEFINE_STRCASECMP
int strcasecmp (const char *s1, const char *s2);
#endif
#endif
#endif

#endif /* ! HAVE_WV_CONFIG_H */
])

AC_CONFIG_FILES([
	wvAbw
	wvDVI
	wvPS
	wvPDF
	wvHtml
	wvDocBook
	wvLatex
	wvCleanLatex
	wvText
	wvWml
	wv-1.0.pc
	version.c
	GNUmakefile
	magick/GNUmakefile
	glib-wv/GNUmakefile
	libole2/GNUmakefile
	oledecod/GNUmakefile
	expat/GNUmakefile
	expat/xmlparse/GNUmakefile
	expat/xmltok/GNUmakefile
	exporter/GNUmakefile
	xml/GNUmakefile
	help/GNUmakefile
	help/man/GNUmakefile
	patterns/GNUmakefile
	wingdingfont/GNUmakefile
])
AC_OUTPUT
