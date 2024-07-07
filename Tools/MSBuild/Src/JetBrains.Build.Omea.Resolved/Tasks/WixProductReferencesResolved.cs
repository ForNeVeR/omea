// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using JetBrains.Build.AllAssemblies;
using JetBrains.Build.Omea.Infra;
using JetBrains.Build.Omea.Resolved.Infra;
using JetBrains.Build.Omea.Util;

using Microsoft.Build.Framework;
using Microsoft.Tools.WindowsInstallerXml.Serialize;

using Directory=Microsoft.Tools.WindowsInstallerXml.Serialize.Directory;
using File=Microsoft.Tools.WindowsInstallerXml.Serialize.File;

namespace JetBrains.Build.Omea.Resolved.Tasks
{
	/// <summary>
	/// Creates a WiX source with the product biaries described in it.
	/// </summary>
	public class WixProductReferencesResolved : TaskResolved
	{
		#region Data

		/// <summary>
		/// Prefix for the generated WiX element ID.
		/// </summary>
		public static readonly string DirectoryId = "D.ProductReferences";

		/// <summary>
		/// Prefix for the generated WiX element ID.
		/// </summary>
		public static readonly string FileComponentIdPrefix = "C.ProductReferences";

		/// <summary>
		/// Prefix for the generated WiX element ID.
		/// </summary>
		public static readonly string FileIdPrefix = "F.ProductReferences";

		#endregion

		#region Implementation

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

		/// <summary>
		/// Processes the files that should be taken from the “References” folder and installed “AS IS”.
		/// </summary>
		private int ProcessReferences(Directory wixDirectory, ComponentGroup wixComponentGroup, AllAssembliesXml allassembliesxml, Dictionary<string, string> mapTargetFiles)
		{
			int nGeneratedComponents = 0;

			// Replaces illegal chars with underscores
			var regexMakeId = new Regex("[^a-zA-Z0-9_.]");

			foreach(ItemGroupXml group in allassembliesxml.ItemGroup)
			{
				if(group.References == null)
					continue;
				foreach(ReferenceXml referencexml in group.References)
				{
					nGeneratedComponents++;
					var fiReference = new FileInfo(Path.Combine(Bag.GetString(AttributeName.ProductReferencesDir), referencexml.Include));
					if(!fiReference.Exists)
						throw new InvalidOperationException(string.Format("The reference file “{0}” could not be found.", fiReference.FullName));

					string sIdSuffix = regexMakeId.Replace(fiReference.Name, "_");

					// Create the component for the assembly (one per assembly)
					var wixComponent = new Component();
					wixDirectory.AddChild(wixComponent);
					wixComponent.Id = string.Format("{0}.{1}", FileComponentIdPrefix, sIdSuffix);
					wixComponent.Guid = referencexml.MsiGuid;
					wixComponent.DiskId = Bag.Get<int>(AttributeName.DiskId);
					wixComponent.Location = Component.LocationType.local;

					// Register component in the group
					var componentref = new ComponentRef();
					wixComponentGroup.AddChild(componentref);
					componentref.Id = wixComponent.Id;

					// Add the reference file (and make it the key path)
					var wixFileReference = new File();
					wixComponent.AddChild(wixFileReference);
					wixFileReference.Id = string.Format("{0}.{1}", FileIdPrefix, sIdSuffix);
					wixFileReference.Name = fiReference.Name;
					wixFileReference.KeyPath = YesNoType.yes;
					wixFileReference.Checksum = YesNoType.yes;
					wixFileReference.Vital = YesNoType.yes;
					wixFileReference.ReadOnly = YesNoType.yes;

					RegisterTargetFile(wixFileReference.Name, string.Format("The “{0}” reference.", referencexml.Include), mapTargetFiles);
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
			wixDirectory.FileSource = Bag.GetString(AttributeName.ProductReferencesDir);
			var wixFragmentGroup = new Fragment(); // Fragment with the component-group that collects the components
			wix.AddChild(wixFragmentGroup);
			var wixComponentGroup = new ComponentGroup(); // ComponentGroup that collects the components
			wixFragmentGroup.AddChild(wixComponentGroup);
			wixComponentGroup.Id = Bag.GetString(AttributeName.WixComponentGroupId);

			// Load the AllAssemblies file
			AllAssembliesXml allassembliesxml = AllAssembliesXml.LoadFrom(Bag.Get<TaskItemByValue>(AttributeName.AllAssembliesXml).ItemSpec);

			// Tracks the files on the target machine, to prevent the same file from being installed both as an assembly and as a reference
			var mapTargetFiles = new Dictionary<string, string>();

			int nGeneratedComponents = ProcessReferences(wixDirectory, wixComponentGroup, allassembliesxml, mapTargetFiles);

			// Save to the output file
			using(var xw = new XmlTextWriter(new FileStream(Bag.GetString(AttributeName.OutputFile), FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8))
			{
				xw.Formatting = Formatting.Indented;
				wix.OutputXml(xw);
			}

			// Report (also to see the target in the build logs)
			Log.LogMessage(MessageImportance.Normal, "Generated {0} product reference components.", nGeneratedComponents);
		}

		#endregion
	}
}
