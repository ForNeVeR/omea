/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for ApplyRulesForm.
	/// </summary>
	public class ApplyRulesForm : DialogBase
	{
        private Label labelApplicableRules;
        private CheckedListBox listRules;
        private GroupBox groupBoxResources;
        private RadioButton radioSelectedResources;
        private RadioButton radioOwnerResource;
        private RadioButton radioTabType;
        private RadioButton radioAllResources;
        private Button buttonOK;
        private Button buttonCancel;
        private Button buttonHelp;

        private readonly IResourceList  SelectedResourcesInBrowser;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ApplyRulesForm( IResourceList selectedResources )
		{
			InitializeComponent();

            SelectedResourcesInBrowser = selectedResources;

            int optionOrder = RestorePreviousOptionSelection();

            //  Set appropriate names and tags for radio button texts.
            if( selectedResources == null || selectedResources.Count == 0 )
            {
                radioSelectedResources.Enabled = false;

                //  Move selection to the next control
                radioOwnerResource.Checked = true;
            }

            if( Core.ResourceBrowser.OwnerResource != null )
                radioOwnerResource.Text = "Resources in \"" + Core.ResourceBrowser.OwnerResource.DisplayName + "\"";
            else
            {
                radioOwnerResource.Enabled = false;
                radioOwnerResource.Text = "Resources in the parent folder";

                //  Move selection to the previous or next control depending
                //  on what is enabled.
                if( radioSelectedResources.Enabled )
                    radioSelectedResources.Checked = true;
                else
                    radioTabType.Checked = true;
            }

            string[]  tabTypes = Core.TabManager.CurrentTab.GetResourceTypes();
            if( tabTypes != null && tabTypes.Length > 0 )
            {
                IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", tabTypes[ 0 ] );
                radioTabType.Text = "All resources of type " + resType.DisplayName;
            }
            else
            {
                radioTabType.Text = "All resources of all types";
                radioTabType.Enabled = false;

                //  Move selection to the previous or next control depending
                //  on what is enabled.
                if( radioOwnerResource.Enabled )
                    radioSelectedResources.Checked = true;
                else
                    radioAllResources.Checked = true;
            }

            radioSelectedResources.Tag = 0;
            radioOwnerResource.Tag = 1;
            radioTabType.Tag = 2;
            radioAllResources.Tag = 3;

            ConstructApplicableRulesList( optionOrder );
            RestoreCheckedRules();
		}

        private int  RestorePreviousOptionSelection()
        {
            //  Read the previous state of the option. Set the same as previous.
            int optionOrder = Core.SettingStore.ReadInt( "Omea", "ApplyRulesOptionOrder", 0 );
            switch( optionOrder )
            {
                case 0: radioSelectedResources.Checked = true; break;
                case 1: radioOwnerResource.Checked = true; break;
                case 2: radioTabType.Checked = true; break;
                case 3: radioAllResources.Checked = true; break;
            }
            return optionOrder;
        }

        private void  RestoreCheckedRules()
        {
            //  Check rules which were selected on previous session,
            //  if they present in the current one.

            string ruleIDs = Core.SettingStore.ReadString( "Omea", "ApplyRulesSavedRules", string.Empty );
            if( ruleIDs.Length > 0 )
            {
                string[]   ids = ruleIDs.Split( ';' );
                for( int i = 0; i < listRules.Items.Count; i++ )
                {
                    string id = ((IResource)listRules.Items[ i ]).Id.ToString();
                    listRules.SetItemChecked( i, (Array.IndexOf( ids, id ) != -1) );
                }
            }
        }

        public IResourceList SelectedRules
        {
            get
            {
                IResourceList list = Core.ResourceStore.EmptyResourceList;
                for( int i = 0; i < listRules.Items.Count; i++ )
                {
                    if( listRules.GetItemChecked( i ) )
                    {
                        list = list.Union( ((IResource) listRules.Items[ i ]).ToResourceList(), true );
                    }
                }
                return list;
            }
        }

        public int Order
        {
            get
            {
                if( radioSelectedResources.Checked )
                    return 0;
                else
                if( radioOwnerResource.Checked )
                    return 1;
                else
                if( radioTabType.Checked )
                    return 2;
                else
                    return 3;
            }
        }

        //---------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.labelApplicableRules = new System.Windows.Forms.Label();
            this.listRules = new CheckedListBox();
            this.groupBoxResources = new System.Windows.Forms.GroupBox();
            this.radioSelectedResources = new System.Windows.Forms.RadioButton();
            this.radioOwnerResource = new System.Windows.Forms.RadioButton();
            this.radioTabType = new System.Windows.Forms.RadioButton();
            this.radioAllResources = new System.Windows.Forms.RadioButton();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonHelp = new System.Windows.Forms.Button();
            this.groupBoxResources.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelApplicableRules
            // 
            this.labelApplicableRules.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelApplicableRules.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.labelApplicableRules.Location = new System.Drawing.Point(8, 134);
            this.labelApplicableRules.Name = "labelApplicableRules";
            this.labelApplicableRules.Size = new System.Drawing.Size(140, 17);
            this.labelApplicableRules.TabIndex = 7;
            this.labelApplicableRules.Text = "Rules which can be applied:";
            // 
            // listRules
            // 
            this.listRules.AllowDrop = true;
            this.listRules.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.listRules.Location = new System.Drawing.Point(8, 159);
            this.listRules.Name = "listRules";
            this.listRules.CheckOnClick = true;
            this.listRules.ThreeDCheckBoxes = true;
            this.listRules.Size = new System.Drawing.Size(332, 177);
            this.listRules.TabIndex = 8;
            // 
            // groupBoxResources
            // 
            this.groupBoxResources.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxResources.Controls.Add(this.radioSelectedResources);
            this.groupBoxResources.Controls.Add(this.radioOwnerResource);
            this.groupBoxResources.Controls.Add(this.radioTabType);
            this.groupBoxResources.Controls.Add(this.radioAllResources);
            this.groupBoxResources.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBoxResources.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.groupBoxResources.Location = new System.Drawing.Point(8, 9);
            this.groupBoxResources.Name = "groupBoxResources";
            this.groupBoxResources.Size = new System.Drawing.Size(332, 112);
            this.groupBoxResources.TabIndex = 0;
            this.groupBoxResources.TabStop = false;
            this.groupBoxResources.Text = "Select range of resources";
            // 
            // radioSelectedResources
            // 
            this.radioSelectedResources.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.radioSelectedResources.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioSelectedResources.Location = new System.Drawing.Point(8, 22);
            this.radioSelectedResources.Name = "radioSelectedResources";
            this.radioSelectedResources.Size = new System.Drawing.Size(308, 17);
            this.radioSelectedResources.TabIndex = 1;
            this.radioSelectedResources.Text = "Selected resources";
            this.radioSelectedResources.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioOwnerResource
            // 
            this.radioOwnerResource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.radioOwnerResource.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioOwnerResource.Location = new System.Drawing.Point(8, 43);
            this.radioOwnerResource.Name = "radioOwnerResource";
            this.radioOwnerResource.Size = new System.Drawing.Size(308, 17);
            this.radioOwnerResource.TabIndex = 2;
            this.radioOwnerResource.Text = "Resources in the <OwnerResource>";
            this.radioOwnerResource.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioTabType
            // 
            this.radioTabType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.radioTabType.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioTabType.Location = new System.Drawing.Point(8, 65);
            this.radioTabType.Name = "radioTabType";
            this.radioTabType.Size = new System.Drawing.Size(308, 17);
            this.radioTabType.TabIndex = 3;
            this.radioTabType.Text = "All resources of type <TabType>";
            this.radioTabType.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioAllResources
            // 
            this.radioAllResources.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.radioAllResources.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioAllResources.Location = new System.Drawing.Point(8, 86);
            this.radioAllResources.Name = "radioAllResources";
            this.radioAllResources.Size = new System.Drawing.Size(308, 17);
            this.radioAllResources.TabIndex = 4;
            this.radioAllResources.Text = "All resources";
            this.radioAllResources.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.buttonOK.Location = new System.Drawing.Point(96, 349);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 25);
            this.buttonOK.TabIndex = 9;
            this.buttonOK.Text = "OK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCancel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.buttonCancel.Location = new System.Drawing.Point(180, 349);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 25);
            this.buttonCancel.TabIndex = 10;
            this.buttonCancel.Text = "Cancel";
            // 
            // buttonHelp
            // 
            this.buttonHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonHelp.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonHelp.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.buttonHelp.Location = new System.Drawing.Point(264, 349);
            this.buttonHelp.Name = "buttonHelp";
            this.buttonHelp.Size = new System.Drawing.Size(75, 25);
            this.buttonHelp.TabIndex = 10;
            this.buttonHelp.Text = "Help";
            this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
            // 
            // ApplyRulesForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(348, 380);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.groupBoxResources);
            this.Controls.Add(this.listRules);
            this.Controls.Add(this.labelApplicableRules);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonHelp);
            this.MinimumSize = new System.Drawing.Size(300, 330);
            this.Name = "ApplyRulesForm";
            this.Text = "Apply Rules";
            this.groupBoxResources.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        //  Construct list of rules which can be applied to the requested
        //  resource list.
        private void  ConstructApplicableRulesList( int optionOrder )
        {
            string[]      requestedTypes;
            IResourceList allRules = Core.ResourceStore.FindResourcesWithProp( FilterManagerProps.RuleResName, "IsActionFilter" );
            IResourceList activeRules = Core.ResourceStore.EmptyResourceList;

            //  Collect all rules which are checked
            IResourceList checkedRules = Core.ResourceStore.EmptyResourceList;
            for( int i = 0; i < listRules.Items.Count; i++ )
            {
                if( listRules.GetItemChecked( i ) )
                    checkedRules = checkedRules.Union( ((IResource)listRules.Items[ i ]).ToResourceList() );
            }

            //  First collect rules which are applicable to all resource types.
            foreach( IResource res in allRules )
            {
                if( !res.HasProp( "ContentType" ) )
                {
                    activeRules = activeRules.Union( res.ToResourceList(), true );
                }
            }
            IResourceList restRules = allRules.Minus( activeRules );

            //-----------------------------------------------------------------
            if( optionOrder == 0 ) //  Selected resources
            {
                requestedTypes = ResourceTypeHelper.GetUnderlyingResourceTypes( SelectedResourcesInBrowser );
                UpgradeList( restRules, ref activeRules, requestedTypes );
            }
            else
            if( optionOrder == 1 ) //  OwnerResource
            {
                IResourceList list = Core.ResourceBrowser.VisibleResources;
                requestedTypes = ResourceTypeHelper.GetUnderlyingResourceTypes( list );

                UpgradeList( restRules, ref activeRules, requestedTypes );
            }
            else
            if( optionOrder == 2 ) //  Tab
            {
                requestedTypes = Core.TabManager.CurrentTab.GetResourceTypes();
                UpgradeList( restRules, ref activeRules, requestedTypes );
            }
            else
                activeRules = allRules;

            activeRules.Sort( new SortSettings( Core.Props.Name, true ) );

            listRules.Items.Clear();
            foreach( IResource rule in activeRules )
            {
                listRules.Items.Add( rule );
            }

            //  Select those rule which were selected on previous option
            //  if they are present in the new list.
            IResourceList newChecked = checkedRules.Intersect( activeRules );
            for( int i = 0; i < listRules.Items.Count; i++ )
            {
                if( newChecked.IndexOf( (IResource)listRules.Items[ i ] ) != -1 )
                    listRules.SetItemChecked( i, true );
            }
        }

        private static void  UpgradeList( IResourceList restRules, ref IResourceList activeRules, string[] reqTypes )
        {
            foreach( IResource res in restRules )
            {
                string[] types = res.GetStringProp( "ContentType" ).Split( '|' );
                foreach( string resType in types )
                {
                    if(( reqTypes != null ) && ( Array.IndexOf( reqTypes, resType ) != -1 ))
                    {
                        activeRules = activeRules.Union( res.ToResourceList(), true );
                        break;
                    }
                }
            }
        }

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton control = (RadioButton) sender;
            if( control.Checked && control.Tag != null )
                ConstructApplicableRulesList( (int) ((RadioButton) sender).Tag );
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            //  Save the choosen option
            Core.SettingStore.WriteInt( "Omea", "ApplyRulesOptionOrder", Order );

            //  Save the choosen rules
            string  ruleIDs = string.Empty;
            for( int i = 0; i < listRules.Items.Count; i++ )
            {
                if( listRules.GetItemChecked( i ) )
                    ruleIDs += ((IResource)listRules.Items[ i ]).Id + ";";
            }
            if( ruleIDs.Length > 0 )
            {
                ruleIDs = ruleIDs.Substring( 0, ruleIDs.Length - 1 );
                Core.SettingStore.WriteString( "Omea", "ApplyRulesSavedRules", ruleIDs );
            }

            DialogResult = DialogResult.OK;
        }

        private void buttonHelp_Click(object sender, EventArgs e)
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "reference\\apply_rules_dialog.htm" );
        }
	}
}
