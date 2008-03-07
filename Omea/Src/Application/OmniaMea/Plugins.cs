/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.ResourceTools;
using Microsoft.Win32;

using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

using JetBrains.Omea.OpenAPI;
using System.IO;
using System.Xml;
using JetBrains.DataStructures;

namespace JetBrains.Omea.Plugins
{
    /// <summary>
    /// Plugins singleton loader.
    /// Loads plugin assemblies enumerated in the 'Plugins' reg key.
    /// Afterwards plugins may be enumerated as instances of IPlugin
    /// </summary>
    class Loader : IEnumerable, IPluginLoader
    {
        #region Exceptions

        // XPluginsRegKeyError is thrown if standard 'Plugins' registry
        // key cannot be opened.
        public class XPluginsRegKeyError : Exception
        {
            public XPluginsRegKeyError( string sError )
                : base( sError ) {}
        }

        #endregion

        public string PluginRegistryKey
        {
            get { return _pluginRegistryKey; }
        }

        public void LoadPlugins()
        {
            RegistryKey pluginsHKCUKey = null;
            RegistryKey pluginConfigHKCUKey = null;
            RegistryKey pluginsHKLMKey = null;
            try
            {
                pluginsHKCUKey = Registry.CurrentUser.OpenSubKey( _csPluginsRegKey + "Tokaj" );
                pluginsHKLMKey = Registry.LocalMachine.OpenSubKey( _csPluginsRegKey + "Tokaj" );
                if ( pluginsHKCUKey == null && pluginsHKLMKey == null )
                {
                    _pluginRegistryKey = _csPluginsRegKey;
                    pluginsHKCUKey = Registry.CurrentUser.OpenSubKey( _csPluginsRegKey );
                    pluginsHKLMKey = Registry.LocalMachine.OpenSubKey( _csPluginsRegKey );
                }
                else
                {
                    _pluginRegistryKey = _csPluginsRegKey + "Tokaj";
                }
                pluginConfigHKCUKey = pluginsHKCUKey.OpenSubKey( _configKey );
            }
            catch( Exception e )
            {
                Trace.WriteLine( e.ToString(), "Plugins.Loader" );
            }
            if( pluginsHKCUKey == null && pluginsHKLMKey == null )
            {
                throw new XPluginsRegKeyError( "Can't open registry key " + _pluginRegistryKey );
            }

            // load & register plugins
            string[] disabledPlugins = new string[] { string.Empty };
            if( pluginConfigHKCUKey != null )
            {
                disabledPlugins =
                    ((string) pluginConfigHKCUKey.GetValue( _disabledValue, string.Empty )).Split(';');
            }
            Array.Sort( disabledPlugins );

            if( pluginsHKCUKey != null )
            {
                LoadAssembliesByRegistryConfig( pluginsHKCUKey, disabledPlugins );
            }
            if( pluginsHKLMKey != null )
            {
                LoadAssembliesByRegistryConfig( pluginsHKLMKey, disabledPlugins );
            }

            foreach( Assembly assembly in _xmlConfigAssemblies )
            {
                LoadXmlConfiguration( assembly );
            }
            _xmlConfigAssemblies = null;

            MarkUnloadedResourceTypes();
        }

        private void LoadAssembliesByRegistryConfig( RegistryKey key, string[] disabledPlugins )
        {
            string[] pluginNames = key.GetValueNames();
            int pluginsLoaded = 0;
            foreach( string pluginName in pluginNames )
            {
                SplashScreen progressWindow = (SplashScreen)Core.ProgressWindow;
                if ( progressWindow != null )
                {
                    progressWindow.UpdateProgress( pluginsLoaded * 100 / pluginNames.Length,
                        "Loading plugins...", null );
                }

                pluginsLoaded++;

                if( Array.BinarySearch( disabledPlugins, pluginName ) < 0 )
                {
                    object pluginPath = key.GetValue( pluginName );
                    if( !_loadedAssemblies.Contains( pluginPath ) && pluginPath is string )
                    {
                        string loadingError = null;
                        string path = (string) pluginPath;
                        if ( File.Exists( path ) )
                        {
                            Trace.WriteLine( "Loading plugin " + path );
                            LoadPlugin( key, pluginName, path, ref loadingError );
                            _loadedAssemblies.Add( pluginPath );

                            if( loadingError != null && progressWindow != null )
                            {
                                progressWindow.AddErrorRecord( pluginName, loadingError );
                            }
                        }
                        else
                        {
                            if (progressWindow != null)
                                progressWindow.AddErrorRecord(pluginName, "Can not find file on disk");
                            CheckRemovePlugin(key, pluginName, path, "was not found");
                        }
                    }
                }
            }
        }

        private static void CheckRemovePlugin( RegistryKey key, string pluginName, string path, string msg )
        {
            DialogResult dr = MessageBox.Show( Core.MainWindow, "The plugin file '" + path + "' " + msg + 
                                               ". Would you like to remove the plugin from the list of registered plugins?",
                                               Core.ProductFullName, MessageBoxButtons.YesNo );
            if ( dr == DialogResult.Yes )
            {
                RemovePlugin( key, pluginName );
            }
        }

        private static void RemovePlugin( RegistryKey key, string pluginName )
        {
            string[] keyParts = key.Name.Split( new char[] { '\\' }, 2 );
            RegistryKey rootKey = (keyParts [0].IndexOf( "LOCAL_MACHINE" ) >= 0 ) 
                ? Registry.LocalMachine 
                : Registry.CurrentUser;
            try
            {
                RegistryKey rwKey = rootKey.OpenSubKey( keyParts [1], true );
                rwKey.DeleteValue( pluginName );
                rwKey.Close();
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "Failed to remove plugin registration: " + ex.ToString() );
                MessageBox.Show( Core.MainWindow,
                                 "Failed to remove plugin registration", Core.ProductFullName );
            }
        }

        private void MarkUnloadedResourceTypes()
        {
            ArrayList pluginNames = new ArrayList();
            foreach( IPlugin plugin in _pluginList )
            {
                pluginNames.Add( plugin.GetType().FullName );
            }
            MyPalStorage.Storage.MarkHiddenResourceTypes( (string[]) pluginNames.ToArray( typeof(string) ) );
        }

        public void StartupPlugins()
        {
            int i = 0;
            IProgressWindow window = Core.ProgressWindow;
            foreach( IPlugin plugin in _pluginList )
            {
                Trace.WriteLine("Starting plugin " + plugin.GetType().Name);

                if (window != null)
                    window.UpdateProgress( ++i * 100 / _pluginList.Count, "Starting plugins...", null );

                _startupError = null;
                Core.ResourceAP.RunJob(new StartupDelegate(DoStartupPlugin), plugin);
                if (_cancelStartup)
                {
                    throw new CancelStartupException();
                }
                else
                if( _startupError != null )
                {
                    ((SplashScreen)window).AddErrorRecord(plugin.GetType().Name, _startupError );
                }
            }
        }

        private void DoStartupPlugin( IPlugin plugin )
        {
            try
            {
                plugin.Startup();
            }
            catch( CancelStartupException )
            {
                _cancelStartup = true;
            }
            catch( Exception e )
            {
                _startupError = e.Message;
            }
        }

        private delegate void StartupDelegate( IPlugin plugin );

        public static bool IsOmniaMeaPlugin( string filename )
        {
            Type[] pluginTypes;
            try
            {
                pluginTypes = Assembly.LoadFrom( filename ).GetExportedTypes();
            }
            catch( Exception e )
            {
                Trace.WriteLine( e.ToString(), "Plugins.Loader" );
                return false;
            }
            // search for IPlugin instances
            foreach( Type aType in pluginTypes )
                if( aType.GetInterface( "IPlugin", false ) != null )
                    return true;
            return false;
        }

        public void RegisterResourceTextProvider( string resType, IResourceTextProvider provider )
        {
            if ( resType == null )
            {
                lock( _genericResourceTextProviders )
                {
                    _genericResourceTextProviders.Add( provider );
                }
                
            }
            else
            {
                ArrayList providerList = (ArrayList) _resourceTextProviders [resType];
                if ( providerList == null )
                {
                    providerList = new ArrayList();
                    _resourceTextProviders [resType] = providerList;
                }
                lock( providerList )
                {
                    providerList.Add( provider );
                }
            }
        }

        public bool HasTypedTextProvider( string resType )
        {
            #region Preconditions
            if( resType == null )
                throw new ArgumentNullException( "PluginLoader -- Input resource type can not be null." );
            #endregion Preconditions

            ArrayList providerList = (ArrayList) _resourceTextProviders[ resType ];
            return (providerList != null);
        }

        public void InvokeResourceTextProviders( IResource res, IResourceTextConsumer consumer )
        {
            #region Preconditions
            if ( res == null )
                throw new ArgumentNullException( "PluginLoader -- Resource is null." );

            if ( consumer == null )
                throw new ArgumentNullException( "PluginLoader -- IResourceTextConsumer is null." );
            #endregion Preconditions
            
            bool      isSuccess = true;
            ArrayList providerList = (ArrayList) _resourceTextProviders [res.Type];
            if ( providerList != null )
            {
                lock( providerList )
                {
                    foreach( IResourceTextProvider provider in providerList )
                    {
                        IResourceTextIndexingPermitter permitter = provider as IResourceTextIndexingPermitter;
                        if( permitter != null && !permitter.CanIndexResource( res ) )
                        {
                            return;
                        }
                    }
                }
            }
            lock( _genericResourceTextProviders )
            {
                foreach( IResourceTextProvider provider in _genericResourceTextProviders )
                {
                    IResourceTextIndexingPermitter permitter = provider as IResourceTextIndexingPermitter;
                    if( permitter != null && !permitter.CanIndexResource( res ) )
                    {
                        return;
                    }
                }
            }
            if ( providerList != null )
            {
                lock( providerList )
                {
                    foreach( IResourceTextProvider provider in providerList )
                    {
                        isSuccess = isSuccess && provider.ProcessResourceText( res, consumer );
                    }
                }
            }
            lock( _genericResourceTextProviders )
            {
                foreach( IResourceTextProvider provider in _genericResourceTextProviders )
                {
                    isSuccess = isSuccess && provider.ProcessResourceText( res, consumer );
                }
            }
            if( !isSuccess )
                consumer.RejectResult();
        }

        #region IPluginLoader members
        public void RegisterResourceUIHandler( string resType, IResourceUIHandler handler )
        {
            #region Preconditions
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
            {
                throw new ArgumentException( "Invalid resource type '" + resType + "'", "resType" );
            }
            #endregion

            _resourceUIHandlerHash [resType] = handler;
        }

        public IResourceUIHandler GetResourceUIHandler( string resType )
        {
            return (IResourceUIHandler) _resourceUIHandlerHash [resType];
        }

        public IResourceUIHandler GetResourceUIHandler( IResource res )
        {
            return (IResourceUIHandler) GetHandlerFromMap( res, _resourceUIHandlerHash );
        }

        public void RegisterResourceDragDropHandler( string resType, IResourceDragDropHandler handler )
        {
            #region Preconditions
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
        		throw new ArgumentException( "Invalid resource type \"" + resType + "\".", "resType" );
            #endregion Preconditions

        	lock( _resourceDragDropHandlerHash )
        	{
        		// Check if a handler is already registered
        		IResourceDragDropHandler existing = _resourceDragDropHandlerHash[ resType ] as IResourceDragDropHandler;
        		if( existing != null )
        		{
        			// The handler's registered; if it's already a composite one, add a new handler to it
        			// If it's a simple handler, create a new composite of the old and new ones
        			ResourceDragDropCompositeHandler composite = existing as ResourceDragDropCompositeHandler;
        			if( composite != null )
        				composite.AddHandler( handler );
        			else
        				_resourceDragDropHandlerHash[ resType ] = new ResourceDragDropCompositeHandler( existing, handler );
        		}
        		else // There were no handler yet, just write the raw unwrapped handler
        			_resourceDragDropHandlerHash[ resType ] = handler;
        	}
        }

    	public IResourceDragDropHandler GetResourceDragDropHandler( IResource res )
        {
			lock(_resourceDragDropHandlerHash)
				return (IResourceDragDropHandler) GetHandlerFromMap( res, _resourceDragDropHandlerHash );
        }

        public IResourceDragDropHandler GetResourceDragDropHandler( string resType )
        {
			lock(_resourceDragDropHandlerHash)
	            return (IResourceDragDropHandler) _resourceDragDropHandlerHash [resType];
        }

        public void RegisterResourceRenameHandler( string resType, IResourceRenameHandler handler )
        {
            #region Preconditions
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
            {
                throw new ArgumentException( "Invalid resource type '" + resType + "'", "resType" );
            }
            #endregion Preconditions

            _resourceRenameHandlerHash [resType] = handler;
        }

        public IResourceRenameHandler GetResourceRenameHandler( IResource res )
        {
            return (IResourceRenameHandler) GetHandlerFromMap( res, _resourceRenameHandlerHash );
        }

        private static object GetHandlerFromMap( IResource res, HashMap handlerMap )
        {
            if ( res == null || Core.ResourceStore == null )
                return null;
    
            object handler = handlerMap [res.Type];
            if ( handler == null )
            {
                foreach( int linkTypeId in res.GetLinkTypeIds() )
                {
                    if ( Core.ResourceStore.PropTypes [linkTypeId].HasFlag( PropTypeFlags.SourceLink ) )
                    {
                        IResource source = res.GetLinkProp( linkTypeId );
                        if ( source != null )   // the link may be reverse
                        {
                            handler = handlerMap [source.Type];
                        }
                    }
                }
            }
            return handler;
        }

        public void RegisterResourceThreadingHandler( string resType, IResourceThreadingHandler handler )
        {
            _threadingHandler.AddHandler( resType, handler );
        }

        public void RegisterResourceThreadingHandler( int propId, IResourceThreadingHandler handler )
        {
            _threadingHandler.AddHandler( propId, handler );
        }

        public void RegisterDefaultThreadingHandler( string resType, int replyProp )
        {
            _threadingHandler.AddHandler( resType, new DefaultThreadingHandler( replyProp ) );
        }

        public IResourceThreadingHandler GetResourceThreadingHandler( string resType )
        {
            return _threadingHandler.GetHandler( resType );
        }

        public IResourceThreadingHandler CompositeThreadingHandler
        {
            get { return _threadingHandler; }
        }

        public IResourceDisplayer GetResourceDisplayer( string resType )
        {
        	return (IResourceDisplayer) _resourceDisplayerHash [ resType ];
        }

        public void RegisterResourceDisplayer( string resType, IResourceDisplayer displayer )
        {
        	_resourceDisplayerHash [ resType ] = displayer;
        }

        public void RegisterNewspaperProvider( string resType, INewspaperProvider provider )
        {
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
                throw new ArgumentException( "Resource type '" + resType + "' does not exist", "resType" );

            _newspaperProviders [resType] = provider;
        }

        public INewspaperProvider GetNewspaperProvider( string resType )
        {
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
                throw new ArgumentException( "Resource type '" + resType + "' does not exist", "resType" );

            return (INewspaperProvider) _newspaperProviders [ resType ];
        }

        public void RegisterResourceDeleter( string resType, IResourceDeleter deleter )
        {
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
                throw new ArgumentException( "Resource type '" + resType + "' does not exist", "resType" );

            _resourceDeleters [ resType ] = deleter;
        }

        public IResourceDeleter GetResourceDeleter( string resType )
        {
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
                throw new ArgumentException( "Resource type '" + resType + "' does not exist", "resType" );

            return (IResourceDeleter) _resourceDeleters [resType];
        }

        public IStreamProvider GetStreamProvider( string resType )
        {
            return (IStreamProvider) _streamProviderHash [ resType ];
        }

        public void RegisterStreamProvider( string resType, IStreamProvider provider )
        {
            _streamProviderHash [ resType ] = provider;
        }

        public void RegisterPluginService( object pluginService )
        {
            _pluginServices.Add( pluginService );
        }

        public object GetPluginService( Type serviceType )
        {
            // scan last registered services first
            for( int i = _pluginServices.Count - 1; i >= 0; i-- )
            {
                object service = _pluginServices [ i ];
                if ( serviceType.IsInstanceOfType( service ) )
                {
                    return service;
                }
            }
            return null;
        }

        public void RegisterResourceSerializer( string resType, IResourceSerializer serializer )
        {
            _resourceSerializers[ resType ] = serializer;
        }

        public IResourceSerializer GetResourceSerializer( string resType )
        {
            return (IResourceSerializer) _resourceSerializers [ resType ];
        }

        public void RegisterViewsConstructor( IViewsConstructor constructor )
        {
            if( _viewsConstructors.IndexOf( constructor ) == -1 )
                _viewsConstructors.Add( constructor );
        }

        public ArrayList GetViewsConstructors()
        {
            return _viewsConstructors;
        }

        public void GetPluginDescription( string path, out string author, out string description )
        {
            author = (string) _pluginsAuthors[ path ];
            description = (string) _pluginsDescriptions[ path ];
        }
        #endregion

        #region implementation of IEnumerable

        public IEnumerator GetEnumerator()
        {
            return _pluginList.GetEnumerator();
        }

        #endregion

        #region implementation details

        private void LoadPlugin( RegistryKey key, string pluginName, string sFullname, ref string error )
        {
            Type[] pluginTypes;
            Assembly pluginAssembly = null;
            try
            {
                pluginAssembly = Assembly.LoadFrom( sFullname );
            }
            catch( Exception e )
            {
                Trace.WriteLine( "Exception in Assembly.LoadFrom: " + e.ToString() );
                CheckRemovePlugin( key, pluginName, sFullname, "failed to load" );
                error = "Failed to load plugin (assembly)";
                return;
            }

            if ( CheckOldVersionPlugin( key, pluginName, sFullname, pluginAssembly ) )
            {
                return;
            }

            try
            {
                pluginTypes = pluginAssembly.GetExportedTypes();
            }
            catch( Exception e )
            {
                Trace.WriteLine( "Exception in pluginAssembly.GetExportedTypes: " + e.ToString() );
                CheckRemovePlugin( key, pluginName, sFullname, "failed to load" );
                error = "Failed to load plugin (assembly) - no proper types.";
                return;
            }

            int loadedTypesCount = _typesOfPluginInstances.Count;
            // search for IPlugin instances
            foreach( Type aType in pluginTypes )
            {
                if( aType.GetInterface( "IPlugin", false ) != null &&
                    !_typesOfPluginInstances.Contains( aType.FullName ) )
                {
#if READER
                    string fullName = aType.FullName;
                    string thisName =  Core.MainWindow.GetType().FullName;
                    if ( fullName.Length >= 14 && fullName.Substring( 0, 14 ) == thisName.Substring( 0, 14  ) )
                    {
                        int hc = fullName.GetHashCode();
                        if ( hc == 1843295539 ||       // JetBrains.Omea.OutlookPlugin.OutlookPlugin
                             hc == 1214525466 ||       // JetBrains.Omea.Tasks.TasksPlugin
                             hc == -525075978 ||       // JetBrains.Omea.FilePlugin.FileProxy
                             hc == -964430527 ||       // JetBrains.Omea.InstantMessaging.ICQ.ICQPlugin
                             hc == 1907795649 ||       // JetBrains.Omea.InstantMessaging.Miranda.MirandaPlugin
                             hc == -463934733 ||       // JetBrains.Omea.WordDocPlugin.WordDocPlugin
                             hc == -143362765 ||       // JetBrains.Omea.PDFPlugin.PDFPlugin
                             hc == -945932621 )        // JetBrains.Omea.ExcelDocPlugin.ExcelDocPlugin 
                        {
                            return;
                        }
                    }
#endif
                    _typesOfPluginInstances.Add( aType.FullName );
                    try
                    {
                        IPlugin newPlugin = (IPlugin) Activator.CreateInstance( aType );
                        newPlugin.Register();
                        _pluginList.Add( newPlugin );
                        Object[] attrs = aType.GetCustomAttributes( false );
                        foreach( Object attribute in attrs )
                        {
                            if( attribute is PluginDescriptionAttribute )
                            {
                                _pluginsDescriptions.Add( sFullname, (attribute as PluginDescriptionAttribute).Description );
                                _pluginsAuthors.Add( sFullname, (attribute as PluginDescriptionAttribute).Author );
                                break;
                            }
                        }
                    }
                    catch( CancelStartupException )
                    {
                        throw;
                    }
                    catch( Exception e )
                    {
#if DEBUG
                        Core.ReportException( e, false );
#endif
                        error = "Plugin failed to register itself: " + e.Message;
                    }
                }
            }
            if( loadedTypesCount < _typesOfPluginInstances.Count )
            {
                _xmlConfigAssemblies.Add( pluginAssembly );
            }
        }

        private static bool IsStandardPlugin( Assembly assembly )
        {
            object[] products = assembly.GetCustomAttributes( typeof(AssemblyProductAttribute), true );
            object[] companies = assembly.GetCustomAttributes( typeof(AssemblyCompanyAttribute), true );
            if ( products.Length > 0 && companies.Length > 0 )
            {
                AssemblyProductAttribute product = (AssemblyProductAttribute) products [0];
                AssemblyCompanyAttribute company = (AssemblyCompanyAttribute) companies [0];
                if ( company.Company == "JetBrains s.r.o." && product.Product == "Omea" )
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CheckOldVersionPlugin( RegistryKey key, string pluginName, string fileName,
                                                   Assembly pluginAssembly )
        {
            if ( !IsStandardPlugin( pluginAssembly ) )
            {
                return false;
            }

            int pluginBuild = pluginAssembly.GetName().Version.Build;
            int omeaBuild = Assembly.GetExecutingAssembly().GetName().Version.Build;
            if ( pluginBuild != omeaBuild )
            {
                DialogResult dr = MessageBox.Show( Core.MainWindow,
                    "The plugin file '" + fileName + 
                    "' was built for Omea build " + pluginBuild + 
                    " and is incompatible with Omea build " + omeaBuild + 
                    ". Would you like to remove the plugin from the list of registered plugins?",
                    Core.ProductFullName, MessageBoxButtons.YesNo );
                if ( dr == DialogResult.Yes )
                {
                    RemovePlugin( key, pluginName );
                    return true;
                }
            }
            return false;
        }

        public delegate void XmlConfigurationDelegate( Assembly pluginAssembly, XmlNode configNode );

        public void RegisterXmlConfigurationHandler( string section, XmlConfigurationDelegate configDelegate )
        {
            _xmlConfigHandlerHash [section] = configDelegate;            
        }

        internal void LoadXmlConfiguration( Assembly pluginAssembly )
        {
            string[] manifestResources = pluginAssembly.GetManifestResourceNames();
            foreach( string filename in manifestResources )
            {
                if ( filename.ToLower().EndsWith( "plugin.xml" ) )
                {
                    try
                    {
                        Stream stream = pluginAssembly.GetManifestResourceStream( filename );
                        XmlDocument doc = new XmlDocument();
                        doc.Load( stream );
                        XmlNode node = doc.SelectSingleNode( "/omniamea-plugin" );
                        if ( node != null )
                        {
                            LoadXmlConfigurationDocument( pluginAssembly, node );
                        }
                    }
                    catch( Exception e )
                    {
                        Core.ReportException( e, false );
                    }
                }
            }
        }

        private void LoadXmlConfigurationDocument( Assembly pluginAssembly, XmlNode rootNode )
        {
            foreach( XmlNode node in rootNode.ChildNodes )
            {
                XmlConfigurationDelegate handler = (XmlConfigurationDelegate) _xmlConfigHandlerHash [node.Name];
                if ( handler != null )
                {
                    handler( pluginAssembly, node );
                }
                else if ( node.Name == "resource-icons" )
                {
                    ConfigurableIconProvider iconProvider = new ConfigurableIconProvider( pluginAssembly, node );
                    Core.ResourceIconManager.RegisterResourceIconProvider( iconProvider.ResourceTypes, 
                        iconProvider );
                    for( int i=0; i<iconProvider.ResourceTypes.Length; i++ )
                    {
                        Core.ResourceIconManager.RegisterOverlayIconProvider( iconProvider.ResourceTypes [i], 
                            iconProvider );
                    }
                }
            }
        }

#if READER 
        private const string    _csPluginsRegKey = @"SOFTWARE\JetBrains\Omea Reader\Plugins";
#else
        private const string    _csPluginsRegKey = @"SOFTWARE\JetBrains\Omea\Plugins";
#endif
        public const string     _configKey = "Config";
        public const string     _disabledValue = "Disabled";

        private string          _pluginRegistryKey;

        private ArrayList       _pluginList = new ArrayList();
        private HashSet         _loadedAssemblies = new HashSet();
        private ArrayList       _xmlConfigAssemblies = new ArrayList();
        private HashMap         _pluginsAuthors = new HashMap();
        private HashMap         _pluginsDescriptions = new HashMap();
        private HashMap         _resourceUIHandlerHash = new HashMap();
        private HashMap         _resourceDragDropHandlerHash = new HashMap();
        private HashMap         _resourceRenameHandlerHash = new HashMap();
        private HashMap         _resourceDisplayerHash = new HashMap();
        private HashMap         _xmlConfigHandlerHash = new HashMap();
        private HashMap         _streamProviderHash = new HashMap();
        private HashMap         _resourceTextProviders = new HashMap();   // resource type -> ArrayList<ITextIndexProvider>
        private HashMap         _resourceSerializers = new HashMap();     // resource type -> IResourceSerializer
        private HashMap         _resourceDeleters = new HashMap();        // resource type -> IResourceDeleter
        private HashMap         _newspaperProviders = new HashMap();      // resource type -> INewspaperProvider
        private HashSet         _typesOfPluginInstances = new HashSet();  // type of IPlugin instances
        private ArrayList       _genericResourceTextProviders = new ArrayList();
        private ArrayList       _pluginServices = new ArrayList();
        private ArrayList       _viewsConstructors = new ArrayList();
        private CompositeThreadingHandler _threadingHandler = new CompositeThreadingHandler();

        private bool            _cancelStartup = false;
        private string          _startupError;

        #endregion

    }
}