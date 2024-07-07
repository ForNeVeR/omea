// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Xml;

namespace JetBrains.PerformanceTestsFramework
{
  public interface IPerformanceTestRunner
  {
    void Initialize(XmlNode configNode, XmlNode testNode);

    int RunTest(object testInstance);
  }
}

