#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutLSTF (LSTF * item, wvStream * fd)
{
    int i;
    U8 temp8 = (U8) 0;

    write_32ubit (fd, item->lsid);
    write_32ubit (fd, item->tplc);

    for (i = 0; i < 9; i++)
	write_16ubit (fd, item->rgistd[i]);

    temp8 |= item->fSimpleList;
    temp8 |= item->fRestartHdn << 1;
    temp8 |= item->reserved1 << 2;
    /* temp8 |= item->reserved2; */
    write_8ubit (fd, temp8);
    write_8ubit (fd, (U8) item->reserved2);
}
