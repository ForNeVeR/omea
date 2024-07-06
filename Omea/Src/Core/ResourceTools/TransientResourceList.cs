// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using JetBrains.Omea.ResourceStore;
using System.Collections;

namespace JetBrains.Omea.ResourceTools
{
    /**
     * A resource list which deletes all resources it contains on dispose.
     */

    public class TransientResourceList: ResourceList
	{
        private ArrayList _resourceList = new ArrayList();

        public TransientResourceList()
            : base( new PlainListPredicate( new IntArrayList() ), false )
		{
		}

        public new void Add( IResource res )
        {
            // ResourceStore stores transient resources in a weak list, so we need to keep
            // strong references to avoid resources being collected while the list is alive
            _resourceList.Add( res );
            if ( IsInstantiated )
            {
                base.Add( res );
            }
            (Predicate as PlainListPredicate).Add( res.Id );
        }
	}
}
