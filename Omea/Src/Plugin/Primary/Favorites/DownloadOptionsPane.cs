// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.GUIControls;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
	internal class DownloadOptionsPane : AbstractOptionsPane
	{
        private System.Windows.Forms.GroupBox _downloadGroup;
        private System.Windows.Forms.RadioButton _onDemandButton;
        private System.Windows.Forms.RadioButton _immediateButton;
        private System.Windows.Forms.RadioButton _idleButton;
        private System.Windows.Forms.Label label1;
        private ResourceComboBox _bookmarkFoldersBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private JetBrains.Omea.GUIControls.CookieProviderSelector _cookieProviderSelector;
		private System.ComponentModel.Container components = null;

		private DownloadOptionsPane()
		{
			InitializeComponent();
		}

        public static AbstractOptionsPane DownloadOptionsPaneCreator()
        {
            return new DownloadOptionsPane();
        }

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
			this._downloadGroup = new System.Windows.Forms.GroupBox();
			this._onDemandButton = new System.Windows.Forms.RadioButton();
			this._immediateButton = new System.Windows.Forms.RadioButton();
			this._idleButton = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this._bookmarkFoldersBox = new JetBrains.Omea.GUIControls.ResourceComboBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this._cookieProviderSelector = new JetBrains.Omea.GUIControls.CookieProviderSelector();
			this._downloadGroup.SuspendLayout();
			this.SuspendLayout();
			//
			// _downloadGroup
			//
			this._downloadGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._downloadGroup.Controls.Add(this._onDemandButton);
			this._downloadGroup.Controls.Add(this._immediateButton);
			this._downloadGroup.Controls.Add(this._idleButton);
			this._downloadGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._downloadGroup.Location = new System.Drawing.Point(0, 0);
			this._downloadGroup.Name = "_downloadGroup";
			this._downloadGroup.Size = new System.Drawing.Size(384, 92);
			this._downloadGroup.TabIndex = 3;
			this._downloadGroup.TabStop = false;
			this._downloadGroup.Text = "Download Bookmarked Pages";
			//
			// _onDemandButton
			//
			this._onDemandButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._onDemandButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._onDemandButton.Location = new System.Drawing.Point(8, 64);
			this._onDemandButton.Name = "_onDemandButton";
			this._onDemandButton.Size = new System.Drawing.Size(368, 24);
			this._onDemandButton.TabIndex = 2;
			this._onDemandButton.Text = "When &viewing a bookmarked Web page";
			//
			// _immediateButton
			//
			this._immediateButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._immediateButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._immediateButton.Location = new System.Drawing.Point(8, 40);
			this._immediateButton.Name = "_immediateButton";
			this._immediateButton.Size = new System.Drawing.Size(368, 24);
			this._immediateButton.TabIndex = 1;
			this._immediateButton.Text = "&Immediately after adding or importing";
			//
			// _idleButton
			//
			this._idleButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._idleButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._idleButton.Location = new System.Drawing.Point(8, 16);
			this._idleButton.Name = "_idleButton";
			this._idleButton.Size = new System.Drawing.Size(368, 24);
			this._idleButton.TabIndex = 0;
			this._idleButton.Text = "&When the computer is idle";
			//
			// label1
			//
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label1.Location = new System.Drawing.Point(0, 140);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(376, 23);
			this.label1.TabIndex = 4;
			this.label1.Text = "Select &location for categorized and annotated weblinks:";
			//
			// _bookmarkFoldersBox
			//
			this._bookmarkFoldersBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this._bookmarkFoldersBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._bookmarkFoldersBox.Location = new System.Drawing.Point(0, 164);
			this._bookmarkFoldersBox.Name = "_bookmarkFoldersBox";
			this._bookmarkFoldersBox.Size = new System.Drawing.Size(264, 21);
			this._bookmarkFoldersBox.TabIndex = 5;
			//
			// groupBox1
			//
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Location = new System.Drawing.Point(0, 128);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(384, 4);
			this.groupBox1.TabIndex = 7;
			this.groupBox1.TabStop = false;
			//
			// _cookieProviderSelector
			//
			this._cookieProviderSelector.Location = new System.Drawing.Point(0, 100);
			this._cookieProviderSelector.Name = "_cookieProviderSelector";
			this._cookieProviderSelector.Size = new System.Drawing.Size(324, 24);
			this._cookieProviderSelector.TabIndex = 6;
			//
			// DownloadOptionsPane
			//
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this._cookieProviderSelector);
			this.Controls.Add(this._bookmarkFoldersBox);
			this.Controls.Add(this.label1);
			this.Controls.Add(this._downloadGroup);
			this.Name = "DownloadOptionsPane";
			this.Size = new System.Drawing.Size(384, 188);
			this._downloadGroup.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

	    public override void ShowPane()
	    {
            switch( BookmarkService.DownloadMethod )
            {
                case 0: _idleButton.Checked = true; break;
                case 1: _immediateButton.Checked = true; break;
                case 2: _onDemandButton.Checked = true; break;
            }
            _cookieProviderSelector.Populate( typeof( FavoriteJob ) );
	    }

        public override void EnterPane()
        {
            _bookmarkFoldersBox.Items.Clear();
            _bookmarkFoldersBox.AddResourceHierarchy(
                BookmarkService.GetBookmarksRoot(), "Folder",
                FavoritesPlugin._propParent, new AcceptResourceDelegate( AcceptFolder ) );
            int id = Core.SettingStore.ReadInt(
                "Favorites", "CatAnnRoot", BookmarkService.GetBookmarksRoot().Id );
            _bookmarkFoldersBox.SelectedItem = Core.ResourceStore.TryLoadResource( id );
        }

	    public override void OK()
	    {
            ISettingStore settings = Core.SettingStore;
            int lastMethod = BookmarkService.DownloadMethod;
            BookmarkService service = FavoritesPlugin._bookmarkService;

	        if( _idleButton.Checked )
            {
                BookmarkService.DownloadMethod = 0;
                if( lastMethod != 0 )
                {
                    service.SynchronizeBookmarks();
                }
            }
            else if( _immediateButton.Checked )
            {
                BookmarkService.DownloadMethod = 1;
                if( lastMethod != 1 )
                {
                    service.SynchronizeBookmarks();
                }
            }
            else
            {
                BookmarkService.DownloadMethod = 2;
            }
	        CookiesManager.SetUserCookieProviderName( typeof( FavoriteJob ), _cookieProviderSelector.SelectedProfileName );
            IResource res = (IResource) _bookmarkFoldersBox.SelectedItem;
            if( res != null )
            {
                settings.WriteInt( "Favorites", "CatAnnRoot", res.Id );
            }
	    }

        private static bool AcceptFolder( IResource folder )
        {
            bool result = folder.GetLinksTo( null, FavoritesPlugin._propParent ).Minus(
                Core.ResourceStore.FindResourcesWithProp( null, FavoritesPlugin._propInvisible ) ).Count > 0;
            while( result && folder != null && folder != BookmarkService.GetBookmarksRoot() )
            {
                if( folder.HasProp( FavoritesPlugin._propInvisible ) )
                {
                    result = false;
                    break;
                }
                folder = folder.GetLinkProp( FavoritesPlugin._propParent );
            }
            return result;
        }

	    public override string GetHelpKeyword()
	    {
	        return "/reference/web_pages.html";
	    }
	}
}
