/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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