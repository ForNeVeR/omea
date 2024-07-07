// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.Tasks
{
	internal class TaskEditPane : AbstractEditPane
	{
        #region Attributes
        private TabControl tabsTaskPages;
        private TabPage    pageGeneral;
        private TabPage    pageLinked;
        private TabPage    pageRecurrence;
        private CustomStylePanel _subjectDescriptionPanel;
        private JetTextBox _descriptionBox;
        private Label label2;
        private TextBox _subjectBox;
        private Label labelSubject;
        private CustomStylePanel _propertiesPanel;
        private Label labelStatus;
        private ComboBox _statusBox;
        private Label labelPriority;
        private ComboBox _priorityBox;
        private DateTimePickerCtrl _startDateTime;
        private Label labelStartDate;
        private Label labelDueDate;
        private DateTimePickerCtrl _dueDateTime;
        private Label labelDateComplete;
        private DateTimePickerCtrl _completeDateTime;
        private Label label9;
        private ResourceComboBox _workspacesBox;
        private DateTimePickerCtrl _reminderDateTime;
        private CheckBox _workspaceReminder;
		private System.ComponentModel.Container components = null;
        private ContextMenu _targetsContextMenu;
        private MenuItem _removeFromTaskMenuItem;
        private Button _btnCategories;
        private TextBox BoxCategories;

        private CustomStylePanel _attachedResourcesPanel;
        private Label            labelLinkedRes;
        private ResourceListView2 _attachedView;
        private Button              btnClearAttached;

        private GroupBox    boxPattern, boxDelimiter;
        private RadioButton radioDaily, radioWeekly, radioMonthly, radioYearly;
        private Panel       panelDaily, panelWeekly, panelMonthly, panelYearly;

        private RadioButton radioEvery, radioEveryWeekday;
        private TextBox     textEveryXDay;
        private Label       labelDays;

        private RadioButton radioWeekEvery;
        private TextBox     textEveryXWeek;
        private Label       labelWeeks;
        private CheckBox    checkMonday, checkTuesday, checkWednesday,
                            checkThursday, checkFriday, checkSaturday, checkSunday;

        private RadioButton radioDayNumber, radioThePrefix;
        private TextBox     textDayOfMonth, textMonthNumber, textMonthNumber2;
        private Label       labelEvery, labelMonths, labelEvery2, labelMonths2;
        private ComboBox    cmbWeekDayNumber, cmbWeekDay;

        private RadioButton radioEvery2, radioThePrefix2;
        private ComboBox    cmbMonth, cmbMonth2, cmbWeekDayNumber2, cmbWeekDay2;
        private TextBox     textDayNumber;
        private Label       labelOf;

        private RadioButton radioRegenerateDay, radioRegenerateWeek, radioRegenerateMonth, radioRegenerateYear;
        private TextBox     textRegDaysAfter, textRegWeeksAfter, textRegMonthsAfter, textRegYearsAfter;
        private Label       labelRegDaysAfter, labelRegWeeksAfter, labelRegMonthsAfter, labelRegYearsAfter;

        private GroupBox    boxRecRange;
        private Label       labelStart;
        private DateTimePicker dateRecStart;
        private RadioButton radioNoEndDate, radioEndAfter, radioEndBy;
        private JetTextBox  textNumberOfOcc;
        private Label       labelOccurences;
        private DateTimePicker dateEndByDate;
        private Button      buttonClear;

        private IResource _task;
        private bool      isSuperTask;
        private int[] _oldTargets;
        private DateTime _remindDateTimeCopy;
        private DateTime _completeDateTimeCopy;
        #endregion Attributes

        #region Ctor and initialization
		public TaskEditPane()
		{
			InitializeComponent();
            InitializeColumns();

            _statusBox.Items.AddRange( TasksPlugin._statuses );
            _priorityBox.Items.AddRange( TasksPlugin._priorities );
            foreach( IResource workspace in Core.WorkspaceManager.GetAllWorkspaces() )
            {
                _workspacesBox.Items.Add( workspace );
            }
            _workspaceReminder.Enabled = _workspacesBox.Enabled = _workspacesBox.Items.Count > 0;
            _completeDateTimeCopy = _remindDateTimeCopy = DateTime.MinValue;

            radioWeekly.Checked = true;
		}

        private void  InitializeColumns()
        {
            _attachedView.AllowColumnReorder = false;
            _attachedView.Columns.Add( new ResourceIconColumn() );
            ResourceListView2Column nameCol = _attachedView.AddColumn( ResourceProps.DisplayName );
            nameCol.AutoSize = true;
        }

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
        #endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            #region Controls creation
            this.tabsTaskPages = new TabControl();
            this.pageGeneral = new TabPage();
            this.pageLinked  = new TabPage();
            this.pageRecurrence  = new TabPage();
            this._subjectDescriptionPanel = new JetBrains.Omea.GUIControls.CustomStylePanel();
            this._descriptionBox = new JetTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this._subjectBox = new System.Windows.Forms.TextBox();
            this.labelSubject = new System.Windows.Forms.Label();
            this._propertiesPanel = new JetBrains.Omea.GUIControls.CustomStylePanel();
            this._workspaceReminder = new System.Windows.Forms.CheckBox();
            this._workspacesBox = new JetBrains.Omea.GUIControls.ResourceComboBox();
            this._reminderDateTime = new JetBrains.Omea.GUIControls.DateTimePickerCtrl();
            this.label9 = new System.Windows.Forms.Label();
            this._completeDateTime = new JetBrains.Omea.GUIControls.DateTimePickerCtrl();
            this.labelDateComplete = new System.Windows.Forms.Label();
            this._dueDateTime = new JetBrains.Omea.GUIControls.DateTimePickerCtrl();
            this.labelDueDate = new System.Windows.Forms.Label();
            this.labelStartDate = new System.Windows.Forms.Label();
            this._startDateTime = new JetBrains.Omea.GUIControls.DateTimePickerCtrl();
            this._priorityBox = new System.Windows.Forms.ComboBox();
            this.labelPriority = new System.Windows.Forms.Label();
            this._statusBox = new System.Windows.Forms.ComboBox();
            this.labelStatus = new System.Windows.Forms.Label();
            this._targetsContextMenu = new System.Windows.Forms.ContextMenu();
            this._removeFromTaskMenuItem = new System.Windows.Forms.MenuItem();
            this._btnCategories = new System.Windows.Forms.Button();
            this.BoxCategories = new System.Windows.Forms.TextBox();

            //  Attached Resources Tab
            this._attachedResourcesPanel = new JetBrains.Omea.GUIControls.CustomStylePanel();
            this.labelLinkedRes = new System.Windows.Forms.Label();
            this._attachedView = new ResourceListView2();
            btnClearAttached = new Button();

            //  Recurrence Tab Controls.
            boxPattern = new GroupBox();
            radioDaily = new RadioButton();
            radioWeekly = new RadioButton();
            radioMonthly = new RadioButton();
            radioYearly = new RadioButton();
            boxDelimiter = new GroupBox();

            panelDaily = new Panel();
            panelWeekly = new Panel();
            panelMonthly = new Panel();
            panelYearly  = new Panel();

            radioEvery = new RadioButton();
            radioEveryWeekday = new RadioButton();
            textEveryXDay = new TextBox();
            labelDays = new Label();

            radioWeekEvery = new RadioButton();
            textEveryXWeek = new TextBox();
            labelWeeks = new Label();
            checkMonday = new CheckBox();
            checkTuesday = new CheckBox();
            checkWednesday = new CheckBox();
            checkThursday = new CheckBox();
            checkFriday = new CheckBox();
            checkSaturday = new CheckBox();
            checkSunday = new CheckBox();

            radioDayNumber = new RadioButton();
            radioThePrefix = new RadioButton();
            textDayOfMonth = new TextBox();
            textMonthNumber = new TextBox();
            labelEvery = new Label();
            labelMonths = new Label();
            cmbWeekDayNumber = new ComboBox();
            cmbWeekDay = new ComboBox();
            textMonthNumber2 = new TextBox();
            labelEvery2 = new Label();
            labelMonths2 = new Label();

            radioEvery2 = new RadioButton();
            radioThePrefix2 = new RadioButton();
            cmbMonth = new ComboBox();
            textDayNumber = new TextBox();
            cmbWeekDayNumber2 = new ComboBox();
            cmbWeekDay2 = new ComboBox();
            cmbMonth2 = new ComboBox();
            labelOf = new Label();

            radioRegenerateDay = new RadioButton();
            radioRegenerateWeek = new RadioButton();
            radioRegenerateMonth = new RadioButton();
            radioRegenerateYear = new RadioButton();

            textRegDaysAfter = new TextBox();
            textRegWeeksAfter = new TextBox();
            textRegMonthsAfter = new TextBox();
            textRegYearsAfter = new TextBox();

            labelRegDaysAfter = new Label();
            labelRegWeeksAfter = new Label();
            labelRegMonthsAfter = new Label();
            labelRegYearsAfter = new Label();

            boxRecRange = new GroupBox();
            labelStart = new Label();
            dateRecStart = new DateTimePicker();
            radioNoEndDate = new RadioButton();
            radioEndAfter = new RadioButton();
            radioEndBy = new RadioButton();
            textNumberOfOcc = new JetTextBox();
            labelOccurences = new Label();
            dateEndByDate = new DateTimePicker();
            buttonClear = new Button();

            this._subjectDescriptionPanel.SuspendLayout();
            this._propertiesPanel.SuspendLayout();
            this._attachedResourcesPanel.SuspendLayout();
            panelDaily.SuspendLayout();
            panelWeekly.SuspendLayout();
            panelMonthly.SuspendLayout();
            panelYearly.SuspendLayout();
            this.SuspendLayout();
            #endregion Controls creation

            #region Tab Controls
            //
            // tabViews
            //
            this.tabsTaskPages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.tabsTaskPages.Location = new System.Drawing.Point(4, 4);
            this.tabsTaskPages.Name = "tabsTaskPages";
            this.tabsTaskPages.SelectedIndex = 0;
            this.tabsTaskPages.Size = new System.Drawing.Size(370, 320);
            this.tabsTaskPages.TabIndex = 11;
            //
            // pageGeneral
            //
            pageGeneral.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            pageGeneral.Location = new System.Drawing.Point(4, 22);
            pageGeneral.Name = "pageGeneral";
            pageGeneral.Size = new System.Drawing.Size(362, 290);
            pageGeneral.TabIndex = 1;
            pageGeneral.Text = "General";
            //
            // pageLinked
            //
            pageLinked.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            pageLinked.Location = new System.Drawing.Point(4, 22);
            pageLinked.Name = "pageLinked";
            pageLinked.Size = new System.Drawing.Size(362, 290);
            pageLinked.TabIndex = 2;
            pageLinked.Text = "Linked Resources";
            //
            // pageRecurrence
            //
            pageRecurrence.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            pageRecurrence.Location = new System.Drawing.Point(4, 22);
            pageRecurrence.Name = "pageRecurrence";
            pageRecurrence.Size = new System.Drawing.Size(362, 290);
            pageRecurrence.TabIndex = 3;
            pageRecurrence.Text = "Recurrence";
            #endregion Tab Controls

            #region Description Panel
            //
            // _subjectDescriptionPanel
            //
            this._subjectDescriptionPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom |
                System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this._subjectDescriptionPanel.BorderColor = System.Drawing.Color.Black;
            this._subjectDescriptionPanel.Controls.Add(this._descriptionBox);
            this._subjectDescriptionPanel.Controls.Add(this.label2);
            this._subjectDescriptionPanel.Controls.Add(this._subjectBox);
            this._subjectDescriptionPanel.Controls.Add(this.labelSubject);
            this._subjectDescriptionPanel.Location = new System.Drawing.Point(4, 4);
            this._subjectDescriptionPanel.Name = "_subjectDescriptionPanel";
            this._subjectDescriptionPanel.Size = new System.Drawing.Size(350, 110);
            this._subjectDescriptionPanel.TabIndex = 0;
            this._subjectDescriptionPanel.Paint += new System.Windows.Forms.PaintEventHandler(this._subjectDescriptionPanel_Paint);
            //
            // labelSubject
            //
            this.labelSubject.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelSubject.Location = new System.Drawing.Point(8, 8);
            this.labelSubject.Name = "labelSubject";
            this.labelSubject.Size = new System.Drawing.Size(64, 20);
            this.labelSubject.TabIndex = 4;
            this.labelSubject.Text = "Subject:";
            this.labelSubject.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _subjectBox
            //
            this._subjectBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._subjectBox.Location = new System.Drawing.Point(72, 8);
            this._subjectBox.Name = "_subjectBox";
            this._subjectBox.Size = new System.Drawing.Size(260, 20);
            this._subjectBox.TabIndex = 0;
            this._subjectBox.Text = "";
            this._subjectBox.TextChanged += new System.EventHandler(this._subjectBox_TextChanged);
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 20);
            this.label2.TabIndex = 6;
            this.label2.Text = "Description:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _descriptionBox
            //
            this._descriptionBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._descriptionBox.Location = new System.Drawing.Point(72, 32);
            this._descriptionBox.Multiline = true;
            this._descriptionBox.AcceptsReturn = true;
            this._descriptionBox.Name = "_descriptionBox";
            this._descriptionBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this._descriptionBox.Size = new System.Drawing.Size(260, 60);
            this._descriptionBox.TabIndex = 1;
            this._descriptionBox.Text = "";
            #endregion Description Panel

            #region Properties Panel
            //
            // _propertiesPanel
            //
            this._propertiesPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._propertiesPanel.BorderColor = System.Drawing.Color.Black;
            this._propertiesPanel.Controls.Add(this._workspaceReminder);
            this._propertiesPanel.Controls.Add(this._workspacesBox);
            this._propertiesPanel.Controls.Add(this._reminderDateTime);
            this._propertiesPanel.Controls.Add(this.label9);
            this._propertiesPanel.Controls.Add(this._completeDateTime);
            this._propertiesPanel.Controls.Add(this.labelDateComplete);
            this._propertiesPanel.Controls.Add(this._dueDateTime);
            this._propertiesPanel.Controls.Add(this.labelDueDate);
            this._propertiesPanel.Controls.Add(this.labelStartDate);
            this._propertiesPanel.Controls.Add(this._startDateTime);
            this._propertiesPanel.Controls.Add(this._priorityBox);
            this._propertiesPanel.Controls.Add(this.labelPriority);
            this._propertiesPanel.Controls.Add(this._statusBox);
            this._propertiesPanel.Controls.Add(this.labelStatus);
            this._propertiesPanel.Location = new System.Drawing.Point(4, 112);
            this._propertiesPanel.Name = "_propertiesPanel";
            this._propertiesPanel.Size = new System.Drawing.Size(350, 130);
            this._propertiesPanel.TabIndex = 1;
            this._propertiesPanel.Paint += new System.Windows.Forms.PaintEventHandler(this._propertiesPanel_Paint);
            //
            // labelStartDate
            //
            this.labelStartDate.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelStartDate.Location = new System.Drawing.Point(8, 10);
            this.labelStartDate.Name = "labelStartDate";
            this.labelStartDate.Size = new System.Drawing.Size(104, 20);
            this.labelStartDate.TabIndex = 1;
            this.labelStartDate.Text = "S&tart date and time:";
            this.labelStartDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _startDateTime
            //
            this._startDateTime.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this._startDateTime.CurrentDateTime = new System.DateTime(((long)(0)));
            this._startDateTime.Location = new System.Drawing.Point(112, 8);
            this._startDateTime.Name = "_startDateTime";
            this._startDateTime.ShowClearButton = true;
            this._startDateTime.Size = new System.Drawing.Size(200, 28);
            this._startDateTime.TabIndex = 2;
            this._startDateTime.ValidStateChanged += new ValidStateEventHandler( TimeFormatStateChanged );
            //
            // labelDueDate
            //
            this.labelDueDate.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelDueDate.Location = new System.Drawing.Point(8, 34);
            this.labelDueDate.Name = "labelDueDate";
            this.labelDueDate.Size = new System.Drawing.Size(104, 20);
            this.labelDueDate.TabIndex = 3;
            this.labelDueDate.Text = "&Due date and time:";
            this.labelDueDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _dueDateTime
            //
            this._dueDateTime.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this._dueDateTime.CurrentDateTime = new System.DateTime(((long)(0)));
            this._dueDateTime.Location = new System.Drawing.Point(112, 32);
            this._dueDateTime.Name = "_dueDateTime";
            this._dueDateTime.ShowClearButton = true;
            this._dueDateTime.Size = new System.Drawing.Size(200, 28);
            this._dueDateTime.TabIndex = 4;
            this._dueDateTime.ValidStateChanged += new ValidStateEventHandler( TimeFormatStateChanged );
            //
            // label9
            //
            this.label9.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label9.Location = new System.Drawing.Point(8, 58);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(104, 20);
            this.label9.TabIndex = 5;
            this.label9.Text = "&Reminder at:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _reminderDateTime
            //
            this._reminderDateTime.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this._reminderDateTime.CurrentDateTime = new System.DateTime(((long)(0)));
            this._reminderDateTime.Location = new System.Drawing.Point(112, 56);
            this._reminderDateTime.Name = "_reminderDateTime";
            this._reminderDateTime.ShowClearButton = true;
            this._reminderDateTime.Size = new System.Drawing.Size(200, 28);
            this._reminderDateTime.TabIndex = 6;
            this._reminderDateTime.ValidStateChanged += new ValidStateEventHandler( TimeFormatStateChanged );

            //
            // labelPriority
            //
            this.labelPriority.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelPriority.Location = new System.Drawing.Point(340, 10);
            this.labelPriority.Name = "labelPriority";
            this.labelPriority.Size = new System.Drawing.Size(80, 20);
            this.labelPriority.TabIndex = 7;
            this.labelPriority.Text = "&Priority:";
            this.labelPriority.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _priorityBox
            //
            this._priorityBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._priorityBox.Location = new System.Drawing.Point(420, 8);
            this._priorityBox.Name = "_priorityBox";
            this._priorityBox.Size = new System.Drawing.Size(80, 21);
            this._priorityBox.TabIndex = 8;
            //
            // labelStatus
            //
            this.labelStatus.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelStatus.Location = new System.Drawing.Point(340, 34);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(80, 20);
            this.labelStatus.TabIndex = 9;
            this.labelStatus.Text = "St&atus:";
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _statusBox
            //
            this._statusBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._statusBox.Location = new System.Drawing.Point(420, 32);
            this._statusBox.Name = "_statusBox";
            this._statusBox.Size = new System.Drawing.Size(80, 21);
            this._statusBox.TabIndex = 10;
            this._statusBox.SelectedIndexChanged += new System.EventHandler(this._statusBox_SelectedIndexChanged);
            //
            // labelDateComplete
            //
            this.labelDateComplete.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelDateComplete.Location = new System.Drawing.Point(340, 58);
            this.labelDateComplete.Name = "labelDateComplete";
            this.labelDateComplete.Size = new System.Drawing.Size(104, 20);
            this.labelDateComplete.TabIndex = 11;
            this.labelDateComplete.Text = "Date &complete:";
            this.labelDateComplete.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _completeDateTime
            //
            this._completeDateTime.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this._completeDateTime.CurrentDateTime = new System.DateTime(((long)(0)));
            this._completeDateTime.Location = new System.Drawing.Point(420, 56);
            this._completeDateTime.Name = "_completeDateTime";
            this._completeDateTime.ShowClearButton = true;
            this._completeDateTime.Size = new System.Drawing.Size(180, 28);
            this._completeDateTime.TabIndex = 12;
            this._completeDateTime.AutoSetTime = true;
            this._completeDateTime.ValidStateChanged += new ValidStateEventHandler( TimeFormatStateChanged );

            //
            // _workspaceReminder
            //
            this._workspaceReminder.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._workspaceReminder.Location = new System.Drawing.Point(8, 92);
            this._workspaceReminder.Name = "_workspaceReminder";
            this._workspaceReminder.Size = new System.Drawing.Size(208, 24);
            this._workspaceReminder.TabIndex = 13;
            this._workspaceReminder.Text = "Remind on activation of &workspace:";
            this._workspaceReminder.CheckedChanged += new System.EventHandler(this._workspaceReminder_CheckedChanged);
            //
            // _workspacesBox
            //
            this._workspacesBox.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this._workspacesBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this._workspacesBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._workspacesBox.Location = new System.Drawing.Point(216, 92);
            this._workspacesBox.Name = "_workspacesBox";
            this._workspacesBox.Size = new System.Drawing.Size(136, 21);
            this._workspacesBox.TabIndex = 14;
            #endregion Properties Panel

            #region Linked Tab Content
            //
            // labelLinkedRes
            //
            this.labelLinkedRes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.labelLinkedRes.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelLinkedRes.Location = new System.Drawing.Point(8, 8);
            this.labelLinkedRes.Name = "labelLinkedRes";
            this.labelLinkedRes.Size = new System.Drawing.Size(244, 20);
            this.labelLinkedRes.TabIndex = 0;
            this.labelLinkedRes.Text = "Linked resources:";
            //
            // _attachedResources
            //
            this._attachedView.AllowDrop = true;
            this._attachedView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._attachedView.ContextMenu = this._targetsContextMenu;
            this._attachedView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._attachedView.HideSelection = false;
            this._attachedView.Location = new System.Drawing.Point(8, 28);
            this._attachedView.Name = "_attachedView";
            this._attachedView.ShowContextMenu = false;
            this._attachedView.Size = new System.Drawing.Size(340, 210);
            this._attachedView.TabIndex = 1;
            this._attachedView.EmptyDropHandler = new DnDHandler( this );
            this._attachedView.KeyDown += new System.Windows.Forms.KeyEventHandler(this._attachedResources_KeyDown);
            //
            // _targetsContextMenu
            //
            this._targetsContextMenu.MenuItems.AddRange(new MenuItem[] { this._removeFromTaskMenuItem } );
            //
            // _removeFromTaskMenuItem
            //
            this._removeFromTaskMenuItem.Index = 0;
            this._removeFromTaskMenuItem.Text = "Remove from Task";
            this._removeFromTaskMenuItem.Click += new System.EventHandler(this._removeFromTaskMenuItem_Click);

            this.btnClearAttached.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
            this.btnClearAttached.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnClearAttached.Location = new System.Drawing.Point(256, 250);
            this.btnClearAttached.Name = "btnClearAttached";
            this.btnClearAttached.Size = new System.Drawing.Size(92, 24);
            this.btnClearAttached.TabIndex = 3;
            this.btnClearAttached.Text = "Remove All";
            this.btnClearAttached.Click +=new EventHandler(btnClearAttached_Click);
            #endregion Linked Tab Content

            #region Categories
            //
            // _categoriesButton
            //
            this._btnCategories.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._btnCategories.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCategories.Location = new System.Drawing.Point(4, 250);
            this._btnCategories.Name = "_btnCategories";
            this._btnCategories.Size = new System.Drawing.Size(92, 24);
            this._btnCategories.TabIndex = 3;
            this._btnCategories.Text = "Categories...";
            this._btnCategories.Click += new System.EventHandler(this._categoriesButton_Click);
            //
            // BoxCategories
            //
            this.BoxCategories.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.BoxCategories.Location = new System.Drawing.Point(110, 250);
            this.BoxCategories.Name = "BoxCategories";
            this.BoxCategories.Size = new System.Drawing.Size(160, 20);
            this.BoxCategories.TabIndex = 0;
            this.BoxCategories.Text = "";
            this.BoxCategories.ReadOnly = true;

            #endregion Categories

            #region Recurrence Pattern Tab
            #region Pattern box
            this.boxPattern.Controls.Add(radioDaily);
            this.boxPattern.Controls.Add(radioWeekly);
            this.boxPattern.Controls.Add(radioMonthly);
            this.boxPattern.Controls.Add(radioYearly);
            this.boxPattern.Controls.Add(boxDelimiter);
            this.boxPattern.Controls.Add(panelDaily);
            this.boxPattern.Controls.Add(panelWeekly);
            this.boxPattern.Controls.Add(panelMonthly);
            this.boxPattern.Controls.Add(panelYearly);

            this.boxPattern.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
            this.boxPattern.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.boxPattern.Location = new System.Drawing.Point(8, 8);
            this.boxPattern.Name = "boxPattern";
            this.boxPattern.Size = new System.Drawing.Size(346, 130);
            this.boxPattern.TabIndex = 1;
            this.boxPattern.TabStop = false;
            this.boxPattern.Text = "Recurrence pattern";

            this.radioDaily.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioDaily.Location = new System.Drawing.Point(8, 18);
            this.radioDaily.Name = "radioDaily";
            this.radioDaily.Size = new System.Drawing.Size(70, 22);
            this.radioDaily.TabIndex = 1;
            this.radioDaily.Text = "&Daily";
            this.radioDaily.CheckedChanged += new EventHandler(PatternCheckedChanged);
            this.radioDaily.Tag = panelDaily;

            this.radioWeekly.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioWeekly.Location = new System.Drawing.Point(8, 42);
            this.radioWeekly.Name = "radioWeekly";
            this.radioWeekly.Size = new System.Drawing.Size(70, 22);
            this.radioWeekly.TabIndex = 2;
            this.radioWeekly.Text = "&Weekly";
            this.radioWeekly.CheckedChanged += new EventHandler(PatternCheckedChanged);
            this.radioWeekly.Tag = panelWeekly;

            this.radioMonthly.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioMonthly.Location = new System.Drawing.Point(8, 66);
            this.radioMonthly.Name = "radioMonthly";
            this.radioMonthly.Size = new System.Drawing.Size(70, 22);
            this.radioMonthly.TabIndex = 3;
            this.radioMonthly.Text = "&Monthly";
            this.radioMonthly.CheckedChanged += new EventHandler(PatternCheckedChanged);
            this.radioMonthly.Tag = panelMonthly;

            this.radioYearly.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioYearly.Location = new System.Drawing.Point(8, 90);
            this.radioYearly.Name = "radioMonthly";
            this.radioYearly.Size = new System.Drawing.Size(70, 22);
            this.radioYearly.TabIndex = 4;
            this.radioYearly.Text = "&Yearly";
            this.radioYearly.CheckedChanged += new EventHandler(PatternCheckedChanged);
            this.radioYearly.Tag = panelYearly;

            this.boxDelimiter.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.boxDelimiter.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.boxDelimiter.Location = new System.Drawing.Point(85, 10);
            this.boxDelimiter.Name = "boxDelimiter";
            this.boxDelimiter.Size = new System.Drawing.Size(4, 110);
            this.boxDelimiter.TabStop = false;
            this.boxDelimiter.Text = string.Empty;

            #region Panel Daily
            this.panelDaily.Controls.Add(radioEvery);
            this.panelDaily.Controls.Add(radioEveryWeekday);
            this.panelDaily.Controls.Add(radioRegenerateDay);
            this.panelDaily.Controls.Add(textEveryXDay);
            this.panelDaily.Controls.Add(labelDays);
            this.panelDaily.Controls.Add(textRegDaysAfter);
            this.panelDaily.Controls.Add(labelRegDaysAfter);
            this.panelDaily.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
            this.panelDaily.Location = new System.Drawing.Point(90, 16);
            this.panelDaily.Name = "panelDaily";
            this.panelDaily.Size = new System.Drawing.Size(250, 110);
            this.panelDaily.Visible = false;

            this.radioEvery.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioEvery.Location = new System.Drawing.Point(8, 8);
            this.radioEvery.Name = "radioEvery";
            this.radioEvery.Size = new System.Drawing.Size(50, 22);
            this.radioEvery.TabIndex = 1;
            this.radioEvery.Text = "E&very";

            this.textEveryXDay.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textEveryXDay.Location = new System.Drawing.Point(60, 8);
            this.textEveryXDay.Multiline = false;
            this.textEveryXDay.Name = "textEveryXDay";
            this.textEveryXDay.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textEveryXDay.Size = new System.Drawing.Size(29, 16);
            this.textEveryXDay.TabIndex = 2;
            this.textEveryXDay.Text = "1";

            this.labelDays.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelDays.Location = new System.Drawing.Point(100, 12);
            this.labelDays.Name = "labelDays";
            this.labelDays.Size = new System.Drawing.Size(64, 20);
            this.labelDays.Text = "day(s)";

            this.radioEveryWeekday.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioEveryWeekday.Location = new System.Drawing.Point(8, 38);
            this.radioEveryWeekday.Name = "radioEveryWeekday";
            this.radioEveryWeekday.Size = new System.Drawing.Size(90, 22);
            this.radioEveryWeekday.TabIndex = 3;
            this.radioEveryWeekday.Text = "&Every weekday";

            this.radioRegenerateDay.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioRegenerateDay.Location = new System.Drawing.Point(8, 68);
            this.radioRegenerateDay.Name = "radioRegenerateDay";
            this.radioRegenerateDay.Size = new System.Drawing.Size(130, 22);
            this.radioRegenerateDay.TabIndex = 4;
            this.radioRegenerateDay.Text = "Re&generate new task";

            this.textRegDaysAfter.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textRegDaysAfter.Location = new System.Drawing.Point(140, 68);
            this.textRegDaysAfter.Multiline = false;
            this.textRegDaysAfter.Name = "textRegDaysAfter";
            this.textRegDaysAfter.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textRegDaysAfter.Size = new System.Drawing.Size(29, 16);
            this.textRegDaysAfter.TabIndex = 5;
            this.textRegDaysAfter.Text = "1";

            this.labelRegDaysAfter.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelRegDaysAfter.Location = new System.Drawing.Point(180, 72);
            this.labelRegDaysAfter.Name = "labelRegDaysAfter";
            this.labelRegDaysAfter.Size = new System.Drawing.Size(180, 20);
            this.labelRegDaysAfter.Text = "day(s) after each task is completed";
            #endregion Panel Daily

            #region Panel Weekly
            this.panelWeekly.Controls.Add(radioWeekEvery);
            this.panelWeekly.Controls.Add(textEveryXWeek);
            this.panelWeekly.Controls.Add(labelWeeks);
            this.panelWeekly.Controls.Add(checkMonday);
            this.panelWeekly.Controls.Add(checkTuesday);
            this.panelWeekly.Controls.Add(checkWednesday);
            this.panelWeekly.Controls.Add(checkThursday);
            this.panelWeekly.Controls.Add(checkFriday);
            this.panelWeekly.Controls.Add(checkSaturday);
            this.panelWeekly.Controls.Add(checkSunday);
            this.panelWeekly.Controls.Add(radioRegenerateWeek);
            this.panelWeekly.Controls.Add(textRegWeeksAfter);
            this.panelWeekly.Controls.Add(labelRegWeeksAfter);
            this.panelWeekly.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
            this.panelWeekly.Location = new System.Drawing.Point(90, 16);
            this.panelWeekly.Name = "panelWeekly";
            this.panelWeekly.Size = new System.Drawing.Size(250, 110);
            this.panelWeekly.Visible = false;

            this.radioWeekEvery.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioWeekEvery.Location = new System.Drawing.Point(8, 4);
            this.radioWeekEvery.Name = "radioEvery";
            this.radioWeekEvery.Size = new System.Drawing.Size(90, 22);
            this.radioWeekEvery.TabIndex = 1;
            this.radioWeekEvery.Text = "Re&cur Every";

            this.textEveryXWeek.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textEveryXWeek.Location = new System.Drawing.Point(100, 4);
            this.textEveryXWeek.Multiline = false;
            this.textEveryXWeek.Name = "textEveryXWeek";
            this.textEveryXWeek.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textEveryXWeek.Size = new System.Drawing.Size(29, 16);
            this.textEveryXWeek.TabIndex = 2;
            this.textEveryXWeek.Text = "1";

            this.labelWeeks.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelWeeks.Location = new System.Drawing.Point(140, 10);
            this.labelWeeks.Name = "labelWeeks";
            this.labelWeeks.Size = new System.Drawing.Size(64, 20);
            this.labelWeeks.Text = "week(s)";

            this.checkMonday.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkMonday.Location = new System.Drawing.Point(20, 28);
            this.checkMonday.Name = "checkMonday";
            this.checkMonday.Size = new System.Drawing.Size(70, 24);
            this.checkMonday.TabIndex = 3;
            this.checkMonday.Text = "Monday";

            this.checkTuesday.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkTuesday.Location = new System.Drawing.Point(110, 28);
            this.checkTuesday.Name = "checkTuesday";
            this.checkTuesday.Size = new System.Drawing.Size(70, 24);
            this.checkTuesday.TabIndex = 4;
            this.checkTuesday.Text = "Tuesday";

            this.checkWednesday.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkWednesday.Location = new System.Drawing.Point(200, 28);
            this.checkWednesday.Name = "checkWednesday";
            this.checkWednesday.Size = new System.Drawing.Size(80, 24);
            this.checkWednesday.TabIndex = 5;
            this.checkWednesday.Text = "Wednesday";

            this.checkThursday.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkThursday.Location = new System.Drawing.Point(290, 28);
            this.checkThursday.Name = "checkThursday";
            this.checkThursday.Size = new System.Drawing.Size(70, 24);
            this.checkThursday.TabIndex = 6;
            this.checkThursday.Text = "Thursday";

            this.checkFriday.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkFriday.Location = new System.Drawing.Point(20, 50);
            this.checkFriday.Name = "checkFriday";
            this.checkFriday.Size = new System.Drawing.Size(70, 24);
            this.checkFriday.TabIndex = 7;
            this.checkFriday.Text = "Friday";

            this.checkSaturday.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkSaturday.Location = new System.Drawing.Point(110, 50);
            this.checkSaturday.Name = "checkSaturday";
            this.checkSaturday.Size = new System.Drawing.Size(70, 24);
            this.checkSaturday.TabIndex = 8;
            this.checkSaturday.Text = "Saturday";

            this.checkSunday.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkSunday.Location = new System.Drawing.Point(200, 50);
            this.checkSunday.Name = "checkSunday";
            this.checkSunday.Size = new System.Drawing.Size(70, 24);
            this.checkSunday.TabIndex = 9;
            this.checkSunday.Text = "Sunday";

            this.radioRegenerateWeek.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioRegenerateWeek.Location = new System.Drawing.Point(8, 78);
            this.radioRegenerateWeek.Name = "radioRegenerateWeek";
            this.radioRegenerateWeek.Size = new System.Drawing.Size(130, 22);
            this.radioRegenerateWeek.TabIndex = 10;
            this.radioRegenerateWeek.Text = "Re&generate new task";

            this.textRegWeeksAfter.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textRegWeeksAfter.Location = new System.Drawing.Point(140, 78);
            this.textRegWeeksAfter.Multiline = false;
            this.textRegWeeksAfter.Name = "textRegWeeksAfter";
            this.textRegWeeksAfter.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textRegWeeksAfter.Size = new System.Drawing.Size(29, 16);
            this.textRegWeeksAfter.TabIndex = 11;
            this.textRegWeeksAfter.Text = "1";

            this.labelRegWeeksAfter.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelRegWeeksAfter.Location = new System.Drawing.Point(180, 82);
            this.labelRegWeeksAfter.Name = "labelRegDaysAfter";
            this.labelRegWeeksAfter.Size = new System.Drawing.Size(180, 20);
            this.labelRegWeeksAfter.Text = "week(s) after each task is completed";
            #endregion Panel Weekly

            #region Panel Monthly
            this.panelMonthly.Controls.Add(radioRegenerateMonth);
            this.panelMonthly.Controls.Add(textRegMonthsAfter);
            this.panelMonthly.Controls.Add(labelRegMonthsAfter);
            this.panelMonthly.Controls.Add(radioDayNumber);
            this.panelMonthly.Controls.Add(textDayOfMonth);
            this.panelMonthly.Controls.Add(labelEvery);
            this.panelMonthly.Controls.Add(textMonthNumber);
            this.panelMonthly.Controls.Add(labelMonths);
            this.panelMonthly.Controls.Add(radioThePrefix);
            this.panelMonthly.Controls.Add(cmbWeekDayNumber);
            this.panelMonthly.Controls.Add(cmbWeekDay);
            this.panelMonthly.Controls.Add(labelEvery2);
            this.panelMonthly.Controls.Add(textMonthNumber2);
            this.panelMonthly.Controls.Add(labelMonths2);
            this.panelMonthly.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
            this.panelMonthly.Location = new System.Drawing.Point(90, 16);
            this.panelMonthly.Name = "panelMonthly";
            this.panelMonthly.Size = new System.Drawing.Size(250, 110);
            this.panelMonthly.Visible = false;

            this.radioDayNumber.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioDayNumber.Location = new System.Drawing.Point(8, 8);
            this.radioDayNumber.Name = "radioDayNumber";
            this.radioDayNumber.Size = new System.Drawing.Size(40, 22);
            this.radioDayNumber.TabIndex = 1;
            this.radioDayNumber.Text = "D&ay";

            this.textDayOfMonth.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textDayOfMonth.Location = new System.Drawing.Point(53, 8);
            this.textDayOfMonth.Multiline = false;
            this.textDayOfMonth.Name = "textDayOfMonth";
            this.textDayOfMonth.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textDayOfMonth.Size = new System.Drawing.Size(29, 16);
            this.textDayOfMonth.TabIndex = 2;
            this.textDayOfMonth.Text = "1";

            this.labelEvery.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelEvery.Location = new System.Drawing.Point(90, 12);
            this.labelEvery.Name = "labelEvery";
            this.labelEvery.Size = new System.Drawing.Size(50, 20);
            this.labelEvery.Text = "of every";

            this.textMonthNumber.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textMonthNumber.Location = new System.Drawing.Point(145, 8);
            this.textMonthNumber.Multiline = false;
            this.textMonthNumber.Name = "textMonthNumber";
            this.textMonthNumber.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textMonthNumber.Size = new System.Drawing.Size(29, 16);
            this.textMonthNumber.TabIndex = 3;
            this.textMonthNumber.Text = "1";

            this.labelMonths.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelMonths.Location = new System.Drawing.Point(180, 12);
            this.labelMonths.Name = "labelMonths";
            this.labelMonths.Size = new System.Drawing.Size(55, 20);
            this.labelMonths.Text = "month(s)";

            this.radioThePrefix.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioThePrefix.Location = new System.Drawing.Point(8, 38);
            this.radioThePrefix.Name = "radioThePrefix";
            this.radioThePrefix.Size = new System.Drawing.Size(40, 22);
            this.radioThePrefix.TabIndex = 4;
            this.radioThePrefix.Text = "Th&e";

            this.cmbWeekDayNumber.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWeekDayNumber.Location = new System.Drawing.Point(53, 38);
            this.cmbWeekDayNumber.Name = "cmbWeekDayNumber";
            this.cmbWeekDayNumber.Size = new System.Drawing.Size(80, 21);
            this.cmbWeekDayNumber.TabIndex = 5;
            this.cmbWeekDayNumber.Items.AddRange( new string[] { "first", "second", "third", "fourth", "last" } );
            this.cmbWeekDayNumber.SelectedItem = 0;

            this.cmbWeekDay.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWeekDay.Location = new System.Drawing.Point(145, 38);
            this.cmbWeekDay.Name = "cmbWeekDay";
            this.cmbWeekDay.Size = new System.Drawing.Size(90, 21);
            this.cmbWeekDay.TabIndex = 6;
            this.cmbWeekDay.Items.AddRange( new string[] { "day", "weekday", "weekend", "Sunday", "Monday", "Tuesday",
                                                           "Wednesday", "Thursday", "Friday", "Saturday" } );
            this.cmbWeekDay.SelectedItem = "day";

            this.labelEvery2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelEvery2.Location = new System.Drawing.Point(250,42);
            this.labelEvery2.Name = "labelEvery2";
            this.labelEvery2.Size = new System.Drawing.Size(50, 20);
            this.labelEvery2.Text = "of every";

            this.textMonthNumber2.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textMonthNumber2.Location = new System.Drawing.Point(300, 38);
            this.textMonthNumber2.Multiline = false;
            this.textMonthNumber2.Name = "textMonthNumber2";
            this.textMonthNumber2.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textMonthNumber2.Size = new System.Drawing.Size(29, 16);
            this.textMonthNumber2.TabIndex = 7;
            this.textMonthNumber2.Text = "1";

            this.labelMonths2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelMonths2.Location = new System.Drawing.Point(340, 42);
            this.labelMonths2.Name = "labelMonths";
            this.labelMonths2.Size = new System.Drawing.Size(55, 20);
            this.labelMonths2.Text = "month(s)";

            this.radioRegenerateMonth.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioRegenerateMonth.Location = new System.Drawing.Point(8, 68);
            this.radioRegenerateMonth.Name = "radioRegenerateWeek";
            this.radioRegenerateMonth.Size = new System.Drawing.Size(130, 22);
            this.radioRegenerateMonth.TabIndex = 8;
            this.radioRegenerateMonth.Text = "Re&generate new task";

            this.textRegMonthsAfter.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textRegMonthsAfter.Location = new System.Drawing.Point(140, 68);
            this.textRegMonthsAfter.Multiline = false;
            this.textRegMonthsAfter.Name = "textRegMonthsAfter";
            this.textRegMonthsAfter.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textRegMonthsAfter.Size = new System.Drawing.Size(29, 16);
            this.textRegMonthsAfter.TabIndex = 9;
            this.textRegMonthsAfter.Text = "1";

            this.labelRegMonthsAfter.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelRegMonthsAfter.Location = new System.Drawing.Point(180, 72);
            this.labelRegMonthsAfter.Name = "labelRegMonthsAfter";
            this.labelRegMonthsAfter.Size = new System.Drawing.Size(190, 20);
            this.labelRegMonthsAfter.Text = "month(s) after each task is completed";
            #endregion Panel Monthly

            #region Panel Yearly
            this.panelYearly.Controls.Add(radioEvery2);
            this.panelYearly.Controls.Add(cmbMonth);
            this.panelYearly.Controls.Add(textDayNumber);
            this.panelYearly.Controls.Add(radioThePrefix2);
            this.panelYearly.Controls.Add(cmbWeekDayNumber2);
            this.panelYearly.Controls.Add(cmbWeekDay2);
            this.panelYearly.Controls.Add(cmbMonth2);
            this.panelYearly.Controls.Add(labelOf);
            this.panelYearly.Controls.Add(radioRegenerateYear);
            this.panelYearly.Controls.Add(textRegYearsAfter);
            this.panelYearly.Controls.Add(labelRegYearsAfter);
            this.panelYearly.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
            this.panelYearly.Location = new System.Drawing.Point(90, 16);
            this.panelYearly.Name = "panelYearly";
            this.panelYearly.Size = new System.Drawing.Size(250, 110);
            this.panelYearly.Visible = false;

            this.radioEvery2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioEvery2.Location = new System.Drawing.Point(8, 8);
            this.radioEvery2.Name = "radioEvery";
            this.radioEvery2.Size = new System.Drawing.Size(50, 22);
            this.radioEvery2.TabIndex = 1;
            this.radioEvery2.Text = "E&very";

            this.cmbMonth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMonth.Location = new System.Drawing.Point(60, 8);
            this.cmbMonth.Name = "cmbMonth";
            this.cmbMonth.Size = new System.Drawing.Size(80, 21);
            this.cmbMonth.TabIndex = 2;
            this.cmbMonth.Items.AddRange( new string[] { "January", "February", "March", "April", "May", "June",
                                                         "July", "August", "September", "October", "November", "December" } );
            this.cmbMonth.SelectedItem = 0;

            this.textDayNumber.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textDayNumber.Location = new System.Drawing.Point(145, 8);
            this.textDayNumber.Multiline = false;
            this.textDayNumber.Name = "textDayNumber";
            this.textDayNumber.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textDayNumber.Size = new System.Drawing.Size(29, 16);
            this.textDayNumber.TabIndex = 3;
            this.textDayNumber.Text = "1";

            this.radioThePrefix2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioThePrefix2.Location = new System.Drawing.Point(8, 38);
            this.radioThePrefix2.Name = "radioThePrefix";
            this.radioThePrefix2.Size = new System.Drawing.Size(40, 22);
            this.radioThePrefix2.TabIndex = 4;
            this.radioThePrefix2.Text = "Th&e";

            this.cmbWeekDayNumber2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWeekDayNumber2.Location = new System.Drawing.Point(60, 38);
            this.cmbWeekDayNumber2.Name = "cmbWeekDayNumber2";
            this.cmbWeekDayNumber2.Size = new System.Drawing.Size(80, 21);
            this.cmbWeekDayNumber2.TabIndex = 5;
            this.cmbWeekDayNumber2.Items.AddRange( new string[] { "first", "second", "third", "fourth", "last" } );
            this.cmbWeekDayNumber2.SelectedItem = 0;

            this.cmbWeekDay2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWeekDay2.Location = new System.Drawing.Point(145, 38);
            this.cmbWeekDay2.Name = "cmbWeekDay2";
            this.cmbWeekDay2.Size = new System.Drawing.Size(90, 21);
            this.cmbWeekDay2.TabIndex = 6;
            this.cmbWeekDay2.Items.AddRange( new string[] { "day", "weekday", "weekend", "Sunday", "Monday", "Tuesday",
                                                            "Wednesday", "Thursday", "Friday", "Saturday" } );
            this.cmbWeekDay2.SelectedItem = "day";

            this.labelOf.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelOf.Location = new System.Drawing.Point(240,42);
            this.labelOf.Name = "labelOf";
            this.labelOf.Size = new System.Drawing.Size(20, 20);
            this.labelOf.Text = "of";

            this.cmbMonth2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMonth2.Location = new System.Drawing.Point(265, 38);
            this.cmbMonth2.Name = "cmbMonth2";
            this.cmbMonth2.Size = new System.Drawing.Size(80, 21);
            this.cmbMonth2.TabIndex = 7;
            this.cmbMonth2.Items.AddRange( new string[] { "January", "February", "March", "April", "May", "June",
                                                          "July", "August", "September", "October", "November", "December" } );
            this.cmbMonth2.SelectedItem = 0;

            this.radioRegenerateYear.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioRegenerateYear.Location = new System.Drawing.Point(8, 68);
            this.radioRegenerateYear.Name = "radioRegenerateWeek";
            this.radioRegenerateYear.Size = new System.Drawing.Size(130, 22);
            this.radioRegenerateYear.TabIndex = 8;
            this.radioRegenerateYear.Text = "Re&generate new task";

            this.textRegYearsAfter.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textRegYearsAfter.Location = new System.Drawing.Point(140, 68);
            this.textRegYearsAfter.Multiline = false;
            this.textRegYearsAfter.Name = "textRegMonthsAfter";
            this.textRegYearsAfter.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textRegYearsAfter.Size = new System.Drawing.Size(29, 16);
            this.textRegYearsAfter.TabIndex = 9;
            this.textRegYearsAfter.Text = "1";

            this.labelRegYearsAfter.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelRegYearsAfter.Location = new System.Drawing.Point(180, 72);
            this.labelRegYearsAfter.Name = "labelRegMonthsAfter";
            this.labelRegYearsAfter.Size = new System.Drawing.Size(190, 20);
            this.labelRegYearsAfter.Text = "years(s) after each task is completed";
            #endregion Panel Yearly
            #endregion Pattern box

            #region Range Box
            this.boxRecRange.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
            this.boxRecRange.Controls.Add(labelStart);
            this.boxRecRange.Controls.Add(dateRecStart);
            this.boxRecRange.Controls.Add(radioNoEndDate);
            this.boxRecRange.Controls.Add(radioEndAfter);
            this.boxRecRange.Controls.Add(radioEndBy);
            this.boxRecRange.Controls.Add(textNumberOfOcc);
            this.boxRecRange.Controls.Add(labelOccurences);
            this.boxRecRange.Controls.Add(dateEndByDate);
            this.boxRecRange.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.boxRecRange.Location = new System.Drawing.Point(8, 140);
            this.boxRecRange.Name = "boxRecRange";
            this.boxRecRange.Size = new System.Drawing.Size(346, 99);
            this.boxRecRange.TabIndex = 2;
            this.boxRecRange.TabStop = false;
            this.boxRecRange.Text = "Range of recurrence";

            this.labelStart.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelStart.Location = new System.Drawing.Point(8, 20);
            this.labelStart.Name = "labelStart";
            this.labelStart.Size = new System.Drawing.Size(30, 16);
            this.labelStart.TabIndex = 1;
            this.labelStart.Text = "&Start:";

            this.dateRecStart.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.dateRecStart.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateRecStart.Location = new System.Drawing.Point(40, 18);
            this.dateRecStart.Name = "dateRecStart";
            this.dateRecStart.Size = new System.Drawing.Size(128, 21);
            this.dateRecStart.TabIndex = 2;

            this.radioNoEndDate.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioNoEndDate.Location = new System.Drawing.Point(180, 14);
            this.radioNoEndDate.Name = "radioNoEndDate";
            this.radioNoEndDate.Size = new System.Drawing.Size(90, 22);
            this.radioNoEndDate.TabIndex = 3;
            this.radioNoEndDate.Text = "N&o end date";
//            this.radioNoEndDate.CheckedChanged += new System.EventHandler(radioLow_CheckedChanged);

            this.radioEndAfter.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioEndAfter.Location = new System.Drawing.Point(180, 39);
            this.radioEndAfter.Name = "radioEndAfter";
            this.radioEndAfter.Size = new System.Drawing.Size(70, 22);
            this.radioEndAfter.TabIndex = 4;
            this.radioEndAfter.Text = "End a&fter:";
//            this.radioEndAfter.CheckedChanged += new System.EventHandler(radioLow_CheckedChanged);

            this.radioEndBy.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioEndBy.Location = new System.Drawing.Point(180, 67);
            this.radioEndBy.Name = "radioEndBy";
            this.radioEndBy.Size = new System.Drawing.Size(70, 22);
            this.radioEndBy.TabIndex = 6;
            this.radioEndBy.Text = "End &by:";
//            this.radioEndBy.CheckedChanged += new System.EventHandler(radioLow_CheckedChanged);

            this.textNumberOfOcc.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.textNumberOfOcc.Location = new System.Drawing.Point(250, 39);
            this.textNumberOfOcc.Multiline = false;
            this.textNumberOfOcc.Name = "textNumberOfOcc";
            this.textNumberOfOcc.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textNumberOfOcc.Size = new System.Drawing.Size(29, 16);
            this.textNumberOfOcc.TabIndex = 5;
            this.textNumberOfOcc.Text = "";

            this.labelOccurences.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelOccurences.Location = new System.Drawing.Point(290, 43);
            this.labelOccurences.Name = "labelOccurences";
            this.labelOccurences.Size = new System.Drawing.Size(63, 16);
            this.labelOccurences.TabStop = false;
            this.labelOccurences.Text = "occurrences";

            this.dateEndByDate.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.dateEndByDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateEndByDate.Location = new System.Drawing.Point(250, 68);
            this.dateEndByDate.Name = "dateEndByDate";
            this.dateEndByDate.Size = new System.Drawing.Size(128, 21);
            this.dateEndByDate.TabIndex = 7;
            #endregion Range Box

            this.buttonClear.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.buttonClear.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonClear.Location = new System.Drawing.Point(8, 250);
            this.buttonClear.Name = "_btnCategories";
            this.buttonClear.Size = new System.Drawing.Size(92, 24);
            this.buttonClear.TabIndex = 3;
            this.buttonClear.Text = "Clear pattern";
            #endregion

            //
            // TaskEditPane
            //
            tabsTaskPages.Controls.Add( pageGeneral );
            tabsTaskPages.Controls.Add( pageLinked );
//            tabsTaskPages.Controls.Add( pageRecurrence );

            pageGeneral.Controls.Add( this._btnCategories );
            pageGeneral.Controls.Add( this._propertiesPanel );
            pageGeneral.Controls.Add( this._subjectDescriptionPanel );
            pageGeneral.Controls.Add( this._btnCategories );
            pageGeneral.Controls.Add( this.BoxCategories );

            pageLinked.Controls.Add( labelLinkedRes );
            pageLinked.Controls.Add( _attachedView );
            pageLinked.Controls.Add( btnClearAttached );

            pageRecurrence.Controls.Add(this.boxRecRange);
            pageRecurrence.Controls.Add(this.buttonClear);
            pageRecurrence.Controls.Add(this.boxPattern);

            this.Controls.Add(this.tabsTaskPages);

            this.Name = "TaskEditPane";
            this.Size = new System.Drawing.Size(376, tabsTaskPages.Height + 10);
            this._subjectDescriptionPanel.ResumeLayout(false);
            this._propertiesPanel.ResumeLayout(false);
            this._attachedResourcesPanel.ResumeLayout(false);
            panelDaily.ResumeLayout(false);
            panelWeekly.ResumeLayout(false);
            panelMonthly.ResumeLayout(false);
            panelYearly.ResumeLayout(false);
            this.ResumeLayout(false);
        }
		#endregion

        #region AbstractEditPane overrides
        public override void EditResource( IResource res )
        {
            _task = res;
            isSuperTask = _task.GetLinksTo( "Task", TasksPlugin._linkSuperTask ).Count > 0;
            if( res.IsTransient )
            {
                _attachedView.ContextMenu = null;
            }
            this._attachedView.RootResource = res;

            /**
             * subject and description
             **/
            _subjectBox.Text = res.GetPropText( Core.Props.Subject );
            _descriptionBox.Text = res.GetPropText( TasksPlugin._propDescription );

            /**
             * datetime's
             */
            _dueDateTime.CurrentDateTime = res.GetDateProp( Core.Props.Date );
            _startDateTime.CurrentDateTime = res.GetDateProp( TasksPlugin._propStartDate );
//            _completeDateTime.CurrentDateTime = res.GetDateProp( TasksPlugin._propCompletedDate );
            if( !isSuperTask )
            {
                _reminderDateTime.CurrentDateTime = res.GetDateProp( TasksPlugin._propRemindDate );
            }
            else
            {
                _reminderDateTime.Visible = false;
                label9.Visible = false;
            }

            /**
             * status and priority
             */
            int status = res.GetIntProp( TasksPlugin._propStatus );
            if( status < 0 || status >= Enum.GetNames( typeof( TaskStatuses ) ).Length )
            {
                status = 0;
            }
            _statusBox.SelectedIndex = status;

            if( !isSuperTask )
            {
                int priority = res.GetIntProp( TasksPlugin._propPriority );
                if( priority < 0 || priority >= Enum.GetNames( typeof( TaskPriorities ) ).Length )
                {
                    priority = 0;
                }
                _priorityBox.SelectedIndex = priority;
            }
            else
            {
                _priorityBox.Visible = false;
                labelPriority.Visible = false;
                _statusBox.Enabled = false;
            }

            /**
             * remind workspace
             **/
            if( !isSuperTask )
            {
                if( _workspaceReminder.Enabled )
                {
                    IResource wks = res.GetLinkProp( TasksPlugin._propRemindWorkspace );
                    if( wks == null )
                    {
                        _workspacesBox.Enabled = _workspaceReminder.Checked = false;
                    }
                    else
                    {
                        _workspaceReminder.Checked = true;
                        _workspacesBox.SelectedItem = wks;
                    }
                }
            }
            else
            {
                _workspaceReminder.Visible = false;
                _workspacesBox.Visible = false;
            }

            /**
             * attached resources
             */
            IResourceList targets;
            lock( targets = res.GetLinksToLive( null, TasksPlugin._linkTarget ) )
            {
                foreach( IResource att in targets )
                {
                    _attachedView.JetListView.Nodes.Add( att );
                }
                _oldTargets = new IntArrayList( targets.ResourceIds ).ToArray();
            }

            /**
             * Categories
             */
            ShowCategories( res );

            /**
             *  Form size settings
             */
            if( isSuperTask )
            {
                labelStatus.Top -= 24;
                _statusBox.Top -= 24;
                labelDateComplete.Top -= 24;
                _completeDateTime.Top -= 24;

                _propertiesPanel.Height -= 60;
                _propertiesPanel.Top += 60;
                _subjectDescriptionPanel.Height += 60;
                this.Height -= 60;
            }

            Control parent = Parent;
            while( parent != null && parent as Form == null )
            {
                parent = parent.Parent;
            }
            if( parent != null )
            {
                Size size = ( parent as Form ).MinimumSize;
                if( size.Width < 440 )
                {
                    size.Width = 440;
                }
                if( size.Height < 240 )
                {
                    size.Height = 240;
                }
                Core.UserInterfaceAP.QueueJob(
                    JobPriority.Immediate, new SetFormMinSizeDelegate( SetFormMinSize ), parent as Form, size );
            }
        }

        public override void Save()
        {
            _descriptionBox.Select();
            // need to pass status and priority as parameters because .NET 2.0 doesn't allow to
            // access ComboBox.SelectedIndex from a non-UI thread. DatePickerCtrl uses ComboBox as well.
            Core.ResourceAP.RunUniqueJob( new SaveTaskDelegate(DoSaveTask),
                _statusBox.SelectedIndex,
                _priorityBox.SelectedIndex,
                _startDateTime.CurrentDateTime,
                _dueDateTime.CurrentDateTime,
                _reminderDateTime.CurrentDateTime,
                _completeDateTime.CurrentDateTime );
        }

        public override void Cancel()
        {
            Core.ResourceAP.RunUniqueJob( new MethodInvoker( RestoreTargets ) );
        }

        #endregion

        private void _subjectDescriptionPanel_Paint( object sender, PaintEventArgs e )
        {
            DrawRectangle( e.Graphics, _subjectDescriptionPanel );
        }

        private void _propertiesPanel_Paint( object sender, PaintEventArgs e )
        {
            DrawRectangle( e.Graphics, _propertiesPanel );
        }

        private void _statusBox_SelectedIndexChanged( object sender, EventArgs e )
        {
            if( _statusBox.SelectedIndex == 2 )
            {
                if( _reminderDateTime.CurrentDateTime != DateTime.MinValue )
                {
                    _remindDateTimeCopy = _reminderDateTime.CurrentDateTime;
                }
                _reminderDateTime.CurrentDateTime = DateTime.MinValue;
                _reminderDateTime.Enabled = false;
                _completeDateTime.Enabled = true;
                if( _completeDateTimeCopy == DateTime.MinValue )
                {
                    _completeDateTime.CurrentDateTime = DateTime.Now;
                }
                else
                {
                    _completeDateTime.CurrentDateTime = _completeDateTimeCopy;
                }
            }
            else
            {
                _reminderDateTime.Enabled = true;
                if( _remindDateTimeCopy != DateTime.MinValue )
                {
                    _reminderDateTime.CurrentDateTime = _remindDateTimeCopy;
                }
                if( _completeDateTime.CurrentDateTime != DateTime.MinValue )
                {
                    _completeDateTimeCopy = _completeDateTime.CurrentDateTime;
                }
                _completeDateTime.CurrentDateTime = DateTime.MinValue;
                _completeDateTime.Enabled = false;
            }
        }

        private void _workspaceReminder_CheckedChanged( object sender, EventArgs e )
        {
            if( _workspacesBox.Enabled == _workspaceReminder.Checked )
            {
                if( _workspacesBox.SelectedItem == null )
                {
                    _workspacesBox.SelectedItem = _workspacesBox.Items[ 0 ];
                }
                _workspacesBox.Focus();
            }
        }

        private void _subjectBox_TextChanged(object sender, EventArgs e)
        {
            OnValidStateChanged( new ValidStateEventArgs( _subjectBox.Text.Length > 0, "Subject cannot be empty" ) );
        }

        private void _removeFromTaskMenuItem_Click( object sender, EventArgs e )
        {
            RemoveSelectedTargets();
        }

        private void _attachedResources_KeyDown( object sender, KeyEventArgs e )
        {
            if( e.KeyCode == Keys.Delete && !e.Alt && !e.Control && !e.Shift )
            {
                RemoveSelectedTargets();
                e.Handled = true;
            }
        }

        #region implementation details

	    private delegate void SaveTaskDelegate( int status, int priority, DateTime startDate, DateTime dueDate,
                                                DateTime reminderDate, DateTime completeDate );

        void DoSaveTask( int status, int priority, DateTime startDate, DateTime dueDate,
                         DateTime reminderDate, DateTime completeDate)
        {
            bool indexIt = false;
            if( _task.IsTransient )
            {
                Core.WorkspaceManager.AddToActiveWorkspace( _task );
                indexIt = true;
            }
            else
            {
                _task.BeginUpdate();
            }
            try
            {
                if( _task.GetPropText( Core.Props.Subject ) != _subjectBox.Text )
                {
                    indexIt = true;
                    _task.SetProp( Core.Props.Subject, _subjectBox.Text );
                }
                if( _task.GetPropText( TasksPlugin._propDescription ) != _descriptionBox.Text )
                {
                    indexIt = true;
                    _task.SetProp( TasksPlugin._propDescription, _descriptionBox.Text );
                }
                _task.SetProp( TasksPlugin._propStatus, status );
                _task.SetProp( TasksPlugin._propPriority, priority );
                _task.SetProp( TasksPlugin._propStartDate, startDate );
                _task.SetProp( Core.Props.Date, dueDate );
                _task.SetProp( TasksPlugin._propRemindDate, reminderDate );
                if( status == 2 )
                    _task.SetProp( TasksPlugin._propCompletedDate, completeDate );
                else
                    _task.DeleteProp( TasksPlugin._propCompletedDate );

                if( _workspaceReminder.Enabled && _workspaceReminder.Checked )
                {
                    IResource wks = (IResource)_workspacesBox.SelectedItem;
                    _task.SetProp( TasksPlugin._propRemindWorkspace, wks );
                    Core.WorkspaceManager.AddResourceToWorkspace( wks, _task );
                }
                else
                {
                    _task.DeleteLinks( TasksPlugin._propRemindWorkspace );
                }
            }
            finally
            {
                _task.EndUpdate();

                //  If we edit leaf task (not a supertask) then propagate
                //  information on the status and dates to the upper supertasks.
                if( _task.GetLinksTo( "Task", TasksPlugin._linkSuperTask ).Count == 0 )
                {
                    IResource root = TasksPlugin.RootOfTask( _task );
                    TasksPlugin.RecalculateTreeParameters( root );
                }

                if( indexIt )
                {
                    Core.TextIndexManager.QueryIndexing( _task.Id );
                }
            }
        }

        private void RestoreTargets()
        {
            IResourceList currAttachs = _task.GetLinksTo( null, TasksPlugin._linkTarget );
            IntArrayList remainedTargetIds = new IntArrayList( currAttachs.ResourceIds );

            foreach( int id in _oldTargets )
            {
                if( remainedTargetIds.IndexOf( id ) < 0 )
                {
                    IResource target = Core.ResourceStore.TryLoadResource( id );
                    if ( target != null )
                    {
                        target.AddLink( TasksPlugin._linkTarget, _task );
                    }
                }
            }
        }

        private static void DrawRectangle( Graphics graphics, Panel panel )
        {
            Pen apen = new Pen( SystemColors.ActiveCaption, 1 );
            using( apen )
            {
                graphics.DrawRectangle( apen, 0, 0, panel.Width - 1, panel.Height - 1 );
            }
        }

        private void _categoriesButton_Click( object sender, EventArgs e )
        {
            Core.UIManager.ShowAssignCategoriesDialog( this, _task.ToResourceList() );
            ShowCategories( _task );
        }

        #region Categories
        private void ShowCategories( IResource res )
        {
            string  presentation = string.Empty;
            IResourceList categories = res.GetLinksOfType( "Category", "Category" );
            foreach( IResource cat in categories )
            {
                presentation += cat.DisplayName + ", ";
            }
            if( presentation.Length > 0 )
                presentation = presentation.Substring( 0, presentation.Length - 2 );

            BoxCategories.Text = presentation;
        }
        #endregion Categories

        private delegate void SetFormMinSizeDelegate( Form form, Size size );

        private static void SetFormMinSize( Form form, Size size )
        {
            form.MinimumSize = size;
        }

        private void TimeFormatStateChanged( object sender, ValidStateEventArgs e )
        {
            OnValidStateChanged( e );
        }
        #endregion

        private void PatternCheckedChanged(object sender, EventArgs e)
        {
            RadioButton radio = (RadioButton) sender;
            Panel       panel = (Panel) radio.Tag;
            panel.Visible = radio.Checked;
        }

        #region Remove Attached Resources
        private void btnClearAttached_Click(object sender, EventArgs e)
        {
            IResourceList list = _task.GetLinksOfType( null, TasksPlugin._linkTarget );
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( RemoveTargets ), list );
            _attachedView.JetListView.Nodes.Clear();
        }

        private void RemoveSelectedTargets()
        {
            IResourceList list = _attachedView.GetSelectedResources();
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceListDelegate( RemoveTargets ), list );
            foreach( IResource res in list )
                _attachedView.JetListView.Nodes.Remove( res );
        }

        private void RemoveTargets( IResourceList list )
        {
            foreach( IResource target in list )
            {
                target.DeleteLink( TasksPlugin._linkTarget, _task );
            }
        }
        #endregion Remove Attached Resources

        #region Drag'n'Drop
        internal class DnDHandler : IResourceDragDropHandler
        {
            private readonly TaskEditPane parentPane;

            public DnDHandler( TaskEditPane pane )
            {
                parentPane = pane;
            }

            public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
            {

            }
            public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
            {
                DragDropEffects result = DragDropEffects.None;
                IResourceList   resources = data.GetData( typeof( IResourceList ) ) as IResourceList;
                if( resources != null )
                {
                    foreach( IResource res in resources )
                    {
                        if( res.Type == "Task" )
                        {
                            return DragDropEffects.None;
                        }
                        if( !Core.ResourceStore.ResourceTypes[ res.Type ].HasFlag( ResourceTypeFlags.Internal ) )
                        {
                            result = DragDropEffects.Link;
                        }
                    }
                }
                return result;
            }

            public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
            {
                IResourceList resources = data.GetData( typeof( IResourceList ) ) as IResourceList;
                Core.ResourceAP.RunUniqueJob( new TasksViewPane.AddTargetsDelegate( TasksPlugin.AddDescendants ),
                                              parentPane._task, resources, TasksPlugin._linkTarget );
                foreach( IResource res in resources )
                {
                    parentPane._attachedView.JetListView.Nodes.Add( res );
                }
            }
        }
        #endregion Drag'n'Drop
    }
}
