/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.GUIControls;
using JetBrains.DataStructures;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.FilePlugin
{
    [PluginDescription("File Explorer", "JetBrains Inc.", "Simple file explorer in Omea with the facilities to represent the Folders tree, include/exclude folders from text searching.", PluginDescriptionFormat.PlainText, "Icons/FilePluginIcon.png")]
    public class FileProxy : IPlugin, IStreamProvider, IResourceTextIndexingPermitter, IResourceIconProvider, IResourceUIHandler, IResourceDragDropHandler
    {
        #region IPlugin Members

        public void Register()
        {
            _folderChanged = SelectedFolderChanged;
            _fileRenamed = SelectedFolderRenamed;

            RegisterTypes();

            IUIManager uiMgr = Core.UIManager;
            OptionsPaneCreator filePaneCreator = SetFoldersForm.SetFoldersFormCreator;
            uiMgr.RegisterOptionsGroup( "Folders & Files", "The Folders & Files options enable you to control the indexing of folders and files." );
            uiMgr.RegisterOptionsPane( "Folders & Files", "Indexed Folders", filePaneCreator,
                "The Indexed Folders options enable you to specify what folders on your computer should be indexed by [product name], or specifically excluded from indexing." );
            uiMgr.RegisterWizardPane( "Indexed Folders", filePaneCreator, 0 );
            uiMgr.RegisterOptionsPane( "Folders & Files", "File Options", FileOptionsPane.FileOptionsPaneCreator,
                "The File Options enable you to control the indexing and display of hidden files, and to specify the extensions of files that [product name] should treat as plain text files." );
            uiMgr.RegisterResourceSelectPane( _folderResourceType, typeof( FileFoldersSelectorPane ) );
            Core.TabManager.RegisterResourceTypeTab( "Files", "Files", new[] { _folderResourceType }, _propParentFolder, 5 );

            IResource filesRoot = FoldersCollection.Instance.FilesRoot;
            Image img = Utils.TryGetEmbeddedResourceImageFromAssembly( Assembly.GetExecutingAssembly(), "FilePlugin.Icons.Folders24.png" );
            _pane = Core.LeftSidebar.RegisterResourceStructureTreePane( "FileFolders", "Files", "Indexed Folders", img, filesRoot );
            _pane.ParentProperty = _propParentFolder;
            _pane.AddNodeFilter( new FoldersFilter() );
            _pane.WorkspaceFilterTypes = new[] { _folderResourceType };
            Core.ResourceTreeManager.SetResourceNodeSort( filesRoot, "DisplayName" );
            Core.LeftSidebar.RegisterViewPaneShortcut( "FileFolders", Keys.Control | Keys.Alt | Keys.I );

            IPluginLoader loader = Core.PluginLoader;
            loader.RegisterStreamProvider( _folderResourceType, this );
            loader.RegisterResourceUIHandler( _folderResourceType, this );
            loader.RegisterResourceDragDropHandler( _folderResourceType, new DragDropLinkAdapter( this ) );

            Core.UIManager.RegisterResourceLocationLink( _folderResourceType, 0, _folderResourceType );

            Core.ResourceIconManager.RegisterPropTypeIcon( _propParentFolder, LoadIconFromAssembly( "file.ico" ) );
            Core.ResourceIconManager.RegisterResourceIconProvider( _folderResourceType, this );

            Core.ResourceBrowser.RegisterLinksPaneFilter( "FileFolder", new FileFolderLinksFilter() );

            ((FileResourceManager)Core.FileResourceManager).RegisterFileTypeColumns( _folderResourceType );

            LoadResourceIcons();

            _filesProcessor = new FileAsyncProcessor();
            _filesProcessor.ThreadName = "Files AsyncProcessor";
            Core.UIManager.RegisterIndicatorLight( "Files Processor", _filesProcessor, 60,
                LoadIconFromAssembly( "files_idle.ico" ),
                LoadIconFromAssembly( "files_busy.ico" ),
                LoadIconFromAssembly( "files_stuck.ico" ) );

            Core.ResourceStore.LinkAdded += ResourceStore_LinkAdded;
            Core.ResourceBrowser.ContentChanged += ResourceBrowser_ContentChanged;
        }

        private static Icon LoadIconFromAssembly( string iconName )
        {
        	return new Icon( Assembly.GetExecutingAssembly().GetManifestResourceStream( "FilePlugin.Icons." + iconName ) );
        }

        public void Startup()
        {
            Core.StateChanged += Core_StateChanged;
        }

        public void Shutdown()
        {
            if( _filesProcessor != null  )
            {
                Core.UIManager.DeRegisterIndicatorLight( "Files Processor" );
                _filesProcessor.Dispose();
            }
            FoldersCollection.Instance.Dispose();
        }

        private static void Core_StateChanged( object sender, EventArgs e )
        {
            if( Core.State == CoreState.Running )
            {
                _filesProcessor.StartThread();
                _filesProcessor.ThreadPriority = ThreadPriority.BelowNormal;
                FoldersCollection.LoadFoldersForest();
            }
            else if( Core.State == CoreState.ShuttingDown )
            {
                FoldersCollection.Instance.Interrupted = true;
                FoldersCollection.ProcessPendingFileDeletions();
            }
        }

        #endregion

        #region IStreamProvider Members

        public Stream GetResourceStream( IResource resource )
        {
            string path = FoldersCollection.Instance.GetFullName( resource );
            return IOTools.OpenRead( path );
        }

        #endregion

        #region IResourceTextIndexingPermitter Members

        bool IResourceTextIndexingPermitter.CanIndexResource( IResource res )
        {
            return !res.HasProp( _propDeletedFile );
        }

        #endregion

        public void LoadResourceIcons()
        {
            _folderIcon = LoadIconFromAssembly( "FolderClosed.ico" );
            _openFolderIcon = LoadIconFromAssembly( "FolderOpen.ico" );
            _excludedFolderIcon = LoadIconFromAssembly( "FolderExcluded.ico" );
        }

        public Icon GetResourceIcon( IResource resource )
        {
            if( resource.Type == _folderResourceType )
            {
                string directory = resource.GetPropText( _propDirectory );
                FoldersCollection instance = FoldersCollection.Instance;
                if( instance.IsPathDeferred( directory ) || instance.IsPathMonitored( directory ) )
                {
                    return ( resource.GetIntProp( "Open" ) != 0 ) ? _openFolderIcon : _folderIcon;
                }
                return _excludedFolderIcon;
            }
            return null;
        }

        public Icon GetDefaultIcon( string resType )
        {
            return ( resType == _folderResourceType ) ? _folderIcon : null;
        }

        #region IResourceUIHandler Members

        public bool CanDropResources( IResource targetResource, IResourceList dragResources )
        {
            return false;
        }

        public bool CanRenameResource( IResource res )
        {
            res = res.GetLinkProp( _propParentFolder );
            return res != null && res.Type == _folderResourceType;
        }

        public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
        {
        }

        public bool ResourceRenamed( IResource res, string newName )
        {
            string directory = res.GetPropText( _propDirectory );

            if( res.Type == _folderResourceType )
            {
                string destination;
                try
                {
                    destination = Path.Combine( Directory.GetParent( directory ).FullName, newName );
                    Directory.Move( directory, destination );
                }
                catch( Exception e )
                {
                    MessageBox.Show( Core.MainWindow, "Can't rename '" + directory + "': " + e.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    return false;
                }
                FoldersCollection.Instance.RenameDirectory( res, newName, destination );
            }
            else
            {
                string source = FoldersCollection.Instance.GetFullName( res );
                string destination = Path.Combine( directory , newName );

                if( source == destination )
                    return false;

                try
                {
                    File.Move( source, destination );
                }
                catch( Exception e )
                {
                    MessageBox.Show( Core.MainWindow, "Can't rename '" + source + "': " + e.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    return false;
                }
                new ResourceProxy( res ).SetPropAsync( Core.Props.Name, newName );
            }

            return true;
        }

        public void ResourceNodeSelected( IResource res )
        {
            try
            {
                ResourceNodeSelected( res, true );
            }
            catch( Exception e )
            {
                Utils.DisplayException( Core.MainWindow, e, "Error" );
                Core.UserInterfaceAP.QueueJob("GoBack", new MethodInvoker( Core.ResourceBrowser.GoBack ) );
            }
        }

        #endregion

        #region IResourceDragDropHandler Members

        public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            IResourceList droppedResources = data.GetData( typeof( IResourceList ) ) as IResourceList;
            if( droppedResources != null && targetResource.Type == _folderResourceType )
            {
                string directory = targetResource.GetPropText( _propDirectory );

                try
                {
                    foreach( IResource file in droppedResources )
                    {
                        string droppedName = file.GetPropText( Core.Props.Name );
                        string dest = Path.Combine( directory, droppedName );
    
                        if( file.Type == _folderResourceType )
                        {
                            if( ( keyState & 8 ) != 0 )
                            {
                                // copy files
                                FileInfo[] fileInfos = IOTools.GetFiles( file.GetPropText( _propDirectory ) );
                                if( fileInfos != null )
                                {
                                    if( Directory.Exists( dest ) || IOTools.CreateDirectory( dest ) != null )
                                    {
                                        foreach( FileInfo fileInfo in fileInfos )
                                        {
                                            string path = IOTools.GetFullName( fileInfo );
                                            if( path.Length > 0 )
                                            {
                                                File.Copy( path, IOTools.Combine( dest, IOTools.GetFileName( path ) ) );
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Core.ResourceAP.RunUniqueJob( "Moving directory",
                                    new MoveDirectoryDelegate( MoveDirectory ), dest, file, targetResource );
                            }
                        }
                        else
                        {
                            string source = Path.Combine( file.GetPropText( _propDirectory ), droppedName );
                            if( ( keyState & 8 ) != 0 )
                            {
                                File.Copy( source, dest, false );
                            }
                            else
                            {
                                Core.ResourceAP.RunUniqueJob( "Moving file",
                                    new MoveFileDelegate( MoveFile ), source, dest, directory, file, targetResource );
                            }
                        }
                    }
                }
                catch( Exception e )
                {
                    MessageBox.Show( Core.MainWindow, GetExceptionMessage( e ),
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                }
            }
        }

        public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            if( targetResource.Type != _folderResourceType )
            {
                return DragDropEffects.None;
            }

            IResourceList dragResources = data.GetData( typeof( IResourceList ) ) as IResourceList;
            if( dragResources != null )
            {
                string targetPath = targetResource.GetPropText( _propDirectory );
                foreach( IResource file in dragResources )
                {
                    IResource folder = file.GetLinkProp( _propParentFolder );
                    if( file.Type != _folderResourceType )
                    {
                        if( folder == null || folder.Type != _folderResourceType )
                        {
                            return DragDropEffects.None;
                        }
                    }
                    else
                    {
                        // do not drop folder to itself or a subfolder
                        if( folder == targetResource || targetPath.StartsWith( file.GetPropText( _propDirectory ) ) )
                        {
                            return DragDropEffects.None;
                        }
                    }
                }
                return ( ( keyState & 8 ) != 0 ) ? DragDropEffects.Copy : DragDropEffects.Link;
            }
            return DragDropEffects.None;
        }

        public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
        {}

        #endregion

        #region implementation details

        private void RegisterTypes()
        {
            IResourceStore store = Core.ResourceStore;

            IResource folderRes = store.FindUniqueResource( "ResourceType", "Name", _folderResourceType );
            if( folderRes == null )
            {
                store.ResourceTypes.Register(
                    _folderResourceType, "File Folder", "Name", ResourceTypeFlags.NoIndex | ResourceTypeFlags.ResourceContainer | ResourceTypeFlags.Internal );
            }
            else
            {
                folderRes.SetProp( "NoIndex", 1 );
                folderRes.SetProp( "ResourceContainer", 1 );
                folderRes.SetProp( "Internal", 1 );
                Core.ResourceStore.ResourceTypes ["FileFolder"].DisplayName = "File Folder";
            }
            
            /** 
             * file folders statuses:
             *   0 - monitored (immediate)
             *   1 - deferred
             *   2 - excluded
             */
            _propStatus = store.PropTypes.Register( "Status", PropDataType.Int );
            _propDirectory = store.PropTypes.Register( "Directory", PropDataType.String );
            _propSize = store.PropTypes.Register( "Size", PropDataType.Int );
            _propFileType = store.PropTypes.Register( "FileType", PropDataType.String );
            _propParentFolder = store.PropTypes.Register( "ParentFolder", 
                PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.SourceLink, this );
            store.PropTypes.RegisterDisplayName( _propParentFolder, "Folder", "File" );
            _propNew = store.PropTypes.Register( "New", PropDataType.Bool, PropTypeFlags.Internal );
            _propDeleted = store.PropTypes.Register( "Deleted", PropDataType.Bool, PropTypeFlags.Internal );
            _propDeletedFile = store.PropTypes.Register( "DeletedFile", PropDataType.Bool, PropTypeFlags.Internal );

            Core.WorkspaceManager.RegisterWorkspaceContainerType( _folderResourceType, 
                new[] { _propParentFolder }, _propParentFolder );
            Core.WorkspaceManager.RegisterWorkspaceSelectorFilter( _folderResourceType, new FoldersFilter() );

            IResourceList rootFolders = store.FindResourcesWithProp( _folderResourceType, _propStatus );
            if( rootFolders.Count == 0 )
            {
                store.GetAllResources( _folderResourceType ).DeleteAll();
            }
            else
            {
                foreach( IResource folder in rootFolders )
                {
                    folder.SetProp( _propDirectory, folder.GetPropText( Core.Props.Name ) );
                }
            }

            /**
             * for files, deleting permanent UnknownFile resources if they aren't linked
             * with no other resources except file folders
             */
            IResourceList unknownFiles = store.FindResourcesWithProp( _unknownFileResourceType, _propDirectory );
            foreach( IResource unknownFile in unknownFiles )
            {
                int[] links = unknownFile.GetLinkTypeIds();
                if( links.Length == 1 && links[ 0 ] == _propDirectory )
                {
                    unknownFile.Delete();
                }
            }
            if( store.ResourceTypes.Exist( "File" ) )
            {
                store.ResourceTypes.Delete( "File" );
            }

            /**
             * delete tree root garbage
             */
            IResourceList roots = store.FindResources( "ResourceTreeRoot", "RootResourceType", _folderResourceType );
            foreach( IResource root in roots )
            {
                if( root.GetLinksTo( _folderResourceType, _propParentFolder ).Count == 0 )
                {
                    FoldersCollection.Instance.DeleteResource( root );
                }
            }

            foreach( IResource res in FoldersCollection.Instance.FilesRoot.GetLinksTo( null, _propParentFolder ) )
            {
                if( res.Type != _folderResourceType || !res.HasProp( _propStatus ) )
                {
                    FoldersCollection.Instance.DeleteResource( res );
                }
            }

            store.RegisterUniqueRestriction( _folderResourceType, _propDirectory );
        }

        /**
         * Moving directories/files in resource thread
         */
        private delegate void MoveDirectoryDelegate( string dest, IResource folder, IResource targetResource );

        private static void MoveDirectory( string dest, IResource folder, IResource targetResource )
        {
            string source = folder.GetPropText( _propDirectory );
            Directory.Move( source, dest );
            folder.BeginUpdate();
            try
            {
                folder.SetProp( _propDirectory, dest );
                folder.SetProp( _propParentFolder, targetResource );
            }
            finally
            {
                folder.EndUpdate();
            }
            foreach( IResource file in folder.GetLinksTo( null, _propParentFolder ).ValidResources )
            {
                if( file.Type != _folderResourceType )
                {
                    file.SetProp( _propDirectory, dest );
                }
                else
                {
                    string name = file.GetPropText( _propDirectory ).Replace( source, null ).Trim( '/', '\\' );
                    file.SetProp( _propDirectory, IOTools.Combine( dest, name ) );
                }
            }
        }

        private delegate void MoveFileDelegate( string source, string dest, string directory, IResource file, IResource targetResource );

        private static void MoveFile( string source, string dest, string directory, IResource file, IResource targetResource )
        {
            File.Move( source, dest );
            if( !file.IsTransient )
            {
                file.BeginUpdate();
                try
                {
                    file.SetProp( _propDirectory, directory );
                    file.SetProp( _propParentFolder, targetResource );
                }
                finally
                {
                    file.EndUpdate();
                }
            }
        }

        /**
         * Actual folder contents displaying code
         */
        private delegate void ResourceNodeSelectedDelegate( IResource res, bool displayAnotherFolder );

        private static void ResourceNodeSelected( IResource res, bool displayAnotherFolder )
        {
            if( Core.State != CoreState.ShuttingDown && res.Type == _folderResourceType )
            {
                IResourceBrowser rBrowser = Core.ResourceBrowser;

                // for deleted folder display empty resource list
                if( res.IsDeleted )
                {
                    rBrowser.DisplayResourceList(
                        null, Core.ResourceStore.EmptyResourceList, res.DisplayName, null );
                    return;
                }

                if( !displayAnotherFolder && res != rBrowser.OwnerResource )
                {
                    return;
                }

                IResourceList selected = null;
                if( res == rBrowser.OwnerResource )
                {
                    selected = rBrowser.SelectedResources;
                }

                FoldersCollection foldersCollection = FoldersCollection.Instance;
                string path = res.GetPropText( _propDirectory );
                IResourceList folderContents = res.GetLinksToLive( null, _propParentFolder );

                /**
                 * look through folders, create new ones and delete obsolete
                 */
                DirectoryInfo[] dirs = IOTools.GetDirectories( path );
                if( dirs != null )
                {
                    foreach( DirectoryInfo dir in dirs )
                    {
                        foldersCollection.FindOrCreateDirectory( dir.FullName );
                    }

                    HashSet fileNames = new HashSet();
                    bool viewHidden = Core.SettingStore.ReadBool( "FilePlugin", "ViewHidden", false );
                    foreach( IResource file in folderContents.ValidResources )
                    {
                        if( file.Type == _folderResourceType )
                        {
                            if( !Directory.Exists( file.GetPropText( _propDirectory ) ) )
                            {
                                foldersCollection.DeleteResource( file );
                            }
                        }
                        else
                        {
                            string fullname = foldersCollection.GetFullName( file );
                            if( !File.Exists( fullname ) )
                            {
                                foldersCollection.DeleteResource( file );
                            }
                            else
                            {
                                FileInfo fi = IOTools.GetFileInfo( fullname );
                                if( viewHidden || ( fi.Attributes & FileAttributes.Hidden ) == 0 )
                                {
                                    fileNames.Add( file.GetPropText( Core.Props.Name ) );    
                                }
                            }
                        }
                    }

                    /**
                     * collect not indexed files
                     */
                    FileInfo[] files = IOTools.GetFiles( path );
                    if( files != null )
                    {
                        foreach( FileInfo fi in files )
                        {
                            if( ( viewHidden || ( fi.Attributes & FileAttributes.Hidden ) == 0 ) &&
                                !fileNames.Contains( fi.Name ) )
                            {
                                IResource file = foldersCollection.FindOrCreateFile( fi, true );
                                if( file != null )
                                {
                                    folderContents = folderContents.Union( file.ToResourceList(), true );
                                }
                            }
                        }
                    }
                }

                /**
                 * for folders excluded from indexing display deleted resources as well
                 */
                if( FoldersCollection.Instance.IsPathDeferred( path ) ||
                    FoldersCollection.Instance.IsPathMonitored( path ) )
                {
                    folderContents = folderContents.Minus(
                        Core.ResourceStore.FindResourcesWithPropLive( null, _propDeletedFile ) );
                }
                ColumnDescriptor[] fileColumns = new ColumnDescriptor[ 4 ];
                fileColumns[ 0 ].PropNames = new[] { "DisplayName" };
                fileColumns[ 0 ].Width = 300;
                fileColumns[ 0 ].CustomComparer = new FoldersUpComparer();
                fileColumns[ 1 ].PropNames = new[] { "FileType" };
                fileColumns[ 1 ].Width = 120;
                fileColumns[ 2 ].PropNames = new[] { "Size" };
                fileColumns[ 2 ].Width = 120;
                fileColumns[ 3 ].PropNames = new[] { "Date" };
                fileColumns[ 3 ].Width = 120;
                rBrowser.DisplayUnfilteredResourceList(
                    res, folderContents, path, Core.DisplayColumnManager.AddAnyTypeColumns( fileColumns ) );

                if( selected != null )
                {
                    foreach( IResource file in selected.ValidResources )
                    {
                        rBrowser.SelectResource( file );
                    }
                }
            }
        }

        /**
         * folders filter for tree pane
         */
        private class FoldersFilter : IResourceNodeFilter
        {
            public bool AcceptNode( IResource res, int level )
            {
                if ( res.Type == _folderResourceType && res.HasProp( _propNew ) )
                    return false;
                
                if ( res.HasProp( _propParentFolder ) && res.Type != _folderResourceType )
                    return false;

                return true;
            }
        }

        /**
         * custom comparer for file browser's DisplayName column
         */
        private class FoldersUpComparer : IResourceComparer
        {
            public int CompareResources( IResource r1, IResource r2 )
            {
                if( r1.Type == _folderResourceType )
                {
                    if( r2.Type == _folderResourceType )
                    {
                        return r1.DisplayName.CompareTo( r2.DisplayName );
                    }
                    return -1;
                }
                if( r2.Type == _folderResourceType )
                {
                    return 1;
                }
                return r1.DisplayName.CompareTo( r2.DisplayName );
            }
        }

        /**
         * tracking adding links to file resources
         * if link added to a transient file resource, it becomes permanent
         * ParentFolder links are obviously ignored
         */
        private static void ResourceStore_LinkAdded( object sender, LinkEventArgs e )
        {
            IResource res = e.Source;
            if( e.PropType != _propParentFolder && res.IsTransient)
            {
                IResource source = FileResourceManager.GetSource( res );
                if( source != null && source.Type == _folderResourceType )
                {
                    res.EndUpdate();
                    Core.TextIndexManager.QueryIndexing( res.Id );
                }
            }
        }

		/// <summary>
		/// Handler on resource browser's content changed.
		/// </summary>
        private void ResourceBrowser_ContentChanged( object sender, EventArgs e )
        {
        	IResource owner = Core.ResourceBrowser.OwnerResource;
            if( owner != null && owner.Type == _folderResourceType )
            {
                Core.ResourceAP.QueueJob(JobPriority.Immediate, "Set Folder Watcher", new ResourceDelegate( SetCurrentFolderWatcher ), owner );
            }
            else
            {
                Core.ResourceAP.QueueJob(JobPriority.Immediate, "Remove Folder Watcher", new MethodInvoker( DisposeCurrentFolderWatcher ) );
            }
        }

        private void DisposeCurrentFolderWatcher()
        {
            if( _selectedFolderWatcher != null )
            {
                _selectedFolderWatcher.Changed -= _folderChanged;
                _selectedFolderWatcher.Created -= _folderChanged;
                _selectedFolderWatcher.Deleted -= _folderChanged;
                _selectedFolderWatcher.Renamed -= _fileRenamed;
                _selectedFolderWatcher.Dispose();
                _selectedFolderWatcher = null;
            }
        }

        private void SetCurrentFolderWatcher( IResource owner )
        {
            DisposeCurrentFolderWatcher();
            string path = owner.GetPropText( _propDirectory );
            if( !FoldersCollection.Instance.IsPathMonitored( path ) )
            {
                try
                {
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    watcher.Path = path;
                    watcher.IncludeSubdirectories = false;
                    watcher.NotifyFilter =
                        NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                    watcher.Changed += _folderChanged;
                    watcher.Created += _folderChanged;
                    watcher.Deleted += _folderChanged;
                    watcher.Renamed += _fileRenamed;
                    watcher.EnableRaisingEvents = true;
                    _selectedFolderWatcher = watcher;
                }
                catch
                {
                    _selectedFolderWatcher = null;
                }
            }
        }

        /**
         * handler of changes in selected folder of file browser
         */
        private void SelectedFolderChanged( object source, FileSystemEventArgs e )
        {
            Core.ResourceAP.QueueJob(JobPriority.Immediate, "Selected Folder Changed", new MethodInvoker( SelectedFolderChangedImpl ) );
        }

        private void SelectedFolderRenamed( object sender, RenamedEventArgs e )
        {
            Core.ResourceAP.QueueJob( JobPriority.Immediate, "Selected Folder Changed", new MethodInvoker( SelectedFolderChangedImpl ) );
        }

        private void SelectedFolderChangedImpl()
        {
            IResource folder = _pane.SelectedNode;
            if( folder == null )
            {
                DisposeCurrentFolderWatcher();
            }
            else
            {
                UpdateFoldersTreePane( folder );
            }
        }

        internal static void UpdateFoldersTreePane( IResource updatedFolder )
        {
            if( updatedFolder != null && updatedFolder == Core.ResourceBrowser.OwnerResource )
            {
                Core.UIManager.QueueUIJob(
                    new ResourceNodeSelectedDelegate( ResourceNodeSelected ), updatedFolder, false );
            }
        }

        private static string GetExceptionMessage( Exception e )
        {
            return Utils.GetMostInnerException( e ).Message;
        }

        private class FileFoldersSelectorPane : ResourceTreeSelectPane, IResourceNodeFilter
        {
            public FileFoldersSelectorPane()
            {
                _resourceTree.ParentProperty = _propParentFolder;
                _resourceTree.AddNodeFilter( this );
            }

            public override IResource GetSelectorRoot( string resType )
            {
                return FoldersCollection.Instance.FilesRoot;
            }

            bool IResourceNodeFilter.AcceptNode( IResource res, int level )
            {
                return res.Type == _folderResourceType;
            }
        }

        private class FileFolderLinksFilter : ILinksPaneFilter
        {
            public bool AcceptLinkType( IResource displayedResource, int propId, ref string displayName )
            {
                if ( propId == _propParentFolder )
                {
                    displayName = "Parent Folder";
                }
                else if ( propId == -_propParentFolder )
                {
                    displayName = "Contains";
                }
                return true;
            }

            public bool AcceptLink( IResource displayedResource, int propId, IResource targetResource,
                                    ref string linkTooltip )
            {
                return true;
            }

            public bool AcceptAction( IResource displayedResource, IAction action )
            {
                return true;
            }
        }

        internal const string               _folderResourceType = "FileFolder";
        internal const string               _unknownFileResourceType = "UnknownFile";
        internal static int                 _propStatus;
        internal static int                 _propDirectory;
        internal static int                 _propSize;
        internal static int                 _propFileType;
        internal static int                 _propParentFolder;
        internal static int                 _propNew;
        internal static int                 _propDeleted;
        internal static int                 _propDeletedFile;
        internal static IResourceTreePane   _pane;
        private FileSystemWatcher           _selectedFolderWatcher;
        private FileSystemEventHandler      _folderChanged;
        private RenamedEventHandler         _fileRenamed;
        private Icon                        _folderIcon, _openFolderIcon, _excludedFolderIcon;
        internal static FileAsyncProcessor  _filesProcessor;

        #endregion
    }
}
