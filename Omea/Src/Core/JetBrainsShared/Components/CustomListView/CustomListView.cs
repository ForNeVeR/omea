// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using JetBrains.UI.Interop;
using JetBrains.UI.RichText;

namespace JetBrains.UI.Components.CustomListView
{
  using RichText;

  /// <summary>
  /// Summary description for CustomListView.
  /// </summary>
  public class CustomListView : System.Windows.Forms.ListView
  {
    /// <summary>
    /// Item painter to use
    /// </summary>
    private IItemPainter myItemPainter;

    /// <summary>
    /// Gets or sets Item painter
    /// </summary>
    public IItemPainter ItemPainter
    {
      get { return myItemPainter; }
      set { myItemPainter = value; }
    }

    protected override void WndProc(ref Message m)
    {
      switch (m.Msg)
      {
        case Win32Declarations.OCM_NOTIFY:
          OnWmNotify(ref m);
          break;
        default:
          base.WndProc(ref m);
          break;
      }
    }

    /// <summary>
    /// Handles the WM_NOTIFY message of the parent control
    /// </summary>
    /// <param name="m">The message to handle</param>
    /// <returns>Whether the message was handled</returns>
    private void OnWmNotify( ref Message m )
    {
      // Marshal lParam into NMHDR:
      NMHDR hdr = new NMHDR();
      hdr = (NMHDR)Marshal.PtrToStructure(m.LParam, typeof(NMHDR));

      switch (hdr.code)
      {
        case Win32Declarations.NM_CUSTOMDRAW:
          NMLVCUSTOMDRAW customDraw;
          customDraw = (NMLVCUSTOMDRAW)Marshal.PtrToStructure(m.LParam, typeof(NMLVCUSTOMDRAW));

          OnCustomDraw(ref customDraw, ref m);

          break;
        default:
          base.WndProc( ref m );
          break;
      }
    }

    public Size GetItemSize( ListViewItem item )
    {
      try
      {
        using (Graphics g = CreateGraphics())
        {
          return myItemPainter.GetSize(item, g);
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("CustomListView.GetItemSize failed : " + ex, "UI");
        return new Size(1,1);
      }
    }

    #region Drawing logic
    /// <summary>
    /// Erases Item
    /// </summary>
    private void EraseItem( ref NMLVCUSTOMDRAW customDraw )
    {
      try
      {
        using (Graphics g = Graphics.FromHdc(customDraw.nmcd.hdc))
        {
          Rectangle rect = new Rectangle(customDraw.nmcd.rc.left, customDraw.nmcd.rc.top, customDraw.nmcd.rc.right - customDraw.nmcd.rc.left, customDraw.nmcd.rc.bottom - customDraw.nmcd.rc.top);
          g.FillRectangle(new SolidBrush(BackColor), rect);
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("CustomListView.EraseItem failed : " + ex, "UI");
      }
    }

    /// <summary>
    /// Draws Item
    /// </summary>
    private void DrawItem( ref NMLVCUSTOMDRAW customDraw )
    {
      try
      {
        ListViewItem item = this.Items[customDraw.nmcd.dwItemSpec];

        if (myItemPainter != null && myItemPainter.IsHandled(item))
          using (Graphics g = Graphics.FromHdc(customDraw.nmcd.hdc))
          {
            myItemPainter.Draw(item, g, new Rectangle(0, 0, ClientSize.Width, 0));
          }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("CustomListView.DrawItem failed : " + ex, "UI");
      }
    }
    #endregion

    /// <summary>
    /// Handles the NM_CUSTOMDRAW notification
    /// </summary>
    private void OnCustomDraw( ref NMLVCUSTOMDRAW customDraw, ref Message m )
    {
      ListViewItem item;

      switch (customDraw.nmcd.dwDrawStage)
      {
        case Win32Declarations.CDDS_PREPAINT:
          m.Result = (IntPtr)(Win32Declarations.CDRF_NOTIFYITEMDRAW | Win32Declarations.CDRF_NOTIFYPOSTPAINT);
          break;
        case Win32Declarations.CDDS_ITEMPREPAINT:
          item = Items[customDraw.nmcd.dwItemSpec];

          if (myItemPainter != null && item != null && myItemPainter.IsHandled(item))
          {
            m.Result = (IntPtr)(Win32Declarations.CDRF_NOTIFYITEMDRAW | Win32Declarations.CDRF_NOTIFYPOSTPAINT);
          }
          else
            m.Result = (IntPtr)Win32Declarations.CDRF_NOTIFYITEMDRAW;

          break;
        case Win32Declarations.CDDS_ITEMPOSTPAINT:
          DrawItem(ref customDraw);
          break;
        case Win32Declarations.CDDS_POSTERASE:
          EraseItem(ref customDraw);
          break;
        default:
          m.Result = (IntPtr)Win32Declarations.CDRF_DODEFAULT;
          break;
      }
    }
  }
}
