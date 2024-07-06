// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.UI.Components.CustomListView
{
  /// <summary>
  /// Interface for creating custom Item painters
  /// </summary>
  public interface IItemPainter
  {
    /// <summary>
    /// Checks if a given Item can be handled by the painter
    /// </summary>
    /// <param name="Item">The Item to check</param>
    /// <returns><c>true</c> if the Item can be handled, <c>false</c> it cannot.</returns>
    bool IsHandled( ListViewItem Item );

    /// <summary>
    /// Returns size of an item
    /// </summary>
    Size GetSize( ListViewItem item, Graphics g );

    /// <summary>
    /// Draws a given Item in specified rectangle
    /// </summary>
    /// <remarks>
    /// If the Item cannot be handled, the method should do nothing
    /// </remarks>
    /// <param name="Item">The Item to draw</param>
    /// <param name="g">Graphics to draw in</param>
    /// <param name="rect">Bounding rectangle to use</param>
    void Draw( ListViewItem Item, Graphics g, Rectangle rect);

    /// <summary>
    /// Gets Item which is displayed at specified point
    /// </summary>
    /// <param name="ListView">ListView control to look for Items in</param>
    /// <param name="point">The point to look for Item at.</param>
    /// <returns>Item at point or <c>null</c> if there's no such Item.</returns>
    ListViewItem GetItemAt( ListView ListView, Point point );
  }
}
