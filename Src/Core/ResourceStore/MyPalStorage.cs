// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using JetBrains.Omea.Base;
using JetBrains.Omea.Database;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.DataStructures;

namespace JetBrains.Omea.ResourceStore
{
    public class MyPalStorage: IResourceStore
    {
        private static MyPalStorage _theStorage;
        private static string _dbPath = ".\\db";

        private DBStructure _dbStructure;
        private IDatabase   _database;
        private bool        _indexesRebuilt;
        private bool        _initializationComplete = false;
        private bool        _shutDown = false;
        private static bool _trace = false;

        private ITable _propTypeTable;
        private ITable _resourceTypeTable;
        private ITable _resources;
        private ITable _intProps;
        private ITable _stringProps;
        private ITable _longStringProps;
        private ITable _dateProps;
        private ITable _blobProps;
        private ITable _doubleProps;
        private ITable _boolProps;
        private ITable _stringListProps;
        private ITable _links;

        private ArrayList _propTables = new ArrayList();
        private ArrayList _contentTables = new ArrayList();

        private IntObjectCache _resourceCache;
        private IntWeakHashTable _resourceWeakCache;
        private IntWeakHashTable _transientResources = new IntWeakHashTable( 256 );  // resource ID -> Resource

        private PropTypeCollection _propTypes;
        private ResourceTypeCollection _resourceTypes;
        private ResourceStoreProps _props;

        private IntHashTable _updatingResources  = new IntHashTable();  // resource ID -> MultiPropChangeSet

        private ITextIndexManager _textIndexManager;

        private static int _resourceCacheSize = 2048;

        private ResourceListUpdateManager _updateManager = new ResourceListUpdateManager();

        public event ResourcePropEventHandler ResourceSaved;
        public event EventHandler ResourceDeleting;
        public event LinkEventHandler LinkAdded;
        public event LinkEventHandler LinkDeleted;
        public event EventHandler IndexCorruptionDetected;
        public event ThreadExceptionEventHandler IOErrorDetected;

        private Thread _ownerThread = null;
        private static IProgressWindow _progressWindow = null;

        private IResourceList _emptyResourceList;

        private bool _repairRequired = false;
        private bool _fullRepairRequired = false;
        private bool _indexCorruptionReported = false;
        private bool _ioErrorReported = false;

        private HashMap _predicateCache = new HashMap();    // ResourceListPredicate -> CachingPredicate

        private ArrayList _displayNameProviders = new ArrayList();

        private IntObjectCache _loadedResourceTypes = new IntObjectCache( 256 );
        private ObjectCache _findUniqueResourceCache = new ObjectCache( 1024 );
        private SpinWaitLock _findUniqueResourceCacheLock = new SpinWaitLock();

        public static void SetProgressWindow( IProgressWindow progressWindow )
        {
            _progressWindow = progressWindow;
        }

        public static MyPalStorage Storage
        {
            [DebuggerStepThrough] get { return _theStorage; }
        }

        public static bool TraceOperations
        {
            get { return _trace; }
            set { _trace = value; }
        }

        public static int ResourceCacheSize
        {
            get { return _resourceCacheSize; }
            set { _resourceCacheSize = value; }
        }

        public static string DBPath
        {
            get { return _dbPath; }
            set { _dbPath = value; }
        }

        public bool RepairRequired
        {
            get { return _repairRequired; }
        }

        public bool FullRepairRequired
        {
            get { return _fullRepairRequired; }
        }

        internal void SetRepairRequired()
        {
            _repairRequired = true;
        }

        protected internal MyPalStorage()
        {
            if ( _theStorage != null && !_theStorage._shutDown )
            {
                throw new InvalidOperationException( "Attempt to create multiple ResourceStore instances" );
            }
            _theStorage = this;
            _transientResources.ValueDead += new IntWeakHashTable.ValueDeadDelegate( HandleTransientResourceDead );
            RebuildProgressListenerEvent +=new RebuildProgressEventHandler(_theStorage_RebuildProgressListenerEvent);
        }

        public ITextIndexManager TextIndexManager
        {
            get { return _textIndexManager; }
            set { _textIndexManager = value; }
        }

        public Thread OwnerThread
        {
            get { return _ownerThread; }
            set { _ownerThread = value; }
        }

        public IPropTypeCollection PropTypes
        {
            get { return _propTypes; }
        }

        public IResourceTypeCollection ResourceTypes
        {
            get { return _resourceTypes; }
        }

        internal ResourceStoreProps Props
        {
            get { return _props; }
        }

        public static void CreateDatabase()
        {
            if (!Directory.Exists( DBPath ) )
                Directory.CreateDirectory( DBPath );

            DBStructure dbStructure =
                new DBStructure( DBPath, "MyPal", DatabaseMode.Create );

            TableStructure table = dbStructure.CreateTable( "PropTypes" );
            table.CreateColumn( "Id", ColumnType.Integer, true );
            table.CreateColumn( "Name", ColumnType.String, true );
            table.CreateColumn( "Type", ColumnType.Integer, false );
            table.CreateColumn( "Flags", ColumnType.Integer, false );

            table = dbStructure.CreateTable( "ResourceTypes" );
            table.CreateColumn( "Id", ColumnType.Integer, true );
            table.CreateColumn( "Name", ColumnType.String, true );
            table.CreateColumn( "DisplayNameMask", ColumnType.String, false );

            table = dbStructure.CreateTable( "Resources" );
            table.CreateColumn( "Id", ColumnType.Integer, true );
            table.CreateColumn( "TypeID", ColumnType.Integer, true );

            table = dbStructure.CreateTable( "IntProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.Integer, false );
            table.SetCompoundIndexWithValue( "Id", "PropType", "PropValue" );
            table.SetCompoundIndexWithValue( "PropType", "PropValue", "Id" );

            table = dbStructure.CreateTable( "StringProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.String, false );
            table.SetCompoundIndex( "Id", "PropType" );
            table.SetCompoundIndex( "PropType", "PropValue" );

            table = dbStructure.CreateTable( "LongStringProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.String, false );
            table.SetCompoundIndex( "Id", "PropType" );

            table = dbStructure.CreateTable( "StringListProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.String, false );
            table.SetCompoundIndex( "Id", "PropType" );
            table.SetCompoundIndex( "PropType", "PropValue" );

            table = dbStructure.CreateTable( "DateProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.DateTime, false );
            table.SetCompoundIndexWithValue( "Id", "PropType", "PropValue" );
            table.SetCompoundIndexWithValue( "PropType", "PropValue", "Id" );

            table = dbStructure.CreateTable( "BlobProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.BLOB, false );
            table.SetCompoundIndex( "Id", "PropType" );

            table = dbStructure.CreateTable( "DoubleProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.Double, false );
            table.SetCompoundIndex( "Id", "PropType" );

            table = dbStructure.CreateTable( "BoolProps" );
            table.CreateColumn( "Id", ColumnType.Integer, true );
            table.CreateColumn( "PropType", ColumnType.Integer, true );

            table = dbStructure.CreateTable( "Links" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "Id2", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, true );
            table.SetCompoundIndexWithValue( "Id", "Id2", "PropType" );
            table.SetCompoundIndexWithValue( "Id2", "Id", "PropType" );

            dbStructure.SaveStructure();
            dbStructure.Shutdown();
        }

        public static void OpenDatabase( )
        {
            if ( _theStorage == null || _theStorage._shutDown )
            {
                new MyPalStorage();
            }
            _theStorage.DoOpenDatabase();
        }

        public static bool DatabaseExists()
        {
            if ( !DBHelper.DatabaseExists( DBPath, "MyPal" ) )
            {
                Trace.WriteLine( "ResourceStore initialization: database does not exist" );
                return false;
            }

            return true;
        }

        public static string DBCreatorBuild
        {
            get
            {
                if ( _theStorage == null || _theStorage._shutDown )
                {
                    new MyPalStorage();
                }
                if ( _theStorage._dbStructure == null )
                {
                    _theStorage._dbStructure = new DBStructure( DBPath, "MyPal" );
                }
                _theStorage._dbStructure.LoadVersionInfo();
                return _theStorage._dbStructure.Build;
            }
        }

        protected void DoOpenDatabase()
        {
            if ( _dbStructure == null )
            {
                _dbStructure = new DBStructure( DBPath, "MyPal" );
            }
            _dbStructure.ProgressListenerEvent += new DBStructure.ProgressEventHandler( dbStructure_ProgressListenerEvent );
            _dbStructure.LoadStructure( true );

            CheckCreateLongStringProps();
            CheckCreateBoolProps();
            CheckCreateStringListProps();
            CheckCreateIndexesWithValue();

            if ( !_dbStructure.IsDatabaseCorrect() )
            {
                _indexesRebuilt = true;
                _dbStructure.RebuildIndexes();
            }

            _dbStructure.LoadStructure();
            _database = _dbStructure.Database;

            _resourceCache = new IntObjectCache( _resourceCacheSize );
            _resourceCache.ObjectRemoved += new IntObjectCacheEventHandler( _resourceCache_ObjectRemoved );
            _resourceWeakCache = new IntWeakHashTable( 1024 );

            _propTypeTable     = CheckGetTable( "PropTypes" );
            _resourceTypeTable = CheckGetTable( "ResourceTypes" );
            _resources       = CheckGetTable( "Resources" );
            _intProps        = CheckGetTable( "IntProps" );
            _stringProps     = CheckGetTable( "StringProps" );
            _longStringProps = CheckGetTable( "LongStringProps" );
            _dateProps       = CheckGetTable( "DateProps" );
            _blobProps       = CheckGetTable( "BlobProps" );
            _doubleProps     = CheckGetTable( "DoubleProps" );
            _boolProps       = CheckGetTable( "BoolProps" );
            _links           = CheckGetTable( "Links" );
            _stringListProps = CheckGetTable( "StringListProps" );
            _dbStructure.SetSortedColumns();
            _propTypes = new PropTypeCollection( this, _propTypeTable );
            _resourceTypes = new ResourceTypeCollection( this, _resourceTypeTable );

            _contentTables.AddRange( new ITable[]
                { _resources,
                  _intProps, _stringProps, _longStringProps, _dateProps, _blobProps, _doubleProps,
                  _boolProps, _links, _stringListProps } );
            _propTables.AddRange( new ITable[]
                { _intProps, _stringProps, _dateProps, _blobProps, _doubleProps, _longStringProps,
                  _stringListProps } );

            foreach( ITable table in _theStorage._contentTables )
            {
                table.AutoFlush = false;
            }

            _resourceTypes.CacheResourceTypes();
            _propTypes.CachePropTypes();
            _props = new ResourceStoreProps( _resourceTypes, _propTypes );
            _props.Initialize();
            _propTypes.CachePropDisplayNames();
            _resourceTypes.CacheResourceTypeFlags();

            _emptyResourceList = ListFromIds( new IntArrayList(), false );

            _loadedResourceTypes.RemoveAll();
            foreach( ResourceTypeItem resType in ResourceTypes )
            {
                CacheResourceTypePredicate( resType );
            }

            _initializationComplete = true;
        }

        public static void QueueDefragmentTableIndexes()
        {
            foreach( ITable table in _theStorage._contentTables )
            {
                DefragmentTableIndexesInIdleMode( table );
            }
        }

        public static void QueueIdleDatabaseBackup()
        {
            DateTime lastBackupDate = Core.SettingStore.ReadDate( "ResourceStore", "LastBackupDate", DateTime.MinValue );
            Core.ResourceAP.QueueJobAt(
                lastBackupDate.AddDays( 1 ), new BackupDatabaseDelegate( BackupDatabase ), true );
        }

        public void FlushTables()
        {
            foreach( ITable table in _contentTables )
            {
                Monitor.Enter( table );
                try
                {
                    table.FlushData();
                }
                finally
                {
                    Monitor.Exit( table );
                }
            }
        }

        private delegate void DefragmentTableIndexesInIdleModeDelegate( ITable table );

        private static void DefragmentTableIndexesInIdleMode( ITable table )
        {
            string message = "Defragmenting indexes of " + table.Name;
            DateTime when = Core.SettingStore.ReadDate( "ResourceStore", table.Name + "LastDefragTime", DateTime.MinValue ).AddDays( 1 );
            if( when > DateTime.Now )
            {
                Core.ResourceAP.QueueJobAt(
                    when, message, new DefragmentTableIndexesInIdleModeDelegate( DefragmentTableIndexesInIdleMode ), table );
            }
            else
            {
                if( Core.IsSystemIdle && Core.State == CoreState.Running )
                {
                    IStatusWriter writer = Core.UIManager.GetStatusWriter( table, StatusPane.UI );
                    try
                    {
                        writer.ShowStatus( message + "..." );
                        table.DefragmentIndexes( true );
                    }
                    finally
                    {
                        writer.ClearStatus();
                    }
                    if( Core.IsSystemIdle )
                    {
                        Core.SettingStore.WriteDate( "ResourceStore", table.Name + "LastDefragTime", DateTime.Now );
                        Core.ResourceAP.QueueJobAt( DateTime.Now.AddDays( 1 ),
                            message, new DefragmentTableIndexesInIdleModeDelegate( DefragmentTableIndexesInIdleMode ), table );
                    }
                }
                else
                {
                    int idlePeriod = Core.SettingStore.ReadInt( "Startup", "IdlePeriod", 5 );
                    Core.ResourceAP.QueueJobAt( DateTime.Now.AddMinutes( idlePeriod ),
                        message, new DefragmentTableIndexesInIdleModeDelegate( DefragmentTableIndexesInIdleMode ), table );
                }
            }
        }

        internal void CacheResourceTypePredicate( ResourceTypeItem resType )
        {
            if ( _loadedResourceTypes.Count >= _loadedResourceTypes.Size-10 )
            {
                _loadedResourceTypes = new IntObjectCache( _loadedResourceTypes.Size*2 );
                foreach( ResourceTypeItem aResType in ResourceTypes )
                {
                    CacheResourceTypePredicate( aResType );
                }
            }
            else
            {
                resType.ResourcesOfType = (ResourceList) GetAllResourcesLive( resType.Name );
                resType.ResourcesOfType.Sort( new SortSettings( ResourceProps.Id, true ) );
                resType.ResourcesOfType.Instantiate( false );
                CachePredicate( resType.ResourcesOfType );
                _loadedResourceTypes.CacheObject( resType.Id, resType );
            }
        }

        private ITable CheckGetTable( string tableName )
        {
            ITable table = _database.GetTable( tableName );
            // if the index was broken and not rebuilt correctly, Count (returned by the index)
            // will be zero, but IsEmpty will return false
            try
            {
                if ( !table.IsEmpty() && table.Count == 0 )
                {
                    table.RebuildIndexes();
                }
            }
            catch( Exception e )
            {
                throw new IndexRebuildException( "Rebuilding indexes on " + tableName + " failed", e );
            }
            return table;
        }

        /**
         * Creates the LongStringProps table if it does not exist in the database.
         * @return true if the table was created
         */

        private bool CheckCreateLongStringProps()
        {
            try
            {
                _dbStructure.GetTable( "LongStringProps" );
            }
            catch( TableDoesNotExistException )
            {
                TableStructure table = _dbStructure.CreateTable( "LongStringProps" );
                table.CreateColumn( "Id", ColumnType.Integer, false );
                table.CreateColumn( "PropType", ColumnType.Integer, false );
                table.CreateColumn( "PropValue", ColumnType.String, false );
                table.SetCompoundIndex( "Id", "PropType" );
                _dbStructure.SaveStructure();
                return true;
            }
            return false;
        }

        /**
         * Creates the BoolProps table if it does not exist in the database.
         * @return true if the table was created
         */

        private bool CheckCreateBoolProps()
        {
            try
            {
                _dbStructure.GetTable( "BoolProps" );
            }
            catch( TableDoesNotExistException )
            {
                TableStructure table = _dbStructure.CreateTable( "BoolProps" );
                table.CreateColumn( "Id", ColumnType.Integer, true );
                table.CreateColumn( "PropType", ColumnType.Integer, true );
                _dbStructure.SaveStructure();
                return true;
            }
            return false;
        }

        /**
         * Creates the StringListProps table if it does not exist in the database.
         */

        private bool CheckCreateStringListProps()
        {
            try
            {
                _dbStructure.GetTable( "StringListProps" );
            }
            catch( TableDoesNotExistException )
            {
                TableStructure table = _dbStructure.CreateTable( "StringListProps" );
                table.CreateColumn( "Id", ColumnType.Integer, false );
                table.CreateColumn( "PropType", ColumnType.Integer, false );
                table.CreateColumn( "PropValue", ColumnType.String, false );
                table.SetCompoundIndex( "Id", "PropType" );
                table.SetCompoundIndex( "PropType", "PropValue" );
                _dbStructure.SaveStructure();
                return true;
            }
            return false;
        }

        /**
         * If the property tables have regular indexes, drop them and create indexes
         * with value instead.
         */

        private bool CheckCreateIndexesWithValue()
        {
            bool changed = false;
            TableStructure intProps = _dbStructure.GetTable( "IntProps" );
            if( intProps.HasCompoundIndex( "Id", "PropType" ) )
            {
                changed = true;
                intProps.DropCompoundIndex( "Id", "PropType" );
                intProps.SetCompoundIndexWithValue( "Id", "PropType", "PropValue" );
                intProps.DropCompoundIndex( "PropType", "PropValue" );
                intProps.SetCompoundIndexWithValue( "PropType", "PropValue", "Id" );
            }

            TableStructure dateProps = _dbStructure.GetTable( "DateProps" );
            if( dateProps.HasCompoundIndex( "Id", "PropType" ) )
            {
                changed = true;
                dateProps.DropCompoundIndex( "Id", "PropType" );
                dateProps.SetCompoundIndexWithValue( "Id", "PropType", "PropValue" );
                dateProps.DropCompoundIndex( "PropType", "PropValue" );
                dateProps.SetCompoundIndexWithValue( "PropType", "PropValue", "Id" );
            }

            TableStructure links = _dbStructure.GetTable( "Links" );
            if ( links.HasCompoundIndex( "Id", "Id2" ) )
            {
                changed = true;
                links.DropCompoundIndex( "Id", "Id2" );
                links.SetCompoundIndexWithValue( "Id", "PropType", "Id2" );
                links.DropCompoundIndex( "Id2", "Id" );
                links.SetCompoundIndexWithValue( "Id2", "PropType", "Id" );
            }
            else if ( links.HasCompoundIndexWithValue( "Id", "Id2" ) )
            {
                changed = true;
                links.DropCompoundIndexWithValue( "Id", "Id2" );
                links.SetCompoundIndexWithValue( "Id", "PropType", "Id2" );
                links.DropCompoundIndexWithValue( "Id2", "Id" );
                links.SetCompoundIndexWithValue( "Id2", "PropType", "Id" );
            }
            if ( changed )
            {
                _dbStructure.SaveStructure();
            }
            return changed;
        }

        public static void CloseDatabase()
        {
            if ( _theStorage != null && !_theStorage._shutDown )
            {
                try
                {
                    if ( _theStorage._dbStructure != null && !_theStorage._ioErrorReported )
                    {
                        //_theStorage.ReportHandlerLeaks();
                        Trace.WriteLine( "Closing database..." );
                        _theStorage._dbStructure.Shutdown();
                        if ( _theStorage._resourceCache != null )
                        {
                            Trace.WriteLine( "Resource cache hit rate: " + (int) ( _theStorage._resourceCache.HitRate() * 100 ) + "%" );
                        }
                    }
                    else
                    {
                        Trace.WriteLine( "MyPalStorage.CloseDatabase(): dbStructure is null" );
                    }
                }
                finally
                {
                    _theStorage._shutDown = true;
                }
            }
        }

        public void DefragmentDatabase( IProgressWindow progressWindow )
        {
            // for better performance, optimize no more than one table at a time
            ArrayList tables = new ArrayList();
            tables.Add( _resources );
            tables.AddRange( _propTables );
            tables.Add( _links );
            tables.Add( _propTypeTable );
            tables.Add( _resourceTypeTable );

            foreach( ITable tbl in tables )
            {
                if ( CheckDefragmentTable( tbl, progressWindow ) )
                    return;
            }
        }

        private bool CheckDefragmentTable( ITable table, IProgressWindow progressWindow )
        {
            RecordsCounts counts = table.ComputeWastedSpace();
            long deletedRecordsCount = counts.TotalRecordCount - counts.NormalRecordCount;
            if ( deletedRecordsCount > counts.NormalRecordCount / 2 )
            {
                progressWindow.UpdateProgress( 0, "Defragmenting " + table.Name + "...", null );
                table.Defragment();
                return true;
            }
            return false;
        }

        public static void FlushDatabase()
        {
            if ( _theStorage != null && _theStorage._database != null )
            {
                _theStorage._database.Flush();
            }
        }

        private static readonly string[] _dbFiles = new string[]
        {
            "MyPal.BlobFileSystem.dbUtil",
            "MyPal.BlobProps.table.dbUtil",
            "MyPal.BoolProps.table.dbUtil",
            "MyPal.database.struct.dbUtil",
            "MyPal.DateProps.table.dbUtil",
            "MyPal.DoubleProps.table.dbUtil",
            "MyPal.IntProps.table.dbUtil",
            "MyPal.Links.table.dbUtil",
            "MyPal.LongStringProps.table.dbUtil",
            "MyPal.PropTypes.table.dbUtil",
            "MyPal.Resources.table.dbUtil",
            "MyPal.ResourceTypes.table.dbUtil",
            "MyPal.StringListProps.table.dbUtil",
            "MyPal.StringProps.table.dbUtil",
            "MyPal_btree.BlobProps.Id#PropType.index.dbUtil",
            "MyPal_btree.BoolProps.Id.index.dbUtil",
            "MyPal_btree.BoolProps.PropType.index.dbUtil",
            "MyPal_btree.DateProps.Id#PropType.index.dbUtil",
            "MyPal_btree.DateProps.PropType#PropValue.index.dbUtil",
            "MyPal_btree.DoubleProps.Id#PropType.index.dbUtil",
            "MyPal_btree.IntProps.Id#PropType.index.dbUtil",
            "MyPal_btree.IntProps.PropType#PropValue.index.dbUtil",
            "MyPal_btree.Links.Id#PropType.index.dbUtil",
            "MyPal_btree.Links.Id2#PropType.index.dbUtil",
            "MyPal_btree.Links.PropType.index.dbUtil",
            "MyPal_btree.LongStringProps.Id#PropType.index.dbUtil",
            "MyPal_btree.PropTypes.Id.index.dbUtil",
            "MyPal_btree.PropTypes.Name.index.dbUtil",
            "MyPal_btree.Resources.Id.index.dbUtil",
            "MyPal_btree.Resources.TypeID.index.dbUtil",
            "MyPal_btree.ResourceTypes.Id.index.dbUtil",
            "MyPal_btree.ResourceTypes.Name.index.dbUtil",
            "MyPal_btree.StringListProps.Id#PropType.index.dbUtil",
            "MyPal_btree.StringListProps.PropType#PropValue.index.dbUtil",
            "MyPal_btree.StringProps.Id#PropType.index.dbUtil",
            "MyPal_btree.StringProps.PropType#PropValue.index.dbUtil"
        };

        public const string _dbBackupFile = "MyPal.Backup.zip";

        public static DateTime BackupTime()
        {
            return IOTools.GetFileLastWriteTime( IOTools.Combine( Core.SettingStore.ReadString( "ResourceStore", "BackupPath", string.Empty ), _dbBackupFile ) );
        }

        public static void RestoreFromBackup()
        {
            CloseDatabase();
            string backupPath = Core.SettingStore.ReadString( "ResourceStore", "BackupPath", string.Empty );
            RestoreFromBackup( IOTools.Combine( backupPath, _dbBackupFile ) );
        }

        private delegate void BackupDatabaseDelegate( bool idle );

        public static void BackupDatabase( bool idle )
        {
            if( !_theStorage.IsOwnerThread() )
            {
                Core.ResourceAP.QueueJob( new BackupDatabaseDelegate( BackupDatabase ), idle );
            }
            else
            {
                bool backupEnabled = Core.SettingStore.ReadBool( "ResourceStore", "EnableBackup", false );
                if( backupEnabled )
                {
                    string backupPath = Core.SettingStore.ReadString( "ResourceStore", "BackupPath", string.Empty );
                    if( backupPath.Length == 0 || ( idle && !Core.IsSystemIdle ) )
                    {
                        int idlePeriod = Core.SettingStore.ReadInt( "Startup", "IdlePeriod", 5 );
                        Core.ResourceAP.QueueJobAt( DateTime.Now.AddMinutes( idlePeriod ),
                                                    new BackupDatabaseDelegate( BackupDatabase ), idle );
                    }
                    else
                    {
                        FlushDatabase();
                        BackupDatabase( IOTools.Combine( backupPath, _dbBackupFile ) );
                        Core.SettingStore.WriteDate( "ResourceStore", "LastBackupDate", DateTime.Now );
                        QueueIdleDatabaseBackup();
                    }
                }
            }
        }

        public static void BackupDatabase( string backupFilename )
        {
            long startTicks = DateTime.Now.Ticks;
            long totalLen = 0;
            foreach( string dbFile in _dbFiles )
            {
                if( dbFile.IndexOf( "_btree" ) < 0 )
                {
                    FileInfo fi = IOTools.GetFileInfo( IOTools.Combine( DBPath, dbFile ) );
                    if( fi != null )
                    {
                        totalLen += fi.Length;
                    }
                }
            }
            byte[] buffer = new byte[ 0x10000 ];
            FileStream zipFile = File.Create( backupFilename );
            ZipOutputStream zipStream = new ZipOutputStream( zipFile );
            IStatusWriter statusWriter = ( ICore.Instance == null ) ? null : Core.UIManager.GetStatusWriter( _theStorage, StatusPane.UI );
            long count = 0;
            int lastPercent = -1;
            foreach( string dbFile in _dbFiles )
            {
                if( dbFile.IndexOf( "_btree" ) >= 0 )
                {
                    continue;
                }
                FileStream fs = new FileStream( IOTools.Combine( DBPath, dbFile ), FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
                ZipEntry entry = new ZipEntry( Path.GetFileName( dbFile ) );
                entry.DateTime = DateTime.Now;
                entry.Size = fs.Length;
                zipStream.PutNextEntry( entry );
                int readBytes;
                while( ( readBytes = fs.Read( buffer, 0, buffer.Length ) ) > 0 )
                {
                    if( statusWriter != null )
                    {
                        int percent = (int) ( ( count * 100 ) / totalLen );
                        if( percent > lastPercent )
                        {
                            lastPercent = percent;
                            statusWriter.ShowStatus( "Database backup: " + percent + "%" );
                        }
                    }
                    zipStream.Write( buffer, 0, readBytes );
                    count += readBytes;
                }
                fs.Close();
            }
            zipStream.Finish();
            zipStream.Close();
            long ticks = DateTime.Now.Ticks - startTicks;
            Debug.WriteLine( "Database backup took " + ticks / 10000 + " ms" );
            if( statusWriter != null )
            {
                statusWriter.ClearStatus();
            }
        }

        public static void RestoreFromBackup( string backupFilename )
        {
            IProgressWindow pw = ( ICore.Instance == null ) ? null : Core.ProgressWindow;
            if( pw != null )
            {
                pw.UpdateProgress( 0, "Restoring database from backup...", null );
            }
            foreach( string dbFile in _dbFiles )
            {
                File.Delete( dbFile );
            }
            FileStream zipFile = File.OpenRead( backupFilename );
            ZipInputStream zip = new ZipInputStream( zipFile );
            ZipEntry theEntry;
            long totalLen = 0;
            while ( ( theEntry = zip.GetNextEntry() ) != null )
            {
                totalLen += theEntry.Size;
            }
            zip.Close();

            zipFile = File.OpenRead( backupFilename );
            zip = new ZipInputStream( zipFile );
            byte[] buffer = new byte[ 0x10000 ];
            long count = 0;

            while ( ( theEntry = zip.GetNextEntry() ) != null )
            {
                FileStream streamWriter = File.Create( IOTools.Combine( DBPath, theEntry.Name ) );
                while( true )
                {
                    if( pw != null )
                    {
                        int percent = (int) ( count * 100 / totalLen );
                        if( percent > 100 )
                        {
                            percent = 100;
                        }
                        pw.UpdateProgress( percent, "Restoring database from backup...", null );
                    }
                    int size = zip.Read( buffer, 0, buffer.Length );
                    if( size == 0 ) break;
                    streamWriter.Write( buffer, 0, size );
                    count += size;
                }
                streamWriter.Close();
            }
            zip.Close();
        }

        public static Exception RunFullRepair( RepairProgressEventHandler progressHandler )
        {
            CloseDatabase();

            DBStructure dbStructure = new DBStructure( DBPath, "MyPal" );
            dbStructure.ProgressListenerEvent += new DBStructure.ProgressEventHandler( _theStorage_RebuildProgressListenerEvent );
            dbStructure.LoadStructure( true );
            dbStructure.RebuildIndexes( true );
            IDatabase db = dbStructure.OpenDatabase();
            ResourceStoreRepair repair = new ResourceStoreRepair( db );
            repair.RepairProgress += new RepairProgressEventHandler( progressHandler );
            repair.FixErrors = true;
            repair.Run();
            return repair.RepairException;
        }

        public string BuildNumber
        {
            get { return _dbStructure.Build; }
            set
            {
                if ( _dbStructure.Build != value )
                {
                    _dbStructure.Build = value;
                    _dbStructure.SaveStructure();
                }
            }
        }

        public bool IndexesRebuilt
        {
            get { return _indexesRebuilt; }
        }

        internal void AddUpdateListener( IUpdateListener listener, bool priority )
        {
            _updateManager.AddUpdateListener( listener, priority );
        }

        internal void RemoveUpdateListener( IUpdateListener listener, bool priority )
        {
            _updateManager.RemoveUpdateListener( listener, priority );
        }

        public int ResourceSavedHandlerCount
        {
            get { return _updateManager.ListenerCount; }
        }

        public int TransientResourceCount
        {
            get { return _transientResources.Count; }
        }

        public void TraceLiveResourceLists()
        {
            _updateManager.TraceListeners();
        }

        public void TraceDbPerformanceCounters()
        {
            long result = 0;
            result += _propTypeTable.TracePerformanceCounters();
            result += _resourceTypeTable.TracePerformanceCounters();
            result += _resources.TracePerformanceCounters();
            foreach( ITable table in _propTables )
            {
                result +=  table.TracePerformanceCounters();
            }
            result += _links.TracePerformanceCounters();
            Trace.WriteLine( "Total DB bytes read: " + Utils.SizeToString( result ) );
        }

        internal void CheckOwnerThread()
        {
            if ( !IsOwnerThread() )
            {
                throw new StorageException( "Write access to the resource store is only allowed from the resource thread" );
            }
        }

        public bool IsOwnerThread()
        {
            if ( _ownerThread == null )
                return true;

            return Thread.CurrentThread == _ownerThread;
        }

        internal void OnIndexCorruptionDetected( string reason )
        {
            if ( !_initializationComplete )
            {
                throw new BadIndexesException( "Index corruption detected when opening the database: " + reason );
            }
            Trace.WriteLine( "MyPalStorage index corruption detected: " + reason );
            if ( _indexCorruptionReported )
            {
                return;
            }
            _indexCorruptionReported = true;
            _fullRepairRequired = true;
            if ( IndexCorruptionDetected != null )
            {
                IndexCorruptionDetected( this, EventArgs.Empty );
            }
        }

        internal void OnIOErrorDetected( Exception exception )
        {
            Trace.WriteLine( "MyPalStorage I/O error detected: " + exception.ToString() );
            if ( _ioErrorReported )
            {
                return;
            }
            _ioErrorReported = true;
            if ( IOErrorDetected != null )
            {
                IOErrorDetected( this, new ThreadExceptionEventArgs( exception ) );
            }
        }

        // -- property type operations ---------------------------------------------

        public int GetPropId( string name )
        {
            if ( name == null )
                throw new ArgumentNullException( "name" );

            return _propTypes [name].Id;
        }

        internal PropDataType GetPropDataType( int propId )
        {
            if ( propId == ResourceProps.Id )
                return PropDataType.Int;

            return _propTypes [propId].DataType;
        }

        internal string GetPropName( int propID )
        {
            return _propTypes [propID].Name;
        }

        internal bool IsLinkDirected( int propID )
        {
            return _propTypes [propID].HasFlag( PropTypeFlags.DirectedLink );
        }

        // -- resource type operations ---------------------------------------

        /**
         * Returns the display name mask of the resource type with the specified ID.
         */

        internal DisplayNameMask GetResourceTypeDisplayNameMask( int ID )
        {
            return (_resourceTypes [ID] as ResourceTypeItem).DisplayNameTemplate;
        }

        // -- resource operations --------------------------------------------------

        public IResource NewResource( string type )
        {
            if ( type == null )
                throw new ArgumentNullException( "type" );

            ResourceTypeItem resourceType = (ResourceTypeItem) _resourceTypes [type];
            if ( _initializationComplete )
            {
                ResourceList resourcesOfType = resourceType.ResourcesOfType;
                lock( resourcesOfType )
                {
                    if ( resourcesOfType._list == null )
                    {
                        resourcesOfType.Instantiate( false );
                    }
                }
            }

            int typeID = resourceType.Id;
            Resource res;
            lock( this )
            {
                int resID = CreateResource( typeID );
                res = new Resource( resID, typeID, true );
                _resourceCache.CacheObject( res.Id, res );
            }
            OnResourceSaved( res, new MultiPropChangeSet( true ) );
            return res;
        }

        public IResource BeginNewResource( string type )
        {
            return BeginNewResource( type, false );
        }

        public IResource NewResourceTransient( string type )
        {
            return BeginNewResource( type, true );
        }

        private IResource BeginNewResource( string type, bool transient )
        {
            if ( type == null )
                throw new ArgumentNullException( "type" );

            ResourceTypeItem resourceType = (ResourceTypeItem) _resourceTypes [type];
            if ( _initializationComplete )
            {
                ResourceList resourcesOfType = resourceType.ResourcesOfType;
                lock( resourcesOfType )
                {
                    if ( resourcesOfType._list == null )
                    {
                        resourcesOfType.Instantiate( false );
                    }
                }
            }

            int typeID = resourceType.Id;
            Resource res;
            lock( this )
            {
                int resID;
                if ( transient )
                {
                    resID = _resources.NextID();
                    Debug.WriteLine( "Created transient resource with ID " + resID );
                }
                else
                {
                    resID = CreateResource( typeID );
                }

                res = new Resource( resID, typeID, true );
                if ( !transient )
                {
                    _resourceCache.CacheObject( res.Id, res );
                }
                else
                {
                    res.SetTransient();
                    _transientResources [res.Id] = res;
                }

                lock( _updatingResources )
                {
                    _updatingResources [res.Id] = new MultiPropChangeSet( true );
                }
            }
            return res;
        }

        public IResource LoadResource( int ID )
        {
            return LoadResource( ID, false, -1 );
        }

        internal IResource LoadResource( int id, bool allowDeleted, int knownTypeId )
        {
            Resource res;
            lock( this )
            {
                res = LoadResourceFromCache( id, allowDeleted );
                if ( res != null && knownTypeId >= 0 && res.TypeId != knownTypeId )
                {
                    OnIndexCorruptionDetected( "LoadResource: type of resource loaded from cache (" + res.Type +
                        ") does not match known type of resource list (" + ResourceTypes [knownTypeId].Name + ")" );
                }
                if ( res == null && knownTypeId >= 0 )
                {
                    res = new Resource( id, knownTypeId, false );
                    _resourceCache.CacheObject( id, res );
                    return res;
                }
                if ( res == null )
                {
                    knownTypeId = LoadResourceTypeFromCachedPredicates( id, true );
                    res = _resourceCache.TryKey( id ) as Resource;
                    if ( res == null )
                    {
                        res = new Resource( id, knownTypeId, false );
                        _resourceCache.CacheObject( id, res );
                    }
                }
            }
            return res;
        }

        public IResource TryLoadResource( int id )
        {
            lock( this )
            {
                Resource res = LoadResourceFromCache( id, true );
                if ( res == null )
                {
                    int resType = LoadResourceTypeFromCachedPredicates( id, false );
                    if ( resType != -1 )
                    {
                        res = new Resource( id, resType, false );
                        _resourceCache.CacheObject( id, res );
                    }
                }
                else if ( res.IsDeleted )
                {
                    return null;
                }
                return res;
            }
        }

        internal Resource LoadResourceFromCache( int ID, bool allowDeleted )
        {
            Resource res = _resourceCache.TryKey( ID ) as Resource;
            if ( res == null )
            {
                res = _resourceWeakCache [ID] as Resource;
                if ( res == null )
                {
                    res = _transientResources [ID] as Resource;
                }
                else if ( res.Id == -1 )
                {
                    if( !allowDeleted )
                    {
                        throw new ResourceDeletedException( ID, res.Type );
                    }
                }
            }
            else if ( res.Id == -1 && !allowDeleted )
            {
                throw new ResourceDeletedException( ID, res.Type );
            }
            return res;
        }

        private int LoadResourceTypeFromCachedPredicates( int id, bool throwException )
        {
            if ( !_initializationComplete )
            {
                return LoadResourceType( id, throwException );
            }

            foreach( ResourceTypeItem item in _loadedResourceTypes )
            {
                ResourceList resList = item.ResourcesOfType;
                lock( resList )
                {
                    if ( resList._list.BinarySearch( id ) >= 0 )
                    {
                        _loadedResourceTypes.TryKey( item.Id );
                        return item.Id;
                    }
                }
            }

            if ( throwException )
            {
                // this will throw the InvalidResourceIdException
                int resourceType = LoadResourceType( id, true );
                if ( resourceType >= 0 )
                {
                    // GetItemSafe() necessary here - see OM-11741
                    IResourceType resType = _resourceTypes.GetItemSafe( resourceType );
                    if ( resType != null )
                    {
                        throw new StorageException( "LoadResourceType mismatch: resource ID=" + id +
                            " of type " + resType.Name + " was not found in cached predicates but found on disk" );
                    }
                }
            }
            return -1;
        }

        private void _resourceCache_ObjectRemoved( object sender, IntObjectCacheEventArgs e )
        {
            Resource resource = e.Object as Resource;
            if ( resource == null )
            {
                if ( e.Object == null )
                {
                    return;
                }
                throw new Exception( "Object of invalid type found in resource cache: "+ e.Object.GetType().Name );
            }
            _resourceWeakCache [ e.Key ] = resource;
            resource.InvalidateDisplayName();
        }

        internal Resource GetResourceFromCache( int ID )
        {
            lock( this )
            {
                Resource res = _resourceCache.TryKey( ID ) as Resource;
                if ( res == null )
                    res = _resourceWeakCache [ID] as Resource;
                return res;
            }
        }

        public int GetResourceWeakCacheCount()
        {
            lock( this )
            {
                _resourceWeakCache.Compact();
                return _resourceWeakCache.Count;
            }
        }

        public int GetUpdatingResourceCount()
        {
            lock( _updatingResources )
            {
                return _updatingResources.Count;
            }
        }

        public IEnumerable ResourceCache
        {
            get { return _resourceCache; }
        }

        public IEnumerable ResourceWeakCache
        {
            get { return _resourceWeakCache; }
        }

        internal int CreateResource( int typeID )
        {
            CheckOwnerThread();
            lock( _resources )
            {
                IRecord resource = _resources.NewRecord();
                resource.SetValue( 1, IntInternalizer.Intern( typeID ) );
                SafeCommitRecord( resource, "MyPalStorage.CreateResource" );

                return resource.GetID();
            }
        }

        internal void ChangeResourceType( int ID, int typeID )
        {
            CheckOwnerThread();
            lock( _resources )
            {
                using( ICountedResultSet rs = _resources.CreateModifiableResultSet( 0, ID ) )
                {
                    using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "MyPalStorage.ChangeResourceType" ) )
                    {
                        if( !enumerator.MoveNext() )
                        {
                            throw new InvalidResourceIdException ( ID, "Attempt to change type for an invalid resource " + ID );
                        }
                        IRecord rec = enumerator.Current;
                        if( enumerator.MoveNext() )
                        {
                            OnIndexCorruptionDetected( "Duplicate resource ID in MyPalStorage.ChangeResourceType" );
                        }
                        rec.SetValue( 1, IntInternalizer.Intern( typeID ) );
                        SafeCommitRecord( rec, "MyPalStorage.ChangeResourceType" );
                    }
                }
            }
        }

        internal void CommitTransientResource( Resource res )
        {
            CheckOwnerThread();
            lock( this )
            {
                _transientResources.Remove( res.Id );
                _resourceCache.CacheObject( res.Id, res );
            }

            lock( _resources )
            {
                IRecord resource = _resources.NewRecord();
                resource.SetValue( 0, IntInternalizer.Intern( res.Id ) );
                resource.SetValue( 1, IntInternalizer.Intern( res.TypeId ) );
                SafeCommitRecord( resource, "MyPalStorage.CommitTransienResource" );
            }
        }

        internal void CleanTransientResource( Resource resource )
        {
            lock( this )
            {
                _transientResources.Remove( resource.Id );
                lock( _updatingResources )
                {
                    _updatingResources.Remove( resource.Id );
                }
            }
        }

        public void CompactTransientResources()
        {
            lock( this )
            {
                _transientResources.Compact();
            }
        }

        private void HandleTransientResourceDead( int id )
        {
            lock( _updatingResources )
            {
                _updatingResources.Remove( id );
            }
        }

        internal void CheckEndUpdate( IResource res )
        {
            while( true )
            {
                bool isUpdating;
                lock( _updatingResources )
                {
                    isUpdating = _updatingResources.Contains( res.Id );
                }
                if ( !isUpdating )
                    break;

                EndUpdateResource( res );
            }
        }

        internal void OnResourceDeleting( IResource res )
        {
            if ( _trace )
            {
                Trace.WriteLine( "OnResourceDeleting: " + res.Id );
            }
            _updateManager.NotifyResourceDeleting( res );
            if ( ResourceDeleting != null )
            {
                ResourceDeleting( res, EventArgs.Empty );
            }
        }

        internal void DeleteResource( IResource res )
        {
            CheckOwnerThread();

            if ( res.Id >= 0 )
            {
                ResourceRestrictions.CheckResourceDelete( res );

                DeleteAllProperties( res.Id );
                DeleteAllLinks( res.Id );
                if ( _textIndexManager != null )
                {
                    if ( !_resourceTypes [res.Type].HasFlag( ResourceTypeFlags.NoIndex ) )
                    {
                        _textIndexManager.DeleteDocumentQueued( res.Id );
                    }
                }
            }

            using( ICountedResultSet rs = _resources.CreateModifiableResultSet( 0, res.Id ) )
            {
                using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "MyPalStorage.DeleteResource" ) )
                {
                    if( !enumerator.MoveNext() )
                    {
                        throw new InvalidResourceIdException ( res.Id, "Attempt to delete an invalid resource " + res.Id );
                    }
                    IRecord rec = enumerator.Current;
                    if( enumerator.MoveNext() )
                    {
                        OnIndexCorruptionDetected( "Duplicate resource ID in MyPalStorage.DeleteResource" );
                    }
                    SafeDeleteRecord( rec, "MyPalStorage.DeleteResource" );
                }
            }
        }

        internal int LoadResourceType( int resId, bool throwException )
        {
            using( IResultSet rs = _resources.CreateResultSet( 0, resId ) )
            {
                using( SafeRecordValueEnumerator enumerator = new SafeRecordValueEnumerator( rs, "MyPalStorage.LoadResourceType" ) )
                {
                    if ( !enumerator.MoveNext() )
                    {
                        if ( throwException )
                            throw new InvalidResourceIdException( resId );
                        return -1;
                    }

                    int result = enumerator.GetCurrentIntValue( 1 );

                    if ( enumerator.MoveNext() )
                    {
                        OnIndexCorruptionDetected( "Multiple entries in Resources table found for resource " + resId );
                    }

                    return result;
                }
            }
        }

        /**
         * If a resource is found in the cache, returns the cached resource type;
         * otherwise, loads the type from disk.
         */

        public int GetResourceType( int resID )
        {
            Resource res = _resourceCache.TryKey( resID ) as Resource;
            if ( res != null )
                return res.TypeId;

            return LoadResourceType( resID, true );
        }

        /// <summary>
        /// Commits the specified record and reports possible IOException and BadIndexesException to the
        /// ResourceStore client.
        /// </summary>
        /// <param name="rec">The record to delete.</param>
        internal void SafeCommitRecord( IRecord rec, string reason )
        {
            try
            {
                rec.Commit();
            }
            catch( IOException ex )
            {
                OnIOErrorDetected( ex );
            }
            catch( BadIndexesException )
            {
                OnIndexCorruptionDetected( reason );
            }
        }

        /// <summary>
        /// Deletes the specified record and reports possible IOException and BadIndexesException to the
        /// ResourceStore client.
        /// </summary>
        /// <param name="rec">The record to delete.</param>
        internal void SafeDeleteRecord( IRecord rec, string reason )
        {
            try
            {
                rec.Delete();
            }
            catch( IOException ex )
            {
                OnIOErrorDetected( ex );
            }
            catch( BadIndexesException )
            {
                OnIndexCorruptionDetected( reason );
            }
        }

        /**
         * Creates a boolean property (true value assumed) for the specified resource
         * and property type.
         */

        internal void CreateBoolProperty( Resource res, int propID )
        {
            CheckOwnerThread();

            lock( _boolProps )
            {
                IRecord prop = _boolProps.NewRecord();

                prop.SetValue( 0, IntInternalizer.Intern( res.Id ) );
                prop.SetValue( 1, IntInternalizer.Intern( propID ) );
                SafeCommitRecord( prop, "MyPalStorage.CreateBoolProperty" );
            }
        }

        /**
         * Deletes (or sets to false, which is the same) a boolean property for
         * the specified resource and property type.
         */

        internal bool DeleteBoolProperty( Resource res, int propID )
        {
            CheckOwnerThread();

            using( IResultSet rs = GetBoolProperties( res.Id ) )
            {
                using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "MyPalStorage.DeleteBoolProperty" ) )
                {
                    while( enumerator.MoveNext() )
                    {
                        IRecord rec = enumerator.Current;
                        if ( rec.GetIntValue( 1 ) == propID )
                        {
                            SafeDeleteRecord( rec, "MyPalStorage.DeleteBoolProperty" );
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal void CreateProperty( Resource res, int propID, object propValue )
        {
            CheckOwnerThread();
            if ( _ioErrorReported )
            {
                return;
            }

            ITable propTable = GetPropTable( propID );
            lock( propTable )
            {
                IRecord property = propTable.NewRecord();

                property.SetValue( 0, IntInternalizer.Intern( res.Id ) );
                property.SetValue( 1, IntInternalizer.Intern( propID ) );
                if ( propValue is Stream )
                {
                    try
                    {
                        propValue = propTable.CreateBLOB( (Stream)propValue );
                    }
                    catch( IOException ex )
                    {
                        OnIOErrorDetected( ex );
                        return;
                    }
                    catch( UnauthorizedAccessException ex )
                    {
                        OnIOErrorDetected( ex );
                        return;
                    }
                }
                property.SetValue( 2, propValue );
                SafeCommitRecord( property, "MyPalStorage.CreateProperty" );
            }

            if ( res.Type == "ResourceType" && _initializationComplete )
            {
                _resourceTypes.UpdateResourceTypeFromResource( res );
            }
        }

        internal void UpdateProperty( Resource res, int propId, object propValue )
        {
            CheckOwnerThread();

            ITable propTable = GetPropTable( propId );
            using( ICountedResultSet rs = propTable.CreateResultSet( 0, res.Id, 1, propId, false ) )
            {
                int count = 0;
                SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "MyPalStorage.UpdateProperty" );
                using( enumerator )
                {
                    while( enumerator.MoveNext() )
                    {
                        IRecord rec = enumerator.Current;
                        if( count == 0 )
                        {
                            if ( propValue is Stream )
                            {
                                try
                                {
                                    rec.GetBLOBValue( 2 ).Set( (Stream)propValue );
                                }
                                catch( IOException ex )
                                {
                                    OnIOErrorDetected( ex );
                                    return;
                                }
                                catch( UnauthorizedAccessException ex )
                                {
                                    OnIOErrorDetected( ex );
                                    return;
                                }
                            }
                            else
                            {
                                rec.SetValue( 2, propValue );
                                SafeCommitRecord( rec, "MyPalStorage.UpdateProperty" );
                            }
                        }
                        else
                        {
                            SafeDeleteRecord( rec, "MyPalStorage.UpdateProperty" );
                        }
                        ++count;
                    }
                }

                if( count == 0 && !enumerator.IOError )
                {
                    OnIndexCorruptionDetected( "Attempt to update a non-existing property " + propId +
                        " on resource " + res.Id );
                }
                if( count > 1 )
                {
                    OnIndexCorruptionDetected( count + " properties with the same resource ID " +
                        res.Id + " and property type  " + GetPropName( propId ) + " found" );
                }
            }

            if ( _initializationComplete )
            {
                if ( res.Type == "ResourceType" )
                {
                    _resourceTypes.UpdateResourceTypeFromResource( res );
                }
                else if ( res.Type == "PropType" )
                {
                    _propTypes.UpdatePropType( res );
                }
            }
        }

        /**
         * Checks if the type of propValue matches the type of the property,
         * and returns the table where the property is stored.
         */

        internal ITable GetPropTable( int propID )
        {
            PropDataType propType = GetPropDataType( propID );
            return GetPropTable( propType );
        }

        internal ITable GetPropTable( PropDataType propType )
        {
            switch( propType )
            {
                case PropDataType.String:
                    return _stringProps;

                case PropDataType.LongString:
                    return _longStringProps;

                case PropDataType.Int:
                    return _intProps;

                case PropDataType.Date:
                    return _dateProps;

                case PropDataType.Double:
                    return _doubleProps;

                case PropDataType.Blob:
                    return _blobProps;

                case PropDataType.StringList:
                    return _stringListProps;

                default:
                    throw new StorageException( "Invalid property data type " + propType );
            }
        }

        public IRecord LoadPropertyRecord( PropDataType propType, int offset )
        {
            ITable table = GetPropTable( propType );
            Monitor.Enter( table );
            try
            {
                return table.GetRecord( offset );
            }
            finally
            {
                Monitor.Exit( table );
            }
        }

        /**
         * Checks that the type of the value passed to SetProp matches the
         * type of the property.
         */

        internal void CheckValueType( int propID, PropDataType propType, object propValue )
        {
            bool mismatch = false;
            Type expectedType = null;

            switch( propType )
            {
                case PropDataType.Int:
                    if ( !(propValue is Int32) )
                    {
                        mismatch = true;
                        expectedType = typeof(Int32);
                    }
                    break;

                case PropDataType.String:
                case PropDataType.LongString:
                case PropDataType.StringList:
                    if ( !(propValue is String) )
                    {
                        mismatch = true;
                        expectedType = typeof(String);
                    }
                    break;

                case PropDataType.Date:
                    if ( !(propValue is DateTime) )
                    {
                        mismatch = true;
                        expectedType = typeof(DateTime);
                    }
                    break;

                case PropDataType.Double:
                    if ( !(propValue is Double ) )
                    {
                        mismatch = true;
                        expectedType = typeof(Double);
                    }
                    break;

                case PropDataType.Blob:
                    if ( !(propValue is Stream ) && !(propValue is String) )
                    {
                        mismatch = true;
                        expectedType = typeof(Stream);
                    }
                    break;

                case PropDataType.Link:
                    if ( !(propValue is IResource ) )
                    {
                        mismatch = true;
                        expectedType = typeof(IResource);
                    }
                    break;

                case PropDataType.Bool:
                    if ( !(propValue is Boolean ) )
                    {
                        mismatch = true;
                        expectedType = typeof(Boolean);
                    }
                    break;

                default:
                    throw new StorageException( "Invalid property type " + propType );
            }

            if ( mismatch )
            {
                throw new StorageException( "Value type mismatch for property " + GetPropName( propID ) +
                    ": expected " + expectedType.ToString() + ", actual " + propValue.GetType().ToString() );
            }
        }

        internal void DeleteProperty( Resource res, int propID )
        {
            CheckOwnerThread();
            ITable propTable = GetPropTable( propID );
            using( ICountedResultSet rs = propTable.CreateResultSet( 0, res.Id, 1, propID, false ) )
            {
                DeleteAllRecords( rs, "MyPalStorage.DeleteProperty" );
            }
        }

        private void DeleteAllRecords( IResultSet rs, string operation )
        {
            using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, operation ) )
            {
                while( enumerator.MoveNext() )
                {
                    SafeDeleteRecord( enumerator.Current, operation );
                }
            }
        }

        internal void DeleteProperty( Resource res, int propID, int index )
        {
            CheckOwnerThread();
            ITable propTable = GetPropTable( propID );
            using( IResultSet rs = propTable.CreateResultSet( 0, res.Id, 1, propID, true ) )
            {
                using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "MyPalStorage.DeleteProperty" ) )
                {
                    for( int i=0; i<=index; i++ )
                    {
                        if ( !enumerator.MoveNext() )
                        {
                            return;
                        }
                    }
                    SafeDeleteRecord( enumerator.Current, "MyPalStorage.DeleteProperty" );
                }
            }
        }

        internal void DeleteAllProperties( int resID )
        {
            foreach( ITable table in _propTables )
            {
                DeleteAllPropertiesFromTable( resID, table );
            }
            DeleteAllPropertiesFromTable( resID, _boolProps );
        }

        private void DeleteAllPropertiesFromTable( int resID, ITable propTable )
        {
            IResultSet rs = propTable.CreateModifiableResultSet( 0, resID );
            using( rs )
            {
                DeleteAllRecords( rs, "MyPalStorage.DeleteAllPropertiesFromTable" );
            }
        }

        internal void OnResourceSaved( IResource resource, IPropertyChangeSet changeSet )
        {
            Trace.WriteLineIf( _trace, "OnResourceSaved: " + resource.Id );

            ResourceRestrictions.CheckResource( resource, changeSet );
            _updateManager.NotifyResourceSaved( resource, changeSet );

            if ( ResourceSaved != null )
            {
                ResourcePropEventArgs args = new ResourcePropEventArgs( resource, changeSet );
                ResourceSaved( this, args );
            }
        }

        internal void OnResourceSaved( Resource resource, int propID, object oldValue )
        {
            DisplayNameMask mask = GetResourceTypeDisplayNameMask( resource.TypeId );
            bool maskAffected = mask.DependsOnProperty( propID );
            if ( propID == Props.DisplayName )
            {
                maskAffected = true;
            }
            if ( maskAffected )
            {
                resource.InvalidateDisplayName();
            }

            bool isNewResource = false;
            MultiPropChangeSet cs;
            lock( _updatingResources )
            {
                cs = (MultiPropChangeSet) _updatingResources [resource.Id];
            }
            if ( cs != null )
            {
                cs.AddChangedProp( propID, oldValue );
                if ( maskAffected )
                    cs.SetDisplayNameAffected();
            }
            else
            {
                Trace.WriteLineIf( _trace, "OnResourceSaved: " + resource.Id );

                SinglePropChangeSet changeSet = new SinglePropChangeSet( propID, oldValue, isNewResource, maskAffected );

                OnResourceSaved( resource, changeSet );
            }
        }

        internal void BeginUpdateResource( IResource resource )
        {
            lock( _updatingResources )
            {
                MultiPropChangeSet changeSet = (MultiPropChangeSet) _updatingResources [resource.Id];
                if ( changeSet == null )
                    _updatingResources [resource.Id] = new MultiPropChangeSet( false );
                else
                    changeSet.BeginUpdate();
            }
        }

        internal int GetResourceUpdateCount( IResource resource )
        {
            lock( _updatingResources )
            {
                MultiPropChangeSet changeSet = (MultiPropChangeSet) _updatingResources [resource.Id];
                if ( changeSet == null )
                    return 0;

                return changeSet.GetUpdateCounter();
            }
        }

        internal int EndUpdateResource( IResource resource )
        {
            MultiPropChangeSet changeSet;
            int newCount;
            lock( _updatingResources )
            {
                changeSet = _updatingResources [resource.Id] as MultiPropChangeSet;
                if ( changeSet == null )
                    throw new StorageException( "IResource.EndUpdate() called without BeginUpdate()" );

                newCount = changeSet.EndUpdate();
                if ( newCount == 0 )
                {
                    _updatingResources.Remove( resource.Id );
                }
            }

            if ( newCount == 0 && ( changeSet.IsNewResource || !changeSet.IsEmpty() ) )
            {
                OnResourceSaved( resource, changeSet );
            }
            return newCount;
        }

        internal bool IsResourceChanged( Resource resource )
        {
            lock( _updatingResources )
            {
                MultiPropChangeSet changeSet = _updatingResources [resource.Id] as MultiPropChangeSet;
                if ( changeSet == null )
                    throw new StorageException( "IResource.IsChanged() can only be called between BeginUpdate() and EndUpdate()" );

                return !changeSet.IsEmpty();
            }
        }

        /**
         * Fires the ResourceSaved event that is caused by a link change.
         */

        private void OnLinkResourceSaved( Resource from, Resource target, int propID, LinkChangeType changeType )
        {
            DisplayNameMask mask = GetResourceTypeDisplayNameMask( from.TypeId );
            bool maskAffected = mask.DependsOnProperty( ( propID < 0 ) ? -propID : propID );
            if ( maskAffected )
            {
                from.InvalidateDisplayName();
            }

            MultiPropChangeSet cs;
            lock( _updatingResources )
            {
                cs = _updatingResources [from.Id] as MultiPropChangeSet;
            }
            if ( cs != null )
            {
                cs.AddChangedLink( propID, target.Id, changeType );
                if ( maskAffected )
                    cs.SetDisplayNameAffected();
            }
            else if ( !from.IsDeleting )
            {
                OnResourceSaved( from, new SinglePropChangeSet( propID, target.Id, changeType, maskAffected ) );
            }
        }

        internal void OnLinkAdded( Resource from, Resource target, int propID )
        {
            OnLinkResourceSaved( from, target, propID, LinkChangeType.Add );
            if ( LinkAdded != null )
            {
                LinkAdded( this, new LinkEventArgs( from, target, propID ) );
            }
        }

        internal void OnLinkDeleted( Resource from, Resource target, int propID )
        {
            if ( !from.IsDeleting )
            {
                OnLinkResourceSaved( from, target, propID, LinkChangeType.Delete );
            }

            if ( LinkDeleted != null )
            {
                LinkDeleted( this, new LinkEventArgs( from, target, propID ) );
            }
        }

        // public for unit tests
        public ICollection GetAllProperties( int resID )
        {
            ArrayList rsList = new ArrayList();
            foreach( ITable table in _propTables )
            {
                rsList.Add( table.CreateModifiableResultSet( 0, resID ) );
            }
            return rsList;
        }

        internal IResultSet GetProperties( int resID, PropDataType propType )
        {
            ITable table = GetPropTable( propType );
            return table.CreateResultSet( 0, resID );
        }

        /**
         * Returns a result set containing all boolean properties of the specified resource.
         */

        // public for unit tests
        public IResultSet GetBoolProperties( int resID )
        {
            return _boolProps.CreateResultSet( 0, IntInternalizer.Intern( resID ) );
        }

        public IResultSet GetStringListProperties( int resID )
        {
            return _stringListProps.CreateResultSet( 0, IntInternalizer.Intern( resID ) );
        }

        public IResultSet GetStringListProperties( int resID, int propID )
        {
            return _stringListProps.CreateResultSet(
                0, IntInternalizer.Intern( resID ), 1, IntInternalizer.Intern( propID ), true );
        }

        internal IBLOB GetBlobProperty( int resID, int propID )
        {
            using( IResultSet rs = _blobProps.CreateResultSet( 0, IntInternalizer.Intern( resID ), 1, IntInternalizer.Intern( propID ), true ) )
            {
                using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "MyPalStorage.GetBlobProperty" ) )
                {
                    if ( !enumerator.MoveNext() )
                        return null;

                    return enumerator.Current.GetBLOBValue( 2 );
                }
            }
        }

        // -- link operations ------------------------------------------------

        internal void SaveLink( int resID, int resID2, int type )
        {
            CheckOwnerThread();
            lock( _links )
            {
                IRecord link = _links.NewRecord();
                link.SetValue( 0, IntInternalizer.Intern( resID ) );
                link.SetValue( 1, IntInternalizer.Intern( resID2 ) );
                link.SetValue( 2, IntInternalizer.Intern( type ) );
                SafeCommitRecord( link, "MyPalStorage.SaveLink" );
            }
        }

        internal void DeleteLink( Resource res, Resource res2, int propType )
        {
            CheckOwnerThread();
            using( IResultSet rs = _links.CreateResultSet( 0, IntInternalizer.Intern( res.Id ), 2, IntInternalizer.Intern( propType ), true ) )
            {
                using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "MyPalStorage.DeleteLink" ) )
                {
                    while( enumerator.MoveNext() )
                    {
                        IRecord rec = enumerator.Current;
                        if ( rec.GetIntValue( 1 ) == res2.Id )
                        {
                            SafeDeleteRecord( rec, "MyPalStorage.DeleteLink" );
                            break;
                        }
                    }
                }
            }

            OnLinkDeleted( res, res2, propType );
        }

        internal void DeleteAllLinks( int resID )
        {
            DeleteAllLinks( resID, 0 );
            DeleteAllLinks( resID, 1 );
        }

        private void DeleteAllLinks( int resID, int columnIndex )
        {
            using( IResultSet rs = _links.CreateModifiableResultSet( columnIndex, IntInternalizer.Intern( resID ) ) )
            {
                DeleteAllRecords( rs, "MyPalStorage.DeleteAllLinks" );
            }
        }

        internal IResultSet GetLinksFrom( int resId )
        {
            return _links.CreateResultSet( 0, IntInternalizer.Intern( resId ) );
        }

        internal IResultSet GetLinksTo( int resId )
        {
            return _links.CreateResultSet( 1, IntInternalizer.Intern( resId ) );
        }

        internal IResultSet GetLinksFrom( int resId, int propType )
        {
            return _links.CreateResultSet(
                0, IntInternalizer.Intern( resId ), 2, IntInternalizer.Intern( propType ), true );
        }

        internal IResultSet GetLinksTo( int resId, int propType )
        {
            return _links.CreateResultSet(
                1, IntInternalizer.Intern( resId ), 2, IntInternalizer.Intern( propType ), true );
        }

        internal IResultSet SelectLinksOfType( int propID )
        {
            return _links.CreateResultSet( 2, IntInternalizer.Intern( propID ) );
        }

        public bool LinkExists( int resID, int resID2, int type )
        {
            using( IResultSet rs = _links.CreateResultSet( 0, IntInternalizer.Intern( resID ), 2, IntInternalizer.Intern( type ), true ) )
            {
                foreach( IRecord rec in rs )
                {
                    if ( rec.GetIntValue( 1 ) == resID2 )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /**
         * Deletes all properties of the specified type from the database and
         * cached resources.
         */

        internal void DeletePropsOfType( int propType )
        {
            PropDataType dataType = PropTypes [propType].DataType;
        	IResultSet rs;
            if ( dataType == PropDataType.LongString || dataType == PropDataType.Blob )
            {
                rs = GetPropTable( dataType ).CreateResultSet( 0 );
            }
            else
            {
                rs = SelectResourcesWithProp( propType );
            }

        	List<int> ids = new List<int>();
        	using( rs )
            {
            	using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "MyPalStorage.DeletePropsOfType()" ) )
                {
                    while( enumerator.MoveNext() )
                    {
                        IRecord rec = enumerator.Current;
                        if ( rec.GetIntValue( 1 ) == propType )
                        {
                        	ids.Add(rec.GetIntValue( 0 ));
/*
                            IResource res = GetResourceFromCache( id );
                            if ( res != null )
                            {
                                res.DeleteProp( propType );
                            }
                            else
                            {
                                SafeDeleteRecord( rec, "MyPalStorage.DeletePropsOfType()" );
                            }
*/
                        }
                    }
                }
            }
        	foreach(var id in ids)
        	{
        		IResource res = TryLoadResource(id);
        		if(res != null)
        			res.DeleteProp(propType);
        	}
        }

        /**
         * Deletes all links of the specified type from the database and
         * cached resources.
         */

        internal void DeleteLinksOfType( int propType )
        {
            using( IResultSet rs = SelectLinksOfType( propType ) )
            {
                using( SafeRecordEnumerator enumerator = new SafeRecordEnumerator( rs, "MyPalStorage.DeleteLinksOfType" ) )
                {
                    while( enumerator.MoveNext() )
                    {
                        IRecord rec = enumerator.Current;

                        int id1 = rec.GetIntValue( 0 );
                        int id2 = rec.GetIntValue( 1 );

                        IResource res1 = GetResourceFromCache( id1 );
                        IResource res2 = GetResourceFromCache( id2 );
                        if ( res1 != null || res2 != null )
                        {
                            if ( res1 == null )
                                res1 = LoadResource( id1 );
                            if ( res2 == null )
                                res2 = LoadResource( id2 );
                            res1.DeleteLink( propType, res2 );
                        }
                        else
                        {
                            SafeDeleteRecord( rec, "MyPalStorage.DeleteLinksOfType" );
                        }
                    }
                }
            }
        }

        public void RegisterLinkRestriction(string fromResourceType, int linkType,
            string toResourceType, int minCount, int maxCount)
        {
            ResourceRestrictions.RegisterLinkRestriction(fromResourceType, linkType, toResourceType, minCount, maxCount);
        }


        public void RegisterLinkRestriction(string fromResourceType, PropId<IResource> linkType, string toResourceType,
                                            int minCount, int maxCount)
        {
            ResourceRestrictions.RegisterLinkRestriction(fromResourceType, linkType.Id, toResourceType,
                minCount, maxCount);
        }

        public int GetMinLinkCountRestriction( string fromResourceType, int linkType )
        {
            return ResourceRestrictions.GetMinLinkCountRestriction( fromResourceType, linkType );
        }

        public int GetMaxLinkCountRestriction( string fromResourceType, int linkType )
        {
            return ResourceRestrictions.GetMaxLinkCountRestriction( fromResourceType, linkType );
        }

        public string GetLinkResourceTypeRestriction( string fromResourceType, int linkType )
        {
            return ResourceRestrictions.GetLinkResourceTypeRestriction( fromResourceType, linkType );
        }

        public void RegisterUniqueRestriction( string resourceType, int propId )
        {
            ResourceRestrictions.RegisterUniqueRestriction( resourceType, propId );
        }

        public void DeleteUniqueRestriction( string resourceType, int propId )
        {
            ResourceRestrictions.DeleteUniqueRestriction( resourceType, propId );
        }

        public void RegisterCustomRestriction( string resourceType, int propId, IResourceRestriction restriction )
        {
            ResourceRestrictions.RegisterCustomRestriction( resourceType, propId, restriction );
        }

        public void DeleteCustomRestriction( string resourceType, int propId )
        {
            ResourceRestrictions.DeleteCustomRestriction( resourceType, propId );
        }

        public void RegisterRestrictionOnDelete( string resourceType, IResourceRestriction restriction )
        {
            ResourceRestrictions.RegisterRestrictionOnDelete( resourceType, restriction );
        }

        public void DeleteRestrictionOnDelete( string resourceType )
        {
            ResourceRestrictions.DeleteRestrictionOnDelete( resourceType );
        }

        // -- resource select operations -------------------------------------------

        public IResourceList ListFromIds( IntArrayList resourceIDs, bool live )
        {
            return new ResourceList( new PlainListPredicate( resourceIDs ), live );
        }

		public IResourceList ListFromIds( int[] resourceIDs, bool live )
		{
            return ListFromIds( new IntArrayList( resourceIDs ), live );
		}

		public IResourceList ListFromIds( ICollection resourceIDs, bool live )
		{
            IntArrayList list = resourceIDs as IntArrayList;
            return ListFromIds( list ?? new IntArrayList( resourceIDs ), live );
		}

        public IResourceList FindResources( string resType, int propID, object propValue )
        {
            if ( propValue == null )
                throw new ArgumentNullException( "propValue" );

            return FindResourcesInRange( SelectionType.Normal, resType, propID, propValue, null );
        }

        public IResourceList FindResources( string resType, string propName, object propValue )
        {
            if ( propValue == null )
                throw new ArgumentNullException( "propValue" );

            return FindResourcesInRange( SelectionType.Normal, resType, GetPropId( propName ), propValue, null );
        }

        public IResourceList FindResources<T>(string resType, PropId<T> propId, T propValue)
        {
            return FindResources(resType, propId.Id, propValue);
        }

        public BusinessObjectList<T> FindResources<T, V>(ResourceTypeId<T> resType, PropId<V> propId, V propValue) where T : BusinessObject
        {
            return new BusinessObjectList<T>(resType, FindResources(resType.Name, propId, propValue));
        }

        public IResourceList FindResourcesLive( string resType, int propID, object propValue )
        {
            if ( propValue == null )
                throw new ArgumentNullException( "propValue" );

            return FindResourcesInRange( SelectionType.Live, resType, propID, propValue, null );
        }

        public IResourceList FindResourcesLive( string resType, string propName, object propValue )
        {
            if ( propValue == null )
                throw new ArgumentNullException( "propValue" );

            return FindResourcesInRange( SelectionType.Live, resType, GetPropId( propName ), propValue, null );
        }

        public IResourceList FindResourcesLive<T>(string resType, PropId<T> propId, T propValue)
        {
            return FindResourcesLive(resType, propId.Id, propValue);
        }

        public IResourceList FindResources( SelectionType selType, string resType, int propID, object propValue )
        {
            if ( propValue == null )
                throw new ArgumentNullException( "propValue" );

            return FindResourcesInRange( selType, resType, propID, propValue, null );
        }

        public IResourceList FindResources( SelectionType selType, string resType, string propName, object propValue )
        {
            if ( propValue == null )
                throw new ArgumentNullException( "propValue" );

            return FindResourcesInRange( selType, resType, GetPropId( propName ), propValue, null );
        }

        public IResourceList FindResourcesInRange( string resType, int propID, object minValue, object maxValue )
        {
            return FindResourcesInRange( SelectionType.Normal, resType, propID, minValue, maxValue );
        }

        public IResourceList FindResourcesInRange( string resType, string propName, object minValue, object maxValue )
        {
            return FindResourcesInRange( SelectionType.Normal, resType, GetPropId( propName ), minValue, maxValue );
        }

        public IResourceList FindResourcesInRangeLive( string resType, int propID, object minValue, object maxValue )
        {
            return FindResourcesInRange( SelectionType.Live, resType, propID, minValue, maxValue );
        }

        public IResourceList FindResourcesInRangeLive( string resType, string propName, object minValue, object maxValue )
        {
            return FindResourcesInRange( SelectionType.Live, resType, GetPropId( propName ), minValue, maxValue );
        }

        public IResourceList FindResourcesInRange( SelectionType listType, string resType, string propName,
            object minValue, object maxValue )
        {
        	return FindResourcesInRange( listType, resType, GetPropId( propName ), minValue, maxValue );
        }

        /// <summary>
        /// Returns the list of resources for which the property with the specified name has a value in the specified range.
        /// </summary>
        /// <param name="listType">Specifies the update notification mode of a resource list.</param>
        /// <param name="resType">The type of resources to return, or <c>null</c> if resources of any type should be returned.</param>
        /// <param name="propID">The ID of the property for which the selection is done.</param>
        /// <param name="minValue">The minimum matching value of the property, must be non-<c>null</c>.</param>
        /// <param name="maxValue">The maximum matching value of the property, may be null, works as exact search in this case.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>
        /// <para>Range selection is only supported for int and date properties.</para>
        /// </remarks>
        public IResourceList FindResourcesInRange( SelectionType listType, string resType, int propID,
            object minValue, object maxValue )
        {
            bool isSnapshot = (listType == SelectionType.LiveSnapshot );
            bool isLive = (listType == SelectionType.Live ||
                listType == SelectionType.LiveSnapshot );

            ResourceListPredicate pred = CreateSelectionPredicate( propID, minValue, maxValue, isSnapshot );

            return IntersectPredicateWithType( pred, resType, isLive );
        }

        private ResourceListPredicate CreateSelectionPredicate( int propId, object minValue,
            object maxValue, bool isSnapshot )
        {
            PropDataType propType = GetPropDataType( propId );
            if ( propType == PropDataType.Bool )
            {
                bool boolValue = (bool) minValue;
                if ( !boolValue )
                {
                    String name = PropTypes[ propId ].Name;
                    throw new StorageException( "Selections by bool property are only supported for true value (propName=[" + name + "]" );
                }
                if ( maxValue != null)
                {
                    throw new StorageException( "Range selections by bool property are not supported" );
                }
                return new ResourcesWithPropPredicate( propId, isSnapshot );
            }
            else if ( propType == PropDataType.Int || propType == PropDataType.String || propType == PropDataType.Date ||
                propType == PropDataType.StringList )
            {
                CheckValueType( propId, propType, minValue );
                if ( maxValue != null )
                {
                    if ( propType == PropDataType.String || propType == PropDataType.StringList )
                        throw new StorageException( "Range selections on string and StringList properties are not supported" );

                    CheckValueType( propId, propType, maxValue );

                    if ( minValue != null && maxValue != null &&
                        ((IComparable) minValue).CompareTo( maxValue ) > 0 )
                    {
                        return new PropValuePredicate( propId, maxValue, minValue, isSnapshot );
                    }
                    else
                    {
                        return new PropValuePredicate( propId, minValue, maxValue, isSnapshot );
                    }
                }
                else
                    return new PropValuePredicate( propId, minValue, null, isSnapshot );
            }
            else
            {
                throw new StorageException( "Selection on " + propType + " properties (" +
                    PropTypes [propId].Name + ") is not supported" );
            }
        }

        public IResourceList FindResourcesWithProp( string resType, int propID )
        {
            return FindResourcesWithProp( SelectionType.Normal, resType, propID );
        }

        public IResourceList FindResourcesWithProp( string resType, string propName )
        {
            return FindResourcesWithProp( SelectionType.Normal, resType, GetPropId( propName ) );
        }

        public IResourceList FindResourcesWithProp<T>(string resType, PropId<T> propId)
        {
            return FindResourcesWithProp(resType, propId.Id);
        }

        public IResourceList FindResourcesWithPropLive( string resType, int propID )
        {
            return FindResourcesWithProp( SelectionType.Live, resType, propID );
        }

        public IResourceList FindResourcesWithPropLive( string resType, string propName )
        {
            return FindResourcesWithProp( SelectionType.Live, resType, GetPropId( propName ) );
        }

        public IResourceList FindResourcesWithPropLive<T>(string resType, PropId<T> propId)
        {
            return FindResourcesWithPropLive(resType, propId.Id);
        }

        public IResourceList FindResourcesWithProp( SelectionType selType, string resType, int propID )
        {
            bool isSnapshot = (selType == SelectionType.LiveSnapshot );
            bool isLive = (selType == SelectionType.Live ||
                selType == SelectionType.LiveSnapshot );

            ResourceListPredicate pred;
            PropDataType dataType = GetPropDataType( propID );
            if ( dataType == PropDataType.LongString || dataType == PropDataType.Blob || dataType == PropDataType.Double )
            {
                throw new StorageException( "FindResourcesWithProp() is not supported for " + dataType + " properties (" +
                    PropTypes [propID].Name + ")" );
            }
            if ( dataType == PropDataType.Link )
            {
                pred = new ResourcesWithLinkPredicate( propID, isSnapshot );
            }
            else
            {
                pred = new ResourcesWithPropPredicate( propID, isSnapshot );
            }
            return IntersectPredicateWithType( pred, resType, isLive );
        }

        public IResourceList FindResourcesWithProp( SelectionType selType, string resType, string propName )
        {
            return FindResourcesWithProp( selType, resType, GetPropId( propName ) );
        }

        internal ResourceList IntersectPredicateWithType( ResourceListPredicate pred, string resType, bool live )
        {
            if ( resType != null )
            {
                ResourceListPredicate typePred = new ResourceTypePredicate( resType );
                // NOTE: the order is important (the type predicate is faster to match, so we
                // put it first)
                pred = new IntersectionPredicate( typePred, pred );
            }
            return new ResourceList( pred, live );
        }

        public IResource FindUniqueResource( string resType, int propID, object propValue )
        {
            if ( propValue == null )
            {
                throw new ArgumentNullException( "propValue" );
            }

            object id;
            _findUniqueResourceCacheLock.Enter();
            try
            {
                id = _findUniqueResourceCache.TryKey( propValue );
            }
            finally
            {
                _findUniqueResourceCacheLock.Exit();
            }
            if( id != null )
            {
                IResource res = TryLoadResource( (int) id );
                if( res != null && ( resType == null || res.Type == resType ) &&
                    res.HasProp( propID ) && propValue.Equals( res.GetProp( propID ) ) )
                {
                    return res;
                }
            }

            ResourceListPredicate pred = CreateSelectionPredicate( propID, propValue, null, false );
            bool sortedById;
            IntArrayList resIds = pred.GetMatchingResources( out sortedById );

            if ( resIds.Count > 128 )
            {
                IntArrayList result = resIds;
                if ( resType != null )
                {
                    bool typeSortedById;
                    IntArrayList typeIds = new ResourceTypePredicate( resType ).GetMatchingResources( out typeSortedById );
                    if ( !typeSortedById )
                    {
                        typeIds.Sort();
                    }
                    if ( !sortedById )
                    {
                        resIds.Sort();
                    }
                    result.IntersectSorted( typeIds );
                }

                if ( result.Count > 1 )
                {
                    if ( resType != null && ResourceRestrictions.UniqueRestrictionExists( resType, propID ) )
                    {
                        SetRepairRequired();
                        return LoadResource( result [0] );
                    }
                    throw new StorageException( "Multiple resources found where a unique resource was expected" );
                }
                if ( result.Count == 0 )
                {
                    return null;
                }
                _findUniqueResourceCacheLock.Enter();
                try
                {
                    _findUniqueResourceCache.CacheObject( propValue, result [0] );
                }
                finally
                {
                    _findUniqueResourceCacheLock.Exit();
                }
                return LoadResource( result [0] );
            }
            else
            {
                IResource result = null;
                for( int i=0; i<resIds.Count; i++ )
                {
                    if ( resType == null && result != null )
                    {
                        throw new StorageException( "Multiple resources found where a unique resource was expected" );
                    }

                    IResource res = TryLoadResource( resIds [i] );
                    if ( res == null )
                    {
                        continue;
                    }

                    if ( resType == null || resType == res.Type )
                    {
                        if ( result != null )
                        {
                            if ( ResourceRestrictions.UniqueRestrictionExists( resType, propID ) )
                            {
                                SetRepairRequired();
                                return result;
                            }
                            throw new StorageException( "Multiple resources found where a unique resource was expected" );
                        }
                        result = res;
                    }
                }
                if( result != null )
                {
                    _findUniqueResourceCacheLock.Enter();
                    try
                    {
                        _findUniqueResourceCache.CacheObject( propValue, result.Id );
                    }
                    finally
                    {
                        _findUniqueResourceCacheLock.Exit();
                    }
                }
                return result;
            }
        }

        public IResource FindUniqueResource( string resType, string propName, object propValue )
        {
            return FindUniqueResource( resType, GetPropId( propName ), propValue );
        }

        public IResourceList GetAllResources( string resType )
        {
            if ( resType == null )
                throw new ArgumentNullException( "resType" );

            return new ResourceList( new ResourceTypePredicate( resType ), false );
        }

        public BusinessObjectList<T> GetAllResources<T>(ResourceTypeId<T> resType) where T : BusinessObject
        {
            return new BusinessObjectList<T>(resType, GetAllResources(resType.Name));
        }

        public IResourceList GetAllResourcesLive( string resType )
        {
            if ( resType == null )
                throw new ArgumentNullException( "resType" );

            return new ResourceList( new ResourceTypePredicate( resType ), true );
        }

        public IResourceList GetAllResources( string[] resTypes )
        {
            return GetMultiTypeResources( resTypes, false );
        }

        public IResourceList GetAllResourcesLive( string[] resTypes )
        {
            return GetMultiTypeResources( resTypes, true );
        }

        private IResourceList GetMultiTypeResources( string[] resTypes, bool live )
        {
            if ( resTypes == null )
                throw new ArgumentNullException( "resTypes" );
            if ( resTypes.Length == 0 )
            {
                return EmptyResourceList;
            }
            for( int i=0; i<resTypes.Length; i++ )
            {
                if ( resTypes [i] == null )
                {
                    throw new ArgumentException( "Found null item at index " + i + " in resTypes array", "resTypes" );
                }
            }

            if ( resTypes.Length == 1 )
            {
                return new ResourceList( new ResourceTypePredicate( resTypes [0] ), live );
            }
            return new ResourceList( new MultiResourceTypePredicate( resTypes ), live );
        }

        public IResourceList EmptyResourceList
        {
            get { return _emptyResourceList; }
        }

        public void CachePredicate( IResourceList resourceList )
        {
            lock( _predicateCache )
            {
                ResourceList rlist = (ResourceList) resourceList;
                if ( !_predicateCache.Contains( rlist.Predicate ) )
                {
                    lock( rlist )
                    {
                        rlist.SetUpdatePriority();
                        rlist.Instantiate( false );
                    }
                    _predicateCache.Add( rlist.Predicate, new CachingPredicate( rlist ) );
                }
            }
        }

        internal ResourceListPredicate GetCachedPredicate( ResourceListPredicate predicate )
        {
            lock( _predicateCache )
            {
                return (ResourceListPredicate) _predicateCache [predicate];
            }
        }

        // -- internal select operations -------------------------------------------

        internal IResultSet SelectResources( int propID, object propValue )
        {
            ITable propTable = GetPropTable( propID );
            if ( _shutDown )
            {
                return propTable.EmptyResultSet;
            }
            return propTable.CreateResultSet( 1, propID, 2, propValue, true );
        }

        internal IResultSet SelectResourcesInRange( int propID, object minValue, object maxValue )
        {
            ITable propTable = GetPropTable( propID );
            if ( _shutDown )
            {
                return propTable.EmptyResultSet;
            }
            return propTable.CreateResultSetForRange( 1, propID, 2, minValue, maxValue );
        }

        internal IResultSet SelectResourcesWithProp( int propID )
        {
            if ( GetPropDataType( propID ) == PropDataType.Bool )
            {
                return _boolProps.CreateResultSet( 1, propID );
            }

            ITable propTable = GetPropTable( propID );
            if ( _shutDown )
            {
                return propTable.EmptyResultSet;
            }
            return propTable.CreateResultSet( 1, propID );
        }

        internal IResultSet SelectAllResources( int typeID )
        {
            return _resources.CreateResultSet( 1, typeID );
        }

        private void dbStructure_ProgressListenerEvent(string progress, int tableNum, int tableCount)
        {
            OnRebuildProgress( progress, tableNum, tableCount );
        }
        public delegate void RebuildProgressEventHandler( string progress, int tableNum, int tableCount );
        public event RebuildProgressEventHandler RebuildProgressListenerEvent;
        public void OnRebuildProgress( string progress, int tableNum, int tableCount )
        {
            if ( RebuildProgressListenerEvent != null )
            {
                RebuildProgressListenerEvent( progress, tableNum, tableCount );
            }
        }

        private static void _theStorage_RebuildProgressListenerEvent(string progress, int tableNum, int tableCount)
        {
            if ( _progressWindow != null )
            {
                if ( tableCount == 0 ) tableCount = 1;
                int percentage = (int)( ( 100.0 * tableNum ) / tableCount );
                if ( percentage > 100 ) percentage = 100;
                _progressWindow.UpdateProgress( percentage, progress, null );
            }
        }

        public void MarkHiddenResourceTypes( string[] loadedPluginNames )
        {
            MarkNotLoadedPlugins( "ResourceType", loadedPluginNames );
            MarkNotLoadedPlugins( "PropType", loadedPluginNames );
        }

        internal void SetOwnerPlugin( IResource res, IPlugin ownerPlugin )
        {
            if ( ownerPlugin != null )
            {
                IStringList ownerPlugins = res.GetStringListProp( "OwnerPluginList" );
                string ownerPluginName = ownerPlugin.GetType().FullName;
                if ( ownerPlugins.IndexOf( ownerPluginName ) < 0 )
                {
                    ownerPlugins.Add( ownerPluginName );
                }
            }
        }

        private void MarkNotLoadedPlugins( string resType, string[] loadedPluginNames )
        {
            foreach( IResource resTypeResource in FindResourcesWithProp( resType, "OwnerPluginList" ) )
            {
                IStringList ownerPlugins = resTypeResource.GetStringListProp( "OwnerPluginList" );
                if ( ownerPlugins.Count == 0 )
                {
                    return;
                }

                bool anyLoaded = false;
                foreach( string ownerPlugin in ownerPlugins )
                {
                    if ( Array.IndexOf( loadedPluginNames, ownerPlugin ) >= 0 )
                    {
                        anyLoaded = true;
                        break;
                    }
                }
                if ( !anyLoaded )
                {
                    string name = resTypeResource.GetStringProp( "Name" );
                    if ( name == null )  // DB corruption recovery (OM-8930)
                    {
                        continue;
                    }
                    if ( resType == "ResourceType" && ResourceTypes.Exist( name ) )
                    {
                        ((ResourceTypeItem)ResourceTypes [name]).SetOwnerPluginUnloaded();
                    }
                    else if ( resType == "PropType" && PropTypes.Exist( name ) )
                    {
                        ((PropTypeItem)PropTypes [name]).SetOwnerPluginUnloaded();
                    }
                }
            }
        }

        public void RegisterDisplayNameProvider( IDisplayNameProvider provider )
        {
            Guard.NullArgument( provider, "provider" );
            _displayNameProviders.Add( provider );
        }

        internal string CalcCustomDisplayName( IResource res )
        {
            foreach( IDisplayNameProvider provider in _displayNameProviders )
            {
                string displayName = provider.GetDisplayName( res );
                if ( !string.IsNullOrEmpty(displayName) )
                {
                    return displayName;
                }
            }
            return "";
        }

        public class IndexRebuildException: Exception
        {
            public IndexRebuildException( string message, Exception innerException )
                : base( message, innerException )
            {
            }
        }
    }
}
