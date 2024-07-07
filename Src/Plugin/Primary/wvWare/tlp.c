#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"
#include "wvinternal.h"

void
wvGetTLP_internal (TLP * dest, wvStream * infd, U8 * pointer)
{
    U16 temp16;

    dest->itl = dread_16ubit (infd, &pointer);

    temp16 = dread_16ubit (infd, &pointer);
    dest->fBorders = temp16 & 0x0001;
    dest->fShading = (temp16 & 0x0002) >> 1;
    dest->fFont = (temp16 & 0x0004) >> 2;
    dest->fColor = (temp16 & 0x0008) >> 3;
    dest->fBestFit = (temp16 & 0x0010) >> 4;
    dest->fHdrRows = (temp16 & 0x0020) >> 5;
    dest->fLastRow = (temp16 & 0x0040) >> 6;
    dest->fHdrCols = (temp16 & 0x0080) >> 7;
    dest->fLastCol = (temp16 & 0x0100) >> 8;
}

void
wvGetTLP (TLP * item, wvStream * infd)
{
    wvGetTLP_internal (item, infd, NULL);
}

void
wvGetTLPFromBucket (TLP * item, U8 * pointer)
{
    wvGetTLP_internal (item, NULL, pointer);
}

void
wvCopyTLP (TLP * dest, TLP * src)
{
    dest->itl = src->itl;
    dest->fBorders = src->fBorders;
    dest->fShading = src->fShading;
    dest->fFont = src->fFont;
    dest->fColor = src->fColor;
    dest->fBestFit = src->fBestFit;
    dest->fHdrRows = src->fHdrRows;
    dest->fLastRow = src->fLastRow;
    dest->fHdrCols = src->fHdrCols;
    dest->fLastCol = src->fLastCol;
}

void
wvInitTLP (TLP * item)
{
    item->itl = 0;
    item->fBorders = 0;
    item->fShading = 0;
    item->fFont = 0;
    item->fColor = 0;
    item->fBestFit = 0;
    item->fHdrRows = 0;
    item->fLastRow = 0;
    item->fHdrCols = 0;
    item->fLastCol = 0;
}
