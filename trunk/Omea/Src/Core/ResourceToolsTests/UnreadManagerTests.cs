/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using JetBrains.Omea.ResourceTools;
using NUnit.Framework;

namespace ResourceToolsTests
{
	/**
	 * Unit tests for the UnreadManager class.
	 */
    
    [TestFixture]
    public class UnreadManagerTests
	{
        private UnreadManager _unreadManager;
        private IWorkspaceManager _wsManager;
        private TestCore _core;
        private IResourceStore _storage;
        private int _propFolder;
        private IResource _folder;
        private int _propUnread;
        private int _propSize;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = Core.ResourceStore;

            _storage.ResourceTypes.Register( "Folder", "Name" );
            _storage.ResourceTypes.Register( "Email", "Name" );
            _storage.ResourceTypes.Register( "Person", "Name" );
            _propFolder = _storage.PropTypes.Register( "Folder", PropDataType.Link, 
                PropTypeFlags.CountUnread | PropTypeFlags.DirectedLink );

            _folder = _storage.NewResource( "Folder" );

            _wsManager = _core.WorkspaceManager;

            MockResourceTabProvider resourceTabProvider = (MockResourceTabProvider) _core.GetComponentInstanceOfType( typeof(MockResourceTabProvider) );
            resourceTabProvider.SetResourceTab( "Email", "Email" );

            _unreadManager = _core.UnreadManager as UnreadManager;
            _propUnread = Core.Props.IsUnread;
            _propSize = _storage.PropTypes.Register( "Size", PropDataType.Int );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        private IResource NewUnreadResource( string type, IResource folder )
        {
            IResource res = _storage.NewResource( type );
            res.SetProp( _propUnread, true );
            res.AddLink( _propFolder, folder );
            return res;
        }

        [Test] public void SimpleTest()
        {
            IResource email = _storage.NewResource( "Email" );
            email.AddLink( _propFolder, _folder );

            Assert.AreEqual( 0, _unreadManager.GetUnreadCount( _folder ) );
            
            email.SetProp( _propUnread, true );
            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );

            email.SetProp( _propSize, 100 );
            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );

            email.SetProp( _propUnread, false );
            Assert.AreEqual( 0, _unreadManager.GetUnreadCount( _folder ) );
        }

        [Test] public void UnreadAndLink()
        {
            IResource email = _storage.BeginNewResource( "Email" );
            email.SetProp( _propUnread, true );
            email.AddLink( _propFolder, _folder );
            email.EndUpdate();

            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );

            IResource email2 = _storage.BeginNewResource( "Email" );
            email2.AddLink( _propFolder, _folder );
            email2.SetProp( _propUnread, true );
            email2.EndUpdate();

            Assert.AreEqual( 2, _unreadManager.GetUnreadCount( _folder ) );

            email.BeginUpdate();
            email.SetProp( _propUnread, false );
            email.DeleteLink( _propFolder, _folder );
            email.EndUpdate();
            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );

            email2.Delete();
            Assert.AreEqual( 0, _unreadManager.GetUnreadCount( _folder ) );
        }

        [Test] public void UnreadAndReverseLink()
        {
            IResource email = _storage.BeginNewResource( "Email" );
            email.SetProp( _propUnread, true );
            _folder.AddLink( _propFolder, email );
            email.EndUpdate();

            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );
        }

        [Test] public void UnreadOnOff()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propUnread, true );
            _folder.AddLink( _propFolder, email );

            email.BeginUpdate();
            email.SetProp( _propUnread, false );
            email.SetProp( _propUnread, true );
            email.EndUpdate();

            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );

            IResource email2 = _storage.NewResource( "Email" );
            _folder.AddLink( _propFolder, email2 );

            email2.BeginUpdate();
            email2.SetProp( _propUnread, true );
            email2.SetProp( _propUnread, false );
            email2.EndUpdate();

            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );
        }

        [Test] public void UnreadState()
        {
            Assert.AreEqual( 0, _unreadManager.GetUnreadCount( _folder ) );

            IResource email = NewUnreadResource( "Email", _folder );
            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );

            IResource person = NewUnreadResource( "Person", _folder );
            Assert.AreEqual( 2, _unreadManager.GetUnreadCount( _folder ) );

            UnreadState emailState = _unreadManager.SetUnreadState( "Email", null );
            Assert.AreEqual( 1, emailState.GetUnreadCount( _folder ) );

            UnreadState defaultState = _unreadManager.SetUnreadState( "", null );
            Assert.AreEqual( 2, defaultState.GetUnreadCount( _folder ) );

            IResource person2 = NewUnreadResource( "Person", _folder );
            Assert.AreEqual( 3, defaultState.GetUnreadCount( _folder ) );
            Assert.AreEqual( 1, emailState.GetUnreadCount( _folder ) );

            IResource email2 = NewUnreadResource( "Email", _folder );
            Assert.AreEqual( 4, defaultState.GetUnreadCount( _folder ) );
            Assert.AreEqual( 2, emailState.GetUnreadCount( _folder ) );

            email2.SetProp( _propUnread, false );
            Assert.AreEqual( 3, defaultState.GetUnreadCount( _folder ) );
            Assert.AreEqual( 1, emailState.GetUnreadCount( _folder ) );

            person2.SetProp( _propUnread, false );
            Assert.AreEqual( 2, defaultState.GetUnreadCount( _folder ) );
            Assert.AreEqual( 1, emailState.GetUnreadCount( _folder ) );

            email2.SetProp( _propUnread, true );
            Assert.AreEqual( 3, defaultState.GetUnreadCount( _folder ) );
            Assert.AreEqual( 2, emailState.GetUnreadCount( _folder ) );

            person2.SetProp( _propUnread, true );
            Assert.AreEqual( 4, defaultState.GetUnreadCount( _folder ) );
            Assert.AreEqual( 2, emailState.GetUnreadCount( _folder ) );

            email2.Delete();
            Assert.AreEqual( 3, defaultState.GetUnreadCount( _folder ) );
            Assert.AreEqual( 1, emailState.GetUnreadCount( _folder ) );

            person2.Delete();
            Assert.AreEqual( 2, defaultState.GetUnreadCount( _folder ) );
            Assert.AreEqual( 1, emailState.GetUnreadCount( _folder ) );

            IResource folder2 = _storage.NewResource( "Folder" );
            Assert.AreEqual( 0, defaultState.GetUnreadCount( folder2 ) );
            Assert.AreEqual( 0, emailState.GetUnreadCount( folder2 ) );
        }

        [Test] public void UnreadResourceEntersWorkspace()
        {
            Core.ResourceStore.ResourceTypes.Register( "Folder", "Name" );
            int propInFolder = Core.ResourceStore.PropTypes.Register( "InFolder", PropDataType.Link,
                PropTypeFlags.CountUnread );
            _wsManager.RegisterWorkspaceType( "Folder", new int[] { propInFolder },
                WorkspaceResourceType.Container );

            IResource ws = _wsManager.CreateWorkspace( "Test" );
            IResource folder = _storage.NewResource( "Folder" );
            _wsManager.AddResourceToWorkspace( ws, folder );

            UnreadState theState = _unreadManager.SetUnreadState( "", ws );
            Assert.AreEqual( 0, theState.GetUnreadCount( folder ) );

            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propUnread, true );

            Assert.AreEqual( 0, theState.GetUnreadCount( folder ) );
            email.AddLink( "InFolder", folder );
            Assert.AreEqual( 1, theState.GetUnreadCount( folder ) );

            email.DeleteLink( "InFolder", folder );
            Assert.AreEqual( 0, theState.GetUnreadCount( folder ) );
        }

        [Test] public void UnreadDeletedResource()
        {
            IResource email = _storage.NewResource( "Email" );
            email.AddLink( _propFolder, _folder );

            Assert.AreEqual( 0, _unreadManager.GetUnreadCount( _folder ) );
            
            email.SetProp( _propUnread, true );
            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );

            email.SetProp( Core.Props.IsDeleted, true );
            Assert.AreEqual( 0, _unreadManager.GetUnreadCount( _folder ) );

            email.SetProp( Core.Props.IsDeleted, false );
            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );
        }

        [Test] public void LinkDeletedUnreadResourceToFolder()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propUnread, true );
            email.SetProp( Core.Props.IsDeleted, true );
            email.AddLink( _propFolder, _folder );

            Assert.AreEqual( 0, _unreadManager.GetUnreadCount( _folder ) );
        }

        [Test] public void DeleteDeletedUnreadResource()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propUnread, true );
            email.SetProp( Core.Props.IsDeleted, true );
            email.AddLink( _propFolder, _folder );

            IResource email2 = NewUnreadResource( "Email", _folder );
            email.Delete();
            Assert.AreEqual( 1, _unreadManager.GetUnreadCount( _folder ) );
        }

        [Test] public void DeletedResourcesInUnreadState()
        {
            IResource email = NewUnreadResource( "Email", _folder );
            email.SetProp( Core.Props.IsDeleted, true );
            IResource email2 = NewUnreadResource( "Email", _folder );

            UnreadState emailState = _unreadManager.SetUnreadState( "Email", null );
            Assert.AreEqual( 1, emailState.GetUnreadCount( _folder ) );
        }
	}
}
