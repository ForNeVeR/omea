// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;

namespace JetBrains.UI.Components.CustomTreeView
{
	/// <summary>
	/// Controls selection of a tree view
	/// </summary>
	public class SelectionController
	{
    /// <summary>
    /// The tree view to use
    /// </summary>
    private TreeView myTreeView;

		public SelectionController( TreeView treeView )
		{
      myTreeView = treeView;
		}

    public static void SelectDeepestVisible( TreeNode node )
    {
      while (node.IsExpanded && node.Nodes.Count > 0)
        node = node.Nodes[0];

      node.TreeView.SelectedNode = node;
      node.Collapse();
    }
	}
}
