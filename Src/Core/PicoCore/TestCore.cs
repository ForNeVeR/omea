// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

using System35;

using JetBrains.Omea.OpenAPI;
using NMock;
using PicoContainer.Defaults;

namespace JetBrains.Omea.PicoCore
{
	/// <summary>
	/// ICore implementation used in tests.
	/// </summary>
	public class TestCore: PicoCore, IDisposable
	{
	    private DefaultPicoContainer _basePicoContainer;
        private DefaultPicoContainer _mockPicoContainer;
        private IProtocolHandlerManager _protocolManager;

        private IAsyncProcessor _resourceAP;
        private IAsyncProcessor _networkAP;
        private IAsyncProcessor _uiAP;

        public TestCore()
            : this( typeof(TestResourceStore) )
        {
        }

        public TestCore( Type resourceStoreType )
	    {
            theInstance = this;

            _basePicoContainer = _picoContainer;
            _mockPicoContainer = new DefaultPicoContainer( _basePicoContainer );
            _picoContainer = _mockPicoContainer;

            _basePicoContainer.RegisterComponentImplementation( resourceStoreType );
            _basePicoContainer.RegisterComponentImplementation( typeof(MockResourceTabProvider) );

            _resourceAP = new MockAsyncProcessor();
            _networkAP = new MockAsyncProcessor();
            _uiAP = new MockAsyncProcessor();

            _basePicoContainer.RegisterComponentInstance( new DynamicMock( typeof(IPluginLoader) ).MockInstance );
            _basePicoContainer.RegisterComponentInstance( new DynamicMock( typeof(ITextIndexManager) ).MockInstance );
            _basePicoContainer.RegisterComponentInstance( new DynamicMock( typeof(IUIManager) ).MockInstance );
            _basePicoContainer.RegisterComponentInstance( new DynamicMock( typeof(IResourceBrowser ) ).MockInstance );
            _basePicoContainer.RegisterComponentInstance( new DynamicMock( typeof(ISidebarSwitcher) ).MockInstance );

            DynamicMock resourceIconManagerMock = new DynamicMock( typeof(IResourceIconManager) );
            resourceIconManagerMock.SetupResult( "IconColorDepth", ColorDepth.Depth8Bit );
            resourceIconManagerMock.SetupResult( "GetIconIndex", 0, typeof(IResource) );
            _basePicoContainer.RegisterComponentInstance( resourceIconManagerMock.MockInstance );

            DynamicMock actionManagerMock = new DynamicMock( typeof(IActionManager) );
            actionManagerMock.SetupResult( "GetKeyboardShortcut", "", typeof(IAction) );
            _basePicoContainer.RegisterComponentInstance( actionManagerMock.MockInstance );

            _basePicoContainer.RegisterComponentInstance( new MockTabManager() );

            DynamicMock displayColumnManagerMock = new DynamicMock( typeof(IDisplayColumnManager) );
            _basePicoContainer.RegisterComponentInstance( displayColumnManagerMock.MockInstance );

            File.Delete( ".\\MockPluginEnvironment.ini" );
            _basePicoContainer.RegisterComponentInstance( new Ini.IniFile( ".\\MockPluginEnvironment.ini" ) );
        }

	    public void Dispose()
	    {
	        _picoContainer.Dispose();
            _basePicoContainer.Dispose();
            _picoContainer = null;
            _basePicoContainer = null;
            _mockPicoContainer = null;
            theInstance = null;
	    }

        public void SetResourceAP( IAsyncProcessor asyncProcessor )
        {
            _resourceAP = asyncProcessor;
        }

        public void SetNetworkAP( IAsyncProcessor asyncProcessor )
        {
            _networkAP = asyncProcessor;
        }

        public override IProtocolHandlerManager ProtocolHandlerManager
        {
            get { return _protocolManager; }
        }

        public void SetProtocolHandlerManager( IProtocolHandlerManager protocolManager )
        {
            _protocolManager = protocolManager;
        }

	    public override IProgressWindow ProgressWindow
	    {
	        get { return null; }
	    }

	    public override IWin32Window MainWindow
	    {
	        get { throw new NotImplementedException(); }
	    }

	    public override AbstractWebBrowser WebBrowser
	    {
	        get { return null; }
	    }

        public override IAsyncProcessor ResourceAP
	    {
	        get { return _resourceAP; }
	    }

        public override IAsyncProcessor NetworkAP
	    {
	        get { return _networkAP; }
	    }

        public override IAsyncProcessor UserInterfaceAP
        {
            get { return _uiAP; }
        }

	    public override string ProductName
	    {
	        get { throw new NotImplementedException(); }
	    }
	    public override string ProductFullName
	    {
	        get { throw new NotImplementedException(); }
	    }
	    public override Version ProductVersion
	    {
	        get { throw new NotImplementedException(); }
	    }
	    public override string ProductReleaseVersion
	    {
	        get { throw new NotImplementedException(); }
	    }

	    public override SizeF ScaleFactor
	    {
	        get { return new SizeF( 1.0f, 1.0f ); }
	    }

	    public override int IdlePeriod
	    {
	        get { throw new NotImplementedException(); }
	        set { throw new NotImplementedException(); }
	    }
	    public override bool IsSystemIdle
	    {
	        get { throw new NotImplementedException(); }
	    }
	    public override CoreState State
	    {
	        get { return CoreState.Running; }
	    }
	    public override event EventHandler StateChanged;
        public Exception _reportedException;

	    public override void ReportException( Exception e, bool fatal )
	    {
            _reportedException = e;
	    }

	    public override void ReportException( Exception e, ExceptionReportFlags flags )
	    {
            _reportedException = e;
        }

	    public override void AddExceptionReportData( string data )
	    {
	        throw new NotImplementedException();
	    }

	    public override void ReportBackgroundException( Exception e )
	    {
	        throw new NotImplementedException();
	    }

	    public override void RestartApplication()
	    {
	        throw new NotImplementedException();
	    }

	    public override IWebProxy DefaultProxy
	    {
	        get { throw new NotImplementedException(); }
	    }

        public object GetComponentInstanceOfType( Type componentType )
        {
            return _picoContainer.GetComponentInstanceOfType( componentType );
        }

	    public void SetDisplayColumnManager( IDisplayColumnManager displayColumnManager )
	    {
	        _displayColumnManager = displayColumnManager;
	    }

	    public override event OmeaThreadExceptionEventHandler BackgroundThreadException;
	}

    internal class MockAsyncProcessor: IAsyncProcessor
    {
        public bool IsOwnerThread
        {
            get { return true; }
        }
        public void CancelJobs()
        {
        }
        public void CancelJobs(AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.CancelUnitsOfWork implementation
        }

        void JetBrains.Omea.OpenAPI.IAsyncProcessor.CancelJobs(Delegate method)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.CancelUnitsOfWork implementation
        }

        void JetBrains.Omea.OpenAPI.IAsyncProcessor.CancelJobs(JetBrains.Omea.OpenAPI.JobFilter filter)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.CancelUnitsOfWork implementation
        }

        public void RunJob(AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.RunUnitOfWork implementation
        }

        public bool QueueJob(JetBrains.Omea.OpenAPI.JobPriority priority, string name, Delegate method, params object[] args)
        {
            return true;// TODO:  Add MockAsyncProcessor.QueueJob implementation
        }

    	#region IAsyncProcessor Members

    	/// <summary>
    	/// Queues a named delegate for asynchronous execution with specified priority.
    	/// These jobs are never merged.
    	/// </summary>
    	/// <param name="priority">The priority of this job.</param>
    	/// <param name="name">Name of operation.</param>
    	/// <param name="action">The delegate to be executed. Arguments should be passed via a closure.</param>
    	/// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
    	public void QueueJob(JobPriority priority, string name, Action action)
    	{
    		// TODO
    	}

    	/// <summary>
    	/// Queues a named delegate for asynchronous execution with specified priority.
    	/// </summary>
    	/// <param name="priority">The priority of this job.</param>
    	/// <param name="name">Name of operation.</param>
    	/// <param name="identity">An optional identity. Jobs with equal non-<c>Null</c> identity will be merged together.</param>
    	/// <param name="action">The delegate to be executed. Arguments should be passed via a closure.</param>
    	/// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
    	/// <returns><c>True</c> if the delegate was really queued, <c>False</c> if it was merged with an equal one.</returns>
    	public bool QueueJob(JobPriority priority, string name, object identity, Action action)
    	{
    		return true; // TODO
    	}

    	bool JetBrains.Omea.OpenAPI.IAsyncProcessor.QueueJob(JetBrains.Omea.OpenAPI.JobPriority priority, Delegate method, params object[] args)
        {
            return true; // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJob implementation
        }

        bool JetBrains.Omea.OpenAPI.IAsyncProcessor.QueueJob(JetBrains.Omea.OpenAPI.JobPriority priority, AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJob implementation
            return false;
        }

        bool JetBrains.Omea.OpenAPI.IAsyncProcessor.QueueJob(string name, Delegate method, params object[] args)
        {
            return true;// TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJob implementation
        }

    	/// <summary>
    	/// Queues a named delegate for asynchronous execution with normal priority.
    	/// These jobs are never merged.
    	/// </summary>
    	/// <param name="name">Name of operation.</param>
    	/// <param name="action">The delegate to be executed. Arguments should be passed via a closure.</param>
    	/// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
    	public void QueueJob(string name, Action action)
    	{
    		// TODO
    	}

    	/// <summary>
    	/// Queues a named delegate for asynchronous execution with normal priority.
    	/// </summary>
    	/// <param name="name">Name of operation.</param>
    	/// <param name="identity">An optional identity. Jobs with equal non-<c>Null</c> identity will be merged together.</param>
    	/// <param name="action">The delegate to be executed. Arguments should be passed via a closure.</param>
    	/// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
    	/// <returns><c>True</c> if the delegate was really queued, <c>False</c> if it was merged with an equal one.</returns>
    	public bool QueueJob(string name, object identity, Action action)
    	{
    		return true; // TODO
    	}

    	bool JetBrains.Omea.OpenAPI.IAsyncProcessor.QueueJob(Delegate method, params object[] args)
        {
            return true;// TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJob implementation
        }

        bool JetBrains.Omea.OpenAPI.IAsyncProcessor.QueueJob(AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJob implementation
            return false;
        }

        public void QueueJobAt(DateTime when, Delegate method, params object[] args)
        {
            // TODO:  Add MockAsyncProcessor.QueueJobAt implementation
        }

        void JetBrains.Omea.OpenAPI.IAsyncProcessor.QueueJobAt(DateTime when, AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJobAt implementation
        }

        public void QueueIdleJob( JetBrains.Omea.OpenAPI.JobPriority priority, AbstractJob uow )
        {
            // TODO:  Add MockAsyncProcessor.QueueIdleUnitOfWork implementation
        }

        void JetBrains.Omea.OpenAPI.IAsyncProcessor.QueueIdleJob(AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueIdleUnitOfWork implementation
        }

        public void CancelTimedJobs(AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.CancelTimedUnitsOfWork implementation
        }

        void JetBrains.Omea.OpenAPI.IAsyncProcessor.CancelTimedJobs(Delegate method)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.CancelTimedUnitsOfWork implementation
        }

        void JetBrains.Omea.OpenAPI.IAsyncProcessor.CancelTimedJobs(JetBrains.Omea.OpenAPI.JobFilter filter)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.CancelTimedUnitsOfWork implementation
        }

        public void QueueJobAt( DateTime when, string name, Delegate method, params object[] args ) {}

    	/// <summary>
    	/// Queues a named delegate for execution at specified time.
    	/// </summary>
    	/// <param name="dateTime">The time when delegate should be executed.</param>
    	/// <param name="name">Name of operation.</param>
    	/// <param name="action">The delegate to be executed. Arguments should be passed via a closure.</param>
    	/// <remarks>If time has passed, job is executed immediately. Name of operation is reflected by corresponding indicator light.</remarks>
    	public void QueueJobAt(DateTime dateTime, string name, Action action)
    	{
    	}

    	public object RunJob( Delegate method, params object[] args )
        {
            return method.DynamicInvoke( args );
        }

        public object RunJob( string name, Delegate method, params object[] args )
        {
            return method.DynamicInvoke( args );
        }

    	/// <summary>
    	/// Queues a named delegate for synchronous execution and waits until it is finished.
    	/// </summary>
    	/// <param name="name">Name of operation.</param>
    	/// <param name="action">The delegate to be executed. Arguments and a return value should be passed via a closure.</param>
    	/// <remarks>Jobs to be run are queued with the immediate priority.
    	/// On attempt to run two or more equal jobs simultaneously the <c>AsyncProcessorException</c> is thrown.
    	/// Name of operation is reflected by corresponding indicator light.</remarks>
    	/// <returns>Whether the execution succeeded.</returns>
    	public bool RunJob(string name, Action action)
    	{
    		action();
			return true;
    	}

    	#endregion

    	public void RunUniqueJob( AbstractJob uow )
        {

        }

        public object RunUniqueJob( Delegate method, params object[] args )
        {
            return method.DynamicInvoke( args );
        }

        public object RunUniqueJob( string name, Delegate method, params object[] args )
        {
            return method.DynamicInvoke( args );
        }

        public string CurrentJobName { get { return "Mock"; } }

        public void Dispose()  { }
        public event EventHandler JobStarting;
        public event EventHandler JobFinished;
        public event EventHandler QueueGotEmpty;

        internal void FireJobStarting()
        {
            if ( JobStarting != null )
            {
                JobStarting( this, EventArgs.Empty );
            }
        }

        internal void FireJobFinished()
        {
            if ( JobFinished != null )
            {
                JobFinished( this, EventArgs.Empty );
            }
        }

        internal void FireQueueGotEmpty()
        {
            if ( QueueGotEmpty != null )
            {
                QueueGotEmpty( this, EventArgs.Empty );
            }
        }
    }

    internal class MockResourceTypeTab: IResourceTypeTab
    {
        public string Id { get { return ""; } }

        public string[] GetResourceTypes()
        {
            return null;
        }

        public IResourceList GetFilterList( bool live ) { return null; }
        public string Name { get { return ""; } }
        public int LinkPropId { get { return -1; } }
    }

    internal class MockTabManager: ITabManager
    {
        public void RegisterResourceTypeTab( string tabID, string tabName, string resType, int order ) { }
        public void RegisterResourceTypeTab( string tabID, string tabName, string[] resTypes, int order ) { }
        public void RegisterResourceTypeTab( string tabID, string tabName, string[] resTypes, int linkPropID,
            int order ) { }

        public void SetDefaultSelectedResource( string tabName, IResource res ) { }

        public void SelectResourceTypeTab( string resType ) { }
        public void SelectLinkPropTab( int linkPropId ) { }
        public string GetTabName( string tabID ) { return null; }

        public string CurrentTabId { get { return null; } set { } }

        public string FindResourceTypeTab( string resourceType ) { return null; }
        public string FindLinkPropTab( int linkPropId ) { return null; }

        public IResourceTypeTabCollection Tabs { get { return null; } }
        public IResourceTypeTab CurrentTab { get { return new MockResourceTypeTab(); } }

        public string GetResourceTab( IResource res ) { return null; }

        public bool ActivateTab( string tabId ) { return false; }

        public event EventHandler TabChanged;
    }
}
