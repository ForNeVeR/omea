/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.MshtmlBrowser;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.Interop;

namespace JetBrains.Omea
{
	/// <summary>
	/// Displays the specified resource list as a newspaper.
	/// </summary>
	public class NewspaperViewer : UserControl, IContextProvider, ICommandProcessor
	{
		#region Data

		/// <summary>
		/// A bar with newspaper navigation and restriction controls.
		/// </summary>
		protected NewspaperBar _bar;

		/// <summary>
		/// Display options for the resource browser which is replaced by the newspaper view in this case.
		/// </summary>
		protected ResourceListDisplayOptions _displayoptions;

		/// <summary>
		/// The Web Browser instance in which the newspaper is being viewed.
		/// </summary>
		protected MshtmlBrowserControl _browser;

		/// <summary>
		/// Nest of the browser control. Should be used as a whole with it.
		/// </summary>
		protected MshtmlBrowserNest _nest;

		/// <summary>
		/// String ID of this newspaper instance.
		/// It's used for constructing the IDs for HTML elements on the generated newspaper page.
		/// </summary>
		protected string _sNewspaperID = null;

		/// <summary>
		/// Maps string names of the drop effect combinations into the combined members of <see cref="DragDropEffects"/> enum.
		/// </summary>
		protected static Hashtable _hashStringToDropEffect = null;

		#region Constants

		/// <summary>
		/// When jumping to an item while doing goto-next or goto-previous, amount of visible space to be left after the item (in the direction of a jump).
		/// </summary>
		protected int c_nMinSpaceAfterItemWhenJumping = 30;

		/// <summary>
		/// When jumping to an item while doing goto-next or goto-previous, the minimum value for visible space after the item divided on the visible space before the item.
		/// </summary>
		protected double c_fMinRelationOfSpaceAfterToSpaceBefore;

		/// <summary>
		/// When jumping to an item while doing goto-next or goto-previous, the desired (to be established when minimum is not respected) value for visible space after the item divided on the visible space before the item.
		/// </summary>
		protected double c_fDesiredRelationOfSpaceAfterToSpaceBefore;

		/// <summary>
		/// When newspaper is being scrolled smoothly, there's source pos and target pos. The scrolling is done so that distance between them is reduced <see cref="c_nScrollFactor"/> times on each step.
		/// </summary>
		protected int c_nScrollFactor = 2;

		/// <summary>
		/// The minimum scrolling step. If the desired step of smooth scrolling is smaller than that, then scrolling finishes in an instant by jumping to the target position.
		/// </summary>
		protected int c_nMinScrollStep = 10;

		/// <summary>
		/// Enables display of additional debug information.
		/// </summary>
		protected bool c_bShowItemNumbers = false;

		/// <summary><seealso cref="_bScrollSmoothly"/>
		/// Defines whether smooth scrolling is allowed or not.
		/// </summary>
		protected bool c_bAllowSmoothScrolling = true;

		/// <summary>
		/// Defines whether the items are selected by mouse hover.
		/// </summary>
		protected bool c_bAllowHoverSelection = true;

		/// <summary>
		/// Timeout for the hover selection — amount of time between mouse-entering the item and setting selection to it, in milliseconds.
		/// </summary>
		protected int c_nHoverSelectionTimeout = 500;

		/// <summary>
		/// Interval for the scrolling timer, in milliseconds.
		/// </summary>
		protected int c_nScrollTimerInterval = 100;

		/// <summary>
		/// Color for painting the newspaper border.
		/// </summary>
		public static readonly Color c_colorBorder = Color.FromArgb( 88, 80, 159 );

		#endregion

		#region Scrolling Data

		/// <summary>
		/// The desired scrolling position of the HTML element that represents the newspaper body (<see cref="NewspaperHtmlElement"/>).
		/// <c>-1</c> means do not scroll (and no async/smooth scrolling is currently in progress).
		/// </summary>
		protected int _nScrollTargetPos = -1;

		/// <summary>
		/// A timer that handles the newspaper scrolling.
		/// </summary>
		protected Timer _timerScroll = null;

		/// <summary>
		/// While scrolling, stores the previous scrolling position.
		/// </summary>
		protected int _nScrollPrevPos = -1;

		/// <summary>
		/// While scrolling, holds the scroll direction (<c>True</c> if down).
		/// As soon as this direction is violated (eg by user's scrolling the newspaper too), the scrolling process is aborted in order to prevent interfering with the user scrolling process.
		/// </summary>
		protected bool _bScrollDir;

		/// <summary>
		/// Indicates whether scrolling has to be smooth.
		/// If <c>False</c>, the newspaper is scrolled to the new position in an instant.
		/// If <c>True</c>, the scrolling is done smoothly so that the user could see what happens to the damn newspaper.
		/// Also this is affected by the newspaper settings.
		/// </summary>
		private bool _bScrollSmoothly = true;

		#endregion

		/// <summary>
		/// An object that manages the newspaper state.
		/// </summary>
		protected NewspaperManager _man = new NewspaperManager();

		/// <summary>
		/// State of the newspaper viewer.
		/// </summary>
		protected NewspaperState _state = NewspaperState.Deactivated;

		/// <summary>
		/// A list of items supplied for display in newspaper, stored here while the newspaper is in the state <see cref="NewspaperState.Activating"/>.
		/// </summary>
		protected IResourceList _itemsBackupCopy = null;

		/// <summary>
		/// Item on which the mouse cursor is being hovered.
		/// When mouse cursor enters item bounds, this field is set and a timer is started. If mouse won't leave until timer elapses, the item is considered to be hovered.
		/// </summary>
		protected IResource _itemHovered = null;

		/// <summary>
		/// The item that should be selected in the newspaper when it finishes initializing.
		/// Valid only in the <see cref="NewspaperState.Activating"/> state.
		/// </summary>
		protected IResource _itemToSelect = null;

		/// <summary>
		/// Maps color names that can be used in stylesheets to their particular values.
		/// </summary>
		protected static Hashtable _hashMacros = null;

		/// <summary>
		/// Contains a list of items that are considered dirty and have to be updated as soon as possible.
		/// When an item is added or changes, its content is not build immediately, but that's done async by the means of this list.
		/// </summary>
		protected HashSet _itemsDirty = null;

		/// <summary>
		/// Defines which borders of the newspaper to draw.
		/// </summary>
		protected AnchorStyles _borders = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

		/// <summary>
		/// Defines the client rectangle, that is, the control's inner area minus the border at the specific sides.
		/// Filled in by <see cref="OnLayout"/>, used in <see cref="OnPaint"/> for painting the background.
		/// </summary>
		protected Rectangle _rectClient = Rectangle.Empty;

		/// <summary>
		/// Displays string messages in the status bar.
		/// </summary>
		protected IStatusWriter _statuswriter;

		/// <summary>
		/// The object that provides an external context to the newspaper.
		/// <c>Null</c> means "do it yourself".
		/// </summary>
		protected IContextProvider _contextprovider = null;

		#endregion

		#region Construction

		internal NewspaperViewer()
		{
			// Load the constants
			InitializeConstants();

			// Create controls
			InitializeComponentSelf();

			// Other components
			_timerScroll = new Timer();
			_timerScroll.Enabled = false;
			_timerScroll.Interval = c_nScrollTimerInterval;
			_timerScroll.Tick += new EventHandler( OnScrollTimerTick );
		}

		static NewspaperViewer()
		{
			// Initialize the dropeffect enumeration
			_hashStringToDropEffect = new Hashtable();
			lock( _hashStringToDropEffect )
			{
				_hashStringToDropEffect[ "copy" ] = DragDropEffects.Copy;
				_hashStringToDropEffect[ "link" ] = DragDropEffects.Link;
				_hashStringToDropEffect[ "move" ] = DragDropEffects.Move;
				_hashStringToDropEffect[ "copyLink" ] = DragDropEffects.Copy | DragDropEffects.Link;
				_hashStringToDropEffect[ "copyMove" ] = DragDropEffects.Copy | DragDropEffects.Move;
				_hashStringToDropEffect[ "linkMove" ] = DragDropEffects.Link | DragDropEffects.Move;
				_hashStringToDropEffect[ "all" ] = DragDropEffects.All;
				_hashStringToDropEffect[ "none" ] = DragDropEffects.None;
			}

			// Initialize the colors list
			_hashMacros = new Hashtable();
			lock( _hashMacros )
			{
				_hashMacros[ "Color.Background" ] = ColorManagement.Hex( SystemColors.Window );
				_hashMacros[ "Color.Text" ] = ColorManagement.Hex( SystemColors.WindowText );
				_hashMacros[ "Color.NormalBorder" ] = ColorManagement.Hex( SystemColors.ControlDark );
				_hashMacros[ "Color.SelectedItemBorder" ] = ColorManagement.Hex( SystemColors.Highlight );
				_hashMacros[ "Color.DeletedItemBackground" ] = ColorManagement.Hex( SystemColors.ControlLight );
				_hashMacros[ "Color.UnreadItemBackground" ] = ColorManagement.Hex( ColorManagement.Mix( SystemColors.Highlight, SystemColors.Window, 0.1 ) );

				_hashMacros[ "Font.Face" ] = "Verdana";
				_hashMacros[ "Font.Size" ] = "small";
				_hashMacros[ "Font.Heading1Face" ] = "Verdana";
				_hashMacros[ "Font.Heading1Size" ] = "14pt";
				_hashMacros[ "Font.Heading2Face" ] = "Verdana";
				_hashMacros[ "Font.Heading2Size" ] = "12pt";
			}
		}

		#endregion

		#region Data Types

		/// <summary>
		/// States of the newspaper viewer.
		/// </summary>
		public enum NewspaperState
		{
			/// <summary>
			/// The newspaper has not been shown yet or has already been hidden.
			/// </summary>
			Deactivated,

			/// <summary>
			/// The newspaper has received the Show command and is starting up to reach the <see cref="Activated"/> state.
			/// Generally, the browser is awaiting for control creation (which occurs not before the window gets visible on screen),
			/// and then for completeion of newspaper template loading.
			/// </summary>
			Activating,

			/// <summary>
			/// The newspaper is shown and running.
			/// </summary>
			Activated
		}

		#endregion

		#region Operations

		public void ShowNewspaper( IResource ownerResource, IResourceList resources,
		                           ResourceListDisplayOptions options )
		{
			// Safety check
			if( _state != NewspaperState.Deactivated )
				throw new InvalidOperationException( "The newspaper view must be deinitialized before reuse." );
			if( options == null )
				throw new ArgumentNullException( "options" );

			// Listen to changes in the settings
			Core.UIManager.AddOptionsChangesListener( "Omea", "General", new EventHandler( OnSettingsChanged ) );
			InitializeConstants();

			_state = NewspaperState.Activating;
			Trace.WriteLine( String.Format( "NewspaperView has been shown. Entering {0} state.", _state ), "[NPV]" );

			// Store the parameters
			_displayoptions = options;
			_itemsBackupCopy = resources;
			_itemToSelect = null;

			// Generate an unique newspaper ID for this newspaper
			_sNewspaperID = Guid.NewGuid().ToString();

			///////
			// Initiate the browser preparations
			// As soon as they're thru, the newspaper will be activated

			// Place the WebBrowser control on the newspaper form
			_nest = Core.WebBrowser as MshtmlBrowserNest;
			if( _nest == null )
				_nest = new MshtmlBrowserNest(); // Create the MSHTML browser in case the default browser is not MSHTML
			_browser = _nest.BrowserControl;

			// Stop the current Browser operation, so that we won't get the Complete notification for a navigation we did not initiate
			try
			{
				_browser.Stop();
			}
			catch(Exception ex)
			{
				// If hte browser control is dead at the moment, just catch the exception silently
				 Trace.WriteLine("The newspaper failed to stop the browser control when initiating. Probably it's dead, will be resurrected when loading the content.\n" + ex.Message);
			}

			//_browser.ExternalObject = new ExternalObject( this );
			_browser.add_KeyDown( new KeyEventHandler( OnBrowserKeyDown ) );
			_browser.ContextProvider = this; // Information about the selected event, default command processor, and so on
			_nest.TabIndex = 1;
			_nest.Select();
			_browser.add_DownloadComplete( new EventHandler( ActivateNewspaper ) );
			_browser.add_BeforeNavigate( new BeforeNavigateEventHandler( OnBeforeNavigate ) );
			if( _nest.Parent != this ) // Add to the form (if it's not there yet)
				Controls.Add( _nest );
			_browser.Focus(); // Set focus so that the keyboard strokes were processed correctly

			// Obtain the status writer
			if( _statuswriter == null )
				_statuswriter = Core.UIManager.GetStatusWriter( this, StatusPane.UI );

			// Load the newspaper template (an empty newspaper page with the styles filled in and ready for action)
			LoadNewspaperTemplate( resources );

			PerformLayout();
		}

		/// <summary>
		/// Invoked by the newspaper user when the newspaper is about to disappear.
		/// </summary>
		public void HideNewspaper()
		{
			if( (_state != NewspaperState.Activated) && (_state != NewspaperState.Activating) )
				throw new InvalidOperationException( "Trying to deactivate a newspaper view that has not been activated yet or has already been deactivated." );

			///////////////////
			// Logical Deinit
			// (non-visual components)
			try
			{
				_nScrollTargetPos = _nScrollPrevPos = -1; // Disable scrolling
				_statuswriter.ClearStatus();

				// In case we're still waiting for the DocumentComplete event, unsubscribe from it
				// Otherwise, we'll get into the handler in an inacceptible state
				if( _browser != null )
					_browser.remove_DownloadComplete( new EventHandler( ActivateNewspaper ) );

				if( _state == NewspaperState.Activated )
				{
					if( !_man.IsInitialized )
						throw new InvalidOperationException( "The newspaper is active, but the newspaper manager is not initialized." );

					// Save settings and shutdown the filterer
					_man.Deinitialize();
					_man.Deinitializing -= new EventHandler( OnManDeinitializing );
					_man.ItemAdded -= new NewspaperManager.ItemAddedEventHandler( OnManItemAdded );
					_man.ItemChanged -= new NewspaperManager.ItemChangedEventHandler( OnManItemChanged );
					_man.ItemRemoved -= new ResourceEventHandler( OnManItemRemoved );
					_man.SelectedItemChanged -= new ResourceEventHandler( OnManSelectedItemChanged );
					_man.PagingChanged -= new EventHandler( OnManPagingChanged );
					_man.EnsureVisible -= new NewspaperManager.EnsureVisibleEventHandler( OnManEnsureVisible );
					_man.ItemDeselected -= new ResourceEventHandler( OnManItemSelectedOrDeselected );
					_man.ItemSelected -= new ResourceEventHandler( OnManItemSelectedOrDeselected );
					_man.EnterPage -= new EventHandler( OnManEnterPage );
					_man.ItemsInViewChanged -= new EventHandler( OnManItemsInViewChanged );

					_itemsDirty.Clear();
					_itemsDirty = null;
				}
				else if( _state == NewspaperState.Activating )
				{
					if( !_man.IsUninitialized )
						throw new InvalidOperationException( "The newspaper has not been activated, but the newspaper manager is not uininitialized." );
				}

				// Deinit the browser
				_browser.ExternalObject = null;
				_browser.remove_KeyDown( new KeyEventHandler( OnBrowserKeyDown ) );
				_browser.remove_BeforeNavigate( new BeforeNavigateEventHandler( OnBeforeNavigate ) );
				_browser.ContextProvider = null;
				_browser.Dock = DockStyle.Fill;

				// Stop listening to changes in the settings
				Core.UIManager.RemoveOptionsChangesListener( "Omea", "General", new EventHandler( OnSettingsChanged ) );
			}
			catch( Exception ex )
			{
				// Trap and report the exceptions
				// This provides that even if there is an error in newspaper deinit sequence, it will not prevent from switching to another view
				Core.ReportException( new Exception( "Failed to complete the newspaper deinitialization.", ex ), ExceptionReportFlags.AttachLog );
			}
			finally
			{
				if( _man.IsUninitialized )
				{
					_state = NewspaperState.Deactivated;
					Trace.WriteLine( String.Format( "NewspaperView has been hidden. Entering {0} state.", _state ), "[NPV]" );
				}
			}

			//////////////////
			// Visual DeInit

			try
			{
				// Detach the browser control
				if( _nest != Core.WebBrowser )
				{
					Controls.Remove( _nest );
					_nest.Dispose();
				}

				_nest = null;
				_browser = null;

				PerformLayout();
			}
			catch( Exception ex )
			{
				// Trap and report the exceptions
				// This provides that even if there is an error in newspaper deinit sequence, it will not prevent from switching to another view
				Core.ReportException( new Exception( "Failed to complete the newspaper visual deinitialization.", ex ), ExceptionReportFlags.AttachLog );
			}

			PerformLayout();
			SafeFireEventAsync( ItemsInViewCountChanged ); // Update the counters
			SafeFireEventAsync( SelectedResourcesChanged ); // Update the selected item information
		}

		#region ResourceBrowser-ish API

		/// <summary>
		/// Sets the selection in the newspaper to the specified resource.
		/// </summary>
		/// <param name="res">The resource to select.</param>
		/// <returns>
		/// <para><c>True</c> if either the resource was selected successfully, or the newspaper was not initialized yet and the resource was schedulled for later selection.</para>
		/// <para><c>False</c> if the resource does not belong to the newspaper, be it initialized or not.</para>
		/// </returns>
		/// <remarks>Calling this member on a non-initialized newspaper causes an exception.</remarks>
		public bool SelectResource( IResource res )
		{
			Trace.WriteLine( String.Format( "NewspaperView was requsted to select \"{0}\" #{1}, the state is {2}.", res.DisplayName, res.OriginalId, _state ), "[NPV]" );
			switch( _state )
			{
			case NewspaperState.Deactivated:
				throw new InvalidOperationException( "The newspaper has not been initialized yet." );
			case NewspaperState.Activating:
				if( _itemsBackupCopy == null )
					throw new InvalidOperationException( "The newspaper is starting up, but the list of resources schedulled for display is invalid." );
				if( !_itemsBackupCopy.Contains( res ) )
					return false; // Foreign resource
				_itemToSelect = res; // Store the selection-wannabe
				return true;
			case NewspaperState.Activated:
				if( !_man.ItemsAvail.Contains( res ) )
					return false; // Foreign resource
				_man.SelectItem( res, NewspaperManager.SelectionCause.Manual ); // Apply
				return true;
			default:
				throw new InvalidOperationException( "Invalid state." );
			}
		}

		/// <summary>
		/// Goes to the next or previous unread item in the current view, if available, or reports that such a navigation cannot be done.
		/// </summary>
		/// <param name="forward"><c>True</c> to go to the next unread item, <c>False</c> for the previous one.
		/// Note that the unread items search wraps around the first/last item, if needed.</param>
		/// <returns>Whether there were unread items to go to, and such a jump was successful.</returns>
		public bool GotoNextUnread( bool forward )
		{
			if( _state != NewspaperState.Activated )
				throw new InvalidOperationException( "The newspaper must be in the Activated state in order to go to the previous or next unread item." );
			// Do the jump, considering the unread items only and favoring the proposed direction
			return GotoNextItem( forward, false, true );
		}

		#endregion

		#region DisplayPane-ish API

		public virtual void GetSelectedText( out string plaintext, out string html )
		{
			if( _state != NewspaperState.Activated ) // Trace only in abnormal conditions
				Trace.WriteLine( String.Format( "NewspaperView has been queried of selected text, state is {0}.", _state ), "[NPV]" );

			switch( _state )
			{
			case NewspaperState.Deactivated:
				throw new InvalidOperationException( "The newspaper has not been initialized yet." );
			case NewspaperState.Activating:
				plaintext = "";
				html = "";
				return;
			case NewspaperState.Activated:
				string[] selection = _browser.TextSelection;
				plaintext = selection[ 0 ];
				html = selection[ 1 ];
				return;
			default:
				throw new InvalidOperationException( "Invalid state." );
			}
		}

		#endregion

		#endregion

		#region Static Operations

		/// <summary>
		/// Substitutes macros, such as color constants, in the texts, such as CSS style sheets.
		/// </summary>
		public static string SubstituteMacros( string source )
		{
			StringWriter sw = new StringWriter();
			int nPos = 0;

			int nStart, nEnd;
			string name;
			while( (nStart = source.IndexOf( "<%=", nPos )) != -1 ) // While there are tag starts left
			{
				if( (nEnd = source.IndexOf( "%>", nStart )) == -1 ) // Look for a tag end
					break; // Unclosed tag, abort

				name = source.Substring( nStart + 3, nEnd - nStart - 3 ).Trim(); // Text inside the tag

				if( name.Length != 0 )
				{
					if( !_hashMacros.ContainsKey( name ) )
						Core.ReportBackgroundException( new ArgumentException( String.Format( "NewspaperViewer.SubstituteMacros: unknown macro tag \"{0}\" in the external text source.", name ) ) );
					else
					{
						sw.Write( source.Substring( nPos, nStart - nPos ) );
						sw.Write( _hashMacros[ name ] );
					}
				}

				nPos = nEnd + 2; // Skip the closing tag
			}

			// Copy the remainder
			sw.Write( source.Substring( nPos ) );

			return sw.ToString();
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the <see cref="NewspaperManager">Newspaper Manager</see> that controls the logic of Newspaper View, and which tells <see cref="NewspaperViewer"/> what and when to render.
		/// </summary>
		/// <remarks>Can be accessed when <see cref="State"/> is <see cref="NewspaperState.Activated"/> only.</remarks>
		public NewspaperManager Manager
		{
			get
			{
				if( _state != NewspaperState.Activated )
					throw new InvalidOperationException( "Newspaper must be in Activated state in order to access the Newspaper Manager." );

				return _man;
			}
		}

		/// <summary>
		/// Gets the current newspaper view state. Most operations are available only in the <see cref="NewspaperState.Activated"/> state.
		/// </summary>
		public NewspaperState State
		{
			get { return _state; }
		}

		/// <summary>
		/// List of the items available for display on the newspaper.
		/// </summary>
		public IResourceList NewspaperResources
		{
			get
			{
				Trace.WriteLine( String.Format( "NewspaperView has been queried of the list of newspaper resources, state is {0}.", _state ), "[NPV]" );
				switch( _state )
				{
				case NewspaperState.Deactivated:
					throw new InvalidOperationException( "The newspaper has not been initialized yet." );
				case NewspaperState.Activating:
					if( _itemsBackupCopy == null )
						throw new InvalidOperationException( "The newspaper is starting up, but the list of resources schedulled for display is invalid." );
					return _itemsBackupCopy;
				case NewspaperState.Activated:
					return _man.ItemsAvail;
				default:
					throw new InvalidOperationException( "Invalid state." );
				}
			}
		}

		#region ResourceBrowser-ish API

		/// <summary>
		/// Returns the list of resources currently selected in the newspaper.
		/// </summary>
		/// <remarks>Calling this member on a non-initialized newspaper causes an exception.</remarks>
		public IResourceList SelectedResources
		{
			get
			{
				if( _state != NewspaperState.Activated ) // Trace only in abnormal conditions
					Trace.WriteLine( String.Format( "NewspaperView has been queried of selection, state is {0}.", _state ), "[NPV]" );

				switch( _state )
				{
				case NewspaperState.Deactivated:
					throw new InvalidOperationException( "The newspaper has not been initialized yet." );
				case NewspaperState.Activating:
					if( _itemsBackupCopy == null )
						throw new InvalidOperationException( "The newspaper is starting up, but the list of resources schedulled for display is invalid." );
					return _itemToSelect != null ? _itemToSelect.ToResourceList() : Core.ResourceStore.EmptyResourceList;
				case NewspaperState.Activated:
					return _man.SelectedItem != null ? _man.SelectedItem.ToResourceList() : Core.ResourceStore.EmptyResourceList;
				default:
					throw new InvalidOperationException( "Invalid state." );
				}
			}
		}

		#endregion

		/// <summary>
		/// Gets or sets which sides of the newspaper have an Omea border.
		/// </summary>
		public AnchorStyles Borders
		{
			get { return _borders; }
			set
			{
				// Check if there are illegal flags
				if( (value & (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom)) != value )
					throw new ArgumentException( "The only valid values are Left, Top, Right, and Bottom." );
				if( _borders != value )
				{
					_borders = value;
					PerformLayout(); // Reapply the new borders
				}
			}
		}

		/// <summary>
		/// Gets or sets the desired location of the newspaper filtering/navigation bar.
		/// Available values are <see cref="DockStyle.Top"/> and <see cref="DockStyle.Bottom"/>.
		/// </summary>
		public DockStyle BarLocation
		{
			get { return _bar.Dock; }
			set
			{
				if( !((value == DockStyle.Top) || (value == DockStyle.Bottom)) )
					throw new ArgumentException( "The location can be either top or bottom. Other values are prohibited." );

				// Apply the new value
				if( _bar.Dock != value )
				{
					_bar.Dock = value;
					PerformLayout();
				}
			}
		}

		/// <summary>
		/// Gets the number of items in the current newspaper view.
		/// This property is valid in any of the NewspaperViewer States (<see cref="State"/>), but in any state but <see cref="NewspaperState.Activated"/> it would return a zero value.
		/// </summary>
		public int ItemsInViewCount
		{
			get
			{
				if( (_state != NewspaperState.Activated) || (_man == null) || (!_man.IsInitialized) )
				{
					Trace.WriteLine( "Newspaper has been requested for the number of items in view, but the Newspaper Manager is not ready for providing the count. Zero value was returned.", "[NPV]" );
					return 0;
				}

				return _man.ItemsInView.Count;
			}
		}

		/// <summary>
		/// Gets or sets an object that provides a context for the newspaper.
		/// <c>Null</c> value means that the context provider is not specified and the newspaper should construct the context on its own.
		/// </summary>
		public IContextProvider ContextProvider
		{
			get { return _contextprovider; }
			set { _contextprovider = value; }
		}

		#endregion

		#region Events

		#region Event Helper Functions

		/// <summary>
		/// Safely fires the specific event, that is, traps and reports all the exceptions arising in its processing.
		/// In most cases, there's no use of unwinding the stack up to the pump if one of event handlers has failed.
		/// </summary>
		/// <param name="evt">The event to be raisen.</param>
		/// <param name="args">Argument object for the event (EventArgs and so on).</param>
		protected void SafeFireEvent( Delegate evt, object args )
		{
			try
			{
				if( evt != null )
					evt.DynamicInvoke( new object[] {this, args} );
			}
			catch( Exception ex )
			{
				Core.ReportException( ex, ExceptionReportFlags.AttachLog );
			}

		}

		/// <summary>
		/// Safely fires the specific event, that is, traps and reports all the exceptions arising in its processing.
		/// In most cases, there's no use of unwinding the stack up to the pump if one of event handlers has failed.
		/// <see cref="EventArgs.Empty"/> is supplied as event arguments, for this overload.
		/// </summary>
		/// <param name="evt">The event to be raisen.</param>
		protected void SafeFireEvent( Delegate evt )
		{
			SafeFireEvent( evt, EventArgs.Empty );
		}

		/// <summary>
		/// Delegate for the <see cref="SafeFireEvent(Delegate, object)"/> function.
		/// </summary>
		protected delegate void SafeFireEventDelegate( Delegate evt, object args );

		/// <summary>
		/// Fires the specific event asynchronously.
		/// </summary>
		/// <param name="evt">The event to be fired.</param>
		protected void SafeFireEventAsync( Delegate evt )
		{
			SafeFireEventAsync( evt, EventArgs.Empty );
		}

		/// <summary>
		/// Fires the specific event asynchronously.
		/// </summary>
		/// <param name="evt">The event to be fired.</param>
		protected void SafeFireEventAsync( Delegate evt, object args )
		{
			Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddMilliseconds( 100 ), "Fire the an event asynchronously.", new SafeFireEventDelegate( SafeFireEvent ), evt, args );
		}

		#endregion

		#region NavigateAway Event

		/// <summary>
		/// The user is about to navigate away from the newspaper by following a web link.
		/// Allows a sinker to do something in response to the navigation request.
		/// Typically, that means shutting down the newspaper and navigating to the URL provided.
		/// </summary>
		public event NavigateAwayEventHandler NavigateAway;

		/// <summary>
		/// Handler type for the <see cref="NavigateAway"/> event.
		/// </summary>
		public delegate void NavigateAwayEventHandler( object sender, NavigateAwayEventArgs args );

		/// <summary>
		/// Arguments for the <see cref="NavigateAway"/> event.
		/// </summary>
		public class NavigateAwayEventArgs
		{
			private readonly string _uri;

			public NavigateAwayEventArgs( string uri )
			{
				_uri = uri;

			}

			/// <summary>
			/// Target URI of the navigation.
			/// </summary>
			public string Uri
			{
				get { return _uri; }
			}
		}

		/// <summary>
		/// Fires the <see cref="NavigateAway"/> event.
		/// </summary>
		public void FireNavigateAway( object sender, NavigateAwayEventArgs args )
		{
			Trace.WriteLine( String.Format( "NewspaperView has detected a request for inplace navigation and fires the NavigateAway event.", _state ), "[NPV]" );
			if( NavigateAway != null )
				NavigateAway( this, args );
		}

		#endregion

		/// <summary>
		/// Fires when the <see cref="ItemsInViewCount"/> property value changes.
		/// </summary>
		public event EventHandler ItemsInViewCountChanged;

		/// <summary>
		/// Fires when the <see cref="SelectedResources"/> property value changes.
		/// </summary>
		public event EventHandler SelectedResourcesChanged;

		/// <summary>
		/// Fires when user hits a key that should cause the focus to jump out of Newspaper and be set to some parent control or list.
		/// </summary>
		public event EventHandler JumpOut;

		#endregion

		#region Implementation

		#region Init/Deinit

		/// <summary>
		/// Loads the newspaper viewer constants from the options.
		/// These constants apply to all newspaper views (for all resource type sets).
		/// </summary>
		protected void InitializeConstants()
		{
			string sKey = _man.GetSettingsKey( true ); // The options key that's not bound to a particular resource types set

			// Set up the constants
			c_nMinSpaceAfterItemWhenJumping = Core.SettingStore.ReadInt( sKey, "MinSpaceAfterItemWhenJumping", c_nMinSpaceAfterItemWhenJumping );

			c_fMinRelationOfSpaceAfterToSpaceBefore
				= (double)Core.SettingStore.ReadInt( sKey, "MinRelationOfSpaceAfterToSpaceBefore,Numerator", 1 )
					/ (double)Core.SettingStore.ReadInt( sKey, "MinRelationOfSpaceAfterToSpaceBefore,Denomenator", 3 );

			c_fDesiredRelationOfSpaceAfterToSpaceBefore
				= (double)Core.SettingStore.ReadInt( sKey, "DesiredRelationOfSpaceAfterToSpaceBefore,Numerator", 3 )
					/ (double)Core.SettingStore.ReadInt( sKey, "DesiredRelationOfSpaceAfterToSpaceBefore,Denomenator", 1 );

			c_nScrollFactor = Core.SettingStore.ReadInt( sKey, "ScrollFactor", c_nScrollFactor );

			c_nMinScrollStep = Core.SettingStore.ReadInt( sKey, "MinScrollStep", c_nMinScrollStep );

			c_bShowItemNumbers = Core.SettingStore.ReadBool( sKey, "ShowItemNumbers", c_bShowItemNumbers );

			c_bAllowSmoothScrolling = Core.SettingStore.ReadBool( sKey, "AllowSmoothScrolling", c_bAllowSmoothScrolling );

			c_bAllowHoverSelection = Core.SettingStore.ReadBool( sKey, "AllowHoverSelection", c_bAllowHoverSelection );

			c_nHoverSelectionTimeout = Core.SettingStore.ReadInt( sKey, "HoverSelectionTimeout", c_nHoverSelectionTimeout );
			c_nScrollTimerInterval = Core.SettingStore.ReadInt( sKey, "ScrollTimerInterval", c_nScrollTimerInterval );
		}

		/// <summary>
		/// A function that switches the newspaper from <see cref="NewspaperState.Activating"/> to <see cref="NewspaperState.Activated"/>.
		/// This happens when browser finishes loading the newspaper template.
		/// </summary>
		protected void ActivateNewspaper( object sender, EventArgs e )
		{
			// Check the state
			switch( _state )
			{
			case NewspaperState.Activated:
				throw new InvalidOperationException( "Cannot complete the newspaper activation because it has already been activated." );
			case NewspaperState.Activating: // The one and only valid state for this function
				break;
			case NewspaperState.Deactivated:
				throw new InvalidOperationException( "Cannot complete the newspaper activation because it has been deactivated." );
			default:

				throw new InvalidOperationException( String.Format( "Cannot complete the newspaper activation because the newspaper state {0} is unexpected.", _state ) );
			}

			// Perform the activation
			try
			{
				// Check if the document loaded into the browser is actually our document
				if( _browser == null )
					throw new NullReferenceException( "Cannot complete the newspaper activation because the browser control reference is not set to an instance of the object." );
				if( /*(_browser.ReadyState != BrowserReadyState.Complete) ||*/ (_browser.ManagedHtmlDocument == null) || (_browser.ManagedHtmlDocument.Body == null) )
				{
					Trace.WriteLine( "NewspaperView could not complete the activation this time, waiting for the next event (the Document or Body object is Null).", "[NPV]" ); // Stay in the Activating state
					return; // Wait for the loaded event …
				}
				object oNewspaperId = _browser.ManagedHtmlDocument.Body.GetAttribute( "NewspaperId", GetAttributeFlags.None );
				if( (!(oNewspaperId is string)) || ((string)oNewspaperId != _sNewspaperID) )
				{
					Trace.WriteLine( "NewspaperView could not complete the activation this time, waiting for the next event (the NewspaperId on the Body is wrong).", "[NPV]" ); // Stay in the Activating state
					return; // That's the dummy browser's document that has been loaded, not our newspaper template, so wait for our's
				}

				// Initialize the dirty items storage
				_itemsDirty = new HashSet();

				// Detach from the one-time event that caused this callback
				_browser.remove_DownloadComplete( new EventHandler( ActivateNewspaper ) );

				// Start listening to the newspaper manager events
				_man.Deinitializing += new EventHandler( OnManDeinitializing );
				_man.ItemAdded += new NewspaperManager.ItemAddedEventHandler( OnManItemAdded );
				_man.ItemChanged += new NewspaperManager.ItemChangedEventHandler( OnManItemChanged );
				_man.ItemRemoved += new ResourceEventHandler( OnManItemRemoved );
				_man.SelectedItemChanged += new ResourceEventHandler( OnManSelectedItemChanged );
				_man.PagingChanged += new EventHandler( OnManPagingChanged );
				_man.EnsureVisible += new NewspaperManager.EnsureVisibleEventHandler( OnManEnsureVisible );
				_man.ItemDeselected += new ResourceEventHandler( OnManItemSelectedOrDeselected );
				_man.ItemSelected += new ResourceEventHandler( OnManItemSelectedOrDeselected );
				_man.EnterPage += new EventHandler( OnManEnterPage );
				_man.ItemsInViewChanged += new EventHandler( OnManItemsInViewChanged );

				// Initialize the newspaper manager
				_man.Initialize( _itemsBackupCopy );
				_itemsBackupCopy = null;

				// Apply deferred selection
				if( (_itemToSelect != null) && (_man.ItemsAvail.Contains( _itemToSelect )) ) // First, the force-selection (with changing of the view to All, if necessary)
				{
					_man.SelectItem( _itemToSelect, NewspaperManager.SelectionCause.Manual );
					_itemToSelect = null;
				}
				else if( (_displayoptions.SelectedResource != null) && (_man.ItemsAvail.Contains( _displayoptions.SelectedResource )) ) // Second, the leftover selection (attempts to restore the MRU selection, no switching of the view)
					_man.SelectItem( _displayoptions.SelectedResource, NewspaperManager.SelectionCause.Approx );

				// Yes!
				_state = NewspaperState.Activated;
				Trace.WriteLine( String.Format( "NewspaperView has finished initializing, became visible and loaded the template. Entering {0} state.", _state ), "[NPV]" );

				_statuswriter.ClearStatus();

				// Explicitly set focus to the Web browser control
				_browser.Focus();
			}
			catch( Exception ex )
			{
				Core.ReportException( new Exception( "The newspaper could not be activated.", ex ), ExceptionReportFlags.AttachLog );

				if( _man.IsUninitialized )
				{
					_state = NewspaperState.Deactivated; // May happen if some early-stage error happens
					Trace.WriteLine( String.Format( "NewspaperView has failed to complete initialization. Entering {0} state.", _state ), "[NPV]" );
				}
			}
			finally
			{
				PerformLayout(); // Layout the controls, fill with buttons, etc
				SafeFireEventAsync( ItemsInViewCountChanged ); // Update the counters
				SafeFireEventAsync( SelectedResourcesChanged ); // Update the selected item information
			}
		}

		/// <summary>
		/// Visual Init.
		/// </summary>
		private void InitializeComponentSelf()
		{
			if( _man == null )
				throw new InvalidOperationException( "Newspaper manager must be created before the newspaper bar." );
			SuspendLayout();
			// 
			// _bar
			// 
			_bar = new NewspaperBar( _man );
			_bar.Name = "_bar";
			_bar.TabIndex = 2;
			_bar.Dock = Core.SettingStore.ReadBool( _man.GetSettingsKey( true ), "BarAtBottom", (_bar.Dock == DockStyle.Bottom) ) ? DockStyle.Bottom : DockStyle.Top;
			// 
			// NewspaperViewer
			// 
			Controls.Add( _bar );
			TabStop = true;
			Name = "NewspaperViewer";
			ResumeLayout( false );

			SetStyle( ControlStyles.AllPaintingInWmPaint
				| ControlStyles.CacheText
				| ControlStyles.ContainerControl
				| ControlStyles.Opaque
				| ControlStyles.UserPaint,
			          true );
			SetStyle( ControlStyles.StandardClick
				| ControlStyles.ResizeRedraw
				| ControlStyles.StandardDoubleClick
				| ControlStyles.Selectable,
			          false );
			UpdateStyles();
		}

		/// <summary>
		/// Generates a Web page that servers as an empty Newspaper template.
		/// The page is then loaded into the browser, and, as this process finishes, can be populated with items.
		/// </summary>
		protected void LoadNewspaperTemplate( IResourceList items )
		{
			_statuswriter.ShowStatus( "Loading the newspaper template …" );

			// Start producing the body
			StringWriter html = new StringWriter();
			html.WriteLine( "<html>" );

			/////////////////////////////
			// Head with styles, newspaper-wide items, and newspaper title
			html.WriteLine( "<head>" );

			// Add common styles
			html.WriteLine( "<style type=\"text/css\">" );
			html.WriteLine( Utils.StreamToString( Assembly.GetExecutingAssembly().GetManifestResourceStream( "OmniaMea.ResourceBrowser.Newspaper.CommonStyles.css" ) ) );
			html.WriteLine( "</style>" );

			// Add custom styles
			html.WriteLine( "<style type=\"text/css\">" );
			string[] allTypes = items.GetAllTypes();
			foreach( string resType in allTypes )
			{
				INewspaperProvider provider = Core.PluginLoader.GetNewspaperProvider( resType );
				if( provider != null )
				{
					try
					{
						provider.GetHeaderStyles( resType, html );
					}
					catch( NotImplementedException )
					{
					}
					catch( Exception ex ) // Do not ruin the whole newspaper because of one provider
					{
						Core.ReportException( ex, false );
					}
				}
			}

			html.WriteLine( "</style>" );
			html.WriteLine( "<title>{0}</title>", (_displayoptions.Caption != null ? HttpUtility.HtmlEncode( _displayoptions.Caption ) : "Untitled") );
			html.WriteLine( "</head>" );

			//////////////////////////////
			// Body section
			html.WriteLine( "<body NewspaperId=\"{0}\">", _sNewspaperID );

			// Here the items will go

			// Add the no-items-banner
			html.WriteLine( "<p id=\"NoItemsBanner\" class=\"NoItemsBanner\" style=\"display: none;\">There are no items to show in this view.</p>" );

			html.WriteLine( "</body>" );

			html.WriteLine( "</html>" );

			// Prepare the content
			string sContent = html.ToString();
			sContent = SubstituteMacros( sContent );

			// Display this HTML
			WebSecurityContext ctx = WebSecurityContext.Restricted;
			ctx.AllowInPlaceNavigation = true;
			ctx.WorkOffline = false;
			_browser.ShowHtml( sContent, ctx );

			// Return from the function and wait for the page to load
		}

		#endregion

		#region Event Handlers — Internal Events

		#region DHTML Events

		/// <summary>
		/// A HTML element of the newspaper item has been double-clicked.
		/// This is a handler for a DHTML event.
		/// </summary>
		protected void OnHtmlItemDoubleClick( object sender, HtmlEventArgs args )
		{
			IResource item = ItemFromHtmlId( ((IHtmlDomElement)sender).Id, false );
			if( item != null )
				Core.ActionManager.ExecuteDoubleClickAction( item );
		}

		/// <summary>
		/// A HTML element of the newspaper item has been clicked.
		/// This is a handler for a DHTML event.
		/// </summary>
		protected void OnHtmlItemClick( object sender, HtmlEventArgs args )
		{
			// Select the clicked item
			_man.SelectItem( ItemFromHtmlId( ((IHtmlDomElement)sender).Id, false ), NewspaperManager.SelectionCause.MouseClick );
		}

		/// <summary>
		/// A HTML element of the newspaper item has been hovered with mouse.
		/// This is a handler for a DHTML event.
		/// </summary>
		protected void OnNewsItemHover( IResource item )
		{
			// Select the hovered item
			_man.SelectItem( item, NewspaperManager.SelectionCause.MouseHover );
		}

		/// <summary>
		/// Mouse pointer has entered the newspaper item bounds.
		/// Start looking for a hover.
		/// </summary>
		protected void OnHtmlItemMouseEnter( object sender, HtmlEventArgs args )
		{
			Core.UserInterfaceAP.CancelTimedJobs( new MethodInvoker( OnHoverSelectionElapsed ) );

			if( c_bAllowHoverSelection )
			{
				// Remember the entered item
				_itemHovered = ItemFromHtmlId( ((IHtmlDomElement)sender).Id, false );

				// Start waiting for the hover to happen in c_nMinSpaceAfterItemWhenJumping ms
				Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddMilliseconds( c_nHoverSelectionTimeout ), "Select the hovered item", new MethodInvoker( OnHoverSelectionElapsed ) );
			}
		}

		/// <summary>
		/// Mouse pointer has entered the newspaper item bounds.
		/// Goodbye, hover.
		/// </summary>
		protected void OnHtmlItemMouseLeave( object sender, HtmlEventArgs args )
		{
			Core.UserInterfaceAP.CancelTimedJobs( new MethodInvoker( OnHoverSelectionElapsed ) );
		}

		/// <summary>
		/// Fires before the item's context menu is displayed.
		/// Assigns selection to the item so that the current resource to which a context menu applies would belong to that item.
		/// </summary>
		protected void OnHtmlItemBeforeContextMenu( object sender, HtmlEventArgs args )
		{
			IResource item = ItemFromHtmlId( ((IHtmlDomElement)sender).Id, false );
			if( item != null )
				_man.SelectItem( item, NewspaperManager.SelectionCause.MouseHover );
		}
		
		/// <summary>
		/// An HTML item has just been resized.
		/// Check if the scroll-pos should be adjusted to maintain the visibility.
		/// </summary>
		protected void OnHtmlItemResize( object sender, HtmlEventArgs args )
		{
			if((State != NewspaperState.Activated) || (_man.SelectedItem == null))
				return;

			// Locate the resource & html item that's being resized
			IHtmlDomElement	htmlResized = (IHtmlDomElement)sender;
			IResource itemResized = ItemFromHtmlId(htmlResized.Id, false);
			if(itemResized == null)
				return;

			// Apply compensative scrolling
			ScrollToCompensateResize(htmlResized, itemResized, htmlResized.OffsetHeight - (int)htmlResized.GetAttribute("OldHeight"));
			
			// Record the new item size to have it as the old value when it changes again
			htmlResized.SetAttribute("OldHeight", htmlResized.OffsetHeight, false);
		}

		#region Drag'n'Drop Events & Their Helpers

		/// <summary>
		/// <see cref="IHtmlDomElement.DragEnter"/> event handler.
		/// </summary>
		private void OnHtmlDragEnter( object sender, HtmlEventArgs args )
		{
			try
			{
				// Determine drop effect
				/*DragDropEffects effect = */
				InvokeDragDropHandler( false, sender, args );

				/*
				// Convert and apply the drop effect
				string sEffect = "none";
				switch( effect )
				{
				case DragDropEffects.Copy:
					sEffect = "copy";
					break;
				case DragDropEffects.Link:
					sEffect = "link";
					break;
				case DragDropEffects.Move:
					sEffect = "move";
					break;
				}
				//IHtmlDomObject dataTransfer = new HtmlDomObject( args.GetProperty( "dataTransfer" ) );
				//dataTransfer.SetProperty( "dropEffect", effect );
				//dataTransfer.SetProperty( "dropEffect", "all" );
				//new HtmlDomObject( args.GetProperty( "dataTransfer" ) ).SetProperty( "dropEffect", "link" );
				Trace.WriteLine( "Exec results: " + _browser.Exec( "window.event.dataTransfer != null ? 'yes' : 'no'" ).ToString(), "[NPV]" );
				*/

				// Set up the returned arguments
				args.ReturnValue = false;
				args.CancelBubble = true;

				// TODO: add some visual cues here
			}
			catch( InvalidOperationException ex ) // These are fatal, let them go
			{
				throw ex;
			}
			catch( Exception ex ) // TODO: let them go?
			{
				Trace.WriteLine( "An exception has occured in the DragEnter handler. " + ex.Message, "[NPV]" );
			}
		}

		/// <summary>
		/// Extracts the OLE <c>IDataObject</c> interface from the HTML event's arguments,
		/// and converts it into a .NET <see cref="IDataObject"/> interface.
		/// </summary>
		/// <param name="args">HTML Event Arguments.</param>
		/// <returns>Data object interface that is .NET-compatible.</returns>
		public static IDataObject DataObjectFromHtmlEventArgs( HtmlEventArgs args )
		{
			IServiceProvider sp = args.GetProperty( "dataTransfer" ) as IServiceProvider;

			object oOleDataObject; // OLE's IDataObject interface-capable object
			Guid IID_IDataObject = new Guid( "0000010e-0000-0000-C000-000000000046" );
			sp.QueryService( ref IID_IDataObject, ref IID_IDataObject, out oOleDataObject ); // Query-service for it

			// This detects that the object being passed in is really an OLE IDataObject interface and it gets wrapped around with a .NET object
			return new DataObject( oOleDataObject );
		}

		/// <summary>
		/// Extracts the keyboard keys and mouse buttons state from the <see cref="HtmlEventArgs"/> and converts into the form applicable for the <see cref="DragEventArgs.KeyState"/> property value.
		/// </summary>
		public static int KeyStateFromHtmlEventArgs( HtmlEventArgs args )
		{
			int value = 0;

			// Keyboard keys
			value += args.ShiftKey ? (int)Win32Declarations.MK_SHIFT : 0;
			value += args.AltKey ? (int)Win32Declarations.MK_ALT : 0;
			value += args.CtrlKey ? (int)Win32Declarations.MK_CONTROL : 0;

			// Mouse buttons
			value += (args.Button & MouseButtons.Left) != 0 ? (int)Win32Declarations.MK_LBUTTON : 0;
			value += (args.Button & MouseButtons.Right) != 0 ? (int)Win32Declarations.MK_RBUTTON : 0;
			value += (args.Button & MouseButtons.Middle) != 0 ? (int)Win32Declarations.MK_MBUTTON : 0;

			return value;
		}

		/// <summary>
		/// <see cref="IHtmlDomElement.DragLeave"/> event handler.
		/// </summary>
		private void OnHtmlDragLeave( object sender, HtmlEventArgs args )
		{
			try
			{
				args.CancelBubble = true;

				// TODO: remove the visual cues here
			}
			catch( Exception ex )
			{
				Trace.WriteLine( "An exception has occured in the DragLeave handler. " + ex.Message, "[NPV]" );
			}
		}

		/// <summary>
		/// <see cref="IHtmlDomElement.DragOver"/> event handler.
		/// </summary>
		private void OnHtmlDragOver( object sender, HtmlEventArgs args )
		{
			try
			{
				args.ReturnValue = false;
				args.CancelBubble = true;
			}
			catch( Exception ex )
			{
				Trace.WriteLine( "An exception has occured in the DragOver handler. " + ex.Message, "[NPV]" );
			}
		}

		/// <summary>
		/// <see cref="IHtmlDomElement.Drop"/> event handler.
		/// </summary>
		private void OnHtmlDrop( object sender, HtmlEventArgs args )
		{
			try
			{
				// Apply the drop
				InvokeDragDropHandler( true, sender, args );

				// TODO: remove the visual cues here
			}
			catch( InvalidOperationException ex ) // Fatal; let it go
			{
				throw ex;
			}
			catch( Exception ex ) // TODO: remove and let all go?
			{
				Trace.WriteLine( "An exception has occured in the Drop handler. " + ex.Message, "[NPV]" );
			}
		}

		/// <summary>
		/// Extracts the needed information form <see cref="HtmlEventArgs"/> and invokes either <see cref="IUIManager.ProcessDragOver"/> or <see cref="IUIManager.ProcessDragDrop"/>, depending on the <paramref name="bDrop"/> param value.
		/// </summary>
		/// <param name="bDrop"><c>True</c> to call <see cref="IUIManager.ProcessDragDrop"/>, <c>False</c> for <see cref="IUIManager.ProcessDragOver"/>.</param>
		/// <param name="args">HTML event arguments.</param>
		/// <returns>The resulting drop effect if <paramref name="bDrop"/> is <c>False</c>, or <see cref="DragDropEffects.None"/> if it's <c>True</c>.</returns>
		protected DragDropEffects InvokeDragDropHandler( bool bDrop, object sender, HtmlEventArgs args )
		{
			// Get the drop-target item
			if( !(sender is IHtmlDomElement) )
				throw new InvalidOperationException( "The source of an HTML drag-drop event is not a valid HTML DOM element." );
			IResource itemTarget = ItemFromHtmlId( ((IHtmlDomElement)sender).Id, false );
			if( (itemTarget == null) || (!_man.ItemsOnPage.Contains( itemTarget )) ) // Hovered a non-existent item
				throw new InvalidOperationException( "Cannot bind the target HTML element to a newspaper item." );
			Trace.WriteLine( String.Format( "{0} for {1}.", (bDrop ? "Drop" : "DragOver"), itemTarget.DisplayName ), "[NPV]" );

			// Determine the allowed effects
			DragDropEffects effectAllowed = DragDropEffects.All;
			/*
			IHtmlDomObject dataTransfer = new HtmlDomObject( args.GetProperty( "dataTransfer" ) );
			object sEffectAllowed = dataTransfer.GetProperty( "effectAllowed" ); // TODO: DBNull?
			lock( _hashStringToDropEffect )
			{
				if( (sEffectAllowed != null) && (_hashStringToDropEffect.ContainsKey( sEffectAllowed )) )
					effectAllowed = (DragDropEffects) _hashStringToDropEffect[ sEffectAllowed ];
			}
			*/

			DragDropEffects value = DragDropEffects.None; // Default ret val

			// Invoke either function
			if( bDrop ) // Drop event
				Core.UIManager.ProcessDragDrop( itemTarget, DataObjectFromHtmlEventArgs( args ), effectAllowed, KeyStateFromHtmlEventArgs( args ) );
			else // DragOver event
				value = Core.UIManager.ProcessDragOver( itemTarget, DataObjectFromHtmlEventArgs( args ), effectAllowed, KeyStateFromHtmlEventArgs( args ), false );

			return value;
		}

		/// <summary>
		/// Starts dragging the resource.
		/// Note that event fires on some sub-element of newspaper item HTML element.
		/// </summary>
		protected void OnElementDragStart( object sender, HtmlEventArgs args )
		{
			// Get the drop-target item
			if( !(sender is IHtmlDomElement) )
				throw new InvalidOperationException( "The source of an HTML drag-drop event is not a valid HTML DOM element." );
			IResource itemTarget = GetParentNewspaperHtmlItem( (IHtmlDomElement)sender );
			if( (itemTarget == null) || (!_man.ItemsOnPage.Contains( itemTarget )) ) // Applies to a non-existent item
				throw new InvalidOperationException( "Cannot bind the target HTML element to a newspaper item." );
			Trace.WriteLine( String.Format( "DragStart for {0}.", itemTarget.DisplayName ), "[NPV]" );

			// Set the drag-data
			IDataObject dataobject = DataObjectFromHtmlEventArgs( args );
			dataobject.SetData( typeof(IResourceList), itemTarget.ToResourceList() );

			/*
			object oDataToSave = itemTarget.ToResourceList();

			FORMATETC fmt = new FORMATETC();
			fmt.cfFormat = (short) DataFormats.GetFormat( oDataToSave.GetType().FullName ).Id;
			fmt.dwAspect = 1;
			fmt.lindex = -1;
			fmt.dummy = 0;
			fmt.ptd = (IntPtr) 0;
			fmt.tymed = Helper32.TYMED_HGLOBAL;

			STGMEDIUM stgmed = new STGMEDIUM();
			Helper32.SaveDataToHandle( ref stgmed, oDataToSave );

			DataObject data = new DataObject();
			IntPtr	handle = (IntPtr)0;
			object[]	parameters = new object[]{(IntPtr)0, itemTarget.ToResourceList()};

			
			int	nResult = (int) data.GetType().InvokeMember( "SaveObjectToHandle", BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, data, parameters);*/

			args.CancelBubble = true;
		}

		/// <summary>
		/// A draggable element has been clicked, select the element to assist with dragging.
		/// Note that event fires on some sub-element of newspaper item HTML element.
		/// </summary>
		protected void OnElementMouseDown( object sender, HtmlEventArgs args )
		{
			MshtmlElement htmlSender = (MshtmlElement)sender;
			object oTxtRange = _browser.ManagedHtmlDocument.Body.InvokeMethod( "createTextRange" );
			new HtmlDomObject( oTxtRange ).InvokeMethod( "moveToElementText", htmlSender.Instance );
			new HtmlDomObject( oTxtRange ).InvokeMethod( "select" );
		}

		/// <summary>
		/// Given a child HTML element, searches up the elements hierarchy to find an HTML element that represents a newspaper item on the page, and returns the item that corresponds to it.
		/// </summary>
		/// <param name="htmlChild">A HTML element to start from, may be the newspaper element itself.</param>
		/// <returns>The newspaper item, as a resource, or <c>Null</c> if there was no parent newspaper html item element or it's been deleted/</returns>
		protected IResource GetParentNewspaperHtmlItem( IHtmlDomElement htmlChild )
		{
			if( htmlChild == null )
				throw new ArgumentNullException();

			IHtmlDomElement htmlCur = htmlChild;
			IResource itemRet;

			do
			{
				// Check the current element
				if( (htmlCur.Id != null) && ((itemRet = ItemFromHtmlId( htmlCur.Id, true )) != null) ) // Try to see if it reps an item
					return itemRet;

				// Go up the hierarchy
				htmlCur = htmlCur.ParentElement;
			} while( htmlCur != null );

			return null; // Failed to locate
		}

		#endregion

		#endregion

		#region Newspaper Manager Events

		/// <summary>
		/// The newspaper manager is being deinitialized.
		/// </summary>
		protected void OnManDeinitializing( object sender, EventArgs e )
		{
			StopEvents();
		}

		/// <summary>
		/// An item should be added to the newspaper.
		/// </summary>
		protected void OnManItemAdded( object sender, NewspaperManager.ItemAddedEventArgs args )
		{
			Trace.WriteLine( String.Format( "NewspaperView is adding an item \"{0}\" #{1} to the view.", args.NewItem.DisplayName, args.NewItem.OriginalId ), "[NPV]" );

			if( HtmlElementFromItem( args.NewItem ) != null )
				throw new InvalidOperationException( String.Format( "Trying to insert an already-present item \"{0}\" #{1}.", args.NewItem.DisplayName, args.NewItem.OriginalId ) );

			// Create the new item
			IHtmlDomElement htmlNewItem;
			if( args.InsertBeforeItem != null ) // There's an item to insert before
			{
				// Get the reference item's element
				IHtmlDomElement htmlBefore = HtmlElementFromItem( args.InsertBeforeItem );
				if( htmlBefore == null )
					throw new InvalidOperationException( String.Format( "Trying to insert before an absent item \"{0}\" #{1}.", args.InsertBeforeItem.DisplayName, args.InsertBeforeItem.OriginalId ) );

				// Insert before it
				NewspaperHtmlElement.InsertBefore( htmlNewItem = CreateNewspaperItem( args.NewItem ), htmlBefore );
			}
			else // Place at end
				NewspaperHtmlElement.AppendChild( htmlNewItem = CreateNewspaperItem( args.NewItem ) );
			
			// Compensative scrolling for the new item size
			ScrollToCompensateResize(htmlNewItem, args.NewItem, htmlNewItem.OffsetHeight);

			// Fill the contents in
			UpdateNewspaperItemAsync( args.NewItem );
		}

		/// <summary>
		/// An item should be updated due to a selection change or item properties change.
		/// </summary>
		protected void OnManItemChanged( object sender, NewspaperManager.ItemChangedEventArgs args )
		{
			Trace.WriteLine( String.Format( "NewspaperView is repainting item \"{0}\" #{1}.", args.Item.DisplayName, args.Item.OriginalId ), "[NPV]" );

			// Check if the only change is in the item's Unread state (for example, marked by timer)
			// If so, do a non-deferred, immediate "lite update"
			int[] arChangedProps = args.Changes.GetChangedProperties();
			if( (arChangedProps.Length == 1) && (arChangedProps[ 0 ] == Core.Props.IsUnread) )
				UpdateNewspaperItem( args.Item, true ); // Unread state update: update the styles only
			else
				UpdateNewspaperItemAsync( args.Item ); // Full item update
		}

		/// <summary>
		/// An item should be removed from the newspaper.
		/// </summary>
		protected void OnManItemRemoved( object sender, ResourceEventArgs e )
		{
			Trace.WriteLine( String.Format( "NewspaperView is removing item \"{0}\" #{1} from view.", e.Resource.DisplayName, e.Resource.OriginalId ), "[NPV]" );

			IHtmlDomElement htmlItem = HtmlElementFromItem( e.Resource );
			if( htmlItem == null )
				throw new InvalidOperationException( String.Format( "Trying to remove a non-existent item \"{0}\" #{1}.", e.Resource.DisplayName, e.Resource.OriginalId ) );

			// Adjust scrolling to compensate for the resized item
			ScrollToCompensateResize(htmlItem, e.Resource, -htmlItem.OffsetHeight);

			// Actually remove from the view
			htmlItem.RemoveNode( true );
		}

		/// <summary>
		/// Another item has been selected.
		/// Abort hover.
		/// </summary>
		protected void OnManSelectedItemChanged( object sender, ResourceEventArgs e )
		{
			Core.UserInterfaceAP.CancelTimedJobs( new MethodInvoker( OnHoverSelectionElapsed ) ); // This also applies in case of page switch, BTW
			SafeFireEventAsync( SelectedResourcesChanged );
		}

		/// <summary>
		/// Something has changed about the pages, eg another page has been selected.
		/// Abort scrolling.
		/// </summary>
		protected void OnManPagingChanged( object sender, EventArgs e )
		{
			_nScrollTargetPos = -1;
			Trace.WriteLine( "Scrolling has been aborted due to a page switch.", "[NPV]" );
		}

		/// <summary>
		/// An item must be brought into view.
		/// </summary>
		protected void OnManEnsureVisible( object sender, NewspaperManager.EnsureVisibleEventArgs args )
		{
			// Queue a job for ensuring that this item is visible, for there might be multiple subsequent requests for different items
			Core.UserInterfaceAP.QueueJob( new EnsureVisibleJob( this, new EnsureVisibleJob.EnsureVisibleDelegate( EnsureVisible ), args.Item, args.Cause, true ) );
		}

		/// <summary>
		/// An item's selected state has changed.
		/// </summary>
		protected void OnManItemSelectedOrDeselected( object sender, ResourceEventArgs e )
		{
			UpdateNewspaperItem( e.Resource, true );
		}

		/// <summary>
		/// A new page is entered.
		/// </summary>
		protected void OnManEnterPage( object sender, EventArgs e )
		{
			// Stop scrolling when a page is switched
			_nScrollPrevPos = -1;

			// Queue updating of the no-items-banner visibility
			Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddMilliseconds( 100 ), "Update the No Items Banner visibility.", new MethodInvoker( UpdateNoItemsBannerVisibility ) );
		}

		protected void OnManItemsInViewChanged( object sender, EventArgs e )
		{
			// Delay the notification: the newspaper manager should have a chance to finish its tasks, while the handler could do something bad, like pumping the messages
			SafeFireEventAsync( ItemsInViewCountChanged );
		}

		#endregion

		/// <summary>
		/// Is invoked when a key is pressed in Web browser.
		/// </summary>
		protected void OnBrowserKeyDown( object sender, KeyEventArgs e )
		{
			if( (_state != NewspaperState.Activated) || (!_man.IsInitialized) )
				return; // A premature event

			switch( e.KeyData )
			{
			case Keys.Space: // On Space, jump to the next item
				e.Handled = true;
				if( !GotoNextItem( true, true, true ) )
					Core.ResourceBrowser.GotoNextUnread();
				break;
			case Keys.Space | Keys.Control: // Ctrl+Space: scroll the current item into view
				e.Handled = true;
				_man.SelectItem(_man.SelectedItem, NewspaperManager.SelectionCause.MouseClick);
				break;
			case Keys.Space | Keys.Shift: // On Shift+Space, jump to the prev item
				e.Handled = true;
				GotoNextItem( false, true, true ); // Do nothing even if cannot go further
				break;
			case Keys.Home: // On Home, goto the first item
				e.Handled = _man.GotoEnd( false, false );
				break;
			case Keys.Home | Keys.Control:
				e.Handled = _man.GotoEnd( false, true );
				break;
			case Keys.End: // On End, goto the last item
				e.Handled = _man.GotoEnd( true, false );
				break;
			case Keys.End | Keys.Control:
				e.Handled = _man.GotoEnd( true, true );
				break;
			case Keys.PageDown | Keys.Control: // Ctrl+PgDn: goto next page
				e.Handled = _man.GotoNextPage( true );
				break;
			case Keys.PageUp | Keys.Control: // Ctrl+PgUp: goto prev page
				e.Handled = _man.GotoNextPage( false );
				break;
			case Keys.Escape: // Esc: raise an event that jumps to the resource tree
				e.Handled = true;
				SafeFireEventAsync( JumpOut );
				break;
			case Keys.Up | Keys.Control: // Ctrl+Up: goto previous item (both read/unread, visual manner)
				e.Handled = GotoNextItem( false, true, false );
				break;
			case Keys.Down | Keys.Control: // Ctrl+Down: goto next item (both read/unread, visual manner)
				e.Handled = GotoNextItem( true, true, false );
				break;
			case Keys.Left | Keys.Control: // Ctrl+Left: goto previoous item (both read/unread, non-visual manner)
				e.Handled = GotoNextItem( false, false, false );
				break;
			case Keys.Right | Keys.Control: // Ctrl+Right: goto next item (both read/unread, non-visual manner)
				e.Handled = GotoNextItem( true, false, false );
				break;
			case Keys.Enter: // Enter: executes the double-click action
				if( _man.SelectedItem != null )
					Core.ActionManager.ExecuteDoubleClickAction( _man.SelectedItem );
				break;
			}
		}

		/// <summary>
		/// The scrolling timer has ticked, advance the scrolling position towards the desired one.
		/// </summary>
		private void OnScrollTimerTick( object sender, EventArgs e )
		{
			// Should stop?
			if( _nScrollTargetPos == -1 )
			{
				_timerScroll.Stop();
				return;
			}

			if( _state != NewspaperState.Activated )
				throw new InvalidOperationException( "Trying to scroll a non-activated newspaper." );

			// Retrieve the current scrolling position
			int nCurPos = NewspaperHtmlElement.ScrollTop;

			// Some scrolling not caused by our actions has occured
			if( nCurPos - _nScrollPrevPos != 0 )
			{
				// Check if this means that we have to abort scrolling
				if( _bScrollDir != (nCurPos > _nScrollPrevPos) ) // Scrolling direction has changed
				{ // Shutdown
					_nScrollTargetPos = -1; // Will stop timer
					Trace.WriteLine( "Scrolling aborted because the scrolling direction was violated.", "[NPV]" );
					return;
				}
			}

			// Done? Or, has to be done in one step?
			if( (Math.Abs( _nScrollTargetPos - nCurPos ) < c_nMinScrollStep) || (!_bScrollSmoothly) )
			{ // Done, shutdown
				nCurPos = _nScrollTargetPos;
				_nScrollTargetPos = -1; // Will stop timer
				Trace.WriteLine( String.Format( "Scrolling has been forcefully completed because {0}.", (_bScrollSmoothly ? "the scrolling distance is below the threshold" : "smooth scrolling is off") ), "[NPV]" );
			}
			else // Advance the scroll pos
				nCurPos = (nCurPos + _nScrollTargetPos) / c_nScrollFactor;

			// Apply the new scroll pos and remember it
			NewspaperHtmlElement.ScrollTop = _nScrollPrevPos = nCurPos;

			// Some dummy check in case the desired scroll pos is now outside the scroll range (eg after the window resize)
			if( NewspaperHtmlElement.ScrollTop != nCurPos ) // Shutdown
			{
				_nScrollTargetPos = -1; // Will stop timer
				Trace.WriteLine( "Scrolling has been aborted because the Web browser has failed to scroll to the desired point and seems like this point is now off the canvas.", "[NPV]" );
			}
		}

		/// <summary>
		/// The browser is about to navigate to some URL.
		/// </summary>
		protected void OnBeforeNavigate( object sender, BeforeNavigateEventArgs args )
		{
			// Do not restrict out-of-this-browser-window navigations
			if( !args.Inplace )
				return;

			args.Cancel = true; // In any case, do not navigate in-place

			// Allow a handler to  do something in response to the navigation request.
			// Typically, that means shutting down the newspaper and navigating to the URL provided.
			// Schedulle execution so that newspaper shutdown won't be called from inside the newspaper callbacks
			Core.UserInterfaceAP.QueueJob( "Navigate to " + args.Uri, new NavigateAwayEventHandler( FireNavigateAway ), new object[] {this, new NavigateAwayEventArgs( args.Uri )} );
		}

		#endregion

		#region IResource Item <-> HTML Item Mapping

		/// <summary>
		/// Produces an ID of the HTML element that reproduces the item specified on the web page.
		/// </summary>
		protected string HtmlIdFromItem( IResource res )
		{
			return _sNewspaperID + '-' + res.OriginalId.ToString( "X" );
		}

		/// <summary>
		/// Looks up the HTML element that corresponds to the newspaper item passed into the function.
		/// If such an element can not be found, returns <c>Null</c>.
		/// </summary>
		protected IHtmlDomElement HtmlElementFromItem( IResource res )
		{
			return _browser.ManagedHtmlDocument.GetElementById( HtmlIdFromItem( res ) );
		}

		/// <summary>
		/// Looks up the item represented by the given ID
		/// </summary>
		/// <param name="sID">HTML representation of the item's ID.</param>
		/// <param name="safe">
		/// <para>If <c>False</c>, throws an exception when trying to get an item that does not belong to this newspaper, 
		///		eg a random element that is not a newspaper HTML item.</para>
		///	<para>If <c>True</c>, silently returns <c>Null</c> for invalid IDs, unless the <paramref name="sID"/> is <c>Null</c>,
		///		in this case an <see cref="ArgumentNullException"/> is thrown.</para>
		/// </param>
		/// <returns>An <see cref="IResource"/> corresponding to the given item, or <c>Null</c> if none available 
		///		(for example, the resource has already been deleted).</returns>
		/// <remarks>
		/// <para>If the ID points to a valid resource, but that resource does not belong to the current newspaper, 
		///		an exception is thrown. Note that this does not mean that the resource must be visible or fall into the current filter.</para>
		/// </remarks>
		protected IResource ItemFromHtmlId( string sID, bool safe )
		{
			if( sID == null )
				throw new ArgumentNullException();

			// Check the newspaper prefix
			if( !sID.StartsWith( _sNewspaperID + '-' ) )
				throw new Exception( "The newspaper item specified does not belong to this newspaper." );

			// Try to get the numeric ID value
			int nID;
			try
			{
				nID = int.Parse( sID.Substring( _sNewspaperID.Length + 1 ), NumberStyles.HexNumber );
			}
			catch( Exception ex )
			{
				throw new Exception( "The newspaper item ID has an invalid format.", ex );
			}

			return Core.ResourceStore.TryLoadResource( nID );
		}

		#endregion

		#region HTML Newspaper Items Management

		/// <summary>
		/// Returns the CSS class for an item that should be set for the item's HTML element
		/// </summary>
		protected string GetItemClassName( IResource item )
		{
			// Add prefixes appropriate to the item class
			string sClassName = "NewspaperItem";

			if( item.IsDeleted ) // Only one style for the deleted items
				sClassName += "-Deleted";
			else
			{ // Determine the style for the normal (living) items
				if( item.HasProp( Core.Props.IsUnread ) )
					sClassName += "-Unread";

				if( _man.IsItemSelected( item ) )
					sClassName += "-Selected";
			}

			return sClassName;
		}

		/// <summary>
		/// Creates an HTML element for the newspaper item and returns it.
		/// The item is not added to the newspaper automatically.
		/// The item is not schedulled for update.
		/// </summary>
		protected IHtmlDomElement CreateNewspaperItem( IResource item )
		{
			// Create the item element
			IHtmlDomElement htmlItem = _browser.ManagedHtmlDocument.CreateElement( "div" );
			htmlItem.Id = HtmlIdFromItem( item );
			htmlItem.ClassName = GetItemClassName( item );
			htmlItem.InnerHtml = String.Format( "<p class=\"NoItemsBanner\">{0} - Loading …</p>", item.DisplayName );
			htmlItem.SetAttribute("OldHeight", htmlItem.OffsetHeight, false);	// Remember the height … Resize event doesn't know the old one, so it'll give the diff

			return htmlItem;
		}

		/// <summary>
		/// Updates the item specified in the newspaper.
		/// An HTML element for this item must exist already.
		/// </summary>
		/// <param name="item">Item resource.</param>
		/// <param name="bStylesOnly">Update the selected/unread state indication only, and not the item contents. If the only item properties affected are the selected state or unread state (ones that are changed most frequently), it's not necessary</param>
		protected void UpdateNewspaperItem( IResource item, bool bStylesOnly )
		{
			if( item == null )
				throw new ArgumentNullException();

			// Remember visibility of the selected item
			// Note: listening to the Resize event, seems like not needed
			//bool bSelectionVisible = (_man.SelectedItem != null) && ((IsItemVisible( _man.SelectedItem ) & ItemVisible.VisibleMask) != 0);

			// Get the HTML element of the item
			string sHtmlId = HtmlIdFromItem( item );
			IHtmlDomElement htmlItem = _browser.ManagedHtmlDocument.GetElementById( sHtmlId );

			// The item must be already present in the newspaper, added by CreateItem
			if( htmlItem == null )
				throw new InvalidOperationException( String.Format( "Trying to update a non-existent item \"{0}\" #{1}.", item.DisplayName, item.OriginalId ) );

			// Set/update the item style, be it deleted or not
			htmlItem.ClassName = GetItemClassName( item );

			// Do not perform further updates for deleted items
			if( item.IsDeleted )
			{
				Trace.WriteLine( "Warning: item #{0} which is being updated has already been deleted, skipping the update code." );
				htmlItem.InnerHtml = "<em>The resource has been deleted.</em>"; // Remove any possible item content
				return;
			}

			// Wire up the events (only if the item is newly-created)
			if( htmlItem.GetAttribute( "HasEvents", GetAttributeFlags.CaseSensitive ) == null )
			{
				htmlItem.DoubleClick += new HtmlEventHandler( OnHtmlItemDoubleClick );
				htmlItem.ContextMenu += new HtmlEventHandler( OnHtmlItemBeforeContextMenu );
				htmlItem.MouseEnter += new HtmlEventHandler( OnHtmlItemMouseEnter );
				htmlItem.MouseLeave += new HtmlEventHandler( OnHtmlItemMouseLeave );
				htmlItem.DragEnter += new HtmlEventHandler( OnHtmlDragEnter );
				htmlItem.DragLeave += new HtmlEventHandler( OnHtmlDragLeave );
				htmlItem.DragOver += new HtmlEventHandler(OnHtmlDragOver);
				htmlItem.Resize += new HtmlEventHandler(OnHtmlItemResize);
				htmlItem.Click += new HtmlEventHandler(OnHtmlItemClick);
				htmlItem.Drop += new HtmlEventHandler( OnHtmlDrop );
				htmlItem.SetAttribute( "HasEvents", true, true );
			}

			// Set the item internals
			if( !bStylesOnly )
			{ // Update the whole item

				/////
				// Generate new content

				// Retrieve the newspaper provdier
				INewspaperProvider provider = Core.PluginLoader.GetNewspaperProvider( item.Type );
				provider = provider != null ? provider : new GenericNewspaperProvider();

				// Item content stream
				StringWriter sw = new StringWriter();

				// Add some optional debug info
				if( c_bShowItemNumbers )
					sw.WriteLine( "<p ResourceDragSource=\"1\">Item #{4} {0}/{1} (on page {2}/{3})</p>", _man.ItemsInView.IndexOf( item ), _man.ItemsInView.Count, _man.ItemsOnPage.IndexOf( item ), _man.ItemsPerPage, item.OriginalId );

				// Item's Body Content (including the header and footers)
				try
				{
					provider.GetItemHtml( item, sw );
				}
				catch( NotImplementedException )
				{
					sw.WriteLine( "<em>Body not available.</em>" );
				}
				catch( Exception ex ) // Do not ruin the whole newspaper because of one provider
				{
					Core.ReportException( ex, false );
				}

				////
				// Check if it has changed; update only if yes
				string sNewContent = sw.ToString();

				if( htmlItem.InnerHtml != sNewContent )
					htmlItem.InnerHtml = sNewContent;

				//////////////////////////////////////////////////////
				// Attach handlers to the elements, if that's needed
				foreach( IHtmlDomElement child in htmlItem.ChildNodes ) // TODO: recurse to deeper children
				{
					try
					{
						if( child.GetAttribute( "ResourceDragSource" ) != null )
						{
							child.DragStart += new HtmlEventHandler( OnElementDragStart );
							child.MouseDown += new HtmlEventHandler( OnElementMouseDown );
						}
					}
					catch( Exception )
					{
					}
				}
			}
			else
			{ // This is a selection/unread update, also update the icon to indicate the current unread state
				foreach( IHtmlDomElement image in htmlItem.GetElementsByTagName( "img" ) )
				{
					if( image.ClassName == "ResourceIcon" )
						image.SetAttribute( "src", GenericNewspaperProvider.GetIconFileName( item ), false );
				}
			}

			// Restore visibility of the selected item, if needed
			// Note: listening to the Resize event, seems like not needed
			//if( (bSelectionVisible) && (_man.SelectedItem != null) && ((IsItemVisible( _man.SelectedItem ) & ItemVisible.InvisibleMask) != 0) )
			//	EnsureVisible( item, NewspaperManager.SelectionCause.PageSwitch, false ); // Scroll in Instant mode and don't defer scrolling, otherwise, it won't take effect as some other items may update and hide this item from the view
		}

		/// <summary>
		/// Initiates updating the item by marking it as dirty.
		/// This function always considers the item to be updated completely, including the content. For small updates (such as selection or read/unread), use the sync version.
		/// </summary>
		protected void UpdateNewspaperItemAsync( IResource item )
		{
			_itemsDirty.Add( item );
			Core.UserInterfaceAP.QueueJob( "Update the Newspaper Items Asynchronously.", new MethodInvoker( UpdateNewspaperItemsDeferred ) );
		}

		/// <summary>
		/// Executes as a result of <see cref="UpdateNewspaperItemAsync"/>. Updates the items on the dirty list.
		/// </summary>
		protected void UpdateNewspaperItemsDeferred()
		{
			Application.DoEvents(); // Allow the paint requests to proceed

			if( _state != NewspaperState.Activated )
				return; // The newspaper has been shut down

			try
			{
				uint start = Win32Declarations.GetTickCount();
				uint limit = 200;

				// Go on updating items for some time (but always allow one item to load)
				for( int a = 0; ((Win32Declarations.GetTickCount() - start < limit) || (a == 0)) && (_state == NewspaperState.Activated); a++ )
				{
					IEnumerator enumItems = _itemsDirty.GetEnumerator();
					if( !enumItems.MoveNext() )
						break; // Items are thru

					// This is the item to update (the first in the list)
					IResource item = (IResource)((HashSet.Entry)enumItems.Current).Key;
					_itemsDirty.Remove( item );
					if( item.IsDeleted )
						continue;

					// Make sure that the item has not gone off newspaper while we were waiting for it to update async
					if( !_man.ItemsOnPage.Contains( item ) )
						continue;

					// Do the full update of the item
					UpdateNewspaperItem( item, false );
				}

				// Requeue
				if( _itemsDirty.Count != 0 )
					Core.UserInterfaceAP.QueueJob( "Update the Newspaper Items Asynchronously.", new MethodInvoker( UpdateNewspaperItemsDeferred ) );
			}
			finally
			{
				// Update the status text to indicate the number of remaining items to be updated
				if( _statuswriter != null )
				{
					if( (_state == NewspaperState.Activated) && (_itemsDirty != null) && (_itemsDirty.Count != 0) && (_man.IsInitialized) )
					{
						int nTotal = _man.ItemsOnPage.Count;
						int nLeft = _itemsDirty.Count;
						if( nLeft <= nTotal ) // A valid percentage can be displayed
							_statuswriter.ShowStatus( String.Format( "Loading newspaper items ({0}%)", (nTotal - nLeft) * 100 / nTotal ) );
						else
							_statuswriter.ShowStatus( "Loading newspaper items…" );
					}
					else // Not running, or state info not available
						_statuswriter.ClearStatus();
				}
			}
		}

		#endregion

		#region Item's Visibility Control

		protected ItemVisible IsItemVisible( IResource item )
		{
			if( item == null )
				throw new ArgumentNullException();

			IHtmlDomElement htmlItem = HtmlElementFromItem( item );
			IHtmlDomElement htmlBody = NewspaperHtmlElement;

			if( htmlItem == null )
				throw new InvalidOperationException( String.Format( "Trying to make a visibility check for an item#{0} \"{1}\" that does not exist in the newspaper view.", item.OriginalId, item.DisplayName ) );

			if( htmlItem.OffsetTop + htmlItem.OffsetHeight < htmlBody.ScrollTop )
				return ItemVisible.InvisibleAboveView;

			if( htmlBody.ScrollTop + htmlBody.ClientHeight < htmlItem.OffsetTop )
				return ItemVisible.InvisibleBelowView;

			if( htmlItem.OffsetTop < htmlBody.ScrollTop )
			{
				if( htmlBody.ScrollTop + htmlBody.ClientHeight < htmlItem.OffsetTop + htmlItem.OffsetHeight )
					return ItemVisible.MiddlePartVisible;

				return ItemVisible.LowerPartVisible;
			}

			if( htmlBody.ScrollTop + htmlBody.ClientHeight < htmlItem.OffsetTop + htmlItem.OffsetHeight )
				return ItemVisible.UpperPartVisible;

			return ItemVisible.CompletelyVisible;
		}

		/// <summary>
		/// Possible item visibility values.
		/// </summary>
		protected enum ItemVisible
		{
			/// <summary>
			/// The item completely fits in view (vertically) and is currently visible entirely.
			/// </summary>
			CompletelyVisible = 0x01,

			/// <summary>
			/// The upper part of the item is visible.
			/// Note that this statement makes no imposition on whether the item fits in view vertically or not.
			/// </summary>
			UpperPartVisible = 0x02,

			/// <summary>
			/// The lower part of the item is visible.
			/// Note that this statement makes no imposition on whether the item fits in view vertically or not.
			/// </summary>
			LowerPartVisible = 0x04,

			/// <summary>
			/// Neither top nor bottom of the item is visible, but some middle part of the item is.
			/// This obviously means that the item does not fit in view.
			/// </summary>
			MiddlePartVisible = 0x08,

			/// <summary>
			/// No part of the item is currently visible, and newspaper should be scrolled up to bring the item into view.
			/// </summary>
			InvisibleAboveView = 0x10,

			/// <summary>
			/// No part of the item is currently visible, and newspaper should be scrolled down to bring the item into view.
			/// </summary>
			InvisibleBelowView = 0x20,

			/// <summary>
			/// A mask to check whether the item is somehow visible.
			/// </summary>
			VisibleMask = 0x0F,

			/// <summary>
			/// A mask to check whether the item is somehow invisible.
			/// </summary>
			InvisibleMask = 0x30
		}

		/// <summary>
		/// Scrolls the newspaper view so that the item specified is visible.
		/// If the item does not fit to view, it's guaranteed that its top will be visible.
		/// </summary>
		/// <param name="item">Newspaper item.</param>
		/// <param name="cause">The cause for scrolling the item into view. Affects what part of the item is revealed and how the scrolling occurs, smoothly or instantly.</param>
		/// <param name="bAllowScrollMerge">If this parameters is set to <c>True</c>, allows queing the scrolling requests so that the subsequent requests (even for instant scrolling) could be merged.</param>
		protected void EnsureVisible( IResource item, NewspaperManager.SelectionCause cause, bool bAllowScrollMerge )
		{
			IHtmlDomElement htmlItem = HtmlElementFromItem( item );
			if( htmlItem == null )
				return; // This may be a valid situation because now the EnsureVisible jobs are deferred and the item may be already gone
			IHtmlDomElement htmlBody = NewspaperHtmlElement;
			bool bItemFitsInView = htmlItem.OffsetHeight < htmlBody.ClientHeight; // Whether the item is small enough to be displayed in the view completely, or not

			ItemVisible visibility = IsItemVisible( item );
			switch( cause )
			{
			case NewspaperManager.SelectionCause.MouseHover:
				if( (visibility == ItemVisible.InvisibleAboveView) || (visibility == ItemVisible.InvisibleBelowView) )
					goto case NewspaperManager.SelectionCause.MouseClick; // Just a dummy check: if the item appears to be invisible, bring it into view
				break;
			case NewspaperManager.SelectionCause.MouseClick:
				if( (visibility == ItemVisible.InvisibleAboveView) || (visibility == ItemVisible.InvisibleBelowView) )
					goto case NewspaperManager.SelectionCause.Manual; // Cause it appear in view (lol)
				if( (visibility == ItemVisible.CompletelyVisible) || (visibility == ItemVisible.MiddlePartVisible) )
					break; // Nothing to do

				// Check if we can scroll item so that it would fit in view and have min-space after it
				// If bItemFitsInViewWithSpace implies on bItemFitsInView
				bool bItemFitsInViewWithSpace = htmlItem.OffsetHeight + c_nMinSpaceAfterItemWhenJumping < htmlBody.ClientHeight;

				if( visibility == ItemVisible.UpperPartVisible )
				{
					// Upper part of the item was visible, it fits even with space — align item at the bottom of the view and leave some spacing
					if( bItemFitsInViewWithSpace )
						ScrollNewspaper( htmlItem.OffsetTop - (htmlBody.ClientHeight - htmlItem.OffsetHeight) + c_nMinSpaceAfterItemWhenJumping, true, bAllowScrollMerge );
						// Upper part of the item was visible, it fits — align item at the bottom of the view
					else if( bItemFitsInView )
						ScrollNewspaper( htmlItem.OffsetTop - (htmlBody.ClientHeight - htmlItem.OffsetHeight), true, bAllowScrollMerge );
						// Upper part of the item was visible, it does not fit into view — align item at the top (its upper part)
					else
						ScrollNewspaper( htmlItem.OffsetTop, true, bAllowScrollMerge );
				}
				else if( visibility == ItemVisible.LowerPartVisible )
				{
					// Lower part of the item was visible, it fits even with space — align item at the top of the view and leave some spacing
					if( bItemFitsInViewWithSpace )
						ScrollNewspaper( htmlItem.OffsetTop - c_nMinSpaceAfterItemWhenJumping, true, bAllowScrollMerge );
						// Lower part of the item was visible, it fits — align item at the top of the view
					else if( bItemFitsInView )
						ScrollNewspaper( htmlItem.OffsetTop, true, bAllowScrollMerge );
						// Lower part of the item was visible, it does not fit into view — align item at the bottom (its lower part)
					else
						ScrollNewspaper( htmlItem.OffsetTop + (htmlItem.OffsetHeight - htmlBody.ClientHeight), true, bAllowScrollMerge );
				}
				break;
			case NewspaperManager.SelectionCause.Manual:
				bool bSmooth = (cause != NewspaperManager.SelectionCause.PageSwitch); // Scroll smoothly, excluding the case it's a page switch
				// Generally — make the item completely visible, if possible
				if( visibility != ItemVisible.CompletelyVisible ) // Don't touch the item if it's completely visible already
				{
					if( !bItemFitsInView ) // Item cannot be fit into view; MiddlePartVisible falls into this case
						ScrollNewspaper( htmlItem.OffsetTop, bSmooth, bAllowScrollMerge );
					else
					{ // Item fits into view
						if( visibility == ItemVisible.LowerPartVisible ) // Item's bottom is visible, just scroll up a bit to see it
							ScrollNewspaper( htmlItem.OffsetTop, bSmooth, bAllowScrollMerge );
						else if( visibility == ItemVisible.UpperPartVisible ) // Item's top is visible, scroll down a bit to reveal it completely
							ScrollNewspaper( htmlItem.OffsetTop - (htmlBody.ClientHeight - htmlItem.OffsetHeight), bSmooth, bAllowScrollMerge );
						else // Item was not visible at all, center it vertically in view
							ScrollNewspaper( htmlItem.OffsetTop - ((htmlBody.ClientHeight - htmlItem.OffsetHeight) / 2), bSmooth, bAllowScrollMerge );
					}
				}
				break;
			case NewspaperManager.SelectionCause.GotoNext:
				{
					// Possible choices
					bool bMinSpaceAfter = false; // Scroll so that the spacing after the item fits the minimum requirements
					bool bDesiredRelation = false; // Establish the desired relation between the space after and before

					int nViewHeight = htmlBody.ClientHeight;
					int nFreeSpace = nViewHeight - htmlItem.OffsetHeight;
					int nSpaceBefore = cause == NewspaperManager.SelectionCause.GotoNext ? htmlItem.OffsetTop - htmlBody.ScrollTop : nFreeSpace - (htmlItem.OffsetTop - htmlBody.ScrollTop);
					int nSpaceAfter = nFreeSpace - nSpaceBefore;

					// Determine which case it is
					if( visibility == ItemVisible.CompletelyVisible ) // Item is visible, ensure there's enough space below the item
					{
						// Check if the item should be scrolled: space below it is too small absolutely or as related to the space above
						if( (nSpaceAfter < c_nMinSpaceAfterItemWhenJumping)
							|| ((double)nSpaceAfter / (double)nSpaceBefore < c_fMinRelationOfSpaceAfterToSpaceBefore) )
							bDesiredRelation = true;
					}
						/*else if( (bItemFitsInView) && (visibility == (cause == SelectionCause.GotoNext ? ItemVisible.UpperPartVisible : ItemVisible.LowerPartVisible)) ) // The item should be scrolled a bit to be brought into view
						bMinSpaceAfter = true;	// COMMENT: seems like it's nice to always obtain the best relation
						*/
					else // Treat the item as not visible, align to the desired relation
						bDesiredRelation = true;

					// Calculate spacing according to the selected case
					if( bDesiredRelation )
					{
						// Calculate the spacings according to the desired relation
						nSpaceBefore = (int)(nFreeSpace / (c_fDesiredRelationOfSpaceAfterToSpaceBefore + 1));
						nSpaceAfter = nFreeSpace - nSpaceBefore;

						// Check if the space below gets too small, enlarge it to the minimum value or free space if it's lower
						if( nSpaceAfter < c_nMinSpaceAfterItemWhenJumping )
							bMinSpaceAfter = true;
					}
					if( bMinSpaceAfter )
					{
						nSpaceAfter = nFreeSpace < c_nMinSpaceAfterItemWhenJumping ? nFreeSpace : c_nMinSpaceAfterItemWhenJumping;
						nSpaceBefore = nFreeSpace - nSpaceAfter;
					}

					// Apply it (choose the upper space, either before or after)
					ScrollNewspaper( htmlItem.OffsetTop - (cause == NewspaperManager.SelectionCause.GotoNext ? nSpaceBefore : nSpaceAfter), true, bAllowScrollMerge );

					break;
				}
			case NewspaperManager.SelectionCause.GotoPrevious:
				goto case NewspaperManager.SelectionCause.GotoNext;
			case NewspaperManager.SelectionCause.PageSwitch:
				goto case NewspaperManager.SelectionCause.Manual;
			case NewspaperManager.SelectionCause.Approx:
				goto case NewspaperManager.SelectionCause.Manual;
			}
		}

		#endregion

		/// <summary>
		/// Returns the HTML element which is the parent for newspaper-item HTML elements.
		/// Initially this is the body element, but it's possible for it to change somewhen to some internal element of the body due to layout needs, so you should not use body directly for the items parent.
		/// </summary>
		protected IHtmlDomElement NewspaperHtmlElement
		{
			get
			{
				if( ((_state != NewspaperState.Activated) && (_state != NewspaperState.Activating)) || (!_man.IsInitialized) )
					throw new InvalidOperationException( String.Format( "The newspaper manager must be initialized in order to request the newspaper template body element ({0}-{1}-{2}).", (_state != NewspaperState.Activated), (_state != NewspaperState.Activating), (!_man.IsInitialized) ) );
				if( _browser == null )
					throw new InvalidOperationException( "The newspaper is active, but the browser component is Null." );

				IHtmlDomElement htmlNewspaper = _browser.ManagedHtmlDocument.Body;
				if( htmlNewspaper == null )
					throw new InvalidOperationException( "The newspaper is activated, but its template root can not be accessed." );

				return htmlNewspaper;
			}
		}

		/// <summary>
		/// Jumps either to the next or previous item.
		/// </summary>
		/// <param name="bNext">Whether to go to the next (<c>True</c>) or previous (<c>False</c>) item.</param>
		/// <param name="bVisual">
		/// <para>If <c>True</c>, imposes on Visual mode, eg that goto-next is done in UI by an explicit user action. Ensures that the entire item text can be read while doing a sequence of goto-next, which means that if the item is too large to fit in view, the subsequent goto-next's for it just do a scroll one page down until it reveals the whole item, and only after that jumps to the next one. Also, the item which is being jumped from is marked as read regardless of whether it was viewed for the proper time or not.</para>
		/// <para>If <c>False</c>, no such check is done and goto-next always jumps to the next item in the list. The prev-selected item is not marked as read.</para>
		/// </param>
		/// <param name="bUnread">Consider unread items only when jumping to the next item.</param>
		/// <returns>Whether there was any reaction.</returns>
		protected bool GotoNextItem( bool bNext, bool bVisual, bool bUnread )
		{
			// Check if the item should/could be marked as read and scrolled down to reveal its full contents before jumping to the next one
			if( (_man.SelectedItem != null) && (bVisual) && (HtmlElementFromItem( _man.SelectedItem ) != null) )
			{
				// Check if the current item has not been shown completely yet and we cannot jump to the next one before we scroll it
				ItemVisible visibility = IsItemVisible( _man.SelectedItem );
				if( ((bNext) && (visibility == ItemVisible.UpperPartVisible))
					|| ((!bNext) && (visibility == ItemVisible.LowerPartVisible))
					|| (visibility == ItemVisible.MiddlePartVisible) ) // Only the beginning of the item is visible, or its middle part
				{
					ScrollNewspaper( (int)(NewspaperHtmlElement.ScrollTop + (bNext ? 1 : -1) * NewspaperHtmlElement.ClientHeight * 0.75), true, true );
					return true;
				}
			}

			// Apply goto-next-item
			return _man.GotoNextItem( bNext, bUnread );
		}

		/// <summary>
		/// Initiates scrolling of the newspaper body to the position specified.
		/// </summary>
		/// <param name="nNewPosition">The desired scrolling pos, in pixels.</param>
		/// <param name="bSmooth">Specifies whether the newspaper should be scrolled smoothly or immediately.
		/// In sync mode, the immediate scrolling is applied instantly.</param>
		/// <param name="bAsync">If this parameter is <c>True</c>, even the immediate scrolling request will be deferred to allow multiple scrolling tasks to be merged together.</param>
		protected void ScrollNewspaper( int nNewPosition, bool bSmooth, bool bAsync )
		{
			if( _state != NewspaperState.Activated )
				throw new InvalidOperationException( "Cannot scroll when newspaper is not activated." );

			IHtmlDomElement htmlBody = NewspaperHtmlElement;
			if( htmlBody == null )
				return;

			// Ensure the scroll range is valid
			if( nNewPosition < 0 )
				nNewPosition = 0;
			if( nNewPosition >= htmlBody.ScrollHeight - htmlBody.ClientHeight )
				nNewPosition = htmlBody.ScrollHeight - htmlBody.ClientHeight;

			// Check if it has changed
			if( _nScrollTargetPos == nNewPosition )
				return;

			// Set up the scrolling task
			_nScrollTargetPos = nNewPosition;
			_nScrollPrevPos = htmlBody.ScrollTop;
			_bScrollDir = _nScrollTargetPos > _nScrollPrevPos;
			_bScrollSmoothly = c_bAllowSmoothScrolling && bSmooth; // Scroll smoothly, if needed and allowed
			Trace.WriteLine( String.Format( "Scrolling has been initiated/merged, smooth mode is {0}.", _bScrollSmoothly ), "[NPV]" );

			// If async mode is requested, requeue scrolling in sync mode
			if( bAsync )
				Core.UserInterfaceAP.QueueJob( "Scroll the Newspaper View.", new MethodInvoker( ScrollNewspaperImpl ) );
			else
				ScrollNewspaperImpl();
		}

		/// <summary>
		/// Actually starts the newspaper scrolling, either smoothly or immediately, according to the settings written by the <see cref="ScrollNewspaper"/> function.
		/// </summary>
		protected void ScrollNewspaperImpl()
		{
			if( (_state != NewspaperState.Activated) || (!_man.IsInitialized) ) // Check this because when executing in async mode the newspaper may deinit before the execution occurs
				return;

			// Go on, if the scrolling should be applied immediately, do not use the timer
			if( _bScrollSmoothly )
				_timerScroll.Start();
			else
				OnScrollTimerTick( _timerScroll, EventArgs.Empty );
		}

		/// <summary>
		/// The hover timeout has elapsed, select the hovered item.
		/// </summary>
		protected void OnHoverSelectionElapsed()
		{
			if( _nScrollTargetPos == -1 ) // Apply hover-selection only if newspaper is not being scrolled
			{
				if( (_itemHovered != null) && (!_itemHovered.IsDeleted) && (HtmlElementFromItem( _itemHovered ) != null) ) // If it's a valid item, and if it's still on the current page
					OnNewsItemHover( _itemHovered ); // Process its hovering
				_itemHovered = null;
			}
		}

		/// <summary>
		/// Cancels all the lengthy or deferred operaitons related to the current newspaper page, such as scrolling and hovering, when the page is being discarded.
		/// </summary>
		protected void StopEvents()
		{
			Trace.WriteLine( String.Format( "NewspaperView has freezed the current events." ), "[NPV]" );
			Core.UserInterfaceAP.QueueJob( new MethodInvoker( StopScrollingTimer ) ); // Stop the scrolling timer, deferred (Timer.Stop pumps the messages, we must not do it in init/deinit functions)
			Core.UserInterfaceAP.CancelTimedJobs( new MethodInvoker( OnHoverSelectionElapsed ) ); // Stop the hover timer
		}

		/// <summary>
		/// Stops the scrolling timer.
		/// Deferred-invoked from StopEvents to prevent from pumping the messages in the StopEvents handler, which is done by Timer.Stop.
		/// </summary>
		protected void StopScrollingTimer()
		{
			_timerScroll.Stop();
		}

		/// <summary>
		/// Checks whether the No Items Banner should be visible now and applies this visibility setting.
		/// </summary>
		protected void UpdateNoItemsBannerVisibility()
		{
			if( (_state != NewspaperState.Activated) || (!_man.IsInitialized) )
				return; // Ensure we're on the run

			// Apply the visibility
			try
			{
				bool bVisible = _man.ItemsOnPage.Count == 0;
				IHtmlDomElement banner = _browser.ManagedHtmlDocument.GetElementById( "NoItemsBanner" );
				HtmlDomObject style = HtmlDomObject.AttachBase( banner.GetProperty( "style" ) );
				style.SetProperty( "display", (bVisible ? "block" : "none") );
			}
			catch( NullReferenceException )
			{
				// Report & reschedule
				Trace.WriteLine( "NullRef exception while updating the banner visibility. Will retry a bit later." );
				Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddMilliseconds( 500 ), "Update the Newspaper No Items Banner Visibility.", new MethodInvoker( UpdateNoItemsBannerVisibility ) );
			}
		}

		/// <summary>
		/// Omea Settings have changed. Update the corresponding parameters.
		/// </summary>
		protected void OnSettingsChanged( object sender, EventArgs e )
		{
			// Reload the settings from the setting store
			InitializeConstants();
		}

		/// <summary>
		/// When an item is added, removed, or changes its size (eg when deferred content gets finally loaded), adjusts the scrolling positions to compensate the possible offset of the selected item.
		/// Note that only the selected item visual position is taken into account — because there's no adequate metric for fixating the viewport over any other item.
		/// </summary>
		protected void ScrollToCompensateResize(IHtmlDomElement htmlResized, IResource itemResized, int nChangeInSize)
		{
			if(htmlResized == null)
				throw new ArgumentNullException("htmlResized");
			if(itemResized == null)
				throw new ArgumentNullException("itemResized");
			if(nChangeInSize == 0)
				return;
			
			// The selected item has resized — ensure it will be visible
			if(itemResized == _man.SelectedItem)
			{
				_nScrollTargetPos = 0;	// Cancel any async scroll, as we're gonna start a new one
				_man.SelectItem(_man.SelectedItem, NewspaperManager.SelectionCause.MouseClick);
				return;	// Nothing more to do
			}
			
			// If the resized item was above the selected one, adjust either the deferred or actual scrolling
			if((_man.SelectedItem != null) && (_man.ItemsOnPage.IndexOf(itemResized) < _man.ItemsOnPage.IndexOf(_man.SelectedItem)))
			{
				if(_nScrollTargetPos != -1)	// Now the deferred scrolling should hit the new target
					_nScrollTargetPos += nChangeInSize;
				NewspaperHtmlElement.ScrollTop += nChangeInSize;	// Jump immediately to compensate with the resize
			}
		}

		#region Overrides

		protected override void OnLayout( LayoutEventArgs levent )
		{
			Rectangle rect = ClientRectangle;

			// Layout the borders and define the client area
			_rectClient = Rectangle.FromLTRB(
				rect.Left + (((_borders & AnchorStyles.Left) != 0) ? 1 : 0),
				rect.Top + (((_borders & AnchorStyles.Top) != 0) ? 1 : 0),
				rect.Right + (((_borders & AnchorStyles.Right) != 0) ? -1 : 0),
				rect.Bottom + (((_borders & AnchorStyles.Bottom) != 0) ? -1 : 0)
				);

			Rectangle avail = _rectClient;

			///////
			// Layout the controls

			// Bar
			if( _bar.Dock == DockStyle.Bottom )
			{ // Dock to bottom
				_bar.Bounds = Rectangle.FromLTRB( avail.Left, avail.Bottom - _bar.Height, avail.Right, avail.Bottom );
				avail = Rectangle.FromLTRB( avail.Left, avail.Top, avail.Right, _bar.Top );
			}
			else
			{ // Dock to top
				_bar.Bounds = new Rectangle( avail.Location, new Size( avail.Width, _bar.Height ) );
				avail = Rectangle.FromLTRB( avail.Left, _bar.Bottom, avail.Right, avail.Bottom );
			}

			// Browser
			if( _nest != null )
			{
				_nest.Bounds = avail;
				avail = Rectangle.Empty;
			}
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			// Borders around the newspaper
			Rectangle rect = ClientRectangle;
			using( Brush brush = new SolidBrush( c_colorBorder ) )
			{
				if( (_borders & AnchorStyles.Left) != 0 )
					e.Graphics.FillRectangle( brush, Rectangle.FromLTRB( rect.Left, rect.Top, rect.Left + 1, rect.Bottom ) );
				if( (_borders & AnchorStyles.Top) != 0 )
					e.Graphics.FillRectangle( brush, Rectangle.FromLTRB( rect.Left, rect.Top, rect.Right, rect.Top + 1 ) );
				if( (_borders & AnchorStyles.Right) != 0 )
					e.Graphics.FillRectangle( brush, Rectangle.FromLTRB( rect.Right - 1, rect.Top, rect.Right, rect.Bottom ) );
				if( (_borders & AnchorStyles.Bottom) != 0 )
					e.Graphics.FillRectangle( brush, Rectangle.FromLTRB( rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom ) );
			}

			// Fill the inner area (normally, it should not be visible when the newspaper is initialized)
			e.Graphics.FillRectangle( SystemBrushes.ControlDark, _rectClient );
		}

		#endregion

		#region IContextProvider Members

		public IActionContext GetContext( ActionContextKind kind )
		{
			ActionContext context;

			// Check if the real context is available (if the newspaper is already active)
			if( _state == NewspaperState.Activated )
			{
				// If a context provider is specified, query it for the context
				context = /*_contextprovider != null ? 
					(ActionContext)_contextprovider.GetContext( kind ) 
					: */ new ActionContext( kind, this, null );
				// The selected resource will be set later
			}
			else // The newspaper is not active, provide a dummy context
				context = new ActionContext( kind, this, null );

			// Add self parameters
			context.SetSelectedResources( _man.SelectedItem != null ? _man.SelectedItem.ToResourceList() : null );
			context.SetOwnerForm( ParentForm );
			context.SetCommandProcessor( this );

			return context;
		}

		#endregion

		#region ICommandProcessor Members

		public void ExecuteCommand( string command )
		{
			if( _state != NewspaperState.Activated )
				return; // Newspaper not active

			if( _browser.CanExecuteCommand( command ) )
				_browser.ExecuteCommand( command, null );
		}

		public bool CanExecuteCommand( string command )
		{
			if( _state != NewspaperState.Activated )
				return false; // Newspaper not active

			return _browser.CanExecuteCommand( command );
		}

		#endregion

		#endregion

		#region EnsureVisibleJob Class

		/// <summary>
		/// A job that performs the EnsureVisible operation and allows to merge the EnsureVisible requests regardless of the arguments passed in to the function.
		/// </summary>
		protected class EnsureVisibleJob : AbstractNamedJob
		{
			/// <summary>
			/// Argument.
			/// </summary>
			protected readonly IResource _item;

			/// <summary>
			/// Argument.
			/// </summary>
			protected readonly NewspaperManager.SelectionCause _cause;

			/// <summary>
			/// Argument.
			/// </summary>
			protected readonly bool _bAllowScrollMerge;

			/// <summary>
			/// The owning <see cref="NewspaperViewer"/> class.
			/// It's checked for the state before executing the delegate, if the state is not <see cref="NewspaperState.Activated"/>, then the call is just omitted.
			/// </summary>
			protected readonly NewspaperViewer _newspaper;

			/// <summary>
			/// 
			/// </summary>
			protected readonly EnsureVisibleDelegate _target;

			/// <summary>
			/// Constructs the object.
			/// </summary>
			/// <param name="newspaper">The owning <see cref="NewspaperViewer"/> class.
			/// It's checked for the state before executing the delegate, if the state is not <see cref="NewspaperState.Activated"/>, then the call is just omitted.</param>
			/// <param name="target">The execution target: method to call.</param>
			/// <param name="item">Argument.</param>
			/// <param name="cause">Argument.</param>
			/// <param name="bAllowScrollMerge">Argument.</param>
			public EnsureVisibleJob( NewspaperViewer newspaper, EnsureVisibleDelegate target, IResource item, NewspaperManager.SelectionCause cause, bool bAllowScrollMerge )
			{
				_newspaper = newspaper;
				_target = target;

				_item = item;
				_cause = cause;
				_bAllowScrollMerge = bAllowScrollMerge;

				Trace.WriteLine( String.Format( "Created an EnsureVisible job for #{3} \"{0}\" due to {1}, merging is {2}", item.DisplayName, cause, bAllowScrollMerge, item.OriginalId ), "[NPV]" );
			}

			/// <summary>
			/// Delegate to the <see cref="NewspaperViewer"/>'s <see cref="NewspaperViewer.EnsureVisible"/> function.
			/// </summary>
			public delegate void EnsureVisibleDelegate( IResource item, NewspaperManager.SelectionCause cause, bool bAllowScrollMerge );

			public override int GetHashCode()
			{
				return _newspaper.GetHashCode();
			}

			public override bool Equals( object obj )
			{
				return (obj is EnsureVisibleJob) && (_newspaper == ((EnsureVisibleJob)obj)._newspaper);
			}

			public override string Name
			{
				get { return "Ensure that Newspaper View Item Is Visible."; }
				set { throw new NotImplementedException(); }
			}

			protected override void Execute()
			{
				// Check
				if( _newspaper.State != NewspaperState.Activated )
					return;
				if( !_newspaper.Manager.IsInitialized )
					return;
				if( !_newspaper.Manager.ItemsAvail.Contains( _item ) )
					return;

				Trace.WriteLine( String.Format( "Executing the EnsureVisible job for #{3} \"{0}\" due to {1}, merging is {2}", _item.DisplayName, _cause, _bAllowScrollMerge, _item.OriginalId ), "[NPV]" );

				// Call
				_target( _item, _cause, _bAllowScrollMerge );
			}
		}

		#endregion
	}

	#region Interop Helpers

	[ComImport]
	[Guid( "6D5140C1-7436-11CE-8034-00AA006009FA" )]
	[InterfaceType( (short)1 )]
	public interface IServiceProvider
	{
		[MethodImpl( MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime )]
		void QueryService( [In] ref Guid guidService, [In] ref Guid riid, [MarshalAs( UnmanagedType.IUnknown )] out object ppvObject );
	}

    #endregion
}