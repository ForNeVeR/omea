#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutNUMRM (NUMRM * item, wvStream * fd)
{
    int i;

    write_8ubit (fd, item->fNumRM);
    write_8ubit (fd, item->Spare1);
    write_16ubit (fd, (U16) item->ibstNumRM);

    wvPutDTTM (&item->dttmNumRM, fd);

    for (i = 0; i < 9; i++)
	write_8ubit (fd, item->rgbxchNums[i]);

    for (i = 0; i < 9; i++)
	write_8ubit (fd, item->rgnfc[i]);

    write_16ubit (fd, (U16) item->Spare2);

    for (i = 0; i < 9; i++)
	write_32ubit (fd, (U32) item->PNBR[i]);

    for (i = 0; i < 32; i++)
	write_16ubit (fd, item->xst[i]);
}
