// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.FiltersManagement;

namespace JetBrains.Omea
{
	/**
     * Dialog for configuring the notifications for the specified resource.
     */

    public class NotifyMeDlg : DialogBase
	{
        private Button _btnOK;
        private Button _btnCancel;
        private Label label1;
        private Panel _separator;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private static int _propNotifyMeRule;
        private IResource _targetResource;
        private IResource[] _notifyMeConditionTemplates;
        private Label _notifyMeConditionLabel;
        private CheckBox[] _notifyMeConditionCheckboxes;

        private const string _playSoundTemplateName = "Play sound from %file%";
        private Panel   _actionPanel;
        private TextBox _edtMessage;
        private TextBox _edtSoundName;
        private CheckBox _chkShowMessage;
        private Button  _btnBrowse;
        private CheckBox _chkPlaySound;
        private CheckBox _chkShowDesktopAlert;
        private OpenFileDialog _openFileDialog;

        private const string _showMessageTemplateName = "Display message box with %text%";
        private const int    _ConditionCheckboxVerSize = 24;

		public NotifyMeDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this._separator = new System.Windows.Forms.Panel();
            this._actionPanel = new System.Windows.Forms.Panel();
            this._edtMessage = new System.Windows.Forms.TextBox();
            this._edtSoundName = new System.Windows.Forms.TextBox();
            this._chkShowMessage = new System.Windows.Forms.CheckBox();
            this._btnBrowse = new System.Windows.Forms.Button();
            this._chkPlaySound = new System.Windows.Forms.CheckBox();
            this._chkShowDesktopAlert = new System.Windows.Forms.CheckBox();
            this._openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this._actionPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // _btnOK
            //
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(220, 252);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 0;
            this._btnOK.Text = "OK";
            //
            // _btnCancel
            //
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(304, 252);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 1;
            this._btnCancel.Text = "Cancel";
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(4, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(148, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "Notify when messages arrive:";
            //
            // _separator
            //
            this._separator.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._separator.Location = new System.Drawing.Point(4, 44);
            this._separator.Name = "_separator";
            this._separator.Size = new System.Drawing.Size(380, 4);
            this._separator.TabIndex = 3;
            //
            // _actionPanel
            //
            this._actionPanel.Controls.Add(this._edtMessage);
            this._actionPanel.Controls.Add(this._edtSoundName);
            this._actionPanel.Controls.Add(this._chkShowMessage);
            this._actionPanel.Controls.Add(this._btnBrowse);
            this._actionPanel.Controls.Add(this._chkPlaySound);
            this._actionPanel.Controls.Add(this._chkShowDesktopAlert);
            this._actionPanel.Location = new System.Drawing.Point(0, 48);
            this._actionPanel.Name = "_actionPanel";
            this._actionPanel.Size = new System.Drawing.Size(384, 84);
            this._actionPanel.TabIndex = 10;
            //
            // _edtMessage
            //
            this._edtMessage.Enabled = false;
            this._edtMessage.Location = new System.Drawing.Point(128, 56);
            this._edtMessage.Name = "_edtMessage";
            this._edtMessage.Size = new System.Drawing.Size(250, 21);
            this._edtMessage.TabIndex = 15;
            this._edtMessage.Text = "";
            this._edtMessage.TextChanged += new System.EventHandler(this._edtMessage_TextChanged);
            //
            // _edtSoundName
            //
            this._edtSoundName.Enabled = false;
            this._edtSoundName.Location = new System.Drawing.Point(128, 28);
            this._edtSoundName.Name = "_edtSoundName";
            this._edtSoundName.Size = new System.Drawing.Size(172, 21);
            this._edtSoundName.TabIndex = 12;
            this._edtSoundName.Text = "";
            this._edtSoundName.TextChanged += new System.EventHandler(this._edtSoundName_TextChanged);
            //
            // _chkShowMessage
            //
            this._chkShowMessage.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkShowMessage.Location = new System.Drawing.Point(8, 60);
            this._chkShowMessage.Name = "_chkShowMessage";
            this._chkShowMessage.Size = new System.Drawing.Size(104, 20);
            this._chkShowMessage.TabIndex = 14;
            this._chkShowMessage.Text = "Show Message";
            this._chkShowMessage.CheckedChanged += new System.EventHandler(this._chkShowMessage_CheckedChanged);
            //
            // _btnBrowse
            //
            this._btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnBrowse.Location = new System.Drawing.Point(304, 28);
            this._btnBrowse.Name = "_btnBrowse";
            this._btnBrowse.TabIndex = 13;
            this._btnBrowse.Text = "Browse...";
            this._btnBrowse.Click += new System.EventHandler(this._btnBrowse_Click);
            //
            // _chkPlaySound
            //
            this._chkPlaySound.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkPlaySound.Location = new System.Drawing.Point(8, 32);
            this._chkPlaySound.Name = "_chkPlaySound";
            this._chkPlaySound.Size = new System.Drawing.Size(104, 20);
            this._chkPlaySound.TabIndex = 11;
            this._chkPlaySound.Text = "Play Sound";
            this._chkPlaySound.CheckedChanged += new System.EventHandler(this._chkPlaySound_CheckedChanged);
            //
            // _chkShowDesktopAlert
            //
            this._chkShowDesktopAlert.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkShowDesktopAlert.Location = new System.Drawing.Point(8, 4);
            this._chkShowDesktopAlert.Name = "_chkShowDesktopAlert";
            this._chkShowDesktopAlert.Size = new System.Drawing.Size(172, 20);
            this._chkShowDesktopAlert.TabIndex = 10;
            this._chkShowDesktopAlert.Text = "Show Desktop Alert";
            //
            // _openFileDialog
            //
            this._openFileDialog.Filter = "Sound files (*.WAV)|*.WAV|All files|*.*";
            //
            // NotifyMeDlg
            //
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(386, 285);
            this.Controls.Add(this._actionPanel);
            this.Controls.Add(this._separator);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "NotifyMeDlg";
            this.Text = "Notify Me";
            this._actionPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        public static void RegisterTypes()
        {
            IResourceStore store = ICore.Instance.ResourceStore;
            _propNotifyMeRule = store.PropTypes.Register( "NotifyMeRule", PropDataType.Link,
                PropTypeFlags.Internal );
        }

        public void ShowNotifyMeDialog( IResource res )
        {
            _targetResource = res;

            _notifyMeConditionTemplates = GetValidNotifyConditions();
            if ( _notifyMeConditionTemplates.Length == 0 )
            {
                MessageBox.Show( Core.MainWindow,
                    "There are no valid notify conditions for '" + res.DisplayName + "'",
                    "Notify Me" );
                return;
            }

            if ( _notifyMeConditionTemplates.Length > 1 )
            {
                ShowNotifyConditionCheckboxes();
            }
            else
            {
                string condName = GetNotifyConditionName( _notifyMeConditionTemplates [0] );
                _notifyMeConditionLabel = new Label();
                _notifyMeConditionLabel.FlatStyle = FlatStyle.System;
                _notifyMeConditionLabel.Text = condName;
                _notifyMeConditionLabel.AutoSize = true;
                _notifyMeConditionLabel.Location = new Point( 4, 24 );
                Controls.Add( _notifyMeConditionLabel );
            }

            _btnOK.Top = _actionPanel.Bottom + 4;
            _btnCancel.Top = _btnOK.Top;
            Height = (Height - ClientSize.Height) + _btnOK.Bottom + 8;

            ShowExistingNotifyRule();

            if ( ShowDialog() == DialogResult.OK )
            {
                ICore.Instance.ResourceAP.RunJob( new MethodInvoker( SaveNotifyRule ) );
            }
        }

        /**
         * Returns the list of notify conditions which have valid parameters
         * for the specified resource.
         */

        private IResource[] GetValidNotifyConditions()
        {
            ArrayList templateList = new ArrayList( Core.NotificationManager.GetNotifyMeConditions( _targetResource.Type ) );
            for( int i=templateList.Count-1; i >= 0; i-- )
            {
                IResource conditionTemplate = (IResource) templateList [i];
                if ( GetNotifyConditionName( conditionTemplate ) == null )
                {
                    templateList.RemoveAt( i );
                }
            }
            return (IResource[]) templateList.ToArray( typeof (IResource) );
        }

        /**
         * Create checkboxes for the multiple available notify conditions and check them
         * if they are already used in the existing rule.
         */

        private void ShowNotifyConditionCheckboxes()
        {
            IResource rule = _targetResource.GetLinkProp( _propNotifyMeRule );
            int y = _ConditionCheckboxVerSize;
            _notifyMeConditionCheckboxes = new CheckBox[ _notifyMeConditionTemplates.Length ];
            for( int i = 0; i < _notifyMeConditionTemplates.Length; i++ )
            {
                IResource condition = _notifyMeConditionTemplates [i];
                CheckBox chkCondition = new CheckBox();
                chkCondition.Location = new Point( 8, y );
                chkCondition.FlatStyle = FlatStyle.System;
                chkCondition.Text = GetNotifyConditionName( condition );
                chkCondition.Size = new Size( Width - 12, _ConditionCheckboxVerSize );
                chkCondition.CheckedChanged += new EventHandler( OnConditionCheckedChanged );

                if ( rule != null )
                {
                    chkCondition.Checked = IsConditionTemplateUsed( condition, rule );
                }
                Controls.Add( chkCondition );
                _notifyMeConditionCheckboxes [i] = chkCondition;

                y += _ConditionCheckboxVerSize;
            }
            UpdateButtonState();
            _separator.Top = y;
            _actionPanel.Top = y + 4;
        }

	    private void OnConditionCheckedChanged( object sender, EventArgs e )
	    {
            UpdateButtonState();
	    }

	    /**
         * If all conditions are unchecked, disables the OK button
         */

        private void UpdateButtonState()
	    {
            bool anyChecked = true;
            if ( _notifyMeConditionCheckboxes != null )
            {
                anyChecked = false;
                foreach( CheckBox chk in _notifyMeConditionCheckboxes )
                {
                    if ( chk != null && chk.Checked )
                    {
                        anyChecked = true;
                        break;
                    }
                }
            }

            if ( !anyChecked )
            {
                _btnOK.Enabled = false;
            }
            else
            {
                if ( ( _chkPlaySound.Checked && _edtSoundName.Text.Length == 0 ) ||
                     ( _chkShowMessage.Checked && _edtMessage.Text.Length == 0 ) )
                {
                    _btnOK.Enabled = false;
                }
                else
                {
                    _btnOK.Enabled = true;
                }
            }
	    }

	    /**
         * Returns true if the specified condition template is used in the specified rule.
         */

        private static bool IsConditionTemplateUsed( IResource conditionTemplate, IResource rule )
        {
            string templateName = conditionTemplate.GetStringProp( Core.Props.Name );
            foreach( IResource condition in Core.FilterRegistry.GetConditions( rule ) )
            {
                IResource template = condition.GetLinkProp( "TemplateLink" );
                if(( template != null ) && ( template.GetStringProp( Core.Props.Name ) == templateName ))
                    return true;
            }
            return false;
        }


        /**
         * If a notify rule already exists for the target resource, show its parameters
         * in the dialog.
         */

        private void ShowExistingNotifyRule()
        {
            IResource rule = _targetResource.GetLinkProp( _propNotifyMeRule );
            if ( rule != null )
            {
                IResourceList actions = Core.FilterRegistry.GetActions( rule );
                foreach( IResource action in actions )
                {
                    IResource template = action.GetLinkProp( "TemplateLink" );
                    string    templateName = (template != null) ? template.GetStringProp( Core.Props.Name ) : string.Empty;
                    if( action.GetStringProp( Core.Props.Name ) == "Show desktop alert" )
                    {
                        _chkShowDesktopAlert.Checked = true;
                    }
                    else if( templateName == _playSoundTemplateName )
                    {
                        _chkPlaySound.Checked = true;
                        _edtSoundName.Text = action.GetStringProp( "ConditionVal" );
                    }
                    else if( templateName == _showMessageTemplateName )
                    {
                        _chkShowMessage.Checked = true;
                        _edtMessage.Text = action.GetStringProp( "ConditionVal" );
                    }
                }
            }
        }

	    private void SaveNotifyRule()
	    {
            IResource rule = _targetResource.GetLinkProp( _propNotifyMeRule );
            if ( rule != null )
            {
                Core.FilterRegistry.DeleteRule( rule );
            }

            string ruleName;
            IResource[] ruleConditions = BuildRuleConditions( out ruleName );
            IResource[] ruleActions = BuildRuleActions();

            if ( ruleActions.Length > 0 )
            {
                string  ruleResType = Core.NotificationManager.GetRuleResourceType( _targetResource.Type );
                rule = Core.FilterRegistry.RegisterRule( StandardEvents.ResourceReceived, ruleName,
                    ( ruleResType == null )? null : new string[ 1 ]{ ruleResType },
                    ruleConditions, null, ruleActions );

                _targetResource.AddLink( _propNotifyMeRule, rule );
            }
	    }

        /**
         * Returns the array of conditions for the selected condition template
         * or templates.
         */

        private IResource[] BuildRuleConditions( out string ruleName )
        {
            StringBuilder ruleNameBuilder = new StringBuilder( "Notify Me: " );
            ArrayList conditions = new ArrayList();
            if ( _notifyMeConditionTemplates.Length == 1 )
            {
                ruleNameBuilder.Append( GetNotifyConditionName( _notifyMeConditionTemplates [0] ) );
                conditions.Add( BuildRuleCondition( _notifyMeConditionTemplates [0] ) );
            }
            else
            {
                for( int i=0; i<_notifyMeConditionTemplates.Length; i++ )
                {
                    if ( _notifyMeConditionCheckboxes [i].Checked )
                    {
                        if ( conditions.Count > 0 )
                        {
                            ruleNameBuilder.Append( ", " );
                        }
                        ruleNameBuilder.Append( GetNotifyConditionName( _notifyMeConditionTemplates [i] ) );
                        conditions.Add( BuildRuleCondition( _notifyMeConditionTemplates [i] ) );
                    }
                }
            }

            ruleName = ruleNameBuilder.ToString();
            return (IResource[]) conditions.ToArray( typeof(IResource) );
        }

        /**
         * Creates a condition with an appropriate parameter from the specified template.
         */

        private IResource BuildRuleCondition( IResource conditionTemplate )
        {
            IResource param = GetConditionParameter( conditionTemplate );
            return FilterConvertors.InstantiateTemplate( conditionTemplate, param.ToResourceList(),
                new string[ 1 ] { _targetResource.Type } );
        }

        /**
         * Returns an array of rule actions currently checked in the dialog.
         */

        private IResource[] BuildRuleActions()
        {
            ArrayList ruleActions = new ArrayList();

            if ( _chkShowDesktopAlert.Checked )
            {
                ruleActions.Add( Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName,
                                                                        "Name", "Show desktop alert" ) );
            }
            if ( _chkPlaySound.Checked && _edtSoundName.Text != "" )
            {
                IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionTemplateResName,
                                                                            "Name", _playSoundTemplateName );
                ruleActions.Add( FilterConvertors.Template2Action( template, _edtSoundName.Text, null ) );
            }
            if ( _chkShowMessage.Checked && _edtMessage.Text != "" )
            {
                IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionTemplateResName,
                                                                            "Name", _showMessageTemplateName );
                ruleActions.Add( FilterConvertors.Template2Action( template, _edtMessage.Text, null ) );
            }

            return (IResource[]) ruleActions.ToArray( typeof(IResource) );
        }

	    /**
         * Returns the name of the condition displayed in the dialog for the specified
         * condition template.
         */

        private string GetNotifyConditionName( IResource conditionTemplate )
        {
            IResource param = GetConditionParameter( conditionTemplate );
            if ( param == null )
                return null;

            string visualName = conditionTemplate.GetStringProp( Core.Props.Name );
            if ( param != null )
            {
                int startPercent = visualName.IndexOf( '%' );
                if ( startPercent >= 0 )
                {
                    int endPercent = visualName.IndexOf( '%', startPercent+1 );
                    if ( endPercent >= 0 )
                    {
                        return visualName.Substring( 0, startPercent ) + "'" + param.DisplayName +
                            "'" + visualName.Substring( endPercent+1 );
                    }
                }
            }
            return visualName;
        }

        /**
         * Returns either the target resource or the resource linked to it with the
         * link type registered for the specified condition.
         */

        private IResource GetConditionParameter( IResource conditionTemplate )
        {
            int linkType = Core.NotificationManager.GetConditionLinkType( _targetResource.Type, conditionTemplate );

            IResource param = (linkType == 0)
                ? _targetResource
                : _targetResource.GetLinkProp( linkType );
            return param;
        }

        private void _chkPlaySound_CheckedChanged( object sender, EventArgs e )
        {
            _edtSoundName.Enabled = _chkPlaySound.Checked;
            _btnBrowse.Enabled = _chkPlaySound.Checked;
            UpdateButtonState();
        }

        private void _chkShowMessage_CheckedChanged(object sender, EventArgs e)
        {
            _edtMessage.Enabled = _chkShowMessage.Checked;
            if ( _chkShowMessage.Checked && _edtMessage.Text == "" )
            {
                CreateDefaultNotificationMessage();
            }
            UpdateButtonState();
        }

        private void CreateDefaultNotificationMessage()
        {
            StringBuilder msgBuilder = new StringBuilder( "New item received: " );
            if ( _notifyMeConditionLabel != null )
            {
                msgBuilder.Append( _notifyMeConditionLabel.Text );
            }
            else
            {
                bool firstItem = true;
                for ( int i=0; i<_notifyMeConditionCheckboxes.Length; i++ )
                {
                    if ( _notifyMeConditionCheckboxes [i].Checked )
                    {
                        if ( !firstItem )
                        {
                            msgBuilder.Append( ", " );
                        }
                        firstItem = false;
                        msgBuilder.Append( _notifyMeConditionCheckboxes [i].Text );
                    }
                }
            }
            _edtMessage.Text = msgBuilder.ToString();
        }

        private void _btnBrowse_Click( object sender, EventArgs e )
        {
            _openFileDialog.FileName = _edtSoundName.Text;
            if ( _openFileDialog.ShowDialog() == DialogResult.OK )
            {
                _edtSoundName.Text = _openFileDialog.FileName;
            }
        }

        private void _edtSoundName_TextChanged(object sender, EventArgs e)
        {
            UpdateButtonState();
        }

        private void _edtMessage_TextChanged(object sender, EventArgs e)
        {
            UpdateButtonState();
        }
    }

    public class NotifyMeAction: IAction
    {
        public void Execute( IActionContext context )
        {
            using( NotifyMeDlg dlg = new NotifyMeDlg() )
            {
                dlg.ShowNotifyMeDialog( context.SelectedResources [0] );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count != 1 )
            {
                presentation.Visible = false;
            }
            else
            {
                string resType = context.SelectedResources [0].Type;
                if ( ICore.Instance.NotificationManager.GetNotifyMeConditions( resType ).Length == 0 )
                {
                    presentation.Visible = false;
                }
            }

            if ( context.Kind == ActionContextKind.MainMenu && !presentation.Visible )
            {
                presentation.Visible = true;
                presentation.Enabled = false;
            }
        }
    }

    public class NotifyMeOnContactNameAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IResource res = context.SelectedResources[ 0 ].GetLinkProp( Core.ContactManager.Props.LinkBaseContact );
            using( NotifyMeDlg dlg = new NotifyMeDlg() )
            {
                dlg.ShowNotifyMeDialog( res );
            }
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.SelectedResources.Count == 1) &&
                                   (context.SelectedResources[ 0 ].Type == "ContactName") &&
                                   (Core.NotificationManager.GetNotifyMeConditions( "Contact" ).Length > 0 );
        }
    }
}
