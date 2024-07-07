// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections.Generic;

using JetBrains.Build.Omea.Util;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JetBrains.Build.Omea.Infra
{
	/// <summary>
	/// Implements a task that requires the path to the product assemblies.
	/// </summary>
	public abstract class ProductTask : ProbingTask
	{
		#region Attributes

		/// <summary>
		/// The list of assemblies to be published, as well as their characteristics.
		/// </summary>
		[Required]
		public ITaskItem AllAssembliesXml
		{
			get
			{
				return (TaskItem)Bag.Get<TaskItemByValue>(AttributeName.AllAssembliesXml);
			}
			set
			{
				Bag.Set(AttributeName.AllAssembliesXml, new TaskItemByValue(value.ItemSpec));
			}
		}

		/// <summary>
		/// Gets or sets the folder where the product binaries are located.
		/// </summary>
		[Required]
		public ITaskItem ProductBinariesDir
		{
			get
			{
				return Bag.Get<ITaskItem>(AttributeName.ProductBinariesDir);
			}
			set
			{
				Bag.Set(AttributeName.ProductBinariesDir, value);
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
				retval.Add(AttributeName.ProductBinariesDir);
				return retval;
			}
		}

		#endregion
	}
}
