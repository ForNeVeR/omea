/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.GUIControls;
using Microsoft.Win32;

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;

namespace JetBrains.Omea.Plugins
{
    public class PluginsConfigPane : AbstractOptionsPane
    {
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.OpenFileDialog LocationDialog;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private RegistryKey _pluginsHKCUKey;
        private RegistryKey _pluginsHKLMKey;
        private RegistryKey _pluginsConfigKey;
        private System.Windows.Forms.Button _enableAllBtn;
        private System.Windows.Forms.ListView _pluginsList;
        private JetLinkLabel _promptLabel;
        private JetLinkLabel _downloadLabel;

        private Label _author;
        private Label _description;
        private Label _authorInfo;
        private TextBox _descriptionInfo;

        private System.Windows.Forms.Button _newBtn;
        private HashSet _activePlugins = new HashSet();

        public static AbstractOptionsPane PluginsConfigPaneCreator()
        {
            return new PluginsConfigPane();
        }

        private PluginsConfigPane()
        {
            InitializeComponent();
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
            this._pluginsList = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.LocationDialog = new System.Windows.Forms.OpenFileDialog();
            this._enableAllBtn = new System.Windows.Forms.Button();
            this._promptLabel = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this._downloadLabel = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this._newBtn = new System.Windows.Forms.Button();

            _author = new Label();
            _description = new Label();
            _authorInfo = new Label();
            _descriptionInfo = new TextBox();

            this.SuspendLayout();
            // 
            // _pluginsList
            // 
            this._pluginsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._pluginsList.CheckBoxes = true;
            this._pluginsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.columnHeader1, this.columnHeader2, this.columnHeader3});
            this._pluginsList.FullRowSelect = true;
            this._pluginsList.HideSelection = false;
            this._pluginsList.Location = new System.Drawing.Point(0, 24);
            this._pluginsList.MultiSelect = false;
            this._pluginsList.Name = "_pluginsList";
            this._pluginsList.Size = new System.Drawing.Size(376, 170);
            this._pluginsList.TabIndex = 0;
            this._pluginsList.View = System.Windows.Forms.View.Details;
            this._pluginsList.Resize += new System.EventHandler(this._pluginsList_Resize);
            this._pluginsList.SelectedIndexChanged += new EventHandler(_pluginsList_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Enabled";
            this.columnHeader1.Width = 55;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Plugin";
            this.columnHeader2.Width = 88;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Location";
            this.columnHeader3.Width = 229;
            // 
            // LocationDialog
            // 
            this.LocationDialog.DefaultExt = "dll";
            this.LocationDialog.Filter = "Omnia Mea plugins|*.dll";
            // 
            // _enableAllBtn
            // 
            this._enableAllBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._enableAllBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._enableAllBtn.Location = new System.Drawing.Point(384, 56);
            this._enableAllBtn.Name = "_enableAllBtn";
            this._enableAllBtn.TabIndex = 3;
            this._enableAllBtn.Text = "Enable All";
            this._enableAllBtn.Click += new System.EventHandler(this._enableAllBtn_Click);
            // 
            // _promptLabel
            // 
            this._promptLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._promptLabel.AutoSize = false;
            this._promptLabel.ClickableLink = false;
            this._promptLabel.Cursor = System.Windows.Forms.Cursors.Default;
            this._promptLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            this._promptLabel.Location = new System.Drawing.Point(0, 0);
            this._promptLabel.Name = "_promptLabel";
            this._promptLabel.Size = new System.Drawing.Size(464, 23);
            this._promptLabel.TabIndex = 3;
            this._promptLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // _downloadLabel
            //
            this._downloadLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left))));
            this._downloadLabel.AutoSize = true;
            this._downloadLabel.Location = new System.Drawing.Point(0, 340);
            this._downloadLabel.Name = "_downloadLabel";
            this._downloadLabel.Size = new System.Drawing.Size(300, 20);
            this._downloadLabel.TabIndex = 3;
            this._downloadLabel.Text = "Download More Plugins";
            this._downloadLabel.Click += new EventHandler( HandleDownloadLabelClick );
            // 
            // _newBtn
            // 
            this._newBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._newBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._newBtn.Location = new System.Drawing.Point(384, 24);
            this._newBtn.Name = "_newBtn";
            this._newBtn.TabIndex = 1;
            this._newBtn.Text = "Add...";
            this._newBtn.Click += new System.EventHandler(this._newBtn_Click);
            //
            // _author
            //
            this._author.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._author.Location = new System.Drawing.Point(0, 205);
            this._author.Name = "_author";
            this._author.Size = new System.Drawing.Size(60, 20);
            this._author.Text = "Author:";
            this._author.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left))));
            //
            // _authorInfo
            //
            this._authorInfo.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._authorInfo.Location = new System.Drawing.Point(80, 205);
            this._authorInfo.Name = "_authorInfo";
            this._authorInfo.Size = new System.Drawing.Size(300, 20);
            this._authorInfo.TabIndex = 4;
            this._authorInfo.Text = "Unknown";
            this._authorInfo.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
            //
            // _description
            //
            this._description.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._description.Location = new System.Drawing.Point(0, 230);
            this._description.Name = "_description";
            this._description.Size = new System.Drawing.Size(80, 20);
            this._description.Text = "Description:";
            this._description.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left))));
            //
            // _descriptionInfo
            //
            this._descriptionInfo.Location = new System.Drawing.Point(80, 230);
            this._descriptionInfo.Name = "_descriptionInfo";
            this._descriptionInfo.Size = new System.Drawing.Size(390, 100);
            this._descriptionInfo.TabIndex = 5;
            this._descriptionInfo.Text = "";
            this._descriptionInfo.Multiline = true;
            this._descriptionInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left))));
            // 
            // PluginsConfigPane
            // 
            this.Controls.Add(this._author);
            this.Controls.Add(this._authorInfo);
            this.Controls.Add(this._description);
            this.Controls.Add(this._descriptionInfo);
            this.Controls.Add(this._newBtn);
            this.Controls.Add(this._promptLabel);
            this.Controls.Add(this._enableAllBtn);
            this.Controls.Add(this._pluginsList);
            this.Controls.Add(this._downloadLabel);
            this.Name = "PluginsConfigPane";
            this.Size = new System.Drawing.Size(464, 361);
            this.ResumeLayout(false);

        }

        #endregion


        public override void ShowPane()
        {
            this.LocationDialog.Filter = Core.ProductName + " plugins|*.dll";

            _promptLabel.Text = "Changes will take effect after the restart of " + Core.ProductName;
            _pluginsHKCUKey = _pluginsHKLMKey = _pluginsConfigKey = null;
            try 
            {
                _pluginsHKCUKey = Registry.CurrentUser.OpenSubKey( (Core.PluginLoader as Loader).PluginRegistryKey, true );
            }
            catch {}
            try 
            {
                _pluginsConfigKey = _pluginsHKCUKey.OpenSubKey( Loader._configKey, true );
            }
            catch {}
            try
            {
                _pluginsHKLMKey = Registry.LocalMachine.OpenSubKey( (Core.PluginLoader as Loader).PluginRegistryKey, false );
            }
            catch {}
            if( _pluginsHKCUKey == null )
            {
                _pluginsHKCUKey = Registry.CurrentUser.CreateSubKey( (Core.PluginLoader as Loader).PluginRegistryKey );
            }
            if( _pluginsConfigKey == null )
            {
                _pluginsConfigKey = _pluginsHKCUKey.CreateSubKey( Loader._configKey );
            }

            _activePlugins.Clear();

            string[] disabledPlugins = 
                ( (string) _pluginsConfigKey.GetValue( Loader._disabledValue, string.Empty ) ).Split( ';' );
            Array.Sort( disabledPlugins );

            EnumPlugins( _pluginsHKCUKey, disabledPlugins );
            if( _pluginsHKLMKey != null )
            {
                EnumPlugins( _pluginsHKLMKey, disabledPlugins );
            }
        }

        public override void OK()
        {
            string disabled = string.Empty;
            foreach( ListViewItem item in _pluginsList.Items )
            {
                string plugin = item.SubItems[ 1 ].Text;
                string path = item.SubItems[ 2 ].Text;

                _pluginsHKCUKey.SetValue( plugin, path );
                if( !item.Checked )
                {
                    if( disabled != string.Empty )
                        disabled += ';';

                    disabled += plugin;
                }
                else
                {
                    ///////////////////////////////////////////////////////////////////////////////
                    ///////////////////////////////////////////////////////////////////////////////
                    // if plugin registration could ever be executed in the resource thread
                    // the following two lines can be uncommented
                    /*if( !_activePlugins.Contains( plugin ) )
                        PluginEnvironment.PluginLoader.LoadSinglePlugin( path, PluginEnvironment );*/
                }
            }

            object obj = _pluginsConfigKey.GetValue( Loader._disabledValue, string.Empty );
            if( !obj.Equals( disabled ) )
            {
                _pluginsConfigKey.SetValue( Loader._disabledValue, disabled );
                NeedRestart = true;
            }
        }

        private void _enableAllBtn_Click(object sender, System.EventArgs e)
        {
            foreach( ListViewItem item in _pluginsList.Items )
            {
                item.Checked = true;
            }
        }

        private void _newBtn_Click(object sender, System.EventArgs e)
        {
            while( LocationDialog.ShowDialog() == DialogResult.OK )
            {
                string filename = LocationDialog.FileName;
                if( Loader.IsOmniaMeaPlugin( filename ) )
                {
                    bool isAdded = false;
                    for( int i = 0; i < _pluginsList.Items.Count; ++i )
                    {
                        if( _pluginsList.Items[ i ].SubItems[ 2 ].Text == filename )
                        {
                            isAdded = true;
                            break;
                        }
                    }
                    if( isAdded )
                    {
                        MessageBox.Show( this, "Plugin '" + filename + "' is already added.", 
                            "Plugin error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                        continue;
                    }
                    AddPlugin( filename );
                    break;
                }
                MessageBox.Show( this, "'" + filename + "' is not a plugin for " + 
                    Core.ProductName + ".", "Plugin error", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
            _pluginsList.Focus();
        }

        private void _pluginsList_Resize(object sender, System.EventArgs e)
        {
            int twoColswidth = columnHeader1.Width + columnHeader2.Width + 4;
            if( _pluginsList.Width > twoColswidth )
            {
                columnHeader3.Width = _pluginsList.Width - twoColswidth;
            }
        }

        private void AddPlugin( string filename )
        {
            string pluginName = Path.GetFileName( filename ).Split( '.' )[ 0 ];
            for( ; ; )
            {
                bool isUnique = true;
                for( int i = 0; i < _pluginsList.Items.Count; ++i )
                {
                    if( _pluginsList.Items[ i ].SubItems[ 1 ].Text == pluginName )
                    {
                        isUnique = false;
                        break;
                    }
                }
                if( isUnique )
                {
                    break;
                }
                pluginName = pluginName + '_';
            }
            RegistryKey PluginsKey = null;
            try
            {
                PluginsKey = Registry.CurrentUser.OpenSubKey( (Core.PluginLoader as Loader).PluginRegistryKey, true );
            }
            catch( Exception e )
            {
                Trace.WriteLine( e.ToString(), "Plugins.Loader" );
            }
            if( PluginsKey != null )
            {
                PluginsKey.SetValue( pluginName, filename );
                ListViewItem item = new ListViewItem();
                item.SubItems.Add( pluginName );
                item.SubItems.Add( filename );
                item.Checked = true;
                _activePlugins.Add( pluginName );
                _pluginsList.Items.Add( item );
                NeedRestart = true;
            }
        }

        public override string GetHelpKeyword()
        {
            return "/reference/plugins.html";
        }

        private void EnumPlugins( RegistryKey key, string[] disabledPlugins )
        {
            string[] pluginNames = key.GetValueNames();
            Array.Sort( pluginNames );
            foreach( string pluginName in pluginNames )
            {
                object pluginPath = key.GetValue( pluginName );
                if( pluginPath is string )
                {
                    string filename = pluginPath as string;
                    bool isAdded = false;
                    for( int i = 0; i < _pluginsList.Items.Count; ++i )
                    {
                        if( _pluginsList.Items[ i ].SubItems[ 2 ].Text == filename )
                        {
                            isAdded = true;
                            break;
                        }
                    }
                    if( !isAdded )
                    {
                        ListViewItem item = new ListViewItem();
                        item.SubItems.Add( pluginName );
                        item.SubItems.Add( (string) pluginPath );
                        item.Checked = Array.BinarySearch( disabledPlugins, pluginName ) < 0;
                        if( item.Checked )
                        {
                            _activePlugins.Add( pluginName );
                        }
                        _pluginsList.Items.Add( item );
                    }
                }
            }
        }

        private void HandleDownloadLabelClick( object sender, EventArgs e )
        {
            Core.UIManager.OpenInNewBrowserWindow( "http://www.jetbrains.com/omea/plugins/" );
        }

        private void _pluginsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            string author, description;
            foreach( ListViewItem item in _pluginsList.SelectedItems )
            {
                string path = item.SubItems[ 2 ].Text;
                Core.PluginLoader.GetPluginDescription( path, out author, out description );
                _authorInfo.Text = (author != null) ? author : "Unknown";
                _descriptionInfo.Text = (description != null) ? description : string.Empty;
            }
        }
    }
}
