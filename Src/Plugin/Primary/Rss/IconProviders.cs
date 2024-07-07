// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.RSSPlugin
{
    internal class RSSFeedIconProvider : IResourceIconProvider, IOverlayIconProvider
    {
        private static Icon[] _updating;
        private static Icon[] _error;
        private static Icon[] _paused;
        private static Icon _default;
        private readonly FavIconManager _favIconManager;

        public RSSFeedIconProvider()
        {
            _favIconManager = (FavIconManager)Core.GetComponentImplementation( typeof(FavIconManager) );
        }

        public Icon GetResourceIcon( IResource resource )
        {
            // Try to get the feed's favicon
            Icon favicon = TryGetResourceIcon( resource );
            if( favicon != null )
                return favicon;

            // No favicon available, return resource-type's default icon (lazy loading)
            if( _default == null )
                _default = RSSPlugin.LoadIconFromAssembly( "RSSFeed.ico" );

            return _default;
        }

        /// <summary>
        /// Almost the same as <see cref="IResourceIconProvider.GetResourceIcon"/>, but returns <c>Null</c> in case a customized favicon
        /// is not available for this feed (in which case the <see cref="GetResourceIcon"/> returns the default resource type icon).
        /// </summary>
        public Icon TryGetResourceIcon( IResource resource )
        {
            string feedUrl = resource.GetStringProp( Props.URL );
            return !string.IsNullOrEmpty( feedUrl ) ? _favIconManager.GetResourceFavIcon( feedUrl ) : null;
        }

        public Icon GetDefaultIcon( string resType )
        {
            return null;
        }

        #region IOverlayIconProvider Members

        public Icon[] GetOverlayIcons( IResource resource )
        {
            string updateStatus = resource.GetStringProp( Props.UpdateStatus );
            if( updateStatus == "(updating)" )
            {
                if( _updating == null )
                {
                    _updating = new Icon[1];
                    _updating[ 0 ] = RSSPlugin.LoadIconFromAssembly( "updating.ico" );
                }
                return _updating;
            }
            if( updateStatus == "(error)" )
            {
                if( _error == null )
                {
                    _error = new Icon[1];
                    _error[ 0 ] = RSSPlugin.LoadIconFromAssembly( "error.ico" );
                }
                return _error;
            }
            if( resource.HasProp( Props.IsPaused ) )
            {
                if( _paused == null )
                {
                    _paused = new Icon[1];
                    _paused[ 0 ] = RSSPlugin.LoadIconFromAssembly( "RSSFeedPaused.ico" );
                }
                return _paused;
            }
            return null;
        }

        #endregion
    }

    /// <summary>
    /// Provides an icon for the RSS Items, so that the icon corresponds to the icon of the parent feed.
    /// </summary>
    internal class RSSItemIconProvider : IResourceIconProvider
    {
        /// <summary>
        /// An icon provider that gives an icon for the RSS Feed resources.
        /// </summary>
        private readonly RSSFeedIconProvider _feedprovider;

        /// <summary>
        /// A default icon for the RSS items.
        /// </summary>
        private Icon _iconDefault;

        /// <summary>
        /// A default icon for the unread RSS items.
        /// </summary>
        private Icon _iconDefaultUnread;

        /// <summary>
        /// Specifies whether the feed item should display the same favicon as the feed does.
        /// This variable caches the corresponding value from Omea Settings.
        /// </summary>
        private bool _bUseFeedIcon = true;

        /// <summary>
        /// Constructs the object.
        /// </summary>
        /// <param name="feedprovider">Icon provider for the feed, which has a favicon as a resource icon.
        /// If the corresponding option is enabled, the feed icon is propagated to the feed item icon.</param>
        public RSSItemIconProvider( RSSFeedIconProvider feedprovider )
        {
            _feedprovider = feedprovider;

            // Read the settings and listen to changes in them
            Core.UIManager.AddOptionsChangesListener( "Internet", "Feeds", OnSettingsChanged );
            OnSettingsChanged( null, EventArgs.Empty );
        }

        #region IResourceIconProvider Members

        public Icon GetResourceIcon( IResource resItem )
        {
            if(_bUseFeedIcon)
            {	// Return the feed's favicon for the feed item (if available)
                IResourceList parents = resItem.GetLinksTo( "RSSFeed", "RSSItem" );
                lock(parents)
                {
                    if( parents.Count != 0 )
                    {
                        IResource feed = parents[ 0 ];
                        Icon	favicon = _feedprovider.TryGetResourceIcon( feed );
                        if(favicon != null)
                            return favicon;
                    }
                    else
                        Trace.WriteLine( "Warning: parentless RSS item enountered, cannot get feed icon for it." );
                }
            }

            // Don't use the feed's favicon (either if option disabled or if the feed does not have a favicon)
            return resItem.HasProp( Core.Props.IsUnread ) ? DefaultUnread : Default;
        }

        public Icon GetDefaultIcon( string resType )
        {
            return Default;
        }

        #endregion

        #region Attributes

        /// <summary>
        /// Gets the default feed item icon (for the read resource).
        /// </summary>
        public Icon Default
        {
            get
            {
                if (_iconDefault == null)
                    _iconDefault = RSSPlugin.LoadIconFromAssembly( "RSSItem.ico" );
                return _iconDefault;
            }
        }

        /// <summary>
        /// Gets the default unread feed item icon.
        /// </summary>
        public Icon DefaultUnread
        {
            get
            {
                if( _iconDefaultUnread == null )
                    _iconDefaultUnread = RSSPlugin.LoadIconFromAssembly( "RSSItemUnread.ico" );
                return _iconDefaultUnread;
            }
        }

        #endregion

        #region Implementation

        /// <summary>
        /// Settings have changed, re-query for them.
        /// </summary>
        protected void OnSettingsChanged( object sender, EventArgs args )
        {
            _bUseFeedIcon = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.PropagateFavIconToItems, _bUseFeedIcon );
        }

        #endregion
    }
}
