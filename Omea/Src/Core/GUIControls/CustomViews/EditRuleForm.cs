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
	public class EditRuleForm : ViewCommonDialogBase
	{
        private const int  _ciFormHeight = 740;

        protected Label         _labelActivationTime;
        protected ComboBox      _comboActivationTime;

        protected GroupBox      _boxActions;
        protected Label         _lblActions;
        protected Panel         _panelActions;
        protected JetLinkLabel  _labelAddAction;

        private readonly Hashtable EventNamesMap = new Hashtable();
        private   Hashtable        EventDeepNamesMap = new Hashtable();

        private System.ComponentModel.IContainer components;

        #region Ctor
		public EditRuleForm( string ruleName ) : base( "IsActionRuleLinked", true, true, true )
		{
            #region Preconditions
            if( String.IsNullOrEmpty( ruleName ))
                throw new ArgumentNullException( "ruleName", "EditRuleForm -- Input rule name is NULL" );
            #endregion Preconditions

            Initialize( "Edit Action Rule", ruleName );
            BaseResource = RStore.FindUniqueResource( FilterManagerProps.RuleResName, "Name", ruleName );
            ArrayList parameters = new ArrayList();
            ArrayList actions = CollectResourcesAndTemplates( BaseResource, parameters, Core.FilterRegistry.Props.LinkedActions );
            AddConditions( _panelActions, actions, parameters );
            InitializeBasePanels( BaseResource );
            InitializeEventsList( BaseResource );
        }

		public EditRuleForm( string name, string[] resTypes,
                             IResource[][] conditions, IResource[] exceptions, IResource[] initActions )
               : base( "IsActionRuleLinked", true, true, true )
		{
            Initialize( "Edit Action Rule", name );

            ArrayList parameters = new ArrayList();
            ArrayList actions = CollectResourcesAndTemplates( initActions, parameters );
            AddConditions( _panelActions, actions, parameters );

            InitializeBasePanels( resTypes, conditions, exceptions );
		}

		public EditRuleForm() : base( "IsActionRuleLinked", true, true, true )
		{
            Initialize( "New Action Rule", null );
            InitializeBasePanels( null, new IResource[][]{}, new IResource[]{} );
		}

        private void  Initialize( string formTitle, string ruleName )
        {
			InitializeComponent();
            ValidResourceTypes = Core.ResourceStore.GetAllResources( FilterRegistry.RuleApplicableResourceTypeResName );
            _externalChecker = CheckValidActions;
            _editHeading.Text = InitialName = !String.IsNullOrEmpty( ruleName ) ? ruleName : string.Empty;
            Text = formTitle;
            _referenceTopic = "reference\\new_edit_rule.html";

            EventDeepNamesMap = Core.FilterEngine.GetRegisteredEvents();
            foreach( string eventDeepName in EventDeepNamesMap.Keys )
            {
                string eventDisplayName = (string) EventDeepNamesMap[ eventDeepName ];
                _comboActivationTime.Items.Add( eventDisplayName );
                EventNamesMap[ eventDisplayName ] = eventDeepName;
            }

            string lastEvent = Core.SettingStore.ReadString( "Rules", "LastEventName", string.Empty );
            if( lastEvent.Length == 0 )
                lastEvent = (string) EventDeepNamesMap[ StandardEvents.ResourceReceived ];
            int itemIndex = _comboActivationTime.Items.IndexOf( lastEvent );
            if( itemIndex != -1 )
                _comboActivationTime.SelectedIndex = itemIndex;
        }

        private void  InitializeEventsList( IResource rule )
        {
            string eventDeepName = rule.GetStringProp( "EventName" );
            string displayName = (string) EventDeepNamesMap[ eventDeepName ];
            _comboActivationTime.SelectedItem = displayName;
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

            this._labelActivationTime = new Label();
            this._comboActivationTime = new ComboBox();
            this._boxActions = new GroupBox();
            this._lblActions = new System.Windows.Forms.Label();
            this._panelActions = new System.Windows.Forms.Panel();
            this._labelAddAction = new JetLinkLabel();
//            this.buttonAddAction = new ImageListButton();

            this.components = new System.ComponentModel.Container();
            this.resTypeToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // _labelActivationTime
            //
            this._labelActivationTime.Location = new System.Drawing.Point(7, 33);
            this._labelActivationTime.Name = "_labelActivationTime";
            this._labelActivationTime.Size = new System.Drawing.Size(62, 16);
            this._labelActivationTime.TabIndex = 2;
            this._labelActivationTime.Text = "Activate at:";
            //
            // _comboActivationTime
            //
            this._comboActivationTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._comboActivationTime.Location = new System.Drawing.Point(72, 30);
            this._comboActivationTime.Name = "_labelActivationTime";
            this._comboActivationTime.Size = new System.Drawing.Size(150, 16);
            this._comboActivationTime.TabIndex = 3;
            this._comboActivationTime.SelectedIndexChanged += new EventHandler(comboActivationTime_SelectedIndexChanged);
            //
            // _boxActions
            //
            this._boxActions.Location = new System.Drawing.Point(7, 513);
            this._boxActions.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            this._boxActions.Name = "_boxActions";
            this._boxActions.Size = new System.Drawing.Size(384, 160);
            this._boxActions.FlatStyle = FlatStyle.System;
            this._boxActions.TabStop = false;
            //
            // _lblActions
            //
            this._lblActions.Location = new System.Drawing.Point(10,10);
            this._lblActions.Name = "_lblActions";
            this._lblActions.Size = new System.Drawing.Size(64, 16);
            this._lblActions.TabIndex = 7;
            this._lblActions.Text = "Actions";
            //
            // _panelActions
            //
            this._panelActions.AutoScroll = true;
            this._panelActions.BackColor = System.Drawing.SystemColors.Window;
            this._panelActions.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._panelActions.Location = new System.Drawing.Point(8, 28);
            this._panelActions.Name = "_panelActions";
            this._panelActions.Size = new System.Drawing.Size(370, 104);
            this._panelActions.TabIndex = 4;
            this._panelActions.Resize += new EventHandler( panel_Resize );
            this._panelActions.Anchor = (AnchorStyles)(AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
            //
            // _labelAddAction
            //
            this._labelAddAction.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            this._labelAddAction.Name = "buttonAddAction";
            this._labelAddAction.Size = new System.Drawing.Size(65, 16);
            this._labelAddAction.TabStop = true;
            this._labelAddAction.TextAlign = ContentAlignment.MiddleLeft;
            this._labelAddAction.Text = "Add Action...";
            this._labelAddAction.Click += new System.EventHandler(this.AddActionClicked);
            int position = _boxActions.Width - _cAddLabelXPosDiff - (int)(_labelAddAction.Size.Width * Core.ScaleFactor.Width);
            this._labelAddAction.Location = new System.Drawing.Point(position, 139);
            //
            // EditRuleForm
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(398, _ciFormHeight);
            this.MinimumSize = new Size( 315, 440 );
            this.Name = "EditRuleForm";
            this.Text = "New Action Rule";

            this.Controls.Add(this._labelActivationTime);
            this.Controls.Add(this._comboActivationTime);
            _boxActions.Controls.Add(this._lblActions);
            _boxActions.Controls.Add(this._panelActions);
            _boxActions.Controls.Add(this._labelAddAction);
            this.Controls.Add(this._boxActions);

            base._lblHeading.Text = "Rule &name:";
            base.okButton.Click += new System.EventHandler(this.okButton_Click);

		    ShiftControlsV( 25, forResourcesLabel, resourceTypesLink, _boxConditions, _boxExceptions );

            PlaceBottomControls( _ciFormHeight );
            this.ResumeLayout(false);
        }
		#endregion

        #region OK
        private void okButton_Click( object sender, EventArgs e )
        {
            Debug.Assert( okButton.Enabled );

            cancelButton.Enabled = okButton.Enabled = false;
            if( isResourceNewAndNameExist( FilterManagerProps.RuleResName ) )
            {
                DialogResult result = MessageBox.Show( this, "Rule with such name already exists. Do you want to overwrite it?",
                                                       "Names collision", MessageBoxButtons.YesNo );
                if( result == DialogResult.No )
                    return;

                FMgr.DeleteRule( _editHeading.Text );
            }

            //-----------------------------------------------------------------
            IResource[][] conds = Controls2Resources( panelConditions.Controls );
            IResource[] excConds = ConvertTemplates2Conditions( panelExceptions.Controls );
            IResource[] actions  = (IResource[]) CollectActions().ToArray( typeof( IResource ));
            string[] formTypes = ReformatTypes( CurrentResTypeDeep );

            string   eventName = (string) _comboActivationTime.SelectedItem;
            eventName = (string) EventNamesMap[ eventName ];

            //-----------------------------------------------------------------
            //  If it is a new rule - do nothing special, otherwise register
            //  the rule over the top of the existing one.
            //-----------------------------------------------------------------
            if( BaseResource == null  ) //  new resource
                BaseResource = FMgr.RegisterRule( eventName, _editHeading.Text, formTypes, conds, excConds, actions );
            else
                FMgr.ReregisterRule( eventName, BaseResource, _editHeading.Text, formTypes, conds, excConds, actions );

            FreeConditionLists( panelConditions.Controls );
            FreeConditionLists( panelExceptions.Controls );
            FreeConditionLists( _panelActions.Controls );
            DialogResult = DialogResult.OK;
        }

        private ArrayList CollectActions()
        {
            ArrayList   actions = new ArrayList();
            foreach( Control ctrl in _panelActions.Controls )
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
            bool isOK = (_panelActions.Controls.Count > 0);

            if( isOK )
            {
                foreach( Control ctrl in _panelActions.Controls )
                {
                    LabelInfo info = (LabelInfo)ctrl.Tag;
                    if( isTemplate( info.AssociatedResource ) && info.Parameters == null )
                    {
                        isOK = false;
                        errorMsg = "Rule action [" + info.AssociatedResource.GetPropText( Core.Props.Name ).Replace( "%", "" ) + "] is not instantiated";
                        break;
                    }
                }
            }
            return isOK;
        }
        #endregion OK

        #region Add/Delete Actions
        protected void AddActionClicked(object sender, EventArgs e)
        {
            ArrayList       usedResources = CollectResourcesInControls( _panelActions.Controls );
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
                AddConditions( _panelActions, selected, emptyParams );
            }

            actions.Dispose();
            CheckFormConsistency();
        }
        protected void DeleteActionClicked( object sender, EventArgs e )
        {
            CheckFormConsistency();
        }
        protected static void OnClickedInsideActionControl(object sender, EventArgs e)
        {
            ((Control)sender).Focus();
        }
        #endregion Add/Delete Actions

        private void comboActivationTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            string lastEvent = (string) _comboActivationTime.SelectedItem;
            if( !String.IsNullOrEmpty( lastEvent ) )
                Core.SettingStore.WriteString( "Rules", "LastEventName", lastEvent );
        }
    }
}
