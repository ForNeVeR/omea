
#include "xlhtml.h"







extern void do_cr(void);
extern int  center_tables;
extern int  first_sheet;
extern int  last_sheet;
extern uni_string  default_font;
extern void trim_sheet_edges(unsigned int);
extern int  next_ws_title;
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
extern int  disclamers;
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
extern char colorTab[MAX_COLORS];
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
extern int Csv;
work_sheet **ws_array;
font_attr **font_array;
xf_attr **xf_array;

extern int IsCellNumeric(cell *);
extern int IsCellSafe(cell *);
extern int IsCellFormula(cell *);
extern void output_formatted_data(uni_string *, U16, int, int);
extern void SetupExtraction(void);


void OutputPartialTableAscii(void)
{
    int i, j, k;

    SetupExtraction();

    /* Here's where we dump the Html Page out */
    for (i=first_sheet; i<=last_sheet; i++) /* For each worksheet */
    {
        if (ws_array[i] == 0)
            continue;
        if ((ws_array[i]->biggest_row == -1)||(ws_array[i]->biggest_col == -1))
            continue;
        if (ws_array[i]->c_array == 0)
            continue;

        /* Now dump the table */
        for (j=ws_array[i]->first_row; j<=ws_array[i]->biggest_row; j++)
        {
            for (k=ws_array[i]->first_col; k<=ws_array[i]->biggest_col; k++)
            {
                int safe, numeric=0;
                cell *c = ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k]; /* This stuff happens for each cell... */

                if (c)
                {
                    numeric = IsCellNumeric(c);
                    if (!numeric && Csv)
                        printf("\"");
                    safe = IsCellSafe(c);

                    if (c->ustr.str)
                    {
                        if (safe)
                            output_formatted_data(&(c->ustr), xf_array[c->xfmt]->fmt_idx, numeric, IsCellFormula(c));
                        else
                            OutputString(&(c->ustr));
                    }
                    else if (!Csv)
                        printf(" ");    /* Empty cell... */
                }
                else
                {       /* Empty cell... */
                    if (!Csv)
                        printf(" ");
                    else
                        printf("\"");
                }
                if (ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k])  /* Honor Column spanning ? */
                {
                    if (ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k]->colspan != 0)
                        k += ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k]->colspan-1;
                }
                if (!numeric && Csv)
                    printf("\"");

                if (Csv && (k < ws_array[i]->biggest_col))
                {   /* big cheat here: quoting everything! */
                    putchar(',');   /* Csv Cell Separator */
                }
                else
                {
                    if (( !Csv )&&( k != ws_array[i]->biggest_col ))
                        putchar('\t');  /* Ascii Cell Separator */
                }
            }
            if (Csv)
                printf("\r\n");
            else
                putchar(0x0A);      /* Row Separator */
        }
        if (!Csv)
            printf("\n\n");         /* End of Table 2 LF-CR */
    }
}
