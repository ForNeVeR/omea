// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.DebugPlugin
{
    [PluginDescription("Debug & Instrumentation", "JetBrains Inc.", "Allows to track various aspects of Omea internals.", PluginDescriptionFormat.PlainText, "Icons/DebugPluginIcon.png")]
	public class DebugPlugin : IPlugin
	{
        #region IPlugin Members

        public void Register()
        {
            SettingOptionsForDebugDlg.RegisterResources();
        }

        public void Startup()
        {
        }

        public void Shutdown()
        {
        }

        #endregion
    }}
