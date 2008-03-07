#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetBKF (BKF * item, wvStream * fd)
{
    U16 temp16;
#ifdef PURIFY
    wvInitBKF (item);
#endif
    item->ibkl = (S16) read_16ubit (fd);
    temp16 = read_16ubit (fd);
    item->itcFirst = temp16 & 0x007F;
    item->fPub = (temp16 & 0x0080) >> 7;
    item->itcLim = (temp16 & 0x7F00) >> 8;
    item->fCol = (temp16 & 0x8000) >> 15;
}

void
wvInitBKF (BKF * item)
{
    item->ibkl = 0;
    item->itcFirst = 0;
    item->fPub = 0;
    item->itcLim = 0;
    item->fCol = 0;
}

int
wvGetBKF_PLCF (BKF ** bkf, U32 ** pos, U32 * nobkf, U32 offset, U32 len,
	       wvStream * fd)
{
    U32 i;
    if (len == 0)
      {
	  *bkf = NULL;
	  *pos = NULL;
	  *nobkf = 0;
      }
    else
      {
	  *nobkf = (len - 4) / (cbBKF + 4);
	  *pos = (U32 *) wvMalloc ((*nobkf + 1) * sizeof (U32));
	  if (*pos == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  (*nobkf + 1) * sizeof (U32)));
		return (1);
	    }

	  *bkf = (BKF *) wvMalloc (*nobkf * sizeof (BKF));
	  if (*bkf == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  *nobkf * sizeof (BKF)));
        wvFree (*pos);
		return (1);
	    }
	  wvStream_goto (fd, offset);
	  for (i = 0; i <= *nobkf; i++)
	      (*pos)[i] = read_32ubit (fd);
	  for (i = 0; i < *nobkf; i++)
	      wvGetBKF (&((*bkf)[i]), fd);
      }
    return (0);
}
