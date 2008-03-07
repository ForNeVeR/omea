/*
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
%                         BBBB   L       OOO   BBBB                           %
%                         B   B  L      O   O  B   B                          %
%                         BBBB   L      O   O  BBBB                           %
%                         B   B  L      O   O  B   B                          %
%                         BBBB   LLLLL   OOO   BBBB                           %
%                                                                             %
%                                                                             %
%                    ImageMagick Binary Large OBjectS Methods                 %
%                                                                             %
%                                                                             %
%                              Software Design                                %
%                                John Cristy                                  %
%                                 July 1999                                   %
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
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
%   G e t B l o b I n f o                                                     %
%                                                                             %
%                                                                             %
%                                                                             %
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
%  Method GetBlobInfo initializes the BlobInfo structure.
%
%  The format of the GetBlobInfo method is:
%
%      void GetBlobInfo(BlobInfo *blob_info)
%
%  A description of each parameter follows:
%
%    o blob_info: Specifies a pointer to a BlobInfo structure.
%
%
*/
Export void GetBlobInfo(BlobInfo *blob_info)
{
  blob_info->data=(char *) NULL;
  blob_info->offset=0;
  blob_info->length=0;
  blob_info->extent=0;
  blob_info->quantum=BlobQuantum;
}


/*
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
+  R e a d B y t e                                                            %
%                                                                             %
%                                                                             %
%                                                                             %
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
%  Method ReadByte reads a single byte from the image file and returns it.
%
%  The format of the ReadByte method is:
%
%      int ReadByte(Image *image)
%
%  A description of each parameter follows.
%
%    o value:  Method ReadByte returns an integer read from the file.
%
%    o image: The address of a structure of type Image.
%
%
*/
Export int ReadByte(Image *image)
{
  int
    count;

  unsigned char
    value;

  count=ReadBlob(image,1,(char *) &value);
  if (count == 0)
    return(EOF);
  return(value);
}

/*
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
+  R e a d B l o b                                                            %
%                                                                             %
%                                                                             %
%                                                                             %
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
%  Method ReadBlob reads data from the blob or image file and returns it.  It
%  returns the number of bytes read.
%
%  The format of the ReadBlob method is:
%
%      unsigned long ReadBlob(Image *image,const unsigned long number_bytes,
%        char *data)
%
%  A description of each parameter follows:
%
%    o count:  Method ReadBlob returns the number of items read.
%
%    o image: The address of a structure of type Image.
%
%    o number_bytes:  Specifies an integer representing the number of bytes
%      to read from the file.
%
%    o data:  Specifies an area to place the information requested from
%      the file.
%
%
*/
Export unsigned long ReadBlob(Image *image,const unsigned long number_bytes,
  char *data)
{
  register int
    i;

  unsigned long
    count,
    offset;

  if (image->blob.data != (char *) NULL)
    {
      /*
        Read bytes from blob.
      */
      offset=Min(number_bytes,(unsigned long)
        (image->blob.length-image->blob.offset));
      if (number_bytes > 0)
        (void) memcpy(data,image->blob.data+image->blob.offset,offset);
      image->blob.offset+=offset;
      return(offset);
    }
  /*
    Read bytes from a file handle.
  */
  offset=0;
  for (i=number_bytes; i > 0; i-=count)
  {
    count=fread(data+offset,1,number_bytes,image->file);
    if (count <= 0)
      break;
    offset+=count;
  }
  return(offset);
}

Export unsigned int OpenBlob(const ImageInfo *image_info,Image *image,
  const char *type)
{
  char
    filename[MaxTextExtent];

  register char
    *p;

  if (image_info->blob.data != (char *) NULL)
    {
      image->blob=image_info->blob;
      return(True);
    }
  image->exempt=False;
  if (image_info->file != (FILE *) NULL)
    {
      /*
        Use previously opened filehandle.
      */
      image->file=image_info->file;
      image->exempt=True;
      return(True);
    }
  (void) strcpy(filename,image->filename);
  p=(char *) NULL;

  if (p != (char *) NULL)
    {
      (void) strcpy(filename,p);
      FreeMemory((char *) p);
    }
  /*
    Open image file.
  */
  image->pipe=False;
  if (strcmp(filename,"-") == 0)
    {
      image->file=(*type == 'r') ? stdin : stdout;
      image->exempt=True;
    }
  else
#if !defined(vms) && !defined(macintosh) && !defined(WIN32)
    if (*filename == '|')
      {
        char
          mode[MaxTextExtent];

        /*
          Pipe image to or from a system command.
        */
        if (*type == 'w')
          (void) signal(SIGPIPE,SIG_IGN);
        (void) strncpy(mode,type,1);
        mode[1]='\0';
        image->file=(FILE *) popen(filename+1,mode);
        image->pipe=True;
        image->exempt=True;
      }
    else
#endif
      {
        if (*type == 'w')
          {
            /*
              Form filename for multi-part images.
            */
            FormatString(filename,image->filename,image->scene);
            if (!image_info->adjoin)
              if ((image->previous != (Image *) NULL) ||
                  (image->next != (Image *) NULL))
                {
                  if ((strcmp(filename,image->filename) == 0) ||
                      (strchr(filename,'%') != (char *) NULL))
                    FormatString(filename,"%.1024s.%u",image->filename,
                      image->scene);
                  if (image->next != (Image *) NULL)
                    (void) strcpy(image->next->magick,image->magick);
                }
            (void) strcpy(image->filename,filename);
          }
#if defined(macintosh)
        if (*type == 'w')
          SetApplicationType(filename,image_info->magick,'8BIM');
#endif
        image->file=(FILE *) fopen(filename,type);
        if (image->file != (FILE *) NULL)
          {
            (void) SeekBlob(image,0L,SEEK_END);
            image->filesize=TellBlob(image);
            (void) SeekBlob(image,0L,SEEK_SET);
          }
      }
  image->status=False;
  if (*type == 'r')
    {
      image->next=(Image *) NULL;
      image->previous=(Image *) NULL;
    }
  return(image->file != (FILE *) NULL);
}


/*
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
+  L S B F i r s t R e a d S h o r t                                          %
%                                                                             %
%                                                                             %
%                                                                             %
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
%  Method LSBFirstReadShort reads a short value as a 16 bit quantity in
%  least-significant byte first order.
%
%  The format of the LSBFirstReadShort method is:
%
%      unsigned short LSBFirstReadShort(Image *image)
%
%  A description of each parameter follows.
%
%    o value:  Method LSBFirstReadShort returns an unsigned short read from
%      the file.
%
%    o image: The address of a structure of type Image.
%
%
*/
Export unsigned short LSBFirstReadShort(Image *image)
{
  unsigned char
    buffer[2];

  unsigned short
    value;

  value=ReadBlob(image,2,(char *) buffer);
  if (value == 0)
    return((unsigned short) ~0);
  value=(unsigned short) (buffer[1] << 8);
  value|=(unsigned short) (buffer[0]);
  return(value);
}

Export unsigned long LSBFirstReadLong(Image *image)
{
  unsigned char
    buffer[4];

  unsigned long
    value;

  value=ReadBlob(image,4,(char *) buffer);
  if (value == 0)
    return((unsigned long) ~0);
  value=(unsigned long) (buffer[3] << 24);
  value|=(unsigned long) (buffer[2] << 16);
  value|=(unsigned long) (buffer[1] << 8);
  value|=(unsigned long) (buffer[0]);
  return(value);
}

/*
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
+   C l o s e B l o b                                                         %
%                                                                             %
%                                                                             %
%                                                                             %
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
%  Method CloseBlob closes a file associated with the image.  If the
%  filename prefix is '|', the file is a pipe and is closed with PipeClose.
%
%  The format of the CloseBlob method is:
%
%      void CloseBlob(Image *image)
%
%  A description of each parameter follows:
%
%    o image: The address of a structure of type Image.
%
%
*/
Export void CloseBlob(Image *image)
{
  /*
    Close image file.
  */
  if (image->blob.data != (char *) NULL)
    {
      image->filesize=image->blob.length;
      image->blob.extent=image->blob.length;
      image->blob.data=(char *)
        ReallocateMemory(image->blob.data,image->blob.extent);
      return;
    }
  if (image->file == (FILE *) NULL)
    return;
  (void) FlushBlob(image);
  image->status=ferror(image->file);
  (void) SeekBlob(image,0L,SEEK_END);
  image->filesize=TellBlob(image);
#if !defined(vms) && !defined(macintosh) && !defined(WIN32)
  if (image->pipe)
    (void) pclose(image->file);
  else
#endif
    if (!image->exempt)
      (void) fclose(image->file);
  image->file=(FILE *) NULL;
  if (!image->orphan)
    {
      while (image->previous != (Image *) NULL)
        image=image->previous;
      for ( ; image != (Image *) NULL; image=image->next)
        image->file=(FILE *) NULL;
    }
  errno=0;
}

Export unsigned long LSBFirstWriteLong(Image *image,const unsigned long value)
{
  unsigned char
    buffer[4];

  assert(image != (Image *) NULL);
  buffer[0]=(unsigned char) (value);
  buffer[1]=(unsigned char) ((value) >> 8);
  buffer[2]=(unsigned char) ((value) >> 16);
  buffer[3]=(unsigned char) ((value) >> 24);
  return(WriteBlob(image,4,(char *) buffer));
}

Export unsigned long LSBFirstWriteShort(Image *image,const unsigned short value)
{
  unsigned char
    buffer[2];

  assert(image != (Image *) NULL);
  buffer[0]=(unsigned char) (value);
  buffer[1]=(unsigned char) ((value) >> 8);
  return(WriteBlob(image,2,(char *) buffer));
}

Export unsigned long WriteBlob(Image *image,const unsigned long number_bytes,
  const char *data)
{
  unsigned long
    count;

  assert(image != (Image *) NULL);
  assert(data != (const char *) NULL);
  if (image->blob.data == (char *) NULL)
    {
      count=(long) fwrite((char *) data,1,number_bytes,image->file);
      return(count);
    }
  if (number_bytes > (unsigned long) (image->blob.extent-image->blob.offset))
    {
      image->blob.extent+=number_bytes+image->blob.quantum;
      image->blob.data=(char *)
        ReallocateMemory(image->blob.data,image->blob.extent);
      if (image->blob.data == (char *) NULL)
        {
          image->blob.extent=0;
          return(0);
        }
    }
  memcpy(image->blob.data+image->blob.offset,data,number_bytes);
  image->blob.offset+=number_bytes;
  if (image->blob.offset > image->blob.length)
    image->blob.length=image->blob.offset;
  return(number_bytes);
}


Export int SeekBlob(Image *image,const long offset,const int whence)
{
  assert(image != (Image *) NULL);
  if (image->blob.data == (char *) NULL)
    return(fseek(image->file,offset,whence));
  switch(whence)
  {
    case SEEK_SET:
    default:
    {
      if (offset < 0)
        return(-1);
      if (offset >= image->blob.length)
        return(-1);
      image->blob.offset=offset;
      break;
    }
    case SEEK_CUR:
    {
      if ((image->blob.offset+offset) < 0)
        return(-1);
      if ((image->blob.offset+offset) >= (long) image->blob.length)
        return(-1);
      image->blob.offset+=offset;
      break;
    }
    case SEEK_END:
    {
      if ((image->blob.offset+image->blob.length+offset) < 0)
        return(-1);
      if ((image->blob.offset+image->blob.length+offset) >= image->blob.length)
        return(-1);
      image->blob.offset+=image->blob.length+offset;
      break;
    }
  }
  return(0);
}

Export int TellBlob(const Image *image)
{
  assert(image != (Image *) NULL);
  if (image->blob.data == (char *) NULL)
    return(ftell(image->file));
  return(image->blob.offset);
}

Export int FlushBlob(const Image *image)
{
  assert(image != (Image *) NULL);
  if (image->blob.data == (char *) NULL)
    return(fflush(image->file));
  return(0);
}

Export unsigned long MSBFirstWriteLong(Image *image,const unsigned long value)
{
  unsigned char
    buffer[4];

  assert(image != (Image *) NULL);
  buffer[0]=(unsigned char) ((value) >> 24);
  buffer[1]=(unsigned char) ((value) >> 16);
  buffer[2]=(unsigned char) ((value) >> 8);
  buffer[3]=(unsigned char) (value);
  return(WriteBlob(image,4,(char *) buffer));
}

Export unsigned long MSBFirstReadLong(Image *image)
{
  unsigned char
    buffer[4];

  unsigned long
    value;

  assert(image != (Image *) NULL);
  value=ReadBlob(image,4,(char *) buffer);
  if (value == 0)
    return((unsigned long) ~0);
  value=(unsigned int) (buffer[0] << 24);
  value|=(unsigned int) (buffer[1] << 16);
  value|=(unsigned int) (buffer[2] << 8);
  value|=(unsigned int) (buffer[3]);
  return(value);
}

