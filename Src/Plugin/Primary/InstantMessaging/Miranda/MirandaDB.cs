// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.InstantMessaging.Miranda
{
    internal interface IMirandaDB
    {
        IMirandaContact UserContact { get; }
        IEnumerable Contacts { get; }
        int ContactCount { get; }
        int FileSize { get; }
        int SlackSpace { get; }
        void Close();
    }

    internal interface IMirandaObject
    {
        int NextOffset { get; }
    }

    internal interface IMirandaContact
    {
        int Offset { get; }
        int LastEventOffset { get; }
        IEnumerable ContactSettings { get; }
        IEnumerable Events { get; }
        bool DatabaseClosed { get; }
    }

    internal interface IMirandaContactSettings
    {
        string ModuleName { get; }
        IDictionary Settings { get; }
    }

    internal interface IMirandaEvent
    {
        string ModuleName { get; }
        int EventType { get; }
        DateTime Timestamp { get; }
        int Flags { get; }
        string EventData { get; }
    }

    internal class MirandaDatabaseCorruptedException: Exception
    {
        public MirandaDatabaseCorruptedException( string message )
            : base( message )
        {
        }
    }

    /**
     * A Miranda database.
     */

	internal class MirandaDB: IMirandaDB
	{
        private int _contactCount;
        private int _ofsFileEnd;
        private int _slackSpace;
        private int _ofsFirstContact;
        private int _ofsUser;
        private int _ofsFirstModuleName;

        private IntHashTable _moduleHash;

        private FileStream   _dbStream;
        private BinaryReader _dbReader;

        private const int SIGNATURE_MODULE = 0x4DDECADE;

		internal MirandaDB( string fileName )
		{
            _dbStream = new FileStream( fileName, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite );
            _dbReader = new BinaryReader( _dbStream, Encoding.Default );

            char[] signature = _dbReader.ReadChars( 16 );
            if ( signature.Length < 16 )
                throw new MirandaDatabaseCorruptedException( "DB signature is too short" );
            string signatureStr = new string( signature, 0, 14 );
            if ( signatureStr != "Miranda ICQ DB" )
                throw new MirandaDatabaseCorruptedException( "Incorrect DB signature" );

            int version = _dbReader.ReadInt32();
            if ( version != 0x0700 )
                throw new MirandaDatabaseCorruptedException( "Unsupported DB version" );

            _ofsFileEnd         = _dbReader.ReadInt32();
            _slackSpace         = _dbReader.ReadInt32();
            _contactCount       = _dbReader.ReadInt32();
            _ofsFirstContact    = _dbReader.ReadInt32();
            _ofsUser            = _dbReader.ReadInt32();
            _ofsFirstModuleName = _dbReader.ReadInt32();
		}

        private void LoadModules()
        {
            _moduleHash = new IntHashTable();
            _dbStream.Position = _ofsFirstModuleName;
            while ( true )
            {
                int ofs = (int) _dbStream.Position;
                int signature = _dbReader.ReadInt32();
                if ( signature != SIGNATURE_MODULE )
                    throw new MirandaDatabaseCorruptedException( "Database corrupted: invalid module signature" );
                int ofsNext = _dbReader.ReadInt32();
                int cbName = _dbReader.ReadByte();
                if( cbName > 0 )
                {
                    char[] nameChars = _dbReader.ReadChars( cbName );
                    _moduleHash [ofs] = new string( nameChars );
                }
                if ( ofsNext == 0 )
                    break;
                _dbStream.Position = ofsNext;
            }
        }

        public void Close()
        {
            _dbReader.Close();
        }

        public IMirandaContact UserContact
        {
            get { return new MirandaContact( this, _dbReader, _ofsUser ); }
        }

        public IEnumerable Contacts
        {
            get { return new MirandaContactEnumerator( this, _dbReader, _ofsFirstContact ); }
        }

        public int ContactCount
        {
            get { return _contactCount; }
        }

	    public int FileSize
	    {
	        get { return _ofsFileEnd; }
	    }

	    public int SlackSpace
	    {
	        get { return _slackSpace; }
	    }

	    internal string GetModuleName( int offset )
        {
            if ( _moduleHash == null )
            {
                LoadModules();
            }

            string moduleName = (string) _moduleHash [offset];
            if ( moduleName == null )
            {
                throw new MirandaDatabaseCorruptedException( "Unknown module at offset" + offset );
            }
            return moduleName;
        }
	}

    /**
     * A generic enumerator for objects in the Miranda database.
     */

    internal abstract class MirandaEnumerator: IEnumerable, IEnumerator
    {
        protected MirandaDB _db;
        protected BinaryReader _reader;
        private int _ofsFirst;
        protected int _ofsCurrent;
        private IMirandaObject _currentObject;

        internal MirandaEnumerator( MirandaDB db, BinaryReader reader, int ofsFirst )
        {
            _db = db;
            _reader = reader;
            _ofsFirst = ofsFirst;
            _ofsCurrent = 0;
            _currentObject = null;
        }

        protected abstract IMirandaObject LoadCurrentObject();

        public IEnumerator GetEnumerator()
        {
            return this;
        }

        public void Reset()
        {
            _ofsCurrent = 0;
        }

        public object Current
        {
            get { return _currentObject; }
        }

        public bool MoveNext()
        {
            if ( _ofsCurrent == 0 )
            {
                if ( _ofsFirst == 0 )
                    return false;
                _ofsCurrent = _ofsFirst;
            }
            else
            {
                if ( _currentObject.NextOffset == 0 )
                    return false;
                _ofsCurrent = _currentObject.NextOffset;
            }
            _currentObject = LoadCurrentObject();
            return true;
        }
    }

    /**
     * An enumerator for the contacts in the Miranda DB.
     */

    internal class MirandaContactEnumerator: MirandaEnumerator
    {
        internal MirandaContactEnumerator( MirandaDB db, BinaryReader reader, int ofsFirst )
            : base( db, reader, ofsFirst ) {}

        protected override IMirandaObject LoadCurrentObject()
        {
            return new MirandaContact( _db, _reader, _ofsCurrent );
        }
    }

    /**
     * A contact in the Miranda database.
     */

    internal class MirandaContact: IMirandaObject, IMirandaContact
    {
        private MirandaDB _db;
        private BinaryReader _reader;
        private int _ofs;
        private int _ofsNext;
        private int _ofsFirstSettings;
        private int _eventCount;
        private int _ofsFirstEvent;
        private int _ofsLastEvent;
        private int _ofsFirstUnreadEvent;
        private int _timestampFirstUnread;

        private const int SIGNATURE_CONTACT = 0x43DECADE;

        internal MirandaContact( MirandaDB db, BinaryReader reader, int ofs )
        {
            Debug.Assert( ofs != 0 );
            _db = db;
            _reader = reader;
            _ofs = ofs;

            reader.BaseStream.Position = ofs;
            int signature = _reader.ReadInt32();
            if ( signature != SIGNATURE_CONTACT )
                throw new MirandaDatabaseCorruptedException( "Database corrupted: invalid contact signature" );

            _ofsNext = reader.ReadInt32();
            _ofsFirstSettings = reader.ReadInt32();
            _eventCount = reader.ReadInt32();
            _ofsFirstEvent = reader.ReadInt32();
            _ofsLastEvent = reader.ReadInt32();
            _ofsFirstUnreadEvent = reader.ReadInt32();
            _timestampFirstUnread = reader.ReadInt32();
        }

        public bool DatabaseClosed
        {
            get { return _reader.BaseStream == null; }
        }

        public IEnumerable ContactSettings
        {
            get
            {
                return new ContactSettingsEnumerator( _db, _reader, _ofsFirstSettings );
            }
        }

        public IEnumerable Events
        {
            get
            {
                return new MirandaEventEnumerator( _db, _reader, _ofsFirstEvent );
            }
        }

        public int Offset
        {
            get { return _ofs; }
        }

        public int NextOffset
        {
            get { return _ofsNext; }
        }

        public int LastEventOffset
        {
            get { return _ofsLastEvent; }
        }
    }

    /**
     * An enumerator for the contact settings in the Miranda DB.
     */

    internal class ContactSettingsEnumerator: MirandaEnumerator
    {
        internal ContactSettingsEnumerator( MirandaDB db, BinaryReader reader, int ofsFirst )
            : base( db, reader, ofsFirst ) {}

        protected override IMirandaObject LoadCurrentObject()
        {
            return new MirandaContactSettings( _db, _reader, _ofsCurrent );
        }
    }

    /**
     * A set of settings from one module for a contact in the Miranda DB.
     */

    internal class MirandaContactSettings: IMirandaObject, IMirandaContactSettings
    {
        private BinaryReader _reader;
        private int _ofsNext;
        private string _moduleName;
        private int _cbBlob;
        private long _ofsData;
        private Hashtable _settings;

        private const int SIGNATURE_CONTACT_SETTINGS = 0x53DECADE;

        internal MirandaContactSettings( MirandaDB db, BinaryReader reader, int ofs )
        {
            Guard.NullArgument( db, "db" );
            Guard.NullArgument( reader, "reader" );
            if ( reader.BaseStream == null )
            {
                throw new InvalidOperationException( "Trying to load contact settings after Miranda DB has been closed" );
            }
            _reader = reader;

            reader.BaseStream.Position = ofs;
            int signature = _reader.ReadInt32();
            if ( signature != SIGNATURE_CONTACT_SETTINGS )
                throw new MirandaDatabaseCorruptedException( "Database corrupted: invalid contact settings signature" );

            _ofsNext = reader.ReadInt32();

            int ofsModuleName = reader.ReadInt32();
            _moduleName = db.GetModuleName( ofsModuleName );

            _cbBlob = reader.ReadInt32();
            _ofsData = reader.BaseStream.Position;
        }

        public int NextOffset
        {
            get { return _ofsNext; }
        }

        public string ModuleName
        {
            get { return _moduleName; }
        }

        public IDictionary Settings
        {
            get
            {
                if ( _settings == null )
                    LoadSettings();
                return _settings;
            }
        }

        private void LoadSettings()
        {
            _settings = new Hashtable( new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer() );
            _reader.BaseStream.Position = _ofsData;
            while( _reader.PeekChar() != 0 )
            {
                MirandaSetting setting = new MirandaSetting( _reader );
                if ( setting.Value != null && !_settings.ContainsKey( setting.Name ) )  // ignore deleted and duplicate settings
                {
                    _settings.Add( setting.Name, setting.Value );
                }
            }
        }
    }

    /// <summary>
    /// A single contact setting in the Miranda DB.
    /// </summary>
    internal class MirandaSetting
    {
        private string _name;
        private object _value;

        private const int DBVT_DELETED = 0;
        private const int DBVT_BYTE    = 1;
        private const int DBVT_WORD    = 2;
        private const int DBVT_DWORD   = 4;
        private const int DBVT_ASCIIZ  = 255;
        private const int DBVT_BLOB    = 254;

        internal MirandaSetting( BinaryReader reader )
        {
            int cbName = reader.ReadByte();
            _name = new string( reader.ReadChars( cbName ) );

            int dataValueType = reader.ReadByte();
            switch( dataValueType )
            {
                case DBVT_DELETED:
                    _value = null;
                    break;

                case DBVT_BYTE:
                    _value = reader.ReadByte();
                    break;

                case DBVT_WORD:
                    _value = reader.ReadUInt16();
                    break;

                case DBVT_DWORD:
                    _value = reader.ReadInt32();
                    break;

                case DBVT_BLOB:
                {
                    int cbBlob = reader.ReadUInt16();
                    byte[] blobBytes = new byte [cbBlob];
                    reader.Read( blobBytes, 0, cbBlob );
                    _value = blobBytes;
                    break;
                }

                case DBVT_ASCIIZ:
                {
                    int cbString = reader.ReadUInt16();
                    _value = new string( reader.ReadChars( cbString ) );
                    break;
                }

                default:
                    throw new MirandaDatabaseCorruptedException( "Unknown or unsupported value type " + dataValueType );

            }
        }

        public string Name
        {
            get { return _name; }
        }

        public object Value
        {
            get { return _value; }
        }
    }

    /**
     * An enumerator for the events in the Miranda DB.
     */

    internal class MirandaEventEnumerator: MirandaEnumerator
    {
        internal MirandaEventEnumerator( MirandaDB db, BinaryReader reader, int ofsFirst )
            : base( db, reader, ofsFirst ) {}

        protected override IMirandaObject LoadCurrentObject()
        {
            return new MirandaEvent( _db, _reader, _ofsCurrent );
        }
    }

    /**
     * An event in the Miranda DB.
     */

    internal class MirandaEvent: IMirandaObject, IMirandaEvent
    {
        private const int SIGNATURE_EVENT = 0x45DECADE;

        private int _ofsNext;
        private string _moduleName;
        private DateTime _timestamp;
        private int _flags;
        private int _eventType;
        private string _eventData;

        internal MirandaEvent( MirandaDB db, BinaryReader reader, int ofs )
        {
            reader.BaseStream.Position = ofs;
            int signature = reader.ReadInt32();
            if ( signature != SIGNATURE_EVENT )
                throw new MirandaDatabaseCorruptedException( "Database corrupted: invalid event signature" );

            reader.ReadInt32();  // skip _ofsPrev
            _ofsNext = reader.ReadInt32();

            int ofsModuleName = reader.ReadInt32();
            _moduleName = db.GetModuleName( ofsModuleName );

            int timestamp = reader.ReadInt32();
            _timestamp = new DateTime( 1970, 1, 1 ).AddDays( (double ) timestamp / ( 24 * 60 * 60 ) );

            _flags = reader.ReadInt16();
            _eventType = reader.ReadInt32();
            int cbBlob = reader.ReadInt32();
            if ( cbBlob == 0 )
                _eventData = "";
            else
            {
                byte[] eventData = reader.ReadBytes( cbBlob );
                int nullIndex = Array.IndexOf( eventData, (byte) 0 );
                if ( _eventType == 0 && nullIndex < cbBlob-3 )
                {
                    _eventData = Encoding.Unicode.GetString( eventData, nullIndex+1, cbBlob-nullIndex-3 );
                }
                else
                {
                    _eventData = Encoding.Default.GetString( eventData, 0, cbBlob-1 );
                }
            }
        }

        public int NextOffset
        {
            get { return _ofsNext; }
        }

        public string ModuleName
        {
            get { return _moduleName; }
        }

        public int EventType
        {
            get { return _eventType; }
        }

        public string EventData
        {
            get { return _eventData; }
        }

        public int Flags
        {
            get { return _flags; }
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
        }
    }
}
