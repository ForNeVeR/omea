// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// Data entries for the Scheduller.
	/// </summary>
	public interface ISchedullerData
	{
		/// <summary>
		/// Gets the resource type … for the … resource.
		/// </summary>
		string SchedullerTaskResourceTypeName { get; }

		/// <summary>
		/// Gets the resource type … for the … resource.
		/// </summary>
		int SchedullerTaskResourceType { get; }

		/// <summary>
		/// Gets the resource type … for the … resource.
		/// </summary>
		string SchedullerGroupResourceTypeName { get; }

		/// <summary>
		/// Gets the resource type … for the … resource.
		/// </summary>
		int SchedullerGroupResourceType { get; }
	}
}
