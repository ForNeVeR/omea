// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace GUIControlsTests
{
	[TestFixture]
    public class DisplayColumnManagerTests
	{
        private DisplayColumnManager _displayColumnManager;
        private TestCore _core;
        private IResourceStore _storage;
        private int _propName;
        private IResource _email;
        private IResourceList _emails;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = _core.ResourceStore;

            _storage.ResourceTypes.Register( "Email", "Name" );
            _propName = _storage.GetPropId( "Name" );
            _storage.PropTypes.Register( "IsUnread", PropDataType.Bool );
            _storage.PropTypes.Register( "Received", PropDataType.Date );

            _email = _storage.NewResource( "Email" );
            _email.SetProp( _propName, "Test Email" );
            _emails = _storage.GetAllResourcesLive( "Email" );

            _displayColumnManager = new DisplayColumnManager();
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
        }

        [Test] public void TestRegisterDisplayColumn()
        {
            _displayColumnManager.RegisterDisplayColumn( "Email", 5,
                new ColumnDescriptor( "Name", 100 )  );
            _displayColumnManager.RegisterDisplayColumn( "Email", 1,
                new ColumnDescriptor( "Received", 20 ) );

            ColumnDescriptor[] descriptors = _displayColumnManager.GetColumnsForTypes( new string[] { "Email" } );
            Assert.AreEqual( 2, descriptors.Length );
            Assert.AreEqual( "Received", descriptors [0].PropNames [0] );
            Assert.AreEqual( "Name", descriptors [1].PropNames [0] );
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void RegisterDisplayColumnWithInvalidProperty()
        {
            _displayColumnManager.RegisterDisplayColumn( "Email", 5,
                new ColumnDescriptor( "Someshit", 100 )  );
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void RegisterDisplayColumnWithInvalidResourceType()
        {
            _displayColumnManager.RegisterDisplayColumn( "Someshit", 5,
                new ColumnDescriptor( "Name", 100 ) );
        }

        [Test] public void TestRegisterAvailableColumn()
        {
            _displayColumnManager.RegisterAvailableColumn( "Email",
                new ColumnDescriptor( "Name", 100 ) );
            _displayColumnManager.RegisterAvailableColumn( null,
                new ColumnDescriptor( "Received", 50 ) );

            _storage.NewResource( "Email" );
            IResourceList emails = _storage.GetAllResources( "Email" );

            IntArrayList propIds = _displayColumnManager.GetAvailableColumns( emails );
            Assert.AreEqual( 2, propIds.Count );
            Assert.IsTrue( propIds.IndexOf( _storage.GetPropId( "Name" ) ) >= 0 );
            Assert.IsTrue( propIds.IndexOf( _storage.GetPropId( "Received" ) ) >= 0 );
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void RegisterAvailableColumnWithInvalidProperty()
        {
            _displayColumnManager.RegisterAvailableColumn( "Email",
                new ColumnDescriptor( "Someshit", 100 ) );
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void RegisterAvailableColumnWithInvalidResourceType()
        {
            _displayColumnManager.RegisterAvailableColumn( "Someshit",
                new ColumnDescriptor( "Name", 100 ) );
        }

        [Test] public void TestRemoveAvailableColumn()
        {
            _displayColumnManager.RegisterAvailableColumn( "Email",
                new ColumnDescriptor( "Name", 100 ) );
            _displayColumnManager.RegisterAvailableColumn( "Email",
                new ColumnDescriptor( "Received", 50 ) );

            _displayColumnManager.RemoveAvailableColumn( "Email", "Name" );

            IResourceList emails = _storage.GetAllResources( "Email" );

            IntArrayList propIds = _displayColumnManager.GetAvailableColumns( emails );
            Assert.AreEqual( 1, propIds.Count );
            Assert.AreEqual( _storage.GetPropId( "Received" ), propIds [0] );
        }

        [Test] public void MergeListAndStateColumnFlags()
        {
            ColumnDescriptor listColumn = new ColumnDescriptor( "Date", 50 );
            ColumnDescriptor stateColumn = new ColumnDescriptor( "Date", 50, ColumnDescriptorFlags.ShowIfNotEmpty );
            ResourceListState state = new ResourceListState( new ColumnDescriptor[] { stateColumn }, null, true );
            ColumnDescriptor[] result = _displayColumnManager.UpdateColumnsFromState( new ColumnDescriptor[] { listColumn },
                state );
            Assert.AreEqual( 1, result.Length );
            Assert.AreEqual( ColumnDescriptorFlags.ShowIfNotEmpty, result [0].Flags );
        }

        [Test] public void MergeListAndStateColumnHidden()
        {
            ColumnDescriptor listSubjectColumn = new ColumnDescriptor( "Subject", 100 );
            ColumnDescriptor stateSubjectColumn = new ColumnDescriptor( "Subject", 100 );
            ColumnDescriptor stateDateColumn = new ColumnDescriptor( "Date", 50, ColumnDescriptorFlags.ShowIfNotEmpty );

            ResourceListState state = new ResourceListState(
                new ColumnDescriptor[] { stateSubjectColumn, stateDateColumn }, null, true );

            ColumnDescriptor[] result = _displayColumnManager.UpdateColumnsFromState( new ColumnDescriptor[] { listSubjectColumn },
                state );
            Assert.AreEqual( 2, result.Length );
            Assert.AreEqual( "Subject", result [0].PropNames [0] );
            Assert.AreEqual( "Date", result [1].PropNames [0] );
        }

        [Test] public void MergeListAndStateHiddenInsert()
        {
            ColumnDescriptor listSubjectColumn = new ColumnDescriptor( "Subject", 100 );
            ColumnDescriptor stateSubjectColumn = new ColumnDescriptor( "Subject", 100 );
            ColumnDescriptor stateDateColumn = new ColumnDescriptor( "Date", 50, ColumnDescriptorFlags.ShowIfNotEmpty );
            ColumnDescriptor listReceivedColumn = new ColumnDescriptor( "Received", 100 );
            ColumnDescriptor stateReceivedColumn = new ColumnDescriptor( "Received", 100 );

            ResourceListState state = new ResourceListState(
                new ColumnDescriptor[] { stateSubjectColumn, stateDateColumn, stateReceivedColumn }, null, true );

            ColumnDescriptor[] result = _displayColumnManager.UpdateColumnsFromState(
                new ColumnDescriptor[] { listSubjectColumn, listReceivedColumn }, state );
            Assert.AreEqual( 3, result.Length );
            Assert.AreEqual( "Subject", result [0].PropNames [0] );
            Assert.AreEqual( "Date", result [1].PropNames [0] );
            Assert.AreEqual( "Received", result [2].PropNames [0] );
        }

        /*
        [Test] public void TestRegisterCustomColumn()
        {
            MockCustomColumn customColumn = new MockCustomColumn();
            _displayColumnManager.RegisterCustomColumn( _storage.GetPropId( "Name" ), customColumn );
            _resourceListView.Columns.Add( "Name", 100, HorizontalAlignment.Left );
            _displayColumnManager.AssignCustomColumns( _resourceListView );

            Assert.AreEqual( customColumn, _resourceListView.Columns [0].GetCustomColumn( _email ) );
        }

        [Test] public void TestRegisterPropertyToTextCallback()
        {
            _displayColumnManager.RegisterPropertyToTextCallback( _propName,
                new PropertyToTextCallback( MockPropertyToText ) );
            _resourceListView.Columns.Add( "Name", 100, HorizontalAlignment.Left );
            _displayColumnManager.AssignCustomColumns( _resourceListView );

            Assert.AreEqual( "Mock Name", _resourceListView.Columns [0].GetPropValue( _email ) );
        }
        */

        [Test] public void TestGetDefaultColumns()
        {
            _displayColumnManager.RegisterDisplayColumn( "Email", 5,
                new ColumnDescriptor( "Name", 100 )  );
            _displayColumnManager.RegisterDisplayColumn( null, 1,
                new ColumnDescriptor( "Received", 20 ) );

            ColumnDescriptor[] descriptors = _displayColumnManager.GetDefaultColumns( _emails );
            Assert.AreEqual( 2, descriptors.Length );
            Assert.AreEqual( "Received", descriptors [0].PropNames [0] );
            Assert.AreEqual( "Name", descriptors [1].PropNames [0] );
        }

        [Test] public void TestAddAnyTypeColumns()
        {
            _displayColumnManager.RegisterDisplayColumn( "Email", 5,
                new ColumnDescriptor( "Name", 100 )  );
            _displayColumnManager.RegisterDisplayColumn( null, 1,
                new ColumnDescriptor( "Received", 20 ) );

            ColumnDescriptor[] descriptors = new ColumnDescriptor[] { new ColumnDescriptor( "Subject", 100 ) };
            descriptors = _displayColumnManager.AddAnyTypeColumns( descriptors );
            Assert.AreEqual( 2, descriptors.Length );
            Assert.AreEqual( "Subject", descriptors [0].PropNames [0] );
            Assert.AreEqual( "Received", descriptors [1].PropNames [0] );
        }

	    private string MockPropertyToText( IResource res, int propId )
	    {
	        return "Mock Name";
	    }

	    private class MockCustomColumn: ICustomColumn
	    {
	        public void Draw( IResource res, Graphics g, Rectangle rc )
	        {
	        }

	        public void DrawHeader( Graphics g, Rectangle rc )
	        {
	        }

	        public void MouseClicked( IResource res, Point pt )
	        {
	        }

	        public bool ShowContextMenu( IActionContext context, Control ownerControl, Point pt )
	        {
                return false;
	        }

	        public string GetTooltip( IResource res )
	        {
                return "";
	        }
	    }
	}
}
