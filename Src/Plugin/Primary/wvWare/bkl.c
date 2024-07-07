#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetBKL (BKL * item, wvStream * fd)
{
    item->ibkf = (S16) read_16ubit (fd);
}

int
wvGetBKL_PLCF (BKL ** bkl, U32 ** pos, U32 * nobkl, U32 bkloffset, U32 bkllen, U32 bkfoffset,U32 bkflen,
	       wvStream * fd)
{
    BKF *bkf;
    U32 *posf,nobkf;

    U32 i, j;
    if (bkllen == 0 || bkflen == 0)
      {
	  *bkl = NULL;
	  *pos = NULL;
	  *nobkl = 0;
      }
    else
      {
      /* the plcbkl table contains offsets only, bkl has to be calculated from bkf */
      *nobkl = (bkllen - 4) / (/*cbBKL +*/ 4);
	  *pos = (U32 *) wvMalloc ((*nobkl + 1) * sizeof (U32));
	  if (*pos == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  (*nobkl + 1) * sizeof (U32)));
		return (1);
	    }

	  *nobkl = (*nobkl ? *nobkl : 1);
	  *bkl = (BKL *) wvMalloc (*nobkl * sizeof (BKL));
	  if (*bkl == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  *nobkl * sizeof (BKL)));
        wvFree (*pos);
		return (1);
	    }
      wvStream_goto (fd, bkloffset);
	  for (i = 0; i <= *nobkl; i++)
	      (*pos)[i] = read_32ubit (fd);

      /* now we have to reconstruct the bkl table; we have to get the bkf records,
         and then search them to find one that matches the index we are processing
      */

      if(wvGetBKF_PLCF (&bkf, &posf, &nobkf, bkfoffset, bkflen, fd))
        {
            wvError (
             ("call to wvGetBKF_PLCF failed\n"));
            wvFree (*pos);
            wvFree (*bkl);
            return (1);
        }

	  for (i = 0; i < *nobkl; i++)
      {
          for(j = 0; j < nobkf; j++)
              if(bkf[j].ibkl == i)
                  break;

          if(j == nobkf)
          {
              wvError (
                 ("unmatched closing bookmark\n"));
              wvFree (*pos);
              wvFree (*bkl);
              wvFree (bkf);
              wvFree (posf);
              return (1);
      }

          (*bkl)[i].ibkf = (U16)j;
      }

      wvFree(bkf);
      wvFree(posf);
      }

    return (0);
}
