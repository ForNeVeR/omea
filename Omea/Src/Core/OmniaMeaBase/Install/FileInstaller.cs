/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

using JetBrains.Build.InstallationData;

namespace JetBrains.Omea.Base.Install
{
	/// <summary>
	/// Processes the <see cref="InstallFileAttribute"/> installations.
	/// </summary>
	[InstallAttributes(typeof(InstallFileAttribute))]
	public class FileInstaller : IInstallAttributes
	{
		#region IInstallAttributes Members

		/// <summary>
		/// Called for each of the attributes of the requested type encountered in the known assemblies.
		/// </summary>
		/// <param name="installer">The installer object that provides the installation data.</param>
		/// <param name="attributeInstance">Instance of the attribute to process.</param>
		/// <returns>The list of the Registry entries to write, or <c>Null</c> if none.</returns>
		public InstallationDataXml InstallInstance(Installer installer, object attributeInstance)
		{
			var attr = attributeInstance as InstallFileAttribute;
			if(attr == null)
				throw new InvalidOperationException(string.Format("No attr instance."));

			return new FolderXml {SourceRoot = attr.SourceRoot, SourceDir = attr.SourceRelativeDir, TargetRoot = attr.TargetRoot, TargetDir = attr.TargetRelativeDir, Id = attr.Id, MsiComponentGuid = attr.MsiGuid.ToString("B").ToUpperInvariant(), Files = new[] {new FileXml(attr.FilesMask, "")}}.ToInstallationData();
		}

		/// <summary>
		/// Called once on the object during the registration process.
		/// </summary>
		/// <param name="installer">The installer object that provides the installation data.</param>
		/// <returns>The list of the Registry entries to write, or <c>Null</c> if none.</returns>
		public InstallationDataXml InstallStatic(Installer installer)
		{
			return null; // NOP
		}

		#endregion
	}
}