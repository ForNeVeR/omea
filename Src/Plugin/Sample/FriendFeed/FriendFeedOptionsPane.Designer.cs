// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Omea.SamplePlugins.FriendFeed
{
    partial class FriendFeedOptionsPane
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this._edtNickname = new System.Windows.Forms.TextBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this._edtRemoteKey = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(151, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "FriendFeed nickname or email:";
            //
            // _edtNickname
            //
            this._edtNickname.Location = new System.Drawing.Point(169, 2);
            this._edtNickname.Name = "_edtNickname";
            this._edtNickname.Size = new System.Drawing.Size(100, 20);
            this._edtNickname.TabIndex = 1;
            //
            // linkLabel1
            //
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.LinkArea = new System.Windows.Forms.LinkArea(13, 13);
            this.linkLabel1.Location = new System.Drawing.Point(3, 30);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(147, 17);
            this.linkLabel1.TabIndex = 3;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Remote key [ find your key ]:";
            this.linkLabel1.UseCompatibleTextRendering = true;
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            //
            // _edtRemoteKey
            //
            this._edtRemoteKey.Location = new System.Drawing.Point(169, 27);
            this._edtRemoteKey.Name = "_edtRemoteKey";
            this._edtRemoteKey.Size = new System.Drawing.Size(100, 20);
            this._edtRemoteKey.TabIndex = 4;
            //
            // FriendFeedOptionsPane
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._edtRemoteKey);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this._edtNickname);
            this.Controls.Add(this.label1);
            this.Name = "FriendFeedOptionsPane";
            this.Size = new System.Drawing.Size(379, 150);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _edtNickname;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.TextBox _edtRemoteKey;
    }
}
