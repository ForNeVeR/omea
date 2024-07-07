// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.UI.Interop;

namespace JetBrains.UI.Components.CustomListBox
{
  public delegate void WindowsEventHandler( object sender, ref Message m );

  /// <summary>
  /// Summary description for CustomListBox.
  /// </summary>
  public class CustomListBox : System.Windows.Forms.ListBox
  {
    /// <summary>
    /// The item painter to use
    /// </summary>
    private IItemPainter myItemPainter;

    private int mySelectedIndexPreview;

    public event WindowsEventHandler MouseEvent;

    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.Container components = null;

    public CustomListBox()
    {
      mySelectedIndexPreview = SelectedIndex;
      DrawMode = DrawMode.OwnerDrawVariable;
      // This call is required by the Windows.Forms Form Designer.
      InitializeComponent();
    }

    public IItemPainter ItemPainter
    {
      get { return myItemPainter; }
      set { myItemPainter = value; }
    }

    public static Color SelectionBackColor(bool isFocused)
    {
      Color result = SystemColors.Highlight;
      if ( !isFocused )
        result = Color.FromArgb(result.A/2,result);
      return result;
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
      //
      // CustomListBox
      //
      this.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
    }
    #endregion

    //    private Rectangle GetFullItemRectangle(int index)
    //    {
    //      //TODO: GetItemRectangle works strange
    //      Rectangle rectangle = GetItemRectangle(index);
    //      rectangle.Height = GetItemHeight(index) + 1;
    //      return rectangle;
    //    }

    //    protected override void OnSelectedIndexChanged(EventArgs e)
    //    {
    //      try
    //      {
    //        SuspendLayout();
    //        if (myOldSelectedIndex != SelectedIndex)
    //        {
    //          if ( myOldSelectedIndex >= 0 && myOldSelectedIndex < Items.Count )
    //            Invalidate(GetFullItemRectangle(myOldSelectedIndex));
    //          int index = SelectedIndex;
    //          if ( index >= 0 && index < Items.Count )
    //            Invalidate(GetFullItemRectangle(index));
    //          myOldSelectedIndex = SelectedIndex;
    //        }
    //        base.OnSelectedIndexChanged (e);
    //        ResumeLayout(false);
    //      }
    //      catch (Exception ex)
    //      {
    //        System.Console.Write( ex );
    //      }
    //    }
    //
    //
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
      try
      {
        if ( myItemPainter != null && e.Index >= 0 && e.Index < Items.Count )
        {
          //System.Console.WriteLine(e.Bounds.Height);
          myItemPainter.Draw(Items[e.Index], e.Graphics, e.Bounds, e.Index == mySelectedIndexPreview);
        }
        else
        {
          base.OnDrawItem(e);
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("CustomListBox.DrawItem failed : " + ex, "UI");
        Invalidate(e.Bounds);
      }
    }

    protected override void OnMeasureItem(MeasureItemEventArgs e)
    {
      try
      {
        if (myItemPainter != null)
        {
          Size size = myItemPainter.GetSize(Items[e.Index], e.Graphics);

          e.ItemWidth = size.Width;
          e.ItemHeight = size.Height;
        }
        else
        {
          base.OnMeasureItem(e);
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("CustomListBox.MeasureItem failed : " + ex, "UI");
      }
    }

    private void CheckSelectedIndexPreview(int index)
    {
      if ( index != mySelectedIndexPreview )
      {
        if ( mySelectedIndexPreview != -1 && mySelectedIndexPreview < Items.Count )
          Invalidate(GetItemRectangle(mySelectedIndexPreview));
        mySelectedIndexPreview = index;
      }
    }

    public override int SelectedIndex
    {
      get
      {
        return base.SelectedIndex;
      }
      set
      {
        //System.Console.WriteLine("Old index=" + SelectedIndex + ", new index=" + value);
        CheckSelectedIndexPreview(value);
        if ( SelectedIndex != -1 )
        {
          Rectangle rectangle = GetItemRectangle(SelectedIndex);
          rectangle.Height+=2;//TODO: remove +2
          Invalidate(rectangle);
        }
        base.SelectedIndex = value;
      }
    }

    protected override void OnSelectedIndexChanged (EventArgs e)
    {
      CheckSelectedIndexPreview(SelectedIndex);
      base.OnSelectedIndexChanged (e);
    }

    protected override void CreateHandle ()
    {
      //TODO: a hack to be able to close the box with double click
      if (!IsDisposed)
        base.CreateHandle ();
    }

    protected override void WndProc(ref Message m)
    {
      if ((m.Msg >= Win32Declarations.WM_MOUSEFIRST && m.Msg <= Win32Declarations.WM_MOUSELAST) ||
        (m.Msg >= Win32Declarations.WM_NCMOUSEFIRST && m.Msg <= Win32Declarations.WM_NCMOUSELAST))
        if (MouseEvent != null)
          MouseEvent(this, ref m);

      if (m.Msg == Win32Declarations.WM_ERASEBKGND)
        return;

      base.WndProc (ref m);
    }
  }
}
