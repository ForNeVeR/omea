#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutPGD (PGD * item, wvStream * fd)
{
    U16 temp16 = 0;

    temp16 |= item->fContinue;
    temp16 |= item->fUnk << 1;
    temp16 |= item->fRight << 2;
    temp16 |= item->fPgnRestart << 3;
    temp16 |= item->fEmptyPage << 4;
    temp16 |= item->fAllFtn << 5;
    temp16 |= item->fColOnly << 6;
    temp16 |= item->fTableBreaks << 7;
    temp16 |= item->fMarked << 8;
    temp16 |= item->fColumnBreaks << 9;
    temp16 |= item->fTableHeader << 10;
    temp16 |= item->fNewPage << 11;
    temp16 |= item->bkc << 12;

    write_16ubit (fd, temp16);

    write_16ubit (fd, (U16) item->lnn);
    write_16ubit (fd, item->pgn);

    /* only for WORD8 */
    write_32ubit (fd, (U32) item->dym);
}
