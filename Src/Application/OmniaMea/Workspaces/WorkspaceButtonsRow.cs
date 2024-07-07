// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Workspaces;
using JetBrains.UI.Components.ImageListButton;
using JetBrains.UI.Interop;

namespace JetBrains.Omea
{
	/// <summary>
	/// Implements a row for the Omea Main Frame that hosts the workspace buttons and shortcuts control.
	/// </summary>
	public class WorkspaceButtonsRow : UserControl, ICommandBarSite, IBackgroundBrushProvider
	{
		#region Data

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		/// <summary>
		/// A child control that renders the workspace buttons bar.
		/// </summary>
		internal WorkspaceButtonsManager _workspaceButtonsManager;

		/// <summary>
		/// A child control that renders the shortcuts bar.
		/// </summary>
		internal ShortcutBar _barShortcut;

		/// <summary>
		/// Cached size of the workspace bar title width.
		/// </summary>
		protected int _nTitleWidth;

		/// <summary>
		/// A base color from which the other colors are produced.
		/// </summary>
		protected static Color _colorBase = Color.FromArgb( 224, 226, 235 );

		/// <summary>
		/// Stores the visibility of the workspace buttons.
		/// Upon load, restored from the settings by the <see cref="MainFrame"/>.
		/// </summary>
		private bool _bWorkspaceButtonsVisible = true;

		/// <summary>
		/// Stores the visibility of the shortcut bar.
		/// Upon load, restored from the settings by the <see cref="MainFrame"/>.
		/// </summary>
		private bool _bShortcutBarVisible = true;

		/// <summary>
		/// Regulates the desired width of the <see cref="_barShortcut"/> toolbar, as it was adjusted by the user.
		/// Initially populated with the <see cref="ICommandBar.OptimalSize"/> of the toolbar in <see cref="InitializeComponentSelf"/>.
		/// Persisted in settings.
		/// </summary>
		protected int _nDesiredToolbarWidth;

		#endregion

		#region Ctor/Dtor

		public WorkspaceButtonsRow()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponentSelf();

			// Calculate the text label size
			_nTitleWidth = JetLinkLabel.GetTextSize( this, Text, Font ).Width;
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
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponentSelf()
		{
			components = new Container();
			using( new LayoutSuspender( this ) )
			{
				// _barShortcut
				_barShortcut = new ShortcutBar();
				_barShortcut.SetSite( this );
				_barShortcut.AllowDrop = true;
				_barShortcut.BackColor = SystemColors.Control;
				_barShortcut.ColorScheme = null;
				_barShortcut.Name = "_shortcutBar";
				_barShortcut.Size = new Size( _barShortcut.OptimalSize.Width, 27 );
				_barShortcut.TabIndex = 0;
				_nDesiredToolbarWidth = 300; // Take 300 pixels of width by default

				// This Control
				Controls.Add( _barShortcut );

				_workspaceButtonsManager = new WorkspaceButtonsManager( this );
				_workspaceButtonsManager.SetSite( this );

				Text = "Workspaces";
				Font = new Font( "Tahoma", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((Byte)(204)) );
				Height = 39;
				TextChanged += new EventHandler( OnTitleChanged );
				FontChanged += new EventHandler( OnTitleChanged );

				SetStyle( ControlStyles.AllPaintingInWmPaint
					| ControlStyles.CacheText
					| ControlStyles.ContainerControl
					| ControlStyles.ResizeRedraw
					| ControlStyles.Selectable
					| ControlStyles.UserPaint
					| ControlStyles.Opaque
				          , true );
			}
		}

		#endregion

		#region ICommandBarSite Interface Members

		public bool RequestMove( ICommandBar sender, Size offset )
		{
			if( !Object.ReferenceEquals( sender, _barShortcut ) )
				throw new InvalidOperationException();

			_nDesiredToolbarWidth = _barShortcut.Width - offset.Width; // Calc the new desired size
			_nDesiredToolbarWidth = _nDesiredToolbarWidth >= 1 ? _nDesiredToolbarWidth : 1; // Constrain
			PerformLayout(); // Apply the new desired width to the layout

			Refresh(); // Request the immediate update

			return true;
		}

		public bool RequestSize( ICommandBar sender, Size difference )
		{
			throw new NotImplementedException();
		}

		public bool PerformLayout( ICommandBar sender )
		{
			Core.UserInterfaceAP.QueueJob( "Layout the Row", new MethodInvoker( PerformLayout ) );
			return true;
		}

		#endregion

		#region Painting

		/// <summary>
		/// Colors used in painting.
		/// Are filled in by <see cref="SetColors"/> invoked from the <see cref="OnLayout"/> step
		/// just because in this case it won't happen on each painting, and the layouter is guaranteed to be called on each change
		/// that may potentionally affect the coloring.
		/// </summary>
		public struct Colors
		{
			/// <summary>
			/// Base color for the border (with which its internals is filled; analogous to the <see cref="SystemColors.Control"/> color).
			/// </summary>
			public Color BorderBase;

			/// <summary>
			/// Light color for the border (with which its highlighted parts are filled; analogous to the <see cref="SystemColors.ControlLightLight"/> color).
			/// </summary>
			public Color BorderLight;

			/// <summary>
			/// Dark color for the border (with which its shadowed parts are filled; analogous to the <see cref="SystemColors.ControlDark"/> color).
			/// </summary>
			public Color BorderDark;

			/// <summary>
			/// Color of the shadow that the border casts on the background.
			/// </summary>
			public Color BorderShadow;
		}

		private Colors _colors = new Colors();

		/// <summary>
		/// Paints the workspace bar (background, borders & separators) according to the rectangles that were defined by the layouting procedure.
		/// </summary>
		protected override void OnPaint( PaintEventArgs e )
		{
			using( Brush brushBackAboveBorder = GetBackgroundBrush( ClientRectangle, true ) )
			using( Brush brushBackBelowBorder = GetBackgroundBrush( ClientRectangle, false ) )
			{
				// Implement the painting
				foreach( Rects.Drawing drawing in _rects.Drawings )
				{
					switch( drawing.What )
					{
					case DrawType.BackAboveBorder: // Background above the border
						e.Graphics.FillRectangle( brushBackAboveBorder, drawing.Rect );
						break;
					case DrawType.BackBelowBorder: // Background below the border
						e.Graphics.FillRectangle( brushBackBelowBorder, drawing.Rect );
						break;
					case DrawType.BorderHor: // Horizontal borders
						DrawBorder( drawing.Rect, e.Graphics, drawing.What );
						break;
					case DrawType.BorderVerLeft: // Vertical borders
						goto case DrawType.BorderHor;
					case DrawType.BorderVerRight: // Vertical borders
						goto case DrawType.BorderHor;
					case DrawType.CornerLeftTop: // Corners
						DrawBorderCorner( drawing.Rect, e.Graphics, drawing.What );
						break;
					case DrawType.CornerLeftBottom:
						goto case DrawType.CornerLeftTop;
					case DrawType.CornerRightBottom:
						goto case DrawType.CornerLeftTop;
					case DrawType.CornerRightTop:
						goto case DrawType.CornerLeftTop;
					case DrawType.Separator: // Separators between the WSP buttons
						int nMiddle = drawing.Rect.Left + (drawing.Rect.Width - 2) / 2;
						e.Graphics.FillRectangle( SystemBrushes.ControlDark, Rectangle.FromLTRB( nMiddle, drawing.Rect.Top + 1, nMiddle + 1, drawing.Rect.Bottom - 1 ) );
						e.Graphics.FillRectangle( SystemBrushes.ControlLightLight, Rectangle.FromLTRB( nMiddle + 1, drawing.Rect.Top + 1, nMiddle + 2, drawing.Rect.Bottom - 1 ) );
						// TODO: remove
						//e.Graphics.DrawLine( SystemPens.ControlDark, new Point( nMiddle, drawing.Rect.Top + 1 ), new Point( nMiddle, drawing.Rect.Bottom - 2 ) );
						//e.Graphics.DrawLine( SystemPens.ControlLightLight, new Point( nMiddle + 1, drawing.Rect.Top + 1 ), new Point( nMiddle + 1, drawing.Rect.Bottom - 2 ) );
						break;
					case DrawType.TitleText: // Draw the title
						JetLinkLabel.DrawText( e.Graphics, Text, drawing.Rect, Font, SystemColors.ControlText, DrawTextFormatFlags.DT_CENTER | DrawTextFormatFlags.DT_NOPREFIX | DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_VCENTER );
						break;
					}
				}
			}
		}

		/// <summary>
		/// Draws a piece of the horizontal border within the bounding rectangle specified.
		/// </summary>
		/// <param name="rect">Bounding rectangle of the border. The whole border is fit into it, including its frame.</param>
		/// <param name="what">Type of the border to draw, the accepted values are <see cref="DrawType.BorderHor"/>, <see cref="DrawType.BorderVerLeft"/>, and <see cref="DrawType.BorderVerRight"/>.</param>
		private void DrawBorder( Rectangle rect, Graphics g, DrawType what )
		{
			using( Brush brushLight = new SolidBrush( _colors.BorderLight ) )
			using( Brush brushDark = new SolidBrush( _colors.BorderDark ) )
			using( Brush brushShadow = new SolidBrush( _colors.BorderShadow ) )
			using( Brush brushBase = new SolidBrush( _colors.BorderBase ) )
			{
				switch( what )
				{
				case DrawType.BorderHor:
					g.FillRectangle( brushLight, Rectangle.FromLTRB( rect.Left, rect.Top, rect.Right, rect.Top + 1 ) ); // Upper light line
					g.FillRectangle( brushBase, Rectangle.FromLTRB( rect.Left, rect.Top + 1, rect.Right, rect.Bottom - 2 ) ); // Body
					g.FillRectangle( brushDark, Rectangle.FromLTRB( rect.Left, rect.Bottom - 2, rect.Right, rect.Bottom - 1 ) ); // Lower very dark line
					g.FillRectangle( brushShadow, Rectangle.FromLTRB( rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom ) ); // Lowermost slightly dark line
					// TODO: remove
//					g.DrawLine( penLight, rect.Left, rect.Top, rect.Right, rect.Top ); // Upper light line
//					g.FillRectangle( brushBase, Rectangle.FromLTRB( rect.Left, rect.Top + 1, rect.Right, rect.Bottom - 2 ) ); // Body
//					g.DrawLine( penDark, rect.Left, rect.Bottom - 2, rect.Right, rect.Bottom - 2 ); // Lower very dark line
//					g.DrawLine( penShadow, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1 ); // Lowermost slightly dark line
					break;
				case DrawType.BorderVerLeft:
					g.FillRectangle( brushLight, Rectangle.FromLTRB( rect.Left, rect.Top, rect.Left + 1, rect.Bottom ) ); // Left light line
					g.FillRectangle( brushBase, Rectangle.FromLTRB( rect.Left + 1, rect.Top, rect.Right - 2, rect.Bottom ) ); // Body
					g.FillRectangle( brushDark, Rectangle.FromLTRB( rect.Right - 2, rect.Top, rect.Right - 1, rect.Bottom ) ); // Right very dark line
					g.FillRectangle( brushShadow, Rectangle.FromLTRB( rect.Right - 1, rect.Top, rect.Right, rect.Bottom ) ); // Rightmost slightly dark line

					// TODO: remove
//					g.DrawLine( brushLight, rect.Left, rect.Top, rect.Left, rect.Bottom - 1 ); // Left light line
//					g.FillRectangle( brushBase, Rectangle.FromLTRB( rect.Left + 1, rect.Top, rect.Right - 2, rect.Bottom ) ); // Body
//					g.DrawLine( brushDark, rect.Right - 2, rect.Top, rect.Right - 2, rect.Bottom - 1 ); // Right very dark line
//					g.DrawLine( brushShadow, rect.Right - 1, rect.Top, rect.Right - 1, rect.Bottom - 1 ); // Rightmost slightly dark line
					break;
				case DrawType.BorderVerRight:
					g.FillRectangle( brushLight, Rectangle.FromLTRB( rect.Left, rect.Top, rect.Left + 1, rect.Bottom ) ); // Left light line
					g.FillRectangle( brushBase, Rectangle.FromLTRB( rect.Left + 1, rect.Top, rect.Right - 1, rect.Bottom ) ); // Body
					g.FillRectangle( brushShadow, Rectangle.FromLTRB( rect.Right - 1, rect.Top, rect.Right, rect.Bottom ) ); // Right dark line
					// TODO: remove
//					g.DrawLine( brushLight, rect.Left, rect.Top, rect.Left, rect.Bottom - 1 ); // Left light line
//					g.FillRectangle( brushBase, Rectangle.FromLTRB( rect.Left + 1, rect.Top, rect.Right - 1, rect.Bottom ) ); // Body
//					g.DrawLine( brushShadow, rect.Right - 1, rect.Top, rect.Right - 1, rect.Bottom - 1 ); // Right dark line
					break;
				default:
					throw new InvalidOperationException( "Unknown border type in the border-drawing routine." );
				}
			}
		}

		/// <summary>
		/// Draws a specific corner of the border.
		/// </summary>
		private void DrawBorderCorner( Rectangle rect, Graphics g, DrawType corner )
		{
			using( Brush brushLight = new SolidBrush( _colors.BorderLight ) )
			using( Brush brushDark = new SolidBrush( _colors.BorderDark ) )
			using( Brush brushShadow = new SolidBrush( _colors.BorderShadow ) )
			using( Brush brushBase = new SolidBrush( _colors.BorderBase ) )
			{
				switch( corner )
				{
				case DrawType.CornerLeftTop:
					// Base
					g.FillRectangle( brushBase, Rectangle.FromLTRB( rect.Left + 1, rect.Top + 1, rect.Right, rect.Bottom ) );

					// Light frame
					g.FillRectangle( brushLight, Rectangle.FromLTRB( rect.Left + 2, rect.Top, rect.Right, rect.Top + 1 ) );
					g.FillRectangle( brushLight, new Rectangle( new Point( rect.Left + 1, rect.Top + 1 ), new Size( 1, 1 ) ) );
					g.FillRectangle( brushLight, Rectangle.FromLTRB( rect.Left, rect.Top + 2, rect.Left + 1, rect.Bottom ) );

					// Dark frame
					g.FillRectangle( brushDark, new Rectangle( new Point( rect.Right - 1, rect.Bottom - 2 ), new Size( 1, 1 ) ) );
					g.FillRectangle( brushDark, new Rectangle( new Point( rect.Right - 2, rect.Bottom - 1 ), new Size( 1, 1 ) ) );

					// Shadow
					g.FillRectangle( brushShadow, new Rectangle( new Point( rect.Right - 1, rect.Bottom - 1 ), new Size( 1, 1 ) ) );

					break;
				case DrawType.CornerRightTop:
					// Base
					g.FillRectangle( brushBase, Rectangle.FromLTRB( rect.Left, rect.Top + 1, rect.Right - 1, rect.Bottom ) );

					// Light frame
					g.FillRectangle( brushLight, Rectangle.FromLTRB( rect.Left, rect.Top, rect.Right - 2, rect.Top + 1 ) );
					g.FillRectangle( brushLight, new Rectangle( new Point( rect.Left, rect.Bottom - 1 ), new Size( 1, 1 ) ) );

					// Dark frame (in color of shadow)
					g.FillRectangle( brushShadow, new Rectangle( new Point( rect.Right - 2, rect.Top + 1 ), new Size( 1, 1 ) ) );
					g.FillRectangle( brushShadow, Rectangle.FromLTRB( rect.Right - 1, rect.Top + 2, rect.Right, rect.Bottom ) );

					break;
				case DrawType.CornerRightBottom:
					// Base
					g.FillRectangle( brushBase, Rectangle.FromLTRB( rect.Left, rect.Top, rect.Right - 2, rect.Bottom - 2 ) );

					// Light frame
					g.FillRectangle( brushLight, new Rectangle( rect.Location, new Size( 1, 1 ) ) );

					// Dark frame
					g.FillRectangle( brushDark, Rectangle.FromLTRB( rect.Right - 2, rect.Top, rect.Right - 1, rect.Bottom - 2 ) );
					g.FillRectangle( brushDark, Rectangle.FromLTRB( rect.Left, rect.Bottom - 2, rect.Right - 2, rect.Bottom - 1 ) );

					// Shadow
					g.FillRectangle( brushShadow, Rectangle.FromLTRB( rect.Right - 1, rect.Top, rect.Right, rect.Bottom - 3 ) );
					g.FillRectangle( brushShadow, Rectangle.FromLTRB( rect.Left, rect.Bottom - 1, rect.Right - 3, rect.Bottom ) );
					g.FillRectangle( brushShadow, new Rectangle( new Point( rect.Right - 2, rect.Bottom - 2 ), new Size( 1, 1 ) ) );

					break;
				case DrawType.CornerLeftBottom:
					// Base
					g.FillRectangle( brushBase, Rectangle.FromLTRB( rect.Left + 1, rect.Top, rect.Right, rect.Bottom - 2 ) );

					// Light frame
					g.FillRectangle( brushLight, Rectangle.FromLTRB( rect.Left, rect.Top, rect.Left + 1, rect.Bottom - 3 ) );

					// Dark frame
					g.FillRectangle( brushDark, Rectangle.FromLTRB( rect.Right - 2, rect.Bottom - 2, rect.Right, rect.Bottom - 1 ) );

					// Shadow
					g.FillRectangle( brushShadow, new Rectangle( new Point( rect.Right - 1, rect.Top ), new Size( 1, 1 ) ) );
					g.FillRectangle( brushShadow, new Rectangle( new Point( rect.Right - 1, rect.Bottom - 1 ), new Size( 1, 1 ) ) );

					break;
				default:
					throw new InvalidOperationException( "The drawing type passed in is not for a corner." );
				}
			}
		}

		/// <summary>
		/// Produces the colors needed for painting and fills the <see cref="Colors"/> structure with them.
		/// </summary>
		private void SetColors()
		{
			// If the default workspace is selected, or the workspace manager is not available yet, use the default colors for the bar
			IResource workspace = Core.State == CoreState.Running ? Core.WorkspaceManager.ActiveWorkspace : null; // Persist to prevent from race conditions
			WorkspaceUIManager ui = new WorkspaceUIManager( workspace );

			_colors.BorderBase = ui.GetWorkspaceColor( WorkspaceUIManager.Colors.Base );
			_colors.BorderLight = ui.GetWorkspaceColor( WorkspaceUIManager.Colors.Light );
			_colors.BorderDark = ui.GetWorkspaceColor( WorkspaceUIManager.Colors.Dark );
			_colors.BorderShadow = ui.GetWorkspaceColor( WorkspaceUIManager.Colors.Shadow );

			// TODO: remove
			/*
			if( workspace == null ) // The default workspace is selected, or the core is not running yet
			{
				_colors.BorderBase = SystemColors.Control;
				_colors.BorderLight = SystemColors.ControlLightLight;
				_colors.BorderDark = SystemColors.ControlDarkDark;
				_colors.BorderShadow = SystemColors.ControlDark;
			}
			else // Use the workspace's colors
			{
				double weight = (double) ColorManagement.MaxHLS / 100; // Converts the hue from 0 … 100 to the desired range

				// Hue of the border color
				_colors.BorderHue = WorkspaceUIManager.GetWorkspaceColorHue( workspace );

				_colors.BorderBase = ColorManagement.HLStoRGB( _colors.BorderHue, (ushort) (38 * weight), (ushort) (79 * weight) );
				_colors.BorderLight = ColorManagement.HLStoRGB( _colors.BorderHue, (ushort) (56 * weight), (ushort) (50 * weight) );
				_colors.BorderDark = ColorManagement.HLStoRGB( _colors.BorderHue, (ushort) (27 * weight), (ushort) (74 * weight) );
				_colors.BorderShadow = SystemColors.ControlDark;

				/ *
				_colors.BorderBase = ColorManagement.HLStoRGB( hueBorder, (ushort) (47 * weight), (ushort) (93 * weight) );
				_colors.BorderLight = ColorManagement.HLStoRGB( hueBorder, (ushort) (69* weight), (ushort) (95* weight) );
				_colors.BorderDark = ColorManagement.HLStoRGB( hueBorder, (ushort) (34 * weight), (ushort) (81 * weight) );
				* /
			}
		*/
		}

		/// <summary>
		/// Creates a brush for either of the two fill types of this control.
		/// </summary>
		/// <param name="rect">If the brush to be created is a gradient brush, this rectangle should define its application rect.</param>
		/// <param name="bAboveBorder">Whether the brush requested is a brush above the border (gradient) or below it.</param>
		/// <returns>The requested brush</returns>
		protected Brush GetBackgroundBrush( Rectangle rect, bool bAboveBorder )
		{
			return bAboveBorder ? (Brush)new LinearGradientBrush( rect, SystemColors.ControlLightLight, SystemColors.Control, LinearGradientMode.Vertical ) : (Brush)new SolidBrush( SystemColors.Control );

		}

		#endregion

		#region Layouting

		#region Layouting Markers

		internal class Rects
		{
			#region Parts

			/// <summary>
			/// Title part bound box.
			/// </summary>
			public Rectangle TitlePart;

			/// <summary>
			/// WSP part bound box.
			/// </summary>
			public Rectangle WspPart;

			/// <summary>
			/// Shortcuts part bound box.
			/// </summary>
			public Rectangle ShortcutsPart;

			/// <summary>
			/// Free space part bound box.
			/// </summary>
			public Rectangle FreeSpacePart;

			#endregion

			#region Parts — Wsp Part

			/// <summary>
			/// Box within which the workspace buttons should be placed (<see cref="WspPart"/>).
			/// </summary>
			public Rectangle WspButtonsBox;

			#endregion

			#region Operations

			/// <summary>
			/// Resets the object.
			/// Clears all the rects lists.
			/// </summary>
			public void Clear()
			{
				Drawings.Clear();
			}

			#endregion

			/// <summary>
			/// A structure that describes one drawing: a rectangle and what should be drawn in it.
			/// </summary>
			internal struct Drawing
			{
				/// <summary>
				/// Rectangle that should be drawn out of this struct.
				/// </summary>
				public Rectangle Rect;

				/// <summary>
				/// What should be drawn in it.
				/// </summary>
				public DrawType What;

				/// <summary>
				/// Initializes the instance.
				/// </summary>
				public Drawing( DrawType what, Rectangle rc )
				{
					Rect = rc;
					What = what;
				}
			}

			/// <summary>
			/// The list of drawings (rectangles + what-to-draw).
			/// </summary>
			public ArrayList Drawings = new ArrayList();

			public void Draw( DrawType what, Rectangle rc )
			{
				Drawings.Add( new Drawing( what, rc ) );
			}
		}

		/// <summary>
		/// The layout rectangles.
		/// </summary>
		private Rects _rects = new Rects();

		#endregion

		#region Layouting Constants

		/// <summary>
		/// Layouting Constants
		/// </summary>
		internal struct Const
		{
			/// <summary>
			/// Padding to the right and to the left of the title text.
			/// </summary>
			public static readonly int TitleHorPadding = 10;

			/// <summary>
			/// Border width (including its frames), in its main, straight part (the straight border along the lower side of the workspace buttons row).
			/// </summary>
			public static readonly int StraightBorderWidth = 6;

			/// <summary>
			/// Border width (including its frames) of the left vertical border.
			/// </summary>
			public static readonly int VerticalLeftBorderWidth = 5;

			/// <summary>
			/// Border width (including its frames) of the right vertical border.
			/// </summary>
			public static readonly int VerticalRightBorderWidth = 4;

			/// <summary>
			/// Width of the upper horizontal border along the upper side of the workspace buttons row, including the frames.
			/// </summary>
			public static readonly int UpperBorderWidth = 5;

			/// <summary>
			/// Padding to the right and to the left of the workspace bar.
			/// </summary>
			public static readonly int WorkspaceBarHorPadding = 10;

			/// <summary>
			/// Padding to the right and to the left of the shortcut bar.
			/// </summary>
			public static readonly int ShortcutBarHorPadding = 2;

			/// <summary>
			/// Padding above and below the shortcut bar.
			/// </summary>
			public static readonly int ShortcutBarVerPadding = 2;

			/// <summary>
			/// Padding above the upper border in its raised part (WSP bar part).
			/// </summary>
			public static readonly int UpperBorderTopPadding = 4;

			/// <summary>
			/// Spacing between the two controls if there's a vertical border between them.
			/// </summary>
			public static readonly int HorSpacingWhenBorder = 12;

			/// <summary>
			/// Width of the separator line between the controls.
			/// </summary>
			public static readonly int SeparatorWidth = 2;

			/// <summary>
			/// Horizontal spacing between the controls in case there's a vertical separator between them.
			/// </summary>
			public static readonly int HorSpacingWhenSeparator = 12;
		}

		/// <summary>
		/// Type of the drawing rectangle.
		/// </summary>
		internal enum DrawType
		{
			/// <summary>
			/// Horizontal border piece.
			/// </summary>
			BorderHor,

			/// <summary>
			/// Vertical border piece, left one (with LT and RB corners).
			/// </summary>
			BorderVerLeft,

			/// <summary>
			/// Vertical border piece, left one (with RT and LB corners).
			/// </summary>
			BorderVerRight,

			/// <summary>
			/// A place with gradiented background.
			/// </summary>
			BackAboveBorder,

			/// <summary>
			/// A lower place that should be backgrounded with a plain color.
			/// </summary>
			BackBelowBorder,

			/// <summary>
			/// A vertical separator between the controls should be drawn here.
			/// </summary>
			Separator,

			/// <summary>
			/// Left-top corner.
			/// </summary>
			CornerLeftTop,

			/// <summary>
			/// Right-top corner.
			/// </summary>
			CornerRightTop,

			/// <summary>
			/// Right-bottom corner.
			/// </summary>
			CornerRightBottom,

			/// <summary>
			/// Left-bottom corner.
			/// </summary>
			CornerLeftBottom,

			/// <summary>
			/// The title text.
			/// </summary>
			TitleText
		}

		#endregion

		protected override void OnLayout( LayoutEventArgs levent )
		{
			using( new LayoutSuspender( _barShortcut ) )
			{
				// Pick the client rectangle and use it furtherly for layouting
				Rectangle rcClient = ClientRectangle;

				// Extreme components' widths (calculate the workspace buttons sizes only if they're not turned off)
				Size sizeWorkspaceMax = _bWorkspaceButtonsVisible ? _workspaceButtonsManager.MaxSize : Size.Empty;
				Size sizeWorkspaceMin = _bWorkspaceButtonsVisible ? _workspaceButtonsManager.MinSize : Size.Empty;
				Size sizeShortcutsMax = _barShortcut.MaxSize;
				Size sizeShortcutsMin = _barShortcut.MinSize;

				///////////////////////
				// Widths of the parts
				int nTitlePart;
				int nWspPart;
				int nShortcutsPart;
				int nFreeSpacePart;

				OnLayout_NegotiateParts( rcClient, sizeShortcutsMin, sizeShortcutsMax, sizeWorkspaceMin, sizeWorkspaceMax, out nTitlePart, out nWspPart, out nShortcutsPart, out nFreeSpacePart );

				//////////
				// Calculate the parts' rectangles as we now known their sizes
				_rects.TitlePart = new Rectangle( rcClient.Left, rcClient.Top, nTitlePart, rcClient.Height );
				_rects.WspPart = new Rectangle( _rects.TitlePart.Right, rcClient.Top, nWspPart, rcClient.Height );
				_rects.FreeSpacePart = new Rectangle( _rects.WspPart.Right, rcClient.Top, nFreeSpacePart, rcClient.Height );
				_rects.ShortcutsPart = new Rectangle( _rects.FreeSpacePart.Right, rcClient.Top, nShortcutsPart, rcClient.Height );
				if( _rects.ShortcutsPart.Right != rcClient.Right )
					throw new InvalidOperationException( "Invalid part widths sum." );

				// Reset the collections
				_rects.Clear();

				// Layout the individual parts
				OnLayout_TitlePart();
				OnLayout_WspPart();
				OnLayout_FreeSpacePart();
				OnLayout_ShortcutsPart();
			}

			// Update the colors
			SetColors();

			// Apply the visual changes
			Invalidate( true );
		}

		/// <summary>
		/// A sub-function of the <see cref="OnLayout"/> function.
		/// Negotiates the sizes of the main parts according to the user draggings and min/max size constraints,
		/// returns the part sizes or zeros if the corresponding part should be turned off.
		/// </summary>
		private void OnLayout_NegotiateParts( Rectangle rcClient, Size sizeShortcutsMin, Size sizeShortcutsMax, Size sizeWorkspaceMin, Size sizeWorkspaceMax, out int nTitlePart, out int nWspPart, out int nShortcutsPart, out int nFreeSpacePart )
		{
			// Min-sizes for the parts
			int nShortcutsPartMin;
			int nWspPartMin;

			// Title part size is fixed, calculate it (don't show if the workspace buttons are not visible)
			nTitlePart = _bWorkspaceButtonsVisible ? _nTitleWidth + Const.TitleHorPadding * 2 : 0;

			// Shortcuts part, the constrained user-defined size
			if( _bShortcutBarVisible )
				_barShortcut.Width = _nDesiredToolbarWidth <= sizeShortcutsMax.Width ? (_nDesiredToolbarWidth >= sizeShortcutsMin.Width ? _nDesiredToolbarWidth : sizeShortcutsMin.Width) : sizeShortcutsMax.Width; // Take the constrained desired size as a starting point
			nShortcutsPart = _bShortcutBarVisible ? _barShortcut.Width + Const.ShortcutBarHorPadding * 2 : 0;
			nShortcutsPartMin = _bShortcutBarVisible ? sizeShortcutsMin.Width + Const.ShortcutBarHorPadding * 2 : 0;

			// Wsp part, the rest
			nWspPartMin = sizeWorkspaceMin.Width;
			nWspPart = rcClient.Width - nTitlePart - nShortcutsPart;

			// Does not fit? Shrink the title & recalc
			if( nWspPart < nWspPartMin )
			{
				nTitlePart = 0;
				nWspPart = rcClient.Width - nTitlePart - nShortcutsPart;
			}

			// Still unfit? Strip the shortcuts down (to their min size, if needed)
			if( nWspPart < nWspPartMin )
			{
				// Let shortcuts part occupy all the available space left from the WSP part (do not allow to grow)
				nShortcutsPart = rcClient.Width - nTitlePart - nWspPartMin < nShortcutsPart ? rcClient.Width - nTitlePart - nWspPartMin : nShortcutsPart;
				nShortcutsPart = nShortcutsPart >= nShortcutsPartMin ? nShortcutsPart : nShortcutsPartMin; // Constrain to the min-size
				nWspPart = rcClient.Width - nTitlePart - nShortcutsPart;
			}

			// No? Throw the shortcuts away
			if( nWspPart < nWspPartMin )
			{
				nShortcutsPart = 0;
				nWspPart = rcClient.Width - nTitlePart - nShortcutsPart;
			}

			// Finally, throw away everything
			if( nWspPart < nWspPartMin )
			{
				nWspPart = 0;
			}

			//////////////

			// Constrain the WspPart size
			nWspPart = nWspPart <= sizeWorkspaceMax.Width ? nWspPart : sizeWorkspaceMax.Width;

			// Calculate the free part size
			nFreeSpacePart = rcClient.Width - nTitlePart - nWspPart - nShortcutsPart;

			// Check for errors, set all to free space if have problems
			if( (nTitlePart < 0) || (nWspPart < 0) || (nShortcutsPart < 0) || (nFreeSpacePart < 0) )
			{
				nTitlePart = nWspPart = nShortcutsPart = 0;
				nFreeSpacePart = rcClient.Width;
			}
		}

		/// <summary>
		/// A part of the <see cref="OnLayout"/> method.
		/// Layouts the rects inside the title part.
		/// </summary>
		private void OnLayout_TitlePart()
		{
			Rectangle part = _rects.TitlePart;

			if( part.Width == 0 ) // Part turned off?
				return;
			else
			{
				Rectangle rcWithoutBorder = new Rectangle( part.Location, new Size( part.Width, part.Height - Const.StraightBorderWidth ) );

				// Background
				_rects.Draw( DrawType.BackAboveBorder, rcWithoutBorder );

				// Title text
				_rects.Draw( DrawType.TitleText, Rectangle.FromLTRB( part.Left + Const.TitleHorPadding, part.Top + Const.UpperBorderTopPadding + Const.UpperBorderWidth, part.Right - Const.TitleHorPadding, part.Bottom - Const.StraightBorderWidth ) );

				// Border
				_rects.Draw( DrawType.BorderHor, new Rectangle( rcWithoutBorder.Left, rcWithoutBorder.Bottom, part.Width, Const.StraightBorderWidth ) );
			}
		}

		/// <summary>
		/// A part of the <see cref="OnLayout"/> method.
		/// Layouts the rects inside the free space part.
		/// </summary>
		private void OnLayout_FreeSpacePart()
		{
			Rectangle part = _rects.FreeSpacePart;

			// Off?
			if( part.Width == 0 )
				return;

			// Background
			_rects.Draw( DrawType.BackAboveBorder, new Rectangle( part.Left, part.Top, part.Width, part.Height - Const.StraightBorderWidth ) );

			// Lower border
			_rects.Draw( DrawType.BorderHor, new Rectangle( part.Left, part.Bottom - Const.StraightBorderWidth, part.Width, Const.StraightBorderWidth ) );
		}

		/// <summary>
		/// A part of the <see cref="OnLayout"/> method.
		/// Layouts the rects inside the shortcuts bar part.
		/// </summary>
		private void OnLayout_ShortcutsPart()
		{
			Rectangle part = _rects.ShortcutsPart;

			// Off?
			if( part.Width == 0 )
			{
				_barShortcut.Visible = false;
				return;
			}

			Rectangle rcWithoutBorder = new Rectangle( part.Location, new Size( part.Width, part.Height - Const.StraightBorderWidth ) );

			// Background
			_rects.Draw( DrawType.BackAboveBorder, rcWithoutBorder );

			// Lower border
			_rects.Draw( DrawType.BorderHor, new Rectangle( rcWithoutBorder.Left, rcWithoutBorder.Bottom, rcWithoutBorder.Width, Const.StraightBorderWidth ) );

			// Position the shortcuts bar
			Rectangle rcBar = rcWithoutBorder;
			rcBar.Inflate( -Const.ShortcutBarHorPadding, 0 );
			_barShortcut.Bounds = new Rectangle( rcBar.Left, rcBar.Top + (rcBar.Height - _barShortcut.Height) / 2, rcBar.Width, _barShortcut.Height );
			_barShortcut.Visible = true;

			// Adjust the height for the shortcut bar to fit its min-size
			Size sizeShortcutMin = _barShortcut.MinSize;
			if( _barShortcut.Height < sizeShortcutMin.Height )
				_barShortcut.Height = sizeShortcutMin.Height;
			// Ensure that it fits within the row
			if( _barShortcut.Height + Const.ShortcutBarVerPadding * 2 + Const.StraightBorderWidth >= part.Height )
			{
				Height += (_barShortcut.Height + Const.ShortcutBarVerPadding * 2 + Const.StraightBorderWidth) - part.Height; // Adjust the row height to fit the shortcut bar and its controls
				Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddMilliseconds( 500 ), "Apply layouting after growing to fit the Shortcut bar.", new MethodInvoker( PerformLayout ) );
			}
		}

		/// <summary>
		/// A part of the <see cref="OnLayout"/> method.
		/// Layouts the rects inside the workspace buttons part.
		/// </summary>
		private void OnLayout_WspPart()
		{
			Rectangle part = _rects.WspPart;

			// Output variables of the workspace buttons layouter
			int[] borders;
			int[] separators;

			// Off?
			if( (part.Width == 0) || (part.Height == 0) )
			{
				_rects.WspButtonsBox = Rectangle.Empty;
				_workspaceButtonsManager.OnLayout( _rects.WspButtonsBox, out borders, out separators ); // This should hide all the controls
				return;
			}

			// Rectangle between the upper and lower border; the workspace buttons will go here
			_rects.WspButtonsBox = Rectangle.FromLTRB( part.Left, part.Top + Const.UpperBorderTopPadding + Const.UpperBorderWidth, part.Right, part.Bottom - Const.StraightBorderWidth );

			// Check if there's enough space for the WSP buttons
			// Grow the v-size of the control, if necessary
			Size sizeWspBoxMin = _workspaceButtonsManager.MinSize;
			if( _rects.WspButtonsBox.Height < sizeWspBoxMin.Height )
			{
				Height += sizeWspBoxMin.Height - _rects.WspButtonsBox.Height;
				Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddMilliseconds( 500 ), "Relayout the bar after increasing the height to fit the WSP buttons.", new MethodInvoker( PerformLayout ) );
				return;
			}

			///////////////////////////////////////
			// Calculate the workspace buttons layout; this will also give us a hint about the botders' placement
			if( !_workspaceButtonsManager.OnLayout( _rects.WspButtonsBox, out borders, out separators ) )
			{
				// Layout has failed. Fill the part with empty stuff
				_rects.Draw( DrawType.BackAboveBorder, new Rectangle( part.Location, new Size( part.Width, part.Height - Const.StraightBorderWidth ) ) );
				_rects.Draw( DrawType.BorderHor, new Rectangle( part.Left, part.Bottom - Const.StraightBorderWidth, part.Width, Const.StraightBorderWidth ) );
				return;
			}

			// Process borders
			if( borders.Length != 2 )
			{
				Trace.WriteLine( "Warning! Race condition detected: the workspace buttons layouter failed to find the active workspace button.", "[WBR]" );
				if( borders.Length != 0 ) // Either two (valid case), either zero; otherwise is a fatal bug
					throw new InvalidOperationException( "There must be either exactly two borders, or none of them." );

				// Fill the part as if all the buttons are above-border, and add no other special cues
				_rects.Draw( DrawType.BackAboveBorder, Rectangle.FromLTRB( part.Left, part.Top, part.Right, part.Bottom - Const.StraightBorderWidth ) );
				foreach( int nPos in separators ) // All the separators
					_rects.Draw( DrawType.Separator, new Rectangle( nPos, _rects.WspButtonsBox.Top, Const.HorSpacingWhenSeparator, _rects.WspButtonsBox.Height ) ); // TODO: appropriate height for the separators
				_rects.Draw( DrawType.BorderHor, new Rectangle( part.Left, part.Bottom - Const.StraightBorderWidth, part.Width, Const.StraightBorderWidth ) );
				return;
			}
			// Sub-parts for the borders: the borders array specifies left X-coordinates for the borders spacing, the borders themselves are centered within the spacing cells, extract the sub-parts that are part-high and correspond to the borders horizontally
			Rectangle spartLeftVBorder = new Rectangle( borders[ 0 ] + (Const.HorSpacingWhenBorder - Const.VerticalLeftBorderWidth) / 2, part.Top, Const.VerticalLeftBorderWidth, part.Height ); // Rectangle of the left vertical border
			Rectangle spartRightVBorder = new Rectangle( borders[ 1 ] + (Const.HorSpacingWhenBorder - Const.VerticalRightBorderWidth) / 2, part.Top, Const.VerticalRightBorderWidth, part.Height ); // Rectangle of the right vertical border

			/////////////////////////////////////////////////////
			// Define the sub-parts around the vertical borders
			// (the vertical borders are not included in either of the sparts; the sparts occupy the whole height)
			Rectangle spartLeftmost = Rectangle.FromLTRB( part.Left, part.Top, spartLeftVBorder.Left, part.Bottom ); // Subpart to the left of the left vertical border
			Rectangle spartInbetween = Rectangle.FromLTRB( spartLeftVBorder.Right, part.Top, spartRightVBorder.Left, part.Bottom ); // Subpart between the two vertical borders
			Rectangle spartRightmost = Rectangle.FromLTRB( spartRightVBorder.Right, part.Top, part.Right, part.Bottom ); // Subpart to the right of the right vertical border

			////////////////////////////
			// Implement the Sub-parts
			Rectangle corner;

			// Implement Spart: Leftmost
			_rects.Draw( DrawType.BackAboveBorder, new Rectangle( spartLeftmost.Location, new Size( spartLeftmost.Width, spartLeftmost.Height - Const.StraightBorderWidth ) ) ); // Background above the border
			// Separators of this part
			foreach( int nPos in separators )
			{
				if( nPos < spartLeftmost.Right ) // Only those that belong to this spart
					_rects.Draw( DrawType.Separator, new Rectangle( nPos, _rects.WspButtonsBox.Top, Const.HorSpacingWhenSeparator, _rects.WspButtonsBox.Height ) );
			}
			_rects.Draw( DrawType.BorderHor, new Rectangle( spartLeftmost.Left, spartLeftmost.Bottom - Const.StraightBorderWidth, spartLeftmost.Width, Const.StraightBorderWidth ) ); // Lower border

			// Implement Spart: Left V-Border
			_rects.Draw( DrawType.BorderVerLeft, Rectangle.FromLTRB( spartLeftVBorder.Left, spartLeftVBorder.Top + Const.UpperBorderTopPadding + Const.UpperBorderWidth, spartLeftVBorder.Right, spartLeftVBorder.Bottom - Const.StraightBorderWidth ) ); // Straight vertical section of the border
			// Upper corner
			corner = new Rectangle( spartLeftVBorder.Left, part.Top + Const.UpperBorderTopPadding, Const.VerticalLeftBorderWidth, Const.UpperBorderWidth );
			_rects.Draw( DrawType.BackAboveBorder, corner );
			_rects.Draw( DrawType.CornerLeftTop, corner );
			// Lower corner
			corner = new Rectangle( spartLeftVBorder.Left, part.Bottom - Const.StraightBorderWidth, Const.VerticalLeftBorderWidth, Const.StraightBorderWidth );
			_rects.Draw( DrawType.BackBelowBorder, corner );
			_rects.Draw( DrawType.CornerRightBottom, corner );
			_rects.Draw( DrawType.BackAboveBorder, new Rectangle( spartLeftVBorder.Location, new Size( spartLeftVBorder.Width, Const.UpperBorderTopPadding ) ) ); // A bit of background above the upper corner

			// Implement Spart: In-between
			_rects.Draw( DrawType.BackAboveBorder, new Rectangle( spartInbetween.Location, new Size( spartInbetween.Width, Const.UpperBorderTopPadding ) ) ); // Background above the border
			_rects.Draw( DrawType.BorderHor, new Rectangle( spartInbetween.Left, spartInbetween.Top + Const.UpperBorderTopPadding, spartInbetween.Width, Const.UpperBorderWidth ) ); // The upper border
			_rects.Draw( DrawType.BackBelowBorder, Rectangle.FromLTRB( spartInbetween.Left, spartInbetween.Top + Const.UpperBorderTopPadding + Const.UpperBorderWidth, spartInbetween.Right, spartInbetween.Bottom ) ); // Background below the upper border

			// Implement Spart: Right V-Border
			_rects.Draw( DrawType.BorderVerRight, Rectangle.FromLTRB( spartRightVBorder.Left, spartRightVBorder.Top + Const.UpperBorderTopPadding + Const.UpperBorderWidth, spartRightVBorder.Right, spartRightVBorder.Bottom - Const.StraightBorderWidth ) ); // Straight vertical section of the border
			// Upper corner
			corner = new Rectangle( spartRightVBorder.Left, part.Top + Const.UpperBorderTopPadding, Const.VerticalRightBorderWidth, Const.UpperBorderWidth );
			_rects.Draw( DrawType.BackAboveBorder, corner );
			_rects.Draw( DrawType.CornerRightTop, corner );
			// Lower corner
			corner = new Rectangle( spartRightVBorder.Left, part.Bottom - Const.StraightBorderWidth, Const.VerticalRightBorderWidth, Const.StraightBorderWidth );
			_rects.Draw( DrawType.BackBelowBorder, corner );
			_rects.Draw( DrawType.CornerLeftBottom, corner );
			_rects.Draw( DrawType.BackAboveBorder, new Rectangle( spartRightVBorder.Location, new Size( spartRightVBorder.Width, Const.UpperBorderTopPadding ) ) ); // A bit of background above the upper corner

			// Implement Spart: Rightmost
			_rects.Draw( DrawType.BackAboveBorder, new Rectangle( spartRightmost.Location, new Size( spartRightmost.Width, spartRightmost.Height - Const.StraightBorderWidth ) ) ); // Background above the border
			// Separators of this part
			foreach( int nPos in separators )
			{
				if( nPos >= spartRightmost.Left ) // Only those that belong to this spart
					_rects.Draw( DrawType.Separator, new Rectangle( nPos, _rects.WspButtonsBox.Top, Const.HorSpacingWhenSeparator, _rects.WspButtonsBox.Height ) );
			}
			_rects.Draw( DrawType.BorderHor, new Rectangle( spartRightmost.Left, spartRightmost.Bottom - Const.StraightBorderWidth, spartRightmost.Width, Const.StraightBorderWidth ) ); // Lower border
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the Workspace Bar control.
		/// </summary>
		internal WorkspaceButtonsManager WorkspaceButtonsManager
		{
			get { return _workspaceButtonsManager; }
		}

		/// <summary>
		/// Gets the Shortcut Bar control.
		/// </summary>
		internal ShortcutBar ShortcutBar
		{
			get { return _barShortcut; }
		}

		/// <summary>
		/// Gets or sets whether the workspace buttons are visible or not on the workspace buttons row.
		/// </summary>
		public bool WorkspaceButtonsVisible
		{
			get { return _bWorkspaceButtonsVisible; }
			set
			{
				if( _bWorkspaceButtonsVisible != value )
				{
					_bWorkspaceButtonsVisible = value;
					PerformLayout();
				}
			}
		}

		/// <summary>
		/// Gets or sets whether the shortcut bar is visible or not on the workspace buttons row.
		/// </summary>
		public bool ShortcutBarVisible
		{
			get { return _bShortcutBarVisible; }
			set
			{
				if( _bShortcutBarVisible != value )
				{
					_bShortcutBarVisible = value;
					PerformLayout();
				}
			}
		}

		#endregion

		#region Internal Event Handlers

		/// <summary>
		/// Either of the title properties has changed, recalc its width.
		/// </summary>
		private void OnTitleChanged( object sender, EventArgs e )
		{
			_nTitleWidth = JetLinkLabel.GetTextSize( this, Text, Font ).Width;
			PerformLayout();
			Invalidate();
		}

		#endregion

		#region Operations

		/// <summary>
		/// Creates and returns a disposable brush for painting a background in a space either above or below the border.
		/// </summary>
		/// <param name="sender">Control for which the brush is being requested.
		/// This may be needed for calculating the rect for gradient brushes.</param>
		/// <returns>Background brush.</returns>
		public Brush GetBackgroundBrush( Control sender )
		{
			bool bAboveBorder = true;

			// Check if the control represents a workspace button and if it's an active button
			// If yes, it's the only control that is painted below the border and should use a solid brush
			WorkspaceButton wb = sender as WorkspaceButton;
			if( (wb != null) && (wb.Active) )
				bAboveBorder = false;
			// All the other controls should use a gradient brush, with the application rect adjusted

			// Create the brush appropriate
			return GetBackgroundBrush( sender.RectangleToClient( RectangleToScreen( ClientRectangle ) ), bAboveBorder );
		}

		/// <summary>
		/// Saves or loads the resource tabs row settings.
		/// </summary>
		/// <param name="isStoring"><c>True</c> to save, <c>False</c> to load.</param>
		public void SerializeSettings( bool isStoring )
		{
			string section = "MainForm";
			string sDesiredToolbarWidth = "WorkspaceButtonsRow.DesiredToolbarWidth";
			string sWorkspaceButtonsVisible = "WorkspaceButtonsRow.WorkspaceButtons.Visible";
			string sShortcutBarVisible = "WorkspaceButtonsRow.ShortcutBar.Visible";

			if( isStoring )
			{
				Core.SettingStore.WriteInt( section, sDesiredToolbarWidth, _nDesiredToolbarWidth );
				Core.SettingStore.WriteBool( section, sWorkspaceButtonsVisible, WorkspaceButtonsVisible );
				Core.SettingStore.WriteBool( section, sShortcutBarVisible, ShortcutBarVisible );
			}
			else
			{
				_nDesiredToolbarWidth = Core.SettingStore.ReadInt( section, sDesiredToolbarWidth, _nDesiredToolbarWidth );
				WorkspaceButtonsVisible = Core.SettingStore.ReadBool( section, sWorkspaceButtonsVisible, WorkspaceButtonsVisible );
				ShortcutBarVisible = Core.SettingStore.ReadBool( section, sShortcutBarVisible, ShortcutBarVisible );
			}
            Visible = WorkspaceButtonsVisible;
		}

		#endregion
	}
}
