// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.LiveJournalPlugin
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    public class LiveJournalPlugin : IPlugin
    {
        private const string _ActionGroup = "RSSImportActions";
        private const string _ActionAbove = "JetBrains.Omea.RSSPlugin.ImportFeedsAction";
        private const string _ActionName  = "Import LiveJournal friends as feeds...";
        private const string _Name        = "LiveJournal friends import.";

        private const string _ConfigSection     = "JetBrains.Omea.SamplePlugins.LiveJournalPlugin";
        private const string _ConfigKeyUsername = "username";
        private const string _ConfigKeyPassword = "password";
        private const string _ConfigKeyFriends  = "friends";

        private static IRssService _RSSService = null;

        internal static IRssService RSSService { get { return _RSSService; } }

        internal static string Name  { get { return _Name; } }

        internal static string ConfigSection     { get { return _ConfigSection; } }
        internal static string ConfigKeyUsername { get { return _ConfigKeyUsername; } }
        internal static string ConfigKeyPassword { get { return _ConfigKeyPassword; } }
        internal static string ConfigKeyFriends  { get { return _ConfigKeyFriends; } }

        public LiveJournalPlugin()
        {}

        #region IPlugin Members

        public void Register()
        {
        }

        public void Startup()
        {
            _RSSService = (IRssService)Core.PluginLoader.GetPluginService( typeof(IRssService) );
            if ( null == _RSSService )
            {
                // Sorry, no RSS plugin
                return;
            }
            // Register our action
            Core.ActionManager.RegisterMainMenuAction(
                new FriendsImportAction(), _ActionGroup,
                new ListAnchor(AnchorType.After, _ActionAbove),
                _ActionName,
                null, null);
        }

        public void Shutdown()
        {
            // do nothing
        }


        #endregion

        private static string[] _updatePeriods = new string[] { "minutely", "hourly", "daily", "weekly" };
        internal static int UpdatePeriodToIndex(string period)
        {
            for( int i = 0; i < _updatePeriods.Length; ++i )
            {
                if(_updatePeriods[i] == period)
                    return i;
            }
            return 1;
        }

        internal static string UpdateIndexToPeriod(int index)
        {
            if( index < 0 || index >= _updatePeriods.Length )
                index = 1;
            return _updatePeriods[index];
        }
    }
}
