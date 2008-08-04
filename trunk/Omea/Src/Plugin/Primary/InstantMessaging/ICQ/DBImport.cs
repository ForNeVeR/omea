/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using JetBrains.Omea.Base;
using JetBrains.Omea.Database.DBF;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using Microsoft.Win32;
using JetBrains.Omea.Containers;
using JetBrains.DataStructures;

namespace JetBrains.Omea.InstantMessaging.ICQ.DBImport
{
    public enum DBVersion
    {
        db_Undefined = 0,
        db_99A = 10, 
        db_99B = 14,
        db_2000a = 17,
        db_2000b = 18,
        db_2001a = 19,  // 2001a, 2001b, 2002a, 2003a
        db_2003b = 20   // 2003b and higher
    }

    /**
     * Database enumerates messages & contacts in a directory
     * Directory may contain database files for several UINs
     */
    internal abstract class IICQDatabase : IEnumerable, IEnumerator
    {
        public abstract string CurrentLocation { get; }
        public abstract DBVersion CurrentDBVersion { get; }
        public abstract int CurrentUIN { get; }
        public abstract bool EnumUINsOnly { get; set; }
        public abstract void SkipUpdate();
        public abstract bool MoveNext();
        public abstract void Reset();
        public abstract IEnumerator GetEnumerator();
        public abstract object Current { get; }

        public static ICachingStrategy CachingStrategy
        {
            get
            {
                if( _strategy == null )
                {
                    _strategy = new SharedCachingStrategy( 1 << 19 );
                }
                return _strategy;
            }
        }

        private static ICachingStrategy _strategy;
    }

    /**
     * ICQDatabase class imports history of versions earlier than 2003b
     */
    internal class ICQDatabase : IICQDatabase
    {
        public ICQDatabase( string sDirectory )
        {
            _sDirectory = sDirectory;
            Reset();
        }

        #region IICQDabase
        public override string CurrentLocation
        {
            get { return _sDirectory; }
        }
        public override DBVersion CurrentDBVersion
        {
            get { return (DBVersion)_iDBVersion; }
        }
        public override int CurrentUIN
        {
            get { return _iUIN; }
        }
        public override bool EnumUINsOnly
        {
            get { return _enumUINsOnly; }
            set { _enumUINsOnly = value; }
        }
        public override void SkipUpdate() {}
        #endregion

        #region implementation of IEnumerator & IEnumerable
        public override IEnumerator GetEnumerator()
        {
            return this;
        }
        public override object Current
        {
            get { return _resultObject; }
        }
        public override bool MoveNext()
        {
            while( !_bFinished )
            {
                if( _bNewFile )
                {
                    // enumerate files in order to find ICQ db file
                    if( !_itfFilesEnumerator.MoveNext() )
                    {
                        _bFinished = true;
                    }
                    else
                    {
                        FileInfo fi = (FileInfo) _itfFilesEnumerator.Current;
                        _iUIN = ExtractUINByFileName( fi.Name );
                        if( _iUIN > 0 )
                        {
                            _currentContact = ContactsFactory.GetInstance().GetContact( _iUIN );
                            if( _enumUINsOnly )
                            {
                                return true;
                            }
                            _bNewFile = !OpenDBFiles( _iUIN );
                        }
                    }
                }
                else
                {
                    if( ReadIndexes() )
                    {
                        return true;
                    }
                    _bNewFile = true;
                }
            }
            return false;
        }
        public override void Reset()
        {
            _bFinished = false;
            _bNewFile = true;
            _enumUINsOnly = false;
            try
            {
                _itfFilesEnumerator = new DirectoryInfo( _sDirectory ).GetFiles().GetEnumerator();
                // messages dated earlier than year the 1998 and very likely broken
                // and should be ignored
                _boundDate = new DateTime( 1998, 1, 1 );
                _Idx = new IdxRecord();
                _datEntry = new DatRecord();
            }
            catch( Exception )
            {
                _bFinished = true;
            }
        }
        private bool ReadIndexes()
        {
            // read dat entries for corresponding indexes
            // index value -1 is meaning null index, so current index
            // was the last one, and we are to try next file
            while( _iCurrentIdx != -1 )
            {
                if( !_Idx.ReadIdxRecord( _iCurrentIdx, _fileIdx ) )
                {
                    break;
                }
                _iCurrentIdx = _Idx.Next;
                if( _Idx.Code == -2 && _Idx.DatPos != -1 && 
                    _datEntry.ReadDatRecord( _Idx.DatPos, _fileDat, CurrentDBVersion ) )
                {
                    // parse dat entry and save it if ok
                    object Result = _datEntry.ParseEntry( _fileDat );
                    if( Result != null )
                    {
                        _resultObject = Result;
                        ICQMessage theMsg = Result as ICQMessage;
                        if( theMsg != null )
                        {
                            // ignore messages with empty body and broken date
                            if( theMsg.Body.Length == 0 || theMsg.Time < _boundDate )
                            {
                                continue;
                            }
                            ++_currentContact.Messages;
                            if( theMsg.From == null )
                            {
                                theMsg.From = _currentContact;
                            }
                            else
                            {
                                theMsg.To = _currentContact;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region implementation details

        /**
         * returns UIN by name of ICQ database file
         * if result <= 0 than sFileName is not a ICQ database file
         */
        private static int ExtractUINByFileName( string sFileName )
        {
            int iUIN = 0;
            int iDotIndex = sFileName.ToLower().IndexOf( ".dat" );
            if( iDotIndex > 0 )
            {
                try
                {
                    iUIN = Convert.ToInt32( sFileName.Substring( 0, iDotIndex ) );
                }
                catch {}
            }
            return iUIN;
        }

        /**
         * opens db files for a specified UIN and reads header
         */
        private bool OpenDBFiles( int iUIN )
        {
            string sUIN = '\\' + iUIN.ToString();
            try
            {
                _fileDat = new BinaryReader( new CachedStream( new FileStream( _sDirectory + sUIN + ".dat",
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 256 ), CachingStrategy ) );
                _fileIdx = new BinaryReader( new CachedStream( new FileStream( _sDirectory + sUIN + ".idx",
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 256 ), CachingStrategy ) );
                // read idx header
                if( _fileIdx.ReadUInt32() != 4 || _fileIdx.ReadUInt32() != 20 || _fileIdx.ReadUInt32() != 8 )
                    throw new Exception( "Header of " + _sDirectory + sUIN + ".idx file is invalid" );
                _iCurrentIdx = _fileIdx.ReadInt32();
                _iDBVersion = _fileIdx.ReadInt32();

                // if the version is not supported then finish processing current database
                if( _iDBVersion != (int)DBVersion.db_2001a &&
                    _iDBVersion != (int)DBVersion.db_2000a &&
                    _iDBVersion != (int)DBVersion.db_2000b)
                    throw new Exception( "ICQ DB version is not supported: " + _iDBVersion );
            }
            catch( Exception e )
            {
                Trace.WriteLine( e.ToString(), "ICQ.DBImport" );
                return false;
            }
            return true;
        }

        private static void SeekBegin( BinaryReader reader, int offset )
        {
            reader.BaseStream.Seek( offset, SeekOrigin.Begin );
        }

        private static void SeekCurrent( BinaryReader reader, int offset )
        {
            reader.BaseStream.Seek( offset, SeekOrigin.Current );
        }

        private static string ReadANSIStr( BinaryReader reader )
        {
            ushort iStrLen = reader.ReadUInt16();
            if( iStrLen == 0 )
            {
                return string.Empty;
            }
            return Encoding.Default.GetString( reader.ReadBytes( iStrLen ) ).Trim( '\0' );
        }
        private static string ReadUTF8Str( BinaryReader reader )
        {
            ushort iStrLen = reader.ReadUInt16();
            if( iStrLen == 0 )
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString( reader.ReadBytes( iStrLen ) ).Trim( '\0' );
        }
        private static DateTime ReadDateTime( int iICQTimeStamp )
        {
            // it's rather strange but ICQ time is shifted on 7 hours
            DateTime DT = new DateTime( 1970, 1, 1 ).AddDays( ((double)iICQTimeStamp) / (24 * 60 * 60) );
            DT = DT.AddHours( _hourAddend );
            DT = new DateTime( DT.Year, DT.Month, DT.Day, DT.Hour, DT.Minute, DT.Second, 0 );
            return DT;
        }

        private class IdxRecord
        {
            public int Code;		// ff entry is valid the it's set to -2
            public int Number;		// DAT entry number
            public int Next;		// next IdxRecord offset
            public int Prev;		// previous IdxRecord offset
            public int DatPos;		// offfset in .dat file
            public bool ReadIdxRecord( int idx, BinaryReader reader )
            {
                try
                {
                    SeekBegin( reader, idx );
                    Code = reader.ReadInt32();
                    Number = reader.ReadInt32();
                    Next = reader.ReadInt32();
                    Prev = reader.ReadInt32();
                    DatPos = reader.ReadInt32();
                } 
                catch { return false; }
                return true;
            }
        }
        private class DatRecord
        {
            private int FillType;
            private int Number;
            private byte Command;
            private DBVersion iDBVersion;
            private bool _ignored;
            public bool ReadDatRecord( int idx, BinaryReader reader, DBVersion iDBVersion )
            {
                try
                {
                    SeekBegin( reader, idx );
                    reader.ReadInt32(); // skip length
                    FillType = reader.ReadInt32();
                    Number = reader.ReadInt32();
                    Command = reader.ReadByte();
                    reader.ReadBytes( 15 );
                    this.iDBVersion = iDBVersion;
                    _ignored = ( FillType == 2 );
                }
                catch { return false; }
                return FillType != 9 && ( !_ignored || ( Command == 0xe5 && Command == 0xe4 ) );
            }
            public object ParseEntry( BinaryReader reader )
            {
                try
                {
							
                    switch( Command )
                    {
                            // long message (ICQ99a-2002a)
                        case 0x50:
                            if( _ignored )
                            {
                                return null;
                            }
                            return ParseMessage( reader );
                            // short message & URL format (ICQ99a-2002a)
                        case 0xe0:
                        case 0xa0:
                            if( _ignored )
                            {
                                return null;
                            }
                            return ParseShortMessageOrURL( reader );
                            // contact
                        case 0xe5:
                            return ParseContact( reader );
                            // my details
                        case 0xe4:
                            return ParseMyDetails( reader );
                    }
                }
                catch {}
                return null;
            }
            private object ParseMessage( BinaryReader reader )
            {
                reader.ReadInt64();                         // skip separator, filling flags & entry subtype
                int iUIN = reader.ReadInt32();				// UIN of sender or receiver
                string sANSIBody = ReadANSIStr( reader );	// message body (ANSI text)
                reader.ReadInt32();                         // skip status of sender/receiver
                bool bIsMessageSent =
                    ( reader.ReadInt32() != 0 );            // is message sent ot received?
                reader.ReadInt16();                         // skip separator value
                int iTimeStamp = reader.ReadInt32();        // time stamp

                ICQMessage Result = new ICQMessage();
                if( sANSIBody.Length > 0 )
                {
                    Result.Body = sANSIBody;
                }
                else
                {
                    SeekCurrent( reader, 19 );              // skip zeroes
                    string sRTFBody = string.Empty;
                    string sUTF8Body = string.Empty;
                    try
                    {
                        sRTFBody = ReadANSIStr( reader );       // message body with rich text
                        sUTF8Body = ReadUTF8Str( reader );      // message body with UTF-8 text
                    }
                    catch( EndOfStreamException )
                    {
                    }
                    Result.Body = (sUTF8Body.Length > 0) ? sUTF8Body : sRTFBody;
                }
                ICQContact theContact = ContactsFactory.GetInstance().GetContact( iUIN );
                if( bIsMessageSent )
                {
                    Result.To = theContact;
                }
                else
                {
                    Result.From = theContact;
                }
                if( !( _ignored = theContact.Ignored ) )
                {
                    ++theContact.Messages;
                }
                Result.Time = ReadDateTime( iTimeStamp );
                return Result;
            }
            private object ParseShortMessageOrURL( BinaryReader reader )
            {
                SeekCurrent( reader, 6 );                   // skip separator & filling flags
                short iSubType = reader.ReadInt16();        // Entry subtype: 1 - message; 4: URL
                int iUIN = reader.ReadInt32();              // UIN of sender or receiver
                string sANSIBody = ReadANSIStr( reader );	// message body (ANSI text)
                reader.ReadInt32();                         // skip status of sender/receiver
                bool bIsMessageSent =
                    ( reader.ReadInt32() != 0 );            // is message sent ot received?
                reader.ReadInt16();                         // skip separator value
                int iTimeStamp = reader.ReadInt32();        // time stamp

                ICQMessage Result = new ICQMessage();
                if( iSubType == 1 )
                {
                    Result.Type = ICQMessage.Types.ShortMessage;
                }
                else if( iSubType == 4 )
                {
                    Result.Type = ICQMessage.Types.URL;
                }
                if( sANSIBody.Length > 0 )
                {
                    int i = sANSIBody.IndexOf( "http://", 1 );
                    if( i > 0 )
                    {
                        sANSIBody = sANSIBody.Substring( 0, i - 1 ) + "\r\n" + sANSIBody.Substring( i );
                    }
                    else
                    {
                        i = sANSIBody.IndexOf( "ftp://", 1 );
                        if( i > 0 )
                        {
                            sANSIBody = sANSIBody.Substring( 0, i - 1 ) + "\r\n" + sANSIBody.Substring( i );
                        }
                    }
                }
                Result.Body = sANSIBody;
                ICQContact theContact = ContactsFactory.GetInstance().GetContact( iUIN );
                if( bIsMessageSent )
                {
                    Result.To = theContact;
                }
                else
                {
                    Result.From = theContact;
                }
                if( !( _ignored = theContact.Ignored ) )
                {
                    ++theContact.Messages;
                }
                Result.Time = ReadDateTime( iTimeStamp );
                return Result;
            }

            private static void ParseProperty( ICQContact aContact, BinaryReader reader )
            {
                string sName = ReadANSIStr( reader );
                switch( reader.ReadByte() )
                {
                        // byte values
                    case 0x64:
                    case 0x65:
                    {
                        int iValue = reader.ReadByte();
                        if( sName == "Age" )
                        {
                            aContact.Age = iValue;
                        }
                        else if( sName == "Gender" )
                        {
                            aContact.Gender = (ICQContact.Genders)iValue;
                        }
                        else if( sName == "BirthDay" )
                        {
                            aContact.BirthDate =
                                ICQDbImportMisc.GetDate( aContact.BirthDate.Year, aContact.BirthDate.Month, iValue );
                        }
                        else if( sName == "BirthMonth" )
                        {
                            aContact.BirthDate =
                                ICQDbImportMisc.GetDate( aContact.BirthDate.Year, iValue, aContact.BirthDate.Day );
                        }
                        break;
                    }
                        // two-byte values
                    case 0x66:
                    case 0x67:
                    {
                        int iValue = reader.ReadInt16();
                        if( sName == "BirthYear")
                        {
                            aContact.BirthDate =
                                ICQDbImportMisc.GetDate( iValue, aContact.BirthDate.Month, aContact.BirthDate.Day );
                        }
                        break;
                    }
                        // four-byte values
                    case 0x68:
                    case 0x69:
                    {
                        int iValue = reader.ReadInt32();
                        if( sName == "UIN" )
                        {
                            aContact.UIN = iValue;
                        }
                        break;
                    }
                        // string values
                    case 0x6b:
                    {
                        string sValue = ReadANSIStr( reader );
                        if( sName == "NickName" )
                        {
                            aContact.NickName = sValue;
                        }
                        else if(sName == "MyDefinedHandle" )
                        {
                            aContact.MyDefinedHandle = sValue;
                        }
                        else if( sName == "FirstName" )
                        {
                            aContact.FirstName = sValue;
                        }
                        else if( sName == "LastName" )
                        {
                            aContact.LastName = sValue;
                        }
                        else if( sName == "PrimaryEmail" )
                        {
                            aContact.eMail = sValue;
                        }
                        else if( sName == "Company" )
                        {
                            aContact.Company = sValue;
                        }
                        else if( sName == "HomeAddress" )
                        {
                            aContact.Address = sValue;
                        }
                        else if( sName == "HomeHomepage" )
                        {
                            aContact.Homepage = sValue;
                        }
                        else if( sName == "Password" )
                        {
                            // for some unknown reasons, password is stored many times with the null value
                            if( aContact.Password.Length == 0 )
                            {
                                aContact.Password = sValue;
                            }
                        }
                        break;
                    }
                    case 0x6d:
                    {
                        int iSublistSize = reader.ReadInt32();
                        int type = reader.ReadByte();
                        if( type == 0x6b || type == 0x6e )
                        {
                            while( iSublistSize-- > 0 )
                            {
                                if( type == 0x6b )
                                {
                                    ReadANSIStr( reader );
                                }
                                else
                                {
                                    reader.ReadInt16();
                                    int iPropValues = reader.ReadInt32();
                                    while( iPropValues-- > 0 )
                                        ParseProperty( aContact, reader );
                                }
                            }
                        }
                        break;
                    }
                    case 0x6f:
                    {
                        SeekCurrent( reader, reader.ReadInt32() );
                        break;
                    }
                }
            }
            private object ParseContact( BinaryReader reader )
            {
                reader.ReadInt16();                     // skip separator
                if( reader.ReadInt32() != 0x55534552 )	// Label = 0x55534552 ('USER')
                {
                    return null;
                }
                SeekCurrent( reader, 10 ); // User entry status, GroupID of contact group containing user, Separator value
                // skip wav. entries
                if( iDBVersion == DBVersion.db_2000a || iDBVersion == DBVersion.db_2000b )
                {
                    int iWavEntries = reader.ReadInt32();
                    while( iWavEntries-- > 0 )
                    {
                        SeekCurrent( reader, 10 );
                        ReadANSIStr( reader );
                    }
                    reader.ReadInt16();
                }
                if( /*iSeparator >= 533 && */iDBVersion == DBVersion.db_2001a )
                {
                    SeekCurrent( reader, 6 );
                }

                ICQContact aContact = new ICQContact();

                // read property blocks
                int iPropBlocks = reader.ReadInt32();
                while( iPropBlocks-- > 0 )
                {
                    reader.ReadInt16();
                    int iProperties = reader.ReadInt32();
                    while( iProperties-- > 0 )
                    {
                        ParseProperty( aContact, reader );
                    }
                }
                reader.ReadInt16();
                reader.ReadInt32();
                /*int iTimeStamp = reader.ReadInt32();           // time stamp, time of last update
                aContact.LastUpdateTime = ReadDateTime( iTimeStamp );*/
                if( aContact.UIN == 0 )
                {
                    return null;
                }

                aContact.Ignored = _ignored;
                return ContactsFactory.GetInstance().Update( aContact, iDBVersion );
            }
            private object ParseMyDetails( BinaryReader reader )
            {
                if( Number != 1005 )
                {
                    return null;
                }
                reader.ReadInt16();                     // skip separator
                if( reader.ReadInt32() != 0x55534552 )	// Label = 0x55534552 ('USER')
                {
                    return null;
                }
                // the following is the sign of "My Details"
                if( reader.ReadInt32() != 6 )
                {
                    return null;
                }
                SeekCurrent( reader, 6 );
                // skip wav. entries
                if( iDBVersion == DBVersion.db_2000a || iDBVersion == DBVersion.db_2000b ||
                    ( iDBVersion == DBVersion.db_2001a ) )
                {
                    int iWavEntries = reader.ReadInt32();
                    while( iWavEntries-- > 0 )
                    {
                        SeekCurrent( reader, 10 );
                        ReadANSIStr( reader );
                    }
                    reader.ReadInt16();
                }
                /*if( iSeparator >= 533 && iDBVersion == DBVersion.db_2001a )
                    SeekCurrent( reader, 6 );*/

                ICQContact aContact = new ICQContact();

                // read property blocks
                int iPropBlocks = reader.ReadInt32();
                while( iPropBlocks-- > 0 )
                {
                    reader.ReadInt16();
                    int iProperties = reader.ReadInt32();
                    while( iProperties-- > 0 )
                    {
                        ParseProperty( aContact, reader );
                    }
                }
                reader.ReadInt16();
                reader.ReadInt32();
                /*int iTimeStamp = reader.ReadInt32();           // time stamp, time of last update
                aContact.LastUpdateTime = ReadDateTime( iTimeStamp );*/
                if( aContact.UIN == 0 )
                {
                    return null;
                }

                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // TODO: here contact's password should be decrypted
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                return ContactsFactory.GetInstance().Update( aContact, iDBVersion );
            }
        }

        #endregion

        private readonly string			_sDirectory;
        private IEnumerator		_itfFilesEnumerator;
        private BinaryReader    _fileDat;
        private BinaryReader    _fileIdx;
        private int				_iCurrentIdx;
        private int				_iDBVersion;
        private int				_iUIN;
        private ICQContact		_currentContact;
        private object			_resultObject;
        private bool			_bFinished;
        private bool			_bNewFile;
        private bool			_enumUINsOnly;
        private DateTime        _boundDate;
        private IdxRecord       _Idx;
        private DatRecord       _datEntry;
        private static readonly int      _hourAddend = DateTime.Now.Hour - DateTime.UtcNow.Hour + 3;
			
    }

    /**
     * ICQModernDatabase class imports history of 2003b and higher versions
     */
    internal class ICQModernDatabase : IICQDatabase, IDisposable
    {
        public ICQModernDatabase( string directory )
        {
            _directory = directory;
            try
            {
                _currentUIN = Convert.ToInt32( IOTools.GetFileName( directory ) );
            }
            catch( Exception ex )
            {
                Trace.WriteLine( ex.Message );
                Dispose();
                return;
            }
            _currentContact = ContactsFactory.GetInstance().GetContact( _currentUIN );
            _skipUpdate = false;
            Reset();
        }

        #region IICQDatabase Members

        public override string CurrentLocation
        {
            get { return _directory; }
        }
        public override DBVersion CurrentDBVersion
        {
            get { return DBVersion.db_2003b; }
        }

        public override int CurrentUIN
        {
            get { return _currentUIN; }
        }

        public override bool EnumUINsOnly
        {
            get { return _enumUINsOnly; }
            set { _enumUINsOnly = value; }
        }

        public override void SkipUpdate()
        {
            _skipUpdate = true;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if( !_skipUpdate )
            {
                // marshall update record numbers with lowest priority in order to make this
                // afters conversations are built
                if( _users != null )
                {
                    Core.ResourceAP.QueueJob( JobPriority.Lowest,
                        new UpdateRecordNumbersDelegate( UpdateRecordNumbers ), _users.FileName, _users.RecordCount - 1 );
                }
                if( _messages != null )
                {
                    Core.ResourceAP.QueueJob( JobPriority.Lowest,
                        new UpdateRecordNumbersDelegate( UpdateRecordNumbers ), _messages.FileName, _messages.RecordCount - 1 );
                }
            }
            _currentUIN = 0;
            if( _messages != null )
            {
                _messages.Dispose();
                _messages = null;
            }
            if( _users != null )
            {
                _users.Dispose();
                _users = null;
            }
        }

        private delegate void UpdateRecordNumbersDelegate( string tableName, int count );

        private static void UpdateRecordNumbers( string tableName, int count )
        {
            if( !Core.NetworkAP.IsOwnerThread )
            {
                Core.NetworkAP.QueueJob( new UpdateRecordNumbersDelegate( UpdateRecordNumbers ), tableName, count );
            }
            else
            {
                if( Core.State == CoreState.Running )
                {
                    ObjectStore.WriteInt( "ICQDbImportTableRecordNumbers", tableName, ( count > 0 ) ? count - 1 : -1 );
                }
            }
        }

        #endregion

        #region implementation of IEnumerator & IEnumerable

        public override IEnumerator GetEnumerator()
        {
            return this;
        }

        public override void Reset()
        {
            _enumUINsOnly = !ICQPlugin.IndexedUIN( _currentUIN );
            if( !_enumUINsOnly )
            {
                _messages = new DBFTable(
                    IOTools.Combine( _directory, "Messages" + _currentUIN + ".dbf" ), true, CachingStrategy );
                if( !_messages.IsOk )
                {
                    Dispose();
                    return;
                }
                _messages.SetRecordArray( new ArrayList() );
                _messages.Fields[ 8 ]._type = FieldType.Blob;
                _users = new DBFTable(
                    IOTools.Combine( _directory, "Users" + _currentUIN + ".dbf" ), true, CachingStrategy );
                if( !_users.IsOk )
                {
                    Dispose();
                    return;
                }
                _users.SetRecordArray( new ArrayList() );
                _users.Fields[ 9 ]._type = FieldType.Blob;
                _userRecord = ObjectStore.ReadInt( "ICQDbImportTableRecordNumbers", _users.FileName, -1 );
                _messageRecord = ObjectStore.ReadInt( "ICQDbImportTableRecordNumbers", _messages.FileName, -1 );
            }
            DBFTable owners = new DBFTable(
                IOTools.Combine( _directory, "O" + _currentUIN + ".dbf" ), true, CachingStrategy );
            try
            {
                if( owners.IsOk )
                {
                    owners.Fields[ 2 ]._type = FieldType.Blob;
                    for( int i = 0; i < owners.RecordCount; ++i )
                    {
                        ArrayList record = owners[ i ];
                        if( record != null )
                        {
                            byte[] blob = (byte[]) record[ 2 ];
                            ParseContactProperties( blob, _currentContact, true );
                        }
                    }
                }
            }
            finally
            {
                owners.Dispose();
            }
        }

        public override object Current
        {
            get
            {
                if( !_enumUINsOnly )
                {
                    int uin;
                    while( _userRecord < _users.RecordCount )
                    {
                        ArrayList record = _users[ _userRecord ];
                        if( record != null )
                        {
                            uin = GetUIN( record );
                            if( uin != 0 )
                            {
                                ICQContact contact = ContactsFactory.GetInstance().GetContact( uin );
                                contact.UIN = uin;
                                contact.NickName = (string) record[ 2 ];
                                contact.FirstName = (string) record[ 3 ];
                                contact.LastName = (string) record[ 4 ];
                                contact.Ignored = (bool) record[ 8 ];
                                byte[] blob = (byte[]) record[ 9 ];
                                ParseContactProperties( blob, contact, false );
                                return contact;
                            }
                        }
                        ++_userRecord;
                    }
                    while( _messageRecord < _messages.RecordCount )
                    {
                        ArrayList record = _messages[ _messageRecord ];
                        if( record != null )
                        {
                            uin = GetUIN( record );
                            if( uin != 0 )
                            {
                                ICQContact contact = ContactsFactory.GetInstance().GetContact( uin );
                                ICQMessage message = new ICQMessage();
                                byte[] blob = (byte[]) record[ 8 ];
                                int statusIndex = SearchMemoForAttributeValue( blob, "Status" );
                                bool isMessageSent = ( statusIndex > 0 ) && ( blob[ statusIndex ] != 0 );
                                if( isMessageSent )
                                {
                                    message.From = _currentContact;
                                    message.To = contact;
                                }
                                else
                                {
                                    message.From = contact;
                                    message.To = _currentContact;
                                }
                                message.Body = (string) record[ 9 ];
                                string timeString = (string) record[ 6 ];
                                message.Time = ParseDateTime( timeString );
                                ++_currentContact.Messages;
                                ++contact.Messages;
                                return message;
                            }
                        }
                        ++_messageRecord;
                    }
                }
                return null;
            }
        }

        public override bool MoveNext()
        {
            if( !_enumUINsOnly && _currentUIN > 0 )
            {
                if( ++_userRecord < _users.RecordCount )
                {
                    return true;
                }
                if( ++_messageRecord < _messages.RecordCount )
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region implementation details

        private static int SearchMemoForAttributeValue( byte[] blob, string attr )
        {
            int attrLength = attr.Length;
            for( int i = 0; i < blob.Length - attrLength; ++i )
            {
                bool eq = true;
                for( int j = 0; j < attrLength; ++j )
                {
                    if( !( eq = ( blob[ i + j ] == (byte)attr[ j ] ) ) )
                    {
                        break;
                    }
                }
                if( eq )
                {
                    return i + attrLength + 2;
                }
            }
            return -1;
        }

        private static string ParseASCIIString( byte[] blob, int index )
        {
            int len = blob[ index ] + ( blob[ index + 1 ] << 8 );
            if( len + index + 2 > blob.Length )
            {
                len = blob.Length - index - 2;
            }
            string result = string.Empty;
            if( len > 0 )
            {
                try
                {
                    result = Encoding.ASCII.GetString( blob, index + 2, len ).TrimEnd( '\0' );
                }
                catch {}
            }
            return result;
        }

        private static string ParseUnicodeString( byte[] blob, int index )
        {
            int len = blob[ index ] + ( blob[ index + 1 ] << 8 ) + ( blob[ index + 2 ] << 16 ) + ( blob[ index + 3 ] << 24 );
            if( len > 0 && len + index + 2 > blob.Length )
            {
                len = blob.Length - index - 4;
            }
            string result = string.Empty;
            if( len > 0 )
            {
                try
                {
                    result = Encoding.Unicode.GetString( blob, index + 4, len ).TrimEnd( '\0' );
                }
                catch {}
            }
            return result;
        }

        private static int ParseInt( string str, int offset, int len )
        {
            int result;
            try
            {
                result = Convert.ToInt32( str.Substring( offset, len ) );
            }
            catch
            {
                result = 0;
            }
            return result;
        }

        private static int GetUIN( ArrayList record )
        {
            string uinStr = (string) record[ 1 ];
            return ParseInt( uinStr, 0, uinStr.Length );
        }

        private static DateTime ParseDateTime( string timeString )
        {
            int year = ParseInt( timeString, 0, 4 );
            int month = ParseInt( timeString, 4, 2 );
            int day = ParseInt( timeString, 6, 2 );
            int hour = ParseInt( timeString, 8, 2 );
            int minute = ParseInt( timeString, 11, 2 );
            int second = ParseInt( timeString, 14, 2 );
            DateTime result;
            try
            {
                result = new DateTime( year, month, day, hour, minute, second, 0 );
                result = result.AddHours(  _hourAddend );
            }
            catch
            {
                result = DateTime.MinValue;
            }
            return result;
        }

        private static void ParseContactProperties( byte[] blob, ICQContact contact, bool mySelf )
        {
            int index = SearchMemoForAttributeValue( blob, "Age" );
            if( index > 0 )
            {
                contact.Age = blob[ index ];
            }
            index = SearchMemoForAttributeValue( blob, "Gender" );
            if( index > 0 )
            {
                contact.Gender = (ICQContact.Genders) blob[ index ];
            }
            index = SearchMemoForAttributeValue( blob, "BirthDay" );
            if( index > 0 )
            {
                contact.BirthDate =
                    ICQDbImportMisc.GetDate( contact.BirthDate.Year, contact.BirthDate.Month, blob[ index ] );
            }
            index = SearchMemoForAttributeValue( blob, "BirthMonth" );
            if( index > 0 )
            {
                contact.BirthDate =
                    ICQDbImportMisc.GetDate( contact.BirthDate.Year, blob[ index ], contact.BirthDate.Day );
            }
            index = SearchMemoForAttributeValue( blob, "BirthYear" );
            if( index > 0 && index < blob.Length - 1 )
            {
                int year = blob[ index ] + ( ( blob[ index + 1 ] ) << 8 );
                contact.BirthDate = ICQDbImportMisc.GetDate( year, contact.BirthDate.Month, contact.BirthDate.Day );
            }
            index = SearchMemoForAttributeValue( blob, "PrimaryEmail" );
            if( index > 0 )
            {
                contact.eMail = ParseASCIIString( blob, index );
            }
            index = SearchMemoForAttributeValue( blob, "HomeHomepage" );
            if( index > 0 )
            {
                contact.Homepage = ParseASCIIString( blob, index );
            }
            index = SearchMemoForAttributeValue( blob, "HomeAddress" );
            if( index > 0 )
            {
                contact.Address = ParseASCIIString( blob, index );
            }
            index = SearchMemoForAttributeValue( blob, "Company" );
            if( index > 0 )
            {
                contact.Company = ParseASCIIString( blob, index );
            }
            index = SearchMemoForAttributeValue( blob, "Alias" );
            if( index > 0 )
            {
                string alias = ParseUnicodeString( blob, index );
                if( alias.Length > 0 )
                {
                    contact.NickName = alias;
                }
            }
            if( mySelf )
            {
                index = SearchMemoForAttributeValue( blob, "NickName" );
                if( index > 0 )
                {
                    contact.NickName = ParseASCIIString( blob, index );
                }
                index = SearchMemoForAttributeValue( blob, "FirstName" );
                if( index > 0 )
                {
                    contact.FirstName = ParseASCIIString( blob, index );
                }
                index = SearchMemoForAttributeValue( blob, "LastName" );
                if( index > 0 )
                {
                    contact.LastName = ParseASCIIString( blob, index );
                }
            }
        }

        private bool			_enumUINsOnly;
        private readonly string _directory;
        private int             _currentUIN;
        private readonly ICQContact _currentContact;
        private DBFTable        _messages;
        private DBFTable        _users;
        private int             _messageRecord;
        private int             _userRecord;
        private bool            _skipUpdate;
        private static readonly int  _hourAddend = DateTime.Now.Hour - DateTime.UtcNow.Hour;

        #endregion
    }

    
    /**
     * the Importer singleton class enumerates ICQ databases
     */
    internal class Importer : IEnumerable, IEnumerator
    {
        private static readonly Importer theImporter = new Importer();
        public static Importer GetInstance()
        {
            return theImporter;
        }

        public IEnumerator GetEnumerator()
        {
            return this;
        }

        public object Current
        {
            get { return _CurrentDB; }
        }
        public bool MoveNext()
        {
            if( !ICQPlugin.GetImportOnly2003b() )
            {
                while( _ICQHKLMPreferencesKey != null )
                {
                    if( !_itfHKLMValueNamesEnumerator.MoveNext() )
                    {
                        _ICQHKLMPreferencesKey = null;
                    }
                    else
                    {
                        string currentValueName = (string) _itfHKLMValueNamesEnumerator.Current;
                        if( currentValueName.IndexOf( "Database" ) >= 0 )
                        {
                            object currentValue = _ICQHKLMPreferencesKey.GetValue( currentValueName );
                            if( currentValue is string )
                            {
                                _CurrentDB = new ICQDatabase( (string) currentValue );
                                return true;
                            }
                        }
                    }
                }
                while( _ICQHKCUPreferencesKey != null )
                {
                    if( !_itfHKCUValueNamesEnumerator.MoveNext() )
                    {
                        _ICQHKCUPreferencesKey = null;
                    }
                    else
                    {
                        string currentValueName = (string) _itfHKCUValueNamesEnumerator.Current;
                        if( currentValueName.IndexOf( "Database" ) >= 0 )
                        {
                            object currentValue = _ICQHKCUPreferencesKey.GetValue( currentValueName );
                            if( currentValue is string )
                            {
                                _CurrentDB = new ICQDatabase( (string) currentValue );
                                return true;
                            }
                        }
                    }
                }
                while( _ICQ2003bHKLMPreferencesKey != null )
                {
                    if( !_itf2003bHKLMValueNamesEnumerator.MoveNext() )
                    {
                        _ICQ2003bHKLMPreferencesKey = null;
                    }
                    else
                    {
                        string currentValueName = (string) _itf2003bHKLMValueNamesEnumerator.Current;
                        if( currentValueName.IndexOf( "Database" ) >= 0 )
                        {
                            object currentValue = _ICQ2003bHKLMPreferencesKey.GetValue( currentValueName );
                            if( currentValue is string )
                            {
                                _CurrentDB = new ICQDatabase( (string) currentValue );
                                return true;
                            }
                        }
                    }
                }
                while( _ICQ2003bHKCUPreferencesKey != null )
                {
                    if( !_itf2003bHKCUValueNamesEnumerator.MoveNext() )
                    {
                        _ICQ2003bHKCUPreferencesKey = null;
                    }
                    else
                    {
                        string currentValueName = (string) _itf2003bHKCUValueNamesEnumerator.Current;
                        if( currentValueName.IndexOf( "Database" ) >= 0 )
                        {
                            object currentValue = _ICQ2003bHKCUPreferencesKey.GetValue( currentValueName );
                            if( currentValue is string )
                            {
                                _CurrentDB = new ICQDatabase( (string) currentValue );
                                return true;
                            }
                        }
                    }
                }
            }
            if( _newDBs.Count > 0 )
            {
                _CurrentDB = new ICQModernDatabase( (string) _newDBs.Dequeue() );
                return true;
            }
            return false;
        }
        public void Reset()
        {
            OpenRegKey( ICQ2003bDefaultPrefsRegKey,
                        Registry.LocalMachine, out _ICQ2003bHKLMPreferencesKey, out _itf2003bHKLMValueNamesEnumerator );
            OpenRegKey( ICQ2003bDefaultPrefsRegKey,
                Registry.CurrentUser, out _ICQ2003bHKCUPreferencesKey, out _itf2003bHKCUValueNamesEnumerator );
            OpenRegKey( ICQDefaultPrefsRegKey,
                Registry.LocalMachine, out _ICQHKLMPreferencesKey, out _itfHKLMValueNamesEnumerator );
            OpenRegKey( ICQDefaultPrefsRegKey,
                Registry.CurrentUser, out _ICQHKCUPreferencesKey, out _itfHKCUValueNamesEnumerator );
            string icqPath = null;
            if( _ICQ2003bHKLMPreferencesKey != null )
            {
                icqPath = (string) _ICQ2003bHKLMPreferencesKey.GetValue( "ICQPath" );
            }
            if( icqPath == null && _ICQ2003bHKCUPreferencesKey != null )
            {
                icqPath = (string) _ICQ2003bHKCUPreferencesKey.GetValue( "ICQPath" );
            }
            _newDBs.Clear();
            EnumModernDatabases( icqPath );
        }

        private static void OpenRegKey( string regKey, RegistryKey baseKey, out RegistryKey key, out IEnumerator values )
        {
            key = null;
            values = null;
            try
            {
                key = baseKey.OpenSubKey( regKey );
            }
            catch { return; }
            if( key != null )
            {
                values = key.GetValueNames().GetEnumerator();
            }
        }

        private void EnumModernDatabases( string path )
        {
            if( string.IsNullOrEmpty( path ) )
            {
                return;
            }
            do
            {
                try
                {
                    Convert.ToInt32( IOTools.GetFileName( path ) );   
                }
                catch
                {
                    break;
                }
                FileInfo[] files = IOTools.GetFiles( path, "messages*.dbf" );
                if( files != null && files.Length > 0 )
                {
                    files = IOTools.GetFiles( path, "users*.dbf" );
                    if( files != null && files.Length > 0 )
                    {
                        _newDBs.Enqueue( path );
                    }
                }
            }
            while( false );
            DirectoryInfo[] dirs = IOTools.GetDirectories( path );
            if( dirs != null )
            {
                foreach( DirectoryInfo di in dirs )
                {
                    EnumModernDatabases( IOTools.GetFullName( di ) );
                }
            }
        }

        private IICQDatabase    _CurrentDB;
        private RegistryKey     _ICQHKLMPreferencesKey;
        private RegistryKey     _ICQHKCUPreferencesKey;
        private RegistryKey     _ICQ2003bHKLMPreferencesKey;
        private RegistryKey     _ICQ2003bHKCUPreferencesKey;
        private IEnumerator		_itfHKLMValueNamesEnumerator;
        private IEnumerator		_itfHKCUValueNamesEnumerator;
        private IEnumerator		_itf2003bHKLMValueNamesEnumerator;
        private IEnumerator		_itf2003bHKCUValueNamesEnumerator;
        private readonly Queue  _newDBs = new Queue();
        private const string	ICQDefaultPrefsRegKey = @"SOFTWARE\Mirabilis\ICQ\DefaultPrefs";
        private const string	ICQ2003bDefaultPrefsRegKey = @"SOFTWARE\Mirabilis\ICQ\ICQPro\DefaultPrefs";
    }

    /**
     * ICQ contacts factory serves for merging same contacts, which may
     * be stored in different ICQ database files
     */
    internal class ContactsFactory
    {
        private ContactsFactory() {}
        public static ContactsFactory GetInstance()
        {
            return _theInstance;
        }

        public ICQContact GetContact( int iUIN )
        {
            ContactEntry theEntry = (ContactEntry) _Contacts[ iUIN ];
            if( theEntry == null )
            {
                ICQContact theContact = new ICQContact();
                theContact.UIN = iUIN;
                theEntry = new ContactEntry();
                theEntry.theContact = theContact;
                theEntry.DBVersion = DBVersion.db_Undefined;
                _Contacts.Add( iUIN, theEntry);
            }
            return theEntry.theContact;
        }
        public ICQContact Update( ICQContact aContact, DBVersion aVersion )
        {
            ContactEntry theEntry = (ContactEntry) _Contacts[ aContact.UIN ];
            if( theEntry == null )
            {
                theEntry = new ContactEntry();
                theEntry.DBVersion = aVersion;
                theEntry.theContact = aContact;
                _Contacts[ aContact.UIN ] = theEntry;
            }
            else
            {
                if( theEntry.DBVersion < aVersion )
                {
                    theEntry.DBVersion = aVersion;
                    // copy all properties in order to effect all data
                    // where the contact was stores (messages and so on)
                    theEntry.theContact.Address = aContact.Address;
                    theEntry.theContact.Age = aContact.Age;
                    theEntry.theContact.BirthDate = aContact.BirthDate;
                    theEntry.theContact.Company = aContact.Company;
                    theEntry.theContact.eMail = aContact.eMail;
                    theEntry.theContact.FirstName = aContact.FirstName;
                    theEntry.theContact.Gender = aContact.Gender;
                    theEntry.theContact.LastName = aContact.LastName;
                    theEntry.theContact.NickName = aContact.NickName;
                    theEntry.theContact.Password = aContact.Password;
                    theEntry.theContact.Messages = aContact.Messages;
                    theEntry.theContact.Ignored = aContact.Ignored;
                }
            }
            return theEntry.theContact;
        }

        private class ContactEntry
        {
            public ICQContact theContact;
            public DBVersion DBVersion;
        }
        private static readonly ContactsFactory _theInstance = new ContactsFactory();
        private readonly IntHashTable           _Contacts = new IntHashTable();
    }

    /**
     * ICQ UINs collection (found on computer)
     */
    internal class UINsCollection
    {
//        private static IntArrayList _UINs = new IntArrayList();
        private static readonly List<int> _UINs = new List<int>();
        private static bool         _hasModernDBs;

        static UINsCollection()
        {
            Refresh();
        }

        public static void Refresh()
        {
            DBImport.Importer theImporter = DBImport.Importer.GetInstance();
            theImporter.Reset();

            _hasModernDBs = false;
    
            IntHashSet uins = new IntHashSet();
            int lastUIN = 0;
    
            foreach( IICQDatabase D in theImporter )
            {
                _hasModernDBs = _hasModernDBs || D is ICQModernDatabase;
                D.EnumUINsOnly = true;
                lastUIN = D.CurrentUIN;
                if( lastUIN != 0 )
                {
                    uins.Add( lastUIN );
                }
                while( D.MoveNext() )
                {
                    if( lastUIN != D.CurrentUIN )
                    {
                        uins.Add( lastUIN = D.CurrentUIN );
                    }
                }
            }
            _UINs.Clear();
            foreach( IntHashSet.Entry e in uins )
            {
                _UINs.Add( e.Key );
            }
            _UINs.Sort();
        }

        public static List<int> GetUINs()
        {
            return _UINs;
        }

        public static bool HasModernDBs
        {
            get { return _hasModernDBs; }
        }
    }

    internal class ICQDbImportMisc
    {
        internal static DateTime GetDate( int year, int month, int day )
        {
            DateTime date;
            try
            {
                date = new DateTime( year, month, day );
            }
            catch
            {
                try
                {
                    date = new DateTime( DateTime.Now.Year, month, day );
                }
                catch
                {
                    try
                    {
                        date = new DateTime( year, 1, day );
                    }
                    catch
                    {
                        try
                        {
                            date = new DateTime( year, month, 1 );
                        }
                        catch
                        {
                            date = DateTime.Now;
                        }
                    }
                }
            }
            return date;
        }
    }
}