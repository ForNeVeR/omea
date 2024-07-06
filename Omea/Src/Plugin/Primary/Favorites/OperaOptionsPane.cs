// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
	internal class OperaOptionsPane : BookmarksOptionsPane
	{
        private System.Windows.Forms.CheckBox _importCheckBox;
        private System.Windows.Forms.GroupBox _howToImportGroupBox;
        private System.Windows.Forms.RadioButton _importOnStartupButton;
        private System.Windows.Forms.RadioButton _importImmediatelyButton;
		private System.ComponentModel.Container components = null;

		private OperaOptionsPane()
		{
			InitializeComponent();
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

        public static AbstractOptionsPane CreatePane()
        {
            return new OperaOptionsPane();
        }

        public override void ShowPane()
        {
            if( OperaBookmarkProfile.ImportImmediately )
            {
                _importImmediatelyButton.Checked = true;
            }
            else
            {
                _importOnStartupButton.Checked = true;
            }
            _howToImportGroupBox.Enabled = _importCheckBox.Checked = OperaBookmarkProfile.ImportAllowed;
            _howToImportGroupBox.Visible = !IsStartupPane;
        }

        public override void OK()
        {
            if( OperaBookmarkProfile.ImportAllowed = _importCheckBox.Checked )
            {
                OperaBookmarkProfile.ImportImmediately = _importImmediatelyButton.Checked;
            }
            if( !IsStartupPane )
            {
                FavoritesPlugin._operaProfile.AsyncUpdateBookmarks();
            }
        }

        public override int OccupiedHeight
        {
            get
            {
                if( _howToImportGroupBox.Visible )
                {
                    return _howToImportGroupBox.Top + _howToImportGroupBox.Height + 4;
                }
                return _importCheckBox.Top + _importCheckBox.Height + 4;
            }
        }

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._importCheckBox = new System.Windows.Forms.CheckBox();
            this._howToImportGroupBox = new System.Windows.Forms.GroupBox();
            this._importImmediatelyButton = new System.Windows.Forms.RadioButton();
            this._importOnStartupButton = new System.Windows.Forms.RadioButton();
            this._howToImportGroupBox.SuspendLayout();
            this.SuspendLayout();
            //
            // _importCheckBox
            //
            this._importCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._importCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._importCheckBox.Location = new System.Drawing.Point(0, 0);
            this._importCheckBox.Name = "_importCheckBox";
            this._importCheckBox.Size = new System.Drawing.Size(452, 24);
            this._importCheckBox.TabIndex = 2;
            this._importCheckBox.Text = "Import bookmarks from &Opera";
            this._importCheckBox.CheckedChanged += new System.EventHandler(this._importCheckBox_CheckedChanged);
            //
            // _howToImportGroupBox
            //
            this._howToImportGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._howToImportGroupBox.Controls.Add(this._importImmediatelyButton);
            this._howToImportGroupBox.Controls.Add(this._importOnStartupButton);
            this._howToImportGroupBox.Location = new System.Drawing.Point(0, 28);
            this._howToImportGroupBox.Name = "_howToImportGroupBox";
            this._howToImportGroupBox.Size = new System.Drawing.Size(456, 68);
            this._howToImportGroupBox.TabIndex = 3;
            this._howToImportGroupBox.TabStop = false;
            this._howToImportGroupBox.Text = "&How to import";
            //
            // _importImmediatelyButton
            //
            this._importImmediatelyButton.Location = new System.Drawing.Point(8, 16);
            this._importImmediatelyButton.Name = "_importImmediatelyButton";
            this._importImmediatelyButton.Size = new System.Drawing.Size(436, 24);
            this._importImmediatelyButton.TabIndex = 1;
            this._importImmediatelyButton.Text = "Immediately &after changes";
            //
            // _importOnStartupButton
            //
            this._importOnStartupButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._importOnStartupButton.Location = new System.Drawing.Point(8, 40);
            this._importOnStartupButton.Name = "_importOnStartupButton";
            this._importOnStartupButton.Size = new System.Drawing.Size(440, 24);
            this._importOnStartupButton.TabIndex = 0;
            this._importOnStartupButton.Text = "On &startup only";
            //
            // OperaOptionsPane
            //
            this.Controls.Add(this._howToImportGroupBox);
            this.Controls.Add(this._importCheckBox);
            this.Name = "OperaOptionsPane";
            this.Size = new System.Drawing.Size(456, 124);
            this._howToImportGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        private void _importCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            _howToImportGroupBox.Enabled = _importCheckBox.Checked;
        }

	    public override string GetHelpKeyword()
	    {
	        return "/reference/opera_bookmarks.htm";
	    }
	}
}
