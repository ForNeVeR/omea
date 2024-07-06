// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using JetBrains.Interop.WinApi;
using JetBrains.UI.Interop;

namespace JetBrains.UI.Components.CustomTreeView
{
  public enum NodeCheckState
  {
    None = 0, Unchecked = 1, Checked = 2, Grayed = 3
  }

  /// <summary>
  /// A custom drawn TreeView control which allows using different node presentations
  /// </summary>
  public class CustomTreeView : System.Windows.Forms.TreeView
  {
    /// <summary>
    /// Node painter to use
    /// </summary>
    private INodePainter myNodePainter;

    /// <summary>
    /// Mouse coordinates
    /// </summary>
    private Point myMouseCoords;

    /// <summary>
    /// ImageList with images for three-state checkboxes.
    /// </summary>
    private ImageList myCheckImageList;

    /// <summary>
    /// Whether three-state checkboxes are enabled.
    /// </summary>
    private bool myThreeStateCheckboxes = false;

    /// <summary>
    /// Whether double-buffering is used for drawing the tree.
    /// </summary>
    private bool myDoubleBuffer = false;

    /// <summary>
    /// The bitmap used for double-buffered drawing.
    /// </summary>
    private Bitmap myDoubleBufferBitmap;

    /// <summary>
    /// Enable or disable multi-selection
    /// </summary>
    private bool myMultiSelect = false;

    private bool myDraggingOver = false;

    private bool mySelectionChanged = false;

    private Point myLastMouseUpPoint = new Point( -1, -1 );
    private DateTime myLastMouseUpTime;

    public event EventHandler MultiSelectChanged;

    protected override void Dispose( bool disposing )
    {
      if ( disposing )
      {
        if ( myDoubleBufferBitmap != null )
        {
          myDoubleBufferBitmap.Dispose();
          myDoubleBufferBitmap = null;
        }
        if ( myCheckImageList != null )
        {
          myCheckImageList.Dispose();
          myCheckImageList = null;
        }
      }
      base.Dispose( disposing );
    }

    /// <summary>
    /// Gets or sets node painter
    /// </summary>
    public INodePainter NodePainter
    {
      get { return myNodePainter; }
      set { myNodePainter = value; }
    }

    /// <summary>
    /// Enables or disables the 3-state checkboxes in the tree.
    /// </summary>
    public bool ThreeStateCheckboxes
    {
      get { return myThreeStateCheckboxes; }
      set
      {
        myThreeStateCheckboxes = value;
        if ( myThreeStateCheckboxes )
        {
          if ( myCheckImageList == null )
          {
            CreateCheckImageList();
            Win32Declarations.SendMessage( Handle, (int) TreeViewMessage.TVM_SETIMAGELIST,
              new IntPtr( (int) TreeViewImageList.TVSIL_STATE ), myCheckImageList.Handle );
          }
        }
      }
    }

    /// <summary>
    /// Enables or disables double-buffering when drawing the tree view.
    /// </summary>
    public bool DoubleBuffer
    {
        get { return myDoubleBuffer; }
        set
        {
          if ( myDoubleBuffer != value )
          {
            myDoubleBuffer = value;
            if ( myDoubleBuffer )
            {
              CreateDoubleBufferBitmap();
            }
            else if ( myDoubleBufferBitmap != null )
            {
              myDoubleBufferBitmap.Dispose();
              myDoubleBufferBitmap = null;
            }
          }
        }
    }

    public event ThreeStateCheckEventHandler AfterThreeStateCheck;

    public void SetNodeCheckState( TreeNode node, NodeCheckState checkState )
    {
      if ( node.TreeView != this )
      {
        return;
      }

      TVITEM item = new TVITEM();
      item.mask = TreeViewItemFlags.STATE | TreeViewItemFlags.HANDLE;
      item.stateMask = 0xF000;
      item.state = (int) checkState << 12;
      item.hItem = node.Handle;
      Win32Declarations.SendMessage( Handle, TreeViewMessage.TVM_SETITEMA, 0, ref item );
    }

    public NodeCheckState GetNodeCheckState( TreeNode node )
    {
      TVITEM item = new TVITEM();
      item.mask = TreeViewItemFlags.STATE | TreeViewItemFlags.HANDLE;
      item.stateMask = 0xFFFF;
      item.hItem = node.Handle;
      Win32Declarations.SendMessage( Handle, TreeViewMessage.TVM_GETITEMA, 0, ref item );
      return (NodeCheckState) (item.state >> 12);
    }

    protected override void WndProc(ref Message m)
    {
      switch (m.Msg)
      {
        case Win32Declarations.OCM_NOTIFY:
          OnWmNotify(ref m);
          break;

        case Win32Declarations.WM_ERASEBKGND:
          if( !myDoubleBuffer )
            base.WndProc( ref m );
          break;

        case Win32Declarations.WM_PAINT:
          if ( !myDoubleBuffer )
            base.WndProc( ref m );
          else
            PaintWithDoubleBuffer( ref m );
          break;

        default:
          base.WndProc(ref m);
          break;
      }
    }

    /// <summary>
    /// Handles the WM_NOTIFY message of the parent control
    /// </summary>
    /// <param name="m">The message to handle</param>
    /// <returns>Whether the message was handled</returns>
    private void OnWmNotify( ref Message m )
    {
      // Marshal lParam into NMHDR:
      NMHDR hdr = new NMHDR();
      hdr = (NMHDR)Marshal.PtrToStructure(m.LParam, typeof(NMHDR));

      switch (hdr.code)
      {
        case Win32Declarations.NM_CUSTOMDRAW:
          NMTVCUSTOMDRAW customDraw = new NMTVCUSTOMDRAW();
          customDraw = (NMTVCUSTOMDRAW)Marshal.PtrToStructure(m.LParam, typeof(NMTVCUSTOMDRAW));

          OnCustomDraw(ref customDraw, ref m);

          break;

        default:
          base.WndProc( ref m );
          break;
      }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
      base.OnSizeChanged (e);
      if ( myDoubleBuffer )
      {
        CreateDoubleBufferBitmap();
      }
    }

    private void CreateDoubleBufferBitmap()
    {
      if ( myDoubleBufferBitmap != null )
      {
        myDoubleBufferBitmap.Dispose();
      }
      if ( ClientSize.Width <= 0 || ClientSize.Height <= 0 )
      {
        myDoubleBufferBitmap = new Bitmap( 1, 1 );
      }
      else
      {
        myDoubleBufferBitmap = new Bitmap( ClientSize.Width, ClientSize.Height );
      }
    }

    private void PaintWithDoubleBuffer( ref Message m )
    {
      using( Graphics g = Graphics.FromImage( myDoubleBufferBitmap ) )
      {
        // TODO: use correct background brush for non-standard background
        g.FillRectangle( SystemBrushes.Window, 0, 0, ClientSize.Width, ClientSize.Height  );
        IntPtr hdc = g.GetHdc();
        // the treeview ignores the drawing options anyway
        Win32Declarations.SendMessage( Handle, Win32Declarations.WM_PRINTCLIENT, hdc, IntPtr.Zero );
        g.ReleaseHdc( hdc );
      }
      PAINTSTRUCT ps = new PAINTSTRUCT();
      Win32Declarations.BeginPaint( m.HWnd, ref ps );
      using( Graphics g = Graphics.FromHdc( ps.hdc ) )
      {
        g.DrawImage( myDoubleBufferBitmap, 0, 0 );
      }
      Win32Declarations.EndPaint( m.HWnd, ref ps );
    }

    #region Drawing logic
    /// <summary>
    /// Erases node
    /// </summary>
    private void EraseNode( ref NMTVCUSTOMDRAW customDraw )
    {
      try
      {
        //TreeNode node = TreeNode.FromHandle(this, (IntPtr)customDraw.nmcd.dwItemSpec);

        using (Graphics g = Graphics.FromHdc(customDraw.nmcd.hdc))
        {
          //int offset = CalculateOffset(node);
          Rectangle rect = new Rectangle(customDraw.nmcd.rc.left/* + offset*/, customDraw.nmcd.rc.top, customDraw.nmcd.rc.right - customDraw.nmcd.rc.left/* - offset*/, customDraw.nmcd.rc.bottom - customDraw.nmcd.rc.top);

          g.FillRectangle(new SolidBrush(BackColor), rect);
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("CustomTreeView.EraseNode failed : " + ex, "UI");
      }
    }

    /// <summary>
    /// Draws node
    /// </summary>
    private void DrawNode( ref NMTVCUSTOMDRAW customDraw )
    {
      try
      {
        TreeNode node = TreeNode.FromHandle(this, (IntPtr)customDraw.nmcd.dwItemSpec);

        if (myNodePainter != null && myNodePainter.IsHandled(node))
        {
          Rectangle rect = new Rectangle(customDraw.nmcd.rc.left, customDraw.nmcd.rc.top, customDraw.nmcd.rc.right - customDraw.nmcd.rc.left, customDraw.nmcd.rc.bottom - customDraw.nmcd.rc.top);

          if (MultiSelect)
          {
            SetNodeSelectedState(node, (node == SelectedNode || mySelectedNodes.Contains(node) ));
          }

          myNodePainter.Draw(node, customDraw.nmcd.hdc, rect);
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("CustomTreeView.DrawNode failed : " + ex, "UI");
      }
    }
    #endregion

    internal bool NeedFocusRect()
    {
        return ShowFocusCues;
    }

    /// <summary>
    /// Handles the NM_CUSTOMDRAW notification
    /// </summary>
    private void OnCustomDraw( ref NMTVCUSTOMDRAW customDraw, ref Message m )
    {
        switch (customDraw.nmcd.dwDrawStage)
        {
            case Win32Declarations.CDDS_PREPAINT:
                m.Result = (IntPtr)(Win32Declarations.CDRF_NOTIFYITEMDRAW | Win32Declarations.CDRF_NOTIFYPOSTPAINT);
                break;

            case Win32Declarations.CDDS_ITEMPREPAINT:
                TreeNode node = TreeNode.FromHandle(this, (IntPtr)customDraw.nmcd.dwItemSpec);

                if (myNodePainter != null && node != null && myNodePainter.IsHandled(node))
                {
                    //DrawNode( ref customDraw );
                    m.Result = (IntPtr)(Win32Declarations.CDRF_NOTIFYITEMDRAW | Win32Declarations.CDRF_NOTIFYPOSTPAINT /*| Win32Declarations.CDRF_SKIPDEFAULT */);
                }
                else
                    m.Result = (IntPtr)Win32Declarations.CDRF_NOTIFYITEMDRAW;
                break;

            case Win32Declarations.CDDS_ITEMPOSTPAINT:
                DrawNode(ref customDraw);
                break;
            case Win32Declarations.CDDS_POSTERASE:
                EraseNode(ref customDraw);
                break;
            default:
                m.Result = (IntPtr)Win32Declarations.CDRF_DODEFAULT;
                break;
        }
    }

      protected override void OnItemDrag( ItemDragEventArgs e )
      {
          base.OnItemDrag( e );
          if ( MultiSelect )
          {
              Invalidate();
          }
      }

      protected override void OnDragEnter( DragEventArgs drgevent )
      {
          myDraggingOver = true;
          base.OnDragEnter( drgevent );
          if ( MultiSelect )
          {
              Refresh();
          }
      }

      protected override void OnDragLeave( EventArgs e )
      {
          base.OnDragLeave( e );
          myDraggingOver = false;
          if ( MultiSelect )
          {
              Invalidate();
          }
      }

      protected override void OnDragDrop( DragEventArgs drgevent )
      {
          base.OnDragDrop( drgevent );
          myDraggingOver = false;
          if ( MultiSelect )
          {
              Invalidate();
          }
      }

      protected override void OnEnter( EventArgs e )
      {
          base.OnEnter( e );
          if ( MultiSelect && SelectedNodes != null && SelectedNodes.Length > 1 )
          {
              Invalidate();
          }
      }

      protected override void OnLeave( EventArgs e )
      {
          base.OnLeave( e );
          if ( MultiSelect && SelectedNodes != null && SelectedNodes.Length > 1 )
          {
              Invalidate();
          }
      }

      internal bool DraggingOver
      {
          get { return myDraggingOver; }
          set { myDraggingOver = value; }
      }

      #region Click tracking
    protected override void OnMouseDown(MouseEventArgs e)
    {
      myMouseCoords = new Point(e.X, e.Y);
      myLastMouseButton = e.Button;
      mySelectionChanged = false;

      base.OnMouseDown (e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
      base.OnMouseUp( e );
      if ( myThreeStateCheckboxes )
      {
        bool doubleClick = false;
        if ( myLastMouseUpPoint.X != -1 )
        {
          if ( Math.Abs( e.X - myLastMouseUpPoint.X ) <= SystemInformation.DoubleClickSize.Width &&
               Math.Abs( e.Y - myLastMouseUpPoint.Y ) <= SystemInformation.DoubleClickSize.Height )
          {
            TimeSpan ts = DateTime.Now - myLastMouseUpTime;
            if ( ts.TotalMilliseconds <= SystemInformation.DoubleClickTime )
            {
              doubleClick = true;
            }
          }
        }
        myLastMouseUpPoint = new Point( e.X,  e.Y );
        myLastMouseUpTime = DateTime.Now;
        if ( doubleClick )
        {
          return;
        }

        TVHITTESTINFO hti = new TVHITTESTINFO();
        hti.pt = new POINT( e.X, e.Y );
        Win32Declarations.SendMessage( Handle, TreeViewMessage.TVM_HITTEST, 0, ref hti );
        if ( ( hti.flags & TreeViewHitTestFlags.ONITEMSTATEICON ) != 0 )
        {
          TreeNode node = TreeNode.FromHandle( this, hti.hItem );
          NodeCheckState state = GetNodeCheckState( node );
          if ( state == NodeCheckState.Checked || state == NodeCheckState.Grayed )
          {
            state = NodeCheckState.Unchecked;
          }
          else
          {
            state = NodeCheckState.Checked;
          }
          ChangeNodeCheckState( node, state );
        }
      }
    }

    /**
     * Changes the checked state of a node and fires the AfterThreeStateCheck event.
     */

    private void ChangeNodeCheckState( TreeNode node, NodeCheckState state )
    {
      SetNodeCheckState( node, state );
      if ( AfterThreeStateCheck != null )
      {
        AfterThreeStateCheck( this, new ThreeStateCheckEventArgs( node, state ) );
      }
    }

    /**
     * Handles selection changes when the user clicks a part of the node which is only
     * included in the area drawn by the custom painter.
     */

    protected override void OnClick(EventArgs e)
    {
      try
      {
        if ( myNodePainter != null && !mySelectionChanged )
        {
          TreeNode pntNode = myNodePainter.GetNodeAt(this, myMouseCoords);
          if (pntNode != null )
          {
            if (MultiSelect)
              SelectedNode = null;
            SelectedNode = pntNode;
          }
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("CustomTreeView.OnClick failed : " + ex, "UI");
      }

      base.OnClick (e);
    }
    #endregion

    #region 3-state checkboxes support

    private void CreateCheckImageList()
    {
      myCheckImageList = new ImageList();

      Bitmap bmp = new Bitmap( 16, 16 );
      using( Graphics g = Graphics.FromImage( bmp ) )
      {
        using( Brush br = new SolidBrush( SystemColors.Window ) )
        {
          g.FillRectangle( br, 0, 0, 16, 16 );
        }
      }
      myCheckImageList.Images.Add( bmp );

      AddCheckIcon( ButtonState.Normal );
      AddCheckIcon( ButtonState.Checked );
      AddCheckIcon( ButtonState.Checked | ButtonState.Inactive );
    }

    private void AddCheckIcon( ButtonState bs )
    {
      Bitmap bmp = new Bitmap( 16, 16 );
      using( Graphics g = Graphics.FromImage( bmp ) )
      {
        ControlPaint.DrawCheckBox( g, 0, 0, 16, 16, bs );
      }
      myCheckImageList.Images.Add( bmp );
    }

    protected override void OnKeyDown( KeyEventArgs e )
    {
      base.OnKeyDown( e );
      if ( myThreeStateCheckboxes && e.KeyCode == Keys.Space )
      {
        TreeNode node = SelectedNode;
        if ( node != null && GetNodeCheckState( node ) != NodeCheckState.None)
        {
          if ( GetNodeCheckState( node ) == NodeCheckState.Checked )
          {
            ChangeNodeCheckState( node, NodeCheckState.Unchecked );
          }
          else
          {
            ChangeNodeCheckState( node, NodeCheckState.Checked );
          }
        }
      }
    }

    #endregion

    #region Multi-Selection support

    #region Selected Nodes Collection
    private class SelectedNodesCollection
    {
      private CustomTreeView myTreeView;
      private ArrayList myNodes = new ArrayList();

      public SelectedNodesCollection(CustomTreeView treeView)
      {
        myTreeView = treeView;
      }

      public void Add (TreeNode node)
      {
        myNodes.Add(node);
        myTreeView.SetNodeSelectedState(node, true);
        myTreeView.InvalidateNode (node);
      }

      public void AddRange (ICollection range)
      {
        foreach (TreeNode node in range)
        {
          if ( node != null )
          {
            myNodes.Add( node );
            myTreeView.SetNodeSelectedState(node, true);
            myTreeView.InvalidateNode (node);
          }
        }
      }

      public void Remove (TreeNode node)
      {
        myNodes.Remove (node);
        myTreeView.SetNodeSelectedState(node, false);
        myTreeView.InvalidateNode (node);
      }

      public void Clear()
      {
        foreach (TreeNode node in myNodes)
        {
          myTreeView.SetNodeSelectedState(node, false);
          myTreeView.InvalidateNode (node);
        }
        myNodes.Clear();
      }

      public int Count
      {
        get {return myNodes.Count;}
      }

      public bool Contains (TreeNode node)
      {
        return myNodes.Contains(node);
      }

      public TreeNode[] Nodes
      {
        get { return (TreeNode[])myNodes.ToArray(typeof(TreeNode)); }
      }
    }
    #endregion

    /// <summary>
    /// Array of currently selected nodes
    /// </summary>
    private SelectedNodesCollection	mySelectedNodes = null;

    private TreeNode myFirstMultiSelectNode, myLastMultiSelectNode;

    private MouseButtons myLastMouseButton;

    /// <summary>
    /// Enables or disable multiselection
    /// </summary>
    public bool MultiSelect
    {
      get { return myMultiSelect; }
      set
      {
        if ( myMultiSelect != value )
        {
            myMultiSelect = value;
            if (myMultiSelect)
                mySelectedNodes = new SelectedNodesCollection(this);
            else
                mySelectedNodes = null;
            OnMultiSelectChanged();
        }
      }
    }

    protected virtual void OnMultiSelectChanged()
    {
        if ( MultiSelectChanged != null )
        {
            MultiSelectChanged( this, EventArgs.Empty );
        }
    }

    public TreeNode[] SelectedNodes
    {
      get
      {
        if ( !MultiSelect )
        {
          if ( SelectedNode == null )
            return new TreeNode[] {};

          return new TreeNode[] { SelectedNode };
        }
        return mySelectedNodes.Nodes;
      }
      set
      {
        if ( value == null || value.Length == 0 )
        {
          SelectedNode = null;
        }
        else
        {
          SelectedNode = value [0];
        }

        if ( MultiSelect )
        {
          mySelectedNodes.Clear();
          for( int i=0; i<value.Length; i++ )
          {
              if ( value [i] != null && value [i].TreeView == this )
              {
                  mySelectedNodes.Add( value [i] );
              }
          }
          myLastMouseButton = MouseButtons.Left;
        }
      }
    }

    protected void SetNodeSelectedState( TreeNode node, bool selected )
    {
      if ( node.TreeView != this )
        return;

      TVITEM item = new TVITEM();
      item.mask = TreeViewItemFlags.STATE | TreeViewItemFlags.HANDLE;
      item.stateMask = (int) TreeViewItemState.SELECTED;
      item.state = selected ? item.stateMask : 0;
      item.hItem = node.Handle;
      Win32Declarations.SendMessage( Handle, TreeViewMessage.TVM_SETITEMA, 0, ref item );
    }

    protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
    {
      base.OnBeforeSelect(e);
      if (e.Cancel || !MultiSelect || e.Node == null)
        return;

      myLastMultiSelectNode = e.Node;
      if (ModifierKeys != Keys.Shift)
        myFirstMultiSelectNode = e.Node; // store begin of shift sequence
    }

    protected override void OnAfterSelect(TreeViewEventArgs e)
    {
      if (e.Node == null)
        return;

      mySelectionChanged = true;
      if (MultiSelect)
      {
        UpdateMultiselection( e.Node );
      }

      base.OnAfterSelect(e);
    }

      private void UpdateMultiselection( TreeNode node )
      {
            bool bControl = (ModifierKeys == Keys.Control);
            bool bShift = (ModifierKeys == Keys.Shift);

            if (bControl)
            {
                if (mySelectedNodes.Contains(node))
                    mySelectedNodes.Remove(node);
                else
                    mySelectedNodes.Add(node);
            }
            else if (bShift)
            {
                Queue myQueue = new Queue();

                TreeNode uppernode = myFirstMultiSelectNode;
                TreeNode bottomnode = node;
                // case 1 : begin and end nodes are parent
                bool bParent = IsParent(myFirstMultiSelectNode, node); // is m_firstNode parent (direct or not) of e.Node
                if (!bParent)
                {
                    bParent = IsParent(bottomnode, uppernode);
                    if (bParent) // swap nodes
                    {
                        TreeNode t = uppernode;
                        uppernode = bottomnode;
                        bottomnode = t;
                    }
                }
                if (bParent)
                {
                    if ( uppernode != null )
                    {
                        TreeNode n = bottomnode;
                        while (n != uppernode.Parent)
                        {
                            if (!mySelectedNodes.Contains(n)) // new node ?
                                myQueue.Enqueue(n);

                            n = n.Parent;
                        }
                    }
                }
                    // case 2 : nor the begin nor the end node are descendant one another
                else
                {
                    if ((uppernode.Parent == null && bottomnode.Parent == null) || (uppernode.Parent != null && uppernode.Parent.Nodes.Contains(bottomnode))) // are they siblings ?
                    {
                        int nIndexUpper = uppernode.Index;
                        int nIndexBottom = bottomnode.Index;
                        if (nIndexBottom < nIndexUpper) // reversed?
                        {
                            TreeNode t = uppernode;
                            uppernode = bottomnode;
                            bottomnode = t;
                            nIndexUpper = uppernode.Index;
                            nIndexBottom = bottomnode.Index;
                        }

                        TreeNode n = uppernode;
                        while (nIndexUpper <= nIndexBottom)
                        {
                            if (!mySelectedNodes.Contains(n)) // new node ?
                                myQueue.Enqueue(n);

                            n = n.NextNode;

                            nIndexUpper++;
                        } // end while

                    }
                    else
                    {
                        if (!mySelectedNodes.Contains(uppernode))
                            myQueue.Enqueue(uppernode);
                        if (!mySelectedNodes.Contains(bottomnode))
                            myQueue.Enqueue(bottomnode);
                    }
                }

                mySelectedNodes.AddRange(myQueue);
                myFirstMultiSelectNode = node; // let us chain several SHIFTs if we like it
            }
            else if ( myLastMouseButton != MouseButtons.Right )   // don't clear selection when popup menu is invoked
            {
                // in the case of a simple click, just add this item
                if (mySelectedNodes != null && mySelectedNodes.Count > 0)
                    mySelectedNodes.Clear();
                mySelectedNodes.Add(node);
            }
      }

      #endregion

    protected override void OnGotFocus( EventArgs e )
    {
      try
      {
        base.OnGotFocus( e );
        if ( SelectedNode != null && myNodePainter != null )
        {
          myNodePainter.InvalidateNode( SelectedNode );
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("CustomTreeView.EraseNode failed : " + ex, "UI");
      }
    }

    public new event EventHandler LostFocus;

    protected override void OnLostFocus(EventArgs e)
    {
      if (LostFocus != null)
        LostFocus(this, e);

      base.OnLostFocus (e);
    }

    /**
     * Marks the node as drop-highlighted.
     */

    public void SetDropHighlightNode( TreeNode node )
    {
      Win32Declarations.SendMessage( Handle, (int) TreeViewMessage.TVM_SELECTITEM,
          (IntPtr) Win32Declarations.TVGN_DROPHILITE, (node != null) ? node.Handle : IntPtr.Zero );
    }

    /**
     * Checks if a node is marked as drop-highlighted.
     */

    public bool IsNodeDropHighlighted( TreeNode node )
    {
      TVITEM item = new TVITEM();
      item.mask = TreeViewItemFlags.HANDLE | TreeViewItemFlags.STATE;
      item.hItem = node.Handle;
      item.stateMask = 0xFFFF;
      Win32Declarations.SendMessage( Handle, TreeViewMessage.TVM_GETITEMA, 0, ref item );
      return (item.state & (int) TreeViewItemState.DROPHILITED) != 0;
    }

    public TreeNode DropHighlightedNode
    {
      get
      {
        int result = Win32Declarations.SendMessage( Handle,
            (int) TreeViewMessage.TVM_GETNEXTITEM, (IntPtr) Win32Declarations.TVGN_DROPHILITE, IntPtr.Zero );
        if ( result == 0 )
          return null;
        return TreeNode.FromHandle( this, new IntPtr( result ) );
      }
    }

    public bool IsParent(TreeNode parentNode, TreeNode childNode)
    {
      if (parentNode == childNode)
        return true;

      TreeNode n = childNode;
      bool bFound = false;
      while (!bFound && n != null)
      {
        n = n.Parent;
        bFound = (n == parentNode);
      }
      return bFound;
    }

    internal void InvalidateNode( TreeNode node )
    {
      if ( node.TreeView == this && myNodePainter != null )
      {
        myNodePainter.InvalidateNode( node );
      }
    }
  }

  public class ThreeStateCheckEventArgs: EventArgs
  {
    private readonly TreeNode       myNode;
    private readonly NodeCheckState myCheckState;

    public ThreeStateCheckEventArgs( TreeNode node, NodeCheckState checkState )
    {
      myNode = node;
      myCheckState = checkState;
    }

    public TreeNode       Node       { get { return myNode; } }
    public NodeCheckState CheckState { get { return myCheckState; } }
  }

  public delegate void ThreeStateCheckEventHandler( object sender, ThreeStateCheckEventArgs e );
}
