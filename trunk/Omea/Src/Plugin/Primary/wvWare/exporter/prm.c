#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutPRM (PRM * item, wvStream * fd)
{
    U16 temp16 = 0;

    temp16 |= item->fComplex;
    if (item->fComplex)
	temp16 |= item->para.var2.igrpprl << 1;
    else
      {
	  temp16 |= item->para.var1.isprm << 1;
	  temp16 |= item->para.var1.val << 8;
      }

    write_16ubit (fd, temp16);
}
