/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Build.Omea.Util;

namespace JetBrains.Build.Omea.Infra
{
	public interface ITaskBaseResolved
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the task parameters bag that comes from the unresolved part of the task.
		/// </summary>
		Bag Bag { get; set; }

		#endregion

		#region Operations

		///<summary>
		/// Executes the resolved task, catches and reports its exceptions.
		/// Should not throw.
		///</summary>
		void Execute();

		#endregion
	}
}