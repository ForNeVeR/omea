// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;
using JetBrains.DataStructures;

namespace JetBrains.Omea.FilePlugin
{
    internal class SetFoldersForm : AbstractOptionsPane
    {
        private System.Windows.Forms.FolderBrowserDialog _selectFolderDialog;
        private System.Windows.Forms.Button _addBtn;
        private System.Windows.Forms.ComboBox _statusBox;
        private System.Windows.Forms.ToolTip _foldersTooltip;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.Button _deleteBtn;
        private ResourceListView2 _folderList;
        private ResourceListView2Column _folderColumn;
        private ResourceListView2Column _statusColumn;
        private IResourceList _folders;
        private HashMap _initialStatuses;
        private bool _wereChanges;
        private IResource _statusChangingFolder;

        internal SetFoldersForm()
        {
            InitializeComponent();
            _initialStatuses = new HashMap();
            _folderColumn = new ResourceListView2Column( new int[] { Core.Props.Name } );
            _folderColumn.Text = "Folder";
            _folderColumn.Width = 180;
            _folderColumn.AutoSize = true;
            _folderList.Columns.Add( _folderColumn );
            _statusColumn = new ResourceListView2Column( new int[] { FileProxy._propStatus } );
            _statusColumn.Text = "Indexed";
            _statusColumn.Width = 90;
            _statusColumn.SetPropToTextConverter( _statusColumn.PropIds[ 0 ],
                new PropertyToTextConverter( new PropertyToTextCallback( StatusToText ) ) );
            _folderList.Columns.Add( _statusColumn );
            _folderList.SelectAddedItems = true;
            _folderList.ShowContextMenu = false;
        }

        public static AbstractOptionsPane SetFoldersFormCreator()
        {
            return new SetFoldersForm();
        }

        public override void ShowPane()
        {
            _wereChanges = false;
            _statusBox.Items.AddRange( new string[] { "Immediately", "On Startup", "Never" } );
            _statusBox.SelectedIndex = 1;

            _folders = Core.ResourceStore.FindResourcesWithPropLive(
                FileProxy._folderResourceType, FileProxy._propStatus );
            _folders = _folders.Minus( Core.ResourceStore.FindResourcesWithPropLive(
                FileProxy._folderResourceType, FileProxy._propDeleted ) );

            ArrayList garbage = new ArrayList();
            foreach( IResource folder in _folders )
            {
                if( !folder.HasLink( FileProxy._propParentFolder, FoldersCollection.Instance.FilesRoot ) )
                {
                    garbage.Add( folder );
                }
            }
            foreach( IResource folder in garbage )
            {
                FoldersCollection.Instance.DeleteResource( folder );
            }

            // force monitoring of my documents if list is empty and we are in Startup Wizard
            if( IsStartupPane )
            {
                _statusColumn.Width = 0;
                _statusBox.Visible = false;
                if( _folders.Count == 0 )
                {
                    _wereChanges = true;
                    Core.ResourceAP.RunUniqueJob( new MethodInvoker( CreateMyDocumentsFolder ) );
                }
            }

            foreach( IResource folder in _folders )
            {
                _initialStatuses[ folder.GetPropText( FileProxy._propDirectory ) ] =
                    folder.GetIntProp( FileProxy._propStatus );
            }
            _folders.Sort( new int[] { Core.Props.Name }, true );
            _folderList.DataProvider = new ResourceListDataProvider( _folders );

        }

        public override void OK()
        {
            if( _wereChanges )
            {
                _wereChanges = false;
                _folderList.DataProvider = null;
                Cursor current = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    FoldersCollection.Instance.Interrupted = true;
                    FoldersCollection.Instance.WaitUntilFinished();
                    Core.ResourceAP.RunUniqueJob(
                        "Modifying settings for Indexed File Folders", new MethodInvoker( SubmitChanges ) );
                    if( !IsStartupPane )
                    {
                        FoldersCollection.LoadFoldersForest();
                    }
                }
                finally
                {
                    Cursor.Current = current;
                }
            }
        }

        public override void Cancel()
        {
            if( _wereChanges )
            {
                _folderList.DataProvider = null;
                Core.ResourceAP.RunUniqueJob(
                    "Rolling back settings for Indexed File Folders", new MethodInvoker( RollBack ) );
            }
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
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this._selectFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            this._folderList = new JetBrains.Omea.GUIControls.ResourceListView2();
            this._addBtn = new System.Windows.Forms.Button();
            this._deleteBtn = new System.Windows.Forms.Button();
            this._statusBox = new System.Windows.Forms.ComboBox();
            this._foldersTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // _folderList
            //
            this._folderList.AllowColumnReorder = false;
            this._folderList.AllowDrop = true;
            this._folderList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._folderList.AutoScroll = true;
            this._folderList.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._folderList.ColumnSchemeProvider = null;
            this._folderList.ContextProvider = this._folderList;
            this._folderList.DataProvider = null;
            this._folderList.ExecuteDoubleClickAction = false;
            this._folderList.FullRowSelect = true;
            this._folderList.HeaderContextMenu = null;
            this._folderList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Clickable;
            this._folderList.InPlaceEdit = false;
            this._folderList.Location = new System.Drawing.Point(0, 24);
            this._folderList.MultiLineView = false;
            this._folderList.MultiSelect = false;
            this._folderList.Name = "_folderList";
            this._folderList.RowDelimiters = false;
            this._folderList.Size = new System.Drawing.Size(288, 223);
            this._folderList.TabIndex = 0;
            this._folderList.KeyDown += new System.Windows.Forms.KeyEventHandler(this._folderList_KeyDown);
            this._folderList.Resize += new System.EventHandler(this._folderList_Resize);
            this._folderList.HandleCreated += new System.EventHandler(this._folderList_ListHandleCreated);
            this._folderList.ColumnSizeChanged += new System.EventHandler(this._folderList_ColumnSizeChanged);
            this._folderList.SelectionChanged += new System.EventHandler(this._folderList_SelectedIndexChanged);
            //
            // _addBtn
            //
            this._addBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._addBtn.Location = new System.Drawing.Point(0, 0);
            this._addBtn.Name = "_addBtn";
            this._addBtn.TabIndex = 1;
            this._addBtn.Text = "&Add...";
            this._addBtn.Click += new System.EventHandler(this._addBtn_Click);
            //
            // _deleteBtn
            //
            this._deleteBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._deleteBtn.Location = new System.Drawing.Point(80, 0);
            this._deleteBtn.Name = "_deleteBtn";
            this._deleteBtn.TabIndex = 2;
            this._deleteBtn.Text = "&Remove";
            this._deleteBtn.Enabled = false;
            this._deleteBtn.Click += new System.EventHandler(this._deleteBtn_Click);
            //
            // _statusBox
            //
            this._statusBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._statusBox.Location = new System.Drawing.Point(192, 0);
            this._statusBox.Name = "_statusBox";
            this._statusBox.Size = new System.Drawing.Size(90, 21);
            this._statusBox.TabIndex = 3;
            this._statusBox.Visible = false;
            this._statusBox.Leave += new System.EventHandler(this._statusBox_Leave);
            //
            // SetFoldersForm
            //
            this.Controls.Add(this._statusBox);
            this.Controls.Add(this._deleteBtn);
            this.Controls.Add(this._addBtn);
            this.Controls.Add(this._folderList);
            this.Name = "SetFoldersForm";
            this.Size = new System.Drawing.Size(288, 248);
            this.ResumeLayout(false);

        }
        #endregion

        private void _addBtn_Click( object sender, System.EventArgs e )
        {
            _selectFolderDialog.ShowNewFolderButton = true;
            if( _selectFolderDialog.SelectedPath.Length == 0 )
            {
                string lastSelectedFolder = Core.SettingStore.ReadString( "FilePlugin", "LastSelectedFolder" );
                if( lastSelectedFolder.Length == 0 )
                {
                    lastSelectedFolder = Environment.GetFolderPath( Environment.SpecialFolder.Desktop );
                }
                _selectFolderDialog.SelectedPath = lastSelectedFolder;
            }
            string description = "Select a path to be monitored";
            if( !IsStartupPane )
            {
                 description += " or excluded";
            }
            _selectFolderDialog.Description = description;
            DialogResult res;
            try
            {
                res = _selectFolderDialog.ShowDialog();
            }
            catch( Exception exc )
            {
                MessageBox.Show( this, "Selected folder could not be added.\r\n" + exc.Message, "Shell Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error );
                res = DialogResult.Cancel;
            }
            if( res == DialogResult.OK )
            {
                string path = _selectFolderDialog.SelectedPath;
                Core.SettingStore.WriteString( "FilePlugin", "LastSelectedFolder", path );
                foreach( IResource folder in _folders )
                {
                    if( String.Compare( path, folder.GetPropText( Core.Props.Name ), true ) == 0 )
                    {
                        MessageBox.Show( this, path + " is already in list",
                            "Attention", MessageBoxButtons.OK, MessageBoxIcon.Error );
                        return;
                    }
                }
                try
                {
                    DirectoryInfo di = new DirectoryInfo( path );
                    di.GetDirectories();
                    di.GetFiles();
                }
                catch( Exception exc )
                {
                    MessageBox.Show( this, exc.Message, path, MessageBoxButtons.OK, MessageBoxIcon.Error );
                    return;
                }
                _wereChanges = true;
                Core.ResourceAP.QueueJob(
                    JobPriority.Immediate, new CreateFolderDelegate( CreateFolder ), path );
            }
        }

        private void _deleteBtn_Click(object sender, System.EventArgs e)
        {
            IResourceList list = _folderList.GetSelectedResources();
            if( list.Count > 0 )
            {
                _wereChanges = true;
                Core.ResourceAP.RunUniqueJob(
                    "Mark directories deleted", new ResourceListDelegate( MarkDeleted ), list );
                Core.UserInterfaceAP.QueueJob( new MethodInvoker( UpdateDeleteButton ) );
            }
        }

        private void _folderList_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if( !Core.UserInterfaceAP.IsOwnerThread )
            {
                Core.UserInterfaceAP.QueueJob( new EventHandler( _folderList_SelectedIndexChanged ), sender, e );
                return;
            }
            IResourceList selected = _folderList.GetSelectedResources();
            if( selected.Count == 0 )
            {
                UpdateFolderStatus();
                _statusBox.Visible = false;
                _foldersTooltip.Active = false;
                _statusChangingFolder = null;
            }
            else
            {
                IResource selectedFolder = selected[ 0 ];
                _statusChangingFolder = selectedFolder;
                _statusBox.SelectedIndex = selectedFolder.GetIntProp( FileProxy._propStatus );
                _statusBox.Visible = true;
                _statusBox.DroppedDown = false;
                _foldersTooltip.Active = true;
                _foldersTooltip.SetToolTip( _folderList, selectedFolder.GetPropText( FileProxy._propDirectory ) );
                AdjustStatusBoxRectangle();
            }
            UpdateDeleteButton();
        }

        private void _folderList_Resize(object sender, System.EventArgs e)
        {
            AdjustStatusBoxRectangle();
        }

        private void _folderList_ColumnSizeChanged(object sender, System.EventArgs e)
        {
            AdjustStatusBoxRectangle();
        }

        private void _statusBox_Leave(object sender, System.EventArgs e)
        {
            UpdateFolderStatus();
        }

        private void _folderList_ListHandleCreated(object sender, System.EventArgs e)
        {
            AdjustStatusBoxRectangle();
        }

        private void _folderList_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            switch( e.KeyCode )
            {
                case Keys.Space:
                {
                    _statusBox.Focus();
                    break;
                }
                case Keys.Delete:
                {
                    _deleteBtn.PerformClick();
                    break;
                }
            }
            e.Handled = true;
        }

        private void CreateMyDocumentsFolder()
        {
            string path = Environment.GetFolderPath( Environment.SpecialFolder.Personal );
            if( Directory.Exists( path ) )
            {
                CreateFolder( path );
            }
        }

        private string StatusToText( IResource folder, int propID )
        {
            switch( folder.GetIntProp( FileProxy._propStatus ) )
            {
                case 0 : return "Immediately »";
                case 1 : return "On Startup »";
                case 2 : return "Never »";
            }
            return string.Empty;
        }

        private delegate void CreateFolderDelegate( string path );

        private void CreateFolder( string path )
        {
            IResource folder = Core.ResourceStore.FindUniqueResource(
                FileProxy._folderResourceType, FileProxy._propDirectory, path );
            if( folder != null )
            {
                folder.BeginUpdate();
            }
            else
            {
                folder = Core.ResourceStore.BeginNewResource( FileProxy._folderResourceType );
            }
            try
            {
                Core.WorkspaceManager.AddToActiveWorkspaceRecursive( folder );
                folder.SetProp( Core.Props.Name, path );
                folder.SetProp( FileProxy._propDirectory, path );
                folder.SetProp( FileProxy._propParentFolder, FoldersCollection.Instance.FilesRoot );
                folder.SetProp( FileProxy._propStatus, _statusBox.SelectedIndex );
                folder.SetProp( FileProxy._propNew, true );
                folder.SetProp( FileProxy._propDeleted, false );
            }
            finally
            {
                folder.EndUpdate();
            }
        }

        private void SubmitChanges()
        {
            IResourceList folders = Core.ResourceStore.FindResourcesWithProp(
                FileProxy._folderResourceType, FileProxy._propStatus );
            foreach( IResource folder in folders )
            {
                if( folder.HasProp( FileProxy._propDeleted ) )
                {
                    FoldersCollection.Instance.DeleteResource( folder );
                }
                else
                {
                    folder.SetProp( FileProxy._propNew, false );
                }
            }
        }

        private void RollBack()
        {
            IResourceList folders = Core.ResourceStore.FindResourcesWithProp(
                FileProxy._folderResourceType, FileProxy._propStatus );
            foreach( IResource folder in folders )
            {
                if( folder.HasProp( FileProxy._propNew ) )
                {
                    FoldersCollection.Instance.DeleteResource( folder );
                }
                else
                {
                    folder.SetProp( FileProxy._propDeleted, false );
                    folder.SetProp( FileProxy._propStatus,
                        _initialStatuses[ folder.GetPropText( FileProxy._propDirectory ) ] );
                }
            }
        }

        private void AdjustStatusBoxRectangle()
        {
            IResourceList selected = _folderList.GetSelectedResources();
            if( selected.Count > 0 )
            {
                Rectangle rect = _folderList.JetListView.GetItemBounds(
                    _folderList.NodeFromItem( selected[ 0 ] ), _statusColumn );
                _statusBox.Top = rect.Top + _folderList.Top;
                _statusBox.Left = rect.Left - 1;
                _statusBox.Width = rect.Width + 3;
                _statusBox.Height = rect.Height;
            }
        }

        private void UpdateFolderStatus()
        {
            if( _statusChangingFolder != null )
            {
                if( _statusChangingFolder.GetIntProp( FileProxy._propStatus ) != _statusBox.SelectedIndex )
                {
                    _wereChanges = true;
                    new ResourceProxy( _statusChangingFolder ).SetPropAsync( FileProxy._propStatus, _statusBox.SelectedIndex );
                }
            }
        }

        private static void MarkDeleted( IResourceList list )
        {
            foreach( IResource toBeDeleted in list )
            {
                toBeDeleted.SetProp( FileProxy._propDeleted, true );
            }
        }

        private void UpdateDeleteButton()
        {
            IResourceList selected = _folderList.GetSelectedResources();
            _deleteBtn.Enabled = selected.Count > 0;
        }

        public override string GetHelpKeyword()
        {
            return "/reference/indexed_folders.html";
        }
    }
}
