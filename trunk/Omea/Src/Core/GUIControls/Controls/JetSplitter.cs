/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Splitter with a gradient background and collapsing support.
	/// </summary>
	public class JetSplitter: Splitter
	{
        private bool _collapsed = false;
        private Control _controlToCollapse;
        private bool _isVertical;
        private int _splitterSize;
        private int _splitterCenterSize;
        private int _splitterCenter;   // in "long" direction
        private int _splitterMiddle;   // in "short" direction
        private int _splitterArrowSize;
        private bool _fillGradient = true;
        private bool _fillCenterRect = true;
        private Rectangle _splitterCenterRect;
        private ColorScheme _colorScheme;

        public event PaintEventHandler PaintSplitterBackground;
        public event EventHandler CollapsedChanged;

	    public JetSplitter()
            : base()
	    {
            SetStyle( ControlStyles.ResizeRedraw, true );
	    }

	    protected override void Dispose( bool disposing )
	    {
            if ( disposing )
            {
            }
	        base.Dispose( disposing );
	    }

	    [DefaultValue(null)]
        public Control ControlToCollapse
	    {
	        get { return _controlToCollapse; }
	        set { _controlToCollapse = value; }
	    }

	    [DefaultValue(true)]
        public bool FillGradient
	    {
	        get { return _fillGradient; }
	        set { _fillGradient = value; }
	    }

	    [DefaultValue(true)]
        public bool FillCenterRect
	    {
	        get { return _fillCenterRect; }
	        set { _fillCenterRect = value; }
	    }

	    [DefaultValue(false)]
        public bool Collapsed
        {
            get { return _collapsed; }
            set
            {
                if ( _collapsed != value )
                {
                    _collapsed = value;
                    if ( _controlToCollapse != null )
                    {
                        _controlToCollapse.Visible = !_collapsed;
                    }
                    if ( CollapsedChanged != null )
                    {
                        CollapsedChanged( this, EventArgs.Empty );
                    }
                }
            }
        }

        public ColorScheme ColorScheme
        {
            get { return _colorScheme; }
            set
            {
                if ( _colorScheme != value )
                {
                    _colorScheme = value;
                    Invalidate();
                }
            }
        }

	    protected override void OnSizeChanged( EventArgs e )
	    {
	        base.OnSizeChanged( e );
            UpdateLayout();
	    }

	    protected override void OnLocationChanged( EventArgs e )
	    {
	        base.OnLocationChanged( e );
            UpdateLayout();
	    }

	    private void UpdateLayout()
	    {
	        _isVertical = ( Dock == DockStyle.Left || Dock == DockStyle.Right );
    
	        _splitterSize = _isVertical ? ClientSize.Height : ClientSize.Width;
	        _splitterCenterSize = Math.Min( 120, (int) (_splitterSize * 0.7) );
    
	        if ( _isVertical )
	        {
	            _splitterCenterRect = new Rectangle( ClientRectangle.Left,
	                                                 ClientRectangle.Top + (ClientRectangle.Height - _splitterCenterSize) / 2,
	                                                 ClientRectangle.Width,
	                                                 _splitterCenterSize );
	            _splitterCenter = ( ClientRectangle.Top + ClientRectangle.Bottom ) / 2;
                _splitterMiddle = ( ClientRectangle.Left + ClientRectangle.Right ) / 2 - 1;
                _splitterArrowSize = ClientRectangle.Width;
	        }
	        else
	        {
	            _splitterCenterRect = new Rectangle( 
	                ClientRectangle.Left + (ClientRectangle.Width - _splitterCenterSize) / 2,
	                ClientRectangle.Top,
	                _splitterCenterSize,
	                ClientRectangle.Height );
	            _splitterCenter = ( ClientRectangle.Left + ClientRectangle.Right ) / 2;
                _splitterMiddle = ( ClientRectangle.Top + ClientRectangle.Bottom ) / 2 - 1;
	            _splitterArrowSize = ClientRectangle.Height;
	        }
	    }


	    protected override void OnPaint( PaintEventArgs e )
	    {
	        base.OnPaint( e );

            if ( ClientRectangle.Width == 0 || ClientRectangle.Height == 0 )
                return;

            if ( PaintSplitterBackground != null )
            {
                PaintSplitterBackground( this, e );
            }

            if ( _fillGradient )
            {
                Brush backBrush = new LinearGradientBrush( ClientRectangle, 
                    SystemColors.ControlLightLight, SystemColors.ControlDark,
                    _isVertical ? LinearGradientMode.Horizontal : LinearGradientMode.Vertical );
                using( backBrush )
                {
                    e.Graphics.FillRectangle( backBrush, ClientRectangle );
                }
            }

            Rectangle rcSplitterCenter = _splitterCenterRect;

            if ( _fillCenterRect )
            {
                ColorScheme.DrawRectangle( e.Graphics, _colorScheme, "Splitter.CenterBorder", 
                    new Rectangle( rcSplitterCenter.Left, rcSplitterCenter.Top, 
                    rcSplitterCenter.Width-1, rcSplitterCenter.Height-1 ),
                    SystemPens.Control );
            }

            if ( _isVertical )
            {
                rcSplitterCenter.Inflate( 0, -1 );
            }
            else
            {
                rcSplitterCenter.Inflate( -1, 0 );
            }

            if ( _fillCenterRect && rcSplitterCenter.Width > 0 && rcSplitterCenter.Height > 0 )
            {
                ColorScheme.FillRectangle( e.Graphics, _colorScheme, 
                    _isVertical ? "Splitter.CenterVert" : "Splitter.CenterHorz",
                    rcSplitterCenter, SystemBrushes.Control );
            }

            if ( _controlToCollapse != null )
            {
                for( int coord=_splitterCenter - _splitterCenterSize/2 + 10; coord < _splitterCenter - 10; coord += 3 )
                {
                    DrawCenterDot( e.Graphics, coord );
                }

                for( int coord=_splitterCenter + 10; coord < _splitterCenter + _splitterCenterSize/2 - 10; coord += 3 )
                {
                    DrawCenterDot( e.Graphics, coord );
                }

                Brush arrowBrush = ColorScheme.GetBrush( _colorScheme, "Splitter.Arrow", rcSplitterCenter,
                    Brushes.Black );

                if ( Dock == DockStyle.Top && _controlToCollapse.Visible )
                {
                    e.Graphics.FillPolygon( arrowBrush, 
                        new Point[] { new Point( _splitterCenter -_splitterArrowSize, ClientRectangle.Bottom-1 ),
                                        new Point( _splitterCenter + _splitterArrowSize + 1, ClientRectangle.Bottom-1 ),
                                        new Point( _splitterCenter, ClientRectangle.Top-1 ) } );
                }
                if ( Dock == DockStyle.Top && !_controlToCollapse.Visible )
                {
                    e.Graphics.FillPolygon( arrowBrush, 
                        new Point[] { new Point( _splitterCenter - _splitterArrowSize, ClientRectangle.Top ),
                                        new Point( _splitterCenter + _splitterArrowSize + 1, ClientRectangle.Top ),
                                        new Point( _splitterCenter, ClientRectangle.Bottom ) } );
                }
                if ( ( Dock == DockStyle.Left && _controlToCollapse.Visible ) ||
                        ( Dock == DockStyle.Right && !_controlToCollapse.Visible ) )
                {
                    e.Graphics.FillPolygon( arrowBrush, 
                        new Point[] { new Point( ClientRectangle.Right-1, _splitterCenter -_splitterArrowSize ),
                                        new Point( ClientRectangle.Right-1, _splitterCenter + _splitterArrowSize + 1 ),
                                        new Point( ClientRectangle.Left-1, _splitterCenter ) } );
                }
                if ( ( Dock == DockStyle.Right && _controlToCollapse.Visible ) ||
                        ( Dock == DockStyle.Left && !_controlToCollapse.Visible ) )
                {
                    e.Graphics.FillPolygon( arrowBrush, 
                        new Point[] { new Point( ClientRectangle.Left, _splitterCenter -_splitterArrowSize ),
                                        new Point( ClientRectangle.Left, _splitterCenter + _splitterArrowSize + 1 ),
                                        new Point( ClientRectangle.Right, _splitterCenter ) } );
                }
            }
            else
            {
                for( int coord=_splitterCenter - _splitterCenterSize/2 + 10; 
                     coord < _splitterCenter + _splitterCenterSize/2 - 10; 
                     coord += 3 )
                {
                    DrawCenterDot( e.Graphics, coord );
                }
            }
        }

        private void DrawCenterDot( Graphics g, int coord )
        {
            Rectangle rcDot = _isVertical
                ? new Rectangle( _splitterMiddle, coord, 1, 1 ) 
                : new Rectangle( coord, _splitterMiddle, 1, 1 );

            ColorScheme.DrawRectangle( g, _colorScheme, "Splitter.Dot", rcDot, Pens.Black );
        }

        protected override void OnMouseDown( MouseEventArgs e )
        {
            // prevent default resize if we're collapsed and the user clicked outside the collapse rect
            if ( Collapsed )
            {
                return;
            }
            base.OnMouseDown (e);
        }

	    protected override void OnMouseMove( MouseEventArgs e )
	    {
	        base.OnMouseMove( e );
            if ( _controlToCollapse != null )
            {
                if ( _splitterCenterRect.Contains( e.X, e.Y ) )
                {
                    Cursor = Cursors.Hand;
                }
                else
                {
                    if ( _controlToCollapse.Visible )
                    {
                        Cursor = _isVertical ? Cursors.VSplit : Cursors.HSplit;
                    }
                    else
                    {
                        Cursor = Cursors.Default;
                    }
                }
            }
	    }

        protected override void OnMouseUp( MouseEventArgs e )
        {
            base.OnMouseUp( e );
            if ( e.Button == MouseButtons.Left && _splitterCenterRect.Contains( e.X, e.Y ) )
            {
                if ( _controlToCollapse != null )
                {
                    Collapsed = !Collapsed;
                    Invalidate();
                }
            }
        }
	}
}
