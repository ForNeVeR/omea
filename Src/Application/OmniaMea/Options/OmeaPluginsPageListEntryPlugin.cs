// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Documents;
using System.Windows.Media;
using JetBrains.Annotations;
using JetBrains.Omea.OpenAPI;
using OmniaMea;

namespace JetBrains.Omea.Plugins
{
	/// <summary>
	/// Omea Plugin Page, list view entries that represent plugin assemblies.
	/// </summary>
	internal class OmeaPluginsPageListEntryPlugin : OmeaPluginsPageListEntry
	{
		#region Data

		[NotNull]
		private OmeaPluginsPageAssemblyInfo _assemblyinfo;

		[NotNull]
		private readonly Func<Assembly> _funcLoadAssembly;

		/// <summary>
		/// Even though <see cref="AssemblyInfo"/> is not <see cref="OmeaPluginsPageAssemblyInfo.IsLoaded"/>, we're currently in progress of loading it.
		/// UI-thread-only access.
		/// </summary>
		private bool _isBusyLoading;

		private readonly PluginLoader.PossiblyPluginFileInfo _pluginfileinfo;

		private readonly string _sRuntimeLoadError;

		#endregion

		#region Init

		/// <summary>
		/// Ctor.
		/// </summary>)
		/// <param name="sPluginAssemblyName">Raw name of the plugin assembly.</param>
		/// <param name="funcLoadAssembly">A funtion to load the assembly on-demand.</param>
		/// <param name="disabledplugins">A cached <see cref="PluginLoader.DisabledPlugins"/> instance for getting the initial <see cref="OmeaPluginsPageListEntry.IsEnabled"/> value.</param>
		/// <param name="pluginfileinfo">Info about the file the plugin were loaded from.</param>
		/// <param name="sRuntimeLoadError">If the plugin failed to be loaded at runtime, records the load error.</param>
		public OmeaPluginsPageListEntryPlugin([NotNull] string sPluginAssemblyName, [NotNull] Func<Assembly> funcLoadAssembly, PluginLoader.PossiblyPluginFileInfo pluginfileinfo, [NotNull] string sRuntimeLoadError, [NotNull] PluginLoader.DisabledPlugins disabledplugins)
		{
			if(sPluginAssemblyName == null)
				throw new ArgumentNullException("sPluginAssemblyName");
			if(funcLoadAssembly == null)
				throw new ArgumentNullException("funcLoadAssembly");
			if(disabledplugins == null)
				throw new ArgumentNullException("disabledplugins");
			if(sRuntimeLoadError == null)
				throw new ArgumentNullException("sRuntimeLoadError");

			_funcLoadAssembly = funcLoadAssembly;
			_pluginfileinfo = pluginfileinfo;
			_sRuntimeLoadError = sRuntimeLoadError;
			_assemblyinfo = OmeaPluginsPageAssemblyInfo.CreateNotLoaded(sPluginAssemblyName, _pluginfileinfo, _sRuntimeLoadError);
			_bIsEnabledInitially = !disabledplugins.Contains(sPluginAssemblyName);
			IsEnabled = _bIsEnabledInitially;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Plugin file origin data.
		/// </summary>
		public PluginLoader.PossiblyPluginFileInfo PluginFileInfo
		{
			get
			{
				return _pluginfileinfo;
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Applies the edited changes.
		/// </summary>
		public bool Commit(PluginLoader.DisabledPlugins disabledplugins)
		{
			// Not changed?
			if(IsEnabled == _bIsEnabledInitially)
				return false;

			// Apply the new state
			if(IsEnabled)
				disabledplugins.Remove(AssemblyInfo.PluginAssemblyName);
			else
				disabledplugins.Add(AssemblyInfo.PluginAssemblyName);

			// Mark as non-dirty
			_bIsEnabledInitially = IsEnabled;

			return true; // Were changes
		}

		/// <summary>
		/// Loads the <see cref="AssemblyInfo"/>, if not loaded yet.
		/// </summary>
		/// <param name="funcOnDone">Executed after loading (or skipping) the item, on the UI thread.</param>
		public void Load([NotNull] Action funcOnDone)
		{
			// Don't start second time
			if((AssemblyInfo.IsLoaded) || (_isBusyLoading))
			{
				funcOnDone();
				return;
			}

			_isBusyLoading = true;

			// Load async
			Assembly assembly = null;
			List<Type> plugintypes = null;

			Action funcPreloadOnOtherThread = delegate
			{
				assembly = _funcLoadAssembly();

				// Plugin types in this assembly
				plugintypes = new List<Type>();
				foreach(Type type in assembly.GetExportedTypes())
				{
					if(PluginLoader.IsOmeaPluginType(type))
						plugintypes.Add(type);
				}
			};

			Action funcRenderOnUiThread = delegate
			{
				_isBusyLoading = false;

				// Create visual objects on the UI thread
				try
				{
					if((assembly != null) && (plugintypes != null))
						AssemblyInfo = OmeaPluginsPageAssemblyInfo.CreateFromAssembly(assembly, plugintypes, PluginFileInfo, _sRuntimeLoadError);
				}
				catch(Exception ex)
				{
					Core.ReportBackgroundException(ex);
				}
				funcOnDone();
			};

			// Schedulle
			Core.NetworkAP.QueueJob(Stringtable.JobLoadPluginAssemblyInfo, delegate
			{
				try
				{
					funcPreloadOnOtherThread();
				}
				finally
				{
					// Commit on the home thread
					Core.UserInterfaceAP.QueueJob(Stringtable.JobLoadPluginAssemblyInfo, funcRenderOnUiThread);
				}
			});
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Contains the info that has to be loaded from the assembly. First, a stub, then it's replaced with real info. Hence <see cref="INotifyPropertyChanged"/>.
		/// </summary>
		[NotNull]
		protected OmeaPluginsPageAssemblyInfo AssemblyInfo
		{
			get
			{
				return _assemblyinfo;
			}
			set
			{
				_assemblyinfo = value;
				// AssemblyInfo has changed
				FirePropertyChanged("AssemblyInfo");

				// Presentation has changed
				FirePropertyChanged("Description");
				FirePropertyChanged("Icon");
				FirePropertyChanged("IsPrimary");
				FirePropertyChanged("Title");
			}
		}

		#endregion

		#region Overrides

		public override FlowDocument Description
		{
			get
			{
				return AssemblyInfo.Description;
			}
		}

		public override ImageSource Icon
		{
			get
			{
				return AssemblyInfo.PluginAssemblyIcon;
			}
		}

		public override bool? IsPrimary
		{
			get
			{
				return AssemblyInfo.IsLoaded ? AssemblyInfo.IsPrimary : (bool?)null;
			}
		}

		public override bool SupportsIsEnabled
		{
			get
			{
				return true;
			}
		}

		public override string Title
		{
			get
			{
				return AssemblyInfo.PluginAssemblyDisplayName;
			}
		}

		#endregion
	}
}
