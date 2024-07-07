#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

ReasonTable reasons[] = {
    {"Nothing wrong", 0},
    {"This is an unsupported (as of yet) pre word 6 doc, sorry", 1},
    {"This is an unsupported (as of yet) word 6 doc", 2},
    {"This is an unsupported (as of yet) word 7 doc", 3},
    {"This is an unsupported (as of yet) encrypted document", 4},
    {"This is not a word document", 5}

};

const char *
wvReason (int reason)
{
    return (reasons[reason].m_name);
}
