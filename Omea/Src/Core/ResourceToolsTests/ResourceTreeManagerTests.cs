/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace ResourceToolsTests
{
    /// <summary>
    /// Unit tests for the ResourceTreeManager class.
    /// </summary>
    [TestFixture]
    public class ResourceTreeManagerTests
	{
        private TestCore _core;
        private IResourceStore _storage;
        private IResourceTreeManager _manager;
        
        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            _manager = _core.ResourceTreeManager;

            _storage.ResourceTypes.Register( "Folder", "Name" );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        [Test] public void InheritSorting()
        {
            IResource root = _manager.GetRootForType( "Folder" );
            _manager.SetResourceNodeSort( root, "Name" );

            IResource folder = _storage.NewResource( "Folder" );
            folder.SetProp( _core.Props.Parent, root );
            Assert.AreEqual( "Name", _manager.GetResourceNodeSort( folder ) );
        }

        [Test] public void ListenerAdd()
        {
            IResource folder = _storage.NewResource( "Folder" );
            MockResourceListListener listener = new MockResourceListListener();
            _manager.RegisterTreeListener( folder, Core.Props.Parent, listener );
            IResource folder2 = _storage.NewResource( "Folder" );
            folder2.AddLink( Core.Props.Parent, folder );
            Assert.AreEqual( 1, listener._addedResources.Count );
            Assert.AreEqual( folder2, listener._addedResources [0] );
        }

        [Test] public void ListenerUnregister()
        {
            IResource folder = _storage.NewResource( "Folder" );
            MockResourceListListener listener = new MockResourceListListener();
            _manager.RegisterTreeListener( folder, Core.Props.Parent, listener );
            _manager.UnregisterTreeListener( folder, Core.Props.Parent, listener );
            IResource folder2 = _storage.NewResource( "Folder" );
            folder2.AddLink( Core.Props.Parent, folder );
            Assert.AreEqual( 0, listener._addedResources.Count );
        }

        [Test] public void ListenerChange()
        {
            IResource folder = _storage.NewResource( "Folder" );
            IResource folder2 = _storage.NewResource( "Folder" );
            folder2.AddLink( Core.Props.Parent, folder );
            MockResourceListListener listener = new MockResourceListListener();
            _manager.RegisterTreeListener( folder, Core.Props.Parent, listener );
            folder2.SetProp( Core.Props.Name, "1" );
            Assert.AreEqual( 1, listener._changedResources.Count );
            Assert.AreEqual( folder2, listener._changedResources [0] );
        }

        [Test] public void ListenerChangeParent()
        {
            IResource folder = _storage.NewResource( "Folder" );
            IResource folder2 = _storage.NewResource( "Folder" );
            IResource folder3 = _storage.NewResource( "Folder" );
            folder2.AddLink( Core.Props.Parent, folder3 );
            
            MockResourceListListener listener = new MockResourceListListener();
            MockResourceListListener listener2 = new MockResourceListListener();
            _manager.RegisterTreeListener( folder, Core.Props.Parent, listener );
            _manager.RegisterTreeListener( folder3, Core.Props.Parent, listener2 );
            
            folder2.SetProp( Core.Props.Parent, folder );
            
            Assert.AreEqual( 1, listener._addedResources.Count );
            Assert.AreEqual( folder2, listener._addedResources [0] );
            Assert.AreEqual( 0, listener._changedResources.Count );

            Assert.AreEqual( 1, listener2._removedResources.Count );
        }

        [Test] public void ListenerDeleteLink()
        {
            IResource folder = _storage.NewResource( "Folder" );
            MockResourceListListener listener = new MockResourceListListener();
            IResource folder2 = _storage.NewResource( "Folder" );
            folder2.AddLink( Core.Props.Parent, folder );

            _manager.RegisterTreeListener( folder, Core.Props.Parent, listener );
            folder2.DeleteLink( Core.Props.Parent, folder );
            Assert.AreEqual( 1, listener._removedResources.Count );
            Assert.AreEqual( folder2, listener._removedResources [0] );
        }

        private class MockResourceListListener: IResourceListListener
        {
            internal ArrayList _addedResources = new ArrayList();
            internal ArrayList _changedResources = new ArrayList();
            internal ArrayList _removedResources = new ArrayList();
            
            public void ResourceAdded( IResource res )
            {
                _addedResources.Add( res );
            }

            public void ResourceDeleting( IResource res )
            {
                _removedResources.Add( res );
            }

            public void ResourceChanged( IResource res, IPropertyChangeSet cs )
            {
                _changedResources.Add( res );
            }
        }
	}
}
