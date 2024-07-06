// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// A composition of several links pane filters.
	/// </summary>
	internal class CompositeLinksPaneFilter: ILinksPaneFilter
	{
		private ArrayList _baseFilters;

        public CompositeLinksPaneFilter( params ILinksPaneFilter[] filters )
		{
            _baseFilters = new ArrayList( filters );
		}

        public void AddBaseFilter( ILinksPaneFilter filter )
        {
            _baseFilters.Add( filter );
        }

	    public bool AcceptLinkType( IResource displayedResource, int propId, ref string displayName )
	    {
	        foreach( ILinksPaneFilter filter in _baseFilters )
	        {
	            if ( !filter.AcceptLinkType( displayedResource, propId, ref displayName ) )
                    return false;
	        }
            return true;
	    }

	    public bool AcceptLink( IResource displayedResource, int propId, IResource targetResource,
            ref string linkTooltip )
	    {
	        foreach( ILinksPaneFilter filter in _baseFilters )
	        {
	            if ( !filter.AcceptLink( displayedResource, propId, targetResource, ref linkTooltip ) )
                    return false;
	        }
            return true;
	    }

	    public bool AcceptAction( IResource displayedResource, IAction action )
	    {
            foreach( ILinksPaneFilter filter in _baseFilters )
            {
                if ( !filter.AcceptAction( displayedResource, action ) )
                    return false;
            }
            return true;
        }
	}
}
