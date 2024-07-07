/*
   dump - dumps individual records for analysis
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

#define PRGNAME "xldump"
#define MAX_COLS 64
#define MAX_ROWS 512

static char FileName[2][12] =			/* The section of the Excel Spreadsheet we read in */
{
	"/Workbook",		/* Office 97 */
	"/Book"			/* Everything else ? */
};

/* Function Prototypes */
COLE_LOCATE_ACTION_FUNC dump_file;
/*static void main_line_processor(int opcode, char data);*/
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
		fprintf (stderr, "dump - Outputs excel file records for analysis.\n"
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

/*static void main_line_processor(int opcode, char data)
{
}
*/

static void output_opcode_string(int opcode)
{
	switch (opcode&0x00FF)
	{

	/* start of documented general opcodes */
	
		case 0x0A:
			puts("\nEOF: End of File");
			break;
		case 0x0C:
			puts("CALCCOUNT: Iteration count");
			break;
		case 0x0D:
			puts("CalcMode: Calculation mode");
			break;
		case 0x0E:
			puts("Precision");
			break;
		case 0x0F:
			puts("Reference Mode");
			break;
		case 0x10:
			puts("Delta: Iteration Increment");
			break;
		case 0x11:
			puts("Iteration Mode");
			break;
		case 0x12:
			puts("Protection Flag");
			break;
		case 0x13:
			puts("Protection Password");
			break;
		case 0x14:
			puts("Print Header on each page");
			break;
		case 0x15:
			puts("Print Footer on each page");
			break;
		case 0x16:
			puts("External Count: Number of external references");
			break;
		case 0x17:
			puts("External reference");
			break;
		case 0x19:
			puts("Windows are Protected");
			break;
		case 0x1A:
			puts("Vertical Page Breaks");
			break;
		case 0x1B:
			puts("Horizontal Page Breaks");
			break;
		case 0x1C:
			puts("Cell Note");
			break;
		case 0x1D:
			puts("Selection");
			break;
		case 0x22:
			puts("1904 date system");
			break;
		case 0x26:
			puts("Left Margin Measurement");
			break;
		case 0x27:
			puts("Right Margin Measurement");
			break;
		case 0x28:
			puts("Top Margin Measurement");
			break;
		case 0x29:
			puts("Bottom Margin Measurement");
			break;
		case 0x2A:
			puts("Print Row/Column Labels");
			break;
		case 0x2B:
			puts("Print Gridlines Flag");
			break;
		case 0x2F:
			puts("File is Password protected");
			break;
		case 0x3C:
			puts("Continues long records");
			break;
		case 0x3D:
			puts("Window1");
			break;
		case 0x40:
			puts("BACKUP: Save Backup Version of the File");
			break;
		case 0x41:
			puts("Number of Panes and their position");
			break;

		/* error in ms docs.
		case 0x42:
			puts("CODEPAGE: Default code page");
			break;
		case 0x42:
			puts("CODENAME: VBE Object Name");
			break;
		*/

		case 0x4D:
			puts("PLS: Environment specific print record");
			break;
		case 0x50:
			puts("DCON: Data consolidation information");
			break;
		case 0x51:
			puts("DCONREF: Data consolidation references");
			break;
		case 0x52:
			puts("DCONNAME: Data Consolidation Named References");
			break;
		case 0x55:
			puts("DEFCOLWIDTH: Default Column Width");
			break;
		case 0x59:
			puts("XCT: CRN Record Count");
			break;
		case 0x5A:
			puts("CRN: Nonresident operands");
			break;
		case 0x5B:
			puts("FILESHARING:File-sharing information");
			break;
		case 0x5C:
			puts("Write Access");
			break;
		case 0x5D:
			puts("OBJ: Describes a Graphic object");
			break;
		case 0x5E:
			puts("UNCALCED: Recalculation Status");
			break;
		case 0x5F:
			puts("SAVERECALC: Recalculate before save");
			break;
		case 0x60:
			puts("TEMPLATE: Workbook is a template");
			break;
		case 0x63:
			puts("OBJPROTECT: Objects are protected");
			break;
		case 0x7D:
			puts("COLINFO: Column formatting information");
			break;
		case 0x7E:
			puts("RK Number");
			break;
		case 0x7F:
			puts("IMDATA: Image data");
			break;
		case 0x80:
			puts("GUTS: Size of row and column gutters");
			break;
		case 0x81:
			puts("WSBOOL: Additional workspace information");
			break;
		case 0x82:
			puts("GRIDSET: State change of gridlines option");
			break;
		case 0x83:
			puts("HCENTER: Center between horizontal margins");
			break;
		case 0x84:
			puts("VCENTER: Center between vertical margins");
			break;
		case 0x85:
			puts("BoundSheet");
			break;
		case 0x86:
			puts("WRITEPROT: Workbook is Write-protected");
			break;
		case 0x87:
			puts("ADDIN: Workbook is add-in macro");
			break;
		case 0x88:
			puts("EDG: Edition globals");
			break;
		case 0x89:
			puts("PUB: Publisher");
			break;
		case 0x8C:
			puts("COUNTRY: Default country and WIN.INI Country");
			break;
		case 0x8D:
			puts("HIDEOBJ: Object display options");
			break;
		case 0x90:
			puts("SORT: Sorting options");
			break;
		case 0x91:
			puts("SUB: Subscriber");
			break;
		case 0x92:
			puts("Palette Info");
			break;
		case 0x94:
			puts("LHRECORD: .WK? File Conversion Information");
			break;
		case 0x95:
			puts("LHNGRAPH: Named Graph Information");
			break;
		case 0x96:
			puts("SOUND: Sound note");
			break;
		case 0x98:
			puts("LPR: Sheet was printed using LINE.PRINT()");
			break;
		case 0x99:
			puts("STANDARDWIDTH: Standard column width");
			break;
		case 0x9A: 
			puts("FNGROUPNAME: Function Group name");
			break;
		case 0x9B:
			puts("FILTERMODE: Sheet contains filtered list");
			break;
		case 0x9C:
			puts("FNGROUPCOUNT: Built-in function group count");
			break;
		case 0x9D:
			puts("AUTOFILTERINFO: Drop Down Arrow Count");
			break;
		case 0x9E:
			puts("AUTOFILTER: AutoFilter data");
			break;
		case 0xA0:
			puts("SCL: Window Zoom magnification");
			break;
		case 0xA1:
			puts("Page Setup");
			break;
		case 0xA9:
			puts("COORDLIST: Polygon Object Vertex coordinates");
			break;
		case 0xAB:
			puts("GCW: Global Column-Wdith flags");
			break;
		case 0xAE:
			puts("SCENMAN: Scenario Output data");
			break;
		case 0xAF:
			puts("PROT4REV: Shared Workbook protection flag");
			break;
		case 0xB0:
			puts("SXVIEW: View Definition");
			break;
		case 0xB1:
			puts("SXVD: View Fields");
			break;
		case 0xB2:
			puts("SXVI: View Item");
			break;
		case 0xB4:
			puts("SXIVD: Row/Column Field Ids");
			break; 
		case 0xB5:
			puts("SXLI: Line item array");
			break;
		case 0xB6:
			puts("SXPI: Page item");
			break;
		case 0xB8:
			puts("DOCROUTE: Routing slip information");
			break;
		case 0xB9:
			puts("RECIPNAME: Recipient name");
			break;
		case 0xBC:
			puts("SHRFMLA: Shared formula");
			break;
		case 0xBD:
			puts("MULRK: Multiple RK cells");
			break;
		case 0xBE:		 
			puts("Multiple Blanks");
			break;
		case 0xC1:
			puts("MMS: ADDMENU/DELMENU Record Group count");
			break;
		case 0xC2:
			puts("ADDMENU: Menu Addition");
			break;
		case 0xC3:
			puts("DELMENU: Menu Deletion");
			break;
		case 0xC5: 
			puts("SXDI: Data Item");
			break;
		case 0xC6:
			puts("SXDB: PivtoTable Cache Data");
			break;
		case 0xCD:
			puts("SXSTRING: String");
			break;
		case 0xD0:
			puts("SXTBL: Multiple Consolidation Source Info"); 
			break;
		case 0xD1:
			puts("SXTBRGIITM: Page Item Name Count");
			break;
		case 0xD2:
			puts("SXTBPG: Page Item Indexes");
			break;
		case 0xD3:
			puts("OBPROJ: Visual Basic Project");
			break;
		case 0xD5:
			puts("SXIDSTM: Stream ID");
			break;
		case 0xD6:
			puts("RString");
			break;
		case 0xD7:
			puts("DBCELL: Stream offsets");
			break;
		case 0xDA:
			puts("BOOKBOOL: Workbook option flag");
			break;

		/* error in ms docs
		case 0xDC:
			puts("PARAMQRY: Query parameters");
			break;
		case 0xDC:
			puts("SXEXT: External source information");
			break;
		*/

		case 0xDD:
			puts("SCENPROTECT: Scenario protection"); 
			break;
		case 0xDE:
			puts("OLESIZE: Size of an OLE object");
			break;
		case 0xDF:
			puts("UDDESC: Description string for chart autoformat");
			break;
		case 0xE0:
			puts("Extended Format");
			break;
		case 0xE1:
			puts("INTERFACEHDR: Beginning of User Interface Records"); 
			break;
		case 0xE2:
			puts("INTERFACEEND: End of User interface records");
			break;
		case 0xE3:
			puts("SXVS: View source");
			break;
		case 0xEA:
			puts("TABIDCONF: Sheet tab ID of Conflict history");
			break;
		case 0xEB:
			puts("MSODRAWINGGROUP: MS Office Drawing Group");
			break;
		case 0xEC:
			puts("MSODRAWING: MS Office Drawing");
			break;
		case 0xED:
			puts("MS Office Drawing Selection");
			break;
		case 0xF0:
			puts("SXRULE: PivotTable Rule data");
			break;
		case 0xF1:
			puts("SXEX: PivotTable Extended information");
			break;
		case 0xF2:
			puts("SXFILT: PivotTable Rule Filter");
			break;
		case 0xF6:
			puts("SXNAME: PivotTable Name");
			break;
		case 0xF7:
			puts("SXSELECT: PivotTable Selection Information");
			break;
		case 0xF8:
			puts("PivotTable Name Pair");
			break;
		case 0xF9:
			puts("PivotTable Parsed Expression");
			break;
		case 0xFB:
			puts("PivotTable Format Record");
			break;
		case 0xFC:
			puts("Shared String Table");
			break;
		case 0xFD:
			puts("Cell Value, String Constant/SST");
			break;
		case 0xFF:
			puts("Extended Shared String Table");
			break;


		default:
			puts("Unknown Opcode");
			break;
	}
}

