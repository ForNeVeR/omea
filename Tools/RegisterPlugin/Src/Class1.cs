// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using Microsoft.Win32;
using System.Reflection;
using System.Globalization;

namespace RegisterPlugin
{
	/// <summary>
	/// RegisterPlugin main class.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			// Parse args
			if(args.Length == 0)
			{
				Console.WriteLine("Usage: RegisterPlugin <plugin DLL>|<directory with plugins> [-full]");
				Console.WriteLine("-full     Checks all the DLLs in the given folder ");
				Console.WriteLine("          for classes implementing the Omea's IPlugin interface");
				return;
			}

			if((args.Length >= 2) && ((string.Compare(args[1], "-full", true, CultureInfo.InvariantCulture) == 0) || (string.Compare(args[1], "/full", true, CultureInfo.InvariantCulture) == 0)))
				bFullMode = true;

			// Create registry keys
			if(bFullMode)
				CreateRegistryKeys();

			// Register either all files in folder, or the given file
			if(Directory.Exists(args[0]))
			{
				foreach(string fileName in Directory.GetFiles(args[0], "*.dll"))
				{
					//string dllName = Path.Combine( Path.GetFullPath( args [0] ), fileName );
					CheckRegisterDll(fileName);
				}
			}
			else
			{
				CheckRegisterDll(args[0]);
			}
		}

		/// <summary>
		/// Executes the same registration process for both registry keys (Omea/OmeaReader).
		/// </summary>
		/// <param name="dllName"></param>
		private static void CheckRegisterDll(string dllName)
		{
			CheckRegisterPlugin(_keyOmea, dllName);
			CheckRegisterPlugin(_keyReader, dllName);
		}

		public const string _sOmeaKey = @"Software\JetBrains\Omea\Plugins";
		public const string _sOmeaReaderKey = @"Software\JetBrains\Omea Reader\Plugins";

		private static RegistryKey _keyOmea = Registry.CurrentUser.OpenSubKey(_sOmeaKey, true);
		private static RegistryKey _keyReader = Registry.CurrentUser.OpenSubKey(_sOmeaReaderKey, true);

		/// <summary>
		/// Whether the registrar tries all the DLLs in folder.
		/// </summary>
		private static bool bFullMode = false;

		private static void CheckRegisterPlugin(RegistryKey key, string dllName)
		{
			if(key == null)
				return;

			// Search for an existing registration for this plugin
			foreach(string valueName in key.GetValueNames())
			{
				string value = (string)key.GetValue(valueName);
				if(String.Compare(Path.GetFileName(value), Path.GetFileName(dllName), true) == 0)
				{
					key.SetValue(valueName, Path.GetFullPath(dllName));
					Console.WriteLine("{0} \t- registration was updated.", Path.GetFileName(dllName));
					return;
				}
			}

			if(bFullMode)
			{
				// If not found, check if this dll should be registered as a plugin
				try
				{
					Assembly assembly = Assembly.LoadFile(Path.GetFullPath(dllName));
					bool plugin = false;
					foreach(Type type in assembly.GetTypes())
					{
						if(type.GetInterface("JetBrains.Omea.OpenAPI.IPlugin", false) != null)
						{
							plugin = true;
							break;
						}
					}
					// Detected as a plugin?
					if(plugin)
					{
						key.SetValue(Path.GetFileName(dllName), Path.GetFullPath(dllName));
						Console.WriteLine("{0} \t- was detected and registered as a plugin.", Path.GetFileName(dllName));
					}
				}
                catch(ReflectionTypeLoadException ex)
                {
                    Console.WriteLine(String.Format("Failed to load assembly \"{0}\". Loader exceptions:", dllName));
                    foreach(Exception lex in ex.LoaderExceptions)
                    {
                        Console.WriteLine(lex);
                    }
                }
				catch(Exception ex)
				{
					Console.WriteLine(String.Format("Warning: could not load assembly \"{0}\". {1}", dllName, ex.Message));
				}
			}
		}

		/// <summary>
		/// Creates the registry keys if they're missing. This is needed at the first run on a clean machine.
		/// </summary>
		public static void CreateRegistryKeys()
		{
			if(_keyOmea == null)
			{
				Console.WriteLine("Omea Plugins Registry Key missing, creating one as “HKCU\\{0}”.", _sOmeaKey);
				_keyOmea = Registry.CurrentUser.CreateSubKey(_sOmeaKey);
			}
			if(_keyReader == null)
			{
				Console.WriteLine("Omea Reader Plugins Registry Key missing, creating one as “HKCU\\{0}”.", _sOmeaReaderKey);
				_keyReader = Registry.CurrentUser.CreateSubKey(_sOmeaReaderKey);
			}
		}
	}
}
