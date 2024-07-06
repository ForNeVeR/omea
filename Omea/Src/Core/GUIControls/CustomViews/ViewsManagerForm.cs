// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using GUIControls.CustomViews;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls.CustomViews
{
    public delegate void LinkingDelegate( IResource view, IResource folder );
	/// <summary>
	/// Summary description for FilterViewsMainForm.
	/// </summary>
	public class ViewsManagerForm : DialogBase
	{
        private Label      topLabel;
        private TabControl tabsViews;
        private ResourceListView2  viewsTree;

        private Button     newButton;
        private Button     newFolderButton;
        private Button     removeButton;
        private Button     editButton;
        private Button     copyButton;
        private Button     okButton;
        private Button     cancelButton;
        private Button     helpButton;

        private readonly ArrayList   AddedViews = new ArrayList();
        private readonly ArrayList   AddedFolders = new ArrayList();
        private readonly Hashtable   RemovedViews = new Hashtable();
        private readonly Hashtable   RemovedFolders = new Hashtable();
        private readonly Hashtable   SavedParents = new Hashtable();

	    private RuleDecorator _decorator;
	    private IResourceList _viewsWithErrors;
        private readonly IResourceStore _store;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        #region Ctor and Initialization
        public ViewsManagerForm()
		{
            _store = Core.ResourceStore;
            InitializeDecorator();
			InitializeComponent();

            SaveParents();
            RestoreSettings();
            UpdateButtonsState();
		}

        private void InitializeDecorator()
        {
            _decorator = new RuleDecorator();
            _viewsWithErrors = _store.FindResourcesWithPropLive( null, "LastError" );
            _viewsWithErrors.ResourceDeleting += _decorator.OnErrorRuleChanged;
        }

        private void  SaveParents()
        {
            IResourceList list = _store.GetAllResources( FilterManagerProps.ViewResName );
            list = list.Union( _store.GetAllResources( FilterManagerProps.ViewFolderResName ));
            foreach( IResource res in list )
            {
                IResourceList parents = res.GetLinksFrom( null, Core.Props.Parent );
                if( parents.Count > 0 )
                    SavedParents[ res.GetStringProp( Core.Props.Name ) ] = parents[ 0 ];
            }
        }
        #endregion Ctor and Initialization

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
            this.topLabel = new System.Windows.Forms.Label();
            this.tabsViews = new System.Windows.Forms.TabControl();
            this.newButton = new System.Windows.Forms.Button();
            this.newFolderButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.editButton = new System.Windows.Forms.Button();
            this.copyButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.helpButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // topLabel
            //
            this.topLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.topLabel.Location = new System.Drawing.Point(8, 8);
            this.topLabel.Name = "topLabel";
            this.topLabel.Size = new System.Drawing.Size(80, 16);
            this.topLabel.TabIndex = 0;
            this.topLabel.Text = "Available views";
            //
            // tabViews
            //
            this.tabsViews.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.tabsViews.Location = new System.Drawing.Point(4, 28);
            this.tabsViews.Name = "tabsViews";
            this.tabsViews.SelectedIndex = 0;
            this.tabsViews.Size = new System.Drawing.Size(240, 284);
            this.tabsViews.TabIndex = 11;
            this.tabsViews.SelectedIndexChanged += new EventHandler(tabViews_SelectedIndexChanged);

            #region Tab Pages Creation
            TabPage pageAll = CreateTab( "General", new ExclusiveTypedViewsFilter( RemovedViews ) );
            tabsViews.Controls.Add( pageAll );
            viewsTree = (ResourceListView2) pageAll.Tag; //  first as default

            //  Collect all resource types whose views are exclusive
            //  and create a separate tab page out of each of them.

            IResourceList allTypes = Core.ResourceStore.GetAllResources( "ResourceType" );
            foreach( IResource type in allTypes )
            {
                string name = type.GetStringProp( Core.Props.Name );
                if( !String.IsNullOrEmpty( name ) &&
                    ResourceTypeHelper.IsBaseResourceTypeActive( name ) &&
                    Core.ResourceTreeManager.AreViewsExclusive( name ) )
                {
                    TabPage page = CreateTab( name + "s", new TypedViewsFilter( name, RemovedViews ) );
                    tabsViews.Controls.Add( page );
                }
            }
            #endregion Tab Pages Creation

            //
            // newButton
            //
            this.newButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.newButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.newButton.Size = new Size(80, 24);
            this.newButton.Location = new System.Drawing.Point(255, 48);
            this.newButton.Name = "newButton";
            this.newButton.TabIndex = 20;
            this.newButton.Text = "&New View...";
            this.newButton.Click += new System.EventHandler(this.newButton_Click);
            //
            // newFolderButton
            //
            this.newFolderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.newFolderButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.newFolderButton.Size = new Size(80, 24);
            this.newFolderButton.Location = new System.Drawing.Point(255, 80);
            this.newFolderButton.Name = "newFolderButton";
            this.newFolderButton.TabIndex = 30;
            this.newFolderButton.Text = "New &Folder...";
            this.newFolderButton.Click += new EventHandler(newFolderButton_Click);
            //
            // editButton
            //
            this.editButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.editButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.editButton.Size = new Size(80, 24);
            this.editButton.Location = new System.Drawing.Point(255, 112);
            this.editButton.Name = "editButton";
            this.editButton.TabIndex = 40;
            this.editButton.Text = "&Edit...";
            this.editButton.Click += new System.EventHandler(this.editButton_Click);
            //
            // copyButton
            //
            this.copyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.copyButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.copyButton.Size = new Size(80, 24);
            this.copyButton.Location = new System.Drawing.Point(255, 144);
            this.copyButton.Name = "copyButton";
            this.copyButton.TabIndex = 50;
            this.copyButton.Text = "&Copy View";
            this.copyButton.Click += new EventHandler(copyButton_Click);
            //
            // removeButton
            //
            this.removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.removeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.removeButton.Size = new Size(80, 24);
            this.removeButton.Location = new System.Drawing.Point(255, 176);
            this.removeButton.Name = "removeButton";
            this.removeButton.TabIndex = 60;
            this.removeButton.Text = "&Delete...";
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            //
            // okButton
            //
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.okButton.Location = new System.Drawing.Point(95, 322);
            this.okButton.Name = "okButton";
            this.okButton.TabIndex = 60;
            this.okButton.Text = "OK";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelButton.Location = new System.Drawing.Point(175, 322);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.TabIndex = 70;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            //
            // helpButton
            //
            this.helpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.helpButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.helpButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.helpButton.Location = new System.Drawing.Point(255, 322);
            this.helpButton.Name = "helpButton";
            this.helpButton.TabIndex = 80;
            this.helpButton.Text = "Help";
            this.helpButton.Click += new EventHandler(helpButton_Click);
            //
            // ViewsManagerForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(340, 356);
            this.MinimumSize = new System.Drawing.Size(250, 300);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.newButton);
            this.Controls.Add(this.newFolderButton);
            this.Controls.Add(this.tabsViews);
            this.Controls.Add(this.topLabel);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.editButton);
            this.Controls.Add(this.copyButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.helpButton);
            this.KeyPreview = true;
            this.Name = "ViewsManagerForm";
            this.Text = "Views Manager";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyDownHandler);
            this.ResumeLayout(false);
        }

        private TabPage CreateTab( string name, ViewsFilter filter )
        {
            ResourceListView2 view = new ResourceListView2();

            TabPage page = new TabPage();
            page.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            page.Controls.Add( view );
            page.Location = new System.Drawing.Point(4, 22);
            page.Name = "tab" + name;
            page.Size = new System.Drawing.Size(232, 258);
            page.TabIndex = 2;
            page.Text = name;
            page.Tag = view;

            view.AllowDrop = true;
            view.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            view.InPlaceEdit = false;
            view.ShowContextMenu = false;
            view.Location = new System.Drawing.Point(0, 0);
            view.Name = name;
            view.Size = new System.Drawing.Size(230, 258);
            view.TabIndex = 10;
            view.OpenProperty = Core.Props.Open;

            view.AddTreeStructureColumn();
            view.AddIconColumn();
            RichTextColumn nameColumn = new RichTextColumn();
            nameColumn.SizeToContent = true;
            nameColumn.AddNodeDecorator(_decorator);
            view.Columns.Add( nameColumn );

		    view.DoubleClick += new HandledEventHandler( this.OnDoubleClicked );
            view.JetListView.SelectionStateChanged += new JetBrains.JetListViewLibrary.StateChangeEventHandler(SelectionStateChanged);
            view.RootResource = Core.ResourceTreeManager.ResourceTreeRoot;
			// TODO: why ResourceListView, not ResourceTreeViewBase?

            if( filter != null )
            {
                view.Filters.Add( filter );
                view.Tag = filter;
            }
            IResource root = Core.ResourceTreeManager.ResourceTreeRoot;
            view.DataProvider = new ResourceTreeDataProvider( root, Core.Props.Parent );

            return page;
        }
		#endregion

        #region New View
        private void  newButton_Click( object sender, EventArgs e )
        {
            string  name = tabsViews.SelectedTab.Name;
            string  type = name.Substring( 3, name.Length - 4 ); // skip "tab" prefix and plural affix 's'
            if( !Core.ResourceTreeManager.AreViewsExclusive( type ))
                type = null;
            EditViewForm constructor = new EditViewForm( type );
            if( constructor.ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                IResource  newView = _store.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, constructor.HeadingText );
                Core.ResourceTreeManager.LinkToResourceRoot( newView, 1 );

                SelectNewViewInProperTab( newView );

                if( AddedViews.IndexOf( constructor.HeadingText ) == -1 )
                    AddedViews.Add( constructor.HeadingText );
            }
            UpdateButtonsState();
            constructor.Dispose();
        }

        private void  SelectNewViewInProperTab( IResource view )
        {
            foreach( TabPage page in tabsViews.TabPages )
            {
                ResourceListView2 tree = (ResourceListView2) page.Tag;
                ViewsFilter       filter = (ViewsFilter) tree.Tag;
                if( filter.AcceptResource( view ))
                {
                    tabsViews.SelectedTab = page;
                    tree.Selection.Clear();
                    tree.Selection.Add( view );
                }
            }
        }
        #endregion New View

        #region New Folder
        private void newFolderButton_Click(object sender, EventArgs e)
        {
            string name = Core.UIManager.InputString( "Enter Name of a View Folder", "Folder Name:", "", null, this );
            if( !String.IsNullOrEmpty( name ) )
            {
                if( _store.FindResources( FilterManagerProps.ViewFolderResName, Core.Props.Name, name ).Count > 0 )
                    MessageBox.Show( this, "View Folder with such name already exists", "Name Collision", MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
                else
                {
                    //  Find a proper place for the new folder - if the current
                    //  selection is on the folder - new folder will be its
                    //  subfolder, otherwize put it under the Root.
                    IResourceList selList = viewsTree.GetSelectedResources();
                    if( selList != null && selList.Count == 1 && selList[ 0 ].Type == FilterManagerProps.ViewFolderResName )
                        Core.FilterRegistry.CreateViewFolder( name, selList[ 0 ].GetStringProp( Core.Props.Name ), 0 );
                    else
                        Core.FilterRegistry.CreateViewFolder( name, null, 0 );
                    RemovedFolders.Remove( name );
                    AddedFolders.Add( name );
                }
            }
        }
        #endregion New Folder

        #region Edit View
        private void  editButton_Click(object sender, EventArgs e)
        {
            EditCurrentView();
        }

        private void  OnDoubleClicked(object sender, HandledEventArgs e)
        {
            if( editButton.Enabled )
                EditCurrentView();
            e.Handled = true;
        }

        private void  EditCurrentView()
        {
            IResource   view = viewsTree.GetSelectedResources()[ 0 ];
            string      viewName = view.GetStringProp( Core.Props.Name );
            EditViewForm form = new EditViewForm( view );
            if( form.ShowDialog( this ) == DialogResult.OK )
            {
                //  if edited view was added within the same session - remember it,
                //  so that we still have the ability to remove it on Cancel action
                if( AddedViews.IndexOf( viewName ) != -1 )
                {
                    AddedViews.Remove( viewName ); // in the case of rename
                    AddedViews.Add( form.HeadingText );
                }

                //  Restart the view so that its content reflects changes
                //  in the parameters.
                Core.LeftSidebar.DefaultViewPane.SelectResource( view );
            }
            form.Dispose();
        }
        #endregion Edit View

        #region Remove View
        private void  removeButton_Click(object sender, EventArgs e)
        {
            IResourceList   list = viewsTree.GetSelectedResources();
            IResourceList   views = Core.ResourceStore.EmptyResourceList,
                            folders = Core.ResourceStore.EmptyResourceList;
            foreach( IResource res in list )
            {
                if( res.Type == FilterManagerProps.ViewResName )
                    views = views.Union( res.ToResourceList() );
                else
                    folders = folders.Union( res.ToResourceList() );
            }

            EnableButtons( false );
            if( views.Count > 0 )
                RemoveViewsImpl( views );
            if( folders.Count > 0 )
                RemoveFoldersImpl( folders );
            EnableButtons( true );
            UpdateButtonsState();
        }

        private void  RemoveViewsImpl( IResourceList list )
        {
            string msg = (list.Count > 1) ? "Delete all selected views?" :
                                            "Delete view \"" + list[ 0 ].DisplayName + "\"?";
            if( MessageBox.Show( msg, "Views Manager", MessageBoxButtons.YesNo, MessageBoxIcon.Question ) == DialogResult.Yes )
            {
                foreach( IResource res in list )
                    RemoveViewImpl( res );
            }
        }
        private void  RemoveFoldersImpl( IResourceList list )
        {
            foreach( IResource folder in list )
            {
                IResource parent = folder.GetLinksFrom( null, Core.Props.Parent )[ 0 ];
                IResourceList children = folder.GetLinksTo( null, Core.Props.Parent );
                for( int i = 0; i < children.Count; i++ )
                {
                    new ResourceProxy( children[ i ] ).SetProp( Core.Props.Parent, parent );
                }
                RemovedFolders[ folder.DisplayName ] = parent.DisplayName;
                new ResourceProxy( folder ).Delete();
            }
        }

        private void  RemoveViewImpl( IResource view )
	    {
            string        viewName = view.GetStringProp( Core.Props.Name );
            IResourceList parents = view.GetLinksFrom( null, Core.Props.Parent );
            Core.ResourceAP.RunJob( new ResourceDelegate( DeleteLinks ), view );

            //  differentiation has to be changed for adequate OK/Cancel
            //  dialog behavior - if we remove the view which was added in
            //  the same session, we have to remove it independently of
            //  OK/Cancel action.
            if( AddedViews.IndexOf( viewName ) != -1 )
            {
                AddedViews.Remove( viewName );
                Core.FilterRegistry.DeleteView( viewName );
            }
            else
            {
                //  For existing view - remember its parent so that we can
                //  correctly restore it upon Cancel.
                //  NB: OM-6831 - occasionally we run into the situation when parents
                //      list is empty. Though this is imposiible situation given the
                //      control flow, insert workaround and support it in Cancel
                //      handler.
                if( parents.Count > 0 )
                    RemovedViews[ view ] = parents[ 0 ];
                else
                    RemovedViews[ view ] = null;
            }
            ((ViewsFilter) viewsTree.Tag).TouchFilter();
	    }
        #endregion Remove View

        #region Copy View
        private void copyButton_Click(object sender, EventArgs e)
        {
            IResource view = viewsTree.GetSelectedResources()[ 0 ];

            //  Construct a name for a new view.
            string newName;
            newName = "Copy of " + view.DisplayName;
            IResource res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, newName );
            if( res != null )
            {
                for( int i = 2;; i++ )
                {
                    newName = "Copy of " + view.DisplayName + "(" + i + ")";
                    res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, newName );
                    if( res == null )
                        break;
                }
            }

            IResource newView = Core.FilterRegistry.CloneView( view, newName );
            Core.ResourceTreeManager.LinkToResourceRoot( newView, 0 );
            ((ResourceTreeDataProvider) viewsTree.DataProvider).SelectResource( newView );

            if( AddedViews.IndexOf( newName ) == -1 )
                AddedViews.Add( newName );
        }
        #endregion Copy View

        #region OK/Cancel
        private void okButton_Click(object sender, EventArgs e)
        {
            EnableButtons( false );

            foreach( IResource view in RemovedViews.Keys )
                Core.FilterRegistry.DeleteView( view.GetStringProp( Core.Props.Name ) );

            foreach( string str in AddedViews )
            {
                IResource res = _store.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, str );
                Debug.Assert( res != null );
            }

            DialogResult = DialogResult.OK;
            EnableButtons( true );
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            EnableButtons( false );

            foreach( string str in AddedViews )
                Core.FilterRegistry.DeleteView( str );

            //  Implement somewhat sophisticated model for folders
            //  recoverage since of unordered nature for storing removed
            //  nested folders.
            while( RemovedFolders.Count > 0 )
            {
                IEnumerator en = RemovedFolders.Keys.GetEnumerator();
                en.MoveNext();
                string folderName = (string) en.Current;
                RestoreFolder( folderName );
            }

            foreach( IResource view in RemovedViews.Keys )
            {
                //  The following check is the workaround of the bug described
                //  in removeButton_Click method.
                if( RemovedViews[ view ] != null )
                    SetLinkToParent( view, (IResource) RemovedViews[ view ] );
                else
                    Core.ResourceTreeManager.LinkToResourceRoot( view, 1 );
            }

            foreach( IResource view in RemovedViews.Keys )
            {
                IResourceList currentParents = view.GetLinksFrom( null, Core.Props.Parent );
                Debug.Assert( currentParents.Count > 0 );
            }

            foreach( string resName in SavedParents.Keys )
            {
                IResource res = _store.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, resName );
                if( res != null )
                {
                    IResource     savedParent = (IResource) SavedParents[ res.GetStringProp( Core.Props.Name ) ];
                    IResourceList currentParents = res.GetLinksFrom( null, Core.Props.Parent );
                    if( currentParents.Count > 0 && currentParents[ 0 ].Id != savedParent.Id )
                        SetLinkToParent( res, savedParent );
                }
            }

            foreach( string folderName in AddedFolders )
            {
                IResource folder = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewFolderResName, Core.Props.Name, folderName );
                if( folder != null )
                {
                    RemoveFoldersImpl( folder.ToResourceList() );
                }
            }

            EnableButtons( true );
        }

        private void  RestoreFolder( string folder )
        {
            string parent = (string) RemovedFolders[ folder ];
            if( parent != null && parent.Length == 0 )
                parent = null;

            //  Do not restore root of all folders (invisible).
            if( parent != null &&
                Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewFolderResName, Core.Props.Name, parent ) == null )
            {
                RestoreFolder( parent );
            }
            Core.FilterRegistry.CreateViewFolder( folder, parent, 0 );
            RemovedFolders.Remove( folder );
	    }
        #endregion OK/Cancel

        #region ResourceTree events support
        private void SelectionStateChanged( object sender, StateChangeEventArgs e )
        {
            IResourceList selList = viewsTree.GetSelectedResources();
            if( selList != null && selList.Count == 1 && selList[ 0 ].Type == FilterManagerProps.ViewFolderResName )
                viewsTree.InPlaceEdit = true;
            else
                viewsTree.InPlaceEdit = false;
            UpdateButtonsState();
        }

        private void KeyDownHandler( object sender, KeyEventArgs e )
        {
            if( removeButton.Enabled )
            {
                if( e.KeyCode == Keys.Delete && !e.Shift &&
                    e.Modifiers != Keys.Alt && e.Modifiers != Keys.ControlKey )
                {
                    removeButton_Click( null, null );
                }
            }
        }
        #endregion ResourceTree events support

        #region Misc
        //---------------------------------------------------------------------
        //  Misc
        //---------------------------------------------------------------------
        private void  UpdateButtonsState()
        {
            IResourceList selList = viewsTree.GetSelectedResources();
            bool anySelected = selList.Count > 0;
            bool oneSelected = selList.Count == 1;
            bool allAreViews = true;
            foreach( IResource res in selList )
            {
                allAreViews = allAreViews && (res.Type == FilterManagerProps.ViewResName);
            }

            removeButton.Enabled = anySelected;
            copyButton.Enabled = editButton.Enabled = oneSelected && allAreViews;
        }

        private static void DeleteLinks( IResource res )
        {
            res.DeleteLinks( Core.Props.Parent );
        }
        private static void SetLinkToParent( IResource newView, IResource parentRes )
        {
            ResourceProxy proxy = new ResourceProxy( newView );
            proxy.BeginUpdate();
            proxy.SetProp( Core.Props.Parent, parentRes );
            proxy.EndUpdate();
        }

        private void tabViews_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabPage page = tabsViews.SelectedTab;
            viewsTree = (ResourceListView2) page.Tag;
            UpdateButtonsState();
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "reference\\manage_views_.html" );
        }

        private void  EnableButtons( bool state )
        {
            removeButton.Enabled = newButton.Enabled = editButton.Enabled =
            okButton.Enabled = cancelButton.Enabled = state;
        }
        #endregion Misc
    }

    #region Filters
    internal class ViewsFilter : IJetListViewNodeFilter
    {
        private readonly Hashtable  _removedViews;
        public event EventHandler   FilterChanged;

        internal  ViewsFilter( Hashtable removedViews )
        {
            _removedViews = removedViews;
        }
        public virtual bool AcceptNode( JetListViewNode node )
        {
            return AcceptResource( (IResource)node.Data );
        }
        public virtual bool AcceptResource( IResource res )
        {
            string  deepName = res.GetStringProp( "DeepName" );
            string  contentType = res.GetStringProp( "ContentType" );
            string[] appTypes = (contentType == null) ? new string[ 0 ] : contentType.Split( '|' );

            //-----------------------------------------------------------------
            //  Type of an accepted resource must be either SearchView or ViewFolder
            //-----------------------------------------------------------------
            bool  resTypeConforms = res.Type == FilterManagerProps.ViewResName ||
                                    res.Type == FilterManagerProps.ViewFolderResName;

            //-----------------------------------------------------------------
            //  Resource types for which a view is defined must be "active", that
            //  is for at least one type its supporting plugin must be loaded.
            //-----------------------------------------------------------------
            bool  contentTypeConforms = contentType == null;
            foreach( string type in appTypes )
            {
                contentTypeConforms = contentTypeConforms ||
                                      ResourceTypeHelper.IsBaseResourceTypeActive( type );
            }

            bool accept = resTypeConforms && contentTypeConforms &&
                          ( deepName == null || deepName != Core.FilterRegistry.ViewNameForSearchResults );
            return accept && !_removedViews.ContainsKey( res.ToString() );
        }
        public void  TouchFilter()
        {
            if( FilterChanged != null )
                FilterChanged( this, EventArgs.Empty );
        }
    }

    internal class TypedViewsFilter : ViewsFilter
    {
        private readonly string  _checkType;

        internal TypedViewsFilter( string type, Hashtable removedViews )
                 : base( removedViews )
        {
            _checkType = type;
        }

        public override bool AcceptNode( JetListViewNode node )
        {
            return AcceptResource( (IResource) node.Data );
        }

        public override bool AcceptResource( IResource res )
        {
            bool accept = base.AcceptResource( res );
            bool typeConformant = false;

            if( res.Type == FilterManagerProps.ViewResName )
            {
                typeConformant = (_checkType == res.GetStringProp("ContentType"));
            }
            else
            if( res.Type == FilterManagerProps.ViewFolderResName )
            {
                typeConformant = true;
                IResourceList viewsUnder = res.GetLinksTo( FilterManagerProps.ViewResName, Core.Props.Parent );
                foreach( IResource v in viewsUnder )
                    typeConformant = typeConformant && (_checkType == v.GetStringProp("ContentType"));
            }

            return accept && typeConformant;
        }

    }

    internal class ExclusiveTypedViewsFilter : ViewsFilter
    {
        internal ExclusiveTypedViewsFilter( Hashtable removedViews )
                 : base( removedViews )
        {}

        public override bool AcceptNode( JetListViewNode node )
        {
            return AcceptResource( (IResource) node.Data );
        }

        public override bool AcceptResource( IResource res )
        {
            bool accept = base.AcceptResource( res );

            if( res.Type == FilterManagerProps.ViewResName )
            {
                accept = accept && IsViewGeneral( res ) ;
            }
            else
            if( res.Type == FilterManagerProps.ViewFolderResName )
            {
                IResourceList viewsUnder = res.GetLinksTo( FilterManagerProps.ViewResName, Core.Props.Parent );
                foreach( IResource v in viewsUnder )
                {
                    accept = accept || IsViewGeneral( v );
                }
            }

            return accept;
        }

        private static bool IsViewGeneral( IResource view )
        {
            string  type = view.GetStringProp( Core.Props.ContentType );
            return (type == null || !Core.ResourceTreeManager.AreViewsExclusive( type ) );
        }
    }
    #endregion Filters
}
