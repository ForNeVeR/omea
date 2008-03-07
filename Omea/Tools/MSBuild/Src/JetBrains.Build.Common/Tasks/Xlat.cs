/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Translates an input string into the output string against the table.
	/// </summary>
	public class Xlat : TaskBase
	{
		#region Attributes

		/// <summary>
		/// The input string to be translated.
		/// </summary>
		[Required]
		public string Input
		{
			get
			{
				return (string)Bag[AttributeName.Input];
			}
			set
			{
				Bag[AttributeName.Input] = value;
			}
		}

		/// <summary>
		/// The translation result.
		/// </summary>
		[Output]
		public string Result
		{
			get
			{
				return (string)Bag[AttributeName.Result];
			}
			set
			{
				Bag[AttributeName.Result] = value;
			}
		}

		/// <summary>
		/// The translation table.
		/// The “Input” and “Output” metadata of each item define the translation, the item spec is ignored.
		/// </summary>
		[Required]
		public ITaskItem[] Table
		{
			get
			{
				return (ITaskItem[])Bag[AttributeName.Table];
			}
			set
			{
				Bag[AttributeName.Table] = value;
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
			string sInput = GetStringValue(AttributeName.Input);
			string sResult = null;

			ITaskItem itemNoneOfTheAbove = null;
			foreach(ITaskItem item in GetValue<ITaskItem[]>(AttributeName.Table))
			{
				if(item.GetMetadata(AttributeName.Input.ToString()) == null)
					throw new InvalidOperationException(string.Format("The item “{0}” is missing the Input metadata.", item.ItemSpec));
				if(string.IsNullOrEmpty(item.GetMetadata(AttributeName.Result.ToString())))
					throw new InvalidOperationException(string.Format("The item “{0}” is missing the Result metadata.", item.ItemSpec));

				// “None of the above” item
				if(item.GetMetadata(AttributeName.Input.ToString()).Length == 0)
				{
					if(itemNoneOfTheAbove != null)
						throw new InvalidOperationException(string.Format("There's more than one empty-Input (“None of the above”) item."));
					itemNoneOfTheAbove = item;
					continue;
				}

				// Normal item, match
				if(item.GetMetadata(AttributeName.Input.ToString()) == sInput)
				{
					if(sResult != null)
						throw new InvalidOperationException(string.Format("There is more than one item matching the “{0}” input.", sInput));
					sResult = item.GetMetadata(AttributeName.Result.ToString());
				}
			}

			// None of the above case
			if((sResult == null) && (itemNoneOfTheAbove != null))
				sResult = itemNoneOfTheAbove.GetMetadata(AttributeName.Result.ToString());

			if(sResult == null)
				throw new InvalidOperationException(string.Format("Could not locate an entry for the “{0}” item in the table.", sInput));

			Log.LogMessage(MessageImportance.Low, "Xlatted “{0}” into “{1}”.", sInput, sResult);

			Result = sResult;
		}

		#endregion
	}
}