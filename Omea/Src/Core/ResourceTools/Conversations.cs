/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.IO;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.HTML;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Conversations
{
    public class IMConversationsManager: IResourceTextProvider
    {
        #region Attributes

        private const string _Style = "<style>" +
                                      " body, td { font-family: verdana; font-size: 10pt; } " +
                                      ".incoming { background-color: #f0f0f0; color: #800080; } " +
                                      ".outgoing { background-color: #e0e0e0; color: #800000; } " +
                                      "</style>";

        private readonly IResourceStore _store;
        private readonly string _conversationResType;
        private readonly int _propAccountLink;
        private readonly int _propFromAccount;
        private readonly int _propToAccount;
        private readonly int _propDate;
        private readonly int _propStartDate;
        private readonly int _propFrom;
        private readonly int _propTo;
        private readonly int _propConversationList;
        private readonly int _propSubject;
        private readonly int _propMySelf;

        private TimeSpan _period;
        private bool _reverseMode;

        #endregion Attributes

        public IMConversationsManager( string resType, string resourceTypeDisplayName, string displayNameMask,
                                       TimeSpan period,
                                       int propAccountLink, int propFromAccount, int propToAccount,
                                       IPlugin ownerPlugin )
        {
            _store = Core.ResourceStore;
            _propDate = _store.PropTypes.Register( "Date", PropDataType.Date );
            _propStartDate = ResourceTypeHelper.UpdatePropTypeRegistration( "StartDate", PropDataType.Date, PropTypeFlags.Normal );
            _store.PropTypes.RegisterDisplayName( _propStartDate, "Start Date" );

            _propFrom = ResourceTypeHelper.UpdatePropTypeRegistration( "From", PropDataType.Link, PropTypeFlags.DirectedLink );
            _propTo = ResourceTypeHelper.UpdatePropTypeRegistration( "To", PropDataType.Link, PropTypeFlags.DirectedLink );

            _propConversationList = ResourceTypeHelper.UpdatePropTypeRegistration( "ConversationList", PropDataType.StringList, PropTypeFlags.Internal );
            _propSubject = _store.PropTypes.Register( "Subject", PropDataType.String );
            _propMySelf = _store.PropTypes.Register( "MySelf", PropDataType.Int, PropTypeFlags.Internal );

            _conversationResType = resType;
            _store.ResourceTypes.Register( resType, resourceTypeDisplayName, displayNameMask, ResourceTypeFlags.Normal, ownerPlugin );
            Core.FilterManager.RegisterRuleApplicableResourceType( resType );
            _store.RegisterLinkRestriction( resType, _propFrom, null, 1, 1 );
            _store.RegisterLinkRestriction( resType, _propTo, null, 1, 1 );
            _store.RegisterLinkRestriction( resType, propFromAccount, null, 1, 1 );
            _store.RegisterLinkRestriction( resType, propToAccount, null, 1, 1 );
            Core.MessageFormatter.RegisterPreviewTextProvider( resType, new ConversationPreviewBuilder( this ) );
            _period = period;
            _reverseMode = false;
            _propAccountLink = propAccountLink;
            _propFromAccount = propFromAccount;
            _propToAccount = propToAccount;
        }

        public TimeSpan ConversationPeriod
        {
            get { return _period; }
            set { _period = value; }
        }

        public bool ReverseMode
        {
            get { return _reverseMode; }
            set { _reverseMode = value; }
        }

        #region Update Conversation
        /** 
         * updates existing or creates the new conversation resource
         * texts should be updated in chronological order (ascending sorting by date)
         */
        public IResource Update( string text,
                                 DateTime date,
                                 IResource fromAccount,
                                 IResource toAccount )
        {
            IResource from = fromAccount.GetLinkProp( _propAccountLink );
            if( from == null )
            {
                throw new Exception( "'From' account is not linked with a contact" );
            }
            IResource to = toAccount.GetLinkProp( _propAccountLink );
            if( to == null )
            {
                throw new Exception( "'To' account is not linked with a contact" );
            }

            IResourceList lastConversations =
                _store.FindResourcesInRange( _conversationResType, _propDate, date - _period, date );

            IResource convs;
            for( int i = lastConversations.Count - 1; i >= 0; --i )
            {
                convs = lastConversations[ i ];
                IResource convsFrom = convs.GetLinkProp( _propFrom );
                IResource convsTo = convs.GetLinkProp( _propTo );
                if( ( from == convsFrom && to == convsTo ) || ( from == convsTo && to == convsFrom ) )
                {
                    convs.BeginUpdate();
                    try
                    {
                        UpdateConversationDate( convs, date, from, to, from == convsTo );
                        AddFragment( convs, text, date, from == convsFrom );
                        string subject = convs.GetPropText( _propSubject );
                        if( subject.Length <= 10 && subject.Split( ' ' ).Length < 2 )
                        {
                            subject = text.Trim().Replace( '\n', ' ' ).Replace( '\r', ' ');
                            if( subject.Length > 64 )
                            {
                                subject = subject.Remove( 61, subject.Length - 61 );
                                subject = subject + "...";
                            }
                            if( subject.Length > 10 || subject.Split( ' ' ).Length > 1 )
                            {
                                UpdateConversationSubject( convs, subject );
                            }
                        }
                    }
                    finally
                    {
                        convs.EndUpdate();
                    }
                    return convs;
                }
            }

            // if not found then create new conversation
            convs = _store.BeginNewResource( _conversationResType );
            try
            {
                convs.AddLink( _propFrom, from );
                convs.AddLink( _propTo, to );
                convs.AddLink( _propFromAccount, fromAccount );
                convs.AddLink( _propToAccount, toAccount );
                convs.SetProp( _propStartDate, date );
                UpdateConversationDate( convs, date, from, to, false );
                AddFragment( convs, text, date, true );
                string subject = text.Trim().Replace( '\n', ' ').Replace( '\r', ' ');
                if( subject.Length > 64 )
                {
                    subject = subject.Remove( 61, subject.Length - 61 );
                    subject = subject + "...";
                }
                UpdateConversationSubject( convs, subject );
            }
            finally
            {
                convs.EndUpdate();
            }
            return convs;
        }
        #endregion Update Conversation

        #region Convert To HTML
        /** 
         * creates html representation for a given conversation resource &
         * string-type property which is used to display accounts
         */
        public string ToHtmlString( IResourceList convs, int propDisplayName )
        {
            StringBuilder htmlBuilder = StringBuilderPool.Alloc();
            try 
            {
                ToHtmlHead( htmlBuilder );
                for( int i = 0; i < convs.Count; i++ )
                {
                    ToHtmlBody( convs[ i ], propDisplayName, htmlBuilder );
                    if( i != convs.Count - 1 )
                        htmlBuilder.Append( "<br><br>" );
                }
                ToHtmlEnd( htmlBuilder );
                return htmlBuilder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( htmlBuilder );
            }
        }

        public string ToHtmlString( IResource convs, int propDisplayName )
        {
            StringBuilder htmlBuilder = StringBuilderPool.Alloc();
            try 
            {
                ToHtmlHead( htmlBuilder );
                ToHtmlBody( convs, propDisplayName, htmlBuilder );
                ToHtmlEnd( htmlBuilder );
                Trace.WriteLine( htmlBuilder.ToString() );
                return htmlBuilder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( htmlBuilder );
            }
        }

        private static void ToHtmlHead( StringBuilder htmlBuilder )
        {
            htmlBuilder.Append( "<html><head>" ).Append( _Style ).Append( "</head><body>\n" );
        }

        private static void ToHtmlEnd( StringBuilder htmlBuilder )
        {
            htmlBuilder.Append("</body></html>");
        }

        private void  ToHtmlBody( IResource convs, int propDisplayName, StringBuilder htmlBuilder )
        {
            string cachedFrom = string.Empty;
            string fromName, toName;
            ExtractNames( convs, propDisplayName, out fromName, out toName );
            htmlBuilder.Append("<table width=100% border=0 cellpadding=4 cellspacing=0>");

            DateTime lastMsgDate = convs.GetDateProp( _propDate );
            XmlTextReader reader = new XmlTextReader( new StringReader( ToString( convs ) ) );
            while( reader.Read() )
            {
                if ( reader.NodeType == XmlNodeType.Element && reader.LocalName == "fragment" )
                {
                    if( reader.MoveToAttribute( "date" ) )
                    {
                        DateTime newMsgDate = new DateTime( Convert.ToInt64( reader.Value ) );
                        if( newMsgDate.Day != lastMsgDate.Day )
                        {
                            ToHtmlDate( newMsgDate, htmlBuilder );
                        }
                        lastMsgDate = newMsgDate;
                    }
                    bool incoming = reader.MoveToAttribute( "incoming" ) && reader.Value.ToLower() == "true";

                    ToHtmlLeadingCols( incoming, fromName, toName, lastMsgDate, ref cachedFrom, htmlBuilder );

                    if( reader.MoveToAttribute( "body" ) )
                    {
                        ToHtmlPhrase( reader.ReadInnerXml(), htmlBuilder );
                    }
                    htmlBuilder.Append("</tr>");
                    reader.MoveToElement();
                }
            }
            htmlBuilder.Append("</table>");
        }

        private static void ToHtmlDate( DateTime date, StringBuilder htmlBuilder )
        {
            htmlBuilder.Append("<tr><td nowrap width=100% colspan=3><br><b>");
            htmlBuilder.Append( date.ToLongDateString());
            htmlBuilder.Append("<br>&nbsp;</b></td></tr>");
        }

        private static void ToHtmlLeadingCols( bool incoming, string fromName, string toName, DateTime date,
                                               ref string cachedName, StringBuilder htmlBuilder )
        {
            string name = ( incoming ) ? fromName : toName;
            if( name == cachedName )
                name = "&nbsp;";
            else
                cachedName = name;

            htmlBuilder.Append("<tr class=\"");
            htmlBuilder.Append( ( incoming ) ? "incoming\">" : "outgoing\">" );
            htmlBuilder.Append("<td nowrap valign=top><b>");
            htmlBuilder.Append( name );
            htmlBuilder.Append("</b></td>");
            htmlBuilder.Append("<td nowrap valign=top>");
            htmlBuilder.Append( date.ToLongTimeString() );
            htmlBuilder.Append("&nbsp;&nbsp;</td>");
        }

        private static void ToHtmlPhrase( string body, StringBuilder htmlBuilder )
        {
            htmlBuilder.Append("<td width=100% valign=top>");
            // convert all urls to active weblinks
            htmlBuilder.Append( HtmlTools.ConvertLinks( body.Replace( "\n", "<br>" ) ) );
            htmlBuilder.Append("</td>");
        }

        private void  ExtractNames( IResource convs, int propNameId,
                                    out string fromName, out string toName )
        {
        	IResource fromAcct = convs.GetLinkProp( _propFromAccount );
        	fromName = fromAcct.GetPropText( propNameId );
            if ( fromName.Length == 0 )
            {
            	fromName = fromAcct.DisplayName;
            }

        	IResource toAcct = convs.GetLinkProp( _propToAccount );
        	toName = toAcct.GetPropText( propNameId );
            if ( toName.Length == 0 )
            {
            	toName = toAcct.DisplayName;
            }
        }
        #endregion Convert To HTML

        public bool ProcessResourceText( IResource convs, IResourceTextConsumer consumer )
        {
            // index conversation as sequence of fragments, each one is a message
            try
            {
                XmlTextReader reader = new XmlTextReader( new StringReader( ToString( convs ) ) );
                while( reader.Read() )
                {
                    if ( reader.NodeType == XmlNodeType.Element )
                    {
                        if( reader.MoveToAttribute( "body" ) )
                        {
                            consumer.AddDocumentFragment( convs.Id, reader.Value );
                        }
                        reader.MoveToElement();
                    }
                }
            }
            catch( XmlException )
            {
                // nothing to do with this :(
            }
            return true;
        }

        public string ToString( int convsID )
        {
            return ToString( _store.LoadResource( convsID ) );
        }

        public string ToString( IResource convs )
        {
            StringBuilder bodyBuilder = StringBuilderPool.Alloc();
            try 
            {
                bodyBuilder.Append( "<conversation>" );
                IStringList convsList = convs.GetStringListProp( _propConversationList );
                if( !_reverseMode )
                {
                    for( int i = 0; i < convsList.Count; ++i )
                    {
                        bodyBuilder.Append( convsList[ i ] );
                    }
                }
                else
                {
                    for( int i = convsList.Count - 1; i >= 0; --i )
                    {
                        bodyBuilder.Append( convsList[ i ] );
                    }
                }
                bodyBuilder.Append( "</conversation>" );
                convsList.Dispose();
                return bodyBuilder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( bodyBuilder );
            }
        }

        private void UpdateConversationDate( IResource convs, DateTime date,
                                             IResource from, IResource to, bool updateLastCorrespondDate )
        {
            convs.SetProp( _propDate, date );

            if( updateLastCorrespondDate )
            {
                IResource correspondent = ( from.GetIntProp( _propMySelf ) == 0 ) ? from : to;
                if( correspondent.GetDateProp( Core.ContactManager.Props.LastCorrespondenceDate ) < date )
                {
                    correspondent.SetProp( Core.ContactManager.Props.LastCorrespondenceDate, date );
                }
            }
        }

        private void UpdateConversationSubject( IResource convs, string subject )
        {
            try
            {
                convs.SetProp( _propSubject, subject );
            }
            catch( ArgumentException )
            {
                // ignore garbage
            }
        }

        private void AddFragment( IResource convs, string text, DateTime date, bool incoming )
        {
            StringBuilder bodyBuilder = StringBuilderPool.Alloc();
            try 
            {
                XmlTextWriter xmlWriter = new XmlTextWriter( new StringWriter( bodyBuilder ) );
                try
                {
                    xmlWriter.WriteStartElement( "fragment" );
                    xmlWriter.WriteAttributeString( "body", text );
                    xmlWriter.WriteAttributeString( "date", date.Ticks.ToString() );
                    xmlWriter.WriteAttributeString( "incoming", incoming.ToString() );
                    xmlWriter.WriteEndElement();
                    xmlWriter.Flush();
                    convs.GetStringListProp( _propConversationList ).Add( bodyBuilder.ToString() );
                }
                catch( ArgumentException )
                {
                    // ignore garbage
                }
                finally
                {
                    xmlWriter.Close();
                }
            }
            finally
            {
                StringBuilderPool.Dispose( bodyBuilder );
            }
        }

        private class ConversationPreviewBuilder: IPreviewTextProvider
        {
            private readonly IMConversationsManager _manager;

            public ConversationPreviewBuilder( IMConversationsManager manager )
            {
                _manager = manager;
            }

            public string GetPreviewText( IResource res, int lines )
            {
                StringBuilder builder = StringBuilderPool.Alloc();
                try 
                {
                    builder.Append( "<conversation>" );
                    IStringList convsList = res.GetStringListProp( _manager._propConversationList );
                    if( !_manager._reverseMode )
                    {
                        for( int i = 0; i < convsList.Count && lines-- > 0; ++i )
                        {
                            builder.Append( convsList[ i ] );
                        }
                    }
                    else
                    {
                        for( int i = convsList.Count - 1; i >= 0 && lines-- > 0; --i )
                        {
                            builder.Append( convsList[ i ] );
                        }
                    }
                    convsList.Dispose();
                    builder.Append( "</conversation>" );
                    XmlTextReader reader = new XmlTextReader( new StringReader( builder.ToString() ) );
                    builder.Length = 0;
                    while( reader.Read() )
                    {
                        if ( reader.NodeType == XmlNodeType.Element )
                        {
                            if( reader.MoveToAttribute( "body" ) )
                            {
                                builder.AppendFormat( "{0}\r\n", reader.Value );
                            }
                            reader.MoveToElement();
                        }
                    }
                    return builder.ToString();
                }
                finally
                {
                    StringBuilderPool.Dispose( builder );
                }
            }
        }
    }
}
