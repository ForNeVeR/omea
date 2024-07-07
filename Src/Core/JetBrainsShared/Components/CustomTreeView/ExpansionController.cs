// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;

namespace JetBrains.UI.Components.CustomTreeView
{
	/// <summary>
	/// Controls expansion on tree views
	/// </summary>
	public class ExpansionController
	{
    /// <summary>
    /// The tree view
    /// </summary>
    private TreeView myTreeView;

    /// <summary>
    /// Enabled flag
    /// </summary>
    private bool myIsEnabled;

    public TreeView TreeView
    {
      get { return myTreeView; }
    }

    public bool IsEnabled
    {
      get { return myIsEnabled; }
      set { myIsEnabled = value; }
    }

		public ExpansionController( TreeView treeView )
		{
      myTreeView = treeView;

      myTreeView.AfterExpand += new TreeViewEventHandler(NodeExpanded);
		}

    public static void ExpandNode( TreeNode node, bool select )
    {
      node.Expand();

      if (select)
        node.TreeView.SelectedNode = node;

      do
      {
        node.Expand();

        if (node.Nodes.Count > 0)
        {
          node = node.Nodes[0];

          if (select)
            node.TreeView.SelectedNode = node;
        }
        else
          break;
      } while (node.NextNode == null);
    }

    private void NodeExpanded( object sender, TreeViewEventArgs e )
    {
      if (myIsEnabled)
      {
        ExpandNode (e.Node, false);
      }
    }
	}
}
