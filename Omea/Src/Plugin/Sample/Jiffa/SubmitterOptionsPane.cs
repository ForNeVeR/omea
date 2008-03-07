/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Windows.Forms;

using JetBrains.Omea.GUIControls.MshtmlBrowser;
using JetBrains.Omea.Jiffa.Res;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Jiffa
{
	public partial class SubmitterOptionsPane : AbstractOptionsPane
	{
		protected MshtmlEdit _browserTemplate;

		protected MshtmlEdit _browserDevelopers;

		public SubmitterOptionsPane()
		{
			InitializeComponent();
			Dock = DockStyle.Fill;

			SpawnControls();
			LoadMru();
		}

		public static void Register(IPlugin plugin)
		{
			Core.UIManager.RegisterOptionsPane("Internet", Jiffa.Name, CreateInstance, Stringtable.SubmitterOptionsPanePrompt);
		}

		public static AbstractOptionsPane CreateInstance()
		{
			return new SubmitterOptionsPane();
		}

		///<summary>
		///
		///            Called always when the pane is left in the dialog.
		///            
		///</summary>
		///
		public override void LeavePane()
		{
			if(!ValidateInput())
				return;
			base.LeavePane();
		}

		///<summary>
		///
		///<seealso cref="T:JetBrains.Omea.OpenAPI.ISettingStore" />
		///            Called when the Options dialog or the Startup Wizard is closed with the OK button.
		///            
		///</summary>
		///
		///<remarks>
		///Typically, this method would save the settings data.
		///</remarks>
		///
		public override void OK()
		{
			if(!ValidateInput())
				return;
			SaveMru();
			base.OK();
		}

		private void SaveMru()
		{
			// Project
			IResource resProject = _comboProject.SelectedItem as IResource;
			if((resProject != null) && (resProject.Type != JiraProject.Type))
				resProject = null;
			JiffaSettings.SubmitToProject = resProject != null ? JiraProject.FromResource(resProject) : null;

			// Developers
			JiffaSettings.DevelopersList = _browserDevelopers.Text;

			// Template
			JiffaSettings.Template = _browserTemplate.Text;

			// Build CF
			JiffaSettings.CustomFieldNames_BuildNumber = _txtBuildCF.Text;

			// Original URI CF
			JiffaSettings.CustomFieldNames_OriginalUri = _txtOriginalUriCF.Text;

			// MRU Enableed
			JiffaSettings.MruEnabled = _checkEnableMru.Checked;

			// Build Number Mask
			JiffaSettings.BuildNumberMask = _txtBuildNumberMask.Text;
		}

		protected void SpawnControls()
		{
			// Combo
			IResourceList resIntersect = Core.ResourceStore.GetAllResources(JiraServer.Type);
			int nServers = resIntersect.Count;
			resIntersect = resIntersect.Union(Core.ResourceStore.GetAllResources(JiraProject.Type));
			resIntersect = resIntersect.Union(JiraServer.RootResource.ToResourceList());
			_comboProject.Items.Add(nServers != 0 ? Stringtable.DontSubmit : Stringtable.GotoTabAddServer);
			if(nServers != 0)
				_comboProject.AddResourceHierarchy(JiraServer.RootResource, null, Core.Props.Parent, resIntersect);

			// Browsers
			_browserTemplate = new MshtmlEdit();
			_browserTemplate.Dock = DockStyle.Fill;
			_panelTemplate.Controls.Add(_browserTemplate);

			_browserDevelopers = new MshtmlEdit();
			_browserDevelopers.Dock = DockStyle.Fill;
			_panelDevelopers.Controls.Add(_browserDevelopers);
		}

		protected void LoadMru()
		{
			// Project
			JiraProject project = JiffaSettings.SubmitToProject;
			if(project != null)
				_comboProject.SelectedItem = project.Resource;
			else
				_comboProject.SelectedIndex = 0;

			// Developers
			_browserDevelopers.Text = JiffaSettings.DevelopersList;

			// Template
			_browserTemplate.Text = JiffaSettings.Template;

			// Build CF
			_txtBuildCF.Text = JiffaSettings.CustomFieldNames_BuildNumber;

			// Original URI CF
			_txtOriginalUriCF.Text = JiffaSettings.CustomFieldNames_OriginalUri;

			// MRU Enableed
			_checkEnableMru.Checked = JiffaSettings.MruEnabled;

			// Build Number Mask
			_txtBuildNumberMask.Text = JiffaSettings.BuildNumberMask;
		}

		protected bool ValidateInput()
		{
			IResource resProject = _comboProject.SelectedItem as IResource;
			if((!(_comboProject.SelectedItem is string)) && ((resProject == null) || (resProject.Type != JiraProject.Type)))
			{
				if(Core.ResourceStore.GetAllResources(JiraProject.Type).Count == 0)
					MessageBox.Show(this, Stringtable.NoJiraProjects, Jiffa.Name, MessageBoxButtons.OK, MessageBoxIcon.Stop);
				else
					MessageBox.Show(this, Stringtable.SelectJiraProject, Jiffa.Name, MessageBoxButtons.OK, MessageBoxIcon.Stop);
				return false;
			}

			return true;
		}
	}
}