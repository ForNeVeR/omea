// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Net;
using System.Xml;
using JetBrains.Omea.Base;
using NUnit.Framework;

namespace RemoteControl.Tests.RemoteCalls
{
	[TestFixture]
	public class Struct : Base
	{
		[Test]
		public void WithStructParam()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateStruct(properDelegateStruct) ) );

			_rsp = GetPOSTRepsonse("NewMethod", "param.i=1&param.s=aaa%20bbb&param.b=1");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			string result = Utils.StreamToString( _rsp.GetResponseStream() );

			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			TestStruct param = new TestStruct();
			param.i = 1; param.s = "aaa bbb"; param.b = true;

			XmlNode retval = xres.SelectSingleNode( "/result/string[@name='retval']" );
			Assert.IsNotNull( retval );
			Assert.AreEqual( retval.InnerText, properDelegateStruct( param ) );
		}

		[Test]
		public void WithStructResult()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateRetStruct(properDelegateRetStruct) ) );
			_rsp = GetPOSTRepsonse("NewMethod", "i=1&s=aaa%20bbb&b=1");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			string result = Utils.StreamToString( _rsp.GetResponseStream() );

			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			TestStruct fromRemote = new TestStruct();
			TestStruct fromLocal  = properDelegateRetStruct( 1, "aaa bbb", true );

            XmlNode structElem = xres.SelectSingleNode( "/result[@status='ok']/struct[@name='retval']" );
			Assert.IsNotNull( structElem );

			XmlNode iElem = structElem.SelectSingleNode( "int[@name='i']" );
			Assert.IsNotNull( iElem );

			XmlNode sElem = structElem.SelectSingleNode( "string[@name='s']" );
			Assert.IsNotNull( sElem );

			XmlNode bElem = structElem.SelectSingleNode( "bool[@name='b']" );
			Assert.IsNotNull( bElem );

			fromRemote.i = Int32.Parse( iElem.InnerText );
			fromRemote.s = sElem.InnerText;
			fromRemote.b = bElem.InnerText != "0";

			Assert.AreEqual( fromLocal,  fromRemote);
		}

		[Test]
		public void WithArrayResult()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateRetArray(properDelegateRetArray) ) );

			_rsp = GetPOSTRepsonse("NewMethod", "i=1&s=aaa%20bbb&b=1");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			string result = Utils.StreamToString( _rsp.GetResponseStream() );

			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			string[] fromRemote = null;
			string[] fromLocal  = properDelegateRetArray( 1, "aaa bbb", true );

			XmlNode arrayElem = xres.SelectSingleNode( "/result[@status='ok']/array[@name='retval']" );
			Assert.IsNotNull( arrayElem );

			fromRemote = new string[ arrayElem.ChildNodes.Count ];
			int i = 0;
			foreach ( XmlNode val in arrayElem.ChildNodes )
			{
				Assert.AreEqual( XmlNodeType.Element, val.NodeType );
				Assert.AreEqual( "string" , val.Name );
				fromRemote[ i++ ] = val.InnerText;
			}

			Assert.AreEqual( fromLocal,  fromRemote );
		}

		[Test]
		public void WithArrayStructResult()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateRetArrayStruct(properDelegateRetArrayStruct) ) );

			_rsp = GetPOSTRepsonse("NewMethod", "i=1&s=aaa%20bbb&b=1");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			string result = Utils.StreamToString( _rsp.GetResponseStream() );

			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			TestStruct[] fromRemote = null;
			TestStruct[] fromLocal  = properDelegateRetArrayStruct( 1, "aaa bbb", true );

			XmlNode arrayElem = xres.SelectSingleNode( "/result[@status='ok']/array[@name='retval']" );
			Assert.IsNotNull( arrayElem );

			fromRemote = new TestStruct[ arrayElem.ChildNodes.Count ];
			int i = 0;
			foreach ( XmlNode structElem in arrayElem.ChildNodes )
			{
				Assert.AreEqual( XmlNodeType.Element, structElem.NodeType );
				Assert.AreEqual( "struct" , structElem.Name );

				XmlNode iElem = structElem.SelectSingleNode( "int[@name='i']" );
				Assert.IsNotNull( iElem );

				XmlNode sElem = structElem.SelectSingleNode( "string[@name='s']" );
				Assert.IsNotNull( sElem );

				XmlNode bElem = structElem.SelectSingleNode( "bool[@name='b']" );
				Assert.IsNotNull( bElem );

				fromRemote[ i   ].i = Int32.Parse( iElem.InnerText );
				fromRemote[ i   ].s = sElem.InnerText;
				fromRemote[ i++ ].b = bElem.InnerText != "0";
			}

			Assert.AreEqual( fromLocal,  fromRemote);
		}
	}
}
