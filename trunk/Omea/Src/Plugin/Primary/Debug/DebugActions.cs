/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.TextIndex;

namespace JetBrains.Omea.DebugPlugin
{
    /**
     * Action to view properties of a resource.
     */

    public class ShowResourcePropertiesAction : ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            if ( context.SelectedResources != null && context.SelectedResources.Count > 0 )
            {
                foreach ( IResource resource in context.SelectedResources )
                {
                    ResourcePropertiesDialog dlg = new ResourcePropertiesDialog();
                    dlg.SetResource( resource );
                    dlg.Show();
                }
            }
        }
    }
    public class StartOutlookAction : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            new OutlookCotrolPanel().Show();
        }
    }

    public class BrowseResourcesAction : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            new ResourceBrowser( ICore.Instance.ResourceStore ).Show();
        }
    }
    public class RegisterQwertyProtocol : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Core.ProtocolHandlerManager.RegisterProtocolHandler( "qwerty", "Simple Qwerty Protocol", OpenURL );
        }
        private static void OpenURL( string url )
        {
            MessageBox.Show( "Qwerty protocol = " + url );
        }
    }

    public class ShowCacheWatchAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            CacheWatch dlg = new CacheWatch();
            dlg.Show();
        }
    }

    public class TraceOperationsAction: IAction
    {
    	public void Execute( IActionContext context )
    	{
            MyPalStorage.TraceOperations = !MyPalStorage.TraceOperations;
    	}

    	public void Update( IActionContext context, ref ActionPresentation presentation )
    	{
            presentation.Checked = MyPalStorage.TraceOperations;
    	}
    }

    public class TextIndexPresentFilter: IActionStateFilter
    {
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = Core.TextIndexManager.IsIndexPresent();
        }
    }

    public class ThrowExceptionAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Core.ReportException( new Exception( "Exception from DebugPlugin" ), ExceptionReportFlags.AttachLog );
        }
    }

    public class SubmitBackgroundExceptionAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Core.ReportBackgroundException( new Exception( "Test background exception from DebugPlugin" ) );
        }
    }

    public class TraceFocusedControlAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Tracer.TraceFocusedControl();
        }
    }

    public class LocateWebBrowserAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Trace.WriteLine( "Core.WebBrowser.Visible is " + Core.WebBrowser.Visible );
            if ( Core.WebBrowser.Parent == null )
            {
                Trace.WriteLine( "Core.WebBrowser.Parent is NULL " );
            }
            else
            {
                Control parent = Core.WebBrowser.Parent;
                while( parent != null )
                {
                    Trace.WriteLine( "Core.WebBrowser.Parent is " + parent.GetType().Name + " " + parent.Name +
                        ", Visible=" + parent.Visible + ", Bounds=" + parent.Bounds + ", Dock=" + parent.Dock );
                    parent = parent.Parent;
                }
            }
            Trace.WriteLine( "Core.WebBrowser.Bounds is " + Core.WebBrowser.Bounds );
            Trace.WriteLine( "Core.WebBrowser.Dock is " + Core.WebBrowser.Dock );
        }
    }

    public class ViewSourceAction: IAction
    {
        public void Execute( IActionContext context )
        {
            context.CommandProcessor.ExecuteCommand( "ViewSource" );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = context.CommandProcessor.CanExecuteCommand( "ViewSource" );
        }
    }

    /**
     * Action to update the unread counters on all resources.
     */

    public class RefreshUnreadCountersAction: ResourceAction
    {
        public override void Execute( IResourceList selectedResources )
        {
            (Core.UnreadManager as UnreadManager).RefreshUnreadCounters();
        }
    }

    public class InvalidateUnreadCounterAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            Core.UnreadManager.InvalidateUnreadCounter( context.SelectedResources [0] );
        }
    }

    public class RepairDatabaseAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Core.SettingStore.WriteBool( "ResourceStore", "FullRepairRequired", true );
        }
    }
    public class SettingOptionsForDebug: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            SettingOptionsForDebugDlg dlg = new SettingOptionsForDebugDlg();
            using ( dlg )
            {
                dlg.ShowDialog();
            }
        }
    }

    public class Queue10SecJobAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            //DebugPossibility.Queue10SecJob();
        }
    }

    public class ViewInResourceListView2Action: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            ResourceListDataProvider provider = new ResourceListDataProvider( Core.ResourceBrowser.VisibleResources );
            ResourceListView2TestForm testForm = new ResourceListView2TestForm( provider, false );
            testForm.Show();
        }
    }

    public class ViewTreeInResourceListView2Action: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            ResourceTreeDataProvider provider = new ResourceTreeDataProvider( Core.ResourceTreeManager.ResourceTreeRoot,
                Core.Props.Parent );
            ResourceListView2TestForm testForm = new ResourceListView2TestForm( provider, true );
            provider.SelectResource( Core.LeftSidebar.DefaultViewPane.SelectedNode );
            testForm.Show();
        }
    }

    public class DeleteETagAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            for( int i=0; i<context.SelectedResources.Count; i++ )
            {
                new ResourceProxy( context.SelectedResources [i] ).DeleteProp( "ETag" );
            }
        }
    }

    public class ThreadTimesAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            new ThreadTimesForm().Show();
        }
    }

    public class RestrictResourceDeleteAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            IResource resTypeResource = Core.UIManager.SelectResource( "ResourceType", 
                "Select a resource type to restrict deletion" );
            if ( resTypeResource != null )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceDelegate( DoRegisterRestriction ),
                    resTypeResource );
            }
        }

        private static void DoRegisterRestriction( IResource res )
        {
            string resType = res.GetStringProp( "Name" );
            Core.ResourceStore.RegisterRestrictionOnDelete( resType, new ForbidRestriction( resType ) );
        }
    }

    public class QueueSocketExceptionAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            ThreadPool.QueueUserWorkItem( ThrowException );
        }

        private static void ThrowException( object o )
        {
            throw new SocketException( 31415 );
        }
    }

    internal class ForbidRestriction : IResourceRestriction
    {
        private readonly string _resType;
        private readonly IResourceList _deletedResources;

        public ForbidRestriction( string resType )
        {
            _resType = resType;
            _deletedResources = Core.ResourceStore.FindResourcesWithPropLive( _resType, Core.Props.IsDeleted );
            _deletedResources.ResourceAdded += HandleDeletedResourceAdded;
        }

        public void CheckResource( IResource res )
        {
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( DeleteSelf ) );
            throw new Exception( "The specified operation is forbidden by a debug action" );
        }

        private void DeleteSelf()
        {
            Core.ResourceStore.DeleteRestrictionOnDelete( _resType );
            _deletedResources.ResourceAdded -= HandleDeletedResourceAdded;
            _deletedResources.Dispose();
        }

        private void HandleDeletedResourceAdded( object sender, ResourceIndexEventArgs e )
        {
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( DeleteSelf ) );
            throw new Exception( "Temporary resource delete is forbidden by a debug action" );
        }
    }

    #region Contacts and ContactNames Cleanup
    public class AnalyzeContactNamesAction : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            int  count = 0, maxCount = 0;
            string maxContact = "";
            IResourceList contacts = Core.ResourceStore.GetAllResources( "Contact" );
            for( int i = 0; i < contacts.Count; i++ )
            {
                Trace.WriteLine( "ViewsInitializer -- Upgrading contact names for contact: " + contacts[ i ].DisplayName );
                IResourceList accounts = contacts[ i ].GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct );
                foreach( IResource accnt in accounts )

                {
                    IResourceList cNames = contacts[ i ].GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
                    cNames = cNames.Intersect( accnt.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkEmailAcct ) );

                    int localCount = ProcessContactNamesList( cNames );
                    if( localCount > maxCount )
                    {
                        maxCount = localCount;
                        maxContact = contacts[ i ].DisplayName;
                    }
                    count += localCount;
                }
            }
            MessageBox.Show( "Met " + count + " repeatable contact names. Maximal duplicating contact is [" + maxContact + "]" );
        }

        private static int ProcessContactNamesList( IResourceList cNames )
        {
            if( cNames.Count <= 1 )
                return 0;

            cNames.Sort( new SortSettings( Core.Props.Name, true ) );

            int count = 0;
            for( int i = 1; i < cNames.Count; i++ )
            {
                if( cNames[ i ].GetStringProp( "Name" ) == cNames[ i - 1 ].GetStringProp( "Name" ) )
                    count++;
            }

            return count;
        }
    }

    public class RemoveHangedByBaseContactLinkContactNamesAction : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            int  hangedCount = 0;
            IResourceList allCNames = Core.ResourceStore.GetAllResources( "ContactName" );
            for( int i = 0; i < allCNames.Count; i++ )
            {
                IResource name = allCNames[ i ];
                int linksCount = name.GetLinksOfType( null, Core.ContactManager.Props.LinkNameTo ).Count + 
                                 name.GetLinksOfType( null, Core.ContactManager.Props.LinkNameFrom ).Count + 
                                 name.GetLinksOfType( null, Core.ContactManager.Props.LinkNameCC ).Count;
                if( linksCount == 0 )
                {
                    hangedCount++;
                    new ResourceProxy( name ).Delete();
                }
            }
            MessageBox.Show( "Cleaned " + hangedCount + " hanged contact names by BaseContact link only." );
        }
    }

    public class RemoveHangedContactNameswithoutEmailAction : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            int  hangedCount = 0, fromMyselfCount = 0, trueDuplicatesCount = 0;
            IResourceList allCNames = Core.ResourceStore.GetAllResources( "ContactName" );

            for( int i = 0; i < allCNames.Count; i++ )
            {
                IResource name = allCNames[ i ];
                int linksCount = name.GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct ).Count;
                if( linksCount == 0 )
                {
                    hangedCount++;

                    IResource baseContact = name.GetLinkProp( Core.ContactManager.Props.LinkBaseContact );
                    if( baseContact.HasProp( "Myself" ))
                    {
                        fromMyselfCount++;
                        if( PassedTest( name, baseContact ) )
                        {
                            trueDuplicatesCount++;
                            new ResourceProxy( name ).Delete();
                        }
                    }
                }
            }
            MessageBox.Show( "Found " + hangedCount + " hanged contact names by \"without emailAccount\" link only. " +
                             "Counted " + fromMyselfCount + " contact from Myself." + 
                             "Counter " + trueDuplicatesCount + " contacts from Myself and having duplicates.");
        }

        private bool  PassedTest( IResource cName, IResource baseContact )
        {
            bool  hasDuplicate = false;
            IResourceList linkedMails = cName.GetLinksOfType( null, Core.ContactManager.Props.LinkNameTo ).Union( 
                                        cName.GetLinksOfType( null, Core.ContactManager.Props.LinkNameFrom ).Union( 
                                        cName.GetLinksOfType( null, Core.ContactManager.Props.LinkNameCC ) ));
            if( linkedMails.Count == 1 )
            {
                IResource mail = linkedMails[ 0 ];
                int  linkNameId = GetLinkId( cName );

                IResourceList linkedNames = mail.GetLinksOfType( "ContactName", linkNameId );
                if( linkedNames.Count > 1 )
                {
                    foreach( IResource res in linkedNames )
                    {
                        if( res.Id != cName.Id &&
                            res.GetLinkProp( Core.ContactManager.Props.LinkBaseContact ) == baseContact )
                        {
                            hasDuplicate = true;
                            break;
                        }
                    }
                }
            }
            return hasDuplicate;
        }

        private int  GetLinkId( IResource cName )
        {
            if( cName.GetLinksOfType( null, Core.ContactManager.Props.LinkNameFrom ).Count > 0 )
                return Core.ContactManager.Props.LinkNameFrom;
            else
            if( cName.GetLinksOfType( null, Core.ContactManager.Props.LinkNameTo ).Count > 0 )
                return Core.ContactManager.Props.LinkNameTo;
            else
            if( cName.GetLinksOfType( null, Core.ContactManager.Props.LinkNameCC ).Count > 0 )
                return Core.ContactManager.Props.LinkNameCC;
            else
                return -1;
        }
    }

    public class CountSingleThusUnnecessaryContactNamesAction : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            int  count = 0, derivedNamesCount = 0;
            IResourceList contacts = Core.ResourceStore.GetAllResources( "Contact" );

            for( int i = 0; i < contacts.Count; i++ )
            {
                IResourceList cNames = contacts[ i ].GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
                foreach( IResource cName in cNames )
                {
                    String cname = cName.GetStringProp( Core.Props.Name );
                    String contact = contacts[ i ].DisplayName;
                    if( cname == contact || CleanedName( cname ) == contact )
                    {
                        count++;
                    }
                    else
                    {
                        derivedNamesCount++;
                    }
                }
            }
            MessageBox.Show( "To be cleaned " + count + " completely unnecessary contact names (" + count * 3 + 
                             " unnecessary links), " + derivedNamesCount + " derived contact names" );
        }
        private static String CleanedName( String name )
        {
            if( name.Length > 2 && name[ 0 ]=='\'' && name[ name.Length - 1 ]=='\'' )
                name = name.Substring( 1, name.Length - 2 ).Trim();

            if( name.Length > 2 && name[ 0 ]=='"' && name[ name.Length - 1 ]=='"' )
                name = name.Substring( 1, name.Length - 2 ).Trim();

            if( name[ 0 ] == '\n' )
                name = name.Substring( 1, name.Length - 1 ).Trim();

            return name;
        }
    }

    public class DeleteSingleThusUnnecessaryContactNamesAction : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Core.UIManager.RunWithProgressWindow( "Upgrading Contact Names Information to 2.5 format.", DeleteCNs );
        }

        private static void DeleteCNs()
        {
            int  count = 0, illegallyNamedCount = 0;
            IResourceList contacts = Core.ResourceStore.GetAllResources( "Contact" );
            ContactManager.UnlinkIdenticalContactNames( contacts, Core.ProgressWindow, 
                                                       ref count, ref illegallyNamedCount );
            MessageBox.Show( count + " completely unnecessary contact names removed, of that - " +
                             illegallyNamedCount + " illegally named Contact Names" );
        }
    }
    #endregion Contacts and ContactNames Cleanup

    #region TextIndex actions
    public class DefragmentAction: IAction
    {
        public void Execute( IActionContext context )
        {
            throw new NotImplementedException( "Text index defragmentation can't be run in the UI thread." );
            //Core.NetworkAP.QueueJob( new MethodInvoker( FullTextIndexer.Instance.DefragmentIndex ) );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = Core.TextIndexManager.IsIndexPresent();
        }
    }

    public class RequestIndexingAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            foreach( IResource res in context.SelectedResources )
            {
                Core.TextIndexManager.QueryIndexing( res.Id );
            }
        }
    }

    public class ShowDocIndexInfoAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ViewDocIndexContentForm form;
            if( context.SelectedResources != null && context.SelectedResources.Count == 1 )
                form = new ViewDocIndexContentForm( context.SelectedResources[ 0 ] );
            else
                form = new ViewDocIndexContentForm();
            form.ShowDialog();
            form.Dispose();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Kind == ActionContextKind.ContextMenu) && 
                                   (context.SelectedResources.Count == 1);
            presentation.Enabled = Core.TextIndexManager.IsIndexPresent();
        }
    }

    public class ListUnindexedResourcesInTextIndexAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ArrayList       unindexedIDs = new ArrayList();
            Hashtable       unindexedByType = new Hashtable();

            foreach( IResourceType resType in Core.ResourceStore.ResourceTypes )
            {
                if( IsResTypeIndexingConformant( resType ) )
                {
                    IResourceList resList = Core.ResourceStore.GetAllResources( resType.Name );
                    foreach( int resID in resList.ResourceIds )
                    {
                        if( resID >= 0 && !Core.TextIndexManager.IsDocumentInIndex( resID ))
                        {
                            unindexedIDs.Add( resID );
                            if( !unindexedByType.ContainsKey( resType.DisplayName ))
                                unindexedByType[ resType.DisplayName ] = 1;
                            else
                                unindexedByType[ resType.DisplayName ] = (int)unindexedByType[ resType.DisplayName ] + 1;
                            Trace.WriteLine( "Debug -- Resource synchronization analysis. Resource [" + resID + "] of " + resType.DisplayName + " is absent in TI" );
                        }
                    }
                }
            }

            if( unindexedIDs.Count == 0 )
                MessageBox.Show( "Resource Store and Text Index are in sync", "Synchronization results", 
                                 MessageBoxButtons.OK, MessageBoxIcon.Information );
            else
            {
                string  resultInfo = "Text Index does not contain " + unindexedIDs.Count + " entries: ";
                foreach( string key in unindexedByType.Keys )
                {
                    resultInfo += key + " - " + (int)unindexedByType[ key ] + "; ";
                }
                MessageBox.Show( resultInfo,  "Synchronization results", MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Kind == ActionContextKind.MainMenu);
            presentation.Enabled = Core.TextIndexManager.IsIndexPresent();
        }
        //  For a resource to be text-indexed its resource type must conform
        //  to the following criteria:
        //  - have valid name
        //  - be indexable
        //  - its oqner plugin must be loaded
        //  - even if its plugin is loaded (or the owner may be omitted), it
        //    must be either a file (for a FilePlugin to be able to index it) or
        //    have some ITextIndexProvider, specific for this particular
        //    resource type.
        private static bool  IsResTypeIndexingConformant( IResourceType resType )
        {
            return   !String.IsNullOrEmpty( resType.Name ) && !resType.HasFlag( ResourceTypeFlags.NoIndex ) &&
                     resType.OwnerPluginLoaded &&
                    ( resType.HasFlag( ResourceTypeFlags.FileFormat ) ||
                      Core.PluginLoader.HasTypedTextProvider( resType.Name ));
        }
    }

    public class DumpTermTrie: IAction
    {
        public void Execute( IActionContext context )
        {
            AsyncProcessor   tiAP = null;
            AsyncProcessor[] pool = AsyncProcessor.GetAllPooledProcessors();
            foreach( AsyncProcessor ap in pool )
            {
                if( ap.ThreadName == "TextIndex AsyncProcessor" )
                {
                    tiAP = ap;
                }
            }
            if( tiAP != null )
            {
                tiAP.RunUniqueJob( new MethodInvoker( DumpTrie ) );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Kind == ActionContextKind.MainMenu);
            presentation.Enabled = Core.TextIndexManager.IsIndexPresent();
        }

        private static void DumpTrie()
        {
            StreamWriter writer = new StreamWriter( "c:\\temp\\dump" );
            int     index = 256;
            string  str = Word.GetTokensById( index );
            while( str != null )
            {
                writer.WriteLine( str );
                index++;
                str = Word.GetTokensById( index );
            }
            writer.Close();
        }
    }
    #endregion TextIndex actions

    #region Stress testing actions

    public class ResourceBrowserSmokeTestAction: SimpleAction
    {
        private IResource _owner;
        private IResourceList _resources;
        private Random _rnd;

        public override void Execute( IActionContext context )
        {
            _owner = Core.ResourceBrowser.OwnerResource;
            _resources = Core.ResourceBrowser.VisibleResources;
            _rnd = new Random();
            QueueNextRedisplay();
        }

        private void DisplayRandomResource()
        {
            if( _owner == Core.ResourceBrowser.OwnerResource )
            {
                Core.ResourceBrowser.SelectResource( _resources[ _rnd.Next( 0, _resources.Count - 1 ) ] );
                QueueNextRedisplay();
            }
        }

        private void QueueNextRedisplay()
        {
            Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddSeconds( 1 ), new MethodInvoker( DisplayRandomResource ) );
        }
    }
    #endregion

	public class TestDdeAction: SimpleAction
	{
		public override void Execute( IActionContext context )
		{
			new TestDde().Run();
		}
	}

    public class CleanOutlookDataAction : SimpleAction
    {
		public override void Execute( IActionContext context )
		{
            Core.UIManager.RunWithProgressWindow( "Deleting Outlook Resources", Do );
		}
        private void Do()
        {
            if( !Core.ResourceAP.IsOwnerThread )
            {
                Core.ResourceAP.RunJob( new MethodInvoker( Do ) );
            }
            else
            {
                DeleteResourcesOfType( "Email", "Deleting Outlook mail" );
//                DeleteResourcesOfType( "MAPIFolder", "Deleting Outlook folders" );
                DeleteFolders();
                DeleteResourcesOfType( "AttachmentType", "Deleting Attachment types" );
                DeleteResourcesOfType( "OutlookABDescriptor", "Deleting Outlook AB descriptors" );
                DeleteResourcesOfType( "SyncVersion", "Deleting Outlook AB descriptors" );
                DeleteResourcesOfType( "ResourceAttachment", "Deleting Outlook AB descriptors" );
                DeleteResourcesOfType( "MAPIStore", "Deleting Outlook AB descriptors" );
                DeleteResourcesOfType( "MAPIInfoStore", "Deleting Outlook AB descriptors" );
            }
        }

        private static void DeleteResourcesOfType( string type, string message )
        {
            int percent = 0;
            IResourceList list = Core.ResourceStore.GetAllResources( type );
            for( int i = 0; i < list.Count; i++ )
            {
                IResourceList attachs = list[ i ].GetLinksOfType( null, "Attachment" );
                attachs.DeleteAll();

                list[ i ].Delete();
                if( (i * 100 / list.Count ) != percent )
                {
                    percent = i * 100 / list.Count;
                    Core.ProgressWindow.UpdateProgress( percent, message, String.Empty );
                }
            }
        }

        private static void DeleteFolders()
        {
            int percent = 0;
            IResourceList list;
            do
            {
                list = Core.ResourceStore.GetAllResources( "MAPIFolder" );
                for( int i = 0; i < list.Count; i++ )
                {
                    if( list[ i ].GetLinksTo( "MAPIFolder", Core.Props.Parent ).Count == 0 )
                    {
                        list[ i ].Delete();
                    }
                    if( (i * 100 / list.Count ) != percent )
                    {
                        percent = i * 100 / list.Count;
                        Core.ProgressWindow.UpdateProgress( percent, "Deleting Outlook Folders", String.Empty );
                    }
                }
            }
            while( list.Count != 0 );
        }
    }

    #region statistics actions

    public class GetNewsReadersStatAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            IResourceStore store = Core.ResourceStore;
            IResourceList articles = store.GetAllResources( "Article" ).Minus(
                store.FindResourcesWithProp( null, Core.Props.IsDeleted ) );
            IResourceList resources = context.SelectedResources;
            if( !resources.AllResourcesOfType( "NewsGroup" ) )
            {
                resources = null;
            }
            IStatusWriter writer = Core.UIManager.GetStatusWriter( this, StatusPane.UI );
            try
            {
                HashMap readers = new HashMap();
                int i = 0;
                int percent = -1;
                foreach( IResource article in articles )
                {
                    int p = ( i++ * 100 ) / articles.Count;
                    if( p > percent )
                    {
                        percent = p;
                        writer.ShowStatus( "Looking through news articles: " + percent.ToString() + "%" );
                        Application.DoEvents();
                    }
                    if( resources != null &&
                        article.GetLinksOfType( "NewsGroup", "Newsgroups" ).Intersect( resources ).Count == 0 )
                    {
                        continue;
                    }
                    string[] headers = article.GetPropText( "MessageHeaders" ).Split( '\n' );
                    string reader = null;
                    foreach( string headerLine in headers )
                    {
                        if( headerLine.StartsWith( "X-Newsreader: " ) )
                        {
                            reader = headerLine.Substring( "X-Newsreader: ".Length ).TrimEnd( '\r', '\n' );
                        }
                        else if( headerLine.StartsWith( "User-Agent: " ) )
                        {
                            reader = headerLine.Substring( "User-Agent: ".Length ).TrimEnd( '\r', '\n' );
                        }
                        if( reader != null )
                        {
                            for( int j = 1; j < reader.Length; ++j )
                            {
                                if( Char.IsDigit( reader[ j ] ) )
                                {
                                    reader = reader.Substring( 0, j - 1 );
                                    break;
                                }
                            }
                            reader = reader.Trim();
                            if( reader.Length == 0 )
                            {
                                break;
                            }
                            HashMap.Entry entry = readers.GetEntry( reader );
                            if( entry == null )
                            {
                                readers[ reader ] = 1;
                            }
                            else
                            {
                                entry.Value = ( (int) entry.Value ) + 1;
                            }
                            break;
                        }
                    }
                }
                PriorityQueue queue = new PriorityQueue();
                foreach( HashMap.Entry e in readers )
                {
                    queue.Push( (int)e.Value, e.Key );
                }
                StringBuilder readersStat = new StringBuilder();
                foreach( PriorityQueue.QueueEntry e in queue )
                {
                    readersStat.Append( "\r\n" );
                    readersStat.Append( e.Value );
                    readersStat.Append( ": " );
                    readersStat.Append( e.Key );
                }
                MessageBox.Show( readersStat.ToString(), "News Readers Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
            finally
            {
                writer.ClearStatus();
            }
        }
    }

    public class CalcLinksStatisticsAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            Hashtable linkCalc = new Hashtable();

            foreach( IResourceType resType in Core.ResourceStore.ResourceTypes )
            {
                IResourceList resList = Core.ResourceStore.GetAllResources( resType.Name );
                foreach( IResource res in resList.ValidResources )
                {
                    CalcResourceLinks( res, ref linkCalc );
                }
            }

            foreach( string type in linkCalc.Keys )
            {
                Trace.WriteLine( type + ": " + linkCalc[ type ] );
            }

            linkCalc.Clear();
            IResourceList list = Core.ResourceStore.FindResourcesWithProp( null, Core.ContactManager.Props.LinkNameFrom );
            foreach( IResource res in list )
            {
                if( linkCalc.ContainsKey( res.Type ) )
                    linkCalc[ res.Type ] = (int)linkCalc[ res.Type ] + 1;
                else
                    linkCalc[ res.Type ] = 1;
            }

            Trace.WriteLine("-------------------------------------------------------");
            foreach( string type in linkCalc.Keys )
            {
                Trace.WriteLine( type + ": " + linkCalc[ type ] );
            }
            Trace.WriteLine("-------------------------------------------------------");
        }

        private static void  CalcResourceLinks( IResource res, ref Hashtable linkCalc )
        {
            IPropertyCollection props = res.Properties;
            foreach( IResourceProperty prop in props )
            {
                if( prop.DataType == PropDataType.Link && ( prop.PropId > 0 ))
                {
                    if( !linkCalc.ContainsKey( prop.Name ) )
                        linkCalc[ prop.Name ] = 1;
                    else
                        linkCalc[ prop.Name ] = (int)linkCalc[ prop.Name ] + 1;
                }
            }
        }
    }
    #endregion

    public class TracePerformanceCountersAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            MyPalStorage.Storage.TraceDbPerformanceCounters();
            FullTextIndexer.Instance.TraceIndexPerformanceCounters();
        }
    }

    public class ShowObscureColumnDescriptors : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            int count = 0;
            int resId = 0;
            IResourceList list = Core.ResourceStore.GetAllResources( "ColumnScheme" );
            foreach( IResource res in list )
            {
                string type = res.GetPropText( "ColumnKeyTypes" );
                if( type == "Email" || type == "Article" )
                {
                    IResourceList descriptors = res.GetLinksOfType( "ColumnDescriptor", "ColumnDescriptor" );
                    foreach( IResource descr in descriptors )
                    {
                        IStringList props = descr.GetStringListProp( "ColumnProps" );
                        int index = props.IndexOf( "DisplayName" );
                        if (index != -1 && props.Count > 1)
                        {
                            count++;
                            if( resId == 0 )
                                resId = descr.Id;
                        }
                    }
                }
            }
            MessageBox.Show("Count of such columns is " + count + " with first id = " + resId);
        }
    }

    public class ListUnlinkedConditions : SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            int count = 0;
            IResourceList list = Core.ResourceStore.FindResources( FilterManagerProps.ConditionResName, 
                                                                   Core.FilterRegistry.Props.OpProp, (int)ConditionOp.QueryMatch );
            Trace.WriteLine( "-----------------------------------------------------------------");
            foreach( IResource res in list )
            {
                IResourceList groups = res.GetLinksOfType( FilterManagerProps.ConjunctionGroup, Core.FilterRegistry.Props.LinkedConditions );
                if( groups.Count == 0 )
                {
                    Trace.WriteLine( "Condition [" + res.Id + "] is hanged");
                    count++;
                }
            }
            Trace.WriteLine( "Total " + count + " conditions are hanged");
            Trace.WriteLine( "-----------------------------------------------------------------");
        }
    }

    public class CorrectObscureColumnDescriptors : SimpleAction
    {
        private delegate void MyDelegate( IStringList list, int index );

        public override void Execute( IActionContext context )
        {
            IResourceList list = Core.ResourceStore.GetAllResources( "ColumnScheme" );
            foreach( IResource res in list )
            {
                string type = res.GetPropText( "ColumnKeyTypes" );
                if( type == "Email" || type == "Article" )
                {
                    int index;
                    IResourceList descriptors = res.GetLinksOfType( "ColumnDescriptor", "ColumnDescriptor" );
                    foreach( IResource descr in descriptors )
                    {
                        IStringList props = descr.GetStringListProp( "ColumnProps" );
                        index = props.IndexOf( "DisplayName" );
                        if( index != -1 && props.Count > 1 )
                        {
                            Core.ResourceAP.RunUniqueJob( new MyDelegate( RemoveStringListValue ), props, index );
                            MessageBox.Show( "Removed prop from ColumnDescriptor " + descr.Id + ", for type=" + type );
                        }
                    }

                    IStringList sortProps = res.GetStringListProp("ColumnSortProps");
                    index = sortProps.IndexOf("DisplayName");
                    if (index != -1 && sortProps.Count > 1)
                    {
                        Core.ResourceAP.RunUniqueJob(new MyDelegate(RemoveStringListValue), sortProps, index);
                        MessageBox.Show("Removed prop from ColumnScheme " + res.Id + ", for type=" + type);
                    }
                }
            }
        }
        private static void RemoveStringListValue( IStringList props, int index )
        {
            props.RemoveAt( index );
        }
    }
}
