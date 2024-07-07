// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Components.CustomTreeView;

namespace JetBrains.Omea.Categories
{
    /**
     * Dialog for editing the list of categories to which a resource belongs.
     */

	public abstract class CategoryEditor : DialogBase
	{
        private const string _cIniSection = "Omea";
        private const string _cFilterWrkspKey = "FilterWorkspaceCategories";
        private const string _cSavedPrevCatIds = "PrevIDs";

        internal ResourceTreeView _categoryTree;

        private Label _lblCategoriesFor;
        private ResourceLinkLabel _lblResource;

        private Button _btnNewCategory;
        private Button _btnOk;
        private Button _btnCancel;
        private Button _btnDeleteCategory;
        private Button _btnRenameCategory;
        private Button _btnCheckAsPrevious;
        private Label  _lblPrevCats;
//        private TextBox _boxPrevCats;

        private CheckBox _chkFilterByWorkspace;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private IResourceNodeFilter _wspFilter = null;
        private ContentTypeFilter   _contentTypeFilter;
        private string[]            _contentTypes;
        private readonly bool       _filterCheckState;

        internal int                 _propCategory;
        internal IResourceList       _resList;
        internal IResourceList       _resultCats;
        internal IResourceList       _prevSessionCategories;

		public CategoryEditor()
		{
            CreateFilter();

            _filterCheckState = Core.SettingStore.ReadBool( _cIniSection, _cFilterWrkspKey, true );
			InitializeComponent();
            LoadPreviousSessionCategories();
            _lblResource.NameLabel.AllowDrop = false;
		}

        private void  LoadPreviousSessionCategories()
        {
            int currY = _lblPrevCats.Bottom + 4;
            _prevSessionCategories = Core.ResourceStore.EmptyResourceList;

            string idString = Core.SettingStore.ReadString( _cIniSection, _cSavedPrevCatIds, string.Empty );
            string[] ids = idString.Split( '#' );
            foreach( string strId in ids )
            {
                int id;
                bool succeed = Int32.TryParse( strId, out id );
                if( succeed )
                {
                    IResource cat = Core.ResourceStore.TryLoadResource( id );

                    //  Prevent dealing with dead resource or mistyped one (as in
                    //  the case of database corruption.
                    if( cat != null && !cat.IsDeleted && cat.Type == "Category" )
                    {
                        _prevSessionCategories = _prevSessionCategories.Union( cat.ToResourceList() );
                        JetLinkLabel lbl = new JetLinkLabel();
                        lbl.Location = new Point( _lblPrevCats.Left + 2, currY );
                        lbl.Text = "* " + cat.DisplayName;
                        lbl.AutoSize = false;
                        lbl.Size = new Size( 70, 16 );
                        lbl.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
                        lbl.EndEllipsis = true;
                        lbl.ClickableLink = false;
                        Controls.Add( lbl );

                        currY += lbl.Height + 4;
                    }
                }
            }
            _lblPrevCats.Visible = _btnCheckAsPrevious.Visible = (_prevSessionCategories.Count > 0);
        }

        private void  SaveSessionCategories()
        {
            IResourceList checkedCats = Core.ResourceStore.EmptyResourceList;
            GetCheckedCategories( ref checkedCats );

            string sig = string.Empty;
            foreach( IResource cat in checkedCats )
                sig += "#" + cat.Id;

            if( sig.Length > 0 )
                sig = sig.Substring( 1 );

            Core.SettingStore.WriteString( _cIniSection, _cSavedPrevCatIds, sig );
        }

        private void  CreateFilter()
        {
            IResource wsp = Core.WorkspaceManager.ActiveWorkspace;
            if( wsp != null )
                _wspFilter = new LWorkspaceCategoryFilter( wsp );
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
            _btnNewCategory = new System.Windows.Forms.Button();
            _categoryTree = new JetBrains.Omea.GUIControls.ResourceTreeView();
            _btnOk = new System.Windows.Forms.Button();
            _btnCancel = new System.Windows.Forms.Button();
            _btnDeleteCategory = new System.Windows.Forms.Button();
            _btnRenameCategory = new System.Windows.Forms.Button();
            _btnCheckAsPrevious = new Button();
            _lblCategoriesFor = new Label();
            _lblResource = new JetBrains.Omea.GUIControls.ResourceLinkLabel();
            _lblPrevCats = new Label();
            _chkFilterByWorkspace = new System.Windows.Forms.CheckBox();

            SuspendLayout();
            //
            // _lblCategoriesFor
            //
            this._lblCategoriesFor.AutoSize = true;
            this._lblCategoriesFor.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblCategoriesFor.Location = new System.Drawing.Point(4, 6);
            this._lblCategoriesFor.Name = "_lblCategoriesFor";
            this._lblCategoriesFor.Size = new System.Drawing.Size(74, 17);
            this._lblCategoriesFor.TabIndex = 6;
            this._lblCategoriesFor.Text = "Categories for";
            //
            // _lblResource
            //
            this._lblResource.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this._lblResource.ClickableLink = false;
            this._lblResource.ExecuteDoubleClickAction = false;
            this._lblResource.LinkOwnerResource = null;
            this._lblResource.LinkType = 0;
            this._lblResource.Location = new System.Drawing.Point(84, 4);
            this._lblResource.Name = "_lblResource";
            this._lblResource.PostfixText = "";
            this._lblResource.Resource = null;
            this._lblResource.ShowIcon = true;
            this._lblResource.Size = new System.Drawing.Size(23, 20);
            this._lblResource.TabIndex = 7;
            //
            // _categoryTree
            //
            this._categoryTree.AllowDrop = true;
            this._categoryTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._categoryTree.DoubleBuffer = false;
            this._categoryTree.DropOnEmpty = true;
            this._categoryTree.HideSelection = false;
            this._categoryTree.ImageIndex = -1;
            this._categoryTree.LabelEdit = true;
            this._categoryTree.Location = new System.Drawing.Point(8, 28);
            this._categoryTree.MultiSelect = false;
            this._categoryTree.Name = "_categoryTree";
            this._categoryTree.NodePainter = null;
            this._categoryTree.ParentProperty = 0;
            this._categoryTree.ResourceChildProvider = null;
            this._categoryTree.SelectedImageIndex = -1;
            this._categoryTree.SelectedNodes = new System.Windows.Forms.TreeNode[0];
            this._categoryTree.ShowRootResource = false;
            this._categoryTree.Size = new System.Drawing.Size(156, 216);
            this._categoryTree.TabIndex = 0;
            this._categoryTree.ThreeStateCheckboxes = false;
            this._categoryTree.ResourceDrop += new JetBrains.Omea.GUIControls.ResourceDragEventHandler(this._categoryTree_ResourceDrop);
            this._categoryTree.ResourceDragOver += new JetBrains.Omea.GUIControls.ResourceDragEventHandler(this._categoryTree_ResourceDragOver);
            this._categoryTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this._categoryTree_AfterSelect);
            this._categoryTree.ResourceAdded += new System.Windows.Forms.TreeViewEventHandler(this._categoryTree_ResourceAdded);
            this._categoryTree.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.OnCategoryLabelEdit);
            this._categoryTree.AfterThreeStateCheck += new ThreeStateCheckEventHandler(_categoryTree_AfterThreeStateCheck);
            //
            // _btnNewCategory
            //
            this._btnNewCategory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnNewCategory.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnNewCategory.Location = new System.Drawing.Point(172, 28);
            this._btnNewCategory.Name = "_btnNewCategory";
            this._btnNewCategory.TabIndex = 1;
            this._btnNewCategory.Text = "&New...";
            this._btnNewCategory.Click += new System.EventHandler(this.OnAddCategory);
            //
            // _btnDeleteCategory
            //
            this._btnDeleteCategory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnDeleteCategory.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnDeleteCategory.Location = new System.Drawing.Point(172, 57);
            this._btnDeleteCategory.Name = "_btnDeleteCategory";
            this._btnDeleteCategory.TabIndex = 2;
            this._btnDeleteCategory.Text = "&Delete";
            this._btnDeleteCategory.Click += new System.EventHandler(this.OnDeleteCategory);
            //
            // _btnRenameCategory
            //
            this._btnRenameCategory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnRenameCategory.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnRenameCategory.Location = new System.Drawing.Point(172, 86);
            this._btnRenameCategory.Name = "_btnRenameCategory";
            this._btnRenameCategory.TabIndex = 3;
            this._btnRenameCategory.Text = "&Rename";
            this._btnRenameCategory.Click += new System.EventHandler(this.OnRenameCategoryClick);
            //
            // _btnCheckAsPrevious
            //
            _btnCheckAsPrevious.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            _btnCheckAsPrevious.FlatStyle = FlatStyle.System;
            _btnCheckAsPrevious.Location = new Point(172, 132);
            _btnCheckAsPrevious.Name = "_btnCheckAsPrevious";
            _btnCheckAsPrevious.TabIndex = 4;
            _btnCheckAsPrevious.Visible = false;
            _btnCheckAsPrevious.Text = "&Check As";
            _btnCheckAsPrevious.Click += new System.EventHandler(OnCheckAsPreviousCaseClick);
            //
            // _lblPrevCats
            //
            _lblPrevCats.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            _lblPrevCats.AutoSize = true;
            _lblPrevCats.FlatStyle = System.Windows.Forms.FlatStyle.System;
            _lblPrevCats.Location = new System.Drawing.Point(172, 158);
            _lblPrevCats.Name = "_lblPrevCats";
            _lblPrevCats.Size = new System.Drawing.Size(80, 17);
            _lblPrevCats.TabStop = false;
            _lblPrevCats.Text = "Previous:";
            //
            // _chkFilterByWorkspace
            //
            this._chkFilterByWorkspace.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkFilterByWorkspace.Location = new System.Drawing.Point(8, 224);
            this._chkFilterByWorkspace.Name = "_chkFilterByWorkspace";
            this._chkFilterByWorkspace.Size = new System.Drawing.Size(200, 20);
            this._chkFilterByWorkspace.TabIndex = 8;
            this._chkFilterByWorkspace.Text = "Filter non-workspace categories";
            this._chkFilterByWorkspace.Visible = false;
            this._chkFilterByWorkspace.Checked = _filterCheckState;
            this._chkFilterByWorkspace.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left);
            this._chkFilterByWorkspace.CheckedChanged += new EventHandler(_chkFilterByWorkspace_CheckedChanged);
            //
            // _btnOk
            //
            this._btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOk.Location = new System.Drawing.Point(88, 254);
            this._btnOk.Name = "_btnOk";
            this._btnOk.TabIndex = 5;
            this._btnOk.Text = "OK";
            this._btnOk.Click += new System.EventHandler(this._btnOk_Click);
            //
            // _btnCancel
            //
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(172, 254);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 5;
            this._btnCancel.Text = "Cancel";
            //
            // CategoryEditor
            //
            this.AcceptButton = this._btnOk;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(256, 287);
            this.Controls.Add(this._lblResource);
            this.Controls.Add(this._lblCategoriesFor);
            this.Controls.Add(this._btnNewCategory);
            this.Controls.Add(this._btnRenameCategory);
            this.Controls.Add(this._btnDeleteCategory);
            this.Controls.Add( _btnCheckAsPrevious );
            this.Controls.Add( _lblPrevCats );
//            this.Controls.Add( _boxPrevCats );
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._categoryTree);
            this.Controls.Add(this._chkFilterByWorkspace);
            this.Name = "CategoryEditor";
            this.Text = "Assign Categories";

            if( _wspFilter != null )
            {
                _categoryTree.Height = _categoryTree.Height - 30;
                _chkFilterByWorkspace.Visible = true;

                if( _chkFilterByWorkspace.Checked )
                    _categoryTree.AddNodeFilter( _wspFilter );
            }

            this.ResumeLayout(false);
        }
		#endregion

        internal void PrepareData( IWin32Window ownerWindow, IResourceList resList )
        {
            CategoryManager mgr = Core.CategoryManager as CategoryManager;
            _resList = resList;

            _propCategory = mgr.PropCategory;

            RestoreSettings();

            _contentTypes = resList.GetAllTypes();

            _contentTypeFilter = new ContentTypeFilter();
            _contentTypeFilter.SetFilter( _contentTypes, -1 );
            _categoryTree.AddNodeFilter( _contentTypeFilter );
            _categoryTree.AddNodeFilter( new CategoryNodeFilter() );

            _categoryTree.ThreeStateCheckboxes = true;
            _categoryTree.ParentProperty = Core.Props.Parent;
            _categoryTree.OpenProperty = mgr.PropCategoryExpanded;
            _categoryTree.RootResource = Core.ResourceTreeManager.ResourceTreeRoot;
            _categoryTree.ShowContextMenu = false;

            if ( resList.Count == 1 )
            {
                _lblResource.Resource = resList [0];
            }
            else
            {
                _lblCategoriesFor.Text = "Categories for " + resList.Count + " resources:";
                _lblResource.Visible = false;
            }

            if ( _categoryTree.SelectedNode == null && _categoryTree.Nodes.Count > 0 )
            {
                _categoryTree.SelectedNode = _categoryTree.Nodes [0];
            }
            UpdateButtonState();

            _categoryTree.Focus();
        }

        /// <summary>
        /// In order to simplify dealing with multiple categories setting, clean
        /// and hide the search window so that if new category will be typed, it
        /// will start fro the beginning, without the necessity to clean the previously
        /// typed characters.
        /// </summary>
        void _categoryTree_AfterThreeStateCheck(object sender, ThreeStateCheckEventArgs e)
        {
            if( e.CheckState == NodeCheckState.Checked )
                _categoryTree.ClearSearchWindow();
        }

        private void _categoryTree_ResourceAdded(object sender, TreeViewEventArgs e)
        {
            UpdateTreeCheckbox( e.Node );
        }

        internal abstract void UpdateTreeCheckbox( TreeNode node );

        /// <summary>
        /// When the "Add" button is clicked, creates a category with the name entered
        /// by the user.
        /// </summary>
        private void OnAddCategory( object sender, EventArgs e )
        {
            IResource root = _categoryTree.SelectedResource;
            string defaultContentType = null;
            if ( root == null )
            {
                root = Core.CategoryManager.RootCategory;
            }
            else
            {
                defaultContentType = root.GetStringProp( Core.Props.ContentType );
            }

            IResource category = new NewCategoryDlg().ShowNewCategoryDialog( this, "", root, defaultContentType );
            _categoryTree.ProcessPendingUpdates();
            if ( category != null )
            {
                TreeNode node = _categoryTree.FindResourceNode( category );
                if ( node != null )
                {
                    _categoryTree.SelectedNode = node;
                    _categoryTree.SetNodeCheckState( node, NodeCheckState.Checked );
                }
            }
            UpdateButtonState();
        }

        /// <summary>
        /// When the "Rename" button is clicked, initiates the in-place edit
        /// for the selected category.
        /// </summary>
        private void OnRenameCategoryClick( object sender, EventArgs e )
        {
            if ( _categoryTree.SelectedNode != null )
            {
                //  Code below doesnot work
                //  _categoryTree.SelectedNode.BeginEdit();
                //  This (strange...) - yes.
                IResource res = _categoryTree.SelectedResource;
                _categoryTree.EditResourceLabel( res );
            }
        }

        private void OnCheckAsPreviousCaseClick( object sender, EventArgs e )
        {
            foreach( IResource cat in _prevSessionCategories )
            {
                TreeNode node = _categoryTree.FindResourceNode( cat );
                _categoryTree.SetNodeCheckState( node, NodeCheckState.Checked );
            }
        }

        /// <summary>
        /// When a category tree node is renamed, renames the respective category resource.
        /// </summary>
        private void OnCategoryLabelEdit( object sender, NodeLabelEditEventArgs e )
        {
            IResource category = (IResource) e.Node.Tag;
            if ( !Core.CategoryManager.CheckRenameCategory( this, category, e.Label ) )
            {
                e.CancelEdit = true;
            }
        }

        private void _categoryTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateButtonState();
        }

        private void _chkFilterByWorkspace_CheckedChanged(object sender, EventArgs e)
        {
            if (_chkFilterByWorkspace.Checked)
            {
                _categoryTree.AddNodeFilter(_wspFilter);
            }
            else
            {
                _categoryTree.RemoveNodeFilter(_wspFilter);
            }
            _categoryTree.UpdateNodeFilter(true);
        }


        #region Delete Category
        /**
         * When the Delete button is clicked, deletes the category selected in the tree.
         */

        private void OnDeleteCategory( object sender, EventArgs e )
        {
            DeleteSelectedCategory();
        }

        /**
         * When the Del key is pressed in the category tree, deletes the selected category.
         */

        private void DeleteSelectedCategory()
        {
            if ( _categoryTree.SelectedNode == null )
                return;

            IResource category = _categoryTree.SelectedResource;
            if ( category.Type != "Category" )
                return;

            if ( CategoryManager.ConfirmDeleteCategories( this, category.ToResourceList() ) )
            {
                Core.ResourceAP.RunJob( new ResourceDelegate( CategoryManager.DeleteCategory ), category );

                _categoryTree.ProcessPendingUpdates();
                UpdateButtonState();
            }
        }
        #endregion Delete Category

        /// <summary>
        /// If there are no categories to delete or rename, disables the Delete
        /// and Rename buttons.
        /// </summary>
        private void UpdateButtonState()
        {
            bool isEnabled = ( _categoryTree.SelectedNode != null ) &&
                             ( _categoryTree.SelectedResource.Type == "Category" );
            _btnDeleteCategory.Enabled = _btnRenameCategory.Enabled = isEnabled;
        }

        protected void  GetCheckedCategories( ref IResourceList checkedCats )
        {
            GetCheckedCategories( _categoryTree.Nodes, ref checkedCats );
        }

        private delegate List<TreeNode> checkingD( TreeNodeCollection nodes );
        private void  GetCheckedCategories( TreeNodeCollection nodes, ref IResourceList checkedCats )
        {
            List<TreeNode> checkedNodes = (List<TreeNode>)Core.UserInterfaceAP.RunUniqueJob( new checkingD( CheckedNodes ), nodes );

            foreach( TreeNode node in checkedNodes )
            {
                IResource categoryRes = (IResource) node.Tag;
                checkedCats = checkedCats.Union( categoryRes.ToResourceList() );
            }
            foreach( TreeNode node in nodes )
            {
                if ( node.Nodes.Count > 0 )
                {
                    GetCheckedCategories( node.Nodes, ref checkedCats );
                }
            }
        }

        private List<TreeNode> CheckedNodes( TreeNodeCollection nodes )
        {
            List<TreeNode> checks = new List<TreeNode>();
            foreach( TreeNode node in nodes )
            {
                NodeCheckState checkState = _categoryTree.GetNodeCheckState( node );
                if ( checkState == NodeCheckState.Checked )
                    checks.Add( node );
            }
            return checks;
        }

        #region D'n'D
        //HACK! almost copy-pasted from ResourceTreePane.cs

        private void _categoryTree_ResourceDragOver( object sender, ResourceDragEventArgs e )
        {
            if ( e.DroppedResources.Count == 0 )
            {
                e.Effect = DragDropEffects.None;
                return;
            }

//            IResource res = (e.Target != null) ? e.Target : Core.CategoryManager.RootCategory;
            IResource res = e.Target ?? Core.CategoryManager.RootCategory;
            // we always need the handler for Category resource
            IResourceUIHandler treeHandler = Core.PluginLoader.GetResourceUIHandler( e.DroppedResources [0] );
            if ( treeHandler != null && treeHandler.CanDropResources( res, e.DroppedResources ) )
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void _categoryTree_ResourceDrop( object sender, ResourceDragEventArgs e )
        {
            if ( e.DroppedResources.Count == 0 )
            {
                return;
            }

//            IResource res = (e.Target != null) ? e.Target : Core.CategoryManager.RootCategory;
            IResource res = e.Target ?? Core.CategoryManager.RootCategory;
            IResourceUIHandler treeHandler = Core.PluginLoader.GetResourceUIHandler(e.DroppedResources[0]);
            if ( treeHandler != null )
            {
                try
                {
                    treeHandler.ResourcesDropped( res, e.DroppedResources );
                }
                catch( Exception ex )
                {
                    Core.ReportException( ex, false );
                }
            }
        }
        #endregion D'n'D

        #region Save
        private void _btnOk_Click( object sender, EventArgs e )
        {
            SaveSessionCategories();
            Core.ResourceAP.RunUniqueJob( new ResourceListDelegate( SaveCategories ), _resList );
        }

        protected override void OnClosing( CancelEventArgs e )
        {
            base.OnClosing( e );
            Core.SettingStore.WriteBool( "Omea", "FilterWorkspaceCategories", _chkFilterByWorkspace.Checked );
        }

        internal abstract void SaveCategories( IResourceList list );
        #endregion Save
    }

    /// <summary>
    /// Show categories assigned to the resources from the list, and perform reassignment
    /// of the result categories to the given resources upon exit.
    /// </summary>
	public class CategoryEditorWithAssignment : CategoryEditor
	{
        public DialogResult EditCategories( IWin32Window ownerWindow, IResourceList resList )
        {
            PrepareData( ownerWindow, resList );
            return ShowDialog( ownerWindow );
        }

        internal override void UpdateTreeCheckbox( TreeNode node )
        {
            IResource category = (IResource) node.Tag;
            if ( category.Type == "Category" )
            {
                int count = GetCategoryLinkCount( _resList, category );
                if ( count == _resList.Count )
                {
                    _categoryTree.SetNodeCheckState( node, NodeCheckState.Checked );
                }
                else if ( count > 0 )
                {
                    _categoryTree.SetNodeCheckState( node, NodeCheckState.Grayed );
                }
                else
                {
                    _categoryTree.SetNodeCheckState( node, NodeCheckState.Unchecked );
                }
            }
        }

        /**
         * Checks how many of the resources in the specified list have a link
         * to the specified category.
         */

        private int GetCategoryLinkCount( IResourceList resList, IResource category )
        {
            int result = 0;
            foreach( IResource res in resList )
            {
                if ( res.HasLink( _propCategory, category ) )
                {
                    result++;
                }
            }
            return result;
        }

        /// <summary>
        /// Creates links between the active resource and the selected categories,
        /// and removes links from de-selected categories.
        /// </summary>
        internal override void SaveCategories( IResourceList resList )
        {
            ArrayList updatingResources = new ArrayList();
            foreach( IResource res in resList.ValidResources )
            {
                res.BeginUpdate();
                updatingResources.Add( res );
            }
            try
            {
                SaveCategoriesFromNodes( resList );
            }
            finally
            {
                foreach( IResource res in updatingResources )
                {
                    if ( !res.IsDeleted )
                    {
                        res.EndUpdate();
                        Core.FilterEngine.ExecRules( StandardEvents.CategoryAssigned, res );
                    }
                }
            }
        }

        /**
         * Saves the category links from the specified tree node collection.
         */

        private void SaveCategoriesFromNodes( IResourceList resList )
        {
            IResourceList checkedCats = Core.ResourceStore.EmptyResourceList;
            GetCheckedCategories( ref checkedCats );

            foreach( IResource res in resList )
            {
                IResourceList currCats = Core.CategoryManager.GetResourceCategories( res );
                IResourceList minusCats = currCats.Minus( checkedCats );

                foreach( IResource cat in checkedCats )
                    Core.CategoryManager.AddResourceCategory( res, cat );

                foreach( IResource cat in minusCats )
                    Core.CategoryManager.RemoveResourceCategory( res, cat );
            }
        }
	}

    /// <summary>
    /// Show categories assigned to the resources from the list, and collect
    /// the result list of categories for later assignment outside the dialog.
    /// </summary>
	public class CategoryEditorOnList : CategoryEditor
	{
        public DialogResult EditCategories( IWin32Window ownerWindow, IResourceList resList,
                                            IResourceList currCats, out IResourceList result )
        {
            _resultCats = currCats;
            PrepareData( ownerWindow, resList );

            DialogResult resCode = ShowDialog( ownerWindow );
            result = _resultCats;
            return resCode;
        }

        internal override void UpdateTreeCheckbox( TreeNode node )
        {
            IResource category = (IResource) node.Tag;
            if ( category.Type == "Category" )
            {
                _categoryTree.SetNodeCheckState( node, (_resultCats.IndexOf( category ) != -1) ?
                                                        NodeCheckState.Checked : NodeCheckState.Unchecked );
            }
        }

        internal override void  SaveCategories( IResourceList list )
        {
            _resultCats = Core.ResourceStore.EmptyResourceList;
            GetCheckedCategories( ref _resultCats );
        }
	}

    internal class LWorkspaceCategoryFilter : IResourceNodeFilter
    {
        readonly IResourceList listWspCategories;
        public LWorkspaceCategoryFilter( IResource wsp )
        {
            #region Preconditions
            if( wsp == null )
                throw new ArgumentNullException( "WorkspaceCategoryFilter -- Workspace resource is NULL" );
            #endregion Preconditions

            listWspCategories = wsp.GetLinksOfType( "Category", "InWorkspace" );
        }

        public bool AcceptNode( IResource res, int level )
        {
            return ( res.Type == "ResourceTreeRoot" ) || listWspCategories.Contains( res );
        }
    }
}
