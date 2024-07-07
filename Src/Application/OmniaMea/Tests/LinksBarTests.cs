// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace GUIControlsTests
{
	[TestFixture]
    public class LinksBarTests
	{
        private TestCore _core;
        private IResourceStore _storage;
        private LinksBar _linksBar;
        private int _propAuthor;
        private int _propAltAuthor;

        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _storage = Core.ResourceStore;
            _linksBar = new LinksBar();

            _storage.ResourceTypes.Register( "Person", "Name" );
            _storage.ResourceTypes.Register( "Email", "Name" );
            _storage.ResourceTypes.Register( "AltPerson", "Name" );
            _propAuthor = _storage.PropTypes.Register( "Author", PropDataType.Link );
            _propAltAuthor = _storage.PropTypes.Register( "AltAuthor", PropDataType.Link );
            _storage.PropTypes.RegisterDisplayName( _propAltAuthor, "Author" );
        }

        [TearDown] public void TearDown()
        {
            _core.Dispose();
            _linksBar.Dispose();
        }

        [Test, Ignore("fails once in a while on build machine")] public void BasicTest()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            email.AddLink( _propAuthor, person );

            _linksBar.DisplayLinks( email, null );
            Assert.AreEqual( 3, _linksBar.Controls.Count );
            Assert.IsTrue( _linksBar.Controls [1] is Label );
            Assert.AreEqual( 4, _linksBar.Controls [1].Left );
            Assert.IsTrue( _linksBar.Controls [2] is ResourceLinkLabel );
        }

        [Test, Ignore("fails once in a while on build machine")]
        public void TypeFilterTest()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            email.AddLink( _propAuthor, person );

            _linksBar.DisplayLinks( email, new HidePersonFilter() );
            Assert.AreEqual( 1, _linksBar.Controls.Count );
        }

        [Test, Ignore("fails once in a while on build machine")]
        public void SameLinkNameFilterTest()
        {
            IResource email = _storage.NewResource( "Email" );
            IResource person = _storage.NewResource( "Person" );
            IResource person2 = _storage.NewResource( "AltPerson" );
            email.AddLink( _propAuthor, person );
            email.AddLink( _propAltAuthor, person2 );

            _linksBar.DisplayLinks( email, new HidePersonFilter() );
            Assert.AreEqual( 3, _linksBar.Controls.Count );

            Assert.IsTrue( _linksBar.Controls [1].Visible );
            Assert.AreEqual( 4, _linksBar.Controls [1].Left );
            Assert.AreEqual( "Author:", _linksBar.Controls [1].Text );

            ResourceLinkLabel person2Label = _linksBar.Controls [2] as ResourceLinkLabel;
            Assert.IsNotNull( person2Label );
            Assert.AreEqual( person2, person2Label.Resource );
        }

        private class HidePersonFilter: ILinksPaneFilter
        {
            public bool AcceptLinkType( IResource displayedResource, int propId, ref string displayName )
            {
                return true;
            }

            public bool AcceptLink( IResource displayedResource, int propId, IResource targetResource, ref string linkTooltip )
            {
                return targetResource.Type != "Person";
            }

            public bool AcceptAction(IResource displayedResource, IAction action)
            {
                return true;
            }
        }
	}
}
