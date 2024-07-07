// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using EnterpriseDT.Net.Ftp;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.Siam
{
	/// <summary>
	/// Siam Omea Plugin
	/// TODO: ensure that all the states execute in the proper thread.
	/// </summary>
	public class Plugin : AbstractNamedJob, IPlugin
	{
		public Plugin()
		{
		}

		/// <summary>
		/// The Siam synchronization data.
		/// Lifetime:
		///		* Sync In: after it is first retrieved and kept until all the mentioned feeds are finally processed.
		///		* Sync Out: while synchronizing out.
		///	At other moments must be null.
		/// </summary>
		protected XmlDocument _xmlSiam = null;

		/// <summary>
		/// StatusWriter that outputs messages to the status bar.
		/// </summary>
		protected IStatusWriter	_statusWriter = null;

		/// <summary>
		/// Current state of the plugin.
		/// Must be modified in the UI thread only, either by the StartSync… function or SyncTerminated.
		/// </summary>
		protected States _state = States.Idle;

		/// <summary>
		/// Possible states of the plugin
		/// </summary>
		protected enum States
		{
			/// <summary>
			/// Doing nothing.
			/// </summary>
			Idle,

			// // // // // // // // // //
			// Sync In

			/// <summary>
			/// Synchronize-in starts the download process.
			/// </summary>
			SyncIn_StartDownload,

			/// <summary>
			/// Sync-in encouters the received XML.
			/// </summary>
			SycnIn_DownloadCompleted,

			/// <summary>
			/// Sync-in loads and updates the feed list from XML data.
			/// </summary>
			SyncIn_SyncFeedList,

			/// <summary>
			/// Each feed is synchronized in (immediate sync).
			/// </summary>
			SyncIn_SyncFeedItems,

			/// <summary>
			/// Initializes the deferred sync facilities and waits for the feeds to update and be synced. When all the threads listed in _resFeeds are through, deferred sync terminates.
			/// </summary>
			SyncIn_StartDeferredSync,

			/// <summary>
			/// Deferred synchronization is handling individual feeds.
			/// Process all the items in the _resUpdatedFeeds and remove from this list, if they're also present in the _resFeeds (feeds to be synced) list, perform sync and remove from the _resFeeds synclist. If it gets empty, stop deferred sync.
			/// </summary>
			SyncIn_DeferredSyncFeedItem,

			// // // // // // // // // //
			// Sync Out

			/// <summary>
			/// SyncOut extracts the feed list into xml sync data and prepares for executing sync-out for each feed.
			/// </summary>
			SyncOut_SyncFeedList,

			/// <summary>
			/// Each feed is synchronized out.
			/// </summary>
			SyncOut_SyncFeedItems,

			/// <summary>
			/// Feeds sync done, starting uploading the results.
			/// </summary>
			SyncOut_StartUpload,

			/// <summary>
			/// Upload done, all's done.
			/// </summary>
			SyncOut_UploadCompleted,

			/// <summary>
			/// Sync has terminated, falling back to the idle state and displaying the results in the UI optionally.
			/// </summary>
			Terminated
		}

		/// <summary>
		/// Detects the stop-flag state.
		/// </summary>
		public bool MustStop { get { lock(this) return _bMustStop; } }

		/// <summary>
		/// Whenever this flag is on, execution must be aborted.
		/// Must be accessed through a lock on the Plugin object.
		/// </summary>
		protected bool	_bMustStop = false;

		/// <summary>
		/// Maps FSM states to FSM event handlers.
		/// </summary>
		protected Hashtable _hashStateToHandler;

		/// <summary>
		/// Invokes the FSM handler.
		/// </summary>
		protected delegate void FsmInvoker();

		/// <summary>
		/// Sets up the mapping of states to handlers.
		/// </summary>
		protected void SetupFSM()
		{
			_hashStateToHandler = new Hashtable();
			_hashStateToHandler.Add( States.Idle, new FsmInvoker( FSM_Idle ) );
			_hashStateToHandler.Add( States.SyncIn_StartDownload, new FsmInvoker( FSM_SyncIn_StartDownload ) );
			_hashStateToHandler.Add( States.SycnIn_DownloadCompleted, new FsmInvoker( FSM_SycnIn_DownloadCompleted ) );
			_hashStateToHandler.Add( States.SyncIn_SyncFeedList, new FsmInvoker( FSM_SyncIn_SyncFeedList ) );
			_hashStateToHandler.Add( States.SyncIn_SyncFeedItems, new FsmInvoker( FSM_SyncIn_SyncFeedItems ) );
			_hashStateToHandler.Add( States.SyncIn_StartDeferredSync, new FsmInvoker( FSM_SyncIn_StartDeferredSync ) );
			_hashStateToHandler.Add( States.SyncIn_DeferredSyncFeedItem, new FsmInvoker( FSM_SyncIn_DeferredSyncFeedItem ) );
			_hashStateToHandler.Add( States.SyncOut_SyncFeedList, new FsmInvoker( FSM_SyncOut_SyncFeedList ) );
			_hashStateToHandler.Add( States.SyncOut_SyncFeedItems, new FsmInvoker( FSM_SyncOut_SyncFeedItems ) );
			_hashStateToHandler.Add( States.SyncOut_StartUpload, new FsmInvoker( FSM_SyncOut_StartUpload ) );
			_hashStateToHandler.Add( States.SyncOut_UploadCompleted, new FsmInvoker( FSM_SyncOut_UploadCompleted ) );
			_hashStateToHandler.Add( States.Terminated, new FsmInvoker( FSM_Terminated ) );
		}

		/// <summary>
		/// Executes the job by invoking the FSM action.
		/// </summary>
		protected override void Execute()
		{
			// Check the stopflag
			lock(this)
			{
				if((_bMustStop) && (_state != States.Terminated))	// If we're stopping, accept execution in the Terminated state only.
					return;
			}

do
{
			// Invoke the handler
			try
			{
				Trace.WriteLine( String.Format( "FSM is executing in the {0} state.", _state ) );
				(_hashStateToHandler[ _state ] as FsmInvoker)();
				Trace.WriteLine( String.Format( "FSM has the {0} state after execution.", _state ) );
			}
			catch( Exception ex )
			{
				if( _state == States.Terminated )
				{
					Trace.WriteLine( "SiamPlugin: an exception has occured when trying to execute in the Treminated state. " + ex.Message );
					throw new Exception( "Synchronization has encountered a fatal error.", ex );
				}
				else if( _state == States.Idle )
				{
					Trace.WriteLine( "SiamPlugin: an exception has somehow occured when trying to execute in the Idle state. " + ex.Message );
					throw new Exception( "Synchronization has encountered a fatal error.", ex );
				}
				else
				{
					Trace.WriteLine( String.Format( "SiamPlugin: an exception has occured when executing in the {0} state. {1}", _state, ex.Message ) );
					if( _swErrors != null )
						_swErrors.WriteLine( "{0}\nSynchronization process has terminated unexpectidly.", ex.Message );
					SwitchState( States.Terminated ); // Display the "Sync terminated" window
				}
			}
			} while( (!_bAsynchronous) && (_state != States.Idle) ); // If executing in the sync mode (no re-schedulling), repeat this while we are not done. Note that the callees invoke SwitchState and thus switch to another mode.
		}

		/// <summary>
		/// Name of this asynchronous job.
		/// </summary>
		public override string Name
		{
			get { return Str.Name; }
			set { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Requeues the FSM execution for the next state.
		/// </summary>
		protected void SwitchState( States state )
		{
			_state = state;
			Trace.WriteLine( String.Format( "FSM has switched to the {0} state.", _state ) );

			// Check the stop-flag and apply if needed
			lock(this)
			{
				if(_bMustStop == true)
				{
					_state = States.Terminated;
					Trace.WriteLine( "FSM has noticed the stop-flag and switches to the Terminated state now." );
					_bMustStop = false;
				}
			}

			// Requeue the task. Choose the thread appropriate to the state.
			if( _bAsynchronous ) // If we're doing it sync, we'll be re-invoked without any queueing. Queue only in async case.
			{
			if( (_state == States.Idle) || (_state == States.Terminated) )
				Core.UIManager.QueueUIJob( new MethodInvoker( Execute ) );
			else if( (_state == States.SyncIn_StartDownload) || (_state == States.SyncOut_StartUpload) || (_state == States.SyncOut_UploadCompleted) )
				Core.NetworkAP.QueueJob( this );
			else
				Core.ResourceAP.QueueJob( this );
		}
		}

		#region Variables used by FSM handlers

		/// <summary>
		/// Type of the current sync process.
		/// Valid only if state is not Idle.
		/// </summary>
		protected SyncType _synctype;

		/// <summary>
		/// Feeds being updated, initialized in the SyncFeedList* and used in SyncFeedItems* to ensure that this feed list won't change during the whole sync
		/// </summary>
		protected IResourceList _resFeeds;

		/// <summary>
		/// Error messages collected during the sync.
		/// </summary>
		protected StringWriter _swErrors;

		/// <summary>
		/// List of the feeds that were updated, collected by the OnFeedUpdated event handler.
		/// Deferred sync-in doesn't have to wait for update of feeds that were already updated when it was initiated because deferred sync-in forces a full sync-in on start.
		/// </summary>
		protected IResourceList _resUpdatedFeeds = null;

		/// <summary>
		/// During SyncIn_SyncFeedItems or SyncOut_SyncFeedItems, contains the current feed being processed.
		/// Defines the switch-to-next-state condition.
		/// </summary>
		protected IEnumerator _enumCurrentFeed = null;

		/// <summary>
		/// Involved in the downloading process.
		/// </summary>
		protected Stream _streamData = null;

		/// <summary>
		/// Controls whether the FSM sequence is executed synchronously or asynchronously. In the sync case, the jobs are not queued onto different threads and all the processing occurs in the resource thread.
		/// </summary>
		protected bool _bAsynchronous = false;

		#endregion

		#region FSM Handlers

		private void FSM_Terminated()
		{
			// Stop the FSM
			_state = States.Idle;

			lock(this)
				_bMustStop = false;	// Reset the stop-flag as we're stopping now

			// Prepare the status message
			string sErrorMessage = "";
			bool	bErrors = ( (_swErrors != null) && (_swErrors.ToString().Length != 0) );
			if(bErrors )
				sErrorMessage = String.Format( "There were errors during the feeds {0}{1}-synchronization.\n{2}", (_synctype == SyncType.DeferredSyncIn ? "deferred " : ""), (_synctype == SyncType.SyncOut ? "out" : "in"), _swErrors.ToString() );

			// Perform some deinitialization
			_swErrors = null;
			_xmlSiam = null;
			_streamData = null;

			_statusWriter.ShowStatus( bErrors ? "Siam: synchronization has failed" : "Siam: synchronization has completed");

			// Show the status message (only if we were doing the synchronization asynchronously)
			if( _bAsynchronous )
				MessageBox.Show( sErrorMessage, Str.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Information );
		}

		private void FSM_SyncOut_UploadCompleted()
		{
			// Nothing to do by now
			SwitchState( States.Terminated );
		}

		private void FSM_SyncOut_StartUpload()
		{
			// Submit Xml
			switch( Core.SettingStore.ReadString( Str.Name, Str.Option.Source ) )
			{
			case Str.Option.Source_File:
				_xmlSiam.Save( Core.SettingStore.ReadString( Str.Name, Str.Option.FileName ) ); // Save to a file
				SwitchState( States.SyncOut_UploadCompleted );
				break;
			case Str.Option.Source_Http:
				throw new NotImplementedException();
			case Str.Option.Source_Ftp:
				Debug.Assert( _streamData == null) ;

					// Save the XML sync data to a stream
				MemoryStream	stream = new MemoryStream();
				_xmlSiam.Save( stream );

				// Connect to FTP
				string	sPathOnServer;
				FTPClient ftp = StartFtpConnection( out sPathOnServer );

				// Upload the file
				ftp.Put( stream.ToArray(), sPathOnServer );
				stream.Close();

				// Switch state
				SwitchState( States.SyncOut_UploadCompleted );
				break;
			default:
				throw new Exception( "Synchronization source type unknown. Please update the options." ); // TODO: open the Options dialog pane
			}
		}

		private void FSM_SyncOut_SyncFeedItems()
		{
			Debug.Assert( _enumCurrentFeed != null );

			if( _enumCurrentFeed.MoveNext() == false ) // Move to the current element, if none, break execution
			{
				_enumCurrentFeed = null;
				// Thru. Start uploading the results
				SwitchState( States.SyncOut_StartUpload );
				return;
			}

			// Find xml sync data for the current feed
			XmlElement xmlFeed = _xmlSiam.SelectSingleNode( String.Format( "/feeds//feed[@xmlUrl='{0}']", (_enumCurrentFeed.Current as IResource).GetStringProp( Str.RssFeedUrlProp ) ) ) as XmlElement;

			if( xmlFeed == null ) // Sync data not found, skip and add an error message
				_swErrors.WriteLine( "Synchronization data for feed {0} cannot be found.", (_enumCurrentFeed.Current as IResource).DisplayName );
			else // Sync data present, apply
				SyncFeedItemsOut( _enumCurrentFeed.Current as IResource, xmlFeed );

			// Reschedule the update of next item (don't change the state)
			SwitchState( _state );
		}

		private void FSM_SyncOut_SyncFeedList()
		{
			// Prepare the XML structure
			// TODO: load from the cache
			_xmlSiam = new XmlDocument();
			XmlElement xmlFeeds = _xmlSiam.CreateElement( "feeds" );
			_xmlSiam.AppendChild( xmlFeeds );
			xmlFeeds.SetAttribute( "version", "1.0" );
			xmlFeeds.SetAttribute( "creator", "SiamPlugin for JetBrains Omea" );

			// Sync feeds
			SyncFeedListOut();

			// Schedule synchronization of each feed's item
			_enumCurrentFeed = _resFeeds.GetEnumerator(); // Enumerator that points before the feed to be processed by SyncNextFeedIn
			SwitchState( States.SyncOut_SyncFeedItems );
		}

		private void FSM_SyncIn_StartDeferredSync()
		{
			// Nothing particular to do by now

			SwitchState( States.SyncIn_DeferredSyncFeedItem );
		}

		private void FSM_SyncIn_DeferredSyncFeedItem()
		{
			Debug.Assert( _resFeeds != null );

			// Check all the feeds updated recently
			if( _resUpdatedFeeds != null )
			{
				foreach( IResource resUpdatedFeed in _resUpdatedFeeds )
				{
					// If feed is on the to-be-synced list, process it
					if( _resFeeds.Contains( resUpdatedFeed ) )
					{
						// Find xml sync data for the current feed
						XmlElement xmlFeed = _xmlSiam.SelectSingleNode( String.Format( "/feeds//feed[@xmlUrl='{0}']", (resUpdatedFeed as IResource).GetStringProp( Str.RssFeedUrlProp ) ) ) as XmlElement;

						if( xmlFeed == null ) // Sync data not found, skip and add an error message
							_swErrors.WriteLine( "Synchronization data for feed {0} cannot be found.", (resUpdatedFeed as IResource).DisplayName );
						else // Sync data present, apply
							SyncFeedItemsIn( resUpdatedFeed as IResource, xmlFeed );

						// The feed has been synced, remove it from the to-be-synced list
						_resFeeds = _resFeeds.Minus( resUpdatedFeed.ToResourceList() );
					}
				}

				// Clear the updated feeds list
				_resUpdatedFeeds = null;
			}

			// If we've completed the synclist, stop the sync. Otherwise, don't schedule the next step and stay in the current state, waiting for the new event from the feeding plugin that a feed has been updated
			if( _resFeeds.Count == 0 )
				SwitchState( States.Terminated );

			// TODO: remove debug output
			StringWriter sw = new StringWriter();
			sw.Write( "SiamPlugin: DefRem({0})", _resFeeds.Count );
			foreach( IResource res in _resFeeds )
				sw.Write( " | " + res.DisplayName );
			Trace.WriteLine( sw.ToString() );
		}

		private void FSM_SyncIn_SyncFeedItems()
		{
			Debug.Assert( _enumCurrentFeed != null );

			if( _enumCurrentFeed.MoveNext() == false ) // Move to the current element, if none, break execution
			{
				_enumCurrentFeed = null;
				// Either terminate the sync, if it is immediate-only, or continue doing the deferred sync
				SwitchState( _synctype == SyncType.SyncIn ? States.Terminated : States.SyncIn_StartDeferredSync );
				return;
			}

			// Find xml sync data for the current feed
			XmlElement xmlFeed = _xmlSiam.SelectSingleNode( String.Format( "/feeds//feed[@xmlUrl='{0}']", (_enumCurrentFeed.Current as IResource).GetStringProp( Str.RssFeedUrlProp ) ) ) as XmlElement;

			if( xmlFeed == null ) // Sync data not found, skip and add an error message
				_swErrors.WriteLine( "Synchronization data for feed {0} cannot be found.", (_enumCurrentFeed.Current as IResource).DisplayName );
			else // Sync data present, apply
				SyncFeedItemsIn( _enumCurrentFeed.Current as IResource, xmlFeed );

			// Reschedule the update of next item (don't change the state)
			SwitchState( _state );
		}

		private void FSM_SyncIn_SyncFeedList()
		{
			// Synchronize the list of subscribed feeds
			SyncFeedListIn(); // _resFeeds must be initialized by this method

			// Initiate synchronization of each feed's items
			_enumCurrentFeed = _resFeeds.GetEnumerator(); // Enumerator that points before the feed to be processed by SyncNextFeedIn
			SwitchState( States.SyncIn_SyncFeedItems );
		}

		private void FSM_SycnIn_DownloadCompleted()
		{
			// Parse the sync data into DOM
			_xmlSiam = new XmlDocument();
			_xmlSiam.Load( _streamData );
			_streamData.Close();
			_streamData = null;

			SwitchState( States.SyncIn_SyncFeedList );
		}

		private void FSM_SyncIn_StartDownload()
		{
			// Reset the list of updated feeds because those updated before will be synchronized during the immediate sync-in
			_resUpdatedFeeds = null;

			HttpDownload download;

			// Retrieve XML
			switch( Core.SettingStore.ReadString( Str.Name, Str.Option.Source ) )
			{
			case Str.Option.Source_File:
				// Create a new request for retrieving the data from a file and execute it
				download = new HttpDownload( "file://" + Core.SettingStore.ReadString( Str.Name, Str.Option.FileName ) );
				download.ContentDownloaded += new HttpDownload.ContentDownloadedEventHandler( OnDownloadComplete );
				Core.NetworkAP.QueueJob( download ); // Queue a download job
				break;
			case Str.Option.Source_Http:
				// Create a new request for retrieving this URL and execute it
				download = new HttpDownload( Core.SettingStore.ReadString( Str.Name, Str.Option.Url ) );
				download.ContentDownloaded += new HttpDownload.ContentDownloadedEventHandler( OnDownloadComplete );
				Core.NetworkAP.QueueJob( download ); // Queue a download job
				break;
			case Str.Option.Source_Ftp:
				// Connect to ftp and download the data
				string	sPathOnServer;
				FTPClient ftp = StartFtpConnection( out sPathOnServer );
				Debug.Assert( _streamData == null );
				_streamData = new MemoryStream(ftp.Get( sPathOnServer ), false);	// Receive the remote file as a byte chunk and create a stream out of it to be fed into Xml at the SycnIn_DownloadCompleted state
				SwitchState( States.SycnIn_DownloadCompleted ); // As we have done it synchronously, just go to the processing step
				break;
			default:

				throw new Exception( "Synchronization source type unknown. Please update the options." ); // TODO: open the Options dialog pane
			}
		}

		private void FSM_Idle()
		{
			throw new InvalidOperationException( "Cannot execute in idle state." );
		}

		#endregion

		#region IPlugin Members

		public void Register()
		{
			// Register the Siam Options Pane
			Core.UIManager.RegisterOptionsGroup( "Internet", "The Internet options enable you to control how [product name] works with several types of online content." );
			Core.UIManager.RegisterOptionsPane( "Internet", "Feed Synchronization", new OptionsPaneCreator( PrimaryOptionsPage.CreateInstance ), "Provides for setting up how and when your RSS/ATOM feeds are synchronized with a server." );

			// Register the context menu items
			Core.ActionManager.RegisterContextMenuActionGroup( Str.Name, ListAnchor.Last );

			Core.ActionManager.RegisterContextMenuAction( new SyncAction( this, SyncType.SyncIn, true ), Str.Name, ListAnchor.Last, Str.Menu.SyncIn, Str.RssFeedGroupType, null );
#if(DEBUG)
			Core.ActionManager.RegisterContextMenuAction( new SyncAction( this, SyncType.DeferredSyncIn, true ), Str.Name, ListAnchor.Last, Str.Menu.DeferredSyncIn, Str.RssFeedGroupType, null );
#endif
			Core.ActionManager.RegisterContextMenuAction( new SyncAction( this, SyncType.SyncOut, true ), Str.Name, ListAnchor.Last, Str.Menu.SyncOut, Str.RssFeedGroupType, null );
			Core.ActionManager.RegisterContextMenuAction( new SyncAction( this, SyncType.SyncIn, false ), Str.Name, ListAnchor.Last, Str.Menu.Abort, Str.RssFeedGroupType, null );

			// Register the main menu items
			Core.ActionManager.RegisterMainMenu( Str.Menu.ToolsMenu, ListAnchor.Last );
			Core.ActionManager.RegisterMainMenuActionGroup( Str.Name, Str.Menu.ToolsMenu, ListAnchor.Last );

			// Enable certain actions for all the resource types that may be present at the RSS tabique™
			foreach(string sResourceType in Str.SupportedResourceTypes)
			{
				Core.ActionManager.RegisterMainMenuAction( new SyncAction( this, SyncType.SyncIn, true ), Str.Name, ListAnchor.Last, Str.Menu.SyncIn, sResourceType, null );	// Sync-in
#if(DEBUG)
				Core.ActionManager.RegisterMainMenuAction( new SyncAction( this, SyncType.DeferredSyncIn, true ), Str.Name, ListAnchor.Last, Str.Menu.DeferredSyncIn, sResourceType, null );	// Deferred Sync In
#endif
				Core.ActionManager.RegisterMainMenuAction( new SyncAction( this, SyncType.SyncOut, true ), Str.Name, ListAnchor.Last, Str.Menu.SyncOut, sResourceType, null );	// Sync-out
				Core.ActionManager.RegisterMainMenuAction( new SyncAction( this, SyncType.SyncIn, false ), Str.Name, ListAnchor.Last, Str.Menu.Abort, sResourceType, null );	// Abort sync
			}
		}

		public void Shutdown()
		{
			// Initiate outgoing sync
			if( Core.SettingStore.ReadBool( Str.Name, Str.Option.SyncOnShutdown, Str.Option.SyncOnShutdown_Default ) )
			{
				// TODO: how to make it complete?..
				if( !Running ) // No sync running, do it
					StartSync( SyncType.SyncOut, false );
				else if( _synctype != SyncType.SyncOut ) // If a sync other than sync-out is running, abort it and do sync-out
				{
					AbortSync();
					StartSync( SyncType.SyncOut, false );
				}
			}
		}

		public void Startup()
		{
			// Register the FSM handlers
			SetupFSM();

			// Get a status-bar writer
			_statusWriter = Core.UIManager.GetStatusWriter( this, StatusPane.Network);

			// Get access to the RSS plugin to subscribe for its event
			RSSPlugin.RSSPlugin pluginRss = Core.PluginLoader.GetPluginService( typeof( RSSPlugin.RSSPlugin ) ) as RSSPlugin.RSSPlugin;
			pluginRss.FeedUpdated += new ResourceEventHandler( OnFeedUpdated );

			// Schedule the first synchronization — deferred sync-in
			if( Core.SettingStore.ReadBool( Str.Name, Str.Option.SyncOnStartup, Str.Option.SyncOnStartup_Default ) )
				StartSync( SyncType.DeferredSyncIn, true );

			// Start listening for the shutdown event
			Core.StateChanged += new EventHandler(OnCoreStateChanged);
			// TODO:
		}

		#endregion

		public enum SyncType
		{
			SyncIn,
			SyncOut,
			DeferredSyncIn
		} ;

		/// <summary>
		/// Initiates the in or out synchronization in either immediate of dereffed mode.
		/// The sync cannot be started when another sync is already in progress.
		/// Must be called in the UI thread.
		/// </summary>
		/// <param name="synctype">Defines the sync mode.</param>
		/// <param name="bAsync">If <c>True</c>, the execution goes asynchronously (the task is divided into cmall jobs, each one executed in the appropriate thread). If <c>False</c>, the task is executed immediately, as a single job, on the resource thread.</param>
		public void StartSync( SyncType synctype, bool bAsync )
		{
			// Check if sync is disabled
			if( Core.SettingStore.ReadString( Str.Name, Str.Option.Source, Str.Option.Source_None ) == Str.Option.Source_None )
				return; // No sync should be done

			// Running already?
			if( _state != States.Idle )
				throw new InvalidOperationException( "Cannot start a sync when another sync is running." );
			/*
				MessageBox.Show( "Cannot start feed synchronization. Another synchronization is already in progress.", "SiamPlugin — Omea", MessageBoxButtons.OK,  MessageBoxIcon.Error );
				return;*/

			_statusWriter.ShowStatus( "Siam: started synchronizing" );

			try
			{
				// Setup the startup parameters and start the appropriate synchronization
				_synctype = synctype;
				_bAsynchronous = bAsync; // SwitchState looks at this
				_swErrors = new StringWriter();

				// Start the FSM (specify the state and queue the job at the thread appropriate, if in async mode)
				SwitchState( _synctype == SyncType.SyncOut ? States.SyncOut_SyncFeedList : States.SyncIn_StartDownload );

				// If we're doing it synchronously, SyncState won't invoke the job for us. Do it explicitly.
				if( !bAsync )
					Core.ResourceAP.RunJob( this ); // By this call, we get into Execute, and, as not async, stay there until completed.
			}
			catch( Exception ex )
			{
				// Reset
				_state = States.Idle;
				_xmlSiam = null;

				throw new Exception( "The Siam synchronization cannot be initiated.", ex );
			}
		}

		/// <summary>
		/// If a sync is currently running, aborts that sync.
		/// </summary>
		public void AbortSync()
		{
			// Not running?
			if( _state == States.Idle )
				throw new InvalidOperationException( "Cannot abort a sync when no sync is running." );

			// Schedule the termniation
			_swErrors.WriteLine( "Aborted by user." );

			// Raise the stopflag
			lock(this)
				_bMustStop = true;

			// Special case: in this state we're waiting for an external event which may never occur; if we abort the wait and the event occurs later, that is no problem. So, break it!
			if(_state == States.SyncIn_DeferredSyncFeedItem)
				SwitchState( States.Terminated );
		}

		/// <summary>
		/// Checks whether synchronization is going now or not.
		/// </summary>
		public bool Running
		{
			get { return (_state != States.Idle); }
		}

		/// <summary>
		/// Type of the synchronization currently running.
		/// </summary>
		public SyncType RunningSyncType
		{
			get
			{
				if( Running )
					return _synctype;
				else
					throw new InvalidOperationException( "Sync Type is undefined when sync is not running." );
			}

		}

		/// <summary>
		/// An Http download has completed.
		/// Go to SycnIn_DownloadCompleted state to process the data in the resource thread.
		/// </summary>
		protected void OnDownloadComplete( Stream streamData, HttpDownload session )
		{
			// Switch to the "Download complete" state in the network thread
			_streamData = streamData; // Save the stream
			SwitchState( States.SycnIn_DownloadCompleted );
		}

		/// <summary>
		/// Synchronizes the list of subscribed feeds in from the XML sync file.
		/// Creates missing feeds by subscribing to the address provided, removes feeds that are not mentioned in the sync file by unsubscribing from the feed (after prompting user).
		/// </summary>
		protected void SyncFeedListIn()
		{
			_statusWriter.ShowStatus( "Siam: synchronizing the feed list in" );
			_resFeeds = null; // List of the feeds that are mentioned in the synchronization data and should be processed
			IResourceList resAvailFeeds = GetFeedList();	// List of the not-yet-processed feeds

			// Lookup all the feed elements currently in XML and check for the corresponding feeds
			foreach( XmlElement xmlFeed in _xmlSiam.SelectNodes( "/feeds//feed" ) )
			{
				if( xmlFeed.GetAttribute( "xmlUrl" ) == "" ) // TODO: move to validation step?
				{
					// TODO: Warning: feed MUST have xmlUrl attribute set
					xmlFeed.ParentNode.RemoveChild( xmlFeed );
				}

				IResource resThisFeed = Core.ResourceStore.FindUniqueResource( Str.RssFeedType, Str.RssFeedUrlProp, xmlFeed.GetAttribute( "xmlUrl" ) );
				if( resThisFeed == null ) // No such feed, it is to be added
				{
					// TODO: add this feed by subscribing to it
					continue;
				}

				// Update feed parameters
				xmlFeed.SetAttribute( "htmlUrl", resThisFeed.GetStringProp( Str.RssFeedHtmlProp ) );
				xmlFeed.SetAttribute( "title", resThisFeed.GetStringProp( Str.RssFeedTitleProp ) );

				resAvailFeeds.Minus( resThisFeed.ToResourceList() ); // Mark as already exported	(remove from the unprocesseds list)
				_resFeeds = resThisFeed.ToResourceList().Union( _resFeeds, true ); // Collect for individual sync
			}

			// Check feeds that are not listed in the XML structure
			foreach( IResource resFeed in resAvailFeeds )
			{
				// TODO: delete resFeed by unsubscribing from it
			}
		}

		/// <summary>
		/// Synchronizes the list of unread items for the specified feed, from the Xml Sync file into Omea.
		/// </summary>
		/// <param name="resFeed">Feed's Omea resource.</param>
		/// <param name="xmlFeed">Feed's Siam Xml element.</param>
		/// <remarks>Feeds are first to be synchronized using the <see cref="SyncFeedListIn"/></remarks>
		protected void SyncFeedItemsIn( IResource resFeed, XmlElement xmlFeed )
		{
			_statusWriter.ShowStatus( "Siam: synchronizing in " + resFeed.DisplayName );
			Trace.WriteLine( String.Format( "SiamPlugin: SyncIn for feed \"{0}\".", resFeed.DisplayName ) );
			Hashtable hashUnreads = new Hashtable(); // Store IDs of the items which are to be marked as unread

			DateTime dtNewest = DateTime.MinValue; // The most recent item contained in the synchronization data
			DateTime dtOldest = DateTime.MaxValue; // The oldest item contained in the synchronization data

			XmlNodeList xmlFeedItems = xmlFeed.SelectNodes( "items/item" ); // List of the items that have sync data attached
			if( xmlFeedItems.Count == 0 ) // No sync data, skip the feed
				return;

			// Extract info
			bool bUnread;
			foreach( XmlElement xmlItem in xmlFeedItems )
			{
				// Check if the item is unread. Other items should be processed too because we need to determine the time span covered by the sync data
				bUnread = (xmlItem.GetAttribute( "status" ) == "unread");

				// Find the resources for this item
				IResourceList resSuitableItems = null;

				if( xmlItem.HasAttribute( "guid" ) )
					resSuitableItems = Core.ResourceStore.FindResources( Str.RssItemType, Str.RssItemGuidProp, xmlItem.GetAttribute( "guid" ) ).Intersect( resSuitableItems, true );
				if( xmlItem.HasAttribute( "link" ) )
					resSuitableItems = Core.ResourceStore.FindResources( Str.RssItemType, Str.RssItemLinkProp, xmlItem.GetAttribute( "link" ) ).Intersect( resSuitableItems, true );

				// Extract the items found
				foreach( IResource resSuitableItem in resSuitableItems )
				{
					// Collect the unread items
					if( bUnread )
						hashUnreads.Add( resSuitableItem.Id, true );

					// Expand the sync time span to include this item as well
					dtNewest = resSuitableItem.GetDateProp( Str.DateProp ) > dtNewest ? resSuitableItem.GetDateProp( Str.DateProp ) : dtNewest;
					dtOldest = resSuitableItem.GetDateProp( Str.DateProp ) < dtOldest ? resSuitableItem.GetDateProp( Str.DateProp ) : dtOldest;
				}

				// If zero items were found, it's not a problem, and we just do nothing …
			}

			// Apply the extracted info
			IResourceList resItems = resFeed.GetLinksOfType( Str.RssItemType, Str.RssFeedItemLink );
			resItems.Sort( Str.DateProp, false ); // Sort by date so that we could select only limited number of most recent items

			// Filter out only the items that are involved in synchronization (tat's needed because we will mark all the items that fall in the sync range but are unmentioned as read)
			resItems = resItems.Intersect( Core.ResourceStore.FindResourcesInRange( Str.RssItemType, Str.DateProp, dtOldest, dtNewest ), true );

			// Apply sync data to the items that fall in the sync range
			foreach( IResource resItem in resItems )
			{
				ResourceProxy proxy = new ResourceProxy( resItem );
				if( hashUnreads.ContainsKey( resItem.Id ) ) // The item should be marked as unread
					proxy.SetPropAsync( Str.RssItemUnreadProp, true );
				else // The item should be marked as read
					proxy.SetPropAsync( Str.RssItemUnreadProp, false );
			}
		}

		/// <summary>
		/// Synchronizes the list of subscribed feeds out into the XML sync file.
		/// </summary>
		protected void SyncFeedListOut()
		{
			_statusWriter.ShowStatus( "Siam: synchronizing the feed list out" );
			_resFeeds = GetFeedList(); // List of all the feeds, their items will be processed later
			object oDummy = _resFeeds.ResourceIds; // Force take snapshot
			IResourceList resFeeds = _resFeeds; // List of the not-yet-processed feeds

			// Lookup all the feed elements currently in XML and check for the corresponding feeds
			foreach( XmlElement xmlFeed in _xmlSiam.SelectNodes( "/feeds//feed" ) )
			{
				if( xmlFeed.GetAttribute( "xmlUrl" ) == "" ) // TODO: move to validation step?
				{
					// TODO: Warning: feed MUST have xmlUrl attribute set
					xmlFeed.ParentNode.RemoveChild( xmlFeed );
				}

				IResource resThisFeed = Core.ResourceStore.FindUniqueResource( Str.RssFeedType, Str.RssFeedUrlProp, xmlFeed.GetAttribute( "xmlUrl" ) );
				if( resThisFeed == null ) // No such feed, it has been deleted during the session
				{
					xmlFeed.ParentNode.RemoveChild( xmlFeed ); // Remove the feed's Xml Element
					continue;
				}

				// Update feed parameters
				xmlFeed.SetAttribute( "htmlUrl", resThisFeed.GetStringProp( Str.RssFeedHtmlProp ) );
				xmlFeed.SetAttribute( "title", resThisFeed.GetStringProp( Str.RssFeedTitleProp ) );
				if( xmlFeed.SelectSingleNode( "items" ) == null ) // Ensure there's Items element in the feed
					xmlFeed.AppendChild( xmlFeed.OwnerDocument.CreateElement( "items" ) );

				// Mark as already exported	(remove from the unprocesseds list)
				resFeeds = resFeeds.Minus( resThisFeed.ToResourceList() );
			}

			// Add feeds that are not listed in the XML structure yet
			XmlElement xmlGlobalFeeds = (XmlElement) _xmlSiam.SelectSingleNode( "/feeds" ); // A parent for non-categorized feeds
			foreach( IResource resFeed in resFeeds )
			{
				// Create
				XmlElement xmlFeed = _xmlSiam.CreateElement( "feed" );
				xmlGlobalFeeds.AppendChild( xmlFeed );

				// Setup
				xmlFeed.SetAttribute( "xmlUrl", resFeed.GetStringProp( Str.RssFeedUrlProp ) );
				xmlFeed.SetAttribute( "htmlUrl", resFeed.GetStringProp( Str.RssFeedHtmlProp ) );
				xmlFeed.SetAttribute( "title", resFeed.GetStringProp( Str.RssFeedTitleProp ) );
				xmlFeed.AppendChild( xmlFeed.OwnerDocument.CreateElement( "items" ) );
			}
		}

		/// <summary>
		/// Synchronizes the list of unread items for the specified feed, from Omea to the Xml Sync file.
		/// </summary>
		/// <param name="resFeed">Feed's Omea resource.</param>
		/// <param name="xmlFeed">Feed's Siam Xml element.</param>
		/// <remarks>Feeds are first to be synchronized using the <see cref="SyncFeedListOut"/></remarks>
		protected void SyncFeedItemsOut( IResource resFeed, XmlElement xmlFeed )
		{
			_statusWriter.ShowStatus( "Siam: synchronizing out " + resFeed.DisplayName );
			Trace.WriteLine( String.Format( "SiamPlugin: SyncOut for feed \"{0}\".", resFeed.DisplayName ) );
			// Implementation #1: just remove all the items mentioned and replace them with the new set. To better comply with SIAM spec, we should try to keep as many existing attributes that we cannot understand as possible

			// Remove all the previously-listed items from the XML representation
			foreach( XmlElement node in xmlFeed.SelectNodes( "items/item" ) )
				node.ParentNode.RemoveChild( node );

			// Pick the feed items
			IResourceList resItems = resFeed.GetLinksOfType( Str.RssItemType, Str.RssFeedItemLink );
			resItems.Sort( Str.DateProp, false ); // Sort by date so that we could select only limited number of most recent items

			// If there are no items in the feed, just return (leave it empty)
			if( resItems.Count == 0 )
				return;
			XmlElement xmlItems = xmlFeed.SelectSingleNode( Str.Siam.FeedItemsElem ) as XmlElement; // Root for the new item elements
			if( xmlItems == null ) // No items element on this feed
				xmlFeed.AppendChild( xmlItems = xmlFeed.OwnerDocument.CreateElement( Str.Siam.FeedItemsElem ) ); // Create a new one though

			int nMaxItemsToSync = Core.SettingStore.ReadInt( Str.Name, Str.Option.MonitoredItems, Str.Option.MonitoredItems_Default ); // Maximum count of exported items
			int nItemsToSync = nMaxItemsToSync < resItems.Count ? nMaxItemsToSync : resItems.Count; // Limit to the number of items present in the feed, at most

			// Add the feed items that fall into the specified range (N most recent items) to its XML representation
			// Take unread items only because the item's default state is 'read'
			for( int a = 0; a < nItemsToSync; a++ )
			{
				// Add all the items that are unread (absent items are assumed to be read), and also the most-recent item, regardless of its possible read status, to define the synchronization region
				if( (resItems[ a ].HasProp( Str.RssItemUnreadProp )) || (a == 0) )
					ExportFeedItem( resItems[ a ], xmlItems );
			}

			// Define the most-ancient boundary of the synchronization span. At a glance, this should be the nMaxItems-th item as it's the oldest item taken into account. However, we can extend this area to the past to include all the adjacent read items: if we store the oldest read item of the contiguos read space, it costs us only that one item, but marks the whole span as read.
			IResource resOldestSyncItem = null;

			if( resItems.Count <= nMaxItemsToSync ) // All the available items fall into the sync range
				resOldestSyncItem = resItems[ nItemsToSync - 1 ].HasProp( Str.RssItemUnreadProp ) ? null : resItems[ nItemsToSync - 1 ]; // If the oldest item is unread, it has already been added, put null and forget it. Otherwise, choose for adding
			else // There are items outside the sync range — older ones
			{
				// Build a list of unread feed items not falling into the sync span
				IResourceList resUnreadOutOfSync = resItems.Intersect( Core.ResourceStore.FindResources( Str.RssItemType, Str.RssItemUnreadProp, true ) ).Intersect( Core.ResourceStore.FindResourcesInRange( Str.RssItemType, Str.DateProp, DateTime.MinValue, resItems[ nMaxItemsToSync ].GetDateProp( Str.DateProp ) ) );

				// The most recent unread item not in sync range, or minvalue, if none
				DateTime nMinDate = resUnreadOutOfSync.Count != 0 ? resUnreadOutOfSync[ resUnreadOutOfSync.Count - 1 ].GetDateProp( Str.DateProp ) : DateTime.MinValue;

				// Build a list of read feed items that are most recent than the items in the above list and do not fall into the sync span. This gives us exactly the contiguous list of read items immediately preceeding the sync span
				IResourceList resContRead = Core.ResourceStore.FindResourcesInRange( Str.RssItemType, Str.DateProp, nMinDate, resItems[ nMaxItemsToSync ].GetDateProp( Str.DateProp ) );
				resContRead = resContRead.Intersect( resItems );
				Trace.WriteLine( Core.ResourceStore.GetAllResources( Str.RssItemType ).Minus( Core.ResourceStore.FindResources( Str.RssItemType, Str.RssItemUnreadProp, true ) ).Count );
				resContRead = resContRead.Intersect( Core.ResourceStore.GetAllResources( Str.RssItemType ).Minus( Core.ResourceStore.FindResources( Str.RssItemType, Str.RssItemUnreadProp, true ) ) );
				resContRead.Sort( Str.DateProp, false );

				// Now take the oldest item as the boundary of the contiguous read items span
				resOldestSyncItem = resContRead.Count > 0 ? resContRead[ resContRead.Count - 1 ] : null;
			}

			// Now submit the "oldest" boundary of the sync span, if found
			if( resOldestSyncItem != null ) // The only thing to check is that the oldest read item is non-null (will be null eg if there are no read items). If it is defined then it's absent from the sync data as we've been adding unread items only
				ExportFeedItem( resOldestSyncItem, xmlItems );

			/*
			IResource resOldestReadItem = null; // TODO: will be slow on large feeds, replace with selection/intersection
			foreach( IResource resItem in resItems )
			{
				nItemsTaken++;
				if( nItemsTaken - 1 < nMaxItems ) // Should store individual unread items
				{
					// Skip items that are not unread
					if( !resItems[a].HasProp( Str.RssItemUnreadProp ) )
					{
						resOldestReadItem = resItem; // By this moment it happens to be the oldest unread item encountered
						continue;
					}

				}
				else // Limit exceeded; individual items should not be stored, but the oldest read item should be determined
				{
					if( resItem.HasProp( Str.RssItemUnreadProp ) ) // The contiguous read items block is thru, exit
						break;
					resOldestReadItem = resItem;
				}
			}

			// The most recent processed item, if not added yet (it would be added automatically if it is unread; otherwise, we should add it to define the range of synchronized items — if we don't, the items newer than the newest unread would be considered to be not included in this sync even if they we processed)
			if( xmlItems.SelectSingleNode( String.Format( "item[@guid='{0}']", resItems[ 0 ].GetPropText( Str.RssItemGuidProp ) ) ) == null )
			{ // Create a new item representing the newest sync item
				xmlItems.AppendChild( xmlNewItem = _xmlSiam.CreateElement( "item" ) );
				// Setup the item's Xml Element
				xmlNewItem.SetAttribute( "guid", resItems[ 0 ].GetPropText( Str.RssItemGuidProp ) );
				xmlNewItem.SetAttribute( "link", resItems[ 0 ].GetPropText( Str.RssItemLinkProp ) );
				xmlNewItem.SetAttribute( "status", resItems[ 0 ].HasProp( Str.RssItemUnreadProp ) ? "unread" : "read" ); // Can be read or unread as well
			}

			// The oldest processed item is not older than resOldestReadItem. If we extend the sync area to include the contiguous set of read items preceeding it (in time), we would not have to add any additional sync items because all the unmentioned items in the sync area are assumed to be read by default. So we move resOldestReadItem back in time to extend the sync span, but with read items only.
			// Also this item may happen to fall within the sync span if the sync items limit has not been exceeded or all the older items are unread. That's not a problem, however.
			if( resOldestReadItem != null ) // The only thing to check is that the oldest read item is non-null (will be null eg if there are no read items). If it is defined then it's absent from the sync data as we've been adding unread items only
			{ // Create a new item representing the newest sync item
				xmlItems.AppendChild( xmlNewItem = _xmlSiam.CreateElement( "item" ) );
				// Setup the item's Xml Element
				xmlNewItem.SetAttribute( "guid", resOldestReadItem.GetPropText( Str.RssItemGuidProp ) );
				xmlNewItem.SetAttribute( "link", resOldestReadItem.GetPropText( Str.RssItemLinkProp ) );
				xmlNewItem.SetAttribute( "status", "read" ); // Definitely read
			}
			*/
		}

		/// <summary>
		/// Exports a feed item (feed item resource) as an XML compatible with the Siam spec.
		/// </summary>
		/// <param name="resItem">Feed item resource.</param>
		/// <param name="xmlItems">Parent XML node to which the newly-created item should be added.</param>
		protected void ExportFeedItem( IResource resItem, XmlElement xmlItems )
		{
			XmlElement xmlNewItem;

			// Create an Xml Element for this item
			xmlItems.AppendChild( xmlNewItem = _xmlSiam.CreateElement( Str.Siam.FeedItemElem ) );

			// Setup the item's Xml Element
			if( resItem.HasProp( Str.RssItemGuidProp ) )
				xmlNewItem.SetAttribute( Str.Siam.GuidAttr, resItem.GetPropText( Str.RssItemGuidProp ) );
			if( resItem.HasProp( Str.RssItemLinkProp ) )
				xmlNewItem.SetAttribute( Str.Siam.LinkAttr, resItem.GetPropText( Str.RssItemLinkProp ) );
			xmlNewItem.SetAttribute( Str.Siam.StatusAttr, (resItem.HasProp( Str.RssItemUnreadProp ) ? Str.Siam.UnreadStatus : Str.Siam.ReadStatus) );
		}

		/// <summary>
		/// Returns a list of all the RSS feeds that are real user feeds (not the service/comment feeds that should be invisible to user).
		/// </summary>
		/// <returns>Resource list of real feeds.</returns>
		protected IResourceList GetFeedList()
		{
			// Subtract those feeds that are the comment feeds actually (they have a specific link pointing to the "parent" rss item)
			return Core.ResourceStore.GetAllResources( Str.RssFeedType ).Minus( Core.ResourceStore.FindResourcesWithProp( Str.RssFeedType, Core.ResourceStore.GetPropId( Str.RssItemCommentsFeedLinkType ) ));
		}

		/// <summary>
		/// An event fired by the RSS plugin when a feed gets updated.
		/// Called on the resource thread.
		/// </summary>
		private void OnFeedUpdated( object sender, ResourceEventArgs e )
		{
			// Collect the updated feeds into the list
			_resUpdatedFeeds = e.Resource.ToResourceList().Union( _resUpdatedFeeds, true );

			// Invoke sync for these threads if we're in the deferred-sync-in mode
			if( _state == States.SyncIn_DeferredSyncFeedItem )
			{
				SwitchState( _state ); // Schedule execution of one more step in this state to process the sync of this new item
				Trace.WriteLine( String.Format( "SiamPlugin: The feed {0} has fininshed updating and was schedulled for individual sync.", e.Resource.DisplayName ) );
			}
		}

		/// <summary>
		/// Reads the FTP-related settings, applies them and starts the FTP connection.
		/// </summary>
		/// <param name="sFilePath">Absolute path below the hostname.</param>
		/// <returns>The FTP client with a connection open.</returns>
		protected FTPClient StartFtpConnection(out string sFilePath)
		{
			// Create
			FTPClient	ftp = new FTPClient();

			// Connection Mode
			ftp.ConnectMode = Core.SettingStore.ReadString( Str.Name,  Str.Option.FtpConnectionMode,  Str.Option.FtpConnectionMode_Default ) == Str.Option.FtpConnectionMode_Passive ? FTPConnectMode.PASV : FTPConnectMode.ACTIVE;

			// Parse the URI
			Uri uriFtp = new Uri(Core.SettingStore.ReadString( Str.Name,  Str.Option.FtpUri, Str.Option.FtpUri_Default));
			sFilePath = uriFtp.PathAndQuery;

			// Host
			ftp.RemoteHost = uriFtp.Host;

			// Connect!
			ftp.Connect();
			ftp.TransferType = FTPTransferType.BINARY;

			// Username & Password
			string sUsername = Core.SettingStore.ReadString( Str.Name, Str.Option.Username, Str.Option.Username_Default );
			if( sUsername.Length != 0 )
			{
				// Username
				ftp.User( sUsername );

				// Password
				string sPassword = Core.SettingStore.ReadString( Str.Name, Str.Option.Password, Str.Option.Password_Default );
				if( sPassword.Length != 0 )
					ftp.Password( sPassword );
			}

			return ftp;
		}

		/// <summary>
		/// Fires when Omea application state changes.
		/// When shutdown is initiated, invokes upload of the sync data.
		/// </summary>
		protected void OnCoreStateChanged(object sender, EventArgs e)
		{
			if(Core.State == CoreState.ShuttingDown)
			{
				// Initiate outgoing sync
				if( Core.SettingStore.ReadBool( Str.Name, Str.Option.SyncOnShutdown, Str.Option.SyncOnShutdown_Default ) )
				{
					// TODO: how to make it complete?..
					if( !Running ) // No sync running, do it
						StartSync( SyncType.SyncOut, false );
					else if( _synctype != SyncType.SyncOut ) // If a sync other than sync-out is running, abort it and do sync-out
					{
						AbortSync();	// TODO: check how the async abort and sync syncout coincide
						StartSync( SyncType.SyncOut, false );
					}
				}
			}
		}
	}

	#region String Constants

	public class Str
	{
		public const string Name = "SiamPlugin";
		public const string Title = "Siam Plugin";
		public const string MessageBoxTitle = "Siam Plugin — Omea";

		public const string DateProp = "Date";

		public const string RssFeedType = "RSSFeed";
		public const string RssItemType = "RSSItem";
		public const string RssFeedGroupType = "RSSFeedGroup";
		public const string RssFeedItemLink = "RSSItem";
		public const string RssFeedUrlProp = "URL";
		public const string RssFeedHtmlProp = "HomePage";
		public const string RssFeedTitleProp = "Name";
		public const string RssItemGuidProp = "GUID";
		public const string RssItemLinkProp = "Link";
		public const string RssItemUnreadProp = "IsUnread";
		public const string RssItemCommentsFeedLinkType = "ItemCommentFeed";

		public static readonly string[] SupportedResourceTypes = new string[]{ RssItemType, RssFeedType, RssFeedGroupType };

		public class Option
		{
			public const string MonitoredItems = "MonitorItems"; // Number of items monitored per thread
			public const int MonitoredItems_Default = 100; // Default value for the number of monitored items

			public const string FileName = "FileName"; // Name of the file we're syncing with

			public const string Url = "URL"; // HTTP URL we're syncing with

			public const string Source = "Source"; // Sync source
			public const string Source_Default = "None";
			public const string Source_None = "None"; // No sync source. Do not synchronize
			public const string Source_File = "File"; // Sync with a file (FileName is valid)
			public const string Source_Http = "HTTP"; // Sync via HTTP (URL is valid)
			public const string Source_Ftp = "FTP"; // Sync via HTTP (URL is valid)

			public const string SyncOnStartup = "SyncOnStartup"; // Synchronize in deferred when Omea starts
			public const bool SyncOnStartup_Default = true;

			public const string SyncOnShutdown = "SyncOnShutdown"; // Synchronize out when Omea shutdowns
			public const bool SyncOnShutdown_Default = true;

			public const string FtpUri = "FtpUri";	// FTP URI
			public const string FtpUri_Default = "";

			public const string Username = "Username";	// User name (if authentication is required)
			public const string Username_Default = "";

			public const string Password = "Password";	// Password (if authentication is required)
			public const string Password_Default = "";

			public const string FtpConnectionMode = "FtpHost";	// FTP connection mode, either active or passive
			public const string FtpConnectionMode_Default = "Passive";
			public const string FtpConnectionMode_Active = "Active";
			public const string FtpConnectionMode_Passive = "Passive";
		}

		public class Menu
		{
			public const string SyncOut = "Export Feeds State";
			public const string SyncIn = "Import Feeds State";
			public const string DeferredSyncIn = "Import Feeds State (when download completes)";
			public const string Abort = "Abort Import/Export";
			public const string ToolsMenu = "Tools";
		}

		public class Siam
		{
			public const string UnreadStatus = "unread";
			public const string ReadStatus = "read";
			public const string GuidAttr = "guid";
			public const string LinkAttr = "link";
			public const string StatusAttr = "status";
			public const string FeedItemElem = "item";
			public const string FeedItemsElem = "items";
		}
	}

	#endregion
}
