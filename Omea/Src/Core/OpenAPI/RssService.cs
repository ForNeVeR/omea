// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Xml;

namespace JetBrains.Omea.OpenAPI
{
	/// <summary>
	/// Importer of subscription from other RSS readers.
	/// </summary>
	/// <since>2.0</since>
	public interface IFeedImporter
	{
		/// <summary>
		/// Check if importer needs configuration before import starts.
		/// </summary>
		bool HasSettings { get; }

		/// <summary>
		/// Returns creator of options pane.
		/// </summary>
		OptionsPaneCreator GetSettingsPaneCreator();

		/// <summary>
		/// Import subscription
		/// </summary>
		/// <param name="importRoot">Link imported structure to this parent.</param>
		/// <param name="addToWorkspace">Add imported feeds to active workspace</param>
		void DoImport( IResource importRoot, bool addToWorkspace );

        /// <summary>
        /// Import cached items, flags, etc.
        /// </summary>
        void DoImportCache();
	}

    /// <summary>
    /// Parser for a single element in an RSS or ATOM feed.
    /// </summary>
    public interface IFeedElementParser
    {
        /// <summary>
        /// Parses the value for the element.
        /// </summary>
        /// <param name="resource">The resource in which the element data should be stored
        /// (of type RSSFeed for channel element parsers or RSSItem for item element
        /// parsers).</param>
        /// <param name="reader">The reader positioned after the starting tag of the element.</param>
        void ParseValue( IResource resource, XmlReader reader );

        /// <summary>
        /// Checks if a Read() call is needed to move to the next element after
        /// <see cref="ParseValue"/> is completed.
        /// </summary>
        bool SkipNextRead { get; }
    }

    /// <summary>
    /// Possible types of feeds for which element parsers can be registered.
    /// </summary>
    public enum FeedType
    {
        /// <summary>
        /// RSS Feed.
        /// </summary>
        Rss,

        /// <summary>
        /// Atom Feed.
        /// </summary>
        Atom
    };

    /// <summary>
	/// Service for performing operations on RSS feeds in Omea.
	/// </summary>
	public interface IRssService
	{
        /// <summary>
        /// Shows the "Subscribe to Feed" wizard, optionally filling it with default
        /// values.
        /// </summary>
        /// <param name="defaultUrl">The URL which is suggested in the dialog,
        /// or null if the URL should be empty.</param>
        /// <param name="defaultGroup">The group where it is suggested to place
        /// the feed, or null if the default location is used.</param>
        void ShowAddFeedWizard( string defaultUrl, IResource defaultGroup );

        /// <summary>
        /// Creates an RSS feed with the specified name and URL under the specified parent.
        /// </summary>
        /// <param name="name">The name of the feed.</param>
        /// <param name="url">The URL of the feed.</param>
        /// <param name="parent">The parent group of the feed, or null if the feed is created
        /// at root level.</param>
        /// <returns>The created feed resource.</returns>
        /// <since>2.0</since>
        IResource CreateFeed( string name, string url, IResource parent );

        /// <summary>
        /// Creates an RSS feed with the specified name and URL under the specified parent,
        /// with the specified authorization parameters.
        /// </summary>
        /// <param name="name">The name of the feed.</param>
        /// <param name="url">The URL of the feed.</param>
        /// <param name="parent">The parent group of the feed, or null if the feed is created
        /// at root level.</param>
        /// <param name="httpLogin">The HTTP login name used for updating the feed.</param>
        /// <param name="httpPassword">The HTTP password used for updating the feed.</param>
        /// <returns>The created feed resource.</returns>
        /// <since>2.0</since>
        IResource CreateFeed( string name, string url, IResource parent, string httpLogin, string httpPassword );

        /// <summary>
        /// Finds an existing feed group with the specified name, or creates a new feed group if
        /// one does not exist.
        /// </summary>
        /// <param name="name">The name of the group.</param>
        /// <param name="parent">The parent under which the group is created, or null
        /// if the group is created under the feed root.</param>
        /// <returns>The group resource.</returns>
        /// <since>2.0</since>
        IResource FindOrCreateGroup( string name, IResource parent );

        /// <summary>
        /// Queues the specified RSS feed for immediate updating.
        /// </summary>
        /// <param name="feed">The feed to update.</param>
        void QueueFeedUpdate( IResource feed );

        /// <summary>
        /// Initiates an update of the selected feed at a time determined by its
        /// last update time and update frequency.
        /// </summary>
        /// <param name="feed">The feed scheduled for updating.</param>
        void ScheduleFeedUpdate( IResource feed );

        /// <summary>
        /// Imports an OPML list of feeds from a stream.
        /// </summary>
        /// <param name="importStream">The stream containing the OPML.</param>
        /// <param name="importRoot">The root group under which the imported feeds are place,
        /// or null if the feeds should be placed under the root of the feed tree.</param>
        /// <param name="importFileName">The name or URL of the OPML file (used for
        /// diagnostic messages only).</param>
        /// <param name="importPreview">Whether a preview dialog should be shown before
        /// completing the import.</param>
        void ImportOpmlStream( Stream importStream, IResource importRoot,
            string importFileName, bool importPreview );

        /// <summary>
        /// Exports the subscription tree or part of it to an OPML file.
        /// </summary>
        /// <param name="exportRoot">The root of the subtree to export, or null
        /// if the entire RSS tree should be exported.</param>
        /// <param name="exportFileName">The name of the file to which the OPML is exported.</param>
        void ExportOpmlFile( IResource exportRoot, string exportFileName );

        /// <summary>
        /// Register a parser for a custom channel-level element of an RSS or ATOM feed.
        /// </summary>
        /// <param name="feedType">The type of the feed (RSS or ATOM) for which the
        /// parser is registered.</param>
        /// <param name="xmlNameSpace">The XML namespace of the element which is handled
        /// by the parser.</param>
        /// <param name="elementName">The tag name of the element which is handled by
        /// the parser.</param>
        /// <param name="parser">The parser instance.</param>
        void RegisterChannelElementParser( FeedType feedType, string xmlNameSpace, string elementName,
            IFeedElementParser parser );

        /// <summary>
        /// Register a parser for a custom item-level element of an RSS or ATOM feed.
        /// </summary>
        /// <param name="feedType">The type of the feed (RSS or ATOM) for which the
        /// parser is registered.</param>
        /// <param name="xmlNameSpace">The XML namespace of the element which is handled
        /// by the parser.</param>
        /// <param name="elementName">The tag name of the element which is handled by
        /// the parser.</param>
        /// <param name="parser">The parser instance.</param>
        void RegisterItemElementParser( FeedType feedType, string xmlNameSpace, string elementName,
            IFeedElementParser parser );

		/// <summary>
		/// Register new subscription importer. Importer will be available to user in startup wizard
		/// and options pane.
		/// </summary>
		/// <param name="name">the name of importer, will be shown to user.</param>
		/// <param name="importer">The importer instance.</param>
		/// <since>2.0</since>
		void RegisterFeedImporter( string name, IFeedImporter importer );

        /// <summary>
        /// An event which is fired on the resource thread when the update of an RSS or ATOM feed is completed.
        /// </summary>
        /// <remarks>
        /// <para>As the event is fired on the resource thread, you don't need to marshal non-read-only
        /// resource operations by using the <see cref="ResourceProxy"/>. All the resource modifications
        /// can be made from the current thread.</para>
        /// <para>However, thus the UI operations must be marshalled to the UI thread.
        /// This can be done by either calling <see cref="System.Windows.Forms.Control.BeginInvoke"/>
        /// or using the <see cref="IUIManager.QueueUIJob"/>. You should not call the synchronous
        /// <see cref="System.Windows.Forms.Control.Invoke"/> from the resource thread.</para>
        /// </remarks>
        event ResourceEventHandler FeedUpdated;

		/// <summary><seealso cref="IsDoingUpdateAll"/><seealso cref="UpdateAllFinished"/><seealso cref="UpdateAll"/>
		/// Fires on the <see cref="Core.UserInterfaceAP">UI thread</see> when a manual “Update All Feeds” action is executed.
		/// </summary>
		/// <remarks>The event is async and imposes no limits on user calls from the handler.</remarks>
		/// <since>2.1</since>
		event EventHandler UpdateAllStarted;

		/// <summary><seealso cref="IsDoingUpdateAll"/><seealso cref="UpdateAllStarted"/><seealso cref="UpdateAll"/>
		/// Fires on the <see cref="Core.UserInterfaceAP">UI thread</see> when the “Update All Feeds” action completes updating all its feeds.
		/// </summary>
		/// <remarks>The event is async and imposes no limits on user calls from the handler.</remarks>
		/// <since>2.1</since>
		event EventHandler UpdateAllFinished;

		/// <summary><seealso cref="UpdateAllStarted"/><seealso cref="UpdateAllFinished"/><seealso cref="UpdateAll"/>
		/// Gets whether the “Update All Feeds” action is currently updating its feeds.
		/// </summary>
		/// <since>2.1</since>
		bool IsDoingUpdateAll { get; }

		/// <summary><seealso cref="IsDoingUpdateAll"/><seealso cref="UpdateAllStarted"/><seealso cref="UpdateAllFinished"/>
		/// Attempts to start updating all the feeds.
		/// </summary>
		/// <returns>Whether an update-all was initiated successfully.</returns>
		/// <remarks><para>The attempt succeeds if an “update all feeds” process is not currently running. In this case the function returns <c>True</c>.</para>
		/// <para>Otherwise, all the feeds are not queued for update again and <c>False</c> is returned.</para></remarks>
		bool UpdateAll();
	}
}
