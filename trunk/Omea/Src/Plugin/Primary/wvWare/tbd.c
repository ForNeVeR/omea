#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"
#include "wvinternal.h"

void
wvInitTBD (TBD * item)
{
    item->jc = 0;
    item->tlc = 0;
    item->reserved = 0;
}

void
wvCopyTBD (TBD * dest, TBD * src)
{
    memcpy (dest, src, sizeof (TBD));
}

void
wvGetTBD (TBD * item, wvStream * fd)
{
    wvGetTBD_internal (item, fd, NULL);
}

void
wvGetTBDFromBucket (TBD * item, U8 * pointer)
{
    wvGetTBD_internal (item, NULL, pointer);
}


void
wvGetTBD_internal (TBD * item, wvStream * fd, U8 * pointer)
{
    U8 temp8;
    temp8 = dread_8ubit (fd, &pointer);
#ifdef PURIFY
    wvInitTBD (item);
#endif
    item->jc = temp8 & 0x07;
    item->tlc = (temp8 & 0x38) >> 3;
    item->reserved = (temp8 & 0xC0) >> 6;
}
