/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.TextIndex;
using JetBrains.Omea.OpenAPI;	
using JetBrains.Omea.Containers;
using JetBrains.DataStructures;

namespace JetBrains.Omea.TextIndex
{
    /**
     * Class that manages the construction of the full-text index.
     */

    public class FullTextIndexer: IResourceTextConsumer
    {
        public FullTextIndexer()
        {
            theIndexer = this;
            RegisterPropertyTypes();
            IndexConstructor.WorkDir = OMEnv.WorkDir;
        }
        public static FullTextIndexer Instance  {  get{ return theIndexer; } }
        public void  RejectResult()             {}
        public TextRequestPurpose Purpose       {  get{ return TextRequestPurpose.Indexing;  }  }

        public void  Initialize()
        {
            #region Preconditions
            if ( _textParser != null )
                throw new InvalidOperationException( "FillTextIndexer.Initialize() invoked twice" );
            #endregion Preconditions

            _textParser  = new TextDocParser();

            _suppTrace = Core.SettingStore.ReadBool( "TextIndexing", "SuppressTraces", false );
            CleanIndexTempFiles();
            LoadExistingIndices();
        }

        #region Index Cleaning and Loading on Startup
        private static void CleanIndexTempFiles()
        {
            DeleteFile( OMEnv.TermIndexFileName + OMEnv.IncChunkExtension );
            DeleteFile( OMEnv.TermIndexFileName + OMEnv.IncChunkExtension + OMEnv.HeaderExtension );

            //  Necessary for old-formatted files.
            //  New format does not use any temp files for doc index.
            DeleteFile( "_doc.index.tmp" );
            DeleteFile( "_doc.index" + OMEnv.IncChunkExtension );
            DeleteFile( "_doc.index" + OMEnv.IncChunkExtension + OMEnv.HeaderExtension );
        }

        //---------------------------------------------------------------------
        //  Split loading of the text index into two phases: first, load main
        //  (and thus the largest) chunk of the index; then load smaller one.
        //  Since chunk mergings are most often done with the small chunk,
        //  we do not loose most of the information if the corruption has been
        //  made during these mergings.
        //---------------------------------------------------------------------
        private void LoadExistingIndices()
        {
            Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- full index is found during start-up - initializing accessors" );
            try
            {
                if( TermIndexAccessor == null )
                {
                    DiscardTextIndex();
                }
                else 
                {
                    NotifyIndexLoaded();

                    //  On startup, clean working directory - delete temp files
                    //  produce by the defragmentation module. Files may not be
                    //  removed in that module if e.g. there is no space for closing
                    //  the output file.
                    CleanPossibleUndeletedFiles();
                }
                if( _needDiscard )
                {
                    DiscardTextIndex();
                }
            }
            catch( FormatException )
            {
                Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- Found a corrupted index while loading accessors." );
                DiscardTextIndex();
            }
            catch( IOException e )
            {
                Trace.WriteLine( e.Message );
                Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- Serious IO Exception occured while loading accessors." );
                DiscardTextIndex();
            }
        }

        private void RerequestDocVersionsIndexing()
        {
            Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- " + DocVersionsToProcess.Count + " documents is rerequested for indexing" );
            foreach( IntHashTableOfInt.Entry e in DocVersionsToProcess )
            {
                Trace.WriteLineIf( !_suppTrace,  "                      " + e.Key + " ID" );
                Core.TextIndexManager.QueryIndexing( e.Key );
            }
            DocVersionsToProcess.Clear();
        }
        #endregion Index Loading on Startup

        #region Registration
        private void  RegisterPropertyTypes()
        {
            IPropTypeCollection props = Core.ResourceStore.PropTypes;
            SearchRankPropId = props.Register( "SearchRank", PropDataType.Double, PropTypeFlags.Virtual );
            props.RegisterDisplayName( SearchRankPropId, "Search Rank" );

            SimilarityPropId = props.Register( "Similarity", PropDataType.Double, PropTypeFlags.Virtual );
            ContextPropId    = props.Register( "Context", PropDataType.String, PropTypeFlags.Virtual );
            ProximityPropId  = props.Register( "Proximity", PropDataType.Int, PropTypeFlags.Virtual );
            ContextHighlightPropId = props.Register( "HighlightContext", PropDataType.String, PropTypeFlags.Virtual );

            _needDiscard = !props.Exist( "InTextIndex" ) || !File.Exists( OMEnv.TokenTreeFileName );
            DocInIndexProp = props.Register( "InTextIndex", PropDataType.Bool, PropTypeFlags.Internal );

            props.Register( DocumentSectionResource.SectionHelpDescription, PropDataType.String, PropTypeFlags.Internal );
            props.Register( "SectionShortName", PropDataType.String, PropTypeFlags.Internal );
            props.Register( "SectionOrder", PropDataType.Int, PropTypeFlags.Internal );

            Core.ResourceStore.ResourceTypes.Register( DocumentSectionResource.DocSectionResName, "", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Core.ResourceStore.RegisterUniqueRestriction( DocumentSectionResource.DocSectionResName, props[ "Name" ].Id );

            RegisterDocumentSection( DocumentSection.BodySection, "Full content of the resource", null );
            RegisterDocumentSection( DocumentSection.SubjectSection, "Describes subject (or heading, title) of the e-mail, article, etc", "SU" );
            RegisterDocumentSection( DocumentSection.AnnotationSection, "A note added by way of comment or explanation", "AN" );
            RegisterDocumentSection( DocumentSection.SourceSection, "Identifies a source of the resource - person, site, server, etc.", "SRC" );
        }
        private IResource RegisterDocumentSection( string sectionName )
        {
            return RegisterDocumentSection( sectionName, null, null );
        }
        private IResource RegisterDocumentSection( string sectionName, string description, string shortName )
        {
            int   sectionNum;
            IResource section = Core.ResourceStore.FindUniqueResource( DocumentSectionResource.DocSectionResName, "Name", sectionName );
            if( section == null )
            {
                sectionNum = Core.ResourceStore.GetAllResources( DocumentSectionResource.DocSectionResName ).Count;
                ResourceProxy proxy = ResourceProxy.BeginNewResource( DocumentSectionResource.DocSectionResName );
                proxy.BeginUpdate();
                proxy.SetProp( "Name", sectionName );
                proxy.SetProp( "SectionOrder", sectionNum );

                if( String.IsNullOrEmpty( description ) )
                    proxy.SetProp( "SectionHelpDescription", description );
                if( String.IsNullOrEmpty( shortName ) )
                    proxy.SetProp( "SectionShortName", shortName );

                proxy.EndUpdate();
                section = proxy.Resource;
            }
            else
                sectionNum = section.GetIntProp( "SectionOrder" );

            SectionsMapping[ sectionName ] = sectionNum;

            return section;
        }
        #endregion Registration

        #region Document Scope
        public void  AddDocumentHeading( int docID, string text )
        {
            AddDocumentFragment( docID, text, DocumentSection.SubjectSection );
        }
        public void  AddDocumentFragment( int docID, string text )
        {
            AddDocumentFragment( docID, text, DocumentSection.BodySection );
        }
        public void  AddDocumentFragment( int docID, string text, string sectionName )
        {
            if( docID < 0 )  return;   //  secure against just deleted resources
            if( text == null ) return; //  secure against null(s).

            //  Ignore new versions of the same document which is under 
            //  processing now, remember its Id and rerequest its indexing
            //  later when current version of the document will be already
            //  in the index. Such scheme significantly reduces the
            //  complexity of maintaining term and doc statistics.
            if( _finishedDocsInBatch.ContainsKey( docID ))
            {
                Trace.WriteIf( !_suppTrace,  "-- FullTextIndexer -- new version of doc " + docID );
                IResource resource = Core.ResourceStore.TryLoadResource( docID );
                if ( resource != null )
                {
                    string type = " has come (" + resource.Type + ")";
                    Trace.WriteIf( !_suppTrace,  type );
                }
                Trace.WriteLineIf( !_suppTrace, " " );

                DocVersionsToProcess[ docID ] = 1;
            }
            else
            {
                uint sectionId = (uint) CheckSection( sectionName );

                // finish with previous document, start new
                if( docID != _lastDocID )
                {
                    DocumentDone(); 
                    CheckPreviewSign( docID );
                }

                //  deal with new fragment of the last document or a completely new doc.
                ProcessDocument( docID, text, sectionId );
                _lastDocID = docID;
            }
        }

        //  This method is called when text provider wants to submit new chunk
        //  of text of different section. Offset starts from the beginning for
        //  the more convenient processing of highlights during the search.
        public void  RestartOffsetCounting()
        {
            _textParser.FlushOffset();
        }

        public void  IncrementOffset( int spacesAmount )
        {
            #region Preconditions
            if ( spacesAmount < 0 )
                throw new ArgumentOutOfRangeException( "spacesAmount", "FullTextIndexer -- Amount of spaces to be passed is negative." );
            #endregion Preconditions

            _textParser.IncrementOffset( spacesAmount );
        }

        private int CheckSection( string sectionName )
        {
            int secId;
            HashMap.Entry entry = SectionsMapping.GetEntry( sectionName );
            if( entry != null ) {
                secId = (int) entry.Value;
            }
            else
            {
                IResource section = RegisterDocumentSection( sectionName );
                secId = section.GetIntProp( "SectionOrder" );
            }
            return secId;
        }

        private void  CheckPreviewSign( int docId )
        {
            IResource res = Core.ResourceStore.TryLoadResource( docId );
            _mustConstructPreview = (res != null) && res.HasProp( Core.Props.NeedPreview );
        }

        private void  ProcessDocument( int docID, string text, uint sectionId )
        {
            #region Preconditions
            if ( _textParser == null )
                throw new ApplicationException( "FullTextIndexer -- Impossible situation when Parser is NULL" );
            #endregion Preconditions

            //  Every new section is a logical delimitation. Thus even if it
            //  does not contain punctuational delimiter we can be sure that
            //  NEAR op searches will correctly separate tokens by this
            //  artificial border.
            if( _prevSectionId != sectionId )
                _textParser.IncrementSentence();

            _prevSectionId = sectionId;

            if( docID != _lastDocID )
                _textParser.Init( text ); // allow ""
            else
                _textParser.Next( text );

            if( _mustConstructPreview && _previewFragment.Length < _cPreviewSize )
            {
                _previewFragment.Append( text );
                if( _previewFragment.Length > _cPreviewSize )
                    _previewFragment.Length = _cPreviewSize;
            }

            Word  word = _textParser.getNextWord();
            while( word.Tag != Word.TokenType.eoEOS )
            {
                if( isValuableToken( word ) )
                {
                    if( sectionId > 0 )
                        word.SectionId = sectionId;

                    OMEnv.LexemeConstructor.NormalizeToken( word );
                    word.SetId();
                    LogTerm( word );
                }
                word = _textParser.getNextWord();
            }
        }

        private void LogTerm( Word word )
        {
            int  HC = word.HC;

            IntHashTableOfInt.Entry e = _termCounterInDoc.GetEntry( HC );

            //-----------------------------------------------------------------
            //  update term's count in this doc
            //-----------------------------------------------------------------
            int  termFreq;
            if( e == null )
                termFreq = _termCounterInDoc[ HC ] = 1;
            else
                e.Value = termFreq = e.Value + 1;

            //  _termMaxFrequency is declared as ushort. And we artificially limit
            //  its upper value to some value (near to Uint16.MaxValue) to avoid
            //  integer overflow.
            if( _termMaxFrequency < termFreq )
                _termMaxFrequency = (ushort)Math.Min( termFreq, _ciMaxMeaningfulCount );

            //-----------------------------------------------------------------
            long mask = MaskEncoder.Mask( word.TokenOrder, word.SentenceNumber, word.StartOffset );

            IntHashTable.Entry entry = _tokens.GetEntry( HC );
            if( entry == null )
            {
                _tokens[ HC ] = mask;
            }
            else
            {
                LongArrayList offsets = entry.Value as LongArrayList;
                if( offsets == null )
                {
                    offsets = new LongArrayList( 4 );
                    offsets.Add( (long) entry.Value );
                    entry.Value = offsets;
                }
                offsets.Add( mask );
            }
        }

        private void  DocumentDone()
        {
            if( _lastDocID != -1 )
            {
                _finishedDocsInBatch[ _lastDocID ] = _termMaxFrequency;
                Trace.WriteLineIf( _termCounterInDoc.Count == 0, "-- FullTextIndexer - Skip empty document " + _lastDocID );

                //  Notify that new document is ready to be inserted to the text index.
                //  Event receivers can use the resource's data computed so far to
                //  precheck its internal conditions (e.g. whether resource is matched
                //  over some text condition, so that optimize text querying...)

                if( _termCounterInDoc.Count > 0 && ResourceProcessed != null )
                    ResourceProcessed( _lastDocID, null );

                //  Prepare data for the next document
                _termMaxFrequency = 0;
                _termCounterInDoc.Clear();
                ManageIndexChunk();
                _tokens.Clear();
                _previewFragment.Length = 0;
                _mustConstructPreview = false;
                Flush();
                if( _lastCollectTick + 5000 < Environment.TickCount )
                {
                    GC.Collect();
                    _lastCollectTick = Environment.TickCount;
                }
            }
        }

        #endregion Document Scope

        #region EndBatchUpdate and Chunk Construction

        private void Flush()
        {
            Word.FlushTermTrie();
            if( _termsAccessor != null ) 
            {
                _termsAccessor.Flush();
            }
        }

        public void EndBatchUpdate()
        {
            if( _lastDocID == -1 )
            {
                Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- EndBatchUpdate is called for empty batch" );
            }
            DocumentDone();
            TraceUpdateInformation();
            PropagateIndexInformation();
            Cleanup();
            RerequestDocVersionsIndexing();
        }

        private void  TraceUpdateInformation()
        {
            Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- EndBatchUpdate, " + _finishedDocsInBatch.Count + " docs processed, LastDocID=" + _lastDocID );
        }

        private void  ManageIndexChunk()
        {
            if( _tokens.Count == 0 ) return;
            try
            {
                FlushDocument();
                IResource doc = Core.ResourceStore.TryLoadResource( _lastDocID );
                if( doc != null ) 
                {
                    _pendingLock.Enter();
                    try
                    {
                        _pendingAddends.Add( _lastDocID );
                        _pendingDeletions.Remove( _lastDocID );
                    }
                    finally
                    {
                        _pendingLock.Exit();
                    }
                    Core.ResourceAP.QueueJob( JobPriority.Immediate, _cJobName, new ResourceDelegate( SetIndexedProps ), doc );
                }
            }
            catch( IOException e )
            {
                Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- Fatal IO Exception occured while constructing text index." );
                DiscardTextIndex();

                throw new IOException( "FullTextIndexer -- IO Exception in chunk construction", e );
            }
        }

        private void  FlushDocument()
        {
            IndexConstructor.FlushDocument( TermIndexAccessor, _lastDocID, _termMaxFrequency, _tokens );
        }

        /// <summary>
        /// Mark resource with a flag that it is now in the Text Index and assign
        /// a preview fragment (if necessary).
        /// </summary>
        private void SetIndexedProps( IResource res )
        {
            if( !res.IsDeleted )
            {
                res.SetProp( DocInIndexProp, true );

                string preview = _previewFragment.ToString();
                if( preview.Length > 0 )
                    res.SetProp( Core.Props.PreviewText, preview );

                _pendingLock.Enter();
                try
                {
                    _pendingAddends.Remove( _lastDocID );
                }
                finally
                {
                    _pendingLock.Exit();
                }
            }
        }

        ///<summary>
        ///  Fire the event that new portion of documents is merged to major or
        ///  incremental index chunk so that:
        /// <para>
        ///  - they are indexed successfully and there is no need to remember them
        ///    in "unflushed" pool;
        /// </para><para>
        ///  - they are available for searching or search-dependent rules completion.
        /// </para><para>
        ///  NB: though amount of submitted tokens may be zero, set of propagated
        ///      documents may be non-empty because of empty documents.
        /// </para>
        ///</summary>
        private void  PropagateIndexInformation()
        {
            if( _finishedDocsInBatch.Count > 0 )
            {
                NotifyIndexLoaded();

                IntArrayList  newDocsInChunk = IntArrayListPool.Alloc();
                try
                {
                    foreach( IntHashTableOfInt.Entry e in _finishedDocsInBatch )
                        newDocsInChunk.Add( e.Key );

                    PropagateSearchableDocuments( newDocsInChunk );
                }
                finally
                {
                    IntArrayListPool.Dispose( newDocsInChunk );
                }
            }
        }

        private void NotifyIndexLoaded()
        {
            if( IndexLoaded != null && !_notificationAlreadyDone &&
                _termsAccessor.LoadedRecords != 0 )
            {
                _notificationAlreadyDone = true;
                IndexLoaded( this, EventArgs.Empty );
            }
        }

        private void Cleanup()
        {
            OMEnv.DictionaryServer.FlushWordforms( OMEnv.WordformsFileName );
            _lastDocID = -1;
            _finishedDocsInBatch.Clear();
        }
        #endregion EndBatchUpdate and Chunk Construction

        #region IFullTextIndexer
        public bool  IsIndexPresent
        { 
            get { return _notificationAlreadyDone; }
        }

        public bool  IsDocumentPresent( int docID )
        {
            if( !IsIndexPresent) return false;

            IResource doc = Core.ResourceStore.TryLoadResource( docID );
            if( doc != null )
            {
                bool propValue = doc.HasProp( DocInIndexProp );
                _pendingLock.Enter();
                try
                {
                    if( propValue )
                    {
                        return !_pendingDeletions.Contains( doc.Id );
                    }
                    return _pendingAddends.Contains( doc.Id );
                }
                finally
                {
                    _pendingLock.Exit();
                }
            }
            return false;
        }

        public static bool IsDocumentPresent( int docID, TermIndexAccessor _accessor )
        {
            IResource doc = Core.ResourceStore.TryLoadResource( docID );
            return doc != null;
        }

        internal delegate void IntDelegate( int ind );

        public void  DeleteDocument( int docID )
        {
            #region Preconditions
            if( !IsIndexPresent )
                throw new ApplicationException( "Intermodule communication error - caller CAN NOT call this method when index is not present" );
            #endregion Preconditions

            _pendingLock.Enter();
            try
            {
                _pendingDeletions.Add( docID );
                _pendingAddends.Remove( docID );
            }
            finally
            {
                _pendingLock.Exit();
            }
            Core.ResourceAP.QueueJob( JobPriority.Immediate, "Marking document not present in text index", 
                                      new IntDelegate( NotInIndex ), docID );
        }

        private void NotInIndex( int id )
        {
            IResource doc = Core.ResourceStore.TryLoadResource( id );
            if( doc != null ) 
            {
                doc.DeleteProp( DocInIndexProp );
                _pendingLock.Enter();
                try
                {
                    _pendingDeletions.Remove( id );
                }
                finally
                {
                    _pendingLock.Exit();
                }
            }
        }

        public ArrayList  GetSimilarDocuments( int docID )
        {
            Debug.Assert( IsIndexPresent, "Intermodule communication error - caller CAN NOT call this method without opened text index" );
            return( null );
        }
        #endregion

        #region Query Processing
        internal const int _MaxQueryTokenLength = 255;

        public class QueryResult
        {
            public QueryResult()
            {
                IsSingularTerm = false;
                Result = null;
            }
            public Entry[]  Result;
            public bool     IsSingularTerm;
            public string   ErrorMessage;
        }

        public bool MatchQuery( string query, int resId, int dummy )
        {
            #region Preconditions
            if( resId != _lastDocID )
                throw new ArgumentException( "MatchQuery (FullTextIndexer) -- Input resource Id does not match internal data" );
            #endregion Preconditions

            bool result = false;
            if( isValidQuery( query ) )
            {
                QueryPostfixForm form = QueryParser.ParseQuery( query );
                if( form != null )
                {
                    result = MatchProcessor.MatchQuery( form, _tokens );
                }
            }
            return result;
        }

        public QueryResult  ProcessQuery( string query, int dummy )
        {
            #region Preconditions
            Debug.Assert( IsIndexPresent, "Intermodule communication error - caller CAN NOT call this method without opened text index" );
            #endregion Preconditions

            QueryResult  qResult = PerformInitialSearch( query );
            IntHashTable validEntries = CompressEntries( qResult.Result );
            FillResult( qResult, validEntries );

            Trace.WriteLineIf( !_suppTrace,  "--- Query [" + query + "]: " + validEntries.Count + " hits found" );

            return( qResult );
        }

        private QueryResult PerformInitialSearch( string query )
        {
            QueryResult qResult = new QueryResult();

            //  perform search only if input query string is of "reasonable" length
            if( isValidQuery( query ) )
            {
                QueryPostfixForm form = QueryParser.ParseQuery( query );
                if( form != null )
                {
                    qResult.IsSingularTerm = (form.TermNodesCount == 1);
                    qResult.Result = QueryProcessor.ProcessQuery( form, TermIndexAccessor );
                    if( QueryProcessor.Status == QueryProcessor.ErrorStatus.IllegalSectionName )
                    {
                        qResult.ErrorMessage = "Illegal document section name specified. Please consult help file for valid secion names.";
                    }
                }
                else
                {
                    qResult.ErrorMessage = QueryParser.Error;
                }
            }
            return qResult;
        }

        /// <summary>
        /// Ensure that there will be no duplicated IDs - this is possible
        /// when doc is removed from index and then inserted with the same ID.
        /// Overwriting usually helps :))
        /// </summary>
        private IntHashTable CompressEntries( IEnumerable<Entry> result )
        {
            IntHashTable validEntries = new IntHashTable();
            if( result != null )
            {
                foreach( Entry e in result ) // body's not optimal but compact
                {
                    if ( IsDocumentPresent( e.DocIndex ) )
                        validEntries[ e.DocIndex ] = e;
                }
            }
            return validEntries;
        }

        private static void FillResult( QueryResult qResult, IntHashTable validEntries )
        {
            qResult.Result = null;
            if( validEntries.Count > 0 )
            {
                int index = 0;
                qResult.Result = new Entry[ validEntries.Count ];
                foreach( IntHashTable.Entry e in validEntries )
                {
                    qResult.Result[ index++ ] = (Entry)e.Value;
                }
            }
        }

        /// <summary>
        /// This public method is designed for simplified processing and is used in tests.
        /// </summary>
        public Entry[]  ProcessQueryInternal( string query )
        {
            Entry[] resultEntries = null;

            if( isValidQuery( query ) )
            {
                QueryPostfixForm form = QueryParser.ParseQuery( query );
                resultEntries = QueryProcessor.ProcessQuery( form, TermIndexAccessor );

                if( resultEntries != null )
                {
                    ArrayList   list = new ArrayList();
                    foreach( Entry e in resultEntries )
                    {
                        if( IsDocumentPresent( e.DocIndex ))
                            list.Add( e );
                    }
                    resultEntries = (list.Count > 0) ? (Entry[])list.ToArray( typeof(Entry) ) : null;
                }
            }
            return( resultEntries );
        }

        private static bool isValidQuery( string query )
        {
            return (query.Length < _MaxQueryTokenLength) || (query.IndexOf( ' ' ) != -1);
        }
        #endregion Query Processing

        #region Accessors and Closers
        private static bool IsIndexPresentCheck()
        {
            return( File.Exists( OMEnv.TermIndexFileName ) );
        }

        public TermIndexAccessor TermIndexAccessor
        {
            get
            {
                if( _termsAccessor == null ) 
                {
                    _termsAccessor = new TermIndexAccessor( OMEnv.TermIndexFileName );

                    Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- Started loading Accessor [" + _termsAccessor.FileName + "]" );
                    _termsAccessor.Load();
                    Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- TermIndexAccessor loaded " + _termsAccessor.TermsNumber + " terms" );
                    Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- Accessors have been loaded" );
                }
                return _termsAccessor;
            }
        }

        //---------------------------------------------------------------------
        //  Delete files produced by defragmentation module.
        //---------------------------------------------------------------------
        private static void CleanPossibleUndeletedFiles()
        {
            try
            {
                if( File.Exists( OMEnv.TermIndexFileName + "_" ) )
                    File.Delete( OMEnv.TermIndexFileName + "_" );
            }
            catch( IOException e )
            {
                //  Generally, nothing to do.
                Trace.WriteLine( !_suppTrace,  "-- FullTextIndexer -- Failed to delete output files from defragmentation with reason: " + e.Message );
            }
        }

        public void  CloseIndices()
        {
            if( IsIndexPresentCheck() )
            {
                Flush();
                TermIndexAccessor.Close();
            }
        }

        public void  DiscardTextIndex()
        {
            Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- Discard Index is started." );
            Cleanup();
            DocVersionsToProcess.Clear();
            _finishedDocsInBatch.Clear();
            _termCounterInDoc.Clear();

            _tokens.Clear();

            if( _termsAccessor != null ) 
            {
                _termsAccessor.Discard();
            }
            _termsAccessor = null;

            CleanIndexTempFiles();
            DeleteFile( OMEnv.WordformsFileName );

            Core.ResourceAP.RunJob( "Marking all documents not present in text index",
                new ResourceListDelegate( ClearIndexedProperty ),
                Core.ResourceStore.FindResourcesWithProp( null, DocInIndexProp ) );
            _pendingLock.Enter();
            try
            {
                _pendingAddends.Clear();
                _pendingDeletions.Clear();
            }
            finally
            {
                _pendingLock.Exit();
            }

            //-------------------------------------------------------------
            if( IsIndexPresentCheck() )
            {
                Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer - Index components are not removed after index was discarded via Accessors" );
                throw new FormatException( "FullTextIndexer -- Index components are not removed after index was discarded via Accessors" );
            }
            Trace.WriteLineIf( !_suppTrace,  "-- FullTextIndexer -- Index has been discarded completely." );
        }

        private static void ClearIndexedProperty( IResourceList list )
        {
            AsyncProcessor ap = Core.ResourceAP as AsyncProcessor;
            foreach( IResource res in list.ValidResources )
            {
                if( ap != null && ap.OutstandingJobs > 0 )
                {
                    ap.DoJobs();
                }
                res.DeleteProp( DocInIndexProp );
            }
        }

        private static void DeleteFile( string fileName )
        {
            if( File.Exists( fileName ))    File.Delete( fileName );
        }

        public void  FlushIndices() {}

        #endregion Accessors and Closers

        #region Open methods for deep access
        public object  GetTermRecordMain( int HC )
        {
            return TermIndexAccessor.TermExist( HC ) ? TermIndexAccessor.GetRecordByHC( HC ) : null;
        }

        public object  GetTermRecordMem( int HC )
        {
            return null;
        }

        public void TraceIndexPerformanceCounters()
        {
            Trace.WriteLine( "Term index: loaded " + TermIndexAccessor.LoadedRecords + " records" );
            Trace.WriteLine( "Term index: saved " + TermIndexAccessor.SavedRecords + " records" );
        }
        #endregion Open methods for deep access

        #region Auxiliaries
        private static bool isValuableToken( Word word )
        {
            return isValuableToken( word.Token );
        }

        public static bool isValuableToken( string token )
        {
            return !OMEnv.DictionaryServer.isStopWord( token );
        }

        public void PropagateSearchableDocuments( IntArrayList newDocsInChunk )
        {
            if( NextUpdateFinished != null )
            {
                NextUpdateFinished( this, new DocsArrayArgs( newDocsInChunk.ToArray() ) );
            }
        }
        #endregion

        #region Attributes

        /// <summary>
        /// Event is raised:
        /// - when the complete text index is loaded into the accessor, or
        /// - in he case of index loading error and its reconstruction, after
        ///   the first chunk was successfully processed.
        /// </summary>
        public event EventHandler IndexLoaded;
        private bool _notificationAlreadyDone = false;

        public event EventHandler ResourceProcessed;

        ///<summary>
        ///  NextUpdateFinished is raised when [new] portion of documents is
        ///  converted to a index chunk, feasible for searching (it may be
        ///  main or incremental chunk).
        ///</summary>
        public event UpdateFinishedEventHandler NextUpdateFinished;

        //---------------------------------------------------------------------
        private const int    _ciMaxMeaningfulCount = 65000;
        private readonly int _cPreviewSize = 120;
        private const string _cJobName = "Marking document as present in text index";

        private TextDocParser       _textParser;
        private TermIndexAccessor   _termsAccessor = null;

        private readonly IntHashTableOfInt  _termCounterInDoc = new IntHashTableOfInt( 2000 );
        private readonly IntHashTableOfInt  DocVersionsToProcess = new IntHashTableOfInt();
        private readonly IntHashTableOfInt  _finishedDocsInBatch = new IntHashTableOfInt();
        private readonly HashMap            SectionsMapping = new HashMap();
        private ushort                      _termMaxFrequency = 0;
        private readonly IntHashTable       _tokens = new IntHashTable( 2000 );
        private readonly IntHashSet         _pendingAddends = new IntHashSet( 100 );
        private readonly IntHashSet         _pendingDeletions = new IntHashSet( 100 );
        private SpinWaitLock                _pendingLock = new SpinWaitLock();
        public static bool                  _suppTrace = false;

        private readonly StringBuilder  _previewFragment = new StringBuilder();
        private bool                    _mustConstructPreview;

        private int                 _lastDocID = -1;
        private uint                _prevSectionId;
        public static int           SimilarityPropId, SearchRankPropId, ProximityPropId,
                                    ContextPropId, ContextHighlightPropId, DocInIndexProp;
        private bool                _needDiscard;
        private static FullTextIndexer theIndexer;
        private int                 _lastCollectTick;

        #endregion Attributes
    }
}
