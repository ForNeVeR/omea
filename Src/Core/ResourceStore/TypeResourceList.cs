// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.ResourceStore
{
    /**
     * The predicate which matches resources of a specific resource type.
     */

    internal class ResourceTypePredicate: ResourceListPredicate
    {
        private int _resTypeId;

        internal ResourceTypePredicate( string type )
        {
            _resTypeId = MyPalStorage.Storage.ResourceTypes [type].Id;
        }

        internal ResourceTypePredicate( int resTypeId )
        {
            _resTypeId = resTypeId;
        }

        internal override IntArrayList GetMatchingResources( out bool sortedById )
        {
            sortedById = false;
            return GetResourcesFromResultSet( MyPalStorage.Storage.SelectAllResources( _resTypeId ), 0 );
        }

        internal override int GetKnownType()
        {
            return _resTypeId;
        }

        internal override bool HasTypePredicate( int typeId )
        {
            return _resTypeId == typeId;
        }

        internal override bool HasAnyTypePredicate()
        {
            return true;
        }

        internal override PredicateMatch MatchResource( IResource res, IPropertyChangeSet cs )
        {
            if ( res.TypeId == _resTypeId )
            {
                return (cs != null && ( cs.IsNewResource || cs.IsPropertyChanged( ResourceProps.Type ) ) )
                    ? PredicateMatch.Add
                    : PredicateMatch.Match;
            }
            else if ( cs != null && cs.IsPropertyChanged( ResourceProps.Type ) &&
                (int) cs.GetOldValue( ResourceProps.Type ) == _resTypeId )
            {
                return PredicateMatch.Del;
            }
            return PredicateMatch.None;
        }

        public override string ToString()
        {
            return "Type(" + MyPalStorage.Storage.ResourceTypes [_resTypeId].Name + ")";
        }

        internal override int GetSelectionCost()
        {
            return 5;
        }

        public override bool Equals( object obj )
        {
            if ( Object.ReferenceEquals( this, obj) )
                return true;

            ResourceTypePredicate rhs = obj as ResourceTypePredicate;
            if ( rhs == null )
                return false;

            return _resTypeId == rhs._resTypeId;
        }

        public override int GetHashCode()
        {
            return _resTypeId;
        }
    }
}
