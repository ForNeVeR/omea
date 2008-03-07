/* for now, just for testing purposes */
#define DEBUG 1

#include <stdlib.h>
#include <string.h>

#ifdef HAVE_CONFIG_H
#include "config.h"
#endif

#if 0
#include <malloc.h>
#endif

#include "wvexporter-priv.h"
#include "ms-ole-summary.h"

  /* the temporary storage struct for our summay data 
   * for important reasons, this gets written out
   * at the end of the export, after the other streams
   * are closed
   */
typedef struct _OleSummaryData OleSummaryData;

struct _OleSummaryData {
    char *title;
    char *subject;
    char *author;
    char *keywords;
    char *comments;
    char *template;
    char *lastauthor;
    char *revnumber;
    char *appname;

    U32 pagecount;
    U32 wordcount;
    U32 charcount;
    U32 security;
    U32 thumbnail;

    time_t total_edittime;
    time_t lastprinted;
    time_t created;
    time_t lastsaved;
};

struct _wvExporter {

    /* consider all of this data to be private 
     * the only reliable way to get/manipulate
     * data is through the exposed API
     */

    /* our toplevel OLE Document 
     * from which all of our following streams
     * are created
     */
    wvDocument *ole;

    /* the open OLE streams within the toplevel OLE document */
    wvStream *documentStream;
    wvStream *table1Stream;
    wvStream *table0Stream;
    wvStream *dataStream;

    /* more accounting structures */

    OleSummaryData *summary;
    wvVersion ver;
    FIB fib;
};

/* document stream names */
#define DOCUMENT_STREAM  "WordDocument"
#define TABLE1_STREAM    "1Table"
#define TABLE0_STREAM    "0Table"
#define DATA_STREAM      "Data"

#define ASSIGN_STRING(a, b) assign_string(&a, b)
#define SUMMARY_SET_STRING(a, b, c) if(c) ms_ole_summary_set_string (a, b, c)

#define ASSERT_STREAM_CREATED(s) \
if(!s) { \
  wvError(("Error creating %s stream\n", #s));\
  return NULL;\
}

#define WVSTREAM_CLOSE(s) \
  ms_ole_stream_close(&((s)->stream.libole_stream))

/* some static functions */

static void assign_string (char **a, const char *b);
static void exporter_close_word8 (wvExporter * exp);
static wvExporter *exporter_create_word8 (const char *filename);
static void write_ole_summary (OleSummaryData * data, MsOleSummary * strm);

/**************************************************************************/
/**                   Public implementation functions                    **/
/**************************************************************************/


/**
 * wvExporter_queryVerionSupported
 *
 * @returns 1 if we can export to version # @v
 * Of the MSWord format or 0 if not
 *
 * Currently Supported Versions: WORD8
 *
 * @v - version
 */
S8 wvExporter_queryVersionSupported (wvVersion v)
{
    switch (v)
      {
      case WORD8:
	  return 1;
      default:
	  return 0;
      }
}

/**
 * wvExporter_getVersion
 *
 * @exp - a valid exporter created by wvExporter_create
 *
 * @returns the version type of the wvExporter @exp, 0 on error
 */
wvVersion wvExporter_getVersion (wvExporter * exp)
{
    if (!exp)
      {
	  wvError (("Attempt to get version from a NULL exporter\n"));
	  return (wvVersion) 0;
      }
    return exp->ver;
}

/**
 * wvExporter_create_version
 *
 * Creates a MSWord exporter object, with the version @v
 * If version @v isn't supported, return NULL. Call
 * <code>wvExporter_queryVersionSupported(@v)</code> to see
 * If @v is a supported version
 *
 * @filename - file on disk to create 
 * @v - version of Word Format to create. Only valid for
 *      versions where wvExporter_queryVersionSupporter (@v) == 1
 *
 * @returns <code>NULL</code> on error, or a valid wvExporter on success
 */
wvExporter *
wvExporter_create_version (const char *filename, wvVersion v)
{
    if (!wvExporter_queryVersionSupported (v))
      {
	  wvError (("wvExporter: unsupported version Word%d", (int) v));
	  return NULL;
      }

    switch (v)
      {
      case WORD8:
	  return exporter_create_word8 (filename);
      default:
	  wvError (("Cannot create unsupported version: %d\n", (int) v));
	  return NULL;
      }
}

/**
 * wvExporter_create
 *
 * Creates a Word exporter object. Guaranteed to return 
 * an exporter for the most recent supported MSWord DOC version.
 * This means that this function currently creates a Word97/2000
 * exporter object (WORD8)
 *
 * @filename - file on disk to create 
 * @returns <code>NULL</code> on error, or a valid wvExporter on success
 */
wvExporter *
wvExporter_create (const char *filename)
{
    return wvExporter_create_version (filename, WORD8);
}

/**
 * wvExporter_close
 *
 * Closes and saves the MSWord document
 *
 * @exp - an exporter created by wvExporter_create()
 */
void
wvExporter_close (wvExporter * exp)
{
    if (exp == NULL)
      {
	  wvError (("Exporter can't be null\n"));
	  return;
      }

    switch (wvExporter_getVersion (exp))
      {
      case WORD8:
	  exporter_close_word8 (exp);
	  break;

      default:
	  wvError (("Closing wvExporter with an invalid version\n"));
	  break;
      }

    wvTrace (("Word Document Written!\n"));
}

/**************************************************************************/
/** All of the wvExporter_summaryXXX functions share 1 common            **/
/** Characteristic: only the final call for any given value is of any    **/
/** Importance. This means that one could call                           **/
/** wvExporter_summaryPutString() for PID_TITLE 35 times, but only the   **/
/** Final call means anything                                            **/
/**************************************************************************/

/**
 * wvExporter_summaryPutString
 *
 * @exp - a valid wvExporter created by wvExporter_create()
 * @field - summary stream id key PID_XXX from "wv.h"
 * @str - string to put into the summary stream
 *
 * If field isn't a valid value or isn't valid for the given
 * type (i.e. string), this function will return 0
 *
 * @returns 1 on success, 0 on failure
 */
S8 wvExporter_summaryPutString (wvExporter * exp, U32 field, const char *str)
{
    if (exp == NULL)
      {
	  wvError (("Exporter can't be null\n"));
	  return 0;
      }
    if (str == NULL)
      {
	  wvError (("String can't be null\n"));
	  return 0;
      }

    switch (field)
      {
	  /* summary stream */
      case PID_TITLE:
	  ASSIGN_STRING (exp->summary->title, str);
	  break;
      case PID_SUBJECT:
	  ASSIGN_STRING (exp->summary->subject, str);
	  break;
      case PID_AUTHOR:
	  ASSIGN_STRING (exp->summary->author, str);
	  break;
      case PID_KEYWORDS:
	  ASSIGN_STRING (exp->summary->keywords, str);
	  break;
      case PID_COMMENTS:
	  ASSIGN_STRING (exp->summary->comments, str);
	  break;
      case PID_TEMPLATE:
	  ASSIGN_STRING (exp->summary->template, str);
	  break;
      case PID_LASTAUTHOR:
	  ASSIGN_STRING (exp->summary->lastauthor, str);
	  break;
      case PID_REVNUMBER:
	  ASSIGN_STRING (exp->summary->revnumber, str);
	  break;
      case PID_APPNAME:
	  ASSIGN_STRING (exp->summary->appname, str);
	  break;
      default:
	  wvError (("Unhandled type: %d\n", field));
	  return 0;
      }

    return 1;
}

/**
 * wvExporter_summaryPutLong
 *
 * @exp - a valid wvExporter created by wvExporter_create()
 * @field - summary stream id key PID_XXX from "wv.h"
 * @l - long value
 *
 * If field isn't a valid value or isn't valid for the given
 * type (i.e. long), this function will return 0
 *
 * @returns 1 on success, 0 on failure
 */
S8 wvExporter_summaryPutLong (wvExporter * exp, U32 field, U32 l)
{
    if (exp == NULL)
      {
	  wvError (("Exporter can't be null\n"));
	  return 0;
      }

    switch (field)
      {
	  /* summary stream */
      case PID_PAGECOUNT:
	  exp->summary->pagecount = l;
	  break;
      case PID_WORDCOUNT:
	  exp->summary->wordcount = l;
	  break;
      case PID_CHARCOUNT:
	  exp->summary->charcount = l;
	  break;
      case PID_SECURITY:
	  exp->summary->security = l;
	  break;
      case PID_THUMBNAIL:
	  exp->summary->thumbnail = l;
	  break;

      default:
	  wvError (("Unhandled type: %d\n", field));
	  return 0;
      }

    return 1;
}

/**
 * wvExporter_summaryPutTime
 *
 * @exp - a valid wvExporter created by wvExporter_create()
 * @field - summary stream id key PID_XXX from "wv.h"
 * @t - UNIX time_t value
 *
 * If @field isn't a valid value or isn't valid for the given
 * type (i.e. time_t), this function will return 0
 *
 * @returns 1 on success, 0 on failure
 */
S8 wvExporter_summaryPutTime (wvExporter * exp, U32 field, time_t t)
{
    if (exp == NULL)
      {
	  wvError (("Exporter can't be null\n"));
	  return 0;
      }

    switch (field)
      {
	  /* summary stream only */
      case PID_TOTAL_EDITTIME:
	  exp->summary->total_edittime = t;
	  break;
      case PID_LASTPRINTED:
	  exp->summary->lastprinted = t;
	  break;
      case PID_CREATED:
	  exp->summary->created = t;
	  break;
      case PID_LASTSAVED:
	  exp->summary->lastsaved = t;
	  break;
      default:
	  wvError (("Unhandled type: %d\n", field));
	  return 0;
      }

    return 1;
}

/**************************************************************************/
/**************************************************************************/

/**
 * wvExporter_writeChars
 *
 * If you're worried, use wvExporter_writeBytes instead.
 *
 * Writes the string @chars to the Word document @exp
 * You should be passing UTF8 here
 *
 * @returns number of chars written
 */
size_t wvExporter_writeChars (wvExporter * exp, const U8 * chars)
{
    if (exp == NULL)
      {
	  wvError (("Exporter can't be NULL\n"));
	  return 0;
      }
    if (chars == NULL)
      {
	  wvError (("I won't write a NULL string\n"));
	  return 0;
      }

    return wvExporter_writeBytes (exp, sizeof (U8), 
				  strlen ((const char *)chars), 
				  (const void *) chars);
}

/**
 * wvExporter_writeBytes
 *
 * Should be UTF-safe
 *
 * Writes @nmemb members from the array of @bytes of 
 * in size @sz chunks to the @exp word document
 *
 * @returns number of bytes written
 */
size_t
wvExporter_writeBytes (wvExporter * exp, size_t sz, size_t nmemb,
		       const void *bytes)
{
    size_t nwr = 0;

    if (exp == NULL)
      {
	  wvError (("Exporter can't be NULL\n"));
	  return 0;
      }
    if (sz == 0)
      {
	  wvError (("Attempting to write an array of zero size items? WTF?\n"));
	  return 0;
      }
    if (nmemb == 0)
      {
	  /* not so bad I guess */
	  wvTrace (("Zero bytes passed to writeBytes\n"));
	  return 0;
      }
    if (bytes == 0)
      {
	  /* TODO: is this an error? */
	  wvTrace (("NULL array passed to writeBytes\n"));
	  return 0;
      }

    /* write the bytes and update the FIB */
    nwr = wvStream_write ((void *) bytes, sz, nmemb, exp->documentStream);
    exp->fib.fcMac = wvStream_tell (exp->documentStream) + 1;

    wvTrace (("Wrote %d byte(s)\n", nwr));
    return nwr;
}

/**
 * wvExporter_flush
 *
 * Flushes any data possibly stored in the exporter's
 * Internal buffers.
 *
 * @exp - an exporter created by wvExporter_create
 */
void
wvExporter_flush (wvExporter * exp)
{
    if (!exp)
      {
	  wvError (("Cannot flush a null exporter object\n"));
	  return;
      }
    /* this will be a noop indefinitely */
}

/**************************************************************************/
/**************************************************************************/

/**
 * wvExporter_pushPAP
 *
 * This function closes any previous paragraph, if any are
 * Open, and begins a new paragraph with the properties
 * Contained in @apap
 *
 * @returns 1 on success, 0 if not
 */
S8 wvExporter_pushPAP (wvExporter * exp, PAP * apap)
{
    if (!exp)
      {
	  wvError (("NULL exporter\n"));
	  return 0;
      }

    if (!apap)
      {
	  wvError (("NULL PAP!\n"));
	  return 0;
      }

    /* noop for now */

    return 1;
}

/**
 * wvExporter_pushCHP
 *
 * This function closes any previous character run, if any are
 * Open, and begins a new character run with the properties
 * Contained in @achp
 *
 * @returns 1 on success, 0 if not
 */
S8 wvExporter_pushCHP (wvExporter * exp, CHP * achp)
{
    if (!exp)
      {
	  wvError (("NULL exporter\n"));
	  return 0;
      }

    if (!achp)
      {
	  wvError (("NULL CHP!\n"));
	  return 0;
      }

    /* noop for now */

    return 1;
}

/**
 * wvExporter_pushSEP
 *
 * This function closes any previous section, if any are
 * Open, and begins a new section with the properties
 * Contained in @asep
 *
 * @returns 1 on success, 0 if not
 */
S8 wvExporter_pushSEP (wvExporter * exp, SEP * asep)
{
    if (!exp)
      {
	  wvError (("NULL exporter\n"));
	  return 0;
      }

    if (!asep)
      {
	  wvError (("NULL SEP!\n"));
	  return 0;
      }

    /* noop for now */

    return 1;
}


/**************************************************************************/
/**                   Static implementation functions                    **/
/**************************************************************************/

static void
assign_string (char **a, const char *b)
{
    int len = 0;

    if (!b)
	return;

    if (*a)
	wvFree (*a);

    len = strlen (b);
    (*a) = (char *) wvMalloc (sizeof (char) * (len + 1));
    strcpy (*a, b);
    (*a)[len] = 0;
}

static void
write_ole_summary (OleSummaryData * data, MsOleSummary * sum)
{
    SUMMARY_SET_STRING (sum, MS_OLE_SUMMARY_TITLE, data->title);
    SUMMARY_SET_STRING (sum, MS_OLE_SUMMARY_SUBJECT, data->subject);
    SUMMARY_SET_STRING (sum, MS_OLE_SUMMARY_AUTHOR, data->author);
    SUMMARY_SET_STRING (sum, MS_OLE_SUMMARY_KEYWORDS, data->keywords);
    SUMMARY_SET_STRING (sum, MS_OLE_SUMMARY_COMMENTS, data->comments);
    SUMMARY_SET_STRING (sum, MS_OLE_SUMMARY_TEMPLATE, data->template);
    SUMMARY_SET_STRING (sum, MS_OLE_SUMMARY_LASTAUTHOR, data->lastauthor);
    SUMMARY_SET_STRING (sum, MS_OLE_SUMMARY_REVNUMBER, data->revnumber);
    SUMMARY_SET_STRING (sum, MS_OLE_SUMMARY_APPNAME, data->appname);

    ms_ole_summary_set_long (sum, MS_OLE_SUMMARY_PAGECOUNT, data->pagecount);
    ms_ole_summary_set_long (sum, MS_OLE_SUMMARY_WORDCOUNT, data->wordcount);
    ms_ole_summary_set_long (sum, MS_OLE_SUMMARY_CHARCOUNT, data->charcount);
    ms_ole_summary_set_long (sum, MS_OLE_SUMMARY_SECURITY, data->security);
    ms_ole_summary_set_long (sum, MS_OLE_SUMMARY_THUMBNAIL, data->thumbnail);

#if 0
    /* TODO: the time types are not currently supported */
#endif
}

static wvExporter *
exporter_create_word8 (const char *filename)
{
    wvExporter *exp = NULL;
    MsOle *ole = NULL;

    if (filename == NULL)
      {
	  wvError (("Error: file name can't be null\n"));
	  return NULL;
      }

    /* first allocate the exporter object, initialized to 0's */
    exp = (wvExporter *) calloc (1, sizeof (wvExporter));
    if (!exp)
      {
	  wvError (("Error allocating memory for the exporter\n"));
	  return NULL;
      }

    if (ms_ole_create ((MsOle **) (&ole), filename) != MS_OLE_ERR_OK)
      {
	  wvError (("Error creating OLE docfile %s\n", filename));
	  wvFree (ole);
	  wvFree (exp);
	  return NULL;
      }

    wvTrace (("Created OLE\n"));
    exp->ole = (wvDocument *) ole;

    /* now to initialize the streams */

    exp->documentStream = wvStream_new (ole, DOCUMENT_STREAM);
    ASSERT_STREAM_CREATED (exp->documentStream);

    exp->table0Stream = wvStream_new (ole, TABLE0_STREAM);
    ASSERT_STREAM_CREATED (exp->table0Stream);

    exp->table1Stream = wvStream_new (ole, TABLE1_STREAM);
    ASSERT_STREAM_CREATED (exp->table1Stream);

    exp->dataStream = wvStream_new (ole, DATA_STREAM);
    ASSERT_STREAM_CREATED (exp->dataStream);

    wvTrace (("Created all relevant OLE streams\n"));

    /* initialize the FIB and put it into the document stream
     * this is fine to do, since we're going to rewind the
     * stream, and put an updated FIB in the stream on
     * wvExporter_close() anyway
     */
    wvInitFIBForExport (&(exp->fib));
    wvPutFIB (&(exp->fib), exp->documentStream);
    wvTrace (
	     ("Initial FIB inserted at: %d (%d)\n",
	      wvStream_tell (exp->documentStream),
	      (wvStream_tell (exp->documentStream) - sizeof (FIB))));

    /* in all of the document's i've run into, the fcMin == 1024 */
    exp->fib.fcMin = wvStream_tell (exp->documentStream);

    exp->ver = WORD8;
    exp->summary = (OleSummaryData *) calloc (1, sizeof (OleSummaryData));
    return exp;
}

static void
exporter_close_word8 (wvExporter * exp)
{
    MsOleSummary *sum;

    wvExporter_flush (exp);

    /* rewind and put the updated FIB in its proper place */

    /* last character's position + 1 */
    /* exp->fib.cbMac = wvStream_tell(exp->documentStream) + 1; */
    exp->fib.ccpText = exp->fib.cbMac - exp->fib.fcMin;

    wvStream_rewind (exp->documentStream);
    wvPutFIB (&(exp->fib), exp->documentStream);
    wvTrace (("Re-inserted FIB into document at: %d\n",
	      wvStream_tell (exp->documentStream)));

    /*
     * Close all of the streams
     * TODO: make this a function instead of a macro
     */
    WVSTREAM_CLOSE ((exp->documentStream));
    WVSTREAM_CLOSE ((exp->table1Stream));
    WVSTREAM_CLOSE ((exp->table0Stream));
    WVSTREAM_CLOSE ((exp->dataStream));
    wvTrace (("Closed all of the main streams\n"));

    /*
     * Close the summary streams
     */
    sum = ms_ole_summary_create (exp->ole);
    write_ole_summary (exp->summary, sum);
    ms_ole_summary_close (sum);

    wvTrace (("Wrote summary stream(s)\n"));

    /* close the document */
    ms_ole_destroy (&(exp->ole));
    wvTrace (("Closed all of the streams and OLE\n"));

    wvFree (exp->summary);
    wvFree (exp);
    exp = NULL;
}
