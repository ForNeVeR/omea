/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.ContactsPlugin
{
    [PluginDescriptionAttribute("Contacts", "JetBrains Inc.", "Contact viewer and editor.", PluginDescriptionFormat.PlainText, "Icons/ContactsPluginIcon.png")]
    public class ContactsPlugin: IPlugin, IResourceDisplayer, IResourceTextProvider
    {
        private const string _tabName = "Contacts";
        private IResourceTreePane _addressBookPane;
        private ColorScheme _colorScheme;
        private static ContactsPlugin _instance;
        private static bool _isReader;

        #region IPlugin Members
        public void Register()
        {
            _instance = this;

            _isReader = Core.ProductFullName.EndsWith( "Reader" );

            _colorScheme = new ColorScheme( Assembly.GetExecutingAssembly(), "ContactsPlugin.Icons.", Core.ResourceIconManager.IconColorDepth );
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "ContactsPlugin.Icons.ContactColorScheme.xml" );
            _colorScheme.Load( stream );

            AddressBook.Initialize();
            Core.ResourceTreeManager.SetViewsExclusive( "Contact" );

            IUIManager uiMgr = Core.UIManager;
            Core.TabManager.RegisterResourceTypeTab( _tabName, _tabName, new[] { "Contact", "AddressBook" }, 10 );
            uiMgr.RegisterResourceSelectPane( "Contact", typeof(CorrespondentCtrl) );
            uiMgr.RegisterResourceSelectPane( "EmailAccount", typeof(EmailAccountSelector) );

            IWorkspaceManager mgr = Core.WorkspaceManager;
            mgr.RegisterWorkspaceType( "Contact", new[] { -Core.ContactManager.Props.LinkFrom, -Core.ContactManager.Props.LinkTo,
                                                          -Core.ContactManager.Props.LinkCC }, WorkspaceResourceType.Filter );

#if !READER
            mgr.RegisterWorkspaceType( "EmailAccount", new[] { -Core.ContactManager.Props.LinkEmailAcctFrom,
                                                               -Core.ContactManager.Props.LinkEmailAcctTo,
                                                               -Core.ContactManager.Props.LinkEmailAcctCC }, WorkspaceResourceType.Filter );
            mgr.RegisterWorkspaceSelectorFilter( "EmailAccount", new EmailAccountFilter() );
            mgr.SetWorkspaceTabName( "EmailAccount", "Email Accounts" );
#endif

            Core.PluginLoader.RegisterViewsConstructor( new ContactsUpgrade1ViewsConstructor() );
            Core.PluginLoader.RegisterViewsConstructor( new ContactsViewsConstructor() );
            Core.PluginLoader.RegisterViewsConstructor( new ContactsUpgrade2ViewsConstructor() );
            Core.PluginLoader.RegisterViewsConstructor( new ContactsUpgrade3ViewsConstructor() );

            //-----------------------------------------------------------------
            //  Register Search Extensions to narrow the list of results using
            //  simple phrases in search queries: for restricting the resource
            //  type to Contact (three synonyms).
            //-----------------------------------------------------------------
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "contacts", "Contact" );
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "contact", "Contact" );
            Core.SearchQueryExtensions.RegisterResourceTypeRestriction( "in", "c", "Contact" );

            IDisplayColumnManager colManager = Core.DisplayColumnManager;
            colManager.RegisterPropertyToTextCallback( Core.ContactManager.Props.LinkFrom, SubstituteName );

            Core.UIManager.RegisterDisplayInContextHandler( "Contact", new DisplayContactInContextHandler() );
            Core.UIManager.RegisterResourceLocationLink( "AddressBook", 0, "AddressBook" );

            Core.PluginLoader.RegisterResourceTextProvider( "Contact", this );
            Core.PluginLoader.RegisterResourceDisplayer( "Contact", this );
            Core.PluginLoader.RegisterResourceSerializer( "Contact", new ContactSerializer() );
            Core.PluginLoader.RegisterResourceSerializer( "Phone", new PhoneSerializer() );
            Core.PluginLoader.RegisterResourceSerializer( "EmailAccount", new EmailAccountSerializer() );

            Core.ActionManager.RegisterLinkClickAction( new DisplayMailsForEmailAccount(), "EmailAccount", null );

            if ( !_isReader )
            {
                IResource abRoot = AddressBook.AddressBookRoot;
                if ( abRoot != null )
                {
                    Assembly theAsm = Assembly.GetExecutingAssembly();
                    Image img = Utils.TryGetEmbeddedResourceImageFromAssembly( theAsm, "ContactsPlugin.Icons.AddressBook24.png" );
                    _addressBookPane = Core.LeftSidebar.RegisterResourceStructureTreePane( "AddressBooks", _tabName, "Address Books", img, "AddressBook" );
                    _addressBookPane.RegisterToolbarAction( new CreateABAction(), LoadIconFromAssembly( "addressbook.ico" ), null, "Create Address Book", null );
                    Core.LeftSidebar.RegisterViewPaneShortcut( "AddressBooks", Keys.Control | Keys.Alt | Keys.D );
                }
            }
            else
            {
                Core.ResourceBrowser.RegisterLinksPaneFilter( "Contact", new ReaderABLinkFilter() );
            }
            Core.ResourceBrowser.RegisterLinksPaneFilter( "Contact", new SkipEmailAddressesFilter() );
            Core.ResourceIconManager.RegisterOverlayIconProvider( "Contact", new ContactOverlayIconProvider() );

            Core.PluginLoader.RegisterResourceDeleter( "Contact", new ContactDeleter() );
            Core.PluginLoader.RegisterResourceDeleter( "ContactName", new ContactDeleter() );

            RegisterContactBlocks();
            Core.ResourceIconManager.RegisterResourceLargeIcon( "Contact", LoadIconFromAssembly( "ContactLarge.ico") );
            Core.ResourceBrowser.RegisterLinksGroup( "Accounts", new[] { Core.ContactManager.Props.LinkEmailAcct }, ListAnchor.First );
        }

        public void Startup()   {}
        public void Shutdown()  {}

        private static Icon LoadIconFromAssembly( string name )
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "ContactsPlugin.Icons." + name );
            return new Icon( stream );
        }
        #endregion

        #region IResourceTextProvider Members
        bool IResourceTextProvider.ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            string body = new ContactBO( res ).ContactBody;
            if ( body != null )
            {
                lock( consumer )
                {
                    consumer.AddDocumentFragment( res.Id, body );
                }
            }
            return true;
        }
        #endregion IResourceTextProvider Members

        private static void RegisterContactBlocks()
        {
            ContactService contactService = ContactService.GetInstance();
            Core.PluginLoader.RegisterPluginService( contactService );

            contactService.RegisterContactEditBlock( 0, ListAnchor.Last, "Name", NameBlock.CreateBlock );
            contactService.RegisterContactEditBlock( 0, ListAnchor.Last, "Email Addresses", EmailBlock.CreateBlock );
            contactService.RegisterContactEditBlock( 1, ListAnchor.Last, "Description", DescriptionBlock.CreateBlock );
            contactService.RegisterContactEditBlock( 1, ListAnchor.Last, "Categories", CategoriesBlock.CreateBlock );

            contactService.RegisterContactEditBlock(ContactTabNames.GeneralTab, ListAnchor.Last, "Name", NameBlock.CreateBlock);
            contactService.RegisterContactEditBlock(ContactTabNames.GeneralTab, ListAnchor.Last, "Email Addresses", EmailBlock.CreateBlock);
            contactService.RegisterContactEditBlock(ContactTabNames.PersonalTab, ListAnchor.Last, "Description", DescriptionBlock.CreateBlock);

            if ( !_isReader )
            {
                contactService.RegisterContactEditBlock(0, ListAnchor.Last, "Phones", PhoneBlock.CreateBlock);
                contactService.RegisterContactEditBlock(1, ListAnchor.Last, "Job", JobBlock.CreateBlock);
                contactService.RegisterContactEditBlock(1, ListAnchor.Last, "Address", AddressBlock.CreateBlock);
                contactService.RegisterContactEditBlock(1, ListAnchor.Last, "Details", DetailsBlock.CreateBlock);

                contactService.RegisterContactEditBlock(ContactTabNames.GeneralTab, ListAnchor.Last, "Phones", PhoneBlock.CreateBlock);
                contactService.RegisterContactEditBlock(ContactTabNames.PersonalTab, ListAnchor.Last, "Job", JobBlock.CreateBlock);
                contactService.RegisterContactEditBlock(ContactTabNames.MailingTab, ListAnchor.Last, "Address", AddressBlock.CreateBlock);
                contactService.RegisterContactEditBlock(ContactTabNames.PersonalTab, ListAnchor.Last, "Details", DetailsBlock.CreateBlock);
            }
        }

        public static IResourceTreePane AddressBookPane
        {
            get { return _instance._addressBookPane; }
        }

        internal static bool IsReader
        {
            get { return _isReader; }
        }

        internal static ColorScheme ColorScheme
        {
            get { return _instance._colorScheme; }
        }

        #region IResourceDisplayer members

        IDisplayPane IResourceDisplayer.CreateDisplayPane( string resourceType )
        {
            if ( String.Compare( resourceType, "Contact", true ) == 0 )
            {
                return new ContactDisplayPane();
            }
            return null;
        }

        #endregion

        #region Actions and Filters
        class EmailAccountFilter: IResourceNodeFilter
        {
            private bool _haveMyself = false;
            private IResource _myselfContact;

            public bool AcceptNode( IResource res, int level )
            {
                if ( !_haveMyself )
                {
                    _myselfContact = Core.ContactManager.MySelf.Resource;
                    _haveMyself = true;
                }

                if ( _myselfContact == null )
                {
                    return true;
                }
                return _myselfContact.HasLink( "EmailAcct", res );
            }
        }

        class ReaderABLinkFilter: ILinksPaneFilter
        {
            public bool AcceptLinkType( IResource displayedResource, int propId, ref string displayName )
            {
                return propId != AddressBook.PropInAddressBook;
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

        internal static string SubstituteName( IResource res, int propId )
        {
            #region Preconditions
            if( res == null )
                throw new ArgumentNullException( "ContactsPlugin -- Input resource is NULL on name substitution." );
            #endregion Preconditions

            ContactManager mgr = Core.ContactManager as ContactManager;
            if ( mgr.IsMajorLink( propId ) )
            {
                IResourceList contacts = res.GetLinksOfType( null, propId );
                IntArrayList contactsIDs = new IntArrayList( contacts.ResourceIds );

                int     linkNameId = mgr.GetNameLinkId( propId );
                IResourceList contactNames = res.GetLinksOfType( "ContactName", linkNameId );
                string[] results = new string [contacts.Count];

                //  In case of any problems or mismatches in the link structure,
                //  fall back to default processing

                if ( contactNames.Count > contactsIDs.Count )
                {
                    return res.GetPropText( propId );
                }
                
                for( int i = 0; i < contactNames.Count; i++ )
                {
                    IResource contactName = Core.ResourceStore.TryLoadResource( contactNames.ResourceIds [ i ] );
                    if ( contactName == null )
                    {
                        results [i] = "";
                        continue;
                    }
                    IResource contact = contactName.GetLinkProp( "BaseContact" );
                    if( contact == null )
                        return res.GetPropText( propId );

                    int index = contactsIDs.IndexOf( contact.Id );
                    if ( index < 0 )
                    {
                        return res.GetPropText( propId );
                    }
                    contactsIDs.RemoveAt( index );
                    if ( contact.HasProp( Core.ContactManager.Props.ShowOriginalNames ) )
                    {
                        results [i] = contactName.GetStringProp( "Name" );
                    }
                    else
                    {
                        results [i] = contact.DisplayName;
                    }
                }

                for( int i = 0; i < contactsIDs.Count; i++ )
                {
                    IResource majorLinkResource = Core.ResourceStore.TryLoadResource( contactsIDs [ i ] );
                    if ( majorLinkResource != null )
                    {
                        results [ i + contactNames.Count] = majorLinkResource.DisplayName;
                    }
                    else
                    {
                        results [ i + contactNames.Count] = "";
                    }
                }

                return String.Join( ", ", results );
            }

            return res.GetPropText( propId );
        }

        class SkipEmailAddressesFilter : ILinksPaneFilter
        {
            public bool AcceptLinkType(IResource res, int propId, ref string displayName)
            {
                return (res.Type != "Contact") ||
                       (( propId != Core.ContactManager.Props.LinkEmailAcct ) &&
                        !((ContactManager) Core.ContactManager).IsMajorLink( Math.Abs( propId ) ));
            }

            public bool AcceptLink( IResource sourceRes, int propId, IResource targetRes, ref string linkTooltip)
            {
                return true;
            }

            public bool AcceptAction(IResource displayedResource, IAction action)
            {
                return true;
            }
        }

        internal class AddContactToABAction : IRuleAction
        {
            public void Exec(IResource res, IActionParameterStore actionStore)
            {
                IResourceList authors = res.GetLinksOfType( "Contact", Core.ContactManager.Props.LinkFrom );
                foreach( IResource contact in authors )
                {
                    IResourceList addrBooks = actionStore.ParametersAsResList();
                    foreach( IResource addrBook in addrBooks )
                    {
                        new AddressBook( addrBook ).AddContact( contact );
                    }
                }
            }
        }

        #endregion Actions and Filters

        #region ContactDeleter
        internal class ContactDeleter: DefaultResourceDeleter
        {
            private bool    SavedIgnoranceValue = true;

            public override bool CanDeleteResource( IResource res, bool permanent )
            {
                return !permanent && ( res == null || !res.HasProp( "MySelf" ) );
            }

	        public override DialogResult ConfirmDeleteResources( IResourceList list, bool permanent, bool showCancel )
	        {
                int  fromCount, toCount, ccCount;
                CountCorrespondenceCounts( list, out fromCount, out toCount, out ccCount );

                string message = string.Empty;

                if( fromCount + toCount + ccCount > 0 )
                {
                    if( fromCount > 0 )
                        message += fromCount + " outcoming items";
                    if( toCount > 0 )
                    {
                        if( message.Length > 0 )
                        {
                            if( ccCount > 0 )
                                message += ", ";
                            else
                                message += " and ";
                        }
                        message += toCount + " incoming items";
                    }
                    if( ccCount > 0 )
                    {
                        if( message.Length > 0 )
                            message += " and ";
                        message += ccCount + " Carbon Copy items";
                    }
                    string  prefix;
                    if( list.Count == 1 )
                        prefix = "A contact \'" + list[ 0 ].DisplayName + "\' has ";
                    else
                        prefix = "Selected contacts have ";
                    message = prefix + message + ".  These items will be moved to Deleted Resources. ";
                }

	            message += "Are you sure you wish to delete ";
                if( list.Count == 1 )
                    message += "'" + list[ 0 ].DisplayName + "'?";
                else
                    message += list.Count + " contacts?";

                MessageBoxWithCheckBox.Result result;
                result = MessageBoxWithCheckBox.ShowYesNo( Core.MainWindow, message, "Delete Contact",
                                                           "&Ignore incoming correspondence from this contact", true );
                SavedIgnoranceValue = result.Checked;
                return (result.IdPressedButton == (int)DialogResult.Yes) ? DialogResult.Yes : DialogResult.No;
	        }

	        public override void UndeleteResource( IResource res )
	        {
	            base.UndeleteResource( res );
                new ResourceProxy( res ).DeleteProp( Core.ContactManager.Props.Ignored );
	        }

            //-----------------------------------------------------------------
            //  Method performs only "soft" contact removal - it never allows
            //  to delete a resource permanently.
            //-----------------------------------------------------------------
            public override void DeleteResource( IResource res )
            {
                #region Preconditions
                if ( res == null )
                    throw new ArgumentNullException( "ContactManager -- Contact for deletion can not be NULL" );
                if( res.Type != "Contact" && res.Type != "ContactName" )
                    throw new ArgumentNullException( "ContactManager -- Contact for deletion has illegal type [" + res.Type + "]" );
                #endregion Preconditions

                if( res.Type == "ContactName" )
                    res = res.GetLinkProp( Core.ContactManager.Props.LinkBaseContact );

                string errMsg;
                Core.ContactManager.DeleteContact( res, SavedIgnoranceValue, out errMsg );
                if( errMsg != null )
                {
                    Core.UIManager.ShowSimpleMessageBox( "Contact Deletion Failed", errMsg );
                }
                else
                {
                    //  And do not forget to perform default actions with the
                    //  resource.
                    base.DeleteResource( res );
                }
            }

            //-----------------------------------------------------------------
            //  This method is overriden in order to forbid the permanent
            //  deletion of contacts.
            //-----------------------------------------------------------------
	        public override void DeleteResourcePermanent( IResource res )
	        {
                //  Nothing to do.
	        }

            private static void CountCorrespondenceCounts( IResourceList list, out int fromCount, out int toCount, out int ccCount )
            {
                fromCount = toCount = ccCount = 0;
                for( int i = 0; i < list.Count; i++ )
                {
                    IResource res = list[ i ];
                    if( res.Type == "ContactName" )
                        res = res.GetLinkProp( Core.ContactManager.Props.LinkBaseContact );

                    fromCount += res.GetLinksOfType( null, Core.ContactManager.Props.LinkFrom ).Count;
                    toCount += res.GetLinksOfType( null, Core.ContactManager.Props.LinkTo ).Count;
                    ccCount += res.GetLinksOfType( null, Core.ContactManager.Props.LinkCC ).Count;
                }
            }
        }
        #endregion ContactDeleter

        /// <summary>
        /// Decorate a contact with "Address Book" sign if it is defined
        /// in some Address Book.
        /// </summary>
        private class ContactOverlayIconProvider: IOverlayIconProvider
        {
            private readonly Icon[] _overlaySign = new Icon[ 1 ];

            public ContactOverlayIconProvider()
            {
                _overlaySign[ 0 ] = LoadIconFromAssembly( "InABoverlay.ico" );
            }

            public Icon[] GetOverlayIcons( IResource res )
            {
                return (res.HasProp( "InAddressBook" ) ? _overlaySign : null);
            }
        }
	}

    internal class DisplayContactInContextHandler : IDisplayInContextHandler
    {
        public void DisplayResourceInContext( IResource res )
        {
            Core.UIManager.BeginUpdateSidebar();
            if ( !Core.TabManager.ActivateTab( "Contacts" ) )
            {
                return;
            }
            
            IResource addressBook = res.GetLinkProp( AddressBook.PropInAddressBook );
            if ( addressBook != null )
            {
                Core.LeftSidebar.ActivateViewPane( "AddressBooks" );
                Core.UIManager.EndUpdateSidebar();
                AbstractViewPane pane = Core.LeftSidebar.GetPane( "AddressBooks" );
                if ( pane != null )
                {
                    pane.SelectResource( addressBook, false );
                }
            }
            else
            {
                IResource allView = Core.ResourceStore.FindUniqueResource( "SearchView", Core.Props.Name, "All" );
                if ( allView != null )
                {
                    Core.LeftSidebar.ActivateViewPane( StandardViewPanes.ViewsCategories );
                    Core.UIManager.EndUpdateSidebar();
                    Core.LeftSidebar.DefaultViewPane.SelectResource( allView );
                }
            }

            Core.ResourceBrowser.SelectResource( res );
        }
    }
}
