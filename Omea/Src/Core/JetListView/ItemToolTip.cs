/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using JetBrains.Interop.WinApi;
using JetBrains.Omea.Base;
using JetBrains.UI.Interop;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// Custom wrapper for the system ToolTips control.
	/// </summary>
	internal class ItemToolTip: NativeWindow, IDisposable
	{
        private const string TOOLTIP_CLASS = "tooltips_class32";
        private JetListView _ownerControl;
        private PinnedStringPool _toolTipPool;
        private Rectangle _lastItemShowRect;
        private JetListViewNode _lastToolTipNode;
        private JetListViewColumn _lastToolTipColumn;
        private bool _lastNeedPlace = true;
        
        public ItemToolTip( JetListView ownerControl )
		{
            _ownerControl = ownerControl;
            _toolTipPool = new PinnedStringPool( 5 );
		}

	    public void Dispose()
	    {
            DestroyHandle();
	        _toolTipPool.Dispose();
	    }

	    internal void CreateHandle()
        {
            CreateParams cp = new CreateParams();
            cp.Parent = _ownerControl.Handle;
            cp.ClassName = TOOLTIP_CLASS;
            unchecked
            {
                cp.Style = (int) Win32Declarations.WS_POPUP | Win32Declarations.TTS_NOFADE | Win32Declarations.TTS_NOPREFIX;
            }
            cp.ExStyle = 0;
            cp.Caption = null;

            CreateHandle( cp );

            Win32Declarations.SetWindowPos( this.Handle, (IntPtr) Win32Declarations.HWND_TOPMOST, 0, 0, 0, 0, 19 ); 

            TOOLINFOCB ti = new TOOLINFOCB();
            ti.cbSize = Marshal.SizeOf( typeof(TOOLINFO) );
            ti.flags = Win32Declarations.TTF_IDISHWND | Win32Declarations.TTF_SUBCLASS | Win32Declarations.TTF_TRANSPARENT;
            ti.hwnd = _ownerControl.Handle;
            ti.uId = _ownerControl.Handle;
            ti.textCallback = Win32Declarations.LPSTR_TEXTCALLBACK;

            if ( Win32Declarations.SendMessage( Handle, Win32Declarations.TTM_ADDTOOLW, 0, ref ti ) == 0 )
            {
                throw new Exception( "Failed to add tool" );
            }

            Win32Declarations.SendMessage( Handle, Win32Declarations.TTM_ACTIVATE, (IntPtr) 1, IntPtr.Zero );
        }

	    internal void HandleWMNotify( ref Message m )
	    {
            uint pos = Win32Declarations.GetMessagePos();
            Point pt = new Point( Win32Declarations.LOWORD( pos ), Win32Declarations.HIWORD( pos ) );
            pt = _ownerControl.PointToClient( pt );

            NMHDR nmhdr = (NMHDR) m.GetLParam( typeof(NMHDR) );
            if ( nmhdr.code == Win32Declarations.TTN_NEEDTEXTW )
            {
                NMTTDISPINFOW dispInfo = (NMTTDISPINFOW) m.GetLParam( typeof(NMTTDISPINFOW ) );
                _lastToolTipNode = _ownerControl.GetNodeAt( pt );
                _lastToolTipColumn = _ownerControl.GetColumnAt( pt );
                string tooltip = null;
                if ( _lastToolTipColumn != null && _lastToolTipNode != null )
                {
                    Rectangle rc = _ownerControl.GetItemBounds( _lastToolTipNode, _lastToolTipColumn );
                    _lastNeedPlace = true;
                    tooltip = _lastToolTipColumn.GetToolTip( _lastToolTipNode, rc, ref _lastNeedPlace );
                    _lastItemShowRect = rc;
                }
                if ( tooltip == null || tooltip.Length == 0 )
                {
                    dispInfo.szText = "";
                    dispInfo.lpszText = IntPtr.Zero;
                }
                else
                {
                    dispInfo.lpszText = _toolTipPool.PinString( tooltip );
                }
                Marshal.StructureToPtr( dispInfo, m.LParam, false );
            }
            else if ( nmhdr.code == Win32Declarations.TTN_SHOW && _lastNeedPlace )
            {
                Rectangle scItemShowRect = _ownerControl.RectangleToScreen( _lastItemShowRect );
                RECT rc = Win32Declarations.RectangleToRECT( scItemShowRect );
                Win32Declarations.SendMessage( Handle, (LVM) Win32Declarations.TTM_ADJUSTRECT,
                    1, ref rc );

                Win32Declarations.SetWindowPos( Handle, IntPtr.Zero, rc.left, rc.top, 0, 0,
                    Win32Declarations.SWP_NOSIZE | Win32Declarations.SWP_NOZORDER | Win32Declarations.SWP_NOACTIVATE );
                m.Result = (IntPtr) 1;
            }
	    }

	    internal void UpdateToolTip( Point pt )
	    {
            JetListViewNode node = _ownerControl.GetNodeAt( pt );
            JetListViewColumn col = _ownerControl.GetColumnAt( pt );
            if ( node != _lastToolTipNode || col != _lastToolTipColumn )
            {
                Hide();
                Win32Declarations.SendMessage( Handle, Win32Declarations.TTM_UPDATE, IntPtr.Zero, IntPtr.Zero );
            }
	    }

        public void Hide()
        {
            Win32Declarations.ShowWindow( Handle, Win32Declarations.SW_HIDE );
        }
	}
}
