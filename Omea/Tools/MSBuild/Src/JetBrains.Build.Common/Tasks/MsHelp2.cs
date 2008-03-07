/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Compiles the MS-Help files into the Document Explorer format.
	/// </summary>
	public class MsHelp2 : ToolTaskBase
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the project (Collection, HxC) file.
		/// </summary>
		[Required]
		public ITaskItem HelpCollectionFile
		{
			get
			{
				return (ITaskItem)Bag[AttributeName.HelpCollectionFile];
			}
			set
			{
				Bag[AttributeName.HelpCollectionFile] = value;
			}
		}

		/// <summary>
		/// Gets or sets the name of the output (generated, HxS) file.
		/// </summary>
		[Required]
		public ITaskItem OutputFile
		{
			get
			{
				return (ITaskItem)Bag[AttributeName.OutputFile];
			}
			set
			{
				Bag[AttributeName.OutputFile] = value;
			}
		}

		/// <summary>
		/// Gets or sets the help project root.
		/// </summary>
		[Required]
		public ITaskItem ProjectDir
		{
			get
			{
				return (ITaskItem)Bag[AttributeName.ProjectDir];
			}
			set
			{
				Bag[AttributeName.ProjectDir] = value;
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
			int nExitCode = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
			Log.LogMessage(MessageImportance.Normal, "The “{0}” tool has exited with code {1}.", ToolName, nExitCode);
			return 0; // Suppress fake error codes
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

			cmd.AppendSwitch("-p");
			cmd.AppendFileNameIfNotNull(TaskHelper.GetValue<ITaskItem>(Bag, AttributeName.HelpCollectionFile));

			cmd.AppendSwitch("-r");
			cmd.AppendFileNameIfNotNull(TaskHelper.GetValue<ITaskItem>(Bag, AttributeName.ProjectDir));

			cmd.AppendSwitch("-o");
			cmd.AppendFileNameIfNotNull(TaskHelper.GetValue<ITaskItem>(Bag, AttributeName.OutputFile));

			return cmd.ToString();
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
				return "HxComp.exe";
			}
		}

		#endregion
	}
}