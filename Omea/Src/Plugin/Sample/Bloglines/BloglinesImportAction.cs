// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.BloglinesPlugin
{
	/// <summary>
	/// Summary description for BloglinesImportAction.
	/// </summary>
	public class BloglinesImportAction : IAction
	{
		internal delegate void ImportJob(string authInfo, bool preview);
		internal delegate void ErrorReportJob(string messaghe);

		public BloglinesImportAction()
		{
		}

		public void Execute( IActionContext context )
		{
			ImportBloglinesSubscription frm = null;

			string login  = Core.SettingStore.ReadString(BloglinesPlugin.ConfigSection,BloglinesPlugin.ConfigKeyLogin);
			string passwd = Core.SettingStore.ReadString(BloglinesPlugin.ConfigSection,BloglinesPlugin.ConfigKeyPassword);
			frm = new ImportBloglinesSubscription( login, passwd, false );
			DialogResult res = frm.ShowDialog();
			if ( DialogResult.OK != res )
			{
				return;
			}
			login = frm.Login;
			passwd = frm.Password;
			Core.SettingStore.WriteString(BloglinesPlugin.ConfigSection,BloglinesPlugin.ConfigKeyLogin,login);
			Core.SettingStore.WriteString(BloglinesPlugin.ConfigSection,BloglinesPlugin.ConfigKeyPassword,passwd);

			string authInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(login + ":" + passwd));
			Core.NetworkAP.QueueJob( new ImportJob(ImportOMPL), new object[] { authInfo, frm.NeedPreview } );
		}

		public void Update( IActionContext context, ref ActionPresentation presentation )
		{
			presentation.Visible = true;
		}

		internal void ImportOMPL(string authInfo, bool preview)
		{
			// Try to import feed
			WebClient client = new WebClient();
			client.Headers.Add("Authorization", "basic " + authInfo);
			try
			{
				Stream stream = client.OpenRead(BloglinesPlugin.ImportURL);
				BloglinesPlugin.RSSService.ImportOpmlStream(stream, null, BloglinesPlugin.ImportName, preview);
			}
			catch(WebException e)
			{
				if(e.Status == WebExceptionStatus.ProtocolError)
				{
					Core.UIManager.QueueUIJob( new ErrorReportJob(ReportError), new object[] { "Authorization failed.\nPlease, check your login and password." });
				}
				else
				{
					Core.UIManager.QueueUIJob( new ErrorReportJob(ReportError), new object[] { "Network problems.\nPlease, try later." });
				}
			}
			catch
			{
				// Which message will be Ok?
				Core.UIManager.QueueUIJob( new ErrorReportJob(ReportError), new object[] { "Data format error.\nPlease, report to bloglines.com." });
			}
		}

		internal void ReportError(string message)
		{
			MessageBox.Show(message,BloglinesPlugin.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}
