/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Threading;

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;
using Microsoft.Win32;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Runs the DEVENV application.
	/// </summary>
	public class StartDevenv : VsHiveTask
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the command-line arguments for the DEVENV application.
		/// </summary>
		[Required]
		public string Arguments
		{
			get
			{
				return (string)Bag[AttributeName.Arguments];
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				Bag[AttributeName.Arguments] = value;
			}
		}

		/// <summary>
		/// Gets or sets whether the operation should be performed asynchronously.
		/// If sync, we wait for Devenv to finish. Otherwise, we do not.
		/// </summary>
		[Required]
		public bool Async
		{
			get
			{
				return (bool)(Bag[AttributeName.Async] ?? false);
			}
			set
			{
				Bag[AttributeName.Async] = value;
			}
		}

		/// <summary>
		/// Gets or sets the name of the executable to run.
		/// It may differ for the development Visual Studio.
		/// Optional.
		/// </summary>
		public string DevenvExecutableName
		{
			get
			{
				return (string)Bag[AttributeName.DevenvExecutableName];
			}
			set
			{
				Bag[AttributeName.DevenvExecutableName] = value;
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Gets the DEVENV installation folder.
		/// </summary>
		protected string GetDevenvInstallDir()
		{
			using(RegistryKey key = Registry.LocalMachine.OpenSubKey(string.Format("SOFTWARE\\Microsoft\\VisualStudio\\{0}", GetVsHive()), false))
				return (string)key.GetValue("InstallDir");
		}

		/// <summary>
		/// Gets the devenv command line argument that specifies the hive (with a trailing space). Could be an empty string.
		/// </summary>
		protected string GetHiveArgument()
		{
			if(string.IsNullOrEmpty(GetVsRootSuffix()))
				return "";
			return string.Format("/RootSuffix {0} ", GetVsRootSuffix());
		}

		/// <summary>
		/// Picks the name from attrs, or uses the default.
		/// </summary>
		private string GetDevenvExecutableName()
		{
			return Bag[AttributeName.DevenvExecutableName] as string ?? "devenv.exe";
		}

		#endregion

		#region Overrides

		/// <summary>
		/// The method to be overriden in inheriting tasks.
		/// Throw an exception in case of an errror.
		/// </summary>
		protected override void ExecuteTask()
		{
			// Prepare
			var process = new Process();
			var si = new ProcessStartInfo();
			si.FileName = GetDevenvInstallDir() + GetDevenvExecutableName();
			si.Arguments = GetHiveArgument() + GetStringValue(AttributeName.Arguments);
			si.ErrorDialog = false;
			si.CreateNoWindow = true;
			process.StartInfo = si;

			// Start
			Log.LogMessage("Starting “{0}” with cmdline “{1}”{2}.", si.FileName, si.Arguments, (Async ? "" : ", waiting for the process to finish"));
			if(!process.Start())
				throw new InvalidOperationException(string.Format("The process has refused to start without specifying an error code."));

			// Wait for, if sync
			if(!Async)
			{
				while(!process.HasExited)
					Thread.Sleep(100);
				if(process.ExitCode == 0)
					Log.LogMessage("The process has completed its execution.");
				else
					throw new InvalidOperationException(string.Format("The process “{0}” cmdline “{1}” has failed with exit code {2}.", si.FileName, si.Arguments, process.ExitCode));
			}
		}

		#endregion
	}
}