#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutSEPX (wvVersion ver, SEPX * item, wvStream * fd)
{
    U16 i = (U16) 0;

    write_16ubit (fd, item->cb);

    if (!item->cb)
	return;

    for (i = 0; i < item->cb; i++)
      {
	  write_8ubit (fd, item->grpprl[i]);
      }
}
