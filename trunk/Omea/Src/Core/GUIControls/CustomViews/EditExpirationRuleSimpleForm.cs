/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.CustomViews;
using JetBrains.Omea.OpenAPI;

namespace GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for EditExpirationRuleSimpleForm.
	/// </summary>
	public class EditExpirationRuleSimpleForm : DialogBase
	{
        private System.Windows.Forms.NumericUpDown numericCountResources;
        private System.Windows.Forms.NumericUpDown numericOlderValue;
        private System.Windows.Forms.ComboBox comboTimeUnits;
        private System.Windows.Forms.Label labelIn;
        private System.Windows.Forms.Label labelExcept;
        private System.Windows.Forms.CheckBox checkFlaggedResources;
        private System.Windows.Forms.CheckBox checkCategorizedResources;
        private System.Windows.Forms.CheckBox checkUnreadResources;
        private System.Windows.Forms.CheckBox checkDeleteResources;
        private System.Windows.Forms.CheckBox checkMarkRead;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonAdvanced;
        private System.Windows.Forms.CheckBox checkDeleteRelatedContacts;
        private System.Windows.Forms.GroupBox groupConditions;
        private System.Windows.Forms.GroupBox groupActions;
        private System.Windows.Forms.GroupBox groupWhen;
        private System.Windows.Forms.RadioButton radioCount;
        private System.Windows.Forms.RadioButton radioOlder;
        private System.Windows.Forms.Label labelRes;

        private IResourceList   _relatedFolders;
        private IResource       _baseResType;
        private bool            _isForDeletedItems;
        private IResource       _resultRule = null;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        #region Ctor and Initialization
		public EditExpirationRuleSimpleForm( IResource resType, IResource rule, bool forDeletedItems )
		{
			InitializeComponent();

		    _relatedFolders = null;
		    _baseResType = resType;
		    _isForDeletedItems = forDeletedItems;

            InitializeControls( rule );
            AdjustForm();
            CheckFormControls();
            if( !forDeletedItems )
                labelIn.Text = "For all " + resType.DisplayName + " resources";
            else
                labelIn.Text = "For all deleted " + resType.DisplayName + " resources";
		}

		public EditExpirationRuleSimpleForm( IResourceList folders, IResource rule )
		{
			InitializeComponent();

		    _relatedFolders = folders;

            InitializeControls( rule );
            AdjustForm();
            CheckFormControls();

            labelIn.Text = "For " + folders.Count.ToString() + " selected resources";
            if( folders.Count > 1 )
                labelIn.Text = labelIn.Text + "s";
		}

		public EditExpirationRuleSimpleForm( IResource rule )
		{
			InitializeComponent();

            IResourceList linked = rule.GetLinksOfType( null, "ExpirationRuleOnDeletedLink" );
            if( linked.Count == 1 )
            {
                _relatedFolders = null;
                _baseResType = linked[ 0 ];
                _isForDeletedItems = true;
            }
            else
            {
                linked = rule.GetLinksOfType( null, "ExpirationRuleLink" );
                if( linked.Count == 1 && linked[ 0 ].Type == "ResourceType" )
                {
                    _relatedFolders = null;
                    _baseResType = linked[ 0 ];
                }
                else
                {
                    _baseResType = null;
                    _relatedFolders = linked;
                    labelIn.Text = "For " + linked.Count.ToString() + " selected resources";
                    if( linked.Count > 1 )
                        labelIn.Text = labelIn.Text + "s";
                }
            }

            InitializeControls( rule );
            AdjustForm();
            CheckFormControls();
		}

        private void  InitializeControls( IResource rule )
        {
            comboTimeUnits.Text = "Days";
            _resultRule = rule;

            if( rule != null )
            {
                IResourceList exceptions = Core.FilterManager.GetExceptions( rule );
                if( exceptions.IndexOf( Core.FilterManager.Std.ResourceIsFlagged ) != -1 )
                {
                    checkFlaggedResources.Checked = true;
                    exceptions = exceptions.Minus( Core.FilterManager.Std.ResourceIsFlagged.ToResourceList() );
                    exceptions = exceptions.Minus( Core.FilterManager.Std.ResourceIsAnnotated.ToResourceList() );
                }
                if( exceptions.IndexOf( Core.FilterManager.Std.ResourceIsCategorized ) != -1 )
                {
                    checkCategorizedResources.Checked = true;
                    exceptions = exceptions.Minus( Core.FilterManager.Std.ResourceIsCategorized.ToResourceList() );
                }
                if( exceptions.IndexOf( Core.FilterManager.Std.ResourceIsUnread ) != -1 )
                {
                    checkUnreadResources.Checked = true;
                    exceptions = exceptions.Minus( Core.FilterManager.Std.ResourceIsUnread.ToResourceList() );
                }

                if( exceptions.Count > 0 )
                {
                    InitializeTimeUnitsCombo( exceptions[ 0 ] );
                    radioOlder.Checked = true;
                    radioCount.Checked = numericCountResources.Enabled = false;
                }
                else
                {
                    int count = rule.GetIntProp( "CountRestriction" );
                    numericCountResources.Value = count;
                    radioCount.Checked = true;
                    radioOlder.Checked = numericOlderValue.Enabled = false;
                    comboTimeUnits.Enabled = false;
                }

                //-----------------------------------------------------------------
                IResourceList actions = Core.FilterManager.GetActions( rule );
                if( actions.IndexOf( Core.FilterManager.Std.DeleteResourceAction ) != -1 )
                {
                    checkDeleteResources.Checked = true;
                    if( checkDeleteResources.Checked )
                        checkDeleteRelatedContacts.Checked = rule.HasProp( "DeleteRelatedContact" );
                }
                if( actions.IndexOf( Core.FilterManager.Std.MarkResourceAsReadAction ) != -1 )
                    checkMarkRead.Checked = true;
            }
            else
            {
                radioOlder.Checked = true;
                radioCount.Checked = numericCountResources.Enabled = false;
            }
        }

        private void  AdjustForm()
        {
            if( _isForDeletedItems )
            {
                checkDeleteResources.Checked = true;
                checkDeleteResources.Enabled = false;
                checkMarkRead.Visible = checkDeleteRelatedContacts.Visible = false;

                int  delta = checkDeleteRelatedContacts.Height + checkMarkRead.Height;
                groupActions.Height -= delta;
                this.Height -= delta;

                labelExcept.Visible = checkFlaggedResources.Visible = 
                checkCategorizedResources.Visible = checkUnreadResources.Visible = false;

                delta = checkUnreadResources.Bottom - labelExcept.Top;
                groupConditions.Height -= delta;
                groupActions.Top -= delta;
                this.Height -= delta;
            }
        }

        private void  InitializeTimeUnitsCombo( IResource cond )
        {
            string text = EditTimeSpanConditionForm.Condition2Text( cond );
            string[] fields = text.Split( ' ' );
            numericOlderValue.Value = int.Parse( fields[ 1 ] );
            comboTimeUnits.Text = fields[ 2 ];
        }
        #endregion Ctor and Initialization

        public IResource  ResultResource  { get{ return _resultRule; } }

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
            this.numericOlderValue = new System.Windows.Forms.NumericUpDown();
            this.comboTimeUnits = new System.Windows.Forms.ComboBox();
            this.numericCountResources = new System.Windows.Forms.NumericUpDown();
            this.labelIn = new System.Windows.Forms.Label();
            this.labelExcept = new System.Windows.Forms.Label();
            this.checkFlaggedResources = new System.Windows.Forms.CheckBox();
            this.checkCategorizedResources = new System.Windows.Forms.CheckBox();
            this.checkUnreadResources = new System.Windows.Forms.CheckBox();
            this.checkDeleteResources = new System.Windows.Forms.CheckBox();
            this.checkMarkRead = new System.Windows.Forms.CheckBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonAdvanced = new System.Windows.Forms.Button();
            this.checkDeleteRelatedContacts = new System.Windows.Forms.CheckBox();
            this.groupConditions = new System.Windows.Forms.GroupBox();
            this.groupWhen = new System.Windows.Forms.GroupBox();
            this.radioCount = new System.Windows.Forms.RadioButton();
            this.labelRes = new System.Windows.Forms.Label();
            this.radioOlder = new System.Windows.Forms.RadioButton();
            this.groupActions = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericOlderValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericCountResources)).BeginInit();
            this.groupConditions.SuspendLayout();
            this.groupWhen.SuspendLayout();
            this.groupActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // numericOlderValue
            // 
            this.numericOlderValue.Location = new System.Drawing.Point(172, 40);
            this.numericOlderValue.Maximum = new System.Decimal(new int[] {
                                                                              1000,
                                                                              0,
                                                                              0,
                                                                              0});
            this.numericOlderValue.Minimum = new System.Decimal(new int[] {
                                                                              1,
                                                                              0,
                                                                              0,
                                                                              0});
            this.numericOlderValue.Name = "numericOlderValue";
            this.numericOlderValue.Size = new System.Drawing.Size(52, 21);
            this.numericOlderValue.TabIndex = 5;
            this.numericOlderValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericOlderValue.Value = new System.Decimal(new int[] {
                                                                            2,
                                                                            0,
                                                                            0,
                                                                            0});
            this.numericOlderValue.ValueChanged += new System.EventHandler(this.numericOlderValue_ValueChanged);
            // 
            // comboTimeUnits
            // 
            this.comboTimeUnits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboTimeUnits.Items.AddRange(new object[] {
                                                                "Hours",
                                                                "Days",
                                                                "Weeks",
                                                                "Months",
                                                                "Years"});
            this.comboTimeUnits.Location = new System.Drawing.Point(224, 40);
            this.comboTimeUnits.MaxDropDownItems = 6;
            this.comboTimeUnits.Name = "comboTimeUnits";
            this.comboTimeUnits.Size = new System.Drawing.Size(68, 21);
            this.comboTimeUnits.TabIndex = 6;
            // 
            // numericCountResources
            // 
            this.numericCountResources.Location = new System.Drawing.Point(172, 16);
            this.numericCountResources.Maximum = new System.Decimal(new int[] {
                                                                                  100000,
                                                                                  0,
                                                                                  0,
                                                                                  0});
            this.numericCountResources.Minimum = new System.Decimal(new int[] {
                                                                                  1,
                                                                                  0,
                                                                                  0,
                                                                                  0});
            this.numericCountResources.Name = "numericCountResources";
            this.numericCountResources.Size = new System.Drawing.Size(52, 21);
            this.numericCountResources.TabIndex = 2;
            this.numericCountResources.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericCountResources.Value = new System.Decimal(new int[] {
                                                                                1,
                                                                                0,
                                                                                0,
                                                                                0});
            this.numericCountResources.ValueChanged += new System.EventHandler(this.numericCountResources_ValueChanged);
            // 
            // labelIn
            // 
            this.labelIn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelIn.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.labelIn.Location = new System.Drawing.Point(8, 8);
            this.labelIn.Name = "labelIn";
            this.labelIn.Size = new System.Drawing.Size(296, 16);
            this.labelIn.TabIndex = 0;
            this.labelIn.Text = "For";
            // 
            // labelExcept
            // 
            this.labelExcept.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelExcept.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.labelExcept.Location = new System.Drawing.Point(12, 92);
            this.labelExcept.Name = "labelExcept";
            this.labelExcept.Size = new System.Drawing.Size(44, 16);
            this.labelExcept.TabIndex = 0;
            this.labelExcept.Text = "Except:";
            // 
            // checkFlaggedResources
            // 
            this.checkFlaggedResources.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkFlaggedResources.Location = new System.Drawing.Point(16, 112);
            this.checkFlaggedResources.Name = "checkFlaggedResources";
            this.checkFlaggedResources.Size = new System.Drawing.Size(196, 20);
            this.checkFlaggedResources.TabIndex = 7;
            this.checkFlaggedResources.Text = "Flagged and annotated resources";
            // 
            // checkCategorizedResources
            // 
            this.checkCategorizedResources.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkCategorizedResources.Location = new System.Drawing.Point(16, 136);
            this.checkCategorizedResources.Name = "checkCategorizedResources";
            this.checkCategorizedResources.Size = new System.Drawing.Size(196, 20);
            this.checkCategorizedResources.TabIndex = 8;
            this.checkCategorizedResources.Text = "Categorized resources";
            // 
            // checkUnreadResources
            // 
            this.checkUnreadResources.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkUnreadResources.Location = new System.Drawing.Point(16, 160);
            this.checkUnreadResources.Name = "checkUnreadResources";
            this.checkUnreadResources.Size = new System.Drawing.Size(112, 20);
            this.checkUnreadResources.TabIndex = 9;
            this.checkUnreadResources.Text = "Unread resources";
            // 
            // checkDeleteResources
            // 
            this.checkDeleteResources.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkDeleteResources.Location = new System.Drawing.Point(12, 20);
            this.checkDeleteResources.Name = "checkDeleteResources";
            this.checkDeleteResources.Size = new System.Drawing.Size(108, 20);
            this.checkDeleteResources.TabIndex = 10;
            this.checkDeleteResources.Text = "Delete resources";
            this.checkDeleteResources.CheckedChanged += new System.EventHandler(this.checkDeleteResources_CheckedChanged);
            // 
            // checkMarkRead
            // 
            this.checkMarkRead.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkMarkRead.Location = new System.Drawing.Point(12, 64);
            this.checkMarkRead.Name = "checkMarkRead";
            this.checkMarkRead.Size = new System.Drawing.Size(124, 20);
            this.checkMarkRead.TabIndex = 11;
            this.checkMarkRead.Text = "Mark resources Read";
            this.checkMarkRead.CheckedChanged += new System.EventHandler(this.checkMarkRead_CheckedChanged);
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Location = new System.Drawing.Point(94, 332);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.TabIndex = 20;
            this.buttonOK.Text = "OK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCancel.Location = new System.Drawing.Point(182, 332);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.TabIndex = 21;
            this.buttonCancel.Text = "Cancel";
            // 
            // buttonAdvanced
            // 
            this.buttonAdvanced.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAdvanced.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonAdvanced.Location = new System.Drawing.Point(270, 332);
            this.buttonAdvanced.Name = "buttonAdvanced";
            this.buttonAdvanced.TabIndex = 22;
            this.buttonAdvanced.Text = "Advanced...";
            this.buttonAdvanced.Click += new System.EventHandler(this.buttonAdvanced_Click);
            // 
            // checkDeleteRelatedContacts
            // 
            this.checkDeleteRelatedContacts.Enabled = false;
            this.checkDeleteRelatedContacts.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkDeleteRelatedContacts.Location = new System.Drawing.Point(28, 40);
            this.checkDeleteRelatedContacts.Name = "checkDeleteRelatedContacts";
            this.checkDeleteRelatedContacts.Size = new System.Drawing.Size(148, 20);
            this.checkDeleteRelatedContacts.TabIndex = 10;
            this.checkDeleteRelatedContacts.Text = "Delete related contacts";
            // 
            // groupConditions
            // 
            this.groupConditions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupConditions.Controls.Add(this.groupWhen);
            this.groupConditions.Controls.Add(this.labelExcept);
            this.groupConditions.Controls.Add(this.checkFlaggedResources);
            this.groupConditions.Controls.Add(this.checkCategorizedResources);
            this.groupConditions.Controls.Add(this.checkUnreadResources);
            this.groupConditions.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupConditions.Location = new System.Drawing.Point(8, 28);
            this.groupConditions.Name = "groupConditions";
            this.groupConditions.Size = new System.Drawing.Size(344, 188);
            this.groupConditions.TabIndex = 7;
            this.groupConditions.TabStop = false;
            this.groupConditions.Text = "Conditions";
            // 
            // groupWhen
            // 
            this.groupWhen.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupWhen.Controls.Add(this.radioCount);
            this.groupWhen.Controls.Add(this.numericCountResources);
            this.groupWhen.Controls.Add(this.labelRes);
            this.groupWhen.Controls.Add(this.radioOlder);
            this.groupWhen.Controls.Add(this.numericOlderValue);
            this.groupWhen.Controls.Add(this.comboTimeUnits);
            this.groupWhen.Location = new System.Drawing.Point(12, 16);
            this.groupWhen.Name = "groupWhen";
            this.groupWhen.Size = new System.Drawing.Size(320, 68);
            this.groupWhen.TabIndex = 5;
            this.groupWhen.TabStop = false;
            this.groupWhen.Text = "When";
            // 
            // radioCount
            // 
            this.radioCount.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioCount.Location = new System.Drawing.Point(12, 16);
            this.radioCount.Name = "radioCount";
            this.radioCount.Size = new System.Drawing.Size(132, 20);
            this.radioCount.TabIndex = 1;
            this.radioCount.Text = "There are more than";
            this.radioCount.CheckedChanged += new System.EventHandler(this.radioCount_CheckedChanged);
            // 
            // labelRes
            // 
            this.labelRes.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelRes.Location = new System.Drawing.Point(232, 20);
            this.labelRes.Name = "labelRes";
            this.labelRes.Size = new System.Drawing.Size(72, 16);
            this.labelRes.TabIndex = 5;
            this.labelRes.Text = "resources";
            // 
            // radioOlder
            // 
            this.radioOlder.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioOlder.Location = new System.Drawing.Point(12, 40);
            this.radioOlder.Name = "radioOlder";
            this.radioOlder.Size = new System.Drawing.Size(156, 20);
            this.radioOlder.TabIndex = 4;
            this.radioOlder.Text = "Resources are older than";
            this.radioOlder.CheckedChanged += new System.EventHandler(this.radioOlder_CheckedChanged);
            // 
            // groupActions
            // 
            this.groupActions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupActions.Controls.Add(this.checkDeleteResources);
            this.groupActions.Controls.Add(this.checkMarkRead);
            this.groupActions.Controls.Add(this.checkDeleteRelatedContacts);
            this.groupActions.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupActions.Location = new System.Drawing.Point(8, 224);
            this.groupActions.Name = "groupActions";
            this.groupActions.Size = new System.Drawing.Size(344, 92);
            this.groupActions.TabIndex = 10;
            this.groupActions.TabStop = false;
            this.groupActions.Text = "Actions";
            // 
            // EditExpirationRuleSimpleForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(358, 367);
            this.Controls.Add(this.groupActions);
            this.Controls.Add(this.groupConditions);
            this.Controls.Add(this.labelIn);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonAdvanced);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "EditExpirationRuleSimpleForm";
            this.Text = "Edit Expiration Rule";
            ((System.ComponentModel.ISupportInitialize)(this.numericOlderValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericCountResources)).EndInit();
            this.groupConditions.ResumeLayout(false);
            this.groupWhen.ResumeLayout(false);
            this.groupActions.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

	    #region OK/Cancel
        private void buttonOK_Click(object sender, System.EventArgs e)
        {
            buttonOK.Enabled = buttonCancel.Enabled = buttonAdvanced.Enabled = false;

            IResource[] exceptions, actions;
            ConstructLists( out exceptions, out actions );

            int  countCond = -1;
            if( radioCount.Checked )
                countCond = (int) numericCountResources.Value;

            if( _resultRule == null )
            {
                if( _relatedFolders != null )
                    _resultRule = Core.ExpirationRuleManager.RegisterRule( _relatedFolders, countCond, exceptions, actions );
                else
                if( !_isForDeletedItems )
                    _resultRule = Core.ExpirationRuleManager.RegisterRule( _baseResType, countCond, exceptions, actions );
                else
                    _resultRule = Core.ExpirationRuleManager.RegisterRuleForDeletedItems( _baseResType, countCond, exceptions, actions );
            }
            else
            {
                if( _relatedFolders != null )
                    Core.ExpirationRuleManager.ReregisterRule( _resultRule, _relatedFolders, countCond, exceptions, actions );
                else
                if( !_isForDeletedItems )
                    Core.ExpirationRuleManager.ReregisterRule( _resultRule, _baseResType, countCond, exceptions, actions );
                else
                    Core.ExpirationRuleManager.ReregisterRuleForDeletedItems( _resultRule, _baseResType, countCond, exceptions, actions );
            }

            if( checkDeleteRelatedContacts.Checked )
                new ResourceProxy( _resultRule ).SetProp( "DeleteRelatedContact", true );
            else
                new ResourceProxy( _resultRule ).DeleteProp( "DeleteRelatedContact" );

            buttonOK.Enabled = buttonCancel.Enabled = buttonAdvanced.Enabled = true;
        }

        private void  ConstructLists( out IResource[] exceptions, out IResource[] actions )
        {
            ArrayList excVector = new ArrayList(), actVector = new ArrayList();
            if( radioOlder.Checked )
            {
                string  paramStr = "Last " + numericOlderValue.Value + " " +
                                   (string)comboTimeUnits.Items[ comboTimeUnits.SelectedIndex ];
                IResource dateTemplate = Core.FilterManager.Std.ReceivedInTheTimeSpanX;
                IResource dateExc = FilterConvertors.InstantiateTemplate( dateTemplate, paramStr, null );
                excVector.Add( dateExc );
            }

            //-----------------------------------------------------------------
            if( checkFlaggedResources.Checked )
            {
                excVector.Add( Core.FilterManager.Std.ResourceIsFlagged );
                excVector.Add( Core.FilterManager.Std.ResourceIsAnnotated );
            }
            if( checkCategorizedResources.Checked )
            {
                excVector.Add( Core.FilterManager.Std.ResourceIsCategorized );
            }
            if( checkUnreadResources.Checked )
                excVector.Add( Core.FilterManager.Std.ResourceIsUnread );

            //-----------------------------------------------------------------
            if( checkDeleteResources.Checked )
                actVector.Add( Core.FilterManager.Std.DeleteResourceAction );
            if( checkMarkRead.Checked )
                actVector.Add( Core.FilterManager.Std.MarkResourceAsReadAction );

            exceptions = (IResource[]) excVector.ToArray( typeof(IResource) );
            actions = (IResource[]) actVector.ToArray( typeof(IResource) );
        }
        #endregion OK/Cancel

        #region Event Handlers
        private void radioCount_CheckedChanged(object sender, System.EventArgs e)
        {
            numericCountResources.Enabled = radioCount.Checked;
            buttonAdvanced.Enabled = !radioCount.Checked;
            CheckFormControls();
        }

        private void radioOlder_CheckedChanged(object sender, System.EventArgs e)
        {
            numericOlderValue.Enabled = comboTimeUnits.Enabled = radioOlder.Checked;
            buttonAdvanced.Enabled = radioOlder.Checked;
            CheckFormControls();
        }

        private void numericOlderValue_ValueChanged(object sender, System.EventArgs e)
        {
            CheckFormControls();
        }

        private void numericCountResources_ValueChanged(object sender, System.EventArgs e)
        {
            CheckFormControls();
        }

        private void buttonAdvanced_Click(object sender, System.EventArgs e)
        {
            EditExpirationRuleForm form;
            if( _relatedFolders != null )
                form = new EditExpirationRuleForm( _relatedFolders, _resultRule );
            else
            if( _resultRule != null )
                form = new EditExpirationRuleForm( _baseResType, _resultRule, _isForDeletedItems );
            else
            {
                IResource[] excpt, actions;
                ConstructLists( out excpt, out actions );
                form = new EditExpirationRuleForm( _baseResType, null, excpt, actions, false, _isForDeletedItems );
            }

            DialogResult dr = form.ShowDialog( this );
            IResource advResult = form.ResultResource;
            form.Dispose();

            if( dr == DialogResult.OK )
            {
                if( _resultRule == null )
                    _resultRule = advResult;

                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void checkDeleteResources_CheckedChanged(object sender, System.EventArgs e)
        {
            checkDeleteRelatedContacts.Enabled = checkDeleteResources.Checked;
            CheckFormControls();
        }

        private void checkMarkRead_CheckedChanged(object sender, System.EventArgs e)
        {
            CheckFormControls();
        }
        #endregion Event Handlers

        #region Misc
        private void  CheckFormControls()
        {
            buttonOK.Enabled = true;
            if( numericOlderValue.Enabled )
                buttonOK.Enabled = numericOlderValue.Value > 0;
            if( numericCountResources.Enabled )
                buttonOK.Enabled = buttonOK.Enabled && numericCountResources.Value > 0;

            buttonOK.Enabled = buttonOK.Enabled && (checkDeleteResources.Checked || checkMarkRead.Checked);
        }
        #endregion Misc
	}
}
