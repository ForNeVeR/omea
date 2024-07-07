// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
    internal class Settings
    {
        private Settings(){}
        private static readonly SettingsCollection _settings = new SettingsCollection( "NNTP Settings" );

        #region Settings for NNTP plugin

        public static BoolSetting DownloadBodiesOnDeliver = _settings.Create( "NNTP", "DownloadBodiesOnDeliver", true );
        public static BoolSetting DownloadBodyOnSelection = _settings.Create( "NNTP", "DownloadBodyOnSelection", true );
        public static BoolSetting DisplayAttachmentsInline = _settings.Create( "NNTP", "DisplayAttachmentsInline", false );
        public static BoolSetting CloseOnReply = _settings.Create( "NNTP", "CloseOnReply", true );
        public static BoolSetting MarkAsReadOnReplyAndFormard = _settings.Create( "NNTP", "MarkAsReadOnReplyAndFormard", false );
        public static BoolSetting MarkAsReadOnReplyThreadStop = _settings.Create( "NNTP", "MarkAsReadOnThreadStop", true );
        public static BoolSetting MarkFromMeAsRead = _settings.Create( "NNTP", "MarkFromMeAsRead", false );
        public static BoolSetting DeliverOnStartup = _settings.Create( "NNTP", "DeliverOnStartup", true );
        public static BoolSetting FirstStart = _settings.Create( "NNTP", "FirstStart", false );
        public static BoolSetting ConfirmCancel = _settings.Create( "NNTP", "ConfirmCancel", true );
        public static BoolSetting ExtraFooterLF = _settings.Create( "NNTP", "ExtraFooterLF", true );
        public static BoolSetting Trace = _settings.Create( "NNTP", "Trace", false );
        public static IntSetting DeliverNewsPeriod = _settings.Create( "NNTP", "DeliverNewsPeriod", 15 );
        public static IntSetting ArticlesPerGroup = _settings.Create( "NNTP", "ArticlesPerGroup", 300 );
        public static IntSetting DeleteFolderType = _settings.Create( "NNTP", "DeleteFolderType", 0 );
        public static StringSetting Charset = _settings.Create( "NNTP", "Charset", string.Empty );
        public static StringSetting Format = _settings.Create( "NNTP", "Format", "MIME" );
        public static StringSetting EncodeTextWith = _settings.Create( "NNTP", "EncodeTextWith", "Quoted-Printable" );
        #endregion

        public static void LoadSettings()
        {
            _settings.LoadSettings();
        }
    }
    internal class MultiSettings
    {
        private readonly IResourceList _servers;
        public MultiSettings( IResourceList servers )
        {
            _servers = servers;
        }
        public Setting ArticlesPerGroup
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propCountToDownloadAtTime, Settings.ArticlesPerGroup );
            }
        }
        public Setting Port
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propPort, 119 );
            }
        }
        public Setting Ssl3Enabled
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propSsl3Enabled, false );
            }
        }
        public Setting DeliverOnStartup
        {
            get
            {
                return SettingArray.IntAsBoolFromResourceList( _servers, NntpPlugin._propDeliverOnStartup, Settings.DeliverOnStartup );
            }
        }
        public Setting MarkFromMeAsRead
        {
            get
            {
                return SettingArray.IntAsBoolFromResourceList( _servers, NntpPlugin._propMarkFromMeAsRead, Settings.MarkFromMeAsRead );
            }
        }
        public Setting PutInOutbox
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propPutInOutbox, false );
            }
        }

        public Setting AbbreviateLevel
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propAbbreviateLevel, 0,
                                                      ServerResource.AbbreviateLevelChanged );
            }
        }
        public Setting DeliverNewsPeriod
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propDeliverFreq, Settings.DeliverNewsPeriod );
            }
        }
        public Setting DownloadBodiesOnDeliver
        {
            get
            {
                return SettingArray.IntAsBoolFromResourceList( _servers, NntpPlugin._propDownloadBodiesOnDeliver, Settings.DownloadBodiesOnDeliver );
            }
        }
        public Setting DownloadBodyOnSelection
        {
            get
            {
                return SettingArray.IntAsBoolFromResourceList( _servers, NntpPlugin._propDownloadBodyOnSelection, Settings.DownloadBodyOnSelection );
            }
        }
        public Setting UserDisplayName
        {
            get
            {
                string Default = Core.ContactManager.MySelf.Resource.DisplayName;
                return SettingArray.FromResourceList( _servers, NntpPlugin._propUserDisplayName, Default, true );
            }
        }
        public Setting UserEmailAddress
        {
            get
            {
                string Default = Core.ContactManager.MySelf.DefaultEmailAddress;
                return SettingArray.FromResourceList( _servers, NntpPlugin._propEmailAddress, Default, true );
            }
        }
        public Setting LoginName
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propUsername, "", true );
            }
        }
        public Setting Password
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propPassword, "", true );
            }
        }

        public Setting UseSignature
        {
            get
            {
                return SettingArray.IntAsBoolFromResourceList( _servers, NntpPlugin._propUseSignature,
                                                               QuoteSettings.Default.UseSignature );
            }
        }

        public Setting Signature
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propMailSignature, QuoteSettings.Default.Signature, true );
            }
        }

        public Setting SignatureInReplies
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propReplySignaturePosition,
                                                      (int)QuoteSettings.Default.SignatureInReplies );
            }
        }

        public Setting EncodeTextWith
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propMIMETextEncoding, Settings.EncodeTextWith, true );
            }
        }
        public Setting Charset
        {
            get
            {
                return SettingArray.FromResourceList( _servers, Core.FileResourceManager.PropCharset, Settings.Charset, true );
            }
        }
        public Setting Format
        {
            get
            {
                return SettingArray.FromResourceList( _servers, NntpPlugin._propMailFormat, Settings.Format, true );
            }
        }
    }
}
