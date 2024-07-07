// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using JetBrains.Interop.WinApi;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.Components.ImageListButton;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.Workspaces
{
	/// <summary>
	/// The button for switching workspaces, with support for showing unread counters for the workspace.
	/// </summary>
	internal class WorkspaceButton : UserControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		/// <summary>
		/// The workspace attached to the button.
		/// <c>Null</c> in the case of default workspace.
		/// </summary>
		private IResource _workspace;

		/// <summary>
		/// The same <see cref="_workspace"/>, but in the form of a live list we're listening to events of.
		/// Used only in case <see cref="_workspace"/> is non-null.
		/// </summary>
		private IResourceList _workspaceLive;

		private IResourceList _workspaceResources;

		private IResourceList _unreadResources;

		private WorkspaceManager _manager;

		/// <summary>
		/// resource type -> counter
		/// Maps the resource type string name to the number of unread resources for it in the current workspace.
		/// </summary>
		private Hashtable _unreadCounters = new Hashtable();

		/// <summary>
		/// Font that is used for painting the counters.
		/// </summary>
		private Font _fontCounter;

		private bool _unreadCountChanged = false;

		private bool _widthChanged = false;

		private bool _workspaceResourcesChanged = false;

		/// <summary>
		/// Indicates whether the item is hot (hovered by the mouse or has keyboard focus).
		/// </summary>
		private bool _hot = false;

		/// <summary>
		/// Handle to the font that is used for painting the counters.
		/// </summary>
		private IntPtr _hFontCounter;

		private ColorScheme _colorScheme;

		/// <summary>
		/// Gap between the controls or items painted.
		/// </summary>
		protected readonly int _nGap = 6;

		/// <summary>
		/// Gap between the icon and its text.
		/// </summary>
		protected readonly int _nIconTextGap = 3;

		/// <summary>
		/// Tooltip of this control.
		/// </summary>
		protected ToolTip _tooltip;

		/// <summary>
		/// If an unread resources icon or counter is hovered, contains the corresponding resource type name.
		/// <c>Null</c> if no such thing hovered.
		/// </summary>
		protected string _sUnreadResTypeHovered = null;

		/// <summary>
		/// Creates a workspace button instance.
		/// </summary>
		/// queried of some additional info.</param>
		/// <param name="workspace">Workspace resource to which this button is attached,
		/// or <c>Null</c> in case of the default workspace. The workspace cannot be changed at runtime.</param>
		public WorkspaceButton(IResource workspace)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponentSelf();

			_fontCounter = Font;

			_hFontCounter = _fontCounter.ToHfont();

			if(ICore.Instance != null)
			{
				Core.ResourceAP.JobFinished += OnResourceOperationFinished;
				_manager = Core.WorkspaceManager as WorkspaceManager;
			}

			// Apply the workspace added in
			_workspace = workspace;
			if(_workspace != null) // Subscribe to the workspace changes if it's not the default one
			{
				_workspaceResources = _manager.GetWorkspaceResourcesLive(_workspace, null);
				_workspaceResources.ResourceAdded += OnWorkspaceResourcesChanged;
				_workspaceResources.ResourceDeleting += OnWorkspaceResourcesChanged;

				// Convert the workspace to a live resource list and subscribe to its events
				_workspaceLive = _workspace.ToResourceListLive();
				_workspaceLive.ResourceChanged += OnWorkspaceChanged;
			}
			UpdateUnreadResourceList();
			UpdateButtonWidth();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(_unreadResources != null)
				{
					_unreadResources.Dispose();
				}
				if(components != null)
					components.Dispose();
			}
			Win32Declarations.DeleteObject(_hFontCounter);
			Core.ResourceAP.JobFinished -= OnResourceOperationFinished;
			base.Dispose(disposing);
		}

		#region Visual Init

		/// <summary>
		/// Visual Init.
		/// </summary>
		private void InitializeComponentSelf()
		{
			components = new Container();
			_tooltip = new ToolTip();

			SetStyle(ControlStyles.AllPaintingInWmPaint
				| ControlStyles.CacheText
				| ControlStyles.ResizeRedraw
				//| ControlStyles.Selectable
				| ControlStyles.UserPaint
				| ControlStyles.Opaque
			         , true);
			SetStyle(ControlStyles.Selectable, false);

			AllowDrop = true;
			Font = new Font("Tahoma", 8);
		}

		#endregion

		public ColorScheme ColorScheme
		{
			get { return _colorScheme; }
			set
			{
				if(_colorScheme != value)
				{
					_colorScheme = value;
					Invalidate();
				}
			}
		}

		/// <summary>
		/// Gets whether the button represents a workspace that is currently active.
		/// </summary>
		public bool Active
		{
			get { return _manager.ActiveWorkspace == _workspace; }
		}

		/// <summary>
		/// Gets the workspace attached to this button.
		/// The workspace is assigned in the button's constructor and cannot be changed at runtime.
		/// </summary>
		public IResource Workspace
		{
			get { return _workspace; }
		}

		/// <summary>
		/// When resources are added or deleted from the workspace, updates the unread counters.
		/// </summary>
		private void OnWorkspaceResourcesChanged(object sender, ResourceIndexEventArgs e)
		{
			_workspaceResourcesChanged = true;
		}

		/// <summary>
		/// Rebuilds the list of unread resources belonging to the workspace.
		/// </summary>
		private void UpdateUnreadResourceList()
		{
			if(_workspace != null && _workspace.IsDeleting)
				return;

			string workspaceName = WorkspaceName;
			Trace.WriteLine("Start update unread resources list for workspace " + workspaceName);
			if(_unreadResources != null)
			{
				_unreadResources.Dispose();
			}

			IResourceList unreadList = Core.ResourceStore.FindResourcesWithPropLive(null, Core.Props.IsUnread);
			unreadList = unreadList.Minus(Core.ResourceStore.FindResourcesWithProp(null, Core.Props.IsDeleted));

			Text = workspaceName;
			if(_workspace != null)
			{
				if(ICore.Instance != null)
				{
					_unreadResources = unreadList.Intersect(_manager.GetFilterList(_workspace), true );
				}
			}
			else
			{
				_unreadResources = unreadList;
			}
			if(_unreadResources != null)
			{
				_unreadResources.ResourceAdded += OnUnreadResourceAdded;
				_unreadResources.ResourceDeleting += OnUnreadResourceDeleting;
			}

			UpdateUnreadCounters();
			Trace.WriteLine("End update unread resources list for workspace " + workspaceName);
		}

		/// <summary>
		/// Updates the unread counters for the resources in the workspace.
		/// </summary>
		private void UpdateUnreadCounters()
		{
			_unreadCounters.Clear();
			lock(_unreadResources)
			{
				foreach(IResourceType resType in Core.ResourceStore.ResourceTypes)
				{
					if(resType.HasFlag(ResourceTypeFlags.CanBeUnread) && resType.OwnerPluginLoaded)
					{
						IResourceList typedUnreads = Core.ResourceStore.GetAllResources(resType.Name).Intersect(_unreadResources);
						if(typedUnreads.Count != 0)
						{
							_unreadCounters[resType.Name] = typedUnreads.Count;
						}
					}
				}
			}
		}

		/// <summary>
		/// Increments or decrements the unread counter for the specified resource type.
		/// If executed from the resource thread, sets a flag that will cause an update
		/// to be performed in the UI thread at the end of the resource operation.
		/// </summary>
		private void AdjustUnreadCounter( string resType, int delta )
		{
			if(!Core.ResourceStore.ResourceTypes[resType].OwnerPluginLoaded)
			{
				return;
			}

			lock(_unreadCounters)
			{
				object oldCount = _unreadCounters[resType];
				int count = (oldCount == null) ? 0 : (int)oldCount;
				int oldLen = (count == 0) ? 0 : count.ToString().Length;

				count += delta;
				int newLen = (count == 0) ? 0 : count.ToString().Length;
				if(count > 0)
				{
					_unreadCounters[resType] = count;
				}
				else
				{
					_unreadCounters.Remove(resType);
				}

				if ( oldLen != newLen )
				{
					_widthChanged = true;
				}
				_unreadCountChanged = true;
			}
		}

		/// <summary>
		/// When an unread resource is added to the workspace filter list, increments the unread counter.
		/// </summary>
		private void OnUnreadResourceAdded( object sender, ResourceIndexEventArgs e )
		{
			AdjustUnreadCounter( e.Resource.Type, 1 );
		}

		/// <summary>
		/// When an unread resource is removed from the workspace filter list, decrements the unread counter.
		/// </summary>
		private void OnUnreadResourceDeleting( object sender, ResourceIndexEventArgs e )
		{
			AdjustUnreadCounter( e.Resource.Type, -1 );
		}

		/// <summary>
		/// If the unread counters were changed by a resource operation, invalidates the button.
		/// </summary>
		private void OnResourceOperationFinished(object sender, EventArgs e)
		{
			if(_workspaceResourcesChanged)
			{
				UpdateUnreadResourceList();
				_workspaceResourcesChanged = false;
			}
			if( _widthChanged || _unreadCountChanged )
			{
				Core.UserInterfaceAP.QueueJob(new MethodInvoker(UpdateWorkspaceButton));
			}
		}

		private void UpdateWorkspaceButton()
		{
			while( _widthChanged )
			{
                _widthChanged = false;
                // _widthChanged can be set again from the resource thread while we're updating the width (OM-11129)
                UpdateButtonWidth();
			}
			if( _unreadCountChanged )
			{
				Invalidate();
				_unreadCountChanged = false;
			}
		}

		/// <summary>
		/// Recalculates the button width to fit all unread counters.
		/// </summary>
		private void UpdateButtonWidth()
		{
			if((IsDisposed) || (Core.State == CoreState.ShuttingDown))
				return;

			// Apply!
			Width = CalcButtonWidth();
		}

		/// <summary>
		/// Measures the optimal button width.
		/// </summary>
		private int CalcButtonWidth()
		{
			if((IsDisposed) || (Core.State == CoreState.ShuttingDown))
				return 0;

			Rectangle client = ClientRectangle;
			int nCurPos = client.Left; // Current position at which the next label will be placed
			ArrayList drawings = new ArrayList();
			using(Graphics g = CreateGraphics())
			{
				IntPtr hdc = g.GetHdc();
				try
				{
					IntPtr hOldFont = Win32Declarations.SelectObject(hdc, _hFontCounter); // Select the font that will be used for drawing

					// Workspace title text
					string text = WorkspaceName; // Take the workspace name

					// Measure the title label size
					RECT rc = new RECT(client.Left, client.Top, client.Right, client.Bottom);
					Win32Declarations.DrawText(hdc, text, text.Length, ref rc, DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_CALCRECT);
					Size sizeTitle = new Size(rc.right - rc.left, rc.bottom - rc.top);
					_rectTitle = new Rectangle(new Point(nCurPos, client.Top + (client.Height - sizeTitle.Height) / 2), sizeTitle);
					nCurPos = _rectTitle.Right;
					drawings.Add(new Drawing(_rectTitle, text)); // Add the title to the drawings

					//////////////////////////////
					// Measure the Unread Counters
					Size sizeIcon = Core.ResourceIconManager.ImageList.ImageSize;
					int nIconTop = client.Top + (client.Height - sizeIcon.Height) / 2;
					lock(_unreadCounters)
					{
						foreach(DictionaryEntry de in _unreadCounters)
						{
							string resType = (string)de.Key;
							string sCounter = de.Value.ToString();

							// Gap before this counter
							nCurPos += _nGap;

							// Icon
							drawings.Add(new Drawing(resType, new Rectangle(new Point(nCurPos, nIconTop), sizeIcon), Core.ResourceIconManager.GetDefaultIconIndex(resType)));
							nCurPos += sizeIcon.Width; // Icon width
							nCurPos += _nIconTextGap; // Gap between the icon and its text

							// Label
							rc = new RECT(nCurPos, client.Top, client.Right, client.Bottom);
							Win32Declarations.DrawText(hdc, sCounter, sCounter.Length, ref rc, DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_CALCRECT);
							Size sizeCounter = new Size(rc.right - rc.left, rc.bottom - rc.top);
							Rectangle rcCounter = new Rectangle(new Point(nCurPos, client.Top + (client.Height - sizeCounter.Height) / 2), sizeCounter);
							drawings.Add(new Drawing(resType, rcCounter, sCounter));
							nCurPos = rcCounter.Right;
						}
						Win32Declarations.SelectObject(hdc, hOldFont);
					}
				}
				finally
				{
					g.ReleaseHdc(hdc);
				}
			}

			// Store the updated drawing data
			_drawings = (Drawing[])drawings.ToArray(typeof(Drawing));

			// Return the calculated width
			return nCurPos;
		}

		#region Layouting / Painting data.

		/// <summary>
		/// Information about the data this control is drawing.
		/// </summary>
		internal class Drawing
		{
			/// <summary>
			/// Draws an icon.
			/// </summary>
			internal Drawing(string sResType, Rectangle rect, int nIconIndex)
			{
				What = Type.CounterIcon;
				ResType = sResType;
				Bounds = rect;
				IconIndex = nIconIndex;

				Text = "";
			}

			/// <summary>
			/// Draws a label.
			/// </summary>
			internal Drawing(string sResType, Rectangle rect, string sText)
			{
				What = Type.CounterLabel;
				ResType = sResType;
				Bounds = rect;
				Text = sText;
				IconIndex = -1;
			}

			/// <summary>
			/// Draws the button title.
			/// </summary>
			internal Drawing(Rectangle rect, string sTitle)
			{
				What = Type.Title;
				ResType = null;
				Bounds = rect;
				Text = sTitle;
				IconIndex = -1;
			}

			/// <summary>
			/// What to draw.
			/// </summary>
			internal enum Type
			{
				/// <summary>
				/// Resource type icon of the counter.
				/// </summary>
				CounterIcon,

				/// <summary>
				/// Text label of the counter.
				/// </summary>
				CounterLabel,

				/// <summary>
				/// Button title.
				/// </summary>
				Title
			}

			/// <summary>
			/// What to draw (either icon or label).
			/// </summary>
			internal Type What;

			/// <summary>
			/// Bounds of the drawing item.
			/// </summary>
			internal Rectangle Bounds;

			/// <summary>
			/// If <see cref="What"/> is <see cref="Type.CounterIcon"/>, index of this icon.
			/// Undefined otherwise.
			/// </summary>
			internal int IconIndex;

			/// <summary>
			/// If <see cref="What"/> is <see cref="Type.CounterLabel"/>, text of the label.
			/// Undefined otherwise.
			/// </summary>
			internal string Text;

			/// <summary>
			/// Resource type of the counter to which this drawing is related.
			/// Serves as a key in the counters dictionary.
			/// </summary>
			internal string ResType;
		}

		/// <summary>
		/// Bounding rectangle of the workspace title.
		/// </summary>
		private Rectangle _rectTitle = Rectangle.Empty;

		/// <summary>
		/// Icons and labels of the unread counters.
		/// </summary>
		/// <remarks>All the items must be arranged in an ascending order by the left x-coordinates of their bounding rects and have no overlappings in the X-axis projection for the hit-tests to work correctly.</remarks>
		private Drawing[] _drawings = null;

		#endregion

		/*
		private int GetWorkspaceTextWidth( Graphics g )
		{
			IntPtr hdc = g.GetHdc();
			IntPtr oldFont = Win32Declarations.SelectObject( hdc, _fontHandle );
			SIZE sz = new SIZE();
			string	text = WorkspaceName;
			Win32Declarations.GetTextExtentPoint32( hdc, text, text.Length, ref sz );
			Win32Declarations.SelectObject( hdc, oldFont );
			g.ReleaseHdc( hdc );
			return sz.cx;
		}
		*/

		/// <summary>
		/// Draws the workspace button.
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			// Background (try to retrieve a brush from the parent)
			IBackgroundBrushProvider bbp = Parent as IBackgroundBrushProvider;
			Brush brushBack = bbp != null ? bbp.GetBackgroundBrush(this) : new SolidBrush(BackColor);
			using(brushBack)
				e.Graphics.FillRectangle(brushBack, ClientRectangle);

			// Do not try to paint a killed workspace
			if((_workspace != null) && (_workspace.IsDeleted))
				return;

			if(Core.State == CoreState.ShuttingDown)
				return;

			Color colorLink = Color.Blue; // Color of the links (unread counter text and underlining)

			// Foreground
			if(Active)
			{ // The button represents an active (currently selected) workspace
				/*
				using( GraphicsPath gp = GdiPlusTools.BuildRoundRectPath( ClientRectangle ) )
				{
					e.Graphics.FillPath( ColorScheme.GetBrush( _colorScheme,
					                                           "WorkspaceBar.ActiveButtonBackground", ClientRectangle, SystemBrushes.Control ), gp );
				}

				Rectangle innerRect = ClientRectangle;
				innerRect.Inflate( -1, -1 );
				Pen borderPen = new Pen( ColorScheme.GetColor( _colorScheme,
				                                               "WorkspaceBar.ActiveButtonBorder", SystemColors.Control ), 2.0f );
				using( borderPen )
				{
					e.Graphics.DrawRectangle( borderPen, innerRect );
				}

				using( Brush cornerBrush = new SolidBrush( BackColor ) )
				{
					e.Graphics.FillRectangle( cornerBrush, innerRect.Left - 1, innerRect.Top - 1, 1, 1 );
					e.Graphics.FillRectangle( cornerBrush, innerRect.Right, innerRect.Top - 1, 1, 1 );
					e.Graphics.FillRectangle( cornerBrush, innerRect.Left - 1, innerRect.Bottom, 1, 1 );
					e.Graphics.FillRectangle( cornerBrush, innerRect.Right, innerRect.Bottom, 1, 1 );
				}
				*/
			}
			else
			{ // The button's workspace is not active
				if(_hot)
				{ // The button is hovered, display UI cues
					/*
					e.Graphics.DrawRectangle( ColorScheme.GetPen( _colorScheme,
					                                              "WorkspaceBar.ActiveButtonBorder", SystemPens.Control ),
					                          ClientRectangle.Left, ClientRectangle.Top,
					                          ClientRectangle.Right - 4, ClientRectangle.Bottom - 1 );
											  */
				}
			}

			/*

			Rectangle	client = ClientRectangle;
			IntPtr	hdc = e.Graphics.GetHdc();
			ArrayList	arIconIndices = new ArrayList();	// Indices of the icons in the resource image list that should be drawn on the control
			ArrayList	arIconPositions = new ArrayList();	// X-coordinates of those icons
			try
			{
				IntPtr hOldFont = Win32Declarations.SelectObject( hdc, _fontHandle );
				int rgbTextColor = Win32Declarations.ColorToRGB( Enabled ? ForeColor : SystemColors.GrayText ); // Title color
				int rgbOldColor = Win32Declarations.SetTextColor( hdc, rgbTextColor );
				BackgroundMode oldMode = Win32Declarations.SetBkMode( hdc, Active ? BackgroundMode.OPAQUE : BackgroundMode.TRANSPARENT );
				int	rgbOldBackColor = Win32Declarations.SetBkColor( hdc, Win32Declarations.ColorToRGB(SystemColors.Control) );
				int	nCurPos = client.Left;	// Current position at which the next label will be placed

				// Workspace title text
				string text = WorkspaceName; // Take the workspace name

				// Measure the title label size
				RECT rc = new RECT( client.Left, client.Top, client.Right, client.Bottom );
				Win32Declarations.DrawText( hdc, text, text.Length, ref rc, DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_CALCRECT );
				Size sizeTitle = new Size( rc.right - rc.left, rc.bottom - rc.top );

				// Draw the title
				Rectangle	rcText = new Rectangle(new Point(nCurPos, client.Top + (client.Height - sizeTitle.Height) / 2), sizeTitle);
				rc = new RECT(rcText.Left,  rcText.Top,  rcText.Right, rcText.Bottom);
				Win32Declarations.DrawText( hdc, text, text.Length, ref rc, DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE );
				nCurPos = rcText.Right;
				nCurPos += _nGap;

				//////////////////////////////
				// Paint the Unread Counters
				// We cannot paint the icons now because HDC is in use for text, save their types and positions for later use
				Win32Declarations.SetTextColor( hdc, Win32Declarations.ColorToRGB( Color.Blue ));
				lock( _unreadCounters )
				{
					foreach( DictionaryEntry de in _unreadCounters )
					{
						string resType = (string) de.Key;
						int counter = (int) de.Value;

						// Icon (cannot draw now as HDC is in use)
						arIconIndices.Add( Core.ResourceIconManager.GetDefaultIconIndex( resType ) );
						arIconPositions.Add( nCurPos );
						nCurPos += 16; // Icon width
						nCurPos += _nIconTextGap; // Gap between the icon and its text

						// Item text
						string sCounter = counter.ToString();

						// Measure
						rc = new RECT( nCurPos, client.Top, client.Right, client.Bottom );
						Win32Declarations.DrawText( hdc, sCounter, sCounter.Length, ref rc, DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_CALCRECT );
						Size sizeCounter = new Size( rc.right - rc.left, rc.bottom - rc.top );

						// Draw
						Rectangle rcCounter = new Rectangle( new Point( nCurPos, client.Top + (client.Height - sizeCounter.Height) / 2 ), sizeCounter );
						rc = new RECT( rcCounter.Left, rcCounter.Top, rcCounter.Right, rcCounter.Bottom );
						Win32Declarations.DrawText( hdc, sCounter, sCounter.Length, ref rc, DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE );
						nCurPos = rcCounter.Right;
						nCurPos += _nGap;
					}

					Win32Declarations.SetBkMode( hdc, oldMode );
					Win32Declarations.SetTextColor( hdc, rgbOldColor );
					Win32Declarations.SetBkColor( hdc, rgbOldBackColor );
					Win32Declarations.SelectObject( hdc, hOldFont );
				}
			}
			finally
			{
				e.Graphics.ReleaseHdc( hdc );
			}

			///////////////////
			// Draw the icons
			if(arIconIndices.Count == arIconPositions.Count)
			{
				int	nIconTop = client.Top + (client.Height - 16) / 2;
				for(int a = 0; a < arIconIndices.Count; a++)
					Core.ResourceIconManager.ImageList.Draw( e.Graphics, (int) arIconPositions[a], nIconTop, 16, 16, (int) arIconIndices[a] );
			}
			else
				Trace.WriteLine( "Icon indices and icon positions lists are desynchronized.", "[WB]" );
			*/

			IntPtr hdc = e.Graphics.GetHdc();
			try
			{
				IntPtr hOldFont = Win32Declarations.SelectObject(hdc, _hFontCounter);
				int rgbTextColor = Win32Declarations.ColorToRGB(Enabled ? ForeColor : SystemColors.GrayText); // Title color
				int rgbOldColor = Win32Declarations.SetTextColor(hdc, rgbTextColor);
				BackgroundMode oldMode = Win32Declarations.SetBkMode(hdc, Active ? BackgroundMode.OPAQUE : BackgroundMode.TRANSPARENT);
				int rgbOldBackColor = Win32Declarations.SetBkColor(hdc, Win32Declarations.ColorToRGB(SystemColors.Control));

				// Workspace title text
				string text = WorkspaceName; // Take the workspace name
				RECT rc = RECTFromRectangle(_rectTitle);
				Win32Declarations.DrawText(hdc, text, text.Length, ref rc, DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE);

				///////////////////////////////////
				// Paint the Unread Counters text
				// (icons cannot be painted while the HDC is in use, wait for it to be released)
				Win32Declarations.SetTextColor(hdc, Win32Declarations.ColorToRGB(colorLink));
				if(_drawings != null)
				{
					foreach(Drawing drawing in _drawings)
					{
						if(drawing.What != Drawing.Type.CounterLabel)
							continue;

						// Update the text by taking it from the unread counters map
						object value = _unreadCounters[drawing.ResType];
						if(value != null)
							drawing.Text = value.ToString();

						// Paint the text
						rc = RECTFromRectangle(drawing.Bounds);
						Win32Declarations.DrawText(hdc, drawing.Text, drawing.Text.Length, ref rc, DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE);
					}
				}

				Win32Declarations.SetBkMode(hdc, oldMode);
				Win32Declarations.SetTextColor(hdc, rgbOldColor);
				Win32Declarations.SetBkColor(hdc, rgbOldBackColor);
				Win32Declarations.SelectObject(hdc, hOldFont);
			}
			finally
			{
				e.Graphics.ReleaseHdc(hdc);
			}

			//////////////////////////
			// Unread Counters Icons
			// and underline the hovered text
			if(_drawings != null)
			{
				using(Brush brushUnderline = new SolidBrush(colorLink))
				{
					foreach(Drawing drawing in _drawings)
					{
						// Draw the icon
						if(drawing.What == Drawing.Type.CounterIcon)
							Core.ResourceIconManager.ImageList.Draw(e.Graphics, drawing.Bounds.Left, drawing.Bounds.Top, drawing.Bounds.Width, drawing.Bounds.Height, drawing.IconIndex);
							// Draw underlining
						else if((drawing.What == Drawing.Type.CounterLabel) && (drawing.ResType == _sUnreadResTypeHovered))
							e.Graphics.FillRectangle(brushUnderline, new Rectangle(drawing.Bounds.Left, drawing.Bounds.Bottom - 1, drawing.Bounds.Width, 1));
					}
				}
			}
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if(levent.AffectedProperty == "Width")
				return; // Prevent from infinite updates

			int nOptimalWidth = CalcButtonWidth();
			if(nOptimalWidth != Width)
				Core.UserInterfaceAP.QueueJobAt(DateTime.Now.AddMilliseconds(100), "Update workspace button width.", new MethodInvoker(UpdateButtonWidth)); // Should recalculate if the height has changed.
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			Point pt = new Point(e.X, e.Y);

			if(Core.State == CoreState.ShuttingDown)
				return;

			// Check which item has been hit by the mouse position
			Drawing hit = HitTest(pt);

			bool bHit = false;
			if(hit != null)
			{
				if(hit.What == Drawing.Type.Title)
				{ // Mouse on the title
					string sWspName = _workspace != null ? _workspace.DisplayName : _manager.Props.DefaultWorkspaceName; // Name of our wsp
					string text = String.Format("{0} Workspace", sWspName);
					if(_tooltip.GetToolTip(this) != text)
						_tooltip.SetToolTip(this, text);
					Cursor = Cursors.Default;
					bHit = true;
				}
				else
				{ // Mouse on the unread counter or its icon
					lock(_unreadCounters)
					{
						if(_unreadCounters.ContainsKey(hit.ResType)) // Check if it fits vertically, and is still actual
						{
							string text = String.Format("{0} ({1})", Core.ResourceStore.ResourceTypes[hit.ResType].DisplayName, _unreadCounters[hit.ResType]);
							if(_tooltip.GetToolTip(this) != text)
								_tooltip.SetToolTip(this, text);
							Cursor = Cursors.Hand;
							bHit = true;

							// Display underlined
							if(_sUnreadResTypeHovered != hit.ResType)
							{
								if(_sUnreadResTypeHovered != null)
									InvalidateResType(_sUnreadResTypeHovered); // Remove prev underline
								_sUnreadResTypeHovered = hit.ResType;
								InvalidateResType(_sUnreadResTypeHovered); // Paint new underline
							}
						}
					}
				}
			}

			// If either hothing has been hit, or the hit unread counter is now outdated, play the no-hit scenario
			if(!bHit)
			{
				_tooltip.SetToolTip(this, "");
				Cursor = Cursors.Default;

				// Remove the underline, if any
				if(_sUnreadResTypeHovered != null)
				{
					InvalidateResType(_sUnreadResTypeHovered);
					_sUnreadResTypeHovered = null;
				}
			}
		}

		/// <summary>
		/// Perfors a hit test within the button: checks whether the mouse is currently hovering the very workspace name, or an unread counter/its icon.
		/// </summary>
		/// <param name="pt">The mouse coordinates (relative to client rect).</param>
		/// <returns>The hovered drawing structure, or <c>Null</c> if none hovered.</returns>
		protected Drawing HitTest(Point pt)
		{
			// Find the drawing rect into which the mouse pointer fits
			int nTarget = -1; // Number of the target element in the array

			int nRangeStart = 0;
			int nRangeEnd = _drawings.Length - 1;
			if((pt.X >= _drawings[nRangeStart].Bounds.Left) && (pt.X < _drawings[nRangeStart].Bounds.Right))
				nTarget = nRangeStart;
			if((pt.X >= _drawings[nRangeEnd].Bounds.Left) && (pt.X < _drawings[nRangeEnd].Bounds.Right))
				nTarget = nRangeEnd;
			else
			{
				for(int a = 0; (a < _drawings.Length) && (nRangeStart < nRangeEnd); a++)
				{
					int nCenter = (nRangeStart + nRangeEnd) / 2;

					// Fits in the range ends?
					if((pt.X >= _drawings[nRangeStart].Bounds.Left) && (pt.X < _drawings[nRangeStart].Bounds.Right))
					{
						nTarget = nRangeStart;
						break;
					}
					if((pt.X >= _drawings[nRangeEnd].Bounds.Left) && (pt.X < _drawings[nRangeEnd].Bounds.Right))
					{
						nTarget = nRangeEnd;
						break;
					}
					// Fits in the center?
					if((pt.X >= _drawings[nCenter].Bounds.Left) && (pt.X < _drawings[nCenter].Bounds.Right))
					{
						nTarget = nCenter; // Found it!
						break;
					}
					else if(pt.X < _drawings[nCenter].Bounds.Left) // To the left of the center item
						nRangeEnd = nCenter - 1;
					else if(pt.X >= _drawings[nCenter].Bounds.Right) // To the left of the center item
						nRangeStart = nCenter + 1;
				}
			}

			// Check if found
			if(nTarget != -1)
			{
				Drawing hit = _drawings[nTarget]; // The proposed hit target
				if(hit.Bounds.Contains(pt)) // We've checked x-coordinates until this, now for the last time see the Y too
					return hit;
			}

			// Nothing found; mouse is outside the drawing rects
			return null;
		}

		/// <summary>
		/// When the drag enters the button and the drag is a workspace, highlights the button.
		/// </summary>
		protected override void OnDragEnter(DragEventArgs drgevent)
		{
			base.OnDragEnter(drgevent);
			if(drgevent.Data.GetDataPresent(typeof(IResourceList)))
			{
				ForeColor = Color.Blue;
				drgevent.Effect = DragDropEffects.Link;
			}
		}

		/// <summary>
		/// When the drag leaves the button, removes the highlighting.
		/// </summary>
		protected override void OnDragLeave(EventArgs e)
		{
			base.OnDragLeave(e);
			ForeColor = SystemColors.WindowText;
		}

		/// <summary>
		/// When resources are dropped on the workspace button, adds them to the workspace.
		/// </summary>
		protected override void OnDragDrop(DragEventArgs drgevent)
		{
			base.OnDragDrop(drgevent);
			if(drgevent.Data.GetDataPresent(typeof(IResourceList)))
			{
				IResourceList resList = (IResourceList)drgevent.Data.GetData(typeof(IResourceList));
                AddToWorkspaceAction.AddResourcesToWorkspace( resList, _workspace );
			}
			ForeColor = SystemColors.WindowText;
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			_hot = true;
			if(!Active)
			{
				Invalidate();
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_hot = false;
			if(_sUnreadResTypeHovered != null)
			{ // Remove the underline
				InvalidateResType(_sUnreadResTypeHovered);
				_sUnreadResTypeHovered = null;
			}
			if(!Active)
			{
				//Invalidate();
				// TODO: repaint the visual cues, if needed
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown( e );

            if ( e.Button == MouseButtons.Left )
            {
                if( WorkspaceClicked != null )
                {
                    try
                    {
						Drawing hit = HitTest(new Point(e.X, e.Y));	// An area of the button that has been hit

						// If an icon for some resource type has been hit, produce a list of resources associated with that icon, such a list is useful for jumping to the proper tab upon click
						IResourceList resClickedUnreads = null;
						if((hit != null) && (hit.ResType != null) && (hit.ResType.Length != 0) && (_unreadResources != null))
							resClickedUnreads = Core.ResourceStore.GetAllResources(hit.ResType).Intersect(_unreadResources);
                        WorkspaceClicked(this, new WorkspaceClickedEventArgs(_workspace, (hit != null ? hit.ResType : null), resClickedUnreads));
                    }
                    catch(Exception ex)
                    {
                        Core.ReportException(ex, ExceptionReportFlags.AttachLog);
                    }
                }
            }
		}

		/// <summary>
		/// Invalidates the resource unread counters for the given resource type.
		/// </summary>
		protected void InvalidateResType(string sResType)
		{
			if(_drawings != null)
			{
				foreach(Drawing drawing in _drawings)
				{
					if(drawing.ResType == sResType)
						Invalidate(Rectangle.FromLTRB(drawing.Bounds.Left, drawing.Bounds.Top, drawing.Bounds.Right, ClientRectangle.Bottom), false); // Leave some place for the underlining
				}
			}
		}

		/// <summary>
		/// A workspace has changed.
		/// If it's a change in its name, update the button.
		/// </summary>
		private void OnWorkspaceChanged(object sender, ResourcePropIndexEventArgs e)
		{
			if(e.ChangeSet.IsDisplayNameAffected) // Relayout the button to update its widthхотя тут наверно жд ту
				Core.UserInterfaceAP.QueueJob("Update Workspace Button", new MethodInvoker(UpdateButtonWidth));
		}

		/// <summary>
		/// Name of the workspace represented by this button.
		/// <see cref="IResource.DisplayName"/> in case of a non-<c>Null</c> workspace, or a special value for the default workspace.
		/// </summary>
		public string WorkspaceName
		{
			get { return _workspace != null ? _workspace.DisplayName : (_manager != null ? _manager.Props.DefaultWorkspaceName : "•"); }
		}

		/// <summary>
		/// Converts a <see cref="Rectangle"/> structure to an equivalent <see cref="JetBrains.Interop.WinApi.RECT"/> one.
		/// </summary>
		public RECT RECTFromRectangle(Rectangle rect)
		{
			return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
		}

		#region WorkspaceClicked Event

		/// <summary>
		/// The workspace button has been clicked, select its workspace,
		///  and (possibly) open the tab corresponding to the resource type whose unread counter has been clicked.
		/// </summary>
		public event WorkspaceClickedEventHandler WorkspaceClicked;

		/// <summary>
		/// Delegate for the <see cref="WorkspaceClicked"/> event.
		/// </summary>
		public delegate void WorkspaceClickedEventHandler(object sender, WorkspaceClickedEventArgs args);

		/// <summary>
		/// Arguments class for the <see cref="WorkspaceClicked"/> event.
		/// </summary>
		public class WorkspaceClickedEventArgs
		{
			/// <summary>
			/// A workspace whose button has been clicked.
			/// </summary>
			private readonly IResource _workspace;

			/// <summary>
			/// If the click was located over an icon or conuter for a particular resource (not on the workspace name or an empty space), the Resource Type being clicked.
			/// </summary>
			private readonly string _sUnreadResourceType;

			/// <summary>
			/// If the click was located over an icon or conuter for a particular resource (not on the workspace name or an empty space), the list of resources being clicked.
			/// </summary>
			private readonly IResourceList _resUnreadClicked;

			/// <summary>
			/// Constructs the object, initializes the data.
			/// </summary>
			public WorkspaceClickedEventArgs( IResource workspace, string sUnreadResourceType, IResourceList resUnreadClicked )
			{
				_workspace = workspace;
				_sUnreadResourceType = sUnreadResourceType;
				_resUnreadClicked = resUnreadClicked;
			}

			/// <summary>
			/// Workspace that is attached to the button that has been clicked, or <c>Null</c> for the Default workspace.
			/// </summary>
			public IResource Workspace
			{
				get { return _workspace; }
			}

			/// <summary>
			/// Name of the resource whose unread counter has been clicked, or a null value (<c>Null</c>) if the workspace button itself has been clicked.
			/// </summary>
			public string UnreadResourceType
			{
				get { return _sUnreadResourceType; }
			}

			/// <summary>
			/// If the click was located over an icon or conuter for a particular resource (not on the workspace name or an empty space), the list of resources being clicked. <c>Null</c> otherwise.
			/// </summary>
			public IResourceList UnreadClickedResources
			{
				get { return _resUnreadClicked; }
			}
		}

		#endregion
	}
}
