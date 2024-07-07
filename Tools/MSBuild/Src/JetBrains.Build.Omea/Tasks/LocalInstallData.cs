// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Build.Omea.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Tasks
{
	/// <summary>
	/// Gets the product Registry data and installs it into the Windows Registry.
	/// </summary>
	public class LocalInstallData : ProductTask
	{
		#region Attributes

		/// <summary>
		/// The home directory of the product.
		/// </summary>
		[Required]
		public ITaskItem ProductHomeDir
		{
			get
			{
				return Bag.Get<ITaskItem>(AttributeName.ProductHomeDir);
			}
			set
			{
				Bag.Set(AttributeName.ProductHomeDir, value);
			}
		}

		/// <summary>
		/// The installation stage, either “<c>Register</c>” or “<c>Unregister</c>”.
		/// </summary>
		[Required]
		public string Stage
		{
			get
			{
				return Bag.Get<string>(AttributeName.Stage);
			}
			set
			{
				Bag.Set(AttributeName.Stage, value);
			}
		}

		#endregion
	}
}
