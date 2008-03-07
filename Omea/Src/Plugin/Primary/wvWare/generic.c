#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <ctype.h>

#ifdef HAVE_CONFIG_H
#include "config.h"
#endif

#ifdef HAVE_MALLOC_H
#include <malloc.h>
#endif

#include "wv.h"

#define SOME_ARBITRARY_LIMIT 1

int
wvGetEmpty_PLCF (U32 ** cps, U32 * nocps, U32 offset, U32 len, wvStream * fd)
{
    U32 i;
    if (len == 0)
      {
	  *cps = NULL;
	  *nocps = 0;
      }
    else
      {
	  *nocps = len / 4;
	  *cps = (U32 *) malloc (*nocps * sizeof (U32));
	  if (*cps == NULL)
	    {
		wvError (
			 ("NO MEM 3, failed to alloc %d bytes\n",
			  *nocps * sizeof (U32)));
		return (1);
	    }
	  wvStream_goto (fd, offset);
	  for (i = 0; i < *nocps; i++)
	      (*cps)[i] = read_32ubit (fd);
      }
    return (0);
}

/**
 * Very simple malloc wrapper
 */
void *
wvMalloc (U32 size)
{
    void *p = NULL;
    int ntries = 0;

    if (size == 0)
	return NULL;

    /* loop trying to obtain memory */
    do
      {
	  p = (void *) malloc (size);
	  if (p)
	      break;
	  ntries++;
      }
    while (ntries < SOME_ARBITRARY_LIMIT);

    if (!p)
      {
	  wvError (("Could not allocate %d bytes\n", size));
	  exit (-1);
      }

    /* zero out the memory */
    memset ( p, 0, size ) ;

    return p;
}

/*!
 * Separate this fn out so that we can use mkstemp someday soon
 */
char *
wvTempName (char *s)
{
    return tmpnam (s);
}

/*
If the
second most significant bit is clear, then this indicates the actual file
offset of the unicode character (two bytes). If the second most significant
bit is set, then the actual address of the codepage-1252 compressed version
of the unicode character (one byte), is actually at the offset indicated by
clearing this bit and dividing by two.
*/
/*
flag = 1 means that this is a one byte char, 0 means that this is a type
byte char
*/
U32
wvNormFC (U32 fc, int *flag)
{
    if (fc & 0x40000000UL)
      {
	  fc = fc & 0xbfffffffUL;
	  fc = fc / 2;
	  if (flag)
	      *flag = 1;
      }
    else if (flag)
	*flag = 0;
    return (fc);
}

U16
wvGetChar (wvStream * fd, U8 chartype)
{
    if (chartype == 1)
	return (read_8ubit (fd));
    else
	return (read_16ubit (fd));
    return (0);
}

int
wvIncFC (U8 chartype)
{
    if (chartype == 1)
	return (1);
    return (2);
}

int
wvStrlen (const char *str)
{
    if (str == NULL)
	return (0);
    return (strlen (str));
}

char *
wvStrcat (char *dest, const char *src)
{
    if (src != NULL)
	return (strcat (dest, src));
    else
	return (dest);
}

void
wvAppendStr (char **orig, const char *add)
{
    int pos;
    wvTrace (("got this far\n"));
    pos = wvStrlen (*orig);
    wvTrace (("len is %d %d\n", pos, wvStrlen (add)));
    (*orig) = (char *) realloc (*orig, pos + wvStrlen (add) + 1);
    (*orig)[pos] = '\0';
    wvTrace (("3 test str of %s\n", *orig));
    wvStrcat (*orig, add);
    wvTrace (("3 test str of %s\n", *orig));
}

void
wvStrToUpper (char *str)
{
    int i;
    if (str == NULL)
	return;
    for (i = 0; i < wvStrlen (str); i++)
	str[i] = toupper (str[i]);
}
