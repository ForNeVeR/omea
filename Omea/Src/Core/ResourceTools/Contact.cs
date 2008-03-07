/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Text;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.Contacts
{
    public class ContactBO: IContact
    {
        protected IResource     _resource;
        private   bool          _changed = false;
        private   string        _oldBody = null;
        private   int           _updateCount;

        public ContactBO( IResource contact )
        {
            #region Preconditions
            if ( contact == null )
                throw new ArgumentNullException( "contact", "Contact -- Input resource is NULL.");
            if( contact.Type != "Contact" )
                throw new ApplicationException( "Contact -- wrong initialization with a resource of inappropriate type [" + contact.Type + "]" );
            #endregion Preconditions

            _resource = contact;
        }
        public int ID
        {
            get { return _resource.Id; }
        }
        public IResource Resource { get { return _resource; } }

        //---------------------------------------------------------------------
        public bool IsImported
        { 
            get { return _resource.HasProp( ContactManager._propImported ); }
        }
        public bool IsMyself
        {
            get{ return (_resource.GetIntProp( Core.ContactManager.Props.Myself ) == 1); }
        }
        public void SetMyself()
        {
            SetProp( Core.ContactManager.Props.Myself, 1 );
        }
        public bool IsSerializationContainer
        {
            get {  return _resource.GetLinksFrom( null, ContactManager._propSerializationBlobLink ).Count > 0; }
        }

        //---------------------------------------------------------------------
        public string FullName
        {
            get
            {
                string text = Title + " " + FirstName + " " + MiddleName + " " + LastName + " " + Suffix;
                return ContactResolver.CompressBlanks( text );
            }
        }
        public string Title
        {
            get { return GetStringProp( ContactManager._propTitle ); }
            set { SetProp( ContactManager._propTitle, value ); }
        }
        public string FirstName 
        {
            get { return GetStringProp( ContactManager._propFirstName ); }
            set { SetProp( ContactManager._propFirstName, value ); }
        }
        public string MiddleName 
        {
            get { return GetStringProp( ContactManager._propMiddleName ); }
            set { SetProp( ContactManager._propMiddleName, value ); }
        }
        public string LastName
        {
            get { return GetStringProp( ContactManager._propLastName ); }
            set { SetProp( ContactManager._propLastName, value ); }
        }
        public string Suffix
        {
            get { return GetStringProp( ContactManager._propSuffix ); }
            set { SetProp( ContactManager._propSuffix, value ); }
        }
        public string HomePage
        {
            get { return GetStringProp( ContactManager._propHomePage ); }
            set { SetProp( ContactManager._propHomePage, value ); }
        }
        public string Address
        {
            get { return GetStringProp( ContactManager._propAddress ); }
            set { SetProp( ContactManager._propAddress, value ); }
        }
        public string Company 
        {
            get { return GetStringProp( ContactManager._propCompany ); }
            set { SetProp( ContactManager._propCompany, value ); }
        }
        public string JobTitle 
        {
            get { return GetStringProp( ContactManager._propJobTitle ); }
            set { SetProp( ContactManager._propJobTitle, value ); }
        }
        public DateTime Birthday
        {
            get { return _resource.GetDateProp( ContactManager._propBirthday ); }
            set { _resource.SetProp( ContactManager._propBirthday, value ); }
        }
        public DateTime LastCorrespondDate
        {
            get { return _resource.GetDateProp( Core.ContactManager.Props.LastCorrespondenceDate ); }
            set
            {
                DateTime curr = DateTime.MinValue;
                if( _resource.HasProp( Core.ContactManager.Props.LastCorrespondenceDate ))
                    _resource.GetDateProp( Core.ContactManager.Props.LastCorrespondenceDate );
                
                //  Do not allow to set earlier date than the current one.
                if( value < curr )
                    throw new ArgumentException( "IContact -- Can not set the date earlier than the current one" );

                SetProp( Core.ContactManager.Props.LastCorrespondenceDate, value );
            }
        }
        public string Description
        {
            get {  return GetStringProp( ContactManager._propDescription );  }
            set {  SetProp( ContactManager._propDescription, value );        }
        }

        public string ContactBody
        {
            get 
            {
                StringBuilder text = StringBuilderPool.Alloc();
                try
                {
                    //-------------------------------------------------------------
                    text.AppendFormat( "{0} {1} {2} {3} {4} {5} {6} {7} {8} ", Title, FirstName, MiddleName, LastName, Suffix, 
                        Address, Company, JobTitle, HomePage );
                    foreach( string phoneName in GetPhoneNames() )
                    {
                        text.AppendFormat( "{0} ", GetPhoneNumber( phoneName ) );
                    }
                    text.Append( Description );

                    //-------------------------------------------------------------
                    IResourceList names = _resource.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
                    if( names.Count > 1 )
                    {
                        text.Append( "|" );
                        HashSet uniqueNames = new HashSet();
                        foreach( IResource res in names.ValidResources )
                        {
                            string name = res.GetStringProp( "Name" );
                            if( name != null )
                            {
                                name = name.Trim( '\'' );
                                if ( !uniqueNames.Contains( name ) )
                                {
                                    uniqueNames.Add( name );
                                    text.Append( name );
                                    text.Append( " " );
                                }
                            }
                        }
                        text.Append( "|" );
                    }

                    //-------------------------------------------------------------
                    IResourceList accounts = _resource.GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct );
                    foreach( IResource res in accounts.ValidResources )
                    {
                        text.Append( res.DisplayName );
                        text.Append( "|" );
                    }

                    //-------------------------------------------------------------
                    string resultText = text.ToString();
                    resultText = ContactResolver.CompressBlanks( resultText );
                
                    return resultText;
                }
                finally
                {
                    StringBuilderPool.Dispose( text );
                }
            }
        }

        public string DefaultEmailAddress
        {
            get
            {
                IResource emailAcct = _resource.GetLinkProp( ContactManager._propDefaultAccount );
                if ( emailAcct == null )
                {
                    IResourceList emails = _resource.GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct );
                    if ( emails.Count > 0 )
                        emailAcct = emails [0];
                }
                if ( emailAcct != null )
                    return emailAcct.GetStringProp( Core.ContactManager.Props.EmailAddress );

                return null;
            }
        }

        public void AddAccount( string account )
        {
            if( !String.IsNullOrEmpty( account ) )
            {
                IResource accountRes = Core.ContactManager.FindOrCreateEmailAccount( account );
                AddAccount( accountRes );
            }
        }
        public void AddAccount( IResource account )
        {
            if( account != null )
            {
                if( !ContactManager.IsEmptyContact( _resource ) )
                    (Core.ContactManager as ContactManager).DeleteBlankContacts( account );

                if( !_resource.HasLink( Core.ContactManager.Props.LinkEmailAcct, account ) )
                {
                    new ResourceProxy( _resource ).AddLink( Core.ContactManager.Props.LinkEmailAcct, account );

                    //  Account which is new for a contact is always considered
                    //  "personal".
                    new ResourceProxy( account ).SetProp( Core.ContactManager.Props.PersonalAccount, true );
                }
            }
        }

        public void  UpdateNameFields( string fullName )
        {
            string    title, fName, midName, lName, suffix, addSpec;
            if( ContactResolver.ResolveName( fullName, null, out title, out fName, out midName, out lName, out suffix, out addSpec ) )
                UpdateNameFields( title, fName, midName, lName, suffix );
        }
        public void  UpdateNameFields( string title, string firstName, string midName, string lastName, string suffix )
        {
            Title = title;
            FirstName = firstName;
            MiddleName = midName;
            LastName = lastName;
            Suffix = suffix;
        }

        #region Phones
        static public void PhonesCleanUp()
        {
            IResourceList phones = Core.ResourceStore.FindResources( "Phone", ContactManager._propPhoneNumber, string.Empty );
            foreach ( IResource phone in phones )
                phone.Delete();
        }

        public bool PhoneNumberExists( string phoneNumber )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( phoneNumber ))
                throw new ArgumentNullException( "phoneNumber", "IContact -- Invalid phone number - null or empty");
            #endregion Preconditions

            return ( GetPhoneByNumber( phoneNumber ) != null );
        }
        public bool IsPhoneNameExists( string phoneName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( phoneName ))
                throw new ArgumentNullException( "phoneName", "IContact -- Invalid phone name - null or empty");
            #endregion Preconditions

            return ( GetPhoneByName( phoneName ) != null );
        }

        public void DeletePhone( string phoneName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( phoneName ))
                throw new ArgumentNullException( "phoneName", "IContact -- Invalid phone name - null or empty");
            #endregion Preconditions

            IResource phone = GetPhoneByName( phoneName );
            if ( phone != null )
            {
                phone.Delete();
            }
        }

        internal static string NormalizedPhoneNumber( string phoneNumber )
        {
            for ( int i = phoneNumber.Length - 1; i >= 0; i-- )
            {
                if ( !Char.IsDigit( phoneNumber, i ) )
                {
                    phoneNumber = phoneNumber.Remove( i, 1 );
                }
            }
            return phoneNumber;
        }

        public string GetPhoneNumber( string phoneName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( phoneName ))
                throw new ArgumentNullException( "phoneName", "IContact -- Invalid phone name - null or empty");
            #endregion Preconditions

            IResource phone = GetPhoneByName( phoneName );
            if ( phone != null )
            {
                return phone.GetStringProp( ContactManager._propPhoneNumber );
            }
            return string.Empty;
        }

        public IResource GetPhoneByName( string phoneName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( phoneName ))
                throw new ArgumentNullException( "phoneName", "IContact -- Invalid phone name - null or empty");
            #endregion Preconditions

            IResourceList phones = _resource.GetLinksOfType( "Phone", ContactManager._propPhone );
            foreach( IResource phone in phones )
            {
                if( phone.GetStringProp( ContactManager._propPhoneName ) == phoneName )
                {
                    return phone;
                }
            }
            return null;
        }
        public IResource GetPhoneByNumber( string phoneNumber )
        {
            #region Preconditions
            if ( String.IsNullOrEmpty( phoneNumber ))
                throw new ArgumentNullException( "phoneNumber", "IContact -- Invalid phone number - null or empty");
            #endregion Preconditions

            IResourceList phones = _resource.GetLinksOfType( "Phone", ContactManager._propPhone );
            foreach ( IResource phone in phones )
            {
                string phoneProp = phone.GetStringProp( ContactManager._propPhoneNumber );
                if ( NormalizedPhoneNumber( phoneProp ) == NormalizedPhoneNumber( phoneNumber ) )
                {
                    return phone;
                }
            }
            return null;
        }

        public string[] GetPhoneNames()
        {
            IResourceList phones = _resource.GetLinksOfType( "Phone", ContactManager._propPhone );
            string[] result = new string[ phones.Count ];
            for( int i = 0; i < phones.Count; ++i )
            {
                result[ i ] = phones[ i ].GetStringProp( ContactManager._propPhoneName );
                if( String.IsNullOrEmpty( result[ i ] ) )
                    throw new ArgumentOutOfRangeException( "result[ i ]", "IContact -- Illegal PhoneName while retrieving - null or empty" );
            }
            return result;
        }

        public void SetPhoneNumber( string phoneName, string phoneNumber )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( phoneName ))
                throw new ArgumentNullException( "phoneName", "IContact -- Invalid phone name - null or empty");
            if( String.IsNullOrEmpty( phoneNumber ))
                throw new ArgumentNullException( "phoneNumber", "IContact -- Invalid phone number - null or empty");
            #endregion Preconditions

            IResource phone = GetPhoneByName( phoneName );
            if( phone != null )
            {
                phone.SetProp( ContactManager._propPhoneNumber, phoneNumber );
            }
            else
            {
                phone = Core.ResourceStore.BeginNewResource( "Phone" );
                try
                {
                    phone.SetProp( ContactManager._propPhoneName, phoneName );
                    phone.SetProp( ContactManager._propPhoneNumber, phoneNumber );
                    _resource.AddLink( ContactManager._propPhone, phone );
                }
                finally
                {
                    phone.EndUpdate();
                }
            }
        }
        #endregion Phones

        #region Miscellaneous
        public void SetProp( int prop, object value )
        {
            bool needUpdate = false;
            PropDataType type = Core.ResourceStore.PropTypes[ prop ].DataType;
            if( type == PropDataType.String || type == PropDataType.LongString )
            {
                if( _resource.GetPropText( prop ) != value as string )
                {
                    needUpdate = true;
                }
            }
            else
            {
                object val = _resource.GetProp( prop );
                if( ( val == null && value != null ) || ( val != null && !val.Equals( value ) ) )
                {
                    needUpdate = true;
                }
            }
            _changed = _changed || needUpdate;
            if( needUpdate )
            {
                if( Core.ResourceStore.IsOwnerThread() )
                {
                    _resource.SetProp( prop, value );
                }
                else
                {
                    new ResourceProxy( _resource ).SetPropAsync( prop, value );
                }
            }
        }
        private string GetStringProp( int prop )
        {
            return _resource.GetPropText( prop );
        }

        public bool Changed { get { return _changed; } }
        public void QueueIndexing()
        {
            if( _changed )
                Core.TextIndexManager.QueryIndexing( _resource.Id );
            _changed = false;
        }

        public void BeginUpdate()
        {
            _updateCount++;
            if ( _updateCount == 1 )
            {
                _oldBody = ContactBody;
                _resource.BeginUpdate();
            }
        }

        public void EndUpdate()
        {
            if ( _updateCount <= 0 )
            {
                throw new InvalidOperationException( "EndUpdate() called without BeginUpdate()" );
            }
            _updateCount--;
            if ( _updateCount == 0 )
            {
                _resource.EndUpdate();
                if ( ContactBody != _oldBody )
                {
                    Core.TextIndexManager.QueryIndexing( _resource.Id );
                }
                _oldBody = null;
            }
        }

        #endregion Miscellaneous
    }
}