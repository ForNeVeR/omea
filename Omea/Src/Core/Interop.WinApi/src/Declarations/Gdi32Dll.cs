// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;

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
	public static unsafe class Gdi32Dll
	{
		#region Operations

		/// <summary>
		/// The CreatePen function creates a logical pen that has the specified style, width, and color. The pen can subsequently be selected into a device context and used to draw lines and curves.
		/// </summary>
		[DllImport("gdi32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern void* CreatePen(int fnPenStyle, int nWidth, UInt32 crColor);

		/// <summary>
		/// The GetStockObject function retrieves a handle to one of the stock pens, brushes, fonts, or palettes.
		/// </summary>
		[DllImport("gdi32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern void* GetStockObject(Int32 fnObject);

		#endregion
	}
}
