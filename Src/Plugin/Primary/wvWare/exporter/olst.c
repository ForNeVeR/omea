#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutOLST (OLST * item, wvStream * fd)
{
    U8 i;
    for (i = 0; i < 9; i++)
	wvPutANLV (&item->rganlv[i], fd);

    write_8ubit (fd, item->fRestartHdr);
    write_8ubit (fd, item->fSpareOlst2);
    write_8ubit (fd, item->fSpareOlst3);
    write_8ubit (fd, item->fSpareOlst4);

    /* assuming WORD8 */
    for (i = 0; i < 32; i++)
	write_16ubit (fd, item->rgxch[i]);

    /* non-word8 */
    /*
       for(i=0;i<64;i++)
       write_8ubit(fd, item->rgxch[i]);
     */
}
