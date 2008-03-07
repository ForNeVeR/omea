/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace JetBrains.ExceptionReport
{
  public struct RPCSubmissionResult
  {
    public int ThreadID;
    public bool IsUpdated;
    public string RequestDescription;
  }

  public class SubmissionResult
  {
    private int myThreadID;
    private bool myIsUpdated;
    private XmlDocument myRequestDescription;

    public SubmissionResult(int threadId, bool isUpdated, XmlDocument requestDescription)
    {
      myThreadID = threadId;
      myIsUpdated = isUpdated;
      myRequestDescription = requestDescription;
    }


    public int ThreadId
    {
      get { return myThreadID; }
    }

    public bool IsUpdated
    {
      get { return myIsUpdated; }
    }

    public XmlDocument RequestDescription
    {
      get { return myRequestDescription; }
    }
  }

  public interface IExceptionSubmitter
  {
    event SubmitProgressEventHandler SubmitProgress;

    SubmissionResult SubmitException(Exception e, string description, string userName, string password, int buildNumber, IWebProxy proxy);
  }

  /// <summary>
  /// Utility class for submitting exceptions to the ITN tracker.
  /// </summary>
  public class ITNExceptionSubmitter: IExceptionSubmitter
  {
    private readonly string myITNProjectName;
    private readonly string myProduct;
    private bool myNeedProcessEvents;

    public ITNExceptionSubmitter(string itnProjectName, string product)
    {
      myITNProjectName = itnProjectName;
      myProduct = product;
      myNeedProcessEvents = true;
    }

    public bool NeedProcessEvents { set { myNeedProcessEvents = value; } }

    public event SubmitProgressEventHandler SubmitProgress;

    protected void OnSubmitProgress(string message)
    {
      if (SubmitProgress != null)
        SubmitProgress(this, new SubmitProgressEventArgs(message));
    }


    public SubmissionResult SubmitException(Exception e, string description, string userName, string password, int buildNumber, IWebProxy proxy)
    {
      return SubmitException( e.ToString(), e.Message, e.StackTrace, description,
                              userName, password, buildNumber, null, proxy );
    }

    public SubmissionResult SubmitException(string excString, string excMessage, string excStackTrace, string description, string itnUserName, string itnPassword, int buildNumber, string osVersion, IWebProxy proxy)
    {
        string md5Hash = GetExceptionHash( excString );
        ErrorReportProxy reportProxy = new ErrorReportProxy( proxy, myNeedProcessEvents );

        XmlDocument requestDescription = null;

        try
        {
            OnSubmitProgress("Checking...");
            int itnThread = -1;
            ExceptionStruct es = new ExceptionStruct();
            bool checkFailed = reportProxy.CheckException( md5Hash, ref es );
             
            if ( !checkFailed )
            {
                itnThread = es.exceptionItnThread;
            }

            OnSubmitProgress("Posting...");
            bool isComment = true;

            string errDescription = (description.Length == 0)
                ? excString
                : description + "\n" + excString;

            if (itnThread == -1)
            {
                isComment = false;
                itnThread = ITNProxy.PostNewThread(myProduct, GetExceptionTitle(excMessage, excStackTrace), 
                    errDescription, itnUserName, itnPassword, buildNumber, osVersion);
            }
            else
            {
                ITNProxy.PostNewComment(myProduct, errDescription, itnUserName, itnPassword, itnThread);
            }

            if (myITNProjectName != String.Empty)
            {
                requestDescription = ITNProxy.GetSCR(myITNProjectName, itnThread);
            }

            if (!checkFailed)
            {
                if (!isComment)
                {
                    es = new ExceptionStruct();
                    es.exceptionHash = md5Hash;
                    es.exceptionMessage = excMessage;
                    es.exceptionDate = DateTime.Now;
                    es.exceptionStack = excStackTrace == null ? String.Empty : excStackTrace;
                    es.exceptionItnThread = itnThread;
                    es.exceptionBuildNumber = buildNumber.ToString();
                    es.exceptionProductCode = myProduct;
                    es.exceptionScrambled = false;
                }

                OnSubmitProgress("Submitting...");
                reportProxy.SubmitException( itnUserName, es, excString, isComment );
                return new SubmissionResult(itnThread, isComment, requestDescription);
            }
            else
                return null;
        }
        finally
        {
            OnSubmitProgress("");
        }
    }

    internal static string FilterExceptionString(string s)
    {
      Regex rx = new Regex(@"([^\s]+\\)?([^\s\\]+)(:.+)?$", RegexOptions.Multiline);
      return rx.Replace(s, "$2").ToLower();
    }

    internal static string GetExceptionHash( string excString )
    {
      string filteredString = FilterExceptionString(excString);
      byte[] excBytes = Encoding.Default.GetBytes(filteredString);
      return Convert.ToBase64String(new MD5CryptoServiceProvider().ComputeHash(excBytes));
    }

    internal static string GetExceptionTitle(string excMessage, string excStackTrace)
    {
      string msg = excMessage;
      string[] stackTrace = (excStackTrace == null) ? new string[0] : excStackTrace.Split('\n');
      foreach (string line in stackTrace)
      {
        int pos = line.IndexOf('(');
        if (pos >= 0)
        {
          int methodNameStart = line.LastIndexOf(' ', pos);
          if (methodNameStart < 0)
            methodNameStart = 0;
          if (line.Substring(methodNameStart + 1).StartsWith("System."))
            continue;
          int lastDot = line.LastIndexOf('.', pos); // start of method name
          if (lastDot >= 0)
            // start of class name
            lastDot = line.LastIndexOf('.', lastDot - 1);
          if (lastDot >= 0)
          {
            msg = msg + " (" + line.Substring(lastDot + 1, pos - lastDot - 1) + ")";
            break;
          }
        }
      }
      return msg;
    }

  }

  public class SubmitProgressEventArgs : EventArgs
  {
    private string _message;


    public SubmitProgressEventArgs(string message)
    {
      _message = message;
    }


    public string Message
    {
      get { return _message; }
    }
  }

  public delegate void SubmitProgressEventHandler(object sender, SubmitProgressEventArgs e);
}