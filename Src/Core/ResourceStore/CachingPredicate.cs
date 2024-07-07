// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceStore
{
	/// <summary>
	/// A predicate which takes its matching resources from a cached live resource list.
	/// </summary>
	internal class CachingPredicate: ResourceListPredicate
	{
	    private ResourceList _cacheList;

	    public CachingPredicate( ResourceList cacheList )
	    {
	        _cacheList = cacheList;
            _cacheList.Sort( new int[] { ResourceProps.Id }, true );
	    }

	    internal override IntArrayList GetMatchingResources( out bool sortedById )
	    {
	        sortedById = true;
            lock( _cacheList )
	        {
                return (IntArrayList) _cacheList.ResourceIdArray.Clone();
            }
	    }

        internal override IntArrayList GetSortedMatchingResourcesRef( out object syncObject )
        {
            syncObject = _cacheList;
            return _cacheList.ResourceIdArray;
        }

	    internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
	    {
            return _cacheList.Predicate.MatchResource( res, cs );
	    }

	    internal override int GetSelectionCost()
	    {
	        return 0;
	    }

	    internal override int GetKnownType()
	    {
	        return _cacheList.GetPredicateKnownType();
	    }

	    public override string ToString()
	    {
	        return "Cache(" + _cacheList.Predicate.ToString() + ")";
	    }
	}
}
