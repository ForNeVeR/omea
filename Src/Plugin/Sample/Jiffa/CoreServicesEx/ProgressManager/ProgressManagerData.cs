// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.CoreServicesEx.ProgressManager
{
	/// <summary>
	/// Hands out various Progress Manager data.
	/// </summary>
	internal class ProgressManagerData : IProgressManagerData
	{
		/// <summary>
		/// Stores the lazy-init singleton instance.
		/// </summary>
		protected static IProgressManagerData _instance = null;

		/// <summary>
		/// Non-public singleton ctor.
		/// </summary>
		protected ProgressManagerData()
		{
		}

		/// <summary>
		/// Name of the resource type for Progress Item resources.
		/// The use of the corresponding integer Resource Type ID is preferred in most cases.
		/// </summary>
		internal static readonly string _sProgressItemResourceTypeName = "ProgressManager.Item";

		/// <summary>
		/// Lazy-init backup for the corresponding Resource ID property.
		/// </summary>
		protected int _nProgressItemResourceType = 0;

		/// <summary>
		/// Name of the resource type for Folder resources.
		/// The use of the corresponding integer Resource Type ID is preferred in most cases.
		/// </summary>
		internal static readonly string _sFolderResourceTypeName = "Folder"; // TODO: use the common “Folder” restype, when common infrastructure gets avail

		protected int _nFolderResourceType = 0;

		internal static readonly string _sMultiParentLinkName = "MultiParent"; // TODO: use the common “Multiparent” restype

		protected int _nMultiParentLink = 0;

		/// <summary>
		/// Gets the singleton instance of the object.
		/// </summary>
		public static IProgressManagerData Instance
		{
			get
			{
				return _instance ?? (_instance = new ProgressManagerData());
			}
		}

		/// <summary>
		/// Gets the name of the resource type for Progress Item resources.
		/// The use of the corresponding integer Resource Type ID is preferred in most cases.
		/// </summary>
		public string ProgressItemResourceTypeName
		{
			get
			{
				return _sProgressItemResourceTypeName;
			}
		}

		/// <summary>
		/// ID of the resource type for Progress Item resources.
		/// You should use this ID rather than string resource name, wherever possible.
		/// It's unsafe to access this property on startup.
		/// </summary>
		public int ProgressItemResourceType
		{
			get
			{
				if(_nProgressItemResourceType == 0)
					_nProgressItemResourceType = OpenAPI.Core.ResourceStore.ResourceTypes[_sProgressItemResourceTypeName].Id;
				return _nProgressItemResourceType;
			}
		}

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
		/// ID of the resource type for Folder resources.
		/// You should use this ID rather than string resource name, wherever possible.
		/// It's unsafe to access this property on startup.
		/// </summary>
		public int FolderResourceType
		{
			get
			{
				if(_nFolderResourceType == 0)
					_nFolderResourceType = OpenAPI.Core.ResourceStore.ResourceTypes[_sFolderResourceTypeName].Id;
				return _nFolderResourceType;
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
		/// It's unsafe to access this property on startup.
		/// </summary>
		public int MultiParentLink
		{
			get
			{
				if(_nMultiParentLink == 0)
					_nMultiParentLink = OpenAPI.Core.ResourceStore.PropTypes[_sMultiParentLinkName].Id;
				return _nMultiParentLink;
			}
		}

		/// <summary>
		/// Gets the root resource for all the progress infratructure.
		/// </summary>
		public IResource RootResource
		{
			get
			{
				return OpenAPI.Core.ResourceTreeManager.GetRootForType(_sProgressItemResourceTypeName);
			}
		}
	}
}
