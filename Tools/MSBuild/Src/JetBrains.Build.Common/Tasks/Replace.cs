// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text.RegularExpressions;

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Performs RegexReplace on a string.
	/// </summary>
	public class Replace : TaskBase
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
		/// Gets or sets whether the task should fail when no replacements were made.
		/// </summary>
		public bool FailOnNoMatch { get; set; }

		/// <summary>
		/// Gets whether there were any replacements, that is, the string was ever matched.
		/// </summary>
		[Output]
		public bool IsMatch { get; set; }

		/// <summary>
		/// On input, specifies the source text on which the replace should be performed.
		/// On output, gives the results of the replacement.
		/// </summary>
		[Output]
		[Required]
		public string Text
		{
			get
			{
				return (string)Bag[AttributeName.Text];
			}
			set
			{
				Bag[AttributeName.Text] = value;
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
			var regex = new Regex(GetStringValue(AttributeName.What), RegexOptions.Singleline | (GetValue<bool>(AttributeName.CaseSensitive) ? 0 : RegexOptions.IgnoreCase));
			string sWith = With ?? "";
			string sWhat = GetStringValue(AttributeName.Text);

			// Check for the matches
			IsMatch = regex.IsMatch(sWhat);
			if((!IsMatch) && (FailOnNoMatch))
				throw new InvalidOperationException(string.Format("The input string does not match the search pattern."));

			// Replace
			Text = regex.Replace(sWhat, sWith);
		}

		#endregion
	}
}
