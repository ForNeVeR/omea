// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;

using JetBrains.Annotations;
using JetBrains.Omea.OpenAPI;
using JetBrains.Util;

using Color=System.Drawing.Color;

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
            #region Preconditions
            if (!Core.UserInterfaceAP.IsOwnerThread)
				throw new InvalidOperationException("Formatting must be called on the UI AP thread.");
            #endregion Preconditions

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
                return TryGetEmbeddedResourceIconFromAssembly( asmName, iconName );
            }
            return null;
        }

        public static Image GetFlagResourceImage( IResource flag )
        {
            string asmName = flag.GetStringProp( "IconAssembly" );
            string iconName = flag.GetStringProp( "IconName" );
            if( asmName != null && iconName != null )
            {
                return TryGetEmbeddedResourceImageFromAssembly( asmName, iconName );
            }
            return null;
        }

    	[CanBeNull]
    	public static Icon TryGetEmbeddedResourceIconFromAssembly([NotNull] string asmName, [NotNull] string resName )
        {
            Icon        icon = null;
            Assembly    iconAssembly = FindAssembly( asmName );
            Stream      iconStream = iconAssembly.GetManifestResourceStream( resName );

            return ( iconStream != null ) ? new Icon( iconStream ) : null;
        }

    	[CanBeNull]
    	public static Image TryGetEmbeddedResourceImageFromAssembly([NotNull] string asmName, [NotNull] string resName )
        {
    		return TryGetEmbeddedResourceImageFromAssembly( FindAssembly( asmName ), resName );
        }

    	[CanBeNull]
    	public static Image TryGetEmbeddedResourceImageFromAssembly([CanBeNull] Assembly asm, [NotNull] string resName )
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
    	/// Makes an Avalon Pack Absolute URI for a resource whose relative name is <paramref name="sRelativeNameInCallingAssembly"/> and which is packaged in the <paramref name="assembly"/>.
    	/// </summary>
    	[NotNull]
    	public static Uri MakeResourceUri([NotNull] string sRelativeNameInCallingAssembly, [NotNull] Assembly assembly)
    	{
    		if(sRelativeNameInCallingAssembly.IsEmpty())
    			throw new ArgumentNullException("sRelativeNameInCallingAssembly");
    		if(assembly == null)
    			throw new ArgumentNullException("assembly");

    		sRelativeNameInCallingAssembly = sRelativeNameInCallingAssembly.TrimStart('/');

    		return new Uri(string.Format("pack://application:,,,/{0};component/{1}", assembly.GetName().Name, sRelativeNameInCallingAssembly), UriKind.Absolute);
    	}

    	/// <summary>
    	/// Loads a raster or relative image from the application Pack Resources.
    	/// // TODO: implement loading vector images.
    	/// </summary>
    	/// <param name="uriPack">Pack URI of the resource.</param>
    	[NotNull]
    	public static ImageSource LoadResourceImage([NotNull] Uri uriPack)
    	{
    		if(uriPack == null)
    			throw new ArgumentNullException("uriPack");

    		var result = (ImageSource)TypeDescriptor.GetConverter(typeof(ImageSource)).ConvertFrom(uriPack);

    		if(result == null)
    			throw new InvalidOperationException(string.Format("Could not load an image from the Pack URI {0}.", uriPack));

    		return result;
    	}

    	/// <summary>
    	/// Loads a raster or relative image from the application Pack Resources.
    	/// // TODO: implement loading vector images.
    	/// </summary>
    	/// <param name="sRelativeNameInCallingAssembly">Relative name of the Pack resource.</param>
    	[NotNull]
    	public static ImageSource LoadResourceImage([NotNull] string sRelativeNameInCallingAssembly)
    	{
    		return LoadResourceImage(sRelativeNameInCallingAssembly, Assembly.GetCallingAssembly());
    	}

    	/// <summary>
    	/// Loads a raster or relative image from the application Pack Resources.
    	/// // TODO: implement loading vector images.
    	/// </summary>
    	/// <param name="sRelativeNameInCallingAssembly">Relative name of the Pack resource.</param>
    	/// <param name="assembly">Assembly that contains the resource.</param>
    	[NotNull]
    	public static ImageSource LoadResourceImage([NotNull] string sRelativeNameInCallingAssembly, [NotNull] Assembly assembly)
    	{
    		if(sRelativeNameInCallingAssembly.IsEmpty())
    			throw new ArgumentNullException("sRelativeNameInCallingAssembly");
    		if(assembly == null)
    			throw new ArgumentNullException("assembly");

    		return LoadResourceImage(MakeResourceUri(sRelativeNameInCallingAssembly, assembly));
    	}

    	/// <summary>
    	/// Makes an Avalon Pack Absolute URI for a resource whose relative name is <paramref name="sRelativeNameInCallingAssembly"/> and which is packaged in the calling assembly.
    	/// </summary>
    	[NotNull]
    	public static Uri MakeResourceUri([NotNull] string sRelativeNameInCallingAssembly)
    	{
    		return MakeResourceUri(sRelativeNameInCallingAssembly, Assembly.GetCallingAssembly());
    	}

        /// <summary>
        /// Save image in the system-dependent Temp directory in the ".png" format
        /// (if it is not present already) and return the result path for the file.
        /// Use image's GetHashCode for file name.
        /// </summary>
        [NotNull]
        public static string IconPath([NotNull] Image img )
        {
        	if(img == null)
        		throw new ArgumentNullException("img");
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
            #region Preconditions
            if( name == null )
    			throw new ArgumentNullException( "name" );
            #endregion Preconditions

    		foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
    		{
    			if(asm.GetName().Name == name)
    				return asm;
    		}
			throw new InvalidOperationException(string.Format("The assembly “{0}” could not be found.", name));
    	}

    	/// <summary>
    	/// Compares two collections for equality.
    	/// </summary>
    	public static bool AreEqual<T>([NotNull] ICollection<T> one, [NotNull] ICollection<T> two)
    	{
    		if(one == null)
    			throw new ArgumentNullException("one");
    		if(two == null)
    			throw new ArgumentNullException("two");

    		if(one.Count != two.Count)
    			return false;

    		using(IEnumerator<T> enumOne = one.GetEnumerator())
    		using(IEnumerator<T> enumTwo = two.GetEnumerator())
    		{
    			for(;;)
    			{
    				bool bOne = enumOne.MoveNext();
    				bool bTwo = enumTwo.MoveNext();
    				if(bOne != bTwo)
    					return false;
    				if(!bOne)
    					return true; // End of collection

    				if(!Equals(enumOne.Current, enumTwo.Current))
    					return false;
    			}
    		}
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

    	/// <summary>
    	/// Creates an image source from a byte stream, a raster or vector one.
    	/// // TODO: support vector images.
    	/// Throws on errors.
    	/// </summary>
    	[NotNull]
    	public static ImageSource LoadImage([NotNull] byte[] imagedata)
    	{
    		if(imagedata == null)
    			throw new ArgumentNullException("imagedata");
    		if(imagedata.Length == 0)
    			throw new ArgumentException("The image data is empty.", "imagedata");

    		var result = (ImageSource)TypeDescriptor.GetConverter(typeof(ImageSource)).ConvertFrom(imagedata);
    		if(result == null)
    			throw new InvalidOperationException(string.Format("Could not load an image from the data stream."));
    		return result;
    	}
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
