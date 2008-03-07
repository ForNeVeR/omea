/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.FiltersManagement;

namespace JetBrains.Omea.GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for ViewConstructorForm.
	/// </summary>
	public class AdvancedSearchForm : ViewCommonDialogBase
	{
        private const int   ciFormHeight = 605;

        public const string SearchViewPrefix = "Search results: ";
        private ArrayList   SavedQueries;

        private Label       labelInSection;
        private ComboBox    comboSearchQueryAndHistory;
        private ComboBox    comboSection;
        private CheckBox    checkKeepDialogOpen;
        private System.ComponentModel.IContainer components;

        #region Ctor
		public AdvancedSearchForm( IResource view )
               : base( "IsAdvSearchLinked", false, true, false )
		{
            Initialize( view.GetStringProp( "Name" ).Substring( 16 ) ); // minus "Search results: "
            RecreateResTypes( view );
		    BaseResource = view;

            ArrayList parameters = new ArrayList(), negParameters = new ArrayList();
            ArrayList conds = CollectResourcesAndTemplates( view, parameters, Core.FilterManager.Props.LinkedConditions );
            ArrayList excpts = CollectResourcesAndTemplates( view, negParameters, Core.FilterManager.Props.LinkedExceptions );
            InitializePanelsAndButtons( conds, parameters, excpts, negParameters );
        }

		public AdvancedSearchForm( string query, string[] resTypes, IResource[][] conditions, IResource[] exceptions )
               : base( "IsAdvSearchLinked", false, true, false )
		{
            Initialize( query );
            RecreateResTypes( resTypes );

            ArrayList parameters = new ArrayList(), negParameters = new ArrayList();
            ArrayList conds = CollectResourcesAndTemplates( conditions, parameters );
            ArrayList excpts = CollectResourcesAndTemplates( exceptions, negParameters );
            InitializePanelsAndButtons( conds, parameters, excpts, negParameters );
        }
		public AdvancedSearchForm( string query )
               : base( "IsAdvSearchLinked", false, true, false )
		{
            Initialize( query );
            RecreateResTypes( (string[])null );
            InitializePanelsAndButtons( new ArrayList(), new ArrayList(), new ArrayList(), new ArrayList() );
		}

        private void  Initialize( string initialQuery )
        {
			InitializeComponent();

            SavedQueries = LoadSavedQueries();
            FillSections();
            if( SavedQueries != null )
            {
                foreach( string str in SavedQueries )
                {
                    comboSearchQueryAndHistory.Items.Add( str );
                }
            }
            comboSearchQueryAndHistory.Text = _editHeading.Text = !String.IsNullOrEmpty( initialQuery ) ? initialQuery : string.Empty;
            FormTitleString = "query string";
            _referenceTopic = "search\\adv_search.html";
        }
        #endregion Ctor

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private new void InitializeComponent()
		{
            base.InitializeComponent();

            this.components = new System.ComponentModel.Container();
            this.resTypeToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.labelInSection = new System.Windows.Forms.Label();
            this.comboSection = new System.Windows.Forms.ComboBox();
            this.checkKeepDialogOpen = new CheckBox();
            this.comboSearchQueryAndHistory = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // labelInSection
            // 
            this.labelInSection.Location = new System.Drawing.Point(8, 32);
            this.labelInSection.Size = new System.Drawing.Size(60, 16);
            this.labelInSection.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.labelInSection.TabIndex = 4;
            this.labelInSection.Text = "In section:";
            // 
            // comboSection
            // 
            this.comboSection.Location = new System.Drawing.Point(72, 29);
            this.comboSection.Size = new System.Drawing.Size(130, 16);
            this.comboSection.Name = "comboSection";
            this.comboSection.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboSection.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.comboSection.SelectedValueChanged += new EventHandler( comboSection_SelectedValueChanged );
            this.comboSection.TabIndex = 5;
            // 
            // comboSearchQueryAndHistory
            // 
            this.comboSearchQueryAndHistory.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this.comboSearchQueryAndHistory.Location = new System.Drawing.Point(72, 4);
            this.comboSearchQueryAndHistory.Size = new System.Drawing.Size(300, 21);
            this.comboSearchQueryAndHistory.Name = "comboSection";
            this.comboSearchQueryAndHistory.DropDownStyle = ComboBoxStyle.DropDown;
            this.comboSearchQueryAndHistory.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.comboSearchQueryAndHistory.TabIndex = 1;
            this.comboSearchQueryAndHistory.SelectedIndexChanged += new System.EventHandler(this.SearchQueryComboBox_SelectedIndexChanged);
            this.comboSearchQueryAndHistory.TextChanged +=new EventHandler(comboSearchQueryAndHistory_TextChanged);
            this.comboSearchQueryAndHistory.Leave += new System.EventHandler(this.SearchQueryComboBox_Leave);
            //
            // checkKeepDialogOpen
            //
            this.checkKeepDialogOpen.Location = new Point( 8, 285 );
            this.checkKeepDialogOpen.Size = new Size( 180, 20 );
            this.checkKeepDialogOpen.Name = "checkKeepDialogOpen";
            this.checkKeepDialogOpen.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.checkKeepDialogOpen.TabIndex = 19;
            this.checkKeepDialogOpen.Text = "Keep dialog open after search";
            this.checkKeepDialogOpen.Checked = Core.SettingStore.ReadBool( "Search", "KeepDialogOpen", false );
            this.checkKeepDialogOpen.CheckedChanged += new EventHandler(checkKeepDialogOpen_CheckedChanged);
            this.checkKeepDialogOpen.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            this.checkKeepDialogOpen.FlatStyle = System.Windows.Forms.FlatStyle.System;
            // 
            // AdvancedSearchForm
            // 
            this.okButton.Text = "Search";
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(398, ciFormHeight);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "AdvancedSearchForm";
            this.Text = "Advanced Search";
            this.ShowInTaskbar = true;

            //-----------------------------------------------------------------
            this.Controls.Add(this.labelInSection);
            this.Controls.Add(this.comboSection);
            this.Controls.Add(this.comboSearchQueryAndHistory);
            this.Controls.Add(this.checkKeepDialogOpen);

            base._editHeading.Visible = false;

            base.okButton.Click += new System.EventHandler(this.okButton_Click);
            base.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);

            base._lblHeading.Text = "&Search for:";
            base.forResourcesLabel.Text = "Within:";

		    ShiftControlsV( 23, forResourcesLabel, resourceTypesLink, _boxConditions, _boxExceptions );

            checkKeepDialogOpen.Location = new Point(checkKeepDialogOpen.Left, ciFormHeight - 83);
            PlaceBottomControls( ciFormHeight );

            ResumeLayout(false);
        }
		#endregion

        #region OK/Cancel
        private void okButton_Click(object sender, EventArgs e)
        {
            if( !checkKeepDialogOpen.Checked )
            {
                Hide();
                PerformSearch();
                FreeConditionLists( panelConditions.Controls );
                FreeConditionLists( panelExceptions.Controls );
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                PerformSearch();
                DialogResult = DialogResult.None;
            }
        }
        private void PerformSearch()
        {
            string      query = _editHeading.Text.Trim();

            IResource[][] conditions = Controls2Resources( panelConditions.Controls );
            if( query.Length > 0 )
            {
                IResource   queryCondition = ((FilterManager)FMgr).CreateQueryConditionAux( null, query, comboSection.Text );
                FilterManager.ReferCondition2Template( queryCondition, FMgr.Std.BodyMatchesSearchQueryXName );

                //  Copy query condition to every subgroup or create the single one.
                if( conditions != null && conditions.Length > 0 )
                {
                    for( int i = 0; i < conditions.Length; i++ )
                    {
                        IResource[] group = conditions[ i ];
                        IResource[] newGroup = new IResource[ group.Length + 1 ];

                        for( int j = 0; j < group.Length; j++ )
                            newGroup[ j ] = group[ j ];
                        newGroup[ newGroup.Length - 1 ] = queryCondition;

                        conditions[ i ] = newGroup;
                    }
                }
                else
                {
                    conditions = FilterManager.Convert2Group( queryCondition );
                }
                UpdateStoredQueriesList( query );
            }

            IResource[] exceptions = ConvertTemplates2Conditions( panelExceptions.Controls );

            //-----------------------------------------------------------------
            //  need to remove existing basic View?
            //  NB: it removes all underlying AUX conditions including query search
            //-----------------------------------------------------------------
            IResource view = RStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", FMgr.ViewNameForSearchResults );
            string[] formTypes = ReformatTypes( CurrentResTypeDeep );
            if( view != null )
            {
                BaseResource = view;
                FMgr.ReregisterView( view, FMgr.ViewNameForSearchResults, formTypes, conditions, exceptions );
            }
            else
                BaseResource = FMgr.RegisterView( FMgr.ViewNameForSearchResults, formTypes, conditions, exceptions );

            //-----------------------------------------------------------------
            bool showContext = (query.Length > 0) && Core.SettingStore.ReadBool( "Resources", "ShowSearchContext", true );
            ResourceProxy proxy = new ResourceProxy( BaseResource );
            proxy.BeginUpdate();
            proxy.SetProp( Core.Props.Name, SearchViewPrefix + query );
            proxy.SetProp( Core.Props.ShowDeletedItems, true );
            proxy.SetProp( "ForceExec", true );
            proxy.SetProp( "ShowContexts", showContext );
            if( BaseResource.HasProp( Core.Props.ContentType ) || BaseResource.HasProp( "ContentLinks" ) )
                proxy.DeleteProp( "ShowInAllTabs" );
            else
                proxy.SetProp( "ShowInAllTabs", true );
            proxy.EndUpdate();

            //  if search is done specifically for the particular resource
            //  type - set the focus onto that tab.
            Core.ResourceTreeManager.LinkToResourceRoot( BaseResource, 1 );
            if(( CurrentResTypeDeep != null ) && 
               ( CurrentResTypeDeep.IndexOf( '|' ) == -1 ) && ( CurrentResTypeDeep.IndexOf( '#' ) == -1 ))
                Core.TabManager.SelectResourceTypeTab( CurrentResTypeDeep );
            else
                Core.TabManager.SelectResourceTypeTab( "" );

            Core.UIManager.BeginUpdateSidebar();
            Core.LeftSidebar.ActivateViewPane( StandardViewPanes.ViewsCategories );
            Core.UIManager.EndUpdateSidebar();
            Core.LeftSidebar.DefaultViewPane.SelectResource( BaseResource );
            BringToFront();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            FreeConditionLists( panelConditions.Controls );
            FreeConditionLists( panelExceptions.Controls );
            DialogResult = DialogResult.Cancel;
            Close();
        }
        #endregion OK/Cancel

        #region Supplementary
        protected override ArrayList CollectResourcesAndTemplates( IResourceList conditions, ArrayList paramList, int group )
        {
            ArrayList result = new ArrayList();

            foreach( IResource res in conditions )
            {
                IResource template = res.GetLinkProp( "TemplateLink" );
                if( template != null )
                {
                    string name = template.GetStringProp( Core.Props.Name );
                    if(!( name == Core.FilterManager.Std.BodyMatchesSearchQueryXName ))
                    {
                        result.Add( template );
                        paramList.Add( ConditionParams2ExplicitList( template, res ) );
                        ((LabelInfo)paramList[ paramList.Count - 1 ]).GroupIndex = group;
                    }
                    else
                    if( res.HasProp( "SectionOrder" ))
                    {
                        string sectionName = DocSectionHelper.FullNameByOrder( (uint)res.GetIntProp( "SectionOrder" ) );
                        Debug.Assert( sectionName != null, "Inverted reference to the section by its order failed" );
                        comboSection.SelectedItem = sectionName;
                    }
                }
                else
                {
                    result.Add( res );
                    paramList.Add( new LabelInfo() );
                    ((LabelInfo)paramList[ paramList.Count - 1 ]).GroupIndex = group;
                }
            }
            return( result );
        }

        private void FillSections()
        {
            IResourceList sections = Core.ResourceStore.GetAllResources( DocumentSectionResource.DocSectionResName );
            foreach( IResource res in sections )
                comboSection.Items.Add( res.GetStringProp( Core.Props.Name ));
            comboSection.SelectedItem = DocumentSection.BodySection;
        }

        private static ArrayList LoadSavedQueries()
        {
            ArrayList   savedQueries = new ArrayList();
            int         QueriesNumber = Core.SettingStore.ReadInt( "Search", "QueriesNumber", 0 );
            QueriesNumber = Math.Min( QueriesNumber, 10 );

            for( int i = 0; i < QueriesNumber; i++ )
            {
                string Query = Core.SettingStore.ReadString( "Search", "Query" + i );
                if( Query.Length > 0 )
                    savedQueries.Add( Query );
            }
            return savedQueries;
        }

        private void UpdateStoredQueriesList( string searchText )
        {
            int Index = SavedQueries.IndexOf( searchText );
            if( Index != -1 )
                SavedQueries.RemoveAt( Index );
            SavedQueries.Insert( 0, searchText );

            int     QueriesNumber = Math.Min( SavedQueries.Count, 10 );
            Core.SettingStore.WriteInt( "Search", "QueriesNumber", QueriesNumber );
            for( int i = 0; i < QueriesNumber; i++ )
                Core.SettingStore.WriteString( "Search", "Query" + i, (string)SavedQueries[ i ] );
        }
        #endregion Supplementary

        #region Events Processing
        private void comboSection_SelectedValueChanged(object sender, EventArgs e)
        {
            string      sectionName = comboSection.Text;
            IResource   section = RStore.FindUniqueResource( DocumentSectionResource.DocSectionResName, "Name", sectionName );
            if( section.HasProp( DocumentSectionResource.SectionHelpDescription ))
                resTypeToolTip.SetToolTip( comboSection, section.GetStringProp( DocumentSectionResource.SectionHelpDescription ));
            else
                resTypeToolTip.SetToolTip( comboSection, "" );
        }

        private void checkKeepDialogOpen_CheckedChanged(object sender, EventArgs e)
        {
            Core.SettingStore.WriteBool( "Search", "KeepDialogOpen", checkKeepDialogOpen.Checked );
        }

        private void SearchQueryComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _editHeading.Text = comboSearchQueryAndHistory.Text;
        }

        private void SearchQueryComboBox_Leave(object sender, EventArgs e)
        {
            _editHeading.Text = comboSearchQueryAndHistory.Text;
        }

        private void comboSearchQueryAndHistory_TextChanged(object sender, EventArgs e)
        {
            _editHeading.Text = comboSearchQueryAndHistory.Text;
        }
        #endregion Events Processing
    }
}
