// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Common.Infra
{
	/// <summary>
	/// A task that defines task input parameters for defining the VS hive.
	/// </summary>
	public abstract class VsHiveTask : TaskBase
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the Visual Studio root suffix to work with, a dash “-” means no hive.
		/// Example: “-” (main hive), “ReSharper” (experimental hive).
		/// <see cref="VsVersion"/> and <see cref="VsRootSuffix"/> together form the Visual Studio Hive.
		/// </summary>
		[Required]
		public string VsRootSuffix
		{
			get
			{
				return (string)Bag[AttributeName.VsRootSuffix];
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				Bag[AttributeName.VsRootSuffix] = value;
			}
		}

		/// <summary>
		/// Gets or sets the Visual Studio version to work with.
		/// Example: “8.0”.
		/// <see cref="VsVersion"/> and <see cref="VsRootSuffix"/> together form the Visual Studio Hive.
		/// </summary>
		[Required]
		public string VsVersion
		{
			get
			{
				return (string)Bag[AttributeName.VsVersion];
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				Bag[AttributeName.VsVersion] = value;
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Gets the Visual Studio hive, which is a concatenation of the version and the hive.
		/// </summary>
		protected string GetVsHive()
		{
			return GetVsVersion() + GetVsRootSuffix();
		}

		/// <summary>
		/// Gets the Visual Studio root suffix, checks that it's been defined.
		/// Replaces the dash special value “-” with an empty root suffix.
		/// </summary>
		protected string GetVsRootSuffix()
		{
			string retval = GetStringValue(AttributeName.VsRootSuffix);
			return retval == "-" ? "" : retval;
		}

		/// <summary>
		/// Gets the Visual Studio version, checks that it's been defined.
		/// </summary>
		protected string GetVsVersion()
		{
			return GetStringValue(AttributeName.VsVersion);
		}

		#endregion
	}
}
