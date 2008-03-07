/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>


using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Contacts;

namespace JetBrains.Omea.ContactsPlugin
{
	/**
     * Contact view block for editing work information.
     */
    
    internal class JobBlock: AbstractContactViewBlock
	{
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private PropertyEditor _jobTitle;
        private PropertyEditor _company;
		private System.ComponentModel.Container components = null;
        private IResource _contact;

		public JobBlock()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

		}

        public static AbstractContactViewBlock CreateBlock()
        {
            return new JobBlock();
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
            this._jobTitle = new JetBrains.Omea.ContactsPlugin.PropertyEditor();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this._company = new JetBrains.Omea.ContactsPlugin.PropertyEditor();
            this.SuspendLayout();
            // 
            // _jobTitle
            // 
            this._jobTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._jobTitle.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._jobTitle.Location = new System.Drawing.Point(92, 4);
            this._jobTitle.Multiline = false;
            this._jobTitle.Name = "_jobTitle";
            this._jobTitle.ReadOnly = false;
            this._jobTitle.Size = new System.Drawing.Size(120, 24);
            this._jobTitle.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label3.Location = new System.Drawing.Point(4, 4);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 16);
            this.label3.TabIndex = 9;
            this.label3.Text = "Job Title:";
            // 
            // label4
            // 
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label4.Location = new System.Drawing.Point(4, 32);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 16);
            this.label4.TabIndex = 11;
            this.label4.Text = "Company:";
            // 
            // _company
            // 
            this._company.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._company.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._company.Location = new System.Drawing.Point(92, 32);
            this._company.Multiline = false;
            this._company.Name = "_company";
            this._company.ReadOnly = false;
            this._company.Size = new System.Drawing.Size(120, 24);
            this._company.TabIndex = 10;
            // 
            // JobBlock
            // 
            this.Controls.Add(this._jobTitle);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this._company);
            this.Name = "JobBlock";
            this.Size = new System.Drawing.Size(216, 60);
            this.ResumeLayout(false);

        }
		#endregion

        public override void EditResource( IResource res )
        {
            _contact = res;
            _jobTitle.Text = _contact.GetStringProp( ContactManager._propJobTitle );
            _company.Text = _contact.GetStringProp( ContactManager._propCompany );
        }

        public override void Save()
        {
            _contact.SetProp( ContactManager._propJobTitle, _jobTitle.Text );
            _contact.SetProp( ContactManager._propCompany, _company.Text );
        }

        public override bool IsChanged()
        {
            return _contact.GetPropText( ContactManager._propJobTitle ) != _jobTitle.Text ||
                   _contact.GetPropText( ContactManager._propCompany ) != _company.Text;
        }

        public override bool OwnsProperty( int propId )
        {
            return propId == ContactManager._propJobTitle ||
                   propId == ContactManager._propCompany;
        }

        public override string  HtmlContent( IResource contact )
        {
            string result = ObligatoryTag( contact, "Job Title:", ContactManager._propJobTitle );
            result += ObligatoryTag( contact, "Company:", ContactManager._propCompany );
            return result;
        }
    }
}
