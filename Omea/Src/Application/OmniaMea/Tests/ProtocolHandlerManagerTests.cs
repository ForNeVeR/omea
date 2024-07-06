// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using JetBrains.Omea.ResourceStore;
using NUnit.Framework;

namespace OmniaMea.Tests
{
	/// <summary>
	/// Summary description for ProtocolHandlerManagerTests.
	/// </summary>
    [TestFixture]
    public class ProtocolHandlerManagerTests
	{
        private TestCore _core;
        private IResourceStore _storage;
        private ProtocolHandlerManager _manager = null;
        private MockPlugin _plugin = null;
        private AsyncProcessor _resourceAP = null;

		public ProtocolHandlerManagerTests()
		{
		}
        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _resourceAP = new AsyncProcessor( true );
            _core.SetResourceAP( _resourceAP );
            _storage = _core.ResourceStore;
            _storage = _storage;
            MyPalStorage.Storage.OwnerThread = _resourceAP.Thread;
            _manager = new ProtocolHandlerManager();
            _core.SetProtocolHandlerManager( _manager );
            _plugin = new MockPlugin();
            _resourceAP.RunJob( new MethodInvoker( RegisterResources ) );
        }
        [TearDown] public void TearDown()
        {
            _core.Dispose();
            _resourceAP.Dispose();
        }
        private void RegisterResources()
        {
            _manager.RegisterResources();
        }
        [ExpectedException(typeof(ArgumentNullException))]
        [Test] public void TestWrongParameter1()
        {
            _manager.RegisterProtocolHandler( null, null, _plugin.Callback );
        }
        [ExpectedException(typeof(ArgumentNullException))]
        [Test] public void TestWrongParameter2()
        {
            _manager.RegisterProtocolHandler( "", null, _plugin.Callback );
        }
        [ExpectedException(typeof(ArgumentNullException))]
        [Test] public void TestWrongParameter3()
        {
            _manager.RegisterProtocolHandler( "", "", null );
        }

        [ExpectedException(typeof(ArgumentException))]
        [Test] public void TestWrongProtocolName1()
        {
            _manager.RegisterProtocolHandler( "", "", _plugin.Callback );
        }
        [ExpectedException(typeof(ArgumentException))]
        [Test] public void TestWrongProtocolName2()
        {
            _manager.RegisterProtocolHandler( "qwe:", "", _plugin.Callback );
        }

        [Test] public void TestInvoke()
        {
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.Callback );
            _manager.Invoke( "qwerty:query by qwerty" );
            Assert.AreEqual( "query by qwerty", _plugin.URL );
        }
        [Test] public void TestInvokeEmptyQuery()
        {
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.Callback );
            _manager.Invoke( "qwerty:" );
            Assert.AreEqual( "", _plugin.URL );
        }
        [Test] public void TestHandlerResetting()
        {
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.Callback );
            _manager.Invoke( "qwerty:1" );
            Assert.AreEqual( "1", _plugin.URL );

            MockPlugin plugin2 = new MockPlugin();
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", plugin2.Callback );
            _manager.Invoke( "qwerty:2" );
            Assert.AreEqual( "1", _plugin.URL );
            Assert.AreEqual( "2", plugin2.URL );
        }
        [Test] public void TestSetAndInvokeOpenURL()
        {
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.Callback );
            _manager.Invoke( "qwerty:1" );
            Assert.AreEqual( "1", _plugin.URL );
            _manager.SetOpenURL( "qwerty:2" );
            _manager.InvokeOpenUrl();
            Assert.AreEqual( "2", _plugin.URL );
        }
        [Test] public void TestCallbackWithException()
        {
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.CallbackWithException );
            _manager.Invoke( "qwerty:1" );
            Assert.AreEqual( _plugin._exception, _core._reportedException );
        }
        [Test] public void CheckRegisteredProtocolHandlers()
        {
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.Callback );
            IResourceList handlers = _manager.ProtocolHandlersList;
            Assert.AreEqual( 1, handlers.Count );
            IResource handler = handlers[0];
            Assert.AreEqual( "qwerty", handler.GetStringProp( ProtocolHandlerManager.PROTOCOL ) );
            Assert.AreEqual( "Simple Qwerty protocol", handler.GetStringProp( ProtocolHandlerManager.FNAME ) );
            Assert.AreEqual( false, handler.HasProp( ProtocolHandlerManager.DEFAULT ) );
            _manager.SaveProtocolSettings( handler, "new name", true );
            handlers = _manager.ProtocolHandlersList;
            Assert.AreEqual( 1, handlers.Count );
            handler = handlers[0];
            Assert.AreEqual( "qwerty", handler.GetStringProp( ProtocolHandlerManager.PROTOCOL ) );
            Assert.AreEqual( "new name", handler.GetStringProp( ProtocolHandlerManager.FNAME ) );
            Assert.AreEqual( true, handler.HasProp( ProtocolHandlerManager.DEFAULT ) );
        }

        [Test] public void TestInvokeMakeDefault()
        {
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.Callback, _plugin.MakeDefaultCallback );
            IResourceList handlers = _manager.ProtocolHandlersList;
            Assert.AreEqual( 1, handlers.Count );
            IResource handler = handlers[0];
            ProtocolHandlerManager.SetAsDefaultHandler( handler, true );
            Assert.AreEqual( true, _plugin.WasSetAsDefault );
        }


        [ExpectedException(typeof(ResourceRestrictionException))]
        [Test] public void CheckUniquessOfRegisteredProtocolName()
        {
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.Callback );
            _manager.RegisterProtocolHandler( "qwerty2", "Simple Qwerty protocol", _plugin.Callback );
            IResourceList handlers = _manager.ProtocolHandlersList;
            Assert.AreEqual( 2, handlers.Count );
            IResource handler = _manager.GetProtocolResource( "qwerty2" );
            Assert.IsNotNull( handler );
            ResourceProxy proxy = new ResourceProxy( handler );
            try
            {
                proxy.SetProp( ProtocolHandlerManager.PROTOCOL, "qwerty" );
            }
            catch ( AsyncProcessorException exception )
            {
                throw Utils.GetMostInnerException( exception );
            }
        }
        [Test] public void ProtocolFromRegistry()
        {
            ProtocolHandlersInRegistry.SetAsDefaultHandler( "omea", "This is Omea url protocol" );
            Assert.AreEqual( true, ProtocolHandlersInRegistry.IsDefaultHandler( "omea" ) );
        }
        [Test] public void SetProtocolToCheckDefault()
        {
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.Callback );
            Assert.AreEqual( true, _manager.IsCheckNeeded( "qwerty" ) );
            ProtocolHandlerManager.SetCheckNeeded( "qwerty", false );
            Assert.AreEqual( false, _manager.IsCheckNeeded( "qwerty" ) );
            ProtocolHandlerManager.SetCheckNeeded( "qwerty", true );
            Assert.AreEqual( true, _manager.IsCheckNeeded( "qwerty" ) );
        }
        [ExpectedException(typeof(ArgumentNullException))]
        [Test] public void MakeDefaultProtocolDelegateIsNull()
        {
            _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.Callback, null );
        }

        class RegisterProtocolHandler : AbstractJob
        {
            private Exception _exception = null;
            ProtocolHandlerManager _manager;
            MockPlugin _plugin;


            public RegisterProtocolHandler( ProtocolHandlerManager manager, MockPlugin plugin )
            {
                _manager = manager;
                _plugin = plugin;
            }
            protected override void Execute()
            {
                try
                {
                    _manager.RegisterProtocolHandler( "qwerty", "Simple Qwerty protocol", _plugin.Callback );
                }
                catch ( Exception exception )
                {
                    _exception = exception;
                }
            }
            public Exception Exception { get { return _exception; } }
        }

        [Test] public void RegisterFromAnotherThread()
        {
            AsyncProcessor processor = new AsyncProcessor( true );
            using ( processor )
            {
                RegisterProtocolHandler handler = new RegisterProtocolHandler( _manager, _plugin );
                processor.RunJob( handler );
                if ( handler.Exception != null )
                {
                    throw handler.Exception;
                }
            }
        }
    }
    [TestFixture]
    public class ResourcesNotRegisteredTests
    {
        private TestCore _core;
        private IResourceStore _storage;
        private ProtocolHandlerManager _manager = null;
        private MockPlugin _plugin = null;

        public ResourcesNotRegisteredTests()
        {
        }
        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            _storage = _storage;
            _manager = new ProtocolHandlerManager();
            _plugin = new MockPlugin();
        }
        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        [ExpectedException(typeof(ApplicationException))]
        [Test] public void RegisterProtocolHandler()
        {
            _manager.RegisterProtocolHandler( "querty", "QQQ", _plugin.Callback );
        }
        [ExpectedException(typeof(ApplicationException))]
        [Test] public void ProtocolHandlersList()
        {
            IResourceList list = _manager.ProtocolHandlersList;
            list = list;
        }
        [ExpectedException(typeof(ApplicationException))]
        [Test] public void SaveProtocolSettings()
        {
            _manager.SaveProtocolSettings( null, "qwe", false );
        }
    }
    public class MockPlugin
    {
        private string _url;
        private bool _wasSetAsDefault = false;
        public Exception _exception = new Exception();
        public ProtocolHandlerCallback Callback = null;
        public MethodInvoker MakeDefaultCallback = null;
        public ProtocolHandlerCallback CallbackWithException = null;
        public MockPlugin()
        {
            Callback = ProtocolHandler;
            MakeDefaultCallback = MakeDefault;
            CallbackWithException = ProtocolHandlerWithException;
        }
        public void MakeDefault()
        {
            _wasSetAsDefault = true;
        }
        public void ProtocolHandler( string url )
        {
            _url = url;
        }
        public void ProtocolHandlerWithException( string url )
        {
            throw _exception;
        }
        public string URL { get { return _url; } }
        public bool WasSetAsDefault { get { return _wasSetAsDefault; } }
    }
}
