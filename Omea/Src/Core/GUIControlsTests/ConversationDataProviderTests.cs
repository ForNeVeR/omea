// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using JetBrains.Omea.ResourceTools;
using NUnit.Framework;

namespace GUIControlsTests
{
	[TestFixture]
    public class ConversationDataProviderTests
	{
	    private TestCore _core;
        private ResourceListView2 _resourceListView;
        private ConversationDataProvider _dataProvider;
        private JetListView _listView;

        private int _propSubject;
        private int _propDate;
        private int _propReply;
        private int _propUnread;
	    private ResourceListView2Column _dateColumn;
        private DefaultThreadingHandler _threadingHandler;

	    [SetUp] public void SetUp()
        {
            _core = new TestCore();

            _propSubject = _core.ResourceStore.PropTypes.Register( "Subject", PropDataType.String );
            _propDate = _core.ResourceStore.PropTypes.Register( "Date", PropDataType.Date );
            _propReply = _core.ResourceStore.PropTypes.Register( "Reply", PropDataType.Link, PropTypeFlags.DirectedLink );
            _propUnread = _core.ResourceStore.PropTypes.Register( "IsUnread", PropDataType.Bool );

            _core.ResourceStore.ResourceTypes.Register( "Email", "Subject" );
            _core.ResourceStore.ResourceTypes.Register( "Person", "Name" );

            _threadingHandler = new DefaultThreadingHandler( _propReply );

            _resourceListView = new ResourceListView2();
        }

        [TearDown] public void TearDown()
        {
            _resourceListView.Dispose();
            _core.Dispose();
        }

        private IResource CreateEmail( string subject, DateTime date, IResource replyParent, bool unread )
        {
            IResource email = _core.ResourceStore.BeginNewResource( "Email" );
            email.SetProp( "Subject", subject );
            email.SetProp( "Date", date );
            if ( replyParent != null )
            {
                email.AddLink( "Reply", replyParent );
            }
            if ( unread )
            {
                email.SetProp( _propUnread, true );
            }
            email.EndUpdate();
            return email;
        }

        private void ShowThreadedEmails()
        {
            ShowThreadedEmails( Core.ResourceStore.GetAllResourcesLive( "Email" ) );
        }

        private void ShowThreadedEmails( IResourceList resourceList )
        {
            _dataProvider = new ConversationDataProvider( resourceList, _threadingHandler );
            _dataProvider.SetInitialSort( new SortSettings( _propDate, true ) );

            _listView = _resourceListView.JetListView;
            _dateColumn = new ResourceListView2Column( new int[] { _propDate } );
            _listView.Columns.Add( _dateColumn );

            _resourceListView.DataProvider = _dataProvider;
        }

        [Test] public void TestConversations()
        {
            IResource email = CreateEmail( "Test", DateTime.Now, null, false );
            ShowThreadedEmails();
            Assert.AreEqual( 1, _listView.Nodes.Count );
            Assert.AreEqual( CollapseState.NoChildren, _listView.Nodes [0].CollapseState );
        }

        [Test] public void TestConversationCollapsed()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, false );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -5 ), email1, true );

            ShowThreadedEmails();
            Assert.AreEqual( 1, _listView.Nodes.Count );
            JetListViewNode node = _listView.Nodes [0];
            Assert.AreEqual( CollapseState.Collapsed, node.CollapseState );

            node.Expanded = true;
            Assert.AreEqual( 1, node.Nodes.Count );
        }

        [Test] public void AddNewThread()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, false );

            ShowThreadedEmails();

            IResource email2 = CreateEmail( "Email2", DateTime.Now, null, false );

            Assert.AreEqual( 2, _listView.Nodes.Count );
            Assert.AreEqual( email1, _listView.Nodes [0].Data );
            Assert.AreEqual( email2, _listView.Nodes [1].Data );
        }

        [Test] public void AddNewThreadStart()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now, null, true );

            ShowThreadedEmails();

            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddMinutes( -10.0 ), null, true );

            Assert.AreEqual( 2, _listView.Nodes.Count );
            Assert.AreEqual( email2, _listView.Nodes [0].Data );
            Assert.AreEqual( email1, _listView.Nodes [1].Data );
        }

        [Test] public void AddToCollapsedThread()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, false );

            ShowThreadedEmails();
            Assert.AreEqual( 1, _listView.Nodes.Count );
            //AssertNoStateIcon( 0 );
            //Assert.AreEqual( FontStyle.Regular, _listView.Items [0].FontStyle );

            // [ ] email1

            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -2 ), email1, true );

            Assert.AreEqual( 1, _listView.Nodes.Count );
            //AssertBoldPlusStateIcon( 0 );
            Assert.IsFalse( _listView.Nodes [0].Expanded );
            //Assert.AreEqual( FontStyle.Regular, _listView.Items [0].FontStyle );

            // [+] email1

            /*
            email2.SetProp( _propUnread, false );
            _resourceBrowser.ThreadProcessPendingUpdates();
            AssertPlusStateIcon( 0 );
            Assert.AreEqual( FontStyle.Regular, _listView.Items [0].FontStyle );

            email2.SetProp( _propUnread, true );
            */
            _listView.Nodes [0].Expanded = true;
            /*
            AssertMinusStateIcon( 0 );
            Assert.AreEqual( FontStyle.Regular, _listView.Items [0].FontStyle );
            Assert.AreEqual( FontStyle.Bold, _listView.Items [1].FontStyle );
            */

            // [-] email1
            //   [ ] email2

            _listView.Nodes [0].Expanded = false;
            /*
            Assert.AreEqual( FontStyle.Regular, _listView.Items [0].FontStyle );
            AssertBoldPlusStateIcon( 0 );
            */

            // [+] email1
            //   (email2)

            IResource email3 = CreateEmail( "Email3", DateTime.Now, email1, true );
            Assert.AreEqual( 1, _listView.Nodes.Count );

            // [+] email1
            //   (email2)
            //   (email3)

            IResource email4 = CreateEmail( "Email4", DateTime.Now, email2, true );
            Assert.AreEqual( 1, _listView.Nodes.Count );

            // [+] email1
            //   (email2)
            //     (email4)
            //   (email3)

            _listView.Nodes [0].Expanded = true;
            //Assert.AreEqual( 4, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email2, _listView.Nodes [0].Nodes [0].Data );
            Assert.AreEqual( email4, _listView.Nodes [0].Nodes [0].Nodes [0].Data  );
            Assert.AreEqual( email3, _listView.Nodes [0].Nodes [1].Data );
        }

        [Test] public void AddToCollapsedThreadChild()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, false );
            IResource email2 = CreateEmail( "Email2", DateTime.Now, email1, false );

            ShowThreadedEmails();

            IResource email3 = CreateEmail( "Email3", DateTime.Now, email2, false );
            Assert.AreEqual( 1, _listView.NodeCollection.VisibleItemCount );

            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( 3, _listView.NodeCollection.VisibleItemCount );
        }

        [Test] public void AddToExpandedThread()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, false );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -5 ), email1, false );

            ShowThreadedEmails();
            _listView.Nodes [0].Expanded = true;

            Assert.AreEqual( 2, _listView.NodeCollection.VisibleItemCount );

            // [-] email1
            //   email2

            IResource email3 = CreateEmail( "Email3", DateTime.Now, email1, false );
            Assert.AreEqual( 3, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email2, _listView.Nodes [0].Nodes [0].Data );
            Assert.AreEqual( email3, _listView.Nodes [0].Nodes [1].Data );

            // [-] email1
            //   email2
            //   email3

            //_listView.Items [0].Expanded = false;
            //AssertPlusStateIcon( 0 );

            // [+] email1
            //   (email2)
            //   (email3)

            /*
            CreateEmail( "Email4", DateTime.Now, email1, true );
            _resourceBrowser.ThreadProcessPendingUpdates();
            AssertBoldPlusStateIcon( 0 );
            */

            // [+] email1
            //   (email2)
            //   (email3)
            //   (* email4)
        }

        [Test] public void AddToSeveralExpandedThreads()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, false );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -9 ), email1, false );
            IResource email3 = CreateEmail( "Email3", DateTime.Now.AddSeconds( -8 ), email2, false );

            IResource email11 = CreateEmail( "Email11", DateTime.Now.AddSeconds( -7 ), null, false );
            IResource email12 = CreateEmail( "Email12", DateTime.Now.AddSeconds( -6 ), email11, false );
            IResource email13 = CreateEmail( "Email13", DateTime.Now.AddSeconds( -5 ), email12, false );

            ShowThreadedEmails();
            _listView.Nodes [1].Expanded = true;
            _listView.Nodes [0].Expanded = true;

            Assert.AreEqual( 6, _listView.NodeCollection.VisibleItemCount );

            // [-] email1
            //   [-] email2
            //     email3
            // [-] email11
            //   [-] email12
            //     email13

            IResource email4 = CreateEmail( "Email4", DateTime.Now,  email3, false );
            Assert.AreEqual( 7, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email4, _listView.Nodes [0].Nodes [0].Nodes [0].Nodes [0].Data );
            Assert.AreEqual( email11, _listView.Nodes [1].Data );

            // [-] email1
            //   [-] email2
            //      [-] email3
            //            email4
            // [-] email11
            //   [-] email12
            //         email13
        }

        [Test] public void AddToMiddleOfThread()  // #5867
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, false );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -9 ), email1, false );
            IResource email3 = CreateEmail( "Email3", DateTime.Now.AddSeconds( -8 ), email2, false );

            ShowThreadedEmails();
            _listView.Nodes [0].Expanded = true;

            // [-] email1
            //   [-] email2
            //         email3

            IResource email4 = CreateEmail( "Email4", DateTime.Now,  email1, false );
            Assert.AreEqual( 4, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email4, _listView.Nodes [0].Nodes [1].Data );

            // [-] email1
            //   [-] email2
            //         email3
            //       email4
        }

        [Test] public void AddChildWrongType()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, false );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -9 ), email1, false );
            IResource email11 = CreateEmail( "Email11", DateTime.Now.AddSeconds( -7 ), null, false );

            IResource nonEmail = _core.ResourceStore.NewResource( "Person" );
            nonEmail.SetProp( "Date", DateTime.Now.AddSeconds( -5 ) );
            nonEmail.AddLink( "Reply", email2 );

            ShowThreadedEmails();
            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( 3, _listView.NodeCollection.VisibleItemCount );

            IResource email3 = CreateEmail( "Email3", DateTime.Now, email2, false );
            Assert.AreEqual( 4, _listView.NodeCollection.VisibleItemCount );
            //Assert.AreEqual( "Email3", _listView.Items [2].Text );
            //Assert.AreEqual( "Email11", _listView.Items [3].Text );
        }

        [Test] public void AddWithSkippedNode()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, true );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -5 ), email1, false );

            ShowThreadedEmails( _core.ResourceStore.FindResourcesLive( "Email", "IsUnread", true ) );

            Assert.AreEqual( 1, _listView.NodeCollection.VisibleItemCount );

            IResource email3 = CreateEmail( "Email3", DateTime.Now, email2, true );
            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( 2, _listView.NodeCollection.VisibleItemCount );
        }

        [Test] public void AddWithSkippedRoot()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, false );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -6 ), email1, true );
            IResource email3 = CreateEmail( "Email3", DateTime.Now.AddSeconds( -5 ), email1, true );
            IResource email4 = CreateEmail( "Email4", DateTime.Now.AddSeconds( -5 ), email2, true );

            ShowThreadedEmails( _core.ResourceStore.FindResourcesLive( "Email", "IsUnread", true ) );

            Assert.AreEqual( 2, _listView.NodeCollection.VisibleItemCount );

            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( 3, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email4, _listView.Nodes [0].Nodes [0].Data );

            /*
            _resourceBrowser.GotoNextUnread();
            Assert.AreEqual( 1, _listView.SelectedItems.Count );
            Assert.AreEqual( email2.Id, _listView.SelectedItems [0].Resource.Id );
            */
        }

        [Test] public void AddWithHiddenParent() // #6376
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, false );
            ShowThreadedEmails( _core.ResourceStore.FindResourcesLive( "Email", "IsUnread", true ) );

            Assert.AreEqual( 0, _listView.NodeCollection.VisibleItemCount );

            IResource email2 = CreateEmail( "Email2", DateTime.Now, email1, true );
            Assert.AreEqual( 1, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email2, _listView.Nodes [0].Data );
        }

        [Test] public void AddParentAfterChild()
        {
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -5 ), null, true );

            ShowThreadedEmails();

            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, true );
            email2.AddLink( _propReply, email1 );

            Assert.AreEqual( 1, _listView.NodeCollection.VisibleItemCount );
            Assert.IsFalse( _listView.Nodes [0].Expanded );

            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( 2, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email1, _listView.Nodes [0].Data );
            Assert.AreEqual( email2, _listView.Nodes [0].Nodes [0].Data );
        }

        [Test] public void AddParentAfterChild_Skipped()
        {
            IResource email3 = CreateEmail( "Email3", DateTime.Now.AddSeconds( -5 ), null, true );

            ShowThreadedEmails();

            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, true );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -7 ), email1, true );
            email3.AddLink( _propReply, email2 );

            Assert.AreEqual( 1, _listView.NodeCollection.VisibleItemCount );

            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( 3, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email1, _listView.Nodes [0].Data );
            Assert.AreEqual( email2, _listView.Nodes [0].Nodes [0].Data );
            Assert.AreEqual( email3, _listView.Nodes [0].Nodes [0].Nodes [0].Data );
        }


        [Test] public void AddParentAfterChild_Expanded()
        {
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -7 ), null, true );
            IResource email3 = CreateEmail( "Email3", DateTime.Now.AddSeconds( -5 ), email2, true );

            ShowThreadedEmails();

            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( 2, _listView.NodeCollection.VisibleItemCount );

            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -10 ), null, true );
            email2.AddLink( _propReply, email1 );

            Assert.AreEqual( 1, _listView.NodeCollection.VisibleItemCount );

            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( 3, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email1, _listView.Nodes [0].Data );
            Assert.AreEqual( email2, _listView.Nodes [0].Nodes [0].Data );
            Assert.AreEqual( email3, _listView.Nodes [0].Nodes [0].Nodes [0].Data );
        }

        [Test] public void DeleteSimple()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -7 ), null, true );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -5 ), null, true );

            ShowThreadedEmails();

            Assert.AreEqual( 2, _listView.NodeCollection.VisibleItemCount );
            _listView.Selection.Add( email2 );
            email2.Delete();
            Assert.AreEqual( 1, _listView.Nodes.Count );
            Assert.AreEqual( 1, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email1, _listView.Nodes [0].Data );

            Assert.AreEqual( 1, _listView.Selection.Count );
            Assert.AreEqual( email1, _listView.Selection.ActiveNode.Data );
        }

        [Test] public void DeleteReplyParent_Expanded()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -7 ), null, true );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -5 ), email1, true );

            ShowThreadedEmails();

            Assert.AreEqual( email1, _listView.Nodes [0].Data );
            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( 2, _listView.NodeCollection.VisibleItemCount );
            email1.Delete();
            Assert.AreEqual( 1, _listView.Nodes.Count );
            Assert.AreEqual( 1, _listView.NodeCollection.VisibleItemCount );
            Assert.AreEqual( email2, _listView.Nodes [0].Data );
        }

        [Test] public void DeleteReplyChild()
        {
            IResource email1 = CreateEmail( "Email1", DateTime.Now.AddSeconds( -7 ), null, true );
            IResource email2 = CreateEmail( "Email2", DateTime.Now.AddSeconds( -5 ), email1, true );

            ShowThreadedEmails();

            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( 2, _listView.NodeCollection.VisibleItemCount );
            email2.Delete();

            Assert.AreEqual( CollapseState.NoChildren, _listView.Nodes [0].CollapseState );
        }

        [Test] public void SortTopLevelThreads()
        {
            DateTime now = DateTime.Now;
            IResource email1 = CreateEmail( "Email1", now.AddSeconds( -7 ), null, true );
            IResource email2 = CreateEmail( "Email2", now.AddSeconds( -5 ), null, true );
            IResource email3 = CreateEmail( "Email3", now.AddSeconds( -6 ), null, true );

            ShowThreadedEmails();

            Assert.AreEqual( email1, _listView.Nodes [0].Data );
            Assert.AreEqual( email3, _listView.Nodes [1].Data );
            Assert.AreEqual( email2, _listView.Nodes [2].Data );

            _dataProvider.HandeColumnClick( _dateColumn );
            Assert.AreEqual( 3, _listView.Nodes.Count );
            Assert.AreEqual( email2, _listView.Nodes [0].Data );
            Assert.AreEqual( email3, _listView.Nodes [1].Data );
            Assert.AreEqual( email1, _listView.Nodes [2].Data );
        }

        [Test] public void SortChildren()
        {
            DateTime now = DateTime.Now;
            IResource email0 = CreateEmail( "Email0", now.AddSeconds( -10 ), null, true );
            IResource email1 = CreateEmail( "Email1", now.AddSeconds( -7 ), email0, true );
            IResource email2 = CreateEmail( "Email2", now.AddSeconds( -5 ), email0, true );
            IResource email3 = CreateEmail( "Email3", now.AddSeconds( -6 ), email0, true );

            IResourceList resourceList = Core.ResourceStore.GetAllResourcesLive( "Email" );
            resourceList.Sort( new int[] { _propDate }, false );
            _dataProvider = new ConversationDataProvider( resourceList, _threadingHandler );

            _listView = _resourceListView.JetListView;
            _dateColumn = new ResourceListView2Column( new int[] { _propDate } );
            _listView.Columns.Add( _dateColumn );

            _resourceListView.DataProvider = _dataProvider;

            _listView.Nodes [0].Expanded = true;
            Assert.AreEqual( email1, _listView.Nodes [0].Nodes [0].Data );
            Assert.AreEqual( email3, _listView.Nodes [0].Nodes [1].Data );
            Assert.AreEqual( email2, _listView.Nodes [0].Nodes [2].Data  );
        }

        [Test] public void SortSkippedRoot()
        {
            DateTime now = DateTime.Now;
            IResource email0 = CreateEmail( "Email0", now.AddSeconds( -10 ), null, false );
            IResource email1 = CreateEmail( "Email1", now.AddSeconds( -7 ), email0, true );
            IResource email2 = CreateEmail( "Email2", now.AddSeconds( -5 ), email0, true );
            IResource email3 = CreateEmail( "Email3", now.AddSeconds( -6 ), email0, true );

            ShowThreadedEmails( Core.ResourceStore.FindResourcesWithProp( "Email", "IsUnread" ) );

            Assert.AreEqual( email1, _listView.Nodes [0].Data );
            Assert.AreEqual( email3, _listView.Nodes [1].Data );
            Assert.AreEqual( email2, _listView.Nodes [2].Data );
        }

        [Test] public void SortPartialSkippedRoot()
        {
            DateTime now = DateTime.Now;
            IResource email0 = CreateEmail( "Email0", now.AddSeconds( -10 ), null, true );
            IResource email1 = CreateEmail( "Email1", now.AddSeconds( -7 ), null, false );
            IResource email2 = CreateEmail( "Email2", now.AddSeconds( -5 ), email1, true );
            IResource email3 = CreateEmail( "Email3", now.AddSeconds( -6 ), null, true );

            ShowThreadedEmails( Core.ResourceStore.FindResourcesWithProp( "Email", "IsUnread" ) );

            Assert.AreEqual( email0, _listView.Nodes [0].Data );
            Assert.AreEqual( email3, _listView.Nodes [1].Data );
            Assert.AreEqual( email2, _listView.Nodes [2].Data );
        }
    }
}
