// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea
{
    /**
     * A temporary storage for resources that can be used, for example, for drag & drop
     * operations between tabs.
     */

	public class ResourceClipboardForm : DialogBase, IContextProvider
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private IResourceList _contents;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label _lblHint;
        private ResourceListView2 _lvResources;
        private JetListViewColumn _nameColumn;
        private IResource         _root;

        private static ResourceClipboardForm _theInstance;

        private const string    _ClipboardResourceType = "ClipboardRootResource";

		public ResourceClipboardForm()
		{
            Core.ResourceAP.RunJob( new MethodInvoker( Registerer ) );

            InitializeComponent();
            InitializeList();

            _contents = Core.ResourceStore.EmptyResourceList;
            _theInstance = this;
		}

        private void  InitializeList()
        {
            _lvResources.AllowColumnReorder = false;
            _lvResources.Columns.Add( new ResourceIconColumn() );
            _nameColumn = _lvResources.AddColumn( ResourceProps.DisplayName );
            _nameColumn.AutoSize = true;
            _lvResources.ContextProvider = this;
        }

        /// <summary>
        /// Register an abstract resource type for a virtual root resource which
        /// serves as D'n'D helper resource for handling dropping and permutations.
        /// </summary>
        private void  Registerer()
        {
            IResourceTypeCollection types = Core.ResourceStore.ResourceTypes;
            if( !types.Exist( _ClipboardResourceType ) )
            {
                Core.ResourceStore.ResourceTypes.Register( _ClipboardResourceType, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            }
            IResourceList roots = Core.ResourceStore.GetAllResources( _ClipboardResourceType );
            if( roots.Count == 0 )
            {
                _root = Core.ResourceStore.BeginNewResource( _ClipboardResourceType );
                _root.SetProp( Core.Props.Name, "fake" );
                _root.EndUpdate();
            }
            else
                _root = roots[ 0 ];
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
            this._lvResources = new GUIControls.ResourceListView2();
            this.panel1 = new System.Windows.Forms.Panel();
            this._lblHint = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            //
            // _lvResources
            //
            this._lvResources.AllowDrop = true;
            this._lvResources.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lvResources.EmptyDropHandler = new DnDHandler( this );
            this._lvResources.FullRowSelect = true;
            this._lvResources.RootResource = _root;
            this._lvResources.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._lvResources.HideSelection = false;
            this._lvResources.Location = new System.Drawing.Point(0, 18);
            this._lvResources.Name = "_lvResources";
            this._lvResources.Size = new System.Drawing.Size(356, 72);
            this._lvResources.TabIndex = 0;
            this._lvResources.KeyDown += new System.Windows.Forms.KeyEventHandler(this._lvResources_KeyDown);
            this._lvResources.ResourceDrop += new GUIControls.ResourceDragEventHandler(this._lvResources_ResourceDrop);
            this._lvResources.ResourceDragOver += new ResourceDragEventHandler( _lvResources_ResourceDragOver );
            //
            // panel1
            //
            this.panel1.Controls.Add(this._lblHint);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(356, 18);
            this.panel1.TabIndex = 1;
            //
            // _lblHint
            //
            this._lblHint.AllowDrop = true;
            this._lblHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblHint.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblHint.Location = new System.Drawing.Point(4, 2);
            this._lblHint.Name = "_lblHint";
            this._lblHint.Size = new System.Drawing.Size(348, 23);
            this._lblHint.TabIndex = 0;
            this._lblHint.Text = "Drop resources to add them to the clipboard";
            this._lblHint.DragEnter += new System.Windows.Forms.DragEventHandler(this._lblHint_DragEnter);
            this._lblHint.DragDrop += new System.Windows.Forms.DragEventHandler(this._lblHint_DragDrop);
            //
            // ResourceClipboardForm
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(356, 90);
            this.Controls.Add(this._lvResources);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "ResourceClipboardForm";
            this.Text = "Resource Clipboard";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.ResourceClipboardForm_Closing);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        public IResourceList ClipboardContents
        {
            get { return _contents; }
            set
            {
                _contents = value;
                _lvResources.JetListView.Nodes.Clear();
                foreach( IResource res in _contents )
                    _lvResources.JetListView.Nodes.Add( res );
                UpdateStatusHint();
            }
        }

        private void ResourceClipboardForm_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            Visible = false;
            e.Cancel = true;
        }

        private void _lvResources_ResourceDragOver( object sender, ResourceDragEventArgs e )
        {
            e.Effect = DragDropEffects.Link;
        }
        private void _lvResources_ResourceDrop( object sender, ResourceDragEventArgs e )
        {
            IResourceList list = e.DroppedResources;
            AddResourceList( list );
        }

	    public void AddResourceList( IResourceList dropResList )
	    {
            // check if any resources in _contents have been deleted (#4310)
            IntArrayList contentsList = new IntArrayList( _contents.ResourceIds );

            for( int i=0; i<dropResList.Count; i++ )
            {
                if ( contentsList.IndexOf( dropResList.ResourceIds [i] ) < 0 )
                {
                    contentsList.Add( dropResList.ResourceIds [i] );
                }
            }

            for( int i=contentsList.Count-1; i >= 0; i-- )
            {
                IResource content;
                try
                {
                    content = Core.ResourceStore.LoadResource( contentsList [i] );
                }
                catch( StorageException )
                {
                    contentsList.RemoveAt( i );
                    continue;
                }

                if ( content.IsTransient )
                {
                    Core.ResourceAP.QueueJob( JobPriority.Immediate,
                        new MethodInvoker( content.EndUpdate ) );
                }
            }

	        // we need a live list
            ClipboardContents = Core.ResourceStore.ListFromIds( contentsList, true );
	        AutoGrow();
	    }

	    private void _lblHint_DragEnter( object sender, System.Windows.Forms.DragEventArgs e )
        {
            if ( e.Data.GetDataPresent( typeof(IResourceList) ) )
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void _lblHint_DragDrop( object sender, System.Windows.Forms.DragEventArgs e )
        {
            IResourceList dropList = (IResourceList) e.Data.GetData( typeof(IResourceList) );
            if ( dropList != null )
            {
                AddResourceList( dropList );
            }
        }

	    private void UpdateStatusHint()
	    {
            if ( _contents.Count > 0 )
            {
                _lblHint.Text = "To link resources, drag and drop from clipboard to another resource";
            }
            else
            {
                _lblHint.Text = "Drop resources to add them to the clipboard";
            }
	    }

	    /**
         * Grows the form automatically after new resources have been dropped.
         */

        private void AutoGrow()
        {
            int count = _lvResources.JetListView.Nodes.Count;
            if ( count > 0 )
            {
                int itemsHeight = _lvResources.JetListView.GetItemBounds( _lvResources.JetListView.Nodes[ 0 ], _nameColumn ).Height * count;
                int lvHeight = _lvResources.ClientSize.Height;
                if ( itemsHeight > lvHeight && itemsHeight < 200 )
                {
                    int heightDelta = Height - _lvResources.ClientSize.Height;
                    Height = itemsHeight + heightDelta + 4;
                }
            }
        }

        private void _lvResources_KeyDown( object sender, System.Windows.Forms.KeyEventArgs e )
        {
            if ( e.KeyCode == Keys.Delete )
            {
            	e.Handled = true;
                IResourceStore store = Core.ResourceStore;
                if ( _lvResources.GetSelectedResources().Count == _lvResources.JetListView.Nodes.Count )
                {
                	ClipboardContents = store.EmptyResourceList;
                }
                else
                {
                	IntArrayList ids = IntArrayListPool.Alloc();
                    try
                    {
                        ClipboardContents = _contents.Minus( _lvResources.GetSelectedResources() );
                    }
                    finally
                    {
                        IntArrayListPool.Dispose( ids );
                    }
                }
            }
            else if ( e.KeyData == ( Keys.Control | Keys.A ) )
            {
                e.Handled = true;
                _lvResources.SelectAll();
            }
            else if ( e.KeyData == Keys.Escape )
            {
                e.Handled = true;
                Close();
            }
        }

        public static bool IsVisible()
        {
            return _theInstance != null && _theInstance.Visible;
        }

        public static void ShowResourceClipboard( IResourceList contents )
        {
            if ( _theInstance == null )
            {
                _theInstance = new ResourceClipboardForm();
                _theInstance.Owner = (Form) Core.MainWindow;
                _theInstance.RestoreSettings();
            }
            if ( contents != null )
            {
                // this ensures we have a correct live list with no deleted resources (#4329)
                _theInstance.ClipboardContents = Core.ResourceStore.EmptyResourceList;
                _theInstance.AddResourceList( contents );
                _theInstance._lvResources.SelectAll();
            }
            _theInstance.Visible = true;
        }

        public static void HideResourceClipboard()
        {
            if ( _theInstance != null )
            {
                _theInstance.Hide();
            }
        }

        public static void RemoveSelectedResources()
        {
            IResourceList selResources = _theInstance._lvResources.GetSelectedResources();
            if ( _theInstance._contents.Count == selResources.Count )
            {
                _theInstance.ClipboardContents = Core.ResourceStore.EmptyResourceList;
            }
            else
            {
                _theInstance.ClipboardContents = _theInstance._contents.Minus( selResources );
            }
        }

        public IActionContext GetContext( ActionContextKind kind )
        {
            return new ActionContext( kind, this, _lvResources.GetSelectedResources() );
        }
    }

    #region EmptySpace D'n'D Handler
    internal class DnDHandler : IResourceDragDropHandler
    {
        ResourceClipboardForm _form;

        public DnDHandler( ResourceClipboardForm parent )
        {
            _form = parent;
        }

        public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
        {}

        public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            if ( data.GetDataPresent( typeof( IResourceList ) ) )
            {
                return DragDropEffects.Link;
            }
            return DragDropEffects.None;
        }

        public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            IResourceList resources = data.GetData( typeof( IResourceList ) ) as IResourceList;
            if ( resources != null )
            {
                _form.AddResourceList(resources);
            }
        }
    }
    #endregion EmptySpace D'n'D Handler

    #region Menu Actions
    /**
     * Action for showing and hiding the resource clipboard window.
     */

    public class ShowResourceClipboardAction: IAction
    {
        public void Execute( IActionContext context )
        {
            if ( !ResourceClipboardForm.IsVisible() )
            {
                ResourceClipboardForm.ShowResourceClipboard( null );
            }
            else
            {
            	ResourceClipboardForm.HideResourceClipboard();
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Checked = ResourceClipboardForm.IsVisible();
        }
    }

    /**
     * Action for removing resources from the resource clipboard.
     */

    public class RemoveFromClipboardAction: IAction
    {
        public void Execute( IActionContext context )
        {
            ResourceClipboardForm.RemoveSelectedResources();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.Instance is ResourceClipboardForm) &&
                                   (context.SelectedResources.Count > 0);
        }
    }
    #endregion Menu Actions
}
