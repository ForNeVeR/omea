// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Microsoft.Tools.WindowsInstallerXml.Serialize;

using File=Microsoft.Tools.WindowsInstallerXml.Serialize.File;

namespace JetBrains.Build.Omea.Resolved.Infra
{
	/// <summary>
	/// Harvests for Registry data of an assembly, typically that for the COM registration, and produces the WiX Registry entries.
	/// </summary>
	public static class SelfRegHarvester
	{
		#region Data

		private static readonly bool OptionUnmanagedUseResources = true;

		#endregion

		#region Operations

		public static void Harvest(FileInfo fi, bool managed, Component wixComponent, File wixFile)
		{
			if(managed)
				InvokeWixRedirector(fi, wixFile, wixComponent, delegate { InvokeManagedSelfReg(fi); });
			else
			{
				if(!OptionUnmanagedUseResources)
					InvokeWixRedirector(fi, wixFile, wixComponent, delegate { InvokeComSelfReg(fi); });
				else
					NativeSelfRegResourceExtractor.ExtractWxsResource(fi, wixComponent);
			}
		}

		#endregion

		#region Implementation

		private static Regex CreateResolveFileRegex(FileInfo fi)
		{
			return new Regex(string.Format(@"(file\:/*)?({0}|{1})", Regex.Escape(fi.FullName), Regex.Escape(fi.FullName.Replace('\\', '/'))), RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase);
		}

		private static void InvokeComSelfReg(FileInfo fi)
		{
			using(var pinvoke = new DynamicPinvoke(fi, "DllRegisterServer", typeof(int)))
				pinvoke.Invoke();
		}

		private static void InvokeManagedSelfReg(FileInfo fi)
		{
			Assembly assembly = Assembly.LoadFrom(fi.FullName);

			// WiX says this helps to prevent binding failures
			assembly.GetExportedTypes();

			new RegistrationServices().RegisterAssembly(assembly, AssemblyRegistrationFlags.SetCodeBase);
		}

		private static void InvokeWixRedirector(FileInfo fi, File wixFile, Component wixComponent, Action onInvokeSelfReg)
		{
			RegistryValue[] values;
			using(new WixRegistryHarvester(true))
			{
				onInvokeSelfReg();
				values = WixRegistryHarvester.HarvestRegistry();
			}

			// Proper roots, file refs
			TranslateValues(values, wixFile, fi);

			// Mount
			foreach(RegistryValue value in values)
				wixComponent.AddChild(value);
		}

		/// <summary>
		/// Takes the values, searches them for the file paths and replaces them with references to the file ID.
		/// </summary>
		private static void TranslateValues(RegistryValue[] values, File wixFile, FileInfo fi)
		{
			Regex regexPath = CreateResolveFileRegex(fi);

			string sFileReference = string.Format("[#{0}]", wixFile.Id);

			foreach(RegistryValue value in values)
			{
				// Use HKMU instead of HKLM for COM registration
				if(value.Root == RegistryRootType.HKLM)
					value.Root = RegistryRootType.HKMU;

				// Use HLMU\Software\Classes instead of HKCR
				if(value.Root == RegistryRootType.HKCR)
				{
					value.Root = RegistryRootType.HKMU;
					value.Key = "Software\\Classes\\" + value.Key;
				}

				// See for the file references
				if(value.Name != null)
					value.Name = regexPath.Replace(value.Name, sFileReference);
				if(value.Value != null)
					value.Value = regexPath.Replace(value.Value, sFileReference);
			}
		}

		#endregion

		#region Action Type

		private delegate void Action();

		#endregion
	}
}
