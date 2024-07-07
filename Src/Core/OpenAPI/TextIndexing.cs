// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;

//-----------------------------------------------------------------------------
//  Text processing API block
//-----------------------------------------------------------------------------
namespace JetBrains.Omea.OpenAPI
{
    /// <since>2.3</since>
    public enum  EntryProximity { Phrase = 1, Sentence = 2, Document = 3  }

    /// <summary>
    /// DocumentSectionResource describes names of resource types and properties,
    /// necessary for referencing of their resources in other components (like
    /// AdvancedSearchForm).
    /// </summary>
    /// <since>2.1.3</since>
    public class DocumentSectionResource
    {
        public const string DocSectionResName = "DocumentSection";
        public const string SectionHelpDescription = "SectionHelpDescription";
    }

    /// <summary>
    /// DocumentSection describes possible standard sections of a document. Query
    /// processing facilities later then restrict the search to any of these
    /// particular sections.
    /// </summary>
    public class DocumentSection
    {
        /// <summary>
        /// Corresponds to the whole body of the document, search will not be restricted at all.
        /// </summary>
        public const string  BodySection = "All Sections";

        /// <summary>
        /// Corresponds to the title/subject of the email, heading of the news article.
        /// </summary>
        public const string  SubjectSection = "Subject/Heading";

        /// <summary>
        /// Corresponds to the annotation of the resource.
        /// </summary>
        public const string  AnnotationSection = "Annotation";

        /// <summary>
        /// Corresponds to the textual representation of authors of the email/article, rss feed.
        /// </summary>
        public const string  SourceSection = "Source/From";

        /// <summary>
        /// Corresponds to the whole body of the document minus Subject/Title. This auxiliary
        /// section is used neither in text indexing nor in UI for restricting the search. Its
        /// auxiliary purpose is to exclude subject/heading offsets from the given set.
        /// </summary>
        public const string  NonSubjectSections = "NonSubject";

        /// <summary>
		/// restricts the search result (list of search matches) to a particular section.
		/// </summary>
		/// <param name="allResults">Search results to be restricted. May be <c>null</c>.</param>
		/// <param name="desiredSection">Section to which the results should be restricted.</param>
		/// <returns>The restricted list of the search results, may be <c>null</c>.</returns>
		/// <since>2.0</since>
		public static WordPtr[] RestrictResults( WordPtr[] allResults, string desiredSection )
		{
			if( allResults == null )
				return null;

			// Count
			int	fit = 0;
			foreach( WordPtr word in allResults )
			{
                if( isProperSection( word, desiredSection ))
					fit++;
			}

            if( fit == 0 )
                return null;

			// Extract
			WordPtr[] result = new WordPtr[ fit ];
			int	index = 0;
			foreach( WordPtr word in allResults )
			{
				if( isProperSection( word, desiredSection ) )
				{
					Debug.Assert(index < fit);
					result[ index++ ] = word;
				}
			}

			return result;
		}

        private static bool isProperSection( WordPtr word, string section )
        {
            return  word.Section == section ||
                   (section == NonSubjectSections && word.Section != SubjectSection );
        }
    }

    /// <summary>
    /// Specifies possible purposes of requesting the text of a resource through
    /// <see cref="IResourceTextConsumer"/>.
    /// </summary>
    public enum TextRequestPurpose
    {
        /// <summary>
        /// The text is requested for indexing. The complete text of the document should be returned.
        /// </summary>
        Indexing,

        /// <summary>
        /// The text is requested for showing the context of a search result. If extracting the text
        /// takes a long time, the extraction should not be performed, and
        /// <see cref="IResourceTextConsumer.RejectResult"/> should be called to reject the results
        /// from other text providers.
        /// </summary>
        ContextExtraction
    };

	#region Struct WordPtr — A structure that represents an individual search results entry.

	/// <summary>
	/// A structure that represents an individual search results entry.
	/// </summary>
	public struct WordPtr
	{
		/// <summary>
		/// Offset of this word from the beginning of the plain-text document representation that was supplied to the <see cref="IResourceTextConsumer"/>.
		/// </summary>
		public int StartOffset;

		/// <summary>
		/// Document section in which this search result resides, for example, title, body, etc.
		/// </summary>
		public string Section;

		/// <summary>
		/// Document section Id in which this search result resides.
		/// </summary>
		public int SectionId;

		/// <summary>
		/// The search result as it occurs in the plain-text document representation.
		/// </summary>
		public string Text;

		/// <summary>
		/// Original wordform that was present in the query. Different textual
		/// representations of the same query token has this attribute equal.
		/// </summary>
		public string Original;

        public static WordPtr[] Empty = new WordPtr[ 0 ];
		#region Error Checks

		/// <summary><seealso cref="AssertValid(WordPtr[], bool)"/>
		/// Performs a runtime check on the WordPtr contents to ensure that the structure is valid.
		/// If not, throws an exception that explains what is wrong.
		/// </summary>
		/// <since>2.0</since>
		public void AssertValid()
		{
			if( (StartOffset < 0) || (StartOffset == int.MaxValue) )
				throw new WordPtrException( "The StartOffset field of a WordPtr must be a non-negative finite value." );
			if( (Section == null) || (Section.Length <= 0) )
				throw new WordPtrException( "The Secion field of a WordPtr must be defined. See DocumentSection structure for the available values." );
			// TODO: check the SectionID
			if( (Text == null) || (Text.Length <= 0) )
				throw new WordPtrException( "The Text field of a WordPtr must be defined." );
			if( (Original == null) || (Original.Length <= 0) )
				throw new WordPtrException( "The Original field of a WordPtr must be defined." );
		}

		/// <summary><seealso cref="AssertValid()"/>
		/// Checks whether an array of WordPtrs is valid.
		/// A <c>Null</c> value is assumed to be valid by default.
		/// See the <see cref="AssertValid()"/> function for details.
		/// </summary>
		/// <param name="words">An array of words to be checked for validness.</param>
		/// <param name="inOneSection">If <c>True</c>, then all the words in the array must belong to the same document section.
		/// If <c>False</c>, no cross-word checks for the <see cref="WordPtr.Section"/> value are performed.</param>
		/// <since>2.0</since>
		public static void AssertValid( WordPtr[] words, bool inOneSection )
		{
			if( words == null )
				return; // A valid case

			// Check the individual WordPtrs, and also collect the section information
			string sSection = null;
			bool bSectionDiffers = !inOneSection; // Don't even compare the strings if the check is not required (raise the flag initially)
			foreach( WordPtr word in words )
			{
				word.AssertValid();
				bSectionDiffers = (bSectionDiffers) || ((sSection != null) && (sSection != word.Section)); // If a section has been assigned and now differs, then raise the error flag; never lower it
				sSection = word.Section;
			}

			// Issue the section error, if needed
			if( (bSectionDiffers) && (inOneSection) )
				throw new WordPtrException( "All the WordPtrs in the array must belong to the same section." );
		}

		/// <summary>
		/// An exception that is thrown by this class.
		/// </summary>
		/// <since>2.0</since>
		public class WordPtrException : Exception
		{
			public WordPtrException( string errortext )
				: base( errortext )
			{
			}
		}

		#endregion
	}
	#endregion

	/// <summary>
    /// Interface describes the core text-indexing engine which consumes the text
    /// fragments, tokenizes them and constructs index chunks.
    /// </summary>
    /// <remark>Fragments for a single document must be submitted consequently,
    /// otherwise engine will decide that a new version of a document is queued for
    /// indexing.</remark>
    public interface IResourceTextConsumer
    {
        /// <summary>
        /// Submit a header/subject/title fragment of a resource.
        /// </summary>
        /// <param name="resourceId">A resource Id from which the fragment is taken.</param>
        /// <param name="text">Fragment text.</param>
        void    AddDocumentHeading( int resourceId, string text );

        /// <summary>
        /// Submit a fragment of a resource.
        /// </summary>
        /// <param name="resourceId">A resource Id from which the fragment is taken.</param>
        /// <param name="text">Fragment text.</param>
        void    AddDocumentFragment( int resourceId, string text );

        /// <summary>
        /// Submit a fragment of a resource from a particular named section.
        /// </summary>
        /// <param name="resourceId">A resource Id from which the fragment is taken.</param>
        /// <param name="text">Fragment text.</param>
        /// <param name="sectionName">Name of a section from which the fragment is taken.</param>
        void    AddDocumentFragment( int resourceId, string text, string sectionName );

        /// <summary>
        /// Method specifies the amount to be added to the starting offset
        /// of the next fragment.
        /// </summary>
        /// <param name="spacesAmount">Number by which the starting offset must be increased.
        /// Must be positive.</param>
        /// <since>2.0</since>
        void  IncrementOffset( int spacesAmount );

        /// <summary>
        /// Start counting the token offset from 0 for all subsequent fragments of
        /// the same document.
        /// </summary>
        void    RestartOffsetCounting();

        /// <summary>
        /// Do not account the fragments collected so far for the currently processed
        /// document if not all fragments can be submitted for some particular reason.
        /// </summary>
        void    RejectResult();

        /// <summary>
        /// Get the purpose of the current IResourceTextConsumer instance -
        /// index construction or context construction.
        /// </summary>
        TextRequestPurpose Purpose { get; }
    }

    /// <summary>
    /// Describes the offset of a highlighed section in a search result context.
    /// </summary>
    /// <since>2.0</since>
    public class  OffsetData
    {
        /// <summary>
        /// Creates an offset data with the specified start and length.
        /// </summary>
        /// <param name="start">The start of a highlighted section.</param>
        /// <param name="len">The length of a highlighted section.</param>
        public OffsetData( int start, int len )
        {
            Start = start;
            Length = len;
        }

        /// <summary>
        /// The start of a highlighted section.
        /// </summary>
        public int  Start;

        /// <summary>
        /// The length of a highlighted section.
        /// </summary>
        public int  Length;
    }

    /// <summary>
    /// Allows to receive search highlight and context data for specific resources.
    /// </summary>
    public interface IHighlightDataProvider
    {
        /// <summary>
        /// Returns the search result highlighting data for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the highlighting data is retrieved.</param>
        /// <param name="words">The returned array of search result records.</param>
        /// <returns>true if highlighting data for the specified resource was found, false otherwise.</returns>
        /// <remarks>An implementation of this interface for a specific search is returned
        /// by <see cref="ITextIndexManager.ProcessQuery(string, int[], out IHighlightDataProvider, out string[], out string)"/>.</remarks>
        bool GetHighlightData( IResource res, out WordPtr[] words );

        /// <summary>
        /// Requests asynchronous context retrieval for the specified list of resource IDs.
        /// </summary>
        /// <param name="resourceIDs">The list of resource IDs for which contexts are requested.</param>
        /// <remarks>The contexts are stored in a virtual property "Context" which is managed
        /// by the <see cref="IPropertyProvider">property provider</see> attached to the search
        /// result resource list.</remarks>
        void RequestContexts( int[] resourceIDs );

        /// <summary>
        /// Retrieves the context for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the context is retrieved.</param>
        /// <returns>The context, or null if the context is not available.</returns>
        /// <since>2.0</since>
        string GetContext( IResource res );

        /// <summary>
        /// Return an array of highlighted tokens in the context string in the format:
        /// { offset in context, highlight length }.
        /// </summary>
        /// <param name="res">The resource for which the information is retrieved.</param>
        /// <returns>Array of highlight data pairs.</returns>
        /// <since>2.0</since>
        OffsetData[] GetContextHighlightData( IResource res );
    }

    /// <summary>
    /// Describes the array of documents which are available for searching.
    /// </summary>
    public class DocsArrayArgs : EventArgs
    {
        public DocsArrayArgs( int[] docs )
        {
            DocsArray = new int[ (docs == null) ? 0 : docs.Length ];
            if( docs != null )
                Array.Copy( docs, DocsArray, docs.Length );
        }

        /// <summary>
        /// Returns the array of IDs of resources which are available for searching.
        /// </summary>
        /// <returns>The array of document IDs.</returns>
        public int[] GetDocuments()
        {
            return DocsArray;
        }

        private  int[]  DocsArray;
    }

    /// <summary>
    /// Callback defines an event when a number of documents becomes available
    /// for searching.
    /// </summary>
    public delegate void UpdateFinishedEventHandler( object sender, DocsArrayArgs docIds );

    /// <summary>
    /// Interface controls the submission of the documents to the text-index
    /// processing - manage the queue of text-indexing jobs, handle the events
    /// on different text index states, handle exceptional situations in the
    /// text index structure (text index corruption).
    /// </summary>
    public interface ITextIndexManager
    {
        /// <summary>
        /// Queue a resource for text-indexing.
        /// </summary>
        /// <param name="resourceId">Id of a resource.</param>
        void QueryIndexing( int resourceId );

        /// <summary>
        /// Queue a deletion of a resource from the text index.
        /// </summary>
        /// <param name="resourceId">Id of a resource.</param>
        void DeleteDocumentQueued( int resourceId );

        /// <summary>
        /// Delete current text index, build a new one from scratch. Usually this
        /// method is used when some exceptional situation is met which causes
        /// text index corruption.
        /// </summary>
        void RebuildIndex();

        /// <summary>
        /// Determines whether text index files are present.
        /// </summary>
        /// <returns>True if valid text index is present.</returns>
        bool IsIndexPresent();

        /// <summary>
        /// Determines whether a particular document is indexed.
        /// </summary>
        /// <param name="resourceId">Id of a resource.</param>
        /// <returns>True if the document text was indexed.</returns>
        bool IsDocumentInIndex( int resourceId );

        /// <summary>
        /// Registers a callback which is called when new batch of documents has
        /// been indexed and is available for searching.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        void SetUpdateResultHandler( UpdateFinishedEventHandler callback );

        /// <summary>
        /// Return a list of resources, textual representation of which mathces
        /// the query.
        /// </summary>
        /// <param name="searchQuery">Query string.</param>
        /// <returns>List of resources matching the query.</returns>
        IResourceList   ProcessQuery( string searchQuery );

        /// <summary>
        /// Return a list of resources, textual representation of which mathces
        /// the query. Additionally specify whether to start the process of
        /// contexts extraction (extraction is done asynchronously).
        /// </summary>
        /// <param name="searchQuery">Query string.</param>
        /// <param name="restrictByIds">List of resource Ids within which the search is to be performed.</param>
        /// <param name="highlightDataProvider">Auxiliary structure to be passed
        /// to the ResourceBrowser.</param>
        /// <param name="stopList">List of stopwords found during parsing the query.</param>
        /// <param name="parsingErrorMsg">Contains message describing the error occured during parsing the query.</param>
        /// <returns>List of resources matching the query.</returns>
        /// <since>2.0</since>
        IResourceList   ProcessQuery( string searchQuery, int[] restrictByIds,
                                      out IHighlightDataProvider highlightDataProvider,
                                      out string[] stopList, out string parsingErrorMsg );

        /// <summary>
        /// Match a resource over the query using <c>TextQueriesOptimizationManager"</c>
        /// class which interacts with FilterRegistry and FullTextIndexer on per-document
        /// basis when they are to appear in the text index.
        /// </summary>
        /// <since>2.3 (2.5) (Grenache)</since>
        bool MatchQuery( string query, IResource res );

        /// <summary>Get or set whether text indexing operations are carried out during
        /// periods when the computer is in the idle mode.</summary>
        bool  IdleIndexingMode { get; set; }

        /// <summary>
        /// Event is fired when text index construction is complete, that is when
        /// there has been constructed at least one chunk of index over which the
        /// search is possible.
        /// </summary>
        event EventHandler IndexLoaded;

        //---------------------------------------------------------------------
        //  Search providers management
        //---------------------------------------------------------------------
        void    RegisterSearchProvider( ISearchProvider host, string title );
        void    RegisterSearchProvider( ISearchProvider host, string title, string groupName );
        void    UnregisterSearchProvider( ISearchProvider host );
        string  GetSearchProviderTitle  ( ISearchProvider host );
        ISearchProvider  CurrentSearchProvider { get; set; }
        ISearchProvider[]  GetSearchProviders();
        string[] GetSearchProviderGroups();
        ISearchProvider[] GetSearchProvidersInGroup( string group );
    }

    /// <summary>
    /// Allows to register specially formatted phrases, which being added to the
    /// end of the search query allow (after parsing) to restrict the search
    /// result without explicit usage of Advanced Search capabilities.
    /// </summary>
    /// <since>2.2</since>
    public interface ISearchQueryExtensions
    {
        /// <summary>
        /// Register phrase "anchor displayType" which (after parsing) restricts
        /// the search result to resources of "resType" type.
        /// Example: RegisterResourceTypeRestriction( "in", "news", "Article" )
        ///          "... in news" - restricts search result to news articles.
        /// </summary>
        void  RegisterResourceTypeRestriction( string anchor, string displayType, string resType );

        /// <summary>
        /// Register phrase "anchor token" which (after parsing) restricts
        /// the search result to resources conforming to "stdCondition" condition.
        /// Example: RegisterSingleTokenRestriction( "in", "unread", conditionResource )
        ///          "... in unread" - restricts search result to those which are
        ///          not read yet.
        /// </summary>
        void  RegisterSingleTokenRestriction ( string anchor, string token, IResource stdCondition );

        /// <summary>
        /// Register phrase "anchor text" which gives this text for parsing to the
        /// IQueryTokenMatcher object. If IQueryTokenMatcher manages to parse the
        /// "text" (that is to extract proper parameters to some ConditionTemplate)
        /// then it produces an instance of that ConditionTemplate as the
        /// instantiated Condition (proxy condition).
        /// Example: RegisterFreestyleRestriction( "from", fromMatcher )
        ///          ".. from Greg" - restrict search result to those which came
        ///          from a person with "Greg" as a first or last name.
        /// </summary>
        void  RegisterFreestyleRestriction ( string anchor, IQueryTokenMatcher matcher );

        /// <summary>
        /// Get a resource type name registered for given anchor and a token
        /// from the query.
        /// </summary>
        /// <returns>Resource type name if such is registered for given anchor and token,
        /// NULL if no such combination is registered</returns>
        string  GetResourceTypeRestriction( string anchor, string token );

        /// <summary>
        /// Get a condition resource registered for given anchor and a token
        /// from the query.
        /// </summary>
        /// <returns>A condition if such is registered for given anchor and token,
        /// NULL if no such combination is registered.</returns>
        IResource  GetSingleTokenRestriction( string anchor, string token );

        /// <summary>
        /// Get a generated condition resource (proxy condition as a result of
        /// condition template instantiation) registered for given anchor and
        /// a parseable text from the query.
        /// </summary>
        /// <returns>A condition if such is registered for given anchor and
        /// text is parsable into template parameters, NULL if no such combination
        /// is found.</returns>
        IResource GetMatchingFreestyleRestriction( string anchor, string text );

        /// <summary>
        /// Retrieve all registered anchors.
        /// </summary>
        string[] GetAllAnchors();
    }

    /// <summary>
    /// Interface for handlers of parts of the search query string starting after the
    /// registered anchor. Handler is responsible for matching of the text with possible
    /// parameters of a handler-defined condition template.
    /// </summary>
    /// <since>2.2</since>
    public interface IQueryTokenMatcher
    {
        /// <summary>
        /// Parse token stream, produce parameters for a [particular] condition template,
        /// and instantiate this template for producing a proxy condition which then
        /// will be used to restrict the search result set.
        /// </summary>
        IResource  ParseTokenStream( string tokens );
    }

    public interface ISearchProvider
    {
        string  Title { get; }
        Icon    Icon  { get; }
        void    ProcessQuery( string query );
    }
}
