/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
    /// <summary>
    /// The main plugin class.
    /// </summary>
    public class SccPlugin: IPlugin, IResourceDisplayer, IResourceIconProvider
    {
        private static IStatusWriter _statusWriter;
        private Icon _changeSetIcon;
        private Icon _openFolderIcon;
        private Icon _closedFolderIcon;
        private Icon _repositoryIcon;
        private static List<RepositoryType> _repositoryTypes;
        private static IResourceTreePane _folderTreePane;

        public static IStatusWriter StatusWriter
        {
            get { return _statusWriter; }
        }

        public static IEnumerable<RepositoryType> RepositoryTypes
        {
            get { return _repositoryTypes; }
        }

        public static IResourceTreePane FolderTreePane
        {
            get { return _folderTreePane; }
        }

        public void Register()
        {
            _repositoryTypes = new List<RepositoryType> {new P4RepositoryType(), new SvnRepositoryType()};

            Props.Register( this );
            
            // delete remnants of old version of the plugin
            if ( Core.ResourceStore.ResourceTypes.Exist( "jetbrains.p4.ChangeSet" ) )
            {
                Core.ResourceStore.GetAllResources( "jetbrains.p4.ChangeSet" ).DeleteAll();
                Core.ResourceStore.ResourceTypes.Delete( "jetbrains.p4.ChangeSet" );
            }
            if ( Core.ResourceStore.ResourceTypes.Exist( "jetbrains.p4.Folder" ) )
            {
                Core.ResourceStore.GetAllResources( "jetbrains.p4.Folder" ).DeleteAll();
                Core.ResourceStore.ResourceTypes.Delete( "jetbrains.p4.Folder" );
            }
            if ( Core.ResourceStore.ResourceTypes.Exist( "jetbrains.p4.FileChange" ) )
            {
                Core.ResourceStore.GetAllResources( "jetbrains.p4.FileChange" ).DeleteAll();
                Core.ResourceStore.ResourceTypes.Delete( "jetbrains.p4.FileChange" );
            }

            Core.TabManager.RegisterResourceTypeTab( "SCC", "SCC", Props.ChangeSetResource, 80 );

            Core.DisplayColumnManager.RegisterDisplayColumn( Props.ChangeSetResource, 0, 
                                                             new ColumnDescriptor( "From", 150 ) );
            Core.DisplayColumnManager.RegisterDisplayColumn( Props.ChangeSetResource, 1, 
                                                             new ColumnDescriptor( "Subject", 300 ) );
            Core.DisplayColumnManager.RegisterDisplayColumn( Props.ChangeSetResource, 2, 
                                                             new ColumnDescriptor( "Date", 120 ) );

            // Register a standard tree pane for showing the folder structure of the Perforce
            // repository and the changesets in each folder.
            _folderTreePane = Core.LeftSidebar.RegisterResourceStructureTreePane( "SccFolders", 
                                                                                  "SCC", "Folders", null,
                                                                                  Props.RepositoryResource );
            _folderTreePane.ToolTipCallback = GetRepositoryToolTip;

            // Set sorting by name for the folder structure
            IResource folderRoot = Core.ResourceTreeManager.GetRootForType( Props.RepositoryResource );
            Core.ResourceTreeManager.SetResourceNodeSort( folderRoot, "Name" );
            
            // Register a custom sidebar pane for showing the list of Perforce developers and
            // the changesets done by each developer.
            Core.LeftSidebar.RegisterViewPane( "SccDevelopers", "SCC", "Developers", null,
                                               new SccDeveloperPane() );

            Core.PluginLoader.RegisterResourceUIHandler( Props.FolderResource, new SccFolderUIHandler() );

            Core.PluginLoader.RegisterResourceDisplayer( Props.ChangeSetResource, this );
            Core.PluginLoader.RegisterResourceTextProvider( Props.ChangeSetResource, 
                                                            new ChangeSetTextProvider() );

            Core.UIManager.RegisterOptionsGroup( "Development", 
                                                 "The Development options pane controls different development-related plugins" );
            Core.UIManager.RegisterOptionsPane( "Development", "Source Control", new OptionsPaneCreator( CreateSccOptionsPane ), 
                                                "The Source Control options pane specifies the source control repositories monitored by the SCC plugin" );
            Core.UIManager.RegisterWizardPane( "Source Control", new OptionsPaneCreator( CreateSccOptionsPane ), 15  );

            _statusWriter = Core.UIManager.GetStatusWriter( this, StatusPane.Network );

            Assembly iconAssembly = Assembly.GetExecutingAssembly();
            _changeSetIcon = new Icon( iconAssembly.GetManifestResourceStream( "SccPlugin.changeset.ico" ) );
            _openFolderIcon = new Icon( iconAssembly.GetManifestResourceStream( "SccPlugin.OpenFolder.ico" ) );
            _closedFolderIcon = new Icon( iconAssembly.GetManifestResourceStream( "SccPlugin.ClosedFolder.ico" ) );
            _repositoryIcon = new Icon( iconAssembly.GetManifestResourceStream( "SccPlugin.repository.ico" ) );

            Core.ResourceIconManager.RegisterResourceIconProvider( 
                new string[] { Props.ChangeSetResource, Props.FolderResource, Props.RepositoryResource }, this );

            // Allow creating rules which affect resources of type ChangeSet
            Core.FilterEngine.RegisterRuleApplicableResourceType( Props.ChangeSetResource );
            
            // Register display columns for multiline view
            Core.DisplayColumnManager.RegisterMultiLineColumn( Props.ChangeSetResource,
                                                               Core.ContactManager.Props.LinkFrom,
                                                               0, 0, 0, 120, 
                                                               MultiLineColumnFlags.AnchorLeft | MultiLineColumnFlags.AnchorRight,
                                                               SystemColors.WindowText, HorizontalAlignment.Left );
            Core.DisplayColumnManager.RegisterMultiLineColumn( Props.ChangeSetResource,
                                                               Core.Props.Date,
                                                               0, 0, 120, 80, 
                                                               MultiLineColumnFlags.AnchorRight,
                                                               SystemColors.WindowText, HorizontalAlignment.Right );
            Core.DisplayColumnManager.RegisterMultiLineColumn( Props.ChangeSetResource,
                                                               Core.Props.Subject,
                                                               1, 1, 0, 144, 
                                                               MultiLineColumnFlags.AnchorLeft | MultiLineColumnFlags.AnchorRight,
                                                               Color.FromArgb( 112, 112, 112 ), HorizontalAlignment.Left );
            
            Core.ActionManager.RegisterMainMenuActionGroup( "SendReceiveActions", "Tools", ListAnchor.First );
            Core.ActionManager.RegisterMainMenuAction( new SynchronizeRepositoriesAction(), "SendReceiveActions",
                                                       ListAnchor.Last, "Synchronize Repositories", null, null, null );
            
            Core.ActionManager.RegisterActionComponent( new DeleteRepositoryAction(), "Delete",
                Props.RepositoryResource, null );
            Core.ActionManager.RegisterActionComponent( new SynchronizeRepositoryAction(), "Refresh",
                Props.RepositoryResource, null );
            
            Core.ActionManager.RegisterContextMenuActionGroup( "PropertiesActions", ListAnchor.Last );
            Core.ActionManager.RegisterContextMenuAction( new ToggleShowSubfolderContentsAction(), 
                                                          "PropertiesActions", ListAnchor.First, 
                                                          "Show subfolder contents", null, Props.FolderResource, null );
            Core.ActionManager.RegisterContextMenuAction( new RepositoryPropertiesAction(), "PropertiesActions", ListAnchor.First, 
                                                          "Properties...", null, Props.RepositoryResource, null );
        }

        public void Startup()
        {
            QueueSynchronizeRepositories();
        }

        public void Shutdown()
        {
        }

        /// <summary>
        /// Performs initial or timed poll of the Perforce repository to get information about
        /// new changesets.
        /// </summary>
        private static void TimedSynchronizeRepositories()
        {
            SynchronizeRepositories();

            QueueSynchronizeRepositories();
        }

        internal static void SynchronizeRepositories()
        {
            foreach( IResource res in Core.ResourceStore.GetAllResources( Props.RepositoryResource ) )
            {
                _statusWriter.ShowStatus( "Updating repository " + res.DisplayName + "..." );
                if ( Core.ProgressWindow != null )
                {
                    Core.ProgressWindow.UpdateProgress( 0, "Updating repository " + res.DisplayName + "...", null );
                }
                
                RepositoryType repType = GetRepositoryType( res );
                if ( repType != null )
                {
                    repType.UpdateRepository( res );
                }
            }
            
            _statusWriter.ClearStatus();
        }

        private static void QueueSynchronizeRepositories()
        {
            if ( Settings.PollInterval != 0 )
            {
                Core.NetworkAP.QueueJobAt( DateTime.Now.AddMinutes( Settings.PollInterval ),
                                           "Updating Perforce repositories",
                                           new MethodInvoker( TimedSynchronizeRepositories ) );
            }
        }

        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            if ( resourceType == Props.ChangeSetResource )
            {
                return new ChangeSetDisplayPane();
            }
            return null;
        }

        public Icon GetResourceIcon( IResource resource )
        {
            if ( resource.Type == Props.ChangeSetResource )
            {
                return _changeSetIcon;
            }
            if ( resource.Type == Props.FolderResource )
            {
                if ( resource.GetIntProp( Core.Props.Open ) == 1 )
                {
                    return _openFolderIcon;
                }
                return _closedFolderIcon;
            }
            if ( resource.Type == Props.RepositoryResource )
            {
                return _repositoryIcon;
            }
            return null;
        }

        public Icon GetDefaultIcon( string resType )
        {
            if ( resType == Props.ChangeSetResource )
            {
                return _changeSetIcon;
            }
            if ( resType == Props.FolderResource )
            {
                return _closedFolderIcon;
            }
            return null;
        }

        private static AbstractOptionsPane CreateSccOptionsPane()
        {
            return new SccOptionsPane();
        }

        public static RepositoryType GetRepositoryType( string id )
        {
            return _repositoryTypes.Find(type => type.Id == id);
        }

        public static RepositoryType GetRepositoryType( IResource repository )
        {
            return GetRepositoryType(repository.GetProp(Props.RepositoryType)); 
        }
        
        private static string GetRepositoryToolTip( IResource res )
        {
            return res.GetProp( Props.LastError );
        }
    }

    /// <summary>
    /// Accessors for the plugin settings.
    /// </summary>
    internal class Settings
    {
        internal static int ChangeSetsToIndex
        {
            get { return Core.SettingStore.ReadInt( "SccPlugin", "ChangeSetsToIndex", 100 ); }
            set { Core.SettingStore.WriteInt( "SccPlugin", "ChangeSetsToIndex", value ); }
        }

        internal static int PollInterval
        {
            get { return Core.SettingStore.ReadInt( "SccPlugin", "PollInterval", 5 ); }
            set { Core.SettingStore.WriteInt( "SccPlugin", "PollInterval", value ); }
        }
        
        internal static bool HideUnchangedFiles
        {
            get { return Core.SettingStore.ReadBool( "SccPlugin", "HideUnchangedFiles", false ); }
            set { Core.SettingStore.WriteBool( "SccPlugin", "HideUnchangedFiles", value ); }
        }
    }

    /// <summary>
    /// The user interface handler for the SccFolder resource type.
    /// </summary>
    internal class SccFolderUIHandler: IResourceUIHandler
    {
        public void ResourceNodeSelected( IResource res )
        {
            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            IResource repository = res;
            while( repository != null && repository.Type != Props.RepositoryResource )
            {
                repository = repository.GetLinkProp( Core.Props.Parent );
            }
            if ( repository != null && repository.HasProp( Props.LastError ) )
            {
                options.StatusLine = repository.GetProp( Props.LastError );
            }
            options.CaptionTemplate = "Changes in %OWNER%";
            IResourceList resourceList;
            if ( !res.HasProp( Props.ShowSubfolderContents ) )
            {
                resourceList = res.GetLinksOfType( Props.ChangeSetResource, Props.AffectsFolder );
            }
            else
            {
                resourceList = Core.ResourceStore.EmptyResourceList;
                resourceList = GatherContentsOfSubfolders( res, resourceList );
            }
            Core.ResourceBrowser.DisplayResourceList( res, resourceList, options );
        }

        private static IResourceList GatherContentsOfSubfolders( IResource folder, IResourceList changesets )
        {
            changesets = changesets.Union( folder.GetLinksOfType( Props.ChangeSetResource, Props.AffectsFolder ), true );
            foreach( IResource subfolder in folder.GetLinksTo( Props.FolderResource, Core.Props.Parent ) )
            {
                changesets = GatherContentsOfSubfolders( subfolder, changesets );
            }
            return changesets;
        }

        public bool CanRenameResource( IResource res )
        {
            return false;
        }

        public bool ResourceRenamed( IResource res, string newName )
        {
            return false;
        }

        public bool CanDropResources( IResource targetResource, IResourceList dragResources )
        {
            return false;
        }

        public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
        {
        }
    }

    /// <summary>
    /// Provides the indexable content for changeset resources.
    /// </summary>
    internal class ChangeSetTextProvider: IResourceTextProvider
    {
        public bool ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            consumer.AddDocumentFragment( res.Id, res.GetPropText( Core.Props.LongBody ) );
            consumer.AddDocumentFragment( res.Id, res.GetPropText( Core.ContactManager.Props.LinkFrom ),
                                          DocumentSection.SourceSection );
            return true;
        }
    }
}
