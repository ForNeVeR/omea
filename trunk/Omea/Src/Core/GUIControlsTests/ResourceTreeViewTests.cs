/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using System.Windows.Forms;

namespace GUIControlsTests
{
	/**
     * Unit tests for the ResourceTreeView class.
     */
    
    [TestFixture]
    public class ResourceTreeViewTests
	{
        private TestCore _core;
        private IResourceStore _storage;
        private ResourceTreeView _treeView;
        private ResourceTreeManager _resourceTreeManager;
        private int _propParent;
        private IResource _root;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            _treeView = new ResourceTreeView();

            _resourceTreeManager = _core.ResourceTreeManager as ResourceTreeManager;
            
            _root = _resourceTreeManager.GetRootForType( "Folder" );
            _treeView.CreateControl();

            _storage.PropTypes.Register( "FirstName", PropDataType.String );
            _storage.ResourceTypes.Register( "Person", "FirstName" );
            _storage.ResourceTypes.Register( "Folder", "Name", ResourceTypeFlags.ResourceContainer );
            _propParent = _storage.PropTypes.Register( "Parent", PropDataType.Link, PropTypeFlags.DirectedLink );
            
            _treeView.ParentProperty = _propParent;
        }

        [TearDown] public void TearDown()
        {
            _treeView.Dispose();
            _core.Dispose();
        }

        private IResource CreateResource( string resType, string propName, string propValue, IResource parent )
        {
            IResource res = _storage.NewResource( resType );
            res.SetProp( propName, propValue );
            res.AddLink( "Parent", parent );
            return res;
        }

        [Test] public void TestSimpleUpdates()
        {
            IResource folder = _storage.NewResource( "Folder" );
            folder.SetProp( _propParent, _root );

            IResource folder2 = _storage.NewResource( "Folder" );
            folder2.SetProp( _propParent, _root );

            _treeView.RootResource = _root;

            Assert.AreEqual( 2, _treeView.Nodes.Count );

            folder2.SetProp( _propParent, folder );
            _treeView.ProcessPendingUpdates();
            Assert.AreEqual( 1, _treeView.Nodes.Count );
        }

        [Test] public void NodeSorting()
        {
            _resourceTreeManager.SetResourceNodeSort( _root, "Type DisplayName" );

            IResource person = _storage.NewResource( "Person" );   // 44
            person.SetProp( "FirstName", "Michael" );
            person.SetProp( _propParent, _root );

            IResource person2 = _storage.NewResource( "Person" );  // 45
            person2.SetProp( "FirstName", "Dmitry" );
            person2.SetProp( _propParent, _root );

            IResource folder = _storage.NewResource( "Folder" );   // 46
            folder.SetProp( _propParent, _root );

            _treeView.RootResource = _root;
            Assert.AreEqual( 3, _treeView.Nodes.Count );

            Assert.AreEqual( folder, _treeView.Nodes [0].Tag );
            Assert.AreEqual( person2, _treeView.Nodes [1].Tag );
            Assert.AreEqual( person, _treeView.Nodes [2].Tag );
        }

        [Test] public void NodeSortingUpdates()
        {
            _resourceTreeManager.SetResourceNodeSort( _root, "Name" );
            _treeView.RootResource = _root;

            IResource folder = CreateResource( "Folder", "Name", "B", _root );

            _treeView.ProcessPendingUpdates();
            
            IResource folder2 = _storage.NewResource( "Folder" );
            folder2.SetProp( "Name", "A" );
            folder2.AddLink( "Parent", _root );

            _treeView.ProcessPendingUpdates();

            Assert.AreEqual( 2, _treeView.Nodes.Count );
            Assert.AreEqual( "A", _treeView.Nodes [0].Text );
            Assert.AreEqual( "B", _treeView.Nodes [1].Text );
        }

        [Test] public void NodeSortingChanges()
        {
            _resourceTreeManager.SetResourceNodeSort( _root, "Name" );
            IResource folder = CreateResource( "Folder", "Name", "A", _root );
            IResource folder2 = CreateResource( "Folder", "Name", "B", _root );
            _treeView.RootResource = _root;

            Assert.AreEqual( 2, _treeView.Nodes.Count );
            Assert.AreEqual( "A", _treeView.Nodes [0].Text );
            Assert.AreEqual( "B", _treeView.Nodes [1].Text );

            folder.SetProp( "Name", "C" );
            _treeView.ProcessPendingUpdates();

            Assert.AreEqual( 2, _treeView.Nodes.Count );
            Assert.AreEqual( "B", _treeView.Nodes [0].Text );
            Assert.AreEqual( "C", _treeView.Nodes [1].Text );

            folder2.SetProp( "Name", "D" );
            _treeView.ProcessPendingUpdates();

            Assert.AreEqual( 2, _treeView.Nodes.Count );
            Assert.AreEqual( "C", _treeView.Nodes [0].Text );
            Assert.AreEqual( "D", _treeView.Nodes [1].Text );
        }

        [Test] public void InsertIntoFilteredTree()
        {
            _resourceTreeManager.SetResourceNodeSort( _root, "Name" );
            _treeView.AddNodeFilter( new TreeResourceTypeFilter( "Person" ) );

            IResource person = CreateResource( "Person", "Name", "A", _root );
            IResource folder = CreateResource( "Folder", "Name", "C", _root );
            _treeView.RootResource = _root;

            IResource folder2 = CreateResource( "Folder", "Name", "B", _root );
            _treeView.ProcessPendingUpdates();

            Assert.AreEqual( 2, _treeView.Nodes.Count );
            Assert.AreEqual( "B", _treeView.Nodes [0].Text );
            Assert.AreEqual( "C", _treeView.Nodes [1].Text );
        }

        [Test] public void SelectAddedItems()
        {
            IResource folder = CreateResource( "Folder", "Name", "A", _root );
            _treeView.RootResource = _root;
            _treeView.SelectResourceNode( folder );

            _treeView.SelectAddedItems = true;
            IResource folder2 = CreateResource( "Folder", "Name", "B", _root );
            _treeView.ProcessPendingUpdates();
            Assert.AreEqual( folder2, _treeView.SelectedResource );
        }

        [Test] public void FilterAfterChange()
        {
            _treeView.AddNodeFilter( new TreeResourceNameFilter( "A") );
            
            IResource person = CreateResource( "Person", "FirstName", "B", _root );
            _treeView.RootResource = _root;

            Assert.AreEqual( 0, _treeView.Nodes.Count );

            person.SetProp( "FirstName", "A" );
            _treeView.ProcessPendingUpdates();

            Assert.AreEqual( 1, _treeView.Nodes.Count );
            Assert.AreEqual( "A", _treeView.Nodes [0].Text );

            person.SetProp( "FirstName", "B" );
            _treeView.ProcessPendingUpdates();

            Assert.AreEqual( 0, _treeView.Nodes.Count );
        }

        [Test] public void ForceCreateChildren()
        {
            IResource folder1 = CreateResource( "Folder", "FirstName", "B", _root );
            IResource folder2 = CreateResource( "Folder", "FirstName", "C", folder1 );
            IResource folder3 = CreateResource( "Folder", "FirstName", "C", folder2 );

            _treeView.RootResource = _root;
            TreeNode folder1Node = _treeView.Nodes [0];
            Assert.AreEqual( 0, folder1Node.Nodes.Count );

            _treeView.ForceCreateChildren( folder1Node );
            Assert.AreEqual( 1, folder1Node.Nodes.Count );
            TreeNode folder2Node = folder1Node.Nodes [0];
            Assert.AreEqual( 1, folder2Node.Nodes.Count );
        }

        [Test] public void ForceCreateChildren_RootExpanded()
        {
            IResource folder1 = CreateResource( "Folder", "FirstName", "B", _root );
            IResource folder2 = CreateResource( "Folder", "FirstName", "C", folder1 );
            IResource folder3 = CreateResource( "Folder", "FirstName", "C", folder2 );

            _treeView.RootResource = _root;
            TreeNode folder1Node = _treeView.Nodes [0];
            folder1Node.Expand();

            _treeView.ForceCreateChildren( folder1Node );
            Assert.AreEqual( 1, folder1Node.Nodes.Count );
            TreeNode folder2Node = folder1Node.Nodes [0];
            Assert.AreEqual( 1, folder2Node.Nodes.Count );
        }

        [Test] public void TwoLevelRemove()
        {
            IResource folder1 = CreateResource( "Folder", "Name", "A", _root );
            IResource folder2 = CreateResource( "Folder", "Name", "X", folder1 );
            IResource folder3 = CreateResource( "Folder", "Name", "MustHaveChildren", _root );
            IResource folder4 = CreateResource( "Folder", "Name", "C", folder3 );

            _treeView.AddNodeFilter( new TreeResourceChildFilter() );
            _treeView.RootResource = _root;
            _treeView.Nodes [0].Expand();
            Assert.AreEqual( 1, _treeView.Nodes [0].Nodes.Count );
            _treeView.Nodes [1].Expand();
            Assert.AreEqual( 1, _treeView.Nodes [1].Nodes.Count );

            folder4.SetProp( "Parent", folder1 );
            _treeView.ProcessPendingUpdates();

            Assert.AreEqual( 1, _treeView.Nodes.Count );
            Assert.AreEqual( 2, _treeView.Nodes [0].Nodes.Count );
        }

        [Test] public void AddAfterRemove()
        {
            IResource folder1 = CreateResource( "Folder", "Name", "A", _root );
            IResource folder2 = CreateResource( "Folder", "Name", "X", folder1 );

            _treeView.RootResource = _root;
            _treeView.Nodes [0].Expand();
            Assert.AreEqual( 1, _treeView.Nodes [0].Nodes.Count );
            
            folder1.SetProp( "Parent", null );
            _treeView.ProcessPendingUpdates();
            Assert.AreEqual( 0, _treeView.Nodes.Count );

            folder1.SetProp( "Parent", _root );
            _treeView.ProcessPendingUpdates();
            Assert.AreEqual( 1, _treeView.Nodes.Count );

            _treeView.Nodes [0].Expand();
            Assert.AreEqual( 1, _treeView.Nodes [0].Nodes.Count );
        }

        [Test] public void NonUniqueResources()
        {
            _treeView.UniqueResources = false;
            IResource folder1 = CreateResource( "Folder", "Name", "A", _root );
            IResource folder2 = CreateResource( "Folder", "Name", "B", _root );
            IResource folder3 = CreateResource( "Folder", "Name", "X", folder1 );
            folder3.AddLink( "Parent", folder2 );

            _treeView.RootResource = _root;
            _treeView.Nodes [0].Expand();
            _treeView.Nodes [1].Expand();

            Assert.AreEqual( 1, _treeView.Nodes [0].Nodes.Count );
            Assert.AreEqual( 1, _treeView.Nodes [1].Nodes.Count );

            folder3.SetProp( "Name", "Y" );
            _treeView.ProcessPendingUpdates();

            Assert.AreEqual( "Y", _treeView.Nodes [0].Nodes [0].Text );
            Assert.AreEqual( "Y", _treeView.Nodes [1].Nodes [0].Text );

            folder1.SetProp( "Name", "C" );
            _treeView.ProcessPendingUpdates();
            Assert.AreEqual( "C", _treeView.Nodes [0].Text );
            Assert.AreEqual( 2, _treeView.Nodes.Count );

            folder1.Delete();
            _treeView.ProcessPendingUpdates();
            Assert.AreEqual( 1, _treeView.Nodes.Count );
        }

        [Test] public void SelectionAfterMove()
        {
            IResource folder1 = CreateResource( "Folder", "Name", "A", _root );
            IResource folder2 = CreateResource( "Folder", "Name", "B", _root );
            IResource folder3 = CreateResource( "Folder", "Name", "X", folder1 );

            _treeView.RootResource = _root;
            _treeView.SelectedResource = folder3;

            folder3.SetProp( "Parent", folder2 );
            _treeView.ProcessPendingUpdates();
            Assert.AreEqual( folder3, _treeView.SelectedResource );
        }

        [Test] public void RemoveTwoLevelsAndMoveChildToRoot()
        {
            IResource folder1 = CreateResource( "Folder", "Name", "A", _root );
            IResource folder2 = CreateResource( "Folder", "Name", "B", folder1 );
            IResource folder3 = CreateResource( "Folder", "Name", "X", folder2 );

            _treeView.RootResource = _root;

            folder3.SetProp( "Parent", _root );
            //folder2.Delete();
            folder1.Delete();

            _treeView.ProcessPendingUpdates();
            Assert.AreEqual( 1, _treeView.Nodes.Count );
            Assert.AreEqual( "X", _treeView.Nodes [0].Text );                        
        }

        [Test] public void FilterAfterExpand()
        {
            IResource folder1 = CreateResource( "Folder", "Name", "A", _root );
            IResource folder2 = CreateResource( "Folder", "Name", "B", folder1 );
            
            _treeView.RootResource = _root;

            Assert.AreEqual( 1, _treeView.GetNodeChildCount( _treeView.Nodes [0] ) );
            _treeView.Nodes [0].Expand();
            Assert.AreEqual( 1, _treeView.GetNodeChildCount( _treeView.Nodes [0] ) );
            _treeView.Nodes [0].Collapse();

            TreeResourceNameFilter filter = new TreeResourceNameFilter( "A" );
            _treeView.AddNodeFilter( filter );
            _treeView.UpdateNodeFilter( true );

            _treeView.Nodes [0].Expand();
            Assert.AreEqual( 0, _treeView.GetNodeChildCount( _treeView.Nodes [0] ) );

            /*
            _treeView.AddNodeFilter( filter );
            _treeView.UpdateNodeFilter( true );
            */
        }

        [Test] public void AddUnderNewNode()
        {
            IResource folder1 = CreateResource( "Folder", "Name", "A", _root );
            _treeView.RootResource = _root;
            Assert.AreEqual( 0, _treeView.GetNodeChildCount( _treeView.Nodes [0] ) );

            IResource folder2 = CreateResource( "Folder", "Name", "B", folder1 );
            _treeView.ProcessPendingUpdates();
            
            _treeView.Nodes [0].Expand();
            Assert.AreEqual( 1, _treeView.Nodes [0].Nodes.Count );

            IResource folder3 = CreateResource( "Folder", "Name", "C", folder2 );
            _treeView.ProcessPendingUpdates();
            _treeView.Nodes [0].Nodes [0].Expand();
            Assert.AreEqual( 1, _treeView.Nodes [0].Nodes [0].Nodes.Count );
        }
	}

    internal class TreeResourceTypeFilter: IResourceNodeFilter
    {
        private string _filterType;

        internal TreeResourceTypeFilter( string filterType )
        {
            _filterType = filterType;
        }

        public bool AcceptNode( IResource res, int level )
        {
            return res.Type != _filterType;
        }
    }

    internal class TreeResourceNameFilter: IResourceNodeFilter
    {
        private string _name;

        internal TreeResourceNameFilter( string name )
        {
            _name = name;
        }

        public bool AcceptNode( IResource res, int level )
        {
            return res.DisplayName == _name;
        }
    }

    internal class TreeResourceChildFilter: IResourceNodeFilter
    {
        public bool AcceptNode( IResource res, int level )
        {
            if ( res.DisplayName == "MustHaveChildren" )
            {
                return res.GetLinksTo( null, "Parent" ).Count > 0;
            }
            return true;
        }
    }

}
