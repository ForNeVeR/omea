// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /**
     * Contact view block for editing email address information.
     */

    public class EmailBlock: AbstractContactViewBlock
    {
        private const string DefltMarker = "Default";
        private const string _enterAddressString = "Enter address";

        private ListView _emailsList;
        private Container components = null;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ContextMenu _emailListcontextMenu;
        private MenuItem _copyMenuItem;

        private Label _lblNoEmail;
        private Button _addButton;
        private Button _removeButton;
        private Button _setDefaultButton;

        private IResource _contact;
        private HashSet _emailSet, _originalAccounts;
        private bool _isStartupMode;

        #region Ctor and Initialization
        public EmailBlock()
        {
            InitializeComponent();
        }

        public static AbstractContactViewBlock CreateBlock()
        {
            return new EmailBlock();
        }

        public void  SetStartupMode()
        {
            _isStartupMode = true;
            _emailsList.CheckBoxes = false;
        }

        public override void EditResource( IResource contact )
        {
            _contact = contact;
            _emailSet = new HashSet();
            _originalAccounts = new HashSet();
            _emailsList.Items.Clear();
            _emailsList.CheckBoxes = contact.HasProp( Core.ContactManager.Props.Myself );

            IResourceList emails = contact.GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct );

            foreach( IResource email in emails )
            {
                string emailText = email.GetPropText( Core.ContactManager.Props.EmailAddress );
                bool   isPersonal = email.HasProp( Core.ContactManager.Props.PersonalAccount );
                if( emailText.Length > 0 )
                {
                    ListViewItem item = new ListViewItem();
                    item.Checked = isPersonal;
                    item.Text = emailText;
                    item.SubItems.Add( email.HasLink( ContactManager._propDefaultAccount, contact ) ? DefltMarker : "" );

                    if ( _emailsList.SmallImageList != null )
                        item.ImageIndex = Core.ResourceIconManager.GetIconIndex( email );

                    _emailsList.Items.Add( item );
                    _originalAccounts.Add( emailText );
                    _emailSet.Add( emailText );
                }
            }
            ChooseDefaultAccount();
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
        #endregion Ctor and Initialization

        #region Save
        public override void Save()
        {
            Save( _contact );
        }
        public void Save( IResource contact )
        {
            ResourceProxy proxy = new ResourceProxy( contact );
            proxy.BeginUpdate();
            try
            {
                proxy.AsyncPriority = JobPriority.Immediate;
                for( int i = 0; i < _emailsList.Items.Count; ++i )
                {
                    ListViewItem item = _emailsList.Items[ i ];
                    IResource account = (IResource) Core.ResourceAP.RunUniqueJob(
                        new FindOrCreateEmailAccountDelegate( Core.ContactManager.FindOrCreateEmailAccount ), item.Text );
                    proxy.AddLink( Core.ContactManager.Props.LinkEmailAcct, account );
                    _originalAccounts.Remove( item.Text );
                    if( item.SubItems[ 1 ].Text == DefltMarker )
                    {
                        proxy.SetProp( ContactManager._propDefaultAccount, account );
                    }
                    new ResourceProxy( account ).SetProp( Core.ContactManager.Props.PersonalAccount, _isStartupMode || item.Checked );
                }

                //  Delete links to those accounts which were originally
                //  connected to the contact and did not keep their place in
                //  the list (were removed or renamed).
                foreach( HashSet.Entry e in _originalAccounts )
                {
                    IResource accnt = Core.ResourceStore.FindUniqueResource(
                        "EmailAccount", Core.ContactManager.Props.EmailAddress, e.Key );
                    if( accnt != null )
                    {
                        Core.ResourceAP.RunUniqueJob( new SplitDelegate( HardRemoveAccountFromContact ), accnt, contact );
                    }
                }
            }
            finally
            {
                proxy.EndUpdateAsync();
            }
        }

        private delegate void SplitDelegate( IResource contact, IResource account );

    	private static void HardRemoveAccountFromContact(IResource accnt, IResource contact)
    	{
    		Core.UIManager.RunWithProgressWindow("Removing Account from a Contact", delegate { ((ContactManager)Core.ContactManager).HardRemoveAccountFromContact(contact, accnt); });
    	}

        private delegate IResource FindOrCreateEmailAccountDelegate(string email);

        #endregion Save

        public override bool OwnsProperty( int propId )
        {
            return propId == Core.ContactManager.Props.LinkEmailAcct;
        }

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._emailsList = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this._emailListcontextMenu = new System.Windows.Forms.ContextMenu();
            this._copyMenuItem = new System.Windows.Forms.MenuItem();
            this._lblNoEmail = new System.Windows.Forms.Label();
            this._addButton = new System.Windows.Forms.Button();
            this._removeButton = new System.Windows.Forms.Button();
            this._setDefaultButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // _emailsList
            //
            this._emailsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._emailsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.columnHeader1,
                                                                                        this.columnHeader2});
            this._emailsList.ContextMenu = this._emailListcontextMenu;
//            this._emailsList.CheckBoxes = true;
            this._emailsList.FullRowSelect = true;
            this._emailsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this._emailsList.HideSelection = false;
            this._emailsList.LabelEdit = true;
            this._emailsList.Location = new System.Drawing.Point(0, 0);
            this._emailsList.MultiSelect = true;
            this._emailsList.Name = "_emailsList";
            this._emailsList.Size = new System.Drawing.Size(188, 112);
            this._emailsList.TabIndex = 0;
            this._emailsList.View = System.Windows.Forms.View.Details;
            this._emailsList.KeyDown += new System.Windows.Forms.KeyEventHandler(this._emailsList_KeyDown);
            this._emailsList.Resize += new System.EventHandler(this._emailsList_Resize);
            this._emailsList.DoubleClick += new System.EventHandler(this._emailsList_DoubleClick);
            this._emailsList.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this._emailsList_AfterLabelEdit);
            this._emailsList.BeforeLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this._emailsList_BeforeLabelEdit);
            this._emailsList.SelectedIndexChanged += new System.EventHandler(this._emailsList_SelectedIndexChanged);
            //
            // columnHeader1
            //
            this.columnHeader1.Text = "Address";
            this.columnHeader1.Width = 100;
            //
            // columnHeader1
            //
            this.columnHeader2.Text = "Default";
            this.columnHeader2.Width = 60;
            //
            // _emailListcontextMenu
            //
            this._emailListcontextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                                  this._copyMenuItem});
            this._emailListcontextMenu.Popup += new System.EventHandler(this._emailListcontextMenu_Popup);
            //
            // _copyMenuItem
            //
            this._copyMenuItem.Index = 0;
            this._copyMenuItem.Text = "Copy";
            this._copyMenuItem.Click += new System.EventHandler(this._copyMenuItem_Click);
            //
            // _lblNoEmail
            //
            this._lblNoEmail.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblNoEmail.ForeColor = System.Drawing.SystemColors.GrayText;
            this._lblNoEmail.Location = new System.Drawing.Point(108, 4);
            this._lblNoEmail.Name = "_lblNoEmail";
            this._lblNoEmail.Size = new System.Drawing.Size(168, 20);
            this._lblNoEmail.TabIndex = 2;
            this._lblNoEmail.Text = "Not specified";
            this._lblNoEmail.Visible = false;
            //
            // _addButton
            //
            this._addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._addButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._addButton.Location = new System.Drawing.Point(196, 0);
            this._addButton.Name = "_addButton";
            this._addButton.Size = new System.Drawing.Size(84, 23);
            this._addButton.TabIndex = 3;
            this._addButton.Text = "&Add...";
            this._addButton.Click += new System.EventHandler(this._addMenuItem_Click);
            //
            // _removeButton
            //
            this._removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._removeButton.Enabled = false;
            this._removeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._removeButton.Location = new System.Drawing.Point(196, 28);
            this._removeButton.Name = "_removeButton";
            this._removeButton.Size = new System.Drawing.Size(84, 23);
            this._removeButton.TabIndex = 4;
            this._removeButton.Text = "&Remove";
            this._removeButton.Click += new System.EventHandler(this._removeButton_Click);
            //
            // _setDefaultButton
            //
            this._setDefaultButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._setDefaultButton.Enabled = false;
            this._setDefaultButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._setDefaultButton.Location = new System.Drawing.Point(196, 56);
            this._setDefaultButton.Name = "_setDefaultButton";
            this._setDefaultButton.Size = new System.Drawing.Size(84, 23);
            this._setDefaultButton.TabIndex = 5;
            this._setDefaultButton.Text = "Set &Default";
            this._setDefaultButton.Click += new System.EventHandler(this._defaultMenuItem_Click);
            //
            // EmailBlock
            //
            this.Controls.Add(this._setDefaultButton);
            this.Controls.Add(this._removeButton);
            this.Controls.Add(this._addButton);
            this.Controls.Add(this._lblNoEmail);
            this.Controls.Add(this._emailsList);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.Name = "EmailBlock";
            this.Size = new System.Drawing.Size(284, 112);
            this.ResumeLayout(false);
        }
        #endregion

        #region Basic Button and Menu Actions
        private void _defaultMenuItem_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection selected = _emailsList.SelectedItems;
            if( selected.Count == 1 )
            {
                for( int i = 0; i < _emailsList.Items.Count; ++i )
                    _emailsList.Items[ i ].SubItems[ 1 ].Text = "";
                selected[ 0 ].SubItems[ 1 ].Text = DefltMarker;
            }
        }

        private void _removeButton_Click(object sender, EventArgs e)
        {
            DeleteSelectedItems();
        }

        private void _copyMenuItem_Click(object sender, EventArgs e)
        {
            if( _emailsList.SelectedItems.Count > 0 )
            {
                string  text = string.Empty;
                foreach( ListViewItem item in _emailsList.SelectedItems )
                    text += item.Text + "\n";

                Clipboard.SetDataObject( text );
            }
        }

        private void _addMenuItem_Click(object sender, EventArgs e)
        {
            _emailsList.Items.Add( _enterAddressString );
            _emailsList.Items[ _emailsList.Items.Count - 1 ].SubItems.Add( "" );
            _emailsList.Items[ _emailsList.Items.Count - 1 ].BeginEdit();
        }
        #endregion Basic Button and Menu Actions

        private void _emailsList_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            e.CancelEdit = true;
            if( !String.IsNullOrEmpty( e.Label ) )
            {
                if( _emailSet.Contains( e.Label ) )
                {
                    MessageBox.Show( this, "The email address <" + e.Label + "> already belongs to " +
                        _contact.DisplayName, "Attention", MessageBoxButtons.OK, MessageBoxIcon.Stop );
                }
                else
                {
                    e.CancelEdit = false;
                    RereadAccountsSet();
                }
            }
            if( e.CancelEdit && e.Item == _emailsList.Items.Count - 1 &&
                _enterAddressString == _emailsList.Items[ e.Item ].Text )
            {
                _emailsList.Items.RemoveAt( _emailsList.Items.Count - 1 );
            }
            SetButtonsEnable();
        }

        private void _emailsList_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            _removeButton.Enabled = _setDefaultButton.Enabled = false;
        }

        private void RereadAccountsSet()
        {
            _emailSet.Clear();
            for( int i = 0; i < _emailsList.Items.Count; i++ )
            {
                _emailSet.Add( _emailsList.Items[ i ].Text );
            }
        }

        /// <summary>
        /// If there is no default email account set, select one. Among
        /// available accounts choose the one which does not conform to
        /// X400 (Exchange-style) format - X25 accounts contain '@' symbol.
        /// </summary>
        private void  ChooseDefaultAccount()
        {
            if ( _emailsList.Items.Count > 0 && !_contact.HasProp( ContactManager._propDefaultAccount ) )
            {
                int noX400Index = 0;
                for( int i = 0; i < _emailsList.Items.Count; i++ )
                {
                    if( _emailsList.Items[ i ].Text.IndexOf( '@' ) != - 1 )
                    {
                        noX400Index = i;
                        break;
                    }
                }

                _emailsList.Items[ noX400Index ].SubItems[ 1 ].Text = DefltMarker;
            }
        }

        private void _emailsList_Resize(object sender, EventArgs e)
        {
            columnHeader1.Width = _emailsList.ClientSize.Width - columnHeader2.Width - 4;
        }

        private void _emailListcontextMenu_Popup(object sender, EventArgs e)
        {
            _copyMenuItem.Enabled = (_emailsList.SelectedItems.Count > 0);
        }

        private void _emailsList_KeyDown(object sender, KeyEventArgs e)
        {
            if( e.KeyData == Keys.Delete )
            {
                DeleteSelectedItems();
            }
            else if ( e.KeyData == Keys.Enter && _emailsList.SelectedItems.Count > 0 )
            {
                _emailsList.SelectedItems[ 0 ].BeginEdit();
            }
        }

        private void DeleteSelectedItems()
        {
            ListView.SelectedListViewItemCollection selected = _emailsList.SelectedItems;
            if( selected.Count > 0 )
            {
                foreach( ListViewItem item in selected )
                {
                    if( item.Index < _emailsList.Items.Count )
                    {
                        _emailsList.Items.RemoveAt( item.Index );
                    }
                }
                RereadAccountsSet();
            }
        }

        private void _emailsList_DoubleClick(object sender, EventArgs e)
        {
            if( _emailsList.SelectedItems.Count > 0 )
            {
                _emailsList.SelectedItems[ 0 ].BeginEdit();
            }
        }

        private void _emailsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetButtonsEnable();
        }

        private void SetButtonsEnable()
        {
            _removeButton.Enabled = _emailsList.SelectedIndices.Count > 0;
            _setDefaultButton.Enabled = _emailsList.SelectedIndices.Count == 1;
        }

        public override string  HtmlContent( IResource contact )
        {
            string result = "\t<tr><td>Email:</td>";
            IResourceList emails = contact.GetLinksOfType( "EmailAccount", Core.ContactManager.Props.LinkEmailAcct );
            if( emails.Count > 0 )
            {
                result += "<td>";
                foreach( IResource res in emails )
                {
                    result += "<a href=\"mailto:" + res.DisplayName + "\">" + res.DisplayName + "</a><br/>";
                }
                result += "</td>";
            }
            else
                result += ContactViewStandardTags.NotSpecifiedHtmlText;
            result += "</tr>";
            return result;
        }
    }
}
