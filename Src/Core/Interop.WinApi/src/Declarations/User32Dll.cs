// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using JetBrains.Annotations;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// User32.dll functions.
	/// Must be 64bit-safe.
	/// </summary>
	/// <remarks>
	/// IMPORTANT! Rules for authoring the class (v1.1):
	/// (1) All the function declarations MUST be 64-bit aware.
	/// (2) When copypasting from older declarations, you MUST check against the MSDN help or header declaration,
	///		and you MUST ensure that each parameter has a proper size.
	/// (3) Call the Wide version of the functions (UCS-2-LE) unless there's a strong reason for calling the ANSI version
	///		(such a reason MUST be indicated in XmlDoc). <c>CharSet = CharSet.Unicode</c>.
	/// (4) ExactSpelling MUST be TRUE. Add the "…W" suffix wherever needed.
	/// (5) SetLastError SHOULD be considered individually for each function. Setting it to <c>True</c> allows to report the errors,
	///		but slows down the execution of critical members.
	/// (6) These properties MUST be explicitly set on DllImport attributes of EACH import:
	///		CharSet, PreserveSig, SetLastError, ExactSpelling.
	/// (7) CLR names MUST be used for types instead of C# ones, eg "Int32" not "int" and "Int64" not "long".
	///		This greately improves the understanding of the parameter sizes.
	/// (8) Sign of the types MUST be favored, eg "DWORD" is "UInt32" not "Int32".
	/// (9) Unsafe pointer types should be used for explicit and implicit pointers rather than IntPtr.
	///		This way we outline the unsafety of the native calls, and also make it more clear for the 64bit transition.
	///		Eg "HANDLE" is "void*". If the rule forces you to mark some assembly as unsafe, it's an indication a managed utility
	///		incapsulating the call and the handle should be provided in one of the already-unsafe assemblies.
	/// (A) Same rules must apply to members of the structures.
	/// (B) All of the structures MUST have the [StructLayout(LayoutKind.Sequential)], [NoReorder] attributes, as appropriate.
	/// </remarks>
	public static unsafe class User32Dll
	{
		#region Operations

		/// <summary>
		/// The DestroyWindow function destroys the specified window. The function sends WM_DESTROY and WM_NCDESTROY messages to the window to deactivate it and remove the keyboard focus from it. The function also destroys the window's menu, flushes the thread message queue, destroys timers, removes clipboard ownership, and breaks the clipboard viewer chain (if the window is at the top of the viewer chain). If the specified window is a parent or owner window, DestroyWindow automatically destroys the associated child or owned windows when it destroys the parent or owner window. The function first destroys child or owned windows, and then it destroys the parent or owner window. DestroyWindow also destroys modeless dialog boxes created by the CreateDialog function.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 DestroyWindow(void* hWnd);

		/// <summary>
		/// The FillRect function fills a rectangle by using the specified brush. This function includes the left and top borders, but excludes the right and bottom borders of the rectangle.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern Int32 FillRect(void* hDC, RECT* lprc, void* hbr);

		/// <summary>
		/// Retrieves the specified system metric or system configuration setting.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern Int32 GetSystemMetrics(Int32 nIndex);

		/// <summary>
		/// The GetWindowLongPtrW function retrieves information about the specified window. The function also retrieves the value at a specified offset into the extra window memory.
		/// </summary>
		public static IntPtr GetWindowLongPtrW(void* hWnd, Int32 nIndex)
		{
			switch(IntPtr.Size)
			{
			case 4:
				return Only32Bit.GetWindowLongPtrW(hWnd, nIndex);
			case 8:
				return Only64Bit.GetWindowLongPtrW(hWnd, nIndex);
			default:
				throw new InvalidOperationException(string.Format("This platform has {0}-byte pointers, which is not supported by the interop wrapper.", IntPtr.Size));
			}
		}

		/// <summary>
		/// The <see cref="GetWindowRect"/> function retrieves the dimensions of the bounding rectangle of the specified window. The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 GetWindowRect(void* hWnd, RECT* lpRect);

		/// <summary>
		/// The InvalidateRect function adds a rectangle to the specified window's update region. The update region represents the portion of the window's client area that must be redrawn.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern Int32 InvalidateRect(void* hWnd, RECT* lpRect, Int32 bErase);

		/// <summary>
		/// The <see cref="IsWindow"/> function determines whether the specified window handle identifies an existing window.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern Int32 IsWindow(void* hWnd);

		/// <summary>
		/// The LoadString function loads a string resource from the executable file associated with a specified module, copies the string into a buffer, and appends a terminating NULL character.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 LoadStringW(void* hInstance, UInt32 uID, UInt16* lpBuffer, Int32 nBufferMax);

		/// <summary>
		/// The MessageBox function creates, displays, and operates a message box. The message box contains an application-defined message and title, along with any combination of predefined icons and push buttons.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 MessageBoxW(void* hWnd, string lpText, string lpCaption, UInt32 uType);

		/// <summary>
		/// Waits until one or all of the specified objects are in the signaled state or the time-out interval elapses. The objects can include input event objects, which you specify using the dwWakeMask parameter. To enter an alertable wait state, use the <see cref="MsgWaitForMultipleObjectsEx"/> function.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern UInt32 MsgWaitForMultipleObjects(UInt32 nCount, void** pHandles, Int32 bWaitAll, UInt32 dwMilliseconds, UInt32 dwWakeMask);

		/// <summary>
		/// Waits until one or all of the specified objects are in the signaled state, an I/O completion routine or asynchronous procedure call (APC) is queued to the thread, or the time-out interval elapses. The array of objects can include input event objects, which you specify using the dwWakeMask parameter.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern UInt32 MsgWaitForMultipleObjectsEx(UInt32 nCount, void** pHandles, UInt32 dwMilliseconds, UInt32 dwWakeMask, UInt32 dwFlags);

		/// <summary>
		/// The PostMessageW function places (posts) a message in the message queue associated with the thread that created the specified window and returns without waiting for the thread to process the message. To post a message in the message queue associate with a thread, use the PostThreadMessage function.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern Int32 PostMessageW(void* hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// The PostThreadMessage function posts a message to the message queue of the specified thread. It returns without waiting for the thread to process the message.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern Int32 PostThreadMessageW(UInt32 idThread, UInt32 Msg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// The PrintWindow function copies a visual window into the specified device context (DC), typically a printer DC.
		/// </summary>
		/// <param name="hwnd">Window to copy</param>
		/// <param name="hdcBlt">HDC to print into</param>
		/// <param name="nFlags">Optional flags</param>
		/// <returns>If the function succeeds, it returns a nonzero value.
		/// If the function fails, it returns zero.</returns>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 PrintWindow(void* hwnd, void* hdcBlt, UInt32 nFlags);

		/// <summary>
		/// The SendMessage function sends the specified message to a window or windows. It calls the window procedure for the specified window and does not return until the window procedure has processed the message.
		/// To send a message and return immediately, use the SendMessageCallback or SendNotifyMessage function. To post a message to a thread's message queue and return immediately, use the PostMessageW or PostThreadMessage function.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern IntPtr SendMessageW(void* hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// The SetLayeredWindowAttributes function sets the opacity and transparency color key of a layered window.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 SetLayeredWindowAttributes(void* hwnd, UInt32 crKey, Byte bAlpha, UInt32 dwFlags);

		/// <summary>
		/// The SetWindowLongPtrW function changes an attribute of the specified window. The function also sets a value at the specified offset in the extra window memory.
		/// </summary>
		public static IntPtr SetWindowLongPtrW(void* hWnd, Int32 nIndex, IntPtr dwNewLong)
		{
			switch(IntPtr.Size)
			{
			case 4:
				return Only32Bit.SetWindowLongPtrW(hWnd, nIndex, dwNewLong);
			case 8:
				return Only64Bit.SetWindowLongPtrW(hWnd, nIndex, dwNewLong);
			default:
				throw new InvalidOperationException(string.Format("This platform has {0}-byte pointers, which is not supported by the interop wrapper.", IntPtr.Size));
			}
		}

		/// <summary>
		/// The ShowWindow function sets the specified window's show state.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern UInt32 ShowWindow(void* hWnd, int nCmdShow);

		/// <summary>
		/// The ValidateRect function validates the client area within a rectangle by removing the rectangle from the update region of the specified window.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern int ValidateRect(void* hWnd, RECT* lpRect);

		#endregion

		#region Helpers Type

		public static class Helpers
		{
			#region Data

			private const int StringLen = 0x400;

			#endregion

			#region Operations

			/// <summary>
			/// Calls <see cref="User32Dll.DestroyWindow"/>.
			/// </summary>
			public static bool DestroyWindow(IntPtr hwnd)
			{
				return User32Dll.DestroyWindow((void*)hwnd) != 0;
			}

			/// <summary>
			/// Wraps <see cref="User32Dll.GetWindowRect"/>.
			/// </summary>
			public static Rectangle GetWindowRect(IntPtr hwnd)
			{
				RECT rc;
				if(User32Dll.GetWindowRect((void*)hwnd, &rc) == 0)
					throw new Win32Exception();
				return rc;
			}

			/// <summary>
			/// Invalidates the specific rectangle. If <paramref name="rect"/> is <c>Null</c>, the whole window is invalidated.
			/// </summary>
			public static bool InvalidateRect(IntPtr hWnd, Rectangle? rect, bool erase)
			{
				if(hWnd == IntPtr.Zero)
					return false;

				if(rect == null) // Full area
					return User32Dll.InvalidateRect((void*)hWnd, null, erase ? 1 : 0) != 0;

				RECT rc = (Rectangle)rect;
				return User32Dll.InvalidateRect((void*)hWnd, &rc, erase ? 1 : 0) != 0;
			}

			/// <summary>
			/// The PostMessageW function places (posts) a message in the message queue associated with the thread that created the specified window and returns without waiting for the thread to process the message.
			/// </summary>
			public static bool PostMessage(IntPtr hWnd, WindowsMessages msg, IntPtr wParam, IntPtr lParam)
			{
				return PostMessageW((void*)hWnd, (UInt32)msg, wParam, lParam) != 0;
			}

			/// <summary>
			/// The SendMessage function sends the specified message to a window or windows. It calls the window procedure for the specified window and does not return until the window procedure has processed the message.
			/// To send a message and return immediately, use the SendMessageCallback or SendNotifyMessage function. To post a message to a thread's message queue and return immediately, use the PostMessageW or PostThreadMessage function.
			/// </summary>
			public static IntPtr SendMessageW(IntPtr hWnd, WindowsMessages msg, IntPtr wParam, IntPtr lParam)
			{
				return User32Dll.SendMessageW((void*)hWnd, (uint)msg, wParam, lParam);
			}

			/// <summary>
			/// Wraps the <see cref="User32Dll.SetLayeredWindowAttributes"/> calls.
			/// </summary>
			public static bool SetLayeredWindowAttributes([NotNull] IWin32Window window, Color colorkey, double alpha, SetLayeredWindowAttributesFlags flags)
			{
				if(window == null)
					throw new ArgumentNullException("window");
				if((alpha < 0) || (alpha > 1))
					throw new ArgumentOutOfRangeException("alpha", alpha, "The alpha value must be in the [0…1] range.");

				return User32Dll.SetLayeredWindowAttributes((void*)window.Handle, unchecked((uint)colorkey.ToArgb()), (byte)(alpha * 0xFF), (uint)flags) != 0;
			}

			/// <summary>
			/// The ShowWindow function sets the specified window's show state.
			/// </summary>
			public static bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow)
			{
				return User32Dll.ShowWindow((void*)hWnd, (int)nCmdShow) != 0;
			}

			/// <summary>
			/// Loads a Win32 string resource from a native DLL.
			/// Returns <c>Null</c> on errors.
			/// </summary>
			[CanBeNull]
			public static string TryLoadStringResource([NotNull] string sDllFilename, uint nResourceId)
			{
				if(sDllFilename == null)
					throw new ArgumentNullException("sDllFilename");

				// DLL
				void* hModule = Kernel32Dll.LoadLibraryW(Path.GetFullPath(Environment.ExpandEnvironmentVariables(sDllFilename)));
				if(hModule == null)
					return null;

				// Resource
				UInt16* buffer = stackalloc UInt16[StringLen];
				if(LoadStringW(hModule, nResourceId, buffer, StringLen) == 0)
					return null;

				buffer[StringLen - 1] = 0; // Ensure it's Null-terminated
				return new string((char*)buffer);
			}

			#endregion
		}

		#endregion

		#region Only32Bit Type

		public static class Only32Bit
		{
			#region Implementation

			/// <summary>
			/// The GetWindowLongPtrW function retrieves information about the specified window. The function also retrieves the value at a specified offset into the extra window memory.
			/// </summary>
			[DllImport("user32.dll", EntryPoint = "GetWindowLongW", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)] // Note: 32-but platforms define this as a macro that points to the legacy function
			internal static extern IntPtr GetWindowLongPtrW(void* hWnd, Int32 nIndex);

			/// <summary>
			/// The SetWindowLongPtrW function changes an attribute of the specified window. The function also sets a value at the specified offset in the extra window memory.
			/// </summary>
			[DllImport("user32.dll", EntryPoint = "SetWindowLongW", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)] // Note: 32-but platforms define this as a macro that points to the legacy function
			internal static extern IntPtr SetWindowLongPtrW(void* hWnd, Int32 nIndex, IntPtr dwNewLong);

			#endregion
		}

		#endregion

		#region Only64Bit Type

		public static class Only64Bit
		{
			#region Operations

			/// <summary>
			/// The GetWindowLongPtrW function retrieves information about the specified window. The function also retrieves the value at a specified offset into the extra window memory.
			/// </summary>
			[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
			public static extern IntPtr GetWindowLongPtrW(void* hWnd, Int32 nIndex);

			/// <summary>
			/// The SetWindowLongPtrW function changes an attribute of the specified window. The function also sets a value at the specified offset in the extra window memory.
			/// </summary>
			[DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
			public static extern IntPtr SetWindowLongPtrW(void* hWnd, Int32 nIndex, IntPtr dwNewLong);

			#endregion
		}

		#endregion
	}
}
