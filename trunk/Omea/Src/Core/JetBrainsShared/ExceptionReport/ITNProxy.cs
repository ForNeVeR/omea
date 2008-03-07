/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;

namespace JetBrains.ExceptionReport
{
  /**
     * Class for submitting exceptions to the ITN tracker through the HTTP POST method.
     */

  public class ITNProxy
  {
	 public static int PostNewThread(string product, string title, string description,
		  string userName, string password, int buildNumber, string osVersion)
	 {
	 	
		 Hashtable paramsHash = new Hashtable();
		 paramsHash ["username"] = userName;
		 paramsHash ["pwd"] = password;
		 paramsHash ["_title"] = title;
		 paramsHash ["_build"] = buildNumber.ToString();
		 paramsHash ["_description"] = description;
		 paramsHash ["addWatch"] = "true";
		 paramsHash ["_jdk"] = "1";
		 paramsHash ["_os"] = (osVersion == null)?GetOSVersionForITN():osVersion;
		 paramsHash ["_visibility"] = "2"; // public
		 paramsHash ["command"] = "createSCR";
		 paramsHash ["_type"] = "1"; // bug

		 WebResponse resp = Post("http://www.intellij.net/trackerRpc/" + product + "/createScr",
			 paramsHash);

		 try
		 {
			 byte[] respData = new byte[4096];
			 int respBytes = resp.GetResponseStream().Read(respData, 0, respData.Length);
			 string respString = Encoding.UTF8.GetString(respData, 0, respBytes);
			 return Convert.ToInt32(respString);
		 }
		 finally
		 {
			 resp.Close();
		 }
	 }

    public static int PostNewThread(string product, string title, string description,
                                    string userName, string password, int buildNumber)
    {
		return PostNewThread(product, title, description, userName, password, buildNumber, GetOSVersionForITN());
    }

    public static void PostNewComment(string product, string description,
                                      string userName, string password, int threadId)
    {
      Hashtable paramsHash = new Hashtable();
      paramsHash ["username"] = userName;
      paramsHash ["pwd"] = password;
      paramsHash ["publicId"] = threadId.ToString();
      paramsHash ["body"] = description;
      paramsHash ["command"] = "Submit";

      WebResponse resp = Post("http://www.intellij.net/trackerRpc/" + product + "/createComment", paramsHash);
      resp.Close();
    }

    private static string GetOSVersionForITN()
    {
      OperatingSystem os = Environment.OSVersion;
      if (os.Platform == PlatformID.Win32Windows)
        return "2"; // Windows 95, 98, Me

      if (os.Version.Major == 5 && os.Version.Minor == 1)
        return "4"; // Windows XP;

      return "3"; // Windows NT, 2000
    }

    private static WebResponse Post(string url, Hashtable paramsHash)
    {
      HttpWebRequest req = (HttpWebRequest) WebRequest.Create(url);
      req.Method = "POST";
      req.ContentType = "application/x-www-form-urlencoded";

      StringBuilder builder = new StringBuilder();
      foreach (DictionaryEntry de in paramsHash)
      {
        builder.Append((string) de.Key);
        builder.Append("=");
        builder.Append(HttpUtility.UrlEncode((string) de.Value));
        builder.Append("&");
      }
      byte[] utf8Bytes = Encoding.UTF8.GetBytes(builder.ToString());
      req.ContentLength = utf8Bytes.Length;
      try
      {
        Stream stream = req.GetRequestStream();
        stream.Write(utf8Bytes, 0, utf8Bytes.Length);
        stream.Close();
      }
      catch (WebException)
      {
      }

      return req.GetResponse();
    }

    public static XmlDocument GetSCR (string productname, int id)
    {
      const string URL = @"http://www.intellij.net/xml/scrToXml.jsp?projectName={0}&publicId={1}";
      WebRequest wr = WebRequest.Create(String.Format(URL, productname, id));
      WebResponse webResponse = wr.GetResponse();
      XmlDocument xmlDocument = new XmlDocument();
      xmlDocument.Load(webResponse.GetResponseStream());
      return xmlDocument;
    }   
  }
}