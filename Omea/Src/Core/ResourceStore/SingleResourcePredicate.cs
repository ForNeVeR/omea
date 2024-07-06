// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceStore
{
	/// <summary>
	/// A predicate which holds a strong reference to a single resource.
	/// </summary>
	internal class SingleResourcePredicate: ResourceListPredicate
	{
        private IResource _resource;

	    public SingleResourcePredicate( IResource resource )
	    {
	        _resource = resource;
	    }

	    internal override IntArrayList GetMatchingResources( out bool sortedById )
	    {
	        sortedById = true;
            IntArrayList result = new IntArrayList( 1 );
            result.Add( _resource.Id );
            return result;
	    }

	    internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
	    {
	        return (_resource.Id == res.Id) ? PredicateMatch.Match : PredicateMatch.None;
	    }

	    internal override int GetSelectionCost()
	    {
	        return 0;
	    }

	    public override string ToString()
	    {
	        return "Resource(" + _resource.Id + ")";
	    }

	    internal override int GetKnownType()
	    {
	        return base.GetKnownType();
	    }

	    public override bool Equals( object obj )
	    {
            if ( Object.ReferenceEquals( this, obj ) )
                return true;

            SingleResourcePredicate rhs = obj as SingleResourcePredicate;
            if ( rhs == null )
                return false;

            return rhs._resource.Id == _resource.Id;
        }

	    public override int GetHashCode()
	    {
	        return _resource.Id;
	    }
	}
}
