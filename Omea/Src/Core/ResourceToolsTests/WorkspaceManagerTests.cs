// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using JetBrains.Omea.ResourceTools;
using NUnit.Framework;
using JetBrains.Omea.Categories;

namespace ResourceToolsTests
{
    /**
     * Unit tests for the WorkspaceManager class.
     */

    [TestFixture]
    public class WorkspaceManagerTests
    {
        private TestCore _core;
        private TestResourceStore _storage;
        private WorkspaceManager _workspaceManager;
        private CategoryManager _categoryManager;
        private int _propAuthor;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore as TestResourceStore;
            _storage.ResourceTypes.Register( "Folder", "Name" );
            _storage.ResourceTypes.Register( "Email", "Name" );
            _storage.ResourceTypes.Register( "Person", "Name" );

            _propAuthor = _storage.PropTypes.Register( "Author", PropDataType.Link );

            InitializeWorkspaceManager();
        }

        private void InitializeWorkspaceManager()
        {
            _categoryManager = _core.CategoryManager as CategoryManager;

            _workspaceManager = _core.WorkspaceManager as WorkspaceManager;

            _workspaceManager.RegisterWorkspaceType( "Person",
                                                     new int[] { _propAuthor }, WorkspaceResourceType.Container );
            _workspaceManager.RegisterWorkspaceType( "Category",
                                                     new int[] { _categoryManager.PropCategory }, WorkspaceResourceType.Filter );
            _workspaceManager.RegisterWorkspaceType( "Folder",
                                                     new int[] { _propAuthor }, WorkspaceResourceType.Container );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        [Test] public void TestFilterList()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource email = _storage.NewResource( "Email" );

            person.AddLink( _propAuthor, email );

            IResource workspace = _workspaceManager.CreateWorkspace( "Test" );

            _workspaceManager.AddResourceToWorkspace( workspace, person );

            IResourceList resList = _workspaceManager.GetFilterList( workspace );
            Assert.AreEqual( 2, resList.Count );
            resList.Sort( "ID" );
            Assert.AreEqual( person.Id, resList [0].Id );
            Assert.AreEqual( email.Id, resList [1].Id );

            person.DeleteLink( _propAuthor, email );
            Assert.AreEqual( 1, _workspaceManager.GetFilterList( workspace ).Count );
        }

        [Test] public void TestFilterListEx()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource category = CategoryManager.CreateCategory( "Test", _categoryManager.RootCategory );
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );

            person.AddLink( _propAuthor, email );
        	_categoryManager.AddResourceCategory( email2, category );

            IResource workspace = _workspaceManager.CreateWorkspace( "Test ");
            _workspaceManager.AddResourceToWorkspace( workspace, person );
            _workspaceManager.AddResourceToWorkspace( workspace, category );
            IResourceList resList = _workspaceManager.GetFilterList( workspace );
            Assert.AreEqual( 4, resList.Count );
            Assert.IsTrue( resList.IndexOf( email ) >= 0 );
            Assert.IsTrue( resList.IndexOf( email2 ) >= 0 );
            Assert.IsTrue( resList.IndexOf( person ) >= 0 );
            Assert.IsTrue( resList.IndexOf( category ) >= 0 );
        }

        [Test] public void ResourcesOutsideContainersTest()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource category = CategoryManager.CreateCategory( "Test", _categoryManager.RootCategory );
            IResource email = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            IResource email3 = _storage.NewResource( "Email" );

            email.AddLink( _propAuthor, person );
            email2.AddLink( _propAuthor, person );
        	_categoryManager.AddResourceCategory( email2, category );
        	_categoryManager.AddResourceCategory( email3, category );

            IResource workspace = _workspaceManager.CreateWorkspace( "Test ");
            _workspaceManager.AddResourceToWorkspace( workspace, person );

            Assert.IsFalse( _workspaceManager.HasResourcesOutsideContainers( workspace ) );

            _workspaceManager.AddResourceToWorkspace( workspace, category );

            Assert.IsTrue( _workspaceManager.HasResourcesOutsideContainers( workspace ) );
            IResourceList outsideList = _workspaceManager.GetResourcesOutsideContainers( workspace );
            Assert.IsFalse( outsideList.Contains( email ) );
            Assert.IsFalse( outsideList.Contains( email2 ) );
            Assert.IsTrue( outsideList.Contains( email3 ) );
        }

        [Test] public void NonWorkspaceTypesInResourcesOutsideContainers()
        {
            _workspaceManager.RegisterWorkspaceType( "Email", new int[] {}, WorkspaceResourceType.None );
            IResource email = _storage.NewResource( "Email" );
            IResource workspace = _workspaceManager.CreateWorkspace( "Test ");
            _workspaceManager.AddResourceToWorkspace( workspace, email );

            IResourceList outsideList = _workspaceManager.GetResourcesOutsideContainers( workspace );
            Assert.AreEqual( 1, outsideList.Count );
            Assert.AreEqual( email, outsideList [0] );
        }

        [Test] public void ResourceRecursive()
        {
            IResource folder = _storage.NewResource( "Folder" );
            IResource workspace = _workspaceManager.CreateWorkspace( "Test" );
            _workspaceManager.AddResourceToWorkspaceRecursive( workspace, folder );

            IResourceList wsResources = _workspaceManager.GetFilterList( workspace );
            Assert.AreEqual( 1, wsResources.Count );

            IResource childFolder = _storage.NewResource( "Folder" );
            childFolder.AddLink( "Parent", folder );

            wsResources = _workspaceManager.GetFilterList( workspace );
            Assert.AreEqual( 2, wsResources.Count );

            IResource person = _storage.NewResource( "Person" );
            person.AddLink( _propAuthor, childFolder );
            Assert.AreEqual( 3, _workspaceManager.GetFilterList( workspace ).Count );

            _workspaceManager.RemoveResourceFromWorkspace( workspace, childFolder );
            Assert.AreEqual( 1, _workspaceManager.GetFilterList( workspace ).Count );

            _workspaceManager.AddResourceToWorkspace( workspace, childFolder );
            Assert.AreEqual( 3, _workspaceManager.GetFilterList( workspace ).Count );
        }

        [Test] public void RecursiveFilter()
        {
            IResource category = CategoryManager.CreateCategory( "Category", _categoryManager.RootCategory );
            IResource category2 = CategoryManager.CreateCategory( "Category2", category );

            IResource person = _storage.NewResource( "Person" );
            _categoryManager.AddResourceCategory( person, category2 );

            IResource ws = _workspaceManager.CreateWorkspace( "WS" );
            _workspaceManager.AddResourceToWorkspaceRecursive( ws, category );

            IResourceList wsList = _workspaceManager.GetFilterList( ws );
            Assert.AreEqual( 3, wsList.Count );
            Assert.IsTrue( wsList.IndexOf( person ) >= 0 );
        }

        [Test] public void RememberRemoveChild()
        {
            IResource folder = _storage.NewResource( "Folder" );
            IResource childFolder = _storage.NewResource( "Folder" );
            childFolder.AddLink( "Parent", folder );

            IResource workspace = _workspaceManager.CreateWorkspace( "Test" );
            _workspaceManager.AddResourceToWorkspaceRecursive( workspace, folder );
            _workspaceManager.RemoveResourceFromWorkspace( workspace, childFolder );

            Assert.AreEqual( 1, _workspaceManager.GetFilterList( workspace ).Count );

            _workspaceManager.RemoveResourceFromWorkspace( workspace, folder );
              // this should forget about the fact that the child was removed from workspace (#3808)

            _workspaceManager.AddResourceToWorkspaceRecursive( workspace, folder );
            Assert.AreEqual( 2, _workspaceManager.GetFilterList( workspace ).Count );
        }

        [Test] public void AddRecursiveAfterAddChild()
        {
            IResource folder = _storage.NewResource( "Folder" );
            IResource childFolder = _storage.NewResource( "Folder" );
            childFolder.AddLink( "Parent", folder );

            IResource workspace = _workspaceManager.CreateWorkspace( "Test" );
            _workspaceManager.AddResourceToWorkspace( workspace, childFolder );
            _workspaceManager.AddResourceToWorkspaceRecursive( workspace, folder );
              // this should delete direct links to workspace from all child resources (#3844)

            Assert.AreEqual( 2, _workspaceManager.GetFilterList( workspace ).Count );

            _workspaceManager.RemoveResourceFromWorkspace( workspace, childFolder );
            Assert.AreEqual( 1, _workspaceManager.GetFilterList( workspace ).Count );
        }

        [Test] public void GetResourceWorkspaces()
        {
            IResource folder = _storage.NewResource( "Folder" );
            IResource workspace = _workspaceManager.CreateWorkspace( "Test" );
            _workspaceManager.AddResourceToWorkspace( workspace, folder );

            IResourceList workspaces = _workspaceManager.GetResourceWorkspaces( folder );
            Assert.AreEqual( 1, workspaces.Count );
            Assert.AreEqual( workspace, workspaces [0] );

            IResource person = _storage.NewResource( "Person" );
            person.AddLink( _propAuthor, folder );

            workspaces = _workspaceManager.GetResourceWorkspaces( person );
            Assert.AreEqual( 1, workspaces.Count );
            Assert.AreEqual( workspace, workspaces [0] );

            IResource folder2 = _storage.NewResource( "Folder" );
            _workspaceManager.AddResourceToWorkspaceRecursive( workspace, folder2 );
            IResource childFolder = _storage.NewResource( "Folder" );
            childFolder.AddLink( "Parent", folder2 );

            workspaces = _workspaceManager.GetResourceWorkspaces( childFolder );
            Assert.AreEqual( 1, workspaces.Count );

            IResource person2 = _storage.NewResource( "Person" ) ;
            person2.AddLink( _propAuthor, childFolder );

            workspaces = _workspaceManager.GetResourceWorkspaces( person2 );
            Assert.AreEqual( 1, workspaces.Count );
            Assert.AreEqual( workspace, workspaces [0] );

            _workspaceManager.RemoveResourceFromWorkspace( workspace, childFolder );

            workspaces = _workspaceManager.GetResourceWorkspaces( person2 );
            Assert.AreEqual( 0, workspaces.Count );

            _workspaceManager.AddResourceToWorkspace( workspace, childFolder );

            workspaces = _workspaceManager.GetResourceWorkspaces( person2 );
            Assert.AreEqual( 1, workspaces.Count );

            _workspaceManager.RemoveResourceFromWorkspace( workspace, folder2 );

            workspaces = _workspaceManager.GetResourceWorkspaces( person2 );
            Assert.AreEqual( 0, workspaces.Count );
        }

        [Test] public void AddRecursiveAfterAdd()
        {
            IResource folder = _storage.NewResource( "Folder" );
            IResource childFolder = _storage.NewResource( "Folder" );
            IResource person = _storage.NewResource( "Person" );
            IResource childPerson = _storage.NewResource( "Person" );

            childFolder.AddLink( "Parent", folder );
            person.AddLink( _propAuthor, folder );
            childPerson.AddLink( _propAuthor, childFolder );

            IResource workspace = _workspaceManager.CreateWorkspace( "Test" );
            _workspaceManager.AddResourceToWorkspace( workspace, folder );
            Assert.IsTrue( _workspaceManager.GetFilterList( workspace ).Contains( person ) );
            Assert.IsFalse( _workspaceManager.GetFilterList( workspace ).Contains( childPerson ) );

            _workspaceManager.AddResourceToWorkspaceRecursive( workspace, folder );
            Assert.IsFalse( folder.HasLink( "InWorkspace", workspace ) );
            Assert.IsTrue( _workspaceManager.GetFilterList( workspace ).Contains( person ) );
            Assert.IsTrue( _workspaceManager.GetFilterList( workspace ).Contains( childPerson ) );
        }

        [Test] public void LoadWorkspaceData()
        {
            IResource folder = _storage.NewResource( "Folder" );
            IResource workspace = _workspaceManager.CreateWorkspace( "Test" );
            _workspaceManager.AddResourceToWorkspace( workspace, folder );

            IResource person = _storage.NewResource( "Person" );
            person.AddLink( _propAuthor, folder );

            _storage.Close();
            _storage = new TestResourceStore( true );

            _workspaceManager = new WorkspaceManager( _storage, new ResourceTreeManager( _storage ),
                _core.PluginLoader );

            IResourceList workspaces = _workspaceManager.GetResourceWorkspaces( person );
            Assert.AreEqual( 1, workspaces.Count );
            Assert.AreEqual( "Test", workspaces [0].DisplayName );
        }

        [Test] public void MultipleCategories()
        {
            IResource person = _storage.NewResource( "Person" );
            IResource category = CategoryManager.CreateCategory( "Test", _categoryManager.RootCategory );
            IResource category2 = CategoryManager.CreateCategory( "Test2", _categoryManager.RootCategory );
            IResource workspace = _workspaceManager.CreateWorkspace( "Test ");
            _workspaceManager.AddResourceToWorkspace( workspace, category );
            _workspaceManager.AddResourceToWorkspace( workspace, category2 );
            _categoryManager.AddResourceCategory( person, category );
            _categoryManager.AddResourceCategory( person, category2 );
            _workspaceManager.RemoveResourceFromWorkspace( workspace, category );
            Assert.IsTrue( person.HasLink( _workspaceManager.Props.WorkspaceVisible, workspace ) );
            _workspaceManager.RemoveResourceFromWorkspace( workspace, category2 );
            Assert.IsFalse( person.HasLink( _workspaceManager.Props.WorkspaceVisible, workspace ) );
        }
    }
}
