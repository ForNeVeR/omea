#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutSED (SED * item, wvStream * fd)
{
    write_16ubit (fd, (U16) item->fn);
    write_32ubit (fd, item->fcSepx);
    write_16ubit (fd, (U16) item->fnMpr);
    write_32ubit (fd, item->fcMpr);
}
