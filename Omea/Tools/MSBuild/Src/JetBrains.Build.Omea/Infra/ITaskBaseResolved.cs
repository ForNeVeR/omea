// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
