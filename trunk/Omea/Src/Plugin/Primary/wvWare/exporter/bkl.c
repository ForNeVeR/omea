#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutBKL (BKL * item, wvStream * fd)
{
    write_16ubit (fd, (U16) item->ibkf);
}
