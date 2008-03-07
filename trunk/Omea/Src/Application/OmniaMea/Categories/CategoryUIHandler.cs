/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Categories
{
	internal class CategoryUIHandler : IResourceUIHandler, IResourceDragDropHandler
	{
		private IResource _lastCategory;
		private IResourceList _lastCategoryList;
		private bool _lastCategoryListRecursive;
		private CategoryManager _categoryManager;

		public CategoryUIHandler( CategoryManager categoryManager )
		{
			_categoryManager = categoryManager;
		}

		public void ResourceNodeSelected( IResource res )
		{
			bool categoryListRecursive = res.HasProp( _categoryManager.PropShowContentsRecursively );
			if( res != _lastCategory || categoryListRecursive != _lastCategoryListRecursive )
			{
				_lastCategory = res;
				_lastCategoryListRecursive = categoryListRecursive;
				if( _lastCategoryListRecursive )
				{
					_lastCategoryList = BuildRecursiveContentsList( res );
				}
				else
				{
					_lastCategoryList = res.GetLinksOfTypeLive( null, _categoryManager.PropCategory );
				}
				_lastCategoryList = ResourceTypeHelper.ExcludeUnloadedPluginResources( _lastCategoryList );
				_lastCategoryList.Sort( new SortSettings( Core.Props.Date, true ) );
			}
			ResourceListDisplayOptions options = new ResourceListDisplayOptions();
			options.Caption = "Resources in category " + res.DisplayName;
			if( _lastCategoryListRecursive )
			{
				options.Caption += " and subcategories";
			}
			options.SeeAlsoBar = true;

			Core.ResourceBrowser.DisplayConfigurableResourceList( res, _lastCategoryList, options );
		}

		private IResourceList BuildRecursiveContentsList( IResource res )
		{
			IResourceList result = res.GetLinksOfTypeLive( null, _categoryManager.PropCategory );
			foreach( IResource child in res.GetLinksTo( "Category", Core.Props.Parent ) )
			{
				result = result.Union( BuildRecursiveContentsList( child ), true );
			}
			return result;
		}

		public bool CanRenameResource( IResource res )
		{
			return true;
		}

		public bool ResourceRenamed( IResource res, string newName )
		{
			return Core.CategoryManager.CheckRenameCategory( Core.MainWindow, res, newName );
		}

		public bool CanDropResources( IResource targetResource, IResourceList dragResources )
		{
			throw new NotImplementedException( "Obsolete." );
		}

		public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
		{
			throw new NotImplementedException( "Obsolete." );
		}

		/// <summary>
		/// Drops a list of categories (resources of other types may be present in the list, but they are ignored) over a category, or a cat-tree-root, or a cat-folder.
		/// Respects the parent constraints over the content-type, changes the content-type if needed and possible.
		/// </summary>
		/// <returns>Whether anything could be dropped.</returns>
		internal bool HandleDropCategoriesOnCategory( IResourceList resDroppedRaw, IResource targetResource )
		{
			// C-type restriction of the droptarget (only categories are restricted)
			string sParentContentType = null;
			if( targetResource.Type == "Category" )
				sParentContentType = targetResource.GetStringProp( Core.Props.ContentType );
			else if( targetResource.Type == "ResourceTreeRoot" )
			{ // If it's a resource tree root, check for its c-type constraint
				string sCType = targetResource.GetStringProp( "RootResourceType" );
				if( (sCType != null) && (sCType != "Category") && (sCType.StartsWith( "Category" )) )
					sParentContentType = sCType.Substring( "Category".Length + 1 ); // Cut the leading “Category:” and leave the c-type
			}

			// Here we process categories only
			IResourceList resDropped = resDroppedRaw.Intersect( Core.ResourceStore.GetAllResources( "Category" ) );

			//////////////////////
			// Check Constraints
			// Ensure that the categories can really be dropped in here
			ArrayList arChangeCTypeFor = new ArrayList(); // A list of those dropped cats whose c-type does not fit the parent's one and must thus be changed
			foreach( IResource res in resDropped )
			{
				// Don't allow dropping onto children
				if( IsParentCategory( res, targetResource ) )
				{
					MessageBox.Show( Core.MainWindow, "A category cannot be made a child of itself.", Core.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
					return false;
				}

				// Check if the dropped-categories (subcategory-wannabe) content-type is broader than the parent's content-type constraint
				string childContent = res.GetStringProp( Core.Props.ContentType );
				if( (sParentContentType != null) && (childContent != sParentContentType) )
				{ // The child has another limitation over the content, or has gotten none at all
					arChangeCTypeFor.Add( res );

					// The change will fail if there're unfit resources
					if( _categoryManager.GetUnmatchingResources( res, sParentContentType ) > 0 )
					{
						MessageBox.Show( Core.MainWindow, String.Format( "You can only drop those categories that contain resources of type “{0}” here.", Core.ResourceStore.ResourceTypes[ sParentContentType ].DisplayName ), Core.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Stop );
						return false;
					}
				}
			}

			//////////////////////
			// Apply Constraints
			// Narrow the content-type restriction wherever needed
			if( arChangeCTypeFor.Count != 0 )
			{
				if( sParentContentType == null )
					throw new InvalidOperationException( "Parent resource type is undefined." );

				// Prompt of the c-type change
				if( MessageBox.Show( Core.MainWindow, String.Format( "The category you're dropping onto restricts its contents to “{0}” resources only.\nThe same restriction will be applied to the dropped categories.", sParentContentType ), Core.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation ) != DialogResult.OK )
					return false;

				// Narrow the c-type
				foreach( IResource res in arChangeCTypeFor )
					_categoryManager.SetContentTypeRecursive( res, sParentContentType );
			}

			//////////
			// Drop!
			foreach( IResource res in resDropped )
				new ResourceProxy( res, JobPriority.Immediate ).SetPropAsync( Core.Props.Parent, targetResource );

			return true; // Dropped
		}

		internal bool HandleDropResourceOnCategory( IResourceList resDroppedRaw, IResource category )
		{
			// Categories have already been processed separately
			IResourceList resDropped = resDroppedRaw.Minus( Core.ResourceStore.GetAllResources( "Category" ) );

			// Content-type of the target category
			string contentType = category.GetStringProp( "ContentType" );

			// Constrain
			foreach( IResource res in resDropped )
			{
				if( Core.ResourceStore.ResourceTypes[ res.Type ].HasFlag( ResourceTypeFlags.Internal ) )
					continue;

				if( contentType != null && contentType != res.Type )
				{
					DialogResult dr = MessageBox.Show( Core.MainWindow,
					                                   String.Format( "The category “{0}” is configured to contain only “{1}” resources. Do you wish to change it to a general category?", category.DisplayName, contentType ), "Add Resource to Category", MessageBoxButtons.YesNo, MessageBoxIcon.Question );
					if( dr == DialogResult.No )
						return false;

					Debug.WriteLine( "Changing parent for category " + category.Id );
					_categoryManager.SetContentTypeRecursive( category, null );
					new ResourceProxy( category, JobPriority.Immediate ).SetPropAsync( "Parent", Core.CategoryManager.RootCategory );
					contentType = null;
					break;
				}
			}

            //  Presence of Alt modifier means "movement" of a resource to a category,
            //  not an addition of another one.
            bool isMove = (Control.ModifierKeys & Keys.Alt) > 0;
            foreach( IResource res in resDropped )
			{
                if( !Core.ResourceStore.ResourceTypes[ res.Type ].HasFlag( ResourceTypeFlags.Internal ) )
                {
                    if( !isMove )
                        Core.CategoryManager.AddResourceCategory( res, category );
                    else
                        Core.CategoryManager.SetResourceCategory( res, category );
                }
			}

			return true;
		}

		private static bool IsParentCategory( IResource parent, IResource child )
		{
			while( child != null )
			{
				if( child == parent )
					return true;

				child = (IResource)child.GetProp( "Parent" );
			}
			return false;
		}

		#region IResourceDragDropHandler Members

		public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			if( targetResource == null )
				return;

			if( data.GetDataPresent( typeof(IResourceList) ) )
			{
				IResourceList droppedResources = (IResourceList)data.GetData( typeof(IResourceList) );

				// Drop the categories as subcategories, and attach generic resources to the category
				bool addedChildren = false;
				addedChildren = HandleDropCategoriesOnCategory( droppedResources, targetResource ) || addedChildren;
				addedChildren = HandleDropResourceOnCategory( droppedResources, targetResource ) || addedChildren;
				if( addedChildren )
					new ResourceProxy( targetResource, JobPriority.Immediate ).SetPropAsync( _categoryManager.PropCategoryExpanded, 1 );
			}
		}

		public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			if( data.GetDataPresent( typeof(IResourceList) ) )
			{
				// The resources we're dragging
				IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

				// Check the droptarget resource type (either a cat, or a root resource type for the cat or one of its filtrants)
				if( !((targetResource.Type == "Category") || ((targetResource.Type == "ResourceTreeRoot") && (targetResource.HasProp( "RootResourceType" )) && (targetResource.GetStringProp( "RootResourceType" ).StartsWith( "Category" )))) )
					return DragDropEffects.None;

				// Collect all the direct and indirect parents of the droptarget; then we'll check to avoid dropping parent on its children
				IntArrayList parentList = new IntArrayList();
				IResource parent = targetResource;
				while( parent != null )
				{
					parentList.Add( parent.Id );
					parent = parent.GetLinkProp( Core.Props.Parent );
				}

				bool bAllDroppable = true; // Feeds or groups are being dragged
				foreach( IResource res in dragResources )
				{
					// Dropping parent over its child?
					if( parentList.IndexOf( res.Id ) >= 0 )
						return DragDropEffects.None;
				}
				return bAllDroppable ? DragDropEffects.Move : DragDropEffects.None;
			}
			else
				return DragDropEffects.None;
		}

		public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
		{
			if( !dataObject.GetDataPresent( typeof(string) ) )
			{
				StringBuilder sb = StringBuilderPool.Alloc();
				try
				{
					foreach( IResource resource in dragResources )
					{
						if( sb.Length != 0 )
							sb.Append( ", " );
						string text = resource.DisplayName;
						if( text.IndexOf( ' ' ) > 0 )
							sb.Append( "“" + text + "”" );
						else
							sb.Append( text );
					}
					dataObject.SetData( sb.ToString() );
				}
				finally
				{
					StringBuilderPool.Dispose( sb );
				}
			}
		}

		#endregion
	}

	internal class CategoryRootUIHandler : IResourceUIHandler
	{
		public void ResourceNodeSelected( IResource res )
		{
			Core.ResourceBrowser.DisplayResourceList( res, Core.ResourceStore.EmptyResourceList,
			                                          res.DisplayName, null );
		}

		public bool CanDropResources( IResource targetResource, IResourceList dragResources )
		{
			throw new NotImplementedException( "Obsolete." );
		}

		public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
		{
			throw new NotImplementedException( "Obsolete." );
		}

		public bool CanRenameResource( IResource res )
		{
			return false;
		}

		public bool ResourceRenamed( IResource res, string newName )
		{
			return false;
		}
	}
}