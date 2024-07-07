// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.ResourceStore;
using CommonTests;
using NUnit.Framework;

namespace StressTests
{
    [TestFixture]
    public class MyPalStorageStressTests: MyPalDBTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }


        [Test] public void StressTest()
        {
            for ( int i = 0; i < 100; i++ )
            {
                RemoveDBFiles();
                MyPalStorage.CreateDatabase();
                MyPalStorage.OpenDatabase();
                _storage = MyPalStorage.Storage;

                MyPalStorage.CloseDatabase();
                _storage = null;
                try
                {
                    RemoveDBFiles();
                }
                catch ( Exception e )
                {
                    Console.WriteLine( " Error cleaning DB files: " + e.Message );
                }
            }
        }
    }
}

