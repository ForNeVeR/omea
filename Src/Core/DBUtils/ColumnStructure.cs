// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;

namespace JetBrains.Omea.Database
{
    public class ColumnStructure
    {
        private string _name;
        private ColumnType _type;
        private bool _indexPresent;

        internal ColumnStructure( BinaryReader structReader )
        {
            _name = structReader.ReadString();
            _type = (ColumnType)structReader.ReadByte();
            if ( structReader.ReadBoolean() )
            {
                CreateIndex();
            }
        }
        public ColumnStructure( string columnName, ColumnType columnType, bool indexPresent )
        {
            _name = columnName;
            _type = columnType;
            if ( indexPresent )
            {
                CreateIndex();
            }
        }

        public string Name{ get{ return _name; } }

        public bool HasIndex{ get{ return _indexPresent; } }

        public void CreateIndex()
        {
            _indexPresent = true;
        }
        public void DropIndex()
        {
            _indexPresent = false;
        }

        internal void SaveStructure( BinaryWriter structWriter )
        {
            structWriter.Write( _name );
            structWriter.Write( (byte)_type );
            structWriter.Write( _indexPresent );
        }

        internal Column MakeColumn( Table table, string name )
        {
            return MakeColumn( table, _type, name );
        }

        internal static Column MakeColumn( Table table, ColumnType type, string name )
        {
            switch ( type )
            {
                case ColumnType.String:
                    if ( table.NeedUpgradeTo22 )
                    {
                        return new StringColumnTo22( table, name, table.Version );
                    }
                    else
                    {
                        return new StringColumn( table, name, table.Version );
                    }
                case ColumnType.DateTime:
                    return new DateTimeColumn( table, name, table.Version );
                case ColumnType.Integer:
                    return new IntColumn( table, name, table.Version );
                case ColumnType.Double:
                    return new DoubleColumn( table, name, table.Version );
                case ColumnType.BLOB:
                    return new BLOBColumn( table, name, table.Version );
                default:
                    throw new NotSupportedException( "'" + type.ToString() + "' column type does not supported" );
            }
        }
    }
}
