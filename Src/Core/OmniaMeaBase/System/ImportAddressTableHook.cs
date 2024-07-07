// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using JetBrains.Annotations;

namespace JetBrains.Util.Interop
{
	/// <summary>
	/// Provides for hooking into the Import Address Table (IAT) of a DLL.
	/// </summary>
	public static unsafe class ImportAddressTableHook
	{
		#region Data

		/// <summary>
		/// Limits the number or items in the list to guard off infinite loops.
		/// </summary>
		private static readonly int IterationLimit = 0x1000;

		private static readonly List<Delegate> myDelegateReferences = new List<Delegate>();

		#endregion

		#region Operations

		/// <summary>
		/// Installs an Import Address Table (IAT) hook.
		/// You choose a function (<paramref name="sFuncMod"/>, <paramref name="sFuncName"/>) whose entry will be replaced in the IAT of the specified module (<paramref name="sCallingMod"/>) to point to your own implementation (<see cref="pNewFunction"/>) instead.
		/// </summary>
		/// <param name="sFuncMod">Name of the module in which the function-to-be-hooked is defined. Example: <c>USER32.DLL</c>.</param>
		/// <param name="sFuncName">Name of the function to be hooked. Example: <c>SystemParametersInfoW</c>. Note that for the functions that have separate ANSI and Wide versions you must include a suffix in the function name. Must have the <c>stdcall</c> (<c>WINAPI</c>, <c>PASCAL</c>) calling convention.</param>
		/// <param name="sCallingMod">The module whose IAT is to be patched. Its calls to the Function will be intercepted. Must be loadable with <c>LoadLibrary</c> (or already loaded).</param>
		/// <param name="pNewFunction">The new implementation to replace the Function, in view of <paramref name"sCallingMod"/>. The hook will hold a reference on the delegate. Note that it's up to you to provide the appropriate signature of the delegate, which must match the one of the Function. Pay attention to the charset and bitness issues.</param>
		public static void Install([NotNull] string sFuncMod, [NotNull] string sFuncName, [NotNull] string sCallingMod, [NotNull] Delegate pNewFunction)
		{
			if(sFuncMod == null)
				throw new ArgumentNullException("sFuncMod");
			if(sFuncName == null)
				throw new ArgumentNullException("sFuncName");
			if(sCallingMod == null)
				throw new ArgumentNullException("sCallingMod");
			if(pNewFunction == null)
				throw new ArgumentNullException("pNewFunction");

			void* hmodCaller = UnsafeNativeMethods.LoadLibraryW(sCallingMod);
			if(Marshal.GetLastWin32Error() != 0)
				throw new InvalidOperationException(string.Format("Could not load the module {0}.", sCallingMod.QuoteIfNeeded()), new Win32Exception());
			if(hmodCaller == null)
				throw new InvalidOperationException(string.Format("Could not load the module {0}.", sCallingMod.QuoteIfNeeded()));

			void* hmodFunc = UnsafeNativeMethods.GetModuleHandleW(sFuncMod);
			if(Marshal.GetLastWin32Error() != 0)
				throw new InvalidOperationException(string.Format("Could not load the module {0}.", sFuncMod.QuoteIfNeeded()), new Win32Exception());
			if(hmodFunc == null)
				throw new InvalidOperationException(string.Format("Could not load the module {0}.", sFuncMod.QuoteIfNeeded()));

			void* pFunc = UnsafeNativeMethods.GetProcAddress(hmodFunc, sFuncName);
			if(Marshal.GetLastWin32Error() != 0)
				throw new InvalidOperationException(string.Format("Could not locate the {0} function in the {1} module.", sFuncName.QuoteIfNeeded(), sFuncMod.QuoteIfNeeded()), new Win32Exception());
			if(pFunc == null)
				throw new InvalidOperationException(string.Format("Could not locate the {0} function in the {1} module.", sFuncName.QuoteIfNeeded(), sFuncMod.QuoteIfNeeded()));

			uint ulSize;
			// Look for the imports section
			void* pImportDescVoid = UnsafeNativeMethods.ImageDirectoryEntryToData(hmodCaller, 1, UnsafeNativeMethods.IMAGE_DIRECTORY_ENTRY_IMPORT, out ulSize);
			if(Marshal.GetLastWin32Error() != 0)
				throw new InvalidOperationException(string.Format("Could not locate the import address table for the {0} module.", sCallingMod.QuoteIfNeeded()), new Win32Exception());
			if(pImportDescVoid == null)
				throw new InvalidOperationException(string.Format("Could not locate the import address table for the {0} module.", sCallingMod.QuoteIfNeeded()));

			// Find the entry for the function's module, look by its name
			var bytes = new List<byte>();
			var pImportDesc = (UnsafeNativeMethods.IMAGE_IMPORT_DESCRIPTOR*)pImportDescVoid;
			int nCount;
			for(nCount = 0; (pImportDesc->Name != 0) && (nCount < IterationLimit); pImportDesc++, nCount++)
			{
				byte* szModName = (byte*)hmodCaller + pImportDesc->Name; // RVA
				bytes.Clear();
				for(int a = 0; (a < 0x100) && (szModName[a] != 0); a++)
					bytes.Add(szModName[a]);
				string sModName = Encoding.Default.GetString(bytes.ToArray());
				if(string.Compare(sModName, sFuncMod, StringComparison.InvariantCultureIgnoreCase) == 0)
					break;
			}
			if(!((pImportDesc->Name != 0) && (nCount < IterationLimit))) // Gotten to the end
				throw new InvalidOperationException(string.Format("Could not find an entry for the {0} module in the import address table of the {1} module.", sFuncMod, sCallingMod));

			// Look for all the functions imported by the calling module from the function's module
			// Tell our Function apart by its address, as gotten from GetProcAddress
			var pThunk = (UnsafeNativeMethods.IMAGE_THUNK_DATA*)((byte*)hmodCaller + pImportDesc->FirstThunk); // RVA
			for(nCount = 0; (pThunk->Function != null) && (nCount < IterationLimit); pThunk++, nCount++)
			{
				void** ppfn = &pThunk->Function;
				if(*ppfn == pFunc)
				{
					var mbi = new UnsafeNativeMethods.MEMORY_BASIC_INFORMATION();
					IntPtr nBytesReturned = UnsafeNativeMethods.VirtualQuery(ppfn, ref mbi, (IntPtr)Marshal.SizeOf(typeof(UnsafeNativeMethods.MEMORY_BASIC_INFORMATION)));
					if((nBytesReturned == IntPtr.Zero) && (Marshal.GetLastWin32Error() != 0)) // Note: sometimes it would state "file not found" without any good reason
						throw new InvalidOperationException("Could not query for the memory protection information.", new Win32Exception());

					// Lift the memory protection
					if(UnsafeNativeMethods.VirtualProtect(mbi.BaseAddress, mbi.RegionSize, UnsafeNativeMethods.PAGE_READWRITE, out mbi.Protect) == 0)
						throw new InvalidOperationException(string.Format("Could not unlock import address table memory."));
					// Hold a reference to the delegate (otherwise the pointer we create will be lost after the delegate gets collected)
					DelegateAddRef(pNewFunction);

					// This is it
					*ppfn = (void*)Marshal.GetFunctionPointerForDelegate(pNewFunction);

					// Restore the protection
					uint dwOldProtect;
					UnsafeNativeMethods.VirtualProtect(mbi.BaseAddress, mbi.RegionSize, mbi.Protect, out dwOldProtect);
					break; // Done!
				}
			}
			if(!((pThunk->Function != null) && (nCount < IterationLimit))) // No such func (btw may so happen we've already hooked it)
				throw new InvalidOperationException(string.Format("Could not find the {0} function from the {1} module in the import address table of the {2} module.", sFuncName, sFuncMod, sCallingMod));
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Ensures the delegate would not be ever collected.
		/// </summary>
		private static void DelegateAddRef(Delegate function)
		{
			lock(myDelegateReferences)
				myDelegateReferences.Add(function);
		}

		#endregion

		#region UnsafeNativeMethods Type

		/// <summary>
		/// Personal declarations for the WinAPI calls.
		/// Not shared with WinAPI.Interop, as there're pointers where applicable instead of intptrs.
		/// Was written to be compatible with ANSI/Wide charsets and 32/64 bit systems.
		/// </summary>
		private static class UnsafeNativeMethods
		{
			#region Data

			public static readonly ushort IMAGE_DIRECTORY_ENTRY_IMPORT = 1; // Import Directory

			public static readonly uint PAGE_READWRITE = 0x04;

			#endregion

			#region Operations

			[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
			public static extern void* GetModuleHandleW([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

			[DllImport("Kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
			public static extern void* GetProcAddress(void* hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

			[DllImport("DbgHelp.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
			public static extern void* ImageDirectoryEntryToData(void* Base, byte MappedAsImage, UInt16 DirectoryEntry, out UInt32 Size);

			[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
			public static extern void* LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName);

			[DllImport("Kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
			public static extern Int32 VirtualProtect(void* lpAddress, IntPtr dwSize, UInt32 flNewProtect, out UInt32 lpflOldProtect);

			[DllImport("Kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
			public static extern IntPtr VirtualQuery(void* lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

			#endregion

			#region IMAGE_IMPORT_DESCRIPTOR Type

			[NoReorder]
			[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
			public struct IMAGE_IMPORT_DESCRIPTOR
			{
				//union {
				public UInt32 Characteristics; // 0 for terminating null import descriptor

				//    public UInt32   OriginalFirstThunk;         // RVA to original unbound IAT (PIMAGE_THUNK_DATA)
				//};
				public UInt32 TimeDateStamp; // 0 if not bound, -1 if bound, and real date\time stamp in IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (new BIND) O.W. date/time stamp of DLL bound to (Old BIND)

				public UInt32 ForwarderChain; // -1 if no forwarders

				public UInt32 Name;

				public UInt32 FirstThunk; // RVA to IAT (if bound this IAT has actual addresses)
			}

			#endregion

			#region IMAGE_THUNK_DATA Type

			/// <summary>
			/// It's DWORD in 32bit and ULONGLONG in 64bit, so use void* for both.
			/// Yes, there's only one field unioned in there.
			/// </summary>
			[NoReorder]
			[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
			public struct IMAGE_THUNK_DATA
			{
				//union {
				//void* ForwarderString;      // PBYTE
				public void* Function; // PDWORD

				//void* Ordinal;
				//void* AddressOfData;        // PIMAGE_IMPORT_BY_NAME
				//} u1;
			}

			#endregion

			#region MEMORY_BASIC_INFORMATION Type

			[NoReorder]
			[StructLayout(LayoutKind.Sequential)]
			public struct MEMORY_BASIC_INFORMATION
			{
				public void* BaseAddress;

				public void* AllocationBase;

				public UInt32 AllocationProtect;

				public IntPtr RegionSize;

				public UInt32 State;

				public UInt32 Protect;

				public UInt32 Type;
			}

			#endregion
		}

		#endregion
	}
}
