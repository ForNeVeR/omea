#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutSHD (SHD * item, wvStream * fd)
{
    U16 temp16 = 0;

    temp16 |= item->icoFore;
    temp16 |= item->icoBack << 5;
    temp16 |= item->ipat << 10;

    write_16ubit (fd, temp16);
}
