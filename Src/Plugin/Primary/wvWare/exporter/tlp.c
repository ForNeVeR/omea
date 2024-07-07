#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutTLP (TLP * item, wvStream * fd)
{
    U16 temp16 = 0;

    write_16ubit (fd, (U16) item->itl);

    temp16 |= item->fBorders;
    temp16 |= item->fShading << 1;
    temp16 |= item->fFont << 2;
    temp16 |= item->fColor << 3;
    temp16 |= item->fBestFit << 4;
    temp16 |= item->fHdrRows << 5;
    temp16 |= item->fLastRow << 6;
    temp16 |= item->fHdrCols << 7;
    temp16 |= item->fLastCol << 8;

    write_16ubit (fd, temp16);
}
