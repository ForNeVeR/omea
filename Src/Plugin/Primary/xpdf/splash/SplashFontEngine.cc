//========================================================================
//
// SplashFontEngine.cc
//
//========================================================================

#include <aconf.h>

#ifdef USE_GCC_PRAGMAS
#pragma implementation
#endif

#if HAVE_T1LIB_H
#include <t1lib.h>
#endif

#include <stdlib.h>
#include <stdio.h>
#ifndef WIN32
#  include <unistd.h>
#endif
#include "gmem.h"
#include "GString.h"
#include "SplashT1FontEngine.h"
#include "SplashFTFontEngine.h"
#include "SplashFontFile.h"
#include "SplashFontFileID.h"
#include "SplashFont.h"
#include "SplashFontEngine.h"

#ifdef VMS
#if (__VMS_VER < 70000000)
extern "C" int unlink(char *filename);
#endif
#endif

//------------------------------------------------------------------------
// SplashFontEngine
//------------------------------------------------------------------------

SplashFontEngine::SplashFontEngine(
#if HAVE_T1LIB_H
				   GBool enableT1lib,
#endif
#if HAVE_FREETYPE_FREETYPE_H || HAVE_FREETYPE_H
				   GBool enableFreeType,
#endif
				   GBool aa) {
  int i;

  for (i = 0; i < splashFontCacheSize; ++i) {
    fontCache[i] = NULL;
  }

#if HAVE_T1LIB_H
  if (enableT1lib) {
    t1Engine = SplashT1FontEngine::init(aa);
  } else {
    t1Engine = NULL;
  }
#endif
#if HAVE_FREETYPE_FREETYPE_H || HAVE_FREETYPE_H
  if (enableFreeType) {
    ftEngine = SplashFTFontEngine::init(aa);
  } else {
    ftEngine = NULL;
  }
#endif
}

SplashFontEngine::~SplashFontEngine() {
  int i;

  for (i = 0; i < splashFontCacheSize; ++i) {
    if (fontCache[i]) {
      delete fontCache[i];
    }
  }

#if HAVE_T1LIB_H
  if (t1Engine) {
    delete t1Engine;
  }
#endif
#if HAVE_FREETYPE_FREETYPE_H || HAVE_FREETYPE_H
  if (ftEngine) {
    delete ftEngine;
  }
#endif
}

SplashFontFile *SplashFontEngine::getFontFile(SplashFontFileID *id) {
  SplashFontFile *fontFile;
  int i;

  for (i = 0; i < splashFontCacheSize; ++i) {
    if (fontCache[i]) {
      fontFile = fontCache[i]->getFontFile();
      if (fontFile && fontFile->getID()->matches(id)) {
	return fontFile;
      }
    }
  }
  return NULL;
}

SplashFontFile *SplashFontEngine::loadType1Font(SplashFontFileID *idA,
						char *fileName,
						GBool deleteFile, char **enc) {
  SplashFontFile *fontFile;

  fontFile = NULL;
#if HAVE_T1LIB_H
  if (!fontFile && t1Engine) {
    fontFile = t1Engine->loadType1Font(idA, fileName, deleteFile, enc);
  }
#endif
#if HAVE_FREETYPE_FREETYPE_H || HAVE_FREETYPE_H
  if (!fontFile && ftEngine) {
    fontFile = ftEngine->loadType1Font(idA, fileName, deleteFile, enc);
  }
#endif

  // delete the (temporary) font file -- with Unix hard link
  // semantics, this will remove the last link; otherwise it will
  // return an error, leaving the file to be deleted later (if
  // loadXYZFont failed, the file will always be deleted)
  if (deleteFile) {
    unlink(fontFile ? fontFile->fileName->getCString() : fileName);
  }

  return fontFile;
}

SplashFontFile *SplashFontEngine::loadType1CFont(SplashFontFileID *idA,
						 char *fileName,
						 GBool deleteFile,
						 char **enc) {
  SplashFontFile *fontFile;

  fontFile = NULL;
#if HAVE_T1LIB_H
  if (!fontFile && t1Engine) {
    fontFile = t1Engine->loadType1CFont(idA, fileName, deleteFile, enc);
  }
#endif
#if HAVE_FREETYPE_FREETYPE_H || HAVE_FREETYPE_H
  if (!fontFile && ftEngine) {
    fontFile = ftEngine->loadType1CFont(idA, fileName, deleteFile, enc);
  }
#endif

  // delete the (temporary) font file -- with Unix hard link
  // semantics, this will remove the last link; otherwise it will
  // return an error, leaving the file to be deleted later (if
  // loadXYZFont failed, the file will always be deleted)
  if (deleteFile) {
    unlink(fontFile ? fontFile->fileName->getCString() : fileName);
  }

  return fontFile;
}

SplashFontFile *SplashFontEngine::loadCIDFont(SplashFontFileID *idA,
					      char *fileName,
					      GBool deleteFile) {
  SplashFontFile *fontFile;

  fontFile = NULL;
#if HAVE_FREETYPE_FREETYPE_H || HAVE_FREETYPE_H
  if (!fontFile && ftEngine) {
    fontFile = ftEngine->loadCIDFont(idA, fileName, deleteFile);
  }
#endif

  // delete the (temporary) font file -- with Unix hard link
  // semantics, this will remove the last link; otherwise it will
  // return an error, leaving the file to be deleted later (if
  // loadXYZFont failed, the file will always be deleted)
  if (deleteFile) {
    unlink(fontFile ? fontFile->fileName->getCString() : fileName);
  }

  return fontFile;
}

SplashFontFile *SplashFontEngine::loadTrueTypeFont(SplashFontFileID *idA,
						   char *fileName,
						   GBool deleteFile,
						   Gushort *codeToGID,
						   int codeToGIDLen) {
  SplashFontFile *fontFile;

  fontFile = NULL;
#if HAVE_FREETYPE_FREETYPE_H || HAVE_FREETYPE_H
  if (!fontFile && ftEngine) {
    fontFile = ftEngine->loadTrueTypeFont(idA, fileName, deleteFile,
					  codeToGID, codeToGIDLen);
  }
#endif

  if (!fontFile) {
    gfree(codeToGID);
  }

  // delete the (temporary) font file -- with Unix hard link
  // semantics, this will remove the last link; otherwise it will
  // return an error, leaving the file to be deleted later (if
  // loadXYZFont failed, the file will always be deleted)
  if (deleteFile) {
    unlink(fontFile ? fontFile->fileName->getCString() : fileName);
  }

  return fontFile;
}

SplashFont *SplashFontEngine::getFont(SplashFontFile *fontFile,
				      SplashCoord *mat) {
  SplashFont *font;
  int i, j;

  font = fontCache[0];
  if (font && font->matches(fontFile, mat)) {
    return font;
  }
  for (i = 1; i < splashFontCacheSize; ++i) {
    font = fontCache[i];
    if (font && font->matches(fontFile, mat)) {
      for (j = i; j > 0; --j) {
	fontCache[j] = fontCache[j-1];
      }
      fontCache[0] = font;
      return font;
    }
  }
  font = fontFile->makeFont(mat);
  if (fontCache[splashFontCacheSize - 1]) {
    delete fontCache[splashFontCacheSize - 1];
  }
  for (j = splashFontCacheSize - 1; j > 0; --j) {
    fontCache[j] = fontCache[j-1];
  }
  fontCache[0] = font;
  return font;
}
