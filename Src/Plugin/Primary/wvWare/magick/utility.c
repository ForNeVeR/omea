/*
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
%             U   U  TTTTT  IIIII  L      IIIII  TTTTT  Y   Y                 %
%             U   U    T      I    L        I      T     Y Y                  %
%             U   U    T      I    L        I      T      Y                   %
%             U   U    T      I    L        I      T      Y                   %
%              UUU     T    IIIII  LLLLL  IIIII    T      Y                   %
%                                                                             %
%                                                                             %
%                       ImageMagick Utility Methods                           %
%                                                                             %
%                                                                             %
%                             Software Design                                 %
%                               John Cristy                                   %
%                              January 1993                                   %
%                                                                             %
%                                                                             %
%  Copyright 1999 E. I. du Pont de Nemours and Company                        %
%                                                                             %
%  Permission is hereby granted, free of charge, to any person obtaining a    %
%  copy of this software and associated documentation files ("ImageMagick"),  %
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
#include <stdlib.h>
#ifdef _MSC_VER
#include <direct.h>
#endif


int ReadInteger(const char *p,char **q)
{
  int
    sign;

  register int
    value;

  value=0;
  sign=1;
  if (*p == '+')
    p++;
  else
    if (*p == '-')
      {
        p++;
        sign=(-1);
      }
  for ( ; (*p >= '0') && (*p <= '9'); p++)
    value=(value*10)+(*p-'0');
  *q=(char *) p;
  if (sign >= 0)
    return(value);
  return(-value);
}

/*
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
%  T e m p o r a r y F i l e n a m e                                          %
%                                                                             %
%                                                                             %
%                                                                             %
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
%  Method TemporaryFilename replaces the contents of the string pointed to
%  by filename by a unique file name.  Some delegates do not like % or .
%  in their filenames.
%
%  The format of the TemporaryFilename method is:
%
%      void TemporaryFilename(char *filename)
%
%  A description of each parameter follows.
%
%   o  filename:  Specifies a pointer to an array of characters.  The unique
%      file name is returned in this array.
%
%
*/
Export void TemporaryFilename(char *filename)
{
  register int
    i;

  *filename='\0';
  for (i=0; i < 256; i++)
  {
#if !defined(vms) && !defined(macintosh) && !defined(WIN32)
    register char
      *p;

    p=(char *) tempnam((char *) NULL,TemporaryTemplate);
    if (p != (char *) NULL)
      {
        (void) strcpy(filename,p);
        free((char *) p);
      }
#else
#if defined(WIN32)
    (void) getcwd(filename,MaxTextExtent >> 1);
    /*    (void) NTTemporaryFilename(filename); */
#else
#if defined(macintosh)
    (void) getcwd(filename,MaxTextExtent >> 1);
#endif
    (void) tmpnam(filename+strlen(filename));
#endif
#endif
    if ((strchr(filename,'%') == (char *) NULL) &&
        (strchr(filename,'.') == (char *) NULL))
      break;
  }
}

/*
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
%  P o s t s c r i p t G e o m e t r y                                        %
%                                                                             %
%                                                                             %
%                                                                             %
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
%  Method PostscriptGeometry replaces any page mneumonic with the equivalent
%  size in picas.
%
%  The format of the PostscriptGeometry method is:
%
%      void DestroyPostscriptGeometry(char *geometry)
%
%  A description of each parameter follows.
%
%   o  page:  Specifies a pointer to an array of characters.  The string is
%      either a Postscript page name (e.g. A4) or a postscript page geometry
%      (e.g. 612x792+36+36).
%
%
*/

Export void DestroyPostscriptGeometry(char *geometry)
{
    FreeMemory(geometry);
}

Export char *PostscriptGeometry(const char *page)
{
  static const char
    *PageSizes[][2]=
    {
      { "10x13",  "720x936>" },
      { "10x14",  "720x1008>" },
      { "11x17",  "792x1224>" },
      { "7x9",  "504x648>" },
      { "9x11",  "648x792>" },
      { "9x12",  "648x864>" },
      { "A0",  "2384x3370>" },
      { "A1",  "1684x2384>" },
      { "A10", "73x105>" },
      { "A2",  "1191x1684>" },
      { "A3",  "842x1191>" },
      { "A4",  "595x842>" },
      { "A4SMALL", "595x842>" },
      { "A5",  "420x595>" },
      { "A6",  "297x420>" },
      { "A7",  "210x297>" },
      { "A8",  "148x210>" },
      { "A9",  "105x148>" },
      { "ARCHA", "648x864>" },
      { "ARCHB", "864x1296>" },
      { "ARCHC", "1296x1728>" },
      { "ARCHD", "1728x2592>" },
      { "ARCHE", "2592x3456>" },
      { "B0",  "2920x4127>" },
      { "B1",  "2064x2920>" },
      { "B10", "91x127>" },
      { "B2",  "1460x2064>" },
      { "B3",  "1032x1460>" },
      { "B4",  "729x1032>" },
      { "B5",  "516x729>" },
      { "B6",  "363x516>" },
      { "B7",  "258x363>" },
      { "B8",  "181x258>" },
      { "B9",  "127x181>" },
      { "C0",  "2599x3676>" },
      { "C1",  "1837x2599>" },
      { "C2",  "1298x1837>" },
      { "C3",  "918x1296>" },
      { "C4",  "649x918>" },
      { "C5",  "459x649>" },
      { "C6",  "323x459>" },
      { "C7",  "230x323>" },
      { "EXECUTIVE", "540x720>" },
      { "FLSA", "612x936>" },
      { "FLSE", "612x936>" },
      { "FOLIO",  "612x936>" },
      { "HALFLETTER", "396x612>" },
      { "ISOB0", "2835x4008>" },
      { "ISOB1", "2004x2835>" },
      { "ISOB10", "88x125>" },
      { "ISOB2", "1417x2004>" },
      { "ISOB3", "1001x1417>" },
      { "ISOB4", "709x1001>" },
      { "ISOB5", "499x709>" },
      { "ISOB6", "354x499>" },
      { "ISOB7", "249x354>" },
      { "ISOB8", "176x249>" },
      { "ISOB9", "125x176>" },
      { "LEDGER",  "1224x792>" },
      { "LEGAL",  "612x1008>" },
      { "LETTER", "612x792>" },
      { "LETTERSMALL",  "612x792>" },
      { "QUARTO",  "610x780>" },
      { "STATEMENT",  "396x612>" },
      { "TABLOID",  "792x1224>" },
      { (char *) NULL, (char *) NULL }
    };

  char
    c,
    *geometry;

  register char
    *p;


  register int
    i;

  /*
    Allocate page geometry memory.
  */
  geometry=(char *) AllocateMemory((Extent(page)+MaxTextExtent)*sizeof(char));
  if (geometry == (char *) NULL)
    {
      MagickWarning(ResourceLimitWarning,"Unable to translate page geometry",
        "Memory allocation failed");
      return((char *) NULL);
    }
  *geometry='\0';
  if (page == (char *) NULL)
    return(geometry);
  /*
    Comparison is case insensitive.
  */
  (void) strcpy(geometry,page);
  if (!isdigit((int) (*geometry)))
    for (p=geometry; *p != '\0'; p++)
    {
      c=(*p);
      if (islower((int) c))
        *p=toupper(c);
    }
  /*
    Comparison is case insensitive.
  */
  for (i=0; *PageSizes[i] != (char *) NULL; i++)
    if (strncmp(PageSizes[i][0],geometry,Extent(PageSizes[i][0])) == 0)
      {
        /*
          Replace mneumonic with the equivalent size in dots-per-inch.
        */
        (void) strcpy(geometry,PageSizes[i][1]);
        (void) strcat(geometry,page+Extent(PageSizes[i][0]));
        break;
      }
  return(geometry);
}

Export int ParseGeometry(const char *geometry,int *x,int *y,unsigned int *width,
  unsigned int *height)
{
  char
    *q;

  int
    mask;

  RectangleInfo
    bounds;

  mask=NoValue;
  if ((geometry == (const char *) NULL) || (*geometry == '\0'))
    return(mask);
  if (*geometry == '=')
    geometry++;
  if ((*geometry != '+') && (*geometry != '-') && (*geometry != 'x'))
    {
      /*
        Parse width.
      */
      bounds.width=ReadInteger(geometry,&q);
      if (geometry == q)
        return(0);
      geometry=q;
      mask|=WidthValue;
    }
  if ((*geometry == 'x') || (*geometry == 'X'))
    {
      /*
        Parse height.
      */
      geometry++;
      bounds.height=ReadInteger(geometry,&q);
      if (geometry == q)
        return(0);
      geometry=q;
      mask|=HeightValue;
    }
  if ((*geometry == '+') || (*geometry == '-'))
    {
      /*
        Parse x value.
      */
      if (*geometry == '-')
        {
          geometry++;
          bounds.x=(-ReadInteger(geometry,&q));
          if (geometry == q)
            return (0);
          geometry=q;
          mask|=XNegative;
        }
      else
        {
          geometry++;
          bounds.x=ReadInteger(geometry,&q);
          if (geometry == q)
            return(0);
          geometry=q;
        }
      mask|=XValue;
      if ((*geometry == '+') || (*geometry == '-'))
        {
          /*
            Parse y value.
          */
          if (*geometry == '-')
            {
              geometry++;
              bounds.y=(-ReadInteger(geometry,&q));
              if (geometry == q)
                return(0);
              geometry=q;
              mask|=YNegative;
            }
          else
            {
              geometry++;
              bounds.y=ReadInteger(geometry,&q);
              if (geometry == q)
                return(0);
              geometry=q;
            }
          mask|=YValue;
        }
    }
  if (*geometry != '\0')
    return(0);
  if (mask & XValue)
    *x=bounds.x;
  if (mask & YValue)
    *y=bounds.y;
  if (mask & WidthValue)
    *width=bounds.width;
  if (mask & HeightValue)
    *height=bounds.height;
  return (mask);
}


Export void FormatString(char *string,const char *format,...)
{
  va_list
    operands;

  va_start(operands,format);
#if !defined(HAVE_VSNPRINTF)
  (void) vsprintf(string,format,operands);
#else
  (void) vsnprintf(string,MaxTextExtent,format,operands);
#endif
  va_end(operands);
}

Export unsigned int CloneString(char **destination,const char *source)
{
  assert(destination != (char **) NULL);
  if (*destination != (char *) NULL)
    FreeMemory(*destination);
  *destination=(char *) NULL;
  if (source == (const char *) NULL)
    return(True);
  *destination=(char *)
    AllocateMemory(Max(Extent(source)+1,MaxTextExtent)*sizeof(char));
  if (*destination == (char *) NULL)
    {
      MagickWarning(ResourceLimitWarning,"Unable to allocate string",
        "Memory allocation failed");
      return(False);
    }
  (void) strcpy(*destination,source);
  return(True);
}

Export char *AllocateString(const char *source)
{
  char
    *destination;

  if (source == (char *) NULL)
    return((char *) NULL);
  destination=(char *)
    AllocateMemory(Max(Extent(source)+1,MaxTextExtent)*sizeof(char));
  if (destination == (char *) NULL)
    {
      MagickWarning(ResourceLimitWarning,"Unable to allocate string",
        "Memory allocation failed");
      return((char *) NULL);
    }
  (void) strcpy(destination,source);
  return(destination);
 }

