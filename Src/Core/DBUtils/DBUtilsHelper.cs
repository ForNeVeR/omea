// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.IO;
using System;
using DBIndex;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Database
{
	public class DBHelper
	{
        private static Tracer _tracer = new Tracer( "(DBUtils) DBHelper" );

        //-------------------------------------------------------------------------------------

		public const string cFileExtension = ".dbUtil";

        public static int GetHashCodeInLowerCase( string str )
        {
            return HashFunctions.HashiString32( str );
        }

		public static string GetFullNameForDBStruct( string path, string dbName )
		{
			return Path.Combine( path, dbName ) + ".database.struct.dbUtil";
		}
        private static void RemoveFilesWithExt( string path, string mask )
        {
            string[] files = System.IO.Directory.GetFiles( path, mask );
            foreach ( string fileName in files )
            {
                System.IO.File.Delete( fileName );
            }
        }

        public static void RemoveDBIndexFiles( string path )
        {
            if ( !Directory.Exists( path ) )
                return;

            try
            {
                RemoveFilesWithExt( path, "*.index.dbUtil" );
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
                throw exception;
            }
        }

        public static void RemoveDBFiles( string path )
        {
            if ( !Directory.Exists( path ) )
                return;

            try
            {
                RemoveFilesWithExt( path, "*.dbUtil" );
                RemoveFilesWithExt( path, "*.blob" );
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
                throw exception;
            }
        }
        public static string GetFullNameForStringsTrie( string path, string dbName )
        {
            return Path.Combine( path, dbName ) + ".strings.trie.dbUtil";
        }

		public static string GetFullNameForTable( string path, string dbName, string tblName )
		{
			return Path.Combine( path, dbName ) + "." + tblName + ".table.dbUtil";
		}
		public static string GetFullNameForIndex( string path, string dbName, string tblName,
			string indexName )
		{
			return Path.Combine( path, dbName ) + "_btree." + tblName + "." + indexName +".index.dbUtil";
		}

		public static bool DatabaseExists( string path, string dbName )
		{
            bool databaseExists = File.Exists( GetFullNameForDBStruct( path, dbName ) );
            _tracer.Trace( "DatabaseExists = " + databaseExists.ToString() );
			return databaseExists;
		}

        public static CachedStream PrepareIOFile( ITable table, string strFullPath, FileMode fileMode )
        {
            int databaseCacheSize = 0x20000;
            if( ICore.Instance != null )
            {
                databaseCacheSize = Core.SettingStore.ReadInt( "Omea", "DatabaseCacheSize", 2048 * 1024 );
            }
            CachedStream stream = new CachedStream( new FileStream(
                strFullPath, fileMode, FileAccess.ReadWrite, FileShare.Read, 256 ), databaseCacheSize >> 3 );
            stream.SyncRoot = table;
            return stream;
        }
	}
}
