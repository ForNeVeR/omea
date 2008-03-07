/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Net;
using System.Xml;
using CookComputing.XmlRpc;

namespace JetBrains.ExceptionReport
{
    /// <summary>
    /// Class for submitting exceptions into JIRA.
    /// </summary>
    public class JiraExceptionSubmitter : IExceptionSubmitter
    {
        public event SubmitProgressEventHandler SubmitProgress;

        private readonly string myJiraProjectKey;
        private readonly string myProduct;

        private bool myNeedProcessEvents = true;
        private string myDefaultUserName;
        private string myDefaultPassword;
        private string myComponent = "Submitted Exceptions";
        private string myStatus = null;

        public string Status
        {
            get { return myStatus; }
            set { myStatus = value; }
        }

        public string Component
        {
            get { return myComponent; }
            set { myComponent = value; }
        }

        public JiraExceptionSubmitter( string jiraProjectKey, string product )
        {
            myJiraProjectKey = jiraProjectKey;
            myProduct = product;
        }

        public bool NeedProcessEvents
        {
            set { myNeedProcessEvents = value; }
        }

        public void SetDefaultLogin( string userName, string password )
        {
            myDefaultUserName = userName;
            myDefaultPassword = password;
        }

        protected void OnSubmitProgress( string message )
        {
            if (SubmitProgress != null)
                SubmitProgress( this, new SubmitProgressEventArgs( message ) );
        }

        public SubmissionResult SubmitException( Exception e, string description, string userName, string password, int buildNumber, IWebProxy proxy )
        {
            return SubmitException( e.ToString(), e.Message, e.StackTrace, description,
                                    userName, password, buildNumber, null, proxy );
        }

        public SubmissionResult SubmitException( string excString, string excMessage, string excStackTrace, string description, string itnUserName, string itnPassword, int buildNumber, string osVersion, IWebProxy proxy )
        {
            string md5Hash = ITNExceptionSubmitter.GetExceptionHash( excString );
            ErrorReportProxy reportProxy = new ErrorReportProxy( proxy, myNeedProcessEvents );

            try
            {
                OnSubmitProgress( "Checking..." );
                int itnThread = -1;
                ExceptionStruct es = new ExceptionStruct();
                bool checkFailed = reportProxy.CheckException( md5Hash, ref es );

                if (!checkFailed)
                    itnThread = es.exceptionItnThread;

                OnSubmitProgress( "Posting..." );
                bool isComment = true;

                string errDescription = (description.Length == 0)
                    ? excString
                    : description + "\n" + excString;

                IJiraService service = (IJiraService) XmlRpcProxyGen.Create( typeof (IJiraService) );

                string token;

                if (myDefaultUserName != null && myDefaultPassword != null)
                {
                    try
                    {
                        token = service.login( itnUserName, itnPassword );
                    }
                    catch (XmlRpcFaultException)
                    {
                        token = service.login( myDefaultUserName, myDefaultPassword );
                        itnUserName = myDefaultUserName;
                        itnPassword = myDefaultPassword;
                    }
                }
                else
                    token = service.login( itnUserName, itnPassword );

                XmlRpcStruct[] statuses = service.getStatuses( token );
                XmlRpcStruct[] resolutions = service.getResolutions( token );

                XmlRpcStruct issue = new XmlRpcStruct();
                if (itnThread == -1)
                {
                    isComment = false;
                    XmlRpcStruct[] components = service.getComponents( token, myJiraProjectKey );
                    XmlRpcStruct[] issueTypes = service.getIssueTypes( token );
                    XmlRpcStruct[] priorities = service.getPriorities( token );

                    issue[ "project" ] = myJiraProjectKey;
                    if (myComponent != null)
                    {
                        foreach( XmlRpcStruct component in components )
                        {
                            if ((string) component[ "name" ] == myComponent)
                                issue[ "components" ] = new XmlRpcStruct[] {component};
                        }
                    }
                    MapNameToId( issue, issueTypes, "type", "Exception" );
                    MapNameToId( issue, priorities, "priority", "Major" );

                    issue[ "summary" ] = ITNExceptionSubmitter.GetExceptionTitle( excMessage, excStackTrace );
                    issue[ "description" ] = errDescription;

                    issue = service.createIssue( token, issue );
                    string issueKey = (string) issue[ "key" ];
                    itnThread = Int32.Parse( issueKey.Substring( issueKey.IndexOf( "-" ) + 1 ) );

                    if (myStatus != null)
                    {
                        foreach( XmlRpcStruct status in statuses )
                        {
                            if ((string) status[ "name" ] == myStatus)
                            {
                                service.setIssueStatus( itnUserName, itnPassword, issueKey, (string) status[ "id" ], "" );
                                break;
                            }
                        }

                        service.setIssueStatus( itnUserName, itnPassword, issueKey, myStatus, "" );
                    }
                }
                else
                {
                    string issueKey = myJiraProjectKey + "-" + itnThread;
                    service.addIssueComment( itnUserName, itnPassword, issueKey,
                                             errDescription );
                    issue = service.getIssue( token, issueKey );
                }

                XmlDocument requestDescription = new XmlDocument();
                requestDescription.LoadXml( "<scr/>" );
                requestDescription.DocumentElement.SetAttribute( "url",
                                                                 "http://www.jetbrains.net/jira/browse/" + issue[ "key" ] );
                requestDescription.DocumentElement.SetAttribute( "state",
                    MapIdToName( issue, statuses, "status" ) );
                requestDescription.DocumentElement.SetAttribute( "resolution",
                    MapIdToName( issue, resolutions, "resolution" ) );
                
                XmlElement fixVersionNodes = requestDescription.CreateElement( "fixVersions" );
                requestDescription.DocumentElement.AppendChild( fixVersionNodes );
                
                object[] fixVersions = (object[]) issue ["fixVersions"];
                foreach( object fixVersionObj in fixVersions )
                {
                    XmlRpcStruct fixVersion = (XmlRpcStruct) fixVersionObj;
                    XmlElement fixVersionNode = requestDescription.CreateElement( "version" );
                    fixVersionNode.SetAttribute( "name",  (string) fixVersion ["name"] );
                    fixVersionNodes.AppendChild( fixVersionNode );
                }

                if (!checkFailed)
                {
                    if (!isComment)
                    {
                        es = new ExceptionStruct();
                        es.exceptionHash = md5Hash;
                        es.exceptionMessage = excString;
                        es.exceptionDate = DateTime.Now;
                        es.exceptionStack = excStackTrace == null ? String.Empty : excStackTrace;
                        es.exceptionItnThread = itnThread;
                        es.exceptionBuildNumber = buildNumber.ToString();
                        es.exceptionProductCode = myProduct;
                        es.exceptionScrambled = false;
                    }

                    OnSubmitProgress( "Submitting..." );
                    reportProxy.SubmitException( itnUserName, es, excString, isComment );
                }
                return new SubmissionResult( itnThread, isComment, requestDescription );
            }
            finally
            {
                OnSubmitProgress( "" );
            }
        }

        private static void MapNameToId( XmlRpcStruct issue, XmlRpcStruct[] items, string idField, string name )
        {
            foreach( XmlRpcStruct item in items )
            {
                if ((string) item[ "name" ] == name )
                {
                    issue[ idField ] = item[ "id" ];
                    break;
                }
            }
        }

        private string MapIdToName( XmlRpcStruct issue, XmlRpcStruct[] items, string idField )
        {
            string id = (string) issue [idField];
            foreach( XmlRpcStruct item in items )
            {
                if ((string) item[ "id" ] == id )
                {
                    return (string) item ["name"];
                }
            }
            return "?";
        }
    }

    [XmlRpcUrl( "http://www.jetbrains.net/jira/rpc/xmlrpc" )]
    public interface IJiraService
    {
        [XmlRpcMethod( "jira1.login" )]
        string login( string userName, string password );

        [XmlRpcMethod( "jira1.getIssueTypes" )]
        XmlRpcStruct[] getIssueTypes( string token );

        [XmlRpcMethod( "jira1.getVersions" )]
        XmlRpcStruct[] getVersions( string token, string projectKey );

        [XmlRpcMethod( "jira1.getComponents" )]
        XmlRpcStruct[] getComponents( string token, string projectKey );

        [XmlRpcMethod( "jira1.getStatuses" )]
        XmlRpcStruct[] getStatuses( string token );

        [XmlRpcMethod( "jira1.getPriorities" )]
        XmlRpcStruct[] getPriorities( string token );

        [XmlRpcMethod( "jira1.getResolutions" )]
        XmlRpcStruct[] getResolutions( string token );

        [XmlRpcMethod( "jira1.createIssue" )]
        XmlRpcStruct createIssue( string token, XmlRpcStruct rIssueStruct );

        [XmlRpcMethod( "jira1.getIssue" )]
        XmlRpcStruct getIssue( string token, string issueKey );

        [XmlRpcMethod( "omeajira1.setIssueStatus" )]
        void setIssueStatus( string userName, string password, string issueKey, string status, string resolution );

        [XmlRpcMethod( "omeajira1.addIssueComment" )]
        void addIssueComment( string userName, string password, string issueKey, string comment );
    }
}