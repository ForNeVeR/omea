/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.Base;

namespace JetBrains.Omea.DebugPlugin
{
    internal class Settings
    {
        private Settings()
        {}
        private static SettingsCollection _settings = new SettingsCollection();
        #region Settings for Debug plugin

        public static StringSetting ProcessWindowStyle = _settings.Create( "Debug", "ProcessWindowStyle", "Minimized" );

        public static BoolSetting UseShellExecuteForOutlook = _settings.Create( "Debug", "UseShellExecuteForOutlook", false );

        //public static IntSetting ListenersStrategyTimeInSeconds = _settings.Create( "MailIndexing", "ListenersStrategyTimeInSeconds", 0 );

        public static void LoadSettings()
        {
            _settings.LoadSettings();
        }

        #endregion
    }
}
