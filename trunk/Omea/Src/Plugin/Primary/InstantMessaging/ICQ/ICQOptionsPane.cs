/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using JetBrains.Omea.InstantMessaging.ICQ.DBImport;

namespace JetBrains.Omea.InstantMessaging.ICQ
{
	public class ICQOptionsPane : AbstractOptionsPane
	{
        private System.Windows.Forms.ImageList _optionsPaneImages;
        private System.ComponentModel.IContainer components;
        private decimal _minutes;
        private System.Windows.Forms.Panel _conversationsPanel;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown _convsTimeSpan;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListView _UINsList;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.CheckBox _indexOnlineCheckBox;
        private System.Windows.Forms.ToolTip _indexOnlineToolTip;
        private System.Windows.Forms.CheckBox _reverseModeCheckBox;
        private System.Windows.Forms.CheckBox _importOnly2003bCheckbox;
        private IntArrayList _uins;

		private ICQOptionsPane()
		{
            UINsCollection.Refresh();
			InitializeComponent();
		}

        internal static AbstractOptionsPane ICQOptionsPaneCreator()
        {
            return new ICQOptionsPane();
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

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ICQOptionsPane));
            this._optionsPaneImages = new System.Windows.Forms.ImageList(this.components);
            this._conversationsPanel = new System.Windows.Forms.Panel();
            this._reverseModeCheckBox = new System.Windows.Forms.CheckBox();
            this._indexOnlineCheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this._convsTimeSpan = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this._importOnly2003bCheckbox = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this._UINsList = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this._indexOnlineToolTip = new System.Windows.Forms.ToolTip(this.components);
            this._conversationsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._convsTimeSpan)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _optionsPaneImages
            // 
            this._optionsPaneImages.ImageSize = new System.Drawing.Size(16, 16);
            this._optionsPaneImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_optionsPaneImages.ImageStream")));
            this._optionsPaneImages.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // _conversationsPanel
            // 
            this._conversationsPanel.Controls.Add(this._reverseModeCheckBox);
            this._conversationsPanel.Controls.Add(this._indexOnlineCheckBox);
            this._conversationsPanel.Controls.Add(this.groupBox1);
            this._conversationsPanel.Controls.Add(this.label4);
            this._conversationsPanel.Controls.Add(this.groupBox2);
            this._conversationsPanel.Controls.Add(this.label3);
            this._conversationsPanel.Controls.Add(this.label2);
            this._conversationsPanel.Controls.Add(this._convsTimeSpan);
            this._conversationsPanel.Controls.Add(this.label1);
            this._conversationsPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._conversationsPanel.Location = new System.Drawing.Point(0, 0);
            this._conversationsPanel.Name = "_conversationsPanel";
            this._conversationsPanel.Size = new System.Drawing.Size(428, 112);
            this._conversationsPanel.TabIndex = 9;
            // 
            // _reverseModeCheckBox
            // 
            this._reverseModeCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._reverseModeCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._reverseModeCheckBox.Location = new System.Drawing.Point(8, 68);
            this._reverseModeCheckBox.Name = "_reverseModeCheckBox";
            this._reverseModeCheckBox.Size = new System.Drawing.Size(412, 24);
            this._reverseModeCheckBox.TabIndex = 14;
            this._reverseModeCheckBox.Text = "Show latest messages in conversations on top";
            // 
            // _indexOnlineCheckBox
            // 
            this._indexOnlineCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._indexOnlineCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._indexOnlineCheckBox.Location = new System.Drawing.Point(8, 44);
            this._indexOnlineCheckBox.Name = "_indexOnlineCheckBox";
            this._indexOnlineCheckBox.Size = new System.Drawing.Size(412, 24);
            this._indexOnlineCheckBox.TabIndex = 1;
            this._indexOnlineCheckBox.Text = "Synchronize database immediately";
            this._indexOnlineToolTip.SetToolTip(this._indexOnlineCheckBox, "_indexOnlineToolTip");
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Location = new System.Drawing.Point(112, 100);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(316, 8);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            // 
            // label4
            // 
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(4, 96);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(96, 20);
            this.label4.TabIndex = 12;
            this.label4.Text = "ICQ Accounts";
            this.label4.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Location = new System.Drawing.Point(144, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(284, 8);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            // 
            // label3
            // 
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(0, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(144, 20);
            this.label3.TabIndex = 9;
            this.label3.Text = "Building conversations";
            this.label3.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(320, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 20);
            this.label2.TabIndex = 8;
            this.label2.Text = "minutes";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _convsTimeSpan
            // 
            this._convsTimeSpan.Location = new System.Drawing.Point(260, 20);
            this._convsTimeSpan.Maximum = new System.Decimal(new int[] {
                                                                           14400,
                                                                           0,
                                                                           0,
                                                                           0});
            this._convsTimeSpan.Minimum = new System.Decimal(new int[] {
                                                                           1,
                                                                           0,
                                                                           0,
                                                                           0});
            this._convsTimeSpan.Name = "_convsTimeSpan";
            this._convsTimeSpan.Size = new System.Drawing.Size(56, 20);
            this._convsTimeSpan.TabIndex = 0;
            this._convsTimeSpan.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._convsTimeSpan.Value = new System.Decimal(new int[] {
                                                                         120,
                                                                         0,
                                                                         0,
                                                                         0});
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(256, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Maximum time span between messages:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this._importOnly2003bCheckbox);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this._UINsList);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 112);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(428, 160);
            this.panel1.TabIndex = 10;
            // 
            // _importOnly2003bCheckbox
            // 
            this._importOnly2003bCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._importOnly2003bCheckbox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._importOnly2003bCheckbox.Location = new System.Drawing.Point(8, 124);
            this._importOnly2003bCheckbox.Name = "_importOnly2003bCheckbox";
            this._importOnly2003bCheckbox.Size = new System.Drawing.Size(412, 24);
            this._importOnly2003bCheckbox.TabIndex = 13;
            this._importOnly2003bCheckbox.Text = "Import only databases of ICQ2003b and later ICQ versions";
            // 
            // label5
            // 
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.ImageAlign = System.Drawing.ContentAlignment.BottomLeft;
            this.label5.Location = new System.Drawing.Point(8, 4);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(104, 20);
            this.label5.TabIndex = 12;
            this.label5.Text = "Index history of:";
            // 
            // _UINsList
            // 
            this._UINsList.CheckBoxes = true;
            this._UINsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                        this.columnHeader1,
                                                                                        this.columnHeader2});
            this._UINsList.FullRowSelect = true;
            this._UINsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._UINsList.LabelWrap = false;
            this._UINsList.Location = new System.Drawing.Point(8, 24);
            this._UINsList.MultiSelect = false;
            this._UINsList.Name = "_UINsList";
            this._UINsList.Size = new System.Drawing.Size(200, 88);
            this._UINsList.SmallImageList = this._optionsPaneImages;
            this._UINsList.TabIndex = 3;
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
            // ICQOptionsPane
            // 
            this.Controls.Add(this.panel1);
            this.Controls.Add(this._conversationsPanel);
            this.Name = "ICQOptionsPane";
            this.Size = new System.Drawing.Size(428, 272);
            this._conversationsPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._convsTimeSpan)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        public override void ShowPane()
        {
            if( IsStartupPane )
            {
                _conversationsPanel.Visible = false;
                _importOnly2003bCheckbox.Left = _UINsList.Left = label5.Left = 0;
            }
            else
            {
                _conversationsPanel.Visible = true;
                TimeSpan span = ICQPlugin.GetConversationTimeSpan();
                _minutes = ( span.Minutes + span.Hours * 60 + span.Days * 24 * 60 );
                if( _minutes >= _convsTimeSpan.Minimum && _minutes <= _convsTimeSpan.Maximum )
                {
                    _convsTimeSpan.Value = _minutes;
                }
                _indexOnlineCheckBox.Checked = ICQPlugin.GetBuildConverstionOnline();
                _reverseModeCheckBox.Checked = ICQPlugin.GetReverseMode();
            }
            _uins = (IntArrayList) UINsCollection.GetUINs().Clone();
            _UINsList.Items.Clear();
            for( int i = 0; i < _uins.Count; )
            {
                ListViewItem item = new ListViewItem();
                int uin = _uins[ i ];
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
                    _uins.RemoveAt( i ); // leave in list only checked uins
                }
                _UINsList.Items.Add( item );
            }
            _importOnly2003bCheckbox.Checked = ICQPlugin.GetImportOnly2003b();
            _importOnly2003bCheckbox.Visible = UINsCollection.HasModernDBs;
        }

        public override void OK()
        {
            IntArrayList uins = IntArrayListPool.Alloc();
            try
            {
                foreach( ListViewItem item in _UINsList.Items )
                {
                    if( item.Checked )
                    {
                        uins.Add( (int) item.Tag );
                    }
                }
                uins.Sort();
                bool changed = ( uins.Count != _uins.Count );
                if( !changed )
                {
                    for( int i = 0; i < uins.Count; ++i )
                    {
                        if( uins[ i ] != _uins[ i ] )
                        {
                            changed = true;
                            break;
                        }
                    }
                }
                if( !IsStartupPane )
                {
                    ICQPlugin.SetBuildConverstionOnline( _indexOnlineCheckBox.Checked );
                    ICQPlugin.SetReverseMode( _reverseModeCheckBox.Checked );
                }
                if( !IsStartupPane && _convsTimeSpan.Value != _minutes )
                {
                    ICQPlugin.SetConversationTimeSpan(
                        new TimeSpan( ((long)_convsTimeSpan.Value ) * 60 * 10000000 ) );
                    changed = true;
                }
                changed = changed ||
                    _importOnly2003bCheckbox.Checked != ICQPlugin.GetImportOnly2003b() ||
                    ( _indexOnlineCheckBox.Checked &&
                    _indexOnlineCheckBox.Checked != ICQPlugin.GetBuildConverstionOnline() );
                ICQPlugin.SetImportOnly2003b( _importOnly2003bCheckbox.Checked );
                // rebuild conversations if there were changes
                if( changed || IsStartupPane )
                {
                    ICQPlugin.SetUpdateDates( DateTime.MaxValue, DateTime.MinValue );
                    ICQPlugin.SaveUINs2BeIndexed( uins );
                    if( !IsStartupPane )
                    {
                        ICQPlugin.AsyncUpdateHistory();                
                    }
                }
            }
            finally
            {
                IntArrayListPool.Dispose( uins );
            }
        }

	    public override string GetHelpKeyword()
	    {
	        return "/reference/icq.html";
	    }
	}
}
