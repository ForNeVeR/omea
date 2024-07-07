// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using JetBrains.Omea.Database;
using NUnit.Framework;
using JetBrains.Omea.Containers;

namespace DBUtil
{
    [TestFixture]
    public class CrashProofTest
    {
        private IDatabase m_database = null;
        public CrashProofTest() {}

        [SetUp]
        public void SetUp()
        {
            try
            {
                DBTest.RemoveDBFiles();
                DBTest.CreateDB();
                DBStructure dbStruct = new DBStructure( "", "MyPal" );
                dbStruct.LoadStructure();
                m_database = dbStruct.OpenDatabase( );
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
                if ( m_database != null )
                {
                    m_database.Shutdown();
                }
                DBTest.RemoveDBFiles();
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.Message );
            }
        }

        [Test][ExpectedException(typeof(BackwardIncompatibility))]
        public void TestBackwardIncompatability( )
        {
            m_database.Shutdown();

            FileStream file = File.Open( "MyPal.database.struct.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 3, SeekOrigin.Begin );
            byte[] bytes = new byte[1];
            bytes[0] = 24;
            file.Write( bytes, 0, 1 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            try
            {
                dbStruct.LoadStructure();
            }
            finally
            {
                dbStruct.Shutdown();
                m_database = null;
            }
        }
        [Test]
        public void TestBackwardIncompatabilityCheckVersionAndEndMark( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, "zhu" );
            record.Commit();
            m_database.Shutdown();

            FileStream file = File.Open( "MyPal.database.struct.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 3, SeekOrigin.Begin );
            byte[] bytes = new byte[1];
            bytes[0] = 21;
            file.Write( bytes, 0, 1 );
            file.Flush();
            file.Close();


            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            Assert.AreEqual( false, dbStruct.IsDatabaseCorrect() );

            if ( !dbStruct.IsDatabaseCorrect() )
            {
                dbStruct.RebuildIndexes();
            }
            m_database = dbStruct.OpenDatabase();
            testTable = m_database.GetTable("People");
            IResultSet resultSet = testTable.CreateResultSet( 0, 0 );
            Assert.AreEqual( 1, testTable.Count );
            int count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu", rec.GetStringValue( 1 ) );
            }
            Assert.AreEqual( 1, count );
        }
        [Test]
        public void DirtyFlag( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, "zhu" );
            record.Commit();
            Assert.AreEqual( true, testTable.Dirty );
            m_database.Flush();
            testTable = m_database.GetTable("People");
            Assert.AreEqual( false, testTable.Dirty );
        }
        [Test][ExpectedException(typeof(BadIndexesException))]
        public void TestReadingOutOfRange( )
        {
            ITable testTable = m_database.GetTable("People");
            Tests.ReadRecordFromOffset( testTable, -1 );
        }
        [Test][ExpectedException(typeof(BadIndexesException))]
        public void TestReadingOutOfRange2( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.Commit();
            Tests.ReadRecordFromOffset( testTable, 18 );
        }
        [Test][ExpectedException(typeof(BadIndexesException))]
        public void TestReadingOutOfRange3( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.Commit();
            Tests.ReadRecordFromOffset( testTable, 26 );
        }
        [Test]
        public void TestReadingFromOffset( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.Commit();
            Tests.ReadRecordFromOffset( testTable, 0 );
        }
        [Test][ExpectedException(typeof(BadIndexesException))]
        public void TestDeleteRecordOutOfRange( )
        {
            ITable testTable = m_database.GetTable("People");
            Tests.DeleteRecord( testTable, -1, new object[4] );
        }
        [Test][ExpectedException(typeof(BadIndexesException))]
        public void TestDeleteRecordOutOfRange2( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.Commit();
            object[] fields = new object[4];
            fields[0] = 0;
            fields[1] = string.Empty;
            fields[2] = 0;
            fields[3] = DateTime.MinValue;
            Tests.DeleteRecord( testTable, 19, fields );
        }

        [Test][ExpectedException(typeof(BadIndexesException))]
        public void TestDeleteRecordOutOfRange3( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.Commit();
            Tests.DeleteRecord( testTable, 27, new object[4] );
        }
        [Test]
        public void TestDateTimeAutoRecovery( )
        {
            ITable testTable = m_database.GetTable("Date");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, 32 );
            DateTime dt = new DateTime( 1972, 7, 26 );
            long ticks = dt.Ticks;
            ticks = ticks;
            record.SetValue( 2, dt );
            record.Commit();
            m_database.Shutdown();
            FileStream file = File.Open( "MyPal.Date.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 9, SeekOrigin.Begin );
            byte[] bytes = new byte[8];
            for ( int i = 0; i < 8; ++i )
            {
                bytes[i] = 255;
            }
            file.Write( bytes, 0, 8 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("Date");
            IResultSet resultSet = testTable.CreateResultSet( 1, 32 );
            testTable.Defragment();
            m_database.Shutdown();
            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            dbStruct.RebuildIndexes( true );

            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("Date");
            Assert.AreEqual( 1, testTable.Count );
            resultSet = testTable.CreateResultSet( 1, 32, 2, DateTime.MinValue, true );
            int count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( DateTime.MinValue, rec.GetDateTimeValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
        }
        [Test]
        public void TestDateTimeQuery( )
        {
            ITable testTable = m_database.GetTable("Date");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, 32 );
            DateTime dt = new DateTime( 1972, 7, 26 );
            long ticks = dt.Ticks;
            ticks = ticks;
            record.SetValue( 2, dt );
            record.Commit();
            IResultSet resultSet = testTable.CreateResultSet( 2, dt );
            try
            {
                foreach ( IRecord rec in resultSet ){ rec.Equals( null ); }
            }
            catch ( System.Exception )
            {
                return;
            }
            Assert.Fail( "Index for DateTime is not supported. It is necessary to review autorecovery for DateTime" );
        }
        [Test][ExpectedException(typeof(BadIndexesException))]
        public void TestThrowBadIndexesWhenCorruptedRecordMarker( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, "zhu" );
            record.Commit();
            m_database.Shutdown();
            FileStream file = File.Open( "MyPal.People.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 0, SeekOrigin.Begin );
            byte[] bytes = new byte[1];
            bytes[0] = 1;
            file.Write( bytes, 0, 1 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("People");
            IResultSet resultSet = testTable.CreateResultSet( 1, "zhu" );
            foreach ( IRecord rec in resultSet )
            {
                rec.Equals( null );
            }
        }
        [Test][ExpectedException(typeof(BadIndexesException))]
        public void TestThrowBadIndexesWhenCorruptedRecordMarker2( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, "zhu" );
            record.Commit();
            m_database.Shutdown();
            FileStream file = File.Open( "MyPal.People.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 0, SeekOrigin.Begin );
            byte[] bytes = new byte[1];
            bytes[0] = 255;
            file.Write( bytes, 0, 1 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("People");
            IResultSet resultSet = testTable.CreateResultSet( 1, "zhu" );
            foreach ( IRecord rec in resultSet )
            {
                rec.Equals( null );
            }
        }
        [Test]
        public void TestPaddingForTableLength( )
        {
            ITable testTable = m_database.GetTable("Date");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, 32 );
            record.SetValue( 2, new DateTime( 1972, 7, 26 ) );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 1, 33 );
            record.SetValue( 2, new DateTime( 1972, 7, 26 ) );
            record.Commit();
            IResultSet resultSet = testTable.CreateResultSet( 0, 1 );
            foreach ( IRecord rec in resultSet )
            {
                Assert.AreEqual( new DateTime( 1972, 7, 26 ), rec.GetDateTimeValue( 2 ) );
            }
            m_database.Shutdown();
            FileStream file = File.Open( "MyPal.Date.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.SetLength( 26 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("Date");
            resultSet = testTable.CreateResultSet( 0, 1 );
            foreach ( IRecord rec in resultSet )
            {
                Assert.AreEqual( DateTime.MinValue, rec.GetDateTimeValue( 2 ) );
            }
        }
        [Test]
        public void TestCorruptedRecordMarker( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, "zhu" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 1, "zhu" );
            record.Commit();
            Assert.AreEqual( 2, testTable.Count );
            m_database.Shutdown();
            FileStream file = File.Open( "MyPal.People.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 0, SeekOrigin.Begin );
            byte[] bytes = new byte[1];
            bytes[0] = 255;
            file.Write( bytes, 0, 1 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("People");
            IResultSet resultSet = testTable.CreateResultSet( 1, "zhu" );
            bool wasException = false;
            try
            {
                foreach ( IRecord rec in resultSet )
                {
                    rec.Equals( null );
                }
            }
            catch ( BadIndexesException )
            {
                wasException = true;
            }
            Assert.AreEqual( true, wasException );
            m_database.Shutdown();
            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            dbStruct.RebuildIndexes( true );
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("People");
            Assert.AreEqual( 1, testTable.Count );
        }
        [Test]
        public void TestStringAutorecovery( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, "zhu" );
            record.Commit();
            m_database.Shutdown();
            FileStream file = File.Open( "MyPal.People.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 0, SeekOrigin.Begin );
            byte[] bytes = new byte[1];
            bytes[0] = 1;
            file.Write( bytes, 0, 1 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("People");
            IResultSet resultSet = testTable.CreateResultSet( 1, "zhu" );
            try
            {
                foreach ( IRecord rec in resultSet )
                {
                    rec.Equals( null );
                }
            }
            catch ( BadIndexesException )
            {}
            m_database.Shutdown();
            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            dbStruct.RebuildIndexes( true );
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("People");
            Assert.AreEqual( 0, testTable.Count );
        }
        [Test]
        public void TestStringAutorecovery_corruptedCount( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, "zhu" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 1, "zhu1" );
            record.Commit();
            m_database.Shutdown();
            FileStream file = File.Open( "MyPal.People.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 5, SeekOrigin.Begin );
            byte[] bytes = new byte[1];
            bytes[0] = 255;
            file.Write( bytes, 0, 1 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("People");
            IResultSet resultSet = testTable.CreateResultSet( 1, "zhu" );
            try
            {
                foreach ( IRecord rec in resultSet )
                {
                    rec.Equals( null );
                }
            }
            catch ( BadIndexesException )
            {}
            m_database.Shutdown();
            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            dbStruct.RebuildIndexes( true );
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("People");
            Assert.AreEqual( 1, testTable.Count );
            resultSet = testTable.CreateResultSet( 1, "zhu1" );
            int count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu1", rec.GetStringValue( 1 ) );
            }
            Assert.AreEqual( 1, count );
            resultSet = testTable.CreateResultSet( 1, "zhu" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                rec.Equals( null );
                //Assert.AreEqual( "zhu", rec.GetStringValue( 1 ) );
            }
            Assert.AreEqual( 0, count );
        }
        [Test]
        public void TestStringAutorecovery_corruptedEndMarker( )
        {
            ITable testTable = m_database.GetTable("EndMarker");
            IRecord record = testTable.NewRecord();
            record.SetValue( 2, "zhu" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 2, "zhu1" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 2, "zhu2" );
            record.Commit();
            m_database.Shutdown();

            FileStream file = File.Open( "MyPal.EndMarker.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 16, SeekOrigin.Begin );
            byte[] bytes = new byte[1];
            bytes[0] = 255;
            file.Write( bytes, 0, 1 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("EndMarker");
            IResultSet resultSet = testTable.CreateResultSet( 2, "zhu" );
            try
            {
                foreach ( IRecord rec in resultSet )
                {
                    rec.Equals( null );
                }
            }
            catch ( BadIndexesException )
            {}
            m_database.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            dbStruct.RebuildIndexes( true );
            dbStruct.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure(  );
            m_database = dbStruct.Database;
            testTable = m_database.GetTable("EndMarker");
            Assert.AreEqual( 2, testTable.Count );
            resultSet = testTable.CreateResultSet( 2, "zhu2" );
            int count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu2", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
            resultSet = testTable.CreateResultSet( 2, "zhu" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                rec.Equals( null );
                //Assert.AreEqual( "zhu", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 0, count );
            resultSet = testTable.CreateResultSet( 2, "zhu1" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu1", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
        }
        [Test]
        public void TestStringAutorecovery_corruptedCount2( )
        {
            ITable testTable = m_database.GetTable("People");
            IRecord record = testTable.NewRecord();
            record.SetValue( 1, "zhu" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 1, "zhu1" );
            record.Commit();
            m_database.Shutdown();
            FileStream file = File.Open( "MyPal.People.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 5, SeekOrigin.Begin );
            byte[] bytes = new byte[1];
            bytes[0] = 10;
            file.Write( bytes, 0, 1 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("People");
            IResultSet resultSet = testTable.CreateResultSet( 1, "zhu" );
            try
            {
                foreach ( IRecord rec in resultSet )
                {
                    rec.Equals( null );
                }
            }
            catch ( BadIndexesException )
            {}
            m_database.Shutdown();
            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            dbStruct.RebuildIndexes( true );
            dbStruct.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure(  );
            m_database = dbStruct.Database;
            testTable = m_database.GetTable("People");
            Assert.AreEqual( 1, testTable.Count );

            resultSet = testTable.CreateResultSet( 1, "zhu1" );
            int count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu1", rec.GetStringValue( 1 ) );
            }
            Assert.AreEqual( 1, count );
            resultSet = testTable.CreateResultSet( 1, "zhu" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                rec.Equals( null );
                //Assert.AreEqual( "zhu", rec.GetStringValue( 1 ) );
            }
            Assert.AreEqual( 0, count );
        }
        [Test]
        public void TestStringAutorecovery_onlyCount( )
        {
            ITable testTable = m_database.GetTable("EndMarker");
            IRecord record = testTable.NewRecord();
            record.SetValue( 2, "zhu" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 2, "zhu1" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 2, "zhu2" );
            record.Commit();
            m_database.Shutdown();

            FileStream file = File.Open( "MyPal.EndMarker.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.SetLength( 54 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("EndMarker");
            IResultSet resultSet = testTable.CreateResultSet( 2, "zhu" );
            try
            {
                foreach ( IRecord rec in resultSet )
                {
                    rec.Equals( null );
                }
            }
            catch ( BadIndexesException )
            {}
            m_database.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            dbStruct.RebuildIndexes( true );
            dbStruct.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure(  );
            m_database = dbStruct.Database;
            testTable = m_database.GetTable("EndMarker");
            Assert.AreEqual( 2, testTable.Count );
            resultSet = testTable.CreateResultSet( 2, "zhu2" );
            int count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu2", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 0, count );
            resultSet = testTable.CreateResultSet( 2, "zhu" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                rec.Equals( null );
                //Assert.AreEqual( "zhu", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
            resultSet = testTable.CreateResultSet( 2, "zhu1" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu1", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
        }
        [Test]
        public void TestStringAutorecovery_onlyPartOfString( )
        {
            ITable testTable = m_database.GetTable("EndMarker");
            IRecord record = testTable.NewRecord();
            record.SetValue( 2, "zhu" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 2, "zhu1" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 2, "zhu2" );
            record.Commit();
            m_database.Shutdown();

            FileStream file = File.Open( "MyPal.EndMarker.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.SetLength( 56 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("EndMarker");
            IResultSet resultSet = testTable.CreateResultSet( 2, "zhu" );
            try
            {
                foreach ( IRecord rec in resultSet )
                {
                    rec.Equals( null );
                }
            }
            catch ( BadIndexesException )
            {}
            m_database.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            dbStruct.RebuildIndexes( true );
            dbStruct.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure(  );
            m_database = dbStruct.Database;
            testTable = m_database.GetTable("EndMarker");
            Assert.AreEqual( 2, testTable.Count );
            resultSet = testTable.CreateResultSet( 2, "zhu2" );
            int count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu2", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 0, count );
            resultSet = testTable.CreateResultSet( 2, "zhu" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                rec.Equals( null );
                //Assert.AreEqual( "zhu", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
            resultSet = testTable.CreateResultSet( 2, "zhu1" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu1", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
        }
        [Test]
        public void TestStringAutorecovery_noMarkerAtEnd( )
        {
            ITable testTable = m_database.GetTable("EndMarker");
            IRecord record = testTable.NewRecord();
            record.SetValue( 2, "zhu" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 2, "zhu1" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 2, "zhu2" );
            record.Commit();
            m_database.Shutdown();

            FileStream file = File.Open( "MyPal.EndMarker.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.SetLength( 58 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("EndMarker");
            IResultSet resultSet = testTable.CreateResultSet( 2, "zhu" );
            try
            {
                foreach ( IRecord rec in resultSet )
                {
                    rec.Equals( null );
                }
            }
            catch ( BadIndexesException )
            {}
            m_database.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            dbStruct.RebuildIndexes( true );
            dbStruct.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure(  );
            m_database = dbStruct.Database;
            testTable = m_database.GetTable("EndMarker");
            Assert.AreEqual( 2, testTable.Count );
            resultSet = testTable.CreateResultSet( 2, "zhu2" );
            int count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu2", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 0, count );
            resultSet = testTable.CreateResultSet( 2, "zhu" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                rec.Equals( null );
                //Assert.AreEqual( "zhu", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
            resultSet = testTable.CreateResultSet( 2, "zhu1" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu1", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
        }
        [Test]
        public void TestStringAutorecovery_CountToEndMarker( )
        {
            ITable testTable = m_database.GetTable("EndMarker");
            IRecord record = testTable.NewRecord();
            record.SetValue( 2, "zhu" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 2, "zhu1" );
            record.Commit();
            record = testTable.NewRecord();
            record.SetValue( 2, "zhu2" );
            record.Commit();
            m_database.Shutdown();

            FileStream file = File.Open( "MyPal.EndMarker.table.dbUtil", FileMode.Open, FileAccess.Write );
            file.Seek( 50, SeekOrigin.Begin );
            byte[] bytes = new byte[4];
            bytes[0] = 0xBA;
            bytes[1] = 0xB0;
            bytes[2] = 0xDA;
            bytes[3] = 0xDE;
            file.Write( bytes, 0, 4 );
            file.Flush();
            file.Close();

            DBStructure dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            m_database = dbStruct.OpenDatabase( );
            testTable = m_database.GetTable("EndMarker");
            IResultSet resultSet = testTable.CreateResultSet( 2, "zhu" );
            try
            {
                foreach ( IRecord rec in resultSet )
                {
                    rec.Equals( null );
                }
            }
            catch ( BadIndexesException )
            {}
            m_database.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure();
            dbStruct.RebuildIndexes( true );
            dbStruct.Shutdown();

            dbStruct = new DBStructure( "", "MyPal" );
            dbStruct.LoadStructure(  );
            m_database = dbStruct.Database;
            testTable = m_database.GetTable("EndMarker");
            Assert.AreEqual( 2, testTable.Count );
            resultSet = testTable.CreateResultSet( 2, "zhu2" );
            int count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu2", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 0, count );
            resultSet = testTable.CreateResultSet( 2, "zhu" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                rec.Equals( null );
                //Assert.AreEqual( "zhu", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
            resultSet = testTable.CreateResultSet( 2, "zhu1" );
            count = 0;
            foreach ( IRecord rec in resultSet )
            {
                ++count;
                Assert.AreEqual( "zhu1", rec.GetStringValue( 2 ) );
            }
            Assert.AreEqual( 1, count );
        }
    }
}
