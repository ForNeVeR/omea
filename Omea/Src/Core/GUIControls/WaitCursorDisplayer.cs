// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Class which displays a wait cursor while some operation is in progress.
	/// </summary>
	public class WaitCursorDisplayer: IDisposable
	{
        private Cursor _oldCursor;

		public WaitCursorDisplayer()
		{
            _oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
		}

	    public void Dispose()
	    {
	        Cursor.Current = _oldCursor;
	    }
	}
}
