/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Base;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// The "Views and Categories" pane.
	/// </summary>
	public class ViewsCategoriesPane: ResourceTreePaneBase
	{
        private WorkspaceCategoryFilter _workspaceCategoryFilter;
        private ViewsCategoriesChildProvider _vcChildProvider;
        private IResource _lastActiveWorkspace = null;
        private readonly CategoryTotalCountDecorator _totalCountDecorator = new CategoryTotalCountDecorator();

        public ViewsCategoriesPane()
	    {
            AddNodeDecorator( new TextQueryViewDecorator() );
	        AddNodeDecorator( _totalCountDecorator );
	    }

        public void AddContentTypes( string[] types )
        {
            _totalCountDecorator.AddContentTypes( types );
        }

        public bool ShowWorkspaceOtherView
        {
            get 
            { 
                return _dataProvider.ResourceChildProvider != null;
            }
            set
            {
                if ( _rootResource == Core.ResourceTreeManager.ResourceTreeRoot )
                {
                    if ( value )
                    {
                        if ( _vcChildProvider == null )
                        {
                            _vcChildProvider = new ViewsCategoriesChildProvider();
                        }
                        _dataProvider.ResourceChildProvider = _vcChildProvider;
                    }
                    else
                    {
                        _dataProvider.ResourceChildProvider = null;
                    }
                }
            }
        }
        public override void SetActiveWorkspace( IResource workspace )
	    {
            if ( _lastActiveWorkspace == workspace )
            {
                return;
            }
            _lastActiveWorkspace = workspace;
            if ( _resourceTree.Filters.Contains( _workspaceCategoryFilter ) )
            {
                _resourceTree.Filters.Remove( _workspaceCategoryFilter );
                _workspaceCategoryFilter.Dispose();
            }
    
            if ( workspace != null )
            {
                IResourceList workspaceCategories = Core.WorkspaceManager.GetWorkspaceResourcesLive( workspace, 
                    "Category" ).Union( Core.WorkspaceManager.GetWorkspaceResourcesLive( workspace, "ResourceTreeRoot" ) );
                _workspaceCategoryFilter = new WorkspaceCategoryFilter( workspace, workspaceCategories );
                _resourceTree.Filters.Add( _workspaceCategoryFilter );
            }
            if ( ShowWorkspaceOtherView )
            {
                _dataProvider.RebuildTree();
            }
            AsyncUpdateSelection();
        }
	}

    /// <summary>
    /// The filter which accepts categories only if they, or their children, are linked 
    /// to the specified workspace.
    /// </summary>
    internal class WorkspaceCategoryFilter: IJetListViewNodeFilter, IDisposable
    {
        private readonly IResource _workspace;
        private readonly IResourceList _workspaceCategories;
        private readonly List<int> _workspaceCategoryList = new List<int>();
        
        internal WorkspaceCategoryFilter( IResource workspace, IResourceList workspaceCategories )
        {
            _workspace = workspace;
            _workspaceCategories = workspaceCategories;
            _workspaceCategories.ResourceAdded += OnWorkspaceCategoryAdded;
            _workspaceCategories.ResourceDeleting += OnWorkspaceCategoryDeleting;
            UpdateFilterList( null );
        }

        public void Dispose()
        {
            _workspaceCategories.ResourceAdded -= OnWorkspaceCategoryAdded;
            _workspaceCategories.ResourceDeleting -= OnWorkspaceCategoryDeleting;
        }

        /**
         * Builds the list of categories added to the workspace and their parents.
         */        
        
        private void UpdateFilterList( IResource removedCategory )
        {
            _workspaceCategoryList.Clear();
            foreach( IResource res in _workspaceCategories )
            {
                if ( res == removedCategory )
                    continue;

                IResource parentRes = res;
                while( parentRes != null && IsCategoryOrRoot( parentRes ) )
                {
                    if ( _workspaceCategoryList.IndexOf( parentRes.Id ) < 0 )
                    {
                        _workspaceCategoryList.Add( parentRes.Id );
                    }
                    parentRes = parentRes.GetLinkProp( Core.Props.Parent );
                }
            }
            if ( FilterChanged != null )
            {
                FilterChanged( this, EventArgs.Empty );
            }
        }

        private static bool IsCategoryOrRoot( IResource parentRes )
        {
            if ( parentRes.Type == "Category" )
            {
                return true;
            }
            if ( parentRes.Type == "ResourceTreeRoot" )
            {
                return parentRes.GetPropText( "RootResourceType" ).StartsWith( "Category" );
            }
            return false;
        }

        /**
         * Updates the filter list when categories are added or removed from the workspace.
         */
        
        private void OnWorkspaceCategoryAdded( object sender, ResourceIndexEventArgs e )
        {
            UpdateFilterList( null );
        }

        private void OnWorkspaceCategoryDeleting( object sender, ResourceIndexEventArgs e )
        {
            UpdateFilterList( e.Resource );
        }

        public bool AcceptNode( JetListViewNode node )
        {
            IResource res = (IResource) node.Data;
            if ( IsCategoryOrRoot( res ) )
            {
                if ( _workspaceCategoryList.IndexOf( res.Id ) >= 0 )
                {
                    return true;
                }
                
                IResource parent = res.GetLinkProp( "Parent" );
                while( parent != null && IsCategoryOrRoot( parent ) )
                {
                    if ( parent.HasLink( "InWorkspaceRecursive", _workspace ) )
                    {
                        return true;
                    }
                    parent = parent.GetLinkProp( Core.Props.Parent );
                }
                return false;
            }
            return true;
        }

        public event EventHandler FilterChanged;
    }

    /// <summary>
    /// The provider which allows showing the "Other" workspace view in the 
    /// Views and Categories pane when it is not possible to show that view
    /// in any other pane.
    /// </summary>
    internal class ViewsCategoriesChildProvider: IJetResourceChildProvider
    {
        public IResourceList GetChildResources( IResource parent )
        {
            if ( parent == Core.ResourceTreeManager.ResourceTreeRoot )
            {
                IResource ws = Core.WorkspaceManager.ActiveWorkspace;
                if ( ws != null )
                {
                    IResourceList children = parent.GetLinksToLive( null, Core.Props.Parent );
                    IResourceList otherView = ws.GetLinksOfType( "WorkspaceOtherView", "InWorkspace" );
                    Debug.Assert( otherView.Count == 1 );
                    children = children.Union( otherView );
            
                    string nodeSort = Core.ResourceTreeManager.GetResourceNodeSort( parent );
                    if ( nodeSort != null )
                    {
                        children.Sort( nodeSort );
                    }
                    return children;
                }
            }
            return null;
        }
    }

	#region ViewsCategoriesRootDragDropHandler Class

	/// <summary>
	/// Drag'n'Drop handler for the root of the “Views and Categories” sidebar pane.
	/// </summary>
	public class ViewsCategoriesRootDragDropHandler : IResourceDragDropHandler
	{
		#region IResourceDragDropHandler Members

		public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			if( data.GetDataPresent( typeof(IResourceList) ) )
			{
				// The resources we're dragging
				IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

				// Mount the items
				foreach( IResource res in dragResources )
					new ResourceProxy( res, JobPriority.Immediate ).SetPropAsync( Core.Props.Parent, targetResource );
			}
		}

		public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			if( data.GetDataPresent( typeof(IResourceList) ) )
			{
				// The resources we're dragging
				IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

				// Check if really dropping over our resource (resource tree root for Views'n'Cats)
				if( !(targetResource == Core.ResourceTreeManager.ResourceTreeRoot) )
					return DragDropEffects.None;

				// Collect all the direct and indirect parents of the droptarget; then we'll check to avoid dropping parent on its children
				List<int> parentList = new List<int>();
				IResource parent = targetResource;
				while( parent != null )
				{
					parentList.Add( parent.Id );
					parent = parent.GetLinkProp( Core.Props.Parent );
				}

				// Check
				foreach( IResource res in dragResources )
				{
					// Dropping parent over its child?
					if( parentList.IndexOf( res.Id ) >= 0 )
						return DragDropEffects.None;
					// Can drop only views, view-folders, and category-tree-roots on the views'n'cats tree root
					if( !(
						(FilterRegistry.IsViewOrFolder( res ))
							|| ((res.Type == "ResourceTreeRoot") && (res.HasProp( "RootResourceType" )) && (res.GetStringProp( "RootResourceType" ).StartsWith( "Category" )))
						) )
						return DragDropEffects.None;
				}
				return DragDropEffects.Move;
			}

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

	#endregion
}
