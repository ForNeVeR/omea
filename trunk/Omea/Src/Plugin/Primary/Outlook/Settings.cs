/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    internal abstract class MailSyncMode
    {
        public const string None = "none";
        public const string Fresh = "fresh";
        public const string All = "all";
    }

    internal class Settings
    {
        private Settings()
        {}
        private static SettingsCollection _settings = new SettingsCollection();
        private static IResourceTreePane _outlookFolders;
        private static IdleModeManager _idleModeManager = new IdleModeManager();
        private static int _lastProgressUpdateTicks = 0;

        public static IdleModeManager IdleModeManager { get { return _idleModeManager; } }

        public static IResourceTreePane OutlookFolders { get { return _outlookFolders; } set { _outlookFolders = value; } }

        public static void UpdateProgress( int percentage, string statusText, string timeMessage )
        {
            if ( Core.ProgressWindow != null && Core.State != CoreState.ShuttingDown )
            {
                if ( Environment.TickCount - _lastProgressUpdateTicks > 250 )
                {
                    if ( percentage > 100 )
                    {
                        percentage = 100;
                    }
                    Core.ProgressWindow.UpdateProgress( percentage, statusText, timeMessage );
                    _lastProgressUpdateTicks = Environment.TickCount;
                }
            }
        }

        #region Settings for Outlook plugin

        public static BoolSetting ExportTasks = _settings.Create( "MailIndexing", "ExportTasks", true );
        public static BoolSetting ProcessMessageMove = _settings.Create( "MailIndexing", "ProcessMessageMove", true );
        public static BoolSetting ProcessMessageAdd = _settings.Create( "MailIndexing", "ProcessMessageAdd", true );
        public static BoolSetting ProcessMessageModify = _settings.Create( "MailIndexing", "ProcessMessageModify", true );
        public static BoolSetting ProcessFolderModify = _settings.Create( "MailIndexing", "ProcessFolderModify", true );
        public static BoolSetting ProcessMessageNew = _settings.Create( "MailIndexing", "ProcessMessageNew", true );
        public static BoolSetting ProcessLoadBody = _settings.Create( "MailIndexing", "ProcessLoadBody", true );
        public static BoolSetting ProcessRecipients = _settings.Create( "MailIndexing", "ProcessRecipients", true );
        public static BoolSetting ProcessAllPropertiesForMessage = _settings.Create( "MailIndexing", "ProcessAllPropertiesForMessage", true );
        public static BoolSetting CreateCategoriesFromMailingLists = _settings.Create( "MailIndexing", "CreateCategoriesFromMailingLists", false );
        public static BoolSetting GreetingInReplies = _settings.Create( "MailFormat", "GreetingInReplies", false );
        public static BoolSetting IdleIndexing = _settings.Create( "Startup", "IdleIndexing", false );
        public static BoolSetting ShowEmbedPics = _settings.Create( "MailIndexing", "ShowEmbedPics", false );
        public static BoolSetting DeliverOnStartup = _settings.Create( "MailIndexing", "DeliverOnStartup", false );
        public static BoolSetting ScheduleDeliver = _settings.Create( "MailIndexing", "ScheduleDeliver", true );
        public static BoolSetting UseSignature = _settings.Create( "MailFormat", "UseSignature", false );
        public static BoolSetting UseOutlookListeners = _settings.Create( "MailIndexing", "UseOutlookListeners", true );
        public static BoolSetting TraceOutlookListeners = _settings.Create( "MailIndexing", "TraceOutlookListeners", false );
        public static BoolSetting TraceContactChanges = _settings.Create( "MailIndexing", "TraceContactChanges", false );
        public static BoolSetting TraceTaskChanges = _settings.Create( "MailIndexing", "TraceTaskChanges", false );
        public static BoolSetting SyncContacts = _settings.Create( "MailIndexing", "SyncContacts", true );
        public static BoolSetting UseBackgroundMailSync = _settings.Create( "MailIndexing", "UseBackgroundMailSync", true );
        public static BoolSetting SyncAttachments = _settings.Create( "MailIndexing", "SyncAttachments", true );
        public static BoolSetting DetectOwnerEmail = _settings.Create( "MailIndexing", "DetectOwnerEmail", true );
        public static BoolSetting MarkAsReadOnReply = _settings.Create( "MailIndexing", "MarkAsReadOnReply", true );
        public static BoolSetting IgnoreDeletedIMAPMessages = _settings.Create( "MailIndexing", "IgnoreDeletedIMAPMessages", true );
        public static BoolSetting SupportIMAP = _settings.Create( "MailIndexing", "SupportIMAP", false );
        public static BoolSetting ShowExcludedFolders = _settings.Create( "MailIndexing", "ShowExcludedFolders", true );
        public static BoolSetting DebugMessageBox = _settings.Create( "MailIndexing", "DebugMessageBox", false );
        public static BoolSetting CreateAnnotationFromFollowup = _settings.Create( "MailIndexing", "CreateAnnotationFromFollowup", false );
        public static BoolSetting UseTimeoutForReadingProp = _settings.Create( "MailIndexing", "UseTimeoutForReadingProp", true );
        public static BoolSetting SetCategoryFromContactWhenEmailArrived = _settings.Create( "MailIndexing", "SetCategoryFromContactWhenEmailArrived", false );
        public static BoolSetting SetCategoryFromContactWhenEmailSent = _settings.Create( "MailIndexing", "SetCategoryFromContactWhenEmailSent", false );

        public static DateSetting LastExecutionTime = _settings.Create( "MailIndexing", "LastExecutionTime", DateTime.MinValue );
        public static DateSetting IndexStartDate = _settings.Create( "Startup", "IndexStartDate", DateTime.MinValue );

        public static StringSetting ListenersStrategy = _settings.Create( "MailIndexing", "ListenersStrategy" );
        public static StringSetting Signature = _settings.Create( "MailFormat", "Signature", string.Empty );
        public static StringSetting SyncMode = _settings.Create( "MailIndexing", "SyncMode", MailSyncMode.Fresh );

        public static IntSetting ListenersStrategyTimeInSeconds = _settings.Create( "MailIndexing", "ListenersStrategyTimeInSeconds", 0 );
        public static IntSetting SendReceiveTimeout = _settings.Create( "MailIndexing", "SendReceiveTimeout", 5 );
        public static IntSetting IdlePeriod = _settings.Create( "MailIndexing", "IdlePeriod", 5 );
        public static StringSetting LastSelectedFileFolder = _settings.Create( "MailIndexing", "LastSelectedFileFolder" );

        public static BoolSetting SyncTaskCategory = _settings.Create( "MailIndexing", "SyncTaskCategory", true );
        public static BoolSetting SyncContactCategory = _settings.Create( "MailIndexing", "SyncContactCategory", true );
        public static BoolSetting SyncMailCategory = _settings.Create( "MailIndexing", "SyncMailCategory", true );

        public static BoolSetting UseFormsWithOutlookModel = _settings.Create( "MailIndexing", "UseFormsWithOutlookModel", true );

        public static void LoadSettings()
        {
            _settings.LoadSettings();
        }

        #endregion
    }
}