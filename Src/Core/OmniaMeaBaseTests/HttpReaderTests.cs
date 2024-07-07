// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;
using NUnit.Framework;

namespace OmniaMeaBaseTests
{
    [TestFixture]
    public class HttpReaderTests
    {

        public class HttpReaderTest : AbstractJob
        {
            protected HttpReader _reader;
            private bool _done = false;
            public HttpReaderTest( string url, bool toFile )
            {
                if ( toFile )
                {
                    FileStream file = File.Create( "c:\\aaa" );
                    _reader = new HttpReaderToFile( url, file );
                }
                else
                {
                    _reader = new HttpReader( url );
                }
            }
            protected override void Execute()
            {
                if ( _done )
                {
                    return;
                }
                MethodInvoker method = _reader.NextMethod;
                method();

                WaitHandle httpHandle = _reader.NextWaitHandle;

                if ( _reader.CurrentState == HttpReader.State.Done || _reader.CurrentState == HttpReader.State.Error )
                {
                    _done = true;
                    DownloadComlete();
                }
                else
                {
                    InvokeAfterWait( new MethodInvoker( Execute ), httpHandle );
                    Execute();
                }
            }
            protected virtual void DownloadComlete()
            {
            }
        }

        public class DownloadHttpReaderTestFile : HttpReaderTest
        {
            public DownloadHttpReaderTestFile( bool toFile ) : base( "http://omeatest-unit/httpReaderTestFile.txt", toFile ){}
            protected override void DownloadComlete()
            {
                Assert.AreEqual( 20, _reader.ReadStream.Length );
                byte[] buffer = new byte[20];
                _reader.ReadStream.Read( buffer, 0, 20 );
                string text = Encoding.Default.GetString( buffer );
                Assert.AreEqual( "This is a test file.", text );
                _reader.ReadStream.Close();
            }
        }
/*

        [Test] public void DownloadTextFileThroughHttp()
        {
            new DownloadHttpReaderTestFile( false ).NextMethod();
        }
        [Test] public void DownloadTextFileThroughHttpToFile()
        {
            new DownloadHttpReaderTestFile( true ).NextMethod();
        }
*/
        [Test, ExpectedException( typeof( ArgumentException ) )]
        public void PassingNullUrl()
        {
            new HttpReader( null );
        }
        [Test, ExpectedException( typeof( ArgumentException ) )]
        public void PassingEmptyUrl()
        {
            new HttpReader( string.Empty );
        }
        [Test, ExpectedException( typeof( ArgumentNullException ) )]
        public void PassingNullPath()
        {
            new HttpReaderToFile( "abra del cadabra", null );
        }
    }
}
