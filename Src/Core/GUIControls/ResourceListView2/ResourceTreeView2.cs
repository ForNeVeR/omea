// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// More or less API-compatible version of ResourceTreeView based on JetListView.
	/// </summary>
	public class ResourceTreeViewBase : ResourceListView2
	{
        protected PersistentCheckBoxColumn _checkBoxColumn;
        protected bool _checkBoxes = false;
        protected ResourceTreeDataProvider _dataProvider;
        protected int _parentProp = -1;

        public ResourceTreeViewBase()
        {
            _dataProvider = new ResourceTreeDataProvider();
        }

        public event ResourceCheckEventHandler BeforeCheck;
        public event ResourceCheckEventHandler AfterCheck;

        public override IResource RootResource
        {
            get { return base.RootResource; }
            set
            {
				if ( RootResource != value )
				{
					base.RootResource = value;
                    CheckFillTree();
                }
            }
        }

	    public int ParentProperty
	    {
	        get { return _parentProp; }
	        set
	        {
                if ( _parentProp != value )
                {
                    _parentProp = value;
                    CheckFillTree();
                }
	        }
	    }

        /// <summary>
        /// Gets or sets the value indicating whether checkboxes are displayed in the resource tree.
        /// </summary>
        public bool CheckBoxes
        {
            get { return _checkBoxes; }
            set
            {
                if ( _checkBoxes != value )
                {
                    _checkBoxes = value;
                    if ( _checkBoxes )
                    {
                        if ( _checkBoxColumn == null )
                        {
                            _checkBoxColumn = new PersistentCheckBoxColumn();
                            _checkBoxColumn.BeforeCheck += ForwardBeforeCheck;
                            _checkBoxColumn.AfterCheck += ForwardAfterCheck;
                        }
                        Columns.Insert( 1, _checkBoxColumn );
                    }
                    else
                    {
                        Columns.Remove( _checkBoxColumn );
                    }
                }
            }
        }

	    /// <summary>
	    /// Gets or sets the value specifying the ID of the property which saves
	    /// the checked state of a node.
	    /// </summary>
        public int CheckedProperty
	    {
	        get { return _checkBoxColumn.CheckedProperty; }
	        set { _checkBoxColumn.CheckedProperty = value; }
	    }

	    /// <summary>
	    /// Gets or sets the value which is saved in the CheckedProperty property
	    /// when the node is checked.
	    /// </summary>
        public object CheckedSetValue
	    {
	        get { return _checkBoxColumn.CheckedSetValue; }
	        set { _checkBoxColumn.CheckedSetValue = value; }
	    }

        /// <summary>
        /// Gets or sets the value which is saved in the CheckedProperty property
        /// when the node is unchecked.
        /// </summary>
        public object CheckedUnsetValue
	    {
	        get { return _checkBoxColumn.CheckedUnsetValue; }
	        set { _checkBoxColumn.CheckedUnsetValue = value; }
	    }

	    private void CheckFillTree()
	    {
	        if ( RootResource != null && _parentProp != -1 )
	        {
	            _dataProvider.SetRootResource( RootResource, _parentProp );
	            DataProvider = _dataProvider;
	        }
	    }

	    /// <summary>
	    /// Sets the checked state of the specified resource node to the specified value.
	    /// </summary>
	    /// <param name="res">The resource to check or uncheck.</param>
	    /// <param name="checkState">The new checked/unchecked state.</param>
        public void SetNodeCheckState( IResource res, CheckBoxState checkState )
        {
            _checkBoxColumn.SetItemCheckState( res, checkState );
        }

        /// <summary>
        /// Gets the checked state of the specified resource.
        /// </summary>
        /// <param name="res">The resource to get the state for.</param>
        /// <returns>The new checked/unchecked state.</returns>
        public CheckBoxState GetNodeCheckState( IResource res )
        {
            return _checkBoxColumn.GetItemCheckState( res );
        }

        private void ForwardBeforeCheck( object sender, CheckBoxEventArgs e )
        {
            if ( BeforeCheck != null )
            {
                ResourceCheckEventArgs args = new ResourceCheckEventArgs( (IResource) e.Item, e.OldState, e.NewState );
                BeforeCheck( e, args );
                e.NewState = args.NewState;
            }
        }

        private void ForwardAfterCheck( object sender, CheckBoxEventArgs e )
        {
            if ( AfterCheck != null )
            {
                ResourceCheckEventArgs args = new ResourceCheckEventArgs( (IResource) e.Item, e.OldState, e.NewState );
                AfterCheck( e, args );
                e.NewState = args.NewState;
            }
        }

        public void AddNodeFilter( IResourceNodeFilter filter )
        {
            Filters.Add( new ResourceNodeFilterAdapter( filter ) );
        }

        public void UpdateNodeFilter( bool keepSelection )
        {
            Filters.Update();
        }

	    public bool SelectResourceNode( IResource resource )
	    {
            return _dataProvider.SelectResource( resource );
	    }
	}

	public class DecoResourceTreeView: ResourceTreeViewBase
	{
        private readonly TreeStructureColumn _treeStructureColumn;
        private readonly ResourceIconColumn  _iconColumn;
        private readonly RichTextColumn      _nameColumn;

        public DecoResourceTreeView() : base()
        {
            _treeStructureColumn = new TreeStructureColumn();
            Columns.Add( _treeStructureColumn );
            _iconColumn = new ResourceIconColumn();
            Columns.Add( _iconColumn );
            _nameColumn = new RichTextColumn();
            _nameColumn.AutoSize = true;
            Columns.Add( _nameColumn );
        }

        public void  AddNodeDecorator( IResourceNodeDecorator decorator )
        {
            _nameColumn.AddNodeDecorator( decorator );
        }
    }

	public class ResourceTreeView2: ResourceTreeViewBase
	{
        private readonly TreeStructureColumn _treeStructureColumn;
        private readonly ResourceIconColumn _iconColumn;
        private readonly JetListViewColumn _nameColumn;

        public ResourceTreeView2() : base()
        {
            _treeStructureColumn = new TreeStructureColumn();
            Columns.Add( _treeStructureColumn );
            _iconColumn = new ResourceIconColumn();
            Columns.Add( _iconColumn );
            _nameColumn = new JetListViewColumn();
            _nameColumn.SizeToContent = true;
            Columns.Add( _nameColumn );
        }
	}

    public class ResourceCheckEventArgs: EventArgs
    {
        private readonly IResource _resource;
        private readonly CheckBoxState _oldState;
        private CheckBoxState _newState;

        public ResourceCheckEventArgs( IResource resource, CheckBoxState oldState, CheckBoxState newState )
        {
            _resource = resource;
            _oldState = oldState;
            _newState = newState;
        }

        public IResource Resource
        {
            get { return _resource; }
        }

        public CheckBoxState OldState
        {
            get { return _oldState; }
        }

        public CheckBoxState NewState
        {
            get { return _newState; }
            set { _newState = value; }
        }
    }

    public delegate void ResourceCheckEventHandler( object sender, ResourceCheckEventArgs e );
}
