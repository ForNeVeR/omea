// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Build.Omea.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Tasks
{
	/// <summary>
	/// Creates a WiX source with the product Registry data included in it.
	/// </summary>
	public class WixInstallationData : WixAndProductTask
	{
		#region Attributes

		/// <summary>
		/// Gets or sets the ID of the media into which the components will be generated.
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
		/// Gets or sets the path to the file that caches the component GUIDs for the generated components.
		/// </summary>
		[Required]
		public ITaskItem GuidCacheFile
		{
			get
			{
				return Bag.Get<ITaskItem>(AttributeName.GuidCacheFile);
			}
			set
			{
				Bag.Set(AttributeName.GuidCacheFile, value);
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
				Bag.Set(AttributeName.OutputFile, value);
			}
		}

		/// <summary>
		/// Gets or sets the folder where the product home is located.
		/// </summary>
		[Required]
		public ITaskItem ProductHomeDir
		{
			get
			{
				return Bag.Get<ITaskItem>(AttributeName.ProductHomeDir);
			}
			set
			{
				Bag.Set(AttributeName.ProductHomeDir, value);
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
				Bag.Set(AttributeName.WixDirectoryId, value);
			}
		}

		#endregion
	}
}
