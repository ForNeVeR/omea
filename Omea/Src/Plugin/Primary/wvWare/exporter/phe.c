#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutPHE6 (PHE * item, wvStream * fd)
{
    U8 temp8 = 0;

    temp8 |= item->var1.fSpare;
    temp8 |= item->var1.fUnk << 1;
    temp8 |= item->var1.fDiffLines << 2;
    temp8 |= item->var1.reserved1 << 3;

    write_8ubit (fd, temp8);

    write_8ubit (fd, (U8) item->var1.clMac);
    write_16ubit (fd, (U16) item->var1.dxaCol);
    write_16ubit (fd, (U16) item->var1.dymHeight);
}

void
wvPutPHE (PHE * item, wvStream * fd)
{
    U8 temp8 = 0;

    temp8 |= item->var1.fSpare;
    temp8 |= item->var1.fUnk << 1;
    temp8 |= item->var1.fDiffLines << 2;
    temp8 |= item->var1.reserved1 << 3;

    write_8ubit (fd, temp8);

    write_8ubit (fd, (U8) item->var1.clMac);
    write_16ubit (fd, (U16) item->var1.reserved2);
    write_32ubit (fd, (U32) item->var1.dxaCol);
    write_32ubit (fd, (U32) item->var1.dymHeight);
}
