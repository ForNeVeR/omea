/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Runtime.InteropServices;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Declarations for the UxTheme.Dll.
	/// Must be 64bit-compatible.
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
	public static unsafe class UxThemeDll
	{
		#region Operations

		/// <summary>
		/// Closes the theme data handle.
		/// </summary>
		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern Int32 CloseThemeData(void* hTheme);

		/// <summary>
		/// Draws the border and fill defined by the visual style for the specified control part.
		/// </summary>
		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern Int32 DrawThemeBackground(void* hTheme, void* hdc, Int32 iPartId, Int32 iStateId, RECT* pRect, RECT* pClipRect);

		/// <summary>
		/// Checks whether the UxTheme DLL is available on this platform.
		/// </summary>
		public static bool IsAvailable()
		{
			return Kernel32Dll.LoadLibraryW("UxTheme.dll") != null;
		}

		/// <summary>
		/// Opens the theme data for a window and its associated class.
		/// </summary>
		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern void* OpenThemeData(void* hwnd, string pszClassList);

		#endregion
	}
}