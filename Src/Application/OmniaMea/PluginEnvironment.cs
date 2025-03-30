// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using CefSharpBrowserControl;
using JetBrains.Interop.WinApi;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Plugins;
using JetBrains.Omea.ResourceStore;

namespace JetBrains.Omea
{
   /**
     * The core implementation of IPluginEnvironment.
     */

    internal class PluginEnvironment: PicoCore.PicoCore
	{
        private IProgressWindow    _progressWindow;
        private MainFrame          _mainWindow;
        private AbstractWebBrowser _webBrowser;
        private AsyncProcessor     _resourceAP;
        private AsyncProcessor     _networkAP;
        private AsyncProcessor     _uiAP;
        private SizeF              _scaleFactor;
        private CustomExceptionHandler _excHandler;
        private CoreState          _state;
        private Version             _versionProduct;
        private IProtocolHandlerManager _protocolHandlerManager;
        private int                _idlePeriod;

		internal PluginEnvironment()
		{
		    theInstance = this;
            RegisterComponentImplementation( typeof(ResourceTabProvider) );
            RegisterComponentImplementation( typeof(PluginInterfaces) );
            RegisterComponentImplementation( typeof(DisplayColumnManager) );
        }

        public override IResourceStore ResourceStore
        {
            [DebuggerStepThrough] get { return MyPalStorage.Storage; }
        }
        public override IProtocolHandlerManager ProtocolHandlerManager
        {
            get { return _protocolHandlerManager; }
        }
        internal void SetProtocolHandlerManager( IProtocolHandlerManager protocolHandlerManager )
        {
            _protocolHandlerManager = protocolHandlerManager;
        }

        public override IProgressWindow ProgressWindow
        {
            [DebuggerStepThrough] get { return _progressWindow; }
        }

        internal void SetProgressWindow( IProgressWindow progressWindow )
        {
            _progressWindow = progressWindow;
        }

        internal new void RegisterComponentInstance( object componentInstance )
        {
            base.RegisterComponentInstance( componentInstance );
        }

        public override IWin32Window MainWindow
        {
            [DebuggerStepThrough] get { return _mainWindow; }
        }

        internal void SetMainWindow( MainFrame mainFrame )
        {
            _mainWindow = mainFrame;
        }

        public override AbstractWebBrowser WebBrowser => _webBrowser ??= new CefSharpWebBrowser();
        // TODO:
        // // Suppress some keys (if they're not handled by editor / event-handler / omea-actions)
        // IntHashSet hashSuppressed = new IntHashSet();
        // hashSuppressed.Add( (int)Keys.F5 ); // Refresh
        // hashSuppressed.Add( (int)(Keys.Control | Keys.N) ); // New window
        // hashSuppressed.Add( (int)(Keys.Escape) ); // Stop
        // hashSuppressed.Add( (int)(Keys.F11) ); // Theater mode

    	public override IAsyncProcessor ResourceAP
        {
            [DebuggerStepThrough] get { return _resourceAP; }
        }

        public override IAsyncProcessor NetworkAP
        {
            [DebuggerStepThrough] get { return _networkAP; }
        }

        public override IAsyncProcessor UserInterfaceAP
        {
            [DebuggerStepThrough] get { return _uiAP; }
        }

        internal void SetAsyncProcessors( AsyncProcessor resourceAP, AsyncProcessor networkAP )
        {
            _resourceAP = resourceAP;
            _networkAP = networkAP;
        }

        internal void SetUserInterfaceAP( AsyncProcessor uiAP )
        {
            _uiAP = uiAP;
        }

        public override string ProductName
        {
            get { return "Omea"; }
        }

        public override string ProductFullName
        {
            get
            {
#if READER
                return "Omea Reader";
#else
                return "Omea Pro";
#endif
            }
        }

        public override Version ProductVersion
        {
            get { return _versionProduct; }
        }

        public override string ProductReleaseVersion
        {
            get { return null; }
//            get { return "2.2"; }
        }

        internal void SetProductVersion( Version version )
        {
            _versionProduct = version;
        }

        public override SizeF ScaleFactor
        {
            get { return _scaleFactor; }
        }

        internal void SetScaleFactor( SizeF scaleFactor )
        {
            _scaleFactor = scaleFactor;
        }

        public override int IdlePeriod
        {
            get
            {
                if( _idlePeriod <= 0 )
                {
                    _idlePeriod = SettingStore.ReadInt( "Startup", "IdlePeriod", 1 );
                    if( _idlePeriod <= 0 )
                    {
                        IdlePeriod = 1;
                    }
                }
                return _idlePeriod;
            }
            set
            {
                _idlePeriod = value;
                SettingStore.WriteInt( "Startup", "IdlePeriod", value );
                int idlePeriod = value * 60000;
                if( _resourceAP != null )
                {
                    _resourceAP.IdlePeriod = idlePeriod;
                }
                if( _networkAP != null )
                {
                    _networkAP.IdlePeriod = idlePeriod;
                }

                TextIndexManager tim = base.TextIndexManager as TextIndexManager;
                if( tim != null )
                {
                    tim.IdlePeriod = idlePeriod;
                }
            }
        }

        public override bool IsSystemIdle
        {
            get
            {
                LASTINPUTINFO   lii = new LASTINPUTINFO();
                lii.cbSize = 8;
                WindowsAPI.GetLastInputInfo( ref lii );
                int idleDuration = (int) ( Kernel32Dll.GetTickCount() - lii.dwTime );
                int idlePeriod = IdlePeriod * 60000;
                return idleDuration >= idlePeriod;
            }
        }

        public override CoreState State
        {
            [DebuggerStepThrough] get { return _state; }
        }

        public override event EventHandler StateChanged;

        internal void SetState( CoreState state )
        {
            if ( _state != state )
            {
                _state = state;
                if ( StateChanged != null )
                {
                    StateChanged( this, EventArgs.Empty );
                }
            }
        }

        public override void ReportException( Exception e, bool fatal )
        {
            ReportException( e, fatal ? ExceptionReportFlags.Fatal : 0 );
        }

        public override void ReportException( Exception e, ExceptionReportFlags flags )
        {
            if ( ProductReleaseVersion != null && flags == 0 )
            {
                ReportBackgroundException( e );
            }
            else
            {
                _excHandler.ReportException( e, flags );
            }
        }

        public override void ReportBackgroundException( Exception e )
	    {
            _mainWindow.ReportBackgroundException( e );
	    }

        public override void RestartApplication()
        {
            if( !Core.UserInterfaceAP.IsOwnerThread )
            {
                Core.UserInterfaceAP.QueueJob(
                    JobPriority.Lowest, new MethodInvoker( RestartApplication ) );
            }
            else
            {
                MainFrame._restartFlag = true;
                _mainWindow.Close();
            }
        }

        public override void AddExceptionReportData( string data )
        {
            if ( _excHandler != null )
            {
                _excHandler.AddExceptionReportData( data );
            }
        }

        internal void SetExcHandler( CustomExceptionHandler excHandler )
        {
            _excHandler = excHandler;
        }

        public override IWebProxy DefaultProxy
        {
            get { return HttpReader.DefaultProxy; }
        }

        public override event OmeaThreadExceptionEventHandler BackgroundThreadException;

        internal bool NotifyBackgroundThreadException( Exception exception )
        {
            if ( BackgroundThreadException != null )
            {
                OmeaThreadExceptionEventArgs args = new OmeaThreadExceptionEventArgs( exception );
                BackgroundThreadException( this, args );
                return args.Handled;
            }
            return false;
        }
	}
}
