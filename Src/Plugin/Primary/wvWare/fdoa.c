#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetFDOA (FDOA * item, wvStream * fd)
{
    item->fc = (S32) read_32ubit (fd);
    item->ctxbx = (S16) read_16ubit (fd);
}


int
wvGetFDOA_PLCF (FDOA ** fdoa, U32 ** pos, U32 * nofdoa, U32 offset, U32 len,
		wvStream * fd)
{
    U32 i;
    if ((len == 0) || (offset == 0))
      {
	  *fdoa = NULL;
	  *pos = NULL;
	  *nofdoa = 0;
      }
    else
      {
	  *nofdoa = (len - 4) / (cbFDOA + 4);
	  *pos = (U32 *) wvMalloc ((*nofdoa + 1) * sizeof (U32));
	  if (*pos == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  (*nofdoa + 1) * sizeof (U32)));
		return (1);
	    }

	  *fdoa = (FDOA *) wvMalloc ((*nofdoa + 1) * sizeof (FDOA));
	  if (*fdoa == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  *nofdoa * sizeof (FDOA)));
		wvFree (pos);
		return (1);
	    }
	  wvStream_goto (fd, offset);
	  for (i = 0; i <= *nofdoa; i++)
	      (*pos)[i] = read_32ubit (fd);
	  for (i = 0; i < *nofdoa; i++)
	      wvGetFDOA (&((*fdoa)[i]), fd);
      }
    return (0);
}

FDOA *
wvGetFDOAFromCP (U32 currentcp, FDOA * fdoa, U32 * pos, U32 nofdoa)
{
    U32 i;
    wvTrace (("nofdoa is %d\n", nofdoa));
    for (i = 0; i < nofdoa; i++)
      {
	  wvTrace (("compare %x %x\n", currentcp, pos[i]));
	  if (pos[i] == currentcp)
	      return (&(fdoa[i]));
      }
    wvError (("found no fdoa, panic\n"));
    return (NULL);
}
