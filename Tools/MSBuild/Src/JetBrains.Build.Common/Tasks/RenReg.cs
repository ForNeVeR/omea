// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Text.RegularExpressions;

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Supports RegEx rename of files.
	/// </summary>
	public class RenReg : TaskBase
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the directory to look for the files.
		/// </summary>
		[Required]
		public string Directory
		{
			get
			{
				return (string)Bag[AttributeName.Directory];
			}
			set
			{
				Bag[AttributeName.Directory] = value;
			}
		}

		/// <summary>
		/// Gets or sets the matching pattern.
		/// </summary>
		[Required]
		public string What
		{
			get
			{
				return (string)Bag[AttributeName.What];
			}
			set
			{
				Bag[AttributeName.What] = value;
			}
		}

		/// <summary>
		/// Gets or sets the replacement string.
		/// </summary>
		[Required]
		public string With
		{
			get
			{
				return (string)Bag[AttributeName.With];
			}
			set
			{
				Bag[AttributeName.With] = value;
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
			var di = new DirectoryInfo(GetStringValue(AttributeName.Directory));
			if(!di.Exists)
				throw new InvalidOperationException(string.Format("The directory “{0}” does not exist.", di.FullName));

			var regex = new Regex(GetStringValue(AttributeName.What), RegexOptions.IgnoreCase | RegexOptions.Singleline);

			string sWith = GetStringValue(AttributeName.With);

			foreach(FileInfo fi in di.GetFiles("*"))
			{
				if(!regex.IsMatch(fi.Name))
					continue;

				string sNewName = regex.Replace(fi.Name, sWith);
				Log.LogMessage(MessageImportance.Low, string.Format("“{0}” -> “{1}”", fi.Name, sNewName));

				fi.MoveTo(Path.Combine(di.FullName, sNewName));
			}
		}

		#endregion
	}
}
