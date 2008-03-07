/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using  System;
using  System.Globalization;
namespace JetBrains.Omea.TextIndex
{
    public class TextDelimitingCategories
    {
        static TextDelimitingCategories()
        {
            CleaningCategories[ (int)UnicodeCategory.SpaceSeparator ] = true;

            CleaningCategories[ (int)UnicodeCategory.ConnectorPunctuation ] = true;
            CleaningCategories[ (int)UnicodeCategory.DashPunctuation ] = true;
            CleaningCategories[ (int)UnicodeCategory.OpenPunctuation ] = true;
            CleaningCategories[ (int)UnicodeCategory.ClosePunctuation ] = true;
            CleaningCategories[ (int)UnicodeCategory.InitialQuotePunctuation ] = true;
            CleaningCategories[ (int)UnicodeCategory.FinalQuotePunctuation ] = true;
            CleaningCategories[ (int)UnicodeCategory.OtherPunctuation ] = true;
            CleaningCategories[ (int)UnicodeCategory.CurrencySymbol ] = true;
            CleaningCategories[ (int)UnicodeCategory.MathSymbol ] = true;
            CleaningCategories[ (int)UnicodeCategory.ModifierSymbol ] = true;
            CleaningCategories[ (int)UnicodeCategory.Control ] = true;

            DelimitingCategories[ (int)UnicodeCategory.SpaceSeparator ] = true;
            DelimitingCategories[ (int)UnicodeCategory.Control ] = true;
            DelimitingCategories[ (int)UnicodeCategory.OtherNotAssigned ] = true;
            DelimitingCategories[ (int)UnicodeCategory.NonSpacingMark ] = true;
            DelimitingCategories[ (int)UnicodeCategory.OtherLetter ] = true;
            DelimitingCategories[ (int)UnicodeCategory.OtherSymbol ] = true;
            DelimitingCategories[ (int)UnicodeCategory.OtherPunctuation ] = true;
            DelimitingCategories[ (int)UnicodeCategory.MathSymbol ] = true;
            DelimitingCategories[ (int)UnicodeCategory.OpenPunctuation ] = true;
            DelimitingCategories[ (int)UnicodeCategory.ClosePunctuation ] = true;
            DelimitingCategories[ (int)UnicodeCategory.FinalQuotePunctuation ] = true;
            DelimitingCategories[ (int)UnicodeCategory.InitialQuotePunctuation ] = true;
        }

		public static bool  IsDelimiter( int charCat )
		{
			return DelimitingCategories[ charCat ];
		}
		public static bool  IsDelimiter( char ch )
		{
			return DelimitingCategories[ (int)Char.GetUnicodeCategory( ch ) ];
		}
		public static bool  IsCleaningCat( int charCat )
        {
            return CleaningCategories[ charCat ];
        }
		public static bool  IsCleaningCat( char ch )
        {
            return CleaningCategories[ (int)Char.GetUnicodeCategory( ch ) ];
        }

        //---------------------------------------------------------------------
        private static bool[]    CleaningCategories = new bool[ 30 ];
        private static bool[]    DelimitingCategories = new bool[ 30 ];
    }
}