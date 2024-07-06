// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using CommonTests;
using JetBrains.Omea;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.TextIndex;
using NUnit.Framework;

namespace OmniaMea.Tests
{
    /// <summary>
    /// Test for MainFrame initialization.
    /// </summary>
    [TestFixture]
    public class StartupTests: MyPalDBTests
    {
        private ArrayList _exceptionList = new ArrayList();

        [SetUp] public void SetUp()
        {
            OMEnv.WorkDir = ".";
            MyPalStorage.DBPath = ".";
            RemoveDBFiles();
            RemoveTextIndexFiles();
        }

        private void ExceptionHandler( Exception e )
        {
            _exceptionList.Add( e );
        }

        [TearDown] public void TearDown()
        {
            RemoveDBFiles();
            RemoveTextIndexFiles();
        }

        [Test, Ignore( "does not work")] public void Startup()
        {
            MainFrame._skipPlugins = true;
            MainFrame._skipWizard = true;
            Console.WriteLine( "Creating MainFrame" );
            MainFrame mainFrame = new MainFrame();
            MainFrame._uiAsyncProcessor = new MainFrame.UIAsyncProcessor( mainFrame );
            mainFrame._theEnvironment.SetUserInterfaceAP( MainFrame._uiAsyncProcessor );
            MainFrame._uiAsyncProcessor.ProcessMessages = true;
            MainFrame._uiAsyncProcessor.ExceptionHandler = new AsyncExceptionHandler( ExceptionHandler );
            Core.UIManager.QueueUIJob( new TestShutdownDelegate( TestShutdown ), mainFrame );
            MainFrame._uiAsyncProcessor.EmployCurrentThread();
            Assert.AreEqual( 0, _exceptionList.Count );
        }

        /**
         * the following code should be executed as ui asyncprocessors job
         */
        private delegate void TestShutdownDelegate( MainFrame mainFrame );

        private void TestShutdown( MainFrame mainFrame )
        {
            try
            {
                Console.WriteLine( "Closing MainFrame" );
                mainFrame.TestShutdown();
                Console.WriteLine( "Closing database" );
                MyPalStorage.CloseDatabase();
            }
            finally
            {
                MainFrame._uiAsyncProcessor.QueueEndOfWork();
            }
            MainFrame._uiAsyncProcessor.Dispose();
        }
    }
}
