/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.TextIndex
{
    public class SearchQueryExtensions : ISearchQueryExtensions
    {
        private HashMap _prep2ResTypeExtensions = new HashMap();
        private HashMap _prep2TokenExtensions = new HashMap();
        private HashMap _prep2FreestyleExtensions = new HashMap();

        public void  RegisterResourceTypeRestriction( string prep, string displayType, string resType )
        {
            #region Preconditions
            if( !Utils.IsValidString( prep ))
                throw new ArgumentNullException( "SearchQueryExtensions -- Preposition is NULL or empty.");
            if( !Utils.IsValidString( displayType ))
                throw new ArgumentNullException( "SearchQueryExtensions -- Displayable name for a resource type is NULL or empty.");
            if( !Utils.IsValidString( resType ))
                throw new ArgumentNullException( "SearchQueryExtensions -- resource type name is NULL or empty.");
            #endregion Preconditions

            HashMap.Entry e = _prep2ResTypeExtensions.GetEntry( prep );
            if( e == null )
            {
                ArrayList list = new ArrayList();
                list.Add( new Pair( displayType, resType ) );
                _prep2ResTypeExtensions[ prep ] = list;
            }
            else
            {
                Debug.Assert( e.Value is ArrayList );
                ((ArrayList)e.Value).Add( new Pair( displayType, resType ));
            }
        }

        public void  RegisterSingleTokenRestriction ( string prep, string token, IResource stdCondition )
        {
            #region Preconditions
            if( !Utils.IsValidString( prep ))
                throw new ArgumentNullException( "SearchQueryExtensions -- Preposition is NULL or empty.");
            if( !Utils.IsValidString( token ))
                throw new ArgumentNullException( "SearchQueryExtensions -- Anchor token for a resource type is NULL or empty.");
            if( stdCondition == null )
                throw new ArgumentNullException( "SearchQueryExtensions -- Mapped condition is null.");
            #endregion Preconditions
            
            HashMap.Entry e = _prep2TokenExtensions.GetEntry( prep );
            if( e == null )
            {
                ArrayList list = new ArrayList();
                list.Add( new Pair( token, stdCondition ) );
                _prep2TokenExtensions[ prep ] = list;
            }
            else
            {
                Debug.Assert( e.Value is ArrayList );
                ((ArrayList)e.Value).Add( new Pair( token, stdCondition ));
            }
        }
        
        public void  RegisterFreestyleRestriction ( string prep, IQueryTokenMatcher matcher )
        {
            #region Preconditions
            if( !Utils.IsValidString( prep ))
                throw new ArgumentNullException( "SearchQueryExtensions -- Preposition is NULL or empty.");
            if( matcher == null )
                throw new ArgumentNullException( "SearchQueryExtensions -- Token stream matcher handler is NULL.");
            #endregion Preconditions
            
            HashMap.Entry e = _prep2FreestyleExtensions.GetEntry( prep );
            if( e == null )
            {
                ArrayList list = new ArrayList();
                list.Add( matcher );
                _prep2FreestyleExtensions[ prep ] = list;
            }
            else
            {
                Debug.Assert( e.Value is ArrayList );
                ((ArrayList)e.Value).Add( matcher );
            }
        }

        public string  GetResourceTypeRestriction( string prep, string anchor )
        {
            ArrayList pairs = (ArrayList)_prep2ResTypeExtensions[ prep ];

            if( pairs != null )
            {
                foreach( Pair p in pairs )
                {
                    if( p.First.Equals( anchor ) )
                        return (string)p.Second;
                }
            }
            return null;
        }

        public IResource  GetSingleTokenRestriction( string prep, string anchor )
        {
            ArrayList pairs = (ArrayList)_prep2TokenExtensions[ prep ];

            if( pairs != null )
            {
                foreach( Pair p in pairs )
                {
                    if( p.First.Equals( anchor ) )
                        return (IResource) p.Second;
                }
            }
            return null;
        }

        public IResource GetMatchingFreestyleRestriction( string prep, string stream )
        {
            ArrayList matchers = (ArrayList)_prep2FreestyleExtensions[ prep ];

            if( matchers != null )
            {
                foreach( IQueryTokenMatcher matcher in matchers )
                {
                    IResource condition = matcher.ParseTokenStream( stream );
                    if( condition != null )
                        return condition;
                }
            }
            return null;
        }

        public string[]  GetAllAnchors()
        {
            ArrayList preps = new ArrayList();

            GetAnchors( _prep2ResTypeExtensions, preps );
            GetAnchors( _prep2TokenExtensions, preps );
            GetAnchors( _prep2FreestyleExtensions, preps );

            return (string[]) preps.ToArray( typeof(string) );
        }

        private void  GetAnchors( HashMap map, ArrayList list )
        {
            foreach( HashMap.Entry e in map )
            {
                if( !list.Contains( e.Key ) )
                    list.Add( e.Key );
            }
        }
    }
}