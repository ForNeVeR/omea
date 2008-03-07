/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Internal parts of IDisplayColumnManager API.
	/// </summary>
	public interface IDisplayColumnManagerEx
	{
        string GetColumnText( int[] propIds );
        int[] PropNamesToIDs( string[] propNames, bool ignoreErrors );
    }
}
