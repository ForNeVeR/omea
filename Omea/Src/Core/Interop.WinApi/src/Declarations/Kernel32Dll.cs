/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

using JetBrains.Annotations;
using JetBrains.Interop.WinApi.Interfaces;

namespace JetBrains.Interop.WinApi
{
	/// <summary>
	/// Kernel32.dll functions.
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
	public static unsafe class Kernel32Dll
	{
		#region Operations

		/// <summary>
		/// Closes an open object handle.
		/// </summary>
		/// <param name="hObject">A valid handle to an open object.</param>
		/// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call GetLastError. If the application is running under a debugger, the function will throw an exception if it receives either a handle value that is not valid or a pseudo-handle value. This can happen if you close a handle twice, or if you call CloseHandle on a handle returned by the FindFirstFile function.</returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 CloseHandle(void* hObject);

		/// <summary>
		/// Takes a snapshot of the specified processes, as well as the heaps, modules, and threads used by these processes.
		/// </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern void* CreateToolhelp32Snapshot(UInt32 dwFlags, UInt32 th32ProcessID);

		/// <summary>
		/// Determines the location of a resource with the specified type and name in the specified module. To specify a language, use the FindResourceEx function.
		/// </summary>
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		public static extern void* FindResourceW(void* hModule, [MarshalAs(UnmanagedType.LPWStr)] string lpName, [MarshalAs(UnmanagedType.LPWStr)] string lpType);

		/// <summary>
		/// The GetCurrentThreadId function retrieves the thread identifier of the calling thread.
		/// Note: same as <see cref="System.AppDomain.GetCurrentThreadId"/>, but doesn't raise the “Obsolete” warning.
		/// </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern UInt32 GetCurrentThreadId();

		[Obsolete("Does not work in .NET, because reports interop/marshalling error status rather than WinAPI one.")]
		public static UInt32 GetLastError()
		{
			throw new NotSupportedException("The GetLastError function is not supported on .NET.");
		}

		/// <summary>
		/// Retrieves an integer associated with a key in the specified section of an initialization file. Note: This function is provided only for compatibility with 16-bit Windows-based applications. Applications should store initialization information in the registry.
		/// </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern UInt32 GetPrivateProfileIntW([MarshalAs(UnmanagedType.LPWStr)] string lpAppName, [MarshalAs(UnmanagedType.LPWStr)] string lpKeyName, Int32 nDefault, [MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

		/// <summary>
		/// Retrieves a string from the specified section in an initialization file.
		/// Note: This function is provided only for compatibility with 16-bit Windows-based applications. Applications should store initialization information in the registry.
		/// </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern UInt32 GetPrivateProfileStringW([MarshalAs(UnmanagedType.LPWStr)] string lpAppName, [MarshalAs(UnmanagedType.LPWStr)] string lpKeyName, [MarshalAs(UnmanagedType.LPWStr)] string lpDefault, UInt16* lpReturnedString, UInt32 nSize, [MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

		/// <summary>
		/// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
		/// </summary>
		/// <param name="hModule">A handle to the DLL module that contains the function or variable. The LoadLibrary or GetModuleHandle function returns this handle.</param>
		/// <param name="lpProcName">The function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be in the low-order word; the high-order word must be zero.</param>
		/// <returns>If the function succeeds, the return value is the address of the exported function or variable. If the function fails, the return value is NULL. To get extended error information, call GetLastError.</returns>
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern void* GetProcAddress(void* hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern UInt32 GetTickCount();

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 GlobalMemoryStatusEx([In] [Out] MEMORYSTATUSEX* lpBuffer);

		/// <summary>
		/// The LoadLibrary function maps the specified executable module into the address space of the calling process. 
		/// For additional load options, use the LoadLibraryEx function.
		/// </summary>
		/// <param name="lpFileName">[in] Pointer to a null-terminated string that names the executable module (either a .dll or .exe file). The name specified is the file name of the module and is not related to the name stored in the library module itself, as specified by the LIBRARY keyword in the module-definition (.def) file. 
		/// If the string specifies a path but the file does not exist in the specified directory, the function fails. When specifying a path, be sure to use backslashes (\), not forward slashes (/).
		/// If the string does not specify a path, the function uses a standard search strategy to find the file. See the Remarks for more information.</param>
		/// <returns>If the function succeeds, the return value is a handle to the module.
		/// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
		/// Windows Me/98/95:  If you are using LoadLibrary to load a module that contains a resource whose numeric identifier is greater than 0x7FFF, LoadLibrary fails. If you are attempting to load a 16-bit DLL directly from 32-bit code, LoadLibrary fails. If you are attempting to load a DLL whose subsystem version is greater than 4.0, LoadLibrary fails. If your DllMain function tries to call the Unicode version of a function, LoadLibrary fails.</returns>
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern void* LoadLibraryW(string lpFileName);

		// The string must be ANSI

		/// <summary>
		/// The OutputDebugString function sends a string to the debugger for display.
		/// </summary>
		/// <param name="lpOutputString">[in] Pointer to the null-terminated string to be displayed.</param>
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
		public static extern void OutputDebugStringW(string lpOutputString);

		/// <summary>
		/// Retrieves information about the first process encountered in a system snapshot.
		/// </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 Process32FirstW(void* hSnapshot, PROCESSENTRY32* lppe);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 Process32NextW(void* hSnapshot, PROCESSENTRY32* lppe);

		/// <summary>
		/// Waits until one or all of the specified objects are in the signaled state or the time-out interval elapses. To enter an alertable wait state, use the WaitForMultipleObjectsEx function.
		/// </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern UInt32 WaitForMultipleObjects(UInt32 nCount, void** lpHandles, Int32 bWaitAll, UInt32 dwMilliseconds);

		/// <summary>
		/// Copies a string into the specified section of an initialization file. Note: This function is provided only for compatibility with 16-bit versions of Windows. Applications should store initialization information in the registry.
		/// </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
		public static extern Int32 WritePrivateProfileStringW(string lpAppName, string lpKeyName, string lpString, string lpFileName);

		#endregion

		#region Implementation

		/// <summary>
		/// Loads the specified resource into global memory. 
		/// </summary>
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		internal static extern void* LoadResource(void* hModule, void* hResInfo);

		/// <summary>
		/// Locks the specified resource in memory. 
		/// </summary>
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		internal static extern void* LockResource(void* hResData);

		/// <summary>
		/// Returns the size, in bytes, of the specified resource. 
		/// </summary>
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		internal static extern UInt32 SizeofResource(void* hModule, void* hResInfo);

		#endregion

		#region Helpers Type

		/// <summary>
		/// Helpers for the raw functions.
		/// </summary>
		public static class Helpers
		{
			#region Data

			private const int StringLen = 0x400;

			#endregion

			#region Operations

			/// <summary>
			/// Creates an instance of a COM object without the Registry information, by loading the DLL and invoking its class factory.
			/// </summary>
			/// <param name="sDllFilename">Pathname of the DLL.</param>
			/// <param name="guidClsid">CLSID of the object to create.</param>
			public static object CoCreateInstanceExplicit(string sDllFilename, Guid guidClsid)
			{
				// Load DLL
				void* hDll = LoadLibraryW(sDllFilename);
				if(hDll == null)
				{
					var exSys = new Win32Exception();
					throw new InvalidOperationException(string.Format("Could not load the COM library “{0}”. {1}", sDllFilename, exSys.Message), exSys);
				}

				// Get factory provider entry point
				void* pMethod = GetProcAddress(hDll, "DllGetClassObject");
				if(pMethod == null)
				{
					var exSys = new Win32Exception();
					throw new InvalidOperationException(string.Format("Could not get the DllGetClassObject entry point from the COM library “{0}”. {1}", sDllFilename, exSys.Message), exSys);
				}
				var funcDllGetClassObject = (DllGetClassObjectDelegate)Marshal.GetDelegateForFunctionPointer((IntPtr)pMethod, typeof(DllGetClassObjectDelegate));

				// Get factory
				Guid iidClassFactory = Marshal.GenerateGuidForType(typeof(IClassFactory));
				int retval;
				IClassFactory factory;
				if((retval = funcDllGetClassObject(&guidClsid, &iidClassFactory, out factory)) < 0)
					Marshal.ThrowExceptionForHR(retval);

				// Make the factory create the object
				var iidIUnknown = new Guid("00000000-0000-0000-C000-000000000046");
				object instance;
				factory.CreateInstance(null, iidIUnknown, out instance);

				// Done
				return instance;
			}

			/// <summary>
			/// Reads an .ini string.
			/// </summary>
			[NotNull]
			public static string GetProfileString([NotNull] string sFilePath, [NotNull] string sSection, [NotNull] string sKey, [NotNull] string sDefaultValue)
			{
				if(sFilePath == null)
					throw new ArgumentNullException("sFilePath");
				if(sSection == null)
					throw new ArgumentNullException("sSection");
				if(sKey == null)
					throw new ArgumentNullException("sKey");
				if(sDefaultValue == null)
					throw new ArgumentNullException("sDefaultValue");

				UInt16* buffer = stackalloc UInt16[StringLen];
				GetPrivateProfileStringW(sSection, sKey, sDefaultValue, buffer, StringLen, sFilePath);
				buffer[StringLen - 1] = 0;
				return new string((sbyte*)buffer);
			}

			/// <summary>
			/// Loads a native dll and looks up the resource.
			/// Throws on errors.
			/// </summary>
			[NotNull]
			public static byte[] GetWin32Resource([NotNull] string sDllFilename, [NotNull] string sResourceName, [NotNull] string sResourceType)
			{
				if(sDllFilename == null)
					throw new ArgumentNullException("sDllFilename");
				if(sResourceName == null)
					throw new ArgumentNullException("sResourceName");
				if(sResourceType == null)
					throw new ArgumentNullException("sResourceType");

				void* hModule = LoadLibraryW(Path.GetFullPath(sDllFilename)); // Freeing not needed
				if(hModule == null)
				{
					var exSys = new Win32Exception();
					throw new Win32InteropException(string.Format("Could not load the unmanaged DLL “{0}”. {1}", sDllFilename, exSys.Message), exSys);
				}

				void* hResInfo = FindResourceW(hModule, sResourceName, sResourceType);
				if(hResInfo == null)
				{
					var exSys = new Win32Exception();
					throw new Win32InteropException(string.Format("Could not find the unmanaged resource “{0}” of type “{1}” in the DLL “{3}”. {2}", sResourceName, sResourceType, exSys.Message, sDllFilename), exSys);
				}

				void* hResData = LoadResource(hModule, hResInfo);
				if(hResData == null)
					throw new Win32Exception();

				var pResBytes = (byte*)LockResource(hResData);
				if(pResBytes == null)
					throw new Win32Exception();

				var nResourceSize = unchecked((int)SizeofResource(hModule, hResInfo));
				if(nResourceSize < 0)
					throw new InvalidOperationException(string.Format("Negative resource size encountered."));

				var data = new byte[nResourceSize];
				Marshal.Copy((IntPtr)pResBytes, data, 0, data.Length);

				return data;
			}

			#endregion

			#region DllGetClassObjectDelegate Type

			/// <summary>
			/// Helper for the <see cref="Helpers.CoCreateInstanceExplicit"/> function.
			/// </summary>
			private delegate Int32 DllGetClassObjectDelegate(Guid* rclsid, Guid* riid, [MarshalAs(UnmanagedType.Interface)] [Out] out IClassFactory ppv);

			#endregion
		}

		#endregion
	}
}