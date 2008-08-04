/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections.Generic;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/**
     * Dialog for rearranging and deleting shortcuts.
     */

    public class OrganizeShortcutsDlg : DialogBase
	{
        private Button _btnOK;
        private Button _btnCancel;
        private Button _btnMoveUp;
        private Button _btnMoveDown;
        private Button _btnDelete;
        private ListView _lvShortcuts;
        private ColumnHeader columnHeader1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private Button _btnRename;
        private ColumnHeader columnHeader2;
        private Button _btnHelp;

//        private readonly IntArrayList _deletedShortcutIDs = new IntArrayList();
        private readonly List<int> _deletedShortcutIDs = new List<int>();

		public OrganizeShortcutsDlg()
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
			this._btnOK = new System.Windows.Forms.Button();
			this._btnCancel = new System.Windows.Forms.Button();
			this._lvShortcuts = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this._btnMoveUp = new System.Windows.Forms.Button();
			this._btnMoveDown = new System.Windows.Forms.Button();
			this._btnDelete = new System.Windows.Forms.Button();
			this._btnRename = new System.Windows.Forms.Button();
			this._btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _btnOK
			// 
			this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnOK.Location = new System.Drawing.Point(256, 271);
			this._btnOK.Name = "_btnOK";
			this._btnOK.TabIndex = 5;
			this._btnOK.Text = "OK";
			// 
			// _btnCancel
			// 
			this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnCancel.Location = new System.Drawing.Point(340, 271);
			this._btnCancel.Name = "_btnCancel";
			this._btnCancel.TabIndex = 6;
			this._btnCancel.Text = "Cancel";
			// 
			// _lvShortcuts
			// 
			this._lvShortcuts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._lvShortcuts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																						   this.columnHeader1,
																						   this.columnHeader2});
			this._lvShortcuts.FullRowSelect = true;
			this._lvShortcuts.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this._lvShortcuts.HideSelection = false;
			this._lvShortcuts.LabelEdit = true;
			this._lvShortcuts.Location = new System.Drawing.Point(4, 8);
			this._lvShortcuts.Name = "_lvShortcuts";
			this._lvShortcuts.Size = new System.Drawing.Size(412, 255);
			this._lvShortcuts.TabIndex = 0;
			this._lvShortcuts.View = System.Windows.Forms.View.Details;
			this._lvShortcuts.Layout += new System.Windows.Forms.LayoutEventHandler(this._lvShortcuts_Layout);
			this._lvShortcuts.SelectedIndexChanged += new System.EventHandler(this._lvShortcuts_SelectedIndexChanged);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Name";
			this.columnHeader1.Width = 150;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Resource";
			this.columnHeader2.Width = 220;
			// 
			// _btnMoveUp
			// 
			this._btnMoveUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._btnMoveUp.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnMoveUp.Location = new System.Drawing.Point(424, 8);
			this._btnMoveUp.Name = "_btnMoveUp";
			this._btnMoveUp.TabIndex = 1;
			this._btnMoveUp.Text = "Move Up";
			this._btnMoveUp.Click += new System.EventHandler(this._btnMoveUp_Click);
			// 
			// _btnMoveDown
			// 
			this._btnMoveDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._btnMoveDown.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnMoveDown.Location = new System.Drawing.Point(424, 40);
			this._btnMoveDown.Name = "_btnMoveDown";
			this._btnMoveDown.TabIndex = 2;
			this._btnMoveDown.Text = "Move Down";
			this._btnMoveDown.Click += new System.EventHandler(this._btnMoveDown_Click);
			// 
			// _btnDelete
			// 
			this._btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnDelete.Location = new System.Drawing.Point(424, 72);
			this._btnDelete.Name = "_btnDelete";
			this._btnDelete.TabIndex = 3;
			this._btnDelete.Text = "Delete";
			this._btnDelete.Click += new System.EventHandler(this._btnDelete_Click);
			// 
			// _btnRename
			// 
			this._btnRename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._btnRename.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnRename.Location = new System.Drawing.Point(424, 104);
			this._btnRename.Name = "_btnRename";
			this._btnRename.TabIndex = 4;
			this._btnRename.Text = "Rename";
			this._btnRename.Click += new System.EventHandler(this._btnRename_Click);
			// 
			// _btnHelp
			// 
			this._btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnHelp.Location = new System.Drawing.Point(424, 271);
			this._btnHelp.Name = "_btnHelp";
			this._btnHelp.TabIndex = 7;
			this._btnHelp.Text = "Help";
			this._btnHelp.Click += new System.EventHandler(this._btnHelp_Click);
			// 
			// OrganizeShortcutsDlg
			// 
			this.AcceptButton = this._btnOK;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.CancelButton = this._btnCancel;
			this.ClientSize = new System.Drawing.Size(508, 302);
			this.Controls.Add(this._btnHelp);
			this.Controls.Add(this._btnRename);
			this.Controls.Add(this._btnDelete);
			this.Controls.Add(this._btnMoveDown);
			this.Controls.Add(this._btnMoveUp);
			this.Controls.Add(this._lvShortcuts);
			this.Controls.Add(this._btnCancel);
			this.Controls.Add(this._btnOK);
			this.MinimumSize = new System.Drawing.Size(348, 200);
			this.Name = "OrganizeShortcutsDlg";
			this.Text = "Organize Shortcuts";
			this.ResumeLayout(false);

		}

	    #endregion

        public void ShowOrganizeDialog()
        {
            FillShortcutList();
            UpdateButtonState();
            if ( ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                Core.ResourceAP.RunJob( new MethodInvoker( SaveShortcutList ) );
            }
        }

	    /**
         * Fills the listview with the list of shortcuts defined in the system.
         */

        private void FillShortcutList()
        {
            _lvShortcuts.SmallImageList = Core.ResourceIconManager.ImageList;

            IResourceList shortcuts = Core.ResourceStore.GetAllResources( "Shortcut" );
            shortcuts.Sort( new[] { ShortcutProps.Order }, true );
            foreach( IResource res in shortcuts )
            {
                IResource target = res.GetLinkProp( ShortcutProps.Target );
                if ( target != null && Core.ResourceStore.ResourceTypes [target.Type].OwnerPluginLoaded )
                {
                    string name = res.GetStringProp( Core.Props.Name ) ?? target.DisplayName;
                    ListViewItem lvItem = _lvShortcuts.Items.Add( name, Core.ResourceIconManager.GetIconIndex( target ) );
                    lvItem.Tag = res.Id;
                    lvItem.SubItems.Add( target.DisplayName );
                    if ( lvItem.Index == 0 )
                    {
                        lvItem.Selected = true;
                    }
                }
            }
        }

        /**
         * Saves the order of the shortcuts and deletes the shortcuts deleted by
         * the user.
         */
        
        private void SaveShortcutList()
        {
            IResourceStore store = Core.ResourceStore;
            foreach( int deletedShortcutID in _deletedShortcutIDs )
            {
                try
                {
                    IResource shortcut = store.LoadResource( deletedShortcutID );
                    shortcut.Delete();
                }
                catch( StorageException )
                {
                    continue;
                }
            }

            for( int i=0; i<_lvShortcuts.Items.Count; i++ )
            {
                ListViewItem lvItem = _lvShortcuts.Items [i];
                int shortcutID = (int) lvItem.Tag;
                try
                {
                    IResource shortcut = store.LoadResource( shortcutID );
                    shortcut.SetProp( ShortcutProps.Order, i );
                    IResource target = shortcut.GetLinkProp( ShortcutProps.Target );
                    if ( target != null && target.DisplayName != lvItem.Text )
                    {
                        if ( shortcut.GetStringProp( "Name" ) != lvItem.Text )
                        {
                            shortcut.SetProp( ShortcutProps.Renamed, true );
                        }
                        shortcut.SetProp( "Name", lvItem.Text ); 
                    }
                    else
                    {
                        shortcut.DeleteProp( "Name" );
                    }
                }
                catch( StorageException )
                {
                    continue;
                }
            }
        }

        private void _lvShortcuts_Layout( object sender, LayoutEventArgs e )
        {
            _lvShortcuts.Columns [1].Width = _lvShortcuts.ClientSize.Width -
                                             _lvShortcuts.Columns [0].Width - 
                                             SystemInformation.VerticalScrollBarWidth;
        }

        private void _lvShortcuts_SelectedIndexChanged( object sender, System.EventArgs e )
        {
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            _btnMoveUp.Enabled = (_lvShortcuts.SelectedIndices.Count == 1 &&
                _lvShortcuts.SelectedIndices [0] > 0 );
            _btnMoveDown.Enabled = (_lvShortcuts.SelectedIndices.Count == 1 &&
                _lvShortcuts.SelectedIndices [0] < _lvShortcuts.Items.Count-1 );
            _btnDelete.Enabled = (_lvShortcuts.SelectedIndices.Count > 0 );
            _btnRename.Enabled = (_lvShortcuts.SelectedIndices.Count == 1 );
        }

        private void _btnMoveUp_Click( object sender, System.EventArgs e )
        {
            MoveSelectedItem( -1 );
        }

        private void _btnMoveDown_Click( object sender, System.EventArgs e )
        {
            MoveSelectedItem( 1 );
        }

	    private void MoveSelectedItem( int delta )
	    {
            ListViewItem lvItem = _lvShortcuts.SelectedItems [0];
            int newIndex = lvItem.Index + delta;
            _lvShortcuts.Items.Remove( lvItem );
            _lvShortcuts.Items.Insert( newIndex, lvItem );
        }

        private void _btnDelete_Click( object sender, System.EventArgs e )
        {
            for( int i=_lvShortcuts.SelectedItems.Count-1; i >= 0; i-- )
            {
                ListViewItem lvItem = _lvShortcuts.SelectedItems [i];
                _deletedShortcutIDs.Add( (int) lvItem.Tag );
                _lvShortcuts.Items.Remove( lvItem );
            }
        }

        private void _btnRename_Click( object sender, System.EventArgs e )
        {
            if ( _lvShortcuts.SelectedItems.Count == 1 )
            {
                _lvShortcuts.SelectedItems [0].BeginEdit();
            }
        }

        private void _btnHelp_Click( object sender, System.EventArgs e )
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "/organizing/using_shortcuts.html" );
        }
	}
}
