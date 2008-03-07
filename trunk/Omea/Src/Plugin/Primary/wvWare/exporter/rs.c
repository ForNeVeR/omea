#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutRS (RS * item, wvStream * fd)
{
    write_16ubit (fd, (U16) item->fRouted);
    write_16ubit (fd, (U16) item->fReturnOrig);
    write_16ubit (fd, (U16) item->fTrackStatus);
    write_16ubit (fd, (U16) item->fDirty);
    write_16ubit (fd, (U16) item->nProtect);
    write_16ubit (fd, (U16) item->iStage);
    write_16ubit (fd, (U16) item->delOption);
    write_16ubit (fd, (U16) item->cRecip);
}
