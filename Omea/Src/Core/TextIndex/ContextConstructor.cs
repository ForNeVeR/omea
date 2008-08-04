/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.DataStructures;

namespace JetBrains.Omea.TextIndex
{
    #region Filters/Comparers
    internal class AnchorComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            WordPtr  inst1 = (WordPtr)x, inst2 = (WordPtr)y;
            if( inst1.SectionId < inst2.SectionId )
                return -1;

            if( inst1.SectionId > inst2.SectionId )
                return 1;

            if( inst1.StartOffset < inst2.StartOffset )
                return -1;

            if( inst1.StartOffset > inst2.StartOffset )
                return 1;

            return 0;
        }
    }
    #endregion Filters/Comparers
    
    public class ContextCtor
	{
        #region Highlighting
        public static void  GetHighlightedTerms( Entry entry, string[] lexemes, out WordPtr[] anchors )
        {
            anchors = new WordPtr[ entry.Count ];

            Trace.WriteLine( "HighlightTerms -- the following terms were processed for highlighting: " );
            for( int i = 0; i < entry.Count; i++ )
            {
                InstanceOffset instance = entry.Instance( i );
                uint   offset = instance.Offset;
                string Lexeme = lexemes[ instance.BaseID ];

                anchors[ i ].Original = Lexeme;
                anchors[ i ].Text = ReconstructWordform( offset, Lexeme, OMEnv.DictionaryServer );
                anchors[ i ].StartOffset = instance.OffsetNormal;
                anchors[ i ].SectionId = (int)instance.SectionId;
                anchors[ i ].Section = DocSectionHelper.FullNameByOrder( instance.SectionId );

//  trace section
                Trace.WriteLine( "      [" + anchors[ i ].Text + "] at " + instance.OffsetNormal +
                                 ", section " + anchors[ i ].Section + ", sentence " + instance.Sentence );
//  end trace section
            }
            Array.Sort( anchors, new AnchorComparer() );
        }
        #endregion Highlighting

        public static string  GetContext( Entry termEntry, string[] lexemes, out ArrayList hgltPairs )
        {
            string  context = cNoContextSign;
            int     contextsNumber = Math.Min( MinimalNumberOfContexts, termEntry.Count );
            int[]   shifts = new int[ termEntry.Count ];
            hgltPairs = new ArrayList();
            Collector.Init( termEntry.Offsets, shifts );

            try
            {
            //  it is possible situation when temporary file is removed
            //  during this processing.
            IResource res = Core.ResourceStore.TryLoadResource( termEntry.DocIndex );
            if( res != null )
            {
                Core.PluginLoader.InvokeResourceTextProviders( res, Collector );

                if( Collector.Body.Length > 0 )
                {
                    context = cFragmentsDelimiter;
                    int  leftBorder = Int32.MaxValue, rightBorder = Int32.MinValue;
                    int  prevContextLength = 0;
                    for( int i = 0; i < contextsNumber; i++ )
                    {
                        InstanceOffset  instance = termEntry.Instance( i );
                        int             origOffset = instance.OffsetNormal;
                        int             offset = Collector.ConvertOffset( origOffset, instance.SectionId );
                        ArrayList       delimiterOffsets = new ArrayList();

                        //  workaround of possible invalid text body reconstruction
                        //  by plugin, when search terms appear out of the text margins...
                        if( offset < Collector.Body.Length )
                        {
                            if(  offset < leftBorder || offset > rightBorder )
                            {
                                leftBorder = Math.Max( 0, offset - cContextSideLength );
                                rightBorder = Math.Min( Collector.Body.Length - 1, offset + cContextSideLength );
                                TuneBorders( offset, Collector.Body, ref leftBorder, ref rightBorder );

                                string  fragment = Collector.Body.Substring( leftBorder, rightBorder - leftBorder + 1 );
                                InsertSectionDelimiters( ref fragment, leftBorder, rightBorder, context.Length, delimiterOffsets );

                                prevContextLength = context.Length;
                                context += fragment + cFragmentsDelimiter;
                            }
                            else
                            if( contextsNumber < termEntry.Count )
                                contextsNumber++;

                            int     startOffset = offset - leftBorder + prevContextLength;
                            string  lexeme = lexemes[ instance.BaseID ];
                            lexeme = ReconstructWordform( instance.Offset, lexeme, OMEnv.DictionaryServer );
                            TuneOffsetByBorders( ref startOffset, delimiterOffsets );

                            hgltPairs.Add( new OffsetData( startOffset, lexeme.Length ));
                        }
                    }
                    context = context.Replace( "\r\n", "  " );
                    context = context.Replace( "\n", " " );
                    context = context.Replace( "\r", " " );
                    context = context.Replace( "\t", " " );
                    Trace.WriteLine( "ContextExtractor -- context for [" + termEntry.DocIndex + "/" + res.Type + "] is [" + context + "]" );
                    foreach( OffsetData pair in hgltPairs )
                    {
                        if( pair.Start + pair.Length >= context.Length )
                            Trace.WriteLine( "                  highlight prefix of token [" + context.Substring( pair.Start ) + "]" );
                        else
                            Trace.WriteLine( "                  highlight token [" + context.Substring( pair.Start, pair.Length ) + "]" );
                    }
                }
            }
            }
            catch
            {
                //  Here we catch exceptions described in the OM-10659, reason
                //  for which is still is not found. Just hide the bug.
            }

            return( context );
        }

        #region Aux
        //---------------------------------------------------------------------
        //  Implemenet several simple heuristics for context aestheticising:
        //  1. do not allow borders cross words.
        //  2. align context along the sentence borders
        //---------------------------------------------------------------------
        private static void    TuneBorders( int offset, string text, ref int leftBorder, ref int rightBorder )
        {
            //--  Preconditions  ----------------------------------------------
            if( offset < 0 )
                throw new ArgumentException( "ContextConstructor -- Offset is negative - " + leftBorder + ":" + rightBorder + ":" + offset + ":" + text.Length );
            if( leftBorder < 0 )
                throw new ArgumentException( "ContextConstructor -- LeftBorder is non-positive - " + leftBorder + ":" + rightBorder + ":" + offset + ":" + text.Length );
            if( rightBorder < 0 )
                throw new ArgumentException( "ContextConstructor -- RightBorder is non-positive - " + leftBorder + ":" + rightBorder + ":" + offset + ":" + text.Length );
            if( leftBorder >= text.Length )
                throw new ArgumentException( "ContextConstructor -- LeftBorder is larger than text fragment - " + leftBorder + ":" + rightBorder + ":" + offset + ":" + text.Length );
            if( rightBorder >= text.Length )
                throw new ArgumentException( "ContextConstructor -- RightBorder is larger than text fragment - " + leftBorder + ":" + rightBorder + ":" + offset + ":" + text.Length );
            if( leftBorder >= rightBorder )
                throw new ArgumentException( "ContextConstructor -- LeftBorder is larger of RightBorder - " + leftBorder + ":" + rightBorder + ":" + offset + ":" + text.Length );
            //--  End of Preconditions  ---------------------------------------

            int delimIndex;
            if( leftBorder > 0 ) // do not touch if == 0.
            {
                delimIndex = text.IndexOf( ' ', leftBorder );
                if(( delimIndex != -1 ) && ( delimIndex < offset - 1 )) // multiple blanks???
                {
                    rightBorder = Math.Min( rightBorder + (delimIndex - leftBorder), text.Length - 1 );
                    leftBorder = delimIndex + 1;
                }
            }

            delimIndex = text.LastIndexOf( ' ', rightBorder, rightBorder - offset );
            if(( delimIndex != -1 ) && ( delimIndex - offset > cMinimalContextSideLength ))
            {
                rightBorder = delimIndex;
            }

            //-----------------------------------------------------------------
            delimIndex = SentenceDelimiterIndex( text, leftBorder, offset - leftBorder );
            if( delimIndex >= offset )
                throw new ArgumentException( "ContextConstructor -- Invalid calculation of sentence border - " + leftBorder + ":" + rightBorder + ":" + offset + ":" + text.Length );
            if(( delimIndex != -1 ) && ( delimIndex + 2 != offset ))
            {
                rightBorder = Math.Min( rightBorder + (delimIndex - leftBorder), text.Length - 1 );
                leftBorder = delimIndex + 2;
            }

            if( rightBorder < 0 )
                throw new ArgumentException( "ContextConstructor -- RightBorder is negative (second round) - " + leftBorder + ":" + rightBorder + ":" + offset + ":" + text.Length );
            if( rightBorder >= text.Length )
                throw new ArgumentException( "ContextConstructor -- RightBorder is larger than text fragment (second round) - " + leftBorder + ":" + rightBorder + ":" + offset + ":" + text.Length );

            delimIndex = text.LastIndexOf( ' ', rightBorder, rightBorder - offset );
            if(( delimIndex != -1 ) && ( delimIndex - offset > cMinimalContextSideLength ))
            {
                rightBorder = delimIndex;
            }
            if( rightBorder >= text.Length )
                throw new ArgumentException( "ContextConstructor -- RightBorder (final) is larger than text fragment (second round) - " + leftBorder + ":" + rightBorder + ":" + offset + ":" + text.Length );
        }

        private static int SentenceDelimiterIndex( string text, int start, int length )
        {
            int  index = Int32.MaxValue;

            DelimiterIndex( ". ", text, start, ref length, ref index );
            DelimiterIndex( ".\n", text, start, ref length, ref index );
            DelimiterIndex( ".\r", text, start, ref length, ref index );
            DelimiterIndex( "? ", text, start, ref length, ref index );
            DelimiterIndex( "?\r", text, start, ref length, ref index );
            DelimiterIndex( "?\n", text, start, ref length, ref index );
            DelimiterIndex( "! ", text, start, ref length, ref index );
            DelimiterIndex( "!\r", text, start, ref length, ref index );
            DelimiterIndex( "!\n", text, start, ref length, ref index );

            return( index == Int32.MaxValue ? -1 : index );
        }

        private static void  DelimiterIndex( string fragment, string text, int start, ref int length, ref int index )
        {
            int delimIndex = text.IndexOf( fragment, start, length );
            if( delimIndex != -1 )
            {
                index = Math.Min( index, delimIndex );
                length = delimIndex - start;
            }
        }

        private static void InsertSectionDelimiters( ref string text,
                                                     int leftBorder, int rightBorder,
                                                     int curLength, IList borders )
        {
            int     shiftOffset = 0;

            foreach( int border in Collector._sectionBorders )
            {
                if( border > leftBorder && border < rightBorder )
                {
                    int offset = border - leftBorder + shiftOffset;
                    if( offset > text.Length )
                        throw new ArgumentException( "ContextCtor -- construction of a context string failed: offset is larger than the length. Sorry." );

                    text = text.Substring( 0, offset ) + cSectionsDelimiter + 
                           text.Substring( offset );
                    shiftOffset += cSectionsDelimiter.Length;
                    borders.Add( offset + curLength );
                }
            }
        }

        private static void  TuneOffsetByBorders( ref int startOffset, ArrayList delimiterOffsets )
        {
            foreach( int offset in delimiterOffsets )
            {
                if( offset <= startOffset )
                    startOffset += 2;
            }
        }
        #endregion Aux

        #region WordformReconstruction
        //---------------------------------------------------------------------
        //  Using simple heuristics, reconstruct wordform (live form) from the
        //  lexeme and information, encoded in the bits of Offset
        //  - 100 - simple plural or 3rd person
        //  - 010 - participle I
        //  - 001 - continuous form
        //---------------------------------------------------------------------
        protected static string  ReconstructWordform( uint Offset, string lexeme, DictionaryServer dicServer )
        {
            string  Context = lexeme;

            //-----------------------------------------------------------------
            if( isPlural( Offset ))
            {
                if( lexeme.EndsWith( "y" ) )
                    Context = Context.Remove( lexeme.Length - 1, 1 ) + "ie";
                Context += "s";
            }
            else
            if( isPast( Offset ))
            {
                if( lexeme[ lexeme.Length - 1 ] == 'y' )
                {
                    Context = Context.Remove( lexeme.Length - 1, 1 );
                    Context += 'i';
                }

                if( Context[ Context.Length - 1 ] != 'e' )
                    Context += 'e';
                Context += 'd';
            }
            else
            if( isContinuous( Offset ))
            {
                if( lexeme[ lexeme.Length - 1 ] == 'e' )
                    Context = Context.Remove( lexeme.Length - 1, 1 );
                Context += "ing";
            }
            else
            if( isWordformIndex( Offset ))
            {
                int  index = RetrieveIndexFromBits( Offset );
                //-------------------------------------------------------------
                //  NB: Exceptional conditions are possible if e.g. DictionaryServer
                //      failed to flush wordforms file into HD (due to any
                //      external conditions) and reread its previous state.
                //-------------------------------------------------------------
                try
                {
                    Context = dicServer.GetLexemeMapping( lexeme, index );
                }
                catch( Exception exc )
                {
                    Trace.WriteLine( "ContextConstructor -- Did not manage to find wordform mapping <" + index +
                                     "> for lexeme [" + lexeme + "] due to the exception:" );
                    Trace.WriteLine( exc.Message );
                    Trace.WriteLine( "ContextConstructor -- The lexeme value is used by default as the Wordform." );
                    Context = lexeme;
                }
            }

            return( Context );
        }

        protected   static  bool    isPlural( uint Mask )
        {   return((( Mask & 0x80000000 ) > 0 ) && (( Mask & 0x63000000 ) == 0));  }

        protected   static  bool    isPast( uint Mask )
        {   return((( Mask & 0x40000000 ) > 0) && (( Mask & 0xA3000000 ) == 0));   }

        protected   static  bool    isContinuous( uint Mask )
        {   return((( Mask & 0x20000000 ) > 0) && (( Mask & 0xC3000000 ) == 0));   }

        protected   static  bool    isWordformIndex( uint Mask )
        {   return(( Mask & 0x03000000 ) > 0 );   }

        protected   static  bool    isSuffixedComma( uint Mask )
        {   return((( Mask & 0x10000000 ) > 0) && (( Mask & 0x08000000 ) == 0));   }

        protected   static  bool    isSuffixedColon( uint Mask )
        {   return((( Mask & 0x08000000 ) > 0) && (( Mask & 0x10000000 ) == 0));   }

        protected   static  bool    isLeftPar( uint Mask )
        {   return(( Mask & 0x04000000 ) > 0 );   }

        protected   static  bool    isRightPar( uint Mask )
        {   return(( Mask & 0x18000000 ) > 0 );   }

        protected   static  int     RetrieveIndexFromBits( uint Mask )
        {
            int     Result = 0;
            if(( Mask & 0x01000000 ) > 0 )
                Result += 1;
            if(( Mask & 0x02000000 ) > 0 )
                Result += 2;
            if(( Mask & 0x20000000 ) > 0 )
                Result += 4;
            if(( Mask & 0x40000000 ) > 0 )
                Result += 8;
            if(( Mask & 0x80000000 ) > 0 )
                Result += 16;
            return( Result );
        }
        #endregion

        #region Attributes
        public const string     cFragmentsDelimiter = "...";
        private static readonly string cNoContextSign = (char)(0x2015) + " no context " + (char)(0x2015);

        private const int       MinimalNumberOfContexts = 2;
        private const int       cContextSideLength = 36;
        private const int       cMinimalContextSideLength = 20;
        private const string    cSectionsDelimiter = "][";

        private static readonly TextCollector  Collector = new TextCollector();
        #endregion Attributes
	}

    #region TextCollector
    /// <summary>
    ///  TextCollector is an implementation of IResourceTextConsumer interface,
    ///  which collects the complete text body of the resource for further
    ///  extraction of context substrings.
    /// </summary>
    internal class TextCollector: IResourceTextConsumer
    {
        internal void   Init( InstanceOffset[] offsets, int[] shifts )
        {
            RejectResult();
            LastSection = "";
            LastSectionRestartsOffset = 0;
//            Shifts = shifts;
//            Offsets = offsets;
            SectionStartOffset.Clear();
            _sectionBorders.Clear();
            SavedNames.Clear();
        }

        internal string Body {  get { return AccumulatedBody.ToString(); }  }

        internal static void  Finished() {}

        #region IResourceTextConsumer2 interface
        public void   AddDocumentHeading( int docID, string text )
        {
            AddDocumentFragment( docID, text, DocumentSection.SubjectSection );
        }
        public void   AddDocumentFragment( int docID, string text )
        {
            AddDocumentFragment( docID, text, DocumentSection.BodySection );
        }
        public void   AddDocumentFragment( int docID, string text, string sectionName )
        {
            if( !String.IsNullOrEmpty( text ) )
            {
                AnalyzeSectionBorder( sectionName );
                AccumulatedBody.Append( text );
            }
            ResId = docID;
        }

        //  As was agreed with HtmlParser, this method is called exclusively
        //  for skipping tag information. Since we have to show the text "nicely",
        //  we subst large amount of blanks with just one for aesteics.
        public void  IncrementOffset( int count )
        {
            for( int i = 0; i < count; i++ )
                AccumulatedBody.Append( ' ' );
        }

        public void  RestartOffsetCounting()
        {
            LastSectionRestartsOffset = AccumulatedBody.Length;
        }
        public void  RejectResult()
        {
            AccumulatedBody.Length = 0;
            if( AccumulatedBody.Capacity > 16384 )
            {
                AccumulatedBody.Capacity = 1024;
            }
        }
        public TextRequestPurpose Purpose
        {   get{  return TextRequestPurpose.ContextExtraction;  }  }
        #endregion IResourceTextConsumer2 interface

        //--------------------------------------------------------------------
        #region Impl
        internal int ConvertOffset( int offset, uint sectionId )
        {
            if( !SectionStartOffset.ContainsKey( (int)sectionId ) )
            {
                string msg = "Mismatch between section names in primary parsing and body extraction [" + sectionId +
                             "] on offset=[" + offset + "], resource type=[" + Core.ResourceStore.LoadResource( ResId ).Type + "];";
                foreach( string str in SavedNames.Keys )
                    msg += " Saved section dump [" + str + "] with Id=" + (int)SavedNames[ str ];
                IResourceList sections = Core.ResourceStore.GetAllResources( DocumentSectionResource.DocSectionResName );
                foreach( IResource section in sections )
                    msg += " DocSection Dump: name=" + section.GetStringProp( "Name" ) + " with order=" + section.GetIntProp("SectionOrder");
                throw new ApplicationException( "ContextConstruction -- " + msg );
            }
            return( offset + SectionStartOffset[ (int)sectionId ] );
        }

        private void  AnalyzeSectionBorder( string sectionName )
        {
            if( sectionName != LastSection )
            {
                if( AccumulatedBody.Length > 0 )
                    _sectionBorders.Add( AccumulatedBody.Length );

                int  sectionId = (int)DocSectionHelper.OrderByFullName( sectionName );
                if( !SectionStartOffset.ContainsKey( sectionId ))
                    SectionStartOffset[ sectionId ] = LastSectionRestartsOffset;

                LastSection = sectionName;
                LastSectionId = sectionId;
                SavedNames[ sectionName ] = sectionId;
            }
        }
        #endregion Impl

        #region Attributes
        private readonly StringBuilder   AccumulatedBody = new StringBuilder();
        private readonly IntHashTableOfInt SectionStartOffset = new IntHashTableOfInt();

        private string          LastSection = "";
        private int             LastSectionId = 0;
        private int             LastSectionRestartsOffset = 0;
        public  List<int>       _sectionBorders = new List<int>();

        private readonly Hashtable  SavedNames = new Hashtable();
        private int             ResId;
        #endregion Attributes
    }
    #endregion TextCollector
}
