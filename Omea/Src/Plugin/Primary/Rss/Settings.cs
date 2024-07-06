// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.RSSPlugin
{
    internal class Settings
    {
        public const string cInitialTimeStamp = "0:00";

        private Settings(){}
        private static SettingsCollection _settings = new SettingsCollection( "RSS Settings" );

        #region Settings for RSS plugin
        public static StringSetting UpdatePeriod = _settings.Create( IniKeys.Section, IniKeys.UpdatePeriod, UpdatePeriods.Hourly );
        public static StringSetting StopUpdatePeriod = _settings.Create( IniKeys.Section, IniKeys.StopUpdatePeriod, UpdatePeriods.Hourly );
        public static StringSetting SendHomePage = _settings.Create( IniKeys.Section, IniKeys.SendHomePage );
        public static StringSetting SendEmail = _settings.Create( IniKeys.Section, IniKeys.SendEmail );
        public static StringSetting SendFrom = _settings.Create( IniKeys.Section, IniKeys.SendFrom );
        public static StringSetting EnclosurePath = _settings.Create( IniKeys.Section, IniKeys.EnclosurePath, GetDefaultEnclosuresPath() );
        public static StringSetting EnclosureDownloadStartHour = _settings.Create( IniKeys.Section, IniKeys.EnclosureDownloadStartHour, cInitialTimeStamp );
        public static StringSetting EnclosureDownloadFinishHour = _settings.Create( IniKeys.Section, IniKeys.EnclosureDownloadFinishHour, cInitialTimeStamp );
        public static StringSetting TracedUrls = _settings.Create( IniKeys.Section, IniKeys.TracedUrls );

        public static IntSetting UpdateFrequency = _settings.Create( IniKeys.Section, IniKeys.UpdateFrequency, 4 );
        public static IntSetting StopUpdateFrequency = _settings.Create( IniKeys.Section, IniKeys.StopUpdateFrequency, 4 );
        public static IntSetting TimeoutInSec = _settings.Create( IniKeys.Section, IniKeys.TimeoutInSec, 60 );

        public static BoolSetting RememberSelection = _settings.Create( IniKeys.Section, IniKeys.RememberSelection, false );
        public static BoolSetting Trace = _settings.Create( IniKeys.Section, IniKeys.Trace, false );
        public static BoolSetting UseEclosureDownloadPeriod = _settings.Create( IniKeys.Section, IniKeys.UseEclosureDownloadPeriod, false );
        public static BoolSetting ShowDesktopAlertWhenEncosureDownloadingComplete = _settings.Create( IniKeys.Section, IniKeys.ShowDesktopAlertWhenEncosureDownloadingComplete, true );
        public static BoolSetting ShowDesktopAlertWhenEncosureDownloadingFailed = _settings.Create( IniKeys.Section, IniKeys.ShowDesktopAlertWhenEncosureDownloadingFailed, true );
        public static BoolSetting CreateSubfolderForEveryFeed = _settings.Create( IniKeys.Section, IniKeys.CreateSubfolderForEveryFeed, false );
        public static BoolSetting DisableCompression = _settings.Create( IniKeys.Section, IniKeys.DisableCompression, false );

        private static string GetDefaultEnclosuresPath()
        {
            return Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.Personal ), "My Enclosures" );
        }
        #endregion

        public static void LoadSettings()
        {
            _settings.LoadSettings();
        }
    }
    internal class IniKeys
    {
        public const string Section           = "RSS";
        public const string UpdateFrequency   = "UpdateFrequency";
        public const string StopUpdateFrequency   = "StopUpdateFrequency";
        public const string UpdatePeriod      = "UpdatePeriod";
        public const string StopUpdatePeriod      = "StopUpdatePeriod";
        public const string RememberSelection = "RememberSelection";
        public const string TimeoutInSec = "TimeoutInSec";
        public const string Trace = "Trace";
        public const string SendHomePage = "SendHomePage";
        public const string SendEmail = "SendEmail";
        public const string SendFrom = "SendFrom";
        public const string RSSSearchEngine = "RSSSearchEngine";
        public const string EnclosurePath = "EnclosurePath";
        public const string UseEclosureDownloadPeriod = "UseEclosureDownloadPeriod";
        public const string EnclosureDownloadStartHour = "EnclosureDownloadStartHour";
        public const string EnclosureDownloadFinishHour = "EnclosureDownloadFinishHour";
        public const string TracedUrls = "TracedUrls";
        public const string ShowDesktopAlertWhenEncosureDownloadingComplete = "ShowDesktopAlertWhenEncosureDownloadingComplete";
        public const string ShowDesktopAlertWhenEncosureDownloadingFailed = "ShowDesktopAlertWhenEncosureDownloadingFailed";
        public const string CreateSubfolderForEveryFeed = "CreateSubfolderForEveryFeed";
		public const string PropagateFavIconToItems = "PropagateFavIconToItems";
        public const string UseDetailedURLs = "UseDetailedSourceURLs";
        public const string ShowSummary = "ShowAtomSummary";
        public const string FilterUnreadFeeds = "FilterFeedsWithReadItems";
        public const string FilterErrorFeeds = "FilterFeedsWithErrors";
        public const string ShowPlaneList = "ShowPlaneList";
        public const string DisableCompression = "DisableCompression";
    }

    internal class UpdatePeriods
    {
        public const string Minutely = "minutely";
        public const string Hourly   = "hourly";
        public const string Daily    = "daily";
        public const string Weekly   = "weekly";
    }
}
