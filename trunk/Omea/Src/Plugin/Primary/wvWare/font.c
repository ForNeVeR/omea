#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

void
wvGetFONTSIGNATURE (FONTSIGNATURE * fs, wvStream * fd)
{
    int i;
    for (i = 0; i < 4; i++)
	fs->fsUsb[i] = read_32ubit (fd);
    for (i = 0; i < 2; i++)
	fs->fsCsb[i] = read_32ubit (fd);
}

void
wvInitFONTSIGNATURE (FONTSIGNATURE * fs)
{
    int i;
    for (i = 0; i < 4; i++)
	fs->fsUsb[i] = 0;
    for (i = 0; i < 2; i++)
	fs->fsCsb[i] = 0;
}

void
wvGetPANOSE (PANOSE * item, wvStream * fd)
{
    item->bFamilyType = read_8ubit (fd);
    item->bSerifStyle = read_8ubit (fd);
    item->bWeight = read_8ubit (fd);
    item->bProportion = read_8ubit (fd);
    item->bContrast = read_8ubit (fd);
    item->bStrokeVariation = read_8ubit (fd);
    item->bArmStyle = read_8ubit (fd);
    item->bLetterform = read_8ubit (fd);
    item->bMidline = read_8ubit (fd);
    item->bXHeight = read_8ubit (fd);
}

void
wvInitPANOSE (PANOSE * item)
{
    item->bFamilyType = 0;
    item->bSerifStyle = 0;
    item->bWeight = 0;
    item->bProportion = 0;
    item->bContrast = 0;
    item->bStrokeVariation = 0;
    item->bArmStyle = 0;
    item->bLetterform = 0;
    item->bMidline = 0;
    item->bXHeight = 0;
}
