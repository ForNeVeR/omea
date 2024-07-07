// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
	internal class FavoritesOptionsPane : BookmarksOptionsPane
	{
        private CheckBox _importCheckBox;
        private CheckBox _exportCheckBox;

        private bool exportAllowed = false;

		private FavoritesOptionsPane()
		{
			InitializeComponent();
		}

        public static AbstractOptionsPane FavoritesOptionsPaneCreator()
        {
            return new FavoritesOptionsPane();
        }

        public override void ShowPane()
        {
            exportAllowed = _exportCheckBox.Checked = IEFavoritesBookmarkProfile.ExportToIEAllowed;
            _importCheckBox.Checked = IEFavoritesBookmarkProfile.ImportAllowed;
            _exportCheckBox.Visible = !IsStartupPane;
            CheckImportBox();
        }

        private class WarnMessageBox : MessageBoxWithCheckBox
        {
            public WarnMessageBox( string text )
                : base( text, "Warning", "Never &warn again", false, new string[] { "OK" } , new int[] { (int) DialogResult.OK }, "OK", "OK")
            {
            }
        }

        public override void LeavePane()
        {
            if( !IsStartupPane && _importCheckBox.Checked && !_exportCheckBox.Checked &&
                Core.SettingStore.ReadBool( "Favorites", "ShowFavoritesWarning", true ) )
            {
                WarnMessageBox mbx = new WarnMessageBox(
                    "NOTE: You will not be able to organize, rename or delete the imported Internet Explorer favorites in " +
                    Core.ProductFullName + ". To allow modifications, enable exporting changes to Internet Explorer." );
                if( mbx.Show( this ).Checked )
                {
                    Core.SettingStore.WriteBool( "Favorites", "ShowFavoritesWarning", false );
                }
            }
        }

        public override void OK()
        {
            IEFavoritesBookmarkProfile.ImportAllowed = _importCheckBox.Checked;
            IEFavoritesBookmarkProfile.ExportToIEAllowed = _exportCheckBox.Checked;
            Core.ResourceAP.QueueJob(
                JobPriority.Immediate, new MethodInvoker( FavoritesPlugin._favoritesProfile.StartImport ) );
        }

        public override int OccupiedHeight
        {
            get
            {
                if( _exportCheckBox.Visible )
                {
                    return _exportCheckBox.Top + _exportCheckBox.Height + 4;
                }
                return _importCheckBox.Top + _importCheckBox.Height + 4;
            }
        }


		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._importCheckBox = new System.Windows.Forms.CheckBox();
            this._exportCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            //
            // _importCheckBox
            //
            this._importCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._importCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._importCheckBox.Location = new System.Drawing.Point(0, 0);
            this._importCheckBox.Name = "_importCheckBox";
            this._importCheckBox.Size = new System.Drawing.Size(340, 24);
            this._importCheckBox.TabIndex = 0;
            this._importCheckBox.Text = "&Import Favorites from Internet Explorer";
            this._importCheckBox.CheckedChanged += new System.EventHandler(this._importCheckBox_CheckedChanged);
            //
            // _exportCheckBox
            //
            this._exportCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._exportCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._exportCheckBox.Location = new System.Drawing.Point(0, 20);
            this._exportCheckBox.Name = "_exportCheckBox";
            this._exportCheckBox.Size = new System.Drawing.Size(340, 24);
            this._exportCheckBox.TabIndex = 1;
            this._exportCheckBox.Text = "&Export changes into Internet Explorer";
            //
            // FavoritesOptionsPane
            //
            this.Controls.Add(this._exportCheckBox);
            this.Controls.Add(this._importCheckBox);
            this.Name = "FavoritesOptionsPane";
            this.Size = new System.Drawing.Size(340, 208);
            this.ResumeLayout(false);

        }
		#endregion

	    public override string GetHelpKeyword()
	    {
	        return "/reference/favorites.html";
	    }

        private void _importCheckBox_CheckedChanged( object sender, System.EventArgs e )
        {
            CheckImportBox();
        }

	    private void CheckImportBox()
	    {
	        if( !_importCheckBox.Checked )
	        {
	            exportAllowed = _exportCheckBox.Checked;
	            _exportCheckBox.Enabled = _exportCheckBox.Checked = false;
	        }
	        else
	        {
	            _exportCheckBox.Enabled = true;
	            _exportCheckBox.Checked = exportAllowed;
	        }
	    }
	}
}
