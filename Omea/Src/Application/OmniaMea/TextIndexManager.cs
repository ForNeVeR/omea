// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls.CustomViews;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.TextIndex;

namespace JetBrains.Omea
{
    #region Generic Text Providers: Annotation and Resource Header

    /// <summary>
    /// Class which provides annotation text for indexing.
    /// </summary>
    internal class AnnotationTextIndexProvider : IResourceTextProvider
    {
        public bool ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            if( res.HasProp( "Annotation" ))
            {
                string anno = res.GetStringProp( "Annotation" );
                consumer.RestartOffsetCounting();
                consumer.AddDocumentFragment( res.Id, anno, DocumentSection.AnnotationSection );
            }
            return true;
        }
        public void  RejectResult() {}
    }

    /// <summary>
    /// Class which provides annotation text for indexing.
    /// </summary>
    internal class TitleTextIndexProvider : IResourceTextProvider
    {
        public bool ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            string title = res.GetPropText( Core.Props.Name );
            consumer.AddDocumentHeading( res.Id, title );

            IResource fromPerson = res.GetLinkProp( Core.ContactManager.Props.LinkFrom );
            if( fromPerson != null )
            {
                consumer.AddDocumentFragment( res.Id, fromPerson.DisplayName, DocumentSection.SourceSection );
            }
            return true;
        }
        public void RejectResult() { }
    }
    #endregion Annotation Provider

    #region MockEnvironment
    /**
     * A TextIndexManager stub which is used when OmniaMea is running with no text index.
     */
    internal class MockTextIndexManager: ITextIndexManager
    {
        public event EventHandler IndexLoaded;

        public void QueueImmediateIndexing( int docID, string from, string subject, string body ) {}
        public void QueryIndexing(int resID)            {}
        public void QueueContextExtraction(IPropertyProvider prov, object e ) {}
        public void RebuildIndex()                      {}
        public void DeleteDocumentQueued(int resID)     {}
        public void DeleteDocumentImmediate(int resID)  {}
        public bool IsIndexPresent()                    {    return false;     }
        public bool IsDocumentInIndex(int docID)        {    return false;     }

        public void SetUpdateResultHandler( UpdateFinishedEventHandler h ) {}
        public void SetTextIndexLoadedHandler( EventHandler h )             {}
        public void ClearTextIndexLoadedHandler( EventHandler h )           {}
        public bool IdleIndexingMode { get{ return false; } set { } }

        public void RegisterSearchProvider( ISearchProvider host, string title ) {}
        public void RegisterSearchProvider( ISearchProvider host, string title, string group ) {}
        public void UnregisterSearchProvider( ISearchProvider host ) {}
        public string GetSearchProviderTitle( ISearchProvider host ) { return null; }
        public ISearchProvider    CurrentSearchProvider { get{ return null;} set{} }
        public ISearchProvider[]  GetSearchProviders() { return null; }
        public string[] GetSearchProviderGroups() { return null; }
        public ISearchProvider[] GetSearchProvidersInGroup( string group ) { return null; }

        #region ProcessQuery
        public IResourceList   ProcessQuery( string SearchQuery )  { return null; }
        public IResourceList   ProcessQuery( string SearchQuery, int[] RestrictByIDs,
                                             out IHighlightDataProvider hldp,
                                             out string[] lastStopList, out string errorMsg )
        { hldp = null; lastStopList = null; errorMsg = null; return null; }
        public IResourceList   ProcessQuery( string SearchQuery, int[] RestrictByIDs,
                                             bool CalcContexts, out IHighlightDataProvider hldp )
        { hldp = null; return null; }

        public bool MatchQuery( string query, IResource res ) {  return false;  }
        #endregion ProcessQuery
    }
    #endregion MockEnvironment

    /**
     * Class which manages asynchronous building of the full-text index.
     */

    public class TextIndexManager: AsyncProcessor, ITextIndexManager
    {
        private const   int         _cDefaultDaysBetweenDefrags = 3;
        private const ulong         _cFreeSpaceMargin = 50 * 1024 * 1024;
        private const string        _cStandardProvidersGroupName = "Standard Search Providers";
        private const string        _cOthersProvidersGroupName = "";

        private readonly IStatusWriter   _statusWriter;
        private readonly FullTextIndexer _textIndexer;
        private TextQueriesOptimizationManager _queryManager;
        private DateTime            _lastStatusUpdate = DateTime.MinValue;
        private readonly HashMap    _SearchProviders = new HashMap();
        private readonly HashMap    _ProvidersGroups = new HashMap();
        private ISearchProvider     _currentSP = null;

        private static AbstractJob  _switchToIdleJob;
        private bool                _idleWaitingStarted = false;
        public  bool                _isJobTraceSuppressed = false;
        private bool                _isCriticalIOCaseFlag = false,
                                    _isManuallySuspended = false;
        private object              _isIdleIndexing;
        private SpinWaitLock        _pendingDocsLock = new SpinWaitLock();
        private readonly IntHashSet _pendingDocs = new IntHashSet( 1000 );
        private readonly DelegateJob    _processPendingDocsDelegate;
        private int                 _documentsIndexed;

        public event EventHandler   IndexLoaded;

        #region Ctor and Initialization
        internal TextIndexManager() : base( false )
        {
            _processPendingDocsDelegate = new DelegateJob( "Indexing documents", new MethodInvoker( ProcessPendingDocs ), new object[] {});

            if( Core.ResourceStore.PropTypes.Exist( "QueuedForIndexing" ) )
            {
                Core.ResourceStore.PropTypes.Delete( Core.ResourceStore.PropTypes[ "QueuedForIndexing" ].Id );
            }
            _statusWriter = Core.UIManager.GetStatusWriter( typeof(FullTextIndexer), StatusPane.UI );
            _isJobTraceSuppressed = Core.SettingStore.ReadBool( "TextIndexing", "SuppressJobTraces", false );

            _textIndexer = new FullTextIndexer();
            _textIndexer.IndexLoaded += IndexLoadedNotification;

            Reenterable = false;
            ThreadName = "TextIndex AsyncProcessor";
            ThreadPriority = System.Threading.ThreadPriority.BelowNormal;
            ThreadStarted += TextIndexProcessor_ThreadStarted;

            Core.PluginLoader.RegisterResourceTextProvider( null, new AnnotationTextIndexProvider() );
            Core.PluginLoader.RegisterResourceTextProvider( null, new TitleTextIndexProvider() );

            //  Register predefined search providers
            CurrentSearchProvider = new OmeaGlobalSearchProvider();
            RegisterSearchProvider( CurrentSearchProvider, "Omea Search", _cStandardProvidersGroupName );
            RegisterSearchProvider( new OmeaQuickSearchProvider(), "Local Search", _cStandardProvidersGroupName );

            DefragmentIndexJob._textIndexManager = IndexingJob._textIndexManager = this;
            SetupDefragmentationQueue();

            _switchToIdleJob = new SwitchToIdleModeJob( this );
            QueueSwitchToIdleModeJob();

            Core.UIManager.RegisterIndicatorLight( "Text Index Manager", this, 30,
                                                   MainFrame.LoadIconFromAssembly( "textindex_idle.ico" ),
                                                   MainFrame.LoadIconFromAssembly( "textindex_busy.ico" ),
                                                   MainFrame.LoadIconFromAssembly( "textindex_stuck.ico" ) );
        }

        /// <summary>
        /// Typical proxy handler, which simply propagates event from internal
        /// subsystem to external consumers.
        /// </summary>
        private void IndexLoadedNotification( object sender, EventArgs e )
        {
            if( IndexLoaded != null )
                IndexLoaded( this, EventArgs.Empty );
        }

        private void TextIndexProcessor_ThreadStarted( object sender, EventArgs e )
        {
            _queryManager = new TextQueriesOptimizationManager( this, _textIndexer );
            _textIndexer.Initialize();
            if( !IsIndexPresent() )
            {
                if( !IdleIndexingMode )
                {
                    RebuildIndexImpl();
                }
            }
            else
            {
                FixUnindexedResources();
            }
            ThreadStarted -= TextIndexProcessor_ThreadStarted;
            ThreadPriority = System.Threading.ThreadPriority.Lowest;
            Trace.WriteLine( "-- TextIndexManager -- Thread started." );
        }

        public void StartIndexingThread()
        {
            Trace.WriteLine( "-- TextIndexManager -- Starting thread." );
            StartThread();
        }
        #endregion Ctor and Initialization

        #region SearchProviders Registration API
        public void  RegisterSearchProvider( ISearchProvider host, string title )
        {
            _SearchProviders[ host ] = new Pair( host.Title, host.Icon );
            AddProviderToGroup( host, _cOthersProvidersGroupName );
        }

        public void  RegisterSearchProvider( ISearchProvider host, string title, string group )
        {
            _SearchProviders[ host ] = new Pair( host.Title, host.Icon );
            AddProviderToGroup( host, group );
        }

        public void  UnregisterSearchProvider( ISearchProvider host )
        {
            if( _SearchProviders.Contains( host ) )
                _SearchProviders.Remove( host );
        }

        public ISearchProvider CurrentSearchProvider
        {
            get{  return _currentSP;  }
            set{  _currentSP = value; }
        }

        public string GetSearchProviderTitle( ISearchProvider host )
        {
            HashMap.Entry e = _SearchProviders.GetEntry( host );
            return (e == null) ? null : (String) ((Pair) e.Value).First;
        }

        public ISearchProvider[]  GetSearchProviders()
        {
            int i = 0;
            ISearchProvider[] array = new ISearchProvider[ _SearchProviders.Count ];
            foreach( HashMap.Entry e in _SearchProviders )
                array[ i++ ] = (ISearchProvider) e.Key;

            return array;
        }

        private void AddProviderToGroup( ISearchProvider host, string group )
        {
            ArrayList hosts;
            HashMap.Entry e = _ProvidersGroups.GetEntry( group );
            if( e == null )
            {
                hosts = new ArrayList();
                _ProvidersGroups.Add( group, hosts );
            }
            else
                hosts = (ArrayList) e.Value;

            hosts.Add( host );
        }

        //---------------------------------------------------------------------
        //  Return group names in arbitrary order except that the name of the
        //  standard group always comes first.
        //---------------------------------------------------------------------
        public string[] GetSearchProviderGroups()
        {
            ArrayList groups = new ArrayList();
            groups.Add( _cStandardProvidersGroupName );
            foreach( HashMap.Entry e in _ProvidersGroups )
            {
                if( (string) e.Key != _cStandardProvidersGroupName )
                    groups.Add( e.Key );
            }
            return (string[]) groups.ToArray( typeof( string ));
        }

        public ISearchProvider[] GetSearchProvidersInGroup( string group )
        {
            ArrayList list = (ArrayList)_ProvidersGroups[ group ];
            return (ISearchProvider[]) list.ToArray( typeof( ISearchProvider) );
        }
        #endregion SearchProviders Registration API

        #region Getters/Setters
        public  FullTextIndexer FullTextIndexer    {  get { return _textIndexer; } }
        public  int  UprocessedJobsInQueue         {  get { return _pendingDocs.Count; } }

        public bool IdleIndexingMode
        {
            get
            {
                if( _isIdleIndexing == null )
                {
                    _isIdleIndexing = ObjectStore.ReadBool( "TextIndex", "IdleIndexingMode", false );
                }
                return (bool) _isIdleIndexing;
            }
            set
            {
                bool lastValue = (bool) _isIdleIndexing;
                _isIdleIndexing = value;
                if( lastValue && !value )
                {
                    QueueJob( JobPriority.Immediate, new MethodInvoker( FixUnindexedResources ) );
                }
                if( lastValue != value )
                {
                    ObjectStore.WriteBool( "TextIndex", "IdleIndexingMode", value );
                }
                QueueSwitchToIdleModeJob();
            }
        }

        public void SetExceptionHandler( AsyncExceptionHandler handler )
        {
            ExceptionHandler = handler;
        }

        ///<summary>
        /// NextUpdateFinished event is raised when new portion of documents is merged
        /// into main or incremental index chunk and thus is available for searching.
        /// </summary>
        public void SetUpdateResultHandler( UpdateFinishedEventHandler h )
        {
            _textIndexer.NextUpdateFinished += h;
        }
        #endregion Getters/Setters

        #region Idle processing

        private class SwitchToIdleModeJob: AbstractJob
        {
            private readonly TextIndexManager _textIndexManager;

            public SwitchToIdleModeJob( TextIndexManager textIndexManager )
            {
                _textIndexManager = textIndexManager;
            }

            protected override void Execute()
            {
                if( _textIndexManager.IdleIndexingMode )
                {
                    _textIndexManager.FixUnindexedResources();
                    _textIndexManager.QueueGotEmpty += _textIndexManager.QueueGotEmptyImpl;
                }
            }
        }

        private void QueueSwitchToIdleModeJob()
        {
            QueueIdleJob( JobPriority.Immediate, _switchToIdleJob );
        }

        private void QueueGotEmptyImpl( object sender, EventArgs e )
        {
            if( !Core.IsSystemIdle )
            {
                QueueSwitchToIdleModeJob();
                QueueGotEmpty -= QueueGotEmptyImpl;
            }
        }

        #endregion

        #region Suspend/Resume Indexing
        public bool  IsIndexingSuspended  {  get { return _isCriticalIOCaseFlag || _isManuallySuspended; } }
        public bool  IsManuallySuspended  {  get { return _isManuallySuspended; }  }

        internal void  SuspendIndexingByError()
        {
            _isCriticalIOCaseFlag = true;
        }

        internal void  SuspendIndexingByUser()
        {
            _isManuallySuspended = true;
            CancelJobs();
            _textIndexer.FlushIndices();
        }

        internal void  ResumeIndexingByUser()
        {
            _isManuallySuspended = false;
            IterateOverResources( true );
        }
        #endregion Suspend/Resume Indexing

        #region Reject And Disposal
        public override void Dispose()
        {
            Trace.WriteLine( "--TextIndexManager -- Dispose has started." );

            ThreadFinished += _textIndexProcessor_ThreadFinished;
            Trace.WriteLine( "--TextIndexManager -- Thread finished Handle is attached." );
            base.Dispose();
            Trace.WriteLine( "--TextIndexManager -- Dispose has finished." );
        }

        private void _textIndexProcessor_ThreadFinished( object sender, EventArgs e )
        {
            Trace.WriteLine( "--TextIndexManager -- [Thread finished] event has been fired." );

            //  Build the first chunck if there is no index yet, otherwise it
            //  will be rebuilt once more.
            if( !IsIndexPresent() )
            {
                EndBatchUpdate();
            }
            Trace.WriteLine( "--TextIndexManager -- Flush indices has started." );
            _textIndexer.CloseIndices();
            Word.DisposeTermTrie();
            Trace.WriteLine( "--TextIndexManager -- Flush indices has finished." );
        }

        //  NB: Purpose of this method is for tests only!!!
        internal void FlushAndCloseIndices()
        {
            _textIndexer.CloseIndices();
        }
        #endregion Reject And Disposal

        #region Defragmentation Control
        internal void StartDefragmentationWaiting()
        {
            Trace.WriteLineIf( !FullTextIndexer._suppTrace, "-- TextIndexManager -- Started waiting for IDLE status" );
            _idleWaitingStarted = true;
        }
        internal void DefragmentationWaitingEnded()
        {
            Trace.WriteLineIf( !FullTextIndexer._suppTrace, "-- TextIndexManager -- Finished waiting for IDLE status" );
            _idleWaitingStarted = false;
        }

        //---------------------------------------------------------------------
        //  - Check for defragmentation necessity every hour
        //  - "DaysBetweenDefragmentations" days must pass between two
        //    subsequent defragmentations
        //  - When defragmentation condition takes place, queue IDLE uow so that
        //    it does not disturb any other process.
        //---------------------------------------------------------------------
        private void SetupDefragmentationQueue()
        {
            Trace.WriteLineIf( !FullTextIndexer._suppTrace, "TextIndexManager -- Setup defragmentation queue." );
            QueueJobAt( DateTime.Now.AddHours( 1.0 ), new MethodInvoker( SetupDefragmentationQueue ) );

            int      daysBetweenDefrags = Core.SettingStore.ReadInt( "Defragmentation", "Interval", _cDefaultDaysBetweenDefrags );
            DateTime lastDefrag = Core.SettingStore.ReadDate( "Defragmentation", "LastDefragmentation", DateTime.MinValue );

            if( lastDefrag == DateTime.MinValue )
            {
                Core.SettingStore.WriteDate( "Defragmentation", "LastDefragmentation", DateTime.Now );
            }
            else
            if( !_idleWaitingStarted &&
                ( lastDefrag.AddDays( daysBetweenDefrags ) < DateTime.Now ) &&
                ( Core.State == CoreState.Running ))
            {
                StartDefragmentationWaiting();
                Trace.WriteLineIf( !FullTextIndexer._suppTrace, "TextIndexManager -- can queue idle job since last date is [" + lastDefrag + "]" );
                QueueIdleJob( new DefragmentIndexJob() );
            }
        }
        #endregion Defragmentation Control

        #region EndBatchUpdate

        internal void EndBatchUpdate()
        {
            _documentsIndexed = 0;

            try
            {
                _textIndexer.EndBatchUpdate();
            }
            catch( FormatException ex )
            {
                Core.ReportException( ex, ExceptionReportFlags.AttachLog );
                RebuildIndex();
                return;
            }
            catch( System.IO.IOException )
            {
                Core.UIManager.ShowSimpleMessageBox( "Text Index Operation Failed",
                                                     "System encountered a serious I/O error while constructing text index." +
                                                     " Indexing operation will be suspended until next start of the Omea.");
                SuspendIndexingByError();
                return;
            }

            if( _statusWriter != null )
                _statusWriter.ClearStatus();
            if ( IndexLoaded != null )
            {
                IndexLoaded( this, EventArgs.Empty );
            }

            AnalyzeFreeSpace();
        }
        #endregion EndBatchUpdate

        #region Indexing querying

        public void QueryIndexing( int docID )
        {
            QueryIndexingImpl( docID, true );
        }

        private void QueryIndexingImpl( int docID, bool invokeProcessingPendingDocs )
        {
            //  Put new job only if the corresponding mode is appropriate:
            //  - either index in real time, or
            //  - index in idle mode and
            if( !IdleIndexingMode || Core.IsSystemIdle )
            {
                //  Do not act on a resource which was possibly deleted just
                //  before the indexing.
                if( docID >= 0 )
                {
                    #region Pending Data Processing
                    _pendingDocsLock.Enter();
                    try
                    {
                        _pendingDocs.Add( docID );
                    }
                    finally
                    {
                        _pendingDocsLock.Exit();
                    }
                    #endregion Pending Data Processing

                    if( invokeProcessingPendingDocs )
                    {
                        QueueProcessingPendingDocs();
                    }
                }
            }
        }

        private void QueueProcessingPendingDocs()
        {
            QueueJobAt( DateTime.Now.AddSeconds( 3 ), new MethodInvoker( DeferredProcessingPendingDocs ) );
        }
        private void DeferredProcessingPendingDocs()
        {
            QueueJob( JobPriority.Lowest, _processPendingDocsDelegate );
        }

        public void  QueueContextExtraction( IPropertyProvider provider, object e, string[] lexemes )
        {
            QueueJob( JobPriority.AboveNormal, new CalcContextUOW( (SimplePropertyProvider)provider, (Entry) e, lexemes ) );
        }
        #endregion Indexing querying

        #region Indexing itself

        private void ProcessPendingDocs()
        {
            int processed = 0;
            while( ( !IdleIndexingMode || Core.IsSystemIdle ) && !Finished && !IsIndexingSuspended )
            {
                int docId = GetNextDocId();
                if( docId == -1 )
                    break;

                processed++;
                if( JobStartingDelegate != null )
                {
                    JobStartingDelegate( this, EventArgs.Empty );
                }
                IndexDocument( docId );
                if( JobFinishedDelegate != null )
                {
                    JobFinishedDelegate( this, EventArgs.Empty );
                }
                if( OutstandingJobs > 0 )
                {
                    QueueProcessingPendingDocs();
                    return;
                }
            }
            if( processed > 0 )
            {
                EndBatchUpdate();
            }
        }

        private int  GetNextDocId()
        {
            int docId = -1;
            _pendingDocsLock.Enter();
            try
            {
                foreach( IntHashSet.Entry e in _pendingDocs )
                {
                    docId = e.Key;
                    break;
                }
                if( docId >= 0 )
                {
                    _pendingDocs.Remove( docId );
                }
            }
            finally
            {
                _pendingDocsLock.Exit();
            }
            return docId;
        }

        private void IndexDocument( int docId )
        {
            string jobNameSaved = _processPendingDocsDelegate.Name;
            try
            {
                IResource resource;
                try
                {
                    resource = Core.ResourceStore.LoadResource( docId );
                    StringBuilder builder = StringBuilderPool.Alloc();
                    try
                    {
                        builder.Append( "Indexing \"" );
                        builder.Append( resource.DisplayName );
                        builder.Append( '\"' );
                        _processPendingDocsDelegate.Rename(builder.ToString());
                    }
                    finally
                    {
                        StringBuilderPool.Dispose( builder );
                    }
                }
                catch( StorageException )
                {
                    return;  // it's OK if the document is already deleted
                }

                try
                {
                    //  Do not even try to call outside components in this state.
                    //  Let this document reside in the "unflushed.dat" unitl next
                    //  run of the program.
                    Core.PluginLoader.InvokeResourceTextProviders( resource, FullTextIndexer );
                }
                catch( System.IO.IOException )
                {
                    Core.UIManager.ShowSimpleMessageBox( "Text Index Operation Failed",
                        "System encountered a serious I/O error while constructing text index." +
                        " Indexing operation will be suspended until next start of the Omea.");
                    SuspendIndexingByError();
                }
                catch( FormatException ex )
                {
                    Core.ReportBackgroundException( ex );
                    //  RebuildIndex RUNS new job which first discards the
                    //  TI completely and then flushes the job queue.
                    RebuildIndex();
                }
                catch( Exception ex )
                {
                    Core.ReportBackgroundException( ex );
                }

                _documentsIndexed++;
                int totalDocs = UprocessedJobsInQueue;
                string message = "Indexing documents (" + _documentsIndexed + "/" + (totalDocs + _documentsIndexed) + ")";
                int percent = _documentsIndexed * 100 / (totalDocs + _documentsIndexed);
                UpdateProgress( percent, message, null );
            }
            finally
            {
                _processPendingDocsDelegate.Rename(jobNameSaved);
            }
        }
        #endregion

        #region Delete Document
        public void DeleteDocumentQueued( int resID )
        {
            if( IsIndexPresent() )
            {
                QueueJob( JobPriority.BelowNormal, new DeleteDocUOW( resID ) );
            }
        }

        internal void DeleteDocumentImmediate( int docID )
        {
            if( IsIndexPresent() )
                _textIndexer.DeleteDocument( docID );
        }
        #endregion Delete Document

        public bool IsIndexPresent()
        {
            return _textIndexer.IsIndexPresent;
        }

        public bool IsDocumentInIndex( int docID )
        {
            return _textIndexer.IsDocumentPresent( docID );
        }

        public void UpdateProgress( int percent, string message, string timeMessage )
        {
            if( _statusWriter != null )
            {
                DateTime dt = DateTime.Now;
                TimeSpan tsStatus = dt - _lastStatusUpdate;
                if ( tsStatus.TotalSeconds > 0.5 )
                {
                    _statusWriter.ShowStatus( message );
                    _lastStatusUpdate = dt;
                }
            }
        }

        #region Rebuild or Upgrade Index
        public void RebuildIndex()
        {
            UpdateProgress( 0, "Rebuilding Text Index...", null );
            RunJob( "Rebuilding Text Index...", new MethodInvoker( RebuildIndexImpl ) );
        }

        private void  RebuildIndexImpl()
        {
            _statusWriter.ShowStatus( "Rebuilding Text Index..." );
            CancelJobs();
            _textIndexer.DiscardTextIndex();

            IterateOverResources( false );
        }

        private void  FixUnindexedResources()
        {
            IterateOverResources( true );
        }

        private void  IterateOverResources( bool checkInIndex )
        {
            foreach( IResourceType resType in Core.ResourceStore.ResourceTypes )
            {
                if( IsResTypeIndexingConformant( resType ) )
                {
                    int  count = 0;
                    IResourceList resList = Core.ResourceStore.GetAllResources( resType.Name );
                    IResourceIdCollection ids = resList.ResourceIds;
                    for( int i = 0; i < ids.Count; ++i )
                    {
                        int resID = ids[ i ];
                        //  Fix for OM-12527 - illegal resource Ids can be met
                        //  during iteration (negative).
                        if( resID >= 0 )
                        {
                            if( !checkInIndex || !IsDocumentInIndex( resID ) )
                            {
                                QueryIndexingImpl( resID, false );
                                count++;
                            }
                        }
                    }
                    Trace.WriteLine( "-- TextIndexManager -- Finished requeuing " + count + " of " + ids.Count +
                                     " <" + resType.Name + "> resources in REBUILD phase." );
                    if( count > 0 )
                    {
                        QueueProcessingPendingDocs();
                    }
                }
            }
        }

        ///<summary>
        ///  For a resource to be text-indexed its resource type must conform
        ///  to the following criteria:
        ///  - have valid name
        ///  - be indexable
        ///  - its owner plugin must be loaded
        ///  - even if its plugin is loaded (or the owner may be omitted), it
        ///    must be either a file (for a FilePlugin to be able to index it) or
        ///    have some ITextIndexProvider, specific for this particular
        ///    resource type.
        /// </summary>
        public static bool  IsResTypeIndexingConformant( IResourceType resType )
        {
            return   !String.IsNullOrEmpty( resType.Name ) &&
                    !resType.HasFlag( ResourceTypeFlags.NoIndex ) &&
                     resType.OwnerPluginLoaded &&
                    ( resType.HasFlag( ResourceTypeFlags.FileFormat ) ||
                      Core.PluginLoader.HasTypedTextProvider( resType.Name ));
        }

        #endregion Rebuild or Upgrade Index

        #region Free Space Analysis
        private void  AnalyzeFreeSpace()
        {
            lock( this )
            {
                ulong freeSpace = IOTools.DiskFreeSpaceForUserDB( OMEnv.WorkDir );
                if( freeSpace < _cFreeSpaceMargin )
                {
                    if( !_isCriticalIOCaseFlag )
                    {
                        Core.UIManager.ShowSimpleMessageBox( "Text Indexing Failed", "Critical amount of free space is achieved on HD. Text indexing is suspended." );
                        SuspendIndexingByError();
                    }
                }
                else
                //  Check whether we can restore the ordinary way of indexing
                //  documents - suppose that the user freed enough space for
                //  growing TextIndex structures.
                if( _isCriticalIOCaseFlag )
                {
                    _isCriticalIOCaseFlag = false;
                }
            }
        }
        #endregion Free Space Analysis

        #region Query Processing Marshaling

        public bool MatchQuery( string query, IResource res )
        {
            bool matched = _queryManager.MatchResource( res, query );
            return matched;
        }

        //  Last parameter is used for Jobs disambiguation - when several request
        //  to _textIndexer are put in the queue, there is no other way do distinguish
        //  between these calls than the difference in this parameter.
        internal delegate FullTextIndexer.QueryResult  QueryRequest( string str, int dummy );

        public IResourceList   ProcessQuery( string query )
        {
            string[] stoplist;
            string   parseError;
            IHighlightDataProvider hldp;
            return ProcessQuery( query, null, out hldp, out stoplist, out parseError );
        }

        public IResourceList   ProcessQuery( string query, out IHighlightDataProvider hldp )
        {
            string[] stoplist;
            string parseError;
            return ProcessQuery( query, null, out hldp, out stoplist, out parseError );
        }

        public IResourceList   ProcessQuery( string query, int[] inDocIds, out IHighlightDataProvider hldp,
                                             out string[] stopList, out string parseError )
        {
            FullTextIndexer.QueryResult qr = _queryManager.QueryList( query );

            hldp = null;
            stopList = null;
            parseError = null;
            IResourceList result = Core.ResourceStore.EmptyResourceList;
            if( qr != null )
            {
                SimplePropertyProvider provider;
                result = ConvertResultList( qr.Result, qr.IsSingularTerm, inDocIds, out provider );

                if( QueryProcessor.LastSearchLexemes.Length > 0 )
                    hldp = new SearchHighlightDataProvider( this, qr.Result, provider, QueryProcessor.LastSearchLexemes  );

                stopList = QueryProcessor.LastStoplist;
                parseError = qr.ErrorMessage;
            }

            return result;
        }

        private static IResourceList ConvertResultList( Entry[] entries, bool isSingleTerm,
                                                        int[] inDocIDs, out SimplePropertyProvider provider )
        {
            List<int> IDs = new List<int>();
            provider = new SimplePropertyProvider();
            if ( entries != null )
            {
                for( int i = 0; i < entries.Length; i++ )
                {
                    if(( inDocIDs == null ) || ( Array.IndexOf( inDocIDs, entries[ i ].DocIndex ) != -1 ))
                    {
                        IDs.Add( entries[ i ].DocIndex );
                        provider.SetProp( entries[ i ].DocIndex, FullTextIndexer.SearchRankPropId, entries[ i ].TfIdf );
                        if( !isSingleTerm )
                            provider.SetProp( entries[ i ].DocIndex, FullTextIndexer.ProximityPropId, entries[ i ].Proximity );
                    }
                }
            }

            IResourceList result = Core.ResourceStore.ListFromIds( IDs, true );
            result.AttachPropertyProvider( provider );
            return result;
        }
        #endregion Query Processing Marshaling
    }

    #region SearchHighlightDataProvider
    /**
     * The class which implements IHighlightDataProvider on an array of search results.
     */

    public class SearchHighlightDataProvider: IHighlightDataProvider
	{
        private readonly IEnumerable             _entries;
        private readonly TextIndexManager        _textIndexManager;
        private readonly SimplePropertyProvider  _provider;
        private readonly string[]                _lexemes;

        public SearchHighlightDataProvider( IEnumerable entries, SimplePropertyProvider provider, string[] lexemes )
		{
            _entries = entries;
            _provider = provider;
            _lexemes = lexemes;
        }

        public SearchHighlightDataProvider( TextIndexManager textIndexManager, IEnumerable entries, SimplePropertyProvider provider, string[] lexemes )
        {
            _textIndexManager = textIndexManager;
            _entries = entries;
            _provider = provider;
            _lexemes = lexemes;
        }

        public bool GetHighlightData( IResource res, out WordPtr[] words )
        {
            words = null;
            if( _entries != null )
            {
                foreach( Entry e in _entries )
                {
                    if ( e.DocIndex == res.Id )
                    {
                        ContextCtor.GetHighlightedTerms( e, _lexemes, out words );
                        return true;
                    }
                }
            }
            return false;
        }

        public void RequestContexts( int[] resourceIDs )
        {
            if( Core.SettingStore.ReadBool( "Resources", "ShowSearchContext", true ) && _entries != null )
            {
                Trace.WriteLine( "--- HighlightProvider -- Starting context extraction" );
                foreach( Entry e in _entries )
                {
                    if( Array.IndexOf( resourceIDs, e.DocIndex ) != -1 )
                    {
                        _textIndexManager.QueueContextExtraction( _provider, e, _lexemes );
                    }
                }
            }
        }

        public string GetContext( IResource res )
        {
            return (string) _provider.GetPropValue( res, FullTextIndexer.ContextPropId );
        }

        public OffsetData[] GetContextHighlightData( IResource res )
        {
            ArrayList list = (ArrayList) _provider.GetPropValue( res, FullTextIndexer.ContextHighlightPropId );
            return (list == null) ? null : (OffsetData[]) list.ToArray( typeof( OffsetData ));
        }
	}
    #endregion SearchHighlightDataProvider

    #region ISearchProvider descendants
    public class OmeaGlobalSearchProvider : ISearchProvider
    {
        private const string _IconRes = "search.ico";
        private const string _ProviderTitle = "Search in all Omea resources";
        private Icon  _providerIcon = null;

        public string  Title
        {
            get {  return _ProviderTitle; }
        }

        public Icon    Icon
        {
            get
            {
                if( _providerIcon == null )
                    _providerIcon = MainFrame.LoadIconFromAssembly( _IconRes );

                return _providerIcon;
            }
        }

        public override string ToString() {  return "Global";  }

        public void  ProcessQuery( string query )
        {
			//-----------------------------------------------------------------
            //  Avoid perform any UI (and other, generally) work if we are
            //  shutting down - some components (like DefaultViewPane) may
            //  already be disposed.
			//-----------------------------------------------------------------
            if( Core.State != CoreState.Running )
                return;

			//-----------------------------------------------------------------
            //  Extract Search Extension subphrases, extract them out of the
            //  query and convert them into the list of conditions.
			//-----------------------------------------------------------------
            int       anchorPos;
            string[]  resTypes = null;
            bool      parseSuccessful = false;
            ArrayList conditions = new ArrayList();

            do
            {
                string    anchor;
                FindSearchExtension( query, out anchor, out anchorPos );
                if( anchorPos != -1 )
                    parseSuccessful = ParseSearchExtension( anchor, anchorPos, ref query, out resTypes, conditions );
            }
            while(( anchorPos != -1 ) && parseSuccessful );

			//-----------------------------------------------------------------
			//  Create condition from the query
			//-----------------------------------------------------------------
			IFilterRegistry fMgr = Core.FilterRegistry;
			IResource queryCondition = ((FilterRegistry) fMgr).CreateStandardConditionAux( null, query, ConditionOp.QueryMatch );
			FilterRegistry.ReferCondition2Template( queryCondition, fMgr.Std.BodyMatchesSearchQueryXName );

            conditions.Add( queryCondition );

			//-----------------------------------------------------------------
            bool showContexts = Core.SettingStore.ReadBool( "Resources", "ShowSearchContext", true );
            bool showDelItems = Core.SettingStore.ReadBool( "Search", "ShowDeletedItems", true );
            IResource[]  condsList = (IResource[]) conditions.ToArray( typeof(IResource) );
			IResource view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", Core.FilterRegistry.ViewNameForSearchResults );
			if( view != null )
                fMgr.ReregisterView( view, fMgr.ViewNameForSearchResults, resTypes, condsList, null );
			else
                view = fMgr.RegisterView( fMgr.ViewNameForSearchResults, resTypes, condsList, null );
			Core.FilterRegistry.SetVisibleInAllTabs( view );

			//-----------------------------------------------------------------
			//  Set additional properties characteristic only for "Search Results"
            //  view.
			//-----------------------------------------------------------------
			ResourceProxy proxy = new ResourceProxy( view );
			proxy.BeginUpdate();
            proxy.SetProp( Core.Props.Name, AdvancedSearchForm.SearchViewPrefix + query );
            proxy.SetProp( "_DisplayName", AdvancedSearchForm.SearchViewPrefix + query );
            proxy.SetProp( Core.Props.ShowDeletedItems, showDelItems);
			proxy.SetProp( "ShowContexts", showContexts );
			proxy.SetProp( "ForceExec", true );
            if( Core.SettingStore.ReadBool( "Search", "AutoSwitchToResults", true ) )
                proxy.SetProp( "RunToTabIfSingleTyped", true );
            else
                proxy.DeleteProp( "RunToTabIfSingleTyped" );
			proxy.EndUpdate();

			//-----------------------------------------------------------------
			//  Add new view to the panel
            //  Some steps to specify the correct user-ordering for the new view.
			//-----------------------------------------------------------------
			Core.ResourceTreeManager.LinkToResourceRoot( view, int.MinValue );

            new UserResourceOrder( Core.ResourceTreeManager.ResourceTreeRoot ).Insert( 0, new int[] {view.Id}, false, null );

			//-----------------------------------------------------------------
            //  If we still in the Running mode we can do some UI work...
			//-----------------------------------------------------------------
            if( Core.State == CoreState.Running )
            {
    			Core.UIManager.BeginUpdateSidebar();
	    		Core.LeftSidebar.ActivateViewPane( StandardViewPanes.ViewsCategories );
		    	Core.UIManager.EndUpdateSidebar();
			    Core.LeftSidebar.DefaultViewPane.SelectResource( view );
            }
        }

        private static void FindSearchExtension( string query, out string prep, out int prepPosition )
        {
            prepPosition = -1;
            prep = null;

            string[] preps = Core.SearchQueryExtensions.GetAllAnchors();
            for( int i = 0; i < preps.Length; i++ )
            {
                int pos = query.LastIndexOf( " " + preps[ i ] + " " );
                if( pos > prepPosition )
                {
                    prep = preps[ i ];
                    prepPosition = pos;
                }
            }
        }

        private static bool ParseSearchExtension( string prep, int prepPosition, ref string query,
                                                  out string[] resTypes, IList conditions )
        {
            string    savedQuery = query;
            ArrayList types = new ArrayList();
            IResource condition = null;

            string tokens = query.Substring( prepPosition + prep.Length + 2 );
            query = query.Substring( 0, prepPosition );
            resTypes = null;

            string[] anchors = tokens.Split( ' ' );
            for( int i = 0; i < anchors.Length; i++ )
            {
                string resType = Core.SearchQueryExtensions.GetResourceTypeRestriction( prep, anchors[ i ] );
                condition = Core.SearchQueryExtensions.GetSingleTokenRestriction( prep, anchors[ i ] );
                if( resType != null )
                {
                    types.Add( resType );
                }
                else
                if( condition != null )
                {
                    conditions.Add( condition );
                }
                else
                {
                    condition = Core.SearchQueryExtensions.GetMatchingFreestyleRestriction( prep, tokens );
                    if( condition != null )
                        conditions.Add( condition );
                    else
                    {
                        //  Restore the query to the original text in the case when
                        //  no alternative matched.
                        query = savedQuery;
                    }
                    break;
                }
            }

            if( types.Count > 0 )
                resTypes = (string[]) types.ToArray( typeof( string ));

            return (resTypes != null) || (condition != null);
        }
     }

    public class OmeaQuickSearchProvider : ISearchProvider
    {
        private const string _ProviderTitle = "Search in the current view";
        private Icon  _providerIcon = null;

        public string Title
        {
            get {  return _ProviderTitle; }
        }

        public Icon Icon
        {
            get
            {
                if( _providerIcon == null )
                    _providerIcon = MainFrame.LoadIconFromAssembly( "previewright.ico" );

                return _providerIcon;
            }
        }

        public override string ToString()
        {
            return "local";
        }

        public void  ProcessQuery( string query )
        {
			//-----------------------------------------------------------------
            //  Avoid perform any UI (and other, generally) work if we are
            //  shutting down - some components (like DefaultViewPane) may
            //  already be disposed.
			//-----------------------------------------------------------------
            if( Core.State != CoreState.Running )
                return;

            IResourceList _resourceListBeforeQuickFind = Core.ResourceBrowser.VisibleResources;
            const string _captionBeforeQuickFind = "_caption";
            IHighlightDataProvider highlightDataProvider;
            TextIndexManager man = (TextIndexManager) Core.TextIndexManager;

            IResourceList searchResults = man.ProcessQuery( query, out highlightDataProvider );
            searchResults = searchResults.Intersect( _resourceListBeforeQuickFind, true );

            string quickFindCaption = query;
            if ( !quickFindCaption.StartsWith( "\"" ) || !quickFindCaption.EndsWith( "\"" ) )
            {
                quickFindCaption = "'" + quickFindCaption + "'";
            }

            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.Caption = "Search results for " + quickFindCaption + " in " + _captionBeforeQuickFind;
            options.HighlightDataProvider = highlightDataProvider;
            options.SuppressContexts = true;
            options.ShowNewspaper = Core.ResourceBrowser.NewspaperVisible;
            IResource ownerView = Core.ResourceBrowser.OwnerResource;
            Core.ResourceBrowser.DisplayResourceList( ownerView, searchResults, options );
            Core.ResourceBrowser.FocusResourceList();
        }
    }
    #endregion ISearchProvider descendants
}
