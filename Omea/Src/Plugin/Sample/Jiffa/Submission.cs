// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using JetBrains.Omea.Base;
using JetBrains.Omea.Jiffa.JiraSoap;
using JetBrains.Omea.Jiffa.Res;
using JetBrains.Omea.Nntp;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Jiffa
{
	public class Submission
	{
		private readonly JiraProject _project;

		protected string _title = "";

		protected string _body = "";

		protected JiraComponent _component;

		protected JiraIssueType _issuetype;

		/// <summary>
		/// Issue priority, <c>Null</c> for server default.
		/// </summary>
		protected JiraPriority _priority;

		protected string _buildnumber = "";

		protected JiraStatus _status;

		protected string _assignee = "";

		protected RemoteIssue _issue = null;

		protected string _originaluri = "";

		protected StringBuilder _errorlog = new StringBuilder();

		protected Dictionary<string, byte[]> _attachments = new Dictionary<string, byte[]>();

		/// <summary>
		/// While submit is in progress, holds the progress dialog.
		/// Otherwise, <c>Null.</c>
		/// </summary>
		protected ProgressDialog _wndSubmitProgress = null;

		public Submission(JiraProject project)
		{
			_project = project;

			// Init the values
			Component = null;
			Status = null;
			Assignee = "";
			BuildNumber = "";
			if(Project.Server.IssueTypes.Count > 0)
				IssueType = Project.Server.IssueTypes[0];
			Priority = null; // Server-defined
		}

		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();
				if(value.Length == 0)
					throw new ArgumentException(Stringtable.Error_TitleEmpty);
				_title = value;
			}
		}

		public string Body
		{
			get
			{
				return _body;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();
				if(value.Length == 0)
					throw new ArgumentException(Stringtable.Error_BodyEmpty);
				_body = value;
			}
		}

		/// <summary>
		/// The component to which the issue will be submitted.
		/// <c>Null</c> is also allowed.
		/// </summary>
		public JiraComponent Component
		{
			get
			{
				return _component;
			}
			set
			{
				if(value != null)
				{
					if(value.Project != Project)
						throw new ArgumentException(Stringtable.Error_ComponentOfWrongProject);
				}
				_component = value;
			}
		}

		public JiraIssueType IssueType
		{
			get
			{
				return _issuetype;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("IssueType");
				if(value.Server != Project.Server)
					throw new ArgumentNullException(Stringtable.Error_IssueTypeOfWrongServer);
				_issuetype = value;
			}
		}

		/// <summary>
		/// Gets or sets the issue priority. May be <c>Null</c> for the server to use the default value.
		/// </summary>
		public JiraPriority Priority
		{
			get
			{
				return _priority;
			}
			set
			{
				if((value != null) && (value.Server != Project.Server))
					throw new ArgumentNullException(Stringtable.Error_PriorityOfWrongServer);
				_priority = value;
			}
		}

		public string BuildNumber
		{
			get
			{
				return _buildnumber;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("BuildNumber");
				_buildnumber = value;
			}
		}

		public JiraProject Project
		{
			get
			{
				return _project;
			}
		}

		public JiraStatus Status
		{
			get
			{
				return _status;
			}
			set
			{
				if(value != null)
				{
					if(value.Server != Project.Server)
						throw new ArgumentException(Stringtable.Error_ComponentOfWrongProject);
				}
				_status = value;
			}
		}

		/// <summary>
		/// The assignee for the issue.
		/// May be an empty string, must not be <c>Null</c>.
		/// </summary>
		public string Assignee
		{
			get
			{
				return _assignee;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("Assignee");
				_assignee = value;
			}
		}

		/// <summary>
		/// The URI that has originated the issue.
		/// </summary>
		public string OriginalUri
		{
			get
			{
				return _originaluri;
			}
			set
			{
				_originaluri = value;
			}
		}

		/// <summary>
		/// Gets the error log of the instance.
		/// Logs the non-fatal errors (warnings).
		/// </summary>
		public StringBuilder ErrorLog
		{
			get
			{
				return _errorlog;
			}
		}

		/// <summary>
		/// Gets the most recently submitted issue.
		/// </summary>
		public RemoteIssue Issue
		{
			get
			{
				return _issue;
			}
		}

		/// <summary>
		/// Gets the list of attachments associated with this issue.
		/// </summary>
		public Dictionary<string, byte[]> Attachments
		{
			get
			{
				return _attachments;
			}
		}

		/// <summary>
		/// Reads the property values from a news article resource.
		/// </summary>
		public void ReadArticle(IResource res)
		{
			if(res == null)
				throw new ArgumentNullException("res");
			if(res.Type != "Article")
				throw new ArgumentException("A news article resource of type “Article” expected.", "res");

			Title = res.GetPropText(Core.Props.Subject);
			OriginalUri = CopyArticleURLAction.GetArticleUri(res);

			ReadArticle_ExtractBuildNumber(res);
			ReadArticle_PickAttachments(res);
			ReadArticle_FormatBody(res);
		}

		/// <summary>
		/// Extracts the attachments, including the names and their content.
		/// </summary>
		private void ReadArticle_PickAttachments(IResource res)
		{
			IResourceList resAttachments = res.GetLinksTo(null, "NewsAttachment");

			foreach(IResource resAttachment in resAttachments)
			{
				// Att name
				string sName = resAttachment.DisplayName;

				// Prevent duplicate names
				int nMaxTries = 0x20;
				int a;
				for(a = 0; (a < nMaxTries) && ((string.IsNullOrEmpty(sName)) || _attachments.ContainsKey(sName)); a++)
					sName = sName + Jiffa.GetRandomName();
				if(a >= nMaxTries)
					throw new InvalidOperationException("Could not chose an unique name for an attachment.");

				// Att content
				byte[] data = null;
				using(Stream datastream = resAttachment.GetBlobProp("Content"))
				{
					if(datastream != null)
					{
						data = new byte[datastream.Length];
						datastream.Read(data, 0, data.Length);
					}
				}
				data = data ?? new byte[] {};

				// Store
				_attachments.Add(sName, data);
			}
		}

		protected void ReadArticle_ExtractBuildNumber(IResource res)
		{
			BuildNumber = "";

			if(string.IsNullOrEmpty(JiffaSettings.BuildNumberMask))
				return;

			string sSubject = res.GetPropText(Core.Props.Subject);
			if(string.IsNullOrEmpty(sSubject))
				return;

			string sBuildNumber = null;
			Regex regex = new Regex(JiffaSettings.BuildNumberMask);
			foreach(Match match in regex.Matches(sSubject))
			{
				bool first = true;
				foreach(Group group in match.Groups)
				{
					// Skip the first group that represents the whole match
					if(first)
					{
						first = false;
						continue;
					}
					if(!string.IsNullOrEmpty(group.Value))
						sBuildNumber = group.Value;
				}
			}

			if(string.IsNullOrEmpty(sBuildNumber))
				return;

			BuildNumber = sBuildNumber;
		}

		/// <summary>
		/// Prepares the body.
		/// </summary>
		protected void ReadArticle_FormatBody(IResource res)
		{
			string sFromName = res.GetPropText("RawFrom");
			string sContent = res.GetPropText(Core.Props.LongBody);

			IResourceList resNewsgroups = res.GetLinksFrom("NewsGroup", "Newsgroups");
			if(resNewsgroups.Count == 0)
				throw new InvalidOperationException("Could not find a newsgroup for the article.");
			string sNewsgroupName = resNewsgroups[0].GetPropText(Core.Props.Name);

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("This issue has been created from an NNTP article.");
			sb.AppendLine("* Newsgroup: " + sNewsgroupName);
			sb.AppendLine("* From: " + sFromName);
			sb.AppendLine("* Link: " + OriginalUri);
			if(!string.IsNullOrEmpty(BuildNumber))
				sb.AppendLine("* Build Number: " + BuildNumber);
			int nAttachment = 0;
			foreach(KeyValuePair<string, byte[]> pair in _attachments)
			{
				sb.AppendFormat("* Attachment[{0}]: {1} ({2})", nAttachment++, pair.Key, Utils.SizeToString(pair.Value.Length));
				sb.AppendLine();
			}
			sb.AppendLine();
			sb.AppendLine("----");
			sb.AppendLine();
			sb.Append(sContent);

			Body = sb.ToString();
		}

		public void ShowUI(IWin32Window window)
		{
			IssueView view = new IssueView(this);
			view.Show(window);
		}

		/// <summary>
		/// Submits the current configuration to the server.
		/// </summary>
		/// <param name="parent">The parent widnow for the progress window. <c>Null</c> for no visible progress.</param>
		public void Submit(IWin32Window parent)
		{
			if(!Core.UserInterfaceAP.IsOwnerThread)
				throw new InvalidOperationException("The submission must be initiated from the UI thread.");
			if(_wndSubmitProgress != null)
				throw new InvalidOperationException("A submission is already in progress.");
			_issue = null;
			ErrorLog.Length = 0;

			// Create the progress
			_wndSubmitProgress = new ProgressDialog();
			_wndSubmitProgress.Text = Jiffa.Name + " – Submitting Issue";
			_wndSubmitProgress.Image = JiffaIconProvider.LoadIcon("JiraIssue.ico").ToBitmap();
			if(parent != null)
				_wndSubmitProgress.Show(parent);

			// Go to the network thread
			Core.NetworkAP.QueueJob("Submitting JIRA Issue.", (MethodInvoker)Submit_Run);
		}

		/// <summary>
		/// Implements the submission on the network thread.
		/// </summary>
		protected void Submit_Run()
		{
			if(!Core.NetworkAP.IsOwnerThread)
				throw new InvalidOperationException("The submission impl must be run on the network thread.");

			try
			{
				SetStatus("Collecting initial parameters");

				RemoteIssue issue = new RemoteIssue();
				issue.project = Project.Key;
				issue.reporter = Project.Server.Username;
				issue.type = IssueType.JiraId.ToString();
				issue.summary = Title;
				issue.description = Body;

				if(Priority != null)
					issue.priority = Priority.JiraId.ToString();

				if(Component != null)
				{
					RemoteComponent rc = new RemoteComponent();
					rc.id = Component.JiraId;
					issue.components = new RemoteComponent[] {rc};
				}

				if(Status != null)
					issue.status = Status.JiraId;

				issue.assignee = Assignee;

				// Create the issue!
				SetStatus("Creating issue carcass");
				_issue = Project.Server.Service.createIssue(Project.Server.GetSignInToken(), issue);

				// In case JIRA rejects some of the issue params at the creation time, set them one-by-one
				Submit_Run_UpdateIssue();

				// Submit the attachments
				AddAttachmentsToIssue();

				SetStatus("Done");
			}
			catch(Exception ex)
			{
				ErrorLog.AppendFormat("FATAL ERROR. {0}", ex.Message);
			}
			finally
			{
				Core.UserInterfaceAP.QueueJob("JIRA Issue Submission Done.", (MethodInvoker)Submit_Done);
			}
		}

		/// <summary>
		/// Run on the UI thread when the submission is done.
		/// </summary>
		protected void Submit_Done()
		{
			if(!Core.UserInterfaceAP.IsOwnerThread)
				throw new InvalidOperationException("The submission must be terminated on the UI thread.");
			if(_wndSubmitProgress == null)
				throw new InvalidOperationException("A submission is not in progress.");

			_wndSubmitProgress.Visible = false;
			_wndSubmitProgress.Dispose();
			_wndSubmitProgress = null;

			// Notify of the attempt
			if(Issue != null)
				FireIssueSubmitted();
			else
				FireIssueSubmissionFailed();
		}

		/// <summary>
		/// While submitting, updates the status of the process.
		/// Can be invoked from any thread. Will be ignored if there is no progress.
		/// </summary>
		/// <param name="status"></param>
		protected void SetStatus(string status)
		{
			// Transition to the UI thread
			if(!Core.UserInterfaceAP.IsOwnerThread)
			{
				Core.UserInterfaceAP.QueueJob("Set JIRA Issue Submission Status.", (StringDelegate)SetStatus, status);
				return;
			}

			if(_wndSubmitProgress == null)
				return;

			_wndSubmitProgress.StatusText = status;
		}

		protected delegate void StringDelegate(string s);

		/// <summary>
		/// In case JIRA rejects some of the issue params at the creation time, set them one-by-one
		/// Uses the <see cref="Project"/> for project and JIRA server, and <see cref="Issue"/> for the issue-key to update.
		/// </summary>
		protected void Submit_Run_UpdateIssue()
		{
			SetStatus("Preparing fields for the issue update");
			List<RemoteFieldValue> arValues = new List<RemoteFieldValue>();

			// Generic fields
			if(Component != null)
				arValues.Add(CreateFieldValue(JiraIssueType.JiraIssueKeys.components, Component.JiraId));
			if(Priority != null)
				arValues.Add(CreateFieldValue(JiraIssueType.JiraIssueKeys.priority, Priority.JiraId));
			//arValues.Add(CreateFieldValue(JiraIssueType.JiraIssueKeys.reporter, Project.Server.Username));
			arValues.Add(CreateFieldValue(JiraIssueType.JiraIssueKeys.type, IssueType.JiraId));
			arValues.Add(CreateFieldValue(JiraIssueType.JiraIssueKeys.summary, Title));
			arValues.Add(CreateFieldValue(JiraIssueType.JiraIssueKeys.description, Body));
			if(Status != null)
				arValues.Add(CreateFieldValue(JiraIssueType.JiraIssueKeys.status, Status.JiraId));
			arValues.Add(CreateFieldValue(JiraIssueType.JiraIssueKeys.assignee, Assignee));

			// Custom fields
			UpdateIssue_SetCustomFields(arValues);

			// Submit the values to the server
			SetStatus("Updating issue fields");
			foreach(RemoteFieldValue value in arValues)
			{
				try
				{
					SetStatus("Updating issue fields — " + value.id);
					Project.Server.Service.updateIssue(Project.Server.GetSignInToken(), Issue.key, new RemoteFieldValue[] {value});
				}
				catch(Exception ex)
				{
					ErrorLog.AppendFormat("Could not set the “{0}” field on the issue. {1}", value.id, ex.Message);
					ErrorLog.AppendLine();
				}
			}

			// Dirty hack: try opening the issue
			if(Status.Name == "Open")
			{
				try
				{
					SetStatus("Updating issue fields — Opening issue");
					Project.Server.Service.progressWorkflowAction(Project.Server.GetSignInToken(), Issue.key, "Open", new RemoteFieldValue[] {});
				}
				catch(Exception ex)
				{
					ErrorLog.AppendFormat("Could not open the issue. {0}", ex.Message);
					ErrorLog.AppendLine();
				}
			}
		}

		/// <summary>
		/// A part of the <see cref="Submit_Run_UpdateIssue"/> fgunction.
		/// Writes the custom field values into the <paramref name="arValues"/> array.
		/// </summary>
		/// <param name="arValues"></param>
		protected void UpdateIssue_SetCustomFields(List<RemoteFieldValue> arValues)
		{
			SetStatus("Getting the list of editable fields");

			// Get the list of available fields
			RemoteField[] jiraFieldsForEdit = Project.Server.Service.getFieldsForEdit(Project.Server.GetSignInToken(), Issue.key);
			Dictionary<string, string> mapFieldNameToId = new Dictionary<string, string>(jiraFieldsForEdit.Length);
			foreach(RemoteField field in jiraFieldsForEdit)
				mapFieldNameToId[field.name] = field.id;

			// Process the fields
			UpdateIssue_SetCustomFields_Add(arValues, mapFieldNameToId, JiffaSettings.CustomFieldNames_BuildNumber, BuildNumber);
			UpdateIssue_SetCustomFields_Add(arValues, mapFieldNameToId, JiffaSettings.CustomFieldNames_OriginalUri, OriginalUri);
		}

		/// <summary>
		/// Adds one single custom field for the <see cref="UpdateIssue_SetCustomFields"/> function.
		/// </summary>
		protected void UpdateIssue_SetCustomFields_Add(List<RemoteFieldValue> arValues, Dictionary<string, string> mapFieldNameToId, string sFieldName, string sFieldValue)
		{
			if(string.IsNullOrEmpty(sFieldName))
				return;
			if(string.IsNullOrEmpty(sFieldValue))
				return;

			// Lookup ID by the name
			string sFieldId;
			if(!mapFieldNameToId.TryGetValue(sFieldName, out sFieldId))
			{
				ErrorLog.AppendFormat("Could not find a custom field named “{0}” on the “{1}” JIRA server.", sFieldName, Project.Server.Name);
				ErrorLog.AppendLine();
				return;
			}

			// Add to the submit-list
			arValues.Add(CreateFieldValue(sFieldId, sFieldValue));
		}

		/// <summary>
		/// Adds the <see cref="Attachments"/> to the <see cref="Issue"/>.
		/// </summary>
		protected void AddAttachmentsToIssue()
		{
			if(Attachments.Count == 0)
				return;

			SetStatus("Preparing the list of attachments");
			foreach(KeyValuePair<string, byte[]> pair in Attachments)
			{
				try
				{
					// Due to an error in WSDL, convert the data to s-bytes from bytes
					SetStatus(string.Format("Adding attachments — {0} — {1}", pair.Key, "Copying"));
					sbyte[] datatemp = new sbyte[pair.Value.Length];
					for(int a = 0; a < pair.Value.Length; a++)
						datatemp[a] = unchecked((sbyte)pair.Value[a]);

					SetStatus(string.Format("Adding attachments — {0} — {1}", pair.Key, "Sending"));
					Project.Server.Service.addAttachmentsToIssue(Project.Server.GetSignInToken(), Issue.key, new string[] {pair.Key}, datatemp);
				}
				catch(Exception ex)
				{
					ErrorLog.AppendFormat("Could not add the “{0}” attachment to the issue. {1}", pair.Key, ex.Message);
					ErrorLog.AppendLine();
				}
			}
		}

		/// <summary>
		/// A helper for creating the remote field value.
		/// </summary>
		public static RemoteFieldValue CreateFieldValue(string id, params string[] values)
		{
			RemoteFieldValue retval = new RemoteFieldValue();
			retval.id = id;
			retval.values = values;
			return retval;
		}

		/// <summary>
		/// A helper for creating the remote field value.
		/// </summary>
		public static RemoteFieldValue CreateFieldValue(JiraIssueType.JiraIssueKeys id, params string[] values)
		{
			RemoteFieldValue retval = new RemoteFieldValue();
			retval.id = id.ToString();
			retval.values = values;
			return retval;
		}

		/// <summary>
		/// Fires <see cref="IssueSubmitted"/>.
		/// </summary>
		protected void FireIssueSubmitted()
		{
			try
			{
				if(IssueSubmitted != null)
					IssueSubmitted(this, EventArgs.Empty);
			}
			catch(Exception ex)
			{
				Core.ReportException(ex, ExceptionReportFlags.AttachLog);
			}
		}

		/// <summary>
		/// Fires <see cref="IssueSubmissionFailed"/>.
		/// </summary>
		protected void FireIssueSubmissionFailed()
		{
			try
			{
				if(IssueSubmissionFailed != null)
					IssueSubmissionFailed(this, EventArgs.Empty);
			}
			catch(Exception ex)
			{
				Core.ReportException(ex, ExceptionReportFlags.AttachLog);
			}
		}

		/// <summary>
		/// Fires after the issue was submitted successfully.
		/// </summary>
		public event EventHandler IssueSubmitted;

		/// <summary>
		/// Fires after the issue was attempted to be submitted, but such an attempt failed.
		/// </summary>
		public event EventHandler IssueSubmissionFailed;
	}
}
