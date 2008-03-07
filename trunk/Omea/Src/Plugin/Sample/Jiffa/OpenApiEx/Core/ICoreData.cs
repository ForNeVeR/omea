/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// Contains various information, such as names and IDs for widely-used resource type and properties.
	/// </summary>
	public interface ICoreData
	{
		#region Resource Types

		/// <summary>
		/// Gets the name of the resource type for Folder resources.
		/// The use of the corresponding integer Resource Type ID is preferred in most cases.
		/// </summary>
		string FolderResourceTypeName { get; }

		/// <summary>
		/// Gets the ID of the resource type for Folder resources.
		/// You should use this ID rather than string resource name, wherever possible.
		/// </summary>
		int FolderResourceType { get; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets the property name for the Uri property.
		/// The use of the corresponding integer ID is preferred in most cases.
		/// </summary>
		string UriPropertyName { get; }

		/// <summary>
		/// Gets the property ID for the Uri property.
		/// You should use this ID rather than string name, wherever possible.
		/// </summary>
		int UriProperty { get; }

		#endregion

		#region Links

		/// <summary>
		/// Gets the link type name for the Multiple Parent link.
		/// The use of the corresponding integer ID is preferred in most cases.
		/// </summary>
		string MultiParentLinkName { get; }

		/// <summary>
		/// Gets the link type ID for the Multiple Parent link.
		/// You should use this ID rather than string name, wherever possible.
		/// </summary>
		int MultiParentLink { get; }

		#endregion
	}
}