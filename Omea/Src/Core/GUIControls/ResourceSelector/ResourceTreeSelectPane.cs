/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// An implementation of AbstractResourceSelectPane which is based on ResourceTreeView.
    /// </summary>
    public class ResourceTreeSelectPane: AbstractResourceSelectPane
    {
        protected ResourceTreeView2 _resourceTree;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private string[] _resTypes;
        private IntArrayList _checkedResources = null;
        private IResourceList _baseList;

        public ResourceTreeSelectPane()
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
            this._resourceTree = new ResourceTreeView2();
            this.SuspendLayout();
            // 
            // _resourceTree
            // 
            this._resourceTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this._resourceTree.ExecuteDoubleClickAction = false;
            this._resourceTree.Location = new System.Drawing.Point(0, 0);
            this._resourceTree.Name = "_resourceTree";
            this._resourceTree.ShowContextMenu = false;
            this._resourceTree.Size = new System.Drawing.Size(150, 150);
            this._resourceTree.TabIndex = 0;
            this._resourceTree.DoubleClick += new HandledEventHandler(this._resourceTree_DoubleClick);
            this._resourceTree.AfterCheck += new ResourceCheckEventHandler(this._resourceTree_AfterThreeStateCheck);
            this._resourceTree.ResourceAdded += new ResourceEventHandler(this._resourceTree_ResourceAdded);
            // 
            // ResourceTreeSelectPane
            // 
            this.Controls.Add(this._resourceTree);
            this.Name = "ResourceTreeSelectPane";
            this.ResumeLayout(false);

        }
        #endregion

        public override void SelectResource( string[] resTypes, IResourceList baseList, IResource selection )
        {
            Populate( resTypes [0] );
            _resourceTree.SelectResourceNode( selection );
        }

        public override void SelectResources( string[] resTypes, IResourceList baseList, IResourceList selection )
        {
            _resourceTree.CheckBoxes = true;
            _baseList = baseList;
            _resTypes = resTypes;
            _checkedResources = new IntArrayList();
            Populate( resTypes[ 0 ] );
            if ( selection != null )
            {
                foreach( IResource res in selection )
                {
                    if ( _resourceTree.DataProvider.FindResourceNode( res ) )
                    {
                        _resourceTree.SetNodeCheckState( res, CheckBoxState.Checked );
                    }
                }
            }
        }

        public override IResourceList GetSelection()
        {
            if ( _resourceTree.CheckBoxes )
            {
                return Core.ResourceStore.ListFromIds( _checkedResources.ToArray(), false );
            }
            if ( _resourceTree.ActiveResource == null )
            {
                return Core.ResourceStore.EmptyResourceList;
            }
            return _resourceTree.ActiveResource.ToResourceList();
        }

        /**
         * Fills the tree with the resources of the specified type.
         */

        private void Populate( string resType )
        {
            if ( _resourceTree.ParentProperty == -1 )
            {
                _resourceTree.ParentProperty = Core.Props.Parent;
            }
            if ( _baseList != null )
            {
                _resourceTree.AddNodeFilter( new ResourceListFilter( _baseList, _resourceTree.ParentProperty ) );
            }
            _resourceTree.RootResource = GetSelectorRoot( resType );
            foreach( JetListViewNode node in _resourceTree.JetListView.Nodes )
            {
                node.Expanded = true;
            }
        }

        public virtual IResource GetSelectorRoot( string resType )
        {
            return Core.ResourceTreeManager.GetRootForType( resType );        	
        }

        /**
         * When a resource of a matching type is added to the tree, sets its
         * checkbox state to unchecked.
         */
        
        private void _resourceTree_ResourceAdded( object sender, ResourceEventArgs e )
        {
            if ( _resourceTree.CheckBoxes )
            {
                bool found = false;
                IResource res = e.Resource;
                for( int i = 0; i < _resTypes.Length; i++ )
                {
                    if ( res.Type == _resTypes[ i ] )
                    {
                        _resourceTree.SetNodeCheckState( res, CheckBoxState.Unchecked );
                        found = true;
                        break;
                    }
                }
                if ( !found )
                {
                    _resourceTree.SetNodeCheckState( res, CheckBoxState.Hidden );
                }
            }
        }

        /**
         * After a resource is checked or unchecked, adds or removes it from
         * the list of checked nodes.
         */

        private void _resourceTree_AfterThreeStateCheck( object sender, ResourceCheckEventArgs e )
        {
            if ( _checkedResources != null )
            {
                if ( e.NewState == CheckBoxState.Checked )
                {
                    _checkedResources.Add( e.Resource.Id );
                }
                else
                {
                    _checkedResources.Remove( e.Resource.Id );
                }
            }
        }

        private void _resourceTree_DoubleClick( object sender, HandledEventArgs e )
        {
            OnAccept();  
            e.Handled = true;
        }
    }

    internal class ResourceListFilter : IResourceNodeFilter
    {
        private IResourceList _baseList;
        private int _parentProp;
        private IntHashSet _filterSet = new IntHashSet();

        public ResourceListFilter( IResourceList list, int parentProp )
        {
            _baseList = list;
            _baseList.ResourceAdded += new ResourceIndexEventHandler( HandleResourceAdded );
            _parentProp = parentProp;
            foreach( IResource res in list )
            {
                AddParentsToSet( res );
            }
        }

        private void HandleResourceAdded( object sender, ResourceIndexEventArgs e )
        {
            AddParentsToSet( e.Resource );
        }

        private void AddParentsToSet( IResource res )
        {
            IResource parent = res;
            while( parent != null )
            {
                _filterSet.Add( parent.Id );
                parent = parent.GetLinkProp( _parentProp );
            }
        }

        public bool AcceptNode( IResource res, int level )
        {
            return _filterSet.Contains( res.Id );
        }
    }
}
