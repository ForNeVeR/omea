// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using System.Drawing;

namespace JetBrains.UI.Components.CustomListBox
{
	/// <summary>
	/// Interface for creating custom painters
	/// </summary>
	public interface IItemPainter
	{
    /// <summary>
    /// Checks if a given Item can be handled by the painter
    /// </summary>
    /// <param name="item">The Item to check</param>
    /// <returns><c>true</c> if the Item can be handled, <c>false</c> it cannot.</returns>
    bool IsHandled( object item );

    /// <summary>
    /// Returns size of an item
    /// </summary>
    Size GetSize( object item, Graphics g );

    /// <summary>
    /// Draws a given Item in specified rectangle
    /// </summary>
    /// <remarks>
    /// If the Item cannot be handled, the method should do nothing
    /// </remarks>
    /// <param name="item">The Item to draw</param>
    /// <param name="g">Graphics to draw in</param>
    /// <param name="rect">Bounding rectangle to use</param>
    void Draw( object item, Graphics g, Rectangle rect, bool drawSelected);
	}
}
