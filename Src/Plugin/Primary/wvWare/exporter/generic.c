#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutChar (wvStream * fd, U8 chartype, U16 ch)
{
    if (chartype == 1)
	write_8ubit (fd, (U8) ch);
    else
	write_16ubit (fd, ch);
}
