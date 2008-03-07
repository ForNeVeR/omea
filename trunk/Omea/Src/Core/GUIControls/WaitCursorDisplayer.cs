/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
