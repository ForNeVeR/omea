#ifdef HAVE_CONFIG_H
#include "config.h"
#endif

extern char *sys_errlist[];

char *
strerror (int errnum)
{
    return sys_errlist[errnum];
}
