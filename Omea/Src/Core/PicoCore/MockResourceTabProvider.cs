// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.PicoCore
{
	/// <summary>
	/// Mock implementation of IResourceTabProvider for UnreadManager testing.
	/// </summary>
	public class MockResourceTabProvider: IResourceTabProvider
	{
	    private Hashtable _tabMap = new Hashtable();

        public void SetResourceTab( string resType, string tab )
        {
            _tabMap [resType] = tab;
        }

        public string GetDefaultTab()
	    {
	        return "";
	    }

	    public string GetResourceTab( IResource res )
	    {
            return (string) _tabMap [res.Type];
	    }

	    public IResourceList GetTabFilterList( string tabId )
	    {
            IResourceList result = null;
            foreach( DictionaryEntry de in _tabMap )
            {
                if ( (string) de.Value == tabId )
                {
                    result = Core.ResourceStore.GetAllResources( (string) de.Key ).Union( result );
                }
            }
            return result;
	    }
	}
}
