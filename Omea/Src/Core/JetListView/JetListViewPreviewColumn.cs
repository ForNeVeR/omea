/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// Subclass of JetListViewColumn which can be used in the auto-preview area.
	/// </summary>
	public class JetListViewPreviewColumn: JetListViewColumn
	{
        private int _autoPreviewLines = 1;

	    public int AutoPreviewLines
	    {
	        get { return _autoPreviewLines; }
	        set { _autoPreviewLines = value; }
	    }

	    public virtual int GetAutoPreviewHeight( JetListViewNode node )
	    {
	        return (int) OwnerControl.Font.GetHeight() + 2;
	    }

        protected virtual void OnRowHeightChanged( JetListViewNode node, int oldHeight, int newHeight )
        {
            if ( RowHeightChanged != null )
            {
                RowHeightChanged( this, new RowHeightChangedEventArgs( node, oldHeight, newHeight ) );
            }
        }

        protected virtual void OnAllRowsHeightChanged()
        {
            if ( AllRowsHeightChanged != null )
            {
                AllRowsHeightChanged( this, EventArgs.Empty );
            }
        }

        protected void ShiftRectForText( ref Rectangle rcRect )
        {
            rcRect.X += 8;
            rcRect.Width -= 8;
        }

        public event RowHeightChangedEventHandler RowHeightChanged;
        public event EventHandler AllRowsHeightChanged;
	}
}
