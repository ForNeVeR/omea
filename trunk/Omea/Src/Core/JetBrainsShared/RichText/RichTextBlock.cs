/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Collections;
using System.Diagnostics;

namespace JetBrains.UI.RichText
{
  /// <summary>
  /// Structure for setting parameters for rich text blocks
  /// </summary>
  public struct RichTextBlockParameters
  {
    /// <summary>
    /// Interline spacing
    /// </summary>
    private int myInterlineSpacing;

    public int InterlineSpacing
    {
      get { return myInterlineSpacing; }
      set { myInterlineSpacing = value; }
    }

    public RichTextBlockParameters( int interlineSpacing )
    {
      myInterlineSpacing = interlineSpacing;
    }
  }


	/// <summary>
	/// Represents a block (possibly multiline) of rich text.
	/// </summary>
	public class RichTextBlock
	{
    /// <summary>
    /// Rich text block parameters
    /// </summary>
    private RichTextBlockParameters myParameters;

    /// <summary>
    /// Lines of rich text block
    /// </summary>
    private ArrayList myLines;

    /// <summary>
    /// Gets array of lines
    /// </summary>
    public RichText[] Lines
    {
      get { return (RichText[])myLines.ToArray(typeof(RichText)); }
    }

	  public RichTextBlockParameters Parameters
	  {
	    get { return myParameters; }
	  }

	  public RichTextBlock( RichTextBlockParameters parameters )
		{			
      myParameters = parameters;
      myLines = new ArrayList();
		}

    public int GetLine( Point point, IntPtr hdc )
    {
      if (point.Y < 0)
        return -1;

      int y = 0;

      for (int i = 0; i < myLines.Count; i++)
      {
        RichText line = (RichText) myLines[i];
        
        y += (int)line.GetSize(hdc).Height;

        if (y > point.Y)
          return i;
      }

      return -1;
    }

    public int GetOffset( Point point, IntPtr hdc )
    {
      int lineIndex = GetLine(point, hdc);

      if (lineIndex < 0)
        return -1;

      RichText line = (RichText)myLines[lineIndex];

      return line.GetCharByOffset(point.X, hdc);
    }

    public int AddLine( RichText line )
    {
      Debug.Assert (line != null);

      return myLines.Add(line);
    }

    public int AddLines( RichTextBlock lines )
    {
      Debug.Assert (lines != null);
      int result = -1;

      foreach (RichText line in lines.Lines)
        result = myLines.Add(line);
      
      return result;
    }

    public void RemoveLine( RichText line )
    {
      myLines.Remove(line);
    }

    public void RemoveLineAt( int index )
    {
      myLines.RemoveAt(index);
    }

    public void InsertLine( RichText line, int index )
    {
      myLines.Insert(index, line);
    }

    public SizeF GetSize( IntPtr hdc )
    {
      float width = 0, height = 0;

      foreach (RichText line in myLines)
      {
        SizeF lineSize = line.GetSize(hdc);

        width = Math.Max(width, lineSize.Width);        
        height += lineSize.Height;
      }

      height += myParameters.InterlineSpacing * (myLines.Count > 0 ? myLines.Count - 1 : 0);

      return new SizeF(width, height);
    }

    public void Draw( IntPtr hdc, Rectangle rect )
    {
      bool isFirst = true;

      foreach (RichText line in myLines)
      {
        if (!isFirst)
        {
          rect.Y += myParameters.InterlineSpacing;
        }

        SizeF size = line.GetSize(hdc);
        rect.Height = (int)size.Height;
        rect.Width = (int)size.Width;

        line.Draw(hdc, rect);     
   
        rect.Y += (int)size.Height;        

        if (isFirst)
          isFirst = false;
      }
    }
	}
}
