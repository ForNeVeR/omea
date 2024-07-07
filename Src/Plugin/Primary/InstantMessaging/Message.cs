// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace OmniaMea.InstantMessaging
{
	/// <summary>
	/// Message is base class for all types of IM messages
	/// </summary>
	public abstract class Message
	{
		public enum Types
		{
			SimpleMsg,			// just a message
			URL,
			ShortMessage,
			Contact
		}

		#region properties
		public Types Type
		{
			get { return m_iType; }
			set { m_iType = value; }
		}
		public string Body
		{
			get { return m_sBody; }
			set { m_sBody = value; }
		}
		public System.DateTime DateTime
		{
			get { return m_DateTime; }
			set { m_DateTime = value; }
		}
		public Contact From
		{
			get { return m_From; }
			set { m_From = value; }
		}
		public Contact To
		{
			get { return m_To; }
			set
			{
				// a message should have same type contacts
				if( (object)m_To == null || (object)value == null || m_To.Type == value.Type )
					m_To = value;
			}
		}
		#endregion

		private Types					m_iType;
		private string					m_sBody;
		private System.DateTime			m_DateTime;
		private Contact					m_From;
		private Contact					m_To;
	}
}
