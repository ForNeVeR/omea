/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Threading;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceStore
{
	internal interface IUpdateListener
	{
        void ResourceSaved( IResource resource, IPropertyChangeSet changeSet );
        void ResourceDeleting( IResource resource );
        void Trace();
        int GetKnownType();
	}
    
    /// <summary>
	/// Class which manages updating live resource lists.
	/// </summary>
	internal class ResourceListUpdateManager
	{
        private IntHashTable _typedUpdateListeners = new IntHashTable(); // <resource type,ArrayList<WeakReference<IUpdateListener>>>
        private ArrayList _untypedUpdateListeners = new ArrayList();            // <WeakReference<IUpdateListener>>
        private ArrayList _priorityUpdateListeners = new ArrayList();           // <WeakReference<IUpdateListener>>
        private ArrayList _allUpdateListeners = new ArrayList();
        private int _reentryLevel = 0;

        public void AddUpdateListener( IUpdateListener listener, bool priority )
        {
            int knownType = listener.GetKnownType();
            if ( knownType >= 0 )
            {
                ArrayList list;
                lock( _typedUpdateListeners )
                {
                    list = (ArrayList) _typedUpdateListeners [knownType];
                    if ( list == null )
                    {
                        list = new ArrayList();
                        _typedUpdateListeners [knownType] = list;
                    }
                }
                lock( list )
                {
                    list.Add( new WeakReference( listener ) );
                }
            }
            else if ( priority )
            {
                lock( _priorityUpdateListeners )
                {
                    _priorityUpdateListeners.Add( new WeakReference( listener ) );
                }
            }
            else 
            {
                lock( _untypedUpdateListeners )
                {
                    _untypedUpdateListeners.Add( new WeakReference( listener ) );
                }
            }
            lock( _allUpdateListeners )
            {
                _allUpdateListeners.Add( new WeakReference( listener ) );
            }
        }

        public void RemoveUpdateListener( IUpdateListener listener, bool priority )
        {
            int knownType = listener.GetKnownType();
            if ( knownType >= 0 )
            {
                ArrayList list;
                lock( _typedUpdateListeners )
                {
                    list = (ArrayList) _typedUpdateListeners [knownType];
                }

                if ( list != null )
                {
                    lock( list )
                    {
                        RemoveUpdateListenerFromList( list, listener );
                    }
                }
            }
            else if ( priority )
            {
                lock( _priorityUpdateListeners )
                {
                    RemoveUpdateListenerFromList( _untypedUpdateListeners, listener );
                }
            }
            else
            {
                lock( _untypedUpdateListeners )
                {
                    RemoveUpdateListenerFromList( _untypedUpdateListeners, listener );
                }
            }
            lock( _allUpdateListeners )
            {
                RemoveUpdateListenerFromList( _allUpdateListeners, listener );
            }
            CompactList( _allUpdateListeners );
        }

	    private void RemoveUpdateListenerFromList( ArrayList list, IUpdateListener listener )
	    {
	        for( int i=list.Count-1; i >= 0; i-- )
	        {
	            WeakReference weakRef = (WeakReference) list [i];
	            if ( weakRef.IsAlive && weakRef.Target == listener )
	            {
	                weakRef.Target = null;
	            }
	        }
	    }

	    public int ListenerCount
        {
            get { return _allUpdateListeners.Count; }
        }

        public void NotifyResourceSaved( IResource resource, IPropertyChangeSet changeSet )
        {
            Interlocked.Increment( ref _reentryLevel );
            ArrayList list;
            lock( _typedUpdateListeners )
            {
                list = (ArrayList) _typedUpdateListeners [resource.TypeId];
            }

            if ( list != null )
            {
                NotifyList( list, resource, changeSet, false );
            }

            if ( changeSet != null && changeSet.IsPropertyChanged( ResourceProps.Type ) )
            {
                int oldType = (int) changeSet.GetOldValue( ResourceProps.Type );
                lock( _typedUpdateListeners )
                {
                    list = (ArrayList) _typedUpdateListeners [oldType];
                }
                if ( list != null )
                {
                    NotifyList( list, resource, changeSet, false );
                }
            }
            
            NotifyList( _priorityUpdateListeners, resource, changeSet, false );
            NotifyList( _untypedUpdateListeners, resource, changeSet, false );
            Interlocked.Decrement( ref _reentryLevel );
        }

        public void NotifyResourceDeleting( IResource resource )
        {
            Interlocked.Increment( ref _reentryLevel );
            ArrayList list;
            lock( _typedUpdateListeners )
            {
                list = (ArrayList) _typedUpdateListeners [resource.TypeId];
            }
            if ( list != null )
            {
                NotifyList( list, resource, null, true );
            }
            
            NotifyList( _priorityUpdateListeners, resource, null, true );
            NotifyList( _untypedUpdateListeners, resource, null, true );
            Interlocked.Decrement( ref _reentryLevel );
        }

        private void NotifyList( ArrayList list, IResource resource, IPropertyChangeSet changeSet, bool deleting )
	    {
            bool needCompact = false;
            int startCount = list.Count;
            for( int index=0; index<startCount; index++ )
            {
                IUpdateListener listener = null;
                lock( list )
                {
                    WeakReference weakRef = (WeakReference) list [index];
                    if ( !weakRef.IsAlive || weakRef.Target == null )
                    {
                        needCompact = true;
                        continue;
                    }
                    listener = (IUpdateListener) weakRef.Target;
                }
                if ( deleting )
                {
                    listener.ResourceDeleting( resource );
                }
                else
                {
                    listener.ResourceSaved( resource, changeSet );
                }
            }
            if ( needCompact && _reentryLevel == 1 )
            {
                CompactList( list );
            }
	    }

        private void CompactList( ArrayList list )
        {
            lock( list )
            {
                for( int i=list.Count-1; i >= 0; i-- )
                {
                    WeakReference weakRef = (WeakReference) list [i];
                    if ( !weakRef.IsAlive || weakRef.Target == null )
                    {
                        list.RemoveAt( i );
                    }
                }
            }
        }

        public void TraceListeners()
        {
            for( int i=_allUpdateListeners.Count-1; i >= 0; i-- )
            {
                WeakReference weakRef = (WeakReference) _allUpdateListeners [i];
                if ( !weakRef.IsAlive )
                {
                    _allUpdateListeners.RemoveAt( i );
                }
                else
                {
                    (weakRef.Target as IUpdateListener).Trace();
                }
            }
        }
	}
}
