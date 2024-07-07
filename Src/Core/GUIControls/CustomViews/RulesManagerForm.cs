// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using GUIControls.CustomViews;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for RulesManagerForm.
	/// </summary>
	public class RulesManagerForm : DialogBase
	{
        #region Attributes
        private Button okButton;
        private Button newButton;
        private Label  topLabel;
        private Button removeButton;
        private Button editButton;
        private Button cancelButton;
        private Button helpButton;
        private Button buttonMoveDown;
        private Button buttonMoveUp;
        private Button buttonCopyRule;
        private TabControl tabRulesTypes;
        private TabPage tabPageActions;
        private TabPage tabPageFormatting;
        private TabPage tabPageTrayIcon;
        private TabPage tabPageExpiration;

        private CheckedPlainListBox _listRules;
        private CheckedPlainListBox _listActionRules;
        private CheckedPlainListBox _listTrayIconRules;
        private CheckedPlainListBox _listFormattingRules;
        private CheckedPlainListBox _listExpirationRules;

        private readonly Hashtable Contexts = new Hashtable();
	    private ArrayList AddedRules = new ArrayList(), RemovedRules = new ArrayList();
        private readonly ArrayList AddedRulesAction = new ArrayList(), AddedRulesFormatting = new ArrayList(),
                                   AddedRulesTrayIcon = new ArrayList(), AddedRulesExpiration = new ArrayList();
        private readonly ArrayList RemovedRulesAction = new ArrayList(), RemovedRulesFormatting = new ArrayList(),
                                   RemovedRulesTrayIcon = new ArrayList(), RemovedRulesExpiration = new ArrayList();

        private RuleDecorator _decorator;
	    private IResourceList _rulesWithErrors;
        //  aliases
        private static readonly IResourceStore Store = Core.ResourceStore;
        private static readonly IFilterRegistry FMan = Core.FilterRegistry;
        #endregion Attributes

        /// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        #region Ctor and Initialization
		public  RulesManagerForm( string rulesType )
		{
		    InitializeDecorator();
		    InitializeLists();
            InitializeComponent();

            Contexts["IsActionFilter"] = tabPageActions;
            Contexts["IsFormattingFilter"] = tabPageFormatting;
            Contexts["IsTrayIconFilter"] = tabPageTrayIcon;
            Contexts["IsExpirationFilter"] = tabPageExpiration;

            SwitchContext( rulesType );
            tabRulesTypes.SelectedTab = (TabPage) Contexts[ rulesType ];

            CheckMajorButtonsAccessibility();
            CheckMoveButtons();
            RestoreSettings();
		}

        private void InitializeDecorator()
        {
            _decorator = new RuleDecorator();
            _rulesWithErrors = Core.ResourceStore.FindResourcesWithPropLive( null, Core.Props.LastError );
            _rulesWithErrors.ResourceDeleting += _decorator.OnErrorRuleChanged;
        }

	    private void InitializeLists()
        {
            String[] contextKeys = new String[] {"IsActionFilter", "IsFormattingFilter", "IsTrayIconFilter", "IsExpirationFilter"};
            IResourceList invisibles = Store.FindResources( null, "Invisible", true );
            foreach( string context in contextKeys )
            {
                IResourceList allRules = Store.FindResourcesWithProp( null, context );
                allRules = allRules.Minus( invisibles );
                allRules = FilterOutRulesByLoadedPluginType( allRules );

                switch( context )
                {
                    case "IsActionFilter": InitList( ref _listActionRules, "_listActionRules", allRules ); break;
                    case "IsFormattingFilter": InitList( ref _listFormattingRules, "_listFormattingRules", allRules ); break;
                    case "IsTrayIconFilter": InitList( ref _listTrayIconRules, "_listTrayIconRules", allRules ); break;
                    case "IsExpirationFilter": InitList( ref _listExpirationRules, "_listExpirationRules", allRules ); break;
                }
            }
        }

	    private static IResourceList FilterOutRulesByLoadedPluginType( IResourceList allRules )
        {
            IResourceList result = Core.ResourceStore.EmptyResourceList;
            foreach( IResource rule in allRules )
            {
                string type = rule.GetStringProp( Core.Props.ContentType );
                if( !ResourceTypeHelper.IsResourceTypePassive( type ) )
                    result = result.Union( rule.ToResourceList() );
            }
            return result;
        }

        private void InitList(ref CheckedPlainListBox list, string name, IResourceList rules)
        {
            list = new CheckedPlainListBox();
            list.Name = name;
            list.TabIndex = 1;
            list.Location = new Point(0, 0);
            list.Size = new Size(300, 246);
            list.DoubleClick += list_DoubleClick;
            list.SelectionChanged += list_SelectionChanged;

            list.AddDecorator(_decorator);

            int propId = Core.ResourceStore.PropTypes["Order"].Id;
            rules.Sort( new SortSettings(propId, true) );

            list.Resources = rules;

            foreach (IResource res in rules)
            {
                list.SetCheckState( res, res.HasProp("RuleTurnedOff") ? CheckBoxState.Unchecked : CheckBoxState.Checked );
            }
            if( rules.Count > 0 )
                list.SelectSingleItem( rules[ 0 ] );
        }
        #endregion Ctor and Initialization

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
            this.newButton = new System.Windows.Forms.Button();
            this.topLabel = new System.Windows.Forms.Label();
            this.removeButton = new System.Windows.Forms.Button();
            this.editButton = new System.Windows.Forms.Button();
            this.buttonMoveDown = new System.Windows.Forms.Button();
            this.buttonMoveUp = new System.Windows.Forms.Button();
            this.buttonCopyRule = new System.Windows.Forms.Button();
            this.tabRulesTypes = new System.Windows.Forms.TabControl();
            this.tabPageActions = new System.Windows.Forms.TabPage();
            this.tabPageTrayIcon = new System.Windows.Forms.TabPage();
            this.tabPageFormatting = new System.Windows.Forms.TabPage();
            this.tabPageExpiration = new System.Windows.Forms.TabPage();
            this.tabPageActions.SuspendLayout();
            this.tabPageTrayIcon.SuspendLayout();
            this.tabPageFormatting.SuspendLayout();
            this.tabPageExpiration.SuspendLayout();
            this.tabRulesTypes.SuspendLayout();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.helpButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // newButton
            //
            this.newButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.newButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.newButton.Location = new System.Drawing.Point(302, 48);
            this.newButton.Name = "newButton";
            this.newButton.TabIndex = 3;
            this.newButton.Text = "&New...";
            this.newButton.Click += new System.EventHandler(this.newButton_Click);
            //
            // topLabel
            //
            this.topLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.topLabel.Location = new System.Drawing.Point(8, 8);
            this.topLabel.Name = "topLabel";
            this.topLabel.Size = new System.Drawing.Size(76, 16);
            this.topLabel.TabIndex = 1;
            this.topLabel.Text = "&Available rules:";
            //
            // editButton
            //
            this.editButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.editButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.editButton.Location = new System.Drawing.Point(302, 80);
            this.editButton.Name = "editButton";
            this.editButton.TabIndex = 4;
            this.editButton.Text = "&Edit...";
            this.editButton.Click += new System.EventHandler(this.editButton_Click);
            //
            // buttonCopyRule
            //
            this.buttonCopyRule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCopyRule.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCopyRule.Location = new System.Drawing.Point(302, 112);
            this.buttonCopyRule.Name = "buttonCopyRule";
            this.buttonCopyRule.TabIndex = 5;
            this.buttonCopyRule.Text = "&Copy Rule";
            this.buttonCopyRule.Click += new System.EventHandler(this.buttonCopyRule_Click);
            //
            // removeButton
            //
            this.removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.removeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.removeButton.Location = new System.Drawing.Point(302, 144);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(75, 24);
            this.removeButton.TabIndex = 7;
            this.removeButton.Text = "&Delete...";
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            //
            // buttonMoveUp
            //
            this.buttonMoveUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMoveUp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonMoveUp.Location = new System.Drawing.Point(302, 184);
            this.buttonMoveUp.Name = "buttonMoveUp";
            this.buttonMoveUp.TabIndex = 8;
            this.buttonMoveUp.Text = "Move &Up";
            this.buttonMoveUp.Click += new System.EventHandler(this.buttonMoveUp_Click);
            //
            // buttonMoveDown
            //
            this.buttonMoveDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMoveDown.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonMoveDown.Location = new System.Drawing.Point(302, 216);
            this.buttonMoveDown.Name = "buttonMoveDown";
            this.buttonMoveDown.TabIndex = 9;
            this.buttonMoveDown.Text = "Move &Down";
            this.buttonMoveDown.Click += new System.EventHandler(this.buttonMoveDown_Click);
            //
            // tabRulesTypes
            //
            this.tabRulesTypes.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            this.tabRulesTypes.Location = new System.Drawing.Point(4, 28);
            this.tabRulesTypes.Name = "tabRulesTypes";
            this.tabRulesTypes.SelectedIndex = 0;
            this.tabRulesTypes.Size = new System.Drawing.Size(292, 276);
            this.tabRulesTypes.TabIndex = 11;
            this.tabRulesTypes.SelectedIndexChanged += new System.EventHandler(this.RulesTypes_TabChanged);

            CreateTab(tabPageActions, _listActionRules, "tabPageActions", "IsActionFilter", "Action");
            CreateTab(tabPageTrayIcon, _listTrayIconRules, "tabPageTrayIcon", "IsTrayIconFilter", "Tray Icon");
            CreateTab(tabPageFormatting, _listFormattingRules, "tabPageFormatting", "IsFormattingFilter", "Font and Color");
            CreateTab(tabPageExpiration, _listExpirationRules, "tabPageExpiration", "IsExpirationFilter", "Auto Expire");

            this.tabRulesTypes.Controls.Add(this.tabPageActions);
            this.tabRulesTypes.Controls.Add(this.tabPageFormatting);
            this.tabRulesTypes.Controls.Add(this.tabPageTrayIcon);
            this.tabRulesTypes.Controls.Add(this.tabPageExpiration);
            //
            // okButton
            //
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.okButton.Location = new System.Drawing.Point(138, 312);
            this.okButton.Name = "okButton";
            this.okButton.TabIndex = 20;
            this.okButton.Text = "OK";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelButton.Location = new System.Drawing.Point(218, 312);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.TabIndex = 21;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            //
            // helpButton
            //
            this.helpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.helpButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.helpButton.Location = new System.Drawing.Point(298, 312);
            this.helpButton.Name = "helpButton";
            this.helpButton.TabIndex = 22;
            this.helpButton.Text = "Help";
            this.helpButton.Click += new EventHandler(helpButton_Click);
            //
            // RulesManagerForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(384, 341);
            this.Controls.Add(this.tabRulesTypes);
            this.Controls.Add(this.buttonMoveDown);
            this.Controls.Add(this.newButton);
            this.Controls.Add(this.topLabel);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.editButton);
            this.Controls.Add(this.buttonMoveUp);
            this.Controls.Add(this.buttonCopyRule);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.helpButton);
            this.MinimumSize = new System.Drawing.Size(230, 320);
            this.Name = "RulesManagerForm";
            this.Text = "Rules Manager";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyDownHandler);
            this.tabRulesTypes.ResumeLayout(false);
            this.tabPageActions.ResumeLayout(false);
            this.tabPageTrayIcon.ResumeLayout(false);
            this.tabPageFormatting.ResumeLayout(false);
            this.tabPageExpiration.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private TabPage CreateTab( TabPage page, CheckedPlainListBox list, String name, String tagString, String title)
        {
            page.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            page.Controls.Add( list );
            page.Location = new System.Drawing.Point( 4, 22 );
            page.Name = name;
            page.Size = new System.Drawing.Size( 284, 250 );
            page.TabIndex = 0;
            page.Tag = tagString;
            page.Text = title;
            list.Dock = DockStyle.Fill;
            return page;
        }
	    #endregion

        #region New, Edit, Remove
        private void newButton_Click(object sender, EventArgs e)
        {
            ViewCommonDialogBase form = CreateForm();
            if( form.ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                if( !_listRules.Contains( form.ResultResource ) ) // new + overwrite
                {
                    _listRules.Nodes.AddResource(form.ResultResource);
                    _listRules.SetCheckState( form.ResultResource, CheckBoxState.Checked );
                    _listRules.SelectSingleItem( form.ResultResource );
                }

                RemovePossiblyDeletedRules();

                //  Set the date of the rule's creation.
                //  NB: To be used in future.
                new ResourceProxy( form.ResultResource ).SetProp( Core.Props.Date, DateTime.Now );
                AddedRules.Add( form.ResultResource );
            }
            form.Dispose();
            CheckMajorButtonsAccessibility();
            CheckMoveButtons();
        }

        private void editButton_Click(object sender, EventArgs e)
        {
            EditRule( _listRules.SelectedResource );
        }
        private void list_DoubleClick(object sender, HandledEventArgs e)
        {
            if (removeButton.Enabled)
                EditRule(_listRules.SelectedResource);
        }
        private void EditRule(IResource rule)
        {
            IResource   result = Core.FilteringFormsManager.ShowEditResourceForm( rule );
            if( result != null )
            {
                //  Ensure that forms that are called to edit the existing
                //  resources always edit exactly them and not create new
                //  resources with the required parameters
                if( rule.Id != result.Id )
                    throw new ApplicationException( "RulesManager -- Internal error: rule editing violates persistency contract." );

                _listRules.SelectSingleItem( result );

                RemovePossiblyDeletedRules();

                //  if edited rule was added within the same session - remember it,
                //  so that we still have the ability to remove it on Cancel action
                if( AddedRules.IndexOf( rule ) != -1 )
                    AddedRules.Add( rule );
            }
            CheckMajorButtonsAccessibility();
            CheckMoveButtons();
            editButton.Focus(); // smtimes focus goes away to other buttons.
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            removeButton.Focus();  // smtimes focus goes away to other buttons.

            IResource rule = _listRules.SelectedResource;
            string name = rule.GetStringProp( "DeepName" );
            if( MessageBox.Show( "Delete rule \"" + name + "\"?", "Rules manager",
                                 MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation ) == DialogResult.Yes )
            {
                int index = _listRules.SelectedIndex;
                _listRules.Nodes.RemoveResource( rule );

                //  differentiation has to be changed for adequate OK/Cancel
                //  dialog behavior - if we remove the view which was added in
                //  the save session, we have to remove it independently of
                //  OK/Cancel action.
                if( AddedRules.IndexOf( rule ) != -1 )
                {
                    AddedRules.Remove( rule );
                    DeleteRule( rule );
                }
                else
                {
                    //  Rename just "removed" (actually, hidden from the list) rule,
                    //  so that if any new rule with the same name is created,
                    //  there will be no duplicate.
                    RenameRule( rule, "###-" + name + "-### " );
                    RemovedRules.Add( rule );
                }

                //-------------------------------------------------------------
                int itemsCount = _listRules.Nodes.Count;
                if (itemsCount > 0)
                    _listRules.SelectSingleItem( ( itemsCount > index )
                                                     ? _listRules.Nodes[index]
                                                     : _listRules.Nodes[itemsCount - 1]);

                CheckMajorButtonsAccessibility();
                CheckMoveButtons();
            }
            removeButton.Focus();  // smtimes focus goes away to other buttons.
            buttonCopyRule.Enabled = (_listRules.SelectedIndex != -1);
        }

        //---------------------------------------------------------------------
        //  In the case when the rule we have just edited changed its name to
        //  the one already existing, that resource has been removed from the
        //  ResourceStore - thus remove that entry from the list.
        //---------------------------------------------------------------------
        private void  RemovePossiblyDeletedRules()
        {
            for( int i = 0; i < _listRules.Nodes.Count; i++ )
            {
                IResource res = _listRules.Nodes[ i ];
                if (res.IsDeleted || res.IsDeleting)
                {
                    _listRules.Nodes.RemoveResource(res);
                    AddedRules.Remove( res ); // in the case of rename
                    break;
                }
            }
        }
        #endregion New, Edit, Remove

        #region OK/Cancel
        private void okButton_Click(object sender, EventArgs e)
        {
            okButton.Enabled = cancelButton.Enabled = false;
            foreach( string context in Contexts.Keys )
            {
                SwitchContext( context );
                tabRulesTypes.SelectedTab.Tag = context;

                foreach( IResource rule in RemovedRules )
                    DeleteRule( rule );

                for (int i = 0; i < _listRules.Nodes.Count; i++)
                {
                    IResource rule = _listRules.Nodes[ i ];
                    FMan.AssignOrderNumber( rule, i );
                    if( _listRules.GetCheckState( rule ) == CheckBoxState.Checked )
                        FMan.ActivateRule( rule );
                    else
                        FMan.DeactivateRule( rule );
                }
            }
            DialogResult = DialogResult.OK;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            okButton.Enabled = cancelButton.Enabled = false;
            foreach( string context in Contexts.Keys )
            {
                SwitchContext( context );
                tabRulesTypes.SelectedTab.Tag = context;

                foreach( IResource res in AddedRules )
                    DeleteRule( res );

                //  restore hidden rules to their origin names
                foreach( IResource rule in RemovedRules )
                    RecoverName( rule );
            }
        }
        #endregion OK/Cancel

        #region MoveUp/MoveDown
        private void buttonMoveDown_Click(object sender, EventArgs e)
        {
            IResource   temp = _listRules.SelectedResource;
            int         currIndex = _listRules.SelectedIndex;
            CheckBoxState state = _listRules.GetCheckState(temp);

            _listRules.Nodes.RemoveResource( temp );
            _listRules.Nodes.AddResourceAt( temp, currIndex + 1 );
            _listRules.SetCheckState( temp, state );
            _listRules.SelectSingleItem( temp );

            CheckMoveButtons();
        }

        private void buttonMoveUp_Click(object sender, EventArgs e)
        {
            IResource temp = _listRules.SelectedResource;
            int currIndex = _listRules.SelectedIndex;
            CheckBoxState state = _listRules.GetCheckState(temp);

            _listRules.Nodes.RemoveResource(temp);
            _listRules.Nodes.AddResourceAt(temp, currIndex - 1);
            _listRules.SetCheckState(temp, state);
            _listRules.SelectSingleItem(temp);

            CheckMoveButtons();
        }
        #endregion MoveUp/MoveDown

        #region CopyRule
        private void buttonCopyRule_Click(object sender, EventArgs e)
        {
            IResource rule = _listRules.SelectedResource;
            string newName = ConstructNewName( rule.DisplayName );
            try
            {
                IResource newRule = CloneRuleInContext( rule, newName );

                _listRules.Nodes.AddResource( newRule );
                Debug.Assert( AddedRules.IndexOf( newRule ) == -1 );
                AddedRules.Add( newRule );
                _listRules.SelectSingleItem( newRule );
            }
            catch( Exception )
            {
                MessageBox.Show( "Failed to create a copy of the rule.", "Rules Manager",
                                 MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
            }
            buttonCopyRule.Focus();   // smtimes focus goes away to other buttons.
        }

        private string  ConstructNewName( string name )
        {
            //  Construct a name for a new view.
            string newName = "Copy of " + name;
            bool   exist = IsRuleExist( newName );
            if( exist )
            {
                for( int i = 2;; i++ )
                {
                    newName = "Copy of " + name + "(" + i + ")";
                    exist = IsRuleExist( newName );
                    if( !exist )
                        break;
                }
            }
            return newName;
        }
        #endregion CopyRule

        #region Event Handlers
        private void  RulesTypes_TabChanged(object sender, EventArgs e)
        {
            SwitchContext( Context );

            CheckMajorButtonsAccessibility();
            CheckMoveButtons();
        }

        private void list_SelectionChanged(object sender, EventArgs e)
        {
            CheckMajorButtonsAccessibility();
            CheckMoveButtons();
        }

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if( _listRules.SelectedIndex != -1 )
            {
                if( e.KeyCode == Keys.Delete && !e.Shift &&
                    e.Modifiers != Keys.Alt && e.Modifiers != Keys.ControlKey )
                {
                    removeButton_Click( null, null );
                }
            }
        }
        #endregion Event Handlers

        #region Context-dependent stuff
        private string  Context
        {  get { return (string)tabRulesTypes.SelectedTab.Tag; }  }

        private void  SwitchContext( string context )
        {
            switch( context )
            {
                case "IsActionFilter":     SwitchContext(_listActionRules, AddedRulesAction, RemovedRulesAction); break;
                case "IsFormattingFilter": SwitchContext(_listFormattingRules, AddedRulesFormatting, RemovedRulesFormatting); break;
                case "IsTrayIconFilter":   SwitchContext(_listTrayIconRules, AddedRulesTrayIcon, RemovedRulesTrayIcon); break;
                case "IsExpirationFilter": SwitchContext(_listExpirationRules, AddedRulesExpiration, RemovedRulesExpiration); break;
                default: Debug.Assert( false ); break;
            }
        }

        private void SwitchContext( CheckedPlainListBox listBox, ArrayList added, ArrayList deleted )
        {
            _listRules = listBox;
            AddedRules = added;
            RemovedRules = deleted;
        }

        private ViewCommonDialogBase  CreateForm()
        {
            switch( Context )
            {
                case "IsActionFilter": return new EditRuleForm();
                case "IsFormattingFilter": return new EditFormattingRuleForm();
                case "IsTrayIconFilter": return new EditTrayIconRuleForm();
                case "IsExpirationFilter": return new EditExpirationRuleForm( null );
                default: return null;
            }
        }

        private void  RenameRule( IResource rule, string newName )
        {
            switch( Context )
            {
                case "IsActionFilter": Core.FilterRegistry.RenameRule( rule, newName ); break;
                case "IsFormattingFilter": Core.FormattingRuleManager.RenameRule( rule, newName ); break;
                case "IsTrayIconFilter": Core.TrayIconManager.RenameRule( rule, newName ); break;
                case "IsExpirationFilter": Core.ExpirationRuleManager.RenameRule( rule, newName ); break;
                default: Debug.Assert( false ); break;
            }
        }

        private void  DeleteRule( IResource rule )
        {
            string  ruleName = rule.GetStringProp( Core.Props.Name );
            switch( Context )
            {
                case "IsActionFilter": Core.FilterRegistry.DeleteRule( ruleName ); break;
                case "IsFormattingFilter": Core.FormattingRuleManager.UnregisterRule( ruleName ); break;
                case "IsTrayIconFilter": Core.TrayIconManager.UnregisterTrayIconRule( ruleName ); break;
                case "IsExpirationFilter": Core.ExpirationRuleManager.UnregisterRule( ruleName ); break;
                default: Debug.Assert( false ); break;
            }
        }

        private IResource  CloneRuleInContext( IResource rule, string newName )
        {
            IResource newRule = null;
            switch( Context )
            {
                case "IsActionFilter": newRule = CloneActionRule( newName, rule ); break;
                case "IsFormattingFilter": newRule = Core.FormattingRuleManager.CloneRule( rule, newName ); break;
                case "IsTrayIconFilter": newRule = Core.TrayIconManager.CloneRule( rule, newName ); break;
                default: Debug.Assert( false ); break;
            }
            return newRule;
        }

        private bool  IsRuleExist( string newName )
        {
            bool  exists = false;
            switch( Context )
            {
                case "IsActionFilter": exists = (Core.FilterRegistry.FindRule( newName ) != null); break;
                case "IsFormattingFilter": exists = (Core.FormattingRuleManager.FindRule( newName ) != null); break;
                case "IsTrayIconFilter": exists = (Core.TrayIconManager.FindRule( newName ) != null); break;
                default: Debug.Assert( false ); break;
            }
            return exists;
        }

        private static IResource CloneActionRule( string newName, IResource from )
        {
            #region Preconditions
            IResource res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleResName, "Name", newName );
            if( res != null )
                throw new AmbiguousMatchException( "Can not assigned a name which is already in use for a cloned rule." );
            #endregion Preconditions

            IResource[][] conditionGroups;
            IResource[]   exceptions;
            FilterRegistry.CloneConditionTypeLinks( from, out conditionGroups, out exceptions );

            IResourceList actionsList = FMan.GetActions( from );
            IResource[] actions = new IResource[ actionsList.Count ];
            for( int i = 0; i < actionsList.Count; i++ )
                actions[ i ] = FMan.CloneAction( actionsList[ i ] );

            string[] formTypes = FilterRegistry.CompoundType( from );
            string   eventName = from.GetStringProp( "EventName" );
            IResource newRule = FMan.RegisterRule( eventName, newName, formTypes, conditionGroups, exceptions, actions );
            return newRule;
        }
        #endregion Context-dependent stuff

        #region Misc
        private void CheckMajorButtonsAccessibility()
        {
            if( _listRules != null )
            {
                bool hasSel = (_listRules.SelectedIndex != -1);

                newButton.Enabled = !(Context == "IsExpirationFilter");
                editButton.Enabled = removeButton.Enabled = hasSel;
                buttonCopyRule.Enabled = !(Context == "IsExpirationFilter") && hasSel;
            }
        }
        private void CheckMoveButtons()
        {
            if (_listRules != null)
            {
                int count = _listRules.Nodes.Count, selected = _listRules.SelectedIndex;
                buttonMoveDown.Enabled = ( count > 1 ) && ( selected != count - 1 );
                buttonMoveUp.Enabled = ( count > 1 ) && ( selected != 0 );
            }
        }

        private void  RecoverName( IResource rule )
        {
            string name = rule.GetStringProp( Core.Props.Name );
            RenameRule( rule, name.Substring( 4, name.Length - 9 ) );
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "reference\\manage_rules.html" );
        }
        #endregion Misc
    }
}
