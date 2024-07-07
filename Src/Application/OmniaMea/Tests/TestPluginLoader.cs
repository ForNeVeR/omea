// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.Plugins;

using NUnit.Framework;

namespace OmniaMea.Tests
{
	[TestFixture]
	public class TestPluginLoader
	{
		[Test]
		public void TestAssemblyNameToPluginDisplayName()
		{
            Assert.AreEqual("Nntp", PluginLoader.AssemblyNameToPluginDisplayName("Nntp.OmeaPlugin"));
			Assert.AreEqual("Delivers", PluginLoader.AssemblyNameToPluginDisplayName("OmeaPlugin.Delivers"));
			Assert.AreEqual("Wonderful", PluginLoader.AssemblyNameToPluginDisplayName("Wonderful.OmeaPlugin"));
			Assert.AreEqual("And More", PluginLoader.AssemblyNameToPluginDisplayName("OmeaPlugin.And.More"));
            Assert.AreEqual("And More", PluginLoader.AssemblyNameToPluginDisplayName("The.OmeaPlugin.And.More"));
            Assert.AreEqual("Longer", PluginLoader.AssemblyNameToPluginDisplayName("Shrtr.OmeaPlugin.Longer"));
            Assert.AreEqual("Longer", PluginLoader.AssemblyNameToPluginDisplayName("Longer.OmeaPlugin.Shrtr"));
            Assert.AreEqual("No.OmniaMeaPlugin.Text", PluginLoader.AssemblyNameToPluginDisplayName("No.OmniaMeaPlugin.Text"));
            Assert.AreEqual("OmeaPlugin", PluginLoader.AssemblyNameToPluginDisplayName("OmeaPlugin"));
		}
	}
}
