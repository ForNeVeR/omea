// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.FiltersManagement;

namespace JetBrains.Omea.GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for EditRuleForm.
	/// </summary>
	public class EditExpirationRuleForm : ViewCommonDialogBase
	{
        private const int  ciFormHeight = 707;

        private System.ComponentModel.IContainer  components;

        protected GroupBox     boxActions;
        protected Label        _lblActions;
        protected Panel        panelActions;
        protected JetLinkLabel labelAddAction;

        private   IResourceList     _baseFolders;
        private readonly IResource  _baseResType;
        private readonly bool       _isForDeletedItems = false;
        private bool DeleteHangedContacts = false;

        #region Ctor
		public EditExpirationRuleForm( IResource resType, IResource[][] conditions,
                                       IResource[] exceptions, IResource[] actions,
                                       bool delContacts, bool forDeletedItems )
               : base( "IsExpirationRuleLinked", false, true, true )
		{
            Initialize( null );
            _baseResType = resType;

            ArrayList parameters = new ArrayList();
            ArrayList actionsL = CollectResourcesAndTemplates( actions, parameters );
            AddConditions( panelActions, actionsL, parameters );
            InitializeBasePanels( new string[] { resType.GetStringProp( Core.Props.Name ) }, conditions, exceptions );

            DeleteHangedContacts = delContacts;
		    _isForDeletedItems = forDeletedItems;
            ConstructLinkText( _baseResType );

            resourceTypesLink.Enabled = false;
		}

		public EditExpirationRuleForm( IResource resType, IResource defaultsRule, bool forDeletedItems )
               : base( "IsExpirationRuleLinked", false, true, true )
		{
            Initialize( null );
            _baseResType = resType;

            if( defaultsRule != null )
            {
                InitializeActions( defaultsRule );
                InitializeBasePanels( defaultsRule );
                DeleteHangedContacts = defaultsRule.HasProp( "DeleteRelatedContact" );
            }

		    _isForDeletedItems = forDeletedItems;
            ConstructLinkText( _baseResType );

            resourceTypesLink.Enabled = false;
		}

		public EditExpirationRuleForm( string ruleName ) : base( "IsExpirationRuleLinked", false, true, true )
		{
            #region Preconditions
            if( String.IsNullOrEmpty( ruleName ))
                throw new ArgumentNullException( "ruleName", "EditExpirationRuleForm -- Input rule name is NULL" );
            #endregion Preconditions

            BaseResource = RStore.FindUniqueResource( FilterManagerProps.RuleResName, "Name", ruleName );
            IResourceList linked = BaseResource.GetLinksOfType( null, "ExpirationRuleLink" );
            if( linked.Count == 0 )
            {
                linked = BaseResource.GetLinksOfType( null, "ExpirationRuleOnDeletedLink" );
                if( linked.Count == 1 && linked[ 0 ].Type == "ResourceType" )
                {
                    _isForDeletedItems = true;
                    _baseResType = linked[ 0 ];
                }
                else
                    throw new ApplicationException( "EditExpirationRule -- Contract violation - No linkage between rule and related resources" ) ;
            }
            else
            if( linked.Count == 1 && linked[ 0 ].Type == "ResourceType" )
                _baseResType = linked[ 0 ];

            Initialize( (_baseResType == null) ? linked : null );

            InitializeActions( BaseResource );
            InitializeBasePanels( BaseResource );
            if( _baseResType != null )
            {
		        ConstructLinkText( _baseResType );
                resourceTypesLink.Enabled = false;
            }
            else
		        ConstructLinkText( linked );
        }

		public EditExpirationRuleForm( IResourceList folders, IResource defaultsRule )
               : base( "IsExpirationRuleLinked", false, true, true )
		{
            Initialize( folders );
            if( defaultsRule != null )
            {
                InitializeActions( defaultsRule );
                InitializeBasePanels( defaultsRule );

                BaseResource = defaultsRule;
                DeleteHangedContacts = defaultsRule.HasProp( "DeleteRelatedContact" );
            }

		    ConstructLinkText( folders );
		}

		public EditExpirationRuleForm( IResourceList folders, IResource[][] conditions,
                                       IResource[] exceptions, IResource[] actions, bool delContacts )
               : base( "IsExpirationRuleLinked", false, true, true )
		{
            Initialize( folders );

            ArrayList parameters = new ArrayList();
            ArrayList actionsL = CollectResourcesAndTemplates( actions, parameters );
            AddConditions( panelActions, actionsL, parameters );
            InitializeBasePanels( null, conditions, exceptions );

		    ConstructLinkText( folders );
            DeleteHangedContacts = delContacts;
		}

        private void  Initialize( IResourceList folders )
        {
			InitializeComponent();

            _baseFolders = folders;
            ValidResourceTypes = Core.ResourceStore.GetAllResources( FilterRegistry.RuleApplicableResourceTypeResName );
            _externalChecker = CheckValidActions;
            _lblHeading.Visible = _editHeading.Visible = false;
            _referenceTopic = "reference\\auto_expiration_rule_dialog.htm";

            resourceTypesLink.Click -= resourceTypesLink_LinkClicked;
            resourceTypesLink.Click += resourceTypesLink_LinkClicked2;
        }

        private void  InitializeActions( IResource baseRes )
        {
            ArrayList parameters = new ArrayList();
            ArrayList actions = CollectResourcesAndTemplates( baseRes, parameters, Core.FilterRegistry.Props.LinkedActions );
            AddConditions( panelActions, actions, parameters );
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

            this.boxActions = new GroupBox();
            this._lblActions = new System.Windows.Forms.Label();
            this.panelActions = new System.Windows.Forms.Panel();
            this.labelAddAction = new JetLinkLabel();

            this.components = new System.ComponentModel.Container();
            this.resTypeToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // boxActions
            //
            this.boxActions.Location = new System.Drawing.Point(7, 492);
            this.boxActions.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            this.boxActions.Name = "boxActions";
            this.boxActions.Size = new System.Drawing.Size(384, 160);
            this.boxActions.FlatStyle = FlatStyle.System;
            this.boxActions.TabStop = false;
            //
            // _lblActions
            //
            this._lblActions.Location = new System.Drawing.Point(10,10);
            this._lblActions.Name = "_lblActions";
            this._lblActions.Size = new System.Drawing.Size(64, 16);
            this._lblActions.TabIndex = 7;
            this._lblActions.Text = "Actions";
            //
            // panelActions
            //
            this.panelActions.AutoScroll = true;
            this.panelActions.BackColor = System.Drawing.SystemColors.Window;
            this.panelActions.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelActions.Location = new System.Drawing.Point(8, 28);
            this.panelActions.Name = "panelActions";
            this.panelActions.Size = new System.Drawing.Size(370, 95);
            this.panelActions.TabIndex = 4;
            this.panelActions.Resize += new EventHandler( panel_Resize );
            this.panelActions.Anchor = (AnchorStyles)(AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
            //
            // labelAddAction
            //
            this.labelAddAction.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            this.labelAddAction.Name = "buttonAddAction";
            this.labelAddAction.Size = new System.Drawing.Size(65, 16);
            this.labelAddAction.TabStop = true;
            this.labelAddAction.TextAlign = ContentAlignment.MiddleLeft;
            this.labelAddAction.Text = "Add Action...";
            this.labelAddAction.Tag = panelActions;
            this.labelAddAction.Click += new System.EventHandler(this.AddActionClicked);
            int position = boxActions.Width - _cAddLabelXPosDiff - (int)(labelAddAction.Size.Width * Core.ScaleFactor.Width);
            this.labelAddAction.Location = new System.Drawing.Point(position, 139);
            //
            // EditRuleForm
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(398, ciFormHeight);
            this.MinimumSize = new Size( 315, 440 );
            this.Name = "EditExpirationRuleForm";
            this.Text = "Edit Expiration Rule";

            boxActions.Controls.Add(this._lblActions);
            boxActions.Controls.Add(this.panelActions);
            boxActions.Controls.Add(this.labelAddAction);
            this.Controls.Add(this.boxActions);

            base._lblHeading.Text = "Rule &name:";
            base.okButton.Click += new System.EventHandler(this.okButton_Click);

		    ShiftControlsV( -22, forResourcesLabel, resourceTypesLink, _boxConditions, _boxExceptions, boxActions );

            PlaceBottomControls( ciFormHeight );
            this.ResumeLayout(false);
        }
		#endregion

        #region OK
        private void okButton_Click(object sender, EventArgs e)
        {
            Debug.Assert( okButton.Enabled );

            cancelButton.Enabled = okButton.Enabled = false;

            IResource[] conds   = ConvertTemplates2Conditions( panelConditions.Controls );
            IResource[] excepts = ConvertTemplates2Conditions( panelExceptions.Controls );
            IResource[] actions = (IResource[]) CollectActions().ToArray( typeof( IResource ));

            //  If user does not specify any condition, put dummy condition
            //  so that general rule for Rule/View is conformed.
            if( conds.Length == 0 )
            {
                conds = new IResource[] { FilterManagerStandards.DummyCondition };
            }

            if( BaseResource == null )
            {
                if( _baseResType == null )
                    BaseResource = Core.ExpirationRuleManager.RegisterRule( _baseFolders, conds, excepts, actions );
                else
                if( !_isForDeletedItems )
                    BaseResource = Core.ExpirationRuleManager.RegisterRule( _baseResType, conds, excepts, actions );
                else
                    BaseResource = Core.ExpirationRuleManager.RegisterRuleForDeletedItems( _baseResType, conds, excepts, actions );
            }
            else
            {
                if( _baseResType == null )
                    Core.ExpirationRuleManager.ReregisterRule( BaseResource, _baseFolders, conds, excepts, actions );
                else
                if( !_isForDeletedItems )
                    Core.ExpirationRuleManager.ReregisterRule( BaseResource, _baseResType, conds, excepts, actions );
                else
                    Core.ExpirationRuleManager.ReregisterRuleForDeletedItems( BaseResource, _baseResType, conds, excepts, actions );
            }

            FreeConditionLists( panelConditions.Controls );
            FreeConditionLists( panelExceptions.Controls );
            FreeConditionLists( panelActions.Controls );
            DialogResult = DialogResult.OK;
        }

        private ArrayList CollectActions()
        {
            ArrayList   actions = new ArrayList();
            foreach( Control ctrl in panelActions.Controls )
            {
                if( ctrl is Label || ctrl is LinkLabel )
                {
                    LabelInfo info = (LabelInfo)ctrl.Tag;
                    IResource  action = info.AssociatedResource;
                    if( isTemplate( action ))
                        action = FilterConvertors.Template2Action( action, info.Parameters, info.Representation );
                    actions.Add( action );
                }
            }
            return( actions );
        }

        private bool CheckValidActions( out string errorMsg, out Control errCtrl )
        {
            errorMsg = "No rule action is present"; //  some default values...
            errCtrl = _lblActions;
            bool isOK = (panelActions.Controls.Count > 0);

            if( isOK )
            {
                foreach( Control ctrl in panelActions.Controls )
                {
                    LabelInfo info = (LabelInfo)ctrl.Tag;
                    if( isTemplate( info.AssociatedResource ) && info.Parameters == null )
                    {
                        isOK = false;
                        errorMsg = "Rule action [" + info.AssociatedResource.GetPropText( Core.Props.Name ) + "] is not instantiated";
                        break;
                    }
                }
            }
            return isOK;
        }
        #endregion OK

        #region Add/Delete Actions
        protected void AddActionClicked( object sender, EventArgs e )
        {
            ArrayList       usedResources = CollectResourcesInControls( panelActions.Controls );
            IResourceList   actions = RStore.GetAllResources( FilterManagerProps.RuleActionResName );
            actions = actions.Minus( RStore.FindResources( FilterManagerProps.RuleActionResName, "Invisible", true ) );
            actions = actions.Union( RStore.GetAllResources( FilterManagerProps.RuleActionTemplateResName ));
            foreach( IResource res in usedResources )
                actions = actions.Minus( res.ToResourceList() );

            //-----------------------------------------------------------------
            //  Remove those actions and templates which are applicable to the
            //  resource types not supported by the corresponding plugins (if
            //  e.g. they are not loaded). Decision is made using the fact that
            //  actions are ALWAYS implemented as classes (not internal logic),
            //  so if plugin is not loaded, then all of its actions are not
            //  instantiated.
            //-----------------------------------------------------------------
            IResourceList auxList = RStore.EmptyResourceList;
            foreach( IResource res in actions )
            {
                if( Core.FilterRegistry.IsActionInstantiated( res ) )
                    auxList = auxList.Union( res.ToResourceList() );
            }
            actions = auxList;

            //-----------------------------------------------------------------
            IResourceList choosenActions = RStore.EmptyResourceList;
            if( CurrentResTypeDeep != null )
            {
                foreach( IResource res in actions )
                {
                    if( isTypeConforms( CurrentResTypeDeep, res ))
                        choosenActions = choosenActions.Union( res.ToResourceList() );
                }
            }
            else
                choosenActions = actions;

            //-----------------------------------------------------------------
            choosenActions.Sort( new SortSettings( Core.Props.Name, true ) );
            IResourceList   selected = Core.UIManager.SelectResourcesFromList( this, choosenActions,
                "Select Rule Action(s)", "/reference/select_rule_actions.html" );
            if(( selected != null ) && ( selected.Count > 0 ))
            {
                ArrayList  emptyParams = CreateEmptyList( selected.Count, -1 );
                AddConditions( panelActions, selected, emptyParams );
            }

            CheckFormConsistency();
        }
        #endregion Add/Delete Actions

        protected void resourceTypesLink_LinkClicked2(object sender, EventArgs e)
        {
            string type = _baseFolders[ 0 ].Type;
            IResourceList result = Core.UIManager.SelectResources( this, type, "Folders for an Expiration Rule", _baseFolders );
            if( result != null && result.Count > 0 )
            {
                _baseFolders = result;
                ConstructLinkText( _baseFolders );
            }
        }

        private void  ConstructLinkText( IResource type )
        {
            resourceTypesLink.Tag = CurrentResTypeDeep = type.GetStringProp( Core.Props.Name );
            if( !_isForDeletedItems )
                CurrentResTypeDeep = ExpirationRuleManager.ItemFromContainerType( CurrentResTypeDeep );
            AssignResTypesText( type.DisplayName + "s" );
        }
        private void  ConstructLinkText( IResourceList folders )
        {
            string text = string.Empty;
            foreach( IResource folder in folders )
                text += folder.DisplayName + ", ";

            if( text.Length > 0 )
                text = text.Substring( 0, text.Length - 2 );

            resourceTypesLink.Tag = text;
            AssignResTypesText( text );

            if( folders.Count > 0 )
                CurrentResTypeDeep = ExpirationRuleManager.ItemFromContainerType( folders[ 0 ].Type );
        }
    }
}
