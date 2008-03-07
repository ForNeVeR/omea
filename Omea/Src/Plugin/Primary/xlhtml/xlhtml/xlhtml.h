
#include "../config.h"		/* Created by ./configure script */
#include "../cole/support.h"		/* Needs to be before internal.h */
#include "../cole/internal.h"		/* Needs to be before cole */
#include "../cole/cole.h"
#include <io.h>			/* for umask */
//#include <dir.h>

#include <stdlib.h>		/* For atof(), calloc() */
#include <string.h>		/* For string functions */
#include <math.h>		/* For fabs() */
#include <ctype.h>		/* For isprint() */
#include <errno.h>
#include "version.h"

/* Used by packed string array Opcode: 0xFC */
#define HARD_MAX_ROWS_97	0x7FFE    /*!< Used in add_wb_array to prevent OOM */
#define HARD_MAX_ROWS_95	0x3FFF    /*!< Used in add_wb_array to prevent OOM */
#define HARD_MAX_COLS		256	      /*!< Used in add_wb_array to prevent OOM */

static U16 HARD_MAX_ROWS = HARD_MAX_ROWS_97;
/**********************************
*
*	Don't change anything below here...
*
************************************/
#define PRGNAME 		"xlhtml"
#define WBUFF_SIZE 		8240	/*!< The working buffer. SB 522+10+4(header) bytes minimum = 536 */
#define MAX_COLORS		65	/*!< This is the size of the built-in color table */
#define EXCEL95		0x500		/*!< This is the file stamp for biff7 - Excel 5 & 95 */
#define EXCEL97		0x600		/*!< This is the file stamp for biff8 - Excel 97 & 2000 */

#include <sys/stat.h>
#include <sys/types.h>
#define GLOBAL_UMASK (2)

typedef struct		/*!< This encapsulates the Unicode String	*/
{
	U8 uni;		/*!< Unicode String: 0==ASCII/8859-1, 1==windows-1252, 2==utf-8 */
	U8 *str;	/*!< Characters of string */
	U16 len;	/*!< Length of string */
	U8 *fmt_run;	/*!< formatting run, short pairs: offset, index to font */
	U8 crun_cnt;	/*!< The count of format runs */
}uni_string;

typedef struct 		/*!< This is everything we need for a cell */
{
	U16 xfmt;	/*!< The high bit will tell us which version 0 =< 2; 1 == 2+ */
	U16 type;	/*!< This will record the record type that generated the cell */
	U16 spanned;		/*!< If 1 don't output */
	uni_string ustr;	/*!< The cell's displayed contents */
	U16 rowspan;		/*!< rows to span */
	U16 colspan;		/*!< columns to span */
	uni_string h_link;	/*!< If a hyperlinked cell, this is the link*/
}cell;

typedef struct	/*!< This encapsulates some information about each worksheet */
{
	U32 first_row;
	S32 biggest_row;
	U32 max_rows;
	U16 first_col;
	S16 biggest_col;
	U16 max_cols;
	uni_string ws_title;
	cell **c_array;
	U16 spanned;
}work_sheet;

typedef struct	/*!< This is everything we need to know about fonts */
{
	U16 size;
	U16 attr;
	U16 c_idx;
	U16 bold;
	U16 super;
	U8 underline;
	uni_string name;
}font_attr;

typedef struct
{
	uni_string *name;
	U16 cnt;
}fnt_cnt;

typedef struct		/*!< This covers the Extended Format records */
{
	U16 fnt_idx;
	U16 fmt_idx;
	U16 gen;
	U16 align;
	U16 indent;
	U16 b_style;
	U16 b_l_color;
	U32  b_t_color;
	U16 cell_color;
}xf_attr;

typedef struct		/*!< HTML Attribute */
{
	int fflag;		/*!< Font Flag */
	int bflag;		/*!< Bold Flag */
	int iflag;		/*!< Itallic Flag */
	int sflag;		/*!< Strike thru flag */
	int uflag;		/*!< Underline flag */
	int sbflag;		/*!< Subscript */
	int spflag;		/*!< Superscript */
}html_attr;


