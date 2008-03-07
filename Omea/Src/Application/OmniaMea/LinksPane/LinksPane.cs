/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
    /**
     * Pane displaying all the links of the specified resource.
     */

    internal class LinksPane: LinksPaneBase
    {
        private System.ComponentModel.IContainer components;

        private ToolTip         _toolTip;
        private Panel           _borderPanel;
        private int             _scrollY;
        private readonly Font   _linkTypeLabelFont = new Font( "Tahoma", 8, FontStyle.Bold );
        private readonly Font   _linkLabelFont = new Font( "Tahoma", 8 );

        private readonly ControlPool _resourceLinkLabelPool;
        private readonly ControlPool _linkTypeLabelPool;
        private readonly ControlPool _actionLabelPool;
        private readonly ControlPool _separatorPool;

        private const int _linkTypeLabelX = 6;
        private const int _linkLabelX = 14;

        public LinksPane()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            _linkTypeLabelPool = new ControlPool( _borderPanel, OnCreateLinkTypeLabel );
            _actionLabelPool = new ControlPool( _borderPanel, OnCreateActionLabel );
            _separatorPool = new ControlPool( _borderPanel, OnCreateSeparator );
            _resourceLinkLabelPool = new ControlPool( _borderPanel, OnCreateResourceLinkLabel );
            _resourceLinkLabelPool.DisposeDelegate = OnDisposeResourceLinkLabel;

            SetStyle( ControlStyles.UserPaint, true );
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
            this.components = new System.ComponentModel.Container();
            this._toolTip = new System.Windows.Forms.ToolTip(this.components);
            this._borderPanel = new System.Windows.Forms.Panel();
            this._borderPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _borderPanel
            // 
            this._borderPanel.AutoScroll = true;
            this._borderPanel.BorderStyle = BorderStyle.None;
            this._borderPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._borderPanel.Location = new System.Drawing.Point(0, 0);
            this._borderPanel.Name = "_borderPanel";
            this._borderPanel.Size = new System.Drawing.Size(150, 150);
            this._borderPanel.TabIndex = 0;
            this._borderPanel.Paint += new PaintEventHandler(_borderPanel_Paint);
            // 
            // LinksPane
            // 
            this.Controls.Add( _borderPanel );
            this.Name = "LinksPane";
            this.Layout += new System.Windows.Forms.LayoutEventHandler(this.LinksPane_Layout);
            this._borderPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        protected override void OnColorSchemeChanged()
        {
            if ( _colorScheme != null )
            {
                BackColor = _colorScheme.GetColor( "LinksPane.Group" );
            }
        }

        public void DisplayLinks( IResourceList resList, ILinksPaneFilter filter )
        {
            SetResourceList( resList, filter );

            _scrollY = 0;
            SuspendLayout();
            try
            {
                if ( _resourceList == null || _resourceList.Count == 0 )
                {
                    RemoveAllControls();
                    DisposePooledControls();
                    _borderPanel.Invalidate();
                }
                else
                {
                    UpdateLinksPane();
                }
            }
            finally
            {
                ResumeLayout();
            }
        }

        /**
         * Moves to the pool or disposes the controls added to the links pane.
         */
        
        private void RemoveAllControls()
        {
            _linkTypeLabelPool.MoveControlsToPool();
            _resourceLinkLabelPool.MoveControlsToPool();
            _actionLabelPool.MoveControlsToPool();
            _separatorPool.MoveControlsToPool();
            for ( int i = _borderPanel.Controls.Count-1; i >= 0; i-- )
            {
                Control ctl = _borderPanel.Controls [i];
                if ( !_linkTypeLabelPool.IsPooledControl( ctl ) && !_resourceLinkLabelPool.IsPooledControl( ctl )
                    && !_actionLabelPool.IsPooledControl( ctl ) && !_separatorPool.IsPooledControl( ctl ) )
                {
                    _borderPanel.Controls.RemoveAt( i );
                    ctl.Dispose();
                }
            }
        }

        /**
         * Disposes of all controls that have been moved to the pool and not reused.
         */

        private void DisposePooledControls()
        {
            _resourceLinkLabelPool.RemovePooledControls();
            _linkTypeLabelPool.RemovePooledControls();
            _actionLabelPool.RemovePooledControls();
            _separatorPool.RemovePooledControls();
        }

        protected override void UpdateLinksPane()
        {
            if ( Core.State == CoreState.ShuttingDown )
            {
                return;
            }
            if ( _resourceList != null && _resourceList.Count == 1 && _resourceList [0].IsDeleting )
            {
                return;
            }

            RemoveAllControls();
            _borderPanel.AutoScrollPosition = new Point( 0, 0 );

            if ( _resourceList != null && _resourceList.Count > 0 )
            {
                int curX = 0;
                int curY = 4 - _scrollY;
                if ( _resourceList.Count == 1 )
                {
                    LinkSection section = BuildLinksForResource( _resourceList[ 0 ] );
                    ShowLinksFromSection( section, ref curX, ref curY );
                    ShowCustomPropertiesForResource( _resourceList[ 0 ], ref curX, ref curY );
                }
                else
                {
                    AddLinkTypeLabel( ref curX, ref curY, _resourceList.Count + " resources selected" );
                    curY += 8;
                }

                LinksPaneActionItem[] actionItems = LinksPaneActionManager.GetManager().CreateActionLinks( _resourceList, _filter );
                if ( actionItems.Length > 0 )
                {
                    AddLinkTypeLabel( ref curX, ref curY, "Actions" );
                    foreach( LinksPaneActionItem item in actionItems )
                    {
                        JetLinkLabel lbl = (JetLinkLabel) _actionLabelPool.GetControl();
                        lbl.AutoSize  = false;
                        lbl.Enabled   = item.Enabled;
                        lbl.Text      = item.Text;
                        lbl.Tag       = item.Action;
                        lbl.BackColor = Color.FromArgb( 0, SystemColors.Control );
                        lbl.Bounds    = new Rectangle( _linkLabelX, curY, Width - _linkLabelX - 4, lbl.PreferredSize.Height );
                        curY += 18;
                    }
                }

                UpdateLinkLabelTooltips();
            }

            DisposePooledControls();
            _borderPanel.Invalidate();
        }

        /**
         * Shows the custom properties for the specified resource.
         */

        private void ShowCustomPropertiesForResource( IResource res, ref int curX, ref int curY )
        {
            foreach( IResource propTypeRes in ResourceTypeHelper.GetCustomProperties() )
            {
                int propID = propTypeRes.GetIntProp( "ID" );
                if ( res.HasProp( propID ) )
                {
                    AddLinkTypeLabel( ref curX, ref curY, Core.ResourceStore.PropTypes.GetPropDisplayName( propID ) );
                    string propText = GetCustomPropText( res, propID );
                    AddRegularLabel( new Rectangle( _linkLabelX, curY, Width-_linkLabelX, 16 ), propText );
                    curY += 16;
                }
            }
        }

        private void ShowLinksFromSection( LinkSection section, ref int curX, ref int curY )
        {
            while( section != null )
            {
                AddLinkTypeLabel( ref curX, ref curY, section.Name );
                
                LinkItem lastLinkItem = null;
                for( int i=0; i < 5 && i < section.LinkItems.Count; i++ )
                {
                    LinkItem item = (LinkItem) section.LinkItems [i];
                    Control ctl = AddResourceLabel( item.Resource, item.PropId, _resourceList [0], curY );
                    ctl.Tag = item.ToolTip;
                    curY += ctl.Height;
                    lastLinkItem = item;
                }
                if ( section.LinkItems.Count > 5 )
                {
                    JetLinkLabel seeAllLabel = AddLinkLabel( new Rectangle( _linkLabelX, curY, Width-_linkLabelX, 16 ),
                        "See All (" + section.LinkItems.Count + ")..." );
                    seeAllLabel.Tag = lastLinkItem.PropId;
                    seeAllLabel.Click += OnSeeAllClick;
                    curY += 18;
                }
                else
                {
                    curY += 2;
                }

                if ( section.Separator )
                {
                    AddLinkGroupSeparator( ref curY );
                }
                
                section = section.NextSection;
            }
        }

        /**
         * Creates a link type label with the specified name.
         */

        private Control AddLinkTypeLabel( ref int curX, ref int curY, string text )
        {
            JetLinkLabel typeLabel = (JetLinkLabel) _linkTypeLabelPool.GetControl();

            int typeLabelHeight = (int) ( 17 * Core.ScaleFactor.Height );
            typeLabel.Text = text;
            typeLabel.Bounds = new Rectangle( _linkTypeLabelX, curY, Width - _linkTypeLabelX - 4, typeLabelHeight );
            typeLabel.Visible = true;
            typeLabel.ForeColor = ColorScheme.GetColor( _colorScheme, "LinksPane.LinkTypeText", Color.Black );
            curY += typeLabelHeight;
            return typeLabel;
        }

        /**
         * Adds an icon and label for a single resource.
         */

        private ResourceLinkLabel AddResourceLabel( IResource linkRes, int linkType, IResource linkOwnerRes,
            int curY )
        {
            ResourceLinkLabel linkLabel = (ResourceLinkLabel) _resourceLinkLabelPool.GetControl();
            linkLabel.Resource = linkRes;
            linkLabel.LinkOwnerResource = linkOwnerRes;
            linkLabel.LinkType = Math.Abs( linkType );
            linkLabel.Bounds = new Rectangle( _linkLabelX, curY, Width - _linkLabelX - 4, 
                (int) ( 19 * Core.ScaleFactor.Height ) );
            linkLabel.EndEllipsis = true;
            return linkLabel;
        }

        /**
         * Creates a resource link label for the pool.
         */
        
        private Control OnCreateResourceLinkLabel()
        {
            ResourceLinkLabel linkLabel = new ResourceLinkLabel();
            linkLabel.LinkContextMenu += linkLabel_LinkContextMenu;
            linkLabel.ResourceChanged += HandleLinkedResourceChanged;
            linkLabel.BackColor = Color.FromArgb( 0, DefaultBackColor );
            return linkLabel;
        }

        private void OnDisposeResourceLinkLabel( Control ctl )
        {
            ResourceLinkLabel linkLabel = ctl as ResourceLinkLabel;
            if ( linkLabel != null )
            {
                linkLabel.LinkContextMenu -= linkLabel_LinkContextMenu;
                linkLabel.ResourceChanged -= HandleLinkedResourceChanged;
            }
        }

        /**
         * Adds a regular label to the list of controls.
         */

        private JetLinkLabel AddRegularLabel( Rectangle bounds, string text )
        {
            JetLinkLabel lbl = new JetLinkLabel();
            lbl.Bounds = bounds;
            lbl.Text = text;
            lbl.AutoSize = true;
            lbl.ClickableLink = false;
            lbl.BackColor = Color.FromArgb( 0, DefaultBackColor );
            lbl.Font = new Font( "Tahoma", 8 );
            lbl.EndEllipsis = true;
            _borderPanel.Controls.Add( lbl );
            return lbl;            
        }

        /**
         * Adds a link-like label to the list of controls.
         */

        private JetLinkLabel AddLinkLabel( Rectangle bounds, string text )
        {
            JetLinkLabel linkLabel = AddRegularLabel( bounds, text );
            linkLabel.ClickableLink = true;
            linkLabel.Font = _linkLabelFont;
            return linkLabel;
        }

        private Control OnCreateLinkTypeLabel()
        {
            JetLinkLabel typeLabel = new JetLinkLabel();
            typeLabel.Font = _linkTypeLabelFont;
            typeLabel.AutoSize = true;
            typeLabel.ClickableLink = false;
            typeLabel.BackColor = Color.FromArgb( 0, DefaultBackColor );
            return typeLabel;
        }

        private Control OnCreateSeparator()
        {
            GradientBar bar = new GradientBar();
            bar.Size = new Size( _borderPanel.Width, 1 );
            bar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            bar.StartColor = Color.FromArgb( 184, 181, 200 );
            bar.EndColor = bar.StartColor;
            bar.GradientMode = LinearGradientMode.Vertical;
            return bar;
        }

        private Control OnCreateActionLabel()
        {
            JetLinkLabel lbl = new JetLinkLabel();
            lbl.Click += LinksPaneActionManager.GetManager().OnActionLabelClick;
            lbl.Font = _linkLabelFont;
            return lbl;
        }

        private void AddLinkGroupSeparator( ref int curY )
        {
            Control separator = _separatorPool.GetControl();
            separator.Location = new Point( 0, curY );
            curY += 3;
        }
        
        private void linkLabel_LinkContextMenu( object sender, ResourceLinkLabelEventArgs e )
        {
            ResourceLinkLabel linkLabel = (ResourceLinkLabel) sender;
            ShowLinkContextMenu( linkLabel, e );
        }

        private void HandleLinkedResourceChanged( object sender, ResourcePropEventArgs e )
        {
            if ( e.Resource.HasProp( Core.Props.IsDeleted ) )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( UpdateLinksPane ) );
            }
        }

        /**
         * When the "See All..." label is clicked, displays all links of the
         * specified type in the resource browser.
         */

        private void OnSeeAllClick( object sender, EventArgs e )
        {
            if ( _resourceList != null && _resourceList.Count == 1 )
            {
                IResource res = _resourceList [0];
                Control linkLabel = (Control) sender;
                int linkType = (int) linkLabel.Tag;

                IResourceList links;
                if ( linkType < 0 )
                {
                    links = res.GetLinksToLive( null, -linkType );
                }
                else if ( Core.ResourceStore.PropTypes [linkType].HasFlag( PropTypeFlags.DirectedLink ) )
                {
                    links = res.GetLinksFromLive( null, linkType );
                }
                else
                {
                    links = res.GetLinksOfTypeLive( null, linkType );
                }
                links = ResourceTypeHelper.ExcludeUnloadedPluginResources( links );

                Core.UIManager.BeginUpdateSidebar();
                Core.TabManager.SelectResourceTypeTab( null );
                Core.UIManager.EndUpdateSidebar();

                ResourceListDisplayOptions options = new ResourceListDisplayOptions();
                options.Caption = _store.PropTypes.GetPropDisplayName( linkType ) + " links to " + res.DisplayName;
                options.TabFilter = false;
                options.SetTransientContainer( Core.ResourceTreeManager.ResourceTreeRoot,
                    StandardViewPanes.ViewsCategories );
                Core.ResourceBrowser.DisplayResourceList( null, links, options );
                Core.ResourceBrowser.FocusResourceList();
            }
        }

        private void LinksPane_Layout( object sender, LayoutEventArgs e )
        {
            UpdateLinkLabelTooltips();
        }

        /// <summary>
        /// For the link labels which are wider than the links pane, shows their
        /// full text in the tooltips.
        /// </summary>
        private void UpdateLinkLabelTooltips()
        {
            foreach( Control control in _borderPanel.Controls )
            {
                if ( control is ResourceLinkLabel )
                {
                    ResourceLinkLabel linkLabel = (ResourceLinkLabel) control;
                    linkLabel.NameLabel.Width = Width - 4 - linkLabel.NameLabel.Left - linkLabel.Left;
                    string linkTooltip = (string) linkLabel.Tag;
                    if ( linkTooltip != null )
                    {
                        _toolTip.SetToolTip( linkLabel.NameLabel, linkTooltip );
                    }
                    else if ( linkLabel.Left + linkLabel.PreferredWidth > this.Width )
                    {
                        _toolTip.SetToolTip( linkLabel.NameLabel, linkLabel.NameLabel.Text );
                    }
                    else
                    {
                        _toolTip.SetToolTip( linkLabel.NameLabel, null );
                    }
                }
                else if ( control is JetLinkLabel )
                {
                    JetLinkLabel linkLabel = (JetLinkLabel) control;
                    linkLabel.Width = Width - 4 - linkLabel.Left;
                    if ( linkLabel.Left + linkLabel.PreferredWidth > this.Width )
                    {
                        _toolTip.SetToolTip( linkLabel, linkLabel.Text );
                    }
                    else
                    {
                        _toolTip.SetToolTip( linkLabel, null );
                    }
                }
            }
        }

        private void _borderPanel_Paint( object sender, PaintEventArgs e )
        {
            using( Pen dividerPen = new Pen( Color.FromArgb( 184, 181, 200 ) ) )
            {
                e.Graphics.DrawLine( dividerPen, 0, 0, 0, _borderPanel.Height-1 );
            }
        }
    }
}
