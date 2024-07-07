/*
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
%                  M   M   AAA    GGGG  IIIII   CCCC  K   K                   %
%                  MM MM  A   A  G        I    C      K  K                    %
%                  M M M  AAAAA  G GGG    I    C      KKK                     %
%                  M   M  A   A  G   G    I    C      K  K                    %
%                  M   M  A   A   GGGG  IIIII   CCCC  K   K                   %
%                                                                             %
%                                                                             %
%               Methods to Read or List ImageMagick Image formats             %
%                                                                             %
%                                                                             %
%                            Software Design                                  %
%                            Bob Friesenhahn                                  %
%                              John Cristy                                    %
%                             November 1998                                   %
%                                                                             %
%                                                                             %
%  Copyright 1999 E. I. du Pont de Nemours and Company                        %
%                                                                             %
%  Permission is hereby granted, free of charge, to any person obtaining a    %
%  copy of this software and associated documentations ("ImageMagick"),       %
%  to deal in ImageMagick without restriction, including without limitation   %
%  the rights to use, copy, modify, merge, publish, distribute, sublicense,   %
%  and/or sell copies of ImageMagick, and to permit persons to whom the       %
%  ImageMagick is furnished to do so, subject to the following conditions:    %
%                                                                             %
%  The above copyright notice and this permission notice shall be included in %
%  all copies or substantial portions of ImageMagick.                         %
%                                                                             %
%  The software is provided "as is", without warranty of any kind, express or %
%  implied, including but not limited to the warranties of merchantability,   %
%  fitness for a particular purpose and noninfringement.  In no event shall   %
%  E. I. du Pont de Nemours and Company be liable for any claim, damages or   %
%  other liability, whether in an action of contract, tort or otherwise,      %
%  arising from, out of or in connection with ImageMagick or the use or other %
%  dealings in ImageMagick.                                                   %
%                                                                             %
%  Except as contained in this notice, the name of the E. I. du Pont de       %
%  Nemours and Company shall not be used in advertising or otherwise to       %
%  promote the sale, use or other dealings in ImageMagick without prior       %
%  written authorization from the E. I. du Pont de Nemours and Company.       %
%                                                                             %
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
%
*/

/*
  Include declarations.
*/
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "magick.h"
#include "defines.h"

/*
  Global declarations.
*/
static MagickInfo
  *magick_info = (MagickInfo *) NULL;

static unsigned int IsBMP(const unsigned char *magick,unsigned int length)
{
  if (length < 2)
    return(False);
  if (strncmp((char *) magick,"BM",2) == 0)
    return(True);
  if (strncmp((char *) magick,"IC",2) == 0)
    return(True);
  if (strncmp((char *) magick,"PI",2) == 0)
    return(True);
  if (strncmp((char *) magick,"CI",2) == 0)
    return(True);
  if (strncmp((char *) magick,"CP",2) == 0)
    return(True);
  return(False);
}

static unsigned int IsPNG(const unsigned char *magick,unsigned int length)
{
  if (length < 8)
    return(False);
  if (strncmp((char *) magick,"\211PNG\r\n\032\n",8) == 0)
    return(True);
  return(False);
}

  
Export MagickInfo *GetMagickInfo(const char *tag)
{
  register MagickInfo
    *p;

  if (magick_info == (MagickInfo *) NULL)
    {
	(void) RegisterMagickInfo("BMP",ReadBMPImage,WriteBMPImage,IsBMP,True,
        True,"Microsoft Windows bitmap image");
      (void) RegisterMagickInfo("BMP24",ReadBMPImage,WriteBMPImage,
        (unsigned int (*)(const unsigned char *,const unsigned int)) NULL,
        True,True,"Microsoft Windows 24-bit bitmap image");
#if defined(HasPNG)
      (void) RegisterMagickInfo("PNG",ReadPNGImage,WritePNGImage,IsPNG,False,
        True,"Portable Network Graphics");
#endif
	}
	  if (tag == (char *) NULL)
    return(magick_info);
  for (p=magick_info; p != (MagickInfo *) NULL; p=p->next)
    if (strcmp(tag,p->tag) == 0)
      return(p);
  return((MagickInfo *) NULL);
}



Export MagickInfo *RegisterMagickInfo(const char *tag,
  Image *(*decoder)(const ImageInfo *),
  unsigned int (*encoder)(const ImageInfo *,Image *),
  unsigned int (*magick)(const unsigned char *,const unsigned int),
  const unsigned int adjoin,const unsigned int blob_support,
  const char *description)
{
  MagickInfo
    *entry;

  register MagickInfo
    *p;

  /*
    Add tag info to the end of the image format list.
  */
  entry=(MagickInfo *) AllocateMemory(sizeof(MagickInfo));
  if (entry == (MagickInfo *) NULL)
    fprintf(stderr,"ResourceLimitWarning: Unable to allocate image\nMemory allocation failed");
  entry->tag=AllocateString(tag);
  entry->decoder=decoder;
  entry->encoder=encoder;
  entry->magick=magick;
  entry->adjoin=adjoin;
  entry->blob_support=blob_support;
  entry->description=AllocateString(description);
  entry->data=(void *) NULL;
  entry->previous=(MagickInfo *) NULL;
  entry->next=(MagickInfo *) NULL;
  if (magick_info == (MagickInfo *) NULL)
    {
      magick_info=entry;
      return(entry);
    }
  for (p=magick_info; p->next != (MagickInfo *) NULL; p=p->next);
  p->next=entry;
  entry->previous=p;
  return(entry);
}
