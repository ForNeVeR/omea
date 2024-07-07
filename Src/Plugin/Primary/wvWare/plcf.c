#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

/*
   this function retrieves a generic PLCF; this is useful since it
   means we do not have to add specific functions for retrieving
   various simple PLCFs (for instance the PLCFs that store information
   about footnotes, endnotes and annotations are just simple arrays of
   U32s)

   plcf - a pointer to a pointer where we should allocate the structure
          the caller needs to free these using wvFree() once not needed
          
   offset - an offset in the stream fd where the PLCF starts
   len - a length in bytes (!!!) of the PLCF
   fd - the stream from which to read the PLCF
*/
int
wvGetPLCF (void ** plcf, U32 offset, U32 len, wvStream * fd)
{
    U32 i, i32, i8;
	
    if (len == 0)
	{
		*plcf = NULL;
	}
    else
	{
		*plcf = wvMalloc (len);
		if (*plcf == NULL)
	    {
			wvError (("NO MEM 1, failed to alloc %d bytes\n",len));
			return (1);
	    }
		
		wvStream_goto (fd, offset);
		
		i32 = len / 4;
		i8  = len % 4;
		
		for (i = 0; i < i32; i++)
			((U32*)(*plcf))[i] = read_32ubit (fd);

		for (i = i32*4; i < i32*4 + i8; i++)
			((U8*)(*plcf))[i] = read_8ubit (fd);
	}
    return (0);
}
