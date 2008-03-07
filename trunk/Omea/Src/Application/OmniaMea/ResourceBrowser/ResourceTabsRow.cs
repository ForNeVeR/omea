/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.CommandBar;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Implements a docked line with the resource tabs conrtol and search control.
	/// </summary>
	public class ResourceTabsRow : UserControl, ICommandBarSite
	{
		#region Data

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		/// <summary>
		/// The resource tabs control.
		/// </summary>
		internal TabSwitcher _barResourceTypeTabs;

		/// <summary>
		/// The Search control.
		/// </summary>
		protected SearchCtrl _barSearch;

		/// <summary>
		/// Gap between the search control and the resourec tabs control.
		/// </summary>
		private static readonly int c_nGap = 10;

		/// <summary>
		/// Horizontal margin of this control.
		/// </summary>
		private static readonly int c_nHorMargin = 2;

		/// <summary>
		/// Vertical margin of this control.
		/// </summary>
		private static readonly int c_nVerMargin = 0;

		/// <summary>
		/// Regulates the desired width of the <see cref="_barSearch"/> toolbar, as it was adjusted by the user.
		/// Initially populated with the <see cref="ICommandBar.OptimalSize"/> of the toolbar in <see cref="InitializeComponentSelf"/>.
		/// Persisted in settings.
		/// </summary>
		protected int _nDesiredToolbarWidth;

		#endregion

		#region Construction

		public ResourceTabsRow()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponentSelf();
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

		#region Visual Init

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponentSelf()
		{
			components = new Container();
			using( new LayoutSuspender( this ) )
			{
				// _barResourceTypeTabs 
				_barResourceTypeTabs = new TabSwitcher();
				_barResourceTypeTabs.Font = new Font( "Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((Byte) (204)) );
				_barResourceTypeTabs.Name = "_barResourceTypeTabs";
				_barResourceTypeTabs.TabIndex = 1;
				_barResourceTypeTabs.Location = new Point( 0, 6 );
				_barResourceTypeTabs.Size = new Size( 648, 27 );
				_barResourceTypeTabs.SetSite( this );

				// _barSearch
				_barSearch = new SearchCtrl();
				_barSearch.Name = "_barSearch";
				_barSearch.TabIndex = 2;
				_barSearch.Size = _barSearch.OptimalSize;
				_barSearch.SetSite( this );
				_nDesiredToolbarWidth = _barSearch.OptimalSize.Width;

				// This Control
				Height = 32;
				Controls.Add( _barResourceTypeTabs );
				Controls.Add( _barSearch );

				SetStyle( ControlStyles.AllPaintingInWmPaint
					| ControlStyles.CacheText
					| ControlStyles.ContainerControl
					| ControlStyles.ResizeRedraw
					| ControlStyles.Selectable
					| ControlStyles.UserPaint
					| ControlStyles.Opaque
				          , true );
			}
		}

		#endregion

		#region ICommandBarSite Interface Members

		public bool RequestMove( ICommandBar sender, Size offset )
		{
			if( !Object.ReferenceEquals( sender, _barSearch ) )
				throw new InvalidOperationException();

			_nDesiredToolbarWidth = _barSearch.Width - offset.Width; // Calc the new desired size
			_nDesiredToolbarWidth = _nDesiredToolbarWidth >= 1 ? _nDesiredToolbarWidth : 1; // Constrain
			PerformLayout(); // Apply the new desired width to the layout

			Refresh();

			return true; // Move allowed
		}

		public bool RequestSize( ICommandBar sender, Size difference )
		{
			throw new InvalidOperationException();
		}

		public bool PerformLayout( ICommandBar sender )
		{
			Core.UserInterfaceAP.QueueJob( "Layout the Row", new MethodInvoker( PerformLayout ) );
			return true;
		}

		#endregion

		#region Implementation — Overrides

		protected override void OnLayout( LayoutEventArgs levent )
		{
			using( new LayoutSuspender( _barSearch ) )
			using( new LayoutSuspender( _barResourceTypeTabs ) )
			{
				// Client rectangle that encounters for the margins
				Rectangle	client = ClientRectangle;
				client.Inflate( -c_nHorMargin, -c_nVerMargin );

				// Collect the minimax sizes of the controls
				Size sizeSearchMax = _barSearch.MaxSize;
				Size sizeSearchMin = _barSearch.MinSize;
				Size sizeSearchOpt = _barSearch.OptimalSize;
				Size sizeTabsMin = _barResourceTypeTabs.MinSize;
				Size sizeTabsMax = _barResourceTypeTabs.MaxSize;
				Size sizeTabsOpt = _barResourceTypeTabs.OptimalSize;

				// Restrict the search bar width
				int nSearchWidth = _nDesiredToolbarWidth;
				bool bSearch = true; // Presence of the control in the layout
				nSearchWidth = nSearchWidth >= sizeSearchMin.Width ? (nSearchWidth <= sizeSearchMax.Width ? nSearchWidth : sizeSearchMax.Width) : sizeSearchMin.Width;
				if( (sizeSearchMin.Width > client.Width) || (sizeSearchMin.Height > client.Height) )
				{
					nSearchWidth = 0; // Hide the search control if it does not fit
					bSearch = false;
				}

				// Restrict the resource tabs bar width
				int nResourceTabsWidth;
				bool bResourceTabs = true; // Presence of the control in the layout
				if( (sizeTabsMin.Width > client.Width) || (sizeTabsMin.Height > client.Height) ) // Resource tabs do not fit at all
				{
					nResourceTabsWidth = 0;
					bResourceTabs = false;
				}
				else // Resource tabs do fit
				{
					nResourceTabsWidth = client.Width - (bSearch ? nSearchWidth + c_nGap : 0); // Occupy the available space
					nResourceTabsWidth = nResourceTabsWidth >= 0 ? nResourceTabsWidth : 0; // Don't allow to drop below zero
					nResourceTabsWidth = nResourceTabsWidth <= sizeTabsMax.Width ? nResourceTabsWidth : sizeTabsMax.Width; // Limit by respecting the maximum size
				}

				// Negotiate the sizes, in case the resource tabs are present
				if( bResourceTabs )
				{
					// Resource tabs do not fit? Remove the gap.
					if( (nResourceTabsWidth < sizeTabsMin.Width) && (bSearch) )
						nResourceTabsWidth += c_nGap; // Remove the gap if short on space

					// Resource tabs still do not fit? Try shrinking the search control
					if( (nResourceTabsWidth < sizeTabsMin.Width) && (bSearch) )
					{
						nSearchWidth = client.Width - sizeTabsMin.Width; // Try to shrink the search control
						nSearchWidth = nSearchWidth >= sizeSearchMin.Width ? (nSearchWidth <= sizeSearchMax.Width ? nSearchWidth : sizeSearchMax.Width) : sizeSearchMin.Width; // Constrain its new width
						nResourceTabsWidth = client.Width - nSearchWidth; // Update the possible tabs width (no gap at this point)
					}

					// Resource tabs still do not fit? Drop the search control at all
					if( (nResourceTabsWidth < sizeTabsMin.Width) && (bSearch) )
					{
						nSearchWidth = 0;
						bSearch = false;
						nResourceTabsWidth = client.Width - nSearchWidth; // Update the possible tabs width
					}

					// Resource tabs still do not fit? Drop em!
					if( nResourceTabsWidth < sizeTabsMin.Width )
					{
						bResourceTabs = false;
						nResourceTabsWidth = 0;
					}
				}

				//////////////////////////
				// Apply layouting to the controls.

				// Resource tabs
				if( (bResourceTabs) && (nResourceTabsWidth > 0) )
				{
					_barResourceTypeTabs.Left = client.Left;
					_barResourceTypeTabs.Height = sizeTabsOpt.Height <= client.Height ? sizeTabsOpt.Height : client.Height; // No more than optimal height
					_barResourceTypeTabs.Top = client.Bottom - _barResourceTypeTabs.Height; // Align at bottom
					_barResourceTypeTabs.Visible = true;
					_barResourceTypeTabs.Width = nResourceTabsWidth;
				}
				else
					_barResourceTypeTabs.Visible = false;

				// Search Control
				if( (bSearch) && (nSearchWidth > 0) )
				{ // Fits, place it
					_barSearch.Width = nSearchWidth;
					_barSearch.Left = client.Right - _barSearch.Width;
					_barSearch.Height = sizeSearchOpt.Height < client.Height ? sizeSearchOpt.Height : client.Height;
					_barSearch.Top = client.Top + (client.Height - _barSearch.Height) / 2;	// Center vertically in the space
					_barSearch.Visible = true;
				}
				else // Does not fit, turn off
					_barSearch.Visible = false;
			}

			// Apply the visual changes
			Invalidate( false );
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			using( Brush brush = new SolidBrush( SystemColors.Control ) )
				e.Graphics.FillRectangle( brush, ClientRectangle );

		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the Resource Type Tabs control.
		/// </summary>
		internal TabSwitcher ResourceTypeTabs
		{
			get { return _barResourceTypeTabs; }
		}

		/// <summary>
		/// Gets the Search control.
		/// </summary>
		public SearchCtrl SearchBar
		{
			get { return _barSearch; }
		}

		/// <summary>
		/// The desired search bar width (that is acquired as soon as it does not conflict with min-size, max-size, and presence of the resource tabs).
		/// This is the value that should be persisted.
		/// </summary>
		public int DesiredSearchBarWidth
		{
			get { return _nDesiredToolbarWidth; }
			set
			{
				if(value <= 0)
					throw new ArgumentOutOfRangeException("value", value, "The desired width must be a positive value.");
				_nDesiredToolbarWidth = value;
			}
		}

		#endregion

		#region Operations

		/// <summary>
		/// Saves or loads the resource tabs row settings.
		/// </summary>
		/// <param name="isStoring"><c>True</c> to save, <c>False</c> to load.</param>
		public void SerializeSettings(bool isStoring)
		{
			string	section = "MainForm";
			string	sDesiredToolbarWidth = "ResourceTabsRow.DesiredToolbarWidth";
			if(isStoring)
				Core.SettingStore.WriteInt( section, sDesiredToolbarWidth, _nDesiredToolbarWidth );
			else
				_nDesiredToolbarWidth = Core.SettingStore.ReadInt( section, sDesiredToolbarWidth, _nDesiredToolbarWidth );
		}

		#endregion
	}
}