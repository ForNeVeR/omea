// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetBrains Omea Mshtml Browser Component
//
// Implements the Web browser component wrapping with full-scale customization, including view options and security settings & zones.
// Consists of an unmanaged part (C++ ATL, raw hosting, a composite ActiveX control) and a managed part (JScript.NET, Windows Forms control around the unmanaged ActiveX control plus AbstractWebBrowser proxy-inheritor).
// The unmanaged parts server as a wrapper for the custom interfaces only, and should not carry out any meaningful processing. All the events should be delegated to the managed part for processing.
//
// This file belongs to the managed part.
// This file contains declarations of the constants which have been taken from WinAPI, MSDN and IDL files of the corresponding components (WebBrowser Control, MSHTML, Url Moniker) for the use by the managed part.
//
// © JetBrains Inc, 2004
// Written by (H) Serge Baltic
//
import System;

package JetBrains.Omea.GUIControls.MshtmlBrowser
{
	/// <summary>
	/// Refresh constants for the web browser. Origin: MSDN.
	/// </summary>
	public enum RefreshConstants
	{
		Normal = 0,
		IfExpired = 1,
		Continue = 2,
		Completely = 3
	}

	/// <summary>
	/// Search options flags for the ITxtRange.Find function (MSHTML interfaces). Origin: MSDN.
	/// </summary>
	public FlagsAttribute enum TextRangeFindFlags
	{
		SearchUp = 1,
		WholeWordsOnly = 2,
		CaseSensitive = 4
	}

	/// <summary>
	/// Constants affecting the Browser's Navigate command execution (IWebBrowser*.Navigate*). Origin: MSDN.
	/// </summary>
	public enum BrowserNavConstants
	{
		OpenInNewWindow = 0x1,
		NoHistory = 0x2,
		NoReadFromCache = 0x4,
		NoWriteToCache = 0x8,
		AllowAutosearch = 0x10,
		BrowserBar = 0x20,
		Hyperlink = 0x40
	}

	/// <summary>
	/// The zone manager maintains policies for a set of standard actions. These actions are identified by integral values (called action indexes) specified below. URL Actions — security actions to which certain policies may be applied. Origin: <urlmon.h>.
	/// </summary>
	public FlagsAttribute enum UrlAction : UInt32
	{
		Min = 0x00001000,
		DownloadMin = 0x00001000,
		DownloadSignedActivex = 0x00001001,
		DownloadUnsignedActivex = 0x00001004,
		DownloadCurrMax = 0x00001004,
		DownloadMax = 0x000011ff,
		ActivexMin = 0x00001200,
		ActivexRun = 0x00001200,
		ActivexOverrideObjectSafety = 0x00001201,
		ActivexOverrideDataSafety = 0x00001202,
		ActivexOverrideScriptSafety = 0x00001203,
		ScriptOverrideSafety = 0x00001401,
		ActivexConfirmNoobjectsafety = 0x00001204,
		ActivexTreatasuntrusted = 0x00001205,
		ActivexNoWebocScript = 0x00001206,
		ActivexCurrMax = 0x00001206,
		ActivexMax = 0x000013ff,
		ScriptMin = 0x00001400,
		ScriptRun = 0x00001400,
		ScriptJavaUse = 0x00001402,
		ScriptSafeActivex = 0x00001405,
		CrossDomainData = 0x00001406,
		ScriptPaste = 0x00001407,
		ScriptCurrMax = 0x00001407,
		ScriptMax = 0x000015ff,
		HtmlMin = 0x00001600,
		HtmlSubmitForms = 0x00001601,
		HtmlSubmitFormsFrom = 0x00001602,
		HtmlSubmitFormsTo = 0x00001603,
		HtmlFontDownload = 0x00001604,
		HtmlJavaRun = 0x00001605,
		HtmlUserdataSave = 0x00001606,
		HtmlSubframeNavigate = 0x00001607,
		HtmlMetaRefresh = 0x00001608,
		HtmlMixedContent = 0x00001609,
		HtmlMax = 0x000017ff,
		ShellMin = 0x00001800,
		ShellInstallDtitems = 0x00001800,
		ShellMoveOrCopy = 0x00001802,
		ShellFileDownload = 0x00001803,
		ShellVerb = 0x00001804,
		ShellWebviewVerb = 0x00001805,
		ShellShellexecute = 0x00001806,
		ShellCurrMax = 0x00001806,
		ShellMax = 0x000019ff,
		NetworkMin = 0x00001a00,
		CredentialsUse = 0x00001a00,
		AuthenticateClient = 0x00001a01,
		Cookies = 0x00001a02,
		CookiesSession = 0x00001a03,
		ClientCertPrompt = 0x00001a04,
		CookiesThirdParty = 0x00001a05,
		CookiesSessionThirdParty = 0x00001a06,
		CookiesEnabled = 0x00001a10,
		NetworkCurrMax = 0x00001a10,
		NetworkMax = 0x00001bff,
		JavaMin = 0x00001c00,
		JavaPermissions = 0x00001c00,
		JavaCurrMax = 0x00001c00,
		JavaMax = 0x00001cff,
		InfodeliveryMin = 0x00001d00,
		InfodeliveryNoAddingChannels = 0x00001d00,
		InfodeliveryNoEditingChannels = 0x00001d01,
		InfodeliveryNoRemovingChannels = 0x00001d02,
		InfodeliveryNoAddingSubscriptions = 0x00001d03,
		InfodeliveryNoEditingSubscriptions = 0x00001d04,
		InfodeliveryNoRemovingSubscriptions = 0x00001d05,
		InfodeliveryNoChannelLogging = 0x00001d06,
		InfodeliveryCurrMax = 0x00001d06,
		InfodeliveryMax = 0x00001dff,
		ChannelSoftdistMin = 0x00001e00,
		ChannelSoftdistPermissions = 0x00001e05,
		ChannelSoftdistMax = 0x00001eff
	}

	/// <summary>
	/// The zone manager maintains policies for a set of standard actions. These actions are identified by integral values (called action indexes) specified below. URL Policies — control whether a particular URL action is allowed or not. Origin: <urlmon.h>.
	/// </summary>
	public FlagsAttribute enum UrlPolicy : UInt32
	{
		ActivexCheckList = 0x00010000,
		CredentialsSilentLogonOk = 0x00000000,
		CredentialsMustPromptUser = 0x00010000,
		CredentialsConditionalPrompt = 0x00020000,
		CredentialsAnonymousOnly = 0x00030000,
		AuthenticateCleartextOk = 0x00000000,
		AuthenticateChallengeResponse = 0x00010000,
		AuthenticateMutualOnly = 0x00030000,
		JavaProhibit = 0x00000000,
		JavaHigh = 0x00010000,
		JavaMedium = 0x00020000,
		JavaLow = 0x00030000,
		JavaCustom = 0x00800000,
		ChannelSoftdistProhibit = 0x00010000,
		ChannelSoftdistPrecache = 0x00020000,
		ChannelSoftdistAutoinstall = 0x00030000,
		Allow = 0x00,
		Query = 0x01,
		Disallow = 0x03,
		NotifyOnAllow = 0x10,
		NotifyOnDisallow = 0x20,
		LogOnAllow = 0x40,
		LogOnDisallow = 0x80,
		MaskPermissions = 0x0f,
		Dontcheckdlgbox = 0x100
	}

	/// <summary>
	/// Contains the flags for the OnUrlAction method. Origin: MSDN.
	/// </summary>
	public FlagsAttribute enum Puaf : UInt32
	{
		Default = 0x0000000,
		Noui = 0x00000001,
		Isfile = 0x00000002,
		WarnIfDenied = 0x00000004,
		ForceuiForeground = 0x00000008,
		CheckTifs = 0x00000010,
		Dontcheckboxindialog = 0x00000020,
		Trusted = 0x00000040,
		AcceptWildcardScheme = 0x00000080
	}

	/// <summary>
	/// DispIds for ambient properties the container would be queried for by the WebBrowser. Origin: <urlmon.h>.
	/// </summary>
	public enum BrowserAmbientProperties
	{
		Windowobject = (-5500),
		Locationobject = (-5506),
		Historyobject = (-5507),
		Navigatorobject = (-5508),
		Securityctx = (-5511),
		AmbientDlcontrol = (-5512),
		AmbientUseragent = (-5513),
		Securitydomain = (-5514),
		DebugIssecureproxy = (-5515),
		DebugTrustedproxy = (-5516),
		DebugInternalwindow = (-5517),
		DebugEnablesecureproxyasserts = (-5518)
	}

	/// <summary>
	/// Flags to be returned by the properry with DispId equal to DISPID AMBIENT DLCONTROL. Origin: <urlmon.h>.
	/// </summary>
	public FlagsAttribute enum DlControl : UInt32
	{
		Dlimages = 0x00000010,
		Videos = 0x00000020,
		Bgsounds = 0x00000040,
		NoScripts = 0x00000080,
		NoJava = 0x00000100,
		NoRunactivexctls = 0x00000200,
		NoDlactivexctls = 0x00000400,
		Downloadonly = 0x00000800,
		NoFramedownload = 0x00001000,
		Resynchronize = 0x00002000,
		PragmaNoCache = 0x00004000,
		NoBehaviors = 0x00008000,
		NoMetacharset = 0x00010000,
		UrlEncodingDisableUtf8 = 0x00020000,
		UrlEncodingEnableUtf8 = 0x00040000,
		Noframes = 0x00080000,
		Forceoffline = 0x10000000,
		NoClientpull = 0x20000000,
		Silent = 0x40000000,
		Offlineifnotconnected = 0x80000000,
		Offline = Offlineifnotconnected
	}

	/// <summary>
	/// Error codes, as applicable to the OnNavigateError function which handles the DWebBrowserEvents*.NavigateError.
	/// </summary>
	public enum NavigateErrorCodes : UInt32
	{
		HttpStatusBadRequest = 400,
		HttpStatusDenied = 401,
		HttpStatusPaymentReq = 402,
		HttpStatusForbidden = 403,
		HttpStatusNotFound = 404,
		HttpStatusBadMethod = 405,
		HttpStatusNoneAcceptable = 406,
		HttpStatusProxyAuthReq = 407,
		HttpStatusRequestTimeout = 408,
		HttpStatusConflict = 409,
		HttpStatusGone = 410,
		HttpStatusLengthRequired = 411,
		HttpStatusPrecondFailed = 412,
		HttpStatusRequestTooLarge = 413,
		HttpStatusUriTooLong = 414,
		HttpStatusUnsupportedMedia = 415,
		HttpStatusRetryWith = 449,
		HttpStatusServerError = 500,
		HttpStatusNotSupported = 501,
		HttpStatusBadGateway = 502,
		HttpStatusServiceUnavail = 503,
		HttpStatusGatewayTimeout = 504,
		HttpStatusVersionNotSup = 505,
		InetEInvalidUrl = 0x800c0002,
		InetENoSession = 0x800c0003,
		InetECannotConnect = 0x800c0004,
		InetEResourceNotFound = 0x800c0005,
		InetEObjectNotFound = 0x800c0006,
		InetEDataNotAvailable = 0x800c0007,
		InetEDownloadFailure = 0x800c0008,
		InetEAuthenticationRequired = 0x800c0009,
		InetENoValidMedia = 0x800c000a,
		InetEConnectionTimeout = 0x800c000b,
		InetEInvalidRequest = 0x800c000c,
		InetEUnknownProtocol = 0x800c000d,
		InetESecurityProblem = 0x800c000e,
		InetECannotLoadData = 0x800c000f,
		InetECannotInstantiateObject = 0x800c0010,
		InetERedirectFailed = 0x800c0014,
		InetERedirectToDir = 0x800c0015,
		InetECannotLockRequest = 0x800c0016,
		InetEUseExtendBinding = 0x800c0017,
		InetETerminatedBind = 0x800c0018,
		InetECodeDownloadDeclined = 0x800c0100,
		InetEResultDispatched = 0x800c0200,
		InetECannotReplaceSfpFile = 0x800c0300
	}

	/// <summary>
	/// Flags that represent the MSHTML hosts's UI capabilities that are returned from the IDocHostUIHandler.GetHostInfo in response to the MSHTML's request.
	/// Defines which parts of MSHTML's behavior are handled by the host and which are not desired (eg don't draw the 3D-border).
	/// </summary>
	public FlagsAttribute enum DocHostUiFlag //: int
	{
		Dialog = 0x00000001,
		DisableHelpMenu = 0x00000002,
		No3dborder = 0x00000004,
		ScrollNo = 0x00000008,
		DisableScriptInactive = 0x00000010,
		Opennewwin = 0x00000020,
		DisableOffscreen = 0x00000040,
		FlatScrollbar = 0x00000080,
		DivBlockdefault = 0x00000100,
		ActivateClienthitOnly = 0x00000200,
		Overridebehaviorfactory = 0x00000400,
		Codepagelinkedfonts = 0x00000800,
		UrlEncodingDisableUtf8 = 0x00001000,
		UrlEncodingEnableUtf8 = 0x00002000,
		EnableFormsAutocomplete = 0x00004000,
		EnableInplaceNavigation = 0x00010000,
		ImeEnableReconversion = 0x00020000,
		Theme = 0x00040000,
		Notheme = 0x00080000,
		Nopics = 0x00100000,
		No3douterborder = 0x00200000
	}

	/// <summary>
	/// Flags that identify a group of context menu targets. May be passed to the OnContextMenu event handler.
	/// </summary>
	public FlagsAttribute enum ContextMenuTargetTypeFlag : int
	{
		Default = 0x1 << ContextMenuTargetType.Default,
		Image = 0x1 << ContextMenuTargetType.Image,
		Control = 0x1 << ContextMenuTargetType.Control,
		Table = 0x1 << ContextMenuTargetType.Table,
		TextSelect = 0x1 << ContextMenuTargetType.TextSelect,
		Anchor = 0x1 << ContextMenuTargetType.Anchor,
		Unknown = 0x1 << ContextMenuTargetType.Unknown
	}

	/// <summary>
	/// OLE Command IDs, especially as applied to the Web browser object. Source: MSDN on OLECMDID.
	/// </summary>
	public enum OleCmdId : int
	{
		Open = 1,
		New = 2,
		Save = 3,
		SaveAs = 4,
		SaveCopyAs = 5,
		Print = 6,
		PrintPreview = 7,
		PageSetup = 8,
		Spell = 9,
		Properties = 10,
		Cut = 11,
		Copy = 12,
		Paste = 13,
		PasteSpecial = 14,
		Undo = 15,
		Redo = 16,
		SelectAll = 17,
		ClearSelection = 18,
		Zoom = 19,
		GetZoomRange = 20,
		UpdateCommands = 21,
		Refresh = 22,
		Stop = 23,
		HideToolbars = 24,
		SetProgressMax = 25,
		SetProgressPos = 26,
		SetProgressText = 27,
		SetTitle = 28,
		SetDownloadState = 29,
		StopDownload = 30
	}

	/// <summary>
	/// OLE Command execution options. Source: MSDN on OLECMDEXECOPT.
	/// </summary>
	public enum OleCmdExecOpt : int
	{
		DoDefault = 0, 	// Prompt the user for input or not, whichever is the default behavior.
		PromptUser = 1,	// Execute the command after obtaining user input.
		DontPromptUser = 2,	// Execute the command without prompting the user. For example, clicking the Print toolbar button causes a document to be immediately printed without user input.
		ShowHelp = 3	//Show help for the corresponding command, but do not execute.
	}

	/// <summary>
	/// OLE Command status returned in response to the QueryStatus request. Source: MSDN on OLECMDF.
	/// </summary>
	public FlagsAttribute enum OleCmdStatus : int
	{
		Supported = 1,	// The command is supported by this object.
		Enabled = 2,	// The command is available and enabled.
		Latched = 4,	// The command is an on-off toggle and is currently on.
		Ninched = 8	// Reserved for future use.
	}

	/// <summary>
	/// Commands that are natively supported by MSHTML <c>document</c> object and can be just forwarded to document for processing by either HTML editor or viewer.
	/// </summary>
	public enum MshtmlDocumentCommands	// TODO: assign values from the corresponding IDM_… constants to members of this enumeration
	{
		CreateBookmark,
		CreateLink,
		InsertImage,
		Bold,
		BrowseMode,
		EditMode,
		InsertButton,
		InsertIFrame,
		InsertInputButton,
		InsertInputCheckbox,
		InsertInputImage,
		InsertInputRadio,
		InsertInputText,
		InsertSelectDropdown,
		InsertSelectListbox,
		InsertTextArea,
		InsertHtmlArea,
		Italic,
		SizeToControl,
		SizeToControlHeight,
		SizeToControlWidth,
		Underline,
		Copy,
		Cut,
		Delete,
		Print,
		JustifyCenter,
		JustifyFull,
		JustifyLeft,
		JustifyRight,
		JustifyNone,
		Paste,
		PlayImage,
		StopImage,
		InsertInputReset,
		InsertInputSubmit,
		InsertInputFileUpload,
		InsertFieldset,
		Unselect,
		BackColor,
		ForeColor,
		FontName,
		FontSize,
		FormatBlock,
		Indent,
		InsertMarquee,
		InsertOrderedList,
		InsertParagraph,
		InsertUnorderedList,
		Outdent,
		Redo,
		Refresh,
		RemoveParaFormat,
		RemoveFormat,
		SelectAll,
		StrikeThrough,
		Subscript,
		Superscript,
		Undo,
		Unlink,
		InsertHorizontalRule,
		UnBookmark,
		OverWrite,
		InsertInputPassword,
		InsertInputHidden,
		DirLTR,
		DirRTL,
		BlockDirLTR,
		BlockDirRTL,
		InlineDirLTR,
		InlineDirRTL,
		SaveAs,
		Open,
		Stop
	}
}
