/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.Jiffa
{
	public static class Types
	{
		/// <summary>
		/// JiraServer resource type.
		/// </summary>
		public static readonly string JiraServer = "JiraServer";

		public static readonly string JiraProject = "JiraProject";

		public static readonly string JiraComponent = "JiraComponent";

		public static readonly string JiraIssueType = "JiraIssueType";

		public static readonly string JiraStatus = "JiraStatus";

		public static readonly string JiraPriority = "JiraPriority";

		public static readonly string JiraCustomField = "JiraCustomField";

		public static readonly string JiraUser = "JiraUser";
	}
}

namespace JetBrains.Omea.Jiffa
{
	public static class Links
	{
	}
}

namespace JetBrains.Omea.Jiffa
{
	public static class Props
	{
		public static readonly string Uri = "Uri";

		public static readonly string Username = "Username";

		public static readonly string Password = "Password";

		public static readonly string JiraId = "JiraId";

		public static readonly string Key = "JiraKey";

		public static readonly string ProjectUri = "JiraProjectUri";

		public static readonly string IconUri = "JiraIconUri";

		public static readonly string Color = "JiraColor";

		public static readonly string Email = "JiraEmail";

		public static readonly string FullName = "JiraFullName";

		public static string[] All
		{
			get
			{
				return new string[] {Uri, Username, Password, JiraId, Key, ProjectUri, IconUri, Color, Email, FullName};
			}
		}

		public static string[] StringProps
		{
			get
			{
				return All;
			}
		}
	}
}