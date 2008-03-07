/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.OpenAPI;

namespace PerformanceTests
{
	/// <summary>
	/// Summary description for ResourceStorePerfTest.
	/// </summary>
	public abstract class ResourceStorePerfTest: PerformanceTestBase
	{
        protected IResourceStore _storage;

        public override void SetUp()
        {
            RemoveDBFiles();
            MyPalStorage.ResourceCacheSize = 16;
            MyPalStorage.CreateDatabase();
            MyPalStorage.OpenDatabase();
            _storage = MyPalStorage.Storage;
        }

        public override void TearDown()
        {
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

        public static void RemoveDBFiles()
        {
            if ( !Directory.Exists( MyPalStorage.DBPath ) )
                return;

            RemoveFilesWithExt( "*.dbUtil" );
            RemoveFilesWithExt( "*.blob" );
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

    public abstract class ResourceListSortTestBase: ResourceStorePerfTest
    {
        public override void SetUp()
        {
            base.SetUp();
            _storage.ResourceTypes.Register( "Test", "Name" );
            _storage.PropTypes.Register( "Date", PropDataType.Date );
            _storage.PropTypes.Register( "Index", PropDataType.Int );

            Random rnd = new Random();
                
            for( int i=0; i<10000; i++ )
            {
                IResource res = _storage.NewResource( "Test" );
                res.SetProp( "Index", i );
                res.SetProp( "Date", DateTime.Today.AddSeconds( rnd.Next( 86400 ) ) );
            }
        }

        protected void RunSortTest( int count )
        {
            IResourceList testList = _storage.FindResourcesInRange( "Test", "Index", 0, count );
            testList.Sort( "Date" );
            Console.WriteLine( testList.Count );
        }

        protected void RunIndexedSortTest( int count )
        {
            ResourceList testList = (ResourceList) _storage.FindResourcesInRange( "Test", "Index", 0, count );
            testList.IndexedSort( _storage.GetPropId( "Date" ) );
            Console.WriteLine( testList.Count );
        }
    }

    public class ResourceListSortTest1000: ResourceListSortTestBase
    {
        public override void DoTest()
        {
            RunSortTest( 1000 );
        }
    }

    public class ResourceListSortTest10000: ResourceListSortTestBase
    {
        public override void DoTest()
        {
            RunSortTest( 10000 );
        }
    }

    public class ResourceListIndexedSortTest1000: ResourceListSortTestBase
    {
        public override void DoTest()
        {
            RunIndexedSortTest( 1000 );
        }
    }

    public class ResourceListIndexedSortTest10000: ResourceListSortTestBase
    {
        public override void DoTest()
        {
            RunIndexedSortTest( 10000 );
        }
    }
}
