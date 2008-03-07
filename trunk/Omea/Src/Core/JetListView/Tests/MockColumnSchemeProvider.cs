/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.JetListViewLibrary.Tests
{
	public class MockColumnSchemeProvider: IColumnSchemeProvider
	{
        private MultiLineColumnScheme _defaultScheme = new MultiLineColumnScheme();

	    public MultiLineColumnScheme DefaultScheme
	    {
	        get { return _defaultScheme; }
	    }

	    public MultiLineColumnScheme GetColumnScheme( object item )
	    {
	        return _defaultScheme;
	    }
	}
}
