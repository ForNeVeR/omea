#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetFRD (FRD * item, wvStream * fd)
{
    item->frd = (S16) read_16ubit (fd);
}

int
wvGetFRD_PLCF (FRD ** frd, U32 ** pos, U32 * nofrd, U32 offset, U32 len,
	       wvStream * fd)
{
    U32 i;
    if (len == 0)
      {
	  *frd = NULL;
	  *pos = NULL;
	  *nofrd = 0;
      }
    else
      {
	  *nofrd = (len - 4) / 6;
	  *pos = (U32 *) wvMalloc ((*nofrd + 1) * sizeof (U32));
	  if (*pos == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  (*nofrd + 1) * sizeof (U32)));
		return (1);
	    }

	  *frd = (FRD *) wvMalloc (*nofrd * sizeof (FRD));
	  if (*frd == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  *nofrd * sizeof (FRD)));
		wvFree (pos);
		return (1);
	    }
	  wvStream_goto (fd, offset);
	  for (i = 0; i <= *nofrd; i++)
	      (*pos)[i] = read_32ubit (fd);
	  for (i = 0; i < *nofrd; i++)
	      wvGetFRD (&((*frd)[i]), fd);
      }
    return (0);
}
