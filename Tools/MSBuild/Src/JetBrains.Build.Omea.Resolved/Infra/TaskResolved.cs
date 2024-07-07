// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using JetBrains.Build.AllAssemblies;
using JetBrains.Build.Omea.Infra;
using JetBrains.Build.Omea.Util;
using JetBrains.Omea.Base.Install;

namespace JetBrains.Build.Omea.Resolved.Infra
{
	/// <summary>
	/// Same as the base class, plus some business logic helpers for extracting and validating specific Bag parameters.
	/// </summary>
	public abstract class TaskResolved : TaskBaseResolved
	{
		#region Data

		/// <summary>
		/// A semicolon-separated list of the valid assembly extensions.
		/// </summary>
		public static readonly string AssemblyExtensions = "dll;exe";

		#endregion

		#region Operations

		/// <summary>
		/// Checks for the existing files and choses the assembly extension from the list, eg “exe” or “dll”.
		/// Throws if not found.
		/// </summary>
		public FileInfo FindAssemblyFile(AssemblyXml assemblyxml)
		{
			var sbProbingPaths = new StringBuilder();
			foreach(string sExtension in AssemblyExtensions.Split(';'))
			{
				string sProbingPath = Path.Combine(Bag.GetString(AttributeName.ProductBinariesDir), string.Format("{0}.{1}", assemblyxml.Include, sExtension));
				var fi = new FileInfo(sProbingPath);
				if(fi.Exists)
					return fi;
				sbProbingPaths.AppendLine();
				sbProbingPaths.Append(sProbingPath);
			}
			throw new InvalidOperationException(string.Format("Could not locate the “{0}” assembly in the “{1}” product binaries directory. The probing paths are listed below.{2}", assemblyxml.Include, Bag.GetString(AttributeName.ProductBinariesDir), sbProbingPaths));
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Creates an installer based on the <see cref="AttributeName.AllAssembliesXml"/> list.
		/// </summary>
		protected Installer CreateInstaller()
		{
			// Load the AllAssemblies file
			AllAssembliesXml allassembliesxml = AllAssembliesXml.LoadFrom(Bag.Get<TaskItemByValue>(AttributeName.AllAssembliesXml).ItemSpec);

			// Get the list of assemblies
			var assemblies = new List<Assembly>();
			foreach(ItemGroupXml group in allassembliesxml.ItemGroup)
			{
				if(group.AllAssemblies == null)
					continue;
				foreach(AssemblyXml assemblyxml in group.AllAssemblies)
				{
					FileInfo fiAssembly = FindAssemblyFile(assemblyxml);

					// Check whether it's a managed or native assembly
					AssemblyName assemblyname = null;
					try
					{
						assemblyname = AssemblyName.GetAssemblyName(fiAssembly.FullName);
					}
					catch(BadImageFormatException)
					{
					}

					if(assemblyname == null)
						continue; // Not a managed assembly

					// Collect
					assemblies.Add(Assembly.LoadFrom(fiAssembly.FullName));
				}
			}

			return new Installer(assemblies);
		}

		#endregion
	}
}
