#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutTC (TC * item, wvStream * fd)
{
    U16 temp16 = 0;

    /* assumes word8 */
    temp16 |= item->fFirstMerged;
    temp16 |= item->fMerged << 1;
    temp16 |= item->fVertical << 2;
    temp16 |= item->fBackward << 3;
    temp16 |= item->fRotateFont << 4;
    temp16 |= item->fVertMerge << 5;
    temp16 |= item->fVertRestart << 6;
    temp16 |= item->vertAlign << 7;
    temp16 |= item->fUnused << 9;
    write_16ubit (fd, temp16);

    write_16ubit (fd, (U16) item->wUnused);

    wvPutBRC (&item->brcTop, fd);
    wvPutBRC (&item->brcLeft, fd);
    wvPutBRC (&item->brcBottom, fd);
    wvPutBRC (&item->brcRight, fd);
}
