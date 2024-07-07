// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using System.Runtime.Remoting;
using System.Threading;

namespace JetBrains.Omea.Base
{
    public class JetMemoryStream : Stream
    {
        public JetMemoryStream()
        {
            Init( _defaultPageSize );
        }

        public JetMemoryStream( int pageSize )
        {
            Init( pageSize );
        }

        public JetMemoryStream( byte[] bytes, bool share )
        {
            int length = bytes.Length;
            if( length == 0 )
            {
                Init( _defaultPageSize );
            }
            else
            {
                if( !share )
                {
                    _pageSize = _defaultPageSize;
                    Init( bytes, 0, length );
                }
                else
                {
                    _pages = new ArrayList( 1 );
                    _pages.Add( bytes );
                    _position = 0;
                    _length = _pageSize = length;
                }
            }
        }

        public JetMemoryStream( byte[] bytes, int offset, int count )
        {
            _pageSize = _defaultPageSize;
            Init( bytes, offset, count );
        }

        #region Stream implementation

        public override bool CanSeek { get { return true; } }

        public override bool CanRead { get { return true; } }

        public override bool CanWrite { get { return true; } }

        public override void Flush() {}

        public override long Length { get { return _length; } }

        public override long Position
        {
            get { return _position; }
            set
            {
                if( value < 0 || value > _length )
                {
                    throw new ArgumentOutOfRangeException( "Position" );
                }
                _position = value;
            }
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            switch( origin )
            {
                case SeekOrigin.Begin:
                {
                    if( offset > _length )
                    {
                        throw new InvalidOperationException( "Can't seek beyond the end of stream." );
                    }
                    _position = offset;
                    break;
                }
                case SeekOrigin.End:
                {
                    if( offset > _length )
                    {
                        throw new InvalidOperationException( "Can't seek before the begin of stream." );
                    }

                    _position = _length - offset;
                    break;
                }
                case SeekOrigin.Current:
                {
                    if( offset + _position > _length )
                    {
                        throw new InvalidOperationException( "Can't seek beyond the end of stream." );
                    }
                    if( offset + _position < 0 )
                    {
                        throw new InvalidOperationException( "Can't seek before the begin of stream." );
                    }
                    _position += offset;
                    break;
                }
            }
            return _position;
        }

        public override void SetLength( long value )
        {
            _length = value;
            if( _position > _length )
            {
                _position = _length;
            }
            int pageIndex = (int) ( _length / _pageSize );
            if( ++pageIndex < _pages.Count )
            {
                _pages.RemoveRange( pageIndex, _pages.Count - pageIndex );
            }
        }

        public override int ReadByte()
        {
            if( _position >= _length )
            {
                return -1;
            }
            byte[] page = (byte[]) _pages[ (int) ( _position / _pageSize ) ];
            byte result = page[ (int) ( _position % _pageSize ) ];
            ++_position;
            return result;
        }

        public override void WriteByte( byte value )
        {
            int pageIndex = (int) ( _position / _pageSize );
            byte[] page;
            if( pageIndex < _pages.Count )
            {
                page = (byte[]) _pages[ pageIndex ];
            }
            else
            {
                page = new byte[ _pageSize ];
                _pages.Add( page );
            }
            page[ (int) ( _position % _pageSize ) ] = value;
            if( ++_position > _length )
            {
                _length = _position;
            }
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            int savedOffset = offset;
            int pageIndex = (int) ( _position / _pageSize );
            int inPageIndex = (int) ( _position % _pageSize );
            while( count > 0 && _position < _length )
            {
                byte[] page = (byte[]) _pages[ pageIndex ];
                int readBytes = page.Length - inPageIndex;
                if( readBytes > count )
                {
                    readBytes = count;
                }
                if( readBytes > _length - _position )
                {
                    readBytes = (int) ( _length - _position );
                }
                Array.Copy( page, inPageIndex, buffer, offset, readBytes );
                _position += readBytes;
                offset += readBytes;
                count -= readBytes;
                inPageIndex = 0;
                ++pageIndex;
            }
            return offset - savedOffset;
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            WriteImpl( buffer, offset, count );
        }

        public override void Close()
        {
            _length = _position = 0;
            _pages.Clear();
        }

        #region async I/O stubs

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException( "JetMemoryStream should not be accessed asynchronously" );
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException( "JetMemoryStream should not be accessed asynchronously" );
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new InvalidOperationException( "JetMemoryStream should not be accessed asynchronously" );
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new InvalidOperationException( "JetMemoryStream should not be accessed asynchronously" );
        }

    	[Obsolete("CreateWaitHandle will be removed eventually.  Please use \"new ManualResetEvent(false)\" instead.")]
    	protected override WaitHandle CreateWaitHandle()
        {
            throw new InvalidOperationException( "JetMemoryStream should not be accessed asynchronously" );
        }

        #endregion

        #region not implemented

        public override ObjRef CreateObjRef( Type requestedType )
        {
            throw new NotImplementedException();
        }

        public override object InitializeLifetimeService()
        {
            throw new NotImplementedException();
        }

        #endregion

        public void WriteTo( Stream target )
        {
            long count = _length;
            foreach( byte[] page in _pages )
            {
                if( count == 0 )
                {
                    break;
                }
                int writeBytes = page.Length;
                if( writeBytes > count )
                {
                    writeBytes = (int) count;
                }
                target.Write( page, 0, writeBytes );
                count -= writeBytes;
            }
        }

        #endregion

        private void Init( int pageSize )
        {
            _pages = new ArrayList();
            _length = _position = 0;
            _pageSize = pageSize;
        }

        private void Init( byte[] bytes, int offset, int count )
        {
            _pages = new ArrayList( count / _pageSize + 1 );
            _length = _position = 0;
            WriteImpl( bytes, offset, count );
            _position = 0;
        }

        private void WriteImpl( byte[] buffer, int offset, int count )
        {
            int pageIndex = (int) ( _position / _pageSize );
            int inPageIndex = (int) ( _position % _pageSize );
            while( count > 0 )
            {
                byte[] page;
                if( pageIndex < _pages.Count )
                {
                    page = (byte[]) _pages[ pageIndex ];
                }
                else
                {
                    page = new byte[ _pageSize ];
                    _pages.Add( page );
                }
                int writeBytes = page.Length - inPageIndex;
                if( writeBytes > count )
                {
                    writeBytes = count;
                }
                Array.Copy( buffer, offset, page, inPageIndex, writeBytes );
                _position += writeBytes;
                offset += writeBytes;
                count -= writeBytes;
                inPageIndex = 0;
                ++pageIndex;
            }
            if( _position > _length )
            {
                _length = _position;
            }
        }

        private const int   _defaultPageSize = 4096;
        private ArrayList   _pages;
        private int         _pageSize;
        private long        _length;
        private long        _position;
    }
}
