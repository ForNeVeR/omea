// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Globalization;

namespace JetBrains.UI.Components.TreeSearchWindow
{
  using JetBrains.UI.RichText;
  using JetBrains.UI.Components.CustomTreeView;

	/// <summary>
	/// Filters nodes by starting substring
	/// </summary>
	public class StartingFilter : INodeFilter
  {
    #region INodeFilter Members

    public bool Matches(System.Windows.Forms.TreeNode node, string text)
    {
      string nodeText = "";

      nodeText = node.Text;

      if (nodeText.Length < text.Length)
        return false;

      string nodeTextStart = nodeText.Substring(0, text.Length);

      return CultureInfo.CurrentCulture.CompareInfo.Compare(nodeTextStart, text, CompareOptions.IgnoreCase) == 0;
    }
    #endregion
  }
}
