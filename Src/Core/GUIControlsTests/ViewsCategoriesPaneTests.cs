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
    /// Unit tests for the ViewsCategoriesPane class.
    /// </summary>
    [TestFixture]
    public class ViewsCategoriesPaneTests
    {
        private TestCore _core;
        private IWorkspaceManager _workspaceManager;
        private ViewsCategoriesPane _treePane;
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

            _treePane = new ViewsCategoriesPane();
        }

        [TearDown] public void TearDown()
        {
            _treePane.Dispose();
            _core.Dispose();
        }

        [Test] public void CategoriesInWorkspace()
        {
            IResource category1 = CategoryManager.CreateCategory( "Category1", Core.CategoryManager.RootCategory );
            IResource category2 = CategoryManager.CreateCategory( "Category2", category1 );
            IResource category3 = CategoryManager.CreateCategory( "Category3", Core.CategoryManager.RootCategory );
            IResource category4 = CategoryManager.CreateCategory( "Category4", category3 );

            _workspaceManager.AddResourceToWorkspaceRecursive( _workspace, category1 );
            _workspaceManager.AddResourceToWorkspace( _workspace, category3 );

            _treePane.RootResource = Core.ResourceTreeManager.ResourceTreeRoot;
            _treePane.Populate();
            _treePane.SetActiveWorkspace( _workspace );
            Assert.AreEqual( 1, _treePane.ResourceTree.JetListView.Nodes.Count );
            JetListViewNode categoriesNode = _treePane.ResourceTree.JetListView.Nodes [0];
            Assert.AreEqual( "Categories", categoriesNode.Data.ToString() );

            categoriesNode.Expanded = true;
            Assert.AreEqual( 2, categoriesNode.Nodes.Count );

            JetListViewNode cat1Node = categoriesNode.Nodes [0];
            Assert.AreEqual( "Category1", cat1Node.Data.ToString() );
            cat1Node.Expanded = true;
            Assert.AreEqual( 1, cat1Node.Nodes.Count );

            JetListViewNode cat3Node = categoriesNode.Nodes [1];
            Assert.AreEqual( "Category3", cat3Node.Data.ToString() );
            cat3Node.Expanded = true;
            Assert.IsFalse( cat3Node.Nodes [0].FiltersAccept );
        }

        [Test] public void OtherViewInViewsCategories()
        {
            _treePane.RootResource = Core.ResourceTreeManager.ResourceTreeRoot;
            _treePane.Populate();
            _treePane.ShowWorkspaceOtherView = true;
            _workspaceManager.ActiveWorkspace = _workspace;
            _treePane.SetActiveWorkspace( _workspace );
            Assert.AreEqual( 2, _treePane.ResourceTree.JetListView.Nodes.Count );
            JetListViewNode categoriesNode = _treePane.ResourceTree.JetListView.Nodes [0];
            Assert.AreEqual( "Other", categoriesNode.Data.ToString() );
        }
    }
}
