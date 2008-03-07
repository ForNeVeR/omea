#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutFFN6 (FFN * item, wvStream * fd)
{
    int len, i;
    U8 temp8 = 0;

    write_8ubit (fd, (U8) item->cbFfnM1);

    temp8 |= item->prq;
    temp8 |= item->fTrueType << 2;
    temp8 |= item->reserved1 << 3;
    temp8 |= item->ff << 4;
    temp8 |= item->reserved2 << 7;
    write_8ubit (fd, temp8);

    write_16ubit (fd, (U16) item->wWeight);
    write_8ubit (fd, item->chs);
    write_8ubit (fd, item->ixchSzAlt);

    len = item->cbFfnM1 - 5;
    if (len > 65)
	len = 65;
    for (i = 0; i < len; i++)
	write_8ubit (fd, (U8) item->xszFfn[i]);
}

void
wvPutFFN (FFN * item, wvStream * fd)
{
    int len, i;
    U8 temp8 = 0;

    write_8ubit (fd, (U8) item->cbFfnM1);

    temp8 |= item->prq;
    temp8 |= item->fTrueType << 2;
    temp8 |= item->reserved1 << 3;
    temp8 |= item->ff << 4;
    temp8 |= item->reserved2 << 7;
    write_8ubit (fd, temp8);

    write_16ubit (fd, (U16) item->wWeight);
    write_8ubit (fd, item->chs);
    write_8ubit (fd, item->ixchSzAlt);

    len = item->cbFfnM1 - 39;
    len = len / 2;
    if (len > 65)
	len = 65;

    for (i = 0; i < len; i++)
	write_16ubit (fd, item->xszFfn[i]);
}
