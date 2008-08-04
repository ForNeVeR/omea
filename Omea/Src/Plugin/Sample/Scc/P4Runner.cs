/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
    /// <summary>
    /// Describes a single Perforce changeset.
    /// </summary>
    internal class ChangeSetSummary
    {
        private int _number;
        private string _user;
        private string _client;
        private DateTime _date;

        public ChangeSetSummary( int number, string user, string client, DateTime date )
        {
            _number = number;
            _user = user;
            _client = client;
            _date = date;
        }

        public int Number
        {
            get { return _number; }
        }

        public string User
        {
            get { return _user; }
        }

        public string Client
        {
            get { return _client; }
        }

        public DateTime Date
        {
            get { return _date; }
        }
    }

    internal class FileChangeData
    {
        private string _path;
        private int _revision;
        private string _changeType;
        private bool _binary = false;
        private StringBuilder _diffBuilder = new StringBuilder();

        public FileChangeData( string path, int revision, string changeType )
        {
            _path = path;
            _revision = revision;
            _changeType = changeType;
            _diffBuilder = new StringBuilder();
        }

        public void AppendDiff( string line )
        {
            _diffBuilder.Append( line );
        }

        public string Path
        {
            get { return _path; }
        }

        public int Revision
        {
            get { return _revision; }
        }

        public string ChangeType
        {
            get { return _changeType; }
        }

        public bool Binary
        {
            get { return _binary; }
            set { _binary = value; }
        }

        public string Diff
        {
            get { return _diffBuilder.ToString(); }
        }
    }
    
    internal class ChangeSetDetails
    {
        public ChangeSetDetails( string description, FileChangeData[] fileChanges )
        {
            Description = description;
            FileChanges = fileChanges;
        }

        public string Description { get; set; }
        public FileChangeData[] FileChanges { get; set; }
    }
    
    /// <summary>
    /// Base class for running executables and catching their console output.
    /// </summary>
    internal class RunnerBase
    {
        protected string ReadStdout( string exeName, string args )
        {
            Process process = new Process();
            process.StartInfo.FileName = exeName;
            process.StartInfo.Arguments = args;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if ( process.ExitCode != 0 )
            {
                throw new RunnerException( stderr.Length > 0 ? stderr : stdout );
            }

            return stdout;
        }
    }
    
    /// <summary>
	/// Supports running Perforce commands and parsing their output.
	/// </summary>
	internal class P4Runner: RunnerBase
	{
        private string _serverPort;
        private string _client;
        private string _userName;
        private string _password;

        public P4Runner( string serverPort, string client, string userName, string password )
        {
            _serverPort = serverPort;
            _client = client;
            _userName = userName;
            _password = password;
        }

        /// <summary>
        /// Returns the specified number of most recent changesets from the repository.
        /// </summary>
        /// <param name="count">The number of changesets to return.</param>
        /// <returns>The array of changeset descriptors.</returns>
        internal ChangeSetSummary[] GetLastChangeSets( int count )
        {
            return ParseChangesList( "-m " + count );
        }

        /// <summary>
        /// Returns the array of changesets which were made after the changeset with
        /// the specified number.
        /// </summary>
        /// <param name="changeSetNo">The number of the changeset to start processing
        /// with (not included in the results).</param>
        /// <param name="pathsToWatch">The repository paths for which changes should be loaded,
        /// or an empty array if changes should be loaded for the entire repository.</param>
        /// <returns>The array of changeset descriptors.</returns>
        internal ChangeSetSummary[] GetChangeSetsAfter( int changeSetNo, string[] pathsToWatch )
        {
            StringBuilder descriptorBuilder = new StringBuilder();
            if (pathsToWatch.Length == 0)
            {
                descriptorBuilder.Append("@").Append(changeSetNo + 1).Append(",@now");
            }
            else
            {
                foreach (string pathToWatch in pathsToWatch)
                {
                    if (descriptorBuilder.Length > 0)
                    {
                        descriptorBuilder.Append(" ");
                    }
                    descriptorBuilder.Append(pathToWatch);
                    if (!pathToWatch.EndsWith( "/" ))
                    {
                        descriptorBuilder.Append( "/..." );
                    }
                    else
                    {
                        descriptorBuilder.Append( "..." );
                    }
                    descriptorBuilder.Append( "@" ).Append( changeSetNo + 1 ).Append( ",@now" );
                }
            }
            return ParseChangesList( descriptorBuilder.ToString() );
        }

        private ChangeSetSummary[] ParseChangesList( string changeSelector )
        {
            string changes = ReadPerforceStdout( "changes -s submitted -t " + changeSelector );
            changes = changes.Replace( "\r\n", "\n" );
            string[] modLines = changes.Split( '\n' );

            ArrayList changeSets = new ArrayList();
            ArrayList modLineList = new ArrayList( modLines );
            while ( modLineList.Count > 0 )
            {
                string modHeader = (string) modLineList [0];
                modLineList.RemoveAt( 0 );
                if ( modHeader == "" )
                    continue;

                int pos = modHeader.IndexOf( '\'' );
                if ( pos != 0 )
                {
                    modHeader = modHeader.Substring( 0, pos ).Trim();
                }

                string[] modHeaderFields = modHeader.Split( ' ' );
                if ( modHeaderFields [0] != "Change" || modHeaderFields.Length != 7 )
                {
                    Trace.WriteLine( "Invalid changelog format: " + modHeader );
                    break;
                }

                changeSets.Add( ParseChangesLine( modHeaderFields ) );
            }
            return (ChangeSetSummary[]) changeSets.ToArray( typeof(ChangeSetSummary) );
        }

        /// <summary>
        /// Completes parsing of a single line of the changes list.
        /// </summary>
        /// <param name="modHeaderFields">The array of fields into which the line has been split.</param>
        private ChangeSetSummary ParseChangesLine( string[] modHeaderFields )
        {
            int changeSetNo = Int32.Parse( modHeaderFields [1] );
    
            DateTime changeTime = DateTime.ParseExact(modHeaderFields [3] + " " + modHeaderFields [4],
                "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            string[] userClient = modHeaderFields [6].Split( new char[] { '@' }, 2 );

            return new ChangeSetSummary( changeSetNo, userClient [0], userClient [1], changeTime );
        }

        /// <summary>
        /// Returns the details of the changeset with the specified number.
        /// </summary>
        /// <param name="changeSetNumber">The changeset to describe.</param>
        /// <returns>The details of the changeset.</returns>
        internal ChangeSetDetails DescribeChangeSet( int changeSetNumber )
        {
            string desc = ReadPerforceStdout( "describe -du " + changeSetNumber );
            desc = desc.Replace( "\r\n", "\n" );
            var descLines = new List<string>( desc.Split( '\n' ) );
            // skip heading
            descLines.RemoveAt( 0 );

            StringBuilder descBuilder = new StringBuilder();
            FileChangeData lastChange = null;
            var fileChanges = new List<FileChangeData>();
            var fileChangesMap = new Dictionary<string, FileChangeData>();

            bool affectedStarted = false, differencesStarted = false;
            while ( descLines.Count > 0 )
            {
                string descLine = descLines [0];
                descLines.RemoveAt( 0 );
                if ( descLine == "" )
                    continue;
                if ( !affectedStarted )
                {
                    if ( descLine.StartsWith("Affected files ..." ) )
                    {
                        affectedStarted = true;
                    }
                    else
                    {
                        if ( descBuilder.Length > 0 )
                        {
                            descBuilder.Append( ' ' );
                        }
                        descBuilder.Append( descLine.Trim() );
                    }
                }
                else if ( !differencesStarted )
                {
                    if ( descLine.StartsWith( "Differences ..." ) )
                    {
                        differencesStarted = true;
                    }
                    else
                    {
                        descLine = descLine.Substring( 4 );
                        int pos = descLine.IndexOf( '#' );
                        string path = descLine.Substring( 0, pos );
                        descLine = descLine.Substring( pos );
                        pos = descLine.IndexOf( ' ' );
                        int revision = Int32.Parse( descLine.Substring( 1, pos ) );
                        string changeType = descLine.Substring( pos + 1 );
                        var change = new FileChangeData( path, revision, changeType );
                        fileChanges.Add( change );
                        fileChangesMap.Add( path, change );
                    }
                }
                else
                {
                    if ( descLine.StartsWith( "====" ) && descLine.EndsWith( "====" ) )
                    {
                        int revisionPos = descLine.LastIndexOf( '#' );
                        string path = descLine.Substring( 5, revisionPos-5 ); // skip =====
                        lastChange = fileChangesMap [path];
                        if ( descLine.IndexOf( "(binary)", revisionPos ) >= 0 )
                        {
                            lastChange.Binary = true;
                        }
                    }
                    else
                    {
                        lastChange.AppendDiff( descLine + "\r\n" );
                    }
                }
            }
            return new ChangeSetDetails( descBuilder.ToString(), fileChanges.ToArray() );
        }

        /// <summary>
        /// Runs a "p4 user" command to get the information about the Perforce user with
        /// the specified user name.
        /// </summary>
        /// <param name="userName">The user name of the user.</param>
        /// <param name="email">The e-mail address of the user.</param>
        /// <param name="fullName">The full name of the user.</param>
        internal void DescribeUser( string userName, out string email, out string fullName )
        {
            fullName = userName;
            email = null;
            string desc = ReadPerforceStdout( "user -o " + userName );
            desc = desc.Replace( "\r\n", "\n" );
            foreach( string line in desc.Split( '\n' ) )
            {
                if ( line.StartsWith( "Email:" ) )
                {
                    email = line.Substring( "Email:".Length ).Trim();
                }
                else if ( line.StartsWith( "FullName:" ) )
                {
                    fullName = line.Substring( "FullName:".Length ).Trim();
                }
            }
        }

        /// <summary>
        /// Starts the p4.exe executable with the specified arguments, reads and returns the
        /// text it prints to the stdout.
        /// </summary>
        /// <param name="args">The arguments to pass to p4.exe</param>
        /// <returns>The standard output of the process.</returns>
        private string ReadPerforceStdout( string args )
        {
            return ReadStdout("p4.exe", BuildLoginArgs() + " " + args);
        }

        private string BuildLoginArgs()
        {
            string args = "";
            if ( _serverPort != null && _serverPort.Length > 0 )
            {
                args += " -H " + _serverPort;
            }
            if ( _client != null && _client.Length > 0 )
            {
                args += " -c " + _client;
            }
            if ( _userName != null && _userName.Length > 0 )
            {
                args += " -u " + _userName;
            }
            if ( _password != null && _password.Length > 0 )
            {
                args += " -P " + _password;
            }
            return args;
        }
	}

    /// <summary>
    /// The exception which is thrown when the p4.exe process returns a non-zero exit code.
    /// </summary>
    internal class RunnerException: Exception
    {
        internal RunnerException( string message )
            : base( message ) { }
    }

}
