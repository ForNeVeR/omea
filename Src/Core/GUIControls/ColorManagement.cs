// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Color Management routines.
	/// Contains a few simple static functions for working with RGB, BGR and HLS colors.
	/// Based in parts on ancient MSDN C++ samples (HLS-RGB conversion).
	/// </summary>
	public class ColorManagement
	{
		/// <summary>
		/// Background for MaxHLS.
		/// </summary>
		protected static byte c_nMaxHLS = 252;

		/// <summary>
		/// H, L, and S vary over 0-c_nMaxHLS.
		/// Best if divisible by 6.
		/// Must fit in a System.Byte.
		/// </summary>
		public static byte MaxHLS
		{
			get { return c_nMaxHLS; }
		}

		/// <summary>
		/// Background for MaxRGB.
		/// </summary>
		protected static byte c_nMaxRGB = 255;

		/// <summary>
		/// R, G, and B vary over 0-c_nMaxRGB.
		/// Must fit in a <see cref="System.Byte"/>.
		/// </summary>
		public static byte MaxRGB
		{
			get { return c_nMaxRGB; }
		}

		/// <summary>
		/// Background for UndefinedHue.
		/// </summary>
		protected static byte c_nUndefinedHue = 168;

		/// <summary>
		/// Hue is undefined if Saturation is 0 (grey-scale). This value determines where the Hue scrollbar is initially set for achromatic colors.
		/// Set tot 2/3 of MaxHLS by default.
		/// </summary>
		public static byte UndefinedHue
		{
			get { return c_nUndefinedHue; }
		}

		/// <summary>
		/// RGBtoHLS() takes a DWORD RGB value and translates it to HLS BYTEs.
		///
		/// A point of reference for the algorithms is Foley and Van Dam, "Fundamentals of Interactive Computer Graphics," Pages 618-19. Their algorithm is in floating point. CHART implements a less general (hardwired ranges) integral algorithm.
		///
		/// There are potential round-off errors throughout this sample. ((0.5 + x)/y) without floating point is phrased ((x + (y/2))/y), yielding a very small round-off error. This makes many of the following divisions look strange.
		/// </summary>
		/// <param name="lRGBColor">The source RGB color.</param>
		/// <param name="H">Resulting Hue value.</param>
		/// <param name="L">Resulting Luminance value.</param>
		/// <param name="S">Resulting Saturation value.</param>
		public static void RGBtoHLS( UInt32 lRGBColor, out Byte H, out Byte L, out Byte S )
		{
			UInt16 R, G, B; /* input RGB values */
			Byte cMax, cMin; /* Math.Max and Math.Min RGB values */
			UInt16 Rdelta, Gdelta, Bdelta; /* intermediate value: % of spread from Math.Max */

			/* get R, G, and B out of System.UInt32 */
			R = GetRValue( lRGBColor );
			G = GetGValue( lRGBColor );
			B = GetBValue( lRGBColor );

			/* calculate lightness */
			cMax = (byte) Math.Max( Math.Max( R, G ), B );
			cMin = (byte) Math.Min( Math.Min( R, G ), B );
			L = (byte) ((((cMax + cMin) * c_nMaxHLS) + c_nMaxRGB) / (2 * c_nMaxRGB));

			if( cMax == cMin )
			{ /* r=g=b --> achromatic case */
				S = 0; /* saturation */
				H = c_nUndefinedHue; /* hue */
			}
			else
			{ /* chromatic case */
				/* saturation */
				if( L <= (c_nMaxHLS / 2) )
					S = (byte) ((((cMax - cMin) * c_nMaxHLS) + ((cMax + cMin) / 2)) / (cMax + cMin));
				else
					S = (byte) ((((cMax - cMin) * c_nMaxHLS) + ((2 * c_nMaxRGB - cMax - cMin) / 2)) / (2 * c_nMaxRGB - cMax - cMin));

				/* hue */
				Rdelta = (byte) ((((cMax - R) * (c_nMaxHLS / 6)) + ((cMax - cMin) / 2)) / (cMax - cMin));
				Gdelta = (byte) ((((cMax - G) * (c_nMaxHLS / 6)) + ((cMax - cMin) / 2)) / (cMax - cMin));
				Bdelta = (byte) ((((cMax - B) * (c_nMaxHLS / 6)) + ((cMax - cMin) / 2)) / (cMax - cMin));

				if( R == cMax )
					H = (byte) (Bdelta - Gdelta);
				else if( G == cMax )
					H = (byte) ((c_nMaxHLS / 3) + Rdelta - Bdelta);
				else /* B == cMax */
					H = (byte) (((2 * c_nMaxHLS) / 3) + Gdelta - Rdelta);

				if( H < 0 )
					H += c_nMaxHLS;
				if( H > c_nMaxHLS )
					H -= c_nMaxHLS;
			}
		}

		/// <summary>
		/// Utility routine for HLStoRGB.
		/// </summary>
		public static UInt16 HueToRGB( UInt16 n1, UInt16 n2, UInt16 hue )
		{
			/* range check: note values passed add/subtract thirds of range */
			if( hue < 0 )
				hue += (ushort) c_nMaxHLS;

			if( hue > c_nMaxHLS )
				hue -= (ushort) c_nMaxHLS;

			/* return r,g, or b value from this tridrant */
			if( hue < (c_nMaxHLS / 6) )
				return (ushort) (n1 + (((n2 - n1) * hue + (c_nMaxHLS / 12)) / (c_nMaxHLS / 6)));
			if( hue < (c_nMaxHLS / 2) )
				return (n2);
			if( hue < ((c_nMaxHLS * 2) / 3) )
				return (ushort) (n1 + (((n2 - n1) * (((c_nMaxHLS * 2) / 3) - hue) + (c_nMaxHLS / 12)) / (c_nMaxHLS / 6)));
			else
				return (n1);
		}

		/// <summary>
		/// Converts an HLS color to an RGB color and returns three byte components.
		/// </summary>
		public static void HLStoRGB( UInt16 H, UInt16 L, UInt16 S, out Byte R, out Byte G, out Byte B )
		{
			UInt16 Magic1, Magic2; /* calculated magic numbers (really!) */

			if( S == 0 )
			{ /* achromatic case */
				R = G = B = (byte) ((L * c_nMaxRGB) / c_nMaxHLS);
				if( H != c_nUndefinedHue )
				{
					/* ERROR */
				}
			}
			else
			{ /* chromatic case */
				/* set up magic numbers */
				if( L <= (c_nMaxHLS / 2) )
					Magic2 = (ushort) ((L * (c_nMaxHLS + S) + (c_nMaxHLS / 2)) / c_nMaxHLS);
				else
					Magic2 = (ushort) (L + S - ((L * S) + (c_nMaxHLS / 2)) / c_nMaxHLS);
				Magic1 = (ushort) (2 * L - Magic2);

				/* get RGB, change units from c_nMaxHLS to c_nMaxRGB */
				R = (byte) ((HueToRGB( Magic1, Magic2, (ushort) (H + (c_nMaxHLS / 3)) ) * c_nMaxRGB + (c_nMaxHLS / 2)) / c_nMaxHLS);
				G = (byte) ((HueToRGB( Magic1, Magic2, H ) * c_nMaxRGB + (c_nMaxHLS / 2)) / c_nMaxHLS);
				B = (byte) ((HueToRGB( Magic1, Magic2, (ushort) (H - (c_nMaxHLS / 3)) ) * c_nMaxRGB + (c_nMaxHLS / 2)) / c_nMaxHLS);
			}
		}

		/// <summary>
		/// Converts HLS to a <see cref="System.Drawing.Color"/> object.
		/// </summary>
		public static Color HLStoRGB( UInt16 H, UInt16 L, UInt16 S )
		{
			byte R, G, B;
			HLStoRGB( H, L, S, out R, out G, out B );
			return Color.FromArgb( R, G, B );
		}

		/// <summary>
		/// Works just like HLStoRGB, but reverses the return value (BGR DWORD instead of an RGB one).
		/// This is useful in case of Windows bitmaps which use BGR colors.
		/// </summary>
		public static UInt32 HLStoBGR( UInt16 hue, UInt16 lum, UInt16 sat )
		{
			UInt16 R, G, B; /* RGB component values */
			UInt16 Magic1, Magic2; /* calculated magic numbers (really!) */

			if( sat == 0 )
			{ /* achromatic case */
				R = G = B = (ushort) ((lum * c_nMaxRGB) / c_nMaxHLS);
				if( hue != c_nUndefinedHue )
				{
					/* ERROR */
				}
			}
			else
			{ /* chromatic case */
				/* set up magic numbers */
				if( lum <= (c_nMaxHLS / 2) )
					Magic2 = (ushort) ((lum * (c_nMaxHLS + sat) + (c_nMaxHLS / 2)) / c_nMaxHLS);
				else
					Magic2 = (ushort) (lum + sat - ((lum * sat) + (c_nMaxHLS / 2)) / c_nMaxHLS);
				Magic1 = (ushort) (2 * lum - Magic2);

				/* get RGB, change units from c_nMaxHLS to c_nMaxRGB */
				R = (ushort) ((HueToRGB( Magic1, Magic2, (ushort) (hue + (c_nMaxHLS / 3)) ) * c_nMaxRGB + (c_nMaxHLS / 2)) / c_nMaxHLS);
				G = (ushort) ((HueToRGB( Magic1, Magic2, hue ) * c_nMaxRGB + (c_nMaxHLS / 2)) / c_nMaxHLS);
				B = (ushort) ((HueToRGB( Magic1, Magic2, (ushort) (hue - (c_nMaxHLS / 3)) ) * c_nMaxRGB + (c_nMaxHLS / 2)) / c_nMaxHLS);
			}

			return (RGB( (byte) B, (byte) G, (byte) R ));
		}

		/// <summary>
		/// Mixes two colors together in the proportion specified.
		/// </summary>
		public static UInt32 Mix( UInt32 colorA, UInt32 colorB, double fA )
		{
			double fB = 1.0 - fA;
			return
				(((UInt32) (((colorA & 0xFF) >> 0) * fA + ((colorB & 0xFF) >> 0) * fB)) & 0xFF) << 0 |
					(((UInt32) (((colorA & 0xFF00) >> 8) * fA + ((colorB & 0xFF00) >> 8) * fB)) & 0xFF) << 8 |
					(((UInt32) (((colorA & 0xFF0000) >> 16) * fA + ((colorB & 0xFF0000) >> 16) * fB)) & 0xFF) << 16;
		}

		/// <summary>
		/// Mixes two colors together in the proportion specified.
		/// </summary>
		/// <param name="colorA">First color.</param>
		/// <param name="colorB">Second color.</param>
		/// <param name="proportion">A number in between <c>0.0</c> and <c>1.0</c>.</param>
		/// <returns>The new color.</returns>
		public static Color Mix( Color colorA, Color colorB, double proportion )
		{
			if( (proportion < 0) || (proportion > 1) )
				throw new ArgumentOutOfRangeException( "proportion", proportion, "Must be in between 0.0 and 1.0." );

			double back = 1.0 - proportion;
			return Color.FromArgb(
				(int) (colorA.R * proportion + colorB.R * back),
				(int) (colorA.G * proportion + colorB.G * back),
				(int) (colorA.B * proportion + colorB.B * back) );
		}

		/// <summary>
		/// Converts a DWORD RGB color to BGR, or vice versa as it's symmetrical.
		/// </summary>
		public static UInt32 RGB2BGR( UInt32 color )
		{
			return RGB( GetBValue( color ), GetGValue( color ), GetRValue( color ) );
		}

		/// <summary>
		/// Mixes two colors
		/// Does the same as Mix but for a fixed 1:1 proportion.
		/// </summary>
		/// <param name="rgbA"></param>
		/// <param name="rgbB"></param>
		/// <returns></returns>
		public static UInt32 BlendTwo( UInt32 rgbA, UInt32 rgbB )
		{
			return (RGB( (byte) ((GetRValue( rgbA ) + GetRValue( rgbB )) >> 1), (byte) ((GetGValue( rgbA ) + GetGValue( rgbB )) >> 1), (byte) ((GetBValue( rgbA ) + GetBValue( rgbB )) >> 1) ));
		}

		/// <summary>
		/// Extracts the R value from the RGB color (R is less significant).
		/// </summary>
		public static Byte GetRValue( UInt32 RGB )
		{
			return (Byte) (RGB & 0xFF);
		}

		/// <summary>
		/// Extracts the G value from the RGB color (R is less significant).
		/// </summary>
		public static Byte GetGValue( UInt32 RGB )
		{
			return (Byte) ((RGB >> 8) & 0xFF);
		}

		/// <summary>
		/// Extracts the B value from the RGB color (R is less significant).
		/// </summary>
		public static Byte GetBValue( UInt32 RGB )
		{
			return (Byte) ((RGB >> 16) & 0xFF);
		}

		/// <summary>
		/// Produces an RGB color out of R, G and B values (R is less significant).
		/// </summary>
		public static UInt32 RGB( Byte R, Byte G, Byte B )
		{
			return (UInt32) R | ((UInt32) G << 8) | ((UInt32) B << 16);
		}

		/// <summary>
		/// Produces an RGB color out of R, G and B values (R is less significant).
		/// </summary>
		public static UInt32 RGB( Color color )
		{
			return (UInt32) color.R | ((UInt32) color.G << 8) | ((UInt32) color.B << 16);
		}

		/// <summary>
		/// Converts a color value into an HTML-compatible hex-string.
		/// </summary>
		public static string Hex(Color color)
		{
			return String.Format( "#{0,2:X}{1,2:X}{2,2:X}", color.R, color.G, color.B ).Replace( ' ', '0' );
		}
	}
}
