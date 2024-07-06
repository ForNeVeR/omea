// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.Net;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.Components.CustomTreeView;

using HandledEventArgs=JetBrains.JetListViewLibrary.HandledEventArgs;

namespace JetBrains.Omea.Nntp
{
    internal class SubscribeForm : DialogBase
    {
        private HelpProvider _helpProvider;
        private Panel _bottomPanel;
        private Button _btnHelp;
        private Label _downloadingLabel;
        private Button _okBtn;
        private Button _cancelBtn;
        private Panel _serversAndGroupsPanel;
        private Splitter splitter1;
        private Panel _serversPanel;
        private CustomStylePanel _groupsPanel;
        private Label _groupsCaptionLabel;
        private Button _propertiesBtn;
        private Button _removeBtn;
        private Button _addBtn;
        private ResourceListView2 _serversView;
        private TabControl _groupTabs;
        private TabPage _allGroupsTabPage;
        private TabPage _subcribedGroupsTabPage;
        private TabPage _newGroupsTabPage;
        private Panel panel1;
        private ProgressBar _groupsDownloadProgress;
        private Button _refreshButton;
        private CheckedListBox _groupsListBox;
        private TextBox _groupName;
        private Label label1;
        private CustomTreeView _groupsTreeView;
        private Label label2;
        private Label _countLabel;
        private Label _serversCaptionLabel;
        private GroupBox _hr;

        private bool _wereChanges;
        private int _lastServerUpdateTick;
        private int _lastProgressUpdateTick;
        private IntHashTable _servers = new IntHashTable();
        private IResourceList _serversList;
        private IResource _server2Select;
        private IResource _lastDisplayedServer;
        private static SubscribeForm _instance = null;
        private static object _syncObject = new object();
        private static IntHashTable _currentConnections = new IntHashTable();
        private static IntHashSet _newServers = new IntHashSet();
        private float _dx;

        private class ServerInfo: IDisposable
        {
            private IResource   _server;
            private string[]    _allGroups;
            private string[]    _newGroups;
            private ArrayList   _subscribedGroups;
            private bool        _newChecked;

            public ServerInfo( IResource server )
            {
                _server = server;
                RefreshNewGroups();
                _subscribedGroups = new ArrayList();
                IStringList subscribed = server.GetStringListProp( NntpPlugin._propSubscribedNewsgroupList );
                for( int i = 0; i < subscribed.Count; ++i )
                {
                    _subscribedGroups.Add( subscribed[ i ] );
                }
                _subscribedGroups.Sort();
            }

            public void RefreshNewGroups()
            {
                _allGroups = GetSortedGroups( NntpPlugin._propNewsgroupList );
                _newGroups = GetSortedGroups( NntpPlugin._propNewNewsgroupList );
            }

            public bool NewChecked
            {
                set { _newChecked = value; }
            }

            /**
             * all returned groups' arrays are sorted
             */
            public string[] AllGroups
            {
                get
                {
                    return _allGroups;
                }
            }

            public string[] SubscribedGroups
            {
                get
                {
                    return (string[]) _subscribedGroups.ToArray( typeof( string ) );
                }
            }

            public string[] NewGroups
            {
                get
                {
                    return _newGroups;
                }
            }

            public int AllGroupsCount
            {
                get { return _server.GetStringListProp( NntpPlugin._propNewsgroupList ).Count; }
            }

            public int SubscribedGroupsCount
            {
                get { return _subscribedGroups.Count; }
            }

            public int NewGroupsCount
            {
                get { return _server.GetStringListProp( NntpPlugin._propNewNewsgroupList ).Count; }
            }

            public bool Contain( string group )
            {
                return Array.BinarySearch( _allGroups, group ) >= 0;
            }

            public bool IsSubscribed( string group )
            {
                return _subscribedGroups.BinarySearch( group ) >= 0;
            }

            public void Subscribe2Group( string group )
            {
                int index = _subscribedGroups.BinarySearch( group );
                if( index < 0 )
                {
                    _subscribedGroups.Insert( ~index, group );
                }
            }

            public void UnsubscribeFromGroup( string group )
            {
                int index = _subscribedGroups.BinarySearch( group );
                if( index >= 0 )
                {
                    _subscribedGroups.RemoveAt( index );
                }
            }

            private string[] GetSortedGroups( int prop )
            {
                IStringList groupList = _server.GetStringListProp( prop );
                string[] result = new string[ groupList.Count ];
                for( int i = 0; i < result.Length; ++i )
                {
                    result[ i ] = groupList[ i ];
                }
                Array.Sort( result );
                return result;
            }

            #region IDisposable Members
            public void Dispose()
            {
                ServerResource serverBO = new ServerResource( _server );
                if( _newChecked )
                {
                    Core.ResourceAP.QueueJob(
                        JobPriority.Immediate, new MethodInvoker( serverBO.ClearNewGroups ) );
                }
                serverBO.DisposeNewsgroupLists();
            }
            #endregion
        }

        private SubscribeForm()
        {
            InitializeComponent();
            Icon = Core.UIManager.ApplicationIcon;

            RestoreSettings();
            _wereChanges = false;
            _groupsTreeView.ThreeStateCheckboxes = true;
            _helpProvider.HelpNamespace = Core.UIManager.HelpFileName;

            int lastSelectedServerId = ObjectStore.ReadInt( "NNTP", "SubscribeForm.LastSelectedServer", 0 );
            if( lastSelectedServerId > 0 )
            {
                _server2Select = Core.ResourceStore.TryLoadResource( lastSelectedServerId );
            }
            int selectedTab = ObjectStore.ReadInt( "NNTP", "SubscribeForm.LastSelectedGroupsTab", 0 );
            if( selectedTab < 0 || selectedTab > 2 )
            {
                selectedTab = 0;
            }
            _groupTabs.SelectedIndex = selectedTab;

            _serversList = Core.ResourceStore.GetAllResourcesLive( NntpPlugin._newsServer );
        }

        protected override void ScaleCore( float dx, float dy )
        {
            base.ScaleCore( dx, dy );
            if( Environment.Version.Major < 2 )
            {
                _dx = dx;
            }
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(SubscribeForm));
            this._helpProvider = new System.Windows.Forms.HelpProvider();
            this._bottomPanel = new System.Windows.Forms.Panel();
            this._hr = new GroupBox();
            this._btnHelp = new System.Windows.Forms.Button();
            this._downloadingLabel = new System.Windows.Forms.Label();
            this._okBtn = new System.Windows.Forms.Button();
            this._cancelBtn = new System.Windows.Forms.Button();
            this._serversAndGroupsPanel = new System.Windows.Forms.Panel();
            this._groupsPanel = new JetBrains.Omea.GUIControls.CustomStylePanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this._countLabel = new System.Windows.Forms.Label();
            this._groupsDownloadProgress = new System.Windows.Forms.ProgressBar();
            this._refreshButton = new System.Windows.Forms.Button();
            this._groupsListBox = new System.Windows.Forms.CheckedListBox();
            this._groupName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this._groupsTreeView = new JetBrains.UI.Components.CustomTreeView.CustomTreeView();
            this.label2 = new System.Windows.Forms.Label();
            this._groupTabs = new System.Windows.Forms.TabControl();
            this._allGroupsTabPage = new System.Windows.Forms.TabPage();
            this._subcribedGroupsTabPage = new System.Windows.Forms.TabPage();
            this._newGroupsTabPage = new System.Windows.Forms.TabPage();
            this._groupsCaptionLabel = new System.Windows.Forms.Label();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this._serversPanel = new System.Windows.Forms.Panel();
            this._serversCaptionLabel = new System.Windows.Forms.Label();
            this._propertiesBtn = new System.Windows.Forms.Button();
            this._removeBtn = new System.Windows.Forms.Button();
            this._addBtn = new System.Windows.Forms.Button();
//            this._serversListView = new JetBrains.Omea.GUIControls.ResourceListView();
            this._serversView = new ResourceListView2();
            this._bottomPanel.SuspendLayout();
            this._serversAndGroupsPanel.SuspendLayout();
            this._groupsPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this._groupTabs.SuspendLayout();
            this._serversPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // _bottomPanel
            //
            this._bottomPanel.Controls.Add(this._hr);
            this._bottomPanel.Controls.Add(this._btnHelp);
            this._bottomPanel.Controls.Add(this._downloadingLabel);
            this._bottomPanel.Controls.Add(this._okBtn);
            this._bottomPanel.Controls.Add(this._cancelBtn);
            this._bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._bottomPanel.Location = new System.Drawing.Point(0, 380);
            this._bottomPanel.Name = "_bottomPanel";
            this._bottomPanel.Size = new System.Drawing.Size(592, 46);
            this._bottomPanel.TabIndex = 9;
            //
            // _hr
            //
            this._hr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._hr.Location = new System.Drawing.Point(0, 0);
            this._hr.Name = "_hr";
            this._hr.Size = new System.Drawing.Size(592, 4);
            this._hr.TabIndex = 15;
            this._hr.TabStop = false;
            //
            // _btnHelp
            //
            this._btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnHelp.Location = new System.Drawing.Point(509, 15);
            this._btnHelp.Name = "_btnHelp";
            this._btnHelp.TabIndex = 11;
            this._btnHelp.Text = "&Help";
            this._btnHelp.Click += new System.EventHandler(this._btnHelp_Click);
            //
            // _downloadingLabel
            //
            this._downloadingLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._downloadingLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._downloadingLabel.Location = new System.Drawing.Point(9, 15);
            this._downloadingLabel.Name = "_downloadingLabel";
            this._downloadingLabel.Size = new System.Drawing.Size(324, 20);
            this._downloadingLabel.TabIndex = 13;
            this._downloadingLabel.Text = "Downloading groups.....";
            this._downloadingLabel.Visible = false;
            //
            // _okBtn
            //
            this._okBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okBtn.Location = new System.Drawing.Point(341, 15);
            this._okBtn.Name = "_okBtn";
            this._okBtn.TabIndex = 9;
            this._okBtn.Text = "OK";
            this._okBtn.Click += new System.EventHandler(this._okBtn_Click);
            //
            // _cancelBtn
            //
            this._cancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelBtn.Location = new System.Drawing.Point(425, 15);
            this._cancelBtn.Name = "_cancelBtn";
            this._cancelBtn.TabIndex = 10;
            this._cancelBtn.Text = "Cancel";
            this._cancelBtn.Click += new System.EventHandler(this._cancelBtn_Click);
            //
            // _serversAndGroupsPanel
            //
            this._serversAndGroupsPanel.Controls.Add(this._groupsPanel);
            this._serversAndGroupsPanel.Controls.Add(this.splitter1);
            this._serversAndGroupsPanel.Controls.Add(this._serversPanel);
            this._serversAndGroupsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._serversAndGroupsPanel.Location = new System.Drawing.Point(0, 0);
            this._serversAndGroupsPanel.Name = "_serversAndGroupsPanel";
            this._serversAndGroupsPanel.Size = new System.Drawing.Size(592, 380);
            this._serversAndGroupsPanel.TabIndex = 10;
            //
            // _groupsPanel
            //
            this._groupsPanel.Controls.Add(this.panel1);
            this._groupsPanel.Controls.Add(this._groupTabs);
            this._groupsPanel.Controls.Add(this._groupsCaptionLabel);
            this._groupsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._groupsPanel.Location = new System.Drawing.Point(266, 0);
            this._groupsPanel.Name = "_groupsPanel";
            this._groupsPanel.Size = new System.Drawing.Size(326, 380);
            this._groupsPanel.TabIndex = 16;
            this._groupsPanel.Paint += new System.Windows.Forms.PaintEventHandler(this._groupsPanel_Paint);
            //
            // panel1
            //
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.Controls.Add(this._countLabel);
            this.panel1.Controls.Add(this._groupsDownloadProgress);
            this.panel1.Controls.Add(this._refreshButton);
            this.panel1.Controls.Add(this._groupsListBox);
            this.panel1.Controls.Add(this._groupName);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this._groupsTreeView);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Location = new System.Drawing.Point(4, 54);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(313, 326);
            this.panel1.TabIndex = 25;
            //
            // _countLabel
            //
            this._countLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._countLabel.Location = new System.Drawing.Point(268, 57);
            this._countLabel.Name = "_countLabel";
            this._countLabel.Size = new System.Drawing.Size(40, 16);
            this._countLabel.TabIndex = 30;
            this._countLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // _groupsDownloadProgress
            //
            this._groupsDownloadProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._groupsDownloadProgress.Location = new System.Drawing.Point(3, 305);
            this._groupsDownloadProgress.Name = "_groupsDownloadProgress";
            this._groupsDownloadProgress.Size = new System.Drawing.Size(226, 12);
            this._groupsDownloadProgress.Step = 1;
            this._groupsDownloadProgress.TabIndex = 29;
            this._groupsDownloadProgress.Visible = false;
            //
            // _refreshButton
            //
            this._refreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._refreshButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._refreshButton.Location = new System.Drawing.Point(238, 297);
            this._refreshButton.Name = "_refreshButton";
            this._refreshButton.TabIndex = 8;
            this._refreshButton.Text = "Re&fresh";
            this._refreshButton.Click += new System.EventHandler(this._refreshButton_Click);
            //
            // _groupsListBox
            //
            this._groupsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._groupsListBox.CheckOnClick = true;
            this._groupsListBox.IntegralHeight = false;
            this._groupsListBox.Location = new System.Drawing.Point(3, 77);
            this._groupsListBox.Name = "_groupsListBox";
            this._groupsListBox.Size = new System.Drawing.Size(309, 214);
            this._groupsListBox.Sorted = true;
            this._groupsListBox.TabIndex = 7;
            this._groupsListBox.Visible = false;
            this._groupsListBox.VisibleChanged += new System.EventHandler(this._groupsListBox_VisibleChanged);
            this._groupsListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this._groupsListBox_ItemCheck);
            //
            // _groupName
            //
            this._groupName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._groupName.Enabled = false;
            this._groupName.Location = new System.Drawing.Point(3, 30);
            this._groupName.Name = "_groupName";
            this._groupName.Size = new System.Drawing.Size(309, 21);
            this._groupName.TabIndex = 5;
            this._groupName.Text = "";
            this._groupName.TextChanged += new System.EventHandler(this._groupName_TextChanged);
            //
            // label1
            //
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(3, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(305, 16);
            this.label1.TabIndex = 27;
            this.label1.Text = "Display newsgroups which contain:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // _groupsTreeView
            //
            this._groupsTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._groupsTreeView.DoubleBuffer = false;
            this._groupsTreeView.ImageIndex = -1;
            this._groupsTreeView.Location = new System.Drawing.Point(3, 77);
            this._groupsTreeView.MultiSelect = false;
            this._groupsTreeView.Name = "_groupsTreeView";
            this._groupsTreeView.NodePainter = null;
            this._groupsTreeView.PathSeparator = ".";
            this._groupsTreeView.SelectedImageIndex = -1;
            this._groupsTreeView.SelectedNodes = new System.Windows.Forms.TreeNode[0];
            this._groupsTreeView.Size = new System.Drawing.Size(309, 214);
            this._groupsTreeView.Sorted = true;
            this._groupsTreeView.TabIndex = 6;
            this._groupsTreeView.ThreeStateCheckboxes = false;
            this._groupsTreeView.AfterThreeStateCheck += new JetBrains.UI.Components.CustomTreeView.ThreeStateCheckEventHandler(this._groupsTreeView_AfterThreeStateCheck);
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(3, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(225, 16);
            this.label2.TabIndex = 26;
            this.label2.Text = "Check newsgroups to subscribe:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            //
            // _groupTabs
            //
            this._groupTabs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._groupTabs.Controls.Add(this._allGroupsTabPage);
            this._groupTabs.Controls.Add(this._subcribedGroupsTabPage);
            this._groupTabs.Controls.Add(this._newGroupsTabPage);
            this._groupTabs.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._groupTabs.Location = new System.Drawing.Point(4, 32);
            this._groupTabs.Multiline = true;
            this._groupTabs.Name = "_groupTabs";
            this._groupTabs.SelectedIndex = 0;
            this._groupTabs.Size = new System.Drawing.Size(313, 21);
            this._groupTabs.SizeMode = System.Windows.Forms.TabSizeMode.FillToRight;
            this._groupTabs.TabIndex = 4;
            this._groupTabs.SelectedIndexChanged += new System.EventHandler(this._groupTabs_SelectedIndexChanged);
            //
            // _allGroupsTabPage
            //
            this._allGroupsTabPage.Location = new System.Drawing.Point(4, 22);
            this._allGroupsTabPage.Name = "_allGroupsTabPage";
            this._allGroupsTabPage.Size = new System.Drawing.Size(305, 0);
            this._allGroupsTabPage.TabIndex = 0;
            this._allGroupsTabPage.Text = "All";
            //
            // _subcribedGroupsTabPage
            //
            this._subcribedGroupsTabPage.Location = new System.Drawing.Point(4, 22);
            this._subcribedGroupsTabPage.Name = "_subcribedGroupsTabPage";
            this._subcribedGroupsTabPage.Size = new System.Drawing.Size(305, -5);
            this._subcribedGroupsTabPage.TabIndex = 1;
            this._subcribedGroupsTabPage.Text = "Subscribed";
            //
            // _newGroupsTabPage
            //
            this._newGroupsTabPage.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._newGroupsTabPage.Location = new System.Drawing.Point(4, 22);
            this._newGroupsTabPage.Name = "_newGroupsTabPage";
            this._newGroupsTabPage.Size = new System.Drawing.Size(305, -5);
            this._newGroupsTabPage.TabIndex = 2;
            this._newGroupsTabPage.Text = "New";
            //
            // _groupsCaptionLabel
            //
            this._groupsCaptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._groupsCaptionLabel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this._groupsCaptionLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this._groupsCaptionLabel.Location = new System.Drawing.Point(4, 8);
            this._groupsCaptionLabel.Name = "_groupsCaptionLabel";
            this._groupsCaptionLabel.Size = new System.Drawing.Size(313, 16);
            this._groupsCaptionLabel.TabIndex = 22;
            this._groupsCaptionLabel.Text = "Newsgroups";
            this._groupsCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // splitter1
            //
            this.splitter1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.splitter1.Location = new System.Drawing.Point(262, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(4, 380);
            this.splitter1.TabIndex = 15;
            this.splitter1.TabStop = false;
            //
            // _serversPanel
            //
            this._serversPanel.Controls.Add(this._serversCaptionLabel);
            this._serversPanel.Controls.Add(this._propertiesBtn);
            this._serversPanel.Controls.Add(this._removeBtn);
            this._serversPanel.Controls.Add(this._addBtn);
            this._serversPanel.Controls.Add(this._serversView);
            this._serversPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this._serversPanel.Location = new System.Drawing.Point(0, 0);
            this._serversPanel.Name = "_serversPanel";
            this._serversPanel.Size = new System.Drawing.Size(262, 380);
            this._serversPanel.TabIndex = 0;
            this._serversPanel.Resize += new System.EventHandler(this._serversPanel_Resize);
            //
            // _serversCaptionLabel
            //
            this._serversCaptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._serversCaptionLabel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this._serversCaptionLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this._serversCaptionLabel.Location = new System.Drawing.Point(8, 8);
            this._serversCaptionLabel.Name = "_serversCaptionLabel";
            this._serversCaptionLabel.Size = new System.Drawing.Size(254, 16);
            this._serversCaptionLabel.TabIndex = 4;
            this._serversCaptionLabel.Text = "News Servers";
            this._serversCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // _propertiesBtn
            //
            this._propertiesBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._propertiesBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._propertiesBtn.Location = new System.Drawing.Point(88, 352);
            this._propertiesBtn.Name = "_propertiesBtn";
            this._propertiesBtn.Size = new System.Drawing.Size(88, 23);
            this._propertiesBtn.TabIndex = 2;
            this._propertiesBtn.Text = "&Properties...";
            this._propertiesBtn.Click += new System.EventHandler(this._propertiesBtn_Click);
            //
            // _removeBtn
            //
            this._removeBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._removeBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._removeBtn.Location = new System.Drawing.Point(180, 352);
            this._removeBtn.Name = "_removeBtn";
            this._removeBtn.Size = new System.Drawing.Size(76, 23);
            this._removeBtn.TabIndex = 3;
            this._removeBtn.Text = "&Remove";
            this._removeBtn.Click += new System.EventHandler(this._removeBtn_Click);
            //
            // _addBtn
            //
            this._addBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._addBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._addBtn.Location = new System.Drawing.Point(8, 352);
            this._addBtn.Name = "_addBtn";
            this._addBtn.Size = new System.Drawing.Size(76, 23);
            this._addBtn.TabIndex = 1;
            this._addBtn.Text = "&Add...";
            this._addBtn.Click += new System.EventHandler(this._addBtn_Click);
            //
            // _serversListView
            //
            this._serversView.AllowDrop = true;
            this._serversView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._serversView.EmptyText = "You do not have any news subscriptions";
            this._serversView.ExecuteDoubleClickAction = false;
            this._serversView.FullRowSelect = true;
            this._serversView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._serversView.HideSelection = false;
            this._serversView.Location = new System.Drawing.Point(8, 32);
            this._serversView.MultiSelect = false;
            this._serversView.Name = "_serversView";
            this._serversView.ShowContextMenu = false;
            this._serversView.Size = new System.Drawing.Size(253, 312);
            this._serversView.TabIndex = 0;
            this._serversView.SelectionChanged += new EventHandler(this._serversListView_SelectedIndexChanged);
//            this._serversView.Resize += new System.EventHandler(this._serversListView_Resize);
            this._serversView.DoubleClick += new JetBrains.JetListViewLibrary.HandledEventHandler( this._serversListView_DoubleClick );
            this._serversView.KeyDown += new System.Windows.Forms.KeyEventHandler(this._serversListView_KeyDown);
            //
            // SubscribeForm
            //
            this.AcceptButton = _okBtn;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = _cancelBtn;
            this.ClientSize = new System.Drawing.Size(592, 426);
            this.Controls.Add(this._serversAndGroupsPanel);
            this.Controls.Add(this._bottomPanel);
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new System.Drawing.Size(600, 300);
            this.Name = "SubscribeForm";
            this.ShowInTaskbar = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Manage Newsgroups";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.SubscribeForm_Closing);
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.SubscribeForm_HelpRequested);
            this._bottomPanel.ResumeLayout(false);
            this._serversAndGroupsPanel.ResumeLayout(false);
            this._groupsPanel.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this._groupTabs.ResumeLayout(false);
            this._serversPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion

        private void Display()
        {
            _propertiesBtn.Enabled = _removeBtn.Enabled = false;
            InitializeColumns();
            lock( _serversList )
            {
                foreach( IResource res in _serversList )
                {
                    _serversView.JetListView.Nodes.Add( res );
                }
                _serversList.ResourceAdded += new ResourceIndexEventHandler( servers_ResourceAdded );
                _serversList.ResourceDeleting += new ResourceIndexEventHandler( servers_ResourceDeleting );
                _serversList.ResourceChanged += new ResourcePropIndexEventHandler( servers_ResourceChanged );
                foreach( IResource server in _serversList )
                {
                    _servers[ server.Id ] = new ServerInfo( server );
                }
                if( _serversList.Count > 0 )
                {
                    _serversView.Selection.Add( ( _server2Select != null ) ? _server2Select : _serversList[ 0 ] );
                }
            }
            _serversView.SelectAddedItems = true;
            _serversPanel.Width = ObjectStore.ReadInt( "NNTP", "SubscribeForm.serversPanelWidth", 262 );
            Show();
        }

        private void  InitializeColumns()
        {
            _serversView.Columns.Add( new ResourceIconColumn() );
            ResourceListView2Column nameCol = _serversView.AddColumn( ResourceProps.DisplayName );
            nameCol.AutoSize = true;
        }

        public static void SubscribeToGroups()
        {
            Cursor current = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                lock( _syncObject )
                {
                    if( _instance != null )
                    {
                        _instance.Activate();
                    }
                    else
                    {
                        _instance = new SubscribeForm();
                        _instance.Display();
                    }
                }
            }
            finally
            {
                Cursor.Current = current;
            }
        }

        public static void SubscribeToGroups( IResource server2Select )
        {
            Cursor current = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                lock( _syncObject )
                {
                    if( _instance != null )
                    {
                        _instance.Activate();
                    }
                    else
                    {
                        _instance = new SubscribeForm();
                        _instance._server2Select = server2Select;
                        _instance.Display();
                    }
                }
            }
            finally
            {
                Cursor.Current = current;
            }
        }

        #region news servers live list handlers

        private void servers_ResourceAdded( object sender, ResourceIndexEventArgs e )
        {
            IResource server = e.Resource;
            _servers[ server.Id ] = new ServerInfo( server );
            _serversView.JetListView.Nodes.Add( server );
        }

        private void servers_ResourceDeleting( object sender, ResourceIndexEventArgs e )
        {
            _servers.Remove( e.Resource.Id );
            _serversView.JetListView.Nodes.Remove( e.Resource );
        }

        private void servers_ResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if( e.ChangeSet.IsPropertyChanged( NntpPlugin._propNewsgroupList ) ||
                e.ChangeSet.IsPropertyChanged( NntpPlugin._propNewNewsgroupList ) ||
                e.ChangeSet.IsPropertyChanged( NntpPlugin._propSubscribedNewsgroupList ) )
            {
                if( Math.Abs( Environment.TickCount - _lastServerUpdateTick ) > 1000 )
                {
                    _lastServerUpdateTick = Environment.TickCount;
                    DisplaySelectedServerInfo();
                }
            }
        }

        #endregion

        #region form event handlers

        private void _serversListView_SelectedIndexChanged( object sender, EventArgs e )
        {
            DisplaySelectedServerInfo();
        }

        private void _serversListView_KeyDown(object sender, KeyEventArgs e)
        {
            if( e.KeyCode == Keys.Delete && !e.Control && !e.Shift && !e.Alt )
            {
                e.Handled = true;
                _removeBtn_Click( this, EventArgs.Empty );
            }
        }

        private void _serversListView_DoubleClick(object sender, HandledEventArgs e)
        {
            _propertiesBtn_Click( sender, e );
        }

        private void _okBtn_Click( object sender, EventArgs e )
        {
            if( _wereChanges )
            {
                _okBtn.Enabled = false;
                Core.ResourceAP.RunUniqueJob( new MethodInvoker( UpdateSubscriptions ) );
            }
            Close();
        }

        private void _cancelBtn_Click(object sender, EventArgs e)
        {
            foreach( IntHashTable.Entry entry in _servers )
            {
                ServerInfo serverInfo = (ServerInfo) entry.Value;
                serverInfo.NewChecked = false;
            }
            Close();
        }

        private void _groupsTreeView_AfterThreeStateCheck(object sender, ThreeStateCheckEventArgs e)
        {
            IResource server = GetSelectedServer();
            if( server != null )
            {
                ServerInfo serverInfo = (ServerInfo) _servers[ server.Id ];
                if( serverInfo != null )
                {
                    string group = (string) e.Node.Tag;
                    if( group != null )
                    {
                        _wereChanges = true;
                        if( e.CheckState == NodeCheckState.Checked )
                        {
                            serverInfo.Subscribe2Group( group );
                        }
                        else
                        {
                            serverInfo.UnsubscribeFromGroup( group );
                        }
                    }
                }
            }
        }

        private void _groupsListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            IResource server = GetSelectedServer();
            if( server != null )
            {
                ServerInfo serverInfo = (ServerInfo) _servers[ server.Id ];
                if( serverInfo != null )
                {
                    if( _groupsListBox.SelectedIndex  < 0 )
                    {
                        return;
                    }
                    _wereChanges = true;
                    string group = _groupsListBox.SelectedItem.ToString();
                    int displayNameIndex = group.IndexOf( " (" );
                    if( displayNameIndex > 0 )
                    {
                        group = group.Substring( 0, displayNameIndex );
                    }
                    if( e.NewValue == CheckState.Checked )
                    {
                        serverInfo.Subscribe2Group( group );
                    }
                    else
                    {
                        serverInfo.UnsubscribeFromGroup( group );
                    }
                    AddGroupToTree( group );
                }
            }
        }

        private void _addBtn_Click(object sender, EventArgs e)
        {
            EditServerForm form = EditServerForm.CreateNewServerPropertiesForm( string.Empty, 119 );
            form.ShowDialog( this );
            _serversView.Focus();
            if( form.DialogResult == DialogResult.OK )
            {
                InitConnection2Server( form.Servers[0], true );
            }
        }

        private void _propertiesBtn_Click(object sender, EventArgs e)
        {
            IResource server = GetSelectedServer();
            if( server != null )
            {
                EditServerForm form = EditServerForm.CreateServerPropertiesForm( server.ToResourceList() );
                form.ShowDialog( this );
                _serversView.Focus();
                if( form.DialogResult == DialogResult.OK )
                {
                    InitConnection2Server( form.Servers[0], false );
                }
            }
        }

        private void _removeBtn_Click(object sender, EventArgs e)
        {
            IResource server = GetSelectedServer();
            if( server != null )
            {
                DeleteNewsServerAction.WarnAndDelete( server, this );
                _serversView.Focus();
            }
        }

        private void _groupName_TextChanged(object sender, EventArgs e)
        {
            Core.UserInterfaceAP.QueueJobAt(
                DateTime.Now.AddSeconds( 0.5 ), new MethodInvoker( DisplayGroups ) );
        }

        private void _groupsListBox_VisibleChanged(object sender, EventArgs e)
        {
            _groupsTreeView.Visible = !_groupsListBox.Visible;
        }

        private void _refreshButton_Click(object sender, EventArgs e)
        {
            IResource server = GetSelectedServer();
            if( server != null )
            {
                InitConnection2Server( server, false );
            }
        }

        private void _serversPanel_Resize(object sender, EventArgs e)
        {
            Core.UserInterfaceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( ResizeLayout ) );
        }

        private void _groupTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            _refreshButton.Visible = _groupTabs.SelectedIndex == 0;
            _groupsDownloadProgress.Visible = _refreshButton.Visible && !_refreshButton.Enabled;
            if( _refreshButton.Visible )
            {
                _groupsListBox.Height = _groupsTreeView.Height = _refreshButton.Top - _groupsListBox.Top - 8;
            }
            else
            {
                _groupsListBox.Height = _groupsTreeView.Height = _refreshButton.Top - _groupsListBox.Top + _refreshButton.Height;
            }
            DisplayGroups();
        }

        private void _groupsPanel_Paint( object sender, PaintEventArgs e )
        {
            Pen apen = new Pen( SystemColors.ActiveCaption, 1 );
            using( apen )
            {
                e.Graphics.DrawRectangle( apen, 0, -1, _groupsPanel.Width, _groupsPanel.Height + 1 );
            }
        }

        private void SubscribeForm_Closing(object sender, CancelEventArgs e)
        {
            lock( _syncObject )
            {
                _instance = null;
            }
            IResource server = GetSelectedServer();
            if( server != null )
            {
                server.Lock();
                try
                {
                    if( server.IsDeleted || server.IsDeleting )
                    {
                        server = null;
                    }
                    else
                    {
                        ObjectStore.WriteInt( "NNTP", "SubscribeForm.LastSelectedServer", server.Id );
                    }
                }
                finally
                {
                    server.UnLock();
                }
            }
            if( server == null )
            {
                ObjectStore.WriteInt( "NNTP", "SubscribeForm.LastSelectedServer", 0 );
            }
            ObjectStore.WriteInt( "NNTP", "SubscribeForm.LastSelectedGroupsTab", _groupTabs.SelectedIndex );
            int w = _serversPanel.Width;
            if( _dx != 0 )
            {
                w = (int) ( w / _dx );
            }
            ObjectStore.WriteInt( "NNTP", "SubscribeForm.serversPanelWidth", w );
            _serversList.Dispose();
            foreach( IntHashTable.Entry entry in _servers )
            {
                ( (ServerInfo) entry.Value ).Dispose();
            }
        }

        #endregion

        private void ResizeLayout()
        {
            if( _groupsPanel.Width < 240 )
            {
                _serversPanel.Width = _serversPanel.Width + _groupsPanel.Width - 240;
            }
            if( _serversPanel.Width < 262 )
            {
                _serversPanel.Width = 262;
            }
        }

        private void ForceDisplaySelectedServerInfo()
        {
            if( ReferenceEquals( this, _instance ) )
            {
                if( !Core.UserInterfaceAP.IsOwnerThread )
                {
                    Core.UserInterfaceAP.QueueJob( new MethodInvoker( ForceDisplaySelectedServerInfo ) );
                }
                else
                {
                    _lastDisplayedServer = null;
                    DisplaySelectedServerInfo();
                }
            }
        }

        private void DisplaySelectedServerInfo()
        {
            if( ReferenceEquals( this, _instance ) )
            {
                if( !Core.UserInterfaceAP.IsOwnerThread )
                {
                    Core.UserInterfaceAP.QueueJob( new MethodInvoker( DisplaySelectedServerInfo ) );
                }
                else
                {
                    IResource server = GetSelectedServer();
                    int allCount = 0;
                    int subscribeCount = 0;
                    int newCount = 0;
                    string caption = "Newsgroups";
                    if( server != null )
                    {
                        ServerInfo serverInfo = (ServerInfo) _servers[ server.Id ];
                        if( serverInfo != null )
                        {
                            caption = "Newsgroups on " + server.DisplayName;
                            allCount = serverInfo.AllGroupsCount;
                            subscribeCount = serverInfo.SubscribedGroupsCount;
                            newCount = serverInfo.NewGroupsCount;
                        }
                    }
                    _countLabel.Text = string.Empty;
                    _allGroupsTabPage.Text = "All (" + allCount + ")";
                    _subcribedGroupsTabPage.Text = ( subscribeCount > 0 ) ? "Subscribed (" + subscribeCount + ")" : "Subscribed";
                    _newGroupsTabPage.Text = ( newCount > 0 ) ? "New (" + newCount + ")" : "New";
                    _groupsCaptionLabel.Text = caption;
                    if( _lastDisplayedServer != server || _groupName.Text.Trim().Length > 0 )
                    {
                        DisplayGroups();
                        _lastDisplayedServer = server;
                    }
                }
            }
        }

        private void AddGroupToTree( string groupName )
        {
            IResource server = GetSelectedServer();
            if( server != null )
            {
                ServerInfo serverInfo = (ServerInfo) _servers[ server.Id ];
                // if current server contains the group
                if( serverInfo.Contain( groupName ) )
                {
                    TreeNodeCollection nodes = _groupsTreeView.Nodes;
                    string[] nameparts = groupName.Split( '.' );
                    TreeNode aNode = null;
                    for( int i = 0; i < nameparts.Length; ++i )
                    {
                        string namePart = nameparts[ i ];
                        aNode = null;
                        if( nodes.Count > 0 )
                        {
                            if( !_groupsTreeView.Sorted )
                            {
                                foreach( TreeNode node in nodes )
                                {
                                    if( node.Text.CompareTo( namePart ) == 0 )
                                    {
                                        aNode = node;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                int l = 0;
                                int h = nodes.Count;
                                while( l < h )
                                {
                                    int m = ( l + h ) >> 1;
                                    TreeNode node = nodes[ m ];
                                    int compare = node.Text.CompareTo( namePart );
                                    if( compare == 0 )
                                    {
                                        aNode = node;
                                        break;
                                    }
                                    if( compare < 0 )
                                    {
                                        l = m + 1;
                                    }
                                    else
                                    {
                                        h = m;
                                    }
                                }
                            }
                        }
                        if( aNode == null )
                        {
                            aNode = new TreeNode();
                            aNode.Text = nameparts[ i ];
                            nodes.Add( aNode );
                            _groupsTreeView.SetNodeCheckState( aNode, NodeCheckState.None );
                        }
                        nodes = aNode.Nodes;
                    }
                    if( aNode != null )
                    {
                        aNode.Tag = groupName;
                        NodeCheckState check = ( serverInfo.IsSubscribed( groupName ) )
                            ? NodeCheckState.Checked : NodeCheckState.Unchecked;
                        _groupsTreeView.SetNodeCheckState( aNode, check );
                    }
                }
            }
        }

        private void UpdateSubscriptions()
        {
//            IResourceList servers = _serversView.ResourceList;
            IResourceList servers = _serversList;
            if( servers != null )
            {
                lock( servers )
                {
                    foreach( IResource server in servers )
                    {
                        if( !server.IsDeleted )
                        {
                            bool needUpdate = false;
                            ServerInfo serverInfo = (ServerInfo) _servers[ server.Id ];
                            IResourceList groups = new NewsTreeNode( server ).Groups;
                            foreach( IResource group in groups )
                            {
                                string name = group.GetPropText( Core.Props.Name );
                                if( !serverInfo.IsSubscribed( name ) )
                                {
                                    UnsubscribeAction.Unsubscribe( group, false );
                                }
                            }
                            string[] subscribed = serverInfo.SubscribedGroups;
                            foreach( string group in subscribed )
                            {
                                needUpdate = NntpPlugin.Subscribe2Group( group, server ) || needUpdate;
                            }
                            if( needUpdate )
                            {
                                NntpClientHelper.DeliverNewsFromServer( server, null, true, null );
                            }
                        }
                    }
                }
            }
        }

        private void ReDrawTree()
        {
            Cursor current = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            _groupsTreeView.BeginUpdate();
            try
            {
                _groupsTreeView.Nodes.Clear();
                IResource server = GetSelectedServer();
                if( server == null )
                {
                    _propertiesBtn.Enabled = _removeBtn.Enabled = _groupName.Enabled = false;
                    return;
                }
                _propertiesBtn.Enabled = _removeBtn.Enabled = _groupName.Enabled = true;
                ServerInfo serverInfo = (ServerInfo) _servers[ server.Id ];
                if( serverInfo != null )
                {
                    serverInfo.RefreshNewGroups();
                    string[] groups = GetSelectedGroups( serverInfo );
                    for( int i = groups.Length; i > 0; )
                    {
                        string groupName = groups[ --i ];
                        string[] nameparts = groupName.Split( '.' );
                        TreeNodeCollection nodes = _groupsTreeView.Nodes;
                        TreeNode aNode = null;
                        foreach( string namepart in nameparts )
                        {
                            int nodesCount = nodes.Count;
                            if( nodesCount > 0 && String.Compare( namepart, nodes[ 0 ].Text, true ) == 0 )
                            {
                                aNode = nodes[ 0 ];
                            }
                            else
                            {
                                aNode = new TreeNode();
                                aNode.Text = namepart;
                                nodes.Insert( 0, aNode );
                                _groupsTreeView.SetNodeCheckState( aNode, NodeCheckState.None );
                            }
                            nodes = aNode.Nodes;
                        }
                        if( aNode != null )
                        {
                            aNode.Tag = groupName;
                            NodeCheckState check = ( serverInfo.IsSubscribed( groupName ) )
                                ? NodeCheckState.Checked : NodeCheckState.Unchecked;
                            _groupsTreeView.SetNodeCheckState( aNode, check );
                        }
                    }
                }
            }
            finally
            {
                _groupsTreeView.EndUpdate();
            }
            Cursor.Current = current;
            if( _groupTabs.SelectedIndex == 1 )
            {
                _groupsTreeView.ExpandAll();
            }
        }

        private void DisplayGroups()
        {
            SetProgress();
            string trimmedGroupName = _groupName.Text.Trim();
            if( trimmedGroupName.Length == 0 )
            {
                _groupsListBox.Visible = false;
                ReDrawTree();
            }
            else
            {
                string[] substrs = trimmedGroupName.Split( ' ' );
                Cursor current = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                _groupsListBox.BeginUpdate();
                _groupsListBox.Items.Clear();
                _groupsListBox.Sorted = false;
                try
                {
                    IResource server = GetSelectedServer();
                    if( server != null )
                    {
                        ServerInfo serverInfo = (ServerInfo) _servers[ server.Id ];
                        serverInfo.RefreshNewGroups();
                        ServerResource serverRes = new ServerResource( server );
                        string[] groups = GetSelectedGroups( serverInfo );
                        foreach( string group in groups )
                        {
                            bool add = true;
                            for( int i = 0; i < substrs.Length; ++i )
                            {
                                if( substrs[ i ].Length > 0 && group.IndexOf( substrs[ i ] ) < 0 )
                                {
                                    add = false;
                                    break;
                                }
                            }
                            if( add )
                            {
                                _groupsListBox.Items.Add( group, serverInfo.IsSubscribed( group ) );
                            }
                        }
                        IResourceList subscribedGroups = serverRes.Groups;
                        foreach( IResource res in subscribedGroups.ValidResources )
                        {
                            bool add = true;
                            string name = res.DisplayName.Replace( " (unsubscribed)", string.Empty );
                            for( int i = 0; i < substrs.Length; ++i )
                            {
                                if( substrs[ i ].Length > 0 && name.IndexOf( substrs[ i ] ) < 0 )
                                {
                                    add = false;
                                    break;
                                }
                            }
                            if( add )
                            {
                                string group = new NewsgroupResource( res ).Name;
                                if( _groupsListBox.Items.IndexOf( group ) < 0 )
                                {
                                    _groupsListBox.Items.Add( group + " (" + name + ")", serverInfo.IsSubscribed( group ) );
                                }
                            }
                        }
                        _countLabel.Text = _groupsListBox.Items.Count.ToString();
                        _groupsListBox.Visible = true;
                    }
                }
                finally
                {
                    _groupsListBox.Sorted = true;
                    _groupsListBox.EndUpdate();
                    Cursor.Current = current;
                }
            }
        }

        private IResource GetSelectedServer()
        {
            IResourceList servers = _serversView.GetSelectedResources();
            return ( servers.Count == 0 ) ? null : servers[ 0 ];
        }

        private string[] GetSelectedGroups( ServerInfo serverInfo )
        {
            string[] result = null;
            switch( _groupTabs.SelectedIndex )
            {
                case 0: result = serverInfo.AllGroups; break;
                case 1: result = serverInfo.SubscribedGroups; break;
                case 2: result = serverInfo.NewGroups; serverInfo.NewChecked = true; break;
            }
            _countLabel.Text = ( result == null ) ? "0" : result.Length.ToString();
            return result;
        }

        private void InitConnection2Server( IResource server, bool newServer )
        {
            NntpConnectionPool.CloseConnections( server );
            NntpConnection connection = NntpConnectionPool.GetConnection( server, "foreground" );
            if( connection != null )
            {
                lock( _currentConnections )
                {
                    _currentConnections[ server.Id ] = connection;
                }
                if( newServer )
                {
                    lock( _newServers )
                    {
                        _newServers.Add( server.Id );
                    }
                }
                NntpDownloadGroupsUnit downloadGroupsUnit =
                    new NntpDownloadGroupsUnit( server, true, JobPriority.Immediate );
                downloadGroupsUnit.Finished += new AsciiProtocolUnitDelegate( downloadGroupsUnit_Finished );
                downloadGroupsUnit.GroupDownloaded +=
                    new NntpDownloadGroupsUnit.DroupDownloadedDelegate( downloadGroupsUnit_GroupDownloaded );
                connection.StartUnit( Int32.MaxValue - 2, downloadGroupsUnit );
                SetProgress();
            }
        }

        private void downloadGroupsUnit_Finished( AsciiProtocolUnit unit )
        {
            NntpDownloadGroupsUnit downloadGroupsUnit = (NntpDownloadGroupsUnit) unit;
            IResource server = downloadGroupsUnit.Server.Resource;
            lock( _currentConnections )
            {
                _currentConnections.Remove( server.Id );
            }
            lock( _newServers )
            {
                _newServers.Remove( server.Id );
            }
            // marshal forcing redisplay through resource thread to make it after group lists have updated
            Core.ResourceAP.QueueJob( JobPriority.Lowest, new MethodInvoker( ForceDisplaySelectedServerInfo ) );
            SetProgress();
        }

        private void downloadGroupsUnit_GroupDownloaded( string group, NntpDownloadGroupsUnit unit )
        {
            if( !Core.UserInterfaceAP.IsOwnerThread )
            {
                if( Math.Abs( Environment.TickCount - _lastProgressUpdateTick ) > 1000 )
                {
                    _lastProgressUpdateTick = Environment.TickCount;
                    Core.UserInterfaceAP.QueueJob(
                        JobPriority.AboveNormal,
                        new NntpDownloadGroupsUnit.DroupDownloadedDelegate( downloadGroupsUnit_GroupDownloaded ),
                        group, unit );
                }
            }
            else
            {
                IResource server = GetSelectedServer();
                if( unit.Server.Resource == server )
                {
                    ServerInfo serverInfo = (ServerInfo) _servers[ server.Id ];
                    if( serverInfo != null )
                    {
                        _groupsDownloadProgress.Maximum = serverInfo.AllGroupsCount;
                        if( unit.Count < _groupsDownloadProgress.Maximum )
                        {
                            _groupsDownloadProgress.Value = unit.Count;
                        }
                        else
                        {
                            _groupsDownloadProgress.Value = _groupsDownloadProgress.Maximum;
                        }
                        string text = "Downloading groups (" + unit.Count + ").";
                        for( int i = 0, count = DateTime.Now.Second & 3; i < count; ++i )
                        {
                            text += '.';
                        }
                        _downloadingLabel.Text = text;
                    }
                }
            }
        }

        private void SetProgress()
        {
            if( !Core.UserInterfaceAP.IsOwnerThread )
            {
                Core.UserInterfaceAP.QueueJob( new MethodInvoker( SetProgress ) );
            }
            else
            {
                IResource server = GetSelectedServer();
                bool hasConnection = ( server != null );
                bool isNewServer = true;
                if( hasConnection )
                {
                    lock( _currentConnections )
                    {
                        hasConnection = _currentConnections.Contains( server.Id );
                    }
                    lock( _newServers )
                    {
                        isNewServer = _newServers.Contains( server.Id );
                    }
                }
                if( !hasConnection )
                {
                    _downloadingLabel.Text = string.Empty;
                    _groupsDownloadProgress.Value = _groupsDownloadProgress.Minimum;
                }
                _downloadingLabel.Visible = hasConnection;
                _refreshButton.Enabled = !hasConnection;
                _groupsDownloadProgress.Visible = hasConnection && !isNewServer && _groupTabs.SelectedIndex == 0;
            }
        }

        private void _btnHelp_Click( object sender, EventArgs e )
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "/reference/manage_newsgroups.html" );
        }

        private void SubscribeForm_HelpRequested( object sender, HelpEventArgs hlpevent )
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "/reference/manage_newsgroups.html" );
        }
    }
}
