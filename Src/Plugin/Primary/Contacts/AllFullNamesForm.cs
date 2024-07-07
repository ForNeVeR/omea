// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.ContactsPlugin
{
	/// <summary>
	/// Summary description for AllFullNamesForm.
	/// </summary>
	public class AllFullNamesForm : DialogBase
	{
        private System.Windows.Forms.Label labelAllNames;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.ListBox listAllNames;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public AllFullNamesForm( IResource contact )
		{
			InitializeComponent();
            InitializeList( contact );
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
            this.labelAllNames = new System.Windows.Forms.Label();
            this.buttonClose = new System.Windows.Forms.Button();
            this.listAllNames = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            //
            // labelAllNames
            //
            this.labelAllNames.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelAllNames.Location = new System.Drawing.Point(4, 4);
            this.labelAllNames.Name = "labelAllNames";
            this.labelAllNames.Size = new System.Drawing.Size(148, 16);
            this.labelAllNames.TabIndex = 0;
            this.labelAllNames.Text = "All Known Names for a user:";
            //
            // buttonClose
            //
            this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonClose.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonClose.Location = new System.Drawing.Point(264, 240);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.TabIndex = 2;
            this.buttonClose.Text = "Close";
            //
            // listAllNames
            //
            this.listAllNames.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.listAllNames.IntegralHeight = false;
            this.listAllNames.Location = new System.Drawing.Point(0, 24);
            this.listAllNames.Name = "listAllNames";
            this.listAllNames.Size = new System.Drawing.Size(344, 208);
            this.listAllNames.TabIndex = 1;
            //
            // AllFullNamesForm
            //
            this.AcceptButton = this.buttonClose;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonClose;
            this.ClientSize = new System.Drawing.Size(344, 269);
            this.Controls.Add(this.listAllNames);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.labelAllNames);
            this.Name = "AllFullNamesForm";
            this.Text = "All Known Names";
            this.ResumeLayout(false);

        }
		#endregion

        private void InitializeList( IResource contact )
        {
            IResourceList names = contact.GetLinksOfType( "ContactName", Core.ContactManager.Props.LinkBaseContact );
            for( int i = 0; i < names.Count; i++ )
            {
                string name = names[ i ].GetStringProp( Core.Props.Name );
                if( listAllNames.Items.IndexOf( name ) == -1 )
                    listAllNames.Items.Add( name );
            }
        }
	}
}
