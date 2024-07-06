// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.ContactsPlugin
{
    /**
     * "New Contact..." action.
     */

    public class NewContactAction: IAction
    {
        private class NewContactLocation
        {
            internal IResource AddressBook;
            internal IResource Category;
        }

        public void Execute( IActionContext context )
        {
            ContactView cv = new ContactView();
            IResource contact = Core.ResourceStore.NewResourceTransient( "Contact" );

            NewContactLocation location = new NewContactLocation();
            IResource owner = Core.ResourceBrowser.OwnerResource;

            if ( context.SelectedResources.Count == 1 && context.SelectedResources [0].Type == "AddressBook" )
            {
                location.AddressBook = context.SelectedResources [0];
            }
            else if ( owner != null && owner.Type == "AddressBook" )
            {
                location.AddressBook = owner;
            }
            if ( owner != null && owner.Type == "Category" )
            {
                location.Category = owner;
            }

            Core.UIManager.OpenResourceEditWindow( cv, contact, true, OnNewContactSaved, location );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( ContactsPlugin.IsReader )
            {
                presentation.Visible = false;
                return;
            }

            if ( context.Kind == ActionContextKind.ContextMenu )
            {
                presentation.Visible = (context.SelectedResources.Count == 1) &&
                                       context.SelectedResources.AllResourcesOfType( "AddressBook");
            }
            else if ( context.Kind == ActionContextKind.Keyboard )
            {
                presentation.Visible = (Core.TabManager.CurrentTabId == "Contacts" );
            }
        }

        private static void OnNewContactSaved( IResource res, object tag )
        {
            Trace.WriteLine( "OnNewContactSaved is called for " + res.DisplayName );
            // NB: SetPropAsync does not work properly under Windows2000
//            new ResourceProxy( res ).SetPropAsync( ContactHelper._propUserCreated, true );
            new ResourceProxy( res ).SetProp( ContactManager._propUserCreated, true );

            if ( tag != null )
            {
                NewContactLocation location = (NewContactLocation) tag;
                if ( location.AddressBook != null )
                {
                    AddressBook ab = new AddressBook( location.AddressBook );
                    ab.AddContact( res );
                }
                if ( location.Category != null )
                {
					Core.CategoryManager.AddResourceCategory( res, location.Category );
                }
            }

            Core.WorkspaceManager.AddToActiveWorkspace( res );
        }
    }

    public class EditContactAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            //  Edit Contact can be issued on resources of both "Contact" and
            //  "ContactName" types.
            IResource contact = context.SelectedResources[ 0 ];
            if( contact.Type == "ContactName" )
                contact = contact.GetLinkProp( Core.ContactManager.Props.LinkBaseContact );

            ContactView cv = new ContactView();
            Core.UIManager.OpenResourceEditWindow( cv, contact, false );
        }
    }

    #region Merge/Split
    public class MergeContactAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IResource  resultContact;
            if( context.SelectedResources.Count == 1 )
            {
                IResourceList candidates = ContactManager.GetContactsForMerging( context.SelectedResources[ 0 ] );
                if( candidates.Count == 0 )
                    candidates = null;
                resultContact = ShowMergeDialog( candidates, context.SelectedResources );
            }
            else
            {
                resultContact = ShowMergeDialog( context.SelectedResources, null );
            }

            if ( resultContact != null )
            {
                if( Core.TabManager.CurrentTab.Name == "Contacts" )
                {
                    Core.ResourceBrowser.SelectResource( resultContact );
                }
                else
                {
                    AbstractViewPane pane = Core.LeftSidebar.GetPane( StandardViewPanes.Correspondents );
                    if( pane != null )
                        pane.SelectResource( resultContact, false );
                }
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            int count = context.SelectedResources.Count;
            presentation.Visible = (count > 0);
            if( count == 1 )
                presentation.Text = "Merge Contact With...";
            else
            if( count > 1 )
                presentation.Text = "Merge Selected Contacts...";
        }

        #region Impl
        private delegate IResource DelegateMerge( string fullName, IResourceList contacts, bool showOrigNames );

        /// <summary>
        /// Shows the contact merge dialog and performs the merge if accepted by the user.
        /// </summary>
        /// <param name="contacts">The contacts which are shown in the suggestions list.</param>
        /// <param name="defaultContactsToMerge">The contacts which are initially shown in
        /// the contacts to merge list.</param>
        /// <returns>The merge result contact, or null if the user cancelled the merge operation.</returns>
        public static IResource ShowMergeDialog( IResourceList contacts, IResourceList defaultContactsToMerge )
        {
        	var dlg = new MergeContactsForm(contacts, defaultContactsToMerge);
        	using(dlg)
        	{
        		if(dlg.ShowDialog() == DialogResult.OK)
        		{
        			//  a list of contacts can be changed in the dialog,
        			//  e.g. new contacts may appeared.
        			Cursor.Current = Cursors.WaitCursor;
        			IResource result = null;
        			Core.UIManager.RunWithProgressWindow("Merging Contacts…", delegate { result = DoMerge(dlg.FullName, dlg.ResultContacts, dlg.ShowOriginalNames); });
        			Cursor.Current = Cursors.Default;
        			return result;
        		}
        		return null;
        	}
        }

        private static IResource DoMerge( string fullName, IResourceList contacts, bool showOrigNames )
        {
            Core.ProgressWindow.UpdateProgress( 0, "Merging...", "" );
            IResource resultContact = (IResource) Core.ResourceAP.RunJob( new DelegateMerge( Merge ),
                fullName, contacts, showOrigNames );
            return resultContact;
        }

        private static IResource Merge( string fullName, IResourceList contacts, bool showOrigNames )
        {
            IResource resultContact = Core.ContactManager.Merge( fullName, contacts );
            if( showOrigNames )
                resultContact.SetProp( Core.ContactManager.Props.ShowOriginalNames, true );
            return resultContact;
        }
        #endregion Impl
    }

    public class SplitContactAction: IAction
    {
        IResourceList  resultContacts;
        private delegate void DelegateSplit( IResource contact, IResourceList contacts2Split );

        private void DoSplit( IResource contact, IResourceList contacts2Split )
        {
            Core.ProgressWindow.UpdateProgress( 0, "Splitting...", "" );
            Core.ResourceAP.RunJob( new DelegateSplit( Split ), contact, contacts2Split );
        }
        private void Split( IResource contact, IResourceList contacts2Split )
        {
            resultContacts = Core.ContactManager.Split( contact, contacts2Split );
        }

    	public void Execute(IActionContext context)
    	{
    		Debug.Assert(context.SelectedResources.Count == 1, "Contact splitting action is called with illegal number of parameters (only one contact is expected)");

    		IResource contact = context.SelectedResources[0];
    		if(contact.Type == "ContactName")
    			contact = contact.GetLinkProp(Core.ContactManager.Props.LinkBaseContact);

    		var form = new SplitContactForm(contact);
    		if(form.ShowDialog() == DialogResult.OK)
    		{
    			Cursor.Current = Cursors.WaitCursor;
    			Core.UIManager.RunWithProgressWindow("Merging Contacts…", delegate { DoSplit(contact, form.Contacts2Split); });
    			Cursor.Current = Cursors.Default;
    			UpdateResourcePanes();
    		}
    	}

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.SelectedResources.Count == 1);
            //  SplitContact can be issued on resources of both "Contact"
            //  and "ContactName" types.
            if( presentation.Visible )
            {
                IResource contact = context.SelectedResources[ 0 ];
                if( contact.Type == "ContactName" )
                    contact = contact.GetLinkProp( Core.ContactManager.Props.LinkBaseContact );

                if ( context.Kind == ActionContextKind.ContextMenu )
                {
                    presentation.Visible = presentation.Visible &&
                        contact.HasProp( ContactManager._propSerializationBlobLink );
                }
                else
                {
                    presentation.Enabled = presentation.Visible &&
                        contact.HasProp( ContactManager._propSerializationBlobLink );
                }
            }
        }
        private void UpdateResourcePanes()
        {
            if( Core.TabManager.CurrentTab.Name == "Contacts" )
                Core.ResourceBrowser.SelectResource( resultContacts[ 0 ] );
            else
            {
                AbstractViewPane pane = Core.LeftSidebar.GetPane( StandardViewPanes.Correspondents );
                if( pane != null && resultContacts.Count > 0 )
                    pane.SelectResource( resultContacts[ 0 ], false );
            }
        }
    }
    #endregion Merge/Split

    #region AdressBooks
    /**
     * Action to create an address book.
     */

    public class CreateABAction: IAction
    {
        public void Execute( IActionContext context )
        {
            string name = "New Address Book";
            int uniqueNumber = 0;
            while ( Core.ResourceStore.FindResources( "AddressBook", "DeepName", name ).Count > 0 )
            {
                uniqueNumber++;
                name = "New Address Book " + uniqueNumber;
            }

            Core.UIManager.BeginUpdateSidebar();
            Core.TabManager.CurrentTabId = "Contacts";
            Core.LeftSidebar.ActivateViewPane( "AddressBooks" );
            Core.UIManager.EndUpdateSidebar();

            AddressBook ab = new AddressBook( name );
            Core.WorkspaceManager.AddToActiveWorkspace( ab.Resource );
            ContactsPlugin.AddressBookPane.EditResourceLabel( ab.Resource );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( ContactsPlugin.IsReader )
            {
                presentation.Visible = false;
            }
        }
    }

    /**
     * Action to delete an address book.
     */

    public class DeleteABAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            string prompt = "Do you wish to delete ";
            if ( context.SelectedResources.Count == 1 )
            {
                prompt += "the address book '" + context.SelectedResources [0].GetStringProp( "Name" ) + "'?";
            }
            else
            {
                prompt += context.SelectedResources.Count + " selected address books?";
            }

            if ( MessageBox.Show( ICore.Instance.MainWindow, prompt, "Delete Address Book",
                MessageBoxButtons.YesNo ) == DialogResult.Yes )
            {
                foreach( IResource res in context.SelectedResources )
                {
                    new ResourceProxy( res ).DeleteAsync();
                }
            }
        }
    }

    /**
     * Action to remove a contact from an address book.
     */

    public class RemoveFromABAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ContactManager.RemoveContactFromAddressBook( context.LinkTargetResource, context.SelectedResources[ 0 ] );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.SelectedResources.Count == 1) &&
                                   (context.LinkTargetResource != null) &&
                                   (context.LinkPropId == AddressBook.PropInAddressBook);
            presentation.Enabled = presentation.Visible && !context.SelectedResources[ 0 ].HasProp("IsNonExportable");
        }
    }

    public class RemoveContactFromAddressBookAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ContactManager.RemoveContactFromAddressBook( context.SelectedResources[ 0 ], context.ListOwnerResource );
        }
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.ListOwnerResource != null) &&
                                   (context.ListOwnerResource.Type == "AddressBook");
            presentation.Enabled = presentation.Visible && !context.ListOwnerResource.HasProp( "IsNonExportable" ) &&
                                   (context.SelectedResources.Count == 1);
        }
    }
    #endregion AdressBooks

    public class DisplayMailsForEmailAccount : IAction
    {
        public void Execute( IActionContext context )
        {
            if( context.SelectedResources.Count == 0 ) return;

            Core.UIManager.BeginUpdateSidebar();
            Core.TabManager.CurrentTabId = Core.TabManager.Tabs [0].Id;
            Core.UIManager.EndUpdateSidebar();

            IResource mailAccount = context.SelectedResources[0];
            IResourceList resourceList = mailAccount.GetLinksOfTypeLive( null, Core.ContactManager.Props.LinkEmailAcctFrom );
            IResourceList resourceListTo = mailAccount.GetLinksOfTypeLive( null, Core.ContactManager.Props.LinkEmailAcctTo );
            IResourceList resourceListCC = mailAccount.GetLinksOfTypeLive( null, Core.ContactManager.Props.LinkEmailAcctCC );
            resourceList = resourceList.Union( resourceListTo );
            resourceList = resourceList.Union( resourceListCC );

            ResourceTypeHelper.ExcludeUnloadedPluginResources( resourceList );

            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.Caption = "Messages for " + mailAccount.DisplayName;
            options.SetTransientContainer( Core.ResourceTreeManager.ResourceTreeRoot,
                StandardViewPanes.ViewsCategories );
            Core.ResourceBrowser.DisplayResourceList( null, resourceList, options );
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.SelectedResources.Count == 1);
        }
    }

    public class CopyEmailAccountAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            try
            {
                Clipboard.SetDataObject( context.SelectedResources [0].GetStringProp( "EmailAddress" ) );
            }
            catch( ExternalException ex )
            {
                MessageBox.Show( Core.MainWindow, "Failed to copy email account to clipboard: " + ex.Message,
                    Core.ProductFullName, MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }
    }

    public class MailToContactAction : IAction
    {
        static private IEmailService _emailService;
        static private bool _init;

        static private IEmailService GetEmailService()
        {
            if ( !_init )
            {
                _init = true;
                _emailService = (IEmailService) Core.PluginLoader.GetPluginService( typeof( IEmailService ) );
            }
            return _emailService;
        }
        public void Execute( IActionContext context )
        {
            Tracer._Trace( "Execute action: MailToContactAction" );

            IResource contact = context.SelectedResources[ 0 ];
            if ( contact.Type == "Contact" || contact.Type == "ContactName" )
            {
                string body = null;
                bool greeting = Core.SettingStore.ReadBool( "MailFormat", "GreetingInReplies", false );
                if ( context.SelectedResources.Count == 1 && greeting )
                {
                    body += "Hello " + contact.DisplayName + ",\r\n\r\n";
                }
                GetEmailService().CreateEmail( null, body, EmailBodyFormat.PlainText, context.SelectedResources, null, true );
            }
            else if ( contact.Type.ToLower().Equals( "emailaccount" ) )
            {
                GetEmailService().CreateEmail(  null, null, EmailBodyFormat.PlainText, context.SelectedResources, null, true  );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (GetEmailService() != null) && (context.SelectedResources.Count > 0);
            presentation.Enabled = presentation.Visible && context.SelectedResources[ 0 ].HasProp( "EmailAcct" );
        }
    }

    public class CleanUnusedContactsAction : IAction
    {
    	public void Execute(IActionContext context)
    	{
    		IResource resource = context.SelectedResources[0];
    		Core.UIManager.RunWithProgressWindow("Deleting Contacts…", delegate { ExecuteMarshaller(resource); });
    	}

    	public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = IsContactsTab() &&
                (context.Instance == Core.LeftSidebar.DefaultViewPane) &&
                (context.SelectedResources.Count == 1 ) &&
                (context.SelectedResources[ 0 ].Type == FilterManagerProps.ViewResName);
        }

        private static bool  IsContactsTab()
        {
            IResourceTypeTab tab = Core.TabManager.CurrentTab;
            return (tab.Name == "Contacts");
        }

        private static void  ExecuteMarshaller( IResource res )
        {
            Core.ResourceBrowser.BeginUpdate();
            Core.ProgressWindow.UpdateProgress( 0, "Deleting Contacts...", "" );
            Core.ResourceAP.RunJob( new ReenteringContactsDeleter( res ) );
            Core.ResourceBrowser.EndUpdate();
        }
    }

    internal class ReenteringContactsDeleter : ReenteringEnumeratorJob
    {
        readonly IResource  SavedView;
        IntArrayList        ResourceIds;
        int                 Index, Percent;

        internal ReenteringContactsDeleter( IResource view )
        {
            SavedView = view;
        }

        public override string Name
        {
            get { return "Performing cleaning of contacts"; }
        }

        public override void  EnumerationStarting()
        {
            IResourceList  inView = Core.FilterEngine.ExecView( SavedView );
            inView = inView.Intersect( Core.ResourceBrowser.FilterResourceList, true );
            ResourceIds = new IntArrayList( inView.ResourceIds );
            Percent = Index = 0;
        }

        public override void EnumerationFinished() {}
        public override AbstractJob GetNextJob()
        {
            //  test anchor.
            if( Index >= ResourceIds.Count )
                return null;

            int newPercent = Index * 100 / ResourceIds.Count;
            if( newPercent != Percent )
            {
                if( Core.ProgressWindow != null )
                    Core.ProgressWindow.UpdateProgress( newPercent, "Deleting Contacts...", "" );
                Percent = newPercent;
            }

            IResource res = Core.ResourceStore.TryLoadResource( ResourceIds[ Index++ ] );
            if( IsContactUseless( res ) )
            {
                return new DelegateJob( new ResourceDelegate( ContactManager.DeleteContactImpl ), new object[] { res } );
            }
            return GetNextJob();
        }

        /// <summary>
        /// Contact is "useless" if it is not linked to any correspondence
        /// resource via "From", "To" or "CC" links and does not belong to any
        /// non-exportable (that is not a user's or non-modifiable directly)
        /// address book.
        /// </summary>
        private static bool  IsContactUseless( IResource res )
        {
            if ( res != null )
            {
                int count = ContactManager.LinkedCorrespondence( res ).Count;
                if( count == 0 )
                {
                    IResource  addrBook = res.GetLinkProp( "InAddressBook" );
                    if( addrBook == null || !addrBook.HasProp( "IsNonExportable" ) )
                        return true;
                }
            }
            return false;
        }
    }
}
