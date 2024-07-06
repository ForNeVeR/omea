// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace JetBrains.Build.Omea.Resolved.Infra
{
	/// <summary>
	/// Creates a runtime wrapper around an exported function of a native DLL.
	/// </summary>
	public class DynamicPinvoke : IDisposable
	{
		#region Data

		private static MethodInfo myMethod;

		#endregion

		#region Init

		/// <summary>
		/// Creates a runtime PInvoke wrapper for the DLL's function.
		/// </summary>
		/// <param name="fiDll">The DLL to invoke into.</param>
		/// <param name="sFunctionName">A name of the DLL's exported function.</param>
		/// <param name="typeReturn">The return type of the exported function.</param>
		/// <param name="typeArgs">Argument types of the exported function.</param>
		public DynamicPinvoke(FileInfo fiDll, string sFunctionName, Type typeReturn, params Type[] typeArgs)
		{
			Setup(fiDll, sFunctionName, typeReturn, typeArgs);
		}

		#endregion

		#region Operations

		/// <summary>
		/// Invokes the wrapped method.
		/// </summary>
		public object Invoke(params object[] args)
		{
			return myMethod.Invoke(null, args);
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Creates a runtime PInvoke wrapper for the DLL's function.
		/// </summary>
		/// <param name="fiDll">The DLL to invoke into.</param>
		/// <param name="sFunctionName">A name of the DLL's exported function.</param>
		/// <param name="typeReturn">The return type of the exported function.</param>
		/// <param name="typeArgs">Argument types of the exported function.</param>
		private static void Setup(FileInfo fiDll, string sFunctionName, Type typeReturn, params Type[] typeArgs)
		{
			var assemblyName = new AssemblyName();
			assemblyName.Name = "wixTempAssembly";

			AssemblyBuilder dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run | AssemblyBuilderAccess.Save, fiDll.Directory.FullName);
			ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");

			MethodBuilder dynamicMethod = dynamicModule.DefinePInvokeMethod(sFunctionName, fiDll.FullName, MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.PinvokeImpl | MethodAttributes.HideBySig, CallingConventions.Standard, typeReturn, typeArgs, CallingConvention.Winapi, CharSet.Ansi);
			dynamicMethod.SetImplementationFlags(MethodImplAttributes.PreserveSig);
			dynamicModule.CreateGlobalFunctions();

			myMethod = dynamicModule.GetMethod(sFunctionName);
		}

		#endregion

		#region IDisposable Members

		///<summary>
		///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		///</summary>
		///<filterpriority>2</filterpriority>
		public void Dispose()
		{
		}

		#endregion
	}
}
