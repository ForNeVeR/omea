// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Reflection;

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Throws if any of the input file is missing a strong name.
	/// </summary>
	public class AssertStrongName : TaskBase
	{
		#region Attributes

		/// <summary>
		/// Specifies the list of the files to check for the strong name.
		/// </summary>
		[Required]
		public ITaskItem[] InputFiles
		{
			get
			{
				return BagGet<ITaskItem[]>(AttributeName.InputFiles);
			}
			set
			{
				BagSet(AttributeName.InputFiles, value);
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
			foreach(ITaskItem item in BagGet<ITaskItem[]>(AttributeName.InputFiles))
			{
				var fi = new FileInfo(item.GetMetadata("FullPath"));
				if(!fi.Exists)
					throw new InvalidOperationException(string.Format("The assembly file “{0}” could not be found.", fi.FullName));
				AssemblyName assemblyname = AssemblyName.GetAssemblyName(fi.FullName);
				byte[] token = assemblyname.GetPublicKeyToken();
				if((token == null) || (token.Length == 0))
					throw new InvalidOperationException(string.Format("The assembly “{0}” from file “{1}” is missing a strong name.", assemblyname.FullName, fi.FullName));
			}
		}

		#endregion
	}
}
