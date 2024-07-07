// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.Database;
using JetBrains.Omea.ResourceStore;

namespace JetBrains.Omea.Maintenance
{
    internal class DatabaseProxy : IDisposable
    {
        private DBStructure _dbStructure;
        private IDatabase   _database;
        private static readonly string[] _tables = new string[]
            {
                "ResourceTypes", "PropTypes", "Resources", "Links", "StringProps", "LongStringProps",
                "StringListProps", "DateProps", "IntProps", "BlobProps", "DoubleProps"
            };

        public DatabaseProxy( string path )
        {
            _dbStructure = new DBStructure( path, "MyPal" );
            _dbStructure.LoadStructure( true );
            _database = _dbStructure.OpenDatabase();
        }

        public void Populate( ListView view )
        {
            view.Items.Clear();
            foreach( string table in _tables )
            {
                ITable tbl = _database.GetTable( table );
                ListViewItem item = view.Items.Add( table );
                item.Tag = tbl;
                RecordsCounts counts = tbl.ComputeWastedSpace();
                int normalRecords = counts.NormalRecordCount;
                item.SubItems.Add( ( normalRecords == 0 ) ? "Empty" : normalRecords.ToString() );
                int totalRecords = counts.TotalRecordCount;
                item.SubItems.Add(
                    ( totalRecords == 0 ) ? "0%" : ( ( totalRecords - normalRecords) * 100 / totalRecords ).ToString() + "%" );
            }
        }

        public void Diagnose( RepairProgressEventHandler progress )
        {
            ResourceStoreRepair rsRepair = new ResourceStoreRepair( _database );
            rsRepair.FixErrors = false;
            rsRepair.DumpStructure = false;
            rsRepair.RepairProgress += progress;
            rsRepair.Run();
            rsRepair.RepairProgress -= progress;
        }

        public void Dispose()
        {
            _dbStructure.Shutdown();
        }

        public void RebuildIndexes( DBStructure.ProgressEventHandler progress )
        {
            _dbStructure.ProgressListenerEvent += progress;
            foreach( string table in _tables )
            {
                ITable tbl = _database.GetTable( table );
                tbl.RebuildIndexes();
            }
            _dbStructure.ProgressListenerEvent -= progress;
        }
    }
}
