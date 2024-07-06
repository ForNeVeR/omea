// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.WebContactPlugin
{
    [PluginDescriptionAttribute("JetBrains Inc.", "Contact viewer and editor.")]
    public class WebContactsPlugin : IPlugin
    {
        /// <summary>
        /// Plugin begins its network activity after this timeout (in order not
        /// to interfere with other network-intensive plugins like NntpPlugin).
        /// </summary>
        private const int _cDelayedStartapInterval = 3; // in minutes

        private const string _cIniSectionName = "WebContactsServer";
        private const string _cDefaultOptionsGroup = "Internet";
        private const string _cOptionsDescription = "The Web Contacts Service options enable you to set the particular" +
                                                    "Contact Server address and parameters.";
        #region Properties Attributes
        private int _propLastSynchUpdate;
        #endregion Properties Attributes

        #region IPlugin Members
        public void Register()
        {
            RegisterTypes();
            RegisterOptionsPane();
        }

        public void Startup()
        {
            if( IsServerParameterSet() )
            {
                StartupActivityIfConnected();
            }
        }
        public void Shutdown() { }
        #endregion

        private void RegisterTypes()
        {
            IPropTypeCollection propTypes = Core.ResourceStore.PropTypes;
            _propLastSynchUpdate = propTypes.Register("LastSynchronizeDate", PropDataType.Date, PropTypeFlags.Internal, this);
        }

        private void RegisterOptionsPane()
        {
            IUIManager uiMgr = Core.UIManager;
            uiMgr.RegisterOptionsGroup( _cDefaultOptionsGroup, "The Internet options enable you to control how [product name] works with several types of online content.");
//            OptionsPaneCreator paneCreator = FavoritesOptionsPane.FavoritesOptionsPaneCreator;
//            uiMgr.RegisterOptionsPane(_cDefaultOptionsGroup, "Web Contacts Service", paneCreator, _cOptionsDescription );
        }

        private void StartupActivityIfConnected()
        {
            if( IsServerConnected() )
            {
                AnalyzeContacts();
            }
            else
            {
                Core.NetworkAP.QueueJobAt( DateTime.Now.AddMinutes( 10 ), "Check Contacts Web Service", new MethodInvoker( StartupActivityIfConnected ), null );
            }
        }

        private static bool IsServerParameterSet()
        {
            return !String.IsNullOrEmpty( ServerAddress ) && !String.IsNullOrEmpty( ServerPort );
        }

        private static string ServerAddress
        {
            get { return Core.SettingStore.ReadString( _cIniSectionName, "ServerAddress", null ); }
        }

        private static string ServerPort
        {
            get { return Core.SettingStore.ReadString( _cIniSectionName, "ServerPort", null ); }
        }

        /// <summary>
        /// Ping Contacts Server for service availability. Since plugin's network activity is
        /// started every several minutes we don't store the status but rather get server's status
        /// right before the request.
        /// </summary>
        /// <returns></returns>
        private static bool IsServerConnected()
        {
            return false;
        }

        private void AnalyzeContacts()
        {
            IResourceList contacts = Core.ResourceStore.GetAllResources( "Contact" );
            contacts = contacts.Minus( Core.ResourceStore.FindResourcesWithProp( "Contact", _propLastSynchUpdate ) );
        }
    }
}
