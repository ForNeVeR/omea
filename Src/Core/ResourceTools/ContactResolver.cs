// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text.RegularExpressions;

namespace JetBrains.Omea.Contacts
{
    public class ContactResolver
    {
        private static Regex  _multipleBlanksRemover = new Regex( "(  +)" );
        private static Regex  _SrJrSkipper = new Regex( ",? ([JjSs]r|I|II|III)\\.?$" );
        private static Regex  _prefixCleaner = new Regex( "^([\\\\'\"]+)" );
        private static Regex  _suffixCleaner = new Regex( "([\\\\'\"]+)$" );
        private static Regex  TitleSelector = new Regex( "^(Prof|Dr|Mr|Ms|Mrs|Miss)\\.? +", RegexOptions.IgnoreCase );
        private static Regex  AffixParCleaner = new Regex( " (\\([^()]+\\)) *$", RegexOptions.IgnoreCase );

        //---------------------------------------------------------------------
        /// <summary>
        /// Method performs a try to extract First name (FN) and Last name (LN) from
        /// Sender name string. The following templates are supported:
        /// "FN LN", "LN, FN", "FN LN, Jr", "FN LN, Sr", "FN X. LN", "FN.LN@...",
        /// "FN LN (string matching the email)", "FN LN (E-mail)", "FN LN (E-mail2)" etc.
        /// If no template is matched, Sender name is assigned to the First name.
        /// Resolve fails if sender name is empty (null or length is zero) or
        /// coinsides with email.
        /// </summary>
        /// <param name="senderName">Name of a contact</param>
        /// <param name="email">Email representing the account</param>
        /// <param name="title">(out) Resolved Title</param>
        /// <param name="firstName">(out) Resolved First name</param>
        /// <param name="middleName">(out) Resolved Middle name</param>
        /// <param name="lastName">(out) Resolved Last name</param>
        /// <param name="suffix">(out) Resolved Suffix (Sr, Jr, I, II, etc)</param>
        /// <param name="addSpec">(out) Resolved additional specificator (e.g. "(E-mail)")</param>
        /// <returns></returns>
        public static bool ResolveName( string senderName, string email,
                                        out string title, out string firstName,
                                        out string middleName, out string lastName,
                                        out string suffix, out string addSpec )
        {
            title = firstName = middleName = lastName = suffix = addSpec = string.Empty;

            //  Resolve fails if sender name is null or empty. By contract
            //  this case is allowed.
            if( senderName == null || senderName.Length == 0 )
                return false;

            string cleanedSenderName, cleanedName;
            cleanedSenderName = cleanedName = CleanSenderName( senderName );

            //-----------------------------------------------------------------
            //  If senderName contains removable garbage, stop processing and
            //  return the same garbage as result.
            //-----------------------------------------------------------------
            if( cleanedName.Length == 0 )
            {
                lastName = senderName;
                return true;
            }

            string pureName = ExtractPrefixAndSuffix( ref cleanedName, ref title, ref suffix, ref addSpec );
            if( addSpec.Length > 0 )
                pureName = CleanSenderName( pureName );

            //-----------------------------------------------------------------
            string[]  tokens = pureName.Split( ' ' );
            if( tokens.Length == 2 || tokens.Length == 3 )
            {
                bool isRevertedOrder = (tokens[ 0 ][ tokens[ 0 ].Length - 1 ] == ',');

                firstName = isRevertedOrder ? tokens[ 1 ] : tokens[ 0 ];
                lastName  = isRevertedOrder ? tokens[ 0 ] : tokens[ tokens.Length - 1 ];
                if( tokens.Length == 3 )
                    middleName = isRevertedOrder ? tokens[ 2 ] : tokens[ 1 ];

                if( isRevertedOrder )
                    lastName = lastName.Substring( 0, lastName.Length - 1 );

                if( IsCleanName( firstName ) && IsCleanName( lastName ))
                    return true;
            }

            //-----------------------------------------------------------------
            int atIndex = cleanedSenderName.IndexOf( '@', 0 );
            if ( atIndex != -1 )
            {
                string senderNameExtract = cleanedSenderName.Substring( 0, atIndex );
                int pointIndex = senderNameExtract.IndexOf( '.', 0 );

                //  NB: "From: " - eliminate our own processing from the
                //  ContactManager.UnlinkAccountAndContact.
                if( pointIndex != -1 &&
                    SplitByChar( senderNameExtract, '.', ref firstName, ref lastName ))
                {
                    if( !firstName.StartsWith( "From: " ))
                        return true;
                }
            }

            if( email != null && senderName == email )
                return false;

            //  clear if we already polluted these fields
            title = firstName = middleName = suffix = addSpec = string.Empty;
            lastName = cleanedSenderName;
            return true;
        }

        private static string CleanSenderName( string senderName )
        {
            string cleanedName = null;
            if( senderName != null )
            {
                cleanedName = senderName.Trim();

                //  remove balanced parentheses around the name
                while( IsParsSurrounded( cleanedName ) )
                    cleanedName = cleanedName.Substring( 1, cleanedName.Length - 2 );

                cleanedName = _multipleBlanksRemover.Replace( cleanedName, " " );

                Match match = _prefixCleaner.Match( cleanedName );
                if( match.Success )
                    cleanedName = cleanedName.Substring( match.Value.Length );

                match = _suffixCleaner.Match( cleanedName );
                if( _suffixCleaner.IsMatch( cleanedName ) )
                    cleanedName = cleanedName.Substring( 0, cleanedName.Length - match.Value.Length );

                cleanedName = cleanedName.Trim();
            }
            return cleanedName;
        }

        private static string ExtractPrefixAndSuffix( ref string name, ref string title,
                                                      ref string suffix, ref string addSpec )
        {
            Match match = AffixParCleaner.Match( name );
            if( match.Success )
            {
                addSpec = match.Value.Trim();
                name = name.Replace( addSpec, string.Empty ).Trim();
            }

            //-----------------------------------------------------------------
            string pureName = name;
            match = _SrJrSkipper.Match( pureName );
            if( match.Success )
            {
                suffix = match.Value;
                pureName = pureName.Replace( suffix, string.Empty ).Trim();
                if( suffix[ 0 ] == ',' )
                    suffix = suffix.Substring( 1 );
                suffix = suffix.Trim();
            }

            //-----------------------------------------------------------------
            match = TitleSelector.Match( pureName );
            if( match.Success )
            {
                title = match.Value;
                pureName = pureName.Substring( title.Length ).Trim();
                title = title.Trim();
                if( title[ title.Length - 1 ] == ',' )
                    title = title.Substring( 0, title.Length - 1 );
            }

            return pureName.Trim();
        }

        private static bool SplitByChar( string senderName, char splitter, ref string name1, ref string name2 )
        {
            string[] names = senderName.Split( splitter );
            if ( names != null && names.Length == 2 )
            {
                name1 = names[0].Trim();
                name2 = names[1].Trim();
                return true;
            }
            return false;
        }

        public static string  CompressBlanks( string str )
        {
            if( _multipleBlanksRemover.IsMatch( str ) )
            {
                str = _multipleBlanksRemover.Replace( str, " " ).Trim();
            }
            return str;
        }

        private static bool IsCleanName( string str )
        {
            foreach( char ch in str )
            {
                if( !Char.IsLetterOrDigit( ch ))
                    return false;
            }
            return true;
        }

        private static bool IsParsSurrounded( string str )
        {
            return str.Length > 2 && str[ 0 ] == '(' && str[ str.Length - 1 ] == ')';
        }
    }
}
