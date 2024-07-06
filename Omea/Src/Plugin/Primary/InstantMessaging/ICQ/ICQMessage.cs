// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Omea.InstantMessaging.ICQ
{
	internal class ICQMessage
	{
        public enum Types
        {
            SimpleMsg,			// just a message
            URL,
            ShortMessage,
            Contact
        }

		public ICQMessage() {}

        #region System.Object overrides

		public override bool Equals(object aMessage)
		{
			ICQMessage right = aMessage as ICQMessage;
			if( right == null )
				return false;
			ICQContact From = this.From;
			ICQContact To = this.To;
			ICQContact rFrom = right.From;
			ICQContact rTo = right.To;
			if( From == null || To == null || rFrom == null || rTo == null )
				return false;
			return From.Equals( rFrom ) && To.Equals( rTo ) &&
			    Time == right.Time && Body == right.Body;
		}

		public override int GetHashCode()
		{
			return (( From == null ) ? 0 : From.GetHashCode() ) + (( To == null ) ? 0 : To.GetHashCode() ) +
			    Time.GetHashCode() + Body.GetHashCode();
		}

        #endregion

        #region properties

        public Types Type
        {
            get { return _type; }
            set { _type = value; }
        }
        public string Body
        {
            get { return _body; }
            set { _body = value; }
        }
        public DateTime Time
        {
            get { return _time; }
            set { _time = value; }
        }
        public ICQContact From
        {
            get { return _from; }
            set { _from = value; }
        }
        public ICQContact To
        {
            get { return _to; }
            set { _to = value; }
        }

        #endregion

        private Types           _type;
        private string          _body;
        private DateTime        _time;
        private ICQContact      _from;
        private ICQContact      _to;
	}
}
