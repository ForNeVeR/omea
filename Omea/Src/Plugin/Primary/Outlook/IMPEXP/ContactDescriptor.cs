/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class ContactDescriptor : AbstractNamedJob
    {
        private static ResourceTracer _tracer = new ResourceTracer( "ContactDescriptor" );
        private string _entryID;
        private string _searchEntryID;
        private ContactNames _contactNames = new ContactNames();
        private string _companyName = string.Empty;
        private string _jobTitle = string.Empty;
        private string _streetAddress = string.Empty;
        private string _businessHomePage = string.Empty;
        private string _description = string.Empty;
        
        private DateTime _birthday = DateTime.MinValue;
        private OutlookAddressBook _addressBook;
        private ArrayList _phones = new ArrayList();
        private ArrayList _outlookCategories;
        private ArrayList _emailAddresses;

        public ContactDescriptor( IEMessage message, string entryID, string searchEntryID, OutlookAddressBook addressBook )
        {
            _searchEntryID = searchEntryID;
            _entryID = entryID;
            _addressBook = addressBook;
            SetFullName( message );

            _emailAddresses = GetEmailAddresses();

            if ( _emailAddresses == null || _emailAddresses.Count == 0 )
            {
                string tempString = message.GetStringProp( MAPIConst.PR_EMAIL_ADDRESS );
                if ( tempString != null )
                {
                    _contactNames.EmailAddress = tempString;
                }
                else
                {
                    int tag = message.GetIDsFromNames( ref GUID.set3, lID.contactEmail, PropType.PT_STRING8 );
                    tempString = message.GetStringProp( tag );
                    if ( tempString != null )
                    {
                        _contactNames.EmailAddress = tempString;
                    }
                }
            }
            else
            {
                _contactNames.EmailAddress = _emailAddresses[0] as string;
            }

            _birthday = message.GetDateTimeProp( MAPIConst.PR_BIRTHDAY );
            foreach ( Phone phone in Phone.GetPhones() )
            {
                _phones.Add( new Pair( phone, message.GetStringProp( phone.MAPIPhoneAsInt ) ) );
            }
            LoadProp( ref _companyName, message, MAPIConst.PR_COMPANY_NAME );
            LoadProp( ref _jobTitle, message, MAPIConst.PR_TITLE );
            LoadProp( ref _streetAddress, message, MAPIConst.PR_POSTAL_ADDRESS );
            LoadProp( ref _businessHomePage, message, MAPIConst.PR_BUSINESS_HOME_PAGE );

            string suffix = string.Empty;
            LoadProp( ref suffix, message, MAPIConst.PR_GENERATION );
            _contactNames.Suffix = suffix;
            string middleName = string.Empty;
            LoadProp( ref middleName, message, MAPIConst.PR_MIDDLE_NAME );
            _contactNames.MiddleName = middleName;
            string namePrefix = string.Empty;
            LoadProp( ref namePrefix, message, MAPIConst.PR_DISPLAY_NAME_PREFIX );
            _contactNames.NamePrefix = namePrefix;
            LoadStreamProp( ref _description, message );
            _outlookCategories = OutlookSession.GetCategories( message );
        }

        public ContactDescriptor( IERowSet rowSet, int rowNum, string entryID, OutlookAddressBook addressBook )
        {
            _entryID = entryID;
            _searchEntryID = _entryID;
            _addressBook = addressBook;

            _emailAddresses = GetEmailAddresses( );
            if ( _emailAddresses == null || _emailAddresses.Count == 0 )
            {
                _contactNames.EmailAddress = rowSet.GetStringProp( 2, rowNum );//row.FindStringProp( MAPIConst.PR_EMAIL_ADDRESS );
            }
            else
            {
                _contactNames.EmailAddress = _emailAddresses[0] as string;
            }

            string fullName = rowSet.GetStringProp( 1, rowNum );//row.FindStringProp( MAPIConst.PR_DISPLAY_NAME );
            if( fullName != null )
            {
                fullName = fullName.Trim();
            }
            _contactNames.FullName = fullName;
            //_phones = contactProperties.GetPhones();
            //_birthday = contactProperties.BirthDay;
        }
        private string ExtractAddress( string email )
        {
            Guard.EmptyStringArgument( email, "email" );
            if ( string.Compare( email, "x400", true ) != 0 )
            {
                int index = email.IndexOf( ':' );
                return email.Substring( index + 1 ).Trim( );
            }
            return null;
        }
        private ArrayList GetEmailAddresses( /*string entryId*/ )
        {
            IEAddrBook ab = OutlookSession.GetAddrBook();
            IEMailUser mailUser = ab.OpenMailUser( _entryID );
            if ( mailUser != null )
            {
                using ( mailUser )
                {
                    ArrayList addresses = mailUser.GetStringArray( MAPIConst.PR_EMS_AB_PROXY_ADDRESSES );
                    if ( addresses != null && addresses.Count > 0 )
                    {
                        ArrayList emailAddresses = new ArrayList( addresses.Count );
                        foreach ( string str in addresses )
                        {
                            string newAddress = ExtractAddress( str );
                            if ( newAddress != null )
                            {
                                emailAddresses.Add( newAddress );
                            }
                        }
                        return emailAddresses;
                    }
                }
            }
            return null;
        }
        private void LoadProp( ref string member, IEMessage message, int propTag )
        {
            string prop = message.GetStringProp( propTag );
            if ( prop != null ) { member = prop; }
        }
        private void LoadStreamProp( ref string member, IEMessage message )
        {
            string prop = message.GetPlainBody();
            if ( prop != null ) { member = prop; }
        }

        private void SetFullName( IEMessage message )
        {
            string tempString = message.GetStringProp( MAPIConst.PR_GIVEN_NAME );
            _contactNames.FirstName = tempString;
            string fullName = null;
            if ( tempString != null )
            {
                fullName = tempString;
            }
            tempString = message.GetStringProp( MAPIConst.PR_SURNAME );
            _contactNames.LastName = tempString;
            if ( tempString != null )
            {
                if ( fullName != null )
                {
                    fullName += " ";
                }
                fullName += tempString;
            }
            if ( fullName == null )
            {
                int tag = message.GetIDsFromNames( ref GUID.set3, lID.contactDisplayName, PropType.PT_STRING8 );
                fullName = message.GetStringProp( tag );
            }
            if( fullName != null )
            {
                fullName = fullName.Trim();
            }
            _contactNames.FullName = fullName;
        }

        protected override void Execute()
        {
            if ( !_contactNames.CheckIfNamesValid() )
            {
                return;
            }
            ImportedContactsChangeWatcher.ProcessingImportFromOutlook = true;

            if ( _searchEntryID == null )
            {
                _searchEntryID = _entryID;
            }

            IContact contactBO = _contactNames.FindOrCreateContact( _searchEntryID );
            contactBO.BeginUpdate();

            if ( _emailAddresses != null && _emailAddresses.Count > 1 )
            {
                for ( int i = 1; i < _emailAddresses.Count; ++i )
                {
                    contactBO.AddAccount( (string)_emailAddresses[i] );
                }
            }

            IResource person = contactBO.Resource;

            _addressBook.AB().AddContact( person );
            person.SetProp( PROP.Imported, 1 );
            person.DeleteProp( "UserCreated" );
            if ( person.HasProp( PROP.EntryID ) && person.GetStringProp( PROP.EntryID ) != _entryID )
            {
                person.SetProp( PROP.EntryID, _entryID );
            }
            else
            {
                person.SetProp( PROP.EntryID, _entryID );
            }

            Pair otherPhone = null;
            foreach ( Pair pair in _phones )
            {
                string phoneNumber = (string)pair.Second;
                if ( phoneNumber != null && phoneNumber.Length > 0 )
                {
                    Phone phone = (Phone)pair.First;
                    if ( phone.Name == Phone.Other.Name )
                    {
                        otherPhone = pair;
                    }
                    else
                    {
                        contactBO.SetPhoneNumber( phone.Name, phoneNumber );
                    }
                }
            }
            if ( otherPhone != null && !contactBO.PhoneNumberExists( (string)otherPhone.Second ) )
            {
                contactBO.SetPhoneNumber( Phone.Other.Name, (string)otherPhone.Second );
            }

            contactBO.Company = _companyName;
            contactBO.JobTitle = _jobTitle;
            contactBO.Address = _streetAddress;
            contactBO.HomePage = _businessHomePage;
            contactBO.Suffix = _contactNames.Suffix;
            contactBO.MiddleName = _contactNames.MiddleName;
            contactBO.Title = _contactNames.NamePrefix;
            contactBO.Description = _description;
            if ( Settings.SyncContactCategory )
            {
                CategorySetter.DoJob( _outlookCategories, person );
            }
            
            if ( _birthday > DateTime.MinValue || contactBO.Birthday.CompareTo( _birthday ) != 0 )
            {
                contactBO.Birthday = _birthday;
            }
            
            contactBO.EndUpdate();
            ImportedContactsChangeWatcher.ProcessingImportFromOutlook = false;
            if ( Settings.TraceContactChanges )
            {
                _tracer.Trace( contactBO.Resource );
            }
        }

        public override string Name
        {
            get { return "Importing contact"; }
        }
    }
    
    internal class ContactDescriptorWrapper : AbstractNamedJob
    {
        private string _entryID;
        private string _searchEntryID;
        
        private FolderDescriptor _folder;

        public static void Do( FolderDescriptor folder, string entryID, string searchEntryID )
        {
            Do( JobPriority.Normal, folder, entryID, searchEntryID );
        }
        public static void Do( JobPriority jobPriority, FolderDescriptor folder, string entryID, string searchEntryID )
        {
            Guard.NullArgument( folder, "folder" );
            if ( IsDataCorrect( folder ) )
            {
                OutlookSession.OutlookProcessor.QueueJob( jobPriority, 
                    new ContactDescriptorWrapper( folder, entryID, searchEntryID ) );
            }
        }
        private ContactDescriptorWrapper( FolderDescriptor folder, string entryID, string searchEntryID )
        {
            _folder = folder;
            _entryID = entryID;
            _searchEntryID = searchEntryID;
        }
        private static bool IsDataCorrect( FolderDescriptor folder )
        {
            IResource resource = Folder.Find( folder.FolderIDs.EntryId );
            if ( resource == null || !Folder.IsFolderOfType( resource, FolderType.Contact ) || 
                Folder.IsIgnoreImport( resource ) )
            {
                return false;
            }
            return true;
        }

        protected override void Execute()
        {
            if ( IsDataCorrect( _folder ) )
            {
                PairIDs folderIDs = _folder.FolderIDs;
                IEMessage message = OutlookSession.OpenMessage( _entryID, folderIDs.StoreId );
                if ( message == null )
                {
                    return;
                }
                using ( message )
                {
                    OutlookAddressBook AB = new OutlookAddressBook( _folder.Name, _folder.FolderIDs, true );
                    ContactDescriptor contactDescriptor = 
                        new ContactDescriptor( message, _entryID, _searchEntryID, AB );
                    Core.ResourceAP.QueueJob( contactDescriptor );
                }
            }
        }

        public override string Name
        {
            get { return "importing contacts"; }
        }
    }
}
