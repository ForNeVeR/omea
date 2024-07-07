
/* Various support functions for xlhtml. */

#include <stdio.h>
#include "version.h"
#include <time.h>
#include "../cole/cole.h"
#include <stdlib.h>

void print_version(void)
{
    printf("xlhtml %s \nCopyright (c) 1999-2002, Charles Wyble\n" 
    "Released under GPL.\n", VERSION );
    exit(0);
}

void display_usage(void)
{
fprintf(stderr, "\nxlhtml  converts excel files (.xls) to Html.\n"
    "Copyright (c) 1999-2001, Charles Wyble. Released under GPL.\n"
"Usage: xlhtml [-xp:# -xc:#-# -xr:#-# -bc###### -bi???????? -tc######] <FILE>\n"
    "\t-a:  aggressive html optimization\n"
    "\t-asc ascii output for -dp & -x? options\n"
    "\t-csv comma separated value output for -dp & -x? options\n"
    "\t-xml XML output\n"
    "\t-bc: Set default background color - default white\n"
    "\t-bi: Set background image path\n"
    "\t-c:  Center justify tables\n"
    "\t-dp: Dumps page count and max rows & colums per page\n"
    "\t-v:  Prints program version number\n"
    "\t-fw: Suppress formula warnings\n"
    "\t-nd: Suppress all disclamers\n"
    "\t-m:  No encoding for multibyte\n"
    "\t-nc: No Colors - black & white\n"
    "\t-nh: No Html Headers\n"
    "\t-tc: Set default text color - default black\n"
    "\t-te: Trims empty rows & columns at the edges of a worksheet\n"
    "\t-xc: Columns (separated by a dash) for extraction (zero based)\n"
    "\t-xp: Page extracted (zero based)\n"
    "\t-xr: Rows (separated by a dash) to be extracted (zero based)\n");
    fprintf(stderr, "\nReport bugs to jackshck@yahoo.com\n");
    exit (1);
}

void do_cr(void)
{
    extern int aggressive;
    if (!aggressive)
        putchar('\n');
}

U16 getShort(U8 *ptr)
{
    if (ptr == 0)
        return (U16)0;
    
    return (U16)((*(ptr+1)<<8)+*ptr);
}

/*! This is used in the RK number, so signedness counts */
S32 getLong(U8 *ptr)
{
    if (ptr == 0)
        return (S32)0;

    return (S32)(*(ptr+3)<<24)+(*(ptr+2)<<16)+(*(ptr+1)<<8)+*ptr;
}

#ifndef WORDS_BIGENDIAN             /* Defined in <config.h> */
/*! Little Endian - 0x86 family */
void getDouble(U8 *ptr, F64 *d)
{
    size_t i;
    F64 dd;
    U8 *t = (U8 *)&dd;

    for (i=0; i<sizeof(F64); i++)
        *(t+i) = *(ptr+i);

    *d = (F64)dd;
}
#else
/*! Big Endian version - UltraSparc's, etc. */
void getDouble (U8 *ptr, F64 *d)
{
    size_t i;
    F64 dd;
    U8 *t = (U8 *)&dd;

    for (i=0; i<sizeof(F64); i++)
        *(t+i) = *(ptr+sizeof(F64) - 1 - i);

    *d = (F64)dd;
}
#endif

int null_string(U8 *str)
{   /* FIXME: This function may not be unicode safe */
    U8 *ptr;
    if ((str == NULL)||(*str == 0))
        return 1;

    ptr = str;
    while (*ptr != 0)
    {
        if (*ptr++ != ' ')
            return 0;
    }
    return 1;
}

void FracToTime(U8 *cnum, int *hr, int *minut, int *sec, int *msec)
{
    int Hr, Min, Sec, Msec;
    F64 fnum, tHr, tMin, tSec, tMsec;

    if (msec)
        fnum = atof((char *)&cnum[0])+(0.05 / 86400.0); /* Round off to 1/10th seconds */
    else if (sec)
        fnum = atof((char *)&cnum[0])+(0.5 / 86400.0);  /* Round off to seconds */
    else
        fnum = atof((char *)&cnum[0])+(30 / 86400.0);   /* Round off to minutes */
    tHr = 24.0 * fnum;
    Hr = (int)tHr;
    tMin = (tHr - (F64)Hr) * 60.0;
    Min = (int)tMin;
    tSec = (tMin - (F64)Min) * 60.0;
    Sec = (int)tSec;
    tMsec = (tSec - (F64)Sec) * 10.0;
    Msec = (int)tMsec;

    Hr = Hr%24; /* Fix roll-overs */
    if (hr)
        *hr = Hr;
    if (minut)
        *minut = Min;
    if (sec)
        *sec = Sec;
    if (msec)
        *msec = Msec;
}


void NumToDate(long num, int *year, int *month, int *day)
{
    const int ldays[]={31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
    const int ndays[]={31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
    
    extern int DatesR1904;

    int t, i, y = 0;

    num = num%36525L;   /* Trim century */
    while (num > (((y%4) == 0) ? 366 : 365))
        num -= ((y++%4) == 0) ? 366 : 365;

    *year = y;
    t = num;
    if (DatesR1904)
        *year += 4;     /* Adjust for McIntosh... */
    if ((*year%4) == 0)
    {   /* Leap Year */
        for (i=0; i<12; i++)
        {
            if (t <= ldays[i])
                break;
            t -= ldays[i];
        }
    }
    else
    {
        for (i=0; i<12; i++)
        {
            if (t <= ndays[i])
                break;
            t -= ndays[i];
        }
    }
    /* Some fixups... */
    *month = 1+i;
    if (t == 0)
        t = 1;
    *day = t;
    *year = *year % 100;
}

/* noaliasdub macro avoids trouble from gcc -O2 type-based alias analysis */
typedef S32 swords[2];
#define noaliasdub(type,ptr) \
  (((union{swords sw; F64 dub;} *)(ptr))->sw)

#ifndef WORDS_BIGENDIAN             /*! Defined in <config.h> */
/*! Little Endian - 0x86 family */
void RKtoDouble(S32 n, F64 *d)
{
  noaliasdub(swords,d)[0] = 0;
  noaliasdub(swords,d)[1] =  n << 2;
}
#else
/*! Big Endian version - UltraSparc's, etc. */
void RKtoDouble(S32 n, F64 *d)
{
    U8 *ptr = (U8 *)&n;

    noaliasdub(swords,d)[1] = 0;
    noaliasdub(swords,d)[0] =
      ((*(ptr+0)<<24)+(*(ptr+1)<<16)+(*(ptr+2)<<8)+(*(ptr+3))) << 2;
}
#endif
