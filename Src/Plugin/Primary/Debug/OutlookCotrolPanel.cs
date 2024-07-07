// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.GUIControls;
using JetBrains.UI.Interop;
using Microsoft.Win32;

namespace JetBrains.Omea.DebugPlugin
{
	/// <summary>
	/// Summary description for OutlookCotrolPanel.
	/// </summary>
	public class OutlookCotrolPanel : DialogBase
	{
        private ComboBoxSettingEditor _windowStyle;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button _startProcess;
        private Process _process;
        private System.Windows.Forms.Button _close;
        private CheckBoxSettingEditor _useShellExecute;
        private IntPtr _mainWnd;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public OutlookCotrolPanel()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            RestoreSettings();
            _close.Enabled = false;
            string[] values = new string[]{ "Minimized", "Hidden" };
            _windowStyle.SetData( values, values );
            _windowStyle.SetSetting( Settings.ProcessWindowStyle );

            _useShellExecute.SetSetting( Settings.UseShellExecuteForOutlook );
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
            this._windowStyle = new JetBrains.Omea.GUIControls.ComboBoxSettingEditor();
            this.label1 = new System.Windows.Forms.Label();
            this._startProcess = new System.Windows.Forms.Button();
            this._close = new System.Windows.Forms.Button();
            this._useShellExecute = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this.SuspendLayout();
            //
            // _windowStyle
            //
            this._windowStyle.Changed = false;
            this._windowStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._windowStyle.Location = new System.Drawing.Point(108, 4);
            this._windowStyle.Name = "_windowStyle";
            this._windowStyle.TabIndex = 0;
            //
            // label1
            //
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Window Style:";
            //
            // _startProcess
            //
            this._startProcess.Location = new System.Drawing.Point(244, 4);
            this._startProcess.Name = "_startProcess";
            this._startProcess.Size = new System.Drawing.Size(88, 23);
            this._startProcess.TabIndex = 2;
            this._startProcess.Text = "Start Process";
            this._startProcess.Click += new System.EventHandler(this.OnStart);
            //
            // _close
            //
            this._close.Location = new System.Drawing.Point(244, 36);
            this._close.Name = "_close";
            this._close.Size = new System.Drawing.Size(88, 23);
            this._close.TabIndex = 3;
            this._close.Text = "Close Process";
            this._close.Click += new System.EventHandler(this.OnClose);
            //
            // _useShellExecute
            //
            this._useShellExecute.Changed = false;
            this._useShellExecute.InvertValue = false;
            this._useShellExecute.Location = new System.Drawing.Point(8, 32);
            this._useShellExecute.Name = "_useShellExecute";
            this._useShellExecute.Size = new System.Drawing.Size(124, 24);
            this._useShellExecute.TabIndex = 4;
            this._useShellExecute.Text = "Use Shell Execute";
            //
            // OutlookCotrolPanel
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(352, 66);
            this.Controls.Add(this._useShellExecute);
            this.Controls.Add(this._close);
            this.Controls.Add(this._startProcess);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._windowStyle);
            this.Name = "OutlookCotrolPanel";
            this.ShowInTaskbar = true;
            this.Text = "OutlookCotrolPanel";
            this.ResumeLayout(false);

        }
		#endregion

        private void OnStart(object sender, System.EventArgs e)
        {
            SettingSaver.Save( Controls );
            Settings.LoadSettings();
            string path = RegUtil.GetValue( Registry.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\OUTLOOK.EXE", "" ) as string;
            if ( path == null )
            {
                path = "Outlook.exe";
            }

            _process = new Process();
            _process.StartInfo.FileName = path;
            if ( Settings.ProcessWindowStyle == "Hidden" )
            {
                _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }
            else
            {
                _process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            }
            _process.StartInfo.UseShellExecute = Settings.UseShellExecuteForOutlook;
            _process.Start();
            _mainWnd = GenericWindow.FindWindow( "rctrl_renwnd32", null );
            int begin = Environment.TickCount;
            Tracer._Trace("Waiting while main window is loaded");
            while ( (int)_mainWnd == 0 && ( Environment.TickCount - begin ) < 3000 )
            {
                _mainWnd = GenericWindow.FindWindow( "rctrl_renwnd32", null );
            }
            Win32Declarations.SendMessage( _mainWnd, Win32Declarations.WM_ACTIVATEAPP, (IntPtr) 1, IntPtr.Zero );
            Win32Declarations.SendMessage( _mainWnd, Win32Declarations.WM_NCACTIVATE, (IntPtr) 0x200001, IntPtr.Zero );
            Win32Declarations.SendMessage( _mainWnd, Win32Declarations.WM_ACTIVATE, (IntPtr) 0x200001, IntPtr.Zero );
            Win32Declarations.SendMessage( _mainWnd, Win32Declarations.WM_ACTIVATETOPLEVEL, (IntPtr) 0x200001, (IntPtr) 0x13FBE8 );
            Win32Declarations.SendMessage( _mainWnd, Win32Declarations.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero );
            _close.Enabled = true;
        }

        private void OnClose(object sender, System.EventArgs e)
        {
            Win32Declarations.SendMessage( _mainWnd, Win32Declarations.WM_CLOSE, IntPtr.Zero, IntPtr.Zero );
            _close.Enabled = false;
        }
	}
}
