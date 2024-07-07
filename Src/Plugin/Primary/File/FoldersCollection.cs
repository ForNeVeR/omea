// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.Base;
using JetBrains.DataStructures;

namespace JetBrains.Omea.FilePlugin
{
    internal class FoldersCollection: ReenteringEnumeratorJob, IDisposable
    {
        private const string _cUpdatingFolderJob = "Creating or updating directory";
        private const string _cUpdatingFileJob = "Creating or updating file";

        private FoldersCollection()
        {
            _settings = Core.SettingStore;
            _resourceAP = Core.ResourceAP;
            _ftm = Core.FileResourceManager as FileResourceManager;
            _monitoredFolders = new HashMap();
            _excludedFolders = new HashMap();
            _deferredFolders = new HashMap();
            _allFolders = new HashMap();
            _folderWatchers = new HashMap();
            _indexHidden = _settings.ReadBool( "FilePlugin", "IndexHidden", false );
            _excludeDelegate = ExcludeFiles;
            _enumerateDelegate = EnumerateFiles;
            _deleteResourceDelegate = DeleteResource;
            _findOrCreateDirectoryDelegate = FindOrCreateDirectory;
            _findOrCreateFileDelegate = FindOrCreateFile;
            _args = new object[ 1 ];
            _enumSignal = new ManualResetEvent( true );
        }

        public static FoldersCollection Instance
        {
            get
            {
                if( _instance == null )
                {
                    _instance = new FoldersCollection();
                }
                return _instance;
            }
        }

        public void Dispose()
        {
            foreach( HashMap.Entry E in _folderWatchers )
            {
                FileSystemWatcher watcher = (FileSystemWatcher) E.Value;
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _folderWatchers.Clear();
            _monitoredFolders.Clear();
            _deferredFolders.Clear();
            _excludedFolders.Clear();
        }

        /**
         * root resource for files
         */
        public IResource FilesRoot
        {
            get
            {
                if( _filesRoot == null || _filesRoot.IsDeleted )
                {
                    _filesRoot = Core.ResourceTreeManager.GetRootForType( FileProxy._folderResourceType );
                }
                return _filesRoot;
            }
        }

        /**
         * finds resource for a directory
         */
        public IResource FindDirectory( string path )
        {
            if( path.Length == 0 )
            {
                return null;
            }

            return Core.ResourceStore.FindUniqueResource(
                FileProxy._folderResourceType, FileProxy._propDirectory, path );
        }

        /**
         * finds or creates resource for a directory, or null if directory could not be created
         * NOTE: should not be used for managed (root) directories
         */
        public IResource FindOrCreateDirectory( string path )
        {
            IResource dir = FindDirectory( path );
            if( dir != null )
            {
                if( !dir.HasProp( FileProxy._propParentFolder ) )
                {
                    if( !Core.ResourceStore.IsOwnerThread() )
                    {
                        dir = (IResource) _resourceAP.RunUniqueJob( _cUpdatingFolderJob, _findOrCreateDirectoryDelegate, path );
                    }
                    else
                    {
                        DirectoryInfo di = IOTools.GetParent( path );
                        IResource parent = null;
                        if( di != null )
                        {
                            parent = FindOrCreateDirectory( IOTools.GetFullName( di ) );
                            if( parent != null && parent.IsDeleted )
                            {
                                parent = null;
                            }
                        }
                        if( parent == null )
                        {
                            dir.Delete();
                            dir = null;
                        }
                        else
                        {
                            dir.SetProp( FileProxy._propParentFolder, parent );
                        }
                    }
                }
            }
            else
            {
                if( !Core.ResourceStore.IsOwnerThread() )
                {
                    dir = (IResource) _resourceAP.RunUniqueJob( _cUpdatingFolderJob, _findOrCreateDirectoryDelegate, path );
                }
                else
                {
                    dir = Core.ResourceStore.BeginNewResource( FileProxy._folderResourceType );
                    try
                    {
                        dir.SetProp( FileProxy._propDirectory, path );
                        dir.SetProp( Core.Props.Name, IOTools.GetFileName( path ) ) ;
                        DirectoryInfo di = IOTools.GetDirectoryInfo( path );
                        if( di != null )
                        {
                            dir.SetProp( Core.Props.Date, IOTools.GetLastWriteTime( di ) );
                        }
                        dir.SetProp( FileProxy._propFileType, "Folder" );
                        di = IOTools.GetParent( path );
                        IResource parent = null;
                        if( di != null )
                        {
                            parent = FindOrCreateDirectory( IOTools.GetFullName( di ) );
                            if( parent != null && parent.IsDeleted )
                            {
                                parent = null;
                            }
                        }
                        if( parent == null )
                        {
                            dir.Delete();
                            dir = null;
                        }
                        else
                        {
                            dir.SetProp( FileProxy._propParentFolder, parent );
                        }
                    }
                    finally
                    {
                        if( dir != null )
                        {
                            dir.EndUpdate();
                        }
                    }
                }
            }
            return dir;
        }

        private delegate IResource FindOrCreateDirectoryDelegate( string path );

        /**
         * finds resource for a file
         */
        public IResource FindFile( FileInfo fileInfo )
        {
            IResource folder = FindDirectory( IOTools.GetDirectoryName( fileInfo ) );
            if( folder == null )
            {
                return null;
            }
            IResourceList files = folder.GetLinksTo( null, FileProxy._propParentFolder );
            files = files.Intersect(
                Core.ResourceStore.FindResources( null, Core.Props.Name, IOTools.GetName( fileInfo ) ), true );
            return ( files.Count > 0 ) ? files[ 0 ] : null;
        }

        public IResource FindFile( string path )
        {
            FileInfo fileInfo = IOTools.GetFileInfo( path );
            return ( fileInfo == null ) ? null : FindFile( fileInfo );
        }

        /**
         * finds or creates resource for a file
         * for the UnknowFile type creates transient resources
         */
        public IResource FindOrCreateFile( FileInfo fileInfo, bool createTransient )
        {
            string resourceType;
            bool indexIt = false;
            DateTime lastWriteTime = IOTools.GetLastWriteTime( fileInfo );
            string extension = IOTools.GetExtension( fileInfo );
            int size = (int) IOTools.GetLength( fileInfo );
            string name = IOTools.GetName( fileInfo );

            IResource file = FindFile( fileInfo );

            if( file != null )
            {
                if( !Core.ResourceStore.IsOwnerThread() )
                {
                    return (IResource) _resourceAP.RunUniqueJob( _cUpdatingFileJob, _findOrCreateFileDelegate, fileInfo, createTransient );
                }
                file.BeginUpdate();
                if( file.Type == FileProxy._unknownFileResourceType &&
                    ( resourceType = _ftm.GetResourceTypeByExtension( extension ) ) != null )
                {
                    file.ChangeType( resourceType );
                    indexIt = true;
                }
                if( name != file.GetPropText( Core.Props.Name ) )
                {
                    file.SetProp( Core.Props.Name, name );
                    indexIt = true;
                }
                if( lastWriteTime != file.GetDateProp( Core.Props.Date ) )
                {
                    file.SetProp( Core.Props.Date, lastWriteTime );
                    indexIt = true;
                }
                indexIt = indexIt || ( !file.IsTransient && !file.HasProp( "InTextIndex" ) );
                file.SetProp( FileProxy._propSize, size );
                string filetype = FileSystemTypes.GetFileType( extension );
                file.SetProp( FileProxy._propFileType, filetype ?? "Unknown" );
            }
            else
            {
                string directoryName = IOTools.GetDirectoryName( fileInfo );
                IResource folder = FindOrCreateDirectory( directoryName );
                if( folder == null )
                {
                    return null;
                }
                resourceType = _ftm.GetResourceTypeByExtension( extension );
                /**
                 * look through pending file deletions
                 */
                IResourceList deletedFiles = Core.ResourceStore.FindResourcesWithProp( resourceType, FileProxy._propDeletedFile );
                deletedFiles = deletedFiles.Intersect(
                    Core.ResourceStore.FindResources( null, FileProxy._propSize, size ), true );
                deletedFiles = deletedFiles.Intersect(
                    Core.ResourceStore.FindResources( null, Core.Props.Name, name ), true );
                if( deletedFiles.Count > 0 )
                {
                    file = deletedFiles[ 0 ];
                    if( !file.IsTransient )
                    {
                        if( !Core.ResourceStore.IsOwnerThread() )
                        {
                            return (IResource) _resourceAP.RunUniqueJob(
                                _cUpdatingFileJob, _findOrCreateFileDelegate, fileInfo, createTransient );
                        }
                        file.BeginUpdate();
                    }
                }
                if( file == null )
                {
                    if( resourceType != null && !createTransient )
                    {
                        if( !Core.ResourceStore.IsOwnerThread() )
                        {
                            return (IResource) _resourceAP.RunUniqueJob(
                                _cUpdatingFileJob, _findOrCreateFileDelegate, fileInfo, createTransient );
                        }
                        file = Core.ResourceStore.BeginNewResource( resourceType );
                        indexIt = true;
                    }
                    else
                    {
                        if( !createTransient )
                        {
                            return null;
                        }
                        if( resourceType == null )
                        {
                            resourceType = FileProxy._unknownFileResourceType;
                        }
                        file = Core.ResourceStore.NewResourceTransient( resourceType );
                    }
                }
                file.SetProp( FileProxy._propParentFolder, folder );
                file.SetProp( FileProxy._propDirectory, directoryName );
                file.SetProp( Core.Props.Name, name );
                file.SetProp( Core.Props.Date, lastWriteTime );
                file.SetProp( FileProxy._propSize, size );
                string filetype = FileSystemTypes.GetFileType( extension );
                file.SetProp( FileProxy._propFileType, filetype ?? "Unknown" );
            }
            file.SetProp( FileProxy._propDeletedFile, false );
            if( !file.IsTransient )
            {
                file.EndUpdate();
                if( indexIt )
                {
                    Core.TextIndexManager.QueryIndexing( file.Id );
                }
            }
            return file;
        }

        private delegate IResource FindOrCreateFileDelegate( FileInfo fileInfo, bool createTransient );

        /**
         * performs renaming of a directory updating all its sub-directories
         */
        public void RenameDirectory( IResource folder, string name, string fullname )
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                _resourceAP.RunUniqueJob( "Renaming directory",
                    new RenameDirectoryDelegate( RenameDirectory ), folder, name, fullname );
            }
            else
            {
                folder.BeginUpdate();
                try
                {
                    folder.SetProp( Core.Props.Name, name );
                    folder.SetProp( FileProxy._propDirectory, fullname );
                }
                finally
                {
                    folder.EndUpdate();
                }
                foreach( IResource res in folder.GetLinksTo( null, FileProxy._propParentFolder ) )
                {
                    if( res.Type != FileProxy._folderResourceType )
                    {
                        res.SetProp( FileProxy._propDirectory, fullname );
                    }
                    else
                    {
                        string folderName = res.GetPropText( Core.Props.Name );
                        string folderFullName = IOTools.Combine( fullname, folderName );
                        if( folderFullName.Length > 0 )
                        {
                            RenameDirectory( res, folderName, folderFullName );
                        }
                    }
                }
            }
        }

        public delegate void RenameDirectoryDelegate( IResource directory, string name, string fullname );

        /**
         * Deletion of FileFolder resource leads to deletion of all subfolders and file
         * recursively. Deletion of File resources linked with format resources leads
         * to deletion of these resources.
         */
        public void DeleteResource( IResource res )
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                _resourceAP.RunUniqueJob(
                    "Deleting file or directory resources", _deleteResourceDelegate, res );
                return;
            }
            // a resource just could be already deleted
            if( !res.IsDeleted )
            {
                if( res.Type == FileProxy._folderResourceType )
                {
                    IResourceList childs = res.GetLinksTo( null, FileProxy._propParentFolder );
                    foreach( IResource child in childs )
                    {
                        DeleteResource( child );
                    }
                    res.Delete();
                }
                else
                {
                    Core.TextIndexManager.DeleteDocumentQueued( res.Id );
                    res.SetProp( FileProxy._propDeletedFile, true );
                    QueueProcessPendingDeletions();
                }
            }
        }

        internal static void QueueProcessPendingDeletions()
        {
            Core.ResourceAP.QueueJobAt(
                DateTime.Now.AddMinutes( 1 ), new MethodInvoker( ProcessPendingFileDeletions ) );
        }

        /**
         * process pending deletions of file resources
         */
        internal static void ProcessPendingFileDeletions()
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.RunUniqueJob( new MethodInvoker( ProcessPendingFileDeletions ) );
            }
            else
            {
                IResourceList deletedFiles =
                    Core.ResourceStore.FindResourcesWithProp( null, FileProxy._propDeletedFile );
                deletedFiles.DeleteAll();
            }
        }

        /**
         * returns full name of a file by resource
         */
        public string GetFullName( IResource file )
        {
            return IOTools.Combine(
                file.GetPropText( FileProxy._propDirectory ), file.GetPropText( Core.Props.Name ) );
        }

        /**
         * is file or path monitored, i.e. processed immediatelly after changes occured
         */
        public bool IsPathMonitored( string path )
        {
            string topSubFolder = string.Empty;
            foreach( HashMap.Entry E in _excludedFolders )
            {
                string excludedFolder = (string) E.Key;
                if( topSubFolder.Length < excludedFolder.Length &&
                    String.Compare( path, 0, excludedFolder, 0, excludedFolder.Length, true ) == 0 )
                {
                    topSubFolder = excludedFolder;
                }
            }
            foreach( HashMap.Entry E in _deferredFolders )
            {
                string deferredFolder = (string) E.Key;
                if( topSubFolder.Length < deferredFolder.Length &&
                    String.Compare( path, 0, deferredFolder, 0, deferredFolder.Length, true ) == 0 )
                {
                    topSubFolder = deferredFolder;
                }
            }
            foreach( HashMap.Entry E in _monitoredFolders )
            {
                string monitoredFolder = (string) E.Key;
                if( topSubFolder.Length < monitoredFolder.Length &&
                    String.Compare( path, 0, monitoredFolder, 0, monitoredFolder.Length, true ) == 0 )
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * is file or path deffered, i.e. processed on startup only
         */
        public bool IsPathDeferred( string path )
        {
            string topSubFolder = string.Empty;
            foreach( HashMap.Entry E in _excludedFolders )
            {
                string excludedFolder = (string) E.Key;
                if( topSubFolder.Length < excludedFolder.Length &&
                    String.Compare( path, 0, excludedFolder, 0, excludedFolder.Length, true ) == 0 )
                {
                    topSubFolder = excludedFolder;
                }
            }
            foreach( HashMap.Entry E in _monitoredFolders )
            {
                string monitoredFolder = (string) E.Key;
                if( topSubFolder.Length < monitoredFolder.Length &&
                    String.Compare( path, 0, monitoredFolder, 0, monitoredFolder.Length, true ) == 0 )
                {
                    topSubFolder = monitoredFolder;
                }
            }
            foreach( HashMap.Entry E in _deferredFolders )
            {
                string deferredFolder = (string) E.Key;
                if( topSubFolder.Length < deferredFolder.Length &&
                    String.Compare( path, 0, deferredFolder, 0, deferredFolder.Length, true ) == 0 )
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * load all folders
         */
        public static void LoadFoldersForest()
        {
            Instance.Interrupted = false;
            FileProxy._filesProcessor.QueueJob( Instance );
        }

        #region ReenteringEnumeratorJob methods

        public override void EnumerationStarting()
        {
            _enumSignal.Reset();

            Dispose();

            if( Core.State == CoreState.ShuttingDown )
            {
                Interrupted = true;
                return;
            }

            _indexHidden = _settings.ReadBool( "FilePlugin", "IndexHidden", false );
            int propParent = FileProxy._propParentFolder;
            int propStatus = FileProxy._propStatus;
            int propName = Core.Props.Name;
            IResourceList folders = Core.ResourceStore.FindResourcesWithProp( FileProxy._folderResourceType, propStatus );
            /**
             * look through root folders, copy them to temporary map
             */
            HashMap foldersTable = new HashMap();
            string[] folderPaths = new string[ folders.Count ];
            int i = 0;
            foreach( IResource fileFolder in folders )
            {
                string path = fileFolder.GetPropText( propName );
                foldersTable[ path ] = fileFolder;
                folderPaths[ i++ ] = path;
            }
            /**
             * sort folders in order to group subfolders
             */
            Array.Sort( folderPaths );
            /**
             * look through sorted paths
             */
            foreach( string path in folderPaths )
            {
                if( path.Length > 0 )
                {
                    IResource fileFolder = (IResource) foldersTable[ path ];
                    new ResourceProxy( fileFolder ).SetProp( propParent, _filesRoot );
                    switch( fileFolder.GetIntProp( propStatus ) )
                    {
                        case 0:
                            /**
                             * check whether a path is already monitored (as a subdir of another monitored path)
                             */
                            if( !IsPathMonitored( path ) )
                            {
                                _monitoredFolders[ path ] = fileFolder;
                                FolderWatcher( path );
                            }
                            break;
                        case 1:
                            _deferredFolders[ path ] = fileFolder;
                            break;
                        case 2:
                            _excludedFolders[ path ] = fileFolder;
                            break;
                        default:
                            break;
                    }
                }
            }

            /**
             * collect all folders, build folders forest
             */
            CollectFolders( _monitoredFolders );
            CollectFolders( _deferredFolders );
            CollectFolders( _excludedFolders );
            _folderEnumerator = _allFolders.GetEnumerator();
        }

        public override void EnumerationFinished()
        {
            try
            {
                if( !Interrupted )
                {
                    /**
                     * enable all file system watchers
                     */
                    foreach( HashMap.Entry E in _folderWatchers )
                    {
                        FileSystemWatcher watcher = (FileSystemWatcher) E.Value;
                        try
                        {
                            watcher.EnableRaisingEvents = true;
                        }
                        catch
                        {
                            Trace.WriteLine( "Could not set file folder watcher for " + watcher.Path );
                        }
                    }
                }
            }
            finally
            {
                _allFolders.Clear();
                Core.UIManager.GetStatusWriter( this, StatusPane.UI ).ClearStatus();
                _enumSignal.Set();
            }
        }

        public override AbstractJob GetNextJob()
        {
            if( !Interrupted && _folderEnumerator.MoveNext() )
            {
                HashMap.Entry entry = (HashMap.Entry) _folderEnumerator.Current;
                string path = (string) entry.Key;
                IResource folder = (IResource) entry.Value;
                Core.UIManager.GetStatusWriter( this, StatusPane.UI ).ShowStatus( path );
                string jobName = "Scanning file folder - " + path;
                _args[ 0 ] = folder;
                if( IsPathDeferred( path ) || IsPathMonitored( path ) )
                {
                    if( _indexHidden )
                    {
                        return new DelegateJob( jobName, _enumerateDelegate, _args );
                    }
                    DirectoryInfo info = IOTools.GetDirectoryInfo( path );
                    if( info != null && ( IOTools.GetAttributes( info ) & FileAttributes.Hidden ) == 0 )
                    {
                        return new DelegateJob( jobName, _enumerateDelegate, _args );
                    }
                }
                return new DelegateJob( jobName, _excludeDelegate, _args );
            }
            return null;
        }

        public void WaitUntilFinished()
        {
            if( Application.MessageLoop )
            {
                FileAsyncProcessor.MsgWaitForSingleObject( _enumSignal, System.Threading.Timeout.Infinite );
            }
            else
            {
                _enumSignal.WaitOne();
            }
        }

        public override string Name
        {
            get { return "Collecting file folders"; }
        }

        #endregion

        private void CollectFolders( HashMap folders )
        {
            foreach( HashMap.Entry E in folders )
            {
                if( Interrupted )
                {
                    break;
                }
                string path = (string) E.Key;
                DirectoryInfo[] dirs = IOTools.GetDirectories( path );
                if( dirs != null )
                {
                    IResource folder = (IResource) E.Value;
                    _allFolders[ path ] = folder;
                    if( dirs.Length > 0 )
                    {
                        HashMap subFolders = new HashMap( dirs.Length / 2 );
                        foreach( DirectoryInfo di in dirs )
                        {
                            if( Interrupted )
                            {
                                break;
                            }
                            path = IOTools.GetFullName( di );
                            if( path.Length > 0 )
                            {
                                IResource directory = FindOrCreateDirectory( path );
                                if( directory != null )
                                {
                                    subFolders[ path ] = directory;
                                }
                            }
                        }
                        if( subFolders.Count > 0 )
                        {
                            CollectFolders( subFolders );
                        }
                    }
                }
                if( Processor.OutstandingJobs > 0 )
                {
                    DoJobs();
                }
            }
        }

        private void EnumerateFiles( IResource folder )
        {
            try
            {
                DateTime lastUpdated = folder.GetDateProp( _ftm.propLastModified );
                string directory = folder.GetPropText( FileProxy._propDirectory );
                FileInfo[] fileInfos = IOTools.GetFiles( directory );

                if( fileInfos != null )
                {
                    DateTime updated = DateTime.Now;
                    HashMap fileNames = null;

                    /**
                     * at first create missing resources
                     */
                    if( fileInfos.Length > 0 )
                    {
                        fileNames = new HashMap( fileInfos.Length / 2 );
                        foreach( FileInfo fileInfo in fileInfos )
                        {
                            fileNames[ IOTools.GetName( fileInfo ) ] = fileInfo;
                            if( IOTools.GetLastWriteTime( fileInfo ) > lastUpdated &&
                                ( _indexHidden || ( IOTools.GetAttributes( fileInfo ) & FileAttributes.Hidden ) == 0 ) )
                            {
                                FindOrCreateFile( fileInfo, false );
                            }
                        }
                    }

                    /**
                     * then remove obsolete resources
                     */
                    foreach( IResource child in folder.GetLinksTo( null, FileProxy._propParentFolder ).ValidResources )
                    {
                        if( child.Type == FileProxy._folderResourceType )
                        {
                            if( !Directory.Exists( child.GetPropText( FileProxy._propDirectory ) ) )
                            {
                                DeleteResource( child );
                            }
                        }
                        else
                        {
                            if( child.GetPropText( FileProxy._propDirectory ) != directory )
                            {
                                new ResourceProxy( child ).SetProp( FileProxy._propDirectory, directory );
                            }
                            string name = child.GetPropText( Core.Props.Name );
                            HashMap.Entry E = ( fileNames == null ) ? null : fileNames.GetEntry( name );
                            /**
                             * check if file exists
                             */
                            if( E == null )
                            {
                                DeleteResource( child );
                            }
                            else
                            {
                                /**
                                 * if exists, check its atributes
                                 */
                                FileInfo fileInfo = (FileInfo) E.Value;
                                if( fileInfo == null ||
                                    ( !_indexHidden && ( IOTools.GetAttributes( fileInfo ) & FileAttributes.Hidden ) != 0 ) )
                                {
                                    DeleteResource( child );
                                }
                            }
                        }
                    }

                    ResourceProxy proxy = new ResourceProxy( folder );
                    proxy.BeginUpdate();
                    proxy.SetProp( _ftm.propLastModified, updated );
                    proxy.SetProp( FileProxy._propFileType, "Folder" );
                    proxy.EndUpdateAsync();
                    FileProxy.UpdateFoldersTreePane( folder );
                }
            }
            catch( InvalidResourceIdException ) {}
            catch( ResourceDeletedException ) {}
        }

        internal void EnumerateParents( IResourceList resources )
        {
            HashSet parents = new HashSet();
            foreach( IResource res in resources.ValidResources )
            {
                IResource parent = res.GetLinkProp( FileProxy._propParentFolder );
                if( parent != null && !parent.IsDeleted )
                {
                    parents.Add( parent );
                }
            }
            foreach( HashSet.Entry e in parents )
            {
                EnumerateFiles( (IResource) e.Key );
            }
        }

        private void ExcludeFiles( IResource folder )
        {
            try
            {
                ResourceProxy proxy = new ResourceProxy( folder );
                proxy.BeginUpdate();
                proxy.DeleteProp( _ftm.propLastModified );
                proxy.SetProp( FileProxy._propFileType, "Folder" );
                proxy.EndUpdateAsync();

                string directory = folder.GetPropText( FileProxy._propDirectory );

                foreach( IResource child in folder.GetLinksTo( null, FileProxy._propParentFolder ).ValidResources )
                {
                    if( child.Type != FileProxy._folderResourceType )
                    {
                        if( child.GetPropText( FileProxy._propDirectory ) != directory )
                        {
                            new ResourceProxy( child ).SetProp( FileProxy._propDirectory, directory );
                        }
                        // do not delete permanent resource linked with any other resource, not only parent folder
                        if( child.GetLinkTypeIds().Length == 1 )
                        {
                            DeleteResource( child );
                        }
                    }
                }
                FileProxy.UpdateFoldersTreePane( folder );
            }
            catch( InvalidResourceIdException ) {}
            catch( ResourceDeletedException ) {}
        }

        private FileSystemWatcher FolderWatcher( string path )
        {
            HashMap.Entry E = _folderWatchers.GetEntry( path );
            if( E != null )
            {
                return (FileSystemWatcher) E.Value;
            }
            FileSystemWatcher watcher = new FileSystemWatcher();
            try
            {
                watcher.Path = path;
            }
            catch( ArgumentException e )
            {
                Trace.WriteLine( e.Message, "FilePlugin" );
                // FileSystemWatcher can treat a path as "invalid"
                // so seems FileSystemWatcher itself is invalid
                return null;
            }
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Changed += OnCreatedOrChanged;
            watcher.Created += OnCreatedOrChanged;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.EnableRaisingEvents = false;
            _folderWatchers[ path ] =  watcher;
            return watcher;
        }

        #region filesystem event handlers

        private void OnCreatedOrChanged( object source, FileSystemEventArgs e )
        {
            string path = e.FullPath;
            Trace.WriteLineIf( TraceWatchers(), "OnCreatedOrChanged( " + path + " )", "FilePlugin" );

            if( Directory.Exists( path ) )
            {
                IResource directory = FindOrCreateDirectory( path );
                if( directory != null )
                {
                    FileProxy.UpdateFoldersTreePane( directory );
                }
            }
            else
            {
                FileInfo fileInfo = IOTools.GetFileInfo( path );
                if( fileInfo != null )
                {
                    if( ( ( IOTools.GetAttributes( fileInfo ) & FileAttributes.Hidden ) == 0 || _indexHidden ) &&
                        _ftm.GetResourceTypeByExtension( IOTools.GetExtension( fileInfo ) ) != null )
                    {
                        FindOrCreateFile( fileInfo, false );
                    }
                }
            }
        }

        private void OnDeleted( object source, FileSystemEventArgs e )
        {
            string path = e.FullPath;
            IResource res;

            Trace.WriteLineIf( TraceWatchers(), "OnDeleted( " + path + " )", "FilePlugin" );

            // check whether a directory or a file was deleted
            res = FindFile( path );
            if( res == null )
            {
                res = FindDirectory( path );
            }
            if ( res != null )
            {
                DeleteResource( res );
            }
        }

        private void OnRenamed( object source, RenamedEventArgs e )
        {
            string oldPath = e.OldFullPath;
            string newPath = e.FullPath;
            Trace.WriteLineIf( TraceWatchers(), "OnRenamed( " + oldPath + " -> " + newPath + " )", "FilePlugin" );

            if( Directory.Exists( newPath ) )
            {
                DirectoryInfo di = IOTools.GetDirectoryInfo( newPath );
                if( di != null )
                {
                    newPath = IOTools.GetFullName( di );
                }
                IResource folder = FindDirectory( oldPath );
                RenameDirectory( folder, e.Name, newPath );
            }
            else
            {
                FileInfo fi = IOTools.GetFileInfo( newPath );
                if( fi != null )
                {
                    newPath = IOTools.GetFullName( fi );
                }
                IResource file = FindFile( oldPath );
                if( file == null )
                {
                    FileInfo fileInfo = IOTools.GetFileInfo( newPath );
                    FindOrCreateFile( fileInfo, false );
                }
                else
                {
                    string directory = Path.GetDirectoryName( newPath );
                    string filename = IOTools.GetFileName( newPath );
                    IResource folder = FindDirectory( directory );
                    ResourceProxy proxy = new ResourceProxy( file );
                    proxy.BeginUpdate();
                    try
                    {
                        proxy.SetProp( FileProxy._propDirectory, directory );
                        proxy.SetProp( Core.Props.Name, filename );
                        if( folder != null )
                        {
                            proxy.SetProp( FileProxy._propParentFolder, folder );
                        }
                    }
                    finally
                    {
                        proxy.EndUpdate();
                    }
                }
            }
        }

        private bool TraceWatchers()
        {
            return _settings.ReadBool( "FilePlugin", "TraceWatcher", false );
        }

        #endregion

        private static FoldersCollection        _instance;
        private bool                            _indexHidden;
        private IEnumerator                     _folderEnumerator;
        private IResource                       _filesRoot;
        private readonly ISettingStore          _settings;
        private readonly IAsyncProcessor        _resourceAP;
        private readonly FileResourceManager    _ftm;
        private readonly HashMap	            _monitoredFolders;
        private readonly HashMap	            _excludedFolders;
        private readonly HashMap	            _deferredFolders;
        private readonly HashMap	            _allFolders;
        private readonly HashMap				_folderWatchers;
        private readonly ResourceDelegate       _excludeDelegate;
        private readonly ResourceDelegate       _enumerateDelegate;
        private readonly ResourceDelegate       _deleteResourceDelegate;
        private readonly FindOrCreateDirectoryDelegate   _findOrCreateDirectoryDelegate;
        private readonly FindOrCreateFileDelegate        _findOrCreateFileDelegate;
        private readonly object[]                        _args;
        private readonly ManualResetEvent                _enumSignal;
    }
}
