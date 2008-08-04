/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    public class ExportContactDescriptor : AbstractNamedJob
    {
        private IResource _contact;
        private bool _createNew;
        private ResourceTracer _resTracer = new ResourceTracer( "ExportContactDescriptor" );
        private static Tracer _tracer = new Tracer( "ExportContactDescriptor" );
        private static IResource _lastContact = null;
        private IResource _AB = null;
        private bool _createdAsMailUser = false;
        private bool _newCreated = false;

        private static ExportContactDescriptor _staticDescriptor = new ExportContactDescriptor();

        public ExportContactDescriptor( IResource contact )
        {
            Guard.NullArgument( contact, "contact" );
            _createNew = false;
            _contact = contact;
            CheckIsContactValid( contact );
        }
        public ExportContactDescriptor( IResource contact, IResource AB )
        {
            Guard.NullArgument( contact, "contact" );
            Guard.NullArgument( AB, "AB" );
            _AB = AB;
            _createNew = true;
            _contact = contact;
            CheckIsContactValid( contact );
        }
        private ExportContactDescriptor( )
        {
        }
        public static IResource LastExportedContact { get { return _lastContact; } }

        internal static void CheckIsContactValid( IResource contact )
        {
            IContact contactBO = Core.ContactManager.GetContact( contact );
            if ( !ContactNames.IsValidString( contactBO.FirstName ) &&
                !ContactNames.IsValidString( contactBO.MiddleName ) &&
                !ContactNames.IsValidString( contactBO.LastName ) &&
                contact.GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct ).Count == 0 )
            {
                throw new Exception( "Invalid contact was constructed" );
            }
        }

        private static void Trace( string message )
        {
            Trace( message, true );
        }

        private static void Trace( string message, bool trace )
        {
            if ( Settings.TraceContactChanges && trace )
            {
                _tracer.Trace( message );
            }
        }

        internal static IEFolder GetAddressBookFolder( IResource AB )
        {
            Guard.NullArgument( AB, "AB" );
            string entryID = AB.GetStringProp( PROP.EntryID );
            string storeID = AB.GetStringProp( PROP.StoreID );

            if ( entryID == null || storeID == null )
            {
                Trace( "ERROR: Can't export contact because address book has not entryID and storeID", false );
                Trace( "WARNING: Probably this address book is unmodifiable", false );
                return null;
            }
            return OutlookSession.OpenFolder( entryID, storeID );
        }

        internal static IEFolder GetContactFolder( IResource contact, bool trace )
        {
            IResource AB = contact.GetLinkProp( "InAddressBook" );
            if ( AB == null )
            {
                Trace( "ERROR: Can't export contact because it is not connected to address book", false );
                return null;
            }
            return GetAddressBookFolder( AB );
        }

        private bool IsClearNeededImpl( IResource contact )
        {
            IEFolder folder = GetContactFolder( contact, false );
            if ( folder == null )
            {
                return false;
            }

            bool newCreated;
            IEMAPIProp message = OpenMessage( folder, contact, false, false, false, out newCreated );
            if ( message == null )
            {
                return true;
            }
            using ( message )
            {
                string folderID = message.GetBinProp( MAPIConst.PR_PARENT_ENTRYID );
                FolderDescriptor folderDescriptor = FolderDescriptor.Get( folderID, message.GetBinProp( MAPIConst.PR_STORE_ENTRYID ) );
                if ( folderDescriptor == null || folderDescriptor.ContainerClass != FolderType.Contact )
                {
                    return true;
                }
                return false;
            }
        }

        public static bool IsClearNeeded( IResource contact )
        {
            return _staticDescriptor.IsClearNeededImpl( contact );
        }

        public IEMAPIProp OpenMessage( IEFolder folder, IResource contact, bool create, bool forceCreate, bool trace, out bool newCreated )
        {
            bool       createdAsMailUser;
            IEMAPIProp prop = OpenMessage( folder, contact, create, forceCreate,
                                           trace, out newCreated, out createdAsMailUser );

            //  "OpenMessage" set the value for "_createdAsMailUser" only in the case
            //  of succeed action, leaving the var unset in other case.
            if( createdAsMailUser )
            {
                _createdAsMailUser = createdAsMailUser;
            }
            return prop;
        }

        internal static IEMAPIProp OpenMessage( IEFolder folder, IResource contact,
                                                bool create, bool forceCreate, bool trace,
                                                out bool newCreated, out bool createdAsMailUser )
        {
            createdAsMailUser = false;
            newCreated = false;
            using ( folder )
            {
                IEMAPIProp message = null;
                if ( !create )
                {
                    string mesEntryId = contact.GetPropText( PROP.EntryID );
                    if ( mesEntryId.Length > 0 )
                    {
                        message = OutlookSession.OpenMessage( folder, mesEntryId );
                        if ( message == null )
                        {
                            Contact.RemoveFromSync( contact, true );
                        }
                    }
                    if ( !forceCreate )
                    {
                        return message;
                    }
                }
                if ( message == null )
                {
                    string folderId = folder.GetBinProp( MAPIConst.PR_ENTRYID );
                    _tracer.Trace( folderId );
                    IEAddrBook ab = OutlookSession.GetAddrBook();
                    if ( ab != null )
                    {
                        for ( int i = 0; i < ab.GetCount(); ++i )
                        {
                            string abEntryId = ab.FindBinProp( i, MAPIConst.PR_ENTRYID_ASSOCIATED_WITH_AB );
                            if ( abEntryId == folderId )
                            {
                                IEABContainer abContainer = ab.OpenAB( i );
                                if ( abContainer != null )
                                {
                                    using ( abContainer )
                                    {
                                        message = abContainer.CreateMailUser( );
                                        if ( message != null )
                                        {
                                            createdAsMailUser = true;
                                            return message;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                    message = folder.CreateMessage( "IPM.Contact" );
                    newCreated = true;
                }
                return message;
            }
        }

        private IEMAPIProp OpenMessage( IResource contact, bool create, bool forceCreate, bool trace )
        {
            IEFolder folder = null;
            if ( _AB != null )
            {
                folder = GetAddressBookFolder( _AB );
            }
            else
            {
                folder = GetContactFolder( contact, trace );
            }
            if ( folder == null )
            {
                return null;
            }
            return OpenMessage( folder, contact, create, forceCreate, trace, out _newCreated );
        }
        protected override void Execute()
        {
            if ( Settings.TraceContactChanges )
            {
                Trace( "Try to export contact" );
                _resTracer.Trace( _contact );
            }
            IEMAPIProp message = OpenMessage( _contact, _createNew, true, true );

            if ( message == null )
            {
                if ( _newCreated )
                {
                    _tracer.Trace( "Cannot Export NEW contact for resource id = " + _contact.Id );
                }
                _tracer.Trace( "Cannot Export contact for resource id = " + _contact.Id );
                return;
            }
            using ( message )
            {
                IContact contactBO = Core.ContactManager.GetContact( _contact );
                ExportProperties( message, contactBO );
                ExportPhones( contactBO, message );
                SetEmailAddress( message );
                if ( Settings.SyncContactCategory )
                {
                    SetCategories( message );
                }
                _lastContact = _contact;
                OutlookSession.SaveChanges( _newCreated, "Export contact for resource id = " + _contact.Id, message, message.GetBinProp( MAPIConst.PR_ENTRYID ) );
            }
        }
        private void SetCategories( IEMAPIProp message )
        {
            ExportCategories.ProcessCategories( message, _contact );
        }

        private void ExportProperties( IEMAPIProp message, IContact contactBO )
        {
            if ( !_createdAsMailUser )
            {
                int tag = message.GetIDsFromNames( ref GUID.set3, lID.contactDisplayName, PropType.PT_STRING8 );
                message.SetStringProp( tag, _contact.DisplayName );
            }

            //  LX: This causes setting of the contact's email address equal to the
            //      "DisplayName" if it is not set later explicitely.
            //  message.SetStringProp( MAPIConst.PR_DISPLAY_NAME, _contact.DisplayName );
            if( !String.IsNullOrEmpty( contactBO.Title ) )
            {
                message.SetStringProp( MAPIConst.PR_DISPLAY_NAME_PREFIX, contactBO.Title );
            }
            message.SetStringProp( MAPIConst.PR_GIVEN_NAME, contactBO.FirstName );
            if( !String.IsNullOrEmpty( contactBO.MiddleName ) )
            {
                message.SetStringProp( MAPIConst.PR_MIDDLE_NAME, contactBO.MiddleName );
            }
            message.SetStringProp( MAPIConst.PR_SURNAME, contactBO.LastName );
            if( !String.IsNullOrEmpty( contactBO.Suffix ) )
            {
                message.SetStringProp( MAPIConst.PR_GENERATION, contactBO.Suffix );
            }
            message.SetDateTimeProp( MAPIConst.PR_BIRTHDAY, contactBO.Birthday );
            message.SetStringProp( MAPIConst.PR_COMPANY_NAME, contactBO.Company );
            message.SetStringProp( MAPIConst.PR_BUSINESS_HOME_PAGE, contactBO.HomePage );
            message.SetStringProp( MAPIConst.PR_TITLE, contactBO.JobTitle );
            message.SetStringProp( MAPIConst.PR_POSTAL_ADDRESS, contactBO.Address );
            message.WriteStringStreamProp( MAPIConst.PR_BODY, contactBO.Description );
        }

        private void SetEmailAddress( IEMAPIProp message )
        {
            IResourceList emails = _contact.GetLinksOfType( "EmailAccount",
                                                            Core.ContactManager.Props.LinkEmailAcct );
            if ( emails.Count > 0 )
            {
                IResource email = emails[ 0 ];
                string emailText = email.GetPropText( Core.ContactManager.Props.EmailAddress );
                if ( emailText.Length > 0 )
                {
                    int tag = message.GetIDsFromNames( ref GUID.set3, lID.contactEmail, PropType.PT_STRING8 );
                    message.SetStringProp( tag, emailText );
                    message.SetStringProp( MAPIConst.PR_EMAIL_ADDRESS, emailText );
                    message.SetStringProp( MAPIConst.PR_CONTACT_EMAIL_ADDRESS, emailText );
                    message.SetStringProp( MAPIConst.PR_CONTACT_EMAIL_ADDRESS1, emailText );
                }
            }
        }

        private void ExportPhones( IContact contactBO, IEMAPIProp message )
        {
            string otherCandidat = null;
            bool wasOther = false;
            foreach ( string phoneName in contactBO.GetPhoneNames() )
            {
                if ( phoneName == Phone.Other.Name )
                {
                    wasOther = true;
                }
                Phone phone = Phone.GetPhone( phoneName );
                if ( phone != null )
                {
                    message.SetStringProp( phone.MAPIPhoneAsInt, contactBO.GetPhoneNumber( phoneName ) );
                }
                else if ( otherCandidat == null )
                {
                    otherCandidat = phoneName;
                }
            }
            if ( !wasOther && otherCandidat != null )
            {
                message.SetStringProp( Phone.Other.MAPIPhoneAsInt, contactBO.GetPhoneNumber( otherCandidat ) );
            }
            if ( !wasOther && otherCandidat == null )
            {
                message.SetStringProp( Phone.Other.MAPIPhoneAsInt, "" );
            }
        }

        public override string Name
        {
            get { return "Exporting contact"; }
        }
    }

    public class ExportContactCategoryDescriptor : AbstractNamedJob
    {
        private static Tracer _tracer = new Tracer( "ExportContactDescriptor" );
        private IResource _contact;
        private bool _newCreated = false;

        public ExportContactCategoryDescriptor( IResource contact )
        {
            Guard.NullArgument( contact, "contact" );
            _contact = contact;
            ExportContactDescriptor.CheckIsContactValid( contact );
        }

        private static IEMAPIProp OpenMessage( IResource contact, bool create, bool forceCreate, bool trace )
        {
            IEMAPIProp  prop = null;
            IEFolder    folder = ExportContactDescriptor.GetContactFolder( contact, false );
            if ( folder != null )
            {
                bool    foo, _newCreated;
                prop = ExportContactDescriptor.OpenMessage( folder, contact, create, forceCreate,
                                                            trace, out _newCreated, out foo );
            }
            return prop;
        }

        protected override void Execute()
        {
            IEMAPIProp message = OpenMessage( _contact, false, true, true );

            if ( message == null )
            {
                if ( _newCreated )
                {
                    _tracer.Trace( "Cannot Export NEW contact for resource id = " + _contact.Id );
                }
                _tracer.Trace( "Cannot Export contact for resource id = " + _contact.Id );
                return;
            }
            using ( message )
            {
                ExportCategories.ProcessCategories( message, _contact );
                OutlookSession.SaveChanges( _newCreated, "Export contact for resource id = " + _contact.Id, message, message.GetBinProp( MAPIConst.PR_ENTRYID ) );
            }
        }

        public override string Name
        {
            get { return "Exporting contact"; }
        }
    }
}