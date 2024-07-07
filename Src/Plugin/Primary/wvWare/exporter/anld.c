/* TODO: do I need to code wvPutANLD_ToBucket()?? */

#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutANLD (wvVersion ver, ANLD * item, wvStream * fd)
{
    U8 temp8 = (U8) 0;
    int i = 0;

    write_8ubit (fd, item->nfc);
    write_8ubit (fd, item->cxchTextBefore);
    write_8ubit (fd, (U8) item->cxchTextAfter);

    temp8 |= item->jc;
    temp8 |= item->fPrev << 2;
    temp8 |= item->fHang << 3;
    temp8 |= item->fSetBold << 4;
    temp8 |= item->fSetItalic << 5;
    temp8 |= item->fSetSmallCaps << 6;
    temp8 |= item->fSetCaps << 7;
    write_8ubit (fd, temp8);

    temp8 = (U8) 0;		/* reset */
    temp8 |= item->fSetStrike;
    temp8 |= item->fSetKul << 1;
    temp8 |= item->fPrevSpace << 2;
    temp8 |= item->fBold << 3;
    temp8 |= item->fItalic << 4;
    temp8 |= item->fSmallCaps << 5;
    temp8 |= item->fCaps << 6;
    temp8 |= item->fStrike << 7;
    write_8ubit (fd, temp8);

    temp8 = (U8) 0;		/* reset */
    temp8 |= item->kul;
    temp8 |= item->ico << 3;
    write_8ubit (fd, temp8);

    write_16ubit (fd, (U16) item->ftc);
    write_16ubit (fd, item->iStartAt);
    write_16ubit (fd, (U16) item->dxaIndent);
    write_16ubit (fd, item->dxaSpace);
    write_8ubit (fd, item->fNumber1);
    write_8ubit (fd, item->fNumberAcross);
    write_8ubit (fd, item->fRestartHdn);
    write_8ubit (fd, item->fSpareX);

    for (i = 0; i < 32; i++)
      {
	  if (ver == WORD8)
	      write_16ubit (fd, item->rgxch[i]);
	  else
	      write_8ubit (fd, (U8) item->rgxch[i]);
      }
}
