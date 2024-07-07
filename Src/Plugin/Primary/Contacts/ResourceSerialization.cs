// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only


using System;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Contacts;

namespace JetBrains.Omea.ContactsPlugin
{
    public class ContactSerializer : IResourceSerializer
    {
        #region IResourceSerializer Members

        public void AfterSerialize( IResource parentResource, IResource res, System.Xml.XmlNode node )
        {}

        private void UpdateProperty( int propId, IResource source, IResource target )
        {
            if( source.HasProp( propId ) )
            {
                if( Core.ResourceStore.PropTypes[ propId ].DataType != PropDataType.StringList )
                    target.SetProp( propId, source.GetProp( propId ) );
                else
                {
                    IStringList strList = target.GetStringListProp( propId );
                    foreach( string str in source.GetStringListProp( propId ) )
                    {
                        if( strList.IndexOf( str ) == -1 )
                            strList.Add( str );
                    }
                }
            }
        }

        public IResource AfterDeserialize( IResource parentResource, IResource res, System.Xml.XmlNode node )
        {
            if( res.Type != "Contact" )
                throw new ArgumentException( "ContactSerialization -- Invalid type of Contact argument - [" + res.Type + "]" );

            string  title, firstName, middleName, lastName, suffix;
            title = res.GetStringProp( ContactManager._propTitle );
            firstName = res.GetStringProp( ContactManager._propFirstName );
            middleName = res.GetStringProp( ContactManager._propMiddleName );
            lastName = res.GetStringProp( ContactManager._propLastName );
            suffix = res.GetStringProp( ContactManager._propSuffix );
            IContact contact = Core.ContactManager.FindContact( title, firstName, middleName, lastName, suffix );

            if ( contact == null )
            {
                res.SetProp( ContactManager._propTransferred, true );
                return res;
            }

            UpdateProperty( ContactManager._propBirthday, res, contact.Resource );
            UpdateProperty( ContactManager._propCompany, res, contact.Resource );
            UpdateProperty( ContactManager._propHomePage, res, contact.Resource );
            UpdateProperty( ContactManager._propJobTitle, res, contact.Resource );
            UpdateProperty( ContactManager._propAddress, res, contact.Resource );
            return contact.Resource;
        }

        public SerializationMode GetSerializationMode( IResource res, string propType )
        {
            switch ( propType )
            {
                case "Phone":
                    return SerializationMode.AskSerialize;
                case "EntryID":
                    return SerializationMode.NoSerialize;
                case "Imported":
                    return SerializationMode.NoSerialize;
                case "MySelf":
                    return SerializationMode.NoSerialize;
                case "LastCorrespondDate":
                    return SerializationMode.NoSerialize;
            }
            return SerializationMode.Default;
        }
        #endregion
    }

    public class EmailAccountSerializer : IResourceSerializer
    {
        #region IResourceSerializer Members

        public void AfterSerialize( IResource parentResource, IResource res, System.Xml.XmlNode node )
        {}

        public IResource AfterDeserialize( IResource parentResource, IResource res, System.Xml.XmlNode node )
        {
            //  Search for account only if we deal with completely consistent
            //  chain of the resources - parent must be a Contact, resource
            //  must be an EmailAccount
            if( res.Type != "EmailAccount" )
                throw new ArgumentException( "EmailAccountSerialization -- Invalid type of EmailAccount argument - [" + res.Type + "]" );

            IResource account = Core.ResourceStore.FindUniqueResource( "EmailAccount", "EmailAddress", res.GetStringProp( "EmailAddress" ));
            //  Do not create new account if there already exists one.
            return ( account != null ) ? account : res;
        }

        public SerializationMode GetSerializationMode( IResource res, string propertyType )
        {
            return SerializationMode.Default;
        }
        #endregion
    }

    public class PhoneSerializer : IResourceSerializer
    {
        public void AfterSerialize( IResource parentResource, IResource res, System.Xml.XmlNode node )
        {}

        public IResource AfterDeserialize( IResource parentResource, IResource phone, System.Xml.XmlNode node )
        {
            //  Search for account only if we deal with completely consistent chain
            //  of the resources - parent must be a Contact, resource must be a Phone
            if( parentResource.Type != "Contact" && phone.Type != "Phone" )
                throw new ArgumentException( "PhoneDeserialization -- Illegal types of input parameters: [" + parentResource.Type + "] and [" + phone.Type + "]" );

            ContactBO contact = new ContactBO( parentResource );
            string    newPhoneNumber = phone.GetStringProp( ContactManager._propPhoneNumber );
            string    newPhoneName = phone.GetStringProp( ContactManager._propPhoneName );

            //  Do not add phone if it is number already exist (even under the
            //  name possibly).
            IResource contactPhone = contact.GetPhoneByNumber( newPhoneNumber );
            if ( contactPhone != null )
                return contactPhone;

            //  If the phone with such name already exist, find more suitable
            //  name for a newly coming phone number - add numeric prefix for
            //  its name.
            contactPhone = contact.GetPhoneByName( newPhoneName );
            if( contactPhone != null )
            {
                string newName = ContactManager.ComposeSuitablePhoneName( contact, newPhoneName );
                phone.SetProp( ContactManager._propPhoneName, newName );
            }

            return phone;
        }

        public SerializationMode GetSerializationMode( IResource res, string propertyType )
        {
            if ( propertyType == "Name" ) return SerializationMode.Serialize;
            return SerializationMode.Default;
        }
    }
}
