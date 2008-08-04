/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.TextIndex;

//-----------------------------------------------------------------------------
//  Internal classes for asynchronous execution of operations with
//  text index component
//-----------------------------------------------------------------------------

namespace JetBrains.Omea
{
    #region IndexingJob class
    internal class IndexingJob: AbstractNamedJob
    {
        protected int       _docID;
        protected IResource _res;

        internal  static int _documentsIndexed = 0;
        internal  static TextIndexManager _textIndexManager;

        internal IndexingJob( int docID ) : base()
        {
            #region Preconditions
            if( docID < 0 )
            {
                throw new ArgumentOutOfRangeException( "Indexing Job - document Id must be non-negative" );
            }
            #endregion Preconditions

            _docID = docID;
        }
        public override string  Name
        {
            get { return ( _res == null ) ? string.Empty : "Indexing " + _res.DisplayName;  }
        }
        public override int     GetHashCode()  {  return _docID;  }
        internal static void    ResetDocumentsIndexed() { _documentsIndexed = 0; }

        protected override void Execute() {}

        protected void UpdateIndexedDocsCounter()
        {
            _documentsIndexed++;
            int totalDocs = _textIndexManager.UprocessedJobsInQueue;
            string message = "Indexing documents (" + _documentsIndexed + "/" +
                            (totalDocs + _documentsIndexed).ToString() + ")";
            int percent = _documentsIndexed * 100 / (totalDocs + _documentsIndexed);
            _textIndexManager.UpdateProgress( percent, message, null );
        }
    }
    #endregion IndexingJob class

    #region DeleteDocUOW class
    internal class DeleteDocUOW: IndexingJob
    {
        internal DeleteDocUOW( int docID ) : base( docID ) {}

        public override bool    Equals( object obj )        
        {
            return(( obj is DeleteDocUOW ) && ( _docID == ((DeleteDocUOW)obj)._docID ));
        }

        public override string  Name
        {
            get { return "Removing resource " + _docID + " from text index";  }
        }

        protected override void Execute()
        {
            //-----------------------------------------------------------------
            //  Can delete only if the text index operates in normal mode
            //  (no previous error occured) or indexing was stopped manually,
            //  that is its structure is valid.
            //-----------------------------------------------------------------
            if( !_textIndexManager.IsIndexingSuspended ||
                 _textIndexManager.IsManuallySuspended )
            {
                try
                {
                    _textIndexManager.FullTextIndexer.DeleteDocument( _docID );
                }
                catch( System.FormatException ex )
                {
                    Core.ReportException( ex, ExceptionReportFlags.AttachLog );
                    //  DeleteDocUOW action is NOT an action which requires
                    //  text index removal, since e.g. search module uses its own
                    //  filterming methods.
//                  _textIndexManager.RebuildIndex();
                }
            }
        }
    }
    #endregion DeleteDocUOW class

    #region DefragmentIndexJob class
    internal class DefragmentIndexJob : AbstractNamedJob
    {
        public static TextIndexManager _textIndexManager;

        public override string Name
        {
            get { return "Defragmenting"; }
        }

        protected override void Execute()
        {
            if( !_textIndexManager.IsIndexingSuspended )
            {
                try
                {
                    Trace.WriteLineIf( !FullTextIndexer._suppTrace,  "TextIndexManager -- Defragmentation Job started" );
/*
                    _textIndexManager.FullTextIndexer.DefragmentIndexIdleMode();
                    _textIndexManager.DefragmentationWaitingEnded();
*/
                    Core.SettingStore.WriteDate( "Defragmentation", "LastDefragmentation", DateTime.Now );
                    Trace.WriteLineIf( !FullTextIndexer._suppTrace,  "TextIndexManager -- Defragmentation Job finished" );
                }
                catch( Exception ex )
                {
                    Core.ReportBackgroundException( ex );
                    _textIndexManager.RebuildIndex();
                }
            }
        }
    }
    #endregion DefragmentIndexJob class

    #region CalcContextUOW class
    internal class CalcContextUOW : AbstractJob
    {
        SimplePropertyProvider  PropProvider;
        Entry                   TermEntry;
        string[]                Lexemes;
        internal CalcContextUOW( SimplePropertyProvider provider, Entry e, string[] lexemes ) : base()
        {
            TermEntry = e;
            Lexemes = lexemes;
            PropProvider = provider;
        }
        private delegate void SetPropProviderDelegate( int resourceID, int propID, object propValue );
        protected override void Execute()
        {
            ArrayList  contextHighlightOffsets = null;
            string context = (Core.State != CoreState.ShuttingDown) ? ContextCtor.GetContext( TermEntry, Lexemes, out contextHighlightOffsets ) : ContextCtor.cFragmentsDelimiter;
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new SetPropProviderDelegate(PropProvider.SetProp),
                                      TermEntry.DocIndex, FullTextIndexer.ContextPropId, context );
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new SetPropProviderDelegate(PropProvider.SetProp),
                                      TermEntry.DocIndex, FullTextIndexer.ContextHighlightPropId, contextHighlightOffsets );
        }
    }
    #endregion CalcContextUOW class
}