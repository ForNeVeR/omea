// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using JetBrains.Omea.Categories;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Contacts
{
    #region IContactManagerProps
    internal class ContactManagerProps: IContactManagerProps
    {
        // links from email to contacts
        private readonly int   _linkFrom, _linkTo, _linkCC;

        //  email account properties
        private readonly int _linkEmailAcct;        // from contacts to accounts
        private readonly int _linkEmailAccountFrom; // from emails to accounts
        private readonly int _linkEmailAccountTo;
        private readonly int _linkEmailAccountCC;

        private readonly int _propEmailAddress;
        private readonly int _propUserAccount, _propDomain;
        private readonly int _propIsPersonalAccount;
        private readonly int _propShowOriginalNames;

        //  Contact Name resource type, links and properties
        //  Links "NameFrom", "NameTo", "NameCC" link from e-mail (article, ect)
        //  to the particularly used name. ContactName_s are linked to the
        //  actual contact by the link "Contact".
        private readonly int _linkNameFrom, _linkNameTo, _linkNameCC;
        private readonly int _linkBaseContact;

        //  This link connects a contact and a those resource types which
        //  represent linked correspondence resources.
        private readonly int _linkLinkedResourceTypes;

        private readonly int _propLastCorrespondenceDate;

        private readonly int _propMySelf;
        private readonly int _propIgnored;
        private readonly int _propPictureThumbnail;
        private readonly int _propPictureOriginal;


        internal ContactManagerProps( IResourceStore store )
        {
            //  "From", "To" and "CC" link Email with Contact
            _linkFrom = ResourceTypeHelper.UpdatePropTypeRegistration( "From", PropDataType.Link, PropTypeFlags.DirectedLink);
            _linkTo = ResourceTypeHelper.UpdatePropTypeRegistration( "To", PropDataType.Link, PropTypeFlags.DirectedLink);
            _linkCC = ResourceTypeHelper.UpdatePropTypeRegistration( "CC", PropDataType.Link, PropTypeFlags.DirectedLink);

            store.PropTypes.RegisterDisplayName( _linkFrom, "From", "Sent" );
            store.PropTypes.RegisterDisplayName( _linkTo, "To", "Received" );
            store.PropTypes.RegisterDisplayName( _linkCC, "CC", "Received CC" );

            _propIgnored = store.PropTypes.Register( "IsIgnored", PropDataType.Bool, PropTypeFlags.Internal );
            _propMySelf = store.PropTypes.Register( "MySelf", PropDataType.Int, PropTypeFlags.Internal );
            _propLastCorrespondenceDate = store.PropTypes.Register( "LastCorrespondDate", PropDataType.Date );
            store.PropTypes.RegisterDisplayName( _propLastCorrespondenceDate, "Last Correspondence Date" );

            _propIsPersonalAccount = store.PropTypes.Register("IsPersonalAccount", PropDataType.Bool, PropTypeFlags.Internal);
            _propShowOriginalNames = store.PropTypes.Register( "ShowOriginalNames", PropDataType.Bool, PropTypeFlags.Internal );
            _propPictureThumbnail = store.PropTypes.Register( "PictureThumbnail", PropDataType.Blob, PropTypeFlags.Internal );
            _propPictureOriginal = store.PropTypes.Register( "ContactOriginalPicture", PropDataType.Blob, PropTypeFlags.Internal );

            //  "EmailAcct" links Contact or ContactName with EMailAccount
            _linkEmailAcct = store.PropTypes.Register( "EmailAcct", PropDataType.Link, PropTypeFlags.ContactAccount );
            //  "EmailAccountFrom", "EmailAccountTo" and "EmailAccountCC" link Email with EMailAccount
            _linkEmailAccountFrom = ResourceTypeHelper.UpdatePropTypeRegistration( "EmailAccountFrom", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
            _linkEmailAccountTo = ResourceTypeHelper.UpdatePropTypeRegistration( "EmailAccountTo", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
            _linkEmailAccountCC = ResourceTypeHelper.UpdatePropTypeRegistration( "EmailAccountCC", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );

            _propEmailAddress = store.PropTypes.Register( "EmailAddress", PropDataType.String );
            _propUserAccount = store.PropTypes.Register( "UserAccount", PropDataType.String, PropTypeFlags.Internal );
            _propDomain = store.PropTypes.Register( "Domain", PropDataType.String, PropTypeFlags.Internal );
            store.PropTypes.RegisterDisplayName( _linkEmailAcct, "E-mail Address" );

            store.ResourceTypes.Register( "EmailAccount", "Email Account", "EmailAddress", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            store.RegisterUniqueRestriction( "EmailAccount", _propEmailAddress );

            //--  ContactName, its links and properties -----------------------
            _linkNameFrom = store.PropTypes.Register( "NameFrom", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
            _linkNameTo   = store.PropTypes.Register( "NameTo",   PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
            _linkNameCC   = store.PropTypes.Register( "NameCC",   PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );

            _linkBaseContact = store.PropTypes.Register( "BaseContact",PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
            if( !store.ResourceTypes.Exist( "ContactName" ) )
                store.ResourceTypes.Register( "ContactName", "Contact Name", "Name|BaseContact", ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );
            else
            {
                store.ResourceTypes[ "ContactName" ].ResourceDisplayNameTemplate = "Name|BaseContact";
                store.ResourceTypes[ "ContactName" ].Flags = ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal;
            }
            store.RegisterLinkRestriction( "ContactName", _linkBaseContact, "Contact", 1, 1 );

            _linkLinkedResourceTypes = store.PropTypes.Register( "LinkedResourcesOfType", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );

            Core.ResourceBrowser.RegisterLinksGroup( "Addresses", new int[] { LinkFrom, LinkTo, LinkCC }, ListAnchor.First );
        }

        public int LinkFrom         {  get { return _linkFrom; }  }
        public int LinkTo           {  get { return _linkTo;   }  }
        public int LinkCC           {  get { return _linkCC;   }  }
        public int LinkEmailAcct    {  get { return _linkEmailAcct; } }
        public int LinkEmailAcctFrom{  get { return _linkEmailAccountFrom; } }
        public int LinkEmailAcctTo  {  get { return _linkEmailAccountTo; } }
        public int LinkEmailAcctCC  {  get { return _linkEmailAccountCC; } }

        public int EmailAddress     {  get { return _propEmailAddress; } }
        public int UserName         {  get { return _propUserAccount; } }
        public int Domain           {  get { return _propDomain; } }
        public int PersonalAccount  {  get { return _propIsPersonalAccount; } }
        public int ShowOriginalNames{  get { return _propShowOriginalNames; } }

        public int LinkNameFrom     {  get { return _linkNameFrom; }  }
        public int LinkNameTo       {  get { return _linkNameTo; }  }
        public int LinkNameCC       {  get { return _linkNameCC; }  }
        public int LinkBaseContact  {  get { return _linkBaseContact; }  }

        public int LinkLinkedOfType {  get { return _linkLinkedResourceTypes; }  }

        public int Ignored          {  get { return _propIgnored; } }
        public int Myself           {  get { return _propMySelf;  } }
        public int LastCorrespondenceDate { get {  return _propLastCorrespondenceDate;  } }

        public int Picture          {  get { return _propPictureThumbnail;    }  }
        public int PictureOriginal  {  get { return _propPictureOriginal;     }  }
    }
    #endregion IContactManagerProps

    //-------------------------------------------------------------------------
    public class ContactManager: IContactManager
    {
        private const string                cFieldsDelimiter = "; ";
        private static IResourceStore       RStore;
        private static readonly string[]    ContactNativeProps =
        {
            "Title", "FirstName", "MiddleName", "LastName", "Suffix", "Specificator",
            "JobTitle", "Address", "Company", "Description", "HomePage", "Birthday",
            "MySelf", "LastCorrespondDate", "Imported"
        };

        private readonly ContactManagerProps _props;

        #region PropertiesDeclaration

        //contact properties
        public static int _propTitle;
        public static int _propFirstName;
        public static int _propMiddleName;
        public static int _propLastName;
        public static int _propSuffix;
        public static int _propSpecificator;

        public static int _propJobTitle;
        public static int _propCompany;
        public static int _propAddress;
        public static int _propHomePage;
        public static int _propBirthday;
        public static int _propDescription;

        //phone properties
        public static int _propPhoneName;
        public static int _propPhoneNumber;
        public static int _propPhone; // link from contact

        public static int _propImported;
        public static int _propTransferred;
        public static int _propDefaultAccount;
        /// <summary>
        /// Property controls the origin of the account. It is set in two cases:
        /// 1. user created the contact by himself;
        /// 2. user has recieved the contact as resource by mail.
        /// Setting this property to <b>true</b> allows to delete the corresponding
        /// property if it has no linked mails.
        /// </summary>
        public static int _propUserCreated;

        //  relations between merged contact and its descendants
        public static int _propSerializationBlob;
        public static int _propSerializationBlobLink;

        public static int   _propResourceAttach;
        public static int   _linkDeletedToRecycleBin;

        private ContactBO       _mySelfContact;
        private IResourceList   _myselfAccounts;
        private IResourceList   _MySelfTrackingList;

        private static bool IsTraceSuppressed;
        private static IResource OperationalSplittedContact;

        public int PropFrom         {  get { return Props.LinkFrom; }   }
        public int PropTo           {  get { return Props.LinkTo;   }   }
        public int PropCC           {  get { return Props.LinkCC;   }   }
        public int PropEmailAcct    {  get { return Props.LinkEmailAcct; }  }
        public int PropEmailAddress {  get { return Props.EmailAddress; }   }

        private ArrayList _contactMergeFilters = new ArrayList();
        #endregion PropertiesDeclaration

        #region Ctor and Initialization
        public ContactManager( IResourceStore resourceStore )
        {
            _props = new ContactManagerProps( resourceStore );
            _myselfAccounts = resourceStore.EmptyResourceList;
            RegisterContactTypes( resourceStore );
        }
        public void  Initialize()
        {
            RegisterFilterForFormattedResources();
        }

        public IContactManagerProps Props
        {
            get { return _props; }
        }
        #endregion Ctor and Initialization

        #region Properties Definition
        private static void RegisterContactTypes( IResourceStore resourceStore )
        {
            RStore = resourceStore;

            _propTitle = RStore.PropTypes.Register( "Title", PropDataType.String );
            _propFirstName = RStore.PropTypes.Register( "FirstName", PropDataType.String );
            _propMiddleName = RStore.PropTypes.Register( "MiddleName", PropDataType.String );
            _propLastName = RStore.PropTypes.Register( "LastName", PropDataType.String );
            _propSuffix = RStore.PropTypes.Register( "Suffix", PropDataType.String );
            _propSpecificator = RStore.PropTypes.Register( "Specificator", PropDataType.String, PropTypeFlags.Internal );
            RStore.PropTypes.RegisterDisplayName( _propFirstName, "First Name" );
            RStore.PropTypes.RegisterDisplayName( _propMiddleName, "Middle Name" );
            RStore.PropTypes.RegisterDisplayName( _propLastName, "Last Name" );

            _propJobTitle = RStore.PropTypes.Register( "JobTitle", PropDataType.String, PropTypeFlags.Internal );
            _propCompany = RStore.PropTypes.Register( "Company", PropDataType.String, PropTypeFlags.Internal );
            _propAddress = RStore.PropTypes.Register( "Address", PropDataType.String, PropTypeFlags.Internal );
            _propHomePage = RStore.PropTypes.Register( "HomePage", PropDataType.String, PropTypeFlags.Internal );
            _propBirthday = RStore.PropTypes.Register( "Birthday", PropDataType.Date );
            _propDescription = RStore.PropTypes.Register( "Description", PropDataType.String, PropTypeFlags.Internal );

            _propImported = RStore.PropTypes.Register( "Imported", PropDataType.Int, PropTypeFlags.Internal );
            _propUserCreated = RStore.PropTypes.Register( "UserCreated", PropDataType.Bool, PropTypeFlags.Internal );
            _propTransferred = RStore.PropTypes.Register( "MailTranferred", PropDataType.Bool, PropTypeFlags.Internal );

            _propDefaultAccount = RStore.PropTypes.Register( "DefaultAccountLink", PropDataType.Link, PropTypeFlags.Internal );

            RStore.ResourceTypes.Register( "MailingList", "Mailing List", "EmailAcct", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );

            _propPhoneName = RStore.PropTypes.Register( "PhoneName", PropDataType.String, PropTypeFlags.Internal );
            _propPhoneNumber = RStore.PropTypes.Register( "PhoneNumber", PropDataType.String, PropTypeFlags.Internal );
            _propPhone = RStore.PropTypes.Register( "Phone", PropDataType.Link, PropTypeFlags.Internal );
            RStore.ResourceTypes.Register( "Phone", "Phone", "PhoneName PhoneNumber", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            ContactBO.PhonesCleanUp();

            RStore.ResourceTypes.Register( "ContactSerializationBlobKeeper", "", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            _propSerializationBlob = RStore.PropTypes.Register( "SerializationBlob", PropDataType.Blob, PropTypeFlags.Internal );
            _propSerializationBlobLink = ResourceTypeHelper.UpdatePropTypeRegistration( "SerializationBlobLink", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal);

            _propResourceAttach = RStore.PropTypes.Register( "ResourceAttachment", PropDataType.Link, PropTypeFlags.DirectedLink );
            RStore.PropTypes.RegisterDisplayName( _propResourceAttach, "Resource Attachment", "Received with" );

            if( !RStore.ResourceTypes.Exist( "Contact" ) )
                RStore.ResourceTypes.Register( "Contact", "Title FirstName MiddleName LastName Suffix Specificator| EmailAcct" );

            if( RStore.PropTypes.Exist( "SenderName" ))
            {
                IPropType pt = RStore.PropTypes[ "SenderName" ];
                RStore.PropTypes.Delete( pt.Id );
            }

            IsTraceSuppressed = Core.SettingStore.ReadBool( "Contacts", "SuppressTraces", false );

            ResourceTypeHelper.UpdatePropTypeRegistration( "EmailAccountFrom", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
            ResourceTypeHelper.UpdatePropTypeRegistration( "EmailAccountTo", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
            ResourceTypeHelper.UpdatePropTypeRegistration( "EmailAccountCC", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
        }

        private static void  RegisterFilterForFormattedResources()
        {
            IResourceTypeCollection types = Core.ResourceStore.ResourceTypes;
            foreach( IResourceType type in types )
            {
                if( (type.Flags | ResourceTypeFlags.FileFormat) > 0 )
                    Core.ResourceBrowser.RegisterLinksPaneFilter( type.Name, new ItemRecipientsFilter() );
            }
        }
        #endregion Properties Definition

        #region MergeContacts
        public IResource Merge( string fullName, IResourceList contacts )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( fullName ))
                throw new ArgumentNullException( "fullName", "Full name can not be empty" );
            #endregion Preconditions

            string title, firstName, midName, lastName, suffix, addSpec;
            bool result = ContactResolver.ResolveName( fullName, null, out title,
                                                       out firstName, out midName,
                                                       out lastName, out suffix, out addSpec );
            Debug.Assert( result );

            return Merge( title, firstName, midName, lastName, suffix, addSpec, contacts );
        }
        public IResource Merge( string firstName, string lastName, IResourceList contacts )
        {
            return Merge( null, firstName, null, lastName, null, null, contacts );
        }

        public IResource Merge( string title, string firstName, string midName,
                                string lastName, string suffix, string addSpec,
                                IResourceList contacts )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( firstName ) && String.IsNullOrEmpty( lastName ))
                throw new ArgumentNullException( "firstName", "First name and Last name can not be empty simultaneously" );

            if( contacts.Count < 2 )
                throw new InvalidOperationException( "Merge is possible with number of contacts >= 2" );
            #endregion Preconditions

            ContactBO targetContact = CreateContactImpl( title, firstName, midName, lastName, suffix );
            if( Core.ProgressWindow != null )
                Core.ProgressWindow.UpdateProgress( 0, "Merging contacts...", "" );
            targetContact.Resource.BeginUpdate();
            int counter = 0;
            foreach( IResource res in contacts )
            {
                ContactBO contact = new ContactBO( res );

                //-------------------------------------------------------------
                //  All links from this contact to its serialized siblings will
                //  be duplicated to the target contact. So there is no need
                //  to keep this contact (and its resource) alive. Thus we
                //  maintain flat structure of serialized contacts.
                //-------------------------------------------------------------
                if( !contact.IsSerializationContainer )
                    SerializeContact( contact.Resource, targetContact.Resource );

                int condCount = contact.Resource.GetLinksOfType( null, "LinkedSetValue" ).Count;
                Trace.WriteLineIf( !IsTraceSuppressed, "ContactManager -- Resource [" + contact.Resource.DisplayName + "] has " + condCount + " links to conditions before Merge" );

                MergeData( contact, targetContact );

                //  Correct the date of the last correspondence
                if( contact.LastCorrespondDate > targetContact.LastCorrespondDate )
                    targetContact.LastCorrespondDate = contact.LastCorrespondDate;

                if( contact.IsMyself )
                    targetContact.SetMyself();
                if( Core.ProgressWindow != null )
                    Core.ProgressWindow.UpdateProgress( (int)(counter * 100.0 / contacts.Count), "Merging contacts...", "" );
            }
            targetContact.Resource.EndUpdate();
            targetContact.QueueIndexing();

            //  Do not forget to remove these contact after they are
            //  already saved (serialized)
            foreach( IResource res in contacts )
                res.Delete();

            if( Core.ProgressWindow != null )
                Core.ProgressWindow.UpdateProgress( 100, "Merging...", "" );
            return targetContact.Resource;
        }

        private static void SerializeContact( IResource contact, IResource root )
        {
            Stream strm = ResourceBinarySerialization.Serialize( contact );
            IResource blobKeeper = RStore.BeginNewResource( "ContactSerializationBlobKeeper" );
            blobKeeper.SetProp( _propSerializationBlob, strm );
            blobKeeper.DisplayName = contact.DisplayName;
            blobKeeper.EndUpdate();
            root.AddLink( _propSerializationBlobLink, blobKeeper );
        }

        internal void  MergeData( ContactBO contact, ContactBO newContact )
        {
            contact.Resource.BeginUpdate();
            newContact.Resource.BeginUpdate();

            DateTime lastCorrDate = newContact.LastCorrespondDate;
            DateTime dt = contact.LastCorrespondDate;

            if( contact.Address.Length > 0 ) //  avoid non-necessary assignment
                newContact.Address = CatenateString( newContact.Address, contact.Address );
            if( contact.Company.Length > 0 ) //  avoid non-necessary assignment
                newContact.Company = CatenateString( newContact.Company, contact.Company );
            if( contact.JobTitle.Length > 0 ) //  avoid non-necessary assignment
                newContact.JobTitle = CatenateString( newContact.JobTitle, contact.JobTitle );
            if( contact.Description.Length > 0 ) //  avoid non-necessary assignment
                newContact.Description = CatenateString( newContact.Description, contact.Description );
            if(( dt > lastCorrDate || newContact.HomePage == string.Empty ) && contact.HomePage != string.Empty )
                newContact.HomePage = contact.HomePage;

            MergePhones( contact, newContact );
            RetargetContactNameLinks( contact.Resource, newContact.Resource );
            RelinkExternals( contact.Resource, newContact.Resource );
            ReassignPropertiesAndDirectLinks( contact.Resource, newContact.Resource );

            contact.Resource.EndUpdate();
            newContact.Resource.EndUpdate();
        }

        //  Catenate new string only if it is non empty and not contained already
        //  in the existing one. Add delimiter only if catenation is necessary.
        //  NB: assume that string can not be null since they are coming as text
        //      properties from IContact which returns "" in such cases.
        private static string CatenateString( string existing, string newString )
        {
            if( newString.Length == 0 )
                return existing;

            //  If the content of the string contains our delimiter inside,
            //  our separation will be errorneous, but we can not forecast
            //  the presence of any particular delimiter until we use
            //  StringList property for such string.
            string[] fields = Utils.SplitString( existing, cFieldsDelimiter );
            foreach( string str in fields )
            {
                if( str.Trim() == newString )
                    return existing;
            }

            string result = existing;
            if( result.Length > 0 )
                result += cFieldsDelimiter;
            result += newString;
            return result;
        }

        private static void  MergePhones( IContact contact, ContactBO newContact )
        {
            string[] phoneNames = contact.GetPhoneNames();
            foreach( string name in phoneNames )
            {
                string oldNumber = contact.GetPhoneNumber( name );
                string newNumber = newContact.GetPhoneNumber( name );

                //  if there is no telephone with such name, or name exists
                //  but the phone number is empty (such behavior is done inside
                //  PhoneBlock.cs when analyzing the content of controls in the
                //  dialog) - then assign new phone; otherwise find suitable
                //  numeric extension for the new phone and append it.

                if( newNumber == string.Empty )
                {
                    newContact.SetPhoneNumber( name, oldNumber );
                }
                else
                if( oldNumber != string.Empty )
                {
                    if( ContactBO.NormalizedPhoneNumber( newNumber ) !=
                        ContactBO.NormalizedPhoneNumber( oldNumber ) )
                    {
                        string  newName = ComposeSuitablePhoneName( newContact, name );
                        newContact.SetPhoneNumber( newName, oldNumber );
                    }
                }
            }
        }

        public static string ComposeSuitablePhoneName( ContactBO contact, string basename )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( basename ))
                throw new ArgumentNullException( "basename", "PhoneNameConstruction -- base name for a phone is null" );
            #endregion Preconditions

            for( int i = 2;; i++ )
            {
                string  pattern = basename + "(" + i + ")";
                if( !contact.IsPhoneNameExists( pattern ) )
                    return pattern;
            }
        }

        //---------------------------------------------------------------------
        //  Enumerate all properties which are not "native" for the Contact
        //  class, that is the properties which are set by other parties to the
        //  contact resource. These properties are to be propagated to the
        //  target Contact.
        //  NB: 1. Links from the contact to other resources are also added to
        //         the target contact.
        //      2. Contacts may be linked between each other with undirected
        //         links or with directed ones (when dest is merged first). This
        //         leads to cyclic links when merging source and dest contacts.
        //---------------------------------------------------------------------
        private static void ReassignPropertiesAndDirectLinks( IResource contact, IResource newContact )
        {
            //  Copy all properties from the contact since some of them may
            //  change during relinking.
            ArrayList props = ArrayListPool.Alloc();
            try
            {
                foreach( IResourceProperty prop in contact.Properties )
                    props.Add( prop );
                foreach( IResourceProperty prop in props )
                {
                    //  IResource.Properties returns also incoming links and
                    //  marks them with negative Id. Do not process them here.
                    //  For outcoming links - next execution block

                    if( prop.PropId >= 0 && prop.DataType != PropDataType.Link &&
                        Array.IndexOf( ContactNativeProps, prop.Name ) == -1 )
                    {
                        //  First, do not forget to remove property from the
                        //  original contact since for some of them there may
                        //  exist a uniqueness restriction.
                        contact.DeleteProp( prop.PropId );
                        newContact.SetProp( prop.PropId, prop.Value );
                    }
                    else
                        if( prop.PropId >= 0 && prop.DataType == PropDataType.Link )
                    {
                        IResourceList list = contact.GetLinksOfType( null, prop.PropId );
                        string name = Core.ResourceStore.PropTypes[ prop.PropId ].Name;
                        Debug.Assert( list.Count > 0, "Link " + name + " exists but no linked resource is returned" );

                        //  We have to explicitely remove links from the contact (and
                        //  not when we just delete a contact as whole) beforehand,
                        //  because we need to conform some link restrictions, e.g. link
                        //  "TO" from ICQConversation must have only one recipient.
                        contact.DeleteLinks( prop.PropId );

                        for( int i = 0; i < list.Count; i++ )
                        {
                            //  Avoid cyclic links.
                            if( list[ i ].Id != newContact.Id )
                                newContact.AddLink( prop.PropId, list[ i ] );
                        }
                    }
                }
            }
            finally
            {
                ArrayListPool.Dispose( props );
            }
        }

        //---------------------------------------------------------------------
        //  Retarget links from ContactNames to the new contact as separate
        //  phase. This allows us to simplify other code.
        //---------------------------------------------------------------------
        private static void RetargetContactNameLinks( IResource contact, IResource newContact )
        {
            IResourceList linkedContactNames = contact.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
            foreach( IResource contactName in linkedContactNames )
                contactName.SetProp( Core.ContactManager.Props.LinkBaseContact, newContact );
        }

        //---------------------------------------------------------------------
        //  Enumerate all links coming TO the contact, and independently of
        //  their type - duplicate them to the target contact.
        //  For resource-correspondent links (From, To, CC) - create ContactName
        //  resources is necessary.
        //  NB:  Avoid links between merged contacts (e.g. custom links).
        //---------------------------------------------------------------------
        private void RelinkExternals( IResource contact, IResource newContact )
        {
            //  Copy all properties from the contact since some of them may
            //  change during relinking.
            ArrayList props = ArrayListPool.Alloc();
            try
            {
                foreach( IResourceProperty prop in contact.Properties )
                    props.Add( prop );
                foreach( IResourceProperty prop in props )
                {
                    //  Negative PropId - incoming links - exactly what we are looking for
                    IPropType type = RStore.PropTypes[ prop.PropId ];
                    if( prop.PropId < 0 && type.DataType == PropDataType.Link &&
                        ((type.Flags & PropTypeFlags.DirectedLink) > 0 ) )
                    {
                        RetargetLinks( contact, newContact, -prop.PropId );
                    }
                }
            }
            finally
            {
                ArrayListPool.Dispose( props );
            }
        }

        private void RetargetLinks( IResource source, IResource target, int linkId )
        {
            IResourceList list = source.GetLinksOfType( null, linkId );
            foreach( IResource res in list )
            {
                //  Avoid cyclic links - links between merged contacts.
                if( res.Id != target.Id && !res.HasLink( linkId, target ) )
                {
                    res.BeginUpdate();
                    if( IsMajorLink( linkId ) && !HasContactNameAlready( res, linkId, target ) )
                    {
                        string fullName = new ContactBO( source ).FullName;

                        int accntLinkId = GetAccountLinkId( linkId );
                        IResourceList contactAcc = source.GetLinksOfType( "EmailAccount", Props.LinkEmailAcct );
                        IResourceList emailAccnts = res.GetLinksOfType( "EmailAccount", accntLinkId );
                        contactAcc = contactAcc.Intersect( emailAccnts, true );

                        CreateAndLinkContactName( target, res, (contactAcc.Count > 0)? contactAcc[ 0 ] : null, linkId, fullName );
                    }
                    res.DeleteLink( linkId, source );
                    res.AddLink( linkId, target );
                    res.EndUpdate();
                }
            }
        }

        private bool HasContactNameAlready( IResource res, int linkId, IResource target )
        {
            int       nameLinkId = GetNameLinkId( linkId );
            IResource cName = res.GetLinkProp( nameLinkId );
            if ( cName != null )
            {
                IResource baseContact = cName.GetLinkProp( Props.LinkBaseContact );
                if ( baseContact != null )
                {
                    return baseContact.Id == target.Id;
                }
            }
            return false;
        }

        /// <summary>
        /// Find all other contacts which are linked to the same e-mail accounts
        /// as the given one.
        /// </summary>
        /// <param name="contact">Source contact, which accounts are taken for
        /// list construction</param>
        /// <returns>List of contact resources</returns>
        public static IResourceList GetContactsForMerging( IResource contact )
        {
            IResourceList feasibleContacts = RStore.EmptyResourceList;
            int[] typeIDs = contact.GetLinkTypeIds();
            foreach ( int typeID in typeIDs )
            {
                if( RStore.PropTypes[ typeID ].HasFlag( PropTypeFlags.ContactAccount ) )
                {
                    IResourceList accounts = contact.GetLinksOfType( null, typeID );
                    foreach ( IResource account in accounts )
                    {
                        feasibleContacts = feasibleContacts.Union(
                            account.GetLinksOfType( "Contact", typeID ), true );
                    }
                }
            }
            return feasibleContacts.Minus( contact.ToResourceList() );
        }

        public void RegisterContactMergeFilter( IContactMergeFilter filter )
        {
            _contactMergeFilters.Add( filter );
        }

        public IContactMergeFilter[] GetContactMergeFilters()
        {
            return (IContactMergeFilter[]) _contactMergeFilters.ToArray( typeof (IContactMergeFilter) );
        }
        #endregion MergeContacts

        #region SplitContacts
        //---------------------------------------------------------------------
        //  Split the given contact. Extract not all subcontacts but only those
        //  which are given in the second parameter <keepersToExtract>.
        //  Comment: Result list of extracted contacts contains also the
        //           source contact.
        //---------------------------------------------------------------------
        public IResourceList  Split( IResource contact, IResourceList keepersToExtract )
        {
            #region Preconditions
            IResourceList allContactKeepers = contact.GetLinksOfType( "ContactSerializationBlobKeeper", _propSerializationBlobLink );
            foreach( IResource res in keepersToExtract )
            {
                if( allContactKeepers.IndexOf( res.Id ) == -1 )
                    throw new ArgumentException( "ContactManager -- not all contacts in the input list are the linked subcontacts of the Source Contact" );
            }
            #endregion Preconditions

            //  Check whether we extract all subcontacts.
            IResourceList extractedContacts;
            if( keepersToExtract.Count == allContactKeepers.Count )
                extractedContacts = Split( contact );
            else
            {
                IResourceList coveredCategories;
                extractedContacts = SplitInternal( contact, keepersToExtract, out coveredCategories );
                Core.TextIndexManager.QueryIndexing( contact.Id );

                extractedContacts = extractedContacts.Union( contact.ToResourceList(), true );
            }

            return extractedContacts;
        }

        //---------------------------------------------------------------------
        //  Split the given contact. Extract all subcontacts which are linked
        //  to it.
        //  Comment: source contact is removed.
        //---------------------------------------------------------------------
        public IResourceList  Split( IResource contact )
        {
            if( !contact.HasProp( _propSerializationBlobLink ))
                throw new ApplicationException( "Contact can not be split - no corresponding references" );

            IResourceList coveredCats;
            IResourceList allKeepers = contact.GetLinksOfType( "ContactSerializationBlobKeeper", _propSerializationBlobLink );
            IResourceList extractedContacts = SplitInternal( contact, allKeepers, out coveredCats );

            IResourceList cnames = contact.GetLinksOfType( "ContactName", Props.LinkBaseContact );
            Debug.Assert( cnames.Count == 0 );

            //  get full set of unresolved links to Categories and From/To/CC mails
            IResourceList currCats = contact.GetLinksOfType( "Category", ((CategoryManager)Core.CategoryManager).PropCategory );
            IResourceList currFrom = contact.GetLinksOfType( null, Props.LinkFrom );
            IResourceList currTo = contact.GetLinksOfType( null, Props.LinkTo );
            IResourceList currCC = contact.GetLinksOfType( null, Props.LinkCC );

            currCats = currCats.Minus( coveredCats );
            if( currCats.Count > 0 || currFrom.Count > 0 || currTo.Count > 0 || currCC.Count > 0 )
            {
                IResource bestContact = FindBestCandidate( extractedContacts );
                foreach( IResource res in currCats )
                    bestContact.AddLink( ((CategoryManager)Core.CategoryManager).PropCategory, res );

                foreach( IResource res in currFrom )
                    res.AddLink( Props.LinkFrom, bestContact );

                foreach( IResource res in currTo )
                    res.AddLink( Props.LinkTo, bestContact );

                foreach( IResource res in currCC )
                    res.AddLink( Props.LinkCC, bestContact );
            }

            contact.Delete();
            return extractedContacts;
        }

        private IResourceList  SplitInternal( IResource contact, IResourceList keepers,
                                              out IResourceList coveredCats )
        {
            //  start recreate serialized contacts
            coveredCats = RStore.EmptyResourceList;
            IResourceList extractedContacts = RStore.EmptyResourceList;
            OperationalSplittedContact = contact;
            contact.BeginUpdate();
            if( Core.ProgressWindow != null )
                Core.ProgressWindow.UpdateProgress( 0, "Splitting...", "" );
            for( int i = 0; i < keepers.Count; i++ )
            {
                Stream strm = keepers[ i ].GetBlobProp( _propSerializationBlob );
                IResource oldContact = ResourceBinarySerialization.Deserialize( strm, RemoveLink);
                Trace.WriteLineIf( !IsTraceSuppressed, "ContactManager -- Resource [" + oldContact.DisplayName + "] has " +
                                   oldContact.GetLinksOfType( null, "LinkedSetValue" ).Count + " links to conditions after split" );

                //  NB: Do not explicitely delete links from current contact to
                //  the category, because if we perform partial subcontact
                //  extraction, base contact must still keep its Category links
                coveredCats = coveredCats.Union( oldContact.GetLinksOfType( "Category", ((CategoryManager)Core.CategoryManager).PropCategory ), true );
                extractedContacts = extractedContacts.Union( oldContact.ToResourceList(), true );

                double percent = ((double)(i + 1)) / ((double)keepers.Count) * 95.00;
                if( Core.ProgressWindow != null )
                    Core.ProgressWindow.UpdateProgress( (int)percent, "Splitting...", "" );
            }
            //  Prevent UI updates
            foreach( IResource res in extractedContacts )
                res.BeginUpdate();

            //  For every mail which was associated with the base contact after
            //  merge, try to find the appropriate subcontact by its e-mail
            //  account - if there is a subcontact which is linked to the mail
            //  via the same account as the base contact - retarget link
            //  with explicit deletion from the base.
            RelinkNewMail( contact, extractedContacts, Props.LinkEmailAcctFrom, Props.LinkFrom );
            RelinkNewMail( contact, extractedContacts, Props.LinkEmailAcctTo, Props.LinkTo );
            RelinkNewMail( contact, extractedContacts, Props.LinkEmailAcctCC, Props.LinkCC );

            SeparateContactAndSubcontacts( contact, keepers );
            RemoveUnlinkedAccounts( contact, extractedContacts );
            RemoveObsoleteContactNames( contact, extractedContacts );
            foreach( IResource res in extractedContacts )
            {
                Core.TextIndexManager.QueryIndexing( res.Id );
            }

            //  Say End update for everybody.
            contact.EndUpdate();
            foreach( IResource res in extractedContacts )
                res.EndUpdate();

            if( Core.ProgressWindow != null )
                Core.ProgressWindow.UpdateProgress( 100, "Splitting...", "" );
            OperationalSplittedContact = null;

            return extractedContacts;
        }

        //  Remove link from base Contact to its subcontact wrappers as if there
        //  was no one :-).
        private static void SeparateContactAndSubcontacts( IResource contact, IResourceList keepers )
        {
            foreach( IResource res in keepers )
                contact.DeleteLink( _propSerializationBlobLink, res );
        }

        //---------------------------------------------------------------------
        //  Whether extracted contact have already their own C-CN-eA-E clumps
        //  (possibly made earlier, before merge) or not (e.g. in the case of
        //  single email account and contact) we do not need to keep CNames for
        //  related emails any more:
        //  1. Collect all CNames from the base contact (list1)
        //  2. Collect all mails linked to extracted contacts (list2)
        //  3. Collect all CNames linked to mails from list2 (list3)
        //  4. Remove intersection between list1 and list3.
        //---------------------------------------------------------------------

        private void RemoveObsoleteContactNames( IResource contact, IResourceList extractedContacts )
        {
            // 1.
            IResourceList baseContactCNames = contact.GetLinksOfType( "ContactName", Props.LinkBaseContact );
            // 2.
            IResourceList newCorrespondence = RStore.EmptyResourceList;
            foreach( IResource extrContact in extractedContacts )
                newCorrespondence = newCorrespondence.Union( LinkedCorrespondence( extrContact ), true );

            // 3.
            IResourceList mailLinkedCNames = RStore.EmptyResourceList;
            foreach( IResource mail in newCorrespondence )
            {
                mailLinkedCNames = mailLinkedCNames.Union( mail.GetLinksOfType( "ContactName", Props.LinkNameFrom ), true );
                mailLinkedCNames = mailLinkedCNames.Union( mail.GetLinksOfType( "ContactName", Props.LinkNameTo ), true );
                mailLinkedCNames = mailLinkedCNames.Union( mail.GetLinksOfType( "ContactName", Props.LinkNameCC ), true );
            }
            // 4.
            IResourceList extraCNames = baseContactCNames.Intersect( mailLinkedCNames, true );
            extraCNames.DeleteAll();
        }

        //---------------------------------------------------------------------
        //  Up to this point, base (merged) contact is linked to the email
        //  accounts of extracted contacts. Remove links to those of accounts
        //  which:
        //  1. have links to extracted contacts
        //  2. have no correspondence links to base contact.
        //---------------------------------------------------------------------
        private void  RemoveUnlinkedAccounts( IResource contact, IResourceList extracted )
        {
            IResourceList mails = LinkedCorrespondence( contact );
            if( mails.Count == 0 )
            {
                return;
            }

            IResourceList selfAccounts = LinkedAccounts( contact ).Intersect( LinkedAccounts( extracted ), true );

            foreach( IResource selfAccount in selfAccounts )
            {
                if( LinkedAccountCorrespondence( selfAccount ).Intersect( mails, true ).Count == 0 )
                {
                    contact.DeleteLink( Props.LinkEmailAcct, selfAccount );
                }
            }
        }

        //---------------------------------------------------------------------
        //  For all primary correspondence links (currently From, To and CC),
        //  which (potentially) have link restrictions (depending on the type of
        //  the vis-a-vis resource) - direcly remove them from the merged contact,
        //  they will be created in just deserialized subcontact). Thus this
        //  method is called JUST BEFORE the link is created in deserializator.
        //
        //  Same problem is with links between Contact and ContactName resources.
        //---------------------------------------------------------------------
        private void RemoveLink( IResource contact, IResource linkedRes, int linkId )
        {
            if( contact.Type != "Contact" )
                throw new ArgumentException( "ContactManager -- Illegal type of deserialized resource during Contact split" );

            int  normLinkId = Math.Abs( linkId );
            if( IsMajorLink( normLinkId ))
            {
                try
                {
                    OperationalSplittedContact.DeleteLink( normLinkId, linkedRes );
                }
                catch( Exception )
                {}
            }
            else
            if( normLinkId == Props.LinkBaseContact )
            {
                if( linkedRes.Type != "ContactName" )
                    throw new ArgumentException( "ContactManager -- Illegal type of linked resource from the deserialized one during Contact split - " +
                                                 linkedRes.Type + " - when ContactNames is expected" );
                //  NB: Pay special attention that method <HasLink> understands
                //      only undirected links and those coming out FROM the
                //      resource. For links coming IN to the resource, use negative
                //      value for the link ID.
                if( OperationalSplittedContact.HasLink( -normLinkId, linkedRes ))
                {
                    try
                    {
                        OperationalSplittedContact.DeleteLink( normLinkId, linkedRes );
                    }
                    catch( Exception )
                    {}
                }
            }
        }

        //---------------------------------------------------------------------
        private static void RelinkNewMail( IResource baseContact, IResourceList savedContacts,
                                           int idAccountLink, int idContactLink )
        {
            IResourceList mails = baseContact.GetLinksOfType( null, idContactLink );
            foreach( IResource res in mails )
            {
                IResourceList accounts = res.GetLinksFrom( "EmailAccount", idAccountLink );
                foreach( IResource contact in savedContacts )
                {
                    if( contact.GetLinksOfType( "EmailAccount", "EmailAcct" ).Intersect( accounts, true ).Count > 0 )
                    {
                        res.DeleteLink( idContactLink, baseContact );
                        res.AddLink( idContactLink, contact );
                        break;
                    }
                }
            }
        }
        #endregion SplitContacts

        #region FindContact
        public IResourceList FindContactList( string title, string firstName, string midName,
                                              string lastName, string suffix )
        {
            return FindContactListImpl( title, firstName, midName, lastName, suffix, null );
        }
        public IResourceList FindContactList( string fullName )
        {
            IResourceList contacts = RStore.EmptyResourceList;
            string    title, firstName, midName, lastName, suffix, addSpec;
            if( ContactResolver.ResolveName( fullName, null, out title, out firstName, out midName, out lastName, out suffix, out addSpec ) )
                contacts = FindContactListImpl( title, firstName, midName, lastName, suffix, null );
            return contacts;
        }

        public IContact FindContact( string fullName )
        {
            IContact contact = null;
            string    title, firstName, midName, lastName, suffix, addSpec;
            if( ContactResolver.ResolveName( fullName, null, out title, out firstName, out midName, out lastName, out suffix, out addSpec ) )
                contact = FindContact( title, firstName, midName, lastName, suffix );

            return contact;
        }

        public IContact FindContact( string title, string firstName, string midName,
                                     string lastName, string suffix )
        {
            ContactBO contact = null;
            IResourceList result = FindContactListImpl( title, firstName, midName, lastName, suffix, null );

            //-----------------------------------------------------------------
            //  It is possible to have several result contacts when no additional
            //  field is specified, and several contacts match the basic params.
            //  Choose one which have lesser amount of specificators.
            //-----------------------------------------------------------------
            if( result.Count == 1 )
                contact = new ContactBO( result[ 0 ] );
            else
            if( result.Count > 1 )
                contact = FindRestrictedContact( result );

            return contact;
        }

        //---------------------------------------------------------------------
        //  Method tries several alternatives to find the contact given its
        //  name fields and an account:
        //  1. Try to find contact only by names. In the case of several contacts
        //     having been found, select the one which has the given account.
        //     If no such - select the contact which has lesser number of
        //     additional restrictions in the name fields.
        //  2. If no contact is found by name, try to analyze sender names of
        //     contacts linked to the account.
        //---------------------------------------------------------------------
        private IContact FindContact( IResource account, string title, string firstName,
                                      string midName, string lastName, string suffix )
        {
            ContactBO contact = null;
            IResourceList result = FindContactListImpl( title, firstName, midName, lastName, suffix, null );

            //-----------------------------------------------------------------
            //  It is possible to have several result contacts when no additional
            //  field is specified, and several contacts match the basic params.
            //  First, perform disambiguation based on account parameter - choose
            //  those which have such account.
            //  Then, choose one which have lesser amount of specificators.
            //-----------------------------------------------------------------
            if( result.Count == 1 )
                contact = new ContactBO( result[ 0 ] );
            else
            {
                IResourceList linkedContacts = RStore.EmptyResourceList;
                if( account != null )
                    linkedContacts = account.GetLinksOfType( "Contact", Props.LinkEmailAcct );

                if( result.Count > 1 )
                {
                    linkedContacts = linkedContacts.Intersect( result, true );

                    //  If one - return it. If more - use it for next restrictions,
                    //  If no - use original list.
                    if( linkedContacts.Count == 1 )
                        contact = new ContactBO( linkedContacts[ 0 ] );
                    else
                    {
                        if( linkedContacts.Count > 1 )
                            result = linkedContacts;
                        contact = FindRestrictedContact( result );
                    }
                }
                else // (result.Count == 0)
                {
                    string fullName = Utils.MergeStrings( new[]{ title, firstName, midName, lastName, suffix }, ' ' );

                    //  Perform search within ContactNames of contacts linked
                    //  to the specified account;
                    foreach( IResource res in linkedContacts )
                    {
                        if( ContactHasCName( res, fullName ))
                        {
                            contact = new ContactBO( res );
                            break;
                        }
                    }
                }
            }
            return contact;
        }
        private bool  ContactHasCName( IResource contact, string fullName )
        {
            bool isEmpty = fullName.Length == 0;
            IResourceList siblings = contact.GetLinksOfType( null, Props.LinkBaseContact );
            foreach( IResource cName in siblings.ValidResources )
            {
                string name = cName.GetStringProp( Core.Props.Name );
                if( name == null )
                {
                    if ( isEmpty ) continue;
                    name = string.Empty;
                }

                //  use the fact that fullname can not be NULL
                if( string.Compare( name, fullName, true ) == 0 )
                {
                    return true;
                }
            }
            return false;
        }

        private static IResourceList FindContactListImpl( string title, string fn, string mn,
                                                          string ln, string suff, string addSpec )
        {
            //  Require that anything from the required fields is present.
            if( string.IsNullOrEmpty( fn ) && string.IsNullOrEmpty( ln ) )
                return RStore.EmptyResourceList;

            if( fn == null ) fn = string.Empty;
            if( ln == null ) ln  = string.Empty;

            //  No difference, what to compare first.
            int  propF = _propLastName, propS = _propFirstName;
            string valF = ln;
            string valS = fn;
            if( string.IsNullOrEmpty( ln ))
            {
                valF = fn; propF = _propFirstName; valS = null;
            }

            //-----------------------------------------------------------------
            IResourceList temp = RStore.FindResources( "Contact", propF, valF );
            IResourceList result = RStore.EmptyResourceList;
            List<int> contactIds = new List<int>();
            bool isValidSecond = !string.IsNullOrEmpty( valS );

            foreach( IResource res in temp )
            {
                string prop = res.GetStringProp( propS );
                bool   isValidProp = !string.IsNullOrEmpty( prop );

                if( ( isValidSecond && isValidProp && string.Compare( prop, valS, true ) == 0 ) ||
                    ( !isValidSecond && !isValidProp ) )
                {
                    contactIds.Add( res.Id );
                }
            }
            if( contactIds.Count > 0 )
            {
                result = RStore.ListFromIds( contactIds, false );
            }

            //-----------------------------------------------------------------
            //  To this point, basic set is constructed. If additional (optional)
            //  fields are give, restrict this set further.
            //-----------------------------------------------------------------
            if( !string.IsNullOrEmpty( mn ))
                result = IntersectSets( result, mn, _propMiddleName );
            if( !string.IsNullOrEmpty( title ))
                result = IntersectSets( result, title, _propTitle );
            if( !string.IsNullOrEmpty( suff ))
                result = IntersectSets( result, suff, _propSuffix );

            //-----------------------------------------------------------------
            //  Do not use constraining on additional specifier conforming
            //  our policy of its construction.
            //if( Utils.IsValidString( addSpec ))
            //    result = IntersectSets( result, addSpec, _propSpecificator );
            //-----------------------------------------------------------------

            //-----------------------------------------------------------------
            //  Among several alternatives, return only those contacts which
            //  are not "deleted" contacts (not visible in the "Deleted Resources"
            //  view). Return the list as is if all contact in the list are
            //  deleted contacts.
            //-----------------------------------------------------------------
            if( result.Count > 1 )
            {
                contactIds = new List<int>();
                foreach( IResource res in result )
                {
                    if( !res.HasProp( Core.Props.IsDeleted ) )
                    {
                        contactIds.Add( res.Id );
                    }
                }
                temp = RStore.ListFromIds( contactIds, false );
                if( temp.Count > 0 && temp.Count != result.Count )
                {
                    result = temp;
                }
            }

            return result;
        }

        private static IResourceList IntersectSets( IResourceList orig, string pattern, int propId )
        {
            return orig.Intersect( Core.ResourceStore.FindResources( null, propId, pattern ), true );
        }
        private static ContactBO FindRestrictedContact( IResourceList list )
        {
            int       constraintsCounter = int.MaxValue;
            IResource resultContact = null;
            foreach( IResource res in list )
            {
                int localCounter = CountConstrainingProps( res );
                if( localCounter < constraintsCounter )
                {
                    resultContact = res;
                    constraintsCounter = localCounter;
                }
            }
            return new ContactBO( resultContact );
        }
        private static int CountConstrainingProps( IResource res )
        {
            int counter = 0;
            if( res.HasProp( _propMiddleName ))     counter++;
            if( res.HasProp( _propTitle ))          counter++;
            if( res.HasProp( _propSuffix ))         counter++;
            if( res.HasProp( _propSpecificator ))   counter++;
            return counter;
        }
        #endregion FindContact

        #region FindOrCreateContact
        public IContact FindOrCreateContact( string email, string firstName, string lastName )
        {
            return FindOrCreateContact( email,  string.Empty, firstName, string.Empty, lastName, string.Empty );
        }

        public IContact FindOrCreateContact( string email, string title, string fn,
                                             string mn, string ln, string suffix )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( email ) && String.IsNullOrEmpty( fn ) && String.IsNullOrEmpty( ln ))
                throw new ArgumentNullException( "email", "ContactManager -- Email, First Name and Last Name can not be null or empty simultaneously" );
            #endregion Preconditions

            IContact contact;
            IResource account = FindOrCreateEmailAccount( email );

            //  First, check whether the given account belongs to the set of accounts
            //  from Myself contact - otherwise we will illegally create extra contact.
            if( _mySelfContact != null )
            {
                if( _myselfAccounts == Core.ResourceStore.EmptyResourceList )
                {
                    _myselfAccounts = _mySelfContact.Resource.GetLinksOfTypeLive( "EmailAccount", Props.LinkEmailAcct );
                }
            }

            if(( account != null ) && ( _myselfAccounts.IndexOf( account.Id ) != -1 ) &&
               account.HasProp( Props.PersonalAccount ) )
            {
                contact = FindOrCreateMySelfContact( email, title, fn, mn, ln, suffix );
            }
            else
            {
                contact = FindOrCreateContactImpl( account, title, fn, mn, ln, suffix );
                contact.QueueIndexing();
            }

            return contact;
        }

        private IContact FindOrCreateContactImpl( IResource account, string title, string fn,
                                                  string mn, string ln, string suffix )
        {
            IContact contact = FindContact( account, title, fn, mn, ln, suffix );
            if( contact == null )
            {
                contact = CreateOrUpdateContact( account, title, fn, mn, ln, suffix );
            }
            contact.AddAccount( account );

            return contact;
        }

        public IContact FindOrCreateContact( string email, string name )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( email ) && String.IsNullOrEmpty( name ) )
                throw new ArgumentNullException( "email", "ContactManager -- Email and SenderName can not be null or empty simultaneously" );
            #endregion Preconditions

            IContact  contact;
            string    title, fn, mn, ln, suffix, addSpec;
            if( ContactResolver.ResolveName( name, email, out title, out fn, out mn, out ln, out suffix, out addSpec ) )
            {
                contact = FindOrCreateContact( email, title, fn, mn, ln, suffix );
            }
            else
            {
                IResource emailAccount = FindOrCreateEmailAccount( email );
                if( emailAccount == null )
                    throw new ArgumentNullException( "emailAccount", "ContactManager - Internal error - Email Account and Unresolved Contact resource can not be null simultaneously" );

                //  Contact has not been found or created. That means that senderName is
                //  not valid, while the email account is. Find any existing contact from
                //  this account or (if there is no one) create new empty one.
                IResourceList linkedContacts = emailAccount.GetLinksOfType( "Contact", Props.LinkEmailAcct );
                if( linkedContacts.Count > 0 )
                {
                    IResource bestFit = FindBestCandidate( linkedContacts );
                    contact = new ContactBO( bestFit );
                }
                else
                    contact = CreateBlankContact();
                contact.AddAccount( emailAccount );

                Core.TextIndexManager.QueryIndexing( contact.Resource.Id );
            }
            return contact;
        }

        //---------------------------------------------------------------------
        //  Method has some intricasy in its logic. First of all, it is called
        //  outside when the caller determines that a given email account
        //  belongs to the application owner - say MySelf contact. Independently
        //  of that information, we split all email accounts into two groups:
        //  - personal: accounts that are truly belong to a contact and are not
        //    shared wrt any emailing service.
        //  - group: shared accounts.
        //  When we meet shared account, we must apply ordinary logic - not
        //  specific for a myself contact, that is link account to a contact only
        //  if a sender's name coinsides with the contacts' one.
        //---------------------------------------------------------------------
        public IContact FindOrCreateMySelfContact( string email, string title, string fn,
                                                   string mn, string ln, string suffix )
        {
            #region Preconditions
            if( RStore.FindResources( "Contact", "MySelf", 1 ).Count > 1 )
                throw new InvalidProgramException( "ContactManager -- Amount of MYSELF contacts exceeds 1" );
            #endregion Preconditions

            if( title == null ) title = string.Empty;
            if( fn == null )    fn = string.Empty;
            if( mn == null )    mn = string.Empty;
            if( ln == null )    ln = string.Empty;
            if( suffix == null )suffix = string.Empty;

            //-----------------------------------------------------------------
            ContactBO contact = null;
            IResource account = FindOrCreateEmailAccount( email );

            if( account == null || account.HasProp( Props.PersonalAccount ) )
            {
                IResource mySelfRes = RStore.FindUniqueResource( "Contact", "MySelf", 1 );
                if ( mySelfRes != null )
                {
                    contact = new ContactBO( mySelfRes );
                    contact.AddAccount( account );
                }
                else
                {
                    contact = (ContactBO) FindOrCreateContact( email, title, fn, mn, ln, suffix );
                    contact.SetMyself();
                }
                if( _mySelfContact == null )
                    _mySelfContact = contact;
            }
            else
            {
                contact = (ContactBO) FindOrCreateContactImpl( account, title, fn, mn, ln, suffix );
            }

            contact.QueueIndexing();

            return contact;
        }

        public IContact FindOrCreateMySelfContact( string email, string name )
        {
            #region Preconditions
            if( RStore.FindResources( "Contact", "MySelf", 1 ).Count > 1 )
                throw new InvalidProgramException( "ContactManager -- Amount of MYSELF contacts exceeds 1" );

            if( String.IsNullOrEmpty( email ) && String.IsNullOrEmpty( name ) )
                throw new ArgumentNullException( "email", "ContactManager -- Email and SenderName for Myself can not be null or empty simultaneously." );
            #endregion Preconditions

            string  title, fn, mn, ln, suffix, addSpec;
            bool    resolved = ContactResolver.ResolveName( name, email, out title, out fn,
                                                            out mn, out ln, out suffix, out addSpec );
            ContactBO contact = null;
            if ( resolved )
                contact = (ContactBO) FindOrCreateMySelfContact( email, title, fn, mn, ln, suffix );
            else
            {
                IResource mySelf = RStore.FindUniqueResource( "Contact", "MySelf", 1 );
                if ( mySelf != null )
                {
                    contact = new ContactBO( mySelf );
                    contact.AddAccount( email );
                }
                else
                {
                    contact = (ContactBO) FindOrCreateContact( email, name );
                    contact.SetMyself();
                }
            }

            return contact;
        }

        public IResource FindOrCreateEmailAccount( string email )
        {
            IResource emailAccount = null;
            if( !string.IsNullOrEmpty( email ) )
            {
                emailAccount = RStore.FindUniqueResource( "EmailAccount", Props.EmailAddress, email );
                if ( emailAccount == null )
                {
                    emailAccount = RStore.NewResource( "EmailAccount" );
                    emailAccount.SetProp( Props.EmailAddress, email );
                }
            }
            return emailAccount;
        }

        public IResource FindOrCreateMailingList( string email )
        {
            IResource emailAccount = FindOrCreateEmailAccount( email );
            if ( emailAccount == null )
                return null;

            IResourceList mailLists = emailAccount.GetLinksOfType( "MailingList", Props.LinkEmailAcct );
            if ( mailLists.Count > 0 )
                return mailLists [ 0 ];

            IResource mailList = RStore.BeginNewResource( "MailingList" );
            mailList.AddLink( Props.LinkEmailAcct, emailAccount );
            mailList.EndUpdate();
            return mailList;
        }
        #endregion FindOrCreateContact

        #region CreateContact
        public IContact CreateContact( string title, string fName, string midName, string lName, string suffix )
        {
            return CreateContactImpl( title, fName, midName, lName, suffix );
        }
        public IContact CreateContact( string fullName )
        {
            IContact contact = null;
            string    title, fName, midName, lName, suffix, addSpec;
            if( ContactResolver.ResolveName( fullName, null, out title, out fName, out midName, out lName, out suffix, out addSpec ) )
                contact = CreateContactImpl( title, fName, midName, lName, suffix );

            return contact;
        }

        public IContact  CreateOrUpdateContact( IResource emailAcc, string fullName )
        {
            string    title, fName, midName, lName, suffix, addSpec;
            ContactResolver.ResolveName( fullName, emailAcc.DisplayName, out title, out fName, out midName, out lName, out suffix, out addSpec );

            //  independently of resolve status, either update empty contact from
            //  the account or create new (empty if not resolved).
            return CreateOrUpdateContact( emailAcc, title, fName, midName, lName, suffix );
        }
        public IContact  CreateOrUpdateContact( IResource emailAcc, string title, string firstName,
                                                string midName, string lastName, string suffix )
        {
            ContactBO contact = FindEmptyContactFromAccount( emailAcc );
            if( contact != null )
            {
                Trace.WriteLine( "ContactManager -- Found empty contact, updating fields in CreateOrUpdateContact." );
                contact.Title = title;
                contact.FirstName = firstName;
                contact.MiddleName = midName;
                contact.LastName = lastName;
                contact.Suffix = suffix;
            }
            else
                contact = CreateContactImpl( title, firstName, midName, lastName, suffix );

            return contact;
        }

        private static ContactBO CreateBlankContact()
        {
            return CreateContactImpl( null, null, null, null, null );
        }
        private static ContactBO CreateContactImpl( string title, string firstName, string midName,
                                                    string lastName, string suffix )

        {
            ContactBO contact = new ContactBO( RStore.NewResource( "Contact" ) );
            contact.Title = title;
            contact.FirstName = firstName;
            contact.MiddleName = midName;
            contact.LastName = lastName;
            contact.Suffix = suffix;
  //          contact.AdditionalSpec = addSpec;

            return contact;
        }
        #endregion CreateContact

        #region EmptyContacts
        private ContactBO FindEmptyContactFromAccount( IResource account )
        {
            if( account != null )
            {
                IResourceList contacts = FilterNonBlankContacts(
                    account.GetLinksOfType( "Contact", Props.LinkEmailAcct ) );
                foreach( IResource res in contacts.ValidResources )
                {
                    return new ContactBO( res );
                }
            }
            return null;
        }

        public static bool  IsEmptyContact( IResource contact )
        {
            return( contact.GetPropText( _propFirstName ).Length == 0 &&
                    contact.GetPropText( _propLastName ).Length == 0 );
        }
        public static IResourceList FilterNonBlankContacts( IResourceList contacts )
        {
            return contacts.Minus( Core.ResourceStore.FindResourcesWithProp( null, _propFirstName ) ).Minus(
                Core.ResourceStore.FindResourcesWithProp( null, _propLastName ) );
        }
        public static bool  IsEmptyContact( IContact contact )
        {
            return( contact.FirstName.Length == 0 && contact.LastName.Length == 0 );
        }

        internal void DeleteBlankContacts( IResource emailAccount )
        {
            foreach( IResource res in FilterNonBlankContacts(
                emailAccount.GetLinksOfType( "Contact", Props.LinkEmailAcct ) ).ValidResources )
            {
                DeleteContactImplLight( res );
            }
        }
        #endregion EmptyContacts

        #region Delete Contact
        public void  DeleteContact( IResource contact, bool ignoreContact, out string message )
        {
            #region Preconditions
            if( contact == null )
                throw new ArgumentNullException( "contact", "ContactManager -- input contact can not be null" );

            if( contact.Type != "Contact" )
                throw new ArgumentException( "ContactManager -- Invalid type of input resource in [DeleteContact] - " + contact.Type );
            #endregion Preconditions

            message = null;
            if( !contact.HasProp( Props.Myself )) //  Never do anything with Myself !!!
            {
                if( !contact.HasProp( Core.Props.IsDeleted ) )
                {
                    message = DeleteLinkedCorrespondence( contact );
                    if( message == null )
                    {
                        if( ignoreContact )
                            contact.SetProp( Props.Ignored, true );
                        contact.SetProp( Core.Props.IsDeleted, true );
                    }
                }
            }
            else
            {
                message = "Omea user information resource can not be deleted";
            }
        }

        public static void  RemoveContactFromAddressBook( IResource contact, IResource addressBook )
        {
            #region Preconditions
            if( contact == null )
                throw new ArgumentNullException( "contact", "ContactManager -- input contact can not be null" );

            if( contact.Type != "Contact" )
                throw new ArgumentException( "ContactManager -- Invalid type of input contact resource in [RemoveContactFromAddressBook] - " + contact.Type );

            if( addressBook == null )
                throw new ArgumentNullException( "addressBook", "ContactManager -- input address book can not be null" );

            if( addressBook.Type != "AddressBook" )
                throw new ArgumentException( "ContactManager -- Invalid type of input address book resource in [RemoveContactFromAddressBook] - " + addressBook.Type );
            #endregion Preconditions

            new AddressBook( addressBook ).RemoveContact( contact );
        }

        //---------------------------------------------------------------------
        //  Currently the main goal of this method is to clean ContactName
        //  resources linked to the given one.
        //
        //  If a given resource is the only linked to the ContactName,
        //  then this ContactName must be deleted as well.
        //---------------------------------------------------------------------
        public void  UnlinkContactInformation( IResource res )
        {
            #region Preconditions
            if( res == null )
                throw new ArgumentNullException( "res", "ContactManager -- Input resource can not be NULL" );
            #endregion Preconditions

            IResourceList linkedCNames = LinkedNameCorrespondence( res );
            for( int i = 0; i < linkedCNames.Count; i++ )
            {
                IResourceList linkedRes = LinkedNameCorrespondence( linkedCNames[ i ] );
                if( linkedRes.Count == 1 )
                    new ResourceProxy( linkedCNames[ i ] ).Delete();
            }
        }
        #region Hanged and Unused Contacts
        public void DeleteUnusedContacts( IResourceList contacts )
        {
            int count = contacts.Count;
            for ( int i = count - 1; i >= 0 ; i-- )
            {
                IResource contact;
                try
                {
                    contact = contacts[ i ];
                }
                catch( StorageException )
                {
                    continue;
                }

                DeleteHangedContactNames( contact );
                DeleteHangedContact( contact );
            }
        }

        private static void  DeleteHangedContactNames( IResource contact )
        {
            IResourceList cNames = contact.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
            for( int i = 0; i < cNames.Count; i++ )
            {
                if ( !cNames [i].HasProp( -Core.ContactManager.Props.LinkNameFrom ) &&
                     !cNames [i].HasProp( -Core.ContactManager.Props.LinkNameTo ) &&
                     !cNames [i].HasProp( -Core.ContactManager.Props.LinkNameCC ) )
                {
                    new ResourceProxy( cNames[ i ] ).Delete();
                }
            }
        }

        //---------------------------------------------------------------------
        //  Remove contact if it does not associated with any mail, article,
        //  etc, thus it has links only to email accounts and Contact names.
        //---------------------------------------------------------------------
        private void  DeleteHangedContact( IResource contact )
        {
            if( !contact.HasProp( Props.Myself ) && !contact.HasProp( _propImported ) &&
                !contact.HasProp( ContactManager._propUserCreated ) )
            {
                int[]  linkTypeIDs = contact.GetLinkTypeIds();
                foreach ( int linkTypeID in linkTypeIDs )
                {
                    if( !ResourceTypeHelper.IsAccountLink( linkTypeID ) && ( linkTypeID != Core.ContactManager.Props.LinkBaseContact ))
                        return;
                }

                string errMsg;
                DeleteContact( contact, false, out errMsg );
            }
        }
        #endregion Hanged and Unused Contacts

        #region Removal Implementation
        public static string  DeleteLinkedCorrespondence( IResource contact )
        {
            bool    isSuccess = true;
            string  errMsg = null;

            //  To remove contact - first, analyze all linked correspondence:
            //  - for each res type in list there must be defined an its own
            //    deleter class;
            //  - actual removal can be successful.

            IResourceList linked = LinkedCorrespondence( contact );
            linked = ResourceTypeHelper.ExcludeUnloadedPluginResources( linked );
            foreach( IResource res in linked )
            {
                IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( res.Type );
                isSuccess = isSuccess && ( deleter != null) && deleter.CanDeleteResource( res, false );
            }

            Trace.WriteLine( "ContactManager -- Analyzed " + linked.Count + " linked correspondence messages." );
            if( isSuccess )
            {
                //   GOOD! all resources (their types have corresponding deleters!!!)
                Trace.WriteLine( "ContactManager -- All messages have Deleter support." );
                foreach( IResource res in linked )
                {
                    //  When performing non-permanent deletion of the linked resources,
                    //  do not call delete of the already deleted items - otherwise they
                    //  potentially will be removed permanently.
                    if( !res.HasProp( Core.Props.IsDeleted ) )
                        Core.PluginLoader.GetResourceDeleter( res.Type ).DeleteResource( res );
                }
            }
            else
            {
                Trace.WriteLine( "ContactManager -- NOT All messages have Deleter support." );
                errMsg = "Contact \"" + contact.DisplayName + "\" can not be deleted because of problems with the removal of its correspondence.";
            }
            return errMsg;
        }

        //---------------------------------------------------------------------
        //  When removing a contact:
        //  1. Remove linked accounts (only those which linked only to this contact)
        //  2. Remove linked contact names
        //  3. Remove a contact resource per se.
        //---------------------------------------------------------------------
        public static void  DeleteContactImpl( IResource contact )
        {
            try
            {
                int[]  linkTypeIDs = contact.GetLinkTypeIds();
                foreach ( int linkTypeID in linkTypeIDs )
                {
                    if( ResourceTypeHelper.IsAccountLink( linkTypeID ) )
                    {
                        IResourceList contactAccounts = contact.GetLinksOfType( null, linkTypeID );
                        foreach ( IResource contactAccount in contactAccounts )
                        {
                            if( contactAccount.GetLinksOfType( "Contact", linkTypeID ).Count == 1 )
                                contactAccount.Delete();
                        }
                    }
                }

                DeleteContactImplLight( contact );
            }
            catch( Exception e )
            {
                Core.ReportBackgroundException( e );
            }
        }

        private static void  DeleteContactImplLight( IResource contact )
        {
            IResourceList contactNames = contact.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
            contactNames.DeleteAll();
            contact.Delete();
        }
        #endregion Removal Implementation
        #endregion Delete Contact

        #region Delete Unnecessary ContactNames
        /// <summary>
        /// Delete all resources of type ContactName which are identical to the contact's name -
        /// or almost identical (e.g. to the extent of apostrophes).
        /// Additionally check several violations - single contact names which are NOT named
        /// equally to the contact and those resources which linked by the "BaseContact" link
        /// but do not belong to the "ContactName" resource type.
        /// </summary>
        /// <returns># of contact names to be deleted and # of illegally named single contact names.</returns>
        public static void UnlinkIdenticalContactNames( IResourceList contacts, IProgressWindow wnd,
                                                        ref int count, ref int illegallyNamedCount )
        {
            count = illegallyNamedCount = 0;
            for( int i = 0; i < contacts.Count; i++ )
            {
                IResourceList cNames = contacts[ i ].GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
                foreach( IResource cName in cNames )
                {
                    String cname = cName.GetStringProp( Core.Props.Name );
                    String contact = contacts[ i ].DisplayName;
                    if( cname == contact || CleanedName( cname ) == contact )
                    {
                        new ResourceProxy( cName ).Delete();
                        count++;
                    }
                    else
                    {
                        illegallyNamedCount++;
                    }
                }
                if( wnd != null && i % 100 == 0 )
                {
                    int percent = i * 100 / contacts.Count;
                    wnd.UpdateProgress( percent, "Upgrading Contact Names information: " + percent + "%, " + count + " names removed", null );
                }
            }
        }
        #endregion Delete Unnecessary ContactNames

        #region Contact Linkage
        public void LinkContactToResource( int linkId, IResource contact, IResource mail,
                                           string account, string senderName )
        {
            IResource accRes = FindOrCreateEmailAccount( account );
            LinkContactToResource( linkId, contact, mail, accRes, senderName );
        }

        /// <summary>
        /// Performs basic operations on linking between mail (news article, etc.)
        /// resource, contact and emailAccount resources:
        /// 1. bind a mail with a contact via "From", "To" or "CC" link;
        /// 2. create a new object "ContactName" and bind a mail and CN with "NameFrom",
        ///    "NameTo" or "NameCC" link, and finally link CN and its base contact
        ///    object;
        /// 3. link email (news article, etc) with emailAccount resource.
        /// </summary>
        /// <param name="linkId">Id of link between a mail and a contact</param>
        /// <param name="contact">Base contact</param>
        /// <param name="mail">A resource to be linked (mail, news article etc.)</param>
        /// <param name="account">Mail account of a mail.</param>
        /// <param name="senderName">Name which will be shown to the user.</param>
        public void LinkContactToResource( int linkId, IResource contact, IResource mail,
                                           IResource account, string senderName )
        {
            #region Preconditions
            if( linkId != Props.LinkFrom && linkId != Props.LinkTo && linkId != Props.LinkCC )
                throw new ArgumentException( "ContactManager -- Illegal [propID] property value - not From, To or CC" );

            if( contact == null )
                throw new ArgumentNullException( "contact", "ContactManager -- Contact resource is null" );

            if( mail == null )
                throw new ArgumentNullException( "mail", "ContactManager -- Mail/Article resource is null" );

            if( contact.Type != "Contact" && contact.Type != "MailingList" )
                throw new ArgumentException( "ContactManager -- Illegal resource type - not a Contact" );

            //  account is allowed to be null or senderName is allowed to be
            //  null (e.g. in the case of news), but not both simultaneously
            if( String.IsNullOrEmpty( senderName ) && (account == null))
                throw new ArgumentException( "ContactManager -- Account and Sender name are not allowed to be null simultaneously");
            #endregion Preconditions

            //-----------------------------------------------------------------
            //  Link Contact to a Resource Type resource of the mail.
            //-----------------------------------------------------------------
            IResource type = Core.ResourceStore.FindUniqueResource( "ResourceType", Core.Props.Name, mail.Type );
            contact.AddLink( Props.LinkLinkedOfType, type );

            //-----------------------------------------------------------------
            //  First check whether the link is unique, that is only one such
            //  link can exist between a resource and the contact, e.g. link
            //  FROM. In such case remove all resources linked by the propId
            //  and GetNameLinkId( propId ) links.
            //-----------------------------------------------------------------
            if( IsUniqueLink( linkId, mail.Type ) )
            {
                mail.SetProp( linkId, contact );
                mail.DeleteLinks( GetNameLinkId( linkId ));
            }
            else
                mail.AddLink( linkId, contact );

            //  Do not link contact name with "MailingList" resources.
            if( contact.Type == "Contact" )
                CreateAndLinkContactName( contact, mail, account, linkId, senderName );

            if( account != null )
            {
                int  linkAccntId = GetAccountLinkId( linkId );
                mail.AddLink( linkAccntId, account );
            }

            //-----------------------------------------------------------------
            //  In the case when the contact is deleted non-permanently,
            //  two continuations are possible:
            //  1. If a contact is marked as Ignored then all newly incoming
            //     correspondence from this contact is to be moved to the
            //     RecycleBin automatically.
            //  2. If a contact is NOT marked as Ignored then it is recovered
            //     from the RecycleBin and appears as ordinary contact.
            //     Deleting of "IsIgnored" property makes that.
            //-----------------------------------------------------------------
            if( contact.HasProp( Core.Props.IsDeleted ))
            {
                if( contact.HasProp( Props.Ignored ) )
                {
                    Core.ResourceAP.QueueJobAt( DateTime.Now.AddMilliseconds( 100.0 ),
                                                new ResourceDelegate( DeleteResourceToRecycleBin ), mail );
                }
                else
                {
                    contact.DeleteProp( Core.Props.IsDeleted );
                }
            }
        }

        private static void DeleteResourceToRecycleBin( IResource res )
        {
            IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( res.Type );
            if( !res.HasProp( Core.Props.IsDeleted ) && deleter != null )
                deleter.DeleteResource( res );
        }

        private void CreateAndLinkContactName( IResource contact, IResource mail, IResource accnt,
                                               int linkId, string senderName )
        {
            #region Preconditions
            if( contact.Type != "Contact" )
                throw new ArgumentException( "ContactManager -- contract violation: expected type [Contact]." );
            #endregion Preconditions

            if( string.IsNullOrEmpty( senderName ))
                return;

            //-----------------------------------------------------------------
            //  Do not add unnecessary info if current naming (from ContactName)
            //  coinsides with the generic name (that of Contact).
            //-----------------------------------------------------------------
            senderName = CleanedName( senderName );
            if( senderName == contact.DisplayName )
                return;

            //-----------------------------------------------------------------
            //  Do not create ContactName if there already exists one with
            //  such senderName and it is linked to these account and contact.
            //-----------------------------------------------------------------
            bool           multipleNamesFound = false;
            IResource      contactName = null;
            IResourceList  temp = RStore.EmptyResourceList;
            if( accnt != null )
                temp = accnt.GetLinksOfType( "ContactName", Props.LinkEmailAcct );

            //  use the fact that usually there is very small amount of ContactNames
            //  linked to a particular email account, thus the iteration over it
            //  is cheap.
            foreach( IResource res in temp )
            {
                if( res.GetStringProp( Core.Props.Name ) == senderName &&
                    res.HasLink( Props.LinkBaseContact, contact ) )
                {
                    multipleNamesFound = (contactName != null);
                    contactName = res;
                }
            }

            //-----------------------------------------------------------------
            //  Internal consistency check - account can not be connected to more
            //  than one ContactName having equal sender names.
            //-----------------------------------------------------------------
            int   nameLinkId = GetNameLinkId( linkId );
            if( multipleNamesFound )
            {
                //  If so - we deal with previous database version which had a bug in the
                //  CNames creation. In such case we can link to ANY CName resource.
                Trace.WriteLine( "ContactManager -- ContactName config violation: more than one equal CName [" +
                                 senderName + "] for an account [" + accnt.DisplayName + "]" );
            }

            //-----------------------------------------------------------------
            //  If there is only one ContactName, then it was created before,
            //  link the mail to this CName, do not create extra one.
            //-----------------------------------------------------------------
            if( contactName != null )
                mail.AddLink( nameLinkId, contactName );
            else
            {
                //-------------------------------------------------------------
                //  Some information sources (e.g. RSS feeds) do not provide an
                //  account at all. Creating new CName thus will lead
                //  to creation of numerous equal CName resources linked to each
                //  RSS post - instead, we must take the one which already exists
                //  without any linkage to accounts.
                //-------------------------------------------------------------
                if( accnt == null )
                {
                    IResourceList linkedCNames = contact.GetLinksOfType( "ContactName", Props.LinkBaseContact );
                    foreach( IResource name in linkedCNames )
                    {
                        if( name.GetStringProp( Core.Props.Name ) == senderName )
                        {
                            mail.AddLink( nameLinkId, name );
                            return;
                        }
                    }
                }

                //  No such CName linked by any configuration - create new one.
                CreateNewContactName( contact, accnt, mail, nameLinkId, senderName );
            }
        }

        private static void  CreateNewContactName( IResource contact, IResource account,
                                                   IResource mail, int linkId, string name )
        {
            IResource contactName = RStore.BeginNewResource( "ContactName" );
            if( !String.IsNullOrEmpty( name ) )
                contactName.SetProp( "Name", name );
            else
                Trace.WriteLineIf( !IsTraceSuppressed, "ContactManager -- Creating CN with empty name" );

            mail.AddLink( linkId, contactName );
            contactName.AddLink( Core.ContactManager.Props.LinkBaseContact, contact );

            if( account != null )
                contactName.AddLink( Core.ContactManager.Props.LinkEmailAcct, account );
            contactName.EndUpdate();
        }

        //---------------------------------------------------------------------
        //  Removal of email account from contact (unlinking of the former and
        //  the latter) requires more actions than just removal of the direct
        //  connection:
        //  - mails which are linked to these contact and account have to be
        //    relinked to a new contact linked with the account.
        //  - this new contact is created anew with the name derived from the
        //    linked ContactName.
        //  - ContactNames which were linked to the source contact are now
        //    relinked to the new contact.
        //---------------------------------------------------------------------
        public void  HardRemoveAccountFromContact( IResource contact, IResource account )
        {
            #region Preconditions
            if( contact == null )
                throw new ArgumentNullException( "contact", "ContactManager -- contact resource can not be null" );

            if( contact.Type != "Contact" )
                throw new ArgumentException( "ContactManager -- first resource parameter has inproper type (Contact is expected)" );

            if( account == null )
                throw new ArgumentNullException( "account", "ContactManager -- email account string can not be null or empty" );

            if( account.Type != "EmailAccount" )
                throw new ArgumentException( "ContactManager -- second resource parameter has inproper type (EmailAccount is expected)" );
            #endregion Preconditions

            Trace.WriteLine( "ContactManager -- Unlinking account [" + account.DisplayName + "] from contact [" + contact.DisplayName + "]");
            contact.BeginUpdate();
            contact.DeleteLink( _propDefaultAccount, account );
            contact.DeleteLink( Props.LinkEmailAcct, account );

            //  Collect all mails linked both to this contact and this account
            IResourceList mails = GetMailsLinkedToContactAndAccount( contact, account );

            int count = mails.Count;
            if( count > 0 )
            {
                int percent = 0;
                Hashtable names2Contact = new Hashtable();
                for( int i = 0; i < count; i++ )
                {
                    IResource mail = mails[ i ];

                    //  A mail can be linked to an account with several types
                    //  of links. For example, when a person sends a mail and puts
                    //  himself to CC. In such case we need to enumerate all links.
                    int[]  accountLink = GetAllAccountLinks( mail, account );
                    foreach( int link in accountLink )
                        RelinkContactFromMail( link, mail, account, contact, ref names2Contact );

                    if( i * 100 / count != percent && Core.ProgressWindow != null )
                    {
                        percent = i * 100 / count;
                        Core.ProgressWindow.UpdateProgress( percent, "Updating correspondence", null );
                        Trace.WriteLine( "ContactManager -- " + percent + " of correspondence processed." );
                    }
                }
            }
            contact.EndUpdate();
        }

        private void  RelinkContactFromMail( int accntLinkId, IResource mail, IResource account,
                                             IResource contact, ref Hashtable names2Contact )
        {
            //  Since the mail can be linked to the Contact by all From,
            //  To and CC links, use that corresponding to the link type from
            //  mail to account.
            int majorLinkId = GetMajorLinkId( accntLinkId );
            int nameLinkId  = GetNameLinkId( majorLinkId );

            //  We iterate over emails linked to the old contact and the
            //  email account. But we can not guarantee that the CURRENT
            //  link between email and account is one of several which do
            //  not correspond to the contact under deletion.
            if( mail.HasLink( majorLinkId, contact ) )
            {
                string name = null;
                IResource contactName = GetContactNameLinkedToContactAndAccount( mail, contact, account, nameLinkId );
                if( contactName != null )
                    name = contactName.DisplayName;
                else
                    name = "From: " + account.DisplayName;
                IContact newContact = CreateNewContact( account, names2Contact, name );
                Trace.WriteLine( "ContactManager -- Created (used) new account with name [" + name + "]" );

                //  Retarget mail, contact name and potentially account.
                mail.DeleteLink( majorLinkId, contact );
                mail.AddLink( majorLinkId, newContact.Resource );

                if( contactName != null )
                    contactName.SetProp( Props.LinkBaseContact, newContact.Resource );
                else
                    CreateAndLinkContactName( newContact.Resource, mail, account, majorLinkId, name );

                if( !account.HasLink( Props.LinkEmailAcct, newContact.Resource ) )
                    account.AddLink( Props.LinkEmailAcct, newContact.Resource );
            }
        }

        //---------------------------------------------------------------------
        //  Collect all mails linked both to a contact and an account
        //---------------------------------------------------------------------
        private static IResourceList GetMailsLinkedToContactAndAccount( IResource contact, IResource account )
        {
            IResourceList temp = LinkedAccountCorrespondence( account );
            return LinkedCorrespondence( contact ).Intersect( temp, true );
        }

        private static IContact CreateNewContact( IResource account, Hashtable names2Contact, string name )
        {
            IContact  contact = (IContact) names2Contact[ name ];
            if( contact == null )
            {
                contact = Core.ContactManager.CreateOrUpdateContact( account, name );
                names2Contact[ name ] = contact;
            }
            return contact;
        }

        //---------------------------------------------------------------------
        //  Get a ContactName resource linked to the particular mail, contact
        //  and an account by the particular link Id. We have to intersect all
        //  resource list since the same mail can be sent to the same Person
        //  under several accounts/contacts with the same linkId.
        //---------------------------------------------------------------------
        private IResource GetContactNameLinkedToContactAndAccount( IResource mail, IResource contact, IResource account, int nameLinkId )
        {
            IResourceList names = mail.GetLinksOfType( "ContactName", nameLinkId );
            names = names.Intersect( contact.GetLinksOfType( "ContactName", Props.LinkBaseContact ), true );
            names = names.Intersect( account.GetLinksOfType( "ContactName", Props.LinkEmailAcct   ), true );

            return (names.Count > 0)? names[ 0 ] : null;
        }

        public static void  CloneLinkage( IResource fromRes, IResource toRes )
        {
            AssignLinksOfType( fromRes, toRes, "Contact", Core.ContactManager.Props.LinkFrom );
            AssignLinksOfType( fromRes, toRes, "Contact", Core.ContactManager.Props.LinkTo );
            AssignLinksOfType( fromRes, toRes, "Contact", Core.ContactManager.Props.LinkCC );
            AssignLinksOfType( fromRes, toRes, "EmailAccount", Core.ContactManager.Props.LinkEmailAcctFrom );
            AssignLinksOfType( fromRes, toRes, "EmailAccount", Core.ContactManager.Props.LinkEmailAcctTo );
            AssignLinksOfType( fromRes, toRes, "EmailAccount", Core.ContactManager.Props.LinkEmailAcctCC );
            AssignLinksOfType( fromRes, toRes, "ContactName", Core.ContactManager.Props.LinkNameFrom );
            AssignLinksOfType( fromRes, toRes, "ContactName", Core.ContactManager.Props.LinkNameTo );
            AssignLinksOfType( fromRes, toRes, "ContactName", Core.ContactManager.Props.LinkNameCC );

        }
        private static void AssignLinksOfType( IResource fromRes, IResource toRes, string resType, int linkId )
        {
            IResourceList list = fromRes.GetLinksOfType( resType, linkId );
            if( list.Count > 0 )
            {
                toRes.BeginUpdate();
                foreach( IResource res in list )
                {
                    //  Make a workaround for corrupted databases, OM-13896.
                    try {  toRes.AddLink( linkId, res );  }
                    catch( Exception ) {}
                }
                toRes.EndUpdate();
            }
        }
        #endregion Contact Linkage

        #region Auxiliary
        public bool IsMajorLink( int propId )
        {
            return (propId == Props.LinkFrom) || (propId == Props.LinkTo) || (propId == Props.LinkCC);
        }

        public bool IsNameLink( int propId )
        {
            return (propId == Props.LinkNameFrom) || (propId == Props.LinkNameTo) || (propId == Props.LinkNameCC);
        }

        public int GetNameLinkId( int propId )
        {
            #region Preconditions
            if( !IsMajorLink( propId ) )
                throw new ArgumentException( "ContactManager -- invalid link Id parameter" );
            #endregion Preconditions

            int resultId = Props.LinkNameCC;
            if( propId == Props.LinkFrom )
                resultId = Props.LinkNameFrom;
            else
            if( propId == Props.LinkTo )
                resultId = Props.LinkNameTo;
            return resultId;
        }

        public int GetAccountLinkId( int propId )
        {
            #region Preconditions
            if( !IsMajorLink( propId ) )
                throw new ArgumentException( "ContactManager -- invalid link Id parameter" );
            #endregion Preconditions

            int resultId = Props.LinkEmailAcctCC;
            if( propId == Props.LinkFrom )
                resultId = Props.LinkEmailAcctFrom;
            else
            if( propId == Props.LinkTo )
                resultId = Props.LinkEmailAcctTo;
            return resultId;
        }

        private int GetMajorLinkId( int propId )
        {
            if( propId == Props.LinkEmailAcctFrom )
                return Props.LinkFrom;
            else
            if( propId == Props.LinkEmailAcctTo )
                return Props.LinkTo;
            else
            if( propId == Props.LinkEmailAcctCC )
                return Props.LinkCC;
            else
                throw new ArgumentException( "ContactManager -- invalid Account link Id parameter" );
        }

        private int[]  GetAllAccountLinks( IResource mail, IResource account )
        {
            IntArrayList links = IntArrayListPool.Alloc();
            try
            {
                if( mail.HasLink( Props.LinkEmailAcctFrom, account ) )
                    links.Add( Props.LinkEmailAcctFrom );
                if( mail.HasLink( Props.LinkEmailAcctTo, account ) )
                    links.Add( Props.LinkEmailAcctTo );
                if( mail.HasLink( Props.LinkEmailAcctCC, account ) )
                    links.Add( Props.LinkEmailAcctCC );

                return links.ToArray();
            }
            finally
            {
                IntArrayListPool.Dispose( links );
            }
        }

        private static bool IsUniqueLink( int propId, string fromRT )
        {
            return (RStore.GetMaxLinkCountRestriction( fromRT, propId ) == 1);
        }

        public bool ResolveName( string fullName, string emailAccount,
                                 out string title, out string firstName,
                                 out string midName, out string lastName, string suffix )
        {
            string  addSpec;
            return ContactResolver.ResolveName( fullName, emailAccount, out title, out firstName,
                                                out midName, out lastName, out suffix, out addSpec );
        }

        public static int GetLinkedIdFromContactName( IResource cName )
        {
            #region Preconditions
            if( cName.Type != "ContactName" )
                throw new ArgumentException( "ContactManager -- invalid argument type [" + cName.Type + "] - ContactName expected." );
            #endregion Preconditions

            if( cName.GetLinkCount( -Core.ContactManager.Props.LinkFrom ) > 0 )
                return Core.ContactManager.Props.LinkFrom;
            else
            if( cName.GetLinkCount( -Core.ContactManager.Props.LinkTo ) > 0 )
                return Core.ContactManager.Props.LinkTo;
            else
            if( cName.GetLinkCount( -Core.ContactManager.Props.LinkCC ) > 0 )
                return Core.ContactManager.Props.LinkCC;
            else
                throw new InvalidConstraintException( "ContactManager -- Found a ContactName resource which is not linked to a primary resource." );
        }

        private IResource FindBestCandidate( IResourceList contacts )
        {
            int maxMails = -1, maxIndex = -1;
            for( int i = 0; i < contacts.Count; i++ )
            {
                int count = contacts[ i ].GetLinkCount( Props.LinkFrom ) +
                            contacts[ i ].GetLinkCount( Props.LinkTo ) +
                            contacts[ i ].GetLinkCount( Props.LinkCC );
                if( count > maxMails )
                {
                    maxMails = count;
                    maxIndex = i;
                }
            }
            return contacts[ maxIndex ];
        }

        public IContact GetContact( IResource res )
        {
            return new ContactBO( res );
        }

        public string  GetFullName( IResource res )
        {
            #region Preconditions
            if( res == null )
                throw new ArgumentNullException( "res", "ContactManager -- input contact can not be null" );

            if( res.Type != "Contact" )
                throw new ArgumentException( "ContactManager -- input parameter has unexpected resource type." );
            #endregion Preconditions

            return new ContactBO( res ).FullName;
        }

        /// <summary>
        /// Return a resource list of all the correspondence linked to this
        /// contact via "From", "To" or "CC" links.
        /// <para>NB: the method can be used in reversed order - given a
        /// correspondence resource method will return all contacts linked to
        /// it via "From", "To" or "CC" links.</para>
        /// </summary>
        /// <param name="contact">A contact resource.</param>
        /// <returns>Correspondence resources.</returns>
        public static IResourceList  LinkedCorrespondence( IResource contact )
        {
            IResourceList linked = contact.GetLinksOfType( null, Core.ContactManager.Props.LinkCC ).Union(
                                   contact.GetLinksOfType( null, Core.ContactManager.Props.LinkTo ).Union(
                                   contact.GetLinksOfType( null, Core.ContactManager.Props.LinkFrom ), true ), true );
            return linked;
        }

        /// <summary>
        /// Return a resource list of all the correspondence linked to this
        /// contact via "From" and "To" (no "CC") links.
        /// NB: the method can be used in reversed order - given a
        /// correspondence resource method will return all contacts linked to
        /// it via "From" and "To" links.
        /// </summary>
        /// <param name="contact">A contact resource.</param>
        /// <returns>Correspondence resources.</returns>
        public static IResourceList  LinkedCorrespondenceDirect( IResource contact )
        {
            IResourceList linked = contact.GetLinksOfType( null, Core.ContactManager.Props.LinkFrom ).Union(
                                   contact.GetLinksOfType( null, Core.ContactManager.Props.LinkTo ));
            return linked;
        }

        /// <summary>
        /// Return a resource list of all the correspondence linked to this
        /// contact name via "NameFrom", "NameTo" or "NameCC" links.
        /// NB: the method can be used in reversed order - given a
        /// correspondence resource method will return all contact names
        /// linked to it via "NameFrom", "NameTo" or "NameCC" links.
        /// </summary>
        /// <param name="res">A contact name resource.</param>
        /// <returns>Correspondence resources.</returns>
        private static IResourceList  LinkedNameCorrespondence( IResource res )
        {
            IResourceList linked = res.GetLinksOfType( null, Core.ContactManager.Props.LinkNameCC ).Union(
                                   res.GetLinksOfType( null, Core.ContactManager.Props.LinkNameTo ).Union(
                                   res.GetLinksOfType( null, Core.ContactManager.Props.LinkNameFrom )));
            return linked;
        }

        /// <summary>
        /// Return a resource list of all the correspondence linked to this
        /// account name via "AcctFrom", "AcctTo" or "AcctCC" links.
        /// NB: the method can be used in reversed order - given a
        /// correspondence resource method will return all accounts
        /// linked to it via "AcctFrom", "AcctTo" or "AcctCC" links.
        /// </summary>
        /// <param name="account">An account resource.</param>
        /// <returns>Correspondence resources.</returns>
        public static IResourceList  LinkedAccountCorrespondence( IResource account )
        {
            IResourceList linked = account.GetLinksOfType( null, Core.ContactManager.Props.LinkEmailAcctCC ).Union(
                                   account.GetLinksOfType( null, Core.ContactManager.Props.LinkEmailAcctTo ).Union(
                                   account.GetLinksOfType( null, Core.ContactManager.Props.LinkEmailAcctFrom )));
            return linked;
        }

        /// <summary>
        /// Return a resource list of all accounts linked to this
        /// contact via "EmailAccnt" link.
        /// </summary>
        /// <param name="contact">An contact resource.</param>
        /// <returns>Account resources.</returns>
        public static IResourceList  LinkedAccounts( IResource contact )
        {
            return contact.GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct );
        }

        public static IResourceList  LinkedAccounts( IResourceList contacts )
        {
            IResourceList accounts = Core.ResourceStore.EmptyResourceList;
            foreach( IResource res in contacts )
            {
                accounts = accounts.Union( LinkedAccounts( res ), true );
            }
            return accounts;
        }

        private static String CleanedName( String name )
        {
            if( name != null )
            {
                if( name.Length > 2 && name[ 0 ]=='\'' && name[ name.Length - 1 ]=='\'' )
                    name = name.Substring( 1, name.Length - 2 ).Trim();

                if( name.Length > 2 && name[ 0 ]=='"' && name[ name.Length - 1 ]=='"' )
                    name = name.Substring( 1, name.Length - 2 ).Trim();

                if( name[ 0 ] == '\n' )
                    name = name.Substring( 1, name.Length - 1 ).Trim();
            }

            return name;
        }
        #endregion Auxiliary

        #region Myself Contact
        public IContact MySelf
        {
            get
            {
                if ( _mySelfContact == null )
                {
                    Core.ResourceAP.RunUniqueJob( new MethodInvoker( CreateMyselfContact ) );
                }

                lock( this )
                {
                    //-------------------------------------------------------------
                    //  Create a resource list which tracks the removal of the MySelf
                    //  contact, e.g. in the case of merging and splitting.
                    //-------------------------------------------------------------
                    if( _MySelfTrackingList == null )
                    {
                        _MySelfTrackingList = _mySelfContact.Resource.ToResourceListLive();
                        _MySelfTrackingList.ResourceDeleting += _MySelfTrackingList_ResourceDeleting;
                    }
                }

//                return new ContactBO( _mySelfContact.Resource );
                return _mySelfContact;
            }
        }

        private void CreateMyselfContact()
        {
            Trace.WriteLine( "ContactManager -- Myself contact is empty" );
            MergeSeveralMyselfIfAny();

            IResourceList mySelves = RStore.FindResources( "Contact", "MySelf", 1 );
            if( mySelves.Count == 0 )
            {
                CreateMySelfResource();
                mySelves = RStore.FindResources( "Contact", "MySelf", 1 );
            }
            if( RStore.PropTypes.Exist( "LastCorrespondDate" ) )
            {
                int propId = Core.ResourceStore.PropTypes[ "LastCorrespondDate" ].Id;
                mySelves.Sort( new SortSettings( propId, false ) );
            }
            int index = 0;
            for ( int i = 0; i < mySelves.Count ; i++ )
            {
                if ( mySelves[ i ].HasProp( "LastCorrespondDate" ) )
                {
                    index = i;
                    break;
                }
            }
            _mySelfContact = new ContactBO( mySelves[ index ] );

            //  Still empty FN and LN? It means that we have to take
            //  information from the Environment and cross the fingers
            //  that it is valid.
            if( _mySelfContact.FirstName == string.Empty &&
                _mySelfContact.LastName == string.Empty )
            {
                Trace.WriteLine( "ContactManager -- FN and LN are still empty - analyzing log name" );
                string userName = SystemInformation.UserName;
                if( !string.IsNullOrEmpty( userName ))
                {
                    Trace.WriteLine( "ContactManager -- Log name is valid - [" + userName + "]" );

                    string title, fName, midName, lName, suffix, addSpec;
                    ExtractPossibleFields( userName, out title, out fName, out midName, out lName, out suffix, out addSpec );

                    _mySelfContact.Title = title;
                    _mySelfContact.FirstName = fName;
                    _mySelfContact.MiddleName = midName;
                    _mySelfContact.LastName = lName;
                    _mySelfContact.Suffix = suffix;
                    _mySelfContact.QueueIndexing();
                    Trace.WriteLine( "ContactManager -- Result fields are: [" + title + "][" + fName + "][" +
                                     midName + "][" + lName + "][" + suffix + "][" + addSpec + "]");
                }
            }
        }

        private static void CreateMySelfResource()
        {
            //-----------------------------------------------------------------
            //  We have to check the presence of the MySelf contact once more
            //  because of cases when caller's getter is called several times
            //  and myself resource is not created yet.
            //  Thus we check here whether previous Job has just created the
            //  necessary object and do not create the second copy.
            //-----------------------------------------------------------------
            IResourceList mySelves = RStore.FindResources( "Contact", "MySelf", 1 );
            if( mySelves.Count == 0 )
            {
                IResource myself = RStore.NewResource( "Contact" );
                myself.SetProp( "MySelf", 1 );
            }
        }

        private void  MergeSeveralMyselfIfAny()
        {
            IResourceList mySelves = RStore.FindResources( "Contact", "MySelf", 1 );
            if( mySelves.Count > 1 )
            {
                if( RStore.PropTypes.Exist( "LastCorrespondDate" ) )
                {
                    int propId = Core.ResourceStore.PropTypes[ "LastCorrespondDate" ].Id;
                    mySelves.Sort( new SortSettings( propId, false ) );
                }

                ContactBO target = new ContactBO( mySelves[ 0 ] );
                for( int i = 1; i < mySelves.Count; i++ )
                    MergeData( new ContactBO( mySelves[ i ] ), target );

                for( int i = 1; i < mySelves.Count; i++ )
                {
                    mySelves[ i ].Delete();
                }

                if( RStore.FindResources( "Contact", "MySelf", 1 ).Count != 1 )
                    throw new ApplicationException( "Merging exception: Myself count still out of range" );
            }
        }

        //  Mark the variable _mySelfContact as null so that it will be
        //  recreated on the next getter call.
        private void _MySelfTrackingList_ResourceDeleting(object sender, ResourceIndexEventArgs e)
        {
            _mySelfContact = null;
            _MySelfTrackingList.ResourceDeleting -= _MySelfTrackingList_ResourceDeleting;
            _MySelfTrackingList = null;
            _myselfAccounts = Core.ResourceStore.EmptyResourceList;
        }

        //  Implement simple heuristic - if user name is a compound of two
        //  (valid) strings delimited by the dot, use compounds as first name
        //  and last name correspondingly.
        private static void  ExtractPossibleFields( string name,
                                                    out string title, out string fName, out string midName,
                                                    out string lName, out string suffix, out string addSpec )
        {
            string[] dotDelimitedFields = name.Split( '.' );
            if( dotDelimitedFields.Length == 2 &&
                !string.IsNullOrEmpty( dotDelimitedFields[ 0 ] ) && !string.IsNullOrEmpty( dotDelimitedFields[ 1 ] ))
            {
                fName = dotDelimitedFields[ 0 ];
                lName = dotDelimitedFields[ 1 ];
                title = midName = suffix = addSpec = "";
            }
            else
            {
                ContactResolver.ResolveName( name, null, out title, out fName,
                                             out midName, out lName, out suffix, out addSpec );
            }
        }
        #endregion Myself Contact
    }

    #region FiltersAndActions
    public class ItemRecipientsFilter : ILinksPaneFilter
    {
        private readonly ContactManager _contactMgr;

        public ItemRecipientsFilter()
        {
            _contactMgr = Core.ContactManager as ContactManager;
        }

        public bool AcceptLinkType(IResource baseRes, int propId, ref string displayName)
        {
            return !_contactMgr.IsNameLink(propId);
        }

        public bool AcceptLink(IResource baseRes, int propId, IResource contact, ref string linkTooltip)
        {
            #region Preconditions
            if( baseRes == null )
                throw new ArgumentNullException( "baseRes", "ContactManager -- Source object is null in filter.");

            if( contact == null )
                throw new ArgumentNullException( "contact", "ContactManager -- Target contact is null in filter.");
            #endregion Preconditions

            if (_contactMgr.IsMajorLink(propId) && contact.Type == "Contact")
            {
                bool showOrig = contact.HasProp(_contactMgr.Props.ShowOriginalNames);
                int nameLinkId = _contactMgr.GetNameLinkId(propId);
                int accountLinkId = _contactMgr.GetAccountLinkId(propId);

                IResource cName;
                IResourceList contactAccounts = contact.GetLinksOfType(null, _contactMgr.Props.LinkEmailAcct);
                IResourceList mailAccounts = baseRes.GetLinksOfType(null, accountLinkId);

                //  Link to direct contact may be rejected if a contact requires
                //  showing of original name, and there is such ContactName for
                //  that contact linked to source resource.
                if (showOrig && (cName = GetContactNamebyPair(baseRes, nameLinkId, contact)) != null)
                {
                    linkTooltip = cName.GetStringProp(Core.Props.Name);
                    if (contactAccounts.Count == 1)
                        linkTooltip = linkTooltip + " <" + contactAccounts[0].DisplayName + ">";
                }
                else
                {
                    mailAccounts = mailAccounts.Intersect(contactAccounts, true);
                    if (mailAccounts.Count > 0)
                    {
                        linkTooltip = string.Empty;
                        foreach (IResource account in mailAccounts)
                            linkTooltip += contact.DisplayName + " <" + account.DisplayName + ">";
                    }
                }
            }
            return true;
        }

        public bool AcceptAction(IResource displayedResource, IAction action)
        {
            return true;
        }

        private IResource GetContactNamebyPair(IResource mail, int nameLinkId, IResource contact)
        {
            IResourceList cNames = mail.GetLinksOfType(null, nameLinkId);
            foreach (IResource name in cNames)
            {
                if (name.HasLink(_contactMgr.Props.LinkBaseContact, contact))
                    return name;
            }
            return null;
        }
    }
    #endregion FiltersAndActions
}
