#include "magick/magick.h"
#include <string.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
int bmptopng (char *prefix);
#if 0
int
main (int argc, char **argv)
{
    return (bmptopng (argv[1]));
}
#endif

int
bmptopng (char *prefix)
{
    Image *image;
    ImageInfo image_info;

    char buffer[4096];

    GetImageInfo (&image_info);
    sprintf (buffer, "%s.bmp", prefix);
    strcpy (image_info.filename, buffer);
    image = ReadBMPImage (&image_info);
    if (image == (Image *) NULL)
	return (1);
    sprintf (buffer, "%s.png", prefix);

    strcpy (image_info.filename, buffer);
    SetImageInfo (&image_info, 1);
    strcpy (image->filename, buffer);
    WritePNGImage (&image_info, image);

    DestroyImage (image);
    return (0);
}
