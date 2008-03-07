/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;
using System.Text;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
    public struct BookmarkChange
    {
        public int type; // 0 - created or updated or renamed, 1 - deleted, 2 - moved
        public int id;
        public string rdfid;
        public string oldparent;
        public int oldparent_id;
        public string parent;
        public int parent_id;
        public string name;
        public string url;
    }

    internal class MozillaBookmarkProfile: IBookmarkProfile, ICookieProvider
    {
        public MozillaBookmarkProfile( string name, IBookmarkService bookmarkService )
        {
            _bookmarkservice = bookmarkService;
            _profileName = name;
            _lastCookiesFileTime = DateTime.MinValue;
            _cookies = new HashMap( 100 );
        }

        internal bool IsActive
        {
            get { return ImportAllowed && _isActive; }
            set { _isActive = value; }
        }

        internal bool ImportAllowed
        {
            get { return _importAllowed; }
            set { _importAllowed = value; }
        }

        internal static void SetImportPropertiesOfProfiles()
        {
            IBookmarkService service = (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
            string[] profiles = Core.SettingStore.ReadString( "Favorites", "MozillaProfile" ).ToLower().Split( ';' );
            foreach( IBookmarkProfile prf in service.Profiles )
            {
                MozillaBookmarkProfile mprf = prf as MozillaBookmarkProfile;
                if( mprf != null )
                {
                    mprf.ImportAllowed = Array.IndexOf( profiles, mprf._profileName.ToLower() ) >= 0;
                }
            }
        }

        #region IBookmarkProfile implementation

        public string Name
        {
            get { return "Mozilla/" + _profileName; }
        }

        public void StartImport()
        {
            _root = _bookmarkservice.GetProfileRoot( this );
            if( !_root.IsDeleted )
            {
                if( !_importAllowed )
                {
                    _root.SetProp( FavoritesPlugin._propInvisible, true );
                }
                else
                {
                    _root.DeleteProp( FavoritesPlugin._propInvisible );
                    MozillaProfile activeProfile = MozillaProfiles.GetProfile( _profileName );
                    if( activeProfile != null )
                    {
                        ArrayList bookmarks = new ArrayList();
                        IEnumerator enumerator = activeProfile.GetEnumerator();
                        while( enumerator.MoveNext() )
                        {
                            object current = enumerator.Current;
                            if( current is string ) // is description of last added bookmark?
                            {
                                if( bookmarks.Count > 0 )
                                {
                                    ( (MozillaBookmark) bookmarks[ bookmarks.Count - 1 ] ).Description = current as string;
                                }
                            }
                            else
                            {
                                bookmarks.Add( current );
                            }
                        }
                        int index = 0;
                        CollectBookmarks(
                            _root, (MozillaBookmark[]) bookmarks.ToArray( typeof( MozillaBookmark ) ), ref index, 0 );
                    }
                }
                FavoritesPlugin._favoritesTreePane.UpdateNodeFilter( false );
            }
        }

        public char[] InvalidNameChars
        {
            get { return Path.InvalidPathChars; }
        }

        public bool CanCreate( IResource res, out string error )
        {
            if( IsActive )
            {
                error = string.Empty;
                return true;
            }
            error = ( ImportAllowed ) ? _notActiveErrorString : _importDisabledErrorString;
            error = "Cannot modify. " + error;
            return false;
        }

        public bool CanRename( IResource res, out string error )
        {
            if( IsActive )
            {
                error = string.Empty;
                return true;
            }
            error = ( ImportAllowed ) ? _notActiveErrorString : _importDisabledErrorString;
            error = "Cannot rename. " + error;
            return false;
        }

        public bool CanMove( IResource res, IResource parent, out string error )
        {
            if( IsActive )
            {
                error = string.Empty;
                return true;
            }
            error = ( ImportAllowed ) ? _notActiveErrorString : _importDisabledErrorString;
            return false;
        }

        public bool CanDelete( IResource res, out string error )
        {
            if( IsActive )
            {
                error = string.Empty;
                return true;
            }
            error = ( ImportAllowed ) ? _notActiveErrorString : _importDisabledErrorString;
            return false;
        }

        public void Create( IResource res )
        {
            Rename( res, res.GetPropText( Core.Props.Name ) );
        }

        public void Rename( IResource res, string newName )
        {
            BookmarkChange change = new BookmarkChange();
            change.type = 0;
            change.id = res.Id;
            change.rdfid = res.GetPropText( FavoritesPlugin._propBookmarkId );
            IResource parent = BookmarkService.GetParent( res );
            if( parent != null )
            {
                change.parent = GetFolderBookmarkId( parent );
                change.parent_id = parent.Id;
            }
            change.name = newName;
            change.url = res.GetPropText( FavoritesPlugin._propURL );
            LogBookmarkChange( change );
        }

        public void Move( IResource res, IResource parent, IResource oldParent )
        {
            BookmarkChange change = new BookmarkChange();
            change.type = 2;
            change.rdfid = res.GetPropText( FavoritesPlugin._propBookmarkId );
            change.parent = GetFolderBookmarkId( parent );
            change.oldparent = GetFolderBookmarkId( oldParent );
            change.parent_id = parent.Id;
            change.oldparent_id = oldParent.Id;
            LogBookmarkChange( change );
        }

        public void Delete( IResource res )
        {
            BookmarkChange change = new BookmarkChange();
            change.type = 1;
            change.id = res.Id;
            change.rdfid = res.GetPropText( FavoritesPlugin._propBookmarkId );
            IResource parent = res.GetLinkProp( FavoritesPlugin._propParent );
            if( parent != null )
            {
                change.parent = GetFolderBookmarkId( parent );
                change.parent_id = parent.Id;
            }
            LogBookmarkChange( change );
        }

        public void Dispose()
        {
            _cookies.Clear();
        }

        #endregion

        #region ICookieProvider implementation

        public string GetCookies( string url )
        {
            MozillaProfile activeProfile = MozillaProfiles.GetProfile( _profileName );
            string cookiesFile = IOTools.Combine( activeProfile.Path, "cookies.txt" );
            DateTime ftime = IOTools.GetFileLastWriteTime( cookiesFile );
            if( ftime > _lastCookiesFileTime )
            {
                _cookies.Clear();
                using( StreamReader reader = new StreamReader( cookiesFile ) )
                {
                    string line;
                    while( ( line = reader.ReadLine() ) != null )
                    {
                        if( line.Length == 0 || line.StartsWith( "#" ) )
                        {
                            continue;
                        }
                        string[] cookieProps = line.Split( '\t' );
                        if( cookieProps.Length > 6 )
                        {
                            string cookie = cookieProps[ 5 ] + '=' + cookieProps[ 6 ];
                            string domain = cookieProps[ 0 ];
                            if( domain.StartsWith( "." ) )
                            {
                                SetUrlCookie( domain.TrimStart( '.' ), cookieProps[ 2 ], cookie );
                                domain = "www" + domain;
                            }
                            SetUrlCookie( domain, cookieProps[ 2 ], cookie );
                        }
                    }
                }
                _lastCookiesFileTime = ftime;
            }
            return (string) _cookies[ url ];
        }

        public void SetCookies( string url, string cookies )
        {
        }

        #endregion
        
        #region implementation details

        private string GetFolderBookmarkId( IResource res )
        {
            string result = res.GetPropText( FavoritesPlugin._propBookmarkId );
            if( result.Length == 0 && res == _bookmarkservice.GetProfileRoot( this ) )
            {
                result = "root";
            }
            return result;
        }

        private void CollectBookmarks( IResource parentFolder, MozillaBookmark[] bookmarks, ref int index, int level )
        {
            Guard.NullArgument( parentFolder, "parentFolder" );

            if( index >= bookmarks.Length )
            {
                return;
            }
            HashMap weblinks = new HashMap(); // urls 2 resources
            HashMap folders = new HashMap(); // folder names 2 resources
            IResourceList childs = BookmarkService.SubNodes( null, parentFolder );

            // at first, collect all child folders and bookmarks
            foreach( IResource child in childs.ValidResources )
            {
                string id = child.GetPropText( FavoritesPlugin._propBookmarkId );
                if( id.Length > 0 )
                {
                    if( child.Type == "Folder" )
                    {
                        folders[ id ] = child;
                    }
                    else if( child.Type == "Weblink" )
                    {
                        weblinks[ id ] = child;
                    }
                    else
                    {
                        child.DeleteLink( FavoritesPlugin._propParent, parentFolder );
                    }
                }
            }

            // look through folders and bookmarks on current level and. recursively, on sub-levels
            while( index < bookmarks.Length )
            {
                MozillaBookmark bookmark = bookmarks[ index ];
                if( bookmark.Level < level )
                {
                    break;
                }
                level = bookmark.Level;
                ++index;
                string id = bookmark.Id;
                if( bookmark.IsFolder )
                {
                    IResource folder = (IResource) folders[ id ];
                    if( folder == null )
                    {
                        folder = _bookmarkservice.FindOrCreateFolder( parentFolder, bookmark.Folder );
                    }
                    else
                    {
                        folders.Remove( id );
                        _bookmarkservice.SetName( folder, bookmark.Folder );
                        _bookmarkservice.SetParent( folder, parentFolder );
                    }
                    folder.SetProp( FavoritesPlugin._propBookmarkId, id );
                    CollectBookmarks( folder, bookmarks, ref index, level + 1 );
                }
                else
                {
                    string url = bookmark.Url;
                    IResource weblink = (IResource) weblinks[ id ];
                    if( weblink == null )
                    {
                        weblink = _bookmarkservice.FindOrCreateBookmark( parentFolder, bookmark.Name, url );
                    }
                    else
                    {
                        weblinks.Remove( id );
                        _bookmarkservice.SetName( weblink, bookmark.Name );
                        _bookmarkservice.SetUrl( weblink, url );
                        _bookmarkservice.SetParent( weblink, parentFolder );
                    }
                    if( weblink != null && bookmark.Description != null && bookmark.Description.Length > 0 )
                    {
                        weblink.SetProp( "Annotation", bookmark.Description );
                    }
                    weblink.SetProp( FavoritesPlugin._propBookmarkId, id );
                }
            }
            // look through obsolete folders and bookmarks, delete them
            foreach( HashMap.Entry E in folders )
            {
                _bookmarkservice.DeleteFolder( (IResource) E.Value );
            }
            foreach( HashMap.Entry E in weblinks )
            {
                _bookmarkservice.DeleteBookmark( (IResource) E.Value );
            }
        }

        private delegate void LogBookmarkChangeDelegate( BookmarkChange change );

        private void LogBookmarkChange( BookmarkChange change )
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.QueueJob( new LogBookmarkChangeDelegate( LogBookmarkChange ), change );
            }
            else
            {
                StringBuilder historyStr = StringBuilderPool.Alloc();
                try 
                {
                    historyStr.AppendFormat( "{0}\x01{1}\x01{2}\x01{3}\x01{4}\x01{5}\x01{6}\x01{7}\x01{8}",
                        change.type, change.id, change.rdfid, change.oldparent, change.oldparent_id,
                        change.parent, change.parent_id, change.name, change.url );
                    _root.GetStringListProp( FavoritesPlugin._propChangesLog ).Add( historyStr.ToString() );
                }
                finally
                {
                    StringBuilderPool.Dispose( historyStr );
                }
            }
        }

        private void SetUrlCookie( string domain, string path, string cookie )
        {
            if( domain.IndexOf( "://" ) < 0 )
            {
                domain = "http://" + domain;
            }
            Uri uri;
            try
            {
                uri = new Uri( new Uri( domain ), path );
            }
            catch
            {
                return;
            }
            HashMap.Entry entry = _cookies.GetEntry( uri.AbsoluteUri );
            if( entry == null )
            {
                _cookies.Add( uri.AbsoluteUri, cookie );
            }
            else
            {
                string oldCookie = (string) entry.Value;
                entry.Value = oldCookie + "; " + cookie;
            }
        }

        private IBookmarkService    _bookmarkservice;
        private string              _profileName;
        private IResource           _root;
        private bool                _isActive;
        private bool                _importAllowed;
        private DateTime            _lastCookiesFileTime;
        private HashMap             _cookies;
        private static string       _notActiveErrorString =
            "Run corresponding browser with Omea plugin.";
        private static string       _importDisabledErrorString =
            "Import of bookmarks of current profile is disabled.";

        #endregion
    }
}