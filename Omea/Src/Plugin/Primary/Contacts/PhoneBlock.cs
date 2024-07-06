// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Contacts;
using System.Collections.Specialized;

namespace JetBrains.Omea.ContactsPlugin
{
    /// <summary>
    /// Contact view block for editing phones.
    /// </summary>
    internal class PhoneBlock: AbstractContactViewBlock
	{
        private const string PhonePrompt = "<enter phone name>";
        private const string NotSpecified = "Not Specified";
        private const int    cInitialY = 4;

        private readonly HashMap   _phoneControls = new HashMap();
        private readonly Hashtable _origPhones = new Hashtable();

        private ContactBO   _contact;
        private Label       _lastClickedLabel;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private JetTextBox  _editPhoneName;
        private Button      _addPhoneButton;
        private ContextMenu _phoneMenu;
        private MenuItem    _deleteItem;

        public PhoneBlock()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
        }

        public static AbstractContactViewBlock CreateBlock()
        {
            return new PhoneBlock();
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

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._editPhoneName = new JetTextBox();
            this._addPhoneButton = new System.Windows.Forms.Button();
            this._phoneMenu = new System.Windows.Forms.ContextMenu();
            this._deleteItem = new System.Windows.Forms.MenuItem();
            this.SuspendLayout();
            //
            // _editPhoneName
            //
            this._editPhoneName.Location = new System.Drawing.Point(4, 4);
            this._editPhoneName.Name = "_editPhoneName";
            this._editPhoneName.Size = new System.Drawing.Size(72, 20);
            this._editPhoneName.TabIndex = 0;
            this._editPhoneName.Text = "<enter name>";
            this._editPhoneName.Visible = false;
            this._editPhoneName.KeyDown += new System.Windows.Forms.KeyEventHandler(this._editPhoneName_KeyDown);
            this._editPhoneName.Leave += new System.EventHandler(this._editPhoneName_Leave);
            this._editPhoneName.Enter += new System.EventHandler(this._editPhoneName_Enter);
            //
            // _addPhoneButton
            //
            this._addPhoneButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._addPhoneButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._addPhoneButton.Location = new System.Drawing.Point(88, 4);
            this._addPhoneButton.Name = "_addPhoneButton";
            this._addPhoneButton.Size = new System.Drawing.Size(72, 23);
            this._addPhoneButton.TabIndex = 1;
            this._addPhoneButton.Text = "New...";
            this._addPhoneButton.Click += new System.EventHandler(this._addPhoneButton_Click);
            //
            // _phoneMenu
            //
            this._phoneMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {this._deleteItem});
            //
            // _deleteItem
            //
            this._deleteItem.Index = 0;
            this._deleteItem.Text = "Delete";
            this._deleteItem.Click += new System.EventHandler(this._deleteItem_Click);
            //
            // PhoneBlock
            //
            this.Controls.Add(this._editPhoneName);
            this.Controls.Add(this._addPhoneButton);
            this.Name = "PhoneBlock";
            this.Size = new System.Drawing.Size(216, 120);
            this.ResumeLayout(false);

        }
		#endregion

        public override void EditResource( IResource contact )
        {
            _contact = new ContactBO( contact );
            string[] phonesNames = _contact.GetPhoneNames();
            foreach (HashMap.Entry e in _phoneControls)
            {
                ( (Label) e.Key ).Tag = false; //  set unused
            }
            _origPhones.Clear();

            //  Display the list of phones. Two conditions are to be satisfied:
            //  1. Three standard phone labels: "Home", "Mobile" and "Work"
            //     must be present independently of their content
            //  2. Order of the phone labels above must be kept.
            AddPhone( "Home", (Array.IndexOf( phonesNames, "Home" ) == -1) ? "" : _contact.GetPhoneNumber( "Home" ), false );
            AddPhone( "Mobile", (Array.IndexOf( phonesNames, "Mobile" ) == -1) ? "" : _contact.GetPhoneNumber( "Mobile" ), false );
            AddPhone( "Work", (Array.IndexOf( phonesNames, "Work" ) == -1) ? "" : _contact.GetPhoneNumber( "Work" ), false );
            foreach( string name in phonesNames )
            {
                _origPhones[ name ] = _contact.GetPhoneNumber( name );
                if( name != "Home" && name != "Mobile" && name != "Work" )
                    AddPhone( name, _contact.GetPhoneNumber( name ), false );
            }

            //  Remove all phone controls from previous "EditResource".
            //  Thus we remove only unused phones => reduce flickering.
            bool changed = false;
            ArrayList toRemove = new ArrayList();
            foreach( HashMap.Entry e in _phoneControls )
            {
                if( (bool)((Label) e.Key ).Tag == false )
                {
                    changed = true;
                    Controls.Remove( (Label) e.Key );
                    Controls.Remove( (Control) e.Value );
                    toRemove.Add( (Label) e.Key );
                }
            }
            foreach( Label label in toRemove )
            {
                _phoneControls.Remove( label );
            }

            if( changed )
                RedrawPhones();
            UpdateHeight();
        }

        public override void Save()
        {
            StringCollection oldPhones = new StringCollection();
            oldPhones.AddRange( _contact.GetPhoneNames() );
            foreach( HashMap.Entry E in _phoneControls )
            {
                string phoneNumber = ((TextBox) E.Value).Text.Trim();
                string phoneName = ((Label) E.Key ).Text;
                if( phoneName.Length > 0 )
                {
                    phoneName = phoneName.Substring( 0, phoneName.Length - 1 );
                    if( !String.IsNullOrEmpty( phoneNumber.Trim() ) )
                        _contact.SetPhoneNumber( phoneName, phoneNumber );
                    else
                        _contact.DeletePhone( phoneName );
                    oldPhones.Remove( phoneName );
                }
            }
            foreach( string remainedPhone in oldPhones )
            {
                _contact.DeletePhone( remainedPhone );
            }
        }

        private void _addPhoneButton_Click( object sender, EventArgs e )
        {
            _editPhoneName.Text = PhonePrompt;
            _addPhoneButton.Enabled = false;
            _editPhoneName.Visible = true;
            _editPhoneName.Focus();
        }

        private void _editPhoneName_Enter(object sender, EventArgs e)
        {
            _editPhoneName.Select( 0, _editPhoneName.Text.Length );
        }

        private void _editPhoneName_Leave(object sender, EventArgs e)
        {
            // it is strongly not recommended to do complex processing, like
            // creating controls and passing focus, in the lost focus handler
            Core.UIManager.QueueUIJob( new MethodInvoker( CreateNewPhone ) );
        }

        private void _editPhoneName_KeyDown(object sender, KeyEventArgs e)
        {
            if( e.KeyCode == Keys.Enter )
            {
                e.Handled = true;
                _addPhoneButton.Enabled = true;
                _addPhoneButton.Focus();
            }
        }

        private void _deleteItem_Click(object sender, EventArgs e)
        {
            if( _lastClickedLabel != null )
            {
                HashMap.Entry E = _phoneControls.GetEntry( _lastClickedLabel );
                if( E != null )
                {
                    Controls.Remove( _lastClickedLabel );
                    Controls.Remove( (Control) E.Value );
                    _phoneControls.Remove( _lastClickedLabel );
                    Core.UIManager.QueueUIJob( new MethodInvoker( RedrawPhones ) );
                }
                _lastClickedLabel = null;
            }
        }

        private void RedrawPhones()
        {
            HashMap phoneCopies = new HashMap();
            foreach( HashMap.Entry E in _phoneControls )
            {
                Label label = (Label) E.Key;
                Control ctl = (Control) E.Value;
                phoneCopies.Add( label.Text, ctl.Text );
                Controls.Remove( label );
                Controls.Remove( ctl );
            }
            _phoneControls.Clear();

            _editPhoneName.Location = new Point( _editPhoneName.Left, cInitialY );
            _addPhoneButton.Location = new Point( _addPhoneButton.Left, cInitialY );
            foreach( HashMap.Entry E in phoneCopies )
            {
                string phoneName = (string) E.Key;
                string phoneValue = (string) E.Value;
                AddPhone( phoneName.Substring( 0, phoneName.Length - 1 ),
                    (phoneValue == NotSpecified) ? null : phoneValue, false );
            }
            UpdateHeight();
        }

        private void CreateNewPhone()
        {
            _editPhoneName.Visible = false;
            _addPhoneButton.Enabled = true;
            if( _editPhoneName.Text.Length > 0 && _editPhoneName.Text != PhonePrompt )
            {
                SuspendLayout();
                try
                {
                    AddPhone( _editPhoneName.Text, "", true );
                    UpdateHeight();
                }
                finally
                {
                    ResumeLayout();
                }
            }
        }

        private void AddPhone( string name, string number, bool setFocus )
        {
            int  xPosSample = -1, widthSample = -1;
            foreach( HashMap.Entry e in _phoneControls )
            {
                xPosSample = ((TextBox)e.Value).Location.X;
                widthSample = ((TextBox)e.Value).Size.Width;

                if( ( (Label) e.Key ).Text == name + ":" )
                {
                    ((Label) e.Key).Tag = true;
                    TextBox textCtl = (TextBox) e.Value;
                    textCtl.Text = number;
                    return;
                }
            }

            int  currentY = _editPhoneName.Location.Y;

            Label   newLabel = CreateLabel( name );
            TextBox newTextBox = new TextBox();
            newTextBox.Text = number;
            SetSizeLocation( newTextBox, newLabel, xPosSample, widthSample, currentY );
            newTextBox.Visible = true;
            newTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            Controls.Add( newTextBox );
            _phoneControls.Add( newLabel, newTextBox );

            _addPhoneButton.TabIndex = newTextBox.TabIndex + 1;
            if( setFocus )
            {
                newTextBox.Focus();
            }

            currentY += (int)(24 * Core.ScaleFactor.Height);
            _editPhoneName.Location = new Point( _editPhoneName.Left, currentY );
            _addPhoneButton.Location = new Point( _addPhoneButton.Left, currentY );
        }

        private Label CreateLabel( string name )
        {
            Label newLabel = new Label();
            newLabel.FlatStyle = FlatStyle.System;
            newLabel.Text = name + ":";
            newLabel.Location = _editPhoneName.Location;
            newLabel.Size = _editPhoneName.Size;
            newLabel.Visible = true;
            newLabel.Click += newLabel_Click;
            newLabel.Tag = true;
            newLabel.ContextMenu = _phoneMenu;

            Controls.Add( newLabel );

            return newLabel;
        }

        //---------------------------------------------------------------------
        //  If possible, for setting size and location use the settings of the
        //  previously created control. This makes possible to properly deal
        //  with form resize and large fonts.
        //---------------------------------------------------------------------
        private void SetSizeLocation( TextBox textBox, Label newLabel, int xPosSample, int widthSample, int currentY )
        {
            if( xPosSample == -1 )
            {
                textBox.Location = new Point( (int)(newLabel.Location.X * Core.ScaleFactor.Width) + newLabel.Size.Width + (int)(8 * Core.ScaleFactor.Width), currentY );
                textBox.Width = Width - textBox.Location.X - (int)(4 * Core.ScaleFactor.Width);
            }
            else
            {
                textBox.Location = new Point( xPosSample, currentY );
                textBox.Width = widthSample;
            }
            textBox.Height = _editPhoneName.Size.Height;
        }

	    private void UpdateHeight()
        {
            Height = _addPhoneButton.Location.Y + _addPhoneButton.Height;
            Height = Height + (int)(10 * Core.ScaleFactor.Height);
        }

        private void newLabel_Click( object sender, EventArgs e )
        {
            _lastClickedLabel = (Label) sender;
        }

        public override bool IsChanged()
        {
            int  nonEmptyPhones = 0;

            //  Each non empty phone number must exist in the original
            //  set of phones under the same name.
            foreach( HashMap.Entry e in _phoneControls )
            {
                string name = ((Label) e.Key).Text;
                string number = ((Control) e.Value).Text;
                if( number != string.Empty )
                {
                    nonEmptyPhones++;
                    if( !_origPhones.ContainsKey( name ) || ((string)_origPhones[ name ]) != number )
                        return true;
                }
            }

            //  All current phones are in place, but wat about removed ones?
            if( nonEmptyPhones != _origPhones.Count )
                return true;

            return false;
        }

        public override bool OwnsProperty( int propId )
        {
            return propId == ContactManager._propPhone;
        }

        public override string  HtmlContent( IResource contact )
        {
            string result = string.Empty;

            ContactBO bo = new ContactBO( contact );
            string[] phoneNames = bo.GetPhoneNames();

            result += AddPhoneText( "Home", phoneNames, bo );
            result += AddPhoneText( "Mobile", phoneNames, bo );
            result += AddPhoneText( "Work", phoneNames, bo );

            foreach( string phoneName in phoneNames )
            {
                if( phoneName != "Home" && phoneName != "Mobile" && phoneName != "Work" )
                    result += "\t<tr><td>" + phoneName + "</td><td>" + bo.GetPhoneNumber(phoneName) + "</td></tr>";
            }
            return result;
        }

        private static string AddPhoneText( string name, string[] names, IContact contact )
        {
            string result = "\t<tr><td>" + name + ":</td>";
            if( Array.IndexOf( names, name ) != -1 )
                result += "<td>" + contact.GetPhoneNumber( name ) + "</td>";
            else
                result += ContactViewStandardTags.NotSpecifiedHtmlText;
            result += "</tr>";
            return result;
        }
    }
}
