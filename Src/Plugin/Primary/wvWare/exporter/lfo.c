#include <stdio.h>
#include <stdlib.h>
#include "wvexporter-priv.h"

void
wvPutLFO (LFO * item, wvStream * fd)
{
    int i;

    write_32ubit (fd, item->lsid);
    write_32ubit (fd, item->reserved1);
    write_32ubit (fd, item->reserved2);
    write_8ubit (fd, item->clfolvl);

    for (i = 0; i < 3; i++)
	write_8ubit (fd, item->reserved3[i]);
}
