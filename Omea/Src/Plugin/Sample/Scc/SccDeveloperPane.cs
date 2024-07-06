// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
	/// <summary>
	/// The pane showing the list of Perforce developers and allowing to view
	/// the changesets from each developer.
	/// </summary>
	public class SccDeveloperPane: AbstractViewPane
	{
        private System.Windows.Forms.ListView _lvDevelopers;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.ColumnHeader colName;

        /// <summary>
        /// The list of contacts which have a Perforce user login.
        /// </summary>
        private IResourceList _developerList;

		public SccDeveloperPane()
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
            this._lvDevelopers = new System.Windows.Forms.ListView();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            //
            // _lvDevelopers
            //
            this._lvDevelopers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                            this.colName});
            this._lvDevelopers.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lvDevelopers.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._lvDevelopers.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._lvDevelopers.Location = new System.Drawing.Point(0, 0);
            this._lvDevelopers.Name = "_lvDevelopers";
            this._lvDevelopers.Size = new System.Drawing.Size(150, 150);
            this._lvDevelopers.TabIndex = 1;
            this._lvDevelopers.View = System.Windows.Forms.View.Details;
            this._lvDevelopers.Layout += new System.Windows.Forms.LayoutEventHandler(this._lvDevelopers_Layout);
            this._lvDevelopers.SelectedIndexChanged += new System.EventHandler(this._lvDevelopers_SelectedIndexChanged);
            //
            // colName
            //
            this.colName.Width = 120;
            //
            // P4DeveloperPane
            //
            this.Controls.Add(this._lvDevelopers);
            this.Name = "P4DeveloperPane";
            this.ResumeLayout(false);

        }
		#endregion

        /// <summary>
        /// Ensures that the first and only column in the listview occupies its entire width.
        /// </summary>
        private void _lvDevelopers_Layout( object sender, System.Windows.Forms.LayoutEventArgs e )
        {
            if ( _lvDevelopers.Columns.Count > 0 )
            {
                _lvDevelopers.Columns [0].Width = _lvDevelopers.Width;
            }
        }

	    /// <summary>
	    /// Called to fill the pane with initial contents.
	    /// </summary>
        public override void Populate()
	    {
            _lvDevelopers.SmallImageList = Core.ResourceIconManager.ImageList;
            _developerList = Core.ResourceStore.FindResourcesWithPropLive( "Contact", Props.UserContact );
            _developerList.Sort( new SortSettings( ResourceProps.DisplayName, true ) );
            _developerList.ResourceAdded += new ResourceIndexEventHandler( HandleDeveloperListChanged );
            _developerList.ResourceDeleting += new ResourceIndexEventHandler( HandleDeveloperListChanged );
            RefreshDeveloperList();
	    }

	    /// <summary>
	    /// Ensures that the list view selection is hidden when the pane is not active.
	    /// </summary>
        public override bool ShowSelection
	    {
	        get { return !_lvDevelopers.HideSelection; }
	        set { _lvDevelopers.HideSelection = !value; }
	    }

	    /// <summary>
	    /// Fills the list view with the contents of the developer resource list.
	    /// </summary>
        private void RefreshDeveloperList()
	    {
            _lvDevelopers.BeginUpdate();
            try
            {
                _lvDevelopers.Items.Clear();
                foreach( IResource contact in _developerList )
                {
                    ListViewItem item = _lvDevelopers.Items.Add( contact.DisplayName, Core.ResourceIconManager.GetIconIndex( contact ) );
                    item.Tag = contact;
                }
            }
            finally
            {
                _lvDevelopers.EndUpdate();
            }
	    }

        /// <summary>
        /// When the selection in the list changes, shows the list of changesets by the
        /// selected developer.
        /// </summary>
        private void _lvDevelopers_SelectedIndexChanged( object sender, System.EventArgs e )
        {
            if ( _lvDevelopers.SelectedIndices.Count == 0 )
            {
                Core.ResourceBrowser.DisplayResourceList( null, Core.ResourceStore.EmptyResourceList,
                    "", null );
            }
            else
            {
                IResource developer = (IResource) _lvDevelopers.SelectedItems [0].Tag;
                Core.ResourceBrowser.DisplayResourceList( developer,
                    developer.GetLinksOfTypeLive( Props.ChangeSetResource, Core.ContactManager.Props.LinkFrom ),
                    "Changesets by " + developer.DisplayName, null );
            }
        }

	    /// <summary>
	    /// When a Perforce developer is added or removed, rebuilds the list shown in the pane.
	    /// </summary>
	    /// <remarks>Note that a complete rebuild is in general very inefficient for such a task.
	    /// However, in this case adding or removing of developers is a rare case, and the list is small.
	    /// Moreover, a full rebuild is much easier to implement than correct marshaling of every individual
	    /// change from the resource thread to the UI thread.</remarks>
        private void HandleDeveloperListChanged( object sender, ResourceIndexEventArgs e )
	    {
            Core.UIManager.QueueUIJob( new MethodInvoker( RefreshDeveloperList ) );
	    }
	}
}
