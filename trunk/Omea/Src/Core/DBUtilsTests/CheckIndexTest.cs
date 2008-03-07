/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.Database;
using NUnit.Framework;

namespace DBUtil
{
	[TestFixture]
	public class CheckIndexTest
	{
        private IDatabase m_database;
		[SetUp]
		public void SetUp()
		{
			DBTest.RemoveDBFiles();
            DBStructure database = new DBStructure( "", "MyPal", DatabaseMode.Create );
            TableStructure tblPeople = database.CreateTable( "People" );
            tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
            tblPeople.CreateColumn( "Name", ColumnType.String, false );
            tblPeople.CreateColumn( "Age", ColumnType.Integer, false );
            tblPeople.SetCompoundIndex( "Name", "Age" );

            database.SaveStructure();
            database.Shutdown();
            database = new DBStructure( "", "MyPal" );
            database.LoadStructure( );
            m_database = database.Database;
        }

		[TearDown]
		public void TearDown()
		{
            m_database.Shutdown();
            DBTest.RemoveDBFiles();
		}
        [Test]
        public void CheckGettingNameFromITable( )
        {
            ITable people = m_database.GetTable( "People" );
            Assert.AreEqual( "People", people.Name );
        }

        [Test]
        public void SelectCompound( )
        {
            DBStructure dbStructure = new DBStructure( "", "Test", DatabaseMode.Create );
            TableStructure tblPeople = dbStructure.CreateTable( "People" );
            tblPeople.CreateColumn( "Id", ColumnType.Integer, false );
            tblPeople.CreateColumn( "Name", ColumnType.String, false );
            tblPeople.CreateColumn( "Age", ColumnType.Integer, false );
            tblPeople.SetCompoundIndex( "Id", "Age" );
            tblPeople.SetCompoundIndex( "Name", "Age" );

            dbStructure.SaveStructure();
            dbStructure.Shutdown();
            dbStructure = new DBStructure( "", "Test", DatabaseMode.Create );
            dbStructure.LoadStructure( );
            IDatabase database = dbStructure.Database;
            ITable people = database.GetTable( "People" );
            IRecord record = people.NewRecord();
            record.SetValue( 2, 777 );
            record.Commit();
            ICountedResultSet rs = people.CreateModifiableResultSet( 0, 0 );
            Assert.AreEqual( 1, rs.Count );
            IRecord rec = rs[0];
            Assert.AreEqual( 0, rec.GetIntValue(0) );
            Assert.AreEqual( 777, rec.GetIntValue(2) );


            for ( int i = 0; i < 100; i++ )
            {
                record = people.NewRecord();
                record.SetValue( 1, "Serg" );
                record.SetValue( 2, i );
                record.Commit();
            }
            rs.Dispose();

            rs = people.CreateResultSet( 1, "Serg", 2, 31, false );
            Assert.AreEqual( 1, rs.Count );
            rs.Dispose();
            rs = people.CreateResultSet( 1, "Serg", 2, 100, false );
            Assert.AreEqual( 0, rs.Count );
            rs.Dispose();

            rs = people.CreateResultSetForRange( 1, "Serg", 2, 10, 50 );
            Assert.AreEqual( 41, rs.Count );
            rs.Dispose();

            database.Shutdown();
        }

        [Test][ExpectedException(typeof(ColumnDoesNotExistException))]
        public void CheckCreateIndexIfColumnDoesNotExist( )
        {
            DBStructure dbStructure = new DBStructure( "", "CheckCreateIndexIfColumnDoesNotExist" );
            TableStructure tblPeople = dbStructure.CreateTable( "People" );
            tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
            tblPeople.CreateColumn( "Name", ColumnType.String, true );
            tblPeople.CreateColumn( "Age", ColumnType.Integer, false );
            tblPeople.CreateIndex( "Name1" );
        }
        [Test][ExpectedException(typeof(IndexAlreadyExistsException))]
        public void CheckCreateIndexIfIndexAlreadyExists( )
        {
            DBStructure dbStructure = new DBStructure( "", "CheckCreateIndexIfIndexAlreadyExists" );
            TableStructure tblPeople = dbStructure.CreateTable( "People" );
            tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
            tblPeople.CreateColumn( "Name", ColumnType.String, true );
            tblPeople.CreateColumn( "Age", ColumnType.Integer, false );
            tblPeople.CreateIndex( "Name" );
        }

        [Test][ExpectedException(typeof(ColumnHasNoIndexException))]
        public void CheckDropIndexIfIndexDoesNotExist( )
        {
            DBStructure dbStructure = new DBStructure( "", "CheckDropIndexIfIndexDoesNotExist" );
            TableStructure tblPeople = dbStructure.CreateTable( "People" );
            tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
            tblPeople.CreateColumn( "Name", ColumnType.String, false );
            tblPeople.CreateColumn( "Age", ColumnType.Integer, false );
            tblPeople.DropIndex( "Name" );
        }

        [Test][ExpectedException(typeof(ColumnDoesNotExistException))]
        public void CheckDropIndexIfColumnDoesNotExist( )
        {
            DBStructure dbStructure = new DBStructure( "", "CheckDropIndexIfColumnDoesNotExist" );
            TableStructure tblPeople = dbStructure.CreateTable( "People" );
            tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
            tblPeople.CreateColumn( "Name", ColumnType.String, false );
            tblPeople.CreateColumn( "Age", ColumnType.Integer, false );
            tblPeople.DropIndex( "Name1" );
        }

        [Test][ExpectedException(typeof(ColumnDoesNotExistException))]
        public void CheckTryCreateCompoundIndexForNotExistedColumn( )
        {
            DBStructure dbStructure = new DBStructure( "", "CheckTryCreateCompoundIndexForNotExistedColumn" );
            TableStructure tblPeople = dbStructure.CreateTable( "People" );
            tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
            tblPeople.CreateColumn( "Name", ColumnType.String, true );
            tblPeople.CreateColumn( "Age", ColumnType.Integer, true );
            tblPeople.SetCompoundIndex( "Name", "Age1" );
        }

        [Test][ExpectedException(typeof(IndexAlreadyExistsException))]
        public void CheckTryCreateSameCompoundIndex( )
        {
            DBStructure dbStructure = null;
            try
            {
                dbStructure = new DBStructure( "", "AnotherDB" );
                TableStructure tblPeople = dbStructure.CreateTable( "People" );
                tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
                tblPeople.CreateColumn( "Name", ColumnType.String, true );
                tblPeople.CreateColumn( "Age", ColumnType.Integer, true );
                tblPeople.SetCompoundIndex( "Name", "Age" );
                tblPeople.SetCompoundIndex( "Name", "Age" );
            }
            finally
            {
                if ( dbStructure != null )
                {
                    dbStructure.Shutdown();
                }
            }
        }
        public void CheckSaveLoadIndex( )
        {
			ITable testTable = m_database.GetTable("People");
			for ( int i = 0; i < 1000; i++ )
			{
				IRecord record = testTable.NewRecord();
				record.SetValue( 1, i.ToString() );
				record.Commit();
				Assert.IsTrue( testTable.Count == (i + 1) );
			}
			m_database.Shutdown();
			DBStructure database = new DBStructure( "", "MyPal" );
			database.LoadStructure();

			m_database = database.OpenDatabase( );
			testTable = m_database.GetTable("People");
			Assert.IsTrue( testTable.Count == 1000 );

			for ( int i = 1000; i < 2000; i++ )
			{
				IRecord record = testTable.NewRecord();
				record.SetValue( 1, i.ToString() );
				record.Commit();
				Assert.IsTrue( testTable.Count == (i + 1) );
			}
		}
        [Test]
        public void CheckNextID( )
        {
            ITable testTable = m_database.GetTable("People");
            for ( int i = 0; i < 1000; i++ )
            {
                IRecord record = testTable.NewRecord();
                record.SetValue( 1, i.ToString() );
                record.Commit();
                Assert.AreEqual( i, record.GetID() );
                Assert.IsTrue( testTable.Count == (i + 1) );
            }
            for ( int i = 1000; i < 2000; i++ )
            {
                Assert.IsTrue( testTable.NextID() == i );
            }
            for ( int i = 0; i < 1000; i++ )
            {
                ICountedResultSet resultSet = testTable.CreateModifiableResultSet( 1, i.ToString() );
                resultSet[0].Delete();
                resultSet.Dispose();
            }
            m_database.Shutdown();
            DBStructure database = new DBStructure( "", "MyPal" );
            database.LoadStructure();

            m_database = database.OpenDatabase( );
            testTable = m_database.GetTable("People");
            Assert.AreEqual( 0, testTable.Count );

            for ( int i = 2000; i < 3000; i++ )
            {
                IRecord record = testTable.NewRecord();
                record.SetValue( 1, i.ToString() );
                record.Commit();
                Assert.AreEqual( i, record.GetID() );
                Assert.AreEqual( (i - 1999), testTable.Count );
            }
        }

        [Test]
        public void ComputeWastedSpace( )
        {
            ITable testTable = m_database.GetTable("People");
            for ( int i = 0; i < 1000; i++ )
            {
                IRecord record = testTable.NewRecord();
                record.SetValue( 1, i.ToString() );
                record.Commit();
            }
            RecordsCounts recCounts = testTable.ComputeWastedSpace();
            Assert.AreEqual( 1000, recCounts.NormalRecordCount );
            Assert.AreEqual( 1000, recCounts.TotalRecordCount );
            ICountedResultSet resultSet = testTable.CreateModifiableResultSet( 1, "5" );
            foreach ( IRecord record in resultSet )
            {
                record.Delete();
            }
            resultSet.Dispose();
            recCounts = testTable.ComputeWastedSpace();
            Assert.AreEqual( 999, recCounts.NormalRecordCount );
            Assert.AreEqual( 1000, recCounts.TotalRecordCount );
            m_database.Shutdown();
            DBStructure database = new DBStructure( "", "MyPal" );
            database.LoadStructure();

            m_database = database.OpenDatabase( );
            testTable = m_database.GetTable("People");

            for ( int i = 1000; i < 2000; i++ )
            {
                IRecord record = testTable.NewRecord();
                record.SetValue( 1, i.ToString() );
                record.Commit();
            }
            recCounts = testTable.ComputeWastedSpace();
            Assert.AreEqual( 1999, recCounts.NormalRecordCount );
            Assert.AreEqual( 2000, recCounts.TotalRecordCount );
            for ( int i = 500; i < 1500; i++ )
            {
                ICountedResultSet resultSet1 = testTable.CreateModifiableResultSet( 1, i.ToString() );
                foreach ( IRecord record in resultSet1 )
                {
                    record.Delete();
                }
                resultSet1.Dispose();
            }
            recCounts = testTable.ComputeWastedSpace();
            Assert.AreEqual( 999, recCounts.NormalRecordCount );
            Assert.AreEqual( 1001, recCounts.TotalRecordCount - recCounts.NormalRecordCount );
            testTable.SortedColumn = -1;
            testTable.Defragment();
            testTable.SortedColumn = 1;
            testTable.Defragment();
            testTable.SortedColumn = 0;
            testTable.Defragment();
            recCounts = testTable.ComputeWastedSpace();
            Assert.AreEqual( 999, recCounts.NormalRecordCount );
            Assert.AreEqual( 999, recCounts.TotalRecordCount );
            Assert.AreEqual( 0, recCounts.TotalRecordCount - recCounts.NormalRecordCount );
        }

        [Test]
        public void CheckSelectByOneColumnWhenCompoundIndexexistsOnlyIndex( )
        {
            ITable table = m_database.GetTable( "People" );

            IRecord record = table.NewRecord();
            record.SetValue( 1, "Sergey" );
            record.SetValue( 2, 31 );
            record.Commit();

            record = table.NewRecord();
            record.SetValue( 1, "Sergey" );
            record.SetValue( 2, 15 );
            record.Commit();

            record = table.NewRecord();
            record.SetValue( 1, "Sergey" );
            record.SetValue( 2, 25 );
            record.Commit();

            record = table.NewRecord();
            record.SetValue( 1, "Misha" );
            record.SetValue( 2, 15 );
            record.Commit();

            record = table.NewRecord();
            record.SetValue( 1, "Null" );
            record.Commit();

            ICountedResultSet resultSet = table.CreateModifiableResultSet( 1, "Sergey" );
            Assert.AreEqual( 3, resultSet.Count );
            resultSet.Dispose();
            resultSet = table.CreateModifiableResultSet( 1, "Misha" );
            Assert.AreEqual( 1, resultSet.Count );
            resultSet.Dispose();
            resultSet = table.CreateModifiableResultSet( 1, "" );
            Assert.AreEqual( 0, resultSet.Count );
            resultSet.Dispose();
            resultSet = table.CreateModifiableResultSet( 1, "Zorro" );
            Assert.AreEqual( 0, resultSet.Count );
            resultSet.Dispose();
            resultSet = table.CreateModifiableResultSet( 1, "Null" );
            Assert.AreEqual( 1, resultSet.Count );
            resultSet.Dispose();
        }
    }

}