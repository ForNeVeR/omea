/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using CommonTests;
using JetBrains.Omea.TextIndex;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Diagnostics;

namespace TextIndexTests
{
    [TestFixture]
    public class QueryAndContextsTest: MyPalDBTests
    {
        public const int ThreadsNumber = 2;
        private AsyncProcessor[]    _processors = new AsyncProcessor[ ThreadsNumber ];
        private FullTextIndexer     indexer;

        #region Startup
        private static void ExceptionHandler( Exception e )
        {
            if( e is ResourceDeletedException )
                return;
            Exception innerException = e.InnerException;
            if ( innerException != null )
                ExceptionHandler( innerException );
            else
            {
                Tracer._TraceException( e );
                Console.WriteLine( e.Message + e.StackTrace );
            }
        }

        [SetUp] public void SetUp()
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles(".", "_*");
                foreach ( string fileName in files )
                {
                    System.IO.File.Delete( fileName );
                }
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.Message );
            }

            OMEnv.DataDir = ".\\Data";
            InitStorage();
            for( int i = 0; i < _processors.Length; ++i )
            {
                _processors[ i ] = new AsyncProcessor( new AsyncExceptionHandler( ExceptionHandler ), false );
            }

            MockPluginEnvironment env = new MockPluginEnvironment( _storage );
            env.SetCoreProps( new MockCoreProps() );
            
            indexer = new FullTextIndexer();
            indexer.Initialize();
            Core.ResourceStore.ResourceTypes.Register( "TestType", "TestType", "Name", ResourceTypeFlags.Normal );
        }

        [TearDown] public void TearDown()
        {
            CloseStorage();
            indexer.CloseIndices();
            indexer.DiscardTextIndex();
            Word.DisposeTermTrie();
            try
            {
                string[] files = System.IO.Directory.GetFiles(".", "_*");
                foreach ( string fileName in files )
                {
                    System.IO.File.Delete( fileName );
                }
            }
            catch ( Exception exc )
            {
                Console.WriteLine( exc.Message );
                Assert.Fail( exc.Message );
            }
        }
        #endregion Startup

        #region Tokenizing And QueryProcessing
        [Test] public void TestDotDelimiters()
        {
            IResource newRes = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes.Id, "token1.token2. token3 token4.token5. " );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "token1" );
            Console.WriteLine( result[ 0 ].Offsets[ 0 ].Sentence );
            AssertIfTrue( "We must find one token from the instance of the document", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "token2" );
            Console.WriteLine( result[ 0 ].Offsets[ 0 ].Sentence );
            AssertIfTrue( "We must find one token from the instance of the document", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "token3" );
            Console.WriteLine( result[ 0 ].Offsets[ 0 ].Sentence );
            AssertIfTrue( "We must find one token from the instance of the document", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "token4" );
            Console.WriteLine( result[ 0 ].Offsets[ 0 ].Sentence );
            AssertIfTrue( "We must find one token from the instance of the document", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "token5" );
            Console.WriteLine( result[ 0 ].Offsets[ 0 ].Sentence );
            AssertIfTrue( "We must find one token from the instance of the document", result.Length == 1 );
        }

        private static void AssertIfTrue( string message, bool condition )
        {
            Assert.IsTrue( condition, message );
        }

        [Test] public void TestQueryParser()
        {
//            LexemeConstructor Constructor = new LexemeConstructor( OMEnv.ScriptMorphoAnalyzer, OMEnv.DictionaryServer );
//            QueryParser parser = new QueryParser( Constructor );
            QueryPostfixForm result = QueryParser.ParseQuery( "(x or \"y z\") [SU]" );
            AssertIfTrue( "Query is parsed successfully.", result != null );
        }

        [Test] public void TestDifferentProximities()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            IResource newRes2 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "token1 token2. token3 tokeninside token4. " );
            indexer.AddDocumentFragment( newRes2.Id, "token3 token4 token5. And All of that finish will ever complain." );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "token3 near token4" );
            AssertIfTrue( "Amount of documents with [near] predicate does not correspond to expected", result.Length == 2 );

            Console.WriteLine( "first - " + result[ 0 ].Proximity );
            Console.WriteLine( "second - " + result[ 1 ].Proximity );
            AssertIfTrue( "Unexpected value of proximity for the first entry", result[ 1 ].Proximity == EntryProximity.Sentence );
            AssertIfTrue( "Unexpected value of proximity for the second entry", result[ 0 ].Proximity == EntryProximity.Phrase );
        }

        [Test] public void TestPhraseDelimitedWithManyBlanks()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "token1                                           token2. " );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "\"token1 token2\"" );
            AssertIfTrue( "Amount of documents with [phrase] predicate does not correspond to expected", result.Length == 1 );
        }

        [Test] public void TestSectionReductionOfPhrase()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentHeading( newRes1.Id, "token1 token2 token3 token4. " );
            indexer.AddDocumentFragment( newRes1.Id, "token5 token4 token3 token2. And All of that finish will ever complain." );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "(\"token2 token3\") [SU]" );
            AssertIfTrue( "Failed to find", result.Length == 1 );
        }

        [Test] public void TestinvalidNearProximity()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            // no sentence end mark !!!
            indexer.AddDocumentFragment( newRes1.Id, "token1 token2 token3 tokeninside token4 " );
            indexer.AddDocumentHeading ( newRes1.Id, "token5 token6 token7 token8" );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "token1 near token6" );
            AssertIfTrue( "Amount of documents with [near] predicate does not correspond to expected", result == null );
        }

        [Test] public void DifferentSentenceNumbersForDoubleNLDelimiters()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            IResource newRes2 = Core.ResourceStore.NewResource( "TestType" );
            IResource newRes3 = Core.ResourceStore.NewResource( "TestType" );
            IResource newRes4 = Core.ResourceStore.NewResource( "TestType" );
            IResource newRes5 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "one two\n\nthree fourplay" );
            indexer.AddDocumentFragment( newRes2.Id, "fourplay.\n\nthreeplay. " );
            indexer.AddDocumentFragment( newRes3.Id, "Word1 Word2.\n\n\nWord3. " );
            indexer.AddDocumentFragment( newRes4.Id, "Word1 Word2\n\n\nWord3. " );
            indexer.AddDocumentFragment( newRes5.Id, "Word4 Word5\nWord6. " );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "one near three" );
            AssertIfTrue( "We must not find any document for the specified query(1)", (result == null) || (result.Length == 0) );

            result = indexer.ProcessQueryInternal( "fourplay near threeplay" );
            AssertIfTrue( "We must not find any document for the specified query(2)", (result == null) || (result.Length == 0) );

            result = indexer.ProcessQueryInternal( "Word1 near Word3" );
            AssertIfTrue( "We must not find any document for the specified query(2)", (result == null) || (result.Length == 0) );
        }

        [Test] public void DelimitableTokensParsingAndQuering()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "one1 two1.three1 four1&five1&&six1" );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "one1 two1" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
            result = indexer.ProcessQueryInternal( "one1 near three1" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
            result = indexer.ProcessQueryInternal( "one1 near four1" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
            result = indexer.ProcessQueryInternal( "one1 near five1" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "two1.three1" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
            result = indexer.ProcessQueryInternal( "five1.six1" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
            result = indexer.ProcessQueryInternal( "one1 five1&six1" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
            result = indexer.ProcessQueryInternal( "\"one1 two1.three1\"" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
        }

        //---------------------------------------------------------------------
        //  Process: add two fragments of text, first one ends with sentence end
        //           sign.
        //  Expected: fourth token must have sentence number == 2.
        //---------------------------------------------------------------------
        [Test] public void DelimiterAsLastSymbolBeforeEOL()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "one1 two2 three3." );
            indexer.AddDocumentFragment( newRes1.Id, "Four4." );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "one1" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
            AssertIfTrue( "Sentence Number is expected to be == 0", result[ 0 ].Offsets[ 0 ].Sentence == 0 );

            result = indexer.ProcessQueryInternal( "four4" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
            AssertIfTrue( "Sentence Number is expected to be == 1", result[ 0 ].Offsets[ 0 ].Sentence == 1 );
        }

        [Test] public void  QueryHeading()
        {
            RegisterTypes();
            _storage.PropTypes.Register( "SectionHelpDescription", PropDataType.String );
            _storage.PropTypes.Register( "SectionShortName", PropDataType.String );
            _storage.PropTypes.Register( "SectionOrder", PropDataType.Int );
            _storage.ResourceTypes.Register( DocumentSectionResource.DocSectionResName, "", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            RegisterDocumentSection( DocumentSection.BodySection, "Full content of the resource", null );
            RegisterDocumentSection( DocumentSection.SubjectSection, "Describes subject (or heading, title) of the e-mail, article, etc", "SU" );
            RegisterDocumentSection( DocumentSection.AnnotationSection, "a note added by way of comment or explanation", "AN" );

            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            IResource newRes2 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentHeading( newRes1.Id, "one two three fourplay" );
            indexer.AddDocumentFragment( newRes1.Id, "fourplay threeplay" );
            indexer.AddDocumentFragment( newRes2.Id, "fourplay threeplay" );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "fourplay [SU]" );
            AssertIfTrue( "Failed to find single document", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "threeplay" );
            AssertIfTrue( "Failed to find two documents", result.Length == 2 );

            result = indexer.ProcessQueryInternal( "threeplay fourplay [SU]" );
            AssertIfTrue( "Failed to find single document", result.Length == 1 );
        }

        //  Test phrasal search - only those token instances must occure in
        //  the output which correspond the phrase only.
        [Test] public void  PhrasalSearchRestrictsOffsets()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "token1 token2 token3 token4 token5." );
            indexer.AddDocumentFragment( newRes1.Id, "blank token1 blank token2 blank token3 blank token4 blank token5." );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "\"token1 token2 token3 token4 token5\"" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
            AssertIfTrue( "Query must be successful", result[ 0 ].Offsets.Length == 5 );
            AssertIfTrue( "Check an offset of token 0", result[ 0 ].Offsets[ 0 ].OffsetNormal == 0 );
            AssertIfTrue( "Check an offset of token 1", result[ 0 ].Offsets[ 1 ].OffsetNormal == 7 );
            AssertIfTrue( "Check an offset of token 2", result[ 0 ].Offsets[ 2 ].OffsetNormal == 14 );
            AssertIfTrue( "Check an offset of token 3", result[ 0 ].Offsets[ 3 ].OffsetNormal == 21 );
            AssertIfTrue( "Check an offset of token 4", result[ 0 ].Offsets[ 4 ].OffsetNormal == 28 );
        }

        //  Test phrasal search - only those token instances must occure in
        //  the output which correspond the phrase only.
        [Test] public void  TestPhrasalSearchThenOrdinary()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "token1 token2 token3 token4 token5." );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "\"token2 token3\"" );
            AssertIfTrue( "Query 1 must be successful", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "token1 token5" );
            AssertIfTrue( "Query 2 must be successful", result.Length == 1 );
        }

        [Test] public void  TestDelimiterSymbols()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "Omea.TextIndex.FullTextIndexer.g() and \"somesome\" l`dargon" );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "omea" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "textIndex" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "fulltextindexer" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "g" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "somesome" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );

            result = indexer.ProcessQueryInternal( "l`dargon" );
            AssertIfTrue( "Query must be successful", result.Length == 1 );
        }

/*
        [Test] public void  TestControlSymbols()
        {
	        UTF8Encoding coder = new UTF8Encoding();
            byte[] buf = new byte[ 3 ];

            buf[ 0 ] = 0x02;
            buf[ 1 ] = 0xb5;
            buf[ 2 ] = 0xc4;
            int charCount = coder.GetCharCount( buf );
            AssertIfTrue( "amount of chars must be only 1", charCount == 1 );
            char[] chars = coder.GetChars( buf );

            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "token1 token2 token3 token4 token5." );
            indexer.EndBatchUpdate();

            string buffer = string.Empty;
            buffer += 'a';
            buffer += ' ';
            buffer += chars[ 0 ];
            buffer += 'a';
            buffer += chars[ 0 ];
            buffer += ' ';
            buffer += chars[ 0 ];
            buffer += ' ';
            indexer.AddDocumentFragment( 200, buffer );
            indexer.EndBatchUpdate();
            indexer.CloseIndices();
        }
*/

        [Test] public void  TestWildcardPrefixes()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id, "Omea omniamea" );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "ome*" );
            AssertIfTrue( "Query must be successful", result.Length == 1 && result[ 0 ].Count == 1 );

            result = indexer.ProcessQueryInternal( "om*" );
            AssertIfTrue( "Query must be successful", result.Length == 1 && result[ 0 ].Count == 2 );
        }

        private void  RegisterTypes()
        {
            _storage.PropTypes.Register( "SectionHelpDescription", PropDataType.String );
            _storage.PropTypes.Register( "SectionShortName", PropDataType.String );
            _storage.PropTypes.Register( "SectionOrder", PropDataType.Int );
            _storage.ResourceTypes.Register( DocumentSectionResource.DocSectionResName, "", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            _storage.GetAllResources( DocumentSectionResource.DocSectionResName ).DeleteAll();
            RegisterDocumentSection( DocumentSection.BodySection, "Full content of the resource", null );
            RegisterDocumentSection( DocumentSection.SubjectSection, "Describes subject (or heading, title) of the e-mail, article, etc", "SU" );
            RegisterDocumentSection( DocumentSection.AnnotationSection, "a note added by way of comment or explanation", "AN" );
        }
        #endregion Tokenizing And QueryProcessing

        #region Contexts
        [Test] public void Context1()
        {
            indexer.AddDocumentFragment( 10,  "Attachment 0\n" );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "attachment" );
            Assert.IsTrue( result.Length == 1 );

            ArrayList contextHighlightData;
            Hashtable map = new Hashtable();
            map[ 0 ] = "attachment";
            string[]  tokens = QueryProcessor.LastSearchLexemes;
            ContextCtor.GetContext( result[ 0 ], tokens, out contextHighlightData );
        }

        [Test] public void TestContextWithQueryTermNearRightEnd()
        {
            IResource newRes1 = Core.ResourceStore.NewResource( "TestType" );
            indexer.AddDocumentFragment( newRes1.Id,  "word1 word2 word3 word4 word5 word6 word7 word8 word9 word10 word11 word21 word31 word41 word51 word61 word71 word81 word91 word101 " );
            indexer.EndBatchUpdate();

            Entry[]  result = indexer.ProcessQueryInternal( "word7 word31" );
            Assert.IsTrue( result.Length == 1 );

            ArrayList contextHighlightData;
            string[]  tokens = QueryProcessor.LastSearchLexemes;
            foreach( string tok in tokens )
                Console.Write( tok + "; " );
            Console.WriteLine( " " );
            string context = ContextCtor.GetContext( result[ 0 ], tokens, out contextHighlightData );
            Console.WriteLine( context );
        }
        #endregion Contexts

        #region Aux
        private void RegisterDocumentSection( string sectionName, string description, string shortName )
        {
            int   sectionsNumber = _storage.GetAllResources( DocumentSectionResource.DocSectionResName ).Count;
            IResource thisSection = _storage.FindUniqueResource( DocumentSectionResource.DocSectionResName, "Name", sectionName );
            if( thisSection == null )
            {
                thisSection = _storage.BeginNewResource( DocumentSectionResource.DocSectionResName );
                thisSection.SetProp( "Name", sectionName );
                thisSection.SetProp( "SectionOrder", sectionsNumber );
                if( description != null || description == "" )
                    thisSection.SetProp( "SectionHelpDescription", description );
                if( shortName != null || shortName == "" )
                    thisSection.SetProp( "SectionShortName", shortName );
                thisSection.EndUpdate();
            }
        }
        #endregion Aux
   }
}