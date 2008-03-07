#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutDTTM (DTTM * item, wvStream * fd)
{

    U16 temp16 = (U16) 0;

    temp16 |= item->mint;
    temp16 |= item->hr << 6;
    temp16 |= item->dom << 11;
    write_16ubit (fd, temp16);

    temp16 = (U16) 0;
    temp16 |= item->mon;
    temp16 |= item->yr << 4;
    temp16 |= item->wdy << 13;
    write_16ubit (fd, temp16);
}

void
wvUnixToDTTM (struct tm *src, DTTM * dest)
{
    dest->mint = src->tm_min;
    dest->hr = src->tm_hour;
    dest->dom = src->tm_mday;
    dest->mon = src->tm_mon + 1;
    dest->yr = src->tm_year;
    dest->wdy = src->tm_wday;
}
