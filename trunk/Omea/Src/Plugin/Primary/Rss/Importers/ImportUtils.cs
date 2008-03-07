/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	internal class ImportUtils
	{
        internal struct FeedUpdateData
        {
            internal int freq;
            internal string period;
        }

        internal static FeedUpdateData ConvertUpdatePeriod( string text, int divForMinutes )
        {
            FeedUpdateData res = new FeedUpdateData();
            int rr;

            res.freq = 4;
            res.period = "hourly";

            try
            {
                rr = Int32.Parse( text );
            }
            catch
            {
                return res;
            }
            rr /= divForMinutes;

            res.period = "minutely";
            res.freq = Math.Max( rr, 1 );

            if( rr < 60 || 0 != rr % 60 ) // Not integer number of hours. Exit now
            {
                return res;
            }
            rr /= 60; // convert to hours
            res.period = "hourly";
            res.freq = rr;
            // Days?
            if( rr < 24 || 0 != rr % 24 ) // Not integer number of days
            {
                return res;
            }
            rr /= 24;
            res.period = "daily";
            res.freq = rr;
            // Weeks?
            if( rr < 7 || 0 != rr % 7 ) // Not integer number of weeks
            {
                return res;
            }
            rr /= 7;
            res.period = "weekly";
            res.freq = rr;
            return res;
        }

        internal static XmlElement GetUniqueChild( XmlElement element, string name )
        {
            XmlNodeList l = element.GetElementsByTagName( name );
            if( l.Count < 1 )
            {
                return null;
            }
            return (XmlElement)l[0];
        }

        internal static string GetUniqueChildText( XmlElement element, string name )
        {
            XmlElement e = GetUniqueChild( element, name );
            if( e != null )
            {
                return e.InnerText;
            }
            else
            {
                return null;
            }
        }

        internal static void Child2Prop( XmlElement element, string name, IResource res, params int[] props )
        {
            string text = GetUniqueChildText( element, name );
            if( text != null )
            {
                foreach( int p in props )
                {
                    res.SetProp( p, text );
                }
            }
        }

        internal static void Attrib2Prop( XmlElement element, string name, IResource res, params int[] props )
        {
            string text = element.GetAttribute( name );
            if( text != null && text.Length > 0 )
            {
                foreach( int p in props )
                {
                    res.SetProp( p, text );
                }
            }
        }

        internal static void InnerText2Prop( XmlElement element, IResource res, params int[] props )
        {
            if( element != null )
            {
                foreach( int p in props )
                {
                    res.SetProp( p, element.InnerText );
                }
            }
        }

	    internal static void UpdateProgress( int progress, string message )
	    {
            if( Core.ProgressWindow != null )
            {
                Core.ProgressWindow.UpdateProgress( Math.Min( progress, 100 ), message, null );
            }
        }

        private delegate void ReportErrorJob( string title, string message );
        private static void DoReportError( string title, string message )
        {
            MessageBox.Show( message, title, MessageBoxButtons.OK, MessageBoxIcon.Error );
        }

        internal static void ReportError( string title, string message )
        {
            Core.UserInterfaceAP.RunJob( new ReportErrorJob( DoReportError ), title, message );
        }
	}
}
