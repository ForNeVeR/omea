// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.TextIndex;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.DebugPlugin
{
	/// <summary>
	/// Summary description for TermEntryContentForm.
	/// </summary>
	public class TermEntryContentForm : System.Windows.Forms.Form
	{
        private System.Windows.Forms.ListView listEntryContent;
        private System.Windows.Forms.ColumnHeader columnOffset;
        private System.Windows.Forms.ColumnHeader columnSentence;
        private System.Windows.Forms.ColumnHeader columnSection;
        private System.Windows.Forms.Button buttonOK;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public TermEntryContentForm( Entry e )
		{
			InitializeComponent();
            FillTable( e );
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
            this.listEntryContent = new System.Windows.Forms.ListView();
            this.columnOffset = new System.Windows.Forms.ColumnHeader();
            this.columnSentence = new System.Windows.Forms.ColumnHeader();
            this.columnSection = new System.Windows.Forms.ColumnHeader();
            this.buttonOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // listEntryContent
            //
            this.listEntryContent.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                               this.columnOffset,
                                                                                               this.columnSentence,
                                                                                               this.columnSection});
            this.listEntryContent.Dock = System.Windows.Forms.DockStyle.Top;
            this.listEntryContent.Location = new System.Drawing.Point(0, 0);
            this.listEntryContent.MultiSelect = false;
            this.listEntryContent.Name = "listEntryContent";
            this.listEntryContent.Size = new System.Drawing.Size(292, 472);
            this.listEntryContent.TabIndex = 0;
            this.listEntryContent.View = System.Windows.Forms.View.Details;
            //
            // columnOffset
            //
            this.columnOffset.Text = "Offset";
            this.columnOffset.Width = 68;
            //
            // columnSentence
            //
            this.columnSentence.Text = "Sentence";
            this.columnSentence.Width = 63;
            //
            // columnSection
            //
            this.columnSection.Text = "Section";
            this.columnSection.Width = 151;
            //
            // buttonOK
            //
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonOK.Location = new System.Drawing.Point(114, 476);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "OK";
            //
            // TermEntryContentForm
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonOK;
            this.ClientSize = new System.Drawing.Size(292, 501);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.listEntryContent);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TermEntryContentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TermIndex Record Entry";
            this.ResumeLayout(false);

        }
		#endregion

        private void FillTable( Entry e )
        {
            Hashtable hash = new Hashtable();
            IResourceList sections = Core.ResourceStore.GetAllResources( DocumentSectionResource.DocSectionResName );
            foreach( IResource res in sections )
            {
                uint order = (uint)res.GetIntProp( "SectionOrder" );
                string name = res.GetStringProp( "Name" );
                if( order > 0 )
                    hash[ order ] = name;
            }

            for( int i = 0; i < e.Count; i++ )
            {
                ListViewItem item = new ListViewItem();
                item.Text = e.Instance( i ).OffsetNormal.ToString();
                item.SubItems.Add( e.Instance( i ).Sentence.ToString() );
                uint sectionId = e.Instance( i ).SectionId;
                if( hash.ContainsKey( sectionId ) )
                    item.SubItems.Add( (string)hash[ sectionId ] );
                else
                if( sectionId > 0 )
                    item.SubItems.Add( "unknown section" );
                listEntryContent.Items.Add( item );
            }
        }
	}
}
