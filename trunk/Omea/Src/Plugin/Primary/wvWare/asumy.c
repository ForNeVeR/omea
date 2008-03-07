#include <sys/types.h>
#include <string.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetASUMY (ASUMY * item, wvStream * fd)
{
    item->lLevel = (S32) read_32ubit (fd);
}
