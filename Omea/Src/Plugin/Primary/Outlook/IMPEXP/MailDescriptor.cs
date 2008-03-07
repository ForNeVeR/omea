/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class MailDescriptor : AbstractNamedJob
    {
        private FolderDescriptor _folder;
        private string _entryID;
        private string _subject;
        private DateTime _lastModifiedDate;
        private bool _unread;
        private int _messageSize;
        private int _iconIndex = 0;
        private int _priority;
        private int _importance;
        private string _flag;
        private DateTime _receivedTime;
        private string _listUnsubscribe;
        private bool _contactCreated;
        private string _senderName;
        private string _senderEmail;
        private DateTime _sentOn;
        private string _internetMessageID;
        private string _replyToID;
        private string _internetReferences;
        private ArrayList _recipients = new ArrayList();
        private ArrayList _attachments = new ArrayList();
        private bool _bSentToMe = false;
        private bool _bSentMySelf = false;
        private bool _toDeleteResource = false;
        private string _recordKey;
        private ArrayList _outlookCategories = null;
        private int _flagColor;
        private int _flagStatus;
        private string _conversationIndex;
        private bool _deletedInIMAP = false;
        private MDState _state;
        protected static Tracer _tracer = new Tracer( "MailDescriptor" );
        private string _longBody = null;
        private const int _longBodyMaxSize = 118;
        private string _messageClass;


        private static MDState _normalState = new MDState();
        private static MDState _updateState = new MDUpdateState();
        private static MDState _movedState = new MDMovedState();

        public static MDState NormalState{ get { return _normalState;} }
        public static MDState UpdateState{ get { return _updateState;} }
        public static MDState MovedState{ get { return _movedState;} }

        public MailDescriptor( FolderDescriptor folderDescriptor, string entryID,  
            IEMessage message, string longBody )
        {
            _state = NormalState;
            Init( folderDescriptor, entryID, message, longBody );
        }

        public MailDescriptor( FolderDescriptor folderDescriptor, string entryID, 
            IEMessage message, MDState state, string longBody )
        {
            _state = state;
            Init( folderDescriptor, entryID, message, longBody );
        }
        public MailDescriptor( FolderDescriptor folderDescriptor, string entryID,  
            IEMessage message )
        {
            _state = NormalState;
            Init( folderDescriptor, entryID, message, null );
            _longBody = message.GetPlainBody( _longBodyMaxSize );
        }

        public MailDescriptor( FolderDescriptor folderDescriptor, string entryID, 
            IEMessage message, MDState state )
        {
            _state = state;
            Init( folderDescriptor, entryID, message, null );
            _longBody = message.GetPlainBody( _longBodyMaxSize );
        }
        public void QueueJob( )
        {
            Core.ResourceAP.QueueJob( this );
        }
        public void QueueJob( JobPriority jobPriority )
        {
            Core.ResourceAP.QueueJob( jobPriority, this );
        }
        protected void Init( FolderDescriptor folderDescriptor, string entryID, IEMessage message, string longBody )
        {
            _longBody = longBody;
            _folder = folderDescriptor;
            _messageClass = MessageType.GetMessageClass( message );
            if ( !Settings.ProcessAllPropertiesForMessage )
            {
                _subject = "test";
                return;
            }
            string folderID = message.GetBinProp( MAPIConst.PR_PARENT_ENTRYID );
            if ( folderID != null )
            {
                string storeID = message.GetBinProp( MAPIConst.PR_STORE_ENTRYID );
                IEFolder folder = OutlookSession.OpenFolder( folderID, storeID );
                if ( folder != null )
                {
                    using ( folder )
                    {
                        _folder = FolderDescriptor.Get( folder );
                    }
                }
            }
            _entryID = entryID;

            if ( _folder != null && Folder.IsIgnored( _folder ) )
            {
                _toDeleteResource = true;
                return;
            }

            int tag = message.GetIDsFromNames( ref GUID.set1, lID.msgDeletedInIMAP, PropType.PT_LONG );
            _deletedInIMAP = message.GetLongProp( tag ) == 1;

            if ( _deletedInIMAP && Settings.IgnoreDeletedIMAPMessages )
            {
                _toDeleteResource = true;
                return;
            }

            _subject = message.GetStringProp( MAPIConst.PR_SUBJECT );
            if ( _subject == null )
            {
                _subject = string.Empty;
            }
            _unread = message.IsUnread();

            _lastModifiedDate = message.GetDateTimeProp( MAPIConst.PR_LAST_MODIFICATION_TIME );
            _lastModifiedDate = _lastModifiedDate.ToUniversalTime();

            _messageSize = message.GetLongProp( MAPIConst.PR_MESSAGE_SIZE );
            _iconIndex = message.GetLongProp( MAPIConst.PR_ICON_INDEX );
            
            _priority = message.GetLongProp( MAPIConst.PR_PRIORITY );
            int importance = message.GetLongProp( MAPIConst.PR_IMPORTANCE, true );
            if ( importance == -9999 )
            {
                importance = 1;    
            }
            _importance = importance - 1;
            _flagStatus = message.GetLongProp( MAPIConst.PR_FLAG_STATUS );

            if ( _flagStatus == 2 )
            {
                _flagColor = message.GetLongProp( MAPIConst.PR_FLAG_COLOR, true );
                if ( _flagColor == -9999 )
                {
                    _flagColor = 6;
                }
            }

            _internetMessageID = message.GetStringProp( MAPIConst.PR_INTERNET_MESSAGE_ID );

            _recordKey = message.GetBinProp( MAPIConst.PR_RECORD_KEY );
            _conversationIndex = message.GetBinProp( MAPIConst.PR_CONVERSATION_INDEX );

            _replyToID = message.GetStringProp( MAPIConst.PR_IN_REPLY_TO_ID );
            _internetReferences = message.GetStringProp( MAPIConst.PR_INTERNET_REFERENCES );

            if ( Settings.CreateAnnotationFromFollowup )
            {
                int annotationTag = message.GetIDsFromNames( ref GUID.set1, lID.msgFlagAnnotation, PropType.PT_STRING8 );
                _flag = message.GetStringProp( annotationTag );
            }

            _outlookCategories = OutlookSession.GetCategories( message );;

            _receivedTime = message.GetDateTimeProp( MAPIConst.PR_MESSAGE_DELIVERY_TIME );
            _sentOn = message.GetDateTimeProp( MAPIConst.PR_CLIENT_SUBMIT_TIME );
            if ( _receivedTime == DateTime.MinValue ) 
            {
                _receivedTime = _sentOn;
            }

            _listUnsubscribe = message.GetStringProp( MAPIConst.PR_LIST_UNSUBSCRIBE );
            if ( _listUnsubscribe != null )
            {
                _listUnsubscribe = ExtractUnsubscribeEmail( _listUnsubscribe );
            }

            _contactCreated = MailSenderHelper.LoadSenderInfo( message, out _senderName, out _senderEmail );

            if ( Settings.ProcessRecipients )
            {
                _state.EndOfInit( this, message );
            }
        }

        public string EntryID
        {
            get { return _entryID; }
        }
        /**
         * Checks if the given address is the address of the mailing list for
         * which the unsubscribe address is known.
         */		

        private bool IsUnsubscribeAddress( string listAddr, string unsubscribeAddr )
        {
            int pos = unsubscribeAddr.IndexOf( '?' );
            if ( pos >= 0 )
                unsubscribeAddr = unsubscribeAddr.Substring( 0, pos );

            string[] listPortions = listAddr.ToLower().Split( '@' );
            string[] unsubPortions = unsubscribeAddr.ToLower().Split( '@' );
            if ( listPortions.Length != 2 || unsubPortions.Length != 2 )
            {
                return false;
            }

            // must be the same domain, and unsubscribe address should contain list addr.
            // for example: python-dev-unsubscribe@python.org, python-dev@python.org
            return 
                unsubPortions [1] == listPortions [1] && 
                unsubPortions [0].StartsWith( listPortions [0] );
        }
        private void AddMailingListCategory( IResource mail, IResource mailingList )
        {
            string listName = mailingList.DisplayName;
            int pos = listName.IndexOf( '@' );
            if ( pos >= 0 )
                listName = listName.Substring( 0, pos );

            IResource listCategory = Core.CategoryManager.FindOrCreateCategory( null, listName );
            Core.CategoryManager.AddResourceCategory( mail, listCategory );
                
            foreach( IResource fromRes in mail.GetLinksOfType( "Contact", PROP.From ).ValidResources )
            {
                Core.CategoryManager.AddResourceCategory( fromRes, listCategory );
            }
            foreach( IResource toRes in mail.GetLinksOfType( "Contact", PROP.To ).ValidResources )
            {
                Core.CategoryManager.AddResourceCategory( toRes, listCategory );
            }
            foreach( IResource ccRes in mail.GetLinksOfType( "Contact", PROP.CC ).ValidResources )
            {
                Core.CategoryManager.AddResourceCategory( ccRes, listCategory );
            }
        }

        private void ProcessRecipients( IResource mail )
        {
            foreach ( RecipientHelper recipient in _recipients )
            {
                IResource recRes = null;
                if ( _listUnsubscribe != null && IsUnsubscribeAddress( recipient.EmailAddr, _listUnsubscribe ) )
                {
                    recRes = Core.ContactManager.FindOrCreateMailingList( recipient.EmailAddr );
                    if( Settings.CreateCategoriesFromMailingLists )
                    {
                        AddMailingListCategory( mail, recRes );
                    }
                }
                else
                {
                    IContact contact = null;
                    if ( recipient.MySelf )
                    {
                        contact = Core.ContactManager.FindOrCreateMySelfContact( recipient.EmailAddr, recipient.DisplayName );
                    }
                    else
                    {
                        contact = Core.ContactManager.FindOrCreateContact( recipient.EmailAddr, recipient.DisplayName );
                    }
                    recRes = contact.Resource;

                    if( _bSentMySelf && (contact.LastCorrespondDate.CompareTo( mail.GetProp( Core.Props.Date ) ) < 0 ))
                        contact.LastCorrespondDate = (DateTime)mail.GetProp( Core.Props.Date );
                }
                recipient.SetPerson( recRes );

                //-------------------------------------------------------------
                //  Link e-mail with the account and the contact
                //-------------------------------------------------------------
                Core.ContactManager.LinkContactToResource( 
                    recipient.IsTo ? Core.ContactManager.Props.LinkTo : Core.ContactManager.Props.LinkCC,
                    recRes, mail, recipient.EmailAddr, recipient.DisplayName );

            }
        }

        /**
         * Creates resources for the attachments of the specified e-mail
         * and adds them as links to the specified e-mail resource.
         */
        private void PrepareAttachments( IEMessage message )
        {
            // NOTE: IEMessage.GetAttachments() restricts the attachment table to a specific
            // set of columns, so if you need to process more columns here, don't forget to
            // modify the code in GetAttachments().
            IETable attaches = message.GetAttachments();
            if ( attaches != null ) using ( attaches )
                                    {
                                        long count = attaches.GetRowCount();
                                        for ( long  i = 0; i < count; i++ )
                                        {
                                            IERowSet rowSet = attaches.GetNextRow();
                                            if ( rowSet == null ) continue;
                                            using ( rowSet )
                                            {

                                                int size = rowSet.FindLongProp( MAPIConst.PR_ATTACH_SIZE );
                                                int attachMethod = rowSet.FindLongProp( MAPIConst.PR_ATTACH_METHOD );
   
                                                string strFileName = rowSet.FindStringProp( MAPIConst.PR_ATTACH_LONG_FILENAME  );
                                                if ( strFileName == null )
                                                {
                                                    strFileName = rowSet.FindStringProp( MAPIConst.PR_ATTACH_FILENAME );
                                                }
                                                if ( strFileName == null )
                                                {
                                                    strFileName = rowSet.FindStringProp( MAPIConst.PR_DISPLAY_NAME );
                                                }
                                                if ( strFileName != null )
                                                {
                                                    string strFileType = string.Empty;
                                                    int dotIndex = strFileName.LastIndexOf(".");
                                                    if ( dotIndex != -1 )
                                                    {
                                                        strFileType = strFileName.Substring( dotIndex ).ToUpper();
                                                    }
                                                    int num = rowSet.FindLongProp( MAPIConst.PR_ATTACH_NUM );
                                                    string strContentID = null;
                                                    IEAttach attachment = message.OpenAttach( num );
                                                    if ( attachment != null )
                                                    {
                                                        using ( attachment )
                                                        {
                                                            strContentID = attachment.GetStringProp( MAPIConst.PR_ATTACH_CONTENT_ID );
                                                        }
                                                    }

                                                    AttachmentHelper attach = 
                                                        new AttachmentHelper( strFileName, strFileType, (int)i, size, attachMethod, strContentID, num );
                                                    _attachments.Add( attach );
                                                }
                                            }
                                        }
                                    }
        }
        /**
         * Finds or adds an attachment type with the specified extension.
         */

        private IResource FindAttachmentType( string ext )
        {
            IResourceList resList = 
                Core.ResourceStore.FindResources( STR.AttachmentType, Core.Props.Name, ext );
            if ( resList.Count > 1 )
                throw new Exception( "Multiple attachment types with the same extension found" );

            if ( resList.Count == 1 )
                return resList [0];

            IResource attType = Core.ResourceStore.NewResource( STR.AttachmentType );
            attType.SetProp( Core.Props.Name, ext );
            return attType;
        }

        private void UpdateAttachments( IResource mail )
        {
            IResourceList attachments = mail.GetLinksOfType( null, PROP.Attachment );

            foreach ( IResource attachment in attachments.ValidResources )
            {
                LinkContactsAndAttachment( mail, attachment );
            }
        }
        private void ProcessAttachments( IResource mail )
        {
            foreach ( AttachmentHelper attachmentHelper in _attachments )
            {

                IResource attType = FindAttachmentType( attachmentHelper.FileType );
                string resourceType;
                if ( attachmentHelper.IsEmbeddedMessage )
                {
                    resourceType = STR.Email;
                }
                else
                {
                    resourceType = Core.FileResourceManager.GetResourceTypeByExtension( attachmentHelper.FileType );
                    if ( attachmentHelper.FileName != null && 
                        string.Compare( attachmentHelper.FileName, ResourceSerializer.ResourceTransferFileName, true ) == 0 )
                    {
                        resourceType = STR.ResourceAttachment;
                    }
                    if ( resourceType == null )
                    {
                        resourceType = "UnknownFile";
                    }
                }

                IResource attachment = Core.ResourceStore.BeginNewResource( resourceType );
                attachment.SetProp( Core.Props.Name, attachmentHelper.FileName );
                
                if ( attachmentHelper.FileName != null && 
                    string.Compare( attachmentHelper.FileName, ResourceSerializer.ResourceTransferFileName, true ) == 0 )
                {
                    attachment.SetProp( PROP.ResourceTransfer, true );
                }
                if ( resourceType == STR.Email )
                {
                    attachment.SetProp( Core.Props.Subject, attachmentHelper.FileName );     
                    attachment.SetProp( PROP.EmbeddedMessage, true );     
                }
                attachment.SetProp( PROP.AttachmentIndex, attachmentHelper.Index );
                attachment.SetProp( Core.Props.Size, attachmentHelper.Size );
                attachment.SetProp( Core.Props.Date, _receivedTime );
                attachment.SetProp( PROP.AttType, attType );
                attachment.SetProp( CommonProps.ContentId, attachmentHelper.ContentID );
                attachment.SetProp( PROP.PR_ATTACH_NUM, attachmentHelper.Num );
                attachment.SetProp( PROP.AttachMethod, attachmentHelper.AttachMethod );
                attachment.SetProp( Core.Props.NeedPreview, true );
                
                mail.AddLink( PROP.AttType, attType );
                if ( resourceType == STR.ResourceAttachment )
                {
                    attachment.AddLink( PROP.InternalAttachment, mail );
                }
                else
                {
                    attachment.AddLink( PROP.Attachment, mail );
                }
                LinkContactsAndAttachment( mail, attachment );

                Guard.QueryIndexingWithCheckId( attachment );
                attachment.EndUpdate();                        
            }
        }
        private void LinkContactsAndAttachment( IResource mail, IResource attachment )
        {
            ContactManager.CloneLinkage( mail, attachment );
        }

        private void PrepareRecipients( IEMessage message )
        {
            IETable recips = message.GetRecipients();
            if ( recips != null )
            {
                using ( recips )
                {
                    long count = recips.GetRowCount();
                    for ( long  i = 0; i < count; i++ )
                    {
                        IERowSet rowSet = recips.GetNextRow();
                        if ( rowSet != null ) using ( rowSet )
                        {
                            string emailAddr = rowSet.FindStringProp( MAPIConst.PR_SMTP_ADDRESS );
                            if ( emailAddr == null || emailAddr.Length == 0 )
                            {
                                emailAddr = rowSet.FindStringProp( MAPIConst.PR_EMAIL_ADDRESS );
                            }
                            if ( emailAddr != null && emailAddr.Length > 0 )
                            {
                                string displayName = rowSet.FindStringProp( MAPIConst.PR_DISPLAY_NAME );

                                bool isTo = ( rowSet.FindLongProp( MAPIConst.PR_RECIPIENT_TYPE ) == (int) RecipientType.To);
                                bool mySelf = OwnerEmailDetector.IsOwnerEmail( emailAddr );

                                if ( mySelf ) 
                                {
                                    _bSentToMe = true;
                                }
                                _recipients.Add( new RecipientHelper( emailAddr, displayName, isTo, mySelf ) );
                            }
                        }
                    }
                }
            }
        }

        private string ExtractUnsubscribeEmail( string listUnsubscribe )
        {
            while ( listUnsubscribe.Length > 0 )
            {
                if ( !listUnsubscribe.StartsWith( "<" ) )
                    return null;

                int pos = listUnsubscribe.IndexOf( '>' );
                if ( pos == -1 )
                    return null;

                string addr = listUnsubscribe.Substring( 1, pos-1 ).Trim().ToLower();
                if ( addr.StartsWith( "mailto:" ) )
                    return addr.Substring( "mailto:".Length );

                listUnsubscribe = listUnsubscribe.Substring( pos+1 ).Trim();
                if ( !listUnsubscribe.StartsWith( "," ) )
                    return null;

                listUnsubscribe = listUnsubscribe.Substring( 1 ).Trim();
            }
            return null;
        }

        /**
         * Parses the "References:" and "In-Reply-To: headers of a message
         * and returns the list of message IDs to which this message is a reply.
         */

        private ArrayList ParseReplyTo( )
        {
            ArrayList repliesTo = new ArrayList();

            if ( _replyToID != null && _replyToID.Length > 0 )
            {
                // in-reply-to     =       "In-Reply-To:" 1*msg-id CRLF
				
                // If there is more than one parent message, then the "In-
                // Reply-To:" field will contain the contents of all of the parents'
                // "Message-ID:" fields.

                while ( _replyToID.Length > 0 )
                {
                    int pos = _replyToID.IndexOf( "<" , 1);
                    string thisReplyTo = null;
                    if ( pos > 0 )
                    {
                        thisReplyTo = _replyToID.Substring( 0, pos ).Trim();
                        _replyToID = _replyToID.Substring( pos );
                    }
                    else
                    {
                        thisReplyTo = _replyToID;
                        _replyToID = string.Empty;
                    }

                    repliesTo.Add( thisReplyTo );
                }
            }
            else if ( _internetReferences != null && _internetReferences.Length > 0 )
            {
                // The "References:" field will contain the contents of the parent's
                // "References:" field (if any) followed by the contents of the parent's
                // "Message-ID:" field (if any).
                // => take only the last "References:" entry
                int pos = _internetReferences.LastIndexOf( "<" );
                if ( pos >= 0 )
                {
                    repliesTo.Add( _internetReferences.Substring( pos ) );
                }
            }
            return repliesTo;
        }


        /**
         * Add up/down links in the reply tree to the message.
         */

        private void AddReplyLinks( IResource mail )
        {
            if ( _internetMessageID != null && _internetMessageID.Trim().Length > 0 )
            {
                mail.SetProp( PROP.InternetMsgID,  _internetMessageID );
                
                IResourceList replies = 
                    Core.ResourceStore.FindResources( "Email", PROP.ReplyTo, _internetMessageID );
                foreach( IResource reply in replies.ValidResources )
                {
                    if ( reply.Id != mail.Id )
                    {
                        reply.AddLink( Core.Props.Reply, mail );
                    }
                }
            }

            ArrayList repliesTo = ParseReplyTo( );
            if ( repliesTo.Count > 0 )
            {
                string replyToID = (string) repliesTo[0];
                mail.SetProp( PROP.ReplyTo, replyToID );

                IResourceList replyToMails = 
                    Core.ResourceStore.FindResources( "Email", PROP.InternetMsgID, replyToID );
                if ( replyToMails.Count > 0 )
                {
                    IResource replyToMail = replyToMails[0];
                    if ( mail.Id != replyToMail.Id )
                    {
                        mail.AddLink( Core.Props.Reply, replyToMail );
                    }
                }
            }
        }

        private void AddConversationReplyLinks( IResource mail )
        {
            // check if the conversation index fits in with the structure documented in
            // MSDN (22 bytes for thread root, 5 bytes extra for every reply)
            // NOTE: _conversationIndex is a hex-encoded binary string, so each byte is 2 chars
            if ( _conversationIndex != null && 
                _conversationIndex.Length >= 44 && (_conversationIndex.Length - 44) % 10 == 0 )
            {
                mail.SetProp( PROP.ConversationIndex, _conversationIndex );
                IResourceList replies = Core.ResourceStore.FindResources( "Email", 
                    PROP.ReplyToConversationIndex, _conversationIndex );
                foreach( IResource reply in replies.ValidResources )
                {
                    if ( reply.Id != mail.Id )
                    {
                        reply.AddLink( Core.Props.Reply, mail );
                    }
                }
                
                if ( _conversationIndex.Length > 44 )
                {
                    string replyToIndex = _conversationIndex.Substring( 0, _conversationIndex.Length - 10 );
                    mail.SetProp( PROP.ReplyToConversationIndex, replyToIndex );

                    IResourceList replyToMails = Core.ResourceStore.FindResources( "Email",
                        PROP.ConversationIndex, replyToIndex );
                    if ( replyToMails.Count > 0 )
                    {
                        IResource replyToMail = replyToMails [0];
                        if ( replyToMail.Id != mail.Id )
                        {
                            mail.AddLink( Core.Props.Reply, replyToMail );
                        }
                    }
                }
            }
        }

        private void Trace( string message )
        {
            if ( Settings.TraceOutlookListeners )
            {
                _tracer.Trace( message );
            }
        }

        protected IResource ExecuteImpl()
        {
            IResource resMail = GetEmailResource();
            if ( resMail == null ) 
            {
                Trace( "GetEmailResource returned null" );
                return null;
            }
            resMail.SetProp( Core.Props.Subject, _subject );
            resMail.SetProp( Core.Props.Date, _receivedTime );
            resMail.SetProp( PROP.SentOn, _sentOn );
            resMail.SetProp( PROP.ContainerClass, _messageClass );

            if ( _contactCreated )
            {
                CreateSenderContact( resMail );
            }
            _state.EndOfExecuteImpl( this, resMail );
            bool isChanged = resMail.IsChanged();

            SetDisplayName( resMail );
            LinkToFolder(resMail);

            AddReplyLinks( resMail );
            AddConversationReplyLinks( resMail );

            SetFlag( resMail );
            if ( Settings.SyncMailCategory )
            {
                CategorySetter.DoJob( _outlookCategories, resMail );
            }
            resMail.SetProp( PROP.LastModifiedTime, _lastModifiedDate );
            resMail.SetProp( Core.Props.Size, _messageSize );
            resMail.SetProp( PROP.PR_ICON_INDEX, _iconIndex );
            resMail.SetProp( PROP.EntryID, _entryID );
            resMail.SetProp( PROP.RecordKey, _recordKey );
            resMail.SetProp( Core.Props.IsUnread, _unread );
            resMail.SetProp( PROP.Priority, _priority );
            if ( _longBody != null )
            {
                if ( _longBody.Length == _longBodyMaxSize )
                {
                    _longBody += "...";
                }
                resMail.SetProp( Core.Props.LongBody, _longBody );
            }

            bool deletedInIMAP = resMail.HasProp( PROP.DeletedInIMAP );
            resMail.SetProp( PROP.DeletedInIMAP, _deletedInIMAP );
            if ( deletedInIMAP != _deletedInIMAP )
            {
                Mail.SetIsDeleted( resMail, _deletedInIMAP );
            }
            
            if ( !(resMail.GetIntProp( PROP.Importance ) == 0 && _importance == 0 ) )
            {
                resMail.SetProp( PROP.Importance, _importance );
            }
            
            if ( Settings.CreateAnnotationFromFollowup )
            {
                if ( _flag == null || _flag.Length == 0 )
                {
                    resMail.DeleteProp( Core.Props.Annotation );
                }
                else
                {
                    resMail.SetProp( Core.Props.Annotation, _flag );
                }
            }
            
            resMail.EndUpdate();
            if ( isChanged )
            {
                QueryIndexing( resMail );
            }
            return resMail;
        }

        private void LinkToFolder(IResource resMail)
        {
            IResource resFolder = null;
            if ( _folder != null )
            {
                resFolder = Folder.Find( _folder.FolderIDs.EntryId );
            }
            if ( resFolder != null )
            {
                Folder.LinkMail( resFolder, resMail );
                IResource msgStore = Folder.GetMAPIStorage( resFolder );
                if ( msgStore.GetDateProp( PROP.LastReceiveDate ) < _receivedTime )
                {
                    msgStore.SetProp( PROP.LastReceiveDate, _receivedTime );
                }
            }
            else
            {
                _tracer.Trace( "Can't link mail to folder" );
            }
        }

        protected override void Execute()
        {
            ExecuteImpl();
        }

        protected virtual void QueryIndexing( IResource resMail )
        {
            Guard.NullArgument( resMail, "resMail" );
            if ( Core.TextIndexManager != null )
            {
                Guard.QueryIndexingWithCheckId( resMail );
            }
        }

        private void SetDisplayName( IResource resMail )
        {
            if ( !_bSentToMe )
            {
                resMail.DisplayName = resMail.GetPropText( Core.Props.Subject ) + " <--- " + resMail.GetPropText( "From" );
            }
            else
            {
                resMail.DisplayName = resMail.GetPropText( Core.Props.Subject ) + " ---> " + resMail.GetPropText( "To" );
            }
        }

        private IResource GetEmailResource()
        {
            if ( _entryID == null ) 
            {
                Trace( "GetEmailResource: entryId = null" );
                return null;
            }
            IResource mail = _state.FindMail( this, _entryID );

            if ( _toDeleteResource )
            {
                if ( mail != null )
                {
                    Trace( "MailDescriptor: deleting email resource in ignored folder ID=" + mail.Id );
                    Mail.ForceDelete( mail );
                }
                return null;
            }

            _state = _state.BeginUpdate( ref mail );
            if ( mail == null )
            {
                _state = _normalState.BeginUpdate( ref mail );
            }
            return mail;
        }

        private void CreateSenderContact( IResource resMail )
        {
            IContact contact = null;
            
            if ( OwnerEmailDetector.IsOwnerEmail( _senderEmail ) )
            {
                contact = Core.ContactManager.FindOrCreateMySelfContact( _senderEmail, _senderName );
                resMail.SetProp( Core.Props.Date, _sentOn );
                _bSentMySelf = true;
            }
            else
            {
                contact = Core.ContactManager.FindOrCreateContact( _senderEmail, _senderName );
            }
            if ( ( _bSentMySelf || _bSentToMe ) && contact.LastCorrespondDate.CompareTo( resMail.GetProp( Core.Props.Date ) ) < 0 )
            {
                contact.LastCorrespondDate = (DateTime)resMail.GetProp( Core.Props.Date );
            }

            Core.ContactManager.LinkContactToResource( Core.ContactManager.Props.LinkFrom, contact.Resource, resMail, _senderEmail, _senderName );
            if ( !resMail.HasLink( Core.ContactManager.Props.LinkFrom, contact.Resource ) )
                throw new ApplicationException( "Core.ContactManager.LinkContactToResource did not linked mail to contact" );
        }

        private void SetFlag( IResource resMail )
        {
            switch ( _flagStatus )
            {
                case 1:
                    OutlookFlags.SetCompletedFlag( resMail );
                    break;
                case 2:
                    if ( OutlookSession.Version < 11 )
                    {
                        if ( !OutlookFlags.IsCustomFlagSet( resMail ) )
                        {
                            OutlookFlags.SetOnResource( resMail, _flagColor );
                        }
                    }
                    else
                    {
                        OutlookFlags.SetOnResource( resMail, _flagColor );
                    }
                    break;
                default:
                    OutlookFlags.ClearFlag( resMail );
                    break;
            }
        }

        private IResourceList GetMailListByRecordKey()
        {
            IResourceList mailsByRecordKey = 
                Core.ResourceStore.FindResources( "Email", PROP.RecordKey, _recordKey );
            if ( mailsByRecordKey.Count > 0 )
            {
                IResource mailByRecordKey = mailsByRecordKey[0];
                mailByRecordKey.SetProp( PROP.EntryID, _entryID );
                return Core.ResourceStore.FindResources( "Email", PROP.EntryID, _entryID );
            }
            return null;
        }
        internal class MDState
        {
            public virtual void EndOfInit( MailDescriptor mailDescriptor, IEMessage message )
            {
                mailDescriptor.PrepareRecipients( message );
                mailDescriptor.PrepareAttachments( message );
            }
            public virtual void EndOfExecuteImpl( MailDescriptor mailDescriptor, IResource resMail )
            {
                mailDescriptor.ProcessRecipients( resMail );
                mailDescriptor.ProcessAttachments( resMail );
            }
            public virtual MDState BeginUpdate( ref IResource resMail )
            {
                if ( resMail == null )
                {
                    resMail = Core.ResourceStore.BeginNewResource( STR.Email );
                    if ( Settings.UseOutlookListeners )
                    {
                        Tracer._Trace( "Created email resource ID=" + resMail.Id );
                    }
                    return this;
                }
                resMail.BeginUpdate();
                return MailDescriptor.UpdateState;
            }
            public virtual IResource FindMail( MailDescriptor mailDescriptor, string entryID )
            {
                return Core.ResourceStore.FindUniqueResource( "Email", PROP.EntryID, entryID );
            }
        }
        internal class MDUpdateState : MDState
        {
            public override void EndOfExecuteImpl( MailDescriptor mailDescriptor, IResource resMail )
            {
                mailDescriptor.ProcessRecipients( resMail );
                IResource resPerson = resMail.GetLinkProp( PROP.From );
                if ( resPerson != null )
                {
                    mailDescriptor.UpdateAttachments( resMail );
                }
            }
            public override MDState BeginUpdate( ref IResource resMail )
            {
                if ( resMail != null )
                {
                    resMail.BeginUpdate();
                }
                return this;
            }
        }
        internal class MDMovedState : MDUpdateState
        {
            public override IResource FindMail( MailDescriptor mailDescriptor, string entryID )
            {
                IResource mail = base.FindMail( mailDescriptor, entryID );
                if ( mail == null && mailDescriptor._recordKey != null )
                {
                    IResourceList mailList = mailDescriptor.GetMailListByRecordKey();
                    if ( mailList.Count != 0 )
                    {
                        for ( int i = mailList.Count - 1; i > 0; i-- )
                        {
                            Mail.ForceDelete( mailList[i] );
                        }
                        return mailList[0];
                    }
                }
                return mail;
            }
        }

        public override string Name
        {
            get
            {
                string name = "Process mail: ";
                if ( _subject != null )
                {
                    name += _subject;
                }
                return name;
            }
            set { }
        }
    }
    internal class NewMailDescriptor : MailDescriptor
    {
        public NewMailDescriptor( FolderDescriptor folderDescriptor, string entryID, IEMessage message ) : 
            base( folderDescriptor, entryID, message )
        {
            if ( Settings.TraceOutlookListeners )
            {
                _tracer.Trace( "NewMailDescriptor was created" );
            }
        }
        protected override void Execute()
        {
            IResource mail = ExecuteImpl();
            if ( mail == null ) return;
            if ( Settings.SetCategoryFromContactWhenEmailArrived )
            {
                IResourceList categories = Core.CategoryManager.GetResourceCategories( mail );
                foreach ( IResource category in categories.ValidResources )
                {
                    foreach( IResource fromRes in mail.GetLinksOfType( "Contact", PROP.From ).ValidResources )
                    {
                        Core.CategoryManager.AddResourceCategory( fromRes, category );
                    }
                }
            }
            Core.FilterManager.ExecRules( StandardEvents.ResourceReceived, mail );
        }
    }
    internal class SyncOnlyMailDescriptor : MailDescriptor
    {
        public SyncOnlyMailDescriptor( FolderDescriptor folderDescriptor, string entryID, IEMessage message ) : 
            base( folderDescriptor, entryID, message, MailDescriptor.UpdateState )
        {
        }
        protected override void Execute()
        {
            ExecuteImpl();
        }
        protected override void QueryIndexing( IResource resMail )
        {
            Guard.NullArgument( resMail, "resMail" );
        }
    }
}
