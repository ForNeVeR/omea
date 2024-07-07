// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using JetBrains.Annotations;
using JetBrains.Build.InstallationData;

using Microsoft.Win32;

namespace JetBrains.Omea.Base.Install
{
	/// <summary>
	/// Handles the Registry entries by writing them into the Windows Registry.
	/// </summary>
	public class LocalInstaller
	{
		#region Data

		/// <summary>
		/// Caches the macro substitution regex.
		/// </summary>
		private static Regex myRegexMacro;

		#endregion

		#region Operations

		/// <summary>
		/// Performs the local installation of the given installation data by writing the Registry keys and copying the files.
		/// </summary>
		/// <param name="dataxml">The installation data.</param>
		/// <param name="stage">Stage, either install or uninstall.</param>
		/// <param name="LogMessage">The logging facility.</param>
		/// <param name="ResolveSourceDirRoot">Resolves the source directory, for copying the files from.</param>
		/// <param name="ResolveTargetDirRoot">Resolves the target directory, for copying the files into.</param>
		/// <param name="macros">The maros to be substituted on install, if needed.</param>
		public static void Install(InstallationDataXml dataxml, RegistrationStage stage, IDictionary<string, string> macros, Func<SourceRootXml, DirectoryInfo> ResolveSourceDirRoot, Func<TargetRootXml, DirectoryInfo> ResolveTargetDirRoot, Action<string> LogMessage)
		{
			InstallRegistry(dataxml.Registry, stage, macros);
			InstallFiles(dataxml, stage, LogMessage, ResolveSourceDirRoot, ResolveTargetDirRoot);
		}

		/// <summary>
		/// Substs the macros in a string, throws if there are undefined macros.
		/// </summary>
		[NotNull]
		public static string SubstituteMacros([NotNull] IDictionary<string, string> macros, [NotNull] string sample)
		{
			if(macros == null)
				throw new ArgumentNullException("macros");
			if(sample == null)
				throw new ArgumentNullException("sample");

			string retval = sample;
			foreach(var pair in macros)
				retval = retval.Replace(string.Format("$({0})", pair.Key), pair.Value);

			// Are there any undefined macros?
			if(myRegexMacro == null)
				myRegexMacro = new Regex(@"\$\((?<MacroName>[^)]*)\)", RegexOptions.Compiled);
			MatchCollection matches = myRegexMacro.Matches(retval);
			if(matches.Count != 0)
			{
				var sw = new StringWriter();
				sw.Write("Error: undefined macros ");
				foreach(Match match in matches)
					sw.Write("“{0}” ", match.Groups["MacroName"].Value);
				sw.Write("in “{0}”.", sample);
				throw new InvalidOperationException(sw.ToString());
			}
			return retval;
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Gets the proper Windows Registry root key.
		/// </summary>
		private static RegistryKey GetWindowsRegistryRootKey(RegistryHiveXml hive)
		{
			switch(hive)
			{
			case RegistryHiveXml.Hkcr:
				return Registry.ClassesRoot;
			case RegistryHiveXml.Hklm:
				return Registry.LocalMachine;
			case RegistryHiveXml.Hkcu:
				return Registry.CurrentUser;
			case RegistryHiveXml.Hkmu:
				return Registry.LocalMachine; // TODO: differentiate per-machine and per-user, if ever needed
			default:
				throw new ArgumentOutOfRangeException("hive", hive, "Unexpected Registry hive.");
			}
		}

		/// <summary>
		/// Copies or deletes the files.
		/// </summary>
		private static void InstallFiles(InstallationDataXml dataxml, RegistrationStage stage, Action<string> LogMessage, Func<SourceRootXml, DirectoryInfo> ResolveSourceDirRoot, Func<TargetRootXml, DirectoryInfo> ResolveTargetDirRoot)
		{
			dataxml.AssertValid();

			foreach(FolderXml folderxml in dataxml.Files)
			{
				var diSource = new DirectoryInfo(Path.Combine(ResolveSourceDirRoot(folderxml.SourceRoot).FullName, folderxml.SourceDir));
				var diTarget = new DirectoryInfo(Path.Combine(ResolveTargetDirRoot(folderxml.TargetRoot).FullName, folderxml.TargetDir));
				diTarget.Create();

				foreach(FileXml filexml in folderxml.Files)
				{
					FileInfo[] files = diSource.GetFiles(filexml.SourceName);
					if(files.Length == 0)
						throw new InvalidOperationException(string.Format("There are no files matching the “{0}” mask in the source folder “{1}”.", filexml.SourceName, diSource.FullName));
					if((files.Length > 1) && (filexml.TargetName.Length > 0))
						throw new InvalidOperationException(string.Format("There are {2} files matching the “{0}” mask in the source folder “{1}”, in which case it's illegal to specify a target name for the file.", filexml.SourceName, diSource.FullName, files.Length));

					foreach(FileInfo fiSource in files)
					{
						var fiTarget = new FileInfo(Path.Combine(diTarget.FullName, (filexml.TargetName.Length > 0 ? filexml.TargetName : fiSource.Name))); // Explicit target name, if present and if a single file; otherwise, use from source

						// Don't copy and, especially, don't delete the inplace files
						if(fiSource.FullName == fiTarget.FullName)
						{
							LogMessage(string.Format("Skipping “{0}” because source and target are the same file.", fiSource.FullName, fiTarget.FullName));
							continue;
						}

						switch(stage)
						{
						case RegistrationStage.Register:
							LogMessage(string.Format("Installing “{0}” -> “{1}”.", fiSource.FullName, fiTarget.FullName));
							fiSource.CopyTo(fiTarget.FullName, true);
							break;
						case RegistrationStage.Unregister:
							LogMessage(string.Format("Uninstalling “{0}”.", fiTarget.FullName));
							fiTarget.Delete();
							break;
						default:
							throw new InvalidOperationException(string.Format("Unexpected stage {0}.", stage));
						}
					}
				}
			}
		}

		/// <summary>
		/// Executes the registration/unregistration operations on the Registry keys.
		/// </summary>
		/// <param name="registry">The Registry to process.</param>
		/// <param name="registrationStage">Processing type.</param>
		/// <param name="macros">The macros to substitute when processing the keys.</param>
		private static void InstallRegistry(RegistryXml registry, RegistrationStage registrationStage, IDictionary<string, string> macros)
		{
			if(registry == null)
				throw new ArgumentNullException("registry");
			if(macros == null)
				throw new ArgumentNullException("macros");
			registry.AssertValid();

			switch(registrationStage)
			{
			case RegistrationStage.Register:
				foreach(RegistryKeyXml key in registry.Key)
					RegisterKey(key, macros);
				foreach(RegistryValueXml value in registry.Value)
					RegisterValue(value, macros);
				break;
			case RegistrationStage.Unregister:
				for(int a = registry.Value.Length - 1; a >= 0; a--) // Reverse order on uninstallation
					UnregisterValue(registry.Value[a], macros);
				for(int a = registry.Key.Length - 1; a >= 0; a--)
					UnregisterKey(registry.Key[a], macros);
				break;
			default:
				throw new ArgumentOutOfRangeException("registrationStage", registrationStage, "Unexpected registration stage.");
			}
		}

		/// <summary>
		/// Writes the key to the Registry.
		/// </summary>
		private static void RegisterKey([NotNull] RegistryKeyXml key, [NotNull] IDictionary<string, string> macros)
		{
			if(key == null)
				throw new ArgumentNullException("key");
			if(macros == null)
				throw new ArgumentNullException("macros");
			try
			{
				string sPath = SubstituteMacros(macros, key.Key);
				RegistryKey regkeyRoot = GetWindowsRegistryRootKey(key.Hive);

				using(regkeyRoot.CreateSubKey(sPath))
				{
				}
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException(string.Format("Failed to process the key {0}. {1}", key, ex.Message), ex);
			}
		}

		/// <summary>
		/// Writes the value to the Registry.
		/// </summary>
		private static void RegisterValue([NotNull] RegistryValueXml value, [NotNull] IDictionary<string, string> macros)
		{
			if(value == null)
				throw new ArgumentNullException("value");
			if(macros == null)
				throw new ArgumentNullException("macros");
			try
			{
				string sPath = SubstituteMacros(macros, value.Key);
				RegistryKey regkeyRoot = GetWindowsRegistryRootKey(value.Hive);

				RegistryKey regkey = regkeyRoot.OpenSubKey(sPath, true);
				if(regkey == null)
					regkey = regkeyRoot.CreateSubKey(sPath); // TODO: check if this works when more than one level is missing
				using(regkey)
				{
					string sName = SubstituteMacros(macros, value.Name);
					object oValue;
					switch(value.Type)
					{
					case RegistryValueTypeXml.Dword:
						oValue = unchecked((int)(uint)long.Parse(value.Value)); // Ensure it qualifies as an Int32 for the DWORD registry value
						break;
					case RegistryValueTypeXml.String:
						oValue = SubstituteMacros(macros, value.Value);
						break;
					default:
						throw new InvalidOperationException(string.Format("The registry value type “{0}” is unknown.", value.Type));
					}
					regkey.SetValue(sName, oValue);
				}
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException(string.Format("Failed to process the value {0}. {1}", value, ex.Message), ex);
			}
		}

		/// <summary>
		/// Deletes the key from the Registry.
		/// </summary>
		private static void UnregisterKey([NotNull] RegistryKeyXml key, [NotNull] IDictionary<string, string> macros)
		{
			if(key == null)
				throw new ArgumentNullException("key");
			if(macros == null)
				throw new ArgumentNullException("macros");
			try
			{
				string sKeyPath = SubstituteMacros(macros, key.Key);
				RegistryKey regkeyRoot = GetWindowsRegistryRootKey(key.Hive);

				using(RegistryKey regkey = regkeyRoot.OpenSubKey(sKeyPath, false))
				{
					if(regkey == null)
						return; // No key, nothing to delete
				}
				regkeyRoot.DeleteSubKeyTree(sKeyPath); // Must do a check because this function will throw if there is no key
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException(string.Format("Failed to process the key {0}. {1}", key, ex.Message), ex);
			}
		}

		/// <summary>
		/// Deletes the value from the Registry.
		/// </summary>
		private static void UnregisterValue([NotNull] RegistryValueXml value, [NotNull] IDictionary<string, string> macros)
		{
			if(value == null)
				throw new ArgumentNullException("value");
			if(macros == null)
				throw new ArgumentNullException("macros");
			try
			{
				string sKeyPath = SubstituteMacros(macros, value.Key);
				RegistryKey regkeyRoot = GetWindowsRegistryRootKey(value.Hive);

				using(RegistryKey regkey = regkeyRoot.OpenSubKey(sKeyPath, true))
				{
					if(regkey == null)
						return; // No key, no value to delete

					string sName = SubstituteMacros(macros, value.Name);
					regkey.DeleteValue(sName, false);
				}
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException(string.Format("Failed to process the value {0}. {1}", value, ex.Message), ex);
			}
		}

		#endregion
	}
}
