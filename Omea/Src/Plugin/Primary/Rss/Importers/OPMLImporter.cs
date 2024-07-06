// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
    /// <summary>
    /// Summary description for BloglinesImporter.
    /// </summary>
    internal class OPMLImporterPane : AbstractOptionsPane
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.OpenFileDialog _odOPML;
        private System.Windows.Forms.Button _btnAdd;
        private System.Windows.Forms.Button _btnDel;
        private System.Windows.Forms.ListBox _lstFiles;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button _btnAddURL;
        private System.Windows.Forms.CheckBox _chkPreview;

        private OPMLImporter _importer = null;
        private ImportManager _manager = null;
        private IResource _importRoot = null;
        private AbstractWizardPane _previewPane = null;

        private abstract class PathItem
        {
            private Control _owner = null;
            private string _name = null;

            private string _shortName = null;
            private int _lastWidth = -1;

            internal string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            internal PathItem( System.Windows.Forms.Control owner, string name )
            {
                _owner = owner;
                _name = name;
            }

            internal abstract string TrimName( System.Windows.Forms.Control owner, string name, int width );

            public override string ToString()
            {
                if( _shortName == null || _owner.Width != _lastWidth )
                {
                    _shortName = TrimName( _owner, _name, _owner.Width );
                    _lastWidth = _owner.Width;
                }
                return _shortName;
            }
        }

        private class FileItem : PathItem
        {
            internal FileItem( System.Windows.Forms.Control owner, string name ) : base( owner, name )
            {
            }

            internal override string TrimName( System.Windows.Forms.Control owner, string name, int width )
            {
                string shortName = "";
                // check width.
                using( Graphics g = Graphics.FromHwnd( owner.Handle ) )
                {
                    // Simple check
                    if( g.MeasureString( name, owner.Font ).Width < width )
                    {
                        return name;
                    }
                    else
                    { // And trim path.
                        string[] path = name.Split( '\\' );
                        int lastComponent = path.Length - 1;

                        shortName = path[lastComponent];
                        string p0 = path[0] + "\\...";
                        while( g.MeasureString( p0 + "\\" + path[lastComponent] + "\\" + shortName, owner.Font ).Width < width &&
                            lastComponent > 1 )
                        {
                            shortName = path[lastComponent] + "\\" + shortName;
                            --lastComponent;
                        }
                        shortName = p0 + "\\" + shortName;
                    }
                }
                return shortName;
            }
        }

        private class URLItem : PathItem
        {
            internal URLItem( System.Windows.Forms.Control owner, string name ) : base( owner, name )
            {
            }

            internal override string TrimName( System.Windows.Forms.Control owner, string name, int width )
            {
                string shortName = "";
                // check width.
                using( Graphics g = Graphics.FromHwnd( owner.Handle ) )
                {
                    // Simple check
                    if( g.MeasureString( name, owner.Font ).Width < width )
                    {
                        return name;
                    }
                    else
                    { // And trim path.
                        string[] path = name.Split( '/' );
                        int lastComponent = path.Length - 1;

                        shortName = path[lastComponent];
                        string p0 = path[0] + "//" + path[2] + "/...";
                        while( g.MeasureString( p0 + "/" + path[lastComponent] + "/" + shortName, owner.Font ).Width < width &&
                            lastComponent > 2 )
                        {
                            shortName = path[lastComponent] + "/" + shortName;
                            --lastComponent;
                        }
                        shortName = p0 + "/" + shortName;
                    }
                }
                return shortName;
            }
        }

        internal OPMLImporterPane(  OPMLImporter importer ) : this( importer, null, null )
        {
        }

        internal OPMLImporterPane( OPMLImporter importer, ImportManager manager, IResource importRoot )
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            _importer = importer;
            _manager = manager;
            _importRoot = importRoot;
            _lstFiles.SizeChanged +=new EventHandler(_lstFiles_SizeChanged);
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

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._btnAdd = new System.Windows.Forms.Button();
            this._odOPML = new System.Windows.Forms.OpenFileDialog();
            this._btnDel = new System.Windows.Forms.Button();
            this._lstFiles = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this._btnAddURL = new System.Windows.Forms.Button();
            this._chkPreview = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            //
            // _btnAdd
            //
            this._btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnAdd.Location = new System.Drawing.Point(160, 288);
            this._btnAdd.Name = "_btnAdd";
            this._btnAdd.Size = new System.Drawing.Size(72, 23);
            this._btnAdd.TabIndex = 1;
            this._btnAdd.Text = "Add &File...";
            this._btnAdd.Click += new System.EventHandler(this._btnAdd_Click);
            //
            // _odOPML
            //
            this._odOPML.DefaultExt = "opml";
            this._odOPML.Filter = "OPML files|*.opml|XML files|*.xml|All files|*.*";
            this._odOPML.Multiselect = true;
            this._odOPML.Title = "Select OPML files to import";
            //
            // _btnDel
            //
            this._btnDel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnDel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnDel.Location = new System.Drawing.Point(319, 288);
            this._btnDel.Name = "_btnDel";
            this._btnDel.Size = new System.Drawing.Size(72, 23);
            this._btnDel.TabIndex = 3;
            this._btnDel.Text = "&Remove";
            this._btnDel.Click += new System.EventHandler(this._btnDel_Click);
            //
            // _lstFiles
            //
            this._lstFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lstFiles.ItemHeight = 17;
            this._lstFiles.Location = new System.Drawing.Point(8, 32);
            this._lstFiles.Name = "_lstFiles";
            this._lstFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this._lstFiles.Size = new System.Drawing.Size(384, 212);
            this._lstFiles.TabIndex = 4;
            this._lstFiles.SelectedIndexChanged += new System.EventHandler(this._lstFiles_SelectedIndexChanged);
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.TabIndex = 5;
            this.label1.Text = "Files to import:";
            //
            // _btnAddURL
            //
            this._btnAddURL.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnAddURL.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnAddURL.Location = new System.Drawing.Point(238, 288);
            this._btnAddURL.Name = "_btnAddURL";
            this._btnAddURL.TabIndex = 6;
            this._btnAddURL.Text = "Add &URL...";
            this._btnAddURL.Click += new System.EventHandler(this._btnAddURL_Click);
            //
            // _chkPreview
            //
            this._chkPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._chkPreview.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkPreview.Location = new System.Drawing.Point(8, 258);
            this._chkPreview.Name = "_chkPreview";
            this._chkPreview.Size = new System.Drawing.Size(152, 24);
            this._chkPreview.TabIndex = 7;
            this._chkPreview.Text = "&Preview subscription";
            this._chkPreview.CheckedChanged += new System.EventHandler(this._chkPreview_CheckedChanged);
            //
            // OPMLImporterPane
            //
            this.Controls.Add(this._chkPreview);
            this.Controls.Add(this._btnAddURL);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._lstFiles);
            this.Controls.Add(this._btnDel);
            this.Controls.Add(this._btnAdd);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "OPMLImporterPane";
            this.Size = new System.Drawing.Size(400, 320);
            this.ResumeLayout(false);

        }
        #endregion

        public override void ShowPane()
        {
            if( _manager == null )
            {
                _chkPreview.Hide();
            }
            _btnDel_enable();
        }

        public override void LeavePane()
        {
            string[] names = new string[_lstFiles.Items.Count];
            for( int i = 0; i < _lstFiles.Items.Count; ++i )
            {
                names[i] = ((PathItem)_lstFiles.Items[i]).Name;
            }
            _importer.FileNames = names;
        }

        private void _btnAdd_Click(object sender, System.EventArgs e)
        {
            DialogResult dr = _odOPML.ShowDialog( this );
            if( dr == DialogResult.OK )
            {
                foreach( string name in _odOPML.FileNames )
                {
                    bool found = false;
                    for( int i = 0; i < _lstFiles.Items.Count && ! found ; ++i )
                    {
                        PathItem item = (PathItem)_lstFiles.Items[i];
                        found = item.Name == name;
                    }
                    if( ! found )
                    {
                        _lstFiles.Items.Add( new FileItem( _lstFiles, name ) );
                    }
                }
            }
            _btnDel_enable();
        }

        private void _btnDel_Click(object sender, System.EventArgs e)
        {
            object[] selected = new object[ _lstFiles.SelectedIndices.Count ];
            for( int i = 0; i < _lstFiles.SelectedIndices.Count; ++i )
            {
                selected[i] = _lstFiles.Items[i];
            }
            for( int i = 0; i < selected.Length; ++i )
            {
                _lstFiles.Items.Remove( selected[ i ] );
            }
            _btnDel_enable();
        }

        private void _btnDel_enable()
        {
            if( _lstFiles.Items.Count > 0 && _lstFiles.SelectedIndices.Count > 0 )
            {
                _btnDel.Enabled = true;
            }
            else
            {
                _btnDel.Enabled = false;
            }
        }

        private void _lstFiles_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            _btnDel_enable();
        }

        private void _lstFiles_SizeChanged(object sender, EventArgs e)
        {
            object[] tmp = new object[ _lstFiles.Items.Count ];
            _lstFiles.BeginUpdate();
            _lstFiles.Items.CopyTo( tmp, 0 );
            _lstFiles.Items.Clear();
            _lstFiles.Items.AddRange( tmp );
            _lstFiles.EndUpdate();
        }

        private void _btnAddURL_Click(object sender, System.EventArgs e)
        {
            string url = Core.UIManager.InputString( "Import OPML File from URL",
                "Enter the URL of the OPML file:", "", null, this );
            if( url == null || url.Length == 0 )
            {
                return;
            }
            // Fix url with "http://"
            if( url.IndexOf( "://" ) == -1 )
            {
                url = "http://" + url;
            }
            url = url.Trim();

            Uri uri = null;
            try
            {
                uri = new Uri( url );
            }
            catch( UriFormatException )
            {
                MessageBox.Show( this, "The format of the URL is not valid:\n" + url +
                    "\nPlease enter the URL again and verify that it is correct.",
                    "Add URL", MessageBoxButtons.OK );
                return;
            }
            if( null != uri )
            {
                _lstFiles.Items.Add( new URLItem( _lstFiles, uri.ToString() ) );
            }
        }

        private void _chkPreview_CheckedChanged(object sender, System.EventArgs e)
        {
            if( _manager == null )
            {
                return;
            }
            AddPreviewPane( _chkPreview.Enabled && _chkPreview.Checked );
        }

        private void AddPreviewPane( bool add )
        {
            if( _manager == null )
            {
                return;
            }
            if( add )
            {
                if( _previewPane == null )
                {
                    _previewPane = new PreviewSubscriptionsPaneAdapter( "Preview subscriptions", new OptionsPaneCreator( CreatePreviewPane ) );
                }
                _manager.Wizard.RegisterPane( 0xFFFF, _previewPane );
            }
            else
            {
                if( _previewPane != null )
                {
                    _manager.Wizard.DeregisterPane( _previewPane );
                }
            }
        }

        private AbstractOptionsPane CreatePreviewPane()
        {
            return new PreviewSubscriptionsPane( _manager, _importRoot );
        }
    }

    internal class OPMLImporter : IFeedImporter
    {
        private const string _progressMessage = "Importing ";

        private string[] _fileNames = null;
        private ImportManager _manager = null;
        private IResource _importRoot = null;

        internal string[] FileNames
        {
            get { return _fileNames; }
            set { _fileNames = value; }
        }

        internal OPMLImporter() : this( null, null )
        {
        }

        internal OPMLImporter( ImportManager manager, IResource importRoot )
        {
            if( manager == null )
            {
                RSSPlugin.GetInstance().RegisterFeedImporter( "OPML Files", this );
            }
            else
            {
                _manager = manager;
                _importRoot = importRoot;
            }
        }

        #region IFeedImporter implementation
        /// <summary>
        /// Check if importer needs configuration before import starts.
        /// </summary>
        public bool HasSettings
        {
            get { return true; }
        }

        /// <summary>
        /// Returns creator of options pane.
        /// </summary>
        public OptionsPaneCreator GetSettingsPaneCreator()
        {
            return new OptionsPaneCreator( this.CreateOptionPane );
        }

        /// <summary>
        /// Import subscription
        /// </summary>
        public void DoImport( IResource importRoot, bool addToWorkspace )
        {
            if( null == FileNames )
            {
                return;
            }

            int totalFeeds = Math.Max( FileNames.Length, 1 );
            int processedFeeds = 0;
            ImportUtils.UpdateProgress( processedFeeds / totalFeeds, _progressMessage );
            IResource currentRoot = null;

            foreach( string fileName in FileNames )
            {
                string defaultName = null;
                Stream opml = null;

                if( ! File.Exists( fileName ) )
                {
                    defaultName = fileName;
                    // Try to load as URL
                    try
                    {
                        opml = new JetMemoryStream( new WebClient().DownloadData( fileName ), true );
                    }
                    catch( Exception ex )
                    {
                        Trace.WriteLine( "OPML file '" + fileName + "' can not be load: '" + ex.Message + "'" );
                        opml = null;
                    }
                }
                else
                {
                    defaultName = Path.GetFileName( fileName );
                    // Try to load title from this file
                    try
                    {
                        opml = new FileStream( fileName, FileMode.Open, FileAccess.Read );
                    }
                    catch( Exception ex )
                    {
                        Trace.WriteLine( "OPML file '" + fileName + "' can not be load: '" + ex.Message + "'" );
                        opml = null;
                    }
                }

                if( null == opml )
                {
                    continue;
                }

                // Try to get name
                string name = null;
                try
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load( opml );
                    XmlElement title = xml.SelectSingleNode( "/opml/head/title" ) as XmlElement;
                    name = title.InnerText;
                }
                catch( Exception ex )
                {
                    Trace.WriteLine( "OPML file '" + fileName + "' doesn't have title: '" + ex.Message + "'" );
                }
                if( name == null || name.Length == 0 )
                {
                    name = defaultName;
                }

                try
                {
                    opml.Seek( 0, SeekOrigin.Begin );
                    if( _manager == null || FileNames.Length > 1 )
                    {
                        currentRoot = RSSPlugin.GetInstance().FindOrCreateGroup( "Subscription from " + name , importRoot );
                    }
                    else
                    {
                        currentRoot = importRoot;
                    }
                    OPMLProcessor.Import( new StreamReader(opml), currentRoot, addToWorkspace );
                }
                catch( Exception ex )
                {
                    Trace.WriteLine( "OPML file '" + fileName + "' can not be load: '" + ex.Message + "'" );
                    RemoveFeedsAndGroupsAction.DeleteFeedGroup( currentRoot );
                    ImportUtils.ReportError( "OPML File Import", "Import of OPML file '" + fileName + "' failed:\n" + ex.Message );
                }

                processedFeeds += 100;
                ImportUtils.UpdateProgress( processedFeeds / totalFeeds, _progressMessage );
            }
            return;
        }

        /// <summary>
        /// Import cached items, flags, etc.
        /// </summary>
        public void DoImportCache()
        {
            // do nothing
        }
        #endregion

        private AbstractOptionsPane CreateOptionPane()
        {
            return new OPMLImporterPane( this, _manager, _importRoot );
        }

    }
}
