/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

using JetBrains.Annotations;
using JetBrains.Build.InstallationData;

namespace JetBrains.Omea.Base.Install
{
	/// <summary>
	/// Adds one or more arbitrary files to the installation.
	/// The files may reside either in Lib or Bin folders.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class InstallFileAttribute : Attribute
	{
		#region Data

		[NotNull]
		private readonly string _FilesMask;

		[NotNull]
		private readonly string _Id;

		private readonly Guid _MsiGuid;

		[NotNull]
		private readonly string _SourceRelativeDir;

		private readonly SourceRootXml _SourceRoot;

		[NotNull]
		private readonly string _TargetRelativeDir;

		private readonly TargetRootXml _TargetRoot;

		#endregion

		#region Init

		/// <summary>
		/// Adds one or more arbitrary files to the installation.
		/// </summary>
		/// <param name="targetroot">Base folder on the installation site.</param>
		/// <param name="sTargetRelativeDir">Relative path from the base folder on the installation site.</param>
		/// <param name="sourceroot">Base folder on the compilation site.</param>
		/// <param name="sSourceRelativeDir">Relative path from the base folder on the compilation site.</param>
		/// <param name="sFilesMask">Mask for picking the files from the folder on the compilation site (source). More than one file is OK. File names will be the same on the installation site (target).</param>
		/// <param name="id">The unique identifier for this installation entry.</param>
		public InstallFileAttribute([NotNull] string id, TargetRootXml targetroot, [NotNull] string sTargetRelativeDir, SourceRootXml sourceroot, [NotNull] string sSourceRelativeDir, [NotNull] string sFilesMask, string sMsiGuid)
		{
			if(sTargetRelativeDir == null)
				throw new ArgumentNullException("sTargetRelativeDir");
			if(sSourceRelativeDir == null)
				throw new ArgumentNullException("sSourceRelativeDir");
			if(sFilesMask == null)
				throw new ArgumentNullException("sFilesMask");
			if(id == null)
				throw new ArgumentNullException("id");

			_TargetRoot = targetroot;
			_Id = id;
			_TargetRelativeDir = sTargetRelativeDir;
			_SourceRoot = sourceroot;
			_SourceRelativeDir = sSourceRelativeDir;
			_FilesMask = sFilesMask;
			_MsiGuid = new Guid(sMsiGuid);
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Mask for picking the files from the folder on the compilation site (source). More than one file is OK. File names will be the same on the installation site (target).
		/// </summary>
		[NotNull]
		public string FilesMask
		{
			get
			{
				return _FilesMask;
			}
		}

		/// <summary>
		/// The unique identifier for this installation entry.
		/// </summary>
		[NotNull]
		public string Id
		{
			get
			{
				return _Id;
			}
		}

		public Guid MsiGuid
		{
			get
			{
				return _MsiGuid;
			}
		}

		/// <summary>
		/// Relative path from the base folder on the compilation site.
		/// </summary>
		[NotNull]
		public string SourceRelativeDir
		{
			get
			{
				return _SourceRelativeDir;
			}
		}

		/// <summary>
		/// Base folder on the compilation site.
		/// </summary>
		public SourceRootXml SourceRoot
		{
			get
			{
				return _SourceRoot;
			}
		}

		/// <summary>
		/// Relative path from the base folder on the installation site.
		/// </summary>
		[NotNull]
		public string TargetRelativeDir
		{
			get
			{
				return _TargetRelativeDir;
			}
		}

		/// <summary>
		/// Base folder on the installation site.
		/// </summary>
		public TargetRootXml TargetRoot
		{
			get
			{
				return _TargetRoot;
			}
		}

		#endregion
	}
}