/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using JetBrains.Interop.WinApi;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A bar of tabs.
	/// </summary>
	public class TabBar: Control, ICommandBar
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private class TabBarTab
        {
            private readonly string _text;
            private readonly object _tag;

            public TabBarTab( string text, object tag )
            {
                _text = text;
                _tag = tag;
            }

            public object Tag
            {
                get { return _tag; }
            }

            public string Text
            {
                get { return _text; }
            }

            public int Width { get; set; }

            public int PreferredWidth { get; set; }
        }

        private readonly ArrayList _tabs = new ArrayList();
        private int _activeTabIndex = -1;
        private IntPtr _fontHandle;
        private ColorScheme _colorScheme;

		/// <summary>
		/// Scaling factor for the component.
		/// </summary>
		protected SizeF _sizeScale = new SizeF(1, 1);

		/// <summary>
		/// Command bar site.
		/// </summary>
		private ICommandBarSite _site;

		public event EventHandler SelectedIndexChanged;

		public TabBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			SetStyle( ControlStyles.AllPaintingInWmPaint
				| ControlStyles.CacheText
				| ControlStyles.ResizeRedraw
				| ControlStyles.UserPaint
				| ControlStyles.Opaque
				| ControlStyles.DoubleBuffer
                
			          , true );
			SetStyle( ControlStyles.Selectable
				| ControlStyles.ContainerControl
				| ControlStyles.SupportsTransparentBackColor
			          , false );
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
            Win32Declarations.DeleteObject( _fontHandle );
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

	    [DefaultValue(null)]
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

	    private void CreateFontHandle()
        {
            if ( _fontHandle != IntPtr.Zero )
            {
                Win32Declarations.DeleteObject( _fontHandle );
                _fontHandle = IntPtr.Zero;
            }

            int logPixY;
            using( Graphics g = CreateGraphics() )
            {
                IntPtr hdc = g.GetHdc();
                logPixY = Win32Declarations.GetDeviceCaps( hdc, Win32Declarations.LOGPIXELSY );
                g.ReleaseHdc( hdc );
            }
        
            _fontHandle = Win32Declarations.CreateFont( (int) (-Font.SizeInPoints * logPixY / 72),
                0, 0, 0, Win32Declarations.FW_NORMAL, 0, 0, 0, 0, 0, 0, 0, 0, Font.Name );
        }

        public void AddTab( string text, object tag )
        {
            InsertTab( _tabs.Count, text, tag );
        }

        public void InsertTab( int index, string text, object tag )
        {
            TabBarTab newTab = new TabBarTab( text, tag );
            _tabs.Insert( index, newTab );
            if ( _tabs.Count == 1 )
            {
                _activeTabIndex = 0;
            }
            else if ( _activeTabIndex >= index )
            {
                _activeTabIndex++;
            }
            CalcPreferredWidth( newTab );
            newTab.Width = newTab.PreferredWidth;
            
			// Request relayouting
			if(_site != null)	// Needed?
				_site.PerformLayout( this );

			Invalidate();
        }

        protected override void OnHandleCreated( EventArgs e )
        {
            base.OnHandleCreated( e );
            CreateFontHandle();
            UpdatePreferredWidth();
            UpdateTabWidth();
        }

	    private void UpdatePreferredWidth()
	    {
	        foreach( TabBarTab tab in _tabs )
	        {
	            CalcPreferredWidth( tab );
	        }
	    }

		/// <summary><seealso cref="OptimalWidth"/>
		/// Calculates the preferred width on a per-tab basis.
		/// </summary>
	    private void CalcPreferredWidth( TabBarTab tab )
		{
			IntPtr hdc = (IntPtr) 0;
			try
			{
				if( IsHandleCreated )
					hdc = Win32Declarations.GetDC( Handle );
				else
					hdc = Win32Declarations.GetDC( (IntPtr) 0 );

				IntPtr oldFont = Win32Declarations.SelectObject( hdc, _fontHandle );
				Win32Declarations.SelectObject( hdc, oldFont );
				SIZE sz = new SIZE();
				Win32Declarations.GetTextExtentPoint32( hdc, tab.Text, tab.Text.Length, ref sz );

				int prefWidth = sz.cx + 2 * 8;
				tab.PreferredWidth = Math.Max( 50, prefWidth );
				tab.Width = tab.PreferredWidth;
			}
			finally
			{
				if( hdc != (IntPtr) 0 )
				{
					Win32Declarations.ReleaseDC( Handle, hdc );
					hdc = (IntPtr) 0;
				}
			}
		}

		public int TabCount
        {
            get { return _tabs.Count; }
        }

        public int SelectedIndex
        {
            get
            {
                return _activeTabIndex;
            }
            set
            {
                if ( _activeTabIndex != value )
                {
                    if ( value < 0 || value >= _tabs.Count )
                    {
                        throw new ArgumentOutOfRangeException( "value", value, "Tab index out of range" );
                    }
                    InvalidateTab( _activeTabIndex );
                    _activeTabIndex = value;
                    InvalidateTab( _activeTabIndex );
                    if ( SelectedIndexChanged != null )
                    {
                        SelectedIndexChanged( this, EventArgs.Empty );
                    }

					// Request relayouting
					if(_site != null)	// Needed?
						_site.PerformLayout( this );
                }
            }
        }

        public object SelectedTabTag
        {
            get
            {
                if ( _activeTabIndex < 0 )
                    return null;
                return ((TabBarTab) _tabs [_activeTabIndex]).Tag;
            }
        }

        public object GetTabTag( int tabIndex )
        {
            return ((TabBarTab) _tabs [tabIndex]).Tag;
        }

        public string GetTabText( int tabIndex )
        {
            return ((TabBarTab) _tabs [tabIndex]).Text;
        }

        protected override void OnFontChanged( EventArgs e )
        {
            base.OnFontChanged( e );
            if ( IsHandleCreated )
            {
                CreateFontHandle();
                UpdatePreferredWidth();
            }
        }

        protected override void OnPaint( PaintEventArgs e )
        {
        	e.Graphics.FillRectangle( SystemBrushes.Control, ClientRectangle );

        	for( int i = 0; i < _tabs.Count; i++ )
        	{
        		Rectangle rc = GetTabRect( i );
        		if( e.ClipRectangle.IntersectsWith( rc ) )
        		{
        			DrawTab( e.Graphics, i );
        		}
        	}
        }

		private void DrawTab( Graphics g, int index )
        {
            Rectangle rc = GetTabRect( index );
            if ( rc.IsEmpty )
                return;

            GraphicsPath gp = BuildBorderPath( rc );
            using( gp )
            {
                if ( index == _activeTabIndex )
                {
                    g.FillPath( GUIControls.ColorScheme.GetBrush( _colorScheme, "ResourceTypeTabs.ActiveBackground",
                        rc, SystemBrushes.Control ), gp );
                }
                else
                {
                    g.FillPath( GUIControls.ColorScheme.GetBrush( _colorScheme, "ResourceTypeTabs.InactiveBackground",
                        rc, SystemBrushes.Control ), gp );
                    
                    Rectangle rcGradient = new Rectangle( rc.Left, rc.Bottom-6, rc.Width, 6 );
                    g.FillRectangle( GUIControls.ColorScheme.GetBrush( _colorScheme, "ResourceTypeTabs.InactiveBackgroundBottom",
                        rcGradient, SystemBrushes.Control ), rcGradient );
                }
            
                g.DrawPath( GUIControls.ColorScheme.GetPen( _colorScheme, 
                    (index == _activeTabIndex) ? "PaneCaption.Border" : "ResourceTypeTabs.Border", 
                    Pens.Black ), gp );
            }

            string tabText = ((TabBarTab) _tabs [index]).Text;
            IntPtr hdc = g.GetHdc();
            try
            {
                Color clrText = (index == _activeTabIndex)
                    ? GUIControls.ColorScheme.GetColor( _colorScheme, "ResourceTypeTabs.ActiveText", Color.Black )
                    : GUIControls.ColorScheme.GetColor( _colorScheme, "ResourceTypeTabs.InactiveText", Color.Black );
                int oldColor = Win32Declarations.SetTextColor( hdc, ColorTranslator.ToWin32( clrText ) );
                IntPtr oldFont = Win32Declarations.SelectObject( hdc, _fontHandle );
                BackgroundMode oldBkMode = Win32Declarations.SetBkMode( hdc, BackgroundMode.TRANSPARENT );
                
                RECT rect = Win32Declarations.RectangleToRECT( rc );
                Win32Declarations.DrawText( hdc, tabText, tabText.Length, ref rect,
                    DrawTextFormatFlags.DT_CENTER | DrawTextFormatFlags.DT_VCENTER | DrawTextFormatFlags.DT_NOPREFIX | 
                    DrawTextFormatFlags.DT_END_ELLIPSIS | DrawTextFormatFlags.DT_SINGLELINE );
                
                Win32Declarations.SetBkMode( hdc, oldBkMode );
                Win32Declarations.SelectObject( hdc, oldFont );
                Win32Declarations.SetTextColor( hdc, oldColor );
            }
            finally
            {
                g.ReleaseHdc( hdc );
            }
        }

        public Rectangle GetTabRect( int index )
        {
            int left = 0;
            for( int i=0; i<index; i++ )
            {
                left += ((TabBarTab) _tabs [i]).Width + 1;
            }
            return new Rectangle( left, ClientRectangle.Top,
                ((TabBarTab) _tabs [index]).Width, ClientRectangle.Height );
        }

        private static GraphicsPath BuildBorderPath( Rectangle rc )
        {
            const int radius = 5;
            GraphicsPath gp = new GraphicsPath();
            int right = rc.Right-1;
            int bottom = rc.Bottom-1;
            gp.AddLine( rc.Left, bottom, rc.Left, bottom - radius );
            gp.AddArc( rc.Left, rc.Top, radius, radius, 180, 90 );
            gp.AddLine( rc.Left + radius, rc.Top, right - radius, rc.Top );
            gp.AddArc( right - radius, rc.Top, radius, radius, 270, 90 );
            gp.AddLine( right, rc.Top + radius, right, bottom );
            gp.CloseFigure();
            return gp;
        }

        private void InvalidateTab( int tabIndex )
        {
            if ( tabIndex >= 0 )
            {
                Invalidate( GetTabRect( tabIndex ) );
            }
        }

        protected override void OnMouseDown( MouseEventArgs e )
        {
            base.OnMouseDown( e );
            if ( e.Button == MouseButtons.Left )
            {
                int tabIndex = GetTabAt( e.X, e.Y );
                if ( tabIndex >= 0 )
                {
                    SelectedIndex = tabIndex;
                }
            }
        }

        private int GetTabAt( int x, int y )
        {
            for( int i=0; i<_tabs.Count; i++ )
            {
                Rectangle rc = GetTabRect( i );
                if ( rc.Contains( x, y ) )
                {
                    return i;
                }
            }
            return -1;
        }

		/// <summary>
		/// When the size of the tab control changes, reduces the tab width if necessary.
		/// </summary>
        protected override void OnLayout( LayoutEventArgs levent )
        {
            base.OnLayout( levent );
            if ( levent.AffectedControl != null && levent.AffectedProperty != null )
            {
                if ( levent.AffectedProperty.ToString() == "Bounds" )
                {
                    Form frm = FindForm();
                    if ( frm != null && frm.WindowState != FormWindowState.Minimized )
                    {
                        UpdateTabWidth();
                    }
                }
            }
        }

	    private void UpdateTabWidth()
	    {
            int availWidth = ClientSize.Width - (_tabs.Count + 1) * 2;   // leave space for tab borders
            int totalWidth = OptimalWidth;
	    	if ( totalWidth == 0 )
                return;

            double availRatio = (double) availWidth / (double) totalWidth;
            //if ( availRatio > 1.0 )
            //    availRatio = 1.0;
            bool needInvalidate = false;
            // if we don't have enough space, shrink all tabs proportionally
            foreach( TabBarTab tab in _tabs )
            {
                int tabWidth = (int) (tab.PreferredWidth * availRatio );
                if ( tabWidth != tab.Width )
                {
                    tab.Width = tabWidth;
                    needInvalidate = true;
                }
            }
            if ( needInvalidate )
            {
                Invalidate();
            }
	    }

		/// <summary>
		/// Scales the control.
		/// </summary>
		protected override void ScaleCore(float dx, float dy)
		{
			_sizeScale = new SizeF(dx, dy);
			base.ScaleCore (dx, dy);
		}

		/// <summary><seealso cref="CalcPreferredWidth"/>
		/// Optimal width of the whole control the tabs would like to occupy.
		/// </summary>
		private int OptimalWidth
		{
			get
			{
				int totalWidth = 0;
				foreach( TabBarTab tab in _tabs )
					totalWidth += tab.PreferredWidth;
				return totalWidth;
			}
		}

		#region ICommandBar Interface Members

		public void SetSite( ICommandBarSite site )
		{
			_site = site;
			//if(_site != null)
			//	_site.PerformLayout(this);
			// TODO: ???
		}

		public Size MinSize
		{
			get { return new Size(100, (int) (15 * _sizeScale.Height)); }
		}

		public Size MaxSize
		{
			get { return new Size(OptimalWidth, int.MaxValue); }
		}

		public Size OptimalSize
		{
			get { return new Size(OptimalWidth, (int) (27 * _sizeScale.Height)); }
		}

		public Size Integral
		{
			get { return new Size(1, 1); }
		}

		#endregion
	}
}
