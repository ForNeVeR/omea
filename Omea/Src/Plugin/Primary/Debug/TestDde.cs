/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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