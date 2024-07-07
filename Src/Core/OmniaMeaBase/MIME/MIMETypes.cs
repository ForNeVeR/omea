// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using Microsoft.Win32;
using JetBrains.Omea.Containers;
using JetBrains.DataStructures;

namespace JetBrains.Omea.MIME
{
    /**
     * enumerates MIME types stored in registy (CLASSES_ROOT\MIME\Database\Content Type )
     * and corresponding file extensions
     */
    public class MIMEContentTypes
    {
        static MIMEContentTypes()
        {
            RegistryKey mimeKey = Registry.ClassesRoot.OpenSubKey( _MIMEDbKey );
            string[] mimeContentTypes = mimeKey.GetSubKeyNames();
            RegistryKey contentTypeKey;

            foreach( string mimeContentType in mimeContentTypes )
            {
                contentTypeKey = mimeKey.OpenSubKey( mimeContentType, false );
                if( contentTypeKey != null )
                {
                    string extension = (string) contentTypeKey.GetValue( "Extension" );
                    if( extension != null )
                    {
                        extension = extension.ToLower();
                        _contentTypes2Extensions[ mimeContentType ] = extension;
                        _extensions2ContentTypes[ extension ] = mimeContentType;
                    }
                    contentTypeKey.Close();
                }
            }
            mimeKey.Close();
        }

        public static string GetContentType( string extension )
        {
            return (string) _extensions2ContentTypes[ extension.ToLower() ];
        }

        public static string GetExtension( string contentType )
        {
            return (string) _contentTypes2Extensions[ contentType ];
        }

        private const string    _MIMEDbKey = @"MIME\Database\Content Type";
        private static HashMap  _contentTypes2Extensions = new HashMap();
        private static HashMap  _extensions2ContentTypes = new HashMap();
    }
}
