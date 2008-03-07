/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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