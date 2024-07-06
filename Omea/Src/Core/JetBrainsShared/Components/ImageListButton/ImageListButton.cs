// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.UI.Components.ImageListButton
{
    /// <summary>
    /// A simple button which draws one of four images from the image list depending on its
    /// state.
    /// </summary>
    public class ImageListButton : UserControl
    {
        private ImageList _imageList;
        private int _normalImageIndex = -1;
        private int _hotImageIndex = -1;
        private int _disabledImageIndex = -1;
        private int _pressedImageIndex = -1;
        private bool _pressed = false;
        private bool _hot = false;

		/// <summary>
		/// Tooltip of this button.
		/// </summary>
		protected ToolTip _tooltip;

        public ImageListButton()
        {
        	SetStyle( ControlStyles.UserPaint
        		| ControlStyles.SupportsTransparentBackColor
        		| ControlStyles.AllPaintingInWmPaint
        		| ControlStyles.CacheText
        		| ControlStyles.UserPaint
        		| ControlStyles.FixedHeight
        		| ControlStyles.FixedWidth,
        	          true );
        	SetStyle( ControlStyles.StandardClick
        		| ControlStyles.Selectable
        		| ControlStyles.ResizeRedraw,
        	          false );

			_tooltip = new ToolTip();
        }

    	[DefaultValue( null )]
        public ImageList ImageList
        {
            get { return _imageList; }
            set
            {
                if (_imageList != value)
                {
                    _imageList = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue( -1 )]
        public int NormalImageIndex
        {
            get { return _normalImageIndex; }
            set
            {
                if (_normalImageIndex != value)
                {
                    _normalImageIndex = value;
                    Invalidate();
                }
            }
        }

        [DefaultValue( -1 )]
        public int HotImageIndex
        {
            get { return _hotImageIndex; }
            set { _hotImageIndex = value; }
        }

        [DefaultValue( -1 )]
        public int DisabledImageIndex
        {
            get { return _disabledImageIndex; }
            set
            {
                if (_disabledImageIndex != value)
                {
                    _disabledImageIndex = value;
                    if (!Enabled)
                        Invalidate();
                }
            }
        }

        public int PressedImageIndex
        {
            get { return _pressedImageIndex; }
            set { _pressedImageIndex = value; }
        }

        protected override void OnEnabledChanged( EventArgs e )
        {
            base.OnEnabledChanged( e );
            Invalidate();
        }

        protected override void OnPaint( PaintEventArgs e )
        {
			// Background
			if(BackColor != Color.Transparent)
			{
				// If the parent is capable of providing a background brush, use it
				IBackgroundBrushProvider	bbp = Parent as IBackgroundBrushProvider;
				Brush brush = bbp != null ? bbp.GetBackgroundBrush(this) : new SolidBrush(BackColor);
				using(brush)
					e.Graphics.FillRectangle( brush, ClientRectangle );
			}

			// Foreground (icon)
            int imageIndex = GetCurrentImageIndex();
            if (_imageList != null && imageIndex >= 0 && imageIndex < _imageList.Images.Count)
                _imageList.Draw( e.Graphics, 0, 0, imageIndex );
        }

        private int GetCurrentImageIndex()
        {
            if (!Enabled && _disabledImageIndex >= 0)
                return _disabledImageIndex;
            if (_pressed && _pressedImageIndex >= 0)
                return _pressedImageIndex;
            if (_hot && _hotImageIndex >= 0)
                return _hotImageIndex;
            return _normalImageIndex;
        }

        protected override void OnMouseDown( MouseEventArgs e )
        {
            base.OnMouseDown( e );
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp( MouseEventArgs e )
        {
            base.OnMouseUp( e );
            if (e.Button == MouseButtons.Left)
            {
                bool wasPressed = _pressed;
                _pressed = false;
                Invalidate();
                if (wasPressed)
                    OnClick( EventArgs.Empty );
            }
        }

        protected override void OnMouseMove( MouseEventArgs e )
        {
            base.OnMouseMove( e );
            if (e.Button == MouseButtons.Left)
            {
                bool newPressed = (ClientRectangle.Contains( e.X, e.Y ));
                if (_pressed != newPressed)
                {
                    _pressed = newPressed;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseEnter( EventArgs e )
        {
            base.OnMouseEnter( e );
            _hot = true;
            if (_hotImageIndex >= 0 && Enabled)
                Invalidate();
        }

        protected override void OnMouseLeave( EventArgs e )
        {
            base.OnMouseLeave( e );
            _hot = false;
            if (_hotImageIndex >= 0 && Enabled)
                Invalidate();
        }

		/// <summary>
		/// Tooltip text of this control. An empty string to cancel the tooltip.
		/// </summary>
		[DefaultValue("")]
		public string ToolTip
		{
			get
			{
				return _tooltip.GetToolTip( this );
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();
				_tooltip.SetToolTip( this, value );
			}
		}

		/// <summary>
		/// Adds an icon to the buttons image list and assigns it to a specific state of the button.
		/// </summary>
		public void AddIcon(Icon icon, ButtonState state)
		{
			// Create & init the image list using the first image's properties, if there's no imagelist currently
			if(_imageList == null)
			{
				_imageList = new ImageList();
				_imageList.ImageSize = icon.Size;
				_imageList.ColorDepth = ColorDepth.Depth32Bit;
				Size = icon.Size;	// Resize the button to fit the icons
			}

			// Add the new image and store its index
			int	nIndex = _imageList.Images.Count;
            _imageList.Images.Add(icon);

			// Assign to the state appropriate (this also invalidates)
			switch(state)
			{
			case ButtonState.Normal:
				NormalImageIndex = nIndex;
				break;
			case ButtonState.Hot:
				HotImageIndex = nIndex;
				break;
			case ButtonState.Disabled:
				DisabledImageIndex = nIndex;
				break;
			case ButtonState.Pushed:
				PressedImageIndex = nIndex;
				break;
			}
		}

		/// <summary>
		/// Possible states of this button.
		/// </summary>
		public enum ButtonState
		{
			/// <summary>
			/// The normal button state. The button is not hovered with mouse nor is in a checked/pushed state.
			/// </summary>
			Normal,
			/// <summary>
			/// The button is hovered by the mouse, but not pushed.
			/// </summary>
			Hot,
			/// <summary>
			/// The button is disabled.
			/// </summary>
			Disabled,
			/// <summary>
			/// The button is pushed.
			/// </summary>
			Pushed
		}
    }

	/// <summary>
	/// An interface for the control that provides its background brush for painting the child controls' background.
	/// </summary>
	public interface IBackgroundBrushProvider
	{
		/// <summary>
		/// Requests the parent for a background brush for painting the child control <paramref name="sender"/>.
		/// If the brush is a gradient brush, its rectangle must be adjusted accordingly for the background to fit seamlessly.
		/// </summary>
		/// <param name="sender">A control that sends the brush request. Must be an immediate child of the requestee, its indirect child, or the same control.</param>
		/// <returns>A brush that should be disposed after use.</returns>
		Brush GetBackgroundBrush(Control sender);
	}
}
