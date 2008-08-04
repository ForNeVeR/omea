/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.FiltersManagement
{
	/// <summary>
	/// Manages updating unread counts for search views.
	/// </summary>
    public class ViewUnreadCountProvider: IUnreadCountProvider
    {
        private readonly UnreadManager _unreadManager;
        private readonly IResourceList _allViews;
        private readonly IntHashTable _viewToUnreadResources = new IntHashTable();  // view ID -> view unread resources
        private readonly Dictionary<IResourceList,IResource> _unreadResourcesToView = new Dictionary<IResourceList,IResource>();  // view unread resources -> view resource

	    public ViewUnreadCountProvider()
	    {
            _unreadManager = Core.UnreadManager as UnreadManager;
            _allViews = Core.FilterRegistry.GetViews();
            _allViews.ResourceAdded += HandleViewAdd;
            _allViews.ResourceDeleting += HandleViewDelete;
            _allViews.ResourceChanged += HandleViewChange;

            UpdateViews();
            SetupMidnightCountersUpdate();
            Core.TextIndexManager.IndexLoaded += TextIndexLoaded;
        }

        private void TextIndexLoaded(object sender, EventArgs e)
        {
            Core.TextIndexManager.IndexLoaded -= TextIndexLoaded;
            UpdateTextViews();
        }

        public IResourceList GetResourcesForView( IResource viewResource )
        {
            IResourceList resList = (IResourceList) _viewToUnreadResources [viewResource.Id];
            return resList ?? Core.ResourceStore.EmptyResourceList;
        }

        #region Midnight counter invalidation
        private void  SetupMidnightCountersUpdate()
        {
            DateTime    startingTime = DateTime.Today.AddDays( 1.0 ).AddSeconds( 5.0 );
            Trace.WriteLine( "Queued midnight update of unread counters for " + startingTime );
            Core.ResourceAP.QueueJobAt( startingTime, new MethodInvoker( InvalidateCounters ) );
        }

	    private void InvalidateCounters()
	    {
	        Trace.WriteLine( "Performing midnight update of unread counters" );

            ClearAllList();
            UpdateViews();
            if( Core.TextIndexManager.IsIndexPresent() )
                UpdateTextViews();

            foreach( IResource view in Core.ResourceStore.GetAllResources( FilterManagerProps.ViewResName ) )
            {
	            Trace.WriteLine( "Invalidating unread counters for view [" + view.GetPropText( Core.Props.Name ) + "]" );
                Core.UnreadManager.InvalidateUnreadCounter( view );
            }
            SetupMidnightCountersUpdate();
	        Trace.WriteLine( "Update of unread counters finished" );
        }
        #endregion Midnight counter invalidation

        #region Views list changes
        private void HandleViewAdd( object sender, ResourceIndexEventArgs e )
	    {
            UpdateView( e.Resource, false );
	    }

	    private void HandleViewDelete( object sender, ResourceIndexEventArgs e )
	    {
            UpdateView( e.Resource, true );
	    }

        private void HandleViewChange( object sender, ResourcePropIndexEventArgs e )
        {
            UpdateView( e.Resource, false );
        }

        private void UpdateView( IResource view, bool remove )
        {
            IResourceList resList = (IResourceList) _viewToUnreadResources[ view.Id ];
            if( resList != null ) // possible on view's creation
            {
                _unreadResourcesToView.Remove( resList );
                _viewToUnreadResources.Remove( view.Id );

                DetachFromList( resList );
                resList.Dispose();
            }

            if ( !remove && ViewCanBeUnread( view ) )
            {
                resList = ComputeList( view );

                _unreadResourcesToView[ resList ] = view;
                _viewToUnreadResources[ view.Id ] = resList;
                AttachToList( resList );

                _unreadManager.InvalidateUnreadCounter( view );
            }
        }

        #endregion Views list changes

        private static bool CanUpdateTextView( IResource view )
        {
            return ViewCanBeUnread( view ) && 
                   Core.TextIndexManager.IsIndexPresent() &&
                   FilterRegistry.HasQueryCondition( view );
        }

        private void UpdateTextViews()
        {
            foreach( IResource view in _allViews )
            {
                if ( CanUpdateTextView( view ) )
                {
                    IResourceList resList = ComputeList( view );
                    CrossRefItems( view, resList );
                    AttachToList( resList );
                }
            }
        }

        private void UpdateViews()
        {
            #region Preconditions
            if( _unreadResourcesToView.Count != 0 || _viewToUnreadResources.Count != 0 )
                throw new ApplicationException( "ViewsUnreadCountProvider -- Contract violation - list are not disposed." );
            #endregion Preconditions

            //  By default, we initially only account for non-trextindex views,
            //  since text index ones requre handling of the "text index ready" event.
            foreach( IResource view in _allViews )
            {
                if ( ViewCanBeUnread( view ) && !FilterRegistry.HasQueryCondition( view ) )
                {
                    IResourceList resList = ComputeList( view );
                    CrossRefItems( view, resList );
                    AttachToList( resList );
                }
            }
        }

        private void CrossRefItems( IResource view, IResourceList resList )
        {
            _unreadResourcesToView[ resList ] = view;
            _viewToUnreadResources[ view.Id ] = resList;
        }

        #region Unread resource in list handling

        private void HandleViewUnreadAdded( object sender, ResourceIndexEventArgs e )
	    {
            IResource viewResource = _unreadResourcesToView[ (IResourceList)sender ];
            _unreadManager.UpdateCountForView( viewResource, e.Resource, 1 );
	    }

        private void HandleViewUnreadDeleting( object sender, ResourceIndexEventArgs e )
        {
            IResource viewResource = _unreadResourcesToView[ (IResourceList)sender ];
            if ( e.Resource != null )
            {
                _unreadManager.UpdateCountForView( viewResource, e.Resource, -1 );
            }
        }

        /// <summary>
        /// Handle adding or removal of the resource to/from a workspace. It is indicated by
        /// the link with property Core.WorkspaceManager.Props.WorkspaceVisible
        /// </summary>
        private void HandleViewUnreadChanged( object sender, ResourcePropIndexEventArgs e )
        {
            LinkChange[] changes = e.ChangeSet.GetLinkChanges( ((WorkspaceManager) Core.WorkspaceManager).Props.WorkspaceVisible );
            if ( changes != null )
            {
                IResource viewResource = _unreadResourcesToView [ (IResourceList)sender ];
                for( int i = 0; i < changes.Length; i++ )
                {
                    _unreadManager.AdjustViewWorkspaceCounter( e.Resource, viewResource, changes[ i ].TargetId,
                        (changes[ i ].ChangeType == LinkChangeType.Add) ? 1 : -1 );
                }
            }
        }
        #endregion Unread resource in list handling

        #region Impl

        private void ClearAllList()
        {
            foreach( IResourceList list in _unreadResourcesToView.Keys )
            {
                DetachFromList( list );
                list.Dispose();
            }
            _unreadResourcesToView.Clear();
            _viewToUnreadResources.Clear();
        }

        private void DetachFromList( IResourceList list )
        {
            list.ResourceAdded -= HandleViewUnreadAdded;
            list.ResourceDeleting -= HandleViewUnreadDeleting;
            list.ResourceChanged -= HandleViewUnreadChanged ;
        }

        private void AttachToList( IResourceList list )
        {
            list.ResourceAdded += HandleViewUnreadAdded;
            list.ResourceDeleting += HandleViewUnreadDeleting;
            list.ResourceChanged += HandleViewUnreadChanged;
        }

        private static IResourceList ComputeList( IResource view )
        {
            IResourceList list = Core.FilterEngine.ExecView( view, null, SelectionType.Live );
            list = ResourceTypeHelper.ExcludeUnloadedPluginResources( list );
            list = list.Minus( Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsDeleted ) );
            list = list.Intersect( Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsUnread ), true );
            return list;
        }

        private static bool ViewCanBeUnread( IResource viewResource )
        {
            string contentType = viewResource.GetPropText( Core.Props.ContentType );
            string contentLinks = viewResource.GetPropText( "ContentLinks" );
            if ( contentType.Length > 0 && contentLinks.Length == 0 )
            {
                string[] contentTypes = contentType.Split( '|' );
                for( int i = 0; i < contentTypes.Length; i++ )
                {
                    string ct = contentTypes[ i ];
                    if ( !Core.ResourceStore.ResourceTypes.Exist( ct ) ||
                          Core.ResourceStore.ResourceTypes[ ct ].HasFlag( ResourceTypeFlags.CanBeUnread ) )
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }
        #endregion Impl
    }
}
