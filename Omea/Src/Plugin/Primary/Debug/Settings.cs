// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
