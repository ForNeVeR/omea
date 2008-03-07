/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using NUnit.Framework;

using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.Database;
using JetBrains.Omea.OpenAPI;

namespace CommonTests
{
    [TestFixture]
    public class ResourceTests: MyPalDBTests
    {
        private ArrayList _addedLinks;

        [SetUp]
        public void SetUp()
        {
            InitStorage();
            RegisterResourcesAndProperties();
            _addedLinks = new ArrayList();
        }

        [TearDown]
        public void TearDown()
        {
            CloseStorage();
        }

        private int GetResourcePropCount( int ID )
        {
            int count = 0;
            foreach( ICountedResultSet rs in _storage.GetAllProperties( ID ) )
            {
                count += rs.Count;
                rs.Dispose();
            }
            return count;
        }

        private int ResultSetCount( IResultSet rs )
        {
            int count = 0;
            foreach( IRecord rec in rs )
            {
                count++;
            }
            return count;
        }

        private Stream GetStreamWithString( string s )
        {
            Stream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter( stream );
            writer.Write( s );
            writer.Flush();
            return stream;
        }

        private void OnLinkAdded( object sender, LinkEventArgs e )
        {
            _addedLinks.Add( e );
        }
		
        [Test] public void TestCreateResource()
        {
            IResource res = _storage.NewResource( "Email" );
            int ID = res.Id;

            ReopenStorage();

            IResource res2 = _storage.LoadResource( ID );
            Assert.AreEqual( "Email", res2.Type );
        }

        [Test] public void CreateResourceCaseInsensitive()
        {
            IResource res = _storage.NewResource( "EMail" );
            Assert.AreEqual( "Email", res.Type );
        }

        [Test] public void TestProperty()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( "Subject", "Test" );

            ReopenStorage();
			
            IResource res2 = _storage.LoadResource( res.Id );
            Assert.AreEqual( "Test", res2.GetStringProp( "Subject" ) );
			
            Assert.AreEqual( "Test", res2.GetStringProp( _propSubject ) );
            res2.SetProp( _propSubject, "Test2" );
            Assert.AreEqual( "Test2", res2.GetStringProp( _propSubject ) );
        }

        [Test] public void SetPropToNull()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( "Subject", "Test" );
            res.SetProp( "Subject", null );
            Assert.IsTrue( !res.HasProp( "Subject" ) );
        }

        [Test] public void TestIntProperty()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( "Size", 654 );

            ReopenStorage();

            IResource res2 = _storage.LoadResource( res.Id );
            Assert.AreEqual( 654, res2.GetIntProp( "Size" ) );

            res2.SetProp( _propSize, 456 );
            Assert.AreEqual( 456, res2.GetIntProp( _propSize ) );
        }

        [Test] public void TestDateProperty()
        {
            DateTime dt = DateTime.Now;
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( "Received", dt );

            ReopenStorage();

            IResource res2 = _storage.LoadResource( res.Id );
            Assert.AreEqual( dt, res2.GetDateProp( "Received" ) );
            Assert.AreEqual( dt, res2.GetDateProp( _propReceived ) );
        }

        [Test] public void TestBlobProperty()
        {
            IResource res = _storage.NewResource( "Email" );
            Assert.AreEqual( null, res.GetBlobProp( _propBody ) );
            Assert.AreEqual( null, res.GetProp( _propBody ) );

            res.SetProp( _propBody, GetStreamWithString( "BLOB test" ) );

            ReopenStorage();
            IResource res2 = _storage.LoadResource( res.Id );
            Stream newStream = res2.GetBlobProp( "Body" );
            StreamReader reader = new StreamReader( newStream );
            string text = Utils.StreamReaderReadToEnd( reader );
            Assert.AreEqual( "BLOB test", text );
            reader.Close();

            Stream newStream2 = res2.GetBlobProp( "Body" );
            Assert.IsTrue( newStream2.CanRead );
        }

        [Test] public void TestUpdateBlob()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propBody, GetStreamWithString( "BLOB test" ) );

            ReopenStorage();

            IResource res2 = _storage.LoadResource( res.Id );
            res2.SetProp( _propBody, GetStreamWithString( "Second test" ) );
        }

        [Test, ExpectedException( typeof(ResourceDeletedException) )]
        public void BlobAfterDelete()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propBody, GetStreamWithString( "BLOB test" ) );
            res.Delete();
            res.GetBlobProp( _propBody );
        }
                                                                                
        [Test] public void UpdateMultipleBlob()
        {
            IResource res1 = _storage.NewResource( "Email" );
            res1.SetProp( _propBody, GetStreamWithString( "BLOB test 1 " ) );

            IResource res2 = _storage.NewResource( "Email" );
            res2.SetProp( _propBody, GetStreamWithString( "BLOB test 2" ) );

            Stream s1 = res1.GetBlobProp( _propBody ); s1 = s1;
            Stream s2 = res2.GetBlobProp( _propBody ); s2 = s2;

            res1.SetProp( _propBody, GetStreamWithString( "BLOB test 3" ) );
            res2.SetProp( _propBody, GetStreamWithString( "BLOB test 4" ) );
        }

        [Test] public void MultipleIndependentBlobStreams()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propBody, GetStreamWithString( "BLOB test" ) );

            Stream s1 = res.GetBlobProp( _propBody );
            Stream s2 = res.GetBlobProp( _propBody );

            Assert.AreEqual( 0, s2.Position );
            s1.Seek( 4, SeekOrigin.Begin );
            Assert.AreEqual( 0, s2.Position );

            s1.Close();
            Assert.AreEqual( (byte) 'B', s2.ReadByte() );
        }
        
        [Test] public void RewriteBlobWithSmallerStream()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propBody, GetStreamWithString( "BLOB test" ) );
            Assert.AreEqual( "BLOB test", res.GetPropText( _propBody ) );

            res.SetProp( _propBody, GetStreamWithString( "BLOB" ) );
            Assert.AreEqual( "BLOB", res.GetPropText( _propBody ) );
        }
        
        private IResource _res;
        private const string _largeBlob = "This is enough large blob test string which is written to the Body property and read from the property simultaneously from a few threads. It's preferable to set the string enough large in order to overcome the capacity of one blob filesysrtem cluster in underlying blob stream. Seems this length should be sufficient.";
        
        [Test] public void MultiThreadedBlobStreams()
        {
            _res = _storage.NewResource( "Email" );
            _res.SetProp( _propBody, "BLOB test" );
            AsyncProcessor currentThreadProc = new AsyncProcessor( false );
            currentThreadProc.ThreadStarted += new EventHandler( currentThreadProc_ThreadStarted );
            currentThreadProc.ThreadFinished += new EventHandler( currentThreadProc_ThreadFinished );
            currentThreadProc.ExceptionHandler = new AsyncExceptionHandler( ExceptionHandler );
            currentThreadProc.EmployCurrentThread();            
            Assert.AreEqual( "BLOB test", _res.GetPropText( _propBody ) );
        }
        
        private void currentThreadProc_ThreadStarted( object sender, EventArgs e )
        {
            _res.SetProp( _propBody, _largeBlob );
            const int procCount = 5;
            AsyncProcessor[] readingProcs = new AsyncProcessor[ procCount ];
            for( int i = 0; i < procCount; ++i )
            {
                AsyncProcessor proc = new AsyncProcessor( false );
                readingProcs[ i ] = proc;
                proc.ExceptionHandler = new AsyncExceptionHandler( ExceptionHandler );
                proc.QueueJob( new InfiniteRecurrentReadingOfBlobPropertyDelegate( InfiniteRecurrentReadingOfBlobProperty ), proc );
                proc.StartThread();
            }
            
            AsyncProcessor caller = (AsyncProcessor) sender;
            caller.QueueJobAt( DateTime.Now.AddSeconds( 10 ),
                               new FinishAsyncProcessorsDelegate( FinishAsyncProcessors ),
                               caller, readingProcs );
            caller.QueueJob(
                new InfiniteRecurrentWritingToBlobPropertyDelegate( InfiniteRecurrentWritingToBlobProperty ), caller );
        }

        private void ExceptionHandler( Exception e )
        {
            Console.WriteLine( e.ToString() );
        }

        private void currentThreadProc_ThreadFinished( object sender, EventArgs e )
        {
            _res.SetProp( _propBody, "BLOB test" );
        }
        
        private delegate void InfiniteRecurrentReadingOfBlobPropertyDelegate( AsyncProcessor caller );
        private void InfiniteRecurrentReadingOfBlobProperty( AsyncProcessor caller )
        {
            Assert.IsTrue( _res.HasProp( _propBody ) );
            Assert.AreEqual( _largeBlob, _res.GetPropText( _propBody ) );
            caller.QueueJob(
                new InfiniteRecurrentReadingOfBlobPropertyDelegate( InfiniteRecurrentReadingOfBlobProperty ), caller );
        }
        
        private delegate void InfiniteRecurrentWritingToBlobPropertyDelegate( AsyncProcessor caller );
        private void InfiniteRecurrentWritingToBlobProperty( AsyncProcessor caller )
        {
            _res.SetProp( _propBody, _largeBlob );
            Assert.AreEqual( _largeBlob, _res.GetPropText( _propBody ) );
            caller.QueueJobAt( DateTime.Now.AddMilliseconds( 10 ), 
                new InfiniteRecurrentWritingToBlobPropertyDelegate( InfiniteRecurrentWritingToBlobProperty ), caller );
        }
        
        private delegate void FinishAsyncProcessorsDelegate( AsyncProcessor caller, AsyncProcessor[] procs );
        private void FinishAsyncProcessors( AsyncProcessor caller, AsyncProcessor[] procs )
        {
            caller.QueueEndOfWork();
            foreach( AsyncProcessor proc in procs )
            {
                proc.Dispose();
            }
        }

        [Test] public void TestDoubleProperty()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propSimilarity, 0.9 );

            ReopenStorage();
            IResource res2 = _storage.LoadResource( res.Id );
            Assert.AreEqual( 0.9, res2.GetDoubleProp( _propSimilarity ), 0.0001 );
        }

        [Test, ExpectedException( typeof(StorageException) )] public void DoubleWrongType()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propSimilarity, "Test" );
        }

        [Test] public void TestLongStringProperty()
        {
            int propLongBody = _storage.PropTypes.Register( "LongBody", PropDataType.LongString );
            
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( propLongBody, "Test");
            Assert.AreEqual( "Test", res.GetStringProp( propLongBody ) );

            ReopenStorage();
            IResource res2 = _storage.LoadResource( res.Id ); res2 = res2;
            Assert.AreEqual( "Test", res.GetStringProp( propLongBody ) );
        }

        [Test] public void TestBoolProperty()
        {
            IResource res = _storage.NewResource( "Email" );
            Assert.IsTrue( !res.HasProp( _propUnread ) );
            Assert.AreEqual( false, res.GetProp( _propUnread ) );

            res.SetProp( _propUnread, false );
            Assert.IsTrue( !res.HasProp( _propUnread ) );

            res.SetProp( _propUnread, true );
            Assert.IsTrue( res.HasProp( _propUnread ) );
            Assert.AreEqual( true, res.GetProp( _propUnread ) );

            ReopenStorage();
            IResource res2 = _storage.LoadResource( res.Id );
            Assert.IsTrue( res2.HasProp( _propUnread ) );
            Assert.AreEqual( true, res2.GetProp( _propUnread ) );

            res2.SetProp( _propUnread, false );
            Assert.IsTrue( !res2.HasProp( _propUnread ) );
        }

        [Test] public void DeleteProp()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( "Size", 654 );
            Assert.IsTrue( res.HasProp( "Size" ) );
			
            res.DeleteProp( "Size" );
            Assert.IsTrue( !res.HasProp( "Size" ) );
            Assert.AreEqual( 0, GetResourcePropCount( res.Id ) );

            ReopenStorage();
            IResource res2 = _storage.LoadResource( res.Id );
            Assert.IsTrue( !res2.HasProp( "Size" ) );
        }

        [Test] public void DeleteBoolProp()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propUnread, true );

            res.DeleteProp( _propUnread );
            Assert.IsTrue( !res.HasProp( _propUnread ) );

            ReopenStorage();
            IResource res2 = _storage.LoadResource( res.Id );
            Assert.IsTrue( !res2.HasProp( _propUnread ) );
        }

        [Test] public void DeleteLinkProp()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            email.AddLink( _propAuthor, person );
            email.DeleteProp( _propAuthor );
            Assert.AreEqual( 0, person.GetLinkCount( _propAuthor ) );
        }

        [Test] public void UpdateProp()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( "Size", 654 );
            res.SetProp( "Size", 456 );

            Assert.AreEqual( 1, GetResourcePropCount( res.Id ) );
        }

        [Test] public void TestLink()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            email.AddLink( "Author", person );
            Assert.AreEqual( 1, person.GetLinksOfType( null, "Author" ).Count );

            ReopenStorage();

            IResource person2 = _storage.LoadResource( person.Id );
            IResourceList links = person2.GetLinksOfType( null, "Author" );
            Assert.AreEqual( 1, links.Count );
			
            IResource email2 = links [0];
            Assert.AreEqual( email.Id, email2.Id );
            Assert.AreEqual( 1, email2.GetLinksOfType( null, "Author" ).Count );

            IResource email3 = _storage.NewResource( "Email" );
            person.AddLink( _propAuthor, email3 );
            Assert.AreEqual( 2, person.GetLinksOfType( null, _propAuthor ).Count );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void LinkResourceToItself()
        {
            IResource email = _storage.NewResource( "Email" );
            email.AddLink( _propAuthor, email );
        }

        [Test] public void TestDuplicateLink()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Email" );
            email.AddLink( _propAuthor, person );
            person.AddLink( _propAuthor, email );
            Assert.AreEqual( 1, person.GetLinksOfType( null, _propAuthor ).Count );
        }

        [Test] public void TypedLinks()
        {
            IResource email = _storage.NewResource( "Email" );

            IResource email2 = _storage.NewResource( "Email" );
            email2.AddLink( _propAuthor, email );

            IResource person = _storage.NewResource( "Person" );
            person.AddLink( _propAuthor, email );

            IResourceList links = email.GetLinksOfTypeLive( "Person", _propAuthor );
            Assert.AreEqual( 1, links.Count );

            IResource email3 = _storage.NewResource( "Email" );
            email3.AddLink( _propAuthor, email );
            Assert.AreEqual( 1, links.Count );

            IResource person2 = _storage.NewResource( "Person" );
            person2.AddLink( _propAuthor, email );
            Assert.AreEqual( 2, links.Count );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void TestLinksWrongType()
        {
            IResource person = _storage.NewResource( "Person" );
            person.GetLinksOfType( null, _propSize );
        }

        [Test] public void LinksOfType_Deleted()
        {
            IResource person = _storage.NewResource( "Person" );
            person.Delete();
            Assert.AreEqual( 0, person.GetLinksOfType( null, _propAuthor ).Count );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void TestCreateLinkWrongType()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( _propSize, email );
        }

        [Test] public void DeleteLink()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            email.AddLink( "Author", person );
			
            IResourceList links = email.GetLinksOfTypeLive( null, "Author" );
            Assert.AreEqual( 1, links.Count );

            email.DeleteLink( "Author", person );
            Assert.AreEqual( 0, links.Count );
            Assert.AreEqual( 0, person.GetLinksOfType( null, "Author" ).Count );
            Assert.IsTrue( !person.HasProp( _propAuthor ), "HasProp must return false after DeleteLink" );

            ReopenStorage();
            IResource email2 = _storage.LoadResource( email.Id );
            Assert.AreEqual( 0, email2.GetLinksOfType( null, "Author" ).Count );
            IResource person2 = _storage.LoadResource( person.Id );
            Assert.AreEqual( 0, person2.GetLinksOfType( null, "Author" ).Count );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void DeleteLinkWrongType()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.DeleteLink( _propSize, email );
        }

        [Test] public void DirectedLinks()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );

            email.AddLink( _propReply, email2 );
            Assert.AreEqual( 1, email.GetLinksFrom( null, _propReply ).Count );
            Assert.AreEqual( 0, email2.GetLinksFrom( null, _propReply ).Count );

            Assert.AreEqual( 0, email.GetLinksTo( null, _propReply ).Count );
            Assert.AreEqual( 1, email2.GetLinksTo( null, _propReply ). Count );

            Assert.AreEqual( 1, email.GetLinksOfType( null, _propReply ).Count );
            Assert.AreEqual( 1, email2.GetLinksOfType( null, _propReply ).Count );

            int[] linkTypes = email2.GetLinkTypeIds();
            Assert.AreEqual( linkTypes.Length, 1 );
            Assert.AreEqual( _propReply, linkTypes [0] );

            Assert.IsTrue( email.HasProp( _propReply ) );
            Assert.IsTrue( !email2.HasProp( _propReply ) );

            ReopenStorage();

            IResource emailnew = _storage.LoadResource( email.Id );
            IResource email2new = _storage.LoadResource( email2.Id );

            Assert.AreEqual( 1, emailnew.GetLinksFrom( null, _propReply ).Count );
            Assert.AreEqual( 0, email2new.GetLinksFrom( null, _propReply ).Count );

            Assert.AreEqual( 0, emailnew.GetLinksTo( null, _propReply ).Count );
            Assert.AreEqual( 1, email2new.GetLinksTo( null, _propReply ). Count );
        }

        [Test] public void DirectedLinksLive()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );

            IResourceList fromEmail = email.GetLinksFromLive( null, _propReply );
            IResourceList toEmail   = email.GetLinksToLive( null, _propReply );
            IResourceList allEmail  = email.GetLinksOfTypeLive( null, _propReply );

            IResourceList fromEmail2 = email2.GetLinksFromLive( null, _propReply );
            IResourceList toEmail2   = email2.GetLinksToLive( null, _propReply );
            IResourceList allEmail2  = email2.GetLinksOfTypeLive( null, _propReply );

            email.AddLink( _propReply, email2 );
            Assert.AreEqual( 1, fromEmail.Count );
            Assert.AreEqual( 0, toEmail.Count );
            Assert.AreEqual( 1, allEmail.Count );

            Assert.AreEqual( 0, fromEmail2.Count );
            Assert.AreEqual( 1, toEmail2.Count );
            Assert.AreEqual( 1, allEmail2.Count );
        }

        [Test] public void DeleteDirectedLinks()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );

            email.AddLink( _propReply, email2 );
            email2.DeleteLink( _propReply, email );

            Assert.AreEqual( 0, email.GetLinksOfType( null, _propReply ).Count );
            Assert.AreEqual( 0, email2.GetLinksOfType( null, _propReply ).Count );
        }

        [Test, ExpectedException( typeof(StorageException) ) ]
        public void InvalidDirectedLinks()
        {
            IResource email = _storage.NewResource( "Email" );
            email.GetLinksFrom( null, _propAuthor );
        }

        [Test] public void LinksAfterDelete()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            email.AddLink( _propReply, email2 );

            IResourceList links = email.GetLinksOfType( null, _propReply );
            email.Delete();
            Assert.AreEqual( 1, links.Count );
        }

        [Test] public void DeleteReverseLink()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            email.AddLink( _propReply, email2 );
            email2.Delete();
            
            Assert.AreEqual( 0, email.GetLinksOfType( null, _propReply ).Count );
        }

        [Test] public void DeleteLinks()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            IResource email3 = _storage.NewResource( "Email" );
            email.AddLink( _propReply, email2 );
            email.AddLink( _propReply, email3 );

            email.DeleteLinks( _propReply );
            Assert.AreEqual( 0, email.GetLinksOfType( null, _propReply ).Count );
            Assert.AreEqual( 0, email2.GetLinksOfType( null, _propReply ).Count );
            Assert.AreEqual( 0, email3.GetLinksOfType( null, _propReply ).Count );
        }

        [Test, ExpectedException(typeof(StorageException))]
        public void HasLink_NotLinkProp()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            email.HasLink( _propSubject, email2 );
        }

        [Test, ExpectedException(typeof(StorageException))]
        public void HasLink_NotDirectedLinkProp()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            email.HasLink( -_propAuthor, email2 );
        }

        [Test] public void HasLink_DirectedLink()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            email.AddLink( _propReply, email2 );
            
            Assert.IsTrue( email.HasLink( _propReply, email2 ) );
            Assert.IsTrue( email2.HasLink( -_propReply, email ) );
            
            Assert.IsTrue( !email2.HasLink( _propReply, email ) );
            Assert.IsTrue( !email.HasLink( -_propReply, email2 ) );
        }

        [Test] public void TestDisplayName()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Subject", "Test" );
            Assert.AreEqual( "Test", email.DisplayName );

            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "FirstName", "Dmitry" );
            person.SetProp( "LastName", "Jemerov" );
            Assert.AreEqual( "Dmitry Jemerov", person.DisplayName );

            person.SetProp( "LastName", "Zhemerov" );
            Assert.AreEqual( "Dmitry Zhemerov", person.DisplayName );
        }

        [Test] public void TestMultiDisplayName()
        {
            _storage.PropTypes.Register( "EmailAcct", PropDataType.Link );
            _storage.ResourceTypes.Register( "Contact", "FirstName LastName | EmailAcct" );

            IResource contact = _storage.NewResource( "Contact" );
            contact.SetProp( "FirstName", "Dmitry" );
            Assert.AreEqual( "Dmitry", contact.DisplayName );

            IResource contact2  = _storage.NewResource( "Contact" );
            contact2.SetProp( "LastName", "Jemerov" );
            Assert.AreEqual( "Jemerov", contact2.DisplayName );

            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Subject", "yole@yole.ru" );
            IResource contact3 = _storage.NewResource( "Contact" );
            contact3.AddLink( "EmailAcct", email );
            Assert.AreEqual( "yole@yole.ru", contact3.DisplayName );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( "Subject", "yole@intellij.com" );
            contact3.SetProp( "EmailAcct", email2 );
            Assert.AreEqual( "yole@intellij.com", contact3.DisplayName );
        }

        [Test] public void EmptyStringDisplayName()
        {
            _storage.ResourceTypes.Register( "Contact", "FirstName | LastName" );

            IResource contact = _storage.NewResource( "Contact" );
            contact.SetProp( "FirstName", "" );
            contact.SetProp( "LastName", "Jemerov" );
            Assert.AreEqual( "", contact.DisplayName );

            _storage.ResourceTypes.Register( "Contact2", "FirstName LastName" );
            IResource contact2 = _storage.NewResource( "Contact2" );
            contact2.SetProp( "FirstName", "" );
            contact2.SetProp( "LastName", "" );
            Assert.AreEqual( "", contact2.DisplayName );
        }

        [Test] public void StaticDisplayName()
        {
            IResource person = _storage.NewResource( "Person" );
            person.DisplayName = "Dmitry Jemerov";
            Assert.AreEqual( "Dmitry Jemerov", person.DisplayName );
            person.SetProp( "FirstName", "Vasya" );
            Assert.AreEqual( "Dmitry Jemerov", person.DisplayName );
        }

        [Test] public void CharsInDisplayName()
        {
            _storage.ResourceTypes.Register( "Contact", "LastName, FirstName" );
            IResource person = _storage.NewResource( "Contact" );
            person.SetProp( "LastName", "Jemerov" );
            person.SetProp( "FirstName", "Dmitry" );
            Assert.AreEqual( "Jemerov, Dmitry", person.DisplayName );

            IResource contactResType = _storage.FindUniqueResource( "ResourceType", "Name", "Contact" );
            contactResType.SetProp( "DisplayNameMask", "LastName [FirstName]" );
            person.SetProp( "LastName", "Jemeroff" );   // force invalidation
            Assert.AreEqual( "Jemeroff [Dmitry]", person.DisplayName );
        }

        [Test] public void CurlyBracesInDisplayName()
        {
            _storage.PropTypes.Register( "FidoNet.Node", PropDataType.Int );
            _storage.PropTypes.Register( "FidoNet.Point", PropDataType.Int );
            _storage.ResourceTypes.Register( "FidoNet.Address", "{FidoNet.Node}.{FidoNet.Point}" );

            IResource addr = _storage.NewResource( "FidoNet.Address" );
            addr.SetProp( "FidoNet.Node", 48 );
            addr.SetProp( "FidoNet.Point", 654 );
            Assert.AreEqual( "48.654", addr.DisplayName );
        }

        [Test] public void DirectedLinkInDisplayName()
        {
            int propParentContact = _storage.PropTypes.Register( "ParentContact", PropDataType.Link, PropTypeFlags.DirectedLink );
            _storage.ResourceTypes.Register( "Contact", "ParentContact | FirstName LastName" );
            IResource parentPerson = _storage.NewResource( "Contact" );
            parentPerson.SetProp( "FirstName", "Dmitry" );
            parentPerson.SetProp( "LastName", "Jemerov" );

            IResource childPerson = _storage.NewResource( "Contact" );
            childPerson.SetProp( "FirstName", "Dima" );

            childPerson.AddLink( propParentContact, parentPerson );

            Assert.AreEqual( "Dmitry Jemerov", parentPerson.DisplayName );
            Assert.AreEqual( "Dmitry Jemerov", childPerson.DisplayName );

            childPerson.DeleteLink( propParentContact, parentPerson );
            Assert.AreEqual( "Dima", childPerson.DisplayName );

            IResource parentPerson2 = _storage.NewResource( "Contact" );
            parentPerson2.SetProp( "FirstName", "Michael" );
            parentPerson2.SetProp( "LastName", "Gerasimov" );
            
            childPerson.AddLink( propParentContact, parentPerson2 );
            Assert.AreEqual( "Michael Gerasimov", childPerson.DisplayName );
        }

        [Test] public void TestPropText()
        {
            DateTime dt = DateTime.Now;
			
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Subject", "Test" );
            email.SetProp( "Size", 654 );
            email.SetProp( "Received", dt );

            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "FirstName", "Dmitry" );
            person.SetProp( "LastName", "Jemerov" );
            email.AddLink( "Author", person );

            Assert.AreEqual( "Test", email.GetPropText( "Subject" ) );
            Assert.AreEqual( "654", email.GetPropText( "Size" ) );
            Assert.AreEqual( dt.ToString(), email.GetPropText( "Received" ) );
            Assert.AreEqual( "Dmitry Jemerov", email.GetPropText( "Author" ) );

            IResource person2 = _storage.NewResource( "Person" );
            person2.SetProp( "FirstName", "Michael");
            person2.SetProp( "LastName", "Gerasimov" );
            email.AddLink( "Author", person2 );
            Assert.AreEqual( "Dmitry Jemerov, Michael Gerasimov", email.GetPropText( "Author" ) );

            email.DeleteLink( _propAuthor, person );
            email.DeleteLink( _propAuthor, person2 );
            Assert.AreEqual( "", email.GetPropText( _propAuthor ) );
        }

        [Test] public void PropTextEmpty()
        {
            IResource person = _storage.NewResource( "Person" );
            Assert.AreEqual( "", person.GetPropText( "FirstName" ), "PropText for non-existing prop must be empty string" );
            Assert.AreEqual( "", person.GetPropText( "Author" ), "PropText for non-existing prop must be empty string" );

            IResource email = _storage.NewResource( "Email" );
            person.AddLink( "Responsible", email );
            Assert.AreEqual( "", person.GetPropText( "Author" ), "PropText for non-existing prop must be empty string" );
        }

        [Test] public void PropTextDirectedLink()
        {
            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( _propSubject, "Email1" );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( _propSubject, "Email2" );
            email1.AddLink( _propReply, email2 );

            Assert.AreEqual( "Email2", email1.GetPropText( _propReply ) );
            Assert.AreEqual( "Email1", email2.GetPropText( -_propReply ) );
        }

        [Test] public void TestProperties()
        {
            DateTime dt = DateTime.Now;
			
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Name", "001" );
            email.SetProp( "Subject", "Test" );
            email.SetProp( "Size", 654 );
            email.SetProp( "Received", dt );
            email.SetProp( "IsUnread", true );

            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "FirstName", "Dmitry" );
            email.AddLink( "Author", person );

            IResource email2 = _storage.NewResource( "Email" );
            IResource email3 = _storage.NewResource( "Email" );
            email.AddLink( _propReply, email2 );
            email3.AddLink( _propReply, email );

            Assert.AreEqual( 8, email.Properties.Count );

            VerifyProperty( email, "Name", PropDataType.String, "001" );
            VerifyProperty( email, "Subject", PropDataType.String, "Test" );
            VerifyProperty( email, "Size", PropDataType.Int, 654 );
            VerifyProperty( email, "Received", PropDataType.Date, dt );
            VerifyProperty( email, "Author", PropDataType.Link, person );
            VerifyProperty( email, _propReply, PropDataType.Link, email2 );
            VerifyProperty( email, -_propReply, PropDataType.Link, email3 );
            VerifyProperty( email, "IsUnread", PropDataType.Bool, true );

            ReopenStorage();
            IResource newEmail = _storage.LoadResource( email.Id );
            Assert.AreEqual( 8, newEmail.Properties.Count );
        }

        [Test] public void EmptyLinkInProperties()
        {
            IResource email = _storage.NewResource( "Email" );
            ReopenStorage();
            IResource newEmail = _storage.LoadResource( email.Id );
            newEmail.GetLinkProp( "Author" );
            Assert.AreEqual( 0, newEmail.Properties.Count );
            IEnumerator enumerator = newEmail.Properties.GetEnumerator();
            Assert.IsFalse( enumerator.MoveNext() );
        }

        private void VerifyProperty( IResource res, string propName, PropDataType propType, object propValue )
        {
            bool found = false;
            foreach( IResourceProperty prop in res.Properties )
            {
                if ( prop.Name == propName )
                {
                    Assert.AreEqual( prop.DataType, propType );
                    Assert.AreEqual( prop.Value, propValue );
                    found = true;
                    break;
                }
            }
            Assert.IsTrue( found, "Property not found" );
        }

        private void VerifyProperty( IResource res, int propID, PropDataType propType, object propValue )
        {
            bool found = false;
            foreach( IResourceProperty prop in res.Properties )
            {
                if ( prop.PropId == propID )
                {
                    Assert.AreEqual( prop.DataType, propType );
                    Assert.AreEqual( prop.Value, propValue );
                    found = true;
                    break;
                }
            }
            Assert.IsTrue( found, "Property not found" );
        }

        [Test] public void PropsAfterLoad()
        {
            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "LastName", "Jemerov" );
            int ID = person.Id;

            ReopenStorage();
            person = _storage.LoadResource( ID );
            person.SetProp( "FirstName", "Dmitry" );
            Assert.AreEqual( person.GetStringProp( "LastName" ), "Jemerov" );
        }

        [Test] public void LinksAfterLoad()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( "Author", email );

            ReopenStorage();

            IResource person2 = _storage.LoadResource( person.Id );
            IResource email2 = _storage.NewResource( "Email" );
            person2.AddLink( "Author", email2 );
            Assert.AreEqual( 2, person2.GetLinksOfType( null, "Author" ).Count );
        }

        [Test] public void DuplicateLinksAfterLoad()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( "Author", email );

            email.SetProp( "Subject", "Test" );

            ReopenStorage();

            IResource person2 = _storage.LoadResource( person.Id );
            IResource email2 = _storage.LoadResource( email.Id );
            Assert.AreEqual( 1, person2.GetLinksOfType( null, "Author" ).Count );
            Assert.AreEqual( 1, email2.GetLinksOfType( null, "Author" ).Count );
        }

        [Test] public void LinkPropAfterLoad()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( "Author", email );

            ReopenStorage();

            IResource person2 = _storage.LoadResource( person.Id );
            IResource email2 = person2.GetLinkProp( "Author" );
            Assert.IsTrue( email2 != null, "LinkProp not found" );
            Assert.AreEqual( email.Id, email2.Id );
        }

        [Test] public void SetPropLink()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( "Author", person );

            IResourceList personLinks = person.GetLinksOfType( null, "Author" );
            Assert.AreEqual( 1, personLinks.Count );

            IResource person2 = _storage.NewResource( "Person" );
            email.SetProp( "Author", person2 );
            Assert.AreEqual( 1, email.GetLinksOfType( null, "Author" ).Count );
            
            // verify that the person has not been deleted
            Assert.AreEqual( MyPalStorage.Storage.ResourceTypes ["Person"].Id, 
                MyPalStorage.Storage.GetResourceType( person.Id ) );

            email.SetProp( "Author", null );
            Assert.AreEqual( 0, email.GetLinksOfType( null, "Author" ).Count );
        }

        [Test] public void SetPropDirectedLink()
        {
            int propParent = _storage.PropTypes.Register( "Parent", PropDataType.Link, PropTypeFlags.DirectedLink );
            IResource person1 = _storage.NewResource( "Person" );
            IResource person2 = _storage.NewResource( "Person" );

            person1.SetProp( propParent, person2 );
            Assert.AreEqual( 1, person1.GetLinksFrom( null, propParent ).Count );
            Assert.AreEqual( 0, person1.GetLinksTo( null, propParent ).Count );
            Assert.AreEqual( 1, person2.GetLinksTo( null, propParent ).Count );

            person2.SetProp( propParent, person1 );
            Assert.AreEqual( 0, person1.GetLinksFrom( null, propParent ).Count );
            Assert.AreEqual( 1, person1.GetLinksTo( null, propParent ).Count );
            Assert.AreEqual( 1, person2.GetLinksFrom( null, propParent ).Count );
            Assert.AreEqual( 0, person2.GetLinksTo( null, propParent ).Count );

            person2.SetProp( propParent, null );
            Assert.AreEqual( 0, person2.GetLinksFrom( null, propParent ).Count );
        }

        [Test] public void SetPropDirectedLink2()
        {
            int propParent = _storage.PropTypes.Register( "Parent", PropDataType.Link, PropTypeFlags.DirectedLink );
            IResource person1 = _storage.NewResource( "Person" );
            IResource person2 = _storage.NewResource( "Person" );
            IResource person3 = _storage.NewResource( "Person" );

            person1.SetProp( propParent, person2 );
            person2.SetProp( propParent, person3 );

            Assert.AreEqual( 1, person1.GetLinksFrom( null, propParent ).Count );
            Assert.AreEqual( 1, person3.GetLinksTo( null, propParent ).Count );
        }

        [Test, ExpectedException(typeof(StorageException))] 
        public void InvalidResourceType()
        {
            _storage.NewResource( "Someshit" );
        }

        [Test, ExpectedException(typeof(InvalidResourceIdException))]
        public void InvalidResourceID()
        {
            _storage.LoadResource( -255 );
        }

        [Test] public void NonexistingProperty()
        {
            IResource person = _storage.NewResource( "Person" );
            Assert.IsTrue( person.GetStringProp( "FirstName" ) == null );
            Assert.IsTrue( person.GetIntProp( "Size" ) == 0 );
            Assert.IsTrue( person.GetLinkProp( "Author" ) == null );
        }

        [Test, ExpectedException(typeof(StorageException))]
        public void InvalidProperty()
        {
            IResource person = _storage.NewResource( "Person" );
            person.GetStringProp( "SomeShit" );
        }

        [Test, ExpectedException(typeof(StorageException))]
        public void PropTypeMismatch()
        {
            IResource person = _storage.NewResource( "Person" );
            person.GetIntProp( "FirstName" );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void InvalidSetProperty()
        {
            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "SomeShit", "dummy" );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void SetPropWrongType()
        {
            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "Size", "SomeShit" );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void InvalidPropertyID()
        {
            IResource person = _storage.NewResource( "Person" );
            person.GetIntProp( -255 );
        }

        [Test, ExpectedException( typeof( StorageException) )]
        public void InvalidGetPropID()
        {
            IResource person = _storage.NewResource( "Person" );
            person.GetProp( -255 );
        }

        [Test] public void TestDuplicateSave()
        {
            IResource person = _storage.NewResource( "Person" );
            int ID = person.Id;
            Assert.AreEqual( ID, person.Id );
        }

        [Test] public void DeleteResource()
        {
            IResource person = _storage.NewResource( "Person" );
            int id = person.Id;
            Assert.AreEqual( 1, _storage.GetAllResources( "Person" ).Count );

            person.Delete();
            Assert.AreEqual( 0, _storage.GetAllResources( "Person" ).Count );
            Assert.AreEqual( id, person.OriginalId );
        }

        [Test] public void DeleteResourceProps()
        {
            IResource person = _storage.NewResource( "Person" );
            int id = person.Id;
            person.SetProp( "FirstName", "Dmitry" );
            person.SetProp( "LastName", "Jemerov" );
            person.SetProp( _propSimilarity, 0.9 );
            person.SetProp( _propUnread, true );

            person.Delete();
            Assert.AreEqual( 0, GetResourcePropCount( id ) );
            IResultSet rs = MyPalStorage.Storage.GetBoolProperties( id );
            Assert.IsFalse( rs.GetEnumerator().MoveNext() );
        }

        [Test] public void DeleteResourceLinks()
        {
            int propRelated = _storage.PropTypes.Register( "Related", PropDataType.Link, PropTypeFlags.DirectedLink );

            IResource person  = _storage.NewResource( "Person" );
            IResource person2 = _storage.NewResource( "Person" );
            IResource email   = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            int id = person.Id; id = id;

            person.AddLink( _propAuthor, email );
            person2.AddLink( propRelated, person );
            person.AddLink( propRelated, email2 );
            person.Delete();

            Assert.AreEqual( 0, email.GetLinksOfType( null, _propAuthor ).Count );
            Assert.AreEqual( 0, person2.GetLinksOfType( null, propRelated ).Count );
            Assert.AreEqual( 0, email2.GetLinksOfType( null, propRelated ).Count );

            ReopenStorage();
            IResource new_email = _storage.LoadResource( email.Id );
            Assert.AreEqual( 0, new_email.GetLinksOfType( null, _propAuthor ).Count );
        }

        [Test] public void DeleteAfterReload()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( _propAuthor, email );

            ReopenStorage();
            IResource person2 = _storage.LoadResource( person.Id );
            person2.Delete();

            IResource email2 = _storage.LoadResource( email.Id );
            Assert.AreEqual( 0, email2.GetLinksOfType( null, _propAuthor ).Count );
        }

        [Test] public void LinkHasProp()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( _propAuthor, email );
            Assert.IsTrue( person.HasProp( _propAuthor ) );
        }

        [Test] public void GetProp_Link()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( _propAuthor, email );
            Assert.AreEqual( email, person.GetProp( _propAuthor ) );
        }

        /*
        [Test] public void CascadeDelete()
        {
            int propSource = _storage.PropTypes.Register( "Source", PropDataType.Link, PropTypeFlags.CascadeDelete );

            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( propSource, email );

            person.Delete();
            bool emailDeleted = false;
            try
            {
                _storage.GetResourceType( email.Id );
            }
            catch( StorageException )
            {
                emailDeleted = true;
            }
            Assert( "Cascade delete must have deleted the email", emailDeleted );
        }

        [Test] public void CascadeDeleteDirected()
        {
            int propCascadeDirected = _storage.PropTypes.Register( "CascadeDirected", PropDataType.Link,
                PropTypeFlags.CascadeDelete | PropTypeFlags.DirectedLink );

            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( propCascadeDirected, email );

            email.Delete();
            bool personDeleted = false;
            try
            {
                _storage.GetResourceType( person.Id );
            }
            catch( StorageException )
            {
                personDeleted = true;
            }
            Assert( "Cascade delete must have deleted the person", personDeleted );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void RegisterCascadeNotLink()
        {
            _storage.PropTypes.Register( "Bad", PropDataType.Double, PropTypeFlags.CascadeDelete );
        }

        */

        [Test, ExpectedException( typeof(ResourceDeletedException) )]
        public void SetPropAfterDelete()
        {
            IResource res = _storage.NewResource( "Email" );
            res.Delete();
            res.SetProp( "FirstName", "Dmitry" );
        }

        [Test, ExpectedException( typeof(ResourceDeletedException) )]
        public void AddLinkAfterDelete()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            email.AddLink( _propAuthor, person );
            email.Delete();
            IResource person2 = _storage.NewResource( "Person" );
            email.AddLink( _propAuthor, person2 );
        }

        [Test, ExpectedException( typeof(ResourceDeletedException) )]
        public void SetLinkPropAfterDelete()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person1 = _storage.NewResource( "Person" );
            IResource person2 = _storage.NewResource( "Person" );
            email.AddLink( _propAuthor, person1 );
            email.Delete();
            email.SetProp( _propAuthor, person2 );
        }

        [Test] public void DoubleDelete()
        {
            IResource res = _storage.NewResource( "Email" );
            res.Delete();
            res.Delete();
        }

        [Test, ExpectedException( typeof(ResourceDeletedException) )]
        public void DeletePropAfterDelete()
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( "Subject", "Test" );
            res.Delete();
            res.DeleteProp( "Subject" );
        }

        [Test, ExpectedException( typeof(ResourceDeletedException) )]
        public void DeleteLinkAfterDelete()
        {
            IResource res = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            res.AddLink( _propAuthor, person );
            res.Delete();
            res.DeleteLink( _propAuthor, person );
        }

        [Test, Ignore("No more .blob files")] public void DeleteBlob()
        {
            Assert.AreEqual( 0, Directory.GetFiles( MyPalStorage.DBPath, "*.blob" ).Length );
            
            IResource res = _storage.NewResource( "Email" );
            MemoryStream blobStream = new MemoryStream();
            blobStream.WriteByte( 1 );
            res.SetProp( _propBody, blobStream );
            Assert.AreEqual( 1, Directory.GetFiles( MyPalStorage.DBPath, "*.blob" ).Length );

            res.Delete();
            Assert.AreEqual( 0, Directory.GetFiles( MyPalStorage.DBPath, "*.blob" ).Length );
        }

        [Test] public void TransientProp()
        {
            IResource res = _storage.NewResourceTransient( "Email" );
            Assert.IsTrue( res.IsTransient );
            res.SetProp( _propSubject, "Test" );
            res.SetProp( _propUnread, true );

            Assert.AreEqual( 0, GetResourcePropCount( res.Id ) );
            Assert.AreEqual( 0, ResultSetCount( _storage.GetBoolProperties( res.Id ) ) );
            res.EndUpdate();
            Assert.IsTrue( !res.IsTransient );
            Assert.AreEqual( 1, GetResourcePropCount( res.Id ) );
            Assert.AreEqual( 1, ResultSetCount( _storage.GetBoolProperties( res.Id ) ) );

            IResource res2 = _storage.LoadResource( res.Id );
            Assert.AreSame( res, res2 );
        }

        [Test] public void TransientPropFalse()
        {
            IResource res = _storage.NewResourceTransient( "Email" );
            res.SetProp( _propUnread, false );
            Assert.IsTrue( !res.HasProp( _propUnread ) );

            res.EndUpdate();
            Assert.AreEqual( 0, ResultSetCount( _storage.GetBoolProperties( res.Id ) ) );
        }

        [Test] public void TransientLink()
        {
            _storage.LinkAdded += new LinkEventHandler( OnLinkAdded );
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResourceTransient( "Email" );
            Assert.IsTrue( email.IsTransient );
            email.AddLink( _propAuthor, person );
            Assert.IsTrue( email.IsTransient );

            Assert.AreEqual( 1, _addedLinks.Count );

            Assert.AreEqual( 0, person.GetLinksOfType( null, _propAuthor ).Count );
            Assert.AreEqual( 1, email.GetLinksOfType( null, _propAuthor ).Count );

            email.EndUpdate();
            Assert.AreEqual( 1, person.GetLinksOfType( null, _propAuthor ).Count );
            Assert.AreEqual( 1, email.GetLinksOfType( null, _propAuthor ).Count );
        }

        [Test] public void LinkToTransient()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResourceTransient( "Email" );
            person.AddLink( _propReply, email );

            Assert.AreEqual( 0, person.GetLinksFrom( null, _propReply ).Count );
            Assert.AreEqual( 1, email.GetLinksTo( null, _propReply ).Count );

            email.EndUpdate();
            Assert.AreEqual( 1, person.GetLinksFrom( null, _propReply ).Count );
            Assert.AreEqual( 0, person.GetLinksTo( null, _propReply ).Count );
            Assert.AreEqual( 0, email.GetLinksFrom( null, _propReply ).Count );
            Assert.AreEqual( 1, email.GetLinksTo( null, _propReply ).Count );
        }

        [Test] public void TransientDeleteLink()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResourceTransient( "Email" );
            email.AddLink( _propAuthor, person );

            Assert.AreEqual( 0, person.GetLinksOfType( null, _propAuthor ).Count );
            Assert.AreEqual( 1, email.GetLinksOfType( null, _propAuthor ).Count );

            email.DeleteLink( _propAuthor, person );
            Assert.AreEqual( 0, person.GetLinksOfType( null, _propAuthor ).Count );
            Assert.AreEqual( 0, email.GetLinksOfType( null, _propAuthor ).Count );

            email.EndUpdate();
            Assert.AreEqual( 0, person.GetLinksOfType( null, _propAuthor ).Count );
            Assert.AreEqual( 0, email.GetLinksOfType( null, _propAuthor ).Count );
        }

        [Test] public void LinkTransientToDeleted()
        {
            IResource person = _storage.NewResourceTransient( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( _propAuthor, email );
            email.Delete();
            person.EndUpdate();
            Assert.AreEqual( 0, person.GetLinkCount( _propAuthor ) );
        }

        [Test, Category("Transient Resources")] 
        public void LinkTransientToDeleted_BeforeCommit()
        {
            IResource person = _storage.NewResourceTransient( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( _propAuthor, email );
            email.Delete();
            Assert.IsNull( person.GetLinkProp( _propAuthor ) );
        }

        [Test] public void TransientLiveLinks()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResourceTransient( "Email" );
            email.AddLink( _propAuthor, person );

            IResourceList liveLinks = person.GetLinksOfTypeLive( null, _propAuthor );
            Assert.AreEqual( 0, liveLinks.Count );

            email.EndUpdate();
            Assert.AreEqual( 1, liveLinks.Count );
        }

        [Test, Category("Transient Resources")] 
        public void TransientDelete()
        {
            IResource res = _storage.NewResourceTransient( "Person" );
            Assert.AreEqual( 1, _storage.TransientResourceCount );
            res.Delete();
            Assert.AreEqual( 0, _storage.TransientResourceCount );
        }

        [Test, Category("Transient Resources")]
        public void TransientDeleteAllLinks()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResourceTransient( "Person" );
            person.AddLink( _propAuthor, email );
            email.Delete();
            person.Delete();
        }

        [Test, Category("Transient Resources")]
        public void TransientUpdateCount()
        {
            IResource res = _storage.NewResourceTransient( "Email" );
            res.Delete();
            Assert.AreEqual( 0, _storage.GetUpdatingResourceCount() );
        }

        [Test, Category("Transient Resources")]
        public void CompactTransientResources()
        {
            IResource res = _storage.NewResourceTransient( "Email" ); res = res;
            res = null;
            GC.Collect();
            _storage.CompactTransientResources();
            Assert.AreEqual( 0, _storage.GetUpdatingResourceCount() );
        }

        [Test, Category("Transient Resources")]
        public void TransientResourceToList()
        {
            IResource res = _storage.NewResourceTransient( "Email" );
            res.SetProp( "Subject", "Test" );
            IResourceList resList = res.ToResourceList();
            res = null;
            GC.Collect();
            _storage.CompactTransientResources();
            Assert.AreEqual( "Test", resList [0].GetStringProp( "Subject" ) );
        }

        [Test] public void DeletePropTypeLink()
        {
            int propCustomLink = _storage.PropTypes.Register( "CustomLink", PropDataType.Link );
            _storage.GetPropId( "CustomLink" );

            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( propCustomLink, email );
            Assert.IsTrue( person.HasProp( propCustomLink ) );

            _storage.PropTypes.Delete( propCustomLink );

            int[] linkTypeIDs = person.GetLinkTypeIds();
            Assert.IsTrue( Array.IndexOf( linkTypeIDs, propCustomLink ) < 0, "The link property of the resource must have been deleted" );

            IResource res = _storage.FindUniqueResource( "PropType", "Name", "CustomLink" );
            Assert.IsTrue( res == null, "The resource for the deleted PropType must have been deleted" );

            ReopenStorage();
            VerifyNoPropName( "CustomLink" );
            VerifyNoPropType( propCustomLink );
        }

        [Test] public void DeletePropTypeLink_Uncached()
        {
            int propCustomLink = _storage.PropTypes.Register( "CustomLink", PropDataType.Link );
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );
            person.AddLink( propCustomLink, email );

            ReopenStorage();
            IResource email2 = _storage.LoadResource( email.Id ); email2 = email2;
            _storage.PropTypes.Delete( propCustomLink );
            Assert.IsTrue( !_storage.LinkExists( person.Id, email.Id, propCustomLink ) );
            Assert.IsTrue( !_storage.LinkExists( email.Id, person.Id, propCustomLink ) );
        }

        [Test] public void DeletePropTypeString()
        {
        	int propCustomString = _storage.PropTypes.Register( "CustomString", PropDataType.String );

            IResource person = _storage.NewResource( "Person" );
            person.SetProp( "Name", "Dmitry" );
            person.SetProp( propCustomString, "AString" );
            Assert.IsTrue( person.HasProp( propCustomString ) );

            _storage.PropTypes.Delete( propCustomString );
            foreach( IResourceProperty prop in person.Properties )
            {
            	Assert.IsTrue( prop.PropId != propCustomString );
            }

            ReopenStorage();
            VerifyNoPropType( propCustomString );
            VerifyNoPropName( "CustomString" );
            IResource person2 = _storage.LoadResource( person.Id );
            foreach( IResourceProperty prop in person2.Properties )
            {
                Assert.IsTrue( prop.PropId != propCustomString );
            }
        }

        [Test] public void DeleteLongStringPropType()
        {
            _storage.ResourceTypes.Register( "Test", "Test", "Name" );
            int propLongString = _storage.PropTypes.Register( "LongString", PropDataType.LongString );
            int propLongString2 = _storage.PropTypes.Register( "LongString2", PropDataType.LongString );
            IResource res = _storage.NewResource( "Test" );
            res.SetProp( propLongString, "Value" );
            res.SetProp( propLongString2, "Value2" );
            
            _storage.PropTypes.Delete( propLongString );
            Assert.AreEqual( 1, res.Properties.Count );
            Assert.IsTrue( res.HasProp( propLongString2 ) );

            ReopenStorage();

            res = _storage.LoadResource( res.Id );
            Assert.AreEqual( 1, res.Properties.Count );
            Assert.IsTrue( res.HasProp( propLongString2 ) );
        }

        [Test] public void GetLinkTypeIDs_Deleted()
        {
            IResource res = _storage.NewResource( "Person" );
            res.Delete();
            Assert.AreEqual( 0, res.GetLinkTypeIds().Length );
        }

        [Test] public void GetLinkTypeIds_EmptyArray()
        {
            IResource res = _storage.NewResource( "Person" );
            ReopenStorage();
            res = _storage.LoadResource( res.Id );
            res.GetLinkCount( _propAuthor );
            IResource res2 = _storage.NewResource( "Email" );
            res.AddLink( _propReply, res2 );
            Assert.AreEqual( 1, res.GetLinkTypeIds().Length );
        }

        [Test] public void StringListProps()
        {
            IResource res = _storage.NewResource( "Person" );
            IStringList valueList = res.GetStringListProp( _propValueList );
            Assert.IsTrue( valueList != null );
            Assert.AreEqual( 0, valueList.Count );
            Assert.IsTrue( !res.HasProp( _propValueList ) );

            valueList.Add( "Dmitry" );
            valueList.Add( "Michael" );
            Assert.AreEqual( 2, valueList.Count );
            Assert.IsTrue( res.HasProp( _propValueList ) );
            Assert.AreEqual( "Dmitry, Michael", res.GetPropText( _propValueList ) );

            ReopenStorage();

            IResource res2 = _storage.LoadResource( res.Id );
            IStringList valueList2 = res2.GetStringListProp( _propValueList );
            Assert.AreEqual( 2, valueList2.Count );
            Assert.AreEqual( "Dmitry", valueList2 [0] );
            Assert.AreEqual( "Michael", valueList2 [1] );

            valueList.RemoveAt( 0 );
            valueList.Add( "Sergey" );

            ReopenStorage();

            IResource res3 = _storage.LoadResource( res.Id );
            IStringList valueList3 = res3.GetStringListProp( _propValueList );
            Assert.AreEqual( 2, valueList3.Count );
            Assert.AreEqual( "Michael", valueList3 [0] );
            Assert.AreEqual( "Sergey", valueList3 [1] );

            res3.DeleteProp( _propValueList );
            
            ReopenStorage();

            IResource res4 = _storage.LoadResource( res.Id );
            IStringList valueList4 = res4.GetStringListProp( _propValueList );
            Assert.AreEqual( 0, valueList4.Count );
        }

        [Test, ExpectedException( typeof(StorageException) )]
        public void StringListSetProp()
        {
            IResource res = _storage.NewResource( "Person" );
            res.SetProp( _propValueList, "Test" );
        }

        [Test] public void StringListAfterDelete()
        {
            IResource res = _storage.NewResource( "Person" );
            IStringList valueList = res.GetStringListProp( _propValueList );
            valueList.Add( "Dmitry" );
            valueList.Add( "Michael" );
            Assert.AreEqual( 2, ResultSetCount( _storage.GetStringListProperties( res.Id ) ) );

            ReopenStorage();
            IResource res2 = _storage.LoadResource( res.Id );
            res2.Delete();
            Assert.AreEqual( 0, ResultSetCount( _storage.GetStringListProperties( res.Id ) ) );
        }

        [Test] public void StringListTransient()
        {
        	IResource res = _storage.NewResourceTransient( "Person" );
            IStringList valueList = res.GetStringListProp( _propValueList );
            valueList.Add( "Dmitry" );
            valueList.Add( "Michael" );

            Assert.AreEqual( 0, ResultSetCount( _storage.GetStringListProperties( res.Id ) ) );

            res.EndUpdate();
            Assert.AreEqual( 2, ResultSetCount( _storage.GetStringListProperties( res.Id ) ) );
        }

        [Test] public void StringListOrder()
        {
            IResource res = _storage.NewResource( "Person" );
            IStringList valueList = res.GetStringListProp( _propValueList );
            valueList.Add( "Dmitry" );
            
            ReopenStorage();
            IResource res2 = _storage.LoadResource( res.Id );
            valueList = res2.GetStringListProp( _propValueList );
            valueList.Add( "Michael" );

            Assert.AreEqual( "Dmitry", valueList [0] );
            Assert.AreEqual( "Michael", valueList [1] );
        }

        [Test] public void StringListAndLoadProperties()
        {
            IResource res = _storage.NewResource( "Person" );
            IStringList valueList = res.GetStringListProp( _propValueList );
            valueList.Add( "Dmitry" );

            ReopenStorage();
            IResource res2 = _storage.LoadResource( res.Id );
            valueList = res2.GetStringListProp( _propValueList );
            Assert.AreEqual( 1, valueList.Count );
            int propCount = res2.Properties.Count; propCount = propCount;
            Assert.AreEqual( 1, valueList.Count );
        }

        [Test] public void StringListLoadAfterDispose()
        {
            int propSecondStringList = _storage.PropTypes.Register( "SecondStringList", PropDataType.StringList );
            IResource res = _storage.NewResource( "Person" );
            IStringList valueList = res.GetStringListProp( _propValueList );
            valueList.Add( "Dmitry" );
            IStringList secondStringList = res.GetStringListProp( propSecondStringList );
            secondStringList.Add( "Dima" );

            ReopenStorage();
            res = _storage.LoadResource( res.Id );
            secondStringList = res.GetStringListProp( propSecondStringList );
            Assert.AreEqual( 1, secondStringList.Count );
            valueList = res.GetStringListProp( _propValueList );
            Assert.AreEqual( 1, valueList.Count );

            secondStringList.Dispose();
            secondStringList = res.GetStringListProp( propSecondStringList );
            Assert.AreEqual( 1, secondStringList.Count );

            valueList = res.GetStringListProp( _propValueList );
            Assert.AreEqual( 1, valueList.Count );
        }

        [Test] public void ThreeStringLists()
        {
            int propSecondStringList = _storage.PropTypes.Register( "SecondStringList", PropDataType.StringList );
            int propThirdStringList = _storage.PropTypes.Register( "ThirdStringList", PropDataType.StringList );
            IResource res = _storage.NewResource( "Person" );
            res.GetStringListProp( _propValueList ).Add( "Dmitry" );
            res.GetStringListProp( propSecondStringList ).Add( "Dmitrij" );
            res.GetStringListProp( propThirdStringList ).Add( "Dima" );

            ReopenStorage();
            res = _storage.LoadResource( res.Id );
            IStringList valueList = res.GetStringListProp( _propValueList );
            Assert.AreEqual( 1, valueList.Count );
            IStringList secondStringList = res.GetStringListProp( propSecondStringList );
            Assert.AreEqual( 1, secondStringList.Count );
            IStringList thirdStringList = res.GetStringListProp( propThirdStringList );
            Assert.AreEqual( 1, thirdStringList.Count );

            valueList.Dispose();
            secondStringList.Dispose();
            thirdStringList.Dispose();

            Assert.AreEqual( 1, valueList.Count );
            Assert.AreEqual( 1, secondStringList.Count );
            Assert.AreEqual( 1, thirdStringList.Count );
        }

        [Test] public void PropAfterDelete()
        {
            IResource res = _storage.NewResource( "Person" );
            res.SetProp( "FirstName", "Dmitry" );
            res.Delete();
            Assert.IsNull( res.GetProp( "FirstName" ) );
        }
        
        [Test] public void ChangeType()
        {
            IResource res = _storage.NewResource( "Person" );
            res.ChangeType( "Email" );
            Assert.AreEqual( res.Type, "Email" );

            ReopenStorage();
            IResource res2  = _storage.LoadResource( res.Id );
            Assert.AreEqual( res2.Type, "Email" );
        }

        [Test] public void SetPropAfterShutdown()
        {
            IResource res = _storage.NewResource( "Person" );
            res.SetProp( "FirstName", "Dmitry" );
            CloseStorage();
            Assert.IsTrue( res.HasProp( "FirstName" ) );
        }

        [Test] public void LinksOfTypeAfterShutdown()
        {
            IResource res = _storage.NewResource( "Person" );
            IResource res2 = _storage.NewResource( "Email" );
            res.AddLink( _propAuthor, res2 );
            ReopenStorage();
            res = _storage.LoadResource( res.Id );
            res2 = _storage.LoadResource( res2.Id );
            CloseStorage();
            Assert.AreEqual( 0, res.GetLinksOfType( null, _propAuthor ).Count );
            Assert.AreEqual( 0, res2.GetLinksOfType( null, _propAuthor ).Count );
        }

        [Test] public void DisplayNameProvider()
        {
            _storage.RegisterDisplayNameProvider( new MockDisplayNameProvider() );
            IResource res = _storage.NewResource( "Person" );
            Assert.AreEqual( "MockDisplayName", res.DisplayName );
        }

        private class MockDisplayNameProvider: IDisplayNameProvider
        {
            public string GetDisplayName( IResource res )
            {
                return "MockDisplayName";
            }
        }

        private void VerifyNoPropType( int propType )
        {
            bool gotException = false;
            try
            {
                IPropType pt = _storage.PropTypes [propType]; pt = pt;
            }
            catch( StorageException )
            {
                gotException = true;
            }
            Assert.IsTrue( gotException, "Getting the prop name of a non-existing property should throw" );
        }

        private void VerifyNoPropName( string name )
        {
            bool gotException = false;
            try
            {
                _storage.GetPropId( name );
            }
            catch( StorageException )
            {
                gotException = true;
            }
            Assert.IsTrue( gotException, "Getting the prop ID of a non-existing property should throw" );
        }
    }
}
