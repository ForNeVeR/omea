// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Database;
using NUnit.Framework;
namespace DBUtil
{
    [TestFixture]
    public class IndexWithValuesTests
    {
        private const int Id = 0;
        private const int Type = 1;
        private const int Number = 2;

        IDatabase m_database;
        ITable _testTable;
        [SetUp]
        public void SetUp()
        {
            try
            {
                DBTest.RemoveDBFiles();
                DBStructure database = new DBStructure( "", "MyPal", DatabaseMode.Create );
                TableStructure tblPeople = database.CreateTable( "People" );
                tblPeople.CreateColumn( "Id", ColumnType.Integer, false );
                tblPeople.CreateColumn( "Type", ColumnType.Integer, false );
                tblPeople.CreateColumn( "Number", ColumnType.Integer, false );
                tblPeople.SetCompoundIndexWithValue( "Type", "Id", "Number" );
                tblPeople.SetCompoundIndex( "Id", "Type" );
                database.SaveStructure();
                database.Shutdown();
                database = new DBStructure( "", "MyPal" );
                database.LoadStructure(  );
                m_database = database.Database;

            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.Message );
            }

        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                m_database.Shutdown();
                DBTest.RemoveDBFiles();
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.Message );
            }
        }
        [Test]
        public void InsertingAndDeleting( )
        {
            _testTable = m_database.GetTable("People");
            for ( int i = 0; i < 10; i++ )
            {
                IRecord record = _testTable.NewRecord();
                Assert.AreEqual( i, record.GetID() );
                record.SetValue( Type, i );
                record.SetValue( Number, i*100 );
                record.Commit();
                Assert.AreEqual( 1, _testTable.Count );
                record.Delete();
                Assert.AreEqual( 0, _testTable.Count );
            }
        }
        [Test]
        public void Search( )
        {
            _testTable = m_database.GetTable("People");
            IRecord record = null;
            for ( int i = 0; i < 10; i++ )
            {
                record = _testTable.NewRecord();
                Assert.AreEqual( i, record.GetID() );
                record.SetValue( Id, i );
                record.SetValue( Type, i );
                record.SetValue( Number, i*100 );
                record.Commit();
                Assert.AreEqual( i + 1, _testTable.Count );
            }
            record = _testTable.GetRecordByEqual( 1, 2 );
            Assert.AreEqual( 2, record.GetIntValue(1) );
            Assert.AreEqual( 200, record.GetIntValue(2) );
            ICountedResultSet resultSet = _testTable.CreateResultSet( 1, 2, 0, 2, false );
            Assert.AreEqual( 1, resultSet.Count );
            record = resultSet[0];
            Assert.AreEqual( 2, record.GetIntValue(1) );
            Assert.AreEqual( 200, record.GetIntValue(2) );

            resultSet.Dispose();
            m_database.Shutdown();
            DBStructure database = new DBStructure( "", "MyPal" );
            database.LoadStructure();
            m_database = database.OpenDatabase();
            _testTable = m_database.GetTable("People");
            record = _testTable.GetRecordByEqual( 1, 2 );
            Assert.AreEqual( 2, record.GetIntValue(1) );
            Assert.AreEqual( 200, record.GetIntValue(2) );
        }

        [Test]
        public void GetAllKeys()
        {
            _testTable = m_database.GetTable("People");
            IRecord record = null;
            for ( int i = 0; i < 10; i++ )
            {
                record = _testTable.NewRecord();
                Assert.AreEqual( i, record.GetID() );
                record.SetValue( Id, i );
                record.SetValue( Type, i );
                record.SetValue( Number, i*100 );
                record.Commit();
                Assert.AreEqual( i + 1, _testTable.Count );
            }

            ICountedResultSet rs = _testTable.CreateResultSet( 1 );
            Assert.AreEqual( 10, rs.Count );
            for( int i=0; i<rs.Count; i++ )
            {
                IRecord rec = rs [i];
                Assert.AreEqual( i, rec.GetIntValue( 0 ) );
                Assert.AreEqual( i, rec.GetIntValue( 1 ) );
                Assert.AreEqual( i*100, rec.GetIntValue( 2 ) );
            }
        }


        [Test]
        public void DropCompoundAndCreateWithValue( )
        {
            _testTable = m_database.GetTable("People");
            IRecord record = null;
            for ( int i = 0; i < 10; i++ )
            {
                record = _testTable.NewRecord();
                Assert.AreEqual( i, record.GetID() );
                record.SetValue( Id, i );
                record.SetValue( Type, i );
                record.SetValue( Number, i*100 );
                record.Commit();
                Assert.AreEqual( i + 1, _testTable.Count );
            }
            record = _testTable.GetRecordByEqual( 0, 2 );
            Assert.AreEqual( 2, record.GetIntValue(1) );
            Assert.AreEqual( 200, record.GetIntValue(2) );
            ICountedResultSet resultSet = _testTable.CreateResultSet( 0, 2, 1, 2, false );
            Assert.AreEqual( 1, resultSet.Count );
            record = resultSet[0];
            Assert.AreEqual( 2, record.GetIntValue(1) );
            Assert.AreEqual( 200, record.GetIntValue(2) );
            resultSet.Dispose();

            m_database.Shutdown();

            DBStructure database = new DBStructure( "", "MyPal" );
            database.LoadStructure();

            m_database = database.Database;
            _testTable = m_database.GetTable("People");

            record = _testTable.GetRecordByEqual( 0, 2 );
            Assert.AreEqual( 2, record.GetIntValue(1) );
            Assert.AreEqual( 200, record.GetIntValue(2) );

            m_database.Shutdown();
            database = new DBStructure( "", "MyPal" );
            database.LoadStructure();

            m_database = database.Database;
            TableStructure tblStruct = database.GetTable( "People" );
            tblStruct.DropCompoundIndex( "Id", "Type" );
            tblStruct.SetCompoundIndexWithValue( "Id", "Type", "Number" );
            _testTable = m_database.GetTable("People");
            database.SaveStructure();
            m_database.Shutdown();
            database.RebuildIndexes( true );

            database = new DBStructure( "", "MyPal" );
            database.LoadStructure();
            m_database = database.Database;
            _testTable = m_database.GetTable("People");
            Assert.AreEqual( 10, _testTable.Count );
            record = _testTable.GetRecordByEqual( 0, 2 );
            Assert.AreEqual( 2, record.GetIntValue(1) );
            Assert.AreEqual( 200, record.GetIntValue(2) );
        }
    }
}
