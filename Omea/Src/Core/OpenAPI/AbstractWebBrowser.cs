// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Reflection;
using System.Windows.Forms;

namespace JetBrains.Omea.OpenAPI
{

	#region Abstract Web Browser Pseudo-Interface

	/// <summary>
	/// The base interface for a Web browser embedded in Omea.
	/// </summary>
	public abstract class AbstractWebBrowser : UserControl, ICommandProcessor
	{
		/// <summary>
		/// Navigates a web browser to show a page identified by the URI specified.
		/// </summary>
		/// <param name="url">URI of the resource to display. That may be a web page, a file, a MSDN collection topic, or some custom protocol scheme resource.</param>
		public abstract void Navigate( string url );

		/// <summary>
		/// Navigates a web browser to show a page identified by the URI specified, within the same browser window.
		/// </summary>
		/// <param name="url">URI of the resource to display. That may be a web page, a file, a MSDN collection topic, or some custom protocol scheme resource.</param>
		public abstract void NavigateInPlace( string url );

		/// <summary>
		/// Renders the specified HTML text in the browser window as if it were downloaded from a web site or a file.
		/// </summary>
		/// <param name="html">HTML text. This must be a complete and well-formed web page, better XHTML.</param>
		/// <remarks>
		/// <para>This function provides for displaying your page in the embedded web browser by directly uploading its contents into the browser.</para>
		/// <para>The browser maintains its history which allows to go Back and Forward within the browser using the Omea Back and Forward actions. The history typically consists of two stacks: pages visited in this session before the current page — Back stack, and pages visited "after" it (this means the pages we've visited but then undone using the "back" action) — Forward stack. If we navigate to a new page, the current one is pushed to the Back stack and the Forward stack is emptied. If we invoke the "back" action, the current page gets pushed into the Forward stack and a page popped from the Back stack gets into the browser. If we invoke the "forward" action, the current page goes into the "Back" stack and a page popped out of the Forward stack gets into the browser.</para>
		/// <para>Each time you inovke the ShowHtml action, the history is reset, which means that both Back and Forward stacks get empty and a new History session starts. If you press either Back or Forward button in this state, the action would not be processed by the browser and will be delegated to the Omea framework.</para>
		/// <para>Do not call this function before the control window is created. Use <see cref="Html"/> property instead.</para>
		/// </remarks>
		[Obsolete( "Use another overload of the ShowHtml method that allows to supply the WebSecurityContext.", false )]
		public abstract void ShowHtml( string html );

		/// <summary>
		/// Renders the specified HTML text in the browser window as if it were downloaded from a web site or a file.
		/// </summary>
		/// <param name="html">HTML text. This must be a complete and well-formed web page, better XHTML.</param>
		/// <param name="ctx">Security context to display the content in. Defines whether the images are displayed, scripts executed, java enabled, and so on, also allows to speicfy one of the standard Internet Security Zones.</param>
		/// <remarks>
		/// <para>This function provides for displaying your page in the embedded web browser by directly uploading its contents into the browser.</para>
		/// <para>The browser maintains its history which allows to go Back and Forward within the browser using the Omea Back and Forward actions. The history typically consists of two stacks: pages visited in this session before the current page — Back stack, and pages visited "after" it (this means the pages we've visited but then undone using the "back" action) — Forward stack. If we navigate to a new page, the current one is pushed to the Back stack and the Forward stack is emptied. If we invoke the "back" action, the current page gets pushed into the Forward stack and a page popped from the Back stack gets into the browser. If we invoke the "forward" action, the current page goes into the "Back" stack and a page popped out of the Forward stack gets into the browser.</para>
		/// <para>Each time you inovke the ShowHtml action, the history is reset, which means that both Back and Forward stacks get empty and a new History session starts. If you press either Back or Forward button in this state, the action would not be processed by the browser and will be delegated to the Omea framework.</para>
		/// <para>Do not call this function before the control window is created. Use <see cref="Html"/> property instead.</para>
		/// </remarks>
		/// <since>2.0</since>
		public abstract void ShowHtml( string html, WebSecurityContext ctx );

		/// <summary>
		/// Renders the specified HTML text in the browser window as if it were downloaded from a web site or a file.
		/// </summary>
		/// <param name="html">HTML text. This must be a complete and well-formed web page, better XHTML.</param>
		/// <param name="ctx">Security context to display the content in. Defines whether the images are displayed, scripts executed, java enabled, and so on, also allows to speicfy one of the standard Internet Security Zones. May be <c>null</c>, in this case the browser defaults to some unspecified set of permissions.</param>
		/// <param name="wordsToHighlight">The words to be highlighted in the loaded content. May be <c>null</c>, which means no highlighting should be applied.</param>
		/// <remarks>
		/// <para>This function provides for displaying your page in the embedded web browser by directly uploading its contents into the browser.</para>
		/// <para>The browser maintains its history which allows to go Back and Forward within the browser using the Omea Back and Forward actions. The history typically consists of two stacks: pages visited in this session before the current page — Back stack, and pages visited "after" it (this means the pages we've visited but then undone using the "back" action) — Forward stack. If we navigate to a new page, the current one is pushed to the Back stack and the Forward stack is emptied. If we invoke the "back" action, the current page gets pushed into the Forward stack and a page popped from the Back stack gets into the browser. If we invoke the "forward" action, the current page goes into the "Back" stack and a page popped out of the Forward stack gets into the browser.</para>
		/// <para>Each time you inovke the ShowHtml action, the history is reset, which means that both Back and Forward stacks get empty and a new History session starts. If you press either Back or Forward button in this state, the action would not be processed by the browser and will be delegated to the Omea framework.</para>
		/// <para>You should use this method to apply highlighting, rather then <see cref="HighlightWords"/>, because it provides better performance and is capable of highlighting exactly those words that were found by the search, and not all the similar words in the document.</para>
		/// <para>Do not call this function before the control window is created. Use <see cref="Html"/> property instead.</para>
		/// </remarks>
		/// <since>2.0</since>
		public abstract void ShowHtml( string html, WebSecurityContext ctx, WordPtr[] wordsToHighlight );

		/// <summary>
		/// Highlights the specified words in the page text when download is complete.
		/// Can be used, for example, for highlighting search results.
		/// </summary>
		/// <param name="words">A set of words to highlight. May be <c>null</c> if no highlighting is actually required.</param>
		/// <param name="startOffset">The offset to subtract from the offsets passed in WordPtr structures.
		/// Words with offsets less than <paramref name="startOffset"/> should be ignored.</param>
		[Obsolete( "Use ShowHtml overload with the wordsToHighlight parameter to apply word highlighting on the content-feeding stage in order to improve highlighting performance and quality.", false )]
		public abstract void HighlightWords( WordPtr[] words, int startOffset );

		/// <summary>
		/// Checks if the command with the specified ID can be executed in the current state
		/// of the control.
		/// </summary>
		/// <param name="command">The ID of the command.</param>
		/// <returns>true if the ID of the command is known to the control and can be
		/// executed; false otherwise.</returns>
		public abstract bool CanExecuteCommand( string command );

		/// <summary>
		/// Executes the command with the specified ID.
		/// </summary>
		/// <param name="command">ID of the command to execute.</param>
		public abstract void ExecuteCommand( string command );

		/// <summary>
		/// Controls whether the browser displays images in the HTML page or not.
		/// </summary>
		[Obsolete( "Set WebSecurityContext.ShowPictures instead, and pass that secutity context to the ShowHtml( string, WebSecurityContext ) method.", false )]
		public abstract bool ShowImages { get; set; }

		/// <summary>
		/// The display name of the current Web page URL.
		/// </summary>
		/// <remarks>
		/// <para>Note that the property setter should not invoke navigation to the new URL.
		/// Instead, it should save the supplied value and use it for display whenever applicable
		/// instead of the original URL of the page currently displayed in the browser.
		/// The property getter should return the display name obtained from property setter, or,
		/// if it was not called, the location URL of the current Web page. Each navigation or
		/// content feeding action should reset the override established with the property setter.</para>
		/// <para>If the current URL is undefined (ie the browser has not been given the content or navigated yet), the property getter should return either a <c>null</c> value or an empty string. If the current URL is not applicable (ie the browser has been populated with content directly), the getter should return an empty string.</para>
		/// </remarks>
		public abstract string CurrentUrl { get; set; }

		/// <summary>
		/// Retrieves the text currently selected in the page, in an HTML-formatted representation.
		/// </summary>
		/// <remarks>
		/// <para>Remember that it is a costly operation. Take care when using it on a regular basis, and try to reduce the calls frequency.</para>
		/// </remarks>
		public abstract string SelectedHtml { get; }

		/// <summary>
		/// Retrieves the text currently selected in the page, in a plain-text, unformatted representation.
		/// </summary>
		/// <remarks>
		/// <para>Remember that it is a costly operation. Take care when using it on a regular basis, and try to reduce the calls frequency.</para>
		/// </remarks>
		public abstract string SelectedText { get; }

		/// <summary><seealso cref="TitleChanged"/>
		/// Gets the title of the document or Web page currently viewed in the browser.
		/// </summary>
		/// <remarks>
		/// <para>The <see cref="TitleChanged"/> event fires when the value of this property changes.</para>
		/// <para>If the title is not available (for example, the Web browser has not been created yet), returns an empty string.</para>
		/// </remarks>
		/// <since>2.0</since>
		public abstract string Title { get; }

		/// <summary><seealso cref="Title"/>
		/// Fires when the title of the document or Web page currently viewed in the browser (see <see cref="Title"/> property) changes.
		/// </summary>
		/// <since>2.0</since>
		public abstract event EventHandler TitleChanged;

		/// <summary>
		/// Represents the security context in which the content is being displayed by the Web browser.
		/// </summary>
		/// <remarks>
		/// <para>Note that in order to reuse the browser object it is shared between multiple plugins, so you have to re-apply the security context each time you want to show new content, to ensure that the security is not violated. Use the <see cref="ShowHtml(string, WebSecurityContext)"/> overload which allows to specify the context explicitly whenever possible.</para>
		/// </remarks>
		/// <since>2.0</since>
		public abstract WebSecurityContext SecurityContext { get; set; }

		/// <summary>
		/// The context provider that supplies the action context, which the browser should use
		/// for showing context menus and doing other UI actions.
		/// </summary>
		/// <remarks>
		/// <para>This is an input property rather than an output one, which means that
		/// the context provider is supplied into the Web browser by its consumer.
		/// It should be stored by the Web browser internally and used to retrieve
		/// the action context through its <see cref="IContextProvider.GetContext"/> method.
		/// The property getter should simply return the value supplied to the Web browser before.</para>
		/// </remarks>
		/// <since>2.0</since>
		public abstract IContextProvider ContextProvider { get; set; }

		/// <summary>
		/// Provides access to the HTML content currently being displayed in the Web browser. Also, by assigning this property to a new value, allows to upload HTML content into the browser.
		/// </summary>
		/// <value>HTML content being displayed in the browser.</value>
		/// <remarks>
		/// <para>This property can be set up at design-time or at any moment, even before the control window is created.</para>
		/// <para>A <c>null</c> value or empty string means that no content will be loaded into the control when it's created. Passing a <c>null</c> value when control has already been created is illegal. The default value that should be returned before the control is created and if no special content has been assigned to the control is an empty string.</para>
		/// </remarks>
		/// <since>2.0</since>
		public abstract string Html { get; set; }

		/// <summary>
		/// Creates a new instance of the Web browser control that can be placed on a Windows form.
		/// </summary>
		/// <returns>A browser control instance.</returns>
		/// <remarks>
		/// <para>For display panes, consider using the default instance of the abstract web browser.</para>
		/// </remarks>
		/// <since>2.0</since>
		public abstract AbstractWebBrowser NewInstance();

		/// <summary>
		/// Border style for the Web Browser control. Note that not all the possible enumeration values are necessarily supported.
		/// </summary>
		/// <since>2.0</since>
		public abstract BorderStyle BorderStyle { get; set; }

		/// <summary>
		/// Provides programmatical access to the document element of the HTML document loaded in the control.
		/// </summary>
		/// <since>2.0</since>
		public abstract IHtmlDomDocument HtmlDocument { get; }

		/// <summary>
		/// Fired by Web browser when page title changes so that the Resource Manager could update the title in UI if appropriate.
		/// </summary>
		/// <since>2.0</since>
		new public abstract event KeyEventHandler KeyDown;

		/// <summary><seealso cref="ContextMenuEventHandler"/><seealso cref="ContextMenuEventArgs"/>
		/// Fired by the Web browser when a context menu is about to be displayed.
		/// </summary>
		/// <remarks>
		/// <para>Having sinked this event, you may show your own context menu or cancel the default context menu displayed by the Web browser, which is the <see cref="IActionManager.ShowResourceContextMenu"/> menu by default.</para>
		/// <para>Also you may enable display of the underlying browser's native context menu in particular cases.</para>
		/// <para>The corresponding event argument properties, <see cref="ContextMenuEventArgs.CancelOmeaMenu"/> and <see cref="ContextMenuEventArgs.CancelNativeMenu"/>, are filled in by the Web browser before raising the event so that they would indicate the browser-preferred behavior which would have taken place in absence of custom processing in response to this event. For example, Omea menu is suppressed for anchors and images. You may use this information as well as the hit target to decide for showing the menu or not.</para>
		/// </remarks>
		/// <since>2.0</since>
		new public abstract event ContextMenuEventHandler ContextMenu;

		/// <summary>
		/// Retrieves the ready state of the Web browser.
		/// </summary>
		/// <since>2.0</since>
		public abstract BrowserReadyState ReadyState { get; }

		/// <summary><seealso cref="DocumentCompleteEventArgs"/><seealso cref="DocumentCompleteEventHandler"/>
		/// Fires when a document finishes loading into the Web browser and is completely ready for interaction, including the full DOM.
		/// </summary>
		/// <since>2.0</since>
		public abstract event DocumentCompleteEventHandler DocumentComplete;

		/// <summary>
		/// Cancels any pending navigation or download operation and stops any dynamic page elements, such as background sounds and animations.
		/// </summary>
		/// <since>2.0</since>
		public abstract void Stop();

		/// <summary><seealso cref="BeforeNavigateEventHandler"/><seealso cref="BeforeNavigateEventArgs"/>
		/// Fires before navigation occurs in the given object on either a window or frameset element.
		/// </summary>
		/// <remarks>Allows to cancel the navigation or control whether it occurs in-place, in external window, etc.</remarks>
		/// <since>2.0</since>
		public abstract event BeforeNavigateEventHandler BeforeNavigate;

		/// <summary><see cref="BeforeShowHtmlEventHandler"/><see cref="BeforeShowHtmlEventArgs"/>
		/// Fires before the HTML content gets actually uploaded into the Web browser.
		/// </summary>
		/// <remarks>
		/// <para>Provides access to the original HTML, the formatted HTML that contains highlighting data, and allows to cancel feeding the data.</para>
		/// </remarks>
		/// <since>2.0</since>
		public abstract event BeforeShowHtmlEventHandler BeforeShowHtml;
	}

	#region AbstractWebBrowser Events Data Types

	#region AbstractWebBrowser.ContextMenu Event Supplementary Data

	/// <summary><seealso cref="AbstractWebBrowser.ContextMenu"/><seealso cref="ContextMenuEventHandler"/>
	/// Arguments for the <see cref="AbstractWebBrowser.ContextMenu"/> event.
	/// </summary>
	/// <since>2.0</since>
	public class ContextMenuEventArgs : EventArgs
	{
		/// <summary>
		/// Specifies whether the default Omea context menu should be cancelled.
		/// </summary>
		/// <remarks>
		/// <para>If not cancelled, the default Omea menu will be displayed after returning from this handler.</para>
		/// <para>The actions present in this context menu are picked according to the context provided by the Web browser. If a context provider is specified for the Web browser, it is queried for the context as well.</para>
		/// <para>The default value for this parameter is <c>False</c>, which means that Omea context menu is the one and only context menu displayed in case the event handler does nothing.</para>
		/// </remarks>
		private bool _isOmeaMenuCancelled = false;

		/// <summary>
		/// Specifies whether the native Web browser's context menu should be cancelled.
		/// </summary>
		/// <para>If not cancelled, the native Web browser menu will be displayed after returning from this handler (after Omea menu displayes, if not cancelled).</para>
		/// <para>Context menu items for this menu are determined by the Web browser itself and do not correspond to Omea selected resource and so on.</para>
		/// <para>The default value for this parameter is <c>True</c>, which means that Omea context menu is the one and only context menu displayed in case the event handler does nothing.</para>
		private bool _isNativeMenuCancelled = true;

		/// <summary>
		/// X-coordinate for the context menu, in pixels. This value is given in screen coordinates.
		/// </summary>
		private int _x;

		/// <summary>
		/// Y-coordinate for the context menu, in pixels. This value is given in screen coordinates.
		/// </summary>
		private int _y;

		/// <summary>
		/// Type of the HTML element right-clicking on which has caused the context menu to appear, or the active element if the context menu was requested by keyboard.
		/// </summary>
		private ContextMenuTargetType _targetType;

		/// <summary>
		/// The HTML element right-clicking on which has caused the context menu to appear, or the active element if the context menu was requested by keyboard.
		/// </summary>
		private IHtmlDomElement _target;

		/// <summary>
		/// Action context for the context menu actions provided by the Web browser.
		/// </summary>
		private IActionContext _actionContext;

		/// <summary>
		/// Initializes the event arguments object.
		/// </summary>
		/// <param name="x">X-coordinate for the context menu, in pixels. This value is given in screen coordinates.</param>
		/// <param name="y">Y-coordinate for the context menu, in pixels. This value is given in screen coordinates.</param>
		/// <param name="targetType">Type of the HTML element right-clicking on which has caused the context menu to appear, or the active element if the context menu was requested by keyboard.</param>
		/// <param name="target">The HTML element right-clicking on which has caused the context menu to appear, or the active element if the context menu was requested by keyboard.</param>
		/// <param name="actionContext">Action context for the context menu actions provided by the Web browser.</param>
		public ContextMenuEventArgs( int x, int y, ContextMenuTargetType targetType, IHtmlDomElement target, IActionContext actionContext )
		{
			// Store the arguments
			_x = x;
			_y = y;
			_targetType = targetType;
			_target = target;
			_actionContext = actionContext;
		}

		/// <summary><seealso cref="CancelNativeMenu"/>
		/// Specifies whether the default Omea context menu should be cancelled.
		/// </summary>
		/// <remarks>
		/// <para>If not cancelled, the default Omea menu will be displayed after returning from this handler.</para>
		/// <para>The actions present in this context menu are picked according to the context provided by the Web browser. If a context provider is specified for the Web browser, it is queried for the context as well.</para>
		/// <para>The default value for this parameter is <c>False</c>, which means that Omea context menu is the one and only context menu displayed in case the event handler does nothing.</para>
		/// </remarks>
		public bool CancelOmeaMenu
		{
			get { return _isOmeaMenuCancelled; }
			set { _isOmeaMenuCancelled = value; }
		}

		/// <summary><seealso cref="CancelOmeaMenu"/>
		/// Specifies whether the native Web browser's context menu should be cancelled.
		/// </summary>
		/// <para>If not cancelled, the native Web browser menu will be displayed after returning from this handler (after Omea menu displayes, if not cancelled).</para>
		/// <para>Context menu items for this menu are determined by the Web browser itself and do not correspond to Omea selected resource and so on.</para>
		/// <para>The default value for this parameter is <c>True</c>, which means that Omea context menu is the one and only context menu displayed in case the event handler does nothing.</para>
		public bool CancelNativeMenu
		{
			get { return _isNativeMenuCancelled; }
			set { _isNativeMenuCancelled = value; }
		}

		/// <summary><seealso cref="Y"/>
		/// X-coordinate for the context menu, in pixels. This value is given in screen coordinates.
		/// </summary>
		public int X
		{
			get { return _x; }
		}

		/// <summary><seealso cref="X"/>
		/// Y-coordinate for the context menu, in pixels. This value is given in screen coordinates.
		/// </summary>
		public int Y
		{
			get { return _y; }
		}

		/// <summary><seealso cref="Target"/>
		/// Type of the HTML element right-clicking on which has caused the context menu to appear, or the active element if the context menu was requested by keyboard.
		/// </summary>
		public ContextMenuTargetType TargetType
		{
			get { return _targetType; }
		}

		/// <summary><seealso cref="TargetType"/>
		/// The HTML element right-clicking on which has caused the context menu to appear, or the active element if the context menu was requested by keyboard.
		/// </summary>
		public IHtmlDomElement Target
		{
			get { return _target; }
		}

		/// <summary>
		/// Action context for the context menu actions provided by the Web browser.
		/// </summary>
		public IActionContext ActionContext
		{
			get { return _actionContext; }
		}

	}

	/// <summary><seealso cref="AbstractWebBrowser.ContextMenu"/><seealso cref="ContextMenuEventArgs"/>
	/// Delegate type for the <see cref="JetBrains.Omea.OpenAPI.AbstractWebBrowser.ContextMenu"/> event.
	/// </summary>
	/// <since>2.0</since>
	public delegate void ContextMenuEventHandler( object sender, ContextMenuEventArgs args );

	#endregion

	#region AbstractWebBrowser.DocumentComplete Event Supplimentary Data

	/// <summary><seealso cref="AbstractWebBrowser.DocumentComplete"/><seealso cref="DocumentCompleteEventArgs"/>
	/// An event handler delegate type for the <see cref="AbstractWebBrowser.DocumentComplete"/> event.
	/// </summary>
	/// <since>2.0</since>
	public delegate void DocumentCompleteEventHandler( object sender, DocumentCompleteEventArgs args );

	/// <summary><seealso cref="AbstractWebBrowser.DocumentComplete"/><seealso cref="DocumentCompleteEventHandler"/>
	/// Event arguments class for the <see cref="AbstractWebBrowser.DocumentComplete"/> event.
	/// </summary>
	/// <since>2.0</since>
	public class DocumentCompleteEventArgs
	{
		/// <summary>
		/// URI of the document that has finished loading.
		/// </summary>
		private string _uri;

		/// <summary>
		/// Initializes the object.
		/// </summary>
		/// <param name="uri">URI of the document that has finished loading.</param>
		public DocumentCompleteEventArgs( string uri )
		{
			_uri = uri;
		}

		/// <summary>
		/// URI of the document that has finished loading.
		/// </summary>
		public string Uri
		{
			get { return _uri; }
		}

	}

	#endregion

	#region AbstractWebBrowser.BeforeNavigate Event Supplimentary Types

	/// <summary><seealso cref="AbstractWebBrowser.BeforeNavigate"/><seealso cref="BeforeNavigateEventArgs"/>
	/// An event handler delegate type for the <see cref="AbstractWebBrowser.BeforeNavigate"/> event.
	/// </summary>
	/// <since>2.0</since>
	public delegate void BeforeNavigateEventHandler( object sender, BeforeNavigateEventArgs args );

	/// <summary><seealso cref="AbstractWebBrowser.BeforeNavigate"/><seealso cref="BeforeNavigateEventHandler"/>
	/// Event arguments class for the <see cref="AbstractWebBrowser.BeforeNavigate"/> event.
	/// </summary>
	/// <since>2.0</since>
	public class BeforeNavigateEventArgs
	{
		#region Data

		private readonly string _uri;

		private readonly string _frame;

		private readonly object _postData;

		private readonly string _headers;

		private readonly BrowserNavigationCause _cause;

		private bool _cancel = false;

		private bool _inplace = false;

		#endregion

		#region Ctor

		public BeforeNavigateEventArgs( string uri, string frame, object postData, string headers, BrowserNavigationCause cause )
		{
			_uri = uri;
			_frame = frame;
			_postData = postData;
			_headers = headers;
			_cause = cause;
		}

		#endregion

		#region Gettersetters

		/// <summary>
		/// The URI to be navigated to.
		/// </summary>
		public string Uri
		{
			get { return _uri; }
		}

		/// <summary>
		/// The name of the frame in which the resource should be displayed, or NULL if no named frame is targeted for the resource.
		/// </summary>
		public string Frame
		{
			get { return _frame; }
		}

		/// <summary>
		/// HTTP POST data.
		/// </summary>
		public object PostData
		{
			get { return _postData; }
		}

		/// <summary>
		/// HTTP headers to be sent to server.
		/// </summary>
		public string Headers
		{
			get { return _headers; }
		}

		/// <summary>
		///	Gets or sets whether navigation should be cancelled.
		/// </summary>
		public bool Cancel
		{
			get { return _cancel; }
			set { _cancel = value; }
		}

		/// <summary>
		/// Gets or sets whether the navigation should happen in-place.
		/// </summary>
		public bool Inplace
		{
			get { return _inplace; }
			set { _inplace = value; }
		}

		/// <summary><seealso cref="BrowserNavigationCause"/>
		/// The cause due to which the navigation is about to occur.
		/// </summary>
		public BrowserNavigationCause Cause
		{
			get { return _cause; }
		}

		#endregion
	}

	#endregion

	#region AbstractWebBrowser.BeforeShowHtml Event Supplimentary Data

	/// <summary><seealso cref="AbstractWebBrowser.BeforeShowHtml"/><seealso cref="BeforeShowHtmlEventArgs"/>
	/// An event handler delegate type for the <see cref="AbstractWebBrowser.BeforeShowHtml"/> event.
	/// </summary>
	/// <since>2.0</since>
	public delegate void BeforeShowHtmlEventHandler( object sender, BeforeShowHtmlEventArgs args );

	/// <summary><seealso cref="AbstractWebBrowser.BeforeShowHtml"/><seealso cref="BeforeShowHtmlEventHandler"/>
	/// Event arguments class for the <see cref="AbstractWebBrowser.BeforeShowHtml"/> event.
	/// </summary>
	/// <since>2.0</since>
	public class BeforeShowHtmlEventArgs
	{
		#region Data

		private string _sourceHtml;

		private string _formattedHtml;

		private WebSecurityContext _ctx;

		private string _uriDisplayName;

		#endregion

		#region Ctor

		public BeforeShowHtmlEventArgs( string sourceHtml, string formattedHtml, WebSecurityContext ctx, string uriDisplayName )
		{
			_sourceHtml = sourceHtml;
			_formattedHtml = formattedHtml;
			_ctx = ctx;
			_uriDisplayName = uriDisplayName;

		}

		#endregion

		#region Gettersetters

		/// <summary><seealso cref="FormattedHtml"/>
		/// Gets the original HTML text that was submitted for upload to the <see cref="AbstractWebBrowser.ShowHtml"/> method or <see cref="AbstractWebBrowser.Html"/> property.
		/// </summary>
		/// <remarks>If you would like to alter the display text, put it into the <see cref="FormattedHtml"/> property.</remarks>
		public string SourceHtml
		{
			get { return _sourceHtml; }
		}

		/// <summary><seealso cref="SourceHtml"/>
		/// Gets or sets the HTML text that will be uploaded into the browser, with all the appropriate formatting applied,
		/// for example, search terms highlighting tags. It is non-<c>Null</c> even if no formatting was applied,
		/// use string comparison with <see cref="SourceHtml"/> if you need to check whether it was formatted or not.
		/// </summary>
		/// <remarks>You may apply additional formatting and submit the new HTML content into this property.</remarks>
		public string FormattedHtml
		{
			get { return _formattedHtml; }
			set { _formattedHtml = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="WebSecurityContext">security context</see> that will be used for displaying the provided content.
		/// </summary>
		public WebSecurityContext SecurityContext
		{
			get { return _ctx; }
			set { _ctx = value; }
		}

		/// <summary>
		/// Gets or sets the URI's display name, the same that can be furtherly accessed thru the <see cref="AbstractWebBrowser.CurrentUrl"/> property.
		/// </summary>
		/// <remarks>
		/// <para>As the manually-uploaded content has no native URI, it can be assigned manually to be then queried thru the
		/// <see cref="AbstractWebBrowser.CurrentUrl"/> property just as an ordinary Web page URI that can be displayed in the title,
		/// header, etc.</para>
		/// <para>Initially, this property will provide an empty string (<c>""</c>) as there's no predefined URI to be assigned
		/// to the manually-uploaded content.</para>
		/// </remarks>
		public string UriDisplayName
		{
			get { return _uriDisplayName; }
			set { _uriDisplayName = value; }
		}

		#endregion
	}

	#endregion

	#endregion

	#region Web Browser's Security Context et cetera

	/// <summary><seealso cref="AbstractWebBrowser"/>
	/// Defines a security context in which the Web browser should display its content. Use <see cref="WebSecurityContext.Internet"/>, <see cref="WebSecurityContext.Restricted"/>, or <see cref="WebSecurityContext.Trusted"/> to obtain a default security context.
	/// </summary>
	/// <remarks>
	/// <para>There is a set of predefined templates for the standard security contexts, which can be obtained from <see cref="WebSecurityContext.Internet"/>, <see cref="WebSecurityContext.Restricted"/>, or <see cref="WebSecurityContext.Trusted"/>. You can freely modify the returned objects to derive a security context from a standard one, and those changes would not affect other instances, both existing and future, obtained from the same property.</para>
	/// <para>The typical use consists of obtaining a default security context, setting up its parameters, and storing the object in your plugin, supplying it to the Web browser each time you wish to display some content.</para>
	/// <para>Note that in order to reuse the browser object it is shared between multiple plugins, so you have to re-apply the security context each time you want to show new content, to ensure that the security is not violated.</para>
	/// </remarks>
	/// <since>2.0</since>
	public class WebSecurityContext
	{
		/// <summary>
		/// You should not create instances of this class directly. Instead, consider using one of the default contexts <see cref="WebSecurityContext.Internet"/>, <see cref="WebSecurityContext.Restricted"/>, or <see cref="WebSecurityContext.Trusted"/>, and altering their individual parameters, if needed.
		/// </summary>
		private WebSecurityContext()
		{
		}

		/// <summary>
		/// Specifies whether graphical images should be included when pages are displayed.
		/// </summary>
		/// <remarks>
		/// <para>If <see cref="ShowPictures"/> is set to <c>False</c>, you can still display an individual picture on a Web page by right-clicking its icon, and then clicking Show Picture.</para>
		/// <para>If the pictures on the current page are still visible after you set <see cref="ShowPictures"/> to <c>False</c>, you can hide them by forcing Refresh.</para>
		/// </remarks>
		public bool ShowPictures
		{
			get { return _bShowPictures; }
			set { _bShowPictures = value; }
		}

		private bool _bShowPictures = true;

		/// <summary>
		/// Specifies whether video clips can play when pages are displayed.
		/// </summary>
		/// <remarks>
		/// <para>If <see cref="PlayVideos"/> is set to <c>False</c>, you can still display an individual animation on a Web page by right-clicking its icon, and then clicking Show Picture.</para>
		/// </remarks>
		public bool PlayVideos
		{
			get { return _bPlayVideos; }
			set { _bPlayVideos = value; }
		}

		private bool _bPlayVideos = true;

		/// <summary>
		/// Specifies whether music and other sounds can play when pages are displayed.
		/// </summary>
		/// <remarks>
		/// <para>If RealNetworks RealAudio is installed, or if a video clip is playing, some sounds might play even if you set <see cref="PlaySounds"/> to <c>False</c>.</para>
		/// </remarks>
		public bool PlaySounds
		{
			get { return _bPlaySounds; }
			set { _bPlaySounds = value; }
		}

		private bool _bPlaySounds = true;

		/// <summary>
		/// Specifies whether the Web browser is working in silent mode. In silent mode, no UI prompts are shown to the user.
		/// </summary>
		public bool SilentMode
		{
			get { return _bSilentMode; }
			set { _bSilentMode = value; }
		}

		private bool _bSilentMode = false;

		/// <summary>
		/// Specifies whether the Web browser is prevented form accessing the Internet while displaying the content.
		/// </summary>
		public bool WorkOffline
		{
			get { return _bWorkOffline; }
			set { _bWorkOffline = value; }
		}

		private bool _bWorkOffline = false;

		/// <summary>
		/// Defines the Internet Security Zone to display the Web page in.
		/// </summary>
		/// <remarks>
		/// <para>The Security Zones are defined in the Internet Options applet in the Control Panel.</para>
		/// <para>This property has no effect unless the <see cref="PermissionSet"/> property is set to <see cref="WebPermissionSet.Zone"/>.</para>
		/// <para>You might use one of the <see cref="System.Security.SecurityZone"/> enumeration constants to specify the Internet Security Zone (casted to <see cref="int"/>), as well as any of user-defined security zone numbers which typically reside in the 1000 … 10000 range, while the predefined security zones occupy the 0 … 999 space.</para>
		/// </remarks>
		public int SecurityZone
		{
			get { return _nSecurityZone; }
			set { _nSecurityZone = value; }
		}

		private int _nSecurityZone = (int) System.Security.SecurityZone.Untrusted;

		/// <summary><seealso cref="WebPermissionSet"/>
		/// Defines a <see cref="WebPermissionSet">permission set</see> that is granted to the content being viewed in the web browser.
		/// </summary>
		/// <remarks>If set to <see cref="WebPermissionSet.Zone"/>, the <see cref="SecurityZone"/> property takes effect.</remarks>
		public WebPermissionSet PermissionSet
		{
			get { return _permissions; }
			set { _permissions = value; }
		}

		private WebPermissionSet _permissions = WebPermissionSet.Auto;

		/// <summary>
		/// Specifies whether the user is allowed to navigate out of the Web page by clicking its links or invoking the navigation actions from client script.
		/// </summary>
		public bool AllowNavigation
		{
			get { return _bAllowNavigation; }
			set { _bAllowNavigation = value; }
		}

		private bool _bAllowNavigation = true;

		/// <summary>
		/// Specifies whether in-place navigation is allowed. If yes, the links are opened depending on the Omea settings, either inplace or in a new browser. If not, all the links are forced to be opened in another browser instance.
		/// </summary>
		public bool AllowInPlaceNavigation
		{
			get { return _bAllowInPlaceNavigation; }
			set { _bAllowInPlaceNavigation = value; }
		}

		private bool _bAllowInPlaceNavigation = true;

		/// <summary>
		/// Specifies whether user can select text in the browser with the mouse and carry out operations on the selection.
		/// </summary>
		public bool AllowSelect
		{
			get { return _bAllowSelect; }
			set { _bAllowSelect = value; }
		}

		private bool _bAllowSelect = true;

		/// <summary>
		/// The security context set up for default Internet browsing.
		/// </summary>
		public static WebSecurityContext Internet
		{
			get
			{
				WebSecurityContext ctx = new WebSecurityContext();

				ctx.PermissionSet = WebPermissionSet.Auto;
				ctx.PlaySounds = true;
				ctx.PlayVideos = true;
				ctx.ShowPictures = true;
				ctx.SilentMode = false;
				ctx.WorkOffline = false;
				ctx.AllowNavigation = true;
				ctx.AllowInPlaceNavigation = true;
				ctx.AllowSelect = true;

				return ctx;
			}
		}

		/// <summary>
		/// The security context set up for viewing the untrasted content.
		/// </summary>
		public static WebSecurityContext Restricted
		{
			get
			{
				WebSecurityContext ctx = new WebSecurityContext();

				ctx.PermissionSet = WebPermissionSet.Nothing;
				ctx.PlaySounds = true;
				ctx.PlayVideos = true;
				ctx.ShowPictures = true;
				ctx.SilentMode = true;
				ctx.WorkOffline = true;
				ctx.AllowNavigation = true;
				ctx.AllowInPlaceNavigation = true;
				ctx.AllowSelect = true;

				return ctx;
			}
		}

		/// <summary>
		/// The security context set up for viewing the trusted content.
		/// </summary>
		public static WebSecurityContext Trusted
		{
			get
			{
				WebSecurityContext ctx = new WebSecurityContext();

				ctx.PermissionSet = WebPermissionSet.Everything;
				ctx.PlaySounds = true;
				ctx.PlayVideos = true;
				ctx.ShowPictures = true;
				ctx.SilentMode = false;
				ctx.WorkOffline = false;
				ctx.AllowNavigation = true;
				ctx.AllowInPlaceNavigation = true;
				ctx.AllowSelect = true;

				return ctx;
			}
		}
	}

	/// <summary><seealso cref="WebSecurityContext.PermissionSet"/>
	/// Defines a permission set which applies to a page being viewed in the Web browser window through the <see cref="WebSecurityContext">security context</see> object.
	/// </summary>
	/// <since>2.0</since>
	public enum WebPermissionSet
	{
		/// <summary>
		/// No permissions granted. The Web page is being viewed in the restricted environment. Use for content obtained from an untrysty source or one which display the user cannot control (e. g. mail messages).
		/// </summary>
		Nothing,

		/// <summary>
		/// All permissions granted. The Web page is being viewed in the trusted environment. Use for content obtained from a trustworthy source, for example, the pages generated by Omea which need to interact with the application.
		/// </summary>
		Everything,

		/// <summary>
		/// The security zone is determined automatically, using the Internet Security Zones set up on this computer (see Control Panel, Internet Options). This option needs the content URI to function and is not effective if the content is being directly fed into the Web browser and has no URI. In the latter case, the Nothing permission set is assumed by default.
		/// </summary>
		Auto,

		/// <summary>
		/// An Internet Security Zone is explicitly defined by the <see cref="WebSecurityContext.SecurityZone"/> property. The Zones are defined by the Internet Options applet form the Control Panel.
		/// </summary>
		Zone
	}

	#endregion

	#endregion

	#region HtmlDom

	/// <summary>
	/// A base wrapper for the generic object in the W3C HTML Document Object Model.
	/// </summary>
	/// <since>2.0</since>
	public interface IHtmlDomObject
	{
		/// <summary>
		/// Provides immediate access to an object that represents the document object. This object can be called via the late binding mechanism from the languages capable of making such calls.
		/// </summary>
		object Instance { get; }

		/// <summary>
		/// Invokes a generic method on the object.
		/// </summary>
		/// <param name="name">Name of the method.</param>
		/// <param name="args">Arguments for the invocation.</param>
		/// <returns>Return value of the method.</returns>
		object InvokeMethod( string name, params object[] args );

		/// <summary>
		/// Retrieves a value of the generic property of the object.
		/// </summary>
		/// <param name="name">Name of the property.</param>
		/// <returns>Property value.</returns>
		object GetProperty( string name );

		/// <summary>
		/// Assigns a value to a generic property of the object.
		/// </summary>
		/// <param name="name">Name of the property.</param>
		/// <param name="value">Proposed property value.</param>
		void SetProperty( string name, object value );
	}

	/// <summary>
	/// A wrapper for the Document object in the W3C HTML Document Object Model.
	/// </summary>
	/// <since>2.0</since>
	public interface IHtmlDomDocument : IHtmlDomObject
	{
		/// <summary>
		/// Looks up an HTML element with a specific ID.
		/// </summary>
		/// <param name="id">ID of the element.</param>
		/// <returns>The desired HTML element, or <c>Null</c>, if it cannot be found.</returns>
		IHtmlDomElement GetElementById( string id );

		/// <summary>
		/// The body element of the document.
		/// </summary>
		IHtmlDomElement Body { get; }

		/// <summary>
		/// Creates an instance of the HTML element for the specified tag.
		/// </summary>
		/// <param name="tagName">Tag name for the element.</param>
		/// <returns>The newly-created element.</returns>
		IHtmlDomElement CreateElement( string tagName );
	}

	/// <summary>
	/// A wrapper for an element object in the W3C HTML Document Object Model.
	/// </summary>
	/// <since>2.0</since>
	public interface IHtmlDomElement : IHtmlDomObject
	{
		#region General

		/// <summary>
		/// Gets or sets the ID of an HTML element.
		/// </summary>
		string Id { get; set; }

		/// <summary>
		/// Gets or sets the name of an HTML element.
		/// </summary>
		/// <remarks>The name is represented by the <c>name</c> HTML attribute.</remarks>
		string Name { get; set; }

		#endregion

		#region Contents

		/// <summary>
		/// Gets or sets the HTML representation of element contents, not including the own element's tags.
		/// </summary>
		string InnerHtml { get; set; }

		/// <summary>
		/// Gets the HTML representation of element contents, including the own element's tags.
		/// </summary>
		string OuterHtml { get; }

		/// <summary>
		/// Gets or sets the text representation of element's contents.
		/// </summary>
		string InnerText { get; set; }

		#endregion

		#region Positioning

		/// <summary>
		/// Causes the object to scroll into view, aligning it either at the top or bottom of the window.
		/// </summary>
		/// <param name="bAlignToTop">
		/// <para>If <c>True</c>, scrolls the object so that top of the object is visible at the top of the window.</para>
		/// <para>If <c>False</c>, scrolls the object so that the bottom of the object is visible at the bottom of the window.</para>
		/// </param>
		void ScrollIntoView( bool bAlignToTop );

		/// <summary>
		/// Gets the calculated top position of the object relative to the layout or coordinate parent, as specified by the <c>offsetParent</c> property.
		/// The dimensions are measured in pixels.
		/// </summary>
		int OffsetTop { get; }

		/// <summary>
		/// Gets the calculated left position of the object relative to the layout or coordinate parent, as specified by the <c>offsetParent</c> property.
		/// The dimensions are measured in pixels.
		/// </summary>
		int OffsetLeft { get; }

		/// <summary>
		/// Gets the height of the object relative to the layout or coordinate parent, as specified by the <c>offsetParent</c> property.
		/// The dimensions are measured in pixels.
		/// </summary>
		int OffsetHeight { get; }

		/// <summary>
		/// Gets the width of the object relative to the layout or coordinate parent, as specified by the <c>offsetParent</c> property.
		/// The dimensions are measured in pixels.
		/// </summary>
		int OffsetWidth { get; }

		/// <summary>
		/// Gets or sets the distance between the left edge of the object and the leftmost portion of the content currently visible in the window.
		/// The dimensions are measured in pixels.
		/// </summary>
		int ScrollLeft { get; set; }

		/// <summary>
		/// Gets or sets the distance between the top of the object and the topmost portion of the content currently visible in the window.		/// The dimensions are measured in pixels.
		/// </summary>
		int ScrollTop { get; set; }

		/// <summary>
		/// Gets the scrolling width of the object.
		/// The dimensions are measured in pixels.
		/// </summary>
		int ScrollWidth { get; }

		/// <summary>
		/// Gets the scrolling height of the object.
		/// The dimensions are measured in pixels.
		/// </summary>
		int ScrollHeight { get; }

		/// <summary>
		/// Gets the distance between the <see cref="OffsetLeft"/> property and the true left side of the client area.
		/// </summary>
		/// <remarks>
		/// The difference between the <see cref="OffsetLeft"/> and <see cref="ClientLeft"/> properties is the border of the object.
		/// </remarks>
		int ClientLeft { get; }

		/// <summary>
		/// Gets the distance between the <see cref="OffsetTop"/> property and the true top of the client area.
		/// </summary>
		/// <remarks>
		/// The difference between the <see cref="OffsetTop"/> and <see cref="ClientTop"/> properties is the border area of the object.
		/// </remarks>
		int ClientTop { get; }

		/// <summary>
		/// Gets the width of the object including padding, but not including margin, border, or scroll bar.
		/// </summary>
		int ClientWidth { get; }

		/// <summary>
		/// Gets the height of the object including padding, but not including margin, border, or scroll bar.
		/// </summary>
		int ClientHeight { get; }

		/// <summary>
		/// Simulates a click on a scroll-bar component.
		/// </summary>
		/// <param name="action">Specifies how the object scrolls.</param>
		/// <remarks>
		/// <para>Cascading Style Sheets (CSS) allow you to scroll on all objects through the <c>Overflow</c> property.</para>
		/// <para>When the content of an element changes and causes scroll bars to display, the <see cref="DoScroll"/> method might not work correctly immediately following the content update. When this happens, you can use the <c>SetTimeout</c> method to enable the browser to recognize the dynamic changes that affect scrolling.</para>
		/// </remarks>
		void DoScroll( ScrollAction action );

		/// <summary>
		/// Gets a reference to the container object that defines the <see cref="OffsetTop"/> and <see cref="OffsetLeft"/> properties of the object.
		/// </summary>
		IHtmlDomElement OffsetParent { get; }

		#endregion

		#region Hierarchy

		/// <summary>
		/// Gets the parent object in the object hierarchy.
		/// </summary>
		/// <remarks>The topmost object returns null as its parent.</remarks>
		IHtmlDomElement ParentElement { get; }

		/// <summary>
		/// Removes the object from the document hierarchy.
		/// </summary>
		/// <param name="deep">Specifies whether the child nodes of the object are removed.</param>
		void RemoveNode( bool deep );

		/// <summary>
		/// Gets a collection of child nodes (HTML elements and text nodes) of this element.
		/// </summary>
		/// <remarks>Note that a text node object lacks support for some of the properties or methods.</remarks>
		IEnumerable /*<IHtmlDomElement>*/ ChildNodes { get; }

		/// <summary>
		/// Inserts an element into the document hierarchy as a child node of a parent object.
		/// </summary>
		/// <param name="newChild">Element to be inserted. Elements can be created with <see cref="IHtmlDomDocument.CreateElement"/>.</param>
		/// <param name="refChild">Element before which a new one should be inserted, or <c>Null</c> to insert at the end.</param>
		/// <returns>The newly-inserted element.</returns>
		IHtmlDomElement InsertBefore( IHtmlDomElement newChild, IHtmlDomElement refChild );

		/// <summary>
		/// Appends an element as a child to the object.
		/// </summary>
		/// <param name="newChild">Element to be inserted. Elements can be created with <see cref="IHtmlDomDocument.CreateElement"/>.</param>
		/// <returns>The newly-inserted element.</returns>
		IHtmlDomElement AppendChild( IHtmlDomElement newChild );

		/// <summary>
		/// Returns a collection of the child elements (direct and indirect) with the tag name specified.
		/// </summary>
		/// <param name="tagname">Tag name of the child elements, case-insensitive by default.</param>
		/// <returns>An enumerator for the collection.</returns>
		IEnumerable /*<IHtmlDomElement>*/ GetElementsByTagName( string tagname );

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the name of the CSS class assigned to this element.
		/// </summary>
		/// <remarks>The class name is represented by the <c>class</c> attribute of an HTML element, like "<c>selected</c>" in "<c>&lt;div class="selected"&gt;text&lt;/div&gt;</c>".</remarks>
		string ClassName { get; set; }

		/// <summary>
		/// Gets the tag name of the object.
		/// </summary>
		/// <remarks>A tag name is the name of the tag which represents this element in HTML text, for example, "<c>DIV</c>" for a "<c>&lt;div&gt;some content&lt;/div&gt;</c>".</remarks>
		string TagName { get; }

		/// <summary>
		/// Retrieves an attribute of the HTML element by its name.
		/// </summary>
		/// <param name="name">Name of the attribuite, either case-sensitive or not.</param>
		/// <param name="flags">Flags that affect the retrieval.</param>
		/// <returns>Attribute value.</returns>
		/// <remarks>In the case-insensitive case, if more than one attribute fits the name, only one is returned.</remarks>
		object GetAttribute( string name, GetAttributeFlags flags );

		/// <summary>
		/// Retrieves an attribute of the HTML element by its name.
		/// </summary>
		/// <param name="name">Name of the attribuite, case-insensitive.</param>
		/// <returns>Attribute value.</returns>
		/// <remarks>In the case-insensitive case, if more than one attribute fits the name, only one is returned.</remarks>
		object GetAttribute( string name );

		/// <summary>
		/// Assigns a value to an attribute of the HTML element.
		/// </summary>
		/// <param name="name">Name of the attribute.</param>
		/// <param name="value">Proposed value for the attribute.</param>
		/// <param name="caseSensitive">Specifies whether the attribute search is case-sensitive or not.</param>
		/// <remarks>Case-insensitive search picks the first suitable attribute.</remarks>
		void SetAttribute( string name, object value, bool caseSensitive );

		#endregion

		#region Events

		#region Mouse Events Family

		/// <summary>
		/// Fires when the user clicks the left mouse button on the object.
		/// </summary>
		event HtmlEventHandler Click;

		/// <summary>
		/// Fires when the user double-clicks the left mouse button on the object.
		/// </summary>
		event HtmlEventHandler DoubleClick;

		/// <summary>
		/// Fires when the user moves the mouse pointer into the object.
		/// </summary>
		event HtmlEventHandler MouseEnter;

		/// <summary>
		/// Fires when the user moves the mouse pointer outside the boundaries of the object.
		/// </summary>
		event HtmlEventHandler MouseLeave;

		/// <summary>
		/// Fires when the user clicks the object with either mouse button.
		/// </summary>
		event HtmlEventHandler MouseDown;

		/// <summary>
		/// Fires when the wheel button is rotated.
		/// </summary>
		event HtmlEventHandler MouseWheel;

		/// <summary>
		/// Fires when the user moves the mouse over the object.
		/// </summary>
		event HtmlEventHandler MouseMove;

		/// <summary>
		/// Fires when the user moves the mouse pointer outside the boundaries of the object.
		/// </summary>
		event HtmlEventHandler MouseOut;

		/// <summary>
		/// Fires when the user moves the mouse pointer into the object.
		/// </summary>
		event HtmlEventHandler MouseOver;

		/// <summary>
		/// Fires when the user releases a mouse button while the mouse is over the object.
		/// </summary>
		event HtmlEventHandler MouseUp;

		#endregion

		/// <summary>
		/// Fires when the user clicks the right mouse button in the client area, opening the context menu.
		/// </summary>
		event HtmlEventHandler ContextMenu;

		#region Drag'n'Drop Events Family

		/// <summary>
		/// Fires on the source object continuously during a drag operation.
		/// </summary>
		/// <remarks>This event fires on the source object after the <see cref="DragStart"/> event. The <see cref="Drag"/> event fires throughout the drag operation, whether the selection being dragged is over the drag source, a valid target, or an invalid target.</remarks>
		event HtmlEventHandler Drag;

		/// <summary>
		/// Fires on the target object when the mouse button is released during a drag-and-drop operation.
		/// </summary>
		/// <remarks>
		/// <para>The <see cref="Drop"/> event fires before the <see cref="DragLeave"/> and <see cref="DragEnd"/> events. Use the <see cref="HtmlEventArgs.ReturnValue"/> property to disable the default action.</para>
		/// <para>You must cancel the default action for <see cref="DragEnter"/> and <see cref="DragOver"/> in order for <see cref="Drop"/> to fire.</para>
		/// </remarks>
		event HtmlEventHandler Drop;

		/// <summary>
		/// Fires on the target element when the user drags the object to a valid drop target.
		/// </summary>
		/// <remarks>
		/// <para>You can handle the <see cref="DragEnter"/> event on the source or on the target object. Of the target events, it is the first to fire during a drag operation. Target events use the <c>getData</c> method to stipulate which data and data formats to retrieve. The list of drag-and-drop target events includes: </para>
		/// <list type="bullet">
		/// <item><description><see cref="BeforePaste"/></description></item>
		/// <item><description><see cref="Paste"/></description></item>
		/// <item><description><see cref="DragEnter"/></description></item>
		/// <item><description><see cref="DragOver"/></description></item>
		/// <item><description><see cref="DragLeave"/></description></item>
		/// <item><description><see cref="Drop"/></description></item>
		/// </list>
		/// <para>When scripting custom functionality, use the <see cref="HtmlEventArgs.ReturnValue"/> property to disable the default action.</para>
		/// </remarks>
		event HtmlEventHandler DragEnter;

		/// <summary>
		/// Fires on the target object when the user moves the mouse out of a valid drop target during a drag operation.
		/// </summary>
		/// <remarks>The <see cref="DragLeave"/> event does not support the <see cref="HtmlEventArgs"/> object's <see cref="HtmlEventArgs.ToElement"/> and <see cref="HtmlEventArgs.FromElement"/> properties.</remarks>
		event HtmlEventHandler DragLeave;

		/// <summary>
		/// Fires on the target element continuously while the user drags the object over a valid drop target.
		/// </summary>
		/// <remarks>
		/// <para>The <see cref="DragOver"/> event fires on the target object after the <see cref="DragEnter"/> event has fired.</para>
		/// <para>When scripting custom functionality, use the <see cref="HtmlEventArgs.ReturnValue"/> property to disable the default action.</para>
		/// </remarks>
		event HtmlEventHandler DragOver;

		/// <summary>
		/// Fires on the source object when the user starts to drag a text selection or selected object.
		/// </summary>
		/// <remarks>
		/// <para>The <see cref="DragStart"/> event is the first to fire when the user starts to drag the mouse. It is essential to every drag operation, yet is just one of several source events in the data transfer object model. Source events use the <c>setData</c> method of the <c>dataTransfer</c> object to provide information about data being transferred. Source events include <see cref="DragStart"/>, <see cref="Drag"/>, and <see cref="DragEnd"/>.</para>
		/// <para>When dragging anything other than an <c>img</c> object, some text to be dragged must be selected.</para>
		/// </remarks>
		event HtmlEventHandler DragStart;

		/// <summary>
		/// Fires on the source object when the user releases the mouse at the close of a drag operation.
		/// </summary>
		/// <remarks>The <see cref="DragEnd"/> event is the final drag event to fire, following the <see cref="DragLeave"/> event, which fires on the target object.</remarks>
		event HtmlEventHandler DragEnd;

		#endregion

		#region CopyPaste Events Family

		/// <summary>
		/// Fires on the target object when the user pastes data, transferring the data from the system clipboard to the document.
		/// </summary>
		event HtmlEventHandler Paste;

		/// <summary>
		/// Fires on the target object before the selection is pasted from the system clipboard to the document.
		/// </summary>
		event HtmlEventHandler BeforePaste;

		/// <summary>
		/// Fires on the source object before the selection is copied to the system clipboard.
		/// </summary>
		event HtmlEventHandler BeforeCopy;

		/// <summary>
		/// Fires on the source element when the user copies the object or selection, adding it to the system clipboard.
		/// </summary>
		event HtmlEventHandler Copy;

		/// <summary>
		/// Fires on the source object before the selection is deleted from the document.
		/// </summary>
		event HtmlEventHandler BeforeCut;

		/// <summary>
		/// Fires on the source element when the object or selection is removed from the document and added to the system clipboard.
		/// </summary>
		event HtmlEventHandler Cut;

		#endregion

		/// <summary>
		/// Fires when a property changes on the object.
		/// </summary>
		event HtmlEventHandler PropertyChange;

		/// <summary>
		/// Fires when the size of the object is about to change.
		/// </summary>
		event HtmlEventHandler Resize;

		#endregion
	}

	#region HtmlDom Events Supplimentary Data Types

	/// <summary>
	/// Handler for the HTML events.
	/// </summary>
	/// <since>2.0</since>
	public delegate void HtmlEventHandler( object sender, HtmlEventArgs args );

	/// <summary>
	/// The event arguments object for the HTML events.
	/// </summary>
	/// <since>2.0</since>
	public abstract class HtmlEventArgs : HtmlDomObject
	{
		/// <summary>
		/// Initializes the instance by passing in the native event args object.
		/// </summary>
		/// <param name="eventArgsObject"></param>
		public HtmlEventArgs( object eventArgsObject )
			: base( eventArgsObject )
		{
		}

		/// <summary>Retrieves a value that indicates the state of the ALT key.  </summary>
		public bool AltKey
		{
			get { return (bool) GetProperty( "altKey" ); }
		}

		/// <summary>Retrieves the mouse button pressed by the user.  </summary>
		/// <remarks>This property is used with the onmousedown, onmouseup, and onmousemove events. For other events, it defaults to 0 regardless of the state of the mouse buttons. </remarks>
		public MouseButtons Button
		{
			get
			{
				MouseButtons ret = 0;
				int nButtons = (int) GetProperty( "button" );
				if( (nButtons & 1) != 0 )
					ret |= MouseButtons.Left;
				if( (nButtons & 2) != 0 )
					ret |= MouseButtons.Right;
				if( (nButtons & 4) != 0 )
					ret |= MouseButtons.Middle;

				return ret;
			}
		}

		/// <summary>Sets or retrieves whether the current event should bubble up the hierarchy of event handlers.  </summary>
		/// <remarks>Using this property to cancel bubbling for an event does not affect subsequent events. </remarks>
		public bool CancelBubble
		{
			get { return (bool) GetProperty( "cancelBubble" ); }
			set { SetProperty( "cancelBubble", value ); }
		}

		/// <summary>Retrieves the x-coordinate of the mouse pointer's position relative to the client area of the window, excluding window decorations and scroll bars. </summary>
		/// <remarks>Within a viewlink, the client area begins at the edge of the master element. </remarks>
		public int ClientX
		{
			get { return (int) GetProperty( "clientX" ); }
		}

		/// <summary>Retrieves the y-coordinate of the mouse pointer's position relative to the client area of the window, excluding window decorations and scroll bars. </summary>
		/// <remarks>Within a viewlink, the client area begins at the edge of the master element. </remarks>
		public int ClientY
		{
			get { return (int) GetProperty( "clientY" ); }
		}

		/// <summary>Retrieves the state of the CTRL key.  </summary>
		public bool CtrlKey
		{
			get { return (bool) GetProperty( "ctrlKey" ); }
		}

		/// <summary>Retrieves the object from which activation or the mouse pointer is exiting during the event.  </summary>
		public abstract IHtmlDomElement FromElement { get; }

		/// <summary>Sets or retrieves the Unicode key code associated with the key that caused the event.  </summary>
		/// <remarks>The property is used with the onkeydown, onkeyup, and onkeypress events. The property's value is 0 if no key caused the event.</remarks>
		public char KeyCode
		{
			get { return (char) (int) GetProperty( "keyCode" ); }
			set { SetProperty( "keyCode", value ); }
		}

		/// <summary>Retrieves the x-coordinate of the mouse pointer's position relative to the object firing the event.  </summary>
		/// <remarks>The coordinates match the <see cref="IHtmlDomElement.OffsetLeft"/> and <see cref="IHtmlDomElement.OffsetTop"/> properties of the object. Use <see cref="IHtmlDomElement.OffsetParent"/> to find the container object that defines this coordinate system. </remarks>
		public int OffsetX
		{
			get { return (int) GetProperty( "offsetX" ); }
		}

		/// <summary>Retrieves the y-coordinate of the mouse pointer's position relative to the object firing the event.  </summary>
		/// <remarks>The coordinates match the <see cref="IHtmlDomElement.OffsetLeft"/> and <see cref="IHtmlDomElement.OffsetTop"/> properties of the object. Use <see cref="IHtmlDomElement.OffsetParent"/> to find the container object that defines this coordinate system. </remarks>
		public int OffsetY
		{
			get { return (int) GetProperty( "offsetY" ); }
		}

		/// <summary>Retrieves the name of the data member provided by a data source object.  </summary>
		public string Qualifier
		{
			get { return (string) GetProperty( "qualifier" ); }
		}

		/// <summary>Retrieves the result of the data transfer for a data source object.  </summary>
		public DataTransferReason Reason
		{
			get { return (DataTransferReason) GetProperty( "reason" ); }
		}

		/// <summary>Sets or retrieves the return value from the event.  </summary>
		/// <remarks>
		/// <para><c>True</c> is the default. Value from the event is returned.</para>
		/// <para><c>False</c>. Default action of the event on the source object is canceled.</para>
		/// </remarks>
		public bool ReturnValue
		{
			get { return (bool) GetProperty( "returnValue" ); }
			set { SetProperty( "returnValue", value ); }
		}

		/// <summary>Retrieves the x-coordinate of the mouse pointer's position relative to the user's screen.  </summary>
		public int ScreenX
		{
			get { return (int) GetProperty( "screenX" ); }
		}

		/// <summary>Retrieves the y-coordinate of the mouse pointer's position relative to the user's screen.  </summary>
		public int ScreenY
		{
			get { return (int) GetProperty( "screenY" ); }
		}

		/// <summary>Retrieves the state of the SHIFT key.  </summary>
		public bool ShiftKey
		{
			get { return (bool) GetProperty( "shiftKey" ); }
		}

		/// <summary>Retrieves the object that fired the event.  </summary>
		public abstract IHtmlDomElement SrcElement { get; }

		/// <summary>Retrieves the filter object that caused the onfilterchange event to fire.  </summary>
		public IHtmlDomObject SrcFilter
		{
			get
			{
				object o = GetProperty( "srcFilter" );
				return o != null ? new HtmlDomObject( o ) : null;
			}
		}

		/// <summary>Retrieves a reference to the object toward which the user is moving the mouse pointer. </summary>
		public abstract IHtmlDomElement ToElement { get; }

		/// <summary>Sets or retrieves the event name from the event object.  </summary>
		/// <remarks>Events are returned without the <c>"on"</c> prefix. For example, the onclick event is returned as <c>"click"</c>. </remarks>
		public string Type
		{
			get { return (string) GetProperty( "type" ); }
			set { SetProperty( "type", value ); }
		}

		/// <summary>Retrieves the x-coordinate, in pixels, of the mouse pointer's position relative to a relatively positioned parent element. </summary>
		public int X
		{
			get { return (int) GetProperty( "x" ); }
		}

		/// <summary>Retrieves the y-coordinate, in pixels, of the mouse pointer's position relative to a relatively positioned parent element. </summary>
		public int Y
		{
			get { return (int) GetProperty( "y" ); }
		}

		/// <summary>
		/// Sets or retrieves the name of the property that changes on the object.
		/// </summary>
		public string PropertyName
		{
			get { return (string) GetProperty( "propertyName" ); }
		}
	}

	#endregion

	/// <summary>
	/// A default implementation for the HTML DOM object.
	/// </summary>
	/// <since>2.0</since>
	public class HtmlDomObject : IHtmlDomObject
	{
		private object _instanceBase;

		public HtmlDomObject( object instance )
		{
			_instanceBase = instance;
		}

		/// <summary>
		/// Wraps the given value into a new instance of <see cref="HtmlDomObject"/>.
		/// Returns a pure <c>Null</c> if the <paramref name="value"/> is <see>Null</see>,
		/// instead of a non-null <see cref="HtmlDomObject"/> instance wrapping a new value.
		/// </summary>
		/// <param name="value">Value to be wrapped.</param>
		/// <returns>The wrapped value, or <c>Null</c>.</returns>
		public static HtmlDomObject AttachBase(object value)
		{
			return value != null ? new HtmlDomObject(value) : null;
		}

		#region IHtmlDomObject Members

		public object Instance
		{
			get { return _instanceBase; }
		}

		public object InvokeMethod( string name, params object[] args )
		{
			if( _instanceBase == null )
				throw new NullReferenceException();
			return _instanceBase.GetType().InvokeMember( name, BindingFlags.InvokeMethod, null, _instanceBase, args );
		}

		public object GetProperty( string name )
		{
			if( _instanceBase == null )
				throw new NullReferenceException();
			return _instanceBase.GetType().InvokeMember( name, BindingFlags.GetProperty, null, _instanceBase, new object[] {} );
		}

		public void SetProperty( string name, object value )
		{
			if( _instanceBase == null )
				throw new NullReferenceException();
			_instanceBase.GetType().InvokeMember( name, BindingFlags.SetProperty, null, _instanceBase, new object[] {value} );
		}

		#endregion
	}

	#endregion

	#region HtmlDom and WebBrowser Constants & Enumerations

	/// <summary>
	/// Lists possible values for the <see cref="HtmlEventArgs.Reason"/> property, which represents the result of the data transfer for a data source object.
	/// </summary>
	/// <since>2.0</since>
	public enum DataTransferReason
	{
		/// <summary>
		/// Data transmitted successfully.
		/// </summary>
		Success,
		/// <summary>
		/// Data transfer aborted.
		/// </summary>
		Aborted,
		/// <summary>
		/// Data transferred in error.
		/// </summary>
		Error
	}

	/// <summary><seealso cref="IHtmlDomElement.DoScroll"/>
	/// Specifies how the HTML object scrolls.
	/// </summary>
	/// <since>2.0</since>
	public enum ScrollAction
	{
		/// <summary>
		/// Scrolls the object in the specified direction.
		/// </summary>
		Left,
		/// <summary>
		/// Scrolls the object in the specified direction.
		/// </summary>
		Up,
		/// <summary>
		/// Scrolls the object in the specified direction.
		/// </summary>
		Right,
		/// <summary>
		/// Scrolls the object in the specified direction.
		/// </summary>
		Down,
		/// <summary>
		/// Scrolls the object in the specified direction.
		/// </summary>
		PageLeft,
		/// <summary>
		/// Scrolls the object in the specified direction.
		/// </summary>
		PageUp,
		/// <summary>
		/// Scrolls the object in the specified direction.
		/// </summary>
		PageRight,
		/// <summary>
		/// Scrolls the object in the specified direction.
		/// </summary>
		PageDown
	}

	/// <summary>
	/// Available types of the content menu targets in the Web browser.
	/// Note that some of them are for internal use only.
	/// </summary>
	/// <remarks>
	/// <para>Numeric values of the basic constants correspond to the MSHTML's context menu constants and thus must be immutable.</para>
	/// <para>Origin: mshtmhst.h</para>
	/// </remarks>
	/// <since>2.0</since>
	public enum ContextMenuTargetType : int
	{
		Default = 0,
		Image = 1,
		Control = 2,
		Table = 3,
		TextSelect = 4,
		Anchor = 5,
		Unknown = 6,
		Imgdynsrc = 7,
		Imgart = 8,
		Debug = 9,
		Vscroll = 10,
		Hscroll = 11
	}

	/// <summary>
	/// Flags for the <see cref="IHtmlDomElement.GetAttribute(string, GetAttributeFlags)"/> method.
	/// </summary>
	/// <since>2.0</since>
	[Flags]
	public enum GetAttributeFlags
	{
		/// <summary>
		/// No flags.
		/// </summary>
		None = 0,
		/// <summary>
		/// Performs a case-sensitive search for the attribute by its name.
		/// </summary>
		CaseSensitive = 1,
		/// <summary>
		/// Returns the attribute value exactly as it was set.
		/// </summary>
		ExactValue = 2
	}

	/// <summary>
	/// Represents the ready state of the browser that tells whether the browser is currently loading the page, parsing its content, or finished and ready for work.
	/// </summary>
	/// <remarks>Particular Web browsers may implement only a subset of these states. However, the <see cref="Complete"/> state must be supported by any browser and it must be achieved only after the page is completely loaded and ready for full-scale DOM interaction.</remarks>
	/// <since>2.0</since>
	public enum BrowserReadyState
	{
		///<summary>Default initialization state.</summary>
		Uninitialized = 0,

		///<summary>Object is currently loading its properties.</summary>
		Loading = 1,

		///<summary>Object has been initialized.</summary>
		Loaded = 2,

		///<summary>Object is interactive, but not all of its data is available.</summary>
		Interactive = 3,

		///<summary>Object has received all of its data.</summary>
		Complete = 4
	}

	/// <summary><seealso cref="BeforeNavigateEventArgs.Cause"/>
	/// Reveals the cause due to which the Web browser performed a navigation.
	/// </summary>
	/// <since>2.0</since>
	public enum BrowserNavigationCause
	{
		/// <summary>
		/// The HTML content was directly uploaded into the Web browser by Omea.
		/// This means that the <see cref="AbstractWebBrowser.ShowHtml"/> function was called,
		/// or <see cref="AbstractWebBrowser.Html"/> property was set.
		/// </summary>
		ShowHtml,

		/// <summary>
		/// The browser is performing a navigation initiated by Omea.
		/// This means that the <see cref="AbstractWebBrowser.Navigate"/> or <see cref="AbstractWebBrowser.NavigateInPlace"/> function was called.
		/// </summary>
		Navigate,

		/// <summary>
		/// Navigation was initiated by user or a script within the page, however, was not explicitly caused by Omea.
		/// </summary>
		FollowLink,

		/// <summary>
		/// Navigation has occured because the browser is going back.
		/// </summary>
		GoBack,

		/// <summary>
		/// Navigation has occured because the browser is going forward.
		/// </summary>
		GoForward,

		/// <summary>
		/// Navigation has occured because the browser is returning to the original page displayed by the <see cref="AbstractWebBrowser.NavigateInPlace"/> or <see cref="AbstractWebBrowser.Navigate"/> function.
		/// </summary>
		ReturnToOriginal
	}

	#endregion
}
