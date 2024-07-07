// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using JetBrains.Omea.Base;
using JetBrains.UI.Avalon;

using OmniaMea;

namespace JetBrains.Omea.Plugins
{
	/// <summary>
	/// Omea Plugin Page, list view entries that represent non-plugin assemblies.
	/// </summary>
	internal class OmeaPluginsPageListEntryNonPlugin : OmeaPluginsPageListEntry
	{
		#region Data

		private static ImageSource _iconNonPlugin;

		private readonly PluginLoader.PossiblyPluginFileInfo _file;

		#endregion

		#region Init

		public OmeaPluginsPageListEntryNonPlugin(PluginLoader.PossiblyPluginFileInfo info)
		{
			_file = info;
		}

		#endregion

		#region Overrides

		public override FlowDocument Description
		{
			get
			{
				var document = new FlowDocument();
				document.SetSystemFont();

				document.AddPara().Append(Stringtable.NotAPlugin, FontStyles.Normal, FontWeights.Bold);
				document.AddPara().Append(_file.ToString());

				return document;
			}
		}

		public override ImageSource Icon
		{
			get
			{
				return _iconNonPlugin ?? (_iconNonPlugin = Utils.LoadResourceImage("Icons/NonPlugin.png"));
			}
		}

		public override bool? IsPrimary
		{
			get
			{
				return null;
			}
		}

		public override bool SupportsIsEnabled
		{
			get
			{
				return false;
			}
		}

		public override string Title
		{
			get
			{
				return _file.File.Name;
			}
		}

		#endregion
	}
}
