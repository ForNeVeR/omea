// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Net;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.Favorites
{
    internal class AddFavoriteForm : DialogBase
    {
        private System.ComponentModel.IContainer components;
        private IResource _parent;
        private IResource _favorite;
        private System.Windows.Forms.ToolTip _errorToolTip;
        private System.Windows.Forms.Panel _mainPanel;
        private System.Windows.Forms.ComboBox _unitBox;
        private System.Windows.Forms.TextBox _fakeTextBox;
        internal System.Windows.Forms.TextBox _URLBox;
        internal System.Windows.Forms.TextBox _nameBox;
        internal System.Windows.Forms.NumericUpDown _hoursBox;
        internal System.Windows.Forms.CheckBox _updateCheckBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel _selectFolderPanel;
        private System.Windows.Forms.Label _createInLabel;
        private JetBrains.Omea.GUIControls.ResourceComboBox _createInCombo;
        private System.Windows.Forms.Panel _buttonsPanel;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private static IBookmarkService _bookmarkService;

        public AddFavoriteForm( IResource parent )
        {
            _parent = parent;
            _bookmarkService = (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
            InitializeComponent();
			this.Icon = Core.UIManager.ApplicationIcon;

			if((_nameBox.Visible) && (_nameBox.Enabled))
				_nameBox.Focus();
            _createInCombo.AddResourceHierarchy( _bookmarkService.BookmarksRoot,
                "Folder", FavoritesPlugin._propParent, new AcceptResourceDelegate( AcceptFolder ) );
            foreach( IBookmarkProfile profile in _bookmarkService.Profiles )
            {
                IResource root = _bookmarkService.GetProfileRoot( profile );
                string error;
                if( !_createInCombo.Items.Contains( root ) && profile.CanCreate( root, out error ) )
                {
                    _createInCombo.AddResourceHierarchy( root, "Folder", FavoritesPlugin._propParent, 1 );
                }
            }
            _createInCombo.SelectedItem = parent;
            _unitBox.SelectedIndex = 0;
            RestoreSettings();
            string defaultUrl = null;
            IDataObject dataObj = Clipboard.GetDataObject();
            if ( dataObj != null )
            {
                defaultUrl = (string) dataObj.GetData( typeof(string) );
                if ( defaultUrl != null && defaultUrl.Length > 0 )
                {
                    try
                    {
                        new Uri( defaultUrl );
                    }
                    catch
                    {
                        defaultUrl = "http://";
                    }
                }
            }
            _URLBox.Text = ( defaultUrl == null || defaultUrl.Length == 0 ) ? string.Empty : defaultUrl;
            _URLBox.SelectionStart = _URLBox.Text.Length;
            Height = MaximumSize.Height;
        }

        private bool AcceptFolder( IResource folder )
        {
            IBookmarkProfile profile = _bookmarkService.GetOwnerProfile( folder );
            string error;
            return profile == null || profile.CanCreate( folder, out error );
        }

        public static void EditFavorite( IResource favorite )
        {
            AddFavoriteForm theForm = new AddFavoriteForm( null );
            using( theForm )
            {
                if( favorite.Type != "Weblink" )
                {
                    favorite = favorite.GetLinkProp( "Source" );
                    if( favorite == null )
                    {
                        return;
                    }
                }
                theForm._favorite = favorite;
                theForm._URLBox.Text = favorite.GetPropText( FavoritesPlugin._propURL );
                theForm._nameBox.Text = favorite.GetPropText( Core.Props.Name );
                int freq = favorite.GetIntProp( FavoritesPlugin._propUpdateFreq ) / 3600;
                if( freq <= 0 )
                {
                    theForm._fakeTextBox.Visible = true;
                }
                else
                {
                    if( freq % 24 == 0 )
                    {
                        theForm._unitBox.SelectedIndex = 1;
                        freq /= 24;
                        if( freq % 7 == 0 )
                        {
                            theForm._unitBox.SelectedIndex = 2;
                            freq /= 7;
                        }
                    }
                    theForm._hoursBox.Value = freq;
                    theForm._updateCheckBox.Checked = true;
                }
                theForm._okButton.Enabled = true;
                theForm.Text = "Bookmark Properties";
                theForm._selectFolderPanel.Visible = false;
                theForm.Height = theForm.MinimumSize.Height;
                IBookmarkProfile profile = _bookmarkService.GetOwnerProfile( favorite );
                string error = null;
                bool readOnly = ( profile != null && !profile.CanCreate( null, out error ) );
                if( theForm._URLBox.ReadOnly = theForm._nameBox.ReadOnly = readOnly )
                {
                    theForm._errorToolTip.SetToolTip( theForm._URLBox, error );
                    theForm._errorToolTip.SetToolTip( theForm._nameBox, error );
                    DisplayError( error );
                }
                theForm.ShowDialog( Core.MainWindow );
            }
        }

        protected override void ScaleCore( float dx, float dy )
        {
            base.ScaleCore( dx, dy );
            if( Environment.Version.Major < 2 )
            {
                MinimumSize = new Size(
                    (int) ( (float) MinimumSize.Width * dx ), (int) ( (float) MinimumSize.Height * dy ) );
                MaximumSize = new Size(
                    (int) ( (float) MaximumSize.Width * dx ), (int) ( (float) MaximumSize.Height * dy ) );
            }
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

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(AddFavoriteForm));
			this._errorToolTip = new System.Windows.Forms.ToolTip(this.components);
			this._mainPanel = new System.Windows.Forms.Panel();
			this._unitBox = new System.Windows.Forms.ComboBox();
			this._fakeTextBox = new System.Windows.Forms.TextBox();
			this._URLBox = new System.Windows.Forms.TextBox();
			this._nameBox = new System.Windows.Forms.TextBox();
			this._hoursBox = new System.Windows.Forms.NumericUpDown();
			this._updateCheckBox = new System.Windows.Forms.CheckBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this._selectFolderPanel = new System.Windows.Forms.Panel();
			this._createInLabel = new System.Windows.Forms.Label();
			this._createInCombo = new JetBrains.Omea.GUIControls.ResourceComboBox();
			this._buttonsPanel = new System.Windows.Forms.Panel();
			this._okButton = new System.Windows.Forms.Button();
			this._cancelButton = new System.Windows.Forms.Button();
			this._mainPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._hoursBox)).BeginInit();
			this._selectFolderPanel.SuspendLayout();
			this._buttonsPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// _mainPanel
			//
			this._mainPanel.Controls.Add(this._unitBox);
			this._mainPanel.Controls.Add(this._fakeTextBox);
			this._mainPanel.Controls.Add(this._URLBox);
			this._mainPanel.Controls.Add(this._nameBox);
			this._mainPanel.Controls.Add(this._hoursBox);
			this._mainPanel.Controls.Add(this._updateCheckBox);
			this._mainPanel.Controls.Add(this.label2);
			this._mainPanel.Controls.Add(this.label1);
			this._mainPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this._mainPanel.Location = new System.Drawing.Point(0, 0);
			this._mainPanel.Name = "_mainPanel";
			this._mainPanel.Size = new System.Drawing.Size(512, 88);
			this._mainPanel.TabIndex = 0;
			//
			// _unitBox
			//
			this._unitBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._unitBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._unitBox.Items.AddRange(new object[] {
														  "hours",
														  "days",
														  "weeks"});
			this._unitBox.Location = new System.Drawing.Point(428, 62);
			this._unitBox.Name = "_unitBox";
			this._unitBox.Size = new System.Drawing.Size(76, 21);
			this._unitBox.TabIndex = 6;
			//
			// _fakeTextBox
			//
			this._fakeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._fakeTextBox.Enabled = false;
			this._fakeTextBox.Location = new System.Drawing.Point(376, 62);
			this._fakeTextBox.Name = "_fakeTextBox";
			this._fakeTextBox.Size = new System.Drawing.Size(32, 21);
			this._fakeTextBox.TabIndex = 5;
			this._fakeTextBox.Text = "";
			//
			// _URLBox
			//
			this._URLBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._URLBox.Location = new System.Drawing.Point(60, 6);
			this._URLBox.Name = "_URLBox";
			this._URLBox.Size = new System.Drawing.Size(444, 21);
			this._URLBox.TabIndex = 1;
			this._URLBox.Text = "http://";
			this._URLBox.TextChanged += new System.EventHandler(this._URLBox_TextChanged);
			//
			// _nameBox
			//
			this._nameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._nameBox.Location = new System.Drawing.Point(60, 34);
			this._nameBox.Name = "_nameBox";
			this._nameBox.Size = new System.Drawing.Size(444, 21);
			this._nameBox.TabIndex = 3;
			this._nameBox.Text = "";
			//
			// _hoursBox
			//
			this._hoursBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._hoursBox.Enabled = false;
			this._hoursBox.Location = new System.Drawing.Point(376, 62);
			this._hoursBox.Maximum = new System.Decimal(new int[] {
																	  99,
																	  0,
																	  0,
																	  0});
			this._hoursBox.Minimum = new System.Decimal(new int[] {
																	  1,
																	  0,
																	  0,
																	  0});
			this._hoursBox.Name = "_hoursBox";
			this._hoursBox.Size = new System.Drawing.Size(48, 21);
			this._hoursBox.TabIndex = 4;
			this._hoursBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this._hoursBox.ThousandsSeparator = true;
			this._hoursBox.Value = new System.Decimal(new int[] {
																	4,
																	0,
																	0,
																	0});
			//
			// _updateCheckBox
			//
			this._updateCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._updateCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._updateCheckBox.Location = new System.Drawing.Point(8, 62);
			this._updateCheckBox.Name = "_updateCheckBox";
			this._updateCheckBox.Size = new System.Drawing.Size(364, 20);
			this._updateCheckBox.TabIndex = 4;
			this._updateCheckBox.Text = "&Download the page and notify me when it is updated every";
			this._updateCheckBox.CheckedChanged += new System.EventHandler(this._updateCheckBox_CheckedChanged);
			//
			// label2
			//
			this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label2.Location = new System.Drawing.Point(8, 10);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(44, 16);
			this.label2.TabIndex = 0;
			this.label2.Text = "&URL:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// label1
			//
			this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label1.Location = new System.Drawing.Point(8, 38);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(44, 16);
			this.label1.TabIndex = 2;
			this.label1.Text = "&Name:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _selectFolderPanel
			//
			this._selectFolderPanel.Controls.Add(this._createInLabel);
			this._selectFolderPanel.Controls.Add(this._createInCombo);
			this._selectFolderPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this._selectFolderPanel.Location = new System.Drawing.Point(0, 88);
			this._selectFolderPanel.Name = "_selectFolderPanel";
			this._selectFolderPanel.Size = new System.Drawing.Size(512, 32);
			this._selectFolderPanel.TabIndex = 1;
			//
			// _createInLabel
			//
			this._createInLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._createInLabel.Location = new System.Drawing.Point(8, 9);
			this._createInLabel.Name = "_createInLabel";
			this._createInLabel.Size = new System.Drawing.Size(88, 16);
			this._createInLabel.TabIndex = 0;
			this._createInLabel.Text = "Create in &folder:";
			this._createInLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _createInCombo
			//
			this._createInCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._createInCombo.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this._createInCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._createInCombo.Location = new System.Drawing.Point(104, 5);
			this._createInCombo.Name = "_createInCombo";
			this._createInCombo.Size = new System.Drawing.Size(400, 22);
			this._createInCombo.TabIndex = 1;
			//
			// _buttonsPanel
			//
			this._buttonsPanel.Controls.Add(this._okButton);
			this._buttonsPanel.Controls.Add(this._cancelButton);
			this._buttonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._buttonsPanel.Location = new System.Drawing.Point(0, 120);
			this._buttonsPanel.Name = "_buttonsPanel";
			this._buttonsPanel.Size = new System.Drawing.Size(512, 34);
			this._buttonsPanel.TabIndex = 2;
			//
			// _okButton
			//
			this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._okButton.Enabled = false;
			this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._okButton.Location = new System.Drawing.Point(348, 6);
			this._okButton.Name = "_okButton";
			this._okButton.TabIndex = 0;
			this._okButton.Text = "OK";
			this._okButton.Click += new System.EventHandler(this.OKButton_Click);
			//
			// _cancelButton
			//
			this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._cancelButton.Location = new System.Drawing.Point(428, 6);
			this._cancelButton.Name = "_cancelButton";
			this._cancelButton.TabIndex = 1;
			this._cancelButton.Text = "Cancel";
			//
			// AddFavoriteForm
			//
			this.AcceptButton = this._okButton;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.CancelButton = this._cancelButton;
			this.ClientSize = new System.Drawing.Size(512, 154);
			this.Controls.Add(this._buttonsPanel);
			this.Controls.Add(this._selectFolderPanel);
			this.Controls.Add(this._mainPanel);
			this.MaximumSize = new System.Drawing.Size(1024, 188);
			this.MinimumSize = new System.Drawing.Size(520, 164);
			this.Name = "AddFavoriteForm";
			this.Text = "Add Bookmark";
			this._mainPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._hoursBox)).EndInit();
			this._selectFolderPanel.ResumeLayout(false);
			this._buttonsPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
        #endregion

        public void SetURL( string url )
        {
            _URLBox.Text = url;
        }

        private void _URLBox_TextChanged(object sender, System.EventArgs e)
        {
            _okButton.Enabled = _URLBox.Text.Length > 0;
        }

        private void OKButton_Click(object sender, System.EventArgs e)
        {
            if( _okButton.Enabled )
            {
                _okButton.Enabled = false;
                string url = _URLBox.Text.Trim();
                if( url.IndexOf( "://" ) < 0 )
                {
                    if( url.IndexOf( '\\' ) >= 0 )
                    {
                        url = "file://" + url;
                    }
                    else
                    {
                        url = "http://" + url;
                    }
                    _URLBox.Text = url;
                }
                try
                {
                    new Uri( url );
                }
                catch( Exception exc )
                {
                    Utils.DisplayException( exc, "Bad URL" );
                    _URLBox.Focus();
                    _okButton.Enabled = true;
                    return;
                }

                string bookmarkName = _nameBox.Text;
                if( bookmarkName.Length == 0 )
                {
                    bookmarkName = url;
                }
                _nameBox.Text = bookmarkName;
                if( _parent != null )
                {
                    if( _createInCombo.SelectedItem != null )
                    {
                        _parent = (IResource) _createInCombo.SelectedItem;
                    }
                    IBookmarkProfile profile = _bookmarkService.GetOwnerProfile( _parent );
                    if( profile != null )
                    {
                        bookmarkName = FavoritesTools.GetSafeBookmarkName( profile, bookmarkName );
                        string error = null;
                        IResource tempWeblink = Core.ResourceStore.NewResourceTransient( "Weblink" );
                        tempWeblink.SetProp( FavoritesPlugin._propURL, url );
                        tempWeblink.SetProp( Core.Props.Name, bookmarkName );
                        tempWeblink.AddLink( FavoritesPlugin._propParent, _parent );
                        if( !profile.CanCreate( tempWeblink, out error ) )
                        {
                            DisplayError( error );
                            _nameBox.Focus();
                            _okButton.Enabled = true;
                            return;
                        }
                    }
                }

                bool newFavorite = _favorite == null;
                Core.ResourceAP.RunUniqueJob( new MethodInvoker( NewWeblink ) );
                if( newFavorite )
                {
                    FavoritesPlugin._favoritesTreePane.SelectResource( _favorite );
                }
                Close();
            }
        }

        private void _updateCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            _fakeTextBox.Visible = !( _hoursBox.Enabled = _updateCheckBox.Checked );
            if( _hoursBox.Enabled )
            {
                _hoursBox.Focus();
            }
        }

        private void NewWeblink()
        {
            bool newWeblink = false;
            if( _favorite == null )
            {
                _favorite = Core.ResourceStore.BeginNewResource( "Weblink" );
                newWeblink = true;
            }
            else
            {
                _favorite.BeginUpdate();
            }
            try
            {
                string url = _URLBox.Text;
                _favorite.SetProp( Core.Props.Name, _nameBox.Text );
                _favorite.SetProp( FavoritesPlugin._propURL, url );
                int updateFreq = 0;
                if( _updateCheckBox.Checked )
                {
                    updateFreq = (int) _hoursBox.Value * 60 * 60;
                    int unitIndex = _unitBox.SelectedIndex;
                    if( unitIndex > 0 ) // days or weeks
                    {
                        updateFreq *= 24;
                        if( unitIndex > 1 ) // weeks
                        {
                            updateFreq *= 7;
                        }
                    }
                }
                _favorite.SetProp( FavoritesPlugin._propUpdateFreq, updateFreq );
                if( _parent != null )
                {
                    _favorite.AddLink( FavoritesPlugin._propParent, _parent );
                }
                Core.WorkspaceManager.AddToActiveWorkspace( _favorite );
            }
            finally
            {
                _favorite.EndUpdate();
            }
            if( newWeblink )
            {
                IBookmarkProfile profile = _bookmarkService.GetOwnerProfile( _favorite );
                string error = null;
                if( profile != null && profile.CanCreate( _favorite, out error ) )
                {
                    profile.Create( _favorite );
                }
                else
                {
                    Core.UserInterfaceAP.QueueJob( new LineDelegate( DisplayError ), error );
                }
                BookmarkService.ImmediateQueueWeblink( _favorite, _URLBox.Text );
            }
        }

        private static void DisplayError( string error )
        {
            if( error != null && error.Length > 0 )
            {
                MessageBox.Show( Core.MainWindow, error, "Bookmark Properties", MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
        }
    }
}
