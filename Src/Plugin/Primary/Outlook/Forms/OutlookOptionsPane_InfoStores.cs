// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
	/// <summary>
	/// Options pane for including and excluding info stores from indexing.
	/// </summary>
	public class OutlookOptionsPane_InfoStores : AbstractOptionsPane
	{
        private Label               label1;
        private Label               labelRemote;
        private ResourceListView2   _lvInfoStores;
        private CheckBoxColumn      _checkBoxColumn;
        private IResourceList       _allStores;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private bool _savingInfoStores = false;

		public OutlookOptionsPane_InfoStores()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
            _allStores = Core.ResourceStore.GetAllResources( STR.MAPIInfoStore );
            IResourceList supportedStores = Core.ResourceStore.FindResourcesWithProp( STR.MAPIInfoStore, PROP.StoreSupported );
            IResourceList unsupportedStores = Core.ResourceStore.GetAllResources( STR.MAPIInfoStore ).Minus( supportedStores );
            if ( unsupportedStores.Count > 0 )
		    {
                label1.Text += " Please note that Omea does not support IMAP message stores.";
            }

            _checkBoxColumn = _lvInfoStores.AddCheckBoxColumn();
            _lvInfoStores.AllowColumnReorder = false;
            _lvInfoStores.DataProvider = new DProvider( _allStores );
            _lvInfoStores.Columns.Add( new ResourceIconColumn() );
            ResourceListView2Column nameCol = _lvInfoStores.AddColumn( Core.Props.Name );
            nameCol.AutoSize = true;
            _checkBoxColumn.AfterCheck += HandleAfterCheck;
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
            this.label1 = new System.Windows.Forms.Label();
            this.labelRemote = new System.Windows.Forms.Label();
            this._lvInfoStores = new ResourceListView2(); //JetBrains.Omea.GUIControls.ResourceListView();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(276, 55);
            this.label1.TabIndex = 4;
            this.label1.Text = "Select the information stores which you would like to access from Omea.";
            //
            // _lvInfoStores
            //
            this._lvInfoStores.AllowDrop = false;
            this._lvInfoStores.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lvInfoStores.Location = new System.Drawing.Point(0, 55);
            this._lvInfoStores.Name = "_lvInfoStores";
            this._lvInfoStores.Size = new System.Drawing.Size(276, 130);
            this._lvInfoStores.TabIndex = 5;
            this._lvInfoStores.HideSelection = false;
            this._lvInfoStores.BorderStyle = BorderStyle.Fixed3D;
            this._lvInfoStores.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            //
            // labelRemote
            //
            this.labelRemote.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelRemote.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelRemote.Location = new System.Drawing.Point(0, 185);
            this.labelRemote.Name = "labelRemote";
            this.labelRemote.ForeColor = Color.Red;
            this.labelRemote.Size = new System.Drawing.Size(276, 75);
            this.labelRemote.TabIndex = 6;
            this.labelRemote.Text = "IMPORTANT! Inclusion of remote information stores may significantly increase application startup time.";
            //
            // OutlookOptionsPane_InfoStores
            //
            this.Controls.Add(this._lvInfoStores);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labelRemote);
            this._lvInfoStores.BringToFront();
            this.Name = "OutlookOptionsPane_InfoStores";
            this.Size = new System.Drawing.Size(276, 220);
            this.ResumeLayout(false);
        }
	    #endregion

        public static AbstractOptionsPane CreateOptionsPane()
        {
            return new OutlookOptionsPane_InfoStores();
        }

        public override void ShowPane()
	    {
            foreach( IResource store in _allStores )
            {
                if ( !store.HasProp( PROP.IgnoredFolder ) && store.HasProp( PROP.StoreSupported ) )
                {
                    _checkBoxColumn.SetItemCheckState( store, CheckBoxState.Checked );
                }
                else
                if( !store.HasProp( PROP.StoreSupported ) )
                {
                    _checkBoxColumn.SetItemCheckState( store, CheckBoxState.Grayed );
                }
            }
        }
        public override void LeavePane()
        {
            if ( IsStartupPane )
            {
                OK();
            }
        }

	    public override void OK()
	    {
            while( _savingInfoStores )
            {
                Thread.Sleep( 50 );
            }
            Core.ResourceAP.RunUniqueJob( new MethodInvoker( SaveInfoStores ) );
	    }

	    private void SaveInfoStores()
	    {
            _savingInfoStores = true;
            try
            {
                bool hadChanges = false;
                foreach( IResource store in _allStores )
                {
                    bool oldChecked = (store.GetIntProp( PROP.IgnoredFolder ) == 0 );
                    bool nowChecked = (_checkBoxColumn.GetItemCheckState( store ) == CheckBoxState.Checked);
                    hadChanges = hadChanges || (oldChecked != nowChecked );

                    if ( nowChecked )
                        store.DeleteProp( PROP.IgnoredFolder );
                    else
                        store.SetProp( PROP.IgnoredFolder, 1 );
                }

                if ( !IsStartupPane && hadChanges )
                {
                    OutlookSession.OutlookProcessor.SynchronizeFolderStructure();
                }
            }
            finally
            {
                _savingInfoStores = false;
            }
	    }

        private void HandleAfterCheck( object sender, CheckBoxEventArgs e )
        {
            if ( IsStartupPane )
            {
                Core.ResourceAP.RunUniqueJob( new MethodInvoker( SaveInfoStores ) );
            }
        }

	    public override string GetHelpKeyword()
	    {
	        return "/reference/outlook_infostores.html";
	    }
	}

    internal class DProvider : IResourceDataProvider
    {
        IResourceList allStores;
        public DProvider( IResourceList stores )
        {
            allStores = stores;
        }
		public void FillResources( ResourceListView2 resourceListView )
		{
            foreach( IResource res in allStores )
            {
		        resourceListView.JetListView.Nodes.Add( res );
            }
		}
		public bool FindResourceNode( IResource res )
		{
		    return allStores.IndexOf( res ) != -1;
		}
    	public virtual void Dispose()
    	{}
    }
}
