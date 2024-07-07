// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;

namespace JetBrains.UI.Components.TreeSearchWindow
{
	/// <summary>
	/// Checks if a given node matches specified text input
	/// </summary>
	public interface INodeFilter
	{
	  /// <summary>
	  /// Checks if the specified tree node matches the specified string
	  /// </summary>
	  /// <param name="node">The node to check</param>
	  /// <param name="text">The given text</param>
	  /// <returns><c>true</c> if the node does match, <c>false</c> if it doesn't</returns>
    bool Matches( TreeNode node, string text );
	}
}
