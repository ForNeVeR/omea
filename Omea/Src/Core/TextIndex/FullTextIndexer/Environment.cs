// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using System.IO;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.TextIndex;

namespace JetBrains.Omea.TextIndex
{
    public class OMEnv
    {
        public static string WorkDir
        {
            get { return _strWorkDir; }
            set { _strWorkDir = value; }
        }

        public static string DataDir
        {
            get { return _strDataDir; }
            set { _strDataDir = value; }
        }

        public static ICachingStrategy CachingStrategy
        {
            get
            {
                if( _cachingStrategy == null )
                {
                    ISettingStore store;
                    int textIndexCacheSize = 2 * 1024 * 1024;
                    if( ICore.Instance != null && ( store = Core.SettingStore ) != null )
                    {
                        textIndexCacheSize = store.ReadInt( "Omea", "TextIndexCacheSize", textIndexCacheSize );
                        if( textIndexCacheSize == 1024 * 1024 )
                        {
                            textIndexCacheSize *= 2;
                        }
                    }
                    _cachingStrategy = new SharedCachingStrategy( textIndexCacheSize );
                }
                return _cachingStrategy;
            }
        }

        public static string MAScriptFileName
        {   get { return Path.Combine( DataDir, "english.bin" ); }     }

        public static string StopWordsFileName
        {   get { return Path.Combine( DataDir, "stopwords.eng"); }    }

        public static string[] DictionaryFileNames
        {   get { return new[] { Path.Combine( DataDir, "oxford.lex" ), Path.Combine( DataDir, "derivates.dat") }; }   }

        public static string TokenTreeFileName
        {   get { return Path.Combine( WorkDir, "_term.index.trie" ); }   }

        public static string CompiledDicsFileName
        {   get { return Path.Combine( WorkDir, "_compiled.dics" ); }   }

        public static string WordformsFileName
        {   get { return Path.Combine( WorkDir, "liveforms.dat" ); }   }

        public static string UnchangeablesList
        {   get { return Path.Combine( DataDir, "unchangeables.dat" ); }   }

        public static string TermIndexFileName
        {   get { return Path.Combine( WorkDir, "_term.index" ); }     }

        public static string TermBatchIndexFileName
        {   get { return Path.Combine( WorkDir, "_term.batch" ); }     }

        public static bool IsDictionaryPresent( ArrayList absentFiles )
        {
            string[] dicts = DictionaryFileNames;
            bool present = new FileInfo( MAScriptFileName ).Exists;
            if( !present )
                absentFiles.Add( MAScriptFileName );
            foreach( string dic in dicts )
            {
                string fileName = Path.Combine( DataDir, dic );
                if( !new FileInfo( fileName ).Exists )
                {
                    present = false;
                    absentFiles.Add( fileName );
                }
            }
            return present;
        }

        //---------------------------------------------------------------------
        //   Set of linguistic-related components
        //---------------------------------------------------------------------

        public static DictionaryServer DictionaryServer
        {
            get
            {
                if( _dictionaryServer == null )
                    _dictionaryServer = new DictionaryServer( WordformsFileName, UnchangeablesList,
                                                              StopWordsFileName, DictionaryFileNames );
                return _dictionaryServer;
            }
        }

        public static ScriptMorphoAnalyzer ScriptMorphoAnalyzer
        {
            get
            {
                if ( _morphAn == null )
                    _morphAn = new ScriptMorphoAnalyzer( MAScriptFileName );

                return _morphAn;
            }
        }

        public static LexemeConstructor LexemeConstructor
        {
            get
            {
                if( _lexemeConstructor == null )
                    _lexemeConstructor = new LexemeConstructor( ScriptMorphoAnalyzer, DictionaryServer );

                return _lexemeConstructor;
            }
        }

        public static void Cleanup()
        {
            if ( _dictionaryServer != null )
                _dictionaryServer.FlushWordforms( WordformsFileName );
        }

        #region Attributes
        public const string  HeaderExtension = ".BHeader";
        public const string  DocHeaderExtension = ".header";
        public const string  IncChunkExtension = ".inc_";

        private static DictionaryServer     _dictionaryServer;
        private static ScriptMorphoAnalyzer _morphAn;
        private static LexemeConstructor    _lexemeConstructor;

        private static string _strWorkDir = ".";
        private static string _strDataDir = "data";

        //-------------------------------------------------------------------------
        // 1MB for whole text index
        private static ICachingStrategy _cachingStrategy;
        //-------------------------------------------------------------------------

        #endregion Attributes
    }
}
