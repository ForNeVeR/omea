/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Class which manages storing per-tab resource browser settings.
	/// </summary>
	internal class PerTabBrowserSettings
	{
        private Hashtable _autoPreviewTabs = new Hashtable();
        private Hashtable _verticalLayoutTabs = new Hashtable();
        private Hashtable _autoPreviewTabDefaults = new Hashtable();
        private Hashtable _verticalLayoutTabDefaults = new Hashtable();

	    public AutoPreviewMode CurTabAutoPreviewMode
	    {
	        get
	        {
                return (AutoPreviewMode) GetCurTabSetting( _autoPreviewTabs, _autoPreviewTabDefaults, "AutoPreview", 
                                                           (int) AutoPreviewMode.Off );
	        }
            set
            {
                SetCurTabSetting( _autoPreviewTabs, "AutoPreview", (int) value );
            }
	    }

        public bool VerticalLayout
        {
            get { return GetCurTabSetting( _verticalLayoutTabs, _verticalLayoutTabDefaults, "VerticalLayout", 0 ) != 0; }
            set { SetCurTabSetting( _verticalLayoutTabs, "VerticalLayout", value ? 1 : 0 ); }
        }

        public void SetDefaultsForTab( string tabId, AutoPreviewMode autoPreviewMode, bool verticalLayout )
        {
            _autoPreviewTabDefaults [tabId] = (int) autoPreviewMode;
            _verticalLayoutTabDefaults [tabId] = verticalLayout ? 1 : 0;
        }

        private int GetCurTabSetting( Hashtable tabSettings, Hashtable defaultTabSettings, string settingName, int defaultValue )
	    {
            string curTab = Core.TabManager.CurrentTabId;
            if ( curTab == null )
            {
                return defaultValue;
            }
            object autoPreviewObj = tabSettings [curTab];
            if ( autoPreviewObj != null )
            {
                return (int) autoPreviewObj;
            }

            if ( defaultTabSettings.ContainsKey( curTab ) )
            {
                defaultValue = (int) defaultTabSettings [curTab];
            }
            int setting = Core.SettingStore.ReadInt( settingName, curTab, defaultValue );
            tabSettings [curTab] = setting;
            return setting;
        }

	    private void SetCurTabSetting( Hashtable tabSettings, string settingName, int value )
	    {
            string curTab = Core.TabManager.CurrentTabId;
            tabSettings [curTab] = value;
            Core.SettingStore.WriteInt( settingName, curTab, value );
        }
	}
}
