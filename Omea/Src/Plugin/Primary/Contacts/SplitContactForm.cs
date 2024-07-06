// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Contacts;

namespace JetBrains.Omea.ContactsPlugin
{
	/// <summary>
	/// Summary description for SplitContactForm.
	/// </summary>
	public class SplitContactForm : DialogBase
	{
        private System.Windows.Forms.ListBox listLeavedContacts;
        private System.Windows.Forms.Label labelLeaveContacts;
        private System.Windows.Forms.Label labelSplitContactsInto;
        private System.Windows.Forms.ListBox listSplittedContacts;
        private System.Windows.Forms.Button buttonOneToSplitted;
        private System.Windows.Forms.Button buttonOneToLeaved;
        private System.Windows.Forms.Button buttonAllToSplitted;
        private System.Windows.Forms.Button buttonAllToLeaved;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;

        private IResource       SourceContact;
        private IResourceList   AccumulatedContactsToSplit = Core.ResourceStore.EmptyResourceList;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public IResourceList Contacts2Split
        {
            get {   return AccumulatedContactsToSplit;   }
        }
        public SplitContactForm( IResource contact )
		{
			InitializeComponent();

            SourceContact = contact;
            InitializeLists();
            VerifyButtonsAccessibility();
		}

        private void InitializeLists()
        {
            IResourceList contactKeepers =
                SourceContact.GetLinksOfType( "ContactSerializationBlobKeeper",
                                              ContactManager._propSerializationBlobLink );
            if( contactKeepers.Count == 0 )
                throw new ArgumentException( "SplitContactForm -- input contact is not splittable" );
            foreach( IResource res in contactKeepers )
            {
                listLeavedContacts.Items.Add( res );
            }
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
            this.listLeavedContacts = new System.Windows.Forms.ListBox();
            this.labelLeaveContacts = new System.Windows.Forms.Label();
            this.labelSplitContactsInto = new System.Windows.Forms.Label();
            this.listSplittedContacts = new System.Windows.Forms.ListBox();
            this.buttonOneToSplitted = new System.Windows.Forms.Button();
            this.buttonOneToLeaved = new System.Windows.Forms.Button();
            this.buttonAllToSplitted = new System.Windows.Forms.Button();
            this.buttonAllToLeaved = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // listLeavedContacts
            //
            this.listLeavedContacts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)));
            this.listLeavedContacts.IntegralHeight = false;
            this.listLeavedContacts.Location = new System.Drawing.Point(4, 28);
            this.listLeavedContacts.Name = "listLeavedContacts";
            this.listLeavedContacts.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listLeavedContacts.Size = new System.Drawing.Size(160, 251);
            this.listLeavedContacts.TabIndex = 2;
            this.listLeavedContacts.DoubleClick += new System.EventHandler(this.buttonOneToSplitted_Click);
            this.listLeavedContacts.SelectedIndexChanged += new System.EventHandler(this.listLeavedContacts_SelectedIndexChanged);
            //
            // labelLeaveContacts
            //
            this.labelLeaveContacts.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelLeaveContacts.Location = new System.Drawing.Point(4, 8);
            this.labelLeaveContacts.Name = "labelLeaveContacts";
            this.labelLeaveContacts.Size = new System.Drawing.Size(156, 18);
            this.labelLeaveContacts.TabIndex = 1;
            this.labelLeaveContacts.Text = "Merged Contacts:";
            //
            // labelSplitContactsInto
            //
            this.labelSplitContactsInto.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelSplitContactsInto.Location = new System.Drawing.Point(240, 8);
            this.labelSplitContactsInto.Name = "labelSplitContactsInto";
            this.labelSplitContactsInto.Size = new System.Drawing.Size(132, 18);
            this.labelSplitContactsInto.TabIndex = 7;
            this.labelSplitContactsInto.Text = "Contacts to Extract:";
            //
            // listSplittedContacts
            //
            this.listSplittedContacts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.listSplittedContacts.IntegralHeight = false;
            this.listSplittedContacts.Location = new System.Drawing.Point(240, 28);
            this.listSplittedContacts.Name = "listSplittedContacts";
            this.listSplittedContacts.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listSplittedContacts.Size = new System.Drawing.Size(160, 251);
            this.listSplittedContacts.TabIndex = 8;
            this.listSplittedContacts.DoubleClick += new System.EventHandler(this.buttonOneToLeaved_Click);
            this.listSplittedContacts.SelectedIndexChanged += new System.EventHandler(this.listSplittedContacts_SelectedIndexChanged);
            //
            // buttonOneToSplitted
            //
            this.buttonOneToSplitted.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOneToSplitted.Location = new System.Drawing.Point(176, 60);
            this.buttonOneToSplitted.Name = "buttonOneToSplitted";
            this.buttonOneToSplitted.Size = new System.Drawing.Size(52, 23);
            this.buttonOneToSplitted.TabIndex = 3;
            this.buttonOneToSplitted.Text = ">";
            this.buttonOneToSplitted.Click += new System.EventHandler(this.buttonOneToSplitted_Click);
            //
            // buttonOneToLeaved
            //
            this.buttonOneToLeaved.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOneToLeaved.Location = new System.Drawing.Point(176, 100);
            this.buttonOneToLeaved.Name = "buttonOneToLeaved";
            this.buttonOneToLeaved.Size = new System.Drawing.Size(52, 23);
            this.buttonOneToLeaved.TabIndex = 4;
            this.buttonOneToLeaved.Text = "<";
            this.buttonOneToLeaved.Click += new System.EventHandler(this.buttonOneToLeaved_Click);
            //
            // buttonAllToSplitted
            //
            this.buttonAllToSplitted.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonAllToSplitted.Location = new System.Drawing.Point(176, 140);
            this.buttonAllToSplitted.Name = "buttonAllToSplitted";
            this.buttonAllToSplitted.Size = new System.Drawing.Size(52, 23);
            this.buttonAllToSplitted.TabIndex = 5;
            this.buttonAllToSplitted.Text = ">>";
            this.buttonAllToSplitted.Click += new System.EventHandler(this.buttonAllToSplitted_Click);
            //
            // buttonAllToLeaved
            //
            this.buttonAllToLeaved.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonAllToLeaved.Location = new System.Drawing.Point(176, 180);
            this.buttonAllToLeaved.Name = "buttonAllToLeaved";
            this.buttonAllToLeaved.Size = new System.Drawing.Size(52, 23);
            this.buttonAllToLeaved.TabIndex = 6;
            this.buttonAllToLeaved.Text = "<<";
            this.buttonAllToLeaved.Click += new System.EventHandler(this.buttonAllToLeaved_Click);
            //
            // buttonOK
            //
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Location = new System.Drawing.Point(240, 292);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.TabIndex = 9;
            this.buttonOK.Text = "OK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCancel.Location = new System.Drawing.Point(324, 292);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.TabIndex = 10;
            this.buttonCancel.Text = "Cancel";
            //
            // SplitContactForm
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(404, 321);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonOneToSplitted);
            this.Controls.Add(this.labelLeaveContacts);
            this.Controls.Add(this.listLeavedContacts);
            this.Controls.Add(this.labelSplitContactsInto);
            this.Controls.Add(this.listSplittedContacts);
            this.Controls.Add(this.buttonOneToLeaved);
            this.Controls.Add(this.buttonAllToSplitted);
            this.Controls.Add(this.buttonAllToLeaved);
            this.Name = "SplitContactForm";
            this.Text = "Extract Merged Contacts";
            this.SizeChanged += new System.EventHandler(this.SplitContactForm_SizeChanged);
            this.VisibleChanged += new System.EventHandler(this.SplitContactForm_SizeChanged);
            this.ResumeLayout(false);

        }
		#endregion


        //---------------------------------------------------------------------
        private void listLeavedContacts_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            VerifyButtonsAccessibility();
        }

        private void listSplittedContacts_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            VerifyButtonsAccessibility();
        }

        private void buttonOneToSplitted_Click(object sender, System.EventArgs e)
        {
            Debug.Assert( listLeavedContacts.SelectedIndex != -1 );
            if( buttonOneToSplitted.Enabled )
            {
                ListBox.SelectedObjectCollection coll = listLeavedContacts.SelectedItems;
                foreach( IResource res in coll )
                {
                    listSplittedContacts.Items.Add( res );
                    AccumulatedContactsToSplit = AccumulatedContactsToSplit.Union( res.ToResourceList() );
                }

                IResource[] list = new IResource[ coll.Count ];
                coll.CopyTo( list, 0 );
                foreach( IResource res in list )
                    listLeavedContacts.Items.Remove( res );

                VerifyButtonsAccessibility();
            }
        }

        private void buttonOneToLeaved_Click(object sender, System.EventArgs e)
        {
            Debug.Assert( listSplittedContacts.SelectedIndex != -1 );
            if( buttonOneToLeaved.Enabled )
            {
                ListBox.SelectedObjectCollection coll = listSplittedContacts.SelectedItems;
                foreach( IResource res in coll )
                    listLeavedContacts.Items.Add( res );

                IResource[] list = new IResource[ coll.Count ];
                coll.CopyTo( list, 0 );
                foreach( IResource res in list )
                {
                    listSplittedContacts.Items.Remove( res );
                    AccumulatedContactsToSplit = AccumulatedContactsToSplit.Minus( res.ToResourceList() );
                }

                VerifyButtonsAccessibility();
            }
        }

        private void buttonAllToSplitted_Click(object sender, System.EventArgs e)
        {
            foreach( IResource res in listLeavedContacts.Items )
            {
                listSplittedContacts.Items.Add( res );
                AccumulatedContactsToSplit = AccumulatedContactsToSplit.Union( res.ToResourceList() );
            }
            listLeavedContacts.Items.Clear();
            VerifyButtonsAccessibility();
        }

        private void buttonAllToLeaved_Click(object sender, System.EventArgs e)
        {
            foreach( IResource res in listSplittedContacts.Items )
            {
                listLeavedContacts.Items.Add( res );
                AccumulatedContactsToSplit = AccumulatedContactsToSplit.Minus( res.ToResourceList() );
            }
            listSplittedContacts.Items.Clear();
            VerifyButtonsAccessibility();
        }

        private void VerifyButtonsAccessibility()
        {
            //  Do not allow to keep the single contact as unsplitted - this
            //  generally makes no sence.
            buttonOneToSplitted.Enabled = (listLeavedContacts.Items.Count > 2) && (listLeavedContacts.SelectedIndex != -1);
            buttonOneToLeaved.Enabled   = (listLeavedContacts.Items.Count > 0) && (listSplittedContacts.SelectedIndex != -1);

            buttonAllToSplitted.Enabled = (listLeavedContacts.Items.Count > 0);
            buttonAllToLeaved.Enabled = (listSplittedContacts.Items.Count > 0);
            buttonOK.Enabled = (listSplittedContacts.Items.Count > 0);
        }

        private void buttonOK_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void SplitContactForm_SizeChanged(object sender, EventArgs e)
        {
            int middle = Width / 2;
            this.SuspendLayout();
            buttonOneToSplitted.Location = new Point( middle - buttonOneToSplitted.Size.Width / 2 - 4, buttonOneToSplitted.Location.Y );
            buttonOneToLeaved.Location = new Point( middle - buttonOneToLeaved.Size.Width / 2 - 4, buttonOneToLeaved.Location.Y );
            buttonAllToSplitted.Location = new Point( middle - buttonAllToSplitted.Size.Width / 2 - 4, buttonAllToSplitted.Location.Y );
            buttonAllToLeaved.Location = new Point( middle - buttonAllToLeaved.Size.Width / 2 - 4, buttonAllToLeaved.Location.Y );

            int height = ClientRectangle.Height - (int)((32 + listLeavedContacts.Top )* Core.ScaleFactor.Height);
            listLeavedContacts.Size = new Size( middle - 50, height );
            listSplittedContacts.Location = new Point( middle + 36, listSplittedContacts.Location.Y );
            listSplittedContacts.Size = new Size( Width - listSplittedContacts.Location.X - 16, height );

            labelSplitContactsInto.Location = new Point( listSplittedContacts.Location.X, labelSplitContactsInto.Location.Y );
            this.ResumeLayout();
        }
    }
}
