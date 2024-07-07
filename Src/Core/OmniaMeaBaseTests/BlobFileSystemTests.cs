// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using NUnit.Framework;

namespace OmniaMeaBaseTests
{
    [TestFixture]
    public class BlobFileSystemTests
    {

        private const String _bfsFile = "test.bfs";

        private BlobFileSystem _bfs;

        [SetUp]
        public void SetUp()
        {
            _bfs = new BlobFileSystem( _bfsFile, 0x10000, 32 );
        }

        [TearDown]
        public void TearDown()
        {
            if( _bfs != null )
            {
                _bfs.Dispose();
                _bfs = null;
            }
            File.Delete( _bfsFile );
        }

        [Test]
        public void AddSingleFile()
        {
            const int count = 100000;
            int handle;
            BinaryWriter file = _bfs.AllocFile( out handle );
            Assert.IsTrue( handle > 0 );
            for( int i = 0; i < count; ++i )
            {
                file.Write( i.ToString() );
            }
            _bfs.Dispose();
            _bfs = null;
            Assert.IsTrue( new FileInfo( _bfsFile ).Length > count * 2 );
        }

        [Test]
        public void AddSingleFile_WriteStrings_CloseFS_ReadTheFile()
        {
            const int count = 100000;
            int handle;
            BinaryWriter file = _bfs.AllocFile( out handle );
            Assert.IsTrue( handle > 0 );
            for( int i = 0; i < count; ++i )
            {
                file.Write( i.ToString() );
            }
            _bfs.Dispose();
            SetUp();
            BinaryReader reader = _bfs.GetFileReader( handle );
            for( int i = 0; i < count; ++i )
            {
                Assertion.AssertEquals( i.ToString(), i.ToString(), reader.ReadString() );
            }
        }

        [Test]
        public void AddSingleFile_WriteInts_CloseFS_ReadTheFile()
        {
            const int count = 100000;
            int handle;
            BinaryWriter file = _bfs.AllocFile( out handle );
            Assertion.Assert( handle > 0 );
            for( int i = 0; i < count; ++i )
            {
                file.Write( i );
            }
            _bfs.Dispose();
            SetUp();
            BinaryReader reader = _bfs.GetFileReader( handle );
            for( int i = 0; i < count; ++i )
            {
                Assertion.AssertEquals( i.ToString(), i, reader.ReadInt32() );
            }
        }

        [Test]
        public void TwoMixedFiles()
        {
            int handle1;
            _bfs.AllocFile( out handle1 );
            int handle2;
            _bfs.AllocFile( out handle2 );
            Assertion.Assert( handle1 != handle2 );
            for( int i = 0; i < 10; ++i )
            {
                int lastHandle = -1;
                BinaryWriter writer = _bfs.AppendFile( handle1, ref lastHandle );
                for( int j = 0; j < 10000; ++j )
                {
                    writer.Write( i );
                    writer.Write( j );
                }

                lastHandle = -1;
                writer = _bfs.AppendFile( handle2, ref lastHandle );
                for( int j = 0; j < 10000; ++j )
                {
                    writer.Write( i + 1 );
                    writer.Write( j - 1 );
                }
            }

            BinaryReader reader = _bfs.GetFileReader( handle1 );
            for( int i = 0; i < 10; ++i )
            {
                for( int j = 0; j < 10000; ++j )
                {
                    Assertion.AssertEquals( i.ToString(), i, reader.ReadInt32() );
                    Assertion.AssertEquals( i.ToString(), j, reader.ReadInt32() );
                }
            }

            reader = _bfs.GetFileReader( handle2 );
            for( int i = 0; i < 10; ++i )
            {
                for( int j = 0; j < 10000; ++j )
                {
                    Assertion.AssertEquals( i.ToString(), i + 1, reader.ReadInt32() );
                    Assertion.AssertEquals( i.ToString(), j - 1, reader.ReadInt32() );
                }
            }
        }

        [Test]
        public void ReadToEndMixedFiles()
        {
            int handle1;
            _bfs.AllocFile( out handle1 );
            int handle2;
            _bfs.AllocFile( out handle2 );
            Assertion.Assert( handle1 != handle2 );
            for( int i = 0; i < 10; ++i )
            {
                int lastHandle = -1;
                BinaryWriter writer = _bfs.AppendFile( handle1, ref lastHandle );
                for( int j = 0; j < 10000; ++j )
                {
                    writer.Write( i );
                    writer.Write( j );
                }

                lastHandle = -1;
                writer = _bfs.AppendFile( handle2, ref lastHandle );
                for( int j = 0; j < 10000; ++j )
                {
                    writer.Write( i + 1 );
                    writer.Write( j - 1 );
                }
            }

            ArrayList ints = new ArrayList();
            BinaryReader reader = _bfs.GetFileReader( handle1 );
            while( true )
            {
                try
                {
                    ints.Add( reader.ReadInt32() );
                }
                catch( EndOfStreamException )
                {
                    break;
                }
            }
            Assertion.AssertEquals( ints.Count, 200000 );
            for( int i = 0; i < ints.Count; ++i )
            {
                if( ( i & 1 ) == 0 )
                {
                    Assertion.AssertEquals( i / 20000, ints[ i ] );
                }
                else
                {
                    Assertion.AssertEquals( (i / 2 % 10000), ints[ i ] );
                }
            }

            ints.Clear();
            reader = _bfs.GetFileReader( handle2 );
            while( true )
            {
                try
                {
                    ints.Add( reader.ReadInt32() );
                }
                catch( EndOfStreamException )
                {
                    break;
                }
            }
            Assertion.AssertEquals( ints.Count, 200000 );
            for( int i = 0; i < ints.Count; ++i )
            {
                if( ( i & 1 ) == 0 )
                {
                    Assertion.AssertEquals( (i / 20000) + 1, ints[ i ] );
                }
                else
                {
                    Assertion.AssertEquals( (i / 2 % 10000) - 1, ints[ i ] );
                }
            }
        }

        [Test]
        public void CreateFile_CloseFS_RewriteFile()
        {
            int handle;
            BinaryWriter file = _bfs.AllocFile( out handle );
            Assertion.Assert( handle > 0 );
            for( int i = 0; i < 100000; ++i )
            {
                file.Write( i );
            }
            _bfs.Dispose();
            SetUp();
            file = _bfs.RewriteFile( handle );
            for( int i = 0; i < 100000; ++i )
            {
                file.Write( i.ToString() );
            }
            BinaryReader reader = _bfs.GetFileReader( handle );
            for( int i = 0; ; ++i )
            {
                try
                {
                    Assertion.AssertEquals( i.ToString(), i.ToString(), reader.ReadString());
                }
                catch( EndOfStreamException )
                {
                    break;
                }
            }
        }

        [Test]
        public void DisposeReaderWriter()
        {
            const int count = 100000;
            int handle;
            using( BinaryWriter file = _bfs.AllocFile( out handle ) )
            {
                Assertion.Assert( handle > 0 );
                for( int i = 0; i < count; ++i )
                {
                    file.Write( i.ToString() );
                }
            }
            Assertion.Assert( new FileInfo( _bfsFile ).Length > count * 2 );
            using( BinaryReader file = _bfs.GetFileReader( handle ) )
            {
                for( int i = 0; i < count; ++i )
                {
                    Assertion.AssertEquals( i.ToString(), file.ReadString() );
                }
            }
            using( BinaryReader file = _bfs.GetFileReader( handle ) )
            {
                for( int i = 0; i < count; ++i )
                {
                    Assertion.AssertEquals( i.ToString(), file.ReadString() );
                }
            }
        }

        [Test]
        public void ReuseDeletedFileSpace()
        {
            IntArrayList handles = new IntArrayList();
            for( int i = 0; i < 10; ++i )
            {
                int handle;
                using( BinaryWriter file = _bfs.AllocFile( out handle ) )
                {
                    handles.Add( handle );
                    for( int j = 0; j < 50; ++j )
                    {
                        file.Write( handle.ToString() );
                    }
                }
                using( BinaryWriter file = _bfs.AllocFile( out handle ) )
                {
                    handles.Add( handle );
                    for( int j = 0; j < 200; ++j )
                    {
                        file.Write( handle.ToString() );
                    }
                }
            }
            for( int i = 0; i < 10; ++i )
            {
                _bfs.DeleteFile( handles[ ( i * 2 ) + 1 ] );
            }
            _bfs.Dispose();

            long bsfLength = new FileInfo( _bfsFile ).Length;

            SetUp();

            // file handles are reused in back order
            for( int i = 10; i > 0; --i )
            {
                int handle;
                using( BinaryWriter file = _bfs.AllocFile( out handle ) )
                {
                    Assertion.AssertEquals( handles[ ( i * 2 ) - 1 ], handle );
                    for( int j = 0; j < 200; ++j )
                    {
                        file.Write( handle.ToString() );
                    }
                }
            }
            _bfs.Dispose();

            Assert.AreEqual(bsfLength, new FileInfo( _bfsFile ).Length );
        }
    }
}
