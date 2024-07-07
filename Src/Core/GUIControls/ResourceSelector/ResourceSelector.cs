// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// The dialog for selecting a resource of any type, a resource of a specific type
    /// or a resource from the specified list.
    /// </summary>
    public class ResourceSelector : DialogBase
    {
        private Button      _btnOK;
        private Button      _btnCancel;
        private Button      _btnNew;
        private ComboBox    _resourceTypeCombo;
        private Label       _lblType;
        private Button      _btnHelp;
        private IResourceSelectPane _selectPane;
        private IResourceSelectPane2 _selectPane2;
        private string _helpTopic;

        private System.ComponentModel.IContainer components;

        public ResourceSelector()
        {
            //
            // Required for Windows Form Designer support
            //
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
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._btnNew = new Button();
            this._lblType = new System.Windows.Forms.Label();
            this._resourceTypeCombo = new System.Windows.Forms.ComboBox();
            this._btnHelp = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // _btnOK
            //
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(136, 286);
            this._btnOK.Name = "_btnOK";
            this._btnOK.Size = new System.Drawing.Size(75, 24);
            this._btnOK.TabIndex = 3;
            this._btnOK.Text = "OK";
            //
            // _btnCancel
            //
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(216, 286);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(75, 24);
            this._btnCancel.TabIndex = 4;
            this._btnCancel.Text = "Cancel";
            //
            // _btnNew
            //
            this._btnNew.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this._btnNew.Click += new EventHandler( HandleNewButtonClick );
            this._btnNew.FlatStyle = FlatStyle.System;
            this._btnNew.Location = new Point( 4, 286 );
            this._btnNew.Name = "_btnNew";
            this._btnNew.Size = new Size(75, 24);
            this._btnNew.TabIndex = 6;
            this._btnNew.Text = "New...";
            //
            // _lblType
            //
            this._lblType.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblType.Location = new System.Drawing.Point(12, 9);
            this._lblType.Name = "_lblType";
            this._lblType.Size = new System.Drawing.Size(40, 17);
            this._lblType.TabIndex = 3;
            this._lblType.Text = "Type:";
            //
            // _resourceTypeCombo
            //
            this._resourceTypeCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._resourceTypeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._resourceTypeCombo.Location = new System.Drawing.Point(52, 4);
            this._resourceTypeCombo.MaxDropDownItems = 16;
            this._resourceTypeCombo.Name = "_resourceTypeCombo";
            this._resourceTypeCombo.Size = new System.Drawing.Size(240, 21);
            this._resourceTypeCombo.TabIndex = 0;
            this._resourceTypeCombo.SelectedValueChanged += new System.EventHandler(this.OnSelectedResourceTypeChanged);
            //
            // _btnHelp
            //
            this._btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnHelp.Location = new System.Drawing.Point(216, 286);
            this._btnHelp.Name = "_btnHelp";
            this._btnHelp.TabIndex = 5;
            this._btnHelp.Text = "Help";
            this._btnHelp.Visible = false;
            this._btnHelp.Click += new System.EventHandler(this._btnHelp_Click);
            //
            // ResourceSelector
            //
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(300, 319);
            this.Controls.Add(this._btnHelp);
            this.Controls.Add(this._resourceTypeCombo);
            this.Controls.Add(this._lblType);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add( this._btnNew );
            this.Name = "ResourceSelector";
            this.Text = "Select Resource";
            this.ResumeLayout(false);

        }

        #endregion

        public IResource SelectResource( IWin32Window ownerWindow, string type, string dialogCaption,
                                         IResource initialSelection, string helpTopic )
        {
            string[] types = (type == null) ? null : new string[] { type };
            type = InitDialog( dialogCaption, types,
                (initialSelection == null) ? null : initialSelection.ToResourceList(),
                helpTopic );

            IResourceList resList = Core.ResourceStore.GetAllResourcesLive( type );
            resList.Sort( new SortSettings( ResourceProps.DisplayName, true ) );
            _selectPane.SelectResource( new string[] { type }, resList, initialSelection );

            if ( ShowDialog( ownerWindow ) != DialogResult.OK )
            {
                return null;
            }

            resList = _selectPane.GetSelection();
            if ( resList == null || resList.Count == 0 )
                return null;

            return resList [0];
        }

        public IResourceList SelectResources( IWin32Window ownerWindow, string[] types, string dialogCaption,
                                              IResourceList initialSelection, string helpTopic )
        {
            string type = InitDialog( dialogCaption, types, initialSelection, helpTopic );

            IResourceList resList = Core.ResourceStore.GetAllResourcesLive( type );
            resList.Sort( new SortSettings( ResourceProps.DisplayName, true ) );
            _selectPane.SelectResources( types, resList, initialSelection );

            if ( ShowDialog( ownerWindow ) != DialogResult.OK )
                return null;

            return _selectPane.GetSelection();
        }

        public IResourceList SelectResourcesFromList( IWin32Window ownerWindow, IResourceList fromList,
                                                      string dialogCaption, string helpTopic, IResourceList initialSelection )
        {
            string selectorType = InitDialog( dialogCaption, fromList.GetAllTypes(), null, helpTopic );

            _selectPane.SelectResources( new string[] { selectorType }, fromList, initialSelection );

            if ( ShowDialog( ownerWindow ) != DialogResult.OK )
                return null;

            return _selectPane.GetSelection();
        }

        private string InitDialog( string dialogCaption, string[] types,
                                   IResourceList initialSelection, string helpTopic )
        {
            if ( dialogCaption != null )
            {
                Text = dialogCaption;
            }

            if ( helpTopic != null )
            {
                _btnOK.Left -= 80;
                _btnCancel.Left -= 80;
                _btnHelp.Visible = true;
                _helpTopic = helpTopic;
            }

            RestoreSettings();

            if ( types == null )
            {
                IResourceList resTypes = ResourceTypeHelper.GetVisibleResourceTypes();
                resTypes.Sort( new SortSettings( ResourceProps.DisplayName, true ) );

                string selType;
                if ( initialSelection == null || initialSelection.Count == 0 )
                {
                    selType = resTypes [0].GetStringProp( Core.Props.Name );
                }
                else
                {
                    selType = initialSelection [0].Type;
                }

                foreach( IResource resType in resTypes )
                {
                    int index = _resourceTypeCombo.Items.Add( resType );
                    if ( resType.GetStringProp( Core.Props.Name ) == selType )
                        _resourceTypeCombo.SelectedIndex = index;
                }
                CreateResourceSelectPane( selType, 4, 30 );
                return selType;
            }
            else
            {
                HideTypeSelector();
                string selectorType = null;
                for( int i=0; i<types.Length; i++ )
                {
                    if ( Core.UIManager.GetResourceSelectPaneType( types [i] ) != null )
                    {
                        selectorType = types [i];
                        break;
                    }
                }

                CreateResourceSelectPane( selectorType, 4, 4 );
                if ( selectorType != null )
                {
                    return selectorType;
                }
                return types [0];
            }
        }

        /**
         * Creates and shows the resource selector pane for the specified resource type.
         */

        private void CreateResourceSelectPane( string type, int x, int y )
        {
            _selectPane = Core.UIManager.CreateResourceSelectPane( type );
            UserControl selectPaneCtl = (UserControl) _selectPane;
            selectPaneCtl.Location = new Point( x, y );
            selectPaneCtl.Size = new Size( ClientSize.Width - 8, _btnOK.Location.Y - y - 8 );
            selectPaneCtl.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            _selectPane.Accept += OnAccept;
            Controls.Add( selectPaneCtl );
            selectPaneCtl.TabIndex = 0;

            _selectPane2 = _selectPane as IResourceSelectPane2;
            if ( _selectPane2 == null )
            {
                _btnNew.Visible = false;
            }
            else
            {
                _btnNew.Visible = _selectPane2.ShowNewButton;
            }
        }

        private void HideTypeSelector()
        {
            SuspendLayout();
            _lblType.Visible = false;
            _resourceTypeCombo.Visible = false;
            ResumeLayout();
        }

        private void OnSelectedResourceTypeChanged( object sender, EventArgs e )
        {
            if ( _selectPane != null )
            {
                IResource selResType = (IResource) _resourceTypeCombo.SelectedItem;
                string resType = selResType.GetStringProp( Core.Props.Name );
                IResourceList allResources = Core.ResourceStore.GetAllResourcesLive( resType );
                allResources.Sort( new SortSettings( ResourceProps.DisplayName, true ) );
                _selectPane.SelectResources( new string[] { resType }, allResources, null );
            }
        }

        private void OnAccept( object sender, EventArgs e )
        {
            IResourceList selection = _selectPane.GetSelection();
            if ( selection != null && selection.Count > 0 )
            {
                DialogResult = DialogResult.OK;
            }
        }

        private void _btnHelp_Click( object sender, EventArgs e )
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, _helpTopic );
        }

        private void HandleNewButtonClick( object sender, EventArgs e )
        {
            if ( _selectPane2 != null )
            {
                _selectPane2.HandleNewButtonClicked();
            }
        }
    }
}
