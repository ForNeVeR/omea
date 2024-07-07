// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    public class OutlookOptionsPane_IgnoredFolders : AbstractOptionsPane
    {
        private Label label1;
        private ResourceTreeView _treeView;
        private JetBrains.Omea.GUIControls.CheckBoxSettingEditor _syncMailCategory;
        private JetBrains.Omea.GUIControls.CheckBoxSettingEditor _chkShowExcludedFolders;
        private Button btnSelAll, btnUnselAll;
        private IContainer components = null;
        private bool _checkStatusUpdated = false;

        public OutlookOptionsPane_IgnoredFolders( )
        {
            // This call is required by the Windows Form Designer.
            InitializeComponent();
            _chkShowExcludedFolders.Visible = false;
            _treeView.AddNodeFilter( new OutlookFoldersFilter( true ) );
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this._treeView = new JetBrains.Omea.GUIControls.ResourceTreeView();
            this._syncMailCategory = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._chkShowExcludedFolders = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            btnSelAll = new Button();
            btnUnselAll = new Button();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(284, 88);
            this.label1.TabIndex = 3;
            this.label1.Text = "Select the Outlook folders you would like to access from Omea. Mail in folders yo" +
                "u do not select will not be indexed.";
            //
            // _treeView
            //
            this._treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._treeView.CheckBoxes = true;
            this._treeView.CheckedSetValue = 0;
            this._treeView.CheckedUnsetValue = 1;
            this._treeView.DoubleBuffer = false;
            this._treeView.ImageIndex = -1;
            this._treeView.Location = new System.Drawing.Point(0, 88);
            this._treeView.MultiSelect = false;
            this._treeView.Name = "_treeView";
            this._treeView.NodePainter = null;
            this._treeView.ParentProperty = 0;
            this._treeView.ResourceChildProvider = null;
            this._treeView.SelectedImageIndex = -1;
            this._treeView.SelectedNodes = new System.Windows.Forms.TreeNode[0];
            this._treeView.ShowContextMenu = false;
            this._treeView.ShowRootResource = false;
            this._treeView.Size = new System.Drawing.Size(200, 92);
            this._treeView.TabIndex = 4;
            this._treeView.ThreeStateCheckboxes = false;
            this._treeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.OnAfterCheck);
            //
            // _syncMailCategory
            //
            this._syncMailCategory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._syncMailCategory.Changed = false;
            this._syncMailCategory.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._syncMailCategory.InvertValue = false;
            this._syncMailCategory.Location = new System.Drawing.Point(4, 180);
            this._syncMailCategory.Name = "_syncMailCategory";
            this._syncMailCategory.Size = new System.Drawing.Size(272, 24);
            this._syncMailCategory.TabIndex = 19;
            this._syncMailCategory.Tag = "";
            this._syncMailCategory.Text = "&Synchronize categories for mail";
            //
            // btnSelAll
            //
            this.btnSelAll.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
            this.btnSelAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnSelAll.Location = new System.Drawing.Point(210, 88);
            this.btnSelAll.Text = "Select All";
            this.btnSelAll.Name = "btnSelAll";
            this.btnSelAll.Size = new System.Drawing.Size(75, 24);
            this.btnSelAll.Click += new EventHandler(btnSelAll_Click);
            this.btnSelAll.TabIndex = 2;
            //
            // btnUnselAll
            //
            this.btnUnselAll.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
            this.btnUnselAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnUnselAll.Location = new System.Drawing.Point(210, 118);
            this.btnUnselAll.Text = "Unselect All";
            this.btnUnselAll.Name = "btnUnselAll";
            this.btnUnselAll.Size = new System.Drawing.Size(75, 24);
            this.btnUnselAll.Click += new EventHandler(btnUnselAll_Click);
            this.btnUnselAll.TabIndex = 3;
            //
            // _chkShowExcludedFolders
            //
            this._chkShowExcludedFolders.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._chkShowExcludedFolders.Changed = false;
            this._chkShowExcludedFolders.InvertValue = false;
            this._chkShowExcludedFolders.Location = new System.Drawing.Point(4, 204);
            this._chkShowExcludedFolders.Name = "_chkShowExcludedFolders";
            this._chkShowExcludedFolders.Size = new System.Drawing.Size(272, 24);
            this._chkShowExcludedFolders.TabIndex = 20;
            this._chkShowExcludedFolders.Text = "Show &excluded folders";
            //
            // OutlookOptionsPane_IgnoredFolders
            //
            this.Controls.Add(this._chkShowExcludedFolders);
            this.Controls.Add(this._syncMailCategory);
            this.Controls.Add(this.btnSelAll);
            this.Controls.Add(this.btnUnselAll);
            this.Controls.Add(this._treeView);
            this.Controls.Add(this.label1);
            this.Name = "OutlookOptionsPane_IgnoredFolders";
            this.Size = new System.Drawing.Size(284, 232);
            this.ResumeLayout(false);

        }
        #endregion

        internal static AbstractOptionsPane OptionsPaneCreator( )
        {
            return new OutlookOptionsPane_IgnoredFolders( );
        }
        public override void EnterPane()
        {
            if ( IsStartupPane )
            {
                using( SynchronizeFoldersProgressForm dlg = new SynchronizeFoldersProgressForm() )
                {
                    // this performs the actual folder structure synchronization
                    dlg.ShowDialog( FindForm() );
                }

                if ( !_checkStatusUpdated )
                {
                    Core.ResourceAP.RunUniqueJob(
                        new MethodInvoker( Folder.IgnoreDeletedItemsIfNoIgnoredFolders ) );
                    UpdateCheckStatus( _treeView.Nodes );
                    foreach ( TreeNode node in _treeView.Nodes )
                    {
                        _treeView.ForceCreateChildren( node );
                        node.Expand();
                    }
                    _checkStatusUpdated = true;
                }
            }
            else
            {
                _chkShowExcludedFolders.Visible = true;
            }
        }

        private void UpdateCheckStatus( TreeNodeCollection nodes )
        {
            foreach ( TreeNode childNode in nodes )
            {
                childNode.Checked = !( childNode.Tag as IResource ).HasProp( PROP.IgnoredFolder );
                childNode.Tag = childNode.Tag;
                UpdateCheckStatus( childNode.Nodes );
            }
        }

        public override void ShowPane()
        {
            _treeView.CheckedProperty = PROP.IgnoredFolder;
            _treeView.DelaySaveChecked = true;
            _treeView.OpenProperty = PROP.OpenIgnoreFolder;
            _treeView.ParentProperty = Core.Props.Parent;
            _treeView.RootResource = ICore.Instance.ResourceTreeManager.GetRootForType(STR.MAPIFolder);
            _syncMailCategory.SetSetting( Settings.SyncMailCategory );
            _chkShowExcludedFolders.SetSetting( Settings.ShowExcludedFolders );
        }
        public override void OK()
        {
            //  Workaround of OM-13897, calling an OutlookSession in the shutdown
            //  state causes unpredictable results.
            if( Core.State == CoreState.Running )
            {
                _treeView.SaveCheckedState();
                MAPIIDs IDs = OutlookSession.GetInboxIDs();
                if ( IDs != null )
                {
                    IResource folder = Folder.Find( IDs.EntryID );
                    if ( folder != null && !Folder.IsIgnored( folder ) )
                    {
                        Core.UIManager.CreateShortcutToResource( folder );
                    }
                }
                SettingSaver.Save( Controls );
                Settings.LoadSettings();
            }
        }

        private void SetCheckStatusForChildren( TreeNode node )
        {
            foreach ( TreeNode childNode in node.Nodes )
            {
                childNode.Checked = node.Checked;
                SetCheckStatusForChildren( childNode );
            }
        }

        private void OnAfterCheck(object sender, TreeViewEventArgs e)
        {
            if ( e.Action.Equals( TreeViewAction.ByKeyboard ) || e.Action.Equals( TreeViewAction.ByMouse ) )
            {
                TreeNode node = e.Node;
                _treeView.ForceCreateChildren( node );
                SetCheckStatusForChildren( node );
            }
        }

        private void btnSelAll_Click(object sender, EventArgs e)
        {
            foreach( TreeNode node in _treeView.Nodes )
            {
                CheckItems( node, true );
            }
        }

        private void btnUnselAll_Click(object sender, EventArgs e)
        {
            foreach( TreeNode node in _treeView.Nodes )
            {
                CheckItems( node, false );
            }
        }
        private void CheckItems( TreeNode node, bool mode )
        {
            node.Checked = mode;
            foreach ( TreeNode childNode in node.Nodes )
            {
                CheckItems( childNode, mode );
            }
        }

        public override string GetHelpKeyword()
        {
            return "/reference/outlook_folders.html";
        }
    }
}

