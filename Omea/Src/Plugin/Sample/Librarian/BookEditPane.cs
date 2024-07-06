// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.Librarian
{
	/// <summary>
	/// Pane for editing the book data.
	/// </summary>
	public class BookEditPane: AbstractEditPane
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _edtName;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.TextBox _edtAuthors;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown _udYear;
        private System.Windows.Forms.TextBox _edtIsbn;
        private System.Windows.Forms.Label label4;

        private IResource _book;

		public BookEditPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

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
            this.label1 = new System.Windows.Forms.Label();
            this._edtName = new System.Windows.Forms.TextBox();
            this._edtAuthors = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this._udYear = new System.Windows.Forms.NumericUpDown();
            this._edtIsbn = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this._udYear)).BeginInit();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(4, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            //
            // _edtName
            //
            this._edtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtName.Location = new System.Drawing.Point(108, 4);
            this._edtName.Name = "_edtName";
            this._edtName.Size = new System.Drawing.Size(328, 21);
            this._edtName.TabIndex = 1;
            this._edtName.Text = "textBox1";
            //
            // _edtAuthors
            //
            this._edtAuthors.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtAuthors.Location = new System.Drawing.Point(108, 28);
            this._edtAuthors.Name = "_edtAuthors";
            this._edtAuthors.Size = new System.Drawing.Size(328, 21);
            this._edtAuthors.TabIndex = 3;
            this._edtAuthors.Text = "textBox1";
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(4, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "Authors:";
            //
            // label3
            //
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(4, 56);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 16);
            this.label3.TabIndex = 4;
            this.label3.Text = "Year:";
            //
            // _udYear
            //
            this._udYear.Location = new System.Drawing.Point(108, 52);
            this._udYear.Maximum = new System.Decimal(new int[] {
                                                                    2050,
                                                                    0,
                                                                    0,
                                                                    0});
            this._udYear.Name = "_udYear";
            this._udYear.Size = new System.Drawing.Size(56, 21);
            this._udYear.TabIndex = 5;
            this._udYear.Value = new System.Decimal(new int[] {
                                                                  2000,
                                                                  0,
                                                                  0,
                                                                  0});
            //
            // _edtISBN
            //
            this._edtIsbn.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtIsbn.Location = new System.Drawing.Point(108, 76);
            this._edtIsbn.Name = "_edtIsbn";
            this._edtIsbn.Size = new System.Drawing.Size(328, 21);
            this._edtIsbn.TabIndex = 7;
            this._edtIsbn.Text = "textBox1";
            //
            // label4
            //
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(4, 80);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 16);
            this.label4.TabIndex = 6;
            this.label4.Text = "ISBN (optional):";
            //
            // BookEditPane
            //
            this.Controls.Add(this._edtIsbn);
            this.Controls.Add(this.label4);
            this.Controls.Add(this._udYear);
            this.Controls.Add(this.label3);
            this.Controls.Add(this._edtAuthors);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._edtName);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "BookEditPane";
            this.Size = new System.Drawing.Size(440, 150);
            ((System.ComponentModel.ISupportInitialize)(this._udYear)).EndInit();
            this.ResumeLayout(false);

        }
		#endregion

	    /// <summary>
	    /// Fills the form with the values of the properties for the specified resource.
	    /// </summary>
	    /// <param name="res">The resource to edit.</param>
        public override void EditResource( IResource res )
	    {
            _book = res;
	        _edtName.Text = res.GetStringProp( "Name" );

            StringBuilder authorBuilder = new StringBuilder();
            foreach( IResource author in _book.GetLinksOfType( "Contact", PropTypes.BookAuthor ) )
            {
                if ( authorBuilder.Length > 0 )
                {
                    authorBuilder.Append( "," );
                }
                authorBuilder.Append( author.DisplayName );
            }
            _edtAuthors.Text = authorBuilder.ToString();

            if ( res.HasProp( PropTypes.PubYear ) )
            {
                _udYear.Value = res.GetIntProp( PropTypes.PubYear );
            }
	        _edtIsbn.Text = res.GetStringProp( PropTypes.Isbn );
        }

	    /// <summary>
	    /// Called when the OK button has been pressed. Runs the resource saving
	    /// code as a resource thread operation.
	    /// </summary>
        public override void Save()
	    {
            Core.ResourceAP.RunJob( new MethodInvoker( DoSave ) );
            Core.TextIndexManager.QueryIndexing( _book.Id );
	    }

	    /// <summary>
	    /// Runs in the resource thread and saves the data entered by the user
	    /// to the resource store.
	    /// </summary>
        private void DoSave()
        {
            _book.SetProp( "Name", _edtName.Text );
            _book.DeleteLinks( PropTypes.BookAuthor );

            // Parse the list of author names separated with
            string[] authors = _edtAuthors.Text.Split( ',', ';' );
            foreach( string author in authors )
            {
                if ( author.Trim().Length == 0 )
                {
                    continue;
                }
                IContact contact = Core.ContactManager.FindOrCreateContact( null, author.Trim() );
                if ( contact != null )
                {
                    _book.AddLink( PropTypes.BookAuthor, contact.Resource );
                }
            }

            _book.SetProp( PropTypes.PubYear, (int) _udYear.Value );

            if ( _edtIsbn.Text.Length > 0 )
            {
                _book.SetProp( PropTypes.Isbn, _edtIsbn.Text );
            }
            else
            {
                _book.DeleteProp( PropTypes.Isbn );
            }
        }
	}
}
