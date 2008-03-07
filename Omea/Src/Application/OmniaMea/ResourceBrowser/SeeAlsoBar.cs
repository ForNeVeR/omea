/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
	/// <summary>
	/// The bar which shows resources of other types except the ones shown in the
	/// current tab that are present in the specified resource list.
	/// </summary>
	internal class SeeAlsoBar : UserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		private ControlPool _iconPool;

		private ControlPool _linkLabelPool;

		private ColorScheme _colorScheme;

		private JetLinkLabel _title;

		private bool _active;

		private Font _linkFont;

		/// <summary>
		/// Horizontal gap between the adjacent labels on the bar.
		/// </summary>
		protected static readonly int c_nInterLabelHorizontalGap = 12;

		/// <summary>
		/// Color of the background beneath the control, to simulate the transparent parts.
		/// </summary>
		private Color _colorUnder;

		public event SeeAlsoEventHandler SeeAlsoLinkClicked;

		public SeeAlsoBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			SetStyle( ControlStyles.AllPaintingInWmPaint
				| ControlStyles.CacheText
				| ControlStyles.ContainerControl
				| ControlStyles.ResizeRedraw
				| ControlStyles.Selectable
				| ControlStyles.UserPaint, true );
			UpdateColors();

			_iconPool = new ControlPool( this, new ControlPoolCreateDelegate( OnCreateIcon ) );
			_linkLabelPool = new ControlPool( this, new ControlPoolCreateDelegate( OnCreateLinkLabel ) );
			_linkFont = new Font( "Tahoma", 8 );
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
				_linkFont.Dispose();
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
			this._title = new JetLinkLabel();
			this.SuspendLayout();
			// 
			// _captionLabel
			// 
			this._title.Font = new System.Drawing.Font( "Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte) (204)) );
			_title.ClickableLink = false;
			this._title.Location = new System.Drawing.Point( 10, 2 );
			this._title.Name = "_title";
			this._title.TabIndex = 0;
			this._title.Text = "See Also:";
			this._title.Click += new System.EventHandler( this._captionLabel_Click );
			_title.BackColor = Color.Blue;
			// 
			// SeeAlsoBar
			// 
			this.Controls.Add( this._title );
			this.Font = new System.Drawing.Font( "Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte) (204)) );
			this.Name = "SeeAlsoBar";
			this.Size = new System.Drawing.Size( 150, 24 );
			this.ResumeLayout( false );
		}

		#endregion

		[DefaultValue( null )]
		public ColorScheme ColorScheme
		{
			get { return _colorScheme; }
			set
			{
				if( _colorScheme != value )
				{
					_colorScheme = value;
					UpdateColors();
				}
			}
		}

		[DefaultValue( false )]
		public bool Active
		{
			get { return _active; }
			set
			{
				if( _active != value )
				{
					_active = value;
					UpdateColors();
				}
			}
		}

		/// <summary>
        /// Shows the "See also" links for every resource type that is found in the
        /// specified resource list but not included in the specified array of resource types.
		/// </summary>
        public void ShowLinks( IResource ownerResource, IResourceList resList,
                               string[] excludeResTypes, int excludeLinkPropId )
		{
            #region Preconditions
            if ( !Core.UserInterfaceAP.IsOwnerThread )
            {
                throw new InvalidOperationException( "See Also bar must be shown from the UI thread" );
            }
            #endregion Preconditions

			CountedSet countByTab = new CountedSet();

			IntArrayList sourceLinkTypes = IntArrayListPool.Alloc();
            try
            {
                foreach( IPropType propType in Core.ResourceStore.PropTypes )
                {
                    if( propType.HasFlag( PropTypeFlags.SourceLink ) )
                    {
                        sourceLinkTypes.Add( propType.Id );
                    }
                }

                WorkspaceManager wspMgr = Core.WorkspaceManager as WorkspaceManager;
                IResource activeWorkspace = Core.WorkspaceManager.ActiveWorkspace;

                bool filterViewsExclusive = false;
                if ( ownerResource.Type == "SearchView" && !ownerResource.HasProp( "ShowInAllTabs" ) )
                {
                    filterViewsExclusive = true;
                }

                int outsideWorkspaceCount = 0, unfoundCount = 0;

                IResourceList actualRcs = resList;
                lock( actualRcs )
                {
                    foreach( IResource res in actualRcs.ValidResources )
                    {
                        string resType = res.Type;
                        if( resType == "Fragment" )
                        {
                            resType = res.GetStringProp( Core.Props.ContentType );
                            // guard for broken DB (OM-12080)
                            if( resType == null || !Core.ResourceStore.ResourceTypes.Exist( resType ) )
                            {
                                continue;
                            }
                        }

                        if( activeWorkspace != null && !activeWorkspace.HasLink( wspMgr.Props.WorkspaceVisible, res ) )
                        {
                            outsideWorkspaceCount++;
                            continue;
                        }

                        if( excludeResTypes != null && IsTabType( resType, excludeResTypes ) )
                            continue;

                        if( excludeLinkPropId >= 0 && res.HasProp( excludeLinkPropId ) )
                            continue;

                        if ( filterViewsExclusive && Core.ResourceTreeManager.AreViewsExclusive( resType ) )
                            continue;

                        bool found = false;
                        if( Core.ResourceStore.ResourceTypes[ resType ].HasFlag( ResourceTypeFlags.FileFormat ) )
                        {
                            foreach( int sourceLinkType in sourceLinkTypes )
                            {
                                if( res.HasProp( sourceLinkType ) )
                                {
                                    string tabId = Core.TabManager.FindLinkPropTab( sourceLinkType );
                                    if( tabId != null )
                                    {
                                        countByTab.Add( tabId );
                                    }

                                    found = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            string resourceTabId = Core.TabManager.FindResourceTypeTab( resType );
                            if( resourceTabId != null )
                            {
                                countByTab.Add( resourceTabId );
                                found = true;
                            }
                        }

                        if( !found )
                        {
                            unfoundCount++;
                        }
                    }
                }

                if( countByTab.Count == 0 && unfoundCount == 0 && outsideWorkspaceCount == 0 )
                {
                    Visible = false;
                    return;
                }

                _iconPool.MoveControlsToPool();
                _linkLabelPool.MoveControlsToPool();

                int x = _title.Right + 4;

                int labelCount = 0;
                for( int i = 0; i < Core.TabManager.Tabs.Count; i++ )
                {
                    string tabId = Core.TabManager.Tabs[ i ].Id;
                    int count = countByTab[ tabId ];
                    if( count > 0 )
                    {
                        string text = Core.TabManager.Tabs[ i ].Name + " (" + count.ToString() + ")";
                        AddLinkLabel( tabId, text, ref x );
                        labelCount++;
                    }
                }

                if( (labelCount > 1 || unfoundCount > 0) && (excludeResTypes != null || excludeLinkPropId >= 0) )
                {
                    AddLinkLabel( "", "All Results (" + (resList.Count - outsideWorkspaceCount) + ")", ref x );
                }

                if( outsideWorkspaceCount > 0 )
                {
                    string sDefaultWspName = ((WorkspaceManager)Core.WorkspaceManager).Props.DefaultWorkspaceName;
                    AddLinkLabel(  "<Main>", String.Format("{0} Workspace ({1})", sDefaultWspName, resList.Count), ref x );
                }

                _iconPool.RemovePooledControls();
                _linkLabelPool.RemovePooledControls();
                Visible = true;
            }
            finally
            {
                IntArrayListPool.Dispose( sourceLinkTypes );
            }
		}

		private void AddLinkLabel( object tag, string text, ref int x )
		{
			JetLinkLabel linkLabel = (JetLinkLabel) _linkLabelPool.GetControl();
			linkLabel.Text = text;
			linkLabel.Location = new Point( x, 2 );
			linkLabel.Tag = tag;
			SetLinkColor( linkLabel );

			x += linkLabel.PreferredWidth + 12;
		}

		private void UpdateColors()
		{
			if( _colorScheme != null )
			{
				// Labels color
				foreach( Control control in Controls )
					SetLinkColor( control );

				// Bar caption color (foreground)
				_title.ForeColor = _colorScheme.GetColor( "SeeAlso.Caption" + ActiveColorSuffix );
			}

			Invalidate(false);
		}

		/// <summary>
		/// Applies back and fore color to a control.
		/// </summary>
		protected void SetLinkColor( Control control )
		{
			if( _colorScheme != null )
			{
				control.ForeColor = _colorScheme.GetColor( "SeeAlso.Link" + ActiveColorSuffix );
				control.BackColor = _colorScheme.GetColor( "SeeAlso.Background" + ActiveColorSuffix );
			}
		}

		/// <summary>
		/// Color name suffix which denotes the active/inactive state of the control.
		/// </summary>
		protected string ActiveColorSuffix
		{
			get { return _active ? "Active" : "Inactive"; }
		}

		/// <summary>
		/// Checks if the type is present in the specified array. 
		/// </summary>
		private bool IsTabType( string resType, string[] tabTypes )
		{
			foreach( string tabType in tabTypes )
			{
				if( resType == tabType )
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Creates an icon to be displayed in the bar.
		/// </summary>
		private Control OnCreateIcon()
		{
			ImageListPictureBox pbox = new ImageListPictureBox();
			pbox.ImageList = Core.ResourceIconManager.ImageList;
			return pbox;
		}

		/// <summary>
		/// Creates a LinkLabel to be displayed in the bar.
		/// </summary>
		private Control OnCreateLinkLabel()
		{
			JetLinkLabel linkLabel = new JetLinkLabel();
			linkLabel.AutoSize = true;
			linkLabel.Font = _linkFont;
			linkLabel.Click += new EventHandler( OnLinkLabelClicked );
			return linkLabel;
		}

		private void OnLinkLabelClicked( object sender, EventArgs e )
		{
			// process asynchronously because the SeeAlso handler will dispose the label
			JetLinkLabel senderLabel = (JetLinkLabel) sender;
			if( SeeAlsoLinkClicked != null )
			{
				bool mainWorkspace = false;
				string tabId = senderLabel.Tag as string;
				if( tabId == "<Main>" )
				{
					mainWorkspace = true;
					tabId = "";
				}

				Core.UIManager.QueueUIJob( new SeeAlsoEventHandler( SeeAlsoLinkClickedAsync ),
				                           new object[] {this, new SeeAlsoEventArgs( tabId, mainWorkspace )} );
			}
		}

		private void SeeAlsoLinkClickedAsync( object sender, SeeAlsoEventArgs e )
		{
			SeeAlsoLinkClicked( this, e );
		}

		private void _captionLabel_Click( object sender, EventArgs e )
		{
			OnClick( EventArgs.Empty );
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			// Pick the background color from the scheme
			Color colorBack = ColorScheme.GetColor( _colorScheme, "SeeAlso.Background" + (_active ? "Active" : "Inactive"), Color.Black );

			// There's a cutting on the left of the bar (diagonal one).
			// This is the size of its bounding box, equal to the bar height, as it is at 45 deg.
			int nCuttingBoxSize = ClientRectangle.Height;

			//////////////////
			// Paint Background

			////
			// Rectangular (main) part

			Rectangle rectBack = ClientRectangle;

			rectBack.X += nCuttingBoxSize; // Leave some space on the left for the cutting
			rectBack.Width -= nCuttingBoxSize;

			using( Brush brush = new SolidBrush( colorBack ) )
				e.Graphics.FillRectangle( brush, rectBack );

			////
			// Cutting on the left

			// Points of the cutting triangle: left-top, right-top, right-bottom
			Point[] ptCuttingTopRight = new Point[] {new Point( 0, 0 ), new Point( nCuttingBoxSize, 0 ), new Point( nCuttingBoxSize, nCuttingBoxSize )};
			Point[] ptCuttingBottomLeft = new Point[] {new Point( 0, 0 ), new Point( 0, nCuttingBoxSize ), new Point( nCuttingBoxSize, nCuttingBoxSize )};
			byte[] types = new byte[] {(byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line};
			using( Brush brushMyBackground = new SolidBrush( colorBack ) )
			using( Brush brushForeignBackground = new SolidBrush( Undercolor ) )
			using( GraphicsPath pathTopRight = new GraphicsPath( ptCuttingTopRight, types ) )
			using( GraphicsPath pathBottomLeft = new GraphicsPath( ptCuttingBottomLeft, types ) )
			{
				e.Graphics.FillPath( brushMyBackground, pathTopRight );
				e.Graphics.FillPath( brushForeignBackground, pathBottomLeft );
			}
		}

		/// <summary>
		/// Color beneath the control.
		/// </summary>
		public Color Undercolor
		{
			get { return _colorUnder; }
			set
			{
				if(value.A != 255)
					throw new ArgumentException("Transparent colors not allowed.", "value");
				_colorUnder = value;
				Invalidate(false);
			}
		}

		/// <summary>
		/// Layout of the bar has to be recalculated, layout the controls.
		/// </summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			// Relocate the controls
			int x = -1; // The current x-coordinate
			int nUpperGap; // Gap above the current control, or its location's y-coord
			int nMyHeight = Height; // Height of the bar
			foreach( Control control in Controls )
			{
				nUpperGap = (nMyHeight - control.Height) / 2;

				if( x < 0 ) // For the first control, calc the starting X according to the cutting
					x = nUpperGap + control.Height;

				// Move
				control.Location = new Point( x, nUpperGap );

				// Skip the control
				x += control.Width + c_nInterLabelHorizontalGap;
			}

			// Apply the visual changes
			Invalidate(false);
		}
	}

	public class SeeAlsoEventArgs : EventArgs
	{
		private string _tabId;

		private bool _mainWorkspace;

		internal SeeAlsoEventArgs( string tabId, bool mainWorkspace )
		{
			_tabId = tabId;
			_mainWorkspace = mainWorkspace;
		}

		public string TabId
		{
			get { return _tabId; }
		}

		public bool MainWorkspace
		{
			get { return _mainWorkspace; }
		}
	}

	public delegate void SeeAlsoEventHandler( object sender, SeeAlsoEventArgs e );
}