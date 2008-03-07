/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.CoreServicesEx.Scheduller
{
	/// <summary>
	/// Data entries for the Scheduller.
	/// </summary>
	internal class SchedullerData : ISchedullerData
	{
		#region Data

		/// <summary>
		/// Stores the lazy-init singleton instance.
		/// </summary>
		protected static ISchedullerData _instance = null;

		private string _sSchedullerTaskResourceTypeName = "Scheduller.Task";

		private int _nSchedullerTaskResourceType = 0;

		private string _sSchedullerGroupResourceTypeName = "Scheduller.Group";

		private int _nSchedullerGroupResourceType = 0;

		#endregion

		#region Init

		/// <summary>
		/// Non-public singleton ctor.
		/// </summary>
		protected SchedullerData()
		{
		}

		#endregion

		#region Attributes

		public static ISchedullerData Instance
		{
			get
			{
				return _instance ?? (_instance = new SchedullerData());
			}
		}

		#endregion

		#region ISchedullerData Members

		/// <summary>
		/// Gets the resource type … for the … resource.
		/// </summary>
		public string SchedullerTaskResourceTypeName
		{
			get
			{
				return _sSchedullerTaskResourceTypeName;
			}
		}

		/// <summary>
		/// Gets the resource type … for the … resource.
		/// </summary>
		public int SchedullerTaskResourceType
		{
			get
			{
				return _nSchedullerTaskResourceType;
			}
		}

		/// <summary>
		/// Gets the resource type … for the … resource.
		/// </summary>
		public string SchedullerGroupResourceTypeName
		{
			get
			{
				return _sSchedullerGroupResourceTypeName;
			}
		}

		/// <summary>
		/// Gets the resource type … for the … resource.
		/// </summary>
		public int SchedullerGroupResourceType
		{
			get
			{
				return _nSchedullerGroupResourceType;
			}
		}

		#endregion
	}
}