/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.FiltersManagement;

namespace JetBrains.Omea.CustomProperties
{
	/**
     * Dialog for configuring the types of custom properties.
     */
    
    public class CustomPropTypesDlg : DialogBase
	{
        private Button _btnOK;
        private Button _btnCancel;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private Button _btnAdd;
        private ContextMenu _mnuPropType;
        private MenuItem miAddStringProp;
        private ListView _lvTypes;
        private Button _btnDelete;
        private MenuItem miAddNumberProp;
        private MenuItem miAddDateProp;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private MenuItem miAddBoolProp;

        private readonly List<int> _deletedPropIDs = new List<int>();

        private class PropTypeTag
        {
        	public readonly int PropID;
            public readonly PropDataType DataType;

        	public PropTypeTag( int propID, PropDataType dataType )
        	{
        		PropID = propID;
        		DataType = dataType;
        	}
        }

		public CustomPropTypesDlg()
		{
			InitializeComponent();
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(CustomPropTypesDlg));
			this._btnOK = new System.Windows.Forms.Button();
			this._btnCancel = new System.Windows.Forms.Button();
			this._lvTypes = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this._btnAdd = new System.Windows.Forms.Button();
			this._mnuPropType = new System.Windows.Forms.ContextMenu();
			this.miAddStringProp = new System.Windows.Forms.MenuItem();
			this.miAddNumberProp = new System.Windows.Forms.MenuItem();
			this.miAddDateProp = new System.Windows.Forms.MenuItem();
			this.miAddBoolProp = new System.Windows.Forms.MenuItem();
			this._btnDelete = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _btnOK
			// 
			this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnOK.Location = new System.Drawing.Point(264, 227);
			this._btnOK.Name = "_btnOK";
			this._btnOK.TabIndex = 0;
			this._btnOK.Text = "OK";
			// 
			// _btnCancel
			// 
			this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnCancel.Location = new System.Drawing.Point(348, 227);
			this._btnCancel.Name = "_btnCancel";
			this._btnCancel.TabIndex = 1;
			this._btnCancel.Text = "Cancel";
			// 
			// _lvTypes
			// 
			this._lvTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this._lvTypes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																					   this.columnHeader1,
																					   this.columnHeader2});
			this._lvTypes.FullRowSelect = true;
			this._lvTypes.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this._lvTypes.HideSelection = false;
			this._lvTypes.Location = new System.Drawing.Point(4, 8);
			this._lvTypes.MultiSelect = false;
			this._lvTypes.Name = "_lvTypes";
			this._lvTypes.Size = new System.Drawing.Size(336, 211);
			this._lvTypes.TabIndex = 2;
			this._lvTypes.View = System.Windows.Forms.View.Details;
			this._lvTypes.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this._lvTypes_AfterLabelEdit);
			this._lvTypes.SelectedIndexChanged += new System.EventHandler(this._lvTypes_SelectedIndexChanged);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Name";
			this.columnHeader1.Width = 92;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Type";
			this.columnHeader2.Width = 88;
			// 
			// _btnAdd
			// 
			this._btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnAdd.Image = ((System.Drawing.Image)(resources.GetObject("_btnAdd.Image")));
			this._btnAdd.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this._btnAdd.Location = new System.Drawing.Point(348, 8);
			this._btnAdd.Name = "_btnAdd";
			this._btnAdd.TabIndex = 3;
			this._btnAdd.Text = "Add";
			this._btnAdd.Click += new System.EventHandler(this._btnAdd_Click);
			// 
			// _mnuPropType
			// 
			this._mnuPropType.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.miAddStringProp,
																						 this.miAddNumberProp,
																						 this.miAddDateProp,
																						 this.miAddBoolProp});
			// 
			// miAddStringProp
			// 
			this.miAddStringProp.Index = 0;
			this.miAddStringProp.Text = "Add Text Property";
			this.miAddStringProp.Click += new System.EventHandler(this.miAddStringProp_Click);
			// 
			// miAddNumberProp
			// 
			this.miAddNumberProp.Index = 1;
			this.miAddNumberProp.Text = "Add Number Property";
			this.miAddNumberProp.Click += new System.EventHandler(this.miAddNumberProp_Click);
			// 
			// miAddDateProp
			// 
			this.miAddDateProp.Index = 2;
			this.miAddDateProp.Text = "Add Date Property";
			this.miAddDateProp.Click += new System.EventHandler(this.miAddDateProp_Click);
			// 
			// miAddBoolProp
			// 
			this.miAddBoolProp.Index = 3;
			this.miAddBoolProp.Text = "Add Yes/No Property";
			this.miAddBoolProp.Click += new System.EventHandler(this.miAddBoolProp_Click);
			// 
			// _btnDelete
			// 
			this._btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnDelete.Location = new System.Drawing.Point(348, 40);
			this._btnDelete.Name = "_btnDelete";
			this._btnDelete.TabIndex = 4;
			this._btnDelete.Text = "Delete";
			this._btnDelete.Click += new System.EventHandler(this._btnDelete_Click);
			// 
			// CustomPropTypesDlg
			// 
			this.AcceptButton = this._btnOK;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.CancelButton = this._btnCancel;
			this.ClientSize = new System.Drawing.Size(432, 258);
			this.Controls.Add(this._btnDelete);
			this.Controls.Add(this._btnAdd);
			this.Controls.Add(this._lvTypes);
			this.Controls.Add(this._btnCancel);
			this.Controls.Add(this._btnOK);
			this.MinimumSize = new System.Drawing.Size(288, 196);
			this.Name = "CustomPropTypesDlg";
			this.Text = "Custom Property Types";
			this.ResumeLayout(false);

		}
		#endregion

        public void EditCustomPropertyTypes()
        {
            IResourceList customPropTypes = Core.ResourceStore.FindResources( "PropType", "Custom", 1 );
            foreach( IResource res in customPropTypes )
            {
            	string name = res.GetStringProp( Core.Props.Name );
                PropDataType dataType = Core.ResourceStore.PropTypes [name].DataType;
                if ( dataType != PropDataType.Link )
                {
                    if ( name.StartsWith( "Custom.") )
                    {
                        name = name.Substring( 7 );
                    }
                    ListViewItem lvItem = _lvTypes.Items.Add( name );
                    lvItem.SubItems.Add( GetDataTypeName( dataType ) );
                    lvItem.Tag = new PropTypeTag( res.GetIntProp( "ID" ), dataType );
                    if ( lvItem.Index == 0 )
                    {
                        lvItem.Selected = true;
                    }
                }
            }
            UpdateButtonState();
        	if ( ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                Core.ResourceAP.RunJob( new MethodInvoker( SaveCustomPropertyTypes ) );
            }
        }

	    private void UpdateButtonState()
	    {
            _btnDelete.Enabled = (_lvTypes.SelectedItems.Count > 0);
	    }

        private void _lvTypes_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateButtonState();        
        }

        private void _btnAdd_Click( object sender, EventArgs e )
        {
            _mnuPropType.Show( _btnAdd, new Point( 0, _btnAdd.Height ) );
        }

        private void miAddStringProp_Click( object sender, EventArgs e )
        {
            AddPropertyType( PropDataType.String );        
        }

        private void miAddNumberProp_Click( object sender, EventArgs e )
        {
            AddPropertyType( PropDataType.Int );        
        }

        private void miAddDateProp_Click( object sender, EventArgs e )
        {
            AddPropertyType( PropDataType.Date );
        }

        private void miAddBoolProp_Click( object sender, EventArgs e )
        {
            AddPropertyType( PropDataType.Bool );        
        }

        private void AddPropertyType( PropDataType dataType )
        {
            _lvTypes.LabelEdit = true;
        	ListViewItem lvItem = _lvTypes.Items.Add( "" );
            lvItem.SubItems.Add( GetDataTypeName( dataType ) );
            lvItem.Tag = new PropTypeTag( -1, dataType );
            lvItem.BeginEdit();
        }

		private static string GetDataTypeName( PropDataType type )
		{
		    switch( type )
		    {
                case PropDataType.String: return "text";
                case PropDataType.Int:    return "number";
                case PropDataType.Date:   return "date";
                case PropDataType.Bool:   return "yes/no";
                default: return "other";
            }
		}

        private void _lvTypes_AfterLabelEdit( object sender, LabelEditEventArgs e )
        {
            if ( e.Label == null )
            {
            	_lvTypes.Items.RemoveAt( e.Item );
                return;
            }

            if ( PropTypeExists( e.Label, e.Item ) )
            {
            	MessageBox.Show( this,
                    "A property type called '" + e.Label + "' already exists. Please choose a different name.",
                    "New Property Type" );
                e.CancelEdit = true;
                _lvTypes.Items [e.Item].BeginEdit();
            }
            else
            {
                _lvTypes.Items [e.Item].Selected = true;
            }
            _lvTypes.LabelEdit = false;
        }

        /**
         * Checks if the property type with the specified name is either registered
         * in the resource store (and not deleted in the dialog) or added in the dialog
         * and not yet saved.
         */
        
        private bool PropTypeExists( string name, int skipItem )
        {
            IResourceStore store = Core.ResourceStore;
            if ( store.PropTypes.Exist( "Custom." + name ) )
            {
                int propID = store.PropTypes ["Custom." + name].Id;
                if ( _deletedPropIDs.IndexOf( propID ) < 0 )
                {
                    return true;
                }
            }
            
            for( int i=0; i<_lvTypes.Items.Count; i++ )
            {
                if ( i != skipItem && _lvTypes.Items [i].Text == name )
                {
                    return true;                    
                }
            }

            return false;
        }

        private void _btnDelete_Click( object sender, EventArgs e )
        {
            if ( _lvTypes.SelectedItems.Count > 0 )
            {
            	ListViewItem lvItem = _lvTypes.SelectedItems [0];
                PropTypeTag tag = (PropTypeTag) lvItem.Tag;
                if ( tag != null )
                {
                    if ( tag.PropID >= 0 )
                    {
                        string  warnText = ". Are you sure you wish to delete it?";
                        IResourceList resList = Core.ResourceStore.FindResourcesWithProp( null, tag.PropID );
                        IResourceList conditions = Core.ResourceStore.FindResources( SelectionType.Normal,
                                                       FilterManagerProps.ConditionResName,
                                                       "ApplicableToProp", "Custom." + lvItem.Text );
                        DialogResult dr = DialogResult.Yes;
                        if ( resList.Count > 0 )
                        {
                            if( conditions.Count > 0 )
                            {
                                warnText = resList.Count + " resources have the property " + lvItem.Text +
                                           " and " + conditions.Count + " views use it" + warnText;
                            }
                            else
                            {
                                warnText = resList.Count + " resources have the property " + lvItem.Text + warnText;
                            }
                            dr = MessageBox.Show( Core.MainWindow, warnText,
                                                  "Delete Custom Property Type", MessageBoxButtons.YesNo );
                        }
                        if ( dr == DialogResult.Yes )
                        {
                            _deletedPropIDs.Add( tag.PropID );
                            _lvTypes.Items.Remove( lvItem );
                        }
                    }
                    else
                    {
                        _lvTypes.Items.Remove( lvItem );
                    }
                }
            }
        }

        private void SaveCustomPropertyTypes()
        {
            IResourceStore store = Core.ResourceStore;
            foreach( int propID in _deletedPropIDs )
            {
                Core.DisplayColumnManager.RemoveAvailableColumn( null, store.PropTypes [propID].Name );
                DeleteCustomPropCondition( propID );
                store.PropTypes.Delete( propID );
            }
            foreach( ListViewItem lvItem in _lvTypes.Items )
            {
            	PropTypeTag tag = (PropTypeTag) lvItem.Tag;
                if ( tag.PropID == -1 )
                {
                    string propName = "Custom." + lvItem.Text;
                    int propID = store.PropTypes.Register( propName, tag.DataType );
                    store.PropTypes.RegisterDisplayName( propID, lvItem.Text );
                    IResource res = store.FindUniqueResource( "PropType", Core.Props.Name, propName );
                    res.SetProp( "Custom", 1 );

                    Core.DisplayColumnManager.RegisterAvailableColumn( null, 
                        new ColumnDescriptor( propName, 100 ) );

                    RegisterCustomPropCondition( propID );
                }
            }
        }

	    /**
         * Registers a condition allowing to use the custom property with the specified
         * ID in views.
         */
        
        private static void RegisterCustomPropCondition( int propID )
	    {
	        IPropType propType = Core.ResourceStore.PropTypes [propID];

            string condName = GetConditionName( propType );
            
            IResource condition = null;
            if ( propType.DataType == PropDataType.String || propType.DataType == PropDataType.Date )
            {
                condition = Core.FilterRegistry.CreateConditionTemplate( condName, condName,
                    null, ConditionOp.Eq, propType.Name );
            }
            else if ( propType.DataType == PropDataType.Int )
            {
                condition = Core.FilterRegistry.CreateConditionTemplate( condName, condName,
                    null, ConditionOp.InRange, propType.Name, Int32.MinValue.ToString(), Int32.MaxValue.ToString() );
            }
            else if ( propType.DataType == PropDataType.Bool )
            {
                condition = Core.FilterRegistry.CreateStandardCondition( condName, condName,
                    null, propType.Name, ConditionOp.HasProp );
            }

            if ( condition != null )
            {
                Core.FilterRegistry.AssociateConditionWithGroup( condition, "Custom Property Conditions" );
            }
	    }

        private static void DeleteCustomPropCondition( int propID )
        {
            IPropType propType = Core.ResourceStore.PropTypes [propID];

            //  remove condition template which is made from this property
            string resTypeName = (propType.DataType != PropDataType.Bool) ? FilterManagerProps.ConditionTemplateResName : 
                                                                            FilterManagerProps.ConditionResName;
            IResourceList conditions = Core.ResourceStore.FindResources( resTypeName, Core.Props.Name, GetConditionName( propType ) );
            if ( conditions.Count == 1 )
            {
                conditions[ 0 ].Delete();
            }

            //  remove views which use conditions based on condition templates
            conditions = Core.ResourceStore.FindResources( SelectionType.Normal, FilterManagerProps.ConditionResName,
                                                           "ApplicableToProp", propType.Name );
            IResourceList views = Core.ResourceStore.EmptyResourceList;
            foreach( IResource res in conditions )
            {
                views = views.Union( res.GetLinksOfType( FilterManagerProps.ViewResName, "LinkedCondition" ));
            }
            foreach( IResource res in views )
            {
                Core.FilterRegistry.DeleteView( res );
            }
        }

        private static string GetConditionName( IPropType propType )
        {
            switch( propType.DataType )
            {
                case PropDataType.String: 
                    return "'" + propType.DisplayName + "' is equal to %value%";
                
                case PropDataType.Int:
                    return "'" + propType.DisplayName + "' is in %range%";

                case PropDataType.Date:
                    return "'" + propType.DisplayName + "' is in %range%";

                case PropDataType.Bool:
                    return "Has property '" + propType.DisplayName + "'";

                default:
                    return propType.DisplayName;
            }
        }
	}
}
