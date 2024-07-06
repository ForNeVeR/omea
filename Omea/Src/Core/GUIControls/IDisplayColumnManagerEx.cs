// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
