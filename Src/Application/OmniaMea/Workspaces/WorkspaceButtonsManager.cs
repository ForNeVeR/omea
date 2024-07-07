// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.Components.ImageListButton;

namespace JetBrains.Omea.Workspaces
{
	/// <summary>
	/// Implements the workspace switching buttons layouting logic for the <see cref="WorkspaceButtonsRow"/> control.
	/// Handles workspace list updates.
	/// Handles the next/prev wsp actions and workspace organizing actions.
	/// </summary>
	/// <remarks>
	/// Though it's not a control, it manages the layout of some controls, which bounding box' dimensions are represented by <see cref="ICommandBar"/> interface of this class.
	/// </remarks>
	internal class WorkspaceButtonsManager : ICommandBar
	{
		#region Data

		/// <summary>
		/// Resource list of workspaces.
		/// The list is ordered in order of appearance (in the same oreder as the buttons should appear on screen).
		/// </summary>
		private IResourceList _workspaces;

		/// <summary>
		/// Maps the workspaces (<see cref="IResource">resources</see> from <see cref="_workspaces"/>)
		/// to the corresponding UI controls of type <see cref="WorkspaceButton"/>.
		/// </summary>
		private Hashtable _hashWorkspaceButtons;

		/// <summary>
		/// Actions for which this object registers UI handlers in Omea.
		/// </summary>
		private ArrayList _workspaceActions = new ArrayList();

		/// <summary>
		/// An action that activates the default workspace.
		/// </summary>
		private IAction _defaultWorkspaceAction;

		/// <summary>
		/// The workspace manager object.
		/// </summary>
		private WorkspaceManager _workspaceManager;

		/// <summary>
		/// Context menu of a workspace button.
		/// <see cref="_resContextMenuWorkspace"/> is the button to which the menu commands apply.
		/// </summary>
		private ContextMenu _menuOnButton;

		/// <summary><seealso cref="_btnChevron"/>
		/// Context menu that pops up when the chevron button gets clicked.
		/// </summary>
		private ContextMenu _menuOnChevron;

		/// <summary>
		/// Menuitem of the <see cref="_menuOnButton"/> that hides the workspace button into the chevron.
		/// </summary>
		private MenuItem _miHide;

		/// <summary>
		/// Workspace for which the context menu <see cref="_menuOnButton"/> was most recently displayed.
		/// </summary>
		private IResource _resContextMenuWorkspace;

		/// <summary>
		/// The "Configure" menu item of the <see cref="_menuOnButton"/> context menu.
		/// </summary>
		private MenuItem _miConfigure;

		/// <summary>
		/// A button that is displayed to the right of all the workspace buttons and brings the add/remove/edit workspace dialog
		/// </summary>
		private ImageListButton _btnOrganize;

		/// <summary>
		/// A button that represents a chevron for the workspaces bar which conceals the unfit workspace buttons.
		/// </summary>
		private ImageListButton _btnChevron;

		/// <summary>
		/// The single instance of this class.
		/// </summary>
		private static WorkspaceButtonsManager _theInstance;

		/// <summary>
		/// The command bar site.
		/// </summary>
		private ICommandBarSite _site;

		/// <summary>
		/// Parent control that owns the child controls we're controlling & layouting.
		/// </summary>
		private WorkspaceButtonsRow _parent;

		/// <summary>
		/// The workspace buttons that are not hidden but do not fit on the toolbar.
		/// Filled in by <see cref="OnLayout"/> who determines which buttons have to be dropped.
		/// Valid after the first <see cref="OnLayout"/> call only.
		/// </summary>
		private HashSet _hashDropped = new HashSet();

		#endregion

		#region Construction

		public static WorkspaceButtonsManager GetInstance()
		{
			return _theInstance;
		}

		/// <summary>
		/// Initializes the object.
		/// </summary>
		/// <param name="parent">Indicates the user control that serves as a parent control for the
		/// controls that are positioned by this object.</param>
		public WorkspaceButtonsManager( WorkspaceButtonsRow parent )
		{
			_parent = parent;
			_theInstance = this;

			// Initialize the bar, but not before the core gets into the running state
			if( ICore.Instance != null )
			{
				if( Core.State == CoreState.Initializing || Core.State == CoreState.StartingPlugins )
					Core.StateChanged += OnCoreStateChanged; // Not running, wait for
				else if( Core.State == CoreState.Running )
					InitInstance(); // Running already, do the init
			}
		}

		#endregion

		#region Visual Init

		/// <summary>
		/// Initializes the persistent controls.
		/// </summary>
		private void InitializeComponentSelf()
		{
			_menuOnButton = new ContextMenu();
			_miConfigure = new MenuItem();
			_miHide = new MenuItem();
			//
			// _btnContextMenu
			//
			_menuOnButton.MenuItems.AddRange(new MenuItem[] { _miConfigure, _miHide } );
			//
			// miConfigure
			//
			_miConfigure.Index = 0;
			_miConfigure.Text = "&Configure…";
			_miConfigure.Click += this.OnConfigureButtonClick;
			//
			// miHideBtn
			//
			_miHide.Index = 1;
			_miHide.Text = "&Hide";
			_miHide.Click += this.OnHideButtonClick;
			//
			// _btnChevron
			//
			_btnChevron = new ImageListButton();
			_btnChevron.Name = "_btnChevron";
			_btnChevron.Click += OnChevronClick;
			_btnChevron.PressedImageIndex = -1;
			_btnChevron.Size = new Size(8, 16);
			_btnChevron.Visible = false;
			_btnChevron.AddIcon(ChevronBar.LoadChevronIcon(false), ImageListButton.ButtonState.Normal);
			_btnChevron.AddIcon(ChevronBar.LoadChevronIcon(true), ImageListButton.ButtonState.Hot);
			_btnChevron.BackColor = SystemColors.Control;
			//
			// _menuOnChevron
			//
			_menuOnChevron = new ContextMenu();
			//
			// _btnOrganize
			//
			_btnOrganize = new ImageListButton();
			_btnOrganize.Name = "_btnOrganize";
			_btnOrganize.Size = new Size(16, 16);
			_btnOrganize.TabIndex = 21;
			_btnOrganize.Click += this.OnOrganizeClick;
			_btnOrganize.Visible = false;
			_btnOrganize.Cursor = Cursors.Hand;
			_btnOrganize.AddIcon(new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("OmniaMea.Icons.Workspaces.Organize.Normal.ico")), ImageListButton.ButtonState.Normal);
			_btnOrganize.AddIcon(new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("OmniaMea.Icons.Workspaces.Organize.Hot.ico")), ImageListButton.ButtonState.Hot);
			_btnOrganize.BackColor = SystemColors.Control;
			_btnOrganize.ToolTip = "Organize Workspaces";
			//
			// WorkspaceBar
			//
			_parent.Controls.Add(_btnOrganize);
			_parent.Controls.Add(_btnChevron);
		}

		#endregion

		#region Events

		/// <summary>
		/// Fires when the active workspace chnages.
		/// </summary>
		public event ResourceEventHandler WorkspaceChanged;

		#endregion

		#region External Event Handlers

		// Handles of the events that come from outside

		/// <summary>
		/// <see cref="ICore.StateChanged"/>.
		/// Listens only for the Running state.
		/// </summary>
		private void OnCoreStateChanged( object sender, EventArgs e )
		{
			if( Core.State == CoreState.Running )
			{
				Core.StateChanged -= OnCoreStateChanged;
				InitInstance();
			}
		}

		/// <summary>
		/// When a new workspace is created, puts it in the beginning of the visibility list.
		/// </summary>
		private void OnWorkspaceAdded( object sender, ResourceIndexEventArgs e )
		{
			e.Resource.SetProp( _workspaceManager.Props.VisibleOrder, _workspaces.Count - 1 );
			AddWorkspaceButton( e.Resource );
			ReorderWorkspaceButtons();
			UpdateWorkspaceActions();
		}

		/// <summary>
		/// When a workspace is deleted, removes it from the visibility list and from the hidden workspaces list.
		/// </summary>
		private void OnWorkspaceDeleting( object sender, ResourceIndexEventArgs e )
		{
			RemoveWorkspaceButton( e.Resource );
			ReorderWorkspaceButtons();
			UpdateWorkspaceActions();
		}

		/// <summary>
		/// As the active workspace changes, applies the necessary changes to the UI.
		/// </summary>
		private void OnActiveWorkspaceChanged( object sender, EventArgs e )
		{
			// The new workspace
			IResource workspaceNew = _workspaceManager.ActiveWorkspace;

			// Unhide the workspace if it were hidden (only if this is not the default workspace)
			if( (workspaceNew != null) && (workspaceNew.HasProp( _workspaceManager.Props.WorkspaceHidden )) )
				new ResourceProxy( workspaceNew ).DeleteProp( _workspaceManager.Props.WorkspaceHidden );

			// Notify of the change
			try
			{
				if( WorkspaceChanged != null )
					WorkspaceChanged( this, new ResourceEventArgs( workspaceNew ) );
			}
			catch( Exception ex )
			{ // Trap and report instantly the event-handler exceptions
				Core.ReportException( ex, ExceptionReportFlags.AttachLog );
			}

			// Cause relayouting of the buttons (this will also repaint them to indicate the updated "Active" state)
			if( _site != null )
				_site.PerformLayout( this );
		}

		#endregion

		#region Internal Event Handlers

		// Handlers of the events fired by things that are managed by this object

		/// <summary>
		/// When the "+" button is clicked, shows the workspace setup dialog.
		/// </summary>
		private void OnOrganizeClick( object sender, EventArgs e )
		{
			ShowWorkspacesDialog( null );
		}

		/// <summary>
		/// Listens to clicks on the chevron button and selects the workspace that was clicked.
		/// </summary>
		private void OnChevronClick( object sender, EventArgs e )
		{
			if( _workspaceManager == null ) // Not initialized yet?
				return;

			// Clear the chevron menu
			_menuOnChevron.MenuItems.Clear();

			bool bSeparator = false; // Tells whether to insert a separator between the hidden and unfit workspace groups (which happens in case they both are non-empty)

			// Add the unfit workspaces
			if( _hashDropped.Contains( _hashWorkspaceButtons[ DBNull.Value ] ) ) // Check if the default workspace did not fit
			{
				_menuOnChevron.MenuItems.Add( new WorkspaceMenuItem( null ) ); // Add the default workspace
				bSeparator = true;
			}

			lock( _workspaces )
			{
				// We enumerate workspaces and check each in order to have the items from HashSet sorted properly
				foreach( IResource workspace in _workspaces.ValidResources )
				{
					// The dropped hash contains buttons not workspaces, convert to button first
					if( (_hashWorkspaceButtons.ContainsKey( workspace )) && (_hashDropped.Contains( _hashWorkspaceButtons[ workspace ] )) )
					{
						_menuOnChevron.MenuItems.Add( new WorkspaceMenuItem( workspace ) );
						bSeparator = true;
					}
				}
			}

			// Add the hidden workspaces
			IResourceList resHidden = Core.ResourceStore.FindResourcesWithProp( _workspaceManager.Props.WorkspaceResourceType, _workspaceManager.Props.WorkspaceHidden );
			lock( resHidden )
			{
				foreach( IResource workspace in resHidden.ValidResources )
				{
					// The active workspace cannot be the hidden one
					if( workspace == _workspaceManager.ActiveWorkspace )
						continue;

					// Add the separator if both groups are non-empty
					if( bSeparator )
					{
						bSeparator = false;
						_menuOnChevron.MenuItems.Add( new MenuItem( "-" ) );
					}

					// Add the workspace menu item
					_menuOnChevron.MenuItems.Add( new WorkspaceMenuItem( workspace ) );
				}
			}

			// Show the menu, in case there are items for it
			if( _menuOnChevron.MenuItems.Count != 0 )
				_menuOnChevron.Show( _parent, new Point( _btnChevron.Right, _btnChevron.Bottom ) );
		}

		/// <summary>
		/// For a left button, activates the workspace of the clicked button.
		/// When the right mouse button is clicked over the workspace button, remembers
		/// which button it was, so that the context menu would apply to this button.
		/// </summary>
		private void OnWorkspaceButtonMouseDown( object sender, MouseEventArgs e )
		{
			WorkspaceButton button = (WorkspaceButton) sender; // Button that has been clicked

			switch( e.Button )
			{
			case MouseButtons.Right: // Right-click causes the button context menu to show
				_resContextMenuWorkspace = button.Workspace;
				_miHide.Enabled = button.Workspace != null;
				break;
			}
		}

		/// <summary>
		/// A workspace button has been clicked in the row. Jump to that workspace.
		/// Also, if an unread counter or icon for a specific resource type has been clicked, jump to that tab in that workspace.
		/// </summary>
		private void OnWorkspaceButtonClicked( object sender, WorkspaceButton.WorkspaceClickedEventArgs args )
		{
			if( _workspaceManager == null ) // Not initialized yet?
				return;

			// Try activating the workspace represented by the clicked button
			// If it was not active yet, it will be activated and the notification event from the manager will cause all the necessary UI updates
			_workspaceManager.ActiveWorkspace = args.Workspace;
			Trace.WriteLine( String.Format( "Selecting \"{0}\" workspace because a workspace button has been clicked.", (args.Workspace != null ? args.Workspace.DisplayName : _workspaceManager.Props.DefaultWorkspaceName) ), "[WBM]" );

			// If user has clicked on an unread counter, open the tab appropriate (if avail)
			if( args.UnreadResourceType != null )
			{
				// Lookup a tab id for this resource type (whose unread counter has been clicked)
				string sTargetTabId = Core.TabManager.FindResourceTypeTab( args.UnreadResourceType );

				// If failed, try picking a tab for the first of the clicked resources
				if( ( (sTargetTabId == null) || (sTargetTabId.Length == 0) ) && (args.UnreadClickedResources != null) && (args.UnreadClickedResources.Count != 0) )
				{
					Trace.WriteLine( String.Format( "Choosing a tab using a list of unread resources, specifically res \"{0}\"#{1}.", args.UnreadClickedResources[ 0 ], args.UnreadClickedResources[ 0 ].OriginalId ), "[WBM]" );
					sTargetTabId = Core.TabManager.GetResourceTab( args.UnreadClickedResources[ 0 ] );
				}

				// Use the target TAB id, if available
				if( (sTargetTabId != null) && (sTargetTabId.Length != 0) ) // May be Null if the resource-type has no dedicated tab
				{ // There's a tab dedicated to that resource type
					Trace.WriteLine( String.Format( "A workspace button has been clicked on the {0}'s unread counter, now jumping to its tab with ID \"{1}\".", args.UnreadResourceType, sTargetTabId ), "[WBM]" );
					Core.TabManager.ActivateTab( sTargetTabId );
				}
				else
					Trace.WriteLine( String.Format( "A workspace button has been clicked on the {0}'s unread counter, but this resource type has no dedicated tab, and the resource list for that unread counter is not available. No tab switch will be done, just switching to the workspace.", args.UnreadResourceType ), "[WBM]" );
			}
		}

		/// <summary>Shows the Workspaces dialog and selects the specified workspace.</summary>
		private void OnConfigureButtonClick( object sender, EventArgs e )
		{
			ShowWorkspacesDialog( _resContextMenuWorkspace );
		}

		/// <summary>
		/// Hides the clicked workspace.
		/// </summary>
		private void OnHideButtonClick( object sender, EventArgs e )
		{
			if( _resContextMenuWorkspace != null )
				HideWorkspace( _resContextMenuWorkspace );
		}

		#endregion

		#region Operations

		/// <summary>
		/// Shows the workspace configuration dialog and selects the specified workspace.
		/// </summary>
		internal void ShowWorkspacesDialog( IResource selectedWorkspace )
		{
			if( Core.State == CoreState.Initializing )
				return;

			// Show the dialog
			using( WorkspacesDlg dlg = new WorkspacesDlg() )
			{
				if( selectedWorkspace != null )
					dlg.SelectWorkspace( selectedWorkspace );
				dlg.ShowDialog( _parent.FindForm() );
			}

			// Apply changes
			_site.PerformLayout( this );
		}

		public void ActivateNextWorkspace( bool bForward )
		{
			lock( _workspaces )
			{
				// Index of the currently selected workspace, or -1 if Default or unavail
				int nCurrentIndex = _workspaceManager.ActiveWorkspace != null ? _workspaces.IndexOf( _workspaceManager.ActiveWorkspace ) : -1;

				IResource resNew = null; // The new workspace

				if( bForward ) // Look at the right
				{
					for( int nIndex = nCurrentIndex + 1; (nIndex < _workspaces.Count) && (resNew == null); nIndex++ )
					{
						if( !_workspaces[ nIndex ].HasProp( _workspaceManager.Props.WorkspaceHidden ) )
							resNew = _workspaces[ nIndex ];
					}
				}
				else // Look at the left
				{
					if( nCurrentIndex == -1 )
						nCurrentIndex = _workspaces.Count;
					for( int nIndex = nCurrentIndex - 1; (nIndex >= 0) && (resNew == null); nIndex-- )
					{
						if( !_workspaces[ nIndex ].HasProp( _workspaceManager.Props.WorkspaceHidden ) )
							resNew = _workspaces[ nIndex ];
					}
				}

				// Select the adjacent resource, or Null for the Default workspace if it was not found
				Core.WorkspaceManager.ActiveWorkspace = resNew;
			}
		}

		public bool CanActivateNextWorkspace()
		{
			// If there are such workspaces that do not have the "hidden" property, then there's some space for switching
			return Core.ResourceStore.GetAllResources( _workspaceManager.Props.WorkspaceResourceType ).Minus( Core.ResourceStore.FindResourcesWithProp( _workspaceManager.Props.WorkspaceResourceType, _workspaceManager.Props.WorkspaceHidden ) ).Count != 0; // TODO: use some constant for the resource type
		}

		/// <summary>
		/// Performs layout of the controls of which this object is in charge.
		/// Those are, all the workspace buttons (some of which are hidden), as well as the Organize button,
		/// and the chevron (if necessary).
		/// </summary>
		/// <param name="rcBounds">The bound rect of the area into which the controls should be layouted.</param>
		/// <param name="borders">
		/// <para>X-coordinates of the left side of the border spacing
		/// that should be drawn around the active workspace button.</para>
		/// <para>There must be exactly two of them.</para>
		/// <para>This includes the around-border horizontal padding as well.</para>
		/// </param>
		/// <param name="separators">
		/// <para>Zero or more X-coordinates of the left side of the separators' spacing
		/// that should be inserted between each pair of controls in case non of them is an
		/// active workspace button (in which case a border is drawn, see <paramref name="borders"/>).</para>
		/// <para>This includes the around-border horizontal padding as well.</para>
		/// </param>
		/// <returns>Whether the component has successfully layouted itself.</returns>
		public bool OnLayout( Rectangle rcBounds, out int[] borders, out int[] separators )
		{
			// Start with an assumption that all the controls will fit
			int nWidth = MaxSize.Width;
			Size sizeMin = MinSize;
			_hashDropped.Clear();

			// If there's not enough space for an adequate layouting, hide all the controls and do nothing to the return arrays
			// Also, cancel layouting if not initialized yet
			if( (rcBounds == Rectangle.Empty) || (rcBounds.Width < sizeMin.Width) || (rcBounds.Height < sizeMin.Height) || (_hashWorkspaceButtons == null) || (_workspaceManager == null) )
			{
				borders = new int[0];
				separators = new int[0];
				if( _hashWorkspaceButtons != null )
				{
					foreach( WorkspaceButton button in _hashWorkspaceButtons.Values )
						button.Visible = false;
				}
				if( _btnOrganize != null )
					_btnOrganize.Visible = false;
				if( _btnChevron != null )
					_btnChevron.Visible = false;

				return false; // Layout failed
			}

			// Indicates presence of the chevron button in the current vision of the layout.
			// As we start from the MaxSize version, we add a chevron only if there are hidden workspaces to be shown under it.
			// In this case its width (along with sep) is already encountered in the MaxSize.
			bool bChevronPresent = HaveHiddenWorkspaces;

			// Total list of workspace buttons that are not manually hidden
			ArrayList arWorkspaceButtons = new ArrayList();
			// Fill the list and also process the hidden buttons here
			arWorkspaceButtons.Add( _hashWorkspaceButtons[ DBNull.Value ] ); // Default workspace
			lock( _workspaces )
			{
				foreach( IResource workspace in _workspaces.ValidResources )
				{
					if( !_hashWorkspaceButtons.ContainsKey( workspace ) ) // Race condition?
						continue;
					WorkspaceButton button = (WorkspaceButton) _hashWorkspaceButtons[ workspace ]; // The corresponding workspace button

					// Visibles list
					if( (!workspace.HasProp( _workspaceManager.Props.WorkspaceHidden )) || (button.Active) )
						arWorkspaceButtons.Add( button ); // Not hidden or active, include in possibly-visibles list
					else
						button.Visible = false; // Hidden, set invisible, dont include in the list
				}
			}

			// Start dropping off the workspace buttons that do not fit, from right to the left, skipping the active wsp button
			int nDropPosition = arWorkspaceButtons.Count - 1; // Points to the control that should be dropped next
			for(; (nWidth > rcBounds.Width) && (nDropPosition >= 0); nDropPosition-- )
			{
				// Check if we have to add a chevron (with one more separator) � this works on the first step only
				if( !bChevronPresent )
				{
					nWidth += _btnChevron.Width; // Chevron
					nWidth += WorkspaceButtonsRow.Const.HorSpacingWhenSeparator; // Separator before the chevron
					bChevronPresent = true;
				}

				// Can not drop the active button
				if( ((WorkspaceButton) arWorkspaceButtons[ nDropPosition ]).Active )
					continue;

				// Exclude the rightmost not excluded yet button, as well as one separator along with it
				WorkspaceButton buttonDrop = (WorkspaceButton) arWorkspaceButtons[ nDropPosition ];
				nWidth -= buttonDrop.Width;
				nWidth -= WorkspaceButtonsRow.Const.HorSpacingWhenSeparator;
				_hashDropped.Add( buttonDrop );
			}

			// Could not fit � not enough controls to drop
			if( nDropPosition < 0 )
				Trace.WriteLine( "Not enought space to layout the controls properly.", "[WBM]" );

			// Apply the layout
			ArrayList arBorders = new ArrayList();
			ArrayList arSeparators = new ArrayList();
			int nCurPos = rcBounds.Left;
			bool bPrevButtonActive = false; // If this is not the first button, indicates whether the prev one was active or not, this defines whether a border or a separator should be inserted before the current button

			// Add the fitting buttons (separators are added BEFORE the button, if it's not the first one)
			foreach( WorkspaceButton button in arWorkspaceButtons )
			{
				// Hide the dropped controls (and they do not affect the bPrevButtonActive-like properties)
				if( _hashDropped.Contains( button ) )
				{
					button.Visible = false;
					continue;
				}

				////
				// If the control is not dropped, show it

				// Add either separator or a border before the button
				if( (bPrevButtonActive) || (button.Active) ) // Prev or this button is active, draw a border
				{
					arBorders.Add( nCurPos );
					nCurPos += WorkspaceButtonsRow.Const.HorSpacingWhenBorder;
				}
				else // Neither is active, draw a mere separator
				{
					arSeparators.Add( nCurPos );
					nCurPos += WorkspaceButtonsRow.Const.HorSpacingWhenSeparator;
				}

				// Add the button itself
				button.Location = new Point( nCurPos, rcBounds.Top + (rcBounds.Height - button.Height) / 2 ); // Hor-layout, ver-center
				button.Visible = true;
				nCurPos += button.Width;

				// Update the prev-button flags
				bPrevButtonActive = button.Active;
			}

			// Draw either a separator or a border after the last button, depending on whether it was active or not
			if( bPrevButtonActive ) // Prev button is active, draw a border
			{
				arBorders.Add( nCurPos );
				nCurPos += WorkspaceButtonsRow.Const.HorSpacingWhenBorder;
			}
			else // Neither is active, draw a mere separator
			{
				arSeparators.Add( nCurPos );
				nCurPos += WorkspaceButtonsRow.Const.HorSpacingWhenSeparator;
			}

			// The Organize button
			_btnOrganize.Location = new Point( nCurPos, rcBounds.Top + (rcBounds.Height - _btnOrganize.Height) / 2 ); // Hor-layout, ver-center
			_btnOrganize.Visible = true;
			nCurPos += _btnOrganize.Width;

			// A chevron button with a separator before it, if needed
			if( bChevronPresent )
			{
				// Separator
				arSeparators.Add( nCurPos );
				nCurPos += WorkspaceButtonsRow.Const.HorSpacingWhenSeparator;

				// Chevron button itself
				_btnChevron.Location = new Point( nCurPos, rcBounds.Top + (rcBounds.Height - _btnChevron.Height) / 2 ); // Hor-layout, ver-center
				_btnChevron.Visible = true;
				nCurPos += _btnChevron.Width;
			}
			else
				_btnChevron.Visible = false;

			// Pass out the layout lists

			// Validness check � the number of borders around the active WSP button
			// Zero borders may happen in case there's no active workspace among the buttons (eg a race condition � the active wsp's button has not be added to the controls yet)
			// One border may happen if the active workspace changes while enumerating the workspace buttons and we add only one border; the same for three and so on
			if( arBorders.Count != 2 )
			{
				Trace.WriteLine( String.Format( "There must be either exactly two borders (found {0}).", arBorders.Count ), "[WBM]" );
				arBorders.Clear(); // Validate � treat as if there were no borders at all
			}
			borders = (int[]) arBorders.ToArray( typeof( int ) );
			separators = (int[]) arSeparators.ToArray( typeof( int ) );

			// Report
			Trace.WriteLine( String.Format( "Layouted the workspace buttons. Buttons: {2}, Borders: {0}, Separators: {1}.", borders.Length, separators.Length, arWorkspaceButtons.Count ), "[WBM]" );

			return true; // Layout successful
		}

		/// <summary>
		/// Hides the workspace specified by placing it under the chevron.
		/// Activates some other unhidden workspace if the active workspace is being hidden.
		/// If there are no suitable workspaces to activate, unhides and activates the default workspace.
		/// </summary>
		/// <param name="workspace">The workspace to hide.</param>
		public void HideWorkspace( IResource workspace )
		{
			// If the button being hidden represents the active workspace; should activate some other workspace instead
			if( workspace == _workspaceManager.ActiveWorkspace )
				ActivateNextWorkspace( true );

			// TODO: remove
			/*
			{

					// Look for the closest workspace which is not hidden and can be activated
					IResource resNewWorkspace = null;
				lock(_workspaces)
				{
					int nCurrentIndex = _workspaces.IndexOf( workspace );
					nCurrentIndex = nCurrentIndex >= 0 ? nCurrentIndex : 0; // Validate in case there were problems in finding the index

					// Look to the right
					for( int nIndex = nCurrentIndex + 1; (nIndex < _workspaces.Count) && (resNewWorkspace == null); nIndex++ )
					{
						if( !_workspaces[nIndex].HasProp( _workspaceManager.Props.WorkspaceHidden ) )
							resNewWorkspace = _workspaces[nIndex];
					}
					// Look to the left
					for( int nIndex = 0; (nIndex < nCurrentIndex) && (resNewWorkspace == null); nIndex++ )
					{
						if( !_workspaces[nIndex].HasProp( _workspaceManager.Props.WorkspaceHidden ) )
							resNewWorkspace = _workspaces[nIndex];
					}
					// If it's still Null (eg all the other workspaces are hidden/nonexistent), this will represent the default workspace, which is quite OK
				}

				// Select the workspace instead of the hidden one; the event handler will update the UI accordingly
				_workspaceManager.ActiveWorkspace = resNewWorkspace;
			}
			*/

			// Hide the workspace represented by the button
			new ResourceProxy( workspace ).SetPropAsync( _workspaceManager.Props.WorkspaceHidden, true );

			_site.PerformLayout( this );
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Determines whether there are such workspaces whose buttons are hidden from the view.
		/// Helps in checking whether the chevron button should be visible or not.
		/// </summary>
		public bool HaveHiddenWorkspaces
		{
			get
			{
				if( _workspaceManager == null ) // Not initialized yet?
					return false;

				// Get the number of hidden workspaces
				int nHiddenCount = Core.ResourceStore.FindResourcesWithProp( _workspaceManager.Props.WorkspaceResourceType, _workspaceManager.Props.WorkspaceHidden ).Count;

				// As the active workspace cannot be hidden, check if it was in the list and exclude it, as necessary
				if( (_workspaceManager.ActiveWorkspace != null) && (_workspaceManager.ActiveWorkspace.HasProp( _workspaceManager.Props.WorkspaceHidden )) )
					nHiddenCount--;

				return nHiddenCount > 0;
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// (Re)initializes the workspace buttons when this object is created or Omea finishes starting up.
		/// </summary>
		private void InitInstance()
		{
			if( !Core.UserInterfaceAP.IsOwnerThread )
			{
				Core.UserInterfaceAP.QueueJob( "Initialize the Workspace Buttons Manager", new MethodInvoker( InitInstance ) );
				return;
			}

			// Init data structures
			_workspaceManager = Core.WorkspaceManager as WorkspaceManager;
			_workspaces = Core.ResourceStore.GetAllResourcesLive( _workspaceManager.Props.WorkspaceResourceType );
			_workspaces.Sort( new int[] {_workspaceManager.Props.VisibleOrder}, true );

			// Wire up the events
			_workspaces.ResourceAdded += OnWorkspaceAdded;
			_workspaces.ResourceDeleting += OnWorkspaceDeleting;
			_workspaceManager.WorkspaceChanged += OnActiveWorkspaceChanged;

			// Init the UI
			InitializeComponentSelf();

			// Create the buttons
			_parent.SuspendLayout();
			try
			{
				_hashWorkspaceButtons = new Hashtable();
				CreateWorkspaceButtons();
			}
			finally
			{
				_parent.ResumeLayout();
			}

			// Misc
			UpdateWorkspaceActions();
			if( _site != null ) // Needed?
				_site.PerformLayout( this );
		}

		/// <summary>
		/// Builds the visibility list of workspaces based on information from the resource store.
		/// </summary>
		private void CreateWorkspaceButtons()
		{
			if( _hashWorkspaceButtons.Count != 0 )
				throw new InvalidOperationException( "The workspace buttons have already been created." );

			// Add a button for the default workspace
			AddWorkspaceButton( null );

			// Create a button for each of the workspaces
			lock( _workspaces )
			{
				foreach( IResource workspace in _workspaces.ValidResources )
					AddWorkspaceButton( workspace );
			}
		}

		/// <summary>Registers the actions for activating the workspaces.</summary>
		private void UpdateWorkspaceActions()
		{
			Core.UserInterfaceAP.QueueJob( "Update Workaspace UI Actions", new MethodInvoker( UpdateWorkspaceActions_Impl ) );
		}

		/// <summary>
		/// Deferred Implementation of the <see cref="UpdateWorkspaceActions"/> function.
		/// </summary>
		private void UpdateWorkspaceActions_Impl()
		{
			if( (_defaultWorkspaceAction == null) && (_workspaceManager != null) )
			{
				_defaultWorkspaceAction = new ActivateWorkspaceAction( null );
				Core.ActionManager.RegisterMainMenuAction( _defaultWorkspaceAction, "WorkspaceSelectActions", ListAnchor.Last,
				                                           _workspaceManager.Props.DefaultWorkspaceName, null, null, null );
			}

			// Remove the old actions
			foreach( IAction action in _workspaceActions )
			{
				Core.ActionManager.UnregisterMainMenuAction( action );
			}
			_workspaceActions.Clear();

			// Add the new actions
			lock( _workspaces )
			{
				foreach( IResource workspace in _workspaces.ValidResources )
				{
					IAction wsAction = new ActivateWorkspaceAction( workspace );
					_workspaceActions.Add( wsAction );
					Core.ActionManager.RegisterMainMenuAction( wsAction, "WorkspaceSelectActions", ListAnchor.Last,
					                                           workspace.DisplayName, null, null, null );
				}
			}
		}

		/// <summary>Creates a single workspace button and sets up its parameters and adds it to the containers appropriate.</summary>
		/// <param name="workspace">Workspace to attach to the button, may be <c>Null</c> for the default workspace.</param>
		/// <remarks>This function does not cause updates of any kind to happen.</remarks>
		private void AddWorkspaceButton( IResource workspace )
		{
			// Marshal to the UI thread, if needed
			if( !Core.UserInterfaceAP.IsOwnerThread )
			{
				Core.UserInterfaceAP.QueueJob( "Add Workspace Button", new ResourceDelegate( AddWorkspaceButton ), workspace );
				return;
			}

			// Create the button
			WorkspaceButton button = new WorkspaceButton(workspace );
			object key = workspace ?? (object) DBNull.Value;
			if( _hashWorkspaceButtons.ContainsKey( key ) )
				throw new InvalidOperationException( String.Format( "There's already a button for the workspace \"{0}\"#{1}.", workspace.DisplayName, workspace.Id ) );

			// Configure the button
			button.BackColor = SystemColors.Control;
			button.Height = 23; // TODO: height?
			button.Visible = false; // Will be updated to the actual value by the OnLayout code
			button.ContextMenu = _menuOnButton;
			button.MouseDown += OnWorkspaceButtonMouseDown;
			button.WorkspaceClicked += OnWorkspaceButtonClicked;

			// Add to the containers
			_parent.Controls.Add( button );
			_hashWorkspaceButtons[ key ] = button;
		}

		/// <summary>
		/// Removes a workspace button from all the corresponding lists.
		/// </summary>
		/// <param name="workspace">Workspace attached to the button.</param>
		private void RemoveWorkspaceButton( IResource workspace )
		{
			// Marshal to the UI thread, if needed
			if( !Core.UserInterfaceAP.IsOwnerThread )
			{
				Core.UserInterfaceAP.QueueJob( "Add Workspace Button", new ResourceDelegate( RemoveWorkspaceButton ), workspace );
				return;
			}

			if( workspace == null )
				throw new ArgumentNullException();

			if( !_hashWorkspaceButtons.ContainsKey( workspace ) )
			{
				Trace.WriteLine( String.Format( "Trying to remove a nonexistent workspace \"{0}\"#{1}.", workspace.DisplayName, workspace.OriginalId ), "[WBM]" );
				return;
			}

			// Remove form the containers
			if( _hashWorkspaceButtons.ContainsKey( workspace ) ) // Race condition?
			{
				_parent.Controls.Remove( (Control) _hashWorkspaceButtons[ workspace ] );
				_hashWorkspaceButtons.Remove( workspace );
			}

			_site.PerformLayout( this );
		}

		/// <summary><seealso cref="ReorderWorkspaceButtons_Impl"/>
		/// Updates the workspace buttons order so that their visibility order values would be all unique, contiguous, and starting from zero.
		/// </summary>
		/// <remarks>Executes on the Resource thread, does marshalling if needed.</remarks>
		private void ReorderWorkspaceButtons()
		{
			Core.ResourceAP.QueueJobAt( DateTime.Now.AddMilliseconds( 500 ), "Reordering Workspace Buttons", new MethodInvoker( ReorderWorkspaceButtons_Impl ) );
		}

		/// <summary>
		/// Implements the deferred execution of the ReorderWorkspaceButtons task.
		/// </summary>
		private void ReorderWorkspaceButtons_Impl()
		{
			// First, take the list of those workspaces that are valid
			// (do not modify workspaces right on the list as it may change their order in case there are dupe values)
			ArrayList listWorkspaces = new ArrayList( _workspaces.ResourceIds );

			// Now reapply the numbering
			IResource workspace;
			for( int a = 0; a < listWorkspaces.Count; a++ )
			{
				if( ((workspace = Core.ResourceStore.TryLoadResource( (int) listWorkspaces[ a ] )) != null) // If the resource still exists
					&& (workspace.GetIntProp( _workspaceManager.Props.VisibleOrder ) != a) ) // And its property has to be changed
					workspace.SetProp( _workspaceManager.Props.VisibleOrder, a );
			}

			_site.PerformLayout( this );
		}

		/// <summary>
		/// Calculates the size when the minimum number of workspace buttons is shown (one button for the current workspace).
		/// </summary>
		protected Size MinButtonsSize
		{
			get
			{
				// Not initialized yet?
				if( (_hashWorkspaceButtons == null) || (_workspaceManager == null) )
					return Size.Empty;

				// Minimum size: one workspace button (the active one, actually), plus the Organize button and the chevron
				int nTotalWidth = 0;
				int nMaxHeight = 0;

				// Add the active borders, as there are always two of them
				nTotalWidth += WorkspaceButtonsRow.Const.HorSpacingWhenBorder * 2;
				// There will be no separators if there's no chevron, or one separator if there is one

				// Pick the active workspace button
				object key = _workspaceManager.ActiveWorkspace ?? (object) DBNull.Value; // Workspace key in the hash
				if( _hashWorkspaceButtons.ContainsKey( key ) ) // Race condition?
				{
					WorkspaceButton button = (WorkspaceButton) _hashWorkspaceButtons[ key ];
					nTotalWidth += button.Width;
					nMaxHeight = nMaxHeight >= button.Height ? nMaxHeight : button.Height;
				}

				// Add the Organize button
				nTotalWidth += _btnOrganize.Width;
				nMaxHeight = nMaxHeight >= _btnOrganize.Height ? nMaxHeight : _btnOrganize.Height;

				// Add the chevron (if it's present)
				if( (HaveHiddenWorkspaces) // If there are hidden workspaces, the chevron should be present
					|| (_workspaces.Count > 0) ) // If the active workspace is not the only one existing (which may happen only if there's just the default workspace available) � then the chevron would be present in the min-size case
				{ // Then the chevron is present
					nTotalWidth += _btnChevron.Width;
					nMaxHeight = nMaxHeight >= _btnChevron.Height ? nMaxHeight : _btnChevron.Height;
					nTotalWidth += WorkspaceButtonsRow.Const.HorSpacingWhenSeparator; // A separator before the chevron and after the Organize button
				}

				return new Size( nTotalWidth, nMaxHeight );
			}
		}

		/// <summary>
		/// Calculates the size when the maximum number of workspace buttons is shown (but for the hidden buttons).
		/// </summary>
		protected Size MaxButtonsSize
		{
			get
			{
				// Not initialized yet?
				if( (_hashWorkspaceButtons == null) || (_workspaceManager == null) )
					return Size.Empty;

				// Maximum size: all the workspace buttons available, plus the Organize button; no chevron
				int nTotalWidth = 0;
				int nMaxHeight = 0;

				// Add the active borders, as there are always two of them
				nTotalWidth += WorkspaceButtonsRow.Const.HorSpacingWhenBorder * 2;

				///////
				// Add width of all the workspace buttons, also count them to determine the number of separators needed

				// Add the Default button
				int nUnhiddenButtons = 1; // Add the Default workspace by default
				WorkspaceButton btnDefault = (WorkspaceButton) _hashWorkspaceButtons[ DBNull.Value ];
				nTotalWidth += btnDefault.Width;
				nMaxHeight = nMaxHeight >= btnDefault.Height ? nMaxHeight : btnDefault.Height;

				// Add non-Default buttons
				lock( _workspaces )
				{
					foreach( IResource workspace in _workspaces.ValidResources )
					{
						// Skip hidden buttons
						if( (workspace.HasProp( _workspaceManager.Props.WorkspaceHidden )) && (_workspaceManager.ActiveWorkspace != workspace) )
							continue;

						// Add this button to the list
						if( _hashWorkspaceButtons.ContainsKey( workspace ) ) // Race condition?
						{
							WorkspaceButton button = (WorkspaceButton) _hashWorkspaceButtons[ workspace ];
							nTotalWidth += button.Width;
							nMaxHeight = nMaxHeight >= button.Height ? nMaxHeight : button.Height;
							nUnhiddenButtons++;
						}
					}
				}

				// Add the separators, their number depends on the max-number (= total-number) of wsp buttons and whether the active is the first one
				nTotalWidth += WorkspaceButtonsRow.Const.HorSpacingWhenSeparator * (nUnhiddenButtons - 1);

				// Add the "Organize" button (its separator is already taken into account)
				nTotalWidth += _btnOrganize.Width;
				nMaxHeight = nMaxHeight >= _btnOrganize.Height ? nMaxHeight : _btnOrganize.Height;

				// Add the Chevron button (in case there are hidden workspaces that cause the chevron to appear even in the maxsize)
				if( HaveHiddenWorkspaces )
				{ // Then the chevron is present
					nTotalWidth += _btnChevron.Width;
					nMaxHeight = nMaxHeight >= _btnChevron.Height ? nMaxHeight : _btnChevron.Height;
					nTotalWidth += WorkspaceButtonsRow.Const.HorSpacingWhenSeparator; // Along with its separator
				}

				return new Size( nTotalWidth, nMaxHeight );
			}
		}

		#endregion

		#region ICommandBar Interface Members

		public void SetSite( ICommandBarSite site )
		{
			_site = site;
		}

		public Size MinSize
		{
			get
			{
				// In case of all-buttons the chevron disappears, evaluate whichever is smaller/larger
				Size	sizeMinButtons = MinButtonsSize;
				Size	sizeMaxButtons = MaxButtonsSize;
				return sizeMinButtons.Width < sizeMaxButtons.Width ? sizeMinButtons : sizeMaxButtons;
			}
		}

		public Size MaxSize
		{
			get
			{
				// In case of all-buttons the chevron disappears, evaluate whichever is smaller/larger
				Size	sizeMinButtons = MinButtonsSize;
				Size	sizeMaxButtons = MaxButtonsSize;
				return sizeMinButtons.Width > sizeMaxButtons.Width ? sizeMinButtons : sizeMaxButtons;
			}
		}

		public Size OptimalSize
		{
			get
			{
				return MaxSize; // Tend to having the maximum size to show all the buttons
			}
		}

		public Size Integral
		{
			get { return new Size( 1, 1 ); }
		}

		#endregion

		#region Class WorkspaceMenuItem — A class for menu items that select a workspace upon clicking.

		/// <summary>
		/// A class for menu items that select a workspace upon clicking.
		/// </summary>
		internal class WorkspaceMenuItem : MenuItem
		{
			#region Construction

			/// <summary>
			/// Constructs the menu item by fetching its text from the workspace given.
			/// </summary>
			/// <param name="workspace"></param>
			public WorkspaceMenuItem( IResource workspace )
			{
				_workspace = workspace;
				Text = workspace != null ? workspace.DisplayName : ((WorkspaceManager) Core.WorkspaceManager).Props.DefaultWorkspaceName;
			}

			#endregion

			#region Data

			/// <summary>
			/// The workspace attached to the menu item.
			/// </summary>
			private IResource _workspace;

			#endregion

			#region Attributes

			/// <summary>
			/// Gets or sets the workspace attached to the menu item.
			/// </summary>
			public IResource Workspace
			{
				get { return _workspace; }
				set { _workspace = value; }
			}

			#endregion

			#region Overrides

			/// <summary>
			/// Handles the menu item clicks.
			/// </summary>
			protected override void OnClick( EventArgs e )
			{
				Core.WorkspaceManager.ActiveWorkspace = _workspace;
			}

			#endregion
		}

		#endregion
	}
}
