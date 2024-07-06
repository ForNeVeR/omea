// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using EMAPILib;
using JetBrains.Omea.OutlookPlugin;
using NUnit.Framework;

namespace OutlookPlugin.Tests
{
    [TestFixture]//, Ignore("Investigating test failure on OMNIAMEA-UNIT")]
    public class DefaultFoldersTests: OutlookTests
	{
        [Test] public void OpenDraftsFolderTest()
        {
            IEMsgStore msgStore = OutlookSession.GetDefaultMsgStore();
            Assert.IsNotNull( msgStore );
            IEFolder folder = msgStore.OpenDraftsFolder();
            Assert.IsNotNull( folder );
            folder.Dispose();
        }
        [Test] public void OpenTasksFolderTest()
        {
            IEMsgStore msgStore = OutlookSession.GetDefaultMsgStore();
            Assert.IsNotNull( msgStore );
            IEFolder folder = msgStore.OpenTasksFolder();
            Assert.IsNotNull( folder );
            folder.Dispose();
        }
    }
}
