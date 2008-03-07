/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using NUnit.Framework;

using JetBrains.Omea.Categories;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;

namespace ResourceToolsTests
{
    /**
     * Tests for the CategoryTools class.
     */
    
    [TestFixture] public class CategoryTests
    {
        private TestCore _environment;
        private CategoryManager _categoryManager;
        private IResourceStore _storage;

        [SetUp] public void SetUp()
        {
            _environment = new TestCore();
            _storage = _environment.ResourceStore;
        	_categoryManager = (CategoryManager) _environment.CategoryManager;
            CreateDefaultResources();
        }

        [TearDown] public void TearDown()
        {
            _environment.Dispose();
        }

        private IResource _cat1;
        private IResource _cat2;
        private IResource _cat3;
        private IResource _cat4;
        private IResource _person;
        private IResource _person2;
        private IResource _person3;
        private IResource _person4;

        private void CreateDefaultResources()
        {
            _storage.ResourceTypes.Register( "Person", "Name" );

            _cat1 = _storage.NewResource( "Category" );
            _cat2 = _storage.NewResource( "Category" );
            _cat3 = _storage.NewResource( "Category" );
            _cat4 = _storage.NewResource( "Category" );
            _person = _storage.NewResource( "Person" );
            _person2 = _storage.NewResource( "Person" );
            _person3 = _storage.NewResource( "Person" );
            _person4 = _storage.NewResource( "Person" );
        }
    
        [Test] public void CategoryExists()
        {
            Assert.IsFalse( _categoryManager.CategoryExists( _categoryManager.RootCategory, "Test" ) );
        	CategoryManager.CreateCategory( "Test", _categoryManager.RootCategory );
            Assert.IsTrue( _categoryManager.CategoryExists( _categoryManager.RootCategory, "Test" ) );
        }

        [Test] public void ResourcesInCategories()
        {
            ArrayList catList = new ArrayList();
            catList.Add( _cat1 );
            catList.Add( _cat2 );
            catList.Add( _cat3 );

            IResourceList resList = CategoryManager.GetResourcesInCategories( catList );
            Assert.AreEqual( 0, resList.Count );

        	_categoryManager.AddResourceCategory( _person, _cat1 );
            Assert.AreEqual( 0, resList.Count );

        	_categoryManager.AddResourceCategory( _person, _cat2 );
            Assert.AreEqual( 0, resList.Count );

        	_categoryManager.AddResourceCategory( _person, _cat3 );
            Assert.AreEqual( 1, resList.Count );

        	_categoryManager.RemoveResourceCategory( _person, _cat2 );
            Assert.AreEqual( 0, resList.Count );
        }

        [Test] public void DeleteCategoryRecursive()
        {
            IResource category = CategoryManager.CreateCategory( "Test", _categoryManager.RootCategory );
            IResource category2 = CategoryManager.CreateCategory( "Test2", category );
        	CategoryManager.DeleteCategory( category );
            Assert.IsTrue( category2.IsDeleted );
        }
    }
}
