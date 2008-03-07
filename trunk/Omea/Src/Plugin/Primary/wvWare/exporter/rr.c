#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutRR (RR * item, wvStream * fd)
{
    write_16ubit (fd, (U16) item->cb);
    write_16ubit (fd, (U16) item->cbSzRecip);
}
