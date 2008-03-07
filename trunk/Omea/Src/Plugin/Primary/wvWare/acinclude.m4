dnl AC_PROG_CC
dnl AC_PROG_CC shoves in -g by default, i *really* hate this so here goes...
dnl ill shove in the autoconf def of AC_PROG_CC verbatim and remove the -g 
dnl stuff

AC_DEFUN(AC_PROG_CC_NO_G,
[AC_BEFORE([$0], [AC_PROG_CPP])dnl
AC_CHECK_PROG(CC, gcc, gcc)
if test -z "$CC"; then
  AC_CHECK_PROG(CC, cc, cc, , , /usr/ucb/cc)
  test -z "$CC" && AC_MSG_ERROR([no acceptable cc found in \$PATH])
fi

AC_PROG_CC_WORKS
AC_PROG_CC_GNU

dnl exec suffix for DOSish OSes, autoconf-2.13 required.

if test "$GCC" = yes; then
  GCC=yes
dnl Check whether -g works, even if CFLAGS is set, in case the package
dnl plays around with CFLAGS (such as to build both debugging and
dnl normal versions of a library), tasteless as that idea is.
  ac_test_CFLAGS="${CFLAGS+set}"
  ac_save_CFLAGS="$CFLAGS"
  CFLAGS=
dnl   C Begin
dnl AC_PROG_CC_G
dnl   C End
  if test "$ac_test_CFLAGS" = set; then
    CFLAGS="$ac_save_CFLAGS"
dnl   C Begin
dnl  elif test $ac_cv_prog_cc_g = yes; then
dnl    CFLAGS="-g -O2"
dnl   C End
  else
    CFLAGS="-O2"
  fi
else
  GCC=
  test "${CFLAGS+set}" = set 
dnl   C Begin
dnl || CFLAGS="-g"
dnl   C End
fi
])
