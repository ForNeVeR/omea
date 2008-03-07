#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutBKF (BKF * item, wvStream * fd)
{
    U16 temp16 = (U16) 0;

    write_16ubit (fd, (U16) item->ibkl);

    temp16 |= item->itcFirst;
    temp16 |= item->fPub << 7;
    temp16 |= item->itcLim << 8;
    temp16 |= item->fCol << 15;

    write_16ubit (fd, temp16);
}
