// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using JetBrains.Omea.RemoteControl;
using NUnit.Framework;

namespace RemoteControl.Tests
{
    [TestFixture]
    public class Client : RemoteControl.Tests.Base
    {
        [SetUp]
        new public void SetUp()
        {
            base.SetUp();
            Assert.IsTrue( _srv.AddRemoteCall( "IntegralMethod",       new ProperDelegateIntegral(properDelegateIntegral) ) );
            Assert.IsTrue( _srv.AddRemoteCall( "StructMethod",         new ProperDelegateStruct(properDelegateStruct) ) );
            Assert.IsTrue( _srv.AddRemoteCall( "RetStructMethod",      new ProperDelegateRetStruct(properDelegateRetStruct) ) );
            Assert.IsTrue( _srv.AddRemoteCall( "RetArrayMethod",       new ProperDelegateRetArray(properDelegateRetArray) ) );
            Assert.IsTrue( _srv.AddRemoteCall( "RetArrayStructMethod", new ProperDelegateRetArrayStruct(properDelegateRetArrayStruct) ) );
        }
        [TearDown]
        new public void TearDown()
        {
            base.TearDown();
        }

		[Test]
		public void IntegralMethod()
		{
			RemoteControlClient cl = new RemoteControlClient( _port, RemoteControlServer.protectionKey );
			object ret = cl.SendRequest( "IntegralMethod", new object[] { "i", 1, "s", "qwerty", "b", true } );
			Assert.IsNotNull( ret as string );
			Assert.AreEqual( properDelegateIntegral( 1, "qwerty", true ), ret as string );
		}

		[Test]
		public void StructMethod()
		{
			RemoteControlClient cl = new RemoteControlClient( _port, RemoteControlServer.protectionKey );
			TestStruct s = new TestStruct();
			s.i = 1;
			s.s = "qwery";
			s.b = true;
			object ret = cl.SendRequest( "StructMethod", new object[] { "param", s } );
			Assert.IsNotNull( ret as string );
			Assert.AreEqual( properDelegateStruct( s ), ret as string );
		}

		[Test]
		public void RetStructMethod()
		{
			RemoteControlClient cl = new RemoteControlClient( _port, RemoteControlServer.protectionKey );
			TestStruct s = new TestStruct();
			object ret = cl.SendRequest( "RetStructMethod", new object[] { "i", 1, "s", "qwerty", "b", true } );

			Assert.IsNotNull( ret as Hashtable );
			Hashtable h = ret as Hashtable;
			Assert.IsTrue( h.ContainsKey( "i" ) );
			s.i = (int)h["i"];
			Assert.IsTrue( h.ContainsKey( "s" ) );
			s.s = (string)h["s"];
			Assert.IsTrue( h.ContainsKey( "b" ) );
			s.b = (bool)h["b"];
			Assert.AreEqual( properDelegateRetStruct( 1, "qwerty", true ), s );
		}

		[Test]
		public void RetArrayMethod()
		{
			RemoteControlClient cl = new RemoteControlClient( _port, RemoteControlServer.protectionKey );
			object ret = cl.SendRequest( "RetArrayMethod", new object[] { "i", 1, "s", "qwerty", "b", true } );

			string[] ra = properDelegateRetArray( 1, "qwerty", true );

			Assert.IsNotNull( ret as ArrayList );
			ArrayList a = ret as ArrayList;
			Assert.IsNotNull( a[0] as string );
			Assert.AreEqual( ra[0], a[0] as string );
			Assert.IsNotNull( a[1] as string );
			Assert.AreEqual( ra[1], a[1] as string );
			Assert.IsNotNull( a[2] as string );
			Assert.AreEqual( ra[2], a[2] as string );
		}

		[Test]
		public void RetArrayStructMethod()
		{
			RemoteControlClient cl = new RemoteControlClient( _port, RemoteControlServer.protectionKey );
			object ret = cl.SendRequest( "RetArrayStructMethod", new object[] { "i", 1, "s", "qwerty", "b", true } );

			Assert.IsNotNull( ret as ArrayList );
			ArrayList a = ret as ArrayList;
			Hashtable h;
			TestStruct[] ras = properDelegateRetArrayStruct( 1, "qwerty", true );

			Assert.IsNotNull( a[0] as Hashtable );
			h = a[0] as Hashtable;
			Assert.IsTrue( h.ContainsKey( "i" ) );
			Assert.AreEqual( ras[0].i, (int)h["i"] );
			Assert.IsTrue( h.ContainsKey( "s" ) );
			Assert.AreEqual( ras[0].s, (string)h["s"] );
			Assert.IsTrue( h.ContainsKey( "b" ) );
			Assert.AreEqual( ras[0].b, (bool)h["b"] );

			Assert.IsNotNull( a[1] as Hashtable );
			h = a[1] as Hashtable;
			Assert.IsTrue( h.ContainsKey( "i" ) );
			Assert.AreEqual( ras[1].i, (int)h["i"] );
			Assert.IsTrue( h.ContainsKey( "s" ) );
			Assert.AreEqual( ras[1].s, (string)h["s"] );
			Assert.IsTrue( h.ContainsKey( "b" ) );
			Assert.AreEqual( ras[1].b, (bool)h["b"] );
		}
	}
}
