﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Reflection;
using System.Xml;

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Generates the publisher policy assemblies for the given set of assemblies and their specific versions.
	/// </summary>
	public class CreatePublisherPolicyBuildFile : TaskBase
	{
		#region Data

		public static readonly string NsBuild = "http://schemas.microsoft.com/developer/msbuild/2003";

		public static readonly string NsPolicy = "urn:schemas-microsoft-com:asm.v1";

		#endregion

		#region Attributes

		/// <summary>
		/// Lists the assemblies for which the policies should be generated.
		/// </summary>
		[Required]
		public ITaskItem[] InputFiles
		{
			get
			{
				return (ITaskItem[])Bag[AttributeName.InputFiles];
			}
			set
			{
				Bag[AttributeName.InputFiles] = value;
			}
		}

		/// <summary>
		/// Specifies the intermediate folder in which the config files should be created.
		/// </summary>
		[Required]
		public ITaskItem IntDir
		{
			get
			{
				return (ITaskItem)Bag[AttributeName.IntDir];
			}
			set
			{
				Bag[AttributeName.IntDir] = value;
			}
		}

		/// <summary>
		/// Specifies the key file to sign the publisher policy assemblies with; must correspond to the key that was used to sign the original assembly.
		/// </summary>
		[Required]
		public ITaskItem KeyFile
		{
			get
			{
				return (ITaskItem)Bag[AttributeName.KeyFile];
			}
			set
			{
				Bag[AttributeName.KeyFile] = value;
			}
		}

		/// <summary>
		/// Specifies the output folder into which the resulting assemblies will be written.
		/// </summary>
		[Required]
		public ITaskItem OutDir
		{
			get
			{
				return (ITaskItem)Bag[AttributeName.OutDir];
			}
			set
			{
				Bag[AttributeName.OutDir] = value;
			}
		}

		/// <summary>
		/// Specifies the output MSBuild project file that will be generated by this task.
		/// </summary>
		[Required]
		public ITaskItem OutputFile
		{
			get
			{
				return (ITaskItem)Bag[AttributeName.OutputFile];
			}
			set
			{
				Bag[AttributeName.OutputFile] = value;
			}
		}

		/// <summary>
		/// The upper boundary of the source versions range for the publisher policy. Optional; if missed, the actual version of the assembly will be used.
		/// </summary>
		public string SourceVersionHigh
		{
			get
			{
				return (string)Bag[AttributeName.SourceVersionHigh];
			}
			set
			{
				Bag[AttributeName.SourceVersionHigh] = value;
			}
		}

		/// <summary>
		/// The lower boundary of the source versions range for the publisher policy. Required.
		/// </summary>
		[Required]
		public string SourceVersionLow
		{
			get
			{
				return (string)Bag[AttributeName.SourceVersionLow];
			}
			set
			{
				Bag[AttributeName.SourceVersionLow] = value;
			}
		}

		/// <summary>
		/// The target version for the publisher policy. Optional; if missed, the actual version of the assembly will be used.
		/// </summary>
		public string TargetVersion
		{
			get
			{
				return (string)Bag[AttributeName.TargetVersion];
			}
			set
			{
				Bag[AttributeName.TargetVersion] = value;
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Ensures that version has at least <paramref name="components"/> components defined.
		/// </summary>
		public static void AssertVersionComponents(Version version, int components)
		{
			if(version == null)
				throw new ArgumentNullException("version");
			if((components < 0) || (components > 4))
				throw new InvalidOperationException(string.Format("Error in constraint: the version number cannot have {0} components.", components));

			if(components == 0)
				return;
			if(version.Major < 0)
				throw new InvalidOperationException(string.Format("The version number “{0}” is expected to have at least {1} components defined.", version, components));

			if(components == 1)
				return;
			if(version.Minor < 0)
				throw new InvalidOperationException(string.Format("The version number “{0}” is expected to have at least {1} components defined.", version, components));

			if(components == 2)
				return;
			if(version.Build < 0)
				throw new InvalidOperationException(string.Format("The version number “{0}” is expected to have at least {1} components defined.", version, components));

			if(components == 3)
				return;
			if(version.Revision < 0)
				throw new InvalidOperationException(string.Format("The version number “{0}” is expected to have at least {1} components defined.", version, components));
		}

		#endregion

		#region Implementation

		private static string GeneratePublisherPolicyFile(AssemblyName assemblyname, Version versionSourceLow, Version versionSourceHigh, Version versionTarget, FileSystemInfo diOut)
		{
			if(versionSourceLow == null)
				throw new ArgumentNullException("versionSourceLow");

			// Determine the properties
			if(versionSourceHigh == null)
			{
				versionSourceHigh = assemblyname.Version;
				AssertVersionComponents(versionSourceHigh, 4);
			}
			if(versionTarget == null)
			{
				versionTarget = assemblyname.Version;
				AssertVersionComponents(versionTarget, 4);
			}

			// The two older version components must not change along the version numbers span
			if((versionSourceLow.Major != versionSourceHigh.Major) || (versionSourceLow.Minor != versionSourceHigh.Minor))
				throw new InvalidOperationException(string.Format("The two older version number components must not change within the source version numbers span [{0}–{1}] (while processing assembly “{2}”).", versionSourceLow, versionSourceHigh, assemblyname.FullName));
			if(versionSourceLow > versionSourceHigh)
				throw new InvalidOperationException(string.Format("The version numbers span [{0}–{1}] must be normalized (while processing assembly “{2}”).", versionSourceLow, versionSourceHigh, assemblyname.FullName));

			// Build the policy file
			var settings = new XmlWriterSettings();
			settings.Indent = true;
			string sConfigFileName = Path.Combine(diOut.FullName, string.Format("Policy.{0}.{1}.{2}.Config", versionSourceLow.Major, versionSourceLow.Minor, assemblyname.Name));
			using(XmlWriter writer = XmlWriter.Create(sConfigFileName, settings))
			{
				writer.WriteStartElement("configuration");
				writer.WriteStartElement("runtime");
				writer.WriteStartElement("assemblyBinding", NsPolicy);
				writer.WriteStartElement("dependentAssembly", NsPolicy);

				writer.WriteStartElement("assemblyIdentity", NsPolicy);
				writer.WriteAttributeString("name", assemblyname.Name);
				writer.WriteStartAttribute("publicKeyToken");
				byte[] arToken = assemblyname.GetPublicKeyToken();
				writer.WriteBinHex(arToken, 0, arToken.Length);
				writer.WriteEndAttribute();
				writer.WriteAttributeString("culture", assemblyname.CultureInfo.Name);
				writer.WriteEndElement();

				writer.WriteStartElement("bindingRedirect", NsPolicy);
				writer.WriteAttributeString("oldVersion", (versionSourceLow == versionSourceHigh ? versionSourceLow.ToString(4) : versionSourceLow.ToString(4) + "-" + versionSourceHigh.ToString(4)));
				writer.WriteAttributeString("newVersion", versionTarget.ToString(4));
				writer.WriteEndElement();

				writer.WriteEndElement();
				writer.WriteEndElement();
				writer.WriteEndElement();
				writer.WriteEndElement();
			}

			return sConfigFileName;
		}

		#endregion

		#region Overrides

		/// <summary>
		/// The method to be overriden in inheriting tasks.
		/// Throw an exception in case of an errror.
		/// </summary>
		protected override void ExecuteTask()
		{
			// Acq the int dir, create as needed
			var diIntermediate = new DirectoryInfo(GetValue<ITaskItem>(AttributeName.IntDir).GetMetadata("FullPath"));
			if(!diIntermediate.Exists)
				diIntermediate.Create();
			var diOutput = new DirectoryInfo(GetValue<ITaskItem>(AttributeName.OutDir).GetMetadata("FullPath"));
			if(!diOutput.Exists)
				diOutput.Create();

			// Acquire the versions: one required and two optional params, leave missed Null for now
			var versionSourceLow = new Version(GetStringValue(AttributeName.SourceVersionLow));
			AssertVersionComponents(versionSourceLow, 4);
			Version versionSourceHigh = null;
			if(!string.IsNullOrEmpty(SourceVersionHigh))
			{
				versionSourceHigh = new Version(SourceVersionHigh);
				AssertVersionComponents(versionSourceHigh, 4);
			}
			Version versionTarget = null;
			if(!string.IsNullOrEmpty(TargetVersion))
			{
				versionTarget = new Version(TargetVersion);
				AssertVersionComponents(versionTarget, 4);
			}

			// Start creating the build file
			var settings = new XmlWriterSettings();
			settings.Indent = true;
			using(XmlWriter writer = XmlWriter.Create(GetStringValue(AttributeName.OutputFile), settings))
			{
				writer.WriteStartElement("Project", NsBuild);

				writer.WriteStartElement("Import", NsBuild);
				writer.WriteAttributeString("Project", @"$(MSBuildBinPath)\Microsoft.Common.Tasks");
				writer.WriteEndElement();

				writer.WriteStartElement("Target", NsBuild);
				writer.WriteAttributeString("Name", "Build");

				// An al task for each of the assemblies
				for(int nItem = 0; nItem < GetValue<ITaskItem[]>(AttributeName.InputFiles).Length; nItem++)
				{
					// Pick the item
					var fiItem = new FileInfo(GetValue<ITaskItem[]>(AttributeName.InputFiles)[nItem].GetMetadata("FullPath"));
					if(!fiItem.Exists)
						throw new InvalidOperationException(string.Format("The input file “{0}” does not exist.", fiItem.FullName));

					// Generate the publisher policy file
					AssemblyName assemblyname = AssemblyName.GetAssemblyName(fiItem.FullName);
					string sConfigFileName = GeneratePublisherPolicyFile(assemblyname, versionSourceLow, versionSourceHigh, versionTarget, diIntermediate);
					string sAssemblyFileName = Path.Combine(diOutput.FullName, string.Format("Policy.{0}.{1}.{2}.dll", versionSourceLow.Major, versionSourceLow.Minor, assemblyname.Name));

					// Produce the build task
					writer.WriteStartElement("AL", NsBuild);
					writer.WriteAttributeString("LinkResources", sConfigFileName);
					writer.WriteAttributeString("OutputAssembly", sAssemblyFileName);
					writer.WriteAttributeString("KeyFile", Path.GetFullPath(GetValue<ITaskItem>(AttributeName.KeyFile).GetMetadata("FullPath")));
					writer.WriteEndElement();
				}

				writer.WriteEndElement();
				writer.WriteEndElement();
			}
		}

		#endregion
	}
}
