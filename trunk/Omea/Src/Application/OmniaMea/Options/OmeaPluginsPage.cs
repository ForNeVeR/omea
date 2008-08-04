using System;
using System.Collection.Generic;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;

using GUIControls.RichText;

using JetBrains.Annotations;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Avalon;

using OmniaMea;

using Binding=System.Windows.Data.Binding;
using Button=System.Windows.Controls.Button;
using CheckBox=System.Windows.Controls.CheckBox;
using HorizontalAlignment=System.Windows.HorizontalAlignment;
using ListView=System.Windows.Controls.ListView;
using Orientation=System.Windows.Controls.Orientation;
using ToolBar=System.Windows.Controls.ToolBar;

namespace JetBrains.Omea.Plugins
{
	/// <summary>
	/// An options page for listing the available plugins and turning them on/off.
	/// </summary>
	public class OmeaPluginsPage : AbstractOptionsPane
	{
		#region Data

		public static readonly RoutedCommand CommandAboutOmeaPlugins = new RoutedCommand("AboutOmeaPlugins", typeof(OmeaPluginsPage));

		public static readonly RoutedCommand CommandDownloadMorePlugins = new RoutedCommand("DownloadMorePlugins", typeof(OmeaPluginsPage));

		public static readonly RoutedCommand CommandRefresh = new RoutedCommand("Refresh", typeof(OmeaPluginsPage)); // TODO: reuse the generic command

		internal static bool _showDebugInfo;

		private static readonly ImageSource _iconPluginPrimaryGlyph = Utils.LoadResourceImage("Icons/PluginPrimaryGlyph.png");

		private static readonly ImageSource _iconPluginThirdPartyGlyph = Utils.LoadResourceImage("Icons/PluginThirdPartyGlyph.png");

		private static readonly string UriDownloadMorePlugins = "http://www.jetbrains.net/confluence/display/OMEA/Third-party+Plugins";

		private static readonly string UriOmeaTechnicalReference = "http://www.jetbrains.net/confluence/display/OMEA/Third-party+Plugins";

		/// <summary>
		/// Items displayed in the list, which includes both plugins and (in debug mode) non-plugins.
		/// </summary>
		[NotNull]
		private readonly List<OmeaPluginsPageListEntry> _arItems = new List<OmeaPluginsPageListEntry>();

		private ICollectionView _arItemsView;

		private bool _isVisible;

		/// <summary>
		/// Not-yet-loaded entries from <see cref="_arItems"/> that are plugins.
		/// </summary>
		[NotNull]
		private readonly Queue<OmeaPluginsPageListEntryPlugin> _queueItemsToLoad = new Queue<OmeaPluginsPageListEntryPlugin>();

		#endregion

		#region Init

		public OmeaPluginsPage()
		{
			FillPluginsList();
			Grid root = InitView();
			InitCommands(root);
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets or sets whether the page displays additional information on why the plugins were loaded or not, useful for debug.
		/// Note: the field static so that to retain the value between Options dialog runs (but not Omea runs), the property is nonstatic for binding.
		/// </summary>
		public bool ShowDebugInfo
		{
			get
			{
				return _showDebugInfo;
			}
			set
			{
				_showDebugInfo = value;
				ReloadPluginsList();
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Concats two collections.
		/// </summary>
		[NotNull]
		private static IEnumerable<object> Concat<TA, TB>([NotNull] IEnumerable<TA> ca, [NotNull] IEnumerable<TB> cb)
		{
			foreach(TA item in ca)
				yield return item;
			foreach(TB item in cb)
				yield return item;
		}

		[NotNull]
		private static DataTemplate InitView_PluginsList_IsEnabledTemplate()
		{
			return new DataTemplateDelegate(() => AvalonEx.Bind(new CheckBox(), ToggleButton.IsCheckedProperty, (BindingBase)new Binding("IsEnabled")).Bind(UIElement.VisibilityProperty, new Binding("SupportsIsEnabled") {Converter = new ValueConverter<bool, Visibility>(b => b ? Visibility.Visible : Visibility.Collapsed)}).Bind(FrameworkElement.ToolTipProperty, new Binding("IsEnabled") {Converter = ValueConverter.Create((bool b) => b ? Stringtable.PluginWillBeLoaded : Stringtable.PluginWillNotBeLoaded)}));
		}

		private static DataTemplate InitView_PluginsList_NameTemplate()
		{
			var stack = new FrameworkElementFactory(typeof(StackPanel));
			stack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

			FrameworkElementFactory image;

			// IsPrimary Icon
			stack.AppendChild(image = new FrameworkElementFactory(typeof(Image)));
			image.SetValue(FrameworkElement.WidthProperty, 12.0);
			image.SetValue(FrameworkElement.HeightProperty, 12.0);
			image.SetValue(Image.StretchProperty, Stretch.Uniform);
			image.SetBinding(Image.SourceProperty, new Binding("IsPrimary") {Converter = new ValueConverter<bool?, ImageSource>(b => b != null ? ((bool)b ? _iconPluginPrimaryGlyph : _iconPluginThirdPartyGlyph) : null)});
			//image.SetValue(FrameworkElement.MarginProperty, new Thickness(0));
			image.SetBinding(FrameworkElement.ToolTipProperty, new Binding("IsPrimary") {Converter = new ValueConverter<bool?, string>(b => b != null ? ((bool)b ? Stringtable.PluginIsPrimary : Stringtable.PluginIsNotPrimary) : "")});
			image.SetValue(UIElement.OpacityProperty, .5);

			// Plugin Icon
			stack.AppendChild(image = new FrameworkElementFactory(typeof(Image)));
			image.SetValue(FrameworkElement.WidthProperty, 16.0);
			image.SetValue(FrameworkElement.HeightProperty, 16.0);
			image.SetValue(Image.StretchProperty, Stretch.Uniform);
			image.SetBinding(Image.SourceProperty, new Binding("Icon"));
			image.SetValue(FrameworkElement.MarginProperty, new Thickness(1));
			image.SetValue(FrameworkElement.ToolTipProperty, Stringtable.PluginIcon);

			FrameworkElementFactory text;
			stack.AppendChild(text = new FrameworkElementFactory(typeof(TextBlock)));
			text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
			text.SetBinding(TextBlock.TextProperty, new Binding("Title"));

			return new DataTemplate {DataType = typeof(OmeaPluginsPageListEntry), VisualTree = stack};
		}

		/// <summary>
		/// Harvest the plugins, both loaded and not.
		/// </summary>
		private void FillPluginsList()
		{
			_arItems.Clear();
			_queueItemsToLoad.Clear();
			var disabledplugins = new PluginLoader.DisabledPlugins();
			var hashDisoveredPluginAssemblies = new HashSet<string>();
			var pluginloader = (PluginLoader)Core.PluginLoader;

			// Stage 1: loaded plugins
			foreach(IPlugin plugin in pluginloader.GetLoadedPlugins())
			{
				try
				{
					string asmname = plugin.GetType().Assembly.GetName().Name;
					if(!hashDisoveredPluginAssemblies.Add(asmname))
						continue;

					// Submit
					IPlugin pluginConst = plugin;
					PluginLoader.PossiblyPluginFileInfo pluginfile = pluginloader.GetPluginFileInfo(plugin);
					_arItems.Add(new OmeaPluginsPageListEntryPlugin(asmname, () => pluginConst.GetType().Assembly, pluginfile, pluginloader.GetPluginLoadRuntimeError(pluginfile.File), disabledplugins));
				}
				catch(Exception ex)
				{
					Core.ReportBackgroundException(ex);
				}
			}

			// Stage 2: not loaded plugins (and non-plugins in Debug mode)
			foreach(PluginLoader.PossiblyPluginFileInfo file in pluginloader.GetAllPluginFiles())
			{
				try
				{
					if(file.IsPlugin)
					{
						string asmname = Path.GetFileNameWithoutExtension(file.File.FullName);
						if(!hashDisoveredPluginAssemblies.Add(asmname))
							continue;

						// Submit
						FileInfo fileConst = file.File;
						_arItems.Add(new OmeaPluginsPageListEntryPlugin(asmname, () => PluginLoader.LoadPluginAssembly(fileConst), file, pluginloader.GetPluginLoadRuntimeError(file.File), disabledplugins));
					}
					else if(ShowDebugInfo)
						_arItems.Add(new OmeaPluginsPageListEntryNonPlugin(file));
				}
				catch(Exception ex)
				{
					Core.ReportBackgroundException(ex);
				}
			}

			// Create the view for this collection that supports sorting, grouping and sharing the selection
			if(_arItemsView == null) // Reuse afterwards, as it takes part in databinding
			{
				_arItemsView = CollectionViewSource.GetDefaultView(_arItems);
				_arItemsView.SortDescriptions.Add(new SortDescription("IsEnabledInitially", ListSortDirection.Descending));
				_arItemsView.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));
				_arItemsView.GroupDescriptions.Add(new PropertyGroupDescription("IsEnabledInitially", ValueConverter.Create((bool b) => b ? Stringtable.Enabled : Stringtable.Disabled)));
			}
			else
				_arItemsView.Refresh();

			// Shedulle loading of the items
			foreach(OmeaPluginsPageListEntry item in _arItems)
			{
				if(item is OmeaPluginsPageListEntryPlugin)
					_queueItemsToLoad.Enqueue((OmeaPluginsPageListEntryPlugin)item);
			}
			if(_queueItemsToLoad.Count > 0)
				Core.UserInterfaceAP.QueueJob(Stringtable.JobLoadPluginAssemblies, PumpLoadQueue);
		}

		private void InitCommands(Grid root)
		{
			root.CommandBindings.Add(new CommandBinding(CommandDownloadMorePlugins, delegate { Core.UIManager.OpenInNewBrowserWindow(UriDownloadMorePlugins); }));
			root.CommandBindings.Add(new CommandBinding(CommandRefresh, delegate { ReloadPluginsList(); }));
			root.CommandBindings.Add(new CommandBinding(CommandAboutOmeaPlugins, delegate { ShowAboutOmeaPlugins(); }));
		}

		/// <summary>
		/// Creates the UI. Returns the root.
		/// </summary>
		private Grid InitView()
		{
			Grid grid;
			Controls.Add(new ElementHost {Dock = DockStyle.Fill, Child = grid = new Grid()});
			KeyboardNavigation.SetDirectionalNavigation(grid, KeyboardNavigationMode.None);

			grid.AddRowChild("Auto", new TextBlock(new Run(Stringtable.ChangesAfterRestart)) {Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Center}); // TODO: show only if there are changes

			// TODO(H): icons on the toolbar
			ToolBar toolbar;
			grid.AddRowChild("Auto", new ToolBarTray {ToolBars = {(toolbar = new ToolBar())}});
			KeyboardNavigation.SetTabNavigation(toolbar, KeyboardNavigationMode.Continue);
			KeyboardNavigation.SetTabIndex(toolbar, 10);
			toolbar.Items.Add(new Button {Content = new StackPanel {Orientation = Orientation.Horizontal, Children = {new Image {Width = 16, Height = 16, Source = Utils.LoadResourceImage("Icons/DownloadMorePlugins.png")}, new TextBlock {VerticalAlignment = VerticalAlignment.Center}.Append(Stringtable.DownloadCommandText)}}, ToolTip = Stringtable.DownloadCommandTooltip, Command = CommandDownloadMorePlugins});
			toolbar.Items.Add(new Button {Content = new StackPanel {Orientation = Orientation.Horizontal, Children = {new Image {Width = 16, Height = 16, Source = Utils.LoadResourceImage("Icons/RefreshPlugins.png")}, new TextBlock {VerticalAlignment = VerticalAlignment.Center}.Append(Stringtable.RefreshCommandText)}}, ToolTip = Stringtable.RefreshCommandTooltip, Command = CommandRefresh});
			toolbar.Items.Add(new Separator());
			toolbar.Items.Add(new Button {Content = Stringtable.AboutPluginsCommandText, ToolTip = Stringtable.AboutPluginsCommandTooltip, Command = CommandAboutOmeaPlugins});

			grid.AddRowChild("*", InitView_PluginsList());

			return grid;
		}

		private UIElement InitView_PluginsList()
		{
			var grid = new Grid();

			////////////////
			// Plugins list
			ListView list;
			grid.AddRowChild("3*", list = new ListView {ItemsSource = _arItemsView, IsSynchronizedWithCurrentItem = true});
			list.SelectionChanged += (sender, e) => // Lazy-load the items on selection
			{
				foreach(object item in e.AddedItems)
				{
					if(item is OmeaPluginsPageListEntryPlugin)
						((OmeaPluginsPageListEntryPlugin)item).Load(() => { });
				}
			};

			GridView gridview;
			list.View = gridview = new GridView {AllowsColumnReorder = true};

			gridview.Columns.Add(new GridViewColumn {Header = Stringtable.PluginColumnHeader, CellTemplate = InitView_PluginsList_NameTemplate(), Width = 300});
			gridview.Columns.Add(new GridViewColumn {Header = Stringtable.EnabledColumnHeader, CellTemplate = InitView_PluginsList_IsEnabledTemplate()});

			/////////////
			// Splitter
			grid.AddRowChild("3px", new GridSplitter {HorizontalAlignment = HorizontalAlignment.Stretch, ResizeBehavior = GridResizeBehavior.PreviousAndNext, ResizeDirection = GridResizeDirection.Rows});

			/////////////////
			// Preview Area
			grid.AddRowChild("1*", new FlowDocumentScrollViewer {VerticalScrollBarVisibility = ScrollBarVisibility.Auto}.Bind(FlowDocumentScrollViewer.DocumentProperty, new Binding("Description") {Source = _arItemsView}));

			return grid;
		}

		/// <summary>
		/// Loads next plugin from the <see cref="_queueItemsToLoad"/>.
		/// </summary>
		private void PumpLoadQueue()
		{
			if(!_isVisible)
				return;
			if(_queueItemsToLoad.Count == 0)
				return;

			_queueItemsToLoad.Dequeue().Load(() => Core.UserInterfaceAP.QueueJob(Stringtable.JobLoadPluginAssemblies, PumpLoadQueue));
		}

		/// <summary>
		/// Refreshes the plugins list to see whether new plugins have appeared, or to update the presentation (eg after the <see cref="ShowDebugInfo"/> flag).
		/// </summary>
		private void ReloadPluginsList()
		{
			FillPluginsList();
		}

		private void ShowAboutOmeaPlugins()
		{
			var window = new Window();

			Grid grid;
			window.Content = grid = new Grid();

			FlowDocument document = RichContentConverter.DocumentFromResource("Resources/AboutOmeaPlugins.xaml", Assembly.GetExecutingAssembly()).SetSystemFont();

			grid.AddRowChild("*", new FlowDocumentScrollViewer {Margin = new Thickness(10), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Document = document});
			grid.AddRowChild("Auto", new CheckBox {Content = Stringtable.ShowDeveloperInfo, Margin = new Thickness(10, 5, 10, 0)}.Bind(ToggleButton.IsCheckedProperty, new Binding("ShowDebugInfo") {Source = this}));
			grid.AddRowChild("Auto", new Button {Content = Stringtable.Close, IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 75, Margin = new Thickness(10)});

			window.ShowDialog();
		}

		#endregion

		#region Overrides

		public override void EnterPane()
		{
			_isVisible = true;
			if(_queueItemsToLoad.Count > 0)
				Core.UserInterfaceAP.QueueJob(Stringtable.JobLoadPluginAssemblies, PumpLoadQueue);
		}

		public override void LeavePane()
		{
			_isVisible = false;
		}

		public override void OK()
		{
			base.OK();

			// Apply the enabled state
			var disabledplugins = new PluginLoader.DisabledPlugins();
			bool bChanged = false;
			foreach(OmeaPluginsPageListEntry item in _arItems)
			{
				if(item is OmeaPluginsPageListEntryPlugin)
					bChanged |= ((OmeaPluginsPageListEntryPlugin)item).Commit(disabledplugins);
			}

			// Require restart to apply changes
			// (Note: even if loading a new plugin, should better restart, as others might depend on its registration)
			if(bChanged)
				NeedRestart = true;
		}

		#endregion
	}
}