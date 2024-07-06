// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Contacts;

namespace JetBrains.Omea.ContactsPlugin
{
    /**
     * Contact view block for editing email address information.
     */

    internal class AddressBlock : AbstractContactViewBlock
	{
        private Omea.ContactsPlugin.PropertyEditor _streetAddress;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Label _lblAddress;
        private IResource _contact;

		public AddressBlock()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

		}

        public static AbstractContactViewBlock CreateBlock()
        {
            return new AddressBlock();
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
            this._streetAddress = new Omea.ContactsPlugin.PropertyEditor();
            this._lblAddress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // _streetAddress
            //
            this._streetAddress.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._streetAddress.Location = new System.Drawing.Point(4, 4);
            this._streetAddress.Multiline = true;
            this._streetAddress.Name = "_streetAddress";
            this._streetAddress.ReadOnly = false;
            this._streetAddress.Size = new System.Drawing.Size(140, 104);
            this._streetAddress.TabIndex = 1;
            //
            // _lblAddress
            //
            this._lblAddress.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblAddress.Location = new System.Drawing.Point(4, 4);
            this._lblAddress.Name = "_lblAddress";
            this._lblAddress.Size = new System.Drawing.Size(80, 16);
            this._lblAddress.TabIndex = 15;
            this._lblAddress.Text = "Address:";
            this._lblAddress.Visible = false;
            //
            // AddressBlock
            //
            this.Controls.Add(this._lblAddress);
            this.Controls.Add(this._streetAddress);
            this.Name = "AddressBlock";
            this.Size = new System.Drawing.Size(150, 112);
            this.ResumeLayout(false);

        }
		#endregion

        public override void EditResource( IResource res )
        {
            _contact = res;
            _streetAddress.Text = _contact.GetStringProp( ContactManager._propAddress );
        }

        public override bool OwnsProperty( int propId )
        {
            return propId == ContactManager._propAddress;
        }

        public override void Save()
        {
            _contact.SetProp( ContactManager._propAddress, _streetAddress.Text );
        }

        public override bool IsChanged()
        {
            return _contact.GetPropText( ContactManager._propAddress ) != _streetAddress.Text;
        }

        public override string  HtmlContent( IResource contact )
        {
            return ObligatoryTag( contact, "Address:", ContactManager._propAddress );
        }
    }
}
