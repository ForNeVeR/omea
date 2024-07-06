// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using NUnit.Framework;

namespace OmniaMeaBaseTests
{
    [TestFixture]
    public class HashFunctionTests
    {
        [Test]
        public void Test()
        {
            Assert.IsTrue( DBIndex.HashFunctions.HashiString64( "ХоРоШо!" ) ==
                DBIndex.HashFunctions.HashiString64( "хОрОшО!" ) );
            Assert.IsTrue( DBIndex.HashFunctions.HashiString32( "ЛжеДмитрий" ) ==
                DBIndex.HashFunctions.HashiString32( "лжеДмитриЙ" ) );
            Assert.IsTrue( DBIndex.HashFunctions.HashiString64( "ЛжеДмитрий" ) ==
                DBIndex.HashFunctions.HashiString64( "лжеДмитриЙ" ) );
            Assert.IsFalse( DBIndex.HashFunctions.HashiString64( "ЛжеДмитрий" ) ==
                DBIndex.HashFunctions.HashiString64( "лжеДмидриЙ" ) );
            Assert.IsTrue( DBIndex.HashFunctions.HashiString64( "LJDmitry" ) ==
                DBIndex.HashFunctions.HashiString64( "ljdmitry" ) );
        }
    }
}
