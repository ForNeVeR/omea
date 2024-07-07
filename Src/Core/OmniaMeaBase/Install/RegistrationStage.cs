// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
