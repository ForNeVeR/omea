
#include "xlhtml.h"
#include <stdio.h>




extern void do_cr(void);
extern int  center_tables;
extern int  first_sheet;
extern int  last_sheet;
extern uni_string  default_font;
extern void trim_sheet_edges(unsigned int);
extern int  next_ws_title;
extern void SetupExtraction(void);
extern void update_default_font(unsigned int);
extern void OutputString(uni_string * );
extern int  default_fontsize;
extern char *default_alignment; 
extern int  aggressive;
extern char *lastUpdated; 
extern int  file_version;
extern int  NoFormat;
extern int  notAccurate;
extern int  formula_warnings;
extern int  disclaimers;
extern int  NoHeaders;
extern int  NotImplemented;
extern int  Unsupported;
extern int  MaxWorksheetsExceeded;
extern int  MaxRowExceeded;
extern int  MaxColExceeded;
extern int  MaxStringsExceeded;
extern int  MaxFontsExceeded;
extern int  MaxPalExceeded;
extern int  MaxXFExceeded;
extern int  MaxFormatsExceeded;
extern char colorTab[MAX_COLORS][8];
extern char *default_text_color;
extern char *default_background_color;
extern char *default_image;
extern char filename[256];
extern int  UnicodeStrings;
extern int  CodePage; 
extern char *title;
extern void update_default_alignment(unsigned int, int);
extern void output_cell( cell *, int); 
extern uni_string author;
extern int null_string(U8 *);
extern unsigned int next_font;
extern int tooold;
work_sheet **ws_array;
font_attr **font_array;


void output_header(void);
void output_footer(void);

void OutputTableHTML(void)
{
    int i, j, k;

    output_header();
    if(tooold)
    {
        printf("<H2>This file was saved in very old version of Excel. Some information can not be extracted.</H2>\n");
    }
    if (center_tables)
    {
        printf("<CENTER>");
        do_cr();
    }

    SetupExtraction();

    /* Here's where we dump the Html Page out */
    for (i=first_sheet; i<=last_sheet; i++) /* For each worksheet */
    {
        update_default_font(i);
        if (ws_array[i] == 0)
            continue;
        if ((ws_array[i]->biggest_row == -1)||(ws_array[i]->biggest_col == -1))
            continue;
        if (ws_array[i]->c_array == 0)
            continue;
        trim_sheet_edges(i);

        /* Print its name */
        if (next_ws_title > 0)
        {
            if (ws_array[i]->ws_title.str)
            {
                printf("<H1><CENTER>");
                OutputString(&ws_array[i]->ws_title);
                printf("</CENTER></H1><br>");
                do_cr();
            }
            else
            {
                printf("<H1><CENTER>(Unknown Page)</CENTER></H1><br>");
                do_cr();
            }
        }

        /* Now dump the table */
        printf("<FONT FACE=\"");
        OutputString(&default_font);
        if (default_fontsize != 3)
            printf("\" SIZE=\"%d", default_fontsize);
        printf("\">");
        do_cr();
		printf("<TABLE cellspacing=\"0\" cellpadding=\"0\">");
        do_cr();
        for (j=ws_array[i]->first_row; j<=ws_array[i]->biggest_row; j++)
        {
            update_default_alignment(i, j);
            printf("<TR");
            if (null_string((U8 *)default_alignment))
                printf(">");
            else
            {
                if (strcmp(default_alignment, "left") != 0)
                    printf(" ALIGN=\"%s\"", default_alignment);
                if (!aggressive)
                    printf(" VALIGN=\"bottom\">\n");
                else
                    printf(">");
            }
            for (k=ws_array[i]->first_col; k<=ws_array[i]->biggest_col; k++)
            {
                output_cell(ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k],0); /* This stuff happens for each cell... */
                if (ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k])
                {
                    if (ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k]->colspan != 0)
                         k += ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k]->colspan-1;
                }
            }

            if (!aggressive)
                printf("</TR>\n");
        }
        printf("</table></FONT><HR>");
        do_cr();
    }

    if (center_tables)
    {
        printf("</CENTER>");
        do_cr();
    }

    /* Print the author's name in itallics... */
    if (author.str)
    {
        printf("<FONT SIZE=-1><I>Spreadsheet's Author:&nbsp;");
        OutputString(&author);
        printf("</I></FONT><br>");
        do_cr();
    }

    /* Print when & how the file was last updated. */
    printf("<FONT SIZE=-1><I>Last Updated ");
    if (lastUpdated)
        printf("%s&nbsp; ", lastUpdated);
	/*
    switch (file_version)
    {
        case EXCEL95:
            printf("with Excel 5.0 or 95");
            break;
        case EXCEL97:
            printf("with Excel 97");
            break;
        default:
            printf("with Excel ????");
            break;
    }
	*/
    printf("</I></FONT><br>");
    do_cr();

    /* Next print Disclaimers... */
    if (NoFormat && disclaimers)
    {
        printf("<br>* This cell's format is not supported.<br>");
        do_cr();
    }
    if ((notAccurate)&&(formula_warnings)&&(disclaimers))
    {
        printf("<br>** This cell's data may not be accurate.<br>");
        do_cr();
    }
    if (NotImplemented && disclaimers)
    {
        printf("<br>*** This cell's data type will be supported in the future.<br>");
        do_cr();
    }
    if (Unsupported && disclaimers)
    {
        printf("<br>**** This cell's type is unsupported.<br>");
        do_cr();
    }

    /* Now out exceeded capacity warnings... */
    if (MaxWorksheetsExceeded || MaxRowExceeded || MaxColExceeded || MaxStringsExceeded ||
        MaxFontsExceeded || MaxPalExceeded || MaxXFExceeded || MaxFormatsExceeded )
        printf("<FONT COLOR=\"%s\">", colorTab[0x0A]);
    if (MaxWorksheetsExceeded)
    {
        printf("The Maximum Number of Worksheets was exceeded. Conversion failed.<br>");
        do_cr();
    }
    if (MaxRowExceeded)
    {
        printf("The Maximum Number of Rows was exceeded. Conversion failed.<br>");
        do_cr();
    }
    if (MaxColExceeded)
    {
        printf("The Maximum Number of Columns was exceeded. Conversion failed.<br>");
        do_cr();
    }
    if (MaxStringsExceeded)
    {
        printf("The Maximum Number of Strings was exceeded. Conversion failed.<br>");
        do_cr();
    }
    if (MaxFontsExceeded)
    {
        printf("The Maximum Number of Fonts was exceeded. Conversion failed.<br>");
        do_cr();
    }
    if (MaxPalExceeded)
    {
        printf("The Maximum Number of Color Palettes was exceeded. Conversion failed.<br>");
        do_cr();
    }
    if (MaxXFExceeded)
    {
        printf("The Maximum Number of Extended Formats was exceeded. Conversion failed.<br>");
        do_cr();
    }
    if (MaxFormatsExceeded)
    {
        printf("The Maximum Number of Formats was exceeded. Conversion failed.<br>");
        do_cr();
    }
    if (MaxWorksheetsExceeded || MaxRowExceeded || MaxColExceeded || MaxStringsExceeded ||
        MaxFontsExceeded || MaxPalExceeded || MaxXFExceeded || MaxFormatsExceeded )
        printf("</FONT>");

    do_cr();

    /* Output Tail */
    output_footer();
}

void output_header(void)
{   /* Ouput Header */
    if (NoHeaders)
        return;
    if (!aggressive)
    {
        printf("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML Transitional//EN\"");
        do_cr();
        printf("\"http://www.w3.org/TR/REC-html40/loose.dtd\">");
        do_cr();
    }
    printf("<HTML><HEAD>");
    do_cr();
    printf("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=");
    if ((UnicodeStrings <= 1)&&CodePage&&(CodePage != 1252))
        printf("windows-%d\">", CodePage);
    else
    {
        switch (UnicodeStrings)
        {
            case 0:
                printf("iso-8859-1\">");        /* Latin-1 */
                break;
            case 1:
                printf("windows-1252\">");  /* Microsoft */
                break;
            default:
                printf("utf-8\">");         /* Unicode */
                break;
        }
    }
    do_cr();

    if (!aggressive)
    {
        printf("<meta name=\"GENERATOR\" content=\"xlhtml\">");
        do_cr();
    }
    printf("<TITLE>");
    if (title)
        printf("%s", title);
    else
        printf("%s", filename);
    printf("</TITLE>");
    do_cr();
	printf("<style type=\"text/css\"><!-- table { border-collapse: collapse; } td { border: 1px solid gray;  padding: 4px; } --> </style>");
    do_cr();
    printf("</HEAD>");
    do_cr();
    do_cr();
    printf("<BODY TEXT=\"#%s\" BGCOLOR=\"#%s\"",
                default_text_color, default_background_color);
    if (default_image)
        printf("BACKGROUND=\"%s\"", default_image);
    printf("><br>");
    do_cr();
}

void output_footer(void)
{
    if (NoHeaders)
        return;
    printf("</BODY></HTML>");
    do_cr();
    fflush(stdout);
}

void output_start_html_attr(html_attr *h, unsigned int fnt_idx, int do_underlines)
{
    if (fnt_idx < next_font)
    {
        if (((font_array[fnt_idx]->underline&0x0023) > 0)&&(do_underlines))
        {
            printf("<U>");
            h->uflag = 1;
        }
        if (font_array[fnt_idx]->bold >= 0x02BC)
        {
            h->bflag = 1;
            printf("<B>");
        }
        if (font_array[fnt_idx]->attr & 0x0002)
        {
            h->iflag = 1;
            printf("<I>");
        }
        if (font_array[fnt_idx]->attr & 0x0008)
        {
            h->sflag = 1;
            printf("<S>");
        }
        if ((font_array[fnt_idx]->super & 0x0003) == 0x0001)
        {
            h->spflag = 1;
            printf("<SUP>");
        }
        else if ((font_array[fnt_idx]->super & 0x0003) == 0x0002)
        {
            h->sbflag = 1;
            printf("<SUB>");
        }
    }
}

void output_end_html_attr(html_attr *h)
{
    if (h->sbflag)
    {
        printf("</SUB>");
        h->sbflag = 0;
    }
    else if (h->spflag)
    {
        printf("</SUP>");
        h->spflag = 0;
    }
    if (h->sflag)
    {
        printf("</S>");
        h->sflag = 0;
    }
    if (h->iflag)
    {
        printf("</I>");
        h->iflag = 0;
    }
    if (h->bflag)
    {
        printf("</B>");
        h->bflag = 0;
    }
    if (h->uflag)
    {
        if (h->uflag == 1)
            printf("</U>");
        else
            printf("</A>");
        h->uflag = 0;
    }
}
