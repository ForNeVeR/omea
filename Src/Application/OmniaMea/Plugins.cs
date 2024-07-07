// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.ResourceTools;
using Microsoft.Win32;

using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

using JetBrains.Omea.OpenAPI;
using System.IO;
using System.Xml;
using JetBrains.DataStructures;

namespace JetBrains.Omea.Plugins
{
	/// <summary>
    /// Plugins singleton loader.
    /// Loads plugin assemblies enumerated in the 'Plugins' reg key.
    /// Afterwards plugins may be enumerated as instances of IPlugin
    /// </summary>
    class PluginInterfaces : PluginLoader, IPluginLoader
    {

        public void RegisterResourceTextProvider( string resType, IResourceTextProvider provider )
        {
            if ( resType == null )
            {
                lock( _genericResourceTextProviders )
                {
                    _genericResourceTextProviders.Add( provider );
                }

            }
            else
            {
                ArrayList providerList = (ArrayList) _resourceTextProviders [resType];
                if ( providerList == null )
                {
                    providerList = new ArrayList();
                    _resourceTextProviders [resType] = providerList;
                }
                lock( providerList )
                {
                    providerList.Add( provider );
                }
            }
        }

        public bool HasTypedTextProvider( string resType )
        {
            #region Preconditions
            if( resType == null )
                throw new ArgumentNullException( "PluginLoader -- Input resource type can not be null." );
            #endregion Preconditions

            ArrayList providerList = (ArrayList) _resourceTextProviders[ resType ];
            return (providerList != null);
        }

        public void InvokeResourceTextProviders( IResource res, IResourceTextConsumer consumer )
        {
            #region Preconditions
            if ( res == null )
                throw new ArgumentNullException( "PluginLoader -- Resource is null." );

            if ( consumer == null )
                throw new ArgumentNullException( "PluginLoader -- IResourceTextConsumer is null." );
            #endregion Preconditions

            bool      isSuccess = true;
            ArrayList providerList = (ArrayList) _resourceTextProviders [res.Type];
            if ( providerList != null )
            {
                lock( providerList )
                {
                    foreach( IResourceTextProvider provider in providerList )
                    {
                        IResourceTextIndexingPermitter permitter = provider as IResourceTextIndexingPermitter;
                        if( permitter != null && !permitter.CanIndexResource( res ) )
                        {
                            return;
                        }
                    }
                }
            }
            lock( _genericResourceTextProviders )
            {
                foreach( IResourceTextProvider provider in _genericResourceTextProviders )
                {
                    IResourceTextIndexingPermitter permitter = provider as IResourceTextIndexingPermitter;
                    if( permitter != null && !permitter.CanIndexResource( res ) )
                    {
                        return;
                    }
                }
            }
            if ( providerList != null )
            {
                lock( providerList )
                {
                    foreach( IResourceTextProvider provider in providerList )
                    {
                        isSuccess = isSuccess && provider.ProcessResourceText( res, consumer );
                    }
                }
            }
            lock( _genericResourceTextProviders )
            {
                foreach( IResourceTextProvider provider in _genericResourceTextProviders )
                {
                    isSuccess = isSuccess && provider.ProcessResourceText( res, consumer );
                }
            }
            if( !isSuccess )
                consumer.RejectResult();
        }

        public void RegisterResourceUIHandler( string resType, IResourceUIHandler handler )
        {
            #region Preconditions
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
            {
                throw new ArgumentException( "Invalid resource type '" + resType + "'", "resType" );
            }
            #endregion

            _resourceUIHandlerHash [resType] = handler;
        }

        public IResourceUIHandler GetResourceUIHandler( string resType )
        {
            return (IResourceUIHandler) _resourceUIHandlerHash [resType];
        }

        public IResourceUIHandler GetResourceUIHandler( IResource res )
        {
            return (IResourceUIHandler) GetHandlerFromMap( res, _resourceUIHandlerHash );
        }

        public void RegisterResourceDragDropHandler( string resType, IResourceDragDropHandler handler )
        {
            #region Preconditions
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
        		throw new ArgumentException( "Invalid resource type \"" + resType + "\".", "resType" );
            #endregion Preconditions

        	lock( _resourceDragDropHandlerHash )
        	{
        		// Check if a handler is already registered
        		IResourceDragDropHandler existing = _resourceDragDropHandlerHash[ resType ] as IResourceDragDropHandler;
        		if( existing != null )
        		{
        			// The handler's registered; if it's already a composite one, add a new handler to it
        			// If it's a simple handler, create a new composite of the old and new ones
        			ResourceDragDropCompositeHandler composite = existing as ResourceDragDropCompositeHandler;
        			if( composite != null )
        				composite.AddHandler( handler );
        			else
        				_resourceDragDropHandlerHash[ resType ] = new ResourceDragDropCompositeHandler( existing, handler );
        		}
        		else // There were no handler yet, just write the raw unwrapped handler
        			_resourceDragDropHandlerHash[ resType ] = handler;
        	}
        }

    	public IResourceDragDropHandler GetResourceDragDropHandler( IResource res )
        {
			lock(_resourceDragDropHandlerHash)
				return (IResourceDragDropHandler) GetHandlerFromMap( res, _resourceDragDropHandlerHash );
        }

        public IResourceDragDropHandler GetResourceDragDropHandler( string resType )
        {
			lock(_resourceDragDropHandlerHash)
	            return (IResourceDragDropHandler) _resourceDragDropHandlerHash [resType];
        }

        public void RegisterResourceRenameHandler( string resType, IResourceRenameHandler handler )
        {
            #region Preconditions
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
            {
                throw new ArgumentException( "Invalid resource type '" + resType + "'", "resType" );
            }
            #endregion Preconditions

            _resourceRenameHandlerHash [resType] = handler;
        }

        public IResourceRenameHandler GetResourceRenameHandler( IResource res )
        {
            return (IResourceRenameHandler) GetHandlerFromMap( res, _resourceRenameHandlerHash );
        }

        private static object GetHandlerFromMap( IResource res, HashMap handlerMap )
        {
            if ( res == null || Core.ResourceStore == null )
                return null;

            object handler = handlerMap [res.Type];
            if ( handler == null )
            {
                foreach( int linkTypeId in res.GetLinkTypeIds() )
                {
                    if ( Core.ResourceStore.PropTypes [linkTypeId].HasFlag( PropTypeFlags.SourceLink ) )
                    {
                        IResource source = res.GetLinkProp( linkTypeId );
                        if ( source != null )   // the link may be reverse
                        {
                            handler = handlerMap [source.Type];
                        }
                    }
                }
            }
            return handler;
        }

        public void RegisterResourceThreadingHandler( string resType, IResourceThreadingHandler handler )
        {
            _threadingHandler.AddHandler( resType, handler );
        }

        public void RegisterResourceThreadingHandler( int propId, IResourceThreadingHandler handler )
        {
            _threadingHandler.AddHandler( propId, handler );
        }

        public void RegisterDefaultThreadingHandler( string resType, int replyProp )
        {
            _threadingHandler.AddHandler( resType, new DefaultThreadingHandler( replyProp ) );
        }

        public IResourceThreadingHandler GetResourceThreadingHandler( string resType )
        {
            return _threadingHandler.GetHandler( resType );
        }

        public IResourceThreadingHandler CompositeThreadingHandler
        {
            get { return _threadingHandler; }
        }

        public IResourceDisplayer GetResourceDisplayer( string resType )
        {
        	return (IResourceDisplayer) _resourceDisplayerHash [ resType ];
        }

        public void RegisterResourceDisplayer( string resType, IResourceDisplayer displayer )
        {
        	_resourceDisplayerHash [ resType ] = displayer;
        }

        public void RegisterNewspaperProvider( string resType, INewspaperProvider provider )
        {
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
                throw new ArgumentException( "Resource type '" + resType + "' does not exist", "resType" );

            _newspaperProviders [resType] = provider;
        }

        public INewspaperProvider GetNewspaperProvider( string resType )
        {
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
                throw new ArgumentException( "Resource type '" + resType + "' does not exist", "resType" );

            return (INewspaperProvider) _newspaperProviders [ resType ];
        }

        public void RegisterResourceDeleter( string resType, IResourceDeleter deleter )
        {
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
                throw new ArgumentException( "Resource type '" + resType + "' does not exist", "resType" );

            _resourceDeleters [ resType ] = deleter;
        }

        public IResourceDeleter GetResourceDeleter( string resType )
        {
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
                throw new ArgumentException( "Resource type '" + resType + "' does not exist", "resType" );

            return (IResourceDeleter) _resourceDeleters [resType];
        }

        public IStreamProvider GetStreamProvider( string resType )
        {
            return (IStreamProvider) _streamProviderHash [ resType ];
        }

        public void RegisterStreamProvider( string resType, IStreamProvider provider )
        {
            _streamProviderHash [ resType ] = provider;
        }


        public void RegisterResourceSerializer( string resType, IResourceSerializer serializer )
        {
            _resourceSerializers[ resType ] = serializer;
        }

        public IResourceSerializer GetResourceSerializer( string resType )
        {
            return (IResourceSerializer) _resourceSerializers [ resType ];
        }

        public void RegisterViewsConstructor( IViewsConstructor constructor )
        {
            if( _viewsConstructors.IndexOf( constructor ) == -1 )
                _viewsConstructors.Add( constructor );
        }

        public ArrayList GetViewsConstructors()
        {
            return _viewsConstructors;
        }



    	private HashMap         _resourceUIHandlerHash = new HashMap();
        private HashMap         _resourceDragDropHandlerHash = new HashMap();
        private HashMap         _resourceRenameHandlerHash = new HashMap();
        private HashMap         _resourceDisplayerHash = new HashMap();
        private HashMap         _streamProviderHash = new HashMap();
        private HashMap         _resourceTextProviders = new HashMap();   // resource type -> ArrayList<ITextIndexProvider>
        private HashMap         _resourceSerializers = new HashMap();     // resource type -> IResourceSerializer
        private HashMap         _resourceDeleters = new HashMap();        // resource type -> IResourceDeleter
        private HashMap         _newspaperProviders = new HashMap();      // resource type -> INewspaperProvider
        private ArrayList       _genericResourceTextProviders = new ArrayList();
        private ArrayList       _viewsConstructors = new ArrayList();
        private CompositeThreadingHandler _threadingHandler = new CompositeThreadingHandler();


    }
}
