/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Database
{
    internal class Database : IDatabase, IDatabaseDesign
    {
        #region class data members
        private HashMap _tables = new HashMap();
        private DBStructure _dbStructure = null;
        private BlobFileSystem _bfs;
        private Tracer _tracer = null;
        #endregion

        public Database( DBStructure dbStructure )
        {
            _dbStructure = dbStructure;
            int databaseCacheSize = 0x20000;
            if( ICore.Instance != null )
            {
                databaseCacheSize = Core.SettingStore.ReadInt( "Omea", "DatabaseCacheSize", 2048 * 1024 );
            }
            _bfs = new BlobFileSystem(
                IOTools.Combine( Path, Name ) + ".BlobFileSystem.dbUtil", databaseCacheSize >> 3, 256 );
            _tracer = new Tracer( "(DBUtils) Database - " + Name );
        }

        public int    Version         { get { return _dbStructure.Version; } }
        public string Name            { get { return _dbStructure.Name; } }
        public string Path            { get { return _dbStructure.Path; } }
        public bool NeedUpgradeTo22 { get { return _dbStructure.NeedUpgradeTo22; } }

        public ITableDesign CreateTable( string name, TableStructure tblStructure )
        {
            if ( !_tables.Contains( name ) )
            {
                Table table = new Table( this, tblStructure );
                _tables.Add( name, table );
                return table;
            }
            throw new TableAlreadyExistsException( "Database '" + Name + "' already contains '" + name +"' table.", name );
        }

        public BlobFileSystem BlobFS
        {
            get { return _bfs; }
        }

        public void RepairBlobFileSystem()
        {
            _bfs.Repair();
        }

        public void Dump()
        {
            foreach ( HashMap.Entry entry in _tables )
            {
                ((Table)entry.Value).Dump();
            }
        }

        public ITable GetTable( string name )
        {
            if ( _tables.Contains( name ) )
            {
                return (Table)_tables[ name ];
            }
            throw new TableDoesNotExistException( "Database '" + Name + "' does not contain '" + name +"' table.", name );
        }

        internal void RebuildIndexes( bool forceRebuild )
        {
            _tracer.Trace( "Rebuild indexes" );
            foreach ( HashMap.Entry entry in _tables )
            {
                Table table = (Table)entry.Value;
                if ( table.Dirty || forceRebuild )
                {
                    table.RebuildIndexes();
                }
            }
        }

        internal void Defragment( )
        {
            _tracer.Trace( "Defragmentation indexes" );
            int tableNum = 1;
            foreach ( HashMap.Entry entry in _tables )
            {
                Table table = (Table)entry.Value;
                try
                {
                    OnProgress( "Defragmenting '" + table.Name + "' table.", tableNum++, _tables.Count );
                }
                catch (Exception)
                {
                }
                table.Defragment();
            }
        }

        public void Flush()
        {
            _tracer.Trace( "Flush" );
            foreach ( HashMap.Entry entry in _tables )
            {
                ((Table)entry.Value).Flush();
            }
            _dbStructure.SaveStructure();
        }

        public void Shutdown()
        {
            _tracer.Trace( "Shutdown" );
            foreach ( HashMap.Entry entry in _tables )
            {
                ((Table)entry.Value).Shutdown();
            }
            _bfs.Dispose();
            _dbStructure.SaveStructure();
            _dbStructure.Shutdowned();
        }

        public delegate void ProgressEventHandler( string progress, int tableNum, int tableCount );
        public event ProgressEventHandler ProgressListenerEvent;
        public void OnProgress( string progress, int tableNum, int tableCount )
        {
            if ( ProgressListenerEvent != null )
            {
                ProgressListenerEvent( progress, tableNum, tableCount );
            }
            else
            {
                _dbStructure.OnProgress( progress, tableNum, tableCount );
            }
        }
        public void LowLevelCheck()
        {
            foreach ( HashMap.Entry entry in _tables )
            {
                ((Table)entry.Value).LowLevelCheck();
            }
        }
    }
}
