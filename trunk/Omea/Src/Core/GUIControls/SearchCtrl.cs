/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using JetBrains.Interop.WinApi;
using JetBrains.Omea.Base;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.Omea.GUIControls.CustomViews;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.Components.ImageListButton;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Summary description for SearchCtrl.
	/// </summary>
	public class SearchCtrl : UserControl, ICommandBar
	{
		#region Attributes

        private const string _ChooserTooltip = "Select where to search";
        private const string _NotReadyErrorMessage = "Text index is not built or loaded yet";
        private const string _NotReadyErrorCaption = "Search query failed";

		/// <summary>Minimum allowed width of the combobox.</summary>
		protected static readonly int c_nMinComboWidth = 100;

		/// <summary>Gap between the controls.</summary>
		protected static readonly int c_nGap = 5;

		private readonly List<String> _storedQueries = new List<String>();

		private IContainer components;
		private JetLinkLabel _labelTitle;

        private SelfDrawnPanelWithBorder _panelChoosingButtons;
        private ImageListButton _btnProvider;
		private ImageListButton _btnProviderChooser;

        private ACMarginableComboBox  _searchQueryCombo;
		private ImageListButton     _btnSearch;
		private JetLinkLabel        _labelAdvanced;
		private ToolTip             _tipReason;

		private ColorScheme         _colorScheme;
		private static AdvancedSearchForm AdvSForm;
		private static string CurrentSearchText = string.Empty;

        private ISearchProvider _currentProvider;
        private ContextMenu     _menuProviders;
        private MenuItem        _currentProviderItem;
        private ArrayList       _searchProviders;

		/// <summary>The command bar site.</summary>
		protected ICommandBarSite _site = null;

		/// <summary>The grip.</summary>
		protected Grip _grip = null;

		/// <summary>Scaling factor for the component.</summary>
		protected SizeF _sizeScale = new SizeF( 1, 1 );

		#endregion Attributes

		#region Construction
		public SearchCtrl()
		{
			InitializeComponent();

			SetStyle( ControlStyles.AllPaintingInWmPaint | ControlStyles.CacheText |
				      ControlStyles.ContainerControl | ControlStyles.ResizeRedraw |
                      ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.Opaque, true );

			// Load the size (change the control width only)
			int nNewWidth = Core.SettingStore.ReadInt( "SearchBar", "CustomWidth", OptimalSize.Width );
			nNewWidth = nNewWidth >= MinSize.Width ? nNewWidth : MinSize.Width;
			Size = new Size( nNewWidth, Size.Height );
		}

        private void  InitializeSearchProviders()
        {
            _currentProvider = Core.TextIndexManager.CurrentSearchProvider;
            _menuProviders = new ContextMenu();
            _searchProviders = new ArrayList();

            string[] groups = Core.TextIndexManager.GetSearchProviderGroups();
            foreach( string group in groups )
            {
                EventHandler eh = onProviderSelected;
                MeasureItemEventHandler mieh = ProviderMenuItem.mi_MeasureItem;
                DrawItemEventHandler dieh = ProviderMenuItem.mi_DrawItem;

                ISearchProvider[] providers = Core.TextIndexManager.GetSearchProvidersInGroup( group );

                //-------------------------------------------------------------
                //  Check this until we manage to delete empty groups when
                //  providers are delete on the fly.
                //-------------------------------------------------------------
                if( providers.Length > 0 )
                {
                    //---------------------------------------------------------
                    //  Add named group only in the case of valid name. Empty name
                    //  is most common used for miscellaneous minor search
                    //  providers just for the possibility not to construct name
                    //  "Others" or smth like this.
                    //---------------------------------------------------------
                    if( group.Length > 0 )
                    {
                        ProvidersHeaderMenuItem pmi = new ProvidersHeaderMenuItem( group );
                        pmi.OwnerDraw = true;
                        pmi.Enabled = false;
                        pmi.MeasureItem += ProvidersHeaderMenuItem.mi_MeasureItem;
                        pmi.DrawItem += ProvidersHeaderMenuItem.mi_DrawItem;
                        _menuProviders.MenuItems.Add( pmi );
                    }

                    foreach( ISearchProvider provider in providers )
                    {
                        MenuItem mi = new ProviderMenuItem( provider, eh );
                        mi.RadioCheck = true;
                        mi.OwnerDraw = true;
                        mi.MeasureItem += mieh;
                        mi.DrawItem += dieh;

                        _menuProviders.MenuItems.Add( mi );
                        _searchProviders.Add( provider );

                        if( _currentProvider == provider )
                        {
			                _btnProvider.AddIcon( provider.Icon, ImageListButton.ButtonState.Normal );
                            _currentProviderItem = mi;
                            mi.Checked = true;
                        }
                    }
                }

                //  Different groups are delimited by the dash line.
                if( group != groups[ groups.Length - 1 ] )
                    _menuProviders.MenuItems.Add( new MenuItem( "-" ) );
            }

            ShowProviderNameTip();
        }

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		#endregion

		[DefaultValue( null )]
		public ColorScheme ColorScheme
		{
			get {  return _colorScheme;  }
			set {  _colorScheme = value; }
		}

		#region Visual Init
		private void InitializeComponent()
		{
			components = new Container();
			_labelAdvanced = new JetLinkLabel();
			_labelTitle = new JetLinkLabel();
            _btnProvider = new ImageListButton();
            _searchQueryCombo = new ACMarginableComboBox();
			_btnSearch = new ImageListButton();
            _btnProviderChooser = new ImageListButton ();
            _panelChoosingButtons = new SelfDrawnPanelWithBorder();
			_tipReason = new ToolTip( components );
			BackColor = SystemColors.Control;
			SuspendLayout();
            // 
            // _btnProviderChooser
            // 
            _btnProviderChooser.Location = new Point(16, 1);
			_btnProviderChooser.Name = "_btnProviderChooser";
			_btnProviderChooser.PressedImageIndex = -1;
			_btnProviderChooser.Size = new Size( 16, 16 );
			_btnProviderChooser.Click += _btnProviderChooser_Click;
            _btnProviderChooser.BackColor = Color.Transparent;
            _btnProviderChooser.AddIcon(Utils.GetResourceIconFromAssembly("GUIControls", "GUIControls.Icons.chooser.ico"), ImageListButton.ButtonState.Normal);
            //
            // _panelChoosingButtons
            //
            _panelChoosingButtons.Location = new Point(90, 8);
            _panelChoosingButtons.Size = new Size(15, 17);
            _panelChoosingButtons.BackColor = Color.Transparent;
            _panelChoosingButtons.ForeColor = Color.Transparent;
//            _panelChoosingButtons.Controls.Add(_btnProviderChooser);
            _panelChoosingButtons.Paint += _panelChoosingButtons_Paint;
            _panelChoosingButtons.EnabledChanged += _panelChoosingButtons_EnabledChanged;
            // 
			// _labelTitle
			// 
			_labelTitle.Font = new Font( "Tahoma", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 204 );
			_labelTitle.Location = new Point( 8, 3 );
			_labelTitle.Name = "_labelTitle";
			_labelTitle.Size = new Size( 44, 16 );
			_labelTitle.TabStop = false;
			_labelTitle.Text = "Search";
			_labelTitle.BackColor = BackColor;
			_labelTitle.ForeColor = SystemColors.ControlText;
			_labelTitle.ClickableLink = false;
            // 
            // _btnProvider
            // 
            _btnProvider.Location = new Point(0, 0);
            _btnProvider.Name = "_btnProvider";
            _btnProvider.PressedImageIndex = -1;
            _btnProvider.Size = new Size(16, 16);
		    _btnProvider.Padding = new Padding( 0 );
            _btnProvider.AutoSize = false;
		    _btnProvider.BackColor = BackColor;
            _btnProvider.Click += _btnProviderChooser_Click;
            // 
			// _searchQueryCombo
			// 
			_searchQueryCombo.DropDownWidth = 200;
			_searchQueryCombo.Location = new Point( 62, 0 );
			_searchQueryCombo.Name = "_searchQueryCombo";
			_searchQueryCombo.Size = new Size( 170, 24 );
			_searchQueryCombo.TabIndex = 2;
			_searchQueryCombo.KeyDown += SearchQuery_KeyDown;
			_searchQueryCombo.KeyPress += _searchQueryCombo_KeyPress;
			_searchQueryCombo.SelectedIndexChanged += _searchQueryCombo_SelectedIndexChanged;
			_searchQueryCombo.Leave += _searchQueryCombo_Leave;
			_searchQueryCombo.Enter += _searchQueryCombo_Enter;
			_searchQueryCombo.GotFocus += _searchQueryCombo_GotFocus;
			_searchQueryCombo.LostFocus += _searchQueryCombo_GotFocus;
            // 
			// _btnSearch
			// 
			_btnSearch.Location = new Point( 248, 0 );
			_btnSearch.Name = "_btnSearch";
			_btnSearch.PressedImageIndex = -1;
			_btnSearch.Size = new Size( 37, 22 );
			_btnSearch.TabIndex = 3;
			_btnSearch.Click += OnClick;
			_btnSearch.BackColor = BackColor;
			_btnSearch.AddIcon( Utils.GetResourceIconFromAssembly( "GUIControls", "GUIControls.Icons.SearchControl.Go.Normal.ico" ), ImageListButton.ButtonState.Normal);
			_btnSearch.AddIcon( Utils.GetResourceIconFromAssembly( "GUIControls", "GUIControls.Icons.SearchControl.Go.Hot.ico" ), ImageListButton.ButtonState.Hot);
			_btnSearch.AddIcon( Utils.GetResourceIconFromAssembly( "GUIControls", "GUIControls.Icons.SearchControl.Go.Disabled.ico" ), ImageListButton.ButtonState.Disabled);
			// 
			// _lblAdvanced
			// 
			_labelAdvanced.Cursor = Cursors.Hand;
			_labelAdvanced.ForeColor = Color.FromArgb( 70, 70, 211 );
			_labelAdvanced.BackColor = BackColor;
			_labelAdvanced.Name = "_labelAdvanced";
			_labelAdvanced.TabIndex = 4;
			_labelAdvanced.Text = "Advanced...";
			_labelAdvanced.Click += AdvancedSearchLinkClicked;
            //
            // Grip
            //
			_grip = new Grip( this );
			// 
			// _tipReason
			// 
			_tipReason.ShowAlways = true;
			// 
			// SearchCtrl
			// 
			Controls.Add( _labelAdvanced );
			Controls.Add( _labelTitle );
//            Controls.Add( _panelChoosingButtons );
			Controls.Add( _searchQueryCombo );
			Controls.Add( _btnSearch );
		    Controls.Add( _btnProvider );
  			Font = new Font( "Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 204 );
			Name = "SearchCtrl";
			Size = OptimalSize;
			ResumeLayout( false );

            _searchQueryCombo.Attach(_btnProvider);
        }
		#endregion

        #region OnLeave/OnEnter
		protected override void OnEnter( EventArgs e )
		{
			base.OnEnter( e );

			// Set the highlighted background color
			BackColor = SystemColors.ControlLight;
			_labelTitle.BackColor = BackColor;
			_labelAdvanced.BackColor = BackColor;
			_btnSearch.BackColor = BackColor;
            HideProviderNameTip();

			Invalidate( true );
		}

		protected override void OnLeave( EventArgs e )
		{
			base.OnLeave( e );

			// Set the non-highlighted background color
			BackColor = SystemColors.Control;
			_labelTitle.BackColor = BackColor;
			_labelAdvanced.BackColor = BackColor;
			_btnSearch.BackColor = BackColor;
            ShowProviderNameTip();

			Invalidate( true );
		}
        #endregion OnLeave/OnEnter

        #region OnGotFocus
        private void _searchQueryCombo_GotFocus(object sender, EventArgs e)
        {
            if( _searchQueryCombo.Focused )
                HideProviderNameTip();
            else
                ShowProviderNameTip();
        }
        #endregion OnGotFocus

        #region OnPaint
		protected override void OnPaint( PaintEventArgs e )
		{ // no base class call — no excessive flicker, all is hanlded here

			// Main Background
			e.Graphics.FillRectangle( new SolidBrush( BackColor ), RectInsideBorder );

			// Border
			/*
			Borders drawing sequence:
			
			# dark-dark
			@ dark
			L lightlight
			
			@ @@@@@@@@@@@@@@-1-@@@@@@@@@@@@@@@@@L
			@                                   L
			@  L LLLLLLLLLLL-5-LLLLLLLLLLLLLL#  L
			@  L                             #  L
			|  |                             |  |
			2  6                             8  4
			|  |                             |  |
			@  L                                L
			@  #############-7-###############  L
			@                                    
			LLLLLLLLLLLLLLLL-3-LLLLLLLLLLLLLLLLLL
			
			*/
			Rectangle rc = ClientRectangle;

			// Outer
			e.Graphics.DrawLine( SystemPens.ControlDark, rc.Left + 1, rc.Top + 0, rc.Right - 2, rc.Top + 0 ); // 1
			e.Graphics.DrawLine( SystemPens.ControlDark, rc.Left + 0, rc.Top + 0, rc.Left + 0, rc.Bottom - 2 ); // 2
			e.Graphics.DrawLine( SystemPens.ControlLightLight, rc.Left + 0, rc.Bottom - 1, rc.Right - 1, rc.Bottom - 1 ); // 3
			e.Graphics.DrawLine( SystemPens.ControlLightLight, rc.Right - 1, rc.Top + 0, rc.Right - 1, rc.Bottom - 2 ); // 4

			// Inner
			e.Graphics.DrawLine( SystemPens.ControlLightLight, rc.Left + 2, rc.Top + 1, rc.Right - 3, rc.Top + 1 ); // 5
			e.Graphics.DrawLine( SystemPens.ControlLightLight, rc.Left + 1, rc.Top + 1, rc.Left + 1, rc.Bottom - 3 ); // 6
			e.Graphics.DrawLine( SystemPens.ControlDarkDark, rc.Left + 1, rc.Bottom - 2, rc.Right - 2, rc.Bottom - 2 ); // 7
			e.Graphics.DrawLine( SystemPens.ControlDarkDark, rc.Right - 2, rc.Top + 1, rc.Right - 2, rc.Bottom - 3 ); // 8

			// Grip
			_grip.OnPaint( e.Graphics );
		}

        private void _panelChoosingButtons_Paint(object sender, PaintEventArgs e)
        {
			Rectangle rc = _panelChoosingButtons.ClientRectangle;

            Brush br = new SolidBrush( Color.Transparent );
            rc.X += 1;
            rc.Y += 1;
            rc.Height -= 2;
            e.Graphics.FillRectangle( br, rc );
        }

        #endregion OnPaint

        #region OnLayout
		protected override void OnLayout( LayoutEventArgs levent )
		{ // no base class call — no excessive layouting, all is handled here

			using( new LayoutSuspender( _labelAdvanced ) )
			using( new LayoutSuspender( _labelTitle ) )
//			using( new LayoutSuspender( _panelChoosingButtons ) )
			using( new LayoutSuspender( _searchQueryCombo ) )
			using( new LayoutSuspender( _btnSearch ) )
			{
				// Take the grip into account
				Rectangle rectInsideGrip = _grip.OnLayout( RectInsideBorder );

				int nLeftMargin = rectInsideGrip.Left + 3; // Actually, that's all we have to do about the grip
				int nRightMargin = 2;
				int nAvailWidth = Width;
				nAvailWidth -= nLeftMargin + nRightMargin; // Margins

				// If we have too small width, start dropping the unnecessary controls
				// Order: title, advanced, go, combo
				bool bTakeTitle = true;
				bool bTakeCombo = true;
				bool bTakeGo = true;
				bool bTakeAdvanced = true;

				int nComboWidth = nAvailWidth - _labelTitle.Width - /*_btnSearch.Width - */_labelAdvanced.Width + c_nGap * 3;

				// Drop title?
				if( nComboWidth < c_nMinComboWidth )
				{
					bTakeTitle = false;
					nComboWidth += _labelTitle.Width + c_nGap;
				}

				// Drop Advanced?
				if( nComboWidth < c_nMinComboWidth )
				{
					bTakeAdvanced = false;
					nComboWidth += _labelAdvanced.Width + c_nGap;
				}

				// Drop Go?
				if( nComboWidth < c_nMinComboWidth )
				{
					bTakeGo = false;
					nComboWidth += _btnSearch.Width + c_nGap;
				}

				// Drop combo?
				if( nComboWidth < c_nMinComboWidth )
					bTakeCombo = false;

				// Now, position the controls
				int nLeft = nLeftMargin; // Current occupied space on the left
				int nRight = nRightMargin; // Current occupied space on the right

				if( bTakeTitle )
				{
					_labelTitle.Visible = true;
					_labelTitle.Location = new Point( nLeft, (Height - _labelTitle.Height) / 2 );
					nLeft += _labelTitle.Width + c_nGap;
				}
				else
					_labelTitle.Visible = false;

				if( bTakeAdvanced )
				{
					_labelAdvanced.Visible = true;
					_labelAdvanced.Location = new Point( Width - nRight - _labelAdvanced.Width, (Height - _labelAdvanced.Height) / 2 );
					nRight += _labelAdvanced.Width + c_nGap;
				}
				else
					_labelAdvanced.Visible = false;

				if( bTakeGo )
				{
					_btnSearch.Visible = true;
					_btnSearch.Location = new Point( Width - nRight - _btnSearch.Width, (Height - _btnSearch.Height) / 2 );
					nRight += _btnSearch.Width + c_nGap;
				}
				else
					_btnSearch.Visible = false;

				if( bTakeCombo )
				{
					_searchQueryCombo.Visible = true;
					_searchQueryCombo.Location = new Point( nLeft, (Height - _searchQueryCombo.Height) / 2 );
					_searchQueryCombo.Width = Width - nRight - nLeft;
				}
				else
					_searchQueryCombo.Visible = false;
			}

			// Apply the visual changes
			Invalidate( false );
		}

		/// <summary>
		/// Scales the control.
		/// </summary>
		protected override void ScaleCore( float dx, float dy )
		{
			_sizeScale = new SizeF( dx, dy );
			base.ScaleCore( dx, dy );
		}

		/// <summary>
		/// Layout the controls.
		/// </summary>
		/// <remarks>
		/// <code>
		///  —  (grip)  —  [#TITLE#] [#COMBO#] [#GO#] [#ADVANCED#]  —
		/// 3px  3px   3px   auto    max-avail auto?      auto     2px
		/// </code>
		/// Total width is adjusted to occupy all the available control width.
		///		All the control widths are constant but for the COMBO, which widens as needed.
		///	Height is constant, controls are v-centered in the available space.
		/// </remarks>
		protected Rectangle RectInsideBorder
		{
			get
			{
				Rectangle rc = ClientRectangle;
				rc.Inflate( -2, -2 );
				return rc;
			}
		}
        #endregion OnLayout

		#region Static Operations

		//  Static method is called exclusively from action "Refine this search"
		//  because in that source point (GUI Controls) we have no access to the
		//  Search control of the MainWindow.
		public static void ShowAdvancedSearchDialog( IResource refinedView )
		{
			if( AdvSForm != null && !AdvSForm.IsDisposed )
			{
				AdvSForm.Close();
			}
			AdvSForm = new AdvancedSearchForm( refinedView );
			AdvSForm.Show();
		}
		#endregion

        #region Query Processing

        #region Load/Save Queries in Ini
		private void LoadSavedQueries()
		{
			int QueriesNumber = Core.SettingStore.ReadInt( "Search", "QueriesNumber", 0 );
			if( QueriesNumber > 0 )
			{
				_storedQueries.Clear();
				QueriesNumber = Math.Min( QueriesNumber, 10 );
				_searchQueryCombo.BeginUpdate();
				for( int i = 0; i < QueriesNumber; i++ )
				{
					string Query = Core.SettingStore.ReadString( "Search", "Query" + i );
					if( Query.Length > 0 )
					{
						_searchQueryCombo.Items.Add( Query );
						_storedQueries.Add( Query );
					}
				}
				_searchQueryCombo.EndUpdate();
			}
		}

		private void UpdateStoredQueriesList( string searchText )
		{
			int Index = _storedQueries.IndexOf( searchText );
			if( Index != -1 )
				_storedQueries.RemoveAt( Index );
			else
			{
				_searchQueryCombo.BeginUpdate();
				_searchQueryCombo.Items.Insert( 0, searchText );
				_searchQueryCombo.EndUpdate();
			}
			_storedQueries.Insert( 0, searchText );

			int QueriesNumber = Math.Min( _storedQueries.Count, 10 );
			Core.SettingStore.WriteInt( "Search", "QueriesNumber", QueriesNumber );
			for( int i = 0; i < QueriesNumber; i++ )
				Core.SettingStore.WriteString( "Search", "Query" + i, _storedQueries[ i ] );
		}
        #endregion Load/Save Queries in Ini

		private void OnIndexConstructionComplete( object sender, EventArgs e )
		{
			Core.TextIndexManager.IndexLoaded -= OnIndexConstructionComplete;

			//  forbid calling the processing during shutdown
			if( Core.State == CoreState.Running )
            {
			    Core.UIManager.QueueUIJob( new MethodInvoker( EnableCtrl ) );
            }
		}

		private void AdvancedSearchLinkClicked( object sender, EventArgs e )
		{
			//  forbid calling the processing during initialization
			if( Core.State == CoreState.Running )
				ShowAdvancedSearchDialog();
		}

		private void OnClick( object sender, EventArgs e )
		{
			if( Core.TextIndexManager == null || !Core.TextIndexManager.IsIndexPresent() )
			{
				MessageBox.Show( Core.MainWindow, _NotReadyErrorMessage, _NotReadyErrorCaption,
				                 MessageBoxButtons.OK, MessageBoxIcon.Warning );
			}
			else
			//  forbid calling the processing during initialization
			if( Core.State == CoreState.Running )
			{
				string searchText = _searchQueryCombo.Text;

			    //  Check for mysterious case when the text value of standard control's
			    //  property is null.
			    if( searchText == null )
				    searchText = string.Empty;
			    searchText = searchText.Trim();

				Trace.WriteLine( "Performing full-scale search using query: [" + searchText + "]" );
				PerformSearchAction( searchText );
				Trace.WriteLine( "Full-scale search done" );
				Core.ResourceBrowser.FocusResourceList();
			}
		}

		private void PerformSearchAction( string searchText )
		{
			if( searchText.Length > 0 )
			{
				UpdateStoredQueriesList( searchText );
				Cursor.Current = Cursors.WaitCursor;
                EnableControls( false );
                SetTooltip( "Query is processed now" );

                _currentProvider.ProcessQuery( searchText );

                EnableControls( true );

                SetTooltip();
				Cursor.Current = Cursors.Default;
			}
		}

		public static void CreateSearchView( string query, bool showContexts )
		{
			//-----------------------------------------------------------------
            //  Avoid perform any UI (and other, generally) work if we are
            //  shutting down - some components (like DefaultViewPane) may
            //  already be disposed.
			//-----------------------------------------------------------------
            if( Core.State != CoreState.Running )
                return;

			//-----------------------------------------------------------------
            //  Extract Search Extension subphrases, extract them out of the
            //  query and convert them into the list of conditions.
			//-----------------------------------------------------------------
            int       anchorPos;
		    string[]  resTypes = null;
            bool      parseSuccessful = false;
            ArrayList conditions = new ArrayList();

            do
            {
                string  anchor;
                FindSearchExtension( query, out anchor, out anchorPos );
                if( anchorPos != -1 )
                    parseSuccessful = ParseSearchExtension( anchor, anchorPos, ref query, out resTypes, conditions );
            }
            while(( anchorPos != -1 ) && parseSuccessful );

			//-----------------------------------------------------------------
			//  Create condition from the query
			//-----------------------------------------------------------------
			IFilterManager fMgr = Core.FilterManager;
			IResource queryCondition = ((FilterManager) fMgr).CreateStandardConditionAux( null, query, ConditionOp.QueryMatch );
			FilterManager.ReferCondition2Template( queryCondition, fMgr.Std.BodyMatchesSearchQueryXName );

            conditions.Add( queryCondition );

			//-----------------------------------------------------------------
            bool showDelItems = Core.SettingStore.ReadBool( "Search", "ShowDeletedItems", true );
            IResource[]  condsList = (IResource[]) conditions.ToArray( typeof(IResource) );
			IResource view = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ViewResName, "DeepName", Core.FilterManager.ViewNameForSearchResults );
			if( view != null )
                fMgr.ReregisterView( view, fMgr.ViewNameForSearchResults, resTypes, condsList, null );
			else
                view = fMgr.RegisterView( fMgr.ViewNameForSearchResults, resTypes, condsList, null );
			Core.FilterManager.SetVisibleInAllTabs( view );

			//-----------------------------------------------------------------
			//  Set additional properties characteristic only for "Search Results"
            //  view.
			//-----------------------------------------------------------------
			ResourceProxy proxy = new ResourceProxy( view );
			proxy.BeginUpdate();
			proxy.SetProp( Core.Props.Name, AdvancedSearchForm.SearchViewPrefix + query );
			proxy.SetProp( "_DisplayName", AdvancedSearchForm.SearchViewPrefix + query );
            proxy.SetProp( Core.Props.ShowDeletedItems, showDelItems );
			proxy.SetProp( "ShowContexts", showContexts );
			proxy.SetProp( "ForceExec", true );
            if( Core.SettingStore.ReadBool( "Search", "AutoSwitchToResults", true ) )
                proxy.SetProp( "RunToTabIfSingleTyped", true );
            else
                proxy.DeleteProp( "RunToTabIfSingleTyped" );
			proxy.EndUpdate();

			//-----------------------------------------------------------------
			//  Add new view to the panel
            //  Some steps to specify the correct user-ordering for the new view.
			//-----------------------------------------------------------------
			Core.ResourceTreeManager.LinkToResourceRoot( view, int.MinValue );

            new UserResourceOrder( Core.ResourceTreeManager.ResourceTreeRoot ).Insert( 0, new int[] {view.Id}, false, null );

			//-----------------------------------------------------------------
            //  If we still in the Running mode we can do some UI work...
			//-----------------------------------------------------------------
            if( Core.State == CoreState.Running )
            {
    			Core.UIManager.BeginUpdateSidebar();
	    		Core.LeftSidebar.ActivateViewPane( StandardViewPanes.ViewsCategories );
		    	Core.UIManager.EndUpdateSidebar();
			    Core.LeftSidebar.DefaultViewPane.SelectResource( view );
            }
		}

        #region SearchExtensions parsing
        private static void FindSearchExtension( string query, out string prep, out int prepPosition )
        {
            prepPosition = -1;
            prep = null;

            string[] preps = Core.SearchQueryExtensions.GetAllAnchors();
            for( int i = 0; i < preps.Length; i++ )
            {
                int pos = query.LastIndexOf( " " + preps[ i ] + " " );
                if( pos > prepPosition )
                {
                    prep = preps[ i ];
                    prepPosition = pos;
                }
            }
        }

        private static bool ParseSearchExtension( string prep, int prepPosition, ref string query,
                                                  out string[] resTypes, IList conditions )
        {
            string    savedQuery = query;
            ArrayList types = new ArrayList();
            IResource condition = null;

            string tokens = query.Substring( prepPosition + prep.Length + 2 );
            query = query.Substring( 0, prepPosition );
            resTypes = null;

            string[] anchors = tokens.Split( ' ' );
            for( int i = 0; i < anchors.Length; i++ )
            {
                string resType = Core.SearchQueryExtensions.GetResourceTypeRestriction( prep, anchors[ i ] );
                condition = Core.SearchQueryExtensions.GetSingleTokenRestriction( prep, anchors[ i ] );
                if( resType != null )
                {
                    types.Add( resType );
                }
                else
                if( condition != null )
                {
                    conditions.Add( condition );
                }
                else
                {
                    condition = Core.SearchQueryExtensions.GetMatchingFreestyleRestriction( prep, tokens );
                    if( condition != null )
                        conditions.Add( condition );
                    else
                    {
                        //  Restore the query to the original text in the case when
                        //  no alternative matched.
                        query = savedQuery;
                    }
                    break;
                }
            }

            if( types.Count > 0 )
                resTypes = (string[]) types.ToArray( typeof( string ));

            return (resTypes != null) || (condition != null);
        }
        #endregion SearchExtensions parsing
        #endregion Query Processing

        #region SearchQuery Combobox Events
		private void SearchQuery_KeyDown( object sender, KeyEventArgs e )
		{
            #region Preconditions
			if( Core.ActionManager == null )
				throw new ApplicationException( "SearchControl -- Action Manager is null" );
			if( _searchQueryCombo == null )
				throw new ApplicationException( "SearchControl -- ComboBoxControl is null" );
            #endregion Preconditions

            //  Repetitious pressing of Ctrl-E enumerates all providers in turn.
            if( e.KeyData == ( Keys.Control | Keys.E ))
            {
                int current = _searchProviders.IndexOf( _currentProvider );
                if( current != -1 )
                {
                    current = (current != _searchProviders.Count - 1) ? (current + 1) : 0;
                    ChangeProviderTo( current );
                }
            }
            else
            if( e.KeyData == ( Keys.Control | Keys.Shift | Keys.E ))
            {
                int current = _searchProviders.IndexOf( _currentProvider );
                if( current != -1 )
                {
                    current = (current > 0) ? (current - 1) : (_searchProviders.Count - 1);
                    ChangeProviderTo( current );
                }
            }
            else
            if( !JetTextBox.IsEditorKey( e.KeyData ) && Core.ActionManager.ExecuteKeyboardAction( null, e.KeyData ) )
			{
				e.Handled = true;
			}
			else if( e.KeyCode == Keys.Enter )
			{
				// async processing ensures we don't get a beep (#5442)
				Core.UIManager.QueueUIJob( new EventHandler( OnClick ), new object[] {this, EventArgs.Empty} );
				e.Handled = true;
			}
			else if( e.KeyData == ( Keys.Control | Keys.A ))
			{
			    _searchQueryCombo.SelectAll();
			}

            CurrentSearchText = _searchQueryCombo.Text;

			//  Check for mysterious case when the text value of standard control's
			//  property is null.
			if( CurrentSearchText == null )
				CurrentSearchText = string.Empty;
		}

        private void  ChangeProviderTo( int index )
        {
            _currentProvider = (ISearchProvider) _searchProviders[ index ];
            _currentProviderItem.Checked = false;
            foreach( MenuItem item in _menuProviders.MenuItems )
            {
                if( item is ProviderMenuItem && ((ProviderMenuItem)item).host == _currentProvider )
                {
                    _currentProviderItem = item;
                    _currentProviderItem.Checked = true;

                    Icon icon = ((ProviderMenuItem) item).host.Icon;
			        _btnProvider.AddIcon( icon, ImageListButton.ButtonState.Normal );

                    ShowProviderNameTip();
                    SetTooltip();
                    break;
                }
            }
        }

		private static void _searchQueryCombo_KeyPress( object sender, KeyPressEventArgs e )
		{
			if( e.KeyChar == '\r' )
			{
				e.Handled = true;
			}
		}

		private void _searchQueryCombo_SelectedIndexChanged( object sender, EventArgs e )
		{
			CurrentSearchText = _searchQueryCombo.Text;
		}

		private void _searchQueryCombo_Leave( object sender, EventArgs e )
		{
			CurrentSearchText = _searchQueryCombo.Text;
		}

		private void _searchQueryCombo_Enter( object sender, EventArgs e )
		{
			_searchQueryCombo.Items.Clear();
			LoadSavedQueries();
		}
        #endregion SearchQuery Combobox Events

        #region Provider Button Events
        private void _btnProviderChooser_Click(object sender, EventArgs e)
        {
            _menuProviders.Show(_btnProvider, new Point( 0, _btnProvider.Height ));
        }

        private void onProviderSelected( object sender, EventArgs e )
        {
            ProviderMenuItem mi = (ProviderMenuItem) sender;
            _currentProviderItem.Checked = false;
            mi.Checked = true;

            _currentProviderItem = mi;
            _currentProvider = mi.host;

			_btnProvider.AddIcon( mi.host.Icon, ImageListButton.ButtonState.Normal );
            _searchQueryCombo.Invalidate();

            ShowProviderNameTip();
            SetTooltip();
        }
        #endregion Provider Button Events

 		#region ICommandBar Interface Members

		public void SetSite( ICommandBarSite site )
		{
			_site = site;
			_grip.SetSite( site );
		}

		public Size MinSize
		{
			get { return new Size( 3 + 3 + 3 + 2 + c_nMinComboWidth, (int) (22 * _sizeScale.Height) ); }
		}

		public Size MaxSize
		{
			get { return new Size( int.MaxValue, int.MaxValue ); }
		}

		public Size OptimalSize
		{
			get { return new Size( 350, (int) (27 * _sizeScale.Height) ); }
		}

		public Size Integral
		{
			get { return new Size( 1, 1 ); }
		}

		#endregion

		#region Operations

		private void EnableCtrl()
		{
			EnableControls( true );
            SetTooltip();
        }

		/// <summary>
		/// Enables or disables the bar's controls
		/// (without disabling the toolbar itself which allows to drag it while the controls are disabled).
		/// </summary>
		/// <param name="enable">The desired enabled/disabled state.</param>
		public void EnableControls( bool enable )
		{
            if( Core.State != CoreState.ShuttingDown )
            {
    			foreach( Control control in Controls )
	    			control.Enabled = enable;
		        _btnProvider.BackColor = _searchQueryCombo.EditBkColor;
            }
		}

		public void Populate()
		{
            EnableControls( false );
            SetTooltip( "Text index is not yet available" );
			_searchQueryCombo.Text = string.Empty;

            Core.TextIndexManager.IndexLoaded += OnIndexConstructionComplete;

            InitializeSearchProviders();
		}

		public void FocusSearchBox()
		{
			_searchQueryCombo.Focus();
		}

		public void SetText( string text )
		{
			_searchQueryCombo.Text = text;
		}

		public void ShowAdvancedSearchDialog()
		{
			if( AdvSForm == null || AdvSForm.IsDisposed )
			{
				string searchText = CurrentSearchText.Trim();
				Trace.WriteLine( "SearchCtrl -- text for advanced search is [" + searchText + "]" );
				AdvSForm = new AdvancedSearchForm( searchText );
				AdvSForm.Show();
			}
			else
				AdvSForm.Activate();
		}

        private void SetTooltip()
        {
            SetTooltip( Core.TextIndexManager.GetSearchProviderTitle( _currentProvider ) );
        }

        private void SetTooltip( string text )
        {
			_tipReason.SetToolTip( this, text );
			_tipReason.SetToolTip( _btnProvider, text );
			_tipReason.SetToolTip( _btnProviderChooser, _ChooserTooltip );
			_tipReason.SetToolTip( _btnSearch, text );
			_tipReason.SetToolTip( _searchQueryCombo, text );
        }

        private void ShowProviderNameTip()
        {
            if( ( !_searchQueryCombo.Focused && String.IsNullOrEmpty( _searchQueryCombo.Text )) ||
                _searchQueryCombo.ForeColor == SystemColors.GrayText )
            {
                _searchQueryCombo.ForeColor = SystemColors.GrayText;
                _searchQueryCombo.Text = _currentProvider.Title;
            }
        }

        private void HideProviderNameTip()
        {
            if( _searchQueryCombo.ForeColor == SystemColors.GrayText )
            {
                _searchQueryCombo.ForeColor = SystemColors.InfoText;
                _searchQueryCombo.Text = string.Empty;
            }
        }
        #endregion

        private void _panelChoosingButtons_EnabledChanged(object sender, EventArgs e)
        {
            _panelChoosingButtons.BackColor = _panelChoosingButtons.Enabled ? Color.White : SystemColors.ControlDark;
        }
    }

    #region Menu Items
    internal class ProviderMenuItem : MenuItem
    {
        private const int _LeftIndent = 0;
        private const int _InterIconsIndent = 5;

        public ISearchProvider  host;
        public ProviderMenuItem( ISearchProvider isp, EventHandler eh ) : base( isp.ToString(), eh )
        {
            host = isp;
        }

        public static void mi_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            ProviderMenuItem pmi = (ProviderMenuItem) sender;
            ISearchProvider  isp = pmi.host;
            string text = isp.Title;
            Icon   icon = isp.Icon;

            SizeF textSize;
            textSize = e.Graphics.MeasureString( text, SystemInformation.MenuFont );
            e.ItemHeight = Math.Max( icon.Height, (int) Math.Ceiling( textSize.Height) );
            e.ItemWidth = _LeftIndent + icon.Width + 5 + (int) Math.Ceiling( textSize.Width );
        }

        public static void mi_DrawItem(object sender, DrawItemEventArgs e)
        {
            ProviderMenuItem pmi = (ProviderMenuItem) sender;
            ISearchProvider  isp = pmi.host;
            string text = isp.Title;
            Icon   icon = isp.Icon;

            Font font = SystemInformation.MenuFont;
            Rectangle glyphRegion = e.Bounds, iconRegion = e.Bounds, textRegion = e.Bounds;
            glyphRegion.X = _LeftIndent;
            glyphRegion.Width = SystemInformation.MenuCheckSize.Width;
            iconRegion.X += _LeftIndent + SystemInformation.MenuCheckSize.Width;
            iconRegion.Width = icon.Width;
            textRegion.X += _LeftIndent + SystemInformation.MenuCheckSize.Width + icon.Width + _InterIconsIndent;

            Brush brush;

            e.DrawBackground();
            if( (e.State & DrawItemState.Checked) != 0 )
                ControlPaint.DrawMenuGlyph( e.Graphics, glyphRegion, MenuGlyph.Bullet );
            e.Graphics.DrawIcon( icon, iconRegion );

            if( (e.State & DrawItemState.Selected) != 0 )
                brush = SystemBrushes.FromSystemColor( SystemColors.Menu );
            else
                brush = SystemBrushes.FromSystemColor( SystemColors.MenuText );
            e.Graphics.DrawString( text, font, brush, textRegion );
        }
    }

    internal class ProvidersHeaderMenuItem : MenuItem
    {
        private readonly string _strHeader;
        private readonly static Font _itemFont = new Font(SystemInformation.MenuFont.FontFamily, 8.25f, FontStyle.Bold);

        public string  Header {  get{  return _strHeader;  }  }

        public ProvidersHeaderMenuItem( string header ) : base( header )
        {
            _strHeader = header;
        }

        public static void mi_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            SizeF textSize = e.Graphics.MeasureString( ((ProvidersHeaderMenuItem) sender).Header, _itemFont );

            e.ItemHeight = (int) Math.Ceiling( textSize.Height );
            e.ItemWidth = (int) Math.Ceiling( textSize.Width );
        }

        public static void mi_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            Brush brush;
            if( (e.State & DrawItemState.Selected) != 0 )
                brush = SystemBrushes.FromSystemColor( SystemColors.Menu );
            else
                brush = SystemBrushes.FromSystemColor( SystemColors.MenuText );

            e.Graphics.DrawString( ((ProvidersHeaderMenuItem) sender).Header, _itemFont, brush, e.Bounds );
        }
    }
    #endregion Menu Items

    internal class SelfDrawnPanelWithBorder : Panel
    {
        public SelfDrawnPanelWithBorder()
        {
            SetStyle( ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true );
        }
    }

    #region ACMarginableComboBox
    internal class ACMarginableComboBox : ComboBox
    {
        private const int cMargin = 26;
        private Control _control;

        public void Attach( Control control )
        {
            _control = control;
            KeyPress += OnKeyPress;
            MoveControl();
        }

        #region OnKeyPress
        private void OnKeyPress( object sender, KeyPressEventArgs e )
        {
            if ( !e.KeyChar.Equals( (char)8 ) )
                SearchItems( ref e );
            else
                e.Handled = false;
        }

        /// <summary>
        /// Searches the combo box item list for a match and selects it.
        /// If no match is found, then selected index defaults to -1.
        /// </summary>
        private void SearchItems( ref KeyPressEventArgs e )
        {
            int selectionStart = SelectionStart;
            int selectionLength = SelectionLength;
            int selectionEnd = selectionStart + selectionLength;

            StringBuilder sb = new StringBuilder();
            sb.Append( Text.Substring( 0, selectionStart ) )
                .Append( e.KeyChar.ToString() )
                .Append( Text.Substring( selectionEnd ) );

            int index = FindString( sb.ToString() );
            
            e.Handled = (index != -1);
            if( e.Handled )
            {
                SelectedIndex = index;
                Select( selectionStart + 1, Text.Length - (selectionStart + 1) );
            }
        }
        #endregion OnKeyPress

        #region Controls Movement
        protected override void OnLayout( LayoutEventArgs levent )
        {
            base.OnLayout( levent );

            // Don't allow the editbox text go under the button 
            SetNearMargin( this, cMargin );
            MoveControl();
        }

        private void MoveControl()
        {
            if (_control != null)
            {
                Point currentLocation = _control.Location;
                RECT rcWindow = new RECT();
                IntPtr editHandle = ComboEdithWnd( Handle );
                Win32Declarations.GetWindowRect(editHandle, ref rcWindow);
                Point moveTo = new Point( rcWindow.left + 2,
                                          rcWindow.top + ((rcWindow.bottom - rcWindow.top) - _control.Height) / 2);

                moveTo = _control.Parent.PointToClient( moveTo );
                if( !currentLocation.Equals( moveTo ) )
                {
                    _control.Location = moveTo;
                    _control.BringToFront();
                }
            }
        }

        /// Gets the handle of the TextBox contained within a 
        /// ComboBox control.
        private static IntPtr ComboEdithWnd( IntPtr handle )
        {
            handle = Win32Declarations.FindWindowEx( handle, IntPtr.Zero, "EDIT", "\0" );
            return handle;
        }

        public Color EditBkColor
        {
            get
            {
                int val = Win32Declarations.GetBkColor( ComboEdithWnd( Handle ) );
                return Color.FromArgb( val );
            }
        }
        /// 
        /// Sets the far margin of a TextBox control or the
        /// TextBox contained within a ComboBox.
        /// 
        public static void SetNearMargin( Control ctl, int margin )
        {
            IntPtr handle = ctl.Handle;
            if( typeof( ComboBox ).IsAssignableFrom( ctl.GetType() ))
            {
                handle = ComboEdithWnd(handle);
            }
            Win32Declarations.SendMessage( handle, Win32Declarations.EM_SETMARGINS, (IntPtr)Win32Declarations.EC_LEFTMARGIN, (IntPtr)margin );
        }
        #endregion Controls Movement
   }
    #endregion ACMarginableComboBox
}