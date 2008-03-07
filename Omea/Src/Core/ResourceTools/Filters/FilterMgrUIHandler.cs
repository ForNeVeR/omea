/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.FiltersManagement
{

	#region FilterManagerUIHandler Class

	//-------------------------------------------------------------------------
	// Class which manages conditions, criteria and filters on the
	// available resources.
	//-------------------------------------------------------------------------

	public class FilterManagerUIHandler : IResourceUIHandler
	{
		public void ResourceNodeSelected( IResource res )
		{
			#region Preconditions

			if( res == null )
				throw new ArgumentNullException( "res", "FilterManager -- Input resource in node selection processing can not be NULL" );

			if( !FilterManager.IsViewOrFolder( res ) )
				throw new ArgumentException( "FilterManager -- IResourceTreeHandler is called with the resource of inappropriate type [" + res.Type + "]" );

			#endregion Preconditions

            //  Selecting a tree node (view) in the Shutdown mode is NOP.
            if( Core.State == CoreState.ShuttingDown )
                return;

			string viewName = res.GetPropText( Core.Props.Name );
			if( res.Type == FilterManagerProps.ViewResName )
			{
				if( (res == _lastSelectedView) && (_lastSelectedResult != null) &&
					(Core.WorkspaceManager.ActiveWorkspace == _lastSelectedWorkspace) &&
					(res.GetStringProp( "DeepName" ) != FilterManagerProps.ViewUnreadDeepName) &&
					(!res.HasProp( "ForceExec" )) )
				{
					Core.ResourceBrowser.DisplayConfigurableResourceList( res, _lastSelectedResult, _displayOptions );
				}
				else
				{
					_lastSelectedResult = Core.FilterManager.ExecView( res, viewName );
					ConfigureDisplayOptions( res, viewName, _lastSelectedResult );
					Core.ResourceBrowser.DisplayConfigurableResourceList( res, _lastSelectedResult, _displayOptions );
				}
				_lastSelectedView = res;
				_lastSelectedWorkspace = Core.WorkspaceManager.ActiveWorkspace;
				new ResourceProxy( res ).DeletePropAsync( "ForceExec" );
			}
			else
			{
				Core.ResourceBrowser.DisplayResourceList( res, Core.ResourceStore.EmptyResourceList, viewName, null );
			}
		}

		public bool CanRenameResource( IResource res )
		{
			return (res.GetStringProp( "DeepName" ) != Core.FilterManager.ViewNameForSearchResults);
		}

		public bool ResourceRenamed( IResource view, string newName )
		{
			bool result = false;
			if( view.DisplayName.ToLower() != newName.ToLower() &&
				Core.ResourceStore.FindResources( view.Type, Core.Props.Name, newName ).Count > 0 )
			{
			    MessageBox.Show( "A " + ((view.Type == FilterManagerProps.ViewResName) ? "view" : "view folder") +
					" with such name already exists", "Names Collision", MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
			}
			else if( newName.Length > 0 )
			{
				ResourceProxy proxy = new ResourceProxy( view );
				proxy.SetProp( Core.Props.Name, newName );
				proxy.SetProp( "_DisplayName", newName );
				result = true;
			}
			return result;
		}

		public void ResourcesDropped( IResource targetRes, IResourceList droppedResList )
		{
			throw new NotImplementedException( "Obsolete." );
		}

		public bool CanDropResources( IResource targetRes, IResourceList dragResList )
		{
			throw new NotImplementedException( "Obsolete." );
		}

		internal void ResetSelection()
		{
			_lastSelectedView = null;
			_lastSelectedResult = null;
		}

		#region Impl

		private void ConfigureDisplayOptions( IResource view, string viewName, IResourceList result )
		{
			_displayOptions = new ResourceListDisplayOptions();
			_displayOptions.Caption = !String.IsNullOrEmpty( viewName ) ? viewName : "Unnamed view";

			string deepName = view.GetStringProp( "DeepName" );
			string errorMsg = view.GetStringProp( Core.Props.LastError );
			if( !String.IsNullOrEmpty( errorMsg ) )
			{
				_displayOptions.StatusLine = errorMsg;
				_displayOptions.StatusLineClickHandler = RefreshViewState;
				_lastErrorView = view;
			}
			else
            if( deepName != Core.FilterManager.ViewNameForSearchResults )
			{
				if( !view.HasProp( "DefaultSort" ) )
                {
                    result = SafeSort( result );
					result.Sort( new SortSettings( Core.Props.Date, false ) );
                }

				string content = view.GetStringProp( Core.Props.ContentType );
				if( content == null || content.IndexOf( '|' ) != -1 )
					_displayOptions.SeeAlsoBar = true;

				_displayOptions.DefaultGroupItems = !view.HasProp( "DisableDefaultGroupping" );
			}
			else
			{
				//  If we are in the normal mode (Running) - just show the
				//  result to the user. Otherwise, through the chain of two
				//  event handlers, wait until both events take place:
				//  1. Core is switched to state Running;
				//  2. Text Index is completely loaded.
				if( Core.State == CoreState.Running )
				{
					_displayOptions.HighlightDataProvider = FilterManager.Highlighter;
					_displayOptions.SeeAlsoBar = true;

					string stopWordsMessage = FilterManager.VisualizeStopWords();
					if( FilterManager._lastQueryError != null )
						_displayOptions.StatusLine = FilterManager._lastQueryError;
					else if( stopWordsMessage.Length > 0 )
						_displayOptions.StatusLine = stopWordsMessage;

					/* [yole] This behavior is unusable.
                       Use case: Perform a search in a tab where there are no search results for some type. Omea
                       remembers that the search view was selected. Then the following code switches to the tab where
                       search results are present. Then, as soon as you try to switch to the original tab again,
                       the search view is selected again, the following code runs again, and the selection jumps back.
                       In effect, the original tab becomes unreachable.
                       
				    if( view.HasProp( "RunToTabIfSingleTyped" ) )
					{
						string[] types = ResourceTypeHelper.GetUnderlyingResourceTypes( result );
						if( types != null && types.Length == 1 )
						{
							string tabID = Core.TabManager.FindResourceTypeTab( types[ 0 ] );
							if( tabID != null )
								Core.TabManager.ActivateTab( tabID );
						}
					}
                    */
				}
				else
				{
					_displayOptions.EmptyText = "Loading information from text index... Please wait or switch to another view.";
					Core.TextIndexManager.IndexLoaded += ExecSearchViewLateMarshaller;
				}
			}
		}

        private static IResourceList SafeSort( IResourceList result )
        {
            //  Workaround fixes for OM-13193, OM-13189, OM-13089, etc.
            try
            {
                result.Sort( new SortSettings( Core.Props.Date, false ) );
            }
            catch( InvalidResourceIdException )
            {
                IntArrayList ids = new IntArrayList( result.Count );
                foreach( IResource res in result.ValidResources ) 
                {
                    ids.Add( res.Id );
                }
                result = Core.ResourceStore.ListFromIds( ids, false );
                
            }
            return result;
        }

		private static void ExecSearchViewLateMarshaller( object sender, EventArgs e )
		{
			Core.TextIndexManager.IndexLoaded -= ExecSearchViewLateMarshaller;
			Core.UserInterfaceAP.QueueJob( new MethodInvoker( ExecSearchViewLate ) );
		}

		private static void ExecSearchViewLate()
		{
			//  All this extra checks are the protection around the possibility
			//  to run in the "non-friendly" environment.
			if( Core.LeftSidebar != null && Core.LeftSidebar.DefaultViewPane != null )
			{
				IResource view = Core.LeftSidebar.DefaultViewPane.SelectedNode;
				if( view != null && FilterManager.IsSearchResultsView( view ) )

				{
					new ResourceProxy( view ).SetProp( "ForceExec", true );
					Core.LeftSidebar.DefaultViewPane.SelectResource( view );
				}
			}
		}

		private void RefreshViewState( object sender, EventArgs e )
		{
			if( _lastErrorView != null )
			{
				new ResourceProxy( _lastErrorView ).DeleteProp( Core.Props.LastError );
				Core.ResourceBrowser.HideStatusLine();
				_lastErrorView = null;
			}
		}

		#endregion Impl

		#region Attributes

		private IResource _lastSelectedView = null;
		private IResourceList _lastSelectedResult = null;
		private IResource _lastSelectedWorkspace = null;
		private IResource _lastErrorView = null;

		private ResourceListDisplayOptions _displayOptions;

		#endregion Attributes
	}

	#endregion

	#region SearchViewDragDropHandler Class

	/// <summary>
	/// Drag'n'Drop Handler for the “SearchView” resource type.
	/// </summary>
	public class SearchViewDragDropHandler : IResourceDragDropHandler
	{
		#region IResourceDragDropHandler Members

		public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			if( data.GetDataPresent( typeof(IResourceList) ) )
			{
				// The resources we're dragging
				IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

				// Currently, only the Deleted Resources view has a drop handler
				if( FilterManager.IsDeletedResourcesView( targetResource ) )
				{
					// Delete the resources dropped onto the deleted items view
					foreach( IResource res in dragResources )
					{
						IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( res.Type );
						if( deleter != null )
						{
							try
							{
                                Core.ResourceAP.RunJob( new ResourceDelegate( deleter.DeleteResource ), res );
//								deleter.DeleteResource( res );
							}
							catch( NotImplementedException )
							{
							}
						}
					}
				}
				else
					return;
			}
		}

		public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
		{
			if( data.GetDataPresent( typeof(IResourceList) ) )
			{
				// The resources we're dragging
				IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

				// Currently, only the Deleted Resources view has a drop handler
				if( !FilterManager.IsDeletedResourcesView( targetResource ) )
					return DragDropEffects.None;

				// Collect all the direct and indirect parents of the droptarget; then we'll check to avoid dropping parent on its children
				IntArrayList parentList = IntArrayListPool.Alloc();
                try 
                {
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
                        // Cannot delete resource containers this way
                        if( (Core.ResourceStore.ResourceTypes[ res.Type ].Flags & ResourceTypeFlags.ResourceContainer) != 0 )
                            return DragDropEffects.None; // Cannot delete containers
                    }
                    return DragDropEffects.Move;
                }
                finally
                {
                    IntArrayListPool.Dispose( parentList );
                }
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

	#endregion

	#region ViewFolderDragDropHandler Class

	/// <summary>
	/// Drag'n'Drop Handler for the “ViewFolder” resource type.
	/// </summary>
	public class ViewFolderDragDropHandler : IResourceDragDropHandler
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

				// Expand the node
				new ResourceProxy( targetResource, JobPriority.Immediate ).SetPropAsync( Core.Props.Open, 1 );
			}
		}

        public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            if( data.GetDataPresent( typeof(IResourceList) ) )
            {
                // The resources we're dragging
                IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

                // Check if really dropping over a view-folder
                if( !(targetResource.Type == FilterManagerProps.ViewFolderResName) )
                    return DragDropEffects.None;

                // Collect all the direct and indirect parents of the droptarget; then we'll check to avoid dropping parent on its children
                IntArrayList parentList = IntArrayListPool.Alloc();
                try 
                {
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
                        // Can drop only views and view-folders on view-folders
                        if( !FilterManager.IsViewOrFolder( res ) )
                            return DragDropEffects.None;
                    }
                    return DragDropEffects.Move;
                }
                finally
                {
                    IntArrayListPool.Dispose( parentList );
                }
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