// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
	/// <summary>
	/// Summary description for UnsubscribeBox.
	/// </summary>
	public class UnsubscribeForm : DialogBase
	{
        private JetLinkLabel _warningLabel;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private JetLinkLabel _groupName;
        private System.Windows.Forms.CheckBox _preserveArchiveBox;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private UnsubscribeForm()
		{
			InitializeComponent();
            this.Icon = Core.UIManager.ApplicationIcon;
		}

        public static DialogResult Unsubscribe( IResourceList groups, out bool deleteArticles )
        {
            if( groups.Count > 0 )
            {
                if( NewsgroupResource.AllUnsubscribed( groups ) )
                {
                    deleteArticles = true;
                    if( groups.Count == 1 )
                    {
                        return MessageBox.Show(
                            "Do you wish to delete " + groups[ 0 ].DisplayName + " with all messages?",
                            "Remove Newsgroup", MessageBoxButtons.OKCancel, MessageBoxIcon.Question );
                    }
                    else
                    {
                        return MessageBox.Show(
                            "Do you wish to delete selected newsgroups with all messages?",
                            "Remove Newsgroups", MessageBoxButtons.OKCancel, MessageBoxIcon.Question );
                    }
                }
                else
                {
                    UnsubscribeForm theForm = new UnsubscribeForm();
                    theForm._warningLabel.Text = "Do you wish to unsubscribe from";
                    if( groups.Count == 1 )
                    {
                        theForm._groupName.Text = groups[ 0 ].DisplayName + "?";
                    }
                    else
                    {
                        theForm._groupName.Text = groups.Count.ToString() + " selected newsgroups?";
                    }
                    DialogResult result = theForm.ShowDialog( Core.MainWindow );
                    deleteArticles = !theForm._preserveArchiveBox.Checked;
                    return result;
                }
            }
            deleteArticles = false;
            return DialogResult.Cancel;
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(UnsubscribeForm));
            this._warningLabel = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this._preserveArchiveBox = new System.Windows.Forms.CheckBox();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._groupName = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this.SuspendLayout();
            //
            // _warningLabel
            //
            this._warningLabel.ClickableLink = false;
            this._warningLabel.Cursor = System.Windows.Forms.Cursors.Default;
            this._warningLabel.ForeColor = System.Drawing.SystemColors.ControlText;
            this._warningLabel.Location = new System.Drawing.Point(24, 16);
            this._warningLabel.Name = "_warningLabel";
            this._warningLabel.Size = new System.Drawing.Size(0, 13);
            this._warningLabel.TabIndex = 3;
            this._warningLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // _preserveArchiveBox
            //
            this._preserveArchiveBox.Checked = true;
            this._preserveArchiveBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this._preserveArchiveBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._preserveArchiveBox.Location = new System.Drawing.Point(72, 32);
            this._preserveArchiveBox.Name = "_preserveArchiveBox";
            this._preserveArchiveBox.Size = new System.Drawing.Size(128, 26);
            this._preserveArchiveBox.TabIndex = 0;
            this._preserveArchiveBox.Text = "Preserve history";
            //
            // _okButton
            //
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(100, 72);
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 1;
            this._okButton.Text = "OK";
            //
            // _cancelButton
            //
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(184, 72);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 2;
            this._cancelButton.Text = "Cancel";
            //
            // _groupName
            //
            this._groupName.ClickableLink = false;
            this._groupName.Cursor = System.Windows.Forms.Cursors.Default;
            this._groupName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._groupName.ForeColor = System.Drawing.SystemColors.ControlText;
            this._groupName.Location = new System.Drawing.Point(176, 16);
            this._groupName.Name = "_groupName";
            this._groupName.Size = new System.Drawing.Size(0, 13);
            this._groupName.TabIndex = 4;
            this._groupName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // UnsubscribeForm
            //
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(370, 107);
            this.Controls.Add(this._groupName);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._preserveArchiveBox);
            this.Controls.Add(this._warningLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "UnsubscribeForm";
            this.Text = "Remove Subscription";
            this.Activated += new System.EventHandler(this.UnsubscribeForm_Activated);
            this.ResumeLayout(false);
        }
		#endregion

        private void UnsubscribeForm_Activated(object sender, System.EventArgs e)
        {
            _groupName.Left = _warningLabel.Left + _warningLabel.Width + 4;
            Width = _groupName.Left + _groupName.Width + 36;
            _okButton.Left = Width / 2 - _okButton.Width - 8;
            _preserveArchiveBox.Left = _okButton.Left + 24;
            _cancelButton.Left = _okButton.Left + _okButton.Width + 8;
        }
	}
}
