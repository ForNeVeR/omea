// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;
using JetBrains.Omea.Categories;
using JetBrains.Omea.OpenAPI;

namespace GUIControlsTests
{
	/// <summary>
    /// Unit tests for the ResourceTreePane class.
	/// </summary>
	[TestFixture]
    public class ResourceTreePaneTests
	{
        private TestCore _core;
        private IWorkspaceManager _workspaceManager;
        private JetResourceTreePane _treePane;
        private IResource _workspace;
        private IResourceStore _storage;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;

            _workspaceManager = Core.WorkspaceManager;

            _workspaceManager.RegisterWorkspaceType( "Category",
                new int[] { (Core.CategoryManager as CategoryManager).PropCategory }, WorkspaceResourceType.Filter );
            _workspace = _workspaceManager.CreateWorkspace( "WS" );

            _storage.ResourceTypes.Register( "Folder", "Name" );
            _storage.ResourceTypes.Register( "Email", "Name" );

            _treePane = new JetResourceTreePane();
        }

        [TearDown] public void TearDown()
        {
            _treePane.Dispose();
            _core.Dispose();
        }

        [Test] public void OtherViewInWorkspace()
        {
            IResource email = _storage.NewResource( "Email" );
            _workspaceManager.AddResourceToWorkspace( _workspace, email );

            _treePane.RootResource = Core.ResourceTreeManager.GetRootForType( "Email" );
            _treePane.Populate();
            _treePane.WorkspaceFilterTypes = new string[] { "Folder" };
            _treePane.SetActiveWorkspace( _workspace );
            Assert.AreEqual( 1, _treePane.ResourceTree.JetListView.NodeCollection.VisibleItemCount );

            IResource workspaceOtherView = Core.ResourceStore.GetAllResources( "WorkspaceOtherView" ) [0];
            JetListViewNode otherViewNode = _treePane.ResourceTree.JetListView.NodeCollection.NodeFromItem( workspaceOtherView );
            Assert.IsNotNull( otherViewNode );
            Assert.IsTrue( otherViewNode.FiltersAccept );
        }
    }
}
