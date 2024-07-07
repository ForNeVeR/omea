// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;

namespace JetBrains.JetListViewLibrary
{
    /// <summary>
    /// An interface which allows to hide nodes from JetListView based on some condition.
    /// </summary>
    public interface IJetListViewNodeFilter
    {
        bool AcceptNode( JetListViewNode node );
        event EventHandler FilterChanged;
    }

    /// <summary>
	/// A collection of filters in JetListView.
	/// </summary>
	public class JetListViewFilterCollection
	{
        private ArrayList _filters = new ArrayList();

        public event EventHandler FilterListChanged;

        public int Count
        {
            get { return _filters.Count; }
        }

        public void Add( IJetListViewNodeFilter filter )
        {
            if ( !_filters.Contains( filter ) )
            {
                filter.FilterChanged += new EventHandler( HandleFilterChanged );
                _filters.Add( filter );
                OnFilterListChanged();
            }
        }

        public bool Contains( IJetListViewNodeFilter filter )
        {
            return _filters.Contains( filter );
        }

        public void Remove( IJetListViewNodeFilter filter )
        {
            if ( _filters.Contains( filter ) )
            {
                filter.FilterChanged -= new EventHandler( HandleFilterChanged );
                _filters.Remove( filter );
                OnFilterListChanged();
            }
        }

        /// <summary>
        /// Refilters the tree.
        /// </summary>
        public void Update()
        {
            OnFilterListChanged();
        }

        private void HandleFilterChanged( object sender, EventArgs e )
        {
            OnFilterListChanged();
        }

        internal bool AcceptNode( JetListViewNode node )
        {
            if( _filters.Count > 0 )
            {
                foreach( IJetListViewNodeFilter filter in _filters )
                {
                    if ( !filter.AcceptNode( node ) )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected void OnFilterListChanged()
        {
            if ( FilterListChanged != null )
            {
                FilterListChanged( this, EventArgs.Empty );
            }
        }
	}
}
