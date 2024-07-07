#ifndef WVEXPORTER_H
#define WVEXPORTER_H

#include "wv.h"

#ifdef __cplusplus
extern "C" {
#endif				/* c++ */

    /* This is our exportation abstraction layer.  
     * Each wvDocument maps to one file,
     * which streams can be opened within.
     */
    typedef MsOle wvDocument;

    /* Opaque DOC structure */

    typedef struct _wvExporter wvExporter;

    /* Creating, Querying, and Destroying DOC objects  */

    S8 wvExporter_queryVersionSupported (wvVersion v);
    wvExporter *wvExporter_create (const char *filename);
    wvExporter *wvExporter_create_version (const char *filename, wvVersion v);
    void wvExporter_close (wvExporter * exp);
    wvVersion wvExporter_getVersion (wvExporter * exp);

    /* Exporting to OLE summary streams */

    S8 wvExporter_summaryPutString (wvExporter * exp,
				    U32 field, const char *str);
    S8 wvExporter_summaryPutLong (wvExporter * exp, U32 field, U32 l);
    S8 wvExporter_summaryPutTime (wvExporter * exp, U32 field, time_t t);

    /* Writing streams of text to a Word DOC */

    size_t wvExporter_writeChars (wvExporter * exp, const U8 * chars);
    size_t wvExporter_writeBytes (wvExporter * exp,
				  size_t sz, size_t nmemb, const void *bytes);
    void wvExporter_flush (wvExporter * exp);

    /* Changing document properities */

    S8 wvExporter_pushPAP (wvExporter * exp, PAP * apap);
    S8 wvExporter_pushCHP (wvExporter * exp, CHP * achp);
    S8 wvExporter_pushSEP (wvExporter * exp, SEP * asep);

#ifdef __cplusplus
}
#endif				/* c++ */
#endif				/* WVEXPORTER_H */
