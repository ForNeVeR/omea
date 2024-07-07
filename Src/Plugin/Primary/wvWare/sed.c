#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"


void
wvGetSED (SED * item, wvStream * fd)
{
    item->fn = (S16) read_16ubit (fd);
    item->fcSepx = read_32ubit (fd);
    item->fnMpr = (S16) read_16ubit (fd);
    item->fcMpr = read_32ubit (fd);
}

int
wvGetSED_PLCF (SED ** item, U32 ** pos, U32 * noitem, U32 offset, U32 len,
	       wvStream * fd)
{
    U32 i;
    if (len == 0)
      {
	  *item = NULL;
	  *pos = NULL;
	  *noitem = 0;
      }
    else
      {
	  *noitem = (len - 4) / (cbSED + 4);
	  *pos = (U32 *) wvMalloc ((*noitem + 1) * sizeof (U32));
	  if (*pos == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  (*noitem + 1) * sizeof (U32)));
		return (1);
	    }

	  *item = (SED *) wvMalloc (*noitem * sizeof (SED));
	  if (*item == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  *noitem * sizeof (SED)));
		wvFree (*pos);
		return (1);
	    }
	  wvStream_goto (fd, offset);
	  for (i = 0; i <= *noitem; i++)
	      (*pos)[i] = read_32ubit (fd);
	  for (i = 0; i < *noitem; i++)
	      wvGetSED (&((*item)[i]), fd);
      }
    return (0);
}
