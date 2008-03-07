#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvInitFIBForExport (FIB * item)
{
    item->wIdent = 0;		/* The 'magic number' */
    item->nFib = 101;
    item->nProduct = 8;		/* The product version */
    item->lid = 0;		/* The 'language stamp' */
    item->pnNext = 0;		/* Fantastically undescribed in the spec... */
    item->fDot = 0;		/* We are not a template */
    item->fGlsy = 0;		/* We are not a glossary either */
    item->fComplex = 0;
    item->fHasPic = 0;		/* No Pictures */
    item->cQuickSaves = 0;
    item->fEncrypted = 0;
    item->fWhichTblStm = 0;
    item->fReadOnlyRecommended = 0;
    item->fWriteReservation = 0;
    item->fExtChar = 0;
    item->fLoadOverride = 0;
    item->fFarEast = 0;
    item->fCrypto = 0;
    item->nFibBack = 101;
    item->lKey = 0;
    item->envr = 0;		/* Pretend we're Word for WIndows (i.e. not mac) */
    item->fMac = 0;		/* And we've haven't been saved on a Mac either */
    item->fEmptySpecial = 0;
    item->fLoadOverridePage = 0;
    item->fFutureSavedUndo = 0;
    item->fWord97Saved = 0;
    item->fSpare0 = 0;
    item->chse = 0;		/* We write windows ANSI */
    item->chsTables = 0;	/* We write windows ANSI */
    item->fcMin = 0;		/* file-offset of beginning of text stream */
    item->fcMac = 0;		/* file-offset of last character + 1 */
    item->csw = 0;
    item->wMagicCreated = 0x6A62;	/* Pretend we're Word.
					   * We should perhaps 'choose a different 
					   * value', as the spec suggests.  I'd like to
					   * choose 0x0AB1 :-)
					 */
    item->wMagicRevised = 0;
    item->wMagicCreatedPrivate = 0;
    item->wMagicRevisedPrivate = 0;
    item->pnFbpChpFirst_W6 = 0;
    item->pnChpFirst_W6 = 0;
    item->cpnBteChp_W6 = 0;
    item->pnFbpPapFirst_W6 = 0;
    item->pnPapFirst_W6 = 0;
    item->cpnBtePap_W6 = 0;
    item->pnFbpLvcFirst_W6 = 0;
    item->pnLvcFirst_W6 = 0;
    item->cpnBteLvc_W6 = 0;
    item->lidFE = 0;
    item->clw = 0;
    item->cbMac = 0;		/* Last character's offset + 1 */
    item->lProductCreated = 0x010700;
    /* Build date of creator  This means 1 July 00 */
    item->lProductRevised = 0;
    /* Build date of program which last revised the
       document */
    item->ccpText = 0;		/* Length of main stream */
    item->ccpFtn = 0;		/* Length of footer stream */
    item->ccpHdr = 0;		/* Length of header stream */
    item->ccpMcr = 0;		/* Length of macro subdocument stream, which should
				   ' always be zero' */
    item->ccpAtn = 0;		/* Length of the annotation stream */
    item->ccpEdn = 0;		/* Length of the endnote stream */
    item->ccpTxbx = 0;		/* Length of the textbox stream */
    item->ccpHdrTxbx = 0;	/* Length of the 'header textbox' stream */
    item->pnFbpChpFirst = 0;
    item->pnChpFirst = 0;
    item->cpnBteChp = 0;
    item->pnFbpPapFirst = 0;
    item->pnPapFirst = 0;
    item->cpnBtePap = 0;
    item->pnFbpLvcFirst = 0;
    item->pnLvcFirst = 0;
    item->cpnBteLvc = 0;
    item->fcIslandFirst = 0;
    item->fcIslandLim = 0;
    item->cfclcb = 0;
    item->fcStshfOrig = 0;
    item->lcbStshfOrig = 0;
    item->fcStshf = 0;
    item->lcbStshf = 0;
    item->fcPlcffndRef = 0;
    item->lcbPlcffndRef = 0;
    item->fcPlcffndTxt = 0;
    item->lcbPlcffndTxt = 0;
    item->fcPlcfandRef = 0;
    item->lcbPlcfandRef = 0;
    item->fcPlcfandTxt = 0;
    item->lcbPlcfandTxt = 0;
    item->fcPlcfsed = 0;
    item->lcbPlcfsed = 0;
    item->fcPlcpad = 0;
    item->lcbPlcpad = 0;
    item->fcPlcfphe = 0;
    item->lcbPlcfphe = 0;
    item->fcSttbfglsy = 0;
    item->lcbSttbfglsy = 0;
    item->fcPlcfglsy = 0;
    item->lcbPlcfglsy = 0;
    item->fcPlcfhdd = 0;
    item->lcbPlcfhdd = 0;
    item->fcPlcfbteChpx = 0;
    item->lcbPlcfbteChpx = 0;
    item->fcPlcfbtePapx = 0;
    item->lcbPlcfbtePapx = 0;
    item->fcPlcfsea = 0;
    item->lcbPlcfsea = 0;
    item->fcSttbfffn = 0;
    item->lcbSttbfffn = 0;
    item->fcPlcffldMom = 0;
    item->lcbPlcffldMom = 0;
    item->fcPlcffldHdr = 0;
    item->lcbPlcffldHdr = 0;
    item->fcPlcffldFtn = 0;
    item->lcbPlcffldFtn = 0;
    item->fcPlcffldAtn = 0;
    item->lcbPlcffldAtn = 0;
    item->fcPlcffldMcr = 0;
    item->lcbPlcffldMcr = 0;
    item->fcSttbfbkmk = 0;
    item->lcbSttbfbkmk = 0;
    item->fcPlcfbkf = 0;
    item->lcbPlcfbkf = 0;
    item->fcPlcfbkl = 0;
    item->lcbPlcfbkl = 0;
    item->fcCmds = 0;
    item->lcbCmds = 0;
    item->fcPlcmcr = 0;
    item->lcbPlcmcr = 0;
    item->fcSttbfmcr = 0;
    item->lcbSttbfmcr = 0;
    item->fcPrDrvr = 0;
    item->lcbPrDrvr = 0;
    item->fcPrEnvPort = 0;
    item->lcbPrEnvPort = 0;
    item->fcPrEnvLand = 0;
    item->lcbPrEnvLand = 0;
    item->fcWss = 0;
    item->lcbWss = 0;
    item->fcDop = 0;
    item->lcbDop = 0;
    item->fcSttbfAssoc = 0;
    item->lcbSttbfAssoc = 0;
    item->fcClx = 0;
    item->lcbClx = 0;
    item->fcPlcfpgdFtn = 0;
    item->lcbPlcfpgdFtn = 0;
    item->fcAutosaveSource = 0;
    item->lcbAutosaveSource = 0;
    item->fcGrpXstAtnOwners = 0;
    item->lcbGrpXstAtnOwners = 0;
    item->fcSttbfAtnbkmk = 0;
    item->lcbSttbfAtnbkmk = 0;
    item->fcPlcdoaMom = 0;
    item->lcbPlcdoaMom = 0;
    item->fcPlcdoaHdr = 0;
    item->lcbPlcdoaHdr = 0;
    item->fcPlcspaMom = 0;
    item->lcbPlcspaMom = 0;
    item->fcPlcspaHdr = 0;
    item->lcbPlcspaHdr = 0;
    item->fcPlcfAtnbkf = 0;
    item->lcbPlcfAtnbkf = 0;
    item->fcPlcfAtnbkl = 0;
    item->lcbPlcfAtnbkl = 0;
    item->fcPms = 0;
    item->lcbPms = 0;
    item->fcFormFldSttbs = 0;
    item->lcbFormFldSttbs = 0;
    item->fcPlcfendRef = 0;
    item->lcbPlcfendRef = 0;
    item->fcPlcfendTxt = 0;
    item->lcbPlcfendTxt = 0;
    item->fcPlcffldEdn = 0;
    item->lcbPlcffldEdn = 0;
    item->fcPlcfpgdEdn = 0;
    item->lcbPlcfpgdEdn = 0;
    item->fcDggInfo = 0;
    item->lcbDggInfo = 0;
    item->fcSttbfRMark = 0;
    item->lcbSttbfRMark = 0;
    item->fcSttbCaption = 0;
    item->lcbSttbCaption = 0;
    item->fcSttbAutoCaption = 0;
    item->lcbSttbAutoCaption = 0;
    item->fcPlcfwkb = 0;
    item->lcbPlcfwkb = 0;
    item->fcPlcfspl = 0;
    item->lcbPlcfspl = 0;
    item->fcPlcftxbxTxt = 0;
    item->lcbPlcftxbxTxt = 0;
    item->fcPlcffldTxbx = 0;
    item->lcbPlcffldTxbx = 0;
    item->fcPlcfhdrtxbxTxt = 0;
    item->lcbPlcfhdrtxbxTxt = 0;
    item->fcPlcffldHdrTxbx = 0;
    item->lcbPlcffldHdrTxbx = 0;
    item->fcStwUser = 0;
    item->lcbStwUser = 0;
    item->fcSttbttmbd = 0;
    item->cbSttbttmbd = 0;
    item->fcUnused = 0;
    item->lcbUnused = 0;
    item->fcPgdMother = 0;
    item->lcbPgdMother = 0;
    item->fcBkdMother = 0;
    item->lcbBkdMother = 0;
    item->fcPgdFtn = 0;
    item->lcbPgdFtn = 0;
    item->fcBkdFtn = 0;
    item->lcbBkdFtn = 0;
    item->fcPgdEdn = 0;
    item->lcbPgdEdn = 0;
    item->fcBkdEdn = 0;
    item->lcbBkdEdn = 0;
    item->fcSttbfIntlFld = 0;
    item->lcbSttbfIntlFld = 0;
    item->fcRouteSlip = 0;
    item->lcbRouteSlip = 0;
    item->fcSttbSavedBy = 0;
    item->lcbSttbSavedBy = 0;
    item->fcSttbFnm = 0;
    item->lcbSttbFnm = 0;
    item->fcPlcfLst = 0;
    item->lcbPlcfLst = 0;
    item->fcPlfLfo = 0;
    item->lcbPlfLfo = 0;
    item->fcPlcftxbxBkd = 0;
    item->lcbPlcftxbxBkd = 0;
    item->fcPlcftxbxHdrBkd = 0;
    item->lcbPlcftxbxHdrBkd = 0;
    item->fcDocUndo = 0;
    item->lcbDocUndo = 0;
    item->fcRgbuse = 0;
    item->lcbRgbuse = 0;
    item->fcUsp = 0;
    item->lcbUsp = 0;
    item->fcUskf = 0;
    item->lcbUskf = 0;
    item->fcPlcupcRgbuse = 0;
    item->lcbPlcupcRgbuse = 0;
    item->fcPlcupcUsp = 0;
    item->lcbPlcupcUsp = 0;
    item->fcSttbGlsyStyle = 0;
    item->lcbSttbGlsyStyle = 0;
    item->fcPlgosl = 0;
    item->lcbPlgosl = 0;
    item->fcPlcocx = 0;
    item->lcbPlcocx = 0;
    item->fcPlcfbteLvc = 0;
    item->lcbPlcfbteLvc = 0;
    wvInitFILETIME (&item->ftModified);
    item->fcPlcflvc = 0;
    item->lcbPlcflvc = 0;
    item->fcPlcasumy = 0;
    item->lcbPlcasumy = 0;
    item->fcPlcfgram = 0;
    item->lcbPlcfgram = 0;
    item->fcSttbListNames = 0;
    item->lcbSttbListNames = 0;
    item->fcSttbfUssr = 0;
    item->lcbSttbfUssr = 0;
}

void
wvPutFIB (FIB * item, wvStream * fd)
{
    U16 temp16 = (U16) 0;
    U8 temp8 = (U8) 0;

    write_16ubit (fd, item->nProduct);
    write_16ubit (fd, item->lid);
    write_16ubit (fd, item->pnNext);

    temp16 |= item->fDot;
    temp16 |= item->fGlsy << 1;
    temp16 |= item->fComplex << 2;
    temp16 |= item->fHasPic << 3;
    temp16 |= item->cQuickSaves << 4;
    temp16 |= item->fEncrypted << 8;
    temp16 |= item->fWhichTblStm << 9;
    temp16 |= item->fReadOnlyRecommended << 10;
    temp16 |= item->fWriteReservation << 11;
    temp16 |= item->fExtChar << 12;
    temp16 |= item->fLoadOverride << 13;
    temp16 |= item->fFarEast << 14;
    temp16 |= item->fCrypto << 15;

    /*
       item->fDot = (temp16 & 0x0001);
       item->fGlsy = (temp16 & 0x0002) >> 1;
       item->fComplex = (temp16 & 0x0004) >> 2;
       item->fHasPic = (temp16 & 0x0008) >> 3;
       item->cQuickSaves = (temp16 & 0x00F0) >> 4;
       item->fEncrypted = (temp16 & 0x0100) >> 8;
       item->fWhichTblStm = (temp16 & 0x0200) >> 9;
       item->fReadOnlyRecommended = (temp16 & 0x0400) >> 10;
       item->fWriteReservation = (temp16 & 0x0800) >> 11;
       item->fExtChar = (temp16 & 0x1000) >> 12;
       item->fLoadOverride = (temp16 & 0x2000) >> 13;
       item->fFarEast = (temp16 & 0x4000) >> 14;
       item->fCrypto = (temp16 & 0x8000) >> 15;
     */

    write_16ubit (fd, temp16);

    write_16ubit (fd, (U16) item->nFibBack);
    write_32ubit (fd, item->lKey);
    write_8ubit (fd, (U8) item->envr);

    temp8 |= item->fMac;
    temp8 |= item->fEmptySpecial << 1;
    temp8 |= item->fLoadOverridePage << 2;
    temp8 |= item->fFutureSavedUndo << 3;
    temp8 |= item->fWord97Saved << 4;
    temp8 |= item->fSpare0 << 5;

    /*
       item->fMac = (temp8 & 0x01);
       item->fEmptySpecial = (temp8 & 0x02) >> 1;
       item->fLoadOverridePage = (temp8 & 0x04) >> 2;
       item->fFutureSavedUndo = (temp8 & 0x08) >> 3;
       item->fWord97Saved = (temp8 & 0x10) >> 4;
       item->fSpare0 = (temp8 & 0xFE) >> 5;
     */

    write_8ubit (fd, temp8);

    write_16ubit (fd, (U16) item->chse);
    write_16ubit (fd, item->chsTables);
    write_32ubit (fd, item->fcMin);
    write_32ubit (fd, item->fcMac);
    write_16ubit (fd, item->csw);
    write_16ubit (fd, item->wMagicCreated);
    write_16ubit (fd, item->wMagicRevised);
    write_16ubit (fd, item->wMagicCreatedPrivate);
    write_16ubit (fd, item->wMagicRevisedPrivate);
    write_16ubit (fd, (U16) item->pnFbpChpFirst_W6);
    write_16ubit (fd, (U16) item->pnChpFirst_W6);
    write_16ubit (fd, (U16) item->cpnBteChp_W6);
    write_16ubit (fd, (U16) item->pnFbpPapFirst_W6);
    write_16ubit (fd, (U16) item->pnPapFirst_W6);
    write_16ubit (fd, (U16) item->cpnBtePap_W6);
    write_16ubit (fd, (U16) item->pnFbpLvcFirst_W6);
    write_16ubit (fd, (U16) item->pnLvcFirst);
    write_16ubit (fd, (U16) item->cpnBteLvc);
    write_16ubit (fd, (U16) item->lidFE);
    write_16ubit (fd, (U16) item->clw);

    write_32ubit (fd, (U32) item->cbMac);
    write_32ubit (fd, item->lProductCreated);
    write_32ubit (fd, item->lProductRevised);
    write_32ubit (fd, item->ccpText);
    write_32ubit (fd, (U32) item->ccpFtn);
    write_32ubit (fd, (U32) item->ccpHdr);
    write_32ubit (fd, (U32) item->ccpMcr);
    write_32ubit (fd, (U32) item->ccpAtn);
    write_32ubit (fd, (U32) item->ccpEdn);
    write_32ubit (fd, (U32) item->ccpTxbx);
    write_32ubit (fd, (U32) item->ccpHdrTxbx);
    write_32ubit (fd, (U32) item->pnFbpChpFirst);
    write_32ubit (fd, (U32) item->pnChpFirst);
    write_32ubit (fd, (U32) item->cpnBteChp);
    write_32ubit (fd, (U32) item->pnFbpPapFirst);
    write_32ubit (fd, (U32) item->pnPapFirst);
    write_32ubit (fd, (U32) item->cpnBtePap);
    write_32ubit (fd, (U32) item->pnFbpLvcFirst);
    write_32ubit (fd, (U32) item->pnLvcFirst);
    write_32ubit (fd, (U32) item->cpnBteLvc);
    write_32ubit (fd, (U32) item->fcIslandFirst);
    write_32ubit (fd, (U32) item->fcIslandLim);
    write_16ubit (fd, item->cfclcb);
    write_32ubit (fd, (U32) item->fcStshfOrig);
    write_32ubit (fd, (U32) item->fcStshf);
    write_32ubit (fd, (U32) item->lcbStshf);

    write_32ubit (fd, (U32) item->fcPlcffndRef);
    write_32ubit (fd, item->lcbPlcffndRef);
    write_32ubit (fd, (U32) item->fcPlcffndTxt);
    write_32ubit (fd, item->lcbPlcffndTxt);
    write_32ubit (fd, (U32) item->fcPlcfandRef);
    write_32ubit (fd, item->lcbPlcfandRef);
    write_32ubit (fd, (U32) item->fcPlcfandTxt);
    write_32ubit (fd, item->lcbPlcfandTxt);
    write_32ubit (fd, (U32) item->fcPlcfsed);
    write_32ubit (fd, item->lcbPlcfsed);
    write_32ubit (fd, (U32) item->fcPlcpad);
    write_32ubit (fd, item->lcbPlcpad);
    write_32ubit (fd, (U32) item->fcPlcfphe);
    write_32ubit (fd, item->lcbPlcfphe);
    write_32ubit (fd, (U32) item->fcSttbfglsy);
    write_32ubit (fd, item->lcbSttbfglsy);
    write_32ubit (fd, (U32) item->fcPlcfglsy);
    write_32ubit (fd, item->lcbPlcfglsy);
    write_32ubit (fd, (U32) item->fcPlcfhdd);
    write_32ubit (fd, item->lcbPlcfhdd);
    write_32ubit (fd, (U32) item->fcPlcfbteChpx);
    write_32ubit (fd, item->lcbPlcfbteChpx);
    write_32ubit (fd, (U32) item->fcPlcfbtePapx);
    write_32ubit (fd, item->lcbPlcfbtePapx);
    write_32ubit (fd, (U32) item->fcPlcfsea);
    write_32ubit (fd, item->lcbPlcfsea);
    write_32ubit (fd, (U32) item->fcSttbfffn);
    write_32ubit (fd, item->lcbSttbfffn);
    write_32ubit (fd, (U32) item->fcPlcffldMom);
    write_32ubit (fd, item->lcbPlcffldMom);
    write_32ubit (fd, (U32) item->fcPlcffldHdr);
    write_32ubit (fd, item->lcbPlcffldHdr);
    write_32ubit (fd, (U32) item->fcPlcffldFtn);
    write_32ubit (fd, item->lcbPlcffldFtn);
    write_32ubit (fd, (U32) item->fcPlcffldAtn);
    write_32ubit (fd, item->lcbPlcffldAtn);
    write_32ubit (fd, (U32) item->fcPlcffldMcr);
    write_32ubit (fd, item->lcbPlcffldMcr);
    write_32ubit (fd, (U32) item->fcSttbfbkmk);
    write_32ubit (fd, item->lcbSttbfbkmk);
    write_32ubit (fd, (U32) item->fcPlcfbkf);
    write_32ubit (fd, item->lcbPlcfbkf);
    write_32ubit (fd, (U32) item->fcPlcfbkl);
    write_32ubit (fd, item->lcbPlcfbkl);
    write_32ubit (fd, (U32) item->fcCmds);
    write_32ubit (fd, item->lcbCmds);
    write_32ubit (fd, (U32) item->fcPlcmcr);
    write_32ubit (fd, item->lcbPlcmcr);
    write_32ubit (fd, (U32) item->fcSttbfmcr);
    write_32ubit (fd, item->lcbSttbfmcr);
    write_32ubit (fd, (U32) item->fcPrDrvr);
    write_32ubit (fd, item->lcbPrDrvr);
    write_32ubit (fd, (U32) item->fcPrEnvPort);
    write_32ubit (fd, item->lcbPrEnvPort);
    write_32ubit (fd, (U32) item->fcPrEnvLand);
    write_32ubit (fd, item->lcbPrEnvLand);
    write_32ubit (fd, (U32) item->fcWss);
    write_32ubit (fd, item->lcbWss);
    write_32ubit (fd, (U32) item->fcDop);
    write_32ubit (fd, item->lcbDop);
    write_32ubit (fd, (U32) item->fcSttbfAssoc);
    write_32ubit (fd, item->lcbSttbfAssoc);
    write_32ubit (fd, (U32) item->fcClx);
    write_32ubit (fd, item->lcbClx);
    write_32ubit (fd, (U32) item->fcPlcfpgdFtn);
    write_32ubit (fd, item->lcbPlcfpgdFtn);
    write_32ubit (fd, (U32) item->fcAutosaveSource);
    write_32ubit (fd, item->lcbAutosaveSource);
    write_32ubit (fd, (U32) item->fcGrpXstAtnOwners);
    write_32ubit (fd, item->lcbGrpXstAtnOwners);
    write_32ubit (fd, (U32) item->fcSttbfAtnbkmk);
    write_32ubit (fd, item->lcbSttbfAtnbkmk);
    write_32ubit (fd, (U32) item->fcPlcdoaMom);
    write_32ubit (fd, item->lcbPlcdoaMom);
    write_32ubit (fd, (U32) item->fcPlcdoaHdr);
    write_32ubit (fd, item->lcbPlcdoaHdr);
    write_32ubit (fd, (U32) item->fcPlcspaMom);
    write_32ubit (fd, item->lcbPlcspaMom);
    write_32ubit (fd, (U32) item->fcPlcspaHdr);
    write_32ubit (fd, item->lcbPlcspaHdr);
    write_32ubit (fd, (U32) item->fcPlcfAtnbkf);
    write_32ubit (fd, item->lcbPlcfAtnbkf);
    write_32ubit (fd, (U32) item->fcPlcfAtnbkl);
    write_32ubit (fd, item->lcbPlcfAtnbkl);
    write_32ubit (fd, (U32) item->fcPms);
    write_32ubit (fd, item->lcbPms);
    write_32ubit (fd, (U32) item->fcFormFldSttbs);
    write_32ubit (fd, item->lcbFormFldSttbs);
    write_32ubit (fd, (U32) item->fcPlcfendRef);
    write_32ubit (fd, item->lcbPlcfendRef);
    write_32ubit (fd, (U32) item->fcPlcfendTxt);
    write_32ubit (fd, item->lcbPlcfendTxt);
    write_32ubit (fd, (U32) item->fcPlcffldEdn);
    write_32ubit (fd, item->lcbPlcffldEdn);
    write_32ubit (fd, (U32) item->fcPlcfpgdEdn);
    write_32ubit (fd, item->lcbPlcfpgdEdn);
    write_32ubit (fd, (U32) item->fcDggInfo);
    write_32ubit (fd, item->lcbDggInfo);
    write_32ubit (fd, (U32) item->fcSttbfRMark);
    write_32ubit (fd, item->lcbSttbfRMark);
    write_32ubit (fd, (U32) item->fcSttbCaption);
    write_32ubit (fd, item->lcbSttbCaption);
    write_32ubit (fd, (U32) item->fcSttbAutoCaption);
    write_32ubit (fd, item->lcbSttbAutoCaption);
    write_32ubit (fd, (U32) item->fcPlcfwkb);
    write_32ubit (fd, item->lcbPlcfwkb);
    write_32ubit (fd, (U32) item->fcPlcfspl);
    write_32ubit (fd, item->lcbPlcfspl);
    write_32ubit (fd, (U32) item->fcPlcftxbxTxt);
    write_32ubit (fd, item->lcbPlcftxbxTxt);
    write_32ubit (fd, (U32) item->fcPlcffldTxbx);
    write_32ubit (fd, item->lcbPlcffldTxbx);
    write_32ubit (fd, (U32) item->fcPlcfhdrtxbxTxt);
    write_32ubit (fd, item->lcbPlcfhdrtxbxTxt);
    write_32ubit (fd, (U32) item->fcPlcffldHdrTxbx);
    write_32ubit (fd, item->lcbPlcffldHdrTxbx);
    write_32ubit (fd, (U32) item->fcStwUser);
    write_32ubit (fd, item->lcbStwUser);
    write_32ubit (fd, (U32) item->fcSttbttmbd);
    write_32ubit (fd, item->cbSttbttmbd);
    write_32ubit (fd, (U32) item->fcUnused);
    write_32ubit (fd, item->lcbUnused);
    write_32ubit (fd, (U32) item->fcPgdMother);
    write_32ubit (fd, item->lcbPgdMother);
    write_32ubit (fd, (U32) item->fcBkdMother);
    write_32ubit (fd, item->lcbBkdMother);
    write_32ubit (fd, (U32) item->fcPgdFtn);
    write_32ubit (fd, item->lcbPgdFtn);
    write_32ubit (fd, (U32) item->fcBkdFtn);
    write_32ubit (fd, item->lcbBkdFtn);
    write_32ubit (fd, (U32) item->fcPgdEdn);
    write_32ubit (fd, item->lcbPgdEdn);
    write_32ubit (fd, (U32) item->fcBkdEdn);
    write_32ubit (fd, item->lcbBkdEdn);
    write_32ubit (fd, (U32) item->fcSttbfIntlFld);
    write_32ubit (fd, item->lcbSttbfIntlFld);
    write_32ubit (fd, (U32) item->fcRouteSlip);
    write_32ubit (fd, item->lcbRouteSlip);
    write_32ubit (fd, (U32) item->fcSttbSavedBy);
    write_32ubit (fd, item->lcbSttbSavedBy);
    write_32ubit (fd, (U32) item->fcSttbFnm);
    write_32ubit (fd, item->lcbSttbFnm);
    write_32ubit (fd, (U32) item->fcPlcfLst);
    write_32ubit (fd, item->lcbPlcfLst);
    write_32ubit (fd, (U32) item->fcPlfLfo);
    write_32ubit (fd, item->lcbPlfLfo);
    write_32ubit (fd, (U32) item->fcPlcftxbxBkd);
    write_32ubit (fd, item->lcbPlcftxbxBkd);
    write_32ubit (fd, (U32) item->fcPlcftxbxHdrBkd);
    write_32ubit (fd, item->lcbPlcftxbxHdrBkd);
    write_32ubit (fd, (U32) item->fcDocUndo);
    write_32ubit (fd, item->lcbDocUndo);
    write_32ubit (fd, (U32) item->fcRgbuse);
    write_32ubit (fd, item->lcbRgbuse);
    write_32ubit (fd, (U32) item->fcUsp);
    write_32ubit (fd, item->lcbUsp);
    write_32ubit (fd, (U32) item->fcUskf);
    write_32ubit (fd, item->lcbUskf);
    write_32ubit (fd, (U32) item->fcPlcupcRgbuse);
    write_32ubit (fd, item->lcbPlcupcRgbuse);
    write_32ubit (fd, (U32) item->fcPlcupcUsp);
    write_32ubit (fd, item->lcbPlcupcUsp);
    write_32ubit (fd, (U32) item->fcSttbGlsyStyle);
    write_32ubit (fd, item->lcbSttbGlsyStyle);
    write_32ubit (fd, (U32) item->fcPlgosl);
    write_32ubit (fd, item->lcbPlgosl);
    write_32ubit (fd, (U32) item->fcPlcocx);
    write_32ubit (fd, item->lcbPlcocx);
    write_32ubit (fd, (U32) item->fcPlcfbteLvc);
    write_32ubit (fd, item->lcbPlcfbteLvc);

    write_32ubit (fd, item->ftModified.dwLowDateTime);
    write_32ubit (fd, item->ftModified.dwHighDateTime);

    /* wvGetFILETIME(&(item->ftModified),fd); */

    /* wvPutFILETIME(&(item->ftModified), fd) */

    write_32ubit (fd, (U32) item->fcPlcflvc);
    write_32ubit (fd, item->lcbPlcflvc);
    write_32ubit (fd, (U32) item->fcPlcasumy);
    write_32ubit (fd, item->lcbPlcasumy);
    write_32ubit (fd, (U32) item->fcPlcfgram);
    write_32ubit (fd, item->lcbPlcfgram);
    write_32ubit (fd, (U32) item->fcSttbListNames);
    write_32ubit (fd, item->lcbSttbListNames);
    write_32ubit (fd, (U32) item->fcSttbfUssr);
    write_32ubit (fd, item->lcbSttbfUssr);
}
