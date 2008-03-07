/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using JetBrains.UI.RichText;

namespace JetBrains.UI.Components.RichTextListView
{
  /// <summary>
  /// Paints Items using rich text
  /// </summary>
  public class RichTextItemPainter : Components.CustomListView.IItemPainter
  {
    /// <summary>
    /// List of handled Items
    /// </summary>
    private Hashtable myItems = new Hashtable();

    /// <summary>
    /// Adds text to a List Item
    /// </summary>
    public void Add( ListViewItem item, RichText.RichText text )
    {
      myItems[item] = text;
      //InvalidateItem(Item);
    }

    /// <summary>
    /// Removes a Item
    /// </summary>
    public void Remove( ListViewItem item )
    {
      myItems.Remove(item);      
    }

    /// <summary>
    /// Gets rich text by a List Item
    /// </summary>
    public RichText.RichText this[ ListViewItem item ]
    {
      get { return (RichText.RichText)myItems[item]; }
      set { myItems[item] = value; }
    }

    // removes all items
    public void Clear()
    {
      myItems = new Hashtable(myItems.Count);
    }

    #region IItemPainter Members

    public bool IsHandled(System.Windows.Forms.ListViewItem item)
    {
      return myItems.Contains(item);
    }

    public Size GetSize( ListViewItem item, Graphics g )
    {
        if (!IsHandled(item))
            return new Size();

        RichText.RichText text = (RichText.RichText)myItems[item];
        IntPtr hdc = g.GetHdc();
        try
        {
            Size size = text.GetSize(hdc).ToSize();
            size.Height = item.Bounds.Height;
            size.Width += 3 + CalculateOffset(item);      

            return size;
        }
        finally
        {
            g.ReleaseHdc( hdc );
        }
    }


    public void Draw(System.Windows.Forms.ListViewItem item, System.Drawing.Graphics g, Rectangle boundRect)
    {
      if (!IsHandled(item))
        return;

      ListView listView = item.ListView;

      RichText.RichText text = (RichText.RichText)myItems[item];
      Rectangle rect = CalculateItemRectangle(item, boundRect);
      Rectangle contentRect = CalculateContentRectangle(item, g, rect);

      // Center content vertically
      if (contentRect.Height < rect.Height)
      {
        int topOffset = (rect.Height - contentRect.Height) / 2;
        contentRect.Y += topOffset;
      }          
      
      g.FillRectangle(new SolidBrush(listView.BackColor), rect);

      if ( item.Selected )
      {
        Color backgroundColor = Colors.ListSelectionBackColor(listView.Focused);

        text = (RichText.RichText)text.Clone();
          
        g.FillRectangle(new SolidBrush(backgroundColor), rect);
        text.SetColors(SystemColors.HighlightText, backgroundColor);
      }

      g.SetClip(rect);

      IntPtr hdc = g.GetHdc();
      try
      {
        text.Draw(hdc, contentRect);
      }
      finally
      {
        g.ReleaseHdc( hdc );
      }

      if ( item.Selected && listView.Focused ) DrawDottedRectangle(g, rect);
    }

    public ListViewItem GetItemAt( ListView listView, Point point )
    {
      return listView.GetItemAt(point.X,point.Y);
    }

//    public void InvalidateItem( ListViewItem item )
//    {
//      Rectangle rect = new Rectangle(item.Bounds.Location, new Size(item.ListView.ClientSize.Width, item.Bounds.Height));
//      item.ListView.Invalidate( rect );
//    }

    #endregion

    #region Private methods

    private Rectangle CalculateItemRectangle(ListViewItem item, Rectangle boundRect)
    {
      Rectangle bounds = item.GetBounds(ItemBoundsPortion.Label);
      Rectangle rect = new Rectangle(boundRect.Left, bounds.Top, boundRect.Width, bounds.Height);

      int offset = CalculateOffset(item);

      rect.X += offset;
      rect.Width -= offset;

      return rect;
    }

    private int CalculateOffset( ListViewItem item )
    {
      return 17 + (item.ListView.CheckBoxes ? 17 : 0);
    }

    /// <summary>
    /// Calculated content rectangle of Item
    /// </summary>
    private Rectangle CalculateContentRectangle( ListViewItem item, Graphics g, Rectangle rect )
    { 
        RichText.RichText text = (RichText.RichText)myItems[item];
        IntPtr hdc = g.GetHdc();
        try
        {
            Rectangle result = new Rectangle(rect.Location, text.GetSize(hdc).ToSize());
            result.Width = rect.Width;
            return result;
        }
        finally
        {
            g.ReleaseHdc( hdc );
        }
    }

    /// <summary>
    /// Draws dotted rectangle
    /// </summary>
    private void DrawDottedRectangle( Graphics g, Rectangle rect )
    {
      Pen pen = new Pen(new HatchBrush(HatchStyle.Percent50, SystemColors.Highlight, Color.Black));
      g.DrawRectangle(pen, rect);
    }
    #endregion
  }
}