// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Text;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Describes the way a resource list is sorted.
    /// </summary>
    /// <since>2.0</since>
    public class SortSettings
    {
        private int[] _sortProps;
        private bool[] _sortDirections;

    	/// <summary>
        /// Creates the sort settings which describe an empty list sort.
        /// </summary>
        public SortSettings()
        {
            _sortProps = new int[] {};
            _sortDirections = new bool[] {};
        }

        /// <summary>
        /// Creates the sort settings with the specified list of sort property IDs and sort directions
        /// for each property ID.
        /// </summary>
        /// <param name="sortProps">The IDs of properties by which the resource list is sorted.</param>
        /// <param name="sortDirections">The sort direction for each property.</param>
        public SortSettings( int[] sortProps, bool[] sortDirections )
        {
            if ( sortProps.Length != sortDirections.Length )
            {
                throw new ArgumentException( "The length of the property IDs array must be equal to the length of the sort directions array." );
            }
            _sortProps = sortProps;
            _sortDirections = sortDirections;
        }

        /// <summary>
        /// Creates the sort settings with the specified list of sort property IDs and the same direction
        /// for each sort property,
        /// </summary>
        /// <param name="sortProps">The IDs of properties by which the resource list is sorted.</param>
        /// <param name="sortAscending">The sort direction.</param>
        public SortSettings( int[] sortProps, bool sortAscending )
        {
            _sortProps = sortProps;
            _sortDirections = new bool [sortProps.Length];
            for( int i=0; i<_sortDirections.Length; i++ )
            {
                _sortDirections [i] = sortAscending;
            }
        }

        /// <summary>
        /// Creates the sort settings for sorting by a single property.
        /// </summary>
        /// <param name="sortProp">The ID of the property by which the resource list is sorted.</param>
        /// <param name="sortAsc">The direction of sorting.</param>
        public SortSettings( int sortProp, bool sortAsc )
        {
            _sortProps = new int[] { sortProp };
            _sortDirections = new bool[] { sortAsc };
        }

		/// <summary>
		/// A constructor that implements the <see cref="Parse"/> function behavior.
		/// </summary>
		/// <param name="resourceStore">The ResourceStore instance used to retrieve the property IDs.</param>
		/// <param name="sortProp">The string to parse.</param>
		protected SortSettings(IResourceStore resourceStore, string sortProp)
		{
			ArrayList propNameArray = new ArrayList( sortProp.Split( ' ' ) );

			// remove multiple spaces
			for( int i=propNameArray.Count-1; i >= 0; i-- )
			{
				if ( ((string) propNameArray [i]).Trim() == "" )
					propNameArray.RemoveAt( i );
			}

			_sortProps = new int [propNameArray.Count];
			_sortDirections = new bool [propNameArray.Count];
			for( int i=0; i<propNameArray.Count; i++ )
			{
				string propName = (string) propNameArray [i];

				if ( propName.EndsWith( "-" ) )
				{
					propName = propName.Substring( 0, propName.Length-1 );
					_sortDirections [i] = false;
				}
				else
				{
					_sortDirections [i] = true;
				}

				if ( propName.Equals( "Type" ) )
				{
					_sortProps [i] = ResourceProps.Type;
				}
				else if ( propName.Equals( "DisplayName" ) )
				{
					_sortProps [i] = ResourceProps.DisplayName;
				}
				else if ( propName.Equals( "ID" ) )
				{
					_sortProps [i] = ResourceProps.Id;
				}
				else
				{
					_sortProps [i] = resourceStore.GetPropId( propName );
				}
			}
		}

        /// <summary>
        /// Returns the array of property IDs by which the list is sorted.
        /// </summary>
        public int[] SortProps
        {
            get { return _sortProps; }
        }

        /// <summary>
        /// Returns the array of sort directions for each property by which the list is sorted.
        /// <c>True</c> stands for ascending.
        /// </summary>
        public bool[] SortDirections
        {
            get { return _sortDirections; }
        }

        /// <summary>
        /// Returns true if the first column used to sort the list has ascending sort order.
        /// </summary>
        public bool SortAscending
        {
            get { return _sortProps.Length > 0 && _sortDirections [0]; }
        }

    	/// <summary>
        /// Parses the specified string containing a space-separated list of property names.
        /// </summary>
        /// <param name="resourceStore">The ResourceStore instance used to retrieve the property IDs.</param>
        /// <param name="sortProp">The string to parse.</param>
        /// <returns>The sort settings.</returns>
        public static SortSettings Parse( IResourceStore resourceStore, string sortProp )
    	{
    		return new SortSettings(resourceStore, sortProp);
    	}

        /// <summary>
        /// Returns the space-separated list of property names by which the list is sorted.
        /// </summary>
        /// <param name="resourceStore">The ResourceStore instance used to retrieve the property names.</param>
        /// <returns>The property name list.</returns>
        public string ToString( IResourceStore resourceStore )
        {
            StringBuilder result = new StringBuilder();
            foreach( int propId in _sortProps )
            {
                if ( result.Length > 0 )
                {
                    result.Append( " " );
                }
                result.Append( resourceStore.PropTypes [propId].DisplayName );
            }
            return result.ToString();
        }

        /// <summary>
        /// Returns a copy of the sort settings with sort directions for all columns reversed.
        /// </summary>
        /// <returns>The reversed sort settings.</returns>
        public SortSettings Reverse()
        {
            int[] sortProps = (int[]) _sortProps.Clone();
            bool[] sortDirections = new bool[_sortDirections.Length];
            for( int i=0; i<sortDirections.Length; i++ )
            {
                sortDirections [i] = !_sortDirections [i];
            }
            return new SortSettings( sortProps, sortDirections );
		}
	}
}
