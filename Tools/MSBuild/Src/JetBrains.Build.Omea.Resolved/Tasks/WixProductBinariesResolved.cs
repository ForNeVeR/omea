// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using JetBrains.Build.AllAssemblies;
using JetBrains.Build.GuidCache;
using JetBrains.Build.Omea.Infra;
using JetBrains.Build.Omea.Resolved.Infra;
using JetBrains.Build.Omea.Util;

using Microsoft.Build.Framework;
using Microsoft.Tools.WindowsInstallerXml.Serialize;

using AssemblyName=System.Reflection.AssemblyName;
using Directory=Microsoft.Tools.WindowsInstallerXml.Serialize.Directory;
using File=Microsoft.Tools.WindowsInstallerXml.Serialize.File;

namespace JetBrains.Build.Omea.Resolved.Tasks
{
	/// <summary>
	/// Creates a WiX source with the product biaries described in it.
	/// </summary>
	public class WixProductBinariesResolved : TaskResolved
	{
		#region Data

		/// <summary>
		/// Prefix for the generated WiX element ID.
		/// </summary>
		public static readonly string DirectoryId = "D.ProductBinaries";

		/// <summary>
		/// Prefix for the generated WiX element ID.
		/// </summary>
		public static readonly string FileComponentIdPrefix = "C.ProductBinaries";

		/// <summary>
		/// Prefix for the generated WiX element ID.
		/// </summary>
		public static readonly string FileIdPrefix = "F.ProductBinaries";

		public static readonly string PluginsRegistryKey = "Software\\JetBrains\\Omea\\Plugins";

		/// <summary>
		/// Prefix for the generated WiX element ID.
		/// </summary>
		public static readonly string RegistryComponentIdPrefix = "C.ProductBinariesRegistry";

		/// <summary>
		/// Prefix for the generated WiX element ID.
		/// </summary>
		public static readonly string RegistryValueIdPrefix = "R.ProductBinaries";

		#endregion

		#region Implementation

		/// <summary>
		/// Create the Registry key for the Plugins section.
		/// </summary>
		private static void CreatePluginsRegistryKey(Component wixComponentRegistry)
		{
			var wixRegKeyPlugins = new RegistryKey();
			wixComponentRegistry.AddChild(wixRegKeyPlugins);
			wixRegKeyPlugins.Id = string.Format("{0}.PluginsKey", RegistryValueIdPrefix);
			wixRegKeyPlugins.Action = RegistryKey.ActionType.createAndRemoveOnUninstall;
			wixRegKeyPlugins.Root = RegistryRootType.HKMU;
			wixRegKeyPlugins.Key = PluginsRegistryKey;
		}

		/// <summary>
		/// Checks whether the DLL defines an Omea plugin, and adds registration for it, if that is the case.
		/// </summary>
		private static void RegisterPlugin(AssemblyXml assemblyxml, File wixFileAssembly, Component wixComponentRegistry)
		{
			Assembly assembly = Assembly.Load(assemblyxml.Include);
			foreach(Type type in assembly.GetTypes())
			{
				if(type.ContainsGenericParameters)
					continue; // Generics cannot be plugins

				if(type.FindInterfaces(delegate(Type m, object filterCriteria) { return (m.Name == "IPlugin") && (m.Assembly.GetName().Name == "OpenAPI"); }, null).Length == 0)
					continue; // Not a single plugin in this DLL

				// Yes, it's a plugin — produce registration in the Registry
				var wixRegValue = new RegistryValue();
				wixComponentRegistry.AddChild(wixRegValue);
				wixRegValue.Id = string.Format("{0}.Plugin.{1}", RegistryValueIdPrefix, assemblyxml.Include);
				wixRegValue.Action = RegistryValue.ActionType.write;
				wixRegValue.Root = RegistryRootType.HKMU;
				wixRegValue.Key = PluginsRegistryKey;
				wixRegValue.Name = Regex.Replace(assemblyxml.Include, "(.+?)(Plugin)?", "$1");
				wixRegValue.Type = RegistryValue.TypeType.@string;
				wixRegValue.Value = string.Format("[#{0}]", wixFileAssembly.Id);
			}
		}

		/// <summary>
		/// Writes a target file to the map, ensures that it's not duplicate.
		/// </summary>
		/// <param name="name">The file name, relative to the install root.</param>
		/// <param name="origin">Some textual comment on where the file is coming from.</param>
		/// <param name="mapTargetFiles">Map.</param>
		private static void RegisterTargetFile(string name, string origin, Dictionary<string, string> mapTargetFiles)
		{
			name = name.ToLowerInvariant();

			string sOtherOrigin;
			if(mapTargetFiles.TryGetValue(name, out sOtherOrigin))
				throw new InvalidOperationException(string.Format("The target file “{0}”is installed twice, first as “{1}”, then as “{2}”.", name, sOtherOrigin, origin));

			mapTargetFiles.Add(name, origin);
		}

		private void HarvestPublisherPolicyAssemblies(AssemblyXml assemblyxml, Directory directory, ComponentGroup componentgroup, ref int nGeneratedComponents, Dictionary<string, string> mapTargetFiles, GuidCacheXml guidcachexml)
		{
			if(!Bag.Get<bool>(AttributeName.IncludePublisherPolicy))
				return;

			int nWasGeneratedComponents = nGeneratedComponents;
			var diFolder = new DirectoryInfo(Bag.GetString(AttributeName.ProductBinariesDir));
			string sSatelliteWildcard = string.Format("Policy.*.{0}.{1}", assemblyxml.Include, "dll"); // Even an EXE assembly has a DLL policy file
			foreach(FileInfo fiPolicyAssembly in diFolder.GetFiles(sSatelliteWildcard))
			{
				// Find the companion policy config file
				var fiPolicyConfig = new FileInfo(Path.ChangeExtension(fiPolicyAssembly.FullName, ".Config"));
				if(!fiPolicyConfig.Exists)
					throw new InvalidOperationException(string.Format("Could not locate the publisher policy config file for the assembly “{0}”; expected: “{1}”.", fiPolicyAssembly.FullName, fiPolicyConfig.FullName));

				// We have to create a new component for each of the DLLs we'd like to GAC as publisher policy assemblies
				nGeneratedComponents++;

				// Create the component for the assembly (one per assembly)
				var component = new Component();
				directory.AddChild(component);
				component.Id = string.Format("{0}.{1}", FileComponentIdPrefix, fiPolicyAssembly.Name);
				component.Guid = guidcachexml[assemblyxml.Include + " PublisherPolicy"].ToString("B").ToUpper();
				component.DiskId = Bag.Get<int>(AttributeName.DiskId);
				component.Location = Component.LocationType.local;

				// Register component in the group
				var componentref = new ComponentRef();
				componentgroup.AddChild(componentref);
				componentref.Id = component.Id;

				// Add the assembly file (and make it the key path)
				var fileAssembly = new File();
				component.AddChild(fileAssembly);
				fileAssembly.Id = string.Format("{0}.{1}", FileIdPrefix, fiPolicyAssembly.Name);
				fileAssembly.Name = fiPolicyAssembly.Name;
				fileAssembly.KeyPath = YesNoType.yes;
				fileAssembly.Checksum = YesNoType.yes;
				fileAssembly.Vital = YesNoType.no;
				fileAssembly.Assembly = File.AssemblyType.net;
				fileAssembly.ReadOnly = YesNoType.yes;

				RegisterTargetFile(fileAssembly.Name, string.Format("Publisher policy assembly file for the {0} product assembly.", assemblyxml.Include), mapTargetFiles);

				// Add the policy config file
				var filePolicy = new File();
				component.AddChild(filePolicy);
				filePolicy.Id = string.Format("{0}.{1}", FileIdPrefix, fiPolicyConfig.Name);
				filePolicy.Name = fiPolicyConfig.Name;
				filePolicy.KeyPath = YesNoType.no;
				filePolicy.Checksum = YesNoType.yes;
				filePolicy.Vital = YesNoType.no;
				filePolicy.ReadOnly = YesNoType.yes;

				RegisterTargetFile(fileAssembly.Name, string.Format("Publisher policy configuration file for the {0} product assembly.", assemblyxml.Include), mapTargetFiles);
			}

			if(nWasGeneratedComponents == nGeneratedComponents) // None were actually collected
				throw new InvalidOperationException(string.Format("Could not locate the Publisher Policy assemblies for the “{0}” assembly. The expected full path is “{1}\\{2}”.", assemblyxml.Include, diFolder.FullName, sSatelliteWildcard));
		}

		/// <summary>
		/// Harvests the satellite files for an assembly.
		/// </summary>
		/// <param name="sFileLocalName">Local name (without path, but with extension) of the file to seek for.</param>
		/// <param name="errorlevel">How to treat the missing file.</param>
		/// <param name="sSatelliteDisplayName">Display name of the satellite to be included into the error message.</param>
		/// <param name="wixparent">The parent component to mount the new entry into.</param>
		/// <param name="assemblyxml">The assembly for which we're seeking for satellites.</param>
		private void HarvestSatellite(AssemblyXml assemblyxml, string sFileLocalName, IParentElement wixparent, MissingSatelliteErrorLevel errorlevel, string sSatelliteDisplayName, Dictionary<string, string> mapTargetFiles)
		{
			if(sFileLocalName == null)
				throw new ArgumentNullException("sFileLocalName");
			if(sSatelliteDisplayName == null)
				throw new ArgumentNullException("sSatelliteDisplayName");
			if(wixparent == null)
				throw new ArgumentNullException("wixparent");

			// Full path
			var fi = new FileInfo(Path.Combine(Bag.GetString(AttributeName.ProductBinariesDir), sFileLocalName));

			// Missing?
			if(!fi.Exists)
			{
				string sErrorMessage = string.Format("Could not locate the {2} for the “{0}” assembly. The expected full path is “{1}”.", assemblyxml.Include, fi.FullName, sSatelliteDisplayName);
				switch(errorlevel)
				{
				case MissingSatelliteErrorLevel.None:
					break; // Ignore
				case MissingSatelliteErrorLevel.Warning:
					Log.LogWarning(sErrorMessage);
					break;
				case MissingSatelliteErrorLevel.Error:
					throw new InvalidOperationException(sErrorMessage);
				default:
					throw new ArgumentOutOfRangeException("errorlevel", errorlevel, "Oops.");
				}
				return;
			}

			// Create an entry
			var wixFile = new File();
			wixparent.AddChild(wixFile);
			wixFile.Id = string.Format("{0}{1}", FileIdPrefix, fi.Name);
			wixFile.Name = fi.Name;
			wixFile.KeyPath = YesNoType.no;
			wixFile.Checksum = YesNoType.no;
			wixFile.Vital = YesNoType.no;

			RegisterTargetFile(wixFile.Name, string.Format("Satellite for the {0} product assembly.", assemblyxml.Include), mapTargetFiles);
		}

		/// <summary>
		/// Processes those AllAssemblies.Xml entries that are our own product assemblies.
		/// </summary>
		private int ProcessAssemblies(Directory wixDirectory, ComponentGroup wixComponentGroup, Component wixComponentRegistry, AllAssembliesXml allassembliesxml, Dictionary<string, string> mapTargetFiles, GuidCacheXml guidcachexml)
		{
			// Collect the assemblies
			int nGeneratedComponents = 0;
			foreach(ItemGroupXml group in allassembliesxml.ItemGroup)
			{
				if(group.AllAssemblies == null)
					continue;
				foreach(AssemblyXml assemblyxml in group.AllAssemblies)
				{
					nGeneratedComponents++;
					FileInfo fiAssembly = FindAssemblyFile(assemblyxml);
					string sExtension = fiAssembly.Extension.TrimStart('.'); // The extension without a dot

					// Create the component for the assembly (one per assembly)
					var wixComponent = new Component();
					wixDirectory.AddChild(wixComponent);
					wixComponent.Id = string.Format("{0}.{1}.{2}", FileComponentIdPrefix, assemblyxml.Include, sExtension);
					wixComponent.Guid = assemblyxml.MsiGuid;
					wixComponent.DiskId = Bag.Get<int>(AttributeName.DiskId);
					wixComponent.Location = Component.LocationType.local;

					// Register component in the group
					var componentref = new ComponentRef();
					wixComponentGroup.AddChild(componentref);
					componentref.Id = wixComponent.Id;

					// Add the assembly file (and make it the key path)
					var wixFileAssembly = new File();
					wixComponent.AddChild(wixFileAssembly);
					wixFileAssembly.Id = string.Format("{0}.{1}.{2}", FileIdPrefix, assemblyxml.Include, sExtension);
					wixFileAssembly.Name = string.Format("{0}.{1}", assemblyxml.Include, sExtension);
					wixFileAssembly.KeyPath = YesNoType.yes;
					wixFileAssembly.Checksum = YesNoType.yes;
					wixFileAssembly.Vital = YesNoType.yes;
					wixFileAssembly.ReadOnly = YesNoType.yes;

					RegisterTargetFile(wixFileAssembly.Name, string.Format("The {0} product assembly.", assemblyxml.Include), mapTargetFiles);

					// Check whether it's a managed or native assembly
					AssemblyName assemblyname = null;
					try
					{
						assemblyname = AssemblyName.GetAssemblyName(fiAssembly.FullName);
					}
					catch(BadImageFormatException)
					{
					}

					// Add COM Self-Registration data
					if(assemblyxml.ComRegister)
					{
						/*
					foreach(ISchemaElement harvested in HarvestComSelfRegistration(wixFileAssembly, fiAssembly))
						wixComponent.AddChild(harvested);
*/
						SelfRegHarvester.Harvest(fiAssembly, assemblyname != null, wixComponent, wixFileAssembly);
					}

					// Ensure the managed DLL has a strong name
					if((assemblyname != null) && (Bag.Get<bool>(AttributeName.RequireStrongName)))
					{
						byte[] token = assemblyname.GetPublicKeyToken();
						if((token == null) || (token.Length == 0))
							throw new InvalidOperationException(string.Format("The assembly “{0}” does not have a strong name.", assemblyxml.Include));
					}

					// Add PDBs
					if(Bag.Get<bool>(AttributeName.IncludePdb))
						HarvestSatellite(assemblyxml, assemblyxml.Include + ".pdb", wixComponent, MissingSatelliteErrorLevel.Error, "PDB file", mapTargetFiles);

					// Add XmlDocs
					if((assemblyname != null) && (Bag.Get<bool>(AttributeName.IncludeXmlDoc)))
						HarvestSatellite(assemblyxml, assemblyxml.Include + ".xml", wixComponent, MissingSatelliteErrorLevel.Error, "XmlDoc file", mapTargetFiles);

					// Add configs
					HarvestSatellite(assemblyxml, assemblyxml.Include + "." + sExtension + ".config", wixComponent, (assemblyxml.HasAppConfig ? MissingSatelliteErrorLevel.Error : MissingSatelliteErrorLevel.None), "application configuration file", mapTargetFiles);
					HarvestSatellite(assemblyxml, assemblyxml.Include + "." + sExtension + ".manifest", wixComponent, (assemblyxml.HasMainfest ? MissingSatelliteErrorLevel.Error : MissingSatelliteErrorLevel.None), "assembly manifest file", mapTargetFiles);
					HarvestSatellite(assemblyxml, assemblyxml.Include + ".XmlSerializers." + sExtension, wixComponent, (assemblyxml.HasXmlSerializers ? MissingSatelliteErrorLevel.Error : MissingSatelliteErrorLevel.None), "serialization assembly", mapTargetFiles);

					// Add publisher policy assemblies
					if(assemblyname != null)
						HarvestPublisherPolicyAssemblies(assemblyxml, wixDirectory, wixComponentGroup, ref nGeneratedComponents, mapTargetFiles, guidcachexml);

					// Register as an OmeaPlugin
					if(assemblyname != null)
						RegisterPlugin(assemblyxml, wixFileAssembly, wixComponentRegistry);
				}
			}
			return nGeneratedComponents;
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Actions under the resolver.
		/// </summary>
		protected override void ExecuteTaskResolved()
		{
			GuidCacheXml guidcachexml = GuidCacheXml.Load(new FileInfo(Bag.GetString(AttributeName.GuidCacheFile)).OpenRead());

			// Global structure of the WiX fragment file
			var wix = new Wix();
			var wixFragmentComponents = new Fragment(); // Fragment with the payload
			wix.AddChild(wixFragmentComponents);
			var wixDirectoryRef = new DirectoryRef(); // Mount into the directories tree, defined externally
			wixFragmentComponents.AddChild(wixDirectoryRef);
			wixDirectoryRef.Id = Bag.GetString(AttributeName.WixDirectoryId);
			var wixDirectory = new Directory(); // A locally created nameless directory that does not add any nested folders but defines the sources location
			wixDirectoryRef.AddChild(wixDirectory);
			wixDirectory.Id = DirectoryId;
			wixDirectory.FileSource = Bag.GetString(AttributeName.ProductBinariesDir);
			var wixFragmentGroup = new Fragment(); // Fragment with the component-group that collects the components
			wix.AddChild(wixFragmentGroup);
			var wixComponentGroup = new ComponentGroup(); // ComponentGroup that collects the components
			wixFragmentGroup.AddChild(wixComponentGroup);
			wixComponentGroup.Id = Bag.GetString(AttributeName.WixComponentGroupId);

			// A component for the generated Registry entries
			var wixComponentRegistry = new Component();
			wixDirectory.AddChild(wixComponentRegistry);
			wixComponentRegistry.Id = RegistryComponentIdPrefix;
			wixComponentRegistry.Guid = guidcachexml[GuidIdXml.MsiComponent_ProductBinaries_Registry_Hkmu].ToString("B").ToUpper();
			wixComponentRegistry.DiskId = Bag.Get<int>(AttributeName.DiskId);
			wixComponentRegistry.Location = Component.LocationType.local;
			var wixComponentRegistryRef = new ComponentRef();
			wixComponentGroup.AddChild(wixComponentRegistryRef);
			wixComponentRegistryRef.Id = wixComponentRegistry.Id;

			// Create the Registry key for the Plugins section
			CreatePluginsRegistryKey(wixComponentRegistry);

			// Load the AllAssemblies file
			AllAssembliesXml allassembliesxml = AllAssembliesXml.LoadFrom(Bag.Get<TaskItemByValue>(AttributeName.AllAssembliesXml).ItemSpec);

			// Tracks the files on the target machine, to prevent the same file from being installed both as an assembly and as a reference
			var mapTargetFiles = new Dictionary<string, string>();

			int nGeneratedComponents = ProcessAssemblies(wixDirectory, wixComponentGroup, wixComponentRegistry, allassembliesxml, mapTargetFiles, guidcachexml);

			// Save to the output file
			using(var xw = new XmlTextWriter(new FileStream(Bag.GetString(AttributeName.OutputFile), FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8))
			{
				xw.Formatting = Formatting.Indented;
				wix.OutputXml(xw);
			}

			// Report (also to see the target in the build logs)
			Log.LogMessage(MessageImportance.Normal, "Generated {0} product binary components.", nGeneratedComponents);
		}

		#endregion

		#region MissingSatelliteErrorLevel Type

		/// <summary>
		/// Defines how to treat a missing satellite of the given type.
		/// </summary>
		protected enum MissingSatelliteErrorLevel
		{
			None,
			Warning,
			Error
		}

		#endregion
	}
}
