/*
   cole - A free C OLE library.
   Copyright 1998, 1999  Roberto Arturo Tena Sanchez

   This program is free software; you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation; either version 2 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program; if not, write to the Free Software
   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */
/*
   Arturo Tena <arturo@directmail.org>
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
//#include <unistd.h>

#include "../xlhtml/version.h"

#include "internal.h"

int __opened_files_count;
char *__opened_files[256];

extern char *tmpdir;

#define MIN(a,b) ((a)<(b) ? (a) : (b))

void __remove_all_files(void)
{
    int i;
    for(i = 0; i < __opened_files_count; ++i)
    {
        if(NULL != __opened_files[i])
        {
            remove(__opened_files[i]);
            free(__opened_files[i]);
        }
    }
    __opened_files_count = 0;
}


char *__cole_generate_tmp_name()
{
    char prefix[64];
    char *filename;
    if( tmpdir )
    {
        putenv("TMP=");
    }
    sprintf(prefix,"cole-%s-0x%08x.", VERSION, getpid());
    filename = tempnam( tmpdir, prefix );
    if(NULL == filename)
        return NULL;
    __opened_files[__opened_files_count++] = strdup(filename);
    atexit(__remove_all_files);
    return filename;
}

int
__cole_extract_file (FILE **file, char **filename, U32 size, U32 pps_start,
		    U8 *BDepot, U8 *SDepot, FILE *sbfile, FILE *inputfile)
{
	/* FIXME rewrite this cleaner */

	U16 BlockSize, Offset;
	U8 *Depot;
	FILE *infile;
	long FilePos;
	size_t bytes_to_copy;
	U8 Block[0x0200];

	FILE *ret;

    if (NULL == (*filename = __cole_generate_tmp_name())) {
        return 2;
    }
	ret = fopen (*filename, "w+b");
	*file = ret;
	if (ret == NULL) {
		free (*filename);
		return 3;
	}

	if (size >= 0x1000) {
		/* read from big block depot */
		Offset = 1;
		BlockSize = 0x0200;
		infile = inputfile;
		Depot = BDepot;
	} else {
		/* read from small block file */
		Offset = 0;
		BlockSize = 0x40;
		infile = sbfile;
		Depot = SDepot;
	}
	while (pps_start != 0xfffffffeUL /*&& pps_start != 0xffffffffUL &&
		pps_start != 0xfffffffdUL*/) {
		FilePos = (long)((pps_start + Offset) * BlockSize);
		if (FilePos < 0) {
			fclose (*file);
			remove (*filename);
			free (*filename);
			return 4;
		}
		bytes_to_copy = MIN ((U32)BlockSize, size);
		if (fseek (infile, FilePos, SEEK_SET)) {
			fclose (*file);
			remove (*filename);
			free (*filename);
			return 4;
		}
		fread (Block, bytes_to_copy, 1, infile);
		if (ferror (infile)) {
			fclose (*file);
			remove (*filename);
			free (*filename);
			return 5;
		}
		fwrite (Block, bytes_to_copy, 1, *file);
		if (ferror (*file)) {
			fclose (*file);
			remove (*filename);
			free (*filename);
			return 6;
		}
		pps_start = fil_sreadU32 (Depot + (pps_start * 4));
		size -= MIN ((U32)BlockSize, size);
		if (size == 0)
			break;
	}

	return 0;
}

