// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Diagnostics
{
    public class Tracer
    {
        private string _traceCategory = string.Empty;
        private const string cExceptionOccurs = "Exception occurs...";
        private const string cItsCOMException = "It's COM exception...";
        private const string cErrorCode = "ErrorCode = ";
        private const string cExceptionMessage = "Exception message: ";
        //private static string cExceptionSource = "Exception source: ";
        //private static string cHelpLink = "Exception help link: ";
        private const string cStackTrace = "Exception stack trace: ";
        private const string cInnerException = "Exception has inner exception...";
        private const string cExceptionType = "Exception type is: ";
        private static Tracer _tracer = new Tracer( "Tracer" );

        public Tracer( string traceCategory )
        {
            _traceCategory = traceCategory;
        }

        public static void _Trace( string trace )
        {
            _tracer.Trace( trace );
        }

        public static void _Trace( string message, object objectToTrace )
        {
            _tracer.Trace( message + " " + objectToTrace );
        }

        public static void _TraceException( Exception exception )
        {
            _tracer.TraceException( exception );
        }

        public void Trace( string trace )
        {
            System.Diagnostics.Trace.WriteLine( trace, _traceCategory );
        }

        public void Trace( string message, object objectToTrace )
        {
            System.Diagnostics.Trace.WriteLine( message + " " + objectToTrace, _traceCategory );
        }

        public void TraceException( Exception exception )
        {
            if ( exception == null ) return;
            System.Diagnostics.Trace.WriteLine( cExceptionOccurs, _traceCategory );

            if ( exception is COMException )
            {
                System.Diagnostics.Trace.WriteLine( cItsCOMException, _traceCategory );
                COMException comException = (COMException)exception;
                System.Diagnostics.Trace.WriteLine( cErrorCode + comException.ErrorCode, _traceCategory );
            }

            System.Diagnostics.Trace.WriteLine( cExceptionType + exception.GetType(), _traceCategory );
            System.Diagnostics.Trace.WriteLine( cExceptionMessage + exception.Message, _traceCategory );
            //System.Diagnostics.Trace.WriteLine( cExceptionSource + exception.Source, _traceCategory );
            //System.Diagnostics.Trace.WriteLine( cHelpLink + exception.HelpLink, _traceCategory );
            System.Diagnostics.Trace.WriteLine( cStackTrace + exception.StackTrace, _traceCategory );
            Exception innerException = exception.InnerException;
            if ( innerException != null )
            {
                System.Diagnostics.Trace.WriteLine( cInnerException, _traceCategory );
                TraceException( innerException );
            }
        }

        public static void TraceFocusedControl()
        {
            Form mainForm = (Form) Core.MainWindow;
            if ( mainForm.ActiveControl == null )
            {
                System.Diagnostics.Trace.WriteLine( "There is no active control in main form" );
            }
            else
            {
                System.Diagnostics.Trace.Write( "The active control in the main form is" );
                Control ctl = mainForm.ActiveControl;
                bool foundFocusedChild;
                do
                {
                    System.Diagnostics.Trace.Write( ": " + ctl.GetType().Name + " " + ctl.Name );
                    foundFocusedChild = false;
                    foreach( Control child in ctl.Controls )
                    {
                        if ( child.ContainsFocus )
                        {
                            ctl = child;
                            foundFocusedChild = true;
                            break;
                        }
                    }
                } while( foundFocusedChild );
                System.Diagnostics.Trace.WriteLine( "" );
            }
        }
    }
    public class MsgBox
    {
        private static Tracer _tracer = new Tracer( "MsgBox.Error" );
        public static void Error( string title, string message )
        {
            _tracer.Trace( title + " : " + message );
            MessageBox.Show( message, title, MessageBoxButtons.OK, MessageBoxIcon.Error );
        }
    }
}
