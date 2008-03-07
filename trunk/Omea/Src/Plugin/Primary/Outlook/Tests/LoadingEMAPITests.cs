/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using CommonTests;
using EMAPILib;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OutlookPlugin;
using NUnit.Framework;

namespace OutlookPlugin.Tests
{
    [TestFixture, Ignore( "Investigating problems on OMNIAMEA-UNIT")]
    public class LoadingEMAPITests : MyPalDBTests
	{
        private MockPluginEnvironment _core;
        [SetUp] public void SetUp()
        {
            InitStorage();
            _core = new MockPluginEnvironment( _storage );
            _core = _core;
            //OutlookSession.Initialize( null );
        }

        [TearDown] public void TearDown()
        {
            //OutlookSession.Uninitialize();
            OutlookKiller.KillFatAsses();
            CloseStorage();     
        }
        [Test]
        public void LoadTest()
        {
            //MessageBox.Show( "LoadTest" );
            for ( int i = 0; i < 10; ++i )
            {
                Tracer._Trace( "Test: " + i );
                OutlookSession.Initialize( );
                
                IEMsgStore msgStore = OutlookSession.GetDefaultMsgStore();
                Assert.IsNotNull( msgStore );
                FolderEnum folderEnum = FolderEnum.SearchForFolders( new string[]{ "TasksTest" } );
                folderEnum.GetFolderDescriptor( "TasksTest" );
                OutlookSession.Uninitialize();
            }
        }
    }
}
