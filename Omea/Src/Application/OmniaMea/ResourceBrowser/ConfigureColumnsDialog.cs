// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using JetBrains.DataStructures;

namespace JetBrains.Omea
{
    /// <summary>
    /// The dialog for selecting the columns displayed in ResourceBrowser for
    /// a specific resource list.
    /// </summary>
    public class ConfigureColumnsDialog : DialogBase
	{
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnCancel;
        private JetListView _propListView;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private IntArrayList _availableColumns;
        private DisplayColumnManager _displayColumnManager;
        private CheckBoxColumn _checkColumn;
        private CheckBoxColumn _autoSizeColumn;
        private CheckBoxColumn _showIfNotEmptyColumn;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radAllViews;
        private System.Windows.Forms.RadioButton radThisView;
        private System.Windows.Forms.Button _btnRestoreDefaults;
        private CheckBoxColumn _showIfDistinctColumn;
        private IResourceList _resourceList;
        private System.Windows.Forms.Button _btnHelp;
        private ResourceListState _state;
        private string[] _allTypes;
        private string[] _allNoFileTypes;

        public ConfigureColumnsDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            _propListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            _propListView.ControlPainter = new GdiControlPainter();

		    _checkColumn = new CheckBoxColumn();
            _propListView.Columns.Add( _checkColumn );
            JetListViewColumn nameCol = new JetListViewColumn();
            nameCol.SizeToContent = true;
            nameCol.Text = "Column";
		    _propListView.Columns.Add( nameCol );

            _checkColumn.AfterCheck += new CheckBoxEventHandler( HandleAfterCheck );

            _autoSizeColumn = new CheckBoxColumn();
            _autoSizeColumn.ShowHeader = true;
            _autoSizeColumn.Text = "Auto size";
            _autoSizeColumn.Width = (int) (80 * Core.ScaleFactor.Width);
            _propListView.Columns.Add( _autoSizeColumn );

            _showIfNotEmptyColumn = new CheckBoxColumn();
            _showIfNotEmptyColumn.ShowHeader = true;
            _showIfNotEmptyColumn.Text = "Show if not empty";
            _showIfNotEmptyColumn.Width = (int) (120 * Core.ScaleFactor.Width);
            _propListView.Columns.Add( _showIfNotEmptyColumn );

            _showIfDistinctColumn = new CheckBoxColumn();
            _showIfDistinctColumn.ShowHeader = true;
            _showIfDistinctColumn.Text = "Show if distinct";
            _showIfDistinctColumn.Width = (int) (100 * Core.ScaleFactor.Width);
            _propListView.Columns.Add( _showIfDistinctColumn );
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._propListView = new JetBrains.JetListViewLibrary.JetListView();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radThisView = new System.Windows.Forms.RadioButton();
            this.radAllViews = new System.Windows.Forms.RadioButton();
            this._btnRestoreDefaults = new System.Windows.Forms.Button();
            this._btnHelp = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            //
            // _propListView
            //
            this._propListView.AllowColumnReorder = false;
            this._propListView.AllowDrop = true;
            this._propListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._propListView.BackColor = System.Drawing.SystemColors.Window;
            this._propListView.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._propListView.DragOver += new JetListViewDragEventHandler(HandleDragOver);
            this._propListView.DragDrop += new JetListViewDragEventHandler(HandleDragDrop);
            this._propListView.EmptyText = "There are no items in this view.";
            this._propListView.FullRowSelect = false;
            this._propListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._propListView.InPlaceEditor = null;
            this._propListView.ItemDrag += new ItemDragEventHandler(HandleItemDrag);
            this._propListView.Location = new System.Drawing.Point(4, 4);
            this._propListView.Name = "_propListView";
            this._propListView.Size = new System.Drawing.Size(380, 188);
            this._propListView.TabIndex = 0;
            //
            // _btnOK
            //
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(140, 276);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 1;
            this._btnOK.Text = "OK";
            //
            // _btnCancel
            //
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(224, 276);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 2;
            this._btnCancel.Text = "Cancel";
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.radThisView);
            this.groupBox1.Controls.Add(this.radAllViews);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox1.Location = new System.Drawing.Point(4, 196);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(380, 72);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Apply settings to";
            //
            // radThisView
            //
            this.radThisView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.radThisView.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radThisView.Location = new System.Drawing.Point(8, 44);
            this.radThisView.Name = "radThisView";
            this.radThisView.Size = new System.Drawing.Size(364, 20);
            this.radThisView.TabIndex = 1;
            this.radThisView.Text = "This view only";
            //
            // radAllViews
            //
            this.radAllViews.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.radAllViews.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radAllViews.Location = new System.Drawing.Point(8, 20);
            this.radAllViews.Name = "radAllViews";
            this.radAllViews.Size = new System.Drawing.Size(364, 24);
            this.radAllViews.TabIndex = 0;
            this.radAllViews.Text = "All views with resources of types";
            //
            // _btnRestoreDefaults
            //
            this._btnRestoreDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._btnRestoreDefaults.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnRestoreDefaults.Location = new System.Drawing.Point(4, 276);
            this._btnRestoreDefaults.Name = "_btnRestoreDefaults";
            this._btnRestoreDefaults.Size = new System.Drawing.Size(116, 23);
            this._btnRestoreDefaults.TabIndex = 4;
            this._btnRestoreDefaults.Text = "Restore Defaults";
            this._btnRestoreDefaults.Click += new System.EventHandler(this._btnRestoreDefaults_Click);
            //
            // _btnHelp
            //
            this._btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnHelp.Location = new System.Drawing.Point(308, 276);
            this._btnHelp.Name = "_btnHelp";
            this._btnHelp.TabIndex = 5;
            this._btnHelp.Text = "Help";
            this._btnHelp.Click += new System.EventHandler(this._btnHelp_Click);
            //
            // ConfigureColumnsDialog
            //
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(392, 306);
            this.Controls.Add(this._btnHelp);
            this.Controls.Add(this._btnRestoreDefaults);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._propListView);
            this.Name = "ConfigureColumnsDialog";
            this.Text = "Resource List Columns";
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        internal ResourceListState ConfigureColumns( ResourceListState state, IResourceList resList,
            IResource ownerResource )
        {
            RestoreSettings();
            _displayColumnManager = Core.DisplayColumnManager as DisplayColumnManager;
            _resourceList = resList;
            _allTypes = _resourceList.GetAllTypes();
            _allNoFileTypes = _displayColumnManager.CollapseFileTypes( _allTypes );
            _availableColumns = _displayColumnManager.GetAvailableColumns( _resourceList );
            _state = state;
            FillPropertyList();

            SetViewRadioButtons( _state, ownerResource );

            if ( ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                if ( _state.KeyTypes != null && radThisView.Checked )
                {
                    _state = new ResourceListState( new ColumnDescriptor[] {}, _state.SortSettings, state.GroupItems );
                    _state.SetOwner( ownerResource, Core.TabManager.CurrentTabId );
                }
                else if ( _state.OwnerResource != null && radAllViews.Checked )
                {
                    _state.Delete();
                    _state = _displayColumnManager.StateFromList( _resourceList,
                        _displayColumnManager.GetDefaultColumns( _resourceList ), true );
                }
                SaveColumnsToState( _state );
            }
            return _state;
        }

        /**
         * Fills the checklistbox with properties for the specified resource list.
         */

        private void FillPropertyList()
        {
            IResourceList allPropList = Core.ResourceStore.GetAllResources( "PropType" );
            allPropList.Sort( new SortSettings( ResourceProps.DisplayName, true ) );
            ArrayList propTypeList = new ArrayList();
            IntHashTable propTypeHash = new IntHashTable();
            foreach( IResource res in allPropList )
            {
                int propId = res.GetIntProp( "ID" );
                if ( !Core.ResourceStore.PropTypes [propId].HasFlag( PropTypeFlags.Internal ) )
                {
                    if ( StateHasProp( _state, propId ) || _availableColumns.IndexOf( propId ) >= 0 ||
                        _resourceList.HasProp( propId ) )
                    {
                        IPropType propType = Core.ResourceStore.PropTypes [propId];
                        propTypeList.Add( propType );
                        propTypeHash [propId] = propType;
                    }
                }
            }
            if ( StateHasProp( _state, ResourceProps.DisplayName ) || IsDisplayNameColumnAvailable() )
            {
                IPropType displayNamePropType = Core.ResourceStore.PropTypes [ResourceProps.DisplayName];
                propTypeList.Add( displayNamePropType );
                propTypeHash [ResourceProps.DisplayName] = displayNamePropType;
            }

            Hashtable nameToPropTagMap = new Hashtable();

            // first, add the columns already in the list, in the list order
            foreach( ColumnDescriptor colDesc in _state.Columns )
            {
                int[] propIds = _displayColumnManager.PropNamesToIDs( colDesc.PropNames, true );
                if ( propIds.Length == 1 && propIds [0] == ResourceProps.Type )
                {
                    continue;
                }

                bool[] reverseLinks = new bool [propIds.Length];
                for( int i=0; i<propIds.Length; i++ )
                {
                    reverseLinks [i] = AreLinksReverse( _resourceList, propIds [i] );
                }
                for( int i=0; i<propIds.Length; i++ )
                {
                    IPropType propType = (IPropType) propTypeHash [propIds [i]];
                    if ( propType == null )
                    {
                        propType = (IPropType) propTypeHash [-propIds [i]];
                    }
                    if ( propType != null )
                    {
                        propTypeList.Remove( propType );
                    }
                }
                PropertyTypeTag tag = AddItemForPropType( colDesc, propIds, reverseLinks, true );
                nameToPropTagMap [tag.ToString()] = tag;
            }

            AddUncheckedColumns( propTypeList, _resourceList, nameToPropTagMap );
        }

        private bool IsDisplayNameColumnAvailable()
        {
            ColumnDescriptor[] descriptors = _displayColumnManager.GetColumnsForTypes( _allTypes );
            for( int i=0; i<descriptors.Length; i++ )
            {
                if ( Array.IndexOf( descriptors [i].PropNames, "DisplayName" ) >= 0 )
                {
                    return true;
                }
            }
            return false;
        }

        private void AddUncheckedColumns( ArrayList propTypeList, IResourceList resList, Hashtable nameToPropTagMap )
        {
            foreach( IPropType propType in propTypeList )
            {
                bool linksReverse = AreLinksReverse( resList, propType.Id );
                string displayName = linksReverse ? propType.ReverseDisplayName : propType.DisplayName;
                if ( displayName == null )
                {
                    displayName = propType.Name;
                }

                PropertyTypeTag propTypeTag = (PropertyTypeTag) nameToPropTagMap [displayName];
                if ( propTypeTag == null )
                {
                    ColumnDescriptor colDesc = new ColumnDescriptor( propType.Name, 150 );
                    _displayColumnManager.FindColumnDescriptor( propType.Name, ref colDesc );
                    propTypeTag = new PropertyTypeTag( colDesc,
                        new int[] { propType.Id }, new bool[] { linksReverse }, false );
                    nameToPropTagMap [displayName] = propTypeTag;
                }
                else if ( !propTypeTag.InitialChecked )
                {
                    propTypeTag.AppendPropType( propType );
                }
            }

            ArrayList tags = new ArrayList( nameToPropTagMap.Values );
            tags.Sort();
            foreach( PropertyTypeTag tag in tags )
            {
                if ( !tag.InitialChecked )
                {
                    AddPropTypeItem( tag, false );
                }
            }
        }

        /// <summary>
        /// Sets the "For all views" and "For this view" radio buttons based on the state.
        /// </summary>
        private void SetViewRadioButtons( ResourceListState state, IResource ownerResource )
        {
            string[] resTypes = new string[ _allNoFileTypes.Length ];
            for( int i=0; i<_allNoFileTypes.Length; i++ )
            {
                // "File" is a pseudo resource type
                if ( Core.ResourceStore.ResourceTypes.Exist( _allNoFileTypes [i] ) &&
                    Core.ResourceStore.ResourceTypes [_allNoFileTypes [i]].DisplayName != null )
                {
                    resTypes [i] = Core.ResourceStore.ResourceTypes [_allNoFileTypes [i]].DisplayName;
                }
                else
                {
                    resTypes [i] = _allNoFileTypes [i];
                }
            }
            radAllViews.Text += " " + String.Join( ", ", resTypes );

            if ( state.KeyTypes != null )
            {
                radAllViews.Checked = true;
            }
            else
            {
                radThisView.Checked = true;
            }

            if ( ownerResource == null )
            {
                radThisView.Enabled = false;
            }
        }

        /**
         * Applies the columns selected in the dialog to the resource list view.
         */

        private void SaveColumnsToState( ResourceListState state )
        {
            ArrayList newColumnDescriptors = new ArrayList();
            foreach( JetListViewNode node in _propListView.Nodes )
            {
                PropertyTypeTag tag = (PropertyTypeTag) node.Data;
                if ( _checkColumn.GetItemCheckState( tag ) == CheckBoxState.Checked )
                {
                    SetFlagFromColumn( tag, _autoSizeColumn, ColumnDescriptorFlags.AutoSize );
                    SetFlagFromColumn( tag, _showIfNotEmptyColumn, ColumnDescriptorFlags.ShowIfNotEmpty );
                    SetFlagFromColumn( tag, _showIfDistinctColumn, ColumnDescriptorFlags.ShowIfDistinct );
                    newColumnDescriptors.Add( tag.ColDesc );
                }
            }
            state.Columns = (ColumnDescriptor[]) newColumnDescriptors.ToArray( typeof (ColumnDescriptor) );
        }

        private void SetFlagFromColumn( PropertyTypeTag tag, CheckBoxColumn column, ColumnDescriptorFlags flag )
        {
            if ( column.GetItemCheckState( tag ) ==  CheckBoxState.Checked )
            {
                tag.ColDesc.Flags |= flag;
            }
            else
            {
                tag.ColDesc.Flags &= ~flag;
            }
        }

        /**
         * Adds a checklistbox item for the specified property resource.
         */

        private PropertyTypeTag AddItemForPropType( ColumnDescriptor colDesc, int[] propIds, bool[] reverseLinks,
            bool isChecked )
        {
            PropertyTypeTag tag = new PropertyTypeTag( colDesc, propIds, reverseLinks, isChecked );
            AddPropTypeItem( tag, isChecked );
            return tag;
        }

        private void AddPropTypeItem( PropertyTypeTag tag, bool isChecked )
        {
            _propListView.Nodes.Add( tag );
            if ( isChecked )
            {
                _checkColumn.SetItemCheckState( tag, CheckBoxState.Checked );
            }
            UpdateCheckboxColumns( tag, isChecked );
        }

        private void UpdateCheckboxColumns( PropertyTypeTag tag, bool isChecked )
        {
            if ( ( tag.ColDesc.Flags & ColumnDescriptorFlags.FixedSize ) != 0 )
            {
                _autoSizeColumn.SetItemCheckState( tag, CheckBoxState.Grayed );
            }
            else
            {
                SetColumnFromFlag( tag, _autoSizeColumn, ColumnDescriptorFlags.AutoSize, isChecked );
            }

            SetColumnFromFlag( tag, _showIfNotEmptyColumn, ColumnDescriptorFlags.ShowIfNotEmpty, isChecked );
            SetColumnFromFlag( tag, _showIfDistinctColumn, ColumnDescriptorFlags.ShowIfDistinct, isChecked );
        }

        private void SetColumnFromFlag( PropertyTypeTag tag, CheckBoxColumn column, ColumnDescriptorFlags flag, bool isChecked )
        {
            if ( isChecked )
            {
                column.SetItemCheckState( tag,
                    ( ( tag.ColDesc.Flags & flag ) != 0 )
                    ? CheckBoxState.Checked
                    : CheckBoxState.Unchecked );
            }
            else
            {
                column.SetItemCheckState( tag, CheckBoxState.Grayed );
            }
        }

        private void HandleAfterCheck( object sender, CheckBoxEventArgs e )
        {
            PropertyTypeTag tag = (PropertyTypeTag) e.Item;
            UpdateCheckboxColumns( tag, (e.NewState == CheckBoxState.Checked ) );
        }

        /// <summary>
        /// Checks if a column displaying the specified property is contained in the specified
        /// resource list state.
        /// </summary>
        private bool StateHasProp( ResourceListState state, int propId )
        {
            string propName = Core.ResourceStore.PropTypes [propId].Name;
            foreach( ColumnDescriptor colDesc in state.Columns )
            {
                for( int i=0; i<colDesc.PropNames.Length; i++ )
                {
                    if ( colDesc.PropNames [i] == propName || colDesc.PropNames [i] == "-" + propName )
                        return true;
                }
            }
            return false;
        }

        /**
         * Checks if all the resources in the specified list have To, not From
         * links of the specified type.
         */

        private bool AreLinksReverse( IResourceList resList, int propId )
        {
            if ( Core.ResourceStore.PropTypes[ propId ].DataType != PropDataType.Link )
                return false;
            if ( !Core.ResourceStore.PropTypes[ propId ].HasFlag( PropTypeFlags.DirectedLink ) )
                return false;

            if ( propId < 0 )
            {
                return true;
            }

            bool haveFromLinks = false;
            bool haveToLinks = false;

            lock( resList )
            {
                foreach( IResource res in resList )
                {
                    if ( res.GetLinkCount( propId ) > 0 )
                        haveFromLinks = true;
                    if ( res.GetLinkCount( -propId ) > 0 )
                        haveToLinks = true;
                }
            }

            return haveToLinks && !haveFromLinks;
        }

        private void _btnRestoreDefaults_Click( object sender, System.EventArgs e )
        {
            string confirmMessage = (_state.OwnerResource != null)
                ? "Would you like to restore the default column settings for this view?"
                : "Would you like to restore the default column settings for all views with resources of these types?";

            DialogResult dr = MessageBox.Show( this, confirmMessage,
                "Configure Columns", MessageBoxButtons.YesNo );
            if ( dr == DialogResult.Yes )
            {
                _state.Columns = Core.DisplayColumnManager.GetDefaultColumns( _resourceList );
                _propListView.Nodes.Clear();
                FillPropertyList();
            }
        }

        private void _btnHelp_Click( object sender, System.EventArgs e )
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "/reference/columns_dialog.html" );
        }

        private void HandleItemDrag( object sender, ItemDragEventArgs e )
        {
            DoDragDrop( e.Item, DragDropEffects.All | DragDropEffects.Move | DragDropEffects.Link );
        }

        private void HandleDragOver( object sender, JetListViewDragEventArgs e )
        {
            if ( e.Data.GetDataPresent( typeof(JetListViewNode) ) && e.DropTargetNode != null )
            {
                e.Effect = DragDropEffects.Move;
                e.DropTargetRenderMode = DropTargetRenderMode.InsertAny;

				// TODO: remove this test code
				/*
				JetListViewNode dragNode = (JetListViewNode) e.Data.GetData( typeof(JetListViewNode ) );
				JetListViewNode dropNode = e.DropTargetNode;
				JetListView list = (JetListView)sender;
				Rectangle bounds = list.GetItemBounds( dropNode, list.Columns[0] );	// TODO: row bounds, not cell's
				Point pnt = _propListView.PointToClient( new Point( e.X, e.Y ) );
				if((pnt.Y >= bounds.Top) && (pnt.Y < bounds.Bottom))
					e.DragInsert = ((pnt.Y - bounds.Top <= bounds.Height / 4) || (bounds.Bottom - pnt.Y < bounds.Height / 4));
            	Trace.WriteLine(dropNode.ToString(), "DDR");
				*/
            }
			else
				e.Effect = DragDropEffects.None;
        }

        private void HandleDragDrop( object sender, JetListViewDragEventArgs e )
        {
            if ( e.Data.GetDataPresent( typeof(JetListViewNode) ) )
            {
                JetListViewNode dropTargetNode = e.DropTargetNode;
                JetListViewNode dragNode = (JetListViewNode) e.Data.GetData( typeof(JetListViewNode ) );
                if ( dropTargetNode != dragNode )
                {
                    Rectangle rc = _propListView.GetItemBounds( dropTargetNode, _checkColumn );
                    Point pnt = _propListView.PointToClient( new Point( e.X, e.Y ) );
                    if ( pnt.Y < (rc.Top + rc.Bottom) / 2 )
                    {
                        dropTargetNode = dropTargetNode.PrevNode;
                    }
                    _propListView.Nodes.Move( dragNode, dropTargetNode );
                }
            }
        }

        private class PropertyTypeTag: IComparable
        {
            internal ColumnDescriptor ColDesc;
            private string _name;
            private bool _initialChecked;

            internal PropertyTypeTag( ColumnDescriptor colDesc, int[] propIds, bool[] reverseLinks, bool initialChecked )
            {
                _initialChecked = initialChecked;
                ColDesc = colDesc;
                ArrayList propNames = new ArrayList();
                for( int i=0; i<propIds.Length; i++ )
                {
                    string displayName = reverseLinks [i]
                        ? Core.ResourceStore.PropTypes [propIds [i]].ReverseDisplayName
                        : Core.ResourceStore.PropTypes [propIds [i]].DisplayName;
                    if ( displayName == null )
                    {
                        displayName = Core.ResourceStore.PropTypes [propIds [i]].Name;
                    }
                    if ( !propNames.Contains( displayName ) )
                    {
                        propNames.Add( displayName );
                    }
                }
                _name = String.Join( ", ", (string[]) propNames.ToArray( typeof (string) ) );
            }

            public override string ToString()
            {
                return _name;
            }

            internal bool InitialChecked
            {
                get { return _initialChecked; }
            }

            internal void AppendPropType( IPropType type )
            {
                string[] newPropNames = new string [ColDesc.PropNames.Length+1];
                Array.Copy( ColDesc.PropNames, newPropNames, ColDesc.PropNames.Length );
                newPropNames [ColDesc.PropNames.Length] = type.Name;
                ColDesc.PropNames = newPropNames;
            }

            public int CompareTo( object obj )
            {
                return _name.CompareTo( ((PropertyTypeTag) obj)._name );
            }
        }
	}

}
