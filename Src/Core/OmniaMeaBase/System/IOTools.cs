// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using JetBrains.Omea.Diagnostics;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.Base
{
    /**
     * Exception-safe IO methods
     */
    public class IOTools
    {
        static private Tracer _tracer = new Tracer( "IOTools" );

        public static FileStream CreateFile( string path )
        {
            FileStream stream;
            try
            {
                stream = File.Create( path );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                stream = null;
            }
            return stream;
        }

        public static FileInfo GetFileInfo( string path )
        {
            FileInfo result;
            try
            {
                result = new FileInfo( path );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                result = null;
            }
            return result;
        }

        public static FileStream Open( string path )
        {
            FileStream stream;
            try
            {
                stream = File.Open( path, FileMode.Open );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                stream = null;
            }
            return stream;
        }

        public static FileStream OpenRead( string path )
        {
            return OpenRead( path, 4096 );
        }

        public static FileStream OpenRead( string path, int bufferSize )
        {
            FileStream stream;
            try
            {
                stream = new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                stream = null;
            }
            return stream;
        }

        public static FileStream OpenRead( FileInfo fi )
        {
            FileStream stream;
            try
            {
                stream = OpenRead( fi.FullName );
            }
            catch( Exception e )
            {
                TraceException( e );
                stream = null;
            }
            return stream;
        }

        public static bool CloseStream( Stream stream )
        {
            try
            {
                stream.Close();
            }
            catch( Exception e )
            {
                TraceException( e, "CloseStream" );
                return false;
            }
            return true;
        }

        public static bool DeleteFile( string path )
        {
            try
            {
                File.Delete( path );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                return false;
            }
            return true;
        }

        public static bool MoveFile( string sourceDir, string destDir )
        {
            try
            {
                File.Move( sourceDir, destDir );
            }
            catch( Exception e )
            {
                TraceException( e, sourceDir, destDir );
                return false;
            }
            return true;
        }

        public static DirectoryInfo CreateDirectory( string path )
        {
            DirectoryInfo di;
            try
            {
                di = Directory.CreateDirectory( path );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                di = null;
            }
            return di;
        }

        public static DirectoryInfo GetDirectoryInfo( string path )
        {
            DirectoryInfo result;
            try
            {
                result = new DirectoryInfo( path );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                result = null;
            }
            return result;
        }

        public static bool DeleteDirectory( string path )
        {
            try
            {
                Directory.Delete( path );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                return false;
            }
            return true;
        }

        public static bool DeleteDirectory( string path, bool recursive )
        {
            try
            {
                Directory.Delete( path, recursive );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                return false;
            }
            return true;
        }

        public static bool MoveDirectory( string sourceDir, string destDir )
        {
            try
            {
                Directory.Move( sourceDir, destDir );
            }
            catch( Exception e )
            {
                TraceException( e, sourceDir, destDir );
                return false;
            }
            return true;
        }

        public static DirectoryInfo GetParent( string path )
        {
            DirectoryInfo di;
            try
            {
                di = Directory.GetParent( path );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                di = null;
            }
            return di;
        }

        public static FileInfo[] GetFiles( string path )
        {
            FileInfo[] result;
            try
            {
                DirectoryInfo di = new DirectoryInfo( path );
                result = di.GetFiles();
            }
            catch( Exception e )
            {
                TraceException( e, path );
                result = null;
            }
            return result;
        }

        public static FileInfo[] GetFiles( string path, string pattern )
        {
            FileInfo[] result;
            try
            {
                DirectoryInfo di = new DirectoryInfo( path );
                result = di.GetFiles( pattern );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                result = null;
            }
            return result;
        }

        public static DirectoryInfo[] GetDirectories( string path )
        {
            DirectoryInfo[] result;
            try
            {
                DirectoryInfo di = new DirectoryInfo( path );
				result = di.Exists ? di.GetDirectories() : null;
            }
            catch( Exception e )
            {
                TraceException( e, path );
                result = null;
            }
            return result;
        }

        public static DirectoryInfo[] GetDirectories( string path, string pattern )
        {
            DirectoryInfo[] result;
            try
            {
                DirectoryInfo di = new DirectoryInfo( path );
                result = di.GetDirectories( pattern );
            }
            catch( Exception e )
            {
                TraceException( e, path );
                result = null;
            }
            return result;
        }

        public static string GetFullName( DirectoryInfo di )
        {
            string result;
            try
            {
                result = di.FullName;
            }
            catch( Exception e )
            {
                TraceException( e, "GetFullName( DirectoryInfo )" );
                result = string.Empty;
            }
            return result;
        }

        public static DateTime GetLastWriteTime( DirectoryInfo di )
        {
            DateTime result;
            try
            {
                result = di.LastWriteTime;
            }
            catch( Exception e )
            {
                TraceException( e, "GetLastWriteTime( DirectoryInfo )" );
                result = DateTime.MinValue;
            }
            return result;
        }

        public static string GetFullName( FileInfo fi )
        {
            string result;
            try
            {
                result = fi.FullName;
            }
            catch( Exception e )
            {
                TraceException( e, "GetFullName( FileInfo )" );
                result = string.Empty;
            }
            return result;
        }

        public static DateTime GetLastWriteTime( FileInfo fi )
        {
            DateTime result;
            try
            {
                result = fi.LastWriteTime;
            }
            catch( Exception e )
            {
                TraceException( e, "GetLastWriteTime( FileInfo )" );
                result = DateTime.MinValue;
            }
            return result;
        }

        public static DateTime GetFileLastWriteTime( string path )
        {
            return GetLastWriteTime( GetFileInfo( path ) );
        }

        public static long GetLength( FileInfo fi )
        {
            try
            {
                return fi.Length;
            }
            catch( Exception e )
            {
                TraceException( e, "GetLength( FileInfo )" );
                return 0;
            }
        }

        public static string GetName( FileInfo fi )
        {
            try
            {
                return fi.Name;
            }
            catch( Exception e )
            {
                TraceException( e, "GetName( FileInfo )" );
                return string.Empty;
            }
        }

        public static string GetExtension( FileInfo fi )
        {
            try
            {
                return fi.Extension;
            }
            catch( Exception e )
            {
                TraceException( e, "GetExtension( FileInfo )" );
                return string.Empty;
            }
        }

        public static string GetDirectoryName( FileInfo fi )
        {
            try
            {
                return fi.DirectoryName;
            }
            catch( Exception e )
            {
                TraceException( e, "GetDirectoryName( FileInfo )" );
                return string.Empty;
            }
        }

        public static string GetFileName( string path )
        {
            string result;
            try
            {
                result = Path.GetFileName( path );
            }
            catch( Exception e )
            {
                TraceException( e, "GetFileName" );
                result = string.Empty;
            }
            return result;
        }

        public static string GetExtension( string path )
        {
            string result;
            try
            {
                result = Path.GetExtension( path );
            }
            catch( Exception e )
            {
                TraceException( e, "GetExtension" );
                result = string.Empty;
            }
            return result;
        }

        public static FileAttributes GetAttributes( DirectoryInfo info )
        {
            FileAttributes result;
            try
            {
                result = info.Attributes;
            }
            catch
            {
                result = (FileAttributes)0;
            }
            return result;
        }

        public static FileAttributes GetAttributes( FileInfo info )
        {
            FileAttributes result;
            try
            {
                result = info.Attributes;
            }
            catch
            {
                result = (FileAttributes)0;
            }
            return result;
        }

        public static string Combine( string path1, string path2 )
        {
            string result;
            try
            {
                result = Path.Combine( path1, path2 );
            }
            catch( Exception e )
            {
                TraceException( e, "Combine" );
                result = string.Empty;
            }
            return result;
        }

	    public static ulong  DiskFreeSpaceForUserDB( string workDir )
	    {
            #region Preconditions
            if( workDir == null )
                throw new ArgumentNullException( "IOTools -- Input directory name is NULL" );
            #endregion Preconditions

            Win32Declarations.ULARGE_INTEGER userFree = new Win32Declarations.ULARGE_INTEGER();
            Win32Declarations.ULARGE_INTEGER total = new Win32Declarations.ULARGE_INTEGER();
            Win32Declarations.ULARGE_INTEGER totalFree = new Win32Declarations.ULARGE_INTEGER();
	        bool rc = Win32Declarations.GetDiskFreeSpaceEx( Path.GetPathRoot( Path.GetFullPath( workDir ) ), ref userFree, ref total, ref totalFree );
            if ( !rc )
            {
                throw new IOException( "Failed to get disk free space" );
            }
            return userFree._value;
	    }

        public static void MakeValidFileName( ref string fileName )
        {
            #region Preconditions
            if( fileName == null )
                throw new ArgumentNullException( "IOTools -- Input file name is NULL" );
            #endregion Preconditions

            fileName = fileName.Replace( Path.VolumeSeparatorChar, '_' );
            fileName = fileName.Replace( Path.DirectorySeparatorChar, '_' );
            fileName = fileName.Replace( Path.AltDirectorySeparatorChar, '_' );
            for( int i=0; i<Path.InvalidPathChars.Length; i++ )
            {
                fileName = fileName.Replace( Path.InvalidPathChars [i], '_' );
            }
            fileName = fileName.Replace( '?', '_' );
            fileName = fileName.Replace( '*', '_' );
        }

        private static void TraceException( Exception e, params string[] paths )
        {
            string traceLine = string.Empty;
            foreach( string path in paths )
            {
                traceLine += " ";
                traceLine += path;
            }
            _tracer.TraceException( e );
            _tracer.Trace( "Parameters: ", traceLine );
        }
    }
}
