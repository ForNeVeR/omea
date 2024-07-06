// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.TextIndex
{
    public class IndexAccessorImpl
    {
        protected   IndexAccessorImpl( string fileName )
        {
            isInitializationComplete = false;
            isErrorFlagRaised = false;
            _indexFileName = fileName;
            try
            {
                _indexFile = new BlobFileSystem( fileName, OMEnv.CachingStrategy, TEXTINDEX_FS_CLUSTER_SIZE );
                _indexFile.ManualFlush = true;
                _indexFile.CurrentFragmentationStrategy = BlobFileSystem.FragmentationStrategy.Exponential;
                _indexFile.ClusterCacheSize = 2047;
            }
            catch( IOException e )
            {
                Trace.WriteLine( "IndexAccessorImpl (base) ctor: " + e.Message );
                Trace.WriteLine( "IndexAccessorImpl (base): Discarding index...");
                Discard();
                throw e;
            }
        }

        public virtual void  Load()
        {
            if( !_indexFile.IsValidHandle( HandleOfHeaderFile ) )
            {
                int handle;
                using( BinaryWriter writer = _indexFile.AllocFile( out handle ) )
                {
                    if( HandleOfHeaderFile != handle )
                    {
                        throw new Exception( "BlobFileSystem allocated unexpectable handle for the auxiliary file." );
                    }
                    IndexConstructor.WriteSignature( writer );
                }
            }
            else
            {
                using( BinaryReader header = _indexFile.GetFileReader( HandleOfHeaderFile ) )
                {
                    header.ReadInt64(); // skip date
                    int version = header.ReadInt32();
                    if( version != Version )
                    {
                        throw new FormatException( "Version of current index is not consistent with currently implemented(" +
                                                   version + ":" + Version + ". Force index reconstruction" );
                    }
                }
            }
            isErrorFlagRaised = false;
        }

        public virtual void Flush()
        {
            if( _indexFile != null )
            {
                _indexFile.Flush();
            }
        }

        public virtual void  Close()
        {
            //  For situations, when DiscardIndex is called after it was closed,
            //  check this twice (different callers know different contexts, so
            //  this case is quite general).
            if( _indexFile != null )
            {
                _indexFile.Dispose();
                _indexFile = null;
            }
        }
        public virtual void  Discard()
        {
            Close();
            if( File.Exists( _indexFileName ))
            {
                try
                {
                    File.Delete( _indexFileName );
                }
                catch( Exception e )
                {
                    Trace.WriteLine( "IndexAccessorImpl (base) -- caught " + e.GetType() + " exception for file " + _indexFileName );
                }
            }
            if( File.Exists( _indexFileName ))
            {
                //  There are often a situation occurs when the file is not
                //  deleted on repeated pattern like create-write-close-delete.
                //  Some people see the problem origin in running external
                //  utilities like other indexing process or antivirus soft.
                //  No guaranteed medicine was suggested, thus we just wait
                //  for some time and try to delete again.
                Trace.WriteLine( "IndexAccessorImpl (base) -- First chance -- Did not removed the file " + _indexFileName + " during discard" );
                Thread.Sleep( 1000 );
                try
                {
                    File.Delete( _indexFileName );
                }
                catch( Exception ee )
                {
                    Trace.WriteLine( "IndexAccessorImpl (base) -- caught " + ee.GetType() + " exception for file " + _indexFileName );
                }

                if( File.Exists( _indexFileName ))
                {
                    throw new ApplicationException( "IndexAccessorImpl (base) -- Did not removed the file " + _indexFileName + " during discard" );
                }
            }
        }
        protected void  ThrowCleanedExceptionIf( bool Condition, string Message )
        {
            if( Condition )
            {
                //  Raise this flag so that callers always know that there is no need
                //  to perform any consistency cross-checking, data synchronization,
                //  etc since after the exception will be rethrown, index component
                //  must be rebuilt.
                if( !isErrorFlagRaised )
                {
                    isErrorFlagRaised = true;
                    Close();
                }
                Trace.WriteLine( "-- IndexAccessorImpl(base) -- Thrown exception [" + Message + "]" );
                throw( new FormatException( Message + " - possible index corruption" ));
            }
        }

        //-------------------------------------------------------------------------
        protected void  InitializationStart()  {  isInitializationComplete = false; }
        protected void  InitializationFinish() {  isInitializationComplete = true;  }
        public  bool    InitializationComplete {  get{  return isInitializationComplete;  } }
        public  string  FileName               {  get{  return _indexFileName;            } }

        //-------------------------------------------------------------------------

        public const int        Version = 16;
        public const int        TEXTINDEX_FS_CLUSTER_SIZE = 64;

        // 256 is encapsulated in the BlobFileSystem maximum possible value of minimum cluster size
        // So this value should changed along with changes in BlobFileSystem
        protected const int     HandleOfHeaderFile = 256 / TEXTINDEX_FS_CLUSTER_SIZE;

        private     bool        isInitializationComplete;
        protected   bool        isErrorFlagRaised;

        protected   string          _indexFileName;
        protected   BlobFileSystem  _indexFile;
    }
}
