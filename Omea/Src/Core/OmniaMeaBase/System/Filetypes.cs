/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using Microsoft.Win32;

namespace JetBrains.Omea.Base
{
    /** 
     * enumerates file extensions stored in registry and its
     * file types, as these are seen in Windows Explorer
     * returns file type string by extension
     */
    public class FileSystemTypes
    {
        public static string GetFileType( string extension )
        {
            if( !extension.StartsWith( "." ) )
            {
                extension = '.' + extension;
            }
            string filetype = null;
            try
            {
                RegistryKey fileTypeKey = Registry.ClassesRoot.OpenSubKey( extension );
                if( fileTypeKey != null )
                {
                    using( fileTypeKey )
                    {
                        filetype = (string) fileTypeKey.GetValue( null );
                        RegistryKey fileTypeKey2 = Registry.ClassesRoot.OpenSubKey( filetype );
                        if( fileTypeKey2 != null )
                        {
                            using( fileTypeKey2 )
                            {
                                filetype = (string) fileTypeKey2.GetValue( null );
                            }
                        }
                    }
                }
            }
            catch {}
            return filetype;
        }
    }
}