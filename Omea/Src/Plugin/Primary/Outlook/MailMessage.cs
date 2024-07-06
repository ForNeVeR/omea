// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.RTF;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class MailMessage
    {
        private static MailMessage _mailMessageNull = null;
        private static RTFParser _rtfParser = new RTFParser();
        private const string _errorText =
            "The mail message could not be displayed. The \"StoreID\" or \"EntryID\" properties were not set.";	// TODO: form the explicit HTML text for the error message so that it did not look like an ordinary email message (ask Serge)
        static MailMessage()
        {
            _mailMessageNull = new MailMessage();
        }
        private MailMessage()
        {
        }
        public static MailMessage Get( IResource res )
        {
            PairIDs IDs = PairIDs.Get( res );
            if ( IDs != null || res.HasProp( PROP.EmbeddedMessage ) || res.HasProp( "FileType" ) )
            {
                return new MailMessageLoaded( res );
            }
            return _mailMessageNull;
        }

        public virtual BodyType BodyType { get { return BodyType.PlainText; } }	// TODO: return HTML value here
        public virtual bool HasPictures { get { return false; } }
        public virtual string Body { get { return _errorText; } }	// TODO: return the HTML version of the error text; maybe even remove the _errorText property and just state this text inplace
        public virtual string Subject { get { return null; } }

        private class MailMessageLoaded : MailMessage
        {
            #region Class members
            private string _body = null;
            private string _subject = null;
            IResource _mail;
            private BodyType _bodyType;
            private bool _hasPictures = false;
            #endregion

            public MailMessageLoaded( IResource mail )
            {
                _mail = mail;
            }

            #region Class properties

            public override BodyType BodyType
            {
                get
                {
                    LoadBody();
                    return _bodyType;
                }
            }
            private void LoadBody()
            {
                if ( !Settings.ProcessLoadBody )
                {
                    SetBodyAndType( "Body is not shown for test purposes. To see body set ProcessLoadBody=1 in MailIndexing section in .ini file", BodyType.PlainText );
                    return;
                }

                if ( _body != null ) return;
                try
                {
                    OutlookSession.OutlookProcessor.RunJob( "Load message body", new MethodInvoker( LoadBodyImpl ) );
                }
                catch( Exception ex )
                {
                    SetBodyAndType( ex.Message, BodyType.PlainText );
                }
            }

            private void LoadBodyImpl()
            {
                Tracer._Trace( "MailMessage: Prepare to load body" );
                try
                {
                    EMAPILib.MessageBody mesBody = ReadBody();
                    if ( mesBody != null )
                    {
                        string text = mesBody.text.TrimEnd( (char)0x00 );
                        if ( mesBody.Format == EMAPILib.MailBodyFormat.PlainText  )
                        {
                            SetBodyAndType( text, BodyType.PlainText );
                        }
                        else if ( mesBody.Format == EMAPILib.MailBodyFormat.PlainTextInRTF  )
                        {
                            SetBodyAndType( _rtfParser.Parse( text ), BodyType.PlainText );
                        }
                        else if ( mesBody.Format == EMAPILib.MailBodyFormat.HTML )
                        {
                            SetBodyAndType( text, BodyType.HTML );
                        }
                        else
                        {
                            SetBodyAndType( text, BodyType.RTF );
                        }
                        return;
                    }
                    SetBodyAndType( "", BodyType.RTF );
                }
                catch ( Exception exception )
                {
                    Tracer._TraceException( exception );
                    string body = "Impossible to show body for this mail.\nError: " + exception.Message;
                    SetBodyAndType( body, BodyType.PlainText );
                }
            }

            private MessageBody ReadBody()
            {
                if ( _mail.HasProp( PROP.EmbeddedMessage ) )
                {
                    OutlookAttachment attach = new OutlookAttachment( _mail );
                    EMAPILib.IEMessage message = attach.OpenEmbeddedMessage();
                    if ( message != null )
                    {
                        using ( message )
                        {
                            return message.GetRawBodyAsRTF();
                        }
                    }
                    return null;
                }
                if ( _mail.Type == STR.EmailFile )
                {
                    IEMessage message = OutlookSession.OpenEmailFile( _mail );
                    if ( message != null )
                    {
                        using ( message )
                        {
                            _subject = message.GetStringProp( MAPIConst.PR_SUBJECT );
                            return message.GetRawBodyAsRTF();
                        }
                    }
                    return null;
                }

                PairIDs IDs = PairIDs.Get( _mail );
                if ( IDs != null )
                {
                    EMAPILib.IEMessage message = OutlookSession.OpenMessage( IDs.EntryId, IDs.StoreId );
                    if ( message != null )
                    {
                        using ( message )
                        {
                            return OutlookSession.GetMessageBody( message );
                        }
                    }
                    else
                    {
                        string bodyText = "Could not open message. It may have been moved or deleted in Outlook. Or message store for this mail cannot be open.";
                        return new MessageBody( bodyText, MailBodyFormat.PlainText, 0 );
                    }
                }
                return null;
            }

            private void SetBodyAndType( string body, BodyType type )
            {
                _bodyType = type;
                _body = body;
                if ( _bodyType == BodyType.HTML )
                {
                    _hasPictures = ( _body.ToLower().IndexOf( "<img" ) != -1 );
                    QueueResourceJob( _mail, "HTML" );
                }
                else if ( _bodyType == BodyType.RTF )
                {
                    QueueResourceJob( _mail, "RTF" );
                }
                else if ( _bodyType == BodyType.PlainText )
                {
                    QueueResourceJob( _mail, "PlainText" );
                }
            }

            private void QueueResourceJob( IResource mail, string format )
            {
                DelegateSetBodyFormat delegateSetBodyFormat =
                    new DelegateSetBodyFormat( SetBodyFormat );
                Core.ResourceAP.QueueJob( JobPriority.Immediate,
                    delegateSetBodyFormat, mail, format );
            }
            private delegate void DelegateSetBodyFormat( IResource mail, string format );
            void SetBodyFormat( IResource mail, string format )
            {
                if ( Guard.IsResourceLive( mail ) )
                {
                    mail.SetProp( PROP.BodyFormat, format );
                }
            }

            public override bool HasPictures
            {
                get
                {
                    LoadBody();
                    return _hasPictures;
                }
            }

            public override string Body
            {
                get
                {
                    LoadBody();
                    return _body;
                }
            }
            public override string Subject
            {
                get
                {
                    LoadBody();
                    return _subject;
                }
            }
            #endregion
        }
    }
    internal enum BodyType
    {
        RTF,
        HTML,
        PlainText
    };
}
