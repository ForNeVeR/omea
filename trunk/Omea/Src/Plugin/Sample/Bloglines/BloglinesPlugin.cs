/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.BloglinesPlugin
{
	/// <summary>
	/// Summary description for BloglinesPlugin
	/// </summary>
	public class BloglinesPlugin : IPlugin
	{
		private const string _ActionGroup = "RSSImportActions";
		private const string _ActionAbove = "JetBrains.Omea.RSSPlugin.ImportFeedsAction";
		private const string _ActionName  = "Import Bloglines Subscriptions...";
		private const string _Name        = "Bloglines Subscription Import";

		private const string _ConfigSection     = "JetBrains.Omea.SamplePlugins.BloglinesPlugin";
		private const string _ConfigKeyLogin    = "login";
		private const string _ConfigKeyPassword = "password";

		private const string _ImportURL = "http://rpc.bloglines.com/listsubs";
		private const string _ImportName = "Bloglines subscription";

		private static IRssService _RSSService = null;

		public BloglinesPlugin()
		{
		}

		internal static IRssService RSSService { get { return _RSSService; } }

		internal static string Name  { get { return _Name; } }

		internal static string ConfigSection     { get { return _ConfigSection; } }
		internal static string ConfigKeyLogin    { get { return _ConfigKeyLogin; } }
		internal static string ConfigKeyPassword { get { return _ConfigKeyPassword; } }

		internal static string ImportURL  { get { return _ImportURL; } }
		internal static string ImportName { get { return _ImportName; } }

		public void Register()
		{
		}

		public void Startup()
		{
			_RSSService = (IRssService)Core.PluginLoader.GetPluginService( typeof(IRssService) );
			if ( null == _RSSService )
			{
				// Sorry, no RSS plugin
				return;	
			}
			// Register our action
			Core.ActionManager.RegisterMainMenuAction( 
				new BloglinesImportAction(), _ActionGroup,
				new ListAnchor(AnchorType.After, _ActionAbove),
				_ActionName,
				null, null);
		}

		public void Shutdown()
		{
			// Do nothing
		}
	}
}
