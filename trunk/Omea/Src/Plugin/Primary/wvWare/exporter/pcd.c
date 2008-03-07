#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutPCD (PCD * item, wvStream * fd)
{
    U8 temp8 = 0;

    temp8 |= item->fNoParaLast;
    temp8 |= item->fPaphNil << 1;
    temp8 |= item->fCopied << 2;
    temp8 |= item->reserved << 3;

    write_8ubit (fd, temp8);

    write_8ubit (fd, (U8) item->fn);
    write_32ubit (fd, item->fc);
    wvPutPRM (&item->prm, fd);
}
