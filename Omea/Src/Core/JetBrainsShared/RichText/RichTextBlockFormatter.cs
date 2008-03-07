/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;

namespace JetBrains.UI.RichText
{
	/// <summary>
	/// Formats rich text block from rich text
	/// </summary>
	public class RichTextBlockFormatter
	{
    private static char[] ourSplitters = new char[] {' ', '\'', '\"', ';', ':', '.'};

    public static RichTextBlock Format( RichTextBlock block, int width, IntPtr hdc )
    {
      RichTextBlock result = new RichTextBlock(block.Parameters);

      foreach (RichText line in block.Lines)
        result.AddLines(Format(line, width, hdc));
      
      return result;
    }

    public static RichTextBlock Format( RichText text, int width, IntPtr hdc )
    {
      RichTextBlock block = new RichTextBlock(new RichTextBlockParameters(1));

      while (text.GetSize(hdc).Width >= width)
      {
        int[] positions = GetPossibleDivisionOffsets(text.Text);
        RichText[] parts, oldParts;

        oldParts = new RichText[] {text, new RichText("", text.Parameters)};

        foreach (int position in positions)
        {
          parts = text.Split(position);

          if (parts[0].GetSize(hdc).Width > width)
            break;

          oldParts = parts;
        }

        block.AddLine(oldParts[0]);

        if (oldParts[0].Length == 0) // prevent endless loop
          break;
        
        text = oldParts[1];
      }

      block.AddLine(text);

      return block;
    }

    private static int[] GetPossibleDivisionOffsets( string text )
    {
      int offset = 0;
      int size = 0;

      while (offset >= 0 && offset < text.Length)
      {
        int oldOffset = offset;
        offset = text.IndexOfAny(ourSplitters, offset + 1);        

        if (offset > oldOffset + 1)
          size++;
      }

      int[] positions = new int[size];
      int i = 0;
      offset = 0;
      
      while (offset >= 0 && offset < text.Length)
      {
        int oldOffset = offset;
        offset = text.IndexOfAny(ourSplitters, offset + 1);        

        if (offset > oldOffset + 1)
          positions[i++] = offset;
      }

      return positions;
    }
	}
}
