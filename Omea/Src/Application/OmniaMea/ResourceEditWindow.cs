// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    /// <summary>
    /// Container for editing resources in a separate window.
    /// </summary>
    internal class ResourceEditWindow : Form
	{
        private Button  _btnSave;
        private Button  _btnCancel;
        private Panel   _contentPane;
        private Label   _lblErrorMessage;
        /// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

        private AbstractEditPane _editPane;
        private IResource _resource;
        private IResourceList _resourceWatchList;
        private bool _newResource;
        private EditedResourceSavedDelegate _savedHandler;
        private object _savedHandlerTag;
        private SizeF _scaleFactor = new SizeF( 1.0f, 1.0f );

		public ResourceEditWindow()
		{
			InitializeComponent();
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._btnSave = new System.Windows.Forms.Button();
			this._btnCancel = new System.Windows.Forms.Button();
			this._contentPane = new System.Windows.Forms.Panel();
			this._lblErrorMessage = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// _btnSave
			//
			this._btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnSave.Location = new System.Drawing.Point(212, 207);
			this._btnSave.Name = "_btnSave";
			this._btnSave.Size = new System.Drawing.Size(100, 23);
			this._btnSave.TabIndex = 1;
			this._btnSave.Text = "Save and Close";
			this._btnSave.Click += new System.EventHandler(this._btnSave_Click);
			//
			// _btnCancel
			//
			this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnCancel.Location = new System.Drawing.Point(320, 207);
			this._btnCancel.Name = "_btnCancel";
			this._btnCancel.Size = new System.Drawing.Size(92, 23);
			this._btnCancel.TabIndex = 2;
			this._btnCancel.Text = "Cancel";
			this._btnCancel.Click += new System.EventHandler(this._btnCancel_Click);
			//
			// _contentPane
			//
			this._contentPane.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._contentPane.Location = new System.Drawing.Point(0, 0);
			this._contentPane.Name = "_contentPane";
			this._contentPane.Size = new System.Drawing.Size(420, 199);
			this._contentPane.TabIndex = 0;
			this._contentPane.Resize += new EventHandler(_contentPane_Resize);
			//
			// _lblErrorMessage
			//
			this._lblErrorMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._lblErrorMessage.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._lblErrorMessage.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
			this._lblErrorMessage.ForeColor = System.Drawing.Color.Red;
			this._lblErrorMessage.Location = new System.Drawing.Point(8, 207);
			this._lblErrorMessage.Name = "_lblErrorMessage";
			this._lblErrorMessage.Size = new System.Drawing.Size(196, 16);
			this._lblErrorMessage.TabIndex = 3;
			this._lblErrorMessage.Text = "label1";
			this._lblErrorMessage.Visible = false;
			//
			// ResourceEditWindow
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.CancelButton = this._btnCancel;
			this.ClientSize = new System.Drawing.Size(420, 238);
			this.Controls.Add(this._lblErrorMessage);
			this.Controls.Add(this._contentPane);
			this.Controls.Add(this._btnCancel);
			this.Controls.Add(this._btnSave);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
			this.KeyPreview = true;
			this.Location = new System.Drawing.Point(100, 100);
			this.MinimumSize = new System.Drawing.Size(100, 100);
			this.Name = "ResourceEditWindow";
			this.Text = "ResourceEditWindow";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ResourceEditWindow_KeyDown);
			this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ResourceEditWindow_KeyPress);
			this.Load += new System.EventHandler(this.ResourceEditWindow_Load);
            this.Closing += new CancelEventHandler( ResourceEditWindow_Closing ) ;
			this.Closed += new System.EventHandler(this.ResourceEditWindow_Closed);
			this.ResumeLayout( false );
		}

        #endregion

        protected override void ScaleCore( float x, float y )
        {
            base.ScaleCore( x, y );
            _scaleFactor = new SizeF( x, y );
        }

        public IResource Resource
        {
            get { return _resource; }
        }

        /**
         * Sets the edit pane that should be displayed in the window.
         */

        public void SetEditPane( AbstractEditPane editPane, IResource res, bool newResource,
                                 EditedResourceSavedDelegate savedHandler, object savedHandlerTag )
        {
            if ( newResource )
            {
                Text = "New " + Core.ResourceStore.ResourceTypes [res.Type].DisplayName;
            }
            else
            {
                Text = res.DisplayName + " — " + Core.ResourceStore.ResourceTypes [res.Type].DisplayName;
            }

            Icon icon = Core.ResourceIconManager.GetResourceLargeIcon( res.Type );
            if ( icon != null )
            {
                Icon = icon;
            }

            _editPane = editPane;
            _resource = res;
            _newResource = newResource;

            editPane.ValidStateChanged += OnEditPaneValidStateChanged;
            _resourceWatchList = res.ToResourceListLive();
            _resourceWatchList.ResourceDeleting += OnEditedResourceDeleting;
            editPane.EditResource( res );

			// Autosize to fit the edit pane
            GrowToContent();

			// Apply the minimum sizes
            Size minSize = _editPane.GetMinimumSize();
            if ( !minSize.IsEmpty )
            {
                MinimumSize = new Size( (Width - _contentPane.Width) + minSize.Width,
                    (Height - _contentPane.Height) + minSize.Height );
            }

			// Try to load and apply the size from settings
			Left = Core.SettingStore.ReadInt( "ResourceEditWindow", _resource.Type + ".Left", Left );
			Top = Core.SettingStore.ReadInt( "ResourceEditWindow", _resource.Type + ".Top", Top);

            int width = Core.SettingStore.ReadInt( "ResourceEditWindow", _resource.Type + ".Width", -1 );
            int height = Core.SettingStore.ReadInt( "ResourceEditWindow", _resource.Type + ".Height", -1 );
            if ( width > 0 && height > 0 )
            {
                ClientSize = new Size( width, height );
            }

            editPane.Dock = DockStyle.Fill;
            _contentPane.Controls.Add( editPane );
            editPane.Select();

            _savedHandler    = savedHandler;
            _savedHandlerTag = savedHandlerTag;
        }

        /**
         * If the edit pane is larger than the available space in the form, enlarges the form.
         */

        private void GrowToContent()
        {
            if ( _contentPane.Width < _editPane.Width )
            {
                Width = (Width - _contentPane.Width) + _editPane.Width;
            }

            if ( _contentPane.Height < _editPane.Height )
            {
                Height = (Height - _contentPane.Height) + _editPane.Height;
            }
        }

        private void _btnSave_Click( object sender, EventArgs e )
        {
            _btnSave.Enabled = false;
            _btnCancel.Enabled = false;
            _editPane.Save();
            if ( _savedHandler != null )
            {
                _savedHandler( _resource, _savedHandlerTag );
            }
            if ( _resource.IsTransient )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( _resource.EndUpdate ) );
            }

            //  Save the size and the position of the edit window
            Close();
        }

        private void _btnCancel_Click( object sender, EventArgs e )
        {
            _btnSave.Enabled = false;
            _btnCancel.Enabled = false;
            _editPane.Cancel();
            DisposeWatchList();
            if ( _newResource )
            {
                new ResourceProxy( _resource ).DeleteAsync();
            }
            Close();
        }

        //  Save the size and position of the edit window
        private void  SaveSizeAndPosition()
        {
			if ( Core.SettingStore == null )
			{
			    return;
			}
            Core.SettingStore.WriteInt( "ResourceEditWindow", _resource.Type + ".Left", Left );
			Core.SettingStore.WriteInt( "ResourceEditWindow", _resource.Type + ".Top", Top );
			Core.SettingStore.WriteInt( "ResourceEditWindow", _resource.Type + ".Width",
                (int) (ClientSize.Width / _scaleFactor.Width) );
			Core.SettingStore.WriteInt( "ResourceEditWindow", _resource.Type + ".Height",
                (int) (ClientSize.Height / _scaleFactor.Height) );
        }

        private void OnEditPaneValidStateChanged( object sender, ValidStateEventArgs e )
        {
            _btnSave.Enabled = e.IsValid || e.IsWarning;
            _lblErrorMessage.Visible = !e.IsValid;
            if ( !e.IsValid )
            {
                _lblErrorMessage.Text = e.Message;
            }
        }

        private void ResourceEditWindow_Closing( object sender, CancelEventArgs e )
        {
            SaveSizeAndPosition();
        }

        private void ResourceEditWindow_Closed( object sender, EventArgs e )
        {
            DisposeWatchList();
        }

        private void DisposeWatchList()
        {
            if ( _resourceWatchList != null )
            {
                _resourceWatchList.ResourceDeleting -= OnEditedResourceDeleting;
                _resourceWatchList.Dispose();
                _resourceWatchList = null;
            }
        }

        private void OnEditedResourceDeleting( object sender, ResourceIndexEventArgs e )
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( CancelEditForm ) );
        }

        private void CancelEditForm()
        {
            _editPane.Cancel();
            Close();
        }

        private void ResourceEditWindow_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.Modifiers == Keys.Control && e.KeyCode == Keys.Enter )
            {
                _btnSave.PerformClick();
            }
        }

        private void ResourceEditWindow_KeyPress( object sender, KeyPressEventArgs e )
        {
            if ( e.KeyChar == '\n' && ModifierKeys == Keys.Control )
            {
                e.Handled = true;
            }
        }

		private void ResourceEditWindow_Load(object sender, EventArgs e)
		{
			Left = Core.SettingStore.ReadInt( "ResourceEditWindow", _resource.Type + ".Left", Left );
			Top = Core.SettingStore.ReadInt( "ResourceEditWindow", _resource.Type + ".Top", Top);
        }

        //  Force setting height of the nested pane artificially since
        //  during the resize of the main form, size of the nested pane
        //  is set inproperly (usually lesser) (OM-5995).
        private void _contentPane_Resize(object sender, EventArgs e)
        {
            if( _editPane != null && _contentPane.Height > _editPane.Height )
            {
                _editPane.Height = _contentPane.Height;
            }
        }
    }
}
