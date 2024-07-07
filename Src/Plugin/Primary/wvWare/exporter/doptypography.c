#include <stdio.h>
#include <stdlib.h>
#include "wvexporter-priv.h"

void
wvPutDOPTYPOGRAPHY (DOPTYPOGRAPHY * dopt, wvStream * fd)
{
    int i = 0;
    U16 temp16 = (U16) 0;

    temp16 |= dopt->fKerningPunct;
    temp16 |= dopt->iJustification << 1;
    temp16 |= dopt->iLevelOfKinsoku << 3;
    temp16 |= dopt->f2on1 << 5;
    temp16 |= dopt->reserved << 6;

    write_16ubit (fd, temp16);
    write_16ubit (fd, (U16) dopt->cchFollowingPunct);
    write_16ubit (fd, dopt->cchLeadingPunct);

    for (i = 0; i < 101; i++)
	write_16ubit (fd, dopt->rgxchFPunct[i]);

    for (i = 0; i < 51; i++)
	write_16ubit (fd, dopt->rgxchLPunct[i]);
}
