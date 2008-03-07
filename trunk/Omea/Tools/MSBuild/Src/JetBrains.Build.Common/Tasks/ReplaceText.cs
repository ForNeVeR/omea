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

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Replaces text in the text files against a regexp.
	/// </summary>
	public class ReplaceText : TaskBase
	{
		#region Attributes

		/// <summary>
		/// Gets or sets whether the search should be case-sensitive.
		/// </summary>
		[Required]
		public bool CaseSensitive
		{
			get
			{
				return (bool)Bag[AttributeName.CaseSensitive];
			}
			set
			{
				Bag[AttributeName.CaseSensitive] = value;
			}
		}

		/// <summary>
		/// Gets or sets the files to replace text within.
		/// </summary>
		[Required]
		public ITaskItem[] InputFiles
		{
			get
			{
				return (ITaskItem[])Bag[AttributeName.InputFiles];
			}
			set
			{
				Bag[AttributeName.InputFiles] = value;
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

		#region Implementation

		/// <summary>
		/// Processes a single file.
		/// </summary>
		private static void ReplaceTextInFile(string pathname, Regex what, string with)
		{
			var fi = new FileInfo(pathname);
			if(!fi.Exists)
				throw new InvalidOperationException(string.Format("The file “{0}” does not exist.", fi.FullName));

			// Read
			string sInput;
			using(Stream stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
			using(var sr = new StreamReader(stream))
				sInput = sr.ReadToEnd();

			// Replace
			string sOutput = what.Replace(sInput, with);

			// Write
			using(Stream stream = fi.Open(FileMode.Create, FileAccess.Write, FileShare.Read))
			using(var sw = new StreamWriter(stream, Encoding.UTF8))
				sw.Write(sOutput);
		}

		#endregion

		#region Overrides

		/// <summary>
		/// The method to be overriden in inheriting tasks.
		/// Throw an exception in case of an errror.
		/// </summary>
		protected override void ExecuteTask()
		{
			var regex = new Regex(GetStringValue(AttributeName.What), RegexOptions.Multiline | (GetValue<bool>(AttributeName.CaseSensitive) ? 0 : RegexOptions.IgnoreCase));
			string sWith = GetStringValue(AttributeName.With);

			foreach(ITaskItem item in GetValue<ITaskItem[]>(AttributeName.InputFiles))
				ReplaceTextInFile(item.ItemSpec, regex, sWith);
		}

		#endregion
	}
}