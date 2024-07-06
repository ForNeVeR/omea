// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;

using JetBrains.Interop.WinApi;
using JetBrains.Omea.Base;
using Microsoft.Win32;

namespace JetBrains.Omea.InstantMessaging.Miranda
{
    /**
     * The class responsible for locating the Miranda database we are
     * going to use.
     */

	internal class ProfileManager
	{
        internal static string[] GetProfileList()
        {
            string profileDir = GetProfileDir();
            if ( profileDir == null || !Directory.Exists( profileDir ) )
            {
                return new string[] {};
            }

            string[] databases = Directory.GetFiles( profileDir, "*.dat" );
            string[] databaseNames = new string [databases.Length];
            for( int i=0; i<databases.Length; i++ )
            {
                databaseNames [i] = Path.GetFileNameWithoutExtension( databases [i] );
            }

            return databaseNames;
        }

        internal static string GetDatabasePath( string profileName )
        {
            string dbPath = GetProfileDir();
            if ( dbPath == null )
            {
                return null;
            }
            return Path.Combine( dbPath, profileName ) + ".dat";
        }

		private static string GetProfileDir()
		{
			var installDir = RegUtil.GetValue(Registry.LocalMachine, "SOFTWARE\\Miranda", "Install_Dir") as string;
			if(installDir == null)
				return null;
			installDir = Environment.ExpandEnvironmentVariables(installDir);

			string iniPath = Path.Combine(installDir, "mirandaboot.ini");

			string profileDir = Environment.ExpandEnvironmentVariables(Kernel32Dll.Helpers.GetProfileString(iniPath, "Database", "ProfileDir", ""));
			return Path.Combine(installDir, profileDir);
		}
	}
}
