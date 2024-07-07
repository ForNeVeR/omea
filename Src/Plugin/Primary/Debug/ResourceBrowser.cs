// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.DebugPlugin
{
	public class ResourceBrowser : DialogBase
	{
        private ListBox _resourceTypes;
        private ListView _resourcesView;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private Button _btn_Close;
        private TextBox _resID;
        private Label label1;
        private Button _btn_ShowResource;
        private Panel panel1;
        private Splitter splitter1;
        private Panel panel2;
        private Label label2;
        private TextBox _count;
        private Button _btnQuery;

        private int _id = 0;
        private readonly IResourceStore _resourceStore;

		private System.ComponentModel.Container components = null;

		public ResourceBrowser( IResourceStore resourceStore )
		{
			InitializeComponent();

            RestoreSettings();
            _resourceStore = resourceStore;
            Populate();
            _count.Text = 0.ToString();
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

        class ResourceItem
        {
            readonly int _count;
            readonly IResource _resource;

            public ResourceItem( IResource resource, int count )
            {
                _resource = resource;
                _count = count;
            }
            public IResource Resource { get { return _resource; } }
            public override string ToString()
            {
                return _resource + " (" + _count + ")";
            }
        }
        private void Populate()
        {
            _resourceTypes.SuspendLayout();
            IResourceList resourceTypes = _resourceStore.GetAllResources( "ResourceType" );
            resourceTypes.Sort( new SortSettings( Core.Props.Name, true ) );
            foreach ( IResource resourceType in resourceTypes )
            {
                string name = resourceType.GetStringProp( Core.Props.Name );
                if( name != null )
                {
                    IResourceList resources = _resourceStore.GetAllResources( name );
                    _resourceTypes.Items.Add( new ResourceItem( resourceType, resources.Count ) );
                }
            }
            _resourceTypes.ResumeLayout();
        }

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._resourceTypes = new System.Windows.Forms.ListBox();
            this._resourcesView = new System.Windows.Forms.ListView();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this._btn_Close = new System.Windows.Forms.Button();
            this._resID = new System.Windows.Forms.TextBox();
            this._btn_ShowResource = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.label2 = new System.Windows.Forms.Label();
            this._count = new System.Windows.Forms.TextBox();
            this._btnQuery = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            //
            // _resourceTypes
            //
            this._resourceTypes.Dock = System.Windows.Forms.DockStyle.Left;
            this._resourceTypes.Location = new System.Drawing.Point(0, 0);
            this._resourceTypes.Name = "_resourceTypes";
            this._resourceTypes.Size = new System.Drawing.Size(128, 147);
            this._resourceTypes.Sorted = true;
            this._resourceTypes.TabIndex = 0;
            this._resourceTypes.SelectedIndexChanged += new System.EventHandler(this._resourceTypes_SelectedIndexChanged);
            //
            // _resourcesView
            //
            this._resourcesView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                             this.columnHeader2,
                                                                                             this.columnHeader1});
            this._resourcesView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._resourcesView.FullRowSelect = true;
            this._resourcesView.Location = new System.Drawing.Point(0, 0);
            this._resourcesView.MultiSelect = false;
            this._resourcesView.Name = "_resourcesView";
            this._resourcesView.Size = new System.Drawing.Size(449, 148);
            this._resourcesView.TabIndex = 1;
            this._resourcesView.View = System.Windows.Forms.View.Details;
            this._resourcesView.DoubleClick += new System.EventHandler(this._resourcesView_DoubleClick);
            //
            // columnHeader2
            //
            this.columnHeader2.Text = "ID";
            this.columnHeader2.Width = 71;
            //
            // columnHeader1
            //
            this.columnHeader1.Text = "DisplayName";
            this.columnHeader1.Width = 409;
            //
            // _btn_Close
            //
            this._btn_Close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btn_Close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btn_Close.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btn_Close.Location = new System.Drawing.Point(496, 162);
            this._btn_Close.Name = "_btn_Close";
            this._btn_Close.Size = new System.Drawing.Size(75, 25);
            this._btn_Close.TabIndex = 5;
            this._btn_Close.Text = "Close";
            this._btn_Close.Click += new System.EventHandler(this.OnClose);
            //
            // _resID
            //
            this._resID.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._resID.Location = new System.Drawing.Point(224, 162);
            this._resID.Name = "_resID";
            this._resID.Size = new System.Drawing.Size(60, 21);
            this._resID.TabIndex = 6;
            this._resID.Text = "";
            this._resID.KeyDown += new System.Windows.Forms.KeyEventHandler(this._resID_KeyDown);
            this._resID.TextChanged += new System.EventHandler(this.OnResourceIDChanged);
            //
            // _btn_ShowResource
            //
            this._btn_ShowResource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._btn_ShowResource.Enabled = false;
            this._btn_ShowResource.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btn_ShowResource.Location = new System.Drawing.Point(288, 162);
            this._btn_ShowResource.Name = "_btn_ShowResource";
            this._btn_ShowResource.Size = new System.Drawing.Size(92, 25);
            this._btn_ShowResource.TabIndex = 7;
            this._btn_ShowResource.Text = "Show Resource";
            this._btn_ShowResource.Click += new System.EventHandler(this.OnShowResource);
            //
            // label1
            //
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(148, 167);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 17);
            this.label1.TabIndex = 8;
            this.label1.Text = "Resource ID:";
            //
            // panel1
            //
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.splitter1);
            this.panel1.Controls.Add(this._resourceTypes);
            this.panel1.Location = new System.Drawing.Point(4, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(580, 148);
            this.panel1.TabIndex = 10;
            //
            // panel2
            //
            this.panel2.Controls.Add(this._resourcesView);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(131, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(449, 148);
            this.panel2.TabIndex = 2;
            //
            // splitter1
            //
            this.splitter1.Location = new System.Drawing.Point(128, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 148);
            this.splitter1.TabIndex = 1;
            this.splitter1.TabStop = false;
            //
            // label2
            //
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.Location = new System.Drawing.Point(12, 164);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(40, 16);
            this.label2.TabIndex = 11;
            this.label2.Text = "Count:";
            //
            // _count
            //
            this._count.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._count.Location = new System.Drawing.Point(56, 160);
            this._count.Name = "_count";
            this._count.ReadOnly = true;
            this._count.Size = new System.Drawing.Size(76, 21);
            this._count.TabIndex = 12;
            this._count.Text = "";
            //
            // _btnQuery
            //
            this._btnQuery.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._btnQuery.Location = new System.Drawing.Point(404, 164);
            this._btnQuery.Name = "_btnQuery";
            this._btnQuery.TabIndex = 13;
            this._btnQuery.Text = "Query";
            this._btnQuery.Click += new System.EventHandler(this.OnQuery);
            //
            // ResourceBrowser
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btn_Close;
            this.ClientSize = new System.Drawing.Size(580, 194);
            this.Controls.Add(this._btnQuery);
            this.Controls.Add(this._count);
            this.Controls.Add(this._resID);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._btn_ShowResource);
            this.Controls.Add(this._btn_Close);
            this.MinimumSize = new System.Drawing.Size(588, 228);
            this.Name = "ResourceBrowser";
            this.ShowInTaskbar = true;
            this.Text = "ResourceBrowser";
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        private void _resourceTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            IResource resourceType = ((ResourceItem)_resourceTypes.SelectedItem).Resource;
            LoadResourceList( _resourceStore.GetAllResources( resourceType.GetStringProp("Name") ) );
        }

        void LoadResourceList( IResourceList resourceList )
        {
            _resourcesView.SuspendLayout();
            _resourcesView.Items.Clear();
            resourceList.Sort( new SortSettings( ResourceProps.Id, true ) );
            foreach ( IResource resource in resourceList )
            {
                ListViewItem item = new ListViewItem( resource.Id.ToString() );
                item.Tag = resource;
                item.SubItems.Add( resource.DisplayName );
                _resourcesView.Items.Add( item );
            }
            _count.Text = _resourcesView.Items.Count.ToString();
            _resourcesView.ResumeLayout();
        }

        private static void ShowResource( IResource resource )
        {
            try
            {
                ResourcePropertiesDialog dlg = new ResourcePropertiesDialog();
                dlg.SetResource( resource );
                dlg.Show();
            }
            catch ( Exception exception )
            {
                MessageBox.Show( exception.Message );
            }
        }

        private void _resourcesView_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                ListView.SelectedListViewItemCollection items = _resourcesView.SelectedItems;
                foreach ( ListViewItem item in items )
                {
                    if ( item.Tag != null )
                    {
                        ShowResource( (IResource)item.Tag );
                    }

                }
            }
            catch ( Exception exception )
            {
                MessageBox.Show( exception.Message );
            }
        }

        private void OnShowResource(object sender, EventArgs e)
        {
            try
            {
                IResource resource = _resourceStore.LoadResource( _id );
                ShowResource( resource );
            }
            catch ( Exception exception )
            {
                MessageBox.Show( exception.Message );
            }

        }

        private void OnResourceIDChanged(object sender, EventArgs e)
        {
            string text = _resID.Text;
            _id = -1;
            try
            {
                _id = Int32.Parse( text );
            }
            catch ( Exception )
            {
            }

            _btn_ShowResource.Enabled = ( _id > -1 );
        }

        private void _resID_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyCode == Keys.Enter )
            {
                e.Handled = true;
                _btn_ShowResource.PerformClick();
            }
        }

        private void OnClose(object sender, EventArgs e)
        {
            Close();
        }

        private void OnQuery(object sender, EventArgs e)
        {
            QueryForm form = new QueryForm();
            using ( form )
            {
                if ( form.ShowDialog() == System.Windows.Forms.DialogResult.OK )
                {
                    if ( form.PropName != null )
                    {
                        LoadResourceList( Core.ResourceStore.FindResourcesWithProp( null, form.PropName ) );
                    }
                }
            }
        }
	}
}
