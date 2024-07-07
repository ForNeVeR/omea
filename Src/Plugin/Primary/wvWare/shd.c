#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetSHD_internal (SHD * item, wvStream * fd, U8 * pointer)
{
    U16 temp16;
#ifdef PURIFY
    wvInitSHD (item);
#endif
    temp16 = dread_16ubit (fd, &pointer);
    item->icoFore = temp16 & 0x001F;
    item->icoBack = (temp16 & 0x03E0) >> 5;
    item->ipat = (temp16 & 0xFC00) >> 10;
}

void
wvGetSHD (SHD * item, wvStream * fd)
{
    wvGetSHD_internal (item, fd, NULL);
}

void
wvGetSHDFromBucket (SHD * item, U8 * pointer)
{
    wvGetSHD_internal (item, NULL, pointer);
}

void
wvInitSHD (SHD * item)
{
    item->icoFore = 0;
    item->icoBack = 0;
    item->ipat = 0;
}

void
wvCopySHD (SHD * dest, SHD * src)
{
    memcpy (dest, src, sizeof (SHD));
}
