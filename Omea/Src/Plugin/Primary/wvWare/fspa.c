#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvInitFSPA (FSPA * item)
{
    item->spid = 0;
    item->xaLeft = 0;
    item->yaTop = 0;
    item->xaRight = 0;
    item->yaBottom = 0;
    item->fHdr = 0;
    item->bx = 0;
    item->by = 0;
    item->wr = 0;
    item->wrk = 0;
    item->fRcaSimple = 0;
    item->fBelowText = 0;
    item->fAnchorLock = 0;
    item->cTxbx = 0;
}

void
wvGetFSPA (FSPA * item, wvStream * fd)
{
    U16 temp16;
#ifdef PURIFY
    wvInitFSPA (item);
#endif
    item->spid = read_32ubit (fd);;
    item->xaLeft = (S32) read_32ubit (fd);
    item->yaTop = (S32) read_32ubit (fd);
    item->xaRight = (S32) read_32ubit (fd);
    item->yaBottom = (S32) read_32ubit (fd);
    temp16 = read_16ubit (fd);
    item->fHdr = temp16 & 0x0001;
    item->bx = (temp16 & 0x0006) >> 1;
    item->by = (temp16 & 0x0018) >> 3;
    item->wr = (temp16 & 0x01E0) >> 5;
    item->wrk = (temp16 & 0x1E00) >> 9;
    item->fRcaSimple = (temp16 & 0x2000) >> 13;
    item->fBelowText = (temp16 & 0x4000) >> 14;
    item->fAnchorLock = (temp16 & 0x8000) >> 15;
    item->cTxbx = (S32) read_32ubit (fd);
}


int
wvGetFSPA_PLCF (FSPA ** fspa, U32 ** pos, U32 * nofspa, U32 offset, U32 len,
		wvStream * fd)
{
    U32 i;
    if (len == 0)
      {
	  *fspa = NULL;
	  *pos = NULL;
	  *nofspa = 0;
      }
    else
      {
	  *nofspa = (len - 4) / 30;
	  *pos = (U32 *) wvMalloc ((*nofspa + 1) * sizeof (U32));
	  if (*pos == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  (*nofspa + 1) * sizeof (U32)));
		return (1);
	    }

	  *fspa = (FSPA *) wvMalloc (*nofspa * sizeof (FSPA));
	  if (*fspa == NULL)
	    {
		wvError (
			 ("NO MEM 1, failed to alloc %d bytes\n",
			  *nofspa * sizeof (FSPA)));

		/* I believe it is not always right to free this. Sometimes len == 4 and
		 * although *nofspa == 0, the data structure is needed.
		 * (Wild guesswork by MV 20.12.2000 -- correct me if I'm wrong)        */
		/*                      wvFree(pos); */
		return (1);
	    }
	  wvStream_goto (fd, offset);
	  for (i = 0; i <= *nofspa; i++)
	      (*pos)[i] = read_32ubit (fd);
	  for (i = 0; i < *nofspa; i++)
	      wvGetFSPA (&((*fspa)[i]), fd);
      }
    return (0);
}

FSPA *
wvGetFSPAFromCP (U32 currentcp, FSPA * fspa, U32 * pos, U32 nofspa)
{
    U32 i;
    wvTrace (("nofspa is %d\n", nofspa));
    for (i = 0; i < nofspa; i++)
      {
	  wvTrace (("compare %x %d\n", currentcp, pos[i]));
	  if (pos[i] == currentcp)
	      return (&(fspa[i]));
      }
    wvError (("found no fspa, panic\n"));
    return (NULL);
}
