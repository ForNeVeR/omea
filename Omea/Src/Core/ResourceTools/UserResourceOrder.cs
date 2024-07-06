// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	#region UserResourceOrder Class — Manages the Order and Its Persistence

	/// <summary>
	/// Supports handling the user order in a serialized way on a resource property.
	/// </summary>
	public class UserResourceOrder
	{
		#region Data

		/// <summary>
		/// The resource that holds the property with serialized user-order.
		/// Cannot be <c>Null</c>.
		/// </summary>
		protected IResource _resPropertyHolder;

		/// <summary>
		/// Identifier of the property on the <see cref="_resPropertyHolder"/> resource.
		/// </summary>
		protected int _nPropertyId = Core.Props.UserResourceOrder;

		/// <summary>
		/// A resource list containing at most one resource that holds the property that defines the user sort order over the resources controlled by this settings object.
		/// </summary>
		protected IResourceList _resChangeListener = null;

		/// <summary>
		/// Priority for the resource-user-order writing operations.
		/// </summary>
		protected JobPriority _priority = JobPriority.Normal;

		#endregion

		#region Construction

		/// <summary>
		/// Initializes the object.
		/// </summary>
		/// <param name="resPropertyHolder">The resource that stores the user-resource-order of its children in a property.</param>
		public UserResourceOrder( IResource resPropertyHolder )
		{
			Init( resPropertyHolder, _nPropertyId, _priority );
		}

		/// <summary>
		/// Initializes the object.
		/// </summary>
		/// <param name="resPropertyHolder">The resource that stores the user-resource-order of its children in a property.</param>
		/// <param name="priority">Priority for the write operations.</param>
		public UserResourceOrder( IResource resPropertyHolder, JobPriority priority )
		{
			Init( resPropertyHolder, _nPropertyId, priority );
		}

		/// <summary>
		/// Initializes the object.
		/// </summary>
		/// <param name="resPropertyHolder">The resource that stores the user-resource-order of its children in a property.</param>
		/// <param name="nPropertyId">ID of the property that stores the user-resource-order of the children in a serialized manner.</param>
		public UserResourceOrder( IResource resPropertyHolder, int nPropertyId )
		{
			Init( resPropertyHolder, nPropertyId, _priority );
		}
		/// <summary>
		/// Initializes the object.
		/// </summary>
		/// <param name="resPropertyHolder">The resource that stores the user-resource-order of its children in a property.</param>
		/// <param name="nPropertyId">ID of the property that stores the user-resource-order of the children in a serialized manner.</param>
		/// <param name="priority">Priority for the write operations.</param>
		public UserResourceOrder( IResource resPropertyHolder, int nPropertyId, JobPriority priority )
		{
			Init( resPropertyHolder, nPropertyId, priority );
		}

		/// <summary>
		/// Implements the construction logic.
		/// </summary>
		protected void Init( IResource resPropertyHolder, int nPropertyId, JobPriority priority )
		{
			// Resource
			if( resPropertyHolder == null )
				throw new ArgumentNullException( "resPropertyHolder" );
			_resPropertyHolder = resPropertyHolder;

			// Property
			if( _nPropertyId == 0 )
				throw new ArgumentNullException( "nPropertyId" );
			if( Core.ResourceStore.PropTypes[ nPropertyId ] == null )
				throw new ArgumentException( String.Format( "", nPropertyId ), "nPropertyId" );
			_nPropertyId = nPropertyId;

			// Start listening to the property changes
			_resChangeListener = _resPropertyHolder.ToResourceListLive();
			_resChangeListener.ResourceChanged += new ResourcePropIndexEventHandler( OnOrderPropertyHolderChanged );
			_resChangeListener.ResourceDeleting += new ResourceIndexEventHandler( OnOrderPropertyHolderDeleting );
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets or sets the resource that holds the property with serialized user-order.
		/// </summary>
		public IResource PropertyHolder
		{
			get { return _resPropertyHolder; }
		}

		/// <summary>
		/// Identifier of the property on the <see cref="_resPropertyHolder"/> resource.
		/// </summary>
		public int PropertyId
		{
			get { return _nPropertyId; }
		}

		/// <summary>
		/// Gets whether the node attached to this object currently has a user-order specified.
		/// </summary>
		public bool HasOrder
		{
			get { return _resPropertyHolder.HasProp( _nPropertyId ); }
		}

		#endregion

		#region Operations

		/// <summary>
		/// Writes the new sorting order of the resources into the user order property.
		/// </summary>
		/// <param name="ids">A collection of resource IDs in the <b>reverse</b> sorting order.
		/// Thus, the resource that will appear at the end of the list must go first, and so on.</param>
		/// <remarks>It's OK to pass IDs of the resources that have been deleted. Such items will be ignored.</remarks>
		public void WriteSortOrder( ICollection ids )
		{
			// Serialize the IDs into a byte array
			MemoryStream ms = new MemoryStream( ids.Count * 4 );
			BinaryWriter bw = new BinaryWriter( ms );
			foreach( int id in ids )
			{
				if(Core.ResourceStore.TryLoadResource( id ) == null)
					continue;	// The resource has been deleted
				bw.Write( id );
			}

			// Encote into Base64
			string sOrder = Convert.ToBase64String( ms.GetBuffer() );

			// Write to a property
			_resPropertyHolder.Lock();
			try
			{
				if( !_resPropertyHolder.IsDeleted )
					new ResourceProxy( _resPropertyHolder, _priority ).SetPropAsync( Core.Props.UserResourceOrder, sOrder );
			}
			finally
			{
				_resPropertyHolder.UnLock();
			}
		}

		/// <summary>
		/// Inserts a set of the resources into the user sort order sequence.
		/// </summary>
		/// <param name="idPoint">ID of the resource that marks out the insertion point. <c>0</c> to insert without the insertion point (beginning/end of the list).</param>
		/// <param name="ids">A list of IDSs of the resources that should be inserted, in the <b>direct</b> order.</param>
		/// <param name="bInsertAfter"><c>False</c> to insert before the marker resource <paramref name="idPoint"/>, and <c>True</c> to insert after it. If the <paramref name="idPoint"/> is <c>0</c>, the new resources go to the beginning/end of the list, respectively.</param>
		/// <param name="existingids">An optional list of IDs of the existing resources we're inserting between, in the <b>direct</b> order; useful in case there's no user-order list yet.</param>
		public void Insert( int idPoint, ICollection ids, bool bInsertAfter, ICollection existingids )
		{
			if( ids == null )
				throw new ArgumentNullException();

			// Create a hash to check quickly whether the particular resource is being inserted and has to be removed from the old list
			IntHashSet hashNewcomers = new IntHashSet( ids.Count );
			foreach( int id in ids )
				hashNewcomers.Add( id );
			IntArrayList arNewcomers = new IntArrayList( ids ); // A reverse list of the newly-coming resources
			arNewcomers.Reverse();

			// Get the list of the resources as they were ordered previously (reversed)
			IntArrayList arOld = ReadOrder();
			IntHashSet hashOld = new IntHashSet( arOld.Count );
			foreach( int id in arOld )
				hashOld.Add( id );

			// Special case: there's no list yet, so it should be created using the resources we have in the current sort order (if provided)
			if( existingids != null )
			{
				if( arOld.Count == 0 )
				{
					arOld.AddRange( existingids );
					arOld.Reverse(); // Reverse the order to make it agree with the other sets
				}
				else
				{ // If the list exists, but some entries reported as existing are missing from it, they should be explicidly added also
					// They should be added to the end, thus to the beginning of the reverse array in the reverse order
					IntArrayList	arOldOld = arOld;
					arOld = new IntArrayList(existingids.Count);
					foreach(int id in existingids)
					{
						if((!hashOld.Contains( id )) && (!hashNewcomers.Contains( id )))	// Don't add the items that are being dragged
							arOld.Add( id );
					}
					arOld.Reverse();
					arOld.AddRange( arOldOld );
				}
			}

			// Here the new order will be stored; allocate to the maximum possible size
			IntArrayList arNew = new IntArrayList( arOld.Count + ids.Count );

			// Special check: if the droptarget is not present in the list, add all the resources either to the beginning or to the end
			if( (idPoint == 0) || (arOld.IndexOf( idPoint ) < 0) )
			{ // Droptarget not found
				if( bInsertAfter )
				{
					arNew.AddRange( arNewcomers );
					arNew.AddRange( arOld );
				}
				else
				{
					arNew.AddRange( arOld );
					arNew.AddRange( arNewcomers );
				}
			}
			else
			{ // Droptarget is there, go on inserting
				foreach( int idOld in arOld )
				{
					// Take along the current resource (if it's not excluded due to being in the inserted resources, and if it's not a droptarget in case the insertion should go before (=added after) the target)
					if( (!hashNewcomers.Contains( idOld )) && (!((idOld == idPoint) && (bInsertAfter))) )
						arNew.Add( idOld );

					// Copy the newcomers if we're on the droptarget
					if( idOld == idPoint )
						arNew.AddRange( arNewcomers );

					// Take along the current resource in case it's the droptarget, and the insertion is to go after (=added before) it, and it has not been already included in the newcomers list
					if( (idOld == idPoint) && (bInsertAfter) && (!hashNewcomers.Contains( idPoint )) )
						arNew.Add( idPoint );
				}
			}

			// Commit
			WriteSortOrder( arNew );
		}

		/// <summary>
		/// Reads the user sort order from the given resource.
		/// </summary>
		/// <param name="cache">Hashset to which the order is written: the resource IDs get added in the <b>reverse</b> order, so that the bucket-comparer of the hash set allows to sort the resources against the reverse sorting order (thus naturally putting missing items to the end as they have zero bucket values).</param>
		public void ReadOrder( ref IntHashSet cache )
		{
			// TODO: is it possible to preallocate cache to the known size?
			_resPropertyHolder.Lock();
			try
			{
				// Foolproof checks
				if( _resPropertyHolder.IsDeleted )
					return;
				if( !_resPropertyHolder.HasProp( Core.Props.UserResourceOrder ) )
					return;

				// Get the byte stream from the saved property
				string sOrder = _resPropertyHolder.GetStringProp( Core.Props.UserResourceOrder );
				JetMemoryStream ms = new JetMemoryStream( Convert.FromBase64String( sOrder ), true );
				BinaryReader br = new BinaryReader( ms );
				int nCount = (int)(br.BaseStream.Length / 4);

				// Create a new cache, or reset an existing one to reuse it
				if (cache == null)
					cache = new IntHashSet();
				else
					cache.Clear();

				// Deserialize the integer IDs
				for (int a = 0; a < nCount; a++)
					cache.Add( br.ReadInt32() );
			}
			finally
			{
				_resPropertyHolder.UnLock();
			}
		}

		/// <summary>
		/// Reads the user sort order from the given resource, in the <b>reverse</b> order.
		/// </summary>
		/// <returns>A list of the resource IDs, in the <b>reverse</b> order.</returns>
		public IntArrayList ReadOrder()
		{
			_resPropertyHolder.Lock();
			try
			{
				// Foolproof checks
				if( _resPropertyHolder.IsDeleted )
					return new IntArrayList();
				if( !_resPropertyHolder.HasProp( Core.Props.UserResourceOrder ) )
					return new IntArrayList();

				// Get the byte stream from the saved property
				string sOrder = _resPropertyHolder.GetStringProp( Core.Props.UserResourceOrder );
				JetMemoryStream ms = new JetMemoryStream( Convert.FromBase64String( sOrder ), true );
				BinaryReader br = new BinaryReader( ms );

				// Deserialize the integer IDs
				int nCount = (int)(br.BaseStream.Length / 4);
				IntArrayList ar = new IntArrayList( nCount );
				for( int a = 0; a < nCount; a++ )
					ar.Add( br.ReadInt32() );
				return ar;
			}
			finally
			{
				_resPropertyHolder.UnLock();
			}
		}

		/// <summary>
		/// Resets the user sort order for the given resource and optionally recurses to its children,
		/// if the <paramref name="nParentLink"/> specifies a valid child link.
		/// </summary>
		/// <param name="nParentLink">A link between parent and children in the hierarchy to recurse into,
		/// or <c>0</c> to reset the order on this node only.</param>
		public void Reset( int nParentLink )
		{
			// Reset order for this node
			new ResourceProxy( _resPropertyHolder, _priority ).DeletePropAsync( _nPropertyId );

			// Reset order for its children recursively
			if( nParentLink != 0 )
			{
				foreach( IResource res in _resPropertyHolder.GetLinksTo( null, nParentLink ) )
					new UserResourceOrder( res ).Reset( nParentLink );
			}
		}

		#endregion

		#region Events

		/// <summary>
		/// An event that fires when the <see cref="PropertyId"/> of the <see cref="PropertyHolder"/> changes
		/// indicating that the ordering should be reapplied, or when <see cref="PropertyHolder"/> is deleted.
		/// </summary>
		public event EventHandler UserResourceOrderChanged;

		/// <summary>
		/// Safely fires the <see cref="UserResourceOrderChanged"/> event.
		/// </summary>
		protected void OnUserResourceOrderChanged( object sender, EventArgs args )
		{
			if( UserResourceOrderChanged != null )
			{
				try
				{ // Don't let the event-handler exceptions affect the outer code
					UserResourceOrderChanged( this, EventArgs.Empty );
				}
				catch( Exception ex )
				{
					Core.ReportException( ex, ExceptionReportFlags.AttachLog );
				}
			}
		}

		#endregion

		#region Implementation

		/// <summary>
		/// The holder has changed, if the change affects the order-property,
		/// </summary>
		protected void OnOrderPropertyHolderChanged( object sender, ResourcePropIndexEventArgs e )
		{
			if( e.ChangeSet.IsPropertyChanged( Core.Props.UserResourceOrder ) )
				OnUserResourceOrderChanged( this, EventArgs.Empty );
		}

		/// <summary>
		/// The resourse that holds the order has been deleted.
		/// </summary>
		protected void OnOrderPropertyHolderDeleting( object sender, ResourceIndexEventArgs e )
		{
			// The resource itself is not Nulled
			_resChangeListener = null;
			OnUserResourceOrderChanged( this, EventArgs.Empty );
		}

		#endregion
	}

	#endregion

	#region UserOrderSortSettings Class — SortSettings that support UserOrder

	/// <summary>
	/// Provides the combined sort-settings for a resource comparer that sorts against the user order
	/// when it's available, and by ordinary properties when it's not.
	/// </summary>
	public class UserOrderSortSettings : SortSettings
	{
		#region Data

		/// <summary>
		/// Identifies a sorting column in the <see cref="_sortProps"/> array before which the user-order sorting is applied.
		/// If greater or equal than the count, or negative, then the user-order sorting is applied after all the normal sorting properties.
		/// </summary>
		protected int _nApplyUserOrderBefore = 0;

		/// <summary>
		/// Defines the sort direction for the user sort order.
		/// <c>True</c> is ascending.
		/// </summary>
		protected bool _bUserOrderSortDirection = true;

		/// <summary>
		/// Contains and manages the resource that holds the user-order property.
		/// </summary>
		protected UserResourceOrder _userorder = null;

		#endregion

		#region Construction

		/// <summary>
		/// Creates the sort settings which describe an empty list sort.
		/// </summary>
		public UserOrderSortSettings()
			: base()
		{
		}

		/// <summary>
		/// Creates the sort settings with the specified list of sort property IDs and sort directions
		/// for each property ID.
		/// </summary>
		/// <param name="sortProps">The IDs of properties by which the resource list is sorted.</param>
		/// <param name="sortDirections">The sort direction for each property.</param>
		public UserOrderSortSettings( int[] sortProps, bool[] sortDirections )
			: base( sortProps, sortDirections )
		{
		}

		/// <summary>
		/// Creates the sort settings with the specified list of sort property IDs and the same direction
		/// for each sort property,
		/// </summary>
		/// <param name="sortProps">The IDs of properties by which the resource list is sorted.</param>
		/// <param name="sortAscending">The sort direction.</param>
		public UserOrderSortSettings( int[] sortProps, bool sortAscending )
			: base( sortProps, sortAscending )
		{
		}

		/// <summary>
		/// Creates the sort settings for sorting by a single property.
		/// </summary>
		/// <param name="sortProp">The ID of the property by which the resource list is sorted.</param>
		/// <param name="sortAsc">The direction of sorting.</param>
		public UserOrderSortSettings( int sortProp, bool sortAsc )
			: base( sortProp, sortAsc )
		{
		}

		/// <summary>
		/// A constructor that implements the <see cref="Parse"/> function behavior.
		/// </summary>
		/// <param name="resourceStore">The ResourceStore instance used to retrieve the property IDs.</param>
		/// <param name="sortProp">The string to parse.</param>
		protected UserOrderSortSettings( IResourceStore resourceStore, string sortProp )
			: base( resourceStore, sortProp )
		{
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets or sets the resource that holds a property that defines a sorting order on the resources by specifying their resource IDs in the descending order.
		/// </summary>
		public UserResourceOrder UserOrder
		{
			get { return _userorder; }
			set
			{
				lock( this )
				{
					if( value == _userorder )
						return;

					// Deinit old
					if( _userorder != null )
						_userorder.UserResourceOrderChanged -= new EventHandler( OnUserResourceOrderChanged );

					// Assign
					_userorder = value;

					// Init new
					if( _userorder != null )
						_userorder.UserResourceOrderChanged += new EventHandler( OnUserResourceOrderChanged );

					// Signal of the update
					if( UserOrderChanged != null )
						UserOrderChanged( this, EventArgs.Empty );
				}
			}
		}

		/// <summary>
		/// Gets or sets the sorting column in the <see cref="_sortProps"/> array before which the user-order sorting is applied.
		/// If greater or equal than the count, or negative, then the user-order sorting is applied after all the normal sorting properties.
		/// </summary>
		public int ApplyUserOrderBefore
		{
			get { return _nApplyUserOrderBefore; }
			set { _nApplyUserOrderBefore = value; }
		}

		/// <summary>
		/// Gets or sets the sort direction for the user sort order.
		/// <c>True</c> is ascending, <c>False</c> is descending.
		/// </summary>
		public bool UserOrderSortDirection
		{
			get { return _bUserOrderSortDirection; }
			set { _bUserOrderSortDirection = value; }
		}

		#endregion

		#region Operations

		/// <summary>
		/// Assigns a new user order by supplying the providing resource.
		/// </summary>
		public void SetUserOrder( IResource resHolder, int nPropId )
		{
			UserOrder = resHolder != null ? new UserResourceOrder( resHolder, nPropId ) : null;
		}

		/// <summary>
		/// Assigns a new user order by supplying the providing resource.
		/// </summary>
		public void SetUserOrder( IResource resHolder )
		{
			UserOrder = resHolder != null ? new UserResourceOrder( resHolder ) : null;
		}

		public static UserOrderSortSettings Parse( IResourceStore resourceStore, string sortProp )
		{
			return new UserOrderSortSettings( resourceStore, sortProp );
		}

		#endregion

		#region Events

		/// <summary>
		/// Signals that the user-order value has changed and should be re-cached by the listener.
		/// </summary>
		public event EventHandler UserOrderChanged;

		#endregion

		#region Implementation

		private void OnUserResourceOrderChanged( object sender, EventArgs e )
		{
			// Notify the external listeners of this change, so that they could update their cache
			if( UserOrderChanged != null )
				UserOrderChanged( this, EventArgs.Empty );
		}

		#endregion
	}

	#endregion

	#region UserOrderResourceComparer — IResourceComparer that supports UserOrder

	/// <summary>
	/// Implements comparing the resources against the user sort order when it's available and by ordinary property-sorting when it's not.
	/// </summary>
	public class UserOrderResourceComparer : IResourceComparer, IComparer
	{
		#region Data

		/// <summary>
		/// The sorting settings for this comparer, including per-property and user-order ones.
		/// </summary>
		protected UserOrderSortSettings _sortsettings = null;

		/// <summary>
		/// A helper array with the property types for sort settings.
		/// </summary>
		protected PropDataType[] _propTypes;

		/// <summary>
		/// Caches the user-order sorting settings for quick access.
		/// </summary>
		protected IntHashSet _hashUserOrder = null;

		#endregion

		#region Construction

		/// <summary>
		/// Initializes from a sort-settings, which include user-sort-order specification.
		/// </summary>
		/// <param name="sortSettings">Sorting settings (per-property + user-order).</param>
		public UserOrderResourceComparer( UserOrderSortSettings sortSettings )
		{
			// Store the sorting settings
			if( sortSettings == null )
				throw new ArgumentNullException( "sortSettings" );
			_sortsettings = sortSettings;

			// Process the user sorting order if it comes along with the sort settings
			UpdateUserOrder( null, null );
			sortSettings.UserOrderChanged += new EventHandler( UpdateUserOrder );

			// Collect the prop-types
			_propTypes = new PropDataType[_sortsettings.SortProps.Length];
			for( int a = 0; a < _sortsettings.SortProps.Length; a++ )
				_propTypes[ a ] = Core.ResourceStore.PropTypes[ _sortsettings.SortProps[ a ] ].DataType;
		}

		#endregion

		#region IResourceComparer Members

		public int CompareResources( IResource r1, IResource r2 )
		{
			int propEquals = 0;
			UserOrderSortSettings ss = _sortsettings;

			for( int i = 0; i < _sortsettings.SortProps.Length; i++ )
			{
				// Compare against the user sort order (if that should be done before this step)
				if( (_hashUserOrder != null)
					&& (ss != null)
					&& (ss.ApplyUserOrderBefore == i)
					&& ((propEquals = -_hashUserOrder.BucketComparer( r1.OriginalId, r2.OriginalId )) != 0) )
					return propEquals * (ss.UserOrderSortDirection ? 1 : -1);

				// Compare the current pair of properties
				int propId = _sortsettings.SortProps[ i ];
				if( propId == ResourceProps.Type )
				{
					propEquals = r1.Type.CompareTo( r2.Type );
				}
				else if( propId == ResourceProps.DisplayName )
				{
					propEquals = String.Compare( r1.DisplayName, r2.DisplayName, true, CultureInfo.CurrentCulture );
				}
				else if( propId == ResourceProps.Id )
				{
					propEquals = r1.Id - r2.Id;
				}
				else if( _propTypes[ i ] == PropDataType.Link )
				{
					propEquals = r1.GetPropText( propId ).CompareTo( r2.GetPropText( propId ) );
				}
				else
				{ // Compare the property values, checking for the Null case also
					IComparable cp;
					object prop1 = r1.GetProp( propId );
					object prop2 = r2.GetProp( propId );
					if( prop1 == null )
						propEquals = prop2 == null ? 0 : -1;
					else if( prop2 == null )
						propEquals = 1;
					else if( (cp = prop1 as IComparable) != null )
						propEquals = cp.CompareTo( prop2 );
				}
				if( propEquals != 0 )
					return propEquals * (_sortsettings.SortDirections[ i ] ? 1 : -1);
			}

			// If all the properties didn't rule out the sort order, check if the user order should be applied after all of them
			if( (_hashUserOrder != null)
				&& (ss != null)
				&& ((ss.ApplyUserOrderBefore < 0) || (ss.ApplyUserOrderBefore >= _sortsettings.SortProps.Length))
				&& ((propEquals = -_hashUserOrder.BucketComparer( r1.OriginalId, r2.OriginalId )) != 0) )
				return propEquals * (ss.UserOrderSortDirection ? 1 : -1);

			return 0;
		}

		#endregion

		#region IComparer Members

		public int Compare( object x, object y )
		{
			IResource r1 = x as IResource;
			IResource r2 = y as IResource;

			if( (r1 != null) && (r2 != null) )
				return CompareResources( r1, r2 );

			return 0;
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Porcesses the given user sorting order and precaches for faster access.
		/// </summary>
		protected void UpdateUserOrder( object sender, EventArgs e )
		{
			UserOrderSortSettings ss = _sortsettings;
			if( ss == null )
			{
				_hashUserOrder = null;
				return;
			}

			lock( ss )
			{
				// Erase the cache if no user sort order specified
				if( ss.UserOrder == null )
				{
					_hashUserOrder = null;
					return;
				}

				// Prepare the hash map that holds the resource IDs of the resources for which the sort order is set.
				// Due to IntHashSet implementation, two IDs picked out from the hash set can be compared by the order in which they were added, which is right the reverse order of the resources on that level.
				ss.UserOrder.ReadOrder( ref _hashUserOrder );
			}
		}

		#endregion
	}

	#endregion

	#region ResetUserOrderAction Class — Resets the user-order on the given resource tree node and all of its descendants

	public class ResetUserOrderAction : IAction
	{
		#region Data

		/// <summary>
		/// ID of the property that holds the user-resource-order.
		/// </summary>
		private int _propResourceOrder = Core.Props.UserResourceOrder;

		/// <summary>
		/// ID of the link type that is a parent link for this hierarchy.
		/// <c>0</c> for non-recursove processing.
		/// </summary>
		private int _linkParent = Core.Props.Parent;

		#endregion

		#region IAction Members

		public void Execute( IActionContext context )
		{
			foreach( IResource res in context.SelectedResources )
				new UserResourceOrder( res, ResourceOrderProperty ).Reset( ParentLink );
		}

		public void Update( IActionContext context, ref ActionPresentation presentation )
		{
			bool bHasOrder = false;
			foreach( IResource res in context.SelectedResources )
			{
				if( new UserResourceOrder( res, ResourceOrderProperty ).HasOrder )
				{
					bHasOrder = true;
					break;
				}
			}
			presentation.Visible = bHasOrder;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets or sets the ID of the property that holds the user-resource-order.
		/// </summary>
		public int ResourceOrderProperty
		{
			get { return _propResourceOrder; }
			set { _propResourceOrder = value; }
		}

		/// <summary>
		/// Gets or sets the ID of the link type that is a parent link for this hierarchy.
		/// <c>0</c> for non-recursove processing.
		/// </summary>
		public int ParentLink
		{
			get { return _linkParent; }
			set { _linkParent = value; }
		}

		#endregion
	}

	#endregion
}
