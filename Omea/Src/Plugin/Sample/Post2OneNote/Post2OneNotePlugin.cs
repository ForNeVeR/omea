// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Web;
using System.Windows.Forms;
using Microsoft.Office.OneNote;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.Post2OneNotePlugin
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class Post2OneNotePlugin : IPlugin
	{
		private const string _mainMenu      = "Tools";
		private const string _actionGroup   = "JetBrains.Omea.SamplePlugins.Post2OneNotePlugin";
		private const string _actionCaption = "Post to OneNote";

		public Post2OneNotePlugin()
		{
		}

		#region IPlugin members
		public void Register()
		{
			Core.ActionManager.RegisterContextMenuActionGroup( _actionGroup, ListAnchor.Last );
			Core.ActionManager.RegisterContextMenuAction(new PostAction(true), _actionGroup, ListAnchor.Last, _actionCaption, null, null);
			Core.ActionManager.RegisterMainMenuActionGroup( _actionGroup, _mainMenu, ListAnchor.Last );
			Core.ActionManager.RegisterMainMenuAction(new PostAction(false), _actionGroup, ListAnchor.Last, _actionCaption, null, null);
		}

		public void Startup()
		{
		}

		public void Shutdown()
		{
		}
		#endregion


		#region PostAction class
		private class PostAction : IAction
		{
			private delegate void ErrorReportJob( string message );

			private bool _isContext;
			public PostAction(bool isContext)
			{
				_isContext = isContext;
			}

			#region IAction members
			public void Execute( IActionContext context )
			{
				if ( SelectionIsUseful(context) )
				{
					PostItem(context.SelectedText, context.SelectedTextFormat == TextFormat.Html);
				}
				else
				{
					IResource r = context.SelectedResources[0];
					PostItem(r.GetStringProp( "LongBody" ), r.HasProp( "LongBodyIsHTML" ), r.DisplayName );
				}
			}

			public void Update( IActionContext context, ref ActionPresentation presentation )
			{
				if(_isContext)
				{
					presentation.Visible = GetState(context);
				}
				else
				{
					presentation.Enabled = GetState(context);
				}
				return;
			}
			#endregion

			private bool GetState(IActionContext ctx)
			{
				if ( SelectionIsUseful(ctx) )
				{
					return true;
				}
				else if(ctx.SelectedResources.Count == 1)
				{
					return ResourceIsUseful(ctx.SelectedResources[0]);
				}
				return false;
			}

			private bool SelectionIsUseful(IActionContext ctx)
			{
				return null != ctx.SelectedText &&
					ctx.SelectedText.Length > 0 &&
					(
						ctx.SelectedTextFormat == TextFormat.Html ||
						ctx.SelectedTextFormat == TextFormat.PlainText
					);
			}

			private bool ResourceIsUseful(IResource res)
			{
				return res.HasProp("LongBody");
			}

			private void PostItem( string html, bool isHtml)
			{
				PostItem(html, isHtml, "Clipping");
			}

			private void PostItem( string html, bool isHtml, string name )
			{
				try
				{
					Page p = new Page("General.one", "Import from " + Core.ProductFullName + ": " + name);
					OutlineObject outline = new OutlineObject();

					if(!isHtml)
					{
						html = HttpUtility.HtmlEncode( html );
					}

					outline.AddContent(new HtmlContent(html));
					p.AddObject(outline);
					p.Commit();
					p.NavigateTo();
				}
				catch
				{
					Core.UIManager.QueueUIJob( new ErrorReportJob(ReportError), new object[] { "Can not create OneNote post. Is OneNote installed?" });
				}
			}

			private void ReportError(string message)
			{
				MessageBox.Show(message, "Post to OneNote", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		#endregion
	}
}
