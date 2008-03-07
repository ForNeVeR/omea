/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.DebugPlugin
{
    [PluginDescriptionAttribute("JetBrains Inc.", "Allows to track various aspects of Omea internals.")]
	public class DebugPlugin : IPlugin
	{
		public DebugPlugin()
		{
        }
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
