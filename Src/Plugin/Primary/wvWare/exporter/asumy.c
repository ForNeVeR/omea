#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutASUMY (ASUMY * item, wvStream * fd)
{
    write_32ubit (fd, (U32) item->lLevel);
}
