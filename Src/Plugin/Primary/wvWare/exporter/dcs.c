#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutDCS (DCS * item, wvStream * fd)
{
    U16 temp16 = (U16) 0;

    temp16 |= item->fdct;
    temp16 |= item->count << 3;
    temp16 |= item->reserved << 8;

    write_16ubit (fd, temp16);
}
