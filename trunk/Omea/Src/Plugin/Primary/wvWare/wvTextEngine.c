#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

int
wvText (wvParseStruct * ps)
{
    if (ps->fib.fComplex)
	wvDecodeComplex (ps);
    else
	wvDecodeSimple (ps, Dmain);
    return (0);
}
