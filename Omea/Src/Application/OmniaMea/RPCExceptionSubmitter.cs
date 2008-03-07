/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using CookComputing.XmlRpc;
using JetBrains.ExceptionReport;

namespace JetBrains.Omea
{
	[XmlRpcUrl("http://omea.jetbrains.net/xmlrpc/TrackerProxy.rem")]
	public class TrackerProxy : XmlRpcClientProtocol
	{
		[XmlRpcMethod("tracker.PostException")] 
		public RPCSubmissionResult PostException(string excString, string excMessage, string excStackTrace,
			string description, string itnUserName, string itnPassword, string product, int buildNumber)
		{
			return (RPCSubmissionResult)Invoke("PostException", new object[] {excString, excMessage, excStackTrace, description, itnUserName, itnPassword, product, buildNumber});
		}

		[XmlRpcBegin]
		public IAsyncResult BeginPostException(string excString, string excMessage, string excStackTrace,
			string description, string itnUserName, string itnPassword, string product, int buildNumber,
			AsyncCallback callback, object asyncState)
		{
			string osVersion;
			if(null == excString)
				excString = "";
			if(null == excMessage)
				excMessage = "";
			if(null == excStackTrace)
				excStackTrace = "";
			if(null == description)
				description = "";
			if(null == itnUserName)
				itnUserName = "";
			if(null == itnPassword)
				itnPassword = "";
			if(null == product)
				product = "";

			OperatingSystem os = Environment.OSVersion;
			if (os.Platform == PlatformID.Win32Windows)
			{
				osVersion = "2"; // Windows 95, 98, Me
			}
			else if (os.Version.Major == 5 && os.Version.Minor == 1)
			{
				osVersion = "4"; // Windows XP;
			}
			else
			{
				osVersion = "3"; // Windows NT, 2000
			}
			return BeginInvoke("PostException", new object[] {excString, excMessage, excStackTrace, description, itnUserName, itnPassword, product, buildNumber, osVersion}, this, callback, asyncState);
		}

		[XmlRpcEnd]
		public RPCSubmissionResult EndPostException(IAsyncResult iasr)
		{
			return (RPCSubmissionResult)EndInvoke(iasr);
		}
	}

	internal class RPCExceptionSubmitter : IExceptionSubmitter
	{
		internal RPCExceptionSubmitter()
		{
		}

		public event SubmitProgressEventHandler SubmitProgress;

		protected void OnSubmitProgress(string message)
		{
			if (SubmitProgress != null)
				SubmitProgress(this, new SubmitProgressEventArgs(message));
		}

		public SubmissionResult SubmitException( Exception e, string description, string itnUserName, string itnPassword, int buildNumber, IWebProxy proxy )
		{
			SubmissionResult res = null;
			try
			{
				bool checkFailed = false;
				TrackerProxy submitter = new TrackerProxy();
				submitter.Proxy = proxy;

				OnSubmitProgress("Submitting...");
				XmlRpcAsyncResult ar = (XmlRpcAsyncResult)submitter.BeginPostException( e.ToString(), 
                    e.Message, e.StackTrace, description, itnUserName, itnPassword, "mypal", buildNumber, 
                    null, null);
				int sleepCount = 0;
				while (!ar.IsCompleted)
				{
					Application.DoEvents();
					Thread.Sleep(250);
					sleepCount++;
					if (sleepCount == 60)
					{
						// 15 seconds
						checkFailed = true;
						ar.Abort();
						break;
					}
				}
				if (!checkFailed)
				{
					try
					{
						RPCSubmissionResult rsr = submitter.EndPostException( ar );	
						OnSubmitProgress("Submitted");
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml( rsr.RequestDescription );
						res = new SubmissionResult( rsr.ThreadID, rsr.IsUpdated, doc );
					}
					catch (XmlRpcFaultException ex)
					{
						Trace.WriteLine(ex.FaultString);
						OnSubmitProgress("Failed");
					}
				}
				else
				{
					OnSubmitProgress("Failed");
				}
			}
			catch (XmlRpcFaultException ex)
			{
				Trace.WriteLine(ex.FaultString);
				OnSubmitProgress("Failed");
			}
			return res;
		}
	}
}
