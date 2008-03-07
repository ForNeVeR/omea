/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
    internal class IEFavoritesBookmarkProfile: IBookmarkProfile
    {
        public IEFavoritesBookmarkProfile( IBookmarkService bookmarkService )
        {
            _bookmarkservice = bookmarkService;
            _IEFavoritesPath = Environment.GetFolderPath( Environment.SpecialFolder.Favorites );
        }

        #region IBookmarkProfile implementation

        public string Name
        {
            get { return "IE Favorites"; }
        }

        public void StartImport()
        {
            IResource root = _bookmarkservice.GetProfileRoot( this );
            if( !root.IsDeleted )
            {
                if( !ImportAllowed )
                {
                    root.SetProp( FavoritesPlugin._propInvisible, true );
                }
                else
                {
                    if( _IEFavoritesPath.Length > 0 )
                    {
                        DisposeFavoritesWatcher();
                        try
                        {
                            root.DeleteProp( FavoritesPlugin._propInvisible );
                            EnumerateFavorites( _IEFavoritesPath, root );
                        }
                        finally
                        {
                            CreateFavoritesWatcher();
                        }
                    }
                }
            }
        }

        public char[] InvalidNameChars
        {
            get { return Path.InvalidPathChars; }
        }

        public bool CanCreate( IResource res, out string error )
        {
            return CheckExport( out error );
        }

        public bool CanRename( IResource res, out string error )
        {
            bool result = CheckExport( out error );
            if( result )
            {
                if( res == _bookmarkservice.GetProfileRoot( this ) )
                {
                    error = "Can't rename Favorites folder";
                    result = false;
                }
            }
            return result;
        }

        public bool CanMove( IResource res, IResource parent, out string error )
        {
            return CheckExport( out error );
        }

        public bool CanDelete( IResource res, out string error )
        {
            return CheckExport( out error );
        }

        public void Create( IResource res )
        {
            SynchronizeIEResource( res );
        }

        public void Rename( IResource res, string newName )
        {
            string fullname = GetResourceFullname( res );
            DirectoryInfo di = IOTools.GetParent( fullname );
            string newFullname = IOTools.Combine( IOTools.GetFullName( di ), newName );
            if( res.Type == "Folder" )
            {
                IOTools.MoveDirectory( fullname, newFullname );
            }
            else
            {
                IOTools.MoveFile( fullname, newFullname + ".url" );
            }
        }

        public void Move( IResource res, IResource parent, IResource oldParent )
        {
            string newFolder = GetResourceFullname( parent );
            string oldFolder = GetResourceFullname( oldParent );
            string name = res.GetPropText( Core.Props.Name );
            if( res.Type != "Folder" )
            {
                name += ".url";
            }
            if( newFolder != oldFolder && name.Length > 0 )
            {
                string oldPath = IOTools.Combine( oldFolder, name );
                string newPath = IOTools.Combine( newFolder, name );
                if( res.Type == "Folder" )
                {
                    IOTools.MoveDirectory( oldPath, newPath );
                }
                else
                {
                    IOTools.MoveFile( oldPath, newPath );
                }
            }
        }

        public void Delete( IResource res )
        {
            string fullname = GetResourceFullname( res );
            if( res.Type == "Folder" )
            {
                IOTools.DeleteDirectory( fullname, true );
            }
            else
            {
                IOTools.DeleteFile( fullname );
            }
        }

        public void Dispose()
        {
            DisposeFavoritesWatcher();
        }

        #endregion

        public static bool ImportAllowed
        {
            get
            {
                return Core.SettingStore.ReadBool( "Favorites", "ImportFromIE", true );
            }
            set
            {
                Core.SettingStore.WriteBool( "Favorites", "ImportFromIE", value );
            }
        }

        public static bool ExportToIEAllowed
        {
            get
            {
                return Core.SettingStore.ReadBool( "Favorites", "ExportToIE", false );
            }
            set
            {
                Core.SettingStore.WriteBool( "Favorites", "ExportToIE", value );
            }
        }

        #region implementation details

        private static bool CheckExport( out string error )
        {
            if( !ExportToIEAllowed )
            {
                error = "Export to IE Favorites is disabled, see Tools | Options | Favorites.";
                return false;
            }
            error = null;
            return true;
        }

        private void EnumerateFavorites( string folder, IResource parent )
        {
            FileInfo[] files = IOTools.GetFiles( folder );
            if( files != null )
            {
                IResourceList weblinks = BookmarkService.SubNodes( "Weblink", parent );
                IntArrayList processedWeblinks = new IntArrayList( files.Length );
                foreach( FileInfo fileInfo in files )
                {
                    IResource weblink = null;
                    try
                    {
                        if( fileInfo.Extension.ToLower() == ".url" )
                        {
                            weblink = ProcessFavoriteFile( fileInfo, parent );
                        }
                        else if ( fileInfo.Extension.ToLower() == ".lnk" ) 
                        {
                            weblink = ProcessShortcut( fileInfo, parent );
                        }
                    }
                    catch( Exception e )
                    {
                        FavoritesTools.TraceIfAllowed( e.Message );
                        continue;
                    }
                    if( weblink != null )
                    {
                        processedWeblinks.Add( weblink.Id );
                    }
                }
                _bookmarkservice.DeleteBookmarks( weblinks.Minus(
                    Core.ResourceStore.ListFromIds( processedWeblinks, false ) ) );
            }

            DirectoryInfo[] dirs = IOTools.GetDirectories( folder );
            if( dirs != null )
            {
                IResourceList folders = BookmarkService.SubNodes( "Folder", parent );
                IntArrayList processedFolders = new IntArrayList( dirs.Length );
                foreach( DirectoryInfo dirInfo in dirs )
                {
                    IResource subfolder = _bookmarkservice.FindOrCreateFolder( parent, dirInfo.Name );
                    EnumerateFavorites( IOTools.GetFullName( dirInfo ), subfolder );
                    processedFolders.Add( subfolder.Id );
                }
                _bookmarkservice.DeleteFolders( folders.Minus(
                    Core.ResourceStore.ListFromIds( processedFolders, false ) ) );
            }
        }

        /** 
         * parse favorite file (.url), return corresponding weblink resource
         */
        private IResource ProcessFavoriteFile( FileInfo fileInfo, IResource parent )
        {
            FileStream stream = IOTools.OpenRead( fileInfo );
            if( stream != null )
            {
                using( StreamReader reader = new StreamReader( stream, Encoding.Default ) )
                {
                    string line;
                    while( ( line = reader.ReadLine() ) != null )
                    {
                        string trimmedLine = line.Trim();
                        if( trimmedLine.ToLower().StartsWith( "url=" ) )
                        {
                            string url = trimmedLine.Substring( 4, line.Length - 4 ).Trim();
                            FavoritesTools.TraceIfAllowed(
                                "Creating or updating '" + fileInfo.FullName + "', " + url );
                            return CreateFavoriteResource( url, fileInfo, parent );
                        }
                    }
                }
            }
            return null;
        }

        /** 
         * resolve shortcut file (.lnk), return corresponding weblink resource
         */
        private IResource ProcessShortcut( FileInfo fileInfo, IResource parent )
        {
            Shell32.Shell shell = new Shell32.ShellClass();
            if( shell != null )
            {
                Shell32.Folder folder = shell.NameSpace( fileInfo.DirectoryName );
                if( folder != null )
                {
                    Shell32.FolderItem item = folder.ParseName( fileInfo.Name );
                    if( item != null && item.IsLink )
                    {
                        Shell32.IShellLinkDual2 link = ( Shell32.IShellLinkDual2 ) item.GetLink;
                        if( link != null && File.Exists( link.Path ) )
                        {
                            return CreateFavoriteResource( "file:///" + link.Path, fileInfo, parent );
                        }
                    }
                }
            }
            return null;
        }

        /**
         * Create favorite resource for URL and fileinfo of file describing favorite
         */
        private IResource CreateFavoriteResource( string url, FileInfo fileInfo, IResource parent  )
        {
            string favoriteName = fileInfo.Name.Substring( 0, fileInfo.Name.Length - 4 );
            return _bookmarkservice.FindOrCreateBookmark( parent, favoriteName, url );
        }

        internal void CreateFavoritesWatcher()
        {
            if( _IEFavoritesWatcher == null && _IEFavoritesPath.Length > 0 && ImportAllowed )
            {
                _IEFavoritesWatcher = new FileSystemWatcher();
                try
                {
                    _IEFavoritesWatcher.Path = _IEFavoritesPath;
                }
                catch
                {
                    _IEFavoritesWatcher = null;
                    return;
                }
                _IEFavoritesWatcher.IncludeSubdirectories = true;
                _IEFavoritesWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                _IEFavoritesWatcher.Created += new FileSystemEventHandler( FavoritesChanged );
                _IEFavoritesWatcher.Changed += new FileSystemEventHandler( FavoritesChanged );
                _IEFavoritesWatcher.Deleted += new FileSystemEventHandler( FavoritesChanged );
                try
                {
                    _IEFavoritesWatcher.EnableRaisingEvents = true;
                }
                catch( PlatformNotSupportedException ) {}
            }
        }

        internal void DisposeFavoritesWatcher()
        {
            if( _IEFavoritesWatcher != null )
            {
                _IEFavoritesWatcher.EnableRaisingEvents = false;
                _IEFavoritesWatcher.Dispose();
                _IEFavoritesWatcher = null;
            }
        }

        private void FavoritesChanged( object sender, FileSystemEventArgs e )
        {
            Core.ResourceAP.QueueJobAt( DateTime.Now.AddSeconds( 0.5 ), new MethodInvoker( StartImport ) );
        }

        /**
         * Gets full name of IE folder or favorite
         */
        public string GetResourceFullname( IResource res )
        {
            Guard.NullArgument( res, "res" );
            IResource parent = res;
            IResource ieRoot = _bookmarkservice.GetProfileRoot( this );
            if( ieRoot != null )
            {
                Stack parents = new Stack();
                for( ; ; )
                {                    
                    if( parent == ieRoot )
                    {
                        string fullName = Environment.GetFolderPath( Environment.SpecialFolder.Favorites );
                        while( parents.Count > 0 )
                        {
                            res = (IResource) parents.Pop();
                            fullName = IOTools.Combine( fullName, res.DisplayName );
                        }
                        if( fullName.IndexOfAny( InvalidNameChars ) >= 0 )
                        {
                            foreach( char invalidChar in Path.InvalidPathChars )
                            {
                                fullName = fullName.Replace( invalidChar, '-' );
                            }
                        }
                        fullName = fullName.Replace( "https://", null );
                        fullName = fullName.Replace( "http://", null );
                        fullName = fullName.Replace( "ftp://", null );
                        fullName = fullName.Replace( "file://", null );
                        fullName = fullName.Replace( "://", null );
                        if( res.Type != "Folder" )
                        {
                            fullName += ".url";
                        }
                        return fullName;
                    }
                    if( ( parent = BookmarkService.GetParent( parent ) ) == null )
                    {
                        break;
                    }
                    parents.Push( res );
                    res = parent;
                }
            }
            return string.Empty;
        }

        /**
         * synchronizes a folder (recursively) or a favorite with IE Favorites
         *  (before, checks whether the folder is an IE folder)
         */
        private void SynchronizeIEResource( IResource res )
        {
            if( ExportToIEAllowed &&
                _bookmarkservice.GetOwnerProfile( res ).GetType() == typeof( IEFavoritesBookmarkProfile ) )
            {
                string path = GetResourceFullname( res );
                if( res.Type == "Folder" )
                {
                    IOTools.CreateDirectory( path );
                    IResourceList childs = res.GetLinksTo( null, FavoritesPlugin._propParent );
                    foreach( IResource child in childs )
                    {
                        SynchronizeIEResource( child );
                    }
                }
                else
                {
                    FileStream stream = File.Exists( path ) ? IOTools.Open( path ) : IOTools.CreateFile( path );
                    if( stream != null )
                    {
                        string URL = res.GetPropText( FavoritesPlugin._propURL );
                        StreamWriter writer = new StreamWriter( stream );
                        using( writer )
                        {
                            writer.WriteLine( "[DEFAULT]\r\nBASEURL={0}\r\n[InternetShortcut]\r\nURL={1}", URL, URL );
                        }
                    }
                }
            }
        }

        private IBookmarkService        _bookmarkservice;
        private string                  _IEFavoritesPath;
        private FileSystemWatcher       _IEFavoritesWatcher;

        #endregion
    }
}