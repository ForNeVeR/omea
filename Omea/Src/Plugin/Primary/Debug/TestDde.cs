// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.DebugPlugin
{
	/// <summary>
	/// Tests the JetBrains.Omea.Base.Dde class.
	/// </summary>
	public class TestDde
	{
		public TestDde()
		{
		}

		public void Run()
		{
			Dde dde = new Dde();

			DdeConversation conv = dde.CreateConversation(Dde.InternetExplorer.Service, Dde.InternetExplorer.TopicOpenUrl);

			conv.StartAsyncTransaction(String.Format("\"http://www.hypersw.net/file{0}.html\",,0", new Random((int)DateTime.Now.Ticks).Next()), null);
		}
	}
}
