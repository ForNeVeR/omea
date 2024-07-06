// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;
using JetBrains.JetListViewLibrary;
using JetBrains.UI.Interop;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.Tasks
{
    internal class TasksViewPane : AbstractViewPane
    {
        #region Attributes
        private TextBox                 _editNewTask;
        private DecoResourceTreeView    _tasksTree;

        internal static ToDoGenericFilter _todoFilter = new ToDoGenericFilter();

        internal delegate void AddTargetsDelegate( IResource task, IResourceList targets, int link );
        #endregion Attributes

        public TasksViewPane()
        {
            InitializeComponent();

            _tasksTree.OpenProperty = Core.Props.Open;
            _tasksTree.AddNodeDecorator( new TasksNodeDecorator() );

            Core.WorkspaceManager.WorkspaceChanged += OnActiveWorkspaceChanged;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose (disposing);
            if( disposing )
            {
                _tasksTree = null;
            }
        }

        #region Initialize Components
        private void InitializeComponent()
        {
            _tasksTree = new DecoResourceTreeView();
            _editNewTask = new TextBox();
            SuspendLayout();
            //
            // _tasksTreeView2
            //
            _tasksTree.AllowDrop = true;
            _tasksTree.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            _tasksTree.BackColor = SystemColors.Window;
            _tasksTree.BorderStyle = BorderStyle.None;
            _tasksTree.ExecuteDoubleClickAction = true;
            _tasksTree.HideSelection = false;
            _tasksTree.Location = new Point(0, 20);
            _tasksTree.MultiSelect = true;
            _tasksTree.Name = "_tasksTree";
            _tasksTree.SelectAddedItems = true;
            _tasksTree.Size = new Size(336, 163);
            _tasksTree.TabIndex = 0;
            _tasksTree.DoubleClick += _tasksTreeView2_DoubleClick;
            _tasksTree.KeyDown += _tasksTreeView_KeyDown;
            _tasksTree.AfterItemEdit += _tasksTreeView_AfterLabelEdit;
            _tasksTree.BeforeItemEdit += _tasksTreeView_BeforeLabelEdit;
            //
            // _newTaskEditBox
            //
            _editNewTask.AllowDrop = true;
            _editNewTask.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            _editNewTask.ForeColor = Color.Gray;
            _editNewTask.Location = new Point(0, 0);
            _editNewTask.Name = "_editNewTask";
            _editNewTask.Size = new Size(336, 21);
            _editNewTask.TabIndex = 1;
            _editNewTask.Text = "Click here to add a new Task";
            _editNewTask.KeyPress += _newTaskEditBox_KeyPress;
            _editNewTask.DragOver += _newTaskEditBox_DragOver;
            _editNewTask.DragDrop += _newTaskEditBox_DragDrop;
            _editNewTask.Leave += _newTaskEditBox_Leave;
            _editNewTask.Enter += _newTaskEditBox_Enter;
            //
            // TasksViewPane
            //
            Controls.Add(_editNewTask);
            Controls.Add(_tasksTree);
            Font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 204);
            Name = "TasksViewPane";
            Size = new Size(336, 184);
            ResumeLayout(false);
        }
        #endregion Initialize Components

        public override void Populate()
        {
            _tasksTree.RootResource = TasksPlugin.RootTask;
            _tasksTree.ParentProperty = TasksPlugin._linkSuperTask;

            ShowCompletedTaks = Core.SettingStore.ReadBool( "Tasks", "ShowCompletedTasks", true );
            _tasksTree.AddNodeFilter( _todoFilter );
            _tasksTree.UpdateNodeFilter( true );
        }

        public bool  ShowCompletedTaks
        {
            set
            {
                _todoFilter.showCompletedTasks = value;
                _tasksTree.UpdateNodeFilter( true );
            }
        }

        private void OnActiveWorkspaceChanged( object sender, EventArgs e )
        {
            if( Core.UserInterfaceAP.IsOwnerThread )
            {
                _tasksTree.UpdateNodeFilter( true );
            }
            else
            {
                Core.UserInterfaceAP.QueueJob( new EventHandler( OnActiveWorkspaceChanged ), sender, e );
            }
        }

        private void OpenAction()
        {
            IResource selected = null;
            IResourceList selList = _tasksTree.GetSelectedResources();
            if( selList.Count == 1 )
                selected = selList[ 0 ];

            if( selected != null )
            {
                if( selected.Type != "Task" )
                {
                    Core.UIManager.DisplayResourceInContext( selected );
                }
                else
                {
                    OpenTaskAction.OpenTask( selected );
                }
            }
        }

        #region EditBox events
        internal delegate void StringDelegate( string name );
        internal delegate void ResStringDelegate( IResource res, string name );

        private void _newTaskEditBox_Enter(object sender, EventArgs e)
        {
            _editNewTask.Text = string.Empty;
            _editNewTask.ForeColor = Color.Black;
        }

        private void _newTaskEditBox_Leave(object sender, EventArgs e)
        {
            if( _editNewTask.Text.Length > 0 )
            {
                Core.ResourceAP.RunUniqueJob( new StringDelegate( CreateNewTaskInline ), _editNewTask.Text );
            }
            _editNewTask.Text = "Click here to add a new Task";
            _editNewTask.ForeColor = Color.Gray;
        }

        private void _newTaskEditBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if( e.KeyChar == '\r' )
            {
                e.Handled = true;
                _tasksTree.Focus();
            }
        }

        private void _tasksTreeView2_DoubleClick(object sender, HandledEventArgs e)
        {
            OpenAction();
        }
        #endregion EditBox events

        #region Drag'n'Drop in EditBox
        private static void _newTaskEditBox_DragOver(object sender, DragEventArgs e)
        {
            IResourceList targets = (IResourceList) e.Data.GetData( typeof( IResourceList ) );
            if( targets != null && targets.Count > 0 )
            {
                e.Effect = DragDropEffects.Link;
            }
        }

        private static void _newTaskEditBox_DragDrop(object sender, DragEventArgs e)
        {
            IResourceList targets = (IResourceList) e.Data.GetData( typeof( IResourceList ) );
            if( targets != null && targets.Count > 0 )
            {
                System.Diagnostics.Trace.WriteLine( "Task from drop on edit, targets count = " + targets.Count );
                new NewTaskAction().Execute( new ActionContext( targets ) );
            }
        }
        #endregion Drag'n'Drop in EditBox

        #region ResourceTreeView Label Editing
        private static void _tasksTreeView_BeforeLabelEdit( object sender, ResourceItemEditEventArgs e )
        {
            e.CancelEdit = ( e.Resource == null || e.Resource.Type != "Task" );
        }

        private static void _tasksTreeView_AfterLabelEdit( object sender, ResourceItemEditEventArgs e )
        {
            IResource task = e.Resource;
            if( task == null || task.Type != "Task" || e.Text == null || e.Text.Length == 0 )
            {
                e.CancelEdit = true;
            }
            else
            {
                ResourceProxy proxy = new ResourceProxy( task );
                proxy.SetProp( Core.Props.Subject, e.Text );
            }
        }
        #endregion ResourceTreeView Label Editing

        #region Keyboard processing
        private void _tasksTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            IResourceList selection = _tasksTree.GetSelectedResources();
            if( !e.Alt && !e.Control && !e.Shift )
            {
                if( e.KeyCode == Keys.Enter && selection.Count == 1 )
                {
                    e.Handled = true;
                    OpenAction();
                }
                else
                if( e.KeyCode == Keys.Delete )
                {
                    foreach( IResource node in selection )
                    {
                        if( node.Type == "Task" )
                        {
                            return;
                        }
                    }
                    e.Handled = true;
                    new DeAttachResourcesFromTaskInPane().Execute( new ActionContext( ActionContextKind.Other, this, null ) );
                }
                else
                if( e.KeyCode == Keys.Insert && ( selection.Count == 1 ))
                {
                    Core.ResourceAP.RunUniqueJob( new StringDelegate( CreateNewTaskInline ), string.Empty );
                    e.Handled = true;
                }
            }
            else
            if( e.Shift && ( e.KeyCode == Keys.Insert ) && ( selection.Count == 1 ))
            {
                Core.ResourceAP.RunUniqueJob( new ResStringDelegate( CreateNewTaskInList ), selection[ 0 ], string.Empty );
                e.Handled = true;
            }
        }

        private void  CreateNewTaskInline( string name )
        {
            CreateNewTaskInList( null, name );
        }
        private void  CreateNewTaskInList( IResource parent, string name )
        {
            IResource task = NewTaskAction.CreateTask( null, null );
            if( task != null )
            {
                task.EndUpdate();
                if( parent != null )
                {
                    task.SetProp( TasksPlugin._linkSuperTask, parent );
                }

                if( String.IsNullOrEmpty( name ))
                {
                    Core.UserInterfaceAP.QueueJob( new ResourceDelegate( _tasksTree.EditResourceLabel ), task );
                }
                else
                {
                    task.SetProp( Core.Props.Subject, name );
                }
            }
        }
        #endregion Keyboard processing

        #region Decorator
        private class TasksNodeDecorator : IResourceNodeDecorator
        {
            public event ResourceEventHandler DecorationChanged;

            public bool DecorateNode( IResource res, RichText nodeText )
            {
                if( res.Type == "Task" )
                {
                    DateTime dueDate = res.GetDateProp( Core.Props.Date );
                    if( res.GetIntProp( TasksPlugin._propStatus ) == 2 )
                    {
                        nodeText.SetColors( Color.Gray, SystemColors.Window );
                        nodeText.SetStyle( FontStyle.Strikeout, 0, nodeText.Text.Length );
                    }
                    else
                    if( dueDate > DateTime.MinValue && dueDate < DateTime.Now )
                    {
                        nodeText.SetColors( Color.Red, SystemColors.Window );
                        nodeText.SetStyle( FontStyle.Bold, 0, nodeText.Text.Length );
                    }
                    else if( dueDate.Date == DateTime.Today )
                    {
                        nodeText.SetColors( Color.Green, SystemColors.Window );
                    }
                    return true;
                }
                return false;
            }

            public string DecorationKey
            {
                get { return "TaskDueDate"; }
            }
        }
        #endregion Decorator
    }

    #region Filters
    internal class ToDoGenericFilter : IResourceNodeFilter
    {
        internal bool showCompletedTasks;

        public bool AcceptNode( IResource res, int level )
        {
            IResourceList subTasks = res.GetLinksTo( null, TasksPlugin._linkSuperTask );
            if( subTasks.Count > 0 )
            {
                //  For a supertask, it is shown if at least one of its
                //  subtasks is shown as well.

                bool accept = false;
                foreach( IResource subTask in subTasks )
                    accept = accept || AcceptNode( subTask, level + 1 );

                return accept;
            }
            else
            {
                //  Option regulates whether we show completed tasks.
                int  taskStatus = res.GetIntProp( TasksPlugin._propStatus );
                if( res.HasProp( Core.Props.IsDeleted ) ||
                    ( !showCompletedTasks && taskStatus == (int)TaskStatuses.Completed ))
                {
                    return false;
                }

                //  By default we do not show tasks which start date is defined
                //  and will become later.
                DateTime startDate = res.GetDateProp( TasksPlugin._propStartDate );
                if( startDate != DateTime.MinValue && startDate.Date > DateTime.Now.Date )
                {
                    return false;
                }

                //  Accept only those tasks which are defined in the current Wsp.
                IResource workspace = Core.WorkspaceManager.ActiveWorkspace;
                return workspace == null || res.HasLink( "InWorkspace", workspace );
            }
        }
    }
    #endregion Filters

    #region Columns
    /// <summary>
    /// A column in JetListView which supports drawing checkboxes.
    /// </summary>
    internal class LinkedResourcesColumn : JetListViewColumn
	{
        public LinkedResourcesColumn()
        {
            Width = 20;
            FixedSize = true;
            _showHeader = false;
        }

        protected override void DrawItem( Graphics g, Rectangle rc, object item, RowState state, string highlightText )
        {
            IResource res = item as IResource;
            if ( res != null && res.GetLinksTo( null, TasksPlugin._linkTarget ).Count > 0 )
            {
                int midPointX = rc.Left + Width / 2;
                int midPointY = (rc.Top + rc.Bottom) / 2;

                RectangleF rcClip = g.ClipBounds;
                rcClip.Intersect( new RectangleF( rc.Left, rc.Top, rc.Width, rc.Height ) );
                IntPtr hdc = g.GetHdc();
                try
                {
                    IntPtr clipRgn = Win32Declarations.CreateRectRgn( 0, 0, 0, 0 );
                    if ( Win32Declarations.GetClipRgn( hdc, clipRgn ) != 1 )
                    {
                        Win32Declarations.DeleteObject( clipRgn );
                        clipRgn = IntPtr.Zero;
                    }
                    Win32Declarations.IntersectClipRect( hdc, (int) rcClip.Left, (int) rcClip.Top,
                                                              (int) rcClip.Right, (int) rcClip.Bottom );

                    int ildState = ( ( state & RowState.ActiveSelected ) != 0 )
                        ? Win32Declarations.ILD_SELECTED : Win32Declarations.ILD_NORMAL;

                    Win32Declarations.ImageList_Draw( TasksPlugin._ImageList.Handle, 0, hdc,
                                                      midPointX - 8, midPointY - 8, ildState );

                    Win32Declarations.SelectClipRgn( hdc, clipRgn );
                    Win32Declarations.DeleteObject( clipRgn );
                }
                finally
                {
                    g.ReleaseHdc( hdc );
                }
            }
        }

        protected override string GetItemText( object item )
        {
            return "";
        }

        public override string GetToolTip( JetListViewNode node, Rectangle rc, ref bool needPlace )
        {
            return null;
        }
	}
    #endregion Columns
}
