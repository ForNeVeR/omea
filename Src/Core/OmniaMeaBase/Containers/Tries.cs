// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.Containers
{
    ///<summary>
    /// <para>Trie of chars.</para>
    /// <para>Each node contains sorted by char list of subnodes.</para>
    /// <para>Chars are compared via comparer passed to constructor.</para>
    /// </summary>
    public class CharTrie : IEnumerable
    {
        ///<summary>
        /// Trie node
        /// If necessary, classes that inherit CharTrie can define specific nodes.
        /// To do this, inherit Node, define constructors and override NewNode()
        /// </summary>
        public class Node : IEnumerable
        {
            protected char          _char;
            protected ArrayList     _subNodes;
            protected static Node   _compareNode = new Node( '\0', 0 );

            #region IEnumerable implementation

            internal class SubNodesEnumerator : IEnumerator
            {
                public SubNodesEnumerator( Node root )
                {
                    _root = root;
                    Reset();
                }
                public void Reset()
                {
                    _current = _root;
                    _nodeStack.Clear();
                    _currentIndex = 0;
                }
                public object Current
                {
                    get {  return _current; }
                }
                public bool MoveNext()
                {
                    for( ; ; )
                    {
                        if( _current._subNodes != null && _currentIndex < _current._subNodes.Count )
                        {
                            _nodeStack.Push( _current );
                            _indexStack.Push( _currentIndex );
                            _current = ( Node ) _current._subNodes[ _currentIndex ];
                            _currentIndex = 0;
                            return true;
                        }
                        if( _indexStack.Count == 0 )
                            return false;
                        _current = ( Node )_nodeStack.Pop();
                        _currentIndex = ( int ) _indexStack.Pop();
                        ++_currentIndex;
                    }
                }
                private Node _root;
                private Node _current;
                private int  _currentIndex;
                private Stack _nodeStack = new Stack();
                private Stack _indexStack = new Stack();
            }

            public IEnumerator GetEnumerator()
            {
                return new SubNodesEnumerator( this );
            }

            #endregion

            /**
             * generic nodes' comparer is tuned by char comparer
             */
            public class Comparer : IComparer
            {
                private readonly IComparer _charComparer;
                public Comparer( IComparer charComparer )
                {
                    _charComparer = charComparer;
                }
                public int Compare( object x, object y )
                {
                    Node nodex = ( Node ) x;
                    Node nodey = ( Node ) y;
                    return ( _charComparer == null ) ? nodex._char - nodey._char : _charComparer.Compare( nodex._char, nodey._char );
                }
            }

            /**
             * Default nodes' comparer
             */
            public class DefaultComparer : IComparer
            {
                private static readonly IComparer _instance = new DefaultComparer();

                private DefaultComparer() {}

                public int Compare( object x, object y )
                {
                    Node nodex = ( Node ) x;
                    Node nodey = ( Node ) y;
                    return nodex._char - nodey._char;
                }

                public static IComparer Instance
                {
                    get { return _instance; }
                }
            }

            public Node( char aChar )
            {
                _char = aChar;
            }
            public Node( char aChar, int subNodesCapacity )
            {
                _char = aChar;
                if( subNodesCapacity > 0 )
                    _subNodes = new ArrayList( subNodesCapacity );
            }

            public virtual Node NewNode( char aChar, Node parent, object context )
            {
                return new Node( aChar );
            }

            public Node SubNode( char aChar, IComparer nodeComparer )
            {
                if( _subNodes == null )
                    return null;
                _compareNode._char = aChar;
                int index = _subNodes.BinarySearch( _compareNode, nodeComparer );
                return ( index < 0 ) ? null : (( Node ) _subNodes[ index ] );
            }

            public Node Insert( char aChar, IComparer nodeComparer )
            {
                if( _subNodes == null )
                {
                    _subNodes = new ArrayList( 1 );
                    Node newNode = NewNode( aChar, this, null );
                    _subNodes.Add( newNode );
                    return newNode;
                }
                else
                {
                    _compareNode._char = aChar;
                    int index = _subNodes.BinarySearch( _compareNode, nodeComparer );
                    if( index < 0 )
                    {
                        index = ~index;
                        _subNodes.Insert( index, NewNode( aChar, this, null ) );
                    }
                    return ( Node ) _subNodes[ index ];
                }
            }

			/// <summary>
			/// Provides access to the char value of this node. Used primarily for comparison needs.
			/// </summary>
			public char Value
			{
				get
				{
					return _char;
				}
			}

            #region saving/loading to/from a binary stream

            internal virtual void Save( BinaryWriter stream )
            {
                stream.Write( _char );
                int subNodes = ( _subNodes == null ) ? 0 : _subNodes.Count;
                stream.Write( (ushort)subNodes );
                if( subNodes > 0 )
                {
                    foreach( Node node in _subNodes )
                        node.Save( stream );
                }
            }

            internal virtual void Load( BinaryReader stream )
            {
                _char = stream.ReadChar();
                int subNodes = ( int )stream.ReadUInt16();
                if( subNodes > 0 )
                {
                    _subNodes = new ArrayList( subNodes );
                    for (int i = 0 ; i < subNodes; ++i )
                    {
                        Node newNode = NewNode( '\0', this, null );
                        newNode.Load( stream );
                        _subNodes.Add( newNode );
                    }
                }
            }

            #endregion
        }

        public CharTrie( IComparer charComparer )
        {
            _nodeComparer = (charComparer != null) ? new Node.Comparer( charComparer ) :
                                                     Node.DefaultComparer.Instance;
            Clear();
        }

        public void Clear()
        {
            _root = new Node( '\0', 128 );
        }

        public Node Root
        {
            get { return _root; }
        }

        public Node Add( string str )
        {
            Node currNode = _root;
            for( int i = 0; i < str.Length; ++i )
                currNode = currNode.Insert( str[ i ], _nodeComparer );
            return currNode;
        }

        public Node Add( string str, int index, int count )
        {
            Node currNode = _root;
            for( int i = index; count > 0 && i < str.Length; ++i, --count )
                currNode = currNode.Insert( str[ i ], _nodeComparer );
            return currNode;
        }

        /**
         * returns length of the largest string in the trie that matches the given
         */
        public int GetMatchingLength( string str )
        {
            Node currNode = _root;
            int i = 0;
            for( ; i < str.Length; ++i )
                if( ( currNode = currNode.SubNode( str[ i ], _nodeComparer )) == null )
                    break;
            return i;
        }

        public int GetMatchingLength( string str, int index, int count )
        {
            Node currNode = _root;
            int i = index;
            for( ; count > 0 && i < str.Length; ++i, --count )
                if( ( currNode = currNode.SubNode( str[ i ], _nodeComparer )) == null )
                    break;
            return i - index;
        }

        /**
         * returns node of the largest string in the trie that matches the given
         */
        public Node GetMatchingNode( string str )
        {
            return GetMatchingNode( str, 0, str.Length );
        }

        public Node GetMatchingNode( string str, int index, int count )
        {
            Node currNode = _root;
            int i = index;
            for( ; count > 0 && i < str.Length; ++i, --count )
            {
                Node nextNode = currNode.SubNode( str[ i ], _nodeComparer );
                if( nextNode == null )
                    break;
                currNode = nextNode;
            }
            return currNode;
        }

        #region IEnumerable implementation

        public IEnumerator GetEnumerator()
        {
            return new Node.SubNodesEnumerator( _root );
        }

        #endregion

        #region saving/loading to/from a binary stream

        public void Save( BinaryWriter stream )
        {
            _root.Save( stream );
        }

        public void Load( BinaryReader stream )
        {
            Clear();
            _root.Load( stream );
        }

        #endregion

        protected internal IComparer    _nodeComparer;
        protected internal Node         _root;
    }


    /**
     * Trie of bytes located in external memory
     */
    public class ExternalTrie: IDisposable
    {
        public const int _recordSize = 13;
        public const int _nodesCacheSize = 4095;
        public const int _stringCachesize = 2047;

        public ExternalTrie( string fileName )
        {
            Init( fileName );
        }

        public ExternalTrie( string fileName, ICachingStrategy strategy )
        {
            _cachingStrategy = strategy;
            Init( fileName );
        }

        public string FileName
        {
            get { return _fileName; }
        }

        public bool IsTrieComplete
        {
            get { return _isTrieComplete; }
        }

        public bool IsDirty
        {
            get { return _isDirty; }
        }

        public int FirstFreeIndex
        {
            get { return (int) ( _storage.Length / _recordSize ); }
        }

        public int NodesCacheSize
        {
            get { return _nodesCache.Size; }
            set { _nodesCache = new IntObjectCache( value ); }
        }

        public void Flush()
        {
            if( _isDirty )
            {
                _storage.Flush();
                _isDirty = false;
            }
        }

        /**
         * returns true if the string was actually added
         * otherwise the string is already located the external trie
         */
        public bool AddString( string str, out int resultIndex )
        {
            resultIndex = -1;
            if( str.Length == 0 )
            {
                return false;
            }
            object key = _stringCache.TryKey( str );
            if( key != null )
            {
                resultIndex = (int) key;
                return true;
            }
            byte[] strBytes = Encoding.UTF8.GetBytes( str );
            int lastIndex = strBytes[ 0 ];
            TrieNode lastNode = _firstByteNodes[ lastIndex ];
            bool result = false;

            for( int i = 1; i < strBytes.Length; ++i )
            {
                byte currentByte = strBytes[ i ];
                int index = lastNode._firstChild;
                TrieNode currentNode;
                for( ; index != 0; )
                {
                    currentNode = LoadNodeByIndex( index );
                    if( currentNode == null )
                    {
                        index = 0;
                        break;
                    }
                    if( currentNode._byte == currentByte )
                    {
                        lastNode = currentNode;
                        lastIndex = index;
                        break;
                    }
                    index = currentNode._sibling;
                }
                if( index == 0 )
                {
                    TrieNode newNode = AllocNode();
                    newNode._byte = currentByte;
                    newNode._firstChild = 0;
                    newNode._sibling = lastNode._firstChild;
                    newNode.Parent = lastIndex;
                    newNode.IsToken = (i == strBytes.Length - 1);
                    int newIndex = SaveNode2End( newNode );
                    lastNode._firstChild = newIndex;
                    SaveNodeByIndex( lastNode, lastIndex );
                    lastNode = newNode;
                    lastIndex = newIndex;
                    result = true;
                }
            }
            resultIndex = lastIndex;
            _stringCache.CacheObject( str, lastIndex );
            return result;
        }

        /**
         * gets index of a string in the external trie
         * if index is more than or equal to zero, the string is located in the trie
         */
        public int GetStringIndex( string str )
        {
            if( str.Length == 0 )
            {
                return -1;
            }

            object key = _stringCache.TryKey( str );
            if( key != null )
            {
                return (int) key;
            }

            byte[] strBytes = Encoding.UTF8.GetBytes( str );
            int lastIndex = strBytes[ 0 ];
            TrieNode lastNode = _firstByteNodes[ lastIndex ];

            for( int i = 1; i < strBytes.Length; ++i )
            {
                byte currentByte = strBytes[ i ];
                int index = lastNode._firstChild;
                TrieNode currentNode;
                for( ; ; )
                {
                    if( index == 0 )
                    {
                        return -1;
                    }
                    currentNode = LoadNodeByIndex( index );
                    if( currentNode == null )
                    {
                        return -1;
                    }
                    if( currentNode._byte == currentByte )
                    {
                        lastNode = currentNode;
                        lastIndex = index;
                        break;
                    }
                    index = currentNode._sibling;
                }
            }

            _stringCache.CacheObject( str, lastIndex );

            return lastIndex;
        }

        public string GetStringByIndex( int index )
        {
            ArrayList bytes = new ArrayList();
            TrieNode node;
            while( index != 0 )
            {
                node = LoadNodeByIndex( index );
                if( node == null )
                {
                    return null;
                }
                bytes.Insert( 0, node._byte );
                index = node.Parent;
            }
            return ( bytes.Count > 0 ) ? Encoding.UTF8.GetString( (byte[]) bytes.ToArray( typeof( byte ) ) ) : null;
        }

        public ArrayList GetMatchingStrings( string wildcard, bool tokensOnly )
        {
            ArrayList result = new ArrayList();
            IntArrayList indexes = new IntArrayList();
            if( wildcard.EndsWith( "*" ) )
            {
                wildcard = wildcard.TrimEnd( '*' );
                GetSubtree( GetStringIndex( wildcard ), indexes, tokensOnly );
                for( int i = 0; i < indexes.Count; ++i )
                {
                    string str = GetStringByIndex( indexes[ i ] );
                    if( str != null )
                    {
                        result.Add( str );
                    }
                }
            }
            else if( wildcard.EndsWith( "?" ) )
            {
                int level = 1;
                int wcLength = wildcard.Length;
                while( ( wildcard = wildcard.Remove( wcLength - level, 1 ) ).EndsWith( "?" ) )
                {
                    ++level;
                }
                GetSubtree( GetStringIndex( wildcard ), indexes, tokensOnly );
                for( int i = 0; i < indexes.Count; ++i )
                {
                    string token = GetStringByIndex( indexes[ i ] );
                    if( token != null && token.Length == wcLength )
                    {
                        result.Add( token );
                    }
                }
            }
            else
            {
                result.Add( wildcard );
            }
            return result;
        }

        private void GetSubtree( int index, IntArrayList wards, bool tokensOnly )
        {
            if( index >= 0 )
            {
                TrieNode node = LoadNodeByIndex( index );
                if( !tokensOnly || node.IsToken )
                {
                    wards.Add( index );
                }
                index = node._firstChild;
                while( index != 0 )
                {
                    GetSubtree( index, wards, tokensOnly );
                    node = LoadNodeByIndex( index );
                    index = node._sibling;
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Flush();
            _storage.Close();
            Trace.WriteLine( "ExternalTrie(" + _fileName + "): nodes cache hit rate = " +
                (int) ( _nodesCache.HitRate() * 100 ) + '%' );
            Trace.WriteLine( "ExternalTrie(" + _fileName + "): strings cache hit rate = " +
                (int) ( _stringCache.HitRate() * 100 ) + '%' );
        }

        #endregion

        #region implementation details

        private class TrieNode
        {
            public byte _byte;
            public int  _firstChild;
            public int  _sibling;
            public int  _parent;

            public bool IsToken
            {
                get { return ( _parent & 1 ) == 1; }
                set
                {
                    if( value )
                    {
                        _parent |= 1;
                    }
                    else
                    {
                        _parent = ( _parent >> 1 ) << 1;
                    }
                }
            }

            public int Parent
            {
                get { return _parent >> 1; }
                set
                {
                    _parent = value << 1;
                }
            }
        }

        private void Init( string fileName )
        {
            _fileName = fileName;
            FileStream stream = new FileStream( fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 256 );
            if( _cachingStrategy != null )
            {
                _storage = new CachedStream( stream, _cachingStrategy );
            }
            else
            {
                _storage = new CachedStream( stream, 1 << 18 );
            }
            _writer = new BinaryWriter( _storage );
            _reader = new BinaryReader( _storage );
            _firstByteNodes = new TrieNode[ 256 ];
            for( int i = 0; i < 256; ++i )
            {
                TrieNode node = new TrieNode();
                _isTrieComplete = LoadNode( node );
                if( !_isTrieComplete )
                {
                    node._byte = (byte) i;
                    node.Parent = 0;
                    node.IsToken = false;
                    SaveNode( node );
                    Flush();
                }
                _firstByteNodes[ i ] = node;
            }
            _stringCache = new ObjectCache( _stringCachesize );
            _nodesCache = new IntObjectCache( _nodesCacheSize );
            _nodesCache.ObjectRemoved += _nodesCache_ObjectRemoved;
            _isDirty = false;
        }

        /**
         * Loads node from current location
         */
        private bool LoadNode( TrieNode node )
        {
            try
            {
                node._byte = (byte)_storage.ReadByte();
                node._firstChild = _reader.ReadInt32();
                node._sibling = _reader.ReadInt32();
                node._parent = _reader.ReadInt32();
            }
            catch( EndOfStreamException )
            {
                return false;
            }
            return true;
        }

        /**
         * Loads node by specified index
         */
        private TrieNode LoadNodeByIndex( int index )
        {
            TrieNode node = (TrieNode) _nodesCache.TryKey( index );
            if( node == null )
            {
                if( Seek2NodeByIndex( index ) )
                {
                    node = AllocNode();
                    LoadNode( node );
                    _nodesCache.CacheObject( index, node );
                }
            }
            return node;
        }

        /**
         * Saves specified node to current location
         */
        private void SaveNode( TrieNode node )
        {
            _storage.WriteByte( node._byte );
            _writer.Write( node._firstChild );
            _writer.Write( node._sibling );
            _writer.Write( node._parent );
            _isDirty = true;
        }

        /**
         * Seeks to the specified index location
         */
        private bool Seek2NodeByIndex( int index )
        {
            long position = ( (long) index ) * _recordSize;
            if( position >= 0 && _storage.Length >= position )
            {
                _storage.Position = position;
                return true;
            }
            return false;
        }

        /**
         * Saves node by its index
         */
        private void SaveNodeByIndex( TrieNode node, int index )
        {
            _nodesCache.Remove( index );
            Seek2NodeByIndex( index );
            SaveNode( node );
        }

        /**
         * saves new node to end and returns its index
         */
        private int SaveNode2End( TrieNode node )
        {
            _storage.Seek( 0, SeekOrigin.End );
            SaveNode( node );
            return FirstFreeIndex - 1;
        }

        private TrieNode AllocNode()
        {
            if( _freeNode == null )
            {
                return new TrieNode();
            }
            TrieNode result = _freeNode;
            _freeNode = null;
            return result;
        }

        private void _nodesCache_ObjectRemoved( object sender, IntObjectCacheEventArgs e )
        {
            _freeNode = (TrieNode) e.Object;
        }

        #endregion

        private string              _fileName;
        private Stream              _storage;
        private BinaryWriter        _writer;
        private BinaryReader        _reader;
        private TrieNode[]          _firstByteNodes;
        private ObjectCache         _stringCache;
        private IntObjectCache      _nodesCache;
        private TrieNode            _freeNode;
        private bool                _isTrieComplete;
        private bool                _isDirty;
        private ICachingStrategy    _cachingStrategy;
    }
}
