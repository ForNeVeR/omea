/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.CategoryHotkeys
{
	/// <summary>
	/// Summary description for AssignHotkeysDlg.
	/// </summary>
	public class AssignHotkeysDlg : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Label _lblCategoryHotkeys;
        private System.Windows.Forms.Label _lblAssign;
        private System.Windows.Forms.Label label1;
        private HotkeyControl _edtHotkeyAssign;
        private HotkeyControl _edtHotkeyRemove;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public AssignHotkeysDlg()
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
            this._lblCategoryHotkeys = new System.Windows.Forms.Label();
            this._lblAssign = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this._edtHotkeyAssign = new JetBrains.Omea.SamplePlugins.CategoryHotkeys.HotkeyControl();
            this._edtHotkeyRemove = new JetBrains.Omea.SamplePlugins.CategoryHotkeys.HotkeyControl();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _lblCategoryHotkeys
            // 
            this._lblCategoryHotkeys.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblCategoryHotkeys.Location = new System.Drawing.Point(8, 8);
            this._lblCategoryHotkeys.Name = "_lblCategoryHotkeys";
            this._lblCategoryHotkeys.Size = new System.Drawing.Size(268, 20);
            this._lblCategoryHotkeys.TabIndex = 0;
            this._lblCategoryHotkeys.Text = "Hotkeys for the category";
            // 
            // _lblAssign
            // 
            this._lblAssign.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblAssign.Location = new System.Drawing.Point(8, 32);
            this._lblAssign.Name = "_lblAssign";
            this._lblAssign.Size = new System.Drawing.Size(48, 20);
            this._lblAssign.TabIndex = 1;
            this._lblAssign.Text = "Assign:";
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Remove:";
            // 
            // _edtHotkeyAssign
            // 
            this._edtHotkeyAssign.Location = new System.Drawing.Point(76, 28);
            this._edtHotkeyAssign.Name = "_edtHotkeyAssign";
            this._edtHotkeyAssign.Size = new System.Drawing.Size(150, 22);
            this._edtHotkeyAssign.TabIndex = 3;
            // 
            // _edtHotkeyRemove
            // 
            this._edtHotkeyRemove.Location = new System.Drawing.Point(76, 52);
            this._edtHotkeyRemove.Name = "_edtHotkeyRemove";
            this._edtHotkeyRemove.Size = new System.Drawing.Size(150, 22);
            this._edtHotkeyRemove.TabIndex = 4;
            // 
            // _btnOk
            // 
            this._btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOk.Location = new System.Drawing.Point(124, 88);
            this._btnOk.Name = "_btnOk";
            this._btnOk.TabIndex = 5;
            this._btnOk.Text = "OK";
            // 
            // _btnCancel
            // 
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(208, 88);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 6;
            this._btnCancel.Text = "Cancel";
            // 
            // AssignHotkeysDlg
            // 
            this.AcceptButton = this._btnOk;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(292, 117);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._edtHotkeyRemove);
            this.Controls.Add(this._edtHotkeyAssign);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._lblAssign);
            this.Controls.Add(this._lblCategoryHotkeys);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AssignHotkeysDlg";
            this.ShowInTaskbar = false;
            this.Text = "Category Hotkeys";
            this.ResumeLayout(false);

        }

	    #endregion

        /// <summary>
        /// Shows the hotkeys for the specified category in the dialog and, if the user
        /// accepts the edit, saves the hotkeys in the properties of the category.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public bool ShowAssignHotkeysDialog( IResource resource )
        {
            _lblCategoryHotkeys.Text = "Hotkeys for category '" + resource.DisplayName + "':";
            _edtHotkeyAssign.Text = resource.GetPropText( PropTypes.HotkeyAssign );
            _edtHotkeyRemove.Text = resource.GetPropText( PropTypes.HotkeyRemove );
            if ( ShowDialog() == DialogResult.OK )
            {
                ResourceProxy proxy = new ResourceProxy( resource );
                proxy.BeginUpdate();
                if ( _edtHotkeyAssign.Text.Length > 0 )
                {
                    proxy.SetProp( PropTypes.HotkeyAssign, _edtHotkeyAssign.Text );
                }
                else
                {
                    proxy.DeleteProp( PropTypes.HotkeyAssign );
                }

                if ( _edtHotkeyRemove.Text.Length > 0 )
                {
                    proxy.SetProp( PropTypes.HotkeyRemove, _edtHotkeyRemove.Text );
                }
                else
                {
                    proxy.DeleteProp( PropTypes.HotkeyRemove );
                }
                proxy.EndUpdate();
                return true;
            }
            return false;
        }
	}
}
