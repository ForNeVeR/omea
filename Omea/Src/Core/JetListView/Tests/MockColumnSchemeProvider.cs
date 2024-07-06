// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
