#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutSTSHI (STSHI * item, U16 cbSTSHI, wvStream * fd)
{
    U16 temp16 = 0, count = 0;
    int i;

    write_16ubit (fd, item->cstd);
    write_16ubit (fd, item->cbSTDBaseInFile);

    temp16 |= item->fStdStylenamesWritten;
    temp16 |= item->reserved << 1;
    write_16ubit (fd, temp16);

    write_16ubit (fd, (U16) item->stiMaxWhenSaved);
    write_16ubit (fd, item->istdMaxFixedWhenSaved);
    write_16ubit (fd, item->nVerBuiltInNamesWhenSaved);

    count = 12;			/* add */

    for (i = 0; i < 3; i++)
      {
	  write_16ubit (fd, item->rgftcStandardChpStsh[i]);
	  count += 2;
	  if (count >= cbSTSHI)
	      break;
      }

    while (count < cbSTSHI)
      {
	  count++;
	  write_8ubit (fd, 0);	/* write garbage */
      }
}

void
wvPutSTD (STD * item, U16 len, wvStream * fd)
{
    U16 temp16 = 0;
    U16 i, j;
    int pos;
    int ret = 0;
    U16 count = 0;

    temp16 |= item->sti;
    temp16 |= item->fScratch << 12;
    temp16 |= item->fInvalHeight << 13;
    temp16 |= item->fHasUpe << 14;
    temp16 |= item->fMassCopy << 15;
    write_16ubit (fd, temp16);

    temp16 = 0;
    temp16 |= item->sgc;
    temp16 |= item->istdBase << 4;
    write_16ubit (fd, temp16);

    temp16 = 0;
    temp16 |= item->cupx;
    temp16 |= item->istdNext << 4;
    write_16ubit (fd, temp16);

    write_16ubit (fd, (U16) item->bchUpe);

    count = 8;			/* add */
    pos = 11;
    ret = 1;

    write_8ubit (fd, (U8) len);

    for (i = 0; i < len; i++)
      {
	  /* if (count > 10) */
	  /* write_16ubit(fd, (U16)item->xstzName[i]) */
	  /* else */
	  write_8ubit (fd, (U8) item->xstzName[i]);
	  pos++;
      }

    for (i = 0; i < item->cupx; i++)
      {
	  if ((pos + 1) / 2 != pos / 2)
	    {
		/* eat odd bytes */
		wvStream_offset (fd, -1);	/* TODO: check me */
		pos++;
	    }

	  write_16ubit (fd, item->grupxf[i].cbUPX);
	  pos += 2;

	  if (item->grupxf[i].cbUPX == 0)
	      continue;

	  if ((item->cupx == 1) || ((item->cupx == 2) && (i == 1)))
	    {
		for (j = 0; j < item->grupxf[i].cbUPX; j++)
		  {
		      write_8ubit (fd, item->grupxf[i].upx.chpx.grpprl[j]);
		      pos++;
		  }
	    }
	  else if ((item->cupx == 2) && (i == 0))
	    {
		write_16ubit (fd, item->grupxf[i].upx.papx.istd);
		pos += 2;

		for (j = 0; j < item->grupxf[i].cbUPX - 2; j++)
		  {
		      write_8ubit (fd, item->grupxf[i].upx.papx.grpprl[j]);
		      pos++;
		  }
	    }
	  else
	    {
		/* something is FUBAR -- maybe try to handle it here someday */
		wvError (("Something FUBAR in wbPutSTD"));
	    }
      }

    /* eat odd bytes */
    if ((pos + 1) / 2 != pos / 2)	/* check me */
	wvStream_offset (fd, -1);
}

void
wvPutSTSH (STSH * item, U16 cbStshi, wvStream * fd)
{
    U16 i;

    write_16ubit (fd, cbStshi);
    wvPutSTSHI (&(item->Stshi), cbStshi, fd);

    if (item->Stshi.cstd == 0)
	return;

    if (item->std == NULL)
      {
	  wvError (("What the @#*@#*: item->std is null"));
	  return;
      }

    for (i = 0; i < item->Stshi.cstd; i++)
      {
	  write_16ubit (fd, i);	/* TODO: is this right?? i+1?? */
/* TODO *//* wvPutSTD(&(item->std[i]), fd); */
      }

    /* TODO: there must be a setting of styles */
    /* TODO: this probably is not finished */
}
