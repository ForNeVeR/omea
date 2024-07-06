// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Windows.Forms;

using JetBrains.Omea.GUIControls.MshtmlBrowser;
using JetBrains.Omea.Jiffa.Res;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Jiffa
{
	public partial class IssueView : Form
	{
		protected Submission _submission;

		protected MshtmlEdit _browser;

		public IssueView(Submission submission)
		{
			if(submission == null)
				throw new ArgumentNullException("submission");
			_submission = submission;
			_submission.IssueSubmissionFailed += OnIssueSubmissionFailed;
			_submission.IssueSubmitted += OnIssueSubmitted;

			InitializeComponent();
			Disposed += OnDisposed;

			AddBrowser();

			LoadJiraData();
			LoadMru();
			LoadSubmission(); // Must be loaded after MRU
		}

		public Submission Submission
		{
			get
			{
				return _submission;
			}
		}

		/// <summary>
		/// Fills in the UI elements that list JIRA objects.
		/// </summary>
		protected void LoadJiraData()
		{
			foreach(JiraIssueType item in Submission.Project.Server.IssueTypes)
				_comboIssueType.Items.Add(item.Resource);
			foreach(JiraPriority item in Submission.Project.Server.Priorities)
				_comboPriority.Items.Add(item.Resource);
			_comboComponent.Items.Add(Stringtable.ComponentNone);
			foreach(JiraComponent item in Submission.Project.Components)
				_comboComponent.Items.Add(item.Resource);
			_comboStatus.Items.Add(Stringtable.StatusAuto);
			foreach(JiraStatus item in Submission.Project.Server.Statuses)
				_comboStatus.Items.Add(item.Resource);

			_comboDeveloper.Items.Add(Stringtable.ComponentNone);
			string sDevelopers = JiffaSettings.DevelopersList;
			sDevelopers = sDevelopers.Replace("\r", "");
			List<string> list = new List<string>(sDevelopers.Split('\n'));
			list.Sort();
			foreach(string sDeveloper in list)
			{
				string sTrimmed = sDeveloper.Trim();
				if(sTrimmed.Length == 0)
					continue;
				_comboDeveloper.Items.Add(sDeveloper);
			}
		}

		/// <summary>
		/// Loads the <see cref="Submission"/> to the UI.
		/// </summary>
		protected void LoadSubmission()
		{
			_txtTitle.Text = Submission.Title;
			_browser.Text = Submission.Body;

			if(!string.IsNullOrEmpty(Submission.BuildNumber))
				_txtBuildNumber.Text = Submission.BuildNumber;
		}

		protected void LoadMru()
		{
			if(!JiffaSettings.MruEnabled) // Don't load the MRU settings
				return;

			int value;

			value = Core.SettingStore.ReadInt("Jiffa.Submission", "MruIssueType", 0);
			value = value >= 0 ? (value < _comboIssueType.Items.Count ? value : 0) : 0;
			_comboIssueType.SelectedIndex = value;

			value = Core.SettingStore.ReadInt("Jiffa.Submission", "MruPriority", 0);
			value = value >= 0 ? (value < _comboPriority.Items.Count ? value : 0) : 0;
			_comboPriority.SelectedIndex = value;

			value = Core.SettingStore.ReadInt("Jiffa.Submission", "MruComponent", 0);
			value = value >= 0 ? (value < _comboComponent.Items.Count ? value : 0) : 0;
			_comboComponent.SelectedIndex = value;

			value = Core.SettingStore.ReadInt("Jiffa.Submission", "MruStatus", 0);
			value = value >= 0 ? (value < _comboStatus.Items.Count ? value : 0) : 0;
			_comboStatus.SelectedIndex = value;

			value = Core.SettingStore.ReadInt("Jiffa.Submission", "MruDeveloper", 0);
			value = value >= 0 ? (value < _comboDeveloper.Items.Count ? value : 0) : 0;
			_comboDeveloper.SelectedIndex = value;

			_txtBuildNumber.Text = Core.SettingStore.ReadString("Jiffa.Submission", "MruBuildNumber", "");
		}

		protected void SaveMru()
		{
			Core.SettingStore.WriteInt("Jiffa.Submission", "MruIssueType", _comboIssueType.SelectedIndex);
			Core.SettingStore.WriteInt("Jiffa.Submission", "MruPriority", _comboPriority.SelectedIndex);
			Core.SettingStore.WriteInt("Jiffa.Submission", "MruComponent", _comboComponent.SelectedIndex);
			Core.SettingStore.WriteInt("Jiffa.Submission", "MruStatus", _comboStatus.SelectedIndex);
			Core.SettingStore.WriteInt("Jiffa.Submission", "MruDeveloper", _comboDeveloper.SelectedIndex);

			Core.SettingStore.WriteString("Jiffa.Submission", "MruBuildNumber", _txtBuildNumber.Text);
		}

		protected void AddBrowser()
		{
			_browser = new MshtmlEdit();
			_browser.Dock = DockStyle.Fill;
			_browser.add_KeyDown(OnKeyDown);
			_panelBrowser.Controls.Add(_browser);
		}

		protected void OnSubmit(object sender, EventArgs e)
		{
			// Pass the values to the backend
			try
			{
				Submission.Body = _browser.Text;
				Submission.Title = _txtTitle.Text;

				Submission.Component = _comboComponent.SelectedItem is IResource ? new JiraComponent(Submission.Project, (IResource)_comboComponent.SelectedItem) : null;

				Submission.Status = _comboStatus.SelectedItem is IResource ? new JiraStatus(Submission.Project.Server, (IResource)_comboStatus.SelectedItem) : null;

				if(!(_comboIssueType.SelectedItem is IResource))
					throw new Exception("The issue type must be specified.");
				Submission.IssueType = new JiraIssueType(Submission.Project.Server, (IResource)_comboIssueType.SelectedItem);

				Submission.Priority = _comboPriority.SelectedItem is IResource ? new JiraPriority(Submission.Project.Server, (IResource)_comboPriority.SelectedItem) : null;

				Submission.Assignee = _comboDeveloper.SelectedItem is string ? (string)_comboDeveloper.SelectedItem : "";
			}
			catch(Exception ex)
			{
				MessageBox.Show(this, ex.Message, Jiffa.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			SaveMru();

			// Initiate async Submit
			try
			{
				Enabled = false; // Will be enabled upon one of the submission-result events
				Submission.Submit(this);
			}
			catch(Exception ex)
			{
				MessageBox.Show(this, "Could not submit the issue.\n\n" + ex.Message, Jiffa.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Enabled = true;
				return;
			}
		}

		protected void OnKeyDown(object sender, KeyEventArgs e)
		{
			switch(e.KeyData)
			{
			case Keys.Escape:
				e.Handled = true;
				Close();
				return;
			case Keys.Enter | Keys.Control:
				e.Handled = true;
				OnSubmit(this, EventArgs.Empty);
				break;
			}
		}

		protected void OnIssueSubmissionFailed(object sender, EventArgs e)
		{
			string sErrors = Submission.ErrorLog.ToString();
			MessageBox.Show(this, string.Format("Failed to submit the issue.\n\n{0}", sErrors), Jiffa.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);

			// Reenable and allow editing
			Enabled = true;
		}

		protected void OnIssueSubmitted(object sender, EventArgs e)
		{
			// Show the warnings, if there were any.
			if(Submission.ErrorLog.Length != 0)
			{
				string sErrors = Submission.ErrorLog.ToString();
				string sIssueKey = Submission.Issue.key;
				Core.UserInterfaceAP.QueueJob("JIRA Issue Submission Warnings.", (MethodInvoker)delegate { MessageBox.Show(Core.MainWindow, string.Format("The issue {0} has been created with warnings.\n\n{1}", sIssueKey, sErrors), Jiffa.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning); });
			}

			// Close the window
			Close();
		}

		protected void OnDisposed(object sender, EventArgs e)
		{
			_submission.IssueSubmitted -= OnIssueSubmitted;
			_submission.IssueSubmissionFailed -= OnIssueSubmissionFailed;
			_submission = null;
		}
	}
}
