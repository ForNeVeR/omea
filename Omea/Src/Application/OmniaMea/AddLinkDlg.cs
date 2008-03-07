/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    /**
     * Dialog for creating a custom link between a list of resources on one side and a
     * target resource on another side.
     */

	public class AddLinkDlg : DialogBase
	{
        private Label label1;
        private ResourceSelBox _fromResourceSelBox;
        private Label label2;
        private ResourceSelBox _toResourceSelBox;
        private Button _btnOK;
        private Button _btnCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

        private IResourceStore _resourceStore;
        private GroupBox groupBox1;
        private ListBox _linkTypeList;
        private Button _btnAddLinkType;
        private Button _btnDeleteLinkType;
        private int _propCustom;
        
        public AddLinkDlg()
        {
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
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
            this._fromResourceSelBox = new GUIControls.ResourceSelBox();
            this.label2 = new System.Windows.Forms.Label();
            this._toResourceSelBox = new GUIControls.ResourceSelBox();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._btnDeleteLinkType = new System.Windows.Forms.Button();
            this._btnAddLinkType = new System.Windows.Forms.Button();
            this._linkTypeList = new System.Windows.Forms.ListBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Link";
            // 
            // _fromResourceSelBox
            // 
            this._fromResourceSelBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._fromResourceSelBox.Location = new System.Drawing.Point(48, 4);
            this._fromResourceSelBox.Name = "_fromResourceSelBox";
            this._fromResourceSelBox.Resource = null;
            this._fromResourceSelBox.ResourceList = null;
            this._fromResourceSelBox.ShowSelectorButton = true;
            this._fromResourceSelBox.Size = new System.Drawing.Size(216, 26);
            this._fromResourceSelBox.TabIndex = 1;
            this._fromResourceSelBox.ResourceChanged += new System.EventHandler(this.OnLinkResourceChanged);
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(28, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "to";
            // 
            // _toResourceSelBox
            // 
            this._toResourceSelBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._toResourceSelBox.Location = new System.Drawing.Point(48, 34);
            this._toResourceSelBox.Name = "_toResourceSelBox";
            this._toResourceSelBox.Resource = null;
            this._toResourceSelBox.ResourceList = null;
            this._toResourceSelBox.ShowSelectorButton = true;
            this._toResourceSelBox.Size = new System.Drawing.Size(216, 26);
            this._toResourceSelBox.TabIndex = 3;
            this._toResourceSelBox.ResourceChanged += new System.EventHandler(this.OnLinkResourceChanged);
            // 
            // _btnOK
            // 
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(104, 254);
            this._btnOK.Name = "_btnOK";
            this._btnOK.Size = new System.Drawing.Size(75, 25);
            this._btnOK.TabIndex = 4;
            this._btnOK.Text = "OK";
            this._btnOK.Click += new System.EventHandler(this._btnOK_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(188, 254);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(75, 25);
            this._btnCancel.TabIndex = 5;
            this._btnCancel.Text = "Cancel";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this._btnDeleteLinkType);
            this.groupBox1.Controls.Add(this._btnAddLinkType);
            this.groupBox1.Controls.Add(this._linkTypeList);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox1.Location = new System.Drawing.Point(8, 69);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(252, 177);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Link Type";
            // 
            // _btnDeleteLinkType
            // 
            this._btnDeleteLinkType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnDeleteLinkType.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnDeleteLinkType.Location = new System.Drawing.Point(168, 55);
            this._btnDeleteLinkType.Name = "_btnDeleteLinkType";
            this._btnDeleteLinkType.Size = new System.Drawing.Size(75, 25);
            this._btnDeleteLinkType.TabIndex = 10;
            this._btnDeleteLinkType.Text = "Delete";
            this._btnDeleteLinkType.Click += new System.EventHandler(this._btnDeleteLinkType_Click);
            // 
            // _btnAddLinkType
            // 
            this._btnAddLinkType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnAddLinkType.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnAddLinkType.Location = new System.Drawing.Point(168, 20);
            this._btnAddLinkType.Name = "_btnAddLinkType";
            this._btnAddLinkType.Size = new System.Drawing.Size(75, 25);
            this._btnAddLinkType.TabIndex = 9;
            this._btnAddLinkType.Text = "New";
            this._btnAddLinkType.Click += new System.EventHandler(this.OnAddLinkType);
            // 
            // _linkTypeList
            // 
            this._linkTypeList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._linkTypeList.IntegralHeight = false;
            this._linkTypeList.Location = new System.Drawing.Point(8, 20);
            this._linkTypeList.Name = "_linkTypeList";
            this._linkTypeList.Size = new System.Drawing.Size(152, 148);
            this._linkTypeList.TabIndex = 7;
            this._linkTypeList.DoubleClick += new System.EventHandler(this.OnLinkTypeDoubleClick);
            this._linkTypeList.SelectedIndexChanged += new System.EventHandler(this.OnLinkResourceChanged);
            // 
            // AddLinkDlg
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(272, 287);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._toResourceSelBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._fromResourceSelBox);
            this.Controls.Add(this.label1);
            this.Name = "AddLinkDlg";
            this.Text = "Add Link";
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        public void ShowAddLinkDialog( IWin32Window ownerWindow, IResourceList sourceList, IResource target )
        {
            _resourceStore = Core.ResourceStore;
            _propCustom = _resourceStore.GetPropId( "Custom" );

            RestoreSettings();

            _fromResourceSelBox.ResourceList = sourceList;
            _toResourceSelBox.Resource       = target;
            _fromResourceSelBox.ShowSelectorButton = false;
            _toResourceSelBox.ShowSelectorButton = false;

            _btnOK.Enabled = false;

            IResourceList linkTypes = _resourceStore.FindResources( "PropType", _propCustom, 1 );
            linkTypes.Sort( new SortSettings( Core.Props.Name, true ) );
            foreach( IResource linkTypeRes in linkTypes )
            {
                if ( linkTypeRes.GetIntProp( "DataType") == (int) PropDataType.Link )
                {
                    _linkTypeList.Items.Add( linkTypeRes );
                }
            }
            if ( _linkTypeList.Items.Count > 0 )
            {
                _linkTypeList.SelectedIndex = 0;
            }

            UpdateButtonState();

            if ( ShowDialog( ownerWindow ) == DialogResult.OK )
            {
                IResource linkTypeRes = (IResource) _linkTypeList.SelectedItem;
                Core.ResourceAP.QueueJob( new AddLinkDelegate( DoAddLink ),
                    _resourceStore,
                    _fromResourceSelBox.ResourceList, _toResourceSelBox.Resource.Id,
                    linkTypeRes.GetIntProp( "Id" ) );
            }
        }

        private static void DoAddLink( IResourceStore store, IResourceList sourceList, int targetID, int linkID )
        {
            IResource target = store.LoadResource( targetID );
            foreach( IResource res in sourceList )
            {
                if ( res.Id != target.Id )
                {
                    res.AddLink( linkID, target );
                }
            }
        }

        private void OnAddLinkType( object sender, EventArgs e )
        {
            string linkTypeName = Core.UIManager.InputString( "Create Link Type",
                                    "Enter the link type name:", "", ValidateDelegate, this );
            if ( linkTypeName != null )
            {
                CheckCreateLinkType( linkTypeName );
            }
        }

	    private static void ValidateDelegate( string value, ref string validateErrorMessage )
	    {
            if ( Core.ResourceStore.PropTypes.Exist( value ) )
            {
                validateErrorMessage = "A link type with that name already exists";
            }
	    }

	    /**
         * If the name of the link type entered by the user does not exist, creates
         * the link type. Otherwise, reports the error and removes the last item from the list.
         */

        private void CheckCreateLinkType( string name )
        {
            Core.ResourceAP.RunJob( new StringDelegate( DoAddLinkType ), name );

            IResource res = _resourceStore.FindUniqueResource( "PropType", "Name", name );
            _linkTypeList.Items.Add( res );
            _linkTypeList.SelectedItem = res;
        }

        private void DoAddLinkType( string name )
        {
            _resourceStore.PropTypes.Register( name, PropDataType.Link );
            IResource res = _resourceStore.FindUniqueResource( "PropType", "Name", name );
            Debug.Assert( res != null );
            res.SetProp( _propCustom, 1 );
        }

        /**
         * Enables the OK button only when both resource select boxes are filled
         * and a link type is selected in the list.
         */
        
        private void OnLinkResourceChanged( object sender, EventArgs e )
        {
            UpdateButtonState();
        }

        /**
         * When the link type list is double-clicked, if the OK buttom is
         * enabled, completes the link creation.
         */
        
        private void OnLinkTypeDoubleClick( object sender, EventArgs e )
        {
            if ( _btnOK.Enabled )
                CheckCreateLink();
        }

        private void _btnOK_Click( object sender, EventArgs e )
        {
            CheckCreateLink();
        }

        private void CheckCreateLink()
        {
            if ( _fromResourceSelBox.Resource != null && 
                _fromResourceSelBox.Resource.Id == _toResourceSelBox.Resource.Id )
            {
                MessageBox.Show( this,
                    "You cannot link a resource to itself",
                    "Add Link", MessageBoxButtons.OK );
                return;
            }
            DialogResult = DialogResult.OK;
        }

        private void _btnDeleteLinkType_Click( object sender, EventArgs e )
        {
            if ( _linkTypeList.SelectedIndex < 0 )
                return;

            IResource res = (IResource) _linkTypeList.SelectedItem;
            IResourceList resLinks = _resourceStore.FindResourcesWithProp( null, res.GetIntProp( "ID" ) );
            if ( resLinks.Count > 0 )
            {
                DialogResult dr = MessageBox.Show( this,
                    "There are " + resLinks.Count + " resources which have links of type " + res.DisplayName +
                    ". Are you sure you wish to delete the link type?",
                    "Delete Link Type",
                    MessageBoxButtons.YesNo );
                if ( dr != DialogResult.Yes )
                {
                    return;
                }
            }

            _linkTypeList.Items.Remove( res );
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                Core.ResourceAP.RunJob( new ResourceDelegate( DoDeleteLinkType ), res );
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private static void DoDeleteLinkType( IResource res )
        {
            int propID = res.GetIntProp( "ID" );
            Core.ResourceStore.PropTypes.Delete( propID );
        }

        /**
         * Disables the Delete button if no link type is selected in the list.
         * Enables the OK button when both source and target resources have been
         * selected.
         */

        private void UpdateButtonState()
        {
            _btnOK.Enabled = ( _fromResourceSelBox.ResourceList != null ) &&
                ( _fromResourceSelBox.ResourceList.Count > 0 ) &&
                ( _toResourceSelBox.Resource != null ) &&
                ( _linkTypeList.SelectedIndex >= 0 );

            if ( _linkTypeList.SelectedIndex >= 0 )
            {
                _btnDeleteLinkType.Enabled = true;
            }
            else
            {
                _btnDeleteLinkType.Enabled = false;
            }
        }

        private delegate void StringDelegate( string name );
        private delegate void AddLinkDelegate( IResourceStore store, IResourceList sourceList, int targetID, int linkID );
	}
}
