// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.InstantMessaging.Trillian
{
	/**
	 * Options pane for the Trillian plugin.
	 */

    public class TrillianOptionsPane: AbstractOptionsPane
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckedListBox _lbxProfilesToIndex;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private TrillianProfileManager _profileManager;

		public TrillianOptionsPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.label1 = new System.Windows.Forms.Label();
            this._lbxProfilesToIndex = new System.Windows.Forms.CheckedListBox();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(124, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Trillian profiles to index:";
            //
            // _lbxProfilesToIndex
            //
            this._lbxProfilesToIndex.Location = new System.Drawing.Point(0, 24);
            this._lbxProfilesToIndex.Name = "_lbxProfilesToIndex";
            this._lbxProfilesToIndex.Size = new System.Drawing.Size(180, 84);
            this._lbxProfilesToIndex.TabIndex = 1;
            //
            // TrillianOptionsPane
            //
            this.Controls.Add(this._lbxProfilesToIndex);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "TrillianOptionsPane";
            this.Size = new System.Drawing.Size(232, 150);
            this.ResumeLayout(false);

        }
		#endregion

		public TrillianProfileManager ProfileManager
		{
			get { return _profileManager; }
			set { _profileManager = value; }
		}

        /**
         * Called when the pane is initially shown. Fills the form controls with data.
         */

        public override void ShowPane()
        {
            // Before the call to ShowPane, AbstractOptionsPane.PluginEnvironment
            // is set to the instance of IPluginEnvironment which can be used by
            // the pane.

            string profilesToIndex = ICore.Instance.SettingStore.ReadString( "Trillian",
                "ProfilesToIndex" );
            ArrayList profileList = new ArrayList( profilesToIndex.Split( ';' ) );

            foreach( TrillianProfile profile in _profileManager.Profiles )
            {
                // The property IsStartupPane is set to true when the pane is being shown
                // in the Startup Wizard. In this case, mark all profiles as indexed by
                // default. Otherwise, use the list of indexed profiles loaded from the
                // settings store.
                bool isChecked = IsStartupPane
                    ? true
                    : profileList.IndexOf( profile.Name ) >= 0;

                _lbxProfilesToIndex.Items.Add( profile.Name, isChecked );
            }
        }

        /**
         * Called when the Options dialog or the Startup Wizard is closed with the OK
         * button. Saves the settings data.
         */

        public override void OK()
        {
            string profilesToIndex = "";
            foreach( string profileName in _lbxProfilesToIndex.CheckedItems )
            {
            	if ( profilesToIndex.Length > 0 )
            	{
            		profilesToIndex += ";";
            	}
                profilesToIndex += profileName;
            }

            ICore.Instance.SettingStore.WriteString( "Trillian", "ProfilesToIndex", profilesToIndex );
        }
	}
}
