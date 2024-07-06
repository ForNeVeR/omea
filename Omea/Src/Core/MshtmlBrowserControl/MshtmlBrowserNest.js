// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetBrains Omea Mshtml Browser Component
//
// Implements the Web browser component wrapping with full-scale customization, including view options and security settings & zones.
// Consists of an unmanaged part (C++ ATL, raw hosting, a composite ActiveX control) and a managed part (JScript.NET, Windows Forms control around the unmanaged ActiveX control plus AbstractWebBrowser proxy-inheritor).
// The unmanaged parts server as a wrapper for the custom interfaces only, and should not carry out any meaningful processing. All the events should be delegated to the managed part for processing.
//
// This file belongs to the managed part and implements the MshtmlBrowserNest class.
// This file inherits the AbstractWebBrowser class and transparently delegates all the calls to the WebBrowserControl class that implements the IWebBrowser interface. The WebBrowserControl cannot be inherited from AbstractWebBrowser directly because it already extends the AxHost class. This class must not contain any meaningful processing but should transparently relay the calls instead.
//
// © JetBrains Inc, 2004
// Written by (H) Serge Baltic
//
import System;
import System.Collections;
import System.ComponentModel;
import System.Drawing;
import System.Data;
import System.Windows.Forms;
import JetBrains.Omea.OpenAPI;
import System.Diagnostics;

package JetBrains.Omea.GUIControls.MshtmlBrowser
{
	/// <summary>
	/// Summary description for MshtmlBrowserNest.
	/// </summary>
	public class MshtmlBrowserNest extends AbstractWebBrowser
	{
		private var _browser : MshtmlBrowserControl;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private var components : System.ComponentModel.Container = null;

		public function MshtmlBrowserNest()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

		}

		/// Provides direct access to the browser control wrapped by this object.
		public function get BrowserControl() : MshtmlBrowserControl
		{
			return _browser;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected function Dispose( disposing : boolean ) : void
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			super.Dispose( disposing );
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private function InitializeComponent() : void
		{
			this._browser = new MshtmlBrowserControl();
			this.SuspendLayout();
			//
			// _browser
			//
			this._browser.Dock = System.Windows.Forms.DockStyle.Fill;
			this._browser.Location = new System.Drawing.Point(0, 0);
			this._browser.Name = "_browser";
			this._browser.Size = new System.Drawing.Size(376, 352);
			this._browser.TabIndex = 0;
			//
			// MshtmlBrowserNest
			//
			this.Controls.Add(this._browser);
			this.Name = "MshtmlBrowserNest";
			this.Size = new System.Drawing.Size(376, 352);
			this.ResumeLayout(false);

		}

		public function Navigate( url : String ) : void
		{
			_browser.Navigate(url);
		}

		public function NavigateInPlace( url : String ) : void
		{
			_browser.NavigateInPlace( url );
		}

		public function ShowHtml( html : String ) : void
		{
			_browser.ShowHtml( html );
		}

		public function ShowHtml( html : String, ctx : WebSecurityContext ) : void
		{
			_browser.ShowHtml( html, ctx );
		}

		public function ShowHtml( html : String, ctx : WebSecurityContext, words : WordPtr[] ) : void
		{
			_browser.ShowHtml( html, ctx, words );
		}

		public function HighlightWords( words : WordPtr[], startOffset : int ) : void
		{
			_browser.HighlightWords( words, startOffset );
		}

		public function CanExecuteCommand( action : String ) : boolean
		{
			return _browser.CanExecuteCommand( action );
		}

		public function ExecuteCommand( action : String ) : void
		{
			_browser.ExecuteCommand( action );
		}

		public function get ShowImages() : boolean
		{ return _browser.ShowImages; }
		public function set ShowImages( value : boolean )
		{ _browser.ShowImages = value; }

		public function get CurrentUrl() : String
		{ return _browser.CurrentUrl;	}
		public function set CurrentUrl( value : String )
		{ _browser.CurrentUrl = value; }

		public function get SelectedHtml() : String
		{ return _browser.SelectedHtml; }

		public function get SelectedText() : String
		{ return _browser.SelectedText; }

		public function get Title() : String
		{ return _browser.Title; }

		public function remove_TitleChanged( handler : EventHandler ) : void
		{	_browser.remove_TitleChanged( handler ); }
		public function add_TitleChanged( handler : EventHandler ) : void
		{	_browser.add_TitleChanged( handler );	}

		public function get SecurityContext() : WebSecurityContext
		{	return _browser.SecurityContext;	}
		public function set SecurityContext( value : WebSecurityContext )
		{	_browser.SecurityContext = value;	}

		public function get ContextProvider() : IContextProvider
		{	return _browser.ContextProvider;	}
		public function set ContextProvider( value : IContextProvider )
		{	_browser.ContextProvider = value;	}

		public function get Html() : System.String
		{	return _browser.Html;	}
		public function set Html( value : System.String )
		{	_browser.Html = value;	}

		public function NewInstance() : AbstractWebBrowser
		{
			return new MshtmlBrowserNest();
		}

		Browsable(true) ComVisible(true) Category("Appearance") Description("Border style") DefaultValue(System.Windows.Forms.BorderStyle.None)
		public function get BorderStyle() : System.Windows.Forms.BorderStyle
		{
			return _browser.BorderStyle;
		}
		public function set BorderStyle(value : System.Windows.Forms.BorderStyle)
		{
			_browser.BorderStyle = value;
		}

		public function Exec(code : System.String) : System.Object
		{
			return _browser.Exec(code);
		}

		public function get HtmlDocument() : IHtmlDomDocument
		{
			return _browser.ManagedHtmlDocument;
		}

		public function remove_KeyDown( handler : KeyEventHandler ) : void
		{	_browser.remove_KeyDown( handler ); }
		public function add_KeyDown( handler : KeyEventHandler ) : void
		{	_browser.add_KeyDown( handler );	}

		public function remove_ContextMenu( handler : ContextMenuEventHandler ) : void
		{	_browser.remove_ContextMenu( handler ); }
		public function add_ContextMenu( handler : ContextMenuEventHandler ) : void
		{	_browser.add_ContextMenu( handler );	}

		public function remove_DocumentComplete( handler : JetBrains.Omea.OpenAPI.DocumentCompleteEventHandler ) : void
		{	_browser.remove_DocumentComplete( handler ); }
		public function add_DocumentComplete( handler : JetBrains.Omea.OpenAPI.DocumentCompleteEventHandler ) : void
		{	_browser.add_DocumentComplete( handler );	}

		public function get ReadyState() : BrowserReadyState
		{	return _browser.ReadyState;	}

		public function Stop() : void
		{	_browser.Stop();	}

		public function remove_BeforeNavigate( handler : JetBrains.Omea.OpenAPI.BeforeNavigateEventHandler ) : void
		{	_browser.remove_BeforeNavigate( handler ); }
		public function add_BeforeNavigate( handler : JetBrains.Omea.OpenAPI.BeforeNavigateEventHandler ) : void
		{	_browser.add_BeforeNavigate( handler );	}

		public function remove_BeforeShowHtml( handler : JetBrains.Omea.OpenAPI.BeforeShowHtmlEventHandler ) : void
		{	_browser.remove_BeforeShowHtml( handler ); }
		public function add_BeforeShowHtml( handler : JetBrains.Omea.OpenAPI.BeforeShowHtmlEventHandler ) : void
		{	_browser.add_BeforeShowHtml( handler );	}
	}
}
