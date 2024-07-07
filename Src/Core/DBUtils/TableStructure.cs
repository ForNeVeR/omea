// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using JetBrains.DataStructures;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.Database
{
    public class TableStructure
    {
        private string _name;
        private bool _dirty = false;
        private HashMap _columns = new HashMap();
        private HashMap _compoundIndexes = new HashMap();
        private HashMap _compoundIndexesWithValue = new HashMap();
        private DBStructure _dbStructure = null;
        private Table _table = null;
        private Tracer _tracer = null;
        private int _nextID = 0;
        private int _totalCount = -1;

        struct CompoundWithValue
        {
            public string firstColumn;
            public string secondColumn;
            public string valueColumn;
        }
        internal TableStructure( BinaryReader structReader, DBStructure dbStructure )
        {
            _dbStructure = dbStructure;
            _name = structReader.ReadString();
            _tracer = new Tracer( "(DBUtils) Table structure - " + _name );
            if ( _dbStructure.Version >= 3 )
            {
                _nextID = structReader.ReadInt32();
            }
            if ( _dbStructure.Version >= 18 )
            {
                _totalCount = structReader.ReadInt32();
            }
            _dirty = structReader.ReadBoolean();
            int count = structReader.ReadInt32();
            for ( int i = 0; i < count; i++ )
            {
                ColumnStructure column = new ColumnStructure( structReader );
                if ( _columns.Contains( column.Name ) )
                {
                    throw new ColumnAlreadyExistsException( "Table structure already contains such column", column.Name );
                }
                _columns.Add( column.Name, column );
            }
            int compoundCount = structReader.ReadInt32();
            for ( int i = 0; i < compoundCount; i++ )
            {
                string firstColumn = structReader.ReadString();
                string secondColumn = structReader.ReadString();
                if ( _compoundIndexes.Contains( firstColumn ) )
                {
                    throw new IndexAlreadyExistsException( "Table structure already contains such compound index: " +
                        firstColumn + " : " + secondColumn );
                }
                _compoundIndexes.Add( firstColumn, secondColumn );
            }
            if ( _dbStructure.Version >= 12 )
            {
                compoundCount = structReader.ReadInt32();
                for ( int i = 0; i < compoundCount; i++ )
                {
                    CompoundWithValue compoundWithValue = new CompoundWithValue();
                    compoundWithValue.firstColumn = structReader.ReadString();
                    compoundWithValue.secondColumn = structReader.ReadString();
                    compoundWithValue.valueColumn = structReader.ReadString();
                    if ( _compoundIndexesWithValue.Contains( compoundWithValue.firstColumn ) )
                    {
                        throw new IndexAlreadyExistsException( "Table structure already contains such compound index with value" +
                            compoundWithValue.firstColumn + " : " + compoundWithValue.secondColumn +
                            " : " + compoundWithValue.valueColumn );
                    }
                    _compoundIndexesWithValue.Add( compoundWithValue.firstColumn, compoundWithValue );
                }
            }
        }

        internal TableStructure( string name, DBStructure dbStructure )
        {
            _dbStructure = dbStructure;
            _name = name;
            _tracer = new Tracer( "(DBUtils) Table structure - " + _name );
        }

        public void SetNextID( int nextID )
        {
            Dirty = true;
            _nextID = nextID;
        }
        public void SetTotalCount( int totalCount )
        {
            _totalCount = totalCount;
        }
        public int NextID()
        {
            Dirty = true;
            return _nextID++;
        }

        public int PeekNextID()
        {
            return _nextID;
        }

        public int TotalCount()
        {
            return _totalCount;
        }
        public string Name{ get{ return _name; } }

        public void CreateIndex( string columnName )
        {
            if ( !_columns.Contains( columnName ) )
            {
                throw new ColumnDoesNotExistException( "", columnName );
            }
            ColumnStructure column = (ColumnStructure)_columns[columnName];
            if ( column.HasIndex )
            {
                throw new IndexAlreadyExistsException( columnName );
            }
            column.CreateIndex();
            Dirty = true;
        }

        public DatabaseMode Mode
        {
            get
            {
                return _dbStructure.Mode;
            }
        }

        public void DropIndex( string columnName )
        {
            _tracer.Trace( "DropIndex : " + columnName );
            if ( !_columns.Contains( columnName ) )
            {
                throw new ColumnDoesNotExistException( "", columnName );
            }

            ColumnStructure column = (ColumnStructure)_columns[columnName];
            if ( !column.HasIndex )
            {
                throw new ColumnHasNoIndexException( columnName );
            }

            if ( _table != null )
            {
                _table.DropIndex( columnName );
            }

            column.DropIndex();

            string strFullPath =
                DBHelper.GetFullNameForIndex( _dbStructure.Path, _dbStructure.Name,
                Name, column.Name );
            try
            {
                File.Delete( strFullPath );
            }
            catch ( Exception )
            {
            }

            Dirty = true;
            Dirty = false;
        }
        public bool HasIndex( string columnName )
        {
            if ( !_columns.Contains( columnName ) )
            {
                throw new ColumnDoesNotExistException( "", columnName );
            }
            ColumnStructure column = (ColumnStructure)_columns[columnName];
            return column.HasIndex;
        }
        public bool HasCompoundIndex( string columnName1, string columnName2 )
        {
            if ( !_compoundIndexes.Contains(columnName1) )
            {
                return false;
            }
            if ( !((string)_compoundIndexes[columnName1]).Equals( columnName2 ) )
            {
                return false;
            }
            return true;
        }
        public bool HasCompoundIndexWithValue( string columnName1, string columnName2 )
        {
            if ( !_compoundIndexesWithValue.Contains(columnName1) )
            {
                return false;
            }
            CompoundWithValue compoundWithValue = (CompoundWithValue)_compoundIndexesWithValue[columnName1];

            if ( !compoundWithValue.secondColumn.Equals( columnName2 ) )
            {
                return false;
            }
            return true;
        }

        public void DropCompoundIndex( string columnName1, string columnName2 )
        {
            _tracer.Trace( "DropCompoundIndex : " + columnName1 + "#" + columnName2 );
            if ( !HasCompoundIndex( columnName1, columnName2 ) )
            {
                throw new IndexDoesNotExistException( columnName1 + "#" + columnName2 );
            }
            _compoundIndexes.Remove( columnName1 );

            if ( _table != null )
            {
                _table.DropCompoundIndex( columnName1 + "#" + columnName2 );
            }

            string strFullPath =
                DBHelper.GetFullNameForIndex( _dbStructure.Path, _dbStructure.Name,
                Name, columnName1 + "#" + columnName2 );
            File.Delete( strFullPath );
            /*try
            {
                File.Delete( strFullPath );
            }
            catch ( Exception )
            {
            }*/

            Dirty = true;
            Dirty = false;
        }
        public void DropCompoundIndexWithValue( string columnName1, string columnName2 )
        {
            _tracer.Trace( "DropCompoundIndex : " + columnName1 + "#" + columnName2 );
            if ( !HasCompoundIndexWithValue( columnName1, columnName2 ) )
            {
                throw new IndexDoesNotExistException( columnName1 + "#" + columnName2 );
            }
            _compoundIndexesWithValue.Remove( columnName1 );

            if ( _table != null )
            {
                _table.DropCompoundIndexWithValue( columnName1 + "#" + columnName2 );
            }

            string strFullPath =
                DBHelper.GetFullNameForIndex( _dbStructure.Path, _dbStructure.Name,
                Name, columnName1 + "#" + columnName2 );
            try
            {
                File.Delete( strFullPath );
            }
            catch ( Exception )
            {
            }

            Dirty = true;
            Dirty = false;
        }
        public ColumnStructure CreateColumn( string name, ColumnType type, bool indexPresent )
        {
            _tracer.Trace( "CreateColumn : " + name );
            if ( !_columns.Contains( name ) )
            {
                ColumnStructure column = new ColumnStructure( name, type, indexPresent );
                _columns.Add( name, column );
                return column;
            }
            throw new ColumnAlreadyExistsException( "Column with this name already exists", name );
        }

        public ColumnStructure GetColumn( string name )
        {
            if ( !_columns.Contains( name ) )
            {
                throw new ColumnDoesNotExistException( string.Empty, name );
            }

            return (ColumnStructure)_columns[name];
        }

        private void CheckIfColumnExists( string columnName )
        {
            if ( !_columns.Contains( columnName ) )
            {
                throw new ColumnDoesNotExistException( "Column does not exist", columnName );
            }
        }

        public void SetCompoundIndex( string columnName1, string columnName2 )
        {
            _tracer.Trace( "SetCompoundIndex : " + columnName1+"#"+columnName2 );
            CheckIfColumnExists( columnName1 );
            CheckIfColumnExists( columnName2 );
            if ( _compoundIndexes.Contains(columnName1) )
            {
                if ( columnName2.Equals((string)_compoundIndexes[columnName1]) )
                {
                    throw new IndexAlreadyExistsException( columnName1+"#"+columnName2 );
                }
            }
            _compoundIndexes.Add( columnName1, columnName2 );
            if ( _dbStructure.Mode != DatabaseMode.Create )
            {
                Dirty = true;
                Dirty = false;
            }
        }
        public void SetCompoundIndexWithValue( string columnName1, string columnName2, string valueColumnName )
        {
            _tracer.Trace( "SetCompoundIndexWithValue : " + columnName1+"#"+columnName2 );
            CheckIfColumnExists( columnName1 );
            CheckIfColumnExists( columnName2 );
            CheckIfColumnExists( valueColumnName );
            CompoundWithValue compoundWithValue;
            if ( _compoundIndexesWithValue.Contains(columnName1) )
            {
                compoundWithValue = (CompoundWithValue)_compoundIndexesWithValue[columnName1];
                if ( columnName2.Equals(compoundWithValue.secondColumn) )
                {
                    throw new IndexAlreadyExistsException( columnName1+"#"+columnName2 );
                }
            }

            compoundWithValue = new CompoundWithValue();
            compoundWithValue.firstColumn = columnName1;
            compoundWithValue.secondColumn = columnName2;
            compoundWithValue.valueColumn = valueColumnName;
            _compoundIndexesWithValue.Add( columnName1, compoundWithValue );
            if ( _dbStructure.Mode != DatabaseMode.Create )
            {
                Dirty = true;
                Dirty = false;
            }
        }

        internal bool Dirty
        {
            get { return _dirty; }
            set
            {
                if ( _dirty != value )
                {
                    _dirty = value;
                    _dbStructure.SaveStructure();
                }
            }
        }

        internal void SaveStructure( BinaryWriter structWriter )
        {
            structWriter.Write( _name );
            structWriter.Write( _nextID );
            structWriter.Write( _totalCount );
            structWriter.Write( _dirty );
            structWriter.Write( _columns.Count );
            foreach ( HashMap.Entry entry in _columns )
            {
                ColumnStructure column = (ColumnStructure)entry.Value;
                column.SaveStructure( structWriter );
            }
            structWriter.Write( _compoundIndexes.Count );
            foreach ( HashMap.Entry entry in _compoundIndexes )
            {
                structWriter.Write( (string)entry.Key );
                structWriter.Write( (string)entry.Value );
            }
            structWriter.Write( _compoundIndexesWithValue.Count );
            foreach ( HashMap.Entry entry in _compoundIndexesWithValue )
            {
                structWriter.Write( (string)entry.Key );
                CompoundWithValue compoundWithValue = (CompoundWithValue)entry.Value;
                structWriter.Write( compoundWithValue.secondColumn );
                structWriter.Write( compoundWithValue.valueColumn );
            }
        }

        internal void OpenTable( IDatabaseDesign dbDesign )
        {
            Table tableDesign = (Table) dbDesign.CreateTable( Name, this );
            _table = tableDesign;
            foreach ( HashMap.Entry entry in _columns )
            {
                ColumnStructure colStructure = (ColumnStructure)entry.Value;
                Column column = colStructure.MakeColumn( tableDesign, colStructure.Name );
                tableDesign.AddColumn( column );
                if ( colStructure.HasIndex )
                {
                    IDBIndex dbIndex =
                        new DBIndex( tableDesign, colStructure.Name, column.GetFixedFactory(), null, null, null );
                    tableDesign.AddIndex( colStructure.Name, dbIndex );
                }
            }
            tableDesign.CheckTableLength();
            foreach ( HashMap.Entry entry in _compoundIndexes )
            {
                string firstName = (string)entry.Key;
                Column firstColumn = tableDesign.GetColumn( firstName );
                string secondName = (string)entry.Value;
                Column secondColumn = tableDesign.GetColumn( secondName );
                string compoundName = firstName + "#" + secondName;
                IDBIndex dbIndex =
                    new DBIndex( tableDesign, compoundName,
                                    new FixedLengthKey_Compound( firstColumn.GetFixedFactory(), secondColumn.GetFixedFactory() ),
                                        firstColumn.GetFixedFactory(), secondColumn.GetFixedFactory(), null );
                tableDesign.AddCompoundIndex( compoundName, dbIndex );
            }
            foreach ( HashMap.Entry entry in _compoundIndexesWithValue )
            {
                string firstName = (string)entry.Key;
                Column firstColumn = tableDesign.GetColumn( firstName );
                CompoundWithValue compoundWithValue = (CompoundWithValue)entry.Value;
                string secondName = compoundWithValue.secondColumn;
                Column secondColumn = tableDesign.GetColumn( secondName );
                Column valueColumn = tableDesign.GetColumn( compoundWithValue.valueColumn );
                string compoundName = firstName + "#" + secondName;
                IDBIndex dbIndex =
                    new DBIndex( tableDesign, compoundName,
                    new FixedLengthKey_CompoundWithValue( firstColumn.GetFixedFactory(), secondColumn.GetFixedFactory(), valueColumn.GetFixedFactory() ),
                    firstColumn.GetFixedFactory(), secondColumn.GetFixedFactory(), valueColumn.GetFixedFactory() );
                tableDesign.AddCompoundIndexWithValue( compoundName, dbIndex, valueColumn.Name );
            }
            if ( !tableDesign.Dirty )
            {
                try
                {
                    tableDesign.OpenIndexes();
                }
                catch( BadIndexesException )
                {
                    _dirty = true;
                }
            }
        }
    }
}
