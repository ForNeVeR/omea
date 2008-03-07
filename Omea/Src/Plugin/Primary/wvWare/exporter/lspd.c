#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutLSPD (LSPD * item, wvStream * fd)
{
    write_16ubit (fd, item->dyaLine);
    write_16ubit (fd, item->fMultLinespace);
}
