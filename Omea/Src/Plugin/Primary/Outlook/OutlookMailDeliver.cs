/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using JetBrains.Omea.COM;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Interop;
using Microsoft.Win32;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class OutlookMailDeliver
    {
        private _com_OutlookExporer _explorer = null;
        private System.IntPtr _mainWnd;
        private const int cTimeOut = 2000;
        protected static Tracer _tracer = new Tracer( "OutlookMailDeliver" );
        private static bool _shown = false;

        private const string cErrorSendReceiveV11 = "Failed to invoke Send/Receive command. Please make sure that the 'Send/Receive All' menu item is available in the 'Tools | Send/Receive' menu in Outlook.";
        private const string cErrorSendReceiveVolder = "Failed to invoke Send/Receive command. Please make sure that the 'Send/Receive' button is available on the main toolbar of Outlook.";
        private const string cErrorCOMInitialize = "Omea is unable to initialize Outlook. Most probably your antivirus software prevents it to activate Outlook DLLs. Please contact <feedback.omea@jetbrains.com> for support.";

        protected void ReleaseCOM()
        {
            try
            {
                if ( _explorer != null )
                {
                    _explorer.Release();
                }
            }
            catch ( COMException exception )
            {
                _tracer.TraceException( exception );
            }
        }

        protected OutlookMailDeliver()
        {
            _mainWnd = new IntPtr( 0 );
        }

        public static void DeliverNow()
        {
            try
            {
                new OutlookMailDeliver().SendReceive();
            }
            catch ( COMException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( FileNotFoundException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( InvalidComObjectException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( InvalidCastException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( System.Threading.ThreadAbortException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( Exception exception )
            {
                Exception innerException = exception.InnerException;
                while ( innerException != null )
                {
                    if ( innerException is COMException )
                    {
                        _tracer.Trace( "Ignore COM exceptions" );
                        return;
                    }
                    Tracer._TraceException( innerException );
                }
                Tracer._TraceException( exception );
                Core.ReportException( exception, ExceptionReportFlags.AttachLog );
            }
        }

        public void SendReceive()
        {
            try
            {
                if ( !OutlookSession.IsOutlookRun )
                {
                    OutlookGUIInit.StartOutlook( ProcessWindowStyle.Minimized );
                    if ( !IsMainWndReady() ) return;
                    OutlookGUIInit.ActivateGUI( _mainWnd );
                }
                _explorer = OutlookGUIInit.IsOutlookExplorerReady( cTimeOut );
                if ( _explorer == null )
                {
                    StandartJobs.MessageBox( cErrorCOMInitialize, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    return;
                }

                ProcessDeliverNow( );
                ReleaseCOM();
            }
            catch ( COMException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( FileNotFoundException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( InvalidComObjectException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( InvalidCastException exception )
            {
                _tracer.TraceException( exception );
            }
        }

        private void ShowErrorMessage()
        {
            if( !_shown )
            {
                _shown = true;
                string message = (OutlookSession.Version >= 11) ? cErrorSendReceiveV11 : cErrorSendReceiveVolder;
                StandartJobs.MessageBox( message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        protected void ProcessDeliverNow( )
        {
            try
            {
/*
                _tracer.Trace( "Start of send/receive" );
                _tracer.Trace( "Get raw pointer on Outlook.Explorer" );
*/
                object oDocument = null;
                Outlook.Explorer explorer = ( Outlook.Explorer )_explorer.COM_Pointer;
                if ( explorer != null )
                {
                    try
                    {
//                        _tracer.Trace( "Try to find CommandBars" );
                        oDocument = explorer.GetType().InvokeMember( "CommandBars", BindingFlags.GetProperty, null, explorer, null );
                    }
                    catch ( COMException )
                    {
                        ShowErrorMessage();
                        return;
                    }
                }
                else
                {
                    _tracer.Trace( "Cannot find explorer" );
                    return;
                }

                if ( oDocument == null )
                {
                    _tracer.Trace( "Cannot find CommandBars" );
                    ShowErrorMessage();
                    return;
                }

                _tracer.Trace( "OutlookSession.Version = " + OutlookSession.Version );
                if ( OutlookSession.Version >= 11 )
                {
                    bool found = false;
                    try
                    {
                        object mainMenu = oDocument.GetType().InvokeMember( "Item", BindingFlags.GetProperty, null, oDocument, new object[] { "Menu Bar" } );
                        if ( mainMenu != null )
                        {
                            object toolsMenu = mainMenu.GetType().InvokeMember( "Controls", BindingFlags.GetProperty, null, mainMenu, new object[] { 5 } );
                            if ( toolsMenu != null )
                            {
                                object sendMenu = toolsMenu.GetType().InvokeMember( "Controls", BindingFlags.GetProperty, null, toolsMenu, new object[] { 1 } );
                                if ( sendMenu != null )
                                {
                                    object sendReceiveAll = sendMenu.GetType().InvokeMember( "Controls", BindingFlags.GetProperty, null, sendMenu, new object[] { 1 } );
                                    if ( sendReceiveAll != null )
                                    {
                                        sendReceiveAll.GetType().InvokeMember( "Execute",BindingFlags.InvokeMethod,null,sendReceiveAll, new object[] {} );
                                        found = true;
                                    }
                                }
                            }
                        }
                    }
                    catch ( COMException exception )
                    {
                        _tracer.TraceException( exception );
                    }
                    if ( !found )
                    {
                        ShowErrorMessage();
                        return;
                    }
                    
                    /*
                    Parameters [0] = (int) 13;
                    Parameters [1] = (int) 32828;
                    
                    object button = oDocument.GetType().InvokeMember( "FindControl",BindingFlags.InvokeMethod,null,oDocument, Parameters );
                    
                    object controls = button.GetType().InvokeMember( "Controls", BindingFlags.GetProperty, null, button, new object[] {} );
                    object control0 = controls.GetType().InvokeMember( "Item", BindingFlags.GetProperty, null, controls, new object[] { 2 } );
                    control0.GetType().InvokeMember( "Execute",BindingFlags.InvokeMethod,null,button, new object[] {} );
                    */
                }
                else
                {
                    object[] Parameters = new Object[2];
                    Parameters[0] = 1;
                    Parameters[1] = 5488;
                    object button = null;
                    try
                    {
                        button = oDocument.GetType().InvokeMember( "FindControl", BindingFlags.InvokeMethod, null, oDocument, Parameters );
                        if ( button != null )
                        {
                            button.GetType().InvokeMember( "Execute", BindingFlags.InvokeMethod, null, button, new object[] {} );
                        }
                    }
                    catch ( COMException exception )
                    {
                        _tracer.TraceException( exception );
                        button = null;
                    }

                    if ( button == null )
                    {
                        ShowErrorMessage();
                        return;
                    }
                }
                _tracer.Trace("Start of send/receive");
            }
            catch ( COMException ex )
            {
                _tracer.TraceException( ex );
            }
        }

        private bool IsMainWndReady( )
        {
            _mainWnd = OutlookGUIInit.PrepareMainWndReady( cTimeOut );
            return (int)_mainWnd != 0;
        }
    }

    internal class OutlookGUIInit
    {
        public  static bool   _isFileNotFoundHappened = false;
        private static Tracer _tracer = new Tracer( "OutlookGUIInit" );
        private OutlookGUIInit(){}

        static public void DebugMessageBox( string message )
        {
            if ( Settings.DebugMessageBox )
                MessageBox.Show( message );
        }
        static public _com_OutlookExporer IsOutlookExplorerLoaded( )
        {
            Outlook.Application outlook = null;
            Outlook.NameSpace nameSpace = null;
            _com_OutlookExporer explorer = null;
            _isFileNotFoundHappened = false;
            try
            {
                _tracer.Trace("Looking for explorer");
                OutlookGUIInit.DebugMessageBox( "Looking for explorer" );

                outlook =  new Outlook.ApplicationClass();
                _tracer.Trace("Outlook.Application object has been initialized properly.");

                nameSpace = outlook.GetNamespace("MAPI");
                _tracer.Trace("Outlook.NameSpace object has been initialized properly? - " + (nameSpace != null).ToString());

                explorer = new _com_OutlookExporer( outlook.ActiveExplorer() );
                _tracer.Trace("_com_OutlookExporer wrapper object has been initialized properly.");
                OutlookGUIInit.DebugMessageBox( "Get ActiveExplorer" );

                if ( explorer == null )
                {
                    _tracer.Trace("Outlook explorer is not found");
                }
                else
                {
                    _tracer.Trace("Outlook explorer is found");
                }
            }
            catch ( FileNotFoundException exception )
            {
                _isFileNotFoundHappened = true;
                _tracer.TraceException( exception );
            }
            catch ( COMException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( InvalidComObjectException exception )
            {
                _tracer.TraceException( exception );
            }
            catch ( InvalidCastException exception )
            {
                _tracer.TraceException( exception );
            }
            finally
            {
                COM_Object.Release( outlook );
                COM_Object.Release( nameSpace );
            }
            return explorer;
        }

        static public _com_OutlookExporer IsOutlookExplorerReady( int cTimeOut )
        {
            int begin = Environment.TickCount;

            _com_OutlookExporer explorer = null;
            while ( (explorer = IsOutlookExplorerLoaded( )) == null && ( Environment.TickCount - begin ) < cTimeOut )
            {
                _tracer.Trace("Waiting for explorer");
            }
            return explorer;
        }


        static public bool StartAndInitializeOutlook(  )
        {
            if ( OutlookSession.IsOutlookRun )
            {
                return true;
            }

            _com_OutlookExporer explorer = null;
            try
            {
                OutlookGUIInit.StartOutlook( ProcessWindowStyle.Minimized );
                IntPtr mainWnd = OutlookGUIInit.PrepareMainWndReady( 2000 );
                if ( (int)mainWnd == 0 )
                {
                    return false;
                }
                ActivateGUI( mainWnd );
                return IsOutlookExplorerReady( 2000 ) != null;
            }
            catch ( COMException exception )
            {
                Tracer._TraceException( exception );
                return false;
            }
            finally
            {
                if ( explorer != null )
                {
                    try
                    {
                        explorer.Release();
                    }
                    catch ( COMException exception )
                    {
                        Tracer._TraceException( exception );
                    }
                }
            }
        }

        static public void StartOutlook( ProcessWindowStyle processWindowStyle )
        {
            DebugMessageBox( "Before start Outlook process" );
            Tracer._Trace("Loading outlook...");
            string path = RegUtil.GetValue( Registry.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\OUTLOOK.EXE", "" ) as string;
            if ( path == null )
            {
                path = "Outlook.exe";
            }
            Tracer._Trace("Trying to start Outlook from path =[" + path + "]" );

            Process process = new Process();
            process.StartInfo.FileName = path;
            process.StartInfo.WindowStyle = processWindowStyle;
            process.StartInfo.UseShellExecute = false;
            try
            {
                process.Start();
                Tracer._Trace("Outlook from path =[" + path + "] started without exceptions." );
            }
            catch ( System.Threading.ThreadAbortException ex )
            {
                Tracer._TraceException( ex );                    
            }
            catch ( Exception exception )
            {
                Tracer._Trace( "StartOutlook -- general exception while starting Outlook process:\n" + exception.Message );
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = "Outlook.exe";
                process.Start();
            }
            DebugMessageBox( "Outlook was started" );
        }
        static public void ActivateGUI( System.IntPtr mainWnd )
        {
            DebugMessageBox( "before ActivateGUI" );
            Tracer._Trace("Activate GUI");
            Win32Declarations.SendMessage( mainWnd, Win32Declarations.WM_ACTIVATEAPP, (IntPtr) 1, IntPtr.Zero );
            DebugMessageBox( "after WM_ACTIVATEAPP" );
            Win32Declarations.SendMessage( mainWnd, Win32Declarations.WM_NCACTIVATE, (IntPtr) 0x200001, IntPtr.Zero );
            DebugMessageBox( "after WM_NCACTIVATE" );
            Win32Declarations.SendMessage( mainWnd, Win32Declarations.WM_ACTIVATE, (IntPtr) 0x200001, IntPtr.Zero );
            DebugMessageBox( "after WM_ACTIVATE" );
            Win32Declarations.SendMessage( mainWnd, Win32Declarations.WM_ACTIVATETOPLEVEL, (IntPtr) 0x200001, (IntPtr) 0x13FBE8 );
            DebugMessageBox( "after WM_ACTIVATETOPLEVEL" );
            Win32Declarations.SendMessage( mainWnd, Win32Declarations.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero );
            int SIZE_MINIMIZED = 1;
            Win32Declarations.SendMessage( mainWnd, Win32Declarations.WM_SIZE, (IntPtr)SIZE_MINIMIZED, IntPtr.Zero );
            
            DebugMessageBox( "after WM_SETFOCUS" );
            Tracer._Trace("GUI activated");
        }
        static public System.IntPtr PrepareMainWndReady( int cTimeOut )
        {
            IntPtr mainWnd = GenericWindow.FindWindow( "rctrl_renwnd32", null );
            int begin = Environment.TickCount;
            Tracer._Trace("Waiting while main window is loaded");
            while ( (int)mainWnd == 0 && ( Environment.TickCount - begin ) < cTimeOut )
            {
                mainWnd = GenericWindow.FindWindow( "rctrl_renwnd32", null );
            }
            return mainWnd;
        }
    }
}
