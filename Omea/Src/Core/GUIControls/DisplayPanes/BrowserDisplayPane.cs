// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using JetBrains.Omea.OpenAPI;
using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Implementation of IDisplayPane based on the default browser.
	/// </summary>
	public class BrowserDisplayPane: IDisplayPane, IDisplayPane2, IContextProvider
	{
		/// <summary>
		/// A delegate to the function that should be invoked for filling the browser with the actual content
		/// when the display pane is requested to show some resource.
		/// </summary>
        private DisplayResourceInBrowserDelegate _displayDelegate;

		/// <summary>
		/// Stores a resource that is being displayed in the pane.
		/// This is needed for providing a correct context to the browser.
		/// </summary>
    	protected  IResource _resource;

    	public event EventHandler DisplayResourceEnded;

        public BrowserDisplayPane( DisplayResourceInBrowserDelegate displayDelegate )
		{
            _displayDelegate = displayDelegate;
		}

        Control IDisplayPane.GetControl()
        {
            return Core.WebBrowser;
        }

        void IDisplayPane.DisplayResource( IResource res )
        {
			DisplayResource( res, null );
        }

        void IDisplayPane.HighlightWords( WordPtr[] words )
        {
        	Trace.WriteLine( "Warning: calling obsolete IDisplayPane.HighlightWords!" );
            Core.WebBrowser.HighlightWords( words, 0 );
        }

        void IDisplayPane.EndDisplayResource( IResource res )
        {
            if ( DisplayResourceEnded != null )
            {
                DisplayResourceEnded( this, EventArgs.Empty );
            }
        }

        void IDisplayPane.DisposePane()
        {
        }

        public string GetSelectedText( ref TextFormat format )
        {
            format = TextFormat.Html;
            return Core.WebBrowser.SelectedHtml;
        }

        public string GetSelectedPlainText()
        {
            return Core.WebBrowser.SelectedText;
        }

        bool ICommandProcessor.CanExecuteCommand( string action )
        {
            return Core.WebBrowser.CanExecuteCommand( action );
        }

        void ICommandProcessor.ExecuteCommand( string action )
        {
            Core.WebBrowser.ExecuteCommand( action );
        }

    	public void DisplayResource( IResource resource, WordPtr[] wordsToHighlight )
    	{
			_resource = resource;
			Core.WebBrowser.ContextProvider = this;	// The selected resource will be given to the browser, whenever needed
			_displayDelegate( resource, Core.WebBrowser, wordsToHighlight );
		}
		#region IContextProvider Members

		public IActionContext GetContext(JetBrains.Omea.OpenAPI.ActionContextKind kind)
		{
			return new ActionContext(kind, this, (_resource != null ? _resource.ToResourceList() : null));
		}

		#endregion
	}

	/// <summary>
	/// A delegate to the function that should be invoked for filling the browser with the actual content
	/// when the display pane is requested to show some resource.
	/// </summary>
	public delegate void DisplayResourceInBrowserDelegate( IResource resource, AbstractWebBrowser browser, WordPtr[] wordsToHighlight );
}
