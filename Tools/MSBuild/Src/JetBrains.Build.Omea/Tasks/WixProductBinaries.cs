// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;

using JetBrains.Build.Omea.Infra;

using Microsoft.Build.Framework;

namespace JetBrains.Build.Omea.Tasks
{
	/// <summary>
	/// Creates a WiX source with the product biaries described in it.
	/// </summary>
	public class WixProductBinaries : WixAndProductTask
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
		/// Gets or sets whether to include the debug info databases along with the assemblies.
		/// </summary>
		[Required]
		public bool IncludePdb
		{
			get
			{
				return Bag.Get<bool>(AttributeName.IncludePdb);
			}
			set
			{
				Bag.Set(AttributeName.IncludePdb, value);
			}
		}

		/// <summary>
		/// Gets or sets whether to include the Publisher Policy assembly files along with the assemblies.
		/// When set to <c>True</c>, at least one pubilsher policy assembly is required for each of the assemblies.
		/// </summary>
		[Required]
		public bool IncludePublisherPolicy
		{
			get
			{
				return Bag.Get<bool>(AttributeName.IncludePublisherPolicy);
			}
			set
			{
				Bag.Set(AttributeName.IncludePublisherPolicy, value);
			}
		}

		/// <summary>
		/// Gets or sets whether to include the XmlDoc xml files along with the assemblies.
		/// </summary>
		[Required]
		public bool IncludeXmlDoc
		{
			get
			{
				return Bag.Get<bool>(AttributeName.IncludeXmlDoc);
			}
			set
			{
				Bag.Set(AttributeName.IncludeXmlDoc, value);
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
		/// Requires all of the assemblies in the AllAssembliesXml list to have a strong name.
		/// </summary>
		[Required]
		public bool RequireStrongName
		{
			get
			{
				return Bag.Get<bool>(AttributeName.RequireStrongName);
			}
			set
			{
				Bag.Set(AttributeName.RequireStrongName, value);
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

		#region Overrides

		/// <summary>
		/// Gets the list of attributes that must contain the probing directories.
		/// </summary>
		protected override ICollection<AttributeName> ProbingDirectoryAttributes
		{
			get
			{
				var retval = new List<AttributeName>(base.ProbingDirectoryAttributes);
				retval.Add(AttributeName.ProductBinariesDir);
				return retval;
			}
		}

		#endregion
	}
}
