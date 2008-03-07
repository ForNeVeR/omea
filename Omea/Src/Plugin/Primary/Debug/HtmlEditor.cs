/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls.MshtmlBrowser;

namespace Jetbrains.Omea.DebugPlugin
{
	/// <summary>
	/// Summary description for HtmlEditor.
	/// </summary>
	public class HtmlEditor : System.Windows.Forms.Form
	{
		private System.Windows.Forms.ToolBar _toolbar;
		private MshtmlEdit _htmled;
		private System.Windows.Forms.ToolBarButton toolBarButton1;
		private System.Windows.Forms.ToolBarButton toolBarButton2;
		private System.Windows.Forms.ToolBarButton toolBarButton3;
		private System.Windows.Forms.ToolBarButton toolBarButton4;
		private System.Windows.Forms.ToolBarButton toolBarButton5;
		private System.Windows.Forms.ToolBarButton toolBarButton6;
		private System.Windows.Forms.ToolBarButton toolBarButton7;
		private System.Windows.Forms.ToolBarButton toolBarButton8;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public HtmlEditor()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(HtmlEditor));
			this._toolbar = new System.Windows.Forms.ToolBar();
			this._htmled = new JetBrains.Omea.GUIControls.MshtmlBrowser.MshtmlEdit();
			this.toolBarButton1 = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton2 = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton3 = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton4 = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton5 = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton6 = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton7 = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton8 = new System.Windows.Forms.ToolBarButton();
			((System.ComponentModel.ISupportInitialize)(this._htmled)).BeginInit();
			this.SuspendLayout();
			// 
			// _toolbar
			// 
			this._toolbar.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
			this._toolbar.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
																						this.toolBarButton1,
																						this.toolBarButton2,
																						this.toolBarButton3,
																						this.toolBarButton4,
																						this.toolBarButton5,
																						this.toolBarButton6,
																						this.toolBarButton7,
																						this.toolBarButton8});
			this._toolbar.ButtonSize = new System.Drawing.Size(60, 22);
			this._toolbar.Divider = false;
			this._toolbar.DropDownArrows = true;
			this._toolbar.Location = new System.Drawing.Point(0, 0);
			this._toolbar.Name = "_toolbar";
			this._toolbar.ShowToolTips = true;
			this._toolbar.Size = new System.Drawing.Size(532, 26);
			this._toolbar.TabIndex = 0;
			this._toolbar.TextAlign = System.Windows.Forms.ToolBarTextAlign.Right;
			this._toolbar.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.OnToolbarClick);
			// 
			// _htmled
			// 
			this._htmled.Dock = System.Windows.Forms.DockStyle.Fill;
			this._htmled.Enabled = true;
			this._htmled.Html = "﻿<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\r\n<HTML><HEAD>\r\n<M" +
				"ETA http-equiv=Content-Type content=\"text/html; charset=unicode\">\r\n<META content" +
				"=\"MSHTML 6.00.2900.2604\" name=GENERATOR></HEAD>\r\n<BODY></BODY></HTML>\r\n";
			this._htmled.Location = new System.Drawing.Point(0, 26);
			this._htmled.Name = "_htmled";
			this._htmled.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("_htmled.OcxState")));
			this._htmled.Size = new System.Drawing.Size(532, 272);
			this._htmled.TabIndex = 1;
			this._htmled.Text = "undefined";
			// 
			// toolBarButton1
			// 
			this.toolBarButton1.Text = "Color";
			// 
			// toolBarButton2
			// 
			this.toolBarButton2.Tag = "Bold";
			this.toolBarButton2.Text = "B";
			// 
			// toolBarButton3
			// 
			this.toolBarButton3.Tag = "Italic";
			this.toolBarButton3.Text = "I";
			// 
			// toolBarButton4
			// 
			this.toolBarButton4.Tag = "Underline";
			this.toolBarButton4.Text = "U";
			// 
			// toolBarButton5
			// 
			this.toolBarButton5.Tag = "JustifyLeft";
			this.toolBarButton5.Text = "Left";
			// 
			// toolBarButton6
			// 
			this.toolBarButton6.Tag = "JustifyRight";
			this.toolBarButton6.Text = "Right";
			// 
			// toolBarButton7
			// 
			this.toolBarButton7.Tag = "JustifyCenter";
			this.toolBarButton7.Text = "Center";
			// 
			// toolBarButton8
			// 
			this.toolBarButton8.Tag = "JustifyFull";
			this.toolBarButton8.Text = "Justify";
			// 
			// HtmlEditor
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(532, 298);
			this.Controls.Add(this._htmled);
			this.Controls.Add(this._toolbar);
			this.Name = "HtmlEditor";
			this.Text = "HtmlEditor";
			((System.ComponentModel.ISupportInitialize)(this._htmled)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		private void OnToolbarClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e)
		{
			// A string — execute the action
			if(e.Button.Tag.GetType() == typeof(string))
			{
				if(_htmled.CanExecuteCommand(e.Button.Tag.ToString()))
					_htmled.ExecuteCommand(e.Button.Tag.ToString());
			}
		}
	}
}
