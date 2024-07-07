// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Interop;

namespace JetBrains.Omea
{
	/// <summary>
	/// A bar containing shortcuts for quickly jumping to OmniaMea resources.
	/// </summary>
	internal class ShortcutBar : UserControl, ICommandBar, ICommandBarSite
	{
		/// <summary>
		/// A toolbar with a chevron that displays the shortcuts.
		/// </summary>
		private ChevronBar _chevronBar;

		private ContextMenu _shortcutContextMenu;

		private MenuItem _miDeleteShortcut;

		private IContainer components;

		private IResource _contextMenuShortcut;

		private int _maxOrder;

		/// <summary>
		/// Number of shortcuts currently displayed on the shortcut bar.
		/// If 0, then the shortcuts toolbar <see cref="_chevronBar"/> should be hidden and the no-items-drop-welcome displayed instead.
		/// </summary>
		private int _numShortcuts;

		private IResourceList _resourcesWithShortcuts;

		private ColorScheme _colorScheme;

		private ToolTip _tooltip;

		private static ShortcutBar _theInstance;

		private Label _lblOrganize;

		/// <summary>
		/// The command bar site.
		/// </summary>
		private ICommandBarSite _site = null;

		/// <summary>
		/// The grip support.
		/// </summary>
		protected Grip _grip;

		/// <summary>
		/// Text for the no-items-banner.
		/// </summary>
		protected static readonly string _sNoItemsBanner = "Drop resources here to create shortcuts";

		/// <summary>
		/// Font for the no-items-banner.
		/// </summary>
		protected static readonly Font _fontNoItemsBanner = new Font( "Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((Byte) (204)) );

		/// <summary>
		/// Format flags for the no-items-banner text.
		/// </summary>
		private DrawTextFormatFlags _dtfNoItemsBannerFormatFlags = DrawTextFormatFlags.DT_CENTER | DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_WORDBREAK | DrawTextFormatFlags.DT_END_ELLIPSIS;

		/// <summary>
		/// Padding between the title and the controls after it.
		/// </summary>
		protected static readonly int c_nAfterTitlePadding = 4;

		#region Construction

		public ShortcutBar()
		{
			_theInstance = this;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponentSelf();

			_lblOrganize = new Label();
			_lblOrganize.Text = "Organize...";
		}

		public static ShortcutBar GetInstance()
		{
			return _theInstance;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Visual Init

		/// <summary>
		/// Visual Init.
		/// </summary>
		private void InitializeComponentSelf()
		{
			components = new Container();
			_shortcutContextMenu = new ContextMenu();
			_miDeleteShortcut = new MenuItem();
			_tooltip = new ToolTip( components );
			SuspendLayout();
			//
			// _chevronBar
			//
			_chevronBar = new ChevronBar();
			_chevronBar.SetSite( this );
			_chevronBar.AllowDrop = true;
			_chevronBar.Name = "_chevronBar";
			_chevronBar.TabIndex = 1;
			_chevronBar.ChevronMenuItemClick += OnChevronMenuItemClick;
			_chevronBar.DragDrop += OnDragDropAny;
			_chevronBar.DragEnter += OnDragEnterAny;
			_chevronBar.BackColor = SystemColors.Control;
			_chevronBar.GetChevronMenuText = OnGetChevronMenuText;
			_chevronBar.SeparateHiddenControls = true;
			_chevronBar.AllowOversizing = true;
			//
			// _shortcutContextMenu
			//
			_shortcutContextMenu.MenuItems.AddRange( new MenuItem[] { _miDeleteShortcut } );
			//
			// miDeleteShortcut
			//
			_miDeleteShortcut.Index = 0;
			_miDeleteShortcut.Text = "Delete Shortcut";
			_miDeleteShortcut.Click += miDeleteShortcut_Click;

			// Grip
			_grip = new Grip( this );
			_grip.SetSite( this );

			//
			// ShortcutBar
			//
			Controls.Add( _chevronBar );
			AllowDrop = true;
			Name = "ShortcutBar";
			Font = new Font( "Tahoma", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((Byte) (204)) );
			Text = "Shortcuts";
			Size = new Size( 308, 30 );
			DragEnter += OnDragEnterAny;
			DragDrop += OnDragDropAny;
			ResumeLayout( false );

			SetStyle( ControlStyles.AllPaintingInWmPaint
				| ControlStyles.CacheText
				| ControlStyles.ContainerControl
				| ControlStyles.ResizeRedraw
				| ControlStyles.Selectable
				| ControlStyles.UserPaint
				| ControlStyles.Opaque
			          , true );
		}

		#endregion

		public ColorScheme ColorScheme
		{
			get { return _colorScheme; }
			set
			{
				_colorScheme = value;
				//BackColor = ColorScheme.GetColor( _colorScheme, "ShortcutBar.Background",
				//                                  Color.FromArgb( 223, 220, 203 ) );
				//_chevronBar.ChevronBackColor = BackColor;
			}
		}

		/// <summary>
		/// Fills the chevron bar with shortcut links.
		/// </summary>
		public void RebuildShortcutBar()
		{
			if( _resourcesWithShortcuts != null )
			{
				_resourcesWithShortcuts.ResourceDeleting -= OnResourceWithShortcutDeleting;
				_resourcesWithShortcuts.Dispose();
			}

			_chevronBar.ClearControls();
			using( new LayoutSuspender( _chevronBar ) )
			{
				IResourceList shortcuts = Core.ResourceStore.GetAllResources( "Shortcut" );
				shortcuts.Sort( new int[] {ShortcutProps.Order}, true );
				_numShortcuts = shortcuts.Count;
				foreach( IResource shortcut in shortcuts )
				{
					if( shortcut.IsDeleting )
						continue;

					IResource target = shortcut.GetLinkProp( ShortcutProps.Target );
					if( target == null || target.IsDeleting )
					{
						// delete the shortcut when its target is deleted
						new ResourceProxy( shortcut ).Delete();
						continue;
					}

					if( !Core.ResourceStore.ResourceTypes[ target.Type ].OwnerPluginLoaded )
					{
						continue;
					}

					ResourceLinkLabel lbl = new ResourceLinkLabel();
					lbl.Resource = target;
					if( shortcut.HasProp( "Name" ) )
					{
						lbl.Text = shortcut.GetStringProp( "Name" );
						if( target.DisplayName != lbl.Text )
						{
							_tooltip.SetToolTip( lbl.NameLabel, target.DisplayName );
						}
					}
					lbl.Tag = shortcut;
					lbl.ResourceLinkClicked += OnResourceLinkClicked;
					lbl.LinkContextMenu += OnResourceLinkContextMenu;
					lbl.ResourceDragOver += OnResourceLinkDragOver;
					lbl.ResourceDrop += OnResourceLinkDrop;
					lbl.Width = lbl.PreferredWidth + 4;
					_chevronBar.AddControl( lbl );

					_maxOrder = shortcut.GetIntProp( ShortcutProps.Order );
				}

				_chevronBar.AddHiddenControl( _lblOrganize );
			}

			_resourcesWithShortcuts = Core.ResourceStore.FindResourcesWithPropLive( null, ShortcutProps.Target );
			_resourcesWithShortcuts.ResourceDeleting += OnResourceWithShortcutDeleting;

			if( _site != null )
				_site.PerformLayout( this );
			PerformLayout();
		}

		private void OnResourceWithShortcutDeleting( object sender, ResourceIndexEventArgs e )
		{
			Core.UIManager.QueueUIJob( new MethodInvoker( RebuildShortcutBar ) );
		}

		private static void OnResourceLinkDragOver( object sender, ResourceDragEventArgs e )
		{
			e.Effect = DragDropEffects.Link;
		}

		private void OnResourceLinkDrop( object sender, ResourceDragEventArgs e )
		{
			AddShortcutsFromList( e.DroppedResources );
		}

		private static void OnResourceLinkClicked( object sender, CancelEventArgs e )
		{
			ResourceLinkLabel lbl = (ResourceLinkLabel) sender;
			HandleLinkLabelClick( lbl );
			e.Cancel = true;
		}

		/// <summary>
		/// When a link is clicked, opens the webpage if it's a Web link, or displays
		/// the resource in context if it's a resource link.
		/// </summary>
		private static void HandleLinkLabelClick( ResourceLinkLabel lbl )
		{
			if( lbl.Resource.IsDeleted )
				return;

			if( lbl.Resource.Type == "Weblink" )
				Core.UIManager.OpenInNewBrowserWindow(lbl.Resource.GetStringProp( "URL" ));
			else
			{
				IResource shortcut = (IResource) lbl.Tag;
				IUIManager uiManager = Core.UIManager;
				uiManager.BeginUpdateSidebar();
				if( shortcut.HasProp( ShortcutProps.Workspace ) )
				{
					Core.WorkspaceManager.ActiveWorkspace = shortcut.GetLinkProp( ShortcutProps.Workspace );
				}
				else
				{
					Core.WorkspaceManager.ActiveWorkspace = null;
				}
				if( shortcut.HasProp( ShortcutProps.TabID ) )
				{
					Core.TabManager.CurrentTabId = shortcut.GetStringProp( ShortcutProps.TabID );
				}
				uiManager.EndUpdateSidebar();
				ActionContext context = new ActionContext( ActionContextKind.Other, null,
				                                           lbl.Resource.ToResourceList() );
				if( !Core.ActionManager.ExecuteLinkClickAction( context ) )
				{
					uiManager.DisplayResourceInContext( lbl.Resource );
				}
			}
		}

		/// <summary>
		/// When a link is right-clicked, shows the context menu.
		/// </summary>
		private void OnResourceLinkContextMenu( object sender, ResourceLinkLabelEventArgs e )
		{
			ResourceLinkLabel lbl = (ResourceLinkLabel) sender;
			_contextMenuShortcut = (IResource) lbl.Tag;
			_shortcutContextMenu.Show( lbl, e.Point );
		}

		/// <summary>
		/// "Delete Shortcut" menu item handler.
		/// </summary>
		private void miDeleteShortcut_Click( object sender, EventArgs e )
		{
			if( _contextMenuShortcut != null )
			{
				new ResourceProxy( _contextMenuShortcut ).Delete();
				//RebuildShortcutBar();
			}
		}

		/// <summary>
		/// Drag enter handler - accepts any resource list dragged in the pane.
		/// </summary>
		private static void OnDragEnterAny( object sender, DragEventArgs e )
		{
			if( e.Data.GetDataPresent( typeof( IResourceList ) ) )
			{
				e.Effect = DragDropEffects.Link;
			}
		}

		/// <summary>
		/// Drag drop handler - creates a shortcut if there isn't any shortcut
		/// for the dropped resource.
		/// </summary>
		private void OnDragDropAny( object sender, DragEventArgs e )
		{
			if( e.Data.GetDataPresent( typeof( IResourceList ) ) )
			{
				IResourceList droppedResources = (IResourceList) e.Data.GetData( typeof( IResourceList ) );
				AddShortcutsFromList( droppedResources );
			}
		}

		public int ShortcutCount
		{
			get { return _numShortcuts; }
		}

		/// <summary>
		/// Adds shortcuts to resources from the specified resource list to the shortcut
		/// bar.
		/// </summary>
		internal void AddShortcutsFromList( IResourceList droppedResources )
		{
			foreach( IResource res in droppedResources )
			{
				AddShortcutToResource( res );
			}
		}

		/// <summary>
		/// Adds a shortcut to the specified resource to the shortcut bar.
		/// </summary>
		public void AddShortcutToResource( IResource newShortcut )
		{
			ITabManager tabManager = Core.TabManager;
            IResource activeWorkspace = Core.WorkspaceManager.ActiveWorkspace;

			IResourceList shortcutTargets = newShortcut.GetLinksOfType( "Shortcut", ShortcutProps.Target );
			if( newShortcut.Type == "SearchView" )
			{
				foreach( IResource res in shortcutTargets )
				{
                    if( res.GetStringProp( ShortcutProps.TabID ) == tabManager.CurrentTabId &&
                        res.GetLinkProp( ShortcutProps.Workspace ) == activeWorkspace )
                    {
                        return;
                    }
				}
			}
			else
			{
				if( shortcutTargets.Count > 0 )
					return;
			}

			ResourceProxy proxy = ResourceProxy.BeginNewResource( "Shortcut" );
			proxy.SetProp( ShortcutProps.Order, _maxOrder + 1 );
			proxy.AddLink( ShortcutProps.Target, newShortcut );

			if( activeWorkspace != null )
			{
				proxy.AddLink( ShortcutProps.Workspace, activeWorkspace );
			}

			if( newShortcut.Type == "SearchView" )
			{
				proxy.SetProp( ShortcutProps.TabID, tabManager.CurrentTabId );
                string wsName = "";
                if ( activeWorkspace != null )
                {
                    wsName = " in " + activeWorkspace.GetStringProp( Core.Props.Name );
                }

				proxy.SetProp( Core.Props.Name, tabManager.CurrentTab.Name + wsName + ": " +
					newShortcut.DisplayName );
			}
			else if( newShortcut.DisplayName.Length > 20 )
			{
				proxy.SetProp( Core.Props.Name, newShortcut.DisplayName.Substring( 0, 20 ) + "..." );
			}
			proxy.EndUpdate();
			RebuildShortcutBar();
		}

		/// <summary>
		/// When a link in the chevron menu is clicked, performs regular processing
		/// of the click.
		/// </summary>
		private void OnChevronMenuItemClick( object sender, ChevronBar.ChevronMenuItemClickEventArgs args )
		{
			ResourceLinkLabel lbl = args.ClickedControl as ResourceLinkLabel;
			if( lbl != null )
			{
				HandleLinkLabelClick( lbl );
			}
			else
			{
				OrganizeShortcuts();
			}
		}

		internal void OrganizeShortcuts()
		{
			using( OrganizeShortcutsDlg dlg = new OrganizeShortcutsDlg() )
			{
				dlg.ShowOrganizeDialog();
			}
			RebuildShortcutBar();
		}

		private static string OnGetChevronMenuText( Control ctl )
		{
			ResourceLinkLabel lbl = ctl as ResourceLinkLabel;
			if( lbl != null )
			{
				IResource shortcut = lbl.Resource.GetLinkProp( ShortcutProps.Target );
				if( shortcut != null && !shortcut.HasProp( ShortcutProps.Renamed ) &&
					!shortcut.HasProp( ShortcutProps.TabID ) )
				{
					return lbl.Resource.DisplayName;
				}
			}

			return ctl.Text;
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			// Background
			e.Graphics.FillRectangle( SystemBrushes.Control, 2, 2, ClientRectangle.Width - 4, ClientRectangle.Height - 4 );

			// Border
			/*
			Borders drawing sequence:

			# dark-dark
			@ dark
			L lightlight

			@ @@@@@@@@@@@@@@-1-@@@@@@@@@@@@@@@@@L
			@                                   L
			@  L LLLLLLLLLLL-5-LLLLLLLLLLLLLL#  L
			@  L                             #  L
			|  |                             |  |
			2  6                             8  4
			|  |                             |  |
			@  L                                L
			@  #############-7-###############  L
			@
			LLLLLLLLLLLLLLLL-3-LLLLLLLLLLLLLLLLLL

			*/
			Rectangle rc = ClientRectangle;

			// Outer
			e.Graphics.DrawLine( SystemPens.ControlDark, rc.Left + 1, rc.Top + 0, rc.Right - 2, rc.Top + 0 ); // 1
			e.Graphics.DrawLine( SystemPens.ControlDark, rc.Left + 0, rc.Top + 0, rc.Left + 0, rc.Bottom - 2 ); // 2
			e.Graphics.DrawLine( SystemPens.ControlLightLight, rc.Left + 0, rc.Bottom - 1, rc.Right - 1, rc.Bottom - 1 ); // 3
			e.Graphics.DrawLine( SystemPens.ControlLightLight, rc.Right - 1, rc.Top + 0, rc.Right - 1, rc.Bottom - 2 ); // 4

			// Inner
			e.Graphics.DrawLine( SystemPens.ControlLightLight, rc.Left + 2, rc.Top + 1, rc.Right - 3, rc.Top + 1 ); // 5
			e.Graphics.DrawLine( SystemPens.ControlLightLight, rc.Left + 1, rc.Top + 1, rc.Left + 1, rc.Bottom - 3 ); // 6
			e.Graphics.DrawLine( SystemPens.ControlDarkDark, rc.Left + 1, rc.Bottom - 2, rc.Right - 2, rc.Bottom - 2 ); // 7
			e.Graphics.DrawLine( SystemPens.ControlDarkDark, rc.Right - 2, rc.Top + 1, rc.Right - 2, rc.Bottom - 3 ); // 8

			// Paint the grip
			_grip.OnPaint( e.Graphics );

			// Paint the title (if visisble)
			if( _rectTitle != Rectangle.Empty )
				JetLinkLabel.DrawText( e.Graphics, Text, _rectTitle, Font, SystemColors.ControlText,
				                       DrawTextFormatFlags.DT_CENTER | DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_VCENTER );

			// Paint the no-items-banner (if visisble)
			if( _rectNoItemsBanner != Rectangle.Empty )
				JetLinkLabel.DrawText( e.Graphics, _sNoItemsBanner, _rectNoItemsBanner, _fontNoItemsBanner, SystemColors.ControlText,
				                       _dtfNoItemsBannerFormatFlags );
		}

		/// <summary>
		/// The outer rectangle (.Bounds at the moment of calculation).
		/// </summary>
		private Rectangle _rectBounds;

		/// <summary>
		/// Client area within the border (no padding).
		/// </summary>
		private Rectangle _rectInBorder;

		/// <summary>
		/// Client area without the padding and with grip excluded.
		/// </summary>
		private Rectangle _rectWithoutGrip;

		/// <summary>
		/// Client area within the borders, without the grip, and with padding included.
		/// </summary>
		private Rectangle _rectClientPadded;

		/// <summary>
		/// Title placeholder.
		/// </summary>
		private Rectangle _rectTitle;

		/// <summary>
		/// Placeholder for the shortcuts chevronette toolbar.
		/// </summary>
		private Rectangle _rectToolbar;

		/// <summary>
		/// Placeholder for the welcome string.
		/// </summary>
		private Rectangle _rectNoItemsBanner;

		/// <summary>
		/// Optimal size of the title string.
		/// </summary>
		private Size _sizeTitle;

		/// <summary>
		/// Optimal size of the no-items-banner string.
		/// </summary>
		private Size _sizeNoItemsBanner;

		protected override void OnLayout( LayoutEventArgs levent )
		{
			using( new LayoutSuspender( _chevronBar ) )
			{
				//////
				// Calculate the rectangles
				Size	sizeToolbarMin = _chevronBar.MinSize;

				_rectBounds = ClientRectangle;

				// Client rect within the control's borders
				_rectInBorder = ClientRectangle;
				_rectInBorder.Inflate( -2, -2 ); // Exclude the borders

				// Exclude the grip
				_rectWithoutGrip = _grip.OnLayout( _rectInBorder );

				// The innermost client rect
				_rectClientPadded = _rectWithoutGrip;
				_rectClientPadded.Inflate( -2, -1 );

				// Size of the labels
				_sizeTitle = JetLinkLabel.GetTextSize( this, Text, Font );
				_sizeNoItemsBanner = JetLinkLabel.GetTextSize( this, _sNoItemsBanner, _fontNoItemsBanner, _rectClientPadded.Size, _dtfNoItemsBannerFormatFlags );

				if( _numShortcuts != 0 )
				{ // There are shortcuts. Include the toolbar and (possibly) the title
					if( _rectClientPadded.Width >= sizeToolbarMin.Width + c_nAfterTitlePadding + _sizeTitle.Width )
					{ // Title fits
						//_rectTitle = new Rectangle( _rectClientPadded.Location, new Size( _sizeTitle.Width, _rectClientPadded.Height ) );
						_rectTitle = new Rectangle( new Point( _rectClientPadded.Left, _rectClientPadded.Top + (_rectClientPadded.Height - _sizeTitle.Height) / 2 ), _sizeTitle );
						_rectToolbar = new Rectangle( _rectTitle.Right + c_nAfterTitlePadding, _rectClientPadded.Top, _rectClientPadded.Width - (_rectTitle.Width + c_nAfterTitlePadding), _rectClientPadded.Height );
					}
					else
					{ // Title does not fit
						_rectTitle = Rectangle.Empty;
						_rectToolbar = _rectClientPadded;
					}
					_rectNoItemsBanner = Rectangle.Empty;
				}
				else
				{ // No shortcuts, display just the no-items-banner
					_rectTitle = Rectangle.Empty;
					_rectToolbar = Rectangle.Empty;

					// Align the banner
					Size sizeFit = new Size
						(
						_sizeNoItemsBanner.Width < _rectClientPadded.Width ? _sizeNoItemsBanner.Width : _rectClientPadded.Width,
						_sizeNoItemsBanner.Height < _rectClientPadded.Height ? _sizeNoItemsBanner.Height : _rectClientPadded.Height
						);
					_rectNoItemsBanner = new Rectangle(
						new Point
							(
							_rectClientPadded.Left, // H-Align left
							_rectClientPadded.Top + (_rectClientPadded.Height - sizeFit.Height) / 2 // V-center
							),
						sizeFit );
				}

				/////////////////////
				// Apply the layout
				_chevronBar.Visible = _rectToolbar != Rectangle.Empty;
				_chevronBar.Bounds = _rectToolbar;
			}

			// Apply the visual changes
			Invalidate( false );
		}

		#region ICommandBar Interface Members

		public void SetSite( ICommandBarSite site )
		{
			_site = site;
		}

		public Size MinSize
		{
			get
			{
				// Min size of the inlying controls
				Size sizeInnerMinSize = _numShortcuts != 0 ?
					_chevronBar.MinSize // It's the chevron only
					: new Size( 0, 0 ); // No shortcuts, no size (we may drop even on the text-less small bar)

				// Append the paddings and grip
				Size sizePadding = _rectBounds.Size - _rectClientPadded.Size;
				return sizeInnerMinSize + sizePadding;
			}
		}

		public Size MaxSize
		{
			get
			{
				// Max size of the inlying controls
				Size sizeInnerMaxSize;
				if( _numShortcuts != 0 ) // There are shortcuts, ask the chevron bar
					sizeInnerMaxSize = _chevronBar.MaxSize; // It's the chevron only
				else
					sizeInnerMaxSize = new Size( _sizeNoItemsBanner.Width, 30 ); // Size width to the max text label width

				// Append the paddings and grip
				Size sizePadding = _rectBounds.Size - _rectClientPadded.Size;
				Size sizeRet = sizeInnerMaxSize + sizePadding;
				sizeRet.Width += _sizeTitle.Width + c_nAfterTitlePadding;

				// Don't allow the max-sizes to wrap over to negative numbers
				if(sizeInnerMaxSize.Width == int.MaxValue)
					sizeRet.Width = int.MaxValue;
				if(sizeInnerMaxSize.Height == int.MaxValue)
					sizeRet.Height = int.MaxValue;

				return sizeRet;
			}
		}

		public Size OptimalSize
		{
			get
			{
				Size sizeInnerOptSize = (_numShortcuts != 0) ?
					_chevronBar.OptimalSize // There are controls, take their summary size
					: _sizeNoItemsBanner; // There are no controls, fit the label

				// Append the paddings and grip
				Size sizePadding = _rectBounds.Size - _rectClientPadded.Size;
				Size sizeRet = sizeInnerOptSize + sizePadding;
				sizeRet.Width += _sizeTitle.Width + c_nAfterTitlePadding;
				return sizeInnerOptSize + sizePadding;
			}
		}

		public Size Integral
		{
			get { return new Size( 1, 1 ); }
		}

		#endregion

		#region ICommandBarSite Interface Members

		public bool RequestMove( ICommandBar sender, Size offset )
		{
			if( _site != null )
				return _site.RequestMove( sender, offset );
			else
				return false;
		}

		public bool RequestSize( ICommandBar sender, Size difference )
		{
			if( _site != null )
				return _site.RequestSize( sender, difference );
			else
				return false;
		}

		public bool PerformLayout( ICommandBar sender )
		{
			if( (_site != null) && (_site.PerformLayout( sender )) )
			{
				PerformLayout(); // Call the base function
				return true;
			}
			else
				return false;
		}

		#endregion
	}

	#region ShortcutProps Class

	internal class ShortcutProps
	{
		private static int _propShortcutTarget;

		private static int _propShortcutWorkspace;

		private static int _propShortcutOrder;

		private static int _propShortcutTabID;

		private static int _propShortcutRenamed;

		/// <summary>
		/// Registers the resource types used for shortcuts.
		/// </summary>
		public static void Register()
		{
			IResourceStore store = ICore.Instance.ResourceStore;
			store.ResourceTypes.Register( "Shortcut", "", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
			_propShortcutTarget = store.PropTypes.Register( "ShortcutTarget", PropDataType.Link, PropTypeFlags.Internal );
			_propShortcutWorkspace = store.PropTypes.Register( "ShortcutWorkspace", PropDataType.Link, PropTypeFlags.Internal );
			_propShortcutOrder = store.PropTypes.Register( "ShortcutOrder", PropDataType.Int, PropTypeFlags.Internal );
			_propShortcutTabID = store.PropTypes.Register( "ShortcutTabID", PropDataType.String, PropTypeFlags.Internal );
			_propShortcutRenamed = store.PropTypes.Register( "ShortcutRenamed", PropDataType.Bool, PropTypeFlags.Internal );
		}

		internal static int Target
		{
			get { return _propShortcutTarget; }
		}

		internal static int Workspace
		{
			get { return _propShortcutWorkspace; }
		}

		internal static int Order
		{
			get { return _propShortcutOrder; }
		}

		internal static int TabID
		{
			get { return _propShortcutTabID; }
		}

		internal static int Renamed
		{
			get { return _propShortcutRenamed; }
		}
	}

	#endregion
}
