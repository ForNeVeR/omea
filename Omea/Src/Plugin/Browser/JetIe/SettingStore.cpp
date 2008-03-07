/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// © JetBrains, Inc, 2005
// Written by (H) Serge Baltic

#include "StdAfx.h"
#include "SettingStore.h"
#include "CommonResource.h"
#include "JetIe.h"

CSettingStore::CSettingStore(CString sCompanyName, CString sApplicationName)
{
	ASSERT(!sCompanyName.IsEmpty());
	ASSERT(!sApplicationName.IsEmpty());

	ASSERT(sCompanyName == _T("JetBrains"));	//	 ;-)

	// Store
	m_sCompanyName = sCompanyName;
	m_sApplicationName = sApplicationName;
}

CSettingStore::CSettingStore(const CSettingStore &other)
{
	*this = other;	// Use the assignment operator
}

CSettingStore::~CSettingStore(void)
{
}

CSettingStore &CSettingStore::operator=(const CSettingStore &other)
{
	m_sCompanyName = other.m_sCompanyName;
	m_sApplicationName = other.m_sApplicationName;
	m_mapSettings = other.m_mapSettings;

	return *this;
}

void CSettingStore::AddEntryDefinition(int nID, CString sSection, CString sEntry, _variant_t vtDefault)
{
	ASSERT(m_mapSettings.find(nID) == m_mapSettings.end());	// ID must be unique
	m_mapSettings[nID] = CSettingsEntry(sSection, sEntry, vtDefault);	// Insertion works if it is already present as well
}

bool CSettingStore::RewriteSettings() const
{
	CSettingsEntryMapCit	cit;
	bool	bOK = true;

	CString	sValue;
	int	nValue;

	for( cit = m_mapSettings.begin(); cit != m_mapSettings.end(); ++cit )
	{
		switch( V_VT(&cit->second.m_vtDefault) )
		{
		case VT_BSTR:
			sValue = GetProfileStringT( cit->first );
			WriteProfileStringT( cit->first, sValue );
			break;
		case VT_I4:
			nValue = GetProfileInt( cit->first );
			WriteProfileInt( cit->first, nValue );
			break;
		default:
			ASSERT(FALSE);
			bOK = false;
		}
	}

	return bOK;
}

int CSettingStore::GetProfileInt(int nID) const
{
	ASSERT(m_mapSettings.find(nID) != m_mapSettings.end());
	if(m_mapSettings.find(nID) == m_mapSettings.end())
		return 0;	// Not found
	const CSettingsEntry	&se = m_mapSettings.find(nID)->second;
	se.ASSERT_VALID(VT_I4);
	return GetProfileInt(se.m_sSection, se.m_sEntry, (long)se.m_vtDefault);
}

CString CSettingStore::GetProfileStringT(int nID) const
{
	ASSERT(m_mapSettings.find(nID) != m_mapSettings.end());
	if(m_mapSettings.find(nID) == m_mapSettings.end())
		return _T("");	// Not found
	const CSettingsEntry	&se = m_mapSettings.find(nID)->second;
	se.ASSERT_VALID(VT_BSTR);
	return GetProfileString(se.m_sSection, se.m_sEntry, (_bstr_t)se.m_vtDefault);
}

CStringA CSettingStore::GetProfileStringA(int nID) const
{
	return (LPCSTR)CT2A((LPCTSTR)GetProfileStringT(nID));
}

CStringW CSettingStore::GetProfileStringW(int nID) const
{
	return (LPCWSTR)CT2W((LPCTSTR)GetProfileStringT(nID));
}

BOOL CSettingStore::GetProfileBinary(int nID, BYTE** ppData, UINT* pBytes) const
{
	ASSERT(m_mapSettings.find(nID) != m_mapSettings.end());
	if(m_mapSettings.find(nID) == m_mapSettings.end())
		return FALSE;	// Not found
	const CSettingsEntry	&se = m_mapSettings.find(nID)->second;
	se.ASSERT_VALID();
	return GetProfileBinary(se.m_sSection, se.m_sEntry, ppData, pBytes);
}

BOOL CSettingStore::WriteProfileInt(int nID, int nValue) const
{
	ASSERT(m_mapSettings.find(nID) != m_mapSettings.end());
	if(m_mapSettings.find(nID) == m_mapSettings.end())
		return FALSE;	// Not found
	const CSettingsEntry	&se = m_mapSettings.find(nID)->second;
	se.ASSERT_VALID(VT_I4);
	return WriteProfileInt(se.m_sSection, se.m_sEntry, nValue);
}

BOOL CSettingStore::WriteProfileStringT(int nID, LPCTSTR lpszValue) const
{
	ASSERT(m_mapSettings.find(nID) != m_mapSettings.end());
	if(m_mapSettings.find(nID) == m_mapSettings.end())
		return FALSE;	// Not found
	const CSettingsEntry	&se = m_mapSettings.find(nID)->second;
	se.ASSERT_VALID(VT_BSTR);
	return WriteProfileString(se.m_sSection, se.m_sEntry, lpszValue);
}

BOOL CSettingStore::WriteProfileStringW(int nID, LPCWSTR lpszValue) const
{
	return WriteProfileStringT(nID, CW2T(lpszValue));
}

BOOL CSettingStore::WriteProfileBinary(int nID, LPBYTE pData, UINT nBytes) const
{
	ASSERT(m_mapSettings.find(nID) != m_mapSettings.end());
	if(m_mapSettings.find(nID) == m_mapSettings.end())
		return FALSE;	// Not found
	const CSettingsEntry	&se = m_mapSettings.find(nID)->second;
	se.ASSERT_VALID();
	return WriteProfileBinary(se.m_sSection, se.m_sEntry, pData, nBytes);
}

int CSettingStore::GetProfileInt(LPCTSTR lpszSection, LPCTSTR lpszEntry, int nDefault) const
{
	ASSERT(lpszSection != NULL);
	ASSERT(lpszEntry != NULL);

HKEY hSecKey = GetSectionKey(lpszSection);
	if (hSecKey == NULL)
		return nDefault;
	DWORD dwValue;
	DWORD dwType;
	DWORD dwCount = sizeof(DWORD);
	LONG lResult = RegQueryValueEx(hSecKey, (LPTSTR)lpszEntry, NULL, &dwType,
		(LPBYTE)&dwValue, &dwCount);
	RegCloseKey(hSecKey);
	if (lResult == ERROR_SUCCESS)
	{
		ASSERT(dwType == REG_DWORD);
		ASSERT(dwCount == sizeof(dwValue));
		return (UINT)dwValue;
	}
	return nDefault;
}

CString CSettingStore::GetProfileString(LPCTSTR lpszSection, LPCTSTR lpszEntry, LPCTSTR lpszDefault) const
{
	ASSERT(lpszSection != NULL);
	ASSERT(lpszEntry != NULL);

	HKEY hSecKey = GetSectionKey(lpszSection);
	if (hSecKey == NULL)
		return lpszDefault;
	CString strValue;
	DWORD dwType, dwCount;
	LONG lResult = RegQueryValueEx(hSecKey, (LPTSTR)lpszEntry, NULL, &dwType,
		NULL, &dwCount);
	if (lResult == ERROR_SUCCESS)
	{
		ASSERT(dwType == REG_SZ);
		lResult = RegQueryValueEx(hSecKey, (LPTSTR)lpszEntry, NULL, &dwType,
			(LPBYTE)strValue.GetBuffer(dwCount/sizeof(TCHAR)), &dwCount);
		strValue.ReleaseBuffer();
	}
	RegCloseKey(hSecKey);
	if (lResult == ERROR_SUCCESS)
	{
		ASSERT(dwType == REG_SZ);
		return strValue;
	}
	return lpszDefault;
}

BOOL CSettingStore::GetProfileBinary(LPCTSTR lpszSection, LPCTSTR lpszEntry, BYTE** ppData, UINT* pBytes) const
{
	ASSERT(lpszSection != NULL);
	ASSERT(lpszEntry != NULL);
	ASSERT(ppData != NULL);
	ASSERT(pBytes != NULL);
	*ppData = NULL;
	*pBytes = 0;

	HKEY hSecKey = GetSectionKey(lpszSection);
	if (hSecKey == NULL)
		return FALSE;

	DWORD dwType, dwCount;
	LONG lResult = RegQueryValueEx(hSecKey, (LPTSTR)lpszEntry, NULL, &dwType,
		NULL, &dwCount);
	*pBytes = dwCount;
	if (lResult == ERROR_SUCCESS)
	{
		ASSERT(dwType == REG_BINARY);
		*ppData = new BYTE[*pBytes];
		lResult = RegQueryValueEx(hSecKey, (LPTSTR)lpszEntry, NULL, &dwType,
			*ppData, &dwCount);
	}
	RegCloseKey(hSecKey);
	if (lResult == ERROR_SUCCESS)
	{
		ASSERT(dwType == REG_BINARY);
		return TRUE;
	}
	else
	{
		delete [] *ppData;
		*ppData = NULL;
	}
	return FALSE;

}

BOOL CSettingStore::WriteProfileInt(LPCTSTR lpszSection, LPCTSTR lpszEntry, int nValue) const
{
	ASSERT(lpszSection != NULL);
	ASSERT(lpszEntry != NULL);

	HKEY hSecKey = GetSectionKey(lpszSection);
	if (hSecKey == NULL)
		return FALSE;
	LONG lResult = RegSetValueEx(hSecKey, lpszEntry, NULL, REG_DWORD,
		(LPBYTE)&nValue, sizeof(nValue));
	RegCloseKey(hSecKey);
	return lResult == ERROR_SUCCESS;
}

BOOL CSettingStore::WriteProfileString(LPCTSTR lpszSection, LPCTSTR lpszEntry, LPCTSTR lpszValue) const
{
	ASSERT(lpszSection != NULL);

	LONG lResult;
	if (lpszEntry == NULL) //delete whole section
	{
		HKEY hAppKey = GetAppRegistryKey();
		if (hAppKey == NULL)
			return FALSE;
		lResult = ::RegDeleteKey(hAppKey, lpszSection);
		RegCloseKey(hAppKey);
	}
	else if (lpszValue == NULL)
	{
		HKEY hSecKey = GetSectionKey(lpszSection);
		if (hSecKey == NULL)
			return FALSE;
		// necessary to cast away const below
		lResult = ::RegDeleteValue(hSecKey, (LPTSTR)lpszEntry);
		RegCloseKey(hSecKey);
	}
	else
	{
		HKEY hSecKey = GetSectionKey(lpszSection);
		if (hSecKey == NULL)
			return FALSE;
		lResult = RegSetValueEx(hSecKey, lpszEntry, NULL, REG_SZ,
			(LPBYTE)lpszValue, (lstrlen(lpszValue)+1)*sizeof(TCHAR));
		RegCloseKey(hSecKey);
	}
	return lResult == ERROR_SUCCESS;
}

BOOL CSettingStore::WriteProfileBinary(LPCTSTR lpszSection, LPCTSTR lpszEntry, LPBYTE pData, UINT nBytes) const
{
	ASSERT(lpszSection != NULL);

	LONG lResult;
	HKEY hSecKey = GetSectionKey(lpszSection);
	if (hSecKey == NULL)
		return FALSE;
	lResult = RegSetValueEx(hSecKey, lpszEntry, NULL, REG_BINARY,
		pData, nBytes);
	RegCloseKey(hSecKey);
	return lResult == ERROR_SUCCESS;
}

// returns key for HKEY_CURRENT_USER\"Software"\RegistryKey\ProfileName
// creating it if it doesn't exist
// responsibility of the caller to call RegCloseKey() on the returned HKEY
HKEY CSettingStore::GetAppRegistryKey() const
{
	ASSERT(!m_sCompanyName.IsEmpty());
	ASSERT(!m_sApplicationName.IsEmpty());

	HKEY hAppKey = NULL;
	HKEY hSoftKey = NULL;
	HKEY hCompanyKey = NULL;
	if (RegOpenKeyEx(HKEY_CURRENT_USER, _T("software"), 0, KEY_WRITE|KEY_READ,
		&hSoftKey) == ERROR_SUCCESS)
	{
		DWORD dw;
		if (RegCreateKeyEx(hSoftKey, m_sCompanyName, 0, REG_NONE,
			REG_OPTION_NON_VOLATILE, KEY_WRITE|KEY_READ, NULL,
			&hCompanyKey, &dw) == ERROR_SUCCESS)
		{
			RegCreateKeyEx(hCompanyKey, m_sApplicationName, 0, REG_NONE,
				REG_OPTION_NON_VOLATILE, KEY_WRITE|KEY_READ, NULL,
				&hAppKey, &dw);
		}
	}
	if (hSoftKey != NULL)
		RegCloseKey(hSoftKey);
	if (hCompanyKey != NULL)
		RegCloseKey(hCompanyKey);

	return hAppKey;
}

// returns key for:
//      HKEY_CURRENT_USER\"Software"\RegistryKey\AppName\lpszSection
// creating it if it doesn't exist.
// responsibility of the caller to call RegCloseKey() on the returned HKEY
HKEY CSettingStore::GetSectionKey(LPCTSTR lpszSection) const
{
	ASSERT(lpszSection != NULL);

	HKEY hSectionKey = NULL;
	HKEY hAppKey = GetAppRegistryKey();
	if (hAppKey == NULL)
		return NULL;

	DWORD dw;
	RegCreateKeyEx(hAppKey, lpszSection, 0, REG_NONE,
		REG_OPTION_NON_VOLATILE, KEY_WRITE|KEY_READ, NULL,
		&hSectionKey, &dw);
	RegCloseKey(hAppKey);
	return hSectionKey;
}

_variant_t CSettingStore::Read(CStringW sSection, CStringW sEntry) const throw(_com_error)
{
	CString	sSectionT = ToT(sSection);
	CString	sEntryT = ToT(sEntry);
	// Currently, we just identify the setting using the linear search
	for(CSettingsEntryMapCit cit = m_mapSettings.begin(); cit != m_mapSettings.end(); ++cit)	// All the settings registered
	{
		if((cit->second.m_sSection == sSectionT) && (cit->second.m_sEntry == sEntryT))	// Is this it?
		{
			switch(V_VT(&cit->second.m_vtDefault))	// Invoke the accessor depending on the setting type
			{
			case VT_I4:
				return GetProfileInt(cit->second.m_sSection, cit->second.m_sEntry, (long)cit->second.m_vtDefault);
			case VT_BSTR:
				return (_bstr_t)(LPCTSTR)GetProfileString(cit->second.m_sSection, cit->second.m_sEntry, (_bstr_t)cit->second.m_vtDefault);
			default:
				CJetIeException::Throw(CJetIe::LoadString(IDS_SETTINGS_TYPEINVALID));	// The type of that setting is not supported
			}
		}
	}
	CJetIeException::Throw(CJetIe::LoadString(IDS_SETTINGS_UNDEFINEDSETTING));	// The requested setting was not found
	return NULL;	// Just a dummy return
}

void CSettingStore::Write(CStringW sSection, CStringW sEntry, _variant_t vtValue) const throw(_com_error)
{
	CString	sSectionT = ToT(sSection);
	CString	sEntryT = ToT(sEntry);
	// Currently, we just identify the setting using the linear search
	for(CSettingsEntryMapCit cit = m_mapSettings.begin(); cit != m_mapSettings.end(); ++cit)	// All the settings registered
	{
		if((cit->second.m_sSection == sSectionT) && (cit->second.m_sEntry == sEntryT))	// Is this it?
		{
			switch(V_VT(&cit->second.m_vtDefault))	// Invoke the accessor depending on the setting type
			{
			case VT_I4:
				WriteProfileInt(cit->second.m_sSection, cit->second.m_sEntry, (int)vtValue);
				return;
			case VT_BSTR:
				WriteProfileString(cit->second.m_sSection, cit->second.m_sEntry, (_bstr_t)vtValue);
				return;
			default:
				CJetIeException::Throw(CJetIe::LoadString(IDS_SETTINGS_TYPEINVALID));	// The type of that setting is not supported
			}
		}
	}
	CJetIeException::Throw(CJetIe::LoadString(IDS_SETTINGS_UNDEFINEDSETTING));	// The requested setting was not found
}

CSettingStore::CSettingsEntry::CSettingsEntry()
{
	V_VT(&m_vtDefault) = VT_EMPTY;
}

CSettingStore::CSettingsEntry::CSettingsEntry(CString sSection, CString sEntry, _variant_t vtDefault)
{
	ASSERT( (V_VT(&vtDefault) == VT_BSTR) || (V_VT(&vtDefault) == VT_I4) );
	m_sSection = sSection;
	m_sEntry = sEntry;
	m_vtDefault = vtDefault;
}

CSettingStore::CSettingsEntry::CSettingsEntry(const CSettingsEntry &other)
{
	*this = other;
}

CSettingStore::CSettingsEntry &CSettingStore::CSettingsEntry::operator=(const CSettingsEntry &other)
{
	m_sSection = other.m_sSection;
	m_sEntry = other.m_sEntry;
	m_vtDefault = other.m_vtDefault;

	return *this;
}

CSettingStore::CSettingsEntry::~CSettingsEntry()
{
}

void CSettingStore::CSettingsEntry::ASSERT_VALID( VARTYPE vt /*= VT_EMPTY*/ ) const
{
	// Don't allow empty section and value names.
	ASSERT(!m_sSection.IsEmpty());
	ASSERT(!m_sEntry.IsEmpty());

	// Check for an unitialized number …
	ASSERT(V_VT(&m_vtDefault) != VT_EMPTY);

	if( vt != VT_EMPTY )	// Constrain required
		ASSERT( V_VT(&m_vtDefault) == vt );
}
