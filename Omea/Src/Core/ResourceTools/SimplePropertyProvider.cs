﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;

namespace JetBrains.Omea.ResourceTools
{
	/**
     * A simple implementation of IPropertyProvider - the class which can be used
     * to attach extra "virtual" properties to resource lists.
     */
    
    public class SimplePropertyProvider: IPropertyProvider
	{
        private IntHashTable _propHashTable = new IntHashTable();

        public event PropertyProviderChangeEventHandler ResourceChanged;

        public void SetProp( int resourceID, int propID, object propValue )
        {
            object oldValue = null;
            lock( _propHashTable )
            {
                IntHashTable propValueHash = (IntHashTable) _propHashTable [propID];
                if ( propValueHash == null )
                {
                    propValueHash = new IntHashTable();
                    _propHashTable [propID] = propValueHash;
                }
                else
                {
                    oldValue = propValueHash [resourceID];
                }
                propValueHash [resourceID] = propValue;
            }

            if ( ResourceChanged != null )
            {
                ResourceChanged( this, new PropertyProviderChangeEventArgs( resourceID, propID, oldValue ) );
            }
        }
        
        public bool HasProp( int propID )
        {
            lock( _propHashTable )
            {
                return _propHashTable.ContainsKey( propID );
            }
        }

        public object GetPropValue( IResource res, int propID )
        {
            lock ( _propHashTable )
            {
                IntHashTable propValueHash = (IntHashTable) _propHashTable [propID];
                if ( propValueHash == null )
                    return null;

                return propValueHash [res.Id];
            }
        }
    }
}
