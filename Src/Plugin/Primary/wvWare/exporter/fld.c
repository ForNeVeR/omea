#include <stdio.h>
#include <stdlib.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wvexporter-priv.h"

void
wvPutFLD (FLD * item, wvStream * fd)
{
    U8 temp8 = (U8) 0;
    U8 ch = (U8) 0;

    /* FLD is a union of var1 && var2
     * but they have a common 'ch' first member
     * I pray to god that this works XP & X-compilers
     */
    ch = item->var1.ch;

    if (ch == 19)
      {
	  temp8 |= ch;
	  temp8 |= item->var1.reserved << 5;
	  write_8ubit (fd, temp8);
	  write_8ubit (fd, (U8) item->var1.flt);
      }
    else
      {
	  temp8 |= ch;
	  temp8 |= item->var2.reserved << 5;
	  write_8ubit (fd, temp8);

	  temp8 |= item->var2.fDiffer;
	  temp8 |= item->var2.fZombieEmbed << 1;
	  temp8 |= item->var2.fResultDirty << 2;
	  temp8 |= item->var2.fResultEdited << 3;
	  temp8 |= item->var2.fLocked << 4;
	  temp8 |= item->var2.fPrivateResult << 5;
	  temp8 |= item->var2.fNested << 6;
	  temp8 |= item->var2.fHasSep << 7;
	  write_8ubit (fd, temp8);
      }
}
