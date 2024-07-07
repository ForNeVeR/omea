/*
 *  Password verification in Microsoft Word 8.0
 *  see D_CREDITS & D_README
 */

#include <stdlib.h>
#include <stdio.h>
#ifndef WIN32
#include <sys/types.h>
#endif
#include <string.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"
#undef S32
#include "rc4.h"
#include "md5.h"

void wvMD5StoreDigest (wvMD5_CTX * mdContext);

static void
makekey (U32 block, rc4_key * key, wvMD5_CTX * valContext)
{
    wvMD5_CTX mdContext;
    U8 pwarray[64];

    memset (pwarray, 0, 64);

    /* 40 bit of hashed password, set by verifypwd() */
    memcpy (pwarray, valContext->digest, 5);

    /* put block number in byte 6...9 */
    pwarray[5] = (U8) (block & 0xFF);
    pwarray[6] = (U8) ((block >> 8) & 0xFF);
    pwarray[7] = (U8) ((block >> 16) & 0xFF);
    pwarray[8] = (U8) ((block >> 24) & 0xFF);

    pwarray[9] = 0x80;
    pwarray[56] = 0x48;

    wvMD5Init (&mdContext);
    wvMD5Update (&mdContext, pwarray, 64);
    wvMD5StoreDigest (&mdContext);
    prepare_key (mdContext.digest, 16, key);
}

static int
verifypwd (U8 pwarray[64], U8 docid[16], U8 salt[64], U8 hashedsalt[16],
	   wvMD5_CTX * valContext)
{
    wvMD5_CTX mdContext1, mdContext2;
    rc4_key key;
    int offset, keyoffset;
    unsigned int tocopy;

    wvMD5Init (&mdContext1);
    wvMD5Update (&mdContext1, pwarray, 64);
    wvMD5StoreDigest (&mdContext1);

    offset = 0;
    keyoffset = 0;
    tocopy = 5;

    wvMD5Init (valContext);

    while (offset != 16)
      {
	  if ((64 - offset) < 5)
	      tocopy = 64 - offset;

	  memcpy (pwarray + offset, mdContext1.digest + keyoffset, tocopy);
	  offset += tocopy;

	  if (offset == 64)
	    {
		wvMD5Update (valContext, pwarray, 64);
		keyoffset = tocopy;
		tocopy = 5 - tocopy;
		offset = 0;
		continue;
	    }

	  keyoffset = 0;
	  tocopy = 5;
	  memcpy (pwarray + offset, docid, 16);
	  offset += 16;
      }

    /* Fix (zero) all but first 16 bytes */

    pwarray[16] = 0x80;
    memset (pwarray + 17, 0, 47);
    pwarray[56] = 0x80;
    pwarray[57] = 0x0A;

    wvMD5Update (valContext, pwarray, 64);
    wvMD5StoreDigest (valContext);

    /* Generate 40-bit RC4 key from 128-bit hashed password */

    makekey (0, &key, valContext);

    rc4 (salt, 16, &key);
    rc4 (hashedsalt, 16, &key);

    salt[16] = 0x80;
    memset (salt + 17, 0, 47);
    salt[56] = 0x80;

    wvMD5Init (&mdContext2);
    wvMD5Update (&mdContext2, salt, 64);
    wvMD5StoreDigest (&mdContext2);

    return (memcmp (mdContext2.digest, hashedsalt, 16));
}

static void
expandpw (U16 password[16], U8 pwarray[64])
{
    /* expandpw expects null terminated 16bit unicode input */
    int i;

    for (i = 0; i < 64; i++)
	pwarray[i] = 0;

    i = 0;
    while (password[i])
      {
	  pwarray[2 * i] = (password[i] & 0xff);
	  pwarray[(2 * i) + 1] = ((password[i] >> 8) & 0xff);
	  i++;
      }

    pwarray[2 * i] = 0x80;
    pwarray[56] = (i << 4);
}

int
wvDecrypt97 (wvParseStruct * ps)
{
    FILE *outtable;
    FILE *outmain;
    wvStream *enc;
    U8 pwarray[64];
    U8 docid[16], salt[64], hashedsalt[16], x;
    int i, j, end;
    unsigned int blk;
    rc4_key key;
    unsigned char test[0x10];
    wvMD5_CTX valContext;

    for (i = 0; i < 4; i++)
	x = read_8ubit (ps->tablefd);

    for (i = 0; i < 16; i++)
	docid[i] = read_8ubit (ps->tablefd);

    for (i = 0; i < 16; i++)
	salt[i] = read_8ubit (ps->tablefd);

    for (i = 0; i < 16; i++)
	hashedsalt[i] = read_8ubit (ps->tablefd);

    expandpw (ps->password, pwarray);

    if (verifypwd (pwarray, docid, salt, hashedsalt, &valContext))
	return (1);

    enc = ps->tablefd;

    wvStream_offset_from_end (enc, 0);
    end = wvStream_tell (enc);

    j = 0;
    wvStream_goto (enc, j);

    outtable = tmpfile ();

    blk = 0;
    makekey (blk, &key, &valContext);

    while (j < end)
      {
	  for (i = 0; i < 0x10; i++)
	      test[i] = read_8ubit (enc);

	  rc4 (test, 0x10, &key);

	  for (i = 0; i < 0x10; i++)
	      fputc (test[i], outtable);
	  j += 0x10;
	  if ((j % 0x200) == 0)
	    {
		/* 
		   at this stage we need to rekey the rc4 algorithm
		   Dieter Spaar <spaar@mirider.augusta.de> figured out
		   this rekeying, big kudos to him 
		 */
		blk++;
		makekey (blk, &key, &valContext);
	    }

      }

    enc = ps->mainfd;

    wvStream_offset_from_end (enc, 0);
    end = wvStream_tell (enc);

    j = 0;
    wvStream_goto (enc, j);

    outmain = tmpfile ();

    blk = 0;
    makekey (blk, &key, &valContext);

    while (j < end)
      {
	  for (i = 0; i < 0x10; i++)
	      test[i] = read_8ubit (enc);

	  rc4 (test, 0x10, &key);

	  for (i = 0; i < 0x10; i++)
	      fputc (test[i], outmain);
	  j += 0x10;
	  if ((j % 0x200) == 0)
	    {
		/* 
		   at this stage we need to rekey the rc4 algorithm
		   Dieter Spaar <spaar@mirider.augusta.de> figured out
		   this rekeying, big kudos to him 
		 */
		blk++;
		makekey (blk, &key, &valContext);
	    }

      }

#if 0
    /* causing us grief on windows */
    if (ps->tablefd0)
	wvStream_close (ps->tablefd0);
#endif

    if (ps->tablefd1)
	wvStream_close (ps->tablefd1);
    wvStream_close (ps->mainfd);
 
   wvStream_FILE_create(&ps->tablefd, outtable);
   wvStream_FILE_create(&ps->mainfd, outmain);

#if 1
   wvStream_FILE_create(&ps->tablefd0, outtable);
   wvStream_FILE_create(&ps->tablefd1, outtable);
#else
   ps->tablefd0 = ps->tablefd;
   ps->tablefd1 = ps->tablefd;
#endif

    wvStream_rewind (ps->tablefd);
    wvStream_rewind (ps->mainfd);
    ps->fib.fEncrypted = 0;
    wvGetFIB (&ps->fib, ps->mainfd);
    ps->fib.fEncrypted = 0;
    return (0);
}


/* 
this is just cut out of wvMD5Final to get the byte order correct
under MSB systems, the previous code was woefully tied to intel
x86

C.
*/
void
wvMD5StoreDigest (wvMD5_CTX * mdContext)
{
    unsigned int i, ii;
    /* store buffer in digest */
    for (i = 0, ii = 0; i < 4; i++, ii += 4)
      {
	  mdContext->digest[ii] = (unsigned char) (mdContext->buf[i] & 0xFF);
	  mdContext->digest[ii + 1] =
	      (unsigned char) ((mdContext->buf[i] >> 8) & 0xFF);
	  mdContext->digest[ii + 2] =
	      (unsigned char) ((mdContext->buf[i] >> 16) & 0xFF);
	  mdContext->digest[ii + 3] =
	      (unsigned char) ((mdContext->buf[i] >> 24) & 0xFF);
      }
}
