/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Reflection;

using JetBrains.Build.Omea.Util;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JetBrains.Build.Omea.Infra
{
	/// <summary>
	/// The resolved part of the <see cref="ProbingTask"/> task that sets up the appdomain and commences the execution.
	/// Must be marshal-by-value.
	/// Any execution exceptions are written into the <see cref="Bag"/> under the <see cref="AttributeName.AfxException"/> id.
	/// </summary>
	[Serializable]
	public class ProbingTaskResolved
	{
		#region Init

		public ProbingTaskResolved(Bag bag)
		{
			Bag = bag;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets or sets the property bag with the task parameters.
		/// </summary>
		public Bag Bag { get; set; }

		#endregion

		#region Operations

		public void Execute()
		{
			try
			{
				using(new AssemblyResolver(Bag.Get<string[]>(AttributeName.AfxProbingDirectories), AppDomain.CurrentDomain))
					ExecuteResolved();
			}
			catch(Exception ex)
			{
				SetException(ex);
			}
		}

		/// <summary>
		/// Records the exceptions in the resolved executor.
		/// <c>Null</c> on success.
		/// </summary>
		public void SetException(Exception value)
		{
			Bag.Set(AttributeName.AfxException, value);
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Executes under the assembly resolver.
		/// </summary>
		private void ExecuteResolved()
		{
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			ExecuteResolved_PreloadAssemblies();
			ExecuteResolved_RunTask();
		}

		/// <summary>
		/// Preloads some assemblies into the current appdomain.
		/// </summary>
		private void ExecuteResolved_PreloadAssemblies()
		{
			if(Bag.Contains<ITaskItem[]>(AttributeName.LoadAssemblies))
			{
				foreach(ITaskItem assemblyname in Bag.Get<ITaskItem[]>(AttributeName.LoadAssemblies))
					Assembly.Load(assemblyname.ItemSpec);
			}
		}

		/// <summary>
		/// Runs the resolved part of the task.
		/// </summary>
		private void ExecuteResolved_RunTask()
		{
			// Load the resolved part DLL
			Assembly assemblyResolved = Assembly.Load(ProbingTask.ResolvedPartAssemblyName);
			if(assemblyResolved == null)
				throw new InvalidOperationException(string.Format("Failed to load the assembly with the resolved parts of the task, assembly name: “{0}”.", ProbingTask.ResolvedPartAssemblyName));

			// Create class
			string sUnresolvedTaskName = Bag.GetString(AttributeName.AfxUnresolvedTaskName);
			string sResolvedClassName = ProbingTask.ResolvedPartAssemblyName + ".Tasks." + sUnresolvedTaskName + "Resolved";
			object oInstance = assemblyResolved.CreateInstance(sResolvedClassName);
			if(oInstance == null)
				throw new InvalidOperationException(string.Format("Failed to load the resolved class for the task “{0}” from the resolved assembly, expected name: “{1}”.", sUnresolvedTaskName, sResolvedClassName));
			var oInstanceAsTask = oInstance as ITaskBaseResolved;
			if(oInstanceAsTask == null)
				throw new InvalidOperationException(string.Format("The resolved class “{1}” for the task “{0}” (instantiated as “{3}”) does not implement the required resolved task interface “{2}”.", sUnresolvedTaskName, sResolvedClassName, typeof(ITaskBaseResolved).AssemblyQualifiedName, oInstance.GetType().AssemblyQualifiedName));
			Bag.Get<TaskLoggingHelper>(AttributeName.AfxLog).LogMessage(MessageImportance.Low, "Executing resolved part of the task, appdomain “{1}”, class “{0}”.", oInstance.GetType().AssemblyQualifiedName, AppDomain.CurrentDomain.FriendlyName);

			// Set up and run!
			oInstanceAsTask.Bag = Bag;
			oInstanceAsTask.Execute();
		}

		/// <summary>
		/// Reports the unhandled exceptions in the appdomain.
		/// </summary>
		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			SetException((args.ExceptionObject as Exception) ?? new Exception("Unidentified failure."));
		}

		#endregion
	}
}