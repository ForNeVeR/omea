#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvFreeXst (Xst ** xst)
{
    Xst *freegroup;
    if ((xst == NULL) || (*xst == NULL))
	return;

    while (*xst != NULL)
      {
	  freegroup = *xst;
	  *xst = (*xst)->next;
	  if (freegroup->u16string != NULL)
	      wvFree (freegroup->u16string);
	  wvFree (freegroup);
      }
}

void
wvGetXst (Xst ** xst, U32 offset, U32 len, wvStream * fd)
{
    U16 clen, i;
    U32 count = 0;
    Xst *authorlist;
    Xst *current = NULL;

    if ((len == 0) || (xst == NULL))
      {
	  *xst = NULL;
	  return;
      }

    wvStream_goto (fd, offset);
    *xst = (Xst *) wvMalloc (sizeof (Xst));
    authorlist = *xst;

    if (authorlist == NULL)
      {
	  wvError (("not enough mem for annotation group\n"));
	  return;
      }

    authorlist->next = NULL;
    authorlist->u16string = NULL;
    authorlist->noofstrings = 0;
    current = authorlist;

    while (count < len)
      {
	  clen = read_16ubit (fd);
	  count += 2;
	  current->u16string = (U16 *) wvMalloc ((clen + 1) * sizeof (U16));
	  authorlist->noofstrings++;
	  if (current->u16string == NULL)
	    {
		wvError (
			 ("not enough mem for author string of clen %d\n",
			  clen));
		break;
	    }
	  for (i = 0; i < clen; i++)
	    {
		current->u16string[i] = read_16ubit (fd);
		count += 2;
	    }
	  current->u16string[i] = '\0';

	  if (count < len)
	    {
		current->next = (Xst *) wvMalloc (sizeof (Xst));
		if (current->next == NULL)
		  {
		      wvError (("not enough mem for annotation group\n"));
		      break;
		  }
		current = current->next;
		current->next = NULL;
		current->u16string = NULL;
	    }
      }
}
