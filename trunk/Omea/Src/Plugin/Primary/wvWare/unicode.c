#include <stdlib.h>
#include <stdio.h>
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif
#include "wv.h"
#include "utf.h"


char *
wvWideStrToMB (U16 * str)
{
    int len, len2 = 0, j;
    char *utf8 = NULL;
    char target[5];		/*
				   no wide char becomes longer than about 3 or 4 chars, 
				   but you never know :-)
				 */
    if (str == NULL)
	return (NULL);

    while (*str != 0)
      {
	  len = our_wctomb (target, *str);
	  utf8 = (char *) realloc (utf8, len2 + len + 1);
	  for (j = 0; j < len; j++)
	      utf8[len2 + j] = target[j];
	  len2 += len;
	  str++;
      }
    if (utf8 != NULL)
	utf8[len2] = '\0';
    return (utf8);
}


char *
wvWideCharToMB (U16 char16)
{
    int len, len2 = 0, j;
    char *utf8 = NULL;
    char target[5];		/*
				   no wide char becomes longer than about 3 or 4 chars, 
				   but you never know :-)
				 */
    len = our_wctomb (target, char16);
    utf8 = (char *) realloc (utf8, len2 + len + 1);
    for (j = 0; j < len; j++)
	utf8[len2 + j] = target[j];
    len2 += len;

    if (utf8 != NULL)
	utf8[len2] = '\0';
    return (utf8);
}
