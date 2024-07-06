// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;

namespace JetBrains.Omea.Favorites
{
    internal class FavoritesPropertiesForm : DialogBase
    {
        internal System.Windows.Forms.CheckBox _updateCheckBox;
        internal System.Windows.Forms.NumericUpDown _hoursBox;
        internal GUIControls.ResourceListView2 _favoritesListView;
        internal System.Windows.Forms.Button _cancelButton;
        internal System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.TextBox _fakeTextBox;
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.ComboBox _unitBox;

        private static IResourceList  content;
        private int _commonFreq;

		public FavoritesPropertiesForm()
		{
			InitializeComponent();
            InitializeListColumns();
            _unitBox.SelectedIndex = 0;
            RestoreSettings();
		}

        private void  InitializeListColumns()
        {
            _favoritesListView.AllowColumnReorder = false;
            _favoritesListView.Columns.Add( new ResourceIconColumn() );
            ResourceListView2Column nameCol = _favoritesListView.AddColumn( Core.Props.Name );
            nameCol.AutoSize = true;
        }

        public static void EditFavoritesProperties( IResourceList selectedResources )
        {
            FavoritesPropertiesForm theForm = new FavoritesPropertiesForm();
            selectedResources.Sort( "ID" );

            content = Core.ResourceStore.EmptyResourceList;
            foreach( IResource res in selectedResources )
            {
                RecursivelyUpdateResourceList( ref content, res, false );
            }
            foreach( IResource page in content )
            {
                theForm._favoritesListView.JetListView.Nodes.Add( page );
            }

            // commonFreq is calculated as common updating frequency for all
            // weblinks in the list. it is set not to zero only if it is
            // equal for all resources in the list
            int commonFreq = 0;
            for( int i = 0; i < content.Count; ++i )
            {
                IResource webLink = content[ i ];
                if( webLink.Type != "Weblink" && webLink.Type != "Folder" )
                {
                    webLink = webLink.GetLinkProp( "Source" );
                }
                int freq = webLink.GetIntProp( FavoritesPlugin._propUpdateFreq );
                if( commonFreq == 0 )
                {
                    commonFreq = freq;
                }
                else if( commonFreq != freq )
                {
                    commonFreq = -1;
                    break;
                }
            }
            commonFreq /= 3600;
            theForm._commonFreq = commonFreq;
            if( commonFreq <= 0 )
            {
                theForm._fakeTextBox.Visible = true;
            }
            else
            {
                if( commonFreq % 24 == 0 )
                {
                    commonFreq /= 24;
                    theForm._unitBox.SelectedIndex = 1;
                }
                if( commonFreq % 7 == 0 )
                {
                    commonFreq /= 7;
                    theForm._unitBox.SelectedIndex = 2;
                }
                theForm._hoursBox.Value = commonFreq;
                theForm._updateCheckBox.Checked = true;
            }
            theForm.ShowDialog( Core.MainWindow );
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
		private void InitializeComponent()
		{
            this._updateCheckBox = new System.Windows.Forms.CheckBox();
            this._hoursBox = new System.Windows.Forms.NumericUpDown();
            this._favoritesListView = new JetBrains.Omea.GUIControls.ResourceListView2();
            this._cancelButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this._fakeTextBox = new System.Windows.Forms.TextBox();
            this._unitBox = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this._hoursBox)).BeginInit();
            this.SuspendLayout();
            //
            // _updateCheckBox
            //
            this._updateCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._updateCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._updateCheckBox.Location = new System.Drawing.Point(8, 8);
            this._updateCheckBox.Name = "_updateCheckBox";
            this._updateCheckBox.Size = new System.Drawing.Size(368, 21);
            this._updateCheckBox.TabIndex = 0;
            this._updateCheckBox.Text = "Download pages and notify me when they are updated every";
            this._updateCheckBox.CheckedChanged += new System.EventHandler(this._updateCheckBox_CheckedChanged);
            //
            // _hoursBox
            //
            this._hoursBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._hoursBox.Enabled = false;
            this._hoursBox.Location = new System.Drawing.Point(380, 8);
            this._hoursBox.Maximum = new System.Decimal(new int[] {
                                                                      999,
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
            this._hoursBox.TabIndex = 1;
            this._hoursBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._hoursBox.ThousandsSeparator = true;
            this._hoursBox.Value = new System.Decimal(new int[] {
                                                                    4,
                                                                    0,
                                                                    0,
                                                                    0});
            //
            // _favoritesListView
            //
            this._favoritesListView.AllowDrop = true;
            this._favoritesListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._favoritesListView.CausesValidation = false;
            this._favoritesListView.FullRowSelect = true;
            this._favoritesListView.HideSelection = false;
            this._favoritesListView.Location = new System.Drawing.Point(8, 43);
            this._favoritesListView.MultiSelect = false;
            this._favoritesListView.Name = "_favoritesListView";
            this._favoritesListView.ShowContextMenu = false;
            this._favoritesListView.Size = new System.Drawing.Size(496, 85);
            this._favoritesListView.TabIndex = 3;
            this._favoritesListView.HeaderStyle = ColumnHeaderStyle.None;
            //
            // _cancelButton
            //
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(432, 136);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 5;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.Click += new System.EventHandler(this._cancelButton_Click);
            //
            // _okButton
            //
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(352, 136);
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 4;
            this._okButton.Text = "OK";
            this._okButton.Click += new System.EventHandler(this._okButton_Click);
            //
            // _fakeTextBox
            //
            this._fakeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._fakeTextBox.Enabled = false;
            this._fakeTextBox.Location = new System.Drawing.Point(380, 8);
            this._fakeTextBox.Name = "_fakeTextBox";
            this._fakeTextBox.Size = new System.Drawing.Size(32, 21);
            this._fakeTextBox.TabIndex = 6;
            this._fakeTextBox.Text = "";
            this._fakeTextBox.Visible = false;
            //
            // _unitBox
            //
            this._unitBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._unitBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._unitBox.Items.AddRange(new object[] {
                                                          "hours",
                                                          "days",
                                                          "weeks"});
            this._unitBox.Location = new System.Drawing.Point(432, 8);
            this._unitBox.Name = "_unitBox";
            this._unitBox.Size = new System.Drawing.Size(72, 21);
            this._unitBox.TabIndex = 2;
            //
            // FavoritesPropertiesForm
            //
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(512, 166);
            this.Controls.Add(this._unitBox);
            this.Controls.Add(this._fakeTextBox);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._favoritesListView);
            this.Controls.Add(this._hoursBox);
            this.Controls.Add(this._updateCheckBox);
            this.MinimumSize = new System.Drawing.Size(520, 160);
            this.Name = "FavoritesPropertiesForm";
            this.Text = "Bookmarks properties";
            ((System.ComponentModel.ISupportInitialize)(this._hoursBox)).EndInit();
            this.ResumeLayout(false);

        }
		#endregion

        private void _cancelButton_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void _okButton_Click(object sender, System.EventArgs e)
        {
            HashSet updatesResources = new HashSet();

            for( int i = 0; i < content.Count; ++i )
            {
                IResource webLink = content[ i ];
                if( webLink.Type != "Weblink" && webLink.Type != "Folder" )
                {
                    webLink = webLink.GetLinkProp( "Source" );
                }
                updatesResources.Add( webLink );
            }

            for( int i = 0; i < content.Count; ++i )
            {
                IResource webLink = content[ i ];
                if( webLink.Type != "Weblink" && webLink.Type != "Folder" )
                {
                    webLink = webLink.GetLinkProp( "Source" );
                }
                IResource parent = BookmarkService.GetParent( webLink );
                while( parent != null )
                {
                    if( content.IndexOf( parent ) >= 0 )
                    {
                        updatesResources.Remove( webLink );
                        break;
                    }
                    parent = BookmarkService.GetParent( parent );
                }
            }

            int updateFreq = ( _updateCheckBox.Checked ) ? ( (int) _hoursBox.Value * 60 * 60 ) : 0;
            int unitIndex = _unitBox.SelectedIndex;
            if( unitIndex > 0 )
            {
                updateFreq *= 24;
                if( unitIndex > 1 )
                {
                    updateFreq *= 7;
                }
            }

            foreach( HashSet.Entry E in updatesResources )
            {
                new ResourceProxy( (IResource) E.Key ).SetPropAsync( FavoritesPlugin._propUpdateFreq, updateFreq );
            }

            Close();
        }

        private void _updateCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            if( _hoursBox.Enabled = _updateCheckBox.Checked )
            {
                _fakeTextBox.Visible = false;
            }
            else if( _commonFreq <= 0 )
            {
                _fakeTextBox.Visible = true;
            }
        }

        internal static void RecursivelyUpdateResourceList( ref IResourceList list, IResource resource, bool stopOnFirst )
        {
            if( stopOnFirst && list.Count > 0 )
            {
                return;
            }
            if( resource.Type == "Weblink" )
            {
                list = list.Union( resource.ToResourceList(), true );
            }
            else if( resource.Type == "Folder" )
            {
                IResourceList childs = resource.GetLinksTo( null, "Parent" );
                foreach( IResource child in childs )
                {
                    RecursivelyUpdateResourceList( ref list, child, stopOnFirst );
                }
            }
            else
            {
                resource = resource.GetLinkProp( "Source" );
                if( resource != null && resource.Type == "Weblink" )
                {
                    list = list.Union( resource.ToResourceList(), true );
                }
            }
        }
	}
}
