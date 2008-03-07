/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Drawing;

namespace JetBrains.UI.Components
{
  public sealed class Colors
  {
    private Colors() {}

    public static Color ListSelectionBackColor(bool isActive)
    {
      Color result = SystemColors.Highlight;
      if ( !isActive )
      {
        //result = Color.FromArgb(result.A/2,result);
        //float k = 2;
        //while ( (int)(result.R*k) > 255 || (int)(result.G*k) > 255 || (int)(result.B*k) > 255 )
        //  k = k * 0.8f;
        //result = System.Drawing.Color.FromArgb(result.A, (int) (result.R*k), (int) (result.G*k), (int) (result.B*k));
        result = result;
      }
      return result;
    }
  }
}