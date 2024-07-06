// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;
using System.Text;

using JetBrains.Interop.WinApi;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;
using System.Globalization;

namespace Ini
{
    /// <summary>
    /// Create a New INI file to store or load data
    /// </summary>
    public class IniFile: ISettingStore
    {
        public readonly string path;
        private readonly ObjectPool _stringBuilderPool;

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <param name="INIPath"></param>
        public IniFile(string INIPath)
        {
        	path = INIPath;
        	_stringBuilderPool = new ObjectPool(64, StringBuilderCreator, null, StringBuilderDisposer);
        }

        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <param name="Section"></param>
        /// Section name
        /// <param name="Key"></param>
        /// Key Name
        /// <param name="Value"></param>
        /// Value Name
        public void WriteString( string Section,string Key,string Value )
        {
            Value = Value.Replace( "\r", "\\0x0d" );
            Value = Value.Replace( "\n", "\\0x0a" );
            Value = Value.Replace( "\"", "\\\"" );
            Kernel32Dll.WritePrivateProfileStringW( Section,Key,Value,path );
        }

        /**
         * Writes an integer value to the INI file.
         */

        public void WriteInt( string Section, string Key, int Value )
        {
            WriteString( Section, Key, Value.ToString() );
        }

        /**
         * Writes a boolean value to the INI file.
         */

        public void WriteBool( string Section, string Key, bool Value )
        {
            WriteString( Section, Key, Value ? "1" : "0" );
        }

        /**
         * Writes a date value to the INI file.
         */

        public void WriteDate( string section, string key, DateTime value )
        {
            WriteString( section, key, value.ToString( "dd.MM.yyyy", CultureInfo.InvariantCulture ) );
        }

        /// <summary>
        /// Read String Value From the Ini File, using an empty string as a default value
        /// </summary>
        public string ReadString( string Section, string Key )
        {
            var temp = (StringBuilder) _stringBuilderPool.Alloc();
            try
            {
                GetPrivateProfileString( Section, Key, "", temp, 64000, path );
                if( temp.Length == 0 )
                    WriteString( Section, Key, "" );
                string result = temp.ToString();
                result = result.Replace( "\\0x0d", "\r" );
                result = result.Replace( "\\0x0a", "\n" );
                result = result.Replace( "\\\"", "\"" );
                return result;
            }
            finally
            {
                _stringBuilderPool.Dispose( temp );
            }
        }

        /// <summary>
        /// Reads a string value from the .ini file, using the <paramref name="defaultValue"/> supplied
        /// </summary>
        /// <param name="section">Ini file section name.</param>
        /// <param name="key">Ini file option name in the <paramref name="section"/>.</param>
        /// <param name="defaultValue">The defaule value.</param>
        /// <returns></returns>
        public string ReadString( string section, string key, string defaultValue )
        {
            var temp = (StringBuilder) _stringBuilderPool.Alloc();
            try
            {
                GetPrivateProfileString( section, key, defaultValue, temp, 64000, path );
                if( temp.Length == 0 )
                    WriteString( section, key, "" );
                string result = temp.ToString();
                result = result.Replace( "\\0x0d", "\r" );
                result = result.Replace( "\\0x0a", "\n" );
                result = result.Replace( "\\\"", "\"" );
                return result;
            }
            finally
            {
                _stringBuilderPool.Dispose( temp );
            }
        }

        /**
         * Reads an integer value from the INI file.
         */

        public int ReadInt( string Section, string Key, int defValue )
        {
            string s = ReadString( Section, Key );
            if ( s == "" )
            {
                WriteInt( Section, Key, defValue );
                return defValue;
            }
            try
            {
                return Int32.Parse( s );
            }
            catch( FormatException )
            {
                return defValue;
            }
        }

        /**
         * Reads a boolean value from the INI file.
         */

        public bool ReadBool( string Section, string Key, bool defValue )
        {
            return ( ReadInt( Section, Key, defValue ? 1 : 0 ) != 0 );
        }

		/// <summary>
		/// Reads a date-time value from the INI file.
		/// </summary>
        public DateTime ReadDate( string section, string key, DateTime defValue )
        {
            string s = ReadString( section, key );
            if ( s != "" )
            {
                try
                {
                    return DateTime.ParseExact( s, "dd.MM.yyyy", CultureInfo.InvariantCulture );
                }
                catch( FormatException ) {}
            }
            WriteDate( section, key, defValue );
            return defValue;
        }

        private static void StringBuilderDisposer( object obj )
        {
            var builder = (StringBuilder) obj;
            builder.Length = 0;
        }

        private static object StringBuilderCreator()
        {
            return new StringBuilder( 64000 );
        }

		/// <summary>
		/// Private unsafe implementation.
		/// Uses a string-builder for convenience.
		/// In most cases, <see cref="Kernel32Dll.Helpers.GetProfileString"/> should be used.
		/// </summary>
    	[DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
    	private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
    }
}
