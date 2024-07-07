// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class MailSyncToFolder
    {
        public static void LinkOrDelete( FolderDescriptor folder, IResource email )
        {
            Guard.NullArgument( email, "email" );
            if ( folder != null )
            {
                IResource resFolder = Folder.Find( folder.FolderIDs.EntryId );
                if ( !Folder.IsIgnored( resFolder ) )
                {
                    if ( email.GetLinkProp( PROP.MAPIFolder ) != resFolder )
                    {
                        Core.ResourceAP.QueueJob( "Link Email To Folder", new LinkMailDelegate( LinkMail ), resFolder, email );
                    }
                    return;
                }
            }
            DeleteMail( email );
        }
        private static void ForceDelete( IResource mail )
        {
            if ( mail.IsDeleting ) return;
            Mail.ForceDelete( mail );
        }
        private delegate void LinkMailDelegate( IResource folder, IResource mail );
        private static void LinkMail( IResource folder, IResource mail )
        {
            if ( mail.IsDeleting ) return;
            if ( folder.IsDeleting ) return;
            Folder.LinkMail( folder, mail );
        }
        public static void DeleteMail( IResource email )
        {
            Guard.NullArgument( email, "email" );
            Core.ResourceAP.QueueJob( "Deleting Email Resource", new ResourceDelegate( ForceDelete ), email );
        }
    }

    public class AttachmentHelper
    {
        private readonly string _fileName;
        private readonly string _fileType;
        private readonly int _index;
        private readonly int _size;
        private readonly int _attachMethod;
        private readonly string _contentID;
        private readonly int _num;

        public AttachmentHelper( string fileName, string fileType, int index, int size, int attachMethod, string contentID, int num )
        {
            _fileName = fileName;
            _fileType = fileType;
            _index = index;
            _size = size;
            _attachMethod = attachMethod;
            _contentID = contentID;
            _num = num;
        }
        public string ContentID{ get { return _contentID; } }
        public string FileName{ get { return _fileName; } }
        public string FileType{ get { return _fileType; } }
        public int Index{ get { return _index; } }
        public int Size{ get { return _size; } }
        public int Num{ get { return _num; } }
        public int AttachMethod{ get { return _attachMethod; } }
        public bool IsEmbeddedMessage{ get { return _attachMethod == 5; } }
    }

    public class RecipientHelper
    {
        private readonly string _emailAddr;
        private readonly string _displayName;
        private readonly bool   _isTo;
        private readonly bool   _mySelf;
        private IResource       _person;

        public RecipientHelper( string emailAddr, string displayName, bool isTo, bool mySelf )
        {
            _emailAddr = emailAddr;
            _displayName = displayName;
            _isTo = isTo;
            _mySelf = mySelf;
        }
        public string EmailAddr{ get { return _emailAddr; } }
        public string DisplayName{ get { return _displayName; } }
        public bool IsTo{ get { return _isTo; } }
        public bool MySelf{ get { return _mySelf; } }
        public void SetPerson( IResource person )
        {
            Guard.NullArgument( person, "person" );
            _person = person;
        }
        public IResource Person { get { return _person; } }

    }

    public class MailSenderHelper
    {
        public static bool LoadSenderInfo( IEMessage message, out string senderName, out string senderEmail )
        {
            senderName = string.Empty;
            senderEmail = message.GetStringProp( MAPIConst.PR_SENT_REPRESENTING_EMAIL_ADDRESS );
            if ( string.IsNullOrEmpty( senderEmail ) )
            {
                senderEmail = message.GetStringProp( MAPIConst.PR_SENDER_EMAIL_ADDRESS );
            }
            if( !string.IsNullOrEmpty( senderEmail ) )
            {
                senderName = message.GetStringProp( MAPIConst.PR_SENT_REPRESENTING_NAME );
                if ( string.IsNullOrEmpty( senderName ) )
                {
                    senderName = message.GetStringProp( MAPIConst.PR_SENDER_NAME );
                }
                return true;
            }
            return false;
        }
    }
}
