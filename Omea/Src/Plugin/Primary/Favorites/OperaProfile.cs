// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using Ini;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
    internal class OperaBookmarkProfile: IBookmarkProfile
    {
        public OperaBookmarkProfile( IBookmarkService bookmarkService )
        {
            _bookmarkservice = bookmarkService;
        }

        /// <summary>
        /// Returns path to the existing file with opera bookmarks, otherwise returns empty string.
        /// </summary>
        public static string OperaBookmarksPath()
        {
            if( _bookmarksPath != null )
            {
                return _bookmarksPath;
            }
            _bookmarksPath = string.Empty;
            string operaPath = IOTools.Combine(
                Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Opera" );
            if( operaPath.Length > 0 && Directory.Exists( operaPath ) )
            {
                DirectoryInfo[] directories = IOTools.GetDirectories( operaPath, "Opera*" );
                if( directories != null && directories.Length > 0 )
                {
                    for( int i = directories.Length - 1; i >= 0; --i )
                    {
                        operaPath = IOTools.Combine( directories[ i ].FullName, "Profile" );
                        if( operaPath.Length == 0 || operaPath == "Profile" || !Directory.Exists( operaPath ) )
                        {
                            continue;
                        }
                        operaPath = IOTools.Combine( operaPath, _settingsFileName );
                        if( !File.Exists( operaPath ) )
                        {
                            continue;
                        }
                        IniFile opera6ini = new IniFile( operaPath );
                        operaPath = opera6ini.ReadString( "User Prefs", "Hot List File Ver2", string.Empty ).TrimEnd( '\\' );
                        if( operaPath.Length > 0 && File.Exists( operaPath ) )
                        {
                            _bookmarksPath = operaPath;
                            break;
                        }
                    }
                }
            }
            return _bookmarksPath;
        }

        public static bool ImportAllowed
        {
            get
            {
                return Core.SettingStore.ReadBool( "Favorites", "ImportFromOpera", true );
            }
            set
            {
                Core.SettingStore.WriteBool( "Favorites", "ImportFromOpera", value );
            }
        }

        public static bool ImportImmediately
        {
            get
            {
                return Core.SettingStore.ReadBool( "Favorites", "ImportFromOperaImmediately", true );
            }
            set
            {
                Core.SettingStore.WriteBool( "Favorites", "ImportFromOperaImmediately", value );
            }
        }

        public string Name { get { return "Opera"; } }

        public void StartImport()
        {
            if( _watcher != null )
            {
                _watcher.Changed -= new FileSystemEventHandler( AsyncUpdateBookmarks );
                _watcher.Dispose();
                _watcher = null;
            }

            _root = _bookmarkservice.GetProfileRoot( this );
            if( !_root.IsDeleted )
            {
                if( !ImportAllowed )
                {
                    _root.SetProp( FavoritesPlugin._propInvisible, true );
                }
                else
                {
                    _root.DeleteProp( FavoritesPlugin._propInvisible );
                    IntHashSet nodes = new IntHashSet();
                    CollectAllSubNodes( _root, nodes );
                    string path = OperaBookmarksPath();
                    if( path.Length > 0 )
                    {
                        using( StreamReader reader = new StreamReader( path ) )
                        {
                            IResource parent = _root;
                            Stack parents = new Stack();
                            bool readingFolder = false;
                            string id = string.Empty;
                            string name = string.Empty;
                            string url = string.Empty;
                            string line;
                            while( ( line = reader.ReadLine() ) != null )
                            {
                                line = line.Trim( ' ', '\r', '\n', '\t' );
                                if( Utils.StartsWith( line, "id=", true ) )
                                {
                                    id = line.Substring( 3 );
                                    continue;
                                }
                                if( Utils.StartsWith( line, "name=", true ) )
                                {
                                    name = line.Substring( 5 );
                                    continue;
                                }
                                if( Utils.StartsWith( line, "url=", true ) )
                                {
                                    url = line.Substring( 4 );
                                    IResource bookmark = _bookmarkservice.FindOrCreateBookmark( parent, name, url );
                                    bookmark.SetProp( FavoritesPlugin._propBookmarkId, id );
                                    nodes.Remove( bookmark.Id );
                                }
                                if( Utils.StartsWith( line, "#folder", true ) )
                                {
                                    if( readingFolder )
                                    {
                                        parent = _bookmarkservice.FindOrCreateFolder( parent, name );
                                        parent.SetProp( FavoritesPlugin._propBookmarkId, id );
                                        nodes.Remove( parent.Id );
                                    }
                                    parents.Push( parent );
                                    readingFolder = true;
                                    continue;
                                }
                                if( Utils.StartsWith( line, "#url", true ) )
                                {
                                    if( readingFolder )
                                    {
                                        parent = _bookmarkservice.FindOrCreateFolder( parent, name );
                                        parent.SetProp( FavoritesPlugin._propBookmarkId, id );
                                        nodes.Remove( parent.Id );
                                    }
                                    readingFolder = false;
                                    continue;
                                }
                                if( line == "-" )
                                {
                                    if( readingFolder )
                                    {
                                        parent = _bookmarkservice.FindOrCreateFolder( parent, name );
                                        parent.SetProp( FavoritesPlugin._propBookmarkId, id );
                                        nodes.Remove( parent.Id );
                                    }
                                    readingFolder = false;
                                    if( parents.Count == 0 )
                                    {
                                        break;
                                    }
                                    parent = (IResource) parents.Pop();
                                }
                            }
                        }
                        foreach( IntHashSet.Entry e in nodes )
                        {
                            Core.ResourceStore.LoadResource( e.Key ).Delete();
                        }
                        if( ImportImmediately )
                        {
                            _watcher = new FileSystemWatcher();
                            try
                            {
                                _watcher.Path = IOTools.GetDirectoryName( IOTools.GetFileInfo( path ) );
                                _watcher.Filter = "*.*";
                                _watcher.IncludeSubdirectories = false;
                                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                                _watcher.Changed += new FileSystemEventHandler( AsyncUpdateBookmarks );
                                _watcher.EnableRaisingEvents = true;
                            }
                            catch
                            {
                                _watcher = null;
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
        }

        public char[] InvalidNameChars { get { return Path.InvalidPathChars; } }


        public bool CanCreate( IResource res, out string error )
        {
            error = "Cannot modify. " + _errorMessage;
            return false;
        }

        public bool CanRename( IResource res, out string error )
        {
            error = "Cannot rename. " + _errorMessage;
            return false;
        }

        public bool CanMove( IResource res, IResource parent, out string error )
        {
            error = _errorMessage;
            return false;
        }

        public bool CanDelete( IResource res, out string error )
        {
            error = _errorMessage;
            return false;
        }

        public void Create( IResource res )
        {
        }

        public void Rename( IResource res, string newName )
        {
        }

        public void Move( IResource res, IResource parent, IResource oldParent )
        {
        }

        public void Delete( IResource res )
        {
        }

        private void CollectAllSubNodes( IResource root, IntHashSet nodes )
        {
            if( root != _root )
            {
                nodes.Add( root.Id );
            }
            foreach( IResource child in BookmarkService.SubNodes( null, root ) )
            {
                CollectAllSubNodes( child, nodes );
            }
        }

        private void AsyncUpdateBookmarks( object sender, FileSystemEventArgs e )
        {
            if( String.Compare( e.FullPath.TrimEnd( '\\' ), _bookmarksPath, true ) == 0 )
            {
                AsyncUpdateBookmarks();
            }
        }

        internal void AsyncUpdateBookmarks()
        {
            Core.ResourceAP.QueueJobAt( DateTime.Now.AddSeconds( 1 ), new MethodInvoker( StartImport ) );
        }

        private const string        _settingsFileName = "opera6.ini";
        private const string        _errorMessage = "Export changes to Opera is not implemented.";
        private IBookmarkService    _bookmarkservice;
        private IResource           _root;
        private FileSystemWatcher   _watcher;
        private static string       _bookmarksPath = null;
    }
}
