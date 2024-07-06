// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// ActionManager.h : Declaration of the CActionManager
//
// CActionManager implements the UI actions logic and binding of UI controls to UI actions.
// Contains services for registering and unregistering actions for different types of
// Internet Explorer UI controls. Works in conjunction with a few other classes which
// serve as a dynamic representation of IE UI controls and delegate action processing to this object.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once
#include "CommonResource.h"       // main symbols

#include "Wrappers.h"

#ifdef JETIE_OMEA
#define ACTIONMANAGER_GUID_BASE { 0x35402c01, 0x1777, 0x4159, { 0x9a, 0xba, 0x34, 0x80, 0xba, 0x70, 0xd9, 0x5a } }
#endif
#ifdef JETIE_BEELAXY
#define ACTIONMANAGER_GUID_BASE { 0x35402c02, 0x1777, 0x4159, { 0x9a, 0xba, 0x34, 0x80, 0xba, 0x70, 0xd9, 0x5a } }
#endif

// IActionManager — external, dual interface for the ActionManager
#ifdef JETIE_OMEA
[
	object,
	uuid("2C5AF886-B545-4D3D-82B1-5B1AE2422842"),
	dual,
	helpstring("Internet Explorer Omea Add-on Action Manager Interface"),
	pointer_default(unique)
]
#endif
#ifdef JETIE_BEELAXY
[
	object,
	uuid("2C5AF887-B545-4D3D-82B1-5B1AE2422842"),
	dual,
	helpstring("Internet Explorer Beelaxy Add-on Action Manager Interface"),
	pointer_default(unique)
]
#endif
__interface IActionManager : IDispatch
{
	[id(1), hidden, helpstring("Executes an arbitary action. Invoked by the Internet Explorer context menu commands.")]
	HRESULT ExecuteContextMenuAction([in] VARIANT ActionRef, [in] VARIANT Parameter);
};

// IRawActionManager — internal interface of the Action Manager for intra-dll calls that has no ole dispatch support
#pragma warning(disable: 4096)	// interface is not a COM interface; will not be emitted to IDL
#ifdef JETIE_OMEA
[
	hidden,
//	local,
	uuid("2C5AF888-B545-4D3D-82B1-5B1AE2422842"),
	helpstring("Internet Explorer Omea Add-on Internal Raw Action Manager Interface"),
	pointer_default(unique)
]
#endif
#ifdef JETIE_BEELAXY
[
	hidden,
//	local,
	uuid("2C5AF889-B545-4D3D-82B1-5B1AE2422842"),
	helpstring("Internet Explorer Beelaxy Add-on Internal Raw Action Manager Interface"),
	pointer_default(unique)
]
#endif
__interface IRawActionManager : IUnknown	// See function declarations in the class in order to get the documenting comments
{
	XmlElement GetActions();
	XmlElement GetAction(_bstr_t id);
	XmlElement GetAction2(XmlElement xmlControl);
	XmlElement ControlFromGuid(REFGUID guid);
	XmlElement ControlFromGuid2(_bstr_t bsGuid);
	XmlElement ControlFromEntryID(int nID, XmlElement xmlParent = NULL, bool bDeep = true);
	XmlElement ControlFamilyFromGuid(REFGUID guid, _bstr_t bsControlType);
	void Execute(_bstr_t bsID, _variant_t vtParam);
	void Execute2(XmlElement xmlControl, _variant_t vtParam);
	void QueryStatus(_bstr_t bsID, _variant_t vtParam, DWORD *pdwOleCmdF, CStringW *psTitle, CStringW *psInfoTip, CStringW *psDescription, bool bDynamic);
	void RegisterControls();
	void UnregisterControls();
	CStringW GetStaticInfoTip(_bstr_t bsID);
	CStringW GetStaticInfoTip2(XmlElement xmlControl);
	CStringW GetStaticTitle2(XmlElement xmlControl);
	void Load();
	void Save();
	bool HasText(_bstr_t bsID);
	XmlElement ShowPopupMenu(XmlElement xmlParent, HWND hwndParent, POINT ptScreenCoordinates, _variant_t vtParam);
	XmlElement ShowPopupMenu(XmlNodeList xmlControls, HWND hwndParent, POINT ptScreenCoordinates, _variant_t vtParam);
};

// Emit the interface placeholder into the IDL file
#ifdef JETIE_OMEA
[idl_quote("[object, hidden, local, uuid(\"2C5AF888-B545-4D3D-82B1-5B1AE2422842\")] interface IRawActionManager : IUnknown {};")];
#endif
#ifdef JETIE_BEELAXY
[idl_quote("[object, hidden, local, uuid(\"2C5AF889-B545-4D3D-82B1-5B1AE2422842\")] interface IRawActionManager : IUnknown {};")];
#endif

_COM_SMARTPTR_TYPEDEF(IActionManager, __uuidof(IActionManager));
_COM_SMARTPTR_TYPEDEF(IRawActionManager, __uuidof(IRawActionManager));

// _IActionManagerEvents
#ifdef JETIE_OMEA
[
	dispinterface,
	uuid("F10F7249-69C7-4EA6-A5A1-1949F903EBED"),
	helpstring("Internet Explorer Omea Add-on Action Manager Events Interface")
]
#endif
#ifdef JETIE_BEELAXY
[
	dispinterface,
	uuid("F10F724A-69C7-4EA6-A5A1-1949F903EBED"),
	helpstring("Internet Explorer Beelaxy Add-on Action Manager Events Interface")
]
#endif
__interface _IActionManagerEvents
{
};


// CActionManager

#ifdef JETIE_OMEA
[
	coclass,
	threading("apartment"),
	support_error_info("IActionManager"),
	event_source("com"),
	vi_progid("IexploreOmea.ActionManager"),
	progid("IexploreOmea.ActionManager.1"),
	version(1.0),
	uuid("B544FBB7-D637-44ED-935E-77592B7C33FD"),
	helpstring("Internet Explorer Omea Add-on Action Manager")
]
#endif
#ifdef JETIE_BEELAXY
[
	coclass,
	restricted(IRawActionManager),
	threading("apartment"),
	support_error_info("IActionManager"),
	event_source("com"),
	vi_progid("IexploreBeelaxy.ActionManager"),
	progid("IexploreBeelaxy.ActionManager.1"),
	version(1.0),
	uuid("B544FBB8-D637-44ED-935E-77592B7C33FD"),
	helpstring("Internet Explorer Beelaxy Add-on Action Manager")
]
#endif
class ATL_NO_VTABLE CActionManager :
#ifndef USE_OLE_DISPATCH
	public IActionManager,
#else
	public IDispatchImpl<IActionManager, &__uuidof(IActionManager), &__uuidof(LIBID_IexploreJetPlugin)>,
#endif
	public IRawActionManager
{
public:
	CActionManager();
	virtual ~CActionManager();

	__event __interface _IActionManagerEvents;

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

	/*
	BEGIN_COM_MAP(CActionManager)
		COM_INTERFACE_ENTRY(IRawActionManager)
	END_COM_MAP()
	*/

/// Data
protected:

	/// The list of the defined UI actions along with the information about their command/state handlers and the default UI representation. Loaded on first call to GetUIActions().
	XmlDocument	m_xmlActions;

	/// The list of the UI controls representing the above-mentioned UI actions in different forms (eg main menu, toolbars, context menu). Loaded on first call to GetUIControls().
	XmlDocument	m_xmlControls;

	/// The base GUID for registering the controls in IE dynamically. A range of GUIDs starting from this value is used for registering the controls dynamically.
	GUID	m_guidBase;

	/// Number of GUIDs in the GUID range involved in dynamic control registration.
	int	m_nGuidRange;

	/// Locks access to the data files.
	CMutex	m_mutexDataFilesAccessLock;

	/// A cache for the actions' dispatch handlers to avoid creating a new instance on each access.
	std::map<CStringW, IDispatchPtr>	m_mapDispatchHandlers;

	/// A cached instance of the ActionManager that can be reused within the boundaries of the main thread to decrease the required number of reloading the controls data.
	/// As the whole DLL is used mainly in one thread, this simple sort of caching should make much sense. Sincerely, IE uses it on one thread only. But we should be prepared to other use from other browsers or third-party software.
	static IActionManagerPtr	m_oMainThreadInstance;

	/// ID of the thread for which IActionManagerPtr is a valid pointer.
	static DWORD	m_dwMainThreadID;

/// Implementation
protected:

	/// Generates a GUID from the range and converts it to a string. nShift specifies the item in the range. Throws if out of range.
	CString	StringFromRangeGuid(int nShift) throw(_com_error);

    /// Loads the data files from the disk (or from resources, if absent on the disk).
	/// bLock specifies whether access to the file should be locked. False value usually means that it's locked by the caller for the whole operation.
	void LoadData(bool bLock) throw(_com_error);

	/// Saves the data files to the disk.
	/// bLock specifies whether access to the file should be locked. False value usually means that it's locked by the caller for the whole operation.
	void SaveData(bool bLock) throw(_com_error);

	/// Registers a COM object for the control specified. Tries a per-user registration, then, if it fails, the standard HKCR routine. RegClassID attribute must be specified for the control.
	void RegisterElementClassId(XmlElement xmlControl) throw(_com_error);

	/// Performs the UI controls validation after loading them, especially when it's done for the first time.
	/// Checks for the missing or duplicate entry IDs.
	void ValidateControls();

	/// Gets an action dispatch handler by its class-id (or prog-id), either by creating a new instance or looking up an existing one in the cache.
	IDispatchPtr GetDispatchHandler(CStringW sClassID) throw(_com_error);

/// Operations
public:

	/// Returns an XML element that has all the defined XML actions as its children. The data necessary is loaded on the first access.
	XmlElement GetActions() throw (_com_error);

	/// Returns the XML action identified by the id, or throws an exception if it cannot be found.
	XmlElement GetAction(_bstr_t id) throw (_com_error);

	/// Returns the XML action attached to the control specified, or throws an exception if it cannot be found.
	XmlElement GetAction2(XmlElement xmlControl) throw (_com_error);

	/// Returns the list of UI controls representing the actions on a certain UI element, defined by bsType, for example, "Toolbar" or "Menu". bsName is the name of this control ("Main" for "Toolbar", and "Tools" or "Context" for "Menu"). The data necessary is loaded on the first access.
	XmlElement GetControls(_bstr_t bsType, _bstr_t bsName) throw (_com_error);

	/// Executes the UI action. Passes the browser parameter to it, if needed.
	void Execute(_bstr_t bsID, _variant_t vtParam) throw (_com_error);

	/// Executes the UI action associated with the supplied control. Passes the browser parameter to it, if needed.
	void Execute2(XmlElement xmlControl, _variant_t vtParam) throw (_com_error);

	/// Queries the UI action status. Passes the browser parameter to it, if needed.
	/// bsID	— ID of the action which is being queired for its state.
	/// vtParam	— parameter to be passed to the dynamic status handler.
	/// pdwOleCmdF	— is filled with a combination of the OLECMDF enumeration members that indicate the action state. May be NULL if not needed.
	/// psTitle	— title for the control. May be NULL if not needed.
	/// psInfoTip	— tooltip for the control. May be NULL if not needed.
	/// psDescription	— description for the control. May be NULL if not needed.
	/// bDynamic	— if true, the dispatch handlers are invoked to retrieve the dynamic values, if applicable, otherwise, they're constructed from the static data only.
	void QueryStatus(_bstr_t bsID, _variant_t vtParam, DWORD *pdwOleCmdF, CStringW *psTitle, CStringW *psInfoTip, CStringW *psDescription, bool bDynamic) throw (_com_error);

	/// Retrieves the info tip for an action, either dynamic or static.
	CStringW GetStaticInfoTip(_bstr_t bsID) throw (_com_error);

	/// Retrieves the info tip for an action, either dynamic or static.
	CStringW GetStaticInfoTip2(XmlElement xmlControl) throw (_com_error);

	/// Registers the Internet Explorer UI controls that cannot be added at runtime but have to be specified in the Registry statically, according to the Controls settings.
	void RegisterControls() throw(_com_error);

	/// Undoes the RegisterControls registration by removing all the JETIE controls from IE customization lists and general COM object info for them. Invoked internally from RegisterControls before setting the new data and upon unregistering the JETIE dll.
	void UnregisterControls() throw(_com_error);

	/// Gets the title for a control. If bDynamic is true, the control is queried for the dynamic text. If unsupplied, the text specified in the control is used. If missing, the action's title text is used.
	CStringW GetStaticTitle2(XmlElement xmlControl) throw(_com_error);

	/// Looks up the GUID specified, checks whether it belongs to a known UI control, and, if yes, returns the control. Otherwise, throws an exception.
	XmlElement ControlFromGuid2(_bstr_t bsGuid) throw(_com_error);

	/// Looks up the GUID specified, checks whether it belongs to a known UI control, and, if yes, returns the control. Otherwise, throws an exception.
	XmlElement ControlFromGuid(REFGUID guid) throw(_com_error);

	/// Looks up a control with the EntryID specified in the controls list, starting at the given parent XML element (or root, if it's Null).
	XmlElement ControlFromEntryID(int nID, XmlElement xmlParent = NULL, bool bDeep = true);

	/// Looks up the GUID specified, checks whether it belongs to a known control family of the type specified, and, if yes, returns its element. Otherwise, throws an exception.
	XmlElement ControlFamilyFromGuid(REFGUID guid, _bstr_t bsControlType) throw(_com_error);

	/// Throws a _com_error exception with an HRESULT and IErrorInfo which resolves to the error text specified. May be used by classes that do not have their own IErrorInfo, but wish to issue meaningful error messages. Also traces the error text to the standard debug output.
	void ThrowError(CStringW sError) throw(_com_error);

	/// Throws a _com_error exception with an HRESULT and IErrorInfo which resolves to the error text corresponding to the specified system error, or GetLastError if unspecified, prepended with a clarification message.
	void ThrowSystemError(DWORD dwError = GetLastError(), LPCWSTR szComment = NULL) throw(_com_error);

	/// Returns an existing (cached) or a new instance which is guaranteed to be valid for use in the callee thread.
	static IRawActionManagerPtr GetInstance() throw(_com_error);

	/// Loads the UI actions and controls from the disk or resource storage.
	/// This is highly recommended before saving any changes to disk so that the other changes won't get discarded.
	void Load() throw(_com_error);

	/// Saves the UI actions and controls to the disk storage, thus persisting the customizations done.
	/// It's recommended to do Load immediately before doing the customizations so that the changes possibly done to those files won't get lost.
	void Save() throw(_com_error);

	/// Determines whether a corresponding action's setting tells the controls based on it to show the title text.
	bool HasText(_bstr_t bsID) throw(_com_error);

	/// Shows a cascaded popup menu for all the child controls of the given parent control or element, and executes an action that is selected from the menu.
	/// XmlParent defines an XML element that is the parent for Control elements that should be included into the menu. vtParam is the parameter that is passed upon action execution to the action handler.
	/// Returns the control element that was clicked, or NULL if user cancelled the menu access.
	XmlElement ShowPopupMenu(XmlElement xmlParent, HWND hwndParent, POINT ptScreenCoordinates, _variant_t vtParam) throw(_com_error);

	/// Shows a cascaded popup menu for the given list of controls, and executes an action that is selected from the menu.
	/// XmlParent defines an XML element that is the parent for Control elements that should be included into the menu. vtParam is the parameter that is passed upon action execution to the action handler.
	/// Returns the control element that was clicked, or NULL if user cancelled the menu access.
	XmlElement ShowPopupMenu(XmlNodeList xmlControls, HWND hwndParent, POINT ptScreenCoordinates, _variant_t vtParam) throw(_com_error);

	/// A helper function for internal use from ShowPopupMenu.
	void FillSubmenu(XmlNodeList xmlControls, HMENU menuParent, CPopupMenuHandleArray &arSubmenus, std::map<int, XmlElement> &mapItemActions, _variant_t vtParam);

// Interface
public:
	STDMETHOD(ExecuteContextMenuAction)(VARIANT ActionRef, VARIANT Parameter);
};

#pragma warning(default: 4096)	// interface is not a COM interface; will not be emitted to IDL
