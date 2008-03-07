/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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