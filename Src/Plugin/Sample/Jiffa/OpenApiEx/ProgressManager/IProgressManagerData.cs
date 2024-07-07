// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OpenApiEx
{
	public interface IProgressManagerData
	{
		/// <summary>
		/// Gets the name of the resource type for Progress Item resources.
		/// The use of the corresponding integer Resource Type ID is preferred in most cases.
		/// </summary>
		string ProgressItemResourceTypeName { get; }

		/// <summary>
		/// ID of the resource type for Progress Item resources.
		/// You should use this ID rather than string resource name, wherever possible.
		/// It's unsafe to access this property on startup.
		/// </summary>
		int ProgressItemResourceType { get; }

		/// <summary>
		/// Gets the root resource for all the progress infratructure.
		/// </summary>
		IResource RootResource { get; }
	}
}
