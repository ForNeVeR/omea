﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using JetBrains.Build.GuidCache;
using JetBrains.Build.InstallationData;
using JetBrains.Build.Omea.Infra;
using JetBrains.Build.Omea.Resolved.Infra;
using JetBrains.Omea.Base.Install;

using Microsoft.Build.Framework;
using Microsoft.Tools.WindowsInstallerXml.Serialize;

using Directory=Microsoft.Tools.WindowsInstallerXml.Serialize.Directory;
using File=Microsoft.Tools.WindowsInstallerXml.Serialize.File;

#pragma warning disable SuggestBaseTypeForParameter

namespace JetBrains.Build.Omea.Resolved.Tasks
{
	/// <summary>
	/// Creates a WiX source with the product Registry data included in it.
	/// </summary>
	public class WixInstallationDataResolved : TaskResolved
	{
		#region Data

		/// <summary>
		/// Component ID for the component that is autogenerated.
		/// </summary>
		public static readonly string ComponentIdPrefix = "C.ProductInstallationData";

		/// <summary>
		/// Directory ID for the directory that is autogenerated.
		/// </summary>
		public static readonly string DirectoryIdPrefix = "D.ProductInstallationData";

		/// <summary>
		/// File ID for the file that is autogenerated.
		/// </summary>
		public static readonly string FileIdPrefix = "F.ProductInstallationData";

		/// <summary>
		/// GUID database, loaded on execution.
		/// </summary>
		protected GuidCacheXml myGuidCache;

		/// <summary>
		/// Stores the components we've created for different Registry hives.
		/// </summary>
		protected readonly Dictionary<RegistryHiveXml, Component> myMapHiveToComponent = new Dictionary<RegistryHiveXml, Component>();

		#endregion

		#region Implementation

		/// <summary>
		/// Converts the hive into a guid cache ID.
		/// </summary>
		protected static GuidIdXml GetGuidId(RegistryHiveXml hive)
		{
			switch(hive)
			{
			case RegistryHiveXml.Hkcr:
				return GuidIdXml.MsiComponent_RegistryData_Hkcr;
			case RegistryHiveXml.Hklm:
				return GuidIdXml.MsiComponent_RegistryData_Hklm;
			case RegistryHiveXml.Hkcu:
				return GuidIdXml.MsiComponent_RegistryData_Hkcu;
			case RegistryHiveXml.Hkmu:
				return GuidIdXml.MsiComponent_RegistryData_Hkmu;
			default:
				throw new ArgumentOutOfRangeException("hive", hive, "Unknown Registry Hive value.");
			}
		}

		/// <summary>
		/// Defines the macros that could be used in the Registry dumps.
		/// They must be substituted before writing into the WiX tables.
		/// WiX variables or MSI macros could still be used.
		/// </summary>
		protected static IDictionary<string, string> GetMacros()
		{
			var macros = new Dictionary<string, string>();

			macros.Add(MacroNameXml.SystemDir.ToString(), "[SystemFolder]");
			macros.Add(MacroNameXml.ProductBinariesDir.ToString(), "[INSTALLDIR]\\Bin");
			macros.Add(MacroNameXml.DateTime.ToString(), "[Date]T[Time]");

			return macros;
		}

		/// <summary>
		/// Converts a RegistryData hive into a WiX Root.
		/// </summary>
		protected static RegistryRootType GetRoot(RegistryHiveXml hivexml)
		{
			switch(hivexml)
			{
			case RegistryHiveXml.Hkcr:
				return RegistryRootType.HKCR;
			case RegistryHiveXml.Hklm:
				return RegistryRootType.HKLM;
			case RegistryHiveXml.Hkcu:
				return RegistryRootType.HKCU;
			case RegistryHiveXml.Hkmu:
				return RegistryRootType.HKMU;
			default:
				throw new InvalidOperationException(string.Format("Unexpected RegistryData hive “{0}”.", hivexml));
			}
		}

		/// <summary>
		/// Converts a RegistryData value type into a WiX one.
		/// </summary>
		protected static RegistryValue.TypeType GetValueType(RegistryValueTypeXml type)
		{
			switch(type)
			{
			case RegistryValueTypeXml.Dword:
				return RegistryValue.TypeType.integer;
			case RegistryValueTypeXml.String:
				return RegistryValue.TypeType.@string;
			default:
				throw new InvalidOperationException(string.Format("Unknown RegistryData value type “{0}”.", type));
			}
		}

		/// <summary>
		/// Creates a new WiX Component for populating with the values of a specific hive, or reuses an existing one.
		/// Registry items from different hives must not be mixed within a single component.
		/// </summary>
		/// <param name="hive">Registry hive.</param>
		/// <param name="directory">A WiX directory to parent the newly-created component.</param>
		/// <param name="componentgroup">A group of components to register the newly-created component into.</param>
		protected Component GetComponentForHive(RegistryHiveXml hive, DirectoryRef directory, ComponentGroup componentgroup)
		{
			if(directory == null)
				throw new ArgumentNullException("directory");
			if(componentgroup == null)
				throw new ArgumentNullException("componentgroup");

			Component component;
			if(myMapHiveToComponent.TryGetValue(hive, out component)) // Lookup
				return component; // Reuse existing

			// Create a new one
			component = new Component();
			myMapHiveToComponent.Add(hive, component);
			directory.AddChild(component);
			component.Id = string.Format("{0}.{1}", ComponentIdPrefix, hive);

			// Chose a GUID for the component, assign
			component.Guid = myGuidCache[GetGuidId(hive)].ToString("B").ToUpperInvariant();

			// Register in the group
			var componentref = new ComponentRef();
			componentgroup.AddChild(componentref);
			componentref.Id = component.Id;

			return component;
		}

		/// <summary>
		/// Processes the installation files, produces the WiX data.
		/// </summary>
		private int ConvertFiles(DirectoryRef wixDirectoryRef, ComponentGroup wixComponentGroup, InstallationDataXml dataxml, IDictionary<string, string> macros)
		{
			int nProduced = 0;

			// Each installation folder derives a component, regardless of whether there are more files in the same folder, or not
			foreach(FolderXml folderxml in dataxml.Files)
			{
				folderxml.AssertValid();

				// Create the component with the files
				var wixComponent = new Component();
				wixComponent.Id = string.Format("{0}.{1}", ComponentIdPrefix, folderxml.Id);
				wixComponent.Guid = folderxml.MsiComponentGuid;
				wixComponent.DiskId = Bag.Get<int>(AttributeName.DiskId);
				wixComponent.Location = Component.LocationType.local;
				ConvertFiles_AddToDirectory(folderxml, wixComponent, wixDirectoryRef); // To the directory structure

				// Add to the feature
				var wixComponentRef = new ComponentRef();
				wixComponentRef.Id = wixComponent.Id;
				wixComponentGroup.AddChild(wixComponentRef); // To the feature

				var diSource = new DirectoryInfo(Path.Combine(LocalInstallDataResolved.ResolveSourceDirRoot(folderxml.SourceRoot, Bag), folderxml.SourceDir));
				if(!diSource.Exists)
					throw new InvalidOperationException(string.Format("The source folder “{0}” does not exist.", diSource.FullName));

				// Add files
				foreach(FileXml filexml in folderxml.Files)
				{
					filexml.AssertValid();

					FileInfo[] files = diSource.GetFiles(filexml.SourceName);
					if(files.Length == 0)
						throw new InvalidOperationException(string.Format("There are no files matching the “{0}” mask in the source folder “{1}”.", filexml.SourceName, diSource.FullName));
					if((files.Length > 1) && (filexml.TargetName.Length > 0))
						throw new InvalidOperationException(string.Format("There are {2} files matching the “{0}” mask in the source folder “{1}”, in which case it's illegal to specify a target name for the file.", filexml.SourceName, diSource.FullName, files.Length));

					foreach(FileInfo fiSource in files)
					{
						nProduced++;

						var wixFile = new File();
						wixComponent.AddChild(wixFile);
						wixFile.Id = string.Format("{0}.{1}.{2}", FileIdPrefix, folderxml.Id, fiSource.Name).Replace('-', '_').Replace(' ', '_');	// Replace chars that are not allowed in the ID
						wixFile.Name = filexml.TargetName.Length > 0 ? filexml.TargetName : fiSource.Name; // Explicit target name, if present and if a single file; otherwise, use from source
						wixFile.Checksum = YesNoType.yes;
						wixFile.ReadOnly = YesNoType.yes;
						wixFile.Source = fiSource.FullName;
					}
				}
			}

			return nProduced;
		}

		/// <summary>
		/// Creates a new or uses the root directory to add the newly-created component with files being installed.
		/// </summary>
		private void ConvertFiles_AddToDirectory(FolderXml folderxml, Component wixComponent, DirectoryRef wixDirectoryRef)
		{
			if(folderxml.TargetRoot != TargetRootXml.InstallDir)
				throw new InvalidOperationException(string.Format("Only the InstallDir target root is supported."));

			// No relative path — nothing to create
			if(folderxml.TargetDir.Length == 0)
			{
				wixDirectoryRef.AddChild(wixComponent);
				return;
			}

			// Create the folder structure, add to the innermost
			string[] arDirectoryChain = folderxml.TargetDir.Split('\\');
			Directory wixParentDir = null;
			for(int a = 0; a < arDirectoryChain.Length; a++)
			{
				bool bInnermost = a == arDirectoryChain.Length - 1; // Whether this is the folder in which are the files itself

				// Create
				var wixDirectory = new Directory {Name = arDirectoryChain[a]};

				// Mount self into the hierarchy
				if(wixParentDir != null)
					wixParentDir.AddChild(wixDirectory); // Previous dir
				else
					wixDirectoryRef.AddChild(wixDirectory); // The very root
				wixParentDir = wixDirectory;

				// Non-innermost folders get a suffix to their ID
				if(bInnermost)
					wixDirectory.Id = string.Format("{0}.{1}", DirectoryIdPrefix, folderxml.Id);
				else
					wixDirectory.Id = string.Format("{0}.{1}.P{2}", DirectoryIdPrefix, folderxml.Id, arDirectoryChain.Length - 1 - a);

				// Mount the component into the innermost dir
				if(bInnermost)
					wixDirectory.AddChild(wixComponent);
			}
		}

		/// <summary>
		/// Emits WiX Registry Keys from the installation data.
		/// </summary>
		private int ConvertRegistryKeys(DirectoryRef directory, ComponentGroup componentgroup, InstallationDataXml dataxml, IDictionary<string, string> macros)
		{
			foreach(RegistryKeyXml keyxml in dataxml.Registry.Key) // Keys
			{
				try
				{
					var key = new RegistryKey();
					GetComponentForHive(keyxml.Hive, directory, componentgroup).AddChild(key);
					key.Root = GetRoot(keyxml.Hive);
					key.Key = LocalInstaller.SubstituteMacros(macros, keyxml.Key);
					key.Action = RegistryKey.ActionType.createAndRemoveOnUninstall;
				}
				catch(Exception ex)
				{
					throw new InvalidOperationException(string.Format("Failed to process the key {0}. {1}", keyxml, ex.Message), ex);
				}
			}

			return dataxml.Registry.Key.Length;
		}

		/// <summary>
		/// Emits WiX Registry Values from the installation data.
		/// </summary>
		private int ConvertRegistryValues(DirectoryRef directory, ComponentGroup componentgroup, InstallationDataXml dataxml, IDictionary<string, string> macros)
		{
			foreach(RegistryValueXml valuexml in dataxml.Registry.Value)
			{
				try
				{
					var value = new RegistryValue();
					GetComponentForHive(valuexml.Hive, directory, componentgroup).AddChild(value);
					value.Root = GetRoot(valuexml.Hive);
					value.Key = LocalInstaller.SubstituteMacros(macros, valuexml.Key);
					if(!string.IsNullOrEmpty(valuexml.Name)) // The default value name must be Null not an empty string
						value.Name = LocalInstaller.SubstituteMacros(macros, valuexml.Name);
					value.Value = LocalInstaller.SubstituteMacros(macros, valuexml.Value);
					value.Type = GetValueType(valuexml.Type);

					value.Action = RegistryValue.ActionType.write;
				}
				catch(Exception ex)
				{
					throw new InvalidOperationException(string.Format("Failed to process the value {0}. {1}", valuexml, ex.Message), ex);
				}
			}

			return dataxml.Registry.Value.Length;
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Actions under the resolver.
		/// </summary>
		protected override void ExecuteTaskResolved()
		{
			// Prepare the GUID cache
			myGuidCache = GuidCacheXml.Load(new FileInfo(Bag.GetString(AttributeName.GuidCacheFile)).OpenRead());

			// Global structure of the WiX fragment file
			var wix = new Wix();
			var wixFragmentComponents = new Fragment(); // Fragment with the payload
			wix.AddChild(wixFragmentComponents);
			var wixDirectoryRef = new DirectoryRef(); // Mount into the directories tree, defined externally
			wixFragmentComponents.AddChild(wixDirectoryRef);
			wixDirectoryRef.Id = Bag.GetString(AttributeName.WixDirectoryId);

			var wixFragmentGroup = new Fragment(); // Fragment with the component-group that collects the components
			wix.AddChild(wixFragmentGroup);
			var wixComponentGroup = new ComponentGroup(); // ComponentGroup that collects the components
			wixFragmentGroup.AddChild(wixComponentGroup);
			wixComponentGroup.Id = Bag.GetString(AttributeName.WixComponentGroupId);

			// Get the dump from the product
			InstallationDataXml dataxml = CreateInstaller().HarvestInstallationData();
			IDictionary<string, string> macros = GetMacros();

			// Nullref guards
			dataxml.AssertValid();

			// Convert into WiX
			int nProducedFiles = ConvertFiles(wixDirectoryRef, wixComponentGroup, dataxml, macros);
			int nProducedKeys = ConvertRegistryKeys(wixDirectoryRef, wixComponentGroup, dataxml, macros);
			int nProducedValues = ConvertRegistryValues(wixDirectoryRef, wixComponentGroup, dataxml, macros);

			// Save to the output file
			using(var xw = new XmlTextWriter(new FileStream(Bag.GetString(AttributeName.OutputFile), FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8))
			{
				xw.Formatting = Formatting.Indented;
				wix.OutputXml(xw);
			}

			// Report (also to see the target in the build logs)
			Log.LogMessage(MessageImportance.Normal, "Generated {0} files, {1} Registry keys, and {2} Registry values.", nProducedFiles, nProducedKeys, nProducedValues);
		}

		#endregion
	}
}
