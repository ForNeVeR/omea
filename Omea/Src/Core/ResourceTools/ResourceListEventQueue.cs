/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	/**
	 * Possible types of ResourceList changes.
	 */

    public enum EventType
    {
        Add, Remove, Change
    }
    
    /**
	 * A notification on a single change in a ResourceList.
	 */

    public class ResourceListEvent
    {
        private IResourceList _resList;
        private EventType _eventType;
        private int _resourceID;
        private int _index;
        private int _listIndex;
        private IPropertyChangeSet _changeSet;

        internal ResourceListEvent( IResourceList resList, EventType eventType, int resourceID, int index )
        {
            _resList    = resList;
            _eventType  = eventType;
            _resourceID = resourceID;
            _index      = index;
            _listIndex  = index;
        }

        internal ResourceListEvent( IResourceList resList, EventType eventType, int resourceID, int index, IPropertyChangeSet changeSet )
            : this( resList, eventType, resourceID, index )
        {
            _changeSet = changeSet;
        }

        public EventType EventType
        {
            get { return _eventType; }
        }

        public int ResourceID
        {
            [DebuggerStepThrough] get { return _resourceID; }
        }

        public int Index
        {
            get { return _index; }
        }
        
        internal void SetIndex( int value )
        {
            if ( value < 0 )
            {
                throw new ArgumentOutOfRangeException( "value", 
                    "Trying to set negative index in ResourceListEvent: value=" + value );
            }
            _index = value;
        }

        public int ListIndex
        {
            get { return _listIndex; }
        }

        internal void SetListIndex( int value )
        {
            _listIndex = value;
        }

        public IPropertyChangeSet ChangeSet
        {
            get
            {
                if ( _eventType != EventType.Change )
                    throw new InvalidOperationException( "Only Change events can have a PropID value" );
                return _changeSet;
            }
        }
        
        internal void SetChangeSet( IPropertyChangeSet changeSet )
        {
            _changeSet = changeSet;
        }

        public IResourceList ResourceList
        {
            get { return _resList; }
        }

        public override string ToString()
        {
            string s = "";
            switch( EventType )
            {
                case EventType.Add:    s = "A"; break;
                case EventType.Change: s = "C"; break;
                case EventType.Remove: s = "R"; break;
            }
            return s + Index + "/" + ResourceID;
        }

    }
    
    /**
	 * A queue for ResourceList events which can merge and modify unprocessed
     * events to ensure that the receiving side always receives consistent
     * information.
	 */

    public class ResourceListEventQueue
	{
		private IResourceList _resList;
        private ArrayList _events = new ArrayList();
        private int _processingLevel = 0;
        private bool _needMerge = false;
        private bool _mergeEvents = true;

        public ResourceListEventQueue() {}

        public ResourceListEventQueue( bool mergeEvents )
        {
            _mergeEvents = mergeEvents;
        }

        public void Attach( IResourceList resList )
        {
            if ( _resList != null )
            {
                Detach();
            }

            Clear();

            _resList = resList;
            if ( _resList != null )
            {
                _resList.ResourceAdded    += new ResourceIndexEventHandler( OnResourceAdded );
                _resList.ResourceDeleting += new ResourceIndexEventHandler( OnResourceDeleting );
                _resList.ResourceChanged  += new ResourcePropIndexEventHandler( OnResourceChanged );
                int cnt = _resList.Count;  // force instantiation
                if ( _processingLevel > 0 )
                {
                    Monitor.Enter( _resList );
                }
            }
        }

        public void Detach()
        {
            if ( _resList != null )
            {
                IResourceList resList = _resList;
                _resList = null;
                if ( _processingLevel > 0 )
                {
                    Monitor.Exit( resList );
                }
                resList.ResourceAdded    -= new ResourceIndexEventHandler( OnResourceAdded );
                resList.ResourceDeleting -= new ResourceIndexEventHandler( OnResourceDeleting );
                resList.ResourceChanged  -= new ResourcePropIndexEventHandler( OnResourceChanged );
                Clear();
            }
        }

        public bool BeginProcessEvents()
        {
            if ( _resList == null )
                return false;

            if ( Interlocked.Increment( ref _processingLevel ) == 1 )
            {
                Monitor.Enter( _resList );   // to prevent deadlock, always lock the list before locking the queue
                // (when we enter ResourceDeleting, the list lock is held 
                // and then the queue locks itself)
                Monitor.Enter( _events );
            }

            if ( _processingLevel == 1 && _needMerge )
            {
                DoMergeEvents();
            }
            return true;
        }

        public void EndProcessEvents()
        {
            if ( _processingLevel == 0 )
                throw new InvalidOperationException( "Attempt to call EndProcessEvents() without matching BeginProcessEvents()" );
            
            if ( Interlocked.Decrement( ref _processingLevel ) == 0 )
            {
                Monitor.Exit( _events );
                if ( _resList != null )
                {
                    Monitor.Exit( _resList );
                }
            }
        }

        public ResourceListEvent GetNextEvent()
        {
            if ( _processingLevel == 0 )
                throw new InvalidOperationException( "You need to call BeginProcessEvents() before calling GetNextEvent()" );

            while( _events.Count > 0 )
            {
                ResourceListEvent ev = (ResourceListEvent) _events [0];
                _events.RemoveAt( 0 );
                if ( ev.ResourceList == _resList )
                {
                    return ev;                    
                }
            }
            
            return null;
        }

        public void Clear()
        {
            if ( _resList != null )
            {
                lock( _resList )
                {
                    lock( _events )
                    {
                        _events.Clear();
                    }
                }
            }
            else
            {
                lock( _events )
                {
                    _events.Clear();
                }
            }
        }

        public bool IsEmpty()
        {
            if ( !_mergeEvents )
            {
                // no need to lock here - it's only called from the resource thread, which
                // is the only thread that can add events, so it's not possible to get
                // incorrect results here
                return _events.Count == 0;
            }

            if ( _resList != null )
            {
                lock( _resList )
                {
                    lock( _events )
                    {
                        DoMergeEvents();
                        return _events.Count == 0;
                    }
                }
            }
            else
            {
                return true;
            }
        }

        private void DoMergeEvents()
        {
            if ( !_mergeEvents )
            {
                return;
            }

            // C0 A1 A2 A3 R1  

            for( int i=_events.Count-1; i >= 0; i-- )
            {
                //             i
                // C0 A1 A2 A3 R1  

                ResourceListEvent ev = (ResourceListEvent) _events [i];
                if ( ev.ResourceList != _resList )
                {
                    _events.RemoveAt( i );
                    continue;
                }

                if ( ev.EventType == EventType.Remove )
                {
                    bool discardRemove = false;

                    // Remove eats all previous events with the same resource, up to and including the Add

                    //          j  i
                    // C0 A1 A2 A3 R1  

                    for( int j=i-1; j >= 0; j-- )
                    {
                        ResourceListEvent ev2 = (ResourceListEvent) _events [j];
                        if ( ev2.ResourceID == ev.ResourceID )
                        {
                            //    j        i
                            // C0 A1 A2 A3 R1  

                            _events.RemoveAt( j );

                            //    j        i
                            // C0 A2 A3 R1  
                            
                            i--;

                            //    j     i
                            // C0 A2 A3 R1  
                            
                            if ( ev2.EventType == EventType.Add )
                            {
                                // adjust the index of events following the removed Add
                                discardRemove = true;

                                //    jk     i
                                // C0 A2 A3 R1  

                                int maxAdjustIndex = ev2.Index;
                                
                                for( int k=j; k <= i; k++ )
                                {
                                    ResourceListEvent ev3 = (ResourceListEvent) _events [k];
                                    if ( ev3.EventType == EventType.Remove && ev3.Index < maxAdjustIndex )
                                    {
                                        maxAdjustIndex--;
                                        if ( maxAdjustIndex < 0 )
                                        {
                                            throw new ApplicationException( "ResourceList event queue internal error: negative maxAdjustIndex" );
                                        }
                                    }
                                    else if ( ev3.Index > maxAdjustIndex )
                                    {
                                        if ( ev3.Index <= 0 )
                                        {
                                            throw new ApplicationException( "ResourceList event queue internal error: negative index on" + ev.ToString() );
                                        }
                                        ev3.SetIndex( ev3.Index-1 );
                                    }
                                }
                                //    j     ik
                                // C0 A1 A2 R1  
                                break;
                            }
                        }
                    }
                    if ( discardRemove )
                    {
                        Debug.Assert( _events [i] == ev );
                        _events.RemoveAt( i );
                        continue;
                    }
                    //    j     ik
                    // C0 A1 A2  
                }
                else if ( ev.EventType == EventType.Change )
                {
                    bool discardChange = false;
                    for( int j=i-1; j >= 0; j-- )
                    {
                        ResourceListEvent ev2 = (ResourceListEvent) _events [j];
                        if ( ev2.ResourceID == ev.ResourceID )
                        {
                            if ( ev2.EventType == EventType.Change )
                            {
                                ev2.SetChangeSet( ev.ChangeSet.Merge( ev2.ChangeSet ) );
                            }
                            discardChange = true;
                            break;
                        }
                    }
                    if ( discardChange )
                    {
                        Debug.Assert( _events [i] == ev );
                        _events.RemoveAt( i );
                        continue;
                    }
                }

                if ( ev.EventType != EventType.Remove )
                {
                    int index = _resList.IndexOf( ev.ResourceID );
                    if ( index < 0 )
                    {
                        throw new InvalidOperationException( "ResourceListEventQueue internal error: resource " +
                            ev.ResourceID + " not found in list (event" + ev.ToString() + ")" );
                    }
                    ev.SetListIndex( index );
                }
            }

            _needMerge = false;
        }

        private void DumpQueue( ArrayList events )
        {
            foreach( ResourceListEvent ev in events )
            {
                Console.Write( ev.ToString() );
                Console.Write( " " );
            }
            Console.WriteLine( "" );
        }

        private void OnResourceAdded( object sender, ResourceIndexEventArgs e )
        {
            if ( _resList == null || e.Index < 0 )
                return;
            
            lock( _resList )
            {
                lock( _events )
                {
                    ResourceListEvent ev = new ResourceListEvent( _resList, EventType.Add, e.Resource.Id, e.Index );
                    _events.Add( ev );
                    _needMerge = true;
                }
            }
        }

        private void OnResourceDeleting( object sender, ResourceIndexEventArgs e )
        {
            if ( _resList == null || e.Index < 0 )
                return;

            lock( _events )
            {
                ResourceListEvent ev = new ResourceListEvent( _resList, EventType.Remove, e.Resource.Id, e.Index );
                _events.Add( ev );
                _needMerge = true;
            }
        }

        private void OnResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            IResourceList resList = _resList;
            if ( resList == null || e.Index < 0 )
                return;

            lock( resList )
            {
                lock( _events )
                {
                    if ( _resList == null )
                    {
                        return;
                    }
                    ResourceListEvent ev = new ResourceListEvent( _resList, EventType.Change, e.Resource.Id, e.Index, e.ChangeSet);
                    _events.Add( ev );
                    _needMerge = true;
                }
            }
        }
	}
}
