// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.Librarian
{
	/// <summary>
	/// Pane for displaying the book data.
	/// </summary>
	public class BookDisplayPane : System.Windows.Forms.UserControl, IDisplayPane
	{
        private System.Windows.Forms.Label _lblCaption;
        private System.Windows.Forms.Label _lblPubYear;
        private System.Windows.Forms.Label _lblIsbn;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public BookDisplayPane()
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
            this._lblCaption = new System.Windows.Forms.Label();
            this._lblPubYear = new System.Windows.Forms.Label();
            this._lblIsbn = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // _captionLabel
            //
            this._lblCaption.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblCaption.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblCaption.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._lblCaption.Location = new System.Drawing.Point(4, 4);
            this._lblCaption.Name = "_lblCaption";
            this._lblCaption.Size = new System.Drawing.Size(224, 16);
            this._lblCaption.TabIndex = 0;
            this._lblCaption.Text = "label1";
            //
            // _lblPubYear
            //
            this._lblPubYear.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblPubYear.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblPubYear.Location = new System.Drawing.Point(4, 24);
            this._lblPubYear.Name = "_lblPubYear";
            this._lblPubYear.Size = new System.Drawing.Size(220, 16);
            this._lblPubYear.TabIndex = 1;
            this._lblPubYear.Text = "Publication Year:";
            //
            // _lblIsbn
            //
            this._lblIsbn.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblIsbn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblIsbn.Location = new System.Drawing.Point(4, 40);
            this._lblIsbn.Name = "_lblIsbn";
            this._lblIsbn.Size = new System.Drawing.Size(220, 16);
            this._lblIsbn.TabIndex = 2;
            this._lblIsbn.Text = "ISBN:";
            //
            // BookDisplayPane
            //
            this.Controls.Add(this._lblIsbn);
            this.Controls.Add(this._lblPubYear);
            this.Controls.Add(this._lblCaption);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "BookDisplayPane";
            this.Size = new System.Drawing.Size(232, 150);
            this.ResumeLayout(false);

        }
		#endregion

        public Control GetControl()
        {
            return this;
        }

        public void DisplayResource( IResource resource )
        {
            _lblCaption.Text = resource.GetStringProp( "Name" );
            _lblPubYear.Text = "Publication Year: " + resource.GetPropText( PropTypes.PubYear );
            if ( resource.HasProp( PropTypes.Isbn ) )
            {
                _lblIsbn.Text = "ISBN: " + resource.GetPropText( PropTypes.Isbn );
            }
            else
            {
                _lblIsbn.Text = "ISBN: Not Specified";
            }
        }

        public bool CanExecuteCommand( string command )
	    {
            return false;
	    }

	    public void HighlightWords( WordPtr[] words )
	    {
	    }

	    public void EndDisplayResource( IResource resource )
	    {
	    }

	    public void DisposePane()
	    {
	    }

	    public string GetSelectedText( ref TextFormat format )
	    {
            return null;
	    }

	    public string GetSelectedPlainText()
	    {
	        return null;
	    }

	    public void ExecuteCommand( string command )
	    {
	    }
	}
}
