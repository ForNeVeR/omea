// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /**
     * A control displaying the name and icon of a resource and containing
     * a button for popping up the resource selector.
     */

	public class ResourceSelBox : UserControl
	{
        private Panel               _borderPanel;
        private ImageListPictureBox _imgResIcon;
        private Label               _resNameLabel;
        private Button              _btnResourceSelector;

        private IResourceList       _resList;

        /// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        public event EventHandler ResourceChanged;

		public ResourceSelBox()
		{
			// This call is required by the Windows.Forms Form Designer.
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

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._borderPanel = new System.Windows.Forms.Panel();
            this._btnResourceSelector = new System.Windows.Forms.Button();
            this._resNameLabel = new System.Windows.Forms.Label();
            this._imgResIcon = new GUIControls.ImageListPictureBox();
            this._borderPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // _borderPanel
            //
            this._borderPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._borderPanel.Controls.Add(this._btnResourceSelector);
            this._borderPanel.Controls.Add(this._resNameLabel);
            this._borderPanel.Controls.Add(this._imgResIcon);
            this._borderPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._borderPanel.Location = new System.Drawing.Point(0, 0);
            this._borderPanel.Name = "_borderPanel";
            this._borderPanel.Size = new System.Drawing.Size(204, 24);
            this._borderPanel.TabIndex = 0;
            this._borderPanel.DoubleClick += new System.EventHandler(this._borderPanel_DoubleClick);
            //
            // _btnResourceSelector
            //
            this._btnResourceSelector.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom| AnchorStyles.Right);
            this._btnResourceSelector.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnResourceSelector.Location = new System.Drawing.Point(180, 0);
            this._btnResourceSelector.Name = "_btnResourceSelector";
            this._btnResourceSelector.Size = new System.Drawing.Size(20, 20);
            this._btnResourceSelector.TabIndex = 2;
            this._btnResourceSelector.Text = "...";
            this._btnResourceSelector.Click += new System.EventHandler(this.OnResourceSelectorClick);
            //
            // _resNameLabel
            //
            this._resNameLabel.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right);
            this._resNameLabel.AutoSize = true;
            this._resNameLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._resNameLabel.Location = new System.Drawing.Point(20, 2);
            this._resNameLabel.Name = "_resNameLabel";
            this._resNameLabel.Size = new System.Drawing.Size(0, 16);
            this._resNameLabel.TabIndex = 1;
            this._resNameLabel.UseMnemonic = false;
            this._resNameLabel.DoubleClick += new System.EventHandler(this._borderPanel_DoubleClick);
            //
            // _imgResIcon
            //
            this._imgResIcon.ImageIndex = 0;
            this._imgResIcon.Location = new System.Drawing.Point(2, 2);
            this._imgResIcon.Name = "_imgResIcon";
            this._imgResIcon.Size = new System.Drawing.Size(16, 16);
            this._imgResIcon.TabIndex = 0;
            this._imgResIcon.TabStop = false;
            //
            // ResourceSelBox
            //
            this.Controls.Add(this._borderPanel);
            this.Name = "ResourceSelBox";
            this.Size = new System.Drawing.Size(204, 24);
            this._borderPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        public bool ShowSelectorButton
        {
            get { return _btnResourceSelector.Visible; }
            set { _btnResourceSelector.Visible = value; }
        }

        public IResource Resource
        {
            get
            {
                return ( _resList != null && _resList.Count == 1 ) ? _resList [0] : null;
            }
            set
            {
                ResourceList = (value == null) ? null : value.ToResourceList();
            }
        }

        public IResourceList ResourceList
        {
            get { return _resList; }
            set
            {
                if ( _resList != value )
                {
                    _resList = value;
                    UpdateCurrentResource();
                    if ( ResourceChanged != null )
                    {
                        ResourceChanged( this, EventArgs.Empty );
                    }
                }
            }
        }


        /**
         * Shows the resource icon and display name in the control.
         */

        private void UpdateCurrentResource()
        {
            if ( _resList != null && _resList.Count == 1  )
            {
                _imgResIcon.ImageList = Core.ResourceIconManager.ImageList;
                _imgResIcon.ImageIndex = Core.ResourceIconManager.GetIconIndex( _resList [0] );
            }
            else
            {
                _imgResIcon.ImageList = null;
            }

            if ( _resList == null || _resList.Count == 0 )
            {
                _resNameLabel.Text = "";
            }
            else if ( _resList.Count == 1 )
            {
                _resNameLabel.Text = _resList [0].DisplayName;
            }
            else
            {
                _resNameLabel.Text = "(" + _resList.Count + " resources)";
            }

            _btnResourceSelector.Enabled = (_resList == null || _resList.Count <= 1);
        }

        private void OnResourceSelectorClick( object sender, System.EventArgs e )
        {
            ShowSelectorDialog();
        }

        private void ShowSelectorDialog()
        {
            IResource initialSelection = (_resList == null) ? null : _resList[ 0 ];
            Resource = Core.UIManager.SelectResource( FindForm(), null, null, initialSelection, null );
        }

        private void _borderPanel_DoubleClick( object sender, System.EventArgs e )
        {
            if ( _btnResourceSelector.Visible && _btnResourceSelector.Enabled )
            {
                ShowSelectorDialog();
            }
        }
	}
}
