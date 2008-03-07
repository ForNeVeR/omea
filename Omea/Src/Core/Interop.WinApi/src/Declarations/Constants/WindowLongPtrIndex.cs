/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Index constants for the <see cref="Win32Declarations.GetWindowLongPtr"/> and <see cref="Win32Declarations.SetWindowLongPtr"/> functions.
	/// </summary>
	public enum WindowLongPtrIndex
	{
		/// <summary>
		/// Sets a new extended window style. For more information, see CreateWindowEx. 
		/// </summary>
		GWL_EXSTYLE = (-20),

		/// <summary>
		/// Sets a new window style.
		/// </summary>
		GWL_STYLE = (-16),

		/// <summary>
		/// Sets a new address for the window procedure. 
		/// Same as GWL_WNDPROC that is for non-“ptr” versions.
		/// </summary>
		GWLP_WNDPROC = (-4),

		/// <summary>
		/// Sets a new application instance handle.
		/// Same as GWL_HINSTANCE that is for non-“ptr” versions.
		/// </summary>
		GWLP_HINSTANCE = (-6),

		/// <summary>
		/// Do not call <see cref="SetWindowLongPtr"/> with the <see cref="GWLP_HWNDPARENT"/> index to change the parent of a child window. Instead, use the SetParent function.
		/// </summary>
		GWLP_HWNDPARENT = (-8),

		/// <summary>
		/// Sets a new identifier of the window.
		/// Same as GWL_ID that is for non-“ptr” versions.
		/// </summary>
		GWLP_ID = (-12),

		/// <summary>
		/// Sets the user data associated with the window. This data is intended for use by the application that created the window. Its value is initially zero.
		/// Same as GWL_USERDATA that is for non-“ptr” versions.
		/// </summary>
		GWLP_USERDATA = (-21),

		/// <summary>
		/// Sets the new pointer to the dialog box procedure.
		/// Valid for dialog boxes only.
		/// </summary>
		DWLP_DLGPROC = DWLP_MSGRESULT + 4, // Note: non-64bit-safe!!

		/// <summary>
		/// Sets the return value of a message processed in the dialog box procedure.
		/// Valid for dialog boxes only.
		/// </summary>
		DWLP_MSGRESULT = 0,

		/// <summary>
		/// Sets new extra information that is private to the application, such as handles or pointers.			
		/// Valid for dialog boxes only.
		/// </summary>
		DWLP_USER = DWLP_DLGPROC + 4, // Note: non-64bit-safe!!
	}
}