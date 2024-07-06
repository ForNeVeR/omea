// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin.SubscribeWizard
{
	/// <summary>
	/// Summary description for SearchSyndic8Pane.
	/// </summary>
	public class SearchSyndic8Pane : System.Windows.Forms.UserControl
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox _chkSearchSyndic8;
        private System.Windows.Forms.Label _lblProgress;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public SearchSyndic8Pane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            this.label1.Text = Core.ProductName + " could not find a feed on the specified site. " +
                Core.ProductName + " can now perform a " +
                "search on Syndic8.com to find a feed for the site that is located elsewhere.";
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
            this._chkSearchSyndic8 = new System.Windows.Forms.CheckBox();
            this._lblProgress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(320, 52);
            this.label1.TabIndex = 0;
            this.label1.Text = "OmniaMea could not find a feed on the specified site. OmniaMea can now perform a " +
                               "search on Syndic8.com to find a feed for the site that is located elsewhere.";
            this.label1.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            //
            // _chkSearchSyndic8
            //
            this._chkSearchSyndic8.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkSearchSyndic8.Location = new System.Drawing.Point(8, 88);
            this._chkSearchSyndic8.Name = "_chkSearchSyndic8";
            this._chkSearchSyndic8.Size = new System.Drawing.Size(268, 24);
            this._chkSearchSyndic8.TabIndex = 1;
            this._chkSearchSyndic8.Text = "Search for the feed on Syndic8.com";
            //
            // _lblProgress
            //
            this._lblProgress.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblProgress.Location = new System.Drawing.Point(24, 268);
            this._lblProgress.Name = "_lblProgress";
            this._lblProgress.Size = new System.Drawing.Size(336, 16);
            this._lblProgress.TabIndex = 6;
            //
            // SearchSyndic8Pane
            //
            this.Controls.Add(this._lblProgress);
            this.Controls.Add(this._chkSearchSyndic8);
            this.Controls.Add(this.label1);
            this.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            this.Name = "SearchSyndic8Pane";
            this.Size = new System.Drawing.Size(384, 396);
            this.ResumeLayout(false);

        }
		#endregion

        public bool SearchSyndic8
        {
            get { return _chkSearchSyndic8.Checked; }
            set { _chkSearchSyndic8.Checked = value; }
        }

	    public Label ProgressLabel
	    {
	        get { return _lblProgress; }
	    }
	}
}
