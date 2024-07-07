// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Globalization;

using JetBrains.Build.Common.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Common.Tasks
{
	/// <summary>
	/// Invokes the <see cref="string.Format(string,object[])"/> function for the given arguments.
	/// </summary>
	public class StringFormat : TaskBase
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the list of arguments to use in formatting.
		/// If you need to specify number-specific etc formatting, use <see cref="ArgumentTypes"/>.
		/// </summary>
		public string[] Arguments { get; set; }

		/// <summary>
		/// If specified, a collection of CLR type names of the <see cref="Arguments"/>.
		/// The length of the argument types collection must be equal to the number of <see cref="Arguments"/>.
		/// The CLR type names are case-insensitive full names of types from <c>mscorlib</c>, or assembly-qualified names in case of other assemblies.
		/// </summary>
		public string[] ArgumentTypes { get; set; }

		/// <summary>
		/// Specifies the culture info for the formatting, if applicable.
		/// By default, that's <see cref="System.Globalization.CultureInfo.InvariantCulture"/>.
		/// </summary>
		public string CultureInfo { get; set; }

		/// <summary>
		/// Gets or sets the format string.
		/// </summary>
		public string Format { get; set; }

		/// <summary>
		/// Gets the resulting formatted string.
		/// </summary>
		[Output]
		public string Result { get; set; }

		#endregion

		#region Implementation

		private object[] GetArguments()
		{
			string[] argumentstrings = Arguments ?? new string[] {};

			// If there are types specified, make the conversion
			object[] @params;
			if((ArgumentTypes != null) && (ArgumentTypes.Length != 0))
			{
				if(ArgumentTypes.Length != Arguments.Length)
					throw new InvalidOperationException(string.Format("There are {0} arguments and {1} argument types. There should either be no argument types, or exactly as many as the arguments.", argumentstrings.Length, ArgumentTypes.Length));

				@params = new object[argumentstrings.Length];

				for(int nArg = 0; nArg < argumentstrings.Length; nArg++)
					@params[nArg] = TypeDescriptor.GetConverter(Type.GetType(ArgumentTypes[nArg], true, true)).ConvertFromInvariantString(argumentstrings[nArg]);
			}
			else
				@params = argumentstrings;

			return @params;
		}

		private CultureInfo GetCultureInfo()
		{
			CultureInfo cultureinfo;
			if(string.IsNullOrEmpty(CultureInfo))
				cultureinfo = System.Globalization.CultureInfo.InvariantCulture;
			else
			{
				cultureinfo = (CultureInfo)TypeDescriptor.GetConverter(typeof(CultureInfo)).ConvertFromInvariantString(CultureInfo);
				if(cultureinfo == null)
					throw new InvalidOperationException(string.Format("Could not parse the culture info."));
			}
			return cultureinfo;
		}

		private string GetFormatString()
		{
			return Format ?? "";
		}

		#endregion

		#region Overrides

		/// <summary>
		/// The method to be overriden in inheriting tasks.
		/// Throw an exception in case of an errror.
		/// </summary>
		protected override void ExecuteTask()
		{
			Result = string.Format(GetCultureInfo(), GetFormatString(), GetArguments());
		}

		#endregion
	}
}
