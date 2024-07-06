// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
    /**
     * The pane displaying the list of correspondents with support for
     * incremental search and category filtering.
     */

    public class CorrespondentCtrl: AbstractViewPane, IResourceSelectPane, IContextProvider
    {
        private System.ComponentModel.IContainer components;
        private IResourceList _correspondents;
        private IResource _lastCorrespondent = null;
        private IResourceList _lastResourceList = null;
        private ResourceListView2 _listContacts;
        private IResourceList _categories;
        private IResourceList _views;
        private IResourceList _addressBooks;
        private readonly ResourceNameJetFilter _nameFilter;
        private bool _forceUpdate = false;
        private bool _selectorMode = false;
        private IResourceList _initialSelection;
        private IResourceList _selectionSourceList;
        private List<int> _checkedResources;
        private bool _updatingCheckedResources = false;
        private ResourceComboBox _cmbCategory;
        private Label _lblShowCategory;
        private JetTextBox _txtFind;
        private IResource _lastWorkspace;
        private string _iniSection;
        private IResourceList _correspondentFilterList;
        private readonly Pen _borderPen = new Pen( Color.FromArgb( 88, 80, 159 ) );
        private ResourceListDataProvider _dataProvider;
        private CheckBoxColumn _checkBoxColumn;

        #region Ctor/Dtor
        public CorrespondentCtrl()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            _nameFilter = new ResourceNameJetFilter( "" );
            _listContacts.Filters.Add( _nameFilter );
            _listContacts.ContextProvider = this;
            _txtFind.EmptyText = "<start typing a name here>";
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
        #endregion Ctor/Dtor

        #region Populate
        public override void Populate()
        {
            AddressBook.Initialize();

            _cmbCategory.BeginUpdate();
            try
            {
                _cmbCategory.Items.Clear();
                LoadViews();
                LoadAddressBooks();
                LoadCategories();
            }
            finally
            {
                _cmbCategory.EndUpdate();
            }

            AttachResourceWatchHandlers();

            _listContacts.Columns.Add( new ResourceIconColumn() );
            ResourceListView2Column nameCol = _listContacts.AddColumn( ResourceProps.DisplayName );
            nameCol.SizeToContent = true;
            _listContacts.SelectionChanged += OnNewQuerySelected;

            SetComboSelection();
        }

        //---------------------------------------------------------------------
        //  Show only those address books which have their "responsible"
        //  plugins loaded.
        //---------------------------------------------------------------------
        private void  LoadAddressBooks()
        {
            _addressBooks = Core.ResourceStore.EmptyResourceList;
            IResourceList abList = Core.ResourceStore.GetAllResourcesLive( "AddressBook" );
            foreach( IResource ab in abList )
            {
                string ownerType = ab.GetStringProp( Core.Props.ContentType );
                if( ResourceTypeHelper.IsResourceTypeActive( ownerType ) )
                    _addressBooks = _addressBooks.Union( ab.ToResourceList() );
            }
            _addressBooks.Sort( new SortSettings( Core.Props.Name, false ) );

            lock( _addressBooks )
            {
                foreach( IResource res in _addressBooks )
                {
                    _cmbCategory.Items.Add( res );
                }
            }

            _addressBooks.ResourceAdded += _addressBooks_ResourceAdded;
            _addressBooks.ResourceDeleting += _addressBooks_ResourceDeleted;
        }

        //---------------------------------------------------------------------
        //  1. Select all views which have content type "Contact"
        //  2. Add artificially views "All" and "Active" if they are not
        //     registered in the system (if e.g. a user has removed them).
        //---------------------------------------------------------------------
        private void  LoadViews()
        {
            IResourceList views = Core.FilterRegistry.GetViews();
            IResourceList cviews = Core.ResourceStore.EmptyResourceList;
            lock( views )
            {
                foreach( IResource view in views )
                {
                    string type = view.GetStringProp( Core.Props.ContentType );
                    if( type != null && type.IndexOf( "Contact" ) != -1 )
                        cviews = cviews.Union( view.ToResourceList() );
                }
            }

            //  Add predefined views as strings first (if necessary), which
            //  are analyzed later in a special way.
            views = Core.ResourceStore.FindResources( FilterManagerProps.ViewResName, "DeepName", "All" );
            if( views.Count == 0 )
                _cmbCategory.Items.Add( "All" );

            views = Core.ResourceStore.FindResources( FilterManagerProps.ViewResName, "DeepName", "Active" );
            if( views.Count == 0 )
                _cmbCategory.Items.Add( "Active" );

            foreach( IResource view in cviews )
                _cmbCategory.Items.Add( view );
        }

        private void  LoadCategories()
        {
            _categories = Core.ResourceStore.FindResourcesLive( "Category", Core.Props.ContentType, "Contact" );
            _categories.Sort( new SortSettings( Core.Props.Name, false ) );

            lock( _categories )
            {
                foreach( IResource res in _categories )
                {
                    _cmbCategory.Items.Add( res );
                }
                if ( _categories.Count > 0 )
                {
                    _cmbCategory.Items.Add( "Not categorized" );
                }
            }
        }

        private void  AttachResourceWatchHandlers()
        {
            _categories.ResourceAdded += _categories_ResourceAdded;
            _categories.ResourceDeleting += _categories_ResourceDeleted;

            _views = Core.ResourceStore.GetAllResourcesLive( FilterManagerProps.ViewResName );
            _views = _views.Minus( Core.ResourceStore.FindResourcesWithPropLive( FilterManagerProps.ViewResName, "IsTrayIconFilter" ) );
            _views = _views.Minus( Core.ResourceStore.FindResourcesWithPropLive( FilterManagerProps.ViewResName, "IsFormattingFilter" ) );
            _views.ResourceAdded += _views_ResourceAdded;
            _views.ResourceChanged += _views_ResourceChanged;
            _views.ResourceDeleting += _views_ResourceDeleted;
        }

        private void  SetComboSelection()
        {
            bool foundLastView = false;
            if ( _iniSection != null )
            {
                int lastSelectedView = Core.SettingStore.ReadInt( _iniSection, "LastSelectedCorrespondentView", -1 );
                for( int i = 0; i < _cmbCategory.Items.Count; i++ )
                {
                    IResource res = _cmbCategory.Items [ i ] as IResource;
                    if ( res != null && res.Id == lastSelectedView )
                    {
                        foundLastView = true;
                        _cmbCategory.SelectedIndex = i;
                        break;
                    }
                }
            }

            if ( !foundLastView )
            {
                _cmbCategory.SelectedIndex = 1;  // Active view
            }
        }
        #endregion Populate

        #region SelectResource
        public override IResource SelectedResource
        {
            get
            {
                IResource selNode = null;
                IResourceList selection = _listContacts.GetSelectedResources();
                if ( selection.Count > 0 )
                {
                    selNode = selection [0];
                    Debug.WriteLine( "CorrespondentCtrl.SelectedNode: returning " + selNode.Id );
                }
                else
                    Debug.WriteLine( "CorrespondentCtrl.SelectedNode: returning no selection" );
                return selNode;
            }
        }

        public override bool SelectResource( IResource node, bool highlightOnly )
        {
            #region Preconditions
            if( node == null )
                throw new ArgumentNullException( "node", "CorrespondentsPane [SelectResources] - Input resource is NULL." );
            #endregion Preconditions

            Trace.WriteLine( "CorrespondentCtrl.SelectNode - selecting " + node.Id );
            _forceUpdate = true;
            if ( !_nameFilter.NameMatches( node.DisplayName ) )
            {
                _txtFind.Text = "";
                _nameFilter.FilterString = "";
            }

            bool selectSuccess = false;

            //  Fix for OM-12499, when operations are done with contacts from
            //  ResourceBrowser but Correspondents pane has not been populated
            //  yet.
            if( _dataProvider != null )
            {
                selectSuccess = _dataProvider.FindResourceNode( node );
                if ( !selectSuccess )
                {
                    if ( node.Type == "Contact" && _lastWorkspace == null &&
                        _cmbCategory.SelectedIndex != 0  )
                    {
                        // show All contacts and retry selection
                        _cmbCategory.SelectedIndex = 0;
                        selectSuccess = _dataProvider.FindResourceNode( node );
                    }
                    if ( !selectSuccess )
                    {
                        Trace.WriteLine( "CorrespondentCtrl.SelectNode: " + node.Id + " not found" );
                    }
                }
                if ( selectSuccess )
                {
                    _listContacts.Focus();
                    _listContacts.Selection.Clear();
                    return _listContacts.Selection.AddIfPresent( node );
                }
            }
            return selectSuccess;
        }
        #endregion SelectResource

        /**
         * If a workspace is active, removes the category combo and shows only the
         * contacts linked to the workspace.
         */

        public override void SetActiveWorkspace( IResource workspace )
        {
            if ( workspace == _lastWorkspace )
                return;

            _lastWorkspace = workspace;
            if ( workspace == null )
            {
                _cmbCategory.Visible = true;
                _lblShowCategory.Visible = true;
                _listContacts.Top = _cmbCategory.Top + 28;
                UpdateSelectedCategory();
            }
            else
            {
                _cmbCategory.Visible = false;
                _lblShowCategory.Visible = false;
                _listContacts.Top = _cmbCategory.Top;
                IResourceList correspondentList = workspace.GetLinksToLive( "Contact", "InWorkspace" );
                if ( _correspondentFilterList != null )
                {
                    correspondentList = correspondentList.Intersect( _correspondentFilterList, true );
                }
                ShowCorrespondents( correspondentList );
            }
            _listContacts.Height = Height - _listContacts.Top;
        }

        /**
         * Whether the pane needs to show the current selection if it is not focused.
         */

        public override bool ShowSelection
        {
            get { return !_listContacts.HideSelection; }
            set { _listContacts.HideSelection = !value; }
        }

        public void SetCorresponentFilterList( IResourceList list )
        {
            _correspondentFilterList = list;
        }

        /// <summary>
        /// The INI section in which the pane saves its settings.
        /// </summary>
        public string IniSection
        {
            get { return _iniSection; }
            set { _iniSection = value; }
        }

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this._listContacts = new ResourceListView2();
            this._cmbCategory = new JetBrains.Omea.GUIControls.ResourceComboBox();
            this._lblShowCategory = new System.Windows.Forms.Label();
            this._txtFind = new JetBrains.Omea.GUIControls.JetTextBox();
            this.SuspendLayout();
            //
            // _listQueries
            //
            this._listContacts.AllowDrop = true;
            this._listContacts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._listContacts.BorderStyle = BorderStyle.None;
            this._listContacts.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._listContacts.HideSelection = false;
            this._listContacts.Location = new System.Drawing.Point(0, 56);
            this._listContacts.Name = "_listContacts";
            this._listContacts.Size = new System.Drawing.Size(148, 94);
            this._listContacts.TabIndex = 2;
            this._listContacts.KeyNavigationCompleted += new EventHandler( HandleKeyNavigationCompleted );
            this._listContacts.DoubleClick += new HandledEventHandler(this._listQueries_DoubleClick);
            this._listContacts.MouseUp += new System.Windows.Forms.MouseEventHandler(this._listQueries_MouseUp);
            this._listContacts.KeyDown += new System.Windows.Forms.KeyEventHandler(this._listQueries_KeyDown);
            //
            // _cmbCategory
            //
            this._cmbCategory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._cmbCategory.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this._cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbCategory.Location = new System.Drawing.Point(48, 28);
            this._cmbCategory.Name = "_cmbCategory";
            this._cmbCategory.Size = new System.Drawing.Size(100, 22);
            this._cmbCategory.TabIndex = 1;
            this._cmbCategory.KeyDown += new System.Windows.Forms.KeyEventHandler(this._cmbCategory_KeyDown);
            this._cmbCategory.SelectedIndexChanged += new System.EventHandler(this.OnSelectedCategoryChanged);
            //
            // _lblShowCategory
            //
            this._lblShowCategory.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblShowCategory.Location = new System.Drawing.Point(4, 32);
            this._lblShowCategory.Name = "_lblShowCategory";
            this._lblShowCategory.Size = new System.Drawing.Size(40, 16);
            this._lblShowCategory.TabIndex = 5;
            this._lblShowCategory.Text = "Show:";
            //
            // _txtFind
            //
            this._txtFind.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._txtFind.EmptyText = null;
            this._txtFind.ForeColor = System.Drawing.Color.DarkGray;
            this._txtFind.Location = new System.Drawing.Point(0, 4);
            this._txtFind.Name = "_txtFind";
            this._txtFind.Size = new System.Drawing.Size(148, 21);
            this._txtFind.TabIndex = 0;
            this._txtFind.Text = "";
            this._txtFind.KeyDown += new System.Windows.Forms.KeyEventHandler(this._txtFind_KeyDown);
            this._txtFind.IncrementalSearchUpdated += new System.EventHandler(this.OnIncrementalSearchUpdated);
            //
            // CorrespondentCtrl
            //
            this.Controls.Add(this._cmbCategory);
            this.Controls.Add(this._lblShowCategory);
            this.Controls.Add(this._txtFind);
            this.Controls.Add(this._listContacts);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "CorrespondentCtrl";
            this.Size = new System.Drawing.Size(148, 150);
            this.ResumeLayout(false);

        }
        #endregion

        private void UpdateSelectedCategory()
        {
            IResource selectedResource = _cmbCategory.SelectedItem as IResource;
            string    view = _cmbCategory.SelectedItem as string;
            if ( _iniSection != null && selectedResource != null )
            {
                Core.SettingStore.WriteInt( _iniSection, "LastSelectedCorrespondentView", selectedResource.Id );
            }

            IResourceList correspondents;
            if ( selectedResource != null )
            {
                switch( selectedResource.Type )
                {
                    case FilterManagerProps.ViewResName: correspondents = Core.FilterEngine.ExecView( selectedResource ); break;
                    case "Category": correspondents = selectedResource.GetLinksOfTypeLive( "Contact", "Category" ); break;
                    case "AddressBook": correspondents = selectedResource.GetLinksOfTypeLive( "Contact", "InAddressBook" ); break;
                    default: throw new NotSupportedException( "Correspondence -- Not supported type discriminator: " + selectedResource.Type );
                }
            }
            else
            {
                switch( view )
                {
                    case "All": correspondents = Core.ResourceStore.GetAllResourcesLive( "Contact" ); break;
                    case "Active": correspondents = GetActiveCorrespondents(); break;
                    case "Not categorized": correspondents = GetNotCategorizedCorrespondents(); break;
                    default: throw new NotSupportedException( "Correspondence -- Not supported string discriminator: " + view );
                }
            }

            correspondents = correspondents.Intersect( _selectionSourceList, true );
            correspondents = correspondents.Intersect( _correspondentFilterList, true );
            correspondents = correspondents.Minus( Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsDeleted ) );

            ShowCorrespondents( correspondents );
        }

        /**
         * Shows the specified list of correspondents in the pane.
         */

        private void ShowCorrespondents( IResourceList correspondents )
        {
            if ( _correspondents != null )
            {
                _correspondents.Dispose();
            }
            _correspondents = correspondents;

            _dataProvider = new ResourceListDataProvider( _correspondents );
            _dataProvider.SetInitialSort( new SortSettings( ResourceProps.DisplayName, true ) );
            _listContacts.DataProvider = _dataProvider;
            if ( _initialSelection != null && _initialSelection.Count > 0 )
            {
                _listContacts.Selection.AddIfPresent( _initialSelection [0] );
            }
            else
            {
                EnsureHasSelection();
            }
            if ( _selectorMode )
            {
                UpdateCheckedResources();
            }
        }

        private static IResourceList GetActiveCorrespondents()
        {
            return Core.ResourceStore.FindResourcesInRangeLive( "Contact", Core.ContactManager.Props.LastCorrespondenceDate,
                                                                DateTime.Now.AddDays( -14 ), DateTime.MaxValue );
        }

        private static IResourceList GetNotCategorizedCorrespondents()
        {
            IResourceList allContacts = Core.ResourceStore.GetAllResourcesLive( "Contact" );
            IResourceList categorizedContacts = Core.ResourceStore.FindResourcesWithPropLive( "Contact", "Category" );
            return allContacts.Minus( categorizedContacts );
        }

        private void _listQueries_MouseUp( object sender, MouseEventArgs e )
        {
            if ( _lastCorrespondent == null )
            {
                OnNewQuerySelected( this, EventArgs.Empty );
            }
        }

        protected override void OnEnter( EventArgs e )
        {
            base.OnEnter( e );
            AsyncUpdateSelection();
        }

        protected override void OnLeave( EventArgs e )
        {
            base.OnLeave( e );
            Core.UserInterfaceAP.CancelJobs( new MethodInvoker( LazyUpdateSelection ) );
        }

        public override void AsyncUpdateSelection()
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( LazyUpdateSelection ) );
        }

        private void LazyUpdateSelection()
        {
            if ( Core.LeftSidebar.GetPane( Core.LeftSidebar.ActivePaneId ) != this )
            {
                return;
            }
            if ( Core.ResourceBrowser.OwnerResource != _listContacts.ActiveResource )
            {
                UpdateSelection();
            }
        }

        protected void OnNewQuerySelected( object sender, EventArgs e )
        {
            if ( _listContacts.ActiveResource == null )
                return;

            // workaround for bug #1595: if the selected correspondent is deleted, update list
            // even if we don't have focus
            // (is the ContainsFocus condition correct in general?)
            if ( _lastCorrespondent == null || !_lastCorrespondent.IsDeleted )
            {
                if ( !ContainsFocus && !_forceUpdate )
                    return;
            }
            if ( _selectorMode )
                return;
            if ( Core.UIManager.IsSidebarUpdating() )
                return;

            _forceUpdate = false;

            UpdateSelection();
        }

        private void HandleKeyNavigationCompleted( object sender, EventArgs e )
        {
            UpdateSelection();
        }

        private delegate void DisplayResDlgt( IResource host, IResourceList list, string caption );
        public override void UpdateSelection()
        {
            if ( _selectorMode )
            {
                return;
            }

            IResourceList selection = _listContacts.GetSelectedResources();
            if ( selection == null || selection.Count == 0 )
            {
                Core.UserInterfaceAP.RunJob( new DisplayResDlgt( DisplayResourcesInUI ),
                                             null, Core.ResourceStore.EmptyResourceList, string.Empty );
            }
            else
            {
                string caption = "Correspondence with " + selection[ 0 ].DisplayName;
                if ( selection.Count == 1 )
                {
                    //---------------------------------------------------------
                    //  Keep the optimization for the case of repeatable selection
                    //  of single items - when the selection is not changed,
                    //  give previous result.
                    //---------------------------------------------------------
                    if ( _lastCorrespondent == null || selection[ 0 ].Id != _lastCorrespondent.Id )
                    {
                        _lastCorrespondent = selection[ 0 ];
                        _lastResourceList = ContactManager.LinkedCorrespondence( selection[ 0 ] );
                        _lastResourceList.Sort( new SortSettings( Core.Props.Date, true ) );
                    }
                }
                else
                {
                    _lastResourceList = ContactManager.LinkedCorrespondence( selection[ 0 ] );
                    for( int i = 1; i < selection.Count; i++ )
                    {
                        _lastResourceList = _lastResourceList.Union( ContactManager.LinkedCorrespondence( selection[ i ] ), true );
                        caption = caption + ", " + selection[ i ].DisplayName;
                    }
                    _lastResourceList.Sort( new SortSettings( Core.Props.Date, true ) );
                    _lastCorrespondent = null;
                }
                Core.UserInterfaceAP.RunJob( new DisplayResDlgt( DisplayResourcesInUI ),
                                             selection[ 0 ], _lastResourceList, caption );
            }
        }

        private static void DisplayResourcesInUI( IResource host, IResourceList list, string caption )
        {
            Core.ResourceBrowser.DisplayResourceList( host, list, caption, null );
            if( host != null && list.Count > 0 )
                Core.ResourceBrowser.ShowSeeAlsoBar( list, true );
        }

        private void OnIncrementalSearchUpdated( object sender, EventArgs e )
        {
            _nameFilter.FilterString = _txtFind.Text;
            EnsureHasSelection();
        }

        private void EnsureHasSelection()
        {
            INodeCollection nodeCollection = _listContacts.JetListView.NodeCollection;
            if ( _listContacts.Selection.Count == 0 && nodeCollection.VisibleItemCount > 0 )
            {
                _listContacts.Selection.Add( nodeCollection.FirstVisibleNode.Data );
            }
        }

        private void OnSelectedCategoryChanged( object sender, EventArgs e )
        {
            UpdateSelectedCategory();
        }

        #region Live Lists Changes Handlers
        private delegate void TypedResDelegate( IResource res, string type );

        //---------------------------------------------------------------------
        //  Categories
        //---------------------------------------------------------------------
        private void _categories_ResourceAdded( object sender, ResourceIndexEventArgs e )
        {
            Core.UIManager.QueueUIJob( new TypedResDelegate( AddResource ), e.Resource, "Category" );
        }
        private void _categories_ResourceDeleted( object sender, ResourceIndexEventArgs e )
        {
            Core.UIManager.QueueUIJob( new TypedResDelegate( DeleteResource ), e.Resource, "Category" );
        }

        //---------------------------------------------------------------------
        //  Views
        //---------------------------------------------------------------------
        private void _views_ResourceAdded( object sender, ResourceIndexEventArgs e )
        {
            string type = e.Resource.GetStringProp( Core.Props.ContentType );
            if( type != null && type.IndexOf( "Contact" ) != -1 )
            {
                Core.UIManager.QueueUIJob( new TypedResDelegate( AddResource ),
                                           e.Resource, FilterManagerProps.ViewResName );
            }
        }

        private void _views_ResourceDeleted( object sender, ResourceIndexEventArgs e )
        {
            string type = e.Resource.GetStringProp( Core.Props.ContentType );
            if( type != null && type.IndexOf( "Contact" ) != -1 )
            {
                Core.UIManager.QueueUIJob( new TypedResDelegate( DeleteResource ),
                                           e.Resource, FilterManagerProps.ViewResName );
            }
        }

        private void _views_ResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            string type = e.Resource.GetStringProp( Core.Props.ContentType );
            bool   isStillContact = (type != null && type.IndexOf( "Contact" ) != -1 );

            for( int i = 0; i < _cmbCategory.Items.Count; i++ )
            {
                object o = _cmbCategory.Items[ i ];
                if( o is IResource )
                {
                    IResource item = (IResource) o;
                    if( item != null && item.Id == e.Resource.Id )
                    {
                        if( !isStillContact )
                            Core.UIManager.QueueUIJob( new TypedResDelegate( DeleteResource ),
                                                       e.Resource, FilterManagerProps.ViewResName );
                        return;
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        //  Address Books
        //---------------------------------------------------------------------
        private void _addressBooks_ResourceAdded( object sender, ResourceIndexEventArgs e )
        {
            Core.UIManager.QueueUIJob( new TypedResDelegate( AddResource ), e.Resource, "AddressBook" );
        }

        private void _addressBooks_ResourceDeleted( object sender, ResourceIndexEventArgs e )
        {
            Core.UIManager.QueueUIJob( new TypedResDelegate( DeleteResource ), e.Resource, "AddressBook" );
        }

        //---------------------------------------------------------------------
        //  Impl
        //---------------------------------------------------------------------
        private void AddResource( IResource res, string type )
        {
            for( int i = 0; i < _cmbCategory.Items.Count; i++ )
            {
                if( _cmbCategory.Items[ i ] is IResource )
                {
                    IResource item = (IResource) _cmbCategory.Items[ i ];
                    if( item != null && item.Type == type )
                    {
                        _cmbCategory.Items.Insert( i, res );
                        return;
                    }
                }
            }
            _cmbCategory.Items.Add( res );
        }

        private void DeleteResource( IResource res, string type )
        {
            string name = res.GetStringProp( Core.Props.Name );
            for( int i = 0; i < _cmbCategory.Items.Count; i++ )
            {
                if( _cmbCategory.Items[ i ] is IResource )
                {
                    IResource item = (IResource) _cmbCategory.Items[ i ];
                    string    itemName = item.GetStringProp( Core.Props.Name );
                    if( item != null && item.Type == type && itemName == name )
                    {
                        _cmbCategory.Items.RemoveAt( i );
                        break;
                    }
                }
            }
        }
        #endregion Live Lists Changes Handlers

        private void _txtFind_KeyDown(object sender, KeyEventArgs e)
        {
            if ( e.KeyCode == Keys.Up  )
            {
                _listContacts.Selection.MoveUp();
                e.Handled = true;
            }
            else if ( e.KeyCode == Keys.Down )
            {
                _listContacts.Selection.MoveDown();
                e.Handled = true;
            }
            else if ( e.KeyCode == Keys.Enter )
            {
                EnsureHasSelection();
                if ( _listContacts.ActiveResource != null )
                {
                    UpdateSelection();
                    Core.ResourceBrowser.FocusResourceList();
                    e.Handled = true;
                }
            }
            else if ( e.KeyCode == Keys.Escape )
            {
                _txtFind.Text = "";
                e.Handled = true;
            }
        }

        private void _cmbCategory_KeyDown(object sender, KeyEventArgs e)
        {
            ActionContext context = new ActionContext( ActionContextKind.Keyboard, null,
                                                       _listContacts.GetSelectedResources() );
            if( Core.ActionManager.ExecuteKeyboardAction( context, e.KeyData ) )
            {
                e.Handled = true;
            }
        }

        private void _listQueries_KeyDown( object sender, KeyEventArgs e )
        {
            if ( Char.IsUpper( (Char) e.KeyCode ) && e.Modifiers == 0 )
            {
                _txtFind.Focus();
                _txtFind.Text = new string( Char.ToLower( (Char) e.KeyCode ), 1 );
                _txtFind.SelectionStart = 1;
                e.Handled = true;
            }
        }

        #region IResourceSelectPane Members
        public event EventHandler Accept;

        public void SelectResource( string[] resTypes, IResourceList baseList, IResource selection )
        {
            SetSelectorMode();
            _checkedResources = new List<int>();
            _selectionSourceList = baseList;

            Populate();
            SelectResource( selection, false );
        }

        public void SelectResources( string[] resTypes, IResourceList baseList, IResourceList selection )
        {
            SetSelectorMode();
            _checkedResources = new List<int>();
            _selectionSourceList = baseList;

            _checkBoxColumn = new CheckBoxColumn();
            _checkBoxColumn.AfterCheck += HandleAfterCheck;
            _listContacts.Columns.Insert( 0, _checkBoxColumn );

            if ( selection != null )
            {
                foreach( IResource res in selection )
                {
                    _checkedResources.Add( res.Id );
                }
            }
            _initialSelection = selection;

            Populate();
        }

        public IResourceList GetSelection()
        {
            if ( _checkBoxColumn != null )
            {
                return Core.ResourceStore.ListFromIds( _checkedResources.ToArray(), false );
            }
            return _listContacts.GetSelectedResources();
        }

        private void SetSelectorMode()
        {
            _iniSection = "ContactSelector";
            _selectorMode = true;
            _listContacts.ExecuteDoubleClickAction = false;
            _listContacts.ShowContextMenu = false;
            _listContacts.BorderStyle = BorderStyle.Fixed3D;
        }

        private void _listQueries_DoubleClick( object sender, HandledEventArgs e )
        {
            if ( _selectorMode && Accept != null )
            {
                e.Handled = true;
                Accept( this, EventArgs.Empty );
            }
        }

        /// <summary>
        /// Restores the checked state of resources after a different category
        /// has been selected.
        /// </summary>
        private void UpdateCheckedResources()
        {
            _updatingCheckedResources = true;
            try
            {
                foreach( int id in _checkedResources )
                {
                    IResource res = Core.ResourceStore.TryLoadResource( id );
                    if ( res != null & _listContacts.JetListView.NodeCollection.Contains( res ) )
                    {
                        _checkBoxColumn.SetItemCheckState( res, CheckBoxState.Checked );
                    }
                }
            }
            finally
            {
                _updatingCheckedResources = false;
            }
        }

        /// <summary>
        /// When the item is checked or unchecked, adds or deletes it from the list
        /// of checked resources.
        /// </summary>
        private void HandleAfterCheck( object sender, CheckBoxEventArgs e )
        {
            if ( _updatingCheckedResources )
                return;

            IResource res = (IResource) e.Item;

            if ( e.NewState == CheckBoxState.Checked )
            {
                _checkedResources.Add( res.Id );
            }
            else
            {
                _checkedResources.Remove( res.Id );
            }
        }
        #endregion

        #region IContextProvider Members
        public IActionContext GetContext( ActionContextKind kind )
        {
            return new ActionContext( kind, this, _listContacts.GetSelectedResources() );
        }
        #endregion IContextProvider Members

        protected override void OnPaint( PaintEventArgs e )
        {
            base.OnPaint( e );
            if ( !_selectorMode )
            {
                e.Graphics.DrawLine( _borderPen, 0, _listContacts.Top-1, ClientRectangle.Width-1, _listContacts.Top-1 );
            }
        }
    }
}
