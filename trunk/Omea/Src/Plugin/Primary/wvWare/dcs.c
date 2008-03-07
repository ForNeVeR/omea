#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetDCS_internal (DCS * item, wvStream * fd, U8 * pointer)
{
    U16 temp16;
    temp16 = dread_16ubit (fd, &pointer);
    item->fdct = temp16 & 0x0007;
    item->count = (temp16 & 0x00F8) >> 3;
    item->reserved = (temp16 & 0xff00) >> 8;
}

void
wvGetDCS (DCS * item, wvStream * fd)
{
    wvGetDCS_internal (item, fd, NULL);
}

void
wvGetDCSFromBucket (DCS * item, U8 * pointer)
{
    wvGetDCS_internal (item, NULL, pointer);
}

void
wvCopyDCS (DCS * dest, DCS * src)
{
    memcpy (dest, src, sizeof (DCS));
}

void
wvInitDCS (DCS * item)
{
    item->fdct = 0;
    item->count = 0;
    item->reserved = 0;
}
