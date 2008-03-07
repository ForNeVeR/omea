/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
