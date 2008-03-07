/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.Database;
using JetBrains.Omea.Containers;

namespace PerformanceTests
{
    /**
     * insertion tests
     */
    public abstract class DBInsertPerformanceTestBase: PerformanceTestBase
    {
        protected ITable _intPropsTable;
        protected ITable _stringPropsTable;
        protected ITable _datePropsTable;
        private DBStructure _dbStructure;
        
        public override void SetUp()
        {
            IBTree._bUseOldKeys = false;

            DBStructure dbStructure = 
                new DBStructure( "", "OmniaMeaPerformanceTest", DatabaseMode.Create );

            TableStructure table = dbStructure.CreateTable( "IntProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.Integer, false );
            table.CreateIndex( "Id" );

            table = dbStructure.CreateTable( "StringProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.String, false );
            table.SetCompoundIndex( "PropValue", "PropType" );

            table = dbStructure.CreateTable( "DateProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.DateTime, false );
            table.SetCompoundIndex( "Id", "PropType" );

            dbStructure.SaveStructure();
            dbStructure.Shutdown();

            _dbStructure = new DBStructure( "", "OmniaMeaPerformanceTest" );
            _dbStructure.LoadStructure();

            IDatabase db = _dbStructure.OpenDatabase();
            _intPropsTable = db.GetTable( "IntProps" );
            _stringPropsTable = db.GetTable( "StringProps" );
            _datePropsTable = db.GetTable( "DateProps" );
        }

        public override void TearDown()
        {
            _dbStructure.Shutdown();

            string[] files = System.IO.Directory.GetFiles( ".", "*.dbUtil" );
            foreach ( string fileName in files )
            {
                System.IO.File.Delete( fileName );
            }
        }
    }

    public class InsertIntsPerfTest: DBInsertPerformanceTestBase
    {
        public override void DoTest()
        {
            Random rnd = new Random();
            for( int i=0; i<100000; i++ )
            {
                IRecord rec = _intPropsTable.NewRecord();
                rec.SetValue( 0, rnd.Next() );
                rec.SetValue( 1, i % 100 );
                rec.SetValue( 2, i );
                rec.Commit();
            }
        }
    }

    public class InsertStringsPerfTest: DBInsertPerformanceTestBase
    {
        public override void DoTest()
        {
            Random rnd = new Random();
            for( int i=0; i<100000; i++ )
            {
                IRecord rec = _stringPropsTable.NewRecord();
                rec.SetValue( 0, i );
                rec.SetValue( 1, i % 100 );
                rec.SetValue( 2, rnd.NextDouble().ToString() );
                rec.Commit();
            }
        }
    }

    public class InsertDatesPerfTest: DBInsertPerformanceTestBase
    {
        public override void DoTest()
        {
            Random rnd = new Random();
            for( int i=0; i<100000; i++ )
            {
                IRecord rec = _datePropsTable.NewRecord();
                rec.SetValue( 0, i % 1000 );
                rec.SetValue( 1, i );
                rec.SetValue( 2, new DateTime( rnd.Next() ) );
                rec.Commit();
            }
        }
    }

    /**
     * search tests
     */
    public abstract class DBSearchPerformanceTestBase: PerformanceTestBase
    {
        protected ITable _intPropsTable;
        protected ITable _stringPropsTable;
        protected ITable _datePropsTable;
        private DBStructure _dbStructure;
        
        public override void SetUp()
        {
            IBTree._bUseOldKeys = false;

            DBStructure dbStructure = 
                new DBStructure( "", "OmniaMeaPerformanceTest", DatabaseMode.Create );

            TableStructure table = dbStructure.CreateTable( "IntProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.Integer, false );
            table.CreateIndex( "Id" );

            table = dbStructure.CreateTable( "StringProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.String, false );
            table.SetCompoundIndex( "PropValue", "PropType" );

            table = dbStructure.CreateTable( "DateProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.DateTime, false );
            table.SetCompoundIndex( "PropValue", "PropType" );

            dbStructure.SaveStructure();
            dbStructure.Shutdown();

            _dbStructure = new DBStructure( "", "OmniaMeaPerformanceTest" );
            _dbStructure.LoadStructure();

            IDatabase db = _dbStructure.OpenDatabase();
            _intPropsTable = db.GetTable( "IntProps" );
            _stringPropsTable = db.GetTable( "StringProps" );
            _datePropsTable = db.GetTable( "DateProps" );

            Random rnd = new Random();
            for( int i=0; i<200000; i++ )
            {
                IRecord rec = _intPropsTable.NewRecord();
                rec.SetValue( 0, rnd.Next() );
                rec.SetValue( 1, i % 100 );
                rec.SetValue( 2, i );
                rec.Commit();
            }
            for( int i=0; i<200000; i++ )
            {
                IRecord rec = _stringPropsTable.NewRecord();
                rec.SetValue( 0, i );
                rec.SetValue( 1, i % 100 );
                rec.SetValue( 2, rnd.NextDouble().ToString() );
                rec.Commit();
            }
            for( int i=0; i<200000; i++ )
            {
                IRecord rec = _datePropsTable.NewRecord();
                rec.SetValue( 0, i % 1000 );
                rec.SetValue( 1, i );
                rec.SetValue( 2, new DateTime( (rnd.Next()) ) );
                rec.Commit();
            }
        }

        public override void TearDown()
        {
            _dbStructure.Shutdown();

            string[] files = System.IO.Directory.GetFiles( ".", "*.dbUtil" );
            foreach ( string fileName in files )
            {
                System.IO.File.Delete( fileName );
            }
        }
    }

    public class SearchIntsPerfTest: DBSearchPerformanceTestBase
    {
        public override void DoTest()
        {
            Random rnd = new Random();
            for( int i=0; i<100000; i++ )
            {
                using( _intPropsTable.CreateResultSet( 0, rnd.Next() ) ) {}
            }
        }
    }

    public class SearchStringsPerfTest: DBSearchPerformanceTestBase
    {
        public override void DoTest()
        {
            Random rnd = new Random();
            for( int i=0; i<100000; i++ )
            {
                using( _stringPropsTable.CreateResultSet( 2, rnd.NextDouble().ToString() ) ) {}
            }
        }
    }

    public class SearchDatesPerfTest: DBSearchPerformanceTestBase
    {
        public override void DoTest()
        {
            Random rnd = new Random();
            for( int i=0; i<100000; i++ )
            {
                using( _datePropsTable.CreateResultSet( 2, new DateTime( ( rnd.Next() ) ) ) ) {}
            }
        }
    }

    /**
     * deletion tests
     */
    public abstract class DBDeletePerformanceTestBase: PerformanceTestBase
    {
        protected ITable _intPropsTable;
        protected ITable _stringPropsTable;
        protected ITable _datePropsTable;
        private DBStructure _dbStructure;
        
        public override void SetUp()
        {
            IBTree._bUseOldKeys = false;

            DBStructure dbStructure = 
                new DBStructure( "", "OmniaMeaPerformanceTest", DatabaseMode.Create );

            TableStructure table = dbStructure.CreateTable( "IntProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.Integer, false );
            table.CreateIndex( "Id" );

            table = dbStructure.CreateTable( "StringProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.String, false );
            table.SetCompoundIndex( "PropValue", "PropType" );

            table = dbStructure.CreateTable( "DateProps" );
            table.CreateColumn( "Id", ColumnType.Integer, false );
            table.CreateColumn( "PropType", ColumnType.Integer, false );
            table.CreateColumn( "PropValue", ColumnType.DateTime, false );
            table.SetCompoundIndex( "PropValue", "PropType" );

            dbStructure.SaveStructure();
            dbStructure.Shutdown();

            _dbStructure = new DBStructure( "", "OmniaMeaPerformanceTest" );
            _dbStructure.LoadStructure();

            IDatabase db = _dbStructure.OpenDatabase();
            _intPropsTable = db.GetTable( "IntProps" );
            _stringPropsTable = db.GetTable( "StringProps" );
            _datePropsTable = db.GetTable( "DateProps" );

            Random rnd = new Random();
            for( int i=0; i<200000; i++ )
            {
                IRecord rec = _intPropsTable.NewRecord();
                rec.SetValue( 0, rnd.Next() );
                rec.SetValue( 1, i % 100 );
                rec.SetValue( 2, i );
                rec.Commit();
            }
            for( int i=0; i<200000; i++ )
            {
                IRecord rec = _stringPropsTable.NewRecord();
                rec.SetValue( 0, i );
                rec.SetValue( 1, i % 100 );
                rec.SetValue( 2, rnd.NextDouble().ToString() );
                rec.Commit();
            }
            for( int i=0; i<200000; i++ )
            {
                IRecord rec = _datePropsTable.NewRecord();
                rec.SetValue( 0, i % 1000 );
                rec.SetValue( 1, i );
                rec.SetValue( 2, new DateTime( rnd.Next() ) );
                rec.Commit();
            }
        }

        public override void TearDown()
        {
            _dbStructure.Shutdown();

            string[] files = System.IO.Directory.GetFiles( ".", "*.dbUtil" );
            foreach ( string fileName in files )
            {
                System.IO.File.Delete( fileName );
            }
        }
    }

    public class DeleteIntsPerfTest: DBDeletePerformanceTestBase
    {
        public override void DoTest()
        {
            Random rnd = new Random();
            for( int i=0; i<100000; i++ )
            {
                IResultSet rs = _intPropsTable.CreateResultSet( 0, rnd.Next() );
                using( rs )
                {
                    foreach( IRecord record in rs )
                    {
                        record.Delete();
                    }
                }
            }
        }
    }

    public class DeleteStringsPerfTest: DBDeletePerformanceTestBase
    {
        public override void DoTest()
        {
            Random rnd = new Random();
            for( int i=0; i<100000; i++ )
            {
                IResultSet rs = _stringPropsTable.CreateResultSet( 2, rnd.NextDouble().ToString() );
                using( rs )
                {
                    foreach( IRecord record in rs )
                    {
                        record.Delete();
                    }
                }
            }
        }
    }

    public class DeleteDatesPerfTest: DBDeletePerformanceTestBase
    {
        public override void DoTest()
        {
            Random rnd = new Random();
            for( int i=0; i<100000; i++ )
            {
                IResultSet rs = _datePropsTable.CreateResultSet( 2, new DateTime( rnd.Next() ) );
                using( rs )
                {
                    foreach( IRecord record in rs )
                    {
                        record.Delete();
                    }
                }
            }
        }
    }
}
