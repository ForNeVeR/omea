// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using JetBrains.DataStructures;

namespace JetBrains.Omea.Base
{
    /// <summary>
    /// Get icons registered for paritular file extensions.
    /// Manages cache for non-application types of files (*.exe, *.dll)
    /// </summary>
    public class FileIcons
    {
        static FileIcons()
        {
            _files2SmallIcons = new HashMap();
            _files2LargeIcons = new HashMap();
        }

        public static Icon GetFileSmallIcon( string path )
        {
            return GetFileSmallIcon( path, WindowsAPI.SHGFI_SMALLICON );
        }

        public static Icon GetFileLargeIcon( string path )
        {
            return GetFileSmallIcon( path, WindowsAPI.SHGFI_LARGEICON );
        }

        private static Icon GetFileSmallIcon( string path, uint format )
        {
            #region NULL if empty path or no extension
            if (String.IsNullOrEmpty(path))
                return null;

            string extension = IOTools.GetExtension(path);
            if (extension.Length == 0)
                return null;
            #endregion NULL if empty path or no extension

            extension = extension.ToLower();
            if (extension == ".dll" || extension == ".exe")
            {
                extension = path;
            }

            Icon result = null;
            HashMap.Entry E = _files2SmallIcons.GetEntry(extension);
            if (E != null)
            {
                result = (Icon)E.Value;
            }
            else
            {
                Stream tempFile = null;
                try
                {
                    if (!File.Exists(path))
                    {
                        path = Path.Combine(Path.GetTempPath(), Path.GetFileName(path));
                        tempFile = IOTools.CreateFile(path);
                    }
                    WindowsAPI.SHFILEINFO fileInfo = new WindowsAPI.SHFILEINFO();
                    WindowsAPI.SHGetFileInfo(path, 0, ref fileInfo, (uint)Marshal.SizeOf(fileInfo),
                                              WindowsAPI.SHGFI_ICON | format );

                    result = Icon.FromHandle(fileInfo.hIcon);
                    if( format == WindowsAPI.SHGFI_SMALLICON )
                        _files2SmallIcons[ extension ] = result;
                    else
                        _files2LargeIcons[ extension ] = result;

                }
                catch
                {
                    //  nothing to do, just ignore any problems from shell32.dll
                }
                finally
                {
                    if (tempFile != null)
                    {
                        tempFile.Close();
                        IOTools.DeleteFile(path);
                    }
                }
            }
            return result;
        }

        private static readonly HashMap _files2SmallIcons;
        private static readonly HashMap _files2LargeIcons;
    }
}
