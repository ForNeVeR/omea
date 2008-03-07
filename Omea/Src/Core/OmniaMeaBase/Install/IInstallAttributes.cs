/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Annotations;
using JetBrains.Build.InstallationData;

namespace JetBrains.Omea.Base.Install
{
	/// <summary>
	/// A class marked with <see cref="InstallAttributesAttribute"/> must implement this interface in order to be called for processing each of the assembly attributes it handles.
	/// </summary>
	public interface IInstallAttributes
	{
		#region Operations

		/// <summary>
		/// Called for each of the attributes of the requested type encountered in the known assemblies.
		/// </summary>
		/// <param name="installer">The installer object that provides the installation data.</param>
		/// <param name="attributeInstance">Instance of the attribute to process.</param>
		/// <returns>The list of the Registry entries to write, or <c>Null</c> if none.</returns>
		[CanBeNull]
		InstallationDataXml InstallInstance(Installer installer, [NotNull] object attributeInstance);

		/// <summary>
		/// Called once on the object during the registration process.
		/// </summary>
		/// <param name="installer">The installer object that provides the installation data.</param>
		/// <returns>The list of the Registry entries to write, or <c>Null</c> if none.</returns>
		[CanBeNull]
		InstallationDataXml InstallStatic(Installer installer);

		#endregion
	}
}