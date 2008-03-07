#include <stdio.h>
#include <stdlib.h>
#include <ctype.h>
#include <string.h>
#include <time.h>
#include <math.h>
#include <assert.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "config.h"
#include "wv.h"
#include "oledecod.h"

#include "wvexporter-priv.h"

wvStream *
wvStream_new (wvDocument * ole_file, const char *name)
{
    MsOleStream *temp_stream;
    wvStream *ret;
    ms_ole_stream_open (&temp_stream, ole_file, "/", name, 'w');
    wvStream_libole2_create (&ret, temp_stream);
    return (ret);
}
