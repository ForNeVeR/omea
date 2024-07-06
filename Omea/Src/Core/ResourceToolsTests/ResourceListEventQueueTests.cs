// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using JetBrains.Omea.ResourceTools;
using NUnit.Framework;

namespace ResourceToolsTests
{
	[TestFixture]
    public class ResourceListEventQueueTests
	{
        private TestCore _core;
        private IResourceStore _storage;
        private ResourceListEventQueue _queue;
        private IResourceList _resList;
        private int _propSubject;
        private int _propFirstName;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;
            _propSubject = _storage.PropTypes.Register( "Subject", PropDataType.String );
            _propFirstName = _storage.PropTypes.Register( "FirstName", PropDataType.String );
            _storage.ResourceTypes.Register( "Email", "Subject" );

            _queue = new ResourceListEventQueue();
            _resList = _storage.GetAllResourcesLive( "Email" );
            _queue.Attach( _resList );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        private void DiscardEvents()
        {
            _queue.BeginProcessEvents();
            while( _queue.GetNextEvent() != null )
                ;
            _queue.EndProcessEvents();
        }

        private void VerifyNextEvent( EventType evType, int id, int index, int listIndex )
        {
            _queue.BeginProcessEvents();
            try
            {
                ResourceListEvent ev = _queue.GetNextEvent();
                Assert.AreEqual( evType, ev.EventType );
                if ( evType == EventType.Remove )
                {
                    Assert.IsTrue( ev.ResourceID == -1 || ev.ResourceID == id,
                        "Resource ID must be correct (expected " + id + ", actual " + ev.ResourceID + ")" );
                }
                else
                {
                    Assert.AreEqual( id, ev.ResourceID, "Resource ID must be correct" );
                }
                Assert.AreEqual( index, ev.Index, "Index must be correct" );
                Assert.AreEqual( listIndex, ev.ListIndex, "ListIndex must be correct" );
            }
            finally
            {
                _queue.EndProcessEvents();
            }
        }

        protected IResource CreateEmail( string subject )
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propSubject, subject );
            return res;
        }

        [Test] public void FirstTest()
        {
            IResource email = _storage.NewResource( "Email" );
            VerifyNextEvent( EventType.Add, email.Id, 0, 0 );

            Assert.IsTrue( _queue.IsEmpty() );

            int emailID = email.Id;
            email.Delete();
            VerifyNextEvent( EventType.Remove, emailID, 0, 0 );
        }

        [Test] public void TestChange()
        {
            IResource email = _storage.NewResource( "Email" );
            VerifyNextEvent( EventType.Add, email.Id, 0, 0 );

            email.SetProp( "Subject", "Test" );
            _queue.BeginProcessEvents();
            ResourceListEvent ev = _queue.GetNextEvent();
            Assert.AreEqual( EventType.Change, ev.EventType );
            Assert.IsTrue( ev.ChangeSet.IsPropertyChanged( _propSubject) );
            _queue.EndProcessEvents();
        }

        [Test] public void TestIndexChange()
        {
            _resList.Sort( "Subject" );
            IResource email1 = _storage.BeginNewResource( "Email" );
            email1.SetProp( "Subject", "A" );
            email1.EndUpdate();

            IResource email2 = _storage.BeginNewResource( "Email" );
            email2.SetProp( "Subject", "B" );
            email2.EndUpdate();

            DiscardEvents();

            email1.SetProp( "Subject", "Z" );
            VerifyNextEvent( EventType.Remove, email1.Id, 0, 0 );
            VerifyNextEvent( EventType.Add, email1.Id, 1, 1 );
        }

        [Test] public void AddAfterAdd()
        {
            _resList.Sort( "Subject" );
            IResource email1 = _storage.BeginNewResource( "Email" );
            email1.SetProp( _propSubject, "B");
            email1.EndUpdate();

            IResource email2 = _storage.BeginNewResource( "Email" );
            email2.SetProp( _propSubject, "A" );
            email2.EndUpdate();

            VerifyNextEvent( EventType.Add, email1.Id, 0, 1 );
            VerifyNextEvent( EventType.Add, email2.Id, 0, 0 );
        }

        [Test] public void AddAndDelete()
        {
            IResource email = _storage.NewResource( "Email" );
            email.Delete();

            Assert.IsTrue( _queue.IsEmpty(), "Delete() must have eaten the Add()" );
        }

        [Test] public void ChangeAndDelete()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSubject, "Subject" );
            email.Delete();

            Assert.IsTrue( _queue.IsEmpty(), "Delete() must have eaten the Change()" );
        }

        [Test] public void AddAndDeleteOther()
        {
            IResource email1 = _storage.NewResource( "Email" );
            IResource email2 = _storage.NewResource( "Email" );
            email1.Delete();

            VerifyNextEvent( EventType.Add, email2.Id, 0, 0 );
        }

        [Test] public void MergeChanges()
        {
            IResource email = _storage.NewResource( "Email" );

            DiscardEvents();

            email.SetProp( _propSubject, "Test" );
            email.SetProp( _propSubject, "Test2" );

            _queue.BeginProcessEvents();
            ResourceListEvent ev = _queue.GetNextEvent();
            Assert.AreEqual( EventType.Change, ev.EventType );
            Assert.IsTrue( ev.ChangeSet.IsPropertyChanged( _propSubject ) );
            _queue.EndProcessEvents();

            Assert.IsTrue( _queue.IsEmpty(), "Changes must have been merged" );
        }

        [Test] public void MergeDifferentChanges()
        {
            IResource email = _storage.NewResource( "Email" );
            DiscardEvents();

            email.SetProp( _propSubject, "Test" );
            email.SetProp( _propFirstName, "Dmitry" );

            _queue.BeginProcessEvents();
            ResourceListEvent ev = _queue.GetNextEvent();
            Assert.AreEqual( EventType.Change, ev.EventType );
            Assert.IsTrue( ev.ChangeSet.IsPropertyChanged( _propSubject ) );
            Assert.IsTrue( ev.ChangeSet.IsPropertyChanged( _propFirstName ) );
            _queue.EndProcessEvents();

            Assert.IsTrue( _queue.IsEmpty(), "Changes must have been merged" );
        }

        [Test] public void MergeAddAndChange()
        {
            IResource email = _storage.NewResource( "Email" );
            email.SetProp( _propSubject, "Test" );

            VerifyNextEvent( EventType.Add, email.Id, 0, 0 );
            Assert.IsTrue( _queue.IsEmpty(), "Change must have been merged with Add" );
        }

        [Test] public void AddAndIndexChange()
        {
            _resList.Sort( "Subject" );
            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( _propSubject, "B" );
            DiscardEvents();

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( _propSubject, "A" );

            VerifyNextEvent( EventType.Add, email2.Id, 0, 0 );
            Assert.IsTrue( _queue.IsEmpty(), "IndexChanged must have been merged with Add" );
        }

        [Test] public void ChangeAndRemove()
        {
            _resList.Sort( "Subject" );
            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( _propSubject, "A" );
            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( _propSubject, "B" );
            DiscardEvents();

            email1.SetProp( _propFirstName, "Dmitry" );

            int email2ID = email2.Id;
            email2.Delete();

            VerifyNextEvent( EventType.Change, email1.Id, 0, 0 );
            VerifyNextEvent( EventType.Remove, email2ID, 1, 1 );

            Assert.IsTrue( _queue.IsEmpty() );
        }

        [Test] public void AddAddRemove()
        {
            _resList.Sort( "Subject" );
            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( _propSubject, "B" );
            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( _propSubject, "A" );

            email2.Delete();

            VerifyNextEvent( EventType.Add, email1.Id, 0, 0 );
        }

        [Test] public void Add1Remove0()
        {
            _resList.Sort( "Subject" );
            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( _propSubject, "A" );

            DiscardEvents();

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( _propSubject, "B" );

            int email1ID = email1.Id;
            email1.Delete();

            VerifyNextEvent( EventType.Add, email2.Id, 1, 0 );
            VerifyNextEvent( EventType.Remove, email1ID, 0, 0 );
        }

        [Test] public void MultipleIndexChange()
        {
            _resList.Sort( "Subject" );
            IResource email1 = _storage.NewResource( "Email" );
            email1.SetProp( "Subject", "A" );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( "Subject", "K" );

            IResource email3 = _storage.NewResource( "Email" );
            email3.SetProp( "Subject", "P" );

            DiscardEvents();

            email1.SetProp( "Subject", "M" );
            email1.SetProp( "Subject", "Z" );

            VerifyNextEvent( EventType.Remove, email1.Id, 0, 0 );
            VerifyNextEvent( EventType.Add, email1.Id, 2, 2 );
        }

        [Test] public void MultipleRemove()
        {
            _resList.Sort( "Subject" );

            IResource email1 = CreateEmail( "A" );

            IResource email2 = _storage.NewResource( "Email" );
            email2.SetProp( _propSubject, "B" );

            IResource email3 = _storage.NewResource( "Email" );
            email3.SetProp( _propSubject, "C" );

            int email1ID = email1.Id;
            int email2ID = email2.Id;
            int email3ID = email3.Id;

            DiscardEvents();
            email1.Delete();
            email2.Delete();
            email3.Delete();

            VerifyNextEvent( EventType.Remove, email1ID, 0, 0 );
            VerifyNextEvent( EventType.Remove, email2ID, 0, 0 );
            VerifyNextEvent( EventType.Remove, email3ID, 0, 0 );
        }

        [Test] public void AdjustAddIndex()
        {
            _resList.Sort( "Subject" );
            IResource a = CreateEmail( "A" );
            IResource b = CreateEmail( "B" );

            int aID = a.Id;
            int bID = b.Id;

            DiscardEvents();

            IResource c = CreateEmail( "C" );
            int cID = c.Id;

            a.Delete();
            b.Delete();

            IResource d = CreateEmail( "D" );

            c.Delete();

            VerifyNextEvent( EventType.Remove, aID, 0, 0 );
            VerifyNextEvent( EventType.Remove, bID, 0, 0 );
            VerifyNextEvent( EventType.Add, d.Id, 0, 0 );
        }
	}
}
