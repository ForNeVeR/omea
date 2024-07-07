#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetASUMYI (ASUMYI * asu, wvStream * fd)
{
    U16 temp16 = read_16ubit (fd);

    asu->fValid = temp16 & 0x0001;
    asu->fView = (temp16 & 0x0002) >> 1;
    asu->iViewBy = (temp16 & 0x000C) >> 2;
    asu->fUpdateProps = (temp16 & 0x0010) >> 4;
    asu->reserved = (temp16 & 0xFFE0) >> 5;

    asu->wDlgLevel = read_16ubit (fd);
    asu->lHighestLevel = read_32ubit (fd);
    asu->lCurrentLevel = read_32ubit (fd);
}


void
wvInitASUMYI (ASUMYI * asu)
{
    asu->fValid = 0;
    asu->fView = 0;
    asu->iViewBy = 0;
    asu->fUpdateProps = 0;
    asu->reserved = 0;
    asu->wDlgLevel = 0;
    asu->lHighestLevel = 0;
    asu->lCurrentLevel = 0;
}
