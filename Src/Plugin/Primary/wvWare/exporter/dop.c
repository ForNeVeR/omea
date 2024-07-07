#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutCOPTS (COPTS * item, wvStream * fd)
{
    U16 temp16 = (U16) 0;

    temp16 |= item->fNoTabForInd;
    temp16 |= item->fNoSpaceRaiseLower << 1;
    temp16 |= item->fSuppressSpbfAfterPageBreak << 2;
    temp16 |= item->fWrapTrailSpaces << 3;
    temp16 |= item->fMapPrintTextColor << 4;
    temp16 |= item->fNoColumnBalance << 5;
    temp16 |= item->fConvMailMergeEsc << 6;
    temp16 |= item->fSuppressTopSpacing << 7;
    temp16 |= item->fOrigWordTableRules << 8;
    temp16 |= item->fTransparentMetafiles << 9;
    temp16 |= item->fShowBreaksInFrames << 10;
    temp16 |= item->fSwapBordersFacingPgs << 11;
    temp16 |= item->reserved << 12;

    write_16ubit (fd, temp16);
}

void
wvPutDOP (wvVersion ver, DOP * item, wvStream * fd)
{
    U16 temp16 = 0;
    U32 temp32 = 0;
    int i;

    temp16 |= item->fFacingPages;
    temp16 |= item->fWidowControl << 1;
    temp16 |= item->fPMHMainDoc << 2;
    temp16 |= item->grfSuppression << 3;
    temp16 |= item->fpc << 5;
    temp16 |= item->reserved1 << 7;
    temp16 |= item->grpfIhdt << 8;
    write_16ubit (fd, temp16);

    temp16 = 0;
    temp16 |= item->rncFtn;
    temp16 |= item->nFtn;
    write_16ubit (fd, temp16);

    temp16 = 0;
    temp16 |= item->fOutlineDirtySave;
    temp16 |= item->reserved2 << 1;
    temp16 |= item->fOnlyMacPics << 8;
    temp16 |= item->fOnlyWinPics << 9;
    temp16 |= item->fLabelDoc << 10;
    temp16 |= item->fHyphCapitals << 11;
    temp16 |= item->fAutoHyphen << 12;
    temp16 |= item->fFormNoFields << 13;
    temp16 |= item->fLinkStyles << 14;
    temp16 |= item->fRevMarking << 15;
    write_16ubit (fd, temp16);

    temp16 = 0;
    temp16 |= item->fBackup;
    temp16 |= item->fExactCWords << 1;
    temp16 |= item->fPagHidden << 2;
    temp16 |= item->fPagResults << 3;
    temp16 |= item->fLockAtn << 4;
    temp16 |= item->fMirrorMargins << 5;
    temp16 |= item->reserved3 << 6;
    temp16 |= item->fDfltTrueType << 7;
    temp16 |= item->fPagSuppressTopSpacing << 8;
    temp16 |= item->fProtEnabled << 9;
    temp16 |= item->fDispFormFldSel << 10;
    temp16 |= item->fRMView << 11;
    temp16 |= item->fRMPrint << 12;
    temp16 |= item->reserved4 << 13;
    temp16 |= item->fLockRev << 14;
    temp16 |= item->fEmbedFonts << 15;
    write_16ubit (fd, temp16);

    wvPutCOPTS (&item->copts, fd);

    write_16ubit (fd, item->dxaTab);
    write_16ubit (fd, item->wSpare);
    write_16ubit (fd, item->dxaHotZ);
    write_16ubit (fd, item->cConsecHypLim);
    write_16ubit (fd, item->wSpare2);

    wvPutDTTM (&item->dttmCreated, fd);
    wvPutDTTM (&item->dttmRevised, fd);
    wvPutDTTM (&item->dttmLastPrint, fd);

    write_16ubit (fd, item->nRevision);
    write_32ubit (fd, item->tmEdited);
    write_32ubit (fd, item->cWords);
    write_32ubit (fd, item->cCh);
    write_16ubit (fd, item->cPg);
    write_32ubit (fd, item->cParas);

    temp16 = 0;
    temp16 |= item->rncEdn;
    temp16 |= item->nEdn << 2;
    write_16ubit (fd, temp16);

    temp16 = 0;
    temp16 |= item->epc;
    temp16 |= item->nfcFtnRef << 2;
    temp16 |= item->nfcEdnRef << 6;
    temp16 |= item->fPrintFormData << 10;
    temp16 |= item->fSaveFormData << 11;
    temp16 |= item->fShadeFormData << 12;
    temp16 |= item->reserved6 << 13;
    temp16 |= item->fWCFtnEdn << 15;
    write_16ubit (fd, temp16);

    write_32ubit (fd, item->cLines);
    write_32ubit (fd, item->cWordsFtnEnd);
    write_32ubit (fd, item->cChFtnEdn);
    write_16ubit (fd, item->cPgFtnEdn);
    write_32ubit (fd, item->cParasFtnEdn);
    write_32ubit (fd, item->cLinesFtnEdn);
    write_32ubit (fd, item->lKeyProtDoc);

    temp16 = 0;
    temp16 |= item->wvkSaved;
    temp16 |= item->wScaleSaved << 3;
    temp16 |= item->zkSaved << 12;
    temp16 |= item->fRotateFontW6 << 14;
    temp16 |= item->iGutterPos << 15;
    write_16ubit (fd, temp16);

    if (ver == WORD6)
      {
	  /* phew... WORD6 support done */
	  return;
      }

    temp32 = 0;
    temp32 |= item->fNoTabForInd;
    temp32 |= item->fNoSpaceRaiseLower << 1;
    temp32 |= item->fSuppressSpbfAfterPageBreak << 2;
    temp32 |= item->fWrapTrailSpaces << 3;
    temp32 |= item->fMapPrintTextColor << 4;
    temp32 |= item->fNoColumnBalance << 5;
    temp32 |= item->fConvMailMergeEsc << 6;
    temp32 |= item->fSuppressTopSpacing << 7;
    temp32 |= item->fOrigWordTableRules << 8;
    temp32 |= item->fTransparentMetafiles << 9;
    temp32 |= item->fShowBreaksInFrames << 10;
    temp32 |= item->fSwapBordersFacingPgs << 11;
    temp32 |= item->reserved7 << 12;
    temp32 |= item->fSuppressTopSpacingMac5 << 16;
    temp32 |= item->fTruncDxaExpand << 17;
    temp32 |= item->fPrintBodyBeforeHdr << 18;
    temp32 |= item->fNoLeading << 19;
    temp32 |= item->reserved8 << 20;
    temp32 |= item->fMWSmallCaps << 21;
    temp32 |= item->reserved9 << 22;
    write_32ubit (fd, temp32);

    if (ver == WORD7)
      {
	  /* Hehe: WORD7 support is done */
	  return;
      }

    /* onto WORD8 */
    write_16ubit (fd, (U16) item->adt);
    wvPutDOPTYPOGRAPHY (&item->doptypography, fd);
    wvPutDOGRID (&item->dogrid, fd);

    temp16 = 0;
    temp16 |= item->reserved10;
    temp16 |= item->lvl << 1;
    temp16 |= item->fGramAllDone << 5;
    temp16 |= item->fGramAllClean << 6;
    temp16 |= item->fSubsetFonts << 7;
    temp16 |= item->fHideLastVersion << 8;
    temp16 |= item->fHtmlDoc << 9;
    temp16 |= item->reserved11 << 10;
    temp16 |= item->fSnapBorder << 11;
    temp16 |= item->fIncludeHeader << 12;
    temp16 |= item->fIncludeFooter << 13;
    temp16 |= item->fForcePageSizePag << 14;
    temp16 |= item->fMinFontSizePag << 15;
    write_16ubit (fd, temp16);

    temp16 = 0;
    temp16 |= item->fHaveVersions;
    temp16 |= item->fAutoVersion << 1;
    temp16 |= item->reserved11;
    write_16ubit (fd, temp16);

    wvPutASUMYI (&item->asumyi, fd);

    write_32ubit (fd, item->cChWS);
    write_32ubit (fd, item->cChWSFtnEdn);
    write_32ubit (fd, item->grfDocEvents);

    temp32 = 0;
    temp32 |= item->fVirusPrompted;
    temp32 |= item->fVirusLoadSafe << 1;
    temp32 |= item->KeyVirusSession30 << 2;
    write_32ubit (fd, temp32);

    for (i = 0; i < 30; i++)
	write_8ubit (fd, item->Spare[i]);

    write_32ubit (fd, item->reserved12);
    write_32ubit (fd, item->reserved13);
    write_32ubit (fd, item->cDBC);
    write_32ubit (fd, item->cDBCFtnEdn);
    write_32ubit (fd, item->reserved14);
    write_16ubit (fd, item->new_nfcFtnRef);
    write_16ubit (fd, item->new_nfcEdnRef);
    write_16ubit (fd, item->hpsZoonFontPag);
    write_16ubit (fd, item->dywDispPag);

    /* eat my shorts Word8 */
}
