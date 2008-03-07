/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.ExceptionReport;

namespace JetBrains.Omea
{
    /// <summary>
    /// Handles uncaught exceptions and reports them to the tracker.
    /// </summary>
    internal class CustomExceptionHandler
    {
        private Control _ownerControl;
        private string _dbCreatorBuild;
        private bool _newDatabase;
        private bool _indexesRebuilt;
        private string _extraData = "";
        private bool _exceptionDialogVisible;
        private Queue _exceptionQueue = new Queue();
        private bool _ignoreAllExceptions;

        internal class QueuedException
        {
            Exception _exception;
            ExceptionReportFlags _flags;

            public QueuedException( Exception exception, ExceptionReportFlags flags )
            {
                _exception = exception;
                _flags = flags;
            }

            public Exception Exception
            {
                get { return _exception; }
            }

            public ExceptionReportFlags Flags
            {
                get { return _flags; }
            }
        }

        internal string DBCreatorBuild
        {
            get { return _dbCreatorBuild; }
            set { _dbCreatorBuild = value; }
        }

        internal bool NewDatabase
        {
            get { return _newDatabase; }
            set { _newDatabase = value; }
        }

        internal bool IndexesRebuilt
        {
            get { return _indexesRebuilt; }
            set { _indexesRebuilt = value; }
        }

        internal Control OwnerControl
        {
            get { return _ownerControl; }
            set { _ownerControl = value; }
        }
       
        internal void OnThreadException( object sender, ThreadExceptionEventArgs e )
        {
            PluginEnvironment environment = (PluginEnvironment) ICore.Instance;
            if ( !environment.NotifyBackgroundThreadException( e.Exception ) )
            {
                ReportException( e.Exception, 0 );
            }
        }

        internal void AddExceptionReportData( string data )
        {
            if ( _extraData != "" )
                _extraData += ",";
            _extraData += data;
        }

        internal void ReportException( Exception ex, ExceptionReportFlags flags )
        {
            Trace.WriteLine( ex.ToString() );
            if ( IsIgnoredException( ex ) )
            {
                return;
            }

            lock( _exceptionQueue )
            {
                _exceptionQueue.Enqueue( new QueuedException( ex, flags ) );
            }

            if ( _ownerControl != null && _ownerControl.IsHandleCreated && !_ownerControl.IsDisposed && 
                _ownerControl.InvokeRequired )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( ShowExceptionDialog ) );
            }
            else
            {
                ShowExceptionDialog();
            }
        }

        private bool IsIgnoredException( Exception ex )
        {
            if ( _ignoreAllExceptions )
            {
                return true;
            }

            string[] stackTrace = ex.ToString().Split( '\n' );
            if ( stackTrace.Length < 2 )
            {
                return false;
            }

            string topMethod = stackTrace [1];
            if ( topMethod.IndexOf( "System.Windows.Forms.UnsafeNativeMethods.CallWindowProc" ) > 0 )
            {
                return true;
            }

            return false;
        }

        private void ShowExceptionDialog()
        {
            if ( _exceptionDialogVisible )
                return;

            _exceptionDialogVisible = true;
            bool exceptionFatal = false;
        	try
        	{
        		while( true )
        		{
        			QueuedException qex = null;
        			lock( _exceptionQueue )
        			{
        				if ( _exceptionQueue.Count > 0 )
        				{
        					qex = (QueuedException) _exceptionQueue.Dequeue();
        				}
        			}
        			if ( qex == null )
        				break;
            
        			if ( ( qex.Flags & ExceptionReportFlags.Fatal ) != 0 )
        			{
        				exceptionFatal = true;
        			}

        			Exception ex = qex.Exception;
        			if ( ex is AsyncProcessorException )
        				ex = ex.InnerException;
        			if ( ex is TargetInvocationException )
        				ex = ex.InnerException;

        			ISettingStore ini = Core.SettingStore;
        			using( ExceptionReportForm dlg = new ExceptionReportForm() )
        			{
        				ProxySettings proxySettings = new ProxySettings();
        				try
        				{
        					string address = ini.ReadString( "HttpProxy", "Address" );
        					proxySettings.CustomProxy = address.Length > 0;
        					if( proxySettings.CustomProxy )
        					{
        						proxySettings.Host = address;
        						proxySettings.Port = ini.ReadInt( "HttpProxy", "Port", 3128 );
        						proxySettings.Login = ini.ReadString( "HttpProxy", "User" );
        						proxySettings.Password = ini.ReadString( "HttpProxy", "Password" );
        					}
        					dlg.SetProxy( proxySettings );
        				}
        				catch( Exception pex )
        				{
        					Trace.WriteLine( "Failed to set exception reporter proxy: " + pex.ToString() );
        				}
        				// Setup our submitter
        				dlg.Submitter = new RPCExceptionSubmitter();
        				dlg.ProjectKey = "OM";
        				dlg.DisplaySubmissionResult = true;
        				string userName = ini.ReadString( "ErrorReport", "UserName" );
        				string password = ini.ReadString( "ErrorReport", "Password" );
        				if ( userName.Length > 0 && password.Length > 0 )
        				{
        					dlg.SetITNLogin( userName, password );
        				}
        				else
        				{
        					dlg.SetDefaultLogin( "om_anonymous", "guest" );
        				}

        				if ( ( qex.Flags & ExceptionReportFlags.AttachLog ) != 0 )
        				{
        					dlg.AttachLog = true;
        				}
                    
        				dlg.SetBuildNumber( Assembly.GetExecutingAssembly().GetName().Version.Build );
                
        				IWin32Window ownerWindow = ( _ownerControl == null || _ownerControl.IsDisposed ) ? null : _ownerControl;
                
        				if ( dlg.ReportException( ownerWindow, ex, GetExceptionDescription() ) == DialogResult.OK )
        				{
        					if ( dlg.ITNUserName != "om_anonymous" )
        					{
        						ini.WriteString( "ErrorReport", "UserName", dlg.ITNUserName );
        						ini.WriteString( "ErrorReport", "Password", dlg.ITNPassword );
        					}

        					if ( dlg.AttachLog )
        					{
        						LogManager.SubmitErrorLog();
        					}
        					proxySettings = dlg.ProxySettings;
        					if( !proxySettings.CustomProxy )
        					{
        						ini.WriteString( "HttpProxy", "Address", string.Empty );
        					}
        					else
        					{
        						ini.WriteString( "HttpProxy", "Address", proxySettings.Host );
        						ini.WriteInt( "HttpProxy", "Port", proxySettings.Port );
        						ini.WriteString( "HttpProxy", "User", proxySettings.Login );
        						ini.WriteString( "HttpProxy", "Password", proxySettings.Password );
        					}
        				}
        			}
        		}
        	}
        	catch(Exception ex)
        	{
        		MessageBox.Show(null, "An exception has occured in the application, and the exception reporter failed to present it.\n\n" + ex.Message + "\n\nThe application will now be terminated.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        		exceptionFatal = true;
        	}
            _exceptionDialogVisible = false;

            if ( exceptionFatal )
            {
                (Core.MainWindow as MainFrame).ForceClose();
            }
        }

        internal string GetExceptionDescription()
        {
            string description = Core.ProductFullName + " v" + Core.ProductVersion + " starting with a ";
            if ( _newDatabase )
            {
                description += "new database";
            }
            else
            {
                description += "database from build " + _dbCreatorBuild;
                if ( _indexesRebuilt )
                    description += " after a crash";
                else
                    description += " after a clean shutdown";
            }
            description += ", " + Core.State;
            description += ", " + MainFrame.PlatformVersions + ", active - " + Environment.Version;
            if ( _extraData.Length > 0 )
            {
                description += ". ";
                description += _extraData;
            }
            return description;
        }

        public void IgnoreAllExceptions()
        {
            _ignoreAllExceptions = true;
        }
    }
}
