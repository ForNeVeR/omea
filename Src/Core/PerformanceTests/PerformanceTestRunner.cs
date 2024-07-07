// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Xml;
using JetBrains.PerformanceTestsFramework;

namespace PerformanceTests
{
    public abstract class PerformanceTestBase
    {
        public virtual void SetUp()
        {

        }

        public virtual void TearDown()
        {

        }

        public int RunTest()
        {
            long startTicks = DateTime.Now.Ticks;
            DoTest();
            long endTicks = DateTime.Now.Ticks;
            return (int) ((endTicks - startTicks) / 10000);
        }

        public abstract void DoTest();
    }

    [Serializable]
    public class PerformanceTestRunner: IPerformanceTestRunner
	{
	    public void Initialize( XmlNode configNode, XmlNode testNode )
	    {
	    }

	    public int RunTest( object testInstance )
	    {
	        PerformanceTestBase perfTest = (PerformanceTestBase) testInstance;
            perfTest.SetUp();
            int ticks = perfTest.RunTest();
            perfTest.TearDown();
            return ticks;
	    }
	}
}
