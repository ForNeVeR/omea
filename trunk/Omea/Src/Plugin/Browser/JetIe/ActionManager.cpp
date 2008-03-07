/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// ActionManager.cpp : Implementation of CActionManager
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "JetIe.h"
#include "ActionManager.h"

IActionManagerPtr	CActionManager::m_oMainThreadInstance = NULL;
DWORD	CActionManager::m_dwMainThreadID = NULL;

CActionManager::CActionManager()
: m_mutexDataFilesAccessLock(NULL, FALSE, _T("JetBrains.JetIe.") + CJetIe::LoadStringT(IDS_PLUGIN_NAME) + _T("ActionManager.DataFilesAccessLock"))
{
	// {35402C00-1777-4159-9ABA-3480BA70D95A}
	GUID guidBase = ACTIONMANAGER_GUID_BASE;
	m_guidBase = guidBase;
	m_nGuidRange = 0xFF;
}

CActionManager::~CActionManager()
{
}

// CActionManager
XmlElement CActionManager::GetActions()
{
	// TODO: guard loading/saving by exclusive file access or a mutex

	if(m_xmlActions == NULL)
		LoadData(true);

	return m_xmlActions->selectSingleNode(L"/JetIe/Actions");
}

XmlElement CActionManager::GetAction(_bstr_t bsID)
{
	XmlElement	xmlAction = GetActions()->selectSingleNode(L"Action[@ID='" + bsID + L"']");
	if(xmlAction == NULL)	// Action with such ID was not found
	{
		CStringW	sError;
		sError.Format(CJetIe::LoadString(IDS_E_NOSUCHACTION), (LPCWSTR)bsID);
		ThrowError(sError);
	}

	return xmlAction;
}

XmlElement CActionManager::GetAction2(XmlElement xmlControl)
{
	CStringW	sControlType = xmlControl->baseName;
	if(sControlType == L"Control")
		return GetAction((_bstr_t)xmlControl->getAttribute(L"Action"));
	else if(sControlType == L"DropDownButton")
		return GetAction2(ControlFromEntryID(xmlControl->getAttribute(L"Default"), xmlControl, false));	// Find a control referenced as the default
	else if(sControlType == L"Separator")
		ThrowError(CJetIe::LoadString(IDS_E_INVALIDCONTROLOPERATION));
	else
		ThrowError(CJetIe::LoadString(IDS_E_UNKNOWNCONTROLTYPE));

	return NULL;	// Dummy return for the implicit Throw operations
}

XmlElement CActionManager::GetControls(_bstr_t bsType, _bstr_t bsName)
{
	// TODO: guard loading/saving by exclusive file access or a mutex

	if(m_xmlControls == NULL)
		LoadData(true);

	XmlElement	xmlRet = m_xmlControls->selectSingleNode((_bstr_t)L"/JetIe/Controls[@Type='" + bsType + L"' and @Name='" + bsName + L"']");

	if(xmlRet == NULL)
	{
		CStringW	sError;
		sError.Format(L"Cannot locate a definiton for the set of controls of type %s named %s.", (LPCWSTR)bsType, (LPCWSTR)bsName);
		ThrowError(sError);
	}

	return xmlRet;
}

void CActionManager::Execute(_bstr_t bsID, _variant_t vtParam)
{
	XmlElement	xmlAction = GetAction(bsID);

	XmlElement	xmlExec = xmlAction->selectSingleNode(L"Exec");
	if(xmlExec == NULL)
	{
		TRACE(L"Warning: execution handler not defined for \"%s\" action.", (LPCWSTR)bsID);
		return;
	}

	// Query the action status when trying to execute the action, and avoid execution if it is disabled or hidden
	DWORD	dwOleCmdf;
	QueryStatus(bsID, vtParam, &dwOleCmdf, NULL, NULL, NULL, true);
	if((!(dwOleCmdf & OLECMDF_SUPPORTED)) || (!(dwOleCmdf & OLECMDF_ENABLED)))
	{
		TRACE(L"Warning: trying to execute a disabled or hidden (%#03X) action \"%s\".", dwOleCmdf, (LPCWSTR)bsID);
		return;
	}

	// Look for the available handlers among all the elements under the QueryStatus element
	XmlNodeList	xmlHandlers = xmlExec->selectNodes(L"*");
	XmlElement	xmlHandler;
	CStringW	sBaseName;
	while((xmlHandler = xmlHandlers->nextNode()) != NULL)
	{
		try
		{
			sBaseName = (LPCWSTR)xmlHandler->baseName;

			// Check the handler type
			if(sBaseName == L"DispatchHandler")
			{
				// Create an instance of the UI action dispatch handler object
				IDispatchPtr	oHandler = GetDispatchHandler((LPCWSTR)(_bstr_t)xmlHandler->getAttribute(L"ClassID"));

				// Try to execute
				DISPID dispid;
				_variant_t avtParams[] = { vtParam };
				DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };

				CStringW	sMember = (LPCWSTR)(_bstr_t)xmlHandler->getAttribute(L"Method");	// This buffer holds the string while it's in use
				OLECHAR	FAR	*szMember = (LPWSTR)(LPCWSTR)sMember;
				COM_CHECK(oHandler, GetIDsOfNames(IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
				COM_CHECK(oHandler, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));
				// We're not interested in the return value here			
			}
			else
				TRACE(L"Unknown or unspecified type of handler for \"%s\" action exec.", (LPCWSTR)bsID);
		}
		catch(_com_error e)	// Do not fail the whole process in case one of the handlers fails
		{
			COM_TRACE();
			ASSERT(FALSE && "Failed to execute the action's Dispatch Handler.");
		}
	}
}

void CActionManager::Execute2(XmlElement xmlControl, _variant_t vtParam)
{
	Execute((_bstr_t)xmlControl->getAttribute(L"Action"), vtParam);
}

void CActionManager::QueryStatus(_bstr_t bsID, _variant_t vtParam, DWORD *pdwOleCmdF, CStringW *psTitle, CStringW *psInfoTip, CStringW *psDescription, bool bDynamic)
{
	// Retrieve the action of question
	XmlElement	xmlAction = GetAction(bsID);

	// Hidden, disabled by default
	if(pdwOleCmdF != NULL)
		*pdwOleCmdF = 0;

	//////////////////////////////////
	// Start with the static values
	XmlNode	xmlNode;

	// Title
	if(psTitle != NULL)
	{
		psTitle->Empty();
		if((xmlNode = xmlAction->selectSingleNode(L"Title/@Text")) != NULL)
			*psTitle = (LPCWSTR)(_bstr_t)xmlNode->nodeValue;
	}

	// InfoTip
	if(psInfoTip != NULL)
	{
		psInfoTip->Empty();
		if((xmlNode = xmlAction->selectSingleNode(L"Title/@InfoTip")) != NULL)
			*psInfoTip = (LPCWSTR)(_bstr_t)xmlNode->nodeValue;
	}

	// Description
	if(psDescription != NULL)
	{
		psDescription->Empty();
		if((xmlNode = xmlAction->selectSingleNode(L"Title/@Description")) != NULL)
			*psDescription = (LPCWSTR)(_bstr_t)xmlNode->nodeValue;
	}

	////////////////////////////////////////////////////////////////////////
	// Static stage is off, if we don't need any dynamic info, we may exit
	if(!bDynamic)
		return;

	/////////////////////////////////////////////////
	// Dynamic stage: locate and query the handlers

	XmlElement	xmlQueryStatus = xmlAction->selectSingleNode(L"QueryStatus");
	if(xmlQueryStatus == NULL)	// No handlers, act as if in the static case
	{
		TRACE(L"Warning: query-status handler not defined for \"%s\" action.", (LPCWSTR)bsID);
		return;
	}

	// Look for the available handlers among all the elements under the QueryStatus element
	XmlNodeList	xmlHandlers = xmlQueryStatus->selectNodes(L"*");
	XmlElement	xmlHandler;
	CStringW	sBaseName;
	while((xmlHandler = xmlHandlers->nextNode()) != NULL)
	{
		try
		{
			sBaseName = (LPCWSTR)xmlHandler->baseName;
			if(sBaseName == L"Constant")	// A constant handler that defines constant state information
			{
				if(pdwOleCmdF != NULL)
				{
					if((long)xmlHandler->getAttribute(L"Visible"))
						*pdwOleCmdF |= OLECMDF_SUPPORTED;
					if((long)xmlHandler->getAttribute(L"Enabled"))
						*pdwOleCmdF |= OLECMDF_ENABLED;
					if((long)xmlHandler->getAttribute(L"Checked"))
						*pdwOleCmdF |= OLECMDF_LATCHED;
				}
			}
			else if(sBaseName == L"DispatchHandler")	// A simple dispatch handler (for the OleCmdF constant)
			{
				if(pdwOleCmdF != NULL)
				{
					// Create an instance of the UI action dispatch handler object
					IDispatchPtr	oHandler = GetDispatchHandler((LPCWSTR)(_bstr_t)xmlHandler->getAttribute(L"ClassID"));

					// Prepare the parameters
					long	nOleCmdF = (long)*pdwOleCmdF;
					_variant_t	vtOleCmdF;
					V_VT(&vtOleCmdF) = VT_I4 | VT_BYREF;
					V_I4REF(&vtOleCmdF) = &nOleCmdF;

					// Try to execute
					DISPID dispid;
					_variant_t avtParams[] = { vtOleCmdF, vtParam };
					DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };

					CStringW	sMember = (LPCWSTR)(_bstr_t)xmlHandler->getAttribute(L"Method");	// This buffer holds the string while it's in use
					OLECHAR	FAR	*szMember = (LPWSTR)(LPCWSTR)sMember;
					COM_CHECK(oHandler, GetIDsOfNames(IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
					COM_CHECK(oHandler, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));

					// Process the updated values
					*pdwOleCmdF = (DWORD)nOleCmdF;
				}
			}
			else if(sBaseName == L"DispatchHandlerEx")	// An extended dispatch handler (for the state and text strings)
			{
				if((pdwOleCmdF != NULL) || (psTitle != NULL) || (psInfoTip != NULL) || (psDescription != NULL))
				{
					// Create an instance of the UI action dispatch handler object
					IDispatchPtr	oHandler = GetDispatchHandler((LPCWSTR)(_bstr_t)xmlHandler->getAttribute(L"ClassID"));

					///////////////////////////
					// Prepare the parameters

					// OleCmdF
					long	nOleCmdF;
					_variant_t	vtOleCmdF;
					V_VT(&vtOleCmdF) = VT_I4 | VT_BYREF;
					if(pdwOleCmdF != NULL)
					{
						nOleCmdF = (long)*pdwOleCmdF;
						V_I4REF(&vtOleCmdF) = &nOleCmdF;
					}
					else
						V_I4REF(&vtOleCmdF) = NULL;

					// Title
					_bstr_t	bsTitle;
					_variant_t	vtTitle;
					V_VT(&vtTitle) = VT_BSTR | VT_BYREF;
					if(psTitle != NULL)
					{
						bsTitle = (LPCWSTR)*psTitle;
						V_BSTRREF(&vtTitle) = &bsTitle.GetBSTR();
					}
					else
						V_BSTRREF(&vtTitle) = NULL;

					// InfoTip
					_bstr_t	bsInfoTip;
					_variant_t	vtInfoTip;
					V_VT(&vtInfoTip) = VT_BSTR | VT_BYREF;
					if(psInfoTip != NULL)
					{
						bsInfoTip = (LPCWSTR)*psInfoTip;
						V_BSTRREF(&vtInfoTip) = &bsInfoTip.GetBSTR();
					}
					else
						V_BSTRREF(&vtInfoTip) = NULL;

					// Description
					_bstr_t	bsDescription;
					_variant_t	vtDescription;
					V_VT(&vtDescription) = VT_BSTR | VT_BYREF;
					if(psDescription != NULL)
					{
						bsDescription = (LPCWSTR)*psDescription;
						V_BSTRREF(&vtDescription) = &bsDescription.GetBSTR();
					}
					else
						V_BSTRREF(&vtDescription) = NULL;

					// Try to execute
					DISPID dispid;
					_variant_t avtParams[] = { vtDescription, vtInfoTip, vtTitle, vtOleCmdF, vtParam };
					DISPPARAMS dispparams = { avtParams, NULL, sizeof(avtParams) / sizeof(*avtParams), 0 };

					CStringW	sMember = (LPCWSTR)(_bstr_t)xmlHandler->getAttribute(L"Method");	// This buffer holds the string while it's in use
					OLECHAR	FAR	*szMember = (LPWSTR)(LPCWSTR)sMember;
					COM_CHECK(oHandler, GetIDsOfNames(IID_NULL, &szMember, 1, LOCALE_USER_DEFAULT, &dispid));
					COM_CHECK(oHandler, Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, NULL, NULL));

					// Process the updated values
					if(pdwOleCmdF != NULL)
						*pdwOleCmdF = (DWORD)nOleCmdF;
					if(psTitle != NULL)
						*psTitle = (LPCWSTR)bsTitle;
					if(psInfoTip != NULL)
						*psInfoTip  =(LPCWSTR)bsInfoTip;
					if(psDescription != NULL)
						*psDescription = (LPCWSTR)bsDescription;
				}
			}
			else
				TRACE(L"Unknown or unspecified type of handler for \"%s\" action query status.", (LPCWSTR)bsID);
		}
		catch(_com_error e)	// Do not fail the whole process in case one of the handlers fails
		{
			COM_TRACE();
			ASSERT(FALSE && "Failed to query the action status.");
		}
	}
}

CStringW CActionManager::GetStaticInfoTip(_bstr_t bsID)
{
	CStringW	sInfoTip;
	QueryStatus(bsID, vtMissing, NULL, NULL, &sInfoTip, NULL, false);
	return sInfoTip;
}

CStringW CActionManager::GetStaticInfoTip2(XmlElement xmlControl)
{
	return GetStaticInfoTip((_bstr_t)xmlControl->getAttribute(L"Action"));
}

void CActionManager::RegisterControls()
{
	// Registering the controls involves updating some of the additional data in the controls set. That's why it has to be reloaded and locked until saved.
	CMutexLock(m_mutexDataFilesAccessLock, true);

	// First, erase all the old registration data.
	UnregisterControls();
	TRACE(L"ActionManager is registering the controls.");

	// Make the configuration up-to-date
	LoadData(false);	// Already locked

	CString	sGuid;	// An LPCTSTR textual GUID rep
	int	nNextGuid = 0;	// Shift for the next GUID

	XmlElement	xmlControls, xmlControl, xmlAction;
	XmlNodeList	xmlItems;
	CRegKey	rkMain;

	//////////////////////////////////////////////////
	// Remove the now-outdated RegClassID attributes
	try
	{
		xmlItems = m_xmlControls->selectNodes(L"//Control[string(@RegClassID) != '']");

		TRACE(L"ActionManager is erasing %d old RegClassID attributes.", xmlItems->length);

		while((xmlControl = xmlItems->nextNode()) != NULL)
			xmlControl->removeAttribute(L"RegClassID");
	}
	COM_CATCH();	// Not a fatal error, though

	/////////////////////
	// Tools Menu Items
	try
	{
		xmlControls = GetControls(L"Menu", L"Tools");
		xmlItems = xmlControls->selectNodes(L"Control");

		while((xmlControl = xmlItems->nextNode()) != NULL)
		{
			xmlAction = GetAction((_bstr_t)xmlControl->getAttribute(L"Action"));	// Action for this control
			sGuid = StringFromRangeGuid(nNextGuid++);	// Generate GUID for the COM object that will handle the control events
			xmlControl->setAttribute(L"RegClassID", (_bstr_t)(LPCTSTR)sGuid);	// Store

			// Register the extension
			rkMain.Create(HKEY_CURRENT_USER, _T("SOFTWARE\\Microsoft\\Internet Explorer\\Extensions\\") + StringFromRangeGuid(nNextGuid++));	// Here we use just some generic extension's GUID
			rkMain.SetStringValue(_T("MenuCustomize"), _T(""));
			rkMain.SetStringValue(_T("CLSID"), _T("{1FBA04EE-3024-11d2-8F1F-0000F87ABD16}"));	// CLSID of the type of extension, not our control one. Points to %SystemRoot%\system32\shdocvw.dll
			rkMain.SetStringValue(_T("MenuText"), CW2T((LPCWSTR)GetStaticTitle2(xmlControl)));	// Do not register the current dynamic value
			rkMain.SetStringValue(_T("MenuStatusBar"), CW2T((LPCWSTR)GetStaticInfoTip2(xmlControl)));
			rkMain.SetStringValue(_T("ClsidExtension"), sGuid);	// This is the GUID of the handler
			rkMain.Close();

			// Register the handler object
			RegisterElementClassId(xmlControl);
		}
	}
	COM_CATCH();

	////////////////////////////
	// IE Main Toolbar Buttons
	try
	{
		xmlControls = GetControls(L"Toolbar", L"Main");
		xmlItems = xmlControls->selectNodes(L"Control");
		CString	sIcon;

		while((xmlControl = xmlItems->nextNode()) != NULL)
		{
			xmlAction = GetAction((_bstr_t)xmlControl->getAttribute(L"Action"));	// Action for this control
			sGuid = StringFromRangeGuid(nNextGuid++);	// Generate GUID for the COM object that will handle the control events
			xmlControl->setAttribute(L"RegClassID", (_bstr_t)(LPCTSTR)sGuid);	// Store

			// Register the extension
			rkMain.Create(HKEY_CURRENT_USER, _T("SOFTWARE\\Microsoft\\Internet Explorer\\Extensions\\") + StringFromRangeGuid(nNextGuid++));	// Here we use just some generic extension's GUID

			rkMain.SetStringValue(_T("CLSID"), _T("{1FBA04EE-3024-11d2-8F1F-0000F87ABD16}"));	// CLSID of the type of extension, not our control one. Points to %SystemRoot%\system32\shdocvw.dll
			rkMain.SetStringValue(_T("ClsidExtension"), sGuid);	// This is the GUID of the handler
			try{
				_variant_t	vtValue = xmlControl->getAttribute(L"DefaultVisible");
				if((V_VT(&vtValue) != VT_NULL) && ((_bstr_t)vtValue == (_bstr_t)L"1"))
					rkMain.SetStringValue(_T("Default Visible"), _T("Yes"));
			}COM_CATCH();
			rkMain.SetStringValue(_T("ButtonText"), CW2T((LPCWSTR)GetStaticTitle2(xmlControl)));	// Do not register the current dynamic value

			// Normal icon
			if(xmlAction->selectSingleNode(L"Image/@Normal") != NULL)
			{
				sIcon = (LPCTSTR)(_bstr_t)xmlAction->selectSingleNode(L"Image/@Normal")->nodeValue;
				sIcon.Replace(_T("%JETIE%"), CJetIe::GetModuleFileName());	// Substitute with the real file name
				rkMain.SetStringValue(_T("Icon"), sIcon);
			}
			// Hot icon
			if(xmlAction->selectSingleNode(L"Image/@Hot") != NULL)
			{
				sIcon = (LPCTSTR)(_bstr_t)xmlAction->selectSingleNode(L"Image/@Hot")->nodeValue;
				sIcon.Replace(_T("%JETIE%"), CJetIe::GetModuleFileName());	// Substitute with the real file name
				rkMain.SetStringValue(_T("HotIcon"), sIcon);
			}
			rkMain.Close();

			// Register the handler object
			RegisterElementClassId(xmlControl);
		}
	}
	COM_CATCH();

	/////////////////////
	// IE Context Menu
	try
	{
		xmlControls = GetControls(L"ContextMenu", L"Main");
		xmlItems = xmlControls->selectNodes(L"Control");
		CString	sURI;
		DWORD	dwContext;

		int	nContextMenuItem = 1000;	// Index of the slot, up to 16 slots available

		while((xmlControl = xmlItems->nextNode()) != NULL)
		{
			xmlAction = GetAction((_bstr_t)xmlControl->getAttribute(L"Action"));	// Action for this control
			if(nContextMenuItem >= 1016)
				ThrowError(L"An attempt was made to generate a context menu item ID out of available range. There are too many context menu items.");

			xmlControl->setAttribute(L"RegClassID", (_variant_t)nContextMenuItem);	// Generate & Store

			// Prepare parameters
			sURI.Format(_T("res://") + CJetIe::GetModuleFileName() + _T("/%d"), nContextMenuItem);
			dwContext = 0;
			if((long)xmlControl->getAttribute(L"OnDefault"))
				dwContext |= (0x1 << CONTEXT_MENU_DEFAULT);
			if((long)xmlControl->getAttribute(L"OnImages"))
				dwContext |= (0x1 << CONTEXT_MENU_IMAGE);
			if((long)xmlControl->getAttribute(L"OnControls"))
				dwContext |= (0x1 << CONTEXT_MENU_CONTROL);
			if((long)xmlControl->getAttribute(L"OnTables"))
				dwContext |= (0x1 << CONTEXT_MENU_TABLE);
			if((long)xmlControl->getAttribute(L"OnTextSelection"))
				dwContext |= (0x1 << CONTEXT_MENU_TEXTSELECT);
			if((long)xmlControl->getAttribute(L"OnAnchor"))
				dwContext |= (0x1 << CONTEXT_MENU_ANCHOR);
			if((long)xmlControl->getAttribute(L"OnUnknown"))
				dwContext |= (0x1 << CONTEXT_MENU_UNKNOWN);

			// Register the MenuExt
			rkMain.Create(HKEY_CURRENT_USER, (CString)_T("SOFTWARE\\Microsoft\\Internet Explorer\\MenuExt\\") + (LPCTSTR)CW2T((LPCWSTR)GetStaticTitle2(xmlControl)));	// Menu items are identified by titles
			rkMain.SetStringValue(NULL, sURI);	// Script that executes the action
			rkMain.SetDWORDValue(_T("Contexts"), dwContext);
			rkMain.SetDWORDValue(_T("Flags"), 0);
			rkMain.SetDWORDValue(CJetIe::LoadStringT(IDS_PLUGIN_NAME), 1);	// A flag that allows to identify this item upon unregistering

			rkMain.Close();

			nContextMenuItem++;
		}
	}
	COM_CATCH();

	/////////////////////
	// Custom Toolbars
	try
	{
		xmlItems = m_xmlControls->selectNodes(L"/JetIe/Controls[@Type='Toolbar' and @Name!='Main']");
		XmlElement	xmlToolbar;

		while((xmlToolbar = xmlItems->nextNode()) != NULL)
		{
			sGuid = StringFromRangeGuid(nNextGuid++);	// Generate GUID for the COM object that will handle the toolbar
			xmlToolbar->setAttribute(L"RegClassID", (_bstr_t)(LPCTSTR)sGuid);	// Store

			// Register the extension
			rkMain.Create(HKEY_LOCAL_MACHINE, _T("SOFTWARE\\Microsoft\\Internet Explorer\\Toolbar"));	// TODO: HKLM or HKCU?
			rkMain.SetDWORDValue(sGuid, 1);
			//rkMain.SetBinaryValue(sGuid, &"", 0);
			rkMain.Close();

			// Register the handler object
			RegisterElementClassId(xmlToolbar);
		}
	}
	COM_CATCH();

	////////////////////////
	// Flush the IE Caches
	TRACE(L"Flushing the Internet Explorer caches.");
	CRegKey	rkHKCR(HKEY_CLASSES_ROOT);
	CRegKey	rkHKCU(HKEY_CURRENT_USER);
	CRegKey	rkHKLM(HKEY_LOCAL_MACHINE);
	rkHKCR.RecurseDeleteKey(_T("Component Categories\\{00021492-0000-0000-C000-000000000046}\\Enum"));
	rkHKCU.RecurseDeleteKey(_T("Software\\Microsoft\\Windows\\Current Version\\Explorer\\Discardable\\PostSetup\\Component Categories\\{00021493-0000-0000-C000-000000000046}\\Enum"));
	rkHKLM.RecurseDeleteKey(_T("Software\\Microsoft\\Windows\\Current Version\\Explorer\\Discardable\\PostSetup\\Component Categories\\{00021493-0000-0000-C000-000000000046}\\Enum"));
	rkHKCU.RecurseDeleteKey(_T("Software\\Microsoft\\Windows\\Current Version\\Explorer\\Discardable\\PostSetup\\Component Categories\\{00021494-0000-0000-C000-000000000046}\\Enum"));
	rkHKLM.RecurseDeleteKey(_T("Software\\Microsoft\\Windows\\Current Version\\Explorer\\Discardable\\PostSetup\\Component Categories\\{00021494-0000-0000-C000-000000000046}\\Enum"));

	TRACE(L"ActionManager has completed registering the controls.");

	SaveData(false);	// Already locked
}

void CActionManager::UnregisterControls()
{
	TRACE(L"ActionManager is unregistering the controls.");

	/////////////////////////////
	// Remove Registry records

	CString	sGuid;	// An LPCTSTR textual GUID rep
	int	nCleaned = 0;	// Number of erased keys

	CRegKey	rkHKCR(HKEY_CLASSES_ROOT);
	CRegKey	rkHKCU(HKEY_CURRENT_USER);

	CRegKey	rkUserToolbars;
	rkUserToolbars.Open(HKEY_CURRENT_USER, _T("Software\\Microsoft\\Internet Explorer\\Toolbar"));
	CRegKey	rkMachineToolbars;
	rkMachineToolbars.Open(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\Internet Explorer\\Toolbar"));

	// Extenstions and objects that use CLSIDs
	for(int a = 0; a < m_nGuidRange; a++)
	{
		// Construct the next GUID and its string rep
		sGuid = StringFromRangeGuid(a);

		// COM object registration in HKCU/Software/Classes (WinNT case)
		nCleaned += (rkHKCU.RecurseDeleteKey(_T("Software\\Classes\\CLSID\\") + sGuid) == ERROR_SUCCESS);

		// COM object registration in HKCU/Software/Classes (Win9x case)
		nCleaned += (rkHKCR.RecurseDeleteKey(_T("CLSID\\") + sGuid) == ERROR_SUCCESS);

		// Toolbars
		nCleaned += (rkUserToolbars.DeleteValue(sGuid) == ERROR_SUCCESS);
		nCleaned += (rkMachineToolbars.DeleteValue(sGuid) == ERROR_SUCCESS);	// TODO: remove one of these
		
		// IE Toolbar Buttons, IE Tools Menu Items, etc
		nCleaned += (rkHKCU.RecurseDeleteKey(_T("Software\\Microsoft\\Internet Explorer\\Extensions\\") + sGuid) == ERROR_SUCCESS);

		// IE Extenstions Cache
		nCleaned += (rkHKCU.RecurseDeleteKey(_T("Software\\Microsoft\\Windows\\CurrentVersion\\Ext\\Stats\\") + sGuid) == ERROR_SUCCESS);
	}

	// Context menu items
	CRegKey	rkMenuExtRoot, rkMenuExt;
	CString	sPluginName = CJetIe::LoadStringT(IDS_PLUGIN_NAME);
	rkMenuExtRoot.Create(HKEY_CURRENT_USER, _T("SOFTWARE\\Microsoft\\Internet Explorer\\MenuExt"));
	TCHAR	szMenuExtName[0x400];
	DWORD	dwNameLen = sizeof(szMenuExtName)/sizeof(*szMenuExtName);
	DWORD	dwValue;
	for(	// Enumerate the subkeys
		int a = 0, nIndex = 0;	// Seed
		(a < 0x1000) && (rkMenuExtRoot.EnumKey(nIndex, szMenuExtName, &dwNameLen) != ERROR_NO_MORE_ITEMS);	// Try getting the next item, plus 0x1000 for a safeguard
		a++, dwNameLen = sizeof(szMenuExtName)/sizeof(*szMenuExtName))	// Refresh the name length
	{
		if(rkMenuExt.Open(rkMenuExtRoot, szMenuExtName) == ERROR_SUCCESS)	// Check if it was created by us
		{
			if((rkMenuExt.QueryDWORDValue(sPluginName, dwValue) == ERROR_SUCCESS) && (dwValue))	// Yes, it was created by us
			{
				rkMenuExt.Close();	// Close first
				nCleaned += (rkMenuExtRoot.RecurseDeleteKey(szMenuExtName) == ERROR_SUCCESS);	// Delete
			}
			else
			{
				rkMenuExt.Close();	// Just close, it's not ours
				nIndex++;	// Not increased in case of a successful search
			}
		}
	}
	rkMenuExtRoot.Close();

	// Delete the actions and controls files
	DeleteFile(CJetIe::GetDataFolderPathName(FALSE) + _T("\\UIActions.xml"));
	DeleteFile(CJetIe::GetDataFolderPathName(FALSE) + _T("\\UIControls.xml"));

	TRACE(L"ActionManager has completed unregistering the controls by erasing %d Registry records.", nCleaned);
}

CString	CActionManager::StringFromRangeGuid(int nShift)
{
	ASSERT(nShift >= 0);
	ASSERT(nShift < 0xFF);
	if(nShift >= 0xFF)
		ThrowError(L"An attempt was made to generate a control GUID out of available range.");

	GUID	guid;
	OLECHAR	szwGuid[0x100];	// An OLESTR buffer for the textual GUID rep

	guid = m_guidBase;
	guid.Data4[7] = nShift;
	StringFromGUID2(guid, szwGuid, 0x100);
	return (LPCTSTR)(_bstr_t)szwGuid;
}

void CActionManager::LoadData(bool bLock)
{
	CMutexLock(m_mutexDataFilesAccessLock, bLock);
#ifdef _TRACE
	DWORD	dwTicks = GetTickCount();
#endif

	bool	bLoaded;
	CString	sFileName;

	// TODO: ensure the Default property is set for the drop-down-button controls
	// TODO: ensure the EntryIDs are set and do not duplicate

	///////////////////
	// Actions
	bLoaded = false;

	// Try loading from the disk
	sFileName = CJetIe::GetDataFolderPathName(FALSE) + _T("\\UIActions.xml");
	if(PathFileExists(sFileName))
	{
		try
		{
			m_xmlActions = CJetIe::CreateXmlDocument();
			//m_xmlActions->setProperty(L"SelectionLanguage", L"XPath"); 
			m_xmlActions->load((_bstr_t)(LPCTSTR)sFileName);	// TODO: validation
			if(m_xmlActions->parseError->errorCode == 0)
			{
				bLoaded = true;
				TRACE(L"Loaded the UI actions set from \"%s\".", ToW(sFileName));
			}
		}
		COM_CATCH();
	}

	// Try using the default copy
	if(!bLoaded)
	{
		try
		{
			m_xmlActions = CJetIe::CreateXmlDocument();
			//m_xmlActions->setProperty(L"SelectionLanguage", L"XPath"); 
			CJetIe::SerializeResource(RT_HTML, _T("UIActions.xml"), m_xmlActions, false);	// TODO: validation
			if(m_xmlActions->parseError->errorCode == 0)
			{
				bLoaded = true;
				TRACE(L"Loaded the UI actions set from the resources.");
			}
		}
		COM_CATCH();
	}

	// Fail if not there
	if(!bLoaded)
		ThrowError(CJetIe::LoadString(IDS_E_CANNOTLOADACTIONS));

	///////////////////
	// Controls
	bLoaded = false;

	// Try loading from the disk
	sFileName = CJetIe::GetDataFolderPathName(FALSE) + _T("\\UIControls.xml");
	if(PathFileExists(sFileName))
	{
		try
		{
			m_xmlControls = CJetIe::CreateXmlDocument();
			//m_xmlActions->setProperty(L"SelectionLanguage", L"XPath"); 
			m_xmlControls->load((_bstr_t)(LPCTSTR)sFileName);	// TODO: validation
			if(m_xmlControls->parseError->errorCode == 0)
			{
				bLoaded = true;
				TRACE(L"Loaded the UI controls set from \"%s\".", ToW(sFileName));
			}
		}
		COM_CATCH();
	}

	// Try using the default copy
	if(!bLoaded)
	{
		try
		{
			m_xmlControls = CJetIe::CreateXmlDocument();
			//m_xmlActions->setProperty(L"SelectionLanguage", L"XPath"); 
			CJetIe::SerializeResource(RT_HTML, _T("UIControls.xml"), m_xmlControls, false);	// TODO: validation
			if(m_xmlControls->parseError->errorCode == 0)
			{
				bLoaded = true;
				TRACE(L"Loaded the UI controls set from the resources.");
			}

			// As we're loading the UI controls set from the resources, it's the first time we're accessing it
			// Do some housekeeping like assigning the missing entry-ids
			ValidateControls();
		}
		COM_CATCH();
	}

	// Fail if not there
	if(!bLoaded)
		ThrowError(CJetIe::LoadString(IDS_E_CANNOTLOADCOMTROLS));

#ifdef _TRACE
	TRACE(L"Action Manager has loaded the UI Actions and Controls in %f sec.", (float)(GetTickCount() - dwTicks) / 1000);
#endif
}

void CActionManager::SaveData(bool bLock)
{
	CMutexLock(m_mutexDataFilesAccessLock, bLock);
	
	CString	sFileName;

	///////////////////
	// Actions

	sFileName = CJetIe::GetDataFolderPathName(FALSE) + _T("\\UIActions.xml");
	try
	{
		m_xmlActions->save((_bstr_t)(LPCTSTR)sFileName);
	}
	catch(_com_error e)
	{
		CStringW	sError;
		sError.Format(L"Cannot save the UI Actions set to \"%s\". %s", ToW(sFileName), COM_REASON(e));
		COM_TRACE();
		ThrowError(sError);
	}

	///////////////////
	// Controls

	sFileName = CJetIe::GetDataFolderPathName(FALSE) + _T("\\UIControls.xml");
	try
	{
		m_xmlControls->save((_bstr_t)(LPCTSTR)sFileName);
	}
	catch(_com_error e)
	{
		CStringW	sError;
		sError.Format(L"Cannot save the UI Controls set to \"%s\". %s", ToW(sFileName), COM_REASON(e));
		COM_TRACE();
		ThrowError(sError);
	}
}

CStringW CActionManager::GetStaticTitle2(XmlElement xmlControl)
{
	// TODO: Implement the Dynamic case!
	// TODO: Implement taking the Control's title override

	CStringW	sTitle;
	QueryStatus((_bstr_t)xmlControl->getAttribute(L"Action"), vtMissing, NULL, &sTitle, NULL, NULL, false);
	return sTitle;
}

void CActionManager::RegisterElementClassId(XmlElement xmlControl)
{
	CRegKey	rkClsid;

	// First, try for the current user (works best for NT) 
	// TODO: check the OS type!
	if((CJetIe::IsWinNT()) && (rkClsid.Create(HKEY_CURRENT_USER, _T("Software\\Classes\\CLSID\\" + (CString)(LPCTSTR)(_bstr_t)xmlControl->getAttribute(L"RegClassID"))) != ERROR_SUCCESS))
		ThrowError(L"Cannot write object registration into Registry, HKCU key on WinNT.");
	if((!CJetIe::IsWinNT()) && (rkClsid.Create(HKEY_CLASSES_ROOT, _T("CLSID\\") + (CString)(LPCTSTR)(_bstr_t)xmlControl->getAttribute(L"RegClassID")) != ERROR_SUCCESS))
		ThrowError(L"Cannot write object registration into Registry, HKCR key on Win98.");

	if(xmlControl->baseName == (_bstr_t)L"Control")
		rkClsid.SetStringValue(NULL, (CString)_T("JetIe ") + (LPCTSTR)CW2T((LPCWSTR)GetStaticTitle2(xmlControl)) + _T(" UI Control Hanlder"));	// This is not quite useful, just for instance
	else if(xmlControl->baseName == (_bstr_t)L"Controls")
		rkClsid.SetStringValue(NULL, (LPCTSTR)(_bstr_t)xmlControl->getAttribute(L"Title"));	// Title is retrieved from here, so just post it
	else
		rkClsid.SetStringValue(NULL, _T("JetIe UI Hanlder"));

	CRegKey	rkInprocServer;
	rkInprocServer.Create(rkClsid, _T("InprocServer32"));
	rkInprocServer.SetStringValue(NULL, CJetIe::GetModuleFileName());
	rkInprocServer.SetStringValue(_T("ThreadingModel"), _T("Apartment"));

	rkInprocServer.Close();
	rkClsid.Close();
}

XmlElement CActionManager::ControlFromGuid2(_bstr_t bsGuid)
{
	if(m_xmlControls == NULL)
		LoadData(true);

	// Just look for a Control element with such a reference
	XmlElement	xmlControl = m_xmlControls->selectSingleNode(L"//Control[@RegClassID = '" + bsGuid + L"']");

	// Check for a failure
	if(xmlControl == NULL)
	{
		CStringW	sError;
		sError.Format(CJetIe::LoadString(IDS_E_NOCONTROLGUID), (LPCWSTR)bsGuid);
		ThrowError(sError);
	}

	return xmlControl;
}

XmlElement CActionManager::ControlFromGuid(REFGUID guid)
{
	OLECHAR	szwGuid[0x100];	// An OLESTR buffer for the textual GUID rep
	StringFromGUID2(guid, szwGuid, 0x100);
	return ControlFromGuid2((_bstr_t)szwGuid);
}

XmlElement CActionManager::ControlFromEntryID(int nID, XmlElement xmlParent /*= NULL*/, bool bDeep /*= true*/)
{
	if(m_xmlControls == NULL)
		LoadData(true);

	if(xmlParent == NULL)	// If parent is not set, lookup all the controls
		xmlParent = m_xmlControls;

	XmlElement	xmlRet;
	if(bDeep)	// Recurse to sub-trees
		xmlRet = xmlParent->selectSingleNode((_bstr_t)L".//*[@EntryID='" + (_bstr_t)(_variant_t)nID + L"']");
	else	// No recurse, lookup immediate children only
		xmlRet = xmlParent->selectSingleNode((_bstr_t)L"*[@EntryID='" + (_bstr_t)(_variant_t)nID + L"']");

	if(xmlRet == NULL)	// If a control is not found, report an error (do not return NULL as it will most probably cause a mute "E_POINTER" exception
	{
		CStringW	sError;
		sError.Format(CJetIe::LoadString(IDS_E_NOCONTROLENTRYID), nID);
		ThrowError(sError);
	}

	return xmlRet;
}

XmlElement CActionManager::ControlFamilyFromGuid(REFGUID guid, _bstr_t bsControlType)
{
	// GUID to string
	OLECHAR	szwGuid[0x100];	// An OLESTR buffer for the textual GUID rep
	StringFromGUID2(guid, szwGuid, 0x100);

	if(m_xmlControls == NULL)
		LoadData(true);

	// Just look for a Controls element with such a type and reference
	XmlElement	xmlControls = m_xmlControls->selectSingleNode(L"/JetIe/Controls[@Type='" + bsControlType + L"' and @RegClassID = '" + (_bstr_t)szwGuid + L"']");

	// Check for a failure
	if(xmlControls == NULL)
	{
		CStringW	sError;
		sError.Format(L"There is no control family of type \"%s\" registered as %s.", (LPCWSTR)bsControlType, szwGuid);
		ThrowError(sError);
	}

	return xmlControls;
}

void CActionManager::ThrowError(CStringW sError)
{
	TRACE(L"" + sError + L"\n");
	_com_issue_errorex(Error(sError), static_cast<IActionManager*>(this), __uuidof(IActionManager));
}

void CActionManager::ThrowSystemError(DWORD dwError /*= GetLastError()*/, LPCWSTR szComment /*= NULL*/)
{
	// Throw the formatted error
	if(szComment == NULL)
		ThrowError(CJetIe::GetSystemError(dwError));
	else
		ThrowError((CStringW)szComment + L'\n' + CJetIe::GetSystemError(dwError));
}

STDMETHODIMP CActionManager::ExecuteContextMenuAction(VARIANT ActionRef, VARIANT Parameter)
{
	try
	{
		// Get the corresponding control (or throw & return error, if nonexistent)
		XmlElement	xmlControl = ControlFromGuid2((_bstr_t)(_variant_t)ActionRef);

		// Prepare the browser parameter
		IServiceProviderPtr	oServiceProvider = (IUnknown*)(_variant_t)Parameter;	// The passed-in Service Provider
		SHDocVw::IWebBrowser2Ptr	oBrowser;
		oServiceProvider->QueryService(SID_SWebBrowserApp, IID_IWebBrowser2, reinterpret_cast<void **>(&oBrowser));	// Get the Web browser object interface

		// Execute the action associated with the referenced control
		Execute2(xmlControl, (_variant_t)(IDispatch*)(IDispatchPtr)oBrowser);
	}
	COM_CATCH_RETURN();

	return S_OK;
}

void CActionManager::ValidateControls()
{
	///////////////////////////////////////////////////////////////
	// Ensure that all the EntryID's are set and do not duplicate

	std::map<int, bool>	mapEntryIds;	// Stores the entry IDs encountered in the controls list to check for duplicates
	std::vector<XmlElement>	xmlEntriless;	// Elements without an entry-ID that should be granted a new entry-id

	// Enumerate all the control families
	XmlNodeList	xmlControlFamilies = m_xmlControls->selectNodes(L"/JetIe/Controls");
	XmlElement	xmlControlFamily;
	while((xmlControlFamily = xmlControlFamilies->nextNode()) != NULL)
	{
		// Enumerate all the controls within the control families
		XmlNodeList	xmlControls = xmlControlFamily->selectNodes(L".//*");
		XmlElement	xmlControl;
		while((xmlControl = xmlControls->nextNode()) != NULL)
		{
			try
			{
				if(xmlControl->getAttributeNode(L"EntryID") != NULL)
				{	// If EntryID is present, check for duplicates
					int	nID = xmlControl->getAttribute(L"EntryID");
					if((nID == 0) || (mapEntryIds.find(nID) != mapEntryIds.end()))	// This ID is duplicated by an existing one; zero entry-ids are also disallowed
					{	// Schedule for generation of a new ID
						xmlControl->removeAttribute(L"EntryID");
						xmlEntriless.insert(xmlEntriless.end(), xmlControl);
					}
					else	// This is a new ID, add it
						mapEntryIds[nID] = true;
				}
				else	// No EntryID speicifed, schedulle for assigning a new one
					xmlEntriless.insert(xmlEntriless.end(), xmlControl);

			}
			catch(_com_error e)
			{	// An error has occured, probably when reading/coercing the ID, schedulle for regeneration
				COM_TRACE();
				xmlEntriless.insert(xmlEntriless.end(), xmlControl);
			}
		}
	}

	// Generate the new entry-IDs, as necessary
	std::vector<XmlElement>::iterator	it;
	int	nNextEntryID = 1;	// Start looking for vacant IDs from this point
	for(it = xmlEntriless.begin(); it != xmlEntriless.end(); ++it)
	{
		// Find the next vacant ID
		for( ; (mapEntryIds.find(nNextEntryID) != mapEntryIds.end()) && (nNextEntryID < 0x7FFF); nNextEntryID++)
			;

		// Apply
		(*it)->setAttribute(L"EntryID", (_variant_t)nNextEntryID);
		mapEntryIds[nNextEntryID] = true;
	}

	/////////////////////////////////////////////////////////////////////
	// Check that the drop-down-buttons have their default controls set
	XmlNodeList	xmlDropDownButtons = m_xmlControls->selectNodes(L"/JetIe/Controls//DropDownButton");
	XmlElement	xmlDropDownButton;
	while((xmlDropDownButton = xmlDropDownButtons->nextNode()) != NULL)
	{
		// If the drop-down-button has no default control (that is, entry of that control assigned to the Default attribute), or there is no such control beneath the button
		if((xmlDropDownButton->getAttributeNode(L"Default") == NULL) || (xmlDropDownButton->selectSingleNode((_bstr_t)L"*[@EntryID='" + (_bstr_t)xmlDropDownButton->getAttribute(L"EntryID") + L"']") == NULL))
		{
			// If the button has no suitable children, just kill it
			if(xmlDropDownButton->selectSingleNode(L"*[string(@EntryID) != '']") == NULL)
				xmlDropDownButton->parentNode->removeChild(xmlDropDownButton);
			else	// There are suitable children, take EntryID of the first of them as a default
				xmlDropDownButton->setAttribute(L"Default", XmlElement(xmlDropDownButton->selectSingleNode(L"*[string(@EntryID) != '']"))->getAttribute(L"EntryID"));
		}
	}
}

IRawActionManagerPtr CActionManager::GetInstance()
{
	// If the cached instance is valid for the current thread, return it
	if(m_dwMainThreadID == GetCurrentThreadId())
		return m_oMainThreadInstance;

	// If there is a cached instance, but it's invalid in this thread, create a new one
	if(m_dwMainThreadID != NULL)
	{
		IRawActionManagerPtr	oInstance(__uuidof(CActionManager));
		return oInstance;
	}

	// There is no caching instance. Create a new one, but under a lock
	{
		CMutex	mutexStaticLock(NULL, TRUE, _T("JetBrains.JetIe.") + CJetIe::LoadStringT(IDS_PLUGIN_NAME) + _T("ActionManager.StaticLock"));	// Lock now
		IRawActionManagerPtr	oInstance(__uuidof(CActionManager));

		if(m_dwMainThreadID != NULL)	// May have became non-null in the time since the last check
			return oInstance;	// Just throw out the temp instance

		// Set up cache for this thread
		m_dwMainThreadID = GetCurrentThreadId();
		m_oMainThreadInstance = oInstance;
		return oInstance;
	}
}

void CActionManager::Load()
{
	LoadData(true);
}

void CActionManager::Save()
{
	SaveData(true);
}

bool CActionManager::HasText(_bstr_t bsID)
{
	XmlNode	xmlNode = GetAction(bsID)->selectSingleNode(L"Style/@Text");
	if(xmlNode == NULL)
		return false;	// Not specified => not shown
	return !!(int)xmlNode->nodeValue;	// Coerce as int to a boolean value
}

IDispatchPtr CActionManager::GetDispatchHandler(CStringW sClassID) throw(_com_error)
{
	IDispatchPtr	oHandler;

	// Try to look up an existing handler
	std::map<CStringW, IDispatchPtr>::iterator itHandler = m_mapDispatchHandlers.find(sClassID);
	if(itHandler == m_mapDispatchHandlers.end())
	{	// Cache missed, create a new instance and add it to the cache
		oHandler.CreateInstance(sClassID);
		m_mapDispatchHandlers[sClassID] = oHandler;
	}
	else
		oHandler = itHandler->second;	// Cache hit, take the existing instance

	return oHandler;
}

XmlElement CActionManager::ShowPopupMenu(XmlElement xmlParent, HWND hwndParent, POINT ptScreenCoordinates, _variant_t vtParam) throw(_com_error)
{
	return ShowPopupMenu(xmlParent->selectNodes(L"*"), hwndParent, ptScreenCoordinates, vtParam);
}

XmlElement CActionManager::ShowPopupMenu(XmlNodeList xmlControls, HWND hwndParent, POINT ptScreenCoordinates, _variant_t vtParam) throw(_com_error)
{
	// Create a popup menu that will hold the top-level items
	CPopupMenuHandle menu;
	menu.Attach(CreatePopupMenu());

	// An array of the submenus whose only duty is to free the resources when they're needed no more
	CPopupMenuHandleArray	arSubmenus;

	// Maps the temporary IDs assigned to the menu items to action IDs that should be executed upon activating a particular item
	std::map<int, XmlElement>	mapItemControls;

	// Fill the top-level items and recurse to submenus, if needed
	FillSubmenu(xmlControls, menu, arSubmenus, mapItemControls, vtParam);

	// Show the menu
	int	nSelectedId = TrackPopupMenu(menu, TPM_LEFTALIGN | TPM_LEFTBUTTON | TPM_TOPALIGN | TPM_RETURNCMD, ptScreenCoordinates.x, ptScreenCoordinates.y, NULL, hwndParent, NULL);

	// Check for errors or user-cancel
	// A false return value means either a failure or user's cancelling the menu, treat as to an error to the first case only
	if(!nSelectedId)
	{
		DWORD	dwPossibleError;
		if((dwPossibleError = GetLastError()) != ERROR_SUCCESS)	
			CJetIeException::ThrowSystemError();
		return NULL;	// User-cancelled
	}

	// Find the action that was selected
	std::map<int, XmlElement>::iterator	itAction = mapItemControls.find(nSelectedId);
	if(itAction == mapItemControls.end())
		CJetIeException::ThrowComError(E_UNEXPECTED);

	// Execute the action
	Execute2(itAction->second, vtParam);

	return itAction->second;	// The control that was selected
}

void CActionManager::FillSubmenu(XmlNodeList xmlControls, HMENU menuParent, CPopupMenuHandleArray &arSubmenus, std::map<int, XmlElement> &mapItemControls, _variant_t vtParam) throw(_com_error)
{
	XmlElement	xmlControl;

	// Enumerate and process all the controls
	while((xmlControl = xmlControls->nextNode()) != NULL)
	{
		try	// Per-control failures do not cause the global failure
		{
			// Further processing depends on the control type
			if(xmlControl->baseName == (_bstr_t)L"Control")	// Just an ordinary control
			{
				XmlElement	xmlAction = GetAction2(xmlControl);
				_bstr_t	bsActionId = xmlAction->getAttribute(L"ID");

				// Query for the control text, visibility, and enabled-state
				DWORD	dwOleCmdF = 0;
				CStringW	sTitle;
				QueryStatus(bsActionId, vtParam, &dwOleCmdF, &sTitle, NULL, NULL, true);

				// Append, if visible
				if(dwOleCmdF & OLECMDF_SUPPORTED)
				{
					int	nID = (int)mapItemControls.size() + 1;	// An unique ID for the next item
					CHECK_BOOL(AppendMenu(menuParent, MF_STRING | ((dwOleCmdF & OLECMDF_ENABLED) ? MF_ENABLED : MF_DISABLED), nID, ToT(sTitle)));
					mapItemControls[nID] = xmlControl;
				}
			}	// "Control"
			else if(xmlControl->baseName == (_bstr_t)L"DropDownButton")	// A drop-down button with a drop-arrow and default action, both clickable:  [button|v]
			{
				// Get the title string for the dropdown
				CStringW	sTitle;
				QueryStatus((_bstr_t)ControlFromEntryID(xmlControl->getAttribute(L"Default"), xmlControl, false)->getAttribute(L"Action"), vtParam, NULL, &sTitle, NULL, NULL, true);

				// Add a popup menu, register for disposal
				HMENU	menuSub = CreateMenu();
				arSubmenus.Add(menuSub);

				// Mount the submenu into the current menu
				CHECK_BOOL(AppendMenu(menuParent, MF_POPUP, (UINT_PTR)menuSub, ToT(sTitle)));

				// Fill in the submenu
				FillSubmenu(xmlControl->selectNodes(L"*"), menuSub, arSubmenus, mapItemControls, vtParam);
			}
			else if(xmlControl->baseName == (_bstr_t)L"Separator")	// Add a separator to the toolbar
			{
				// Add a separator to the menu
				CHECK_BOOL(AppendMenu(menuParent, MF_SEPARATOR, 0, NULL));
			}
			else	// Yet another control type
			{
				ASSERT(FALSE);
				ThrowError(CJetIe::LoadString(IDS_E_CONTROLTYPE));
			}
		}
		catch(_com_error e)
		{
			COM_TRACE();
			TRACE(L"Failed to add the %s control to the popup menu.", (LPCWSTR)(_bstr_t)xmlControl->xml);
		}
	}
}

// TODO: resolve the collision that we add extensions for the current user but have to register the handlers globally under Windows 9x !! Most probably, by having an individual GUID range for each user.