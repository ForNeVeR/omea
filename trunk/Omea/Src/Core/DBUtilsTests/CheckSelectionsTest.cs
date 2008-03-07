/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.Database;
using NUnit.Framework;
using JetBrains.Omea.Containers;

namespace DBUtil
{
	[TestFixture]
	public class CheckSelectionsTest
	{
		IDatabase _database;
        ITable _peopleTable;
        ITable _testTable;
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

                TableStructure tblTest = database.CreateTable( "Test" );
                tblTest.CreateColumn( "Id", ColumnType.Integer, false );
                tblTest.CreateColumn( "Date", ColumnType.DateTime, false );
                tblTest.SetCompoundIndex( "Id", "Date" );

				database.SaveStructure();
                database.Shutdown();
                database = new DBStructure( "", "MyPal", DatabaseMode.Create );
                database.LoadStructure( );
				_database = database.Database;

				_peopleTable = _database.GetTable("People");
                _testTable = _database.GetTable("Test");
				//m_testTable.InputMode = InputMode.Batch;
				for ( int i = 0; i < 10; i++ )
				{
					IRecord record = _peopleTable.NewRecord();
					Assert.AreEqual( i, record.GetID(), "Id is wrong" );
					record.SetValue( 1, ( 500 - i ).ToString() );
					record.Commit();
					Assert.AreEqual( (i+1), _peopleTable.Count, "Count is wrong" );
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
				_database.Shutdown();
				DBTest.RemoveDBFiles();
			}
			catch ( Exception exc )
			{
				Assert.Fail( exc.Message );
			}
		}
		public void SortEqualAscWithNoRecords( )
		{
			IResultSet resultSet = _peopleTable.CreateModifiableResultSet( 1, "300" );
			int count = 0;
			foreach ( IRecord record in resultSet )
			{
				string name = record.GetStringValue( 1 );
                name = name;
				count++;
			}
			Assert.AreEqual( 0, count, "Count is wrong" );
            resultSet.Dispose();
		}
		[Test]
		public void SortEqualAsc( )
		{
			IResultSet resultSet = _peopleTable.CreateModifiableResultSet( 1, "495" );
			int count = 0;
			foreach ( IRecord record in resultSet )
			{
				string name = record.GetStringValue( 1 );
				Assert.AreEqual( "495", name, "Name is wrong" );
				count++;
			}
			Assert.AreEqual( 1, count, "Count is wrong" );
            resultSet.Dispose();
		}
		public void CheckGetRecordByEqual( )
		{
			IRecord record = _peopleTable.GetRecordByEqual( 1, "496" );
			string name = record.GetStringValue(1);
			Assert.AreEqual( "Name is wrong", "496", name );
			record = _peopleTable.GetRecordByEqual( 1, "300" );
			Assert.AreEqual( null, record, "record must null" );
		}
		[Test]
		public void CheckIfColumnHasNotIndex( )
		{
			try
			{
				IRecord record = _peopleTable.GetRecordByEqual( 2, 30 );
                record = record;
			}
			catch ( ColumnHasNoIndexException )
			{
				//It's normal
				return;
			}
			Assert.Fail( "ColumnHasNotIndex must be thrown" );
		}
		
		[Test]
		public void CheckSelectionByTwoColumns( )
		{
			ICountedResultSet resultSet = _peopleTable.CreateResultSet( 0, 1, 1, "499", false );
			Assert.AreEqual( 1, resultSet.Count, "Count is wrong" );
			Assert.AreEqual( 1, resultSet[0].GetIntValue(0), "Id is wrong" );
			Assert.AreEqual( "499", resultSet[0].GetStringValue(1), "Name is wrong" );
            resultSet.Dispose();
		}

		public void CheckSelectionByTwoColumnsWithNoRecords( )
		{
			ICountedResultSet resultSet = _peopleTable.CreateResultSet( 0, 1, 1, "498", false );
			Assert.AreEqual( 0, resultSet.Count );
            resultSet.Dispose();
		}
        [Test]
        public void CreateResultSetForRangeByTwoColumnsIndex( )
        {
            for ( int i = 0; i < 100; i++ )
            {
                IRecord record = _peopleTable.NewRecord();
                record.SetValue( 1, "Ivan" );
                record.SetValue( 3, i );
                record.Commit();
            }
            ICountedResultSet resultSet = _peopleTable.CreateResultSetForRange( 1, "Ivan", 3, 0, 99 );
            Assert.AreEqual( 100, resultSet.Count );
            resultSet.Dispose();

        }
        [Test]
        public void CreateResultSetForRangeByTwoColumnsIndex1( )
        {
            for ( int i = 0; i < 100; i++ )
            {
                IRecord record = _peopleTable.NewRecord();
                record.SetValue( 1, "Ivan" );
                record.SetValue( 3, i );
                record.Commit();
            }
            ICountedResultSet resultSet = _peopleTable.CreateResultSetForRange( 1, "Sergey", 3, 0, 99 );
            Assert.AreEqual( 0, resultSet.Count );
            resultSet.Dispose();
        }
        [Test]
        public void CreateResultSetForRangeByTwoColumnsIndex2( )
        {
            for ( int i = 0; i < 100; i++ )
            {
                IRecord record = _peopleTable.NewRecord();
                record.SetValue( 1, "Ivan" );
                record.SetValue( 3, i );
                record.Commit();
            }
            ICountedResultSet resultSet = _peopleTable.CreateResultSetForRange( 1, "Ivan", 3, 100, 200 );
            Assert.AreEqual( 0, resultSet.Count );
            resultSet.Dispose();
        }
        [Test]
        public void CreateResultSetForRangeByTwoColumnsIndex3( )
        {
            for ( int i = 0; i < 100; i++ )
            {
                IRecord record = _peopleTable.NewRecord();
                record.SetValue( 1, "Ivan" );
                record.SetValue( 3, i );
                record.Commit();
            }
            ICountedResultSet resultSet = _peopleTable.CreateResultSetForRange( 1, "Ivan", 3, -100, -1 );
            Assert.AreEqual( 0, resultSet.Count );
            resultSet.Dispose();
        }
        [Test]
        public void CreateResultSetForRangeByTwoColumnsIndex4( )
        {
            for ( int i = 0; i < 100; i++ )
            {
                IRecord record = _peopleTable.NewRecord();
                record.SetValue( 1, "Ivan" );
                record.SetValue( 3, i );
                record.Commit();
            }
            ICountedResultSet resultSet = _peopleTable.CreateResultSetForRange( 1, "Ivan", 3, -100, 50 );
            Assert.AreEqual( 51, resultSet.Count );
            resultSet.Dispose();
        }
        [Test]
        public void CreateResultSetForRangeByTwoColumnsIndex5( )
        {
            for ( int i = 0; i < 100; i++ )
            {
                IRecord record = _peopleTable.NewRecord();
                record.SetValue( 1, "Ivan" );
                record.SetValue( 3, i );
                record.Commit();
            }
            ICountedResultSet resultSet = _peopleTable.CreateResultSetForRange( 1, "Ivan", 3, 50, 1000 );
            Assert.AreEqual( 50, resultSet.Count );
            resultSet.Dispose();
        }
        public void CreateResultSetForRangeByTwoColumnsIndex7( )
        {
            for ( int i = 0; i < 100; i = i + 2 )
            {
                IRecord record = _peopleTable.NewRecord();
                record.SetValue( 1, "Ivan" );
                record.SetValue( 3, i );
                record.Commit();
            }
            ICountedResultSet resultSet = _peopleTable.CreateResultSetForRange( 1, "Ivan", 3, -1, 1 );
            Assert.AreEqual( 1, resultSet.Count );
            resultSet.Dispose();
        }
        [Test]
        public void CreateResultSetForRangeByTwoColumnsIndex8( )
        {
            for ( int i = 0; i < 20; i = i + 2 )
            {
                IRecord record = _peopleTable.NewRecord();
                record.SetValue( 1, "Ivan" );
                record.SetValue( 3, i );
                record.Commit();
            }
            ICountedResultSet resultSet = _peopleTable.CreateResultSetForRange( 1, "Ivan", 3, 17, 21 );
            Assert.AreEqual( 1, resultSet.Count );
            resultSet.Dispose();
        }
        [Test]
        public void CreateResultSetForRangeByTwoColumnsIndex9( )
        {
            for ( int i = 0; i < 20; i = i + 2 )
            {
                IRecord record = _peopleTable.NewRecord();
                record.SetValue( 1, "Ivan" );
                record.SetValue( 3, i );
                record.Commit();
            }
            ICountedResultSet resultSet = _peopleTable.CreateResultSetForRange( 1, "Ivan", 3, 13, 17 );
            Assert.AreEqual( 2, resultSet.Count );
            resultSet.Dispose();
        }
        [Test]
        public void CreateResultOnFirstPartOfCompoundIndex( )
        {
            for ( int i = 0; i < 20; i = i + 2 )
            {
                IRecord record = _peopleTable.NewRecord();
                record.SetValue( 1, "Ivan" );
                record.SetValue( 3, i );
                record.Commit();
            }
            ICountedResultSet resultSet = _peopleTable.CreateResultSet( 1 );
            Assert.AreEqual( 20, resultSet.Count );
            resultSet.Dispose();
        }
        [Test]
        public void CheckCompoundIndexKeyFactory( )
        {
            for ( int i = 0; i < 13000; i++ )
            {
                IRecord record = _testTable.NewRecord();
                record.SetValue( 1, DateTime.Today.AddDays(i) );
                record.Commit();
            }
            _database.Shutdown();
            DBStructure dbStructure = new DBStructure( "", "MyPal" );
            _database = dbStructure.OpenDatabase();
            int i1 = ("1308554180").GetHashCode();
            i1 = i1;
            int i2 = ("2117097963").GetHashCode();
            i2 = i2;

        }
        [Test]
        public void CheckSelectionByStringWithSameHashCode( )
        {
            IRecord record = _peopleTable.NewRecord();
            record.SetValue( 1, "1308554180" );
            record.Commit();
            record = _peopleTable.NewRecord();
            record.SetValue( 1, "2117097963" );
            record.Commit();
            ICountedResultSet resultSet = _peopleTable.CreateModifiableResultSet( 1, "1308554180" );
            Assert.AreEqual( 1, resultSet.Count );
            resultSet.Dispose();
        }
        [Test]
        public void CheckSelectionByCompoundKeyWithValueOnBTree( )
        {
            FixedLengthKey_Int intKey1 = new FixedLengthKey_Int( 0 );
            FixedLengthKey_Int intKey2 = new FixedLengthKey_Int( 0 );
            FixedLengthKey_Int value = new FixedLengthKey_Int( 0 );
            FixedLengthKey_CompoundWithValue keyFactory = new FixedLengthKey_CompoundWithValue( intKey1, intKey2, value );
            BTree bTree = new BTree( "CheckSelectionByCompoundKeyWithValueOnBTree.dbUtil", keyFactory );
            bTree.Open();
            keyFactory.Key = new CompoundAndValue( 1, 2, 3 );
            bTree.InsertKey( keyFactory, 888 );
            Assert.AreEqual( 1, bTree.Count );
            IntArrayList offsets = new IntArrayList();
            bTree.SearchForRange( keyFactory, keyFactory, offsets );
            Assert.AreEqual( 1, offsets.Count );
            Assert.AreEqual( 888, offsets[0] );
            bTree.Close();
        }
        [Test][ExpectedException(typeof(AttemptDeleteNotNormalOrNotUpdatedRecordException))]
        public void CheckDeletingNotCommitedRecord( )
        {
            IRecord record = _peopleTable.NewRecord();
            record.Delete();
        }
        [Test]
        public void CheckDeletingAfterCommitRecord( )
        {
            IRecord record = _peopleTable.NewRecord();
            record.Commit();
            record.Delete();
        }
        [Test][ExpectedException(typeof(AttemptWritingToDeletedRecordException))]
        public void CheckWritingToDeletedRecord( )
        {
            IRecord record = _peopleTable.NewRecord();
            record.Commit();
            record.Delete();
            record.SetValue( 1, "value" );
        }
        [Test]
        public void CheckReadingFromDeletedRecord( )
        {
            IRecord record = _peopleTable.NewRecord();
            record.Commit();
            record.Delete();
            record.GetValue( 1 );
        }
        [Test][ExpectedException(typeof(AttemptDeleteNotNormalOrNotUpdatedRecordException))]
        public void CheckDeletingDeletedRecord( )
        {
            IRecord record = _peopleTable.NewRecord();
            record.Commit();
            record.Delete();
            record.Delete();
        }
        [Test]
        public void CheckUnicodeStringReading( )
        {
            IRecord record = _peopleTable.NewRecord();
            string str = @"Ѓ¦–ўЏі‘ш‚ЖЏі‘шЌLЌђЃЎ8ђз–њ‰~Ћы“ь•ы–@’с‹џѓЃѓ‹ѓ}ѓKЃЎђV”N‚НЃy‚T‰­‚Xђз–њ‰~ЏШ‹’—LѓrѓWѓlѓXЃz‚Е–Ъ“I’Bђ¬6ђз–њ‰~ЏШ‹’—LЋы“ь•ы–@‚ ‚и‚Ь‚·Ѓ@-‚Uђз–њ‰~€И‰є”NЋы‚М•ы‚Й‘еЉЅЊ}‚і‚к‚Д‚ў‚Ь‚·---_‚ж‚иЏШ‹’‚Е‚·ЃBЃ@";
            record.SetValue( 0, 888 );
            record.SetValue( 1, str );
            record.Commit();
            IResultSet resultSet = _peopleTable.CreateResultSet( 0, 888 );
            foreach ( IRecord rec in resultSet )
            {
                Assert.AreEqual( str, rec.GetStringValue( 1 ) );
            }
            resultSet.Dispose();
        }

    }

}