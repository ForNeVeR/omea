// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Text;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Contacts;

namespace JetBrains.Omea.ContactsPlugin
{
    /**
     * Contact view block for editing email address information.
     */

    internal class DescriptionBlock : AbstractContactViewBlock
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.RichTextBox richDescription;
        private System.Windows.Forms.Label      labelNA;
        private IResource _contact;

		public DescriptionBlock()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

        public static AbstractContactViewBlock CreateBlock()
        {
            return new DescriptionBlock();
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
            this.richDescription = new System.Windows.Forms.RichTextBox();
            this.labelNA = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // richDescription
            //
            this.richDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.richDescription.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.richDescription.Location = new System.Drawing.Point(4, 4);
            this.richDescription.Name = "richDescription";
            this.richDescription.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richDescription.Size = new System.Drawing.Size(234, 82);
            this.richDescription.TabIndex = 1;
            this.richDescription.TabStop = false;
            this.richDescription.Text = "";
            //
            // labelNA
            //
            this.labelNA.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNA.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelNA.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.labelNA.ForeColor = System.Drawing.SystemColors.GrayText;
            this.labelNA.Location = new System.Drawing.Point(92, 4);
            this.labelNA.Name = "labelNA";
            this.labelNA.Size = new System.Drawing.Size(132, 16);
            this.labelNA.TabIndex = 16;
            this.labelNA.Text = "Not Specified";
            this.labelNA.Visible = false;
            //
            // DescriptionBlock
            //
            this.Controls.Add(this.labelNA);
            this.Controls.Add(this.richDescription);
            this.Name = "DescriptionBlock";
            this.Size = new System.Drawing.Size(240, 90);
            this.ResumeLayout(false);

        }
		#endregion

        public override void EditResource( IResource res )
        {
            _contact = res;
            richDescription.Text = _contact.GetStringProp( ContactManager._propDescription );
        }

        public override void Save()
        {
            _contact.SetProp( ContactManager._propDescription, richDescription.Text );
        }

        public override bool IsChanged()
        {
            return _contact.GetPropText( ContactManager._propDescription ) != richDescription.Text;
        }

        public override bool OwnsProperty( int propId )
        {
            return propId == ContactManager._propDescription;
        }

        public override string  HtmlContent( IResource contact )
        {
/*
            StringBuilder result = new StringBuilder( "<tr><td>Description:</td><td>" );
            string text = contact.GetPropText( ContactManager._propDescription );
            if( text.Length > 0 )
                result.Append( text );
            else
                result.Append( ContactViewStandardTags.NotSpecifiedHtmlText );
            result.Append( "</td></tr>" );
            return result.ToString();
*/
            string result = ObligatoryTag(contact, "Description:", ContactManager._propDescription);
            return result;
        }
    }
}
