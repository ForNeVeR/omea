// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetBrains Omea Mshtml Browser Component
//
// Implements the Web browser component wrapping with full-scale customization, including view options and security settings & zones.
// Consists of an unmanaged part (C++ ATL, raw hosting, a composite ActiveX control) and a managed part (JScript.NET, Windows Forms control around the unmanaged ActiveX control plus AbstractWebBrowser proxy-inheritor).
// The unmanaged parts server as a wrapper for the custom interfaces only, and should not carry out any meaningful processing. All the events should be delegated to the managed part for processing.
//
// This file belongs to the managed part and implements the MshtmlBrowserControl class.
// This file contains the implementation of the Windows Forms Web browser control and concentrates all the component logic. Other entites, such as unmanaged part and the AbstractWebBrowser proxy-inheritor, should not carry out any meaningful processing.
// The MshtmlBrowserControl class consumes MSHTML/WebBrowser events delegated by the unmanaged part, responds to the IWebBrower interface members invocation and fires the IWebBrowser events.
//
// © JetBrains Inc, 2004—2006
// Written by (H) Serge Baltic
//
import System;
import System.ComponentModel;
import System.Windows.Forms;
import System.Diagnostics;
import JetBrains.Omea.OpenAPI;
import JetBrains.Omea.GUIControls;
import System.Runtime.InteropServices;
import System.IO;
import System.Text;
import JetBrains.Omea.HTML;
import System.Globalization;
import Microsoft.Win32;
import JetBrains.DataStructures;

package JetBrains.Omea.GUIControls.MshtmlBrowser
{
	public
	AxHost.ClsidAttribute("{8a2f0dbe-ec1b-4d1d-8712-4259211b41b4}") DesignTimeVisible( true ) IDispatchImplAttribute( IDispatchImplType.InternalImpl )
	class MshtmlBrowserControl extends AxHost implements ICommandProcessor
	{
		/// The underlying MshtmlSite ActiveX Control that wraps the WebBrowser object.
		protected var _ocx : MshtmlSite.Net.IMshtmlBrowser;

		/// The Security Context in which the content is being displayed.
		protected var _ctx : WebSecurityContext = WebSecurityContext.Internet;

		/// Status Bar writer to which the browser's status bar output is redirected.
	    protected var _statusWriter : IStatusWriter;

		/// The most recently displayed status bar message (used for appending the progress suffix).
	    protected var _sStatusMessage : System.String = "";

	    /// The page download progress that is appended to the status bar text while the page is being downloaded. When the download progress is updated, status bar text is updated. When a new status bar text comes from the browser, the download progress indication is appended to it in case it is less than 100% (download is not completed).
	    protected var _nDownloadProgress : double = 1.0;

	    /// The user may request highlighting of some of the search words when the page starts loading. In this case we have to wait until it loads and apply the highlighting. Here the highlighting data is stored until its time comes.
	    protected var _wordsToHighlight : WordPtr[] = null;

	    /// Stores the list of search hits (derived from the WordPtr passed in for highlighting via either scheme) to navigate them and scroll to the first entry when the page loads.
	    /// The Section field contains a name of the anchor to which to navigate when jumping to the search result, or an ID of the span which is responsible of highlighting the search term (also to jump to it).
	    /// Should be null when displaying content without the search terms and non-null if there are search terms present.
	    /// Note that offsets are not guaranteed to be correct in this list, and also they are not guaranteed to reflect the sorting order of the search hits, it corresponds to the order of appearance.
	    protected var _wordsSearchHits : WordPtr[] = null;

	    /// The current search hit. This variable is used for navigating to a prev/next search hit in the document, and is updated upon the navigation.
	    /// -1 means that either there are no search hits, or there are ones, but we're currently positioned at none (before the first or after the last one).
	    protected var _nCurrentSearchHit : int = -1;

		/// A set of background and foreground colors for highlighting the words in HTML text
	    protected static var _colorsHighlight : BiColor[] = InitColors();
	    public static class BiColor { public var ForeColor; public var BackColor; public function BiColor(sForeColor, sBackColor) { ForeColor = sForeColor; BackColor = sBackColor; } }

	    /// Set to True when we're doing the Back action and kept until the corresponding OnBeforeNavigate comes. Reset if we're directly uploading the content.
	    /// This flag is not set when we're returning to the content uploaded with ShowHtml.
	    protected var _isGoingBack : boolean = false;

	    /// Set to True when we're doing the Forward action and kept until the corresponding OnBeforeNavigate comes. Reset if we're directly uploading the content.
	    protected var _isGoingForward : boolean = false;

	    /// History depth, that is, number of pages on the Back stack plus the current one. Helps in determining whether we can go back (ie if above zero). Reset with the history session, upon direct content feeding or any explicit navigation.
	    protected var _nHistoryDepth : int = 0;

	    /// Maximum history depth reached within this history session (_nHistoryDepth decreases upon Back, this value does not). Helps in determining whether we can go forward (ie if above the history depth). Reset with the history session, upon direct content feeding or any explicit navigation.
	    protected var _nMaxHistoryDepth : int = 0;

	    /// HTML text that was passed into ShowHtml cached in order we have to "go back" to this page. Null if we have started the life cycle with Navigate* not ShowHtml.
	    protected var _sCachedHtml : System.String = null;

	    /// The scrolling position of the document being displayed thru the ShowHtml function stored so that we could restore it when doing Back.
	    protected var _nCachedScrollPos : int = 0;

	    /// The SecurityContext of the document being displayed thru the ShowHtml function stored so that we could restore it when doing Back.
	    protected var _ctxCachedSecurityContext : WebSecurityContext;

	    /// The URI of the page going right after the one that was fed into the browser via ShowHtml.
	    protected var _sForwardUri : System.String = "";

	    /// If non-null, scrolls to the specified position when page is loaded. Used to schedule the scrolling when feeding the page into the browser in case we're doing Back to the manually-fed page.
	    protected var _nOnLoadScrollTo = null;

	    /// The URL currently open in the browser window. If we feed the content directly or the content is not loaded yet, it is an empty string. If we navigate to some resource, this variable holds null indicating that we have to take the up-to-date URL directly from the Web browser. Also it can be overridden by setting the CurrentUrl property explicitly.
	    protected var _uri : System.String = "";

	    /// A source for obtaining the action context whenever needed, originates from the ContextProvider property setter.
	    protected var _actionContextProvider : IContextProvider = null;

	    /// An HTML content that has been schedulled for display when the control is created. Can be assigned by the Html property if invoked before the control has actually been created. Not used afterwards.
	    protected var _sDeferredShowHtml : System.String = "";

	    /// Determines whether the control shows its etched 3D-border or not.
	    protected var _border : boolean = false;

	    /// A hash table that contains a list of string command identifiers that are supported by both this control and MSHTML control and can be just forwarded for processing.
	    protected static var _hashMshtmlCommands : Hashtable = InitHashMshtmlCommands();

	    /// An external object for access from the Web page scripts that can be retrieved thru the window.external property.
	    protected var _externalObject : System.Object = null;

	    /// Denotes the reason due to which the browser has done the most recent content downloading.
	    /// The ShowHtml, Html, Navigate, NavigateInPlace methods set this member to corresponding values
	    ///		until user leaves by clicking a link and navigating to it inplace, which resets the value to FollowLink.
	    /// You can determine the original cause (either .ShowHtml or .Navigate) by checking the _sCachedHtml member,
		///		which is Null in the latter case.
	    protected var _navigationCause : BrowserNavigationCause = BrowserNavigationCause.ShowHtml;

	    /// If the async backup highlighting is currently running, its context is stored in this variable.
	    /// Setting it to Null gently aborts the running highlighting process.
	    protected var _ctxHiliteBackup : HiliteBackupContext = null;

	    /// A timer that performs the delayed execution of the backup highlighting steps.
	    protected var _timerHiliteBackup : Timer = new Timer();

	    /// A list of the protocols that are supported by this control.
	    protected static var _hashSupportedProto : HashSet = InitHashSupportedProto();

	    /// A list of keys that should be suppressed at the current time.
	    /// The keys that were not handled either by the editor mode, or event handler, or Omea keys processing, will be prevented from being processed by the Web browser core if they're present in this list.
	    protected var _hashSuppressedUnhandledKeys : IntHashSet = null;

	    /// The last title passed to the TitleChanged event
	    private var _lastTitle : System.String = null;

	    /// Default ctor, initializes the instance variables.
		public function MshtmlBrowserControl()
		{
			super("8a2f0dbe-ec1b-4d1d-8712-4259211b41b4");
			Trace.WriteLine("[OMEA.MSHTML] MshtmlBrowserControl..ctor()");


			// Initialize the default security context
			_ctx = WebSecurityContext.Restricted;
 		}

 		/// Initializes the colors that are used for highlighting the search keywords in the page text
 		public static function InitColors() : BiColor[]
 		{
			var colorsHighlight = new BiColor[8];
			colorsHighlight[0] = new BiColor("#000000", "#F5C68E");
			colorsHighlight[1] = new BiColor("#000000", "#AAA6DD");
			colorsHighlight[2] = new BiColor("#000000", "#E0A2E1");
			colorsHighlight[3] = new BiColor("#000000", "#94DCEE");
			colorsHighlight[4] = new BiColor("#000000", "#F4FF84");
			colorsHighlight[5] = new BiColor("#000000", "#B4F58E");
			colorsHighlight[6] = new BiColor("#000000", "#F5958E");
			colorsHighlight[7] = new BiColor("#000000", "#8EF5D1");

			return colorsHighlight;
 		}

 		/// Provides access to the storage of the colors used for highlighting
 		public static function get HiliteColors() : BiColor[]
 		{	return _colorsHighlight;	}
 		public static function set HiliteColors( value : BiColor[] )
 		{	_colorsHighlight = value;	}

 		/// Resets the navigation history, which occurs each time the browser is navigated not due to the user request but because of an external action, like Navigate or NavigateInPlace call, or direct page feeding via ShowHtml.
 		public function ResetHistory() : void
 		{
			// Reset the History
			_sForwardUri = "";
			_nHistoryDepth = 0;
			_nMaxHistoryDepth = 0;
			_sCachedHtml = null;
			_nCachedScrollPos = 0;
			_isGoingBack = false;
			_isGoingForward = false;
			_nOnLoadScrollTo = null;
			_ctxCachedSecurityContext = _ctx;
			_uri = null;	// Use one from Web browser
			_wordsSearchHits = null;	// Search hits collection is reset when displaying a new resource
			_nCurrentSearchHit = -1;	// Positioned beyond the search hits
 		}

		public function ShowHtml( htmlText : String ) : void
		{
			ShowHtml( htmlText, null, null );
		}

		public function ShowHtml( htmlText : String, ctx : WebSecurityContext ) : void
		{
			ShowHtml( htmlText, ctx, null );
		}

		public function ShowHtml( htmlText : String, ctx : WebSecurityContext, wordsToHighlight : WordPtr[] ) : void
		{
			// Optional debug dumping of the HTML content being viewed (normally, commented-out)
			/*
@if(@DEBUG)
			var	sw = new StreamWriter("t:\\" + DateTime.Now.ToString("s").Replace(':', '-') + ".html");
			sw.Write(htmlText);
			sw.Close();
@end
			*/

			// This variable is True if we cannot upload the given HTML content right now; if it's False, then the Web browser is not ready for displaying the content due to some cause (eg not created yet or in an invalid state) and it should be deferred
			var	bCanShowHtmlNow = true;

			// Check the parameters, substitute the defaults
			if(htmlText == null)
				throw new ArgumentNullException("htmlText");
			if(ctx == null)
				ctx = WebSecurityContext.Restricted;

			//Trace.WriteLine(String.Format("ShowHtml has been invoked for content-length={0}, words-to-highlight={1}.", htmlText.Length, (wordsToHighlight != null ? wordsToHighlight.Length : "<Null>")), "[OMEA.MSHTML]");

			// Is the browser available?
			if((_ocx == null) || (_ocx.Browser == null))
			{
				bCanShowHtmlNow = false;	// Defer!

				//throw new InvalidActiveXStateException( "Navigate", ActiveXInvokeKind.MethodInvoke );
				Trace.WriteLine("The control has not been created yet. Your navigation request could not be completed now. It was deferred and will be executed when the browser control creates.", "[OMEA.MSHTML]");
@if(@DEBUG)
//System.Windows.Forms.MessageBox.Show("[OMEA.MSHTML] The control has not been created yet. Your navigation request could not be completed now.");
@end
			}
			else	// The browser is available, but it can have an invalid state …
			{
				// Try to stop the browser. First, this helps in case some download is still in progress — it may interfere with the upload. Second, it checks whether there's still a live COM object on the other end
				try
				{
					_ocx.Browser.Stop();

					if(_ocx.HtmlDocument == null)	// Try to recreate back the MSHTML control if the browser holds some other document class now
					{
						bCanShowHtmlNow = false;
						Trace.WriteLine("There is no HTML document in the browser control. Probably it currently hosts some other document class. Trying to get the HTML renderer back by navigating to “about:blank”.");
						_ocx.Navigate("about:blank");	// Put the HTML document class back, when it loads, the deferred content will be uploaded
					}
				}
				catch(ex : InvalidComObjectException)
				{
					// This is quite bad. The object has fallen off somehow …
					bCanShowHtmlNow = false;	// Will try to resurrect the control

@if(@DEBUG)
					var sDiagnosticMessage = String.Format("The MSHTML Browser Site ActiveX control has deceased. Its IsComObject is {0}, Window handle is {1}, HandleCreated is {2}.\n{3}", Marshal.IsComObject(_ocx), Handle, IsHandleCreated, ex.Message);
					Core.ReportException(new Exception(sDiagnosticMessage, ex), ExceptionReportFlags.AttachLog);
@end

					// Some resurrection attempts …
					Visible = false;
					CreateControl();
					Visible = true;
				}
				catch(ex : Exception)
				{
					// Some other exception has occured, but this most probably relates to the Web browser control drifting off our cradle
					bCanShowHtmlNow = false;	// Will try to resurrect the control

@if(@DEBUG)
					var sDiagnosticMessage = String.Format("The MSHTML ActiveX control has drifted off its MshtmlSite cradle. Our IsComObject is {0}, Window handle is {1}, HandleCreated is {2}.\n{3}", Marshal.IsComObject(_ocx), Handle, IsHandleCreated, ex.Message);
					Core.ReportException(new Exception(sDiagnosticMessage, ex), ExceptionReportFlags.AttachLog);
@end

					// Some resurrection attempts …
					_ocx.ResurrectWebBrowser();	// Tries to create a new control for us
				}
			}

			// Defer the HTML content if it cannot be displayed now
			if(!bCanShowHtmlNow)
			{
				Trace.WriteLine("The HTML content could not be shown right now and is being deferred on until the browser is ready for action.", "[OMEA.MSHTML]");

				// Store the call parameters
				_sDeferredShowHtml = htmlText;
				_ctx = ctx;
				_wordsToHighlight = wordsToHighlight;
				return;
			}

			try
			{
				////////////
				// Preformat the content being fed
				var	sHtmlTextOriginal : System.String = htmlText;

				// Check if we have to do some kinda on-the-fly highlighting
				var wordsSearchHits : WordPtr[] = null;	// A temporary storage for the list of the search hits, as provided by the main hilighing scheme; store here to survive the ResetHistory call below
				if( wordsToHighlight != null )
				{
					// Check the passed-in WordPtrs for consistency
					WordPtr.AssertValid(wordsToHighlight, true);

					// Create a copy of the array to avoid spoiling it
					var	wordsToHighlightCopy : WordPtr[] = new WordPtr[wordsToHighlight.length];
					System.Array.Copy(wordsToHighlight, wordsToHighlightCopy, int(wordsToHighlight.length));

					var	htmlHighlighted = HiliteMain( htmlText, wordsToHighlightCopy );
					if(htmlHighlighted != null)	// On-the-fly hilite succeeded
					{
						htmlText = htmlHighlighted;
						wordsSearchHits = wordsToHighlightCopy;
					}
					else	// Failed, back to the secondary scheme
						_wordsToHighlight = wordsToHighlightCopy;	// Store here for the backup hilite scheme to take action
					Trace.WriteLine("[OMEA.MSHTML] HTML On-the-fly Highlighting has " + (htmlHighlighted != null ? "succeeded." : "failed."));
				}

				/////////////////////////////
				// Fire the event and query whether to show the content
				var args : BeforeShowHtmlEventArgs = new BeforeShowHtmlEventArgs(sHtmlTextOriginal, htmlText, ctx, "");
				fire_BeforeShowHtml(args);
				htmlText = args.FormattedHtml;
				ctx = args.SecurityContext;
				var	sUriDisplayName : System.String = args.UriDisplayName;

				//////////////////////////////
				// Perform the upload

				SecurityContext = ctx;	// Apply the security context

				ResetHistory();	// Reset
				_wordsSearchHits = wordsSearchHits;	// Store for scrolling to the first search hit and navigation back-forth
				_uri = sUriDisplayName;	// The content was fed explicitly; either an emptystring or something the user has set

				_ocx.SettingsChanged();	// Force IE re-read the options

				_navigationCause = BrowserNavigationCause.ShowHtml;
				_sCachedHtml = htmlText;	// Cache the submitted HTML text to restore it when going Back
				_ocx.ShowHtmlText( htmlText );	// Upload the page's HTML source into the browser
			}
			catch(ex : Exception)
			{
				Trace.WriteLine("[OMEA.MSHTML] Cannot upload the supplied HTML content into the browser: " + ex.Message);

				// Display the error in the UI
				try
				{
					var	sw = new StringWriter();
					sw.WriteLine("<html><body style=\"font-family: Tahoma; font-size: 8pt; text-align: center;\">");
					sw.WriteLine("<p style=\"color: red;\">The document could not be displayed.</p>");
					sw.WriteLine("<p>{0}</p>", HttpUtility.HtmlEncode(ex.Message));
					sw.WriteLine("</body></html>");
					_ocx.ShowHtmlText(sw.ToString());
				}
				catch(Exception)
				{	// Could not display the error using HTML; try a good old messagebox
					//MessageBox.Show(this, "The document could not be displayed.\n" + HttpUtility.HtmlEncode(ex.Message), "Omea", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}

				// Report the exception
				ReportException(ex, false);	// Rethrow if the reporting service is unavailable
			}
		}

		public function HighlightWords(words : WordPtr[], startOffset : int) : void
		{
			Trace.WriteLine("Warning: the obsolete MshtmlBrowserControl.HighlightWords function is called.", "[OMEA.MSHTML]");
			if( _wordsToHighlight != null )	// There are some words schedulled for highlighting already
				Trace.WriteLine( "Warning: schedulling the backup highlighting when the previous one has not been applied yet.", "[OMEA.MSHTML]");

			// Check if the new words are valid
			WordPtr.AssertValid(words, true);

			// Backup the word list for highlighting. This function is called at any moment whilst the highlighting must be applied not before the content to be highlighted gets loaded.
			_wordsToHighlight = words;
			//Trace.WriteLine(String.Format("{0} words have been queued for highlighting.", _wordsToHighlight.Length), "[OMEA.MSHTML]");
		}

		public function get ShowImages() : boolean
		{ return _ctx.ShowPictures; }
		public function set ShowImages(value : boolean)
		{ _ctx.ShowPictures = value; }

		public function get CurrentUrl() : String
		{
			try
			{
				return (_ocx != null) ? (( _uri != null ) ? ( _uri ) : ( _ocx.Browser.LocationURL )) : (null);
			}
			catch(ex : Exception)
			{
				Trace.WriteLine("[OMEA.MSHTML] Cannot retrieve the current URI: " + ex.Message);
				return "";
			}
		}

		public function set CurrentUrl(uri : String)
		{
			_uri = uri;	// Override the URI being displayed as current
		}

		public function get SelectedHtml() : String
		{
			return TextSelection[1];
		}

		public function get SelectedText() : String
		{
			return TextSelection[0];
		}

		/// Retrieves the text selection, in both plain-text and HTML form.
		public function get TextSelection() : System.String[]
		{
			var arSafeRet : System.String[] = new System.String[2];
			arSafeRet[0] = arSafeRet[1] = "";
			try
			{
				var	arStrings = GetSelectionRecursive(_ocx.HtmlDocument);
				arSafeRet = arStrings != null ? arStrings : arSafeRet;
			}
			catch(ex : Exception)
			{
				Trace.WriteLine("Cannot retrieve the text selection. " + ex.Message, "[OMEA.MSHTML]");
				// Two empty strings will be returned
			}
			return arSafeRet;
		}

		/// Gets the selection from the given HTML document. Recurses into the child frames.
		/// Returns Null if there is no selection, or an array of two strings otherwise, the first string is plain text, the second is HTML.
		public function GetSelectionRecursive(oDoc) : System.String[]
		{
			if(oDoc == null)
				return null;
			try
			{
				var	oSelRange = oDoc.selection.createRange();
				var	oSelText = oSelRange.text;
				var	oSelHtml = oSelRange.htmlText;
				var	arRetVal : System.String[] = null;

				// Check for the selection in this document
				if((oSelText != null) && (oSelHtml != null) && (oSelText.Length != 0) && (oSelHtml.Length != 0))	// Yes, there is some selection in this document
				{
					arRetVal = new System.String[2];
					arRetVal[0] = oSelText;
					arRetVal[1] = oSelHtml;
					return arRetVal;
				}

				// No selection in this document, check for the frames
				var nFrames : int = oDoc.frames.length;
				for(var a : int = 0; a < nFrames; a++)
				{
					var oFrame = oDoc.frames.item(a);
					if((arRetVal = GetSelectionRecursive(oFrame.document)) != null)	// Try getting a selection from this frame, return as a result if present
						return arRetVal;
				}
			}
			catch(ex : Exception)
			{	// An exception here most probably means that the document is not an HTML document at all
				// Return an empty string (as if there were no selection) in such a case
				return null;
			}

			return null;	// No selection was found
		}

		public function Navigate( uri : String ) : void
		{
			// Check if the resource should be opened in a new window; if so, do not start any navigation sequence at all
			if(!GetNavigateInplaceSetting())
				NavigateInExternalWindow(uri);

			// OKAY, open in this window — it's now just like NavigateInPlace
			NavigateInPlace(uri);
		}

		public function NavigateInPlace( uri : String ) : void
		{
			if( _ocx == null )
			{
				//throw new InvalidActiveXStateException( "Navigate", ActiveXInvokeKind.MethodInvoke );
				Trace.WriteLine("[OMEA.MSHTML] The control has not been created yet. Your navigation request has been forcefully rejected.")
@if(@DEBUG)
System.Windows.Forms.MessageBox.Show("[OMEA.MSHTML] The control has not been created yet. Your navigation request has been forcefully rejected.");
@end
				return;
			}

			// Do the navigation
			// It also checks whether there's still a live COM object on the other end
			// If there's not, we attempt to re-create the control and re-do the navigation attempt
			try
			{
				NavigateInPlaceImpl(uri);
			}
			catch(ex : InvalidComObjectException)
			{	// This is quite bad. The object has fallen off somehow …
@if(@DEBUG)
				Core.ReportException(new Exception(String.Format("The MSHTML Browser Site ActiveX control has deceased. Its IsComObject is {0}, Window handle is {1}, HandleCreated is {2}.", Marshal.IsComObject(_ocx), Handle, IsHandleCreated), ex), ExceptionReportFlags.AttachLog);
@end
				// Some resurrection attempts …
				Visible = false;
				CreateControl();
				Visible = true;

				// Retry!
				NavigateInPlaceImpl(uri); // (this time the exception will go out, if any)
			}
			catch(ex : Exception)
			{	// Some other exception has occured, but this most probably relates to the Web browser control drifting off our cradle
@if(@DEBUG)
				Core.ReportException(new Exception(String.Format("The MSHTML ActiveX control has drifted off its MshtmlSite cradle. Our IsComObject is {0}, Window handle is {1}, HandleCreated is {2}.", Marshal.IsComObject(_ocx), Handle, IsHandleCreated), ex), ExceptionReportFlags.AttachLog);
@end

				// Some resurrection attempts …
				_ocx.ResurrectWebBrowser();	// Tries to create a new control for us

				// Retry!
				NavigateInPlaceImpl(uri); // (this time the exception will go out, if any)
			}
		}

		/// Called from the NavigateInPlace function, does the main part of the navigation, while the outer function "just" checks that the control is still alive
		protected function NavigateInPlaceImpl(uri : System.String) : void
		{
			ResetHistory();	// Reset
			_nHistoryDepth = -1;	// This will be changed to a valid 0 value in the BeforeNavigate event handler (OnBeforeNavigate func)
			_navigationCause = BrowserNavigationCause.Navigate;	// Remember the cause
			_ocx.SettingsChanged();	// Force IE re-read the options
			_ocx.Navigate(uri);
		}

		/// Determines whether (due to Omea settings) navigation should occur inplace, or not.
		public static function GetNavigateInplaceSetting() : boolean
		{
			if(ICore.Instance != null)
			{
				try
				{
					return !Core.SettingStore.ReadBool("Resources", "LinksInNewWindow", false);
				}
				catch(ex : Exception)
				{
					ReportException(ex, true);
				}
			}
			return true;
		}

		/// Opens an external browser window in order to navigate to the resource specified.
		/// The system's default browser is used for that.
		public static function NavigateInExternalWindow(uri : System.String) : boolean
		{
			NavigateInExternalWindow2(uri, true);
		}

		/// Opens an external browser window in order to navigate to the resource specified.
		/// The system's default browser is used for that.
		public static function NavigateInExternalWindow2(uri : System.String, dde : boolean) : boolean
		{
			if(ICore.Instance != null)
				dde ? Core.UIManager.OpenInNewBrowserWindow(uri) : Core.UIManager.OpenInNewBrowserWindow(uri, false);	// If DDE is allowed, we let the system use value from the settings; otherwise, we explicitly prohibit the use of DDE
			else
			{
				try
				{
					var	psi : ProcessStartInfo = new ProcessStartInfo();
					psi.FileName = uri;
					psi.Verb = "open";
					psi.UseShellExecute = true;
					Process.Start(psi);
				}
				catch(ex : Exception)
				{
					Trace.WriteLine(String.Format("Core was Null, tried to open the link in a new window using ShellExecute, but it has failed. {0}", ex.Message), "[OMEA.MSHTML]");
				}
			}

			return true;
		}

		public function get SecurityContext() : WebSecurityContext
		{	return _ctx;	}
		public function set SecurityContext( value : WebSecurityContext )
		{
			_ctx = value;	// Update the storage

			if( _ocx != null )
				_ocx.SettingsChanged();	// Force browser re-request the updated settings
		}

		/*protected function void CreateSink()
		{
			try
			{
				eventMulticaster = new MshtmlBrowserEventMulticaster( this );
				cookie = new ConnectionPointCookie( _ocx, eventMulticaster, typeof( MshtmlSite._IMshtmlBrowserEvents ) );
			}
			catch( Exception )
			{
			}
		}

		protected override void DetachSink()
		{
			try
			{
				cookie.Disconnect();
			}
			catch( Exception )
			{
			}
		}*/

		/// Called by the base when the control instance is to be created.
		/// Base implementation uses standard CoCreateInstance thru the Registry, we would like to live without it.
		protected function CreateInstanceCore(clsid : Guid) : System.Object
		{
			var sDllFilename = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "MshtmlSite.dll");

			return JetBrains.Interop.WinApi.Kernel32Dll.Helpers.CoCreateInstanceExplicit(sDllFilename, new Guid("{8a2f0dbe-ec1b-4d1d-8712-4259211b41b4}"));
		}

		/// Invokes when the underlying object gets created. This is the first chance to get the ActiveX pointer.
		protected function AttachInterfaces() : void
		{
			Trace.WriteLine("[OMEA.MSHTML] The MSHTML wrapper is attaching its interfaces to the underlying ActiveX control.");
			try
			{
				// Retrieve a pointer to the encapsulated OLE control
				_ocx = GetOcx();

				// Attach this object as a callback to be notified of the web browser events
				_ocx.ParentCallback = this;

				// Get the status bar writer
				_statusWriter = Core.UIManager.GetStatusWriter( GetType(), StatusPane.UI );
			}
			catch( ex : Exception )
			{
				Trace.WriteLine("[OMEA.MSHTML] An exception has occured when trying to attach the managed wrapper to the underlying MSHTML control: " + ex.ToString() );
				if(ICore.Instance != null)
					Core.ReportException(ex, ExceptionReportFlags.AttachLog);
			}
		}

		public function CanExecuteCommand( command : String ) : boolean
		{
			try
			{
				// Special processing for some of the commands
        		switch( command )
        		{
        			case DisplayPaneCommands.Back:       return ( _nHistoryDepth >= 1 );
					case DisplayPaneCommands.Forward:    return ( _nHistoryDepth < _nMaxHistoryDepth );
					case DisplayPaneCommands.FindInPage: return true;
					case DisplayPaneCommands.Cut:        return _ocx.Browser.QueryStatusWB(OleCmdId.Cut) & OleCmdStatus.Enabled;
					case DisplayPaneCommands.Copy:       return _ocx.Browser.QueryStatusWB(OleCmdId.Copy) & OleCmdStatus.Enabled;
					case DisplayPaneCommands.Paste:      return _ocx.Browser.QueryStatusWB(OleCmdId.Paste) & OleCmdStatus.Enabled;
					case DisplayPaneCommands.Print:      return true;
					case DisplayPaneCommands.PageDown:   return CanPageDown();
					case "Refresh":                      return _uri == null;	// Navigated, not uploaded page
					case "ForceRefresh":                 return _uri == null;	// Navigated, not uploaded page
					case "ViewSource":                   return true;
					case DisplayPaneCommands.PrevSearchResult:	return CanGotoNextSearchHit(false);
					case DisplayPaneCommands.NextSearchResult:	return CanGotoNextSearchHit(true);
        		}

				// Check for direct forwarding to an IE command
				if(_hashMshtmlCommands.ContainsKey(command))
					return _ocx.HtmlDocument.queryCommandEnabled(command);

				// Unknown/unsupported command
				return false;
			}
			catch(ex : Exception)
			{
				Trace.WriteLine(String.Format("[OMEA.MSHTML] Cannot query whether the \"{0}\" command execution is enabled. {1}", command, ex.Message));
				return false;
			}
		}

		public function ExecuteCommand(command : String) : void
		{
			ExecuteCommand(command, null);
		}

		public function ExecuteCommand(command : String, param : Object) : boolean
        {
			try	// Outer try/catch block: traps and reports the global failures
			{
				try	// Inner try/catch block: traps the COM exceptions and checks if they should be suppressed
				{
					// Special processing for some of the commands
        			switch( command )
        			{
        				case DisplayPaneCommands.Back:       return GoBack();
						case DisplayPaneCommands.Forward:    return GoForward();
						case DisplayPaneCommands.FindInPage: _ocx.ExecDocumentCommand( "Find", true ); return true;
						case DisplayPaneCommands.Cut:        _ocx.Browser.ExecWB(OleCmdId.Cut, OleCmdExecOpt.DontPromptUser); return true;
						case DisplayPaneCommands.Copy:       _ocx.Browser.ExecWB(OleCmdId.Copy, OleCmdExecOpt.DontPromptUser); return true;
						case DisplayPaneCommands.Paste:      _ocx.Browser.ExecWB(OleCmdId.Paste, OleCmdExecOpt.DontPromptUser); return true;
						case DisplayPaneCommands.Print:      _ocx.Browser.ExecWB(OleCmdId.Print, OleCmdExecOpt.PromptUser ); return true;
						case DisplayPaneCommands.PageDown:   return PageDown();
						case "Refresh":                      _ocx.Browser.Refresh2( RefreshConstants.Normal );  return true;
						case "ForceRefresh":                 _ocx.Browser.Refresh2( RefreshConstants.Completely );  return true;
						case "ViewSource":                   _ocx.ExecDocumentCommand( "ViewSource", false );  return true;
						case DisplayPaneCommands.PrevSearchResult:	GotoNextSearchHit(false, true);	return true;
						case DisplayPaneCommands.NextSearchResult:	GotoNextSearchHit(true, true);	return true;
        			}

					// Check for direct forwarding to an IE command
					if(_hashMshtmlCommands.ContainsKey(command))
						return _ocx.HtmlDocument.execCommand(command, true, param);
 				}
	 			catch(ex : COMException)
	 			{
	 				if((ex.ErrorCode == 0x80040100) || (ex.ErrorCode == int(0x80040100)))	// "Trying to revoke a drop target that has not been registered", occurs from time to time without any reason
						Trace.WriteLine(String.Format("Cannot execute the \"{0}\" command. {1} Exception has been suppressed.", command, ex.Message), "[OMEA.MSHTML]");
					else
						throw ex;	// Rethrow and let it be handled the usual way
	 			}
	 		}
			catch(ex : Exception)
			{
				Trace.WriteLine(String.Format("Cannot execute the \"{0}\" command. {1}", command, ex.Message), "[OMEA.MSHTML]");
				ReportException(new Exception(String.Format("Cannot execute the \"{0}\" command.", command), ex), false);
			}
		}

		public function get ContextProvider() : IContextProvider
		{	return _actionContextProvider;	}
		public function set ContextProvider( value : IContextProvider )
		{	_actionContextProvider = value;	}

		public function get Html() : System.String
		{
			if( _ocx != null )	// Control exists
				return _ocx.GetHtmlText();
			else
				return _sDeferredShowHtml;	// Content prepared for display
		}
		public function set Html( value : System.String )
		{
			if( _ocx != null )	// The control has already been created
				ShowHtml( value, _ctx, null );	// Just invoke the runtime routine, using the current security context and without word-highlighting
			else	// The control has not been created yet, should defer the content display until it's instantiated
				_sDeferredShowHtml = value;
		}

		/// The backup word-highlighting routine which should not be used for highlighting the internal Omea content. The fed content should be highlighted on-the-fly when processing the content being received. In case this content fails to be highlighted propertly (eg the offsets do not correspond to the actual ones due to being generated by an old version), highligh-list is saved and highlighting falls back to using this utility. Also, when working with the downloaded content we have no other chance but to apply highlighting after the content is loaded. The current implementation won't use offsets in this case.
		/// The disadvantages of not using the offsets are: excessive words may get highlighted (individual entries when doing phrasal search) and pieces of the phrasal search may get different highlighting colors. At least, the real hits won't be left unhighlighted.
		protected function HiliteBackup( words : WordPtr[] ) : void
		{
			Trace.WriteLine(String.Format("Warning: applying the backup highlighting routine to {0} words.", words.Length), "[OMEA.MSHTML]");

			try
			{
				// Run the backup hilite, currently still in the sync manner
				_ctxHiliteBackup = new HiliteBackupContext(words);
				if(HiliteBackup_Start(_ctxHiliteBackup))
				{
					_timerHiliteBackup.Interval = 10;	// Tick each 10 ms — actually, give no delay to execution
					_timerHiliteBackup.add_Tick(HiliteBackup_Tick);
					_timerHiliteBackup.Start();
					_statusWriter.ShowStatus( "Highlighting search hits in the document…" );
				}
				else
				{
					_ctxHiliteBackup = null;	// Something has failed, no hilite
					Trace.WriteLine("Failed to initiate the async backup highlighting scheme.", "[OMEA.MSHTML]");
				}
			}
			catch( ex : Exception )
			{
				Trace.WriteLine( "Cannot start applying backup highlighting to the HTML text. " + ex.Message, "[OMEA.MSHTML]" );
				ReportException(new Exception("Cannot start applying backup highlighting to the HTML text.", ex), true);
			}
		}

		protected function HiliteBackup_Tick(sender : Object, args : EventArgs) : void
		{
			if(_timerHiliteBackup != null)
				_timerHiliteBackup.Stop();	// Will be re-started to avoid collecting the events
			if(_ctxHiliteBackup == null)
				return;	// Has been aborted

			var dwStart = DateTime.Now.Ticks / 10000;
			var dwLimit = 222; // Allow running for this much milliseconds continuously

			try
			{
				var nIterations: int;
				for(nIterations = 0; (DateTime.Now.Ticks / 10000) - dwStart < dwLimit; nIterations++) // Work for some limited time
				{
					if(!HiliteBackup_Step(_ctxHiliteBackup)) // Invoke the individual highlighting step
					{ // Highlighting Completed!

						// Reset the status bar dials
						_statusWriter.ClearStatus();

						// Retrieve the values
						_wordsSearchHits = _ctxHiliteBackup.ActualSearchHits;
						_nCurrentSearchHit = -1;

						// Deinitialize the hilite search
						_ctxHiliteBackup = null;

						// Jump to the next search hit
						GotoNextSearchHit( true, false );

						// Done!
						Trace.WriteLine( String.Format( "The MshtmlBrowserControl has completed the async backup highlighting with {0} hits total.", _wordsSearchHits.Length ), "[OMEA.MSHTML]" );
						return;
					}
				}
				Trace.WriteLine( String.Format( "The MshtmlBrowserControl async backup highlighting has done {0} highlightings on this step.", nIterations ), "[OMEA.MSHTML]" );
			}
			catch(ex : Exception)
			{
				Trace.WriteLine("Cannot apply backup highlighting to the HTML text. An async step has failed. " + ex.Message, "[OMEA.MSHTML]");
				ReportException(new Exception("Cannot apply backup highlighting to the HTML text. An async step has failed.", ex), true);
				_ctxHiliteBackup = null;	// Abort
				return;
			}

			// Requeue the rest of execution
			Application.DoEvents(); // Without this, the painting events won't occur
			if(_timerHiliteBackup != null)
				_timerHiliteBackup.Start();
		}

		protected function HiliteBackup_Start(ctx : HiliteBackupContext) : boolean
		{
			Trace.WriteLine(String.Format("Warning: applying the backup highlighting routine to {0} words.", ctx.Words.Length), "[OMEA.MSHTML]");

			// Make a hash of the words to highlight
			ctx.HashWordForms = new Hashtable( ctx.Words.Length );
			for(var nWord = 0; nWord < ctx.Words.Length; nWord++)
				ctx.HashWordForms[ctx.Words[nWord].Text] = ctx.Words[nWord];
			ctx.Enum = ctx.HashWordForms.Keys.GetEnumerator();	// Enumerator of the words
			Trace.WriteLine(String.Format("{0} unique forms were picked out of {1} original word-ptrs.", ctx.HashWordForms.Count, ctx.Words.Length), "[OMEA.MSHTML]");

			ctx.ActualSearchHitsCache = new ArrayList(ctx.HashWordForms.Count);	// A list of WordPtrToHtmlTxtRange objects that store search hits and their real offsets in the text as HTML ranges that serve as the sorting keys. Note that this may exceed words.Length

			ctx.HashSources = new Hashtable();	// Holds the original search string entries that we have met, mapped to the indexes of highlighting colors. Provides for highlighing tokens produced from the same search entry with the same color

			ctx.Range = null;

			return true;	// Start highlighting
		}

		protected function HiliteBackup_Step(ctx : HiliteBackupContext) : boolean
		{
			if(ctx.Range == null) // Should we pick a new word form for searching it?
			{
				if(!ctx.Enum.MoveNext())
				{ // Completed!!

					// Sort the search hits in order of appearance
					ctx.ActualSearchHitsCache.Sort();

					// Store for later use (scroll-to-view, navigation)
					ctx.ActualSearchHits = new WordPtr[ctx.ActualSearchHitsCache.Count];
					for(var a : int = 0; a < ctx.ActualSearchHitsCache.Count; a++)
						ctx.ActualSearchHits[a] = WordPtr(WordPtrToHtmlTxtRange(ctx.ActualSearchHitsCache[a])._word);

					return false; // Finish it
				}

				// Seed the searching range
				ctx.Range = _ocx.HtmlDocument.body.createTextRange();
				ctx.Range.collapse(true);	// Start at the beginning of the body
			}
			var sWordForm : System.String = System.String(ctx.Enum.Current);
			var wordSearchHit : WordPtr = WordPtr(ctx.HashWordForms[sWordForm]);

			// Choose a color for highlighting the hits of this text
			var color : BiColor = ctx.PickNextColor( wordSearchHit.Original );

			// Look for the next entry, starting from the very place we left it the prev time
			try
			{
				if(!(ctx.Range.findText(sWordForm, Int32.MaxValue, TextRangeFindFlags.WholeWordsOnly)))
				{	// False return value means we can find no more entries for this word form; mark to switch to the next word form, or terminate if there are no more left
					ctx.Range = null;	// This range is thru, take the next word on the next step
					return true;	// Make out one more step
				}
			}
			catch(ex : System.Runtime.InteropServices.COMException)
			{
				// An exception means there were problems iterating the content, for example the "Unpositioned markup pointer for this operation." exception. Just ignore and start searching for the next word-ptr
				Trace.WriteLine(String.Format("Hilite Backup: there was a problem finding the next search hit. {0}", ex.Message), "[OMEA.MSHTML]");
				ctx.Range = null;	// This range is thru, take the next word on the next step
				return true;	// Make out one more step
			}

			// Apply the search result
			ctx.Range.execCommand( "BackColor", false, color.BackColor );	// Highlight it
			ctx.Range.execCommand( "ForeColor", false, color.ForeColor );	// Highlight it

			// Set the marker
			wordSearchHit.Section = "SearchHit-" + Guid.NewGuid().ToString();
			ctx.Range.pasteHTML(String.Format("<span id=\"{0}\" title=\"Search result for {1}\">{2}</span>", wordSearchHit.Section, wordSearchHit.Original, ctx.Range.htmlText));

			// Store this hit
			ctx.ActualSearchHitsCache.Add(new WordPtrToHtmlTxtRange(wordSearchHit, ctx.Range.duplicate()));

			ctx.Range.collapse( false );	// Go to the end of this range

			return true;	// Try looking for the next entry of the same word form
		}

		/// Holds the context for executing the backup highlighting scheme in an asynchronous manner.
		protected class HiliteBackupContext
		{
			/// <summary>
			/// Search hits to be highlighted.
			/// </summary>
			public var Words : WordPtr[] = null;

			/// <summary>
			/// Enumerates the word forms as they are expected to appear in the text.
			/// This is the outer loop's variable.
			/// </summary>
			public var Enum : IEnumerator = null;

			/// <summary>
			/// Backup sceme stores its hits in here.
			/// A list of WordPtr objects that represent the search hits in the document; they may not correspond to the words list passed in.
			/// This new list is sorted and stored for search result navigation, etc.
			/// </summary>
			public var ActualSearchHitsCache : ArrayList = null;

			/// <summary>
			/// The final version of the search hits list, as they were encountered in the document.
			/// </summary>
			public var ActualSearchHits : WordPtr[] = null;

			/// <summary>
			/// Maps the original word forms to the corresponding color for highlighting.
			/// Holds the original search string entries that we have met, mapped to the indexes of highlighting colors.
			/// Provides for highlighing tokens produced from the same search entry with the same color.
			/// </summary>
			public var HashSources : Hashtable = null;

			/// <summary>
			/// Stores all the target word forms in the way they should be encountered in the text, for the backup hilite to search for them, one by one.
			/// Maps the word form (key) to the whole <see cref="WordPtr"/> that references it (value).
			/// </summary>
			public var HashWordForms : Hashtable = null;

			/// <summary>
			/// Current position in the backup highlighting scheme.
			/// If Null, then the next word should be taken from the word forms hash.
			/// </summary>
			public var Range : Object = null;

			/// <summary>
			/// Initializes the object to an indeterminate state.
			/// </summary>
			public function HiliteBackupContext(words : WordPtr[])
			{
				Words = words;
				HashSources = new Hashtable();
			}

			/// <summary>
			/// Chooses the color that corresponds to the given source form string, or assigns a new one if not specified yet.
			/// </summary>
			public function PickNextColor(source : System.String) : BiColor
			{
				// Choose a color
				var color : BiColor;
				if(HashSources.ContainsKey(source)) // Source already known and has a color assigned
					color = BiColor(HashSources[source]);
				else // Assign a new color
				{
					color = _colorsHighlight[HashSources.Count % _colorsHighlight.Length];
					HashSources[source] = color;
				}

				return color;
			}
		}

		/// The main highlighting routine. Tries to apply highlighitng to the content that is being fed into the browser on-the-fly. May fail if the offsets are all wrong (eg generated with an old version) and return null, in this case, the caller should fallback to the backup routine.
		public static function HiliteMain( content : System.String, words : WordPtr[] ) : System.String
		{
			Trace.WriteLine(String.Format("Applyting the main hiliting scheme to {0} words in the content of length {1}.", words.Length, content.Length), "[OMEA.MSHTML]");

			try
			{
				var reader : HtmlEntityReader = new HtmlEntityReader( new StringReader(content) );	// Input reader capable of substituting entites
				var	writer : StringBuilder = new StringBuilder();	// Main output writer

				var	writerToken : StringBuilder = new StringBuilder();	// Cache for the search match as it occurs in the HTML stream (with entities unsubstituted), to pass it thru as is, while being matched to the substituted version

				var	hashSources = new Hashtable();	// Holds the original search string entries that we have met, mapped to the indexes of highlighting colors. Provides for highlighing tokens produced from the same search entry with the same color
				var	nCurrentColorIndex : int = 0;	// Index of the highlighting color that is to be assigned to the next new source entry. Wraps over the total number of highlight colors
				var	wordsSearchHits : ArrayList = new ArrayList();	// List of the search hits along with there assigned IDs. Should be assigned to _wordsSearchHits on successful completion
				var	wordSearchHit : WordPtr;	// An individual entry for the above array

@if(@DEBUG)	// Length check
				var nIntroducedLength : int = 0;	// Total length of the text we insert into the content to make it highlighted, must be equal to lengths delta
@end

				// Pass through the content up to the next word, apply highlighting to that word, and dump the highlighted chunk
				var	nCharsToPassthru : int;
				var	a : int;
				var	word : WordPtr;
				for(var nWord = 0; nWord < words.Length; nWord++)
				{
				    var wordMixedCase : WordPtr = words[nWord];

					// Make the word lowercase
					word = wordMixedCase;
					word.Text = word.Text.ToLower(CultureInfo.CurrentCulture);

					// Pass thru the chars after the prev reading point and up to the next token to be highlighted
					nCharsToPassthru = word.StartOffset - reader.Position;
					nCharsToPassthru = nCharsToPassthru > 0 ? nCharsToPassthru : 0;	// It may be less than zero if the offset for the next match has been shifted back, and the gap is too small, so that the actual previous match overlaps the supposed next match
					Debug.Assert(nCharsToPassthru >= 0);
					for( a= 0; a < nCharsToPassthru; a++ )
						writer.Append( reader.ReadChar( false ) );	// Simply read the next char
					Debug.Assert(word.StartOffset <= reader.Position);

					// Choose a color
					var color : BiColor;
					if( hashSources.ContainsKey( word.Original ) )	// Source already known and has a color assigned
						color = hashSources[ word.Original ];
					else	// Assign a new color
					{
						color = _colorsHighlight[ nCurrentColorIndex++ ];
						nCurrentColorIndex = nCurrentColorIndex < _colorsHighlight.Length ? nCurrentColorIndex : 0;	// Wrap around
						hashSources[ word.Original ] = color;
					}

					////////////////////////////
					// Do the hilite

					// Now ensure that we hilite the proper thing. Check that the characters in the stream are the same as in the word we're expected to highlight on this step
					// First, we admit that there may happen some small desynchronization and the real offsets in HTML get greater than the supposed (which are text offsets altered, actually, and should be smaller). So, try skipping some characters, but no more than the length of the word, before the actual occurence happens
					var	nHtmlWordLen : int = 0;	// Length of the match in the HTML representation (may disagree to text rep due to entity substitiution)
					var	ch : int;
					var	nEntityLen : int = 0;
					for( var nSeekSkip : int = 0; nSeekSkip < word.Text.Length; nSeekSkip++ )
					{
						// We make a guess that the word starts here, start checking the characters, one by one
						writerToken.Length = 0;	// Reset
						for( var nTextChars : int = 0; nTextChars < word.Text.Length; nTextChars++ )
						{
							ch = reader.Read( true, false, &nEntityLen );	// Peek one char (noremove), substitute entities if any, get the entity's length also (or 1 if an ordinary char)
							if( ch == -1 )
								throw new EndOfStreamException();
							if( char.ToLower(char( ch ), CultureInfo.CurrentCulture) != word.Text[ nTextChars ] )	// Break on unsuitable characters
								break;

							// Suitable character, collect its raw unmatched representation to the token-cache
							for( var c : int = 0; c < nEntityLen; c++ )
								writerToken.Append( reader.ReadChar( false ) );	// Entity-unmatched rep
						}
						if( !( nTextChars < word.Text.Length ) )	// Break if the word has been matched successfully
							break;

						// Flush the cached chars to the output, if any, as is (entity-unmatched)
						writer.Append( writerToken.ToString() );
						// And do the same to the latest char
						writer.Append( reader.ReadChar( false ) );
					}
					if(!(nSeekSkip < word.Text.Length))	// Fail if the word could not be matched even with the maximum allowed shift
						return null;	// Indicates failure, fallback to another solution

					// Create an entry for the search hits list and generate an ID for this search hit
					wordSearchHit = word;
					wordSearchHit.Section = "SearchHit-" + Guid.NewGuid().ToString();
					wordsSearchHits.Add(wordSearchHit);	// Record

					// Submit the opening tag for highlighting
					writer.Append( String.Format("<span id=\"{3}\" style=\"color: {0}; background-color: {1};\" title=\"Search result for {2}\">", color.ForeColor, color.BackColor, word.Original, wordSearchHit.Section) );	// Start hilite tag

@if(@DEBUG)	// Length check
					nIntroducedLength += String.Format("<span id=\"{3}\" style=\"color: {0}; background-color: {1};\" title=\"Search result for {2}\">", color.ForeColor, color.BackColor, word.Original, wordSearchHit.Section).Length;
@end

					// Write the matched word (introduced length is zero)
					writer.Append( writerToken.ToString() );

					// End hilite tag
					writer.Append( "</span>" );

@if(@DEBUG)	// Length check
					nIntroducedLength += ( "</span>" ).Length;
@end
				}

				// Copy the rest of the content (after the last search hit)
				nCharsToPassthru = content.Length - reader.Position;
				Debug.Assert(nCharsToPassthru >= 0);
				for( a= 0; a < nCharsToPassthru; a++ )
					writer.Append( reader.ReadChar( false ) );	// Simply read the next char
				Debug.Assert(content.Length - reader.Position == 0);

@if(@DEBUG)	// Length-check
				Debug.Assert( content.Length + nIntroducedLength == writer.ToString().Length, "Unexpected length of the hilited text." );
@end

				// Persist the word hits array for this session
				if(words.Length > 0)
					System.Array.Copy(wordsSearchHits.ToArray(words[0].GetType()), words, wordsSearchHits.Count);

				return writer.ToString();	// Our resulting string
			}
			catch(ex : EndOfStreamException)
			{
				Trace.WriteLine("HiliteMain has encountered an unexpected input stream end.");

				if(Core.ProductReleaseVersion == null)	// Non-retail
					ReportException(new Exception("The main highlighting scheme has encountered an unexpected input stream end. This usually indicates that the search hit offsets are wrong and point outside the content. Please write in the comments the type of the resource you were trying to view.\n\nPS: this is a DEBUG-only exception.", ex), false);

				return null;	// We've sucked, fallback to other methods
			}
		}

		/// Provides a context for actions that the browser wants to execute
        protected function GetActionContext( kind : ActionContextKind ) : IActionContext
        {
			var context : ActionContext;

			// Use the ContextProvider to get the action context, if possible
			if( _actionContextProvider != null )
				context = ActionContext(_actionContextProvider.GetContext( kind ));
			else
			{	// Construct manually
				Trace.WriteLine("[OMEA.MSHTML] Warning! Browser should have been populated with the context provider.");

				context = new ActionContext( kind, null, null );
				context.SetCommandProcessor( this );
            }

            // Update the context to reflect the current state
			var	strings = GetSelectionRecursive(_ocx.HtmlDocument);
			if(strings != null)
				context.SetSelectedText(strings[1], strings[0], TextFormat.Html);
            context.SetCurrentUrl(CurrentUrl);
            context.SetCurrentPageTitle(Title);

            return context;
        }

		/// Invokes the "back" action using the browser history, if possible, or some extra hacks.
		protected function GoBack() : boolean
		{
			if( ( _nHistoryDepth == 1 ) && ( _sCachedHtml != null ) )	// Special processing for the case we're going back to the manually-loaded page
			{
				var oldMaxNavigateDepth : int = _nMaxHistoryDepth;
				ShowHtml( _sCachedHtml, _ctxCachedSecurityContext );   // This resets MaxNavigateDepth
				_nMaxHistoryDepth = _nMaxHistoryDepth;

				// Ensure the page scrolls to where it should scroll
				_nOnLoadScrollTo = _nCachedScrollPos;
			}
			else	// Invoke the browser's internal "back" processing
			{
				try
				{
					_isGoingBack = true;
					_ocx.Browser.GoBack();
				}
				catch(ex : System.Runtime.InteropServices.COMException)
				{	// Sometimes the browser would throw an exception when trying to go Back, possibly when it's not capable of going back
					Trace.WriteLine("Cannot go Back using the Web browser control. " + ex.Message, "[OMEA.MSHTML]");
					return false;
				}
			}

			return true;
		}

		/// Invokes the "forward" action using the browser history, if possible, or some extra hacks.
		protected function GoForward() : boolean
		{
			try
			{
				_isGoingForward = true;
				if((_nHistoryDepth == 0) && (_nMaxHistoryDepth != 0) && (_sCachedHtml != null))	// Special processing for the case we're going out of the manually loaded page
					_ocx.Navigate( _sForwardUri );
				else	// Use browser's history
					_ocx.Browser.GoForward();
			}
			catch(ex : System.Runtime.InteropServices.COMException)
			{	// Sometimes the browser would throw an exception when trying to go Forward, possibly when it's not capable of going forward
				Trace.WriteLine("Cannot go Forward using the Web browser control. " + ex.Message, "[OMEA.MSHTML]");
				return false;
			}

			return true;
		}

		/// Invokes when MSHTML wants to change the status bar text.
		/// Formats it as sppropriate, appends the download progress indication (if currently downloading) and submits to the Omea StatusWriter.
		public function OnStatusTextChange( sNewText : System.String ) : void
		{
			sNewText = sNewText.Trim();	// Cut whitespace. Mostly needed when our default status message (" ") is received

			// Check if this is a jump-link to some anchor on this page, and this page is directly fed into view. In this case, prevent the raw "about:blank#name" text from displaying and change it with human-readable text instead
			if( sNewText.StartsWith("about:blank#") )
				sNewText = "Jump to " + sNewText.Substring( "about:blank#".Length );	// "Jump to name"

			//Trace.WriteLine("[OMEA.MSHTML] Status text has changed to: " + sNewText);
			_sStatusMessage = sNewText;	// Cache the status bar text

			if( _nDownloadProgress <= 0.999 )	// If a page download is in progress, append the progress indication to the status bar text
			{
				if( sNewText.Length != 0 )
					sNewText = String.Format( "{0} ({1}%)", sNewText, int( _nDownloadProgress * 100 ) );
				else
					sNewText = String.Format( "Loading… ({0}%)", int( _nDownloadProgress * 100 ) );
			}

			if(_statusWriter != null)
				_statusWriter.ShowStatus( sNewText, false );
		}

		public function OnTitleChange( sNewText : String ) : void
		{
			_lastTitle = sNewText;
			fire_TitleChanged(EventArgs.Empty);
		}

		public function OnNavigateComplete( uri : String ) : void
		{
			//Trace.WriteLine("[OMEA.MSHTML] OnNavigateComplete");
		}

		/// Return True to cancel navigation.
		public function OnBeforeNavigate( uri : System.String, frame : System.String, oPostData : System.Object, sHeaders : System.String) : boolean
		{
			//Trace.WriteLine("[OMEA.MSHTML] OnBeforeNavigate raised for " + uri);

			try
			{
				////////////////////////////////////////////////////////////////////////////////////////////////////////////////
				// Check if navigation should be cancelled/hanlded by the browser itself, without asking the external handlers
				if( !_ctx.AllowNavigation )
					return true;	// Cancel the navigation

				// Check if we were navigating to about:blank and navigaton was cancelled — the browser tries to show a banner, and it should be suppressed
				if((uri != null) && (uri.StartsWith("res:")) && (uri.IndexOf("navcancl.htm#") > 0))
					return true;	// Cancel the navigation

				// Check if we're trying to navigate to a bookmark on this very page
				// This case should be handled explicitly
				if( uri.StartsWith("about:blank#") )
				{
					try
					{
						// Extract the label name and search for it on the page
						var	sLabelName : System.String = uri.Substring( "about:blank#".Length );
						for( var oLabel in _ocx.HtmlDocument.getElementsByName( sLabelName ) )	// This query returns all the objects with such a name. Look for labels among them
						{
							if( oLabel.nodeName.ToLower() == "a" )	// Found a label. Jump to it and stop
							{
								oLabel.scrollIntoView();
								Trace.WriteLine( "[OMEA.MSHTML] Jumped to the anchor named " + sLabelName );
								return true;	// Search no more, cancel navigation
							}
						}
						Trace.WriteLine( "[OMEA.MSHTML] Failed to find the anchor named " + sLabelName );
					}
					catch( ex : Exception )
					{
						Trace.WriteLine( "[OMEA.MSHTML] Failed to find the anchor named " + sLabelName + ". " + ex.Message );
						Debug.Fail( ex );
					}

					return true;	// Cancel navigation even though not found
				}

				// Update the navigation cause
				var causeOld : BrowserNavigationCause = _navigationCause;	// The old navigation cause before this navigation attempt
				var causeNew : BrowserNavigationCause = _navigationCause;	// The proposed new cause that will be accepted when the navigation is finally decided
				switch(_navigationCause)
				{
				case BrowserNavigationCause.ShowHtml:	// Was uploaded directly, but user has drifted off by a link
					causeNew = BrowserNavigationCause.FollowLink;
					break;
				case BrowserNavigationCause.Navigate:	// Navigation was once requested; if history-depth is -1, we're fulfilling the original request (will be set to 0 below), otherwise, we're following a link
					causeNew = _nHistoryDepth == -1 ? BrowserNavigationCause.Navigate : BrowserNavigationCause.FollowLink;
					break;
				case BrowserNavigationCause.FollowLink:	// We've already followed a link once, here is one more, dont change the cause
					causeNew = BrowserNavigationCause.FollowLink;
					break;
				default:
					causeNew = BrowserNavigationCause.FollowLink;
				}
				if(_isGoingBack)
					causeNew = _nHistoryDepth <= 1 ? BrowserNavigationCause.ReturnToOriginal : BrowserNavigationCause.GoBack;
				else if(_isGoingForward)
					causeNew = BrowserNavigationCause.GoForward;
				Trace.WriteLine("NavigationCause is " + causeNew + ".", "[OMEA.MSHTML]");

				// Check whether this URI has a content that can be loaded into the mebedded browser window
				var bUriHasContent : boolean = DoesUriHaveContent(uri);

				//////////////////////////////////////////////////////////////////////////////////////////////////////
				// Prepare the BeforeNavigate event arguments: check the cases and fill the cancel/inplace arguments
				var args : BeforeNavigateEventArgs = new BeforeNavigateEventArgs(uri, frame, oPostData, sHeaders, causeNew);

				// Check if this link is to be opened in a new window
				// If in-place navigation is not allowed, the links are always opened in a new window
				// Otherwise, the NavigateInPlace results are always displayed in the current window, while other resources obey the general "Open ion new window" setting
				// URL protocols that do not provide any content act as if shown in a new window
				args.Inplace =
				(
					(bUriHasContent)
					&& (_ctx.AllowInPlaceNavigation)
					&& ((GetNavigateInplaceSetting()) || (causeOld == BrowserNavigationCause.Navigate))
				);

				///////////////////////////////
				// Query the external handler
				fire_BeforeNavigate(args);

				////////////////////////////////////////////////
				// Process the result, navigate if/how allowed

				if(args.Cancel)
					return true;	// Cancel navigation (new nav-koz not applied)

				// External window?
				if(!args.Inplace)
				{
					Trace.WriteLine( String.Format( "[OMEA.MSHTML] Opening link in new window, URI = \"{0}\".", uri) );
					//_ocx.Browser.Navigate( uri, BrowserNavConstants.navOpenInNewWindow );
					NavigateInExternalWindow2(uri, bUriHasContent);	// Do not use DDE if it's an URI of a type we do not understand
					return true;	// Cancel WebBrowser's navigation, we've already done our own (new nav-koz not applied)
				}

				///
				// Internal window, from now on (inplace navigation)

				// Check if we're going back, update history in this case
				if(_isGoingBack)
				{
					_nHistoryDepth--;	// "Pop from the Back stack"
					Debug.Assert(_nHistoryDepth >= 0);
				}
				else if( uri != "about:blank" )	// Just some normal navigation
				{
					// If we're navigating away from the manually fed page, remember its scrolling and the URL that goes after it
					if(causeOld == BrowserNavigationCause.ShowHtml)
					{
						_nCachedScrollPos = _ocx.HtmlDocument.body.scrollTop;
						_sForwardUri = uri;	// The new one
						_ctxCachedSecurityContext = _ctx;
					}

					// Update the History
					_nHistoryDepth++;	// "Push to Back stack" (for NavigateInPlace calls, this will bring the counter from -1 to 0)
					_nMaxHistoryDepth = _nHistoryDepth > _nMaxHistoryDepth ? _nHistoryDepth : _nMaxHistoryDepth;	// Update maximum if needed

					// Change Security Context to the default Internet one
					SecurityContext = WebSecurityContext.Internet;
				}

				// Reset the manually-set URI, if any, and retrieve it from the Web browser live from now on
				_uri = null;
				_navigationCause = causeNew;	// Accept the new nav-koz
				return false;	// Allow browser's navigation
			}
			catch(ex : Exception)
			{
				ReportException(ex, true);
			}
			finally
			{
				_isGoingBack = false;
				_isGoingForward = false;
			}
		}

		/// Checks whether the protocol hander responsive for handling this URI is capable of providing content or just can execute an action.
		function DoesUriHaveContent(uri : System.String) : boolean
		{
			var	sUriScheme : System.String = "<unretrievable>" + uri;
			try
			{
				sUriScheme = new Uri(uri).Scheme;
				/*var	keyProto : RegistryKey;
				if((keyProto = Registry.ClassesRoot.OpenSubKey("Protocols\\Handler\\" + sUriScheme, false)) != null)
				{
					Trace.WriteLine(String.Format("[OMEA.MSHTML] An URI with protocol schema {0} provides content for display.", sUriScheme));
					return true;
				}*/
				sUriScheme = sUriScheme.ToLower();
				if(_hashSupportedProto.Contains(sUriScheme))
				{
					Trace.WriteLine(String.Format("[OMEA.MSHTML] An URI with protocol schema {0} provides content for display.", sUriScheme));
					return true;
				}
			}
			catch(ex : Exception) {}

			Trace.WriteLine(String.Format("[OMEA.MSHTML] An URI with protocol schema {0} does not provide content for display.", sUriScheme));
			return false;
		}

		public function OnProgressChange( fProgress : double ) : void
		{
			_nDownloadProgress = fProgress;	// Save the progress value

			// Update the status bar text to indicate the download progress
			if( _statusWriter != null )
			{
				if( _sStatusMessage.Length != 0 )
					_statusWriter.ShowStatus( String.Format( "{0} ({1}%)", _sStatusMessage, int( _nDownloadProgress * 100 ) ), false );
				else
					_statusWriter.ShowStatus( String.Format( "Loading… ({0}%)", int( _nDownloadProgress * 100 ) ), false );
			}
		}

		/// Return True to cancel error processing.
		public function OnNavigateError( uri : String, frame : String, code : int) : boolean
		{
			var nCode : UInt32 = code >= 0 ? code : uint(0xFFFFFFFF) - uint(-code) + 1;
			var nErrorCode : NavigateErrorCodes = nCode;
			var sError : System.String = nErrorCode.ToString();
			var sMessage = String.Format("[OMEA.MSHTML] OnNavigateError for {0}, {1}, {2}: {3}", Object(uri), Object(frame), Object(code), Object(sError) );
			Trace.WriteLine( sMessage );
			return false;
		}

		/// Return True to cancel the web browser's new window processing.
		public function OnNewWindow( uri : String, TargetFrameName : String, a, b ) : boolean
		{
			// TODO: if you open a new tab for it, return that MSHTML instance as one of the parameters. Or tie it up to the new tab, don't rememebr that …
			NavigateInExternalWindow( uri );
			return true;
		}

		/// The Web browser control has completed loading and parsing a document, and it is full operational and ready for programmatical use.
		public function OnDocumentComplete(uri : System.String) : void
		{
			////////////
			// TODO: remove the diagnostic traces from this function !!!
			////////////
			try
			{
				// Check if there's deferred content to be uploaded (when we create or resurrect the Browser, a navigation to about:blank occurs and causes this event to happen)
				if(uri == "about:blank")
				{
					// Invoke the upload; if it happens, a True is returned, suppress all the actions, including firing of the DocumentComplete event, in this case, as it's a fake navigation we're currently processing
					if(DeferredShowHtml())
						return;
				}

				// Execute the following block only if the document class loaded into the Web browser is actually the HTML document class
				if(_ocx.HtmlDocument != null)
				{
					// Highlight the search words in the document if there are any cached
					if( _wordsToHighlight != null )
					{
						HiliteBackup( _wordsToHighlight );
						_wordsToHighlight = null;
					}
					else	// Main scheme: navigate to the first search hit, if available
						GotoNextSearchHit(true, false);

					// Change the status bar text to be empty by default
					try{
						_ocx.HtmlDocument.parentWindow.defaultStatus = " ";
					}catch(ex:Exception){Trace.WriteLine("Failed to set the default status in the DocumentComplete handler.", "[OMEA.MSHTML]");}

					// If we should scroll the page to the remembered position, do it
					// Do it after highlighting the words because the highlighting would scroll to search results as well
					if(_nOnLoadScrollTo != null)
					{
						_ocx.HtmlDocument.body.scrollTop = _nOnLoadScrollTo;
						_nOnLoadScrollTo = null;
					}

					// Raise the Web browser event
					fire_DocumentComplete(new DocumentCompleteEventArgs(uri));

					//_ocx.HtmlDocument.add_onkeydown(fire_KeyDown);
				}
			}
			catch(ex : Exception)
			{
				ReportException(ex, true);
			}
		}

		/// The OnQuit event has been fired from the site.
		public function OnQuit() : void
		{
			//Trace.WriteLine("OnQuit has been fired.", "[OMEA.MSHTML]");
		}

		/// The download operation has completed with either result (success, abortion, or failure).
		public function OnDownloadComplete() : void
		{
			//Trace.WriteLine("OnDownloadComplete has been fired.", "[OMEA.MSHTML]");

			// Fire the outgoing event
			fire_DownloadComplete(EventArgs.Empty);
		}

		/// Return True unless you want IE to show its menu as well.
		public function OnContextMenu( nTargetType : int, x : int, y : int, oCommandTarget, oHitObject )
		{
			// Generate the context menu arguments object
			// By default it is set up for showing the Omea context menu and suppressing the browser's one
			var	args : ContextMenuEventArgs = new ContextMenuEventArgs(x, y, ContextMenuTargetType(nTargetType), new MshtmlElement(oHitObject), GetActionContext(ActionContextKind.ContextMenu));

			// If we're requesting a menu for an image or a hyperlink, show the default web browser's menu
			if( ( nTargetType == ContextMenuTargetType.Anchor ) || ( nTargetType == ContextMenuTargetType.Control ) || ( nTargetType == ContextMenuTargetType.Image ) )
			{
				// Suppress Omea's context menu and enable the browsers
				args.CancelOmeaMenu = true;
				args.CancelNativeMenu = false;
			}

			// Query the event handlers for menu handling or setup
			fire_ContextMenu(args);

			// Show Omea menu, if not cancelled
			if((!args.CancelOmeaMenu) && (ICore.Instance != null))
			{
				try
				{
					var pnt : Point = PointToClient( new Point( x, y ) );	// Screen to client conversion
					Core.ActionManager.ShowResourceContextMenu( args.ActionContext, Parent, pnt.X, pnt.Y );
				}
				catch(ex : Exception)
				{
					ReportException(ex, true);
				}
            }

            // Return value determines whether the native menu is cancelled
			return args.CancelNativeMenu;
		}

		/// Return True if we've handled it and should not pass to the Browser.
		public function OnBeforeKeyDown( code : int, ctrl : boolean, alt : boolean, shift : boolean ) : boolean
		{
			//Trace.WriteLine("[OMEA.MSHTML] OnBeforeKeyDown for " + /*code*/_ocx.HtmlDocument.activeElement.tagName);

			// First, issue thet KeyDown event to check if someone wants to override the event processing
			var kea : KeyEventArgs = new KeyEventArgs(Keys(code | (ctrl ? Keys.Control : 0) | (alt ? Keys.Alt : 0) | (shift ? Keys.Shift : 0)));
			fire_KeyDown(kea);
			if(kea.Handled)
				return true;	// Do not execute an action or proecss this message in MSHTML

			var oActiveEl = _ocx.HtmlDocument.activeElement;

			// If no modifier keys are pressed, and a text edit is active, let that edit process the keystroke
			if(((ctrl == false) && (alt == false) && (shift == false)) || (JetTextBox.IsEditorKey(kea.KeyData)))
			{
				// If a text edit element has focus, let it process the keystroke
//				if( ( _ocx.HtmlDocument.activeElement.isTextEdit ) && ( String.Compare(_ocx.HtmlDocument.activeElement.tagName, "body", true, CultureInfo.InvariantCulture) != 0 ) )
				if( oActiveEl.isTextEdit && ( String.Compare( oActiveEl.tagName, "body", true, CultureInfo.InvariantCulture) != 0 ) )
					return false;
			}

			// Scrolling check: scroll if we can, otherwise (end of scroll) — go to the next unread item
			if(
				(code == 0x20) &&
					(
						((ctrl == false) && (alt == false) && (shift == false)) ||
						((ctrl == true) && (alt == false) && (shift == false))
					)
				)	// Spacebar or Ctrl+Spacebar is a shortcut for scroll / go to next unread item
			{
				// We can scroll by space (and jump to the next element) only if the body element is selected. If it's some other element, space should apply to it (either invoke onclick or scroll it if it's scrollable), but never jump to the next resource
//				if( String.Compare(_ocx.HtmlDocument.activeElement.tagName, "body", true, CultureInfo.InvariantCulture) != 0 )
				if((String.Compare( oActiveEl.tagName, "body", true, CultureInfo.InvariantCulture) != 0) && (String.Compare( oActiveEl.tagName, "a", true, CultureInfo.InvariantCulture) != 0))
					return false;

				// If we're in Design mode or ContentEditable mode, do not treat space as a jumping character
				if(( String.Compare( _ocx.HtmlDocument.designMode, "on", true, CultureInfo.InvariantCulture) == 0 ) || ( String.Compare(_ocx.HtmlDocument.body.contentEditable, "true", true, CultureInfo.InvariantCulture) == 0 ))
					return false;

				// If we can page down, let us page down and not jump to the next unread resource. If can page down no more, do jump to the next unread resource.
				if( CanPageDown() )
					return false;

				if(ICore.Instance != null)
				{
					try
					{
						Trace.WriteLine("[OMEA.MSHTML] Going to the next unread resource …");
						Core.ResourceBrowser.GotoNextUnread();
						return true;	// Handled by jumping to the next unread resource
					}
					catch(ex : Exception)
					{
						ReportException(ex, true);
					}
				}
			}

			// Omea Shortcut Processing
			if(ICore.Instance != null)
			{
				try
				{
					// Delegate processing to the action manager, return true if handled and false otherwise, True prevents IE from processing the key
					if(Core.ActionManager.ExecuteKeyboardAction(GetActionContext(ActionContextKind.Keyboard), Keys(code) | (ctrl ? Keys.Control : 0) | (alt ? Keys.Alt : 0) | (shift ? Keys.Shift : 0)))
						return true;
				}
				catch(ex : Exception)
				{
					ReportException(ex, true);
				}
			}

			// Check against the list if the unhandled keypress should be suppressed
			if((_hashSuppressedUnhandledKeys != null) && (_hashSuppressedUnhandledKeys.Contains(kea.KeyData)))
				return true;	// Prevent the key from being handled

			return false;	// Action not handled, let Browser play with it
		}

		/// Return True if we've handled it and should not pass to the Browser.
		public function OnBeforeKeyUp( code : int, ctrl : boolean, alt : boolean, shift : boolean ) : boolean
		{
			// Issue thet KeyUp event to check if someone wants to override the event processing
			var kea : KeyEventArgs = new KeyEventArgs(Keys(code | (ctrl ? Keys.Control : 0) | (alt ? Keys.Alt : 0) | (shift ? Keys.Shift : 0)));
			OnKeyUp(kea);
			if(kea.Handled)
				return true;	// Do not execute an action or proecss this message in MSHTML
		}

		/// Return True if we've handled it and should not pass to the Browser.
		public function OnBeforeKeyPress( code : int, ctrl : boolean, alt : boolean, shift : boolean ) : boolean
		{
			// Issue thet KeyPress event to check if someone wants to override the event processing
			var kpea : KeyPressEventArgs = new KeyPressEventArgs(char(code));
			OnKeyPress(kpea);
			if(kpea.Handled)
				return true;	// Do not execute an action or proecss this message in MSHTML
		}

		// Return one of the UrlPolicy values to apply the policy, or VT_EMPTY or missing-argument to relay processing to the default Internet security manager.
		public function OnUrlAction( uri : System.String, action : UrlAction, flags : Puaf )
		{
			//Trace.WriteLine( "[OMEA.MSHTML] UrlAction for " + uri );
			if( _ctx.PermissionSet == WebPermissionSet.Nothing )	// Prohibit All
				return UrlPolicy.Disallow;
			else if( _ctx.PermissionSet == WebPermissionSet.Everything )	// Permit All
				return UrlPolicy.Allow;

			return System.Reflection.Missing.Value;	// Deleagte resolution to the default Internet Security Manager (or other instance in the chain ;)
		}

		// The mshtml site has created a browser object
		public function OnBrowserCreated() : void
		{
		}

		/// If an attempt to do ShowHtml failed to succeed immediately due to the browser being not ready to serve the request, this function gets called when the Browser gets ready at last in order to feed it with the desired content
		/// Returns whether any deferred content has actually been uploaded
		protected function DeferredShowHtml() : boolean
		{
			try
			{
				if((_sDeferredShowHtml == null) || (_sDeferredShowHtml.Length == 0))
					return;	// There has been no content prepared for the deferred display

				Trace.WriteLine(String.Format("Uploading the deferred HTML content (length: {0}).", _sDeferredShowHtml.Length), "[OMEA.MSHTML]");

				// Reset the storage in order to check whether it tries to get deferred once more
				var	sHtml : System.String = _sDeferredShowHtml;
				_sDeferredShowHtml = null;

				// Upload!
				ShowHtml(sHtml, _ctx, _wordsToHighlight);

				// Deferred again? That should never be allowed to avoid infinite loops
				if(_sDeferredShowHtml != null)
					throw new InvalidOperationException("Could not display the content in the Web browser: the browser control is permanently not ready for serving our content.");
			}
			catch(ex : Exception)
			{
				ReportException(new Exception("The deferred HTML content could not be uploaded into the Web browser.", ex), false);
			}
			finally
			{
				// In either case (of success or failure), we prevent the content from being submitted again and again
				_sDeferredShowHtml = null;
			}
		}

		/// Ambient container's property specifying settings for the WebBrowser
		public DispIdAttribute( BrowserAmbientProperties.AmbientDlcontrol )
		function get AmbientDlControl() : System.Int32
		{
			var value : long = 0;	// In this function we assign the UInt32 values to the Int32-variable. Due to this, the compiler issues its warning that "this conversion may fail at runtime". However, the OLE which accepts our return value does not support a representation for UInt32 and an attempt to return a UInt32 will result in a loss of information (flags won't work, in short). So we use Int32. In practice (as checked at the calling side), all the values, even those above 0x7FF…FF, survive the assignment and conversion, and get received appropriately by the caller (if cast back to unsigned, of course).

			// Check the settings from the SecurityContext and apply to the DownloadControl value, as appropriate
			if( _ctx.ShowPictures )
				value |= long(DlControl.Dlimages);
			if( _ctx.PlayVideos )
				value |= long(DlControl.Videos);
			if( _ctx.PlaySounds )
				value |= long(DlControl.Bgsounds);

			if( _ctx.SilentMode )
				value |= long(DlControl.Silent);

			if( _ctx.WorkOffline )
				value |= long(DlControl.Forceoffline);

			if( _ctx.PermissionSet == WebPermissionSet.Nothing )
				value |= long(DlControl.NoScripts) | long(DlControl.NoJava) | long(DlControl.NoDlactivexctls) | long(DlControl.NoRunactivexctls) | long(DlControl.NoFramedownload) | long(DlControl.NoBehaviors) | long(DlControl.NoClientpull);

//			Trace.WriteLine("AmbientDlControl: " + value, "[OMEA.MSHTML]");
			return UInt2Int(uint(value));
		}

		/// Allows MSHTML to retrieve information about the host's UI requirements. Return the DocHostUiFlag combination.
		function get AmbientHostUiInfo()
		{
			try
			{
				//Trace.WriteLine( "[OMEA.MSHTML] AmbientHostUiInfo requested." );
				var value : long = 0;	// In this function we assign the UInt32 values to the Int32-variable. Due to this, the compiler issues its warning that "this conversion may fail at runtime". However, the OLE which accepts our return value does not support a representation for UInt32 and an attempt to return a UInt32 will result in a loss of information (flags won't work, in short). So we use Int32. In practice (as checked at the calling side), all the values, even those above 0x7FF…FF, survive the assignment and conversion, and get received appropriately by the caller (if cast back to unsigned, of course).

				// General flags
				value |= long(0)
				| long(DocHostUiFlag.FlatScrollbar)	// MSHTML uses flat scroll bars for any user interface (UI) it displays.
				| long(DocHostUiFlag.EnableFormsAutocomplete)	// Internet Explorer 5 or later. This flag enables the AutoComplete feature for forms in the hosted browser. The Intelliforms feature is only turned on if the user has previously enabled it. If the user has turned the AutoComplete feature off for forms, it is off whether this flag is specified or not.
				| long(DocHostUiFlag.ImeEnableReconversion)	// Internet Explorer 5 or later. During initialization, the host can set this flag to enable Input Method Editor (IME) reconversion, allowing computer users to employ IME reconversion while browsing Web pages. An input method editor is a program that allows users to enter complex characters and symbols, such as Japanese Kanji characters, using a standard keyboard. For more information, see the International Features reference in the Base Services section of the Microsoft Platform Software Development Kit (SDK).
				| long(DocHostUiFlag.Theme)	// Internet Explorer 6 or later. Specifies that the hosted browser should use themes for pages it displays.
				| long(DocHostUiFlag.No3dborder)	// MSHTML does not use 3-D borders on any frames or framesets. To turn the border off on only the outer frameset use DOCHOSTUIFLAG_NO3DOUTERBORDER
				| long(DocHostUiFlag.ActivateClienthitOnly)	// MSHTML only becomes UI active if the mouse is clicked in the client area of the window. It does not become UI active if the mouse is clicked on a nonclient area, such as a scroll bar.
				;

				if( !_ctx.AllowSelect )
					value |= DocHostUiFlag.Dialog;	// MSHTML does not enable selection of the text in the form.

				if( _ctx.AllowInPlaceNavigation )
					value |= DocHostUiFlag.EnableInplaceNavigation;	// Internet Explorer 5 or later. This flag enables the host to specify that navigation should happen in place. This means that applications hosting MSHTML directly can specify that navigation happen in the application's window. For instance, if this flag is set, you can click a link in HTML mail and navigate in the mail instead of opening a new Internet Explorer window.

				if( ( !_ctx.AllowInPlaceNavigation ) || (!GetNavigateInplaceSetting()) )	// Should we open a new window when navigating?
					value |= DocHostUiFlag.Opennewwin;	// MSHTML opens a site in a new window when a link is clicked rather than browse to the new site using the same browser window.

				return int(value);
			}
			catch( ex : Exception )
			{
				Trace.WriteLine("[OMEA.MSHTML] An exception has occured in the AmbientHostUiInfo property getter. " + ex.Message);
				return System.Reflection.Missing.Value;
			}
		}

		/// Determines whether the Web page can be scrolled down or not. Used mostly for scrolling down by spacebar (if it cannot scroll, it goes to the next unread resource).
		public function CanPageDown() : boolean
		{
			if((_ocx == null) || (_ocx.HtmlDocument == null))
				return false;
			var oFocused = _ocx.HtmlDocument.activeElement;

			if( oFocused == null || oFocused.tagName == "a" || oFocused.tagName == "A" )
				oFocused = _ocx.HtmlDocument.body;

			// Is there a space in the active element to scroll down?
			if( oFocused.scrollTop + oFocused.clientHeight < oFocused.scrollHeight )	// There is space to scroll
				return true;
		}

		/// Scrolls the content of the active element one page down.
		public function PageDown() : boolean
		{
			if((_ocx == null) || (_ocx.HtmlDocument == null))
				return false;
			var oFocused = _ocx.HtmlDocument.activeElement;
			if(oFocused == null)
				oFocused = _ocx.HtmlDocument.body;

			// Note: the exceptions should be trapped here because sometimes this member may be called when the browser is not quite ready yet, and in this case it will throw a COM Exception
			try
			{
				// Scroll the browser window down
				oFocused.doScroll( "pageDown" );
			}
			catch(ex : COMException)
			{
				Trace.WriteLine("An exception has occured when trying to scroll the document down. " + ex.Message, "[OMEA.MSHTML]");
			}

			return true;
		}

		public function get Title() : System.String
		{
			/*
			var	title = (_ocx != null) && (_ocx.Browser != null) ? _ocx.Browser.LocationName : null;
			return title != null ? title : "";	// Prevent from Null values returned from the valid existing browser
			*/
			return _lastTitle != null ? _lastTitle : "";
		}

		// TitleChanged event
		protected var _evtTitleChanged : HashSet = new HashSet();
		public function remove_TitleChanged( handler : EventHandler ) : void
		{	_evtTitleChanged.Remove(handler); }
		public function add_TitleChanged( handler : EventHandler ) : void
		{	_evtTitleChanged.Add(handler);	}
		public function fire_TitleChanged( args : EventArgs ) : void
		{
			var handler : EventHandler;
			for( var entry : HashSet.Entry in _evtTitleChanged.GetEnumerator() )
			{
				try
				{
					handler = entry.Key;
					handler( this, args );
				}
				catch(ex : Exception)
				{
					ReportException(ex, true);
				}
			}
		}

		// KeyDown event
		protected var _evtKeyDown : HashSet = new HashSet();
		public function remove_KeyDown( handler : KeyEventHandler ) : void
		{	_evtKeyDown.Remove(handler); }
		public function add_KeyDown( handler : KeyEventHandler ) : void
		{	_evtKeyDown.Add(handler);	}
		public function fire_KeyDown( args : KeyEventArgs ) : void
		{
			var handler : KeyEventHandler;
			for( var entry : HashSet.Entry in _evtKeyDown.GetEnumerator() )
			{
				try
				{
					handler = entry.Key;
					handler( this, args );
				}
				catch(ex : Exception)
				{
					ReportException(ex, true);
				}
			}
		}

		// ContextMenu event
		protected var _evtContextMenu : HashSet = new HashSet();
		public function remove_ContextMenu( handler : ContextMenuEventHandler ) : void
		{	_evtContextMenu.Remove(handler); }
		public function add_ContextMenu( handler : ContextMenuEventHandler ) : void
		{	_evtContextMenu.Add(handler);	}
		public function fire_ContextMenu( args : ContextMenuEventArgs ) : void
		{
			var handler : ContextMenuEventHandler;
			for( var entry : HashSet.Entry in _evtContextMenu.GetEnumerator() )
			{
				try
				{
					handler = entry.Key;
					handler( this, args );
				}
				catch(ex : Exception)
				{
					ReportException(ex, true);
				}
			}
		}

		public function get ReadyState() : BrowserReadyState
		{
			if((_ocx == null) || (_ocx.Browser == null))	// Return Unitialized if the control has not been created yet
				return BrowserReadyState.Uninitialized;
			return BrowserReadyState(_ocx.Browser.ReadyState);
		}

		// DocumentComplete event
		protected var _evtDocumentComplete : HashSet = new HashSet();
		public function remove_DocumentComplete( handler : DocumentCompleteEventHandler ) : void
		{	_evtDocumentComplete.Remove(handler); }
		public function add_DocumentComplete( handler : DocumentCompleteEventHandler ) : void
		{	_evtDocumentComplete.Add(handler);	}
		public function fire_DocumentComplete( args : DocumentCompleteEventArgs ) : void
		{
			var handler : DocumentCompleteEventHandler;
			for( var entry : HashSet.Entry in _evtDocumentComplete.GetEnumerator() )
			{
				try
				{
					handler = entry.Key;
					handler( this, args );
				}
				catch(ex : Exception)
				{
					ReportException(ex, true);
				}
			}
		}

		public function Stop() : void
		{
			if((_ocx != null) && (_ocx.Browser != null))	// If the browser has not been created yet
				_ocx.Browser.Stop();
		}

		// BeforeNavigate event
		protected var _evtBeforeNavigate : HashSet = new HashSet();
		public function remove_BeforeNavigate( handler : BeforeNavigateEventHandler ) : void
		{	_evtBeforeNavigate.Remove(handler); }
		public function add_BeforeNavigate( handler : BeforeNavigateEventHandler ) : void
		{	_evtBeforeNavigate.Add(handler);	}
		public function fire_BeforeNavigate( args : BeforeNavigateEventArgs ) : void
		{
			var handler : BeforeNavigateEventHandler;
			for( var entry : HashSet.Entry in _evtBeforeNavigate.GetEnumerator() )
			{
				try
				{
					handler = entry.Key;
					handler( this, args );
				}
				catch(ex : Exception)
				{
					ReportException(ex, true);
				}
			}
		}

		// BeforeShowHtml event
		protected var _evtBeforeShowHtml : HashSet = new HashSet();
		public function remove_BeforeShowHtml( handler : BeforeShowHtmlEventHandler ) : void
		{	_evtBeforeShowHtml.Remove(handler); }
		public function add_BeforeShowHtml( handler : BeforeShowHtmlEventHandler ) : void
		{	_evtBeforeShowHtml.Add(handler);	}
		public function fire_BeforeShowHtml( args : BeforeShowHtmlEventArgs ) : void
		{
			var handler : BeforeShowHtmlEventHandler;
			for( var entry : HashSet.Entry in _evtBeforeShowHtml.GetEnumerator() )
			{
				try
				{
					handler = entry.Key;
					handler( this, args );
				}
				catch(ex : Exception)
				{
					ReportException(ex, true);
				}
			}
		}

		// DownloadComplete event
		protected var _evtDownloadComplete : HashSet = new HashSet();
		public function remove_DownloadComplete( handler : EventHandler ) : void
		{	_evtDownloadComplete.Remove(handler); }
		public function add_DownloadComplete( handler : EventHandler ) : void
		{	_evtDownloadComplete.Add(handler);	}
		public function fire_DownloadComplete( args : EventArgs ) : void
		{
			var handler : EventHandler;
			for( var entry : HashSet.Entry in _evtDownloadComplete.GetEnumerator() )
			{
				try
				{
					handler = entry.Key;
					handler( this, args );
				}
				catch(ex : Exception)
				{
					ReportException(ex, true);
				}
			}
		}

		/// Converts a 32-bit unsigned integer into bitwise-equal signed representation for transfer thru OLE.
		public static function UInt2Int( src : UInt32 ) : Int32
		{
			return ((src <= 0x7FFFFFFF) ? int(src) : (-int(uint(0xFFFFFFFF) - src + 1)));
		}

		/// Converts a 32-bit unsigned integer incapsulated into a 64-bit signed integer into bitwise-equal signed representation for transfer thru OLE.
		public static function Long2Int( src : long ) : Int32
		{
			return ((src <= 0x7FFFFFFF) ? int(src) : (-int(long(0xFFFFFFFF) - src + 1)));
		}

		/// Border style of the control.
		Browsable(true) ComVisible(true) Category("Appearance") Description("Border style") DefaultValue(System.Windows.Forms.BorderStyle.None)
		public function get BorderStyle() : System.Windows.Forms.BorderStyle
		{
			return _border ? System.Windows.Forms.BorderStyle.Fixed3D : System.Windows.Forms.BorderStyle.None;
		}
		public function set BorderStyle(value : System.Windows.Forms.BorderStyle)
		{
			if(value == System.Windows.Forms.BorderStyle.Fixed3D)
				_border = true;
			else if(value == System.Windows.Forms.BorderStyle.None)
				_border = true;
			else
				throw new ArgumentOutOfRangeException("value", value, "Value of the BorderStyle property can be either BorderStyle.Fixed3D or BorderStyle.None.");
		}

		/// Executes a JScript expression over the HTML document and window objects (they're added to the global namespace) and returns the result.
		public function Exec(code : System.String) : System.Object
		{
			var document = _ocx.HtmlDocument;
			var window = document.window;

			return eval(code, "unsafe");	// Grant full permissions
		}

		/// <summary>
		/// Provides programmatical access to the document element of the HTML document loaded in the control.
		/// </summary>
		/// <remarks>
		/// <p>You may access the methods and properties of W3C HTML DOM <c>document</c> object by using the late binding mechanism.
		/// The <c>window</c> object may be retrieved as well from the <c>document.window</c> property.</p>
		/// <p>Note that the events are not guaranteed to work as is, and their handling is browser-specific.</p>
		/// <p>If the HTML document is not available, <c>Null</c> value is returned.
		/// This may happen if the control has not been instantiated yet, or the document loaded into the browser is not an HTML document
		/// (for example, MSHTML-based browser control is capable of displaying any ActiveDocument type registered with the system).</p>
		/// </remarks>
		/// <since>350</since>
		public function get HtmlDocument() : System.Object
		{
			if(_ocx == null)
				return null;
			return _ocx.HtmlDocument;
		}

		/// As opposed to HtmlDocument which returns the ActiveX object representing the document 'as is', wraps that object into a custom managed object that is convenient for use in non-late-bound languages.
		public function get ManagedHtmlDocument() : IHtmlDomDocument
		{
			return MshtmlDocument.Attach(HtmlDocument);
		}

		/// Initializes the _hashMshtmlCommands static variable and serves as a static constructor for it.
		protected static function InitHashMshtmlCommands() : Hashtable
		{
			var	hashMshtmlCommands : Hashtable = new Hashtable();

			for(var mi : MemberInfo in MshtmlDocumentCommands.GetMembers().GetEnumerator())
			{
				if(mi.MemberType == MemberTypes.Field)
					hashMshtmlCommands.Add(mi.Name, true);
			}

			return hashMshtmlCommands;
		}

		/// Initializes the _hashSupportedProto set.
		protected static function InitHashSupportedProto() : HashSet
		{
			var hashSupportedProto : HashSet = new HashSet();
			hashSupportedProto.Add("about");
			hashSupportedProto.Add("file");
			hashSupportedProto.Add("ftp");
			hashSupportedProto.Add("http");
			hashSupportedProto.Add("https");
			hashSupportedProto.Add("mk");
			hashSupportedProto.Add("ms-help");
			hashSupportedProto.Add("res");
			hashSupportedProto.Add("javascript");

			return hashSupportedProto;
		}

	    /// An external object for access from the Web page scripts that can be retrieved thru the window.external property.
		public function get ExternalObject() : System.Object
		{
			try
			{
				return new DispatchWrapper(_externalObject);
			}
			catch(ex : Exception)
			{
				Trace.WriteLine("[OMEA.MSHTML] Could not obtain an IDispatch wrapper for the external object.");
				return null;
			}
		}
		public function set ExternalObject(value : System.Object)
		{
			_externalObject = value;
		}

		/// Determines whether the GotoNextSearchHit function can perform its action at this time.
		public function CanGotoNextSearchHit(bForward : boolean) : boolean
		{
			if(_wordsSearchHits == null)	// Search has not been performed
				return false;

			// If the search is currently positioned "beyond", allow if there are any search hits
			if(_nCurrentSearchHit == -1)
				return _wordsSearchHits.Length != 0;

			// Check for each of the directions
			return ((bForward) && (_nCurrentSearchHit < _wordsSearchHits.Length - 1)) || ((!bForward) && (_nCurrentSearchHit > 0));
		}

		/// If there are search hits in the document, navigates to either previous or next one, depending on the parameter value.
		/// Does not loop around the end. Never throws an exception.
		public function GotoNextSearchHit(bForward : boolean, bHilite : boolean) : void
		{
			try
			{
				// Check if allowed
				if(!CanGotoNextSearchHit(bForward))
					return;

				// Position at a new hit
				if(_nCurrentSearchHit == -1)	// Beyond?
					_nCurrentSearchHit = bForward ? 0 : _wordsSearchHits.Length - 1;	// Position at the end
				else	// Positioned at some of the hits already
					_nCurrentSearchHit += int(bForward) * 2 - 1;	// Move in the direction appropriate

				// Apply the hit by scrolling to it
				var	htmlSearchHit = _ocx.HtmlDocument.getElementById( _wordsSearchHits[_nCurrentSearchHit].Section );	// This query returns the search hit HTML element (most probably, a <span/>)
				if(htmlSearchHit != null)	// Found
				{
					// Scroll the search hit into view
					htmlSearchHit.scrollIntoView(false);
					Trace.WriteLine(String.Format("Scrolled to the next/prev search hit #{0} \"{1}\".", _nCurrentSearchHit, _wordsSearchHits[_nCurrentSearchHit].Section), "[MSHTML]");

					// Highlight the search hit with selection, if needed
					if(bHilite)
					{
						var range = _ocx.HtmlDocument.body.createTextRange();
						range.moveToElementText(htmlSearchHit);
						range.select();
					}
				}
				else
					Trace.WriteLine(String.Format("Could not scroll into view the next/prev search hit though it's present in the list: could not find the element with id of \"{0}\".", _wordsSearchHits[_nCurrentSearchHit].Section), "[MSHTML]");
			}
			catch(ex : Exception)
			{
				Trace.WriteLine(String.Format("Failed to jump to the next/prev search hit. {0}.", ex.Message), "[MSHTML]");
				ReportException(new Exception("Failed to jump to the next/prev search hit.", ex), true);
			}
		}

		/// Reports an exception using the Core's reporting service.
		/// If the latter is unavailable, either shows a message-box (True), or rethrows the exception (False).
		public static function ReportException(ex : Exception, bShowOrThrowIfUnavailable : boolean) : void
		{
			if(ICore.Instance != null)
				Core.ReportException(ex, ExceptionReportFlags.AttachLog);
			else
			{
				if(bShowOrThrowIfUnavailable)
					MessageBox.Show(ex.Message, "An Exception Has Occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
				else
					throw ex;
			}
		}

		/// Gets or sets the set of keys that, if unhandled by text editor, event consumer and keyboard action system, should be prevented from being processed by the Web browser core.
		/// Null is a valid value that means "none".
		public function get SuppressedUnhandledKeys() : IntHashSet
		{
			return _hashSuppressedUnhandledKeys;
		}
		public function set SuppressedUnhandledKeys(value : IntHashSet)
		{
			_hashSuppressedUnhandledKeys = value;
		}

		/// Maps a WordPtr that represents a search hit to be highlighted to an HTML text range that represents it in the document.
		/// This is used in the backup highlighting scheme for collecting all the hits and sorting them in order of appearance.
		/// This information should be used locally and not persisted.
		internal class WordPtrToHtmlTxtRange implements IComparable
		{
			/// Initializer
			public function WordPtrToHtmlTxtRange(word : WordPtr, range : System.Object)
			{
				_word = word;
				_range = range;
			}

			/// The search hit.
			public var	_word : WordPtr;

			/// The object that implements the IHTMLTxtRange interface and represents the range in the document corresponding to the word.
			/// Also serves as the sorting key.
			public var _range : System.Object;

			/// Enables sorting by the order of appearance.
			public function CompareTo(obj : System.Object) : int
			{
				try
				{
					return _range.compareEndPoints("StartToStart", obj._range);	// Use the MSHTML's range-comparison service
				}
				catch(ex : Exception)
				{
					Trace.WriteLine("[MSHTML] Could not compare two range objects. " + ex.Message);
					return 0;
				}
			}
		}
	}
}

// TODO: maybe, suppress the exceptions in highlighting for the production version?
