/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.ContactsPlugin
{
	public class MergeContactsForm : DialogBase
	{
        private Button _btn_Cancel;
        private Button _btn_OK;
        private TextBox textFullName;
        private Label label1;
        private Button buttonAddContact;
        private Button buttonRemoveContact;
        private Label labelOtherContacts;
        private CheckBox checkShowOrigNames;

        private IResourceList   _suggestedContactList;
        private IResourceList   _contactsToMergeList;

        private ResourceListView2 _suggestedContacts;
        private ResourceListView2 _contactsToMerge;

        private Label _lblContacts2Merge;
        private Button _btnMoreContacts;
        private Label _lblError;
        private ContextMenuStrip _menu;
        private ToolStripMenuItem _miShowContact;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        public string FullName              {  get{ return textFullName.Text;     }  }
        public bool   ShowOriginalNames     {  get{ return checkShowOrigNames.Checked;  }   }
        public IResourceList ResultContacts {  get{ return _contactsToMergeList;  }  }

        #region Ctor and Initialization
		public MergeContactsForm( IResourceList contacts, IResourceList defaultContactsToMerge )
		{
			InitializeComponent();
            InitializeColumns();
            InitializeContent( contacts, defaultContactsToMerge );
            SetOrigNames( contacts );
            VerifyButtonsCondition();
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

        private void  InitializeColumns()
        {
            ResourceListView2Column nameCol;

            _suggestedContacts.Columns.Add( new ResourceIconColumn() );
            nameCol = _suggestedContacts.AddColumn( ResourceProps.DisplayName );
            nameCol.AutoSize = true;

            _contactsToMerge.Columns.Add( new ResourceIconColumn() );
            nameCol = _contactsToMerge.AddColumn( ResourceProps.DisplayName );
            nameCol.AutoSize = true;
        }

        private void  InitializeContent( IResourceList contacts, IResourceList defaults )
        {
            _suggestedContactList = contacts ?? Core.ResourceStore.EmptyResourceList;
            foreach( IResource res in _suggestedContactList )
                _suggestedContacts.JetListView.Nodes.Add( res );

            _contactsToMergeList = defaults ?? Core.ResourceStore.EmptyResourceList;
            foreach( IResource res in _contactsToMergeList )
                _contactsToMerge.JetListView.Nodes.Add( res );
        }
        #endregion Ctor and Initialization

        private void  SetOrigNames( IResourceList contacts )
        {
            //  Set option "ON" if any of the contacts to merge has already
            //  this option set.
            bool showOrigNames = false;
            if( contacts != null )
            {
                foreach( IResource res in contacts )
                    showOrigNames = showOrigNames || res.HasProp( "ShowOriginalNames" );
            }
            checkShowOrigNames.Checked = showOrigNames;
        }

        #region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._btn_Cancel = new System.Windows.Forms.Button();
            this._btn_OK = new System.Windows.Forms.Button();
            this.textFullName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonAddContact = new System.Windows.Forms.Button();
            this.buttonRemoveContact = new System.Windows.Forms.Button();
            this.labelOtherContacts = new System.Windows.Forms.Label();
            this.checkShowOrigNames = new System.Windows.Forms.CheckBox();
            this._suggestedContacts = new ResourceListView2();
            this._contactsToMerge = new ResourceListView2();
            this._menu = new System.Windows.Forms.ContextMenuStrip();
            this._miShowContact = new ToolStripMenuItem();
            this._lblContacts2Merge = new System.Windows.Forms.Label();
            this._btnMoreContacts = new System.Windows.Forms.Button();
            this._lblError = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _btn_Cancel
            // 
            this._btn_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btn_Cancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btn_Cancel.Location = new System.Drawing.Point(332, 364);
            this._btn_Cancel.Name = "_btn_Cancel";
            this._btn_Cancel.Size = new System.Drawing.Size(75, 25);
            this._btn_Cancel.TabIndex = 11;
            this._btn_Cancel.Text = "Cancel";
            // 
            // _btn_OK
            // 
            this._btn_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btn_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btn_OK.Enabled = false;
            this._btn_OK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btn_OK.Location = new System.Drawing.Point(248, 364);
            this._btn_OK.Name = "_btn_OK";
            this._btn_OK.Size = new System.Drawing.Size(75, 25);
            this._btn_OK.TabIndex = 10;
            this._btn_OK.Text = "&Merge";
            this._btn_OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // textFullName
            // 
            this.textFullName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.textFullName.Location = new System.Drawing.Point(160, 8);
            this.textFullName.Name = "textFullName";
            this.textFullName.ReadOnly = true;
            this.textFullName.Size = new System.Drawing.Size(248, 21);
            this.textFullName.TabIndex = 1;
            this.textFullName.TabStop = false;
            this.textFullName.Text = "";
            this.textFullName.TextChanged += new System.EventHandler(this.NameChanged);
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(140, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Merged Contact &Name:";
            // 
            // buttonAddContact
            // 
            this.buttonAddContact.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonAddContact.Location = new System.Drawing.Point(168, 76);
            this.buttonAddContact.Name = "buttonAddContact";
            this.buttonAddContact.TabIndex = 5;
            this.buttonAddContact.Text = ">>";
            this.buttonAddContact.Click += new System.EventHandler(this.buttonAddContact_Click);
            // 
            // buttonRemoveContact
            // 
            this.buttonRemoveContact.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonRemoveContact.Location = new System.Drawing.Point(168, 104);
            this.buttonRemoveContact.Name = "buttonRemoveContact";
            this.buttonRemoveContact.TabIndex = 6;
            this.buttonRemoveContact.Text = "<<";
            this.buttonRemoveContact.Click += new System.EventHandler(this.buttonRemoveContact_Click);
            // 
            // labelOtherContacts
            // 
            this.labelOtherContacts.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelOtherContacts.Location = new System.Drawing.Point(8, 56);
            this.labelOtherContacts.Name = "labelOtherContacts";
            this.labelOtherContacts.Size = new System.Drawing.Size(112, 18);
            this.labelOtherContacts.TabIndex = 3;
            this.labelOtherContacts.Text = "&Suggested Contacts:";
            // 
            // checkShowOrigNames
            // 
            this.checkShowOrigNames.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkShowOrigNames.Location = new System.Drawing.Point(8, 32);
            this.checkShowOrigNames.Name = "checkShowOrigNames";
            this.checkShowOrigNames.Size = new System.Drawing.Size(380, 18);
            this.checkShowOrigNames.TabIndex = 2;
            this.checkShowOrigNames.Text = "&Show name used in messages addressed to this contact";
            // 
            // _suggestedContacts2
            // 
            this._suggestedContacts.AllowColumnReorder = false;
            this._suggestedContacts.AllowDrop = true;
            this._suggestedContacts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left)));
            this._suggestedContacts.ContextMenuStrip = _menu;
            this._suggestedContacts.ExecuteDoubleClickAction = false;
            this._suggestedContacts.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._suggestedContacts.HideSelection = false;
            this._suggestedContacts.Location = new System.Drawing.Point(4, 76);
            this._suggestedContacts.Name = "_suggestedContacts";
            this._suggestedContacts.Size = new System.Drawing.Size(160, 260);
            this._suggestedContacts.ShowContextMenu = false;
            this._suggestedContacts.TabIndex = 4;
            this._suggestedContacts.DoubleClick += new JetBrains.JetListViewLibrary.HandledEventHandler( buttonAddContact_Click );
            // 
            // _menu
            // 
            this._menu.Items.Add( _miShowContact );
            this._menu.Opening += new System.ComponentModel.CancelEventHandler( contextMenu1_Opening );
            // 
            // menuItem2
            // 
            this._miShowContact.Text = "Show Contact...";
            this._miShowContact.Click += new System.EventHandler(this.itemShowContact_Click);
            // 
            // _contactsToMerge2
            // 
            this._contactsToMerge.AllowColumnReorder = false;
            this._contactsToMerge.AllowDrop = true;
            this._contactsToMerge.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._contactsToMerge.ContextMenuStrip = _menu;
            this._contactsToMerge.ExecuteDoubleClickAction = false;
            this._contactsToMerge.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._contactsToMerge.HideSelection = false;
            this._contactsToMerge.Location = new System.Drawing.Point(248, 76);
            this._contactsToMerge.Name = "_contactsToMerge";
            this._contactsToMerge.Size = new System.Drawing.Size(160, 232);
            this._contactsToMerge.TabIndex = 8;
            this._contactsToMerge.SelectionChanged += new EventHandler(this.OnSelectContact);
            this._contactsToMerge.DoubleClick += new JetBrains.JetListViewLibrary.HandledEventHandler(buttonRemoveContact_Click);
            // 
            // _lblContacts2Merge
            // 
            this._lblContacts2Merge.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblContacts2Merge.Location = new System.Drawing.Point(248, 56);
            this._lblContacts2Merge.Name = "_lblContacts2Merge";
            this._lblContacts2Merge.Size = new System.Drawing.Size(112, 18);
            this._lblContacts2Merge.TabIndex = 7;
            this._lblContacts2Merge.Text = "&Contacts to Merge:";
            // 
            // _btnMoreContacts
            // 
            this._btnMoreContacts.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._btnMoreContacts.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnMoreContacts.Location = new System.Drawing.Point(248, 312);
            this._btnMoreContacts.Name = "_btnMoreContacts";
            this._btnMoreContacts.Size = new System.Drawing.Size(120, 23);
            this._btnMoreContacts.TabIndex = 9;
            this._btnMoreContacts.Text = "&More Contacts...";
            this._btnMoreContacts.Click += new System.EventHandler(this._btnMoreContacts_Click);
            // 
            // _lblError
            // 
            this._lblError.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._lblError.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblError.ForeColor = System.Drawing.Color.Red;
            this._lblError.Location = new System.Drawing.Point(8, 340);
            this._lblError.Name = "_lblError";
            this._lblError.Size = new System.Drawing.Size(400, 16);
            this._lblError.TabIndex = 12;
            this._lblError.Text = "label3";
            this._lblError.Visible = false;
            // 
            // MergeContactsForm
            // 
            this.AcceptButton = this._btn_OK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btn_Cancel;
            this.ClientSize = new System.Drawing.Size(416, 398);
            this.Controls.Add(this._lblError);
            this.Controls.Add(this._btnMoreContacts);
            this.Controls.Add(this._lblContacts2Merge);
            this.Controls.Add(this._contactsToMerge);
            this.Controls.Add(this._suggestedContacts);
            this.Controls.Add(this.checkShowOrigNames);
            this.Controls.Add(this.buttonAddContact);
            this.Controls.Add(this._btn_Cancel);
            this.Controls.Add(this._btn_OK);
            this.Controls.Add(this.textFullName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonRemoveContact);
            this.Controls.Add(this.labelOtherContacts);
            this.Name = "MergeContactsForm";
            this.Text = "Merge Contacts";
            this.ResumeLayout(false);

        }
		#endregion

        #region OK/Cancel
        private void OK_Click( object sender, EventArgs e )
        {
            DialogResult = DialogResult.OK;
            bool isMyself = false;
            if( isMyself && FullName != Core.ContactManager.GetFullName( Core.ContactManager.MySelf.Resource ) )
            {
                DialogResult dr = MessageBox.Show( "Name which represents you as a contact will be changed. Proceed further?",
                                                   "Names Collision", MessageBoxButtons.OKCancel, MessageBoxIcon.Question );
                if( dr == DialogResult.Cancel )
                {
                    DialogResult = DialogResult.None;
                }
            }
        }
        #endregion OK/Cancel
    
        #region Event Handlers
        private void OnSelectContact( object sender, EventArgs e )
        {
            IResourceList sel = _contactsToMerge.GetSelectedResources();
            if( sel.Count > 0 )
            {
                textFullName.Text = sel[ 0 ].DisplayName;
            }
            VerifyButtonsCondition();
        }

        private void NameChanged(object sender, EventArgs e)
        {
            VerifyButtonsCondition();
        }

        private void buttonAddContact_Click(object sender, EventArgs e)
        {
            IResourceList sel = _suggestedContacts.GetSelectedResources();
            for( int i = 0; i < sel.Count; i++ )
            {
                _contactsToMergeList = _contactsToMergeList.Union( sel[ i ].ToResourceList() );
                _suggestedContactList = _suggestedContactList.Minus( sel[ i ].ToResourceList() );
                _contactsToMerge.JetListView.Nodes.Add( sel[ i ] );
                _suggestedContacts.JetListView.Nodes.Remove( sel[ i ] );
            }
            VerifyButtonsCondition();
        }
        private void buttonAddContact_Click(object sender, HandledEventArgs e)
        {
            buttonAddContact_Click( sender, (EventArgs)e );
        }

        private void buttonRemoveContact_Click( object sender, EventArgs e )
        {
            IResourceList sel = _contactsToMerge.GetSelectedResources();
            for( int i = 0; i < sel.Count; i++ )
            {
                _suggestedContactList = _suggestedContactList.Union( sel );
                _contactsToMergeList = _contactsToMergeList.Minus( sel );
                _suggestedContacts.JetListView.Nodes.Add( sel[ i ] );
                _contactsToMerge.JetListView.Nodes.Remove( sel[ i ] );
            }
            VerifyButtonsCondition();
        }

        private void buttonRemoveContact_Click( object sender, HandledEventArgs e )
        {
            buttonRemoveContact_Click( sender, (EventArgs)e );
        }

	    public void _btnMoreContacts_Click( object sender, EventArgs e )
	    {
            IResourceList contactsList = Core.ResourceStore.GetAllResources( "Contact" );
            contactsList = contactsList.Minus( _contactsToMergeList );
            contactsList.Sort( new SortSettings( ResourceProps.DisplayName, true ) );
            IResourceList contacts = Core.UIManager.SelectResourcesFromList( this, contactsList, 
                                     "Select Contact(s) for Merging" );
            if ( contacts != null )
            {
                foreach( IResource res in contacts )
                {
                    _contactsToMerge.JetListView.Nodes.Add( res );
                }
                _contactsToMergeList = _contactsToMergeList.Union( contacts );
            }
            VerifyButtonsCondition();
        }
        #endregion Event Handlers

        #region Verify Buttons
        private void VerifyButtonsCondition()
        {
            buttonAddContact.Enabled = (_suggestedContactList.Count > 0);
            buttonRemoveContact.Enabled = (_contactsToMergeList.Count > 0);

            if ( textFullName.Text.Length == 0 || _contactsToMergeList.Count < 2 )
            {
                _lblError.Visible = false;
                _btn_OK.Enabled = false;                
            }
            else
            {
                IContactMergeFilter[] filters = Core.ContactManager.GetContactMergeFilters();
                foreach( IContactMergeFilter filter in filters )
                {
                    string errMsg = filter.CheckMergeAllowed( _contactsToMergeList );
                    if ( errMsg != null )
                    {
                        _lblError.Text = errMsg;
                        _lblError.Visible = true;
                        _btn_OK.Enabled = false;
                        return;
                    }
                }
                _lblError.Visible = false;
                _btn_OK.Enabled = true;
            }
        }
        #endregion Verify Buttons

        private void contextMenu1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Control ctrl = ((ContextMenuStrip)sender).SourceControl;
            int count = ((ResourceListView2)ctrl).Selection.Count;
            _miShowContact.Enabled = (count == 1);
        }

        private void itemShowContact_Click(object sender, EventArgs e)
        {
            Control ctrl = ((MenuItem)sender).Parent.GetContextMenu().SourceControl;
            ResourceListView2 list = (ResourceListView2)ctrl;
            int count = list.Selection.Count;
            if( count == 1 )
            {
                IResourceList sel = list.GetSelectedResources();
                ContactView cv = new ContactView();
                Core.UIManager.OpenResourceEditWindow( cv, sel[ 0 ], false );
            }
        }
	}
}
