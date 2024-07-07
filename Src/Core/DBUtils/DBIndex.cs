// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using OmniaMeaBTree = DBIndex.OmniaMeaBTree;

namespace JetBrains.Omea.Database
{
    internal interface IDBIndex
    {
        void Flush();
        void Shutdown();

        void RefreshCacheSize();

        void SearchForRange( IntArrayList offsets, IComparable firstKey, IComparable secondKey );
        void SearchForRange( ArrayList offsets, IComparable firstKey, IComparable secondKey );
        IEnumerable SearchForRange( IComparable firstKey, IComparable secondKey );

        void GetAllOffsets( IntArrayList offsets );
        void GetAllOffsets( ArrayList offsets );
        IEnumerable GetAllKeys();
        int Count { get; }
        int AddIndexEntry( IComparable key, int offset );
        void RemoveIndexEntry( IComparable dbKey, int offset );
        void Clear();
        string Name{ get; }
        string FirstCompoundName { get; }
        string SecondCompoundName { get; }
        void Dump();
        int Version{ get; }
        bool Open();
        void Close();
        FixedLengthKey SearchKeySecond();
        FixedLengthKey SearchKeyValue();

        void Defragment( bool idleMode );

        int LoadedPages { get; }
        int PageSize { get; }
    }

    public class DBIndex : IDBIndex
    {
        private string _name = null;
        private string _fullName = null;
        private ITableDesign _tableDesign = null;

        private string _firstCompoundName = null;
        private string _secondCompoundName = null;
        private int _version;
        private IBTree _bTree = null;
        private bool _isOpen;

        private FixedLengthKey _fixedFactory;
        private FixedLengthKey _fixedFactory2;
        private FixedLengthKey _fixedFactoryValue;

        private FixedLengthKey _searchBegin;
        private FixedLengthKey _searchEnd;

        public static readonly int  _minimumCacheSize = 10;
        public static int           _cacheSizeMultiplier = 1;

        internal DBIndex( ITableDesign tableDesign, string name, FixedLengthKey fixedFactory,
            FixedLengthKey fixedFactory1, FixedLengthKey fixedFactory2, FixedLengthKey fixedFactoryValue )
        {
            Init( tableDesign, name );
            string[] names = name.Split( '#' );
            if ( names.Length == 2 )
            {
                _firstCompoundName = names[0];
                _secondCompoundName = names[1];
            }
            _version = _tableDesign.Version;

            _fullName =
                DBHelper.GetFullNameForIndex( tableDesign.Database.Path, tableDesign.Database.Name,
                tableDesign.Name, _name );

            _fixedFactory = fixedFactory;
            _fixedFactory2 = fixedFactory2;
            _fixedFactoryValue = fixedFactoryValue;
            _searchBegin = (FixedLengthKey)_fixedFactory.FactoryMethod();
            _searchEnd = (FixedLengthKey)_fixedFactory.FactoryMethod();

            if( !IBTree._bUseOldKeys )
            {
                Object key = _fixedFactory.Key;
                if( key is int || key is long || key is DateTime || key is Double )
                {
                    _bTree = new OmniaMeaBTree( _fullName, _fixedFactory );
                }
                else
                {
                    Compound compound = key as Compound;
                    if( compound != null )
                    {
                        if( compound._key1 is int )
                        {
                            if( compound._key2 is int )
                            {
                                _bTree = new OmniaMeaBTree( _fullName, _fixedFactory );
                            }
                            if( compound._key2 is DateTime )
                            {
                                _bTree = new OmniaMeaBTree( _fullName, _fixedFactory );
                            }
                        }
                    }
                    else
                    {
                        CompoundAndValue compval = key as CompoundAndValue;
                        if( compval != null )
                        {
                            if( compval._key1 is int )
                            {
                                if( compval._key2 is int )
                                {
                                    if( compval._value is int || compval._value is DateTime )
                                    {
                                        _bTree = new OmniaMeaBTree( _fullName, _fixedFactory );
                                    }
                                }
                                else if( compval._key2 is DateTime )
                                {
                                    if( compval._value is int )
                                    {
                                        _bTree = new OmniaMeaBTree( _fullName, _fixedFactory );
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if ( _bTree == null )
            {
                throw new InvalidOperationException( "Not supported key type!" );
                //_bTree = new BTree( _fullName, _fixedFactory );
            }
            _isOpen = false;
        }

        public void RefreshCacheSize()
        {
            _bTree.SetCacheSize( _minimumCacheSize * _cacheSizeMultiplier );
        }

        public FixedLengthKey SearchKeyValue( )
        {
            return _fixedFactoryValue;
        }

        public FixedLengthKey SearchKeySecond( )
        {
            return _fixedFactory2;
        }

        public bool Open()
        {
            if( !_isOpen )
            {
                _isOpen = true;
                RefreshCacheSize();
                return _bTree.Open();
            }
            return true;
        }
        public void Close()
        {
            if( _isOpen )
            {
                _bTree.Close();
                _isOpen = false;
            }
        }

        public void Clear()
        {
            Open();
            _bTree.Clear();
            RefreshCacheSize();
        }

        public int Version { get { return _version; } }

        private void Init( ITableDesign tableDesign, string name )
        {
            _name = name;
            _tableDesign = tableDesign;
        }

        public string FirstCompoundName { get { return _firstCompoundName; } }
        public string SecondCompoundName { get { return _secondCompoundName; } }
        public string Name{ get { return _name; } }

        public void Flush()
        {
            (_bTree as OmniaMeaBTree).Flush();
        }

        public void Shutdown()
        {
            Close();
        }

        public int Count { get { return _bTree.Count; } }
        public void GetAllOffsets( IntArrayList offsets )
        {
            _bTree.GetAllKeys( offsets );
        }
        public void GetAllOffsets( ArrayList keys_offsets )
        {
            _bTree.GetAllKeys( keys_offsets );
        }
        public IEnumerable GetAllKeys()
        {
            return _bTree.GetAllKeys();
        }

        public void SearchForRange( IntArrayList offsets, IComparable firstKey, IComparable secondKey )
        {
            _searchBegin.Key = firstKey;
            _searchEnd.Key = secondKey;
            _bTree.SearchForRange( _searchBegin, _searchEnd, offsets );
        }
        public void SearchForRange( ArrayList keys_offsets, IComparable firstKey, IComparable secondKey )
        {
            _searchBegin.Key = firstKey;
            _searchEnd.Key = secondKey;
            _bTree.SearchForRange( _searchBegin, _searchEnd, keys_offsets );
        }

        public IEnumerable SearchForRange( IComparable firstKey, IComparable secondKey )
        {
            _searchBegin.Key = firstKey;
            _searchEnd.Key = secondKey;
            return _bTree.SearchForRange( _searchBegin, _searchEnd );
        }

        public void Dump()
        {
            /*
            foreach ( DBKey id in m_index )
            {
                System.Diagnostics.Trace.WriteLine( ((int)id.Key).ToString() + "\t" + id.Offsets.Count.ToString() );
                if ( id.Offsets.Count > 1 )
                {
                    System.Diagnostics.Trace.WriteLine( "___________________" );
                    System.Diagnostics.Trace.WriteLine( "offsets" );
                    foreach ( long offset in id.Offsets )
                    {
                        System.Diagnostics.Trace.WriteLine( offset.ToString() );
                    }
                    System.Diagnostics.Trace.WriteLine( "___________________" );
                }
            }*/
        }
        public void RemoveIndexEntry( IComparable key, int offset )
        {
            _searchBegin.Key = key;
            _bTree.DeleteKey( _searchBegin, offset );
        }

        public int AddIndexEntry( IComparable key, int offset )
        {
            _searchBegin.Key = key;
            _bTree.InsertKey( _searchBegin, offset );
            return 0;
        }

        public void Defragment( bool idleMode )
        {
            IEnumerable keys = _bTree.GetAllKeys();
            OmniaMeaBTree defragmented = new OmniaMeaBTree( _fullName + ".defrag", _fixedFactory );
            defragmented.Open();
            try
            {
                defragmented.Clear();
                foreach( KeyPair pair in keys )
                {
                    if( idleMode && !Core.IsSystemIdle )
                    {
                        return;
                    }
                    defragmented.InsertKey( pair._key, pair._offset );
                }
            }
            finally
            {
                defragmented.Close();
            }
            _bTree.Close();
            File.Delete( _fullName );
            File.Move( _fullName + ".defrag", _fullName );
            _bTree = new OmniaMeaBTree( _fullName, _fixedFactory );
            _isOpen = false;
            Open();
        }

        public int LoadedPages
        {
            get { return _bTree.GetLoadedPages(); }
        }

        public int PageSize
        {
            get { return _bTree.GetPageSize(); }
        }
    }
}
