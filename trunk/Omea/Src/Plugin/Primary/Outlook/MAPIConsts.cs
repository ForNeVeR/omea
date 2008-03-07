/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using EMAPILib;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.OutlookPlugin
{
    #region Extended MAPI specific constants
    internal class MapiError
    {
        internal const int 
        MAPI_E_NOT_ENOUGH_DISK = (unchecked((int)0x8004010D)),
        MAPI_E_NETWORK_ERROR = (unchecked((int)0x80040115)),
        MAPI_E_EXTENDED_ERROR = (unchecked((int)0x80040119)),
        MAPI_E_FAILONEPROVIDER = (unchecked((int)0x8004011D)),
        MAPI_E_SUBMITTED = (unchecked((int)0x80040608));
    }
    internal class AttachMethod
    {
        internal const int ATTACH_OLE = 6;
    }
    internal class MAPIConst
    {
        internal const int PR_ENTRYID = 0x0FFF0102,
        PR_ENTRYID_ASSOCIATED_WITH_AB = 0x66100102,
        PR_STORE_ENTRYID = 0x0FFB0102,
        PR_PARENT_ENTRYID = 0x0E090102,
        PR_BODY = 0x1000001E,
        PR_SENDER_NAME = 0x0C1A001E,
        PR_SUBJECT = 0x0037001E,
        PR_MESSAGE_DELIVERY_TIME = 0x0E060040,
        PR_SENT_REPRESENTING_NAME = 0x0042001E,
        PR_SENT_REPRESENTING_EMAIL_ADDRESS = 0x0065001E,
        PR_HASATTACH = 0x0E1B000B,
        PR_SENDER_EMAIL_ADDRESS = 0x0C1F001E,
        PR_INTERNET_MESSAGE_ID = 0x1035001E,
        PR_IN_REPLY_TO_ID = 0x1042001E,
        PR_INTERNET_REFERENCES = 0x1039001E,
        PR_EMAIL_ADDRESS = 0x3003001E,
        PR_DISPLAY_NAME = 0x3001001E,
        PR_ATTACH_FILENAME = 0x3704001E,
        PR_ATTACH_LONG_FILENAME = 0x3707001E,
        PR_ATTACH_NUM = 0xe210003,
        PR_ATTACH_SIZE = 0x0E200003,
        PR_ATTACH_METHOD = 0x37050003,
        PR_SMTP_ADDRESS = 0x39FE001E,
        PR_COMPANY_NAME = 0x3A16001E,
        PR_SURNAME = 0x3A11001E,
        PR_GIVEN_NAME = 0x3A06001E,
        PR_MIDDLE_NAME = 0x3A44001E,
        PR_POSTAL_ADDRESS = 0x3A15001E,
        PR_BUSINESS_HOME_PAGE = 0x3A51001E,
        PR_TITLE = 0x3A17001E,
        PR_BIRTHDAY = 0x3A420040,
        PR_RECIPIENT_TYPE = 0x0C150003,
        PR_LIST_UNSUBSCRIBE = 0x1045001E,
        PR_CLIENT_SUBMIT_TIME = 0x00390040,
        PR_MESSAGE_SIZE = 0x0E080003,
        PR_LAST_MODIFICATION_TIME = 0x30080040,
        PR_MESSAGE_FLAG = (unchecked((int)0x803E001E)),
        PR_MESSAGE_CLASS = 0x001A001E,
        PR_PRIORITY = 0x00260003,
        PR_IMPORTANCE = 0x00170003,
        PR_CONTAINER_CLASS = 0x3613001E,
        TASK_DUE_DATE = (unchecked((int)0x80E20040)),
        TASK_REMIND_DATE = (unchecked((int)0x80250040)),
        TASK_REMIND = (unchecked((int)0x8013000B)),
        TASK_STATUS = (unchecked((int)0x81380003)),
        PR_LONGTERM_ENTRYID_FROM_TABLE = (unchecked(0x66700102)),
        PR_INTERNET_CPID = 0x3FDE0003,
        PR_RTF_COMPRESSED = 0x10090102,
        PR_TRANSPORT_MESSAGE_HEADERS = 0x007D001E,
        PR_RECORD_KEY = 0x300B0102,
        PR_INTERNET_CONTENT = 0x66590102,
        PR_CONTACT_EMAIL_ADDRESS = (unchecked((int)0x805F001E)),
        PR_CONTACT_EMAIL_ADDRESS1 = (unchecked((int)0x8050001E)),
        PR_FLAG_STATUS = 0x10900003,
        PR_FLAG_COLOR = 0x10950003,
        PR_IPM_WASTEBASKET_ENTRYID = 0x35E30102,
        PR_IPM_SENTMAIL_ENTRYID = 0x35E40102, 
		PR_IPM_OUTBOX_ENTRYID = 0x35E20102,
		PR_IPM_APPOINTMENT_ENTRYID = 0x36D00102,
		PR_IPM_CONTACT_ENTRYID = 0x36D10102,
		PR_IPM_DRAFTS_ENTRYID = 0x36D70102,
		PR_IPM_JOURNAL_ENTRYID = 0x36D20102,
		PR_IPM_NOTE_ENTRYID = 0x36D30102,
		PR_IPM_TASK_ENTRYID = 0x36D40102,
        PR_CONVERSATION_INDEX = 0x00710102,
        PR_DISPLAY_NAME_PREFIX = 0x3A45001E,
        PR_GENERATION = 0x3A05001E,
        PR_REPORT_TIME = 0x00320040,
        PR_ORIGINAL_SUBJECT = 0x0049001E,
        PR_ORIGINAL_SUBMIT_TIME = 0x004E0040,
        PR_ORIGINAL_DISPLAY_TO = 0x0074001E,
        PR_STORE_SUPPORT_MASK = 0x340D0003,
        PR_CONTENT_COUNT = 0x36020003,
        PR_ICON_INDEX = 0x10800003,
        PR_ATTACH_CONTENT_ID = 0x3712001E,
        PR_DISPLAY_TYPE = 0x39000003,
        PR_ADDITIONAL_REN_ENTRYIDS = 0x36D81102,
        PR_EMS_AB_PROXY_ADDRESSES = (unchecked((int)0x800F101E)),
        PR_DISPLAY_TO = 0x0E04001E,
        PR_RENDERING_POSITION = 0x370B0003;
    };

    internal class ABType
    {
        internal const int DT_GLOBAL = 131072;
        internal const int DT_NOTSPECIFIC = 327680;

        internal const int DT_MAILUSER = 0;
        internal const int DT_DISTLIST = 1;
        internal const int DT_FORUM = 2;
        internal const int DT_AGENT = 3;
        internal const int DT_ORGANIZATION = 4;
        internal const int DT_PRIVATE_DISTLIST = 5;
        internal const int DT_REMOTE_MAILUSER = 6;
    }

    public class PropType
    {
        public const int PT_LONG = 3;
        public const int PT_BOOLEAN = 11;
        public const int PT_STRING8 = 30;
        public const int PT_UNICODE = 31;
        public const int PT_SYSTIME = 64;

        public const int PT_MV_STRING8 = MV_FLAG | PT_STRING8;
        public const int PT_MV_UNICODE = MV_FLAG | PT_UNICODE;

        public const int MV_FLAG = 0x1000;
    }

    public enum MAPIPhones
    {
        PR_ASSISTANT_TELEPHONE_NUMBER = 0x3A2E001E, //Assistant
        PR_OFFICE_TELEPHONE_NUMBER = 0x3A08001E,    //Work
        PR_BUSINESS2_TELEPHONE_NUMBER = 0x3A1B001E, //Business2
        PR_BUSINESS_FAX_NUMBER = 0x3A24001E,        //Business Fax
        PR_CALLBACK_TELEPHONE_NUMBER = 0x3A02001E,  //Callback
        PR_CAR_TELEPHONE_NUMBER = 0x3A1E001E,       //Car
        PR_COMPANY_MAIN_PHONE_NUMBER = 0x3A57001E,  //Company
        PR_HOME_TELEPHONE_NUMBER = 0x3A09001E,      //Home
        PR_HOME2_TELEPHONE_NUMBER = 0x3A2F001E,     //Home2
        PR_HOME_FAX_NUMBER = 0x3A25001E,            //Home Fax
        PR_ISDN_NUMBER = 0x3A2D001E,                //ISDN
        PR_CELLULAR_TELEPHONE_NUMBER = 0x3A1C001E,  //Mobile
        PR_OTHER_TELEPHONE_NUMBER = 0x3A1F001E,     //Other
        PR_PRIMARY_FAX_NUMBER = 0x3A23001E,         //Other Fax
        PR_PAGER_TELEPHONE_NUMBER = 0x3A21001E,     //Pager
        PR_PRIMARY_TELEPHONE_NUMBER = 0x3A1A001E,   //Primary
        PR_RADIO_TELEPHONE_NUMBER = 0x3A1D001E,     //Radio
        PR_TELEX_NUMBER = 0x3A2C001E,               //Telex
        PR_TTYTDD_PHONE_NUMBER = 0x3A4B001E,        //TTY/TTD
    }

    public class Phone
    {
        private string _name;
        private MAPIPhones _mapiPhone;

        public Phone( string name, MAPIPhones mapiPhone )
        {
            Guard.NullArgument( name, "name" );
            _name = name;
            _mapiPhone = mapiPhone;
            _phones.Add( this );
            _nameToPhone.Add( _name, this );
        }
        public string Name { get { return _name; } }
        public MAPIPhones MAPIPhone { get { return _mapiPhone; } }
        public int MAPIPhoneAsInt { get { return (int)_mapiPhone; } }

        static Phone( )
        {
            _assistant = new Phone( "Assistant", MAPIPhones.PR_ASSISTANT_TELEPHONE_NUMBER );
            _work = new Phone( "Work", MAPIPhones.PR_OFFICE_TELEPHONE_NUMBER );
            _business2 = new Phone( "Business2", MAPIPhones.PR_BUSINESS2_TELEPHONE_NUMBER );
            _businessFax = new Phone( "Business Fax", MAPIPhones.PR_BUSINESS_FAX_NUMBER );
            _callback = new Phone( "Callback", MAPIPhones.PR_CALLBACK_TELEPHONE_NUMBER );
            _car = new Phone( "Car", MAPIPhones.PR_CAR_TELEPHONE_NUMBER );
            _company = new Phone( "Company", MAPIPhones.PR_COMPANY_MAIN_PHONE_NUMBER );
            _home = new Phone( "Home", MAPIPhones.PR_HOME_TELEPHONE_NUMBER );
            _home2 = new Phone( "Home2", MAPIPhones.PR_HOME2_TELEPHONE_NUMBER );
            _homeFax = new Phone( "Home Fax", MAPIPhones.PR_HOME_FAX_NUMBER );
            _isdn = new Phone( "ISDN", MAPIPhones.PR_ISDN_NUMBER );
            _mobile = new Phone( "Mobile", MAPIPhones.PR_CELLULAR_TELEPHONE_NUMBER );
            _other = new Phone( "Other", MAPIPhones.PR_OTHER_TELEPHONE_NUMBER );
            _otherFax = new Phone( "Other Fax", MAPIPhones.PR_PRIMARY_FAX_NUMBER );
            _pager = new Phone( "Pager", MAPIPhones.PR_PAGER_TELEPHONE_NUMBER );
            _primary = new Phone( "Primary", MAPIPhones.PR_PRIMARY_TELEPHONE_NUMBER );
            _radio = new Phone( "Radio", MAPIPhones.PR_RADIO_TELEPHONE_NUMBER );
            _telex = new Phone( "Telex", MAPIPhones.PR_TELEX_NUMBER );
            _ttyttd = new Phone( "TTY/TTD", MAPIPhones.PR_TTYTDD_PHONE_NUMBER );
        }

        private static Phone _assistant; 
        private static Phone _work; 
        private static Phone _business2; 
        private static Phone _businessFax; 
        private static Phone _callback; 
        private static Phone _car; 
        private static Phone _company; 
        private static Phone _home; 
        private static Phone _home2;
        private static Phone _homeFax; 
        private static Phone _isdn; 
        private static Phone _mobile;
        private static Phone _other; 
        private static Phone _otherFax; 
        private static Phone _pager; 
        private static Phone _primary; 
        private static Phone _radio; 
        private static Phone _telex; 
        private static Phone _ttyttd; 
        private static HashMap _nameToPhone = new HashMap();
        private static ArrayList _phones = new ArrayList();

        static public Phone Assistant { get { return _assistant; } }
        static public Phone Work { get { return _work; } }
        static public Phone Business2 { get { return _business2; } }
        static public Phone BusinessFax { get { return _businessFax; } }
        static public Phone Callback { get { return _callback; } }
        static public Phone Car { get { return _car; } }
        static public Phone Company { get { return _company; } }
        static public Phone Home { get { return _home; } }
        static public Phone Home2 { get { return _home2; } }
        static public Phone HomeFax { get { return _homeFax; } }
        static public Phone ISDN { get { return _isdn; } }
        static public Phone Mobile { get { return _mobile; } }
        static public Phone Other { get { return _other; } }
        static public Phone OtherFax { get { return _otherFax; } }
        static public Phone Pager { get { return _pager; } }
        static public Phone Primary {get { return _primary; } }
        static public Phone Radio { get { return _radio; } }
        static public Phone Telex { get { return _telex; } }
        static public Phone TTYTTD { get { return _ttyttd; } }
        static public Phone GetPhone( string name )
        {
            HashMap.Entry entry = _nameToPhone.GetEntry( name );
            if ( entry != null )
            {
                return (Phone)entry.Value;
            }
            return null;
        }
        static public ArrayList GetPhones()
        {
            return _phones;
        }
    }

    public class MessageType
    {
        private const string Task = "ipm.task";
        private const string Contact = "ipm.contact";
        private const string Post = "ipm.post";
        private const string Note = "ipm.stickynote";
        private const string Report_read = "report.ipm.note.ipnrn";
        private const string Report_delivered = "report.ipm.note.dr";
        private const string IPM_NOTE = "ipm.note";
        private const string REPORT = "report";
        private const string IPM_SCHEDULE_MEETING = "ipm.schedule.meeting";

        public static bool IsReportRead( string messageClass )
        {
            return string.Compare( messageClass, Report_read, true ) == 0;
        }
        public static bool IsReportDelivered( string messageClass )
        {
            return string.Compare( messageClass, Report_delivered, true ) == 0;
        }
        public static bool IsScheduleMeeting( string messageClass )
        {
            return Utils.StartsWith( messageClass, IPM_SCHEDULE_MEETING, true );
        }
        public static bool IsReportMessage( string messageClass )
        {
            return string.Compare( messageClass, 0, REPORT, 0, REPORT.Length, true ) == 0;
        }
        public static bool IsNoteMessage( string messageClass )
        {
            return string.Compare( messageClass, 0, IPM_NOTE, 0, IPM_NOTE.Length, true ) == 0;
        }

        public static bool InterpretAsMail( string messageClass )
        {
            return ( IsNoteMessage( messageClass ) || 
                ( string.Compare( messageClass, Post, true ) == 0 ) ||
                ( string.Compare( messageClass, Note, true ) == 0 ) ||
                IsScheduleMeeting( messageClass ) || 
                IsReportMessage( messageClass ) || 
                ( string.Compare( messageClass, "ipm", true ) == 0 ) );
        }
        public static bool InterpretAsTask( string messageClass )
        {
            return string.Compare( messageClass, Task, true ) == 0;
        }
        public static bool InterpretAsContact( string messageClass )
        {
            return string.Compare( messageClass, Contact, true ) == 0;
        }
        public static string GetMessageClass( IEMessage message )
        {
            string messageClass = message.GetStringProp( MAPIConst.PR_MESSAGE_CLASS );
            if ( messageClass == null ) return string.Empty;
            return messageClass;
        }
    };

    public class FolderType
    {
        private FolderType(){}
        public static string Task = "IPF.Task";
        public static string IMAP = "IPF.Imap";
        public static string Dav = "IPF.Dav";
        public static string Mail = "IPF.Note";
        public static string Post = "IPF.Post";
        public static string Contact = "IPF.Contact";
        public static bool IsIMAPorDav( string messageClass )
        {
            if ( messageClass == null ) return false;
            return ( messageClass.Equals( Dav ) || messageClass.Equals( IMAP ) );
        }
    }

    public enum RecipientType
    {
        To = 1, CC = 2, BCC = 3
    };

    #endregion
    internal class GUID
    {
        public static Guid set1 = new Guid( "{00062008-0000-0000-C000-000000000046}" );
        public static Guid set2 = new Guid( "{00062003-0000-0000-C000-000000000046}" );
        public static Guid set3 = new Guid( "{00062004-0000-0000-C000-000000000046}" );
        public static Guid set4 = new Guid( "{00020329-0000-0000-C000-000000000046}" );
    }
    internal class STORE_SUPPORT_MASK
    {
        internal const int STORE_ENTRYID_UNIQUE	    = 0x00000001;
        internal const int STORE_READONLY           = 0x00000002;
        internal const int STORE_SEARCH_OK          = 0x00000004;
        internal const int STORE_MODIFY_OK          = 0x00000008;
        internal const int STORE_CREATE_OK          = 0x00000010;
        internal const int STORE_ATTACH_OK          = 0x00000020;
        internal const int STORE_OLE_OK             = 0x00000040;
        internal const int STORE_SUBMIT_OK          = 0x00000080;
        internal const int STORE_NOTIFY_OK          = 0x00000100;
        internal const int STORE_MV_PROPS_OK        = 0x00000200;
        internal const int STORE_CATEGORIZE_OK      = 0x00000400;
        internal const int STORE_RTF_OK             = 0x00000800;
        internal const int STORE_RESTRICTION_OK     = 0x00001000;
        internal const int STORE_SORT_OK            = 0x00002000;
        internal const int STORE_PUBLIC_FOLDERS     = 0x00004000;
        internal const int STORE_UNCOMPRESSED_RTF   = 0x00008000;
        
    }
    internal class lID
    {
        public const int taskCompleted = 0x811C;
        public const int taskStatus = 0x8101;
        public const int taskReminderActive = 0x8503;
        public const int taskRemindDate = 0x8502;
        public const int taskSnoozeDate = 0x8560;
        public const int taskStartDate = 0x8104;
        public const int taskDueDate = 0x8105;
        public const int msgDeletedInIMAP = 0x8570;
        public const int msgFlagAnnotation = 0x8530;
        
        public const int contactDisplayName = 0x8005;
        public const int contactEmail = 0x8084;
        
    }
}
