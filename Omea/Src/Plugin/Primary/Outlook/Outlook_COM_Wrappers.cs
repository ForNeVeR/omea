// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Reflection;
using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.COM;
using JetBrains.Omea.Diagnostics;
using Outlook;
using Exception = System.Exception;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class _com_outlook : _com_object
    {
        private Outlook.ApplicationClass _outlook = null;

        public _com_outlook(){}
        public void CreateApplication()
        {
            _outlook = new Outlook.ApplicationClass();
        }
        public Outlook.NameSpace NameSpace
        {
            get { return _outlook.GetNamespace( "MAPI" ); }
        }
    }

    internal class _com_Outlook_Application : Object_Ref_Counting
    {
        private Outlook.ApplicationClass _application;

        public _com_Outlook_Application()
        {
            _application = new Outlook.ApplicationClass();
        }
        public _com_Outlook_Application CloneRef()
        {
            return (_com_Outlook_Application) AddRef();
        }
        protected override void ReleaseObject()
        {
            _com_object.Release( _application );
        }
        public _com_OutlookMailItem CreateNew()
        {
            return new _com_OutlookMailItem( (Outlook.MailItem)_application.CreateItem( OlItemType.olMailItem ) );
        }
        public string Version
        {
            get { return _application.Version; }
        }
        public _com_OutlookNameSpace NameSpace
        {
            get { return new _com_OutlookNameSpace( _application.Session ); }
        }
        public _com_OutlookExporer ActiveExplorer
        {
            get { return new _com_OutlookExporer( _application.ActiveExplorer() ); }
        }
        public _com_OutlookExporers Explorers
        {
            get { return new _com_OutlookExporers( _application.Explorers ); }
        }
    }

    internal interface IOutlookFolderEnumeratorEvent
    {
        bool FolderFetched( _com_OutlookMAPIFolder folder );
    }

    internal class _com_OutlookExporers : COM_Object_Ref_Counting
    {
        private Outlook.Explorers _explorers = null;

        public _com_OutlookExporers( Outlook.Explorers explorers ) : base( explorers )
        {
            _explorers = explorers;
        }
        public _com_OutlookExporers CloneRef()
        {
            return (_com_OutlookExporers) AddRef();
        }
        public int Count { get { return _explorers.Count; } }
        public _com_OutlookExporer Item( int index )
        {
            return new _com_OutlookExporer( _explorers.Item( ++index ) );
        }
        public _com_OutlookExporer Add( _com_OutlookMAPIFolder folder, Outlook.OlFolderDisplayMode displayMode )
        {
            return new _com_OutlookExporer( _explorers.Add( folder.COM_Pointer, displayMode ) );
        }
    }

    internal class _com_OutlookExporer : COM_Object_Ref_Counting
    {
        private Outlook.Explorer _explorer = null;

        public _com_OutlookExporer( Outlook.Explorer explorer ) : base( explorer )
        {
            _explorer = explorer;
        }
        public _com_OutlookExporer CloneRef()
        {
            return (_com_OutlookExporer) AddRef();
        }
        public void Close()
        {
            _explorer.Close();
        }
    }

    internal class _com_OutlookNameSpace : COM_Object_Ref_Counting
    {
        private Outlook.NameSpace _nameSpace = null;
        public _com_OutlookNameSpace( Outlook.NameSpace nameSpace ) : base( nameSpace )
        {
            _nameSpace = nameSpace;
        }
        public void Logon()
        {
            _nameSpace.Logon( Missing.Value, Missing.Value, false, true );
        }
        public _com_OutlookFolders Folders
        {
            get { return new _com_OutlookFolders( _nameSpace.Folders ); }
        }
        public void Logoff()
        {
            _nameSpace.Logoff();
        }
        public _com_OutlookItem GetOutlookItemFromID( string entryID, string storeID )
        {
            object obj = _nameSpace.GetItemFromID( entryID, storeID );
            if ( obj != null && obj is Outlook.MailItem )
            {
                return new _com_OutlookMailItem( (Outlook.MailItem) obj );
            }
            if ( obj != null && obj is Outlook.MeetingItem )
            {
                return new _com_OutlookMeetingItem( (Outlook.MeetingItem) obj );
            }
            if ( obj != null && obj is Outlook.TaskItem )
            {
                return new _com_OutlookTaskItem( (Outlook.TaskItem) obj );
            }
            if ( obj != null && obj is Outlook.ContactItem )
            {
                return new _com_OutlookContactItem( (Outlook.ContactItem) obj );
            }

            return null;
        }

        public _com_OutlookMailItem GetItemFromID( string entryID, string storeID )
        {
            object obj = _nameSpace.GetItemFromID( entryID, storeID );
            if ( obj != null && obj is Outlook.MailItem )
            {
                return new _com_OutlookMailItem( (Outlook.MailItem) obj );
            }
            return null;
        }
        public _com_OutlookMAPIFolder GetFolderFromID( string entryID, string storeID )
        {
            return new _com_OutlookMAPIFolder( _nameSpace.GetFolderFromID( entryID, storeID ) );
        }
        public _com_OutlookMAPIFolder GetOutboxFolder()
        {
            return new _com_OutlookMAPIFolder( _nameSpace.GetDefaultFolder( Outlook.OlDefaultFolders.olFolderOutbox ) );
        }
        public _com_OutlookMAPIFolder GetDefaultFolder( Outlook.OlDefaultFolders defaultFolders )
        {
            return new _com_OutlookMAPIFolder( _nameSpace.GetDefaultFolder( defaultFolders ) );
        }
        public _com_OutlookNameSpace CloneRef()
        {
            return (_com_OutlookNameSpace) AddRef();
        }
        private void EnumerateFolders( IOutlookFolderEnumeratorEvent listener, _com_OutlookFolders folders )
        {
            int count = folders.Count;
            for ( int i = 0; i < count; i++ )
            {
                _com_OutlookMAPIFolder folder = folders.Item( i );
                listener.FolderFetched( folder.CloneRef() );
                EnumerateFolders( listener, folder.Folders );
                folder.Release();
            }
            folders.Release();
        }
        public void EnumerateFolders( IOutlookFolderEnumeratorEvent listener )
        {
            if ( listener == null )
                throw new ArgumentNullException( "listener" );
            EnumerateFolders( listener, Folders );
        }
    }

    internal abstract class _com_OutlookItem : COM_Object_Ref_Counting
    {
        public _com_OutlookItem( object outlookItem ) : base( outlookItem )
        {
            Guard.NullArgument( outlookItem, "outlookItem" );
        }
        public _com_OutlookMailItem CloneRef()
        {
            return (_com_OutlookMailItem) AddRef();
        }
        public abstract string Body
        {
            get;
            set;
        }
        public abstract void Display( bool modal );
        public abstract void PrintOut();
        public abstract void Delete();
        public virtual void AddCategories( ArrayList categories ){}
        public abstract string Subject
        {
            get;
            set;
        }
    }

    internal class _com_OutlookMailItem : _com_OutlookItem
    {
        private Outlook.MailItem _mailItem = null;
        public _com_OutlookMailItem( Outlook.MailItem mailItem ) : base( mailItem )
        {
            Guard.NullArgument( mailItem, "mailItem" );
            _mailItem = mailItem;
        }
        public _com_OutlookMailItem CloneRef()
        {
            return (_com_OutlookMailItem) AddRef();
        }
        public object MAPIOBJECT { get { return _mailItem.MAPIOBJECT; } }
        public string EntryID { get { return _mailItem.EntryID; } }
        public override void AddCategories( ArrayList categories )
        {
            Guard.NullArgument( categories, "categories" );
            if ( categories.Count == 0 ) return;
            string result = string.Empty;
            for ( int i = 0; i < categories.Count - 1; ++i )
            {
                result += categories[i+1];
            }
            result += categories[categories.Count - 1];
            _mailItem.Categories = result;
        }
        public override string Body
        {
            get { return _mailItem.Body; }
            set { _mailItem.Body = value; }
        }
        public string HTMLBody
        {
            get { return _mailItem.HTMLBody; }
            set { _mailItem.HTMLBody = value; }
        }
        public Outlook.OlBodyFormat BodyFormat {  get { return _mailItem.BodyFormat; } set { _mailItem.BodyFormat = value; } }
        public override void Display( bool modal )
        {
            _mailItem.Display( modal );
        }
        public _com_OutlookMailItem Reply()
        {
            return new _com_OutlookMailItem( _mailItem.Reply() );
        }
        public _com_OutlookMailItem ReplyAll()
        {
            return new _com_OutlookMailItem( _mailItem.ReplyAll() );
        }
        public _com_OutlookMailItem Forward()
        {
            return new _com_OutlookMailItem( _mailItem.Forward() );
        }
        public override void PrintOut()
        {
            _mailItem.PrintOut();
        }
        public void AddAttachments( string[] attachments )
        {
            foreach ( string attachment in attachments )
            {
                _mailItem.Attachments.Add( attachment, OlAttachmentType.olByValue, Missing.Value, "attachment" );
            }
        }

        public void AddRecipients( ArrayList recipList )
        {
            Guard.NullArgument( recipList, "recipList" );
            foreach ( RecipInfo recInfo in recipList )
            {
                Recipient recipient = _mailItem.Recipients.Add( recInfo.DisplayName + "<" + recInfo.Email + ">" );
                COM_Object.Release( recipient );
            }
        }
        public override void Delete()
        {
            _mailItem.Delete();
        }
        public override string Subject
        {
            get { return _mailItem.Subject; }
            set { _mailItem.Subject = value; }
        }
    }

    internal class _com_OutlookMeetingItem : _com_OutlookItem
    {
        private Outlook.MeetingItem _item = null;
        public _com_OutlookMeetingItem( Outlook.MeetingItem item ) : base( item )
        {
            Guard.NullArgument( item, "item" );
            _item = item;
        }
        public _com_OutlookMeetingItem CloneRef()
        {
            return (_com_OutlookMeetingItem) AddRef();
        }
        public object MAPIOBJECT { get { return _item.MAPIOBJECT; } }
        public string EntryID { get { return _item.EntryID; } }
        public override string Body
        {
            get { return _item.Body; }
            set { _item.Body = value; }
        }
        public override void Display( bool modal )
        {
            _item.Display( modal );
        }
        public _com_OutlookMailItem Reply()
        {
            return new _com_OutlookMailItem( _item.Reply() );
        }
        public _com_OutlookMailItem ReplyAll()
        {
            return new _com_OutlookMailItem( _item.ReplyAll() );
        }
        public override void PrintOut()
        {
            _item.PrintOut();
        }
        public void AddAttachments( string[] attachments )
        {
            foreach ( string attachment in attachments )
            {
                _item.Attachments.Add( attachment, OlAttachmentType.olByValue, Missing.Value, "attachment" );
            }
        }

        public void AddRecipients( ArrayList recipList )
        {
            Guard.NullArgument( recipList, "recipList" );
            foreach ( RecipInfo recInfo in recipList )
            {
                Recipient recipient = _item.Recipients.Add( recInfo.DisplayName + "<" + recInfo.Email + ">" );
                COM_Object.Release( recipient );
            }
        }
        public override void Delete()
        {
            _item.Delete();
        }
        public override string Subject
        {
            get { return _item.Subject; }
            set { _item.Subject = value; }
        }
    }

    internal class _com_OutlookTaskItem : _com_OutlookItem
    {
        private Outlook.TaskItem _item = null;
        public _com_OutlookTaskItem( Outlook.TaskItem item ) : base( item )
        {
            Guard.NullArgument( item, "item" );
            _item = item;
        }
        public _com_OutlookTaskItem CloneRef()
        {
            return (_com_OutlookTaskItem) AddRef();
        }
        public object MAPIOBJECT { get { return _item.MAPIOBJECT; } }
        public string EntryID { get { return _item.EntryID; } }
        public override string Body
        {
            get { return _item.Body; }
            set { _item.Body = value; }
        }
        public override void Display( bool modal )
        {
            _item.Display( modal );
        }
        public override void PrintOut()
        {
            _item.PrintOut();
        }
        public void AddAttachments( string[] attachments )
        {
            foreach ( string attachment in attachments )
            {
                _item.Attachments.Add( attachment, OlAttachmentType.olByValue, Missing.Value, "attachment" );
            }
        }

        public void AddRecipients( ArrayList recipList )
        {
            Guard.NullArgument( recipList, "recipList" );
            foreach ( RecipInfo recInfo in recipList )
            {
                Recipient recipient = _item.Recipients.Add( recInfo.DisplayName + "<" + recInfo.Email + ">" );
                COM_Object.Release( recipient );
            }
        }
        public override void Delete()
        {
            _item.Delete();
        }
        public override string Subject
        {
            get { return _item.Subject; }
            set { _item.Subject = value; }
        }
    }

    internal class _com_OutlookContactItem : _com_OutlookItem
    {
        private Outlook.ContactItem _item = null;
        public _com_OutlookContactItem( Outlook.ContactItem item ) : base( item )
        {
            Guard.NullArgument( item, "item" );
            _item = item;
        }
        public _com_OutlookContactItem CloneRef()
        {
            return (_com_OutlookContactItem) AddRef();
        }
        public object MAPIOBJECT { get { return _item.MAPIOBJECT; } }
        public string EntryID { get { return _item.EntryID; } }
        public override string Body
        {
            get { return _item.Body; }
            set { _item.Body = value; }
        }
        public override void Display( bool modal )
        {
            _item.Display( modal );
        }
        public override void PrintOut()
        {
            _item.PrintOut();
        }
        public void AddAttachments( string[] attachments )
        {
            foreach ( string attachment in attachments )
            {
                _item.Attachments.Add( attachment, OlAttachmentType.olByValue, Missing.Value, "attachment" );
            }
        }

        public override void Delete()
        {
            _item.Delete();
        }
        public override string Subject
        {
            get { return _item.Subject; }
            set { _item.Subject = value; }
        }
    }

    internal class _com_OutlookItems : COM_Object_Ref_Counting
    {
        private Outlook.Items _items = null;

        public _com_OutlookItems( Outlook.Items items ) : base( items )
        {
            _items = items;
        }
        public int Count { get { return _items.Count; } }

        public _com_OutlookItems CloneRef()
        {
            return (_com_OutlookItems) AddRef();
        }
    }

    internal class _com_OutlookFolders : COM_Object_Ref_Counting
    {
        private Outlook.Folders _folders = null;
        public _com_OutlookFolders( Outlook.Folders folders ) : base( folders )
        {
            _folders = folders;
        }

        public _com_OutlookFolders CloneRef()
        {
            return (_com_OutlookFolders) AddRef();
        }
        public int Count { get { return _folders.Count; } }
        public _com_OutlookMAPIFolder Item( int index )
        {
            return new _com_OutlookMAPIFolder( _folders.Item( ++index ) );
        }
    }

    internal class _com_OutlookMAPIFolder : COM_Object_Ref_Counting
    {
        private Outlook.MAPIFolder _folder = null;
        public _com_OutlookMAPIFolder( Outlook.MAPIFolder folder ) : base( folder )
        {
            _folder = folder;
        }
        public string StoreID { get { return _folder.StoreID; }  }
        public string EntryID { get { return _folder.EntryID; } }
        public _com_OutlookMAPIFolder CloneRef()
        {
            return (_com_OutlookMAPIFolder) AddRef();
        }
        public _com_OutlookItems Items { get { return new _com_OutlookItems( _folder.Items ); } }
        public _com_OutlookFolders Folders
        {
            get { return new _com_OutlookFolders( _folder.Folders ); }
        }
        public string Name { get { return _folder.Name; } }
        public _com_OutlookExporer GetExplorer( Outlook.OlFolderDisplayMode displayMode )
        {
            return new _com_OutlookExporer( _folder.GetExplorer( displayMode ) );
        }
        public int Count
        {
            get
            {
                _com_OutlookItems items = null;
                try
                {
                    items = Items;
                    int count = items.Count;
                    return count;
                }
                catch ( Exception exception )
                {
                    Tracer._TraceException( exception );
                    return 0;
                }
                finally
                {
                    if ( items != null ) items.Release();
                }
            }
        }
    }
}
