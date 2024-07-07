#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutPropHeader (PropHeader * header, wvStream * file)
{
    int i = 0;

    write_16ubit (file, header->byteOrder);
    write_16ubit (file, header->wFormat);
    write_16ubit (file, header->osVersion1);
    write_16ubit (file, header->osVersion2);

    for (i = 0; i < 16; i++)
	write_8ubit (file, header->classId[i]);
    write_32ubit (file, header->cSections);
}

void
wvPutFIDAndOffset (FIDAndOffset * fid, wvStream * file)
{
    int i;
    for (i = 0; i < 4; i++)
	write_32ubit (file, fid->dwords[i]);
    write_32ubit (file, fid->dwOffset);
}


void
wvPutSummaryInfo (SummaryInfo * si, wvStream * file, U32 offset)
{
    U32 i = (U32) 0;
    U32 temp32 = (U32) 0;

    /* TODO: is this needed/correct? */
    wvStream_offset (file, offset);

    write_32ubit (file, si->cBytes);
    write_32ubit (file, si->cProps);

    /* TODO: is probably right -- used in import */
    if (si->cProps == 0)
	return;

    for (i = 0; i < si->cProps; i++)
      {
	  write_32ubit (file, si->aProps[i].propID);
	  temp32 = si->aProps[i].dwOffset;
	  temp32 += (U32) (8 + si->cProps * 8);
	  write_32ubit (file, temp32);
      }

    if (si->cBytes - 8 * si->cProps > 0)
      {
	  for (i = 0; i < si->cBytes - 8 * si->cProps; i++)
	      write_8ubit (file, si->data[i]);
      }
}
