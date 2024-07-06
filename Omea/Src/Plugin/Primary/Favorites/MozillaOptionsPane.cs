// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
    internal class MozillaOptionsPane : BookmarksOptionsPane
    {
        private System.Windows.Forms.CheckedListBox _profilesList;
        private System.Windows.Forms.Label label1;
        private string _savedProfiles;

        private MozillaOptionsPane()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this._profilesList = new System.Windows.Forms.CheckedListBox();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(318, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Import bookmarks from the following &Mozilla or Firefox profiles:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _profilesList
            //
            this._profilesList.Dock = System.Windows.Forms.DockStyle.Fill;
            this._profilesList.Location = new System.Drawing.Point(0, 16);
            this._profilesList.Name = "_profilesList";
            this._profilesList.Size = new System.Drawing.Size(376, 229);
            this._profilesList.TabIndex = 2;
            //
            // MozillaOptionsPane
            //
            this.Controls.Add(this._profilesList);
            this.Controls.Add(this.label1);
            this.Name = "MozillaOptionsPane";
            this.Size = new System.Drawing.Size(376, 248);
            this.ResumeLayout(false);

        }

        public static AbstractOptionsPane MozillaOptionsPaneCreator()
        {
            return new MozillaOptionsPane();
        }

        public override void ShowPane()
        {
            _profilesList.BeginUpdate();
            try
            {
                IEnumerable firefoxProfiles = MozillaProfiles.GetFirefoxProfiles();
                IEnumerable firefox09Profiles = MozillaProfiles.GetFirefox09Profiles();
                IEnumerable mozillaProfiles = MozillaProfiles.GetMozillaProfiles();
                string[] activeProfiles = new string[ 0 ];
                if( !IsStartupPane )
                {
                    _savedProfiles = Core.SettingStore.ReadString( "Favorites", "MozillaProfile" );
                    activeProfiles = _savedProfiles.ToLower().Split( ';' );
                }
                else
                {
                    string activeProfile = null;
                    foreach( MozillaProfile profile in firefoxProfiles )
                    {
                        string name = profile.Name;
                        if( name.IndexOf( "default" ) >= 0 )
                        {
                            activeProfile = name;
                            break;
                        }
                    }
                    if( activeProfile == null )
                    {
                        foreach( MozillaProfile profile in firefox09Profiles )
                        {
                            string name = profile.Name;
                            if( name.IndexOf( "default" ) >= 0 )
                            {
                                activeProfile = name;
                                break;
                            }
                        }
                        if( activeProfile == null )
                        {
                            foreach( MozillaProfile profile in firefoxProfiles )
                            {
                                activeProfile = profile.Name;
                                break;
                            }
                            if( activeProfile == null )
                            {
                                foreach( MozillaProfile profile in firefox09Profiles )
                                {
                                    activeProfile = profile.Name;
                                    break;
                                }
                                if( activeProfile == null )
                                {
                                    foreach( MozillaProfile profile in mozillaProfiles )
                                    {
                                        string name = profile.Name;
                                        if( name.IndexOf( "default" ) >= 0 )
                                        {
                                            activeProfile = name;
                                            break;
                                        }
                                    }
                                    if( activeProfile == null )
                                    {
                                        foreach( MozillaProfile profile in mozillaProfiles )
                                        {
                                            activeProfile = profile.Name;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if( activeProfile != null )
                    {
                        activeProfiles = new string[] { activeProfile.ToLower() };
                    }
                }
                UpdateProfileList( activeProfiles, firefoxProfiles );
                UpdateProfileList( activeProfiles, firefox09Profiles );
                UpdateProfileList( activeProfiles, mozillaProfiles );
                UpdateProfileList( activeProfiles, MozillaProfiles.GetAbsoluteFirefoxProfiles() );
            }
            finally
            {
                _profilesList.EndUpdate();
            }
        }

        public override void OK()
        {
            string selectedProfile = string.Empty;
            foreach( string profileName in _profilesList.CheckedItems )
            {
                selectedProfile += profileName;
                selectedProfile += ';';
            }
            selectedProfile = selectedProfile.TrimEnd( ';' );
            if( Core.SettingStore.ReadString( "Favorites", "MozillaProfile" ) != selectedProfile )
            {
                Core.SettingStore.WriteString( "Favorites", "MozillaProfile", selectedProfile );
                OnlineUpdateMozillaProfiles();
            }
        }

        public override void Cancel()
        {
            if( !IsStartupPane )
            {
                Core.SettingStore.WriteString( "Favorites", "MozillaProfile", _savedProfiles );
                OnlineUpdateMozillaProfiles();
            }
        }

        public override void LeavePane()
        {
            if( !IsStartupPane )
            {
                OK();
            }
        }

        private static void OnlineUpdateMozillaProfiles()
        {
            MozillaBookmarkProfile.SetImportPropertiesOfProfiles();
            IBookmarkService service =
                (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
            foreach( IBookmarkProfile profile in service.Profiles )
            {
                MozillaBookmarkProfile mozillaPrf = profile as MozillaBookmarkProfile;
                if( mozillaPrf != null )
                {
                    Core.ResourceAP.RunUniqueJob( new MethodInvoker( profile.StartImport ) );
                }
            }
        }

        public override int OccupiedHeight
        {
            get
            {
                return _profilesList.Top + _profilesList.Items.Count * _profilesList.ItemHeight + 2;
            }
        }

        private void UpdateProfileList( string[] activeProfiles, IEnumerable profiles )
        {
            foreach( MozillaProfile profile in profiles )
            {
                string name = profile.Name;
                _profilesList.Items.Add( name,
                    ( Array.IndexOf( activeProfiles, name.ToLower() ) < 0 ) ? CheckState.Unchecked : CheckState.Checked );
            }
        }

        public override string GetHelpKeyword()
        {
            return "/reference/mozilla_bookmarks.html";
        }
    }
}
