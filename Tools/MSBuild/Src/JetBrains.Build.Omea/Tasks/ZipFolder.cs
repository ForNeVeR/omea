// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;

using JetBrains.Build.Omea.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Tasks
{
	/// <summary>
	/// Compresses all the files in a folder into a ZIP archive.
	/// </summary>
	public class ZipFolder : TaskBase
	{
		#region Attributes

		[Required]
		public ITaskItem Directory
		{
			get
			{
				return Bag.Get<ITaskItem>(AttributeName.Directory);
			}
			set
			{
				Bag.Set(AttributeName.Directory, value);
			}
		}

		[Required]
		public ITaskItem OutputFile
		{
			get
			{
				return Bag.Get<ITaskItem>(AttributeName.OutputFile);
			}
			set
			{
				Bag.Set(AttributeName.OutputFile, value);
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
			var directory = new DirectoryInfo(Bag.Get<ITaskItem>(AttributeName.Directory).GetMetadata("FullPath"));
			if(!directory.Exists)
				throw new InvalidOperationException(string.Format("The directory “{0}” does not exist.", directory.FullName));
			var fileOutput = new FileInfo(Bag.Get<ITaskItem>(AttributeName.OutputFile).GetMetadata("FullPath"));
			Log.LogMessage("Zipping directory “{0}” into “{1}”.", directory.FullName, fileOutput.FullName);
			new FastZip().CreateZip(fileOutput.FullName, directory.FullName, true, null);
		}

		#endregion
	}
}
