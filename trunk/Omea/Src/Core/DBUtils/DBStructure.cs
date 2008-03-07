/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using JetBrains.DataStructures;
using JetBrains.Omea.Diagnostics;
using System.Threading;

namespace JetBrains.Omea.Database
{
    public class DBStructure
    {
        private const int VERSION = 23;
        private HashMap _tables = new HashMap();
        private string _dbName;
        private string _path;
        private int _version = VERSION;
        private bool _dirty = false;
        private Tracer _tracer;
        private Database _database = null;
        private string _build = "undefined";
        private DatabaseMode _dbMode = DatabaseMode.Open;
        private bool _forceRebuild = false;

        // Upgrades
        private bool _upgradeTo22 = false;

        public DBStructure( string path, string dbName, DatabaseMode dbMode ) 
        { 
            _dbMode = dbMode;
            Init( path, dbName );
        }

        public DBStructure( string path, string dbName ) 
        { 
            Init( path, dbName );
        }

        public void LoadVersionInfo()
        {
            if ( DBHelper.DatabaseExists( _path, _dbName ) )
            {
                _tracer.Trace( "(DBUtils) LoadVersionInfo BEGIN " );
                try
                {
                    OpenDBStructureFile( false );
                    _structReader.ReadString();
                    _version = _structReader.ReadInt32();
                    if ( _version > 0 )
                    {
                        _build = _structReader.ReadString();
                        _tracer.Trace( "Build is " + _build );
                    }
                }
                catch ( Exception exception )
                {
                    _tracer.TraceException( exception );
                    throw new CannotGetVersionInfoException( "Cannot get version info", exception );
                }
                finally
                {
                    CloseDBStructureFile();
                }
                _tracer.Trace( "(DBUtils) LoadVersionInfo END " );
            }
        }

        private void Init( string path, string dbName )
        {
            _path = path;
            _dbName = dbName; 
            _tracer = new Tracer( "(DBUtils) DBStructure - " + dbName );
            _tracer.Trace( "(DBUtils) DBStructure mode = " + _dbMode.ToString() );
        }

        public DatabaseMode Mode
        {
            get 
            {
                return _dbMode;
            }
        }

        public string Build
        {
            set
            { 
                _build = value; 
                _tracer.Trace( _build );
            }
            get
            { 
                return _build; 
            }
        }

        public void Dump()
        {
            _database.Dump();
        }

        public TableStructure CreateTable( string name )
        {
            if ( !_tables.Contains( name ) )
            {
                TableStructure table = new TableStructure( name, this );
                _tables.Add( name, table );
                return table;
            }
            throw new TableAlreadyExistsException( "Table with this name already exists", name );
        }

        public TableStructure GetTable( string name )
        {
            if ( !_tables.Contains( name ) )
            {
                throw new TableDoesNotExistException( "", name );
            }
            return (TableStructure)_tables[name];
        }
        public void SaveStructure( )
        {
            Monitor.Enter( this );
            OpenDBStructureFile( true );
            try
            {
                _structWriter.Write( "DB" );
                _structWriter.Write( VERSION );
                _version = VERSION;
                _structWriter.Write( _build );
                _structWriter.Write( _tables.Count );
                foreach ( HashMap.Entry entry in _tables )
                {
                    ((TableStructure)entry.Value).SaveStructure( _structWriter );
                }
            }
            finally
            {
                if ( _stream != null ) 
                {
                    _stream.Flush();
                }
                Monitor.Exit( this );
            }
        }

        public bool IsDatabaseCorrect()
        {
            return !_dirty;
        }

        public void Defragment( )
        {
            _tracer.Trace( "Defragmentation tables" );
            InitDatabase();
            SetSortedColumns();
            _database.ProgressListenerEvent += new Database.ProgressEventHandler( OnRebuildProgress );

            int ticks = System.Environment.TickCount;
            _database.Defragment( );
            int totalTicks = System.Environment.TickCount - ticks;
            System.Diagnostics.Trace.WriteLine( "Rebuild Indexes took: " + totalTicks.ToString() );
            UninitDatabase();
        }

        public void SetSortedColumns()
        {
            ((Table)_database.GetTable( "Resources" )).SortedColumn = 1;
            ((Table)_database.GetTable( "IntProps" )).SortedColumn = 0;
            ((Table)_database.GetTable( "StringProps" )).SortedColumn = 1;
            ((Table)_database.GetTable( "LongStringProps" )).SortedColumn = 0;
            ((Table)_database.GetTable( "DateProps" )).SortedColumn = 0;
            ((Table)_database.GetTable( "BlobProps" )).SortedColumn = 0;
            ((Table)_database.GetTable( "DoubleProps" )).SortedColumn = 0;
            ((Table)_database.GetTable( "BoolProps" )).SortedColumn = 1;
            ((Table)_database.GetTable( "Links" )).SortedColumn = 2;
            ((Table)_database.GetTable( "StringListProps" )).SortedColumn = -1;
        }

        public bool NeedUpgradeTo22 { get { return _upgradeTo22; } }

        public void LowLevelCheck()
        {
            if ( _version < 22 )
            {
                //Console.WriteLine( "Cannot check database" );
            }

            InitDatabase();
            _database.LowLevelCheck();
            UninitDatabase();
        }

        private void UninitDatabase()
        {
            _database.ProgressListenerEvent -= new Database.ProgressEventHandler( OnRebuildProgress );
            _database.Shutdown();
            _database = null;
            _dirty = false;
        }

        private void InitDatabase()
        {
            if ( _database == null )
            {
                _database = new Database( this );
                IDatabaseDesign dbDesign = _database; 
                foreach ( HashMap.Entry entry in _tables )
                {
                    try
                    {
                        ((TableStructure)entry.Value).OpenTable( dbDesign );
                    }
                    catch( IndexIsCorruptedException ) {}
                }
            }
            _database.ProgressListenerEvent += new Database.ProgressEventHandler( OnRebuildProgress );
        }

        public void RebuildIndexes( )
        {
            RebuildIndexes( _forceRebuild );
        }
        public void RebuildIndexes( bool forceRebuild )
        {
            _forceRebuild = forceRebuild;
            _tracer.Trace( "Rebuild indexes" );

            InitDatabase();

            int ticks = System.Environment.TickCount;
            if ( _upgradeTo22 )
            {
                _database.Defragment();
                _upgradeTo22 = false;
            }
            else
            {
                _database.RebuildIndexes( _forceRebuild );
            }
            SaveStructure();
            int totalTicks = System.Environment.TickCount - ticks;
            System.Diagnostics.Trace.WriteLine( "Rebuild Indexes took: " + totalTicks.ToString() );
            UninitDatabase();
        }

        public int TablesCount
        {
            get { return _tables.Count; }
        }

        public void LoadStructure()
        {
            LoadStructure( false );
        }

        private FileStream _stream = null;
        private BinaryWriter _structWriter = null;
        private BinaryReader _structReader = null;
        private void OpenDBStructureFile( bool truncate )
        {
            if ( _stream == null )
            {
                string strFileName = DBHelper.GetFullNameForDBStruct( _path, _dbName );
                _stream = File.Open( strFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read );
                _structWriter = new BinaryWriter( _stream );
                _structReader = new BinaryReader( _stream );
            }
            if ( truncate )
            {
                _stream.SetLength( 0 );
                _stream.Flush();
            }
            _structWriter.BaseStream.Position = 0;
            _structReader.BaseStream.Position = 0;
        }
        private void CloseDBStructureFile()
        {
            if ( _stream != null )
            {
                _stream.Close();
                _stream = null;
                _structWriter.Close();
                _structWriter = null;
                _structReader.Close();
                _structReader = null;
            }
        }
        
        public void LoadStructure( bool headerOnly )
        {
            Monitor.Enter( this );
            try
            {
                ShutdownImpl();
                OpenDBStructureFile( false );
                _structReader.ReadString();
                _version = _structReader.ReadInt32();
                if ( _version > 0 )
                {
                    _build = _structReader.ReadString();
                    _tracer.Trace( "Build is " + _build );
                }
                if ( _version > VERSION )
                {
                    throw new BackwardIncompatibility( "Cannot load database ( version = " + _version + 
                        " ) + with engine ( version = " + VERSION + " )" );
                }
                if ( _version != VERSION )
                {
                    _dirty = true;
                    _forceRebuild = true;
                    if ( _version < 22 && VERSION == 22 )
                    {
                        _upgradeTo22 = true;
                    }
                    else
                    {
                        DBHelper.RemoveDBIndexFiles( _path );
                    }
                }

                int count = _structReader.ReadInt32();
                for ( int i = 0; i < count; i++ )
                {
                    TableStructure table = new TableStructure( _structReader, this );
                    _dirty = _dirty || table.Dirty;
                    _tables.Add( table.Name, table );
                }
            }
            finally
            {
                if ( !headerOnly )
                {
                    _database = OpenDatabaseInternal();
                }
                Monitor.Exit( this );
            }
        }

        public int Version { get { return _version; } }
        public string Name { get { return _dbName; } }
        public string Path { get { return _path; } }
        public IDatabase Database { get { return _database; } }

        public IDatabase OpenDatabase()
        {
            return OpenDatabaseInternal( );
        }
        
        private Database OpenDatabaseInternal()
        {
            _dbMode = DatabaseMode.Open;
            if ( _database == null )
            {
                _database = new Database( this );
                IDatabaseDesign dbDesign = _database; 
                foreach ( HashMap.Entry entry in _tables )
                {
                    try
                    {
                        ((TableStructure)entry.Value).OpenTable( dbDesign );
                    }
                    catch ( IndexIsCorruptedException exception )
                    {
                        _tracer.TraceException( exception );
                        _dirty = true;
                    }
                }
            }
            return _database;
        }
        private void OnRebuildProgress( string progress, int tableNum, int tableCount )
        {
            OnProgress( progress, tableNum, tableCount );
        }
        public void Shutdown()
        {
            ShutdownImpl();
            CloseDBStructureFile();
        }
        private void ShutdownImpl()
        {
            if ( _database != null )
            {
                _database.Shutdown();
                _database = null;
            }
            _dirty = false;
            _tables.Clear();
        }
        internal void Shutdowned()
        {
            _database = null;
            CloseDBStructureFile();
        }
        public void RemoveDBFiles()
        {
            if ( _database != null )
            {
                Shutdown();
            }
            DBHelper.RemoveDBFiles( _path );
        }
        public delegate void ProgressEventHandler( string progress, int tableNum, int tableCount );
        public event ProgressEventHandler ProgressListenerEvent;
        public void OnProgress( string progress, int tableNum, int tableCount )
        {
            if ( ProgressListenerEvent != null )
            {
                ProgressListenerEvent( progress, tableNum, tableCount );
            }
        }
    }
}