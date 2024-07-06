// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for ChooseConditionForm.
	/// </summary>
	public class ChooseConditionForm : DialogBase
	{
        private IResourceStore Store = Core.ResourceStore;

        private System.Windows.Forms.Label  labelChooseConditions;
        private ResourceListView2           treeConditions;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonHelp;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ChooseConditionForm( ArrayList usedConditions, string resTypes,
                                    bool viewConditionsOnly, bool enableQueryCondition )
		{
			InitializeComponent();
            IResourceList  list = GatherSuitableConditons( usedConditions, resTypes,
                                                           viewConditionsOnly, enableQueryCondition );
            InitializeTree( list, resTypes );
            RestoreSettings();
            CheckButtonState();
		}
        public IResourceList SelectedConditions
        {
            get{  return treeConditions.GetSelectedResources(); }
        }

        private void InitializeTree( IResourceList list, string resTypes )
        {
            IJetListViewNodeFilter filter = new UnusedConditionsOnlyFilter( list, resTypes );
            treeConditions.Filters.Add( filter );
            treeConditions.HeaderStyle = ColumnHeaderStyle.None;

            IResource root = Core.ResourceStore.FindUniqueResource( "ConditionGroup", "Name", "AllConditionGroups" );
            Core.ResourceTreeManager.SetResourceNodeSort( root, "Name" );

            ResourceTreeDataProvider provider = new ResourceTreeDataProvider( root, Core.ResourceStore.PropTypes ["Parent"].Id );
            treeConditions.DataProvider = provider;
        }

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
            this.labelChooseConditions = new System.Windows.Forms.Label();
            this.treeConditions = new ResourceListView2();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonHelp = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // labelChooseConditions
            //
            this.labelChooseConditions.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelChooseConditions.Location = new System.Drawing.Point(4, 4);
            this.labelChooseConditions.Name = "labelChooseConditions";
            this.labelChooseConditions.Size = new System.Drawing.Size(112, 16);
            this.labelChooseConditions.TabIndex = 0;
            this.labelChooseConditions.Text = "Available Conditions:";
            //
            // treeConditions
            //
            this.treeConditions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.treeConditions.Location = new System.Drawing.Point(0, 24);
            this.treeConditions.Name = "treeConditions";
            this.treeConditions.Size = new System.Drawing.Size(282, 360);
            this.treeConditions.TabIndex = 1;
            this.treeConditions.DoubleClick += new HandledEventHandler(this.treeConditions_DoubleClick);
            this.treeConditions.JetListView.SelectionStateChanged += new StateChangeEventHandler(Selection_SelectionStateChanged);
            this.treeConditions.OpenProperty = Core.Props.Open;
            this.treeConditions.AllowSameViewDrag = false;

            this.treeConditions.AddTreeStructureColumn();
            this.treeConditions.AddIconColumn();
		    ResourceListView2Column column = this.treeConditions.AddColumn( ResourceProps.DisplayName );
            column.SizeToContent = true;
            column.Text = "Conditions";
		    //
            // buttonOK
            //
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Location = new System.Drawing.Point(32, 392);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "OK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCancel.Location = new System.Drawing.Point(116, 392);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            //
            // buttonHelp
            //
            this.buttonHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonHelp.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonHelp.Location = new System.Drawing.Point(200, 392);
            this.buttonHelp.Name = "buttonHelp";
            this.buttonHelp.TabIndex = 4;
            this.buttonHelp.Text = "Help";
            this.buttonHelp.Click += new System.EventHandler(this.HelpButton_Click);
            //
            // ChooseConditionForm
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(280, 421);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.treeConditions);
            this.Controls.Add(this.labelChooseConditions);
            this.Controls.Add(this.buttonHelp);
            this.Name = "ChooseConditionForm";
            this.Text = "Select Condition(s)";
            this.ResumeLayout(false);
        }
        #endregion

        private IResourceList GatherSuitableConditons( ArrayList usedConditions, string resTypes,
                                                       bool viewConditionsOnly, bool enableQueryCondition )
        {
            //-----------------------------------------------------------------
            //  Collect conditions which can be presented to the user
            //  and from which the user can choose the necessary ones.
            //  This list excludes:
            //  - those which are already used;
            //  - thouse which are internal conditions (temporary);
            //  - those which are persistent but can not be removed, e.g.
            //    from standard views;
            //  - those which are applicable to resource types implemented by
            //    unloaded plugins.
            //  If we prepare a set of conditions for views manager (not rules manager),
            //  then exclude those conditions, which can appear only in rules.
            //-----------------------------------------------------------------

            IResourceList   conditions = Store.GetAllResources( FilterManagerProps.ConditionResName );
            conditions = conditions.Union( Store.GetAllResources( FilterManagerProps.ConditionTemplateResName ));
            conditions = conditions.Minus( Store.FindResources( FilterManagerProps.ConditionResName, "InternalView", 1 ) );
            conditions = conditions.Minus( Store.FindResources( null, "Invisible", true ) );
            foreach( IResource res in usedConditions )
                conditions = conditions.Minus( res.ToResourceList() );
            if( viewConditionsOnly )
                conditions = conditions.Minus( Store.FindResourcesWithProp( null, "IsOnlyForRule" ) );

            //-----------------------------------------------------------------
            IResourceList auxList = Store.EmptyResourceList;
            foreach( IResource res in conditions )
            {
                string appResType = res.GetStringProp( Core.Props.ContentType );
                if( ResourceTypeHelper.IsResourceTypeActive( appResType ))
                    auxList = auxList.Union( res.ToResourceList() );
            }
            conditions = auxList;

            //-----------------------------------------------------------------
            if( !enableQueryCondition )
            {
                IResource queryTemplate = Store.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name",
                                                                    Core.FilterRegistry.Std.BodyMatchesSearchQueryXName );
                if( queryTemplate != null ) // it may be absent if no text index is loaded
                    conditions = conditions.Minus( queryTemplate.ToResourceList() );
                queryTemplate = Store.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name",
                                                          Core.FilterRegistry.Std.SubjectMatchSearchQueryXName );
                if( queryTemplate != null ) // it may be absent if no text index is loaded
                    conditions = conditions.Minus( queryTemplate.ToResourceList() );
                queryTemplate = Store.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name",
                                                          Core.FilterRegistry.Std.SourceMatchSearchQueryXName );
                if( queryTemplate != null ) // it may be absent if no text index is loaded
                    conditions = conditions.Minus( queryTemplate.ToResourceList() );
            }

            //-----------------------------------------------------------------
            //  Among chosen conditions, select those which conform to currently
            //  selected set of resource types.
            //-----------------------------------------------------------------
            IResourceList choosenConditions = Store.EmptyResourceList;
            if( resTypes != null )
            {
                foreach( IResource res in conditions )
                {
                    if( isTypeConforms( resTypes, res ))
                        choosenConditions = choosenConditions.Union( res.ToResourceList() );
                }
            }
            else
                choosenConditions = conditions;

            //-----------------------------------------------------------------
            //  For every condition, check whether they belong to some condition
            //  group. "Not belonging" inconsistency may occure when user runs
            //  newer version of OM over the older database and proper initialization
            //  of conditions was not run. In such case, ascribe such condition
            //  to the default group "Other"
            //-----------------------------------------------------------------
            for( int i = 0; i < choosenConditions.Count; i++ )
            {
                if( !choosenConditions[ i ].HasProp( Core.Props.Parent ) )
                    Core.FilterRegistry.AssociateConditionWithGroup( choosenConditions[ i ], "Other" );
            }
            return choosenConditions;
        }

        protected static bool isTypeConforms( string resTypes, IResource cond )
        {
            if( cond.HasProp( Core.Props.ContentType ))
            {
                string[] types = cond.GetStringProp( Core.Props.ContentType ).Split( '|' );
                foreach( string type in types )
                {
                    if( resTypes.IndexOf( type ) == -1 )
                        return false;
                }
            }
            return true;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void treeConditions_DoubleClick( object sender, HandledEventArgs e )
        {
            IResourceList selected = treeConditions.GetSelectedResources();
            if( selected.Count == 1 )
            {
                IResource res = selected[ 0 ];
                if( res != null && res.Type != FilterManagerProps.ConditionGroupResName )
                {
                    e.Handled = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
        }

        private void  CheckButtonState()
        {
            bool  can = true;
            IResourceList all = treeConditions.GetSelectedResources();
            foreach( IResource res in all )
                can = can && (res.Type != FilterManagerProps.ConditionGroupResName);

            buttonOK.Enabled = can && (all.Count > 0);
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "/reference/select_conditions.html" );
        }

        private void Selection_SelectionStateChanged(object sender, StateChangeEventArgs e)
        {
            CheckButtonState();
        }
    }

    #region Filters
    internal class UnusedConditionsOnlyFilter : IJetListViewNodeFilter
    {
        private IResourceList       UnusedConditions;
        private string[]            UsedResTypes;
        public event EventHandler   FilterChanged;

        internal UnusedConditionsOnlyFilter( IResourceList list, string resTypes )
        {
            UnusedConditions = list;
            UsedResTypes = (resTypes == null) ? new string[ 0 ] : resTypes.Split( '|' );
        }
        public bool AcceptNode( JetListViewNode node )
        {
            IResource res = (IResource)node.Data;
            if( res.Type == FilterManagerProps.ConditionResName ||
                res.Type == FilterManagerProps.ConditionTemplateResName )
            {
                return (UnusedConditions.IndexOf( res.Id ) >= 0);
            }
            else
            if( res.Type == FilterManagerProps.ConditionGroupResName )
            {
                IResourceList condInGroup = res.GetLinksOfType( FilterManagerProps.ConditionResName, Core.Props.Parent ).Union
                                           (res.GetLinksOfType( FilterManagerProps.ConditionTemplateResName, Core.Props.Parent ));
                foreach( IResource cond in condInGroup )
                {
                    string appResType = cond.GetStringProp( Core.Props.ContentType );
                    if( ResourceTypeHelper.IsResourceTypeActive( appResType ) &&
                        ResTypesIntersect( appResType, UsedResTypes ))
                        return true;
                }
            }
            return false;
        }
        private static bool ResTypesIntersect( string appResType, string[] usedResTypes )
        {
            string[] types = (appResType == null) ? new string[ 0 ] : appResType.Split( '|', '#' );
            if( types.Length == 0 || usedResTypes.Length == 0 )
                return true;

            foreach( string type in types )
            {
                if( Array.IndexOf( usedResTypes, type ) != -1 )
                    return true;
            }
            return false;
        }
    }
    #endregion Filters
}
