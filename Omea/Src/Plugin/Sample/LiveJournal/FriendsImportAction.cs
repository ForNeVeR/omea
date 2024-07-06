// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.LiveJournalPlugin
{
    /// <summary>
    /// Summary description for FriendsImportAction.
    /// </summary>
    public class FriendsImportAction : SimpleAction
    {
        private const string _protocolURL = "http://www.livejournal.com/interface/flat";
        private const string _contentType = "application/x-www-form-urlencoded";
        private const string _urlTemplate = "http://www.livejournal.com/users/{0}/data/rss?auth=digest";

        private const string _groupNameTemplate = "LiveJournal friends of {1}";
        private const string _feedNameTemplate  = "{1} ({0})";
        private const string _feedDescTemplate  = "Posts of LeveJournal user '{0}'";

        private static char[] _hexChars;

        private delegate void ImportJob( string UserName, string Password, int UpdateFreq, string UpdatePeriod );
        private delegate void StatusReportJob( string message, MessageBoxIcon icon );

        public FriendsImportAction()
        {
            _hexChars = new char[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};
        }

        public override void Execute( IActionContext context )
        {
            FriendsImportForm frm = null;

            string login   = Core.SettingStore.ReadString(LiveJournalPlugin.ConfigSection,LiveJournalPlugin.ConfigKeyUsername);
            string passwd  = Core.SettingStore.ReadString(LiveJournalPlugin.ConfigSection,LiveJournalPlugin.ConfigKeyPassword);
            int updateFreq = Core.SettingStore.ReadInt( "RSS" , "UpdateFrequency", -1);
            string updatePeriod = Core.SettingStore.ReadString( "RSS" , "UpdatePeriod", "hourly");
            frm = new FriendsImportForm( login, passwd, updateFreq, LiveJournalPlugin.UpdatePeriodToIndex(updatePeriod) );
            DialogResult res = frm.ShowDialog();
            if ( DialogResult.OK != res )
            {
                return;
            }
            login = frm.Login;
            passwd = frm.Password;
            updateFreq = frm.UpdateFreq;
            updatePeriod = LiveJournalPlugin.UpdateIndexToPeriod( frm.UpdatePeriod );
            Core.SettingStore.WriteString(LiveJournalPlugin.ConfigSection,LiveJournalPlugin.ConfigKeyUsername,login);
            Core.SettingStore.WriteString(LiveJournalPlugin.ConfigSection,LiveJournalPlugin.ConfigKeyPassword,passwd);
            Core.NetworkAP.QueueJob( new ImportJob(ImportFreinds), new object[] { login, passwd, updateFreq, updatePeriod } );
        }

        private void ImportFreinds( string UserName, string Password, int UpdateFreq, string UpdatePeriod )
        {
            Hashtable answer;
            Hashtable friends = new Hashtable();
            string[] chrsp;
            string fullName = "";
            try
            {
                chrsp = ChallengeResponse(Password);
                answer = FlatRequest("login",
                    "user", UserName,
                    "auth_method",    "challenge",
                    "auth_challenge", chrsp[0],
                    "auth_response",  chrsp[1]);
                fullName = answer.ContainsKey( "name" ) ? answer["name"] as string : UserName;

                chrsp = ChallengeResponse(Password);
                answer = FlatRequest("getfriends",
                    "user", UserName,
                    "auth_method",    "challenge",
                    "auth_challenge", chrsp[0],
                    "auth_response",  chrsp[1]);
                if(!answer.ContainsKey("friend_count"))
                    throw new Exception("Answer format error: no number of friends provided");
                int friendsCount = 0;
                try
                {
                    friendsCount = Int32.Parse( answer["friend_count"] as string );
                }
                catch
                {
                    throw new Exception("Answer format error: number of friends is not numeric");
                }
                for(int i = 1; i <= friendsCount; ++i)
                {
                    string key;
                    string friend;

                    key = String.Format( "friend_{0}_user", i );
                    if(!answer.ContainsKey(key))
                        throw new Exception( String.Format( "Answer format error: no friend {0} provided", i ) );
                    friend =  answer[key] as string;

                    key = String.Format( "friend_{0}_name", i );
                    if(!answer.ContainsKey(key))
                        throw new Exception( String.Format( "Answer format error: no name for friend {0} provided", i ) );
                    friends.Add( friend,  answer[key] as string );
                }
            }
            catch(Exception ex)
            {
                Core.UIManager.QueueUIJob( new StatusReportJob(ReportStatus), new object[] { "Protocol error:\n" + ex.Message, MessageBoxIcon.Error });
                return;
            }

            // Make group
            fullName = String.Format( _groupNameTemplate, UserName, fullName );
            IResource feedGroup = Core.ResourceStore.FindUniqueResource( "RSSFeedGroup", "Name", fullName );
            if( null == feedGroup )
            {
                IResource parentGroup = null;
                parentGroup = Core.ResourceStore.FindUniqueResource( "ResourceTreeRoot", "RootResourceType", "RSSFeed" );

                ResourceProxy proxy = ResourceProxy.BeginNewResource( "RSSFeedGroup" );
                try
                {
                    proxy.SetProp( "Name", fullName );
                    if( null != parentGroup )
                    {
                        proxy.SetProp( "Parent", parentGroup );
                    }
                }
                finally
                {
                    proxy.EndUpdate();
                    feedGroup = proxy.Resource;
                }
                if(null == feedGroup)
                {
                    Core.UIManager.QueueUIJob( new StatusReportJob(ReportStatus), new object[] { "Can not add group for new feeds", MessageBoxIcon.Error });
                    return;
                }
            }
            // Ok, friends list is populated
			bool added = false;
            foreach( string friend in friends.Keys )
            {
                string URL = String.Format( _urlTemplate, friend );
                IResource feed = null;

                feed = Core.ResourceStore.FindUniqueResource( "RSSFeed", "URL", URL );
                if( null != feed )
                    continue;

                ResourceProxy proxy = ResourceProxy.BeginNewResource( "RSSFeed" );
                try
                {
                    proxy.SetProp( "Name",            String.Format( _feedNameTemplate, friend, friends[friend] as string ));
                    proxy.SetProp( "Description",     String.Format( _feedDescTemplate, friend, friends[friend] as string ));
                    proxy.SetProp( "URL",             URL );
                    proxy.SetProp( "Parent",          feedGroup );
                    proxy.SetProp( "HttpUserName",    UserName );
                    proxy.SetProp( "HttpPassword",    Password );
                    proxy.SetProp( "UpdateFrequency", UpdateFreq );
                    proxy.SetProp( "UpdatePeriod",    UpdatePeriod );
                }
                finally
                {
                    proxy.EndUpdate();
                    feed = proxy.Resource;
                }
                if(null != feed)
                {
					added = true;
					LiveJournalPlugin.RSSService.QueueFeedUpdate( feed );
                }
            }
			if(added)
			{
				Core.WorkspaceManager.AddToActiveWorkspace( feedGroup );
			}
			else
			{
				Core.UIManager.QueueUIJob( new StatusReportJob(ReportStatus), new object[] { "No new friends were added", MessageBoxIcon.Information });
			}
		}

        private string md5_hex(string s)
        {
            string res = "";
            foreach( byte b in MD5.Create().ComputeHash( Encoding.UTF8.GetBytes( s ) ) )
            {
                res += _hexChars[ ( b >> 4 ) & 0x0f ];
                res += _hexChars[ ( b      ) & 0x0f ];
            }
            return res;
        }

        private Hashtable FlatRequest(string mode, params string[] parameters)
        {
            HttpWebRequest  req = null;
            HttpWebResponse rsp = null;
            string content;
            int i;

            content = "mode=" + mode;
            for(i = 0; i < parameters.Length - 1; i += 2)
            {
                content += "&" +
                    HttpUtility.UrlEncode(parameters[i]) +
                    "=" +
                    HttpUtility.UrlEncode(parameters[i + 1]);
            }
            if(i < parameters.Length)
                throw new ArgumentException("Invalid number of parameters","parameters");
            content += "&ver=1";
            // Make request
            byte[] reqb = Encoding.UTF8.GetBytes( content );

            // Get response
            try
            {
                req = WebRequest.Create(_protocolURL) as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = _contentType;
                req.ContentLength = content.Length;
                req.SendChunked = false;
                Stream reqs = req.GetRequestStream();
                reqs.Write(reqb , 0,  reqb.Length);
                reqs.Close();
                rsp = req.GetResponse() as HttpWebResponse;
            }
            catch(Exception ex)
            {
                throw new Exception("HTTP request failed: " + ex.Message);
            }
            if(rsp.StatusCode != HttpStatusCode.OK)
                throw new Exception("HTTP request failed: " + rsp.StatusDescription);
            // Parse response
            TextReader rsptr = new StreamReader(rsp.GetResponseStream(), Encoding.UTF8);
            Hashtable answer = new Hashtable();
            while(true)
            {
                string var = null;
                string val = null;
                try
                {
                    var = rsptr.ReadLine();
                    if(var == null)
                    {
                        // End of loop
                        break;
                    }
                    val = rsptr.ReadLine();
                }
                catch(Exception ex)
                {
                    throw new Exception("Response error: " + ex.Message);
                }
                if(var == null || val == null)
                    throw new Exception("Response error: No var-val pair");
                answer.Add( var, val );
            }
            if(!answer.ContainsKey("success"))
                throw new Exception("Answer format error");
            if(answer["success"] as string != "OK")
                throw new Exception(
                    answer.ContainsKey("errmsg") ? answer["errmsg"] as string : "Unknown error"
                    );
            return answer;
        }

        private string[] ChallengeResponse( string Password )
        {
            Hashtable answer = FlatRequest("getchallenge");
            // check answer
            if(!answer.ContainsKey("challenge"))
                throw new Exception("Answer format error: no challenge provided");
            // Extract challenge
            string challenge = answer["challenge"] as string;
            // Prepare response
            string response = md5_hex( challenge + md5_hex( Password ) );
            return new string[] { challenge, response };
        }

        private void ReportStatus(string message, MessageBoxIcon icon)
        {
            MessageBox.Show(message,LiveJournalPlugin.Name, MessageBoxButtons.OK, icon);
        }
    }
}
