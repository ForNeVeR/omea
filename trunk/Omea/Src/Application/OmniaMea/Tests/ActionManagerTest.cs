/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Windows.Forms;
using CommonTests;
using JetBrains.Omea;
using JetBrains.Omea.OpenAPI;
using NUnit.Framework;

namespace OmniaMea.Tests
{
    [TestFixture]
    public class ActionManagerTest
	{
        private MockPluginEnvironment _environment;
        private MenuStrip _mainMenu;
        private ContextMenuStrip _contextMenu;
        private ActionManager _actionManager;

        [SetUp] public void SetUp()
        {
#pragma warning disable 1717
            _environment = _environment;
#pragma warning restore 1717
            _environment = new MockPluginEnvironment( null );
            _mainMenu = new MenuStrip();
            _contextMenu = new ContextMenuStrip();
            _actionManager = new ActionManager( _mainMenu, _contextMenu, null );
        }

        [TearDown] public void TearDown()
        {
            _mainMenu.Dispose();
            _contextMenu.Dispose();
        }

        [Test] public void TestShortcutForAction()
        {
            MockAction action = new MockAction();
            _actionManager.RegisterKeyboardAction( action, Keys.F9, null, null );
            Assert.AreEqual( "F9", _actionManager.GetKeyboardShortcut( action ) );
        }
	}

    internal class MockAction: IAction
    {
        public void Execute( IActionContext context )
        {
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
        }
    }

    internal class DisableActionFilter: IActionStateFilter
    {
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Enabled = false;
        }
    }
}
