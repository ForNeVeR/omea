// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using JetBrains.Annotations;

using Color=System.Windows.Media.Color;
using Control=System.Windows.Controls.Control;
using FontFamily=System.Windows.Media.FontFamily;
using Image=System.Windows.Controls.Image;
using PixelFormat=System.Drawing.Imaging.PixelFormat;
using Point=System.Drawing.Point;
using Size=System.Windows.Size;

namespace JetBrains.UI.Avalon.Interop
{
	/// <summary>
	/// Carries out the internal members out of the <c>WindowsFormsIntegration</c> DLL, and some more.
	/// Bridges the Avalon and WinForms worlds.
	/// </summary>
	public static class WindowsFormsIntegration
	{
		#region Operations

		/// <summary>
		/// Tears apart a WinForms <see cref="Font"/> and applies it to an Avalon element.
		/// </summary
		public static TElement SetFontA<TElement>(this TElement element, Font font) where TElement : TextElement
		{
			element.FontFamily = new FontFamily(font.FontFamily.Name);
			element.FontWeight = font.Bold ? FontWeights.Bold : FontWeights.Normal;
			element.FontStyle = font.Italic ? FontStyles.Italic : FontStyles.Normal;
			return element;
		}

		/// <summary>
		/// Tears apart a WinForms <see cref="Font"/> and applies it to an Avalon element.
		/// </summary
		public static TElement SetFontB<TElement>(this TElement element, Font font, bool sized) where TElement : Control
		{
			element.FontFamily = new FontFamily(font.FontFamily.Name);
			element.FontWeight = font.Bold ? FontWeights.Bold : FontWeights.Normal;
			element.FontStyle = font.Italic ? FontStyles.Italic : FontStyles.Normal;
			if(sized)
				element.FontSize = font.Size;
			return element;
		}

		/// <summary>
		/// Tears apart a WinForms <see cref="Font"/> and applies it to an Avalon element.
		/// </summary
		public static TElement SetFontC<TElement>(this TElement element, Font font, bool sized) where TElement : TextBlock
		{
			element.FontFamily = new FontFamily(font.FontFamily.Name);
			element.FontWeight = font.Bold ? FontWeights.Bold : FontWeights.Normal;
			element.FontStyle = font.Italic ? FontStyles.Italic : FontStyles.Normal;
			if(sized)
				element.FontSize = font.Size;
			return element;
		}

		public static Color ToAvalonColor(this System.Drawing.Color value)
		{
			return Color.FromArgb(value.A, value.R, value.G, value.B);
		}

		public static Image ToAvalonImage([NotNull] this System.Drawing.Image value)
		{
			if(value == null)
				throw new ArgumentNullException("value");

			var bmp = value as Bitmap;
			if(bmp == null)
				throw new InvalidOperationException(string.Format("Your image is not a bitmap."));
			BitmapData bits = bmp.LockBits(new Rectangle(new Point(), bmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			try
			{
				return new Image {Source = BitmapSource.Create(bmp.Width, bmp.Height, 96, 96, PixelFormats.Bgra32, null, bits.Scan0, bits.Stride * bits.Height, bits.Stride), Stretch = Stretch.None};
			}
			finally
			{
				bmp.UnlockBits(bits);
			}
		}

		public static Size ToAvalonSize(this SizeF value)
		{
			double width = value.Width == float.MaxValue ? double.PositiveInfinity : value.Width;
			double height = value.Height == float.MaxValue ? double.PositiveInfinity : value.Height;
			return new Size(width, height);
		}

		public static Size ToAvalonSize(this System.Drawing.Size value)
		{
			double width = value.Width == int.MaxValue ? double.PositiveInfinity : value.Width;
			double height = value.Height == int.MaxValue ? double.PositiveInfinity : value.Height;
			return new Size(width, height);
		}

		public static ImageSource ToImageSource(this Icon icon)
		{
			var memoryStream = new MemoryStream();
			icon.Save(memoryStream);
			memoryStream.Seek(0, SeekOrigin.Begin);

			var decoder = new IconBitmapDecoder(memoryStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
			return decoder.Frames[0];
		}

		public static MessageBoxButtons ToWinFormsMessageBoxButtons(this MessageBoxButton button)
		{
			return (MessageBoxButtons)button; // Those are WinAPI constants in background, so they're compatible
		}

		public static MessageBoxIcon ToWinFormsMessageBoxIcon(this MessageBoxImage image)
		{
			return (MessageBoxIcon)image; // Those are WinAPI constants in background, so they're compatible
		}

		public static SizeF ToWinFormsSize(this Vector value)
		{
			return new SizeF((float)value.X, (float)value.Y);
		}

		public static System.Drawing.Size ToWinFormsSizeCeiling(this Vector value)
		{
			return System.Drawing.Size.Ceiling(ToWinFormsSize(value));
		}

		#endregion
	}
}
