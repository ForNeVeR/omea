/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Text;
using System.Threading;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.Base
{
    public class ObjectPool
    {        
        public delegate object CreateObjectDelegate();
        public delegate void AllocObjectDelegate( object obj );
        public delegate void DisposeObjectDelegate( object obj );

        public ObjectPool( int maxObjects,
                           CreateObjectDelegate creator,
                           AllocObjectDelegate allocator,
                           DisposeObjectDelegate disposer )
        {
            if( creator == null )
            {
                throw new ArgumentNullException( "CreateObjectDelegate creator" );
            }
            _creator = creator;
            _allocator = allocator;
            _disposer = disposer;
            _pool = new PoolEntry[ maxObjects ];
        }

        public object Alloc()
        {
            for( int i = 0; i < _pool.Length; ++i )
            {
                if( Interlocked.Exchange( ref _pool[ i ]._employed, 1 ) == 0 )
                {
                    object result = _pool[ i ]._object;
                    if( result == null )
                    {
                        _pool[ i ]._object = result = _creator();
                    }
                    if( _allocator != null )
                    {
                        _allocator( result );
                    }
                    return result;
                }
            }
            throw new Exception( "Alloc: ObjectPool exhausted" );
        }

        public void Dispose( object obj )
        {
            if( obj == null )
            {
                throw new ArgumentNullException( "object obj" );
            }
            for( int i = 0; i < _pool.Length; ++i )
            {
                if( ReferenceEquals( _pool[ i ]._object, obj ) )
                {
                    if( _disposer != null )
                    {
                        _disposer( obj );
                    }
                    int oldEmployed = Interlocked.Exchange( ref _pool[ i ]._employed, 0 );
                    if ( oldEmployed != 1 )
                    {
                        throw new InvalidOperationException( "Duplicate ObjectPool dispose" );
                    }
                    return;
                }
            }
            throw new Exception( "Dispose: Object not found" );
        }

        private struct PoolEntry
        {
            public int     _employed;
            public object  _object;
        }

        private readonly CreateObjectDelegate    _creator;
        private readonly AllocObjectDelegate     _allocator;
        private readonly DisposeObjectDelegate   _disposer;
        private readonly PoolEntry[]             _pool;
    }

    public class StringBuilderPool
    {
        private const int _maximumCapacity = 16384;
        
        public static StringBuilder Alloc()
        {
            CheckPool();
            return (StringBuilder) _pool.Alloc();
        }

        public static void Dispose( StringBuilder builder )
        {
            _pool.Dispose( builder );
        }

        private static void CheckPool()
        {
            if( _pool == null )
            {
                lock( _syncObject )
                {
                    if( _pool == null )
                    {
                        _pool = new ObjectPool( 80, CreatePooledStringBuilder, null, DisposePooledStringBuilder );
                    }
                }
            }
        }

        private static object CreatePooledStringBuilder()
        {
            return new StringBuilder( _maximumCapacity );
        }

        private static void DisposePooledStringBuilder( object obj )
        {
            StringBuilder sb = (StringBuilder) obj;
            sb.Length = 0;
            if ( sb.Capacity > _maximumCapacity )
            {
                sb.Capacity = _maximumCapacity;    
            }
        }

        private static ObjectPool   _pool = null;
        private static readonly object  _syncObject = new object();
    }

    public class ArrayListPool
    {
        private const int _maximumCapacity = 2048;
        
        public static ArrayList Alloc()
        {
            CheckPool();
            return (ArrayList) _pool.Alloc();
        }

        public static void Dispose( ArrayList list )
        {
            _pool.Dispose( list );
        }

        private static void CheckPool()
        {
            if( _pool == null )
            {
                lock( _syncObject )
                {
                    if( _pool == null )
                    {
                        _pool = new ObjectPool( 80, CreatePooledArrayList, null, DisposePooledArrayList );
                    }
                }
            }
        }

        private static object CreatePooledArrayList()
        {
            return new ArrayList( _maximumCapacity );
        }

        private static void DisposePooledArrayList( object obj )
        {
            ArrayList list = (ArrayList) obj;
            list.Clear();
            if( list.Capacity > _maximumCapacity )
            {
                list.Capacity = _maximumCapacity;
            }
        }

        private static ObjectPool   _pool = null;
        private static readonly object  _syncObject = new object();
    }

    public class IntArrayListPool
    {
        private const int _maximumCapacity = 2048;
        
        public static IntArrayList Alloc()
        {
            CheckPool();
            return (IntArrayList) _pool.Alloc();
        }

        public static void Dispose( IntArrayList builder )
        {
            _pool.Dispose( builder );
        }

        private static void CheckPool()
        {
            if( _pool == null )
            {
                lock( _syncObject )
                {
                    if( _pool == null )
                    {
                        _pool = new ObjectPool( 80, CreatePooledIntArrayList, null, DisposePooledIntArrayList );
                    }
                }
            }
        }

        private static object CreatePooledIntArrayList()
        {
            return new IntArrayList( _maximumCapacity );
        }

        private static void DisposePooledIntArrayList( object obj )
        {
            IntArrayList list = (IntArrayList) obj;
            if( list.Capacity > _maximumCapacity ) 
            {
                list.Capacity = _maximumCapacity;
            }
            list.SetSize( 0 );
        }

        private static ObjectPool   _pool = null;
        private static readonly object  _syncObject = new object();
    }
}