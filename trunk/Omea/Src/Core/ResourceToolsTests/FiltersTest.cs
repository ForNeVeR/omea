/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using  System;
using  JetBrains.Omea.PicoCore;
using  NUnit.Framework;
using  JetBrains.Omea.OpenAPI;
using  JetBrains.Omea.FiltersManagement;
using  JetBrains.Omea.ResourceTools;

namespace FilterManagerTests
{
    [TestFixture]
    public class FilterManagerTest
    {
        private IResourceStore _storage;
        private IResource   _emailResource, _newsResource, _rssResource;
        private IResource   category1, category2, category3;
        private FilterRegistry _registry;
        private FilterEngine _engine;
        private UnreadManager _unreads;
        private IWorkspaceManager _wsManager;
        private MockResourceTabProvider _mockResourceTabProvider;
        private TestCore _core;

        #region Setup
        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;

            CreateNecessaryResources();

            _registry = _core.FilterRegistry as FilterRegistry;
            _engine = _core.FilterEngine as FilterEngine;
            _wsManager = _core.WorkspaceManager;
            _unreads = _core.UnreadManager as UnreadManager;
            _mockResourceTabProvider = _core.GetComponentInstanceOfType( typeof(MockResourceTabProvider) ) as MockResourceTabProvider;
            _unreads.RegisterUnreadCountProvider( FilterManagerProps.ViewResName, new ViewUnreadCountProvider() );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }
        #endregion Setup

        [Test] public void SeveralResTypeChosenForTab()
        {
            IResource condition = _registry.RecreateStandardCondition( "Today", "Today", null, "Date", ConditionOp.InRange, "Today", "+1" );
            IResource today = _registry.RegisterView( "Today", new IResource[ 1 ]{ condition }, null );
            _engine.InitializeCriteria();
            //Console.WriteLine( "After view is initialized: " + _unreads.GetUnreadCount( today ) + " + " + _unreads.GetPersistentUnreadCount( today ) );
            Assert.AreEqual( 3, Core.UnreadManager.GetUnreadCount( today ) );

            _mockResourceTabProvider.SetResourceTab( "Email", "MyTab" );
            _mockResourceTabProvider.SetResourceTab( "RSSFeed", "MyTab" );

            UnreadState myTabState = _unreads.SetUnreadState( "MyTab", null );
            /*
            _registry.UpdateCountersOnTab( new string[2] { "Email", "RSSFeed" } );
            Console.WriteLine( "After Tab is set to Email and RSSFed: " + _unreads.GetUnreadCount( today ) + " + " + _unreads.GetPersistentUnreadCount( today ) );
            */
            Assert.AreEqual( 2, myTabState.GetUnreadCount( today ) );
            Console.WriteLine( "(AUX): " + _unreads.GetUnreadCount( today ) + " + " + _unreads.GetPersistentUnreadCount( today ) );

            /*
            _registry.UpdateCountersOnTab( null );
            Console.WriteLine( _unreads.GetUnreadCount( today ) + " + " + _unreads.GetPersistentUnreadCount( today ) );
            Assert.AreEqual( 3, _unreads.GetUnreadCount( today ) );

            _registry.UpdateCountersOnTab( new string[2] { "Email", "NewsArticle" } );
            Assert.AreEqual( 2, _unreads.GetUnreadCount( today ) );
            */
        }

        [Test] public void MarkResourceRead()
        {
            IResource condition = _registry.RecreateStandardCondition( "Today", "Today", null, "Date", ConditionOp.InRange, "Today", "+1" );
            IResource today = _registry.RegisterView( "Today", new IResource[ 1 ]{ condition }, null );
            _engine.InitializeCriteria();

            UnreadState defaultUnreadState = _unreads.CurrentUnreadState;

            //  select one type of resources, change unread state for the
            //  resource of anothere type, and viewable count must not change
            _mockResourceTabProvider.SetResourceTab( "Email", "Email" );
            UnreadState emailUnreadState = _unreads.SetUnreadState( "Email", null );
            Assert.AreEqual( 1, emailUnreadState.GetUnreadCount( today ) );
            _emailResource.SetProp( "IsUnread", false );
            Assert.AreEqual( 0, emailUnreadState.GetUnreadCount( today ) );

            Console.WriteLine( "Current count = " + _unreads.GetUnreadCount( today ));
            Assert.AreEqual( 2, defaultUnreadState.GetUnreadCount( today ) );
            _rssResource.SetProp( "IsUnread", false );
            Assert.AreEqual( 1, defaultUnreadState.GetUnreadCount( today ) );
        }

        [Test] public void UnreadCountersForViewsInWorkspace()
        {
            Core.ResourceStore.ResourceTypes.Register( "Folder", "Name" );
            int propInFolder = Core.ResourceStore.PropTypes.Register( "InFolder", PropDataType.Link );
            _wsManager.RegisterWorkspaceType( "Folder", new int[] { propInFolder },
                WorkspaceResourceType.Container );

            IResource condition = _registry.RecreateStandardCondition( "Today", "Today", null, "Date", ConditionOp.InRange, "Today", "+1" );
            IResource today = _registry.RegisterView( "Today", new IResource[ 1 ]{ condition }, null );
            _engine.InitializeCriteria();

            IResource ws = _wsManager.CreateWorkspace( "Test" );
            IResource folder = _storage.NewResource( "Folder" );
            _wsManager.AddResourceToWorkspace( ws, folder );

            UnreadState theState = _unreads.SetUnreadState( "", ws );
            Assert.AreEqual( 0, theState.GetUnreadCount( today ) );

            _emailResource.AddLink( propInFolder, folder );
            Assert.AreEqual( 1, theState.GetUnreadCount( today ) );
        }

        [Test] public void TestDirectSetCondition()
        {
            IResourceList   categories = category1.ToResourceList();
            categories = categories.Union( category2.ToResourceList() );
            categories = categories.Union( category3.ToResourceList() );
            IResource condition = _registry.RecreateStandardCondition( "Available", "DeepName", null, "Category", ConditionOp.In, categories );
            IResource available = _registry.RegisterView( "Available", new IResource[ 1 ]{ condition }, null );
            _engine.InitializeCriteria();

            IResource test1 = _storage.BeginNewResource( "Email" );
            test1.SetProp( "Name", "Email 1" );
            test1.SetProp( "Date", DateTime.Now );
            test1.SetProp( "Category", category1 );
            test1.EndUpdate();

            IResource test2 = _storage.BeginNewResource( "Email" );
            test2.SetProp( "Name", "Email 2" );
            test2.SetProp( "Date", DateTime.Now );
            test2.SetProp( "Category", category2 );
            test2.SetProp( "Category", category3 );
            test2.EndUpdate();

            IResourceList result = _engine.ExecView( available );
            Assert.AreEqual( 2, result.Count, "Illegal number of matched objects" );
        }

        [Test] public void TestConditionOnSeveralResourceTypes()
        {
            IResource x = _registry.RecreateStandardCondition( "New", "DeepName", new string[ 2 ]{ "Email", "Category" }, "Date", ConditionOp.InRange, "Today", "+1" );
            IResource test = _registry.RegisterView( "TestNew", new string[ 2 ]{ "Email", "Category" }, new IResource[ 1 ]{ x }, null );
            _engine.InitializeCriteria();

            IResourceList result = _engine.ExecView( test );
            Assert.AreEqual( 4, result.Count, "Illegal number of matched objects" );
        }

        private void CreateNecessaryResources()
        {
            _storage.ResourceTypes.Register( "Email", "", ResourceTypeFlags.Normal );
            _storage.ResourceTypes.Register( "NewsArticle", "", ResourceTypeFlags.Normal );
            _storage.ResourceTypes.Register( "RSSFeed", "", ResourceTypeFlags.Normal );
            _storage.ResourceTypes.Register( "Category", "", ResourceTypeFlags.Normal );

            _storage.PropTypes.Register( "IsUnread", PropDataType.Bool, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "Date", PropDataType.Date, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "UnreadCount", PropDataType.Int, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "Category", PropDataType.Link, PropTypeFlags.Normal );
            _storage.PropTypes.Register( "DeepName", PropDataType.String, PropTypeFlags.Normal );

            _emailResource = _storage.BeginNewResource( "Email" );
            _emailResource.SetProp( "IsUnread", true );
            _emailResource.SetProp( "Date", DateTime.Now );
            _emailResource.EndUpdate();

            _newsResource = _storage.BeginNewResource( "NewsArticle" );
            _newsResource.SetProp( "IsUnread", true );
            _newsResource.SetProp( "Date", DateTime.Now );
            _newsResource.EndUpdate();

            _rssResource = _storage.BeginNewResource( "RSSFeed" );
            _rssResource.SetProp( "IsUnread", true );
            _rssResource.SetProp( "Date", DateTime.Now );
            _rssResource.EndUpdate();

            category1 = _storage.BeginNewResource( "Category" );
            category1.SetProp( "Name", "Category 1" );
            category1.SetProp( "Date", DateTime.Now );
            category1.EndUpdate();

            category2 = _storage.BeginNewResource( "Category" );
            category2.SetProp( "Name", "Category 2" );
            category2.SetProp( "Date", DateTime.Now );
            category2.EndUpdate();

            category3 = _storage.BeginNewResource( "Category" );
            category3.SetProp( "Name", "Category 2" );
            category3.SetProp( "Date", DateTime.Now );
            category3.EndUpdate();
        }
    }
}
