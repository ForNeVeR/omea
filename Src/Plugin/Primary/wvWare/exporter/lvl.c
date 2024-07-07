#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutLVL (LVL * item, wvStream * fd)
{
    int len;

    wvPutLVLF (&item->lvlf, fd);

    if (item->lvlf.cbGrpprlPapx > 0)
	wvStream_write (item->grpprlPapx, sizeof (U8), item->lvlf.cbGrpprlPapx,
			fd);

    if (item->lvlf.cbGrpprlChpx > 0)
	wvStream_write (item->grpprlChpx, sizeof (U8), item->lvlf.cbGrpprlChpx,
			fd);

    if (item->numbertext)
	len = item->numbertext[0];
    else
	len = 0;

    write_16ubit (fd, (U16) len);
}

void
wvPutLVLF (LVLF * item, wvStream * fd)
{
    U8 temp8;
    int i;

    write_32ubit (fd, item->iStartAt);
    write_8ubit (fd, (U8) item->nfc);

    temp8 = 0;
    temp8 |= item->jc;
    temp8 |= item->fLegal << 2;
    temp8 |= item->fNoRestart << 3;
    temp8 |= item->fPrev << 4;
    temp8 |= item->fPrevSpace << 5;
    temp8 |= item->fWord6 << 6;
    temp8 |= item->reserved1 << 7;
    write_8ubit (fd, temp8);

    for (i = 0; i < 9; i++)
	write_8ubit (fd, item->rgbxchNums[i]);

    write_8ubit (fd, item->ixchFollow);
    write_32ubit (fd, item->dxaSpace);
    write_32ubit (fd, item->dxaIndent);
    write_8ubit (fd, item->cbGrpprlChpx);
    write_8ubit (fd, item->cbGrpprlPapx);
    write_16ubit (fd, item->reserved2);
}
