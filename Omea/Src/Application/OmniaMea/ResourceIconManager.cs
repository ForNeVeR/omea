/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/**
     * Manages the ImageList of resource icons.
     */
    
    internal class ResourceIconManager: IResourceIconManager
	{
        private Hashtable    _resourceIconProviders = new Hashtable();  // resource type -> IResourceIconProvider
        private Hashtable    _overlayIconProviders = new Hashtable();   // resource type -> ArrayList<IOverlayIconProvider>
        private HashMap      _iconMap = new HashMap();     // Icon -> image list index
        private ImageList    _imageList;
        private Icon         _defaultPropIcon;
        private Icon         _defaultLinkIcon;
        private int          _defaultPropIconIndex = 0;
        private int          _defaultLinkIconIndex = 0;
        private IntHashTableOfInt _propTypeIconIndexes = new IntHashTableOfInt();
        private IntHashTable _propTypeIcons = new IntHashTable();
        private HashMap      _largeIcons = new HashMap();     // resource type -> Icon
        private static readonly int[] _emptyIconList = new int[] {};

        public ResourceIconManager( ImageList imageList )
		{
            _imageList = imageList;
		}

        public ImageList ImageList
        {
            get { return _imageList; }
        }

        public ColorDepth IconColorDepth
        {
            get { return _imageList.ColorDepth; }
        }

        public void SetDefaultPropIcons( Icon propIcon, Icon linkIcon )
        {
            _defaultPropIcon = propIcon;
            _defaultLinkIcon = linkIcon;
            _defaultPropIconIndex = AddIconIfNew( propIcon );
            _defaultLinkIconIndex = AddIconIfNew( linkIcon );
        }

        public void RegisterResourceIconProvider( string resType, IResourceIconProvider provider )
        {
            _resourceIconProviders [resType] = provider;
        }

        public void RegisterResourceIconProvider( string[] resTypes, IResourceIconProvider provider )
        {
            foreach( string type in resTypes )
            {
                _resourceIconProviders [type] = provider;
            }
        }

        public void RegisterOverlayIconProvider( string resType, IOverlayIconProvider provider )
        {
            if ( resType == null )
            {
                resType = "*";
            }
            ArrayList providers = (ArrayList) _overlayIconProviders [resType];
            if ( providers == null )
            {
                providers = new ArrayList();
                _overlayIconProviders [resType] = providers;
            }
            providers.Add( provider );
        }

        public IResourceIconProvider GetResourceIconProvider( string resType )
        {
            return (IResourceIconProvider) _resourceIconProviders [resType];
        }

        public int GetIconIndex( IResource res )
        {
            if ( Core.ResourceStore == null )
                return 0;

            IResourceIconProvider provider = (IResourceIconProvider) _resourceIconProviders [res.Type];

            if ( provider != null )
            {
                Icon icon = provider.GetResourceIcon( res );
                return AddIconIfNew( icon );
            }

            return 0;
        }

        /**
         * Returns the default icon index for the specified resource type.
         */
        
        public int GetDefaultIconIndex( string resType )
        {
            if ( Core.ResourceStore == null )
                return 0;

            IResourceIconProvider provider = (IResourceIconProvider) _resourceIconProviders [resType];

            if ( provider != null )
            {
                Icon icon = provider.GetDefaultIcon( resType );
                return AddIconIfNew( icon );
            }

            return 0;
        }

        public int[] GetOverlayIconIndices( IResource res )
        {
            IntArrayList result = null;
            ProcessOverlayIconProviders( res, res.Type, ref result );
            ProcessOverlayIconProviders( res, "*", ref result );
            if ( result != null )
            {
                return result.ToArray();
            }
            return _emptyIconList;
        }

        private void ProcessOverlayIconProviders( IResource res, string resType, ref IntArrayList result )
        {
            ArrayList overlayIconProviders = (ArrayList) _overlayIconProviders [resType];
            if ( overlayIconProviders != null )
            {
                foreach( IOverlayIconProvider provider in overlayIconProviders )
                {
                    Icon[] icons = provider.GetOverlayIcons( res );
                    if ( icons != null )
                    {
                        for( int i=0; i<icons.Length; i++ )
                        {
                            if ( result == null )
                            {
                                result = new IntArrayList();
                            }
                            result.Add( AddIconIfNew( icons [i] ) );
                        }
                    }
                }
            }
        }

        public int GetPropTypeIconIndex( int propId )
	    {
	        if ( propId < 0 && Core.ResourceStore.PropTypes [propId].HasFlag( PropTypeFlags.DirectedLink ) )
	        {
	            propId = -propId;
	        }
            int iconIndex = _propTypeIconIndexes [propId];
            if ( iconIndex != _propTypeIconIndexes.MissingKeyValue )
            {
                return iconIndex;
            }

            if ( Core.ResourceStore.PropTypes [propId].DataType == PropDataType.Link )
	        {
	            return _defaultLinkIconIndex;
	        }
            return _defaultPropIconIndex;
	    }

        internal Icon GetPropTypeIcon( int propId )
        {
            if ( propId < 0 && Core.ResourceStore.PropTypes [propId].HasFlag( PropTypeFlags.DirectedLink ) )
            {
                propId = -propId;
            }
            Icon icon = (Icon) _propTypeIcons [propId];
            if ( icon != null )
            {
                return icon;
            }

            if ( Core.ResourceStore.PropTypes [propId].DataType == PropDataType.Link )
            {
                return _defaultLinkIcon;
            }
            return _defaultPropIcon;
        }

        public void RegisterPropTypeIcon( int propId, Icon icon )
        {
            _propTypeIcons [propId] = icon;
            _propTypeIconIndexes [propId] = AddIconIfNew( icon );
        }

	    public void RegisterResourceLargeIcon( string resType, Icon icon )
	    {
            _largeIcons [resType] = icon;
	    }

	    public Icon GetResourceLargeIcon( string resType )
	    {
	        return (Icon) _largeIcons [resType];
	    }

	    /**
         * Adds the icon to the list if it was not already added and returns its index.
         */

        private int AddIconIfNew( Icon icon )
        {
            if ( icon == null )
            {
                return 0;
            }

            if ( _iconMap.Contains( icon ) )
            {
                return (int) _iconMap [icon];
            }
            int index = _imageList.Images.Count;
            _imageList.Images.Add( icon );
            _iconMap [icon] = index;
            return index;
        }

        public Hashtable  CollectAssemblyIcons()
        {
            Hashtable icons = new Hashtable();
            Assembly[] asmList = AppDomain.CurrentDomain.GetAssemblies();
            foreach( Assembly asm in asmList )
            {
                try
                {
                    string[] manifestResources = asm.GetManifestResourceNames();
                    foreach( string filename in manifestResources )
                    {
                        try
                        {
                            if( filename.ToLower().EndsWith( ".ico" ) )
                            {
                                Stream iconStream = asm.GetManifestResourceStream( filename );
                                if ( iconStream != null )
                                {
                                    Icon icon = new Icon( iconStream );
                                    icons[ filename ] = icon;
                                }
                            }
                        }
                        catch( Exception exc )
                        {
                            Core.ReportException( exc, false );
                        }
                    }
                }
                catch( Exception )
                {
                }
            }
            return icons;
        }
	}

    /// <summary>
    /// Icon provider for resources of type ResourceType.
    /// </summary>
    internal class ResourceTypeIconProvider: IResourceIconProvider
    {
        private Icon _defaultIcon;

        public ResourceTypeIconProvider( Icon defaultIcon )
        {
            _defaultIcon = defaultIcon;
        }

        public Icon GetResourceIcon( IResource resource )
        {
            if ( resource.Type != "ResourceType" )
                throw new ArgumentException( "Resource of type ResourceType expected" );

            string resType = resource.GetStringProp( "Name" );
            IResourceIconProvider provider = Core.ResourceIconManager.GetResourceIconProvider( resType );
            if ( provider == null )
            {
                return _defaultIcon;
            }
            return provider.GetDefaultIcon( resType );
        }

        public Icon GetDefaultIcon( string resType )
        {
            return _defaultIcon;
        }
    }

    /// <summary>
    /// Icon provider for resources of type PropType.
    /// </summary>
    internal class PropTypeIconProvider: IResourceIconProvider
    {
        private ResourceIconManager _manager;
        private Icon _defaultIcon;

        public PropTypeIconProvider( ResourceIconManager manager, Icon defaultIcon )
        {
            _manager = manager;
            _defaultIcon = defaultIcon;
        }

        public Icon GetResourceIcon( IResource resource )
        {
            if ( resource.Type != "PropType" )
                throw new ArgumentException( "Resource of type ResourceType expected" );

            int propId = resource.GetIntProp( "ID" );
            return _manager.GetPropTypeIcon( propId );
        }

        public Icon GetDefaultIcon( string resType )
        {
            return _defaultIcon;
        }
    }
}
