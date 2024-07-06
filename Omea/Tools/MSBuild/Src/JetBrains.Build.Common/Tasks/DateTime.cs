// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Presents the current Date and Time in an arbitrary format.
	/// </summary>
	public class DateTime : TaskBase
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the formatting string.
		/// </summary>
		[Required]
		public string Format { get; set; }

		/// <summary>
		/// The resulting date/time string.
		/// </summary>
		[Output]
		public string Value { get; set; }

		#endregion

		#region Overrides

		/// <summary>
		/// The method to be overriden in inheriting tasks.
		/// Throw an exception in case of an errror.
		/// </summary>
		protected override void ExecuteTask()
		{
			Value = System.DateTime.Now.ToString(Format);
		}

		#endregion
	}
}
