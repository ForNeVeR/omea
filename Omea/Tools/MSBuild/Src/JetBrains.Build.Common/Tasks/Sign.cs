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
	/// Signs the files with Microsoft Authenticode.
	/// Note: this file calls the SignTool.exe manually instead of using the MSBuild utilities.
	/// </summary>
	public class Sign : ToolTaskBase
	{
		#region Attributes

		/// <summary>
		/// The input files that will be signed by the task.
		/// </summary>
		[Required]
		public ITaskItem[] InputFiles
		{
			get
			{
				return BagGetTry<ITaskItem[]>(AttributeName.InputFiles);
			}
			set
			{
				BagSet(AttributeName.InputFiles, value);
			}
		}

		/// <summary>
		/// The file that contains the private keys to use for signing.
		/// </summary>
		[Required]
		public ITaskItem KeyFile
		{
			get
			{
				return BagGetTry<ITaskItem>(AttributeName.KeyFile);
			}
			set
			{
				BagSet(AttributeName.KeyFile, value);
			}
		}

		/// <summary>
		/// Specifies an optional server to timestamp the files being signed.
		/// </summary>
		public string TimestampingServer
		{
			get
			{
				return BagGetTry<string>(AttributeName.TimestampingServer);
			}
			set
			{
				BagSet(AttributeName.TimestampingServer, value);
			}
		}

		/// <summary>
		/// Whether the tool output should be verbose.
		/// </summary>
		public bool Verbose
		{
			get
			{
				return BagGetTry<bool>(AttributeName.Verbose);
			}
			set
			{
				BagSet(AttributeName.Verbose, value);
			}
		}

		#endregion

		#region Overrides

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

			// signtool mode
			cmd.AppendSwitch("sign");

			// Verbose output?
			if(BagGet(AttributeName.Verbose, false))
				cmd.AppendSwitch("/v");

			// Key file
			cmd.AppendSwitch("/f");
			cmd.AppendFileNameIfNotNull(BagGet<ITaskItem>(AttributeName.KeyFile));

			// Timestamp
			if(BagContains(AttributeName.TimestampingServer))
			{
				cmd.AppendSwitch("/t");
				cmd.AppendSwitch(BagGet<string>(AttributeName.TimestampingServer));
			}

			// The files to process
			foreach(ITaskItem item in BagGet<ITaskItem[]>(AttributeName.InputFiles))
				cmd.AppendFileNameIfNotNull(item);

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
				return "signtool.exe";
			}
		}

		#endregion
	}
}