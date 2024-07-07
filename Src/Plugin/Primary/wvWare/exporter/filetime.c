#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutFILETIME (FILETIME * ft, wvStream * fd)
{
    write_32ubit (fd, ft->dwLowDateTime);
    write_32ubit (fd, ft->dwHighDateTime);
}
