// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// The default implementation of AbstractResourceSelectPane which is used when
    /// a type-specific implementation is not registered.
    /// </summary>
    public class GenericResourceSelectPane: AbstractResourceSelectPane
	{
        private TextBox _findEdit;
        private Label _lblFind;
        private IContainer components;
        private Timer _tmrIncSearch;

        private CheckBoxColumn _chkColumn;
        private ResourceListView2 _listView;
        private ResourceListDataProvider _dataProvider;
        private readonly ResourceNameJetFilter _nameFilter;

		public GenericResourceSelectPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            _nameFilter = new ResourceNameJetFilter( "" );
            _listView.Filters.Add( _nameFilter );
            _listView.FullRowSelect = true;
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
            this.components = new Container();
            this._findEdit = new TextBox();
            this._lblFind = new Label();
            this._tmrIncSearch = new Timer(this.components);
            this._listView = new ResourceListView2();
            this.SuspendLayout();
            //
            // _findEdit
            //
            this._findEdit.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left)
                | AnchorStyles.Right)));
            this._findEdit.Location = new Point(48, 0);
            this._findEdit.Name = "_findEdit";
            this._findEdit.Size = new Size(184, 22);
            this._findEdit.TabIndex = 6;
            this._findEdit.Text = "";
            this._findEdit.TextChanged += new EventHandler(this.OnSearchTextChanged);
            //
            // _lblFind
            //
            this._lblFind.FlatStyle = FlatStyle.System;
            this._lblFind.Location = new Point(8, 4);
            this._lblFind.Name = "_lblFind";
            this._lblFind.Size = new Size(32, 16);
            this._lblFind.TabIndex = 8;
            this._lblFind.Text = "Find:";
            //
            // _tmrIncSearch
            //
            this._tmrIncSearch.Interval = 300;
            this._tmrIncSearch.Tick += new EventHandler(this.OnSearchTimerTick);
            //
            // _listView
            //
            this._listView.AllowDrop = true;
            this._listView.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
                | AnchorStyles.Left)
                | AnchorStyles.Right)));
            this._listView.ExecuteDoubleClickAction = false;
            this._listView.FullRowSelect = true;
            this._listView.HeaderStyle = ColumnHeaderStyle.None;
            this._listView.HideSelection = false;
            this._listView.Location = new Point(4, 24);
            this._listView.Name = "_listView";
            this._listView.ShowContextMenu = false;
            this._listView.Size = new Size(228, 124);
            this._listView.TabIndex = 9;
			this._listView.DoubleClick += new JetBrains.JetListViewLibrary.HandledEventHandler(_listView_DoubleClick);
            //
            // GenericResourceSelectPane
            //
            this.Controls.Add(this._listView);
            this.Controls.Add(this._findEdit);
            this.Controls.Add(this._lblFind);
            this.Name = "GenericResourceSelectPane";
            this.Size = new Size(236, 150);
            this.ResumeLayout(false);

        }

        #endregion

        /// <summary>
        /// Sets the dialog to the mode for selecting a single resource.
        /// </summary>
        public override void SelectResource( string[] resTypes, IResourceList baseList, IResource selection )
        {
            _listView.AddIconColumn();
            ResourceListView2Column col = _listView.AddColumn( ResourceProps.DisplayName );
            col.Width = 20;
            col.AutoSize = true;
            _listView.MultiSelect = false;
            _dataProvider = new ResourceListDataProvider( baseList );
            _listView.DataProvider = _dataProvider;
            bool haveSelection = false;
            if ( selection != null )
            {
                haveSelection = _listView.Selection.AddIfPresent( selection );
            }
            if ( !haveSelection )
            {
                _listView.Selection.MoveDown();
            }
        }

        /**
         * Sets the dialog to the mode for selecting multiple resources.
         */

        public override void SelectResources( string[] resTypes, IResourceList baseList, IResourceList selection )
        {
            _chkColumn = _listView.AddCheckBoxColumn();
            _chkColumn.HandleAllClicks = true;
            _listView.AddIconColumn();
            ResourceListView2Column col = _listView.AddColumn( ResourceProps.DisplayName );
            col.Width = 20;
            col.AutoSize = true;

            _dataProvider = new ResourceListDataProvider( baseList );
            _listView.DataProvider = _dataProvider;
            if ( selection != null )
            {
                foreach( IResource res in selection )
                {
                    _chkColumn.SetItemCheckState( res, CheckBoxState.Checked );
                }
                if ( selection.Count > 0 )
                {
                    _listView.Selection.AddIfPresent( selection [0] );
                }
            }
        }

        /// <summary>
        /// Returns the list of resources selected in the pane.
        /// </summary>
        public override IResourceList GetSelection()
        {
            if ( _chkColumn != null )
            {
                List<int> resourceIds = new List<int>();
                foreach( IResource res in _dataProvider.ResourceList )
                {
                    if ( _chkColumn.GetItemCheckState( res ) == CheckBoxState.Checked )
                    {
                        resourceIds.Add( res.Id );
                    }
                }
                return Core.ResourceStore.ListFromIds( resourceIds, false );
            }

            if ( _listView.ActiveResource == null )
            {
                return Core.ResourceStore.EmptyResourceList;
            }
            return _listView.ActiveResource.ToResourceList();
        }

        private void OnSearchTextChanged( object sender, EventArgs e )
        {
            _tmrIncSearch.Stop();
            _tmrIncSearch.Start();
        }

        private void OnSearchTimerTick( object sender, EventArgs e )
        {
            _tmrIncSearch.Stop();
            _nameFilter.FilterString = _findEdit.Text;
        }

		private void _listView_DoubleClick(object sender, JetListViewLibrary.HandledEventArgs e)
        {
            OnAccept();
            e.Handled = true;
        }
	}
}
