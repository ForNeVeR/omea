// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Database;
using NUnit.Framework;

namespace DBUtil
{
    /// <summary>
    /// Summary description for DBUtilsTest.
    /// </summary>
    ///

    public class DBTest
    {
        //private const int Id = 0;
        //private const int Name = 1;
        //private const int Age = 2;
        //private const int Birthday = 3;
        //private const int Price = 2;


        public static void RemoveDBFiles()
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles(".", "*.dbUtil");
                foreach ( string fileName in files )
                {
                    System.IO.File.Delete( fileName );
                }
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.Message );
            }
        }
        public static void CreateDB()
        {
            DBStructure database = new DBStructure( "", "MyPal", DatabaseMode.Create );
            database.Build = "Build";
            TableStructure tblPeople = database.CreateTable( "People" );
            tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
            ColumnStructure colName =
                tblPeople.CreateColumn( "Name", ColumnType.String, true );
            colName = colName;
            ColumnStructure colAge =
                tblPeople.CreateColumn( "Age", ColumnType.Integer, false );
            colAge = colAge;
            tblPeople.CreateColumn( "Birthday", ColumnType.DateTime, false );
            tblPeople.SetCompoundIndex( "Name", "Age" );

            TableStructure tblBooks = database.CreateTable( "Books" );
            tblBooks.CreateColumn( "Id", ColumnType.Integer, true );
            ColumnStructure  colBookName =
                tblBooks.CreateColumn( "Name", ColumnType.String, true );
            colBookName = colBookName;
            ColumnStructure colPrice =
                tblBooks.CreateColumn( "Price", ColumnType.Integer, false );
            colPrice = colPrice;

            TableStructure tblDate = database.CreateTable( "Date" );
            tblDate.CreateColumn( "Id", ColumnType.Integer, true );
            tblDate.CreateColumn( "Age", ColumnType.Integer, false );
            tblDate.CreateColumn( "Birthday", ColumnType.DateTime, true );
            tblDate.SetCompoundIndexWithValue( "Age", "Birthday", "Id" );

            TableStructure tblEndMarker = database.CreateTable( "EndMarker" );
            tblEndMarker.CreateColumn( "Id", ColumnType.Integer, true );
            tblEndMarker.CreateColumn( "Age", ColumnType.Integer, false );
            tblEndMarker.CreateColumn( "Name", ColumnType.String, true );
            tblEndMarker.SetCompoundIndex( "Name", "Age" );

            database.SaveStructure();
            database.Shutdown();
        }
    }

    [TestFixture]
    public class DBStructureTest
    {
        //private const int Id = 0;
        private const int Name = 1;
        private const int Age = 2;
        //private const int Birthday = 3;
        private const int Price = 2;

        [SetUp]
        public void SetUp()
        {
            DBTest.RemoveDBFiles();
        }

        [TearDown]
        public void TearDown()
        {
            DBTest.RemoveDBFiles();
        }

        [Test]
        public void SmokeTestSaveLoadDBStructure( )
        {
            DBTest.CreateDB();
        }
        [Test]
        public void GetVersionEndBuild( )
        {
            DBTest.CreateDB();
            DBStructure database = new DBStructure( "", "MyPal" );
            database.LoadVersionInfo();
            Assert.AreEqual( true, database.Build != "undefined" );
            Assert.AreEqual( true, database.Version > 0 );
        }

        [Test]
        public void SmokeTestLoadDatabase( )
        {
            DBTest.CreateDB();
            DBStructure dbStruct = new DBStructure( "", "MyPal" );

            dbStruct.LoadStructure();

            IDatabase database = dbStruct.OpenDatabase( );
            ITable people = database.GetTable( "People" );
            IRecord record = people.NewRecord();
            record.SetValue(Name, "zhu" );
            record.SetValue(Age, 30 );
            ITable books = database.GetTable( "Books" );
            record = books.NewRecord();
            record.SetValue(Name, "Algorithm" );
            record.SetValue(Price, 1000 );
            database.Shutdown();
        }
        public void RemoveDBFilesTest( )
        {
            DBTest.CreateDB();
            Assert.IsTrue( DBHelper.DatabaseExists( ".", "MyPal" ) );
            DBHelper.RemoveDBFiles( "." );
            Assert.IsTrue( !DBHelper.DatabaseExists( ".", "MyPal" ) );
        }

        [Test]
        public void TableNameMustBeUnique( )
        {
            DBStructure database = new DBStructure( ".", "Test" );
            database.CreateTable("People");
            try
            {
                database.CreateTable("People");
            }
            catch ( TableAlreadyExistsException )
            {
                //it's normal
                return;
            }
            Assert.Fail( "'People' table already exists but 'TableAlreadyExists' was not thrown" );
        }

        [Test]
        public void ColumnNameMustBeUnique( )
        {
            DBStructure database = new DBStructure( ".", "Test" );
            TableStructure table = database.CreateTable("People");
            table.CreateColumn( "Name", ColumnType.String, true );
            try
            {
                table.CreateColumn( "Name", ColumnType.String, true );
            }
            catch ( ColumnAlreadyExistsException )
            {
                //it's normal
                return;
            }
            Assert.Fail( "'Name' column already exists but 'ColumnAlreadyExists' was not thrown" );
        }

    }

    [TestFixture]
    public class SmokeTests
    {
        //private const int Id = 0;
        private const int Name = 1;
        //private const int Age = 2;
        //private const int Birthday = 3;
        //private const int Price = 2;

        [SetUp]
        public void SetUp()
        {
            DBTest.RemoveDBFiles();
        }

        [TearDown]
        public void TearDown()
        {
            DBTest.RemoveDBFiles();
        }
        [Test]
        public void SmokeTestForSetCacheSize( )
        {
            DBStructure database = new DBStructure( "", "MyPal" );
            TableStructure tblPeople = database.CreateTable( "People" );

            tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
            tblPeople.CreateColumn( "Name", ColumnType.String, true );

            database.SaveStructure();
            IDatabase m_database = database.OpenDatabase( );

            ITable testTable = m_database.GetTable("People");
            testTable = testTable;
            m_database.Shutdown();
        }

        [Test]
        public void CheckSaveLoadIndex( )
        {
            DBStructure database = new DBStructure( "", "MyPal" );
            TableStructure tblPeople = database.CreateTable( "People" );

            tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
            tblPeople.CreateColumn( "Name", ColumnType.String, true );

            database.SaveStructure();
            IDatabase m_database = database.OpenDatabase( );

            ITable testTable = m_database.GetTable("People");
            for ( int i = 0; i < 1000; i++ )
            {
                IRecord record = testTable.NewRecord();
                record.SetValue( Name, ( 500 - i ).ToString() );
                record.Commit();
                Assert.IsTrue( testTable.Count == (i + 1) );
            }
            m_database.Shutdown();
        }
    }

    [TestFixture]
    public class CheckIndexingOnFiledsTest
    {
        //private const int Id = 0;
        private const int Name = 1;
        //private const int Age = 2;
        //private const int Birthday = 3;
        //private const int Price = 2;

        IDatabase m_database;
        ITable m_testTable;
        [SetUp]
        public void SetUp()
        {
            try
            {
                DBTest.RemoveDBFiles();
                DBStructure database = new DBStructure( "", "MyPal" );
                TableStructure tblPeople = database.CreateTable( "People" );
                tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
                tblPeople.CreateColumn( "Name", ColumnType.String, true );

                database.SaveStructure();
                m_database = database.OpenDatabase( );

                m_testTable = m_database.GetTable("People");
                for ( int i = 0; i < 10; i++ )
                {
                    IRecord record = m_testTable.NewRecord();
                    record.SetValue( Name, ( 500 - i ).ToString() );
                    record.Commit();
                    Assert.AreEqual( (i+1), m_testTable.Count );
                }
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
        public void SortOnFiled( )
        {

            IResultSet resultSet = m_testTable.CreateResultSet( Name );
            int count = 0;
            foreach ( IRecord record in resultSet )
            {
                string name = record.GetStringValue( Name );
                name = name;
                count++;
            }
            Assert.AreEqual( 10, count );
            resultSet.Dispose();

            return;
        }
    }
}
