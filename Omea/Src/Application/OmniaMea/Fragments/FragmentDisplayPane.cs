/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using System.Windows.Forms;

namespace JetBrains.Omea
{
	/// <summary>
	/// Display pane for Fragment resources. Switches dynamically between IEBrowserDisplayPane and
	/// RTFDisplayPane depending on the format of the selected fragment.
	/// </summary>
    internal class FragmentDisplayPane: IDisplayPane, IDisplayPane2, IContextProvider
    {
        private RTFDisplayPane _rtfDisplayPane;
        private Panel _panel;
        private IResource _displayedResource;
        
        public FragmentDisplayPane()
        {
            _panel = new Panel();
			_panel.Controls.Add( Core.WebBrowser );
			Core.WebBrowser.Dock = DockStyle.Fill;

            _rtfDisplayPane = new RTFDisplayPane( new LoadTextDelegate( ShowRichTextFragment ) );
            _panel.Controls.Add( _rtfDisplayPane.GetControl() );
            _rtfDisplayPane.GetControl().Dock = DockStyle.Fill;
        }

        private void ShowRichTextFragment( IResource res, RichTextBox richTextBox )
        {
             richTextBox.Rtf = res.GetPropText( Core.Props.LongBody );
        }

        public void DisplayResource( IResource resource )
        {
			DisplayResource( resource, null );
        }

        public bool CanExecuteCommand( string command )
        {
            if ( Core.WebBrowser.Visible )
            {
                return Core.WebBrowser.CanExecuteCommand( command );
            }
            return _rtfDisplayPane.CanExecuteCommand( command );
        }

        public void ExecuteCommand( string command )
        {
            if ( Core.WebBrowser.Visible )
            {
            	Core.WebBrowser.ExecuteCommand( command );
            }
            else
            {
                _rtfDisplayPane.ExecuteCommand( command );
            }
        }

        public Control GetControl()
        {
            return _panel;
        }

        public void HighlightWords( WordPtr[] words )
        {
            if ( Core.WebBrowser.Visible )
            {
            	Core.WebBrowser.HighlightWords( words, 0 );
            }
            else
            {
                _rtfDisplayPane.HighlightWords( words );
            }
        }

        public void EndDisplayResource( IResource resource )
        {
        }

        public void DisposePane()
        {
            if ( _panel.Controls.Contains( Core.WebBrowser ) )
            {
                _panel.Controls.Remove( Core.WebBrowser );
            }
            _panel.Dispose();
            Core.WebBrowser.Visible = true;
        }

        public string GetSelectedText( ref TextFormat format )
        {
            if ( Core.WebBrowser.Visible )
            {
                format = TextFormat.Html;
                return Core.WebBrowser.SelectedHtml;
            }
            return _rtfDisplayPane.GetSelectedText( ref format );
        }

	    public string GetSelectedPlainText()
	    {
            if ( Core.WebBrowser.Visible )
            {
                return Core.WebBrowser.SelectedText;
            }
            return _rtfDisplayPane.GetSelectedPlainText();
		}

		#region IDisplayPane2 Members

		public void DisplayResource( IResource resource, WordPtr[] wordsToHighlight )
		{
			_displayedResource = resource;
			if ( resource.HasProp( Core.Props.LongBodyIsRTF ) )
			{
				_rtfDisplayPane.GetControl().Visible = true;
				Core.WebBrowser.Visible = false;
				_rtfDisplayPane.DisplayResource( resource );

				if(wordsToHighlight != null)
					HighlightWords( wordsToHighlight );
			}
			else
			{
				// Ensure the Web browser is hosted on our pane
				_panel.Controls.Add( Core.WebBrowser );
				Core.WebBrowser.Dock = DockStyle.Fill;
				Core.WebBrowser.Visible = true;
				Core.WebBrowser.ContextProvider = this;
				_rtfDisplayPane.GetControl().Visible = false;
                WebSecurityContext context = WebSecurityContext.Restricted;
                context.WorkOffline = false;
                Core.WebBrowser.ShowHtml( resource.GetPropText( Core.Props.LongBody ), context, 
                    DocumentSection.RestrictResults( wordsToHighlight, DocumentSection.BodySection ) );
			}
		}

		#endregion

        public IActionContext GetContext( ActionContextKind kind )
        {
            return new ActionContext( kind, null, 
                (_displayedResource == null) ? null : _displayedResource.ToResourceList() );
        }
	}

    public class FragmentDisplayer: IResourceDisplayer
    {
        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            return new FragmentDisplayPane();
        }
    }

}
