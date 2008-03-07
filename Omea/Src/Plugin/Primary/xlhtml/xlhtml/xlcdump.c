/*
   xlcdump - dumps individual chart records for analysis
   Copyright 2002 Charles N Wyble <jackshck@yahoo.com>

   This program is free software; you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published  by
   the Free Software Foundation; either version 2 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program; if not, write to the Free Software
   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307  USA
 */

#include "../config.h"	/* Created by ./configure script */
#include "../cole/cole.h"
#include <io.h>		/* for umask */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>		/* for strcpy() */
#include <ctype.h>		/* For isprint */


#define MODE 0 			/* 0 - ascii;  1 - hex */
#define TEXT 0			/* In ascii mode, 0 - ascii, 1 - hex */

#define PRGNAME "xlcdump"
#define MAX_COLS 64
#define MAX_ROWS 512

static char FileName[2][12] =			/* The section of the Excel Spreadsheet we read in */
{
	"/Workbook",		/* Office 97 */
	"/Book"			/* Everything else ? */
};

/* Function Prototypes */
COLE_LOCATE_ACTION_FUNC dump_file;
static void output_opcode_string(int);

/* Global data */
static char filename[128];


int main (int argc, char **argv)
{
	int f_ptr = 0;
	COLEFS * cfs;
	COLERRNO colerrno;

	if (argc < 2)
	{
		fprintf (stderr, "dump - Outputs excel chart records for analysis.\n"
			"Usage: "PRGNAME" <FILE>\n");
		exit (1);
	}
	else
	{
		strncpy(filename, argv[1], 124);
		cfs = cole_mount (filename, &colerrno);
		if (cfs == NULL)
		{
			cole_perror (PRGNAME, colerrno);
			exit (1);
		}
	}

	while (cole_locate_filename (cfs, FileName[f_ptr], NULL, dump_file, &colerrno)) 
	{
		if (f_ptr)
		{
			cole_perror (PRGNAME, colerrno);
			break;
		}
		else
			f_ptr++;
	}

	if (cole_umount (cfs, &colerrno))
	{
		cole_perror ("travel", colerrno);
		exit (1);
	}
		
	return 0;
}


void dump_file(COLEDIRENT *cde, void *_info)
{
	unsigned int length=0, opcode=0, target=0, count = 0;
	unsigned char buf[16];
	COLEFILE *cf;
	COLERRNO err;
	
	cf = cole_fopen_direntry(cde, &err);	

	/* Ouput Header */
	printf("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 3.2//EN\">\n");
	printf("<HTML><HEAD><TITLE>%s", filename);	
	printf("</TITLE></HEAD><BODY>\n");	

/* Output body */
#if (MODE == 1)
	while (cole_fread(cf, buf, 8, &err)) /* For mode >= 1 */
#else
	while (cole_fread(cf, buf, 1, &err))
#endif	
	{
		if (MODE == 0)
		{
			if (count == 0)
			{
				length = 0;
				opcode = (unsigned)buf[0];
				target = 80;	/* ficticious number */
				printf("<br>");
			}
			else if (count == 1)
				opcode |= (buf[0]<<8)&0x0000FFFFL;
			else if (count == 2)
				length = (unsigned)buf[0];	
			else if (count == 3)
			{
				length |= (buf[0]<<8);
				target = length;
				printf("<br>\nLength:%04X Opcode:%04X - ", length, opcode);
				output_opcode_string(opcode);
				puts("<br>\n");
			}
			if (count > 3)
			{	/* Here is where we want to process the data */
				/* based on the opcode... */
#if (TEXT == 0)
				if (isprint(buf[0]))
					putc(buf[0], stdout);	
#else
				printf("%02X ", buf[0]);
				if (((count-3) % 8) == 0)
					printf("<br>\n");
#endif
			}		
			if (count == (target+3))
				count = 0;
			else		
				count++;	
		}
		else	/* mode >= 1 */
		{
			printf("%02x %02x %02x %02x %02x %02x %02x %02x &nbsp; &nbsp; &nbsp; &nbsp; ",
			  (unsigned)buf[0], (unsigned)buf[1], (unsigned)buf[2], (unsigned)buf[3], 
			  (unsigned)buf[4], (unsigned)buf[5], (unsigned)buf[6], (unsigned)buf[7]);
			  putchar(buf[0]); putchar(buf[1]); 
			  putchar(buf[2]); putchar(buf[3]); 
			  putchar(buf[4]); putchar(buf[5]); 
			  putchar(buf[6]); putchar(buf[7]);
			  printf("<br>\n");
		}
	}

/* Output Tail */
	printf("</BODY></HTML>\n");	
	cole_fclose(cf, &err);
}



static void output_opcode_string(int opcode)
{
	switch (opcode&0x00FF)
	{

	case 0x10:
		case 0x01:
			puts("UNITS: Chart Units");
			break;
		case 0x02:
			puts("CHART: Location and overall chart dimensions");
			break;
		case 0x03:
			puts("SERIES: Series Definition");
			break;
		case 0x06:
			puts("DATAFORMAT: Series and Data Point Numbers");
			break;
		case 0x07:
			puts("LINEFORMAT: Style of a line or border");
			break;
		case 0x09:
			puts("MARKERFORMAT: Style of a line marker");
			break;
		case 0x0A:
			puts("AREAFORMAT: Colors and patterns for an area");
			break;
		case 0x0B:
			puts("PIEFORMAT: Position of the pie slice");
			break;
		case 0x0C:
			puts("ATTACHEDLABEL: Series data/value labels");
			break;
		case 0x0D:
			puts("SERIESTEXT: Legend/category/value text");
			break;
		case 0x14:
			puts("CHARTFORMAT: Parent record for chart group");
			break;
		case 0x15:
			puts("LEGEND: Legend type and position");
			break;
		case 0x16:
			puts("SERIESLIST: Specifies the series in an overlay chart");
			break;
		case 0x17:
			puts("BAR: Chart group is a bar or column chart group");
			break;
		case 0x18:
			puts("LINE: Chart group is a line chart group");
			break;
		case 0x19:
			puts("PIE: Chart group is a pie chart group");
			break;
		case 0x1A:
			puts("AREA: Chart group is an area chart group");
			break;
		case 0x1B:
			puts("SCATTER: Chart group is a scatter chart group");
			break;
		case 0x1C:
			puts("CHARTLINE: Drop/Hi-Lo/Series Lines on a line chart");
			break;
		case 0x1D:
			puts("AXIS: Axis Type");
			break;
		case 0x1E:
			puts("TICK: Tick marks and labels format");
			break;
		case 0x1F:
			puts("VALUERANGE: Defines value axis scale");
			break;
		case 0x20:
			puts("CATSERRANGE: Defines a category or series axis");
			break;
		case 0x21:
			puts("AXISLINEFORMAT: Defines a line that spans an axis");
			break;
		case 0x22:
			puts("CHARTFORMTLINK: Not Used");
			break;
		case 0x24:
			puts("DEFAULTTEXT: Default data label text properties");
			break;
		case 0x25:
			puts("TEXT: Defines display of text fields");
			break;
		case 0x26:
			puts("FONTX: Font Index");
			break;
		case 0x27:
			puts("OBJECTLINK: Attaches Text to chart or chart item");
			break;
		case 0x32:
			puts("FRAME: Defines border shape around displayed text");
			break;
		case 0x33:
			puts("BEGIN: Defines the beginning of an object");
			break;
		case 0x34:
			puts("END: Defines the end of an object");
			break;
		case 0x35:
			puts("PLOTAREA: Frame belongs to ploat area");
			break;
		case 0x3A:
			puts("3d Chart group");
			break;
		case 0x3C:
			puts("PICF: Picture Format");
			break;
		case 0x3D:
			puts("DROPBAR: Defines drop bars");
			break;
		case 0x3E:
			puts("RADAR: Chart group is a radar chart group");
			break;
		case 0x3F:
			puts("SURFACE: Chart group is a surface chart group");
			break;
		case 0x40:
			puts("RADARAREA: Chart group is a radar area chart group");
			break;
		case 0x41:
			puts("AXISPARENT: Axis size and location");
			break;
		case 0x43:
			puts("LEGENDXN: Legend Exception");
			break;
		case 0x44:
			puts("SHTPROPS: Sheet Properties");
			break;
		case 0x45:
			puts("SERTOCRT: Series chart-group index");
			break;
		case 0x46:
			puts("AXESUSED: Number of axes sets");
			break;
		case 0x48:
			puts("SBASEREF: PivotTable Reference");
			break;
		case 0x4A:
			puts("SERPARENT: Trendline or Errorbar series index");
			break;
		case 0x4B:
			puts("SERAUXTREND: Series trendline");
			break;
		case 0x4E:
			puts("IFMT: Number-Format Index");
			break;
		case 0x4F:
			puts("POS: Position information");
			break;
		case 0x50:
			puts("ALRUNS: Text formatting");
			break;
		case 0x51:
			puts("AI: Linked data");
			break;
		case 0x5B:
			puts("Series ErrorBar");
			break;
		case 0x5D:
			puts("SERFMT: Series Format"); 
			break;
		case 0x60:
			puts("FBI: Font Basis"); 
			break;
		case 0x61:
			puts("BOPPOP: Bar of pie/pie of pie chart options");
			break;
		case 0x62:
			puts("AXCEXT: Axis options");
			break;
		case 0x63:
			puts("DAT: Data Table Options");
			break;
		case 0x64:
			puts("PLOTGROWTH: Font scale factors");
			break;
		case 0x65:
			puts("SIINDEX: Series Index");
			break;
		case 0x66:
			puts("GELFRAME: Fill data");
			break;
		case 0x67:
			puts("Custom bar of pie/ pie of pie chart options");
			break;
		default:
			puts("Unknown Chart Opcode");
			break;
	}
}

