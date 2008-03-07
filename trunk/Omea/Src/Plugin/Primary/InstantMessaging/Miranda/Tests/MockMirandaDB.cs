/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using JetBrains.Omea.InstantMessaging.Miranda;

namespace JetBrains.Omea.InstantMessaging.Miranda.Tests
{
	internal class MockMirandaDB: IMirandaDB
	{
	    private MockMirandaContact _userContact = new MockMirandaContact( 0 );
        private ArrayList _contacts = new ArrayList();
        private int _lastContactOffset = 1;
        
        public IMirandaContact UserContact
	    {
	        get { return _userContact; }
	    }

	    public IEnumerable Contacts
	    {
	        get { return _contacts; }
	    }

	    public int ContactCount
	    {
	        get { return _contacts.Count; }
	    }

	    public int FileSize
	    {
	        get { return 0; }
	    }

	    public int SlackSpace
	    {
	        get { return 0; }
	    }

	    public void Close()
	    {
	    }

        internal MockMirandaContact AddContact()
        {
            MockMirandaContact result = new MockMirandaContact( _lastContactOffset++ );
            _contacts.Add( result );
            return result;
        }
	}

    internal class MockMirandaContact: IMirandaContact
    {
        private ArrayList _settings = new ArrayList();
        private ArrayList _events = new ArrayList();
        private int _offset;

        public MockMirandaContact( int offset )
        {
            _offset = offset;
        }

        public int Offset
        {
            get { return _offset; }
        }

        public int LastEventOffset
        {
            get { return 0; }
        }

        public IEnumerable ContactSettings
        {
            get { return _settings; }
        }

        public IEnumerable Events 
        {
            get { return _events; }
        }

        public bool DatabaseClosed
        {
            get { return false; }
        }

        internal void AddSetting( string moduleName, string setting, object val )
        {
            foreach( MockMirandaContactSettings settings in _settings )
            {
                if ( settings.ModuleName == moduleName )
                {
                    settings.AddSetting( setting, val );
                    return;
                }
            }
            MockMirandaContactSettings newSettings = new MockMirandaContactSettings( moduleName );
            _settings.Add( newSettings );
            newSettings.AddSetting( setting, val );
        }

        internal void AddEvent( string moduleName, int eventType, DateTime timestamp, int flags, string eventData )
        {
            _events.Add( new MockMirandaEvent( moduleName, eventType, timestamp, flags, eventData ) );
        }
    }

    internal class MockMirandaContactSettings: IMirandaContactSettings
    {
        private string _moduleName;
        private Hashtable _settings = new Hashtable();

        public MockMirandaContactSettings( string moduleName )
        {
            _moduleName = moduleName;
        }

        public string ModuleName
        {
            get { return _moduleName; }
        }

        public IDictionary Settings
        {
            get { return _settings; }
        }

        internal void AddSetting( string setting, object val )
        {
            _settings [setting] = val;
        }
    }

    internal class MockMirandaEvent: IMirandaEvent
    {
        private string _moduleName;
        private int _eventType;
        private DateTime _timestamp;
        private int _flags;
        private string _eventData;

        public MockMirandaEvent( string moduleName, int eventType, DateTime timestamp, int flags, string eventData )
        {
            _moduleName = moduleName;
            _eventType = eventType;
            _timestamp = timestamp;
            _flags = flags;
            _eventData = eventData;
        }

        public string ModuleName
        {
            get { return _moduleName; }
        }

        public int EventType
        {
            get { return _eventType; }
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
        }

        public int Flags
        {
            get { return _flags; }
        }

        public string EventData
        {
            get { return _eventData; }
        }
    }
}
