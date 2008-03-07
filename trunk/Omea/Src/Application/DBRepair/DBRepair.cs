/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using System.Threading;
using Ini;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Database;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.TextIndex;
using Microsoft.Win32;

namespace DBRepair
{
    /**
     * OmniaMea database analysis and repair utility.
     */
    
    class DBRepair
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main( string[] args )
        {
            args = args;
            try
            {
                new DBRepair().RunRepair();
            }
            catch( Exception e )
            {
                Console.WriteLine( "Exception processing database: " + e.ToString() );
            }
        }

        private bool _fixErrors = false;
        private bool _textAnalyze = false;
        private bool _lowCheck = false;
        private bool _dump = false;
        private bool _dbdump = false;
        private bool _defrag = false;
        private bool _forceRebuild = false;
        private bool _calcSpace = false;
        private bool _backup = false;
        private bool _restore = false;
        private bool _deleteIndex = false;
        private bool _deleteTextIndex = false;
        private bool _ignoreMutex = false;
        private string _lastProgressLine = null;
        private IniFile _ini;
        
        private void ShowHelp()
        {
            Console.WriteLine( "Omea database diagnostics and repair utility" );
            Console.WriteLine( "Usage: ");
            Console.WriteLine( "  DBRepair.exe           - diagnose errors" );
            Console.WriteLine( "  DBRepair.exe /fix      - diagnose and fix errors" );
            Console.WriteLine( "  DBRepair.exe /lowcheck - diagnose low level records consistance for all tables" );
            Console.WriteLine( "  DBRepair.exe /rebuild  - force the rebuild of database indexes" );
            Console.WriteLine( "  DBRepair.exe /defrag   - defragment the database" );
            Console.WriteLine( "  DBRepair.exe /space    - calculate wasted space in the database" );
            Console.WriteLine( "  DBRepair.exe /backup   - backup the database" );
            Console.WriteLine( "  DBRepair.exe /restore  - restore the database from the last backup" );
            Console.WriteLine( "  DBRepair.exe /dump     - dump storage contents to the standard output" );
            Console.WriteLine( "  DBRepair.exe /dbdump   - dump database contents for all tables to the standard output" );
            Console.WriteLine( "  DBRepair.exe /text     - diagnose text index" );
            Console.WriteLine( "  DBRepair.exe /workdir <directory> - use a non-standard database directory" );
            Console.WriteLine( "  DBRepair.exe /deleteindex - delete database and text index files" );
            Console.WriteLine( "  DBRepair.exe /deletetextindex - delete text index files" );
        }

        private void RunRepair()
        {
            string dbPath = null;

            string[] args = Environment.GetCommandLineArgs();
            if ( args.Length > 1 )
            {
                if ( args [1] == "-?" || args [1] == "/?" )
                {
                    ShowHelp();
                    return;
                }
                string arg = args [1].ToLower();
                if ( ( arg == "-workdir" || arg == "/workdir" ) && args.Length > 2 )
                {
                    dbPath = args [2];
                    if ( args.Length > 3 )
                    {
                        ProcessArgument( args [3] );
                    }
                }
                else
                {
                    ProcessArgument( arg );
                }
            }

            if ( dbPath == null )
            {
                dbPath = RegUtil.DatabasePath;
                if ( dbPath == null )
                {
                    dbPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ),
                        "JetBrains\\Omea" );
                }
            }

            OMEnv.WorkDir = dbPath;
            MyPalStorage.DBPath = Path.Combine( OMEnv.WorkDir, "db" );
            _ini = new IniFile(Path.Combine( OMEnv.WorkDir, "OmniaMea.ini" ) );

            Console.WriteLine( "Processing database in " + dbPath );
           
            bool omniaMeaIsNotRun;
            Mutex omniaMeaMux = new Mutex( true, "OmniaMeaMutex", out omniaMeaIsNotRun );
            try
            {
                if ( !omniaMeaIsNotRun && !_ignoreMutex )
                {
                    Console.WriteLine( "Omea is currently running. Please close Omea before running DBRepair." );
                    return;
                }

                if ( _deleteIndex )
                {
                    DeleteIndex();
                }
                else if ( _deleteTextIndex )
                {
                    DeleteFiles( OMEnv.WorkDir, "_*" );
                }
                else if( _backup )
                {
                    string backupPath = _ini.ReadString( "ResourceStore", "BackupPath", string.Empty );
                    if( backupPath.Length > 0 ) 
                    {
                        Console.Write( "Database backup in progress..." );
                        MyPalStorage.BackupDatabase( IOTools.Combine( backupPath, MyPalStorage._dbBackupFile ) );
                        Console.WriteLine( "\r                               \rDatabase backup done." );
                    }
                    else
                    {
                        Console.WriteLine( "Backup path is not set. Run Omea, in Options | Paths set the path." );
                    }
                }
                else if( _restore )
                {
                    string backupPath = _ini.ReadString( "ResourceStore", "BackupPath", string.Empty );
                    if( backupPath.Length > 0 ) 
                    {
                        Console.Write( "Restoring database from backup..." );
                        MyPalStorage.RestoreFromBackup( IOTools.Combine( backupPath, MyPalStorage._dbBackupFile ) );
                        Console.WriteLine( "\r                                  \rDatabase restored from backup." );
                    }
                    else
                    {
                        Console.WriteLine( "Backup path is not set. Run Omea, in Options | Paths set the path." );
                    }
                }
                else 
                {
                    if( !_textAnalyze || _lowCheck )
                    {
                        ProcessDB();
                    }
                    else
                    {
                        try
                        {
                            ProcessTextIndex( OMEnv.WorkDir + "\\_term.index" );
                        }
                        catch( FormatException exc_ )
                        {
                            Console.Error.WriteLine( exc_.Message );
                        }
                    }
                }
            }
            finally
            {
                omniaMeaMux.Close();
            }
            if ( !_dump && !_deleteIndex )
            {
                Console.WriteLine( "Press Enter to continue..." );
                Console.ReadLine();
            }
        }

        private void ProcessArgument( string arg )
        {
            if ( arg.StartsWith( "/" ) )
            {
                arg = "-" + arg.Substring( 1 );
            }

            if ( arg.ToLower() == "-fix"  )
            {
                _fixErrors = true;
                _forceRebuild = true;
            }
            else
                _fixErrors = false;

            _dump = ( arg == "-dump" );
            _defrag = ( arg == "-defrag" );
            _backup = ( arg == "-backup" );
            _restore = ( arg == "-restore" );

            if ( arg == "-dbdump" )
                _dbdump = true;
            else
                _dbdump = false;

            switch( arg )
            {
                case "-lowcheck":            _lowCheck = true; break;
                case "-text":            _textAnalyze = true; break;
                case "-rebuild":         _forceRebuild = true; break;
                case "-space":           _calcSpace = true; break;
                case "-deleteindex":     _deleteIndex = true; break;
                case "-deletetextindex": _deleteTextIndex = true; break;
                case "-deleteindex-ignoremutex": _deleteIndex = true; _ignoreMutex = true; break;
            }
        }

        private void DeleteIndex()
        {
            DeleteFiles( MyPalStorage.DBPath, "*.blob" );
            DeleteFiles( MyPalStorage.DBPath, "*.dbUtil" );
            DeleteFiles( OMEnv.WorkDir, "_*" );
            DeleteFiles( OMEnv.WorkDir, "liveforms.dat" );
            if ( Directory.Exists( MyPalStorage.DBPath ) && Directory.GetFiles( MyPalStorage.DBPath ).Length == 0 )
            {
                Directory.Delete( MyPalStorage.DBPath );
            }
            RegUtil.DeleteValue( Registry.CurrentUser, @"Software\JetBrains\Omea", "DbPath" );
        }

        private static void DeleteFiles( string dir, string mask )
        {
            if ( !Directory.Exists( dir ) )
            {
                return;
            }

            string[] dbFiles = Directory.GetFiles( dir, mask );
            foreach (string dbFile in dbFiles)
            {
                string path = Path.Combine( dir, dbFile );
                Console.WriteLine( "Deleting " + path );
                File.Delete( path );
            }
        }

        private void ProcessDB()
        {
            if ( !DBHelper.DatabaseExists( MyPalStorage.DBPath, "MyPal" ) )
            {
                Console.WriteLine( "Omea database not found" );
                return;
            }

            DBStructure dbStructure = new DBStructure( MyPalStorage.DBPath, "MyPal" );
            dbStructure.ProgressListenerEvent += new DBStructure.ProgressEventHandler( dbStructure_ProgressListenerEvent );

            Console.WriteLine( "Loading database..." );
            
            bool structureCorrupted = false;
            try
            {
                dbStructure.LoadStructure( true );
            }
            catch( Exception )
            {
                structureCorrupted = true;
            }

            if ( structureCorrupted || !AllTablesExist( dbStructure ) )
            {
                Console.WriteLine( "Rebuilding database structure..." );
                dbStructure.Shutdown();
                
                MyPalStorage.CreateDatabase();
                dbStructure = new DBStructure( MyPalStorage.DBPath, "MyPal" );
                dbStructure.LoadStructure( true );
                _forceRebuild = true;
            }

            if ( _defrag )
            {
                Console.WriteLine( "Defragmenting database..." );
                dbStructure.Defragment();
            }
            else if ( !dbStructure.IsDatabaseCorrect() )
            {
                Console.WriteLine( "Database indexes are corrupt. Rebuilding..." );
                dbStructure.RebuildIndexes();
            }
            else if ( _forceRebuild )
            {
                Console.WriteLine( "Performing forced rebuild of database indexes..." );
                dbStructure.RebuildIndexes( true );
            }
            else if ( _lowCheck )
            {
                dbStructure.LowLevelCheck( );
                return;                
            }

            if ( _dbdump )
            {
                Console.WriteLine( "Dumping database..." );
                dbStructure.Dump();
            }

            IDatabase db = dbStructure.OpenDatabase();

            if ( _calcSpace )
            {
                CalcSpace( db, "ResourceTypes" );
                CalcSpace( db, "PropTypes" );
                CalcSpace( db, "IntProps" );
                CalcSpace( db, "StringProps" );
                CalcSpace( db, "LongStringProps" );
                CalcSpace( db, "StringListProps" );
                CalcSpace( db, "DateProps" );
                CalcSpace( db, "BlobProps" );
                CalcSpace( db, "DoubleProps" );
                CalcSpace( db, "Resources" );
                CalcSpace( db, "Links" );
                return;                
            }

            ResourceStoreRepair rsRepair = new ResourceStoreRepair( db );
            rsRepair.FixErrors = _fixErrors;
            rsRepair.DumpStructure = _dump;
            rsRepair.RepairProgress += new RepairProgressEventHandler( RsRepair_OnRepairProgress );
            rsRepair.Run();
        }

        private bool AllTablesExist( DBStructure structure )
        {
            foreach( string tableName in new string[] { "PropTypes", "ResourceTypes",
                                                        "IntProps", "StringProps", "LongStringProps",
                                                        "StringListProps", "DateProps", "BlobProps",
                                                        "DoubleProps", "Resources", "Links" } )
            {
                try
                {
                    structure.GetTable( tableName );
                }
                catch( TableDoesNotExistException )
                {
                    return false;
                }
            }
            return true;
        }

        private void RsRepair_OnRepairProgress( object sender, RepairProgressEventArgs e )
        {
            Console.WriteLine( e.Message );
        }

        private void CalcSpace( IDatabase db, string tableName )
        {
            ITable tbl = db.GetTable( tableName );
            RecordsCounts counts = tbl.ComputeWastedSpace();
            if ( counts.NormalRecordCount == 0 && counts.TotalRecordCount == 0 )
            {
                Console.WriteLine( "{0}: empty", tableName );
            }
            else
            {
                long deletedRecordsCount = counts.TotalRecordCount - counts.NormalRecordCount;
                Console.WriteLine( "{0}: total {1} records , wasted {2} records, percentage {3}%", 
                    tableName, counts.TotalRecordCount, deletedRecordsCount,
                    (deletedRecordsCount * 100) / counts.TotalRecordCount );
            }
        }

        //-------------------------------------------------------------------------
        private void ProcessTextIndex( string TermIndexName )
        {
            TermIndexAccessor   termIndex = new TermIndexAccessor( TermIndexName );
            try 
            {

                Console.Write( "ok\nChecking TermIndex component..." );
                termIndex.Load();

                Console.Write( "ok\nPerforming cross component linkage checks..." );
                CrossIndexChecks( termIndex );

                Console.WriteLine( "ok\nText index corruption check passed:" );
                Console.WriteLine( "\t" + termIndex.TermsNumber + " term entries parsed" );
            }
            finally
            {
                termIndex.Close();
            }
        }
        protected   void    CrossIndexChecks( TermIndexAccessor termIndex )
        {
            foreach( KeyPair pair in termIndex.Keys )
            {
                TermIndexRecord termRecord = termIndex.GetRecordByHandle( pair._offset );
                for( int j = 0; j < termRecord.DocsNumber; j++ )
                {
                    Entry   entry = termRecord.GetEntryAt( j );
                    if( entry.DocIndex < -1 )
                        throw new FormatException( "DocIndex is negative in the TermIndex record entry" );
                    if( entry.Count <= 0 )
                        throw new FormatException( "Number of term instances is negative in the TermIndex record" );
                }
            }
        }
        private void dbStructure_ProgressListenerEvent( string progress, int tableNum, int tableCount )
        {
            if ( progress != _lastProgressLine )
            {
                _lastProgressLine = progress;
                Console.WriteLine( progress );
            }
        }
    }
}
