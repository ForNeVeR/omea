// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Threading;
using JetBrains.Omea;
using JetBrains.Omea.TextIndex;
using NUnit.Framework;
using JetBrains.Omea.OpenAPI;

namespace CommonTests
{
    [TestFixture]
    public class ConcurrentTests: MyPalDBTests
    {
        private ITextIndexManager  indexer;

        [SetUp] public void SetUp()
        {
            RemoveTextIndexFiles();
            OMEnv.DataDir = ".\\Data";
            InitStorage();

            new MockPluginEnvironment( _storage );
            indexer = new TextIndexManager();
            (indexer as TextIndexManager).StartIndexingThread();

            RegisterResourcesAndProperties();
        }

        [TearDown] public void TearDown()
        {
            CloseStorage();
//            ((TextIndexManager)indexer).RejectUnhostedJobs();
            //((TextIndexManager)indexer).EndBatchUpdate();
            ((TextIndexManager)indexer).Dispose();
            //((TextIndexManager)indexer).FlushAndCloseIndices();

            RemoveTextIndexFiles();
        }

        [Test] public void CreateThread()
        {
            AsyncTestCollection coll = new AsyncTestCollection( _storage );
            coll.AddRunner( new BaseTestRunner() );
            coll.RunTests();
            TestRunner runner = (TestRunner) coll.Runners [0];
            Assert.AreEqual( 1, runner.Exceptions.Count );
        }
    }

    class AsyncTestCollection
    {
        private IResourceStore _resourceStore;
        private ArrayList _runners  = new ArrayList();
        private ArrayList _threads  = new ArrayList();

        public AsyncTestCollection( IResourceStore store )
        {
            _resourceStore = _resourceStore;
            _resourceStore = store;
        }

        public IList Runners
        {
            get { return _runners; }
        }

        public void AddRunner( TestRunner runner )
        {
            _runners.Add( runner );
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
                bool joined = t.Join( 5 * 60 * 1000 );
                if ( !joined )
                    throw new Exception( "Failed to join thread in " + 5 * 60 + " seconds" );

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

    //-------------------------------------------------------------------------
    //  "Runners" classes emulate different logic of plugins and other
    //  components.
    //-------------------------------------------------------------------------
    abstract class TestRunner
    {
        private ArrayList           _exceptions = new ArrayList();
        protected IResourceStore    _store;
//        protected FullTextIndexer   Indexer;
        protected ITextIndexManager   Indexer;
        protected Random            Rand = new Random();
        protected static int        MaximalResId = 0;

        public IResourceStore ResourceStore
        {
            get { return _store; }
            set { _store = value; }
        }
        public IList Exceptions {  get { return _exceptions; } }

        public void Run()
        {
            try
            {
                RunTests();
            }
            catch( Exception e )
            {
                Console.WriteLine( e.Message );
                _exceptions.Add( e );
            }
        }

        protected string GenDoc()
        {
            int tokens = Rand.Next( 200 );
            string content = string.Empty;
            for( int i = 0; i < tokens; i++ )
            {
                content += Rand.Next( 10000 ).ToString() + "token" + Rand.Next( 10000 ).ToString() + " ";
            }
            return content;
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
}
