// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Components.CustomTreeView;
using JetBrains.DataStructures;

namespace JetBrains.Omea.OutlookPlugin
{
    public class OutlookOptionsPane_AddressBooks : JetBrains.Omea.OpenAPI.AbstractOptionsPane
    {
        private System.Windows.Forms.Label label1;
        private MAPIFolderTreeView _treeView;
        private System.ComponentModel.IContainer components = null;
        private JetBrains.Omea.GUIControls.CheckBoxSettingEditor _categoriesFromMailLists;
        private JetBrains.Omea.GUIControls.CheckBoxSettingEditor _syncContactCategory;

        public OutlookOptionsPane_AddressBooks( )
        {
            // This call is required by the Windows Form Designer.
            InitializeComponent();
            _treeView.Init( FolderType.Contact, "AddressBook" );
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
            this._syncContactCategory = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._categoriesFromMailLists = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
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
            this.label1.Text = "Select the Outlook address books you would like to synchronize with Omea. Contact" +
                "s from the selected address books will be imported into Omea.";
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
            // _syncContactCategory
            //
            this._syncContactCategory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._syncContactCategory.Changed = false;
            this._syncContactCategory.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._syncContactCategory.InvertValue = false;
            this._syncContactCategory.Location = new System.Drawing.Point(4, 204);
            this._syncContactCategory.Name = "_syncContactCategory";
            this._syncContactCategory.Size = new System.Drawing.Size(272, 24);
            this._syncContactCategory.TabIndex = 18;
            this._syncContactCategory.Tag = "";
            this._syncContactCategory.Text = "&Synchronize categories for contacts";
            //
            // _categoriesFromMailLists
            //
            this._categoriesFromMailLists.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._categoriesFromMailLists.Changed = false;
            this._categoriesFromMailLists.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._categoriesFromMailLists.InvertValue = false;
            this._categoriesFromMailLists.Location = new System.Drawing.Point(4, 180);
            this._categoriesFromMailLists.Name = "_categoriesFromMailLists";
            this._categoriesFromMailLists.Size = new System.Drawing.Size(272, 24);
            this._categoriesFromMailLists.TabIndex = 19;
            this._categoriesFromMailLists.Text = "&Create categories from Internet mailing lists";
            //
            // OutlookOptionsPane_AddressBooks
            //
            this.Controls.Add(this._categoriesFromMailLists);
            this.Controls.Add(this._syncContactCategory);
            this.Controls.Add(this._treeView);
            this.Controls.Add(this.label1);
            this.Name = "OutlookOptionsPane_AddressBooks";
            this.Size = new System.Drawing.Size(284, 232);
            this.ResumeLayout(false);

        }
        #endregion

        internal static AbstractOptionsPane OptionsPaneCreator( )
        {
            return new OutlookOptionsPane_AddressBooks( );
        }
        public override void EnterPane()
        {
            _treeView.CollectCheckStates();
            _treeView.ClearTree();
            IResourceList globalAddressBooks =
                Core.ResourceStore.GetAllResources( STR.OutlookABDescriptor );

            foreach ( IResource globalAddressBook in globalAddressBooks )
            {
                int iconIndex = Core.ResourceIconManager.GetDefaultIconIndex( "AddressBook" );
                TreeNode treeNode = new TreeNode( globalAddressBook.DisplayName, iconIndex, iconIndex );
                treeNode.Tag = globalAddressBook;
                _treeView.Nodes.Add( treeNode );
                _treeView.SetNodeCheckStateFromCollection( treeNode );
            }
            _treeView.PopulateTree();
        }
        public override void ShowPane()
        {
            _syncContactCategory.SetSetting( Settings.SyncContactCategory );
            _categoriesFromMailLists.SetSetting( Settings.CreateCategoriesFromMailingLists );
        }
        public override void OK()
        {
            _treeView.Save();
            SettingSaver.Save( Controls );
            Settings.LoadSettings();
        }

        public override string GetHelpKeyword()
        {
            return "/reference/outlook_address_books.htm";
        }
    }

	internal class HashNode
    {
        private HashMap _map = new HashMap();
        private IResource _resource;
        public HashNode( IResource resource )
        {
            _resource = resource;
        }
        public IResource Resource { get { return _resource; } }
        public HashNode InsertResource( IResource resource )
        {
            HashMap.Entry entry = _map.GetEntry( resource.Id );
            if ( entry == null )
            {
                HashNode hashNode = new HashNode( resource );
                _map.Add( resource.Id, hashNode );
                return hashNode;
            }
            else
            {
                return (HashNode)entry.Value;
            }
        }
        public HashMap HashNodes
        {
            get { return _map; }
        }
    }

}
