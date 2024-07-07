// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

using JetBrains.Interop.WinApi;

namespace JetBrains.UI.Interop
{
    /// <summary>
    /// Common Win32 Interop declarations
    /// </summary>
    public static class Win32Declarations
    {
        public const uint WS_POPUP = 0x80000000;
        public const int WM_SIZE          = 0x0005;
        public const int WM_ACTIVATE      = 0x0006;
        public const int WM_SETFOCUS      = 0x0007;
        public const int WM_GETTEXT       = 0x000D;
        public const int WM_GETTEXTLENGTH = 0x000E;
        public const int WM_ACTIVATEAPP   = 0x001C;
        public const int WM_NCPAINT = 0x0085;
        public const int WM_NCACTIVATE = 0x0086;
        public const int WM_NCMOUSEFIRST = 0x00A0;
        public const int WM_NCMOUSELAST = 0x00A9;
        public const int WM_NOTIFY = 0x004E;
        public const int WM_USER = 0x400;
        public const int WM_VSCROLL = 0x115;
        public const int WM_PAINT = 0x000F;
        public const int WM_PRINTCLIENT = 0x318;
        public const int WM_APPCOMMAND  = 0x319;
        public const int WM_SETREDRAW = 0x000B;
        public const int WM_CONTEXTMENU = 0x007B;
        public const int WM_CREATE = 0x0001;
        public const int WM_ERASEBKGND = 0x0014;
        public const int WM_COMMAND = 0x0111;
        public const int WM_EXITMENULOOP = 0x212;
        public const int WM_ACTIVATETOPLEVEL = 0x36E;
        public const int NM_FIRST = 0;
        public const int NM_CUSTOMDRAW = NM_FIRST - 12;
        public const int TTN_FIRST = (0 - 520);
        public const int TTN_SHOW = (TTN_FIRST - 1);
        public const int TTN_NEEDTEXTW = (TTN_FIRST - 10);
        public const int CDDS_PREPAINT = 0x1;
        public const int CDDS_POSTPAINT = 0x2;
        public const int CDDS_POSTERASE = 0x4;
        public const int CDDS_ITEM = 0x00010000;
        public const int CDDS_ITEMPREPAINT = (CDDS_ITEM | CDDS_PREPAINT);
        public const int CDDS_ITEMPOSTPAINT = (CDDS_ITEM | CDDS_POSTPAINT);
        public const int CDRF_DODEFAULT = 0;
        public const int CDRF_NOTIFYITEMDRAW = 0x20;
        public const int CDRF_NOTIFYPOSTPAINT = 0x10;
        public const int OCM__BASE = WM_USER + 0x1C00;
        public const int OCM_NOTIFY = OCM__BASE + WM_NOTIFY;
        public const int SB_LINEUP = 0;
        public const int SB_LINEDOWN = 1;
        public const int WM_CLOSE = 0x0010;
        public const int WM_KEYFIRST = 0x0100;
        public const int WM_SYSCHAR = 0x0106;
        public const int WM_KEYLAST = 0x0109;
        public const int WM_MOUSEFIRST = 0x0200;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_MOUSELAST = 0x020D;
        public const int BITSPIXEL = 12;
        public const int PLANES = 14;
        public const int LOGPIXELSY = 90;
        public const int FW_NORMAL = 400;
        public const int HWND_TOPMOST = -1;
        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOZORDER = 0x0004;
        public const int SWP_NOACTIVATE = 0x0010;
        public const int SW_HIDE = 0;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int TB_SETINDENT = WM_USER + 47;
        public const int MF_BYPOSITION = 0x400;
        public const uint MK_LBUTTON  = 0x0001;
        public const uint MK_RBUTTON  = 0x0002;
        public const uint MK_SHIFT    = 0x0004;
        public const uint MK_CONTROL  = 0x0008;
        public const uint MK_ALT      = 0x0020;
        public const uint MK_MBUTTON  = 0x0010;
        public const int TVGN_DROPHILITE = 8;
        public const int LOCALE_USER_DEFAULT    = 0x00000400;
        public const int LOCALE_IDATE           = 0x00000021;
        public const int LOCALE_ITIME           = 0x00000023;
        public const int LOCALE_IDAYLZERO       = 0x00000026;
        public const int LOCALE_ITIMEMARKPOSN   = 0x00001005;
        public const int TIME_NOSECONDS = 2;
        public const int TTF_IDISHWND     = 0x0001;
        public const int TTF_SUBCLASS     = 0x0010;
        public const int TTF_TRANSPARENT  = 0x0100;
        public const int LPSTR_TEXTCALLBACK = -1;
        public const int TTM_ACTIVATE   = 0x401;
        public const int TTM_UPDATE     = 0x41D;
        public const int TTM_ADJUSTRECT = 0x41F;
        public const int TTM_ADDTOOLW   = 0x432;
        public const int TTS_NOPREFIX   = 0x02;
        public const int TTS_NOFADE     = 0x20;
        public const int APPCOMMAND_BROWSER_BACKWARD = 1;
        public const int APPCOMMAND_BROWSER_FORWARD = 2;
        public const int ILD_NORMAL     = 0x00000;
        public const int ILD_SELECTED   = 0x00004;
        public const int EM_SETMARGINS = 0xD3;
        public const int EC_LEFTMARGIN = 0x1;

        public static Rectangle RECTToRectangle( RECT rect )
        {
            return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }

        public static RECT RectangleToRECT( Rectangle rect )
        {
            return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        [DllImport( "user32.dll", CharSet=CharSet.Auto )]
        public static extern int SendMessage( IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam );

        [DllImport( "user32.dll", CharSet=CharSet.Auto )]
        public static extern int SendMessage( IntPtr hWnd, int msg, IntPtr wParam, StringBuilder lParam );
        [DllImport( "user32.dll", CharSet=CharSet.Auto )]
        public static extern int SendMessage( IntPtr hWnd, int msg, int wParam, ref TOOLINFOCB lParam );

        [DllImport( "user32.dll", CharSet=CharSet.Auto )]
        public static extern IntPtr SendMessage( IntPtr hWnd, LVM msg, int wParam, ref RECT rc );

        [DllImport( "user32.dll", CharSet=CharSet.Auto )]
        public static extern IntPtr SendMessage( IntPtr hWnd, TreeViewMessage msg, int wParam, ref TVITEM lParam );

        [DllImport( "user32.dll", CharSet=CharSet.Auto )]
        public static extern IntPtr SendMessage( IntPtr hWnd, TreeViewMessage msg, int wParam, ref TVHITTESTINFO lParam );

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, EditMessage msg, SCF wParam, ref CHARFORMAT2 fmt);

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, EditMessage msg, ref SETTEXTEX wParam, byte[] lParam );

        [DllImport( "user32.dll", CharSet=CharSet.Auto )]
        public static extern int DrawText( IntPtr hDC, string lpString, int nCount, ref RECT lpRect, DrawTextFormatFlags flags );

        [DllImport( "user32.dll")]
        public static extern IntPtr GetSystemMenu( IntPtr hWnd, bool bRevert );

        [DllImport( "user32.dll")]
        public static extern int GetMenuItemCount( IntPtr hMenu );

        [DllImport("user32.dll")]
        public static extern bool RemoveMenu( IntPtr hMenu, uint uPosition, uint uFlags );

        [DllImport("user32.dll")]
        public static extern bool DrawMenuBar( IntPtr hWnd );

        [DllImport( "gdi32.dll" )]
        public static extern IntPtr SelectObject( IntPtr hDC, IntPtr hGDIObj );

        [DllImport( "gdi32.dll" )]
        public static extern int DeleteObject( IntPtr hGDIObj );

        [DllImport( "gdi32.dll" )]
        public static extern int SetTextColor( IntPtr hdc, int color );

        [DllImport("gdi32.dll")]
        public static extern int SetBkColor( IntPtr hdc, int color );

        [DllImport("gdi32.dll")]
        public static extern int GetBkColor( IntPtr hdc );

        [DllImport("gdi32.dll")]
        public static extern BackgroundMode SetBkMode( IntPtr hdc, BackgroundMode bkMode );

        [DllImport( "gdi32.dll" )]
        public static extern IntPtr CreateFont( int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight, uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet, uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality, uint fdwPitchAndFamily, string lpszFace );

        [DllImport( "gdi32.dll" )]
        public static extern int GetDeviceCaps( IntPtr hdc, int nIndex );

        [DllImport( "gdi32.dll", CharSet=CharSet.Auto )]
        public static extern int TextOut( IntPtr hDC, int nXStart, int nYStart, string lpString, int cbString );

        [DllImport( "gdi32.dll", CharSet=CharSet.Auto )]
        public static extern int GetTextExtentPoint32( IntPtr hDC, string lpString, int cbString, ref SIZE lpSize );

        [DllImport( "gdi32.dll", CharSet=CharSet.Auto )]
        public static extern bool GetTextExtentExPoint( IntPtr hdc, string lpszStr, int cchString, int nMaxExtent, out int lpnFit, IntPtr alpDx, out SIZE lpSize );

        [DllImport( "gdi32.dll") ]
        public static extern int IntersectClipRect( IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect );

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll")]
        public static extern int GetClipRgn( IntPtr hdc, IntPtr hrgn );

        [DllImport( "user32.dll" )]
        public static extern int DrawFocusRect( IntPtr hdc, ref RECT lprc );

        [DllImport( "gdi32.dll" )]
        public static extern IntPtr CreateSolidBrush( int color );

        [DllImport( "gdi32.dll") ]
        public static extern IntPtr CreatePen( int fnPenStyle, int nWidth, int color );

        [DllImport( "gdi32.dll") ]
        public static extern bool Rectangle( IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect );

        [DllImport( "user32.dll" )]
        public static extern int FrameRect( IntPtr hdc, ref RECT lprc, IntPtr hbr );

        [DllImport( "user32.dll" )]
        public static extern int FillRect( IntPtr hdc, ref RECT lprc, IntPtr hbr );

    	[DllImport( "user32.dll", EntryPoint = "GetWindowRect" )]
        public static extern bool GetWindowRect( IntPtr hWnd, ref RECT lpRect );

    	[DllImport( "user32.dll", EntryPoint="GetDC" )]
        public static extern IntPtr GetDC( IntPtr ptr );

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx( IntPtr hwndParent, IntPtr hwndChildAfter,
                                                  [MarshalAs(UnmanagedType.LPTStr)] string lpszClass,
                                                  [MarshalAs(UnmanagedType.LPTStr)] string lpszWindow);


        [DllImport( "user32.dll", EntryPoint="GetWindowDC" )]
        public static extern IntPtr GetWindowDC( IntPtr ptr );

        [DllImport( "user32.dll", EntryPoint="ReleaseDC" )]
        public static extern IntPtr ReleaseDC( IntPtr hWnd, IntPtr hDc );

        [DllImport( "user32.dll" )]
        public static extern uint GetMessagePos();


    	[DllImport( "user32.dll" )]
        public static extern bool SetWindowPos( IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags );

    	[DllImport( "user32.dll" )]
        public static extern bool ShowWindow( IntPtr hWnd, int nCmdShow );

    	[DllImport( "user32.dll") ]
        public static extern IntPtr BeginPaint( IntPtr hWnd, ref PAINTSTRUCT lpPaint );

        [DllImport("user32.dll")]
        public static extern IntPtr EndPaint( IntPtr hWnd, ref PAINTSTRUCT lpPaint );

    	[DllImport( "gdi32.dll", CharSet=CharSet.Auto )]
        public static extern int SelectClipRgn( IntPtr hDC, IntPtr hRgn );

        public  delegate IntPtr WndProcCallBack(IntPtr hwnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, WndProcCallBack wndProcCallBack);

        [DllImport("kernel32.dll")]
        public static extern int GetTimeFormat(uint Locale, uint dwFlags, ref SYSTEMTIME lpTime,
            string lpFormat, [Out] StringBuilder lpTimeStr, int cchTime);

        [DllImport("kernel32.dll")]
        public static extern int GetLocaleInfo( uint Locale, int LCType, out byte lpLCData, int cchData );

        [DllImport( "kernel32.dll" )]
        public static extern UInt32 GetTickCount();


        [DllImport("User32.dll")]
        public static extern bool MessageBeep(int type);

        [StructLayout( LayoutKind.Sequential )]
            public struct ULARGE_INTEGER
        {
            public ulong _value;
        }
        [DllImport("kernel32.dll")]
        public static extern bool GetDiskFreeSpaceEx( string drive,
            ref ULARGE_INTEGER userFreeBytes, ref ULARGE_INTEGER totalBytes, ref ULARGE_INTEGER totalFreeBytes );

        [DllImport("comctl32.dll")]
        public static extern int ImageList_Draw( IntPtr himl, int i, IntPtr hdcDst, int x, int y, int fStyle );


        public static ushort LOWORD( uint l )
        {
            return (ushort)(l & 0xffff);
        }

        public static ushort HIWORD( uint l )
        {
            return (ushort)(l >> 16);
        }

        public static int ColorToRGB( Color color )
        {
            return ((int)(((byte)(color.R) | ((short)((byte)(color.G)) << 8)) | (((short)(byte)(color.B)) << 16)));
        }


        public const int CP_WINANSI = 1004;
        public const int CP_WINUNICODE = 1200;
        public static readonly int CP_WINNEUTRAL = Marshal.SystemDefaultCharSize == 2 ? CP_WINUNICODE : CP_WINANSI;

		/// <summary>
		/// The CreateCompatibleDC function creates a memory device context (DC) compatible with the specified device.
		/// </summary>
		/// <param name="hdc">Handle to an existing DC. If this handle is NULL, the function creates a memory DC compatible with the application's current screen.</param>
		/// <returns>If the function succeeds, the return value is the handle to a memory DC. If the function fails, the return value is NULL.</returns>
		[DllImport( "Gdi32.dll" )]
		public static extern IntPtr CreateCompatibleDC( IntPtr hdc );

		/// <summary>
		/// The CreateCompatibleBitmap function creates a bitmap compatible with the device that is associated with the specified device context.
		/// </summary>
		/// <param name="hdc">handle to DC</param>
		/// <param name="nWidth">width of bitmap, in pixels</param>
		/// <param name="nHeight">height of bitmap, in pixels</param>
		/// <returns>If the function succeeds, the return value is a handle to the compatible bitmap (DDB). If the function fails, the return value is NULL.</returns>
		[DllImport( "Gdi32.dll" )]
		public static extern IntPtr CreateCompatibleBitmap( IntPtr hdc, int nWidth, int nHeight );

		/// <summary>
		/// The BitBlt function performs a bit-block transfer of the color data corresponding to a rectangle of pixels from the specified source device context into a destination device context.
		/// </summary>
		/// <param name="hdcDest">handle to destination DC</param>
		/// <param name="nXDest">x-coord of destination upper-left corner</param>
		/// <param name="nYDest">y-coord of destination upper-left corner</param>
		/// <param name="nWidth">width of destination rectangle</param>
		/// <param name="nHeight">height of destination rectangle</param>
		/// <param name="hdcSrc">handle to source DC</param>
		/// <param name="nXSrc">x-coordinate of source upper-left corner</param>
		/// <param name="nYSrc">y-coordinate of source upper-left corner</param>
		/// <param name="dwRop">raster operation code</param>
		/// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
		[DllImport( "Gdi32.dll" )]
		public static extern int BitBlt( IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, RasterOperations dwRop );

		/// <summary>
		/// Possible raster operations for the <see cref="BitBlt"/> function.
		/// </summary>
		public enum RasterOperations : uint
		{
			/// <summary>dest = source</summary>
			SRCCOPY = 0x00CC0020,
			/// <summary>dest = source OR dest</summary>
			SRCPAINT = 0x00EE0086,
			/// <summary>dest = source AND dest</summary>
			SRCAND = 0x008800C6,
			/// <summary>dest = source XOR dest</summary>
			SRCINVERT = 0x00660046,
			/// <summary>dest = source AND (NOT dest )</summary>
			SRCERASE = 0x00440328,
			/// <summary>dest = (NOT source)</summary>
			NOTSRCCOPY = 0x00330008,
			/// <summary>dest = (NOT src) AND (NOT dest)</summary>
			NOTSRCERASE = 0x001100A6,
			/// <summary>dest = (source AND pattern)</summary>
			MERGECOPY = 0x00C000CA,
			/// <summary>dest = (NOT source) OR dest</summary>
			MERGEPAINT = 0x00BB0226,
			/// <summary>dest = pattern</summary>
			PATCOPY = 0x00F00021,
			/// <summary>dest = DPSnoo</summary>
			PATPAINT = 0x00FB0A09,
			/// <summary>dest = pattern XOR dest</summary>
			PATINVERT = 0x005A0049,
			/// <summary>dest = (NOT dest)</summary>
			DSTINVERT = 0x00550009,
			/// <summary>dest = BLACK</summary>
			BLACKNESS = 0x00000042,
			/// <summary>dest = WHITE</summary>
			WHITENESS = 0x00FF0062,
			/// <summary>Do not Mirror the bitmap in this call</summary>
			NOMIRRORBITMAP = 0x80000000,
			/// <summary>Include layered windows</summary>
			CAPTUREBLT = 0x40000000
		}

    	/// <summary>
    	/// The CreateDC function creates a device context (DC) for a device using the specified name.
    	/// </summary>
    	/// <param name="lpszDriver">driver name</param>
    	/// <param name="lpszDevice">device name</param>
    	/// <param name="lpszOutput">not used; should be NULL</param>
    	/// <param name="lpInitData">optional printer data</param>
    	/// <returns>If the function succeeds, the return value is the handle to a DC for the specified device. If the function fails, the return value is NULL. The function will return NULL for a DEVMODE structure other than the current DEVMODE.</returns>
    	[DllImport( "Gdi32.dll" )]
    	public static extern IntPtr CreateDC( String lpszDriver, String lpszDevice, String lpszOutput, IntPtr lpInitData );

    	/// <summary>
    	/// The DeleteDC function deletes the specified device context (DC).
    	/// </summary>
    	/// <param name="hDC">handle to DC</param>
    	/// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
    	[DllImport( "Gdi32.dll" )]
    	public static extern int DeleteDC( IntPtr hDC );

		/// <summary>
		/// Converts a numeric value into a string that represents the number expressed as a size value in bytes, kilobytes, megabytes, or gigabytes, depending on the size.
		/// </summary>
		/// <param name="nSize">Numeric value to be converted.</param>
		/// <param name="pBuffer">Pointer to a buffer to hold the converted number. Note: this function is bound to call the ANSI version.</param>
		/// <param name="nBufSize">Size of the buffer, in characters. Note: in our case, in bytes.</param>
		/// <returns>Returns the address of the converted string, or <see cref="IntPtr.Zero"/> if the conversion fails.</returns>
		/// <remarks>
		/// The following table illustrates how this function converts a numeric value into a text string.
		///
		/// Numeric value -> Text string
		/// 532 532 -> bytes
		/// 1340 -> 1.30KB
		/// 23506 -> 22.9KB
		/// 2400016 -> 2.29MB
		/// 2400000000 -> 2.23GB
		/// </remarks>
		[DllImport("shlwapi.dll", CharSet=CharSet.Ansi)]
		public static extern IntPtr StrFormatByteSize64A(Int64 nSize, byte[] pBuffer, uint nBufSize);

		/// <summary>
		/// The GetCurrentThreadId function retrieves the thread identifier of the calling thread.
		/// Note: same as <see cref="System.AppDomain.GetCurrentThreadId"/>, but doesn't raise the “Obsolete” warning.
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern int GetCurrentThreadId();
		/// <summary>
		/// Extends the window frame behind the client area.
		/// If Desktop Window Manager (DWM) composition is toggled, this function must be called again. Handle the WM_DWMCOMPOSITIONCHANGED message for composition change notification.
		/// Negative margins are used to create the "sheet of glass" effect where the client area is rendered as a solid surface with no window border.
		/// </summary>
		/// <param name="hwnd">The handle to the window for which the frame is extended into the client area.</param>
		/// <param name="margins"><see cref="MARGINS"/> that describes the margins to use when extending the frame into the client area.</param>
		/// <returns>Returns S_OK if successful, or an error value otherwise.</returns>
		[DllImport("dwmapi.dll", PreserveSig = true)]
		public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, MARGINS margins);

		[DllImport("dwmapi.dll", PreserveSig = false)]
		public static extern bool DwmIsCompositionEnabled();

		/// <summary>
		/// Returned by the GetThemeMargins function to define the margins of windows that have visual styles applied.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public class MARGINS
		{
			/// <summary>
			/// Sets no margin.
			/// </summary>
			public MARGINS()
				: this(0)
			{
			}

			/// <summary>
			/// Sets all margins to the same value.
			/// </summary>
			public MARGINS(int uniformvalue)
			{
				Left = Right = Top = Bottom = uniformvalue;
			}

			/// <summary>
			/// Sets the margins so that they cover the whole surface.
			/// </summary>
			public static MARGINS WholeSurface
			{
				get
				{
					return new MARGINS(-1);
				}
			}
			/// <summary>
			/// Sets the margins so that they cover none of the window inner surface.
			/// </summary>
			public static MARGINS Null
			{
				get
				{
					return new MARGINS(0);
				}
			}

			public int Left;

			public int Right;

			public int Top;

			public int Bottom;
		}
		[DllImport("user32.dll", EntryPoint = "EnumWindows")]
		public static extern bool EnumWindows(EnumWindowsCallback lpfn, int lParam);
		public delegate bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam);
		[DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
		public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
		public const uint SPI_GETCLEARTYPE = 0x1048;

		public const uint SPI_GETFONTSMOOTHING = 0x004A;

		public const uint SPI_SETFONTSMOOTHING = 0x004B;

		public const uint SPI_GETFONTSMOOTHINGTYPE = 0x200A;

		public const uint SPI_SETFONTSMOOTHINGTYPE = 0x200B;
		public const uint FE_FONTSMOOTHINGCLEARTYPE = 0x0002;
	}


    public enum DrawTextFormatFlags
    {
        DT_TOP = 0x00000000,
        DT_LEFT = 0x00000000,
        DT_CENTER = 0x00000001,
        DT_RIGHT = 0x00000002,
        DT_VCENTER = 0x00000004,
        DT_BOTTOM = 0x00000008,
        DT_WORDBREAK = 0x00000010,
        DT_SINGLELINE = 0x00000020,
        DT_EXPANDTABS = 0x00000040,
        DT_TABSTOP = 0x00000080,
        DT_NOCLIP = 0x00000100,
        DT_EXTERNALLEADING = 0x00000200,
        DT_CALCRECT = 0x00000400,
        DT_NOPREFIX = 0x00000800,
        DT_INTERNAL = 0x00001000,
        DT_EDITCONTROL = 0x00002000,
        DT_PATH_ELLIPSIS = 0x00004000,
        DT_END_ELLIPSIS = 0x00008000,
        DT_MODIFYSTRING = 0x00010000,
        DT_RTLREADING = 0x00020000,
        DT_WORD_ELLIPSIS = 0x00040000
    }

    public enum BackgroundMode
    {
        TRANSPARENT = 1,
        OPAQUE = 2
    }

    public enum TreeViewMessage
    {
        TV_FIRST = 0x1100,
        TVM_SETIMAGELIST = (TV_FIRST + 9),
        TVM_GETNEXTITEM = (TV_FIRST + 10),
        TVM_SELECTITEM = (TV_FIRST + 11),
        TVM_GETITEMA = (TV_FIRST + 12),
        TVM_SETITEMA = (TV_FIRST + 13),
        TVM_HITTEST = (TV_FIRST + 17)
    }

    public enum TreeViewImageList
    {
        TVSIL_NORMAL = 0,
        TVSIL_STATE = 2
    }

    public enum LVM
    {
        LVM_FIRST = 0x1000,
        LVM_GETITEMCOUNT = LVM_FIRST + 4,
        LVM_SETITEMA = LVM_FIRST + 6,
        LVM_DELETEITEM = LVM_FIRST + 8,
        LVM_DELETEALLITEMS = LVM_FIRST + 9,
        LVM_SETCALLBACKMASK = LVM_FIRST + 11,
        LVM_GETNEXTITEM = LVM_FIRST + 12,
        LVM_GETITEMRECT = LVM_FIRST + 14,
        LVM_HITTEST = LVM_FIRST + 18,
        LVM_ENSUREVISIBLE = LVM_FIRST + 19,
        LVM_GETEDITCONTROL = LVM_FIRST + 24,
        LVM_GETTOPINDEX = LVM_FIRST + 39,
        LVM_SETITEMSTATE = LVM_FIRST + 43,
        LVM_GETITEMSTATE = LVM_FIRST + 44,
        LVM_SETITEMCOUNT = LVM_FIRST + 47,
        LVM_GETSELECTEDCOUNT = LVM_FIRST + 50,
        LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54,
        LVM_GETSUBITEMRECT = LVM_FIRST + 56,
        LVM_SUBITEMHITTEST = LVM_FIRST + 57,
        LVM_GETCOLUMNORDERARRAY = LVM_FIRST + 59,
        LVM_SETSELECTIONMARK = LVM_FIRST + 67,
        LVM_INSERTITEM = LVM_FIRST + 77,
        LVM_EDITLABEL = LVM_FIRST + 118
    } ;

    public enum ListViewNotification
    {
        LVN_FIRST = -100,
        LVN_BEGINDRAG = LVN_FIRST - 9,
        LVN_BEGINRDRAG = LVN_FIRST - 11,
        LVN_ODSTATECHANGED = LVN_FIRST - 15,
        LVN_KEYDOWN = LVN_FIRST - 55,
        LVN_ENDLABELEDIT = LVN_FIRST - 76,
        LVN_GETDISPINFO = LVN_FIRST - 77
    }


    public enum ComboBoxNotification : uint
    {
        CBN_SELCHANGE = 1,
        CBN_DBLCLK = 2,
        CBN_SETFOCUS = 3,
        CBN_KILLFOCUS = 4,
        CBN_EDITCHANGE = 5,
        CBN_EDITUPDATE = 6,
        CBN_DROPDOWN = 7,
        CBN_CLOSEUP = 8,
        CBN_SELENDOK = 9,
        CBN_SELENDCANCEL = 10
    }

    public enum EditMessage : int
    {
        FIRST				= 0x400,
        GETLIMITTEXT		= FIRST + 37,
        POSFROMCHAR			= FIRST + 38,
        CHARFROMPOS			= FIRST + 39,
        SCROLLCARET			= FIRST + 49,
        CANPASTE			= FIRST + 50,
        DISPLAYBAND			= FIRST + 51,
        EXGETSEL			= FIRST + 52,
        EXLIMITTEXT			= FIRST + 53,
        EXLINEFROMCHAR		= FIRST + 54,
        EXSETSEL			= FIRST + 55,
        FINDTEXT			= FIRST + 56,
        FORMATRANGE			= FIRST + 57,
        GETCHARFORMAT		= FIRST + 58,
        GETEVENTMASK		= FIRST + 59,
        GETOLEINTERFACE		= FIRST + 60,
        GETPARAFORMAT		= FIRST + 61,
        GETSELTEXT			= FIRST + 62,
        HIDESELECTION		= FIRST + 63,
        PASTESPECIAL		= FIRST + 64,
        REQUESTRESIZE		= FIRST + 65,
        SELECTIONTYPE		= FIRST + 66,
        SETBKGNDCOLOR		= FIRST + 67,
        SETCHARFORMAT		= FIRST + 68,
        SETEVENTMASK		= FIRST + 69,
        SETOLECALLBACK		= FIRST + 70,
        SETPARAFORMAT		= FIRST + 71,
        SETTARGETDEVICE		= FIRST + 72,
        STREAMIN			= FIRST + 73,
        STREAMOUT			= FIRST + 74,
        GETTEXTRANGE		= FIRST + 75,
        FINDWORDBREAK		= FIRST + 76,
        SETOPTIONS			= FIRST + 77,
        GETOPTIONS			= FIRST + 78,
        FINDTEXTEX			= FIRST + 79,
        GETWORDBREAKPROCEX	= FIRST + 80,
        SETWORDBREAKPROCEX	= FIRST + 81,
        // RichEdit 2.0 messages
        SETUNDOLIMIT		= FIRST + 82,
        REDO				= FIRST + 84,
        CANREDO				= FIRST + 85,
        GETUNDONAME			= FIRST + 86,
        GETREDONAME			= FIRST + 87,
        STOPGROUPTYPING		= FIRST + 88,
        SETTEXTMODE			= FIRST + 89,
        GETTEXTMODE			= FIRST + 90,
        SETTEXTEX           = FIRST + 97,
    }

    public struct SETTEXTEX
    {
        public uint flags;
        public int codepage;
    }

    public enum SCF : int
    {
        SELECTION		= 0x0001,
        WORD			= 0x0002,
        DEFAULT			= 0x0000,	// Set default charformat or paraformat
        ALL				= 0x0004,	// Not valid with SCF_SELECTION or SCF_WORD
        USEUIRULES		= 0x0008,	// Modifier for SCF_SELECTION; says that
        //  format came from a toolbar, etc., and
        //  hence UI formatting rules should be
        //  used instead of literal formatting
        ASSOCIATEFONT	= 0x0010,	// Associate fontname with bCharSet (one
        //  possible for each of Western, ME, FE,
        //  Thai)
        NOKBUPDATE		= 0x0020,	// Do not update KB layput for this change
        //  even if autokeyboard is on
        ASSOCIATEFONT2	= 0x0040,	// Associate plane-2 (surrogate) font
    }

    /// <summary>
    /// Represents the Win32 NMTVCUSTOMDRAW structure
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    internal struct NMTVCUSTOMDRAW
    {
        /// <summary>
        /// <see cref="NMCUSTOMDRAW"/> structure that contains general custom draw information.
        /// </summary>
        public NMCUSTOMDRAW nmcd;

        public int clrText;
        public int clrTextBk;

        /// <summary>
        /// Zero-based level of the item being drawn. The root item is at level zero, a child of the root item is at level one, and so on.
        /// </summary>
        public int iLevel;
    }

    /// <summary>
    /// Represents the Win32 NMCUSTOMDRAW structure
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct NMCUSTOMDRAW
    {
        /// <summary>
        /// <see cref="NMHDR"/> structure that contains information about this notification message.
        /// </summary>
        public NMHDR hdr;

        /// <summary>
        /// Current drawing stage.
        /// </summary>
        public int dwDrawStage;

        /// <summary>
        /// Handle to the control's device context. Use this HDC to perform any GDI functions.
        /// </summary>
        public IntPtr hdc;

        /// <summary>
        /// RECT structure that describes the bounding rectangle of the area being drawn. This member is initialized only by the CDDS_ITEMPREPAINT notification.
        /// </summary>
        public RECT rc;

        /// <summary>
        /// Item number. What is contained in this member will depend on the type of control that is sending the notification.
        /// </summary>
        public int dwItemSpec;

        /// <summary>
        /// Current item state.
        /// </summary>
        public int uItemState;

        /// <summary>
        /// Application-defined item data.
        /// </summary>
        public IntPtr lItemParam;
    }

    /// <summary>
    /// Represents the Win32 NMHDR structure
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct NMHDR
    {
        /// <summary>
        /// Window handle to the control sending a message.
        /// </summary>
        public IntPtr hwndFrom;

        /// <summary>
        /// Identifier of the control sending a message
        /// </summary>
        public int idFrom;

        /// <summary>
        /// Notification code. This member can be a control-specific notification code or it can be one of the common notification codes.
        /// </summary>
        public int code;
    }

    /// <summary>
    /// Represents the Win32 NMHDR structure
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct OBJECTPOSITIONS
    {
        public NMHDR nmhdr;
        public int cObjectCount;
        public IntPtr pcpPositions;
    }
    [StructLayout( LayoutKind.Sequential )]
    public struct NMKEY
    {
        public NMHDR hdr;
        public uint nVKey;
        public uint uFlags;
    }

    [StructLayout( LayoutKind.Sequential )]
    public struct NMHEADER
    {
        public NMHDR hdr;
        public int iItem;
        public int iButton;
        public IntPtr pitem;
    }

    [StructLayout( LayoutKind.Sequential, CharSet=CharSet.Auto )]
    public struct NMTTDISPINFOW
    {
        public NMHDR hdr;

        public IntPtr lpszText;

        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 80 )] public string szText;
        public IntPtr hinst;
        public int uFlags;
        public IntPtr lParam;
    }

	[StructLayout( LayoutKind.Sequential )]
    public struct SIZE
    {
        public SIZE( Int32 CX, Int32 CY )
        {
            cx = CX;
            cy = CY;
        }

        public Int32 cx;
        public Int32 cy;
    }

	[StructLayout( LayoutKind.Sequential, CharSet=CharSet.Auto )]
    public struct TVITEM
    {
        public TreeViewItemFlags mask;
        public IntPtr hItem;
        public int state;
        public int stateMask;
        public IntPtr pszText;
        public int cchTextMax;
        public int iImage;
        public int iSelectedImage;
        public int cChildren;
        public int lParam;
    }

    [StructLayout( LayoutKind.Sequential )]
    public struct TVHITTESTINFO
    {
        public POINT pt;
        public TreeViewHitTestFlags flags;
        public IntPtr hItem;
    }

    public enum TreeViewItemFlags
    {
        TEXT = 0x0001,
        IMAGE = 0x0002,
        PARAM = 0x0004,
        STATE = 0x0008,
        HANDLE = 0x0010,
        SELECTEDIMAGE = 0x0020,
        CHILDREN = 0x0040
    }

    public enum TreeViewHitTestFlags
    {
        NOWHERE = 0x0001,
        ONITEMICON = 0x0002,
        ONITEMLABEL = 0x0004,
        ONITEM = (ONITEMICON | ONITEMLABEL | ONITEMSTATEICON),
        ONITEMINDENT = 0x0008,
        ONITEMBUTTON = 0x0010,
        ONITEMRIGHT = 0x0020,
        ONITEMSTATEICON = 0x0040,

        ABOVE = 0x0100,
        BELOW = 0x0200,
        TORIGHT = 0x0400,
        TOLEFT = 0x0800
    }

    public enum TreeViewItemState
    {
        SELECTED = 0x0002,
        CUT = 0x0004,
        DROPHILITED = 0x0008,
        BOLD = 0x0010,
        EXPANDED = 0x0020,
        EXPANDEDONCE = 0x0040
    }

    public enum ListViewItemFlags : int
    {
        TEXT = 0x0001,
        IMAGE = 0x0002,
        PARAM = 0x0004,
        STATE = 0x0008,
        INDENT = 0x0010,
        NORECOMPUTE = 0x0800,
        GROUPID = 0x0100,
        COLUMNS = 0x0200,
    }

    public enum ListViewItemStates : int
    {
        None = 0,
        FOCUSED = 0x0001,
        SELECTED = 0x0002,
        CUT = 0x0004,
        DROPHILITED = 0x0008,
        GLOW = 0x0010,
        ACTIVATING = 0x0020,
        OVERLAYMASK = 0x0F00,
        STATEIMAGEMASK = 0xF000,
    }

    public enum ListViewHitTestFlags
    {
        NOWHERE = 0x0001,
        ONITEMICON = 0x0002,
        ONITEMLABEL = 0x0004,
        ONITEMSTATEICON = 0x0008,
        ONITEM = ONITEMICON | ONITEMLABEL | ONITEMSTATEICON,
        ABOVE = 0x0008,
        BELOW = 0x0010,
        TORIGHT = 0x0020,
        TOLEFT = 0x0040,
    }

    public enum HitTestResult
    {
        HTERROR = -2,
        HTTRANSPARENT = -1,
        HTNOWHERE = 0,
        HTCLIENT = 1,
        HTCAPTION = 2,
        HTSYSMENU = 3,
        HTGROWBOX = 4,
        HTSIZE = HTGROWBOX,
        HTMENU = 5,
        HTHSCROLL = 6,
        HTVSCROLL = 7,
        HTMINBUTTON = 8,
        HTMAXBUTTON = 9,
        HTLEFT = 10,
        HTRIGHT = 11,
        HTTOP = 12,
        HTTOPLEFT = 13,
        HTTOPRIGHT = 14,
        HTBOTTOM = 15,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17,
        HTBORDER = 18,
        HTREDUCE = HTMINBUTTON,
        HTZOOM = HTMAXBUTTON,
        HTSIZEFIRST = HTLEFT,
        HTSIZELAST = HTBOTTOMRIGHT,
        HTOBJECT = 19,
        HTCLOSE = 20,
        HTHELP = 21
    }

	[StructLayout( LayoutKind.Sequential )]
    public struct LVITEM_NOTEXT
    {
        public ListViewItemFlags mask;
        public Int32 iItem;
        public Int32 iSubItem;
        public ListViewItemStates state;
        public ListViewItemStates stateMask;
        public IntPtr pszText;
        public Int32 cchTextMax;
        public Int32 iImage;
        public IntPtr lParam;
        public Int32 iIndent;
    }

    [StructLayout( LayoutKind.Sequential )]
    public struct LVITEM
    {
        public ListViewItemFlags mask;
        public Int32 iItem;
        public Int32 iSubItem;
        public ListViewItemStates state;
        public ListViewItemStates stateMask;

        [MarshalAs( UnmanagedType.LPWStr )] public string pszText;
        public Int32 cchTextMax;
        public Int32 iImage;
        public IntPtr lParam;
        public Int32 iIndent;
    }

    [StructLayout( LayoutKind.Sequential )]
    public struct LVHITTESTINFO
    {
        public POINT pt;
        public ListViewHitTestFlags flags;
        public Int32 iItem;
        public Int32 iSubItem;
    }

    /// <summary>
    /// Represents the Win32 NMLVCUSTOMDRAW structure
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct NMLVCUSTOMDRAW
    {
        /// <summary>
        /// <see cref="NMCUSTOMDRAW"/> structure that contains general custom draw information.
        /// </summary>
        public NMCUSTOMDRAW nmcd;

        public int clrText;
        public int clrTextBk;

        /// <summary>
        /// One-based number of subItem drawn. Zero for main item
        /// </summary>
        public int iSubItem;

        //For the IE 6.0+
        public uint dwItemType;
        // Item Custom Draw
        public int clrFace;
        int iIconEffect;
        int iIconPhase;
        int iPartId;
        int iStateId;
        // Group Custom Draw
        RECT rcText;
        uint uAlign;
    }

    [StructLayout( LayoutKind.Sequential )]
    public struct NMLVDISPINFO_NOTEXT
    {
        public NMHDR hdr;
        public LVITEM_NOTEXT item;
    }

    [StructLayout( LayoutKind.Sequential )]
    public struct NMLVDISPINFO
    {
        public NMHDR hdr;
        public LVITEM item;
    }

    [StructLayout( LayoutKind.Sequential )]
    public struct NMLISTVIEW
    {
        public NMHDR hdr;
        public int iItem;
        public int iSubItem;
        public ListViewItemStates uNewState;
        public ListViewItemStates uOldState;
        public ListViewItemFlags uChanged;
        public POINT ptAction;
        public int lParam;
    }

    [StructLayout( LayoutKind.Sequential )]
    public struct NMLVKEYDOWN
    {
        public NMHDR hdr;
        public ushort wVKey;
        public uint uFlags;
    }

    [StructLayout( LayoutKind.Sequential )]
    public struct NMLVODSTATECHANGE
    {
        public NMHDR hdr;
        public int iFrom;
        public int iTo;
        public uint uNewState;
        public uint uOldState;
    }

    [StructLayout( LayoutKind.Sequential )]
    public struct NMTBCUSTOMDRAW
    {
        public NMCUSTOMDRAW hdr;
        public IntPtr hbrMonoDither;
        public IntPtr hbrLines;
        public IntPtr hpenLines;
        public int clrText;
        public int clrMark;
        public int clrTextHighlight;
        public int clrBtnFace;
        public int clrBtnHighlight;
        public int clrHighlightHotTrack;
        public RECT rcText;
        public int nStringBkMode;
        public int nHLStringBkMode;
    }


	/// <summary>
    /// Represents the Win32 MOUSEHOOKSTRUCT structure
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct MOUSEHOOKSTRUCT
    {
        public POINT pt;
        public IntPtr hwnd;
        public uint wHitTestCode;
        public IntPtr dwExtraInfo;
    } ;

    /// <summary>
    /// Represents the Win32 MOUSEHOOKSTRUCTEX structure
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct MOUSEHOOKSTRUCTEX
    {
        public MOUSEHOOKSTRUCT MOUSEHOOKSTRUCT;
        public uint mouseData;
    }

    [StructLayout( LayoutKind.Sequential) ]
    public struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs( UnmanagedType.ByValArray, SizeConst=32)]
        public byte[] rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    public struct SCROLLBARINFO
    {
        public uint  cbSize;
        public RECT  rcScrollBar;
        public int   dxyLineButton;
        public int   xyThumbTop;
        public int   xyThumbBottom;
        public int   reserved;
        [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst=6)]
        public uint[] rgstate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LOGBRUSH
    {
        public uint lbStyle;
        public uint lbColor;
        public uint lbHatch;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct SYSTEMTIME
    {
        [MarshalAs(UnmanagedType.U2)] public short Year;
        [MarshalAs(UnmanagedType.U2)] public short Month;
        [MarshalAs(UnmanagedType.U2)] public short DayOfWeek;
        [MarshalAs(UnmanagedType.U2)] public short Day;
        [MarshalAs(UnmanagedType.U2)] public short Hour;
        [MarshalAs(UnmanagedType.U2)] public short Minute;
        [MarshalAs(UnmanagedType.U2)] public short Second;
        [MarshalAs(UnmanagedType.U2)] public short Milliseconds;
    }

    public enum CFM : uint
    {
        BOLD			= 0x00000001,
        ITALIC			= 0x00000002,
        UNDERLINE		= 0x00000004,
        STRIKEOUT		= 0x00000008,
        PROTECTED		= 0x00000010,
        LINK			= 0x00000020,		// Exchange hyperlink extension
        SIZE			= 0x80000000,
        COLOR			= 0x40000000,
        FACE			= 0x20000000,
        OFFSET			= 0x10000000,
        CHARSET			= 0x08000000,

        // CHARFORMAT effects
        //#define CFE_BOLD		0x0001
        //#define CFE_ITALIC	0x0002
        //#define CFE_UNDERLINE	0x0004
        //#define CFE_STRIKEOUT	0x0008
        //#define CFE_PROTECTED	0x0010
        //#define CFE_LINK		0x0020
        //#define CFE_AUTOCOLOR	0x40000000	// NOTE: this corresponds to
        // CFM_COLOR, which controls it
        // Masks and effects defined for CHARFORMAT2 -- an (*) indicates
        // that the data is stored by RichEdit 2.0/3.0, but not displayed

        SMALLCAPS		= 0x0040,			// (*)
        ALLCAPS			= 0x0080,			// Displayed by 3.0
        HIDDEN			= 0x0100,			// Hidden by 3.0
        OUTLINE			= 0x0200,			// (*)
        SHADOW			= 0x0400,			// (*)
        EMBOSS			= 0x0800,			// (*)
        IMPRINT			= 0x1000,			// (*)
        DISABLED		= 0x2000,
        REVISED			= 0x4000,
        //
        BACKCOLOR		= 0x04000000,
        LCID			= 0x02000000,
        UNDERLINETYPE	= 0x00800000,		// Many displayed by 3.0
        WEIGHT			= 0x00400000,
        SPACING			= 0x00200000,  		// Displayed by 3.0
        KERNING			= 0x00100000,  		// (*)
        STYLE			= 0x00080000,  		// (*)
        ANIMATION		= 0x00040000,  		// (*)
        REVAUTHOR		= 0x00008000,

        CFE_SUBSCRIPT		= 0x00010000,	// Superscript and subscript are
        CFE_SUPERSCRIPT		= 0x00020000,	//  mutually exclusive

        SUBSCRIPT		= CFE_SUBSCRIPT | CFE_SUPERSCRIPT,
        SUPERSCRIPT		= SUBSCRIPT,
        //
        //	CHARFORMAT "ALL" masks
        EFFECTS  = (BOLD | ITALIC | UNDERLINE | COLOR | STRIKEOUT | /* CFE_*/ PROTECTED | LINK),
        ALL      = (EFFECTS | SIZE | FACE | OFFSET | CHARSET),
        EFFECTS2 = (EFFECTS | DISABLED | SMALLCAPS | ALLCAPS | HIDDEN  | OUTLINE | SHADOW | EMBOSS | IMPRINT | DISABLED | REVISED | SUBSCRIPT | SUPERSCRIPT | BACKCOLOR),
        ALL2     = (ALL | EFFECTS2 | BACKCOLOR | LCID | UNDERLINETYPE | WEIGHT | REVAUTHOR | SPACING | KERNING | STYLE | ANIMATION),
    }

    [StructLayout(LayoutKind.Sequential, Pack=8, CharSet=CharSet.Auto)]
    public struct CHARFORMAT2
    {
        private const int LF_FACESIZE = 32;       // Max size of a font name

        public int cbSize;
        public CFM dwMask;
        public UInt32 dwEffects;
        public UInt32 yHeight;
        public UInt32 yOffset;
        public int crTextColor;
        public Byte   bCharSet;
        public Byte   bPitchAndFamily;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=LF_FACESIZE)]
        public String szFaceName;
        public UInt16 wWeight;
        public UInt16 sSpacing;
        public int crBackColor;
        public UInt32 lcid;
        public UInt32 dwReserved;
        public UInt16 sStyle;
        public UInt16 wKerning;
        public Byte bUnderlineType;
        public Byte bAnimation;
        public Byte bRevAuthor;
        public Byte bReserved1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOOLINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hwnd;
        public IntPtr uId;
        public RECT rect;
        public IntPtr hinst;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string text;

        public IntPtr lparam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOOLINFOCB
    {
        public int cbSize;
        public int flags;
        public IntPtr hwnd;
        public IntPtr uId;
        public RECT rect;
        public IntPtr hinst;

        public int textCallback;

        public IntPtr lparam;
    }
}
