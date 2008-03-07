/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.Base.Install
{
	/// <summary>
	/// Defines the exact stage of the registration process of which the handler is being queried.
	/// </summary>
	public enum RegistrationStage
	{
		/// <summary>
		/// The assembly is being registered.
		/// </summary>
		Register,
		/// <summary>
		/// The assembly is being unregistered.
		/// </summary>
		Unregister
	}
}