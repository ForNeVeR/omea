#include "magick.h"
#include "defines.h"

Export void MagickWarning(const unsigned int warning,const char *message,
const char *qualifier)
	{
	fprintf(stderr,"Magick Warning: %s %s\n",message,qualifier);
	}
