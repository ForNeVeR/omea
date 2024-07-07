// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JetBrains.Omea.HTML;
using JetBrains.Omea.OpenAPI;
using Microsoft.Win32;

namespace JetBrains.Omea
{
	/// <summary>
	/// Implementation of IEmailService based on Simple MAPI
	/// </summary>
	public class SimpleMapiEmailService: IEmailService
	{
        public void CreateEmail( string subject, string body, EmailBodyFormat bodyFormat,
            IResourceList recipients, string[] attachments, bool addSignature )
        {
            EmailRecipient[] emailRecipients = null;
            if ( recipients != null )
            {
                emailRecipients = new EmailRecipient [recipients.Count];
                for( int i=0; i<recipients.Count; i++ )
                {
                    IResource res = recipients [i];
                    if ( res.Type != "Contact" && res.Type != "EmailAccount" )
                    {
                        throw new ArgumentException( "Invalid recipient resource type" );
                    }
                    emailRecipients [i].Name = res.DisplayName;
                    if ( res.Type == "Contact" )
                    {
                        IContact contact = Core.ContactManager.GetContact( res );
                        emailRecipients [i].EmailAddress = contact.DefaultEmailAddress;
                    }
                    else
                    {
                        emailRecipients [i].EmailAddress = BuildAddressWithType( res.GetStringProp( "EmailAddress" ) );
                    }
                }
            }

            CreateEmail( subject, body, bodyFormat, emailRecipients, attachments, addSignature );
        }

        public void CreateEmail( string subject, string body, EmailBodyFormat bodyFormat,
            EmailRecipient[] recipients, string[] attachments, bool addSignature )
	    {
            MapiMessage msg = new MapiMessage();
            msg.subject = subject;

            if ( bodyFormat == EmailBodyFormat.Html )
            {
                body = body.Replace( "<p>", "\r\n\r\n" );
                body = body.Replace( "<P>", "\r\n\r\n" );
                body = body.Replace( "<br>", "\r\n" );
                body = body.Replace( "<BR>", "\r\n" );
                body = HtmlTools.StripHTML( body );
                body = HtmlTools.SafeHtmlDecode( body );
            }

            if( addSignature && Core.SettingStore.ReadBool( "MailFormat", "UseSignature", false ) )
            {
                body += "\r\n";
                body += Core.SettingStore.ReadString( "MailFormat", "Signature" );
            }

            msg.noteText = body;

	        if ( recipients != null && recipients.Length > 0 )
	        {
                ArrayList recipArray = new ArrayList();
                foreach( EmailRecipient recipient in recipients )
                {
                    MapiRecipDesc recip = new MapiRecipDesc();
                    recip.recipClass = MapiTO;
                    recip.name = recipient.Name;
                    recip.address = BuildAddressWithType( recipient.EmailAddress );
                    recipArray.Add( recip );
                }

                msg.recips = AllocRecips( recipArray );
                msg.recipCount = recipArray.Count;
            }

            if ( attachments != null && attachments.Length > 0 )
            {
                msg.files = AllocAttachs( attachments );
                msg.fileCount = attachments.Length;
            }

            try
            {
                int rc = MAPISendMail( IntPtr.Zero, Core.MainWindow.Handle, msg, MAPI_DIALOG, 0 );
                if ( rc != 0 && rc != MAPI_E_USER_ABORT )
                {
                    ReportError( rc );
                }
            }
            catch( Exception )
            {
                MessageBox.Show( Core.MainWindow, "Failed to send e-mail (unknown error)", "Send E-mail", MessageBoxButtons.OK );
            }


            if( msg.recips != IntPtr.Zero )
            {
                int runptr = (int) msg.recips;
                for( int i = 0; i < msg.recipCount; i++ )
                {
                    Marshal.DestroyStructure( (IntPtr) runptr, typeof(MapiRecipDesc) );
                    runptr += Marshal.SizeOf( typeof(MapiRecipDesc) );
                }
                Marshal.FreeHGlobal( msg.recips );
            }

            if( msg.files != IntPtr.Zero )
            {
                Type ftype = typeof(MapiFileDesc);
                int fsize = Marshal.SizeOf( ftype );

                int runptr = (int) msg.files;
                for( int i = 0; i < msg.fileCount; i++ )
                {
                    Marshal.DestroyStructure( (IntPtr) runptr, ftype );
                    runptr += fsize;
                }
                Marshal.FreeHGlobal( msg.files );
            }
	    }

	    private string BuildAddressWithType( string email )
	    {
	        try
	        {
                RegistryKey regKey = Registry.ClassesRoot.OpenSubKey( "mailto\\shell\\open\\command" );
                string value = (string) regKey.GetValue( "" );
                if ( value.ToLower( CultureInfo.InvariantCulture ).IndexOf( "outlook.exe" ) >= 0 )
                {
                    email = "SMTP:" + email;
                }
                regKey.Close();
	        }
            catch( Exception ex )
            {
                Trace.WriteLine( "Exception when determining default e-mail client in Simple MAPI: " + ex.ToString() );
            }
            return email;
	    }

	    private IntPtr AllocRecips( ArrayList recpts )
        {
            if( recpts.Count == 0 )
                return IntPtr.Zero;

            Type rtype = typeof(MapiRecipDesc);
            int rsize = Marshal.SizeOf( rtype );
            IntPtr ptrr = Marshal.AllocHGlobal( recpts.Count * rsize );

            int runptr = (int) ptrr;
            for( int i = 0; i < recpts.Count; i++ )
            {
                Marshal.StructureToPtr( recpts[i] as MapiRecipDesc, (IntPtr) runptr, false );
                runptr += rsize;
            }

            return ptrr;
        }

        private IntPtr AllocAttachs( string[] attachs )
        {
            if( attachs == null )
                return IntPtr.Zero;

            Type atype = typeof(MapiFileDesc);
            int asize = Marshal.SizeOf( atype );
            IntPtr ptra = Marshal.AllocHGlobal( attachs.Length * asize );

            MapiFileDesc mfd = new MapiFileDesc();
            mfd.position = -1;
            int runptr = (int) ptra;
            for( int i = 0; i < attachs.Length; i++ )
            {
                string path = attachs[i] as string;
                mfd.name = Path.GetFileName( path );
                mfd.path = path;
                Marshal.StructureToPtr( mfd, (IntPtr) runptr, false );
                runptr += asize;
            }

            return ptra;
        }

        private const int MapiTO	= 1;
        private const int MAPI_DIALOG = 0x8;

        private const int MAPI_E_USER_ABORT               = 1;
        //private const int MAPI_E_FAILURE                  = 2;
        private const int MAPI_E_LOGON_FAILURE            = 3;
        //private const int MAPI_E_DISK_FULL                = 4;
        private const int MAPI_E_INSUFFICIENT_MEMORY      = 5;
        //private const int MAPI_E_ACCESS_DENIED            = 6;
        //private const int MAPI_E_TOO_MANY_SESSIONS        = 8;
        private const int MAPI_E_TOO_MANY_FILES           = 9;
        private const int MAPI_E_TOO_MANY_RECIPIENTS      = 10;
        private const int MAPI_E_ATTACHMENT_NOT_FOUND     = 11;
        private const int MAPI_E_ATTACHMENT_OPEN_FAILURE  = 12;
        //private const int MAPI_E_ATTACHMENT_WRITE_FAILURE = 13;
        private const int MAPI_E_UNKNOWN_RECIPIENT        = 14;
        //private const int MAPI_E_BAD_RECIPTYPE            = 15;
        //private const int MAPI_E_NO_MESSAGES              = 16;
        //private const int MAPI_E_INVALID_MESSAGE          = 17;
        private const int MAPI_E_TEXT_TOO_LARGE           = 18;
        //private const int MAPI_E_INVALID_SESSION          = 19;
        //private const int MAPI_E_TYPE_NOT_SUPPORTED       = 20;
        private const int MAPI_E_AMBIGUOUS_RECIPIENT      = 21;
        //private const int MAPI_E_MESSAGE_IN_USE           = 22;
        //private const int MAPI_E_NETWORK_FAILURE          = 23;
        //private const int MAPI_E_INVALID_EDITFIELDS       = 24;
        private const int MAPI_E_INVALID_RECIPS           = 25;
        //private const int MAPI_E_NOT_SUPPORTED            = 26;

        private void ReportError( int rc )
        {
            string msg;
            switch( rc )
            {
                case MAPI_E_AMBIGUOUS_RECIPIENT:
                    msg = "A recipient matched more than one of the recipient descriptor structures.";
                    break;

                case MAPI_E_ATTACHMENT_NOT_FOUND:
                    msg = "The specified attachment was not found.";
                    break;

                case MAPI_E_ATTACHMENT_OPEN_FAILURE:
                    msg = "The specified attachment could not be opened.";
                    break;

                case MAPI_E_INSUFFICIENT_MEMORY:
                    msg = "There was insufficient memory to proceed.";
                    break;

                case MAPI_E_INVALID_RECIPS:
                    msg = "One or more recipients were invalid or did not resolve to any address.";
                    break;

                case MAPI_E_LOGON_FAILURE:
                    msg = "Logon failed.";
                    break;

                case MAPI_E_TEXT_TOO_LARGE:
                    msg = "The text in the message was too large.";
                    break;

                case MAPI_E_TOO_MANY_FILES:
                    msg = "There were too many file attachments.";
                    break;

                case MAPI_E_TOO_MANY_RECIPIENTS:
                    msg = "There were too many recipients.";
                    break;

                case MAPI_E_UNKNOWN_RECIPIENT:
                    msg = "A recipient did not appear in the address list.";
                    break;

                default:
                    msg = "Unknown error " + rc;
                    break;
            }

            MessageBox.Show( Core.MainWindow,
                "Failed to send e-mail: " + msg, "Send E-mail", MessageBoxButtons.OK );
        }

        [StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi )]
        private class MapiMessage
        {
            public int		reserved = 0;
            public string	subject = null;
            public string	noteText = null;
            public string	messageType = null;
            public string	dateReceived = null;
            public string	conversationID = null;
            public int		flags = 0;
            public IntPtr	originator = IntPtr.Zero;		// MapiRecipDesc* [1]
            public int		recipCount = 0;
            public IntPtr	recips = IntPtr.Zero;			// MapiRecipDesc* [n]
            public int		fileCount = 0;
            public IntPtr	files = IntPtr.Zero;			// MapiFileDesc*  [n]
        }

        [StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi )]
        private class MapiRecipDesc
        {
            public int		reserved = 0;
            public int		recipClass = 0;
            public string	name = null;
            public string	address = null;
            public int		eIDSize = 0;
            public IntPtr	entryID = IntPtr.Zero;			// void*
        }

        [StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi )]
        private class MapiFileDesc
        {
            public int		reserved = 0;
            public int		flags = 0;
            public int		position = 0;
            public string	path = null;
            public string	name = null;
            public IntPtr	type = IntPtr.Zero;
        }

        [DllImport( "MAPI32.DLL")]
        private static extern int MAPISendMail(	IntPtr sess, IntPtr hwnd,
            MapiMessage message,
            int flg, int rsv );
    }
}
