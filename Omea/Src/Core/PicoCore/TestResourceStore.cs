// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using JetBrains.Omea.ResourceStore;
using NUnit.Framework;

namespace JetBrains.Omea.PicoCore
{
	/// <summary>
	/// The IResourceStore implementation used in tests.
	/// </summary>
	public class TestResourceStore: MyPalStorage, IDisposable
	{
		public TestResourceStore()
		{
            RemoveDBFiles();
            MyPalStorage.ResourceCacheSize = 16;
            MyPalStorage.CreateDatabase();
            DoOpenDatabase();
		}

        public TestResourceStore( bool reopen )
        {
            if ( !reopen )
            {
                RemoveDBFiles();
                MyPalStorage.ResourceCacheSize = 16;
                MyPalStorage.CreateDatabase();
            }
            DoOpenDatabase();
        }

	    public void Dispose()
	    {
            MyPalStorage.CloseDatabase();
            try
            {
                RemoveDBFiles();
            }
            catch ( Exception e )
            {
                Console.WriteLine( " Error cleaning DB files: " + e.Message );
            }
	    }

        public void Close()
        {
            MyPalStorage.CloseDatabase();
        }

	    private static void RemoveDBFiles()
        {
            if ( !Directory.Exists( MyPalStorage.DBPath ) )
                return;

            try
            {
                RemoveFilesWithExt( "*.dbUtil" );
                RemoveFilesWithExt( "*.blob" );
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.Message );
            }
        }

        private static void RemoveFilesWithExt( string mask )
        {
            string[] files = System.IO.Directory.GetFiles( MyPalStorage.DBPath, mask );
            foreach ( string fileName in files )
            {
                System.IO.File.Delete( fileName );
            }
        }
	}
}
