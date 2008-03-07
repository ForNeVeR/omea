/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using JetBrains.Annotations;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Base
{
    public class Utils
    {
        public static int GetHashCodeInLowerCase( params string[] strings )
        {
            int result = 5381;
            ushort len = 0xffff;

            foreach( string str in strings)
            {
                foreach ( char ch in str )
                {
                    result = ( ( result << 5 ) + result ) ^ Char.ToLower( ch );
                    ++len;
                }
            }
            return ( result & 0xffffff ) | ( ( len >> 3 ) << 24 );
        }

        public static Exception GetMostInnerException( Exception exception )
        {
            Exception innerExption = exception;
            while ( innerExption != null )
            {
                exception = innerExption;
                innerExption = exception.InnerException;
            }
            return exception;
        }

        public static void DisplayException( Exception e, string caption )
        {
            DisplayException( Core.MainWindow, e, caption );
        }

        public static void DisplayException( IWin32Window parent, Exception e, string caption )
        {
            MessageBox.Show( parent, e.Message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error );
        }

        public static void RunAssociatedApplicationOnFile( string fileName )
        {
            Process p = new Process();
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.FileName = fileName;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.WaitForExit();
            p.Dispose();
        }

        public static string[] SplitString( string orig, string splitter )
        {
            Guard.NullArgument( orig, "orig" );
            Guard.EmptyStringArgument( splitter, "splitter" );
            ArrayList list = new ArrayList();
            string rest = orig;
            int index = rest.IndexOf( splitter );
            while ( index != -1 )
            {
                list.Add( rest.Substring( 0, index ) );
                rest = ( ( index + splitter.Length ) >= rest.Length ) ? string.Empty : rest.Substring( index + splitter.Length );
                index = rest.IndexOf( splitter );
            }
            if ( rest != string.Empty )
            {
                list.Add( rest );
            }
            return (string[])( list.ToArray( typeof (string) ) );
        }

        public static string QuotedString( string str )
        {
            return ( "\"" + str + "\"" );
        }

        public static string ReadEncodedFile( string FileName )
        {
            FileStream fsin = new FileStream( FileName, FileMode.Open, FileAccess.Read );
            BinaryReader br = new BinaryReader( fsin, Encoding.Default );

            byte[] Buffer = br.ReadBytes( (int)fsin.Length );
            for ( int i = 0; i < Buffer.Length; i++ )
            {
                int shift = i % 6;
                if ( i / 2 * 2 == i )
                {
                    shift = i % 8;
                }
                else if ( i / 3 * 3 == i )
                {
                    shift = i % 7;
                }
                Buffer[ i ] = (byte)( Buffer[ i ] + shift );
            }

            fsin.Close();
            return ( Encoding.Default.GetString( Buffer, 0, Buffer.Length ) );
        }

        public static void WriteEncodedFile( string Content, string FileName )
        {
            FileStream fsout = new FileStream( FileName, FileMode.Create );

            byte[] Buffer = Encoding.Default.GetBytes( Content );
            for ( int i = 0; i < Buffer.Length; i++ )
            {
                Debug.Assert( Buffer[ i ] > _EncodingShift );
                int shift = i % 6;
                if ( i / 2 * 2 == i )
                {
                    shift = i % 8;
                }
                else if ( i / 3 * 3 == i )
                {
                    shift = i % 7;
                }
                Buffer[ i ] = (byte)( Buffer[ i ] - shift );
            }
            fsout.Write( Buffer, 0, Buffer.Length );
            fsout.Close();
        }

        public static Color ColorFromString( string str )
        {
            Color color = Color.FromName( str );
            if ( color.ToArgb() == 0 )
            {
                int argb = Int32.Parse( str, NumberStyles.HexNumber );
                color = Color.FromArgb( argb );
            }
            return color;
        }

        public static bool StartsWith( string str, string pattern, bool ignoreCase )
        {
            return string.Compare( str, 0, pattern, 0, pattern.Length, ignoreCase ) == 0;
        }

        public static int IndexOf( string str, string of, bool ignoreCase )
        {
            return IndexOf( str, of, 0, ignoreCase );
        }

        public static int IndexOf( string str, string of, int start, bool ignoreCase )
        {
            if( !ignoreCase )
            {
                return str.IndexOf( of );
            }
            int ofLen = of.Length;
            int len = str.Length - ofLen;
            for( int i = start; i < len; ++i )
            {
                if( string.Compare( str, i, of, 0, ofLen, true ) == 0 )
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool IsValidString( string str )
        {
            return ( str != null ) && ( str.Length > 0 );
        }

        public static string MergeStrings( string[] array, char delim )
        {
            Guard.NullArgument( array, "array" );

            string result = string.Empty;
            foreach ( string str in array )
            {
                if ( str == null )
                {
                    throw new ArgumentNullException( "array", "Some string component in the array is null" );
                }
                if ( str.Length > 0 )
                {
                    result += str + delim;
                }
            }
            if ( result.Length > 0 )
            {
                result = result.Remove( result.Length - 1, 1 );
            }

            return result;
        }
        
        /// <summary>
        /// Returns string read from the stream in еру UTF8 encoding.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string StreamToString( Stream stream )
        {
            return StreamToString( stream, Encoding.UTF8 );
        }
        
        /// <summary>
        /// Returns string read from the stream in specified encoding.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string StreamToString( Stream stream, Encoding encoding )
        {
            Guard.NullArgument( stream, "stream" );
            Guard.NullArgument( encoding, "stream" );
            
            StreamReader reader = new StreamReader( stream, encoding );

            return StreamReaderReadToEnd( reader );
        }
        
        /// <summary>
        /// Memory saving replacement for the TextReader.ReadToEnd() method.
        /// </summary>
        public static string StreamReaderReadToEnd( TextReader reader )
        {
            Guard.NullArgument( reader, "reader" );
            
            StringBuilder builder = StringBuilderPool.Alloc();
            try
            {
                int nextChar;
                while( ( nextChar = reader.Read() ) >= 0 )
                {
                    builder.Append( (char) nextChar );
                }
                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( builder );
            }
        }

        private delegate void UpdateHttpStatusDelegate( IStatusWriter writer, string name, int kilobytes );

        public static void UpdateHttpStatus( IStatusWriter writer, string name, int bytes )
        {
            IAsyncProcessor ap = Core.UserInterfaceAP;
            if( !ap.IsOwnerThread )
            {
                // update status in UI thread, because size formatting requires that
                ap.QueueJob( JobPriority.Immediate,
                    new UpdateHttpStatusDelegate( UpdateHttpStatus ), writer, name, bytes );
            }
            else
            {
                bytes &=~1023;
                StringBuilder builder = StringBuilderPool.Alloc();
                try
                {
                    builder.Append( "Downloading " );
                    builder.Append( name );
                    if( bytes == 0 )
                    {
                        builder.Append( "..." );
                    }
                    else
                    {
                        builder.Append( " (" );
                        builder.Append( SizeToString( bytes ) );
                        builder.Append( ')' );
                    }
                    writer.ShowStatus( builder.ToString() );
                }
                finally
                {
                    StringBuilderPool.Dispose( builder );
                }
            }
        }

		/// <summary>
		/// Converts a numeric value into a string that represents the number
		/// expressed as a size value in bytes, kilobytes, megabytes, or gigabytes,
		/// depending on the size.
		/// </summary>
        public static string SizeToString( long size )
        {
			if(!Core.UserInterfaceAP.IsOwnerThread)
				throw new InvalidOperationException("Formatting must be called on the UI AP thread.");

			// Invoke the system formatting routine
			if(StrFormatByteSize64A( size, _buffer, (uint)_buffer.Length ) == IntPtr.Zero)
				return size.ToString( "N" ) + " bytes";	// Format manually on failure

			// Measure string length
			int	nLen;
			for(nLen = 0; (nLen < _buffer.Length) && (_buffer[nLen] != 0); nLen++);

			// Convert to a string
			return Encoding.Default.GetString( _buffer, 0, nLen );
        }

        /// <summary>
        /// Analyze the networking activity on the current computer. Analysis is performed
        /// in periods of time not lesser than "_MaxTimeDiff" in order to minimize the
        /// performance overload.
        /// </summary>
        /// <returns>True if computer is connected to the network and any adapter is active.</returns>
        public static bool IsNetworkConnected()
        {
            bool result = _isNetworkConnected;
            int ticks = Environment.TickCount;
            if( ticks > _lastCheckNetworkTicks + _checkNetworkFrequency )
            {
                _lastCheckNetworkTicks = ticks;
                _isNetworkConnected = result = SystemInformation.Network;
                if( _wasNetworkConnected != result ) 
                {
                    _wasNetworkConnected = result;
                    MethodInvoker changed = NetworkConnectedStateChanged;
                    if( changed != null )
                    {
                        changed();
                    }
                }
            }
            return result;
        }

        public static bool IsNetworkConnectedLight()
        {
            return _isNetworkConnected;
        }

        public static event MethodInvoker NetworkConnectedStateChanged;

        public static Icon GetFlagResourceIcon( IResource flag )
        {
            string asmName = flag.GetStringProp( "IconAssembly" );
            string iconName = flag.GetStringProp( "IconName" );
            if( asmName != null && iconName != null )
            {
                return GetResourceIconFromAssembly( asmName, iconName );
            }
            return null;
        }

        public static Image GetFlagResourceImage( IResource flag )
        {
            string asmName = flag.GetStringProp( "IconAssembly" );
            string iconName = flag.GetStringProp( "IconName" );
            if( asmName != null && iconName != null )
            {
                return GetResourceImageFromAssembly( asmName, iconName );
            }
            return null;
        }

        public static Icon GetResourceIconFromAssembly( string asmName, string resName )
        {
            Icon     icon = null;
            Assembly iconAssembly = FindAssembly( asmName );

            if ( iconAssembly != null )
            {
                Stream iconStream = iconAssembly.GetManifestResourceStream( resName );
                if ( iconStream != null )
                {
                    icon = new Icon( iconStream );
                }
            }

            return icon;
        }

        public static Image GetResourceImageFromAssembly( string asmName, string resName )
        {
            Assembly asm = FindAssembly( asmName );
            return GetResourceImageFromAssembly( asm, resName );
        }

        public static Image GetResourceImageFromAssembly( Assembly asm, string resName )
        {
            if( asm != null )
            {
                Stream stream = asm.GetManifestResourceStream( resName );
                if( stream != null )
                    return Image.FromStream( stream );
            }
            return null;
        }

        /// <summary>
        /// Save image in the system-dependent Temp directory in the ".png" format
        /// (if it is not present already) and return the result path for the file.
        /// Use image's GetHashCode for file name.
        /// </summary>
        public static string IconPath( Image img )
        {
            string iconName = img.GetHashCode() + ".png";
            string path = Path.Combine( Path.GetTempPath(), iconName );
            if( !File.Exists( path ) )
            {
                using( FileStream fs = new FileStream( path, FileMode.Append, FileAccess.Write, FileShare.Read ))
                    img.Save( fs, ImageFormat.Png );
            }
            return path;
        }

    	[NotNull]
    	public static Assembly FindAssembly([NotNull] string name)
    	{
    		if(name == null)
    			throw new ArgumentNullException("name");
    		foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
    		{
    			if(asm.GetName().Name == name)
    				return asm;
    		}
			throw new InvalidOperationException(string.Format("The assembly “{0}” could not be found.", name));
    	}

		/// <summary>
		/// Formatting buffer for the <see cref="SizeToString"/> routine.
		/// </summary>
		protected static byte[] _buffer = new byte[0x100];

		/// <summary>
		/// Converts a numeric value into a string that represents the number
		/// expressed as a size value in bytes, kilobytes, megabytes, or gigabytes,
		/// depending on the size.
		/// </summary>
		/// <param name="nSize">Numeric value to be converted.</param>
		/// <param name="pBuffer">Pointer to a buffer to hold the converted number. Note: this function is bound to call the ANSI version.</param>
		/// <param name="nBufSize">Size of the buffer, in characters. Note: in our case, in bytes.</param>
		/// <returns>Returns the address of the converted string, or <see cref="IntPtr.Zero"/> if the conversion fails.</returns>
		/// <remarks>
		/// The following table illustrates how this function converts a numeric value into a text string.
		/// 
		/// Numeric value -> Text string 
		/// 532 -> 532 bytes 
		/// 1340 -> 1.30KB 
		/// 23506 -> 22.9KB 
		/// 2400016 -> 2.29MB 
		/// 2400000000 -> 2.23GB 
		/// </remarks>
		[DllImport("shlwapi.dll", CharSet=CharSet.Ansi)]
		public static extern IntPtr StrFormatByteSize64A(Int64 nSize, byte[] pBuffer, uint nBufSize);

    	//---------------------------------------------------------------------
        private const int       _EncodingShift = 8;
        private const int       _checkNetworkFrequency = 10000; // milliseconds
        private static int      _lastCheckNetworkTicks = 0;
        private static bool     _isNetworkConnected = true;
        private static bool     _wasNetworkConnected = false;
    }

    public class StringStrictComparer : IComparer
    {
        private readonly CompareInfo info = CultureInfo.InvariantCulture.CompareInfo;

        #region IComparer Members

        public int Compare( object x, object y )
        {
            // Do you think this works??? Huj !!! Fuck!!! - not enough info...
            //            return String.Compare( x_, y_, false, System.Globalization.CultureInfo.InvariantCulture );
            return info.Compare( (string)x, (string)y, CompareOptions.Ordinal );
        }

        #endregion
    }
}