/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using DBIndex;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.TextIndex
{
//-----------------------------------------------------------------------------
//  Provide access to Term Index records. Extend the functionality provided by
//  TermIndexQuestionnaire by allowing extraction of actual data from the file.
//  - TermIndexAccessor - for direct access using string as a key or term index
//  - SeqTermIndexAccessor - for sequencial access using iterators (design)
//-----------------------------------------------------------------------------

public  class   TermIndexAccessor : IndexAccessorImpl
{
    protected   enum  InitAction { ToInit, ToAdd };

    private int _loadedRecords;
    private int _savedRecords;

    #region TermIndexKey
    internal class TermIndexKey : IFixedLengthKey
    {
        private IComparable    _Key;

        internal TermIndexKey( IComparable key )
        {
            _Key = key;
        }
        #region IFixedLengthKey Members
        public IComparable Key
        {
            get {  return _Key;         }
            set {  _Key = value;  }
        }

        public int KeySize
        {
            get {  return 0;  }
        }

        public void Write(BinaryWriter writer) {}
        public void Read(BinaryReader reader)  {}

        public IFixedLengthKey FactoryMethod()
        {
            return new TermIndexKey( _Key );
        }

        IFixedLengthKey IFixedLengthKey.FactoryMethod(BinaryReader reader)
        {
            return null;
        }

        public void SetIntKey( int key )
        {
            _Key = (IComparable) IntInternalizer.Intern( key );
        }

        #endregion

        #region IComparable Members
        public int CompareTo(object obj)
        {
            return 0;
        }
        #endregion
    }

    #endregion TermIndexKey

    #region Init-Discard

    public TermIndexAccessor( string fileName ) : base( fileName ) {}
    
    public override void  Close()
    {
        base.Close();

        //  check whether close was issued before the index was actually loaded
        if( TermId2RecordHandle != null )
        {
            TermId2RecordHandle.Close();
            TermId2RecordHandle.Dispose();
            TermId2RecordHandle = null;
        }
    }

    public override void  Discard()
    {
        base.Discard();
        if( File.Exists( IndexHeaderFileName )) 
        {
            File.Delete( IndexHeaderFileName );
        }
        Word.DisposeTermTrie();
        if( File.Exists( OMEnv.TokenTreeFileName ))
        {
            File.Delete( OMEnv.TokenTreeFileName );
        }
    }
    
    public override void  Load()
    {
        base.Load();
        InitializationStart();
        TermId2RecordHandle = new OmniaMeaBTree( IndexHeaderFileName, new TermIndexKey( 0 ) );
        TermId2RecordHandle.SetCacheSize( 40 );

        if( !File.Exists( IndexHeaderFileName ) )
        {
            Trace.WriteLineIf( !FullTextIndexer._suppTrace,  "-- TermIndexAccessor - Header file [" + IndexHeaderFileName + "] does not exist." );
            Trace.WriteLineIf( !FullTextIndexer._suppTrace,  "-- TermIndexAccessor - loading indices directly from index" );
            TermId2RecordHandle.Open();
            LoadOffsetsFromIndex();
        }
        else
        {
            bool status = TermId2RecordHandle.Open();
            if( status && TermId2RecordHandle.Count > 0 )
            {
                Trace.WriteLineIf( !FullTextIndexer._suppTrace,  "-- TermIndexAccessor - loaded indices from header subcomponent" );
                Trace.WriteLineIf( !FullTextIndexer._suppTrace,  "-- TermIndexAccessor - Number of loaded offsets: [" + TermId2RecordHandle.Count + "]" );
            }
            else
            {
                if( !status )
                    Trace.WriteLineIf( !FullTextIndexer._suppTrace,  "-- TermIndexAccessor - Header file [" + IndexHeaderFileName + "] exists but did not open." );
                else
                    Trace.WriteLineIf( !FullTextIndexer._suppTrace,  "-- TermIndexAccessor - Header file [" + IndexHeaderFileName + "] exists, opened but its count is 0." );

                Trace.WriteLineIf( !FullTextIndexer._suppTrace,  "-- TermIndexAccessor - loading indices directly from index" );
                TermId2RecordHandle.Clear();
                LoadOffsetsFromIndex();
            }
        }

        InitializationFinish();
    }
    #endregion Init-Discard

    #region LoadOffsets
    protected void  LoadOffsetsFromIndex()
    {
        foreach( int handle in _indexFile.GetAllFiles( false ) )
        {
            if( Core.State == CoreState.ShuttingDown )
            {
                break;
            }
            if( handle != HandleOfHeaderFile ) 
            {
                TermIndexRecord record = GetRecordByHandle( handle );
                recordKey.SetIntKey( record.HC );
                TermId2RecordHandle.InsertKey( recordKey, handle );
            }
        }
        if( Core.State == CoreState.ShuttingDown )
        {
            TermId2RecordHandle.Clear();
        }
    }
    #endregion LoadOffsets

    #region GetTermRecord
    public TermIndexRecord  GetRecord( string term )
    {
        return GetRecordByHC( Word.GetTermId( term.ToLower() ) );
    }

    public TermIndexRecord  GetRecordByHC( int termId )
    {
        #region Preconditions
        if( _indexFile == null )
            throw new ApplicationException( "Aplication has not initialized Accessor yet" );

        if( !InitializationComplete )
            throw new ApplicationException( "Attempt to read TermIndex record before index offsets are loaded" );
        #endregion Preconditions

        int handle = GetRecordHandle( termId );
        return ( handle != -1 ) ? GetRecordByHandle( handle ) : null;
    }

    public TermIndexRecord  GetRecordByHandle( int handle )
    {
        #region Preconditions
        if( !_indexFile.IsValidHandle( handle ) )
            throw new FormatException( "Term handle is invalid: " + handle );
        #endregion Preconditions

        ++_loadedRecords;

        BinaryReader reader = _indexFile.GetFileReader( handle );
        using( reader )
        {
            return new TermIndexRecord( reader );
        }

    }
    #endregion GetTermRecord

    #region AddRecord

    public int AddRecord( int docId, int termId, object instances, int maxTermInDoc )
    {
        #region Preconditions
        if( _indexFile == null )
            throw new ApplicationException( "Aplication has not initialized Accessor yet" );
        #endregion Preconditions

        int   recordHandle = GetRecordHandle( termId );
        BinaryWriter writer;
        if( recordHandle <= 0 )
        {
            writer = _indexFile.AllocFile( out recordHandle );
            TermId2RecordHandle.InsertKey( recordKey, recordHandle );
            IndexConstructor.WriteCount( writer, termId );
        }
        else
        {
            int lastClusterHandle = GetRecordHandle( -termId );
            int saved = lastClusterHandle;
            writer = _indexFile.AppendFile( recordHandle, ref lastClusterHandle );
            if( saved != lastClusterHandle )
            {
                if( saved > 0 )
                {
                    TermId2RecordHandle.DeleteKey( recordKey, saved );
                }
                TermId2RecordHandle.InsertKey( recordKey, lastClusterHandle );
            }
        }
        ++_savedRecords;

        using( writer )
        {
            IndexConstructor.WriteEntry( writer, docId, termId, instances, maxTermInDoc );
        }
        return termId;
    }
    #endregion AddRecord

    //-------------------------------------------------------------------------
    public int  TermsNumber   {  get{ return( TermId2RecordHandle.Count ); }  }

    public bool TermExist( int termId )
    {  
        return( GetRecordHandle( termId ) > 0 );
    }

    public IEnumerable   Keys
    {
        get { return TermId2RecordHandle.GetAllKeys(); }
    }

    public int LoadedRecords
    {
        get { return _loadedRecords; }
    }

    public int SavedRecords
    {
        get { return _savedRecords; }
    }

    //-------------------------------------------------------------------------
    private string IndexHeaderFileName
    {
        get{ return _indexFileName + OMEnv.HeaderExtension; }
    }

    private int  GetRecordHandle( int termId )
    {
        recordKey.SetIntKey( termId );
        IEnumerator enumerator = TermId2RecordHandle.SearchForRange( recordKey, recordKey ).GetEnumerator();
        try
        {
            if( !enumerator.MoveNext() )
            {
                return -1;
            }
            int offset = ( (KeyPair) enumerator.Current )._offset;
            if( enumerator.MoveNext() )
            {
                throw new FormatException( "TermIndexAccessor -- Amount of offsets can not exceed 1 in the B-Tree" );
            }
            return offset;
        }
        finally
        {
            IDisposable disposable = enumerator as IDisposable;
            if( disposable != null )
            {
                disposable.Dispose();
            }
        }
    }

    #region Attributes
    protected   OmniaMeaBTree       TermId2RecordHandle;
    internal    TermIndexKey        recordKey = new TermIndexKey( 0 );
    #endregion
}
}