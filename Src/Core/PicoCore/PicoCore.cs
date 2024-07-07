// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using JetBrains.Omea.Categories;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.MailParser;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.TextIndex;
using PicoContainer.Defaults;

namespace JetBrains.Omea.PicoCore
{
	/// <summary>
	/// Base implementation of ICore based on PicoContainer.
	/// </summary>
    public abstract class PicoCore: ICore
	{
        internal DefaultPicoContainer _picoContainer;

        private IResourceStore _resourceStore;
	    private IWorkspaceManager _workspaceManager;
	    private ICategoryManager _categoryManager;
	    private IResourceTreeManager _resourceTreeManager;
	    private IUnreadManager _unreadManager;
	    private IPluginLoader _pluginLoader;
	    private ISettingStore _settingStore;
	    private ITextIndexManager _textIndexManager;
	    private IResourceIconManager _resourceIconManager;
	    private IUIManager _uiManager;
	    private IContactManager _contactManager;
	    private IActionManager _actionManager;
	    private IResourceBrowser _resourceBrowser;
	    protected IDisplayColumnManager _displayColumnManager;
	    private ITabManager _tabManager;
	    private ISidebarSwitcher _sidebarSwitcher;
	    private ISidebar _rightSidebar;
	    private IFileResourceManager _fileResourceManager;
	    private INotificationManager _notificationManager;
	    private IMessageFormatter _messageFormatter;
	    private IRemoteControlManager _remoteControlManager;
	    private IFilterRegistry         _filterRegistry;
	    private IFilterEngine           _filterEngine;
	    private ITrayIconManager        _trayIconManager;
	    private IFormattingRuleManager _formattingRuleManager;
	    private IExpirationRuleManager _expirationRuleManager;
	    private IFilteringFormsManager _filteringFormsManager;
        private ISearchQueryExtensions _queryExtensions;
	    private ICoreProps _coreProps;
	    private ICorePropIds _corePropIds;

	    protected PicoCore()
        {
            _picoContainer = new DefaultPicoContainer();
            _picoContainer.RegisterComponentImplementation( typeof(CategoryManager) );
            _picoContainer.RegisterComponentImplementation( typeof(ResourceTreeManager) );
            _picoContainer.RegisterComponentImplementation( typeof(WorkspaceManager) );
            _picoContainer.RegisterComponentImplementation( typeof(UnreadManager) );
            _picoContainer.RegisterComponentImplementation( typeof(FilterRegistry) );
            _picoContainer.RegisterComponentImplementation( typeof(FormattingRuleManager) );
            _picoContainer.RegisterComponentImplementation( typeof(ExpirationRuleManager) );
            _picoContainer.RegisterComponentImplementation( typeof(FilteringFormsManager) );
            _picoContainer.RegisterComponentImplementation( typeof(FilterEngine) );
            _picoContainer.RegisterComponentImplementation( typeof(ContactManager) );
            _picoContainer.RegisterComponentImplementation( typeof(FileResourceManager) );
            _picoContainer.RegisterComponentImplementation( typeof(NotificationManager) );
            _picoContainer.RegisterComponentImplementation( typeof(MessageFormatter) );
            _picoContainer.RegisterComponentImplementation( typeof(CoreProps) );
            _picoContainer.RegisterComponentImplementation( typeof(CorePropIds) );
            _picoContainer.RegisterComponentImplementation( typeof(FavIconManager) );
            _picoContainer.RegisterComponentImplementation( typeof(SearchQueryExtensions) );
        }

	    public override IResourceStore ResourceStore
	    {
	        [DebuggerStepThrough]
            get
	        {
	            if ( _resourceStore == null )
	            {
	                _resourceStore = (IResourceStore) _picoContainer.GetComponentInstanceOfType( typeof(IResourceStore) );
	            }
                return _resourceStore;
	        }
	    }

	    public override IWorkspaceManager WorkspaceManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _workspaceManager == null )
                {
                    _workspaceManager = (IWorkspaceManager) _picoContainer.GetComponentInstanceOfType( typeof(IWorkspaceManager) );
                }
                return _workspaceManager;
            }
        }

        public override ICategoryManager CategoryManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _categoryManager == null )
                {
                    _categoryManager = (ICategoryManager) _picoContainer.GetComponentInstanceOfType( typeof(ICategoryManager) );
                }
                return _categoryManager;
            }
        }

        public override IResourceTreeManager ResourceTreeManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _resourceTreeManager == null )
                {
                    _resourceTreeManager = (IResourceTreeManager) _picoContainer.GetComponentInstanceOfType( typeof(IResourceTreeManager) );
                }
                return _resourceTreeManager;
            }
        }

        public override IUnreadManager UnreadManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _unreadManager == null )
                {
                    _unreadManager = (IUnreadManager) _picoContainer.GetComponentInstanceOfType( typeof(IUnreadManager) );
                }
                return _unreadManager;
            }
        }

        public override IPluginLoader PluginLoader
        {
            [DebuggerStepThrough]
            get
            {
                if ( _pluginLoader == null )
                {
                    _pluginLoader = (IPluginLoader) _picoContainer.GetComponentInstanceOfType( typeof(IPluginLoader) );
                }
                return _pluginLoader;
            }
        }

        public override ISettingStore SettingStore
        {
            [DebuggerStepThrough]
            get
            {
                if ( _settingStore == null )
                {
                    _settingStore = (ISettingStore) _picoContainer.GetComponentInstanceOfType( typeof(ISettingStore) );
                }
                return _settingStore;
            }
        }

        public override ITextIndexManager TextIndexManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _textIndexManager == null )
                {
                    _textIndexManager = (ITextIndexManager) _picoContainer.GetComponentInstanceOfType( typeof(ITextIndexManager) );
                }
                return _textIndexManager;
            }
        }

        public override IResourceIconManager ResourceIconManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _resourceIconManager == null )
                {
                    _resourceIconManager = (IResourceIconManager) _picoContainer.GetComponentInstanceOfType( typeof(IResourceIconManager) );
                }
                return _resourceIconManager;
            }
        }

        public override IUIManager UIManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _uiManager == null )
                {
                    _uiManager = (IUIManager) _picoContainer.GetComponentInstanceOfType( typeof(IUIManager) );
                }
                return _uiManager;
            }
        }

        public override IContactManager ContactManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _contactManager == null )
                {
                    _contactManager = (IContactManager) _picoContainer.GetComponentInstanceOfType( typeof(IContactManager) );
                }
                return _contactManager;
            }
        }

        public override IActionManager ActionManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _actionManager == null )
                {
                    _actionManager = (IActionManager) _picoContainer.GetComponentInstanceOfType( typeof(IActionManager) );
                }
                return _actionManager;
            }
        }

        public override IResourceBrowser ResourceBrowser
        {
            [DebuggerStepThrough]
            get
            {
                if ( _resourceBrowser == null )
                {
                    _resourceBrowser = (IResourceBrowser) _picoContainer.GetComponentInstanceOfType( typeof(IResourceBrowser) );
                }
                return _resourceBrowser;
            }
        }

        public override IDisplayColumnManager DisplayColumnManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _displayColumnManager == null )
                {
                    _displayColumnManager = (IDisplayColumnManager) _picoContainer.GetComponentInstanceOfType( typeof(IDisplayColumnManager) );
                }
                return _displayColumnManager;
            }
        }

        public override ITabManager TabManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _tabManager == null )
                {
                    _tabManager = (ITabManager) _picoContainer.GetComponentInstanceOfType( typeof(ITabManager) );
                }
                return _tabManager;
            }
        }

        public override ISidebarSwitcher LeftSidebar
        {
            [DebuggerStepThrough]
            get
            {
                if ( _sidebarSwitcher == null )
                {
                    _sidebarSwitcher = (ISidebarSwitcher) _picoContainer.GetComponentInstanceOfType( typeof(ISidebarSwitcher) );
                }
                return _sidebarSwitcher;
            }
        }

        public override ISidebar RightSidebar
        {
            [DebuggerStepThrough]
            get
            {
                if ( _rightSidebar == null )
                {
                    _rightSidebar = (ISidebar) _picoContainer.GetComponentInstanceOfType( typeof(ISidebar) );
                }
                return _rightSidebar;
            }
        }

        public override IFileResourceManager FileResourceManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _fileResourceManager == null )
                {
                    _fileResourceManager = (IFileResourceManager) _picoContainer.GetComponentInstanceOfType( typeof(IFileResourceManager) );
                }
                return _fileResourceManager;
            }
        }

        public override INotificationManager NotificationManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _notificationManager == null )
                {
                    _notificationManager = (INotificationManager) _picoContainer.GetComponentInstanceOfType( typeof(INotificationManager) );
                }
                return _notificationManager;
            }
        }

        public override IMessageFormatter MessageFormatter
        {
            [DebuggerStepThrough]
            get
            {
                if ( _messageFormatter == null )
                {
                    _messageFormatter = (IMessageFormatter) _picoContainer.GetComponentInstanceOfType( typeof(IMessageFormatter) );
                }
                return _messageFormatter;
            }
        }

        public override IRemoteControlManager RemoteControllerManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _remoteControlManager == null )
                {
                    _remoteControlManager = (IRemoteControlManager) _picoContainer.GetComponentInstanceOfType( typeof(IRemoteControlManager) );
                }
                return _remoteControlManager;
            }
        }

        public override IFilterRegistry FilterRegistry
        {
            [DebuggerStepThrough]
            get
            {
                if ( _filterRegistry == null )
                {
                    _filterRegistry = (IFilterRegistry) _picoContainer.GetComponentInstanceOfType( typeof(IFilterRegistry) );
                }
                return _filterRegistry;
            }
        }

        public override ITrayIconManager TrayIconManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _trayIconManager == null )
                {
                    _trayIconManager = (ITrayIconManager) _picoContainer.GetComponentInstanceOfType( typeof(ITrayIconManager) );
                }
                return _trayIconManager;
            }
        }

        public override IFormattingRuleManager FormattingRuleManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _formattingRuleManager == null )
                {
                    _formattingRuleManager = (IFormattingRuleManager) _picoContainer.GetComponentInstanceOfType( typeof(IFormattingRuleManager) );
                }
                return _formattingRuleManager;
            }
        }

        public override IExpirationRuleManager ExpirationRuleManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _expirationRuleManager == null )
                {
                    _expirationRuleManager = (IExpirationRuleManager) _picoContainer.GetComponentInstanceOfType( typeof(IExpirationRuleManager) );
                }
                return _expirationRuleManager;
            }
        }

        public override IFilteringFormsManager FilteringFormsManager
        {
            [DebuggerStepThrough]
            get
            {
                if ( _filteringFormsManager == null )
                {
                    _filteringFormsManager = (IFilteringFormsManager) _picoContainer.GetComponentInstanceOfType( typeof(IFilteringFormsManager) );
                }
                return _filteringFormsManager;
            }
        }

        public override IFilterEngine FilterEngine
        {
            [DebuggerStepThrough]
            get
            {
                if ( _filterEngine == null )
                {
                    _filterEngine = (IFilterEngine) _picoContainer.GetComponentInstanceOfType( typeof(IFilterEngine) );
                }
                return _filterEngine;
            }
        }

        public override ISearchQueryExtensions SearchQueryExtensions
        {
            [DebuggerStepThrough]
            get
            {
                if ( _queryExtensions == null )
                {
                    _queryExtensions = (ISearchQueryExtensions) _picoContainer.GetComponentInstanceOfType( typeof(ISearchQueryExtensions) );
                }
                return _queryExtensions;
            }
        }

        public override ICoreProps Props
        {
            [DebuggerStepThrough]
            get
            {
                if ( _coreProps == null )
                {
                    _coreProps = (ICoreProps) _picoContainer.GetComponentInstanceOfType( typeof(ICoreProps) );
                }
                return _coreProps;
            }
        }

	    public override ICorePropIds PropIds
	    {
            [DebuggerStepThrough]
            get
            {
                if (_corePropIds == null)
                {
                    _corePropIds = (ICorePropIds)_picoContainer.GetComponentInstanceOfType(typeof(ICorePropIds));
                }
                return _corePropIds;
            }
        }

	    protected void RegisterComponentInstance( object impl )
        {
            _picoContainer.RegisterComponentInstance( impl );
        }

        protected void RegisterComponentImplementation( Type aType )
        {
            _picoContainer.RegisterComponentImplementation( aType );
        }

	    public override object GetComponentImplementation( Type componentType )
	    {
	        return _picoContainer.GetComponentInstanceOfType( componentType );
	    }
	}
}
