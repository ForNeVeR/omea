/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.DebugPlugin
{
	/// <summary>
	/// Summary description for ResourceListView2TestForm.
	/// </summary>
	public class ResourceListView2TestForm : System.Windows.Forms.Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Panel _topPanel;
        private JetTextBox _edtSearch;
        private ResourceListView2 _resourceListView;
        private ResourceNameJetFilter _nameFilter;

		public ResourceListView2TestForm( IResourceDataProvider provider, bool isTree )
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            _resourceListView = new ResourceListView2();
            _resourceListView.Dock = DockStyle.Fill;
            _resourceListView.AllowDrop = true;
            _resourceListView.InPlaceEdit = true;
            if ( isTree )
            {
                _resourceListView.AddTreeStructureColumn();
                _resourceListView.HideSelection = true;
            }
            else
            {
                //_resourceListView.AddCheckBoxColumn();
                _resourceListView.HeaderStyle = ColumnHeaderStyle.Clickable;
                _resourceListView.AllowColumnReorder = true;
            }
            _resourceListView.AddIconColumn();
            if ( isTree )
            {
                RichTextColumn rtCol = new RichTextColumn();
                rtCol.AddNodeDecorator( new UnreadNodeDecorator() );
                rtCol.SizeToContent = true;
                _resourceListView.Columns.Add( rtCol );
            }
            else
            {
                /*
                DisplayColumnManager dcm = (DisplayColumnManager) Core.DisplayColumnManager;
                ColumnDescriptor[] columns = dcm.GetDefaultColumns( Core.ResourceBrowser.VisibleResources );
                foreach( ColumnDescriptor colDesc in columns )
                {
                    int[] propIds = dcm.PropNamesToIDs( colDesc.PropNames, true );
                    bool isCustom = false;
                    for( int i=0; i<propIds.Length; i++ )
                    {
                        if ( dcm.GetCustomColumn( propIds [i] ) != null )
                        {
                            isCustom = true;
                            break;
                        }
                    }

                    if ( isCustom )
                    {
                        AddCustomColumn( dcm, propIds );
                    }
                    else
                    {
                        ResourceListView2Column column = _resourceListView.AddColumn( propIds [0] );
                        column.Width = colDesc.Width;
                    }
                }
                */
                _resourceListView.FullRowSelect = true;
            }
            

            /*
            int propId = Core.ResourceStore.GetPropId( "Annotation" );
            ICustomColumn col = (Core.DisplayColumnManager as DisplayColumnManager).GetCustomColumn( propId );
            _resourceListView.AddCustomColumn( col );
            */

            _resourceListView.DataProvider = provider;
            Controls.Add( _resourceListView );
            Controls.SetChildIndex( _resourceListView, 0 );

            _nameFilter = new ResourceNameJetFilter( "" );
		}

        /*
        private void AddCustomColumn( DisplayColumnManager dcm, int[] propIds )
        {
            ICustomColumn[] customColumns = new ICustomColumn[ propIds.Length ];
            for( int i=0; i<propIds.Length; i++ )
            {
                customColumns [i] = dcm.GetCustomColumn( propIds [i] );
            }
            _resourceListView.AddCustomColumn( propIds, customColumns );
        }
        */

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
            this._topPanel = new System.Windows.Forms.Panel();
            this._edtSearch = new JetTextBox();
            this._topPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _topPanel
            // 
            this._topPanel.Controls.Add(this._edtSearch);
            this._topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._topPanel.Location = new System.Drawing.Point(0, 0);
            this._topPanel.Name = "_topPanel";
            this._topPanel.Size = new System.Drawing.Size(292, 28);
            this._topPanel.TabIndex = 0;
            // 
            // _edtSearch
            // 
            this._edtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtSearch.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._edtSearch.Location = new System.Drawing.Point(4, 4);
            this._edtSearch.Name = "_edtSearch";
            this._edtSearch.Size = new System.Drawing.Size(284, 21);
            this._edtSearch.TabIndex = 0;
            this._edtSearch.Text = "";
            this._edtSearch.IncrementalSearchUpdated += new EventHandler(_edtSearch_IncrementalSearchUpdated);
            // 
            // ResourceListView2TestForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this._topPanel);
            this.Name = "ResourceListView2TestForm";
            this.Text = "ResourceListView2TestForm";
            this._topPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void _edtSearch_IncrementalSearchUpdated( object sender, EventArgs e )
        {
            if ( _edtSearch.Text.Length > 0 )
            {
                _nameFilter.FilterString = _edtSearch.Text;
                _resourceListView.Filters.Add( _nameFilter );
            }
            else
            {
                _resourceListView.Filters.Remove( _nameFilter );
            }
        }
	}
}
