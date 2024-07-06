// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetIe Setting Store
//
// Generic implementation for a Registry setting store,
// which has a strict list of settings and default values so that they could be accessed
// via an ID to avoid disagreement in default values, names and locations.
//
// © JetBrains, Inc, 2005
// Written by (H) Serge Baltic

#pragma once

// TODO: implement critical sections to synchronize access to this object from different threads.
class CSettingStore
{
public:
	/// Initializes the instance by specifying the registry location which will point to the HKCR\Software\<CompanyName>\<ApplicationName> key as the settints store root key.
	CSettingStore(CString sCompanyName, CString sApplicationName);

	/// Alows cloning the instances.
	CSettingStore(const CSettingStore &other);

	/// Deinitializes the instance.
	virtual ~CSettingStore();

	/// Allows assigning/cloning the instance.
	CSettingStore &operator=(const CSettingStore &other);

// Operations
public:

	/// Adds a definition of the settings entry. Note that the settings values are not actually stored in this class, they're stored in the Registry, and here they're defined to avoid desynchronization of key, value names and default values across different calls to the setting store.
	/// Accespts an ID by which this new entry will be referenced and which must be unique (uniqueness check is performed in DEBUG build only), and the entry parameters.
	/// Type of the default value must be either VT_BSTR or VT_I4 and it defines the registry value type.
	void AddEntryDefinition(int nID, CString sSection, CString sEntry, _variant_t vtDefault);

	/// Loads and writes back all the settings whose definitions are currently present in the setting store, causing the missing settings to be written with their default values. Returns whether all the write operations have succeeded. Does no abort after the first failure.
	bool RewriteSettings() const;

	/// Returns an integer option value identified by an option ID which must map into a Registry value of the type appropriate, or the default value given by the option ID if the value is not available.
	int GetProfileInt(int nID) const;

	/// Returns a T-string option value identified by an option ID which must map into a Registry value of the type appropriate, or the default value given by the option ID if the value is not available.
	CString GetProfileStringT(int nID) const;

	/// Returns an ANSI string option value identified by an option ID which must map into a Registry value of the type appropriate, or the default value given by the option ID if the value is not available.
	CStringA GetProfileStringA(int nID) const;

	/// Returns a UNICODE string option value identified by an option ID which must map into a Registry value of the type appropriate, or the default value given by the option ID if the value is not available.
	CStringW GetProfileStringW(int nID) const;

	/// Returns an binary option value identified by an option ID which must map into a Registry value of the type appropriate.
	BOOL GetProfileBinary(int nID, BYTE** ppData, UINT* pBytes) const;

	/// Persists an integer option value, identified by an option ID which must map into a Registry value.
	BOOL WriteProfileInt(int nID, int nValue) const;

	/// Persists a string option value, identified by an option ID which must map into a Registry value.
	BOOL WriteProfileStringT(int nID, LPCTSTR lpszValue) const;

	/// Persists a string option value, identified by an option ID which must map into a Registry value.
	BOOL WriteProfileStringW(int nID, LPCWSTR lpszValue) const;

	/// Persists a binary option value, identified by an option ID which must map into a Registry value.
	BOOL WriteProfileBinary(int nID, LPBYTE pData, UINT nBytes) const;

	/// Reads a setting of either type from the Registry and returns its value as a VARIANT type.
	/// Fails if there's no such setting registered.
	_variant_t Read(CStringW sSection, CStringW sEntry) const throw(_com_error);

	/// Writes a setting of either type to the Registry.
	/// Fails if there's no such setting registered.
	void Write(CStringW sSection, CStringW sEntry, _variant_t vtValue) const throw(_com_error);

// Deprecated functions
protected:

	/// Returns an integer option value (or a default value, if absent), identified by the section specifying the Registry key (use a backslash to separate subkeys) and the entry which defines the value name within that key, which must be of the appropriate type.
	int GetProfileInt(LPCTSTR lpszSection, LPCTSTR lpszEntry, int nDefault) const;

	/// Returns a string option value (or a default value, if absent), identified by the section specifying the Registry key (use a backslash to separate subkeys) and the entry which defines the value name within that key, which must be of the appropriate type.
	CString GetProfileString(LPCTSTR lpszSection, LPCTSTR lpszEntry, LPCTSTR lpszDefault) const;

	/// Returns an binary option value (or a default value, if absent), identified by the section specifying the Registry key (use a backslash to separate subkeys) and the entry which defines the value name within that key, which must be of the appropriate type.
	BOOL GetProfileBinary(LPCTSTR lpszSection, LPCTSTR lpszEntry, BYTE** ppData, UINT* pBytes) const;

	/// Persists an integer option value, identified by the section specifying the Registry key (use a backslash to separate subkeys) and the entry which defines the value name within that key.
	BOOL WriteProfileInt(LPCTSTR lpszSection, LPCTSTR lpszEntry, int nValue) const;

	/// Persists a string option value, identified by the section specifying the Registry key (use a backslash to separate subkeys) and the entry which defines the value name within that key.
	BOOL WriteProfileString(LPCTSTR lpszSection, LPCTSTR lpszEntry, LPCTSTR lpszValue) const;

	/// Persists a binary option value, identified by the section specifying the Registry key (use a backslash to separate subkeys) and the entry which defines the value name within that key.
	BOOL WriteProfileBinary(LPCTSTR lpszSection, LPCTSTR lpszEntry, LPBYTE pData, UINT nBytes) const;

// Implementation
protected:

	/// Company name in the registry key (under HKCU/Software).
	CString	m_sCompanyName;

	/// Application name in the registry (under the company key).
	CString m_sApplicationName;

	/// Returns the application's Registry Key as a WinAPI entity, which must be explicitly closed by the caller.
	HKEY GetAppRegistryKey() const;

	/// Returns the registry key to the section where the application settings are stored, which must be explicitly closed by the caller.
	HKEY GetSectionKey(LPCTSTR lpszSection) const;

	/// Represents a single entry in the settings store, which consists of a section name (optionally including subsections), value name, and its default value (either a string or an integer, binary has no default values). Used to insert in the hash map of the option values.
	class CSettingsEntry
	{
	public:
		/// Default ctor. Makes the object invalid.
		CSettingsEntry();
		/// Ctor initializing the values.
		CSettingsEntry(CString sSection, CString sEntry, _variant_t vtDefautl);
		/// Copy ctor.
		CSettingsEntry(const CSettingsEntry &other);
		/// Assignment op.
		CSettingsEntry &operator=(const CSettingsEntry &other);
		/// Dtor.
		virtual ~CSettingsEntry();
		/// Checks the validness of this structure (if vt is not VT_EMPTY, constrains the option type, as defined by the default value's type, also).
		void ASSERT_VALID( VARTYPE vt = VT_EMPTY ) const;

		/// Section name (opt. with subsections divided by backslashes, as required by advapi).
		CString	m_sSection;
		/// Entry name (name of the value in the corresponding key).
		CString	m_sEntry;
		/// Default value to be used if the setting is missing in the Registry.
		_variant_t	m_vtDefault;
	};

	/// A map of the settings entries.
	typedef std::map<int, CSettingsEntry>	CSettingsEntryMap;
	typedef std::map<int, CSettingsEntry>::const_iterator	CSettingsEntryMapCit;
	typedef std::pair<int, CSettingsEntry>	CSettingsEntryMapItem;

	/// A hash map of the known settings types. Note that the settings values are not actually stored here, they're stored in the Registry, and here they're defined to avoid desynchronization of key, value names and default values across different calls to the setting store.
	CSettingsEntryMap	m_mapSettings;
};
