/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;
using SP.Windows;

#if DEBUG

namespace OmniaMea.Tests
{
    [TestFixture]
    public class ResourceBrowserTests
	{
        private TestCore _core;
        private ResourceBrowser _resourceBrowser;
        private JetListView _jetListView;
        private Header _header;
        private ColumnDescriptor _cdIcon;
        private ColumnDescriptor _cdName;
        private ColumnDescriptor _cdDate;
        private ColumnDescriptor _cdAnnotation;
        private int _propReply;
        private IResource _theEmail;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _core.SetDisplayColumnManager( new DisplayColumnManager() );

            _core.ResourceStore.PropTypes.Register( "IsUnread", PropDataType.Bool );
            _core.ResourceStore.PropTypes.Register( "Date", PropDataType.Date );
            _core.ResourceStore.PropTypes.Register( "Subject", PropDataType.String );
            _core.ResourceStore.PropTypes.Register( "Annotation", PropDataType.String );
            _propReply = _core.ResourceStore.PropTypes.Register( "Reply", PropDataType.Link, PropTypeFlags.DirectedLink );

            _core.ResourceStore.ResourceTypes.Register( "Email", "Subject" );

            _theEmail = _core.ResourceStore.NewResource( "Email" );

            _resourceBrowser = new ResourceBrowser();
            _jetListView = _resourceBrowser.ListView.JetListView;
            _header = _jetListView.Header;

            _cdIcon = new ColumnDescriptor( "Type", 20, ColumnDescriptorFlags.FixedSize );
            _cdName = new ColumnDescriptor( "DisplayName", 100 );
            _cdDate = new ColumnDescriptor( "Date", 50 );
            _cdAnnotation = new ColumnDescriptor( "Annotation", 20, ColumnDescriptorFlags.FixedSize );
        }

        [TearDown] public void TearDown()
        {
            _resourceBrowser.Dispose();
            _core.Dispose();
        }

        private void ShowPlainEmails()
        {
            _resourceBrowser.DisplayResourceList( null, _core.ResourceStore.GetAllResources( "Email" ),
                "", new ColumnDescriptor[] {});
        } 

        private void ShowThreadedEmails()
        {
            _resourceBrowser.DisplayThreadedResourceList( null, _core.ResourceStore.GetAllResources( "Email" ), 
                "", null, _propReply, new ColumnDescriptor[] {}, null );
        }

        private void VerifySection( int index, int width, string text )
        {
            Assert.AreEqual( width, _header.Sections [index].Width );
            Assert.AreEqual( text, _header.Sections [index].Text );
        }

        private void VerifyPropColumn( int index, string propName )
        {
            ResourcePropsColumn col = (ResourcePropsColumn) _jetListView.Columns [index];
            Assert.AreEqual( propName, _core.ResourceStore.PropTypes [col.PropIds [0]].Name );
        }

        [Test] public void ShowPlainColumns()
        {
            ShowPlainEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate } );
            Assert.AreEqual( 3, _jetListView.Header.Sections.Count );
            VerifySection( 0, 20, null );
            VerifySection( 1, 100, "Name" );
            VerifySection( 2, 50, "Date" );
        }

        [Test] public void ShowThreadedColumns()
        {
            ShowThreadedEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate } );
            Assert.AreEqual( 2, _jetListView.Header.Sections.Count );
            Assert.AreEqual( 120, _jetListView.Header.Sections [0].Width );
            Assert.AreEqual( 50, _jetListView.Header.Sections [1].Width );
        }

        [Test] public void DragForward()
        {
            ShowThreadedEmails();
            // {icon,name} | date -> {icon,date} | name
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate } );
            Assert.AreEqual( 2, _jetListView.Header.Sections.Count );
            _header.HandleEndDrag( _header.Sections [0], MouseButtons.Left, 1 );
            Assert.AreEqual( 2, _header.Sections.Count );
            Assert.AreEqual( 70,_header.Sections [0].Width );
            Assert.AreEqual( 100, _header.Sections [1].Width );
            Assert.AreEqual( "Date", _header.Sections [0].Text );
            Assert.AreEqual( "Name", _header.Sections [1].Text );
        }

        [Test] public void DragBackward()
        {
            ShowThreadedEmails();
            // {icon,name} | date -> {icon,date} | name
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate } );
            _header.HandleEndDrag( _header.Sections [1], MouseButtons.Left, 0 );
            Assert.AreEqual( 2, _header.Sections.Count );
            Assert.AreEqual( 70,_header.Sections [0].Width );
            Assert.AreEqual( 100, _header.Sections [1].Width );
            Assert.AreEqual( "Date", _header.Sections [0].Text );
            Assert.AreEqual( "Name", _header.Sections [1].Text );
        }

        [Test] public void DragFixedSizeBackward()
        {
            ShowThreadedEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate, _cdAnnotation } );
            Assert.AreEqual( 3, _header.Sections.Count );
            _header.HandleEndDrag( _header.Sections [2], MouseButtons.Left, 0 );
            VerifySection( 0, 20, "Annotation" );
            VerifySection( 1, 120, "Name" );
            VerifySection( 2, 50, "Date" );
        }

        [Test] public void MultiDragFixedSize()
        {
            ShowThreadedEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate, _cdAnnotation } );
            _header.HandleEndDrag( _header.Sections [2], MouseButtons.Left, 0 );
            _header.HandleEndDrag( _header.Sections [0], MouseButtons.Left, 2 );
            VerifySection( 0, 120, "Name" );
            VerifySection( 1, 50, "Date" );
            VerifySection( 2, 20, "Annotation" );

            Assert.IsTrue( _jetListView.Columns [0] is ConversationStructureColumn );
            Assert.IsTrue( _jetListView.Columns [1] is ResourceIconColumn );
            VerifyPropColumn( 2, "DisplayName" );
            VerifyPropColumn( 3, "Date" );
            VerifyPropColumn( 4, "Annotation" );
        }

        [Test] public void DragToRightOfMerged()
        {
            ShowThreadedEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate, _cdAnnotation } );
            _header.HandleEndDrag( _header.Sections [2], MouseButtons.Left, 1 );
            VerifySection( 0, 120, "Name" );
            VerifySection( 1, 20, "Annotation" );
            VerifySection( 2, 50, "Date" );

            Assert.IsTrue( _jetListView.Columns [0] is ConversationStructureColumn );
            Assert.IsTrue( _jetListView.Columns [1] is ResourceIconColumn );
            VerifyPropColumn( 2, "DisplayName" );
            VerifyPropColumn( 3, "Annotation" );
            VerifyPropColumn( 4, "Date" );
        }

        [Test] public void DragInPlainList()
        {
            ShowPlainEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate } );
            _header.HandleEndDrag( _header.Sections [2], MouseButtons.Left, 1 );
            Assert.AreEqual( 3, _jetListView.Header.Sections.Count );
            VerifySection( 0, 20, null );
            VerifySection( 1, 50, "Date" );
            VerifySection( 2, 100, "Name" );

            Assert.AreEqual( 3, _jetListView.Columns.Count );
            Assert.IsTrue( _jetListView.Columns [0] is ResourceIconColumn );
            VerifyPropColumn( 1, "Date" );
            VerifyPropColumn( 2, "DisplayName" );
        }

        [Test] public void DragBeforeIconColumn()
        {
            ShowPlainEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate } );
            _header.HandleEndDrag( _header.Sections [2], MouseButtons.Left, 0 );
            Assert.AreEqual( 3, _jetListView.Header.Sections.Count );
            VerifySection( 0, 20, null );
            VerifySection( 1, 50, "Date" );
            VerifySection( 2, 100, "Name" );

            Assert.AreEqual( 3, _jetListView.Columns.Count );
            Assert.IsTrue( _jetListView.Columns [0] is ResourceIconColumn );
            VerifyPropColumn( 1, "Date" );
            VerifyPropColumn( 2, "DisplayName" );
        }

        [Test] public void ShowIfNotEmpty_IsEmpty()
        {
            _cdDate.Flags |= ColumnDescriptorFlags.ShowIfNotEmpty;            
            ShowPlainEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate } );
            Assert.AreEqual( 2, _jetListView.Header.Sections.Count );
            VerifySection( 0, 20, null );
            VerifySection( 1, 100, "Name" );
        }

        [Test] public void ShowIfNotEmpty_IsNotEmpty()
        {
            _cdDate.Flags |= ColumnDescriptorFlags.ShowIfNotEmpty;
            _theEmail.SetProp( "Date", DateTime.Now );
            ShowPlainEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdDate } );
            Assert.AreEqual( 3, _jetListView.Header.Sections.Count );
            VerifySection( 0, 20, null );
            VerifySection( 1, 100, "Name" );
            VerifySection( 2, 50, "Date" );
        }

        [Test] public void ShowIfDistinct_NotDistinct()
        {
            _cdAnnotation.Flags |= ColumnDescriptorFlags.ShowIfDistinct;
            _theEmail.SetProp( "Annotation", "A" );
            IResource newEmail = _core.ResourceStore.NewResource( "Email" );
            newEmail.SetProp( "Annotation", "A" );
            ShowPlainEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdAnnotation } );
            Assert.AreEqual( 2, _jetListView.Header.Sections.Count );
            VerifySection( 0, 20, null );
            VerifySection( 1, 100, "Name" );
        }

        [Test] public void ShowIfDistinct_Distinct()
        {
            _cdAnnotation.Flags |= ColumnDescriptorFlags.ShowIfDistinct;
            _theEmail.SetProp( "Annotation", "A" );
            IResource newEmail = _core.ResourceStore.NewResource( "Email" );
            newEmail.SetProp( "Annotation", "B" );
            ShowPlainEmails();
            _resourceBrowser.ShowListViewColumns( new ColumnDescriptor[] { _cdIcon, _cdName, _cdAnnotation } );
            Assert.AreEqual( 3, _jetListView.Header.Sections.Count );
            VerifySection( 0, 20, null );
            VerifySection( 1, 100, "Name" );
            VerifySection( 2, 20, "Annotation" );
        }
    }
}
#endif