// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;

using JetBrains.UI.Components.CustomListBox;

namespace JetBrains.UI.Components.RichTextListBox
{
	/// <summary>
	/// Paints items with rich text
	/// </summary>
	public class RichTextItemPainter : IItemPainter
	{
    /// <summary>
    /// Represents a list item
    /// </summary>
    private class ListItem
    {
      /// <summary>
      /// The text to use
      /// </summary>
      private RichText.RichText myRichText;

      /// <summary>
      /// The icon to use
      /// </summary>
      private Image myIcon;

      public RichText.RichText RichText
      {
        get { return myRichText; }
      }

      public Image Icon
      {
        get { return myIcon; }
      }

      public ListItem( RichText.RichText richText, Image icon )
      {
        myRichText = richText;
        myIcon = icon;
      }
    }

    /// <summary>
    /// List of handled Items
    /// </summary>
    private Hashtable myItems = new Hashtable();

    /// <summary>
    /// The list box to use
    /// </summary>
    private CustomListBox.CustomListBox myListBox;

    public CustomListBox.CustomListBox ListBox
    {
      get { return myListBox; }
      set { myListBox = value; }
    }

    /// <summary>
    /// Adds text to a List Item
    /// </summary>
    public void Add( object item, RichText.RichText text, Image icon )
    {
      myItems[item] = new ListItem(text, icon);
    }

    /// <summary>
    /// Removes a Item
    /// </summary>
    public void Remove( object item )
    {
      myItems.Remove(item);
    }

    public void Clear()
    {
      myItems = new Hashtable(myItems.Count);
    }

    #region IItemPainter Members

    public bool IsHandled(object item)
    {
      return myItems.Contains(item);
    }

    public System.Drawing.Size GetSize(object item, System.Drawing.Graphics g)
    {
      if (!IsHandled(item))
        return new Size();

      RichText.RichText text = ((ListItem)myItems[item]).RichText;
      IntPtr hdc = g.GetHdc();
      try
      {
        Size size = text.GetSize(hdc).ToSize();
        size.Height = myListBox.ItemHeight;
        size.Width += 3 + CalculateOffset(item);

        return size;
      }
      finally
      {
        g.ReleaseHdc( hdc );
      }
    }

    public void Draw( object item, Graphics g, Rectangle boundRect, bool drawSelected )
    {
      if (!IsHandled(item))
        return;

      RichText.RichText text = ((ListItem)myItems[item]).RichText;
      Image icon = ((ListItem)myItems[item]).Icon;

      Rectangle rect = CalculateItemRectangle(item, boundRect);
      Rectangle contentRect = CalculateContentRectangle(item, g, rect);

      // Center content vertically
      if (contentRect.Height < rect.Height)
      {
        int topOffset = (rect.Height - contentRect.Height) / 2;
        contentRect.Y += topOffset;
      }

      //object selectedItem = myListBox.SelectedIndex < 0 || myListBox.SelectedIndex >= myListBox.Items.Count
      //  ? null : myListBox.Items[myListBox.SelectedIndex];

      g.FillRectangle(new SolidBrush(myListBox.BackColor), boundRect);

      if ( drawSelected )
      {
        Color backgroundColor = Colors.ListSelectionBackColor(myListBox.Focused);

        text = (RichText.RichText)text.Clone();

        g.FillRectangle(new SolidBrush(backgroundColor), rect);
        text.SetColors(SystemColors.HighlightText, backgroundColor);
      }

      if (icon != null)
        DrawIcon(g, icon, boundRect, item);

      IntPtr hdc = g.GetHdc();
      try
      {
        text.Draw(hdc, contentRect);
        //System.Console.WriteLine(text.ToString());
      }
      finally
      {
        g.ReleaseHdc( hdc );
      }

      if ( drawSelected && myListBox.Focused )
        DrawDottedRectangle(g, rect);
    }

    #endregion

    protected virtual void DrawIcon(Graphics g, Image icon, Rectangle boundRect, object item)
    {
      g.DrawImage(icon, boundRect.Left, boundRect.Top, icon.Width, icon.Height);
    }

    #region Private methods

    private Rectangle CalculateItemRectangle( object item, Rectangle boundRect )
    {
      Rectangle rect = boundRect;
      int offset = CalculateOffset(item);

      rect.X += offset;
      rect.Width -= offset;

      return rect;
    }

    private int CalculateOffset( object item )
    {
      Image icon = ((ListItem)myItems[item]).Icon;

      if (icon != null)
        return icon.Width;
      else
        return 16;
    }

    /// <summary>
    /// Calculated content rectangle of Item
    /// </summary>
    private Rectangle CalculateContentRectangle( object item, Graphics g, Rectangle rect )
    {
      RichText.RichText text = ((ListItem)myItems[item]).RichText;
      IntPtr hdc = g.GetHdc();
      try
      {
        Rectangle result = new Rectangle(rect.Location, text.GetSize(hdc).ToSize());
        result.Width = rect.Width;
        return result;
      }
      finally
      {
        g.ReleaseHdc(hdc);
      }
    }

    /// <summary>
    /// Draws dotted rectangle
    /// </summary>
    private void DrawDottedRectangle( Graphics g, Rectangle rect )
    {
      Region oldClip = g.Clip;
      g.SetClip(rect);
      Pen pen = new Pen(new HatchBrush(HatchStyle.Percent50, SystemColors.Highlight, Color.Black), 1);
      rect.Width--;
      rect.Height--;
      g.DrawRectangle(pen, rect);
      g.SetClip(oldClip, CombineMode.Replace);
    }
    #endregion
  }
}
