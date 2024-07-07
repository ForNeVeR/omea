// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Database;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
    public class ResourcePropertiesDialog : DialogBase
    {
        private readonly Tracer _tracer = new Tracer( "DEBUG" );
        private Label label1;
        private TextBox _displayName;
        private TextBox _resourceType;
        private Label label2;
        private ListView _properties;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader3;
        private Panel panel1;
        private Panel panel2;
        private CheckBox _showLinks;
        private IResource _resource;
        private IResourceList _resourceList;
        private StatusBar _statusBar;
        private Button _btnCopy;
        private Button _btnClose;
        private Button _traceProps;
        private Button _refresh;
        private Splitter splitter1;
        private ListBox _traceBox;
        private ColumnHeader columnHeader4;
        private ContextMenu contextMenu1;
        private MenuItem menuItem1;
        private MenuItem menuItem2;
        private Button _btnDeleteResource;
        private Button _btnDeleteProperty;
        private Button _setPropButton;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public ResourcePropertiesDialog()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            RestoreSettings();
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
            this.label1 = new System.Windows.Forms.Label();
            this._displayName = new System.Windows.Forms.TextBox();
            this._resourceType = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this._properties = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.panel1 = new System.Windows.Forms.Panel();
            this._btnDeleteProperty = new System.Windows.Forms.Button();
            this._refresh = new System.Windows.Forms.Button();
            this._traceProps = new System.Windows.Forms.Button();
            this._btnDeleteResource = new System.Windows.Forms.Button();
            this._btnClose = new System.Windows.Forms.Button();
            this._showLinks = new System.Windows.Forms.CheckBox();
            this._btnCopy = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this._traceBox = new System.Windows.Forms.ListBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this._statusBar = new System.Windows.Forms.StatusBar();
            this._setPropButton = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Display name:";
            //
            // _displayName
            //
            this._displayName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._displayName.Location = new System.Drawing.Point(88, 8);
            this._displayName.Name = "_displayName";
            this._displayName.ReadOnly = true;
            this._displayName.Size = new System.Drawing.Size(392, 21);
            this._displayName.TabIndex = 1;
            this._displayName.Text = "";
            //
            // _resourceType
            //
            this._resourceType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._resourceType.Location = new System.Drawing.Point(88, 36);
            this._resourceType.Name = "_resourceType";
            this._resourceType.ReadOnly = true;
            this._resourceType.Size = new System.Drawing.Size(392, 21);
            this._resourceType.TabIndex = 3;
            this._resourceType.Text = "";
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "Resource type:";
            //
            // _properties
            //
            this._properties.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                          this.columnHeader1,
                                                                                          this.columnHeader2,
                                                                                          this.columnHeader4,
                                                                                          this.columnHeader3});
            this._properties.ContextMenu = this.contextMenu1;
            this._properties.Dock = System.Windows.Forms.DockStyle.Top;
            this._properties.FullRowSelect = true;
            this._properties.Location = new System.Drawing.Point(0, 0);
            this._properties.Name = "_properties";
            this._properties.Size = new System.Drawing.Size(592, 232);
            this._properties.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this._properties.TabIndex = 4;
            this._properties.View = System.Windows.Forms.View.Details;
            this._properties.DoubleClick += new System.EventHandler(this.OnDoubleClick);
            this._properties.SelectedIndexChanged += new System.EventHandler(this._properties_SelectedIndexChanged);
            //
            // columnHeader1
            //
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 92;
            //
            // columnHeader2
            //
            this.columnHeader2.Text = "Type";
            this.columnHeader2.Width = 107;
            //
            // columnHeader4
            //
            this.columnHeader4.Text = "Dir";
            this.columnHeader4.Width = 40;
            //
            // columnHeader3
            //
            this.columnHeader3.Text = "Value";
            this.columnHeader3.Width = 242;
            //
            // contextMenu1
            //
            this.contextMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                         this.menuItem1,
                                                                                         this.menuItem2});
            //
            // menuItem1
            //
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "Show Blob As Picture";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);
            //
            // menuItem2
            //
            this.menuItem2.Index = 1;
            this.menuItem2.Text = "Show BlobAs Text";
            this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);
            //
            // panel1
            //
            this.panel1.Controls.Add(this._setPropButton);
            this.panel1.Controls.Add(this._btnDeleteProperty);
            this.panel1.Controls.Add(this._refresh);
            this.panel1.Controls.Add(this._traceProps);
            this.panel1.Controls.Add(this._btnDeleteResource);
            this.panel1.Controls.Add(this._btnClose);
            this.panel1.Controls.Add(this._showLinks);
            this.panel1.Controls.Add(this._btnCopy);
            this.panel1.Controls.Add(this._resourceType);
            this.panel1.Controls.Add(this._displayName);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(592, 104);
            this.panel1.TabIndex = 5;
            //
            // _btnDeleteProperty
            //
            this._btnDeleteProperty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnDeleteProperty.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnDeleteProperty.Location = new System.Drawing.Point(368, 76);
            this._btnDeleteProperty.Name = "_btnDeleteProperty";
            this._btnDeleteProperty.Size = new System.Drawing.Size(108, 23);
            this._btnDeleteProperty.TabIndex = 10;
            this._btnDeleteProperty.Text = "Delete Property";
            this._btnDeleteProperty.Click += new System.EventHandler(this._btnDeleteProperty_Click);
            //
            // _refresh
            //
            this._refresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._refresh.Enabled = false;
            this._refresh.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._refresh.Location = new System.Drawing.Point(96, 76);
            this._refresh.Name = "_refresh";
            this._refresh.TabIndex = 9;
            this._refresh.Text = "Refresh";
            this._refresh.Click += new System.EventHandler(this.OnRefresh);
            //
            // _traceProps
            //
            this._traceProps.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._traceProps.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._traceProps.Location = new System.Drawing.Point(176, 76);
            this._traceProps.Name = "_traceProps";
            this._traceProps.TabIndex = 8;
            this._traceProps.Text = "Trace";
            this._traceProps.Click += new System.EventHandler(this.OnTrace);
            //
            // _btnDeleteResource
            //
            this._btnDeleteResource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnDeleteResource.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnDeleteResource.Location = new System.Drawing.Point(256, 76);
            this._btnDeleteResource.Name = "_btnDeleteResource";
            this._btnDeleteResource.Size = new System.Drawing.Size(108, 23);
            this._btnDeleteResource.TabIndex = 7;
            this._btnDeleteResource.Text = "Delete Resource";
            this._btnDeleteResource.Click += new System.EventHandler(this.OnDeleteResource);
            //
            // _btnClose
            //
            this._btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnClose.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnClose.Location = new System.Drawing.Point(500, 36);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(75, 24);
            this._btnClose.TabIndex = 6;
            this._btnClose.Text = "Close";
            this._btnClose.Click += new System.EventHandler(this.OnClose);
            //
            // _showLinks
            //
            this._showLinks.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._showLinks.Location = new System.Drawing.Point(8, 64);
            this._showLinks.Name = "_showLinks";
            this._showLinks.Size = new System.Drawing.Size(128, 16);
            this._showLinks.TabIndex = 5;
            this._showLinks.Text = "Show links:";
            this._showLinks.CheckedChanged += new System.EventHandler(this._showLinks_CheckedChanged);
            //
            // _btnCopy
            //
            this._btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCopy.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCopy.Location = new System.Drawing.Point(500, 8);
            this._btnCopy.Name = "_btnCopy";
            this._btnCopy.Size = new System.Drawing.Size(75, 24);
            this._btnCopy.TabIndex = 4;
            this._btnCopy.Text = "Copy";
            this._btnCopy.Click += new System.EventHandler(this._btnCopy_Click);
            //
            // panel2
            //
            this.panel2.Controls.Add(this._traceBox);
            this.panel2.Controls.Add(this.splitter1);
            this.panel2.Controls.Add(this._properties);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 104);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(592, 416);
            this.panel2.TabIndex = 6;
            //
            // _traceBox
            //
            this._traceBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._traceBox.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._traceBox.Location = new System.Drawing.Point(0, 235);
            this._traceBox.Name = "_traceBox";
            this._traceBox.Size = new System.Drawing.Size(592, 173);
            this._traceBox.TabIndex = 6;
            //
            // splitter1
            //
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 232);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(592, 3);
            this.splitter1.TabIndex = 5;
            this.splitter1.TabStop = false;
            //
            // _statusBar
            //
            this._statusBar.Location = new System.Drawing.Point(0, 520);
            this._statusBar.Name = "_statusBar";
            this._statusBar.Size = new System.Drawing.Size(592, 22);
            this._statusBar.TabIndex = 5;
            //
            // _setPropButton
            //
            this._setPropButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._setPropButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._setPropButton.Location = new System.Drawing.Point(480, 76);
            this._setPropButton.Name = "_setPropButton";
            this._setPropButton.Size = new System.Drawing.Size(96, 23);
            this._setPropButton.TabIndex = 11;
            this._setPropButton.Text = "Set Property";
            this._setPropButton.Click += new System.EventHandler(this._setPropButton_Click);
            //
            // ResourcePropertiesDialog
            //
            this.AcceptButton = this._btnClose;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnClose;
            this.ClientSize = new System.Drawing.Size(592, 542);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this._statusBar);
            this.MinimumSize = new System.Drawing.Size(544, 336);
            this.Name = "ResourcePropertiesDialog";
            this.ShowInTaskbar = true;
            this.Text = "Resource Properties";
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private ListViewItem AddListViewItem( string name, string dir, string type, string value )
        {
            ListViewItem item = new ListViewItem();
            item.Text = name;
            item.SubItems.Add( type );
            item.SubItems.Add( dir );
            item.SubItems.Add( value );
            _properties.Items.Add( item );
            return item;
        }

        public void SetResource( IResource resource )
        {
            _refresh.Enabled = false;
            if ( resource == null ) return;

            _properties.BeginUpdate();
            _properties.Items.Clear();
            _resource = resource;
            if ( _resourceList != null )
            {
                _resourceList.ResourceChanged -= _resourceList_ResourceChanged;
                _resourceList.Dispose();
            }
            _resourceList = _resource.ToResourceListLive();
            _resourceList.ResourceChanged += _resourceList_ResourceChanged;
            _displayName.Text = resource.DisplayName;
            _resourceType.Text = resource.Type;
            if ( resource.IsTransient )
            {
                _resourceType.ForeColor = Color.Blue;
                _resourceType.Text += " (Transient)";
            }

            int linksCount = 0;
            IPropertyCollection properties = resource.Properties;
            foreach ( IResourceProperty property in properties )
            {
                if ( property.DataType == PropDataType.Link )
                {
                    bool directed = ICore.Instance.ResourceStore.PropTypes[property.PropId].HasFlag( PropTypeFlags.DirectedLink );

                    IResourceList resources = null;
                    string linkType = string.Empty;

                    if ( !directed )
                    {
                        resources = resource.GetLinksOfType( null, property.Name );
                    }
                    else
                    {
                        if ( property.PropId < 0 )
                        {
                            resources = resource.GetLinksTo( null, property.Name );
                            linkType = "To This";
                        }
                        else
                        {
                            resources = resource.GetLinksFrom( null, property.Name );
                            linkType = "From This";
                        }
                    }
                    linksCount += resources.Count;
                    if ( _showLinks.Checked )
                    {
                        foreach ( IResource linkedResource in resources )
                        {
                            ListViewItem item = AddListViewItem( property.Name, linkType, property.DataType.ToString(),
                                linkedResource.Type + ":" + linkedResource );
                            item.Tag = linkedResource;
                        }
                    }
                }
                else
                {
                    string value = string.Empty;
                    if ( property.Value != null )
                    {
                        value = property.Value.ToString();
                    }
                    ListViewItem item = AddListViewItem( property.Name, "", property.DataType.ToString(), value );
                    item.Tag = property.Value;
                }
            }
            AddListViewItem( "ID", "", "int", resource.Id.ToString() );
            _properties.EndUpdate();
            _showLinks.Text = "Show Links: " + linksCount;
            if ( resource.Id == -1 )
            {
                Text += " ( resource deleted )";
            }
            else
            {
                Text += " ( id = '" + resource.Id + "' Type = '" + resource.Type + "' DN = '" + resource.DisplayName + "' ) ";
            }
        }

        private void OnDoubleClick(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection list = _properties.SelectedItems;
            foreach ( ListViewItem item in list )
            {
                IResource resource = item.Tag as IResource;
                if ( resource != null )
                {
                    ResourcePropertiesDialog dlg = new ResourcePropertiesDialog();
                    dlg.SetResource( resource );
                    dlg.Show();
                }
            }
        }

        private void _showLinks_CheckedChanged(object sender, EventArgs e)
        {
            SetResource( _resource );
        }

        private void _properties_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( _properties.SelectedItems.Count == 1 )
            {
                _statusBar.Text = "Selected value length: " + _properties.SelectedItems [0].SubItems [3].Text.Length;
                _btnCopy.Enabled = true;
                _btnDeleteProperty.Enabled = true;
            }
            else
            {
                _statusBar.Text = "";
                _btnCopy.Enabled = false;
                _btnDeleteProperty.Enabled = false;
            }
        }

        private void _btnCopy_Click(object sender, EventArgs e)
        {
            if ( _properties.SelectedItems.Count == 1 )
            {
                Clipboard.SetDataObject( _properties.SelectedItems [0].SubItems [3].Text );
            }
        }

        private void _btnDeleteProperty_Click( object sender, EventArgs e )
        {
            if ( _properties.SelectedItems.Count == 1 )
            {
                string propName = _properties.SelectedItems [0].Text;
                DialogResult result = MessageBox.Show( "Are you sure you want to delete property '" + propName + "'?",
                    "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question );
                if ( result == DialogResult.Yes )
                {
                    new ResourceProxy( _resource ).DeleteProp( propName );
                    SetResource( _resource );
                }
            }
        }

        private void OnClose(object sender, EventArgs e)
        {
            Close();
        }

        private void OnDeleteResource(object sender, EventArgs e)
        {
            DialogResult result =
                MessageBox.Show( "Are you sure you want to delete resource?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question );
            if ( result == DialogResult.Yes )
            {
                try
                {
                    ResourceTracer._Trace( _resource );
                    _tracer.Trace( "Resource " + _resource.Id + " to be deleted" );
                    new ResourceProxy( _resource ).DeleteAsync();
                }
                catch ( Exception exception )
                {
                    _tracer.TraceException( exception );
                    MessageBox.Show( "Can't delete resource: " + exception.Message );
                    return;
                }
                Close();
            }
        }

        private void OnTrace(object sender, EventArgs e)
        {
            ResourceTracer._Trace( _resource, _showLinks.Checked );
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            SetResource( _resource );
        }

        private void AddStringToTraceBox( string message )
        {
            _traceBox.Items.Add( message );
            _tracer.Trace( message );
        }

        private void _resourceList_ResourceChanged(object sender, ResourcePropIndexEventArgs e)
        {
            _refresh.Enabled = true;

            AddStringToTraceBox( DateTime.Now.ToLongTimeString() + " resource was changed." );
            foreach( int propID in e.ChangeSet.GetChangedProperties() )
            {
                string message = "\t" + Core.ResourceStore.PropTypes.GetPropDisplayName( propID );
                if ( Core.ResourceStore.PropTypes[propID].DataType != PropDataType.Link )
                {
                    message += "\t\t" + e.ChangeSet.GetOldValue( propID );
                    message += " --> " + e.Resource.GetProp( propID );
                }
                AddStringToTraceBox( message );
            }
        }

        private void menuItem1_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection list = _properties.SelectedItems;
            foreach ( ListViewItem item in list )
            {
                IBLOB blob = item.Tag as IBLOB;
                if ( blob != null )
                {
                    try
                    {
                        new ShowBlobAsPicture( blob.Stream ).Show();
                    }
                    catch{}
                }
            }
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection list = _properties.SelectedItems;
            foreach ( ListViewItem item in list )
            {
                IBLOB blob = item.Tag as IBLOB;
                if ( blob != null )
                {
                    try
                    {
                        new ShowBlobAsText( blob.Stream ).Show();
                    }
                    catch{}
                }
            }
        }

        private void _setPropButton_Click(object sender, EventArgs e)
        {
            string name = Core.UIManager.InputString( "Enter Property Name", "", "", null, this );
            if( !String.IsNullOrEmpty( name ) )
            {
                new ResourceProxy( _resource ).SetProp( name,
                    Core.UIManager.InputString( "Set Property Value", "", "", null, this ) );
            }
        }
    }
}
