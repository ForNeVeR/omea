// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A toolbar with a gradient background.
	/// </summary>
	public class GradientToolbar : ToolBar
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private Brush _backBrush;
        private int _buttonIndent = 0;
        private Color _gradientStartColor = Color.White;
        private Color _gradientEndColor = SystemColors.ControlDark;

        public event PaintEventHandler PaintBackground;

		public GradientToolbar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			InitBackBrush();
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
                _backBrush.Dispose();
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

	    public Color GradientStartColor
	    {
	        get { return _gradientStartColor; }
	        set
            {
                if ( _gradientStartColor != value )
                {
                    _gradientStartColor = value;
                    InitBackBrush();
                    Invalidate();
                }
            }
	    }

	    public Color GradientEndColor
	    {
	        get { return _gradientEndColor; }
	        set
            {
                if ( _gradientEndColor != value )
                {
                    _gradientEndColor = value;
                    InitBackBrush();
                    Invalidate();
                }
            }
	    }


	    private void InitBackBrush()
        {
            if ( _backBrush != null )
            {
                _backBrush.Dispose();
            }
            if ( _gradientStartColor == _gradientEndColor )
            {
                _backBrush = new SolidBrush( _gradientStartColor );
            }
            else
            {
                _backBrush = new LinearGradientBrush( ClientRectangle,
                    _gradientStartColor, _gradientEndColor, LinearGradientMode.Vertical );

                Blend blend = new Blend();
                blend.Positions = new float[] { 0.0f, 0.7f, 1.0f };
                blend.Factors = new float[] { 0.0f, 0.5f, 1.0f };
                ((LinearGradientBrush) _backBrush).Blend = blend;
            }
        }

	    [DefaultValue(0)]
        public int ButtonIndent
	    {
	        get { return _buttonIndent; }
	        set
            {
                if ( _buttonIndent != value )
                {
                    _buttonIndent = value;
                    if ( IsHandleCreated )
                    {
                        Win32Declarations.SendMessage( Handle, Win32Declarations.TB_SETINDENT,
                            (IntPtr) _buttonIndent, IntPtr.Zero );
                    }
                }
            }
	    }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated( e );
            Win32Declarations.SendMessage( Handle, Win32Declarations.TB_SETINDENT,
                (IntPtr) _buttonIndent, IntPtr.Zero );
        }

	    protected override void OnSizeChanged( EventArgs e )
        {
            base.OnSizeChanged( e );
            if ( ClientRectangle.Width > 0 && ClientRectangle.Height > 0 )
            {
                InitBackBrush();
            }
        }

        protected override void WndProc( ref Message m )
        {
            base.WndProc( ref m );
            try
            {
                if ( m.Msg == Win32Declarations.OCM_NOTIFY )
                {
                    NMHDR hdr = (NMHDR) Marshal.PtrToStructure( m.LParam, typeof(NMHDR) );
                    if ( hdr.hwndFrom == Handle )
                    {
                        if ( hdr.code == Win32Declarations.NM_CUSTOMDRAW )
                        {
                            NMTBCUSTOMDRAW customDraw = (NMTBCUSTOMDRAW) Marshal.PtrToStructure( m.LParam, typeof(NMTBCUSTOMDRAW) );
                            OnCustomDraw( ref customDraw, ref m );
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                Core.ReportBackgroundException( ex );
            }
        }

	    private void OnCustomDraw( ref NMTBCUSTOMDRAW customDraw, ref Message m )
	    {
            if ( customDraw.hdr.dwDrawStage == Win32Declarations.CDDS_PREPAINT )
            {
                using( Graphics g = Graphics.FromHdc( customDraw.hdr.hdc ) )
                {
                    Rectangle rect = Win32Declarations.RECTToRectangle( customDraw.hdr.rc );
                    g.FillRectangle( _backBrush, rect );
                    if ( PaintBackground != null )
                    {
                        PaintBackground( this, new PaintEventArgs( g, rect ) );
                    }
                }
                m.Result = (IntPtr) Win32Declarations.CDRF_DODEFAULT;
            }
	    }
	}
}
