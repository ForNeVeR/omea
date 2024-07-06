// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Omea.InstantMessaging.ICQ
{
	internal class ICQContact : IComparable
	{
		public enum Genders
		{
			Undefined,
			Female,
			Male
		}

		public ICQContact()
		{
			_sFirstName = string.Empty;
			_sLastName = string.Empty;
			_sNickName = string.Empty;
            _sMyDefinedHandle = string.Empty;
            _sEmail = string.Empty;
			_sPassword = string.Empty;
			_sAddress = string.Empty;
			_sCompany = string.Empty;
			_iGender = Genders.Undefined;
			_BirthDate = DateTime.MinValue;
            _messages = 0;
            _ignored = false;
		}

		// ICQ contacts are compared by UIN
		public override bool Equals(object aContact)
		{
			ICQContact right = aContact as ICQContact;
			return ( right != null ) && UIN == right.UIN;
		}
		public override int GetHashCode()
		{
			return UIN;
		}
		public int CompareTo(object aContact)
		{
			ICQContact right = aContact as ICQContact;
			if( right == null )
				throw new ArgumentException("object is not an ICQContact");
			return UIN - right.UIN;
		}

		#region properties
		public int UIN
		{
			get { return _UIN; }
			set { _UIN = value; }
		}
		public string FirstName
		{
			get { return _sFirstName; }
			set { if( value.Length > 0 ) _sFirstName = value; }
		}
		public string LastName
		{
			get { return _sLastName; }
			set { if( value.Length > 0 ) _sLastName = value; }
		}
		public string NickName
		{
			get
			{
                // override nickname with the "MyDefinedHandle" if any
                if( _sMyDefinedHandle.Length > 0 )
                {
                    return _sMyDefinedHandle;
                }
                return _sNickName;
			}
			set { if( value.Length > 0 ) _sNickName = value; }
		}
        public string MyDefinedHandle
        {
            get { return _sMyDefinedHandle; }
            set { if( value.Length > 0 ) _sMyDefinedHandle = value; }
        }
        public string eMail
        {
            get { return _sEmail; }
            set { if( value.Length > 0 ) _sEmail = value; }
        }
		public string Password
		{
			get { return _sPassword; }
			set { if( value.Length > 0 ) _sPassword = value; }
		}
		public string Address
		{
			get { return _sAddress; }
			set { if( value.Length > 0 ) _sAddress = value; }
		}
		public string Company
		{
			get { return _sCompany; }
			set { if( value.Length > 0 ) _sCompany = value; }
		}
        public string Homepage
        {
            get { return _sHomepage; }
            set { if( value.Length > 0 ) _sHomepage = value; }
        }
		public Genders Gender
		{
			get { return _iGender; }
			set { _iGender = value; }
		}
		public DateTime BirthDate
		{
			get { return _BirthDate; }
			set { _BirthDate = value; }
		}
		public int Age
		{
			get { return _iAge; }
			set { _iAge = value; }
		}
        public int Messages
        {
            get { return _messages; }
            set { _messages = value; }
        }
        public bool Ignored
        {
            get { return _ignored; }
            set { _ignored = value; }
        }
		#endregion

        public bool IsIgnored()
        {
            return _ignored || ( _messages == 0 && _sEmail.Length == 0 );
        }

		#region Contact's info
		private int         _UIN;
		private string      _sFirstName;
		private string      _sLastName;
		private string      _sNickName;
        private string      _sMyDefinedHandle;
        private string      _sEmail;
		private string      _sPassword;
		private string      _sAddress;
		private string      _sCompany;
        private string      _sHomepage;
		private Genders	    _iGender;
		private DateTime    _BirthDate;
		private int         _iAge;
        private int         _messages;
        private bool        _ignored;
		#endregion
	}
}
