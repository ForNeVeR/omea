/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using JetBrains.UI.Interop;

namespace JetBrains.UI.Components.Separator
{
	/// <summary>
	/// Summary description for Separator.
	/// </summary>
	public class Separator : System.Windows.Forms.Label
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Separator()
		{
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

		protected override void OnPaint(PaintEventArgs pe)
		{
			// TODO: Add custom paint code here
      Graphics graphics = pe.Graphics;
      Rectangle rect = ClientRectangle;
      SizeF textSize = graphics.MeasureString(Text, Font);

      switch (TextAlign)
      {
        case ContentAlignment.TopLeft:
          ControlPaint.DrawBorder3D (graphics, rect.Left, rect.Top + (int)(textSize.Height+1)/2, rect.Width, 2);
          graphics.FillRectangle(new SolidBrush (BackColor), rect.Left, rect.Top, textSize.Width, textSize.Height);
          break;
      }

			// Calling the base class OnPaint
			base.OnPaint(pe);
		}
	}
}
