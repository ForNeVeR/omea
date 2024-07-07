// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text;

namespace JetBrains.Omea.ResourceStore
{
    /**
     * A predicate based on several source predicates.
     */

    internal abstract class ResourceListDerivedPredicate: ResourceListPredicate
	{
        protected ResourceListPredicate[] _sourcePredicates;

        protected ResourceListDerivedPredicate( params ResourceListPredicate[] predicates )
        {
            _sourcePredicates = predicates;
        }

        protected ResourceListPredicate[] AddPredicate( ResourceListPredicate[] list, ResourceListPredicate pred )
        {
            int len = list.Length;
            ResourceListPredicate[] newList = new ResourceListPredicate [len+1];
            Array.Copy( list, 0, newList, 0, len );
            newList [len] = pred;
            return newList;
        }

        protected abstract string GetDerivationName();

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder( GetDerivationName() );
            builder.Append( "(" );
            for( int i=0; i<_sourcePredicates.Length; i++ )
            {
                if ( i > 0 )
                {
                    builder.Append( "," );
                }
                builder.Append( _sourcePredicates [i].ToString() );
            }
            builder.Append( ")" );
            return builder.ToString();
        }

        public override bool Equals( object obj )
        {
            if ( Object.ReferenceEquals( this, obj ) )
                return true;

            if ( GetType() != obj.GetType() )
                return false;

            ResourceListDerivedPredicate rhs = obj as ResourceListDerivedPredicate;
            if ( rhs == null )
                return false;
            if ( rhs._sourcePredicates.Length != _sourcePredicates.Length )
                return false;

            // there is no guarantee that the source predicates have the same sort order,
            // so we use O(N^2) compare - fortunately the N is quite small...
            for( int i=0; i<_sourcePredicates.Length; i++ )
            {
                if ( !rhs.ContainsPredicate( _sourcePredicates [i] ) )
                    return false;
            }
            return true;
        }

        /**
         * Checks if one of the predicates in the intersection is equal to the
         * specified predicate.
         */

        internal bool ContainsPredicate( ResourceListPredicate pred )
        {
            for( int i=0; i<_sourcePredicates.Length; i++ )
            {
                if ( _sourcePredicates [i].Equals( pred ) )
                    return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hc = 0;
            for( int i=0; i<_sourcePredicates.Length; i++ )
            {
                hc ^= _sourcePredicates [i].GetHashCode();
            }
            return hc;
        }

        protected ResourceListPredicate OptimizeSourcePredicates( ResourceListPredicate result, bool isLive )
        {
            if ( result == this )
            {
                for( int i=0; i<_sourcePredicates.Length; i++ )
                {
                    _sourcePredicates [i] = _sourcePredicates [i].Optimize( isLive );
                }
            }
            else
            {
                result = result.Optimize( isLive );
            }

            ResourceListPredicate predicate = MyPalStorage.Storage.GetCachedPredicate( result );
            if ( predicate != null )
            {
                return predicate;
            }
            return result;
        }
    }
}
