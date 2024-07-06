// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.GZip;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Manages writing of the log file and the usage log.
	/// </summary>
	internal class LogManager
	{
        private static string _logDir;
        private static string _logName;
        private static OmniaMeaTraceListener _traceListener;
        private static string _usageLogId;
#if USAGE_LOG
        private static StreamWriter _usageLogWriter;
#endif

        internal static void InitializeLog()
        {
            _logDir = RegUtil.LogPath;
            if ( _logDir == null )
            {
                _logDir = Application.StartupPath;
            }

            string logName = "Omea-" + DateTime.Now.ToString( "yyMMdd-HHmm" ) + ".log";

            if ( _logDir != "" )
            {
                _logName = Path.Combine( _logDir, logName );
                try
                {
                    Directory.CreateDirectory( _logDir );
                    StreamWriter fs = File.CreateText( _logName );
                    fs.Close();
                }
                catch( Exception )
                {
                    // don't fail if the log dir is set to a folder where we do not have write access
                    _logDir = "";
                }

            }
            if ( _logDir != "" )
            {
                _traceListener = new OmniaMeaTraceListener( _logName );
                Debug.Listeners.Add( _traceListener );
                Trace.WriteLine( "Starting Omea" );
            }
        }

        internal static void InitializeUsageLog()
        {
            _usageLogId = Core.SettingStore.ReadString( "MainFrame", "UsageLogID ");
            if ( string.IsNullOrEmpty( _usageLogId ) )
            {
                _usageLogId = Guid.NewGuid().ToString();
                Core.SettingStore.WriteString( "MainFrame", "UsageLogID", _usageLogId );
            }
            Core.AddExceptionReportData( "ID " + _usageLogId );
        }

		/// <summary>
		/// Deletes log files that are older than the specified number of days.
		/// </summary>
		/// <param name="maxAge">Maximum log age, in days.</param>
        internal static void DeleteOldLogs( int maxAge )
		{
			if( _logDir != "" )
			{
				// Find the log files, their name format is: "Omea-" + DateTime.Now.ToString( "yyMMdd-HHmm" ) + ".log"
				DirectoryInfo dirLogs = new DirectoryInfo( _logDir );
				ArrayList logFiles = new ArrayList();
				logFiles.AddRange( dirLogs.GetFiles( "Omea-??????-????.log" ) ); // Add Omea log files
				logFiles.AddRange( dirLogs.GetFiles( "Omea-??????-????.log.gz" ) ); // Add the log files that were packed for submitting along with an exception
				foreach( FileInfo logFile in logFiles )
				{
					try
					{
						if( (DateTime.Now - logFile.LastWriteTime).TotalDays >= maxAge )
							logFile.Delete();
					}
					catch( Exception )
					{
					}
				}
			}
		}

		/// <summary>
		/// Generates an unique name for a usage log file and opens the file.
		/// </summary>
        internal static void InitUsageLog()
        {
#if USAGE_LOG
            string usageLogName = Path.Combine( _logDir, "UsageLog.txt" );
            bool usageLogEnabled = Core.SettingStore.ReadBool( "MainFrame", "UsageLogEnabled", true );
            if ( usageLogEnabled )
            {
                if ( _usageLogWriter == null )
                {
                    _usageLogWriter = new StreamWriter( usageLogName, true );
                    WriteToUsageLog( "[Run] Starting [" + Core.ProductName + "] ver [" + Core.ProductVersion + "]" );
                }
            }
            else
            {
                if ( _usageLogWriter != null )
                {
                    _usageLogWriter.Close();
                }
                _usageLogWriter = null;
                File.Delete( usageLogName );
            }
#endif
        }

		/// <summary>
		/// Writes a string to the usage log.
		/// </summary>
        internal static void WriteToUsageLog( string text )
        {
#if USAGE_LOG
            if ( _usageLogWriter != null )
            {
                _usageLogWriter.Write( DateTime.Now.ToString( "dd.MM.yyyy HH:mm:ss " ) + text + "\n" );
            }
#endif
        }

        internal static bool UsageLogEnabled
        {
            get
            {
#if USAGE_LOG
                return _usageLogWriter != null;
#else
                return false;
#endif
            }

        }

		/// <summary>
		/// Submits the usage log to the server.
		/// </summary>
        internal static void SubmitUsageLog()
        {
#if USAGE_LOG
            if ( _usageLogWriter == null )
            {
                return;
            }
            _usageLogWriter.Close();
            _usageLogWriter = null;

            string usageLogName = Path.Combine( _logDir, "UsageLog.txt" );
            string usageLogID = Core.SettingStore.ReadString( "MainFrame", "UsageLogID ");

            WebClient client = new WebClient();
            try
            {
                client.UploadFile( "http://collect-it.jetbrains.net/" + usageLogID, "POST", usageLogName );
                File.Delete( usageLogName );
            }
            catch( Exception ex )
            {
                Trace.WriteLine( "Error submitting usage log: " + ex );
            }

            InitUsageLog();
#endif
        }

        internal static void CloseUsageLog()
        {
#if USAGE_LOG
            if ( _usageLogWriter != null )
            {
                _usageLogWriter.Close();
				_usageLogWriter = null;
            }
#endif
        }

	    public static void SubmitErrorLog()
	    {
            if ( _logName == null )
            {
                return;
            }

            _traceListener.Close();
            Debug.Listeners.Remove( _traceListener );

            Exception submitError = null;

	        using( new WaitCursorDisplayer() )
	        {
                try
                {
                    string gzLogName = _logName + ".gz";
                    Stream fileStream = new FileStream( gzLogName, FileMode.Create );
                    GZipOutputStream gzStream = new GZipOutputStream( fileStream );
                    Stream logStream = new FileStream( _logName, FileMode.Open );
                    if ( logStream.Length > 1000000 )
                    {
                        logStream.Position = logStream.Length - 1000000;
                    }

                    int bytesRemaining = (int) (logStream.Length - logStream.Position);
                    byte[] buffer = new byte[65536] ;
                    while( bytesRemaining > 0 )
                    {
                        int blockSize = Math.Min( bytesRemaining, 65536 );
                        bytesRemaining -= blockSize;
                        logStream.Read( buffer, 0, blockSize );
                        gzStream.Write( buffer, 0, blockSize );
                    }
                    logStream.Close();
                    gzStream.Close();

                    WebClient client = new WebClient();
                    client.UploadFile( "http://collect-it.jetbrains.net/" + _usageLogId, "POST", gzLogName );
                    File.Delete( gzLogName );
                }
                catch( Exception ex )
                {
                    submitError = ex;
                }
	        }

            _traceListener = new OmniaMeaTraceListener( _logName );
            Debug.Listeners.Add( _traceListener );

            if ( submitError != null )
            {
                Trace.WriteLine( "Error submitting log: " + submitError.ToString() );
            }
        }
	}

    internal class OmniaMeaTraceListener: TextWriterTraceListener
    {
        private bool _NewLine = true;

        internal OmniaMeaTraceListener( string filename )
            : base( filename )
        {
        }

        public override void Write( string message )
        {
            try
            {
                if ( _NewLine )
                    WriteDate();
                base.Write(message);
                Writer.Flush();
            }
            catch( Exception )
            {
                // ignore
            }
        }

        public override void WriteLine( string message )
        {
            try
            {
                if ( _NewLine )
                    WriteDate();
                base.WriteLine( message );
                Writer.Flush();
                _NewLine = true;
            }
            catch( Exception )
            {
                // ignore
            }
        }

        private void WriteDate()
        {
            base.Write( DateTime.Now.ToString( "dd.MM.yyyy HH:mm:ss.fff " ) );

            string threadName = Thread.CurrentThread.Name;
            base.Write( "[" + ( !string.IsNullOrEmpty( threadName ) ? threadName [0] : '?' ) + "] " );

            _NewLine = false;
        }
    }


}
