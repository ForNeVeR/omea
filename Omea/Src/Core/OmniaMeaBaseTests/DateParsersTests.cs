// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using NUnit.Framework;
using JetBrains.Omea.Base;

namespace OmniaMeaBaseTests
{
    [TestFixture]
    public class DateParsersTests
    {
        private void AssertEquals( object expected, object actual )
        {
            Assert.AreEqual( expected, actual );
        }
        [Test]
        public void Test()
        {
            DateTime date = RFC822DateParser.ParseDate( "Fri, 20 Feb 2004 11:31:17 -0700" );
            AssertEquals( 2004, date.Year );
            AssertEquals( 2, date.Month );
            AssertEquals( 20, date.Day );
            AssertEquals( 21, date.Hour );
            AssertEquals( 31, date.Minute );
        }

        [Test] public void TwoDigitYearTest()
        {
            DateTime date = RFC822DateParser.ParseDate( "Fri, 20 Feb 04 11:31:17 -0700" );
            AssertEquals( 2004, date.Year );
            AssertEquals( 2, date.Month );
            AssertEquals( 20, date.Day );
            AssertEquals( 21, date.Hour );
            AssertEquals( 31, date.Minute );
        }

        [Test] public void OneDigitDayTest()
        {
            DateTime date = RFC822DateParser.ParseDate( "Mon, 2 Aug 2004 11:31:17 -0700" );
            AssertEquals( 2004, date.Year );
            AssertEquals( 8, date.Month );
            AssertEquals( 2, date.Day );
        }

        [Test] public void WrongWeekdayTest()
        {
            DateTime date = RFC822DateParser.ParseDate( "Tue, 2 Aug 2004 11:31:17 -0700" );
            AssertEquals( 2004, date.Year );
            AssertEquals( 8, date.Month );
            AssertEquals( 2, date.Day );
        }

        [Test] public void NoWeekdayTest()
        {
            DateTime date = RFC822DateParser.ParseDate( "02 Aug 2004 11:31:17 -0700" );
            AssertEquals( 2004, date.Year );
            AssertEquals( 8, date.Month );
            AssertEquals( 2, date.Day );
        }
    }

}
