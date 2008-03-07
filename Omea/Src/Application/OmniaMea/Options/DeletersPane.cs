/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
	public class DeletersPane : AbstractOptionsPane
	{
        private JetListView _deletersListView;
        private CheckBoxColumn _confirmDeleteColumn;
        private CheckBoxColumn _confirmPermanentDeleteColumn;
        private CheckBoxColumn _alwaysDeletePermanentlyColumn;
		private System.ComponentModel.Container components = null;
        private HashSet _deleteConfirmedItems = new HashSet();

        public static AbstractOptionsPane DeletersPaneCreator()
        {
            return new DeletersPane();
        }

		private DeletersPane()
		{
			InitializeComponent();
            _deletersListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _deletersListView.ControlPainter = new GdiControlPainter();
            JetListViewColumn nameCol = new JetListViewColumn();
            nameCol.SizeToContent = true;
            nameCol.Text = "Resource Type";
            _deletersListView.Columns.Add( nameCol );
		    _confirmDeleteColumn = new CheckBoxColumn();
            _confirmDeleteColumn.ShowHeader = true;
            _confirmDeleteColumn.Width = 110;
            _confirmDeleteColumn.Text = "Confirm Deletion";
            _deletersListView.Columns.Add( _confirmDeleteColumn );
		    _confirmPermanentDeleteColumn = new CheckBoxColumn();
            _confirmPermanentDeleteColumn.ShowHeader = true;
            _confirmPermanentDeleteColumn.Width = 160;
            _confirmPermanentDeleteColumn.Text = "Confirm Permanent Deletion";
            _deletersListView.Columns.Add( _confirmPermanentDeleteColumn );
		    _alwaysDeletePermanentlyColumn = new CheckBoxColumn();
            _alwaysDeletePermanentlyColumn.ShowHeader = true;
            _alwaysDeletePermanentlyColumn.Width = 160;
            _alwaysDeletePermanentlyColumn.Text = "Always Delete Permanently";
            _deletersListView.Columns.Add( _alwaysDeletePermanentlyColumn );
            _confirmDeleteColumn.AfterCheck += new CheckBoxEventHandler( _confirmDeleteColumn_AfterCheck );
            _alwaysDeletePermanentlyColumn.AfterCheck += new CheckBoxEventHandler( _alwaysDeletePermanentlyColumn_AfterCheck );
		}

        public override void ShowPane()
        {
            /**
             * marshal showing pane through resource thread, because pressing Apply in the Options
             * lead to re-creating pane, and we need to get async updates ( in OK() ) finished
             */
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( ShowPaneImpl ) );
        }

	    private void ShowPaneImpl()
	    {
            if( !Core.UserInterfaceAP.IsOwnerThread )
            {
                Core.UserInterfaceAP.QueueJob( new MethodInvoker( ShowPaneImpl ) );
                return;
            }
	        _deletersListView.Nodes.Clear();
	        IResourceList resTypes = Core.ResourceStore.GetAllResources( "ResourceType" );
	        foreach( IResource resType in resTypes.ValidResources )
	        {
	            if( resType.GetIntProp( "Internal" ) != 0 )
	            {
	                continue;
	            }
	            string type = resType.GetPropText( Core.Props.Name );
	            if( type.Length > 0 && Core.ResourceStore.ResourceTypes.Exist( type ) )
	            {
	                IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( type );
	                if( deleter != null )
	                {
	                    _deletersListView.Nodes.Add( resType );
	                    bool canDelete = deleter.CanDeleteResource( null, false );
	                    if( !canDelete )
	                    {
	                        _confirmDeleteColumn.SetItemCheckState( resType, CheckBoxState.Grayed | CheckBoxState.Unchecked );
	                    }
	                    else
	                    {
	                        _confirmDeleteColumn.SetItemCheckState( resType,
	                                                                ResourceDeleterOptions.GetConfirmDeleteToRecycleBin( type ) ? CheckBoxState.Checked : CheckBoxState.Unchecked );
	                    }
	                    bool canDeletePermanently = deleter.CanDeleteResource( null, true );
	                    if( !canDeletePermanently )
	                    {
	                        _confirmPermanentDeleteColumn.SetItemCheckState( resType, CheckBoxState.Grayed | CheckBoxState.Unchecked );
	                    }
	                    else
	                    {
	                        _confirmPermanentDeleteColumn.SetItemCheckState( resType,
	                                                                         ResourceDeleterOptions.GetConfirmDeletePermanently( type ) ? CheckBoxState.Checked : CheckBoxState.Unchecked );
	                    }

                        if( !deleter.CanIgnoreRecyclebin() )
                        {
	                        _alwaysDeletePermanentlyColumn.SetItemCheckState( resType, CheckBoxState.Grayed | CheckBoxState.Unchecked );
                        }
                        else
	                    if( canDelete != canDeletePermanently )
	                    {
	                        _alwaysDeletePermanentlyColumn.SetItemCheckState( resType, CheckBoxState.Grayed |
	                            ( canDelete ? CheckBoxState.Unchecked : CheckBoxState.Checked ) );
	                    }
	                    else
	                    {
	                        _alwaysDeletePermanentlyColumn.SetItemCheckState( resType,
	                                                                          ResourceDeleterOptions.GetDeleteAlwaysPermanently( type ) ? CheckBoxState.Checked : CheckBoxState.Unchecked );
	                    }
	                }
	            }
	        }
	    }

	    public override void OK()
        {
            foreach( JetListViewNode node in _deletersListView.Nodes )
            {
                IResource resType = (IResource) node.Data;
                string type = resType.GetPropText( Core.Props.Name );
                if( type.Length > 0 )
                {
                    CheckBoxState state = _confirmDeleteColumn.GetItemCheckState( resType );
                    if( state == CheckBoxState.Checked )
                    {
                        ResourceDeleterOptions.SetConfirmDeleteToRecycleBin( type, true );
                    }
                    else if( state == CheckBoxState.Unchecked )
                    {
                        ResourceDeleterOptions.SetConfirmDeleteToRecycleBin( type, false );
                    }
                    ResourceDeleterOptions.SetConfirmDeletePermanently( type,
                        _confirmPermanentDeleteColumn.GetItemCheckState( resType ) == CheckBoxState.Checked );
                    ResourceDeleterOptions.SetDeleteAlwaysPermanently( type,
                        _alwaysDeletePermanentlyColumn.GetItemCheckState( resType ) == CheckBoxState.Checked );
                }
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

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._deletersListView = new JetListView();
            this.SuspendLayout();
            // 
            // _deletersListView
            // 
            this._deletersListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._deletersListView.Location = new System.Drawing.Point(0, 0);
            this._deletersListView.Name = "_deletersListView";
            this._deletersListView.Size = new System.Drawing.Size(184, 156);
            this._deletersListView.TabIndex = 0;
            // 
            // DeletersPane
            // 
            this.Controls.Add(this._deletersListView);
            this.Name = "DeletersPane";
            this.Size = new System.Drawing.Size(184, 156);
            this.ResumeLayout(false);

        }
        #endregion

        private void _alwaysDeletePermanentlyColumn_AfterCheck(object sender, CheckBoxEventArgs e)
        {
            object item = e.Item;
            if( e.NewState == CheckBoxState.Checked )
            {
                _confirmDeleteColumn.SetItemCheckState( item, CheckBoxState.Grayed );
            }
            else
            {
                _confirmDeleteColumn.SetItemCheckState( item,
                    _deleteConfirmedItems.Contains( item ) ? CheckBoxState.Checked : CheckBoxState.Unchecked );
            }
        }

        private void _confirmDeleteColumn_AfterCheck(object sender, CheckBoxEventArgs e)
        {
            object item = e.Item;
            if( e.NewState == CheckBoxState.Checked )
            {
                _deleteConfirmedItems.Add( item );
            }
            else if( e.NewState == CheckBoxState.Unchecked )
            {
                _deleteConfirmedItems.Remove( item );
            }
        }

	    public override string GetHelpKeyword()
	    {
	        return "/reference/delete_confirmations.htm";
	    }
	}
}
