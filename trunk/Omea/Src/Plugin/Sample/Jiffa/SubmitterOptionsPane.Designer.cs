/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.Jiffa
{
	partial class SubmitterOptionsPane
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
			this._panelProject = new System.Windows.Forms.Panel();
			this._comboProject = new JetBrains.Omea.GUIControls.ResourceComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this._panelTemplate = new System.Windows.Forms.Panel();
			this.label3 = new System.Windows.Forms.Label();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this._panelDevelopers = new System.Windows.Forms.Panel();
			this._labelBuildCF = new System.Windows.Forms.Label();
			this._labelOriginalUriCF = new System.Windows.Forms.Label();
			this._txtBuildCF = new System.Windows.Forms.TextBox();
			this._txtOriginalUriCF = new System.Windows.Forms.TextBox();
			this._checkEnableMru = new System.Windows.Forms.CheckBox();
			this._labelBuildNumberMask = new System.Windows.Forms.Label();
			this._txtBuildNumberMask = new System.Windows.Forms.TextBox();
			this._panelProject.SuspendLayout();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 6);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(90, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Submit to &Project:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// _panelProject
			// 
			this._panelProject.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this._panelProject.Controls.Add(this._checkEnableMru);
			this._panelProject.Controls.Add(this._txtBuildNumberMask);
			this._panelProject.Controls.Add(this._txtOriginalUriCF);
			this._panelProject.Controls.Add(this._labelBuildNumberMask);
			this._panelProject.Controls.Add(this._txtBuildCF);
			this._panelProject.Controls.Add(this._labelOriginalUriCF);
			this._panelProject.Controls.Add(this._labelBuildCF);
			this._panelProject.Controls.Add(this.label1);
			this._panelProject.Controls.Add(this._comboProject);
			this._panelProject.Dock = System.Windows.Forms.DockStyle.Top;
			this._panelProject.Location = new System.Drawing.Point(0, 0);
			this._panelProject.Name = "_panelProject";
			this._panelProject.Size = new System.Drawing.Size(573, 131);
			this._panelProject.TabIndex = 1;
			// 
			// _comboProject
			// 
			this._comboProject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._comboProject.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this._comboProject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboProject.FormattingEnabled = true;
			this._comboProject.Location = new System.Drawing.Point(99, 3);
			this._comboProject.Name = "_comboProject";
			this._comboProject.Size = new System.Drawing.Size(471, 21);
			this._comboProject.TabIndex = 1;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Dock = System.Windows.Forms.DockStyle.Top;
			this.label2.Location = new System.Drawing.Point(0, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(114, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "News Reply &Template:";
			// 
			// _panelTemplate
			// 
			this._panelTemplate.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this._panelTemplate.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panelTemplate.Location = new System.Drawing.Point(0, 13);
			this._panelTemplate.Name = "_panelTemplate";
			this._panelTemplate.Size = new System.Drawing.Size(573, 187);
			this._panelTemplate.TabIndex = 3;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Dock = System.Windows.Forms.DockStyle.Top;
			this.label3.Location = new System.Drawing.Point(0, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(83, 13);
			this.label3.TabIndex = 4;
			this.label3.Text = "&Developers List:";
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 131);
			this.splitContainer1.Margin = new System.Windows.Forms.Padding(5);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this._panelTemplate);
			this.splitContainer1.Panel1.Controls.Add(this.label2);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this._panelDevelopers);
			this.splitContainer1.Panel2.Controls.Add(this.label3);
			this.splitContainer1.Size = new System.Drawing.Size(573, 278);
			this.splitContainer1.SplitterDistance = 200;
			this.splitContainer1.TabIndex = 5;
			// 
			// _panelDevelopers
			// 
			this._panelDevelopers.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this._panelDevelopers.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panelDevelopers.Location = new System.Drawing.Point(0, 13);
			this._panelDevelopers.Name = "_panelDevelopers";
			this._panelDevelopers.Size = new System.Drawing.Size(573, 61);
			this._panelDevelopers.TabIndex = 5;
			// 
			// _labelBuildCF
			// 
			this._labelBuildCF.AutoSize = true;
			this._labelBuildCF.Location = new System.Drawing.Point(3, 33);
			this._labelBuildCF.Name = "_labelBuildCF";
			this._labelBuildCF.Size = new System.Drawing.Size(135, 13);
			this._labelBuildCF.TabIndex = 0;
			this._labelBuildCF.Text = "“Build” Custom Field Name:";
			this._labelBuildCF.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// _labelOriginalUriCF
			// 
			this._labelOriginalUriCF.AutoSize = true;
			this._labelOriginalUriCF.Location = new System.Drawing.Point(3, 59);
			this._labelOriginalUriCF.Name = "_labelOriginalUriCF";
			this._labelOriginalUriCF.Size = new System.Drawing.Size(169, 13);
			this._labelOriginalUriCF.TabIndex = 0;
			this._labelOriginalUriCF.Text = "“Original URI” Custom Field Name:";
			this._labelOriginalUriCF.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// _txtBuildCF
			// 
			this._txtBuildCF.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._txtBuildCF.Location = new System.Drawing.Point(175, 30);
			this._txtBuildCF.Name = "_txtBuildCF";
			this._txtBuildCF.Size = new System.Drawing.Size(395, 20);
			this._txtBuildCF.TabIndex = 2;
			// 
			// _txtOriginalUriCF
			// 
			this._txtOriginalUriCF.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._txtOriginalUriCF.Location = new System.Drawing.Point(175, 56);
			this._txtOriginalUriCF.Name = "_txtOriginalUriCF";
			this._txtOriginalUriCF.Size = new System.Drawing.Size(395, 20);
			this._txtOriginalUriCF.TabIndex = 2;
			// 
			// _checkEnableMru
			// 
			this._checkEnableMru.AutoSize = true;
			this._checkEnableMru.Location = new System.Drawing.Point(6, 106);
			this._checkEnableMru.Name = "_checkEnableMru";
			this._checkEnableMru.Size = new System.Drawing.Size(189, 17);
			this._checkEnableMru.TabIndex = 3;
			this._checkEnableMru.Text = "Remember MRU Values in Dialogs";
			this._checkEnableMru.UseVisualStyleBackColor = true;
			// 
			// _labelBuildNumberMask
			// 
			this._labelBuildNumberMask.AutoSize = true;
			this._labelBuildNumberMask.Location = new System.Drawing.Point(3, 83);
			this._labelBuildNumberMask.Name = "_labelBuildNumberMask";
			this._labelBuildNumberMask.Size = new System.Drawing.Size(102, 13);
			this._labelBuildNumberMask.TabIndex = 0;
			this._labelBuildNumberMask.Text = "Build Number Mask:";
			this._labelBuildNumberMask.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// _txtBuildNumberMask
			// 
			this._txtBuildNumberMask.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._txtBuildNumberMask.Location = new System.Drawing.Point(175, 80);
			this._txtBuildNumberMask.Name = "_txtBuildNumberMask";
			this._txtBuildNumberMask.Size = new System.Drawing.Size(395, 20);
			this._txtBuildNumberMask.TabIndex = 2;
			// 
			// SubmitterOptionsPane
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this._panelProject);
			this.Name = "SubmitterOptionsPane";
			this.Size = new System.Drawing.Size(573, 409);
			this._panelProject.ResumeLayout(false);
			this._panelProject.PerformLayout();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel _panelProject;
		private ResourceComboBox _comboProject;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Panel _panelTemplate;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.Panel _panelDevelopers;
		private System.Windows.Forms.Label _labelBuildCF;
		private System.Windows.Forms.Label _labelOriginalUriCF;
		private System.Windows.Forms.CheckBox _checkEnableMru;
		private System.Windows.Forms.TextBox _txtOriginalUriCF;
		private System.Windows.Forms.TextBox _txtBuildCF;
		private System.Windows.Forms.TextBox _txtBuildNumberMask;
		private System.Windows.Forms.Label _labelBuildNumberMask;
	}
}
