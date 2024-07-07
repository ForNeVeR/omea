// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using JetBrains.Omea.OpenAPI;

namespace CommonTests
{
    [TestFixture]
    public class ConcurrentTests: MyPalDBTests
    {
        [SetUp] public void SetUp()
        {
            InitStorage();
            RegisterResourcesAndProperties();
        }

        [TearDown] public void TearDown()
        {
            CloseStorage();
        }

        [Test] public void CreateThread()
        {
            AsyncTestCollection coll = new AsyncTestCollection( _storage );
            coll.AddRunner( new BaseTestRunner() );
            coll.RunTests();
            TestRunner runner = (TestRunner) coll.Runners [0];
            Assert.AreEqual( 1, runner.Exceptions.Count );
        }

        [Test] public void ConcurrentResourceOperations()
        {
            AsyncTestCollection coll = new AsyncTestCollection( _storage );
            for( int i=0; i<10; i++ )
            {
                _storage.PropTypes.Register( "Test" + i, PropDataType.String );
                coll.AddRunner( new ResourceTestRunner( i ) );
            }

            coll.RunTests();
            coll.DumpExceptions();
            foreach( TestRunner runner in coll.Runners )
            {
                Assert.AreEqual( 0, runner.Exceptions.Count );
            }
        }

        [Test] public void ConcurrentResourceDelete()
        {
            AsyncTestCollection coll = new AsyncTestCollection( _storage );
            coll.Timeout = 100000;
            coll.AddRunner( new CreateDeleteTestRunner() );
            for( int i=0; i<10; i++ )
            {
                coll.AddRunner( new LoadResourceTestRunner() );
            }
            //coll.AddRunner( new SortTodayResourcesTestRunner() );

            coll.RunTests();
            coll.DumpExceptions();
            foreach( TestRunner runner in coll.Runners )
            {
                Assert.AreEqual( 0, runner.Exceptions.Count );
            }
        }

        [Test] public void LiveSnapshotVsDelete()
        {
            for( int i=0; i<2000; i++ )
            {
                IResource res = _storage.NewResource( "Email" );
                res.SetProp( "Received", DateTime.Now );
            }

            AsyncTestCollection coll = new AsyncTestCollection( _storage );
            coll.Timeout = 100000;
            coll.AddRunner( new DeleteEmailsTestRunner() );
            coll.AddRunner( new FindResourcesTestRunner() );

            coll.RunTests();
            coll.DumpExceptions();
            foreach( TestRunner runner in coll.Runners )
            {
                Assert.AreEqual( 0, runner.Exceptions.Count );
            }
        }

        [Test] public void SortVsDelete()
        {
            for( int i=0; i<20000; i++ )
            {
                IResource res = _storage.NewResource( "Email" );
                res.SetProp( "Received", DateTime.Now );
            }

            AsyncTestCollection coll = new AsyncTestCollection( _storage );
            coll.Timeout = 100000;
            coll.AddRunner( new DeleteEmailsTestRunner() );
            coll.AddRunner( new SortAllEmailsTestRunner() );

            coll.RunTests();
            coll.DumpExceptions();
            foreach( TestRunner runner in coll.Runners )
            {
                Assert.AreEqual( 0, runner.Exceptions.Count );
            }
        }

        [Test] public void InsertSortedVsDelete()
        {
            for( int i=0; i<20000; i++ )
            {
                IResource res = _storage.NewResource( "Email" );
                res.SetProp( "Subject", i.ToString() );
            }

            AsyncTestCollection coll = new AsyncTestCollection( _storage );
            coll.Timeout = 100000;
            coll.AddRunner( new DeleteEmailsTestRunner() );
            coll.AddRunner( new InsertSortedTestRunner() );

            coll.RunTests();
            coll.DumpExceptions();
            foreach( TestRunner runner in coll.Runners )
            {
                Assert.AreEqual( 0, runner.Exceptions.Count );
            }
        }

        [Test] public void ResourceListUpdateManagerDeadlock()
        {
            AsyncTestCollection coll = new AsyncTestCollection( _storage );
            coll.Timeout = 100000;
            coll.AddRunner( new CreateDeleteTestRunner() );
            coll.AddRunner( new GetAllLiveTestRunner() );

            coll.RunTests();
            coll.DumpExceptions();
            foreach( TestRunner runner in coll.Runners )
            {
                Assert.AreEqual( 0, runner.Exceptions.Count );
            }
        }
    }

    class AsyncTestCollection
    {
        private IResourceStore _resourceStore;
        private ArrayList _runners = new ArrayList();
        private ArrayList _threads = new ArrayList();
        private int _timeout = 10000;

        public AsyncTestCollection( IResourceStore store )
        {
            _resourceStore = store;
        }

        public IList Runners
        {
            get { return _runners; }
        }

        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        public void AddRunner( TestRunner runner )
        {
            _runners.Add( runner );
            runner.ResourceStore = _resourceStore;
            _threads.Add( new Thread( new ThreadStart( runner.Run ) ) );
        }

        public void RunTests()
        {
            foreach( Thread t in _threads )
            {
                t.Start();
            }
            foreach( Thread t in _threads )
            {
                bool joined = t.Join( _timeout );
                if ( !joined )
                    throw new Exception( "Failed to join thread in " + _timeout / 1000 + " seconds" );
                if ( !joined )
                {
                    t.Abort();
                }
            }
        }

        public void DumpExceptions()
        {
            foreach( TestRunner r in _runners )
            {
                foreach( Exception e in r.Exceptions )
                {
                    Console.WriteLine( e.ToString() );
                }
            }
        }
    }

    abstract class TestRunner
    {
        private ArrayList _exceptions = new ArrayList();
        protected IResourceStore _store;

        public IResourceStore ResourceStore
        {
            get { return _store; }
            set { _store = value; }
        }

        public IList Exceptions
        {
            get { return _exceptions; }
        }

        public void Run()
        {
            try
            {
                RunTests();
            }
            catch( Exception e )
            {
                _exceptions.Add( e );
            }
        }

        protected abstract void RunTests();
    }

    class BaseTestRunner: TestRunner
    {
        protected override void RunTests()
        {
            Assert.IsTrue( false, "Exception from a thread" );
        }
    }

    class ResourceTestRunner: TestRunner
    {
        private int _index;

        internal ResourceTestRunner( int index )
        {
            _index = index;
        }

        protected override void RunTests()
        {
            IResourceList resList = _store.GetAllResourcesLive( "Email" );
            bool found = false;

            IResource theRes = _store.NewResource( "Email" );
            theRes.SetProp( "Subject", "Test " + _index );

            for( int count=0; count<100; count++ )
            {
                int resCount = resList.Count;
                for( int i=0; i<resCount; i++ )
                {
                    IResource res = resList [i];
                    if ( res.Id == theRes.Id )
                    {
                        Assert.AreEqual( "Test " + _index, theRes.GetStringProp( "Subject" ) );
                        found = true;
                    }
                    res.SetProp( "Test" + _index, "Data" + count );
                }
                Assert.IsTrue( found );

                for( int i=0; i<resCount; i++ )
                {
                    Assert.AreEqual( "Data" + count, resList [i].GetStringProp( "Test" + _index ) );
                }
            }
        }
    }

    class CreateDeleteTestRunner: TestRunner
    {
        protected override void RunTests()
        {
            for( int i=0; i<1000; i++ )
            {
                IResource res = _store.NewResource( "Email" );
                res.SetProp( "Received", DateTime.Now );
            }
            int maxResourceID = 1000;

            Random rnd = new Random();
            for( int i=0; i<20000; i++ )
            {
                if ( rnd.Next(0, 1) == 0 )
                {
                    IResource res = _store.NewResource( "Email" );
                    res.SetProp( "Received", DateTime.Now );
                    maxResourceID = res.Id;
                }
                else
                {
                    int resID = rnd.Next( maxResourceID );
                    try
                    {
                        IResource res = _store.LoadResource( resID );
                        res.Delete();
                    }
                    catch( ResourceDeletedException )
                    {
                    }
                    catch( InvalidResourceIdException )
                    {
                    }
                }
            }
        }
    }

    class LoadResourceTestRunner: TestRunner
    {
        protected override void RunTests()
        {
            Random rnd = new Random();
            for( int i=0; i<100000; i++ )
            {
                int id = rnd.Next( 50000 );
                try
                {
                    _store.LoadResource( id );
                }
                catch( ResourceDeletedException )
                {
                }
                catch( InvalidResourceIdException )
                {
                }
            }
        }
    }

    class DeleteEmailsTestRunner: TestRunner
    {
        protected override void RunTests()
        {
            Random rnd = new Random();
            IResourceList allEmails = _store.GetAllResourcesLive( "Email" );
            while( allEmails.Count > 0 )
            {
                int i = rnd.Next( allEmails.Count );
                allEmails [i].Delete();
            }
        }

    }

    class FindResourcesTestRunner: TestRunner
    {
        protected override void RunTests()
        {
            ArrayList resourceLists = new ArrayList();
            for( int i=0; i<500; i++ )
            {
                IResourceList resList = _store.FindResourcesInRange( SelectionType.LiveSnapshot, null, "Received",
                    DateTime.Today, DateTime.MaxValue );
                int cnt = resList.Count; cnt = cnt;
                resourceLists.Add( resList );
            }
        }
    }

    class SortAllEmailsTestRunner: TestRunner
    {
        protected override void RunTests()
        {
            for( int i=0; i<1000; i++ )
            {
                IResourceList allEmails = _store.GetAllResources( "Email" );
                allEmails.Sort( "Received" );
                int cnt = allEmails.Count;  // ensures that the list is instantiated
                cnt = cnt;
            }
        }
    }

    class InsertSortedTestRunner: TestRunner
    {
        protected override void RunTests()
        {
            for( int i=0; i<100; i++ )
            {
                IResourceList allEmails = _store.GetAllResourcesLive( "Email" );
                allEmails.Sort( "Subject" );
                int cnt = allEmails.Count;  // ensures that the list is instantiated
                cnt = cnt;

                IResource newEmail = _store.BeginNewResource( "Email" );
                newEmail.SetProp( "Subject", i.ToString() );
                newEmail.EndUpdate();
            }
        }
    }

    class GetAllLiveTestRunner : TestRunner
    {
        protected override void RunTests()
        {
            for( int i=0; i<1000; i++ )
            {
                IResourceList allEmails = _store.GetAllResourcesLive( "Email" );
                allEmails.ResourceAdded += new ResourceIndexEventHandler(allEmails_ResourceAdded);
                allEmails.ResourceDeleting += new ResourceIndexEventHandler(allEmails_ResourceDeleting);
                int cnt = allEmails.Count; cnt = cnt;
                allEmails.Dispose();
            }
        }

        private void allEmails_ResourceAdded(object sender, ResourceIndexEventArgs e)
        {

        }

        private void allEmails_ResourceDeleting(object sender, ResourceIndexEventArgs e)
        {

        }
    }
}
