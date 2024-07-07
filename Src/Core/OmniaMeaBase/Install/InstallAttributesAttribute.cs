// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Annotations;

namespace JetBrains.Omea.Base.Install
{
	/// <summary>
	/// An attribute to mark the classes that process the installation data written in the form of assembly attributes of type <see cref="AttributeToInstall"/> during registration and unregistration.
	/// Such classes will also be called once to perform their own attribute-independent installation.
	/// The class must implement the <see cref="IInstallAttributes"/> interface.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class InstallAttributesAttribute : Attribute
	{
		#region Data

		private readonly Type myAttributeToInstall;

		#endregion

		#region Init

		/// <summary>
		/// Creates the attribute.
		/// The installer is not attached to any particular attributes, but only executes the one-time action.
		/// </summary>
		public InstallAttributesAttribute()
			: this(null)
		{
		}

		/// <summary>
		/// Creates the attribute.
		/// </summary>
		/// <param name="typeAttributeToInstall">Type of the attribute for which the class marked by <see cref="InstallAttributesAttribute"/> should be invoked to process the installation. May be <c>Null</c> if the class wants to execute its own installation only.</param>
		public InstallAttributesAttribute([CanBeNull] Type typeAttributeToInstall)
		{
			myAttributeToInstall = typeAttributeToInstall;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the type of the attribute for which the class marked by <see cref="InstallAttributesAttribute"/> should be invoked to process the installation.
		/// </summary>
		public Type AttributeToInstall
		{
			get
			{
				return myAttributeToInstall;
			}
		}

		#endregion
	}
}
