// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Security;

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;
using Microsoft.Build.Tasks.Deployment.ManifestUtilities;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Signs the files with Microsoft Authenticode.
	/// Note: this file calls the MSBuild Security Utilities to sign the file, instead of calling SignTool.exe manually.
	/// </summary>
	public class Sign_SecurityUtilities : TaskBase
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
		/// An optional password to the key file.
		/// </summary>
		public string Password
		{
			get
			{
				return BagGet<string>(AttributeName.Password);
			}
			set
			{
				BagSet(AttributeName.Password, value);
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

		#endregion

		#region Overrides

		/// <summary>
		/// The method to be overriden in inheriting tasks.
		/// Throw an exception in case of an errror.
		/// </summary>
		protected override void ExecuteTask()
		{
			// Keyfile password
			var password = new SecureString();
			foreach(char c in BagGet(AttributeName.Password, ""))
				password.AppendChar(c);

			// Timestamping server
			string sTimestampServer = BagGet(AttributeName.TimestampingServer, "");
			Uri uriTimestampServer = string.IsNullOrEmpty(sTimestampServer) ? null : new Uri(sTimestampServer);
			if(uriTimestampServer == null)
				Log.LogWarning("It would be better to specify the Timestamping Server Uri.");

			// Sign each file
			foreach(ITaskItem item in BagGet<ITaskItem[]>(AttributeName.InputFiles))
				SecurityUtilities.SignFile(GetStringValue(AttributeName.KeyFile), password, uriTimestampServer, item.GetMetadata("FullPath"));
		}

		#endregion
	}
}
