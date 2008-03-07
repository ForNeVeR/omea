/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;

namespace JetBrains.Omea.Containers
{
    internal class RBColor
    {
        public const byte Black = 0;
        public const byte Red = 1;
    }

    public interface INodeFetcherIndexAccess
    {
        void NodeFetched( RBNodeIndexAccess node );
    }

    public interface INodeFetcher
    {
        void NodeFetched( RBNodeBase node );
    }

    public abstract class RBNodeBase
    {
        internal RBNodeBase _left;
        internal RBNodeBase _right;
        internal RBNodeBase _parent;
        internal byte _color;

        public abstract object Key { get; set; }
    }

    public class RedBlackTree
    {
        private RBNodeBase _root;
        private int _count = 0;
        private IComparer _comparer;
        internal RBNodeBase _null;

        private class RBNode : RBNodeBase
        {
            private object _key;

            public override object Key
            {
                get { return _key; }
                set { _key = value; }
            }

            internal RBNode()
            {
                _right = _left = _parent = this;
            }
            internal RBNode( object key, RBNodeBase NullNode )
            {
                _key = key;
                _right = _left = _parent = NullNode;
            }
        }

        public RedBlackTree()
        {
            _null = new RBNode();
            _null.Key = "sentinel";
            _null._left = _null;
            _null._right = _null;
            _null._parent = _null;
            _root = _null;
        }

        public RedBlackTree( IComparer comparer )
            : this()
        {
            _comparer = comparer;
        }

        public int Count{ get { return _count; } }

        public RBNodeBase Search( object searchKey )
        {
            RBNodeBase x = _root;
            int iCompare;
            while( x != _null )
            {
                iCompare = CompareKeys( searchKey, x.Key );
                if ( iCompare < 0 )
                {
                    x = x._left;
                }
                else if ( iCompare > 0 )
                {
                    x = x._right;
                }
                else
                {
                    return x;
                }
            }
            return null;
        }

        public RBNodeBase GetEqualOrLess( object searchKey )
        {
            if ( _root == _null ) return null;

            RBNodeBase x = _root;
            RBNodeBase prev = _null;
            RBNodeBase found = null;
            int iCompare;
            while( x != _null )
            {
                iCompare = CompareKeys( searchKey, x.Key );
                if( iCompare == 0 )
                {
                    found = x;
                }
                if( iCompare <= 0 )
                {
                    x = x._left;
                }
                else
                {
                    prev = x;
                    x = x._right;
                }
            }
            if ( found != null ) return found;
            if ( prev == _null ) 
            {
                return null;
            }
            return prev;
        }

        public RBNodeBase GetMaximumLess( object searchKey )
        {
            if ( _root == _null ) return null;

            RBNodeBase x = _root;
            RBNodeBase prev = _null;
            int iCompare;
            while( x != _null )
            {
                iCompare = CompareKeys( searchKey, x.Key );
                if ( iCompare <= 0 )
                {
                    x = x._left;
                }
                else
                {
                    prev = x;
                    x = x._right;
                }
            }
            if ( prev == _null ) 
            {
                return null;
            }
            return prev;
        }

        public RBNodeBase GetEqualOrMore( object searchKey, object key2 )
        {
            if ( _root == _null ) return null;

            RBNodeBase x = _root;
            RBNodeBase next = _null;
            int iCompare;
            while( x != _null )
            {
                iCompare = CompareKeys( searchKey, x.Key );
                if( iCompare == 0 )
                    return x;
                if ( iCompare > 0 )
                {
                    x = x._right;
                }
                else
                {
                    next = x;
                    x = x._left;
                }
            }
            if ( next == _null ) 
            {
                return null;
            }
            return next;
        }

        public void RB_Delete( RBNodeBase node )
        {
            if ( node == null || _count == 0 ) return;
            _count--;
            RBNodeBase z = node;
            RBNodeBase y;
            if ( z._left == _null || z._right == _null )
            {
                y = z;
            }
            else
            {
                y = GetSuccessor( z );
            }
            RBNodeBase x;
            if ( y._left != _null )
            {
                x = y._left;
            }
            else 
            {
                x = y._right;
            }
            x._parent = y._parent;
            if ( y._parent == _null )
            {
                _root = x;
            }
            else if ( y == y._parent._left )
            {
                y._parent._left = x;
            }
            else
            {
                y._parent._right = x;
            }
            if ( y != z )
            {
                z.Key = y.Key;
                // copy of additional data
            }
            if ( y._color == RBColor.Black )
            {
                RB_Delete_Fixup( x );
            }
        }

        public void RB_Delete( IComparable key )
        {
            RB_Delete( Search( key ) );
        }

        public object GetMaximum(  )
        {
            if ( _count == 0 ) return null;
            return GetMaximum( _root ).Key;
        }
        public RBNodeBase GetRootNode(  )
        {
            if ( _root == _null ) return null;
            return _root;
        }

        public RBNodeBase GetMaximumNode(  )
        {
            if ( _count == 0 ) return null;
            return GetMaximum( _root );
        }

        public object GetMinimum( )
        {
            if ( _count == 0 ) return null;
            return GetMinimum( _root ).Key;
        }

        public RBNodeBase GetMinimumNode( )
        {
            if ( _count == 0 ) return null;
            return GetMinimum( _root );
        }

        public void RB_Insert( object key )
        {
            RBNodeBase node = new RBNode( key, _null );
            RB_InsertNode( node );
        }

        public void RB_InsertNode( RBNodeBase node )
        {
            Insert( node );
            RB_Insert( node );
            _count++;
        }

        public void GetRange( object key1, object key2 )
        {
        }

        private void RB_Delete_Fixup( RBNodeBase x )
        {
            while ( x != _root && x._color == RBColor.Black )
            {
                RBNodeBase w;
                if ( x == x._parent._left )
                {
                    w = x._parent._right;
                    if ( w._color == RBColor.Red )
                    {
                        w._color = RBColor.Black;
                        x._parent._color = RBColor.Red;
                        LeftRotate( x._parent );
                        w = x._parent._right;
                    }
                    if ( w._left._color == RBColor.Black && w._right._color == RBColor.Black )
                    {
                        w._color = RBColor.Red;
                        x = x._parent;
                    }
                    else if ( w._right._color == RBColor.Black )
                    {
                        w._left._color = RBColor.Black;
                        w._color = RBColor.Red;
                        RightRotate( w );
                        w = x._parent._right;
                    }
                    else
                    {
                        w._color = x._parent._color;
                        x._parent._color = RBColor.Black;
                        w._right._color = RBColor.Black;
                        LeftRotate( x._parent );
                        x = _root;
                    }
                }
                else
                {
                    w = x._parent._left;
                    if ( w._color == RBColor.Red )
                    {
                        w._color = RBColor.Black;
                        x._parent._color = RBColor.Red;
                        RightRotate( x._parent );
                        w = x._parent._left;
                    }
                    if ( w._right._color == RBColor.Black && w._left._color == RBColor.Black )
                    {
                        w._color = RBColor.Red;
                        x = x._parent;
                    }
                    else if ( w._left._color == RBColor.Black )
                    {
                        w._right._color = RBColor.Black;
                        w._color = RBColor.Red;
                        LeftRotate( w );
                        w = x._parent._left;
                    }
                    else
                    {
                        w._color = x._parent._color;
                        x._parent._color = RBColor.Black;
                        w._left._color = RBColor.Black;
                        RightRotate( x._parent );
                        x = _root;
                    }
                }
            }
            x._color = RBColor.Black;
        }

        private RBNodeBase GetMaximum( RBNodeBase x )
        {
            if ( _root == _null ) return null;
            while ( x._right != _null )
            {
                x = x._right;
            }
            return x;
        }
        private RBNodeBase GetMinimum( RBNodeBase x )
        {
            if ( _root == _null ) return null;
            while ( x._left != _null )
            {
                x = x._left;
            }
            return x;
        }

        public RBNodeBase GetNext( RBNodeBase x )
        {
            RBNodeBase node = GetSuccessor( x );
            if ( node != _null )
            {
                return node;
            }
            else
            {
                return null;
            }
        }

        public RBNodeBase GetPrevious( RBNodeBase x )
        {
            RBNodeBase node = GetPredecessor( x );
            if ( node != _null )
            {
                return node;
            }
            else
            {
                return null;
            }
        }

        public RBNodeBase GetPredecessor( RBNodeBase x )
        {
            if ( x._left != _null )
            {
                return GetMaximum( x._left );
            }
            RBNodeBase y = x._parent;
            while ( y != _null && x == y._left )
            {
                x = y;
                y = y._parent;
            }
            return y;
        }

        public RBNodeBase GetSuccessor( RBNodeBase x )
        {
            if ( x._right != _null )
            {
                return GetMinimum( x._right );
            }
            RBNodeBase y = x._parent;
            while ( y != _null && x == y._right )
            {
                x = y;
                y = y._parent;
            }
            return y;
        }

        public bool SearchOrInsert( object insertedKey, out RBNodeBase foundOrNew )
        {
            RBNodeBase current = _root;
            RBNodeBase parent = _null;
            while ( current != _null )
            {
                parent = current;
                int iCompare = CompareKeys( insertedKey, current.Key );
                if ( iCompare < 0 )
                {
                    current = current._left;
                }
                else if ( iCompare > 0 )
                {
                    current = current._right;
                }
                else
                {
                    foundOrNew = current;
                    return true;
                }
            }
            _count++;
            foundOrNew = new RBNode( insertedKey, _null );
            foundOrNew._parent = parent;
            if ( parent == _null )
            {
                _root = foundOrNew;
                RB_Insert( foundOrNew );
                return false;
            }
            if ( CompareKeys( insertedKey, parent.Key ) < 0 )
            {
                parent._left = foundOrNew;
            }
            else
            {
                parent._right = foundOrNew;
            }
            RB_Insert( foundOrNew );
            return false;
        }

        private void Insert( RBNodeBase node )
        {
            RBNodeBase current = _root;
            RBNodeBase parent = _null;
            object insertedKey = node.Key;
            while ( current != _null )
            {
                parent = current;
                if ( CompareKeys( insertedKey, current.Key ) < 0 )
                {
                    current = current._left;
                }
                else
                {
                    current = current._right;
                }
            }
            node._parent = parent;
            if ( parent == _null )
            {
                _root = node;
                return;
            }
            if ( CompareKeys( insertedKey, parent.Key ) < 0 )
            {
                parent._left = node;
            }
            else
            {
                parent._right = node;
            }
        }


        private void RB_Insert( RBNodeBase x )
        {
            x._color = RBColor.Red;
            while ( x != _root && x._parent._color == RBColor.Red )
            {
                RBNodeBase xParent = x._parent;
                RBNodeBase xParentParent = xParent._parent;
                if ( xParent == xParentParent._left )
                {
                    RBNodeBase y = xParentParent._right;
                    if ( /*y != _null && */y._color == RBColor.Red )
                    {
                        xParent._color = RBColor.Black;
                        y._color = RBColor.Black;
                        xParentParent._color = RBColor.Red;
                        x = xParentParent;
                    }
                    else if ( x == xParent._right )
                    {
                        x = xParent;
                        LeftRotate( x );
                    }
                    else
                    {
                        xParent._color = RBColor.Black;
                        xParentParent._color = RBColor.Red;
                        RightRotate( xParentParent );
                    }
                }
                else
                {
                    RBNodeBase y = xParentParent._left;
                    if ( /*y != _null && */y._color == RBColor.Red )
                    {
                        xParent._color = RBColor.Black;
                        y._color = RBColor.Black;
                        xParentParent._color = RBColor.Red;
                        x = xParentParent;
                    }
                    else if ( x == xParent._left )
                    {
                        x = xParent;
                        RightRotate( x );
                    }
                    else
                    {
                        xParent._color = RBColor.Black;
                        xParentParent._color = RBColor.Red;
                        LeftRotate( xParentParent );
                    }
                }
            }
            _root._color = RBColor.Black;
        }

        private void LeftRotate( RBNodeBase x )
        {
            RBNodeBase y = x._right;
            x._right = y._left;

            if ( y._left != _null )
            {
                y._left._parent = x;
            }
            y._parent = x._parent;
            RBNodeBase xParent = x._parent;
            if ( xParent == _null )
            {
                _root = y;
            }
            else if ( x == xParent._left )
            {
                xParent._left = y;
            }
            else
            {
                xParent._right = y;
            }
            y._left = x;
            x._parent = y;
        }

        private void RightRotate( RBNodeBase x )
        {
            RBNodeBase y = x._left;
            x._left = y._right;

            if ( y._right != _null )
            {
                y._right._parent = x;
            }
            y._parent = x._parent;
            RBNodeBase xParent = x._parent;
            if ( xParent == _null )
            {
                _root = y;
            }
            else if ( x == xParent._right )
            {
                xParent._right = y;
            }
            else
            {
                xParent._left = y;
            }
            y._right = x;
            x._parent = y;
        }

        public void InOrderEnum( INodeFetcher fetcher )
        {
            if ( _root == _null || fetcher == null ) return;
            InOrderEnum( fetcher, _root );
        }
        private void InOrderEnum( INodeFetcher fetcher, RBNodeBase node )
        {
            fetcher.NodeFetched( node );

            if ( node._left != _null )
            {
                InOrderEnum( fetcher, node._left );
            }
            if ( node._right != _null )
            {
                InOrderEnum( fetcher, node._right );
            }
        }

        public void InorderPrint()
        {
            System.Diagnostics.Trace.WriteLine("_________________________");
            InorderPrint( _root );
        }

        private void InorderPrint( RBNodeBase node )
        {
            /*
            if ( node != _null )
            {
                if ( node.Key.Equals( _root.Key ) )
                {
                    Trace.WriteLine("___ root ___");
                    Console.WriteLine("___ root ___");
                }
                node.Print();
                if ( node.Key.Equals( _root.Key ) )
                {
                    Trace.WriteLine("___ root ___");
                    Console.WriteLine("___ root ___");
                }

                if ( node._left != _null )
                {
                    Trace.Indent();
                    Trace.WriteLine("go left ___ begin");
                    Console.WriteLine("go left ___ begin");
                    InorderPrint( node._left );
                    Trace.WriteLine("go left ___ end");
                    Console.WriteLine("go left ___ end");
                    Trace.Unindent();
                }
                if ( node._right != _null )
                {
                    Trace.Indent();
                    Trace.WriteLine("go right ___ begin");
                    Console.WriteLine("go right ___ begin");
                    InorderPrint( node._right );
                    Trace.WriteLine("go right ___ end");
                    Console.WriteLine("go right ___ end");
                    Trace.Unindent();
                }
            }
            */
        }

        private int CompareKeys( object key1, object key2 )
        {
            if( _comparer != null )
            {
                return _comparer.Compare( key1, key2 );
            }
            return ( (IComparable) key1 ).CompareTo( key2 );
        }
    }


    public class RBNodeIndexAccess
    {
        internal RBNodeIndexAccess _left;
        internal RBNodeIndexAccess _right;
        internal RBNodeIndexAccess _parent;
        internal byte _color;
        internal IComparable _key;
        internal int _size;

        public IComparable Key
        {
            get { return _key; }
            set { _key = value; }
        }

        internal static RBNodeIndexAccess GetNull()
        {
            return new RBNodeIndexAccess();
        }
        private RBNodeIndexAccess()
        {
            _size = 0;
            _parent = this;
            _left = this;
            _right = this;
        }
        internal RBNodeIndexAccess( IComparable key, RBNodeIndexAccess NullNode )
        {
            _key = key;
            _size = 1;
            _parent = NullNode;
            _left = NullNode;
            _right = NullNode;
        }
        public void Print()
        {
            if ( _key != null )
            {
                Trace.WriteLine( _key.ToString() + " " + _color.ToString() + " " + _size.ToString() );
            }
        }
        public string GetString()
        {
            if ( _key == null ) return string.Empty;
            return _key.ToString() + " " + _color.ToString() + " " + _size.ToString();
        }
    }

    public class RBRange
    {
        private int _count = 0; 
        internal RBRange( RedBlackTreeWithIndexAccess tree, IComparable key1, IComparable key2 )
        {
            RBNodeIndexAccess node1 = tree.Search( key1 );
            int index1 = tree.OS_Rank( node1 );
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }
    }

    public class RedBlackTreeWithIndexAccess
    {
        private RBNodeIndexAccess _root;
        private int _count = 0;
        internal RBNodeIndexAccess _null;

        public RedBlackTreeWithIndexAccess( )
        {
            _null = RBNodeIndexAccess.GetNull();
            _null.Key = "sentinel";
            _null._size = 0;
            _null._left = _null;
            _null._right = _null;
            _null._parent = _null;
            _root = _null;
        }

        public int Count{ get { return _count; } }

        public RBNodeIndexAccess Search( IComparable searchKey )
        {
            RBNodeIndexAccess x = _root;
            int iCompare;
            while( x != _null )
            {
                iCompare = searchKey.CompareTo( x.Key );
                if( iCompare == 0 )
                    return x;
                x = ( iCompare < 0 ) ? x._left : x._right;
            }
            return null;
        }

        public int SearchForIndex( IComparable k )
        {
            if ( _root == _null ) return -1;

            RBNodeIndexAccess found;
            int res = SearchIndex( k, out found );

            if ( res == 0 )
            {
                return OS_Rank( found.Key );
            }
            else if ( res == 1 ) 
            {
                return -OS_Rank( found.Key ) - 2;
            }
            else return -OS_Rank( found.Key ) - 1;
        }

        public RBNodeIndexAccess GetEqualOrMore( IComparable searchKey, IComparable key2 )
        {
            if ( _root == _null ) return null;

            RBNodeIndexAccess x = _root;
            RBNodeIndexAccess next = _null;
            //IComparable searchKey = (IComparable)key1;
            int iCompare;
            while( x != _null )
            {
                iCompare = searchKey.CompareTo( x.Key );
                if( iCompare == 0 )
                    return x;
                if ( iCompare > 0 )
                {
                    x = x._right;
                }
                else
                {
                    next = x;
                    x = x._left;
                }
            }
            if ( next == _null ) 
            {
                return null;
            }
            return next;
        }

        public int OS_Rank( RBNodeIndexAccess node )
        {
            if ( node == null || node._left == null ) return -1;

            int r = node._left._size + 1;
            RBNodeIndexAccess y = node;
            while ( y != _root )
            {
                if ( y == y._parent._right )
                {
                    r = r + y._parent._left._size + 1;
                }
                y = y._parent;
            }
            return r - 1;
        }

        public int OS_Rank( IComparable k )
        {
            RBNodeIndexAccess x = Search( k );
            return OS_Rank( x );
        }

        public object OS_Select( int index )
        {
            if ( index < 0 || index > _count ) return null;
            index++;
            RBNodeIndexAccess param = _root;
            int	r = param._left._size + 1;

            while ( index != r )
            {
                if ( index < r )
                {
                    param = param._left;
                }
                else
                {
                    param = param._right;
                    index = index - r;
                }
                r = param._left._size + 1;
            }
            return param.Key;
        }

        public void RB_Delete( RBNodeIndexAccess node )
        {
            if ( node == null || _count == 0 ) return;
            _count--;
            RBNodeIndexAccess z = node;
            RBNodeIndexAccess y;
            if ( z._left == _null || z._right == _null )
            {
                y = z;
            }
            else
            {
                y = GetSuccessor( z );
            }
            RBNodeIndexAccess parent = y._parent;
            RBNodeIndexAccess x;
            if ( y._left != _null )
            {
                x = y._left;
            }
            else 
            {
                x = y._right;
            }
            x._parent = y._parent;
            if ( y._parent == _null )
            {
                _root = x;
            }
            else if ( y == y._parent._left )
            {
                y._parent._left = x;
            }
            else
            {
                y._parent._right = x;
            }
            if ( y != z )
            {
                z.Key = y.Key;
                // copy of additional data
            }
            if ( parent != _null )
            {
                while ( parent != _null )
                {
                    parent._size--;
                    parent = parent._parent;
                }
            }
            if ( y._color == RBColor.Black )
            {
                RB_Delete_Fixup( x );
            }
        }

        public void RB_Delete( IComparable key )
        {
            RB_Delete( Search( key ) );
        }

        public object GetMaximum(  )
        {
            if ( _count == 0 ) return null;
            return GetMaximum( _root ).Key;
        }

        public RBNodeIndexAccess GetMaximumNode(  )
        {
            if ( _count == 0 ) return null;
            return GetMaximum( _root );
        }

        public object GetMinimum( )
        {
            if ( _count == 0 ) return null;
            return GetMinimum( _root ).Key;
        }

        public RBNodeIndexAccess GetMinimumNode( )
        {
            if ( _count == 0 ) return null;
            return GetMinimum( _root );
        }

        public void RB_Insert( IComparable key )
        {
            RBNodeIndexAccess node = new RBNodeIndexAccess( key, _null );
            Insert( node );
            RB_Insert( node );
            _count++;
        }

        public void GetRange( object key1, object key2 )
        {
        }

        public RBNodeIndexAccess GetNext( RBNodeIndexAccess x )
        {
            RBNodeIndexAccess node = GetSuccessor( x );
            if ( node != _null )
            {
                return node;
            }
            else
            {
                return null;
            }
        }

        private int SearchIndex( IComparable searchKey, out RBNodeIndexAccess found )
        {
            RBNodeIndexAccess x = _root;
            RBNodeIndexAccess prev = _null;
            RBNodeIndexAccess next = _null;
            int iCompare = searchKey.CompareTo( x.Key );
            bool bSearch = ( x != _null && iCompare != 0 );
            while ( bSearch )
            {
                if ( iCompare < 0 )
                {
                    if ( x._left == _null )
                    {
                        prev = x;
                    }
                    x = x._left;
                }
                else
                {
                    if ( x._right == _null )
                    {
                        next = x;
                    }
                    x = x._right;
                }
                if ( x != _null )
                {
                    iCompare = searchKey.CompareTo( x.Key );
                }
                bSearch = ( x != _null && iCompare != 0 );
            }
            if ( x == _null )
            {
                if ( prev == _null ) 
                {
                    found = next;
                    return 1;
                }
                found = prev;
                return -1;
            }
            found = x;
            return 0;
        }

        private void RB_Delete_Fixup( RBNodeIndexAccess x )
        {
            while ( x != _root && x._color == RBColor.Black )
            {
                RBNodeIndexAccess w;
                if ( x == x._parent._left )
                {
                    w = x._parent._right;
                    if ( w._color == RBColor.Red )
                    {
                        w._color = RBColor.Black;
                        x._parent._color = RBColor.Red;
                        LeftRotate( x._parent );
                        w = x._parent._right;
                    }
                    if ( w._left._color == RBColor.Black && w._right._color == RBColor.Black )
                    {
                        w._color = RBColor.Red;
                        x = x._parent;
                    }
                    else if ( w._right._color == RBColor.Black )
                    {
                        w._left._color = RBColor.Black;
                        w._color = RBColor.Red;
                        RightRotate( w );
                        w = x._parent._right;
                    }
                    else
                    {
                        w._color = x._parent._color;
                        x._parent._color = RBColor.Black;
                        w._right._color = RBColor.Black;
                        LeftRotate( x._parent );
                        x = _root;
                    }
                }
                else
                {
                    w = x._parent._left;
                    if ( w._color == RBColor.Red )
                    {
                        w._color = RBColor.Black;
                        x._parent._color = RBColor.Red;
                        RightRotate( x._parent );
                        w = x._parent._left;
                    }
                    if ( w._right._color == RBColor.Black && w._left._color == RBColor.Black )
                    {
                        w._color = RBColor.Red;
                        x = x._parent;
                    }
                    else if ( w._left._color == RBColor.Black )
                    {
                        w._right._color = RBColor.Black;
                        w._color = RBColor.Red;
                        LeftRotate( w );
                        w = x._parent._left;
                    }
                    else
                    {
                        w._color = x._parent._color;
                        x._parent._color = RBColor.Black;
                        w._left._color = RBColor.Black;
                        RightRotate( x._parent );
                        x = _root;
                    }
                }
            }
            x._color = RBColor.Black;
        }

        private RBNodeIndexAccess GetMaximum( RBNodeIndexAccess x )
        {
            if ( _root == _null ) return null;
            while ( x._right != _null )
            {
                x = x._right;
            }
            return x;
        }
        private RBNodeIndexAccess GetMinimum( RBNodeIndexAccess x )
        {
            if ( _root == _null ) return null;
            while ( x._left != _null )
            {
                x = x._left;
            }
            return x;
        }
        public RBNodeIndexAccess GetPrevious( RBNodeIndexAccess x )
        {
            RBNodeIndexAccess node = GetPredecessor( x );
            if ( node != _null )
            {
                return node;
            }
            else
            {
                return null;
            }
        }

        private RBNodeIndexAccess GetPredecessor( RBNodeIndexAccess x )
        {
            if ( x._left != _null )
            {
                return GetMaximum( x._left );
            }
            RBNodeIndexAccess y = x._parent;
            while ( y != _null && x == y._left )
            {
                x = y;
                y = y._parent;
            }
            return y;
        }

        private RBNodeIndexAccess GetSuccessor( RBNodeIndexAccess x )
        {
            if ( x._right != _null )
            {
                return GetMinimum( x._right );
            }
            RBNodeIndexAccess y = x._parent;
            while ( y != _null && x == y._right )
            {
                x = y;
                y = y._parent;
            }
            return y;
        }

        public bool SearchOrInsert( IComparable insertedKey, out RBNodeIndexAccess foundOrNew )
        {
            RBNodeIndexAccess current = _root;
            RBNodeIndexAccess parent = _null;
            while ( current != _null )
            {
                parent = current;
                int iCompare = insertedKey.CompareTo( current.Key );
                if ( iCompare < 0 )
                {
                    current = current._left;
                }
                else if ( iCompare > 0 )
                {
                    current = current._right;
                }
                else if ( iCompare == 0 )
                {
                    foundOrNew = current;
                    return true;
                }
            }
            _count++;
            foundOrNew = new RBNodeIndexAccess( insertedKey, _null );
            foundOrNew._parent = parent;
            if ( parent == _null )
            {
                _root = foundOrNew;
                RB_Insert( foundOrNew );
                return false;
            }
            if ( insertedKey.CompareTo( parent.Key ) < 0 )
            {
                parent._left = foundOrNew;
            }
            else
            {
                parent._right = foundOrNew;
            }
            while ( parent != _null )
            {
                parent._size++;
                parent = parent._parent;
            }
            RB_Insert( foundOrNew );
            return false;
        }

        private void Insert( RBNodeIndexAccess node )
        {
            RBNodeIndexAccess current = _root;
            RBNodeIndexAccess parent = _null;
            IComparable insertedKey = (IComparable)node.Key;
            while ( current != _null )
            {
                current._size++;
                parent = current;
                if ( insertedKey.CompareTo( current.Key ) < 0 )
                {
                    current = current._left;
                }
                else
                {
                    current = current._right;
                }
            }
            node._parent = parent;
            if ( parent == _null )
            {
                _root = node;
                return;
            }
            if ( insertedKey.CompareTo( parent.Key ) < 0 )
            {
                parent._left = node;
            }
            else
            {
                parent._right = node;
            }
        }


        private void RB_Insert( RBNodeIndexAccess x )
        {
            x._color = RBColor.Red;
            while ( x != _root && x._parent._color == RBColor.Red )
            {
                RBNodeIndexAccess xParent = x._parent;
                RBNodeIndexAccess xParentParent = xParent._parent;
                if ( xParent == xParentParent._left )
                {
                    RBNodeIndexAccess y = xParentParent._right;
                    if ( /*y != _null && */y._color == RBColor.Red )
                    {
                        xParent._color = RBColor.Black;
                        y._color = RBColor.Black;
                        xParentParent._color = RBColor.Red;
                        x = xParentParent;
                    }
                    else if ( x == xParent._right )
                    {
                        x = xParent;
                        LeftRotate( x );
                    }
                    else
                    {
                        xParent._color = RBColor.Black;
                        xParentParent._color = RBColor.Red;
                        RightRotate( xParentParent );
                    }
                }
                else
                {
                    RBNodeIndexAccess y = xParentParent._left;
                    if ( /*y != _null && */y._color == RBColor.Red )
                    {
                        xParent._color = RBColor.Black;
                        y._color = RBColor.Black;
                        xParentParent._color = RBColor.Red;
                        x = xParentParent;
                    }
                    else if ( x == xParent._left )
                    {
                        x = xParent;
                        RightRotate( x );
                    }
                    else
                    {
                        xParent._color = RBColor.Black;
                        xParentParent._color = RBColor.Red;
                        LeftRotate( xParentParent );
                    }
                }
            }
            _root._color = RBColor.Black;
        }

        private void LeftRotate( RBNodeIndexAccess x )
        {
            RBNodeIndexAccess y = x._right;
            x._right = y._left;

            if ( y._left != _null )
            {
                y._left._parent = x;
            }
            y._parent = x._parent;
            RBNodeIndexAccess xParent = x._parent;
            if ( xParent == _null )
            {
                _root = y;
            }
            else if ( x == xParent._left )
            {
                xParent._left = y;
            }
            else
            {
                xParent._right = y;
            }
            y._left = x;
            x._parent = y;
            y._size = x._size;
            x._size = x._left._size + x._right._size + 1;
        }

        private void RightRotate( RBNodeIndexAccess x )
        {
            RBNodeIndexAccess y = x._left;
            x._left = y._right;

            if ( y._right != _null )
            {
                y._right._parent = x;
            }
            y._parent = x._parent;
            RBNodeIndexAccess xParent = x._parent;
            if ( xParent == _null )
            {
                _root = y;
            }
            else if ( x == xParent._right )
            {
                xParent._right = y;
            }
            else
            {
                xParent._left = y;
            }
            y._right = x;
            x._parent = y;
            y._size = x._size;
            x._size = x._left._size + x._right._size + 1;
        }

        public void InOrderEnum( INodeFetcherIndexAccess fetcher )
        {
            if ( _root == _null || fetcher == null ) return;
            InOrderEnum( fetcher, _root );
        }
        private void InOrderEnum( INodeFetcherIndexAccess fetcher, RBNodeIndexAccess node )
        {
            fetcher.NodeFetched( node );

            if ( node._left != _null )
            {
                InOrderEnum( fetcher, node._left );
            }
            if ( node._right != _null )
            {
                InOrderEnum( fetcher, node._right );
            }
        }

        public void InorderPrint()
        {
            System.Diagnostics.Trace.WriteLine("_________________________");
            InorderPrint( _root );
        }

        private void InorderPrint( RBNodeIndexAccess node )
        {
            /*
            if ( node != _null )
            {
                if ( node.Key.Equals( _root.Key ) )
                {
                    Trace.WriteLine("___ root ___");
                    Console.WriteLine("___ root ___");
                }
                node.Print();
                if ( node.Key.Equals( _root.Key ) )
                {
                    Trace.WriteLine("___ root ___");
                    Console.WriteLine("___ root ___");
                }

                if ( node._left != _null )
                {
                    Trace.Indent();
                    Trace.WriteLine("go left ___ begin");
                    Console.WriteLine("go left ___ begin");
                    InorderPrint( node._left );
                    Trace.WriteLine("go left ___ end");
                    Console.WriteLine("go left ___ end");
                    Trace.Unindent();
                }
                if ( node._right != _null )
                {
                    Trace.Indent();
                    Trace.WriteLine("go right ___ begin");
                    Console.WriteLine("go right ___ begin");
                    InorderPrint( node._right );
                    Trace.WriteLine("go right ___ end");
                    Console.WriteLine("go right ___ end");
                    Trace.Unindent();
                }
            }
            */
        }
    }
}
