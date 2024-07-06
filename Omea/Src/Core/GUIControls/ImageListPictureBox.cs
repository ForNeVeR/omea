// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// A PictureBox which displays an image or a series of overlaid images from an imagelist.
    /// </summary>
    public class ImageListPictureBox : System.Windows.Forms.Control
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private ImageList _imageList;
        private int[] _imageIndices = new int[] {};
        private Point _imageLeftTopPoint = new Point( 0, 0 );

		public ImageListPictureBox()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			SetStyle( ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true );
            SetStyle( ControlStyles.Selectable, false );
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
					components.Dispose();
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

        /// <summary>
        /// The image list from which the images to draw are taken.
        /// </summary>
        [DefaultValue(null)]
        public ImageList ImageList
        {
            get { return _imageList; }
            set { _imageList = value; }
        }

        /// <summary>
        /// Gets or sets the index of the image to draw in the picture box.
        /// </summary>
        /// <remarks>If multiple images are drawn, the property returns the index of the first image.</remarks>
        public int ImageIndex
        {
            get
            {
                if ( _imageIndices.Length == 0 )
                {
                    return -1;
                }
                return _imageIndices [0];
            }
            set
            {
                if ( _imageIndices.Length != 1 || _imageIndices [0] != value )
                {
                    _imageIndices = new int[] { value };
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the indices of the images to draw in the picture box.
        /// </summary>
        public int[] ImageIndices
        {
            get { return _imageIndices; }
            set
            {
                if ( value == null )
                {
                    _imageIndices = new int[] {};
                }
                else
                {
                    _imageIndices = value;
                }
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the offset of the image from the top left corner of the control.
        /// </summary>
        public Point ImageLeftTopPoint
        {
            get { return _imageLeftTopPoint; }
            set { _imageLeftTopPoint = value; }
        }

		protected override void OnPaint( PaintEventArgs pe )
		{
            if ( _imageList != null )
            {
                for( int i=0; i<_imageIndices.Length; i++ )
                {
                    int index = _imageIndices [i];
                    if ( index >= 0 && index < _imageList.Images.Count )
                    {
                        _imageList.Draw( pe.Graphics, _imageLeftTopPoint.X, _imageLeftTopPoint.Y, index );
                    }
                }
            }
		}
	}
}
