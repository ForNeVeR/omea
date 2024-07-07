#include <sys/types.h>
#include <string.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetDOGRID (DOGRID * dog, wvStream * fd)
{
    U16 temp16;
    dog->xaGrid = read_16ubit (fd);
    dog->yaGrid = read_16ubit (fd);
    dog->dxaGrid = read_16ubit (fd);
    dog->dyaGrid = read_16ubit (fd);

    temp16 = read_16ubit (fd);

    dog->dyGridDisplay = temp16 & 0x007F;
    dog->fTurnItOff = (temp16 & 0x0080) >> 7;
    dog->dxGridDisplay = (temp16 & 0x7F00) >> 8;
    dog->fFollowMargins = (temp16 & 0x8000) >> 15;
}

void
wvInitDOGRID (DOGRID * dog)
{
    dog->xaGrid = 0;
    dog->yaGrid = 0;
    dog->dxaGrid = 0;
    dog->dyaGrid = 0;
    dog->dyGridDisplay = 0;
    dog->fTurnItOff = 0;
    dog->dxGridDisplay = 0;
    dog->fFollowMargins = 0;
}
