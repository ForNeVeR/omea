﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// This file was generated by C# Refactory.
// To modify this template, go to Tools/Options/C# Refactory/Code

using System;

using JetBrains.Omea.Database;

using NUnit.Framework;

namespace DBUtil
{
	[TestFixture]
	public class DataSaveLoadTest
	{
		private IDatabase m_database = null;
		public DataSaveLoadTest() {}
        private const int Id = 0;
        private const int Name = 1;
        private const int Age = 2;
        private const int Birthday = 3;

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

		private void InsertRecord( ITable testTable, string name, int age, DateTime dateTime )
		{
			IRecord record = testTable.NewRecord();
			record.SetValue( Name, name );
			record.SetValue( Age, age );
			record.SetValue( Birthday, dateTime );
			record.Commit();
		}

		private void Insert_100_NewRecordsAndCheck( ITable testTable, int expectedCount )
		{
			for ( int i = 0; i < 100; i++ )
			{
				InsertRecord( testTable, "zhu", i, DateTime.Now );
			}
			Assert.AreEqual( expectedCount, testTable.Count );
		}

		[Test]
		public void NumOfRecords( )
		{
			ITable testTable = m_database.GetTable("People");
			Insert_100_NewRecordsAndCheck( testTable, 100 );
			Insert_100_NewRecordsAndCheck( testTable, 200 );
			Insert_100_NewRecordsAndCheck( testTable, 300 );
		}
		[Test]
		public void NavigationThroughRecords( )
		{
			ITable testTable = m_database.GetTable("People");
			Insert_100_NewRecordsAndCheck( testTable, 100 );
			IResultSet people = testTable.CreateResultSet( Id );
			int count = 0;
			foreach ( IRecord person in people )
			{
				person.GetValue(Id);
				count++;
			}
			Assert.AreEqual( 100, count );
            people.Dispose();
			people = testTable.CreateResultSet( Id );
			count = 0;
			foreach ( IRecord person in people )
			{
				person.GetValue(Id);
				count++;
			}
			Assert.AreEqual( 100, count );
            people.Dispose();
		}
		[Test]
		public void SaveAndLoad( )
		{
			ITable testTable = m_database.GetTable("People");
			DateTime dateTime = DateTime.Now;
			for ( int i = 0; i < 100; i++ )
			{
				InsertRecord( testTable, i.ToString(), i,
					dateTime.Subtract( new TimeSpan( i, 0, 0, 0 ) ) );
			}

			IResultSet people = testTable.CreateResultSet( Id );
			int count = 0;
			foreach ( IRecord person in people )
			{
				int id = person.GetIntValue( Id );
				Assert.IsTrue( id == count );
				string name = person.GetStringValue( Name );
				Assert.IsTrue( name == count.ToString() );
				int age = person.GetIntValue( Age );
                age = age;
				DateTime receivedTime = (DateTime)person.GetValue( Birthday );
				Assert.IsTrue( receivedTime == dateTime.Subtract( new TimeSpan( count, 0, 0, 0 ) ) );
				count++;
			}
			Assert.AreEqual( 100, count );
            people.Dispose();
		}
	}

}
