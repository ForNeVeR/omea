// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.FilePlugin
{
	internal class FileOptionsPane: AbstractOptionsPane
	{
        private System.Windows.Forms.TextBox _textExtsList;
        private System.Windows.Forms.Label _textExtsLabel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox _indexHiddenCheckBox;
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.CheckBox _viewHiddenCheckBox;
        private bool _wereChanges;

		public FileOptionsPane()
		{
			InitializeComponent();
            _wereChanges = false;
		}

        public static AbstractOptionsPane FileOptionsPaneCreator()
        {
            return new FileOptionsPane();
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
            this._textExtsList = new System.Windows.Forms.TextBox();
            this._textExtsLabel = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this._indexHiddenCheckBox = new System.Windows.Forms.CheckBox();
            this._viewHiddenCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            //
            // _textExtsList
            //
            this._textExtsList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._textExtsList.Location = new System.Drawing.Point(0, 24);
            this._textExtsList.Multiline = true;
            this._textExtsList.Name = "_textExtsList";
            this._textExtsList.Size = new System.Drawing.Size(376, 40);
            this._textExtsList.TabIndex = 8;
            this._textExtsList.Text = "";
            this._textExtsList.TextChanged += new System.EventHandler(this._textExtsList_TextChanged);
            //
            // _textExtsLabel
            //
            this._textExtsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._textExtsLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._textExtsLabel.Location = new System.Drawing.Point(0, 4);
            this._textExtsLabel.Name = "_textExtsLabel";
            this._textExtsLabel.Size = new System.Drawing.Size(376, 16);
            this._textExtsLabel.TabIndex = 9;
            this._textExtsLabel.Text = "List file extensions recognized as plain text:";
            this._textExtsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Location = new System.Drawing.Point(0, 96);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(376, 4);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            //
            // label1
            //
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(0, 68);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(376, 16);
            this.label1.TabIndex = 11;
            this.label1.Text = "Use comma \',\' to separate entries";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _indexHiddenCheckBox
            //
            this._indexHiddenCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._indexHiddenCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._indexHiddenCheckBox.Location = new System.Drawing.Point(0, 112);
            this._indexHiddenCheckBox.Name = "_indexHiddenCheckBox";
            this._indexHiddenCheckBox.Size = new System.Drawing.Size(376, 24);
            this._indexHiddenCheckBox.TabIndex = 12;
            this._indexHiddenCheckBox.Text = "Index hidden files";
            this._indexHiddenCheckBox.CheckedChanged += new System.EventHandler(this._textExtsList_TextChanged);
            //
            // _viewHiddenCheckBox
            //
            this._viewHiddenCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._viewHiddenCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._viewHiddenCheckBox.Location = new System.Drawing.Point(0, 136);
            this._viewHiddenCheckBox.Name = "_viewHiddenCheckBox";
            this._viewHiddenCheckBox.Size = new System.Drawing.Size(376, 24);
            this._viewHiddenCheckBox.TabIndex = 13;
            this._viewHiddenCheckBox.Text = "View hidden files in File Browser";
            this._viewHiddenCheckBox.CheckedChanged += new System.EventHandler(this._textExtsList_TextChanged);
            //
            // FileOptionsPane
            //
            this.Controls.Add(this._viewHiddenCheckBox);
            this.Controls.Add(this._indexHiddenCheckBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this._textExtsLabel);
            this.Controls.Add(this._textExtsList);
            this.Name = "FileOptionsPane";
            this.Size = new System.Drawing.Size(376, 304);
            this.ResumeLayout(false);

        }
		#endregion

        public override void ShowPane()
        {
            ISettingStore settings = Core.SettingStore;
            string exts = settings.ReadString( "FilePlugin", "PlainTextExts" );
            if( exts.Length == 0 )
            {
                exts = ".txt";
            }
            _textExtsList.Text = exts;
            _indexHiddenCheckBox.Checked = settings.ReadBool( "FilePlugin", "IndexHidden", false );
            _viewHiddenCheckBox.Checked = settings.ReadBool( "FilePlugin", "ViewHidden", false );
        }

        public override void OK()
        {
            string exts = _textExtsList.Text;
            if( exts.Length == 0 )
            {
                exts = ".txt";
                _wereChanges = true;
            }
            if( _wereChanges )
            {
                ISettingStore settings = Core.SettingStore;
                settings.WriteString( "FilePlugin", "PlainTextExts", exts );
                settings.WriteBool( "FilePlugin", "IndexHidden", _indexHiddenCheckBox.Checked );
                settings.WriteBool( "FilePlugin", "ViewHidden", _viewHiddenCheckBox.Checked );
                if( !IsStartupPane )
                {
                    FoldersCollection.Instance.Interrupted = true;
                    FoldersCollection.Instance.WaitUntilFinished();
                    FoldersCollection.LoadFoldersForest();
                }
            }
        }

        private void _textExtsList_TextChanged(object sender, System.EventArgs e)
        {
            _wereChanges = true;
        }

	    public override string GetHelpKeyword()
	    {
	        return "/reference/file_options.htm";
	    }
	}
}
