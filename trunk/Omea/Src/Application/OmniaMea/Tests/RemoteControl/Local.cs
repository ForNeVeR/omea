/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using NUnit.Framework;

namespace RemoteControl.Tests
{

    [TestFixture]
    public class Local : Base
    {
        [Test]
        public void AddNewIntegralMethod()
        {
            Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
        }

		[Test]
		public void AddNewMethodWithStructParam()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateStruct(properDelegateStruct) ) );
		}

		[Test]
		public void AddNewMethodWithStructResult()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateRetStruct(properDelegateRetStruct) ) );
		}
		
		[Test]
		public void AddNewMethodWithArrayResult()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateRetArray(properDelegateRetArray) ) );
		}

		[Test]
		public void AddNewMethodWithArrayStructResult()
		{
			Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateRetArrayStruct(properDelegateRetArrayStruct) ) );
		}

		[Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddNewMethodWithUnsupportedParameters()
        {
            _srv.AddRemoteCall( "NewMethod", new NotproperDelegateParam(notproperDelegateParam));

        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddNewMethodWithUnsupportedResult()
        {
            _srv.AddRemoteCall( "NewMethod", new NotproperDelegateResult(notproperDelegateResult));
        }

        [Test]
        public void AddSameMethodTwice()
        {
            Assert.IsTrue( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
            Assert.IsFalse( _srv.AddRemoteCall( "NewMethod", new ProperDelegateIntegral(properDelegateIntegral) ) );
        }
    }
}
