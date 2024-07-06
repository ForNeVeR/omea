// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using JetBrains.Build.Omea.Util;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Infra
{
	/// <summary>
	/// The base task that locates the “resolved” part of the task being executed, loads it into another appdomain using the assembly resolved, and then executes in there.
	/// </summary>
	public abstract class ProbingTask : TaskBase
	{
		#region Data

		public static readonly string ResolvedPartAssemblyName = "JetBrains.Build.Omea.Resolved";

		#endregion

		#region Attributes

		/// <summary>
		/// Gets or sets the list of assemblies that must be loaded for the <c>Installer</c> and such to function properly.
		/// For example, for the installer to locate the <c>ApplicationDescriptorAttribute</c>, the DLL containing it must be already loaded.
		/// The assemblies may be specified either as files or as assembly names, in which case their probing will be relied upon the resolver and the <see cref="ProductTask.ProductBinariesDir"/> spec.
		/// </summary>
		public ITaskItem[] LoadAssemblies
		{
			get
			{
				return Bag.Get<ITaskItem[]>(AttributeName.LoadAssemblies);
			}
			set
			{
				Bag.Set(AttributeName.LoadAssemblies, value);
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Produces the list of probing locations for the <see cref="AssemblyResolver"/>.
		/// </summary>
		protected string[] CollectProbingDirectories()
		{
			var directories = new List<string>();
			foreach(AttributeName attribute in ProbingDirectoryAttributes)
				directories.Add(Bag.GetString(attribute));
			return directories.ToArray();
		}

		/// <summary>
		/// Gets the list of attributes that must contain the probing directories.
		/// </summary>
		protected virtual ICollection<AttributeName> ProbingDirectoryAttributes
		{
			get
			{
				return new AttributeName[] {};
			}
		}

		#endregion

		#region Overrides

		///<summary>
		///When overridden in a derived class, executes the task.
		///</summary>
		///
		///<returns>
		///true if the task successfully executed; otherwise, false.
		///</returns>
		///
		protected override sealed void ExecuteTask()
		{
			// Create a new appdomain
			var appdomainparams = new AppDomainSetup();
			appdomainparams.ApplicationBase = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath);
			AppDomain appdomain = AppDomain.CreateDomain(ResolvedPartAssemblyName, AppDomain.CurrentDomain.Evidence, appdomainparams);

			// Add service parameters to the bag
			Bag.Set(AttributeName.AfxProbingDirectories, CollectProbingDirectories());
			Bag.Set(AttributeName.AfxUnresolvedTaskName, GetType().Name);
			Bag.Set(AttributeName.AfxLog, Log);

			// Use the appdomain
			try
			{
				appdomain.DoCallBack(new ProbingTaskResolved(Bag).Execute);
			}
			finally
			{
				// Dispose of the appdomain
				AppDomain.Unload(appdomain);
			}

			// Check for execution exceptions, rethrow as needed
			if(Bag.Contains<Exception>(AttributeName.AfxException))
				throw Bag.Get<Exception>(AttributeName.AfxException);
		}

		#endregion
	}
}
