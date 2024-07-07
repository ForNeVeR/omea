/*
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%                                                                             %
%                                                                             %
%                                                                             %
%           CCCC   OOO   M   M  PPPP   RRRR   EEEEE   SSSSS  SSSSS            %
%          C      O   O  MM MM  P   P  R   R  E       SS     SS               %
%          C      O   O  M M M  PPPP   RRRR   EEE      SSS    SSS             %
%          C      O   O  M   M  P      R R    E          SS     SS            %
%           CCCC   OOO   M   M  P      R  R   EEEEE   SSSSS  SSSSS            %
%                                                                             %
%                                                                             %
%                  Image Compression/Decompression Methods                    %
%                                                                             %
%                                                                             %
%                           Software Design                                   %
%                             John Cristy                                     %
%                              May  1993                                      %
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
%   S e t R u n l e n g t h P a c k e t s                                     %
%                                                                             %
%                                                                             %
%                                                                             %
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
%  Method SetRunlengthPackets initializes the runlength encoder.
%
%  The format of the SetRunlengthPackets function is:
%
%      SetRunlengthPackets(image,packets)
%
%  A description of each parameter follows:
%
%    o image: The address of a structure of type Image.
%
%    o packets: The number of runlength packets.
%
%
*/
Export void SetRunlengthPackets(Image *image,const unsigned long packets)
{
  image->packets=packets;
  image->pixels=(RunlengthPacket *) ReallocateMemory((char *) image->pixels,
    image->packets*sizeof(RunlengthPacket));
}

Export void SetRunlengthEncoder(RunlengthPacket *packet)
{
  packet->red=0;
  packet->green=0;
  packet->blue=0;
  packet->index=0;
  packet->length=MaxRunlength;
}

