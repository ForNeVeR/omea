// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.TextIndex;

namespace JetBrains.Omea.DebugPlugin
{
	/// <summary>
	/// Summary description for ViewDocIndexContent.
	/// </summary>
	public class ViewDocIndexContentForm : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Label labelDocID;
        private System.Windows.Forms.TextBox textDocID;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonShowContent;

        private System.Windows.Forms.ColumnHeader columnTermID;
        private System.Windows.Forms.ColumnHeader columnMetric;
        private System.Windows.Forms.ListView listDocContent;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ViewDocIndexContentForm( IResource doc ) : this()
		{
            textDocID.Text = doc.Id.ToString();
            buttonShowContent_Click( null, null );
		}
		public ViewDocIndexContentForm()
		{
			InitializeComponent();
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
            this.labelDocID = new System.Windows.Forms.Label();
            this.textDocID = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonShowContent = new System.Windows.Forms.Button();
            this.listDocContent = new System.Windows.Forms.ListView();
            this.columnTermID = new System.Windows.Forms.ColumnHeader();
            this.columnMetric = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            //
            // labelDocID
            //
            this.labelDocID.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelDocID.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.labelDocID.Location = new System.Drawing.Point(12, 8);
            this.labelDocID.Name = "labelDocID";
            this.labelDocID.Size = new System.Drawing.Size(40, 16);
            this.labelDocID.TabIndex = 0;
            this.labelDocID.Text = "&Doc ID:";
            //
            // textDocID
            //
            this.textDocID.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.textDocID.Location = new System.Drawing.Point(56, 4);
            this.textDocID.Name = "textDocID";
            this.textDocID.Size = new System.Drawing.Size(60, 21);
            this.textDocID.TabIndex = 1;
            this.textDocID.Text = "";
            //
            // buttonOK
            //
            this.buttonOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.buttonOK.Location = new System.Drawing.Point(100, 480);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.TabIndex = 4;
            this.buttonOK.Text = "OK";
            //
            // buttonShowContent
            //
            this.buttonShowContent.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonShowContent.Location = new System.Drawing.Point(128, 4);
            this.buttonShowContent.Name = "buttonShowContent";
            this.buttonShowContent.TabIndex = 2;
            this.buttonShowContent.Text = "Show";
            this.buttonShowContent.Click += new System.EventHandler(this.buttonShowContent_Click);
            //
            // listDocContent
            //
            this.listDocContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.listDocContent.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {   this.columnTermID,
                                                                                             this.columnMetric});
            this.listDocContent.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listDocContent.Location = new System.Drawing.Point(8, 32);
            this.listDocContent.MultiSelect = false;
            this.listDocContent.Name = "listDocContent";
            this.listDocContent.Size = new System.Drawing.Size(256, 440);
            this.listDocContent.TabIndex = 3;
            this.listDocContent.View = System.Windows.Forms.View.Details;
            this.listDocContent.DoubleClick += new System.EventHandler(this.ItemDoubleClicked);
            //
            // columnTermID
            //
            this.columnTermID.Text = "Term ID";
            this.columnTermID.Width = 136;
            //
            // columnMetric
            //
            this.columnMetric.Text = "Tf * Idf";
            this.columnMetric.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnMetric.Width = 91;
            //
            // ViewDocIndexContentForm
            //
            this.AcceptButton = this.buttonShowContent;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonOK;
            this.ClientSize = new System.Drawing.Size(272, 505);
            this.Controls.Add(this.listDocContent);
            this.Controls.Add(this.buttonShowContent);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textDocID);
            this.Controls.Add(this.labelDocID);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ViewDocIndexContentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "View Content For Document in TextIndex";
            this.ResumeLayout(false);

        }
		#endregion

        private void buttonShowContent_Click(object sender, System.EventArgs e)
        {
            try
            {
                int docID = Int32.Parse( textDocID.Text );
                listDocContent.Items.Clear();
                if( Core.TextIndexManager.IsDocumentInIndex( docID ))
                {
                    listDocContent.Items.Add( "Document exist(!) in the Text Index" );
                }
                else
                {
                    listDocContent.Items.Add( "Document does not exist in the Text Index" );
                }
            }
            catch( Exception )
            {}
        }

        private void ItemDoubleClicked(object sender, System.EventArgs e)
        {
            ListViewItem item = listDocContent.FocusedItem;
            if( item != null )
            {
                int termID = (int) item.Tag;
                ViewTermIndexContentForm form = new ViewTermIndexContentForm( termID );
                form.ShowDialog();
            }
        }
	}
}
