// NetfxBootstrap.h

#pragma once

using namespace System;
using namespace System::Reflection;

namespace NetfxBootstrap
{
	class NetfxBootstrap
	{
	public:
		/// Loads a managed assembly by its name, invokes a static member of a managed type.
		/// LParam is an optional pointer to an IntPtr parameter.
		static void InvokeStaticFromAssemblyName(LPWSTR szAssemblyName, LPWSTR szTypeName, LPWSTR szMemberName, LPARAM *pLParam)
		{
			try
			{
				Assembly ^assembly = Assembly::Load(gcnew String(szAssemblyName));

				Type ^type = assembly->GetType(gcnew String(szTypeName), true);

				// Args: zero or one
				array<Object^> ^args;
				if(pLParam == NULL)
					args = gcnew array<Object^>(0);
				else
				{
					args = gcnew array<Object^>(1);
					args[0] = IntPtr((void*)*pLParam);
				}

				type->InvokeMember(gcnew String(szMemberName), BindingFlags::Static | BindingFlags::InvokeMethod | BindingFlags::Public, nullptr, nullptr, args);
			}
			catch(Exception ^ex)
			{
				pin_ptr<Char> pstr = &ex->Message->ToCharArray()[0];
				OutputDebugStringW(pstr);
				throw (int)E_FAIL;
			}
		}
	};
}
