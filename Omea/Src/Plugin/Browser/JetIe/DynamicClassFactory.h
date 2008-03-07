/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// CDynamicClassFactory — the dynamic class factory that is capable of
// creating a multiple COM objects based on the single C++ class by
// looking up the provided GUID and parameterizing the newly-created
// object with that GUID.
//
// Used for creating the Internet Explorer UI elements.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic

#pragma once

class CDynamicClassFactory : public CComClassFactory
{
protected:
	CDynamicClassFactory();

public:
	virtual ~CDynamicClassFactory();

	/// Specifies which exactly object should be created by this class factory.
	void SetClsid( CLSID clsid );

	// IClassFactory methods
	STDMETHOD(CreateInstance)( LPUNKNOWN pUnkOuter, REFIID riid, void** ppvObj );

protected:
	/// CLSID of the control we're about to produce. Inambiguously derives to an action.
	CLSID	m_clsid;
};
