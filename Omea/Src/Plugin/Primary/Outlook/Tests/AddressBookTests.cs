// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using EMAPILib;
using JetBrains.Omea.OutlookPlugin;
using NUnit.Framework;

namespace OutlookPlugin.Tests
{
    [TestFixture]
    public class AddressBookTests: OutlookTests
    {
        [Test] public void OpenAddressBookTest()
        {
            IEAddrBook addrBook = OutlookSession.GetAddrBook();
            Assert.IsNotNull( addrBook );
            int count = addrBook.GetCount();
            for ( int i = 0; i < count; ++i )
            {
                IEABContainer ab = addrBook.OpenAB( i );
                Assert.IsNotNull( ab );
                using ( ab )
                {
                }
            }
        }
    }
}
