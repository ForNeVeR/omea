// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Wrapper class for working with Windows XP themes.
	/// </summary>
	public class XPThemes
	{
	    public enum ColorProperty
	    {
		    AccentColorHint = 0xeef,
		    BorderColor = 0xed9,
		    BorderColorHint = 0xeee,
		    EdgeDarkShadowColor = 0xedf,
		    EdgeFillColor = 0xee0,
		    EdgeHighlightColor = 0xedd,
		    EdgeLightColor = 0xedc,
		    EdgeShadowColor = 0xede,
		    FillColor = 0xeda,
		    FillColorHint = 0xeed,
		    GlowColor = 0xee8,
		    GlyphTextColor = 0xeeb,
		    GlyphTransparentColor = 3820,
		    GradientColor1 = 3810,
		    GradientColor2 = 0xee3,
		    GradientColor3 = 0xee4,
		    GradientColor4 = 0xee5,
		    GradientColor5 = 0xee6,
		    ShadowColor = 0xee7,
		    TextBorderColor = 0xee9,
		    TextColor = 0xedb,
		    TextShadowColor = 0xeea,
		    TransparentColor = 0xee1
	    }

        [DllImport( "uxtheme.dll")]
        private static extern bool IsAppThemed();

        [DllImport("uxtheme", ExactSpelling=true, CharSet=CharSet.Unicode)]
        private extern static Int32 GetCurrentThemeName( StringBuilder stringThemeName, int lengthThemeName,
            StringBuilder stringColorName, int lengthColorName,
            StringBuilder stringSizeName, int lengthSizeName );

		[DllImport("uxtheme.dll", CharSet=CharSet.Auto)]
		public static extern int GetThemeColor(HandleRef hTheme, int iPartId, int iStateId, int iPropId, ref int pColor);

        private XPThemes()
		{
		}

        public static bool IsThemed
        {
            get
            {
                OperatingSystem ver = Environment.OSVersion;
                if ( ver.Platform == PlatformID.Win32NT )
                {
                    if ( ver.Version.Major > 5 || (ver.Version.Major == 5 && ver.Version.Minor >= 1) )
                    {
                        return IsAppThemed();
                    }
                }
                return false;
            }
        }

        public static string ColorSchemeName
        {
            get
            {
                StringBuilder themeName = new StringBuilder( 256 );
                StringBuilder colorSchemeName = new StringBuilder( 256 );
                StringBuilder sizeName = new StringBuilder( 256 );
                GetCurrentThemeName( themeName, 255, colorSchemeName, 255, sizeName, 255 );
                return colorSchemeName.ToString();
            }
        }
	}
}
