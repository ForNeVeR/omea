
#include "xlhtml.h"

extern int  first_sheet;
extern int  last_sheet;
extern uni_string  default_font;
extern void trim_sheet_edges(unsigned int);
extern int  next_ws_title;
extern void SetupExtraction(void);
extern void update_default_font(unsigned int);
extern void OutputString(uni_string * );
extern char *lastUpdated; 
extern int  file_version;
extern int  NoFormat;
extern int  notAccurate;
extern int  formula_warnings;
extern int  disclaimers;
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
extern char filename[256];
extern int  UnicodeStrings;
extern int  CodePage; 
extern char *title;
extern void update_default_alignment(unsigned int, int);
extern void output_cell( cell *, int); 
extern uni_string author;

work_sheet **ws_array;


void OutputTableXML(void)
{
    int i, j, k;

        printf( "<?xml version=\"1.0\" encoding=\"" );
        switch (UnicodeStrings)
    {
        case 0:
            printf("iso-8859-1\" ?>\n");        /* Latin-1 */
            break;
        case 1:
            printf("windows-1252\"?>\n");       /* Microsoft */
            break;
        default:
            printf("utf-8\"?>\n");          /* Unicode */
            break;
    }

    SetupExtraction();

    printf( "<excel_workbook>\n" );
    printf( "\t<sheets>\n" );

    /* Here's where we dump the Html Page out */
    for (i=first_sheet; i<=last_sheet; i++) /* For each worksheet */
    {
        trim_sheet_edges(i);
             update_default_font(i);
        if (ws_array[i] == 0)
            continue;
        if ((ws_array[i]->biggest_row == -1)||(ws_array[i]->biggest_col == -1))
            continue;
        if (ws_array[i]->c_array == 0)
            continue;

        printf( "\t\t<sheet>\n" );
        printf( "\t\t\t<page>%d</page>\n", i );
        
        /* Print its name */
        if (next_ws_title > 0)
        {
            if (ws_array[i]->ws_title.str)
            {
                printf("\t\t\t<pagetitle>");
                OutputString(&ws_array[i]->ws_title);
                printf("</pagetitle>\n");
            }
            else
                printf("\t\t\t<pagetitle>(Unknown Page)</pagetitle>\n");
        }

        printf( "\t\t\t<firstrow>%ld</firstrow>\n", ws_array[i]->first_row );
        printf( "\t\t\t<lastrow>%ld</lastrow>\n", ws_array[i]->biggest_row );
        printf( "\t\t\t<firstcol>%d</firstcol>\n", ws_array[i]->first_col );
        printf( "\t\t\t<lastcol>%d</lastcol>\n", ws_array[i]->biggest_col );
        printf( "\t\t\t<rows>\n" );

        for (j=ws_array[i]->first_row; j<=ws_array[i]->biggest_row; j++)
        {
            update_default_alignment(i, j);
            printf("\t\t\t\t<row>\n");
            for (k=ws_array[i]->first_col; k<=ws_array[i]->biggest_col; k++)
            {
                printf("\t\t\t\t\t<cell row=\"%d\" col=\"%d\">", j, k );
                output_cell(ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k], 1); /* This stuff happens for each cell... */
                printf("</cell>\n" );
                if (ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k])
                {
                    if (ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k]->colspan != 0)
                        k += ws_array[i]->c_array[(j*ws_array[i]->max_cols)+k]->colspan-1;
                }
                                
            }

            printf("</row>\n");
        }
        printf( "\t\t\t</rows>\n" );
        printf("\t\t</sheet>\n"); 
    }
     printf( "\t</sheets>\n" );

    /* Print the author's name in itallics... */
    if (author.str)
    {
        printf("\t<author>");
        OutputString(&author);
        printf("</author>\n");
    }

    /* Print when & how the file was last updated. */
    if (lastUpdated)
        printf("\t<lastwrite>%s</lastwrite>", lastUpdated);
    printf( "\t<excelversion>" );
    switch (file_version)
    {
        case EXCEL95:
            printf("using Excel 5.0 or 95");
            break;
        case EXCEL97:
            printf("using Excel 97/2000");
            break;
        default:
            printf("using Excel ????");
            break;
    }
    printf("</excelversion>\n");
    
    /* Next print Disclaimers... */
    if (NoFormat)
        printf("\t<noformat>%d</noformat>\n", NoFormat );
    if ((notAccurate)&&(formula_warnings)&&(disclaimers))
        printf("\t<accuracy>%d</accuracy>\n", notAccurate );
    if (NotImplemented&&(disclaimers))
        printf("\t<notimplemented>%d</notimplemented>\n", NotImplemented );
    if (Unsupported&&(disclaimers))
        printf("\t<unsupported>%d</unsupported>\n", Unsupported );

    /* Now out exceeded capacity warnings... */
    if (MaxWorksheetsExceeded)
        printf("\t<MaxWorksheetsExceeded>The Maximum Number of Worksheets were exceeded, you might want to increase it.</MaxWorksheetsExceeded>\n ");
    if (MaxRowExceeded)
        printf("\t<MaxRowExceeded>The Maximum Number of Rows were exceeded, you might want to increase it.</MaxRowExceeded>\n ");
    if (MaxColExceeded)
        printf("\t<MaxColExceeded>The Maximum Number of Columns were exceeded, you might want to increase it.</MaxColExceeded>\n");
    if (MaxStringsExceeded)
        printf("\t<MaxStringsExceeded>The Maximum Number of Strings were exceeded, you might want to increase it.</MaxStringsExceeded>\n");
    if (MaxFontsExceeded)
        printf("\t<MaxFontsExceeded>The Maximum Number of Fonts were exceeded, you might want to increase it.</MaxFontsExceeded>\n");
    if (MaxPalExceeded)
        printf("\t<MaxPalExceeded>The Maximum Number of Color Palettes were exceeded, you might want to increase it.</MaxPalExceeded>\n");
    if (MaxXFExceeded)
        printf("\t<MaxXFExceeded>The Maximum Number of Extended Formats were exceeded, you might want to increase it.</MaxXFExceeded>\n");
    if (MaxFormatsExceeded)
        printf("\t<MaxFormatsExceeded>The Maximum Number of Formats were exceeded, you might want to increase it.</MaxFormatsExceeded>\n");

    /* Output Credit */
    printf("\t<tool>Created with xlhtml %s</tool>\n", VERSION);
    printf("\t<toollink>http://chicago.sf.net/xlhtml/</toollink>\n");
     printf( "</excel_workbook>\n" );
}
