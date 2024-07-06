// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

namespace JetBrains.Build
{
	public class Peverify : ToolTask
	{
		#region Data

		protected static Regex myRegexErrorCode = new Regex(@"\(\s*Error\:\s*(?<Code>0x[0-9a-zA-z]+)\s*\)", RegexOptions.Compiled | RegexOptions.Singleline);

		protected static Regex myRegexErrorFile = new Regex(@"Error\:\s+\[\s*(?<Name>.+?)\s+\:\s+", RegexOptions.Compiled | RegexOptions.Singleline);

		protected static Regex myRegexInt32Hex = new Regex(@"^0x[0-9a-zA-z]{8}$", RegexOptions.Compiled | RegexOptions.Singleline);

		protected static Regex myRegexMessageCategory = new Regex(@"\[(?<Category>[A-Z]+)\]\:", RegexOptions.Compiled | RegexOptions.Singleline);

		protected static Regex myRegexParseErrorInstance = new Regex(@"\s+\:\s+(?<Member>.+?)\].*\[offset (?<Offset>0x[0-9A-Za-z]{8})\].*\(\s*Error\:\s*(?<Code>0x[0-9a-zA-z]{8})\s*\)", RegexOptions.Compiled | RegexOptions.Singleline);

		protected static Regex myRegexWarningCode = new Regex(@"\(\s*Warning\:\s*(?<Code>0x[0-9a-zA-z]+)\s*\)", RegexOptions.Compiled | RegexOptions.Singleline);

		protected static Regex myRegexWarningFile = new Regex(@"Warning\:\s+\[\s*(?<Name>.+?)\s+\:\s+", RegexOptions.Compiled | RegexOptions.Singleline);

		protected Hashtable myBag = new Hashtable();

		#endregion

		#region Attributes

		public bool HResult
		{
			get
			{
				return GetBoolParameterWithDefault("HResult", true);
			}
			set
			{
				Bag["HResult"] = value;
			}
		}

		/// <summary>
		/// Ignores a specifc instance of the error, ie in the specific member of the specific type, and at the specific offset.
		/// The item spec is up to you, but you must set the “Code”, “Member” and “Offset” metadata items.
		/// </summary>
		public ITaskItem[] IgnoreErrorInstances
		{
			get
			{
				return (ITaskItem[])Bag["IgnoreErrorInstances"];
			}
			set
			{
				Bag["IgnoreErrorInstances"] = value;
			}
		}

		/// <summary>
		/// Ignores errors by their code.
		/// </summary>
		public ITaskItem[] IgnoreErrors
		{
			get
			{
				return (ITaskItem[])Bag["IgnoreErrors"];
			}
			set
			{
				Bag["IgnoreErrors"] = value;
			}
		}

		public bool NoLogo
		{
			get
			{
				return GetBoolParameterWithDefault("NoLogo", true);
			}
			set
			{
				Bag["NoLogo"] = value;
			}
		}

		public bool Quiet
		{
			get
			{
				return GetBoolParameterWithDefault("Quiet", false);
			}
			set
			{
				Bag["Quiet"] = value;
			}
		}

		public ITaskItem[] Sources
		{
			get
			{
				return (ITaskItem[])Bag["Sources"];
			}
			set
			{
				Bag["Sources"] = value;
			}
		}

		public bool Unique
		{
			get
			{
				return GetBoolParameterWithDefault("Unique", false);
			}
			set
			{
				Bag["Unique"] = value;
			}
		}

		public bool Verbose
		{
			get
			{
				return GetBoolParameterWithDefault("Verbose", true);
			}
			set
			{
				Bag["Verbose"] = value;
			}
		}

		#endregion

		#region Implementation

		/// <summary>Gets the collection of parameters used by the derived task class.</summary>
		/// <returns>The collection of parameters used by the derived task class.</returns>
		protected internal Hashtable Bag
		{
			get
			{
				return myBag;
			}
		}

		/// <summary>Gets the value of the specified Boolean parameter.</summary>
		/// <returns>The parameter value.</returns>
		/// <param name="defaultValue">The value to return if parameterName does not exist in the <see cref="P:Microsoft.Build.Tasks.ToolTaskExtension.Bag"></see>.</param>
		/// <param name="parameterName">The name of the parameter to return.</param>
		protected internal bool GetBoolParameterWithDefault(string parameterName, bool defaultValue)
		{
			object obj1 = Bag[parameterName];
			if(obj1 != null)
				return (bool)obj1;
			return defaultValue;
		}

		/// <summary>
		/// Checks whether the error instance has been specifically ignored.
		/// </summary>
		protected bool IsIgnoredErrorInstance(string text)
		{
			if((IgnoreErrorInstances == null) || (IgnoreErrorInstances.Length == 0))
				return false;

			foreach(ITaskItem item in IgnoreErrorInstances)
			{
				if(IsIgnoredErrorInstance_Item(item, text))
					return true;
			}
			return false;
		}

		/// <summary>
		/// <see cref="IsIgnoredErrorInstance"/> for one specific item.
		/// </summary>
		protected bool IsIgnoredErrorInstance_Item(ITaskItem item, string text)
		{
			string sCode = item.GetMetadata("Code");
			string sMember = item.GetMetadata("Member");
			string sOffset = item.GetMetadata("Offset");

			if(!myRegexInt32Hex.IsMatch(sCode))
			{
				Log.LogError("The Code item metadata value “{0}” must be a prefixed 8-digit hex number, eg “0xDEADBEEF”.", sCode);
				return false;
			}
			if(!myRegexInt32Hex.IsMatch(sOffset))
			{
				Log.LogError("The Offset item metadata value “{0}” must be a prefixed 8-digit hex number, eg “0xDEADBEEF”.", sOffset);
				return false;
			}

			// Parse components
			Match match = myRegexParseErrorInstance.Match(text);
			if(!match.Success)
				return false;

			// Check
			if(sCode != match.Groups["Code"].Value)
				return false;
			if(sMember != match.Groups["Member"].Value)
				return false;
			if(sOffset != match.Groups["Offset"].Value)
				return false;

			return true;
		}

		#endregion

		#region Overrides

		///<summary>
		///Creates a temporoary response (.rsp) file and runs the executable file.
		///</summary>
		///
		///<returns>
		///The returned exit code of the executable file. If the task logged errors, but the executable returned an exit code of 0, this method returns -1.
		///</returns>
		///
		///<param name="commandLineCommands">The command line arguments to pass directly to the executable file.</param>
		///<param name="responseFileCommands">The command line arguments to place in the .rsp file.</param>
		///<param name="pathToTool">The path to the executable file.</param>
		protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
		{
			base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
			return 0;
		}

		///<summary>
		///Returns a string value containing the command line arguments to pass directly to the executable file.
		///</summary>
		///
		///<returns>
		///A string value containing the command line arguments to pass directly to the executable file.
		///</returns>
		///
		protected override string GenerateCommandLineCommands()
		{
			var cmd = new CommandLineBuilderExtension();

			cmd.AppendFileNamesIfNotNull(Sources, " ");
			if(NoLogo)
				cmd.AppendSwitch("/nologo");
			if(Verbose)
				cmd.AppendSwitch("/verbose");
			if(Quiet)
				cmd.AppendSwitch("/quiet");
			if(Unique)
				cmd.AppendSwitch("/unique");
			if(HResult)
				cmd.AppendSwitch("/hresult");

			// Ignore list
			ITaskItem[] arIgnore = IgnoreErrors;
			if((arIgnore != null) && (arIgnore.Length > 0))
			{
				var sb = new StringBuilder();
				foreach(ITaskItem item in arIgnore)
				{
					if(sb.Length != 0)
						sb.Append(',');
					sb.Append(item.ItemSpec);
				}
				cmd.AppendSwitch("/ignore=" + sb);
			}

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
			string path = ToolLocationHelper.GetPathToDotNetFrameworkSdkFile(ToolName, TargetDotNetFrameworkVersion.VersionLatest);
			if(path == null)
				Log.LogError("Could not locate the {0} compiler.", ToolName);
			return path;
		}

		///<summary>
		///Returns a string value containing the command line arguments to add to the response (.rsp) file before running the executable file.
		///</summary>
		///
		///<returns>
		///a string value containing the command line arguments to add to the response (.rsp) file before running the executable file.
		///</returns>
		///
		protected override string GenerateResponseFileCommands()
		{
			// NOP
			return "";
		}

		///<summary>
		///Parses a single line of text to identify any errors or warnings in canonical format.
		///</summary>
		///
		///<param name="messageImportance">A value of <see cref="T:Microsoft.Build.Framework.MessageImportance"></see> that indicates the importance level with which to log the message.</param>
		///<param name="text">A single line of text for the method to parse.</param>
		protected override void LogEventsFromTextOutput(string text, MessageImportance messageImportance)
		{
			// Check if it's a warning or an error
			ErrorOrWarning type = ErrorOrWarning.None;
			if(text.Contains("Error:"))
				type = IsIgnoredErrorInstance(text) ? ErrorOrWarning.ErrorAsWarning : ErrorOrWarning.Error;
			else if(text.Contains("Warning:"))
				type = ErrorOrWarning.Warning;

			// Normal messages?
			if(type == ErrorOrWarning.None)
			{
				base.LogEventsFromTextOutput(text, messageImportance);
				return;
			}

			// Rip out data
			string sCode = "";
			string sCategory = "";
			string sFile = "";

			Match match;
			switch(type)
			{
			case ErrorOrWarning.Warning:
				match = myRegexWarningCode.Match(text);
				if(match.Success)
					sCode = match.Groups["Code"].Value;

				match = myRegexMessageCategory.Match(text);
				if(match.Success)
					sCategory = match.Groups["Category"].Value;

				match = myRegexWarningFile.Match(text);
				if(match.Success)
					sFile = match.Groups["Name"].Value;
				break;
			case ErrorOrWarning.Error:
				match = myRegexErrorCode.Match(text);
				if(match.Success)
					sCode = match.Groups["Code"].Value;

				match = myRegexMessageCategory.Match(text);
				if(match.Success)
					sCategory = match.Groups["Category"].Value;

				match = myRegexErrorFile.Match(text);
				if(match.Success)
					sFile = match.Groups["Name"].Value;
				break;
			case ErrorOrWarning.ErrorAsWarning:
				goto case ErrorOrWarning.Error;
			default:
				throw new InvalidOperationException(string.Format("Unexpected type “{0}”.", type));
			}

			// Log
			switch(type)
			{
			case ErrorOrWarning.Warning:
				Log.LogWarning(sCategory, sCode, "", sFile, 0, 0, 0, 0, text);
				break;
			case ErrorOrWarning.Error:
				Log.LogError(sCategory, sCode, "", sFile, 0, 0, 0, 0, text);
				break;
			case ErrorOrWarning.ErrorAsWarning:
				Log.LogWarning(sCategory, sCode, "", sFile, 0, 0, 0, 0, "[IgnoreErrorInstance] " + text);
				break;
			default:
				throw new InvalidOperationException(string.Format("Unexpected type “{0}”.", type));
			}
		}

		///<summary>
		///Indicates whether all task paratmeters are valid.
		///</summary>
		///
		///<returns>
		///true if all task parameters are valid; otherwise, false.
		///</returns>
		///
		protected override bool ValidateParameters()
		{
			if((Sources == null) || (Sources.Length == 0))
			{
				Log.LogError("There must be at least one source for the task.");
				return false;
			}

			if((IgnoreErrorInstances != null) && (IgnoreErrorInstances.Length != 0) && ((!Verbose) || (!HResult) || (Quiet)))
			{
				Log.LogError("In order to use IgnoreErrorInstances, you must turn on Verbose and HResult, and turn off Quiet.");
				return false;
			}

			return base.ValidateParameters();
		}

		///<summary>
		///Gets the name of the executable file to run.
		///</summary>
		///
		///<returns>
		///The name of the executable file to run.
		///</returns>
		///
		protected override string ToolName
		{
			get
			{
				return "Peverify.exe";
			}
		}

		#endregion

		#region ErrorOrWarning Type

		/// <summary>
		/// How to treat a message we're logging.
		/// </summary>
		protected enum ErrorOrWarning
		{
			/// <summary>
			/// Just text.
			/// </summary>
			None,
			/// <summary>
			/// A warning (go yellow).
			/// </summary>
			Warning,
			/// <summary>
			/// An error (go red).
			/// </summary>
			Error,
			/// <summary>
			/// Formally, an error, but it's in the personal ignore list and we must still go yellow.
			/// </summary>
			ErrorAsWarning,
		}

		#endregion
	}
}
