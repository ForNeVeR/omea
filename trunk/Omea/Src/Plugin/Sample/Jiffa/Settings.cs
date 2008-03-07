/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Jiffa
{
	public static class JiffaSettings
	{
		/// <summary>
		/// Gets or sets the list of valid assignees.
		/// </summary>
		public static string DevelopersList
		{
			get
			{
				return Core.SettingStore.ReadString("Jiffa.Submission", "DevelopersList", DevelopersList_Default);
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				Core.SettingStore.WriteString("Jiffa.Submission", "DevelopersList", value);
			}
		}

		/// <summary>
		/// Default for the <see cref="DevelopersList"/>.
		/// </summary>
		public static string DevelopersList_Default
		{
			get
			{
				return "andrew.serebryansky\nands\ndsl\ndsha\npasynkov\neugene.petrenko\norangy\nobfuscator\nww\noleg.stepanov@jetbrains.com\nolka\nbaltic\ncoox\nvalentin";
			}
		}

		/// <summary>
		/// Gets or sets the news reply template.
		/// </summary>
		public static string Template
		{
			get
			{
				return Core.SettingStore.ReadString("Jiffa.Submission", "Template", Template_Default);
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				Core.SettingStore.WriteString("Jiffa.Submission", "Template", value);
			}
		}

		/// <summary>
		/// Default for the <see cref="Template"/>.
		/// </summary>
		public static string Template_Default
		{
			get
			{
				//return "Hello,\n\nWhy, your issue sounds worth creating a JIRA request.\n\nYou could monitor its progress at <%=IssueUri%>.\n\n--\nDev Team";
				return "Hello,\n\nWe appreciate your feedback.\n\nThe corresponding JIRA request has been created, and you are welcome to monitor its status at <%=IssueUri%>.\n\nBest regards,\n - Development Team.";
			}
		}

		/// <summary>
		/// Gets or sets the JIRA project to which the requests should be submitted.
		/// <c>Null</c> if not set.
		/// </summary>
		public static JiraProject SubmitToProject
		{
			get
			{
				int nResId = Core.SettingStore.ReadInt("Jiffa.Submission", "SubmitToProject", -1);
				IResource res = Core.ResourceStore.TryLoadResource(nResId);
				return res != null ? JiraProject.FromResource(res) : null;
			}
			set
			{
				int nResId = -1;
				if(value != null)
					nResId = value.Resource.Id;
				Core.SettingStore.WriteInt("Jiffa.Submission", "SubmitToProject", nResId);
			}
		}

		/// <summary>
		/// Gets or sets the regexp mask that should fetch the build number from the news article title.
		/// </summary>
		public static string BuildNumberMask
		{
			get
			{
				return Core.SettingStore.ReadString("Jiffa.Submission", "BuildNumberMask", BuildNumberMask_Default);
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				Core.SettingStore.WriteString("Jiffa.Submission", "BuildNumberMask", value);
			}
		}

		/// <summary>
		/// Default for <see cref="BuildNumberMask"/>.
		/// </summary>
		public static string BuildNumberMask_Default
		{
			get
			{
				return @"\(([^)]+)\)|\[([^]]+)\]|\#(\w+)\b";
			}
		}

		/// <summary>
		/// The DisplayName of the “Build” custom field in JIRA.
		/// </summary>
		public static string CustomFieldNames_BuildNumber
		{
			get
			{
				return Core.SettingStore.ReadString("Jiffa.Submission", "CustomFieldNames.BuildNumber", CustomFieldNames_BuildNumber_Default);
			}
			set
			{
				Core.SettingStore.WriteString("Jiffa.Submission", "CustomFieldNames.BuildNumber", value);
			}
		}

		/// <summary>
		/// Default for the <see cref="CustomFieldNames_BuildNumber"/>.
		/// </summary>
		public static string CustomFieldNames_BuildNumber_Default
		{
			get
			{
				return "Build";
			}
		}

		/// <summary>
		/// The DisplayName of the “Original URI” custom field in JIRA.
		/// </summary>
		public static string CustomFieldNames_OriginalUri
		{
			get
			{
				return Core.SettingStore.ReadString("Jiffa.Submission", "CustomFieldNames.OriginalUri", CustomFieldNames_OriginalUri_Default);
			}
			set
			{
				Core.SettingStore.WriteString("Jiffa.Submission", "CustomFieldNames.OriginalUri", value);
			}
		}

		/// <summary>
		/// Default for the <see cref="CustomFieldNames_OriginalUri"/>.
		/// </summary>
		public static string CustomFieldNames_OriginalUri_Default
		{
			get
			{
				return "Old URL";
			}
		}

		/// <summary>
		/// Gets or sets whether the MRU values should be restored in the dialogs when they are opened.
		/// </summary>
		public static bool MruEnabled
		{
			get
			{
				return Core.SettingStore.ReadBool("Jiffa.Submission", "CustomFieldNames.MruEnabled", MruEnabled_Default);
			}
			set
			{
				Core.SettingStore.WriteBool("Jiffa.Submission", "CustomFieldNames.MruEnabled", value);
			}
		}

		/// <summary>
		/// Default for the <see cref="MruEnabled"/>.
		/// </summary>
		public static bool MruEnabled_Default
		{
			get
			{
				return true;
			}
		}
	}
}