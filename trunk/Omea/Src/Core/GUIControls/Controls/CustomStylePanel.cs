/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using JetBrains.Interop.WinApi;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.GUIControls
{
	public class CustomStylePanel : Panel
	{
		private System.ComponentModel.Container components = null;
        private bool _resizeRedraw = true;
        private bool _doubleBuffer = false;
        private Color _borderColor = Color.Black;

		public CustomStylePanel() : base()
		{
            SetStyle( ControlStyles.ResizeRedraw, true );
            SetStyle( ControlStyles.Opaque, false );
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
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

	    [DefaultValue(true)]
        new public bool ResizeRedraw
	    {
	        get { return _resizeRedraw; }
	        set
	        {
	            if ( _resizeRedraw != value )
	            {
	                _resizeRedraw = value;
                    SetStyle( ControlStyles.ResizeRedraw, value );
	            }
	        }
	    }
	    
        
        [DefaultValue(false)]
        public bool DoubleBuffer
	    {
	        get { return _doubleBuffer; }
	        set
	        {
	            if ( _doubleBuffer != value )
	            {
	                _doubleBuffer = value;
                    SetStyle( ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, _doubleBuffer );
	            }
	        }
	    }

	    public Color BorderColor
	    {
	        get { return _borderColor; }
	        set
	        {
	            if ( _borderColor != value )
	            {
                    _borderColor = value;
                    Invalidate();
	            }
	        }
	    }

	    protected override void WndProc( ref Message m )
        {
            if ( m.Msg == Win32Declarations.WM_NCPAINT && BorderStyle == BorderStyle.FixedSingle )
            {
                IntPtr hdc = Win32Declarations.GetWindowDC( Handle );
                IntPtr brush = Win32Declarations.CreateSolidBrush( Win32Declarations.ColorToRGB( _borderColor ) );
                IntPtr oldBrush = Win32Declarations.SelectObject( hdc, brush );
                RECT rect = new RECT( 0, 0, Width, Height );
                Win32Declarations.FrameRect( hdc, ref rect, brush );
                Win32Declarations.SelectObject( hdc, oldBrush );
                Win32Declarations.DeleteObject( brush );
                Win32Declarations.ReleaseDC( Handle, hdc );
                m.Result = IntPtr.Zero;
            }
            else
            {
                base.WndProc( ref m );
            }
        }

	}
}
