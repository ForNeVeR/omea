/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using CommonTests;
using JetBrains.Omea;
using JetBrains.Omea.OpenAPI;
using NUnit.Framework;

namespace OmniaMea.Tests
{
	[TestFixture]
    public class CompositeActionTests: MyPalDBTests
	{
        private MockPluginEnvironment _environment;        
        private CompositeAction _composite;

        [SetUp] public void SetUp()
        {
            _environment = _environment;
            InitStorage();
            RegisterResourcesAndProperties();
            _environment = new MockPluginEnvironment( _storage );
            _composite = new CompositeAction( "Delete" );

            _storage.NewResource( "Person" );
            _storage.NewResource( "Email" );
        }

        [TearDown] public void TearDown()
        {
            CloseStorage();
        }
        
        [Test] public void MultiTypeTest()
        {
            MockAction contactAction = new MockAction( "Person" );
            _composite.AddComponent( "Person", contactAction, null ); 
            MockAction emailAction = new MockAction( "Email" );
            _composite.AddComponent( "Email", emailAction, null );

            IResourceList resList = _storage.GetAllResources( "Email" ).Union( 
                _storage.GetAllResources( "Person" ) );
            ActionContext context = new ActionContext( resList );
            ActionPresentation presentation = new ActionPresentation();

            _composite.Update( context, ref presentation );
            Assert.IsTrue( presentation.Visible );

            _composite.Execute( context );
            Assert.IsTrue( contactAction._executed );
            Assert.IsTrue( emailAction._executed );
        }

        private class MockAction: IAction
        {
            private string _resourceType;
            internal bool _executed = false;

            public MockAction( string resourceType )
            {
                _resourceType = resourceType;
            }

            public void Execute( IActionContext context )
            {
                _executed = true;
            }

            public void Update( IActionContext context, ref ActionPresentation presentation )
            {
                presentation.Visible = context.SelectedResources.AllResourcesOfType( _resourceType );
            }
        }
	}
}
