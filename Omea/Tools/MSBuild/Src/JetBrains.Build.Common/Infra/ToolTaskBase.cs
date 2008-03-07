/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JetBrains.Build.Common.Infra
{
	/// <summary>
	/// A base task for tool-based tasks, defining the attributes bag.
	/// </summary>
	public abstract class ToolTaskBase : ToolTask
	{
		#region Data

		protected Hashtable myBag = new Hashtable();

		#endregion

		#region Attributes

		public Hashtable Bag
		{
			get
			{
				return myBag;
			}
		}

		/// <summary>
		/// Gets or sets the directory in which the tool task executable resides.
		/// </summary>
		public ITaskItem ToolDir
		{
			get
			{
				return BagGetTry<ITaskItem>(AttributeName.ToolDir);
			}
			set
			{
				BagSet(AttributeName.ToolDir, value);
			}
		}

		/// <summary>
		/// Gets the name of the environment variable that provides the path to the tool in case the <see cref="ToolDir"/> is not defined.
		/// </summary>
		public virtual string ToolDirEnvName
		{
			get
			{
				return null;
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Checks whether a bag entry is present.
		/// </summary>
		public bool BagContains(AttributeName name)
		{
			return Bag[name] != null;
		}

		/// <summary>
		/// Gets a typed value from the bag. Throws if a value is missing.
		/// </summary>
		public T BagGet<T>(AttributeName name)
		{
			return TaskHelper.GetValue<T>(Bag, name);
		}

		/// <summary>
		/// Gets a typed value from the bag. Returns the <paramref name="defaultvalue"/> if an entry is missing from the bag.
		/// </summary>
		public T BagGet<T>(AttributeName name, T defaultvalue)
		{
			object entry = Bag[name];
			return (T)(entry ?? defaultvalue);
		}

		/// <summary>
		/// Gets a typed value from the bag. <c>Null</c> (a missing value) is OK.
		/// </summary>
		public T BagGetTry<T>(AttributeName name)
		{
			return (T)Bag[name];
		}

		/// <summary>
		/// Puts a typed value to the bag. <c>Null</c> (a missing value) is OK.
		/// </summary>
		public void BagSet<T>(AttributeName name, T value)
		{
			Bag[name] = value;
		}

		#endregion

		#region Overrides

		///<summary>
		///Returns the fully qualified path to the executable file.
		///</summary>
		///
		///<returns>
		///The fully qualified path to the executable file.
		///</returns>
		///
		protected override string GenerateFullPathToTool()
		{
			if(string.IsNullOrEmpty(ToolName))
				throw new InvalidOperationException(string.Format("The tool name must not be empty in the build task."));
			if(string.IsNullOrEmpty(Path.GetExtension(ToolName)))
				throw new InvalidOperationException(string.Format("The tool name “{0}” must include the extension.", ToolName));

			// Fetch the tool dir
			string sToolDir = null;

			if((ToolDir != null) && (!string.IsNullOrEmpty(ToolDir.ItemSpec))) // Manual tool dir
			{
				Log.LogMessage(MessageImportance.Low, "{0} location is given by the ToolDir property value.", ToolName);
				sToolDir = ToolDir.ItemSpec;
			}
			else if(!string.IsNullOrEmpty(ToolDirEnvName)) // Via env
			{
				sToolDir = Environment.GetEnvironmentVariable(ToolDirEnvName);
				Log.LogMessage(MessageImportance.Low, "{0} location is given by the ToolDirEnvName environment variable (“{1}”).", ToolName, Environment.GetEnvironmentVariable(ToolDirEnvName));
			}
			if(!string.IsNullOrEmpty(sToolDir)) // Use if could be obtained
				return Path.Combine(sToolDir, ToolName);

			// Lookup an SDK file
			foreach(var action in new Func<string>[] {delegate { return ToolLocationHelper.GetPathToDotNetFrameworkFile(ToolName, TargetDotNetFrameworkVersion.VersionLatest); }, delegate { return ToolLocationHelper.GetPathToDotNetFrameworkSdkFile(ToolName, TargetDotNetFrameworkVersion.VersionLatest); }, delegate { return ToolLocationHelper.GetPathToSystemFile(ToolName); }, delegate { return ToolLocationHelper.GetPathToDotNetFrameworkFile(ToolName, TargetDotNetFrameworkVersion.Version20); }, delegate { return ToolLocationHelper.GetPathToDotNetFrameworkSdkFile(ToolName, TargetDotNetFrameworkVersion.Version20); }, delegate { return ToolLocationHelper.GetPathToDotNetFrameworkFile(ToolName, TargetDotNetFrameworkVersion.Version11); }, delegate { return ToolLocationHelper.GetPathToDotNetFrameworkSdkFile(ToolName, TargetDotNetFrameworkVersion.Version11); },})
			{
				try
				{
					string sFullPath = action();
					if((!string.IsNullOrEmpty(sFullPath)) && (File.Exists(sFullPath)))
					{
						Log.LogMessage(MessageImportance.Low, "{0} location is given by the ToolLocationHelper (“{1}”).", ToolName, sFullPath);
						return sFullPath;
					}
				}
				catch(Exception ex)
				{
					Log.LogMessage("Warning when probing for {0}: {1}", ToolName, ex.Message);
				}
			}

			Log.LogMessage(MessageImportance.Low, "{0} location could not be determined, leaving to the shell.", ToolName);
			return ToolName; // Fallback to %PATH% on execution
		}

		#endregion
	}
}