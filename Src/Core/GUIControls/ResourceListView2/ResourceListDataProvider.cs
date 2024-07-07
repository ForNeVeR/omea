// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceStore;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Fills ResourceListView2 with data from a plain resource list.
	/// </summary>
    public class ResourceListDataProvider: IResourceDataProvider
	{
	    protected JetListView _listView;
	    protected IResourceList _resourceList;
	    protected ResourceComparer _lastComparer;
        private ResourcePropsColumn _lastSortColumn;
	    protected bool _disposed;
        private bool _nodesAdded = false;
        private readonly Hashtable _lastSortDirections = new Hashtable();
        private SortSettings _curSortSettings;

	    public ResourceListDataProvider( IResourceList resourceList )
	    {
            Guard.NullArgument( resourceList, "resourceList" );
	        _resourceList = resourceList;
	    }

	    public IResourceList ResourceList
        {
            get { return _resourceList; }
        }

        public void SetInitialSort( SortSettings sortSettings )
        {
            if ( _listView != null )
            {
                throw new InvalidOperationException( "SetInitialSort() must be called before FillResources()" );
            }
            _curSortSettings = sortSettings;
        }

        public SortSettings SortSettings
        {
            get { return _curSortSettings; }
        }

	    public event EventHandler ResourceCountChanged;
        public event EventHandler SortChanged;

        public void FillResources( ResourceListView2 listView )
	    {
            if ( _listView != null )
            {
                throw new InvalidOperationException( "Attempt to attach a ResourceListDataProvider which is already attached" );
            }
            _listView = listView.JetListView;
            ApplySortSettings( listView.JetListView, _curSortSettings );

            lock( _resourceList )
            {
                AddResourceNodes();
                _nodesAdded = true;

                _resourceList.ResourceAdded += HandleResourceAdded;
                _resourceList.ResourceChanged += HandleResourceChanged;
                _resourceList.ResourceDeleting += HandleResourceDeleting;
            }

            _listView.ColumnClick += HandleColumnClick;
	    }

	    public void ApplySortSettings( JetListView listView, SortSettings sortSettings )
	    {
	        if ( sortSettings != null )
	        {
                _curSortSettings = sortSettings;
                bool foundComparerColumn = false;
                foreach( JetListViewColumn col in listView.Columns )
                {
                    ResourcePropsColumn propsCol = col as ResourcePropsColumn;
                    if ( propsCol != null && propsCol.PropIdsEqual( sortSettings.SortProps ) )
                    {
                        _lastComparer = CreateColumnComparer( propsCol, sortSettings );
                        propsCol.SortIcon = sortSettings.SortAscending ? SortIcon.Ascending : SortIcon.Descending;
                        _lastSortColumn = propsCol;
                        _listView.GroupProvider = _lastSortColumn.GroupProvider;
                        foundComparerColumn = true;
                    }
                    else
                    {
                        col.SortIcon = SortIcon.None;
                    }
                }
                if ( !foundComparerColumn )
                {
                    _lastComparer = new ResourceComparer( _resourceList, _curSortSettings, true );
                }

                if ( _lastComparer != null )
                {
                    _listView.NodeCollection.SetItemComparer( null, _lastComparer );
                    if ( _nodesAdded )
                    {
                        _listView.NodeCollection.Sort();
                    }
                }
	        }
	    }

	    public void UpdateSortColumn()
	    {
	        if ( _curSortSettings != null )
	        {
	            foreach( JetListViewColumn col in _listView.Columns )
	            {
	                ResourcePropsColumn rlvCol = col as ResourcePropsColumn;
	                if ( rlvCol != null && rlvCol.PropIdsEqual( _curSortSettings.SortProps ) )
	                {
	                    rlvCol.SortIcon = _curSortSettings.SortAscending ? SortIcon.Ascending : SortIcon.Descending;
	                    _lastSortColumn = rlvCol;
                        _listView.GroupProvider = _lastSortColumn.GroupProvider;
	                    break;
	                }
	            }
	        }
	    }

	    protected virtual void AddResourceNodes()
	    {
	        for( int i=0; i<_resourceList.Count; i++ )
	        {
	            _listView.Nodes.Add( _resourceList [i] );
	        }
	    }

        protected void OnResourceCountChanged()
        {
            if ( ResourceCountChanged != null )
            {
                ResourceCountChanged( this, EventArgs.Empty );
            }
        }

        protected void OnSortChanged()
        {
            if ( SortChanged != null )
            {
                SortChanged( this, EventArgs.Empty );
            }
        }

	    protected virtual void HandleResourceAdded( object sender, ResourceIndexEventArgs e )
        {
	        if ( !_disposed )
	        {
	            _listView.Nodes.Add( e.Resource );
	            OnResourceCountChanged();
	        }
        }

        protected virtual void HandleResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if ( !_disposed )
            {
                // UpdateItemSafe() is necessary because in some cases (OM-8629) some plugin performs
                // a resource change from the ResourceAdded event handler, which causes
                // JetListView to receive ResourceChanged notification for a resource before it
                // has received a ResourceAdded notification for the same resource.
                _listView.UpdateItemSafe( e.Resource );
            }
        }

        protected virtual void HandleResourceDeleting( object sender, ResourceIndexEventArgs e )
        {
            if ( !_disposed && e.Resource != null )
            {
                // it's possible that a just added resource can be moved in the resource list and
                // we receive a delete notification before an add notification (OM-10510)
                if ( _listView.NodeCollection.NodeFromItem( e.Resource ) != null )
                {
                    _listView.Nodes.Remove( e.Resource );
                    OnResourceCountChanged();
                }
            }
        }

	    public virtual void Dispose()
	    {
            if ( !_disposed )
            {
                _disposed = true;
                if( _listView != null )
                {
                    _listView.ColumnClick -= HandleColumnClick;
                    lock( _resourceList )
                    {
                        _resourceList.ResourceAdded -= HandleResourceAdded;
                        _resourceList.ResourceChanged -= HandleResourceChanged;
                        _resourceList.ResourceDeleting -= HandleResourceDeleting;
                        _resourceList = null;
                        _listView = null;
                    }
                }
            }
	    }

	    public virtual bool FindResourceNode( IResource res )
	    {
	        return _resourceList.Contains( res );
	    }

	    private void HandleColumnClick( object sender, ColumnEventArgs e )
	    {
            ResourcePropsColumn col = e.Column as ResourcePropsColumn;
            if ( col != null && _listView != null )
            {
                HandeColumnClick( col );
            }
	    }

	    public void HandeColumnClick( ResourcePropsColumn col )
	    {
            ComparableArrayList propList = new ComparableArrayList( col.PropIds );
            if ( _lastComparer != null && col == _lastSortColumn )
	        {
	            Guard.NullMember( _curSortSettings, "_curSortSettings" );
                _curSortSettings = _curSortSettings.Reverse();
                _lastSortDirections [propList] = (col.SortIcon == SortIcon.Descending ); // reverse
	        }
	        else
	        {
                bool sortAscending = true;
                if ( _lastSortDirections.ContainsKey( propList ) )
                {
                    sortAscending = (bool) _lastSortDirections [propList];
                }
                _curSortSettings = new SortSettings( col.PropIds, sortAscending );

	            if ( _lastSortColumn != null )
	            {
                    _lastSortColumn.SortIcon = SortIcon.None;
	            }
	            _lastSortColumn = col;
	        }

            _lastComparer = CreateColumnComparer( col, _curSortSettings );
            Guard.NullMember( _lastSortColumn, "_lastSortColumn" );
            _lastSortColumn.SortIcon = _curSortSettings.SortAscending ? SortIcon.Ascending : SortIcon.Descending;
            Guard.NullMember( _listView, "_listView" );
	        _listView.NodeCollection.SetItemComparer( null, _lastComparer );
	        _listView.NodeCollection.Sort();
            _listView.GroupProvider = _lastSortColumn.GroupProvider;

            OnSortChanged();
	    }

	    private ResourceComparer CreateColumnComparer( ResourcePropsColumn col, SortSettings sortSettings )
	    {
	        if ( col.CustomComparer != null )
	        {
	            return new ResourceComparer( _resourceList, col.CustomComparer, sortSettings.SortAscending );
	        }
	        else
	        {
	            return new ResourceComparer( _resourceList, sortSettings, true );
	        }
	    }
	}
}
