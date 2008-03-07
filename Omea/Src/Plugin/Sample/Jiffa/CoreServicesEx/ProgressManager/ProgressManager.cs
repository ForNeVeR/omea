/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenApiEx;

namespace JetBrains.Omea.CoreServicesEx.ProgressManager
{
	internal class ProgressManager : IProgressManager
	{
		/// <summary>
		/// Non-public singleton ctor.
		/// </summary>
		protected ProgressManager()
		{
		}

		/// <summary>
		/// A lazy-init singleton instance.
		/// </summary>
		private static IProgressManager _instance = null;

		/// <summary>
		/// Gets the container for various ProgressManager-related data and information, such as resource types and so on.
		/// </summary>
		public IProgressManagerData Data
		{
			get
			{
				return ProgressManagerData.Instance;
			}
		}

		/// <summary>
		/// Gets the single instance of the manager.
		/// </summary>
		public static IProgressManager Instance
		{
			get
			{
				return _instance ?? (_instance = new ProgressManager());
			}
		}
	}
}