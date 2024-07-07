// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace OmniaMea.Categories
{
	/// <summary>
	/// Summary description for ChooseIconForm.
	/// </summary>
	public class ChooseIconForm : DialogBase
	{
        private Panel panelIcon;
        private Button btnBrowse;
        private Button btnOK;
        private Button btnCancel;

        private Icon            _icon, _defaultIcon;
        private IResource       _category;
        private Button btnDefault;

        private const string  DefaultIcon = "categories.ico";
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ChooseIconForm( IResource cat )
		{
		    _category = cat;
            _defaultIcon = GetStandardIcon();
			InitializeComponent();

            if( cat.HasProp( "IconBlob" ) )
            {
                Stream strm = cat.GetBlobProp( "IconBlob" );
                try
                {
                    _icon = new Icon( strm );
                }
                catch( Exception )
                {
                    MessageBox.Show( "Can not load a category icon from the database", "Error Loading Icon",
                                     MessageBoxButtons.OK, MessageBoxIcon.Error );
                    _icon = null;
                }
            }
            panelIcon.Invalidate();
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
            this.panelIcon = new System.Windows.Forms.Panel();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnDefault = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // panelIcon
            //
            this.panelIcon.BackColor = System.Drawing.Color.White;
            this.panelIcon.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelIcon.Location = new System.Drawing.Point(8, 12);
            this.panelIcon.Name = "panelIcon";
            this.panelIcon.Size = new System.Drawing.Size(72, 64);
            this.panelIcon.TabIndex = 0;
            this.panelIcon.Paint += new System.Windows.Forms.PaintEventHandler(this.panelIcon_Paint);
            //
            // btnBrowse
            //
            this.btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnBrowse.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.btnBrowse.Location = new System.Drawing.Point(92, 12);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            //
            // btnOK
            //
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOK.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.btnOK.Location = new System.Drawing.Point(8, 88);
            this.btnOK.Name = "btnOK";
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.btnCancel.Location = new System.Drawing.Point(92, 88);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            //
            // btnDefault
            //
            this.btnDefault.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnDefault.Location = new System.Drawing.Point(92, 44);
            this.btnDefault.Name = "btnDefault";
            this.btnDefault.TabIndex = 2;
            this.btnDefault.Text = "Default";
            this.btnDefault.Click += new System.EventHandler(this.btnDefault_Click);
            //
            // ChooseIconForm
            //
            this.AcceptButton = this.btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(174, 122);
            this.Controls.Add(this.btnDefault);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.panelIcon);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ChooseIconForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Category Icon";
            this.ResumeLayout(false);

        }
		#endregion

        private void btnOK_Click(object sender, EventArgs e)
        {
            JetMemoryStream mstrm = new JetMemoryStream( 2048 );
            if( _icon != null )
            {
                _icon.Save( mstrm );
                new ResourceProxy( _category ).SetProp( "IconBlob", mstrm );
            }
            else
            {
                new ResourceProxy( _category ).DeleteProp( "IconBlob" );
            }

            DialogResult = DialogResult.OK;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.DefaultExt = "ico";
            dlg.Multiselect = false;
            dlg.Filter = "Icon files (*.ico)|*.ico|All files|*.*";
            if( dlg.ShowDialog( this ) == DialogResult.OK )
            {
                try
                {
                    panelIcon.Invalidate();
                    _icon = new Icon( dlg.FileName );
                }
                catch( Exception )
                {
                    MessageBox.Show( this, "File does not contain a valid Icon resource", "Error Loading Icon",
                                           MessageBoxButtons.OK, MessageBoxIcon.Error );
                    _icon = null;
                }
                DrawIcon( _icon );
            }
        }

        private void  DrawIcon( Icon icon )
        {
            if( icon != null )
            {
                int shift = (panelIcon.Width - icon.Width) / 2 - 1;
                Graphics.FromHwnd( panelIcon.Handle ).DrawIcon( icon, shift, shift );
            }
        }

        private void btnDefault_Click(object sender, EventArgs e)
        {
            _icon = null;
            panelIcon.Invalidate();
        }

        private Icon  GetStandardIcon()
        {
            return MainFrame.LoadIconFromAssembly( DefaultIcon );
        }

        private void panelIcon_Paint(object sender, PaintEventArgs e)
        {
            DrawIcon( (_icon != null) ? _icon : _defaultIcon );
        }
    }
}
