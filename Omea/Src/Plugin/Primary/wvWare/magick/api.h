/*
  ImageMagick Application Programming Interface declarations.

  This must not include any system system headers.  The header
  <stdio.h> must be included before this header in order to use the
  ImageMagick C API.
*/

#ifndef _MAGICK_API_H
#define _MAGICK_API_H

#if defined(__cplusplus) || defined(c_plusplus)
#define class  c_class
#endif

#if defined(WIN32) || defined(__CYGWIN__)
#define Export  __declspec(dllexport)
#if defined(_VISUALC_)
#pragma warning(disable : 4018)
#pragma warning(disable : 4244)
#pragma warning(disable : 4142)
#endif
#else
# define Export
#endif

#define MaxTextExtent  1664

#include "classify.h"
#include "image.h"
#include "blob.h"
#include "compress.h"
#include "utility.h"
#include "error.h"
#include "memory.h"
#include "quantize.h"

#endif
