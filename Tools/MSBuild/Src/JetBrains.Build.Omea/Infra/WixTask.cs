// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Infra
{
	/// <summary>
	/// Implements a task that requites paths to the Windows Installer XML.
	/// </summary>
	public abstract class WixTask : ProbingTask
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the folder where the Windows Installer XML binaries are located.
		/// </summary>
		[Required]
		public ITaskItem WixBinariesDir
		{
			get
			{
				return Bag.Get<ITaskItem>(AttributeName.WixBinariesDir);
			}
			set
			{
				Bag.Set(AttributeName.WixBinariesDir, value);
			}
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Gets the list of attributes that must contain the probing directories.
		/// </summary>
		protected override ICollection<AttributeName> ProbingDirectoryAttributes
		{
			get
			{
				var retval = new List<AttributeName>(base.ProbingDirectoryAttributes);
				retval.Add(AttributeName.WixBinariesDir);
				return retval;
			}
		}

		#endregion
	}
}
