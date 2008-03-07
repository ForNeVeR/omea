/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;
using JetBrains.DataStructures;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
    public class OptionsForm : DialogBase
    {
        private Button _cancelButton;
        private Button _okButton;
        private string _selectedGroup;
        private string _selectedPaneName;
        private Panel _panelFormBody;
        private TreeView _panesView;
        private HashMap _optionsGroups;
        private Splitter splitter1;
        private CustomStylePanel _canvasPanel;
        private JetLinkLabel _captionLabel;
        private Panel _panePanel;
        private Button _btnHelp;
        private Button _applyButton;
        private Control _lastPane;
        private string _errorMessage;
        private Control _controlToSelect;
        private float _dx;
        private float _dy;
        private bool _needRestart;

        private System.ComponentModel.Container components = null;

        public OptionsForm()
        {
            InitializeComponent();
            _selectedGroup = _selectedPaneName = string.Empty;
            _panesView.Width = ObjectStore.ReadInt( "OptionsForm", "PanesViewWidth", 176 );
            _dx = _dy = 1;
        }

        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
                ObjectStore.WriteInt(
                    "OptionsForm", "PanesViewWidth", (int) ( ( (float) _panesView.Width ) / _dx ) );
            }
            base.Dispose( disposing );
        }

        protected override void ScaleCore( float dx, float dy )
        {
            base.ScaleCore( dx, dy );
            if( Environment.Version.Major < 2 )
            {
                _dx = dx;
                _dy = dy;
            }
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this._cancelButton = new System.Windows.Forms.Button();
			this._okButton = new System.Windows.Forms.Button();
			this._panelFormBody = new System.Windows.Forms.Panel();
			this._canvasPanel = new JetBrains.Omea.GUIControls.CustomStylePanel();
			this._panePanel = new System.Windows.Forms.Panel();
			this._captionLabel = new JetBrains.Omea.GUIControls.JetLinkLabel();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this._panesView = new System.Windows.Forms.TreeView();
			this._btnHelp = new System.Windows.Forms.Button();
			this._applyButton = new System.Windows.Forms.Button();
			this._panelFormBody.SuspendLayout();
			this._canvasPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// _cancelButton
			// 
			this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._cancelButton.Location = new System.Drawing.Point(460, 532);
			this._cancelButton.Name = "_cancelButton";
			this._cancelButton.TabIndex = 2;
			this._cancelButton.Text = "Cancel";
			this._cancelButton.Click += new System.EventHandler(this._cancelButton_Click);
			// 
			// _okButton
			// 
			this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._okButton.Location = new System.Drawing.Point(376, 532);
			this._okButton.Name = "_okButton";
			this._okButton.TabIndex = 1;
			this._okButton.Text = "OK";
			this._okButton.Click += new System.EventHandler(this._okButton_Click);
			// 
			// _panelFormBody
			// 
			this._panelFormBody.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._panelFormBody.Controls.Add(this._canvasPanel);
			this._panelFormBody.Controls.Add(this.splitter1);
			this._panelFormBody.Controls.Add(this._panesView);
			this._panelFormBody.Location = new System.Drawing.Point(8, 8);
			this._panelFormBody.Name = "_panelFormBody";
			this._panelFormBody.Size = new System.Drawing.Size(692, 516);
			this._panelFormBody.TabIndex = 0;
			// 
			// _canvasPanel
			// 
			this._canvasPanel.Controls.Add(this._panePanel);
			this._canvasPanel.Controls.Add(this._captionLabel);
			this._canvasPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._canvasPanel.Location = new System.Drawing.Point(180, 0);
			this._canvasPanel.Name = "_canvasPanel";
			this._canvasPanel.Size = new System.Drawing.Size(512, 516);
			this._canvasPanel.TabIndex = 0;
			this._canvasPanel.Paint += new System.Windows.Forms.PaintEventHandler(this._canvasPanel_Paint);
			// 
			// _panePanel
			// 
			this._panePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._panePanel.Location = new System.Drawing.Point(8, 28);
			this._panePanel.Name = "_panePanel";
			this._panePanel.Size = new System.Drawing.Size(496, 480);
			this._panePanel.TabIndex = 0;
			// 
			// _captionLabel
			// 
			this._captionLabel.AutoSize = false;
			this._captionLabel.BackColor = System.Drawing.SystemColors.ActiveCaption;
			this._captionLabel.ClickableLink = false;
			this._captionLabel.Cursor = System.Windows.Forms.Cursors.Default;
			this._captionLabel.Dock = System.Windows.Forms.DockStyle.Top;
			this._captionLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
			this._captionLabel.Location = new System.Drawing.Point(0, 0);
			this._captionLabel.Name = "_captionLabel";
			this._captionLabel.Size = new System.Drawing.Size(512, 20);
			this._captionLabel.TabIndex = 3;
			this._captionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// splitter1
			// 
			this.splitter1.BackColor = System.Drawing.SystemColors.ControlLight;
			this.splitter1.Location = new System.Drawing.Point(176, 0);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(4, 516);
			this.splitter1.TabIndex = 12;
			this.splitter1.TabStop = false;
			// 
			// _panesView
			// 
			this._panesView.Dock = System.Windows.Forms.DockStyle.Left;
			this._panesView.HideSelection = false;
			this._panesView.ImageIndex = -1;
			this._panesView.Location = new System.Drawing.Point(0, 0);
			this._panesView.Name = "_panesView";
			this._panesView.SelectedImageIndex = -1;
			this._panesView.Size = new System.Drawing.Size(176, 516);
			this._panesView.TabIndex = 1;
			this._panesView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this._panesView_AfterSelect);
			// 
			// _btnHelp
			// 
			this._btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnHelp.Location = new System.Drawing.Point(628, 532);
			this._btnHelp.Name = "_btnHelp";
			this._btnHelp.TabIndex = 4;
			this._btnHelp.Text = "Help";
			this._btnHelp.Click += new System.EventHandler(this._btnHelp_Click);
			// 
			// _applyButton
			// 
			this._applyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._applyButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._applyButton.Location = new System.Drawing.Point(544, 532);
			this._applyButton.Name = "_applyButton";
			this._applyButton.TabIndex = 3;
			this._applyButton.Text = "&Apply";
			this._applyButton.Click += new System.EventHandler(this._applyButton_Click);
			// 
			// OptionsForm
			// 
			this.AcceptButton = this._okButton;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.CancelButton = this._cancelButton;
			this.ClientSize = new System.Drawing.Size(712, 566);
			this.Controls.Add(this._applyButton);
			this.Controls.Add(this._btnHelp);
			this.Controls.Add(this._panelFormBody);
			this.Controls.Add(this._okButton);
			this.Controls.Add(this._cancelButton);
			this.MinimumSize = new System.Drawing.Size(416, 272);
			this.Name = "OptionsForm";
			this.Text = "Options";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.OptionsForm_Closing);
			this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.OptionsForm_HelpRequested);
			this._panelFormBody.ResumeLayout(false);
			this._canvasPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
        #endregion

        internal void EditOptions( HashMap optionsGroups, IWin32Window owner )
        {
            RestoreSettings();
            _panesView.BeginUpdate();
            try
            {
                _panesView.Sorted = true;
                _panesView.Nodes.Clear();
                lock( optionsGroups )
                {
                    _optionsGroups = optionsGroups;
                    foreach( HashMap.Entry E in optionsGroups )
                    {
                        string group = (string) E.Key;
                        UIManager.OptionsGroupDescriptor desc = (UIManager.OptionsGroupDescriptor) E.Value;
                        TreeNode rootNode = _panesView.Nodes.Add( group );
                        rootNode.Tag = desc.Prompt;
                        rootNode.Expand();
                        foreach( HashMap.Entry paneEntry in desc._optionsPanes )
                        {
                            TreeNode paneNode = rootNode.Nodes.Add( (string) paneEntry.Key );
                            paneNode.Tag = paneEntry.Value;
                        }
                    }
                }
                _panesView.Sorted = false;
                for( int i = 0; i < _panesView.Nodes.Count; ++i )
                {
                    TreeNode node = _panesView.Nodes[ i ];
                    if( node.Text.IndexOf( Core.ProductName ) >= 0 )
                    {
                        _panesView.Nodes.RemoveAt( i );
                        _panesView.Nodes.Insert( 0, node );
                        break;
                    }
                }
            }
            finally
            {
                _panesView.EndUpdate();
            }
            bool bSelected = false;
            if( _selectedGroup.Length == 0 )
            {
                _selectedGroup = Core.SettingStore.ReadString( "OptionsDialog", "SelectedGroup" );
                _selectedPaneName = Core.SettingStore.ReadString( "OptionsDialog", "SelectedPane" );
            }
            if( _selectedGroup.Length > 0 )
            {
                for( int i = 0; i < _panesView.Nodes.Count; ++i )
                {
                    TreeNode groupNode = _panesView.Nodes[ i ];
                    if( _selectedGroup == groupNode.Text )
                    {
                        if( _selectedPaneName.Length > 0 )
                        {
                            for( int j = 0; j < groupNode.Nodes.Count; ++j )
                            {
                                if( groupNode.Nodes[ j ].Text == _selectedPaneName )
                                {
                                    _panesView.SelectedNode = groupNode.Nodes[ j ];
                                    bSelected = true;
                                    break;
                                }
                            }
                        }
                        if( !bSelected )
                        {
                            _panesView.SelectedNode = groupNode;
                            bSelected = true;
                        }
                        break;
                    }
                }
            }
            if( !bSelected && _panesView.Nodes.Count > 0 )
            {
                _panesView.SelectedNode = _panesView.Nodes[ 0 ];
            }
            ShowDialog( owner );
        }

        internal void EditOptions( string group, string panePame, HashMap optionsGroups, IWin32Window owner )
        {
            _selectedGroup = group;
            _selectedPaneName = panePame;
            EditOptions( optionsGroups, owner );
        }

        private void _cancelButton_Click(object sender, EventArgs e)
        {
            foreach( TreeNode rootNode in _panesView.Nodes )
            {
                foreach( TreeNode paneNode in rootNode.Nodes )
                {
                    if( paneNode.Tag is AbstractOptionsPane )
                    {
                        ( paneNode.Tag as AbstractOptionsPane ).Cancel();
                    }
                }
            }
        }

        private void _okButton_Click(object sender, EventArgs e)
        {
            if( _okButton.Enabled && ApplyChanges() )
            {
                _okButton.Enabled = false;
                DialogResult = DialogResult.OK;
                if( _needRestart &&
                    MessageBox.Show( this, "Changes will take effect only after restart of " + Core.ProductFullName +
                    ". Restart now?", "Confirm Restart", MessageBoxButtons.YesNo, MessageBoxIcon.Question ) == DialogResult.Yes )
                {
                    Core.UIManager.QueueUIJob( new MethodInvoker( Core.RestartApplication ) );
                }
            }
        }

        private void _applyButton_Click(object sender, EventArgs e)
        {
            if( ApplyChanges() )
            {
                AfterSelectPaneView();
            }
        }

        private void _canvasPanel_Paint(object sender, PaintEventArgs e)
        {
            Pen apen = new Pen( SystemColors.ActiveCaption, 1 );
            using( apen )
            {
                e.Graphics.DrawRectangle( apen, 0, 0, _canvasPanel.Width - 1, _canvasPanel.Height - 1 );
            }
        }

        private void _panesView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            AfterSelectPaneView();
        }

        private void OptionsForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TreeNode selected = _panesView.SelectedNode;
            if( selected.Parent == null )
            {
                Core.SettingStore.WriteString( "OptionsDialog", "SelectedGroup", selected.Text );
                Core.SettingStore.WriteString( "OptionsDialog", "SelectedPane", "" );
            }
            else
            {
                Core.SettingStore.WriteString( "OptionsDialog", "SelectedGroup", selected.Parent.Text );
                Core.SettingStore.WriteString( "OptionsDialog", "SelectedPane", selected.Text );
            }
        }

        private void _btnHelp_Click( object sender, EventArgs e )
        {
            ShowActivePaneHelp();
        }

        private void OptionsForm_HelpRequested( object sender, HelpEventArgs hlpevent )
        {
            ShowActivePaneHelp();        
        }

        private void ShowActivePaneHelp()
        {
            string topic = null;
            if ( _panesView.SelectedNode != null )
            {
                AbstractOptionsPane optionsPane = _panesView.SelectedNode.Tag as AbstractOptionsPane;
                if ( optionsPane != null )
                {
                    topic = optionsPane.GetHelpKeyword();
                }
            }
            
            if ( topic == null )
            {
                topic = "/reference/options_dialog.html";
            }

            Help.ShowHelp( this, Core.UIManager.HelpFileName, topic );
        }

        private bool ApplyChanges()
        {
            lock( _optionsGroups )
            {
                // save cursor
                Cursor currentCursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    foreach( TreeNode rootNode in _panesView.Nodes )
                    {
                        UIManager.OptionsGroupDescriptor desc =
                            (UIManager.OptionsGroupDescriptor) _optionsGroups[ rootNode.Text ];
                        if( desc != null )
                        {
                            _errorMessage = null;
                            _controlToSelect = null;
                            foreach( TreeNode paneNode in rootNode.Nodes )
                            {
                                AbstractOptionsPane pane = paneNode.Tag as AbstractOptionsPane;
                                if( pane != null )
                                {
                                    if( !pane.IsValid( ref _errorMessage, ref _controlToSelect ) )
                                    {
                                        if( _panesView.SelectedNode == paneNode )
                                        {
                                            DisplayError();
                                        }
                                        else
                                        {
                                            _panesView.SelectedNode = paneNode;
                                        }
                                        return false;
                                    }
                                }

                            }        
                            foreach( TreeNode paneNode in rootNode.Nodes )
                            {
                                AbstractOptionsPane pane = paneNode.Tag as AbstractOptionsPane;
                                if( pane != null )
                                {
                                    pane.OK();
                                    _needRestart = _needRestart || pane.NeedRestart;
                                    HashMap.Entry E = desc._optionsListeners.GetEntry( paneNode.Text );
                                    if( E != null )
                                    {
                                        ArrayList handlers = (ArrayList) E.Value;
                                        for( int i = 0; i < handlers.Count; i++ )
                                        {
                                            EventHandler handler = (EventHandler) handlers[ i ];
                                            handler( this, new EventArgs() );
                                        }
                                    }
                                    // extract options pane creator from map and replace pane with it
                                    paneNode.Tag = desc._optionsPanes[ paneNode.Text ];
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Cursor.Current = currentCursor;
                }
            }
            return true;
        }

        private void AfterSelectPaneView()
        {
            TreeNode selected = _panesView.SelectedNode;
            if( selected != null && selected.Tag != null )
            {
                if( _lastPane != null )
                {
                    if( _lastPane is AbstractOptionsPane )
                    {
                        ( (AbstractOptionsPane) _lastPane ).LeavePane();
                    }
                    _lastPane.Visible = false;
                }
                object tag = selected.Tag;
                Control pane;
                AbstractOptionsPane optionsPane;
                if( tag is Control )
                {
                    pane = tag as Control;
                }
                else
                {
                    if( tag is string )
                    {
                        pane = new Label();
                        pane.Text = tag as string;
                        ( pane as Label ).UseMnemonic = false;
                        ( pane as Label ).FlatStyle = FlatStyle.System;
                    }
                    else
                    {
                        optionsPane = ( tag as OptionsPaneCreator )();
                        AdjustContolProperties( optionsPane );
                        optionsPane.IsStartupPane = false;
                        optionsPane.ShowPane();
                        optionsPane.AutoScroll = true;
                        optionsPane.Scale( new SizeF( _dx, _dy ) );
                        pane = optionsPane;
                    }
                    pane.Parent = _panePanel;
                    pane.Dock = DockStyle.Fill;
                    selected.Tag = pane;
                }
                optionsPane = pane as AbstractOptionsPane;
                if( optionsPane != null )
                {
                    optionsPane.EnterPane();
                    if( _errorMessage != null )
                    {
                        Core.UIManager.QueueUIJob( new MethodInvoker( DisplayError ) );
                    }
                }
                pane.Visible = true;
                string caption = selected.Text;
                if( selected.Parent != null )
                {
                    caption = selected.Parent.Text + ": " + caption;
                }
                _captionLabel.Text = caption;
                _lastPane = pane;
            }
        }

        private void DisplayError()
        {
            MessageBox.Show( this, _errorMessage, "Options are incorrect",  MessageBoxButtons.OK, MessageBoxIcon.Error );
            if( _controlToSelect != null )
            {
                _controlToSelect.Select();
            }
            _errorMessage = null;
            _controlToSelect = null;
        }
    }
}
