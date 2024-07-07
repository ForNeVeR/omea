// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using DBIndex;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.TextIndex;
using JetBrains.DataStructures;

namespace JetBrains.Omea.DebugPlugin
{
	/// <summary>
	/// Summary description for ViewTermIndexContentForm.
	/// </summary>
	public class ViewTermIndexContentForm : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Button buttonShowContent;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.TextBox textTermID;
        private System.Windows.Forms.Label labelTermID;

        private TermIndexRecord recMain, recMem;
        private IntHashTableOfInt hash = new IntHashTableOfInt();
        private static Font      mainFont = new Font( "Tahoma", 8.0f );
        private static Font      delFont  = new Font( mainFont, FontStyle.Strikeout );

        private System.Windows.Forms.ListView listEntries;
        private System.Windows.Forms.ColumnHeader TermNumber;
        private System.Windows.Forms.ColumnHeader InstancesNumber;
        private System.Windows.Forms.ColumnHeader DocID;
        private System.Windows.Forms.ColumnHeader ChainedBy;
        private System.Windows.Forms.Label labelNormForm;
        private System.Windows.Forms.Label labelHC;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ViewTermIndexContentForm()
        {
			InitializeComponent();
        }

		public ViewTermIndexContentForm( long termID ) : this()
		{
            textTermID.Text = termID.ToString();
            buttonShowContent_Click( null, null );
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
            this.buttonShowContent = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.textTermID = new System.Windows.Forms.TextBox();
            this.labelTermID = new System.Windows.Forms.Label();
            this.listEntries = new System.Windows.Forms.ListView();
            this.TermNumber = new System.Windows.Forms.ColumnHeader();
            this.DocID = new System.Windows.Forms.ColumnHeader();
            this.InstancesNumber = new System.Windows.Forms.ColumnHeader();
            this.ChainedBy = new System.Windows.Forms.ColumnHeader();
            this.labelNormForm = new System.Windows.Forms.Label();
            this.labelHC = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // buttonShowContent
            //
            this.buttonShowContent.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonShowContent.Location = new System.Drawing.Point(168, 4);
            this.buttonShowContent.Name = "buttonShowContent";
            this.buttonShowContent.TabIndex = 7;
            this.buttonShowContent.Text = "Show";
            this.buttonShowContent.Click += new System.EventHandler(this.buttonShowContent_Click);
            //
            // buttonOK
            //
            this.buttonOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.buttonOK.Location = new System.Drawing.Point(88, 460);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.TabIndex = 9;
            this.buttonOK.Text = "OK";
            //
            // textTermID
            //
            this.textTermID.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.textTermID.Location = new System.Drawing.Point(64, 4);
            this.textTermID.Name = "textTermID";
            this.textTermID.TabIndex = 6;
            this.textTermID.Text = "";
            //
            // labelTermID
            //
            this.labelTermID.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelTermID.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.labelTermID.Location = new System.Drawing.Point(8, 8);
            this.labelTermID.Name = "labelTermID";
            this.labelTermID.Size = new System.Drawing.Size(44, 16);
            this.labelTermID.TabIndex = 5;
            this.labelTermID.Text = "&Term ID:";
            //
            // listEntries
            //
            this.listEntries.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.listEntries.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                          this.TermNumber,
                                                                                          this.DocID,
                                                                                          this.InstancesNumber,
                                                                                          this.ChainedBy});
            this.listEntries.FullRowSelect = true;
            this.listEntries.Location = new System.Drawing.Point(8, 76);
            this.listEntries.MultiSelect = false;
            this.listEntries.Name = "listEntries";
            this.listEntries.Size = new System.Drawing.Size(232, 368);
            this.listEntries.TabIndex = 10;
            this.listEntries.View = System.Windows.Forms.View.Details;
            this.listEntries.DoubleClick += new System.EventHandler(this.DoubleClickedItem);
            //
            // TermNumber
            //
            this.TermNumber.Text = "N";
            this.TermNumber.Width = 33;
            //
            // DocID
            //
            this.DocID.Text = "Doc ID";
            this.DocID.Width = 59;
            //
            // InstancesNumber
            //
            this.InstancesNumber.Text = "Count";
            this.InstancesNumber.Width = 47;
            //
            // ChainedBy
            //
            this.ChainedBy.Text = "Chained By";
            this.ChainedBy.Width = 68;
            //
            // labelNormForm
            //
            this.labelNormForm.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelNormForm.Location = new System.Drawing.Point(8, 32);
            this.labelNormForm.Name = "labelNormForm";
            this.labelNormForm.Size = new System.Drawing.Size(236, 20);
            this.labelNormForm.TabIndex = 11;
            this.labelNormForm.Text = "Normalized Form:";
            //
            // labelHC
            //
            this.labelHC.Location = new System.Drawing.Point(8, 52);
            this.labelHC.Name = "labelHC";
            this.labelHC.Size = new System.Drawing.Size(236, 20);
            this.labelHC.TabIndex = 12;
            this.labelHC.Text = "Hash value:";
            //
            // ViewTermIndexContentForm
            //
            this.AcceptButton = this.buttonShowContent;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonOK;
            this.ClientSize = new System.Drawing.Size(248, 485);
            this.Controls.Add(this.labelHC);
            this.Controls.Add(this.labelNormForm);
            this.Controls.Add(this.listEntries);
            this.Controls.Add(this.buttonShowContent);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textTermID);
            this.Controls.Add(this.labelTermID);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ViewTermIndexContentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "View Content of TermIndex Record";
            this.ResumeLayout(false);

        }
		#endregion

        private void buttonShowContent_Click(object sender, System.EventArgs e)
        {
            int termHC;
            int  entryCounter = 1;
            listEntries.Items.Clear();
            hash.Clear();
            try
            {
                termHC = Int32.Parse( textTermID.Text );
            }
            catch( Exception )
            {
                termHC = Word.GetTermId( textTermID.Text.ToLower() );
                LexemeConstructor ctor = new LexemeConstructor( OMEnv.ScriptMorphoAnalyzer,
                                                                OMEnv.DictionaryServer );
                string normForm = ctor.GetNormalizedToken( textTermID.Text );
                labelNormForm.Text = "Normalized Form: " + normForm;
                termHC = Word.GetTermId( normForm.ToLower() );
            }

            if( termHC != -1 )
            {
                labelHC.Text = termHC.ToString();
                recMain = (TermIndexRecord) FullTextIndexer.Instance.GetTermRecordMain( termHC );
                recMem = (TermIndexRecord) FullTextIndexer.Instance.GetTermRecordMem( termHC );

                if( recMain != null )
                    FillList( recMain, ref entryCounter, Color.LightSkyBlue );
                if( recMem != null )
                    FillList( recMem, ref entryCounter, Color.LightYellow );
            }
            else
                MessageBox.Show( "No such term in the index" );
        }

        private void DoubleClickedItem(object sender, System.EventArgs e)
        {
            Entry entry;
            ListViewItem item = listEntries.FocusedItem;
            int  counter = Int32.Parse( item.Text );
            if( item.BackColor == Color.LightSkyBlue )
                entry = recMain.GetEntryAt( counter - 1 );
            else
            {
                if( recMain == null )
                    entry = recMem.GetEntryAt( counter - 1 );
                else
                    entry = recMem.GetEntryAt( counter - recMain.DocsNumber - 1 );
            }
            TermEntryContentForm form = new TermEntryContentForm( entry );
            form.ShowDialog();
        }

        private void FillList( TermIndexRecord rec, ref int entryCounter, Color backColor )
        {
            for( int i = 0; i < rec.DocsNumber; i++, entryCounter++ )
            {
                Entry e = rec.GetEntryAt( i );
                ListViewItem item = new ListViewItem();
                item.BackColor = backColor;
                item.Text = entryCounter.ToString();
                item.SubItems.Add( e.DocIndex.ToString() );
                item.SubItems.Add( e.Offsets.Length.ToString() );
                if( hash.ContainsKey( e.DocIndex ))
                {
                    ListViewItem refItem = listEntries.Items[ hash[ e.DocIndex ] - 1 ];
                    item.ForeColor = refItem.ForeColor = Color.Red;
                    ListViewItem.ListViewSubItem prev2 = refItem.SubItems[ 3 ];
                    prev2.Text = prev2.Text + ">>" + entryCounter.ToString();

                    item.SubItems.Add( "<<" + hash[ e.DocIndex ].ToString() );
                }
                else
                    item.SubItems.Add( "" );
                hash[ e.DocIndex ] = entryCounter;

                if( !Core.TextIndexManager.IsDocumentInIndex( e.DocIndex ))
                    item.Font = delFont;
                listEntries.Items.Add( item );
            }
        }
	}
}
