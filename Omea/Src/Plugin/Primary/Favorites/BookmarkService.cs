// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
    /// <summary>
    /// Fake bookmarks profile which restricts everything and is registered for parent folders
    /// of folders of profiles, e.x., the "Mozilla" folder for the path "Mozilla->Firefox - default"
    /// </summary>
    internal class FakeRestrictProfile: IBookmarkProfile
    {
        #region IBookmarkProfile Members

        public bool CanRename( IResource res, out string error  )
        {
            error = _error;
            return false;
        }

        public char[] InvalidNameChars
        {
            get { return new char[] {}; }
        }

        public bool CanMove( IResource res, IResource parent, out string error  )
        {
            error = _error;
            return false;
        }

        public bool CanCreate( IResource res, out string error  )
        {
            error = _error;
            return false;
        }

        public bool CanDelete( IResource res, out string error  )
        {
            error = _error;
            return false;
        }

        public void Create( IResource res )
        {
        }

        public string Name
        {
            get
            {
                if( _name == null )
                {
                    _name = _random.NextDouble().ToString();
                }
                return _name;
            }
        }

        public void Rename( IResource res, string newName )
        {
        }

        public void Delete( IResource res )
        {
        }

        public void StartImport()
        {
        }

        public void Move( IResource res, IResource parent, IResource oldParent )
        {
        }

        public void Dispose()
        {
        }

        #endregion

        private string       _name;
        private const string _error = "No changes can be made here.";
        private static readonly Random _random = new Random();
    }

    /// <summary>
    /// Implemetation of IBookmarkService
    /// </summary>
    internal class BookmarkService: IBookmarkService
    {
        public BookmarkService()
        {
            _theService = this;
            _rootFolder = Core.ResourceTreeManager.GetRootForType( "Weblink" );
            Core.ResourceTreeManager.SetResourceNodeSort( _rootFolder, "Type- Name" );
            _rootFolder.DisplayName = "Bookmarks";
            _profileNames2RootResources = new HashMap();
            _rootResources2Profiles = new HashMap();
            _propName = Core.Props.Name;
        }

        #region IBookmarkService implementation

        public void RegisterProfile( IBookmarkProfile profile )
        {
            Guard.NullArgument( profile, "profile" );
            string name = NormalizeProfileName( profile.Name );
            if( _profileNames2RootResources.Contains( name ) )
            {
                throw new InvalidOperationException( "Bookmark profile '" + profile.Name + "' is already registered" );
            }
            string[] nameParts = profile.Name.Split( '\\', '/' );
            IResource parent = _rootFolder;
            IResource interimParent = null;
            foreach( string namePart in nameParts )
            {
                if( namePart.Length == 0 )
                {
                    throw new Exception( "Invalid profile name: " + profile.Name );
                }
                parent = FindOrCreateFolder( parent, namePart );
                /**
                 * Register fake all-restricting profile for interim folders which contain
                 * profiles' folders, e.x., for Mozilla->profile1, Mozilla->profile2... etc...
                 */
                if( interimParent != null && GetOwnerProfile( interimParent ) == null )
                {
                    FakeRestrictProfile fakeProfile = new FakeRestrictProfile();
                    _profileNames2RootResources[ NormalizeProfileName( fakeProfile.Name ) ] = interimParent;
                    _rootResources2Profiles[ interimParent ] = fakeProfile;
                }
                interimParent = parent;
            }
            if( parent == _rootFolder )
            {
                throw new Exception( "Invalid profile name: " + profile.Name );
            }
            _profileNames2RootResources[ name ] = parent;
            _rootResources2Profiles[ parent ] = profile;
            if( _rootResources2Profiles.Count != _profileNames2RootResources.Count )
            {
                throw new Exception( "Root resources and profiles count mismatch. Last added profile name: " + profile.Name );
            }
            Trace.WriteLine( "Registered bookmark profile: " + profile.Name, "Favorites.Plugin" );
        }

        public void DeRegisterProfile( IBookmarkProfile profile )
        {
            Guard.NullArgument( profile, "profile" );
            using( profile )
            {
                string name = NormalizeProfileName( profile.Name );
                HashMap.Entry e = _profileNames2RootResources.GetEntry( name );
                if( e == null )
                {
                    throw new InvalidOperationException( "Bookmark profile '" + profile.Name + "' was not registered" );
                }
                IResource profileRoot = (IResource) e.Value;
                _profileNames2RootResources.Remove( name );
                _rootResources2Profiles.Remove( profileRoot );
                if( _rootResources2Profiles.Count != _profileNames2RootResources.Count )
                {
                    throw new Exception( "Root resources and profiles count mismatch. Last removed profile name: " + profile.Name );
                }
                if( profileRoot.HasProp( FavoritesPlugin._propInvisible ) )
                {
                    DeleteFolder( profileRoot );
                }
            }
        }

        public IBookmarkProfile[] Profiles
        {
            get
            {
                IBookmarkProfile[] profiles = new IBookmarkProfile[ _rootResources2Profiles.Count ];
                int i = 0;
                foreach( HashMap.Entry e in _rootResources2Profiles )
                {
                    profiles[ i++ ] = (IBookmarkProfile) e.Value;
                }
                return profiles;
            }
        }

        public IBookmarkProfile GetOwnerProfile( IResource res )
        {
            Guard.NullArgument( res, "res" );
            IBookmarkProfile result = null;
            while( res != null && ( result = (IBookmarkProfile) _rootResources2Profiles[ res ] ) == null )
            {
                res = GetParent( res );
            }
            return result;
        }

        public IResource BookmarksRoot
        {
            get { return _rootFolder; }
        }

        public IResource GetProfileRoot( string profileName )
        {
            return (IResource) _profileNames2RootResources[ NormalizeProfileName( profileName ) ];
        }

        public IResource GetProfileRoot( IBookmarkProfile profile )
        {
            return GetProfileRoot( profile.Name );
        }

        public IResourceList GetBookmarks()
        {
            return GetBookmarks( _rootFolder );
        }

        public IResourceList GetBookmarks( IBookmarkProfile profile )
        {
            return GetBookmarks( GetProfileRoot( profile ) );
        }

        public IResource FindOrCreateBookmark( IResource parent, string name, string url )
        {
            return FindOrCreateBookmark( parent, name, url, false );
        }

        public void SetName( IResource res, string name )
        {
            SetNameImpl( res, name );
        }

        public void SetUrl( IResource res, string url )
        {
            SetUrlImpl( res, url );
        }

        public void SetParent( IResource res, IResource parent )
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.RunUniqueJob( new ResourceResourceDelegate( SetParent ), res, parent );
            }
            else
            {
                res.SetProp( FavoritesPlugin._propParent, parent );
            }
        }

        public IResource FindOrCreateFolder( IResource parent, string name )
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                return (IResource) Core.ResourceAP.RunUniqueJob(
                    new ResourceStringDelegate2( FindOrCreateFolder ), parent, name );
            }
            IResource result;
            IResourceList candidates = SubNodesWithName( "Folder", parent, name );
            if( candidates.Count > 0 )
            {
                result = candidates[ 0 ];
                result.BeginUpdate();
            }
            else
            {
                result = Core.ResourceStore.BeginNewResource( "Folder" );
                Core.WorkspaceManager.AddToActiveWorkspace( result );
            }
            try
            {
                result.SetProp( _propName, name );
                result.DeleteProp( "_DisplayName" );
                result.SetProp( FavoritesPlugin._propParent, parent );
            }
            finally
            {
                result.EndUpdate();
            }
            return result;
        }

        public void DeleteBookmark( IResource res )
        {
            if( res.Type != "Weblink" )
            {
                throw new ArgumentException( "DeleteBookmark() is applicable to the \"Weblink\" resources only" );
            }
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.QueueJob( new ResourceDelegate( DeleteBookmark ), res );
            }
            else
            {
                IResource source = res.GetLinkProp( "Source" );
                if( source != null )
                {
                    source.Delete();
                }
                res.Delete();
            }
        }

        public void DeleteBookmarks( IResourceList resources )
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.QueueJob( new ResourceListDelegate( DeleteBookmarks ), resources );
            }
            else
            {
                foreach( IResource res in resources )
                {
                    DeleteBookmark( res );
                }
            }
        }

        public void DeleteFolder( IResource res )
        {
            if( res.Type != "Folder" )
            {
                throw new ArgumentException( "DeleteFolder() is applicable to the \"Folder\" resources only" );
            }
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.QueueJob( new ResourceDelegate( DeleteFolder ), res );
            }
            else
            {
                IResourceList subnodes = SubNodes( null, res );
                foreach( IResource subnode in subnodes )
                {
                    if( subnode.Type == "Folder" )
                    {
                        DeleteFolder( subnode );
                    }
                    else
                    {
                        DeleteBookmark( subnode );
                    }
                }
                res.Delete();
            }
        }

        public void DeleteFolders( IResourceList resources )
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.QueueJob( new ResourceListDelegate( DeleteFolders ), resources );
            }
            else
            {
                foreach( IResource res in resources )
                {
                    DeleteFolder( res );
                }
            }
        }

        #endregion

        /// <summary>
        /// 0 - download when system is idle
        /// 1 - download when imported
        /// 2 - downlaod on preview
        /// </summary>
        public static int DownloadMethod
        {
            get
            {
                return Core.SettingStore.ReadInt( "Favorites", "DownloadMethod", 0 );
            }
            set
            {
                if( value < 0 || value > 2 )
                {
                    throw new ArgumentOutOfRangeException( "value", "Can only be in [0, 2]" );
                }
                Core.SettingStore.WriteInt( "Favorites", "DownloadMethod", value );
            }
        }

        public void SynchronizeBookmarks()
        {
            IResourceList weblinks = GetBookmarks();
            List<int> postponedIds = new List<int>();
            foreach( IResource weblink in weblinks.ValidResources )
            {
                string URL = weblink.GetPropText( FavoritesPlugin._propURL );
                if( URL.Length > 0 )
                {
                    if( weblink.HasProp( "Source" ) || weblink.HasProp( Core.Props.LastError ) )
                    {
                        QueueWeblink( weblink, URL, BookmarkSynchronizationTime( weblink ) );
                    }
                    else
                    {
                        postponedIds.Add( weblink.Id );
                    }
                }
            }
            if( postponedIds.Count > 0 )
            {
                IResourceList postponedWeblinks = Core.ResourceStore.ListFromIds( postponedIds.ToArray(), false );
                int downloadMethod = DownloadMethod;
                if( downloadMethod == 0 )
                {
                    Core.NetworkAP.QueueIdleJob( new FavoritesUpdateQueue( postponedWeblinks ) );
                }
                else if( downloadMethod == 1 )
                {
                    Core.NetworkAP.QueueJob( new FavoritesUpdateQueue( postponedWeblinks ) );
                }
            }
        }

        public static IResourceList SubNodes( string type, IResource res )
        {
            return res.GetLinksTo( type, FavoritesPlugin._propParent );
        }

        public static IResourceList SubNodesWithName( string type, IResource res, string name )
        {
            return SubNodes( type, res ).Intersect(
                Core.ResourceStore.FindResources( null, _propName, name ), true );
        }

        public static IResourceList GetBookmarks( IResource parent )
        {
            IResourceList result = SubNodes( "Weblink", parent );
            IResourceList folders = SubNodes( "Folder", parent );
            foreach( IResource folder in folders )
            {
                result = result.Union( GetBookmarks( folder ), true );
            }
            return result;
        }

        public static bool HasSubNodeWithName( IResource node, string name )
        {
            name = name.ToLower();
            IResourceList childs = SubNodes( node.Type, node );
            foreach( IResource child in childs )
            {
                if( name == child.GetPropText( Core.Props.Name ).ToLower() )
                {
                    return true;
                }
            }
            return false;
        }

        public static IResource GetBookmarksRoot()
        {
            return _theService.BookmarksRoot;
        }

        public static IResource GetParent( IResource res )
        {
            if( res != GetBookmarksRoot() )
            {
                IResourceList parents = res.GetLinksFrom( null, FavoritesPlugin._propParent );
                if( parents.Count > 0 )
                {
                    return parents[ 0 ];
                }
            }
            return null;
        }

        #region implementation details

        private delegate IResource FindOrCreateBookmarkDelegate( IResource parent, string name, string url, bool transient );
        private delegate void ResourceStringDelegate( IResource res, string str );
        private delegate IResource ResourceStringDelegate2( IResource res, string str );
        private delegate void ResourceResourceDelegate( IResource res1, IResource res2 );

        internal static IResource FindOrCreateBookmark( IResource parent, string name, string url, bool transient )
        {
            url = url.Trim();
            if( url.IndexOf( "://" ) < 0 )
            {
                url = "http://" + url;
            }

            IResourceStore store = Core.ResourceStore;
            IResourceList candidates = SubNodesWithName( "Weblink", parent, name ).Intersect(
                store.FindResources( null, FavoritesPlugin._propURL, url ), true );
            if( candidates.Count > 0 )
            {
                return candidates[ 0 ];
            }
            if( !store.IsOwnerThread() )
            {
                return (IResource) Core.ResourceAP.RunUniqueJob(
                    new FindOrCreateBookmarkDelegate( FindOrCreateBookmark ), parent, name, url, transient );
            }
            IResource result = ( transient ) ?
                store.NewResourceTransient( "Weblink" ) : store.BeginNewResource( "Weblink" );
            try
            {
                result.SetProp( _propName, name );
                result.SetProp( FavoritesPlugin._propURL, url );
                result.AddLink( FavoritesPlugin._propParent, parent );
            }
            finally
            {
                if( !transient )
                {
                    result.EndUpdate();
                }
            }
            return result;
        }

        private static void SetNameImpl( IResource res, string name )
        {
            if( !res.IsDeleted )
            {
                if( !Core.ResourceStore.IsOwnerThread() )
                {
                    Core.ResourceAP.RunUniqueJob( new ResourceStringDelegate( SetNameImpl ), res, name );
                }
                else
                {
                    res.DeleteProp( "_DisplayName" );
                    res.SetProp( Core.Props.Name, name );
                }
            }
        }

        private static IResource SetUrlImpl( IResource res, string url )
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                return (IResource) Core.ResourceAP.RunUniqueJob(
                    new ResourceStringDelegate2( SetUrlImpl ), res, url );
            }
            res.SetProp( FavoritesPlugin._propURL, url );
            return res;
        }

        internal static string NormalizeProfileName( string profileName )
        {
            return profileName.ToLower().Trim( '\\', '/' );
        }

        internal static int BookmarkSynchronizationFrequency( IResource webLink )
        {
            int freq = 0;
            while( webLink != null && ( freq = webLink.GetIntProp( FavoritesPlugin._propUpdateFreq ) ) <= 0 )
            {
                webLink = GetParent( webLink );
            }
            return freq;
        }

        internal static DateTime BookmarkSynchronizationTime( IResource webLink )
        {
            int freq = BookmarkSynchronizationFrequency( webLink );
            if( freq <= 0 )
            {
                return DateTime.MaxValue;
            }
            DateTime lastUpdated = webLink.GetDateProp( FavoritesPlugin._propLastUpdated );
            if( lastUpdated == DateTime.MinValue )
            {
                lastUpdated = DateTime.Now;
            }
            return lastUpdated.AddSeconds( freq );
        }

        internal static void QueueWeblink( IResource webLink, string URL, DateTime when )
        {
            if( when < DateTime.MaxValue )
            {
                FavoritesTools.TraceIfAllowed( "Queueing " + URL + " to be processed at " + when );
                Core.NetworkAP.QueueJobAt( when, new FavoriteJob( webLink, URL ) );
            }
        }

        internal static void ImmediateQueueWeblink( IResource webLink, string URL )
        {
            FavoritesTools.TraceIfAllowed( "Queueing " + URL );
            Core.NetworkAP.QueueJob( JobPriority.Immediate, new FavoriteJob( webLink, URL ) );
        }

        #endregion

        private static BookmarkService  _theService;
        private readonly IResource      _rootFolder;
        private readonly HashMap        _profileNames2RootResources;
        private readonly HashMap        _rootResources2Profiles;
        private static int              _propName;
    }
}
