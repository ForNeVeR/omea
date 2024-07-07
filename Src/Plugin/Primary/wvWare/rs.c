#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetRS (RS * item, wvStream * fd)
{
    item->fRouted = (S16) read_16ubit (fd);
    item->fReturnOrig = (S16) read_16ubit (fd);
    item->fTrackStatus = (S16) read_16ubit (fd);
    item->fDirty = (S16) read_16ubit (fd);
    item->nProtect = (S16) read_16ubit (fd);
    item->iStage = (S16) read_16ubit (fd);
    item->delOption = (S16) read_16ubit (fd);
    item->cRecip = (S16) read_16ubit (fd);
}
