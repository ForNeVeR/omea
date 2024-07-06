// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.IO;
using System.Net;
using System.Xml;
using JetBrains.Omea.Base;
using NUnit.Framework;

namespace RemoteControl.Tests.RemoteCalls
{
	[TestFixture]
	public class Simple : Base
	{
		[Test]
		public void Status()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
			_rsp = GetPOSTRepsonse("NewMethod", "i=1&s=aaa%20bbb&b=0");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
		}

		[Test]
		public void ResultAnswerStructure()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
			_rsp = GetPOSTRepsonse("NewMethod", "i=1&s=aaa%20bbb&b=0");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );

			string result = Utils.StreamToString( _rsp.GetResponseStream() );

			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			Assert.AreEqual( 1, xres.ChildNodes.Count );
			//Assert.AreEqual( XmlNodeType.XmlDeclaration, xres.ChildNodes[0].NodeType );
			Assert.AreEqual( XmlNodeType.Element, xres.ChildNodes[0].NodeType );
			Assert.AreEqual( "result", xres.ChildNodes[0].Name );
			Assert.IsNotNull( xres.ChildNodes[0].Attributes["status"] );
			Assert.AreEqual( "ok", xres.ChildNodes[0].Attributes["status"].Value );
		}

		[Test]
		public void StringResult()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
			_rsp = GetPOSTRepsonse("NewMethod", "i=1&s=aaa%20bbb&b=1");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			string result = Utils.StreamToString( _rsp.GetResponseStream() );

			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			XmlNode retval = xres.SelectSingleNode( "/result/string[@name='retval']" );
			Assert.IsNotNull( retval );
			Assert.AreEqual( retval.InnerText, properDelegateIntegral(1,"aaa bbb",true) );
		}

		[Test]
		public void WithDefaultStringParameter()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
			_rsp = GetPOSTRepsonse("NewMethod", "i=1&b=1");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			string result = Utils.StreamToString( _rsp.GetResponseStream() );
			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			XmlNode retval = xres.SelectSingleNode( "/result/string[@name='retval']" );
			Assert.IsNotNull( retval );
			Assert.AreEqual( retval.InnerText, properDelegateIntegral(1,"",true) );
		}

		[Test]
		public void WithDefaultBoolParameter()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
			_rsp = GetPOSTRepsonse("NewMethod", "i=1&s=a");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			string result = Utils.StreamToString( _rsp.GetResponseStream() );
			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			XmlNode retval = xres.SelectSingleNode( "/result/string[@name='retval']" );
			Assert.IsNotNull( retval );
			Assert.AreEqual( retval.InnerText, properDelegateIntegral(1,"a",false) );
		}

		[Test]
		public void WithBoolParemeterSetToFalse()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
			_rsp = GetPOSTRepsonse("NewMethod", "i=1&s=a&b=0");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			string result = Utils.StreamToString( _rsp.GetResponseStream() );
			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			XmlNode retval = xres.SelectSingleNode( "/result/string[@name='retval']" );
			Assert.IsNotNull( retval );
			Assert.AreEqual( retval.InnerText, properDelegateIntegral(1,"a",false) );
		}

		[Test]
		public void WithBoolParameterSetToTrue()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
			_rsp = GetPOSTRepsonse("NewMethod", "i=1&s=a&b=yes");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			string result = Utils.StreamToString( _rsp.GetResponseStream() );
			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			XmlNode retval = xres.SelectSingleNode( "/result/string[@name='retval']" );
			Assert.IsNotNull( retval );
			Assert.AreEqual( retval.InnerText, properDelegateIntegral(1,"a",true) );
		}

		[Test]
		public void WithBoolParameterWithoutvalue()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
			_rsp = GetPOSTRepsonse("NewMethod", "i=1&s=a&b");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			string result = Utils.StreamToString( _rsp.GetResponseStream() );
			XmlDocument xres = new XmlDocument();
			xres.LoadXml( result );

			XmlNode retval = xres.SelectSingleNode( "/result/string[@name='retval']" );
			Assert.IsNotNull( retval );
			Assert.AreEqual( retval.InnerText, properDelegateIntegral(1,"a",true) );
		}

		[Test]
		public void WithBadHTTPMethod()
		{
			HttpWebRequest req = null;

			try
			{
				req = WebRequest.Create(RootURL + "xml/System.ListAllMethods") as HttpWebRequest;
				req.Method = "GET";
				req.SendChunked = false;
				_rsp = req.GetResponse() as HttpWebResponse;
				Assert.AreEqual( HttpStatusCode.MethodNotAllowed, _rsp.StatusCode );
			}
			catch(WebException ex)
			{
				Assert.AreEqual( HttpStatusCode.MethodNotAllowed, (ex.Response as HttpWebResponse).StatusCode );
			}
		}

		[Test]
		public void WithUnknwonMethod()
		{
			try
			{
				_rsp = GetPOSTRepsonse("NewMethod", "");
				Assert.AreEqual( HttpStatusCode.NotFound, _rsp.StatusCode );
			}
			catch(WebException ex)
			{
				Assert.AreEqual( HttpStatusCode.NotFound, (ex.Response as HttpWebResponse).StatusCode );
			}
		}

		[Test]
		public void WithInvalidContentType()
		{
			try
			{
				_rsp = GetPOSTRepsonse("NewMethod", "", "text/plain");
				Assert.AreEqual( HttpStatusCode.UnsupportedMediaType, _rsp.StatusCode );
			}
			catch(WebException ex)
			{
				Assert.AreEqual( HttpStatusCode.UnsupportedMediaType, (ex.Response as HttpWebResponse).StatusCode );
			}
		}

		[Test]
		public void ContetnTypeOfResult()
		{
			_rsp = GetPOSTRepsonse("System.ListAllMethods", "");
			Assert.AreEqual( HttpStatusCode.OK, _rsp.StatusCode );
			Assert.AreEqual( "text/xml", _rsp.ContentType );
		}
	}
}
