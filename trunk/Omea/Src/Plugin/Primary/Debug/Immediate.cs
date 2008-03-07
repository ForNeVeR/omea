/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.JScript;
using Microsoft.JScript.Vsa;

namespace JetBrains.Omea.DebugPlugin
{
	/// <summary>
	/// Summary description for Immediate.
	/// </summary>
	public class Immediate : System.Windows.Forms.Form
	{
		private System.Windows.Forms.MenuItem _miBack;
		private System.Windows.Forms.MainMenu _menuMain;
		private System.Windows.Forms.MenuItem _miForward;
		private System.Windows.Forms.MenuItem _miExecute;
		private JetBrains.Omea.GUIControls.MshtmlBrowser.MshtmlEdit _htmled;
		private System.Windows.Forms.Panel _panelInput;
		private JetBrains.Omea.GUIControls.MshtmlBrowser.MshtmlBrowserControl _htmlvw;
		private System.Windows.Forms.Splitter _splitterInput;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Immediate()
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(Immediate));
			this._menuMain = new System.Windows.Forms.MainMenu();
			this._miBack = new System.Windows.Forms.MenuItem();
			this._miForward = new System.Windows.Forms.MenuItem();
			this._miExecute = new System.Windows.Forms.MenuItem();
			this._htmled = new JetBrains.Omea.GUIControls.MshtmlBrowser.MshtmlEdit();
			this._panelInput = new System.Windows.Forms.Panel();
			this._htmlvw = new JetBrains.Omea.GUIControls.MshtmlBrowser.MshtmlBrowserControl();
			this._splitterInput = new System.Windows.Forms.Splitter();
			((System.ComponentModel.ISupportInitialize)(this._htmled)).BeginInit();
			this._panelInput.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._htmlvw)).BeginInit();
			this.SuspendLayout();
			// 
			// _menuMain
			// 
			this._menuMain.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this._miBack,
																					  this._miForward,
																					  this._miExecute});
			// 
			// _miBack
			// 
			this._miBack.Index = 0;
			this._miBack.ShowShortcut = false;
			this._miBack.Text = "&Back";
			this._miBack.Click += new System.EventHandler(this.OnBack);
			// 
			// _miForward
			// 
			this._miForward.Index = 1;
			this._miForward.Text = "&Forward";
			this._miForward.Click += new System.EventHandler(this.OnForward);
			// 
			// _miExecute
			// 
			this._miExecute.Index = 2;
			this._miExecute.Text = "&Execute";
			this._miExecute.Click += new System.EventHandler(this.OnExecute);
			// 
			// _htmled
			// 
			this._htmled.ContainingControl = this;
			this._htmled.Dock = System.Windows.Forms.DockStyle.Fill;
			this._htmled.Enabled = true;
			this._htmled.Html = "﻿<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\r\n<HTML><HEAD>\r\n<M" +
				"ETA http-equiv=Content-Type content=\"text/html; charset=unicode\">\r\n<META content" +
				"=\"MSHTML 6.00.2900.2604\" name=GENERATOR></HEAD>\r\n<BODY></BODY></HTML>\r\n";
			this._htmled.Location = new System.Drawing.Point(0, 0);
			this._htmled.Name = "_htmled";
			this._htmled.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("_htmled.OcxState")));
			this._htmled.Size = new System.Drawing.Size(624, 204);
			this._htmled.TabIndex = 0;
			this._htmled.Text = "undefined";
			this._htmled.BorderStyle = BorderStyle.Fixed3D;
			// 
			// _panelInput
			// 
			this._panelInput.Controls.Add(this._htmled);
			this._panelInput.Dock = System.Windows.Forms.DockStyle.Bottom;
			this._panelInput.Location = new System.Drawing.Point(0, 217);
			this._panelInput.Name = "_panelInput";
			this._panelInput.Size = new System.Drawing.Size(624, 204);
			this._panelInput.TabIndex = 1;
			// 
			// _htmlvw
			// 
			this._htmlvw.ContextProvider = null;
			this._htmlvw.CurrentUrl = "";
			this._htmlvw.Dock = System.Windows.Forms.DockStyle.Fill;
			this._htmlvw.Enabled = true;
			this._htmlvw.Html = "﻿<HTML></HTML>";
			this._htmlvw.Location = new System.Drawing.Point(0, 0);
			this._htmlvw.Name = "_htmlvw";
			this._htmlvw.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("_htmlvw.OcxState")));
			this._htmlvw.ShowImages = true;
			this._htmlvw.Size = new System.Drawing.Size(624, 214);
			this._htmlvw.TabIndex = 0;
			this._htmlvw.BorderStyle = BorderStyle.Fixed3D;
			// 
			// _splitterInput
			// 
			this._splitterInput.Dock = System.Windows.Forms.DockStyle.Bottom;
			this._splitterInput.Location = new System.Drawing.Point(0, 214);
			this._splitterInput.Name = "_splitterInput";
			this._splitterInput.Size = new System.Drawing.Size(624, 3);
			this._splitterInput.TabIndex = 2;
			this._splitterInput.TabStop = false;
			// 
			// Immediate
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(624, 421);
			this.Controls.Add(this._htmlvw);
			this.Controls.Add(this._splitterInput);
			this.Controls.Add(this._panelInput);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Menu = this._menuMain;
			this.Name = "Immediate";
			this.Text = "Immediate";
			((System.ComponentModel.ISupportInitialize)(this._htmled)).EndInit();
			this._panelInput.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._htmlvw)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		private void OnBack(object sender, System.EventArgs e)
		{
		
		}

		private void OnForward(object sender, System.EventArgs e)
		{
		
		}

		private void OnExecute(object sender, System.EventArgs e)
		{
			Execute();
		}

		private object Execute()
		{
			string	code = _htmled.Text;
			string	sOutput;
			object	result = null;

			try
			{
				VsaEngine engine1 = VsaEngine.CreateEngine();
				result = Eval.JScriptEvaluate(code, true, engine1).ToString();

				sOutput = "<html><body><pre>" + result.ToString() + "</pre></body></html>";
			}
			catch(Exception ex)
			{
				sOutput = "<html><body><pre style=\"background-color: silver; color: red;\">" + ex.ToString() + "</pre></body></html>";
			}

			_htmlvw.Html = sOutput;

			return result;
		}
	}
}
