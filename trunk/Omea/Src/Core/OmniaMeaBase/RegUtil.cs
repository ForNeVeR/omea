/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.Base;
using Microsoft.Win32;

namespace JetBrains.Omea.Base
{
    public class RegUtil
    {
        public const string OMEA_KEY = @"Software\JetBrains\Omea";

        public static bool CreateOmeaKey()
        {
            return RegUtil.CreateSubKey( Registry.CurrentUser, OMEA_KEY );
        }

        public static string DatabasePath
        {
            get
            {
                return RegUtil.GetValue( Registry.CurrentUser, OMEA_KEY, "DbPath" ) as string;
            }
            set
            {
                RegUtil.SetValue( Registry.CurrentUser, OMEA_KEY, "DbPath", value );
            }
        }
        public static string LogPath
        {
            get
            {
                return RegUtil.GetValue( Registry.CurrentUser, OMEA_KEY, "LogPath" ) as string;
            }
            set
            {
                RegUtil.SetValue( Registry.CurrentUser, OMEA_KEY, "LogPath", value );
            }
        }
        public static void DeleteValue( RegistryKey root, string key, string valueName )
        {
            Guard.NullArgument( root, "root" );
            Guard.EmptyStringArgument( key, "key" );
            Guard.EmptyStringArgument( valueName, "valueName" );
            RegistryKey regKey = null;
            try
            {
                regKey = root.OpenSubKey( key, true );
            }
            catch ( Exception exception )
            {
                throw new ApplicationException( "Cannot open subKey = '" + key + "' for root = '" + root.Name + "'", exception );
            }

            if ( regKey != null )
            {
                using ( regKey )
                {
                    try
                    {
                        regKey.DeleteValue( valueName );
                    }
                    catch ( Exception exception )
                    {
                        throw new ApplicationException( "Cannot delete value = '" + valueName + "' for key = '" + key + "'", exception );
                    }
                }
            }
        }
        public static object GetValue( RegistryKey root, string key, string valueName )
        {
            Guard.NullArgument( root, "root" );
            Guard.EmptyStringArgument( key, "key" );
            Guard.NullArgument( valueName, "valueName" );
            RegistryKey regKey = null;
            try
            {
                regKey = root.OpenSubKey( key );
            }
            catch ( Exception exception )
            {
                throw new ApplicationException( "Cannot open subKey = '" + key + "' for root = '" + root.Name + "'", exception );
            }
            if ( regKey != null )
            {
                using ( regKey )
                {
                    try
                    {
                        return regKey.GetValue( valueName );
                    }
                    catch ( Exception exception )
                    {
                        throw new ApplicationException( "Cannot get value = '" + valueName + "' for key = '" + key + "'", exception );
                    }
                }
            }
            return null;
        }
        public static object GetValue( RegistryKey root, string key, string valueName, object defaultValue )
        {
            Guard.NullArgument( root, "root" );
            Guard.EmptyStringArgument( key, "key" );
            Guard.EmptyStringArgument( valueName, "valueName" );
            RegistryKey regKey = null;
            try
            {
                regKey = root.OpenSubKey( key );
            }
            catch ( Exception exception )
            {
                throw new ApplicationException( "Cannot open subKey = '" + key + "' for root = '" + root.Name + "'", exception );
            }
            if ( regKey != null )
            {
                using ( regKey )
                {
                    try
                    {
                        return regKey.GetValue( valueName, defaultValue );
                    }
                    catch ( Exception exception )
                    {
                        throw new ApplicationException( "Cannot get value = '" + valueName + "' for key = '" + key + "'", exception );
                    }
                }
            }
            return defaultValue;
        }
        public static void SetValue( RegistryKey root, string key, string valueName, object value )
        {
            Guard.NullArgument( root, "root" );
            Guard.EmptyStringArgument( key, "key" );
            Guard.NullArgument( valueName, "valueName" );
            Guard.NullArgument( value, "value" );
            RegistryKey regKey = null;
            try
            {
                regKey = root.OpenSubKey( key, true );
            }
            catch ( Exception exception )
            {
                throw new ApplicationException( "Cannot open subKey = '" + key + "' for root = '" + root.Name + "'", exception );
            }
            if ( regKey != null )
            {
                using ( regKey )
                {
                    try
                    {
                        regKey.SetValue( valueName, value );
                    }
                    catch ( Exception exception )
                    {
                        throw new ApplicationException( "Cannot set value = '" + valueName + "' for key = '" + key + "'", exception );
                    }
                }
            }
        }
        public static bool IsKeyExists( RegistryKey root, string key )
        {
            Guard.NullArgument( root, "root" );
            Guard.EmptyStringArgument( key, "key" );
            try
            {
                RegistryKey regKey = root.OpenSubKey( key );
                if ( regKey != null )
                {
                    using ( regKey )
                    {
                        return true;
                    }
                }
                return false;
            }
            catch ( Exception exception )
            {
                throw new ApplicationException( "Cannot open subKey = '" + key + "' for root = '" + root.Name + "'", exception );
            }
        }
        public static bool CreateSubKey( RegistryKey root, string newKey )
        {
            Guard.NullArgument( root, "root" );
            Guard.EmptyStringArgument( newKey, "newKey" );
            try
            {
                RegistryKey regKey = root.CreateSubKey( newKey );
                if ( regKey != null )
                {
                    using ( regKey )
                    {
                        return true;
                    }
                }
                return false;
            }
            catch ( Exception exception )
            {
                throw new ApplicationException( "Cannot create subKey = '" + newKey + "' for root = '" + root.Name + "'", exception );
            }
        }
    }
}
