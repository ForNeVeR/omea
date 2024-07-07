// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Net;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Queue for updating RSS feeds, allowing to limit the number of feeds updated at the same time.
	/// </summary>
    internal class FeedUpdateQueue
    {
        private int _updatingCount = 0;
        private int _maxCount = 10;
        private PriorityQueue _pendingFeeds = new PriorityQueue();

        internal event ResourceEventHandler FeedUpdated;

		/// <summary>
		/// Fires when either all the feeds queued for update are thru,
		/// or a single feed has been updated without ever getting into the queue.
		/// </summary>
		internal event EventHandler QueueGotEmpty;

        internal FeedUpdateQueue()
        {
            // if we're using a proxy, all the connections will go through one ServicePoint,
            // so it does not make sense to set a queue size larger than the maximum connection
            // limit on the proxy
            WebProxy defaultProxy = GlobalProxySelection.Select as WebProxy;
            if ( defaultProxy != null && defaultProxy.Address != null )
            {
                _maxCount = ServicePointManager.DefaultConnectionLimit;
            }
        }

		/// <summary>
		/// Initiates an update of the selected feed at a time determined by its
		/// last update time and update frequency.
		/// </summary>
        public void ScheduleFeedUpdate( IResource feed )
        {
            string updatePeriod = feed.GetStringProp( Props.UpdatePeriod );
            int updateFrequency = feed.GetIntProp( Props.UpdateFrequency );
            if ( updateFrequency > 0 )
            {
                DateTime dt = feed.GetDateProp( Props.LastUpdateTime );
                switch ( updatePeriod )
                {
                    case UpdatePeriods.Daily:
                        dt = dt.AddDays( updateFrequency );
                        break;
                    case UpdatePeriods.Weekly:
                        dt = dt.AddDays( 7 * updateFrequency );
                        break;
                    case UpdatePeriods.Minutely:
                        dt = dt.AddMinutes( updateFrequency );
                        break;
                    default:
                        dt = dt.AddHours( updateFrequency );
                        break;
                }

                Core.NetworkAP.QueueJobAt( dt, new ResourceDelegate( QueueFeedUpdate ), feed );
            }
        }

        private delegate void QueueFeedUpdateDelegate( IResource feed, int attempt );

		/// <summary>
		/// Queues the specified feed for an immediate update.
		/// </summary>
        public void QueueFeedUpdate( IResource feed, JobPriority jobPriority )
        {
            QueueFeedUpdate( feed, 0, jobPriority );
        }
        public void QueueFeedUpdate( IResource feed )
        {
            QueueFeedUpdate( feed, JobPriority.Normal );
        }

        private class PendingFeed
        {
            private int _attempt;
            private IResource _feed;
            public PendingFeed( IResource feed, int attempt )
            {
                _feed = feed;
                _attempt = attempt;
            }
            public IResource Feed { get { return _feed; } }
            public int Attempt { get { return _attempt; } }
        }

        private void QueueFeedUpdate( IResource feed, int attempt )
        {
            QueueFeedUpdate( feed, attempt, JobPriority.Normal );
        }

        private void QueueFeedUpdate( IResource feed, int attempt, JobPriority jobPriority )
        {
            if ( feed.Type != "RSSFeed" )
            {
                throw new ArgumentException( "Invalid resource type for QueueFeedUpdate: " + feed.Type );
            }

            if ( !HttpReader.IsSupportedProtocol( feed.GetPropText( Props.URL ) ) )
            {
                return;
            }

            //  Do not update feeds which were manually set into
            //  hybernating state.
            if( feed.HasProp( Props.IsPaused ) )
            {
                return;
            }

            lock ( this )
            {
                if ( _updatingCount >= _maxCount )
                {
                    _pendingFeeds.Push( (int) jobPriority, new PendingFeed( feed, attempt ) );
                }
                else
                {
                    RSSUnitOfWork uow = new RSSUnitOfWork( feed, true, false );
                    uow.Attempts = attempt;
                    uow.ParseDone += new EventHandler( OnRSSParseDone );
                    Core.NetworkAP.QueueJob( jobPriority, uow );
                    _updatingCount++;
                }
            }
            if ( feed.HasProp( Props.AutoUpdateComments ) )
            {
                foreach ( IResource commentFeed in feed.GetLinksTo( null, Props.FeedComment2Feed ) )
                {
                    if ( NeedUpdate( commentFeed ) )
                    {
                        QueueFeedUpdate( commentFeed );
                    }
                }
            }
        }

        private bool NeedUpdate( IResource commentFeed )
        {
            Guard.NullArgument( commentFeed, "commentFeed" );
            IResourceList rssItems = commentFeed.GetLinksOfType( "RSSItem", Props.RSSItem );
            rssItems.Sort( new SortSettings( Core.Props.Date, false ) );

            IResource lastItem;
            if ( rssItems.Count == 0 )
            {
                lastItem = commentFeed.GetLinkProp( Props.ItemCommentFeed );
            }
            else
            {
                lastItem = rssItems[ 0 ];
            }
            DateTime dt = lastItem.GetDateProp( Core.Props.Date );

            string updatePeriod = Settings.StopUpdatePeriod;
            int updateFrequency = Settings.StopUpdateFrequency;
            if ( updateFrequency > 0 )
            {
                switch ( updatePeriod )
                {
                    case UpdatePeriods.Daily:
                        dt = dt.AddDays( updateFrequency );
                        break;
                    case UpdatePeriods.Weekly:
                        dt = dt.AddDays( 7 * updateFrequency );
                        break;
                    case UpdatePeriods.Minutely:
                        dt = dt.AddMinutes( updateFrequency );
                        break;
                    default:
                        dt = dt.AddHours( updateFrequency );
                        break;
                }
            }
            return DateTime.Now < dt;
        }

        public static void CleanupCommentFeed( RSSUnitOfWork uow )
        {
            IResource commentItem = uow.Feed.GetLinkProp( Props.ItemCommentFeed );
            if ( commentItem == null )
            {
                return;
            }
            IResourceList comments = commentItem.GetLinksTo( "RSSItem", Props.ItemComment );
            int commentCount = comments.Count;
            foreach ( IResource existingComment in comments )
            {
                if ( existingComment.HasProp( Props.Transient ) )
                {
                    --commentCount;
                    new ResourceProxy( existingComment ).Delete();
                }
                else if ( commentItem.HasProp( Core.Props.IsDeleted ) )
                {
                    new ResourceProxy( existingComment ).SetProp( Core.Props.IsDeleted, true );
                }
            }
            new ResourceProxy( commentItem ).SetProp( Props.CommentCount, commentCount );
            new ResourceProxy( uow.Feed ).DeleteProp( Props.UpdateStatus );
        }

        private void OnRSSParseDone( object sender, EventArgs e )
        {
            RSSUnitOfWork uow = (RSSUnitOfWork)sender;
            uow.ParseDone -= new EventHandler( OnRSSParseDone );

            if ( uow.Status == RSSWorkStatus.FeedDeleted )
            {
                return;
            }

            ResourceProxy proxy = new ResourceProxy( uow.Feed );
            proxy.BeginUpdate();
            proxy.SetProp( Props.LastUpdateTime, DateTime.Now );
            if ( !uow.Feed.HasProp( Core.Props.Parent ) && !uow.Feed.HasProp( Props.ItemCommentFeed ) )
            {
                proxy.SetProp( Core.Props.Parent, RSSPlugin.RootFeedGroup );
            }
            if ( uow.Status == RSSWorkStatus.HTTPError || uow.Status == RSSWorkStatus.XMLError )
            {
                proxy.SetProp( Props.UpdateStatus, "(error)" );
                proxy.SetProp( Core.Props.LastError, uow.LastException.Message );
            }
            else
            {
                proxy.DeleteProp( Props.UpdateStatus );
                proxy.DeleteProp( Core.Props.LastError );
            }
            if ( uow.LastException is HttpDecompressException )
            {
                proxy.SetProp( Props.DisableCompression, true );
            }

            if ( uow.Status == RSSWorkStatus.HTTPError && uow.HttpStatus == HttpStatusCode.Gone )
            {
                proxy.SetProp( Props.UpdateFrequency, -1 );
            }
            else if ( !uow.Feed.HasProp( Props.UpdateFrequency ) )
            {
                proxy.SetProp( Props.UpdateFrequency, (int)Settings.UpdateFrequency );
            }
            if ( !uow.Feed.HasProp( Props.UpdatePeriod ) )
            {
                proxy.SetProp( Props.UpdatePeriod, (string)Settings.UpdatePeriod );
            }

            proxy.EndUpdate();

            if ( uow.Status == RSSWorkStatus.HTTPError && uow.Attempts < 3 )
            {
                Core.NetworkAP.QueueJobAt(
                    DateTime.Now.AddMinutes( 5 ),
                    new QueueFeedUpdateDelegate( QueueFeedUpdate ), uow.Feed, uow.Attempts + 1 );
            }

            if( uow.Status == RSSWorkStatus.Success && uow.Feed.HasProp( Props.AutoDownloadEnclosure ))
                ScheduleEnclosures( uow.Feed );

            CleanupCommentFeed( uow );
            ScheduleFeedUpdate( uow.Feed );

            if ( FeedUpdated != null )
            {
                FeedUpdated( this, new ResourceEventArgs( uow.Feed ) );
            }

            lock ( this )
            {
                _updatingCount--;
                while ( _pendingFeeds.Count > 0 )
                {
                    PendingFeed feed = (PendingFeed)_pendingFeeds.Pop();
                    if ( feed.Feed.IsDeleted )
                    {
                        continue;
                    }
                    QueueFeedUpdate( feed.Feed, feed.Attempt );
                    break;
                }
				// Has queue gotten empty?
				if((_pendingFeeds.Count == 0) && (_updatingCount == 0))
					Core.UserInterfaceAP.QueueJob( "Feeds Update Queue has Gotten Empty.", new MethodInvoker(FireQueueGotEmpty) );
            }
        }

		/// <summary>
		/// Fires the <see cref="QueueGotEmpty"/> event async.
		/// </summary>
		private void FireQueueGotEmpty()
		{
			try
			{
				if(QueueGotEmpty != null)
					QueueGotEmpty(this, EventArgs.Empty);
			}
			catch(Exception ex)
			{
				Core.ReportException( ex, ExceptionReportFlags.AttachLog );
			}
		}

        /// <summary>
        /// Queue for download those enclosures of the feed's unread items which
        /// are not yet downloaded or queued.
        /// </summary>
        /// <param name="feed">A feed resource which items are to be analyzed.</param>
        private void ScheduleEnclosures( IResource feed )
        {
            IResourceList items = feed.GetLinksOfType( Props.RSSItemResource, Props.RSSItem );
            items = items.Intersect( Core.ResourceStore.FindResourcesWithProp( Props.RSSItemResource, Core.Props.IsUnread ), true );
            items = items.Minus( Core.ResourceStore.FindResourcesWithProp( Props.RSSItemResource, Core.Props.IsDeleted ) );

            IResourceList itemsWithEncls = Core.ResourceStore.FindResources( null, Props.EnclosureDownloadingState, (int)EnclosureDownloadState.NotDownloaded );
            items = items.Intersect( itemsWithEncls, true );

            foreach( IResource item in items )
            {
                EnclosureDownloadManager.PlanToDownload( item );
            }
        }
    }
}
