/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Current state of the Omea application.
    /// </summary>
    public enum CoreState
    {
        /// <summary>
        /// <para>The application is performing startup initialization.</para>
        /// <para>This is the first state of Omea application when it starts. Changes to <see cref="StartingPlugins"/>.</para>
        /// </summary>
        Initializing, 
        
        /// <summary>
        /// <para>The plugins are being started at application startup.</para>
        /// <para>Occurs after <see cref="Initializing"/> and before <see cref="Running"/>.</para>
        /// </summary>
        StartingPlugins, 
        
        /// <summary>
        /// <para>The application is running.</para>
        /// <para>When the application is started, it falls into the <see cref="Running"/> state after the startup initialization (<see cref="Initializing"/> and <see cref="StartingPlugins"/> <see cref="CoreState">states</see>). This is the state which Omea has most of time, between startup and shutdown. When shutdown is initiated, the state changes to <see cref="ShuttingDown"/>.</para>
        /// </summary>
        Running, 
        
        /// <summary>
        /// <para>The application and all of the plugins are being shut down.</para>
        /// <para>Occurs after <see cref="Running"/>. Do not initiate lengthy 
        /// actions not related to shutdown tasks when Omea application has this state.</para>
        /// </summary>
        ShuttingDown
    };

    /// <summary><seealso cref="ICore.ReportException"/><seealso cref="ICore.ReportBackgroundException"/>
    /// Defines the flags affecting exception reporting. You may combine more than one value.
    /// </summary>
    [Flags]
    public enum ExceptionReportFlags
    {
        /// <summary>
        /// The exception being submitted is fatal.
        /// </summary>
        Fatal = 1,

        /// <summary>
        /// The log file should be attached when the exception is submitted.
        /// </summary>
        AttachLog = 2
    };

    /// <summary>
    /// Provides data for events related to exceptions happening in threads not managed by
    /// <see cref="IAsyncProcessor"/> instances.
    /// </summary>
    /// <since>2.0</since>
    public class OmeaThreadExceptionEventArgs: EventArgs
    {
        private Exception _exception;
        private bool _handled = false;

        public OmeaThreadExceptionEventArgs( Exception exception )
        {
            _exception = exception;
        }

        /// <summary>
        /// Gets the exception which occurred in a background thread.
        /// </summary>
        public Exception Exception
        {
            get { return _exception; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether the exception was handled by the
        /// plugin or it needs to be passed to other plugins.
        /// </summary>
        public bool Handled
        {
            get { return _handled; }
            set { _handled = value; }
        }
    }

    /// <summary>
    /// Allows a plugin to handle exceptions happening in threads not managed by
    /// <see cref="IAsyncProcessor"/> instances.
    /// </summary>
    /// <since>2.0</since>
    public delegate void OmeaThreadExceptionEventHandler( object sender, OmeaThreadExceptionEventArgs e );

    /// <summary>
    /// The static class that provides access to all core service interfaces.
    /// </summary>
    /// <remarks>Plugin implementors interact with Omea through interfaces which they get from this static class.</remarks>
    public static class Core
    {
    	/// <summary><seealso cref="ResourceProxy"/>
        /// The main interface for creating and accessing resources.
        /// </summary>
        /// <remarks>At most times when Omea is running (except for the time when the
        /// <see cref="IPlugin.Register"/> method is called), only one thread is designated
        /// as the resource store write thread, and all operations that modify the resource store
        /// (creating resources, changing resource properties, deleting resources) must be executed
        /// in that thread. The <see cref="ResourceProxy"/> class provides an easy way to run a resource write
        /// operation synchronously or asynchronously.</remarks>
        public static IResourceStore        ResourceStore    { [DebuggerStepThrough] get { return ICore.Instance.ResourceStore; } }

        /// <summary>
        /// The <see cref="IProgressWindow">interface</see> controls the progress window 
        /// which is displayed at program startup and when <see cref="IUIManager.RunWithProgressWindow"/> is used.
        /// </summary>
        public static IProgressWindow       ProgressWindow   { [DebuggerStepThrough] get { return ICore.Instance.ProgressWindow; } }
        
        /// <summary>
        /// The <see cref="IActionManager">Action Manager</see> allows to register actions — 
        /// functions which can be invoked by the user through different user interface controls 
        /// (menu items, toolbar buttons, keyboard and so on).
        /// </summary>
        public static IActionManager        ActionManager
        {
            [DebuggerStepThrough] 
            get
            {
                if ( ICore.Instance == null )
                {
                    return null;
                }
                return ICore.Instance.ActionManager;
            }
        }

        /// <summary>
        /// The <see cref="IUIManager">User Interface manager</see> provides access 
        /// to UI elements and their settings.
        /// </summary>
        public static IUIManager            UIManager        { [DebuggerStepThrough] get { return ICore.Instance.UIManager; } }
        
        /// <summary><seealso cref="IResourceTypeTab"/>
        /// The <see cref="ITabManager">Tab Manager</see> that allows to register resource type tabs 
        /// and to get information about the registered resource type tabs.
        /// </summary>
        public static ITabManager           TabManager           { [DebuggerStepThrough] get { return ICore.Instance.TabManager; } }
        
        /// <summary><seealso cref="IResource"/><seealso cref="IResourceList"/><seealso cref="ColumnDescriptor"/>
        /// The <see cref="IResourceBrowser">Resource Browser</see> manages the display of 
        /// resources and resource lists in Omea.
        /// </summary>
        public static IResourceBrowser      ResourceBrowser      { [DebuggerStepThrough] get { return ICore.Instance.ResourceBrowser; } }

        /// <summary>
        /// The <see cref="ISidebarSwitcher">sidebar</see> at the left side of Omea main window. 
        /// Supports showing different sidebars depending on the active tab.
        /// </summary>
        public static ISidebarSwitcher      LeftSidebar          { [DebuggerStepThrough] get { return ICore.Instance.LeftSidebar; } }
        
        /// <summary>
        /// The <see cref="ISidebar">sidebar</see> at the right side of Omea main window.
        /// </summary>
        public static ISidebar              RightSidebar         { [DebuggerStepThrough] get { return ICore.Instance.RightSidebar; } }

        /// <summary>
        /// The <see cref="ISettingStore">Setting Store</see> is a facility for storing 
        /// the settings of Omea application and plugins.
        /// </summary>
        /// <remarks>You should use the Setting Store rather than implement your own settings-storing facilities.</remarks>
        public static ISettingStore         SettingStore         { [DebuggerStepThrough] get { return ICore.Instance.SettingStore; } }
        
        /// <summary>
        /// The <see cref="IPluginLoader">Plugin Loader</see> manages the registration of 
        /// interfaces for handling resources of specific types provided by plugins.
        /// </summary>
        public static IPluginLoader         PluginLoader         { [DebuggerStepThrough] get { return ICore.Instance.PluginLoader; } }

        /// <summary>
        /// The <see cref="ITextIndexManager">Text Index Manager</see> provides services
        /// for indexing documents and executing text index queries.
        /// </summary>
        public static ITextIndexManager     TextIndexManager     { [DebuggerStepThrough] get { return ICore.Instance.TextIndexManager; } }
        
        /// <summary>
        /// The <see cref="IFilterManager">Filter Manager</see> provides interfaces for
        /// working with custom views, rules, conditions and rule actions.
        /// </summary>
        public static IFilterManager        FilterManager        { [DebuggerStepThrough] get { return ICore.Instance.FilterManager; } }

        /// <summary>
        /// The <see cref="ITrayIconManager">Tray Icon Manager</see> provides interfaces
        /// for working with tray icon rules.
        /// </summary>
        /// <since>2.0</since>
        public static ITrayIconManager      TrayIconManager      { [DebuggerStepThrough] get { return ICore.Instance.TrayIconManager; } }

        /// <summary>
        /// The <see cref="IFormattingRuleManager">Formatting Rule Manager</see> provides interfaces
        /// for working with formatting rules.
        /// </summary>
        /// <since>2.0</since>
        public static IFormattingRuleManager FormattingRuleManager { [DebuggerStepThrough] get { return ICore.Instance.FormattingRuleManager; } }

        /// <summary>
        /// The <see cref="IExpirationRuleManager">Expiration Rule Manager</see> provides interfaces
        /// for working with expiration rules.
        /// </summary>
        /// <since>2.0</since>
        public static IExpirationRuleManager ExpirationRuleManager { [DebuggerStepThrough] get { return ICore.Instance.ExpirationRuleManager; } }

        /// <summary>
        /// The <see cref="IFilteringFormsManager">Filtering Forms Manager</see> allows plugins
        /// to open standard dialogs for advanced search and creating/editing views and rules of different types.
        /// </summary>
        /// <since>2.0</since>
        public static IFilteringFormsManager FilteringFormsManager { [DebuggerStepThrough] get { return ICore.Instance.FilteringFormsManager; } }

        public static ISearchQueryExtensions SearchQueryExtensions { [DebuggerStepThrough] get { return ICore.Instance.SearchQueryExtensions; } }

        /// <summary>
        /// The <see cref="IUnreadManager">Unread Manager</see> allows to retrieve
        /// unread counts for views and other resources.
        /// </summary>
        public static IUnreadManager        UnreadManager        { [DebuggerStepThrough] get { return ICore.Instance.UnreadManager; } }

        /// <summary><seealso cref="IWin32Window"/>
        /// Handle to the main window.
        /// </summary>
        public static IWin32Window          MainWindow           { [DebuggerStepThrough] get { return ICore.Instance.MainWindow; } }

        /// <summary>
        /// The Web browser embedded in Omea.
        /// </summary>
        public static AbstractWebBrowser    WebBrowser           { [DebuggerStepThrough] get { return ICore.Instance.WebBrowser; } }
        
        /// <summary>
        /// The <see cref="IAsyncProcessor">async processor</see> for executing operations
        /// in the resource thread.
        /// </summary>
        public static IAsyncProcessor       ResourceAP           { [DebuggerStepThrough] get { return ICore.Instance.ResourceAP; } }

        /// <summary>
        /// The <see cref="IAsyncProcessor">async processor</see> for executing operations
        /// in the network thread.
        /// </summary>
        public static IAsyncProcessor       NetworkAP            { [DebuggerStepThrough] get { return ICore.Instance.NetworkAP; } }

        /// <summary>
        /// The <see cref="IAsyncProcessor">async processor</see> for executing operations
        /// in the UI thread. It is recommended to use this call instead of Control.BeginInvoke().
        /// <remarks>Is equal to null until <see cref="CoreState"/>CoreState is not Running. 
        /// It is always safe to use <see cref="IUIManager.QueueUIJob">Core.UIManager.QueueUIJob</see> 
        /// as an alternative.</remarks>
        /// <since>2.0</since>
        /// </summary>
        public static IAsyncProcessor       UserInterfaceAP            { [DebuggerStepThrough] get { return ICore.Instance.UserInterfaceAP; } }
        
        /// <summary>
        /// The <see cref="IWorkspaceManager">workspace manager</see> which provides information
        /// about workspaces and services for registering the relationship between resources and
        /// workspaces.
        /// </summary>
        public static IWorkspaceManager     WorkspaceManager     { [DebuggerStepThrough] get { return ICore.Instance.WorkspaceManager; } }
        
        /// <summary>
        /// Returns the <see cref="ICategoryManager">category manager</see> interface.
        /// </summary>
        public static ICategoryManager      CategoryManager      { [DebuggerStepThrough] get { return ICore.Instance.CategoryManager; } }
        
        /// <summary>
        /// The <see cref="IResourceTreeManager">Resource Tree Manager</see> which handles
        /// resource tree roots for specific resource types and sorting of items in resource trees.
        /// </summary>
        public static IResourceTreeManager  ResourceTreeManager  { [DebuggerStepThrough] get { return ICore.Instance.ResourceTreeManager; } }
        
        /// <summary>
        /// The <see cref="IDisplayColumnManager">Display Column Manager</see> that handles 
        /// the default resource list columns used when displaying resource lists
        /// and the columns which are available in the "Configure Columns" dialog.
        /// </summary>
        public static IDisplayColumnManager DisplayColumnManager { [DebuggerStepThrough] get { return ICore.Instance.DisplayColumnManager; } }
        
        /// <summary>
        /// The <see cref="IResourceIconManager">Icon Manager</see> that handles registration 
        /// and retrieval of icons for resources.
        /// </summary>
        /// <remarks>All small (16x16) resource icons are put in one global image list, 
        /// which can be accessed through the <see cref="ImageList"/>property.</remarks>
        public static IResourceIconManager  ResourceIconManager  { [DebuggerStepThrough] get { return ICore.Instance.ResourceIconManager; } }

        /// <summary>
        /// The <see cref="INotificationManager">Notification Manager</see> that handles the rules 
        /// that are used for notifying the user about arriving resources.
        /// </summary>
        public static INotificationManager  NotificationManager  { [DebuggerStepThrough] get { return ICore.Instance.NotificationManager; } }
        
        /// <summary>
        /// The <see cref="IMessageFormatter">Message Formatter</see> which provides services
        /// for getting an HTML formatted presentation of plain-text messages and for quoting
        /// replies.
        /// </summary>
        public static IMessageFormatter     MessageFormatter     { [DebuggerStepThrough] get { return ICore.Instance.MessageFormatter; } }
        
        /// <summary>
        /// The <see cref="IContactManager">Contact Manager</see> that handles various core operations with contacts.
        /// </summary>
        public static IContactManager       ContactManager       { [DebuggerStepThrough] get { return ICore.Instance.ContactManager; } }

        /// <summary>
        /// The <see cref="IFileResourceManager">File Resource Manager</see> that handles the 
        /// relationship between file extensions, MIME types and format-describing resources.
        /// </summary>
        public static IFileResourceManager     FileResourceManager     { [DebuggerStepThrough] get { return ICore.Instance.FileResourceManager; } }

        /// <summary>
        /// IDs of the resource properties which are registered by the core.
        /// </summary>
        /// <since>2.0</since>
        public static ICoreProps Props { [DebuggerStepThrough] get { return ICore.Instance.Props; } }

        /// <summary>
        /// Omea product short name.
        /// </summary>
        public static string                ProductName      { [DebuggerStepThrough] get { return ICore.Instance.ProductName; } }

        /// <summary>
        /// Omea product full name.
        /// </summary>
        public static string                ProductFullName  { [DebuggerStepThrough] get { return ICore.Instance.ProductFullName; } }

        /// <summary>
        /// Omea product build number.
        /// </summary>
        public static Version               ProductVersion     { [DebuggerStepThrough] get { return ICore.Instance.ProductVersion; } }

        /// <summary>
        /// Omea product release version, marketing-style. Non-<c>Null</c> in RTM builds only, <c>Null</c> in internal/beta builds.
        /// </summary>
        public static string                ProductReleaseVersion { [DebuggerStepThrough] get { return ICore.Instance.ProductReleaseVersion; } }

        /// <summary>
        /// The current scale factor.
        /// </summary>
        public static SizeF                 ScaleFactor      { [DebuggerStepThrough] get { return ICore.Instance.ScaleFactor; } }

        /// <summary><seealso cref="IsSystemIdle"/>
        /// The idle period, in minutes.
        /// </summary>
        /// <remarks>The idle time is detected by monitoring the user activity. This information allows to run complex tasks at a time when they would not interfere with normal work.</remarks>
        public static int                   IdlePeriod // in minutes
        { 
            [DebuggerStepThrough] get { return ICore.Instance.IdlePeriod; } 
            [DebuggerStepThrough] set { ICore.Instance.IdlePeriod = value; } 
        } 

        /// <summary>
        /// Detects whether the system is in the idle state or not depending on the <see cref="IdlePeriod"/> value.
        /// </summary>
        public static bool                IsSystemIdle       { [DebuggerStepThrough] get { return ICore.Instance.IsSystemIdle; } }

        /// <summary>
        /// Current state of the Omea application.
        /// </summary>
        public static CoreState State                        { [DebuggerStepThrough] get { return ICore.Instance.State; } }

        /// <summary><seealso cref="CoreState"/>
        /// Fires when Omea <see cref="State">application state</see> changes.
        /// </summary>
        public static event EventHandler StateChanged
        {
            add
            {
                ICore.Instance.StateChanged += value;
            }
            remove
            {
                ICore.Instance.StateChanged -= value;
            }
        }

        /// <summary>
        /// The default proxy configured in the Omea options dialog.
        /// </summary>
        public static IWebProxy DefaultProxy
        {
            get { return ICore.Instance.DefaultProxy; }
        }

        /// <summary>
        /// Reports an exception to the UI and provides an opportunity to submit this exception to the tracker.
        /// </summary>
        /// <param name="e">The exception being reported.</param>
        /// <param name="fatal">Specifies whether the exception is fatal or not.</param>
        public static void ReportException( Exception e, bool fatal ) { ICore.Instance.ReportException( e, fatal ); }

        /// <summary>
        /// Reports an exception to the UI and provides an opportunity to submit this exception to the tracker.
        /// </summary>
        /// <param name="e">The exception being reported.</param>
        /// <param name="flags">Provides a combination of flags that specify whether the exception is fatal or if the log file is to be attached when the exception is submitted. See <see cref="ExceptionReportFlags"/> for possible values.</param>
        public static void ReportException( Exception e, ExceptionReportFlags flags ) { ICore.Instance.ReportException( e, flags ); }

        /// <summary>
        /// Shows an icon notifying a user that an exception occurred during background processing.
        /// </summary>
        /// <remarks>Double-clicking the icon shows a dialog for submitting the exception to the
        /// tracker.</remarks>
        /// <param name="e">The exception to report.</param>
        public static void ReportBackgroundException( Exception e ) { ICore.Instance.ReportBackgroundException( e ); }

        /// <summary>
        /// Forces self-restart of the application. Can be invoked in the arbitrary thread.
        /// </summary>
        /// <since>2.2</since>
        public static void RestartApplication()
        {
            ICore.Instance.RestartApplication();
        }
        
        /// <summary>
        /// Adds the specified text string to the environment data included in exception reports
        /// submitted to the tracker.
        /// </summary>
        /// <param name="data">The string to include in exception reports.</param>
        public static void AddExceptionReportData( string data ) { ICore.Instance.AddExceptionReportData( data ); }

        /// <summary>
        /// Occurs when an exception happens in an Omea thread which is not managed by an
        /// <see cref="IAsyncProcessor"/>.
        /// </summary>
        /// <since>2.0</since>
        public static event OmeaThreadExceptionEventHandler BackgroundThreadException
        {
            add { ICore.Instance.BackgroundThreadException += value; }
            remove { ICore.Instance.BackgroundThreadException -= value; }
        }

        /// <summary>
        /// The <see cref="IRemoteControlManager">Remote controller Manager</see> registers
        /// methods for remote invokation.
        /// </summary>
        /// <since>2.0</since>
        public static IRemoteControlManager RemoteControllerManager { [DebuggerStepThrough] get { return ICore.Instance.RemoteControllerManager; } }

        /// <summary>
        /// The <see cref="IProtocolHandlerManager">Protocol Handler Manager</see> for registering of protocol handlers.
        /// </summary>
        /// <since>2.0</since>
        public static IProtocolHandlerManager ProtocolHandlerManager { get { return ICore.Instance.ProtocolHandlerManager; } }

        /// <summary>
        /// Returns the registered implementation of a component of the specified type.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <returns>Component implementation, or null if none was registered.</returns>
        /// <since>2.0</since>
        public static object GetComponentImplementation( Type componentType )
        {
            return ICore.Instance.GetComponentImplementation( componentType );
        }
    }
    
    /// <summary>
    /// An interface for the static <see cref="Core"/> class that provides access to all core service interfaces.
    /// </summary>
    /// <remarks>Plugin implementors interact with Omea through interfaces which they get from a class implementing the <see cref="ICore"/> interface. Use the static <see cref="Core"/> class to obtain an <see cref="ICore"/> implementor.</remarks>
    public abstract class ICore
    {
        /// <summary>
        /// The one and only <see cref="Core"/> instance.
        /// </summary>
        protected static ICore theInstance;
        
        /// <summary><seealso cref="ResourceProxy"/>
        /// The main interface for creating and accessing resources.
        /// </summary>
        /// <remarks>
        /// <para>Do not modify resources in non-resource threads! At most times when Omea is running (except for the time when the
        /// <see cref="IPlugin.Register"/> method is called), only one thread is designated
        /// as the resource store write thread, and all operations that modify the resource store
        /// (creating resources, changing resource properties, deleting resources) must be executed
        /// in that thread. The <see cref="ResourceProxy"/> class provides an easy way to run a resource write
        /// operation synchronously or asynchronously.</para></remarks>
        public abstract IResourceStore      ResourceStore   { get; }

        /// <summary>
        /// Controls the progress window which is displayed at program startup and when 
        /// <see cref="IUIManager.RunWithProgressWindow"/> is used.
        /// </summary>
        public abstract IProgressWindow     ProgressWindow  { get; }
        
        /// <summary>
        /// The Action Manager that allows to register actions — functions which can be invoked by the user through
        /// different user interface controls (menu items, toolbar buttons, keyboard and so on).
        /// </summary>
        public abstract IActionManager      ActionManager   { get; }

        /// <summary><seealso cref="Core.UIManager"/>
        /// The User Interface manager that provides access to UI elements and their settings.
        /// </summary>
        public abstract IUIManager          UIManager       { get; }

        /// <summary><seealso cref="IResourceTypeTab"/>
        /// The Tab Manager that allows to register resource type tabs and to get information about the
        /// registered resource type tabs.
        /// </summary>
        public abstract ITabManager         TabManager      { get; }

        /// <summary><seealso cref="IResource"/><seealso cref="IResourceList"/><seealso cref="ColumnDescriptor"/>
        /// The Resource Browser that manages the display of resources and resource lists in Omea.
        /// </summary>
        public abstract IResourceBrowser    ResourceBrowser { get; }

        /// <summary>
        /// The sidebar at the left side of Omea main window. Supports showing different sidebars depending on the active tab.
        /// </summary>
        public abstract ISidebarSwitcher    LeftSidebar     { get; }
        
        /// <summary>
        /// The sidebar at the right side of Omea main window.
        /// </summary>
        public abstract ISidebar            RightSidebar    { get; }

        /// <summary><seealso cref="ISettingStore"/>
        /// The Setting Store that is a facility for storing the settings of Omea application and plugins.
        /// </summary>
        /// <remarks>You should use the Setting Store rather than implement your own settings-storing facilities.</remarks>
        public abstract ISettingStore       SettingStore    { get; }

        /// <summary>
        /// The Plugin Loader that manages the registration of interfaces for handling resources of specific types
        /// provided by plugins.
        /// </summary>
        public abstract IPluginLoader       PluginLoader    { get; }

        /// <summary>
        /// TextIndexManager controls the text indexing process - queues indexing requests,
        /// checks the existence of the index, queues query processing.
        /// </summary>
        public abstract ITextIndexManager   TextIndexManager { get; }

        /// <summary>
        /// FilterManager controls the creation, maintenance and execution of
        /// rules, formatting rules and views.
        /// </summary>
        public abstract IFilterManager      FilterManager    { get; }

        /// <summary>
        /// The <see cref="ITrayIconManager">Tray Icon Manager</see> provides interfaces
        /// for working with tray icon rules.
        /// </summary>
        /// <since>2.0</since>
        public abstract ITrayIconManager    TrayIconManager  { get; }

        /// <summary>
        /// The <see cref="IFormattingRuleManager">Formatting Rule Manager</see> provides interfaces
        /// for working with formatting rules.
        /// </summary>
        /// <since>2.0</since>
        public abstract IFormattingRuleManager  FormattingRuleManager { get; }

        /// <summary>
        /// The <see cref="IExpirationRuleManager">Expiration Rule Manager</see> provides interfaces
        /// for working with expiration rules.
        /// </summary>
        /// <since>2.0</since>
        public abstract IExpirationRuleManager  ExpirationRuleManager { get; }

        /// <summary>
        /// The <see cref="IFilteringFormsManager">Filtering Forms Manager</see> allows plugins
        /// to open standard dialogs for advanced search and creating/editing views and rules of different types.
        /// </summary>
        /// <since>2.0</since>
        public abstract IFilteringFormsManager  FilteringFormsManager { get; }

        /// <summary>
        /// <see cref="ISearchQueryExtensions">Search query extensions</see> allow plugins
        /// to extend search query with predefined conditions in a very simple way.
        /// </summary>
        /// <since>2.2</since>
        public abstract ISearchQueryExtensions SearchQueryExtensions  { get; }

        /// <summary>
        /// The <see cref="IUnreadManager">Unread Manager</see> which allows to retrieve
        /// unread counts for views and other resources.
        /// </summary>
        public abstract IUnreadManager          UnreadManager    { get; }

        /// <summary><seealso cref="IWin32Window"/>
        /// Handle to the main window.
        /// </summary>
        public abstract IWin32Window        MainWindow       { get; }

        /// <summary>
        /// The Web browser embedded in Omea.
        /// </summary>
        public abstract AbstractWebBrowser   WebBrowser        { get; }

        /// <summary>
        /// Provides an <see cref="IAsyncProcessor">asynchronous processor</see> for the resource thread.
        /// Allows to queue jobs for execution on the resource thread, or execute them immediately and wait for them to complete.
        /// </summary>
        public abstract IAsyncProcessor     ResourceAP       { get; }

        /// <summary>
        /// Provides an <see cref="IAsyncProcessor">asynchronous processor</see> for the network thread.
        /// Allows to queue jobs for execution on the network thread, or execute them immediately and wait for them to complete.
        /// </summary>
        public abstract IAsyncProcessor     NetworkAP        { get; }

        /// <summary>
        /// Provides an <see cref="IAsyncProcessor">asynchronous processor</see> for the user interface thread.
        /// Allows to queue jobs for execution on the user interface thread, or execute them immediately and wait for them to complete.
        /// </summary>
        /// <remarks>
        /// <para>This async processor supercedes the old <see cref="IUIManager.QueueUIJob"/> method and provides the full-scale <see cref="IAsyncProcessor">asynchronous processor</see> for the user interface thread.</para>
        /// <para>Normally, the Windows Message Queue is not used for executing the UI tasks. However, if an UI job has to be executed from inside another UI job, the standard <see cref="Control.BeginInvoke"/> is still used.</para>
        /// </remarks>
        /// <since>2.0</since>
        public abstract IAsyncProcessor     UserInterfaceAP  { get; }

        /// <summary>
        /// Returns the <see cref="IWorkspaceManager">workspace manager</see> interface.
        /// </summary>
        public abstract IWorkspaceManager   WorkspaceManager { get; }

        /// <summary>
        /// Returns the <see cref="ICategoryManager">category manager</see> interface.
        /// </summary>
        public abstract ICategoryManager      CategoryManager  { get; }

        /// <summary>
        /// Returns the <see cref="IResourceTreeManager">resource tree manager</see> interface.
        /// </summary>
        public abstract IResourceTreeManager  ResourceTreeManager  { get; }

        /// <summary>
        /// The Display Column Manager that arranges the default resource list columns used when displaying resource lists
        /// and the columns which are available in the "Configure Columns" dialog.
        /// </summary>
        public abstract IDisplayColumnManager DisplayColumnManager { get; }

        /// <summary>
        /// The Icon Manager that handles registration and retrieval of icons for resources.
        /// </summary>
        /// <remarks>All small (16x16) resource icons are put in one global image list, 
        /// which can be accessed through the <see cref="ImageList"/>property.</remarks>
        public abstract IResourceIconManager  ResourceIconManager  { get; }

        /// <summary>
        /// The Notification Manager that handles the rules that are used for notifying the user about arriving resources.
        /// </summary>
        public abstract INotificationManager  NotificationManager  { get; }

        /// <summary>
        /// The Contact Manager that handles various core operations with contacts.
        /// </summary>
        public abstract IContactManager       ContactManager       { get; }

        /// <summary>
        /// The <see cref="IFileResourceManager">File Resource Manager</see> that handles the 
        /// relationship between file extensions, MIME types and format-describing resources.
        /// </summary>
        public abstract IFileResourceManager     FileResourceManager     { get; }

        /// <summary>
        /// The <see cref="IMessageFormatter">Message Formatter</see> which provides services
        /// for getting an HTML formatted presentation of plain-text messages and for quoting
        /// replies.
        /// </summary>
        public abstract IMessageFormatter     MessageFormatter     { get; }

        /// <summary>
        /// IDs of the resource properties which are registered by the core.
        /// </summary>
        /// <since>2.0</since>
        public abstract ICoreProps Props { get; }

        /// <summary>
        /// Omea product short name.
        /// </summary>
        public abstract string              ProductName            { get; }

        /// <summary>
        /// Omea product full name.
        /// </summary>
        public abstract string              ProductFullName        { get; }

        /// <summary>
        /// Omea product build number.
        /// </summary>
        public abstract Version              ProductVersion           { get; }

        /// <summary>
        /// Omea product release version.
        /// </summary>
        public abstract string              ProductReleaseVersion  { get; }

        /// <summary>
        /// The current scale factor.
        /// </summary>
        public abstract SizeF               ScaleFactor            { get; }

        /// <summary><seealso cref="IsSystemIdle"/>
        /// The idle period, in minutes.
        /// </summary>
        /// <remarks>The idle time is detected by monitoring the user activity. This information allows to run complex tasks at a time when they would not interfere with normal work.</remarks>
        public abstract int                 IdlePeriod { get; set; } // in minutes

        /// <summary>
        /// Detects whether the system is in the idle state or not depending on the <see cref="IdlePeriod"/> value.
        /// </summary>
        public abstract bool                IsSystemIdle { get; }

        /// <summary>
        /// Current state of the Omea application.
        /// </summary>
        public abstract CoreState State { get; }

        /// <summary><seealso cref="CoreState"/>
        /// Fires when Omea <see cref="State">application state</see> changes.
        /// </summary>
        public abstract event EventHandler StateChanged;

        /// <summary>
        /// Reports an exception to the UI and provides an opportunity to submit this exception to the ITN tracker.
        /// </summary>
        /// <param name="e">The exception being reported.</param>
        /// <param name="fatal">Specifies whether the exception is fatal or not.</param>
        public abstract void ReportException( Exception e, bool fatal );

        /// <summary>
        /// Reports an exception to the UI and provides an opportunity to submit this exception to the ITN tracker.
        /// </summary>
        /// <param name="e">The exception being reported.</param>
        /// <param name="flags">Provides a combination of flags that specify whether the exception is fatal or if the log file is to be attached when the exception is submitted. See <see cref="ExceptionReportFlags"/> for possible values.</param>
        public abstract void ReportException( Exception e, ExceptionReportFlags flags );

        /// <summary>
        /// Adds the specified text string to the environment data included in exception reports
        /// submitted to the tracker.
        /// </summary>
        /// <param name="data">The string to include in exception reports.</param>
        public abstract void AddExceptionReportData( string data );

        /// <summary>
        /// Shows an icon notifying a user that an exception occurred during background processing.
        /// </summary>
        /// <remarks>Double-clicking the icon shows a dialog for submitting the exception to the
        /// tracker.</remarks>
        /// <param name="e">The exception to report.</param>
        public abstract void ReportBackgroundException( Exception e );

        /// <summary>
        /// Forces self-restart of the application. Can be invoked in the arbitrary thread.
        /// </summary>
        /// <since>2.2</since>
        public abstract void RestartApplication();

        /// <summary>
        /// Occurs when an exception happens in an Omea thread which is not managed by an
        /// <see cref="IAsyncProcessor"/>.
        /// </summary>
        /// <since>2.0</since>
        public abstract event OmeaThreadExceptionEventHandler BackgroundThreadException;

        /// <summary>
        /// The default proxy configured in the Omea options dialog.
        /// </summary>
        public abstract IWebProxy DefaultProxy
        {
            get;
        }

        /// <summary>
        /// The one and only Core instance implementing the <see cref="ICore"/> interface.
        /// </summary>
        public static ICore Instance
        {
            [DebuggerStepThrough] get { return theInstance; }
        }

        /// <summary>
        /// The <see cref="IRemoteControlManager">Remote controller Manager</see> that registers
        /// methods for remote invokation.
        /// </summary>
        /// <since>2.0</since>
        public abstract IRemoteControlManager RemoteControllerManager { get ; }

        /// <summary>
        /// The <see cref="IProtocolHandlerManager">Protocol Handler Manager</see> for registering of protocol handlers.
        /// </summary>
        /// <since>2.0</since>
        public abstract IProtocolHandlerManager ProtocolHandlerManager { get; }

        /// <summary>
        /// Returns the registered implementation of a component of the specified type.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <returns>Component implementation, or null if none was registered.</returns>
        /// <since>2.0</since>
        public abstract object GetComponentImplementation( Type componentType );
    }
}
