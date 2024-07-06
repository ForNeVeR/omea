// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Database;
using NUnit.Framework;

namespace DBUtil
{
	[TestFixture]
	public class DeletingRecordTest
	{
		private IDatabase m_database = null;
		public DeletingRecordTest() {}

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
				m_database.Shutdown();
				DBTest.RemoveDBFiles();
			}
			catch ( Exception exc )
			{
				Assert.Fail( exc.Message );
			}
		}
        //private const int Id = 0;
        private const int Name = 1;
        //private const int Age = 2;
        private const int Birthday = 3;
        //private const int Price = 2;

		[Test]
		public void CheckDeletingRow( )
		{
			ITable testTable = m_database.GetTable("People");
			for ( int i = 0; i < 10; i++ )
			{
				IRecord record = testTable.NewRecord();
				record.SetValue( Name, "zhu"+ i.ToString() );
				record.SetValue( Birthday, DateTime.Now );
				record.Commit();
				Assert.IsTrue( testTable.Count == (i + 1) );
			}

			ICountedResultSet resultTest = testTable.CreateModifiableResultSet( Name, "zhu4" );
			Assert.AreEqual( 1, resultTest.Count );

			foreach ( IRecord record in resultTest )
			{
				record.Delete();
			}
			resultTest = testTable.CreateModifiableResultSet( Name, "zhu4" );
			Assert.AreEqual( 0, resultTest.Count );
		}

		[Test]
		public void CheckUpdatingRow( )
		{
			ITable testTable = m_database.GetTable("People");
			for ( int i = 0; i < 10; i++ )
			{
				IRecord record = testTable.NewRecord();
				record.SetValue( Name, "zhu"+ i.ToString() );
				record.SetValue( Birthday, DateTime.Now );
				record.Commit();
				Assert.AreEqual( (i + 1), testTable.Count );
			}

			ICountedResultSet resultTest = testTable.CreateModifiableResultSet( Name, "zhu4" );
			Assert.AreEqual( 1, resultTest.Count );
			foreach( IRecord record in resultTest )
			{
				record.SetValue( Name, "SergZ" );
				Assert.AreEqual( "SergZ", record.GetValue( Name ) );
			}
			resultTest = testTable.CreateModifiableResultSet( Name, "zhu4" );
			Assert.AreEqual( 1, resultTest.Count );

			resultTest = testTable.CreateModifiableResultSet( Name, "SergZ" );
			Assert.AreEqual( 0, resultTest.Count );

			resultTest = testTable.CreateModifiableResultSet( Name, "zhu4" );
			Assert.AreEqual( 1, resultTest.Count );
			foreach( IRecord record in resultTest )
			{
				record.SetValue( Name, "SergZ" );
				Assert.AreEqual( "SergZ", record.GetValue( Name ) );
				record.Commit();
				Assert.AreEqual( "SergZ", record.GetValue( Name ) );
			}
			resultTest = testTable.CreateModifiableResultSet( Name, "zhu4" );
			Assert.AreEqual( 0, resultTest.Count );

			resultTest = testTable.CreateModifiableResultSet( Name, "SergZ" );
			Assert.AreEqual( 1, resultTest.Count );
		}
	}
}
