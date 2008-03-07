#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"

/* 
basically the definition of a twip is that there
are 1440 twips per inch, now for html we need this
figure in pixels, so we have to take some screen
resolution as a standard to work from.

if we were to take hozitontal twips and a 1280 pixel
wide screen then there are
1440 twips per 75 pixels

if we were to take vertical twips and a 1024 pixel
high screen then there are
1440 twips per 75 pixels
*/

#define TWIPS_PER_INCH 1440
#if 0
#define PIXELS_PER_H_INCH 75
#define PIXELS_PER_V_INCH 75
#else
#define PIXELS_PER_H_INCH 100
#define PIXELS_PER_V_INCH 100
#endif

static S16 pperhi = PIXELS_PER_H_INCH;
static S16 ppervi = PIXELS_PER_V_INCH;

void
wvSetPixelsPerInch (S16 hpixels, S16 vpixels)
{
    pperhi = hpixels;
    ppervi = vpixels;
}


float
wvTwipsToHPixels (S16 twips)
{
    float ret = ((float) (pperhi * twips)) / TWIPS_PER_INCH;
    wvTrace (("ret is %f\n", ret));
    return (ret);
}

float
wvTwipsToVPixels (S16 twips)
{
    float ret = ((float) (ppervi * twips)) / TWIPS_PER_INCH;
    wvTrace (("ret is %f\n", ret));
    return (ret);
}

float
wvTwipsToMM (S16 twips)
{
    float ret;
    ret = ((float) twips) / TWIPS_PER_INCH;
    ret = ret * (float) 25.0;
    return (ret);
}

/* [A twip ] is one-twentieth of a point size*/
float
wvPointsToMM (S16 points)
{
    return (wvTwipsToMM ((S16) (points * 20)));
}
