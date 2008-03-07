/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace GUIControlsTests
{
    [TestFixture]
    public class ResourceTreeDataProviderTests
    {
        private TestCore _core;
        private IResource _rootFolder;
        private int _propParent;
        private ResourceListView2 _resourceListView;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();

            _core.ResourceStore.ResourceTypes.Register( "Folder", "Name", ResourceTypeFlags.ResourceContainer );
            _core.ResourceStore.ResourceTypes.Register( "Item", "Name", ResourceTypeFlags.Normal );
            _propParent = _core.ResourceStore.PropTypes.Register( "Parent", PropDataType.Link, PropTypeFlags.DirectedLink );
            _core.ResourceStore.PropTypes.Register( "IsUnread", PropDataType.Bool );

            _rootFolder = _core.ResourceStore.NewResource( "Folder" );

            _resourceListView = new ResourceListView2();
        }

        [TearDown] public void TearDown()
        {
            _resourceListView.Dispose();
            _core.Dispose();
        }

        private IResource NewFolder( IResource parent )
        {
            IResource result = _core.ResourceStore.NewResource( "Folder" );
            result.AddLink( _propParent, parent );
            return result;
        }

        private IResource NewItem( IResource parent )
        {
            IResource result = _core.ResourceStore.NewResource( "Item" );
            result.AddLink( _propParent, parent );
            return result;
        }

        [Test] public void SimpleTest()
        {
            IResource childFolder = NewFolder( _rootFolder );

            ResourceTreeDataProvider provider = new ResourceTreeDataProvider( _rootFolder, _propParent );
            _resourceListView.DataProvider = provider;

            Assert.AreEqual( 1, provider.ListView.Nodes.Count );
        }

        [Test] public void TwoLevels()
        {
            IResource child1 = NewFolder( _rootFolder );
            IResource child2 = NewFolder( child1 );

            ResourceTreeDataProvider provider = new ResourceTreeDataProvider( _rootFolder, _propParent );
            _resourceListView.DataProvider = provider;

            Assert.AreEqual( 1, provider.ListView.Nodes.Count );
            JetListViewNode node = provider.ListView.Nodes [0];
            Assert.AreEqual( CollapseState.Collapsed, node.CollapseState );
            node.Expanded = true;
            Assert.AreEqual( 1, node.Nodes.Count );
        }

        [Test] public void ChangeParent()
        {
            IResource child1 = NewFolder( _rootFolder );
            IResource child2 = NewFolder( _rootFolder );

            ResourceTreeDataProvider provider = new ResourceTreeDataProvider( _rootFolder, _propParent );
            _resourceListView.DataProvider = provider;

            Assert.AreEqual( 2, provider.ListView.Nodes.Count );

            child2.SetProp( _propParent, child1 );
            Assert.AreEqual( 1, provider.ListView.Nodes.Count );
        }

        [Test] public void AddToRoot()
        {
            ResourceTreeDataProvider provider = new ResourceTreeDataProvider( _rootFolder, _propParent );
            _resourceListView.DataProvider = provider;

            IResource child = NewFolder( _rootFolder );
            Assert.AreEqual( 1, provider.ListView.Nodes.Count );
        }

        [Test] public void ChangeParentHierarchy()
        {
            IResource child1 = NewFolder( _rootFolder );
            IResource child2 = NewFolder( child1 );
            IResource child3 = NewFolder( child2 );

            ResourceTreeDataProvider provider = new ResourceTreeDataProvider( _rootFolder, _propParent );
            _resourceListView.DataProvider = provider;

            child2.SetProp( _propParent, _rootFolder );
            JetListViewNode node = provider.ListView.NodeCollection.NodeFromItem( child2 );
            Assert.IsFalse( node.Expanded );
            node.Expanded = true;
            Assert.AreEqual( 1, node.Nodes.Count );
        }

        [Test] public void ChangeParentHierarchyWithExpand()
        {
            IResource child1 = NewFolder( _rootFolder );
            IResource child2 = NewFolder( child1 );
            IResource child3 = NewFolder( child2 );

            ResourceTreeDataProvider provider = new ResourceTreeDataProvider( _rootFolder, _propParent );
            _resourceListView.DataProvider = provider;

            provider.ListView.NodeCollection.NodeFromItem( child1 ).Expanded = true;
            provider.ListView.NodeCollection.NodeFromItem( child2 ).Expanded = true;

            child2.SetProp( _propParent, _rootFolder );
            JetListViewNode node = provider.ListView.NodeCollection.NodeFromItem( child2 );
            Assert.IsFalse( node.Expanded );
            node.Expanded = true;
            Assert.AreEqual( 1, node.Nodes.Count );
        }

        [Test] public void RemoveNotAddedNode()
        {
            IResource child1 = NewFolder( _rootFolder );
            IResource child2 = NewItem( child1 );

            ResourceTreeDataProvider provider = new ResourceTreeDataProvider( _rootFolder, _propParent );
            _resourceListView.DataProvider = provider;

            JetListViewNode node = provider.ListView.NodeCollection.NodeFromItem( child1 );
            node.Expanded = true;

            child2.Delete();
        }
    }
}
