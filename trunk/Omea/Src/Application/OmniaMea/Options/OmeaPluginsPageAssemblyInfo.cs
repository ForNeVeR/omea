using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

using GUIControls.RichText;

using JetBrains.Annotations;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Avalon;
using JetBrains.Util;

using OmniaMea;

namespace JetBrains.Omea.Plugins
{
	/// <summary>
	/// Contains the <see cref="OmeaPluginsPageListEntry"/> info that has to be loaded from an assembly.
	/// </summary>
	internal class OmeaPluginsPageAssemblyInfo
	{
		#region Data

		/// <summary>
		/// The assembly, if already loaded.
		/// </summary>
		[CanBeNull]
		private readonly Assembly _assembly;

		/// <summary>
		/// Lazy-init <see cref="Description"/>.
		/// </summary>
		[CanBeNull]
		private FlowDocument _description;

		private readonly bool _isLoaded;

		private readonly bool _isPrimary;

		private string _PluginAssemblyDisplayName;

		private ImageSource _PluginAssemblyIcon;

		private readonly PluginLoader.PossiblyPluginFileInfo _pluginfileinfo;

		private readonly PluginDescription[] _plugins;

		private readonly string _sPluginAssemblyName;

		private readonly string _sRuntimeLoadError;

		#endregion

		#region Init

		/// <summary>
		/// Not-loaded-mode ctor.
		/// </summary>
		/// <param name="sAssemblyName">The assembly name. Mandatory. For display needs only.</param>
		/// <param name="pluginfileinfo">Plugin file of origin.</param>
		/// <param name="sRuntimeLoadError">If the plugin failed to be loaded at runtime, records the load error.</param>
		private OmeaPluginsPageAssemblyInfo([NotNull] string sAssemblyName, PluginLoader.PossiblyPluginFileInfo pluginfileinfo, [NotNull] string sRuntimeLoadError)
		{
			if(sAssemblyName.IsEmpty())
				throw new ArgumentNullException("sAssemblyName");

			_sPluginAssemblyName = sAssemblyName;
			_pluginfileinfo = pluginfileinfo;
			_sRuntimeLoadError = sRuntimeLoadError;
		}

		/// <summary>
		/// Loaded-mode ctor.
		/// </summary>
		/// <param name="assembly">The assembly, if loaded.</param>
		/// <param name="plugintypes">Preloaded plugin types to create the descriptions from.</param>
		/// <param name="pluginfileinfo">Plugin file origin information.</param>
		/// <param name="sRuntimeLoadError">If the plugin failed to be loaded at runtime, records the load error.</param>
		private OmeaPluginsPageAssemblyInfo([NotNull] Assembly assembly, [NotNull] IList<Type> plugintypes, PluginLoader.PossiblyPluginFileInfo pluginfileinfo, [NotNull] string sRuntimeLoadError)
			: this(assembly.GetName().Name, pluginfileinfo, sRuntimeLoadError)
		{
			// Called non-loaded base ctor

			_isLoaded = true;
			// Assembly
			_assembly = assembly;

			// Is Primary?
			try
			{
				var fiAssembly = new FileInfo(new Uri(assembly.CodeBase).LocalPath);
				string sError;
				_isPrimary = (_pluginfileinfo.Folder.IsPrimary) && (PluginLoader.IsPrimaryAssemblyStrongNameOk(assembly.GetName(), fiAssembly.FullName, out sError));

				_plugins = LoadPluginsList(plugintypes);
			}
			catch(Exception ex)
			{
				Core.ReportBackgroundException(ex);
			}
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the cached description.
		/// </summary>
		[NotNull]
		public FlowDocument Description
		{
			get
			{
				return _description ?? (_description = LoadPluginDescription());
			}
		}

		/// <summary>
		/// Whether the item has been loaded and contains the live info, otherwise, a stub.
		/// </summary>
		public bool IsLoaded
		{
			get
			{
				return _isLoaded;
			}
		}

		/// <summary>
		/// Gets whether this is a Core Plugin.
		/// </summary>
		public bool IsPrimary
		{
			get
			{
				return _isPrimary;
			}
		}

		/// <summary>
		/// Makes a display name out of the assembly name by dropping the technical information (eg ".OmeaPlugin.").
		/// </summary>
		[NotNull]
		public string PluginAssemblyDisplayName
		{
			get
			{
				// Choose a display name: name of the plugin if loaded and only one plugin in the assembly
				return _PluginAssemblyDisplayName ?? (_PluginAssemblyDisplayName = (IsLoaded) && (Plugins != null) && (Plugins.Length == 1) ? Plugins[0].Title : PluginLoader.AssemblyNameToPluginDisplayName(PluginAssemblyName));
			}
		}

		/// <summary>
		/// Icon for the plugin assembly. A plugin icon if loaded and single-plugin, a generic icon otherwise.
		/// </summary>
		[NotNull]
		public ImageSource PluginAssemblyIcon
		{
			get
			{
				return _PluginAssemblyIcon ?? (_PluginAssemblyIcon = LoadPluginAssemblyIcon());
			}
		}

		/// <summary>
		/// Name of the assembly, AS IS, including the ".OmeaPlugin." part.
		/// </summary>
		[NotNull]
		public string PluginAssemblyName
		{
			get
			{
				return _sPluginAssemblyName;
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Creates a new instance, fully-initialized upon an assembly.
		/// </summary>
		[NotNull]
		public static OmeaPluginsPageAssemblyInfo CreateFromAssembly([NotNull] Assembly assembly, [NotNull] IList<Type> plugintypes, PluginLoader.PossiblyPluginFileInfo pluginfileinfo, [NotNull] string sRuntimeLoadError)
		{
			return new OmeaPluginsPageAssemblyInfo(assembly, plugintypes, pluginfileinfo, sRuntimeLoadError);
		}

		/// <summary>
		/// Creates a new instance that has its <see cref="IsLoaded"/> <c>False</c>.
		/// </summary>
		[NotNull]
		public static OmeaPluginsPageAssemblyInfo CreateNotLoaded([NotNull] string sAssemblyName, PluginLoader.PossiblyPluginFileInfo pluginfileinfo, [NotNull] string sRuntimeLoadError)
		{
			return new OmeaPluginsPageAssemblyInfo(sAssemblyName, pluginfileinfo, sRuntimeLoadError);
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Chooses the icon for an assembly from its plugins.
		/// </summary>
		private ImageSource LoadPluginAssemblyIcon()
		{
			// Icon yet unknown?
			if((!IsLoaded) || (Plugins == null))
				return PluginDescription.GenericPluginIcon;

			// Check for the one and only plugin icon
			ImageSource icondata = null;
			foreach(PluginDescription plugin in Plugins)
			{
				if((plugin.Icon == null) || (plugin.Icon.Width == 0) || (plugin.Icon.Height == 0))
					continue;
				if(icondata == null) // First met
					icondata = plugin.Icon;
				else
				{
					// Two plugins, each with an icon
					// If different icons, return a generic one for the assembly
					string sA = TypeDescriptor.GetConverter(typeof(ImageSource)).ConvertToString(icondata);
					string sB = TypeDescriptor.GetConverter(typeof(ImageSource)).ConvertToString(plugin.Icon);
					if(sA != sB)
						return PluginDescription.GenericPluginIcon;
				}
			}

			// Inambiguous icon?
			return icondata ?? PluginDescription.GenericPluginIcon;
		}

		/// <summary>
		/// Lazy-loads the author and description infos for all the plugins in the assembly.
		/// Should not be done in ctor 'cause sometimes the assembly is loaded from a file, and there's the some conversion to be done.
		/// </summary>
		[NotNull]
		private FlowDocument LoadPluginDescription()
		{
			try
			{
				FlowDocument document = new FlowDocument().SetSystemFont();

				// Render each plugin
				bool bNotFirst = false;
				foreach(PluginDescription plugindesc in Plugins ?? new PluginDescription[] {})
				{
					// Plugin separator
					if(bNotFirst)
						document.Blocks.Add(new BlockUIContainer(new Rectangle {Height = 2, Fill = SystemColors.ControlDarkBrush}));
					else
						bNotFirst = true;

					var sectionPlugin = new Section();
					document.Blocks.Add(sectionPlugin);

					// Plugin heading
					sectionPlugin.Blocks.Add(new Paragraph {Inlines = {new InlineUIContainer(new Image {Source = plugindesc.Icon, Width = 16, Height = 16, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 3, 0)}) {BaselineAlignment = BaselineAlignment.Bottom}, new Bold(new Run(plugindesc.Title)), new Run(" " + Stringtable.PluginWrittenBy + " "), new Run(plugindesc.Author)}});

					// Plugin description
					var sectionDesc = new Section();
					sectionDesc.Margin = sectionDesc.Margin.Add(24, 0, 16, 0);
					sectionPlugin.Blocks.Add(sectionDesc);
					sectionDesc.Blocks.AddRange(new List<Block>(plugindesc.Description.Blocks));
				}

				// “No plugins in this assembly” case, or “Looking for plugins…” case (not loaded)
				if((Plugins ?? new PluginDescription[] {}).Length == 0)
					document.Blocks.Add(new Paragraph(new Italic(new Run(IsLoaded ? Stringtable.NoPluginsInAssembly : Stringtable.LookingForPlugins))) {TextAlignment = TextAlignment.Center});

				if(OmeaPluginsPage._showDebugInfo)
				{
					// Separator
					document.Blocks.Add(new BlockUIContainer(new Rectangle {Height = 2, Fill = SystemColors.ControlDarkBrush}));

					// Debug info
					Paragraph para = document.AddPara();
					para.Append(IsPrimary ? Stringtable.PluginDescDebug_IsPrimary : Stringtable.PluginDescDebug_IsNonPrimary);
					para.Append(string.Format(" {2} {0} ({1}).", _pluginfileinfo.Folder.Name, _pluginfileinfo.Folder.Location, Stringtable.PluginDescDebug_LoadedFrom));
					document.AddPara().Append(string.Format("{1}: {0}", _pluginfileinfo.File.FullName, Stringtable.File));
					if(!_pluginfileinfo.Reason.IsEmpty()) // File-lookup-time errors
						document.AddPara().Append(_pluginfileinfo.Reason);
					if(!_sRuntimeLoadError.IsEmpty()) // Runtime load errors
						document.AddPara().Append(_sRuntimeLoadError);
				}

				return document;
			}
			catch(Exception ex)
			{
				Core.ReportBackgroundException(ex);
				return RichContentConverter.DocumentFromException(ex);
			}
		}

		/// <summary>
		/// Fetches the plugin descriptions out of the assembly.
		/// </summary>
		private PluginDescription[] LoadPluginsList([NotNull] IList<Type> plugintypes)
		{
			if(plugintypes == null)
				throw new ArgumentNullException("plugintypes");
			if(_assembly == null)
				throw new InvalidOperationException(string.Format("Need an assembly to load the plugins list."));

			// Create descriptions
			var descriptions = new List<PluginDescription>();
			foreach(Type plugintype in plugintypes)
			{
				try
				{
					descriptions.Add(PluginDescription.CreateFromPluginType(plugintype));
				}
				catch(Exception ex)
				{
					Core.ReportBackgroundException(ex);
				}
			}

			return descriptions.ToArray();
		}

		/// <summary>
		/// Gets the list of plugins, if <see cref="IsLoaded"/>.
		/// Otherwise, <c>Null</c>.
		/// </summary>
		[CanBeNull]
		private PluginDescription[] Plugins
		{
			get
			{
				return _plugins;
			}
		}

		#endregion
	}
}