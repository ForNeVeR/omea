// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using JetBrains.UI.Components.CustomListView;
using JetBrains.UI.RichText;

namespace JetBrains.UI.Components.RichTextListView
{
  /// <summary>
  /// Enumerates possible alignments for columns
  /// </summary>
  public enum ColumnAlignment
  {
    Left,
    Right,
    Center
  }

	/// <summary>
	/// Paints items in columns
	/// </summary>
	public class ColumnItemPainter : Components.CustomListView.IItemPainter
	{
    /// <summary>
    /// Sets spacing between columns
    /// </summary>
    private const int SPACING = 10;

    /// <summary>
    /// Stores alignments for columns
    /// </summary>
    private ColumnAlignment[] myAlignments;

    /// <summary>
    /// The painters to use for rendering
    /// </summary>
    private IItemPainter[] myPainters;

    /// <summary>
    /// The starting offsets
    /// </summary>
    private int[] myOffsets;

    /// <summary>
    /// Indicates wheter the starting offsets are correct
    /// </summary>
    private bool myOffsetsAreCorrect;

    /// <summary>
    /// List of registered items
    /// </summary>
    private ArrayList myItems = new ArrayList();

    /// <summary>
    /// The width
    /// </summary>
    private int myWidth;

    public int Width
    {
      get { return myWidth; }
    }

		public ColumnItemPainter( ColumnAlignment[] alignments, IItemPainter[] painters )
		{
      myAlignments = alignments;
      myPainters = painters;
      myOffsetsAreCorrect = false;
    }

    public void RegisterItem( ListViewItem item )
    {
      if (!IsHandled(item))
        throw new ArgumentException("The item is not handled by at least one item painter. Please register the item in all item painters first");

      myItems.Add(item);
      myOffsetsAreCorrect = false;
    }

    #region IItemPainter Members

    public bool IsHandled(ListViewItem item)
    {
      foreach (IItemPainter painter in myPainters)
        if (!painter.IsHandled(item))
          return false;

      return true;
    }

    public Size GetSize(ListViewItem item, Graphics g)
    {
      if (!myOffsetsAreCorrect)
        UpdateOffsets(g);

      int maxHeight = 0;

      foreach (IItemPainter painter in myPainters)
      {
        int height = painter.GetSize(item, g).Height;

        if (height > maxHeight)
          maxHeight = height;
      }

      return new Size(myWidth, maxHeight);
    }

    public void Draw(ListViewItem item, Graphics g, Rectangle rect)
    {
      if (!myOffsetsAreCorrect)
        UpdateOffsets(g);

      int startOffset = CalculateOffset(item);

      for (int i = 0; i < myPainters.Length; i++)
      {
        IItemPainter painter = myPainters[i];

        Size size = painter.GetSize(item, g);
        int offset = myOffsets[i] + startOffset;
        int endOffset = i == myPainters.Length - 1 ? rect.Width : myOffsets[i + 1];

        switch (myAlignments[i])
        {
          case ColumnAlignment.Right:
            offset += (endOffset - offset) - size.Width;
            break;
          case ColumnAlignment.Center:
            offset += ((endOffset - offset) - size.Width) / 2;
            break;
        }

        painter.Draw(item, g, new Rectangle(offset, 0, endOffset - offset, 0));
      }
    }

    public ListViewItem GetItemAt(ListView ListView, Point point)
    {
      return null;
    }

    #endregion

    #region Private methods
    private void UpdateOffsets( Graphics g )
    {
      myOffsets = new int[myPainters.Length];

      int offset = 0;

      for (int i = 0; i < myPainters.Length; i++)
      {
        IItemPainter painter = myPainters[i];

        myOffsets[i] = offset;
        int maxWidth = 0;

        foreach (ListViewItem item in myItems)
        {
          Size size = painter.GetSize(item, g);

          if (size.Width > maxWidth)
            maxWidth = size.Width;
        }

        offset += maxWidth + SPACING;
      }

      myWidth = offset;
      myOffsetsAreCorrect = true;
    }

    /// <summary>
    /// Calculates offset of a given Item from the left border
    /// </summary>
    private int CalculateOffset( ListViewItem item )
    {
      int offset = 0;

      //if ( item.ImageList != null && item.ImageList.Images.Count > 0 && item.ImageIndex >= 0 )
      offset += 17;

      if ( item.ListView.CheckBoxes )
        offset += 17;

      return offset;
    }
    #endregion
  }
}
