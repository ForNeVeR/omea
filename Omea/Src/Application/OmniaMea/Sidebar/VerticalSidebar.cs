// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    public enum SidebarSide { Left, Right };

    /// <summary>
    /// A pane with a row of vertical buttons and a stack of panes connected to those buttons.
    /// </summary>
    internal class VerticalSidebar : UserControl, ISidebar, IContextProvider
	{
        private const int _minPaneHeight = 120;

        private Panel _contentPane;
        private SidebarButtons _paneButtons;

        private PaneData _lastActivePaneData;
        private ColorScheme _colorScheme;
        private SidebarSide _side = SidebarSide.Left;

        private int _expandedWidth;
        private int _updateCount;
        private bool _expanded;
        private readonly HashSet _populatedPanes = new HashSet();

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components;

		internal class PaneData
		{
            public string PaneId;
            public int PaneHeight;
            public SidebarPaneBackground PaneBackground;
            public AbstractViewPane Pane;
            public PaneCaption PaneCaption;
            public Splitter PaneSplitter;

		    public PaneData( string paneID, SidebarPaneBackground paneBackground, AbstractViewPane pane,
                             PaneCaption paneCaption, Splitter paneSplitter, int paneHeight )
		    {
		        PaneId = paneID;
                PaneBackground = paneBackground;
		        Pane = pane;
                PaneCaption = paneCaption;
                PaneSplitter = paneSplitter;
                PaneHeight = paneHeight;
		    }
		}

        public VerticalSidebar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            SetStyle( ControlStyles.Selectable, false );
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
            this._paneButtons = new SidebarButtons();
            this._contentPane = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            //
            // _paneButtons
            //
            this._paneButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._paneButtons.Name = "_paneButtons";
            this._paneButtons.Size = new System.Drawing.Size(150, 30);
            this._paneButtons.TabIndex = 0;
            //
            // _contentPane
            //
            this._contentPane.Dock = System.Windows.Forms.DockStyle.Fill;
            this._contentPane.Location = new System.Drawing.Point(30, 0);
            this._contentPane.Name = "_contentPane";
            this._contentPane.Size = new System.Drawing.Size(120, 150);
            this._contentPane.TabIndex = 1;
            //
            // VerticalSidebar
            //
            this.Controls.Add(this._contentPane);
            this.Controls.Add(this._paneButtons);
            this.Name = "VerticalSidebar";
            this.ResumeLayout(false);

        }
		#endregion

        public event EventHandler PaneAdded;
        public event EventHandler ExpandedChanged;

        [DefaultValue(SidebarSide.Left)]
        public SidebarSide Side
	    {
	        get { return _side;  }
	        set { _side = value; }
	    }

        public int ExpandedWidth
        {
            get
            {
                return Expanded ? Width : _expandedWidth;
            }
            set
            {
                _expandedWidth = value;
                if ( Expanded )
                {
                    Width = value;
                }
            }
        }

        public bool Expanded
        {
//            get { return _expanded; }
            get { return true; }
        }

        private void SetExpanded( bool value )
        {
            if ( _expanded != value )
            {
                _expanded = value;
                if ( ExpandedChanged != null )
                {
                    ExpandedChanged( this, EventArgs.Empty );
                }
            }
        }

        public int PanesCount
        {
            get { return _paneButtons.Items.Count; }
        }

	    public ColorScheme ColorScheme
	    {
	        get { return _colorScheme; }
	        set
            {
                _colorScheme = value;
                foreach( ToolStripButton btn in _paneButtons.Items )
                {
                    PaneData paneData = (PaneData) btn.Tag;
                    paneData.PaneCaption.ColorScheme = _colorScheme;
                    paneData.PaneBackground.ColorScheme = _colorScheme;
                    IColorSchemeable schemable = paneData.Pane as IColorSchemeable;
                    if ( schemable != null )
                    {
                        schemable.ColorScheme = _colorScheme;
                    }
                }
            }
	    }

        public void RegisterPane( AbstractViewPane pane, string id, string caption, Image icon )
        {
            PaneCaption paneCaption = new PaneCaption();
            paneCaption.Text = caption;
            paneCaption.Dock = DockStyle.Top;
            paneCaption.CaptionButtons = PaneCaptionButtons.Minimize;
            paneCaption.Click += OnPaneCaptionClick;
            paneCaption.MinimizeClick += OnPaneMinimize;
            paneCaption.ColorScheme = _colorScheme;
            _contentPane.Controls.Add( paneCaption );
            _contentPane.Controls.SetChildIndex( paneCaption, 0 );

            int paneHeight = pane.Height;

            SidebarPaneBackground background = new SidebarPaneBackground();
            background.SetContents( pane );
            background.ColorScheme = _colorScheme;
            background.Dock = DockStyle.Top;
            background.Visible = false;
            background.Height = 0;
            _contentPane.Controls.Add( background );
            _contentPane.Controls.SetChildIndex( background, 0 );

            pane.ShowSelection = false;
            pane.Enter += OnPaneEnter;
            pane.Leave += OnPaneLeave;

            Splitter paneSplitter = new Splitter();
            paneSplitter.Dock = DockStyle.Top;
            paneSplitter.Height = 3;
            _contentPane.Controls.Add( paneSplitter );
            _contentPane.Controls.SetChildIndex( paneSplitter, 0 );

            PaneData paneData = new PaneData( id, background, pane, paneCaption, paneSplitter, paneHeight );
            paneCaption.Tag = paneData;

            IColorSchemeable schemeable = pane as IColorSchemeable;
            if ( schemeable != null )
            {
                schemeable.ColorScheme = _colorScheme;
            }

            ToolStripButton button = _paneButtons.AddButton( paneData, icon, caption );
            button.CheckedChanged += OnToolbarButtonClicked;

            AdjustDockAndSplitters( null, false );
            if ( PaneAdded != null )
            {
                PaneAdded( this, EventArgs.Empty );
            }
        }

        public bool ContainsPane( string paneId )
        {
            AbstractViewPane pane = GetPane( paneId );
            return pane != null;
        }

        public AbstractViewPane GetPane( string paneId )
        {
            ToolStripButton btn = _paneButtons.GetPaneButton( paneId );
            return ((PaneData) btn.Tag).Pane;
        }

        public bool IsPaneExpanded( string paneId )
        {
            ToolStripButton btn = _paneButtons.GetPaneButton( paneId );
            return btn.Checked;
        }

        public void SetPaneExpanded( string paneId, bool expanded )
        {
            ToolStripButton btn = _paneButtons.GetPaneButton( paneId );
            btn.Checked = expanded;
        }

        public void SetPaneCaption( string paneId, string caption )
        {
            ToolStripButton btn = _paneButtons.GetPaneButton( paneId );
            ((PaneData)btn.Tag).PaneCaption.Text = btn.ToolTipText = caption;
        }

        private int GetPressedCount()
        {
            return _paneButtons.CheckedButtonsCount;
        }

        public void ActivatePane( string paneId )
        {
            AbstractViewPane pane = GetPane( paneId );
            pane.Focus();

            // hack (see #5371, #5746)
            IResource selResource = pane.SelectedResource;
            if ( selResource != null )
            {
                pane.SelectResource( selResource, false );
            }
            _paneButtons.SetButtonPressed( paneId, true );
        }

        public void PopulateViewPanes()
        {
            foreach( ToolStripButton btn in _paneButtons.Items )
            {
                if ( btn.Checked )
                {
                    PaneData paneData = (PaneData) btn.Tag;
                    if ( !_populatedPanes.Contains( paneData.PaneId ) )
                    {
                        _populatedPanes.Add( paneData.PaneId );
                        paneData.Pane.Populate();
                    }
                }
            }
            _paneButtons.CheckButtonsState();
        }

        public void UpdateActiveWorkspace()
        {
            IResource workspace = Core.WorkspaceManager.ActiveWorkspace;
            foreach( ToolStripButton btn in _paneButtons.Items )
            {
                PaneData paneData = (PaneData) btn.Tag;
                if ( _populatedPanes.Contains( paneData.PaneId ) )
                {
                    paneData.Pane.SetActiveWorkspace( workspace );
                }
            }
        }

        public void BeginUpdate()
        {
            SuspendLayout();
            _updateCount++;
        }

        public void EndUpdate()
        {
            _updateCount--;
            if ( _updateCount == 0 )
            {
                AdjustDockAndSplitters( null, false );
            }
            ResumeLayout();
        }

        /// <summary>
        /// Tries to select the resource in any of the sidebar panes.
        /// </summary>
        /// <param name="res"></param>
        public void ForceSelectResource( IResource res )
        {
            foreach( ToolStripButton btn in _paneButtons.Items )
            {
                PaneData paneData = (PaneData) btn.Tag;
                CheckPopulatePane( paneData );
                if ( paneData.Pane.SelectResource( res, false ) )
                {
                    ActivatePane( paneData.PaneId );
                    break;
                }
            }
        }

        internal SidebarState CurrentState
        {
            get
            {
                int[] paneHeights = new int [ PanesCount ];
                int activePaneIndex = -1;
                IResource selectedResource = null;

                for( int i = 0; i < _paneButtons.Items.Count; i++ )
                {
                    ToolStripButton btn = (ToolStripButton)_paneButtons.Items[ i ];
                    PaneData paneData = (PaneData) btn.Tag;
                    if ( !btn.Checked )
                    {
                        paneHeights[ i ] = -1;
                    }
                    else
                    {
                        paneHeights[ i ] = paneData.PaneBackground.Height;
                        if ( paneData == _lastActivePaneData )
                        {
                            activePaneIndex = i;
                            selectedResource = paneData.Pane.SelectedResource;
                        }
                    }
                }

                return new SidebarState( paneHeights, activePaneIndex, selectedResource );
            }
            set
            {
                if ( value == null )
                    return;

                int[] paneHeights = value.PaneHeights;
                bool foundSelectedNode = false;
                BeginUpdate();
                for( int i = 0; i < _paneButtons.Items.Count; i++ )
                {
                    if ( i >= paneHeights.Length )
                        break;

                    ToolStripButton btn = (ToolStripButton)_paneButtons.Items[ i ];
                    PaneData paneData = (PaneData) btn.Tag;
                    if ( paneHeights [i] < 0 )
                    {
                        _paneButtons.SetButtonPressed( paneData.PaneId, false );
                    }
                    else
                    {
                        _paneButtons.SetButtonPressed( paneData.PaneId, true );
                        if ( paneHeights [i] > 0 )
                        {
                            paneData.PaneBackground.Height = paneHeights [i];
                        }
                    }
                    if ( i == value.ActivePaneIndex )
                    {
                        _lastActivePaneData = paneData;
                        if( value.SelectedResource != null )
                        {
                            SetActivePane( paneData.Pane );
                            if ( paneData.Pane.SelectResource( value.SelectedResource, true ) )
                            {
                                if ( Core.State == CoreState.StartingPlugins )
                                {
                                    paneData.Pane.UpdateSelection();
                                }
                                else
                                {
                                    paneData.Pane.AsyncUpdateSelection();
                                }
                                foundSelectedNode = true;
                            }
                            else
                            {
                                value.SelectedResource = null;
                            }
                        }
                    }
                }
                if ( !foundSelectedNode )
                {
                    Core.ResourceBrowser.DisplayResourceList( null, Core.ResourceStore.EmptyResourceList, "", null );
                }
                EndUpdate();
                UpdatePaneSizes( _minPaneHeight );
            }
        }

        public void FocusActivePane()
        {
            if ( _lastActivePaneData != null )
            {
                _lastActivePaneData.Pane.Focus();
                _lastActivePaneData.Pane.ShowSelection = true;
            }
        }

        public string ActivePaneId
        {
            get
            {
                return ( _lastActivePaneData == null ) ? null : _lastActivePaneData.PaneId;
            }
        }

        private void OnToolbarButtonClicked(object sender, EventArgs e)
        {
            ToolStripButton btn = (ToolStripButton) sender;
            SuspendLayout();
            try
            {
                PaneData paneData = (PaneData) btn.Tag;
                if ( _updateCount == 0 )
                {
                    AdjustDockAndSplitters( paneData, btn.Checked );
                }

                bool focusedPaneHidden = false;

                if ( btn.Checked )
                {
                    CheckPopulatePane( paneData );
                    paneData.PaneBackground.Height = paneData.PaneHeight;
                }
                else
                {
                    focusedPaneHidden = ( paneData == _lastActivePaneData );
                    paneData.PaneHeight = paneData.PaneBackground.Height;
                    paneData.PaneBackground.Height = 0;
                }

                paneData.PaneBackground.Visible = btn.Checked;
                paneData.PaneCaption.Visible = btn.Checked;

                if ( focusedPaneHidden )
                {
                    bool foundActive = false;
                    foreach( ToolStripButton button in _paneButtons.Items )
                    {
                        if( button.Checked )
                        {
                            PaneData visibleData = (PaneData)button.Tag;
                            visibleData.Pane.Focus();
                            visibleData.Pane.UpdateSelection();
                            foundActive = true;
                            break;
                        }
                    }
                    if ( !foundActive )
                    {
                        Core.ResourceBrowser.FocusResourceList();
                    }
                }

                if ( GetPressedCount() == 0 )
                {
                    _expandedWidth = Width;
                    Width = 0;
                }
                else
                {
                    Width = _expandedWidth;
                }
            }
            finally
            {
                ResumeLayout();
            }

            if ( btn.Checked )
            {
                UpdatePaneSizes( _minPaneHeight );
            }

//            SetExpanded( GetPressedCount() != 0 );

            _paneButtons.CheckButtonsState();
        }

        private void CheckPopulatePane( PaneData paneData )
        {
            if ( !_populatedPanes.Contains( paneData.PaneId ) )
            {
                _populatedPanes.Add( paneData.PaneId );
                paneData.Pane.Populate();
                paneData.Pane.SetActiveWorkspace( Core.WorkspaceManager.ActiveWorkspace );
            }
        }

        ///<summary>
        /// Sets DockStyle.Fill for the last visible pane and DockStyle.Top
        /// for the remaining ones. Also updates splitter visibility.
        ///</summary>
        private void AdjustDockAndSplitters( PaneData toggledPane, bool toggledPaneState )
        {
            bool lastPane = true;
            int visiblePanes = 0;
            for( int i = PanesCount - 1; i >= 0; i-- )
            {
                ToolStripButton btn = (ToolStripButton)_paneButtons.Items[ i ];
                PaneData paneData = (PaneData)btn.Tag;

                // don't check Visible because it will return false if the sidebar
                // as a whole is hidden
                bool visible = btn.Checked;
                if ( paneData == toggledPane )
                    visible = toggledPaneState;

                if ( visible )
                {
                    visiblePanes++;
                    paneData.PaneBackground.Dock = lastPane ? DockStyle.Fill : DockStyle.Top;
                    paneData.PaneSplitter.Visible = !lastPane;
                    lastPane = false;
                }
                else
                {
                    paneData.PaneSplitter.Visible = false;
                }

                int paneIndex = PanesCount - 1 - i;
                _contentPane.Controls.SetChildIndex( paneData.PaneSplitter, paneIndex * 3 );
                _contentPane.Controls.SetChildIndex( paneData.PaneBackground, paneIndex * 3 + 1 );
                _contentPane.Controls.SetChildIndex( paneData.PaneCaption, paneIndex * 3 + 2 );
            }

            if ( visiblePanes > 0 )
            {
                for( int i = 0; i < PanesCount; i++ )
                {
                    ToolStripButton btn = (ToolStripButton)_paneButtons.Items[ i ];
                    PaneData paneData = (PaneData)btn.Tag;

                    bool visible = btn.Checked;
//                    if ( paneData == toggledPane )
//                        visible = toggledPaneState;

                    if ( visible )
                    {
                        PaneCaptionButtons buttons = (visiblePanes > 1) ? PaneCaptionButtons.Minimize : PaneCaptionButtons.None;
                        paneData.PaneCaption.CaptionButtons = buttons;
                    }
                }
            }

//            _paneButtons.Visible = ( _paneButtons.Items.Count > 1 );
        }

        ///<summary>
        /// Updates the heights of panes so that all visible panes get their share of height.
        ///</summary>
        private void UpdatePaneSizes( int minHeight )
        {
            int totalNeedHeight = 0;
            for( int i = 0; i < PanesCount; i++ )
            {
                ToolStripButton btn = (ToolStripButton)_paneButtons.Items[ i ];
                PaneData paneData = (PaneData)btn.Tag;
                if( btn.Checked )
                {
                    int needHeight = minHeight - paneData.PaneBackground.Height;
                    if( needHeight > 0 )
                    {
                        totalNeedHeight += needHeight;
                    }
                }
            }

            if ( totalNeedHeight > 0 )
            {
                int totalAvailHeight = 0;
                for( int i = 0; i < PanesCount; i++ )
                {
                    PaneData paneData = DataAt( i );
                    if ( paneData.PaneBackground.Height > minHeight )
                    {
                        int availHeight = paneData.PaneBackground.Height - minHeight;
                        if ( availHeight > totalNeedHeight )
                        {
                            availHeight = totalNeedHeight;
                        }
                        paneData.PaneBackground.Height = paneData.PaneBackground.Height - availHeight;
                        totalAvailHeight += availHeight;
                    }
                    else if ( paneData.PaneBackground.Height < minHeight )
                    {
                        int needHeight = minHeight - paneData.PaneBackground.Height;
                        if ( needHeight < totalAvailHeight )
                        {
                            needHeight = totalAvailHeight;
                        }
                        paneData.PaneBackground.Height = paneData.PaneBackground.Height + needHeight;
                        totalNeedHeight -= needHeight;
                        totalAvailHeight -= needHeight;
                    }
                }
            }
        }

        ///<summary>
        /// When the size of the pane changes, ensures that all visible child panes get their
        /// share of height.
        ///</summary>
        protected override void OnLayout( LayoutEventArgs levent )
        {
            base.OnLayout( levent );
            if ( levent.AffectedControl != null && levent.AffectedProperty != null )
            {
                if ( levent.AffectedProperty == "Bounds" )
                {
                    Form frm = FindForm();
                    if ( frm != null && frm.WindowState != FormWindowState.Minimized )
                    {
                        UpdatePaneSizes( 40 );
                    }
                }
            }
        }

	    private PaneData DataForPane( Control pane )
        {
            foreach( ToolStripButton btn in _paneButtons.Items )
            {
                PaneData data = (PaneData) btn.Tag;
                if ( data.Pane == pane )
                    return data;
            }
            throw new Exception( "Could not find PaneData for pane " + pane );
        }

        private PaneData DataAt( int index )
        {
        	return (PaneData) _paneButtons.Items[ index ].Tag;
        }

        private void OnPaneEnter( object sender, EventArgs e )
        {
            SetActivePane( sender as Control );
            _lastActivePaneData.PaneCaption.Active = true;
        }

        private void OnPaneLeave( object sender, EventArgs e )
        {
            PaneData paneData = DataForPane( sender as Control );
            paneData.PaneCaption.Active = false;
        }

        private void SetActivePane( Control senderCtl )
        {
            PaneData paneData = DataForPane( senderCtl );
            if ( _lastActivePaneData != null )
            {
                _lastActivePaneData.Pane.ShowSelection = false;
            }
            _lastActivePaneData = paneData;
            _lastActivePaneData.Pane.ShowSelection = true;
        }

        private static void OnPaneCaptionClick( object sender, EventArgs e )
        {
            PaneCaption caption = (PaneCaption) sender;
            AbstractViewPane pane = ((PaneData)caption.Tag).Pane;

            if ( !pane.ContainsFocus )
            {
                pane.Focus();

                // HACK: Remove when all panes are converted to JetListView
                if ( !(pane is ResourceTreePaneBase ))
                {
                    IResource node = pane.SelectedResource;
                    if( node != null )
                    {
                        pane.SelectResource( node, false );
                    }
                }
            }
        }

        private void OnPaneMinimize( object sender, EventArgs e )
        {
            PaneCaption caption = (PaneCaption) sender;
            PaneData paneData = (PaneData) caption.Tag;
            _paneButtons.SetButtonPressed( paneData.PaneId, false );
        }

	    public IActionContext GetContext( ActionContextKind kind )
	    {
            if ( _lastActivePaneData != null )
            {
                IContextProvider provider = _lastActivePaneData.Pane as IContextProvider;
                if ( provider != null )
                {
                    return provider.GetContext( kind );
                }
            }
            return null;
        }
	}

    internal class SidebarState
    {
        private readonly int[] _paneHeights;
        private readonly int _activePaneIndex;
        private IResource _selectedResource;

        internal SidebarState( int[] paneHeights, int activePaneIndex, IResource selectedResource )
        {
            _paneHeights = paneHeights;
            _activePaneIndex = activePaneIndex;
            _selectedResource = selectedResource;
        }

        public int[] PaneHeights
        {
            get { return _paneHeights; }
        }

        public int ActivePaneIndex
        {
            get { return _activePaneIndex; }
        }

        public IResource SelectedResource
        {
            get { return _selectedResource; }
            set { _selectedResource = value; }
        }

        public static SidebarState RestoreFromIni( string section )
        {
            ISettingStore ini = Core.SettingStore;
            int count = ini.ReadInt( section, "PaneCount", 0 );
            if ( count == 0 )
                return null;

            int[] paneHeights = new int[ count ];
            for( int i = 0; i < count; i++ )
            {
                paneHeights[ i ] = ini.ReadInt( section, "Pane" + i + "Height", 0 );
            }

            int activePaneIndex = ini.ReadInt( section, "ActivePaneIndex", 0 );
            if ( activePaneIndex >= paneHeights.Length )
            {
                activePaneIndex = 0;
            }

            IResource selResource = null;
            int resId = ini.ReadInt( section, "SelectedResource", -1 );
            if ( resId >= 0 )
            {
                selResource = Core.ResourceStore.TryLoadResource( resId );
            }

            return new SidebarState( paneHeights, activePaneIndex, selResource );
        }

        public void SaveToIni( string section )
        {
            ISettingStore ini = Core.SettingStore;
            ini.WriteInt( section, "PaneCount", _paneHeights.Length );
            for( int i = 0; i < _paneHeights.Length; i++ )
            {
                ini.WriteInt( section, "Pane" + i + "Height", _paneHeights [i] );
            }
            ini.WriteInt( section, "ActivePaneIndex", _activePaneIndex );
            ini.WriteInt( section, "SelectedResource", (_selectedResource == null) ? -1 : _selectedResource.Id );
        }
    }

	/// <summary>
	/// The container panel for the toolbar with icons corresponding to structural panes
	/// possible within the particular resource tab.
	/// </summary>
	internal class SidebarButtons: Panel
	{
        private ToolStrip  _strip;
        private readonly Dictionary<string,ToolStripButton> _mapId = new Dictionary<string, ToolStripButton>();
//        private ColorScheme _colorScheme;
//        private string _colorSchemeKey = "Sidebar.Background";

        public SidebarButtons()
        {
            SetStyle( ControlStyles.ResizeRedraw, true );
            SetStyle( ControlStyles.Opaque, false );

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _strip = new ToolStrip();
            _strip.AllowItemReorder = true;
            _strip.CanOverflow = true;
            _strip.GripStyle = ToolStripGripStyle.Hidden;
            _strip.AutoSize = true;
            _strip.ImageScalingSize = new Size( 24, 24 );

            Controls.Add( _strip );
        }

        public ToolStripButton GetPaneButton( string paneId )
        {
            ToolStripButton btn = _mapId[ paneId ];
            if( btn == null )
                throw new ArgumentException( "Internal Error -- Invalid value for PaneID [" + paneId + "]" );
            return btn;
        }

        public ToolStripButton AddButton( VerticalSidebar.PaneData paneData, Image image, string text )
        {
            ToolStripButton btn = new ToolStripButton();
            btn.Image = image;
            btn.ToolTipText = text;
            btn.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btn.CheckOnClick = true;
            btn.Tag = paneData;
            _strip.Items.Add( btn );
            _mapId.Add( paneData.PaneId, btn );

            return btn;
        }

        public void SetButtonPressed( string id, bool val )
        {
            ToolStripButton btn = _mapId[ id ];
            if( btn != null )
            {
                btn.Checked = val;
            }
        }

	    public int CheckedButtonsCount
	    {
	        get
	        {
                int count = 0;
	            foreach( ToolStripButton btn in _strip.Items )
	            {
	                if( btn.Checked )
                        count ++;
	            }
                return count;
	        }
	    }

        public void CheckButtonsState()
        {
            bool enable = (CheckedButtonsCount > 1);
            foreach( ToolStripButton btn in _strip.Items )
            {
                if( btn.Checked )
                    btn.Enabled = enable;
            }
        }

	    public ToolStripItemCollection Items
	    {
	        get {  return _strip.Items;  }
	    }
	}
}
