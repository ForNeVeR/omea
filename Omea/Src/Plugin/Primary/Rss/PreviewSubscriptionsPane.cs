/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Summary description for PreviewSubscriptionsPane.
	/// </summary>
    public class PreviewSubscriptionsPane : AbstractOptionsPane
    {

        private ImportManager _manager = null;
        private bool _isBacked = false;

        private ResourceTreeView2 _tvFeeds;
        private System.Windows.Forms.Button _btnSelectAll;
        private System.Windows.Forms.Button _btnUnselectAll;
        private System.Windows.Forms.GroupBox _gbDesc;
        private System.Windows.Forms.TextBox _edtDescription;
        private System.Windows.Forms.Label label1;
        private JetLinkLabel _lblHomepage;

        private IResource _importRoot = null;
        private IResource _finalImportRoot = null;

        
        public bool IsBacked
        {
            set { _isBacked = value; }
        }

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        internal PreviewSubscriptionsPane( ImportManager manager ) : this( manager, RSSPlugin.RootFeedGroup )
        {
        }
            
        internal PreviewSubscriptionsPane( ImportManager manager, IResource importRoot )
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
            _manager = manager;
            _finalImportRoot = importRoot;
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

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._tvFeeds = new JetBrains.Omea.GUIControls.ResourceTreeView2();
            this._btnSelectAll = new System.Windows.Forms.Button();
            this._btnUnselectAll = new System.Windows.Forms.Button();
            this._gbDesc = new System.Windows.Forms.GroupBox();
            this._edtDescription = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this._lblHomepage = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this._gbDesc.SuspendLayout();
            this.SuspendLayout();
            // 
            // _tvFeeds
            // 
            this._tvFeeds.AllowColumnReorder = false;
            this._tvFeeds.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._tvFeeds.CheckBoxes = true;
            this._tvFeeds.CheckedProperty = -1;
            this._tvFeeds.CheckedSetValue = null;
            this._tvFeeds.CheckedUnsetValue = null;
            this._tvFeeds.ColumnSchemeProvider = null;
            this._tvFeeds.ContextProvider = this._tvFeeds;
            this._tvFeeds.DataProvider = null;
            this._tvFeeds.FullRowSelect = false;
            this._tvFeeds.HeaderContextMenu = null;
            this._tvFeeds.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._tvFeeds.InPlaceEdit = true;
            this._tvFeeds.Location = new System.Drawing.Point(8, 8);
            this._tvFeeds.MultiLineView = false;
            this._tvFeeds.MultiSelect = false;
            this._tvFeeds.Name = "_tvFeeds";
            this._tvFeeds.OpenProperty = -1;
            this._tvFeeds.ParentProperty = -1;
            this._tvFeeds.RootResource = null;
            this._tvFeeds.RowDelimiters = false;
            this._tvFeeds.Size = new System.Drawing.Size(280, 280);
            this._tvFeeds.TabIndex = 0;
            this._tvFeeds.ResourceAdded += new JetBrains.Omea.OpenAPI.ResourceEventHandler(this._tvFeeds_ResourceAdded);
            this._tvFeeds.ActiveResourceChanged += new System.EventHandler(this._tvFeeds_AfterSelect);
            // 
            // _btnSelectAll
            // 
            this._btnSelectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnSelectAll.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnSelectAll.Location = new System.Drawing.Point(300, 8);
            this._btnSelectAll.Name = "_btnSelectAll";
            this._btnSelectAll.TabIndex = 1;
            this._btnSelectAll.Size = new System.Drawing.Size(80, 24);
            this._btnSelectAll.Text = "&Select All";
            this._btnSelectAll.Click += new System.EventHandler(this._btnSelectAll_Click);
            // 
            // _btnUnselectAll
            // 
            this._btnUnselectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnUnselectAll.Location = new System.Drawing.Point(300, 38);
            this._btnUnselectAll.Name = "_btnUnselectAll";
            this._btnUnselectAll.TabIndex = 2;
            this._btnUnselectAll.Size = new System.Drawing.Size(80, 24);
            this._btnUnselectAll.Text = "&Unselect All";
            this._btnUnselectAll.Click += new System.EventHandler(this._btnUnselectAll_Click);
            // 
            // _gbDesc
            // 
            this._gbDesc.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._gbDesc.Controls.Add(this._edtDescription);
            this._gbDesc.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._gbDesc.Location = new System.Drawing.Point(8, 304);
            this._gbDesc.Name = "_gbDesc";
            this._gbDesc.Size = new System.Drawing.Size(368, 88);
            this._gbDesc.TabIndex = 3;
            this._gbDesc.TabStop = false;
            this._gbDesc.Text = "Description:";
            // 
            // _edtDescription
            // 
            this._edtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtDescription.Location = new System.Drawing.Point(8, 16);
            this._edtDescription.Multiline = true;
            this._edtDescription.Name = "_edtDescription";
            this._edtDescription.ReadOnly = true;
            this._edtDescription.Size = new System.Drawing.Size(352, 64);
            this._edtDescription.TabIndex = 4;
            this._edtDescription.Text = "";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.Location = new System.Drawing.Point(8, 400);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 23);
            this.label1.TabIndex = 5;
            this.label1.Text = "Homepage:";
            // 
            // _lblHomepage
            // 
            this._lblHomepage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._lblHomepage.Cursor = System.Windows.Forms.Cursors.Hand;
            this._lblHomepage.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(70)), ((System.Byte)(70)), ((System.Byte)(211)));
            this._lblHomepage.Location = new System.Drawing.Point(88, 400);
            this._lblHomepage.Name = "_lblHomepage";
            this._lblHomepage.Size = new System.Drawing.Size(0, 0);
            this._lblHomepage.TabIndex = 6;
            this._lblHomepage.Click += new System.EventHandler(this._lblHomepage_Click);
            // 
            // PreviewSubscriptionsPane
            // 
            this.Controls.Add(this._lblHomepage);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._gbDesc);
            this.Controls.Add(this._btnUnselectAll);
            this.Controls.Add(this._btnSelectAll);
            this.Controls.Add(this._tvFeeds);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "PreviewSubscriptionsPane";
            this.Size = new System.Drawing.Size(384, 424);
            this._gbDesc.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private delegate void ImportJob( IResource root, bool addToWorkspace );
        public override void ShowPane()
        {
            //  OM-13761, -12533: importing during shutdown leads to creation
            //  of illegal proxy resource. Just do nothing if not in "Running" mode.
            if( Core.State == CoreState.Running )
            {
                DoImport();
            }
            base.ShowPane();
        }

        public override void EnterPane()
        {
            DoImport();
            base.EnterPane();
        }

        public override void LeavePane()
        {
            if( _isBacked )
            {
                // Sync
                Core.ResourceAP.RunUniqueJob( new MethodInvoker( DoCancel ) );
                _manager.RepeatImport();
            }
            _isBacked = false;
        }
	
        public override void OK()
        {
            // Really, we should fix import here!
            Core.ResourceAP.QueueJob( new FeedsTreeCommiter.ConfirmImport( FeedsTreeCommiter.DoConfirmImport ),  _importRoot, _finalImportRoot );
        }

        public override void Cancel()
        {
            // Canceled import. Delete all preview tree.
            // Async
            Core.ResourceAP.QueueJob( new MethodInvoker( DoCancel ) );
        }

        internal void DoCancel()
        {
            if( _importRoot != null )
            {
                RemoveFeedsAndGroupsAction.DeleteFeedGroup( _importRoot );
                _importRoot = null;
            }
        }

        private void DoImport()
        {
            if( _importRoot != null )
            {
                return;
            }

            ResourceProxy p = ResourceProxy.BeginNewResource( "RSSFeedGroup" );
            p.EndUpdate();
            _importRoot = p.Resource;
            Core.UIManager.RunWithProgressWindow( ImportManager.ImportPaneName, new ImportJob( _manager.DoImport ), _importRoot, false );
    
            Core.ResourceTreeManager.SetResourceNodeSort( _importRoot, "Type- Name" );
            _tvFeeds.RootResource = _importRoot;
            _tvFeeds.CheckedProperty = Props.Transient;
            _tvFeeds.CheckedSetValue = 0;
            _tvFeeds.CheckedUnsetValue = 1;
            _tvFeeds.ParentProperty = Core.Props.Parent;
            _tvFeeds.OpenProperty = Core.Props.Open;
        }
        
        public void _tvFeeds_AfterSelect( object sender, EventArgs e )
        {
            IResource feed = _tvFeeds.ActiveResource;
            if ( feed != null )
            {
                _edtDescription.Text = feed.GetStringProp( Props.Description );
                _lblHomepage.Text = feed.GetStringProp( Props.HomePage );
            }
            else
            {
                _edtDescription.Text = "";
                _lblHomepage.Text = "";
            }
        }

        public void _tvFeeds_ResourceAdded( object sender, ResourceEventArgs e )
        {
            if ( e.Resource.Type == "RSSFeedGroup" )
            {
                _tvFeeds.SetNodeCheckState( e.Resource, CheckBoxState.Hidden );
            }
            else
            {
                _tvFeeds.SetNodeCheckState( e.Resource, CheckBoxState.Checked );
            }
        }

        private void _lblHomepage_Click( object sender, System.EventArgs e )
        {
			Core.UIManager.OpenInNewBrowserWindow( _lblHomepage.Text );
        }

        private void _btnSelectAll_Click(object sender, System.EventArgs e)
        {
            _tvFeeds.ForEachNode( new ResourceDelegate( CheckResource ) );
        }

        private void _btnUnselectAll_Click(object sender, System.EventArgs e)
        {
            _tvFeeds.ForEachNode( new ResourceDelegate( UncheckResource ) );
        }

        private void CheckResource( IResource res )
        {
            if ( res.Type != "RSSFeedGroup" )
            {
                _tvFeeds.SetNodeCheckState( res, CheckBoxState.Checked );
            }
        }

        private void UncheckResource( IResource res )
        {
            if ( res.Type != "RSSFeedGroup" )
            {
                _tvFeeds.SetNodeCheckState( res, CheckBoxState.Unchecked );
            }
        }
    }

    internal class PreviewSubscriptionsPaneAdapter : OptionsPaneWizardAdapter
    {
        internal PreviewSubscriptionsPaneAdapter( string header, OptionsPaneCreator creator ) : base(header, creator)
        {
        }

        public override void LeavePane( PaneChangeReason reason )
        {
            ((PreviewSubscriptionsPane)Pane).IsBacked = reason == PaneChangeReason.BackPressed;
            base.LeavePane( reason );
        }
    }
}
