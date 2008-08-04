/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.InstantMessaging.ICQ.DBImport;

namespace JetBrains.Omea.InstantMessaging.ICQ
{
	internal class RebuildForm : DialogBase
	{
        private Button _okButton;
        private Button _cancelButton;
        private Label label5;
        private ListView _UINsList;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private GroupBox groupBox1;
        private ImageList _optionsPaneImages;
        private System.ComponentModel.IContainer components;

		public RebuildForm()
		{
			InitializeComponent();
            RestoreSettings();
//            IntArrayList uins = (IntArrayList) UINsCollection.GetUINs().Clone();
            List<int> uins = new List<int>( UINsCollection.GetUINs() );
            _UINsList.BeginUpdate();
            try
            {
                _UINsList.Items.Clear();
                for( int i = 0; i < uins.Count; )
                {
                    ListViewItem item = new ListViewItem();
                    int uin = uins[ i ];
                    item.Text = uin.ToString();
                    ICQContact aContact = ContactsFactory.GetInstance().GetContact( uin );
                    item.SubItems.Add( aContact.NickName );
                    item.ImageIndex = 0;
                    item.Tag = uin;
                    if( item.Checked = ICQPlugin.IndexedUIN( uin ) )
                    {
                        ++i;
                    }
                    else
                    {
                        uins.RemoveAt( i ); // leave in list only checked uins
                    }
                    _UINsList.Items.Add( item );
                }
            }
            finally
            {
                _UINsList.EndUpdate();
            }
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(RebuildForm));
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this._UINsList = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this._optionsPaneImages = new System.Windows.Forms.ImageList(this.components);
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // _okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(104, 156);
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 1;
            this._okButton.Text = "OK";
            this._okButton.Click += new System.EventHandler(this._okButton_Click);
            // 
            // _cancelButton
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(188, 156);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 2;
            this._cancelButton.Text = "Cancel";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.ImageAlign = System.Drawing.ContentAlignment.BottomLeft;
            this.label5.Location = new System.Drawing.Point(8, 8);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(256, 20);
            this.label5.TabIndex = 14;
            this.label5.Text = "Select ICQ profiles:";
            // 
            // _UINsList
            // 
            this._UINsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._UINsList.CheckBoxes = true;
            this._UINsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                        this.columnHeader1,
                                                                                        this.columnHeader2});
            this._UINsList.FullRowSelect = true;
            this._UINsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._UINsList.LabelWrap = false;
            this._UINsList.Location = new System.Drawing.Point(8, 28);
            this._UINsList.MultiSelect = false;
            this._UINsList.Name = "_UINsList";
            this._UINsList.Size = new System.Drawing.Size(256, 108);
            this._UINsList.SmallImageList = this._optionsPaneImages;
            this._UINsList.TabIndex = 0;
            this._UINsList.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 120;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Width = 76;
            // 
            // _optionsPaneImages
            // 
            this._optionsPaneImages.ImageSize = new System.Drawing.Size(16, 16);
            this._optionsPaneImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_optionsPaneImages.ImageStream")));
            this._optionsPaneImages.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Location = new System.Drawing.Point(8, 144);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(256, 4);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            // 
            // RebuildForm
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(272, 186);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this._UINsList);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.MinimumSize = new System.Drawing.Size(240, 140);
            this.Name = "RebuildForm";
            this.Text = "Rebuild ICQ Conversations";
            this.ResumeLayout(false);

        }
		#endregion

        private void _okButton_Click(object sender, System.EventArgs e)
        {
            IntArrayList uins = IntArrayListPool.Alloc();
            try
            {
                foreach( ListViewItem item in _UINsList.Items )
                {
                    int uin = (int) item.Tag;
                    if( item.Checked || ICQPlugin.IndexedUIN( uin ) )
                    {
                        uins.Add( uin );
                    }
                }
                ICQPlugin.SetUpdateDates( DateTime.MaxValue, DateTime.MinValue );
                ICQPlugin.SaveUINs2BeIndexed( uins );
                ICQPlugin.AsyncUpdateHistory();
            }
            finally
            {
                IntArrayListPool.Dispose( uins );
            }
        }
	}
}
