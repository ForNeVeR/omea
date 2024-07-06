// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.FileTypes;

namespace JetBrains.Omea.Pictures
{
	internal class PicturePreview : System.Windows.Forms.UserControl, IDisplayPane
	{
        private System.Windows.Forms.Label _errorLabel;
        private System.Windows.Forms.Panel _picturePanel;
        private System.Windows.Forms.PictureBox _picturebox;
        private bool _bShrinkToFit = true;
        private Stream _imageStream;
        private IResource _resource;

		public PicturePreview()
		{
			_bShrinkToFit = Core.SettingStore.ReadBool( "Pictures", "ShrinkToFit", _bShrinkToFit );
			InitializeComponent();
		}

		protected override void Dispose( bool disposing )
		{
            Core.SettingStore.WriteBool( "Pictures", "ShrinkToFit", _bShrinkToFit );
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._errorLabel = new System.Windows.Forms.Label();
            this._picturePanel = new System.Windows.Forms.Panel();
            this._picturebox = new System.Windows.Forms.PictureBox();
            this._picturePanel.SuspendLayout();
            this.SuspendLayout();
            //
            // _errorLabel
            //
            this._errorLabel.Location = new System.Drawing.Point(8, 8);
            this._errorLabel.Name = "_errorLabel";
            this._errorLabel.Size = new System.Drawing.Size(272, 23);
            this._errorLabel.TabIndex = 1;
            this._errorLabel.Text = "Could not display image, it probably is corrupted.";
            //
            // _picturePanel
            //
            this._picturePanel.AutoScroll = true;
            this._picturePanel.Controls.Add(this._picturebox);
            this._picturePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._picturePanel.Location = new System.Drawing.Point(0, 0);
            this._picturePanel.Name = "_picturePanel";
            this._picturePanel.Size = new System.Drawing.Size(456, 304);
            this._picturePanel.TabIndex = 2;
            this._picturePanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this._picturePanel_MouseDown);
            //
            // _pictureBox
            //
            this._picturebox.Name = "_picturebox";
            this._picturebox.TabIndex = 1;
            this._picturebox.TabStop = false;
            this._picturebox.MouseDown += new System.Windows.Forms.MouseEventHandler(this._picturePanel_MouseDown);
            //
            // PicturePreview
            //
            this.Controls.Add(this._picturePanel);
            this.Controls.Add(this._errorLabel);
            this.Name = "PicturePreview";
            this.Size = new System.Drawing.Size(456, 304);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PicturePreview_KeyDown);
            this._picturePanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

	    void IDisplayPane.DisposePane()
        {
            // do nothing
        }

        Control IDisplayPane.GetControl()
        {
            return this;
        }

        public void DisplayResource( IResource resource )
        {
            _resource = resource;
            _errorLabel.Visible = false;
            _picturePanel.Visible = true;
			using( new LayoutSuspender(this) )
            {
                FileStream stream = IOTools.OpenRead( Core.FileResourceManager.GetSourceFile( resource ) );
                if( stream != null )
                {
                    _imageStream = new JetMemoryStream( 0x10000 );
                    using( stream )
                    {
                        FileResourceManager.CopyStream( stream, _imageStream );
                        try
                        {
                            _picturebox.Image = Image.FromStream( _imageStream );
                        }
                        catch( Exception )
                        {
                            _errorLabel.Visible = true;
                            _picturePanel.Visible = false;
                            return;
                        }
                    }
                }
            }
			PerformLayout();
        }

        void IDisplayPane.EndDisplayResource( IResource res )
        {
            _resource = null;
            _imageStream = null;
        }

        void IDisplayPane.HighlightWords( WordPtr[] words )
        {
        }

	    public string GetSelectedText( ref TextFormat format )
        {
            return null;
	    }

	    public string GetSelectedPlainText()
	    {
	        return null;
	    }

	    bool ICommandProcessor.CanExecuteCommand( string action )
        {
            return false;
        }

        void ICommandProcessor.ExecuteCommand( string action )
        {
        }

		/// <summary>
		/// Gets whether the images are shrinked to fit the preview pane if they are larger.
		/// </summary>
		internal bool ShrinkToFit
		{
			get { return _bShrinkToFit; }
		}

		internal void ToggleShrinkToFit()
        {
        	_bShrinkToFit = !_bShrinkToFit;
        	PerformLayout();
        }

        private void _picturePanel_MouseDown( object sender, System.Windows.Forms.MouseEventArgs e )
        {
            if( _resource != null && e.Button == MouseButtons.Right )
            {
                Core.ActionManager.ShowResourceContextMenu(
                    new ActionContext( ActionContextKind.ContextMenu, this, _resource.ToResourceList() ), this, e.X, e.Y );
            }
        }

        private void PicturePreview_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            e.Handled = Core.ActionManager.ExecuteKeyboardAction(
                new ActionContext( ActionContextKind.Keyboard, this, _resource.ToResourceList() ), e.KeyData );
		}

		/// <summary>
		/// Layouts the panel that contains a picture box.
		/// </summary>
		protected void LayoutPicturePanel()
		{
			using( new LayoutSuspender( _picturebox ) )
			{
				// Do not layout if the image has not been loaded yet
				if(_picturebox.Image == null)
				{
					_picturebox.Visible = false;
					return;
				}

				Size sizeImage = _picturebox.Image.Size;

				if( (sizeImage.Width <= _picturePanel.Width) && (sizeImage.Height <= _picturePanel.Height) )
				{ // Images fits into the view
					_picturebox.Location = new Point( 0, 0 ); // Default location is at the upper-left corner
					_picturebox.Size = _picturePanel.Size;
					_picturebox.SizeMode = PictureBoxSizeMode.CenterImage;
				}
				else
				{ // Image does not fit, further processing depends on the mode
					if( _bShrinkToFit )
					{ // Shrink the image and center in the view
						_picturebox.SizeMode = PictureBoxSizeMode.StretchImage;

						// Size either to fit-width or fit-height, whichever is stricter
						float widthRatio = (float)sizeImage.Width / (float)_picturePanel.Width;
						float heightRatio = (float)sizeImage.Height / (float)_picturePanel.Height;
						if( widthRatio > heightRatio )
						{
							_picturebox.Width = _picturePanel.Width;
							_picturebox.Height = (int)(_picturePanel.Height * heightRatio / widthRatio);
						}
						else
						{
							_picturebox.Height = _picturePanel.Height;
							_picturebox.Width = (int)(_picturePanel.Width * widthRatio / heightRatio);
						}

						// Center the image in the view
						_picturebox.Left = 0 + (_picturePanel.Width - _picturebox.Width) / 2;
						_picturebox.Top = 0 + (_picturePanel.Height - _picturebox.Height) / 2;
					}
					else
					{ // Show the image 1:1 and allow scrolling it
						_picturebox.Location = new Point( 0, 0 ); // Default location is at the upper-left corner
						_picturebox.Size = _picturePanel.Size;
						_picturebox.SizeMode = PictureBoxSizeMode.AutoSize;
					}
				}

				_picturebox.Visible = true;
			}
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			using( new LayoutSuspender( _picturePanel ) )
			using( new LayoutSuspender( _picturebox ) )
			{
				_picturePanel.Bounds = ClientRectangle;
				LayoutPicturePanel();
			}
			// Update the layout twice to adjust the scrollbars properly
			_picturePanel.PerformLayout();
			//_picturePanel.PerformLayout(  );
		}
	}
}
