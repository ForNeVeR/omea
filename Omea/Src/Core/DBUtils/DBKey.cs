// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.Database
{
    public class FixedLengthKey : IFixedLengthKey
    {
        public FixedLengthKey( )
        {
        }
        #region IFixedLengthKey Members

        public virtual IComparable Key
        {
            get { return null; }
            set {}
        }
        public virtual int KeySize
        {
            get
            {
                return 8;
            }
        }

        public virtual IComparable MinKey
        {
            get { return null; }
        }
        public virtual IComparable MaxKey
        {
            get { return null; }
        }
        public virtual void Write(System.IO.BinaryWriter writer)
        {
        }

        public virtual void Read(System.IO.BinaryReader reader)
        {
        }

        public virtual int CompareTo(object obj)
        {
            return 0;
        }

        public virtual IFixedLengthKey FactoryMethod(System.IO.BinaryReader reader)
        {
            return null;
        }
        public virtual IFixedLengthKey FactoryMethod()
        {
            return null;
        }
        public virtual void SetIntKey( int key )
        {
        }

        #endregion
    }

    public class FixedLengthKey_Int : FixedLengthKey
    {
        private int _key;
        private static IComparable _minKey = Int32.MinValue;
        private static IComparable _maxKey = Int32.MaxValue;

        public FixedLengthKey_Int( int key )
        {
            _key = key;
        }
        #region IFixedLengthKey Members
        public override IComparable Key
        {
            get{ return (IComparable) IntInternalizer.Intern( _key ); }
            set
            {
                if ( value == null )
                {
                    value = _minKey;
                }
                _key = (int)value;
            }
        }

        public override int KeySize
        {
            get
            {
                return 4;
            }
        }
        public override IComparable MinKey
        {
            get { return _minKey; }
        }
        public override IComparable MaxKey
        {
            get { return _maxKey; }
        }

        public override void Write(System.IO.BinaryWriter writer)
        {
            writer.Write( _key );
        }

        public override void Read(System.IO.BinaryReader reader)
        {
            _key = reader.ReadInt32();
        }

        public override int CompareTo(object obj)
        {
            int aKey = ((FixedLengthKey_Int)obj)._key;
            if ( _key < aKey ) return -1;
            if ( _key == aKey ) return 0;
            return 1;
        }

        public override IFixedLengthKey FactoryMethod(System.IO.BinaryReader reader)
        {
            FixedLengthKey_Int key = new FixedLengthKey_Int( _key );
            key.Read( reader );
            return key;
        }
        public override IFixedLengthKey FactoryMethod()
        {
            return new FixedLengthKey_Int( _key );
        }

        public override void SetIntKey( int key )
        {
            _key = key;
        }

        #endregion
    }

    public class FixedLengthKey_Double : FixedLengthKey
    {
        private double _key;
        public FixedLengthKey_Double( double key )
        {
            _key = key;
        }
        #region IFixedLengthKey Members

        public override IComparable Key
        {
            get{ return _key; }
            set
            {
                if ( value == null )
                {
                    value = 0.0;
                }
                _key = (double)value;
            }
        }
        public override int KeySize
        {
            get
            {
                return 8;
            }
        }
        public override IComparable MinKey
        {
            get { return Double.MinValue; }
        }
        public override IComparable MaxKey
        {
            get { return Double.MaxValue; }
        }

        public override void Write(System.IO.BinaryWriter writer)
        {
            writer.Write( _key );
        }

        public override void Read(System.IO.BinaryReader reader)
        {
            _key = reader.ReadDouble();
        }

        public override int CompareTo(object obj)
        {
            double aKey = ((FixedLengthKey_Double)obj)._key;
            if ( _key == aKey ) return 0;
            if ( _key < aKey ) return -1;
            return 1;
        }

        public override IFixedLengthKey FactoryMethod(System.IO.BinaryReader reader)
        {
            FixedLengthKey_Double key = new FixedLengthKey_Double( _key );
            key.Read( reader );
            return key;
        }
        public override IFixedLengthKey FactoryMethod()
        {
            return new FixedLengthKey_Double( _key );
        }

        #endregion
    }

    public class FixedLengthKey_DateTime : FixedLengthKey
    {
        private DateTime _key;
        private static IComparable _minKey = DateTime.MinValue;
        private static IComparable _maxKey = DateTime.MaxValue;

        public FixedLengthKey_DateTime( DateTime key )
        {
            _key = key;
        }
        #region IFixedLengthKey Members
        public override IComparable Key
        {
            get{ return _key; }
            set
            {
                if ( value == null )
                {
                    value = _minKey;
                }
                _key = (DateTime)value;
            }
        }

        public override int KeySize
        {
            get
            {
                return 8;
            }
        }
        public override IComparable MinKey
        {
            get { return _minKey; }
        }
        public override IComparable MaxKey
        {
            get { return _maxKey; }
        }

        public override void Write(System.IO.BinaryWriter writer)
        {
            writer.Write( _key.Ticks );
        }

        public override void Read(System.IO.BinaryReader reader)
        {
            long int64 = reader.ReadInt64();
            _key = new DateTime( int64 );
        }

        public override int CompareTo(object obj)
        {
            return _key.CompareTo( ((FixedLengthKey_DateTime)obj)._key );
        }

        public override IFixedLengthKey FactoryMethod(System.IO.BinaryReader reader)
        {
            FixedLengthKey_DateTime key = new FixedLengthKey_DateTime( _key );
            key.Read( reader );
            return key;
        }
        public override IFixedLengthKey FactoryMethod()
        {
            return new FixedLengthKey_DateTime( _key );
        }

        #endregion
    }

    public class FixedLengthKey_Compound : FixedLengthKey
    {
        private Compound _key;
        private FixedLengthKey _key1;
        private FixedLengthKey _key2;

        public FixedLengthKey_Compound( FixedLengthKey key1, FixedLengthKey key2 )
        {
            _key1 = key1;
            _key2 = key2;
            _key = new Compound( _key1.Key, _key2.Key );
        }
        #region IFixedLengthKey Members

        public override int KeySize
        {
            get
            {
                return _key1.KeySize + _key2.KeySize;
            }
        }
        public override IComparable MinKey
        {
            get { return null; }
        }
        public override IComparable MaxKey
        {
            get { return null; }
        }

        public override void Write(System.IO.BinaryWriter writer)
        {
            _key1.Key = _key._key1;
            _key1.Write( writer );
            _key2.Key = _key._key2;
            _key2.Write( writer );
        }

        public override void Read(System.IO.BinaryReader reader)
        {
            _key1.Read( reader );
            _key._key1 = _key1.Key;
            _key2.Read( reader );
            _key._key2 = _key2.Key;
        }
        public override IComparable Key
        {
            get{ return _key; }
            set { _key = (Compound)value; }
        }

        public override int CompareTo(object obj)
        {
            return _key.CompareTo( ((FixedLengthKey_Compound)obj)._key );
        }

        public override IFixedLengthKey FactoryMethod(System.IO.BinaryReader reader)
        {
            IFixedLengthKey key = FactoryMethod();
            key.Read( reader );
            return key;
        }
        public override IFixedLengthKey FactoryMethod()
        {
            _key1.Key = ((Compound)_key)._key1;
            _key2.Key = ((Compound)_key)._key2;
            return new FixedLengthKey_Compound( _key1, _key2 );
        }

        #endregion
    }

    public class FixedLengthKey_CompoundWithValue : FixedLengthKey
    {
        private CompoundAndValue _key;
        private FixedLengthKey _key1;
        private FixedLengthKey _key2;
        private FixedLengthKey _value;

        public FixedLengthKey_CompoundWithValue( FixedLengthKey key1, FixedLengthKey key2, FixedLengthKey value )
        {
            _key1 = key1;
            _key2 = key2;
            _value = value;
            _key = new CompoundAndValue( _key1.Key, _key2.Key, _value.Key );
        }
        #region IFixedLengthKey Members

        public override int KeySize
        {
            get
            {
                return _key1.KeySize + _key2.KeySize + _value.KeySize;
            }
        }
        public override IComparable MinKey
        {
            get { return null; }
        }
        public override IComparable MaxKey
        {
            get { return null; }
        }

        public override void Write(System.IO.BinaryWriter writer)
        {
            _key1.Key = _key._key1;
            _key1.Write( writer );
            _key2.Key = _key._key2;
            _key2.Write( writer );
            _value.Key = _key._value;
            _value.Write( writer );
        }

        public override void Read(System.IO.BinaryReader reader)
        {
            _key1.Read( reader );
            _key._key1 = _key1.Key;
            _key2.Read( reader );
            _key._key2 = _key2.Key;
            _value.Read( reader );
            _key._value = _value.Key;
        }
        public override IComparable Key
        {
            get{ return _key; }
            set { _key = (CompoundAndValue)value; }
        }
        public IComparable Value
        {
            get{ return _value.Key; }
            set { _value.Key = value; }
        }

        public override int CompareTo(object obj)
        {
            return _key.CompareTo( ((FixedLengthKey_CompoundWithValue)obj)._key );
        }

        public override IFixedLengthKey FactoryMethod(System.IO.BinaryReader reader)
        {
            IFixedLengthKey key = FactoryMethod();
            key.Read( reader );
            return key;
        }
        public override IFixedLengthKey FactoryMethod()
        {
            _key1.Key = ((CompoundAndValue)_key)._key1;
            _key2.Key = ((CompoundAndValue)_key)._key2;
            _value.Key = ((CompoundAndValue)_key)._value;
            return new FixedLengthKey_CompoundWithValue( _key1, _key2, _value );
        }

        #endregion
    }
}
