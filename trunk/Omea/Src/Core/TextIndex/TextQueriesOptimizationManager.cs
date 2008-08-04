/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Containers;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.TextIndex
{
    public class TextQueriesOptimizationManager
    {
        private readonly AsyncProcessor _textProcessor;
        private readonly FullTextIndexer _textIndexer;

        private readonly Dictionary<string,IntArrayList>  _queryResults;
        private readonly List<string>                     _activeQueries;

        private IResourceList  _allTextConditions;
        private int            _dummyCounter;

        #region Ctor and Intialization
        public TextQueriesOptimizationManager( AsyncProcessor processor, FullTextIndexer indexer )
        {
            _textProcessor = processor;
            _textIndexer = indexer;
            _queryResults = new Dictionary<string,IntArrayList>();
            _activeQueries = new List<string>();

            InitializeConditionsList();
            InitializeListeners();
        }

        private void InitializeConditionsList()
        {
            _allTextConditions = CollectAllTextConditions();
            ReloadQueryResults();

            _allTextConditions.ResourceAdded += QuerySetChanged;
            _allTextConditions.ResourceDeleting += QuerySetChanged;
            _allTextConditions.ResourceChanged += QueryResourceChanged;
        }

        private void InitializeListeners()
        {
            _textIndexer.ResourceProcessed += CheckNewResourceOverQueries;
        }
        #endregion Ctor and Intialization

        #region Querying and matching resources

        internal delegate FullTextIndexer.QueryResult  QueryRequest( string str, int dummy );
        internal delegate bool MatchRequest( string str, int id, int dummy );

        public FullTextIndexer.QueryResult QueryList( string query )
        {
            FullTextIndexer.QueryResult qr;
            ThreadPriority priority = _textProcessor.ThreadPriority;
            _textProcessor.ThreadPriority = ThreadPriority.Normal;
            try
            {
                qr = (FullTextIndexer.QueryResult) _textProcessor.RunJob( "Searching for " + query, 
                    new QueryRequest( _textIndexer.ProcessQuery ), query, Interlocked.Increment( ref _dummyCounter ) );
            }
            finally
            {
                _textProcessor.ThreadPriority = priority;
            }
            return qr;
        }

        public bool MatchResource( IResource res, string query )
        {
            #region Preconditions - Check that query has been processed earlier
            if( !_activeQueries.Contains( query ) )
                throw new ApplicationException( "TextQueriesOptimizationManager -- Outside query [" + query + "] does not have a counterpart internally");
            #endregion Preconditions

            IntArrayList list;
            lock( _queryResults )
            {
                if( _queryResults.TryGetValue( query, out list ) )
                {
                    return (list.IndexOf( res.Id ) != -1);
                }
            }

            return false;
        }
        #endregion Querying and matching resources

        #region Checking resource on readiness
        private void CheckNewResourceOverQueries( object sender, EventArgs e )
        {
            #region Preconditions - Check that method is called in TextIndex thread
            if( !_textProcessor.IsOwnerThread )
                throw new ApplicationException( "TextQueryOptimizationManager -- [CheckNewResourceOverQueries] can be executed only in the TextIndex thread.");
            #endregion Preconditions

            int resId = (int)sender;
            foreach( string query in _activeQueries )
            {
                bool matched = _textIndexer.MatchQuery( query, resId, Interlocked.Increment( ref _dummyCounter ) );
                if( matched )
                {
                    AddMatchedResourceToCache( resId, query );
                }
            }
        }

        private void AddMatchedResourceToCache( int Id, string query )
        {
            IntArrayList list;
            if( !_queryResults.TryGetValue( query, out list ) )
            {
                list = new IntArrayList();
                _queryResults.Add( query, list );
            }
            list.Add( Id );
        }
        #endregion Impl

        #region Collecting and Monitoring Query Condition Resources

        private static IResourceList CollectAllTextConditions()
        {
            // NOTE: Do not change to int value since FilterRegistry might not been initialized to that time.
            string condProp = "ConditionOp" /*FilterRegistry.OpProp*/;

            //  Get ALL conditions with text queries and remove those which are
            //  "hanged" due to the bug of Search Views cleanup.
            IResourceList allQuery = Core.ResourceStore.FindResourcesLive( FilterManagerProps.ConditionResName, condProp, (int)ConditionOp.QueryMatch );
            IResourceList allActive = Core.ResourceStore.FindResourcesWithPropLive( FilterManagerProps.ConditionResName, "LinkedCondition" );
            IResourceList result = allQuery.Intersect( allActive );

            return result;
        }

        private void QueryResourceChanged(object sender, ResourcePropIndexEventArgs e)
        {
            ReloadQueryResults();
        }

        private void QuerySetChanged(object sender, ResourceIndexEventArgs e)
        {
            ReloadQueryResults();
        }

        /// <summary>
        /// Do not pay attention to the queries which could have been deleted - their
        /// memory consumtion is too small to be accounted for.
        /// </summary>
        private void ReloadQueryResults()
        {
            foreach( IResource res in _allTextConditions )
            {
                string query = FilterRegistry.ConstructQuery( res );
                if( !_activeQueries.Contains( query ) )
                {
                    _activeQueries.Add( query );
                }
            }
        }
        #endregion Collecting and Monitoring Query Condition Resources
    }
}
