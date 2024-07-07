#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutFTXBXS (FTXBXS * item, wvStream * fd)
{
    write_32ubit (fd, (U32) item->cTxbx_iNextReuse);
    write_32ubit (fd, (U32) item->cReusable);
    write_16ubit (fd, (U16) item->fReusable);
    write_32ubit (fd, (U32) item->reserved);
    write_32ubit (fd, (U32) item->lid);
    write_32ubit (fd, (U32) item->txidUndo);
}
