/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace OmniaMea.InstantMessaging
{
	/// <summary>
	/// Contact class represents common functionality of IM contacts.
	/// Three types of contacts are supported: ICQ, MSN & AOL.
	/// </summary>
	public abstract class Contact : IComparable
	{
		public enum Types
		{
			ICQ,			// ICQ, Miranda & other clones
			MSN,			// Windows Messenger
			AOL				// AOL IM
		}
		public enum Statuses
		{
			Offline,
			Online,
			Away,
			NA,			// not available
			DND,			// do not disturb
			Privacy
		}

		// all derived constructors should call this one
		protected Contact( Types iType )
		{
			m_iType = iType;
			m_sName = System.String.Empty;
			m_sEmail = System.String.Empty;
			m_iStatus = Statuses.Offline;
		}

		public abstract override bool Equals( object aContact );
		public abstract override int GetHashCode();
		public abstract int CompareTo( object aContact );

		public abstract bool Login( params string[] LoginData );

		#region operators
		public static bool operator== ( Contact c1, Contact c2 )
		{
			return ( (object)c1 == null && (object)c2 == null ) ||
				   ( (object)c1 != null && (object)c2 != null && c1.Equals(c2) );
		}
		public static bool operator!= ( Contact c1, Contact c2 )
		{
			return !( c1 == c2 );
		}
		#endregion

		#region properties
		public Types Type
		{
			get { return m_iType; }
		}
		public Statuses Status
		{
			get { return m_iStatus; }
			set { m_iStatus = value; }
		}
		public string Name
		{
			get { return m_sName; }
			set { m_sName = value; }
		}
		public string ScreenName
		{
			get { return m_sName; }
			set { m_sName = value; }
		}
		public string DisplayName
		{
			get { return m_sName; }
			set { m_sName = value; }
		}
		public string eMail
		{
			get { return m_sEmail; }
			set { m_sEmail = value; }
		}
		#endregion

		private Types						m_iType;

		#region Contact's info
		private string                      m_sName;    // Name | ScreenName | DisplayName
		private string                      m_sEmail;
		private Statuses					m_iStatus;
		#endregion

	}
}
