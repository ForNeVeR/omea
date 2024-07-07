// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls.CommandBar
{
	/// <summary>
	/// Implements the toolbar grip logic.
	/// </summary>
	public class Grip
	{
		#region Data Members

		/// <summary>
		/// Control that owns the grip.
		/// </summary>
		protected Control _control;

		/// <summary>
		/// The command bar site of the owning control.
		/// </summary>
		protected ICommandBarSite _site = null;

		/// <summary>
		/// The relative dragging position (offset of the mouse pointer from the left-top point of the grip at the time when the dragging started).
		/// While dragging, we try to maintain this offset constant so that it would look like the mouse being attached to the grip and works better than moving the bar by the momentary mouse offsets each time.
		/// </summary>
		protected Size _sizeStartDragOffset = Size.Empty;

		/// <summary>
		/// Tells whether the toolbar is being dragged on its grip.
		/// </summary>
		protected bool _isDraggingGrip = false;

		/// <summary>
		/// Tells whether the mouse cursor is currently on the toolbar's grip.
		/// </summary>
		protected bool _bMouseOnGrip = false;

		/// <summary>
		/// The default cursor that should be applied when mouse leaves the grip.
		/// Stored here when mouse enters the grip.
		/// </summary>
		protected Cursor _cursorDefault = null;

		#endregion

		#region Constants

		/// <summary>
		/// Total size of each of the grip's dots groups.
		/// </summary>
		protected static readonly int c_nDotCellSize = 4;

		/// <summary>
		/// Size of each of the two dots in the group.
		/// </summary>
		protected static readonly int c_nDotSize = 2;

		/// <summary>
		/// Padding to the left of the grip.
		/// </summary>
		protected static readonly int c_nHorPadding = 2;

		/// <summary>
		/// Padding to the top and bottom of the grip.
		/// </summary>
		protected static readonly int c_nVerPadding = 3;

		/// <summary>
		/// Client rectangle for the grip, as calculated in the <see cref="OnLayout"/>, which is used for mouse positioning, painting, and dragging needs.
		/// </summary>
		private Rectangle _rectClient = Rectangle.Empty;

		#endregion

		#region Ctor

		/// <summary>
		/// Initializes the grip.
		/// </summary>
		/// <param name="control">Control that owns the grip.</param>
		public Grip( Control control )
		{
			_control = control;

			// Sink the related control events
			_control.MouseDown += new MouseEventHandler( OnMouseDown );
			_control.MouseUp += new MouseEventHandler( OnMouseUp );
			_control.MouseLeave += new EventHandler( OnMouseLeave );
			_control.MouseMove += new MouseEventHandler( OnMouseMove );
		}

		#endregion

		#region Mouse Event Handling

		protected virtual void OnMouseDown( object sender, MouseEventArgs e )
		{
			if( _bMouseOnGrip )
			{
				_control.Capture = true; // Capture the mouse
				_sizeStartDragOffset = GetDragOffset();
				_isDraggingGrip = true;
				_control.Cursor = Cursors.VSplit;
			}
		}

		protected virtual void OnMouseUp( object sender, MouseEventArgs e )
		{
			if( _isDraggingGrip ) // Dragging currently
			{
				// Cancel dragging
				_control.Capture = false;
				_sizeStartDragOffset = Size.Empty; // Dragging off
				_isDraggingGrip = false;
				_control.Cursor = PointOnGrip( e.X,  e.Y ) ? Cursors.SizeWE : _cursorDefault;
			}
		}

		protected virtual void OnMouseLeave( object sender, EventArgs e )
		{
			if( !_isDraggingGrip ) // Not dragging currently
			{
				if( _bMouseOnGrip ) // Leave the grip, as leaving the window
					GripMouseEnterOrLeave( false );
			}
		}

		protected virtual void OnMouseMove( object sender, MouseEventArgs e )
		{
			if( _isDraggingGrip )
			{ // Do the dragging

				// Calculate the offset between the initial mouse position (when drag was initiated) and the current mouse position
				// This defines the distance by which the parent control is requested to move due to the drag
				Size sizeDragOffset = GetDragOffset() - _sizeStartDragOffset;

				if( !sizeDragOffset.IsEmpty )
				{
					if( _site == null )
						throw new InvalidOperationException( "Control bar site must not be Null." );
					//Trace.WriteLine( "Old size: " + _control.Size.ToString(), "[SBS]" );
					//Trace.WriteLine( "Move requested: " + new Size( ptDragPosition.X - _sizeStartDragOffset.X, ptDragPosition.Y - _sizeStartDragOffset.Y ).ToString(), "[SBS]" );
					_site.RequestMove( _control as ICommandBar, sizeDragOffset );
					//Trace.WriteLine( "New size: " + _control.Size.ToString(), "[SBS]" );
				}
				_control.Cursor = Cursors.VSplit;
			}
			else
			{ // Not dragging currently, set cursor to indicate whether it's on the grip
				bool bOnGrip = PointOnGrip( e.X, e.Y );

				if( (_bMouseOnGrip) && (!bOnGrip) ) // Leaving the grip
					GripMouseEnterOrLeave( false );
				else if( (!_bMouseOnGrip) && (bOnGrip) ) // Entering the grip
					GripMouseEnterOrLeave( true );
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Performs the actions considering the mouse cursor entering or leaving the toolbar grip.
		/// </summary>
		/// <param name="bEnter"><c>True</c> to enter, <c>False</c> to leave.</param>
		protected virtual void GripMouseEnterOrLeave( bool bEnter )
		{
			if( bEnter )
			{
				_bMouseOnGrip = true;
				_cursorDefault = _control.Cursor;
				_control.Cursor = Cursors.SizeWE;
			}
			else
			{
				_bMouseOnGrip = false;
				_control.Cursor = _cursorDefault != null ? _cursorDefault : Cursors.Default;
			}
		}

		/// <summary>
		/// Tests whether the point specified (in client coordinates) is on the toolbar's grip.
		/// </summary>
		protected virtual bool PointOnGrip( int x, int y )
		{
			return _rectClient.Contains( new Point( x, y ) );
		}

		/// <summary>
		/// Calculates the relative dragging position, that is, the distance between the mouse position and the upper-left point of the grip's client area
		/// </summary>
		private Size GetDragOffset()
		{
			Point ptMouseRel = _control.PointToClient( Control.MousePosition ); // Mouse coordinates within the parent control
			return (Size) ptMouseRel - (Size) _rectClient.Location; // Drag offset, offset between the mouse position and the grip's client coordinates
		}

		#endregion

		#region Attributes

		/// <summary>
		/// The controlled Control.
		/// </summary>
		public Control Control
		{
			get { return _control; }
		}

		/// <summary>
		/// Command bar site of the controlled control.
		/// </summary>
		public ICommandBarSite Site
		{
			get { return _site; }
		}

		/// <summary>
		/// Gets whether the grip is currently being dragged.
		/// </summary>
		public bool IsDraggingGrip
		{
			get { return _isDraggingGrip; }
		}

		/// <summary>
		/// Gets whether mouse cursor is currently on the grip (and the cursor has the form appropriate).
		/// </summary>
		public bool MouseOnGrip
		{
			get { return _bMouseOnGrip; }
		}

		#endregion

		#region Operations

		/// <summary>
		/// Subtracts the space occupied by the grip from the provided client rectangle (which should not include the border).
		/// </summary>
		/// <param name="client">Source client rectangle.</param>
		/// <returns>The decreased client rectangle.</returns>
		public Rectangle OnLayout( Rectangle client )
		{
			int nMyWidth = c_nHorPadding + c_nDotCellSize;
			_rectClient = new Rectangle( client.Location, new Size( nMyWidth, client.Height ) ); // Remember the Grip's client rect
			return Rectangle.FromLTRB( _rectClient.Right, client.Top, client.Right, client.Bottom ); // Exclude it from the parent's client rect
		}

		/// <summary>
		/// Paints the grip.
		/// </summary>
		public void OnPaint( Graphics graphics )
		{
			int nLeft = _rectClient.Left + c_nHorPadding;
			int nHeightAvail = _rectClient.Height - c_nVerPadding * 2; // Height available for the grip dots
			int nDots = (nHeightAvail + (c_nDotCellSize - c_nDotSize)) / c_nDotCellSize; // Number of dots; exclude padding from the lowermost cell
			int nUsedHeight = (c_nDotCellSize * nDots) - (c_nDotCellSize - c_nDotSize); // Height exactly occupied by the dots
			int nTop = _rectClient.Top + c_nVerPadding + (nHeightAvail - nUsedHeight) / 2; // Starting y-coordinate
			for( int a = 0; a < nDots; a++, nTop += c_nDotCellSize ) // Jump one cell
			{
				graphics.FillRectangle( SystemBrushes.ControlLightLight, nLeft + 1, nTop + 1, c_nDotSize, c_nDotSize );
				graphics.FillRectangle( SystemBrushes.ControlDark, nLeft, nTop, c_nDotSize, c_nDotSize );
			}
		}

		/// <summary>
		/// Assigns the command bar site.
		/// </summary>
		public void SetSite( ICommandBarSite site )
		{
			_site = site;
		}

		#endregion
	}

	#region Class LayoutSuspender — Suspends the layout of the given control and resumes it automatically when disposed of.

	/// <summary>
	/// Suspends the layout of the given control and resumes it automatically when disposed of.
	/// </summary>
	public class LayoutSuspender : IDisposable
	{
		#region Data

		/// <summary>
		/// Control that is being suspended.
		/// </summary>
		private Control _control = null;

		#endregion

		#region Construction

		/// <summary>
		/// Suspends the layout of the following control and resumes it when disposed of.
		/// </summary>
		/// <param name="control">Control to be suspended/resumed. Must not be <c>Null</c>.</param>
		public LayoutSuspender( Control control )
		{
			if( control == null )
				throw new ArgumentNullException( "Cannot suspend layout of a Null control." );
			_control = control;
			_control.SuspendLayout();
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Resumes the control's layout.
		/// </summary>
		public void Dispose()
		{
			_control.ResumeLayout();
			_control = null;
		}

		#endregion
	}

	#endregion
}
