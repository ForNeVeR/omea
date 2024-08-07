﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Build.Omea.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Tasks
{
	/// <summary>
	/// Converts a .reg file into a .wxs file with Registry entries and a dummy structure around them to provide for an XSD-valid file.
	/// </summary>
	public class Reg2Wxs : WixTask
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the full path to the input REG file.
		/// </summary>
		[Required]
		public ITaskItem InputFile
		{
			get
			{
				return Bag.Get<ITaskItem>(AttributeName.InputFile);
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				Bag.Set(AttributeName.InputFile, value);
			}
		}

		/// <summary>
		/// Gets or sets the full path to the output WiX source code file.
		/// </summary>
		[Required]
		public ITaskItem OutputFile
		{
			get
			{
				return Bag.Get<ITaskItem>(AttributeName.OutputFile);
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				Bag.Set(AttributeName.OutputFile, value);
			}
		}

		#endregion
	}
}
