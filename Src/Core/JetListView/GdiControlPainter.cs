// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
/*
using System.Windows.Forms.Themes;
*/
using JetBrains.Interop.WinApi;
using JetBrains.UI.Interop;
using JetBrains.UI.RichText;

// TODO(H): reenable theming, it's been disabled to see if we can work without the UxTheme cpp component

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// An implenentation of IControlPainter which uses GDI and XPTheme functions for drawing.
	/// </summary>
	public class GdiControlPainter : IControlPainter, IDisposable
	{
		private readonly FontCache _fontCache = new FontCache();

		public void Dispose()
		{
			_fontCache.Dispose();
		}

		public void DrawFocusRect(Graphics g, Rectangle rc)
		{
			RECT rect = new RECT(rc.Left, rc.Top, rc.Right, rc.Bottom);
			IntPtr hdc = g.GetHdc();
			try
			{
				Win32Declarations.DrawFocusRect(hdc, ref rect);
			}
			finally
			{
				g.ReleaseHdc(hdc);
			}
		}

		public int DrawText(Graphics g, string text, Font font, Color color, Rectangle rc, StringFormat format)
		{
			int height;
			RectangleF rcClip = g.ClipBounds;
			RECT rect = new RECT(rc.Left, rc.Top, rc.Right, rc.Bottom);
			IntPtr hdc = g.GetHdc();
			try
			{
				IntPtr clipRgn = Win32Declarations.CreateRectRgn(0, 0, 0, 0);
				if(Win32Declarations.GetClipRgn(hdc, clipRgn) != 1)
				{
					Win32Declarations.DeleteObject(clipRgn);
					clipRgn = IntPtr.Zero;
				}
				Win32Declarations.IntersectClipRect(hdc, (int)rcClip.Left, (int)rcClip.Top, (int)rcClip.Right, (int)rcClip.Bottom);

				IntPtr hFont = _fontCache.GetHFont(font);
				IntPtr oldFont = Win32Declarations.SelectObject(hdc, hFont);
				int textColor = Win32Declarations.ColorToRGB(color);
				int oldColor = Win32Declarations.SetTextColor(hdc, textColor);
				BackgroundMode oldMode = Win32Declarations.SetBkMode(hdc, BackgroundMode.TRANSPARENT);

				DrawTextFormatFlags flags = 0;
				if((format.FormatFlags & StringFormatFlags.NoWrap) != 0)
					flags |= DrawTextFormatFlags.DT_SINGLELINE;
				else
					flags |= DrawTextFormatFlags.DT_WORDBREAK;
				if(format.Alignment == StringAlignment.Center)
					flags |= DrawTextFormatFlags.DT_CENTER;
				else if(format.Alignment == StringAlignment.Far)
					flags |= DrawTextFormatFlags.DT_RIGHT;

				if(format.LineAlignment == StringAlignment.Center)
					flags |= DrawTextFormatFlags.DT_VCENTER;
				if(format.Trimming == StringTrimming.EllipsisCharacter)
					flags |= DrawTextFormatFlags.DT_END_ELLIPSIS;
				if(format.HotkeyPrefix == HotkeyPrefix.None)
					flags |= DrawTextFormatFlags.DT_NOPREFIX;

				height = Win32Declarations.DrawText(hdc, text, text.Length, ref rect, flags);

				Win32Declarations.SelectClipRgn(hdc, clipRgn);
				Win32Declarations.DeleteObject(clipRgn);

				Win32Declarations.SetBkMode(hdc, oldMode);
				Win32Declarations.SetTextColor(hdc, oldColor);
				Win32Declarations.SelectObject(hdc, oldFont);
			}
			finally
			{
				g.ReleaseHdc(hdc);
			}
			return height;
		}

		public Size MeasureText(string text, Font font)
		{
			IntPtr hdc = Win32Declarations.GetDC(IntPtr.Zero);
			try
			{
				return MeasureText(hdc, font, text);
			}
			finally
			{
				Win32Declarations.ReleaseDC(IntPtr.Zero, hdc);
			}
		}

		public Size MeasureText(Graphics g, string text, Font font)
		{
			IntPtr hdc = g.GetHdc();
			try
			{
				return MeasureText(hdc, font, text);
			}
			finally
			{
				g.ReleaseHdc(hdc);
			}
		}

		private Size MeasureText(IntPtr hdc, Font font, string text)
		{
			SIZE sz = new SIZE();
			IntPtr hFont = _fontCache.GetHFont(font);
			IntPtr oldFont = Win32Declarations.SelectObject(hdc, hFont);
			Win32Declarations.GetTextExtentPoint32(hdc, text, text.Length, ref sz);
			Win32Declarations.SelectObject(hdc, oldFont);
			return new Size(sz.cx, sz.cy);
		}

		public Size MeasureText(Graphics g, string text, Font font, int maxWidth)
		{
			Size result;
			IntPtr hdc = g.GetHdc();
			try
			{
				IntPtr hFont = _fontCache.GetHFont(font);
				IntPtr oldFont = Win32Declarations.SelectObject(hdc, hFont);
				SIZE sz = new SIZE();
				Win32Declarations.GetTextExtentPoint32(hdc, text, text.Length, ref sz);
				if(sz.cx < maxWidth)
					result = new Size(sz.cx, sz.cy);
				else
				{
					RECT rc = new RECT(0, 0, maxWidth, Screen.PrimaryScreen.Bounds.Height);
					int height = Win32Declarations.DrawText(hdc, text, text.Length, ref rc, DrawTextFormatFlags.DT_CALCRECT | DrawTextFormatFlags.DT_WORDBREAK);
					result = new Size(maxWidth, height);
				}

				Win32Declarations.SelectObject(hdc, oldFont);
			}
			finally
			{
				g.ReleaseHdc(hdc);
			}
			return result;
		}

		public static bool IsAppThemed
		{
			get
			{
				//return UxTheme.IsAppThemed;
				return false;
			}
		}

		public void DrawCheckBox(Graphics g, Rectangle rc, ButtonState state)
		{
			if(IsAppThemed)
			{
/*
				ThemePartState partState = GetCheckBoxPart(state);
				if(partState != null)
				{
					Rectangle rcClip = new Rectangle((int)g.ClipBounds.Left, (int)g.ClipBounds.Top, (int)g.ClipBounds.Width, (int)g.ClipBounds.Height);
					try
					{
						partState.DrawBackground(g, rc, rcClip);
					}
					catch(Exception ex)
					{
						Trace.WriteLine("Failed to DrawCheckBox: " + ex.Message);
					}
				}
				else
					ControlPaint.DrawCheckBox(g, rc, state);
*/
			}
			else
				ControlPaint.DrawCheckBox(g, rc, state);
		}

		public void DrawListViewBorder(Graphics g, Rectangle rc, BorderStyle borderStyle)
		{
			if(borderStyle == BorderStyle.FixedSingle)
				ControlPaint.DrawBorder3D(g, rc, Border3DStyle.Flat);
			else if(borderStyle == BorderStyle.Fixed3D)
			{
/*
				object partState = null;
				if(IsAppThemed)
					partState = (object)GetListViewBorderPart();

				if(partState != null)
				{
					Rectangle rcClip = new Rectangle((int)g.ClipBounds.Left, (int)g.ClipBounds.Top, (int)g.ClipBounds.Width, (int)g.ClipBounds.Height);
					try
					{
						partState.DrawBackground(g, rc, rcClip);
					}
					catch(Exception ex)
					{
						Trace.WriteLine("Failed to DrawListViewBorder: " + ex.Message);
					}
				}
				else
*/
					ControlPaint.DrawBorder3D(g, rc);
			}
		}

		public int GetListViewBorderSize(BorderStyle borderStyle)
		{
			if(borderStyle == BorderStyle.None)
				return 0;
			return 1;
		}

		public void DrawTreeIcon(Graphics g, Rectangle rc, bool expanded)
		{
			if(IsAppThemed)
			{
/*
				ThemePart part = GetGlyphPart();
				if(part != null)
				{
					ThemePartState partState = part.States[expanded ? "OPENED" : "CLOSED"];

					Rectangle rcClip = new Rectangle((int)g.ClipBounds.Left, (int)g.ClipBounds.Top, (int)g.ClipBounds.Width, (int)g.ClipBounds.Height);
					try
					{
						partState.DrawBackground(g, rc, rcClip);
					}
					catch(Exception ex)
					{
						Trace.WriteLine("Failed to DrawTreeIcon: " + ex.Message);
					}
					return;
				}
*/
			}
			DefaultControlPainter.DoDrawTreeIcon(g, rc, expanded);
		}

		public Size GetTreeIconSize(Graphics g, Rectangle rc)
		{
			return DefaultControlPainter.DoGetTreeIconSize(rc);
		}

/*
		private static ThemePart GetGlyphPart()
		{
			ThemeInfo info = new ThemeInfo();
			try
			{
				WindowTheme theme = info["TREEVIEW"];
				return theme.Parts["GLYPH"];
			}
			catch(IndexOutOfRangeException)
			{
				// possibly the theme on the user's machine does not contain that part
				return null;
			}
		}
*/

/*
		private static ThemePartState GetListViewBorderPart()
		{
			ThemeInfo info = new ThemeInfo();
			try
			{
				WindowTheme theme = info["LISTVIEW"];
				ThemePart part = theme.Parts["LISTITEM"];
				return part.States["NORMAL"];
			}
			catch(IndexOutOfRangeException)
			{
				return null;
			}
		}
*/

/*
		private static ThemePartState GetCheckBoxPart(ButtonState state)
		{
			ThemeInfo info = new ThemeInfo();
			try
			{
				WindowTheme theme = info["BUTTON"];
				ThemePart part = theme.Parts["CHECKBOX"];

				string firstPart = ((state & ButtonState.Checked) != 0) ? "CHECKED" : "UNCHECKED";
				string secondPart = ((state & ButtonState.Inactive) != 0) ? "DISABLED" : "NORMAL";

				return part.States[firstPart + secondPart];
			}
			catch(IndexOutOfRangeException)
			{
				return null;
			}
		}
*/
	}
}
