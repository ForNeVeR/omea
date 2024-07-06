// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text;
using System.Runtime.InteropServices;

using JetBrains.Interop.WinApi;

namespace JetBrains.Omea.Base
{
	[StructLayout(LayoutKind.Sequential)]
    public struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    };

	public class GenericWindow
	{
		public static IntPtr FindWindow( string strClassName, string strWindowName )
		{
			return WindowsAPI.FindWindow( strClassName, strWindowName );
		}
	}

	public class WindowsAPI
	{

		[DllImport("user32", CharSet=CharSet.Auto)]
		public static extern IntPtr FindWindow( string strClassName, string strWindowName );

        [DllImport("user32")]
        public static extern IntPtr WindowFromPoint( POINT point );

		public const uint INFINITE          = 0xffffffff;
        public const uint WAIT_OBJECT_0     = 0;
        public const uint WAIT_FAILED       = 0xffffffff;
        public const uint WAIT_TIMEOUT      = 258;

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern bool GetLastInputInfo( ref LASTINPUTINFO lii );

		[StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
        public static extern IntPtr GetCurrentThread();



		[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
        public static extern bool GetThreadTimes(
            IntPtr handle, ref FILETIME creationTime, ref FILETIME exitTime, ref FILETIME kernelTime, ref FILETIME userTime );


		/**
         * shell api
         */
        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr   hwnd;
            public uint     wFunc;
            public String   pFrom;
            public String   pTo;
            public uint     fFlags;
            public bool     fAnyOperationsAborted;
            public IntPtr   hNameMappings;
            public String   lpszProgressTitle;
        }

        public const int MAX_PATH = 260;

        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int    iIcon;
            public uint   dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public const uint FO_DELETE = 0x03;
        public const uint FOF_ALLOWUNDO = 0x40;

        public const uint SHGFI_LARGEICON = 0x0;
        public const uint SHGFI_SMALLICON = 0x1;
        public const uint SHGFI_ICON = 0x100;

        [DllImport("shell32.dll")]
        public static extern int SHFileOperation( ref SHFILEOPSTRUCT opts );
        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo( string path, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags );


	}

    public class Shell32
    {
        public static int MoveFile2RecycleBin( params string[] fullNames )
        {
            WindowsAPI.SHFILEOPSTRUCT fileOperation = new WindowsAPI.SHFILEOPSTRUCT();
            fileOperation.wFunc = WindowsAPI.FO_DELETE;
            fileOperation.pFrom = String.Join( "\0", fullNames ) + "\0";
            fileOperation.fFlags = WindowsAPI.FOF_ALLOWUNDO;
            return WindowsAPI.SHFileOperation( ref fileOperation );
        }
    }

    public class WindowsMultiMedia
    {
        public const uint SND_ASYNC     = 1;
        public const uint SND_FILENAME  = 0x00020000;
        [DllImport("Winmm.dll")]
        public static extern bool PlaySound( string sound, IntPtr hModule, uint flags );
    }

    public class Winsock
    {
        public const System.Int32 FD_ACCEPT = 8;
        public const System.UInt32 WSA_WAIT_EVENT_0 = WindowsAPI.WAIT_OBJECT_0;
        public const System.UInt32 WSA_INFINITE = WindowsAPI.INFINITE;

        [DllImport("Ws2_32.dll")]
        public extern static System.UInt32 WSAWaitForMultipleEvents( System.UInt32 cEvents, IntPtr[] pEvents, System.Int32 fWaitAll, System.UInt32 timeout, System.Int32 fAlterable );

        [DllImport("Ws2_32.dll")]
        public extern static IntPtr WSACreateEvent();

        [DllImport("Ws2_32.dll")]
        public extern static System.Int32 WSACloseEvent(IntPtr hEvent);

        [DllImport("Ws2_32.dll")]
        public extern static System.Int32 WSASetEvent(IntPtr hEvent);

        [DllImport("Ws2_32.dll")]
        public extern static System.Int32 WSAResetEvent(IntPtr hEvent);

        [DllImport("Ws2_32.dll")]
        public extern static System.Int32 WSAEventSelect(IntPtr hSocket, IntPtr hEvent, System.Int32 lNetworkEvents);
    }
}
