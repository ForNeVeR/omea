// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;

using JetBrains.Omea.Jiffa.Res;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Jiffa
{
	public partial class ServerPropertiesSheet : Form
	{
		private readonly JiraServer _server;

		public ServerPropertiesSheet(JiraServer server)
		{
			_server = server;
			InitializeComponent();

			_propsheet.SelectedObject = Server;
			Text = string.Format(Stringtable.JiraServerPropertiesTitle, Server.Name);
		}

		public JiraServer Server
		{
			get
			{
				return _server;
			}
		}

		///<summary>
		///Raises the <see cref="E:System.Windows.Forms.Control.KeyDown"></see> event.
		///</summary>
		///
		///<param name="e">A <see cref="T:System.Windows.Forms.KeyEventArgs"></see> that contains the event data. </param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if(e.KeyData == Keys.Escape)
			{
				Close();
				return;
			}
			base.OnKeyDown(e);
		}

		///<summary>
		///Raises the <see cref="E:System.Windows.Forms.Form.Closed"></see> event.
		///</summary>
		///
		///<param name="e">The <see cref="T:System.EventArgs"></see> that contains the event data. </param>
		protected override void OnClosed(EventArgs e)
		{
			Core.NetworkAP.QueueJob((MethodInvoker)SyncServer);

			base.OnClosed(e);
		}

		protected void SyncServer()
		{
			IStatusWriter statusbar = Core.UIManager.GetStatusWriter(this, StatusPane.Network);
			try
			{
				statusbar.ShowStatus(string.Format(Stringtable.SyncingJiraServer, Server.Name));

				Server.Sync();

				statusbar.ShowStatus(string.Format(Stringtable.SyncingJiraServerSucceeded, Server.Name), 10);
			}
			catch(Exception ex)
			{
				statusbar.ShowStatus(string.Format(Stringtable.SyncingJiraServerFailed, Server.Name), 10);
				Core.UserInterfaceAP.QueueJob((MethodInvoker)delegate { MessageBox.Show(Core.MainWindow, string.Format(Stringtable.SyncingJiraServerFailed, Server.Name) + "\n\n" + ex.Message, Jiffa.Name, MessageBoxButtons.OK, MessageBoxIcon.Error); });
			}
		}
	}
}
