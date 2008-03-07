/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    /// <summary>
    /// A pane with a row of vertical buttons and a stack of panes connected to those buttons.
    /// </summary>
    internal class VerticalSidebar : UserControl, ISidebar, IContextProvider
	{
        private SidebarBackground _buttonPane;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private Container components = null;

        private SidebarSide _side = SidebarSide.Left;
        private Panel _contentPane;
        private int _expandedWidth;
        private int _minPaneHeight = 120;
        private int _updateCount;
        private PaneData _lastActivePaneData;
        private int _buttonHeight;
        private int _collapsedWidth = 30;
        private const int _defaultButtonHeight = 135;
        private ColorScheme _colorScheme;
        private bool _expanded = false;
        private HashSet _populatedPanes = new HashSet();

		private class PaneData
		{
            public string PaneId;
            public VerticalButton Button;
            public SidebarPaneBackground PaneBackground;
            public AbstractViewPane Pane;
            public int PaneHeight;
            public PaneCaption PaneCaption;
            public Splitter PaneSplitter;

		    public PaneData( string paneID, VerticalButton button, SidebarPaneBackground paneBackground,
                AbstractViewPane pane, PaneCaption paneCaption, Splitter paneSplitter, int paneHeight )
		    {
		        PaneId = paneID;
                Button = button;
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

            _buttonHeight = _defaultButtonHeight;

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
            this._buttonPane = new SidebarBackground();
            this._contentPane = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // _buttonPane
            // 
            this._buttonPane.Dock = System.Windows.Forms.DockStyle.Left;
            this._buttonPane.Location = new System.Drawing.Point(0, 0);
            this._buttonPane.Name = "_buttonPane";
            this._buttonPane.Size = new System.Drawing.Size(30, 150);
            this._buttonPane.TabIndex = 0;
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
            this.Controls.Add(this._buttonPane);
            this.Name = "VerticalSidebar";
            this.ResumeLayout(false);

        }
		#endregion

        public event EventHandler PaneAdded;
        public event EventHandler ExpandedChanged;
        
        public event PaintEventHandler PaintSidebarBackground
        {
            add { _buttonPane.PaintSidebarBackground += value; }
            remove { _buttonPane.PaintSidebarBackground -= value; }
        }

        [DefaultValue(SidebarSide.Left)]
        public SidebarSide Side
	    {
	        get { return _side; }
	        set 
            { 
                _side = value;
                _buttonPane.Dock = (_side == SidebarSide.Left ) ? DockStyle.Left : DockStyle.Right;
            }
	    }

        public int ExpandedWidth
        {
            get 
            { 
                if ( Expanded )
                    return Width;

                return _expandedWidth; 
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

        [DefaultValue(30)]
        public int CollapsedWidth
        {
            get { return _collapsedWidth; }
            set
            {
                _collapsedWidth = value;
                if ( !_expanded )
                {
                    _buttonPane.Width = value;
                }
            }
        }

        public bool Expanded
        {
            get { return _expanded; }
        }

        private void SetExpanded( bool value )
        {
            if ( _expanded != value )
            {
                _expanded = value;
                if ( _expanded )
                {
                    _buttonPane.Width = 30;
                }
                else
                {
                    _buttonPane.Width = _collapsedWidth;
                }
                if ( ExpandedChanged != null )
                {
                    ExpandedChanged( this, EventArgs.Empty );
                }
            }
        }

        public int PaneCount
        {
            get { return _buttonPane.Controls.Count; }
        }

	    public ColorScheme ColorScheme
	    {
	        get { return _colorScheme; }
	        set 
            { 
                _colorScheme = value; 
                _buttonPane.ColorScheme = _colorScheme;
                foreach( VerticalButton btn in _buttonPane.Controls )
                {
                    btn.ColorScheme = _colorScheme;
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

        public string BackgroundColorSchemeKey
        {
            get { return _buttonPane.ColorSchemeKey; }
            set { _buttonPane.ColorSchemeKey = value; }
        }

        public int BackgroundFillHeight
        {
            get { return _buttonPane.FillHeight; }
            set { _buttonPane.FillHeight = value; }
        }

        public void RegisterPane( AbstractViewPane pane, string id, string caption, Icon icon )
        {
            int y = 0;
            if ( _buttonPane.Controls.Count > 0 )
            {
                VerticalButton lastBtn = (VerticalButton) _buttonPane.Controls [_buttonPane.Controls.Count-1];
                y = lastBtn.Bottom + 2;
            }

            VerticalButton btn = new VerticalButton();
            btn.Location = new Point( _buttonPane.Width - 28, y );
            btn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btn.Angle = (_side == SidebarSide.Left) ? 90 : 270;
            btn.Text = caption;
            btn.Font = new Font( "Tahoma", 10 );
            btn.Icon = icon;
            btn.BackColor = Color.FromArgb( 0, Color.Black );
            btn.ColorScheme = _colorScheme;
            btn.PressedChanged += new EventHandler( OnVerticalButtonPressed );

            if ( btn.PreferredHeight > _defaultButtonHeight )
            {
                btn.HeightMultiplier = (double) btn.PreferredHeight / (double) _defaultButtonHeight;
            }

            btn.Size = new Size( 26, (int) (_buttonHeight * btn.HeightMultiplier ));

            if ( GetPressedCount() == 0 && Width > _buttonPane.Width )
            {
                if ( _expandedWidth == 0 )
                {
                    _expandedWidth = Width;
                }
                
                Width = _buttonPane.Width;
            }

            _buttonPane.Controls.Add( btn );

            PaneCaption paneCaption = new PaneCaption();
            paneCaption.Text = caption;
            paneCaption.Dock = DockStyle.Top;
            paneCaption.Height = 18;
            paneCaption.Visible = false;
            paneCaption.Active = false;
            paneCaption.CaptionButtons = PaneCaptionButtons.Minimize;
            paneCaption.Click += new EventHandler( OnPaneCaptionClick );
            paneCaption.MinimizeClick += new EventHandler( OnPaneMinimize );
            paneCaption.MaximizeClick += new EventHandler( OnPaneMaximize );
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
            pane.Enter += new EventHandler( OnPaneEnter );
            pane.Leave += new EventHandler( OnPaneLeave );

            Splitter paneSplitter = new Splitter();
            paneSplitter.Dock = DockStyle.Top;
            paneSplitter.Height = 3;
            _contentPane.Controls.Add( paneSplitter );
            _contentPane.Controls.SetChildIndex( paneSplitter, 0 );

            PaneData paneData = new PaneData( id, btn, background, pane, paneCaption, paneSplitter, paneHeight );
            btn.Tag = paneData;
            paneCaption.Tag = paneData;

            IColorSchemeable schemeable = pane as IColorSchemeable;
            if ( schemeable != null )
            {
                schemeable.ColorScheme = _colorScheme;
            }

            AdjustDockAndSplitters( null, false );
            if ( PaneAdded != null )
            {
                PaneAdded( this, EventArgs.Empty );
            }
        }

        public void PopulateViewPanes()
        {
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                if ( btn.Pressed )
                {
                    PaneData paneData = (PaneData) btn.Tag;
                    if ( !_populatedPanes.Contains( paneData.PaneId ) )
                    {
                        _populatedPanes.Add( paneData.PaneId );
                        paneData.Pane.Populate();
                    }
                }
            }
        }

        public void UpdateActiveWorkspace()
        {
            IResource workspace = Core.WorkspaceManager.ActiveWorkspace;
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                PaneData paneData = (PaneData) btn.Tag;
                if ( _populatedPanes.Contains( paneData.PaneId ) )
                {
                    paneData.Pane.SetActiveWorkspace( workspace );
                }
            }
        }

        public bool IsPaneExpanded( string paneId )
        {                                                                                       
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                PaneData paneData = (PaneData) btn.Tag;
                if ( paneData.PaneId == paneId )
                    return paneData.Button.Pressed;
            }
            throw new ArgumentException( "Pane ID " + paneId + " not found" );
        }

        public void SetPaneExpanded( string paneId, bool expanded )
        {
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                PaneData paneData = (PaneData) btn.Tag;
                if ( paneData.PaneId == paneId )
                {
                    SetButtonPressed( paneData.Button, expanded );
                    return;
                }
            }
            throw new ArgumentException( "Pane ID " + paneId + " not found" );
        }

        public void SetPaneCaption( string paneId, string caption )
        {
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                PaneData paneData = (PaneData) btn.Tag;
                if ( paneData.PaneId == paneId )
                {
                    paneData.PaneCaption.Text = caption;
                    paneData.Button.Text = caption;
                    paneData.Button.Invalidate();
                    return;
                }
            }
            throw new ArgumentException( "Pane ID " + paneId + " not found" );
        }

        private int GetPressedCount()
        {
            int pressedCount = 0;
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                if ( btn.Pressed )
                {
                    pressedCount++;
                }
            }
            return pressedCount;
        }

        private void SetButtonPressed( VerticalButton button, bool pressed )
        {
            button.Pressed = pressed;
        }

        public void ActivatePane( string paneID )
        {
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                PaneData paneData = (PaneData) btn.Tag;
                if ( paneData.PaneId == paneID )
                {
                    SetButtonPressed( paneData.Button, true );
                    paneData.Pane.Focus();
                    IResource selResource = paneData.Pane.SelectedResource;
                    if ( selResource != null ) 
                    {
                        // hack (see #5371, #5746)
                        paneData.Pane.SelectResource( selResource, false );  
                    }  
                    return;
                }
            }
            throw new ArgumentException( "Pane ID " + paneID + " not found" );
        }

        public AbstractViewPane GetPane( string paneID )
        {
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                PaneData paneData = (PaneData) btn.Tag;
                if ( paneData.PaneId == paneID )
                    return paneData.Pane;
            }
            return null;
        }

        public bool ContainsPane( string paneId )
        {
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                PaneData paneData = (PaneData) btn.Tag;
                if ( paneData.PaneId == paneId )
                    return true;
            }
            return false;            
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
            foreach( VerticalButton btn in _buttonPane.Controls )
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
                int[] paneHeights = new int [_buttonPane.Controls.Count];
                int activePaneIndex = -1;
                IResource selectedResource = null;
                for( int i=0; i<_buttonPane.Controls.Count; i++ )
                {
                    PaneData paneData = (PaneData) _buttonPane.Controls [i].Tag;
                    if ( !paneData.Button.Pressed )
                    {
                        paneHeights [i] = -1;
                    }
                    else
                    {
                        paneHeights [i] = paneData.PaneBackground.Height;
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
                for( int i=0; i<_buttonPane.Controls.Count; i++ )
                {
                    if ( i >= paneHeights.Length )
                        break;

                    PaneData paneData = (PaneData) _buttonPane.Controls [i].Tag;
                    if ( paneHeights [i] < 0 )
                    {
                        SetButtonPressed( paneData.Button, false );
                    }
                    else
                    {
                        SetButtonPressed( paneData.Button, true );
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

        private void OnVerticalButtonPressed( object sender, EventArgs e )
        {
            VerticalButton btn = (VerticalButton) sender;

            SuspendLayout();
            try
            {
                PaneData paneData = (PaneData) btn.Tag;
                if ( _updateCount == 0 )
                {
                    AdjustDockAndSplitters( paneData, btn.Pressed );
                }

                bool focusedPaneHidden = false;
                
                if ( btn.Pressed )
                {
                    CheckPopulatePane( paneData );
                    paneData.PaneBackground.Height = paneData.PaneHeight;
                }
                else
                {
                    if ( paneData == _lastActivePaneData )
                    {
                        focusedPaneHidden = true;                        
                    }
                    paneData.PaneHeight = paneData.PaneBackground.Height;
                    paneData.PaneBackground.Height = 0;
                }
                
                paneData.PaneBackground.Visible = btn.Pressed;
                paneData.PaneCaption.Visible = btn.Pressed;

                if ( focusedPaneHidden )
                {
                    bool foundActive = false;
                    for( int i=0; i<_buttonPane.Controls.Count; i++ )
                    {
                        PaneData visibleData = DataAt( i );
                        if ( visibleData.Button.Pressed )
                        {
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
                    Width = _collapsedWidth;
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

            if ( btn.Pressed )
            {
                UpdatePaneSizes( _minPaneHeight );
            }

            SetExpanded( GetPressedCount() != 0 );
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

        /**
         * Sets DockStyle.Fill for the last visible pane and DockStyle.Top 
         * for the remaining ones. Also updates splitter visibility.
         */

        private void AdjustDockAndSplitters( PaneData toggledPane, bool toggledPaneState )
        {
            bool lastPane = true;
            int visiblePanes = 0;
            for( int i=_buttonPane.Controls.Count-1; i >= 0; i-- )
            {
                PaneData paneData = DataAt( i );
                // don't check Visible because it will return false if the sidebar 
                // as a whole is hidden
                bool visible = paneData.Button.Pressed;
                if ( paneData == toggledPane )
                    visible = toggledPaneState;

                if ( visible )
                {
                    visiblePanes++;
                    if ( lastPane )
                    {
                        paneData.PaneBackground.Dock = DockStyle.Fill;
                        paneData.PaneSplitter.Visible = false;
                        lastPane = false;
                    }
                    else
                    {
                        paneData.PaneBackground.Dock = DockStyle.Top;
                        paneData.PaneSplitter.Visible = true;
                    }
                }
                else
                {
                    paneData.PaneSplitter.Visible = false;
                }

                int paneIndex = _buttonPane.Controls.Count-1 - i;
                _contentPane.Controls.SetChildIndex( paneData.PaneSplitter, paneIndex * 3 );
                _contentPane.Controls.SetChildIndex( paneData.PaneBackground, paneIndex * 3 + 1 );
                _contentPane.Controls.SetChildIndex( paneData.PaneCaption, paneIndex * 3 + 2 );
            }

            if ( visiblePanes > 0 )
            {
                for( int i=0; i<_buttonPane.Controls.Count; i++ )
                {
                    PaneData paneData = DataAt( i );
                    bool visible = paneData.Button.Pressed;
                    if ( paneData == toggledPane )
                        visible = toggledPaneState;

                    if ( visible )
                    {
                        if ( visiblePanes == 1 )
                        {
                            paneData.PaneCaption.CaptionButtons = PaneCaptionButtons.Minimize;
                        }
                        else
                        {
                            paneData.PaneCaption.CaptionButtons = PaneCaptionButtons.Minimize | PaneCaptionButtons.Maximize;
                        }
                    }
                }
            }
        }

        /**
         * Updates the heights of panes so that all visible panes get their share of height.
         */

        private void UpdatePaneSizes( int minHeight )
        {
            int totalNeedHeight = 0;
            for( int i=0; i<_buttonPane.Controls.Count; i++ )
            {
                PaneData paneData = DataAt( i );
                if ( !paneData.Button.Pressed )
                {
                    continue;
                }

                int needHeight = minHeight - paneData.PaneBackground.Height;
                if ( needHeight > 0 )
                {
                    totalNeedHeight += needHeight;
                }
            }

            if ( totalNeedHeight > 0 )
            {
                int totalAvailHeight = 0;
                for( int i=0; i<_buttonPane.Controls.Count; i++ )
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

        /**
         * When the size of the pane changes, ensures that all visible child panes get their 
         * share of height.
         */
        
        protected override void OnLayout( LayoutEventArgs levent )
        {
            base.OnLayout( levent );
            if ( levent.AffectedControl != null && levent.AffectedProperty != null )
            {
                if ( levent.AffectedProperty.ToString() == "Bounds" )
                {
                    Form frm = FindForm();
                    if ( frm != null && frm.WindowState != FormWindowState.Minimized )
                    {
                        UpdatePaneSizes( 40 );
                        
                        if ( _buttonPane.Controls.Count > 0 )
                        {
                            UpdateButtonHeight();
                        }
                    }
                }
            }
        }

	    private void UpdateButtonHeight()
	    {
	        int newButtonHeight = (int) ( ( (double) ClientSize.Height / GetTotalMultiplier() ) - 2);
	        if ( newButtonHeight > _defaultButtonHeight )
	        {
	            newButtonHeight = _defaultButtonHeight;
	        }
    
	        if ( newButtonHeight != _buttonHeight )
	        {
                _buttonHeight = newButtonHeight;
                int y = 0;
                for( int i=0; i<_buttonPane.Controls.Count; i++ )
                {
                    PaneData data = DataAt( i );
                    data.Button.Location = new Point( 2, y );
                    data.Button.Size = new Size( 26, (int) ( _buttonHeight * data.Button.HeightMultiplier ) );
                    y += data.Button.Height + 2;
                }
	        }
	    }

        private double GetTotalMultiplier()
        {
            double result = 0.0;
            for( int i=0; i<_buttonPane.Controls.Count; i++ )
            {
                result += DataAt( i ).Button.HeightMultiplier;
            }
            return result;
        }

	    private PaneData DataForPane( Control pane )
        {
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                PaneData data = (PaneData) btn.Tag;
                if ( data.Pane == pane )
                    return data;
            }
            throw new Exception( "Could not find PaneData for pane " + pane );
        }

        private PaneData DataAt( int index )
        {
        	return (PaneData) _buttonPane.Controls [index].Tag;
        }

        private void OnPaneEnter( object sender, EventArgs e )
        {
            SetActivePane( sender as Control );
            _lastActivePaneData.PaneCaption.Active = true;
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

        private void OnPaneLeave( object sender, EventArgs e )
        {
            PaneData paneData = DataForPane( sender as Control );
            paneData.PaneCaption.Active = false;
        }

        private void OnPaneCaptionClick( object sender, EventArgs e )
        {
            PaneCaption caption = (PaneCaption) sender;
            PaneData paneData = (PaneData) caption.Tag;
            
            if ( !paneData.Pane.ContainsFocus )
            {
                paneData.Pane.Focus();
            
                // HACK: Remove when all panes are converted to JetListView
                if ( !(paneData.Pane is ResourceTreePaneBase ))
                {
                    IResource node = paneData.Pane.SelectedResource;
                    if ( node != null )
                    {
                        paneData.Pane.SelectResource( node, false );
                    }
                }
            }
        }

        private void OnPaneMinimize( object sender, EventArgs e )
        {
            PaneCaption caption = (PaneCaption) sender;
            PaneData paneData = (PaneData) caption.Tag;
            SetButtonPressed( paneData.Button, false );
        }

        private void OnPaneMaximize( object sender, EventArgs e )
        {
            PaneCaption caption = (PaneCaption) sender;
            PaneData paneData = (PaneData) caption.Tag;
            BeginUpdate();
            foreach( VerticalButton btn in _buttonPane.Controls )
            {
                if ( btn.Tag != paneData )
                {
                    SetButtonPressed( btn, false );
                }
            }
            EndUpdate();
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

    public enum SidebarSide { Left, Right };

    internal class SidebarState
    {
        private int[] _paneHeights;
        private int _activePaneIndex;
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
            ISettingStore ini = ICore.Instance.SettingStore;
            int count = ini.ReadInt( section, "PaneCount", 0 );
            if ( count == 0 )
                return null;

            int[] paneHeights = new int [count];
            for( int i=0; i<count; i++ )
            {
                paneHeights [i] = ini.ReadInt( section, "Pane" + i + "Height", 0 );
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
            ISettingStore ini = ICore.Instance.SettingStore;
            ini.WriteInt( section, "PaneCount", _paneHeights.Length );
            for( int i=0; i<_paneHeights.Length; i++ )
            {
                ini.WriteInt( section, "Pane" + i + "Height", _paneHeights [i] );
            }
            ini.WriteInt( section, "ActivePaneIndex", _activePaneIndex );
            ini.WriteInt( section, "SelectedResource", (_selectedResource == null) ? -1 : _selectedResource.Id );
        }
    }
}
