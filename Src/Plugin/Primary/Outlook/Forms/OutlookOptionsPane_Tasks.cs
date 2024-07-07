// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    public class OutlookOptionsPane_Tasks : AbstractOptionsPane
    {
        private Label label1;
        private MAPIFolderTreeView _treeView;
        private JetBrains.Omea.GUIControls.CheckBoxSettingEditor _syncTaskCategory;
        private JetBrains.Omea.GUIControls.CheckBoxSettingEditor _taskExport;
        private IContainer components = null;

        private OutlookOptionsPane_Tasks( )
        {
            // This call is required by the Windows Form Designer.
            InitializeComponent();
            _treeView.Init( FolderType.Task, "Task" );
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
            this._treeView = new JetBrains.Omea.OutlookPlugin.MAPIFolderTreeView();
            this._syncTaskCategory = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._taskExport = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
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
            this.label1.Text = "Select the Outlook task folders you would like to synchronize with Omea. Tasks fr" +
                "om the selected folders will be imported into Omea.";
            //
            // _treeView
            //
            this._treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._treeView.DoubleBuffer = false;
            this._treeView.ImageIndex = -1;
            this._treeView.Location = new System.Drawing.Point(0, 88);
            this._treeView.MultiSelect = false;
            this._treeView.Name = "_treeView";
            this._treeView.NodePainter = null;
            this._treeView.SelectedImageIndex = -1;
            this._treeView.SelectedNodes = new System.Windows.Forms.TreeNode[0];
            this._treeView.Size = new System.Drawing.Size(284, 92);
            this._treeView.TabIndex = 4;
            this._treeView.ThreeStateCheckboxes = false;
            //
            // _syncTaskCategory
            //
            this._syncTaskCategory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._syncTaskCategory.Changed = false;
            this._syncTaskCategory.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._syncTaskCategory.InvertValue = false;
            this._syncTaskCategory.Location = new System.Drawing.Point(4, 204);
            this._syncTaskCategory.Name = "_syncTaskCategory";
            this._syncTaskCategory.Size = new System.Drawing.Size(272, 24);
            this._syncTaskCategory.TabIndex = 18;
            this._syncTaskCategory.Tag = "";
            this._syncTaskCategory.Text = "&Synchronize categories for tasks";
            //
            // _taskExport
            //
            this._taskExport.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._taskExport.Changed = false;
            this._taskExport.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._taskExport.InvertValue = false;
            this._taskExport.Location = new System.Drawing.Point(4, 180);
            this._taskExport.Name = "_taskExport";
            this._taskExport.Size = new System.Drawing.Size(272, 24);
            this._taskExport.TabIndex = 11;
            this._taskExport.Text = "&Export tasks to Outlook";
            //
            // OutlookOptionsPane_Tasks
            //
            this.Controls.Add(this._taskExport);
            this.Controls.Add(this._syncTaskCategory);
            this.Controls.Add(this._treeView);
            this.Controls.Add(this.label1);
            this.Name = "OutlookOptionsPane_Tasks";
            this.Size = new System.Drawing.Size(284, 232);
            this.ResumeLayout(false);

        }
        #endregion

        internal static AbstractOptionsPane OptionsPaneCreator( )
        {
            return new OutlookOptionsPane_Tasks( );
        }
        public override void EnterPane()
        {
            _treeView.ShowTree();
        }

        public override void ShowPane()
        {
            _taskExport.SetSetting( Settings.ExportTasks );
            _syncTaskCategory.SetSetting( Settings.SyncTaskCategory );

        }
        public override void OK()
        {
            _treeView.Save();
            SettingSaver.Save( Controls );
            Settings.LoadSettings();
        }

        public override string GetHelpKeyword()
        {
            return "/reference/outlook_tasks.htm";
        }
    }
}
