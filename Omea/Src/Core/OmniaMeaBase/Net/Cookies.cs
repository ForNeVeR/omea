/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.HttpTools
{
    public interface ICookieProvider
    {
        string Name { get; }
        string GetCookies( string url );
        void SetCookies( string url, string cookies );
    }

    public class CookiesManager
    {
        public const string InternetExplorerCookieProviderName = "Internet Explorer";

        public static void RegisterCookieProvider( ICookieProvider provider )
        {
            Guard.NullArgument( provider, "provider" );
            lock( _providers )
            {
                string name = provider.Name;
                if( _providers.Contains( name ) )
                {
                    throw new InvalidOperationException( "Cookie provider named " + name + " is already registered."  );
                }
                _providers[ name ] = provider;
            }
        }

        public static void DeregisterCookieProvider( ICookieProvider provider )
        {
            Guard.NullArgument( provider, "provider" );
            lock( _providers )
            {
                string name = provider.Name;
                if( _providers.Contains( name ) )
                {
                    throw new InvalidOperationException( "Cookie provider named " + name + " was not registered."  );
                }
                _providers.Remove( name );
            }
        }

        public static ICookieProvider GetCookieProvider( string providerName )
        {
            Guard.NullArgument( providerName, "providerName" );
            lock( _providers )
            {
                return (ICookieProvider)_providers[ providerName ];
            }
        }

        public static ICookieProvider[] GetAllProviders()
        {
            lock( _providers )
            {
                ICookieProvider[] providers = new ICookieProvider[ _providers.Count ];
                int i = 0;
                foreach( HashMap.Entry e in _providers )
                {
                    providers[ i++ ] = (ICookieProvider) e.Value;
                }
                return providers;
            }
        }

        public static string GetUserCookieProviderName( Type userType )
        {
            Guard.NullArgument( userType, "userType" );
            return Core.SettingStore.ReadString( "Cookies", userType.ToString(), InternetExplorerCookieProviderName );
        }

        public static ICookieProvider GetUserCookieProvider( Type userType )
        {
            Guard.NullArgument( userType, "userType" );
            lock( _providers )
            {
                return (ICookieProvider) _providers[ GetUserCookieProviderName( userType ) ];
            }
        }

        public static void SetUserCookieProviderName( Type userType, string providerName )
        {
            Guard.NullArgument( userType, "userType" );
            Guard.NullArgument( providerName, "providerName" );
            Core.SettingStore.WriteString( "Cookies", userType.ToString(), providerName );
        }

        private static HashMap _providers = new HashMap();
    }
}