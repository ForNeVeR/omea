// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

using JetBrains.Build.InstallationData;
using JetBrains.Build.Omea.Infra;
using JetBrains.Build.Omea.Resolved.Infra;
using JetBrains.Build.Omea.Util;
using JetBrains.Omea.Base.Install;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Resolved.Tasks
{
	/// <summary>
	/// Base for the registry-data installation and uninstallation tasks.
	/// </summary>
	public class LocalInstallDataResolved : TaskResolved
	{
		#region Operations

		/// <summary>
		/// Root -> file system path.
		/// </summary>
		public static string ResolveSourceDirRoot(SourceRootXml root, Bag bag)
		{
			switch(root)
			{
			case SourceRootXml.ProductBinariesDir:
				return bag.GetString(AttributeName.ProductBinariesDir);
			case SourceRootXml.ProductHomeDir:
				return bag.GetString(AttributeName.ProductHomeDir);
			default:
				throw new InvalidOperationException(string.Format("Unsupported installation data file source root “{0}”.", root));
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Root -> file system path.
		/// </summary>
		private static string ResolveTargetDirRoot(TargetRootXml root, Bag bag)
		{
			switch(root)
			{
			case TargetRootXml.InstallDir:
				return bag.GetString(AttributeName.ProductBinariesDir);
			default:
				throw new InvalidOperationException(string.Format("Unsupported installation data file target root “{0}”.", root));
			}
		}

		/// <summary>
		/// Defines the macros that could be used in the Registry dumps.
		/// <see cref="LocalInstaller"/> will subst them.
		/// </summary>
		protected IDictionary<string, string> GetMacros()
		{
			var macros = new Dictionary<string, string>();

			macros.Add(MacroNameXml.SystemDir.ToString(), Environment.GetFolderPath(Environment.SpecialFolder.System));
			macros.Add(MacroNameXml.ProductBinariesDir.ToString(), Path.GetFullPath(Bag.GetString(AttributeName.ProductBinariesDir)));
			macros.Add(MacroNameXml.DateTime.ToString(), DateTime.Now.ToString("s"));

			return macros;
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Actions under the resolver.
		/// </summary>
		protected override void ExecuteTaskResolved()
		{
			// Get the stage
			var stage = (RegistrationStage)TypeDescriptor.GetConverter(typeof(RegistrationStage)).ConvertFromString(Bag.GetString(AttributeName.Stage));

			// Collect the installation data and install it
			LocalInstaller.Install(CreateInstaller().HarvestInstallationData(), stage, GetMacros(), root => new DirectoryInfo(ResolveSourceDirRoot(root, Bag)), root => new DirectoryInfo(ResolveTargetDirRoot(root, Bag)), text => { Log.LogMessage(MessageImportance.Low, text); });
		}

		#endregion
	}
}
