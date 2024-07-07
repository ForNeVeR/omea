// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A control that displays a resource icon and its name as a clickable link.
	/// </summary>
    public class ResourceLinkLabel : System.Windows.Forms.UserControl
    {
        private GUIControls.ImageListPictureBox _iconBox;
        private JetLinkLabel _label;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private IResource _resource;
        private IResource _linkOwnerResource;
        private int _linkType;
        private Point _dragStartPoint;
        private bool _clickableLink = true;
        private bool _executeDoubleClickAction = true;
        private bool _showIcon = true;
        private IResourceList _resourceList;

        private static Color _linkColor = Color.FromArgb( 70, 70, 211 );

        public event ResourceLinkLabelEventHandler LinkContextMenu;
        public event CancelEventHandler ResourceLinkClicked;
        public event ResourceDragEventHandler ResourceDragOver;
        public event ResourceDragEventHandler ResourceDrop;
        public event ResourcePropEventHandler ResourceChanged;

        public ResourceLinkLabel()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

        	SetStyle( ControlStyles.UserPaint
        		| ControlStyles.SupportsTransparentBackColor
        		| ControlStyles.AllPaintingInWmPaint
        		| ControlStyles.CacheText
        	          , true );

        	_label.BackColor = Color.FromArgb( 0, DefaultBackColor );
            _label.ForeColor = _linkColor;

            if ( ICore.Instance != null )
            {
                _iconBox.ImageList = Core.ResourceIconManager.ImageList;
            }
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

                if ( _resourceList != null )
                {
                    _resourceList.Dispose();
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
            this._iconBox = new JetBrains.Omea.GUIControls.ImageListPictureBox();
            this._label = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this.SuspendLayout();
            //
            // _iconBox
            //
            this._iconBox.ImageIndex = 0;
            this._iconBox.ImageLeftTopPoint = new System.Drawing.Point(0, 0);
            this._iconBox.Location = new System.Drawing.Point(1, 2);
            this._iconBox.Name = "_iconBox";
            this._iconBox.Size = new System.Drawing.Size(16, 16);
            this._iconBox.TabIndex = 0;
            this._iconBox.Text = "imageListPictureBox1";
            this._iconBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this._iconBox_MouseUp);
            this._iconBox.DoubleClick += new System.EventHandler(this.OnResourceIconDoubleClick);
            this._iconBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnLinkMouseMove);
            this._iconBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnLinkMouseDown);
            //
            // _label
            //
            this._label.AllowDrop = true;
            this._label.Cursor = System.Windows.Forms.Cursors.Hand;
            this._label.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._label.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(70)), ((System.Byte)(70)), ((System.Byte)(211)));
            this._label.Location = new System.Drawing.Point(21, 2);
            this._label.Name = "_label";
            this._label.Size = new System.Drawing.Size(34, 17);
            this._label.TabIndex = 1;
            this._label.DragEnter += new System.Windows.Forms.DragEventHandler(this.OnLinkDragEnter);
            this._label.Resize += new System.EventHandler(this._label_Resize);
            this._label.DragLeave += new System.EventHandler(this.OnLinkDragLeave);
            this._label.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnLinkMouseUp);
            this._label.MouseEnter += new System.EventHandler(this.OnLinkEnter);
            this._label.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnLinkDrop);
            this._label.MouseLeave += new System.EventHandler(this.OnLinkLeave);
            this._label.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnLinkMouseDown);
            //
            // ResourceLinkLabel
            //
            this.Controls.Add(this._label);
            this.Controls.Add(this._iconBox);
            this.Name = "ResourceLinkLabel";
            this.Size = new System.Drawing.Size(256, 20);
            this.Enter += new System.EventHandler(this.ResourceLinkLabel_Enter);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ResourceLinkLabel_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ResourceLinkLabel_KeyDown);
            this.Leave += new System.EventHandler(this.ResourceLinkLabel_Leave);
            this.ResumeLayout(false);

        }
        #endregion

        public IResource Resource
        {
            get { return _resource; }
            set
            {
                if ( _resource != value )
                {
                    if ( _resourceList != null )
                    {
                        _resourceList.ResourceChanged -= new ResourcePropIndexEventHandler( HandleResourceChanged );
                        _resourceList.Dispose();
                    }
                    _resource = value;
                    if ( _resource != null )
                    {
                        _resourceList = _resource.ToResourceListLive();
                        _resourceList.ResourceChanged += new ResourcePropIndexEventHandler( HandleResourceChanged );
                        UpdateContents();
                    }
                }
            }
        }

        private void UpdateContents()
        {
            if ( _showIcon )
            {
                int[] overlayIconIndices = Core.ResourceIconManager.GetOverlayIconIndices( _resource );
                if ( overlayIconIndices != null && overlayIconIndices.Length > 0 )
                {
                    int[] indices = new int [overlayIconIndices.Length+1];
                    indices [0] = Core.ResourceIconManager.GetIconIndex( _resource );
                    Array.Copy( overlayIconIndices, 0, indices, 1, overlayIconIndices.Length );
                    _iconBox.ImageIndices = indices;
                }
                else
                {
                    _iconBox.ImageIndex = Core.ResourceIconManager.GetIconIndex( _resource );
                }
            }
            Text = _resource.DisplayName;
        }

        public string PostfixText
        {
            get { return _label.PostfixText; }
            set { _label.PostfixText = value; }
        }

        [DefaultValue( true )]
        public bool AutoSize
        {
            get { return _label.AutoSize; }
            set { _label.AutoSize = value; }
        }

        [DefaultValue( false )]
        public bool EndEllipsis
        {
            get { return _label.EndEllipsis; }
            set { _label.EndEllipsis = value; }
        }

        private void HandleResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if ( e.ChangeSet.IsDisplayNameAffected || _showIcon )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( RefreshResource ) );
            }
            if ( ResourceChanged != null )
            {
                ResourceChanged( this, new ResourcePropEventArgs( e.Resource, e.ChangeSet ) );
            }
        }

        private void RefreshResource()
        {
            UpdateContents();
            Invalidate();   // redraw the focus frame for new text size
        }

        public IResource LinkOwnerResource
        {
            get { return _linkOwnerResource; }
            set { _linkOwnerResource = value; }
        }

        public int LinkType
        {
            get { return _linkType; }
            set { _linkType = value; }
        }

        [DefaultValue(true)]
        public bool ClickableLink
        {
            get { return _clickableLink; }
            set
            {
                _clickableLink = value;
                _label.ClickableLink = value;
            }
        }

        [DefaultValue(true)]
        public bool ShowIcon
        {
            get { return _showIcon; }
            set
            {
                if ( _showIcon != value )
                {
                    _showIcon = value;
                    _iconBox.Visible = _showIcon;
                    _label.Left = _showIcon ? 21 : 1;
                    if ( _showIcon )
                    {
                        UpdateContents();
                    }
                }
            }
        }

        public Control NameLabel
        {
            get { return _label; }
        }

        public int PreferredWidth
        {
            get { return _label.Left + _label.PreferredWidth + 2; /* extra space for the focus frame */ }
        }

        /**
         * Enables or disables executing the double-click action when an item is double-clicked
         * or Enter is pressed on an item.
         */

        [DefaultValue(true)]
        public bool ExecuteDoubleClickAction
        {
            get { return _executeDoubleClickAction; }
            set { _executeDoubleClickAction = value; }
        }

        /**
         * When the text of the control is changed, updates the link label
         * text to match.
         */

        protected override void OnTextChanged( EventArgs e )
        {
            base.OnTextChanged( e );
            _label.Text = Text;
        }

        /**
         * When the mouse is pressed down on a link label or resource icon,
         * remembers the coordinates so that we can check to start a drag later.
         */

        private void OnLinkMouseDown( object sender, System.Windows.Forms.MouseEventArgs e )
        {
            if ( e.Button == MouseButtons.Left )
            {
                _dragStartPoint = new Point( e.X, e.Y );
            }
            Focus();
            Invalidate();
        }

        /**
         * WHen the mouse is moved over the link label or resource icon,
         * checks if it's time to start a drag.
         */

        private void OnLinkMouseMove( object sender, System.Windows.Forms.MouseEventArgs e )
        {
            if ( e.Button == MouseButtons.Left && _dragStartPoint.X != -1 && _resource != null )
            {
                int dx = Math.Abs( _dragStartPoint.X - e.X );
                int dy = Math.Abs( _dragStartPoint.Y - e.Y );
                if ( dx >= 4 || dy >= 4 )
                {
                    _dragStartPoint = new Point( -1, -1 );
                    DataObject dataObj = new DataObject();
                    dataObj.SetData( typeof(IResourceList), _resource.ToResourceList() );
                    DoDragDrop( dataObj, DragDropEffects.Link | DragDropEffects.Move );
                }
            }
        }

        /**
         * When a link label is clicked, displays the respective resource.
         * When it is right-clicked, shows the resource popup menu.
         */

        private void OnLinkMouseUp( object sender, System.Windows.Forms.MouseEventArgs e )
        {
            _dragStartPoint = new Point( -1, -1 );
            if ( e.Button == MouseButtons.Left && _clickableLink && ClientRectangle.Contains( e.X, e.Y ) )
            {
                CancelEventArgs args = new CancelEventArgs( false );
                if ( ResourceLinkClicked != null )
                {
                    ResourceLinkClicked( this, args );
                }
                if ( !args.Cancel )
                {
                    ActionContext context = GetActionContext( ActionContextKind.Other );
                    if ( !Core.ActionManager.ExecuteLinkClickAction( context ) )
                    {
                        if ( !_resource.IsDeleted )
                        {
                            // the link label may get reused, and _resource may be replaced
                            IResource resourceToDisplay = _resource;
                            IResource linkOwner = _linkOwnerResource;
                            Core.UIManager.DisplayResourceInContext( _resource );
                            if ( linkOwner != null && Core.ResourceBrowser.OwnerResource != null &&
                                 !Core.ResourceBrowser.VisibleResources.Contains( resourceToDisplay ) )
                            {
                                Core.ResourceBrowser.SelectResource( linkOwner );
                            }
                        }
                    }
                }
            }
            else if ( e.Button == MouseButtons.Right )
            {
                if ( LinkContextMenu != null )
                {
                    LinkContextMenu( this, new ResourceLinkLabelEventArgs( new Point( e.X, e.Y ),
                                                                            _resource) );
                }
            }
        }

        private ActionContext GetActionContext( ActionContextKind kind )
        {
            IResourceList resList = null;
            if ( _resource != null )
            {
                resList = _resource.ToResourceList();
            }
            ActionContext context = new ActionContext( kind, null, resList );
            context.SetLinkTarget( _linkType, _linkOwnerResource );
            return context;
        }

        private void _iconBox_MouseUp( object sender, System.Windows.Forms.MouseEventArgs e )
        {
            if ( e.Button == MouseButtons.Right )
            {
                if ( LinkContextMenu != null )
                {
                    LinkContextMenu( this, new ResourceLinkLabelEventArgs( new Point( e.X, e.Y ),
                                                                           _resource) );
                }
            }
        }

        /// <summary>
        /// When a resource icon is double-clicked, executes the double-click action
        /// of the resource.
        /// </summary>
        private void OnResourceIconDoubleClick( object sender, System.EventArgs e )
        {
            if ( _executeDoubleClickAction && _resource != null )
            {
                Core.ActionManager.ExecuteDoubleClickAction( _resource.ToResourceList() );
            }
        }

        /**
         * When the cursor enters the link label, highlights it as a link.
         */

        private void OnLinkEnter( object sender, System.EventArgs e )
        {
        }

        private void OnLinkLeave( object sender, System.EventArgs e )
        {
            _dragStartPoint = new Point( -1, -1 );
        }

        /**
         * When a IResourceList drag enters the link label, highlights it
         * as a drop target.
         */

        private void OnLinkDragEnter( object sender, System.Windows.Forms.DragEventArgs e )
        {
            if ( e.Data.GetDataPresent( typeof(IResourceList) ) )
            {
                _label.BackColor = SystemColors.Highlight;
                _label.ForeColor = SystemColors.HighlightText;

                IResourceList resList = (IResourceList) e.Data.GetData( typeof(IResourceList) );
                if ( ResourceDragOver != null )
                {
                    ResourceDragEventArgs args = new ResourceDragEventArgs( _resource, resList );
                    ResourceDragOver( this, args );
                    e.Effect = args.Effect;
                }
                else
                {
                    if ( Core.UIManager.CanDropResource( _resource, resList ) )
                        e.Effect = DragDropEffects.Link;
                    else
                        e.Effect = DragDropEffects.None;
                }
            }
        }

        /**
         * When a drag leaves the link label, removes its highlighting.
         */

        private void OnLinkDragLeave( object sender, System.EventArgs e )
        {
            _label.BackColor = BackColor;
            _label.ForeColor = _linkColor;
        }

        /**
         * When a resource is dropped over the link label, pops up the "Add Link"
         * dialog.
         */

        private void OnLinkDrop( object sender, System.Windows.Forms.DragEventArgs e )
        {
            if ( e.Data.GetDataPresent( typeof(IResourceList ) ) )
            {
                IResourceList resList = (IResourceList) e.Data.GetData( typeof( IResourceList ) );
                _label.BackColor = BackColor;
                _label.ForeColor = _linkColor;

                if ( ResourceDrop != null )
                {
                    ResourceDrop( this, new ResourceDragEventArgs( _resource, resList ) );
                }
                else
                {
                    Core.UIManager.ProcessResourceDrop( _resource, resList );
                }
            }
        }

        private void ResourceLinkLabel_Paint( object sender, System.Windows.Forms.PaintEventArgs e )
        {
            if ( ContainsFocus )
            {
                ControlPaint.DrawFocusRectangle( e.Graphics, new Rectangle( 0, 0,
                    _label.Width+22, Height ) );
            }
        }

        private void ResourceLinkLabel_Enter(object sender, System.EventArgs e)
        {
            Invalidate();
        }

        private void ResourceLinkLabel_Leave(object sender, System.EventArgs e)
        {
            Invalidate();
        }

        protected override void OnBackColorChanged( EventArgs e )
        {
            base.OnBackColorChanged( e );
            _label.BackColor = BackColor;
            _iconBox.BackColor = BackColor;
        }

        private void ResourceLinkLabel_KeyDown( object sender, KeyEventArgs e )
        {
            if ( Core.ActionManager.ExecuteKeyboardAction( GetActionContext( ActionContextKind.Keyboard ),
                e.KeyCode | e.Modifiers ) )
            {
                e.Handled = true;
            }
        }

        private void _label_Resize(object sender, System.EventArgs e)
        {
            Width = _label.Width+23;
        }

		protected override void OnPaint(PaintEventArgs e)
		{
			if( BackColor != Color.Transparent )
			{
				using( Brush brush = new SolidBrush( BackColor ) )
					e.Graphics.FillRectangle( brush, ClientRectangle );
			}
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			// Center the controls vertically
			_iconBox.Top = 0 + (Height - _iconBox.Height) / 2;
			_label.Top = 0 + (Height - _label.Height) / 2;
		}
	}

    public class ResourceLinkLabelEventArgs: EventArgs
    {
        private Point _pnt;
        private IResource _res;

        internal ResourceLinkLabelEventArgs( Point pnt, IResource res )
        {
            _pnt = pnt;
            _res = res;
        }

        public Point Point
        {
            get { return _pnt; }
        }

        public IResource Resource
        {
            get { return _res; }
        }

    }

    public delegate void ResourceLinkLabelEventHandler( object sender, ResourceLinkLabelEventArgs e );
}
