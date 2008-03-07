/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
   /**
    * Scans the e-mails in the Sent Items folder and adds the From 
    * addresses of those e-mails to the owner e-mails list.
    */
    internal class OwnerEmailDetector
    {
        public static void Initialize()
        {
            _ownerEmails = null;
            _ownerEmailsSet.Clear();
        }
        internal static void Detect()
        {
            if ( !Settings.DetectOwnerEmail ) return;
            IEMsgStore defStore = OutlookSession.GetDefaultMsgStore();
            if ( defStore == null ) return;
            string folderId = defStore.GetBinProp( MAPIConst.PR_IPM_SENTMAIL_ENTRYID );
            if ( folderId == null ) return;
            string storeID = defStore.GetBinProp( MAPIConst.PR_ENTRYID );
            if ( storeID == null ) return;
            IEFolder folder = OutlookSession.OpenFolder( folderId, storeID );
            if ( folder == null ) return;
            using ( folder )
            {
                ProcessFolder( folder );
            }
        }
        private static void ProcessFolder( IEFolder folder )
        {
            IETable table = folder.GetEnumTableForOwnEmail();
            if ( table == null ) return;
            using ( table )
            {
                ArrayList ownerEmails = GetOwnerEmails();
                ArrayList ownerNames = new ArrayList();

                int count = table.GetRowCount();
                if ( count > 0 )
                {
                    table.Sort( MAPIConst.PR_MESSAGE_DELIVERY_TIME, false );
                }
                for ( uint i = 0; i < count; i++ )
                {
                    ProcessRow( ownerEmails, ownerNames, table );
                }
                ProcessOwnerEmails( ownerEmails, ownerNames );
            }
        }
        private static void ProcessRow( ArrayList ownerEmails, ArrayList ownerNames, IETable table )
        {
            IERowSet row = table.GetNextRow();
            if ( row == null ) return;
            using ( row )
            {
                string senderEmail = row.GetStringProp( 0 );
                string senderName = row.GetStringProp( 1 );
                if ( senderEmail != null && senderEmail.Length > 0 && ownerEmails.IndexOf( senderEmail ) == -1 )
                {
                    ownerEmails.Add( senderEmail );
                }
                if ( senderName != null && senderName.Length > 0 && ownerNames.IndexOf( senderName ) == -1 )
                {
                    ownerNames.Add( senderName );
                }
            }
        }

        private delegate void ProcessOwnerEmailsDelegate( ArrayList emails, ArrayList ownerNames );

        private static void ProcessOwnerEmails( ArrayList emails, ArrayList ownerNames )
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.RunJob(
                    new ProcessOwnerEmailsDelegate( ProcessOwnerEmails ), emails, ownerNames );
            }
            else
            {
                IContact  myself = Core.ContactManager.MySelf;
                foreach( string email in emails )
                {
                    myself.AddAccount( email );
                }
                if( ownerNames.Count > 0 )
                {
                    myself.UpdateNameFields( (string) ownerNames[ 0 ] );
                }
                Core.ResourceStore.NewResource( "SentItemsEnumSign" );
            }
        }
        public static ArrayList GetOwnerEmails()
        {
            ArrayList result = new ArrayList();
            IResource myself = Core.ContactManager.MySelf.Resource;
            foreach( IResource emailAcct in myself.GetLinksOfType( "EmailAccount", "EmailAcct" ) )
            {
                result.Add( emailAcct.GetStringProp( "EmailAddress") );
            }
            return result;
        }
        private static IResourceList _ownerEmails = null;
        private static HashSet _ownerEmailsSet = new HashSet();
        public static bool IsOwnerEmail( string email )
        {
            if ( !ContactNames.IsValidString( email ) )
            {
                return false;
            }
            if ( _ownerEmails == null )
            {
                IResource myself = Core.ContactManager.MySelf.Resource;
                _ownerEmails = myself.GetLinksOfTypeLive( "EmailAccount", "EmailAcct" );
                _ownerEmails.ResourceAdded+=new ResourceIndexEventHandler(_ownerEmails_ResourceAdded);
                _ownerEmails.ResourceDeleting+=new ResourceIndexEventHandler(_ownerEmails_ResourceDeleting);
                foreach( IResource emailAcct in _ownerEmails )
                {
                    AddEmailAcct( emailAcct );
                }
            }
            lock ( _ownerEmailsSet )
            {
                return _ownerEmailsSet.Contains( email );
            }
        }
        private static void AddEmailAcct( IResource emailAcct )
        {
            string email = emailAcct.GetStringProp( "EmailAddress" );
            if ( email == null ) return;
            lock ( _ownerEmailsSet )
            {
                _ownerEmailsSet.Add( email );
            }
        }
        private static void RemoveEmailAcct( IResource emailAcct )
        {
            lock ( _ownerEmailsSet )
            {
                _ownerEmailsSet.Remove( emailAcct.GetStringProp( "EmailAddress" ) );
            }
        }

        private static void _ownerEmails_ResourceAdded(object sender, ResourceIndexEventArgs e)
        {
            AddEmailAcct( e.Resource );
        }

        private static void _ownerEmails_ResourceDeleting(object sender, ResourceIndexEventArgs e)
        {
            RemoveEmailAcct( e.Resource );
        }
    }
}