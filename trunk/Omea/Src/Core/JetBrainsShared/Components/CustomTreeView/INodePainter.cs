/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.UI.Components.CustomTreeView
{
	/// <summary>
	/// Interface for creating custom node painters
	/// </summary>
	public interface INodePainter
	{
    /// <summary>
    /// Checks if a given node can be handled by the painter
    /// </summary>
    /// <param name="node">The node to check</param>
    /// <returns><c>true</c> if the node can be handled, <c>false</c> it cannot.</returns>
    bool IsHandled( TreeNode node );

    /// <summary>
    /// Draws a given node in specified rectangle
    /// </summary>
    /// <remarks>
    /// If the node cannot be handled, the method should do nothing
    /// </remarks>
    /// <param name="node">The node to draw</param>
    /// <param name="hdc">Device context to draw in</param>
    /// <param name="rect">Bounding rectangle to use</param>
    void Draw( TreeNode node, IntPtr hdc, Rectangle rect );    

    /// <summary>
    /// Gets node which is displayed at specified point
    /// </summary>
    /// <param name="treeView">TreeView control to look for nodes in</param>
    /// <param name="point">The point to look for node at.</param>
    /// <returns>Node at point or <c>null</c> if there's no such node.</returns>
    TreeNode GetNodeAt( TreeView treeView, Point point );

    /// <summary>
    /// Invalidates the specified tree node.
    /// </summary>
    /// <param name="node">The node to invalidate.</param>
    void InvalidateNode( TreeNode node );
	}
}
