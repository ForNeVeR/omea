// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.CoreServicesEx.Core
{
	/// <summary>
	/// Core data implementation.
	/// </summary>
	internal class CoreData : ICoreData
	{
		#region Data

		/// <summary>
		/// The lazy-init singleton instance.
		/// </summary>
		protected static ICoreData _instance = null;

		internal static readonly string _sFolderResourceTypeName = "Folder";

		protected int _nFolderResourceType = 0;

		internal static readonly string _sUriPropertyName = "Uri";

		protected int _nUriProperty = 0;

		internal static readonly string _sMultiParentLinkName = "MultiParent";

		protected int _nMultiParentLink = 0;

		#endregion

		#region Init

		protected CoreData()
		{
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the singleton instance.
		/// </summary>
		public static ICoreData Instance
		{
			get
			{
				return _instance ?? (_instance = new CoreData());
			}
		}

		#endregion

		#region ICoreData Members

		/// <summary>
		/// Gets the name of the resource type for Folder resources.
		/// The use of the corresponding integer Resource Type ID is preferred in most cases.
		/// </summary>
		public string FolderResourceTypeName
		{
			get
			{
				return _sFolderResourceTypeName;
			}
		}

		/// <summary>
		/// Gets the ID of the resource type for Folder resources.
		/// You should use this ID rather than string resource name, wherever possible.
		/// </summary>
		public int FolderResourceType
		{
			get
			{
				return _nFolderResourceType;
			}
		}

		/// <summary>
		/// Gets the property name for the Uri property.
		/// The use of the corresponding integer ID is preferred in most cases.
		/// </summary>
		public string UriPropertyName
		{
			get
			{
				return _sUriPropertyName;
			}
		}

		/// <summary>
		/// Gets the property ID for the Uri property.
		/// You should use this ID rather than string name, wherever possible.
		/// </summary>
		public int UriProperty
		{
			get
			{
				return _nUriProperty;
			}
		}

		/// <summary>
		/// Gets the link type name for the Multiple Parent link.
		/// The use of the corresponding integer ID is preferred in most cases.
		/// </summary>
		public string MultiParentLinkName
		{
			get
			{
				return _sMultiParentLinkName;
			}
		}

		/// <summary>
		/// Gets the link type ID for the Multiple Parent link.
		/// You should use this ID rather than string name, wherever possible.
		/// </summary>
		public int MultiParentLink
		{
			get
			{
				return _nMultiParentLink;
			}
		}

		#endregion
	}
}
