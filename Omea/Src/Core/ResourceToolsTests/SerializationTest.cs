/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;
using JetBrains.Omea.ResourceTools;

namespace ResourceToolsTests
{
    [TestFixture]
    public class SerializationTests
    {
        private TestCore _core;
        private IResourceStore _storage;
        private int _propReply;
        private int _propAuthor;
        private int _propFrom;
        private int _propTo;
        private int _propSize;
        private int _propReceived;
        private int _propUnread;
        private int _propValueList;
        private int _propSimilarity;
        private int _propBody;

        [SetUp]
        public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;

            _storage.ResourceTypes.Register( "Person", "Name" );
            _storage.ResourceTypes.Register( "Email", "Name" );

            _propAuthor = _storage.PropTypes.Register( "Author", PropDataType.Link );
            _propReply = _storage.PropTypes.Register( "Reply", PropDataType.Link, PropTypeFlags.DirectedLink );
            _propFrom = _storage.PropTypes.Register( "From", PropDataType.Link );
            _propTo = _storage.PropTypes.Register( "To", PropDataType.Link );
            _propSize = _storage.PropTypes.Register( "Size", PropDataType.Int );
            _propReceived = _storage.PropTypes.Register( "Received", PropDataType.Date );
            _propUnread = _storage.PropTypes.Register( "IsUnread", PropDataType.Bool );
            _propValueList = _storage.PropTypes.Register( "ValueList", PropDataType.StringList );
            _propSimilarity = _storage.PropTypes.Register( "Similarity", PropDataType.Double );
            _propBody = _storage.PropTypes.Register( "Body", PropDataType.Blob );
        }

        [TearDown]
        public void TearDown()
        {
            _core.Dispose();
        }

        [Test]
        public void TestEmptyResource()
        {
            IResource person = _storage.NewResource( "Person" );
            Stream stream = ResourceBinarySerialization.Serialize( person );
            IResource restored = ResourceBinarySerialization.Deserialize( stream );
            Assert.AreEqual( person.Type, restored.Type, "Types of original and deserialized resources are not equal" );
        }

        [Test]
        public void TestLinks()
        {
            IResource origin = _storage.NewResource( "Email" );
            IResource reply = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            reply.AddLink( _propReply, origin );
            origin.AddLink( _propAuthor, person );
            Stream stream = ResourceBinarySerialization.Serialize( origin );
            origin.Delete();
            origin = ResourceBinarySerialization.Deserialize( stream );
            Assert.IsTrue( reply.HasLink( _propReply, origin ), "Reply has no link to deserialized origin" );
            Assert.IsTrue( person.HasLink( _propAuthor, origin ), "Person has no link to deserialized origin" );
        }

        public void TestComplexLinks()
        {
/*
            IResource e1In = _storage.NewResource( "Email" );
            e1In.SetProp( "Folder", "Humor" );
            IResource e1Out = _storage.NewResource( "Email" );
            e1Out.SetProp( "Folder", "Humor" );
*/
            IResource e2 = _storage.NewResource( "Email" );
//            e2.SetProp( "Folder", "Sergey" );
            IResource e2Reply = _storage.NewResource( "Email" );
//            e2Reply.SetProp( "Folder", "SentItems" );
            IResource e3 = _storage.NewResource( "Email" );
//            e3.SetProp( "Folder", "Sergey" );
            IResource e3Reply = _storage.NewResource( "Email" );
//            e3Reply.SetProp( "Folder", "SentItems" );

            IResource mySelf = _storage.NewResource( "Person" );
            IResource person = _storage.NewResource( "Person" );
            e2.AddLink( _propFrom, person );
            e2.AddLink( _propTo, mySelf );
            e2Reply.AddLink( _propFrom, mySelf );
            e2Reply.AddLink( _propTo, person );

            e3.AddLink( _propFrom, person );
            e3.AddLink( _propTo, mySelf );
            e3Reply.AddLink( _propFrom, mySelf );
            e3Reply.AddLink( _propTo, person );

            Console.WriteLine( person.GetLinksOfType( null, _propFrom ).Count.ToString() );
            Console.WriteLine( person.GetLinksOfType( null, _propTo ).Count.ToString() );
            Stream stream = ResourceBinarySerialization.Serialize( person );
            person.Delete();
            person = ResourceBinarySerialization.Deserialize( stream );
            Console.WriteLine( person.GetLinksOfType( null, _propFrom ).Count.ToString() );
            Console.WriteLine( person.GetLinksOfType( null, _propTo ).Count.ToString() );
            Assert.AreEqual( 2, person.GetLinksOfType( null, _propFrom ).Count, "There must be 2 links From person to MySelf" );
            Assert.AreEqual( 2, person.GetLinksOfType( null, _propTo ).Count, "There must be 2 links To person From MySelf" );
//            Assert( "There must be 2 links From MySelf to person", mySelf.GetLinksOfType( null, _propFrom ).Count == 2 );
//            Assert( "There must be 2 links To MySelf From person", mySelf.GetLinksOfType( null, _propTo ).Count == 2 );
/*
            reply.AddLink( _propReply, origin );
            origin.AddLink( _propAuthor, person );
            Stream stream = ResourceBinarySerialization.Serialize( origin );
            origin.Delete();
            origin = ResourceBinarySerialization.Deserialize( stream );
            Assert( "Reply has no link to deserialized origin", reply.HasLink( _propReply, origin ) );
            Assert( "Person has no link to deserialized origin", person.HasLink( _propAuthor, origin ) );
*/
        }

        [Test]
        public void TestMixedProps()
        {
            IResource origin = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            origin.AddLink( _propAuthor, person );
            origin.SetProp( _propSize, 100 );
            DateTime now = DateTime.Now;
            origin.SetProp( _propReceived, now );
            IStringList strLst = origin.GetStringListProp( _propValueList );
            using( strLst )
            {
                strLst.Add( "One" );
                strLst.Add( "Two" );
                strLst.Add( "Three" );
            }
            origin.SetProp( _propUnread, true );
            origin.SetProp( _propSimilarity, 1.0 );
            Stream stream = ResourceBinarySerialization.Serialize( origin );
            origin.Delete();
            origin = ResourceBinarySerialization.Deserialize( stream );
            Assert.IsTrue( person.HasLink( _propAuthor, origin ), "Person has no link to deserialized origin" );
            Assert.AreEqual( 100, origin.GetIntProp( _propSize ), "Deserialized origin has invalid size" );
            Assert.AreEqual( now, origin.GetDateProp( _propReceived ), "Deserialized origin has invalid received date" );
            strLst = origin.GetStringListProp( _propValueList );
            using( strLst )
            {
                Assert.IsTrue( strLst.Count == 3 && strLst[ 0 ] == "One" && strLst[ 1 ] == "Two" && strLst[ 2 ] == "Three",
                    "Deserialized origin has invalid value list" );
            }
            Assert.IsTrue( origin.HasProp( _propUnread ), "Deserialized origin is read" );
            Assert.IsTrue( origin.GetDoubleProp( _propSimilarity ) == 1.0, "Deserialized origin has invalid similarity" );
        }

        [Test]
        public void TestBlobProps()
        {
            IResource origin = _storage.NewResource( "Email" );
            MemoryStream body = new MemoryStream( Encoding.ASCII.GetBytes( "This is a body" ) );
            origin.SetProp( _propBody, body );
            Stream stream = ResourceBinarySerialization.Serialize( origin );
            origin.Delete();
            origin = ResourceBinarySerialization.Deserialize( stream );
            stream = origin.GetBlobProp( _propBody );
            Assert.AreEqual( "This is a body", Utils.StreamToString( stream, Encoding.ASCII ),
                "Types of original and deserialized resources are not equal" );
        }

        [Test]
        public void TestStringListProps()
        {
            ResourceSerializer serializer = new ResourceSerializer();
            IResource origin = _storage.NewResource( "Email" );
            origin.SetProp( _propSize, 100 );
            origin.SetProp( _propReceived, DateTime.Now );
            IStringList strLst = origin.GetStringListProp( _propValueList );
            using( strLst )
            {
                strLst.Add( "One" );
                strLst.Add( "Two" );
                strLst.Add( "Three" );
            }
            origin.SetProp( _propUnread, true );
            origin.SetProp( _propSimilarity, 1.0 );
            ResourceNode resNode = serializer.AddResource( origin );
            foreach( IResourceProperty prop in origin.Properties )
                resNode.AddProperty( prop );
            serializer.GenerateXML( "SerializationResult.xml");
            origin.Delete();

            StreamReader sr = new StreamReader( "SerializationResult.xml", Encoding.Default );
            string str = Utils.StreamReaderReadToEnd( sr );
            Console.WriteLine( str );
            sr.Close();

            ResourceDeserializer deserializer = new ResourceDeserializer( "SerializationResult.xml" );
            ArrayList list = deserializer.GetSelectedResources();
            Assert.AreEqual( 1, list.Count, "List must contain only one resource. Current count is [" + list.Count + "]" );
            ResourceUnpack ru = (ResourceUnpack)list[ 0 ];
            origin = ru.Resource;
            Assert.IsTrue( origin.HasProp( _propValueList ), 
                "Resource must contain StringList property" );
            IStringList stringsList = origin.GetStringListProp( _propValueList );
            Assert.AreEqual( 3, stringsList.Count, "StringList must contain three elements. Current count is [" + stringsList.Count + "]" );

            Assert.AreEqual( "One", stringsList [0], "StringList[ 0 ] must be equal to [One]. Current value is [" + stringsList[ 0 ] + "]" );
            Assert.AreEqual( "Two", stringsList [1],"StringList[ 1 ] must be equal to [Two]. Current value is [" + stringsList[ 1 ] + "]" );
            Assert.AreEqual( "Three", stringsList [2], "StringList[ 2 ] must be equal to [Three]. Current value is [" + stringsList[ 2 ] + "]" );
        }
    }
}
        