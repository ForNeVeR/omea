// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using CommonTests;
using JetBrains.Omea.RemoteControl;
using NUnit.Framework;

namespace RemoteControl.Tests
{
    public class Base
    {
		protected const int _port = 0xdead;

		protected string RootURL { get { return "http://127.0.0.1:" + _port.ToString() + "/"; } }

        protected const string _contentType = "application/x-www-form-urlencoded";

        protected delegate string ProperDelegateIntegral(int i, string s, bool b);
        protected string properDelegateIntegral(int i, string s, bool b)
        {
            return i + ":" + s + ":" + b;
        }

		public struct TestStruct
		{
			public int i;
			public string s;
			public bool b;
			public override string ToString()
			{
				return "{i:" + i.ToString() + ", s:" + s + ", b:" + b.ToString() + "}";
			}
		}
		protected delegate string ProperDelegateStruct(TestStruct param);
		protected string properDelegateStruct(TestStruct param)
		{
			return param.i + ":" + param.s + ":" + param.b;
		}

		protected delegate TestStruct ProperDelegateRetStruct(int i, string s, bool b);
		protected TestStruct properDelegateRetStruct(int i, string s, bool b)
		{
			TestStruct ret = new TestStruct();
			ret.i = i;
			ret.s = s;
			ret.b = b;
			return ret;
		}

		protected delegate string[] ProperDelegateRetArray(int i, string s, bool b);
		protected string[] properDelegateRetArray(int i, string s, bool b)
		{
			return new string[] { i.ToString() , s, b ? "yes" : "no" };
		}

		protected delegate TestStruct[] ProperDelegateRetArrayStruct(int i, string s, bool b);
		protected TestStruct[] properDelegateRetArrayStruct(int i, string s, bool b)
		{
			TestStruct[] ss = new TestStruct[2];
			ss[0].i = i;  ss[0].s = s;  ss[0].b = b;
			ss[1].i = -i; ss[1].s = ""; ss[1].b = !b;
			return ss;
		}

		protected delegate string NotproperDelegateParam(int i, string s, bool b, object o);
        protected string notproperDelegateParam(int i, string s, bool b, object o)
        {
            return "";
        }

        protected delegate object NotproperDelegateResult(int i, string s, bool b);
        protected object notproperDelegateResult(int i, string s, bool b)
        {
            return null;
        }

        protected MockPluginEnvironment _environment;
        protected RemoteControlServer _srv;

        [SetUp]
        public void SetUp()
        {
            _environment = new MockPluginEnvironment( null );
            _srv = new RemoteControlServer( _port );
			_srv.Start();
        }
        [TearDown]
        public void TearDown()
        {
            _srv.Stop();
            _srv = null;
        }
    }
}
