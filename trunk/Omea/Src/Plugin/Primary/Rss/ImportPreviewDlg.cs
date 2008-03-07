/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Dialog for previewing the subscriptions imported from an OPML file.
	/// </summary>
	public class ImportPreviewDlg: DialogBase
	{
        private ResourceTreeView2 _tvFeeds;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox _edtDescription;
        private System.Windows.Forms.Label label1;
        private JetLinkLabel _lblHomepage;
        private System.Windows.Forms.Button _btnSelectAll;
        private System.Windows.Forms.Button _btnUnselectAll;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ImportPreviewDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
            this._tvFeeds = new JetBrains.Omea.GUIControls.ResourceTreeView2();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._edtDescription = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this._lblHomepage = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this._btnSelectAll = new System.Windows.Forms.Button();
            this._btnUnselectAll = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _tvFeeds
            // 
            this._tvFeeds.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._tvFeeds.CheckBoxes = true;
            this._tvFeeds.Location = new System.Drawing.Point(4, 4);
            this._tvFeeds.MultiSelect = false;
            this._tvFeeds.Name = "_tvFeeds";
            this._tvFeeds.ShowContextMenu = false;
            this._tvFeeds.Size = new System.Drawing.Size(280, 184);
            this._tvFeeds.TabIndex = 0;
            this._tvFeeds.ActiveResourceChanged += new EventHandler(this._tvFeeds_AfterSelect);
            this._tvFeeds.ResourceAdded += new ResourceEventHandler(this._tvFeeds_ResourceAdded);
            // 
            // _btnSelectAll
            // 
            this._btnSelectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnSelectAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnSelectAll.Location = new System.Drawing.Point(300, 4);
            this._btnSelectAll.Size = new System.Drawing.Size(80, 24);
            this._btnSelectAll.Name = "_btnSelectAll";
            this._btnSelectAll.TabIndex = 1;
            this._btnSelectAll.Text = "Select All";
            this._btnSelectAll.Click += new System.EventHandler(this._btnSelectAll_Click);
            // 
            // _btnUnselectAll
            // 
            this._btnUnselectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnUnselectAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnUnselectAll.Location = new System.Drawing.Point(300, 34);
            this._btnUnselectAll.Name = "_btnUnselectAll";
            this._btnUnselectAll.Size = new System.Drawing.Size(80, 24);
            this._btnUnselectAll.TabIndex = 2;
            this._btnUnselectAll.Text = "Unselect All";
            this._btnUnselectAll.Click += new System.EventHandler(this._btnUnselectAll_Click);
            // 
            // _btnOK
            // 
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(180, 340);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 5;
            this._btnOK.Text = "OK";
            // 
            // _btnCancel
            // 
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(264, 340);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 6;
            this._btnCancel.Text = "Cancel";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this._edtDescription);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox1.Location = new System.Drawing.Point(4, 224);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(340, 92);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Description";
            // 
            // _edtDescription
            // 
            this._edtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtDescription.Location = new System.Drawing.Point(8, 27);
            this._edtDescription.Multiline = true;
            this._edtDescription.Name = "_edtDescription";
            this._edtDescription.ReadOnly = true;
            this._edtDescription.Size = new System.Drawing.Size(324, 57);
            this._edtDescription.TabIndex = 0;
            this._edtDescription.Text = "";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 320);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 16);
            this.label1.TabIndex = 4;
            this.label1.Text = "Homepage:";
            // 
            // _lblHomepage
            // 
            this._lblHomepage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._lblHomepage.Cursor = System.Windows.Forms.Cursors.Hand;
            this._lblHomepage.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(70)), ((System.Byte)(70)), ((System.Byte)(211)));
            this._lblHomepage.Location = new System.Drawing.Point(108, 320);
            this._lblHomepage.Name = "_lblHomepage";
            this._lblHomepage.Size = new System.Drawing.Size(0, 0);
            this._lblHomepage.TabIndex = 12;
            this._lblHomepage.Click += new System.EventHandler(this._lblHomepage_Click);
            // 
            // ImportPreviewDlg
            // 
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(348, 371);
            this.Controls.Add(this._btnUnselectAll);
            this.Controls.Add(this._btnSelectAll);
            this.Controls.Add(this._lblHomepage);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._tvFeeds);
            this.Name = "ImportPreviewDlg";
            this.Text = "Subscription Import Preview";
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
        }

	    #endregion
    
        public void ShowImportPreview( IResource root )
        {
            Core.ResourceTreeManager.SetResourceNodeSort( root, "Type- Name" );
            _tvFeeds.RootResource = root;
            _tvFeeds.CheckedProperty = Props.Transient;
            _tvFeeds.CheckedSetValue = 0;
            _tvFeeds.CheckedUnsetValue = 1;
            _tvFeeds.ParentProperty = Core.Props.Parent;
            _tvFeeds.OpenProperty = Core.Props.Open;
        }

        private void _tvFeeds_AfterSelect( object sender, EventArgs e )
        {
            IResource feed = _tvFeeds.ActiveResource;
            if ( feed != null )
            {
                _edtDescription.Text = feed.GetStringProp( Props.Description );
                _lblHomepage.Text = feed.GetStringProp( Props.HomePage );
            }
            else
            {
                _edtDescription.Text = "";
                _lblHomepage.Text = "";
            }
        }

        private void _lblHomepage_Click( object sender, System.EventArgs e )
        {
            Core.UIManager.OpenInNewBrowserWindow( _lblHomepage.Text );
        }

        private void _tvFeeds_ResourceAdded( object sender, ResourceEventArgs e )
        {
            if ( e.Resource.Type == "RSSFeedGroup" )
            {
                _tvFeeds.SetNodeCheckState( e.Resource, CheckBoxState.Hidden );
            }
            else
            {
                _tvFeeds.SetNodeCheckState( e.Resource, CheckBoxState.Checked );
            }
        }

        private void _btnSelectAll_Click( object sender, System.EventArgs e )
        {
            _tvFeeds.ForEachNode( new ResourceDelegate( CheckResource ) );
        }

        private void _btnUnselectAll_Click( object sender, System.EventArgs e )
        {
            _tvFeeds.ForEachNode( new ResourceDelegate( UncheckResource ) );
        }

	    private void CheckResource( IResource res )
	    {
            if ( res.Type != "RSSFeedGroup" )
            {
                _tvFeeds.SetNodeCheckState( res, CheckBoxState.Checked );
            }
	    }

	    private void UncheckResource( IResource res )
	    {
            if ( res.Type != "RSSFeedGroup" )
            {
                _tvFeeds.SetNodeCheckState( res, CheckBoxState.Unchecked );
            }
        }
	}
}
