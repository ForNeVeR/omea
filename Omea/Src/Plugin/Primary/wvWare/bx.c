#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetBX (BX * item, U8 * page, U16 * pos)
{
    item->offset = bread_8ubit (&(page[*pos]), pos);
    wvGetPHE (&item->phe, 0, page, pos);
}

void
wvGetBX6 (BX * item, U8 * page, U16 * pos)
{
    item->offset = bread_8ubit (&(page[*pos]), pos);
    wvGetPHE6 (&item->phe, page, pos);
}
