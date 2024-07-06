// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
	/// <summary>
	/// Supports running Subversion commands and parsing their output.
	/// </summary>
	internal class SvnRunner: RunnerBase
	{
	    private string _repositoryUrl;
	    private string _userName;
	    private string _password;

	    public SvnRunner( string repositoryUrl, string userName, string password )
	    {
	        _repositoryUrl = repositoryUrl;
	        _userName = userName;
	        _password = password;
	    }

	    public string RepositoryUrl
	    {
	        get { return _repositoryUrl; }
	        set { _repositoryUrl = value; }
	    }

	    internal int GetLastRevision()
	    {
	        return Int32.Parse( GetSvnInfo( "Revision:" ) );
	    }

	    internal string GetSvnInfo( string key )
	    {
	        string svnInfo = ReadStdout( "svn.exe", GetLoginParameters() + "info " + _repositoryUrl );
	        string[] lines = svnInfo.Split( '\n' );
	        foreach( string line in lines )
	        {
	            if ( line.StartsWith( key ) )
	            {
	                return line.Substring( key.Length ).Trim();
	            }
	        }
	        throw new Exception( key + " line not found in Subversion output" );
	    }

	    public string GetXmlLog( int startRevision, int lastRevision )
	    {
	        string lastRevStr = (lastRevision >= 0) ? lastRevision.ToString() : "HEAD";
	        return ReadStdout( "svn.exe", GetLoginParameters() + "log " + _repositoryUrl +
	                                      " -r" + startRevision + ":" + lastRevStr +
	                                      " -v --xml" );
	    }

	    public string GetDiff( string repositoryPath, int fromRevision, int toRevision )
	    {
	        string url = BuildFullRepositoryPath( repositoryPath );
	        string cmdLine = GetLoginParameters() + "diff " + url + " -r" + fromRevision + ":" + toRevision;
	        string diff = ReadStdout( "svn.exe", cmdLine );
	        string[] lines = diff.Split( '\n' );
	        if ( lines.Length >= 4 )
	        {
                // first 4 lines are diff header
                return String.Join( "\n", lines, 4, lines.Length - 4 );
	        }
	        return "Failed to get diff: " + diff;
	    }

	    public string GetProperty( string repositoryPath, string propName )
	    {
            string url = BuildFullRepositoryPath( repositoryPath );
            string cmdLine = GetLoginParameters() + "propget " + propName + " " + url;
            return ReadStdout( "svn.exe", cmdLine );
        }

	    private string BuildFullRepositoryPath( string repositoryPath )
	    {
	        string url = _repositoryUrl;
	        if ( url.EndsWith( "/" ) )
	        {
	            url = url.Substring( 0, url.Length - 1 );
	        }
	        url +=  repositoryPath;
	        return url;
	    }

	    private string GetLoginParameters()
	    {
	        string result = "";
	        if ( _userName != null && _userName.Length > 0 )
	        {
	            result += "--username " + _userName + " ";
	        }
	        if ( _password != null && _password.Length > 0 )
	        {
	            result += "--password " + _password + " ";
	        }
	        return result;
	    }
	}
}
