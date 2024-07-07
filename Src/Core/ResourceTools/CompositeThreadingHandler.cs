// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	/// <summary>
	/// A threading handler which aggregates different sub-handlers based on the resource type.
	/// </summary>
	public class CompositeThreadingHandler: IResourceThreadingHandler
	{
		private Hashtable _handlerMap = new Hashtable();  // resource type -> IResourceThreadingHandler
        private IntHashTable _propHandlerMap = new IntHashTable();   // source link type -> IResourceThreadingHandler

        public void AddHandler( string resType, IResourceThreadingHandler handler )
        {
            _handlerMap [resType] = handler;
        }

        public void AddHandler( int propId, IResourceThreadingHandler handler )
        {
            _propHandlerMap [propId] = handler;
        }

        private IResourceThreadingHandler GetResourceThreadingHandler( IResource res )
        {
            IResourceThreadingHandler handler = (IResourceThreadingHandler) _handlerMap [res.Type];
            if ( handler == null )
            {
                lock( _propHandlerMap )
                {
                    foreach( IntHashTable.Entry entry in _propHandlerMap )
                    {
                        if ( res.HasProp( entry.Key ) )
                        {
                            handler = (IResourceThreadingHandler) entry.Value;
                            break;
                        }
                    }
                }
            }
            return handler;
        }

	    public IResource GetThreadParent( IResource res )
	    {
	        IResourceThreadingHandler handler = GetResourceThreadingHandler( res );
            if ( handler != null )
            {
                return handler.GetThreadParent( res );
            }
            return null;
	    }

	    public IResourceList GetThreadChildren( IResource res )
	    {
            IResourceThreadingHandler handler = GetResourceThreadingHandler( res );
            if ( handler != null )
            {
                return handler.GetThreadChildren( res );
            }
            return Core.ResourceStore.EmptyResourceList;
        }

	    public bool IsThreadChanged( IResource res, IPropertyChangeSet changeSet )
	    {
            IResourceThreadingHandler handler = GetResourceThreadingHandler( res );
            if ( handler != null )
            {
                return handler.IsThreadChanged( res, changeSet );
            }
            return false;
	    }

	    public bool CanExpandThread( IResource res, ThreadExpandReason reason )
	    {
            IResourceThreadingHandler handler = GetResourceThreadingHandler( res );
            if ( handler != null )
            {
                return handler.CanExpandThread( res, reason );
            }
            return false;
        }

	    public bool HandleThreadExpand( IResource res, ThreadExpandReason reason )
	    {
            IResourceThreadingHandler handler = GetResourceThreadingHandler( res );
            if ( handler != null )
            {
                return handler.HandleThreadExpand( res, reason );
            }
            return true;
        }

	    public IResourceThreadingHandler GetHandler( string type )
	    {
	        return (IResourceThreadingHandler) _handlerMap [type];
	    }
	}
}
