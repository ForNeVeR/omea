// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Database;
using NUnit.Framework;

namespace DBUtil
{
    [TestFixture]
    public class MovingToTrieTest
    {
        //private const int Id = 0;
        private const int Name = 1;
        //private const int Age = 2;
        //private const int Type = 3;
        //private const int DateTime = 4;

        IDatabase m_database;
        ITable m_testTable;
        [SetUp]
        public void SetUp()
        {
            try
            {
                DBTest.RemoveDBFiles();
                DBStructure database = new DBStructure( "", "MyPal", DatabaseMode.Create );
                TableStructure tblPeople = database.CreateTable( "People" );
                tblPeople.CreateColumn( "Id", ColumnType.Integer, true );
                ColumnStructure colName =
                    tblPeople.CreateColumn( "Name", ColumnType.String, false );
                colName = colName;
                tblPeople.CreateColumn( "Age", ColumnType.Integer, false );
                tblPeople.CreateColumn( "Type", ColumnType.Integer, true );
                tblPeople.CreateColumn( "DateTime", ColumnType.DateTime, true );
                tblPeople.SetCompoundIndex( "Id", "Name" );
                tblPeople.SetCompoundIndex( "Name", "Type" );

                database.SaveStructure();
                database.Shutdown();
                database = new DBStructure( "", "MyPal" );
                database.LoadStructure( );
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
        public void Inserting( )
        {
            m_testTable = m_database.GetTable("People");
            for ( int i = 0; i < 10; i++ )
            {
                IRecord record = m_testTable.NewRecord();
                Assert.AreEqual( i, record.GetID() );
                record.SetValue( Name, ( 500 - i ).ToString() );
                record.Commit();
                Assert.AreEqual( (i+1), m_testTable.Count );
            }
        }
        [Test]
        public void Search( )
        {
            m_testTable = m_database.GetTable("People");
            for ( int i = 0; i < 10; i++ )
            {
                IRecord record = m_testTable.NewRecord();
                Assert.AreEqual( i, record.GetID() );
                record.SetValue( Name, ( 500 - i ).ToString() );
                record.Commit();
                Assert.AreEqual( (i+1), m_testTable.Count );
            }
            IResultSet resultSet = m_testTable.CreateModifiableResultSet( Name, "300" );
            int count = 0;
            foreach ( IRecord record in resultSet )
            {
                string name = record.GetStringValue( Name );
                name = name;
                count++;
            }
            Assert.AreEqual( 0, count );
            resultSet.Dispose();
        }
    }
}
