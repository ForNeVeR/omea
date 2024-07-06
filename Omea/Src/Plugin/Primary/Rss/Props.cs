// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
    internal class Props
    {
        internal const string RSSFeedResource = "RSSFeed";
        internal const string RSSItemResource = "RSSItem";
        internal const string RSSFeedGroupResource = "RSSFeedGroup";
        internal const string RSSSearchEngineResource = "RSSSearchEngine";
        internal const string RSSLinkedPostResource = "RSSLinkedPost";

        private static int _propRSSItem;
        private static int _propURL;
        private static int _propOriginalName;
        private static int _propLink;
        private static int _propGUID;
        private static int _propHomePage;
        private static int _propDescription;
        private static int _propLanguage;
        private static int _propFeedComment;
        private static int _propFeedComment2Feed;
        private static int _propItemComment;
        private static int _propItemCommentFeed;
        private static int _propCommentRSS;
        private static int _propWfwComment;
        private static int _propCommentURL;
        private static int _propAutoUpdateComments;
        private static int _propEnclosureURL;
        private static int _propEnclosureSize;
        private static int _propEnclosureType;
        private static int _propEnclosureFailureReason;
        private static int _propEnclosureTempFile;
        private static int _propHttpUserName;
        private static int _propHttpPassword;
        private static int _propETag;
        private static int _propUpdateStatus;
        private static int _propIsPaused;
        private static int _propUpdatePeriod;
        private static int _propUpdateFrequency;
        private static int _propRSSCategory;
        private static int _propLastUpdateTime;
        private static int _propDownloadDate;
        private static int _propTransient;
        private static PropId<int> _propCommentCount;
        private static int _propSelectedRSSItem;
        private static int _propSummary;
        private static int _propDefaultDesktopAlertRule;
        private static int _propWeblog;
        private static int _propImageTitle;
        private static int _propImageURL;
        private static int _propImageLink;
        private static int _propAuthor;
        private static int _propAuthorEmail;
        private static int _propDeletedItemHashList;
        private static int _propIndexInFeed;
        private static int _propLastItemIndex;
        private static int _propLinkList;
        private static int _propImageContent;
        private static int _propRSSSourceTag;
        private static int _propRSSSourceTagUrl;
        private static int _propRSSSearchPhrase;
        private static int _propEnclosureDownloadingState;
        private static int _propEnclosurePath;
        private static int _propEnclosureDownloadedSize;
        private static int _propPubDate;
        private static int _propDateModified;
        private static int _propRssLongBodyCRC;
        private static int _propUniqueLinks;
        private static int _propMarkReadOnLeave;
        private static int _propAutoFollowLink;
        private static int _propDisableCompression;
        private static int _propAllowEqualPosts;
        private static int _propAutoDownloadEncl;
        private static int _propLinkBase;
        private static int _propLinkedPost;
        internal static int _propFake;

        internal static int RSSItem { get { return _propRSSItem; } }
        internal static int URL { get { return _propURL; } }
        internal static int OriginalName { get { return _propOriginalName; } }
        internal static int Link { get { return _propLink; } }
        internal static int GUID { get { return _propGUID; } }
        internal static int HomePage { get { return _propHomePage; } }
        internal static int Description { get { return _propDescription; } }
        internal static int Language { get { return _propLanguage; } }
        internal static int FeedComment { get { return _propFeedComment; } }
        internal static int FeedComment2Feed { get { return _propFeedComment2Feed; } }
        internal static int ItemComment { get { return _propItemComment; } }
        internal static int ItemCommentFeed { get { return _propItemCommentFeed; } }
        internal static int CommentRSS { get { return _propCommentRSS; } }
        internal static int WfwComment { get { return _propWfwComment; } }
        internal static int CommentURL { get { return _propCommentURL; } }
        internal static int AutoUpdateComments { get { return _propAutoUpdateComments; } }
        internal static int EnclosureURL { get { return _propEnclosureURL; } }
        internal static int EnclosureSize { get { return _propEnclosureSize; } }
        internal static int EnclosureType { get { return _propEnclosureType; } }
        internal static int EnclosureFailureReason { get { return _propEnclosureFailureReason; } }
        internal static int EnclosureTempFile { get { return _propEnclosureTempFile; } }
        internal static int HttpUserName { get { return _propHttpUserName; } }
        internal static int HttpPassword { get { return _propHttpPassword; } }
        internal static int ETag { get { return _propETag; } }
        internal static int UpdateStatus { get { return _propUpdateStatus; } }
        internal static int IsPaused { get { return _propIsPaused; } }
        internal static int UpdatePeriod { get { return _propUpdatePeriod; } }
        internal static int UpdateFrequency { get { return _propUpdateFrequency; } }
        internal static int RSSCategory { get { return _propRSSCategory; } }
        internal static int LastUpdateTime { get { return _propLastUpdateTime; } }
        internal static int DownloadDate { get { return _propDownloadDate; } }
        internal static int Transient { get { return _propTransient; } }
        internal static PropId<int> CommentCount { get { return _propCommentCount; } }
        internal static int SelectedRSSItem { get { return _propSelectedRSSItem; } }
        internal static int Summary { get { return _propSummary; } }
        internal static int DefaultDesktopAlertRule { get { return _propDefaultDesktopAlertRule; } }
        internal static int Weblog { get { return _propWeblog; } }
        internal static int ImageTitle { get { return _propImageTitle; } }
        internal static int ImageURL { get { return _propImageURL; } }
        internal static int ImageLink { get { return _propImageLink; } }
        internal static int Author { get { return _propAuthor; } }
        internal static int AuthorEmail { get { return _propAuthorEmail; } }
        internal static int DeletedItemHashList { get { return _propDeletedItemHashList; } }
        internal static int IndexInFeed { get { return _propIndexInFeed; } }
        internal static int LastItemIndex { get { return _propLastItemIndex; } }
        internal static int LinkList { get { return _propLinkList; } }
        internal static int ImageContent { get { return _propImageContent; } }
        internal static int RSSSourceTag { get { return _propRSSSourceTag; } }
        internal static int RSSSourceTagUrl { get { return _propRSSSourceTagUrl; } }
        internal static int RSSSearchPhrase { get { return _propRSSSearchPhrase; } }
        internal static int EnclosureDownloadingState { get { return _propEnclosureDownloadingState; } }
        internal static int EnclosurePath { get { return _propEnclosurePath; } }
        internal static int EnclosureDownloadedSize { get { return _propEnclosureDownloadedSize; } }
        internal static int PubDate { get { return _propPubDate; } }
        internal static int DateModified { get { return _propDateModified; } }
        internal static int RssLongBodyCRC { get { return _propRssLongBodyCRC; } }
        internal static int UniqueLinks { get { return _propUniqueLinks; } }
        internal static int MarkReadOnLeave { get { return _propMarkReadOnLeave; } }
        internal static int AutoFollowLink { get { return _propAutoFollowLink; } }
        internal static int DisableCompression { get { return _propDisableCompression; } }
        internal static int AllowEqualPosts { get { return _propAllowEqualPosts; } }
        internal static int AutoDownloadEnclosure { get { return _propAutoDownloadEncl; } }
        internal static int LinkBase { get { return _propLinkBase; } }
        internal static int LinkedPost { get { return _propLinkedPost; } }

        internal static void Register( IPlugin ownerPlugin )
        {
            IResourceStore store = Core.ResourceStore;
            _propRSSItem = store.PropTypes.Register( "RSSItem", PropDataType.Link,
                PropTypeFlags.CountUnread | PropTypeFlags.DirectedLink );
            store.PropTypes.RegisterDisplayName( _propRSSItem, "Posts", "Weblog" );

            _propURL = store.PropTypes.Register( "URL", PropDataType.String );
            store.PropTypes.RegisterDisplayName( _propURL, "U R L" );

            _propOriginalName = store.PropTypes.Register( "OriginalName", PropDataType.String,
                PropTypeFlags.Internal );
            _propLink = store.PropTypes.Register( "Link", PropDataType.String );

            _propGUID = store.PropTypes.Register( "GUID", PropDataType.String,
                PropTypeFlags.Internal );
            _propHomePage = store.PropTypes.Register( "HomePage", PropDataType.String );
            store.PropTypes.RegisterDisplayName( _propHomePage, "Home Page" );

            _propDescription = store.PropTypes.Register( "Description", PropDataType.String );

            _propLanguage = store.PropTypes.Register( "Language", PropDataType.String );

            _propFeedComment = store.PropTypes.Register( "FeedComment", PropDataType.Link,
                PropTypeFlags.Internal | PropTypeFlags.DirectedLink );
            _propFeedComment2Feed = store.PropTypes.Register( "FeedComment2Feed", PropDataType.Link,
                PropTypeFlags.Internal | PropTypeFlags.DirectedLink );
            _propItemComment = store.PropTypes.Register( "ItemComment", PropDataType.Link,
                PropTypeFlags.Internal | PropTypeFlags.DirectedLink );
            _propItemCommentFeed = store.PropTypes.Register( "ItemCommentFeed", PropDataType.Link,
                PropTypeFlags.Internal | PropTypeFlags.DirectedLink );
            _propCommentRSS = store.PropTypes.Register( "CommentRSS", PropDataType.String,
                PropTypeFlags.Internal );
            _propWfwComment = store.PropTypes.Register( "WfwComment", PropDataType.String,
                PropTypeFlags.Internal );
            _propCommentURL = store.PropTypes.Register( "CommentURL", PropDataType.String,
                PropTypeFlags.Internal );
            _propAutoUpdateComments = store.PropTypes.Register( "AutoUpdateComments", PropDataType.Bool,
                PropTypeFlags.Internal );
            _propEnclosureURL = store.PropTypes.Register( "EnclosureURL", PropDataType.String );
            store.PropTypes.RegisterDisplayName( _propEnclosureURL, "Enclosure U R L" );

            _propEnclosureSize = store.PropTypes.Register( "EnclosureSize", PropDataType.Int );
            store.PropTypes.RegisterDisplayName( _propEnclosureSize, "Enclosure Size" );

            _propEnclosureType = store.PropTypes.Register( "EnclosureType", PropDataType.String );
            store.PropTypes.RegisterDisplayName( _propEnclosureType, "Enclosure Type" );

            _propEnclosureFailureReason = store.PropTypes.Register( "EnclosureFailureReason", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propEnclosureTempFile = store.PropTypes.Register( "EnclosureTempFile", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propHttpUserName = store.PropTypes.Register( "HttpUserName", PropDataType.String,
                PropTypeFlags.Internal );
            _propHttpPassword = store.PropTypes.Register( "HttpPassword", PropDataType.String,
                PropTypeFlags.Internal );
            _propETag = store.PropTypes.Register( "ETag", PropDataType.String,
                PropTypeFlags.Internal );
            _propUpdateStatus = store.PropTypes.Register( "UpdateStatus", PropDataType.String,
                PropTypeFlags.Internal );
            store.PropTypes.RegisterDisplayName( Core.Props.LastError, "Last Error" );

            _propIsPaused = store.PropTypes.Register( "IsPaused", PropDataType.Bool,
                PropTypeFlags.Internal );
            _propUpdatePeriod = store.PropTypes.Register( "UpdatePeriod", PropDataType.String,
                PropTypeFlags.Internal );
            _propUpdateFrequency = store.PropTypes.Register( "UpdateFrequency", PropDataType.Int,
                PropTypeFlags.Internal );
            _propRSSCategory = store.PropTypes.Register( "RSSCategory", PropDataType.String );
            store.PropTypes.RegisterDisplayName( _propRSSCategory, "Pub. Category" );

            _propLastUpdateTime = store.PropTypes.Register( "LastUpdateTime", PropDataType.Date,
                PropTypeFlags.Internal );
            _propDownloadDate = store.PropTypes.Register( "DownloadDate", PropDataType.Date );
            store.PropTypes.RegisterDisplayName( _propDownloadDate, "Download Date" );

            _propTransient = store.PropTypes.Register( "Transient", PropDataType.Int,
                PropTypeFlags.Internal );
            _propCommentCount = store.PropTypes.Register( "CommentCount", PropDataTypes.Int );
            store.PropTypes.RegisterDisplayName( _propCommentCount.Id, "Comment Count" );

            _propSelectedRSSItem = store.PropTypes.Register( "SelectedRSSItem", PropDataType.Link,
                PropTypeFlags.Internal );
            _propSummary = store.PropTypes.Register( "Summary", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propDefaultDesktopAlertRule = store.PropTypes.Register( "DefaultDesktopAlertRule", PropDataType.Bool,
                PropTypeFlags.Internal );
            _propWeblog = store.PropTypes.Register( "Weblog", PropDataType.Link,
                PropTypeFlags.DirectedLink );
            store.PropTypes.RegisterDisplayName( _propWeblog, "Weblog", "Author" );

            _propImageTitle = store.PropTypes.Register( "ImageTitle", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propImageURL = store.PropTypes.Register( "ImageURL", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propImageLink = store.PropTypes.Register( "ImageLink", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propAuthor = store.PropTypes.Register( "Author", PropDataType.String );

            _propAuthorEmail = store.PropTypes.Register( "AuthorEmail", PropDataType.Link );
            store.PropTypes.RegisterDisplayName( _propAuthorEmail, "Author Email" );

            _propDeletedItemHashList = store.PropTypes.Register( "DeletedItemHashList", PropDataType.StringList,
                PropTypeFlags.Internal );
            _propIndexInFeed = store.PropTypes.Register( "IndexInFeed", PropDataType.Int,
                PropTypeFlags.Internal );
            _propLastItemIndex = store.PropTypes.Register( "LastItemIndex", PropDataType.Int,
                PropTypeFlags.Internal );
            _propLinkList = store.PropTypes.Register( "LinkList", PropDataType.StringList,
                PropTypeFlags.Internal );
            _propImageContent = store.PropTypes.Register( "ImageContent", PropDataType.Blob,
                PropTypeFlags.Internal );
            _propRSSSourceTag = store.PropTypes.Register( "RSSSourceTag", PropDataType.String );
            store.PropTypes.RegisterDisplayName( _propRSSSourceTag, "RSS Source" );

            _propRSSSourceTagUrl = store.PropTypes.Register( "RSSSourceTagUrl", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propRSSSearchPhrase = store.PropTypes.Register( "RSSSearchPhrase", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propEnclosureDownloadingState = store.PropTypes.Register( "EnclosureDownloadingState", PropDataType.Int );
            store.PropTypes.RegisterDisplayName( _propEnclosureDownloadingState, "Enclosure Downloading State" );

            _propEnclosurePath = store.PropTypes.Register( "EnclosurePath", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propEnclosureDownloadedSize = store.PropTypes.Register( "EnclosureDownloadedSize", PropDataType.Int,
                PropTypeFlags.Internal );
            _propPubDate = store.PropTypes.Register( "PubDate", PropDataType.Date );
            store.PropTypes.RegisterDisplayName( _propPubDate, "Pub Date" );

            _propDateModified = store.PropTypes.Register( "DateModified", PropDataType.Date );
            store.PropTypes.RegisterDisplayName( _propDateModified, "Date Modified" );

            _propRssLongBodyCRC = store.PropTypes.Register( "RssLongBodyCRC", PropDataType.Int, PropTypeFlags.Internal );
            _propUniqueLinks = store.PropTypes.Register( "UniqueLinks", PropDataType.Int, PropTypeFlags.Internal );
            _propMarkReadOnLeave = store.PropTypes.Register( "MarkReadOnLeave", PropDataType.Bool, PropTypeFlags.Internal );
            _propAutoFollowLink = store.PropTypes.Register( "AutoFollowLink", PropDataType.Bool, PropTypeFlags.Internal );
            _propDisableCompression = store.PropTypes.Register( "DisableCompression", PropDataType.Bool, PropTypeFlags.Internal );
            _propAllowEqualPosts = store.PropTypes.Register( "AllowEqualPosts", PropDataType.Bool, PropTypeFlags.Internal );
            _propAutoDownloadEncl = store.PropTypes.Register( "AutoDownloadEnclosures", PropDataType.Bool, PropTypeFlags.Internal );
            _propLinkBase = store.PropTypes.Register( "LinkBase", PropDataType.String, PropTypeFlags.Internal );
            _propLinkedPost = store.PropTypes.Register( "LinkedPost", PropDataType.Link, PropTypeFlags.Internal | PropTypeFlags.DirectedLink );

            _propFake = store.PropTypes.Register( "FakeLinkProp", PropDataType.Int, PropTypeFlags.Internal );

            store.ResourceTypes.Register( RSSFeedResource, "RSS/ATOM Feed", "Name UpdateStatus",
                ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal, ownerPlugin );
            store.ResourceTypes.Register( RSSItemResource, "RSS/ATOM Post", Core.ResourceStore.PropTypes [Core.Props.Subject].Name,
                ResourceTypeFlags.CanBeUnread, ownerPlugin );
            store.ResourceTypes.Register( RSSFeedGroupResource, "Feed Folder", "Name",
                ResourceTypeFlags.ResourceContainer | ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal, ownerPlugin );
            store.ResourceTypes.Register( RSSSearchEngineResource, RSSSearchEngineResource, "Name",
                ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal, ownerPlugin );
            store.ResourceTypes.Register( RSSLinkedPostResource, RSSLinkedPostResource, "Name",
                ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal, ownerPlugin );

            store.RegisterUniqueRestriction( RSSSearchEngineResource, _propURL );
            store.RegisterLinkRestriction( RSSFeedResource, _propRSSItem, RSSItemResource, 0, Int32.MaxValue );
            store.RegisterLinkRestriction( RSSFeedResource, _propFeedComment, RSSItemResource, 0, Int32.MaxValue );
            store.RegisterLinkRestriction( RSSItemResource, _propItemCommentFeed, RSSFeedResource, 0, 1 );
            store.RegisterLinkRestriction( RSSFeedResource, _propSelectedRSSItem, RSSItemResource, 0, 1 );
            store.RegisterLinkRestriction( RSSFeedResource, _propAuthorEmail, "EmailAccount", 0, 1 );
        }
    }
}
