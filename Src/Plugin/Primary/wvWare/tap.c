#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvCopyTAP (TAP * dest, TAP * src)
{
    memcpy (dest, src, sizeof (TAP));
}

void
wvInitTAP (TAP * item)
{
    int i;
    static TAP cache;
    static int test = 0;
    if (!test)
      {
	  cache.jc = 0;
	  cache.dxaGapHalf = 0;
	  cache.dyaRowHeight = 0;
	  cache.fCantSplit = 0;
	  cache.fTableHeader = 0;

	  wvInitTLP (&cache.tlp);

	  cache.lwHTMLProps = 0;
	  cache.fCaFull = 0;
	  cache.fFirstRow = 0;
	  cache.fLastRow = 0;
	  cache.fOutline = 0;
	  cache.reserved = 0;
	  cache.itcMac = 0;
	  cache.dxaAdjust = 0;
	  cache.dxaScale = 0;
	  cache.dxsInch = 0;

	  for (i = 0; i < itcMax + 1; i++)
	      cache.rgdxaCenter[i] = 0;
	  for (i = 0; i < itcMax + 1; i++)
	      cache.rgdxaCenterPrint[i] = 0;
	  for (i = 0; i < itcMax; i++)
	      wvInitTC (&(cache.rgtc[i]));
	  for (i = 0; i < itcMax; i++)
	      wvInitSHD (&(cache.rgshd[i]));
	  for (i = 0; i < 6; i++)
	      wvInitBRC (&(cache.rgbrcTable[i]));
	  test++;
      }
    wvCopyTAP (item, &cache);
}
