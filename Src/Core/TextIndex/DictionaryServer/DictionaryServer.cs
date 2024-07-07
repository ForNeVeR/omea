// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using   System;
using   System.Text;
using   System.IO;
using   System.Collections;
using   System.Diagnostics;
using   JetBrains.Omea.Base;
using   JetBrains.DataStructures;

/*
  DictionaryServer is a component to access string data by string Key.
  DictionaryServer collects data from one or several plain text files
  where lines are sorted case-insensitively (it is an obligatory condition,
  otherwise DictionaryServer will not work properly) to make loading data
  quicker. Each source text file can contain lines of two different formats:
    1) <key_value>
       Search is performed only for presence of the given Key.

    2) <key_value>$<data_value>
       Search is performed for presence of the given Key and DataValue is
       returned

  Hierarchy of data access is the following:
  Server->Sequence->Entry->(Key -> Value)

  DictionaryServer performs proper data loading and supplies subcollections
  of data - Sequencies. Sequence can create its own subsequences by any
  string mask: including in the subsequnce all key-data pairs which keys
  are started from the given mask.
  Sequence consists of Entry pairs that represent Key and Value correspondence.
  All keys over all sources *must* be unique; i.e. the given Entry
  represents one source and can return TAG of this source.

  Features
  --------
  1. DictionaryServer's search mechanism relies on the case-insensitive sort.
  2. Case-insensitive sort is made by converting all characters in the
     Key strings to the upper case (NB: in difference to the default C string
     case-insensitive comparison, where all characters are converted to the
     lower case before comparison).

  Dependencies
  ------------
  No external component is used.
 */

namespace JetBrains.Omea.TextIndex
{
    public class DictionaryServer
    {
        //-------------------------------------------------------------------------
        public DictionaryServer( string WordformsFileName, string unchangeablesFileName,
            string stoplistFileName, params string[] dictionaries )
            : this( dictionaries )
        {
            LoadTokenList( stoplistFileName, StopList );
            LoadTokenList( unchangeablesFileName, Unchangeables );
            ReconstructWordformsMappings( WordformsFileName );
        }

        private DictionaryServer( params string[] dictionaries )
        {
            /**
             * try to load compiled dictionaries
             */
            if( File.Exists( OMEnv.CompiledDicsFileName ) )
            {
                try
                {
                    HashMap compiledDicNames = new HashMap();
                    FileStream compiled = IOTools.OpenRead( OMEnv.CompiledDicsFileName, 0x10000 );
                    using( compiled )
                    {
                        BinaryReader reader = new BinaryReader( compiled, Encoding.Unicode  );
                        string name;
                        while( ( name = reader.ReadString() ).Length > 0 )
                        {
                            compiledDicNames[ name ] = reader.ReadInt64();
                        }
                        bool need2Rebuild = false;
                        // check whether all necessary dictionaries are compiled
                        foreach( string dictionary in dictionaries )
                        {
                            if( !compiledDicNames.Contains( dictionary ) ||
                                (long) compiledDicNames[ dictionary ] !=
                                IOTools.GetLastWriteTime( IOTools.GetFileInfo( dictionary ) ).Ticks )
                            {
                                need2Rebuild = true;
                                break;
                            }
                        }
                        if( !need2Rebuild )
                        {
                            int charCount = reader.ReadInt32();
                            DicBase = reader.ReadChars( charCount );
                            int intCount = reader.ReadInt32();
                            BaseIndices = new int[ intCount ];
                            for( int i = 0; i < intCount; ++i )
                            {
                                BaseIndices[ i ] = reader.ReadInt32();
                            }
                            if( BaseIndices.Length >= 2 )
                            {
                                LastWordCharIndex = BaseIndices[ BaseIndices.Length - 2 ];
                                FirstDicChar = DicBase[ 0 ];
                                LastDicChar  = DicBase[ LastWordCharIndex ];
                            }
                            return;
                        }
                    }
                }
                catch {}
            }

            DefaultDictionariesLoading( dictionaries );
            SaveCompiledDictionaries( dictionaries );
        }

        #region LoadAndNormalization
        private void  DefaultDictionariesLoading( string[] dictionaries )
        {
            foreach( string str_ in dictionaries )
            {
                try
                {
                    using( StreamReader file_ = new StreamReader( str_ ) )
                    {
                        LoadDictionaryIntoPool( file_ );
                    }
                }
                catch( FileNotFoundException exc_ )
                {
                    Trace.WriteLine( "Can not process dictionary file [" + str_ + "], reason - " + exc_.Message );
                }
            }
            aUnitedDictionary.Sort( new StringStrictComparer() );
            aUnitedDictionary.TrimToSize();
            NormalizeDictionaryEntries();
            CheckDuplicatesAndReductions();
            ConvertArray2String();
            aUnitedDictionary.Clear();
            aUnitedDictionary = null;
        }

        /**
         * save compiled dictionary
         */
        private void  SaveCompiledDictionaries( string[] dictionaries )
        {
            FileStream compiled = new FileStream( OMEnv.CompiledDicsFileName, FileMode.Create, FileAccess.Write );
            using( compiled )
            {
                BinaryWriter writer = new BinaryWriter( compiled, Encoding.Unicode );
                foreach( string dictionary in dictionaries )
                {
                    writer.Write( dictionary );
                    writer.Write( IOTools.GetLastWriteTime( IOTools.GetFileInfo( dictionary ) ).Ticks );
                }
                writer.Write( string.Empty );
                writer.Write( DicBase.Length );
                writer.Write( DicBase );
                writer.Write( BaseIndices.Length );
                for( int i = 0 ; i < BaseIndices.Length; ++i )
                    writer.Write( BaseIndices[ i ] );
            }
        }

        private void  LoadTokenList( string fileName, HashSet storage )
        {
            if( File.Exists( fileName ) )
            {
                string str;
                using( StreamReader file = new StreamReader( fileName ) )
                {
                    while(( str = file.ReadLine()) != null )
                    {
                        storage.Add( str.Trim() );
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Load new dictionary entries into the pool of other (possibly)
        /// loaded entries.
        /// Perform substitution of all blanks to "&" (which code is greater
        /// than that of blank and "$" - delimiter between Key and DataValue.
        /// </summary>
        //-------------------------------------------------------------------------
        protected void  LoadDictionaryIntoPool( StreamReader file_ )
        {
            string  str_Buffer;
            while(( str_Buffer = file_.ReadLine()) != null )
            {
                aUnitedDictionary.Add( str_Buffer.Replace( " ", "&" ).ToLower() );
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Perform reverse replacing of '&' into blanks, so that LowerBound
        /// can properly find sequences starting from the same words.
        /// </summary>
        //---------------------------------------------------------------------
        protected void  NormalizeDictionaryEntries()
        {
            for( int i = 0; i < aUnitedDictionary.Count; i++ )
            {
                aUnitedDictionary[ i ] = ((string)aUnitedDictionary[ i ]).Replace( "&", " " );
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// - remove duplicated strings
        /// - remove standalone lexemes which also have mapping variants (next
        ///   to them in the list).
        /// </summary>
        //---------------------------------------------------------------------
        protected void  CheckDuplicatesAndReductions()
        {
            int     i = 0;
            while( i < aUnitedDictionary.Count - 1 )
            {
                if( (string)aUnitedDictionary[ i ] == (string)aUnitedDictionary[ i + 1 ] )
                    aUnitedDictionary.RemoveAt( i );
                else
                    if( ((string)aUnitedDictionary[ i + 1 ]).StartsWith( (string)aUnitedDictionary[ i ] ) &&
                    ((string)aUnitedDictionary[ i + 1 ]).Length > ((string)aUnitedDictionary[ i ]).Length &&
                    ((string)aUnitedDictionary[ i + 1 ])[ ((string)aUnitedDictionary[ i ]).Length ] == '$' )
                {
                    aUnitedDictionary.RemoveAt( i );
                }
                else
                    i++;
            }
        }

        protected void  ConvertArray2String()
        {
            // calc the size of the destination store
            int     totalByteSize = 0;
            foreach( string str in aUnitedDictionary )
            {
                totalByteSize += str.Length;
            }
            totalByteSize += aUnitedDictionary.Count; // # of 0x00 bytes

            // create linear structures
            DicBase = new char[ totalByteSize ];
            BaseIndices = new int[ aUnitedDictionary.Count + 1 ];

            // copy string by string
            int     charIndex = 0;
            for( int i = 0; i < aUnitedDictionary.Count; i++ )
            {
                string  str = (string)aUnitedDictionary[ i ];
                str.CopyTo( 0, DicBase, charIndex, str.Length );
                BaseIndices[ i ] = charIndex;

                charIndex += str.Length;
                DicBase[ charIndex++ ] = (char)0x00;
            }

            //  fake index, to simplify calculation of last string length
            BaseIndices[ aUnitedDictionary.Count ] = charIndex;
            LastWordCharIndex = BaseIndices[ aUnitedDictionary.Count - 1 ];
            FirstDicChar = DicBase[ 0 ];
            LastDicChar  = DicBase[ LastWordCharIndex ];
        }
        #endregion

        #region PredicateLookup

        public bool    isUnchangeable( string str )
        {
            return Unchangeables.Contains( str );
        }
        public bool    isStopWord( string str )
        {
            return StopList.Contains( str );
        }

        /// <summary>
        ///  <para>Use BinarySearch on sorted sequence to find the lowerbound of possible
        ///  keys. Three cases are possible:</para>
        ///  <para>
        ///  1. pure [^Key$] is present in the sequence - BinarySearch >= 0 and points
        ///     to the position of Key in the sequence</para>
        ///  <para>
        ///  2. [^Key$Value$] is present in the sequence - BinarySearch less than 0
        ///     and points to the position of Key$Data in the sequence</para>
        ///  <para>
        ///  3. [^Key ...smth...$Value$] is present in the sequence - BinarySearch
        ///     less than 0 and points to the position of ^Key ...smth...$Value$
        ///     in the sequence.</para>
        /// </summary>
        /// <returns>
        /// true, if correponding dictionary entry (single or compound) was
        /// found, false otherwise.
        /// </returns>
        public bool  FindLowerBound( string mask, out int baseIndex, out int length )
        {
            length = baseIndex = -1;
            if( mask.Length > 0 )
            {
                if(( mask[ 0 ] >= FirstDicChar ) && ( mask[ 0 ] <= LastDicChar ))
                {
                    baseIndex = BinarySearch( mask );
                    if( baseIndex >= 0 || isMaskAsProperPrefix( mask, -baseIndex ))
                    {
                        int  absIndex = Math.Abs( baseIndex );
                        int  baseOffset = BaseIndices[ absIndex ];
                        length = BaseIndices[ absIndex + 1 ] - baseOffset - 1;
                        baseIndex = (baseIndex > 0) ? baseOffset : -baseOffset;
                    }
                    else
                        baseIndex = -1;
                }
            }
            return( baseIndex != -1 );
        }

        public int  GetMapIndex( int start, int length )
        {
            Debug.Assert( start >= 0, "Start index is negative" );
            Debug.Assert( start < DicBase.Length, "Start index is larger than dictionary base" );
            Debug.Assert( start + length < DicBase.Length, "End index is larger than dictionary base" );

            int endIndex = start + length;
            for( int i = start + 1; i < endIndex; i++ )
            {
                if( DicBase[ i ] == '$' )
                    return i + 1;
            }
            return -1;
        }
        #endregion

        #region Auxiliary
        //-------------------------------------------------------------------------
        //  Use BinarySearch on sorted sequence to find the lowerbound of possible
        //  keys. Three cases are possible:
        //  1. pure [^Key$] is present in the sequence - BinarySearch >= 0 and points
        //     to the position of Key in the sequence
        //  2. [^Key$Value$] is present in the sequence - BinarySearch < 0 and points
        //     to the position of Key$Data in the sequence
        //  3. [^Key ...smth...$Value$] is present in the sequence - BinarySearch < 0
        //     and points to the position of ^Key ...smth...$Value$ in the sequence.
        //-------------------------------------------------------------------------
        private int BinarySearch( string mask )
        {
            int   left = 0, right = BaseIndices.Length - 2, middle = 0;
            int   compareStatus = 0;

            while( left <= right )
            {
                middle = (left + right) / 2;
                compareStatus = CompareStrings( mask, middle );
                if( compareStatus < 0 )
                    right = middle - 1;
                else
                    if( compareStatus > 0 )
                    left = middle + 1;
                else
                    return middle;
            }
            return (compareStatus > 0) ? -left : -middle;
        }

        private int  CompareStrings( string mask, int index )
        {
            int  maskLen = mask.Length;
            int  dicLen = BaseIndices[ index + 1 ] - BaseIndices[ index ] - 1;
            int  minLen = Math.Min( maskLen, dicLen );
            int  baseIndex = BaseIndices[ index ];

            for( int i = 0; i < minLen; i++ )
            {
                if( mask[ i ] < DicBase[ baseIndex + i ] )
                    return -1;
                else
                    if( mask[ i ] > DicBase[ baseIndex + i ] )
                    return 1;
            }

            if( maskLen < dicLen )
                return -maskLen;
            else
                if( maskLen > dicLen )
                return dicLen;
            else
                return 0;
        }

        protected bool  isMaskAsProperPrefix( string mask, int keyIndex )
        {
            Debug.Assert( keyIndex >= 0 && keyIndex < BaseIndices.Length );

            if( keyIndex == BaseIndices.Length - 1 )
                return false;

            int     maskLen = mask.Length;
            int     baseIndex = BaseIndices[ keyIndex ];
            int     keyLen = BaseIndices[ keyIndex + 1 ] - baseIndex - 1;
            return(( keyLen > maskLen ) && ( DicBase[ baseIndex + maskLen ] == '$' ) &&
                ( CompareStrings( mask, keyIndex ) == -maskLen ));
        }

        public char GetChar( int offset )
        {
            Debug.Assert( offset >= 0 && offset < DicBase.Length );
            return DicBase[ offset ];
        }

        public string GetDicString( int offsetIndex, int length )
        {
            Debug.Assert( offsetIndex >= 0 && offsetIndex < DicBase.Length );
            return new String( DicBase, offsetIndex, length );
        }
        #endregion Auxiliary

        #region Wordforms Processing
        public int  GetWordformVariant( string token )
        {
            HashMap.Entry e = WordformsVariant.GetEntry( token );
            return (e != null) ? (int) e.Value : -1;
        }

        public int  StoreMapping( string wordform, string lexeme )
        {
            Debug.Assert( !WordformsVariant.Contains( wordform ) );

            int  derivationVariant;
            ArrayList   forms;
            HashMap.Entry e = WordformsValues.GetEntry( lexeme );
            if( e != null )
            {
                forms = (ArrayList)e.Value;
            }
            else
            {
                forms = new ArrayList();
                WordformsValues[ lexeme ] = forms;
            }
            forms.Add( wordform );
            WordformsVariant[ wordform ] = derivationVariant = forms.Count;

            WordformsChanged = true;
            return( derivationVariant );
        }

        //---------------------------------------------------------------------
        //  NB: Exceptional conditions are possible if e.g. we failed to flush
        //      wordforms file into HD (due to any external conditions) and
        //      reread its previous state.
        //---------------------------------------------------------------------
        public string  GetLexemeMapping( string lexeme, int mapVariant )
        {
            if( mapVariant <= 0 )
            {
                throw new ArgumentOutOfRangeException( "DictionaryServer -- Mapping variant parameter must be positive." );
            }

            HashMap.Entry e = WordformsValues.GetEntry( lexeme );
            if( e == null )
            {
                throw new ArgumentException( "DictionaryServer -- Illegal call to GetMappingLexeme - lexeme [" +
                    lexeme + " is not present with index " + mapVariant );
            }

            ArrayList   forms = (ArrayList) e.Value;
            if( mapVariant > forms.Count )
            {
                throw new ArgumentOutOfRangeException( "DictionaryServer -- Mapping variant [" + mapVariant +
                    "] is greater than the number of possible wordforms " + forms.Count );
            }

            return (string)forms[ mapVariant - 1 ];
        }

        public void  FlushWordforms( string fileName )
        {
            if( !WordformsChanged )
                return;
            try
            {
                StreamWriter  writer = new StreamWriter( fileName );
                foreach( HashMap.Entry e in WordformsValues )
                {
                    ArrayList  list = (ArrayList) e.Value;
                    writer.Write( e.Key + " - " );
                    for( int i = 0; i < list.Count; i++ )
                    {
                        writer.Write( (string) list[ i ] );
                        if( i != list.Count - 1 )
                        {
                            writer.Write( "|" );
                        }
                    }
                    writer.WriteLine();
                }
                writer.Close();
                WordformsChanged = false;
            }
            catch( System.IO.IOException exc )
            {
                Trace.WriteLine( "DictionaryServer -- Did not manage to flush wordforms into standard file ["
                    + fileName + "] due to the exception:");
                Trace.WriteLine( exc.Message );
            }
        }

        protected void  ReconstructWordformsMappings( string WordformsFileName )
        {
            if( File.Exists( WordformsFileName ) )
            {
                StreamReader  reader = new StreamReader( WordformsFileName );
                string        Buffer;
                WordformsVariant.Clear();
                WordformsValues.Clear();

                while(( Buffer = reader.ReadLine()) != null )
                {
                    int     i_DelimiterIndex = Buffer.IndexOf( " - " );
                    if( i_DelimiterIndex == -1 )
                    {
                        //  Wordforms index corrupted - no delimiter found
                        continue;
                    }

                    string      Lexeme = Buffer.Substring( 0, i_DelimiterIndex );
                    Buffer = Buffer.Substring( i_DelimiterIndex + 3 );
                    string[]    Wordforms = Buffer.Split( '|' );
                    ArrayList   FormsForLexeme = new ArrayList();

                    for( int i = 0; i < Wordforms.Length; i++ )
                    {
                        //  Forbid adding the same wordform twice - this can
                        //  happen e.g. as the result of corruption or other
                        //  internal bugs.
                        string wordform = Wordforms[ i ];
                        if( FormsForLexeme.IndexOf( wordform ) == -1 )
                        {
                            FormsForLexeme.Add( wordform );
                            WordformsVariant[ wordform ] = FormsForLexeme.Count;
                        }
                    }
                    WordformsValues[ Lexeme ] = FormsForLexeme;
                }
                reader.Close();
            }
        }
        #endregion

        //---------------------------------------------------------------------
        //---------------------------------------------------------------------
        protected ArrayList         aUnitedDictionary = new ArrayList();
        protected HashSet           StopList = new HashSet();
        protected HashSet           Unchangeables = new HashSet();
        protected HashMap           WordformsVariant = new HashMap();
        protected HashMap           WordformsValues = new HashMap();
        protected char[]            DicBase;
        protected int[]             BaseIndices;
        protected int               LastWordCharIndex = -1;
        protected char              FirstDicChar, LastDicChar;
        protected bool              WordformsChanged = false;
    }
}
