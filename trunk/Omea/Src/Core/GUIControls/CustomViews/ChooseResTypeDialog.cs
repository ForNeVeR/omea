/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Summary description for ChooseResTypeDialog.
	/// </summary>
	public class ChooseResTypeDialog : DialogBase
	{
        private Button      okButton;
        private Button      cancelButton;
        private Button      buttonHelp;
        private Label       label1;
        private CheckBox    checkAllResourceTypes;
        private JetListView _listFileTypes;
        private CheckBoxColumn  checkFileTypes;
        private Label       label2;
        private GroupBox    groupBox1;
        private JetListView _listLinkTypes;
        private CheckBoxColumn checkLinkTypes;
        private JetListView _listResourceTypes;
        private CheckBoxColumn  checkMajorTypes;

        private readonly IResourceStore  Store;
        private IResourceList   MajorTypes, FormattedTypes, LinkTypes;
        private string          ChosenResFull = "", ChosenResDeep = "";
        public  const string    AllResTypesRepresentation = "All resource types";
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        #region Ctor and Initialization
        public ChooseResTypeDialog( string choosenText ) : this( choosenText, null ) {}
        public ChooseResTypeDialog( string choosenText, IResourceList validResTypes )
		{
            Store = Core.ResourceStore;

			InitializeComponent();
            CollectResourceTypesNames( validResTypes );

            SetupListView( _listResourceTypes, null, ref checkMajorTypes );
            SetupListView( _listLinkTypes, GetLinkItemText, ref checkLinkTypes );
            SetupListView( _listFileTypes, null, ref checkFileTypes );
            checkMajorTypes.AfterCheck += listResourceTypes_ItemCheck;
            checkLinkTypes.AfterCheck += listLinkTypes_ItemCheck;
            checkFileTypes.AfterCheck += listFileTypes_ItemCheck;

            //-----------------------------------------------------------------
            FillListContent( MajorTypes, _listResourceTypes );
            if( LinkTypes.Count > 0 )
            {
                FillListContent( LinkTypes, _listLinkTypes );
                FillListContent( FormattedTypes, _listFileTypes );
            }
            else
            {
                _listFileTypes.Visible = _listLinkTypes.Visible = 
                label1.Visible = label2.Visible = false;
                Size = new Size( Size.Width / 2 + 10, Size.Height );
                _listResourceTypes.Size = new Size( Size.Width - 12, _listResourceTypes.Size.Height );
                _listResourceTypes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Left;
            }

			//-----------------------------------------------------------------
            if( choosenText == null )
            {
                checkAllResourceTypes.Checked = true;
                for( int i = 0; i < _listFileTypes.Nodes.Count; i++ )
                    checkFileTypes.SetItemCheckState( _listFileTypes.Nodes[ i ].Data, CheckBoxState.Checked );
            }
            else
            {
                choosenText = choosenText.Replace( "#", "|" ).Replace( ",", "|" );
                string[]  choosenTypes = choosenText.Split( '|' );

                for( int i = 0; i < MajorTypes.Count; i++ )
                {
                    IResource res = (IResource) _listResourceTypes.Nodes[ i ].Data;
                    if( Array.IndexOf( choosenTypes, res.GetStringProp("Name") ) != -1 )
                        checkMajorTypes.SetItemCheckState( res, CheckBoxState.Checked );
                }
                for( int i = 0; i < LinkTypes.Count; i++ )
                {
                    IResource res = (IResource) _listLinkTypes.Nodes[ i ].Data;
                    if( Array.IndexOf( choosenTypes, res.GetStringProp("Name") ) != -1 )
                        checkLinkTypes.SetItemCheckState( res, CheckBoxState.Checked );
                }
                for( int i = 0; i < FormattedTypes.Count; i++ )
                {
                    IResource res = (IResource) _listFileTypes.Nodes[ i ].Data;
                    if( Array.IndexOf( choosenTypes, res.GetStringProp("Name") ) != -1 )
                        checkFileTypes.SetItemCheckState( res, CheckBoxState.Checked );
                }

                if( !AnyItemChecked( checkLinkTypes ) || !AnyItemChecked( checkFileTypes ) )
                {
                    for( int i = 0; i < _listFileTypes.Nodes.Count; i++ )
                        checkFileTypes.SetItemCheckState( _listFileTypes.Nodes[ i ].Data, CheckBoxState.Checked );
                }
            }
            _listFileTypes.Enabled = AnyItemChecked( checkLinkTypes );
		}

	    private static void SetupListView( JetListView listView, ItemTextCallback callback, ref CheckBoxColumn chkCol )
	    {
	        listView.ControlPainter = new GdiControlPainter();
            listView.NodeCollection.SetItemComparer( null, new TypeByNameComparer() );

            chkCol = new CheckBoxColumn();
            chkCol.HandleAllClicks = true;
            listView.Columns.Add( chkCol );

            ResourceIconColumn iconColumn = new ResourceIconColumn();
            iconColumn.Width = 20;
	        listView.Columns.Add( iconColumn );

	        JetListViewColumn column = new JetListViewColumn();
	        column.SizeToContent = true;
            column.ItemTextCallback = callback;
	        listView.Columns.Add( column );
	    }

        private static void  FillListContent( IResourceList typesList, JetListView _list )
        {
            foreach( IResource typeRes in typesList )
                _list.Nodes.Add( typeRes );
        }
        #endregion Ctor and Initialization

	    //------------------------------------------------------------------------
        public string ChosenResourcesFullText  {  get{  return ChosenResFull;  }   }
        public string ChosenResourcesDeepText  {  get{  return ChosenResDeep;  }   }

        //---------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
					components.Dispose();
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
            this._listResourceTypes = new JetBrains.JetListViewLibrary.JetListView();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this._listFileTypes = new JetBrains.JetListViewLibrary.JetListView();
            this.checkAllResourceTypes = new System.Windows.Forms.CheckBox();
            this._listLinkTypes = new JetBrains.JetListViewLibrary.JetListView();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonHelp = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _listResourceTypes
            // 
            this._listResourceTypes.AllowColumnReorder = false;
            this._listResourceTypes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left)));
            this._listResourceTypes.BackColor = System.Drawing.SystemColors.Window;
            this._listResourceTypes.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._listResourceTypes.ColumnScheme = null;
            this._listResourceTypes.ColumnSchemeProvider = null;
            this._listResourceTypes.GroupProvider = null;
            this._listResourceTypes.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._listResourceTypes.InPlaceEditor = null;
            this._listResourceTypes.Location = new System.Drawing.Point(2, 36);
            this._listResourceTypes.MultiLineView = false;
            this._listResourceTypes.Name = "_listResourceTypes";
            this._listResourceTypes.RowDelimiters = false;
            this._listResourceTypes.Size = new System.Drawing.Size(188, 276);
            this._listResourceTypes.TabIndex = 2;
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.okButton.Location = new System.Drawing.Point(160, 320);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(72, 24);
            this.okButton.TabIndex = 7;
            this.okButton.Text = "OK";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cancelButton.Location = new System.Drawing.Point(240, 320);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(72, 24);
            this.cancelButton.TabIndex = 8;
            this.cancelButton.Text = "Cancel";
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(196, 168);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 16);
            this.label1.TabIndex = 5;
            this.label1.Text = "Matching File Types:";
            // 
            // _listFileTypes
            // 
            this._listFileTypes.AllowColumnReorder = false;
            this._listFileTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this._listFileTypes.BackColor = System.Drawing.SystemColors.Window;
            this._listFileTypes.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._listFileTypes.ColumnScheme = null;
            this._listFileTypes.ColumnSchemeProvider = null;
            this._listFileTypes.GroupProvider = null;
            this._listFileTypes.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._listFileTypes.InPlaceEditor = null;
            this._listFileTypes.Location = new System.Drawing.Point(192, 188);
            this._listFileTypes.MultiLineView = false;
            this._listFileTypes.Name = "_listFileTypes";
            this._listFileTypes.RowDelimiters = false;
            this._listFileTypes.Size = new System.Drawing.Size(208, 124);
            this._listFileTypes.TabIndex = 6;
            // 
            // checkAllResourceTypes
            // 
            this.checkAllResourceTypes.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkAllResourceTypes.Location = new System.Drawing.Point(4, 8);
            this.checkAllResourceTypes.Name = "checkAllResourceTypes";
            this.checkAllResourceTypes.Size = new System.Drawing.Size(120, 20);
            this.checkAllResourceTypes.TabIndex = 1;
            this.checkAllResourceTypes.Text = "All Resource Types";
            this.checkAllResourceTypes.CheckedChanged += new System.EventHandler(this.checkAllResourceTypes_CheckedChanged);
            // 
            // _listLinkTypes
            // 
            this._listLinkTypes.AllowColumnReorder = false;
            this._listLinkTypes.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this._listLinkTypes.BackColor = System.Drawing.SystemColors.Window;
            this._listLinkTypes.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._listLinkTypes.ColumnScheme = null;
            this._listLinkTypes.ColumnSchemeProvider = null;
            this._listLinkTypes.GroupProvider = null;
            this._listLinkTypes.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this._listLinkTypes.InPlaceEditor = null;
            this._listLinkTypes.Location = new System.Drawing.Point(192, 60);
            this._listLinkTypes.MultiLineView = false;
            this._listLinkTypes.Name = "_listLinkTypes";
            this._listLinkTypes.RowDelimiters = false;
            this._listLinkTypes.Size = new System.Drawing.Size(208, 100);
            this._listLinkTypes.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(196, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(136, 16);
            this.label2.TabIndex = 3;
            this.label2.Text = "Resources Containing Files:";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Location = new System.Drawing.Point(4, 28);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(400, 4);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // buttonHelp
            // 
            this.buttonHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonHelp.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonHelp.Location = new System.Drawing.Point(320, 320);
            this.buttonHelp.Name = "buttonHelp";
            this.buttonHelp.Size = new System.Drawing.Size(72, 24);
            this.buttonHelp.TabIndex = 8;
            this.buttonHelp.Text = "Help";
            this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
            // 
            // ChooseResTypeDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(400, 349);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._listLinkTypes);
            this.Controls.Add(this.checkAllResourceTypes);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this._listResourceTypes);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this._listFileTypes);
            this.Controls.Add(this.buttonHelp);
            this.MinimumSize = new System.Drawing.Size(300, 350);
            this.Name = "ChooseResTypeDialog";
            this.Text = "Choose Resource Types";
            this.SizeChanged += new System.EventHandler(this.ChooseResTypeDialog_SizeChanged);
            this.ResumeLayout(false);

        }
		#endregion

        private void okButton_Click(object sender, System.EventArgs e)
        {
            Debug.Assert( IsValidChecksCombination(), "OK conversions are called on illegal checks combination" );
            if( checkAllResourceTypes.Checked )
            {
                ChosenResFull = AllResTypesRepresentation;
                ChosenResDeep = null;
            }
            else
            {
                ChosenResFull = ChosenResDeep = "";

                for( int i = 0; i < _listResourceTypes.Nodes.Count; i++ )
                {
                    if( checkMajorTypes.GetItemCheckState( _listResourceTypes.Nodes[ i ].Data ) == CheckBoxState.Checked )
                    {
                        string name = _listResourceTypes.Nodes[ i ].Data.ToString();
                        ChosenResFull += name + ", ";
                        ChosenResDeep += ((IResource)_listResourceTypes.Nodes[ i ].Data).GetStringProp( "Name" ) + "|";
                    }
                }

                if( ChosenResFull.Length > 0 )
                {
                    ChosenResFull = ChosenResFull.Remove( ChosenResFull.Length - 2, 2 );
                    ChosenResDeep = ChosenResDeep.Remove( ChosenResDeep.Length - 1, 1 );

                    if( AnyItemChecked( checkLinkTypes ) )
                    {
                        ChosenResFull += " and ";
                        ChosenResDeep += "|";
                    }
                }

                //-----------------------------------------------------------------
                for( int i = 0; i < _listLinkTypes.Nodes.Count; i++ )
                {
                    if( checkLinkTypes.GetItemCheckState( _listLinkTypes.Nodes [i].Data ) == CheckBoxState.Checked )
                    {
                        IResource  link = (IResource)_listLinkTypes.Nodes[ i ].Data;
                        string name = GetLinkItemText( link );

                        ChosenResFull += name + ", ";
                        ChosenResDeep += link.GetStringProp( "Name" ) + "|";
                    }
                }

                if( ChosenResFull[ ChosenResFull.Length - 1 ] == ' ' )
                {
                    ChosenResFull = ChosenResFull.Remove( ChosenResFull.Length - 2, 2 );
                    ChosenResDeep = ChosenResDeep.Remove( ChosenResDeep.Length - 1, 1 );
                }

                //-----------------------------------------------------------------
                if( AnyItemChecked( checkLinkTypes ) )
                {
                    if( AllItemsChecked( checkFileTypes ) )
                        ChosenResFull += " within all formats";
                    else
                    {
                        ChosenResFull += " within " + FormattedResources( false );
                        ChosenResDeep += "#" + FormattedResources( true ).Replace( ", ", "|" );
                    }
                }
            }
            DialogResult = DialogResult.OK;
        }

        private void checkAllResourceTypes_CheckedChanged(object sender, System.EventArgs e)
        {
            if( checkAllResourceTypes.Checked )
            {
                for( int i = 0; i < _listResourceTypes.Nodes.Count; i++ )
                    checkMajorTypes.SetItemCheckState( _listResourceTypes.Nodes [i].Data, CheckBoxState.Unchecked );
                for( int i = 0; i < _listLinkTypes.Nodes.Count; i++ )
                    checkLinkTypes.SetItemCheckState( _listLinkTypes.Nodes [i].Data, CheckBoxState.Unchecked );
                for( int i = 0; i < _listFileTypes.Nodes.Count; i++ )
                    checkFileTypes.SetItemCheckState( _listFileTypes.Nodes [i].Data, CheckBoxState.Checked );
                _listFileTypes.Enabled = false;
            }
            _listLinkTypes.Selection.Clear();
            _listResourceTypes.Selection.Clear();
            _listFileTypes.Selection.Clear();
            okButton.Enabled = checkAllResourceTypes.Checked;
        }

        //---------------------------------------------------------------------
        private bool IsValidChecksCombination()
        {
            return( checkAllResourceTypes.Checked || 
                    AnyItemChecked( checkMajorTypes ) && !AnyItemChecked( checkLinkTypes ) ||
                    AnyItemChecked( checkLinkTypes ) && AnyItemChecked( checkFileTypes ) );
        }

        private static bool AnyItemChecked( CheckBoxColumn col )
        {
            JetListView listView = col.OwnerControl;
            for( int i=0; i<listView.Nodes.Count; i++ )
            {
                if ( col.GetItemCheckState( listView.Nodes [i].Data ) == CheckBoxState.Checked )
                    return true;
            }
            return false;
        }

        private static bool AllItemsChecked( CheckBoxColumn col )
        {
            JetListView listView = col.OwnerControl;
            for( int i=0; i<listView.Nodes.Count; i++ )
            {
                if ( col.GetItemCheckState( listView.Nodes [i].Data ) == CheckBoxState.Unchecked )
                    return false;
            }
            return true;
        }

        public static void Deep2Display( string deepName, out string shortName, out string fullName )
        {
            string[] resTypes, formats, linkTypes;
            ResourceTypeHelper.ExtractFields( deepName, out formats, out resTypes, out linkTypes );

            fullName = shortName = "";
            if( resTypes.Length > 0 )
            {
                foreach( string str in resTypes )
                {
                    fullName += ResourceTypeHelper.ResTypeDisplayName( str ) + ", ";
                    shortName += ResourceTypeHelper.ResTypeDisplayName( str ) + ", ";
                }
                fullName = fullName.Substring( 0, fullName.Length - 2 );
                shortName = shortName.Substring( 0, shortName.Length - 2 );
            }
            if( resTypes.Length > 0 && linkTypes.Length > 0 )
            {
                shortName += " and ";
                fullName += " and ";
            }
            if( linkTypes.Length > 0 )
            {
                foreach( string str in linkTypes )
                {
                    fullName += ResourceTypeHelper.LinkTypeReversedDisplayName( str ) + ", ";
                    shortName += ResourceTypeHelper.LinkTypeReversedDisplayName( str ) + ", ";
                }
                fullName = fullName.Substring( 0, fullName.Length - 2 );
                shortName = shortName.Substring( 0, shortName.Length - 2 );
            }
            if( formats.Length == 0 && linkTypes.Length > 0 )
                fullName += " within all formats";
            else
            if( formats.Length > 0 )
            {
                fullName += " within ";
                foreach( string str in formats )
                    fullName += str + ", ";
                fullName = fullName.Substring( 0, fullName.Length - 2 );
            }
        }

        private string  FormattedResources( bool deepName )
        {
            string result = "";
            for( int i = 0; i < _listFileTypes.Nodes.Count; i++ )
            {
                if( checkFileTypes.GetItemCheckState( _listFileTypes.Nodes [i].Data ) == CheckBoxState.Checked )
                {
                    string name = _listFileTypes.Nodes[ i ].Data.ToString();
                    if( deepName )
                        name = ResourceTypeHelper.ResTypeDeepName( name );
                    result += name + ", ";
                }
            }
            if( result.Length > 0 )
                result = result.Remove( result.Length - 2, 2 );
            return result;
        }

        private void  CollectResourceTypesNames( IResourceList validResTypes )
        {
            MajorTypes = FormattedTypes = LinkTypes = Core.ResourceStore.EmptyResourceList;

            ArrayList validNames = new ArrayList();
            if( validResTypes != null )
            {
                foreach( IResource res in validResTypes )
                    validNames.Add( res.GetStringProp( Core.Props.Name ));
            }

            //-----------------------------------------------------------------
            foreach( IResourceType rt in Store.ResourceTypes )
            {
                IResource resForType = Core.ResourceStore.FindUniqueResource( "ResourceType", Core.Props.Name, rt.Name );

                //  possible when resource types are deleted via DebugPlugin.
                if( resForType != null )
                {
                    if( rt.HasFlag( ResourceTypeFlags.FileFormat ))
                    {
                        if( IsFeasibleRT( rt, validNames, ResourceTypeFlags.Internal ))
                            FormattedTypes = FormattedTypes.Union( resForType.ToResourceList() );
                    }
                    else
                    {
                        if( IsFeasibleRT( rt, validNames, ResourceTypeFlags.NoIndex ))
                            MajorTypes = MajorTypes.Union( resForType.ToResourceList() );
                    }
                }
            }
            //  Resource "Clipping" is a special resource since it is semantically
            //  subsumed to be the part of other resource (and thus indirectly
            //  inherits its type)
//            temp1.Remove( "Clipping" );
            //  end hack

            //-----------------------------------------------------------------
            foreach( IPropType pt in Store.PropTypes )
            {
                if( pt.HasFlag( PropTypeFlags.SourceLink ) && ( pt.DisplayName != null ) && pt.OwnerPluginLoaded )
                {
                    if( validResTypes == null || validNames.IndexOf( pt.Name ) != -1 )
                    {
                        IResource resForProp = Core.ResourceStore.FindUniqueResource( "PropType", "Name", pt.Name );
                        LinkTypes = LinkTypes.Union( resForProp.ToResourceList() );
                    }
                }
            }
        }

        private bool IsFeasibleRT( IResourceType rt, ArrayList validNames, ResourceTypeFlags checkFlag )
        {
            //  1. Plugin must be loaded which is responsible for that res type
            //  2. If there is no restriction on particular res types
            //     (validNames.Count == 0) then allow those which are indexable and
            //     with nonempty name.
            //  3. If there is restriction on the particular res types, then only
            //     those res types are allowed.
            return  rt.OwnerPluginLoaded && 
                   (( !rt.HasFlag( checkFlag ) && ( rt.DisplayName != null ) && (validNames.Count == 0)) ||
                   ( validNames.IndexOf( rt.Name ) != -1 ));
        }

        private void listResourceTypes_ItemCheck(object sender, CheckBoxEventArgs e)
        {
            if( e.NewState == CheckBoxState.Checked )
            {
                if( AllItemsChecked( checkMajorTypes ) && 
                    AllItemsChecked( checkLinkTypes ) && AllItemsChecked( checkFileTypes ) )
                {
                    checkAllResourceTypes.Checked = true;
                }
                else
                    checkAllResourceTypes.Checked = false;
            }
            _listLinkTypes.Selection.Clear();
            _listFileTypes.Selection.Clear();
            okButton.Enabled = IsValidChecksCombination();
        }

        private void listLinkTypes_ItemCheck(object sender, CheckBoxEventArgs e)
        {
            if( e.NewState == CheckBoxState.Checked )
            {
                if( AllItemsChecked( checkLinkTypes ) && 
                    AllItemsChecked( checkMajorTypes ) && AllItemsChecked( checkFileTypes ) )
                {
                    checkAllResourceTypes.Checked = true;
                }
                else
                    checkAllResourceTypes.Checked = false;
            }
            _listFileTypes.Enabled = AnyItemChecked( checkLinkTypes );
            _listFileTypes.Selection.Clear();
            _listResourceTypes.Selection.Clear();
            okButton.Enabled = IsValidChecksCombination();
        }

        private void listFileTypes_ItemCheck( object sender, CheckBoxEventArgs e )
        {
            if( AllItemsChecked( checkFileTypes ) && 
                AllItemsChecked( checkMajorTypes ) && AllItemsChecked( checkLinkTypes ) )
            {
                checkAllResourceTypes.Checked = true;
            }
            _listLinkTypes.Selection.Clear();
            _listResourceTypes.Selection.Clear();
            okButton.Enabled = IsValidChecksCombination();
        }

        private void ChooseResTypeDialog_SizeChanged(object sender, EventArgs e)
        {
            if( _listFileTypes.Visible )
            {
                SuspendLayout();
                _listResourceTypes.Size = new Size( Size.Width / 2 - 16, _listResourceTypes.Size.Height );
                _listLinkTypes.Size = new Size( Size.Width / 2 + 4, _listLinkTypes.Size.Height );
                _listFileTypes.Size = new Size( Size.Width / 2 + 4, _listFileTypes.Size.Height );
                _listLinkTypes.Location = new Point( Size.Width / 2 - 12, _listLinkTypes.Location.Y );
                _listFileTypes.Location = new Point( Size.Width / 2 - 12, _listFileTypes.Location.Y );
                label1.Location = new Point( Size.Width / 2 - 8, label1.Location.Y );
                label2.Location = new Point( Size.Width / 2 - 8, label2.Location.Y );
                ResumeLayout();
            }
        }

        private static string GetLinkItemText( object item )
        {
            IResource res = (IResource) item;
            string propName = res.GetStringProp( Core.Props.Name );
            string reverseDisplayName = Core.ResourceStore.PropTypes [propName].ReverseDisplayName;
            if ( reverseDisplayName == null )
                return Core.ResourceStore.PropTypes [propName].DisplayName;
            return reverseDisplayName;
        }

        private void buttonHelp_Click(object sender, System.EventArgs e)
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "reference\\Choose_Recourse_Types_Dialog.htm" );
        }
    }

    internal class TypeByNameComparer : IComparer
    {
        #region IComparer Members
        public int Compare(object x, object y)
        {
            IResource xRes = (IResource)x, yRes = (IResource)y;
            return String.Compare( xRes.DisplayName, yRes.DisplayName );
        }
        #endregion
    }
}
