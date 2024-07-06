// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

using JetBrains.Build.Omea.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Tasks
{
	/// <summary>
	/// Creates a WiX source with the product biaries described in it.
	/// </summary>
	public class WixProductReferences : WixAndProductTask
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the ID of the media into which the binaries will be packed.
		/// </summary>
		[Required]
		public string DiskId
		{
			get
			{
				return Bag.Get(AttributeName.DiskId, 0).ToString();
			}
			set
			{
				Bag.Set(AttributeName.DiskId, int.Parse(value));
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

		/// <summary>
		/// Gets or sets the product references dir location.
		/// </summary>
		[Required]
		public ITaskItem ProductReferencesDir
		{
			get
			{
				return Bag.Get<ITaskItem>(AttributeName.ProductReferencesDir);
			}
			set
			{
				Bag.Set(AttributeName.ProductReferencesDir, value);
			}
		}

		/// <summary>
		/// Gets or sets the WiX ComponentGroup ID that will be created in the fragment and populated with the newly-created components, so that it could be mounted into the feature tree.
		/// </summary>
		[Required]
		public string WixComponentGroupId
		{
			get
			{
				return Bag.Get<string>(AttributeName.WixComponentGroupId);
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				Bag.Set(AttributeName.WixComponentGroupId, value);
			}
		}

		/// <summary>
		/// Gets or sets the WiX Directory ID in the WiX sources into which the created components should be mounted.
		/// The directory must be defined somewhere in the directories tree, and a <c>DirectoryRef</c> element will be added to the generated source file referencing the given ID.
		/// </summary>
		[Required]
		public string WixDirectoryId
		{
			get
			{
				return Bag.Get<string>(AttributeName.WixDirectoryId);
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				Bag.Set(AttributeName.WixDirectoryId, value);
			}
		}

		#endregion
	}
}
