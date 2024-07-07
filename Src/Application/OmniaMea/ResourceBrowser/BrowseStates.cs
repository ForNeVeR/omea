// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    internal class BrowseStack: IDisposable
    {
        private ResourceBrowser _owner;
        private ArrayList       _browseStack = new ArrayList();
        private int             _browseStackPos = -1;
        private static int      _maxBrowseStackSize = 10;
        private bool            _showingState = false;

        public BrowseStack( ResourceBrowser owner )
        {
            _owner = owner;
        }

        public void Clear()
        {
            _browseStack.Clear();
            _browseStackPos = -1;
        }

        public void Dispose()
        {
            foreach( AbstractBrowseState state in _browseStack )
            {
                state.Dispose();
            }
        }

        public int Count
        {
            get { return _browseStackPos+1; }
        }

        public static int MaxBrowseStackSize
        {
            get { return _maxBrowseStackSize; }
            set
            {
                _maxBrowseStackSize = value;
                if ( _maxBrowseStackSize < 2 )
                    _maxBrowseStackSize = 2;
            }
        }

        /**
         * Pushes a browse state to the browse stack.
         */

        internal void Push( AbstractBrowseState state )
        {
            if ( _showingState )
                return;

            // remove entries on the top of the stack
            int removeCount = _browseStack.Count - _browseStackPos - 1;
            if ( removeCount > 0 )
            {
                for( int i=_browseStackPos + 1; i < _browseStack.Count; i++ )
                {
                    AbstractBrowseState oldState = (AbstractBrowseState) _browseStack [i];
                    oldState.Dispose();
                }
                _browseStack.RemoveRange( _browseStackPos + 1, removeCount );
            }

            while ( _browseStack.Count > _maxBrowseStackSize )
            {
                AbstractBrowseState oldState = (AbstractBrowseState) _browseStack [0];
                oldState.Dispose();
                _browseStack.RemoveAt( 0 );
                _browseStackPos--;
            }

            if ( _browseStack.Count > 0 )
            {
                SaveSelectedResource( _browseStack.Count-1 );
            }
            _browseStack.Add( state );
            _browseStackPos++;
        }

        /**
         * Save the currently selected resource at the specified position
         * in the browse stack.
         */

        private void SaveSelectedResource( int browseStackPos )
        {
            if ( browseStackPos < 0 || browseStackPos >= _browseStack.Count )
            {
                return;
            }

            AbstractBrowseState browseState = (AbstractBrowseState) _browseStack [browseStackPos];
            IResourceList selection = _owner.SelectedResources;
            if ( selection.Count > 0 && selection.ResourceIds [0] != -1 )
            {
                browseState.SetSelectedResource( selection [0] );
            }
            else
            {
                browseState.SetSelectedResource( null );
            }
        }

        /**
         * Shows the browse state at the current stack position, or the previous
         * valid browse state.
         */

        internal void ShowCurrentBrowseState()
        {
            _showingState = true;
            try
            {

                while ( _browseStackPos >= 0 )
                {
                    AbstractBrowseState state = (AbstractBrowseState) _browseStack [_browseStackPos];

                    // if the state failed to show, switch to previous state
                    if ( state.Show( _owner ) )
                        break;

                    _browseStack.Remove( state );
                    _browseStackPos--;
                }
            }
            finally
            {
                _showingState = false;
            }
        }

        public void GoBack()
        {
            Debug.WriteLine( "BrowseStack.GoBack: before back: _browseStackPos=" + _browseStackPos );
            if ( _browseStackPos > 0 )
            {
                SaveSelectedResource( _browseStackPos );
                _browseStackPos--;
                ShowCurrentBrowseState();
                Debug.WriteLine( "BrowseStack.GoBack: after back: _browseStackPos=" + _browseStackPos );
            }
        }

        public bool CanBack()
        {
            return _browseStackPos > 0;
        }

        public void GoForward()
        {
            if ( _browseStackPos < _browseStack.Count-1 )
            {
                SaveSelectedResource( _browseStackPos );
                _browseStackPos++;
                ShowCurrentBrowseState();
            }
        }

        public bool CanForward()
        {
            return _browseStackPos < _browseStack.Count-1;
        }

        internal void DropTop()
        {
            GoBack();
            if ( _browseStack.Count > 1 )
            {
                _browseStack.RemoveAt( _browseStack.Count-1 );
            }
        }

        internal AbstractBrowseState Peek( int depth )
        {
            return (AbstractBrowseState) _browseStack [_browseStackPos-depth];
        }

        internal void Drop( int count )
        {
            _browseStackPos -= count;
            ShowCurrentBrowseState();
        }

        /// <summary>
        /// Discards the top entry from the browse stack without showing any other entry.
        /// </summary>
        public void DiscardTop()
        {
            Peek( 0 ).MarkDiscarded();
        }
    }

    internal abstract class AbstractBrowseState: IDisposable
    {
        private string _activeTab;
        private string _activeSidebarPane;
        protected IResource _selectedResource;
        protected IResource _ownerResource;
        private object _tag;
        private bool _discarded;

        protected AbstractBrowseState( IResource ownerResource )
        {
            _activeTab = Core.TabManager.CurrentTabId;

            string activePaneID = Core.LeftSidebar.ActivePaneId;
            if ( activePaneID != null )
            {
                AbstractViewPane viewPane = Core.LeftSidebar.GetPane( activePaneID );
                if ( viewPane != null && viewPane.SelectedResource == ownerResource )
                {
                    _activeSidebarPane = activePaneID;
                }
            }

            _ownerResource = ownerResource;
            _tag = null;
        }

        public virtual void Dispose()
        {
        }

        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        internal void MarkDiscarded()
        {
            _discarded = true;
        }

        public bool Show( ResourceBrowser owner )
        {
            if ( _discarded )
                return false;

            Core.UIManager.BeginUpdateSidebar();
            if ( !Core.TabManager.ActivateTab( _activeTab ) )
            {
                Core.UIManager.EndUpdateSidebar();
                return false;
            }

            if ( _ownerResource != null && _activeSidebarPane != null )
            {
                AbstractViewPane pane = Core.LeftSidebar.ActivateViewPane( _activeTab, _activeSidebarPane );
                Core.UIManager.EndUpdateSidebar();
                if ( pane == null )
                {
                    // DoEvents() could have happened when activating pane and caused switch to other pane
                    // (OM-11136)
                    return false;
                }
                pane.SelectResource( _ownerResource, false );
                if ( _selectedResource != null )
                {
                    owner.SelectResource( _selectedResource );
                }

                return true;
            }
            else
            {
                Core.UIManager.EndUpdateSidebar();
                return DoShow( owner );
            }
        }

        protected abstract bool DoShow( ResourceBrowser owner );

        public void SetSelectedResource( IResource res )
        {
            _selectedResource = res;
        }
    }

    internal class ResourceListBrowseState: AbstractBrowseState
    {
        private IResourceList              _resourceList;
        private ResourceListDisplayOptions _options;

        internal ResourceListBrowseState( IResource ownerResource, IResourceList resourceList,
            ResourceListDisplayOptions options )
            : base( ownerResource )
        {
            _resourceList = resourceList;
            _options = options;
        }

        public override void Dispose()
        {
            _resourceList.Dispose();
        }

        protected override bool DoShow( ResourceBrowser owner )
        {
            owner.DoShowResources( _ownerResource, _resourceList, _options );
            return true;
        }
    }

    internal class ResourceBrowseState: AbstractBrowseState
    {
        private IResource _resource;
        private bool _backOnDelete;

        internal ResourceBrowseState( IResource resource, bool backOnDelete )
            : base( resource )
        {
            _resource = resource;
            _backOnDelete = backOnDelete;
        }

        protected override bool DoShow( ResourceBrowser owner )
        {
            if ( _resource.IsDeleted )
                return false;

            owner.DoShowResource( _resource, _backOnDelete );
            return true;
        }
    }
}
