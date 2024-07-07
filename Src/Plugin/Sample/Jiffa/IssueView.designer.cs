// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.Jiffa
{
	partial class IssueView
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._menuSubmit = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this._menuClose = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this._panelHeaderFlowControls = new System.Windows.Forms.FlowLayoutPanel();
			this._labelIssueType = new System.Windows.Forms.Label();
			this._comboIssueType = new JetBrains.Omea.GUIControls.ResourceComboBox();
			this._labelStatus = new System.Windows.Forms.Label();
			this._comboStatus = new JetBrains.Omea.GUIControls.ResourceComboBox();
			this._labelPriority = new System.Windows.Forms.Label();
			this._comboPriority = new JetBrains.Omea.GUIControls.ResourceComboBox();
			this._labelComponent = new System.Windows.Forms.Label();
			this._comboComponent = new JetBrains.Omea.GUIControls.ResourceComboBox();
			this._labelBuildNumber = new System.Windows.Forms.Label();
			this._txtBuildNumber = new System.Windows.Forms.TextBox();
			this._panelHeaderControls = new System.Windows.Forms.TableLayoutPanel();
			this._comboDeveloper = new System.Windows.Forms.ComboBox();
			this._labelDeveloper = new System.Windows.Forms.Label();
			this._labelTitle = new System.Windows.Forms.Label();
			this._txtTitle = new System.Windows.Forms.TextBox();
			this._panelBrowser = new System.Windows.Forms.Panel();
			this.menuStrip1.SuspendLayout();
			this._panelHeaderFlowControls.SuspendLayout();
			this._panelHeaderControls.SuspendLayout();
			this.SuspendLayout();
			//
			// menuStrip1
			//
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
			this.menuStrip1.Size = new System.Drawing.Size(1070, 24);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "_menu";
			//
			// fileToolStripMenuItem
			//
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._menuSubmit,
            this.toolStripSeparator1,
            this._menuClose});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
			this.fileToolStripMenuItem.Text = "File";
			//
			// _menuSubmit
			//
			this._menuSubmit.Name = "_menuSubmit";
			this._menuSubmit.ShortcutKeyDisplayString = "Ctrl+Enter";
			this._menuSubmit.Size = new System.Drawing.Size(175, 22);
			this._menuSubmit.Text = "&Submit";
			this._menuSubmit.Click += new System.EventHandler(this.OnSubmit);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(172, 6);
			//
			// _menuClose
			//
			this._menuClose.Name = "_menuClose";
			this._menuClose.ShortcutKeyDisplayString = "Esc";
			this._menuClose.Size = new System.Drawing.Size(175, 22);
			this._menuClose.Text = "&Close";
			//
			// statusStrip1
			//
			this.statusStrip1.Location = new System.Drawing.Point(0, 414);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
			this.statusStrip1.Size = new System.Drawing.Size(1070, 22);
			this.statusStrip1.TabIndex = 1;
			this.statusStrip1.Text = "_status";
			//
			// _panelHeaderFlowControls
			//
			this._panelHeaderControls.SetColumnSpan(this._panelHeaderFlowControls, 2);
			this._panelHeaderFlowControls.Controls.Add(this._labelIssueType);
			this._panelHeaderFlowControls.Controls.Add(this._comboIssueType);
			this._panelHeaderFlowControls.Controls.Add(this._labelStatus);
			this._panelHeaderFlowControls.Controls.Add(this._comboStatus);
			this._panelHeaderFlowControls.Controls.Add(this._labelPriority);
			this._panelHeaderFlowControls.Controls.Add(this._comboPriority);
			this._panelHeaderFlowControls.Controls.Add(this._labelComponent);
			this._panelHeaderFlowControls.Controls.Add(this._comboComponent);
			this._panelHeaderFlowControls.Controls.Add(this._labelBuildNumber);
			this._panelHeaderFlowControls.Controls.Add(this._txtBuildNumber);
			this._panelHeaderFlowControls.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panelHeaderFlowControls.Location = new System.Drawing.Point(3, 30);
			this._panelHeaderFlowControls.Name = "_panelHeaderFlowControls";
			this._panelHeaderFlowControls.Size = new System.Drawing.Size(1064, 27);
			this._panelHeaderFlowControls.TabIndex = 3;
			//
			// _labelIssueType
			//
			this._labelIssueType.AutoSize = true;
			this._labelIssueType.Dock = System.Windows.Forms.DockStyle.Left;
			this._labelIssueType.Location = new System.Drawing.Point(3, 0);
			this._labelIssueType.Name = "_labelIssueType";
			this._labelIssueType.Size = new System.Drawing.Size(40, 28);
			this._labelIssueType.TabIndex = 0;
			this._labelIssueType.Text = "Type:";
			this._labelIssueType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _comboIssueType
			//
			this._comboIssueType.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this._comboIssueType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboIssueType.FormattingEnabled = true;
			this._comboIssueType.Location = new System.Drawing.Point(49, 3);
			this._comboIssueType.MaxDropDownItems = 16;
			this._comboIssueType.Name = "_comboIssueType";
			this._comboIssueType.Size = new System.Drawing.Size(140, 22);
			this._comboIssueType.Sorted = true;
			this._comboIssueType.TabIndex = 1;
			//
			// _labelStatus
			//
			this._labelStatus.AutoSize = true;
			this._labelStatus.Dock = System.Windows.Forms.DockStyle.Left;
			this._labelStatus.Location = new System.Drawing.Point(195, 0);
			this._labelStatus.Name = "_labelStatus";
			this._labelStatus.Size = new System.Drawing.Size(48, 28);
			this._labelStatus.TabIndex = 8;
			this._labelStatus.Text = "Status:";
			this._labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _comboStatus
			//
			this._comboStatus.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this._comboStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboStatus.FormattingEnabled = true;
			this._comboStatus.Location = new System.Drawing.Point(249, 3);
			this._comboStatus.MaxDropDownItems = 16;
			this._comboStatus.Name = "_comboStatus";
			this._comboStatus.Size = new System.Drawing.Size(140, 22);
			this._comboStatus.Sorted = true;
			this._comboStatus.TabIndex = 9;
			//
			// _labelPriority
			//
			this._labelPriority.AutoSize = true;
			this._labelPriority.Dock = System.Windows.Forms.DockStyle.Left;
			this._labelPriority.Location = new System.Drawing.Point(395, 0);
			this._labelPriority.Name = "_labelPriority";
			this._labelPriority.Size = new System.Drawing.Size(53, 28);
			this._labelPriority.TabIndex = 2;
			this._labelPriority.Text = "Priority:";
			this._labelPriority.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _comboPriority
			//
			this._comboPriority.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this._comboPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboPriority.FormattingEnabled = true;
			this._comboPriority.Location = new System.Drawing.Point(454, 3);
			this._comboPriority.MaxDropDownItems = 16;
			this._comboPriority.Name = "_comboPriority";
			this._comboPriority.Size = new System.Drawing.Size(140, 22);
			this._comboPriority.TabIndex = 3;
			//
			// _labelComponent
			//
			this._labelComponent.AutoSize = true;
			this._labelComponent.Dock = System.Windows.Forms.DockStyle.Left;
			this._labelComponent.Location = new System.Drawing.Point(600, 0);
			this._labelComponent.Name = "_labelComponent";
			this._labelComponent.Size = new System.Drawing.Size(78, 28);
			this._labelComponent.TabIndex = 4;
			this._labelComponent.Text = "Component:";
			this._labelComponent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _comboComponent
			//
			this._comboComponent.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this._comboComponent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboComponent.FormattingEnabled = true;
			this._comboComponent.Location = new System.Drawing.Point(684, 3);
			this._comboComponent.MaxDropDownItems = 16;
			this._comboComponent.Name = "_comboComponent";
			this._comboComponent.Size = new System.Drawing.Size(140, 22);
			this._comboComponent.Sorted = true;
			this._comboComponent.TabIndex = 5;
			//
			// _labelBuildNumber
			//
			this._labelBuildNumber.AutoSize = true;
			this._labelBuildNumber.Dock = System.Windows.Forms.DockStyle.Left;
			this._labelBuildNumber.Location = new System.Drawing.Point(830, 0);
			this._labelBuildNumber.Name = "_labelBuildNumber";
			this._labelBuildNumber.Size = new System.Drawing.Size(40, 28);
			this._labelBuildNumber.TabIndex = 6;
			this._labelBuildNumber.Text = "Build:";
			this._labelBuildNumber.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _txtBuildNumber
			//
			this._txtBuildNumber.Location = new System.Drawing.Point(876, 3);
			this._txtBuildNumber.Name = "_txtBuildNumber";
			this._txtBuildNumber.Size = new System.Drawing.Size(55, 21);
			this._txtBuildNumber.TabIndex = 7;
			//
			// _panelHeaderControls
			//
			this._panelHeaderControls.AutoSize = true;
			this._panelHeaderControls.ColumnCount = 2;
			this._panelHeaderControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this._panelHeaderControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this._panelHeaderControls.Controls.Add(this._comboDeveloper, 1, 0);
			this._panelHeaderControls.Controls.Add(this._labelDeveloper, 0, 0);
			this._panelHeaderControls.Controls.Add(this._panelHeaderFlowControls, 0, 1);
			this._panelHeaderControls.Controls.Add(this._labelTitle, 0, 2);
			this._panelHeaderControls.Controls.Add(this._txtTitle, 1, 2);
			this._panelHeaderControls.Dock = System.Windows.Forms.DockStyle.Top;
			this._panelHeaderControls.Location = new System.Drawing.Point(0, 24);
			this._panelHeaderControls.Name = "_panelHeaderControls";
			this._panelHeaderControls.RowCount = 3;
			this._panelHeaderControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this._panelHeaderControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this._panelHeaderControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this._panelHeaderControls.Size = new System.Drawing.Size(1070, 87);
			this._panelHeaderControls.TabIndex = 0;
			//
			// _comboDeveloper
			//
			this._comboDeveloper.Dock = System.Windows.Forms.DockStyle.Fill;
			this._comboDeveloper.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboDeveloper.FormattingEnabled = true;
			this._comboDeveloper.Location = new System.Drawing.Point(80, 3);
			this._comboDeveloper.Name = "_comboDeveloper";
			this._comboDeveloper.Size = new System.Drawing.Size(987, 21);
			this._comboDeveloper.TabIndex = 1;
			//
			// _labelDeveloper
			//
			this._labelDeveloper.AutoSize = true;
			this._labelDeveloper.Dock = System.Windows.Forms.DockStyle.Fill;
			this._labelDeveloper.Location = new System.Drawing.Point(3, 0);
			this._labelDeveloper.Name = "_labelDeveloper";
			this._labelDeveloper.Size = new System.Drawing.Size(71, 27);
			this._labelDeveloper.TabIndex = 0;
			this._labelDeveloper.Text = "Developer:";
			this._labelDeveloper.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _labelTitle
			//
			this._labelTitle.AutoSize = true;
			this._labelTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this._labelTitle.Location = new System.Drawing.Point(3, 60);
			this._labelTitle.Name = "_labelTitle";
			this._labelTitle.Size = new System.Drawing.Size(71, 27);
			this._labelTitle.TabIndex = 4;
			this._labelTitle.Text = "Title:";
			this._labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// _txtTitle
			//
			this._txtTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this._txtTitle.Location = new System.Drawing.Point(80, 63);
			this._txtTitle.Name = "_txtTitle";
			this._txtTitle.Size = new System.Drawing.Size(987, 21);
			this._txtTitle.TabIndex = 4;
			//
			// _panelBrowser
			//
			this._panelBrowser.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this._panelBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panelBrowser.Location = new System.Drawing.Point(0, 111);
			this._panelBrowser.Name = "_panelBrowser";
			this._panelBrowser.Size = new System.Drawing.Size(1070, 303);
			this._panelBrowser.TabIndex = 4;
			//
			// IssueView
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1070, 436);
			this.Controls.Add(this._panelBrowser);
			this.Controls.Add(this._panelHeaderControls);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.menuStrip1);
			this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "IssueView";
			this.Text = "Submit to JIRA";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDown);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this._panelHeaderFlowControls.ResumeLayout(false);
			this._panelHeaderFlowControls.PerformLayout();
			this._panelHeaderControls.ResumeLayout(false);
			this._panelHeaderControls.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.FlowLayoutPanel _panelHeaderFlowControls;
		private System.Windows.Forms.TableLayoutPanel _panelHeaderControls;
		private System.Windows.Forms.Label _labelDeveloper;
		private System.Windows.Forms.Label _labelTitle;
		private System.Windows.Forms.TextBox _txtTitle;
		private System.Windows.Forms.Label _labelIssueType;
		private System.Windows.Forms.Label _labelPriority;
		private System.Windows.Forms.Label _labelBuildNumber;
		private System.Windows.Forms.TextBox _txtBuildNumber;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _menuSubmit;
		private System.Windows.Forms.ToolStripMenuItem _menuClose;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private ResourceComboBox _comboIssueType;
		private ResourceComboBox _comboPriority;
		private System.Windows.Forms.Label _labelComponent;
		private ResourceComboBox _comboComponent;
		private System.Windows.Forms.Panel _panelBrowser;
		private System.Windows.Forms.Label _labelStatus;
		private ResourceComboBox _comboStatus;
		private System.Windows.Forms.ComboBox _comboDeveloper;
	}
}
