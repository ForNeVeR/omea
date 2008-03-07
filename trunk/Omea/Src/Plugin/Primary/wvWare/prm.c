#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"


void
wvGetPRM (PRM * item, wvStream * fd)
{
    U16 temp16;
    temp16 = read_16ubit (fd);
    item->fComplex = temp16 & 0x0001;
    wvTrace (
	     ("u16 is %x,fComplex is %d %d\n", temp16, temp16 & 0x0001,
	      item->fComplex));

    if (item->fComplex)
	item->para.var2.igrpprl = (temp16 & 0xfffe) >> 1;
    else
      {
	  item->para.var1.isprm = (temp16 & 0x00fe) >> 1;
	  item->para.var1.val = (temp16 & 0xff00) >> 8;
      }
}

void
wvInitPRM (PRM * item)
{
    item->fComplex = 0;
    item->para.var2.igrpprl = 0;
}
