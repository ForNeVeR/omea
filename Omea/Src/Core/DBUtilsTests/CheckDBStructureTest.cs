// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.Omea.Database;
using NUnit.Framework;

namespace DBUtil
{
    [TestFixture]
    public class CheckDBStructureTest
    {
        private IDatabase _database = null;
        public CheckDBStructureTest() {}

        [SetUp]
        public void SetUp()
        {
            try
            {
                DBTest.RemoveDBFiles();
                DBTest.CreateDB();
                DBStructure dbStruct = new DBStructure( "", "MyPal" );
                dbStruct.LoadStructure();
                _database = dbStruct.OpenDatabase( );
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.ToString() );
            }

        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _database.Shutdown();
                DBTest.RemoveDBFiles();
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.ToString() );
            }
        }

        [Test][ExpectedException(typeof(TableDoesNotExistException))]
        public void WrongTableNameTest( )
        {
            ITable table = _database.GetTable( "Mails1" );
            table = table;
        }
        public void CheckUnlockDatabaseWhenSaveStructureTest( )
        {
            DBStructure dbStruct = new DBStructure( "", "Test" );

            TableStructure tableStruct = dbStruct.CreateTable( "SomeTable" );

            tableStruct.CreateColumn( "SomeString", ColumnType.String, false );
            tableStruct.CreateColumn( "SomeInt", ColumnType.Integer, false );
            tableStruct.CreateColumn( "SomeDate", ColumnType.DateTime, false );
            dbStruct.SaveStructure();

            dbStruct = new DBStructure( "", "Test" );
            dbStruct.LoadStructure();

            IDatabase database = dbStruct.OpenDatabase( );
            database.Shutdown();
        }

        [Test]
        public void GetColumnInfosTest( )
        {

            ITable people = _database.GetTable( "People" );
            ArrayList columnInfos = people.GetColumnInfos();
            Assert.AreEqual( 4, columnInfos.Count );
        }
    }

}
