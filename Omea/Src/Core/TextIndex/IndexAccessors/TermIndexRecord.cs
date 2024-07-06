// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using   System;
using   System.IO;
using   System.Collections;
using   System.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.TextIndex
{
//-----------------------------------------------------------------------------
//  Wrapper around binary representation of the term record.
//  Structure:
//  8b  Record Term hash code (HC)
//  1b  Term Descriptor (not used now)
//  4b  Chaining Offset
//  4b  Amount of Entries
//  {Entry}+
//
//  Entry structure:
//  4b  Document (resource) index
//  4b  Amount of Instances
//-----------------------------------------------------------------------------

public class TermIndexRecord
{
    public TermIndexRecord( BinaryReader reader )
    {
        try
        {
            listTemporaryStorage.Clear();
            HC = IndexConstructor.ReadCount( reader );
            while( true )
            {
                ParseEntry( reader );
//                _chainsCount++;
            }
        }
        catch( EndOfStreamException )
        {
            if( listTemporaryStorage.Count > 0 )
            {
                aEntries = new Entry[ listTemporaryStorage.Count ];
                listTemporaryStorage.CopyTo( aEntries );
            }
        }
    }

    //-------------------------------------------------------------------------
    //  Parser plain sequence of bytes into the entries and their instances.
    //  Comment: Some entries may be marked as "removed", that means that
    //           corresponding documents are no longer exist. Thus field
    //           "DocsNumber" counts *ALL* entries - valid and removed, since
    //           we do not have an ability to physically strip sequence of
    //           bytes. Non-existing documents are marked with "-1" as DocID
    //           Thus we have to allocate actual space only AFTER the number of
    //           entries is known.
    //-------------------------------------------------------------------------

    protected static void ParseEntry( BinaryReader reader )
    {
        int  instancesNumber;
        Entry  new_ = new Entry();

        new_.DocIndex   = IndexConstructor.ReadCount( reader );
        new_.TfIdf      = reader.ReadSingle();
        instancesNumber = IndexConstructor.ReadCount( reader ) + 1;

        if( instancesNumber < 0 )
        {
            throw new FormatException( "TermIndexRecord -- Illegal number of instances for a TermIndex record (" + instancesNumber + ") - possible index corruption" );
        }

        // NB: Discuss an OpenAPI issue for getting current maximal vlaue of document Id
        //     from the ResourceStore.
        //            if( new_.DocIndex >= 10000000 )
        //                throw( new IndexConstructor.TextIndexCorruption( "[DocIndex=" + new_.DocIndex + "] value in [TermIndex record Entry] is greater than a reasonable number of documents - possible index corruption" ));

        //-----------------------------------------------------------------
        try
        {
            if( new_.DocIndex != -1 )
            {
                InstanceOffset[] Offsets = new InstanceOffset[ instancesNumber ];

                for( int j = 0; j < instancesNumber; j++ )
                {
                    Offsets[ j ].Offset = reader.ReadUInt32();
                    Offsets[ j ].CompoundInfo = reader.ReadUInt32();
                }
                new_.Offsets = Offsets;
                listTemporaryStorage.Add( new_ );
            }
            else
            {
                //  this entry has been "removed", do not use in subsequent
                //  processing
                new_ = null;
            }
        }
        catch( OutOfMemoryException )
        {
            throw new FormatException( "TermIndexRecord - illegal number of term instances: [" + instancesNumber + "]" );
        }
    }

    //-------------------------------------------------------------------------
    //  Assuming that caller has already set the necessay offset in the binary
    //  stream
    //-------------------------------------------------------------------------
    public void Save( BinaryWriter writer )
    {
        Debug.Assert( DocsNumber > 0 );

        IndexConstructor.WriteCount( writer, HC );
        //---------------------------------------------------------------------
        for( int i = 0; i < DocsNumber; i++ )
        {
            Entry   e = GetEntryAt( i );
            IndexConstructor.WriteCount( writer, e.DocIndex );
            writer.Write( e.TfIdf );
            IndexConstructor.WriteCount( writer, e.Count - 1 ); // save count minus 1

            foreach( InstanceOffset insoff in e.Offsets )
            {
                writer.Write( insoff.Offset );
                writer.Write( insoff.CompoundInfo );
            }
        }
    }

    public void Compress()
    {
        ArrayList validEntries = new ArrayList();
        for( int i = 0; i < DocsNumber; i++ )
        {
            Entry e = GetEntryAt( i );
            if( e.DocIndex != -1 )
                validEntries.Add( e );
        }
        Debug.Assert( validEntries.Count > 0, "After compression the number of valid entries must be positive" );
        aEntries = (Entry[])validEntries.ToArray( typeof( Entry ));
    }

    public void PopulateRecordID( ushort termNumber )
    {
        for( int i = 0; i < DocsNumber; i++ )
        {
            for( int j = 0; j < aEntries[ i ].Count; j++ )
                aEntries[ i ].Offsets[ j ].BaseID = termNumber;
        }
    }

    public  int     DocsNumber  {   get{  return( (aEntries == null)? 0 : aEntries.Length );  }  }
    public  Entry[] Entries
    {
        get{ return aEntries;  }
        set{ aEntries = value; }
    }
    public  Entry   GetEntryAt( int i_ )
    {
        Debug.Assert( i_ >= 0 && i_ < aEntries.Length );
        return( aEntries[ i_ ] );
    }

    #region Attributes
    public      const  int          ciRecordPrologSize = 4 + 1 + 4 + 4;
    public      const  int          ciEntryPrologSize = 4 + 4 + 4;
    public      const  int          ciEntryDataSize = 4 + 4;
    protected   static ArrayList    listTemporaryStorage = new ArrayList();

    public      int         HC;
    protected   Entry[]     aEntries;
//    public      int         ChainingOffset;
//    public      ushort      _termNumber;
//    public      int         _chainsCount;
    #endregion
}

//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
public  class   Entry : IComparable
{
    public  Entry() { Proximity = EntryProximity.Document; }

    //-------------------------------------------------------------------------
    public  int     DocIndex
    {
        get{ return iDocIndex; }
        set{ iDocIndex = value; }
    }
    public  float   TfIdf
    {
        get{ return fTfIdf; }
        set{ fTfIdf = value; }
    }
    public  InstanceOffset[]   Offsets
    {
        get{ return( aInstances ); }
        set{ aInstances = value;   }
    }
    public  int     Count
    {
        get{ return aInstances.Length; }
    }
    public  InstanceOffset  Instance( int i_ )
    {
        Debug.Assert( aInstances != null );
        Debug.Assert( i_ < aInstances.Length );
        return( aInstances[ i_ ] );
    }
    public  EntryProximity  Proximity
    {
        set{ ResultProximity = value; }
        get{ return ResultProximity;  }
    }

    //-------------------------------------------------------------------------
    //  Define default sorting criterion - by Document ID
    //-------------------------------------------------------------------------
    int     IComparable.CompareTo( object o )
    {
        Entry   entry_ = (Entry)o;
        if( iDocIndex < entry_.DocIndex )
            return( -1 );
        else
        if( iDocIndex == entry_.DocIndex )
            return( 0 );
        else
            return( 1 );
    }

    internal InstanceOffset[]  FilterOffsetsBySection( uint sectionId )
    {
        ArrayList validOffsets = new ArrayList();
        for( int i = 0; i < Count; i++ )
        {
            if( Instance( i ).SectionId == sectionId )
                validOffsets.Add( Instance( i ) );
        }
        return (InstanceOffset[]) validOffsets.ToArray( typeof( InstanceOffset ) );
    }

    //-------------------------------------------------------------------------
    #region Attributes
    protected   int                 iDocIndex;
    protected   float               fTfIdf;
    protected   InstanceOffset[]    aInstances;
    protected   EntryProximity      ResultProximity;
    #endregion
}

    ///<summary>
    ///  NB:  method returns "inverted" value of comparison, so that standard
    ///  Array.Sort operation automatically orders the elements in the descending
    ///  order of metric value
    ///</summary>
    public class CompareByTfIdf : IComparer
    {
        int IComparer.Compare( object left, object right )
        {
            if( ((Entry)left).TfIdf < ((Entry)right).TfIdf )
                return( +1 );
            else
            if( ((Entry)left).TfIdf > ((Entry)right).TfIdf )
                return( -1 );
            else
                return( 0 );
        }
    }

    //-----------------------------------------------------------------------------
    //  Structure keeps basic data for a term in a document - doc index, relevance
    //  metric and instances offsets.
    //-----------------------------------------------------------------------------
    public struct  InstanceOffset
    {
        public  uint    Offset       {  get{ return( iOffset );   }   set{ iOffset = value;    }}
        public  int     OffsetNormal {  get{ return (int)(iOffset & 0x00FFFFFF);  } }
        public  uint    CompoundInfo {  get{ return iCompoundInfo; }  set{ iCompoundInfo = value;   }}
        public  ushort  BaseID       {  get{ return( BaseTermID ); }  set{ BaseTermID = value; }}
        public  ushort  Sentence     {  get{ return (ushort) (iCompoundInfo & 0x0000FFFF);          }}
        public  ushort  TokenOrder   {  get{ return (ushort) ((iCompoundInfo & 0xFFFF0000) >> 16 ); }}
        public  uint    SectionId    {  get{ return( iOffset & 0x1CFFFFFF) >> 26; }}

        uint    iOffset;
        uint    iCompoundInfo;
        ushort  BaseTermID;
    }
}
