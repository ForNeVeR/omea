// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace JetBrains.Build.Omea.Util
{
	/// <summary>
	/// Helps with finding the assemblies that are referenced but could not be located by probing.
	/// </summary>
	public class AssemblyResolver : MarshalByRefObject, IDisposable
	{
		#region Data

		/// <summary>
		/// <see cref="AppDomain"/>.
		/// </summary>
		protected readonly AppDomain myAppDomain;

		/// <summary>
		/// <see cref="Directories"/>.
		/// </summary>
		protected readonly List<DirectoryInfo> myDirectories;

		#endregion

		#region Init

		/// <summary>
		/// Cretaes the resolver and attaches it to the appdomain. Call <see cref="IDisposable.Dispose"/> after use.
		/// </summary>
		/// <param name="directories">Directories to probe for the missing references.</param>
		/// <param name="appdomain">The appdomain we're patching.</param>
		public AssemblyResolver(ICollection<string> directories, AppDomain appdomain)
		{
			if(directories == null)
				throw new ArgumentNullException("directories");
			if(appdomain == null)
				throw new ArgumentNullException("appdomain");

			myAppDomain = appdomain;
			myDirectories = new List<DirectoryInfo>(directories.Count);
			foreach(string directory in directories)
			{
				var di = new DirectoryInfo(directory);
				if(!di.Exists)
					throw new InvalidOperationException(string.Format("The specified references directory “{0}” does not exist.", di.FullName));
				myDirectories.Add(di);
			}
			myAppDomain.AssemblyResolve += OnAssemblyResolve;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// The appdomain we're patching.
		/// </summary>
		public AppDomain AppDomain
		{
			get
			{
				return myAppDomain;
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Gets the list of directories for probing.
		/// </summary>
		protected List<DirectoryInfo> Directories
		{
			get
			{
				return myDirectories;
			}
		}

		/// <summary>
		/// Probes for the assembly.
		/// </summary>
		protected Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
		{
			string sAssemblyShortName = new AssemblyName(args.Name).Name;
			foreach(DirectoryInfo di in myDirectories)
			{
				var fi = new FileInfo(Path.Combine(di.FullName, sAssemblyShortName + ".dll"));
				if(fi.Exists)
					return Assembly.LoadFrom(fi.FullName);
				fi = new FileInfo(Path.Combine(di.FullName, sAssemblyShortName + ".exe"));
				if(fi.Exists)
					return Assembly.LoadFrom(fi.FullName);
			}
			return null;
		}

		#endregion

		#region IDisposable Members

		///<summary>
		///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		///</summary>
		///<filterpriority>2</filterpriority>
		public void Dispose()
		{
			myAppDomain.AssemblyResolve -= OnAssemblyResolve;
		}

		#endregion
	}
}
