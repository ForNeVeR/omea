#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutBKD (BKD * item, wvStream * fd)
{
    U16 temp16 = (U16) 0;

    write_16ubit (fd, (U16) item->ipgd_itxbxs);
    write_16ubit (fd, (U16) item->dcpDepend);

    temp16 |= item->icol;
    temp16 |= item->fTableBreak << 8;
    temp16 |= item->fColumnBreak << 9;
    temp16 |= item->fMarked << 10;
    temp16 |= item->fUnk << 11;
    temp16 |= item->fTextOverflow << 12;
    temp16 |= item->reserved1 << 13;

    write_16ubit (fd, temp16);
}
