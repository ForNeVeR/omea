// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

namespace JetBrains.Build
{
	/// <summary>
	/// An MSBuild task that compiles the IL projects.
	/// </summary>
	public class Ilasm : ManagedCompiler
	{
		#region Data

		public static Regex myRegexError = new Regex(@"^\s*(?<Filename>.+?)\((?<Line>\d+)\)\s+\:\s+error\s+\-\-\s+(?<Text>.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

		public static Regex myRegexWarning = new Regex(@"^\s*(?<Filename>.+?)\((?<Line>\d+)\)\s+\:\s+warning\s+\-\-\s+(?<Text>.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

		#endregion

		#region Init

		public Ilasm()
		{
			HostCompilerSupportsAllParameters = false;
			NoConfig = false;
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Adds Ilasm-specific command-line commands not handled by the base class.
		/// </summary>
		/// <param name="commandLine">The command line to popuplate.</param>
		protected void AddMoreCommandLineCommands(CommandLineBuilderExtension commandLine)
		{
			AddMoreCommandLineCommands_TargetType(commandLine);
		}

		/// <summary>
		/// The target type command line commands.
		/// </summary>
		/// <param name="commandLine">The command line to popuplate.</param>
		protected void AddMoreCommandLineCommands_TargetType(CommandLineBuilderExtension commandLine)
		{
			CommandLineAppendWhenTrue(commandLine, "/DLL", "Dll");
			CommandLineAppendWhenTrue(commandLine, "/EXE", "Exe");
		}

		/// <summary>
		/// Carries out an internal <c>CommandLineBuilderExtension.AppendWhenTrue</c> method.
		/// </summary>
		protected void CommandLineAppendWhenTrue(CommandLineBuilderExtension commandLine, string switchName, string parameterName)
		{
			object o = Bag[parameterName];
			if((o != null) && ((bool)o))
				commandLine.AppendSwitch(switchName);
		}

		/// <summary>
		/// Adapts the bag property values to the Ilasm before they get into the command line.
		/// </summary>
		protected void PreprocessBag()
		{
			PreprocessBag_TargetType();
		}

		/// <summary>
		/// Removes the TargetType property, adds the /dll and /exe instead.
		/// </summary>
		protected void PreprocessBag_TargetType()
		{
			var sTargetType = Bag["TargetType"] as string;
			if(sTargetType == null)
				return;

			// Suppress
			Bag["TargetType"] = null;

			// Inject the replacement
			switch(sTargetType.ToLowerInvariant())
			{
			case "library":
				Bag["Dll"] = true;
				break;
			case "winexe":
				Bag["Exe"] = true;
				break;
			default:
				Log.LogError(string.Format("Unexpected TargetType value {0}.", sTargetType));
				break;
			}
		}

		#endregion

		#region Overrides

		///<summary>
		///Fills the specified <see cref="T:Microsoft.Build.Tasks.CommandLineBuilderExtension"></see> with the switches and other information that cannot go into a response file.
		///</summary>
		///
		///<param name="commandLine">The <see cref="T:Microsoft.Build.Tasks.CommandLineBuilderExtension"></see> to fill with switches and other information that cannot go into a response file.</param>
		protected override void AddCommandLineCommands(CommandLineBuilderExtension commandLine)
		{
			// Update values to fit the ilasm
			PreprocessBag();

			base.AddCommandLineCommands(commandLine);
			base.AddResponseFileCommands(commandLine); // All the response file content goes to the command line for Ilasm

			// Additional commands, not supported by the base class
			AddMoreCommandLineCommands(commandLine);
		}

		///<summary>
		///Fills the specified <see cref="T:Microsoft.Build.Tasks.CommandLineBuilderExtension"></see> with the switches and other information that can go into a response file.
		///</summary>
		///
		///<param name="commandLine">The <see cref="T:Microsoft.Build.Tasks.CommandLineBuilderExtension"></see> to fill with switches and other information.</param>
		protected override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
		{
			return; // Nothing goes to the response file, Ilasm won't support one
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
			string path = ToolLocationHelper.GetPathToDotNetFrameworkFile(ToolName, TargetDotNetFrameworkVersion.VersionLatest);
			if(path == null)
				Log.LogError("Could not locate the {0} compiler.", ToolName);
			return path;
		}

		///<summary>
		///Parses a single line of text to identify any errors or warnings in canonical format.
		///</summary>
		///
		///<param name="messageImportance">A value of <see cref="T:Microsoft.Build.Framework.MessageImportance"></see> that indicates the importance level with which to log the message.</param>
		///<param name="text">A single line of text for the method to parse.</param>
		protected override void LogEventsFromTextOutput(string text, MessageImportance messageImportance)
		{
			if(text.IndexOf("error --", StringComparison.InvariantCultureIgnoreCase) >= 0)
			{
				Match match = myRegexError.Match(text);
				if(match.Success)
				{
					int nLine;
					if(!int.TryParse(match.Groups["Line"].Value, out nLine))
						nLine = 0;
					Log.LogError("", "", "", match.Groups["Filename"].Value, nLine, 0, 0, 0, match.Groups["Text"].Value);
				}
				else
					Log.LogError(text);
			}
			else if(text.IndexOf("warning --", StringComparison.InvariantCultureIgnoreCase) >= 0)
			{
				Match match = myRegexWarning.Match(text);
				if(match.Success)
				{
					int nLine;
					if(!int.TryParse(match.Groups["Line"].Value, out nLine))
						nLine = 0;
					Log.LogWarning("", "", "", match.Groups["Filename"].Value, nLine, 0, 0, 0, match.Groups["Text"].Value);
				}
				else
					Log.LogWarning(text);
			}
			else
				base.LogEventsFromTextOutput(text, messageImportance);
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
				return "IlAsm.exe";
			}
		}

		#endregion
	}
}
