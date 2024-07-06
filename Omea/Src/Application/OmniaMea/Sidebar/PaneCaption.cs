// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea
{
    /// <summary>
    /// The caption of an expanded or collapsed pane, with a set of buttons (minimize, restore).
    /// </summary>
    internal class PaneCaption : UserControl
	{
        private Label _captionLabel;
        private ImageList _iconList;
        private ImageListPictureBox _minimizeBtn;
        private ImageListPictureBox _restoreBtn;

        private PaneCaptionButtons _captionButtons = (PaneCaptionButtons.Minimize | PaneCaptionButtons.Restore);

        private Timer _restoreTimer;
        private Timer _dragOverTimer;
        private ToolTip _toolTip;
        private bool _active = true;
        private ColorScheme _colorScheme;

        private System.ComponentModel.IContainer components;

		public PaneCaption()
		{
			InitializeComponent();

            TabStop = false;
		}

        /// <summary>
        /// Fired when the Minimize button is clicked on the pane caption.
        /// </summary>
        public event EventHandler MinimizeClick;

        /// <summary>
        /// Fired when the Restore button is clicked on the pane caption.
        /// </summary>
        public event EventHandler RestoreClick;

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
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(PaneCaption));
            this._captionLabel = new System.Windows.Forms.Label();
            this._iconList = new System.Windows.Forms.ImageList(this.components);
            this._minimizeBtn = new GUIControls.ImageListPictureBox();
            this._restoreBtn = new GUIControls.ImageListPictureBox();
            this._restoreTimer = new System.Windows.Forms.Timer(this.components);
            this._dragOverTimer = new System.Windows.Forms.Timer(this.components);
            this._toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // _captionLabel
            //
            this._captionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._captionLabel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this._captionLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._captionLabel.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._captionLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this._captionLabel.Location = new System.Drawing.Point(5, 1);
            this._captionLabel.Name = "_captionLabel";
            this._captionLabel.Size = new System.Drawing.Size(140, 23);
            this._captionLabel.TabIndex = 1;
            this._captionLabel.Text = "label1";
            this._captionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._captionLabel.DoubleClick += new System.EventHandler(this._captionLabel_DoubleClick);
            //
            // _iconList
            //
            this._iconList.ImageSize = new System.Drawing.Size(16, 16);
            this._iconList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_iconList.ImageStream")));
            this._iconList.TransparentColor = System.Drawing.Color.Transparent;
            //
            // _minimizeBtn
            //
            this._minimizeBtn.Dock = System.Windows.Forms.DockStyle.Right;
            this._minimizeBtn.ImageIndex = 1;
            this._minimizeBtn.ImageList = this._iconList;
            this._minimizeBtn.Location = new System.Drawing.Point(101, 1);
            this._minimizeBtn.Name = "_minimizeBtn";
            this._minimizeBtn.Size = new System.Drawing.Size(16, 16);
            this._minimizeBtn.TabIndex = 3;
            this._minimizeBtn.Text = "imageListPictureBox1";
            this._toolTip.SetToolTip(this._minimizeBtn, "Minimize Pane");
            this._minimizeBtn.Click += new System.EventHandler(this._minimizeBtn_Click);
            //
            // _restoreBtn
            //
            this._restoreBtn.Dock = System.Windows.Forms.DockStyle.Right;
            this._restoreBtn.ImageIndex = 2;
            this._restoreBtn.ImageList = this._iconList;
            this._restoreBtn.Location = new System.Drawing.Point(117, 1);
            this._restoreBtn.Name = "_restoreBtn";
            this._restoreBtn.Size = new System.Drawing.Size(16, 16);
            this._restoreBtn.TabIndex = 4;
            this._restoreBtn.Text = "imageListPictureBox1";
            this._toolTip.SetToolTip(this._restoreBtn, "Restore Pane");
            this._restoreBtn.Click += new System.EventHandler(this._restoreBtn_Click);
            //
            // _restoreTimer
            //
            this._restoreTimer.Interval = 200;
            this._restoreTimer.Tick += new System.EventHandler(this._restoreTimer_Tick);
            //
            // _dragOverTimer
            //
            this._dragOverTimer.Interval = 500;
            this._dragOverTimer.Tick += new System.EventHandler(this._dragOverTimer_Tick);
            //
            // PaneCaption
            //
            Height = 18;
            Visible = false;
            Active = false;

            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.Controls.Add(this._minimizeBtn);
            this.Controls.Add(this._restoreBtn);
            this.Controls.Add(this._captionLabel);
            this.DockPadding.All = 1;
            this.Name = "PaneCaption";
            this.Size = new System.Drawing.Size(150, 18);
            this.ResumeLayout(false);
        }
		#endregion

        /**
         * Sets or gets the text displayed in the caption label.
         */

        public override string Text
        {
            get { return _captionLabel.Text; }
            set { _captionLabel.Text = value; }
        }

        /**
         * Selects the buttons displayed in the caption.
         */

        public PaneCaptionButtons CaptionButtons
        {
            get { return _captionButtons; }
            set
            {
                if ( _captionButtons != value )
                {
                    _captionButtons = value;
                    _minimizeBtn.Visible = ( (_captionButtons & PaneCaptionButtons.Minimize) != 0 );
                    _restoreBtn.Visible  = ( (_captionButtons & PaneCaptionButtons.Restore) != 0 );
                }
            }
        }

	    public ColorScheme ColorScheme
	    {
	        get { return _colorScheme; }
	        set
            {
                _colorScheme = value;
                UpdateColors();
            }
	    }

	    /**
         * Specifies if the caption is drawn in an active or inactive color.
         */

        public bool Active
        {
            get { return _active; }
            set
            {
                _active = value;
                UpdateColors();
            }
        }

	    private void UpdateColors()
	    {
	        BackColor = _active
	            ? ColorScheme.GetColor( _colorScheme, "PaneCaption.Active", SystemColors.ActiveCaption )
	            : ColorScheme.GetColor( _colorScheme, "PaneCaption.Inactive", SystemColors.InactiveCaption );
	        _captionLabel.BackColor = BackColor;
	        _captionLabel.ForeColor = _active
	            ? ColorScheme.GetColor( _colorScheme, "PaneCaption.ActiveText", SystemColors.ActiveCaptionText )
	            : ColorScheme.GetColor( _colorScheme, "PaneCaption.InactiveText", SystemColors.InactiveCaptionText );
	    }

        private void _minimizeBtn_Click( object sender, EventArgs e )
        {
            if ( MinimizeClick != null )
            {
                MinimizeClick( this, EventArgs.Empty );
            }
        }

        private void _restoreBtn_Click( object sender, EventArgs e )
        {
            if ( RestoreClick != null )
            {
                RestoreClick( this, EventArgs.Empty );
            }
        }

        /**
         * A double-click on the header of a pane maximizes it if the Maximize button
         * is visible, and restores it otherwise.
         */

        private void _captionLabel_DoubleClick( object sender, EventArgs e )
        {
            _restoreTimer.Stop();
            if ( _restoreBtn.Visible && RestoreClick != null )
            {
                RestoreClick( this, EventArgs.Empty );
            }
        }

        private void _restoreTimer_Tick( object sender, EventArgs e )
        {
            _restoreTimer.Stop();
            if ( _restoreBtn.Visible )
            {
                if ( RestoreClick != null )
                {
                    RestoreClick( this, EventArgs.Empty );
                }
            }
            else
            {
                OnClick( e );
            }
        }

        /**
         * When a drag enters the pane caption, starts a timer to restore the pane.
         */

        protected override void OnDragEnter( DragEventArgs drgevent )
        {
            base.OnDragEnter( drgevent );
            if ( _restoreBtn.Visible )
            {
                _dragOverTimer.Start();
            }
        }

        /**
         * When a drag leaves the pane caption, stops the drag over timer.
         */

        protected override void OnDragLeave( EventArgs e )
        {
            base.OnDragLeave( e );
            _dragOverTimer.Stop();
        }

        /**
         * When the drag has been hovering over the caption for 500 ms, restores the pane.
         */

        private void _dragOverTimer_Tick( object sender, EventArgs e )
        {
            _dragOverTimer.Stop();
            if ( RestoreClick != null )
            {
                RestoreClick( this, EventArgs.Empty );
            }
        }

        protected override void OnPaint( PaintEventArgs pevent )
        {
            base.OnPaint( pevent );
            Pen borderPen = ColorScheme.GetPen( _colorScheme, "PaneCaption.Border", SystemPens.Control );
            int x2 = ClientRectangle.Width-1;
            int y2 = ClientRectangle.Height-1;
            pevent.Graphics.DrawLine( borderPen, 0, 0, x2, 0 );
            pevent.Graphics.DrawLine( borderPen, 0, 0, 0, y2 );
            pevent.Graphics.DrawLine( borderPen, x2, 0, x2, y2 );
        }
	}

    [Flags]
    public enum PaneCaptionButtons
    {
        None = 0, Minimize = 2, Restore = 4
    }
}
