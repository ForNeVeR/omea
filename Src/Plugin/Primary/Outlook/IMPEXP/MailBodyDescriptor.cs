// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using EMAPILib;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.RTF;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class MailBodyDescriptor
    {
        private string _subject = string.Empty;
        private string _body = string.Empty;
        private bool _isHTML = false;
        private RTFParser _rtfParser = new RTFParser();

        public MailBodyDescriptor( IResource mail )
        {
            try
            {
                IEMessage message = null;
                if ( mail.Type == STR.Email )
                {
                    PairIDs IDs = PairIDs.Get( mail );
                    if ( IDs != null )
                    {
                        message = OutlookSession.OpenMessage( IDs.EntryId, IDs.StoreId );
                    }
                    else
                    {
                        message = new OutlookAttachment( mail ).OpenEmbeddedMessage();
                    }
                }
                else if ( mail.Type == STR.EmailFile )
                {
                    message = OutlookSession.OpenEmailFile( mail );
                }
                if ( message != null )
                {
                    using ( message )
                    {
                        _subject = message.GetStringProp( MAPIConst.PR_SUBJECT );
                        MessageBody body = message.GetRawBodyAsRTF();

                        if ( body.Format == MailBodyFormat.PlainTextInRTF || body.Format == MailBodyFormat.RTF )
                        {
                            _body = _rtfParser.Parse( body.text );
                        }
                        else
                        {
                            _body = body.text;
                        }

                        _isHTML = ( body.Format == EMAPILib.MailBodyFormat.HTML );
                    }
                }
            }
            catch ( Exception exception )
            {
                Tracer._TraceException( exception );
            }
            if ( _subject == null ) _subject = string.Empty;
            if ( _body == null ) _body = string.Empty;
        }
        public bool IsHTML { get { return _isHTML; } }
        public string Subject { get { return _subject; } }
        public string Body { get { return _body; } }
    }

}
