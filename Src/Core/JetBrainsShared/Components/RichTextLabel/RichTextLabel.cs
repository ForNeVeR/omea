// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using JetBrains.UI.RichText;

namespace JetBrains.UI.Components.RichTextLabel
{
	/// <summary>
	/// Summary description for RichTextLabel.
	/// </summary>
	public class RichTextLabel : System.Windows.Forms.Control
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
    private RichTextBlock myRichTextBlock = new RichTextBlock(new RichTextBlockParameters(0));

	  public RichTextBlock RichTextBlock
	  {
	    get { return myRichTextBlock; }
	    set
      {
        myRichTextBlock = value;
        Invalidate();
        Update();
      }
	  }

	  public RichTextLabel()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

    protected override Size DefaultSize
    {
      get
      {
        using (Graphics g = CreateGraphics())
        {
          IntPtr hDC = g.GetHdc();

          try
          {
            return myRichTextBlock.GetSize(hDC).ToSize();
          }
          finally
          {
            g.ReleaseHdc(hDC);
          }
        }
      }
    }

		protected override void OnPaint(PaintEventArgs pe)
		{
      IntPtr hDC = pe.Graphics.GetHdc();

      try
      {
        myRichTextBlock.Draw(hDC, pe.ClipRectangle);
      }
      finally
      {
        pe.Graphics.ReleaseHdc(hDC);
      }

			// Calling the base class OnPaint
			base.OnPaint(pe);
		}
	}
}
