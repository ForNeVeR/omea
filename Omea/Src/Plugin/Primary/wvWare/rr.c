#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetRR (RR * item, wvStream * fd)
{
    item->cb = (S16) read_16ubit (fd);
    item->cbSzRecip = (S16) read_16ubit (fd);
}
