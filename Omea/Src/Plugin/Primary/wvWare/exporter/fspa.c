#include <stdio.h>
#include <stdlib.h>
#include "wvexporter-priv.h"

void
wvPutFSPA (FSPA * item, wvStream * fd)
{
    U16 temp16 = (U16) 0;

    write_32ubit (fd, item->spid);
    write_32ubit (fd, (U32) item->xaLeft);
    write_32ubit (fd, (U32) item->yaTop);
    write_32ubit (fd, (U32) item->xaRight);
    write_32ubit (fd, (U32) item->yaBottom);

    temp16 |= item->fHdr;
    temp16 |= item->bx << 1;
    temp16 |= item->by << 3;
    temp16 |= item->wr << 5;
    temp16 |= item->wrk << 9;
    temp16 |= item->fRcaSimple << 13;
    temp16 |= item->fBelowText << 14;
    temp16 |= item->fAnchorLock << 14;
    write_16ubit (fd, temp16);

    write_32ubit (fd, item->cTxbx);
}
