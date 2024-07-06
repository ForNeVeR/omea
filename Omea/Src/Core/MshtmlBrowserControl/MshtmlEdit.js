// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetBrains Omea Mshtml Browser Component
//
// Implements the Html Editor Based on the Mshtml Control
//
// © JetBrains Inc, 2004—2006
// Written by (H) Serge Baltic

import System;
import System.ComponentModel;
import System.Windows.Forms;
import System.Diagnostics;
import JetBrains.Omea.OpenAPI;
import System.Runtime.InteropServices;
import System.IO;
import System.Text;
import JetBrains.Omea.HTML;
import JetBrains.Omea.GUIControls;
import System.Globalization;
import System.Web;
import System.Reflection;

package JetBrains.Omea.GUIControls.MshtmlBrowser
{
	public
	AxHost.ClsidAttribute("{8a2f0dbe-ec1b-4d1d-8712-4259211b41b4}") DesignTimeVisible( true ) IDispatchImplAttribute( IDispatchImplType.InternalImpl )
	class MshtmlEdit extends AxHost implements ICommandProcessor
	{
		/// The underlying MshtmlSite ActiveX Control that wraps the WebBrowser object.
		protected var _ocx : MshtmlSite.Net.IMshtmlBrowser;

	    /// Default ctor, initializes the instance variables.
		public function MshtmlEdit()
		{
			super("8a2f0dbe-ec1b-4d1d-8712-4259211b41b4");
			Trace.WriteLine("[OMEA.MSHTML] MshtmlEditorControl..ctor()");
 		}

 		/// As we may assign a content property even before the control creation (for example, at design time), we have to store the value somewhere until we can upload it into the actual MSHTML COM control. Here's the storage place.
 		/// OnBrowserCreated initiates displaying it.
 		/// It's not used after creating the control and submitting this value into it.
 		/// May be an emptystring or Null before the control is created or some value is assigned to it.
 		protected var _sDeferredHtml : System.String = "";

		/// When the HTML content is uploaded before the control is created, and if the security context is set, then the context is stored here and applied when loading the deferred HTML content as control gets created.
        protected var _wscDeferredContext : WebSecurityContext = null;

	    /// A source for obtaining the action context whenever needed, originates from the ContextProvider property setter.
	    protected var _actionContextProvider : IContextProvider = null;

	    /// A timer that, when the browser is created, waits for a small amount of time and then submits the deferred content into it, if needed. Note that this timer goal is not the timeout itself, but avoiding calls to the mshtml browser host from its own callback, which may cause unexpected results.
	    protected var _timerDeferCreation : Timer = null;

	    /// Determines whether the control shows its etched 3D-border or not.
	    protected var _border : boolean = false;

	    protected var _pasteHandler = null;

	    /// A hash table that contains a list of string command identifiers that are supported by both this control and MSHTML control and can be just forwarded for processing.
	    protected static var _hashMshtmlCommands : Hashtable = InitHashMshtmlCommands();

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
			}
			catch( ex : Exception )
			{
				Trace.WriteLine("[OMEA.MSHTML] An exception has occured when trying to attach the managed wrapper to the underlying MSHTML control: " + ex.ToString() );
			}
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

		public function CanExecuteCommand(command : String) : boolean
		{
			try
			{
				// Check for direct forwarding to an IE command
				if(_hashMshtmlCommands.ContainsKey(command))
					return _ocx.HtmlDocument.queryCommandEnabled(command);

				// Other commands
        		switch(command)
        		{
					case DisplayPaneCommands.FindInPage: return true;
					case DisplayPaneCommands.Cut:        return _ocx.Browser.QueryStatusWB(OleCmdId.Cut) & OleCmdStatus.Enabled;
					case DisplayPaneCommands.Copy:       return _ocx.Browser.QueryStatusWB(OleCmdId.Copy) & OleCmdStatus.Enabled;
					case DisplayPaneCommands.Paste:      return _ocx.Browser.QueryStatusWB(OleCmdId.Paste) & OleCmdStatus.Enabled;
					case DisplayPaneCommands.Print:      return true;
					case "IncFontSize":                  return true;
					case "DecFontSize":                  return true;
					default: return false;
        		}
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
			/*
				Commands "IncFontSize", "DecFontSize" are artificial wrappers around
				supported command "FontSize". Since Ome toolbar support only parameterless
				commands these commands emulate passing a predefined parameter (in our case
				+1 and -1).
			*/
			try
			{
				// Check for direct forwarding to an IE command
				if(_hashMshtmlCommands.ContainsKey(command))
					return _ocx.HtmlDocument.execCommand(command, true, param);

				// Other commands
        		switch(command)
        		{
				case DisplayPaneCommands.FindInPage: _ocx.ExecDocumentCommand( "Find", true ); break;
				case DisplayPaneCommands.Cut:        _ocx.Browser.ExecWB(OleCmdId.Cut, OleCmdExecOpt.DontPromptUser); break;
				case DisplayPaneCommands.Copy:       _ocx.Browser.ExecWB(OleCmdId.Copy, OleCmdExecOpt.DontPromptUser); break;
				case DisplayPaneCommands.Paste:      _ocx.Browser.ExecWB(OleCmdId.Paste, OleCmdExecOpt.DontPromptUser); break;
				case DisplayPaneCommands.Print:      _ocx.Browser.ExecWB(OleCmdId.Print, OleCmdExecOpt.PromptUser ); break;
				case "IncFontSize":					ExecChangeFontSize(+1);
				case "DecFontSize":					ExecChangeFontSize(-1);
				default:
					throw new InvalidOperationException(String.Format("Trying to execute the disabled command \"{0}\" on an MSHTML Editor control.", command));
        		}
 			}
			catch(ex : Exception)
			{
				Trace.WriteLine(String.Format("[OMEA.MSHTML] Cannot execute the \"{0}\" command. {1}", command, ex.Message));
				throw new Exception(String.Format("MSHTML Editor cannot execute the \"{0}\" command. {1}", command, ex.Message));
			}
		}

		/// The Html property gets or sets the HTML source being edited by this control.
		/// If the control has not been created yet, returns either the assigned deferred HTML, an empty string, or null.
		Browsable(true) ComVisible(true) Category("Appearance") Description("HTML content of the editor") DefaultValue("<html><body>&nbsp;</body></html>")
		public function get Html() : System.String
		{
			if( _ocx != null )	// Control exists
				return _ocx.GetHtmlText();
			else
				return _sDeferredHtml;	// Content prepared for display
		}
		public function set Html(value : System.String)
		{
			if(_ocx != null)
			{	// The control has already been created, upload the HTML content directly
				_sDeferredHtml = "";	// Reset

				// Upload the content
				if(_ocx.HtmlDocument == null)	// Try to recreate back the MSHTML control if the browser holds some other document now
					_ocx.Navigate("about:blank");
				_ocx.SettingsChanged();	// Force IE re-read the options

				_ocx.ShowHtmlText(value);	// Upload the page's HTML source into the browser
			}
			else	// The control has not been created yet, should defer the content display until it's instantiated
				_sDeferredHtml = value;
		}

		public function SetHtml( value : System.String )
		{
		    SetHtml( value, null );
		}

		public function SetHtml( value : System.String, ctx : WebSecurityContext )
		{
			if(_ocx != null)
			{	// The control has already been created, upload the HTML content directly
				_sDeferredHtml = "";	// Reset

				// Upload the content
				if(_ocx.HtmlDocument == null)	// Try to recreate back the MSHTML control if the browser holds some other document now
					_ocx.Navigate("about:blank");
				_ocx.SettingsChanged();	// Force IE re-read the options

				// Upload the page's HTML source into the browser
				if( ctx != null )
				    _ocx.ShowHtmlText( value, ctx );
				else
				    _ocx.ShowHtmlText( value );
			}
			else // The control has not been created yet, should defer the content display until it's instantiated
			{
				_sDeferredHtml = value;
				_wscDeferredContext = ctx;
            }
		}

		/// The Html property gets or sets the plain-text representation of an HTML source being edited by this control.
		/// If the control has not been created yet, always returns null.
		ReadOnly(true) Browsable(true) ComVisible(true) Category("Appearance") Description("Text representation of the HTML content being edited")
		public function get Text() : System.String
		{
			if((_ocx != null) && (_ocx.HtmlDocument != null) && (_ocx.HtmlDocument.body))	// Control exists, and has something loaded into
				return _ocx.HtmlDocument.body.innerText;
			else
				return null;	// Can not extract text from the stored HTML without the MSHTML parser
		}
		public function set Text(value : System.String)
		{
			if(value == null)
				return;

			// Prepare the HTML representation of the plain text
			value = "<html><body><pre>" + HttpUtility.HtmlEncode(value) + "</pre></body></html>";

			// Apply the HTML rep
			Html = value;
		}

		/// Provides a context for actions that the editor wants to execute
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

            // Retrieve the rich and plaintext selection
            var	sHtmlSelection;
			sHtmlSelection = ((sHtmlSelection = _ocx.Browser.Document.selection.createRange().htmlText) != null) ? sHtmlSelection : "";
            var	sTextSelection;
			sTextSelection = ((sTextSelection = _ocx.Browser.Document.selection.createRange().text) != null) ? sTextSelection : "";

            // Update the context to reflect the current state
            context.SetSelectedText(sHtmlSelection, sTextSelection, TextFormat.Html);
            context.SetCurrentUrl("");

            return context;
        }

		/// Some content has been loaded into the editor.
		public function OnDocumentComplete( url : System.String ) : void
		{
			// Enable the editing mode
			if(_ocx.HtmlDocument != null)
				_ocx.HtmlDocument.designMode = "On";
		}

		/// The download operation has completed with either result (success, abortion, or failure).
		public function OnDownloadComplete() : void
		{
			//Trace.WriteLine("OnDownloadComplete has been fired.", "[OMEA.MSHTML]");

			// Fire the outgoing event
			fire_DownloadComplete(EventArgs.Empty);

			// Start listening for the document changes
			if((ManagedHtmlDocument != null) || (ManagedHtmlDocument.Body != null))	// If Null, it's a premature DownloadComplete, another one will come
				ManagedHtmlDocument.Body.add_PropertyChange(OnBodyPropertyChange);
		}

		/// Shows the editor's context menu
		/// Return True unless you want IE to show its menu as well.
		public function OnContextMenu( nTargetType : int, x : int, y : int, oCommandTarget, oHitObject )
		{
			// Show our own menu
			var pnt : Point = PointToClient( new Point( x, y ) );
            Core.ActionManager.ShowResourceContextMenu( GetActionContext( ActionContextKind.ContextMenu ), Parent, pnt.X, pnt.Y );
			return true;
		}

		/// Preprocesses the keys pressed within the browser and allows to execute Omea actions associated with them.
		/// Return True if we've handled it and should not pass to the Browser.
		public function OnBeforeKeyDown( code : int, ctrl : boolean, alt : boolean, shift : boolean ) : boolean
		{
			//Trace.WriteLine("[OMEA.MSHTML] OnBeforeKeyDown for " + /*code*/_ocx.HtmlDocument.activeElement.tagName);

			// First, issue thet KeyDown event to check if someone wants to override the event processing
			var kea : KeyEventArgs = new KeyEventArgs((Keys(code)) | (alt ? Keys.Alt : 0) | (ctrl ? Keys.Control : 0) | (alt ? Keys.Shift : 0));
			OnKeyDown(kea);

			// Raise the KeyPress event
			var	kpea : KeyPressEventArgs = new KeyPressEventArgs(char(code));
			OnKeyPress(kpea);

			// Handled?
			if((kea.Handled) || (kpea.Handled))	// We have processed the event ourselves
				return true;

			// If no modifier keys are pressed, or the key is an editor one, let us process the keystroke ourselves (regardless of whether the active element is reported as edit or not — we're always an editor in the whole)
			if(((ctrl == false) && (alt == false) && (shift == false)) || (JetTextBox.IsEditorKey(kea.KeyData)))
				return false;

			// Omea Shortcut Processing
			if(Core.ActionManager.ExecuteKeyboardAction(GetActionContext(ActionContextKind.Keyboard), Keys(code) | (ctrl ? Keys.Control : 0) | (alt ? Keys.Alt : 0) | (shift ? Keys.Shift : 0)))	// Delegate processing to the action manager, return true if handled
				return true;	// Done with this key

			return false;	// Leave the event processing to MSHTML
		}

		// The mshtml site has created a browser object
		public function OnBrowserCreated() : void
		{
			// Activate the timer and schedulle the deferred action
			_timerDeferCreation = new Timer();
			_timerDeferCreation.Interval = 100;
			_timerDeferCreation.add_Tick(OnBrowserCreatedDeferred);
			_timerDeferCreation.Start();
		}

		// The mshtml site has created a browser object (the deferred handler)
		public function OnBrowserCreatedDeferred( sender : Object, e : EventArgs ) : void
		{
			// Deactivate the timer
			_timerDeferCreation.Stop();
			_timerDeferCreation.Dispose();
			_timerDeferCreation = null;

			// If there was a deferred content, display that content in the browser
			if(( _sDeferredHtml != null ) && (_sDeferredHtml != "") )
			{
				Html = _sDeferredHtml;
//				SetHtml( _sDeferredHtml, _wscDeferredContext );
			}
			if( _pasteHandler != null )
			{
                var document = _ocx.HtmlDocument;
                var body = document.body;
                body.onpaste = _pasteHandler;
			}
		}

		// Return one of the UrlPolicy values to apply the policy, or VT_EMPTY or missing-argument to relay processing to the default Internet security manager.
		public function OnUrlAction( uri : System.String, action : UrlAction, flags : Puaf )
		{
			Trace.WriteLine( "[OMEA.MSHTML] UrlAction for " + uri );
			// TODO: determine what an html editor actually needes from these.
			return System.Reflection.Missing.Value;	// Deleagte resolution to the default Internet Security Manager (or other instance in the chain ;)
		}

		/// Ambient container's property specifying settings for the WebBrowser
		public DispIdAttribute( BrowserAmbientProperties.AmbientDlcontrol )
		function get AmbientDlControl() : System.Int32
		{
			var value : long = 0;	// In this function we assign the UInt32 values to the Int32-variable. Due to this, the compiler issues its warning that "this conversion may fail at runtime". However, the OLE which accepts our return value does not support a representation for UInt32 and an attempt to return a UInt32 will result in a loss of information (flags won't work, in short). So we use Int32. In practice (as checked at the calling side), all the values, even those above 0x7FF…FF, survive the assignment and conversion, and get received appropriately by the caller (if cast back to unsigned, of course).

			value |= long(DlControl.Forceoffline);
			value |= long(DlControl.NoScripts) | long(DlControl.NoJava) | long(DlControl.NoDlactivexctls) | long(DlControl.NoRunactivexctls) | long(DlControl.NoFramedownload) | long(DlControl.NoBehaviors) | long(DlControl.NoClientpull);

			//Trace.WriteLine("[OMEA.MSHTML] AmbientDlControl");
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
				| long(_border ? 0 : DocHostUiFlag.No3dborder)	// MSHTML does not use 3-D borders on any frames or framesets. To turn the border off on only the outer frameset use DOCHOSTUIFLAG_NO3DOUTERBORDER
				| long(DocHostUiFlag.ActivateClienthitOnly)	// MSHTML only becomes UI active if the mouse is clicked in the client area of the window. It does not become UI active if the mouse is clicked on a nonclient area, such as a scroll bar.
				;

				//value |= DocHostUiFlag.Dialog;	// MSHTML does not enable selection of the text in the form.
				value |= DocHostUiFlag.Opennewwin;	// MSHTML opens a site in a new window when a link is clicked rather than browse to the new site using the same browser window.

				return int(value);
			}
			catch( ex : Exception )
			{
				Trace.WriteLine("[OMEA.MSHTML] An exception has occured in the AmbientHostUiInfo property getter. " + ex.Message);
				return System.Reflection.Missing.Value;
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
		Browsable(false)
		public function get HtmlDocument() : System.Object
		{
			if(_ocx == null)
				return null;
			return _ocx.HtmlDocument;
		}

		/// Provides access to the inlying Shell Doc View object.
		Browsable(false)
		public function get WebBrowser() : System.Object
		{
			if(_ocx == null)
				throw new InvalidOperationException("The control has not been created yet.");
			return _ocx.Browser;
		}

		/// Executes a JScript expression over the HTML document and window objects (they're added to the global namespace) and returns the result.
		public function Exec(code)
		{
			var document = _ocx.HtmlDocument;
			var window = document.window;

			return eval(code, "unsafe");	// Grant full permissions
		}

		/// The selection, as HTML.
		public function get SelectedHtml() : String
		{
			try
			{
				// Return either the selection text or an empty string if it is null (by default JS will coerce that null to an "undefined" string-value :)
				var	sSelection;
				return ((sSelection = _ocx.Browser.Document.selection.createRange().htmlText) != null) ? sSelection : "";
			}
			catch(ex : Exception)
			{
				Trace.WriteLine("[OMEA.MSHTML] Cannot retrieve the selected html text: " + ex.Message);
				return "";
			}
		}

		/// The selection, as plain text.
		public function get SelectedText() : String
		{
			try
			{
				// Return either the selection text or an empty string if it is null (by default JS will coerce that null to an "undefined" string-value :)
				var	sSelection;
				return ((sSelection = _ocx.Browser.Document.selection.createRange().text) != null) ? sSelection : "";
			}
			catch(ex : Exception)
			{
				Trace.WriteLine("[OMEA.MSHTML] Cannot retrieve the selected text: " + ex.Message);
				return "";
			}
		}

		// KeyDown event
		protected var evtKeyDown : ArrayList = new ArrayList();
		public function remove_KeyDown( handler : KeyEventHandler ) : void
		{	evtKeyDown.Remove( handler ); }
		public function add_KeyDown( handler : KeyEventHandler ) : void
		{	evtKeyDown.Add( handler );	}
		protected function OnKeyDown( args : KeyEventArgs ) : void
		{
			for( var handler : KeyEventHandler in evtKeyDown.GetEnumerator() )
				handler( this, args );
		}

		public function add_PasteHandler( handler : EventHandler ) : void
		{
            _pasteHandler = handler;
		}

		/// As opposed to HtmlDocument which returns the ActiveX object
		/// representing the document 'as is', wraps that object into a custom
		/// managed object that is convenient for use in non-late-bound languages.
		public function get ManagedHtmlDocument() : IHtmlDomDocument
		{
			return MshtmlDocument.Attach(HtmlDocument);
		}

		/// Increases or decreases the selection font size.
		public function ExecChangeFontSize( incr : int )
	    {
            var obj : Object;
            var range = _ocx.HtmlDocument.selection.createRange();

            if( range == null )
                range = _ocx.HtmlDocument;

            obj = range.queryCommandValue( "FontSize" );

            if( obj != null )
            {
				var num1 : int = obj;
				var num2 : int = num1 + incr;
				range.execCommand( "FontSize", false, num2 );
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

		/// Some property of the Body element has changed
		public function OnBodyPropertyChange(sender : System.Object, args : HtmlEventArgs)
		{
			// Check if the text has changed, notify if so
			if((args.PropertyName == "innerText") || (args.PropertyName == "innerHTML"))
			{
				Trace.WriteLine("[MSHTML] The text property has changed.");
				fire_PropertyChanged(new PropertyChangedEventArgs("Html"));
				fire_PropertyChanged(new PropertyChangedEventArgs("Text"));
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

		/// PropertyChanged event (emulates one from the System.ComponentModel.INotifyPropertyChanged interface)
		/// Upon receiving, cast EventArgs args to PropertyChangedEventArgs
		protected var _evtPropertyChanged : HashSet = new HashSet();
		public function remove_PropertyChanged( handler : EventHandler ) : void
		{	_evtPropertyChanged.Remove(handler); }
		public function add_PropertyChanged( handler : EventHandler ) : void
		{	_evtPropertyChanged.Add(handler);	}
		public function fire_PropertyChanged( args : EventArgs ) : void
		{
			var handler : EventHandler;
			for( var entry : HashSet.Entry in _evtPropertyChanged.GetEnumerator() )
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
	}

	/// The arguments class for MshtmlEdit.PropertyChanged event, emulates the System.ComponentModel.PropertyChangedEventArgs class
	public class PropertyChangedEventArgs extends EventArgs
	{
		protected var _sPropertyName : System.String;
		public function PropertyChangedEventArgs(sPropertyName : System.String)
		{
			_sPropertyName = sPropertyName;
		}

		/// Gets the name of the property that changed.
		public function get PropertyName() : System.String
		{
			return _sPropertyName;
		}
	}
}
