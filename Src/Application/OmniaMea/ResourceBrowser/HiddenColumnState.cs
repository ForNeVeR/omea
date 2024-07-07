// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Stores the information about columns hidden from ResourceBrowser because of
	/// ShowIfNotEmpty and ShowIfDistinct flags.
	/// </summary>
	internal class HiddenColumnState
	{
	    private ArrayList _mayBeEmptyColumns;
	    private ArrayList _mayBeEmptyPropIds;
	    private ArrayList _distinctColumns;
	    private ArrayList _distinctPropIds;
	    private IntHashTable _distinctValueMap;

	    public int HiddenColumnCount
        {
            get { return _mayBeEmptyColumns.Count; }
        }

        /// <summary>
        /// If the array of column descriptors has any "show if not empty" or "show if distinct" columns,
        /// removes the columns which are empty or non-distinct from the column array.
        /// </summary>
        /// <param name="columns">The array of columns to filter.</param>
        /// <param name="resList">The resource list by which the filtering is performed.</param>
        /// <returns>Filtered array of columns.</returns>
        internal ColumnDescriptor[] HideEmptyColumns( ColumnDescriptor[] columns, IResourceList resList )
        {
	        _mayBeEmptyColumns = new ArrayList();
	        _mayBeEmptyPropIds = new ArrayList();
            _distinctColumns = new ArrayList();
            _distinctPropIds = new ArrayList();
            _distinctValueMap = new IntHashTable();

            foreach( ColumnDescriptor colDesc in columns )
            {
                if ( (colDesc.Flags & ColumnDescriptorFlags.ShowIfNotEmpty ) != 0 )
                {
                    _mayBeEmptyColumns.Add( colDesc );
                    _mayBeEmptyPropIds.Add( ((DisplayColumnManager) Core.DisplayColumnManager).PropNamesToIDs( colDesc.PropNames, true ) );
                }
                else if ((colDesc.Flags & ColumnDescriptorFlags.ShowIfDistinct) != 0 )
                {
                    _distinctColumns.Add( colDesc );
                    _distinctPropIds.Add( ((DisplayColumnManager) Core.DisplayColumnManager).PropNamesToIDs( colDesc.PropNames, true ) );
                }
            }
            if ( _mayBeEmptyColumns.Count == 0 && _distinctColumns.Count == 0 )
            {
                return columns;
            }

            lock( resList )
            {
                foreach( IResource res in resList.ValidResources )
                {
                    for( int i=_mayBeEmptyColumns.Count-1; i >= 0; i-- )
                    {
                        if ( !IsColumnEmpty( resList, res, i ) )
                        {
                            _mayBeEmptyPropIds.RemoveAt( i );
                            _mayBeEmptyColumns.RemoveAt( i );
                        }
                    }

                    for( int i=_distinctColumns.Count-1; i >= 0; i-- )
                    {
                        if ( !ValueMatchesDistinctColumn( res, i ) )
                        {
                            _distinctPropIds.RemoveAt( i );
                            _distinctColumns.RemoveAt( i );
                        }
                    }

                    if ( _mayBeEmptyColumns.Count == 0 && _distinctColumns.Count == 0 )
                    {
                        return columns;
                    }
                }
            }

            ColumnDescriptor[] result = new ColumnDescriptor[ columns.Length - _mayBeEmptyColumns.Count - _distinctColumns.Count ];
            int destIndex=0;
            for( int i=0; i<columns.Length; i++ )
            {
                if ( !_mayBeEmptyColumns.Contains( columns [i] ) && !_distinctColumns.Contains( columns [i] ) )
                {
                    result [destIndex++] = columns [i];
                }
            }
            return result;
        }

        private bool IsColumnEmpty( IResourceList resList, IResource res, int index )
        {
            int[] propIds = (int[]) _mayBeEmptyPropIds [index];
            for( int j=0; j<propIds.Length; j++ )
            {
                if ( resList.HasProp( res, propIds [j] ) )
                {
                    return false;
                }
            }
            return true;
        }

        private bool ValueMatchesDistinctColumn( IResource res, int index )
        {
            int[] propIds = (int[]) _distinctPropIds [index];
            for( int j=0; j<propIds.Length; j++ )
            {
                string oldText = (string) _distinctValueMap [propIds [j]];
                string propText = res.GetPropText( propIds [j] );
                if ( oldText != null && propText != oldText )
                {
                    return false;
                }
                _distinctValueMap [propIds [j]] = propText;
            }
            return true;
        }

	    public bool HiddenColumnsChanged( IResource res, IResourceList resList )
	    {
            for( int i=0; i<_mayBeEmptyPropIds.Count; i++ )
            {
                if ( !IsColumnEmpty( resList, res, i ) )
                {
                    return true;
                }
            }

            for( int i=0; i<_distinctPropIds.Count; i++ )
            {
                if ( !ValueMatchesDistinctColumn( res, i ) )
                {
                    return true;
                }
            }

            return false;
	    }
	}
}
