// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.OutlookPlugin;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class ContactNames
    {
        private string _firstName;
        private string _lastName;
        private string _fullName;
        private string _suffix = string.Empty;
        private string _middleName = string.Empty;
        private string _namePrefix = string.Empty;
        private string _emailAddress = string.Empty;
        private bool _namesExist = false;
        public ContactNames(){}
        public ContactNames( string namePrefix, string firstName, string middleName, string lastName, string suffix, string fullName, string emailAddress, bool namesExist )
        {
            _namesExist = namesExist;
            _fullName = fullName;
            _firstName = firstName;
            _namePrefix = namePrefix;
            _middleName = middleName;
            _lastName = lastName;
            _suffix = suffix;
            _emailAddress = emailAddress;
        }
        public string FirstName
        {
            get { return _firstName; }
            set { _firstName = value; }
        }
        public string LastName
        {
            get { return _lastName; }
            set { _lastName = value; }
        }
        public string FullName
        {
            get { return _fullName; }
            set { _fullName = value; }
        }
        public string MiddleName
        {
            get { return _middleName; }
            set { _middleName = value; }
        }
        public string NamePrefix
        {
            get { return _namePrefix; }
            set { _namePrefix = value; }
        }
        public string Suffix
        {
            get { return _suffix; }
            set { _suffix = value; }
        }
        public string EmailAddress
        {
            get { return _emailAddress; }
            set { _emailAddress = value; }
        }
        private void UpdateFields( IContact contact )
        {
            if ( _namesExist )
            {
                contact.UpdateNameFields( _namePrefix, _firstName, _middleName, _lastName, _suffix );
            }
            else
            {
                contact.UpdateNameFields( _fullName );
            }
        }
        public static bool IsValidString( string str )
        {
            return !string.IsNullOrEmpty( str );
        }
        public bool CheckIfNamesValid()
        {
            _namesExist = IsValidString( FirstName ) || IsValidString( LastName );
            if ( !_namesExist && !IsValidString( FullName ) )
            {
                return false;
            }
            return true;
        }
        private IContact CreateContact()
        {
            return _namesExist
                ? Core.ContactManager.CreateContact( _namePrefix, _firstName, _middleName, _lastName, _suffix )
                : Core.ContactManager.CreateContact( _fullName );
        }

        public IContact FindOrCreateContact( string entryID )
        {
            if ( !CheckIfNamesValid() )
            {
                return null;
            }
            IContact contactBO = null;
            bool isMyself = OwnerEmailDetector.IsOwnerEmail( _emailAddress );

            IResource person = Contact.FindByEntryID( entryID );
            if ( person == null && isMyself )
            {
                contactBO = Core.ContactManager.MySelf;
                if ( contactBO.Resource.HasProp( PROP.EntryID ) && contactBO.Resource.GetStringProp( PROP.EntryID ) != entryID )
                {
                    contactBO = FindOrCreateRegularContact(entryID);
                }
                else
                {
                    UpdateFields( contactBO );
                }
                contactBO.AddAccount( _emailAddress );
            }
            else
            {
                if ( person != null )
                {
                    contactBO = Core.ContactManager.GetContact( person );
                    UpdateFields( contactBO );
                    contactBO.AddAccount( _emailAddress );
                }
                else
                {
                    contactBO = FindOrCreateRegularContact( entryID );
                }
            }
            return contactBO;
        }

        private IContact CheckContactByEmail( IContact contact )
        {
            return contact;
            /*
            IResourceList emailAccts = contact.Resource.GetLinksOfType( STR.EmailAccount, "EmailAcct" );
            foreach ( IResource account in emailAccts )
            {
                if ( account.GetStringProp( "EmailAddress" ) == _emailAddress )
                {
                    return contact;
                }
            }
            return null;
            */
        }
        private bool CheckNames( IContact contact )
        {
            if ( _namesExist )
            {
                return (
                    contact.FirstName == _firstName &&
                    contact.LastName == _lastName &&
                    contact.Suffix == _suffix &&
                    contact.MiddleName == _middleName &&
                    contact.Title == _namePrefix );
            }
            else
            {
                return ( contact.Resource.DisplayName == _fullName );
            }
        }
        private IContact TryGettingLastExportedContact()
        {
            IResource lastExportedContact = ExportContactDescriptor.LastExportedContact;
            if ( lastExportedContact != null && !lastExportedContact.HasProp( PROP.EntryID ) )
            {
                IContact contact = Core.ContactManager.GetContact( lastExportedContact );
                if ( CheckNames( contact ) )
                {
                    return CheckContactByEmail( contact );
                }
            }
            return null;
        }

        private IContact FindOrCreateRegularContact( string entryID )
        {
            IContact contactBO = null;
            IResource resource = Contact.FindByEntryID( entryID );
            if ( resource != null )
            {
                contactBO = Core.ContactManager.GetContact( resource );
                UpdateFields( contactBO );
                contactBO.AddAccount( _emailAddress );
            }
            else
            {
                contactBO = TryGettingLastExportedContact();
                if ( contactBO != null )
                {
                    UpdateFields( contactBO );
                    contactBO.AddAccount( _emailAddress );
                    return contactBO;
                }
                IResourceList contacts = _namesExist
                    ? Core.ContactManager.FindContactList( _namePrefix, _firstName, _middleName, _lastName, _suffix )
                    : Core.ContactManager.FindContactList( _fullName );

                IResourceList contactsWithEmail = null;
                IResource accntRes = Core.ContactManager.FindOrCreateEmailAccount( _emailAddress );
                if ( accntRes != null )
                {
                    contactsWithEmail = accntRes.GetLinksOfType( "Contact", "EmailAcct" );
                    contacts = contacts.Intersect( contactsWithEmail, true );
                }
                IResource candidat = null;
                foreach ( IResource res in contacts )
                {
                    if ( res.HasProp( PROP.EntryID ) )
                    {
                        REGISTRY.ClearNeeded( res );
                    }
                }
                foreach ( IResource res in contacts )
                {
                    if ( !res.HasProp( PROP.EntryID ) )
                    {
                        candidat = res;
                        break;
                    }
                }

                if ( candidat != null )
                {
                    contactBO = Core.ContactManager.GetContact( candidat );
                    UpdateFields( contactBO );
                    contactBO.AddAccount( _emailAddress );
                }
                else
                {
                    IResource blankContact = null;
                    if ( contactsWithEmail != null )
                    {
                        foreach ( IResource res in contactsWithEmail )
                        {

                            if ( ContactManager.IsEmptyContact( res ) )
                            {
                                blankContact = res;
                                break;
                            }
                        }
                    }
                    bool updateContact = false;
                    if ( blankContact != null )
                    {
                        string strEntryID = blankContact.GetStringProp( PROP.EntryID );
                        if ( strEntryID == null || strEntryID == entryID )
                        {
                            updateContact = true;
                        }
                    }
                    if ( updateContact )
                    {
                        contactBO = Core.ContactManager.GetContact( blankContact );
                        UpdateFields( contactBO );
                        contactBO.AddAccount( _emailAddress );
                    }
                    else
                    {
                        contactBO = CreateContact( );
                        contactBO.AddAccount( _emailAddress );
                    }
                }
            }
            return contactBO;
        }
    }

}
