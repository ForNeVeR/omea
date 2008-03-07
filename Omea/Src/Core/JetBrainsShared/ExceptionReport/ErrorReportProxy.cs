/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using CookComputing.XmlRpc;

namespace JetBrains.ExceptionReport
{
	/// <summary>
	/// Proxy for the JetBrains error reporter service.
	/// </summary>
	public class ErrorReportProxy
	{
        private ErrorReport _errorReport;
        private bool _needProcessEvents;

		public ErrorReportProxy( IWebProxy proxy, bool needProcessEvents )
		{
            _errorReport = new ErrorReport();
            _errorReport.Proxy = proxy;
            _errorReport.Proxy.Credentials = CredentialCache.DefaultCredentials;

            _needProcessEvents = needProcessEvents;
		}

        internal bool CheckException( string md5Hash, ref ExceptionStruct es )
        {
            bool checkFailed = false;
            try
            {
                
                XmlRpcAsyncResult checkAR = (XmlRpcAsyncResult) _errorReport.BegincheckException(md5Hash, null, null);
                int sleepCount = 0;
                while (!checkAR.IsCompleted)
                {
                    if(_needProcessEvents)
                    {
                        Application.DoEvents();
                    }
                    Thread.Sleep(250);
                    sleepCount++;
                    if (sleepCount == 60)
                    {
                        // 15 seconds
                        checkFailed = true;
                        checkAR.Abort();
                        break;
                    }
                }

                if (!checkFailed)
                {
                    es = _errorReport.EndcheckException(checkAR);
                }
            }
            catch (XmlRpcFaultException ex)
            {
                Trace.WriteLine(ex.FaultString);
                es.exceptionItnThread = -1;
            }
            return checkFailed;
        }

        internal void SubmitException( string itnUserName, ExceptionStruct es, string excString, bool isComment )
        {
            IAsyncResult ar = _errorReport.Beginauthorize("eap", itnUserName, null, null);
            while (!ar.IsCompleted)
            {
                if(_needProcessEvents)
                {
                    Application.DoEvents();
                }
            }
            string notifierID = _errorReport.Endauthorize(ar);

            ErrorStruct errs = new ErrorStruct();
            errs.errorDate = DateTime.Now;
            errs.errorDescription = excString;
            errs.errorNotifierId = notifierID;
            errs.errorOs = Environment.OSVersion.ToString();
            errs.errorAction = "";

            ar = _errorReport.BeginpostError(errs, es, isComment, null, null);
            while (!ar.IsCompleted)
            {
                if(_needProcessEvents)
                {
                    Application.DoEvents();
                }
            }
                    
            try
            {
                _errorReport.EndpostError(ar);
            }
            catch (XmlRpcFaultException ex)
            {
                if(_needProcessEvents)
                {
                    MessageBox.Show("Error submitting exception data.\n" + ex.FaultString, "Report Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
	
        public struct ErrorStruct
        {
            [XmlRpcMember("error.notifier.id")] public string errorNotifierId;
            [XmlRpcMember("error.date")] public DateTime errorDate;
            [XmlRpcMember("error.os")] public string errorOs;
            [XmlRpcMember("error.action")] public string errorAction;
            [XmlRpcMember("error.description")] public string errorDescription;
        }

        [XmlRpcUrl("http://www.intellij.net/websupport/error/report?sender=i")]
            public class ErrorReport : XmlRpcClientProtocol
        {
            [XmlRpcMethod("error.checkException")]
            public ExceptionStruct checkException(string md5Hash)
            {
                return (ExceptionStruct) Invoke("checkException", new object[] {md5Hash});
            }


            public IAsyncResult BegincheckException(string md5Hash, AsyncCallback callback, object asyncState)
            {
                return BeginInvoke("checkException", new object[] {md5Hash}, this, callback, asyncState);
            }


            public ExceptionStruct EndcheckException(IAsyncResult ar)
            {
                return (ExceptionStruct) EndInvoke(ar);
            }


            [XmlRpcMethod("error.authorize")]
            public string authorize(string method, string loginName)
            {
                return (string) Invoke("authorize", new object[] {method, loginName});
            }


            public IAsyncResult Beginauthorize(string method, string loginName,
                AsyncCallback callback, object asyncState)
            {
                return BeginInvoke("authorize", new object[] {method, loginName}, this, callback, asyncState);
            }


            public string Endauthorize(IAsyncResult ar)
            {
                return (string) EndInvoke(ar);
            }


            [XmlRpcMethod("error.postError")]
            public void postError(ErrorStruct errStruct, ExceptionStruct excStruct, bool comment)
            {
                Invoke("postError", new object[] {errStruct, excStruct, comment});
            }


            public IAsyncResult BeginpostError(ErrorStruct errStruct, ExceptionStruct excStruct, bool comment,
                AsyncCallback callback, object asyncState)
            {
                return BeginInvoke("postError", new object[] {errStruct, excStruct, comment},
                    this, callback, asyncState);
            }


            public void EndpostError(IAsyncResult ar)
            {
                EndInvoke(ar);
            }
        }

    }

    public struct ExceptionStruct
    {
        //[XmlRpcMember("exception.cause")]      string exceptionCause;
        [XmlRpcMember("exception.hash.code")] public string exceptionHash;
        [XmlRpcMember("exception.message")] public string exceptionMessage;
        [XmlRpcMember("exception.date")] public DateTime exceptionDate;
        [XmlRpcMember("exception.stack")] public string exceptionStack;
        [XmlRpcMember("exception.itn.thread")] public int exceptionItnThread;
        [XmlRpcMember("exception.build.number")] public string exceptionBuildNumber;
        [XmlRpcMember("exception.product.code")] public string exceptionProductCode;
        [XmlRpcMember("exception.scrambled")] public bool exceptionScrambled;
    }
}
