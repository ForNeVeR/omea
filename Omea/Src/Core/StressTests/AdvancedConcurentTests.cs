// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Threading;
using System.Diagnostics;
using NUnit.Framework;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Diagnostics;


namespace CommonTests
{
    [TestFixture]
    public class AdvancedConcurrentTests : MyPalDBTests
    {
        public const int ThreadsNumber = 10;
        private ConsoleOutListener _traceListener;

        [SetUp]
        public void SetUp()
        {
            InitStorage();
            RegisterResourcesAndProperties();
            for( int i = 0; i < _processors.Length; ++i )
            {
                _processors[ i ] = new AsyncProcessor( new AsyncExceptionHandler( ExceptionHandler ), false );
            }
            _wereExceptions = false;

            _traceListener = new ConsoleOutListener();
            Trace.Listeners.Add( _traceListener );
        }

        [TearDown]
        public void TearDown()
        {
            CloseStorage();
            Trace.Listeners.Remove( _traceListener );
        }

        [Test]
        public void StressMTStorageTest()
        {
            for( int i = 0; i < _processors.Length; ++i )
            {
                for( int j = 0; j < 100; ++j )
                {
                    _processors[ i ].QueueJob( new NewResource( _storage ) );
                    _processors[ i ].QueueJob( new NewResource( _storage ) );
                    _processors[ i ].QueueJob( new DeleteRandomResource( _storage ) );
                    _processors[ i ].QueueJob( new FindLinksAndUpdate( _storage, this._propSize ) );
                    _processors[ i ].QueueJob( new ChangeLinks( _storage ) );
                    _processors[ i ].QueueJob( new FindLinksAndUpdate( _storage, this._propSize ) );
                    _processors[ i ].QueueJob( new ChangeLinks( _storage ) );
                }
            }

            for( int i = 0; i < _processors.Length; ++i )
            {
                _processors[ i ].QueueEndOfWork();
                _processors[ i ].ThreadPriority = ThreadPriority.BelowNormal;
                _processors[ i ].StartThread();
            }

            for( int i = 0; i < _processors.Length; ++i )
                _processors[ i ].WaitUntilFinished();

            if( _wereExceptions )
                throw new Exception( "Multithreaded stress test of resource storage FAILED!" );
        }


        [Test]
        public void StressMTStorageTestUltraAdvanced()
        {
            int count = 0;
            for( int j = 0; j < 100; ++j )
            {
                for( int i = 0; i < _processors.Length; ++i )
                {
                    _processors[ i ].QueueJob( new NewSimpleResourceType( _storage, count++ ) );
                    _processors[ i ].QueueJob( new NewSimpleResource( _storage ) );
                    _processors[ i ].QueueJob( new NewSimpleProperty( _storage, count++ ) );
                    _processors[ i ].QueueJob( new NewSimpleProperty( _storage, count++ ) );
                    _processors[ i ].QueueJob( new NewSimpleResourceType( _storage, count++ ) );
                    _processors[ i ].QueueJob( new NewResource( _storage ) );
                    _processors[ i ].QueueJob( new DeleteRandomResource( _storage ) );
                    _processors[ i ].QueueJob( new FindLinksAndUpdate( _storage, this._propSize ) );
                    _processors[ i ].QueueJob( new ChangeLinks( _storage ) );
                    _processors[ i ].QueueJob( new FindLinksAndUpdate( _storage, this._propSize ) );
                    _processors[ i ].QueueJob( new ChangeLinks( _storage ) );
                    _processors[ i ].QueueJob( new NewSimpleResourceType( _storage, count++ ) );
                    _processors[ i ].QueueJob( new NewSimpleLink( _storage ) );

                }
            }

            for( int i = 0; i < _processors.Length; ++i )
            {
                _processors[ i ].QueueEndOfWork();
                _processors[ i ].ThreadPriority = ThreadPriority.BelowNormal;
                _processors[ i ].StartThread();
            }


            for( int i = 0; i < _processors.Length; ++i )
                _processors[ i ].WaitUntilFinished();

            if( _wereExceptions )
                throw new Exception( "Multithreaded stress test of resource storage FAILED!" );
        }
        [Test]
        public void StressMTStorageTestNewSimpleProperty()
        {
            int count = 0;
            for( int j = 0; j < 100; ++j )
            {
                for( int i = 0; i < _processors.Length; ++i )
                {
                    _processors[ i ].QueueJob( new NewSimpleProperty( _storage, count++ ) );
                    _processors[ i ].QueueJob( new NewSimpleProperty( _storage, count++ ) );
                    _processors[ i ].QueueJob( new NewSimpleProperty( _storage, count++ ) );
                }
            }

            for( int i = 0; i < _processors.Length; ++i )
            {
                _processors[ i ].QueueEndOfWork();
                _processors[ i ].ThreadPriority = ThreadPriority.BelowNormal;
                _processors[ i ].StartThread();
            }


            for( int i = 0; i < _processors.Length; ++i )
                _processors[ i ].WaitUntilFinished();

            if( _wereExceptions )
                throw new Exception( "Multithreaded stress test of resource storage FAILED!" );
        }
        [Test]
        public void StressMTStorageTestNewSimpleResourceType()
        {
            int count = 0;
            for( int j = 0; j < 100; ++j )
            {
                for( int i = 0; i < _processors.Length; ++i )
                {
                    _processors[ i ].QueueJob( new NewSimpleResourceType( _storage, count++ ) );
                    _processors[ i ].QueueJob( new NewSimpleResourceType( _storage, count++ ) );
                    _processors[ i ].QueueJob( new NewSimpleResourceType( _storage, count++ ) );
                }
            }

            for( int i = 0; i < _processors.Length; ++i )
            {
                _processors[ i ].QueueEndOfWork();
                _processors[ i ].ThreadPriority = ThreadPriority.BelowNormal;
                _processors[ i ].StartThread();
            }


            for( int i = 0; i < _processors.Length; ++i )
                _processors[ i ].WaitUntilFinished();

            if( _wereExceptions )
                throw new Exception( "Multithreaded stress test of resource storage FAILED!" );
        }


        private void ExceptionHandler( Exception e )
        {
            if( e is ResourceDeletedException )
                return;
            Exception innerException = e.InnerException;
            if ( innerException != null )
                ExceptionHandler( innerException );
            else
            {
                Tracer._TraceException( e );
                _wereExceptions = true;
            }
        }

        private AsyncProcessor[] _processors = new AsyncProcessor[ ThreadsNumber ];
        private bool _wereExceptions;
    }
    internal class NewSimpleLink : AbstractJob
    {
        private IResourceStore _store;
        private static int _resourceTypeID = -1;
        private static int _linkTypeID = -1;
        public NewSimpleLink( IResourceStore store )
            : base()
        {
            _store = store;
        }
        protected override void Execute()
        {
            IResource res = _store.LoadResource( 28 );
            if ( _resourceTypeID == -1 )
            {
                _resourceTypeID =
                    _store.ResourceTypes.Register( "New Resource Type", string.Empty );
                _linkTypeID =
                    _store.PropTypes.Register( "New Link", PropDataType.Link );
            }
            IResource target = _store.NewResource( "New Resource Type" );
            res.AddLink( _linkTypeID, target );
        }
    }
    internal class NewSimpleResource : AbstractJob
    {
        private IResourceStore _store;
        public NewSimpleResource( IResourceStore store )
            : base()
        {
            _store = store;
        }
        protected override void Execute()
        {
            IResource res = _store.NewResource( "Email" );
            res.SetProp( "Subject", res.Id.ToString() );
        }
    }
    internal class NewSimpleProperty : AbstractJob
    {
        private IResourceStore _store;
        private int _index;
        public NewSimpleProperty( IResourceStore store, int index )
            : base()
        {
            _store = store;
            _index = index;
        }
        protected override void Execute()
        {
            _store.PropTypes.Register( _index.ToString(), PropDataType.Int );
        }
    }
    internal class NewSimpleResourceType : AbstractJob
    {
        private IResourceStore _store;
        private int _index;
        public NewSimpleResourceType( IResourceStore store, int index )
            : base()
        {
            _store = store;
            _index = index;
        }
        protected override void Execute()
        {
            _store.ResourceTypes.Register( _index.ToString(), string.Empty );
        }
    }

    internal class NewResource : AbstractJob
    {
        private IResourceStore _store;
        public NewResource( IResourceStore store ) : base() { _store = store; }
        protected override void Execute()
        {
            IResource res = _store.NewResource( "Email" );
            res.SetProp( "Subject", res.Id.ToString() );
            IResource author = _store.NewResource( "Person" );
            author.SetProp( "LastName", "author" + res.Id.ToString() );
            res.AddLink( "Author", author );
            IResource reply = _store.NewResource( "Person" );
            reply.SetProp( "LastName", "reply" + res.Id.ToString() );
            res.AddLink( "Reply", reply );
            res.SetProp( "Size", res.Id );
        }
    }

    internal class DeleteRandomResource : AbstractJob
    {
        private IResourceStore _store;
        private static Random r = new Random();
        public DeleteRandomResource( IResourceStore store ) : base() { _store = store; }
        protected override void Execute()
        {
            IResourceList list = _store.GetAllResources( "Email" );
            if( list.Count > 0 )
            {
                list[ r.Next( list.Count ) ].Delete();
            }
        }
    }

    internal class FindLinksAndUpdate : AbstractJob
    {
        private static Random r = new Random();
        private IResourceStore _store;
        private int _propSize;
        public FindLinksAndUpdate( IResourceStore store, int propSize ) : base() { _store = store; _propSize = propSize; }
        protected override void Execute()
        {
            IResourceList list = _store.GetAllResources( "Email" );
            if( list.Count > 10 )
            {
                int i = r.Next( list.Count - 10 );
                IResourceList updatelist = _store.FindResourcesInRange( "Email", _propSize, i, i + 9999 );
                foreach( IResource res in updatelist )
                {
                    IResource author = res.GetLinkProp( "Author" );
                    author.SetProp( "LastName", author.GetStringProp( "LastName" ) + "updated" );
                }
            }
        }
    }

    internal class ChangeLinks : AbstractJob
    {
        private IResourceStore _store;
        private static Random r = new Random();
        public ChangeLinks( IResourceStore store ) : base() { _store = store; }
        protected override void Execute()
        {
            IResourceList list = _store.GetAllResources( "Email" );
            if( list.Count > 2 )
            {
                int i = r.Next( list.Count - 1 ) + 1;
                IResource res1 = list[ i - 1 ];
                IResource res2 = list[ i ];
                IResource author1 = res1.GetLinkProp( "Author" );
                IResource author2 = res2.GetLinkProp( "Author" );
                res1.SetProp( "Author", author2 );
                res2.SetProp( "Author", author1 );
            }
        }
    }

    class ConsoleOutListener : TraceListener
    {
        public override void Write( string message )
        {
            Console.Out.Write( message );
        }

        public override void WriteLine( string message )
        {
            Console.Out.WriteLine( message );
        }

    }
}
