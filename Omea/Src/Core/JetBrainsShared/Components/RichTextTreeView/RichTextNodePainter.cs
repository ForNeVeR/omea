// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using JetBrains.Interop.WinApi;
using JetBrains.UI.Components.CustomTreeView;
using JetBrains.UI.Interop;

namespace JetBrains.UI.Components.RichTextTreeView
{
  /// <summary>
  /// Paints nodes using rich text
  /// </summary>
  public class RichTextNodePainter : INodePainter
  {
    /// <summary>
    /// List of handled nodes
    /// </summary>
    private Hashtable myNodes = new Hashtable();

    /// <summary>
    /// Adds text to a tree node
    /// </summary>
    public void Add( TreeNode node, RichText.RichText text )
    {
      myNodes [node] = text;
      InvalidateNode(node);
    }

    /// <summary>
    /// Removes a node
    /// </summary>
    public void Remove( TreeNode node )
    {
      myNodes.Remove(node);
    }

    /// <summary>
    /// Gets rich text by a tree node
    /// </summary>
    public RichText.RichText this[ TreeNode node ] { get { return (RichText.RichText)myNodes[node]; } set { myNodes[node] = value; } }

    #region INodePainter Members

    public bool IsHandled( TreeNode node )
    {
      return myNodes.Contains(node);
    }

    public void Draw( TreeNode node, IntPtr hdc, Rectangle rect )
    {
      if (!IsHandled(node) || node.Bounds.IsEmpty || node.IsEditing)
        return;

      CustomTreeView.CustomTreeView treeView = (CustomTreeView.CustomTreeView)node.TreeView;

      int offset = CalculateOffset(node);

      RichText.RichText text = (RichText.RichText)myNodes[node];

      Rectangle contentRect = CalculateContentRectangle(node, hdc, offset);
      rect.X += offset;
      rect.Width -= offset;

      Rectangle fullRect = new Rectangle(contentRect.X - 1, node.Bounds.Top, contentRect.Width + 5, node.Bounds.Height);

      //g.SetClip(fullRect);

      TreeNode dropHiliteNode = treeView.DraggingOver ? treeView.DropHighlightedNode : null;
      bool hasFocus = node.TreeView.Focused;
      bool drawSelected = treeView.DraggingOver
          ? (node == dropHiliteNode)
          : node.IsSelected && hasFocus;
      bool drawNonfocusedSelection = node.IsSelected && !node.TreeView.HideSelection &&
          (!hasFocus || treeView.DraggingOver);

      Color backColor;
      if (drawSelected)
      {
          backColor = SystemColors.Highlight;
      }
      else if (drawNonfocusedSelection)
      {
          backColor = SystemColors.Control;
      }
      else
        backColor = treeView.BackColor;

      IntPtr hBrush = Win32Declarations.CreateSolidBrush(Win32Declarations.ColorToRGB(backColor));

      RECT lineRect = new RECT(fullRect.Left-1, node.Bounds.Top, fullRect.Right+1, node.Bounds.Bottom);
      Win32Declarations.FillRect(hdc, ref lineRect, hBrush);
      Win32Declarations.DeleteObject(hBrush);

      if (drawSelected)
      {
        text = (RichText.RichText)text.Clone();
        text.SetColors(SystemColors.HighlightText, SystemColors.Highlight);
      }
      else if (drawNonfocusedSelection)
      {
        text = (RichText.RichText)text.Clone();
        text.SetColors(SystemColors.WindowText, SystemColors.Control);
      }

      text.Draw(hdc, contentRect);

      if (hasFocus && treeView.SelectedNode == node && treeView.NeedFocusRect() && dropHiliteNode == null)
      {
        RECT rc = new RECT(fullRect.Left-1, fullRect.Top, fullRect.Right+1, fullRect.Bottom);
        Win32Declarations.DrawFocusRect(hdc, ref rc);
      }
    }

    public TreeNode GetNodeAt( TreeView treeView, Point point )
    {
      TVHITTESTINFO hti = new TVHITTESTINFO();
      hti.pt = new POINT(point);
      Win32Declarations.SendMessage(treeView.Handle, TreeViewMessage.TVM_HITTEST, 0, ref hti);
      if ((hti.flags & TreeViewHitTestFlags.ONITEM) != 0)
      {
        return TreeNode.FromHandle(treeView, hti.hItem);
      }
      if ((hti.flags & TreeViewHitTestFlags.ONITEMRIGHT) != 0)
      {
        using (Graphics g = treeView.CreateGraphics())
        {
          IntPtr hdc = g.GetHdc();
          try
          {
            TreeNode node = TreeNode.FromHandle(treeView, hti.hItem);
            int offset = CalculateOffset(node);
            Rectangle content = CalculateContentRectangle(node, hdc, offset);
            if (content.Contains(point))
              return node;
          }
          finally
          {
            g.ReleaseHdc(hdc);
          }
        }
      }

      return null;
    }

    public void InvalidateNode( TreeNode node )
    {
      try
      {
        Rectangle rect = new Rectangle(node.Bounds.Location, new Size(node.TreeView.ClientSize.Width, node.Bounds.Height));
        node.TreeView.Invalidate(rect);
      }
      catch (Exception ex )
      {
          Trace.WriteLine( "Error invalidating in RichTextNodePainter: " + ex.ToString() );
      }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Calculated content rectangle of node
    /// </summary>
    private Rectangle CalculateContentRectangle( TreeNode node, IntPtr hdc, int offset )
    {
      Rectangle rect = new Rectangle(node.TreeView.ClientRectangle.Left, node.Bounds.Top, node.TreeView.ClientSize.Width, node.Bounds.Height);
      RichText.RichText text = (RichText.RichText)myNodes[node];

      rect.X += offset;
      rect.Width -= offset;

      return new Rectangle(rect.Location, text.GetSize(hdc).ToSize());
    }

    /// <summary>
    /// Calculates offset of a given node from the left border
    /// </summary>
    private int CalculateOffset( TreeNode node )
    {
      TreeView tv = node.TreeView;
      int delta = tv.Nodes [0].Bounds.Left - tv.Indent;
      int level = GetNodeLevel(node);
      int offset;
      if (node.TreeView.ShowRootLines)
        offset = node.TreeView.Indent*(level + 1);
      else
        offset = node.TreeView.Indent*level;

      if (node.TreeView.ImageList != null && node.TreeView.ImageList.Images.Count > 0)
      {
        if (node.ImageIndex >= 0)
          offset += tv.ImageList.ImageSize.Width;
        //if ( tv.Nodes [0].ImageIndex >= 0 )
        delta -= tv.ImageList.ImageSize.Width;
      }

      if ((node.TreeView.CheckBoxes || (node.TreeView as CustomTreeView.CustomTreeView).ThreeStateCheckboxes) && (node.TreeView as CustomTreeView.CustomTreeView).GetNodeCheckState(node) != NodeCheckState.None)
        offset += 17;

      offset += delta;

      offset += 2;

      return offset;
    }

    /// <summary>
    /// Gets level of a given node
    /// </summary>
    /// <param name="node">The node to get level of</param>
    private int GetNodeLevel( TreeNode node )
    {
      int level = 0;

      while (node.Parent != null)
      {
        node = node.Parent;
        level++;
      }

      return level;
    }

    #endregion
  }
}
