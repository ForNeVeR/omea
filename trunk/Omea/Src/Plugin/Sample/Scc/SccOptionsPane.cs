/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
	/// <summary>
	/// The options pane for the SCC plugin.
	/// </summary>
	public class SccOptionsPane: AbstractOptionsPane
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown _udInitialSync;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown _udPollInterval;
        private System.Windows.Forms.CheckBox _chkHideUnchanged;
        private System.Windows.Forms.GroupBox grpRepositories;
        private System.Windows.Forms.ListBox _lbxRepositories;
        private System.Windows.Forms.Button _btnAddRepository;
        private System.Windows.Forms.Button _btnEditRepository;
        private System.Windows.Forms.Button _btnRemoveRepository;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.GroupBox grpRegexes;
        private System.Windows.Forms.ListBox _lbxRegexes;
        private System.Windows.Forms.Button _btnAddRegex;
        private System.Windows.Forms.Button _btnEditRegex;
        private System.Windows.Forms.Button _btnRemoveRegex;
	    private ContextMenu _repositoryTypesMenu = null;

		public SccOptionsPane()
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
			    if ( _repositoryTypesMenu != null )
			    {
			        _repositoryTypesMenu.Dispose();
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
            this._udInitialSync = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this._udPollInterval = new System.Windows.Forms.NumericUpDown();
            this._chkHideUnchanged = new System.Windows.Forms.CheckBox();
            this.grpRepositories = new System.Windows.Forms.GroupBox();
            this._btnRemoveRepository = new System.Windows.Forms.Button();
            this._btnEditRepository = new System.Windows.Forms.Button();
            this._btnAddRepository = new System.Windows.Forms.Button();
            this._lbxRepositories = new System.Windows.Forms.ListBox();
            this.grpRegexes = new System.Windows.Forms.GroupBox();
            this._btnRemoveRegex = new System.Windows.Forms.Button();
            this._btnEditRegex = new System.Windows.Forms.Button();
            this._btnAddRegex = new System.Windows.Forms.Button();
            this._lbxRegexes = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this._udInitialSync)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._udPollInterval)).BeginInit();
            this.grpRepositories.SuspendLayout();
            this.grpRegexes.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 136);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(204, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Old changesets to synchronize initially:";
            // 
            // _udInitialSync
            // 
            this._udInitialSync.Location = new System.Drawing.Point(220, 132);
            this._udInitialSync.Name = "_udInitialSync";
            this._udInitialSync.Size = new System.Drawing.Size(68, 21);
            this._udInitialSync.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(8, 160);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(200, 16);
            this.label4.TabIndex = 6;
            this.label4.Text = "Poll interval (minutes):";
            // 
            // _udPollInterval
            // 
            this._udPollInterval.Location = new System.Drawing.Point(220, 156);
            this._udPollInterval.Name = "_udPollInterval";
            this._udPollInterval.Size = new System.Drawing.Size(68, 21);
            this._udPollInterval.TabIndex = 1;
            // 
            // _chkHideUnchanged
            // 
            this._chkHideUnchanged.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkHideUnchanged.Location = new System.Drawing.Point(8, 180);
            this._chkHideUnchanged.Name = "_chkHideUnchanged";
            this._chkHideUnchanged.Size = new System.Drawing.Size(296, 20);
            this._chkHideUnchanged.TabIndex = 9;
            this._chkHideUnchanged.Text = "Hide unchanged files from changeset descriptions";
            // 
            // grpRepositories
            // 
            this.grpRepositories.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpRepositories.Controls.Add(this._btnRemoveRepository);
            this.grpRepositories.Controls.Add(this._btnEditRepository);
            this.grpRepositories.Controls.Add(this._btnAddRepository);
            this.grpRepositories.Controls.Add(this._lbxRepositories);
            this.grpRepositories.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.grpRepositories.Location = new System.Drawing.Point(4, 4);
            this.grpRepositories.Name = "grpRepositories";
            this.grpRepositories.Size = new System.Drawing.Size(420, 124);
            this.grpRepositories.TabIndex = 10;
            this.grpRepositories.TabStop = false;
            this.grpRepositories.Text = "Repositories";
            // 
            // _btnRemoveRepository
            // 
            this._btnRemoveRepository.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnRemoveRepository.Location = new System.Drawing.Point(180, 96);
            this._btnRemoveRepository.Name = "_btnRemoveRepository";
            this._btnRemoveRepository.TabIndex = 3;
            this._btnRemoveRepository.Text = "Remove";
            this._btnRemoveRepository.Click += new System.EventHandler(this._btnRemoveRepository_Click);
            // 
            // _btnEditRepository
            // 
            this._btnEditRepository.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnEditRepository.Location = new System.Drawing.Point(92, 96);
            this._btnEditRepository.Name = "_btnEditRepository";
            this._btnEditRepository.TabIndex = 2;
            this._btnEditRepository.Text = "Edit...";
            this._btnEditRepository.Click += new System.EventHandler(this._btnEditRepository_Click);
            // 
            // _btnAddRepository
            // 
            this._btnAddRepository.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnAddRepository.Location = new System.Drawing.Point(4, 96);
            this._btnAddRepository.Name = "_btnAddRepository";
            this._btnAddRepository.TabIndex = 1;
            this._btnAddRepository.Text = "Add...";
            this._btnAddRepository.Click += new System.EventHandler(this._btnAddRepository_Click);
            // 
            // _lbxRepositories
            // 
            this._lbxRepositories.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lbxRepositories.Location = new System.Drawing.Point(4, 22);
            this._lbxRepositories.Name = "_lbxRepositories";
            this._lbxRepositories.Size = new System.Drawing.Size(408, 69);
            this._lbxRepositories.TabIndex = 0;
            this._lbxRepositories.SelectedIndexChanged += new System.EventHandler(this._lbxRepositories_SelectedIndexChanged);
            // 
            // grpRegexes
            // 
            this.grpRegexes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpRegexes.Controls.Add(this._btnRemoveRegex);
            this.grpRegexes.Controls.Add(this._btnEditRegex);
            this.grpRegexes.Controls.Add(this._btnAddRegex);
            this.grpRegexes.Controls.Add(this._lbxRegexes);
            this.grpRegexes.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.grpRegexes.Location = new System.Drawing.Point(4, 204);
            this.grpRegexes.Name = "grpRegexes";
            this.grpRegexes.Size = new System.Drawing.Size(420, 112);
            this.grpRegexes.TabIndex = 11;
            this.grpRegexes.TabStop = false;
            this.grpRegexes.Text = "Regular Expressions for Links in Changeset Descriptions";
            // 
            // _btnRemoveRegex
            // 
            this._btnRemoveRegex.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnRemoveRegex.Location = new System.Drawing.Point(180, 84);
            this._btnRemoveRegex.Name = "_btnRemoveRegex";
            this._btnRemoveRegex.TabIndex = 3;
            this._btnRemoveRegex.Text = "Remove";
            this._btnRemoveRegex.Click += new System.EventHandler(this._btnRemoveRegex_Click);
            // 
            // _btnEditRegex
            // 
            this._btnEditRegex.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnEditRegex.Location = new System.Drawing.Point(92, 84);
            this._btnEditRegex.Name = "_btnEditRegex";
            this._btnEditRegex.TabIndex = 2;
            this._btnEditRegex.Text = "Edit...";
            this._btnEditRegex.Click += new System.EventHandler(this._btnEditRegex_Click);
            // 
            // _btnAddRegex
            // 
            this._btnAddRegex.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnAddRegex.Location = new System.Drawing.Point(4, 84);
            this._btnAddRegex.Name = "_btnAddRegex";
            this._btnAddRegex.TabIndex = 1;
            this._btnAddRegex.Text = "Add...";
            this._btnAddRegex.Click += new System.EventHandler(this._btnAddRegex_Click);
            // 
            // _lbxRegexes
            // 
            this._lbxRegexes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lbxRegexes.Location = new System.Drawing.Point(4, 23);
            this._lbxRegexes.Name = "_lbxRegexes";
            this._lbxRegexes.Size = new System.Drawing.Size(408, 56);
            this._lbxRegexes.TabIndex = 0;
            // 
            // SccOptionsPane
            // 
            this.Controls.Add(this.grpRegexes);
            this.Controls.Add(this.grpRepositories);
            this.Controls.Add(this._chkHideUnchanged);
            this.Controls.Add(this._udPollInterval);
            this.Controls.Add(this.label4);
            this.Controls.Add(this._udInitialSync);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "SccOptionsPane";
            this.Size = new System.Drawing.Size(428, 336);
            ((System.ComponentModel.ISupportInitialize)(this._udInitialSync)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._udPollInterval)).EndInit();
            this.grpRepositories.ResumeLayout(false);
            this.grpRegexes.ResumeLayout(false);
            this.ResumeLayout(false);

        }
	    #endregion

	    public override void ShowPane()
	    {
	        _udInitialSync.Value      = Settings.ChangeSetsToIndex;
	        _udPollInterval.Value     = Settings.PollInterval;
	        _chkHideUnchanged.Checked = Settings.HideUnchangedFiles;
	        RefreshRepositoryList( null );
	        RefreshRegexList( null );
	    }

	    public override void OK()
	    {
	        Settings.ChangeSetsToIndex  = (int) _udInitialSync.Value;
	        Settings.PollInterval       = (int) _udPollInterval.Value;
	        Settings.HideUnchangedFiles = _chkHideUnchanged.Checked;
	    }

	    private void _btnAddRepository_Click(object sender, EventArgs e)
	    {
	        if ( _repositoryTypesMenu == null )
	        {
	            _repositoryTypesMenu = new ContextMenu();
                foreach( RepositoryType repType in SccPlugin.RepositoryTypes )
                {
                    _repositoryTypesMenu.MenuItems.Add( repType.Name, new EventHandler( DoAddRepository ) );
                }
	        }
	        _repositoryTypesMenu.Show( this, new Point( _btnAddRepository.Left+4, _btnAddRepository.Bottom ) );
	    }

	    private void DoAddRepository( object sender, EventArgs e )
	    {
	        string repTypeId = null;
	        MenuItem senderItem = (MenuItem) sender;
	        foreach( RepositoryType repType in SccPlugin.RepositoryTypes )
	        {
	            if ( repType.Name == senderItem.Text )
	            {
	                repTypeId = repType.Id;
	                break;
	            }
	        }
	        if ( repTypeId == null )
	        {
	            return;
	        }
	        
	        ResourceProxy proxy = ResourceProxy.BeginNewResource( Props.RepositoryResource );
	        proxy.SetProp( Core.Props.Name, "<unnamed>" );
	        proxy.SetProp( Props.RepositoryType, repTypeId );
	        proxy.AddLink( Core.Props.Parent, Core.ResourceTreeManager.GetRootForType( Props.RepositoryResource ) );
	        proxy.EndUpdate();
	        
	        SccPlugin.GetRepositoryType( repTypeId ).EditRepository( this, proxy.Resource );
	        
	        RefreshRepositoryList( proxy.Resource );
	    }

	    private void RefreshRepositoryList( IResource itemToSelect )
	    {
	        _lbxRepositories.Items.Clear();
	        IResourceList repList = Core.ResourceStore.GetAllResources( Props.RepositoryResource );
	        repList.Sort( new SortSettings( Core.Props.Name, true ) );
	        foreach( IResource res in repList )
	        {
	            RepositoryItem item = new RepositoryItem( res );
	            _lbxRepositories.Items.Add( item );
	            if ( res == itemToSelect )
	            {
	                _lbxRepositories.SelectedItem = item;
	            }
	        }
	        if ( _lbxRepositories.SelectedItem == null && _lbxRepositories.Items.Count > 0 )
	        {
	            _lbxRepositories.SelectedItem = _lbxRepositories.Items [0];
	        }
	        UpdateRepositoryButtons();
	    }

        private void _lbxRepositories_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateRepositoryButtons();
        }

        private void UpdateRepositoryButtons()
	    {
	        _btnEditRepository.Enabled = _btnRemoveRepository.Enabled = (_lbxRepositories.SelectedItem != null);
	    }

        private void _btnRemoveRepository_Click( object sender, EventArgs e )
        {
            RepositoryItem item = (RepositoryItem) _lbxRepositories.SelectedItem;
            if ( DeleteRepositoryAction.DeleteRepository( this, item.Resource, false ) )
            {
                RefreshRepositoryList( null );
            }
        }
	    
	    private void _btnEditRepository_Click( object sender, EventArgs e )
	    {
	        RepositoryItem item = (RepositoryItem) _lbxRepositories.SelectedItem;
	        RepositoryType repType = SccPlugin.GetRepositoryType( item.Resource );
	        repType.EditRepository( this, item.Resource );
	        RefreshRepositoryList( item.Resource );
	    }

	    private void _btnAddRegex_Click( object sender, System.EventArgs e )
	    {
	        using( RegexOptions dlg = new RegexOptions() )
	        {
	            if ( dlg.ShowDialog( this ) == DialogResult.OK )
	            {
	                LinkRegex linkRegex = LinkRegex.Create();
	                linkRegex.RegexMatch = dlg.RegexMatch;
	                linkRegex.RegexReplace = dlg.RegexReplace;
                    linkRegex.Save();
	                RefreshRegexList( linkRegex );
	            }
	        }
	    }

	    private void RefreshRegexList( LinkRegex itemToSelect )
	    {
	        _lbxRegexes.Items.Clear();
	        BusinessObjectList<LinkRegex> repList = Core.ResourceStore.GetAllResources( LinkRegex.ResourceType );
            foreach( LinkRegex res in repList )
	        {
	            _lbxRegexes.Items.Add( res );
	        }
            if (itemToSelect != null)
            {
                _lbxRegexes.SelectedItem = itemToSelect;
            }
	        if ( _lbxRegexes.SelectedItem == null && _lbxRegexes.Items.Count > 0 )
	        {
	            _lbxRegexes.SelectedItem = _lbxRegexes.Items [0];
	        }
	        UpdateRegexButtons();
	    }

	    private void UpdateRegexButtons()
	    {
	        _btnEditRegex.Enabled = _btnRemoveRegex.Enabled = (_lbxRegexes.SelectedItem != null);
	    }

	    private void _btnRemoveRegex_Click(object sender, EventArgs e)
	    {
	        LinkRegex item = (LinkRegex) _lbxRegexes.SelectedItem;
	        item.Delete();
	        RefreshRegexList( null );
	    }

	    private void _btnEditRegex_Click( object sender, EventArgs e )
	    {
	        LinkRegex item = (LinkRegex) _lbxRegexes.SelectedItem;
	        using( RegexOptions dlg = new RegexOptions() )
	        {
	            dlg.RegexMatch = item.RegexMatch;
	            dlg.RegexReplace = item.RegexReplace;
	            if ( dlg.ShowDialog( this ) == DialogResult.OK )
	            {
	                item.RegexMatch = dlg.RegexMatch;
	                item.RegexReplace = dlg.RegexReplace;
	                item.Save();
	                RefreshRegexList( item );
	            }
	        }
	    }

	    private class RepositoryItem
	    {
	        private readonly IResource _resource;

	        public RepositoryItem( IResource res )
	        {
	            _resource = res;
	        }

	        public IResource Resource
	        {
	            get { return _resource; }
	        }

	        public override string ToString()
	        {
	            return _resource.GetProp( Core.Props.Name ) + " (" +
	                   SccPlugin.GetRepositoryType( _resource ).Name + ")";
	        }
	    }
	}

    partial class LinkRegex
    {
        public override string ToString()
        {
            return RegexMatch + " -> " + RegexReplace;
        }
    }
}
