#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutFDOA (FDOA * item, wvStream * fd)
{
    write_32ubit (fd, (U32) item->fc);
    write_16ubit (fd, (U16) item->ctxbx);
}
