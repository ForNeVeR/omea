// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.Tasks
{
	internal class ReminderForm : DialogBase
	{
        private static readonly TimeSpan[] _snoozePeriods =
        {
            //  5, 10, 15, 30 minutes
            new TimeSpan( 0, 0, 5, 0 ), new TimeSpan( 0, 0, 10, 0 ),new TimeSpan( 0, 0, 15, 0 ),new TimeSpan( 0, 0, 30, 0 ),
            //  1, 2, 4, 8, 12 hours
            new TimeSpan( 0, 1, 0, 0 ), new TimeSpan( 0, 2, 0, 0 ), new TimeSpan( 0, 4, 0, 0 ), new TimeSpan( 0, 8, 0, 0 ), new TimeSpan( 0, 12, 0, 0 ),
            //  1, 2, 3, 4 days
            new TimeSpan( 1, 0, 0, 0 ), new TimeSpan( 2, 0, 0, 0 ), new TimeSpan( 3, 0, 0, 0 ), new TimeSpan( 4, 0, 0, 0 ),
            //  1 or 2 weeks
            new TimeSpan( 7, 0, 0, 0 ), new TimeSpan( 14, 0, 0, 0 ),
        };

        private System.Windows.Forms.Button _dismissButton, _dismissAllButton;
        private System.Windows.Forms.Button _snoozeButton, _snoozeAllButton;
        private System.Windows.Forms.ComboBox _snoozePeriodList;
        private System.Windows.Forms.TextBox _descriptionTextBox;
        private System.Windows.Forms.Label _snoozeLabel;
        private System.Windows.Forms.Label _targetsLabel;
        private System.Windows.Forms.Panel _controlPanel;
        private System.Windows.Forms.Button _editButton;
        private ResourceLinkLabel _taskSubject;
		private ToolTip   toolTipReason;
		private System.ComponentModel.Container components = null;

        private ResourceListView2 _targetList;
        private IResourceList _taskLive = Core.ResourceStore.EmptyResourceList;

        private static ReminderForm _reminderForm = null;
        private static bool         _switchingDone = false;

        public ReminderForm( IResource task )
		{
			InitializeComponent();
            this.Icon = Core.UIManager.ApplicationIcon;

            RestoreSettings();
            Text += " at " + DateTime.Now.ToShortTimeString();

            int index = Core.SettingStore.ReadInt( "Tasks", "ReminderInterval", 0 );
            _snoozePeriodList.SelectedIndex = index;

            _targetList.JetListView.SelectionStateChanged += new StateChangeEventHandler( OnSelectedTaskChanged );

            InitSpyResourceList( task );
            InitDescription( task );
            InitListWithAttachments( task );

            WindowsMultiMedia.PlaySound( Path.Combine( Application.StartupPath, "reminder.wav" ), new IntPtr( 0 ),
                                         WindowsMultiMedia.SND_FILENAME | WindowsMultiMedia.SND_ASYNC );
		}

        //  NB: important to call <HideDescriptionBox> after the live resource
        //      list had been updated in order to count correct amount of
        //      current tasks.
        private void InitDescription( IResource task )
        {
            _taskSubject.Resource = task;
            string description = task.GetPropText( TasksPlugin._propDescription );
            if( description.Length > 0 && _taskLive.Count == 1 )
            {
                _descriptionTextBox.Text = description;
            }
            else
            {
                HideDescriptionBox();
            }

            InitializeTooltips( task, description );
        }

        private void  HideDescriptionBox()
        {
            _descriptionTextBox.Visible = false;
            int heightSubtrahend = _targetsLabel.Top - _descriptionTextBox.Top;
            _targetsLabel.Top -= heightSubtrahend;
            _targetList.Top -= heightSubtrahend;
            _targetList.Height += heightSubtrahend;
            Height -= heightSubtrahend;
        }

        private void  InitializeTooltips( IResource task, string description )
        {
            IResourceList attaches = task.GetLinksTo( null, TasksPlugin._linkTarget );
            if( attaches.Count > 0 )
            {
                if( description.Length > 0 )
                    description += "\n";
                description += attaches.Count.ToString() + " Attachment(s):";

                foreach( IResource res in attaches )
                    description += "\n   " + res.DisplayName;
            }
            toolTipReason.SetToolTip( _taskSubject, description );
            toolTipReason.SetToolTip( _taskSubject.NameLabel, description );
        }

        private void InitListWithAttachments( IResource task )
        {
            _targetList.AllowColumnReorder = false;
            _targetList.Columns.Add( new ResourceIconColumn() );
            ResourceListView2Column nameCol = _targetList.AddColumn( ResourceProps.DisplayName );
            nameCol.AutoSize = true;

            IResourceList attaches = task.GetLinksTo( null, TasksPlugin._linkTarget );
            foreach( IResource res in attaches )
                _targetList.JetListView.Nodes.Add( res );
        }

        private void InitSpyResourceList( IResource task )
        {
		    IResourceStore store = Core.ResourceStore;
            IResourceList  currTasks = Core.ResourceStore.EmptyResourceList;

            //  In order to correctly form a new live list we have to reconstruct
            //  it from scratch - first, form a "plain" (Union with merge) list
            //  and only then add minus and intersection.
            foreach( IResource res in _taskLive )
            {
                currTasks = currTasks.Union( res.ToResourceListLive(), true );
            }
            _taskLive = currTasks.Union( task.ToResourceListLive(), true );

		    _taskLive = _taskLive.Minus( store.FindResources( null, TasksPlugin._propStatus, (int)TaskStatuses.Completed ) );
            _taskLive = _taskLive.Intersect( store.FindResourcesInRange( null, TasksPlugin._propRemindDate,
                                                                         DateTime.MinValue.AddSeconds( 1 ), DateTime.Now ), true );
            _taskLive.ResourceDeleting += new ResourceIndexEventHandler( _taskLive_ResourceDeleting );
        }

		public static void  AddTask( IResource task )
		{
            try
            {
    		    if( _reminderForm == null )
                {
                    _reminderForm = new ReminderForm( task );
                    _reminderForm.Show();
                }
                else
		        {
                    _reminderForm.Text = "Summary for " + (_reminderForm._taskLive.Count + 1).ToString() + " reminders";
		            _reminderForm._targetsLabel.Text = "Reminders:";
                    _reminderForm._dismissAllButton.Visible = true;
                    _reminderForm._snoozeAllButton.Visible = true;

                    _reminderForm.InitSpyResourceList( task );
                    _reminderForm.HideDescriptionBox();

                    //  Moment when we have to switch between modes...
                    if( !_switchingDone )
                    {
                        _switchingDone = true;
                        _reminderForm._targetList.JetListView.Nodes.Clear();
                        foreach( IResource res in _reminderForm._taskLive )
                        {
                            _reminderForm._targetList.JetListView.Nodes.Add( res );
                        }
                        _reminderForm._targetList.Selection.SelectSingleItem( task );
                    }
                }
            }
            catch( ResourceDeletedException )
            {
                // task could be deleted while we did smth in the UI thread
            }
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

		#region Windows Form Designer generated code

        private void InitializeComponent()
		{
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ReminderForm));
//			components = new Container();
            this._targetList = new JetBrains.Omea.GUIControls.ResourceListView2();
            this._controlPanel = new System.Windows.Forms.Panel();
            this._snoozeLabel = new System.Windows.Forms.Label();
            this._snoozePeriodList = new System.Windows.Forms.ComboBox();
            this._snoozeButton = new System.Windows.Forms.Button();
            this._editButton = new System.Windows.Forms.Button();
            this._dismissButton = new System.Windows.Forms.Button();
            this._dismissAllButton = new System.Windows.Forms.Button();
            this._snoozeAllButton = new System.Windows.Forms.Button();
            this._descriptionTextBox = new System.Windows.Forms.TextBox();
            this._targetsLabel = new System.Windows.Forms.Label();
            this._taskSubject = new JetBrains.Omea.GUIControls.ResourceLinkLabel();
            this._controlPanel.SuspendLayout();

            toolTipReason = new ToolTip();
			toolTipReason.ShowAlways = true;
            this.SuspendLayout();
            //
            // _targetList
            //
            this._targetList.AllowDrop = true;
            this._targetList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._targetList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._targetList.HideSelection = false;
            this._targetList.Location = new System.Drawing.Point(8, 128);
            this._targetList.Name = "_targetList";
            this._targetList.ShowContextMenu = false;
            this._targetList.Size = new System.Drawing.Size(348, 72);
            this._targetList.TabIndex = 2;
            //
            // _controlPanel
            //
            this._controlPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._controlPanel.Controls.Add(this._snoozeLabel);
            this._controlPanel.Controls.Add(this._snoozePeriodList);
            this._controlPanel.Controls.Add(this._snoozeButton);
            this._controlPanel.Controls.Add(this._editButton);
            this._controlPanel.Controls.Add(this._dismissButton);
            this._controlPanel.Controls.Add(this._dismissAllButton);
            this._controlPanel.Controls.Add(this._snoozeAllButton);
            this._controlPanel.Location = new System.Drawing.Point(8, 204);
            this._controlPanel.Name = "_controlPanel";
            this._controlPanel.Size = new System.Drawing.Size(348, 84);
            this._controlPanel.TabIndex = 3;
            //
            // _snoozeLabel
            //
            this._snoozeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._snoozeLabel.Location = new System.Drawing.Point(8, 36);
            this._snoozeLabel.Name = "_snoozeLabel";
            this._snoozeLabel.Size = new System.Drawing.Size(248, 17);
            this._snoozeLabel.TabIndex = 9;
            this._snoozeLabel.Text = "Click Snooze to be reminded in:";
            //
            // _snoozePeriodList
            //
            this._snoozePeriodList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._snoozePeriodList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._snoozePeriodList.Items.AddRange(new object[] {
                                                                   "5 minutes",
                                                                   "10 minutes",
                                                                   "15 minutes",
                                                                   "30 minutes",
                                                                   "1 hour",
                                                                   "2 hours",
                                                                   "4 hours",
                                                                   "8 hours",
                                                                   "0.5 days",
                                                                   "1 day",
                                                                   "2 days",
                                                                   "3 days",
                                                                   "4 days",
                                                                   "1 week",
                                                                   "2 weeks"});
            this._snoozePeriodList.Location = new System.Drawing.Point(8, 57);
            this._snoozePeriodList.Name = "_snoozePeriodList";
            this._snoozePeriodList.Size = new System.Drawing.Size(252, 21);
            this._snoozePeriodList.TabIndex = 5;
            //
            // _snoozeButton
            //
            this._snoozeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._snoozeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._snoozeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._snoozeButton.Location = new System.Drawing.Point(272, 56);
            this._snoozeButton.Name = "_snoozeButton";
            this._snoozeButton.Size = new System.Drawing.Size(75, 25);
            this._snoozeButton.TabIndex = 6;
            this._snoozeButton.Text = "Snooze";
            this._snoozeButton.Click += new System.EventHandler(this._snoozeButton_Click);
            //
            // _editButton
            //
            this._editButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._editButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._editButton.Location = new System.Drawing.Point(272, 0);
            this._editButton.Name = "_editButton";
            this._editButton.Size = new System.Drawing.Size(75, 25);
            this._editButton.TabIndex = 3;
            this._editButton.Text = "Edit Task...";
            this._editButton.Click += new System.EventHandler(this._openButton_Click);
            //
            // _dismissButton
            //
            this._dismissButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._dismissButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._dismissButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._dismissButton.Location = new System.Drawing.Point(272, 28);
            this._dismissButton.Name = "_dismissButton";
            this._dismissButton.Size = new System.Drawing.Size(75, 25);
            this._dismissButton.TabIndex = 4;
            this._dismissButton.Text = "Dismiss";
            this._dismissButton.Click += new System.EventHandler(this._dismissButton_Click);
            //
            // _dismissAllButton
            //
            this._dismissAllButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this._dismissAllButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._dismissAllButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._dismissAllButton.Location = new System.Drawing.Point(8, 8);
            this._dismissAllButton.Name = "_dismissAllButton";
            this._dismissAllButton.Size = new System.Drawing.Size(85, 25);
            this._dismissAllButton.TabIndex = 4;
            this._dismissAllButton.Text = "Dismiss All";
            this._dismissAllButton.Visible = false;
            this._dismissAllButton.Click += new System.EventHandler(this._dismissAllButton_Click);
            //
            // _snoozeAllButton
            //
            this._snoozeAllButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this._snoozeAllButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._snoozeAllButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._snoozeAllButton.Location = new System.Drawing.Point(102, 8);
            this._snoozeAllButton.Name = "_snoozeAllButton";
            this._snoozeAllButton.Size = new System.Drawing.Size(85, 25);
            this._snoozeAllButton.TabIndex = 4;
            this._snoozeAllButton.Text = "Snooze All";
            this._snoozeAllButton.Visible = false;
            this._snoozeAllButton.Click += new System.EventHandler(this._snoozeAllButton_Click);
            //
            // _descriptionTextBox
            //
            this._descriptionTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._descriptionTextBox.Location = new System.Drawing.Point(8, 32);
            this._descriptionTextBox.Multiline = true;
            this._descriptionTextBox.Name = "_descriptionTextBox";
            this._descriptionTextBox.ReadOnly = true;
            this._descriptionTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._descriptionTextBox.Size = new System.Drawing.Size(348, 72);
            this._descriptionTextBox.TabIndex = 1;
            this._descriptionTextBox.Text = "";
            //
            // _targetsLabel
            //
            this._targetsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._targetsLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._targetsLabel.Location = new System.Drawing.Point(8, 110);
            this._targetsLabel.Name = "_targetsLabel";
            this._targetsLabel.Size = new System.Drawing.Size(348, 17);
            this._targetsLabel.TabIndex = 6;
            this._targetsLabel.Text = "Attached resources:";
            this._targetsLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // _taskSubject
            //
            this._taskSubject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._taskSubject.LinkOwnerResource = null;
            this._taskSubject.LinkType = 0;
            this._taskSubject.Location = new System.Drawing.Point(8, 8);
            this._taskSubject.Name = "_taskSubject";
            this._taskSubject.PostfixText = "";
            this._taskSubject.Resource = null;
            this._taskSubject.ShowIcon = true;
            this._taskSubject.Size = new System.Drawing.Size(348, 20);
            this._taskSubject.TabIndex = 0;
            //
            // ReminderForm
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(364, 294);
            this.Controls.Add(this._taskSubject);
            this.Controls.Add(this._targetsLabel);
            this.Controls.Add(this._descriptionTextBox);
            this.Controls.Add(this._controlPanel);
            this.Controls.Add(this._targetList);
            this.KeyPreview = true;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new System.Drawing.Size(360, 320);
            this.Name = "ReminderForm";
            this.ShowInTaskbar = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Reminder";
            this.TopMost = true;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ReminderForm_KeyDown);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.ReminderForm_Closing);
            this._controlPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        private void _openButton_Click(object sender, System.EventArgs e)
        {
            int elements = _targetList.JetListView.Nodes.Count;
            IResource task = GetActiveTask();
            if ( task == null ) return;

            OpenTaskAction.OpenTask( task );

            if( elements == 1 )
            {
                Close();
                _reminderForm = null;
            }
        }

        #region Dismiss
        private void _dismissButton_Click(object sender, System.EventArgs e)
        {
            IResource task = GetActiveTask();
            if ( task != null )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceDelegate(Dismiss), task );
            }
        }

        private void _dismissAllButton_Click(object sender, System.EventArgs e)
        {
            foreach( JetListViewNode node in _targetList.JetListView.Nodes )
            {
                IResource res = (IResource) node.Data;
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceDelegate( Dismiss ), res );
            }
        }

        private void Dismiss( IResource task )
        {
            if( !task.IsDeleted )
            {
                task.BeginUpdate();
                try
                {
                    task.DeleteProp( TasksPlugin._propRemindDate );
                    task.DeleteLinks( TasksPlugin._propRemindWorkspace );
                }
                finally
                {
                    task.EndUpdate();
                }
            }
        }
        #endregion Dismiss

        #region Snooze
        private void _snoozeButton_Click(object sender, System.EventArgs e)
        {
            IResource task = GetActiveTask();
            if ( task != null )
            {
                Core.ResourceAP.QueueJob(JobPriority.Immediate, new ResourceDelegate(Snooze), task);
            }
        }

        private void _snoozeAllButton_Click(object sender, System.EventArgs e)
        {
            foreach( JetListViewNode node in _targetList.JetListView.Nodes )
            {
                IResource res = (IResource) node.Data;
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new ResourceDelegate( Snooze ), res );
            }
        }

        private void Snooze( IResource task )
        {
            if( !task.IsDeleted )
            {
                task.BeginUpdate();
                try
                {
                    DateTime rd = DateTime.Now.Add( _snoozePeriods[ _snoozePeriodList.SelectedIndex ] );
                    task.SetProp( TasksPlugin._propRemindDate, rd );
                }
                finally
                {
                    task.EndUpdate();
                }
            }
        }
        #endregion Snooze

        private void _taskLive_ResourceDeleting( object sender, ResourceIndexEventArgs e )
        {
            if( _switchingDone )
            {
                //  Remove a task from the list if only we have switched to the
                //  multiple view mode, and it contains tasks and not attachments.
                _reminderForm._targetList.JetListView.Nodes.Remove( e.Resource );
            }

            //  If there is only one active task in our live list - we have processed all
            //  tasks independently of the current mode (and the last one is being deleted).
            if (_taskLive.Count == 1)
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( Close ) );
            }
        }

        private void ReminderForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Core.SettingStore.WriteInt( "Tasks", "ReminderInterval", _snoozePeriodList.SelectedIndex );

            _taskLive.Dispose();
            _reminderForm = null;
            _switchingDone = false;
        }

        private void ReminderForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            IResource task = GetActiveTask();
            if ( task != null )
            {
                Core.ActionManager.ExecuteKeyboardAction(new ActionContext(task.ToResourceList()), e.KeyCode);
            }
        }

        private void OnSelectedTaskChanged( object sender, StateChangeEventArgs e )
        {
            if( _switchingDone )
            {
                IResource task = (IResource) e.Node.Data;
                InitDescription( task );
            }
        }

        private IResource GetActiveTask()
        {
            if (_switchingDone )
            {
                JetListViewNode activeNode = _targetList.JetListView.Selection.ActiveNode;
                if ( activeNode != null )
                {
                    return (IResource) activeNode.Data;
                }
                return null;
            }
            return _taskLive[ 0 ];
        }
	}
}
