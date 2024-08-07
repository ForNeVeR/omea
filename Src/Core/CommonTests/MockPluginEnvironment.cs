﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

using JetBrains.Omea.OpenAPI;
using System.IO;
using NMock;
using PicoContainer.Defaults;

namespace CommonTests
{
    //---------------------------------------------------------------------

    public class MockResourceTypeTab: IResourceTypeTab
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

    public class MockTabManager: ITabManager
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

    public class MockAsyncProcessor: IAsyncProcessor
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

        void IAsyncProcessor.CancelJobs(Delegate method)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.CancelUnitsOfWork implementation
        }

        void IAsyncProcessor.CancelJobs(JobFilter filter)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.CancelUnitsOfWork implementation
        }

        public void RunJob(AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.RunUnitOfWork implementation
        }

        public bool QueueJob(JobPriority priority, string name, Delegate method, params object[] args)
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
    		return; // TODO
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

    	bool IAsyncProcessor.QueueJob(JobPriority priority, Delegate method, params object[] args)
        {
           return true; // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJob implementation
        }

        bool IAsyncProcessor.QueueJob(JobPriority priority, AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJob implementation
            return false;
        }

        bool IAsyncProcessor.QueueJob(string name, Delegate method, params object[] args)
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
    		return; // TODO
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

    	bool IAsyncProcessor.QueueJob(Delegate method, params object[] args)
        {
            return true;// TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJob implementation
        }

        bool IAsyncProcessor.QueueJob(AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJob implementation
            return false;
        }

        public void QueueJobAt(DateTime when, Delegate method, params object[] args)
        {
            // TODO:  Add MockAsyncProcessor.QueueJobAt implementation
        }

        void IAsyncProcessor.QueueJobAt(DateTime when, AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueJobAt implementation
        }

        public void QueueIdleJob( JobPriority priority, AbstractJob uow )
        {
            // TODO:  Add MockAsyncProcessor.QueueIdleUnitOfWork implementation
        }

        void IAsyncProcessor.QueueIdleJob(AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.QueueIdleUnitOfWork implementation
        }

        public void CancelTimedJobs(AbstractJob uow)
        {
            // TODO:  Add MockAsyncProcessor.CancelTimedUnitsOfWork implementation
        }

        void IAsyncProcessor.CancelTimedJobs(Delegate method)
        {
            // TODO:  Add MockAsyncProcessor.OmniaMea.OpenAPI.IAsyncProcessor.CancelTimedUnitsOfWork implementation
        }

        void IAsyncProcessor.CancelTimedJobs(JobFilter filter)
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

    public class MockCoreProps : ICoreProps
    {
        public int Date             { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int Size             { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int Subject          { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int LongBody         { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int LongBodyIsHTML   { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int LongBodyIsRTF    { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int Parent           { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int Reply            { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int IsDeleted        { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int Order            { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int ShowDeletedItems { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int ShowTotalCount   { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int IsUnread         { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int DisplayUnread    { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int DisplayThreaded  { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int DisplayNewspaper { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int Open             { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int Annotation       { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int ContentType      { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int ResourceVisibleOrder { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int UserResourceOrder { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int LastError        { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int DeleteDate       { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int PreviewText      { get { throw new NotImplementedException( "PropertyRequest is not implemented"); } }
        public int Name
        {
            get
            {
                if( _name == -1 )
                    _name = Core.ResourceStore.PropTypes.Register( "Name", PropDataType.String, PropTypeFlags.Normal );
                return _name;
            }
        }
        public int NeedPreview
        {
            get
            {
                if( _needPreview == -1 )
                    _needPreview= Core.ResourceStore.PropTypes.Register( "NeedPreview", PropDataType.Bool, PropTypeFlags.Internal | PropTypeFlags.NoSerialize );
                return _needPreview;
            }
        }

        private int _needPreview = -1;
        private int _name = -1;
    }

    public class MockPluginEnvironment : ICore, IDisposable
    {
        readonly IResourceStore Storage;
        readonly ISettingStore _settingStore;
        readonly IActionManager _actionManager;
        readonly IUIManager _uiManager;
        readonly IPluginLoader _pluginLoader;
        readonly IResourceBrowser _resourceBrowser;
        readonly ITabManager _tabManager;
        readonly MockAsyncProcessor _resourceAP;
        readonly IAsyncProcessor _networkAP;
        readonly IAsyncProcessor _uiAP;

        readonly IResourceIconManager _resourceIconManager;
        readonly INotificationManager _notificationManager;
        readonly ITextIndexManager    _textIndexManager;
        IContactManager      _contactManager;
        readonly IMessageFormatter    _messageFormatter;
        ISidebarSwitcher     _leftSidebar;
        readonly IDisplayColumnManager _displayColumnManager;
        IFilterRegistry        _filterRegistry;
		IRemoteControlManager _rcManager;
        readonly ITrayIconManager      _trayIconManager;
        readonly IFormattingRuleManager _formattingRuleManager;
        readonly IExpirationRuleManager _expirationRuleManager;
        readonly IFilteringFormsManager _filteringFormsManager;
        readonly IFilterEngine          _filterEngine;
        readonly ISearchQueryExtensions _searchQueryExtensions;
        DefaultPicoContainer  _mockPicoContainer;
        readonly DefaultPicoContainer  _picoContainer;
        ICoreProps           _coreProps;
        ICorePropIds           _corePropIds;

        public MockPluginEnvironment( IResourceStore storage )
        {
            _picoContainer = new DefaultPicoContainer();
            _mockPicoContainer = new DefaultPicoContainer( _picoContainer );

            Storage = storage;
            if ( storage != null )
            {
                _picoContainer.RegisterComponentInstance( storage );
            }
            File.Delete( ".\\MockPluginEnvironment.ini" );
            _settingStore = new Ini.IniFile( ".\\MockPluginEnvironment.ini" );

            DynamicMock actionManagerMock = new DynamicMock( typeof(IActionManager) );
            actionManagerMock.SetupResult( "GetKeyboardShortcut", "", typeof(IAction) );
            _actionManager = (IActionManager) actionManagerMock.MockInstance;

            _uiManager = (IUIManager) new DynamicMock( typeof(IUIManager) ).MockInstance;
            _pluginLoader = (IPluginLoader) new DynamicMock( typeof(IPluginLoader) ).MockInstance;
            _resourceBrowser = (IResourceBrowser) new DynamicMock( typeof(IResourceBrowser ) ).MockInstance;
            _tabManager = new MockTabManager();
            _resourceAP = new MockAsyncProcessor();
            _networkAP = new MockAsyncProcessor();
            _uiAP = new MockAsyncProcessor();

            DynamicMock resourceIconManagerMock = new DynamicMock( typeof(IResourceIconManager) );
            resourceIconManagerMock.SetupResult( "IconColorDepth", ColorDepth.Depth8Bit );
            resourceIconManagerMock.SetupResult( "GetIconIndex", 0, typeof(IResource) );
            _resourceIconManager = (IResourceIconManager) resourceIconManagerMock.MockInstance;

            _notificationManager = (INotificationManager) new DynamicMock( typeof(INotificationManager) ).MockInstance;
            _textIndexManager = (ITextIndexManager) new DynamicMock( typeof(ITextIndexManager) ).MockInstance;
            _messageFormatter = (IMessageFormatter) new DynamicMock( typeof(IMessageFormatter ) ).MockInstance;
            _displayColumnManager = (IDisplayColumnManager) new DynamicMock( typeof(IDisplayColumnManager) ).MockInstance;

//            DynamicMock filterManagerMock = new DynamicMock( typeof(IFilterRegistry) );
            DynamicMock filterEngineMock = new DynamicMock( typeof(IFilterEngine) );
            filterEngineMock.SetupResult( "ExecRules", true, typeof(string), typeof(IResource) );

            DynamicMock filterManagerMock = new DynamicMock( typeof(IFilterRegistry) );
            _filterRegistry = (IFilterRegistry) filterManagerMock.MockInstance;

			_rcManager = (IRemoteControlManager) new DynamicMock( typeof(IRemoteControlManager) ).MockInstance;
            _trayIconManager = (ITrayIconManager) new DynamicMock( typeof(ITrayIconManager) ).MockInstance;
            _formattingRuleManager = (IFormattingRuleManager) new DynamicMock( typeof(IFormattingRuleManager) ).MockInstance;
            _expirationRuleManager = (IExpirationRuleManager) new DynamicMock( typeof(IExpirationRuleManager) ).MockInstance;
            _filteringFormsManager = (IFilteringFormsManager) new DynamicMock( typeof(IFilteringFormsManager) ).MockInstance;
            _searchQueryExtensions = (ISearchQueryExtensions) new DynamicMock( typeof(ISearchQueryExtensions) ).MockInstance;
            _filterEngine = (IFilterEngine) new DynamicMock( typeof(IFilterEngine) ).MockInstance;

            theInstance = this;
        }

        public void SetContactManager( IContactManager mgr )
        {
            _contactManager = mgr;
        }

        public void SetFilterManager( IFilterRegistry mgr )
        {
            _filterRegistry = mgr;
        }

        public void SetCoreProps( ICoreProps props )
        {
            _coreProps = props;
        }

        public void FireResourceOperationStarting()
        {
            _resourceAP.FireJobStarting();
        }

        public void FireResourceOperationFinished()
        {
            _resourceAP.FireJobFinished();
        }
        public override IProtocolHandlerManager ProtocolHandlerManager
        {
            get { return null; }
        }

        public void FireResourceQueueGotEmpty()
        {
            _resourceAP.FireQueueGotEmpty();
        }

        public override string                  ProductName         { get { return "MockPluginEnvironment"; } }
        public override string                  ProductFullName     { get { return "MockPluginEnvironment"; } }
        public override Version                  ProductVersion        { get { return new Version(); } }
        public override string                  ProductReleaseVersion { get { return null; } }
        public override IResourceStore          ResourceStore       { get{ return Storage; } }
        public override IProgressWindow         ProgressWindow      { get{ return null; } }
        public override IActionManager          ActionManager       { get{ return _actionManager; } }
        public override IUIManager              UIManager           { get { return _uiManager; } }
        public override IResourceBrowser        ResourceBrowser     { get{ return _resourceBrowser; } }
        public override ITabManager             TabManager          { get { return _tabManager; } }
        public override ITrayIconManager        TrayIconManager     { get { return _trayIconManager; } }
        public override IFormattingRuleManager  FormattingRuleManager{ get { return _formattingRuleManager; } }
        public override IExpirationRuleManager  ExpirationRuleManager{ get { return _expirationRuleManager; } }
        public override IFilteringFormsManager  FilteringFormsManager{ get { return _filteringFormsManager; } }
        public override ISearchQueryExtensions  SearchQueryExtensions{ get { return _searchQueryExtensions; } }

        public override ISidebarSwitcher    LeftSidebar
        {
            get
            {
                if ( _leftSidebar == null )
                {
                    _leftSidebar = (ISidebarSwitcher) new DynamicMock( typeof(ISidebarSwitcher) ).MockInstance;
                }
                return _leftSidebar;
            }
        }

        public override ISidebar            RightSidebar    { get { return null; } }
        public override ISettingStore       SettingStore    { get{ return _settingStore; } }
        public override IPluginLoader       PluginLoader    { get{ return _pluginLoader; } }
        public override ITextIndexManager   TextIndexManager{ get{ return _textIndexManager; } }
        public override IFilterRegistry     FilterRegistry  { get{ return _filterRegistry; } }
        public override IFilterEngine       FilterEngine    { get{ return _filterEngine; } }
        public override AbstractWebBrowser   WebBrowser     { get{ return null; } }
        public override IWin32Window        MainWindow      { get{ return null; } }

        public override IUnreadManager      UnreadManager
        {
            get{ return (IUnreadManager) _picoContainer.GetComponentInstanceOfType( typeof(IUnreadManager) ); }
        }

        public override SizeF               ScaleFactor     { get { return new SizeF( 1.0f, 1.0f ); } }
        public override int                 IdlePeriod { get { return 0; } set { } }
        public override bool                IsSystemIdle { get { return false; } }
        public override IAsyncProcessor     ResourceAP      { get { return _resourceAP; } }
        public override IAsyncProcessor     NetworkAP       { get { return _networkAP; } }
        public override IAsyncProcessor     UserInterfaceAP       { get { return _uiAP; } }
        public override IWorkspaceManager   WorkspaceManager
        {
            get { return (IWorkspaceManager) _picoContainer.GetComponentInstanceOfType( typeof(IWorkspaceManager) ); }
        }

        public override ICategoryManager    CategoryManager
        {
            get { return (ICategoryManager) _picoContainer.GetComponentInstanceOfType( typeof(ICategoryManager) ); }
        }

        public override IResourceTreeManager ResourceTreeManager
        {
            get { return (IResourceTreeManager) _picoContainer.GetComponentInstanceOfType( typeof(IResourceTreeManager) ); }
        }

        public override IDisplayColumnManager DisplayColumnManager { get { return _displayColumnManager; } }
        public override IResourceIconManager ResourceIconManager { get { return _resourceIconManager; } }
        public override INotificationManager NotificationManager { get { return _notificationManager; } }
        public override IContactManager      ContactManager      { get { return _contactManager; } }

        public override IFileResourceManager FileResourceManager
        {
            get { return (IFileResourceManager) _picoContainer.GetComponentInstanceOfType( typeof(IFileResourceManager) ); }
        }

        public override IMessageFormatter    MessageFormatter    { get { return _messageFormatter; } }
        public override ICoreProps Props { get { return _coreProps; } }


        public override ICorePropIds PropIds
        {
            get { return _corePropIds; }
        }

        public override CoreState State { get { return CoreState.Running; } }
        public override event EventHandler StateChanged;

        public override void ReportException( Exception e, bool fatal )
        {
            throw new Exception( e.Message, e );
        }
        public override void ReportException( Exception e, ExceptionReportFlags flags )
        {
            throw new Exception( e.Message, e );
        }
        public override void ReportBackgroundException( Exception e )
        {
            throw new Exception( e.Message, e );
        }

        public override void RestartApplication()
        {
            throw new InvalidOperationException( "Can't restart mock application");
        }

        public override void AddExceptionReportData( string data ) {}

        public override IWebProxy DefaultProxy
        {
            get { return GlobalProxySelection.Select; }
        }
		public override IRemoteControlManager RemoteControllerManager
		{
			get { return _rcManager; }
		}

		internal void SetRCManager( IRemoteControlManager rcManager )
		{
			_rcManager = rcManager;
		}

        public void RegisterComponentImplementation( Type componentImplementation )
        {
            _picoContainer.RegisterComponentImplementation( componentImplementation );
        }

        public void RegisterComponentInstance( object componentInstance )
        {
            _picoContainer.RegisterComponentInstance( componentInstance );
        }

        public override object GetComponentImplementation( Type componentType )
        {
            return _picoContainer.GetComponentInstanceOfType( componentType );
        }

        public void Dispose()
        {
            _picoContainer.Dispose();
        }

        public override event OmeaThreadExceptionEventHandler BackgroundThreadException;
    }
}
