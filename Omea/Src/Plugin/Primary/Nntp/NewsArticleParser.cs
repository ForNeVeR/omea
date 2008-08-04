/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Text;
using JetBrains.DataStructures;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;
using JetBrains.Omea.MIME;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Nntp
{
    /** 
      * processing article: parsing headers, parsing multi-part bodies,
      *                     creating attachments, etc
      */

    internal delegate void CreateArticleDelegate( string[] lines, IResource groupRes, string articleId );
    internal delegate void CreateArticleByProtocolHandlerDelegate( string[] lines, IResource article );
    internal delegate void CreateArticleFromHeadersDelegate( string line, IResource groupRes  );

    internal class NewsArticleParser
    {
        /** 
         * all article's processing should be performed in the resource thread
         * this function is called if article is downloaded from a group
         */
        public static void CreateArticle( string[] lines, IResource groupRes, string articleId )
        {
            // if user has already unsubscribed from the group or the
            // groups is already deleted then skip the article
            IResource server = new NewsgroupResource( groupRes ).Server;
            if( server == null )
            {
                return;
            }
            
            articleId = ParseTools.EscapeCaseSensitiveString( articleId );
            // is article deleted?
            if( NewsArticleHelper.IsArticleDeleted( articleId ) )
            {
                return;
            }

            PrepareLines( lines );

            try
            {
                ServerResource serverRes = new ServerResource( server );
                string charset = serverRes.Charset;
                bool bodyStarted = false;
                string line;
                string content_type = string.Empty;
                string content_transfer_encoding = string.Empty;
                IContact sender = null;
                DateTime date = DateTime.MinValue;
                bool mySelf = false;
                bool newArticle;

                IResource article = Core.ResourceStore.FindUniqueResource(
                    NntpPlugin._newsArticle, NntpPlugin._propArticleId, articleId );
                if( article != null )
                {
                    if( !article.HasProp( NntpPlugin._propHasNoBody ) )
                    {
                        return;
                    }
                    article.BeginUpdate();
                    newArticle = false;
                }
                else
                {
                    article = Core.ResourceStore.BeginNewResource( NntpPlugin._newsArticle );
                    newArticle = true;
                }

                for( int i = 0; i < lines.Length; ++i )
                {
                    line = lines[ i ];
                    if( line == null )
                    {
                        continue;
                    }
                    if( bodyStarted )
                    {
                        _bodyBuilder.Append( line );
                    }
                    else
                    {
                        _headersBuilder.Append( line );
                        _headersBuilder.Append( "\r\n" );
                        if( Utils.StartsWith( line, "from: ", true ) )
                        {
                            string from = line.Substring( 6 );
                            article.SetProp( NntpPlugin._propRawFrom, from );
                            mySelf = ParseFrom( article, TranslateHeader( charset, from ), out sender );
                            UpdateLastCorrespondDate( sender, date );
                        }
                        else if( Utils.StartsWith( line, "subject: ", true ) )
                        {
                            string subject = line.Substring( 9 );
                            article.SetProp( NntpPlugin._propRawSubject, subject );
                            article.SetProp( Core.Props.Subject, TranslateHeader( charset, subject ) );
                        }
                        else if( Utils.StartsWith( line, "message-id: ", true ) )
                        {
                            ParseMessageId( article, ParseTools.EscapeCaseSensitiveString( line.Substring( 12 ) ), articleId );
                        }
                        else if( Utils.StartsWith( line, "newsgroups: ", true ) )
                        {
                            if( line.IndexOf( ',' ) > 12 ) 
                            {
                                ParseNewsgroups( article, groupRes, serverRes, line.Substring( 12 ) );
                            }
                        }
                        else if( Utils.StartsWith( line, "date: ", true ) )
                        {
                            date = ParseDate( article, line.Substring( 6 ) );
                            UpdateLastCorrespondDate( sender, date );
                        }
                        else if( Utils.StartsWith( line, "references: ", true ) )
                        {
                            ParseReferences( article, line.Substring( 12 ) );
                        }
                        else if( Utils.StartsWith( line, "content-type: ", true ) )
                        {
                            content_type = line.Substring( 14 );
                        }
                        else if( Utils.StartsWith( line, "followup-to: ", true ) )
                        {
                            article.SetProp( NntpPlugin._propFollowupTo, line.Substring( 13 ) );
                        }
                        else if( Utils.StartsWith( line, "content-transfer-encoding: ", true ) )
                        {
                            content_transfer_encoding = line.Substring( 27 );
                        }
                        else if( line == "\r\n" )
                        {
                            bodyStarted = true;
                        }
                    }
                }
                ProcessBody( _bodyBuilder.ToString(), content_type, content_transfer_encoding, article, charset );
                article.SetProp( NntpPlugin._propArticleHeaders, _headersBuilder.ToString() );
                article.AddLink( NntpPlugin._propTo, groupRes );
                if( article.GetPropText( NntpPlugin._propArticleId ).Length == 0 )
                {
                    article.SetProp( NntpPlugin._propArticleId, articleId );
                }
                if( newArticle )
                {
                    article.SetProp( NntpPlugin._propIsUnread, true );
                    IResourceList categories = Core.CategoryManager.GetResourceCategories( groupRes );
                    foreach( IResource category in categories )
                    {
                        Core.CategoryManager.AddResourceCategory( article, category );
                    }

                    Core.FilterEngine.ExecRules( StandardEvents.ResourceReceived, article );
                    CleanLocalArticle( articleId, article );
                }
                article.EndUpdate();

                CheckArticleInIgnoredThreads( article );
                CheckArticleInSelfThread( article );

                if( mySelf && serverRes.MarkFromMeAsRead )
                {
                    article.BeginUpdate();
                    article.DeleteProp( NntpPlugin._propIsUnread );
                    article.EndUpdate();
                }
                Core.TextIndexManager.QueryIndexing( article.Id );
            }
            finally
            {
                DisposeStringBuilders();
            }
        }

        /**
         * this function is called if article is downloaded from a group
         * article should be a newly created transient resource
         */
        public static void CreateArticleByProtocolHandler( string[] lines, IResource article )
        {
            if( !article.IsTransient )
            {
                throw new ArgumentException( "Article should be a newly created transient resource", "article" );
            }

            string articleId = article.GetPropText( NntpPlugin._propArticleId );
            if( Core.ResourceStore.FindUniqueResource(
                NntpPlugin._newsArticle, NntpPlugin._propArticleId, articleId ) != null )
            {
                return;
            }

            PrepareLines( lines );

            try
            {
                IResource server = article.GetLinkProp( NntpPlugin._propTo );
                string charset = new ServerResource( server ).Charset;
                bool bodyStarted = false;
                string line;
                string content_type = string.Empty;
                string content_transfer_encoding = string.Empty;
                IContact sender = null;
                DateTime date = DateTime.MinValue;
                bool mySelf = false;

                for( int i = 0; i < lines.Length; ++i )
                {
                    line = lines[ i ];
                    if( line == null )
                    {
                        continue;
                    }
                    if( bodyStarted )
                    {
                        _bodyBuilder.Append( line );
                    }
                    else
                    {
                        _headersBuilder.Append( line );
                        _headersBuilder.Append( "\r\n" );
                        if( Utils.StartsWith( line, "from: ", true ) )
                        {
                            string from = line.Substring( 6 );
                            article.SetProp( NntpPlugin._propRawFrom, from );
                            mySelf = ParseFrom( article, TranslateHeader( charset, from ), out sender );
                            UpdateLastCorrespondDate( sender, date );
                        }
                        else if( Utils.StartsWith( line, "subject: ", true ) )
                        {
                            string subject = line.Substring( 9 );
                            article.SetProp( NntpPlugin._propRawSubject, subject );
                            article.SetProp( Core.Props.Subject, TranslateHeader( charset, subject ) );
                        }
                        else if( Utils.StartsWith( line, "message-id: ", true ) )
                        {
                            ParseMessageId( article, ParseTools.EscapeCaseSensitiveString( line.Substring( 12 ) ), articleId );
                        }
                        else if( Utils.StartsWith( line, "newsgroups: ", true ) )
                        {
                            Subscribe2Groups( article, line.Substring( 12 ) );
                        }
                        else if( Utils.StartsWith( line, "date: ", true ) )
                        {
                            date = ParseDate( article, line.Substring( 6 ) );
                            UpdateLastCorrespondDate( sender, date );
                        }
                        else if( Utils.StartsWith( line, "references: ", true ) )
                        {
                            ParseReferences( article, line.Substring( 12 ) );
                        }
                        else if( Utils.StartsWith( line, "content-type: ", true ) )
                        {
                            content_type = line.Substring( 14 );
                        }
                        else if( Utils.StartsWith( line, "followup-to: ", true ) )
                        {
                            article.SetProp( NntpPlugin._propFollowupTo, line.Substring( 13 ) );
                        }
                        else if( Utils.StartsWith( line, "content-transfer-encoding: ", true ) )
                        {
                            content_transfer_encoding = line.Substring( 27 );
                        }
                        else if( line == "\r\n" )
                        {
                            bodyStarted = true;
                        }
                    }
                }
                ProcessBody( _bodyBuilder.ToString(), content_type, content_transfer_encoding, article, charset );
                article.SetProp( NntpPlugin._propArticleHeaders, _headersBuilder.ToString() );
                article.SetProp( NntpPlugin._propIsUnread, true );
                Core.FilterEngine.ExecRules( StandardEvents.ResourceReceived, article );
                CleanLocalArticle( articleId, article );
                article.EndUpdate();
                if( mySelf && new ServerResource( server ).MarkFromMeAsRead )
                {
                    article.BeginUpdate();
                    article.DeleteProp( NntpPlugin._propIsUnread );
                    article.EndUpdate();
                }
                Core.TextIndexManager.QueryIndexing( article.Id );
            }
            finally
            {
                DisposeStringBuilders();
            }
        }

        public static void CreateArticleFromHeaders( string line, IResource groupRes )
        {
            string[] headers = line.Split( '\t' );
            if( headers.Length <= 5 )
            {
                return;
            }
            string articleId = ParseTools.EscapeCaseSensitiveString( headers[ 4 ] );
            if( articleId.Length == 0 || NewsArticleHelper.IsArticleDeleted( articleId ) )
            {
                return;
            }
            // if user has already unsubscribed from the group or the
            // groups is already deleted then skip the article
            IResource server = new NewsgroupResource( groupRes ).Server;
            if( server == null )
            {
                return;
            }

            // is article deleted?
            if( NewsArticleHelper.IsArticleDeleted( articleId ) )
            {
                return;
            }

            IResource article;
            IContact sender;
            bool mySelf;
            bool newArticle;
            string charset = new ServerResource( server ).Charset;

            article = Core.ResourceStore.FindUniqueResource(
                NntpPlugin._newsArticle, NntpPlugin._propArticleId, articleId );
            if( article != null )
            {
                article.BeginUpdate();
                newArticle = false;
            }
            else
            {
                article = Core.ResourceStore.BeginNewResource( NntpPlugin._newsArticle );
                newArticle = true;
            }
            article.SetProp( NntpPlugin._propHasNoBody, true );
            NewsArticleHelper.SetArticleNumber( article, groupRes, headers[ 0 ] );
            DateTime date = ParseDate( article, headers[ 3 ] );
            string from = headers[ 2 ];
            article.SetProp( NntpPlugin._propRawFrom, from );
            mySelf = ParseFrom( article, TranslateHeader( charset, from ), out sender );
            string subject = headers[ 1 ];
            article.SetProp( NntpPlugin._propRawSubject, subject );
            article.SetProp( Core.Props.Subject, TranslateHeader( charset, subject ) );
            UpdateLastCorrespondDate( sender, date );
            ParseMessageId( article, articleId, articleId );
            string references = headers[ 5 ];
            if( references.Length > 0 )
            {
                ParseReferences( article, references );
            }
            article.AddLink( NntpPlugin._propTo, groupRes );
            if( newArticle )
            {
                article.SetProp( NntpPlugin._propIsUnread, true );
                IResourceList categories = Core.CategoryManager.GetResourceCategories( groupRes );
                foreach( IResource category in categories )
                {
                    Core.CategoryManager.AddResourceCategory( article, category );
                }
                Core.FilterEngine.ExecRules( StandardEvents.ResourceReceived, article );
                CleanLocalArticle( articleId, article );
            }
            article.EndUpdate();
            if( mySelf && new ServerResource( server ).MarkFromMeAsRead )
            {
                article.BeginUpdate();
                article.DeleteProp( NntpPlugin._propIsUnread );
                article.EndUpdate();
            }
            Core.TextIndexManager.QueryIndexing( article.Id );
        }

        /** 
         * preparing article lines: gathering line-broken headers,
         * trimimg, replacing tabs in headers with spaces
         */
        private static void PrepareLines( string[] lines )
        {
            string line;
            int headersLines = 0;

            // at first, search for body begining
            for( ; headersLines < lines.Length && lines[ headersLines ] != "\r\n"; ++headersLines );

            // remove escaped single periods
            for( int i = headersLines + 1; i < lines.Length; ++i )
            {
                line = lines[ i ];
                if( line.StartsWith( ".." ) )
                {
                    lines[ i ] = line.Remove( 0, 1 );
                }
            }

            // then remove ending CRs
            for( int i = 0; i < headersLines; ++i )
            {
                lines[ i ] = lines[ i ].TrimEnd( '\r', '\n' );
            }

            // finally, look through headers, combine headers where necessary
            for( ; headersLines > 1; )
            {
                line = lines[ --headersLines ];
                if(  line.StartsWith( " " ) || line.StartsWith( "\t" ) )
                {
                    string previousLine = lines[ headersLines - 1 ];
                    lines[ headersLines - 1 ] = previousLine + line.TrimStart( ' ', '\t' );
                    lines[ headersLines ] = null;
                }
            }
        }

        /**
         * returns true if a contact is myself
         */
        internal static bool ParseFrom( IResource article, string fromValue, out IContact contact )
        {
            if( MIMEParser.ContainsMIMEStrings( fromValue ) )
            {
                fromValue = ParseTools.ParseMIMEHeader( fromValue );
            }

            fromValue = fromValue.Replace( "<", null ).Replace( ">", null ).Replace( "\\", null ).Replace( "//", null );
            string[] parts = fromValue.Split( ' ' );
            string eMail = string.Empty;
            foreach( string part in parts )
            {
                if( part.IndexOf( '@' ) >= 0 )
                {
                    eMail = part;
                    break;
                }
            }
            string displayName = fromValue;
            if( eMail.Length > 0 )
            {
                displayName = displayName.Replace( eMail, null ).Trim();
            }
            if( eMail.Length > 0 || displayName.Length > 0 )
            {
                IContactManager cm = Core.ContactManager;
                IResource oldFrom = article.GetLinkProp( cm.Props.LinkFrom );
                contact = cm.FindOrCreateContact( eMail, displayName );
                cm.LinkContactToResource( cm.Props.LinkFrom, contact.Resource, article, eMail, displayName );
                if( oldFrom != null && contact.Resource != oldFrom )
                {
                    cm.DeleteUnusedContacts( oldFrom.ToResourceList() );
                }
                return contact.IsMyself;
            }
            contact = null;
            return false;
        }

        private static void ParseMessageId( IResource article, string idValue, string idCandidate )
        {
            if( article.HasProp( NntpPlugin._propArticleId ) )
            {
                idValue = article.GetPropText( NntpPlugin._propArticleId );
            }
            else
            {
                if( idCandidate != idValue )
                {
                    idValue = idCandidate;
                }
                article.SetProp( NntpPlugin._propArticleId, idValue );
            }
            IResourceList childs = Core.ResourceStore.FindResources(
                NntpPlugin._newsArticle, NntpPlugin._propReferenceId, idValue );
            foreach( IResource child in childs )
            {
                SetReply( child, article );
                UpdateLastThreadArticleDate( article );
            }
        }

        private static void ParseNewsgroups( IResource article, IResource groupRes, ServerResource serverRes, string newsgroups )
        {
            string[] groups = newsgroups.Split( ',' );
            foreach( string group in groups )
            {
                bool groupFound = false;
                foreach( IResource res in serverRes.Groups )
                {
                    if( groupRes != res &&
                        String.Compare( group, res.GetPropText( Core.Props.Name ), true ) == 0 &&
                        new NewsgroupResource( res ).IsSubscribed )
                    {
                        article.AddLink( NntpPlugin._propTo, res );
                        groupFound = true;
                        break;
                    }
                }
                if( !groupFound )
                {
                    IResource fakeGroup = null;
                    IResourceList fakeGroups =  Core.ResourceStore.FindResources(
                        NntpPlugin._newsGroup, Core.Props.Name, group );
                    if( fakeGroups.Count == 0 )
                    {
                        fakeGroup = Core.ResourceStore.NewResource( NntpPlugin._newsGroup );
                        fakeGroup.SetProp( Core.Props.Name, group );
                    }
                    else
                    {
                        foreach( IResource res in fakeGroups )
                        {
                            if( fakeGroup == null || res.HasProp( Core.Props.Parent ) )
                            {
                                fakeGroup = res;
                                if( res.HasProp( Core.Props.Parent ) )
                                {
                                    break;
                                }
                            }
                        }
                    }
                    article.AddLink( NntpPlugin._propTo, fakeGroup );
                }
            }
        }

        private static void Subscribe2Groups( IResource article, string newsgroups )
        {
            IResource server = article.GetLinkProp( NntpPlugin._propTo );
            if( server != null )
            {
                article.DeleteLink( NntpPlugin._propTo, server );
                string[] groups = newsgroups.Split( ',' );
                foreach( string group in groups )
                {
                    NntpPlugin.Subscribe2Group( group, server );
                    foreach( IResource groupRes in new ServerResource( server ).Groups )
                    {
                        if( String.Compare( groupRes.GetPropText( Core.Props.Name ), group, true ) == 0 )
                        {
                            article.AddLink( NntpPlugin._propTo, groupRes );
                            break;
                        }
                    }
                }
            }
        }

        private static DateTime ParseDate( IResource article, string dateValue )
        {
            DateTime date;
            DateTime threadDate = article.GetDateProp( NntpPlugin._propLastArticleDate );
            try
            {
                date = RFC822DateParser.ParseDate( dateValue );
                article.SetProp( Core.Props.Date, date );
                
                if( threadDate == DateTime.MinValue || threadDate < date )
                    article.SetProp( NntpPlugin._propLastArticleDate, date );
            }
            catch( Exception e )
            {
                Trace.WriteLine( "Failed to parse RFC-822 date " + dateValue + ": " + e.Message );
                date = DateTime.Now;
                if( !article.HasProp( Core.Props.Date ) )
                {
                    article.SetProp( Core.Props.Date, date );
                }
            }
            return date;
        }

        internal static void ParseReferences( IResource article, string refValue )
        {
            string[] refs = refValue.Trim().Split( ' ' );
            if( refs.Length > 0 )
            {
                string reference = ParseTools.EscapeCaseSensitiveString( refs[ refs.Length - 1 ] );
                article.SetProp( NntpPlugin._propReferenceId, reference );
                IResource parentArticle = Core.ResourceStore.FindUniqueResource(
                    NntpPlugin._newsArticle, NntpPlugin._propArticleId, reference );
                if( parentArticle != null )
                {
                    SetReply( article, parentArticle );
                    UpdateLastThreadArticleDate( article );
                }
            }
        }

        internal static void ProcessBody( string body, string content_type, IResource articleRes, string defaultCharset )
        {
            ProcessBody( body, content_type, string.Empty, articleRes, defaultCharset );
        }

        private static void ProcessBody( string body, string content_type,
            string content_transfer_encoding, IResource article, string defaultCharset )
        {
            _mimeBodyParser.ProcessBody( body, content_type, content_transfer_encoding, defaultCharset );

            // store charset as prop
            string charset = _mimeBodyParser.Charset;
            article.SetProp( Core.FileResourceManager.PropCharset, charset );
            // correct from for a specific charset
            string from = article.GetPropText( NntpPlugin._propRawFrom );
            IContact sender;
            ParseFrom( article, TranslateHeader( charset, from ), out sender );
            // correct subject for a specific charset
            string subject = article.GetPropText( NntpPlugin._propRawSubject );
            if( subject.Length > 0 )
            {
                article.SetProp( Core.Props.Subject, TranslateHeader( charset, subject ) );
            }

            // walk though article parts -- body & atatchments
            MessagePart[] parts = _mimeBodyParser.GetParts();
            foreach( MessagePart part in parts )
            {
                if( part.PartType == MessagePartTypes.Body )
                {
                    article.SetProp( Core.Props.LongBody, part.Body );
                    article.SetProp( Core.Props.Size, part.Body.Length );
                }
                else if( part.PartType == MessagePartTypes.HtmlBody )
                {
                    article.SetProp( NntpPlugin._propHtmlContent, part.Body );
                }
                else
                {
                    /**
                     * forbid adding attachments for the LocalArticles resources
                     */
                    if( article.Type != NntpPlugin._newsLocalArticle )
                    {
                        string extension = IOTools.GetExtension( part.Name );
                        string resourceType = Core.FileResourceManager.GetResourceTypeByExtension( extension );
                        if( resourceType == null )
                        {
                            resourceType = NntpPlugin._unknownFileResourceType;
                        }
                        IResource attachment = Core.ResourceStore.BeginNewResource( resourceType );
                        try
                        {
                            attachment.SetProp( Core.Props.Name, part.Name );
                            attachment.SetProp( Core.Props.Size, (int) part.Content.Length );
                            attachment.SetProp( NntpPlugin._propContent, part.Content );
                            if( part.PartType == MessagePartTypes.Inline )
                            {
                                attachment.SetProp( NntpPlugin._propInlineAttachment, true );
                            }
                            else if( part.PartType == MessagePartTypes.Embedded )
                            {
                                attachment.SetProp( CommonProps.ContentId, part.ContentId );
                                attachment.AddLink( NntpPlugin._propEmbeddedContent, article );
                                continue;
                            }
                            attachment.AddLink( NntpPlugin._propAttachment, article );
                        }
                        finally
                        {
                            attachment.EndUpdate();
                            Core.TextIndexManager.QueryIndexing( attachment.Id );
                        }
                    }
                }
            }
            if( !article.HasProp( Core.Props.LongBody ) && !article.HasProp( NntpPlugin._propHtmlContent ) )
            {
                article.SetProp( Core.Props.LongBody, " " );
                article.SetProp( Core.Props.Size, 0 );
            }
            article.DeleteProp( NntpPlugin._propHasNoBody );
            NewsArticleHelper.RemoveArticleNumbers( article );
            _mimeBodyParser.Clear();
        }

        internal static string TranslateHeader( string charset, string header )
        {
            header = MIMEParser.ContainsMIMEStrings( header ) ? ParseTools.ParseMIMEHeader( header ) :
                                                                MIMEParser.TranslateRawStringInCharset( charset, header );
            return header;
        }

        private static void UpdateLastCorrespondDate( IContact sender, DateTime date )
        {
            if( sender != null )
            {
                if( ProductType.Reader == NntpPlugin._productType )
                {
                    IResource contactRes = sender.Resource;
                    if( contactRes.GetDateProp( Core.ContactManager.Props.LastCorrespondenceDate ) < date )
                    {
                        contactRes.SetProp( Core.ContactManager.Props.LastCorrespondenceDate, date );
                    }
                }
            }
        }

        /// <summary>
        /// Set the date of the last thread article to all its roots along the
        /// hierarchy.
        /// </summary>
        internal static void UpdateLastThreadArticleDate( IResource article )
        {
            DateTime val = article.GetDateProp( NntpPlugin._propLastArticleDate );
            IResourceList   roots = article.GetLinksFrom( NntpPlugin._newsArticle, NntpPlugin._propReply );

            while( roots.Count > 0 )
            {
                DateTime rootVal = roots[ 0 ].GetDateProp( NntpPlugin._propLastArticleDate );
                if( rootVal == DateTime.MinValue || rootVal < val )
                    roots[ 0 ].SetProp( NntpPlugin._propLastArticleDate, val );

                roots = roots[ 0 ].GetLinksFrom( NntpPlugin._newsArticle, NntpPlugin._propReply );
            }
        }

        /// <summary>
        /// When new article comes check whether it is linked to the thread
        /// which was paused for updates. In such case, simply delete the
        /// article (non-permanently, through registered IResourceDeleter).
        /// </summary>
        private static void CheckArticleInIgnoredThreads( IResource article )
        {
            IResource   root;
            bool        ignore = ConversationBuilder.CheckPropOnParents( article,
                                                             NntpPlugin._propIsIgnoredThread,
                                                             out root );
            if( ignore )
            {
                //  We have not only to delete this article, but also check
                //  downwards the thread because it is possible for replies
                //  to be downloaded before the source article.

                IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( article.Type );
                deleter.DeleteResource( article );

                DateTime      ignoreStartDate = root.GetDateProp( NntpPlugin._propThreadVisibilityToggleDate );
                IResourceList thread = ConversationBuilder.UnrollConversation( article );
                foreach( IResource res in thread )
                {
                    DateTime dateTime = res.GetDateProp( Core.Props.Date );
                    if( !res.HasProp( Core.Props.IsDeleted ) && dateTime > ignoreStartDate )
                    {
                        deleter.UndeleteResource( res );
                    }
                }
            }
        }

        private static void CheckArticleInSelfThread( IResource article )
        {
            IResource from = article.GetLinkProp( Core.ContactManager.Props.LinkFrom );
            if( from != null && from.HasProp( Core.ContactManager.Props.Myself ) )
            {
                article.SetProp( NntpPlugin._propIsSelfThread, true );
            }
            else
            {
                IResource   root;
                bool        hasProp = ConversationBuilder.CheckPropOnParents( article,
                                                                NntpPlugin._propIsSelfThread,
                                                                out root );
                if( hasProp )
                {
                    article.SetProp( NntpPlugin._propIsSelfThread, true );

                    //  We have not only to set property for this article, but
                    //  also check downwards the thread because it is possible
                    //  for replies to be downloaded before the source article.

                    IResourceList thread = ConversationBuilder.UnrollConversation( article );
                    foreach( IResource res in thread )
                    {
                        res.SetProp( NntpPlugin._propIsSelfThread, true );
                    }
                }
            }
        }

        private static void CleanLocalArticle( string articleId, IResource articleRes )
        {
            IResource localArticle = Core.ResourceStore.FindUniqueResource(
                NntpPlugin._newsLocalArticle, NntpPlugin._propArticleId, articleId );
            if( localArticle != null )
            {
                foreach( IResource wsp in localArticle.GetLinksOfType( "Workspace", "WorkspaceVisible" ) )
                {
                    Core.WorkspaceManager.AddResourceToWorkspace( wsp, articleRes );                    
                }
                Core.ContactManager.UnlinkContactInformation( localArticle );
                localArticle.Delete();
                NewsFolders.PlaceResourceToFolder( articleRes, NewsFolders.SentItems );
            }
        }

        private static IntHashSet _convsParents = new IntHashSet( 16 );

        private static void SetReply( IResource child, IResource article )
        {
            try
            {
                _convsParents.Add( child.Id );
                IResource res = article;
                while( res != null )
                {
                    int id = res.Id;
                    if( _convsParents.Contains( id ) )
                    {
                        return;
                    }
                    _convsParents.Add( id );
                    res = res.GetLinkProp( NntpPlugin._propReply );

                }
                child.SetProp( NntpPlugin._propReply, article );
            }
            finally
            {
                _convsParents.Clear();
            }
        }

        private static void DisposeStringBuilders()
        {
            _bodyBuilder.Length = 0;
            if( _bodyBuilder.Capacity > 16384 )
            {
                _bodyBuilder.Capacity = 1024;
            }
            _headersBuilder.Length = 0;
            if( _headersBuilder.Capacity > 16384 )
            {
                _headersBuilder.Capacity = 1024;
            }
        }

        private static StringBuilder _bodyBuilder = new StringBuilder();
        private static StringBuilder _headersBuilder = new StringBuilder();
        private static MultiPartBodyParser _mimeBodyParser = new MultiPartBodyParser();
    }
}
