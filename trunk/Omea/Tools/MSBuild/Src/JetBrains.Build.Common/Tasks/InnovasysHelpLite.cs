/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Builds the Innovasis Help Studio Lite projects.
	/// </summary>
	public class InnovasysHelpLite : ToolTaskBase
	{
		#region Data

		/// <summary>
		/// A message with all the line/cols available.
		/// </summary>
		public static readonly Regex myRegexDetailedMessage = new Regex(@"^\s*(?<Type>\w+)\:\s*(?<Code>\w+)\:\s*(?<Message>.*)\s+\[(?<File>.+)\s+line\s+(?<Line>\d+)\s+col\s+(?<Col>\d+)\s*$", RegexOptions.Compiled | RegexOptions.Singleline);

		/// <summary>
		/// A line that has either message.
		/// </summary>
		public static readonly Regex myRegexLineWithMessage = new Regex(@"^(?<Text>\s*(?:Error|Warning)\:.*)$", RegexOptions.Multiline | RegexOptions.Compiled);

		public readonly string LogFileName = "HelpCompilerLog.txt";

		#endregion

		#region Attributes

		/// <summary>
		/// Gets or sets the Help Studio project file pathname.
		/// </summary>
		[Required]
		public ITaskItem InputFile
		{
			get
			{
				return BagGetTry<ITaskItem>(AttributeName.InputFile);
			}
			set
			{
				BagSet(AttributeName.InputFile, value);
			}
		}

		/// <summary>
		/// Gets or sets the output folder path/name.
		/// </summary>
		[Required]
		public ITaskItem OutDir
		{
			get
			{
				return BagGetTry<ITaskItem>(AttributeName.OutDir);
			}
			set
			{
				BagSet(AttributeName.OutDir, value);
			}
		}

		/// <summary>
		/// Gets or sets the name of the output file in the <see cref="OutDir"/> folder.
		/// </summary>
		public ITaskItem OutputFile
		{
			get
			{
				return BagGetTry<ITaskItem>(AttributeName.OutputFile);
			}
			set
			{
				BagSet(AttributeName.OutputFile, value);
			}
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
			// Run the compiler
			int nExitCode = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

			// Harvest the errors/warnings, as the dumb compiler won't spit them outta
			var fiLog = new FileInfo(Path.Combine(TaskHelper.GetValue<ITaskItem>(Bag, AttributeName.OutDir).ItemSpec, LogFileName));
			if(fiLog.Exists)
			{
				// Read da log
				string sLog;
				using(FileStream stream = fiLog.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
					sLog = new StreamReader(stream, Encoding.Default).ReadToEnd();

				// Fetch da messages
				foreach(Match matchLine in myRegexLineWithMessage.Matches(sLog))
				{
					string sMessage = matchLine.Groups["Text"].Value;

					Match match = myRegexDetailedMessage.Match(sMessage);
					if(match.Success)
					{
						// We've managed to fetch the message details
						try
						{
							if(match.Groups["Type"].Value == "Warning")
								Log.LogWarning("HelpCompiler", match.Groups["Code"].Value, null, match.Groups["File"].Value, int.Parse(match.Groups["Line"].Value), int.Parse(match.Groups["Col"].Value), 0, 0, match.Groups["Message"].Value);
							else
								Log.LogError("HelpCompiler", match.Groups["Code"].Value, null, match.Groups["File"].Value, int.Parse(match.Groups["Line"].Value), int.Parse(match.Groups["Col"].Value), 0, 0, match.Groups["Message"].Value);

							continue; // Done with this one!
						}
						catch(Exception ex)
						{
							Log.LogWarningFromException(ex, true);
						}
					}

					// No details available, or failed to use them, log just the full text
					Log.LogError(sMessage);
				}
			}

			// Parse the exit code
			switch(nExitCode)
			{
			case 0:
				break; // Success
			case -1:
				Log.LogError("HelpStudioLite2", nExitCode.ToString(), "", "", 0, 0, 0, 0, "Missing or invalid project filename.");
				break;
			case -2:
				Log.LogError("HelpStudioLite2", nExitCode.ToString(), "", "", 0, 0, 0, 0, "Build was cancelled.");
				break;
			case -3:
				Log.LogError("HelpStudioLite2", nExitCode.ToString(), "", "", 0, 0, 0, 0, "Project compile failed.");
				break;
			case -4:
				Log.LogError("HelpStudioLite2", nExitCode.ToString(), "", "", 0, 0, 0, 0, "Project compiled completed with errors or warnings.");
				break;
			case -5:
				Log.LogError("HelpStudioLite2", nExitCode.ToString(), "", "", 0, 0, 0, 0, "The Build Profile or Booklet specified could not be found.");
				break;
			default:
				Log.LogError("HelpStudioLite2", nExitCode.ToString(), "", "", 0, 0, 0, 0, "Unspecified error.");
				break;
			}

			return nExitCode;
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
			var cmd = new CommandLineBuilder();

			// Project file to build
			cmd.AppendSwitch(new FileInfo(TaskHelper.GetValue<ITaskItem>(Bag, AttributeName.InputFile).ItemSpec).FullName);

			// Make a build!
			cmd.AppendSwitch("/b");

			// Don't show GUI
			cmd.AppendSwitch("/s");

			// Output folder
			cmd.AppendSwitch("/o=" + new FileInfo(TaskHelper.GetValue<ITaskItem>(Bag, AttributeName.OutDir).ItemSpec).FullName);

			// Output file name
			if(BagGetTry<ITaskItem>(AttributeName.OutputFile) != null)
				cmd.AppendSwitch("/f=" + new FileInfo(TaskHelper.GetValue<ITaskItem>(Bag, AttributeName.OutputFile).ItemSpec).FullName);

			return cmd.ToString();
		}

		/// <summary>
		/// Gets the name of the environment variable that provides the path to the tool in case the <see cref="ToolTaskBase.ToolDir"/> is not defined.
		/// </summary>
		public override string ToolDirEnvName
		{
			get
			{
				return "InnovasysHelpStudioLite0220Dir";
			}
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
				return "HelpStudioLite2.exe";
			}
		}

		#endregion
	}
}