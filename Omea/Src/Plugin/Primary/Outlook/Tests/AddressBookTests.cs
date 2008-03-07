/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
