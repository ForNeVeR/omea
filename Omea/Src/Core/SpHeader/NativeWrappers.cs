// SPDX-FileCopyrightText: 2004 by Sergei Pavlovsky (sergei_vp@hotmail.com, sergei_vp@ukr.net)
//
// SPDX-License-Identifier: LicenseRef-SP.Windows

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SP.Windows
{
	/// <summary>
	/// InitCommonControlsHelper class
	/// </summary>
	internal class InitCommonControlsHelper
	{
		/// <summary>
		/// Constants: Platform
		/// </summary>
		const int ICC_LISTVIEW_CLASSES   = 0x00000001;
		const int ICC_TREEVIEW_CLASSES   = 0x00000002;
		const int ICC_BAR_CLASSES        = 0x00000004;
		const int ICC_TAB_CLASSES        = 0x00000008;
		const int ICC_UPDOWN_CLASS       = 0x00000010;
		const int ICC_PROGRESS_CLASS     = 0x00000020;
		const int ICC_HOTKEY_CLASS       = 0x00000040;
		const int ICC_ANIMATE_CLASS      = 0x00000080;
		const int ICC_WIN95_CLASSES      = 0x000000FF;
		const int ICC_DATE_CLASSES       = 0x00000100;
		const int ICC_USEREX_CLASSES     = 0x00000200;
		const int ICC_COOL_CLASSES       = 0x00000400;
		// IE 4.0
		const int ICC_INTERNET_CLASSES   = 0x00000800;
		const int ICC_PAGESCROLLER_CLASS = 0x00001000;
		const int ICC_NATIVEFNTCTL_CLASS = 0x00002000;
		// WIN XP
		const int ICC_STANDARD_CLASSES   = 0x00004000;
		const int ICC_LINK_CLASS         = 0x00008000;

		/// <summary>
		/// Types
		/// </summary>
		[Flags]
			public enum Classes : int
		{
			ListView        = ICC_LISTVIEW_CLASSES,
			TreeView        = ICC_TREEVIEW_CLASSES,
			Header          = ICC_LISTVIEW_CLASSES,
			ToolBar         = ICC_BAR_CLASSES,
			StatusBar       = ICC_BAR_CLASSES,
			TrackBar        = ICC_BAR_CLASSES,
			ToolTips        = ICC_BAR_CLASSES,
			TabControl      = ICC_TAB_CLASSES,
			UpDown          = ICC_UPDOWN_CLASS,
			Progress        = ICC_PROGRESS_CLASS,
			HotKey          = ICC_HOTKEY_CLASS,
			Animate         = ICC_ANIMATE_CLASS,
			Win95           = ICC_WIN95_CLASSES,
			DateTimePicker  = ICC_DATE_CLASSES,
			ComboBoxEx      = ICC_USEREX_CLASSES,
			Rebar           = ICC_COOL_CLASSES,
			Internet        = ICC_INTERNET_CLASSES,
			PageScroller    = ICC_PAGESCROLLER_CLASS,
			NativeFont      = ICC_NATIVEFNTCTL_CLASS,
			Standard        = ICC_STANDARD_CLASSES,
			Link            = ICC_LINK_CLASS
		};

		/// <summary>
		/// Types: Platform
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
			private struct INITCOMMONCONTROLSEX
		{
			public int cbSize;
			public int nFlags;

			public INITCOMMONCONTROLSEX(int cbSize, int nFlags)
			{
				this.cbSize = cbSize;
				this.nFlags = nFlags;
			}
		}

		[DllImport("comctl32.dll")]
		private static extern bool InitCommonControlsEx(ref INITCOMMONCONTROLSEX icc);

		/// <summary>
		/// Operations
		/// </summary>

		/// <summary>
		/// <code>void Init(Classes fClasses)</code>
		/// <para>Initializes common controls.</para>
		/// </summary>
		/// <param name="fClasses"> Bit flags defining classes to be initialized</param>
		static public void Init(Classes fClasses)
		{
			INITCOMMONCONTROLSEX icc =
				new INITCOMMONCONTROLSEX(Marshal.SizeOf(typeof(INITCOMMONCONTROLSEX)),
										 (int)fClasses);

			bool bResult = InitCommonControlsEx(ref icc);
			Debug.Assert( bResult );
			if ( !bResult )
			{
				throw new SystemException("Failture initializing common controls.");
			}
		}

	} // InitCommonControlsHelper class


	/// <summary>
	/// NativeWindowCommon class
	/// </summary>
	internal class NativeWindowCommon
	{
		/// <summary>
		/// Constants: Window Styles
		/// </summary>
		public const int WS_OVERLAPPED       = 0x00000000;

		public const int WS_POPUP            = unchecked((int)0x80000000);
		public const int WS_CHILD            = 0x40000000;
		public const int WS_MINIMIZE         = 0x20000000;
		public const int WS_VISIBLE          = 0x10000000;
		public const int WS_DISABLED         = 0x08000000;
		public const int WS_CLIPSIBLINGS     = 0x04000000;
		public const int WS_CLIPCHILDREN     = 0x02000000;
		public const int WS_MAXIMIZE         = 0x01000000;
		public const int WS_CAPTION          = 0x00C00000;  // WS_BORDER|WS_DLGFRAME
		public const int WS_BORDER           = 0x00800000;
		public const int WS_DLGFRAME         = 0x00400000;
		public const int WS_VSCROLL          = 0x00200000;
		public const int WS_HSCROLL          = 0x00100000;
		public const int WS_SYSMENU          = 0x00080000;
		public const int WS_THICKFRAME       = 0x00040000;
		public const int WS_GROUP            = 0x00020000;
		public const int WS_TABSTOP          = 0x00010000;
		public const int WS_MINIMIZEBOX      = 0x00020000;
		public const int WS_MAXIMIZEBOX      = 0x00010000;

		public const int WS_TILED            = WS_OVERLAPPED;
		public const int WS_ICONIC           = WS_MINIMIZE;
		public const int WS_SIZEBOX          = WS_THICKFRAME;
		public const int WS_TILEDWINDOW      = WS_OVERLAPPEDWINDOW;

		public const int WS_OVERLAPPEDWINDOW = WS_OVERLAPPED|WS_CAPTION|
											   WS_SYSMENU|WS_THICKFRAME|
											   WS_MINIMIZEBOX|WS_MAXIMIZEBOX;

		public const int WS_POPUPWINDOW      = WS_POPUP|WS_BORDER|WS_SYSMENU;
		public const int WS_CHILDWINDOW      = WS_CHILD;

		/// <summary>
		/// Constants: Extended Window Styles
		/// </summary>
		public const int WS_EX_DLGMODALFRAME     = 0x00000001;
		public const int WS_EX_NOPARENTNOTIFY    = 0x00000004;
		public const int WS_EX_TOPMOST           = 0x00000008;
		public const int WS_EX_ACCEPTFILES       = 0x00000010;
		public const int WS_EX_TRANSPARENT       = 0x00000020;

		public const int WS_EX_MDICHILD          = 0x00000040;
		public const int WS_EX_TOOLWINDOW        = 0x00000080;
		public const int WS_EX_WINDOWEDGE        = 0x00000100;
		public const int WS_EX_CLIENTEDGE        = 0x00000200;
		public const int WS_EX_CONTEXTHELP       = 0x00000400;

		public const int WS_EX_RIGHT             = 0x00001000;
		public const int WS_EX_LEFT              = 0x00000000;
		public const int WS_EX_RTLREADING        = 0x00002000;
		public const int WS_EX_LTRREADING        = 0x00000000;
		public const int WS_EX_LEFTSCROLLBAR     = 0x00004000;
		public const int WS_EX_RIGHTSCROLLBAR    = 0x00000000;

		public const int WS_EX_CONTROLPARENT     = 0x00010000;
		public const int WS_EX_STATICEDGE        = 0x00020000;
		public const int WS_EX_APPWINDOW         = 0x00040000;

		public const int WS_EX_OVERLAPPEDWINDOW  = WS_EX_WINDOWEDGE|WS_EX_CLIENTEDGE;
		public const int WS_EX_PALETTEWINDOW     = WS_EX_WINDOWEDGE|WS_EX_TOOLWINDOW|
			WS_EX_TOPMOST;

		public const int WS_EX_LAYERED           = 0x00080000;
		public const int WS_EX_NOINHERITLAYOUT   = 0x00100000;
		public const int WS_EX_LAYOUTRTL         = 0x00400000;

		public const int WS_EX_COMPOSITED        = 0x02000000;
		public const int WS_EX_NOACTIVATE        = 0x08000000;

		// Common control shared messages
		public const int CCM_FIRST               = 0x00002000;
		public const int CCM_LAST                = CCM_FIRST + 0x200;
		public const int CCM_SETBKCOLOR          = CCM_FIRST + 1;
		public const int CCM_SETCOLORSCHEME      = CCM_FIRST + 2;
		public const int CCM_GETCOLORSCHEME      = CCM_FIRST + 3;
		public const int CCM_GETDROPTARGET       = CCM_FIRST + 4;
		public const int CCM_SETUNICODEFORMAT    = CCM_FIRST + 5;
		public const int CCM_GETUNICODEFORMAT    = CCM_FIRST + 6;


		// Common messages
		public const int WM_SETREDRAW           = 0x000B;
		public const int WM_CANCELMODE          = 0x001F;

		public const int WM_KEYDOWN             = 0x100;
		public const int WM_KEYUP               = 0x101;
		public const int WM_CHAR                = 0x0102;
		public const int WM_SYSKEYDOWN          = 0x104;
		public const int WM_SYSKEYUP            = 0x105;

		public const int WM_MOUSELAST           = 0x20a;
		public const int WM_MOUSEMOVE           = 0x200;
		public const int WM_LBUTTONDOWN         = 0x201;

		public const int WM_MENUCHAR            = 0x120;

		public const int WM_NCHITTEST           = 0x0084;

		public const int WM_SETCURSOR           = 0x0020;

		public const int WM_NOTIFY              = 0x4e;
		public const int WM_COMMAND             = 0x111;

		public const int WM_USER                = 0x0400;
		public const int OCM__BASE              = WM_USER + 0x1c00;


		public const int HTERROR			= -2;
		public const int HTTRANSPARENT		= -1;
		public const int HTNOWHERE			= 0;
		public const int HTCLIENT			= 1;
		public const int HTCAPTION			= 2;
		public const int HTSYSMENU			= 3;
		public const int HTGROWBOX			= 4;
		public const int HTSIZE				= HTGROWBOX;
		public const int HTMENU				= 5;
		public const int HTHSCROLL			= 6;
		public const int HTVSCROLL			= 7;
		public const int HTMINBUTTON		= 8;
		public const int HTMAXBUTTON		= 9;
		public const int HTLEFT				= 10;
		public const int HTRIGHT			= 11;
		public const int HTTOP				= 12;
		public const int HTTOPLEFT			= 13;
		public const int HTTOPRIGHT			= 14;
		public const int HTBOTTOM			= 15;
		public const int HTBOTTOMLEFT		= 16;
		public const int HTBOTTOMRIGHT		= 17;
		public const int HTBORDER			= 18;
		public const int HTREDUCE			= HTMINBUTTON;
		public const int HTZOOM				= HTMAXBUTTON;
		public const int HTSIZEFIRST		= HTLEFT;
		public const int HTSIZELAST			= HTBOTTOMRIGHT;
		public const int HTOBJECT			= 19;
		public const int HTCLOSE			= 20;
		public const int HTHELP				= 21;

		/// <summary>
		/// Constants for SetWindowPos
		/// </summary>
		public const int SWP_NOSIZE         = 0x0001;
		public const int SWP_NOMOVE         = 0x0002;
		public const int SWP_NOZORDER       = 0x0004;
		public const int SWP_NOREDRAW       = 0x0008;
		public const int SWP_NOACTIVATE     = 0x0010;
		public const int SWP_FRAMECHANGED   = 0x0020;
		public const int SWP_SHOWWINDOW     = 0x0040;
		public const int SWP_HIDEWINDOW     = 0x0080;
		public const int SWP_NOCOPYBITS     = 0x0100;
		public const int SWP_NOOWNERZORDER  = 0x0200;
		public const int SWP_NOSENDCHANGING = 0x0400;

		public const int SWP_DRAWFRAME      = SWP_FRAMECHANGED;
		public const int SWP_NOREPOSITION   = SWP_NOOWNERZORDER;
		public const int SWP_DEFERERASE     = 0x2000;
		public const int SWP_ASYNCWINDOWPOS = 0x4000;

		public static readonly IntPtr HWND_TOP;
		public static readonly IntPtr HWND_BOTTOM;
		public static readonly IntPtr HWND_TOPMOST;
		public static readonly IntPtr HWND_NOTOPMOST;

		/// <summary>
		/// Constants for GetWindowLong
		/// </summary>
		public const int GWL_WNDPROC          = -4;
		public const int GWL_HINSTANCE        = -6;
		public const int GWL_HWNDPARENT       = -8;
		public const int GWL_STYLE            = -16;
		public const int GWL_EXSTYLE          = -20;
		public const int GWL_USERDATA         = -21;
		public const int GWL_ID               = -12;

		/// <summary>
		/// Types
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct WINDOWPOS
		{
			public IntPtr hwnd;
			public IntPtr hwndInsertAfter;
			public int    x;
			public int    y;
			public int    cx;
			public int    cy;
			public int    flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NMHDR
		{
			public IntPtr hwndFrom;
			public int    idFrom;
			public int    code;
		}

		/// <summary>
		/// Static constuctor
		/// </summary>
		static NativeWindowCommon()
		{
			HWND_TOP = (IntPtr)0;
			HWND_BOTTOM = (IntPtr)1;
			HWND_TOPMOST = (IntPtr)(-1);
			HWND_NOTOPMOST = (IntPtr)(-2);
		}

		/// <summary>
		/// Helpers
		/// </summary>
		protected static bool IsSysCharSetAnsi()
		{
			return Marshal.SystemDefaultCharSize == 1;
		}

		/// <summary>
		/// Operations
		/// </summary>
		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern bool PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
			int X, int Y, int cx, int cy, int uFlags);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("gdi32.dll", CharSet=CharSet.Auto)]
		public static extern bool DeleteObject(IntPtr hObject);

	} // NativeWindowCommon


	/// <summary>
	/// NativeHeader class
	/// </summary>
	internal sealed class NativeHeader : NativeWindowCommon
	{
		/// <summary>
		/// Constants: Window class name
		/// </summary>
		public const string WC_HEADER = "SysHeader32";

		/// <summary>
		/// Constants: Control styles
		/// </summary>
		public const int HDS_HORZ       = 0x00000000;
		public const int HDS_BUTTONS    = 0x00000002;
		public const int HDS_HOTTRACK   = 0x00000004;
		public const int HDS_HIDDEN     = 0x00000008;
		public const int HDS_DRAGDROP   = 0x00000040;
		public const int HDS_FULLDRAG   = 0x00000080;
		public const int HDS_FILTERBAR  = 0x00000100;
		public const int HDS_FLAT       = 0x00000200;

		/// <summary>
		/// Constants: Control specific messages
		/// </summary>
		public const int HDM_FIRST                  = 0x00001200;
		public const int HDM_GETITEMCOUNT           = HDM_FIRST + 0;
		public static readonly int HDM_INSERTITEM;
		public const int HDM_DELETEITEM             = HDM_FIRST + 2;
		public static readonly int HDM_GETITEM;
		public static readonly int HDM_SETITEM;
		public const int HDM_LAYOUT                 = HDM_FIRST + 5;
		public const int HDM_HITTEST                = HDM_FIRST + 6;
		public const int HDM_GETITEMRECT            = HDM_FIRST + 7;
		public const int HDM_SETIMAGELIST           = HDM_FIRST + 8;
		public const int HDM_GETIMAGELIST           = HDM_FIRST + 9;
		public const int HDM_ORDERTOINDEX           = HDM_FIRST + 15;
		public const int HDM_CREATEDRAGIMAGE        = HDM_FIRST + 16;
		public const int HDM_GETORDERARRAY          = HDM_FIRST + 17;
		public const int HDM_SETORDERARRAY          = HDM_FIRST + 18;
		public const int HDM_SETHOTDIVIDER          = HDM_FIRST + 19;
		public const int HDM_SETBITMAPMARGIN        = HDM_FIRST + 20;
		public const int HDM_GETBITMAPMARGIN        = HDM_FIRST + 21;
		public const int HDM_SETUNICODEFORMAT       = CCM_SETUNICODEFORMAT;
		public const int HDM_GETUNICODEFORMAT       = CCM_GETUNICODEFORMAT;
		public const int HDM_SETFILTERCHANGETIMEOUT = HDM_FIRST + 22;
		public const int HDM_EDITFILTER             = HDM_FIRST + 23;
		public const int HDM_CLEARFILTER            = HDM_FIRST + 24;

		/// <summary>
		/// Constants: Control specific notifications
		/// </summary>
		public const int HDN_FIRST            = 0 - 300;
		public const int HDN_LAST             = 0 - 399;

		public static readonly int HDN_ITEMCHANGING;
		public static readonly int HDN_ITEMCHANGED;
		public static readonly int HDN_ITEMCLICK;
		public static readonly int HDN_ITEMDBLCLICK;
		public static readonly int HDN_DIVIDERDBLCLICK;
		public static readonly int HDN_BEGINTRACK;
		public static readonly int HDN_ENDTRACK;
		public static readonly int HDN_TRACK;
		public static readonly int HDN_GETDISPINFO;

		public const int HDN_BEGINDRAG        = HDN_FIRST - 10;
		public const int HDN_ENDDRAG          = HDN_FIRST - 11;
		public const int HDN_FILTERCHANGE     = HDN_FIRST - 12;
		public const int HDN_FILTERBTNCLICK   = HDN_FIRST - 13;

        public const int NM_CUSTOMDRAW = -12;

		/// <summary>
		/// Constants: HDITEM mask
		/// </summary>
		public const int HDI_WIDTH            = 0x00000001;
		public const int HDI_HEIGHT           = HDI_WIDTH;
		public const int HDI_TEXT             = 0x00000002;
		public const int HDI_FORMAT           = 0x00000004;
		public const int HDI_LPARAM           = 0x00000008;
		public const int HDI_BITMAP           = 0x00000010;
		public const int HDI_IMAGE            = 0x00000020;
		public const int HDI_DI_SETITEM       = 0x00000040;
		public const int HDI_ORDER            = 0x00000080;
		public const int HDI_FILTER           = 0x00000100;

		/// <summary>
		/// Constants: HDITEM fmt
		/// </summary>
		public const int HDF_LEFT             = 0x00000000;
		public const int HDF_RIGHT            = 0x00000001;
		public const int HDF_CENTER           = 0x00000002;
		public const int HDF_JUSTIFYMASK      = 0x00000003;
		public const int HDF_RTLREADING       = 0x00000004;
		public const int HDF_OWNERDRAW        = 0x00008000;
		public const int HDF_STRING           = 0x00004000;
		public const int HDF_BITMAP           = 0x00002000;
		public const int HDF_BITMAP_ON_RIGHT  = 0x00001000;
		public const int HDF_IMAGE            = 0x00000800;
		public const int HDF_SORTUP           = 0x00000400;
		public const int HDF_SORTDOWN         = 0x00000200;

		public const int HHT_NOWHERE          = 0x00000001;
		public const int HHT_ONHEADER         = 0x00000002;
		public const int HHT_ONDIVIDER        = 0x00000004;
		public const int HHT_ONDIVOPEN        = 0x00000008;
		public const int HHT_ONFILTER         = 0x00000010;
		public const int HHT_ONFILTERBUTTON   = 0x00000020;
		public const int HHT_ABOVE            = 0x00000100;
		public const int HHT_BELOW            = 0x00000200;
		public const int HHT_TORIGHT          = 0x00000400;
		public const int HHT_TOLEFT           = 0x00000800;

        public const int CDDS_PREPAINT = 0x1;
        public const int CDDS_POSTPAINT = 0x2;
        public const int CDDS_PREERASE = 0x3;
        public const int CDDS_POSTERASE = 0x4;
        public const int CDDS_ITEM = 0x00010000;
        public const int CDDS_SUBITEM = 0x00020000;
        public const int CDDS_ITEMPREPAINT = (CDDS_ITEM | CDDS_PREPAINT);
        public const int CDDS_ITEMPOSTPAINT = (CDDS_ITEM | CDDS_POSTPAINT);

        public const int CDRF_DODEFAULT = 0;
        public const int CDRF_NEWFONT = 0x2;
        public const int CDRF_SKIPDEFAULT = 0x4;
        public const int CDRF_NOTIFYITEMDRAW = 0x20;
        public const int CDRF_NOTIFYSUBITEMDRAW = 0x20;
        public const int CDRF_NOTIFYPOSTPAINT = 0x10;

        /// <summary>
		/// Types
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct HDHITTESTINFO
		{
			public POINT pt;
			public int   flags;
			public int   iItem;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		public struct HDITEM
		{
			public int    mask;
			public int    cxy;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpszText;
			public IntPtr hbm;
			public int    cchTextMax;
			public int    fmt;
			public int    lParam;
			public int    iImage;
			public int    iOrder;
			public int    type;
			public IntPtr pvFilter;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		public struct HDITEM2
		{
			public int    mask;
			public int    cxy;
			public IntPtr lpszText;
			public IntPtr hbm;
			public int    cchTextMax;
			public int    fmt;
			public int    lParam;
			public int    iImage;
			public int    iOrder;
			public int    type;
			public IntPtr pvFilter;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct HDLAYOUT
		{
			public IntPtr prc;   // RECT*
			public IntPtr pwpos; // WINDOWPOS*
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		public struct HDTEXTFILTER
		{
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpszText;
			public int cchTextMax;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		public struct NMHDDISPINFO
		{
			public NMHDR hdr;
			public int iItem;
			public int mask;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpszText;
			public int cchTextMax;
			public int iImage;
			public int lParam;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		public struct NMHDFILTERBTNCLICK
		{
			public NMHDR hdr;
			public int iItem;
			public RECT rc;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		public struct NMHEADER
		{
			public NMHDR hdr;
			public int iItem;
			public int iButton;
			public IntPtr pitem;
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

            public int rcLeft;
            public int rcTop;
            public int rcRight;
            public int rcBottom;

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
		/// Static constructor
		/// </summary>
		static NativeHeader()
		{
			if ( IsSysCharSetAnsi() )
			{
				HDM_INSERTITEM      = HDM_FIRST + 1;
				HDM_GETITEM         = HDM_FIRST + 3;
				HDM_SETITEM         = HDM_FIRST + 4;

				HDN_ITEMCHANGING    = HDN_FIRST - 0;
				HDN_ITEMCHANGED     = HDN_FIRST - 1;
				HDN_ITEMCLICK       = HDN_FIRST - 2;
				HDN_ITEMDBLCLICK    = HDN_FIRST - 3;
				HDN_DIVIDERDBLCLICK = HDN_FIRST - 5;
				HDN_BEGINTRACK      = HDN_FIRST - 6;
				HDN_ENDTRACK        = HDN_FIRST - 7;
				HDN_TRACK           = HDN_FIRST - 8;
				HDN_GETDISPINFO     = HDN_FIRST - 9;
			}
			else
			{
				HDM_INSERTITEM      = HDM_FIRST + 10;
				HDM_GETITEM         = HDM_FIRST + 11;
				HDM_SETITEM         = HDM_FIRST + 12;

				HDN_ITEMCHANGING    = HDN_FIRST - 20;
				HDN_ITEMCHANGED     = HDN_FIRST - 21;
				HDN_ITEMCLICK       = HDN_FIRST - 22;
				HDN_ITEMDBLCLICK    = HDN_FIRST - 23;
				HDN_DIVIDERDBLCLICK = HDN_FIRST - 25;
				HDN_BEGINTRACK      = HDN_FIRST - 26;
				HDN_ENDTRACK        = HDN_FIRST - 27;
				HDN_TRACK           = HDN_FIRST - 28;
				HDN_GETDISPINFO     = HDN_FIRST - 29;
			}
		}

		/// <summary>
		/// Helpers
		/// </summary>
		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern int SendMessage(IntPtr hWnd, int msg, bool wParam,
											  int lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
											  ref HDITEM hdi);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
											  ref RECT rc);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam,
												 IntPtr lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
											  ref HDLAYOUT lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
											  ref HDHITTESTINFO lParam);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern int SendMessage(IntPtr hWnd, int msg, int cItems,
											  int[] aOrders);

		/// <summary>
		/// Operations
		/// </summary>
		public static int GetItemCount(IntPtr hwnd)
		{
			Debug.Assert( hwnd != IntPtr.Zero );

			return SendMessage(hwnd, HDM_GETITEMCOUNT, 0, 0);
		}

		public static int InsertItem(IntPtr hWnd, int index, ref HDITEM hdi)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_INSERTITEM, index, ref hdi);
		}

		public static bool DeleteItem(IntPtr hWnd, int index)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_DELETEITEM, index, 0) != 0;
		}

		public static bool GetItem(IntPtr hWnd, int index, ref HDITEM hdi)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_GETITEM, index, ref hdi) != 0;
		}

		public static bool SetItem(IntPtr hWnd, int index, ref HDITEM hdi)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_SETITEM, index, ref hdi) != 0;
		}

		public static bool GetItemRect(IntPtr hWnd, int index, out RECT rect)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			rect = new RECT();

			return SendMessage(hWnd, HDM_GETITEMRECT, index, ref rect) != 0;
		}

		public static IntPtr GetImageList(IntPtr hWnd)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_GETIMAGELIST, 0, IntPtr.Zero);
		}

		public static IntPtr SetImageList(IntPtr hWnd, IntPtr himl)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_SETIMAGELIST, 0, himl);
		}

		public static IntPtr CreateDragImage(IntPtr hWnd, int index)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_CREATEDRAGIMAGE, index, IntPtr.Zero);
		}

		public static bool Layout(IntPtr hWnd, ref HDLAYOUT layout)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_LAYOUT, 0, ref layout) != 0;
		}

		public static int HitTest(IntPtr hWnd, ref HDHITTESTINFO hdhti)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_HITTEST, 0, ref hdhti);
		}

		public static int GetBitmapMargin(IntPtr hWnd)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_GETBITMAPMARGIN, 0, 0);
		}

		public static int SetBitmapMargin(IntPtr hWnd, int iWidth)
		{
			Debug.Assert( hWnd != IntPtr.Zero && iWidth >= 0 );

			return SendMessage(hWnd, HDM_SETBITMAPMARGIN, iWidth, 0);
		}

		public static int SetHotDivider(IntPtr hWnd, bool flag, int dwInputValue)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_SETHOTDIVIDER, flag, dwInputValue);
		}

		public static int OrderToIndex(IntPtr hWnd, int iOrder)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_ORDERTOINDEX, iOrder, 0);
		}

		public static bool GetOrderArray(IntPtr hWnd, out int[] aOrders)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			int cItems = GetItemCount(hWnd);
			aOrders = new int[cItems];

			return SendMessage(hWnd, HDM_GETORDERARRAY, cItems, aOrders) != 0;
		}

		public static bool SetOrderArray(IntPtr hWnd, int[] aOrders)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			return SendMessage(hWnd, HDM_GETORDERARRAY, aOrders.Length, aOrders) != 0;
		}

		public static bool GetUnicodeFormat(IntPtr hWnd)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			// ???

			return false;
		}

		public static bool SetUnicodeFormat(IntPtr hWnd, bool fUnicode)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			// ???

			return false;
		}

		public static int ClearAllFilters(IntPtr hWnd)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			// ???

			return 0;
		}

		public static int ClearFilter(IntPtr hWnd, int index)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			// ???

			return 0;
		}

		public static int EditFilter(IntPtr hWnd, int i, bool fDiscardChanges)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			// ???

			return 0;
		}

		public static int SetFilterChangeTimeout(IntPtr hWnd, int i)
		{
			Debug.Assert( hWnd != IntPtr.Zero );

			// ???

			return 0;
		}

	} // NativeHeader class
}
