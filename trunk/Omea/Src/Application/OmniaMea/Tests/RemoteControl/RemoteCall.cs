/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.IO;
using System.Net;
using System.Text;
using JetBrains.Omea.RemoteControl;
using NUnit.Framework;

namespace RemoteControl.Tests.RemoteCalls
{

    public class Base : RemoteControl.Tests.Base
    {
		protected HttpWebResponse _rsp = null;

		[SetUp]
		public new void SetUp()
		{
			base.SetUp();
			_rsp = null;
		}
		[TearDown]
		public new void TearDown()
		{
			base.TearDown();
			if( null != _rsp )
			{
				try
				{
					_rsp.Close();
				}
				catch {}
			}
		}

		
		protected HttpWebResponse GetPOSTRepsonse(string method, string content)
        {
            return GetPOSTRepsonse(method, content, _contentType);
        }

        protected HttpWebResponse GetPOSTRepsonse(string method, string content, string contentType)
        {
            HttpWebRequest  req = null;
            HttpWebResponse rsp = null;
            
            byte[] reqb = Encoding.UTF8.GetBytes( content );
            req = WebRequest.Create(RootURL + RemoteControlServer.protectionKey + "/xml/" + method) as HttpWebRequest;
            req.Method = "POST";
            req.ContentType = contentType;
            req.ContentLength = content.Length;
            req.SendChunked = false;
            Stream reqs = req.GetRequestStream();
            reqs.Write(reqb , 0,  reqb.Length);
            reqs.Close();
            rsp = req.GetResponse() as HttpWebResponse;
            return rsp;
        }
    }
}
