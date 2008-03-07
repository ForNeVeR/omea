/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls
{
    public class TimeUpDownEditor : UpDownBase
    {
        public TimeUpDownEditor() : base()
        {
            this.TextChanged += new EventHandler(HourUpDownEditor_TextChanged);
        }

        public bool isValid { get { return TimeParseable(); } }

        public void SetLocaleInfo( int locale_itime, int locale_itimemarkposn )
        {}

        public override void UpButton()
        {
            if( TimeParseable() )
            {
                DateTimeFormatInfo info = CultureInfo.CurrentCulture.DateTimeFormat;
                DateTime span = DateTime.Parse( Text, info );
                Text = span.AddMinutes( 15 ).ToString( "t" );
            }
        }

        public override void DownButton()
        {
            if( TimeParseable() )
            {
                DateTimeFormatInfo info = CultureInfo.CurrentCulture.DateTimeFormat;
                DateTime span = DateTime.Parse( Text, info );
                Text = span.AddMinutes( -15 ).ToString( "t" );
            }
        }

        protected override void ValidateEditText()
        {
            this.ForeColor = TimeParseable() ? Color.Black : Color.Red;
        }

        public bool TimeParseable()
        {
            try
            {
                DateTimeFormatInfo info = CultureInfo.CurrentCulture.DateTimeFormat;
                DateTime.Parse( Text, info );
                return true;
            }
            catch( Exception )
            {
                return false;
            }
        }

        public string Value { get { return Text; } set { Text = value; } }

        private void HourUpDownEditor_TextChanged(object sender, EventArgs e)
        {
            ValidateEditText();
        }

        protected override void UpdateEditText()
        {}
    }
}
