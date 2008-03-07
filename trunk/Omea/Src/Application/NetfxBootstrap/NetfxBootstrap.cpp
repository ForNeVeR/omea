// This is the main DLL file.

#include "stdafx.h"

#include "NetfxBootstrap.h"

/// Loads a managed assembly by its name, invokes a static member of a managed type.
/// LParam is an optional pointer to an IntPtr parameter.
extern "C" __declspec(dllexport) HRESULT InvokeStaticFromAssemblyName(LPWSTR szAssemblyName, LPWSTR szTypeName, LPWSTR szMemberName, LPARAM *pLParam)
{
	try
	{
		// A method containing managed code can throw on entry, so we have no such code in here
		NetfxBootstrap::NetfxBootstrap::InvokeStaticFromAssemblyName(szAssemblyName, szTypeName, szMemberName, pLParam);

		return S_OK;
	}
	catch(int n)
	{
		return n ? n : E_FAIL;
	}
	catch(unsigned int n)
	{
		return n ? n : E_FAIL;
	}
	catch(...)
	{
		return E_FAIL;
	}
}