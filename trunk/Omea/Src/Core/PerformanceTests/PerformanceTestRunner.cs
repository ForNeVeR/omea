/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
