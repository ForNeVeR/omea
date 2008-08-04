/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Implements the newspaper filtering and paging logic as well as some other actions not related to UI display.
	/// </summary>
	public class NewspaperManager : IDisposable
	{
		#region Implementation — Data

		/// <summary>
		/// All the Omea views. This list is listened for changes in, and thus new views may appear in the filtering views list.
		/// </summary>
		protected IResourceList _viewsAll = null;

		/// <summary>
		/// Views that should be displayed in the views combobox.
		/// </summary>
		protected IResourceList _viewsFiltering = null;

		/// <summary>
		/// The last-seen valid value for the items-per-page setting.
		/// Needed as the entered text is not valid all the time, but we may be requested of this value at any moment.
		/// </summary>
		protected int _nItemsPerPage = -1;

		/// <summary>
		/// Resource types that might be displayed by the controlled newspaper.
		/// Affects the options set (individual for each set of resources) and the list of the views avail for filtering (only applicable ones).
		/// </summary>
		protected string[] _resourceTypes = null;

		/// <summary>
		/// Number of pages currently available for the newspaper.
		/// </summary>
		protected int _nPagesCount = -1;

		/// <summary>
		/// A zero-based number of the page currently selected in the newspaper.
		/// If there are no pages, it's equal to <c>0</c>.
		/// </summary>
		protected int _nCurrentPage = -1;

		/// <summary>
		/// The live resource list of all the items available for the newspaper for display.
		/// This list comes from outside.
		/// This live version of the list is used only for listening to the events and producing the dead list.
		/// </summary>
		protected IResourceList _itemsAvailLive = null;

		/// <summary>
		/// The dead snapshot of <see cref="_itemsAvailLive"/> that is guaranteed not to change between recalculations of the view.
		/// This list is produced by killing <see cref="_itemsAvailLive"/> periodically.
		/// </summary>
		protected IResourceList _itemsAvail = null;

		/// <summary>
		/// Items remaining after restricting the newspaper to the current view.
		/// This list is calculated by this class by narrowing the <see cref="_itemsAvail"/>.
		/// This is the live version of the list. Used only for producing the dead list <see cref="_itemsInView"/>.
		/// </summary>
		protected IResourceList _itemsInViewLive = null;

		/// <summary>
		/// Items remaining after restricting the newspaper to the current view.
		/// Dead list which is produced from its live form, <see cref="_itemsInViewLive"/>, on each view recalculation, be it refetch- or not.
		/// </summary>
		protected IResourceList _itemsInView = null;

		/// <summary>
		/// Items remaining after restricting the items in view to the current page.
		/// This list is calculated by this class by narrowing the <see cref="_itemsInView"/>.
		/// It is always dead and never live.
		/// </summary>
		protected IResourceList _itemsOnPage = null;

		/// <summary>
		/// The view currently selected for filtering.
		/// <c>Null</c> means no filter.
		/// </summary>
		protected IResource _currentView = null;

		/// <summary>
		/// Means that the object is in process of initialization or deinitialization and no change events should be fired in this period of time.
		/// </summary>
		protected bool _bInitializingOrDeinitializing = false;

		/// <summary>
		/// The item that is currently selected in the newspaper, or <c>Null</c>, if there is no selection.
		/// </summary>
		protected IResource _itemSelected = null;

		/// <summary>
		/// Stores the global (in ItemsAvail) index of the most recently selected item.
		/// When an item gets deleted, this index helps to determine the selection position, as the previously-selected item exists no more in the list.
		/// <c>-1</c> means no selection.
		/// This field is quite secondary, as related to <see cref="_itemSelected"/>, and the latter should be used wherever applicable.
		/// </summary>
		protected int _nSelectedIndex = -1;

		/// <summary>
		/// Determines whether auto-marking as read by timeout is allowed in the newspaper.
		/// Is ANDed with the global Omea settings.
		/// </summary>
		protected bool _bAllowAutoMarkAsRead = true;

		/// <summary>
		/// Determines whether items get marked as read when selection jumps to the next/prev item from them.
		/// This setting does not respect the Omea-global option, which behavior is by design.
		/// </summary>
		protected bool _bMarkAsReadOnGotoNext = true;

		#region Constants

		/// <summary>
		/// A limit for the number of items on page.
		/// The only its goal is to prevent from arithmetic overflow in the further calculations.
		/// </summary>
		public static readonly int c_nMaxItemsOnPage = 0x1000;

		#endregion

		#endregion

		#region Construction

		public NewspaperManager()
		{
		}

		#endregion

		#region Interface — Operations

		/// <summary>
		/// Retrieves the name of the key under which settings for this newspaper type should be stored.
		/// </summary>
		/// <param name="global">If <c>True</c>, then settings for all the newspaper views are considered (style, jumping, etc). If <c>False</c>, then they're applied to the current set of resource types only (number of pages, current view, etc).</param>
		public string GetSettingsKey( bool global )
		{
			Preconditions( global ? 0 : Pre.HasResourceTypes );
			return String.Format( "{0}({1})", GlobalSettingsKey, (global ? "" : string.Join( ",", _resourceTypes )) );
		}

		/// <summary>
		/// Checks whether there's the next page to go to.
		/// </summary>
		public bool CanGotoNextPage( bool bForward )
		{
			return bForward ? CurrentPage < PagesCount - 1 : CurrentPage > 0;
		}

		/// <summary>
		/// Jumps to the next page.
		/// </summary>
		/// <returns>Whether the jump was successful or not.</returns>
		public bool GotoNextPage( bool bForward )
		{
			if( CanGotoNextPage( bForward ) )
			{
				CurrentPage += (bForward ? 1 : 0) * 2 - 1;
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Sets selection to the item specified.
		/// Resets selection from the previously selected item.
		/// </summary>
		/// <param name="itemNew">The resource of the new item to select, or a <c>Null</c> value if the selection should be reset totally.</param>
		/// <param name="cause">Rules out switching of the pages, views, and later — how scrolling is performed to make the selected item come in sight.</param>
		public void SelectItem( IResource itemNew, SelectionCause cause )
		{
			if( (itemNew != null) && (itemNew.IsDeleted) )
			{
			    Trace.WriteLine( String.Format( "Prevented from selecting a deleted item #{0}.", itemNew.OriginalId ), "[NPV]" );
				return;
			}

			Preconditions( Pre.Initialized );
			Trace.WriteLine( String.Format( "Selecting \"{0}\" #{1} due to {2}.", (itemNew != null ? itemNew.DisplayName : "<none>"), (itemNew != null ? (object)itemNew.OriginalId : (object)"<none>"), cause ), "[NPV]" );

			// Do not reapply selection if there was no change
			if( itemNew == _itemSelected )
			{
				// Scroll the item into view if it was clicked or somehow else reactivated (but not by hover)
				if( itemNew != null )
					FireEnsureVisible( new EnsureVisibleEventArgs( itemNew, cause ) );
				return;
			}

			// We're requested to select the item from the current view which is the closest to the given one
			if( cause == SelectionCause.Approx )
			{
				if( itemNew == null )
					throw new ArgumentException( "You must specify the reference item when doing an approximate selection.\nThe item-to-select parameter “itemNew” cannot be “Null” if the “cause” parameter is “SelectionCause.Approx”." );

				// Is it our item?
				if( !ItemsAvail.Contains( itemNew ) )
					throw new InvalidOperationException( "Trying to select an item in the newspaper that does not belong to this newspaper." );

				// If the item requested belongs to the current view, do the ordinary selection
				if( (CurrentFilteringView == null) || (ItemsInView.Contains( itemNew )) )
					SelectItem( itemNew, SelectionCause.Manual );

				// Find the nearest item to the unavailable selection
				IResource itemClosest = FindNearestItem( ItemsAvail.IndexOf( itemNew ) );

				// Apply the choice
				// May be null, which is quite OK — selection will be reset
				SelectItem( itemClosest, SelectionCause.Manual );

				return;
			}

			// Remember the old selection
			IResource itemOld = _itemSelected;
			if( (itemOld != null) && (itemOld.IsDeleted) ) // TODO: is this possible?
				itemOld = null; // Validate
			Core.UserInterfaceAP.CancelTimedJobs( new MethodInvoker( OnMarkAsReadElapsed ) ); // Cancel the marking-as-read "timer"

			// Check whether the page should be switched as a part of the selection process
			bool bPageSwitch = itemNew != null ? (!ItemsOnPage.Contains( itemNew )) : false;

			/////////////////
			// Page-switching scenario
			if( bPageSwitch )
			{
				Debug.Assert( itemNew != null ); // We switch a page only in case it's non-null
				if( cause == SelectionCause.PageSwitch )
					throw new InvalidOperationException( "The item-selecting routine has detected that a page needs to be switched to select the item, however, this selection is already caused by a page switch. Refuse to recurse further." );

				// Ensure the item is valid for this newspaper
				if( !ItemsAvail.Contains( itemNew ) )
					throw new InvalidOperationException( "An attempt was made to select an item in the newspaper that does not belong to the current newspaper." );

				// Ensure this item is present in view
				if( !ItemsInView.Contains( itemNew ) )
					CurrentFilteringView = null; // Select the "All" view

				// Must go to another page?
				if( !ItemsOnPage.Contains( itemNew ) )
					CurrentPage = PageNumberFromItem( itemNew );

				// Now must be on page now! Select the proper item now within the bounds of the page
				SelectItem( itemNew, SelectionCause.PageSwitch ); // The second argument also prevents from an infinite recustion

				return;
			}

			/////////////////
			// Non-page-switching scenario

			////////////
			// Apply the selection!
			_itemSelected = itemNew; // Apply selection beforehand so that styles would be build correctly
			_nSelectedIndex = itemNew != null ? ItemsAvail.IndexOf( itemNew ) : -1;

			// Reset the previous selection, if there were any, and if the page is not gonna switch (otherwise, selection will be retained by the page-switch)
			if( (itemOld != null) && (!bPageSwitch) )
				FireItemDeselected( new ResourceEventArgs( itemOld ) );

			// Set the new selection
			if( itemNew != null )
			{
				FireItemSelected( new ResourceEventArgs( itemNew ) );

				// Scroll into view (if it's hidden partially)
				FireEnsureVisible( new EnsureVisibleEventArgs( itemNew, cause ) );

				// Start couting the mark-as-read time
				if( _bAllowAutoMarkAsRead ) // Allowed by newspaper settings? (ANDed with Omea's global ones)
				{
					int nTimeout = Core.SettingStore.ReadInt( "Resources", "MarkAsReadTimeOut", 2000 );
					if( nTimeout != 0 ) // Allowed by Omea settings?
						Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddMilliseconds( nTimeout ), "Mark resource as read", new MethodInvoker( OnMarkAsReadElapsed ) );
				}
			}

			// Notify of the selection chgange
			SafeFireEvent( SelectedItemChanged, new ResourceEventArgs( itemNew ) );
		}

		/// <summary>
		/// Looks up in the current view the nearest item to an item given by the index.
		/// Note that the referenced item might no longer exist neither in the view nor the complete list, and the same index could well point to quite another item now (this is what exactly happens in case of item deletion). In this case an item with the same index could be returned, or, if it's unsuitable, another one.
		/// The index is allowed to fall beyond the end of the list, in which case the last item in view is returned.
		/// If no item fits, <c>Null</c> is returned.
		/// </summary>
		protected IResource FindNearestItem( int index )
		{
			Preconditions( Pre.Initialized );

			if( index < 0 )
				throw new ArgumentException( "Trying to lookup a nearest item by a negative index." );

			if( ItemsInView.Count == 0 )
				return null; // No items to select, in any case

			if( index >= ItemsAvail.Count ) // Requested beyond the end of list — return the last item in the view (which is guaranteed to exist at this point)
				return ItemsInView[ ItemsInView.Count - 1 ];

			if( ItemsInView.Contains( ItemsAvail[ index ] ) )
				return ItemsAvail[ index ]; // The very desired item fits into view, use it

			////
			// Look for the closest item
			int nLastIndex = ItemsAvail.Count - 1;
			int nStart = index; // Index of the desired item that is not present in the current view
			int nLimit = nStart - 0 >= nLastIndex - nStart ? nStart - 0 : nLastIndex - nStart; // Maximum number of other items around th edesired one, in each direction

			// Try to find the closest suitable items, going in both directions simultaneousely
			IResource itemClosest = null;
			for( int a = 0; a < nLimit; a++ )
			{
				if( (nStart + a <= nLastIndex) && (ItemsInView.Contains( ItemsAvail[ nStart + a ] )) )
				{
					itemClosest = ItemsAvail[ nStart + a ];
					break;
				}
				if( (nStart - a >= 0) && (ItemsInView.Contains( ItemsAvail[ nStart - a ] )) )
				{
					itemClosest = ItemsAvail[ nStart - a ];
					break;
				}
			}
			return itemClosest;
		}

		/// <summary>
		/// Causes the selection to jump to the either end of the current page or the whole newspaper.
		/// </summary>
		/// <param name="bForward">Jump direction: <c>True</c> for the last item, <c>False</c> for the first.</param>
		/// <param name="bGlobal">Whether the scope of jumping is the whole newspaper (<c>True</c>), or the current page only (<c>False</c>).</param>
		/// <returns>Whether the jump was successful (it is not if the target item is already the selected one).</returns>
		public bool GotoEnd( bool bForward, bool bGlobal )
		{
			Preconditions( Pre.Initialized );

			IResource itemTarget = null; // Target of the jump

			// Are there items to jump to?
			if( ItemsInView.Count == 0 )
				return false;
			if( (!bGlobal) && (ItemsOnPage.Count == 0) )
				return false;

			// Choose the new item
			if( bForward )
			{
				itemTarget = bGlobal ? ItemsInView[ ItemsInView.Count - 1 ] :
                                       ItemsOnPage[ ItemsOnPage.Count - 1 ];
			}
			else
			{
				itemTarget = bGlobal ? ItemsInView[ 0 ] : ItemsOnPage[ 0 ];
			}

			// Select item or, if it's already selected, make it visible
			SelectItem( itemTarget, SelectionCause.Manual );

			return true;
		}

		/// <summary>
		/// Selectes the next or previous item.
		/// </summary>
		/// <param name="bForward">Jump direction: <c>True</c> for the next item, <c>False</c> for the previous one.</param>
		/// <param name="bUnread">Indicates whether to jump to the unread items only.
		/// Note that if the next unread item is before the selection in the current view, the proposed jump direction will be ignored.
		/// Also, the current item is marked as read only if this parameter is set to <c>True</c>.</param>
		/// <returns>Whether there was any jump.
		/// Absence of a jump in case of <paramref name="bUnread"/> set to <c>True</c> means that ResourceBrowser should go to the next view
		/// or container that has unread items.</returns>
		public bool GotoNextItem( bool bForward, bool bUnread )
		{
			Preconditions( Pre.Initialized );

			if( ItemsInView.Count == 0 )
				return false; // No space to jump

			IResource itemSelOld = SelectedItem; // Store to check

			// Index of the prev-selected item, or -1 if none/invalid
			int nOldIndex = SelectedItem != null ? ItemsInView.IndexOf( SelectedItem ) : -1;
			int nNewIndex = -1; // -1 is an "invalid value"

			if( bUnread ) // Unread Mode: find the "next" unread item to jump to, wrapping around the zero
			{
				// Mark the prev item as read, if needed
				if( (itemSelOld != null) && (!itemSelOld.IsDeleted) && (_bMarkAsReadOnGotoNext) )
					new ResourceProxy( itemSelOld ).DeletePropAsync( Core.Props.IsUnread );

				int nDirection = bForward ? 1 : -1; // Direction in which to go in search of the item
				for( int a = 1; (a < ItemsInView.Count) && (nNewIndex == -1); a++ ) // Note: do not take the self item, as it may still be unread by this time
				{ // Take the resources in the view one by one, wrapping around zero
					IResource resTry = ItemsInView[ (nOldIndex + a * nDirection + ItemsInView.Count) % ItemsInView.Count ];
					if( resTry.IsDeleted )
						continue;
					if( resTry.HasProp( Core.Props.IsUnread ) ) // The resource is unread!
						nNewIndex = (nOldIndex + a * nDirection + ItemsInView.Count) % ItemsInView.Count; // Choose it to be the next
				}
			}
			else // Normal mode: jump to the "next" item
			{
				// Index of the desired item (maybe off the bounds)
				nNewIndex = nOldIndex != -1 ? (nOldIndex + (bForward ? 1 : -1)) : (bForward ? 0 : ItemsInView.Count - 1);

				// Constrain by the bounds
				nNewIndex = nNewIndex > 0 ? (nNewIndex < ItemsInView.Count - 1 ? nNewIndex : ItemsInView.Count - 1) : 0;
			}

			// Apply (select or just scroll in view)
			if( nNewIndex != -1 )
				SelectItem( ItemsInView[ nNewIndex ], (bForward ? SelectionCause.GotoNext : SelectionCause.GotoPrevious) );

			return SelectedItem != itemSelOld; // Whether selection has changed (eg whether the jump was successful)
		}

		/// <summary>
		/// Tells whether this item is selected or not.
		/// More effective in use than comparison to <see cref="SelectedItem"/>.
		/// </summary>
		public bool IsItemSelected( IResource item )
		{
			return item == _itemSelected;
		}

		/// <summary>
		/// Reapplies the current view or applies a new view after switching to another view.
		/// Opens such a page so that the current selection would be still visible, if available. If not, the closest item is selected.
		/// </summary>
		/// <param name="refetch">
		/// <para>Defines whether to re-fetch the resource list for this view, or not.</para>
		/// <para>If <c>True</c>, the view's list is totally rebuilt; this should be done if we're switching to another view.</para>
		/// <para>If <c>False</c>, the list is not rebuilt; instead, all the possble changes to the view's resource list that happened outside are applied: page numbers recalculated, item list on page rebuilt, etc. The selection is maintained on the currently selected item unless it leaves the list.</para>
		/// </param>
		public void RecalculateView( bool refetch )
		{
			Preconditions( Pre.HasItems | Pre.HasViews );

			Trace.WriteLine( String.Format( "Recalculating the view." ), "[NPV]" );

			// Remember the selection
			IResource itemSelected = SelectedItem;
			int nSelectedIndex = _nSelectedIndex;

			// There are some recalculations that involve live resource lists. They all must be secured by a lock that assures they're populated from the same list
			lock( _itemsAvailLive )
			{
				// Update the dead all-items resource list
				RecalculateItemsAvail();

				// Force the resource list to be re-fetched?
				if( (refetch) || (_itemsInViewLive == null) ) // Rebuild always on the first call, even if not requested explicitly
				{
					// Rebuild the view (both live and non-live versions)
					_itemsInViewLive = (CurrentFilteringView == null) ? _itemsAvailLive :
                                                                        Core.FilterEngine.ExecView( CurrentFilteringView, _itemsAvailLive );
				}

				// Update the dead snapshot
				_itemsInView = KillResourceList( _itemsInViewLive );

				// Notify of the change in the ItemsInView list
				SafeFireEvent( ItemsInViewChanged );

				Trace.WriteLine( String.Format( "ItemsInView recalculated, {0} pcs.", _itemsInView.Count ), "[NPV]" );

				// Force recalculation of the page items
				RecalculatePage();
			}

			// Select some item that is the closest to the previous selection
			if( itemSelected != null )
			{
				if( ItemsAvail.Contains( itemSelected ) ) // The former selection has not gone from the newspaper list
					SelectItem( itemSelected, SelectionCause.Approx );
				else if( _nSelectedIndex != -1 ) // No more prev-selected item, just select some with the same index (or nearest)
				{
					IResource itemClosest = FindNearestItem( nSelectedIndex );
					if( itemClosest != null ) // If there are no items left in the view, then there's no closest item, cannot select it approx
						SelectItem( itemClosest, SelectionCause.Approx );
				}
			}
		}

		/// <summary>
		/// A function that is deferred-invoked to recalculate the view.
		/// Does not force-rebuild the view (passes a <c>False</c> parameter to <see cref="RecalculateView"/>.
		/// </summary>
		protected void RecalculateViewDeferred()
		{
			// Check whether the newspaper has been deinitialized while we were waiting for the deferred execution
			if( !IsInitialized )
				return;

			// Invoke the view recalculation
			RecalculateView( false );
		}

		/// <summary>
		/// Rebuilds the list of items for the current page, updates paging information, and maintains the selection to fall into the current page.
		/// </summary>
		public void RecalculatePage()
		{
			Trace.WriteLine( String.Format( "Recalculating the page." ), "[NPV]" );

			// Save the old items-per-page list for implementing the page switch
			IResourceList itemsOld = null;
			if( IsInitialized ) // Should notify?
				itemsOld = _itemsOnPage;

			////////////
			// Apply!

			////
			// Update paging
			Repaginate(); // Update the paging information

			////
			// Rebuild the items list
			int nItemsPerPage = ItemsPerPage;
			IResourceList source = ItemsInView; // Restrict this view down to the current page

			if( source.Count < nItemsPerPage ) // There are too few items for paging, use the whole filtered list
				_itemsOnPage = KillResourceList( source );
			else
			{ // The set should be narrowed down to the current page
				int nStartIndex = CurrentPage * nItemsPerPage;
				int nEndIndex = nStartIndex + nItemsPerPage;
				nEndIndex = nEndIndex <= source.Count ? nEndIndex : source.Count; // Restrict in case the last page is not filled completely
				Debug.Assert( nStartIndex < nEndIndex, "Trying to filter to a non-existent page." );

				// Collect IDs of the the page items
				int[] ids = new int[nEndIndex - nStartIndex]; // List IDs of resources included in this page
				for( int a = nStartIndex; a < nEndIndex; a++ )
					ids[ a - nStartIndex ] = source[ a ].OriginalId;

				// Build a list out of them
				_itemsOnPage = Core.ResourceStore.ListFromIds( ids, false );
			}
			Trace.WriteLine( String.Format( "ItemsOnPage rebuilt, {0} pcs.", _itemsOnPage.Count ), "[NPV]" );

			//////////////
			// Do the page switch
			if( (IsInitialized) && (itemsOld != null) && (ItemsOnPage != null) ) // Here we also generate the new set
				SwitchPage( itemsOld, ItemsOnPage ); // This function also updates the selection
		}

		/// <summary>
		/// Checks the view against fitting into the newspaper filtering views list.
		/// </summary>
		/// <param name="view">View to be checked.</param>
		/// <param name="hashResourceTypes">A cache for the resource types that improves performance of multiple calles to this function. Pass <c>Null</c> for the first call and the same object for the following ones.</param>
		/// <returns>The diagnosis.</returns>
		public bool IsFilteringView( IResource view, ref HashSet hashResourceTypes )
		{
			Preconditions( Pre.HasResourceTypes );

			// Initialize the cache
			if( hashResourceTypes == null )
				hashResourceTypes = new HashSet( _resourceTypes );

			if( !view.HasProp( "ContentType" ) )
				return true; // Has no restriction, fits thus

			// If the view has content type specified, check if it's capable of handling the newspaper's resources
			string[] viewResourceTypes = view.GetStringProp( "ContentType" ).Split( new char[] {'|'} );
			if( viewResourceTypes.Length == 0 )
				return true; // Has no restriction, let it last

			foreach( string type in viewResourceTypes )
			{
				if( hashResourceTypes.Contains( type ) )
					return true; // Intersects and thus legal in the list
			}

			return false; // A view has resource types specified, but has none in common with our resource list
		}

		/// <summary>
		/// Turns a possibly-live (or snapshot) resource list into a dead resource list that is non-live and does not change spontaneousely.
		/// </summary>
		/// <param name="source">The source resource list, be it true live, one-way live snapshot, or dead.</param>
		/// <returns>A dead resource list with a set of resources equivalent to <paramref name="source"/>.</returns>
		public static IResourceList KillResourceList( IResourceList source )
		{
			lock( source )
			{
				return Core.ResourceStore.ListFromIds( source.ResourceIds, false );
			}
		}

		#endregion

		#region Interface — Attributes

		/// <summary>
		/// Tells whether the object is completely initialized.
		/// </summary>
		public bool IsInitialized
		{
			get { return (InitializedState == 1) && (!_bInitializingOrDeinitializing); }
		}

		/// <summary>
		/// Tells whether the object is completely uninitialized.
		/// </summary>
		public bool IsUninitialized
		{
			get { return (InitializedState == -1) && (!_bInitializingOrDeinitializing); }
		}

		/// <summary>
		/// Gets the list of views available for filtering the newspaper.
		/// </summary>
		/// <remarks>In addition to the views listed here, there's also one fake view represented with a <c>Null</c> value which means "All Items".</remarks>
		public IResourceList FilteringViews
		{
			get
			{
				Preconditions( Pre.HasViews );

				return _viewsFiltering;
			}
		}

		/// <summary>
		/// Gets or sets number of the items to show per page.
		/// </summary>
		public int ItemsPerPage
		{
			get
			{
				Preconditions( Pre.HasItemsPerPage );

				return _nItemsPerPage; // Return the cached value of the combobox
			}
			set
            {
                #region Preconditions
                if ( value <= 0 )
					throw new ArgumentOutOfRangeException( "value", value, "Number of items per page cannot be negative." );

				if( value >= c_nMaxItemsOnPage )
					throw new ArgumentOutOfRangeException( "value", value, String.Format( "Number of items per page must be below {0}.", c_nMaxItemsOnPage ) );
                #endregion Preconditions

				bool bChanged = false; // Determines whether to raise the Changed event

				// Store
				if( _nItemsPerPage != value )
				{
					bChanged = true;
					_nItemsPerPage = value;
					Trace.WriteLine( String.Format( "ItemsPerPage set to {0}.", _nItemsPerPage ), "[NPV]" );
					RecalculateView( false ); // This also maintains the selection, not the current page number
				}

				// Fire the change event
				if( (bChanged) && (ItemsPerPageChanged != null) && (IsInitialized) && (!_bInitializingOrDeinitializing) )
					SafeFireEvent( ItemsPerPageChanged );
			}
		}

		/// <summary>
		/// List of the items available for display on the newspaper.
		/// </summary>
		public IResourceList ItemsAvail
		{
			get
			{
				Preconditions( Pre.HasItems );

				// Lasy instantiation of the dead list from the live list
				if( _itemsAvail == null )
					throw new InvalidOperationException( "The list of items must not be null." );

				return _itemsAvail;
			}
		}

		/// <summary>
		/// Retrieves the list of items filtered to the current view applied to the newspaper.
		/// </summary>
		public IResourceList ItemsInView
		{
			get
			{
				Preconditions( Pre.HasItems | Pre.HasViews );

				if( _itemsInView == null )
					throw new InvalidOperationException( "The list of items in view must not be null." );

				return _itemsInView;
			}
		}

		/// <summary>
		/// Retrieves the list of items to be displayed on the current page.
		/// </summary>
		public IResourceList ItemsOnPage
		{
			get
			{
				Preconditions( Pre.HasItems | Pre.HasViews | Pre.Paginated );

				if( _itemsOnPage == null )
					throw new InvalidOperationException( "The list of items on page must not be null." );

				return _itemsOnPage;
			}
		}

		/// <summary>
		/// Gets or sets the zero-based number of a page which is currently selected in the newspaper.
		/// </summary>
		public int CurrentPage
		{
			get
			{
				Preconditions( Pre.Paginated );

				Debug.Assert( _nCurrentPage < PagesCount, "The current page selection is beyond the last page." );
				return _nCurrentPage;
			}
			set
			{
				Preconditions( Pre.Initialized );

				if( (value < 0) || (value >= PagesCount) )
					throw new ArgumentOutOfRangeException( "value", value, "Number of the current page must be above or equal to zero and below the total number of pages." );

				bool bChanged = false; // Determines whether to raise the Changed event

				// Set the new value
				if( _nCurrentPage != value )
				{
					bChanged = true;
					_nCurrentPage = value;
					Trace.WriteLine( String.Format( "CurrentPage changed to {0}.", _nCurrentPage ), "[NPV]" );
					RecalculatePage();
				}

				// Notify about the paging change
				if( (bChanged) && (PagingChanged != null) && (IsInitialized) && (!_bInitializingOrDeinitializing) )
					SafeFireEvent( PagingChanged );
			}
		}

		/// <summary>
		/// Gets the number of pages in the newspaper view.
		/// </summary>
		/// <remarks>Guaranteed to be above zero.</remarks>
		public int PagesCount
		{
			get { return _nPagesCount; }
		}

		/// <summary>
		/// Gets or sets the current filtering view.
		/// </summary>
		/// <remarks><c>Null</c> value indicates the fake "All Items" view.</remarks>
		public IResource CurrentFilteringView
		{
			get
			{
				Preconditions( Pre.HasViews );

				return _currentView;
			}
			set
			{
				Preconditions( Pre.HasViews );

				// Validate
				if( !((value == null) || (_viewsFiltering.Contains( value ))) )
					throw new ArgumentException( "An attempt was made to select a view that is not available for filtering.", "value" );

				bool bChanged = false; // Determines whether to raise the Changed event

				// Store the new value
				if( _currentView != value )
				{
					bChanged = true;
					_currentView = value; // Store
					Trace.WriteLine( String.Format( "CurrentFilteringView changed to {0}.", (_currentView != null ? _currentView.DisplayName : "All Items") ), "[NPV]" );
					RecalculateView( true ); // Apply
				}

				// Fire the Changed event
				if( (bChanged) && (CurrentFilteringViewChanged != null) && (IsInitialized) && (!_bInitializingOrDeinitializing) )
					SafeFireEvent( CurrentFilteringViewChanged );
			}
		}

		/// <summary>
		/// Gets or sets the item that is currently selected in the newspaper.
		/// <c>Null</c> value means no selection and is valid both for getter and setter.
		/// Do not use this property to check if an item is selected or not, use <see cref="IsItemSelected"/> instead.
		/// </summary>
		/// <remarks>
		/// <para>Selecting an item from another page causes switching to that page.</para>
		/// <para>Selecting an item that does not fit the current newspaper filter causes the "All" filter to apply.</para>
		/// <para>An attempt to select an item that does not belong to the current newspaper view at all causes an exception.</para>
		/// <para>Note: do not use the property setter when making the UI operations, employ the <see cref="SelectItem"/> function instead that may control the item selection process.</para>
		/// </remarks>
		public IResource SelectedItem
		{
			get
			{
				if( !IsInitialized )
					return null; // Not ready, no selection

				// If the selected item has been deleted, deselect it
				if( (_itemSelected != null) && (_itemSelected.IsDeleted) )
					SelectedItem = null;

				return _itemSelected;
			}
			set { SelectItem( value, SelectionCause.Manual ); }
		}

		/// <summary>
		/// Gets the Omea Settings key for newspaper-global settings that are not related to a specific set of resource types being currently displayed.
		/// This key also serves as a base name for the resource-type-specific settings keys.
		/// </summary>
		public static string GlobalSettingsKey
		{
			get { return "NewspaperView"; }
		}

		#endregion

		#region Interface — Events

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

		#endregion

		#region FilteringViewAdded Event

		/// <summary>
		/// A new filtering view is available for this newspaper.
		/// </summary>
		public event ResourceIndexEventHandler FilteringViewAdded;

		/// <summary>
		/// Fires the <see cref="FilteringViewAdded"/> event.
		/// </summary>
		protected void FireFilteringViewAdded( ResourceIndexEventArgs args )
		{
			SafeFireEvent( FilteringViewAdded, args );
		}

		#endregion

		#region FilteringViewChanged Event

		/// <summary>
		/// One of the newspaper's filtering views has changed.
		/// </summary>
		public event ResourcePropIndexEventHandler FilteringViewChanged;

		/// <summary>
		/// Fires the <see cref="FilteringViewChanged"/> event.
		/// </summary>
		protected void FireFilteringViewChanged( ResourcePropIndexEventArgs args )
		{
			SafeFireEvent( FilteringViewChanged, args );
		}

		#endregion

		#region FilteringViewDeleted Event

		/// <summary>
		/// One of the filtering views has been made unavaillable for this newspaper.
		/// </summary>
		public event ResourceIndexEventHandler FilteringViewDeleted;

		/// <summary>
		/// Fires the <see cref="FilteringViewDeleted"/> event.
		/// </summary>
		protected void FireFilteringViewDeleted( ResourceIndexEventArgs args )
		{
			SafeFireEvent( FilteringViewDeleted, args );
		}

		#endregion

		/// <summary>
		/// The object has just been initialized.
		/// </summary>
		public event EventHandler Initializing;

		/// <summary>
		/// The object is about to be deinitialized.
		/// </summary>
		public event EventHandler Deinitializing;

		/// <summary>
		/// The <see cref="ItemsPerPage"/> property has changed.
		/// </summary>
		public event EventHandler ItemsPerPageChanged;

		/// <summary>
		/// The <see cref="CurrentFilteringView"/> property has changed.
		/// </summary>
		public event EventHandler CurrentFilteringViewChanged;

		/// <summary>
		/// Paging information has changed (either number of pages or the current page).
		/// </summary>
		public event EventHandler PagingChanged;

		/// <summary>
		/// A page has been left.
		/// </summary>
		public event EventHandler LeavePage;

		/// <summary>
		/// The page is being entered.
		/// It's not necessary to update items as that will be done by individual events.
		/// </summary>
		public event EventHandler EnterPage;

		#region ItemChanged

		/// <summary>
		/// A newspaper item has changed and should be updated in the view.
		/// </summary>
		public event ItemChangedEventHandler ItemChanged;

		/// <summary>
		/// Delegate for the <see cref="ItemChanged"/> event.
		/// </summary>
		public delegate void ItemChangedEventHandler( object sender, ItemChangedEventArgs args );

		/// <summary>
		/// Arguments for the <see cref="ItemChanged"/> event.
		/// </summary>
		public class ItemChangedEventArgs
		{
			private readonly IResource _item;

			private readonly IPropertyChangeSet _changes;

			public ItemChangedEventArgs( IResource item, IPropertyChangeSet changes )
			{
				_item = item;
				_changes = changes;

			}

			/// <summary>
			/// The item being updated.
			/// </summary>
			public IResource Item
			{
				get { return _item; }
			}

			/// <summary>
			/// Changes to the object properties that this event reports.
			/// </summary>
			public IPropertyChangeSet Changes
			{
				get { return _changes; }
			}
		}

		/// <summary>
		/// Fires the <see cref="ItemChanged"/> event.
		/// </summary>
		protected void FireItemChanged( ItemChangedEventArgs args )
		{
			SafeFireEvent( ItemChanged, args );
		}

		#endregion

		#region ItemRemoved Event

		/// <summary>
		/// A newspaper item has been deleted or has somehow become invisible.
		/// </summary>
		public event ResourceEventHandler ItemRemoved;

		/// <summary>
		/// Fires the <see cref="ItemRemoved"/> event.
		/// </summary>
		protected void FireItemRemoved( ResourceEventArgs args )
		{
			SafeFireEvent( ItemRemoved, args );
		}

		#endregion

		#region ItemAdded Event

		/// <summary>
		/// A new item is available that should be displayed in the newspaper.
		/// </summary>
		public event ItemAddedEventHandler ItemAdded;

		/// <summary>
		/// Delegate for the <see cref="ItemAdded"/> event.
		/// </summary>
		public delegate void ItemAddedEventHandler( object sender, ItemAddedEventArgs args );

		/// <summary>
		/// Arguments for the <see cref="ItemAdded"/> event.
		/// </summary>
		public class ItemAddedEventArgs
		{
			private readonly IResource _itemNew;
			private readonly IResource _itemInsertBefore;

			public ItemAddedEventArgs( IResource itemNew, IResource itemInsertBefore )
			{
				_itemNew = itemNew;
				_itemInsertBefore = itemInsertBefore;
			}

			/// <summary>
			/// The item that is being inserted.
			/// </summary>
			public IResource NewItem
			{
				get { return _itemNew; }
			}

			/// <summary>
			/// Item before which the new one should be inserted, or <c>Null</c> if it should be the last one.
			/// </summary>
			public IResource InsertBeforeItem
			{
				get { return _itemInsertBefore; }
			}
		}

		/// <summary>
		/// Fires the <see cref="ItemAdded"/> event.
		/// </summary>
		protected void FireItemAdded( ItemAddedEventArgs args )
		{
			SafeFireEvent( ItemAdded, args );
		}

		#endregion

		/// <summary>
		/// The selected item has changed.
		/// Passes the newly-selected resource as a parameter, which may be <c>Null</c> in case the selection has been reset.
		/// </summary>
		public event ResourceEventHandler SelectedItemChanged;

		#region EnsureVisible Event

		/// <summary>
		/// A newspaper item should be scrolled in view, according to the accompanying options.
		/// </summary>
		public event EnsureVisibleEventHandler EnsureVisible;

		/// <summary>
		/// Delegate type for the <see cref="EnsureVisible"/> event.
		/// </summary>
		public delegate void EnsureVisibleEventHandler( object sender, EnsureVisibleEventArgs args );

		/// <summary>
		/// Arguments for the <see cref="EnsureVisible"/> event.
		/// </summary>
		public class EnsureVisibleEventArgs
		{
			private readonly IResource _item;

			private readonly SelectionCause _cause;

			public EnsureVisibleEventArgs( IResource item, SelectionCause cause )
			{
				_item = item;
				_cause = cause;

			}

			/// <summary>
			/// Item that should be scrolled into view.
			/// The item is guaranteed to be present on the current newspaper page.
			/// </summary>
			public IResource Item
			{
				get { return _item; }
			}

			/// <summary>
			/// A reason for scrolling the item into view.
			/// Item position on screen after scrolling may depend on the reason.
			/// </summary>
			public SelectionCause Cause
			{
				get { return _cause; }
			}
		}

		/// <summary>
		/// Fires the <see cref="EnsureVisible"/> event.
		/// </summary>
		protected void FireEnsureVisible( EnsureVisibleEventArgs args )
		{
			if( (EnsureVisible != null) && (IsInitialized) )
				SafeFireEvent( EnsureVisible, args );
		}

		#endregion

		#region ItemSelected Event

		/// <summary>
		/// An item has been selected. Its visual cues have to be updated.
		/// </summary>
		public event ResourceEventHandler ItemSelected;

		/// <summary>
		/// Fires the <see cref="ItemSelected"/> event.
		/// </summary>
		protected void FireItemSelected( ResourceEventArgs args )
		{
			SafeFireEvent( ItemSelected, args );
		}

		#endregion

		#region ItemDeselected Event

		/// <summary>
		/// An item has been selected. Its visual cues have to be updated.
		/// </summary>
		public event ResourceEventHandler ItemDeselected;

		/// <summary>
		/// Fires the <see cref="ItemDeselected"/> event.
		/// </summary>
		protected void FireItemDeselected( ResourceEventArgs args )
		{
			SafeFireEvent( ItemDeselected, args );
		}

		#endregion

		/// <summary>
		/// The resource list provided by the <see cref="ItemsInView"/> property has changed.
		/// Note that this does not mean that any of the resources that belong to the list have changed,
		/// but that the contents of the list (which resources do belong to the list and which do not) has changed.
		/// </summary>
		/// <remarks>Note that the Newspaper Manager is not necessarily in the Initialized state when this event fires.
		/// Neither the items on page are guaranteed to correspond to the desired items set.
		/// The only thing guaranteed is that a call to the <see cref="ItemsInView"/> is valid in responce to the event.</remarks>
		public event EventHandler ItemsInViewChanged;

		#endregion

		#region Interface — Data Types

		/// <summary>
		/// Defines the reason due to which the item gets selected.
		/// It may has effect on the way it gets scrolled into view.
		/// </summary>
		public enum SelectionCause
		{
			/// <summary>
			/// No special cause to be mentioned.
			/// In other words, none of the following reasons fits the cause.
			/// </summary>
			Manual,

			/// <summary>
			/// An item gets selected due to being hovered with mouse.
			/// If the item is visible, at least in part, (yow, is the other choice possible?:), it should not be moved at all.
			/// </summary>
			MouseHover,

			/// <summary>
			/// An item gets selected due to being clicked with mouse.
			/// If the item is not entirely visible, it should be scrolled into view, if possible (if it can fit).
			/// </summary>
			MouseClick,

			/// <summary>
			/// Item gets selected while jumping to the next item.
			/// Item should be scrolled so that some of the next items would be visible too.
			/// </summary>
			GotoNext,

			/// <summary>
			/// Item gets selected while jumping to the previous item.
			/// Item should be scrolled so that some of the previous items would be visible too.
			/// </summary>
			GotoPrevious,

			/// <summary>
			/// An item gets selected along with a page switch either due to goto-next-(item/page) or manual item selection or manual page switch.
			/// Newspaper should be immediately positioned so that the selected item would be displayed.
			/// </summary>
			PageSwitch,

			/// <summary>
			/// The weak manual selection — if the item requested exists in the current view, it should be selected, if not, the closest item that exists in the current view should be selected instead.
			/// Note that making a selection with this param may result in no selection if the current view has no items.
			/// </summary>
			Approx
		}

		#endregion

		#region Implementation

		#region Implementation — Resource Event Handlers

		#region Views

		/// <summary>
		/// A new view has appeared in the list of views applicable for filtering the current newspaper.
		/// Executed in the Resource thread.
		/// </summary>
		protected void OnViewAdded( object sender, ResourceIndexEventArgs e )
		{
			Core.UserInterfaceAP.QueueJob( "Adding a Filtering View to Newspaper", new ResourceIndexEventHandler( OnViewAddedMarshalled ), new object[] {sender, e} );
		}

		/// <summary>
		/// A new view has appeared in the list of views applicable for filtering the current newspaper.
		/// Executed in the UI thread.
		/// </summary>
		protected void OnViewAddedMarshalled( object sender, ResourceIndexEventArgs e )
		{
			if( !IsInitialized )
				return;

			// Check if the new view may be a filtering view of the newspaper
			HashSet cache = null;
			if( (IsFilteringView( e.Resource, ref cache ) && (!_viewsFiltering.Contains( e.Resource ))) )
			{
				Trace.WriteLine( String.Format( "Adding a new filtering view {0}.", e.Resource.DisplayName ), "[NPV]" );

				// Add
				_viewsFiltering = _viewsFiltering.Union( e.Resource.ToResourceList(), true );

				// Notify
				FireFilteringViewAdded( e );
			}
		}

		/// <summary>
		/// A filtering view has changed.
		/// Executed in the Resource thread.
		/// </summary>
		protected void OnViewChanged( object sender, ResourcePropIndexEventArgs e )
		{
			Core.UserInterfaceAP.QueueJob( "A Filtering View of Newspaper Has Changed", new ResourcePropIndexEventHandler( OnViewChangedMarshalled ), new object[] {sender, e} );
		}

		/// <summary>
		/// A filtering view has changed.
		/// Executed in the UI thread.
		/// </summary>
		protected void OnViewChangedMarshalled( object sender, ResourcePropIndexEventArgs e )
		{
			if( !IsInitialized )
				return;

			// Some view has changed; this may mean that:
			// •1 a view has just to be updated, its name repainted, and its contents recalculated
			// •2 a non-fit view has become a fit view; add it to the list
			// •3 a fit view has become a non-fit view; remove it from the list and jump to the All view if it was active

			HashSet cache = null;
			if( _viewsFiltering.Contains( e.Resource ) ) // Either •1 or •3
			{
				if( IsFilteringView( e.Resource, ref cache ) ) // •1
					FireFilteringViewChanged( e ); // Notify of the change
				else
				{ // •3
					Trace.WriteLine( String.Format( "{0} is no more a filtering view.", e.Resource.DisplayName ), "[NPV]" );

					if( CurrentFilteringView == e.Resource ) // It was the current view and must be deselected first
						CurrentFilteringView = null;

					// Remove from the views list
					_viewsFiltering = _viewsFiltering.Minus( e.Resource.ToResourceList() );

					// Notify of the change
					FireFilteringViewDeleted( new ResourceIndexEventArgs( e.Resource, e.Index ) );
				}
			}
			else // Possibly •2
			{
				if( IsFilteringView( e.Resource, ref cache ) )
				{ // •2
					Trace.WriteLine( String.Format( "{0} has become a filtering view.", e.Resource.DisplayName ), "[NPV]" );

					// Add this new view to the list
					_viewsFiltering = _viewsFiltering.Union( e.Resource.ToResourceList() );

					// Notify of the change
					FireFilteringViewAdded( new ResourceIndexEventArgs( e.Resource, e.Index ) );
				}
			}
		}

		/// <summary>
		/// A filtering view has been deleted.
		/// Executed in the Resource thread.
		/// </summary>
		protected void OnViewDeleted( object sender, ResourceIndexEventArgs e )
		{
			Core.UserInterfaceAP.QueueJob( "Removing a Filtering View from Newspaper", new ResourceIndexEventHandler( OnViewDeletedMarshalled ), new object[] {sender, e} );
		}

		/// <summary>
		/// A filtering view has been deleted.
		/// Executed in the UI thread.
		/// </summary>
		protected void OnViewDeletedMarshalled( object sender, ResourceIndexEventArgs e )
		{
			if( !IsInitialized )
				return;

			if( _viewsFiltering.Contains( e.Resource ) )
			{
				Trace.WriteLine( String.Format( "Deleting filtering view {0}.", e.Resource.DisplayName ), "[NPV]" );

				// Deselect if the active view is being deleted
				if( CurrentFilteringView == e.Resource )
					CurrentFilteringView = null; // "All Items" view

				// Drop from the list
				_viewsFiltering = _viewsFiltering.Minus( e.Resource.ToResourceList() );

				// Notify of the change
				FireFilteringViewDeleted( e );
			}
		}

		#endregion

		#region Items

		/// <summary>
		/// An item has appeared in the list of available items.
		/// Executed in the Resource thread.
		/// </summary>
		protected void OnItemAdded( object sender, ResourceIndexEventArgs e )
		{
			Core.UserInterfaceAP.QueueJob( "Adding an Item to Newspaper", new ResourceIndexEventHandler( OnItemAddedMarshalled ), new object[] {sender, e} );
		}

		/// <summary>
		/// An item has appeared in the list of available items.
		/// Executed in the UI thread.
		/// </summary>
		protected void OnItemAddedMarshalled( object sender, ResourceIndexEventArgs e )
		{
			if( !IsInitialized )
				return;

			// Check if the item fits into the current view; if not, do not bother
			if( !_itemsInViewLive.Contains( e.Resource ) )
				return;

			Trace.WriteLine( String.Format( "Adding an item \"{0}\" #{1} to the current view.", e.Resource.DisplayName, e.Resource.OriginalId ), "[NPV]" );

			// This will reapply the view (no need for fetching the items), update paging information, and switch to another page if needed
			// No-more-fit items won't go away
			Core.UserInterfaceAP.QueueJob( "Recalculate the Current Newspaper View.", new MethodInvoker( RecalculateViewDeferred ) );
		}

		/// <summary>
		/// A newspaper item has changed.
		/// Executed in the Resource thread.
		/// </summary>
		protected void OnItemChanged( object sender, ResourcePropIndexEventArgs e )
		{
			Core.UserInterfaceAP.QueueJob( "An Item Has Changed in Newspaper", new ResourcePropIndexEventHandler( OnItemChangedMarshalled ), new object[] {sender, e} );
		}

		/// <summary>
		/// A newspaper item has changed.
		/// Executed in the UI thread.
		/// </summary>
		protected void OnItemChangedMarshalled( object sender, ResourcePropIndexEventArgs e )
		{
			if( !IsInitialized )
				return;

			if( !ItemsOnPage.Contains( e.Resource ) ) // Item is not visible, do not update
				return;

			Trace.WriteLine( String.Format( "Updating the dirty item \"{0}\" #{1} on the current page.", e.Resource.DisplayName, e.Resource.OriginalId ), "[NPV]" );

			// Cause the item to be updated
			FireItemChanged( new ItemChangedEventArgs( e.Resource, e.ChangeSet ) );
		}

		/// <summary>
		/// A newspaper item is about to be deleted.
		/// Executed in the Resource thread.
		/// </summary>
		protected void OnItemDeleting( object sender, ResourceIndexEventArgs e )
		{
			Core.UserInterfaceAP.QueueJob( "Removing an Item from Newspaper", new ResourceIndexEventHandler( OnItemDeletingMarshalled ), new object[] {sender, e} );
		}

		/// <summary>
		/// A newspaper item is about to be deleted.
		/// Executed in the UI thread.
		/// </summary>
		protected void OnItemDeletingMarshalled( object sender, ResourceIndexEventArgs e )
		{
			if( !IsInitialized )
				return;

			// Check if the item belongs to the dead snapshot we're currently visualizing; from the live one it's definitely missing by this time
			if( !_itemsInView.Contains( e.Resource ) )
				return;
			/*	// Do not throw the exception as the item may still be present in the resource list while we're executing the handler
			if( _itemsInViewLive.Contains( e.Resource ) )
				throw new InvalidOperationException( "The deleted resource is still present in the live view's list." );
				*/

			Trace.WriteLine( String.Format( "Removing an item \"{0}\" #{1} from the current view.", e.Resource.DisplayName, e.Resource.OriginalId ), "[NPV]" );

			// This will reapply the view, update paging information, and select some item which is the closest to the gone one
			Core.UserInterfaceAP.QueueJob( "Recalculate the Current Newspaper View.", new MethodInvoker( RecalculateViewDeferred ) );
		}

		#endregion

		#endregion

		/// <summary>
		/// A set of preconditions that must hold in a specific case.
		/// </summary>
		[Flags]
		protected enum Pre
		{
			None = 0x01,
			HasResourceTypes = 0x02,
			HasViews = 0x04,
			Initialized = 0x08,
			Uninitialized = 0x10,
			HasItems = 0x20,
			HasItemsPerPage = 0x40,
			Paginated = 0x80
		}

		/// <summary>
		/// Ensures that the specified preconditions hold.
		/// </summary>
		protected void Preconditions( Pre pre )
		{
			// Ensure that we're on the proper thread
			if( (!Core.UserInterfaceAP.IsOwnerThread) && (pre != Pre.Uninitialized) ) // Do not check from the finalizer
				throw new InvalidOperationException( "The newspaper is being executed on a foreign thread." );

			// Check the stock conditions
			if( ((pre & Pre.HasViews) != 0) && ((_viewsFiltering == null) || (_viewsAll == null)) )
				throw new InvalidOperationException( "Filtering views not defined." );
			if( ((pre & Pre.HasResourceTypes) != 0) && (_resourceTypes == null) )
				throw new InvalidOperationException( "Resource types not defined." );
			if( ((pre & Pre.Initialized) != 0) && (!IsInitialized) )
				throw new InvalidOperationException( "The newspaper must be initialized." );
			if( ((pre & Pre.Uninitialized) != 0) && (!IsUninitialized) )
				throw new InvalidOperationException( "The newspaper must be non-initialized." );
			if( ((pre & Pre.HasItems) != 0) && (_itemsAvailLive == null) )
				throw new InvalidOperationException( "The newspaper must be populated with items." );
			if( ((pre & Pre.HasItemsPerPage) != 0) && (_nItemsPerPage == -1) )
				throw new InvalidOperationException( "Numer of items per page must be specified." );
			if( (pre & Pre.Paginated) != 0 )
			{
				if( (_nPagesCount <= 0) || (_nCurrentPage < 0) || (_nItemsPerPage <= 0) )
					throw new InvalidOperationException( "The newspaper has not been paginated." );
				if( _nCurrentPage >= _nPagesCount )
					throw new InvalidOperationException( "The newspaper paging information is invalid." );
			}
		}

		/// <summary>
		/// Initializes or reinitializes the newspaper bar for displaying the new newspaper.
		/// </summary>
		/// <param name="resources">The list of all the items available for display on the newspaper.</param>
        public void Initialize( IResourceList resources )
        {
            Preconditions( Pre.Uninitialized );
            Trace.WriteLine( "Started initializing the newspaper.", "[NPV]" );

            _bInitializingOrDeinitializing = true;

            // Store the list of resources
            lock( resources )
            {
                _itemsAvailLive = resources;
                RecalculateItemsAvail(); // Fill in _itemsAvail
            }

            // Wire up the events
            _itemsAvailLive.ResourceAdded += OnItemAdded;
            _itemsAvailLive.ResourceChanged += OnItemChanged;
            _itemsAvailLive.ResourceDeleting += OnItemDeleting;

            ////////////////////////////
            // Pick the resource types

            _resourceTypes = _itemsAvail.GetAllTypes();

            ///////////////////////////
            // Update the views list

            // Pick only those views that intersect with this resource type list (or have no restrictions)
            _viewsAll = Core.FilterRegistry.GetViews();
            _viewsFiltering = Core.ResourceStore.EmptyResourceList;
            List<int> viewIds = new List<int>();
            HashSet cache = null;

            foreach( IResource view in _viewsAll )
            {
                if( IsFilteringView( view, ref cache ) )
                {
                    viewIds.Add( view.Id );
                }
            }
            if( viewIds.Count > 0 )
            {
                _viewsFiltering = Core.ResourceStore.ListFromIds( viewIds, false );
            }
			
            // Monitor changes in views
            _viewsAll.ResourceAdded += OnViewAdded;
            _viewsAll.ResourceChanged += OnViewChanged;
            _viewsAll.ResourceDeleting += OnViewDeleted;

            //////////////////////
            // Load the settings
            // This will also apply a setting to the combos
            SerializeSettings( false );

            //////////////////////
            // Initialize the view
            RecalculateView( true );
            _itemsOnPage = Core.ResourceStore.EmptyResourceList; // There are still no items in the view. Make the back-end correspond

            _bInitializingOrDeinitializing = false;

            Preconditions( Pre.Initialized );

            // Notify of state change
            SafeFireEvent( Initializing, EventArgs.Empty );

            // Listen to changes in the settings
            Core.UIManager.AddOptionsChangesListener( "Omea", "General", OnSettingsChanged );
            OnSettingsChanged( null, EventArgs.Empty );

            // Fill the first page in
            RecalculateView( false ); // Re-fill the empty list of items on page, and also submit the items to the view

            Trace.WriteLine( "Finished initializing the newspaper.", "[NPV]" );
            Preconditions( Pre.Initialized );
        }

		/// <summary>
		/// Deinitializes the newspaper bar before turning off the newspaper view.
		/// </summary>
		public void Deinitialize()
		{
			Trace.WriteLine( "Started deinitializing the newspaper.", "[NPV]" );
			Preconditions( Pre.Initialized );

			// Stop listening to changes in the settings
			Core.UIManager.RemoveOptionsChangesListener( "Omea", "General", OnSettingsChanged );

			// Cancel the pending view updates
			Core.UserInterfaceAP.CancelJobs( new MethodInvoker( RecalculateViewDeferred ) );

			// Switch off the last page (this will also remove selection from the selected item that deinitely leaves the page)
			SwitchPage( ItemsOnPage, null );
			_itemsOnPage = Core.ResourceStore.EmptyResourceList; // As there are no items on-screen now, make the back-end correspond

			// Notify of state change
			SafeFireEvent( Deinitializing ); // This should be the last event, and it should not be called under _bInitializingOrDeinitializing

			// Save the newspaper settings
			SerializeSettings( true );

			_bInitializingOrDeinitializing = true;

			// Invalidate the drived settings
			_itemsOnPage = null;
			_itemsInView = null;
			_itemsInViewLive = null;

			// Deinit the items list
			_itemsAvail = null;
			_resourceTypes = null;
			_itemsAvailLive.ResourceAdded -= OnItemAdded;
			_itemsAvailLive.ResourceChanged -= OnItemChanged;
			_itemsAvailLive.ResourceDeleting -= OnItemDeleting;
			_itemsAvailLive = null;

			// Deinit the filtering views list
			_viewsFiltering = null;
			_viewsAll.ResourceAdded -= OnViewAdded;
			_viewsAll.ResourceChanged -= OnViewChanged;
			_viewsAll.ResourceDeleting -= OnViewDeleted;
			_viewsAll = null;

			// Mark as deinitialized
			_nItemsPerPage = -1;
			_nPagesCount = -1;
			_nCurrentPage = -1;

			_bInitializingOrDeinitializing = false;

			Trace.WriteLine( "Finished deinitializing the newspaper.", "[NPV]" );
			Preconditions( Pre.Uninitialized );
		}

#if DEBUG
		~NewspaperManager()
		{
			// Assert that the object has been deinitialized
			Preconditions( Pre.Uninitialized );
		}
#endif

		/// <summary>
		/// Checks whether this instance is completely initialized (<c>1</c>), totally non-initialized (<c>-1</c>), or at some intermediate state (<c>0</c>).
		/// </summary>
		protected int InitializedState
		{
			get
			{
				// Calculate the initialization points
				// ---
				// This function checks the initialized state by testing individual components;
				//    each test case results in some points given or reclaimed
				// If a test case indicates initialized state of the individual component, it's +1 point to the score
				// If a test case indicates uninitialized state of the individual component, it's -1 point from the score
				int score = 0; // For each initialized case, add 1; for each unitialized, add -1
				int cases = 0; // Count the number of test cases

				score += (_itemsAvail != null) ? +1 : -1;
				cases++;
				score += (_itemsAvailLive != null) ? +1 : -1;
				cases++;
				score += (_itemsInView != null) ? +1 : -1;
				cases++;
				score += (_itemsInViewLive != null) ? +1 : -1;
				cases++;
				score += (_itemsOnPage != null) ? +1 : -1;
				cases++;
				score += (_viewsFiltering != null) ? +1 : -1;
				cases++;
				score += (_viewsAll != null) ? +1 : -1;
				cases++;
				score += (_nItemsPerPage != -1) ? +1 : -1;
				cases++;
				score += (_resourceTypes != null) ? +1 : -1;
				cases++;
				score += (_nPagesCount != -1) ? +1 : -1;
				cases++;
				score += (_nCurrentPage != -1) ? +1 : -1;
				cases++;

				// Now, if all the cases indicated initialized state, the score should be equal to the number of cases
				if( score == cases )
					return 1;

				// Seemingly, if all the cases indicated uninitialized state, the score should be equal to the negated number of cases
				if( score == -cases )
					return -1;

				// Neither of the above — the thing is in some intermediate state, neither completely initialized nor completely uninitialized
				return 0;
			}
		}

		/// <summary>
		/// Saves or restores those newspaper settings that are handled by this bar.
		/// </summary>
		/// <param name="saving">Whether we're saving or loading the settings.</param>
		protected void SerializeSettings( bool saving )
		{
			// Generate the options key name based on the set of resources in this newspaper
			string sKeyName = GetSettingsKey( false );

			if( saving ) // Saving
			{
				Preconditions( Pre.Initialized );

				Core.SettingStore.WriteInt( sKeyName, "ItemsPerPage", ItemsPerPage );
				Core.SettingStore.WriteInt( sKeyName, "MruViewId", (CurrentFilteringView != null ? CurrentFilteringView.OriginalId : -1) );
			}
			else // Loading
			{
				int nItemsPerPage = Core.SettingStore.ReadInt( sKeyName, "ItemsPerPage", 10 );
				if( nItemsPerPage <= 0 )
					nItemsPerPage = 10;
				ItemsPerPage = nItemsPerPage;

				// Get the MRU view ID from the saved settings
				int nMruViewId = Core.SettingStore.ReadInt( sKeyName, "MruViewId", -1 );

				// Try to pick this view's resource
				IResource resMruView = null;
				if( nMruViewId != -1 )
				{
					resMruView = Core.ResourceStore.TryLoadResource( nMruViewId );
					if( (resMruView == null) || (!_viewsFiltering.Contains( resMruView )) ) // Ensure this view falls into the combobox (if it's non-null)
						resMruView = null;
				}
				CurrentFilteringView = resMruView;
			}
		}

		/// <summary>
		/// Updates the number of pages in the newspaper.
		/// Also this function notifies of switching the pages which causes the set of items to update, in case the object is in working state.
		/// This function is called initially and from <see cref="RecalculatePage"/>. Does not update <see cref="ItemsOnPage"/>, 
		///		this must be done by the caller.
		/// </summary>
		protected void Repaginate()
		{
			Preconditions( Pre.HasViews | Pre.HasItemsPerPage );
			Trace.WriteLine( String.Format( "Repaginating the newspaper." ), "[NPV]" );

			// Store to detect whether to raise the event
			int nOldPagesCount = _nPagesCount;
			int nOldCurrentPage = _nCurrentPage;

			// Calculate the new number of pages
			_nPagesCount = (int)Math.Ceiling( (double)ItemsInView.Count / (double)ItemsPerPage );
			_nPagesCount = _nPagesCount >= 1 ? _nPagesCount : 1; // Enforce one page at least

			// Adjust the current page, if necessary
			_nCurrentPage = _nCurrentPage <= _nPagesCount - 1 ? (_nCurrentPage >= 0 ? _nCurrentPage : 0) : _nPagesCount - 1;

			// Throw the event, if neeeded
			if( ((nOldPagesCount != PagesCount) || (nOldCurrentPage != CurrentPage)) && (PagingChanged != null) && (IsInitialized) )
				SafeFireEvent( PagingChanged );
		}

		/// <summary>
		/// A function that should be called when newspaper switches to another page.
		/// This includes the case of switching to the first page ever displayed from nowhere (on init), and vice versa (on deinit).
		/// </summary>
		/// <remarks>At most one parameter may be <c>Null</c>, in case we're initializing or deinitialzing only. Other cases should pass empty lists if needed.</remarks>
		protected void SwitchPage( IResourceList itemsOld, IResourceList itemsNew )
		{
			Preconditions( Pre.Initialized );
			if( (itemsOld == null) && (itemsNew == null) )
				throw new ArgumentNullException( "itemsOld", "Both lists of the page-switching function cannot be null simultaneously." );
			Trace.WriteLine( String.Format( "Switching a page from {0} pcs to {1} pcs.", (itemsOld != null ? itemsOld.Count.ToString() : "<null>"), (itemsNew != null ? itemsNew.Count.ToString() : "<null>") ), "[NPV]" );

			// Debug Output: dump all the items being added and removed; in non-release versions only
			if( Core.ProductReleaseVersion == null )
			{
				if( itemsOld != null )
				{
				    StringWriter sw = new StringWriter();
					sw.Write( "Old items:" );
					foreach( IResource item in itemsOld )
						sw.Write( " \"{0}#{1}\"", item.DisplayName, item.OriginalId );
					Trace.WriteLine( sw.ToString(), "[NPV]" );
				}
				if( itemsNew != null )
				{
					StringWriter sw = new StringWriter();
					sw.Write( "New items:" );
					foreach( IResource item in itemsNew )
						sw.Write( " \"{0}#{1}\"", item.DisplayName, item.OriginalId );
					Trace.WriteLine( sw.ToString(), "[NPV]" );
				}
			}

			// Calculate the sets
			IResourceList itemsPersistent = ((itemsOld != null) && (itemsNew != null)) ? itemsOld.Intersect( itemsNew ) : Core.ResourceStore.EmptyResourceList; // Items that will stay
			IResourceList itemsGone = itemsOld != null ? itemsOld.Minus( itemsPersistent ) : Core.ResourceStore.EmptyResourceList; // Items that have to go away
			IResourceList itemsCome = itemsNew != null ? itemsNew.Minus( itemsPersistent ) : Core.ResourceStore.EmptyResourceList;
			; // Items that should come

			// If there are items to add, prepare a sorted array of their indices in the view's list
			// The second array should contain the persistent items indices in a sorted order
			IntArrayList arPersistent = null; // Indices of the persistent items
			IntArrayList arCome = null; // Indices of the newcomer items
			try
			{
				if( (itemsNew != null) && (itemsCome.Count > 0) && (itemsPersistent.Count != 0) ) // No need if there are no persistent items
				{
					// A sorted array of persistent items
					arPersistent = IntArrayListPool.Alloc();
					foreach( IResource item in itemsPersistent )
						arPersistent.Add( ItemsInView.IndexOf( item ) );
					arPersistent.Sort();

					// A sorted array of newcomers
					arCome = IntArrayListPool.Alloc();
					foreach( IResource item in itemsCome )
						arCome.Add( ItemsInView.IndexOf( item ) );
					arCome.Sort();
				}

				// Remove selection if the corresponding item goes away
				if( itemsOld != null )
				{
					// Remove selection if the selected item is about to leave the page
					if( (SelectedItem != null) && (itemsGone.Contains( SelectedItem )) )
						SelectItem( null, SelectionCause.PageSwitch );

					// Fire the page-leaves event
					SafeFireEvent( LeavePage );

					// Remove old items
					if( ItemRemoved != null )
					{
						foreach( IResource item in itemsGone )
							FireItemRemoved( new ResourceEventArgs( item ) );
					}
				}

				// Fire the page-enter event
				if( itemsNew != null )
				{
					// Add new items
					if( (ItemAdded != null) && (itemsCome.Count > 0) )
					{
						if( itemsPersistent.Count != 0 )
						{ // There are persistent items — choose the proper places to insert the newcomers
							int nBefore = 0; // Index of the persistent item before which the newcomer should be inserted
						    for( int nNew = 0; nNew < arCome.Count; nNew++ )
							{
								// Find the persistent item before which this one should be inserted — the first with a greater index
								for(; (nBefore < arPersistent.Count) && (arPersistent[ nBefore ] <= arCome[ nNew ]); nBefore++ )
									;

								// Item before which to insert, or null, if beyond the end
								IResource itemBefore = nBefore < arPersistent.Count ? ItemsInView[ arPersistent[ nBefore ] ] : null;

								// Do the insert
								FireItemAdded( new ItemAddedEventArgs( ItemsInView[ arCome[ nNew ] ], itemBefore ) );
							}
						}
						else
						{ // No persistent items — no problems with insertion, just respect the order
							foreach( IResource item in itemsNew )
								FireItemAdded( new ItemAddedEventArgs( item, null ) );
						}

					}

					// Fire the page-enters event
					SafeFireEvent( EnterPage );

					// Select something on the new page (unless there was a selected item that survived the page switch in the itemsPersistent)
					if( (ItemsOnPage.Count != 0) && (SelectedItem == null) )
						SelectItem( ItemsOnPage[ 0 ], SelectionCause.PageSwitch );
				}
			}
			finally
			{
				if( arPersistent != null )
				{
					IntArrayListPool.Dispose( arPersistent );
				}
				if( arCome != null )
				{
					IntArrayListPool.Dispose( arCome );
				}
			}
		}

		/// <summary>
		/// An item has to be marked as read.
		/// </summary>
		protected void OnMarkAsReadElapsed()
		{
			if( !IsInitialized ) // Dummy check in case the timer is stopped too slowly
				return;

			// Mark the selected item as read
			if( (SelectedItem != null) && (_bAllowAutoMarkAsRead) )
			{
				Trace.WriteLine( String.Format( "Mark-as-read timer is marking \"{0}\" #{1} as read.", SelectedItem.DisplayName, SelectedItem.OriginalId ), "[NPV]" );
				new ResourceProxy( SelectedItem ).DeletePropAsync( Core.Props.IsUnread );
			}
		}

		/// <summary>
		/// Provides number of the page to which the item of question belongs.
		/// </summary>
		private int PageNumberFromItem( IResource item )
		{
			Preconditions( Pre.Initialized );

			if( PagesCount == 1 )
				return 1;

			int nIndex = ItemsInView.IndexOf( item );
			if( nIndex == -1 )
				throw new InvalidOperationException( "Cannot tell the number of page for an item that does not fall into the current newspaper filtering view." );

			return nIndex / ItemsPerPage;
		}

		/// <summary>
		/// Omea Settings have changed. Update the corresponding parameters.
		/// </summary>
		protected void OnSettingsChanged( object sender, EventArgs e )
		{
			Trace.WriteLine( String.Format( "Omea settings have changed, re-quering the settings." ), "[NPV]" );

			// Allow/disallow
			_bAllowAutoMarkAsRead = Core.SettingStore.ReadBool( GetSettingsKey( true ), "AllowAutoMarkAsRead", _bAllowAutoMarkAsRead );
			_bMarkAsReadOnGotoNext = Core.SettingStore.ReadBool( GetSettingsKey( true ), "MarkAsReadOnGotoNext", _bMarkAsReadOnGotoNext ); // Read this supplimentary setting, too
		}

		/// <summary>
		/// Rebuilds the dead ItemsAvail list from the live one.
		/// </summary>
		protected void RecalculateItemsAvail()
		{
			Preconditions( Pre.HasItems );

			_itemsAvail = KillResourceList( _itemsAvailLive );
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if( !IsUninitialized )
				Deinitialize();
		}

		#endregion
	}
}

// TODO: check when to lock the live versions of the resource lists