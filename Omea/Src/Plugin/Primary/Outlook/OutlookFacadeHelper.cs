// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.COM;
using JetBrains.Omea.Diagnostics;
using Outlook;
using Exception = System.Exception;

namespace JetBrains.Omea.OutlookPlugin
{
    public class ReplyQuoter : IQuoting
    {
        private IResource _email;
        public ReplyQuoter( IResource email )
        {
            _email = email;
        }
        public string QuoteReply( string originalBody )
        {
            try
            {
                return Core.MessageFormatter.QuoteMessage( _email, originalBody );
            }
            catch( Exception e )
            {
                Core.ReportException( e, false );
            }
            return string.Empty;
        }
    }

    public abstract class FormHelper
    {
        static protected void ReportProblem( Exception exception )
        {
            Tracer._TraceException( exception );
            //if ( exception is NullReferenceException )
            MsgBox.Error( "Outlook Plugin", "Operation cannot be completed.\nReason: " + exception.Message );
        }

        public static ArrayList GetRecipientsArray( EmailRecipient[] recipients )
        {
            if ( recipients == null )
            {
                return new ArrayList(0);
            }
            ArrayList recipList = new ArrayList( recipients.Length );
            foreach ( EmailRecipient recipient in recipients )
            {
                recipList.Add( new RecipInfo( recipient.Name, recipient.EmailAddress ) );
            }
            return recipList;
        }

        public static ArrayList GetRecipientsArray( IResourceList contactList )
        {
            return GetRecipientsArray( contactList, false );
        }
        public static ArrayList GetRecipientsArray( IResourceList contactList, bool excludeMySelf )
        {
            ArrayList recipients = new ArrayList();
            if ( contactList == null || contactList.Count == 0 ) return recipients;
            foreach ( IResource contactRes in contactList.ValidResources )
            {
                IResource contact = contactRes;
                if( contact.Type == "ContactName" )
                    contact = contact.GetLinkProp( Core.ContactManager.Props.LinkBaseContact );

                string emailAddr = null;
                string fullName = string.Empty;
                if ( contact.Type == STR.EmailAccount )
                {
                    emailAddr = contact.GetStringProp( "EmailAddress" );
                    fullName = emailAddr;
                }
                else
                {
                    if ( excludeMySelf && contact.HasProp( PROP.MySelf ) )
                        continue;

                    emailAddr = Core.ContactManager.GetContact( contact ).DefaultEmailAddress;
                    fullName = Core.ContactManager.GetFullName( contact );
                }

                if( emailAddr != null && emailAddr != string.Empty )
                {
                    if ( fullName == null || fullName.Length == 0 )
                    {
                        fullName = emailAddr;
                    }
                    recipients.Add( new RecipInfo( fullName, emailAddr ) );
                }
            }
            return recipients;
        }

        public bool PrintMessage( string EntryID, string StoreID )
        {
            _com_Outlook_Application outlook = null;
            _com_OutlookNameSpace session = null;
            _com_OutlookMailItem mailItem = null;
            try
            {
                outlook = new _com_Outlook_Application();

                session = outlook.NameSpace;
                session.Logon( );
                mailItem = session.GetItemFromID( EntryID, StoreID );

                if ( mailItem != null )
                {
                    mailItem.PrintOut();
                }
                return true;
            }
            catch ( Exception exception )
            {
                ReportProblem( exception );
            }
            finally
            {
                COM_Object.ReleaseIfNotNull( mailItem );
                COM_Object.ReleaseIfNotNull( session );
                COM_Object.ReleaseIfNotNull( outlook );
            }
            return false;
        }
        public abstract bool ForwardMessage( string EntryID, string StoreID );
        public abstract bool ReplyAllMessage( IResource mail, string EntryID, string StoreID );
        public abstract bool ReplyMessage( IResource mail, string EntryID, string StoreID );
        public abstract bool DisplayMessage( string EntryID, string StoreID );
        public abstract bool CreateNewMessage( string subject, string body, EmailBodyFormat bodyFormat, IResourceList recipients,
            string[] attachments, bool useTemplatesInBody );
        public abstract bool CreateNewMessage( string subject, string body, EmailBodyFormat bodyFormat, EmailRecipient[] recipients,
            string[] attachments, bool useTemplatesInBody );

    }
    public class EMapiFormHelper : FormHelper
    {
        private static Regex _rxHtmlComment = new Regex( @"\<\!\-\-.+\-\-\>", RegexOptions.Singleline );

        public override bool CreateNewMessage( string subject, string body, EmailBodyFormat bodyFormat, EmailRecipient[] recipients,
            string[] attachments, bool useTemplatesInBody )
        {
            ArrayList recipList = GetRecipientsArray( recipients );
            ArrayList attachList = new ArrayList();
            if ( attachments != null )
            {
                foreach ( string path in attachments )
                {
                    attachList.Add( new AttachInfo( path, Path.GetFileName( path ) ) );
                }
            }
            return CreateNewMessage( subject, body, bodyFormat, recipList, attachList, useTemplatesInBody );
        }
        public override bool CreateNewMessage( string subject, string body, EmailBodyFormat bodyFormat, IResourceList recipients,
            string[] attachments, bool useTemplatesInBody )
        {
            ArrayList recipList = GetRecipientsArray( recipients );
            ArrayList attachList = new ArrayList();
            if ( attachments != null )
            {
                foreach ( string path in attachments )
                {
                    attachList.Add( new AttachInfo( path, Path.GetFileName( path ) ) );
                }
            }
            return CreateNewMessage( subject, body, bodyFormat, recipList, attachList, useTemplatesInBody );
        }

        private bool CreateNewMessage( string subject, string body, EmailBodyFormat bodyFormat, ArrayList recipients,
            ArrayList attachments, bool useTemplatesInBody )
        {
            try
            {
                MailBodyFormat mailBodyFormat = MailBodyFormat.HTML;
                if ( bodyFormat == EmailBodyFormat.PlainText )
                {
                    if ( useTemplatesInBody && Settings.UseSignature )
                    {
                        body += "\r\n";
                        body += Settings.Signature;
                    }

                    mailBodyFormat = MailBodyFormat.PlainText;
                }
                else if ( bodyFormat == EmailBodyFormat.Html )
                {
                    body = _rxHtmlComment.Replace( body, "" );
                    body = "<HTML><BODY>" + body;
                    if ( useTemplatesInBody && Settings.UseSignature )
                    {
                        body += "\r\n";
                        body += Settings.Signature;
                    }
                    body += "</BODY></HTML>";
                    Trace.WriteLine( "HTML body for Outlook: \r\n" + body );
                }

                IEMsgStore msgStore = OutlookSession.GetDefaultMsgStore();
                if ( msgStore != null )
                {
                    return msgStore.CreateNewMessage( subject, body, mailBodyFormat, recipients,
                        attachments, OutlookSession.GetOutlookDefaultEncodingOut() );
                }
                else
                {
                    throw new ApplicationException( "There are no default message store for outlook" );
                }
            }
            catch ( Exception exception )
            {
                ReportProblem( exception );
            }
            return false;
        }

        public override bool DisplayMessage( string EntryID, string StoreID )
        {
            try
            {
                IEMsgStore msgStore = OutlookSession.OpenMsgStore( StoreID );
                if ( msgStore != null )
                {
                    return msgStore.DisplayMessage( EntryID, OutlookSession.GetDefaultMsgStore() );
                }
            }
            catch ( Exception exception )
            {
                ReportProblem( exception );
            }
            return false;
        }

        public override bool ForwardMessage( string EntryID, string StoreID )
        {
            try
            {
                IEMsgStore msgStore = OutlookSession.OpenMsgStore( StoreID );
                if ( msgStore != null )
                {
                    return msgStore.ForwardMessage( EntryID, OutlookSession.GetDefaultMsgStore() );
                }
                return true;
            }
            catch ( Exception exception )
            {
                ReportProblem( exception );
            }
            return false;
        }
        public override bool ReplyAllMessage( IResource mail, string EntryID, string StoreID )
        {
            try
            {
                OutlookSession.EMAPISession.SetQuoter( new ReplyQuoter( mail ) );
                IEMsgStore msgStore = OutlookSession.OpenMsgStore( StoreID );
                if ( msgStore != null )
                {
                    return msgStore.ReplyAllMessage( EntryID, OutlookSession.GetDefaultMsgStore() );
                }
                return true;
            }
            catch ( Exception exception )
            {
                ReportProblem( exception );
            }
            return false;
        }
        public override bool ReplyMessage( IResource mail, string EntryID, string StoreID )
        {
            try
            {
                OutlookSession.EMAPISession.SetQuoter( new ReplyQuoter( mail ) );
                IEMsgStore msgStore = OutlookSession.OpenMsgStore( StoreID );
                if ( msgStore != null )
                {
                    return msgStore.ReplyMessage( EntryID, OutlookSession.GetDefaultMsgStore() );
                }
                return true;
            }
            catch ( Exception exception )
            {
                ReportProblem( exception );
            }
            return false;
        }
    }
    public class OutlookFormHelper : FormHelper
    {
        class OutlookItemAction : IDisposable
        {
            private _com_OutlookItem _outlookItem = null;
            public OutlookItemAction( string EntryID, string StoreID )
            {
                Guard.EmptyStringArgument( EntryID, "EntryID" );
                Guard.EmptyStringArgument( StoreID, "StoreID" );
                try
                {
                    _outlookItem = GetItemFromID( EntryID, StoreID );
                }
                catch ( Exception exception )
                {
                    ReportProblem( exception );
                }
            }
            private _com_OutlookItem GetItemFromID( string EntryID, string StoreID )
            {
                OutlookGUIInit.StartAndInitializeOutlook();
                _com_Outlook_Application outlook = null;
                _com_OutlookNameSpace session = null;
                try
                {
                    outlook = new _com_Outlook_Application();
                    session = outlook.NameSpace;
                    return session.GetOutlookItemFromID( EntryID, StoreID );
                }
                catch ( Exception exception )
                {
                    ReportProblem( exception );
                }
                finally
                {
                    COM_Object.ReleaseIfNotNull( session );
                    COM_Object.ReleaseIfNotNull( outlook );
                }
                return null;
            }

            public void DisplayMessage()
            {
                try
                {
                    if ( _outlookItem != null )
                    {
                        _outlookItem.Display( false );
                    }
                }
                catch ( Exception exception )
                {
                    ReportProblem( exception );
                }
            }
            #region IDisposable Members
            public void Dispose()
            {
                COM_Object.ReleaseIfNotNull( _outlookItem );
            }

            #endregion
        }

        class MailAction : IDisposable
        {
            private _com_OutlookMailItem _mailItem = null;
            private _com_OutlookMailItem _newMail = null;
            private string _entryId;
            private string _storeId;
            public MailAction( string EntryID, string StoreID )
            {
                Guard.EmptyStringArgument( EntryID, "EntryID" );
                Guard.EmptyStringArgument( StoreID, "StoreID" );
                try
                {
                    _entryId = EntryID;
                    _storeId = StoreID;
                    _mailItem = GetItemFromID( EntryID, StoreID );
                }
                catch ( Exception exception )
                {
                    ReportProblem( exception );
                }
            }
            private _com_OutlookMailItem GetItemFromID( string EntryID, string StoreID )
            {
                OutlookGUIInit.StartAndInitializeOutlook();
                _com_Outlook_Application outlook = null;
                _com_OutlookNameSpace session = null;
                try
                {
                    outlook = new _com_Outlook_Application();
                    session = outlook.NameSpace;
                    return session.GetItemFromID( EntryID, StoreID );
                }
                catch ( Exception exception )
                {
                    ReportProblem( exception );
                }
                finally
                {
                    COM_Object.ReleaseIfNotNull( session );
                    COM_Object.ReleaseIfNotNull( outlook );
                }
                return null;
            }

            public void ForwardMessage()
            {
                try
                {
                    if ( _mailItem != null )
                    {
                        _newMail = _mailItem.Forward();
                        _newMail.Display( false );
                    }
                }
                catch ( Exception exception )
                {
                    ReportProblem( exception );
                }
            }
            private void QuoteReply( IResource mail )
            {
                try
                {
                    if ( _mailItem.BodyFormat == OlBodyFormat.olFormatPlain )
                    {
                    }
                }
                catch ( NullReferenceException )
                {
                    return;
                }
                if ( _mailItem.BodyFormat == OlBodyFormat.olFormatPlain )
                {
                    IEMessage message = OutlookSession.OpenMessage( _entryId, _storeId );
                    if ( message != null )
                    {
                        string body = message.GetStringProp( MAPIConst.PR_BODY );
                        if ( body != null )
                        {
                            _newMail.Body = new ReplyQuoter( mail ).QuoteReply( body );
                        }
                    }
                }
            }
            public void ReplyAllMessage( IResource mail )
            {
                try
                {
                    if ( _mailItem != null )
                    {
                        _newMail = _mailItem.ReplyAll();
                        QuoteReply( mail );
                        _newMail.Display( false );
                    }
                }
                catch ( Exception exception )
                {
                    ReportProblem( exception );
                }
            }
            public void ReplyMessage( IResource mail )
            {
                try
                {
                    if ( _mailItem != null )
                    {
                        _newMail = _mailItem.Reply();
                        QuoteReply( mail );
                        _newMail.Display( false );
                    }
                }
                catch ( Exception exception )
                {
                    ReportProblem( exception );
                }
            }
            #region IDisposable Members
            public void Dispose()
            {
                COM_Object.ReleaseIfNotNull( _mailItem );
                COM_Object.ReleaseIfNotNull( _newMail );
            }

            #endregion
        }

        private bool CreateNewMessage( string subject, string body, EmailBodyFormat bodyFormat, ArrayList recipients,
            string[] attachments, bool useTemplatesInBody, ArrayList categories )
        {
            Settings.LoadSettings();
            OutlookGUIInit.StartAndInitializeOutlook();
            _com_Outlook_Application outlook = null;
            _com_OutlookMailItem newMail = null;
            try
            {
                outlook = new _com_Outlook_Application();
                newMail = outlook.CreateNew();
                newMail.Subject = subject;

                bool validBody = !String.IsNullOrEmpty( body );

                if ( useTemplatesInBody && Settings.UseSignature )
                {
                    body += "\r\n";
                    body += Settings.Signature;
                }

                if ( validBody && EmailBodyFormat.Html == bodyFormat )
                {
                    try
                    {
                        newMail.BodyFormat = OlBodyFormat.olFormatHTML;
                    }
                    catch ( Exception ){}
                    newMail.HTMLBody = body;
                }
                else
                if ( validBody )
                {
                    try
                    {
                        newMail.BodyFormat = OlBodyFormat.olFormatPlain;
                    }
                    catch ( Exception ){}
                    newMail.Body = body;
                }
                else
                if ( !String.IsNullOrEmpty( body ) )
                {
                    newMail.Body = body;
                }

                if ( recipients != null && recipients.Count > 0 )
                {
                    OutlookSession.EMAPISession.AddRecipients( newMail.MAPIOBJECT, recipients );
                    if ( Settings.SetCategoryFromContactWhenEmailSent && categories != null )
                    {
                        newMail.AddCategories( categories );
                    }
                }
                if ( attachments != null && attachments.Length > 0 )
                {
                    newMail.AddAttachments( attachments );
                }
                newMail.Display( false );
                return true;
            }
            catch ( Exception exception )
            {
                ReportProblem( exception );
            }
            finally
            {
                COM_Object.ReleaseIfNotNull( newMail );
                COM_Object.ReleaseIfNotNull( outlook );
            }
            return false;
        }

        public override bool CreateNewMessage( string subject, string body, EmailBodyFormat bodyFormat, EmailRecipient[] recipients,
            string[] attachments, bool useTemplatesInBody )
        {
            ArrayList recipientsArray = null;
            if ( recipients != null && recipients.Length > 0 )
            {
                recipientsArray = GetRecipientsArray( recipients );
            }
            return CreateNewMessage( subject, body, bodyFormat, recipientsArray, attachments, useTemplatesInBody, null );
        }
        public override bool CreateNewMessage( string subject, string body, EmailBodyFormat bodyFormat, IResourceList recipients,
            string[] attachments, bool useTemplatesInBody )
        {
            ArrayList recipientsArray = null;
            ArrayList categories = new ArrayList(  );
            if ( recipients != null && recipients.Count > 0 )
            {
                recipientsArray = GetRecipientsArray( recipients );
                foreach ( IResource recipient in recipients )
                {
                    if ( recipient.Type == "Contact" )
                    {
                        IResourceList resCategories = Core.CategoryManager.GetResourceCategories( recipient );
                        ExportCategories.LoadCategoriesArrayList( resCategories, categories );
                    }
                }
            }
            return CreateNewMessage( subject, body, bodyFormat, recipientsArray, attachments, useTemplatesInBody, categories );
        }
        public override bool DisplayMessage( string EntryID, string StoreID )
        {
            using ( OutlookItemAction action = new OutlookItemAction( EntryID, StoreID ) )
            {
                action.DisplayMessage();
            }
            return true;
        }
        public override bool ForwardMessage( string EntryID, string StoreID )
        {
            using ( MailAction action = new MailAction( EntryID, StoreID ) )
            {
                action.ForwardMessage( );
            }
            return true;
        }
        public override bool ReplyAllMessage( IResource mail, string EntryID, string StoreID )
        {
            using ( MailAction action = new MailAction( EntryID, StoreID ) )
            {
                action.ReplyAllMessage( mail );
            }
            return true;
        }
        public override bool ReplyMessage( IResource mail, string EntryID, string StoreID )
        {
            using ( MailAction action = new MailAction( EntryID, StoreID ) )
            {
                action.ReplyMessage( mail );
            }
            return true;
        }
    }

    public class OutlookFacadeHelper
    {
        private static FormHelper _mapiFormHelper = new EMapiFormHelper();
        private static FormHelper _outlookFormHelper = new OutlookFormHelper();

        public static FormHelper FormHelper
        {
            get
            {
                return Settings.UseFormsWithOutlookModel ? _outlookFormHelper : _mapiFormHelper;
            }
        }

        static public IEmailService GetEmailService()
        {
            return (IEmailService) Core.PluginLoader.GetPluginService( typeof( IEmailService ) );
        }

        static public void CreateNewMessage( string subject, string body, EmailBodyFormat bodyFormat, IResourceList recipients,
            string[] attachments, bool useTemplatesInBody )
        {
            FormHelper.CreateNewMessage( subject, body, bodyFormat, recipients, attachments, useTemplatesInBody );
        }
        static public void CreateNewMessage( string subject, string body, EmailBodyFormat bodyFormat, EmailRecipient[] recipients,
            string[] attachments, bool useTemplatesInBody )
        {
            FormHelper.CreateNewMessage( subject, body, bodyFormat, recipients, attachments, useTemplatesInBody );
        }

        static public bool DisplayMessage( string EntryID, string StoreID )
        {
            return FormHelper.DisplayMessage( EntryID, StoreID );
        }

        static public bool ReplyMessage( IResource mail, string EntryID, string StoreID )
        {
            return FormHelper.ReplyMessage( mail, EntryID, StoreID );
        }

        static public bool ReplyAllMessage( IResource mail, string EntryID, string StoreID )
        {
            return FormHelper.ReplyAllMessage( mail, EntryID, StoreID );
        }

        static public bool ForwardMessage( string EntryID, string StoreID )
        {
            return FormHelper.ForwardMessage( EntryID, StoreID );
        }

        static public bool PrintMessage( string EntryID, string StoreID )
        {
            return FormHelper.PrintMessage( EntryID, StoreID );
        }
    }
}
