/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A bar which is entirely filled by a gradient fill.
	/// </summary>
	public class GradientBar : UserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components;

        private Color _startColor = Color.Black;
        private Color _endColor = Color.White;
        private LinearGradientMode _gradientMode = LinearGradientMode.Horizontal;

		public GradientBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			SetStyle( ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, 
                true );
            SetStyle( ControlStyles.Selectable, false );
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


	    public Color StartColor
	    {
	        get { return _startColor; }
	        set 
            { 
                if ( _startColor != value )
                {
                    _startColor = value; 
                    Invalidate();                    
                }
            }
	    }


	    public Color EndColor
	    {
	        get { return _endColor; }
	        set 
            { 
                if ( _endColor != value )
                {
                    _endColor = value; 
                    Invalidate();
                }
            }
	    }

	    public LinearGradientMode GradientMode
	    {
	        get { return _gradientMode; }
	        set 
            { 
                if ( _gradientMode != value )
                {
                    _gradientMode = value; 
                    Invalidate();
                }
            }
	    }

	    protected override void OnPaint( PaintEventArgs e )
        {
            base.OnPaint( e );
            if ( ClientRectangle.Width > 0 && ClientRectangle.Height > 0 )
            {
                using( LinearGradientBrush b = new LinearGradientBrush( ClientRectangle,
                           _startColor, _endColor, _gradientMode ) )
                {
                    e.Graphics.FillRectangle( b, ClientRectangle );
                }
            }
        }
	}
}
