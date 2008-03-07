/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JetBrains.Omea.SamplePlugins.CategoryHotkeys
{
	/// <summary>
	/// A control for entering hot keys (duplicates the functionality of the COMCTL32 hotkey control).
	/// </summary>
	public class HotkeyControl: TextBox
	{
	    private KeysConverter _keysConverter = new KeysConverter();
        
	    protected override void OnKeyDown( KeyEventArgs e )
	    {
	        base.OnKeyDown( e );
            if ( e.KeyData == ( Keys.Alt | Keys.Menu ) || 
                 e.KeyData == ( Keys.ControlKey | Keys.Control ) ||
                 e.KeyData == ( Keys.ShiftKey | Keys.Shift ) )
            {
                return;
            }
                 
            if ( e.KeyData == Keys.Delete )
            {
                Text = "";
            }
            else
            {
                Text = (string) _keysConverter.ConvertTo( e.KeyData, typeof(string) );
            }
            e.Handled = true;
	    }

	    protected override void OnKeyPress( KeyPressEventArgs e )
	    {
            base.OnKeyPress( e );
	        e.Handled = true;
	    }
	}
}
