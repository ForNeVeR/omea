/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JetBrains.Build.Tools
{
	/// <summary>
	/// A task that produces a strongly-typed C# serialization classes file out of an XSD schema.
	/// We have to use a tool-task because the API is very poor.
	/// </summary>
	public class XsdCs : ToolTask
	{
		#region Data

		private ITaskItem myInputFile;

		private string myNamespace;

		private ITaskItem myOutDir;

		#endregion

		#region Attributes

		/// <summary>
		/// Gets or sets the input XSD file.
		/// </summary>
		[Required]
		public ITaskItem InputFile
		{
			get
			{
				return myInputFile;
			}
			set
			{
				myInputFile = value;
			}
		}

		/// <summary>
		/// Gets or sets the namespace for the generated classes.
		/// </summary>
		[Required]
		public string Namespace
		{
			get
			{
				return myNamespace;
			}
			set
			{
				myNamespace = value;
			}
		}

		/// <summary>
		/// Gets or sets the output directory.
		/// </summary>
		[Required]
		public ITaskItem OutDir
		{
			get
			{
				return myOutDir;
			}
			set
			{
				myOutDir = value;
			}
		}

		#endregion

		#region Overrides

		protected override string GenerateCommandLineCommands()
		{
			CommandLineBuilder cmd = new CommandLineBuilder();

			// XSD file
			if(InputFile == null)
				throw new InvalidOperationException(string.Format("The input file must be specified."));
			cmd.AppendFileNameIfNotNull(InputFile);

			// Generate classes
			cmd.AppendSwitch("/classes");

			// Namespace
			if(string.IsNullOrEmpty(Namespace))
				throw new InvalidOperationException(string.Format("The namespace must be specified."));
			cmd.AppendSwitch("/namespace:" + Namespace);

			// Outdir
			if(OutDir == null)
				throw new InvalidOperationException(string.Format("The output folder must be specified."));
			cmd.AppendSwitch("/out:" + OutDir);

			return cmd.ToString();
		}

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

			if((ToolPath != null) && (!string.IsNullOrEmpty(ToolPath))) // Manual tool dir
			{
				Log.LogMessage(MessageImportance.Low, "{0} location is given by the ToolDir property value.", ToolName);
				sToolDir = ToolPath;
			}
			if(!string.IsNullOrEmpty(sToolDir)) // Use if could be obtained
				return Path.Combine(sToolDir, ToolName);

			// Lookup an SDK file
			foreach(Func<string> action in new Func<string>[] {delegate { return ToolLocationHelper.GetPathToDotNetFrameworkFile(ToolName, TargetDotNetFrameworkVersion.VersionLatest); }, delegate { return ToolLocationHelper.GetPathToDotNetFrameworkSdkFile(ToolName, TargetDotNetFrameworkVersion.VersionLatest); }, delegate { return ToolLocationHelper.GetPathToSystemFile(ToolName); }, delegate { return ToolLocationHelper.GetPathToDotNetFrameworkFile(ToolName, TargetDotNetFrameworkVersion.Version20); }, delegate { return ToolLocationHelper.GetPathToDotNetFrameworkSdkFile(ToolName, TargetDotNetFrameworkVersion.Version20); }, delegate { return ToolLocationHelper.GetPathToDotNetFrameworkFile(ToolName, TargetDotNetFrameworkVersion.Version11); }, delegate { return ToolLocationHelper.GetPathToDotNetFrameworkSdkFile(ToolName, TargetDotNetFrameworkVersion.Version11); },})
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

		protected override string ToolName
		{
			get
			{
				return "Xsd.exe";
			}
		}

		#endregion

		#region Func Type

		public delegate T Func<T>();

		#endregion
	}
}