#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvCopyLSPD (LSPD * dest, LSPD * src)
{
    dest->dyaLine = src->dyaLine;
    dest->fMultLinespace = src->fMultLinespace;
}


void
wvInitLSPD (LSPD * item)
{
    item->dyaLine = 0;
    item->fMultLinespace = 0;
}

void
wvGetLSPDFromBucket (LSPD * item, U8 * pointer)
{
    item->dyaLine = dread_16ubit (NULL, &pointer);
    item->fMultLinespace = dread_16ubit (NULL, &pointer);
}
