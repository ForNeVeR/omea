// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Globalization;
using JetBrains.DataStructures;

namespace JetBrains.Omea.Base
{
    public class RFC822DateParser
    {
        private static HashMap _timeZones = new HashMap();
        private static CultureInfo _cultureInfo = new CultureInfo( "en-us" );

        static RFC822DateParser()
        {
            _timeZones ["UT"] = 0;
            _timeZones ["UTC"] = 0;
            _timeZones ["AST"] = 4;
            _timeZones ["ADT"] = 3;
            _timeZones ["EST"] = 5;
            _timeZones ["EDT"] = 4;
            _timeZones ["CST"] = 6;
            _timeZones ["CDT"] = 5;
            _timeZones ["MST"] = 7;
            _timeZones ["MST"] = 7;
            _timeZones ["MDT"] = 6;
            _timeZones ["PDT"] = 7;
            _timeZones ["PST"] = 8;

            _timeZones ["MSK"] = -3;
            _timeZones ["MSD"] = -4;
        }

        public static DateTime ParseDate( string dateStr )
        {
            if ( dateStr.EndsWith( ")" ) )
            {
                int ppos = dateStr.LastIndexOf( "(" );
                if ( ppos >= 0 )
                {
                    dateStr = dateStr.Substring( 0, ppos ).Trim();
                }
            }

            // strip weekdat
            int weekdayIndex = dateStr.IndexOf( "," );
            if ( weekdayIndex == 3 && Char.IsLetter( dateStr, 0 ) &&
                Char.IsLetter( dateStr, 1 ) && Char.IsLetter( dateStr, 2 ) )
            {
                dateStr = dateStr.Substring( 4 ).Trim();
            }

            int tzHours   = 0;
            int tzMinutes = 0;
            int pos = dateStr.LastIndexOf( ' ' );
            if ( pos >= 0 )
            {
                string tz = dateStr.Substring( pos+1 ).Trim();
                if (tz.Length > 0 )
                {
                    if ( tz [0] != '+' && tz [0] != '-' )
                    {
                        HashMap.Entry entry = _timeZones.GetEntry( tz );
                        if( entry != null )
                        {
                            tzHours = (int) entry.Value;
                        }
                    }
                    else
                    {
                        int tzModifier = (tz [0] == '-') ? 1 : -1;
                        tzHours = Int32.Parse( tz.Substring( 1, 2 ) ) * tzModifier;
                        tzMinutes = Int32.Parse( tz.Substring( 3, 2 ) ) * tzModifier;
                    }
                    dateStr = dateStr.Substring( 0, pos+1 ) + "GMT";
                }
            }

            string[] words = dateStr.Split( ' ' );
            bool wordsChanged = false;
            if ( words.Length >= 1 && words [0].Length == 1 )
            {
                words [0] = '0' + words [0];
                wordsChanged = true;
            }
            if ( words.Length >= 3 && words [2].Length == 2 )
            {
                words [2] = DateTime.ParseExact( words [2], "yy", _cultureInfo ).Year.ToString();
                wordsChanged = true;
            }
            if ( wordsChanged )
            {
                dateStr = String.Join( " ", words );
            }

            DateTime dt = DateTime.ParseExact( dateStr, "dd MMM yyyy HH':'mm':'ss 'GMT'", _cultureInfo );
            dt = dt.AddHours( tzHours );
            dt = dt.AddMinutes( tzMinutes );
            return dt.ToLocalTime();
        }
    }
}
