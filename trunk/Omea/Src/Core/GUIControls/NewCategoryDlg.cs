/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Categories
{
	/**
     * The dialog for creating a new category. Allows to select its content type
     * and location in the resource tree.
     */
    
    public class NewCategoryDlg : DialogBase
	{
        private Label       label1;
        private TextBox     _edtName;
        private Label       label2;
        private Button      _btnOK;
        private Button      _btnCancel;
        private Label       label3;
        private ComboBox    _cmbResourceType;
        private ResourceTreeView2 _resourceTree;
        /// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private IResource _newCategoryResource;
        private readonly CategoryTypeFilter _categoryTypeFilter = new CategoryTypeFilter( new string[] {} );

        public NewCategoryDlg()
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
            this.label1 = new System.Windows.Forms.Label();
            this._edtName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this._resourceTree = new ResourceTreeView2();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this._cmbResourceType = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            // 
            // _edtName
            // 
            this._edtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtName.Location = new System.Drawing.Point(8, 26);
            this._edtName.Name = "_edtName";
            this._edtName.Size = new System.Drawing.Size(276, 21);
            this._edtName.TabIndex = 0;
            this._edtName.Text = "";
            this._edtName.TextChanged += new System.EventHandler(this._edtName_TextChanged);
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(12, 108);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(196, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Select where to place this category:";
            // 
            // _resourceTree
            // 
            this._resourceTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._resourceTree.HideSelection = false;
            this._resourceTree.Location = new System.Drawing.Point(8, 129);
            this._resourceTree.MultiSelect = false;
            this._resourceTree.Name = "_resourceTree";
            this._resourceTree.ShowContextMenu = false;
            this._resourceTree.Size = new System.Drawing.Size(276, 151);
            this._resourceTree.TabIndex = 2;
            this._resourceTree.ActiveResourceChanged += new EventHandler(this._resourceTree_AfterSelect);
            // 
            // _btnOK
            // 
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.Cursor = System.Windows.Forms.Cursors.Default;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(120, 289);
            this._btnOK.Name = "_btnOK";
            this._btnOK.Size = new System.Drawing.Size(75, 24);
            this._btnOK.TabIndex = 3;
            this._btnOK.Text = "OK";
            this._btnOK.Click += new System.EventHandler(this._btnOK_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(208, 289);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(75, 24);
            this._btnCancel.TabIndex = 4;
            this._btnCancel.Text = "Cancel";
            // 
            // label3
            // 
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(12, 56);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(268, 17);
            this.label3.TabIndex = 7;
            this.label3.Text = "Contains resources of type:";
            // 
            // _cmbResourceType
            // 
            this._cmbResourceType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._cmbResourceType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbResourceType.Location = new System.Drawing.Point(8, 78);
            this._cmbResourceType.Name = "_cmbResourceType";
            this._cmbResourceType.Size = new System.Drawing.Size(276, 21);
            this._cmbResourceType.TabIndex = 1;
            this._cmbResourceType.SelectedIndexChanged += new System.EventHandler(this._cmbResourceType_SelectedIndexChanged);
            // 
            // NewCategoryDlg
            // 
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(292, 322);
            this.Controls.Add(this._cmbResourceType);
            this.Controls.Add(this.label3);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._resourceTree);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._edtName);
            this.Controls.Add(this.label1);
            this.Name = "NewCategoryDlg";
            this.Text = "New Category";
            this.ResumeLayout(false);

        }
		#endregion

        public IResource ShowNewCategoryDialog( IWin32Window ownerWindow, string defaultName, 
            IResource defaultParent, string defaultContentType )
        {
            _edtName.Text = defaultName;
            RestoreSettings();
            _resourceTree.AddNodeFilter( _categoryTypeFilter );

            _resourceTree.RootResource = Core.ResourceTreeManager.ResourceTreeRoot;
            _resourceTree.ParentProperty = Core.Props.Parent;
            _resourceTree.OpenProperty = Core.ResourceStore.GetPropId( "CategoryExpanded" );
            _resourceTree.SelectResourceNode( defaultParent ?? Core.CategoryManager.RootCategory );

            _cmbResourceType.Items.Add( "All Resources" );

            int selectedIndex = 0;
            foreach( IResource res in ResourceTypeHelper.GetVisibleResourceTypes() )
            {
                string type = res.GetStringProp( Core.Props.Name );
                if( IsSuitableResType( type ))
                {
                    _cmbResourceType.Items.Add( res );
                    if ( type == defaultContentType )
                        selectedIndex = _cmbResourceType.Items.Count - 1;
                }
            }
            _cmbResourceType.SelectedIndex = selectedIndex;

            UpdateButtonState();

            if ( ShowDialog( ownerWindow ) == DialogResult.OK )
                Core.ResourceAP.RunJob( new MethodInvoker( DoCreateCategory ) );

            return _newCategoryResource;
        }
        
        private static bool IsSuitableResType( string type )
        {
            // (type == null) - workaround for corrupted databases
            return (type != null) && (type != "Fragment") && 
                   !Core.ResourceStore.ResourceTypes [ type ].HasFlag( ResourceTypeFlags.FileFormat ) && 
                    Core.ResourceStore.ResourceTypes [ type ].OwnerPluginLoaded;
        }

        private void _edtName_TextChanged( object sender, EventArgs e )
        {
            UpdateButtonState();        
        }

        private void _resourceTree_AfterSelect( object sender, EventArgs e )
        {
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            _btnOK.Enabled = (_edtName.Text.Trim() != "") && 
                             (_resourceTree.ActiveResource != null || _resourceTree.VisibleItemCount == 0);
        }

        private void _btnOK_Click( object sender, EventArgs e )
        {
            IResource parent = _resourceTree.ActiveResource;
            if ( (Core.CategoryManager as CategoryManager).CategoryExists( parent, _edtName.Text ) )
            {
                MessageBox.Show( this, "A category with this name already exists. Please choose a different name." );
            }
            else
            {
                DialogResult = DialogResult.OK;
            }
        }

        private void DoCreateCategory()
        {
            IResource parent = _resourceTree.ActiveResource;
            string contentType = null;
            if ( _cmbResourceType.SelectedItem is IResource )
            {
                contentType = ( _cmbResourceType.SelectedItem as IResource ).GetStringProp( Core.Props.Name );
                IResource root = Core.CategoryManager.GetRootForTypedCategory( contentType );
                if( parent == null )
                    parent = root;
            }
            _newCategoryResource = CategoryManager.CreateCategory( _edtName.Text, parent, contentType );
            Core.WorkspaceManager.AddToActiveWorkspace( _newCategoryResource );
        }

        private void _cmbResourceType_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( !(_cmbResourceType.SelectedItem is IResource ) )
            {
                _categoryTypeFilter.ResourceTypes = new string[] { null };
            }
            else
            {
                string contentType = ( _cmbResourceType.SelectedItem as IResource ).GetStringProp( Core.Props.Name );
                _categoryTypeFilter.ResourceTypes = new string[] { contentType };
            }
            _resourceTree.UpdateNodeFilter( true );
            if ( _resourceTree.Selection.Count == 0 && _resourceTree.VisibleItemCount > 0 )
            {
                _resourceTree.Selection.MoveDown();
            }
        }
	}

    public class CategoryNodeFilter: IResourceNodeFilter
    {
        public bool AcceptNode( IResource res, int level )
        {
            return ( level != 0 ) || ( res.Type == "ResourceTreeRoot" );
        }
    }

    public class CategoryTypeFilter : IResourceNodeFilter
    {
        private string[] _types;

        public CategoryTypeFilter(string[] types)
        {
            _types = types;
        }

        public string[] ResourceTypes
        {
            get { return _types; }
            set { _types = value; }
        }

        public bool AcceptNode(IResource res, int level)
        {
            if (level == 0)
            {
                if (res.Type != "ResourceTreeRoot")
                {
                    return false;
                }
                string contentType = res.GetStringProp( Core.Props.ContentType );
                for (int i = 0; i < _types.Length; i++)
                {
                    if (Object.Equals(contentType, _types[i]))
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }
    }
}
