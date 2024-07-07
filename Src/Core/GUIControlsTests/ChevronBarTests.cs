// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.CommandBar;
using NUnit.Framework;

namespace GUIControlsTests
{
	[TestFixture]
    public class ChevronBarTests
	{
        private ChevronBar _bar;
        private Control _clickedControl;

        [SetUp] public void SetUp()
        {
            _bar = new ChevronBar();
            _bar.ChevronMenuItemClick += new ChevronBar.ChevronMenuItemClickEventHandler( bar_ChevronMenuClick );
        }

        [TearDown] public void TearDown()
        {
            _bar.ChevronMenuItemClick -= new ChevronBar.ChevronMenuItemClickEventHandler(bar_ChevronMenuClick );
            _bar.Dispose();
        }

        [Test] public void SeparateHiddenControls()
        {
            _bar.Size = _bar.MinSize;
            _bar.SeparateHiddenControls = true;
            Label lbl1 = new Label();
            lbl1.Text = "lbl1";
            Label lbl2 = new Label();
            lbl2.Text = "lbl2";
			using(new LayoutSuspender(_bar))
			{
				_bar.AddControl( lbl1 );
				_bar.AddHiddenControl( lbl2 );
			}
			_bar.Size = _bar.MinSize;	// Let the chevron bar have its valid size

            ContextMenu menu = new ContextMenu();
            _bar.FillChevronContextMenu( menu );
            Assert.AreEqual( 3, menu.MenuItems.Count );
            Assert.AreEqual( "-", menu.MenuItems [1].Text );

            menu.MenuItems [2].PerformClick();
            Assert.AreSame( lbl2, _clickedControl );  // see bug #6157
        }

        [Test] public void SeparateHiddenControls_NoHidden()
        {
            _bar.Size = new Size( 10, 10 );
            _bar.SeparateHiddenControls = true;
            Label lbl1 = new Label();
            lbl1.Text = "lbl1";

            _bar.AddHiddenControl( lbl1 );

            ContextMenu menu = new ContextMenu();
            _bar.FillChevronContextMenu( menu );
            Assert.AreEqual( 1, menu.MenuItems.Count );

            menu.MenuItems [0].PerformClick();
            Assert.AreSame( lbl1, _clickedControl );  // see bug #6258
        }

        private void bar_ChevronMenuClick( object sender, ChevronBar.ChevronMenuItemClickEventArgs args )
        {
            _clickedControl = args.ClickedControl;
        }
    }
}
