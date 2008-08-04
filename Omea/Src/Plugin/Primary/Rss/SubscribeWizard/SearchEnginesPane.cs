/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin.SubscribeWizard
{
    /// <summary>
    /// Summary description for FeedAddressPane.
    /// </summary>
    public class SearchEnginesPane : UserControl
    {
        private Label   label1, _lblTitle;
        private Label   _lblError;
        private Label   _lblProgress, _lblEngineNameInProgress;
        private JetTextBox _edtSearchQuery;
        private System.Windows.Forms.Button btnSelAll, btnUnselAll;
        private CheckBox _chkSaveSelection;

        private JetListView _searchEngines;
        private JetListViewColumn _nameColumn;
        private CheckBoxColumn _checkColumn;

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private Container components = null;

        private HashMap _urls = new HashMap();
        private string[] _savedFeeds = new string[ 0 ];
        private const string cIniBoolKey = "SaveSearchFeedSet";
        private const string cIniSetKey = "SearchFeedSet";

        public event EventHandler NextPage;

        //  Any step pane must be able to control the possibility to move
        //  further (via button Next) depending on the internal state.
        internal SubscribeForm.CanMoveNextDelegate  NextPredicate;

        #region Ctor & initialization
        public SearchEnginesPane()
        {
            InitializeComponent();

            InitializeList();
            ReadSavedSelection();
            CollectEngines();
        }

        private void InitializeList()
        {
            _checkColumn = new CheckBoxColumn();
            _checkColumn.AfterCheck += new CheckBoxEventHandler(_checkColumn_AfterCheck);
            _nameColumn = new JetListViewColumn();
            _nameColumn.SizeToContent = true;
            _searchEngines.Columns.AddRange( new JetListViewColumn[] { _checkColumn, _nameColumn } );
            _searchEngines.ControlPainter = new GdiControlPainter();
            _searchEngines.FullRowSelect = true;
        }

        private void CollectEngines()
        {
            IResourceList list = Core.ResourceStore.GetAllResources( Props.RSSSearchEngineResource );
            list.Sort( new int[] { Core.Props.Name }, true );
            foreach( IResource engine in list )
            {
                string name = engine.GetPropText( Core.Props.Name );
                _searchEngines.Nodes.Add( name );
                _urls[ name ] = engine.GetPropText( Props.URL );
            }
        }
        #endregion Ctor & initialization

        public string   FeedTitle       { set { _lblEngineNameInProgress.Text = value; } }
        public Label    ProgressLabel   { get { return _lblProgress; } }
        public bool     ControlsEnabled { get { return _edtSearchQuery.Enabled; }
                                          set { _edtSearchQuery.Enabled = value; } }
        public void     ClearProgress() { _lblProgress.Text = _lblEngineNameInProgress.Text = string.Empty; }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if ( disposing )
            {
                if ( components != null )
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
            this._edtSearchQuery = new JetTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this._lblError = new System.Windows.Forms.Label();
            this._lblProgress = new System.Windows.Forms.Label();
            this._lblEngineNameInProgress = new Label();
            this._lblTitle = new System.Windows.Forms.Label();
            this.btnSelAll = new System.Windows.Forms.Button();
            this.btnUnselAll = new System.Windows.Forms.Button();
            this._searchEngines = new JetListView();
            _chkSaveSelection = new CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(12, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(372, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Enter search keywords:";
            // 
            // _edtSearchQuery
            // 
            this._edtSearchQuery.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtSearchQuery.Location = new System.Drawing.Point(12, 44);
            this._edtSearchQuery.Name = "_edtSearchQuery";
            this._edtSearchQuery.Size = new System.Drawing.Size(355, 20);
            this._edtSearchQuery.TabIndex = 2;
            this._edtSearchQuery.Text = "";
            this._edtSearchQuery.KeyDown += new System.Windows.Forms.KeyEventHandler(this._edtURL_KeyDown);
            this._edtSearchQuery.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this._edtURL_KeyPress);
            this._edtSearchQuery.TextChanged += new EventHandler(StateChanged);
            this._edtSearchQuery.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            // 
            // _lblError
            // 
            this._lblError.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblError.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._lblError.Location = new System.Drawing.Point(12, 252);
            this._lblError.Name = "_lblError";
            this._lblError.Size = new System.Drawing.Size(344, 72);
            this._lblError.TabIndex = 5;
            this._lblError.Text = "label3";
            this._lblError.Visible = false;
            // 
            // labelTitle
            // 
            this._lblTitle.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblTitle.Location = new System.Drawing.Point(12, 76);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(372, 17);
            this._lblTitle.TabIndex = 3;
            this._lblTitle.Text = "Choose Search Engine:";
            // 
            // _searchEngines
            // 
            this._searchEngines.Location = new System.Drawing.Point(12, 96);
            this._searchEngines.Name = "_searchEngines";
            this._searchEngines.Size = new System.Drawing.Size(260, 205);
            this._searchEngines.TabIndex = 4;
            this._searchEngines.TabStop = false;
            this._searchEngines.Text = "Choose Search Engine";
            this._searchEngines.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            // 
            // btnSelAll
            // 
            this.btnSelAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnSelAll.Location = new System.Drawing.Point(295, 96);
            this.btnSelAll.Text = "&Select All";
            this.btnSelAll.Name = "btnSelAll";
            this.btnSelAll.Size = new System.Drawing.Size(75, 24);
            this.btnSelAll.Click += new EventHandler(btnSelAll_Click);
            this.btnSelAll.TabIndex = 5;
            this.btnSelAll.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            // 
            // btnUnselAll
            // 
            this.btnUnselAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnUnselAll.Location = new System.Drawing.Point(295, 126);
            this.btnUnselAll.Text = "&Unselect All";
            this.btnUnselAll.Name = "btnUnselAll";
            this.btnUnselAll.Size = new System.Drawing.Size(75, 24);
            this.btnUnselAll.Click += new EventHandler(btnUnselAll_Click);
            this.btnUnselAll.TabIndex = 6;
            this.btnUnselAll.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            // 
            // _chkSaveSelection
            // 
            _chkSaveSelection.FlatStyle = System.Windows.Forms.FlatStyle.System;
            _chkSaveSelection.Location = new System.Drawing.Point(12, _searchEngines.Bottom + 6);
            _chkSaveSelection.Text = "&Remember selection";
            _chkSaveSelection.Name = "_chkSaveSelection";
            _chkSaveSelection.Size = new System.Drawing.Size(300, 20);
            _chkSaveSelection.TabIndex = 7;
            _chkSaveSelection.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _chkSaveSelection.CheckedChanged += new EventHandler(_chkSaveSelection_CheckedChanged);
            // 
            // _lblEngineNameInProgress
            // 
            this._lblEngineNameInProgress.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblEngineNameInProgress.Location = new System.Drawing.Point(16, _searchEngines.Bottom + 32);
            this._lblEngineNameInProgress.Name = "_lblProgress";
            this._lblEngineNameInProgress.Size = new System.Drawing.Size(344, 48);
            this._lblEngineNameInProgress.TabIndex = 8;
            this._lblEngineNameInProgress.Font = new Font( "Tahoma", 10.0f, FontStyle.Bold );
            this._lblEngineNameInProgress.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            // 
            // _lblProgress
            // 
            this._lblProgress.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblProgress.Location = new System.Drawing.Point(16, _searchEngines.Bottom + 52);
            this._lblProgress.Name = "_lblProgress";
            this._lblProgress.Size = new System.Drawing.Size(344, 48);
            this._lblProgress.TabIndex = 9;
            this._lblProgress.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            // 
            // SearchEnginesPane
            // 
            this.Controls.Add(this._searchEngines);
            this.Controls.Add(this._lblProgress);
            this.Controls.Add(this._lblEngineNameInProgress);
            this.Controls.Add(this._lblError);
            this.Controls.Add(this._edtSearchQuery);
            this.Controls.Add(this.btnSelAll);
            this.Controls.Add(this.btnUnselAll);
            this.Controls.Add(_chkSaveSelection);
            this.Controls.Add(this._lblTitle);
            this.Controls.Add(this.label1);
            this.Name = "SearchEnginesPane";
            this.Size = new System.Drawing.Size(384, 396);
            this.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            this.VisibleChanged += new EventHandler(SearchEnginesPane_VisibleChanged);
            this.ResumeLayout(false);

        }
        #endregion

        public string SearchPhrase { get { return _edtSearchQuery.Text.Trim(); } }

        public string SearchQuery
        {
            get
            {
                string text = _edtSearchQuery.Text.Trim();
                text = text.Replace( ' ', '+' ).Replace( "\"", "%22" );
                return text;
            }
        }

        public string[] CheckedURLs
        {
            get
            {
                ArrayList urls = new ArrayList();
                foreach( JetListViewNode node in _searchEngines.Nodes )
                {
                    if ( _checkColumn.GetItemCheckState( node.Data ) == CheckBoxState.Checked )
                    {
                        urls.Add( (string) _urls[ (string)node.Data ] );
                    }
                }
                return (string[]) urls.ToArray( typeof( string ));
            }
        }

        public string[] CheckedFeedNames
        {
            get
            {
                ArrayList names = new ArrayList();
                foreach( JetListViewNode node in _searchEngines.Nodes )
                {
                    if ( _checkColumn.GetItemCheckState( node.Data ) == CheckBoxState.Checked )
                        names.Add( (string)node.Data );
                }
                return (string[]) names.ToArray( typeof( string ));
            }
        }

        public string ErrorMessage
        {
            get { return string.Empty; }
            set
            {
                if ( value == null )
                {
                    _lblError.Visible = false;
                }
                else
                {
                    _lblError.Text = value;
                    _lblError.Visible = true;
                }
            }
        }

        private void _edtURL_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyData == Keys.Enter )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( OnNextPage ) );
            }
        }

        private void _edtURL_KeyPress( object sender, KeyPressEventArgs e )
        {
            ErrorMessage = "";
            if ( e.KeyChar == '\r' )
            {
                e.Handled = true;
            }
        }

        private void OnNextPage()
        {
            SaveSelection();
            if ( NextPage != null )
            {
                NextPage( this, EventArgs.Empty );
            }
        }

        private void btnSelAll_Click(object sender, EventArgs e)
        {
            foreach( JetListViewNode node in _searchEngines.Nodes )
            {
                _checkColumn.SetItemCheckState( node.Data, CheckBoxState.Checked );
            }
            NextPredicate( IsValidForm() );
        }

        private void btnUnselAll_Click(object sender, EventArgs e)
        {
            foreach( JetListViewNode node in _searchEngines.Nodes )
            {
                _checkColumn.SetItemCheckState( node.Data, CheckBoxState.Unchecked );
            }
            NextPredicate( IsValidForm() );
        }

        private void _checkColumn_AfterCheck(object sender, CheckBoxEventArgs e)
        {
            NextPredicate( IsValidForm() );
        }

        private void StateChanged(object sender, EventArgs e)
        {
            NextPredicate( IsValidForm() );
        }

        private void SearchEnginesPane_VisibleChanged(object sender, EventArgs e)
        {
            if( !Visible )
                SaveSelection();
            else
                SetSelection();
            NextPredicate( false );
        }

        private bool IsValidForm()
        {
            return !String.IsNullOrEmpty( _edtSearchQuery.Text ) && AnyItemChecked();
        }

        private bool AnyItemChecked()
        {
            foreach( JetListViewNode node in _searchEngines.Nodes )
            {
                if ( _checkColumn.GetItemCheckState( node.Data ) == CheckBoxState.Checked )
                    return true;
            }
            return false;
        }

        private void ReadSavedSelection()
        {
            string feedsSet = string.Empty;
            _chkSaveSelection.Checked = Core.SettingStore.ReadBool( "RSS", cIniBoolKey, true );
            if( _chkSaveSelection.Checked )
                feedsSet = Core.SettingStore.ReadString( "RSS", cIniSetKey, string.Empty );

            _savedFeeds = (feedsSet.Length > 0) ? feedsSet.Split( '|' ) : new string[ 0 ];
            Array.Sort( _savedFeeds );
        }

        private void SetSelection()
        {
            foreach( string name in _savedFeeds )
                _checkColumn.SetItemCheckState( name, CheckBoxState.Checked );
        }

        private void SaveSelection()
        {
            if( _chkSaveSelection.Checked )
            {
                string str = Utils.MergeStrings( CheckedFeedNames, '|' );
                Core.SettingStore.WriteString( "RSS", cIniSetKey, str );
            }
        }

        private void _chkSaveSelection_CheckedChanged(object sender, EventArgs e)
        {
            Core.SettingStore.WriteBool( "RSS", cIniBoolKey, _chkSaveSelection.Checked );
        }
    }
}