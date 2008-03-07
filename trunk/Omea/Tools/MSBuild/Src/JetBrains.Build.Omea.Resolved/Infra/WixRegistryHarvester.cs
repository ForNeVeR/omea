/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Tools.WindowsInstallerXml.Serialize;

using Registry=Microsoft.Win32.Registry;
using RegistryKey=Microsoft.Win32.RegistryKey;

namespace JetBrains.Build.Omea.Resolved.Infra
{
	/// <summary>
	/// Harvest WiX authoring from the registry.
	/// </summary>
	public sealed class WixRegistryHarvester : IDisposable
	{
		#region Data

		private readonly string remappedPath;

		#endregion

		#region Init

		/// <summary>
		/// Instantiate a new RegistryHarvester.
		/// </summary>
		/// <param name="remap">Set to true to remap the entire registry to a private location for this process.</param>
		public WixRegistryHarvester(bool remap)
		{
			// create a path in the registry for redirected keys which is process-specific
			if(remap)
			{
				remappedPath = String.Concat(@"SOFTWARE\WiX\heat\", Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));

				// remove the previous remapped key if it exists
				RemoveRemappedKey();

				// remap the registry roots supported by MSI (the order in which the roots are remapped is important)
				RemapRegistryKey(NativeMethods.HkeyClassesRoot, String.Concat(remappedPath, @"\\HKEY_CLASSES_ROOT"));
				RemapRegistryKey(NativeMethods.HkeyCurrentUser, String.Concat(remappedPath, @"\\HKEY_CURRENT_USER"));
				RemapRegistryKey(NativeMethods.HkeyUsers, String.Concat(remappedPath, @"\\HKEY_USERS"));
				RemapRegistryKey(NativeMethods.HkeyLocalMachine, String.Concat(remappedPath, @"\\HKEY_LOCAL_MACHINE"));
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Harvest all registry roots supported by Windows Installer.
		/// </summary>
		/// <returns>The registry keys and values in the registry.</returns>
		public static RegistryValue[] HarvestRegistry()
		{
			var registryValues = new ArrayList();

			HarvestRegistryKey(Registry.ClassesRoot, registryValues);
			HarvestRegistryKey(Registry.CurrentUser, registryValues);
			HarvestRegistryKey(Registry.LocalMachine, registryValues);
			HarvestRegistryKey(Registry.Users, registryValues);

			return (RegistryValue[])registryValues.ToArray(typeof(RegistryValue));
		}

		/// <summary>
		/// Close the RegistryHarvester and remove any remapped registry keys.
		/// </summary>
		public void Close()
		{
			NativeMethods.OverrideRegistryKey(NativeMethods.HkeyClassesRoot, IntPtr.Zero);
			NativeMethods.OverrideRegistryKey(NativeMethods.HkeyCurrentUser, IntPtr.Zero);
			NativeMethods.OverrideRegistryKey(NativeMethods.HkeyLocalMachine, IntPtr.Zero);
			NativeMethods.OverrideRegistryKey(NativeMethods.HkeyUsers, IntPtr.Zero);

			RemoveRemappedKey();
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Gets the parts of a registry key's path.
		/// </summary>
		/// <param name="path">The registry key path.</param>
		/// <returns>The root and key parts of the registry key path.</returns>
		private static string[] GetPathParts(string path)
		{
			return path.Split(@"\".ToCharArray(), 2);
		}

		/// <summary>
		/// Harvest a registry key.
		/// </summary>
		/// <param name="registryKey">The registry key to harvest.</param>
		/// <param name="registryValues">The collected registry values.</param>
		private static void HarvestRegistryKey(RegistryKey registryKey, ArrayList registryValues)
		{
			// harvest the sub-keys
			foreach(string subKeyName in registryKey.GetSubKeyNames())
			{
				using(RegistryKey subKey = registryKey.OpenSubKey(subKeyName))
					HarvestRegistryKey(subKey, registryValues);
			}

			string[] parts = GetPathParts(registryKey.Name);

			RegistryRootType root;
			switch(parts[0])
			{
			case "HKEY_CLASSES_ROOT":
				root = RegistryRootType.HKCR;
				break;
			case "HKEY_CURRENT_USER":
				root = RegistryRootType.HKCU;
				break;
			case "HKEY_LOCAL_MACHINE":
				root = RegistryRootType.HKLM;
				break;
			case "HKEY_USERS":
				root = RegistryRootType.HKU;
				break;
			default:
				throw new InvalidOperationException(string.Format("Unexpected root."));
			}

			// harvest the values
			foreach(string valueName in registryKey.GetValueNames())
			{
				var registryValue = new RegistryValue();

				registryValue.Action = RegistryValue.ActionType.write;

				registryValue.Root = root;

				if(1 < parts.Length)
					registryValue.Key = parts[1];

				if(null != valueName && 0 < valueName.Length)
					registryValue.Name = valueName;

				object value = registryKey.GetValue(valueName);

				if(value == null)
					throw new InvalidOperationException(string.Format("Value is Null."));

				if(value is byte[]) // binary
				{
					var hexadecimalValue = new StringBuilder();

					// convert the byte array to hexadecimal
					foreach(byte byteValue in (byte[])value)
						hexadecimalValue.Append(byteValue.ToString("X2", CultureInfo.InvariantCulture.NumberFormat));

					registryValue.Type = RegistryValue.TypeType.binary;
					registryValue.Value = hexadecimalValue.ToString();
				}
				else if(value is int) // integer
				{
					registryValue.Type = RegistryValue.TypeType.integer;
					registryValue.Value = ((int)value).ToString(CultureInfo.InvariantCulture);
				}
				else if(value is string[]) // multi-string
				{
					registryValue.Type = RegistryValue.TypeType.multiString;

					foreach(string multiStringValueContent in (string[])value)
					{
						var multiStringValue = new MultiStringValue();

						multiStringValue.Content = multiStringValueContent;

						registryValue.AddChild(multiStringValue);
					}
				}
				else if(value is string) // string, expandable (there is no way to differentiate a string and expandable value in .NET 1.1)
				{
					registryValue.Type = RegistryValue.TypeType.@string;
					registryValue.Value = (string)value;
				}
				else
					throw new InvalidOperationException(string.Format("Value is {0}.", value.GetType().AssemblyQualifiedName));

				registryValues.Add(registryValue);
			}
		}

		/// <summary>
		/// Remap a registry key to an alternative location.
		/// </summary>
		/// <param name="registryKey">The registry key to remap.</param>
		/// <param name="remappedPath">The path to remap the registry key to under HKLM.</param>
		private static void RemapRegistryKey(UIntPtr registryKey, string remappedPath)
		{
			IntPtr remappedKey = IntPtr.Zero;

			try
			{
				remappedKey = NativeMethods.OpenRegistryKey(NativeMethods.HkeyCurrentUser, remappedPath);

				NativeMethods.OverrideRegistryKey(registryKey, remappedKey);
			}
			finally
			{
				if(IntPtr.Zero != remappedKey)
					NativeMethods.CloseRegistryKey(remappedKey);
			}
		}

		/// <summary>
		/// Remove the remapped registry key.
		/// </summary>
		private void RemoveRemappedKey()
		{
			try
			{
				Registry.LocalMachine.DeleteSubKeyTree(remappedPath);
			}
			catch(ArgumentException)
			{
				// ignore the error where the key does not exist
			}
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Dispose the RegistryHarvester.
		/// </summary>
		public void Dispose()
		{
			Close();
		}

		#endregion

		#region NativeMethods Type

		/// <summary>
		/// The native methods for re-mapping registry keys.
		/// </summary>
		private sealed class NativeMethods
		{
			#region Data

			private const uint GenericAll = 0x10000000;

			private const uint GenericExecute = 0x20000000;

			private const uint GenericRead = 0x80000000;

			private const uint GenericWrite = 0x40000000;

			private const uint StandardRightsAll = 0x001F0000;

			internal static readonly UIntPtr HkeyClassesRoot = (UIntPtr)0x80000000;

			internal static readonly UIntPtr HkeyCurrentUser = (UIntPtr)0x80000001;

			internal static readonly UIntPtr HkeyLocalMachine = (UIntPtr)0x80000002;

			internal static readonly UIntPtr HkeyUsers = (UIntPtr)0x80000003;

			#endregion

			#region Implementation

			/// <summary>
			/// Closes a previously open registry key.
			/// </summary>
			/// <param name="key">Handle to key to close.</param>
			internal static void CloseRegistryKey(IntPtr key)
			{
				if(0 != RegCloseKey(key))
					throw new Exception();
			}

			/// <summary>
			/// Opens a registry key.
			/// </summary>
			/// <param name="key">Base key to open.</param>
			/// <param name="path">Path to subkey to open.</param>
			/// <returns>Handle to new key.</returns>
			internal static IntPtr OpenRegistryKey(UIntPtr key, string path)
			{
				IntPtr newKey;
				uint disposition;
				uint sam = StandardRightsAll | GenericRead | GenericWrite | GenericExecute | GenericAll;

				if(0 != RegCreateKeyEx(key, path, 0, null, 0, sam, 0, out newKey, out disposition))
					throw new Exception();

				return newKey;
			}

			/// <summary>
			/// Override a registry key.
			/// </summary>
			/// <param name="key">Handle of the key to override.</param>
			/// <param name="newKey">Handle to override key.</param>
			internal static void OverrideRegistryKey(UIntPtr key, IntPtr newKey)
			{
				if(0 != RegOverridePredefKey(key, newKey))
					throw new Exception();
			}

			/// <summary>
			/// Interop to RegCloseKey.
			/// </summary>
			/// <param name="key">Handle to key to close.</param>
			/// <returns>0 if success.</returns>
			[DllImport("advapi32.dll", EntryPoint = "RegCloseKey", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
			private static extern int RegCloseKey(IntPtr key);

			/// <summary>
			/// Interop to RegCreateKeyW.
			/// </summary>
			/// <param name="key">Handle to base key.</param>
			/// <param name="subkey">Subkey to create.</param>
			/// <param name="reserved">Always 0</param>
			/// <param name="className">Just pass null.</param>
			/// <param name="options">Just pass 0.</param>
			/// <param name="desiredSam">Rights to registry key.</param>
			/// <param name="securityAttributes">Just pass null.</param>
			/// <param name="openedKey">Opened key.</param>
			/// <param name="disposition">Whether key was opened or created.</param>
			/// <returns>Handle to registry key.</returns>
			[DllImport("advapi32.dll", EntryPoint = "RegCreateKeyExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
			private static extern int RegCreateKeyEx(UIntPtr key, string subkey, uint reserved, string className, uint options, uint desiredSam, uint securityAttributes, out IntPtr openedKey, out uint disposition);

			/// <summary>
			/// Interop to RegOverridePredefKey.
			/// </summary>
			/// <param name="key">Handle to key to override.</param>
			/// <param name="newKey">Handle to override key.</param>
			/// <returns>0 if success.</returns>
			[DllImport("advapi32.dll", EntryPoint = "RegOverridePredefKey", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
			private static extern int RegOverridePredefKey(UIntPtr key, IntPtr newKey);

			#endregion
		}

		#endregion
	}
}