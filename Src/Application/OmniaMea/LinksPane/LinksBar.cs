// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.Components.ImageListButton;

namespace JetBrains.Omea
{
	/**
     * The collapsed version of the links pane.
     */

    internal class LinksBar: LinksPaneBase
	{
        private const int _cSpaceAfterLinkType = 6;
        private const int _cStartLinkY = 2;

        private System.ComponentModel.IContainer components;

        private IResource              _resource;
        private readonly Font          _linkTypeLabelFont = new Font( "Tahoma", 8, FontStyle.Bold );
        private readonly Font          _actionFont = new Font( "Tahoma", 8 );
        private readonly ControlPool   _resourceLinkLabelPool;
        private readonly ControlPool   _linkTypeLabelPool;
        private readonly ControlPool   _actionLabelPool;

        private Panel           _contactThumb;
        private ImageListButton _btnExpand;
        private ImageList       _iconImages;
        private bool            _expanded;
        private ToolTip         _toolTip;
        private bool            _verticalViewMode;
        private bool            _verticalViewExpanded;
        private readonly int    _defltVerticalViewLines;

        private float _scaleHeight = 1.0f;

		public LinksBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            SetStyle( ControlStyles.ResizeRedraw, true );

            _linkTypeLabelPool = new ControlPool( this, OnCreateLinkTypeLabel );
            _resourceLinkLabelPool = new ControlPool( this, OnCreateResourceLinkLabel );
            _resourceLinkLabelPool.DisposeDelegate = OnDisposeResourceLinkLabel;
            _actionLabelPool = new ControlPool( this, OnCreateActionLabel );

            _defltVerticalViewLines = Core.SettingStore.ReadInt( "LinksBar", "VerticalViewLines", 3 );
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

        public event EventHandler LinksPaneExpandChanged;

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            _toolTip = new System.Windows.Forms.ToolTip(this.components);
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(LinksBar));
            this._btnExpand = new ImageListButton();
		    _contactThumb = new Panel();
            this._iconImages = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            //
            // _btnExpand
            //
            this._btnExpand.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnExpand.BackColor = Color.Transparent;
            this._btnExpand.ImageList = this._iconImages;
            this._btnExpand.Location = new System.Drawing.Point(134, 3);
            this._btnExpand.Name = "_chkExpand";
            this._btnExpand.Size = new System.Drawing.Size(16, 16);
            this._btnExpand.TabIndex = 0;
            this._btnExpand.TabStop = false;
            this._btnExpand.Click += new EventHandler(_btnExpand_OnClick);
            //
            // _contactThumb
            //
            _contactThumb.BorderStyle = BorderStyle.Fixed3D;
            _contactThumb.Location = new Point(4, 4);
            _contactThumb.Name = "_contactThumb";
            _contactThumb.Size = new Size(48, 48);
		    _contactThumb.Anchor = AnchorStyles.Left | AnchorStyles.Top;
		    _contactThumb.BackColor = Color.FromArgb( 0, DefaultBackColor );
            //
            // _iconImages
            //
            this._iconImages.ImageSize = new System.Drawing.Size(16, 16);
            this._iconImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_iconImages.ImageStream")));
            this._iconImages.TransparentColor = System.Drawing.Color.Transparent;
            //
            // LinksBar
            //
            this.Controls.Add(this._btnExpand);
            this.Controls.Add(_contactThumb);
            this.Name = "LinksBar";
            this.Size = new System.Drawing.Size(150, 20);
            this.ResumeLayout(false);
        }
        #endregion

        protected override void ScaleCore( float dx, float dy )
        {
            base.ScaleCore( dx, dy );
            _scaleHeight = dy;
        }

        public bool LinksPaneExpanded
        {
            get { return _expanded; }
            set
            {
                if ( _expanded != value )
                {
                    _expanded = value;
                    UpdateExpandButtonImage();

                    if ( LinksPaneExpandChanged != null )
                    {
                        LinksPaneExpandChanged( this, EventArgs.Empty );
                    }
                }
            }
        }

        [Browsable(false)]
        public bool VerticalViewMode
        {
            get { return _verticalViewMode; }
            set
            {
                if ( _verticalViewMode != value )
                {
                    _verticalViewMode = value;
                    if ( _verticalViewMode )
                    {
                        Height = 60;
                        BackColor = SystemColors.Window;
                    }
                    else
                    {
                        Height = 20;
                    }
                    UpdateLinksPane();
                    UpdateExpandButtonImage();
                }
            }
        }

        protected override void OnColorSchemeChanged()
        {
            _btnExpand.ImageList = _colorScheme.GetImageList( "LinksBar.ExpandCollapse" );
            UpdateExpandButtonImage();
        }

        private void UpdateExpandButtonImage()
        {
            int stateStartIndex;
            if ( _verticalViewMode )
            {
                stateStartIndex = _verticalViewExpanded ? 0 : 3;
            }
            else
            {
                stateStartIndex = _expanded ? 0 : 3;
            }
            _btnExpand.NormalImageIndex = stateStartIndex;
            _btnExpand.HotImageIndex = stateStartIndex + 1;
            _btnExpand.PressedImageIndex = stateStartIndex + 2;
        }

        private void _btnExpand_OnClick( object sender, EventArgs e )
        {
            if ( !_verticalViewMode )
            {
                LinksPaneExpanded = !LinksPaneExpanded;
			}
            else
            {
                _verticalViewExpanded = !_verticalViewExpanded;
				UpdateExpandButtonImage();
                UpdateLinksPane();
            }
        }

        /**
         * Displays the links of the specified resource in the links bar.
         */

        public void DisplayLinks( IResource res, ILinksPaneFilter filter )
        {
            _resource = res;
            SetResourceList( res == null ? null : res.ToResourceList(), filter );
            UpdateLinksPane();
        }

        /**
         * Rebuilds the links bar.
         */

        protected override void UpdateLinksPane()
        {
            if ( Core.State == CoreState.ShuttingDown )
            {
                return;
            }

            _linkTypeLabelPool.MoveControlsToPool();
            _resourceLinkLabelPool.MoveControlsToPool();
            _actionLabelPool.MoveControlsToPool();

            if ( _resource != null )
            {
                FillLinkedThumb( _resource );
                FillLinksBar( _resource );
            }
            _resourceLinkLabelPool.RemovePooledControls();
            _linkTypeLabelPool.RemovePooledControls();
            _actionLabelPool.RemovePooledControls();
        }

        /**
         * Creates the link type and link labels for the specified resource.
         */

        private void FillLinksBar( IResource res )
        {
            LinkSection section = BuildLinksForResource( res );
            if ( _verticalViewMode )
            {
                BuildVerticalViewLinks( section );
            }
            else
            {
                BuildHorizontalViewLinks( section );
            }
        }

        private void BuildHorizontalViewLinks( LinkSection section )
        {
            int curX = 4, curY = 0;
            while( section != null )
            {
                int maxWidth = 0;
                Label typeLabel = AddLinkTypeLabel( ref curX, ref curY, section.Name, ref maxWidth );
                if ( curX > Width )
                {
                    typeLabel.Visible = false;
                    break;
                }

                if ( !AddLinksFromSection( section, ref curX, curY ) )
                {
                    typeLabel.Visible = false;
                    break;
                }
                curX += 12;

                section = section.NextSection;
            }
        }

        private void BuildVerticalViewLinks( LinkSection section )
        {
            int curY = _cStartLinkY;
            int maxWidth = 0;
            int lines = 0;
            int curX = _contactThumb.Visible ? 4 + _contactThumb.Width : 4;

            LinksPaneActionItem[] actionItems = null;
            IntArrayList customPropIds = null;
            if ( _verticalViewExpanded )
            {
                actionItems = LinksPaneActionManager.GetManager().CreateActionLinks( _resourceList, _filter );
                try
                {
                    foreach( IResource propTypeRes in ResourceTypeHelper.GetCustomProperties() )
                    {
                        int propID = propTypeRes.GetIntProp( "ID" );
                        if ( _resourceList [0].HasProp( propID ) )
                        {
                            if ( customPropIds == null )
                            {
                                customPropIds = IntArrayListPool.Alloc();
                            }
                            customPropIds.Add( propID );
                        }
                    }
                }
                finally
                {
                    if( customPropIds != null )
                    {
                        IntArrayListPool.Dispose( customPropIds );
                    }
                }
            }

            LinkSection startSection = section;
            while( section != null )
            {
                AddLinkTypeLabel( ref curX, ref curY, section.Name, ref maxWidth );
                if ( _verticalViewExpanded && section.Separator )
                {
                    curY += 24;
                }
                else
                {
                    curY += 16;
                }

                section = section.NextSection;
                lines++;
                if ( !_verticalViewExpanded && lines == _defltVerticalViewLines )
                {
                    break;
                }
            }

            if ( _verticalViewExpanded )
            {
                if ( customPropIds != null )
                {
                    foreach( int propId in customPropIds )
                    {
                        AddLinkTypeLabel( ref curX, ref curY, Core.ResourceStore.PropTypes [propId].DisplayName, ref maxWidth );
                        curY += 16;
                    }
                    if ( actionItems.Length > 0 )
                    {
                        curY += 8;
                    }
                }
                if ( actionItems.Length > 0 )
                {
                    AddLinkTypeLabel( ref curX, ref curY, "Actions", ref maxWidth );
                }
            }

            maxWidth += _contactThumb.Visible ? _contactThumb.Width : 0;
            curY = _cStartLinkY;
            section = startSection;

            lines = 0;
            while( section != null )
            {
                curX = maxWidth + 20;
                AddLinksFromSection( section, ref curX, curY );
                if ( _verticalViewExpanded && section.Separator )
                {
                    curY += 24;
                }
                else
                {
                    curY += 16;
                }
                section = section.NextSection;
                lines++;
                if ( !_verticalViewExpanded && lines == _defltVerticalViewLines )
                {
                    break;
                }
            }

            if ( _verticalViewExpanded )
            {
                if ( customPropIds != null || actionItems.Length > 0 )
                {
                    curY += 2;       // 2 is delta in AddLinkTypeLabel()
                }

                if ( customPropIds != null )
                {
                    curX = maxWidth + 20;
                    foreach( int propId in customPropIds )
                    {
                        AddCustomPropertyLabel( curX, curY, GetCustomPropText( _resourceList [0], propId ) );
                        curY += 16;
                    }
                    if ( actionItems.Length > 0 )
                    {
                        curY += 8;
                    }
                }
                if ( actionItems.Length > 0 )
                {
                    curX = maxWidth + 20;
                    int midX = curX + (Width - curX) / 2;
                    for( int i=0; i<actionItems.Length; i += 2 )
                    {
                        AddActionLabel( actionItems[ i ], curX, curY );
                        if ( i + 1 < actionItems.Length )
                        {
                            AddActionLabel( actionItems[ i + 1 ], midX, curY );
                        }
                        curY += 16;
                    }
                }
            }

            if ( curY > 16 )
            {
                Height = curY + 6;
            }
            else
            {
                // make sure the expand/collapse button is visible when the links bar is empty
                Height = 22;
            }
            //  Update height if the amount of links is small (1 or 2).
            if( _contactThumb.Visible )
                Height = Math.Max( Height, _contactThumb.Height + 8 );
        }

        private bool AddLinksFromSection( LinkSection section, ref int curX, int curY )
        {
            ResourceLinkLabel lastLabel = null;
            foreach( LinkItem linkItem in section.LinkItems )
            {
                ResourceLinkLabel lbl = AddResourceLabel( linkItem.Resource, linkItem.PropId,
                                                          ref curX, curY );
                if ( curX >= Width - 20 && (!_verticalViewMode || lastLabel != null ) )
                {
                    if ( lastLabel != null )
                    {
                        lastLabel.PostfixText = "...";
                        lastLabel.Width = lastLabel.PreferredWidth;
                    }
                    _resourceLinkLabelPool.MoveControlToPool( lbl );
                    return lastLabel != null;
                }
                if ( lastLabel != null )
                {
                    lastLabel.PostfixText = ",";
                    lastLabel.Width = lastLabel.PreferredWidth;
                }
                _toolTip.SetToolTip( lbl.NameLabel, linkItem.ToolTip );
                lastLabel = lbl;
            }
            return true;
        }

        /**
         * Creates a link type label with the specified name.
         */

        private Label AddLinkTypeLabel( ref int curX, ref int curY, string text, ref int maxWidth )
        {
            Label typeLabel = (Label) _linkTypeLabelPool.GetControl();

            typeLabel.Text = text + ":";
            typeLabel.Bounds = new Rectangle( curX, curY+2, Width-8, 15 );
            typeLabel.Visible = true;
            typeLabel.AutoSize = true;

            if ( !_verticalViewMode )
            {
                curX += typeLabel.Width + _cSpaceAfterLinkType;
            }
            if ( typeLabel.Width > maxWidth )
            {
                maxWidth = typeLabel.Width;
            }
            return typeLabel;
        }

        /**
         * Adds an icon and label for a single resource.
         */

        private ResourceLinkLabel AddResourceLabel( IResource linkRes, int linkType, ref int curX, int curY )
        {
            ResourceLinkLabel linkLabel = (ResourceLinkLabel) _resourceLinkLabelPool.GetControl();

            linkLabel.Resource = linkRes;
            linkLabel.LinkType = Math.Abs( linkType );

            int width = linkLabel.PreferredWidth;
            if ( _verticalViewMode && width > Width - curX - 8 )
            {
                width = Width - curX - 8;
                linkLabel.AutoSize = false;
                linkLabel.EndEllipsis = true;
            }
            else
            {
                linkLabel.AutoSize = true;
                linkLabel.EndEllipsis = false;
            }
            int dy = (_scaleHeight >= 1.01f) ? 2 : 1;
            linkLabel.Bounds = new Rectangle( curX, curY+dy, width, 16 );
            linkLabel.LinkOwnerResource = _resource;
            linkLabel.PostfixText = "";
            curX += width + 4;
            return linkLabel;
        }

        private void AddActionLabel( LinksPaneActionItem actionItem, int curX, int curY )
        {
            JetLinkLabel lbl = (JetLinkLabel) _actionLabelPool.GetControl();
            lbl.ClickableLink = true;
            lbl.Enabled       = actionItem.Enabled;
            lbl.Text          = actionItem.Text;
            lbl.Tag           = actionItem.Action;
            lbl.BackColor     = Color.FromArgb( 0, SystemColors.Control );
            lbl.Location      = new Point( curX, curY );
        }

        private void AddCustomPropertyLabel( int curX, int curY, string text )
        {
            JetLinkLabel lbl = (JetLinkLabel) _actionLabelPool.GetControl();
            lbl.ClickableLink = false;
            lbl.Enabled       = true;
            lbl.Text          = text;
            lbl.Tag           = null;
            lbl.BackColor     = Color.FromArgb( 0, SystemColors.Control );
            lbl.Location      = new Point( curX, curY );
        }

        /**
         * Creates a link type label for the pool.
         */

        private Control OnCreateLinkTypeLabel()
        {
            Label typeLabel = new Label();
            typeLabel.Font = _linkTypeLabelFont;
            typeLabel.ForeColor = ColorScheme.GetColor( _colorScheme, "LinksBar.LinkTypeText",
                Color.FromArgb( 96, 96, 96 ) );
            typeLabel.BackColor = Color.FromArgb( 0, DefaultBackColor );
            typeLabel.AutoSize = true;
            return typeLabel;
        }

        /**
         * Creates a resource link label for the pool.
         */

        private Control OnCreateResourceLinkLabel()
        {
            ResourceLinkLabel linkLabel = new ResourceLinkLabel();
            linkLabel.BackColor = Color.FromArgb( 0, DefaultBackColor );
            linkLabel.ShowIcon = false;
            linkLabel.LinkContextMenu += linkLabel_LinkContextMenu;
            linkLabel.ResourceChanged += HandleLinkedResourceChanged;
            return linkLabel;
        }

        private void OnDisposeResourceLinkLabel( Control ctl )
        {
            ResourceLinkLabel linkLabel = ctl as ResourceLinkLabel;
            if ( linkLabel != null )
            {
                linkLabel.ResourceChanged -= HandleLinkedResourceChanged;
                linkLabel.LinkContextMenu -= linkLabel_LinkContextMenu;
            }
        }

        private Control OnCreateActionLabel()
        {
            JetLinkLabel lbl = new JetLinkLabel();
            lbl.Click += LinksPaneActionManager.GetManager().OnActionLabelClick;
            lbl.Font = _actionFont;
            return lbl;
        }

        /**
         * Fills the control with a gradient background.
         */

        protected override void OnPaintBackground( PaintEventArgs pevent )
        {
            base.OnPaintBackground( pevent );

            if ( !_verticalViewMode )
            {
                pevent.Graphics.FillRectangle(
                    ColorScheme.GetBrush( _colorScheme, "LinksBar.Background", ClientRectangle, SystemBrushes.Control ),
                    ClientRectangle );
            }
            else
            {
                Rectangle rc = ClientRectangle;
                pevent.Graphics.FillRectangle(
                    ColorScheme.GetBrush( _colorScheme, "LinksBar.VerticalBackground", rc, SystemBrushes.Control ),
                    rc );
            }
            using( Pen dividerPen = new Pen( Color.FromArgb( 184, 181, 200 ) ) )
            {
                pevent.Graphics.DrawLine( dividerPen, 0, Height-1, Width-1, Height-1 );
            }
        }

        private void linkLabel_LinkContextMenu( object sender, ResourceLinkLabelEventArgs e )
        {
            ResourceLinkLabel linkLabel = (ResourceLinkLabel) sender;
            ShowLinkContextMenu( linkLabel, e );
        }

        private void HandleLinkedResourceChanged( object sender, ResourcePropEventArgs e )
        {
            if ( e.Resource.HasProp( Core.Props.IsDeleted ) || e.ChangeSet.IsDisplayNameAffected )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( UpdateLinksPane ) );
            }
        }

        private void FillLinkedThumb( IResource res )
        {
            if( VerticalViewMode && res != null )
            {
                IResource from = res.GetLinkProp( Core.ContactManager.Props.LinkFrom );
                _contactThumb.Visible = ( from != null && from.Type == "Contact" ) &&
                                        from.HasProp( Core.ContactManager.Props.Picture );
                if( _contactThumb.Visible )
                {
                    Stream strm = from.GetBlobProp( Core.ContactManager.Props.Picture );
                    Image img = Image.FromStream( strm );
                    _contactThumb.BackgroundImageLayout = ImageLayout.Center;
                    _contactThumb.BackgroundImage = img;
                }
            }
            else
                _contactThumb.Visible = false;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout( levent );
            if ( _verticalViewMode )
            {
                foreach( Control ctl in Controls )
                {
                    ResourceLinkLabel linkLabel = ctl as ResourceLinkLabel;
                    if ( linkLabel != null )
                    {
                        int prefWidth = linkLabel.PreferredWidth;
                        if ( prefWidth > Width - linkLabel.Left - 8 )
                        {
                            linkLabel.Width = Width - linkLabel.Left - 8;
                            linkLabel.AutoSize = false;
                            linkLabel.EndEllipsis = true;
                        }
                        else
                        {
                            linkLabel.Width = prefWidth;
                            linkLabel.AutoSize = true;
                            linkLabel.EndEllipsis = false;
                        }
                    }
                }
            }
        }
	}
}
