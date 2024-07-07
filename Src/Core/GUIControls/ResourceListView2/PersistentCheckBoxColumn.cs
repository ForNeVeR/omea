// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A CheckBoxColumn which saves the checked state of a node to a resource property.
	/// </summary>
	public class PersistentCheckBoxColumn: CheckBoxColumn
	{
        private int _checkedProperty = -1;
        private object _checkedSetValue = null;
        private object _checkedUnsetValue = null;

	    public int CheckedProperty
	    {
	        get { return _checkedProperty; }
	        set { _checkedProperty = value; }
	    }

	    public object CheckedSetValue
	    {
	        get { return _checkedSetValue; }
	        set { _checkedSetValue = value; }
	    }

	    public object CheckedUnsetValue
	    {
	        get { return _checkedUnsetValue; }
	        set { _checkedUnsetValue = value; }
	    }

	    protected override void OnAfterCheck( CheckBoxEventArgs args )
	    {
	        base.OnAfterCheck( args );
            if ( _checkedProperty >= 0 )
            {
                IResource res = (IResource) args.Item;
                new ResourceProxy( res ).SetPropAsync( _checkedProperty,
                    (args.NewState == CheckBoxState.Checked ) ? _checkedSetValue : _checkedUnsetValue );
            }
	    }

	    protected override CheckBoxState GetDefaultCheckState( object item )
	    {
	        if ( _checkedProperty >= 0 )
	        {
	            IResource res = (IResource) item;
                if ( Object.Equals( res.GetProp( _checkedProperty ), _checkedSetValue ) )
                {
                    return CheckBoxState.Checked;
                }
	        }
            return base.GetDefaultCheckState( item );
	    }
	}
}
