// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collection.Generic;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

using System35;

using JetBrains.Annotations;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceStore;
using JetBrains.UI.Util;
using JetBrains.Util;

using OmniaMea;

namespace JetBrains.Omea.Plugins
{
	/// <summary>
	/// Loads and starts the plugins.
	/// Controls the detection, disabling and enabling of the plugin DLLs.
	/// </summary>
	public class PluginLoader // TODO: make sure the assembly/file name is case-sensitive
	{
		#region Data

		/// <summary>
		/// A regex to check whether the file name is recognized as an Omea Plugin file.
		/// </summary>
		[NotNull]
		public static readonly Regex RegexOmeaPluginFile = new Regex(@"^.*\bOmeaPlugin\b.*(\.dll|\.exe)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

		private static readonly Regex _regexAssemblyNameToPluginDisplayName = new Regex(@"^(?<Before>.*)\.?OmeaPlugin\.?(?<After>.*)$");

		/// <summary>
		/// The list of default plugin folders.
		/// Note: cannot be made static, as <see cref="MyPalStorage.DBPath"/> used for one of the folders might not be yet available when doing static init.
		/// </summary>
		[NotNull]
		public readonly PluginFolder[] PluginFolders = PluginFolder.CreateDefaultFolders();

		/// <summary>
		/// The list of plugin assemblies for which the XML config hasn't been loaded yet.
		/// </summary>
		[NotNull]
		private readonly List<Assembly> _arAssembliesToLoadXmlConfigFrom = new List<Assembly>();

		/// <summary>
		/// Full names of types of the loaded <see cref="IPlugin"/> instances.
		/// Prevent duplicate class names in separate assemblies.
		/// </summary>
		private readonly HashSet<string> _hashLoadedPluginTypes = new HashSet<string>();

		/// <summary>
		/// For the loaded plugins (<see cref="_plugins"/>), tracks the files they were loaded from, along with the possible load-time comments from the plugin loader.
		/// </summary>
		[NotNull]
		private readonly Dictionary<IPlugin, PossiblyPluginFileInfo> _mapPluginFileInfo = new Dictionary<IPlugin, PossiblyPluginFileInfo>();

		/// <summary>
		/// <para>Tracks errors on loading those assemblies that were considered valid Omea plugins and were attempted to be loaded (so their <see cref="PossiblyPluginFileInfo"/> holds a clean record when enumerating the folders), but later failed the runtime checks.</para>
		/// <para>Key: file full name.</para>
		/// <para>Value: error text.</para>
		/// </summary>
		private readonly Dictionary<string, string> _mapPluginLoadRuntimeErrors = new Dictionary<string, string>();

		/// <summary>
		/// The set of handlers for reading and applying the declarative XML config.
		/// </summary>
		[NotNull]
		private readonly Dictionary<string, Action<Assembly, XmlElement>> _mapXmlConfigHandlers = new Dictionary<string, Action<Assembly, XmlElement>>();

		/// <summary>
		/// Loaded plugins.
		/// </summary>
		[NotNull]
		private readonly List<IPlugin> _plugins = new List<IPlugin>();

		/// <summary>
		/// Service provider storage.
		/// </summary>
		[NotNull]
		private readonly List<object> _pluginServices = new List<object>();

		#endregion

		#region Operations

		/// <summary>
		/// Makes a user-friendly name out of the assembly name by removing the �OmeaPlugin� infix.
		/// </summary>
		[NotNull]
		public static string AssemblyNameToPluginDisplayName([NotNull] string sAssemblyName)
		{
			if(sAssemblyName == null)
				throw new ArgumentNullException("sAssemblyName");

			Match match = _regexAssemblyNameToPluginDisplayName.Match(sAssemblyName);
			if(!match.Success)
				return sAssemblyName;

			string before = match.Groups["Before"].Value.Trim('.').Trim().Replace('.', ' ').Replace('_', ' ');
			string after = match.Groups["After"].Value.Trim('.').Trim().Replace('.', ' ').Replace('_', ' ');

			if((before.IsEmpty()) && (after.IsEmpty())) // No extra text could be taken
				return sAssemblyName;

			// Choose the longest
			return before.Length >= after.Length ? before : after;
		}

		/// <summary>
		/// Makes a user-friendly name out of the assembly name by removing the �OmeaPlugin� infix.
		/// </summary>
		[NotNull]
		public static string AssemblyNameToPluginDisplayName([NotNull] FileInfo pluginfile)
		{
			if(pluginfile == null)
				throw new ArgumentNullException("pluginfile");

			return AssemblyNameToPluginDisplayName(Path.GetFileNameWithoutExtension(pluginfile.FullName));
		}

		/// <summary>
		/// Checks if the given file is an Omea plugin DLL.
		/// Does not check whether it resides in one of the Omea Plugins folders or not.
		/// For plugins in the Primary Plugins folder, makes sure they qualify as primary plugins.
		/// Warning! Loads the DLL with CLR! Locks the file!
		/// </summary>
		public static bool IsOmeaPluginDll([NotNull] FileInfo file, [NotNull] out string sError)
		{
			if(file == null)
				throw new ArgumentNullException("file");

			// Exists?
			if(!file.Exists)
			{
				Trace.WriteLine(sError = string.Format("The plugin file �{0}� does not exist.", file.FullName), "Plugins.Loader");
				return false;
			}
			if(file.Directory == null)
			{
				Trace.WriteLine(sError = string.Format("The plugin file �{0}� does not have a parent directory.", file.FullName), "Plugins.Loader");
				return false;
			}

			// File name?
			if(!RegexOmeaPluginFile.IsMatch(file.Name))
			{
				Trace.WriteLine(sError = string.Format("The file �{0}� does not match the Omea plugin name pattern, {1}.", file.FullName, RegexOmeaPluginFile), "Plugins.Loader");
				return false;
			}

			// Try loading the assembly
			// TODO: don't lock the plugin DLL if we're not loading it yet
			Type[] assemblytypes;
			Assembly assembly;
			try
			{
				// Load assembly: Load Context for Primary Plugins, LoadFrom for all others
				// Primary plugins must have its file name equal to the assembly name plus an extension
				assembly = LoadPluginAssembly(file);
				assemblytypes = assembly.GetExportedTypes();
			}
			catch(Exception ex)
			{
				Trace.WriteLine(string.Format("Could not load the plugin file �{1}�. {0}", ex.Message, file.FullName), "Plugins.Loader");
				sError = ex.Message;
				return false;
			}

			// Primary plugin: strong name check
			if((PluginFolder.PrimaryPluginFolder.IsPluginUnderFolder(file)) && (!IsPrimaryAssemblyStrongNameOk(assembly.GetName(), file.FullName, out sError)))
				return false;

			// Search for IPlugin instances
			foreach(Type type in assemblytypes)
			{
				if(IsOmeaPluginType(type))
				{
					sError = ""; // No error
					return true;
				}
			}

			// No IPlugin
			Trace.WriteLine(sError = string.Format("The plugin file �{0}� does not have any plugins inside. Could not find any classes implementing �{1}�.", file.FullName, typeof(IPlugin).AssemblyQualifiedName), "Plugin.Loader");
			return false;
		}

		/// <summary>
		/// Checks whether the given type implements an Omea plugin.
		/// </summary>
		public static bool IsOmeaPluginType([NotNull] Type type)
		{
			if(type == null)
				throw new ArgumentNullException("type");

			return typeof(IPlugin).IsAssignableFrom(type);
		}

		/// <summary>
		/// For a candidate Primary Plugin Assembly, checks that its strong name is OK.
		/// That is, equal to the strong name of the current assembly, including the no-strong-name case.
		/// </summary>
		/// <param name="assnameCandidate">The assembly name of the candidate assembly.</param>
		/// <param name="sFileDisplayName">Filename, used for error reporting only.</param>
		/// <param name="sError">On failure, contains the error message.</param>
		/// <returns>Success flag.</returns>
		public static bool IsPrimaryAssemblyStrongNameOk(AssemblyName assnameCandidate, string sFileDisplayName, out string sError)
		{
			byte[] tokenMy = Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken();
			byte[] tokenCandidate = assnameCandidate.GetPublicKeyToken();
			if((tokenMy != null) && (tokenCandidate == null))
			{
				Trace.WriteLine(sError = string.Format("The plugin file �{0}� is in the primary plugins folder, but does not have the required strong name.", sFileDisplayName), "Plugin.Loader");
				return false;
			}
			if((tokenMy == null) && (tokenCandidate != null))
			{
				Trace.WriteLine(sError = string.Format("The plugin file �{0}� in the primary plugins folder has a strong name, but Omea is built without strong names.", sFileDisplayName), "Plugin.Loader");
				return false;
			}
			if(tokenMy != null) // Both not Null
			{
				bool match = false;
				if(tokenMy.Length == tokenCandidate.Length)
				{
					match = true;
					for(int a = 0; (a < tokenMy.Length) && (match); a++)
						match &= tokenMy[a] == tokenCandidate[a];
				}
				if(!match)
				{
					Trace.WriteLine(string.Format(sError = "The plugin file �{0}� is in the primary plugins folder, but its strong name is wrong.", sFileDisplayName), "Plugin.Loader");
					return false;
				}
			}
			// The remaining case � neither has strong names � is OK (Debug configuration)
			sError = "";
			return true;
		}

		/// <summary>
		/// Loads the plugin assembly, applying either <c>Load</c> or <c>LoadFrom</c> context (see <see cref="PluginFolder.IsPrimary"/>).
		/// </summary>
		public static Assembly LoadPluginAssembly([NotNull] FileInfo file)
		{
			if(file == null)
				throw new ArgumentNullException("file");

			string sPluginName = Path.GetFileNameWithoutExtension(file.FullName);

			// Primary plugin: Load context, we assume that file name agrees to the assembly name
			if(PluginFolder.PrimaryPluginFolder.IsPluginUnderFolder(file))
				return Assembly.Load(sPluginName);

			// Non-primary plugin: LoadFrom context, ensure file name agrees to the assembly name
			Assembly assembly = Assembly.LoadFrom(file.FullName);
			if(assembly.GetName().Name != sPluginName)
				throw new InvalidOperationException(StringEx.FormatQuoted("The plugin assembly file {0} has an assembly name {1} that does not agree to the plugin name {2}.", file.FullName, assembly.GetName().Name, sPluginName));
			return assembly;
		}

		/// <summary>
		/// Collects all of the assembly files eligible for loading plugins from.
		/// Duplicates are resolved so that high-priorities go first.
		/// </summary>
		[NotNull]
		public List<PossiblyPluginFileInfo> GetAllPluginFiles()
		{
			var arAllPlugins = new List<PossiblyPluginFileInfo>();
			var pluginnames = new HashSet<string>();

			// Discover plugin files, skip disabled
			foreach(PluginFolder folder in PluginFolders)
			{
				foreach(PossiblyPluginFileInfo file in folder.GetPluginFiles())
				{
					// Higher priority goes first, ignore subsequent duplicates (applies to plugins only)
					// Non-plugins are always added
					if((!file.IsPlugin) || (pluginnames.Add(Path.GetFileNameWithoutExtension(file.File.FullName))))
						arAllPlugins.Add(file);
					else
					{
						// Duplicate
						PossiblyPluginFileInfo fileNot = PossiblyPluginFileInfo.CreateNo(file.Folder, file.File, "There was another plugin with the same assembly name in the same or a higher-priority folder.");
						arAllPlugins.Add(fileNot);
						Trace.WriteLine(fileNot.ToString(), "Plugin.Loader");
					}
				}
			}
			return arAllPlugins;
		}

		/// <summary>
		/// Gets the list of plugins currently loaded in the application.
		/// </summary>
		public IPlugin[] GetLoadedPlugins()
		{
			return _plugins.ToArray();
		}

		/// <summary>
		/// For a currently loaded plugin (see <see cref="GetLoadedPlugins"/>), gets its file of origin information.
		/// </summary>
		[NotNull]
		public PossiblyPluginFileInfo GetPluginFileInfo([NotNull] IPlugin plugin)
		{
			if(plugin == null)
				throw new ArgumentNullException("plugin");
			PossiblyPluginFileInfo retval;
			if(!_mapPluginFileInfo.TryGetValue(plugin, out retval))
				throw new InvalidOperationException(string.Format("There is no information available for the plugin {0}, as it has not been loaded by PluginLoader.", plugin.GetType().AssemblyQualifiedName.QuoteIfNeeded()));
			return retval;
		}

		/// <summary>
		/// For those plugins whose <see cref="PossiblyPluginFileInfo"/> says <see cref="PossiblyPluginFileInfo.IsPlugin"/> <c>True</c> when enumerating folders, but who had a runtime error at load time, returns that runtime error.
		/// Otherwise, returns <c>""</c> (for both successful plugins and random non-plugin <paramref name="fi"/>).
		/// </summary>
		[NotNull]
		public string GetPluginLoadRuntimeError([NotNull] FileInfo fi)
		{
			if(fi == null)
				throw new ArgumentNullException("fi");

			return _mapPluginLoadRuntimeErrors.TryGetValue(fi.FullName) ?? "";
		}

		public object GetPluginService(Type serviceType)
		{
			// scan last registered services first
			for(int i = _pluginServices.Count - 1; i >= 0; i--)
			{
				object service = _pluginServices[i];
				if(serviceType.IsInstanceOfType(service))
					return service;
			}
			return null;
		}

		/// <summary>
		/// Looks up Omea Plugin assembly files, loads them, instantiates the plugin classes, calls their <see cref="IPlugin.Register"/>, all on the calling thread.
		/// </summary>
		public void LoadPlugins()
		{
			var disabledplugins = new DisabledPlugins();

			// Collect to get the full count for the progress before we start loading the DLLs/types
			var plugins = new List<PossiblyPluginFileInfo>();
			foreach(PossiblyPluginFileInfo file in GetAllPluginFiles())
			{
				if(!file.IsPlugin)
					continue; // Need plugins only

				// Disabled plugin? Check by the DLL name
				if(!disabledplugins.Contains(Path.GetFileNameWithoutExtension(file.File.FullName)))
					plugins.Add(file);
			}

			// Load!
			var progressWindow = (SplashScreen)Core.ProgressWindow;
			for( int nFile = 0; nFile < plugins.Count; nFile++ )
			{
				if( progressWindow != null )
					progressWindow.UpdateProgress((nFile + 1) * 100 / plugins.Count, Stringtable.LoadingPluginsProgressMessage, null); // Progress after the current file before we process it (at the beginning, accomodate for collecting the files; at the and, let see the 100% fill for some time)

				// Try loading
				string sError;
				if( !LoadPlugins_LoadFile( plugins[ nFile ], out sError ))
				{
					// Failed, report the error
					_mapPluginLoadRuntimeErrors[ plugins[nFile].File.FullName ] = sError; // Overwrite old

                    Core.ReportBackgroundException( new ApplicationException( sError ) );

					// Disable the failed plugin?
					LoadPlugins_SuggestDisable(plugins[nFile].File, disabledplugins, sError, progressWindow);
				}
				else
					_mapPluginLoadRuntimeErrors.Remove(plugins[nFile].File.FullName); // Clean the error record
			}

			// Apply declarative plugin settings
			LoadPlugins_XmlConfiguration();

			// Types without supporting plugins
			LoadPlugins_MarkUnloadedResourceTypes();
		}

		public void RegisterPluginService(object pluginService)
		{
			_pluginServices.Add(pluginService);
		}

		public void RegisterXmlConfigurationHandler(string section, Action<Assembly, XmlElement> configDelegate)
		{
			_mapXmlConfigHandlers[section] = configDelegate;
		}

		/// <summary>
		/// Calls <see cref="IPlugin.Shutdown"/> on each of the plugins, on the calling thread.
		/// </summary>
		/// <param name="bShowProgress">Whether to report to the progress window.</param>
		public void ShutdownPlugins(bool bShowProgress)
		{
			IProgressWindow window = Core.ProgressWindow;
			foreach(IPlugin plugin in _plugins)
			{
				if((bShowProgress) && (window != null))
					window.UpdateProgress(0, string.Format(Stringtable.StoppingPluginProgressMessage, plugin.GetType().Name), null);
				try
				{
					plugin.Shutdown();
				}
				catch(Exception ex)
				{
					Core.ReportException(ex, false);
				}
			}
		}

		/// <summary>
		/// Invokes <see cref="IPlugin.Startup"/> for each of the loaded plugins, on the Resource thread, sync.
		/// </summary>
		public void StartupPlugins()
		{
			IProgressWindow window = Core.ProgressWindow;
			for(int a = 0; a < _plugins.Count; a++)
			{
				IPlugin plugin = _plugins[a];
				Trace.WriteLine("Starting plugin " + plugin.GetType().Name);

				if(window != null)
					window.UpdateProgress((a + 1) * 100 / _plugins.Count, Stringtable.StartingPluginsProgressMessage, null);

				// Run on another thread
				string sStartupError = null;
				bool bCancelStartup = false;
				IPlugin pluginConst = plugin; // Precaution: don't access modified closure
				Core.ResourceAP.RunJob(string.Format(Stringtable.JobStartingPlugin, plugin.GetType().Name), delegate
				{
					try
					{
						pluginConst.Startup();
					}
					catch(CancelStartupException)
					{
						Trace.WriteLine(string.Format("Starting Plugin �{0}�: CancelStartupException.", pluginConst.GetType().AssemblyQualifiedName), "Plugin.Loader");
						bCancelStartup = true;
					}
					catch(Exception ex)
					{
						sStartupError = ex.Message;
					}
				});

				// Process results on home thread
				if( bCancelStartup )
					throw new CancelStartupException();

                if( sStartupError != null )
					Trace.WriteLine(string.Format("Error Starting Plugin �{0}�. {1}", plugin.GetType().AssemblyQualifiedName, sStartupError));
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Whenever a plugin fails to load, suggests disabling it.
		/// </summary>
		private static void LoadPlugins_SuggestDisable([NotNull] FileInfo file, [NotNull] DisabledPlugins disabledplugins, [NotNull] string sError, [CanBeNull] IWin32Window owner)
		{
			if(file == null)
				throw new ArgumentNullException("file");
			if(disabledplugins == null)
				throw new ArgumentNullException("disabledplugins");
			if(sError.IsEmpty())
				throw new ArgumentNullException("sError");

			// Plugin name, error text, question
			var sb = new StringBuilder();
			string displayname = AssemblyNameToPluginDisplayName(file);
			sb.AppendFormat(Stringtable.MessageBoxPluginCouldNotBeLoaded, displayname);
			sb.AppendLine();
			sb.AppendLine();
			sb.AppendLine(sError);
			sb.AppendLine();
			sb.AppendFormat(Stringtable.MessageBoxDisableFailedPlugin, displayname);

			// Confirm
			if(MessageBox.Show(owner, sb.ToString(), Stringtable.MessageBoxFailedPluginTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
				disabledplugins.Add(Path.GetFileNameWithoutExtension(file.FullName)); // Disable
		}

		/// <summary>
		/// Looks for �Plugin.xml� resources in the assembly, loads the XML configuration from them.
		/// </summary>
		internal void LoadXmlConfiguration(Assembly pluginAssembly)
		{
			string[] manifestResources = pluginAssembly.GetManifestResourceNames();
			foreach(string filename in manifestResources)
			{
				if(filename.ToLower().EndsWith("plugin.xml"))
				{
					try
					{
						var doc = new XmlDocument();
						doc.Load(pluginAssembly.GetManifestResourceStream(filename));
						XmlNode node = doc.SelectSingleNode("/omniamea-plugin");
						if(node != null)
							LoadXmlConfiguration_Document(pluginAssembly, node);
					}
					catch(Exception ex)
					{
						Core.ReportException(ex, false);
					}
				}
			}
		}

		/// <summary>
		/// Loads the plugin assembly and creates one or more <see cref="IPlugin"/> types from it.
		/// </summary>
		private bool LoadPlugins_LoadFile([NotNull] PossiblyPluginFileInfo file, [NotNull] out string sError)
		{
			if(file.File == null)
				throw new ArgumentNullException("file");

			// Here the plugin DLL is loaded by CLR
			// IPlugin instances are searched for, primary plugins checked for authenticity
			if(!IsOmeaPluginDll(file.File, out sError))
				return false;

			Trace.WriteLine(string.Format("Loading plugin file �{0}�.", file.File.FullName), "Plugin.Loader");

			// Get the types from the assembly, to look for plugins
			Assembly assembly;
			Type[] types;
			try
			{
				assembly = LoadPluginAssembly(file.File);
				types = assembly.GetExportedTypes();
			}
			catch(Exception ex)
			{
				sError = ex.Message;
				return false;
			}

			// Load each IPlugin
			int nLoadedTypes = 0;
			foreach(Type type in types)
			{
				if(IsOmeaPluginType(type))
				{
					string sTypeError;
					if(LoadPlugins_LoadFile_LoadType(type, file, out sTypeError))
						nLoadedTypes++;
					else
						sError += sTypeError + " ";
				}
			}

			// Any plugins loaded? Read declarative XML config
			if(nLoadedTypes > 0)
				_arAssembliesToLoadXmlConfigFrom.Add(assembly);

			// If we have picked the DLL as a plugin, then it must have at least one IPlugin type
			return nLoadedTypes > 0;
		}

		/// <summary>
		/// Tries creating and registering the plugin instance from the given class.
		/// </summary>
		private bool LoadPlugins_LoadFile_LoadType([NotNull] Type type, PossiblyPluginFileInfo file, out string sError)
		{
			if(type == null)
				throw new ArgumentNullException("type");

			// Duplicate with someone?
			if(!_hashLoadedPluginTypes.Add(type.FullName))
			{
				sError = string.Format("The plugin class �{0}� has already been loaded from another assembly.", type.FullName);
				return false;
			}

			// Create and register
			try
			{
				var plugin = (IPlugin)Activator.CreateInstance(type);
				plugin.Register();
				_plugins.Add(plugin);
				_mapPluginFileInfo.Add(plugin, file);
			}
			catch(CancelStartupException) // Special exception to abort the startup
			{
				throw;
			}
			catch(Exception ex)
			{
#if DEBUG
				Core.ReportException(ex, false);
#endif
				sError = "The plugin failed to register itself. " + ex.Message;
				return false;
			}

			sError = "";
			return true;
		}

		private void LoadPlugins_MarkUnloadedResourceTypes()
		{
			var pluginNames = new ArrayList();
			foreach(IPlugin plugin in _plugins)
				pluginNames.Add(plugin.GetType().FullName);
			MyPalStorage.Storage.MarkHiddenResourceTypes((string[])pluginNames.ToArray(typeof(string)));
		}

		private void LoadPlugins_XmlConfiguration()
		{
			foreach(Assembly assembly in _arAssembliesToLoadXmlConfigFrom)
				LoadXmlConfiguration(assembly);
			_arAssembliesToLoadXmlConfigFrom.Clear();
		}

		private void LoadXmlConfiguration_Document(Assembly pluginAssembly, XmlNode rootNode)
		{
			foreach(XmlElement xmlElement in rootNode.ChildNodes)
			{
				Action<Assembly, XmlElement> handler;
				if(!_mapXmlConfigHandlers.TryGetValue(xmlElement.Name, out handler))
					continue;
				handler(pluginAssembly, xmlElement);
			}
		}

		#endregion

		#region CancelStartupException Type

		/// <summary>
		/// A service exception that aborts the plugins startup sequence.
		/// </summary>
		public class CancelStartupException : Exception
		{
			#region Init

			public CancelStartupException()
				: base("Omea startup has been cancelled.")
			{
			}

			#endregion
		}

		#endregion

		#region DisabledPlugins Type

		/// <summary>
		/// Maintains the colletion of disabled plugin DLL assembly names. Supports persisting.
		/// </summary>
		public class DisabledPlugins : ICollection<string>
		{
			#region Data

			/// <summary>
			/// Separates serialized assembly names in the settings entry.
			/// Safe to use, as we require that assembly name math its file name, and this char is illegal in a file name.
			/// </summary>
			private static readonly char EntrySeparator = '|';

			private static readonly string SettingsSectionName = "Plugins";

			private static readonly string SettingsValueName = "DisabledPlugins";

			private readonly HashSet<string> _storage = Load();

			#endregion

			#region Operations

			/// <summary>
			/// Checks if the plugins from this assembly are disabled.
			/// It's assumed that assembly name and file name agree.
			/// </summary>
			public bool Contains(Assembly assembly)
			{
				return Contains(assembly.GetName().Name);
			}

			/// <summary>
			/// Checks if the plugins from this file are disabled.
			/// It's assumed that assembly name and file name agree.
			/// </summary>
			public bool Contains(FileInfo file)
			{
				return Contains(Path.GetFileNameWithoutExtension(file.FullName));
			}

			#endregion

			#region Implementation

			/// <summary>
			/// Loads the collection from app settings.
			/// </summary>
			private static HashSet<string> Load()
			{
				var result = new HashSet<string>();

				string list = Core.SettingStore.ReadString(SettingsSectionName, SettingsValueName, "");
				foreach(string entry in list.Split(EntrySeparator))
					result.Add(entry);
				return result;
			}

			/// <summary>
			/// Saves the current collection into the app settings.
			/// </summary>
			private void Save()
			{
				Core.SettingStore.WriteString(SettingsSectionName, SettingsValueName, string.Join(EntrySeparator.ToString(), new List<string>(_storage).ToArray()));
			}

			#endregion

			#region ICollection<string> Members

			public void Add(string item)
			{
				_storage.Add(item);
				Save();
			}

			public void Clear()
			{
				_storage.Clear();
				Save();
			}

			/// <summary>
			/// Checks if the plugins from this assembly/file are disabled.
			/// It's assumed that assembly name and file name agree.
			/// </summary>
			public bool Contains(string item)
			{
				return _storage.Contains(item);
			}

			public void CopyTo(string[] array, int arrayIndex)
			{
				_storage.CopyTo(array, arrayIndex);
			}

			public IEnumerator<string> GetEnumerator()
			{
				return ((ICollection<string>)_storage).GetEnumerator();
			}

			public bool Remove(string item)
			{
				if(!_storage.Remove(item))
					return false;
				Save();
				return true;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<string>)this).GetEnumerator();
			}

			public int Count
			{
				get
				{
					return _storage.Count;
				}
			}

			public bool IsReadOnly
			{
				get
				{
					return false;
				}
			}

			#endregion
		}

		#endregion

		#region PluginFolder Type

		/// <summary>
		/// Describes a folder from which plugins are collected.
		/// </summary>
		public class PluginFolder
		{
			#region Data

			/// <summary>
			/// Gets the folder with primary Omea plugins.
			/// </summary>
			[NotNull]
			public static readonly PluginFolder PrimaryPluginFolder = new PluginFolder("Omea Core Plugins", "<OmeaInstallDir>", new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory, true);

			/// <summary>
			/// Actual folder path on the local file system.
			/// </summary>
			[NotNull]
			public readonly DirectoryInfo Directory;

			/// <summary>
			/// <para>Gets whether this folder contains primary plugins only.</para>
			/// <para>Primary plugins are stongly-named with the same key as this DLL, and are loaded using the Load Context, instead of the LoadFrom context.</para>
			/// <para>In non-primary folders, each subfolder should also be checked for plugins.</para>
			/// </summary>
			public readonly bool IsPrimary;

			/// <summary>
			/// Logical machine-independent representation of the location, eg �<c>RoamingAppData</c>� instead of the exact path.
			/// </summary>
			[NotNull]
			public readonly string Location;

			/// <summary>
			/// Human-readable name of the folder.
			/// </summary>
			[NotNull]
			public readonly string Name;

			/// <summary>
			/// Valid extensions for plugin assemblies (without a leading dot).
			/// </summary>
			[NotNull]
			private readonly IList<string> _arPluginAssemblyExtensions = new[] {"dll", "exe"};

			#endregion

			#region Init

			/// <summary>
			/// Fills in the entry. Use <see cref="PluginLoader.PluginFolders"/> to access the existing entries.
			/// </summary>
			protected PluginFolder([NotNull] string name, [NotNull] string location, [NotNull] DirectoryInfo directory, bool isprimary)
			{
				if(name == null)
					throw new ArgumentNullException("name");
				if(location == null)
					throw new ArgumentNullException("location");
				if(directory == null)
					throw new ArgumentNullException("directory");

				Name = name;
				Location = location;
				Directory = directory;
				IsPrimary = isprimary;
			}

			#endregion

			#region Operations

			/// <summary>
			/// Fills the list of default folders to look for plugins.
			/// </summary>
			public static PluginFolder[] CreateDefaultFolders()
			{
				var folders = new List<PluginFolder>();

				// 1. (P.JB) Primary
				folders.Add(PrimaryPluginFolder);

				// 2. (P.DB) Database
				CreateDefaultFolders_DbPath(folders.Add);

				// 3. (P.LA) Per-user local
				CreateDefaultFolders_SpecialFolder("User Local Plugins", "LocalAppData", Environment.SpecialFolder.LocalApplicationData, folders.Add);

				// 4. (P.RA) Per-user roaming
				CreateDefaultFolders_SpecialFolder("User Roaming Plugins", "RoamingAppData", Environment.SpecialFolder.ApplicationData, folders.Add);

				// 5. (P.AU) Per-machine user
				CreateDefaultFolders_SpecialFolder("All Users Plugins", "AllUsersAppData", Environment.SpecialFolder.CommonApplicationData, folders.Add);

				// 6. (P.OB) Plugins under Omea Binaries
				folders.Add(new PluginFolder("Administrative Plugins", "<OmeaInstallDir>/Plugins[/*]", new DirectoryInfo(Path.Combine(PrimaryPluginFolder.Directory.FullName, "Plugins")), false));

				return folders.ToArray();
			}

			/// <summary>
			/// Gets the potential plugin assemblies, as collected from the <see cref="Directory"/>, if it exists, and its subdirectories, if applicable for this folder type (see <see cref="IsPrimary"/>).
			/// The files are recolleted on each call.
			/// </summary>
			[NotNull]
			public IEnumerable<PossiblyPluginFileInfo> GetPluginFiles()
			{
				if(!Directory.Exists)
					return new PossiblyPluginFileInfo[] {};

				// List of folders to check
				var directories = new List<DirectoryInfo> {Directory};

				// In non-primary folders, also check subfolders: complex plugins might not want to mix their files up with others
				if(!IsPrimary)
				{
					foreach(DirectoryInfo directory in Directory.GetDirectories())
					{
						if((directory.Attributes & FileAttributes.Hidden) == 0)
							directories.Add(directory);
					}
				}

				var files = new List<PossiblyPluginFileInfo>();
				PossiblyPluginFileInfo pluginfileinfo;

				// Look for the regex-matching files
				foreach(DirectoryInfo directory in directories)
				{
					foreach(FileInfo file in directory.GetFiles())
					{
						if(!_arPluginAssemblyExtensions.Contains(file.Extension.Trim('.')))
						{
							// Note: do not report such items at all, to avoid noise
							Trace.WriteLine(PossiblyPluginFileInfo.CreateNo(this, file, "File extension mismatch, assembly expected."), "Plugin.Loader");
							continue;
						}
						if((file.Attributes & FileAttributes.Hidden) != 0) // Skip hidden files
							pluginfileinfo = PossiblyPluginFileInfo.CreateNo(this, file, "The file is hidden.");
						else if(!RegexOmeaPluginFile.IsMatch(file.Name)) // Apply file name filter
							pluginfileinfo = PossiblyPluginFileInfo.CreateNo(this, file, "The file name does not include �OmeaPlugin�.");
						else
							pluginfileinfo = PossiblyPluginFileInfo.CreateYes(this, file);

						// Note: here we do not check for IPlugin types and such, as it will cause loading the DLLs without the progress (we don't know the total count yet)

						// Pick
						files.Add(pluginfileinfo);
						Trace.WriteLine(pluginfileinfo.ToString(), "Plugin.Loader");
					}
				}

				return files;
			}

			/// <summary>
			/// Checks whether the plugin file is under the given plugin folder.
			/// </summary>
			public bool IsPluginUnderFolder([NotNull] FileInfo pluginfile)
			{
				if(pluginfile == null)
					throw new ArgumentNullException("pluginfile");

				DirectoryInfo plugindir = pluginfile.Directory;
				if(plugindir == null)
					return false;

				// Directly under the folder (the only option for primaries)
				if(plugindir.FullName == Directory.FullName)
					return true;

				// In a subfolder (non-primaries only)
				if(!IsPrimary)
				{
					DirectoryInfo plugindirparent = plugindir.Parent;
					if((plugindirparent != null) && (plugindirparent.FullName == Directory.FullName))
						return true;
				}

				return false;
			}

			#endregion

			#region Implementation

			/// <summary>
			/// Helper for creating the plugin folder record for the in-database-path plugins.
			/// </summary>
			/// <param name="funcOnSuccess">Executed to acknowledge the result on success, skipped on failure.</param>
			private static void CreateDefaultFolders_DbPath(Action<PluginFolder> funcOnSuccess)
			{
				string sDbFolder = MyPalStorage.DBPath;
				if(string.IsNullOrEmpty(sDbFolder))
					return;

				var pluginfolder = new PluginFolder("Database Plugins", "<DatabaseDir>/Plugins[/*]", new DirectoryInfo(Path.Combine(Path.GetFullPath(sDbFolder), "Plugins")), false);

				// �Return� the result
				funcOnSuccess(pluginfolder);
			}

			/// <summary>
			/// Helper for creating plugin folder records under system special folders.
			/// Paths: <c>{SpecialFolder}/JetBrains/Omea/Plugins</c>.
			/// </summary>
			/// <param name="sFriendlyName"><see cref="Name"/>.</param>
			/// <param name="sSpecialFolderTitle">Part of the <see cref="Location"/> that identifies the special folder.</param>
			/// <param name="specialfolder">The system folder to look under.</param>
			/// <param name="funcOnSuccess">Executed to acknowledge the result on success, skipped on failure.</param>
			private static void CreateDefaultFolders_SpecialFolder(string sFriendlyName, string sSpecialFolderTitle, Environment.SpecialFolder specialfolder, Action<PluginFolder> funcOnSuccess)
			{
				// Folder exists on disk?
				string sSpecialFolder = Environment.GetFolderPath(specialfolder);
				if(string.IsNullOrEmpty(sSpecialFolder))
					return; // No such folder

				// Create entry
				var pluginfolder = new PluginFolder(sFriendlyName, string.Format("<{0}>/JetBrains/Omea/Plugins[/*]", sSpecialFolderTitle), new DirectoryInfo(Path.Combine(Path.Combine(Path.Combine(sSpecialFolder, "JetBrains"), "Omea"), "Plugins")), false);

				// �Return� the result
				funcOnSuccess(pluginfolder);
			}

			#endregion
		}

		#endregion

		#region PossiblyPluginFileInfo Type

		/// <summary>
		/// Lists assemblies whose location makes them eligible for plugin probing, specifies whether the assembly qualifies as a plugin.
		/// </summary>
		public struct PossiblyPluginFileInfo
		{
			#region Data

			/// <summary>
			/// Path to the file.
			/// </summary>
			public FileInfo File;

			/// <summary>
			/// The folder under which the plugin file was collected.
			/// </summary>
			public PluginFolder Folder;

			/// <summary>
			/// Whether the file qualifies as an Omea plugin.
			/// </summary>
			public bool IsPlugin;

			/// <summary>
			/// A reason for the <see cref="IsPlugin"/> flag. Optional.
			/// Mainly needed to tell why the DLL was not taken as a plugin.
			/// </summary>
			public string Reason;

			#endregion

			#region Init

			public PossiblyPluginFileInfo([NotNull] PluginFolder folder, [NotNull] FileInfo file, bool bIsPlugin, [CanBeNull] string sReason)
			{
				if(folder == null)
					throw new ArgumentNullException("folder");
				if(file == null)
					throw new ArgumentNullException("file");
				Folder = folder;
				File = file;
				IsPlugin = bIsPlugin;
				Reason = sReason;
			}

			#endregion

			#region Operations

			/// <summary>
			/// Creates an item that is NOT an Omea Plugin.
			/// </summary>
			public static PossiblyPluginFileInfo CreateNo([NotNull] PluginFolder folder, [NotNull] FileInfo file, [CanBeNull] string sReason)
			{
				return new PossiblyPluginFileInfo(folder, file, false, sReason);
			}

			/// <summary>
			/// Creates an item that qualifies as an Omea Plugin.
			/// </summary>
			public static PossiblyPluginFileInfo CreateYes([NotNull] PluginFolder folder, [NotNull] FileInfo file)
			{
				return new PossiblyPluginFileInfo(folder, file, true, null);
			}

			#endregion

			#region Overrides

			public override string ToString()
			{
				if(File == null)
					return "<Undefined>";
				var sb = new StringBuilder();
				sb.AppendFormat("The file {0}", File.FullName.QuoteIfNeeded());
				sb.Append(IsPlugin ? " is an Omea Plugin" : " is not an Omea Plugin");
				sb.Append('.');
				if(!Reason.IsEmpty())
				{
					sb.Append(' ');
					sb.Append(Reason);
				}
				return sb.ToString();
			}

			#endregion
		}

		#endregion
	}
}
