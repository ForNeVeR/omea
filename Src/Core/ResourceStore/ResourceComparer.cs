// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Globalization;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceStore
{
    public class ResourceComparer: IComparer
    {
		/// <summary>
		/// Queried for a property provider when comparing its resources.
		/// </summary>
        protected ResourceList _owner;
        protected static SortSettings _emptySortSettings = new SortSettings();
        protected SortSettings _sortSettings;
        protected PropDataType[]   _propTypes;
        protected static PropDataType[] _emptyPropTypes = new PropDataType[] {};
        protected bool         _propsEquivalent;
        protected bool         _customSortAscending;
        protected IResourceComparer _customComparer;

		/// <summary>
		/// Constructs a comparer that uses a hierarchical set of properties to compare by.
		/// </summary>
		/// <param name="owner">A resource list that has a property provider
		/// that will be queried to get the property values for individual resources.</param>
		/// <param name="sortSettings">Deinfes the sorting columns and ascending/descending directions for them.</param>
		/// <param name="propsEquivalent">Specifies whether all the properties are treated equivalent,
		/// which means that if two sorting properties are suggested, the first of them which is defined for that particular object
		/// will take part in the comparison, not necessarily the same props on both objects.</param>
        public ResourceComparer( IResourceList owner, SortSettings sortSettings, bool propsEquivalent )
        {
			if(sortSettings == null)
				throw new ArgumentNullException("sortSettings");
            _owner          = (ResourceList) owner;
            _sortSettings   = sortSettings;
            _propTypes      = new PropDataType [_sortSettings.SortProps.Length];

            for( int i=0; i<_sortSettings.SortProps.Length; i++ )
            {
                _propTypes [i] = GetSortPropType( _sortSettings.SortProps [i] );
            }
            _propsEquivalent = propsEquivalent;
        }

		/// <summary>
		/// Allows to delegate all the comparison operations to a custom resource comparer.
		/// </summary>
		/// <param name="owner">Reserved. This value is ignored when sorting against a custom comparer.</param>
		/// <param name="customComparer">The custom comparer that handles all the comparison operations for this object.
		/// Property IDs are converted to resources before getting into the custom comparer.</param>
		/// <param name="dontReverse"><c>True</c> if you would like to accept the sorting direction of the custom comparer,
		/// and <c>False</c> if you'd like to reverse it.</param>
    	public ResourceComparer( IResourceList owner, IResourceComparer customComparer, bool dontReverse )
        {
            _owner = (ResourceList) owner;
            _sortSettings = _emptySortSettings;
            _propTypes = _emptyPropTypes;
            _customComparer = customComparer;
            _customSortAscending = dontReverse;
        }

		/// <summary>
		/// A helper function that returnes a property type of the given property ID.
		/// </summary>
        protected PropDataType GetSortPropType( int propID )
        {
            switch( propID )
            {
                case ResourceProps.Type:        return PropDataType.String;
                case ResourceProps.DisplayName: return PropDataType.String;
                case ResourceProps.Id:          return PropDataType.Int;
                default:
                    return MyPalStorage.Storage.GetPropDataType( propID );
            }
        }

		/// <summary>
		/// Sort settings.
		/// </summary>
        public SortSettings SortSettings
        {
            get { return _sortSettings; }
        }

		/// <summary>
		/// Gets or sets whether all the properties supplied for sorting are equivalent, and sorting can be performed against any of them that are defined on the resource.
		/// </summary>
        public bool PropsEquivalent
        {
            get { return _propsEquivalent; }
            set { _propsEquivalent = value; }
        }

		/// <summary>
		/// Checks whether this comparer is sorting the resources only by their IDs in ascending order.
		/// </summary>
        internal bool IsIdSort()
        {
            return _sortSettings.SortProps.Length == 1
				&& _sortSettings.SortProps [0] == ResourceProps.Id
				&& _sortSettings.SortDirections [0];
        }

		/// <summary>
		/// A comparer that can compare both IResource instances and resource IDs.
		/// </summary>
        int IComparer.Compare( object x, object y )
        {
            IResource r1 = x as IResource;
            IResource r2;
            if ( r1 == null )
            {
                int id1 = (int) x;
                int id2 = (int) y;
                if ( id1 == id2 )
                    return 0;

                r1 = MyPalStorage.Storage.LoadResource( id1 );
                r2 = MyPalStorage.Storage.LoadResource( id2 );
            }
            else
            {
                r2 = (IResource) y;
            }

            if ( x == y )
                return 0;

            if ( _customComparer != null )
            {
                return _customComparer.CompareResources( r1, r2 ) * (_customSortAscending ? 1 : -1);
            }
            if ( _propsEquivalent )
            {
                return CompareResourcesEquivalent( r1, r2 );
            }
            return CompareResources( r1, r2 );
        }

		/// <summary>
		/// Compares resources where properties are hierarchical (when sorting by P1 and P2,
		/// if a resource doesn't have the value for P1, it is considered smaller than any
		/// resource which has a value for P1).
		/// </summary>
		public int CompareResources( IResource r1, IResource r2 )
        {
	        int propEquals = 0;

            for( int i=0; i<_sortSettings.SortProps.Length; i++ )
            {
				// Compare the current pair of properties
                int propId = _sortSettings.SortProps [i];
                if ( propId == ResourceProps.Type )
                {
                    propEquals = r1.Type.CompareTo( r2.Type );
                }
                else if ( propId == ResourceProps.DisplayName )
                {
                    propEquals = String.Compare( r1.DisplayName, r2.DisplayName, true, CultureInfo.CurrentCulture );
                }
                else if ( propId == ResourceProps.Id )
                {
                    propEquals = r1.Id - r2.Id;
                }
                else if ( _propTypes [i] == PropDataType.Link )
                {
                    propEquals = r1.GetPropText( propId ).CompareTo( r2.GetPropText( propId ) );
                }
                else
                {
                    object prop1 = (_owner != null) ? _owner.GetPropValue( r1, propId ) : r1.GetProp( propId );
                    object prop2 = (_owner != null) ? _owner.GetPropValue( r2, propId ) : r2.GetProp( propId );
                    if ( prop1 != null || prop2 != null )
                    {
                        propEquals = CompareProps( prop1, prop2 );
                    }
                }
                if ( propEquals != 0 )
                    return propEquals * (_sortSettings.SortDirections [i] ? 1 : -1);
            }

            return 0;
        }

		/// <summary>
		/// Compares resources where the values of all properties are equivalent
		/// (when sorting by P1 and P2, if a resource doesn't have the value for P1,
		/// the value of P2 is taken and compared with other resources' values of P1.
		/// </summary>
        public int CompareResourcesEquivalent( IResource r1, IResource r2 )
        {
            object prop1 = GetAnySortProperty( r1 );
            object prop2 = GetAnySortProperty( r2 );
            if ( prop1 == null && prop2 == null )
            {
                return 0;
            }
            return CompareProps( prop1, prop2 ) * (_sortSettings.SortAscending ? 1 : -1);
        }

		/// <summary>
		/// Returns the first available property from the list of sort properties
		/// for the specified resource.
		/// </summary>
        protected object GetAnySortProperty( IResource res )
        {
            for( int i=0; i<_sortSettings.SortProps.Length; i++ )
            {
                int propId = _sortSettings.SortProps [i];
                if ( propId == ResourceProps.Type )
                {
                    return res.Type;
                }
                else if ( propId == ResourceProps.DisplayName )
                {
                    return res.DisplayName;
                }
                else if ( propId == ResourceProps.Id )
                {
                    return res.Id;
                }
                else if ( _propTypes [i] == PropDataType.Link )
                {
                    if ( res.HasProp( propId ) )
                    {
                        return res.GetPropText( propId );
                    }
                }
                else
                {
                    object propValue = (_owner != null) ? _owner.GetPropValue( res, propId ) : res.GetProp( propId );
                    if ( propValue != null )
                    {
                        return propValue;
                    }
                }
            }
            return null;
        }

		/// <summary>
		/// Compares two generic object values.
		/// If either is null, it's smaller.
		/// If the first prop implements IComparable, it's used to compare it to the second one.
		/// Otherwise, the values are considered to be equal.
		/// </summary>
        protected int CompareProps( object prop1, object prop2 )
        {
            if ( prop1 != null && prop2 == null )
                return 1;

            if ( prop1 == null && prop2 != null )
                return -1;

            IComparable prop1Comparable = prop1 as IComparable;
            if ( prop1Comparable == null )
                return 0;

            int cmp = prop1Comparable.CompareTo( prop2 );
            if ( cmp != 0 )
                return cmp;

            return 0;
        }
    }
}
