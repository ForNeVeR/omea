/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using System.Collections;
using JetBrains.Omea.ResourceStore;
using JetBrains.DataStructures;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.ResourceTools
{
    public interface IResourceTabProvider
    {
        string GetDefaultTab();
        string GetResourceTab( IResource res );
        IResourceList GetTabFilterList( string tabId );
    }

    /**
     * A specific state of unread resources.
     */

    public class UnreadState
    {
        private UnreadManager _unreadManager;
        private string _tab;
        private IResource _workspace;
        private IntHashTableOfInt _unreadCountBuffer = new IntHashTableOfInt();
        private IntHashTableOfInt _unreadCountersValid = new IntHashTableOfInt();

        internal UnreadState( UnreadManager mgr, string tab, IResource workspace )
        {
            _unreadManager = mgr;
            _unreadCountBuffer.MissingKeyValue = 0;
            _tab = tab;
            _workspace = workspace;
        }

        internal string Tab
        {
            get { return _tab; }
        }

        internal IResource Workspace
        {
            get { return _workspace; }
        }

        internal bool IsPersistent
        {
            get { return _tab == null; }
        }

        internal int GetCountFromBuffer( IResource res )
        {
            lock( _unreadCountBuffer )
            {
                return _unreadCountBuffer [res.Id];
            }
        }

        internal void ResetCounters()
        {
            lock( _unreadCountersValid )
            {
                _unreadCountersValid.Clear();
            }
        }

        internal bool IsCounterValid( IResource res )
        {
            lock( _unreadCountersValid )
            {
                return _unreadCountersValid [res.Id] == 1;
            }
        }

        internal void InvalidateCounter( IResource res )
        {
            lock( _unreadCountersValid )
            {
                _unreadCountersValid.Remove( res.Id );
            }
        }
        
        public int GetUnreadCount( IResource res )
        {
            return _unreadManager.GetCountForState( this, res );
        }

        internal void UpdateUnreadCounter( IResource res, int count )
        {
            bool countChanged = false;
            lock( _unreadCountBuffer )
            {
                if ( _unreadCountBuffer [res.Id] != count )
                {
                    _unreadCountBuffer [res.Id] = count;
                    countChanged = true;
                }

                lock( _unreadCountersValid )
                {
                    _unreadCountersValid [res.Id] = 1;
                }
            }
            if ( countChanged )
            {
                OnUnreadCountChanged( res );
            }
        }

        internal void OnUnreadCountChanged( IResource res )
        {
            if ( UnreadCountChanged != null )
            {
                UnreadCountChanged( this, new ResourceEventArgs( res ) );
            }
        }

        /*
        public override string ToString()
        {
            return "UnreadState(" + ((_key == null ) ? "default" : _key.ToString() ) + ")";
        }
        */

        public event ResourceEventHandler UnreadCountChanged;
    }

    /**
     * Monitors resources that become read or unread and adjusts the unread
     * count of related containers accordingly.
     */

    public class UnreadManager : IUnreadManager
    {
        private IResourceStore _store;
        private IResourceTabProvider _tabProvider;
        private WorkspaceManager _workspaceManager;
        private int            _propUnreadCount;
        private ICoreProps     _coreProps;

        private Hashtable _unreadStateTabMap = new Hashtable();  // tab ID -> IntHashTable<workspace ID, UnreadState>
        private UnreadState _defaultUnreadState;
        private UnreadState _curUnreadState;
        private IntHashSet _unreadCountersChanged = new IntHashSet();
        private HashMap _unreadCountProviders = new HashMap();   // resource type -> IUnreadCountProvider

        private bool _isEnabled = false;
        private bool _traceUnreadCounters;

        public UnreadManager( WorkspaceManager workspaceManager, IResourceTabProvider tabProvider,
                              ISettingStore settingStore, ICoreProps coreProps )
        {
            _tabProvider = tabProvider;
            _workspaceManager = workspaceManager;
            _coreProps = coreProps;
            _defaultUnreadState = new UnreadState( this, null, null );
            _curUnreadState = _defaultUnreadState;
            
            _store = Core.ResourceStore;
            _propUnreadCount = _store.PropTypes.Register( "UnreadCount", PropDataType.Int, PropTypeFlags.Internal );

            _traceUnreadCounters = settingStore.ReadBool( "UnreadCounters", "TraceUnreadCounters", false );

            Core.ResourceAP.JobFinished += environment_ResourceOperationFinished;

            Enabled = true;
        }

        public UnreadState CurrentUnreadState
        {
            get { return _curUnreadState; }
        }

        public bool Enabled
        {
            get { return _isEnabled; }
            set
            {
                if ( _isEnabled != value )
                {
                    _isEnabled = value;
                    if ( _isEnabled )
                    {
                        MyPalStorage.Storage.ResourceSaved += OnResourceSaved;
                        MyPalStorage.Storage.ResourceDeleting += OnResourceDeleting;
                    }
                    else
                    {
                        MyPalStorage.Storage.ResourceSaved -= OnResourceSaved;
                        MyPalStorage.Storage.ResourceDeleting -= OnResourceDeleting;
                    }
                }
            }
        }

        public void RegisterUnreadCountProvider( string resType, IUnreadCountProvider provider )
        {
            _unreadCountProviders [resType] = provider;
        }

        /**
         * Switches the state of unread counters to the state associated with the 
         * specified key and filter list, and creates a new state if necessary.
         */

        public UnreadState SetUnreadState( string activeTab, IResource activeWorkspace )
        {
            if ( activeTab == _tabProvider.GetDefaultTab() && activeWorkspace == null )
            {
                _curUnreadState = _defaultUnreadState;
            }
            else
            {
                IntHashTable tabHash = (IntHashTable) _unreadStateTabMap [activeTab];
                if ( tabHash == null )
                {
                    tabHash = new IntHashTable();
                    _unreadStateTabMap [activeTab] = tabHash;
                }

                int wsId = (activeWorkspace == null) ? 0 : activeWorkspace.Id;
                UnreadState state = (UnreadState) tabHash [wsId];
                if ( state == null )
                {
                    state = new UnreadState( this, activeTab, activeWorkspace );
                    tabHash [wsId] = state;
                }

                _curUnreadState = state;
            }
            return _curUnreadState;
        }

        /**
         * Returns the unread counter to be displayed for the specified resource
         * (if an in-memory unread counter was specified, it overrides the persistent
         * unread counter).
         */

        public int GetUnreadCount( IResource res )
        {
            return GetCountForState( _curUnreadState, res );
        }

        /**
         * Sets the in-memory unread counter for the specified resource.
         */

        public void SetInMemoryUnreadCount( IResource res, int count )
        {
            if ( _traceUnreadCounters )
            {
                Trace.WriteLine( "Count for " + res.ToString() + " on " + _curUnreadState + " is " + count );
            }
            _curUnreadState.UpdateUnreadCounter( res, count );
        }

        /**
         * Returns the persistent unread counter for the specified resource 
         * (the counter which is not view-specific and which is saved in the
         * resource store).
         */

        public int GetPersistentUnreadCount( IResource res )
        {
            if ( _defaultUnreadState.IsCounterValid( res ) )
            {
                return _defaultUnreadState.GetUnreadCount( res );
            }
                
            int count = res.GetIntProp( _propUnreadCount );
            _defaultUnreadState.UpdateUnreadCounter( res, count );
            return count;
        }

        /**
         * Returns the unread count for the specified state.
         */

        internal int GetCountForState( UnreadState state, IResource res )
        {
            if ( state.IsCounterValid( res ) || ( state != _curUnreadState && !state.IsPersistent) )
            {
                return state.GetCountFromBuffer( res );
            }

            int count;
            IUnreadCountProvider provider = (IUnreadCountProvider) _unreadCountProviders [res.Type];
            if ( provider == null )
            {
                if ( state.IsPersistent )
                {
                    count = res.GetIntProp( _propUnreadCount );
                }
                else
                {
                    count = GetUnreadCountFromLinks( res, state );
                }
            }
            else
            {
                count = GetProviderUnreadCount( res, state, provider );
            }
            state.UpdateUnreadCounter( res, count );
            return count;
        }

        private int GetUnreadCountFromLinks( IResource res, UnreadState state )
        {
            int count;
            int persistentCount = GetPersistentUnreadCount( res );
            if ( persistentCount == 0 )
            {
                count = 0;
            }
            else
            {
                int unfilteredCount;
                IResourceList links = GetUnreadCountedLinks( res, out unfilteredCount );
                if ( links == null )
                {
                    count = 0;
                }
                else
                {
                    links = links.Intersect( _tabProvider.GetTabFilterList( state.Tab ), true );
                    count = CountUnreadResources( links );
                    // HACK: Cleanup for out-of-sync UnreadCount values
                    if ( links.Count == unfilteredCount && count != persistentCount )
                    {
                        SetPersistentUnreadCount( res, count );
                        MarkUnreadCounterChanged( res );
                    }
                }
            }
            return count;
        }

        /**
         * Returns the count of unread resources returned by the specified provider and
         * filtered by the workspace.
         */

        private int GetProviderUnreadCount( IResource res, UnreadState state, IUnreadCountProvider provider )
        {
            IResourceList unreadList = provider.GetResourcesForView( res );
            if ( unreadList == Core.ResourceStore.EmptyResourceList )
            {
                return 0;
            }
            if ( state != _defaultUnreadState )
            {
                unreadList = unreadList.Intersect( _tabProvider.GetTabFilterList( state.Tab ) );
                if ( state.Workspace != null )
                {
                    unreadList = unreadList.Intersect( _workspaceManager.GetFilterList( state.Workspace ) );
                }
            }
            return unreadList.Count;
        }

        /**
         * Sets the persistent unread counter for the specified resource.
         * (The counter will be actually flushed to the resource store
         * when the resource operation is finished.)                
         */

        public void SetPersistentUnreadCount( IResource res, int count )
        {
            _defaultUnreadState.UpdateUnreadCounter( res, count );
        }

        public void InvalidateUnreadCounter( IResource res )
        {
            _defaultUnreadState.InvalidateCounter( res );
            foreach( DictionaryEntry de in _unreadStateTabMap )
            {
                IntHashTable ht = (IntHashTable) de.Value;
                foreach ( IntHashTable.Entry entry in ht )
                {
                    UnreadState state = (UnreadState) entry.Value;
                    state.InvalidateCounter( res );
                }
            }
            _curUnreadState.OnUnreadCountChanged( res );
        }

        private void OnResourceSaved( object sender, ResourcePropEventArgs e )
        {
            if ( e.ChangeSet.IsPropertyChanged( _coreProps.IsUnread ) || e.ChangeSet.IsPropertyChanged( _coreProps.IsDeleted ) )
            {
                bool wasUnread = e.ChangeSet.GetOldValue( _coreProps.IsUnread ) != null ||
                    (!e.ChangeSet.IsPropertyChanged( _coreProps.IsUnread ) && e.Resource.HasProp( _coreProps.IsUnread ) );
                bool wasDeleted = e.ChangeSet.GetOldValue( _coreProps.IsDeleted ) != null ||
                    (!e.ChangeSet.IsPropertyChanged( _coreProps.IsDeleted ) && e.Resource.HasProp( _coreProps.IsDeleted ) );
                bool isUnread = e.Resource.HasProp( _coreProps.IsUnread );
                bool isDeleted = e.Resource.HasProp( _coreProps.IsDeleted );
                
                int oldUnreadStatus = (wasUnread && !wasDeleted) ? 1 : 0;
                int newUnreadStatus = (isUnread && !isDeleted) ? 1 : 0;

                if ( oldUnreadStatus != newUnreadStatus )
                {
                    ProcessResourceUnreadChange( e.Resource, newUnreadStatus - oldUnreadStatus, 
                        (newUnreadStatus > oldUnreadStatus) ? null : e.ChangeSet );
                }
            }
            else if ( e.Resource.HasProp( _coreProps.IsUnread ) && !e.Resource.HasProp( _coreProps.IsDeleted ) )
            {
                ProcessUnreadCountedLinksChange( e.Resource, e.ChangeSet );
                if ( e.ChangeSet.IsPropertyChanged( _workspaceManager.Props.WorkspaceVisible ) )
                {
                    ProcessWorkspaceChange( e.Resource, e.ChangeSet );
                }
            }
        }

        private void OnResourceDeleting( object sender, EventArgs e )
        {
            IResource res = (IResource) sender;
            if ( res.HasProp( _coreProps.IsUnread ) && !res.HasProp( _coreProps.IsDeleted ) )
            {
                ProcessResourceUnreadChange( res, -1, null );                                
            }
        }

        /**
         * Increments the unread counters of resources linked with unread-counted links
         * to the specified resource by 'delta'. If cs is not null, the affected resources
         * are not the currently linked ones, but rather the ones linked before the changes
         * described by cs happened.
         */
        
        private void ProcessResourceUnreadChange( IResource res, int delta, IPropertyChangeSet cs )
        {
            UnreadState[] unreadStates = GetResourceUnreadStates( res );

            IntArrayList linkTypeIDs = GetAllLinkTypes( res, cs );
            for( int i=0; i<linkTypeIDs.Count; i++ )
            {
                int linkType = linkTypeIDs [i];
                if ( IsUnreadCountedLink( linkType ) )
                {
                    IntArrayList linkedResList = new IntArrayList( res.GetLinksOfType( null, linkType ).ResourceIds );
                    if ( cs != null )
                    {
                        foreach( LinkChange change in cs.GetLinkChanges( linkType ) )
                        {
                            if ( change.ChangeType == LinkChangeType.Add )
                            {
                                linkedResList.Remove( change.TargetId );
                            }
                            else
                            {
                                linkedResList.Add( change.TargetId );
                            }
                        }
                    }
                    for( int j=0; j<linkedResList.Count; j++ )
                    {
                        IResource linkRes = _store.LoadResource( linkedResList [j] );
                        foreach( UnreadState state in unreadStates )
                        {
                            AdjustUnreadCount( linkRes, delta, state );
                        }
                    }
                }
            }
        }

        internal void UpdateCountForView( IResource view, IResource res, int delta )
        {
            UnreadState[] states = GetResourceUnreadStates( res );
            for( int i=0; i<states.Length; i++ )
            {
                AdjustUnreadCount( view, delta, states [i] );
            }
        }

        private void ProcessUnreadCountedLinksChange( IResource res, IPropertyChangeSet cs )
        {
            UnreadState[] unreadStates = GetResourceUnreadStates( res );

            int[] propIDs = cs.GetChangedProperties();
            for( int i=0; i<propIDs.Length; i++ )
            {
                int propID = propIDs [i];
                if ( _store.PropTypes [propID].HasFlag( PropTypeFlags.CountUnread ) )
                {
                    foreach( LinkChange change in cs.GetLinkChanges( propID ) )
                    {
                        IResource target = _store.TryLoadResource( change.TargetId );
                        if ( target == null )
                        {
                            continue;
                        }

                        int delta = ( change.ChangeType == LinkChangeType.Add ) ? 1 : -1;

                        foreach( UnreadState state in unreadStates )
                        {
                            AdjustUnreadCount( target, delta, state );
                        }
                    }
                }
            }
        }

        private void ProcessWorkspaceChange( IResource res, IPropertyChangeSet cs )
        {
            IntHashTable defaultTabMap = (IntHashTable) _unreadStateTabMap [_tabProvider.GetDefaultTab()];
            IntHashTable specificTabMap = null;
            string resourceTab = _tabProvider.GetResourceTab( res );
            if ( resourceTab != null )
            {
                specificTabMap = (IntHashTable) _unreadStateTabMap [resourceTab];
            }

            LinkChange[] wsLinkChanges = cs.GetLinkChanges( _workspaceManager.Props.WorkspaceVisible );
            
            int[] linkTypes = res.GetLinkTypeIds();
            for( int i=0; i<linkTypes.Length; i++ )
            {
                if ( IsUnreadCountedLink( linkTypes [i] ) )
                {
                    IResourceList linkList = res.GetLinksOfType( null, linkTypes [i] );
                    foreach( IResource link in linkList )
                    {
                        if ( cs.GetLinkChange( linkTypes [i], link.Id ) == LinkChangeType.Add )
                            continue;

                        foreach( LinkChange linkChange in wsLinkChanges )
                        {
                            int delta = (linkChange.ChangeType == LinkChangeType.Add) ? 1 : -1;
                            AdjustCounterInState( defaultTabMap, linkChange.TargetId, link, delta );
                            AdjustCounterInState( specificTabMap, linkChange.TargetId, link, delta );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// When a resource matching a view enters or leaves a workspace, update unread counter for the
        /// view in that workspace.
        /// </summary>
        /// <param name="res">The resource entering or leaving the workspace.</param>
        /// <param name="viewResource">The view for which the counter should be updated.</param>
        /// <param name="workspaceId">The ID of the workspace which the resource enters or leaves.</param>
        /// <param name="delta">The value by which the counter is changed (1 or -1).</param>
        internal void AdjustViewWorkspaceCounter( IResource res, IResource viewResource, int workspaceId, int delta )
        {
            IntHashTable defaultTabMap = (IntHashTable) _unreadStateTabMap [_tabProvider.GetDefaultTab()];
            AdjustCounterInState( defaultTabMap, workspaceId, viewResource, delta );
            string resourceTab = _tabProvider.GetResourceTab( res );
            if ( resourceTab != null )
            {
                IntHashTable specificTabMap = (IntHashTable) _unreadStateTabMap [resourceTab];
                AdjustCounterInState( specificTabMap, workspaceId, viewResource, delta );
            }
        }

        private void AdjustCounterInState( IntHashTable tabMap, int workspaceId, IResource resource, int delta )
        {
            if ( tabMap != null )
            {
                UnreadState tabState = (UnreadState) tabMap [workspaceId];
                if ( tabState != null )
                {
                    AdjustUnreadCount( resource, delta, tabState );
                }
            }
        }

        /**
         * Returns the list of all link types for the given resource. If the resource
         * was changed, the list also includes the link types which were present on
         * the resource before the change.
         */

        private IntArrayList GetAllLinkTypes( IResource res, IPropertyChangeSet cs )
        {
            IntArrayList linkTypeIDs = new IntArrayList( res.GetLinkTypeIds() );
            if ( cs != null )
            {
                int[] changedPropIDs = cs.GetChangedProperties();
                for( int i=0; i<changedPropIDs.Length; i++ )
                {
                    int propID = changedPropIDs [i];
                    if ( _store.PropTypes [propID].DataType == PropDataType.Link && linkTypeIDs.IndexOf( propID ) < 0 )
                    {
                        linkTypeIDs.Add( propID );
                    }
                }
            }
            return linkTypeIDs;
        }

        /// <summary>
        /// Returns the array of existing unread states in which the specified
        /// resource is visible.
        /// </summary>
        private UnreadState[] GetResourceUnreadStates( IResource res )
        {
            ArrayList states = ArrayListPool.Alloc();
            try 
            {
                IResourceList workspaces = _workspaceManager.GetResourceWorkspaces( res );
                FillStatesForTab( states, workspaces, _tabProvider.GetDefaultTab() );
                string tab = _tabProvider.GetResourceTab( res );
                if ( tab != null )
                {
                    FillStatesForTab( states, workspaces, tab );
                }
                return (UnreadState[]) states.ToArray( typeof (UnreadState) );
            }
            finally
            {
                ArrayListPool.Dispose( states );
            }
        }

        /**
         * Adds the existing unread states for the specified tab and list of workspaces
         * to the specified array list.
         */

        private void FillStatesForTab( ArrayList states, IResourceList workspaces, string tab )
        {
            if ( tab == _tabProvider.GetDefaultTab() )
            {
                states.Add( _defaultUnreadState );
            }

            IntHashTable tabHash = (IntHashTable) _unreadStateTabMap [tab];
            if ( tabHash != null )
            {
                if ( tab != _tabProvider.GetDefaultTab() )
                {
                    UnreadState state = (UnreadState) tabHash [0];
                    if ( state != null )
                    {
                        states.Add( state );
                    }
                }
                
                foreach( IResource ws in workspaces )
                {
                    UnreadState state = (UnreadState) tabHash [ws.Id];
                    if ( state != null )
                    {
                        states.Add( state );
                    }
                }
            }
        }

        /// <summary>
        /// Adjusts the unread count for the specified resource stored in the specified
        /// count map by the specified delta value.
        /// </summary>
        private void AdjustUnreadCount( IResource res, int delta, UnreadState state )
        {
            if ( !state.IsPersistent && !state.IsCounterValid( res ) )
                return;

            int count;
            if ( state.IsCounterValid( res ) || ( state.IsPersistent && !_unreadCountProviders.Contains( res.Type ) ) )
            {
                count = state.GetUnreadCount( res ) + delta;
            }
            else
            {
                // this will initiate a new count calculation which will already take into account
                // the new unread state of the resource
                count = state.GetUnreadCount( res );
            }
            if ( count >= 0 )
            {
                state.UpdateUnreadCounter( res, count );
                if ( state.IsPersistent && !_unreadCountProviders.Contains( res.Type ) )
                {
                    MarkUnreadCounterChanged( res );
                }
            }
        }

        private void MarkUnreadCounterChanged( IResource res )
        {
            lock ( _unreadCountersChanged )
            {
                _unreadCountersChanged.Add( res.Id );
            }
        }

        private bool IsUnreadCountedLink( int propID )
        {
            return _store.PropTypes [propID].HasFlag( PropTypeFlags.CountUnread );
        }

        /**
         * When a resource operation is finished, flushes the persistent unread 
         * counters to the resource store.
         */
        
        private void environment_ResourceOperationFinished( object sender, EventArgs e )
        {
            int[] countersToFlush = null;
            lock ( _unreadCountersChanged )
            {
                if ( _unreadCountersChanged.Count > 0 )
                {
                    countersToFlush = new int [_unreadCountersChanged.Count];
                    int i = 0;
                    foreach( IntHashSet.Entry hse in _unreadCountersChanged )
                    {
                        countersToFlush [i++] = hse.Key;
                    }
                    _unreadCountersChanged.Clear();
                }
            }

            if ( countersToFlush != null )
            {
                for( int i = 0; i < countersToFlush.Length; ++i )
                {
                    IResource res = _store.TryLoadResource( countersToFlush[i] );
                    if ( res != null )
                    {
                        int count = _defaultUnreadState.GetUnreadCount( res );
                        Trace.WriteLineIf( _traceUnreadCounters,
                            "Flushing unread count " + count + " for resource " + res.ToString() );
                        res.SetProp( _propUnreadCount, count );
                    }
                }
            }
        }

        /**
         * Refreshes the unread counters on all resources. Assumes to be invoked from the
         * resource thread.
         */

        public void RefreshUnreadCounters()
        {
            IResourceList unreadCountedResources = _store.FindResourcesWithProp( null, _propUnreadCount );
            foreach( IResource unreadCountedRes in unreadCountedResources )
            {
                int linkCount;
                IResourceList unreadCountedLinks = GetUnreadCountedLinks( unreadCountedRes, out linkCount );
                unreadCountedRes.SetProp( _propUnreadCount, CountUnreadResources( unreadCountedLinks ) );
            }
            _defaultUnreadState.ResetCounters();
            foreach( DictionaryEntry de in _unreadStateTabMap )
            {
                IntHashTable tabHash = (IntHashTable) de.Value;
                foreach( IntHashTable.Entry ie in tabHash )
                {
                    UnreadState state = (UnreadState) ie.Value;
                    state.ResetCounters();
                }
            }
        }

        /// <summary>
        /// Returns the list of all resources linked to the specified resource
        /// with links marked as CountUnread.
        /// </summary>
        private IResourceList GetUnreadCountedLinks( IResource res, out int count )
        {
            IResourceList unreadCountedLinks = null;
            count = 0;
            foreach( IPropType propType in Core.ResourceStore.PropTypes )
            {
                if ( propType.HasFlag( PropTypeFlags.CountUnread ) )
                {
                    bool hasLink = res.HasProp( propType.Id );
                    if ( !hasLink && propType.HasFlag( PropTypeFlags.DirectedLink ) )
                    {
                        hasLink = res.HasProp( -propType.Id );
                    }
                    if ( hasLink )
                    {
                        IResourceList linkList = res.GetLinksOfType( null, propType.Id );
                        count += linkList.Count;
                        unreadCountedLinks = linkList.Union( unreadCountedLinks, true );
                    }
                }
            }

            return unreadCountedLinks;
        }

        /// <summary>
        /// Returns the count of resources in the specified list that have the 
        /// Unread flag set.
        /// </summary>
        private int CountUnreadResources( IResourceList list )
        {
            return ( list == null ) ? 0 :
                list.Intersect( Core.ResourceStore.FindResourcesWithProp( null, _coreProps.IsUnread ), true ).Minus(
                Core.ResourceStore.FindResourcesWithProp( null, _coreProps.IsDeleted ) ).Count;
        }
    }
}
