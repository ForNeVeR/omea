/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using CookComputing.XmlRpc;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.RSSPlugin
{
    [NoObfuscate, XmlRpcUrl("http://www.syndic8.com/xmlrpc.php")]
    class Syndic8: XmlRpcClientProtocol
    {
        [NoObfuscate, XmlRpcMethod("syndic8.FindSites")]
        public int[] FindSites(string url)
        {
            return (int[]) Invoke("FindSites", new object[] { url });
        }

        [NoObfuscate]
        public IAsyncResult BeginFindSites(string url, AsyncCallback callback, object asyncState)
        {
            return BeginInvoke("FindSites", new object[] { url }, this, callback, asyncState);
        }

        [NoObfuscate]
        public int[] EndFindSites(IAsyncResult ar)
        {
            return (int[]) EndInvoke(ar);
        }

        [NoObfuscate, XmlRpcMethod("syndic8.GetFeedInfo")]
        public object[] GetFeedInfo(int[] feedIDs)
        {
            object[] o = (object[]) Invoke("GetFeedInfo", new object[] { feedIDs });
            return o;
        }

        [NoObfuscate]
        public IAsyncResult BeginGetFeedInfo(int[] feedIDs, AsyncCallback callback, object asyncState)
        {
            return BeginInvoke("GetFeedInfo", new object[] { feedIDs }, this, callback, asyncState);
        }

        [NoObfuscate]
        public object[] EndGetFeedInfo(IAsyncResult ar)
        {
            return (object[]) EndInvoke(ar);
        }
    }
}

