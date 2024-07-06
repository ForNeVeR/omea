// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Components.ImageListButton;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A bar that displays a sequence of controls (as many as fit in the available
	/// horizontal space) and a chevron button under which the controls which do not fit
	/// are hidden.
	/// </summary>
	public class ChevronBar : UserControl, ICommandBar
	{
		#region Data

		private IContainer components;

		/// <summary>
		/// The chevron button that draws the context menu upon a click.
		/// Visisble only if there are hidden or unfit controls.
		/// </summary>
		private ImageListButton _btnChevron;

		/// <summary>
		/// If this flag is raisen, then some shit has happened. Yuck.
		/// </summary>
		private bool _isLayoutBroken = false;

		/// <summary>
		/// All the normal controls of the chevron bar (ones that potentially can be visible on the toolbar).
		/// Defines the order of controls on the toolbar.
		/// The unfit controls are not excluded from this list.
		/// The full set of toolbar's controls can be gotten by taking <see cref="_controls"/> and <see cref="_hiddenControls"/>.
		/// </summary>
		private ArrayList _controls = new ArrayList();

		/// <summary>
		/// Controls that could have been visible on the bar if there were a little bit more space.
		/// Note that these controls are not excluded from the <see cref="_controls"/> list.
		/// Should be displayed under the chevron menu, as well as the natively-hidden controls.
		/// This set is recalculated each time by the <see cref="OnLayout"/> function.
		/// </summary>
		private HashSet _hashDropped = new HashSet();

		/// <summary>
		/// Controls that are never allowed to come visible into the bar and permanenly stick to the chevron's drop down list.
		/// Have nothing to do with the <see cref="_controls"/> array.
		/// The full set of toolbar's controls can be gotten by taking <see cref="_controls"/> and <see cref="_hiddenControls"/>.
		/// </summary>
		private ArrayList _hiddenControls = new ArrayList();

		/// <summary>
		/// A delegate that allows to assign some text to the chevron menu item that
		/// is different from its <see cref="Control"/>'s <see cref="Text"/> property, which is used by default.
		/// </summary>
		private GetChevronMenuTextDelegate _getChevronMenuText;

		/// <summary>
		/// Separator between the hidden and unfit items in the chevron's popup menu.
		/// </summary>
		private bool _separateHiddenControls = true;

		/// <summary>
		/// Left margin of the toolbar (spacing between the control's left side and the first child control).
		/// </summary>
		private int _nLeftMargin = 0;

		/// <summary>
		/// Right margin of the toolbar (spacing between the control's right side and the last child control, or the chevron, if visible).
		/// </summary>
		private int _nRightMargin = 0;

		/// <summary>
		/// Context menu of the chevron.
		/// </summary>
		private ContextMenu _menuOnChevron;

		/// <summary>
		/// Gap between the adjacent controls. Does not apply to the gap between the first/last control and this control's edges.
		/// </summary>
		private int _nGap = 4;

		/// <summary>
		/// Tells whether the toolbar may be sized above the total maximum size of its controls and spacings.
		/// </summary>
		protected bool _bAllowOversizing = false;

		/// <summary>
		/// The command bar site of this control.
		/// </summary>
		private ICommandBarSite _site;

		#endregion

		#region Construction

		public ChevronBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponentSelf();
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

		private void InitializeComponentSelf()
		{
			components = new Container();
			using( new LayoutSuspender( this ) )
			{
				//
				// _chevronToolbar
				//
				_btnChevron = new ImageListButton();
				_btnChevron.Location = new Point( 124, 2 );
				_btnChevron.Name = "_btnChevron";
				_btnChevron.TabStop = false;
				_btnChevron.Visible = false;
				_btnChevron.Click += new EventHandler( OnChevronClick );
				_btnChevron.AddIcon(LoadChevronIcon( false ), ImageListButton.ButtonState.Normal);
				_btnChevron.AddIcon(LoadChevronIcon( true ), ImageListButton.ButtonState.Hot);
				_btnChevron.BackColor = BackColor;
				//
				// _menuOnChevron
				//
				_menuOnChevron = new ContextMenu();
				//
				// ChevronBar
				//
				Controls.Add( _btnChevron );
				Name = "ChevronBar";
				Size = OptimalSize;

				SetStyle( ControlStyles.AllPaintingInWmPaint
					| ControlStyles.CacheText
					| ControlStyles.ContainerControl
					| ControlStyles.UserPaint
					| ControlStyles.Opaque
				          , true );
				SetStyle( ControlStyles.DoubleBuffer
					| ControlStyles.ResizeRedraw
					| ControlStyles.Selectable
				          , false );
			}
		}

		#endregion

		#region Events

		#region ChevronMenuItemClick Event

		/// <summary>
		/// Fires when the chevron
		/// </summary>
		public event ChevronMenuItemClickEventHandler ChevronMenuItemClick;

		/// <summary>
		/// Delegate type for the <see cref="ChevronMenuItemClick"/> event.
		/// </summary>
		public delegate void ChevronMenuItemClickEventHandler( object sender, ChevronMenuItemClickEventArgs args );

		/// <summary>
		/// Arguments for the the <see cref="ChevronMenuItemClick"/> event.
		/// </summary>
		public class ChevronMenuItemClickEventArgs
		{
			private Control _clicked;

			public ChevronMenuItemClickEventArgs( Control clicked )
			{
				_clicked = clicked;
			}

			/// <summary>
			/// A control that has been clicked within the Chevron menu.
			/// </summary>
			public Control ClickedControl
			{
				get { return _clicked; }
			}
		}

		/// <summary>
		/// Fires the <see cref="ChevronMenuItemClick"/> event.
		/// </summary>
		protected void FireChevronMenuItemClick( ChevronMenuItemClickEventArgs args )
		{
			try
			{
				if( ChevronMenuItemClick != null )
					ChevronMenuItemClick( this, args );
			}
			catch( Exception ex )
			{
				Core.ReportException( ex, ExceptionReportFlags.AttachLog );
			}
		}

		/// <summary>
		/// A delegate for the <see cref="FireChevronMenuItemClick"/> function.
		/// </summary>
		internal delegate void FireChevronMenuItemClickDelegate( ChevronMenuItemClickEventArgs args );

		#endregion

		#endregion

		#region Operations

		/// <summary>
		/// Removes all the controls from the bar.
		/// Warning! The controls get disposed when they are removed!
		/// </summary>
		public void ClearControls()
		{
			using( new LayoutSuspender( this ) )
			{
				foreach( Control control in _controls )
					Controls.Remove( control );
				_controls.Clear();
				_hiddenControls.Clear();
			}
			// Note: there's no use of clearing the _hashUnfit, it will be updated by the panding Layout request

			_isLayoutBroken = false; // The layout is broken no more if there are no more controls

			if( _site != null )
				_site.PerformLayout( this );
		}

		/// <summary>
		/// Adds a control to the bar.
		/// </summary>
		public void AddControl( Control ctl )
		{
			_controls.Add( ctl );
			Controls.Add( ctl );
			if( _site != null )
				_site.PerformLayout( this );
		}

		/// <summary>
		/// Adds a control to the list of controls which are only visible when the chevron menu is dropped down.
		/// </summary>
		public void AddHiddenControl( Control ctl )
		{
			_hiddenControls.Add( ctl );
			if( _site != null )
				_site.PerformLayout( this );
			PerformLayout(); // This is needed in case the chevron gets visible
		}

		/// <summary>
		/// Retrieves the chevron's icon for either of its states.
		/// </summary>
		/// <param name="hot"><c>True</c> for the hot (hovered by mouse) chevron state, and <c>False</c> for the normal state.</param>
		/// <returns></returns>
		public static Icon LoadChevronIcon( bool hot )
		{
			return new Icon( Assembly.GetExecutingAssembly().GetManifestResourceStream( String.Format( "GUIControls.Icons.Chevron.{0}.ico", (hot ? "Hot" : "Normal") ) ) );
		}

		#endregion

		#region Implementation

		/// <summary>
		/// When the control is resized, if there is not enough space to show all buttons or if there is space to show the next button, recreates the bar.
		/// </summary>
		protected override void OnLayout( LayoutEventArgs levent )
		{
			if( _isLayoutBroken )
				return;

			try
			{
				// Start with an assumption that all the controls will fit
				int nWidth = OptimalSize.Width;	// MaxSize may be euqal to +inf
				Size sizeMin = MinSize;
				_hashDropped.Clear();
				Rectangle client = ClientRectangle; // The client rectangle within which the controls should be layouted

				// If there's not enough space for an adequate layouting, hide all the controls and do nothing to the return arrays
				// Also, cancel layouting if not initialized yet
				if( (client.Width <= 0) || (client.Height <= 0) || (client.Width < sizeMin.Width) || (client.Height < sizeMin.Height) )
				{
					foreach( Control control in Controls )
						control.Visible = false;
					return; // Layout failed
				}

				// Indicates presence of the chevron button in the current vision of the layout.
				// As we start from the MaxSize version, we add a chevron only if there are hidden controls to be shown under it.
				// In this case its width (along with gap) is already encountered in the MaxSize.
				bool bChevronPresent = _hiddenControls.Count != 0;

				// Start dropping off the controls that do not fit, from right to the left
				int nDropPosition = _controls.Count - 1; // Points to the control that should be dropped next
				for(; (nWidth > client.Width) && (nDropPosition >= 0); nDropPosition-- )
				{
					// Check if we have to add a chevron (with one more separator) — this works on the first step only
					if( !bChevronPresent )
					{
						nWidth += _btnChevron.Width; // Chevron
						nWidth += _nGap; // Separator before the chevron
						bChevronPresent = true;
					}

					// Exclude the rightmost not excluded yet control, as well as one separator along with it
					Control controlDrop = (Control) _controls[ nDropPosition ];
					nWidth -= controlDrop.Width;
					nWidth -= _nGap;
					_hashDropped.Add( controlDrop );
				}

				// Could not fit … not enough controls to drop
				if( nDropPosition < 0 )
					Trace.WriteLine( "Not enought space to layout the controls properly.", "[CB]" );

				// Apply the layout
				int nCurPos = client.Left + _nLeftMargin;

				// Add the fitting controls
				foreach( Control control in _controls )
				{
					// Hide the dropped controls
					if( _hashDropped.Contains( control ) )
					{
						control.Visible = false;
						continue;
					}

					////
					// If the control is not dropped, show it

					// Add the control itself
					control.Location = new Point( nCurPos, client.Top + (client.Height - control.Height) / 2 ); // Hor-layout, ver-center
					control.Visible = true;
					nCurPos += control.Width;

					// Gap after the control
					nCurPos += _nGap;
				}

				// Add the chevron (if it's been chosen to be visible)
				if( bChevronPresent )
				{
					// Rihgt-align the chevron within the bar's bounds
					_btnChevron.Location = new Point( client.Right - _nRightMargin - _btnChevron.Width, client.Top + (client.Height - _btnChevron.Height) / 2 );
					_btnChevron.Visible = true;
				}
				else
					_btnChevron.Visible = false;
			}
			catch( Exception e )
			{
				Core.ReportBackgroundException( e );
				_isLayoutBroken = true;
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if( BackColor != Color.Transparent )
			{
				using( Brush brush = new SolidBrush( BackColor ) )
					e.Graphics.FillRectangle( brush, ClientRectangle );
			}
		}

		/// <summary>
		/// Shows the popup menu with controls that didn't fit into the chevron bar and
		/// controls which were explicitly hidden.
		/// </summary>
		private void OnChevronClick( object sender, EventArgs e )
		{
			_menuOnChevron.MenuItems.Clear();

			FillChevronContextMenu( _menuOnChevron ); // Extracted for the tests

			// Show
			_menuOnChevron.Show( this, new Point( _btnChevron.Right, _btnChevron.Bottom ) );
		}

		/// <summary>
		/// Fills in the chevron context menu.
		/// Extracted for the sake of the tests.
		/// </summary>
		public void FillChevronContextMenu( ContextMenu menu ) // public for unit tests
		{
			bool bSeparator = false; // Indicates whether to add a separator between the menu groups

			// Add the unfit, but visible-wannabe controls
			foreach( Control control in _controls )
			{
				if( _hashDropped.Contains( control ) )
				{
					menu.MenuItems.Add( new ChevronMenuItem( control, _getChevronMenuText, new FireChevronMenuItemClickDelegate( FireChevronMenuItemClick ) ) );
					bSeparator = bSeparator | _separateHiddenControls; // Needed, if allowed
				}
			}

			// Add the forever-concealed controls
			foreach( Control control in _hiddenControls )
			{
				// Add the separator if both groups are non-empty
				if( bSeparator )
				{
					bSeparator = false;
					menu.MenuItems.Add( new MenuItem( "-" ) );
				}

				menu.MenuItems.Add( new ChevronMenuItem( control, _getChevronMenuText, new FireChevronMenuItemClickDelegate( FireChevronMenuItemClick ) ) );
			}
		}

		#endregion

		#region ICommandBar Members

		public void SetSite( ICommandBarSite site )
		{
			_site = site;
		}

		public Size MinSize
		{
			get
			{
				// Is the chevron mandatory in the minimum toolbar size (if there's a control of any kind)?
				if( (_controls.Count != 0) || (_hiddenControls.Count != 0) )
					return new Size( _btnChevron.Size.Width + _nLeftMargin + _nRightMargin, _btnChevron.Size.Height );
				else
					return new Size( _nLeftMargin + _nRightMargin, 0 );
			}
		}

		/// <summary>
		/// Size that is needed to house all the controls.
		/// Width is the sum of all the controls' widths, plus the chevron if there are mandatory hidden controls under it.
		/// Height is the maximum control height encountered.
		/// </summary>
		public Size OptimalSize
		{
			get
			{
				// All the controls to be measured
				ArrayList controlsAll = new ArrayList( _controls );

				// Is the chevron mandatory?
				if( _hiddenControls.Count > 0 )
					controlsAll.Add( _btnChevron ); // This is just a chevron button, not the toolbar :)

				// Add all the controls
				int nTotalWidth = 0;
				int nMaxHeight = 0;
				ICommandBar cmdbar = null;
				Size size;
				foreach( Control control in controlsAll )
				{
					//cmdbar = control as ICommandBar;
					size = cmdbar != null ? cmdbar.OptimalSize : control.Size; // If a control knows its optimal size, use it

					nTotalWidth += size.Width;
					nMaxHeight = nMaxHeight > size.Height ? nMaxHeight : size.Height;
				}

				// Add the gaps between the controls
				nTotalWidth += _nGap;

				// Add the left-right margins
				nTotalWidth += _nLeftMargin + _nRightMargin;

				return new Size( nTotalWidth, nMaxHeight );
			}
		}

		public Size MaxSize
		{
			get
			{
				// If oversizing is allowed, report no limit on the toolbar's size; otherwise, calculate it from the sizes of the controls
				if( _bAllowOversizing )
					return new Size( int.MaxValue, int.MaxValue );
				else
				{
					// All the controls to be measured
					ArrayList controlsAll = new ArrayList( _controls );

					// Is the chevron mandatory?
					if( _hiddenControls.Count > 0 )
						controlsAll.Add( _btnChevron ); // This is just a chevron button, not the toolbar :)

					// Add all the controls
					int nTotalWidth = 0;
					int nMaxHeight = 0;
					ICommandBar cmdbar = null;
					Size size;
					foreach( Control control in controlsAll )
					{
						//cmdbar = control as ICommandBar;
						size = cmdbar != null ? cmdbar.MaxSize : control.Size; // If a control knows its optimal size, use it

						nTotalWidth += size.Width;
						nMaxHeight = nMaxHeight > size.Height ? nMaxHeight : size.Height;
					}

					// Add the gaps between the controls
					nTotalWidth += _nGap * (controlsAll.Count - 1);

					// Add the left-right margins
					nTotalWidth += _nLeftMargin + _nRightMargin;

					return new Size( nTotalWidth, nMaxHeight );
				}
			}
		}

		public Size Integral
		{
			get { return new Size( 1, 1 ); }
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Tells whether the chevron bar's chevron control should be currently visible.
		/// Unlike checking the control's visiiblity state, it indicates the desired chevron state, not the current one.
		/// </summary>
		public bool IsChevronVisible
		{
			get { return _hiddenControls.Count + _hashDropped.Count != 0; }
		}

		/// <summary>
		/// Gets or sets whether a separator between the hidden and unfit items in the chevron's popup menu is displayed.
		/// </summary>
		[DefaultValue( true )]
		public bool SeparateHiddenControls
		{
			get { return _separateHiddenControls; }
			set { _separateHiddenControls = value; }
		}

		/// <summary>
		/// Gets or sets a delegate that allows to assign some text to the chevron menu item that
		/// is different from its <see cref="Control"/>'s <see cref="Control.Text"/> property, which is used by default.
		/// <c>Null</c> is a valid value which defaults to taking the <see cref="Control.Text"/> property.
		/// </summary>
		public GetChevronMenuTextDelegate GetChevronMenuText
		{
			get { return _getChevronMenuText; }
			set { _getChevronMenuText = value; }
		}

		/// <summary>
		/// Tells whether the toolbar may be sized above the total maximum size of its controls and spacings.
		/// </summary>
		public bool AllowOversizing
		{
			get { return _bAllowOversizing; }
			set
			{
				if( _bAllowOversizing != value )
				{
					_bAllowOversizing = value;
					if( _site != null )
						_site.PerformLayout( this );
				}
			}
		}

		/// <summary>
		/// Background color of the toolbar.
		/// </summary>
		public override Color BackColor
		{
			get
			{
				return base.BackColor;
			}
			set
			{
				base.BackColor = value;
				if(_btnChevron != null)
					_btnChevron.BackColor = value;
			}
		}


		#endregion

		#region Class ChevronMenuItem — Represents an item of the chevron context menu.

		/// <summary>
		/// Represents an item of the chevron context menu.
		/// </summary>
		internal class ChevronMenuItem : MenuItem
		{
			/// <summary>
			/// Control represented by the item.
			/// </summary>
			private Control _control;

			/// <summary>
			/// If an item gets clicked, fires the corresponding notification event.
			/// </summary>
			private FireChevronMenuItemClickDelegate _delegateEventFirer;

			/// <summary>
			/// Constructs the menu item.
			/// </summary>
			/// <param name="control">Control represented by the item.</param>
			/// <param name="delegateTextGetter">Gets the item text (may be <c>Null</c>).</param>
			/// <param name="delegateEventFirer">If an item gets clicked, fires the corresponding notification event.</param>
			internal ChevronMenuItem( Control control, GetChevronMenuTextDelegate delegateTextGetter, FireChevronMenuItemClickDelegate delegateEventFirer )
			{
				_control = control;
				_delegateEventFirer = delegateEventFirer;
				Text = delegateTextGetter != null ? delegateTextGetter( _control ) : _control.Text; // Retrieve the menu item text
			}

			protected override void OnClick( EventArgs e )
			{
				_delegateEventFirer( new ChevronMenuItemClickEventArgs( _control ) );
			}

		}

		#endregion
	}

	public delegate string GetChevronMenuTextDelegate( Control ctl );
}
